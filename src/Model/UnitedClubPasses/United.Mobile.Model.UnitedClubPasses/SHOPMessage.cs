using System;
using System.Collections.Generic;

namespace United.Mobile.Model.UnitedClubPasses
{
    [Serializable()]
    public class SHOPMessage
    {
        public string TripId { get; set; } = string.Empty;
        public string FlightId { get; set; } = string.Empty;
        public string ConnectionIndex { get; set; } = string.Empty;
        public string FlightNumberField { get; set; } = string.Empty;
        public string MessageCode { get; set; } = string.Empty;
        public List<object> MessageParameters { get; set; }

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

        //public string ConnectionIndex
        //{
        //    get
        //    {
        //        return this.connectionIndex;
        //    }
        //    set
        //    {
        //        this.connectionIndex = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string FlightNumberField
        //{
        //    get
        //    {
        //        return this.flightNumberField;
        //    }
        //    set
        //    {
        //        this.flightNumberField = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public string MessageCode
        //{
        //    get
        //    {
        //        return this.messageCode;
        //    }
        //    set
        //    {
        //        this.messageCode = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        //    }
        //}

        //public List<object> MessageParameters
        //{
        //    get
        //    {
        //        return this.messageParameters;
        //    }
        //    set
        //    {
        //        this.messageParameters = value;
        //    }
        //}


        public SHOPMessage()
        {
            MessageParameters = new List<object>();
        }
    }
}
