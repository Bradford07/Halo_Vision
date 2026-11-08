using System.Text.Json.Serialization;
using HaloVision.Api;
using HaloVision.Api.Auth;
using HaloVision.Api.Data;
using HaloVision.Api.Features.Athletes;
using HaloVision.Api.Features.Devices;
using HaloVision.Api.Features.Pairing;
using HaloVision.Api.Features.Sync;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

//Set up the web server:: settings, logging, database and logins
var builder = WebApplication.CreateBuilder(args);

//Register the database: SQLite, using the "Default" connection string
//(appsettings.Development.json on our PCs; the tests supply their own).
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

//Register athlete accounts (ASP.NET Core Identity), stored in our database,
//plus the ready-made register/login endpoints that MapIdentityApi adds below.
builder.Services.AddIdentityApiEndpoints<IdentityUser>()
    .AddEntityFrameworkStores<AppDbContext>();

//Athletes stay signed in with a cookie. This is an API, not a website with a login page,
//so when someone isn't signed in we answer 401/403 instead of redirecting to a login page.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

//Headsets log in differently: with the secret token they got when they paired ("Device" scheme).
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, DeviceAuthHandler>(DeviceAuthHandler.SchemeName, null);

//Each policy names exactly one login scheme on purpose:
//a headset token can never count as an athlete login, and an athlete cookie can never count as a headset.
builder.Services.AddAuthorizationBuilder()
    //"Athlete": an athlete signed in with the Identity cookie (the phone web app).
    .AddPolicy(AuthPolicies.Athlete, policy => policy
        .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme)
        .RequireAuthenticatedUser())
    //"Device": a paired headset sending its token.
    .AddPolicy(AuthPolicies.Device, policy => policy
        .AddAuthenticationSchemes(DeviceAuthHandler.SchemeName)
        .RequireAuthenticatedUser());

//JSON settings for every endpoint: property names in camelCase (the default for web APIs)
//and enums as text ("Peripheral") instead of numbers, matching the database and the headset.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

//Build the app from that setup.
var app = builder.Build();

//On our PCs only: build the database from the migrations and add fake data if it's empty.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DevDataSeeder.SeedAsync(scope.ServiceProvider);
}

//Check who is calling (authentication), then whether they're allowed (authorization).
app.UseAuthentication();
app.UseAuthorization();

//Endpoint that answers "server is running".
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

//Athlete accounts: POST /api/auth/register, POST /api/auth/login?useCookies=true, and more.
var auth = app.MapGroup("/api/auth");
auth.MapIdentityApi<IdentityUser>();

//MapIdentityApi has no logout, so we add one: it clears the athlete's login cookie.
auth.MapPost("/logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.NoContent();
}).RequireAuthorization(AuthPolicies.Athlete);

//The signed-in athlete's own profile (one account = one profile).
app.MapMyProfileEndpoints();

//Pairing an athlete's headset with their profile (replaces P3's Bluetooth pair()).
app.MapPairingEndpoints();

//Profile sync for a paired headset: download the latest copy and upload changes made on it
app.MapProfileSyncEndpoints();

//Endpoints a paired headset calls about itself.
app.MapDeviceEndpoints();

//Start the serer and keep it running until the user stops it.
app.Run();

//Lets the test project start this app in memory (WebApplicationFactory<Program>).
public partial class Program { }
