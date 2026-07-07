using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PrivateListings.Application.Authentication;
using PrivateListings.Infrastructure.Identity;
using PrivateListings.Infrastructure.Persistence;
using PrivateListings.Infrastructure.Persistence.Seeding;
using PrivateListings.Web.Components.Account.Pages;

namespace PrivateListings.IntegrationTests;

[Collection(PostgreSqlTestCollectionDefinition.Name)]
public sealed class UserAuthenticationIntegrationTests(PostgreSqlFixture fixture)
{
    private const string ValidPassword = "phrase de passe privee 2026";
    private const string NewPassword = "nouvelle phrase de passe 2026";

    [Fact]
    public async Task InscriptionValideCreeUtilisateurProfilJetonEtAudit()
    {
        await fixture.ResetDatabaseAsync();
        var emailSender = new CapturingEmailSender();
        await using var serviceProvider = CreateServiceProvider(emailSender);
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var accountService = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();

        var result = await accountService.RegisterAsync(new RegisterAccountCommand(
            "membre@example.test",
            "Membre Test",
            ValidPassword,
            "https://localhost/confirm-email",
            CreateRequestContext()));

        Assert.Equal(AccountOperationStatus.RequiresEmailConfirmation, result.Status);
        Assert.NotNull(emailSender.ConfirmationLink);
        Assert.False(emailSender.PasswordWasSent);
        Assert.Equal(1, await dbContext.Users.CountAsync());
        Assert.Equal(1, await dbContext.UserProfiles.CountAsync());
        Assert.Equal(1, await dbContext.AccountSecurityTokens.CountAsync(token => token.ConsumedAt == null));
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Account.Registered"));
    }

    [Fact]
    public async Task InscriptionInvalideRefuseMotDePasseCourt()
    {
        await fixture.ResetDatabaseAsync();
        var emailSender = new CapturingEmailSender();
        await using var serviceProvider = CreateServiceProvider(emailSender);
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var accountService = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();

        var result = await accountService.RegisterAsync(new RegisterAccountCommand(
            "membre@example.test",
            "Membre Test",
            "court",
            "https://localhost/confirm-email",
            CreateRequestContext()));

        Assert.Equal(AccountOperationStatus.Failed, result.Status);
        Assert.Null(emailSender.ConfirmationLink);
        Assert.False(await dbContext.Users.AnyAsync());
        Assert.False(await dbContext.UserProfiles.AnyAsync());
    }

    [Fact]
    public async Task ConfirmationEmailConsommeLeLienEtCreeAudit()
    {
        await fixture.ResetDatabaseAsync();
        var emailSender = new CapturingEmailSender();
        await using var serviceProvider = CreateServiceProvider(emailSender);
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var accountService = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PrivateListingsUser>>();
        var callback = await RegisterAndReadConfirmationLinkAsync(accountService, emailSender);

        var result = await accountService.ConfirmEmailAsync(new ConfirmEmailCommand(
            callback.UserId,
            callback.Code,
            CreateRequestContext()));
        var replay = await accountService.ConfirmEmailAsync(new ConfirmEmailCommand(
            callback.UserId,
            callback.Code,
            CreateRequestContext()));
        var user = await userManager.FindByIdAsync(callback.UserId.ToString("D"));

        Assert.True(result.Succeeded);
        Assert.False(replay.Succeeded);
        Assert.NotNull(user);
        Assert.True(await userManager.IsEmailConfirmedAsync(user));
        Assert.Equal(1, await dbContext.AccountSecurityTokens.CountAsync(token => token.ConsumedAt != null));
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Account.EmailConfirmed"));
    }

    [Fact]
    public async Task LoginValideCreeAudit()
    {
        await fixture.ResetDatabaseAsync();
        var emailSender = new CapturingEmailSender();
        await using var serviceProvider = CreateServiceProvider(emailSender);
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var accountService = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        await RegisterAndConfirmAsync(accountService, emailSender);
        ConfigureHttpContext(scope.ServiceProvider);

        var result = await accountService.LoginAsync(new LoginAccountCommand(
            "membre@example.test",
            ValidPassword,
            RememberMe: false,
            CreateRequestContext()));

        Assert.True(result.Succeeded);
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Account.LoginSucceeded"));
    }

    [Fact]
    public async Task LoginInvalideCreeAudit()
    {
        await fixture.ResetDatabaseAsync();
        var emailSender = new CapturingEmailSender();
        await using var serviceProvider = CreateServiceProvider(emailSender);
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var accountService = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        await RegisterAndConfirmAsync(accountService, emailSender);

        var result = await accountService.LoginAsync(new LoginAccountCommand(
            "membre@example.test",
            "mauvais mot de passe",
            RememberMe: false,
            CreateRequestContext()));

        Assert.False(result.Succeeded);
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Account.LoginFailed"));
    }

    [Fact]
    public async Task ResetPasswordConsommeLeLienEtPermetNouveauLogin()
    {
        await fixture.ResetDatabaseAsync();
        var emailSender = new CapturingEmailSender();
        await using var serviceProvider = CreateServiceProvider(emailSender);
        await using var scope = serviceProvider.CreateAsyncScope();
        await InitializeIdentityAsync(scope.ServiceProvider);
        var accountService = scope.ServiceProvider.GetRequiredService<IUserAccountService>();
        var dbContext = scope.ServiceProvider.GetRequiredService<PrivateListingsDbContext>();
        await RegisterAndConfirmAsync(accountService, emailSender);

        var requestResult = await accountService.RequestPasswordResetAsync(new RequestPasswordResetCommand(
            "membre@example.test",
            "https://localhost/reset-password",
            CreateRequestContext()));
        var callback = ReadCallback(emailSender.ResetLink);
        var resetResult = await accountService.ResetPasswordAsync(new ResetPasswordCommand(
            callback.UserId,
            callback.Code,
            NewPassword,
            CreateRequestContext()));
        ConfigureHttpContext(scope.ServiceProvider);
        var loginResult = await accountService.LoginAsync(new LoginAccountCommand(
            "membre@example.test",
            NewPassword,
            RememberMe: false,
            CreateRequestContext()));

        Assert.True(requestResult.Succeeded);
        Assert.True(resetResult.Succeeded);
        Assert.True(loginResult.Succeeded);
        Assert.Equal(2, await dbContext.AccountSecurityTokens.CountAsync(token => token.ConsumedAt != null));
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Account.PasswordResetRequested"));
        Assert.True(await dbContext.AuditLogs.AnyAsync(auditLog => auditLog.Action == "Account.PasswordResetSucceeded"));
    }

    [Fact]
    public void AccesNonAutoriseEstRefuseSurLaPageSecuriteCompte()
    {
        var attributes = typeof(AccountSecurity).GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true);

        Assert.NotEmpty(attributes);
    }

    private ServiceProvider CreateServiceProvider(CapturingEmailSender emailSender)
        => fixture.CreateServiceProvider(configureServices: services =>
        {
            services.AddSingleton<IEmailSender<PrivateListingsUser>>(emailSender);
        });

    private static async Task InitializeIdentityAsync(IServiceProvider serviceProvider)
    {
        ConfigureHttpContext(serviceProvider);
        await serviceProvider.GetRequiredService<PrivateListingsIdentitySeeder>().SeedAsync();
    }

    private static void ConfigureHttpContext(IServiceProvider serviceProvider)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = serviceProvider
        };
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Request.Headers["User-Agent"] = "PrivateListings.Tests";

        serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = context;
    }

    private static async Task RegisterAndConfirmAsync(
        IUserAccountService accountService,
        CapturingEmailSender emailSender)
    {
        var callback = await RegisterAndReadConfirmationLinkAsync(accountService, emailSender);
        var result = await accountService.ConfirmEmailAsync(new ConfirmEmailCommand(
            callback.UserId,
            callback.Code,
            CreateRequestContext()));

        Assert.True(result.Succeeded);
    }

    private static async Task<CallbackValues> RegisterAndReadConfirmationLinkAsync(
        IUserAccountService accountService,
        CapturingEmailSender emailSender)
    {
        var registerResult = await accountService.RegisterAsync(new RegisterAccountCommand(
            "membre@example.test",
            "Membre Test",
            ValidPassword,
            "https://localhost/confirm-email",
            CreateRequestContext()));

        Assert.Equal(AccountOperationStatus.RequiresEmailConfirmation, registerResult.Status);
        return ReadCallback(emailSender.ConfirmationLink);
    }

    private static CallbackValues ReadCallback(string? link)
    {
        Assert.False(string.IsNullOrWhiteSpace(link));

        var uri = new Uri(link);
        var query = QueryHelpers.ParseQuery(uri.Query);
        var userId = Guid.Parse(query["userId"].ToString());
        var code = query["code"].ToString();

        Assert.False(string.IsNullOrWhiteSpace(code));
        return new CallbackValues(userId, code);
    }

    private static AccountRequestContext CreateRequestContext()
        => new("127.0.0.1", "PrivateListings.Tests");

    private sealed record CallbackValues(Guid UserId, string Code);

    private sealed class CapturingEmailSender : IEmailSender<PrivateListingsUser>
    {
        public string? ConfirmationLink { get; private set; }

        public string? ResetLink { get; private set; }

        public bool PasswordWasSent { get; private set; }

        public Task SendConfirmationLinkAsync(
            PrivateListingsUser user,
            string email,
            string confirmationLink)
        {
            ConfirmationLink = confirmationLink;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetLinkAsync(
            PrivateListingsUser user,
            string email,
            string resetLink)
        {
            ResetLink = resetLink;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetCodeAsync(
            PrivateListingsUser user,
            string email,
            string resetCode)
        {
            PasswordWasSent = true;
            return Task.CompletedTask;
        }
    }
}
