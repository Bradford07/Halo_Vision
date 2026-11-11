using HaloVision.Api.Features.Athletes;
using HaloVision.Shared;

namespace HaloVision.Api.Tests;

public class AthleteProfileRulesTests
{
    [Fact]
    public void Valid_profile_has_no_validation_errors()
    {
        var errors = AthleteProfileRules.Validate(new AthleteProfile { Name = "Avery" });

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Name_is_required(string? name)
    {
        var profile = new AthleteProfile { Name = name! };

        var errors = AthleteProfileRules.Validate(profile);

        Assert.Equal("Name is required.", errors["name"][0]);
    }

    [Theory]
    [InlineData(0.49f, false)]
    [InlineData(0.5f, true)]
    [InlineData(2.5f, true)]
    [InlineData(2.51f, false)]
    public void Height_must_be_between_point_five_and_two_point_five_meters(float height, bool isValid)
    {
        var profile = new AthleteProfile { Name = "Avery", Height = height };

        var errors = AthleteProfileRules.Validate(profile);

        Assert.Equal(isValid, !errors.ContainsKey("height"));
    }

    [Theory]
    [InlineData("#2F7CF6", true)]
    [InlineData("#abcdef", true)]
    [InlineData("#12345", false)]
    [InlineData("#1234567", false)]
    [InlineData("123456", false)]
    [InlineData("#GGGGGG", false)]
    public void Overlay_color_must_be_six_hex_digits(string color, bool isValid)
    {
        var profile = new AthleteProfile { Name = "Avery", OverlayColor = color };

        var errors = AthleteProfileRules.Validate(profile);

        Assert.Equal(isValid, !errors.ContainsKey("overlayColor"));
    }

    [Theory]
    [InlineData("brightness", 0f, true)]
    [InlineData("brightness", 1f, true)]
    [InlineData("brightness", -0.01f, false)]
    [InlineData("brightness", 1.01f, false)]
    [InlineData("contrast", 0f, true)]
    [InlineData("contrast", 1f, true)]
    [InlineData("contrast", -0.01f, false)]
    [InlineData("contrast", 1.01f, false)]
    [InlineData("audioVolume", 0f, true)]
    [InlineData("audioVolume", 1f, true)]
    [InlineData("audioVolume", -0.01f, false)]
    [InlineData("audioVolume", 1.01f, false)]
    public void Brightness_contrast_and_volume_must_be_between_zero_and_one(
        string field, float value, bool isValid)
    {
        var profile = new AthleteProfile { Name = "Avery" };
        SetNumericField(profile, field, value);

        var errors = AthleteProfileRules.Validate(profile);

        Assert.Equal(isValid, !errors.ContainsKey(field));
    }

    [Fact]
    public void Copy_editable_fields_copies_settings_but_preserves_server_fields()
    {
        var updatedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var source = new AthleteProfile
        {
            Name = "Avery",
            VisionLossType = VisionLossType.Central,
            Severity = Severity.Severe,
            AffectedEye = AffectedEye.Left,
            Height = 1.9f,
            SkillLevel = SkillLevel.Advanced,
            SurfaceMode = SurfaceMode.Snow,
            CueMode = CueMode.Both,
            OverlayColor = "#abcdef",
            ColorPalette = ColorPalette.Tritanopia,
            Brightness = 0.5f,
            Contrast = 0.6f,
            AudioOn = false,
            AudioVolume = 0.7f,
            Notes = "Use extra contrast."
        };
        var target = new AthleteProfile
        {
            ProfileID = 42,
            Version = 8,
            UpdatedAt = updatedAt
        };

        AthleteProfileRules.CopyEditableFields(source, target);

        Assert.Equal(source.Name, target.Name);
        Assert.Equal(source.VisionLossType, target.VisionLossType);
        Assert.Equal(source.Severity, target.Severity);
        Assert.Equal(source.AffectedEye, target.AffectedEye);
        Assert.Equal(source.Height, target.Height);
        Assert.Equal(source.SkillLevel, target.SkillLevel);
        Assert.Equal(source.SurfaceMode, target.SurfaceMode);
        Assert.Equal(source.CueMode, target.CueMode);
        Assert.Equal(source.OverlayColor, target.OverlayColor);
        Assert.Equal(source.ColorPalette, target.ColorPalette);
        Assert.Equal(source.Brightness, target.Brightness);
        Assert.Equal(source.Contrast, target.Contrast);
        Assert.Equal(source.AudioOn, target.AudioOn);
        Assert.Equal(source.AudioVolume, target.AudioVolume);
        Assert.Equal(source.Notes, target.Notes);

        Assert.Equal(42, target.ProfileID);
        Assert.Equal(8, target.Version);
        Assert.Equal(updatedAt, target.UpdatedAt);
    }

    private static void SetNumericField(AthleteProfile profile, string field, float value)
    {
        switch (field)
        {
            case "brightness":
                profile.Brightness = value;
                break;
            case "contrast":
                profile.Contrast = value;
                break;
            case "audioVolume":
                profile.AudioVolume = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(field), field, "Unknown profile field.");
        }
    }
}
