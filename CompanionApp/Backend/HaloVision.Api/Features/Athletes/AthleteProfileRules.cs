using System.Text.RegularExpressions;
using HaloVision.Shared;

namespace HaloVision.Api.Features.Athletes;

// Server-side rules for athlete profiles, shared by my-profile endpoints (phone)
// and the headset sync endpoint (step 4) so both follow exactly the same rules.
public static partial class AthleteProfileRules
{
    // Copies every field a person can edit from "source" onto "target".
    // ProfileID, Version, UpdatedAt and the account link are NOT copied: the server controls those.
    public static void CopyEditableFields(AthleteProfile source, AthleteProfile target)
    {
        target.Name = source.Name;
        target.VisionLossType = source.VisionLossType;
        target.Severity = source.Severity;
        target.AffectedEye = source.AffectedEye;
        target.Height = source.Height;
        target.SkillLevel = source.SkillLevel;
        target.SurfaceMode = source.SurfaceMode;
        target.CueMode = source.CueMode;
        target.OverlayColor = source.OverlayColor;
        target.ColorPalette = source.ColorPalette;
        target.Brightness = source.Brightness;
        target.Contrast = source.Contrast;
        target.AudioOn = source.AudioOn;
        target.AudioVolume = source.AudioVolume;
        target.Notes = source.Notes;
    }

    // Checks the values make sense. Returns a list of problems by field name (empty = all good).
    // The web app should catch these too, but the server never trusts what it's sent.
    public static Dictionary<string, string[]> Validate(AthleteProfile profile)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(profile.Name))
            errors["name"] = ["Name is required."];

        if (profile.Height < 0.5f || profile.Height > 2.5f)
            errors["height"] = ["Height must be between 0.5 and 2.5 meters."];

        if (!HexColor().IsMatch(profile.OverlayColor ?? ""))
            errors["overlayColor"] = ["Overlay color must be a hex color like #2F7CF6."];

        CheckZeroToOne(profile.Brightness, "brightness", errors);
        CheckZeroToOne(profile.Contrast, "contrast", errors);
        CheckZeroToOne(profile.AudioVolume, "audioVolume", errors);

        return errors;
    }

    private static void CheckZeroToOne(float value, string field, Dictionary<string, string[]> errors)
    {
        if (value < 0f || value > 1f)
            errors[field] = [$"{field} must be between 0 and 1."];
    }

    // "#" followed by exactly 6 hex digits, e.g. #2F7CF6.
    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex HexColor();
}
