using System;
using United.Mobile.Model.Common;
using United.Mobile.Model.FlightStatus;

namespace United.Mobile.Model.Fitbit
{
    [Serializable]
    public class MOBFlightStatusSegment : MOBSegment
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

        public MOBAirline OperatingCarrier { get; set; }

        public MOBAirline CodeShareCarrier { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool EnableSeatMap { get; set; }

        public bool EnableStandbyList { get; set; }

        public bool EnableUpgradeList { get; set; }

        public bool EnableAmenity { get; set; }

        public string CodeShareflightNumber { get; set; } = string.Empty;

        public bool CanPushNotification { get; set; }

        public bool IsSegmentCancelled { get; set; }

        public bool GetInBoundSegment { get; set; }

        public MOBFlightSegment InBoundSegment { get; set; }

        public bool ISWiFiAvailable { get; set; }

        public string LastUpdatedGMT { get; set; } = string.Empty;

        public string PushNotificationRegId { get; set; } = string.Empty;

        public string StatusShort { get; set; } = string.Empty;

        public bool AddToComplications { get; set; }

        public MOBBaggage Baggage { get; set; }

        //User Story - 160153 - Added below DepartureAirport and ArrivalAirport property to get the airportname from database
        public string ArrivalAirport { get; set; } = string.Empty;

        public string DepartureAirport { get; set; } = string.Empty;

        public bool EnableAllFlifoTopTabs { get; set; }

        public bool EnableFlifoPushNotification { get; set; }

        public bool EnableShareMyFlight { get; set; }

        public bool EnableWhereAircraftCurrently { get; set; }

        public bool EnableWhereAirCraftCurrently { get; set; }

        public MOBFlightStatusSegmentDetails FlightStatusSegmentDetails { get; set; }

        public bool IsShuttleService { get; set; }

    }
}
