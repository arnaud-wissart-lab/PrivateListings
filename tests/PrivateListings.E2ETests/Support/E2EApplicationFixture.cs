using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PrivateListings.Application;
using PrivateListings.Infrastructure;
using PrivateListings.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PrivateListings.E2ETests.Support;

public sealed class E2EApplicationFixture : IAsyncLifetime, IDisposable
{
    private const string EnvironmentName = "Demo";
    private const string DatabaseName = "privatelistings_e2e";
    private const string DatabaseUser = "privatelistings";
    private const string DatabasePassword = "privatelistings_e2e";

    private readonly SemaphoreSlim _resetLock = new(1, 1);
    private readonly ConcurrentQueue<string> _webOutput = new();
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase(DatabaseName)
        .WithUsername(DatabaseUser)
        .WithPassword(DatabasePassword)
        .Build();

    private IServiceProvider? _resetServices;
    private IPlaywright? _playwright;
    private Process? _webProcess;
    private string? _certificatePath;
    private string? _certificatePassword;

    public Uri BaseAddress { get; private set; } = default!;

    public IBrowser Browser { get; private set; } = default!;

    public FakeSmtpServer EmailServer { get; } = new();

    public string RepositoryRoot { get; } = FindRepositoryRoot();

    public string ArtifactDirectory { get; private set; } = default!;

    public bool IsHeaded { get; } = IsTruthy(Environment.GetEnvironmentVariable("E2E_HEADED"));

    public async Task InitializeAsync()
    {
        ArtifactDirectory = Path.Combine(
            RepositoryRoot,
            "output",
            "playwright",
            DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
        Directory.CreateDirectory(ArtifactDirectory);

        await _postgres.StartAsync().ConfigureAwait(false);
        EmailServer.Start();
        _resetServices = CreateResetServiceProvider();
        await ResetDatabaseAsync().ConfigureAwait(false);
        await StartWebApplicationAsync().ConfigureAwait(false);

        _playwright = await Playwright.CreateAsync().ConfigureAwait(false);
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = !IsHeaded
        }).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync().ConfigureAwait(false);
        }

        _playwright?.Dispose();
        await StopWebApplicationAsync().ConfigureAwait(false);

        if (_resetServices is IAsyncDisposable asyncDisposableServices)
        {
            await asyncDisposableServices.DisposeAsync().ConfigureAwait(false);
        }
        else if (_resetServices is IDisposable disposableServices)
        {
            disposableServices.Dispose();
        }

        await EmailServer.DisposeAsync().ConfigureAwait(false);
        await _postgres.DisposeAsync().AsTask().ConfigureAwait(false);

        TryDeleteFile(_certificatePath);
        _resetLock.Dispose();
    }

    public void Dispose()
        => _resetLock.Dispose();

    public async Task ResetAsync()
    {
        await _resetLock.WaitAsync().ConfigureAwait(false);
        try
        {
            EmailServer.Clear();
            await StopWebApplicationAsync().ConfigureAwait(false);
            await ResetDatabaseAsync().ConfigureAwait(false);
            await StartWebApplicationAsync().ConfigureAwait(false);
        }
        finally
        {
            _resetLock.Release();
        }
    }

    public async Task<T> QueryAsync<T>(Func<PrivateListingsDbContext, Task<T>> query)
    {
        ArgumentNullException.ThrowIfNull(query);

        await using var dbContext = CreateDbContext();
        return await query(dbContext).ConfigureAwait(false);
    }

    public string CreateArtifactPath(string fileName)
    {
        var safeName = string.Join(
            "_",
            fileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));

        var fullPath = Path.Combine(ArtifactDirectory, safeName);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        return fullPath;
    }

    public string ReadWebOutput()
        => FormatWebOutput();

    public PrivateListingsDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PrivateListingsDbContext>()
            .UseNpgsql(
                _postgres.GetConnectionString(),
                npgsql => npgsql.MigrationsAssembly(typeof(PrivateListingsDbContext).Assembly.FullName))
            .Options;

        return new PrivateListingsDbContext(options);
    }

    private ServiceProvider CreateResetServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        services.AddDataProtection()
            .SetApplicationName("PrivateListings.E2ETests")
            .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(ArtifactDirectory, "data-protection")));
        services
            .AddPrivateListingsApplication()
            .AddPrivateListingsInfrastructure(BuildConfiguration(), new TestHostEnvironment(EnvironmentName, RepositoryRoot));

        return services.BuildServiceProvider(validateScopes: true);
    }

    private IConfiguration BuildConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(BuildApplicationSettings())
            .Build();

    private Dictionary<string, string?> BuildApplicationSettings()
        => new()
        {
            ["ConnectionStrings:privatelistingsdb"] = _postgres.GetConnectionString(),
            ["PrivateListings:Persistence:ConnectionName"] = "privatelistingsdb",
            ["PrivateListings:Persistence:ApplyMigrationsOnStartup"] = "true",
            ["PrivateListings:ReferenceData:SeedInitialCategories"] = "true",
            ["PrivateListings:DemoSeed:Enabled"] = "true",
            ["PrivateListings:DemoSeed:Accounts:Admin:Email"] = DemoAccounts.Admin.Email,
            ["PrivateListings:DemoSeed:Accounts:Admin:DisplayName"] = "Administrateur Démo",
            ["PrivateListings:DemoSeed:Accounts:Admin:Password"] = DemoAccounts.Admin.Password,
            ["PrivateListings:DemoSeed:Accounts:Moderator:Email"] = DemoAccounts.Moderator.Email,
            ["PrivateListings:DemoSeed:Accounts:Moderator:DisplayName"] = "Modérateur Démo",
            ["PrivateListings:DemoSeed:Accounts:Moderator:Password"] = DemoAccounts.Moderator.Password,
            ["PrivateListings:DemoSeed:Accounts:Member:Email"] = DemoAccounts.Member.Email,
            ["PrivateListings:DemoSeed:Accounts:Member:DisplayName"] = "Membre Démo",
            ["PrivateListings:DemoSeed:Accounts:Member:Password"] = DemoAccounts.Member.Password,
            ["PrivateListings:Email:FromAddress"] = "noreply@privatelistings.local",
            ["PrivateListings:Email:FromName"] = "PrivateListings",
            ["PrivateListings:Email:SmtpHost"] = IPAddress.Loopback.ToString(),
            ["PrivateListings:Email:SmtpPort"] = EmailServer.Port.ToString(CultureInfo.InvariantCulture),
            ["PrivateListings:ListingImages:StorageRoot"] = Path.Combine(ArtifactDirectory, "listing-images"),
            ["PrivateListings:HealthChecks:ExposeEndpoints"] = "true",
            ["Logging:LogLevel:Default"] = "Warning",
            ["Logging:LogLevel:Microsoft.AspNetCore"] = "Warning"
        };

    private async Task ResetDatabaseAsync()
    {
        if (_resetServices is null)
        {
            throw new InvalidOperationException("Les services de réinitialisation E2E ne sont pas initialisés.");
        }

        await using var scope = _resetServices.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        await dbContext.Database.EnsureDeletedAsync().ConfigureAwait(false);

        var initializer = scope.ServiceProvider.GetRequiredService<PrivateListingsDatabaseInitializer>();
        await initializer.InitializeAsync().ConfigureAwait(false);
    }

    private async Task StartWebApplicationAsync()
    {
        var port = GetFreeTcpPort();
        BaseAddress = new Uri($"https://127.0.0.1:{port}");
        (_certificatePath, _certificatePassword) = CreateHttpsCertificate();

        var webProject = Path.Combine(
            RepositoryRoot,
            "src",
            "PrivateListings.Web",
            "PrivateListings.Web.csproj");

        var configuration = ResolveAppConfiguration();
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = RepositoryRoot,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(webProject);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add(configuration);
        startInfo.ArgumentList.Add("--no-build");
        startInfo.ArgumentList.Add("--no-launch-profile");

        ApplyEnvironment(startInfo.Environment);

        _webProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Le processus Web E2E n'a pas pu démarrer.");

        _webProcess.OutputDataReceived += (_, args) => EnqueueOutput(args.Data);
        _webProcess.ErrorDataReceived += (_, args) => EnqueueOutput(args.Data);
        _webProcess.BeginOutputReadLine();
        _webProcess.BeginErrorReadLine();

        await WaitForReadinessAsync().ConfigureAwait(false);
    }

    private void ApplyEnvironment(IDictionary<string, string?> environment)
    {
        foreach (var setting in BuildApplicationSettings())
        {
            environment[setting.Key.Replace(":", "__", StringComparison.Ordinal)] = setting.Value;
        }

        environment["ASPNETCORE_ENVIRONMENT"] = EnvironmentName;
        environment["DOTNET_ENVIRONMENT"] = EnvironmentName;
        environment["ASPNETCORE_URLS"] = BaseAddress.ToString();
        environment["ASPNETCORE_Kestrel__Certificates__Default__Path"] = _certificatePath;
        environment["ASPNETCORE_Kestrel__Certificates__Default__Password"] = _certificatePassword;
        environment["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        environment["DOTNET_NOLOGO"] = "1";
    }

    private async Task WaitForReadinessAsync()
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, errors) =>
                errors == SslPolicyErrors.None || errors == SslPolicyErrors.RemoteCertificateChainErrors
        };
        using var client = new HttpClient(handler)
        {
            BaseAddress = BaseAddress,
            Timeout = TimeSpan.FromSeconds(2)
        };

        var timeoutAt = DateTimeOffset.UtcNow.AddSeconds(90);
        while (DateTimeOffset.UtcNow < timeoutAt)
        {
            if (_webProcess?.HasExited == true)
            {
                throw new InvalidOperationException(
                    $"L'application Web E2E s'est arrêtée avec le code {_webProcess.ExitCode}.{Environment.NewLine}{FormatWebOutput()}");
            }

            try
            {
                using var response = await client.GetAsync("/health").ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }

            await Task.Delay(500).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"L'application Web E2E n'a pas répondu sur {BaseAddress} dans le délai imparti.{Environment.NewLine}{FormatWebOutput()}");
    }

    private async Task StopWebApplicationAsync()
    {
        if (_webProcess is null)
        {
            return;
        }

        if (!_webProcess.HasExited)
        {
            _webProcess.Kill(entireProcessTree: true);
            await _webProcess.WaitForExitAsync().ConfigureAwait(false);
        }

        _webProcess.Dispose();
        _webProcess = null;
    }

    private string FormatWebOutput()
        => string.Join(Environment.NewLine, _webOutput.TakeLast(80));

    private void EnqueueOutput(string? line)
    {
        if (!string.IsNullOrWhiteSpace(line))
        {
            _webOutput.Enqueue(line);
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private static (string Path, string Password) CreateHttpsCertificate()
    {
        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"privatelistings-e2e-{Guid.NewGuid():N}.pfx");
        var password = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        using var key = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=PrivateListings E2E",
            key,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));

        var subjectAlternativeNames = new SubjectAlternativeNameBuilder();
        subjectAlternativeNames.AddDnsName("localhost");
        subjectAlternativeNames.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(subjectAlternativeNames.Build());

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(7));

        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));
        return (path, password);
    }

    private static string ResolveAppConfiguration()
    {
        var configured = Environment.GetEnvironmentVariable("E2E_APP_CONFIGURATION");
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var baseDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        return baseDirectory.Parent?.Name is { Length: > 0 } configuration
            ? configuration
            : "Release";
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PrivateListings.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("La racine du dépôt PrivateListings est introuvable.");
    }

    private static bool IsTruthy(string? value)
        => value is not null
            && (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase));

    private static void TryDeleteFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
