using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HaloVision.Api.Features.Devices;
using static HaloVision.Api.Tests.TestAppFactory;

namespace HaloVision.Api.Tests;

// Tests for the "Device" login scheme headsets use (Authorization: Bearer <token>).
public class DeviceAuthTests
{
    // A paired headset's token works, and the server knows which athlete it belongs to.
    [Fact]
    public async Task Paired_headset_token_works()
    {
        using var factory = new TestAppFactory();
        var athlete = await factory.CreateAthleteClientAsync("athlete-a@example.com");
        var profile = await SaveProfileAsync(athlete, "Athlete One");
        var headset = await factory.PairHeadsetAsync(athlete);

        var response = await headset.GetAsync("/api/devices/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = (await response.Content.ReadFromJsonAsync<DeviceEndpoints.DeviceInfo>(Json))!;
        Assert.Equal(profile.ProfileID, me.ProfileId);
        Assert.Equal("HoloLens 2", me.Name);
    }

    //A missing token gets 401.
    [Fact]
    public async Task Missing_token_gets_401()
    {
        using var factory = new TestAppFactory();
        var noToken = factory.CreateClient();

        var response = await noToken.GetAsync("/api/devices/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    //A wrong token gets 401.
    [Fact]
    public async Task Wrong_token_gets_401()
    {
        using var factory = new TestAppFactory();
        var wrongToken = factory.CreateClient();
        wrongToken.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this-is-not-a-real-token");

        var response = await wrongToken.GetAsync("/api/devices/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // An athlete's login cookie does not count as a headset login.
    [Fact]
    public async Task Athlete_cookie_is_not_a_headset_login()
    {
        using var factory = new TestAppFactory();
        var athlete = await factory.CreateAthleteClientAsync("athlete-a@example.com");

        var response = await athlete.GetAsync("/api/devices/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // A headset token does not count as a athlete login.
    [Fact]
    public async Task Headset_token_is_not_an_athlete_login()
    {
        using var factory = new TestAppFactory();
        var athlete = await factory.CreateAthleteClientAsync("athlete-a@example.com");
        var profile = await SaveProfileAsync(athlete, "Athlete One");
        var headset = await factory.PairHeadsetAsync(athlete);

        var response = await headset.GetAsync("/api/my-profile");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
