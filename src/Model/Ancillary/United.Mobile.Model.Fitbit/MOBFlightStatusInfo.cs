using System;
using System.Collections.Generic;

namespace United.Mobile.Model.Fitbit
{
    [Serializable]
    public class MOBFlightStatusInfo
    {
        public string CarrierCode { get; set; } = string.Empty;

        public string FlightNumber { get; set; } = string.Empty;

        public string CodeShareflightNumber { get; set; } = string.Empty;

        public string FlightDate { get; set; } = string.Empty;

        public List<MOBFlightStatusSegment> Segments { get; set; }

        public MOBAirportAdvisoryMessage AirportAdvisoryMessage { get; set; }

        public MOBFlightStatusInfo()
        {
            Segments = new List<MOBFlightStatusSegment>();
        }

    }


    public class MOBFlightStatusError
    {
        public Error[] Errors { get; set; }

        public DateTime GenerationTime { get; set; }

        public string InnerException { get; set; }

        public string Message { get; set; }

        public string ServerName { get; set; }

        public int Severity { get; set; }

        public string StackTrace { get; set; }

        public string Version { get; set; }
    }

    public class Error
    {
        public string MajorCode { get; set; }

        public string MajorDescription { get; set; }

        public string MinorCode { get; set; }

        public string MinorDescription { get; set; }
    }

}
