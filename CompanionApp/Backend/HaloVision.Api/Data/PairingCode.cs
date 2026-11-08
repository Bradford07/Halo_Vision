namespace HaloVision.Api.Data;

// A one-time 6-character code an athlete creates so their headset can pair with their profile.
// Replaces P3's Bluetooth bonding.
public class PairingCode
{
    // Primary key.
    public int Id { get; set; }

    // The code shown on the web app (as a QR code) and sent back by the headset, e.g. "K7Q2MX".
    public string Code { get; set; } = "";

    // The athlete profile the headset will be linked to.
    public int ProfileID { get; set; }

    // The code stops working after this time (10 minutes after it was made), in UTC.
    public DateTime ExpiresAt { get; set; }

    // When a headset used the code, in UTC. Null means it hasn't been used yet; codes work once.
    public DateTime? UsedAt { get; set; }
}
