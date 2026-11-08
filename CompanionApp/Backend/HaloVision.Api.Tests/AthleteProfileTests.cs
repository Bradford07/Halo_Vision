using HaloVision.Shared;

namespace HaloVision.Api.Tests;


//Unit tests for the AthleteProfile class.
public class AthleteProfileTests
{
    // marks a method as a test that xUnit shoudl run
    [Fact]
    public void New_profile_has_expected_defaults()
    {
        //Arrange: create a brand-new profile without setting anything (creates defaults)
        var profile = new AthleteProfile();

        //Assert: check that the defaults are what we expect.

        //Profile id will be 0 for a new profile, since it hasn't been saved yet.
        Assert.Equal(0, profile.ProfileID);

        //Name is empty by default
        Assert.Equal("", profile.Name);

        //Vision loss type is Peripheral by default
        Assert.Equal(VisionLossType.Peripheral, profile.VisionLossType);

        //Severity is Mild by default
        Assert.Equal(Severity.Mild, profile.Severity);

        //Affected eye is Both by default
        Assert.Equal(AffectedEye.Both, profile.AffectedEye);

        //Height is 1.6 meters by default
        Assert.Equal(1.6f, profile.Height);

        //Eye height is 1.488 meters by default (1.6 * 0.93)
        Assert.Equal(1.488f, profile.EyeHeight());

        //Skill level is Beginner by default
        Assert.Equal(SkillLevel.Beginner, profile.SkillLevel);

        //Surface mode is Paved by default
        Assert.Equal(SurfaceMode.Paved, profile.SurfaceMode);

        //Cue mode is Overlay by default
        Assert.Equal(CueMode.Overlay, profile.CueMode);

        //Overlay color is a shade of blue by default
        Assert.Equal("#2F7CF6", profile.OverlayColor);

        //Color palette is Standard by default
        Assert.Equal(ColorPalette.Standard, profile.ColorPalette);

        //Brightness is 1.0 by default
        Assert.Equal(1.0f, profile.Brightness);

        //Contrast is 1.0 by default
        Assert.Equal(1.0f, profile.Contrast);

        //Audio is on by default
        Assert.True(profile.AudioOn);

        //Audio volume is 1.0 by default
        Assert.Equal(1.0f, profile.AudioVolume);

        //Notes is empty by default
        Assert.Equal("", profile.Notes);

        //Version is 1 by default = first saved version
        Assert.Equal(1, profile.Version);

        //Last updated at is default value for DateTime (1/1/0001 12:00:00 AM) by default
        Assert.Equal(default(DateTime), profile.UpdatedAt);
    }

    [Fact]
    public void EyeHeight_is_93_of_body_height_is_correct()
    {
        //Arrange: create a profile with a 1.75 m athlete
        var profile = new AthleteProfile();
        profile.Height = 1.75f; // 1.75 meters

        //Act: calculate the eye height
        float eyeHeight = profile.EyeHeight();

        //Assert: check that the eye height is correct (1.75 * 0.93) = 1.6275 m
        Assert.Equal(1.6275, eyeHeight, 0.001); // Allow a small delta for floating-point comparison
    }
}