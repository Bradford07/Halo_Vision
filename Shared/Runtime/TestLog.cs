using System;

namespace HaloVision.Shared
{
    //One field-test run, recoded on the headst and uploaded when it has Wi-Fi
    public class TestLog
    {
        //Made on the haedset with Guid.NewGuid().
        // is retried, so the server can recognize it and skip instead of saving it twice

        public Guid Id {get; set;}

        // The headset taht recorded the run. The server fills this in from the headset's token
        public int DeviceId {get; set;}

        public DateTime StartedAt {get; set;}

        //Where the test happned ex: "Practice trail A"
        public string Location {get; set;} = "";

        //The participant's code, like "P03"
        public string ParticipantCode {get; set;} = "";

        //Light level in lux (bright sun is 100,000; indors is a few hundred)
        public float Lux {get; set;}

        //outside air tempurature, in degrees C
        public float AirTempC {get; set;}

        //The headsets own tempurature, in degrees C, slows down when hot
        public float DeviceTempC {get; set;}

        //Battery percentage at tthe start and end of the run
        public int BatteryStartPct {get; set;}
        public int BatteryEndPct {get; set;}

        //Average frames per second during run
        public float AvgFps {get; set;}

        //Average time from camera frame to cue on screen in milliseconds
        public float LatencyMs {get; set;}

        //How amy times the headset lost track of where it was
        public int TrackingLossCount {get; set;}

        //How well the detected path matched hand checked smaple frame from 0 -> 1
        public float SampledIoU {get; set;}

        //Anything else the tester noticed
        public string Notes {get; set;} = "";
    }
}