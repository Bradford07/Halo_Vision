namespace HaloVision.Api;

// Names of our authorization policies, so endpoints and Program.cs can't disagree on a spelling.
public static class AuthPolicies
{
    // An athlete signed in with the Identity login cookie (the phone web app).
    public const string Athlete = "Athlete";

    // A paired headset logged in with its device token ("Authorization: Bearer <token>").
    public const string Device = "Device";
}
