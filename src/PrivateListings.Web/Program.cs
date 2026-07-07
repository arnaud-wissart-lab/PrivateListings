using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using PrivateListings.Application;
using PrivateListings.Infrastructure;
using PrivateListings.Infrastructure.Identity;
using PrivateListings.Web.Components;
using PrivateListings.Web.Components.Account;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddPrivateListingsApplication()
    .AddPrivateListingsInfrastructure(builder.Configuration, builder.Environment);

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "__Host-PrivateListings.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/access-denied";
    options.SlidingExpiration = true;
});

// Ajoute les services Blazor.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<PrivateListings.Infrastructure.Persistence.PrivateListingsDatabaseInitializer>();
    await initializer.InitializeAsync();
}

// Configure le pipeline HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // La durée HSTS par défaut est de 30 jours. Elle devra être ajustée avant la production.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapAdditionalIdentityEndpoints();
app.MapDefaultEndpoints();

app.Run();
