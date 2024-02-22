using System;

namespace United.Mobile.Model.Fitbit
{
    [Serializable]
    public class FlightStatusResponse : MOBResponse
    {
        public FlightStatusSegment FlightStatusSegment { get; set; }
    }
}
