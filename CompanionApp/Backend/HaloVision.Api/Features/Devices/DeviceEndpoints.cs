using System.Security.Claims;
using HaloVision.Api.Auth;
using HaloVision.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HaloVision.Api.Features.Devices;

// Endpoints for a paired headset to ask about itself.
public static class DeviceEndpoints
{
    public record DeviceInfo(int DeviceId, int ProfileId, string Name, DateTime PairedAt);

    public static void MapDeviceEndpoints(this IEndpointRouteBuilder app)
    {
        // Only a paired headset, logged in with its token, can call these.
        var group = app.MapGroup("/api/devices").RequireAuthorization(AuthPolicies.Device);

        // GET /api/devices/me: "am I still paired, and to which athlete?"
        // The headset can call this at startup to check its token still works.
        group.MapGet("/me", async (ClaimsPrincipal user, AppDbContext db) =>
        {
            var deviceId = user.GetDeviceId();
            var device = await db.Devices.AsNoTracking().SingleAsync(d => d.Id == deviceId);
            return Results.Ok(new DeviceInfo(device.Id, device.ProfileID, device.Name, device.PairedAt));
        });
    }
}
