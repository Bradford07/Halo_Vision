using System.Net;
using System.Net.Http.Json;
using HaloVision.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HaloVision.Api.Tests.TestAppFactory;

namespace HaloVision.Api.Tests;

// Tests for profile sync from a paired headset: GET /api/profile and PUT /api/profile.
// Sync rule: the server saves the headset's copy only if its Version is higher than the server's.
// A tie or a lower number gets 409, and the response body is the server's copy.
public class ProfileSyncTests
{
    // Helper: a signed-in athlete with a saved profile (Version 1), plus a headset paired to it.
    private static async Task<(HttpClient Athlete, HttpClient Headset, AthleteProfile Profile)> PairedHeadsetAsync(TestAppFactory factory)
    {
        var athlete = await factory.CreateAthleteClientAsync("athlete-a@example.com");
        var profile = await SaveProfileAsync(athlete, "Athlete One");
        var headset = await factory.PairHeadsetAsync(athlete);
        return (athlete, headset, profile);
    }

    // The headset downloads its athlete's profile, and the server remembers when it synced.
    [Fact]
    public async Task Headset_downloads_its_profile()
    {
        using var factory = new TestAppFactory();
        var (_, headset, profile) = await PairedHeadsetAsync(factory);

        var response = await headset.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var downloaded = (await response.Content.ReadFromJsonAsync<AthleteProfile>(Json))!;
        Assert.Equal(profile.ProfileID, downloaded.ProfileID);
        Assert.Equal("Athlete One", downloaded.Name);
        Assert.Equal(1, downloaded.Version);

        // The download counts as a sync: the device's LastSyncAt is now set.
        using var scope = factory.Services.CreateScope();
        var device = await factory.GetDb(scope).Devices.SingleAsync(d => d.ProfileID == profile.ProfileID);
        Assert.NotNull(device.LastSyncAt);
    }

    // A change made on the headset, sent with a higher Version, is saved and the phone sees it.
    [Fact]
    public async Task Higher_version_from_headset_is_saved()
    {
        using var factory = new TestAppFactory();
        var (athlete, headset, _) = await PairedHeadsetAsync(factory);

        // The headset edits its copy and raises the Version by 1 before uploading.
        var copy = (await headset.GetFromJsonAsync<AthleteProfile>("/api/profile", Json))!;
        copy.Brightness = 0.5f;
        copy.Version = 2;
        var response = await headset.PutAsJsonAsync("/api/profile", copy, Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // The phone now sees the headset's change and its Version number.
        var onPhone = (await athlete.GetFromJsonAsync<AthleteProfile>("/api/my-profile", Json))!;
        Assert.Equal(0.5f, onPhone.Brightness);
        Assert.Equal(2, onPhone.Version);
    }

    // If the phone saved first, the headset's copy is out of date: 409, with the server's copy in the body.
    [Fact]
    public async Task Tie_or_lower_version_is_rejected_with_the_servers_copy()
    {
        using var factory = new TestAppFactory();
        var (athlete, headset, profile) = await PairedHeadsetAsync(factory);

        // The headset's copy, taken before the phone's edit (Version 1).
        var copy = (await headset.GetFromJsonAsync<AthleteProfile>("/api/profile", Json))!;

        // The phone saves first: the server is now at Version 2.
        profile.Name = "Edited On Phone";
        var phoneSave = await athlete.PutAsJsonAsync("/api/my-profile", profile, Json);
        Assert.Equal(HttpStatusCode.OK, phoneSave.StatusCode);

        // A tie: the headset also made an edit and raised its Version to 2.
        copy.Name = "Edited On Headset";
        copy.Version = 2;
        var tie = await headset.PutAsJsonAsync("/api/profile", copy, Json);

        // A lower number: the headset sends Version 1.
        copy.Version = 1;
        var lower = await headset.PutAsJsonAsync("/api/profile", copy, Json);

        // Both are rejected, and both answers carry the server's copy (the phone's edit, Version 2).
        foreach (var rejected in new[] { tie, lower })
        {
            Assert.Equal(HttpStatusCode.Conflict, rejected.StatusCode);
            var serverCopy = (await rejected.Content.ReadFromJsonAsync<AthleteProfile>(Json))!;
            Assert.Equal("Edited On Phone", serverCopy.Name);
            Assert.Equal(2, serverCopy.Version);
        }

        // Nothing from the headset was saved.
        var onPhone = (await athlete.GetFromJsonAsync<AthleteProfile>("/api/my-profile", Json))!;
        Assert.Equal("Edited On Phone", onPhone.Name);
        Assert.Equal(2, onPhone.Version);
    }

    // The headset's upload goes through the same value checks as the phone's: a bad value gets 400 and nothing is saved.
    [Fact]
    public async Task Invalid_profile_from_headset_is_rejected()
    {
        using var factory = new TestAppFactory();
        var (_, headset, _) = await PairedHeadsetAsync(factory);

        // Exactly one bad value: Brightness must be between 0 and 1. The Version is fine.
        var copy = (await headset.GetFromJsonAsync<AthleteProfile>("/api/profile", Json))!;
        copy.Brightness = 5f;
        copy.Version = 2;
        var response = await headset.PutAsJsonAsync("/api/profile", copy, Json);
        var after = (await headset.GetFromJsonAsync<AthleteProfile>("/api/profile", Json))!;

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(1, after.Version); // nothing was saved
        Assert.Equal(1.0f, after.Brightness);
    }

    // Profile sync is for headsets only: an athlete's login cookie does not count.
    [Fact]
    public async Task Athlete_cookie_cannot_use_profile_sync()
    {
        using var factory = new TestAppFactory();
        var (athlete, _, _) = await PairedHeadsetAsync(factory);

        var response = await athlete.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
