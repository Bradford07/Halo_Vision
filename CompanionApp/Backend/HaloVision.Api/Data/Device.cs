namespace HaloVision.Api.Data;

// A paired HoloLens 2 headset (P3's paired_headset table).
// A Device row is created when a headset claims a pairing code (step 3).
public class Device
{
    // Primary key: the database gives each new headset the next number.
    public int Id { get; set; }

    // The athlete profile this headset keeps and syncs.
    public int ProfileID { get; set; }

    // A friendly name shown to the athlete, e.g. "HoloLens 2".
    public string Name { get; set; } = "";

    // SHA-256 hash of the headset's secret token, as hex text.
    // We never store the token itself, so a leaked database can't be used to pretend to be a headset.
    public string TokenHash { get; set; } = "";

    // When the headset was paired, in UTC.
    public DateTime PairedAt { get; set; }

    // When the headset last synced its profile, in UTC. Null until the first sync.
    public DateTime? LastSyncAt { get; set; }
}
