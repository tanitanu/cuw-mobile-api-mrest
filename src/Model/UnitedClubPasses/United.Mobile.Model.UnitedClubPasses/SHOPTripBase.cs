using System;
using System.Runtime.Serialization;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class SHOPTripBase
    {
        public string Origin { get; set; } = string.Empty;

        //MB-2639 add all airports flag
        public int OriginAllAirports { get; set; } = -1;

        public string Destination { get; set; } = string.Empty;

        //MB-2639 add all airports flag
        public int DestinationAllAirports { get; set; } = -1;

        public string DepartDate { get; set; } = string.Empty;
        public string ArrivalDate { get; set; } = string.Empty;
        public string Cabin { get; set; } = string.Empty;
        public bool UseFilters { get; set; } = false;
        public SHOPSearchFilters SearchFiltersIn { get; set; }
        public SHOPSearchFilters SearchFiltersOut { get; set; }
        public bool SearchNearbyOriginAirports { get; set; } = false;
        public bool SearchNearbyDestinationAirports { get; set; } = false;
        public string ShareMessage { get; set; } = string.Empty;
        public int Index { get; set; }
        public SHOPTripChangeType ChangeType { get; set; } = SHOPTripChangeType.NoChange;

        //public int Index
        //{
        //    get
        //    {
        //        return this.index;
        //    }
        //    set
        //    {
        //        this.index = value;
        //    }
        //}
        //public string Origin
        //{
        //    get
        //    {
        //        return this.origin;
        //    }
        //    set
        //    {
        //        this.origin = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
        //    }
        //}

        //public int OriginAllAirports
        //{
        //    get
        //    {
        //        return originAllAirports;
        //    }
        //    set
        //    {
        //        this.originAllAirports = value;
        //    }
        //}

        //public string Destination
        //{
        //    get
        //    {
        //        return this.destination;
        //    }
        //    set
        //    {
        //        this.destination = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
        //    }
        //}
        //public int DestinationAllAirports
        //{
        //    get
        //    {
        //        return destinationAllAirports;
        //    }
        //    set
        //    {
        //        this.destinationAllAirports = value;
        //    }
        //}

        //public string DepartDate
        //{
        //    get
        //    {
        //        return this.departDate;
        //    }
        //    set
        //    {
        //        this.departDate = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string ArrivalDate
        //{
        //    get
        //    {
        //        return this.arrivalDate;
        //    }
        //    set
        //    {
        //        this.arrivalDate = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string Cabin
        //{
        //    get
        //    {
        //        return this.cabin;
        //    }
        //    set
        //    {
        //        this.cabin = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public bool UseFilters
        //{
        //    get
        //    {
        //        return this.useFilters;
        //    }
        //    set
        //    {
        //        this.useFilters = value;
        //    }
        //}

        //public SHOPSearchFilters SearchFiltersIn
        //{
        //    get
        //    {
        //        return this.searchFiltersIn;
        //    }
        //    set
        //    {
        //        this.searchFiltersIn = value;
        //    }
        //}

        //public SHOPSearchFilters SearchFiltersOut
        //{
        //    get
        //    {
        //        return this.searchFiltersOut;
        //    }
        //    set
        //    {
        //        this.searchFiltersOut = value;
        //    }
        //}

        //public bool SearchNearbyOriginAirports
        //{
        //    get
        //    {
        //        return this.searchNearbyOriginAirports;
        //    }
        //    set
        //    {
        //        this.searchNearbyOriginAirports = value;
        //    }
        //}

        //public bool SearchNearbyDestinationAirports
        //{
        //    get
        //    {
        //        return this.searchNearbyDestinationAirports;
        //    }
        //    set
        //    {
        //        this.searchNearbyDestinationAirports = value;
        //    }
        //}
        //public string ShareMessage
        //{
        //    get
        //    {
        //        return this.shareMessage;
        //    }
        //    set
        //    {
        //        this.shareMessage = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public SHOPTripChangeType ChangeType
        //{
        //    get
        //    {
        //        return this.changeType;
        //    }
        //    set
        //    {
        //        this.changeType = value;
        //    }
        //}
    }

    [Serializable]
    public enum SHOPTripChangeType
    {
        [EnumMember(Value = "0")]
        ChangeFlight,
        [EnumMember(Value = "1")]
        AddFlight,
        [EnumMember(Value = "2")]
        DeleteFlight,
        [EnumMember(Value = "3")]
        NoChange
    }
}
