using System.Net;
using System.Net.Http.Json;
using HaloVision.Api.Features.Devices;
using HaloVision.Api.Features.TestLogs;
using HaloVision.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HaloVision.Api.Tests.TestAppFactory;

namespace HaloVision.Api.Tests;

// Tests for uploading field-test logs from a headset: POST /api/test-logs.
public class TestLogTests
{
    // Helper: a signed-in athlete with a saved profile, plus a headset paired to it.
    private static async Task<(HttpClient Athlete, HttpClient Headset)> AthleteWithHeadsetAsync(TestAppFactory factory)
    {
        var athlete = await factory.CreateAthleteClientAsync("athlete-a@example.com");
        await SaveProfileAsync(athlete, "Athlete One");
        var headset = await factory.PairHeadsetAsync(athlete);
        return (athlete, headset);
    }

    // Helper: one field-test run, the way the headset would record it.
    private static TestLog NewLog(Guid id)
    {
        return new TestLog
        {
            Id = id,
            StartedAt = DateTime.UtcNow,
            Location = "Practice trail A",
            ParticipantCode = "P03",
        };
    }

    // Helper: uploads a batch and returns the status code plus the ids the server accepted.
    private static async Task<(HttpStatusCode Status, List<Guid> Accepted)> UploadAsync(HttpClient headset, params TestLog[] logs)
    {
        var response = await headset.PostAsJsonAsync("/api/test-logs", logs, Json);
        var body = await response.Content.ReadFromJsonAsync<TestLogEndpoints.UploadResponse>(Json);
        return (response.StatusCode, body!.Accepted);
    }

    // (P4 required) Sending the same batch twice is safe: each log is stored only once.
    [Fact]
    public async Task Same_batch_twice_stores_each_log_once()
    {
        using var factory = new TestAppFactory();
        var (_, headset) = await AthleteWithHeadsetAsync(factory);
        var first = NewLog(Guid.NewGuid());
        var second = NewLog(Guid.NewGuid());

        var firstTry = await UploadAsync(headset, first, second);
        var retry = await UploadAsync(headset, first, second);

        // Both answers are 200 and list both ids, whether they were saved now or on the earlier try.
        foreach (var (status, accepted) in new[] { firstTry, retry })
        {
            Assert.Equal(HttpStatusCode.OK, status);
            Assert.Equal(2, accepted.Count);
            Assert.Contains(first.Id, accepted);
            Assert.Contains(second.Id, accepted);
        }

        // The database has exactly 2 rows.
        using var scope = factory.Services.CreateScope();
        Assert.Equal(2, await factory.GetDb(scope).TestLogs.CountAsync());
    }

    // The server trusts the headset's token, not the DeviceId in the body.
    [Fact]
    public async Task Device_id_comes_from_the_token()
    {
        using var factory = new TestAppFactory();
        var (_, headset) = await AthleteWithHeadsetAsync(factory);
        var me = (await headset.GetFromJsonAsync<DeviceEndpoints.DeviceInfo>("/api/devices/me", Json))!;
        var log = NewLog(Guid.NewGuid());
        log.DeviceId = 999;

        var (status, _) = await UploadAsync(headset, log);

        Assert.Equal(HttpStatusCode.OK, status);

        // The stored row has the real headset's device id.
        using var scope = factory.Services.CreateScope();
        var stored = await factory.GetDb(scope).TestLogs.AsNoTracking().SingleAsync(l => l.Id == log.Id);
        Assert.NotEqual(999, me.DeviceId);
        Assert.Equal(me.DeviceId, stored.DeviceId);
    }

    // A log without an id can't be recognized on a retry, so the server doesn't take it.
    [Fact]
    public async Task Log_without_an_id_is_not_accepted()
    {
        using var factory = new TestAppFactory();
        var (_, headset) = await AthleteWithHeadsetAsync(factory);

        var (status, accepted) = await UploadAsync(headset, NewLog(Guid.Empty));

        // It's not in "accepted", and nothing is stored.
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Empty(accepted);
        using var scope = factory.Services.CreateScope();
        Assert.Equal(0, await factory.GetDb(scope).TestLogs.CountAsync());
    }

    // An athlete's login cookie does not count as a headset login.
    [Fact]
    public async Task Athlete_cookie_cannot_upload_logs()
    {
        using var factory = new TestAppFactory();
        var (athlete, _) = await AthleteWithHeadsetAsync(factory);

        var response = await athlete.PostAsJsonAsync("/api/test-logs", new[] { NewLog(Guid.NewGuid()) }, Json);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
