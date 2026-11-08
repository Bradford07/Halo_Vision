using System.Security.Claims;
using HaloVision.Api.Auth;
using HaloVision.Api.Data;
using HaloVision.Shared;
using Microsoft.EntityFrameworkCore;


namespace HaloVision.Api.Features.TestLogs;



//Field-test logs uploaded by a paired headset

//POST /api/test-logs body: a list of logs 

//Uploading the same batch again is safe: logs the server already has are skipped


public static class TestLogEndpoints
{
    //What the headset gets back: every id the server now ahs, whether it was saved
    //Just now or aon an erlier try, the headset can delete these from its upload queue

    public record UploadResponse(List<Guid>Accepted);

    public static void MapTestLogEndpoints(this IEndpointRouteBuilder app)
    {
        //Only a paired headset, logged in with its token can upload logs
        app.MapPost("/api/test-logs", Upload).RequireAuthorization(AuthPolicies.Device);
    }

         // POST /api/test-logs
    private static async Task<IResult> Upload(List<TestLog> logs, ClaimsPrincipal user, AppDbContext db)
    {
            var deviceId = user.GetDeviceId();

            // Ask the database once: which of these ids do we already have?
            var ids = logs.Select(l => l.Id).ToList();
            var alreadyStored = await db.TestLogs.Where(l => ids.Contains(l.Id)).Select(l => l.Id).ToListAsync();

            var accepted = new List<Guid>();
            foreach (var log in logs)
            {
                // A log without an id can't be recognized on a retry, so it isn't accepted.
                // A log listed twice in this same batch is only handled once.
                if (log.Id == Guid.Empty || accepted.Contains(log.Id))
                {
                    continue;
                }

                // Save it only if we don't have it yet. Either way, the server now has it.
                if (!alreadyStored.Contains(log.Id))
                {
                    // trust the token, not what the headset sent
                    log.DeviceId = deviceId;    
                    db.TestLogs.Add(log);
                }
                accepted.Add(log.Id);
            }

            // One save for the whole batch.
            await db.SaveChangesAsync();

            return Results.Ok(new UploadResponse(accepted));
    }
}   
