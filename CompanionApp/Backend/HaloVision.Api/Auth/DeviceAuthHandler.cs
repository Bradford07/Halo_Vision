// Written with Claude Code, reviewed by Owen Cespon
using System.Security.Claims;
using System.Text.Encodings.Web;
using HaloVision.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HaloVision.Api.Auth;

// The "Device" login scheme for headsets.
// A headset sends "Authorization: Bearer <token>" on every request. We hash the token and
// look for a paired Device with that hash. If one exists, the request is logged in as that
// headset, and endpoints can read which device and which athlete profile it belongs to.
public class DeviceAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    // The scheme's name, used when registering it and in the "Device" policy.
    public const string SchemeName = "Device";

    // Names of the facts ("claims") we attach to a logged-in headset.
    public const string DeviceIdClaim = "device_id";
    public const string ProfileIdClaim = "profile_id";

    private readonly AppDbContext _db;

    public DeviceAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        AppDbContext db)
        : base(options, logger, encoder)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // No "Authorization: Bearer ..." header: this scheme has nothing to say.
        // The endpoint then answers 401 because nobody is logged in.
        string? header = Request.Headers.Authorization;
        const string prefix = "Bearer ";
        if (header is null || !header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = header[prefix.Length..].Trim();
        if (token.Length == 0)
        {
            return AuthenticateResult.Fail("Empty token.");
        }

        // Look up the device by the token's hash (we never stored the token itself).
        var hash = DeviceTokens.Hash(token);
        var device = await _db.Devices.AsNoTracking().SingleOrDefaultAsync(d => d.TokenHash == hash);
        if (device is null)
        {
            return AuthenticateResult.Fail("Unknown or revoked token.");
        }

        // Logged in: remember which device this is and which athlete profile it syncs.
        var claims = new[]
        {
            new Claim(DeviceIdClaim, device.Id.ToString()),
            new Claim(ProfileIdClaim, device.ProfileID.ToString()),
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return AuthenticateResult.Success(ticket);
    }
}
