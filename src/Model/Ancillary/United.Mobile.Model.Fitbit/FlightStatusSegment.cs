using System;
using United.Mobile.Model.FlightStatus;

namespace United.Mobile.Model.Fitbit
{
    [Serializable]
    public class FlightStatusSegment : Segment
    {
        public string ScheduledFlightTime { get; set; } = string.Empty;

        public string ActualFlightTime { get; set; } = string.Empty;

        public string EstimatedDepartureDateTime { get; set; } = string.Empty;

        public string EstimatedArrivalDateTime { get; set; } = string.Empty;

        public string ActualDepartureDateTime { get; set; } = string.Empty;

        public string ActualArrivalDateTime { get; set; } = string.Empty;

        public string DepartureTerminal { get; set; } = string.Empty;

        public string ArrivalTerminal { get; set; } = string.Empty;

        public string DepartureGate { get; set; } = string.Empty;

        public string ArrivalGate { get; set; } = string.Empty;

        public Equipment Ship { get; set; }

        public Airline OperatingCarrier { get; set; }

        public Airline CodeShareCarrier { get; set; }

        public string Status { get; set; } = string.Empty;

        public string CodeShareflightNumber { get; set; } = string.Empty;

        public bool IsSegmentCancelled { get; set; }

        public bool ISWiFiAvailable { get; set; }

        public string LastUpdatedGMT { get; set; } = string.Empty;

        public string StatusShort { get; set; } = string.Empty;
    }
}
