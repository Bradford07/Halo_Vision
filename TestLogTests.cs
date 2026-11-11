using System.Net;
using System.Net.Http.Json;
using HaloVision.Api.Features.Devices;
using HaloVision.Api.Features.TestLogs;
using HaloVision.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static HaloVision.Api.Tests.TestAppFactory;

namespace HaloVision.Api.Tests;

// Tests for field-test log upload from a paired headset: POST /api/test-logs.
// Rule: the body is a list of logs. Logs the server already has (same Id) are skipped,
// so sending a batch twice is safe. The answer lists every id the server now has.
public class TestLogTests
{
    // Helper: a signed-in athlete with a saved profile, plus a headset paired to it.
    private static async Task<(HttpClient Athlete, HttpClient Headset)> PairedHeadsetAsync(TestAppFactory factory)
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
            Lux = 20000f,
            AvgFps = 30f,
            LatencyMs = 80f,
        };
    }

    // Helper: uploads a batch and reads back which ids the server accepted.
    private static async Task<(HttpStatusCode Status, List<Guid> Accepted)> UploadAsync(HttpClient headset, params TestLog[] logs)
    {
        var response = await headset.PostAsJsonAsync("/api/test-logs", logs, Json);
        var body = await response.Content.ReadFromJsonAsync<TestLogEndpoints.UploadResponse>(Json);
        return (response.StatusCode, body!.Accepted);
    }

    // (P4 required) Sending the same batch twice is safe: each log is stored once.
    [Fact]
    public async Task Same_batch_twice_stores_each_log_once()
    {
        using var factory = new TestAppFactory();
        var (_, headset) = await PairedHeadsetAsync(factory);
        var first = NewLog(Guid.NewGuid());
        var second = NewLog(Guid.NewGuid());

        var firstTry = await UploadAsync(headset, first, second);
        var retry = await UploadAsync(headset, first, second);

        // Both answers are 200 and list both ids: the server has them either way.
        foreach (var answer in new[] { firstTry, retry })
        {
            Assert.Equal(HttpStatusCode.OK, answer.Status);
            Assert.Equal(2, answer.Accepted.Count);
            Assert.Contains(first.Id, answer.Accepted);
            Assert.Contains(second.Id, answer.Accepted);
        }

        // The retry did not store anything twice.
        using var scope = factory.Services.CreateScope();
        Assert.Equal(2, await factory.GetDb(scope).TestLogs.CountAsync());
    }

    // The stored DeviceId is the headset the token belongs to, never what the body claims.
    [Fact]
    public async Task Device_id_comes_from_the_token()
    {
        using var factory = new TestAppFactory();
        var (_, headset) = await PairedHeadsetAsync(factory);
        var me = (await headset.GetFromJsonAsync<DeviceEndpoints.DeviceInfo>("/api/devices/me", Json))!;

        var log = NewLog(Guid.NewGuid());
        log.DeviceId = 999; // not this headset
        var upload = await UploadAsync(headset, log);

        Assert.Equal(HttpStatusCode.OK, upload.Status);
        Assert.NotEqual(999, me.DeviceId);

        using var scope = factory.Services.CreateScope();
        var stored = await factory.GetDb(scope).TestLogs.SingleAsync(l => l.Id == log.Id);
        Assert.Equal(me.DeviceId, stored.DeviceId);
    }

    // A log without an id can't be recognized on a retry, so it isn't accepted or stored.
    [Fact]
    public async Task Log_without_an_id_is_not_accepted()
    {
        using var factory = new TestAppFactory();
        var (_, headset) = await PairedHeadsetAsync(factory);

        var upload = await UploadAsync(headset, NewLog(Guid.Empty));

        Assert.Equal(HttpStatusCode.OK, upload.Status);
        Assert.Empty(upload.Accepted);

        using var scope = factory.Services.CreateScope();
        Assert.Equal(0, await factory.GetDb(scope).TestLogs.CountAsync());
    }

    // Log upload is for headsets only: an athlete's login cookie does not count.
    [Fact]
    public async Task Athlete_cookie_cannot_upload_logs()
    {
        using var factory = new TestAppFactory();
        var (athlete, _) = await PairedHeadsetAsync(factory);

        var response = await athlete.PostAsJsonAsync("/api/test-logs", new[] { NewLog(Guid.NewGuid()) }, Json);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        Assert.Equal(0, await factory.GetDb(scope).TestLogs.CountAsync());
    }
}
