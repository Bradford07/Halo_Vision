using System.Security.Claims;

namespace HaloVision.Api.Auth;

// Small helpers for reading who is calling an endpoint.
public static class ClaimsPrincipalExtensions
{
    // The signed-in athlete's account id (Identity puts it in the NameIdentifier claim).
    public static string GetUserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Signed-in athlete has no account id.");

    // The logged-in headset's device id (set by DeviceAuthHandler).
    public static int GetDeviceId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(DeviceAuthHandler.DeviceIdClaim)
            ?? throw new InvalidOperationException("Request is not from a paired headset."));

    // The athlete profile the logged-in headset syncs (set by DeviceAuthHandler).
    public static int GetDeviceProfileId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(DeviceAuthHandler.ProfileIdClaim)
            ?? throw new InvalidOperationException("Request is not from a paired headset."));
}
