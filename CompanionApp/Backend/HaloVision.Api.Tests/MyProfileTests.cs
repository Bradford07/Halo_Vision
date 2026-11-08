using System.Net;
using System.Net.Http.Json;
using HaloVision.Shared;

namespace HaloVision.Api.Tests;

// Tests for /api/my-profile: athlete login, each athlete only sees thier own prfile, and Version on every save. 
public class MyProfileTests
{

    // Without signing in, the profile is off limits.
    [Fact]
    public async Task Not_signed_in_gets_401()
    {
        using var factory = new TestAppFactory();
        var anonymous = factory.CreateClient();

        var response = await anonymous.GetAsync("/api/my-profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // A new account has no profyle yet (404). The first save creates it at version 1. 
    [Fact]
    public async Task First_save_creates_the_profile_at_version_1()
    {
        using var factory = new TestAppFactory();
        var athlete = await factory.CreateAthleteClientAsync("athlete-a@example.com");

        var before = await athlete.GetAsync("/api/my-profile");
        var created = await TestAppFactory.SaveProfileAsync(athlete, "Athlete One");
        var after = await athlete.GetFromJsonAsync<AthleteProfile>("/api/my-profile", TestAppFactory.Json);

        //Tests everything is expected
        // No profile until first save
        Assert.Equal(HttpStatusCode.NotFound, before.StatusCode); 

        // A new profile starts at version 1
        Assert.True(created.ProfileID > 0);
        Assert.Equal(1, created.Version);
        Assert.Equal("Athlete One", after!.Name);
    }

    // Each athlete only ever sees thier own profile. There is no id in the URL so B cant ask for As
    [Fact]
    public async Task Each_athlete_only_sees_their_own_profile()
    {
        using var factory = new TestAppFactory();

        var athleteA = await factory.CreateAthleteClientAsync("athlete-a@example.com");
        var athleteB = await factory.CreateAthleteClientAsync("athlete-b@example.com");

        await TestAppFactory.SaveProfileAsync(athleteA, "Belongs To A");

        //B hasn't saved a profile yet, so B gets 404, not A's profile.
        var readByB = await athleteB.GetAsync("/api/my-profile");

        //B's save creates B's own profile and leaves A's alone.
        await TestAppFactory.SaveProfileAsync(athleteB, "Belongs To B");

        var profileA = await athleteA.GetFromJsonAsync<AthleteProfile>("/api/my-profile", TestAppFactory.Json);

        //Test expected
        Assert.Equal(HttpStatusCode.NotFound, readByB.StatusCode);
        Assert.Equal("Belongs To A", profileA!.Name);
        Assert.Equal(1, profileA.Version);

    }

    // Each ssave raises Version by 1 and updates UpdatedAt
    [Fact]
    public async Task Each_save_raises_version_and_sets_updated_at()
    {
        using var factory = new TestAppFactory();
        var athlete = await factory.CreateAthleteClientAsync("athlete-a@example.com");
        var profile = await TestAppFactory.SaveProfileAsync(athlete, "Athlete One");

        profile.Name = "Renamed";
        profile.Brightness = 0.5f;
        var first = await athlete.PutAsJsonAsync("/api/my-profile", profile, TestAppFactory.Json);
        var second = await athlete.PutAsJsonAsync("/api/my-profile", profile, TestAppFactory.Json);
        var saved = await second.Content.ReadFromJsonAsync<AthleteProfile>(TestAppFactory.Json);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(3, saved!.Version);            // 1 at creation, +1, +1
        Assert.Equal("Renamed", saved.Name);
        Assert.Equal(0.5f, saved.Brightness);
        Assert.True(saved.UpdatedAt >= profile.UpdatedAt);
    }

    // Bad values are rejected with 400 and nothing is saved.
    [Fact]
    public async Task Invalid_profile_is_rejected()
    {
        using var factory = new TestAppFactory();
        var athlete = await factory.CreateAthleteClientAsync("athlete-a@example.com");

        var response = await athlete.PutAsJsonAsync("/api/my-profile",
            new AthleteProfile { Name = "Too Bright", Brightness = 5f, OverlayColor = "blue" },
            TestAppFactory.Json);
        var readAfter = await athlete.GetAsync("/api/my-profile");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, readAfter.StatusCode); //nothing was saved
    }


    // After logging out, the athlete's cookie no longer works.
    [Fact]
    public async Task Logout_ends_the_session()
    {
        using var factory = new TestAppFactory();
        var athlete = await factory.CreateAthleteClientAsync("athlete-a@example.com");

        var logout = await athlete.PostAsync("/api/auth/logout", null);
        var afterLogout = await athlete.GetAsync("/api/my-profile");

        //Check that logout successfull and you can no longer access the athlete's prfile
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }
}
