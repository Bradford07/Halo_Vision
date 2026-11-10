using System.Net;
using System.Net.Http.Json;
using HaloVision.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HaloVision.Api.Tests.TestAppFactory;

namespace HaloVision.Api.Tests;

// Tests for headset profile sync: GET /api/profile and PUT /api/profile.
// Sync rule: the server saves the headset's copy only if its Version is higher than the server's.
public class ProfileSyncTests
{
    // Helper: a signed-in athlete with a saved profile (Version 1), plus a headset paired to it.
    private static async Task<(HttpClient Athlete, HttpClient Headset)> AthleteWithHeadsetAsync(TestAppFactory factory)
    {
        var athlete = await factory.CreateAthleteClientAsync("athlete-a@example.com");
        await SaveProfileAsync(athlete, "Athlete One");
        var headset = await factory.PairHeadsetAsync(athlete);
        return (athlete, headset);
    }

    // Helper: the headset downloads its copy of the profile.
    private static async Task<AthleteProfile> DownloadAsync(HttpClient headset)
    {
        var response = await headset.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AthleteProfile>(Json))!;
    }

    // The headset gets its athlete's profile, and the server remembers when it synced.
    [Fact]
    public async Task Headset_downloads_its_profile()
    {
        using var factory = new TestAppFactory();
        var (_, headset) = await AthleteWithHeadsetAsync(factory);

        var response = await headset.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = (await response.Content.ReadFromJsonAsync<AthleteProfile>(Json))!;
        Assert.Equal("Athlete One", profile.Name);
        Assert.Equal(1, profile.Version);

        // In the database, the device's LastSyncAt is now set.
        using var scope = factory.Services.CreateScope();
        var device = await factory.GetDb(scope).Devices.AsNoTracking().SingleAsync();
        Assert.NotNull(device.LastSyncAt);
    }

    // A newer copy from the headset is saved, and the phone sees it.
    [Fact]
    public async Task Higher_version_from_headset_is_saved()
    {
        using var factory = new TestAppFactory();
        var (athlete, headset) = await AthleteWithHeadsetAsync(factory);

        // The headset changes one thing and raises its Version by 1.
        var copy = await DownloadAsync(headset);
        copy.Notes = "Changed on the headset";
        copy.Version = 2;

        var response = await headset.PutAsJsonAsync("/api/profile", copy, Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The phone now shows the headset's change and Version 2.
        var onPhone = (await athlete.GetFromJsonAsync<AthleteProfile>("/api/my-profile", Json))!;
        Assert.Equal("Changed on the headset", onPhone.Notes);
        Assert.Equal(2, onPhone.Version);
    }

    // A copy that isn't newer than the server's is refused, and the headset gets the server's copy back.
    [Fact]
    public async Task Tie_or_lower_version_is_rejected_with_the_servers_copy()
    {
        using var factory = new TestAppFactory();
        var (athlete, headset) = await AthleteWithHeadsetAsync(factory);
        var copy = await DownloadAsync(headset);

        // The phone saves first, so the server is now at Version 2.
        await SaveProfileAsync(athlete, "Edited on the phone");

        // The headset's own edit, sent first as a tie (Version 2) and then as a lower number (Version 1).
        copy.Name = "Edited on the headset";
        copy.Version = 2;
        var tie = await headset.PutAsJsonAsync("/api/profile", copy, Json);
        copy.Version = 1;
        var lower = await headset.PutAsJsonAsync("/api/profile", copy, Json);

        // Both are refused, and both bodies are the server's copy: the phone's edit, Version 2.
        foreach (var response in new[] { tie, lower })
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var serverCopy = (await response.Content.ReadFromJsonAsync<AthleteProfile>(Json))!;
            Assert.Equal("Edited on the phone", serverCopy.Name);
            Assert.Equal(2, serverCopy.Version);
        }
    }

    // The headset's copy goes through the same value checks as the phone's.
    [Fact]
    public async Task Invalid_profile_from_headset_is_rejected()
    {
        using var factory = new TestAppFactory();
        var (_, headset) = await AthleteWithHeadsetAsync(factory);

        // Exactly one bad value: Brightness must be between 0 and 1.
        var copy = await DownloadAsync(headset);
        copy.Brightness = 5;
        copy.Version = 2;

        var response = await headset.PutAsJsonAsync("/api/profile", copy, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Nothing was saved: the server is still at Version 1.
        var current = await DownloadAsync(headset);
        Assert.Equal(1, current.Version);
    }

    // An athlete's login cookie does not count as a headset login.
    [Fact]
    public async Task Athlete_cookie_cannot_use_profile_sync()
    {
        using var factory = new TestAppFactory();
        var (athlete, _) = await AthleteWithHeadsetAsync(factory);

        var response = await athlete.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
