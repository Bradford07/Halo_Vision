using System.Collections.Concurrent;
using System.Security.Claims;
using HaloVision.Api.Auth;
using HaloVision.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HaloVision.Api.Features.Guidance;

public static class GuidanceEndpoints
{
    // Temporary guidance state stored by athlete profile ID.
    // Defaults to false (standby) when no state exists.
    private static readonly ConcurrentDictionary<int, bool>
        GuidanceStates = new();

    public record GuidanceRequest(bool Active);
    public record GuidanceResponse(bool Active);

    public static void MapGuidanceEndpoints(this IEndpointRouteBuilder app)
    {
        // The signed-in athlete controls their guidance state.
        app.MapPut("/api/guidance",
            async (GuidanceRequest request,
                   ClaimsPrincipal user,
                   AppDbContext db) =>
            {
                string userId = user.GetUserId();

                var profile = await db.AthleteProfiles
                    .AsNoTracking()
                    .SingleOrDefaultAsync(p =>
                        EF.Property<string>(
                            p, AppDbContext.UserIdColumn) == userId);

                if (profile is null)
                    return Results.NotFound();

                GuidanceStates[profile.ProfileID] = request.Active;

                return Results.Ok(
                    new GuidanceResponse(request.Active));
            })
            .RequireAuthorization(AuthPolicies.Athlete);

        // A paired headset reads its athlete's guidance state.
        app.MapGet("/api/devices/guidance",
            async (ClaimsPrincipal user, AppDbContext db) =>
            {
                int deviceId = user.GetDeviceId();

                var device = await db.Devices
                    .AsNoTracking()
                    .SingleAsync(d => d.Id == deviceId);

                bool active = GuidanceStates.GetValueOrDefault(
                    device.ProfileID, false);

                return Results.Ok(new GuidanceResponse(active));
            })
            .RequireAuthorization(AuthPolicies.Device);
    }
}