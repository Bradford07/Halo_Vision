using System.Security.Claims;
using System.Security.Cryptography;
using HaloVision.Api.Auth;
using HaloVision.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HaloVision.Api.Features.Pairing;

// Pairing an athlete's headset with their profile (replaces P3's Bluetooth pair()).
//
//   1. The athlete, signed in on the web app, asks for a code:  POST /api/pairing/codes
//      The web app shows it (as a QR code plus text).
//   2. The headset scans it and claims it:                        POST /api/pairing/claim
//      The server creates a Device and returns its secret token ONCE.
//
// A code lasts 10 minutes and works once.
public static class PairingEndpoints
{
    // Letters and digits that are hard to mix up (no 0/O, no 1/I).
    public const string CodeAlphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    public const int CodeLength = 6;

    // What the web app gets back.
    public record CreateCodeResponse(string Code, DateTime ExpiresAt);

    // What the headset sends and gets back.
    public record ClaimRequest(string Code);
    public record ClaimResponse(string Token, int DeviceId, int ProfileId);

    public static void MapPairingEndpoints(this IEndpointRouteBuilder app)
    {
        // Only a signed-in athlete can make codes.
        app.MapPost("/api/pairing/codes", CreateCode).RequireAuthorization(AuthPolicies.Athlete);

        // The headset isn't logged in yet: a valid code is its proof.
        app.MapPost("/api/pairing/claim", Claim).AllowAnonymous();
    }

    // POST /api/pairing/codes   (no body: always for the signed-in athlete's own profile)
    private static async Task<IResult> CreateCode(ClaimsPrincipal user, AppDbContext db)
    {
        // Find the signed-in athlete's own profile.
        var userId = user.GetUserId();
        var profile = await db.AthleteProfiles
            .SingleOrDefaultAsync(p => EF.Property<string>(p, AppDbContext.UserIdColumn) == userId);
        if (profile is null)
        {
            return Results.NotFound();   // they haven't created their profile yet
        }

        // Make a random code for it, valid for 10 minutes, and save it.
        var pairingCode = new PairingCode
        {
            Code = RandomNumberGenerator.GetString(CodeAlphabet, CodeLength),
            ProfileID = profile.ProfileID,
            ExpiresAt = DateTime.UtcNow.AddMinutes(10),
        };
        db.PairingCodes.Add(pairingCode);
        await db.SaveChangesAsync();

        return Results.Ok(new CreateCodeResponse(pairingCode.Code, pairingCode.ExpiresAt));
    }

    // POST /api/pairing/claim   body: { "code": "K7Q2MX" }
    private static async Task<IResult> Claim(ClaimRequest request, AppDbContext db)
    {
        // Accept "k7q2mx " the same as "K7Q2MX", in case someone types it.
        var code = (request.Code ?? "").Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;

        // The code must exist, be unused, and not be expired.
        var pairingCode = await db.PairingCodes.SingleOrDefaultAsync(c => c.Code == code);
        if (pairingCode is null || pairingCode.UsedAt != null || pairingCode.ExpiresAt <= now)
        {
            // Same message for every failure, so nobody can learn which codes exist.
            return Results.BadRequest(new { error = "The code is invalid, expired, or already used." });
        }

        // Use up the code, and create the headset's Device with a new secret token.
        // Only the token's hash is stored; the token itself is sent back once, below.
        var token = DeviceTokens.NewToken();
        var device = new Device
        {
            ProfileID = pairingCode.ProfileID,
            Name = "HoloLens 2",
            TokenHash = DeviceTokens.Hash(token),
            PairedAt = now,
        };
        pairingCode.UsedAt = now;
        db.Devices.Add(device);

        // SaveChanges writes both changes together: the code is marked used AND the device exists,
        // or neither happens. (Two headsets claiming one code at the same instant can't happen
        // with our single headset, so we don't need anything fancier.)
        await db.SaveChangesAsync();

        return Results.Ok(new ClaimResponse(token, device.Id, device.ProfileID));
    }
}
