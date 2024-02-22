using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.SeatMapEngine;

namespace United.Mobile.Model.Common
{
    [Serializable()]
    public class MOBSeatMapRequest : MOBRequest
    {
        private string flightNumber = string.Empty;
        private string departureAirportCode = string.Empty;
        private string arrivalAirportCode = string.Empty;
        private string marketingCarrierCode = string.Empty;
        private string operatingCarrierCode = string.Empty;
        private string sessionId = string.Empty;
        private MOBSHOPFlattenedFlight fFlight;
        private List<string> productIDS;
        private string equipmentType = string.Empty;

        public MOBSeatMapRequest()
            : base()
        {
        }

        public MOBSHOPFlattenedFlight FFlight
        {
            get { return fFlight; }
            set { this.fFlight = value; }
        }

        public List<string> ProductIDS
        {
            get
            {
                return this.productIDS;
            }
            set
            {
                this.productIDS = value;
            }
        }
        public string SessionId
        {
            get
            {
                return this.sessionId;
            }
            set
            {
                this.sessionId = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string FlightNumber
        {
            get
            {
                return this.flightNumber;
            }
            set
            {
                this.flightNumber = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string FlightDate { get; set; } = string.Empty;

        public string DepartureAirportCode
        {
            get
            {
                return this.departureAirportCode;
            }
            set
            {
                this.departureAirportCode = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
            }
        }

        public string ArrivalAirportCode
        {
            get
            {
                return this.arrivalAirportCode;
            }
            set
            {
                this.arrivalAirportCode = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
            }
        }

        public string MarketingCarrierCode
        {
            get
            {
                return marketingCarrierCode;
            }
            set
            {
                this.marketingCarrierCode = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
            }
        }

        public string OperatingCarrierCode
        {
            get
            {
                return operatingCarrierCode;
            }
            set
            {
                this.operatingCarrierCode = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
            }
        }

        public string EquipmentType
        {
            get
            {
                return this.equipmentType;
            }
            set
            {
                this.equipmentType = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }
    }
}
