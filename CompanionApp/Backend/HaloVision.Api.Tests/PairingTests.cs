// Written with Claude Code, reviewed by Owen Cespon
using System.Net;
using System.Net.Http.Json;
using HaloVision.Api.Auth;
using HaloVision.Api.Features.Pairing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HaloVision.Api.Tests.TestAppFactory;

namespace HaloVision.Api.Tests;

// Tests for pairing a headset: POST /api/pairing/codes and POST /api/pairing/claim.
public class PairingTests
{
    // Helper: a signed-in guide with one athlete, plus a fresh pairing code for that athlete.
    private static async Task<(HttpClient Guide, int ProfileId, string Code)> GuideWithCodeAsync(TestAppFactory factory)
    {
        var guide = await factory.CreateGuideClientAsync("guide-a@example.com");
        var athlete = await CreateAthleteAsync(guide, "Athlete One");
        var response = await guide.PostAsJsonAsync("/api/pairing/codes", new { profileId = athlete.ProfileID });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var code = (await response.Content.ReadFromJsonAsync<CodeResult>(Json))!.Code;
        return (guide, athlete.ProfileID, code);
    }

    // REQUIRED (P4 table): a valid code works once.
    [Fact]
    public async Task Valid_code_works_once()
    {
        using var factory = new TestAppFactory();
        var (_, profileId, code) = await GuideWithCodeAsync(factory);
        var headset = factory.CreateClient();

        var first = await headset.PostAsJsonAsync("/api/pairing/claim", new { code });
        var second = await headset.PostAsJsonAsync("/api/pairing/claim", new { code });

        // The first claim pairs the headset and returns a token for the right athlete.
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var claim = (await first.Content.ReadFromJsonAsync<ClaimResult>(Json))!;
        Assert.Equal(profileId, claim.ProfileId);
        Assert.False(string.IsNullOrEmpty(claim.Token));

        // REQUIRED: reusing the same code fails.
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    // REQUIRED (P4 table): an expired code fails.
    [Fact]
    public async Task Expired_code_fails()
    {
        using var factory = new TestAppFactory();
        var (_, _, code) = await GuideWithCodeAsync(factory);

        // Pretend 11 minutes have passed: move the code's expiry into the past.
        using (var scope = factory.Services.CreateScope())
        {
            await factory.GetDb(scope).PairingCodes
                .Where(c => c.Code == code)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.ExpiresAt, DateTime.UtcNow.AddMinutes(-1)));
        }

        var response = await factory.CreateClient().PostAsJsonAsync("/api/pairing/claim", new { code });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // A made-up code fails.
    [Fact]
    public async Task Unknown_code_fails()
    {
        using var factory = new TestAppFactory();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/pairing/claim", new { code = "ZZZZZZ" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Codes are 6 characters from the easy-to-read alphabet, and typing them in lowercase still works.
    [Fact]
    public async Task Code_is_six_readable_characters_and_case_insensitive()
    {
        using var factory = new TestAppFactory();
        var (_, _, code) = await GuideWithCodeAsync(factory);

        Assert.Equal(PairingEndpoints.CodeLength, code.Length);
        Assert.All(code, c => Assert.Contains(c, PairingEndpoints.CodeAlphabet));

        var response = await factory.CreateClient().PostAsJsonAsync(
            "/api/pairing/claim", new { code = $" {code.ToLowerInvariant()} " });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // A code lasts 10 minutes.
    [Fact]
    public async Task Code_expires_after_ten_minutes()
    {
        using var factory = new TestAppFactory();
        var guide = await factory.CreateGuideClientAsync("guide-a@example.com");
        var athlete = await CreateAthleteAsync(guide, "Athlete One");

        var response = await guide.PostAsJsonAsync("/api/pairing/codes", new { profileId = athlete.ProfileID });
        var result = (await response.Content.ReadFromJsonAsync<CodeResult>(Json))!;

        var minutesLeft = (result.ExpiresAt - DateTime.UtcNow).TotalMinutes;
        Assert.InRange(minutesLeft, 9.5, 10.0);
    }

    // A guide can't make a code for another guide's athlete (answers 404, as if it didn't exist).
    [Fact]
    public async Task Guide_cannot_make_code_for_another_guides_athlete()
    {
        using var factory = new TestAppFactory();
        var guideA = await factory.CreateGuideClientAsync("guide-a@example.com");
        var guideB = await factory.CreateGuideClientAsync("guide-b@example.com");
        var athleteOfA = await CreateAthleteAsync(guideA, "Belongs To A");

        var response = await guideB.PostAsJsonAsync("/api/pairing/codes", new { profileId = athleteOfA.ProfileID });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // Only a signed-in guide can make codes.
    [Fact]
    public async Task Not_signed_in_cannot_make_codes()
    {
        using var factory = new TestAppFactory();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/pairing/codes", new { profileId = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // The database stores only the token's hash, never the token itself.
    [Fact]
    public async Task Only_the_token_hash_is_stored()
    {
        using var factory = new TestAppFactory();
        var (_, _, code) = await GuideWithCodeAsync(factory);

        var response = await factory.CreateClient().PostAsJsonAsync("/api/pairing/claim", new { code });
        var claim = (await response.Content.ReadFromJsonAsync<ClaimResult>(Json))!;

        using var scope = factory.Services.CreateScope();
        var device = await factory.GetDb(scope).Devices.SingleAsync(d => d.Id == claim.DeviceId);
        Assert.NotEqual(claim.Token, device.TokenHash);
        Assert.Equal(DeviceTokens.Hash(claim.Token), device.TokenHash);
    }
}
