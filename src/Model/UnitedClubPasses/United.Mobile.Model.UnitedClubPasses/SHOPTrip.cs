using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class SHOPTrip : SHOPTripBase
    {
        public string TripId { get; set; } = string.Empty;
        public string OriginDecoded { get; set; } = string.Empty;
        public string DestinationDecoded { get; set; } = string.Empty;
        public int FlightCount { get; set; }
        public int TotalFlightCount { get; set; }
        public List<SHOPFlattenedFlight> FlattenedFlights { get; set; }
        //public List<SHOPFlightSection> flightSections;
        public int LastTripIndexRequested { get; set; }
        public List<SHOPShoppingProduct> Columns { get; set; }
        public string YqyrMessage { get; set; } = string.Empty;
        public int PageCount { get; set; }
        public string OriginDecodedWithCountry { get; set; }
        public string DestinationDecodedWithCountry { get; set; }
        public bool ShowOriginDestinationForFlights { get; set; } = false;
        public string CallDurationText { get; set; } = string.Empty;
        public bool TripHasNonStopflightsOnly { get; set; }

        //public string flightDateChangeMessage = ConfigurationManager.AppSettings["FlightDateChangeMessage"].ToString();

        ////public string DestinationDecodedWithCountry
        ////{
        ////    get
        ////    {
        ////        return this.destinationDecodedWithCountry;
        ////    }
        ////    set
        ////    {
        ////        this.destinationDecodedWithCountry = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        ////    }
        ////}

        ////public string OriginDecodedWithCountry
        ////{
        ////    get
        ////    {
        ////        return this.originDecodedWithCountry;
        ////    }
        ////    set
        ////    {
        ////        this.originDecodedWithCountry = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        ////    }
        ////}


        ////public string YqyrMessage
        ////{
        ////    get { return yqyrMessage; }
        ////    set { yqyrMessage = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); }
        ////}

        ////public string TripId
        ////{
        ////    get
        ////    {
        ////        return this.tripId;
        ////    }
        ////    set
        ////    {
        ////        this.tripId = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        ////    }
        ////}

        ////public string OriginDecoded
        ////{
        ////    get
        ////    {
        ////        return this.originDecoded;
        ////    }
        ////    set
        ////    {
        ////        this.originDecoded = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        ////    }
        ////}

        ////public string DestinationDecoded
        ////{
        ////    get
        ////    {
        ////        return this.destinationDecoded;
        ////    }
        ////    set
        ////    {
        ////        this.destinationDecoded = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        ////    }
        ////}

        ////public bool ShowOriginDestinationForFlights
        ////{
        ////    get
        ////    {
        ////        return this.showOriginDestinationForFlights;
        ////    }
        ////    set
        ////    {
        ////        this.showOriginDestinationForFlights = value;
        ////    }
        ////}

        //public List<SHOPFlattenedFlight> FlattenedFlights
        //{
        //    get
        //    {
        //        return this.flattenedFlights;
        //    }
        //    set
        //    {
        //        this.flattenedFlights = value;
        //    }
        //}

        ////public List<SHOPFlightSection> FlightSections
        ////{
        ////    get
        ////    {
        ////        return this.flightSections;
        ////    }
        ////    set
        ////    {
        ////        this.flightSections = value;
        ////    }
        ////}

        ////public int FlightCount
        ////{
        ////    get
        ////    {
        ////        return this.flightCount;
        ////    }
        ////    set
        ////    {
        ////        this.flightCount = value;
        ////    }
        ////}

        ////public int TotalFlightCount
        ////{
        ////    get
        ////    {
        ////        return this.totalFlightCount;
        ////    }
        ////    set
        ////    {
        ////        this.totalFlightCount = value;
        ////    }
        ////}

        ////public int LastTripIndexRequested
        ////{
        ////    get
        ////    {
        ////        return this.lastTripIndexRequested;
        ////    }
        ////    set
        ////    {
        ////        this.lastTripIndexRequested = value;
        ////    }
        ////}

        ////public List<SHOPShoppingProduct> Columns
        ////{
        ////    get
        ////    {
        ////        return this.columns;
        ////    }
        ////    set
        ////    {
        ////        this.columns = value;
        ////    }
        ////}


        //public string CallDurationText
        //{
        //    get { return callDurationText; }
        //    set { callDurationText = string.IsNullOrEmpty(value) ? string.Empty : value.Trim(); }
        //}

        //public int PageCount
        //{
        //    get
        //    {
        //        return this.pageCount;
        //    }
        //    set
        //    {
        //        this.pageCount = value;
        //    }
        //}


        //public string FlightDateChangeMessage
        //{
        //    get { return this.flightDateChangeMessage; }
        //    set { }
        //}


        //public bool TripHasNonStopflightsOnly
        //{
        //    get { return tripHasNonStopflightsOnly; }
        //    set { tripHasNonStopflightsOnly = value; }
        //}

    }
}
