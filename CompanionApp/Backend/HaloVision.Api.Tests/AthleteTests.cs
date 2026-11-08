// Written with Claude Code, reviewed by Owen Cespon
using System.Net;
using System.Net.Http.Json;
using HaloVision.Shared;

namespace HaloVision.Api.Tests;

// Tests for /api/athletes: guide login, guide isolation, and Version on every save.
public class AthleteTests
{
    // Helper: create an athlete through the API and return what the server saved.
    private static async Task<AthleteProfile> CreateAthleteAsync(HttpClient guide, string name)
    {
        var response = await guide.PostAsJsonAsync(
            "/api/athletes", new AthleteProfile { Name = name }, TestAppFactory.Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AthleteProfile>(TestAppFactory.Json))!;
    }

    // Without signing in, the athlete list is off limits.
    [Fact]
    public async Task Not_signed_in_gets_401()
    {
        using var factory = new TestAppFactory();
        var anonymous = factory.CreateClient();

        var response = await anonymous.GetAsync("/api/athletes");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // A guide can create an athlete and then see it in their list.
    [Fact]
    public async Task Guide_can_create_and_list_their_athletes()
    {
        using var factory = new TestAppFactory();
        var guide = await factory.CreateGuideClientAsync("guide-a@example.com");

        var created = await CreateAthleteAsync(guide, "Athlete One");
        var list = await guide.GetFromJsonAsync<List<AthleteProfile>>("/api/athletes", TestAppFactory.Json);

        Assert.True(created.ProfileID > 0);
        Assert.Equal(1, created.Version);                 // a new profile starts at version 1
        var only = Assert.Single(list!);
        Assert.Equal("Athlete One", only.Name);
    }

    // REQUIRED (P4 table): guide A cannot read or edit guide B's athletes.
    [Fact]
    public async Task Guide_cannot_read_or_edit_another_guides_athlete()
    {
        using var factory = new TestAppFactory();
        var guideA = await factory.CreateGuideClientAsync("guide-a@example.com");
        var guideB = await factory.CreateGuideClientAsync("guide-b@example.com");
        var athleteOfA = await CreateAthleteAsync(guideA, "Belongs To A");
        var url = $"/api/athletes/{athleteOfA.ProfileID}";

        // Guide B tries to read, edit, and delete guide A's athlete.
        var read = await guideB.GetAsync(url);
        var edit = await guideB.PutAsJsonAsync(url, new AthleteProfile { Name = "Hacked" }, TestAppFactory.Json);
        var delete = await guideB.DeleteAsync(url);
        var listOfB = await guideB.GetFromJsonAsync<List<AthleteProfile>>("/api/athletes", TestAppFactory.Json);

        // Every attempt looks like the athlete doesn't exist, and B's list is empty.
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, edit.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Empty(listOfB!);

        // And guide A's athlete is untouched.
        var stillThere = await guideA.GetFromJsonAsync<AthleteProfile>(url, TestAppFactory.Json);
        Assert.Equal("Belongs To A", stillThere!.Name);
        Assert.Equal(1, stillThere.Version);
    }

    // Each guide save raises Version by 1 and updates UpdatedAt (in UTC).
    [Fact]
    public async Task Each_save_raises_version_and_sets_updated_at()
    {
        using var factory = new TestAppFactory();
        var guide = await factory.CreateGuideClientAsync("guide-a@example.com");
        var athlete = await CreateAthleteAsync(guide, "Athlete One");
        var url = $"/api/athletes/{athlete.ProfileID}";

        athlete.Name = "Renamed";
        athlete.Brightness = 0.5f;
        var first = await guide.PutAsJsonAsync(url, athlete, TestAppFactory.Json);
        var second = await guide.PutAsJsonAsync(url, athlete, TestAppFactory.Json);
        var saved = await second.Content.ReadFromJsonAsync<AthleteProfile>(TestAppFactory.Json);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(3, saved!.Version);                   // 1 at creation, +1, +1
        Assert.Equal("Renamed", saved.Name);
        Assert.Equal(0.5f, saved.Brightness);
        Assert.Equal(DateTimeKind.Utc, saved.UpdatedAt.Kind);
        Assert.True(saved.UpdatedAt >= athlete.UpdatedAt);
    }

    // Bad values are rejected with 400 and nothing is saved.
    [Fact]
    public async Task Invalid_profile_is_rejected()
    {
        using var factory = new TestAppFactory();
        var guide = await factory.CreateGuideClientAsync("guide-a@example.com");

        var response = await guide.PostAsJsonAsync(
            "/api/athletes",
            new AthleteProfile { Name = "Too Bright", Brightness = 5f, OverlayColor = "blue" },
            TestAppFactory.Json);
        var list = await guide.GetFromJsonAsync<List<AthleteProfile>>("/api/athletes", TestAppFactory.Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(list!);
    }

    // A guide can delete their own athlete.
    [Fact]
    public async Task Guide_can_delete_their_athlete()
    {
        using var factory = new TestAppFactory();
        var guide = await factory.CreateGuideClientAsync("guide-a@example.com");
        var athlete = await CreateAthleteAsync(guide, "Delete Me");
        var url = $"/api/athletes/{athlete.ProfileID}";

        var delete = await guide.DeleteAsync(url);
        var readAfter = await guide.GetAsync(url);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, readAfter.StatusCode);
    }

    // After logging out, the guide's cookie no longer works.
    [Fact]
    public async Task Logout_ends_the_session()
    {
        using var factory = new TestAppFactory();
        var guide = await factory.CreateGuideClientAsync("guide-a@example.com");

        var logout = await guide.PostAsync("/api/auth/logout", null);
        var afterLogout = await guide.GetAsync("/api/athletes");

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }
}
