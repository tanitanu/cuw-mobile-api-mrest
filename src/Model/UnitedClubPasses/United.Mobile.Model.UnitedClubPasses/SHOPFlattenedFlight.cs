using System;
using System.Collections.Generic;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class SHOPFlattenedFlight
    {
        public string TripId { get; set; } = string.Empty;
        public string FlightId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string TripDays { get; set; } = string.Empty;
        public string CabinMessage { get; set; } = string.Empty;
        public List<SHOPFlight> Flights;
        public bool IsUADiscount { get; set; }
        public bool IsAddCollectWaived { get; set; }
        public string AddCollectProductId{ get; set; } 
        public string FlightHash { get; set; } = string.Empty;
        public bool IsIBELite { get; set; }
        public bool IsIBE { get; set; }
        public bool IsElf { get; set; }
        public bool IsCovidTestFlight { get; set; }
        public bool IsChangeFeeWaiver { get; set; }
        public List<string> FlightLabelTextList { get; set; }
        public string MsgFlightCarrier { get; set; }

        //public string TripId
        //{
        //    get
        //    {
        //        return this.tripId;
        //    }
        //    set
        //    {
        //        this.tripId = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string FlightId
        //{
        //    get
        //    {
        //        return this.flightId;
        //    }
        //    set
        //    {
        //        this.flightId = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string CabinMessage
        //{
        //    get
        //    {
        //        return this.cabinMessage;
        //    }
        //    set
        //    {
        //        this.cabinMessage = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string ProductId
        //{
        //    get
        //    {
        //        return this.productId;
        //    }
        //    set
        //    {
        //        this.productId = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public List<SHOPFlight> Flights
        //{
        //    get
        //    {
        //        return this.flights;
        //    }
        //    set
        //    {
        //        this.flights = value;
        //    }
        //}

        //public string TripDays
        //{
        //    get
        //    {
        //        return this.tripDays;
        //    }
        //    set
        //    {
        //        this.tripDays = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public bool IsUADiscount
        //{
        //    get
        //    {
        //        return this.isUADiscount;
        //    }
        //    set
        //    {
        //        this.isUADiscount = value;
        //    }
        //}

        //public bool IsAddcollectWaived
        //{
        //    get
        //    {
        //        return this.isAddCollectWaived;
        //    }
        //    set
        //    {
        //        this.isAddCollectWaived = value;
        //    }
        //}

        //public string AddCollectProductId
        //{
        //    get
        //    {
        //        return this.addCollectProductId;
        //    }
        //    set
        //    {
        //        this.addCollectProductId = value;
        //    }
        //}

        /// <example>
        /// "16-31|1180-UA"
        /// </example>
        /// <hint>
        /// The flight's hash of the flight. If a flight has connection, this is the flight hash of the first segment 
        /// </hint>
        //public string FlightHash
        //{
        //    get
        //    {
        //        return flightHash;
        //    }
        //    set
        //    {
        //        flightHash = string.IsNullOrEmpty(value) ? "US" : value.Trim().ToUpper();
        //    }
        //}

        public string AirportChange { get; set; }

        //public string AirPortChange
        //{
        //    get { return this.airportChange; }
        //    set
        //    {
        //        this.airportChange = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}
        ///// <summary>
        ///// True if the product is IBELite amd is available
        ///// </summary>
        //public bool IsIBELite
        //{
        //    get { return isIBELite; }
        //    set { isIBELite = value; }
        //}

        //public bool IsIBE
        //{
        //    set { isIBE = value; }
        //    get { return isIBE; }
        //}

        //public bool IsElf
        //{
        //    set { isElf = value; }
        //    get { return isElf; }
        //}
        //public bool IsChangeFeeWaiver
        //{
        //    get { return isChangeFeeWaiver; }
        //    set { isChangeFeeWaiver = value; }
        //}
        //public bool IsCovidTestFlight
        //{
        //    get { return isCovidTestFlight; }
        //    set { isCovidTestFlight = value; }
        //}
        //public List<string> FlightLabelTextList
        //{
        //    get { return flightLabelTextList; }
        //    set { flightLabelTextList = value; }
        //}

        ////public List<StyledText> flightBadges;
        ////public List<StyledText> FlightBadges
        ////{
        ////    get { return flightBadges; }
        ////    set { flightBadges = value; }
        ////}

        //public string MsgFlightCarrier
        //{
        //    get { return msgFlightCarrier; }
        //    set { msgFlightCarrier = value; }
        //}

        public SHOPFlattenedFlight()
        {
            Flights = new List<SHOPFlight>();
        }

    }
}
