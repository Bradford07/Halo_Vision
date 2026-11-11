using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using HaloVision.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HaloVision.Api.Tests;

// Starts the whole backend in memory for a test, with its own empty in-memory SQLite database.
// Every test that needs the API or the database creates one of these:
//
//     using var factory = new TestAppFactory();
//     var client = factory.CreateClient();
//
// Each factory gets a brand-new database, so tests never see each other's data.
public class TestAppFactory : WebApplicationFactory<Program>
{
    // A unique name, so every factory gets its own in-memory database.
    private readonly string _connectionString =
        $"Data Source=HaloVisionTest-{Guid.NewGuid()};Mode=Memory;Cache=Shared";

    // An in-memory SQLite database disappears when its last connection closes.
    // We keep this one connection open for the whole test so the data stays alive.
    private readonly SqliteConnection _keepAlive;

    public TestAppFactory()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();

        // Outside Development, Program.cs marks the login cookie Secure, so it is only sent over HTTPS.
        // Use an https:// address so test clients send the athlete's cookie back like a real browser would.
        ClientOptions.BaseAddress = new Uri("https://localhost");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Testing" is not "Development", so Program.cs skips the fake-data seeder.
        builder.UseEnvironment("Testing");

        // Point the app at our in-memory database instead of halovision.db.
        builder.UseSetting("ConnectionStrings:Default", _connectionString);
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // Build the tables by running the real migrations, which also proves the migrations work.
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();

        return host;
    }

    // The same JSON settings the API uses (camelCase names, enums as text),
    // so tests send and read JSON exactly like the web app and headset will.
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    // Creates a new athlete account, signs it in, and returns an HttpClient that carries
    // that athlete's login cookie. Each call gets its own client, so two athletes never mix up.
    public async Task<HttpClient> CreateAthleteClientAsync(string email)
    {
        const string password = "Test-Password1!";
        var client = CreateClient();

        var register = await client.PostAsJsonAsync("/api/auth/register", new { email, password });
        register.EnsureSuccessStatusCode();

        // useCookies=true: sign in with a cookie (what the web app uses) instead of a token.
        var login = await client.PostAsJsonAsync("/api/auth/login?useCookies=true", new { email, password });
        login.EnsureSuccessStatusCode();

        return client;
    }

    //Saves the signed-in athletes ownprofile through the API and returns whatthe server saved
    public static async Task<HaloVision.Shared.AthleteProfile> SaveProfileAsync(HttpClient athlete, string name)
    {
        var response = await athlete.PutAsJsonAsync("/api/my-profile", new HaloVision.Shared.AthleteProfile { Name = name }, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HaloVision.Shared.AthleteProfile>(Json))!;
    }

    // Pairs a new headset with the athlete's profile, the same way the real web app and headset will:
    //The athlete makes a code, then an anonymous client claims it
    // Returns an HttpClient that sends the headset's token on every request.
    public async Task<HttpClient> PairHeadsetAsync(HttpClient athlete)
    {
        var codeResponse = await athlete.PostAsync("/api/pairing/codes", null);
        codeResponse.EnsureSuccessStatusCode();
        var code = (await codeResponse.Content.ReadFromJsonAsync<CodeResult>(Json))!.Code;

        var headset = CreateClient();
        var claimResponse = await headset.PostAsJsonAsync("/api/pairing/claim", new { code });
        claimResponse.EnsureSuccessStatusCode();
        var token = (await claimResponse.Content.ReadFromJsonAsync<ClaimResult>(Json))!.Token;

        headset.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return headset;
    }

    // Shapes of the pairing responses, for reading them in tests.
    public record CodeResult(string Code, DateTime ExpiresAt);
    public record ClaimResult(string Token, int DeviceId, int ProfileId);

    // Gives a test direct access to the database, for setting up data and checking results.
    // Dispose the scope when you're done: using var scope = factory.Services.CreateScope();
    public AppDbContext GetDb(IServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}
