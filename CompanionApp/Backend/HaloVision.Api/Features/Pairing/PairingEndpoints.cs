// Written with Claude Code, reviewed by Owen Cespon
using System.Security.Claims;
using System.Security.Cryptography;
using HaloVision.Api.Auth;
using HaloVision.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace HaloVision.Api.Features.Pairing;

// Pairing a headset with an athlete profile (replaces P3's Bluetooth pair()).
//
//   1. The guide asks for a code for one of their athletes:  POST /api/pairing/codes
//      The web app shows the code (as a QR code plus text).
//   2. The headset scans it and claims it:                    POST /api/pairing/claim
//      The server creates a Device and returns its secret token ONCE.
//
// A code expires 10 minutes after it's made and works only once.
public static class PairingEndpoints
{
    // How long a code stays valid.
    public static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(10);

    // Letters and digits that are hard to confuse: no 0/O or 1/I. 32 characters, so a
    // 6-character code has 32^6 = about 1 billion possibilities.
    public const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    public const int CodeLength = 6;

    // What the guide's web app sends and gets back.
    public record CreateCodeRequest(int ProfileId);
    public record CreateCodeResponse(string Code, DateTime ExpiresAt);

    // What the headset sends and gets back.
    public record ClaimRequest(string Code, string? DeviceName);
    public record ClaimResponse(string Token, int DeviceId, int ProfileId);

    public static void MapPairingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pairing");

        // Only a signed-in guide can make codes.
        group.MapPost("/codes", CreateCode).RequireAuthorization(AuthPolicies.Guide);

        // The headset isn't logged in yet: having a valid code is its proof.
        group.MapPost("/claim", Claim).AllowAnonymous();
    }

    // POST /api/pairing/codes  { "profileId": 1 }
    private static async Task<IResult> CreateCode(CreateCodeRequest request, ClaimsPrincipal user, AppDbContext db)
    {
        // The guide may only pair headsets to their own athletes. Someone else's athlete
        // answers 404, exactly as if it didn't exist (same rule as /api/athletes).
        var guideId = user.GetGuideId();
        var ownsAthlete = await db.AthleteProfiles.AnyAsync(p =>
            p.ProfileID == request.ProfileId &&
            EF.Property<string>(p, AppDbContext.GuideIdColumn) == guideId);
        if (!ownsAthlete)
            return Results.NotFound();

        var pairingCode = new PairingCode
        {
            ProfileID = request.ProfileId,
            GuideId = guideId,
            ExpiresAt = DateTime.UtcNow.Add(CodeLifetime),
        };
        db.PairingCodes.Add(pairingCode);

        // Codes are random, so two could (very rarely) come out the same. The database refuses
        // duplicates (unique index), so if that happens we just try a new random code.
        for (var attempt = 1; ; attempt++)
        {
            pairingCode.Code = RandomNumberGenerator.GetString(CodeAlphabet, CodeLength);
            try
            {
                await db.SaveChangesAsync();
                break;
            }
            catch (DbUpdateException) when (attempt < 5)
            {
                // Duplicate code: loop and try another one.
            }
        }

        return Results.Ok(new CreateCodeResponse(pairingCode.Code, pairingCode.ExpiresAt));
    }

    // POST /api/pairing/claim  { "code": "K7Q2MX", "deviceName": "HoloLens 2" }
    private static async Task<IResult> Claim(ClaimRequest request, AppDbContext db)
    {
        // Accept "k7q2mx " the same as "K7Q2MX" in case someone types it.
        var code = (request.Code ?? "").Trim().ToUpperInvariant();
        var now = DateTime.UtcNow;

        // Everything below happens together or not at all: if creating the device fails,
        // the code is NOT used up.
        await using var transaction = await db.Database.BeginTransactionAsync();

        // Use the code in ONE database step: "mark it used, but only if it exists, hasn't been
        // used, and hasn't expired." If two headsets claim the same code at the same moment,
        // only one UPDATE can match, so only one of them wins.
        var claimed = await db.PairingCodes
            .Where(c => c.Code == code && c.UsedAt == null && c.ExpiresAt > now)
            .ExecuteUpdateAsync(setters => setters.SetProperty(c => c.UsedAt, now));

        if (claimed == 0)
        {
            // Don't say WHICH check failed, so nobody can learn which codes exist.
            return Results.BadRequest(new { error = "The code is invalid, expired, or already used." });
        }

        var profileId = await db.PairingCodes
            .Where(c => c.Code == code)
            .Select(c => c.ProfileID)
            .SingleAsync();

        // Make the headset's secret token. Only its hash goes in the database.
        var token = DeviceTokens.NewToken();
        var device = new Device
        {
            ProfileID = profileId,
            Name = string.IsNullOrWhiteSpace(request.DeviceName) ? "HoloLens 2" : request.DeviceName.Trim(),
            TokenHash = DeviceTokens.Hash(token),
            PairedAt = now,
        };
        db.Devices.Add(device);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();

        // The only time the token is ever sent. The headset must save it.
        return Results.Ok(new ClaimResponse(token, device.Id, profileId));
    }
}
