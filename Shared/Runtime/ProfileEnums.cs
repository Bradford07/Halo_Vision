namespace HaloVision.Shared
{
    //Which part of the athlete's vision is affected.
        //Middle: Loss in the middle of view; edges still work
        //Peripheral: Loss in the edges of view; middle still works
        //Both: Loss in both middle and edges of view
    public enum VisionLossType 
    { 
        Central, Peripheral, Both
    }

    //Severity of the vision loss.
        //Mild: Slight loss of vision; can still see most things
        //Moderate: Significant loss of vision; can see some things but not all
        //Severe: Almost complete loss of vision; can barely see anything
    public enum Severity
    {
        Mild, Moderate, Severe
    }

    //Which eye is affected by the vision loss.
        //Left: Loss in the left eye
        //Right: Loss in the right eye
        //Both: Loss in both eyes
    public enum AffectedEye
    {
        Left, Right, Both
    }

    //Skill level of the athlete.
        //Beginner: New to the sport; still learning the basics
        //Intermediate: Has some experience in the sport; can perform basic skills
        //Advanced: Experienced in the sport; can perform advanced skills
    public enum SkillLevel
    {
        Beginner, Intermediate, Advanced
    }

    //Selecting Segmentation Model, this is a Level 2 requirement
    //Type of surface the athlete is playing on.
        //Paved: Hard surface; typically used for sports like basketball or tennis
        //Dirt: Soft surface; typically used for sports like baseball or soccer
        //Snow: Cold surface; typically used for sports like skiing or snowboarding
    public enum SurfaceMode
    {
        Paved, Dirt, Snow
    }

    //Selecting Cue Mode, this is a Level 3 requirement
    //Cue mode type
        //Overlay: Visual cues are displayed on the screen
        //Audio: Audio cues are played through the speakers
        //Both: Both visual and audio cues are used
    public enum CueMode
    {
        Overlay, Audio, Both
    }


    //Palettes that avoid red versus green
    //Standard: Standard color palette; no color blindness considerations
        //Protanopia: Color palette for those with protanopia; avoids red colors
        //Deuteranopia: Color palette for those with deuteranopia; avoids green colors
        //Tritanopia: Color palette for those with tritanopia; avoids blue colors
    public enum ColorPallette
    {
        Standard, Protanopia, Deuteranopia, Tritanopia
    }

}