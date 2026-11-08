// Written with Claude Code, reviewed by Owen Cespon
namespace HaloVision.Api;

// Names of our authorization policies, so endpoints and Program.cs can't disagree on a spelling.
public static class AuthPolicies
{
    // A guide signed in with the Identity login cookie (the phone web app).
    public const string Guide = "Guide";

    // A paired headset logged in with its device token ("Authorization: Bearer <token>").
    public const string Device = "Device";
}
