using System;

namespace Halo_Vision.Shared.Runtime
{
     // The guide edits it on the web app; the headset keeps a copy for offline runs.
    public class AthleteProfile
    {
        // Unique identifier for the athlete profile.
        public string ProfileId { get; set; }

        // Name of the athlete.
        public string Name { get; set; } = "";

        //Vision loss type, severity, and affected eye are all Level 1 requirements
        public VisionLossType VisionLossType { get; set; } = VisionLossType.Peripheral;

        // Severity of the vision loss, defaulting to Mild.
        public Severity Severity { get; set; } = Severity.Mild;

        // Which eye is affected by the vision loss, defaulting to Both.
        public AffectedEye AffectedEye { get; set; } = AffectedEye.Both;

        //Height of the athlete in meters, defaulting to 1.6 meters.
        //The headset uses this to determine the distance of the path edges. 
        public float Height { get; set; } = 1.6f;

        //Skill level of the athlete, defaulting to Beginner.
        public SkillLevel SkillLevel { get; set; } = SkillLevel.Beginner;

        //The ground type, and picks the segmetation model, defaulting to Paved. This is a Level 2 requirement.
        public SurfaceMode SurfaceMode { get; set; } = SurfaceMode.Paved;

        //Cue mode type, defaulting to Overlay. This is a Level 3 requirement.
        public CueMode CueMode { get; set; } = CueMode.Overlay;

        //Color of the overlay, defaulting to a shade of blue. 
        public string OverlayColor{ get; set; } = "#2F7CF6";

        //Color palette for the overlay, defaulting to Standard.
        public ColorPalette ColorPalette { get; set; } = ColorPalette.Standard;

        //Brightness of the overlay, defaulting to 1.0 (normal brightness).
        public float Brightness { get; set; } = 1.0f;

        //Contrast of the overlay, defaulting to 1.0 (normal contrast).
        public float Contrast { get; set; } = 1.0f;

        //Whether audio cues are enabled, defaulting to true (enabled).
        public bool AudioOn { get; set; } = true;

        //Volume of the audio cues, defaulting to 1.0 (maximum volume).
        public float AudioVolume { get; set; } = 1.0f;

        //Notes about the athletes capabiliteis.
        public string Notes { get; set; } = "";
    
        //Goes up by 1 every time the profile is saved.
        //When syncing, the copy with the higher version wins.
        public int Version { get; set; } = 1;

        //When the profile was last updated, in UTC. 
        public DateTime UpdatedAt { get; set; }
    }
}