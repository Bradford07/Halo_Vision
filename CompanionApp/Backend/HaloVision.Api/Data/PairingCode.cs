// Written with Claude Code, reviewed by Owen Cespon
namespace HaloVision.Api.Data;

// A one-time 6-character code a guide creates so a headset can pair with one athlete profile.
// Replaces P3's Bluetooth bonding. Used in step 3.
public class PairingCode
{
    // Primary key.
    public int Id { get; set; }

    // The code the guide reads out and the headset sends back, e.g. "K7Q2MX".
    public string Code { get; set; } = "";

    // The athlete profile the headset will be linked to.
    public int ProfileID { get; set; }

    // The guide who created the code (an Identity user id).
    public string GuideId { get; set; } = "";

    // The code stops working after this time (10 minutes after it was made), in UTC.
    public DateTime ExpiresAt { get; set; }

    // When a headset used the code, in UTC. Null means it hasn't been used yet; codes work once.
    public DateTime? UsedAt { get; set; }
}
