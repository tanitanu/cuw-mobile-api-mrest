using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using United.Mobile.Model.FlightStatus;

namespace United.Mobile.Model.Shopping
{
    [Serializable]
    public class FlightStatusInfo
    {
        private string carrierCode = string.Empty;
        private string flightNumber = string.Empty;
        private string codeShareflightNumber = string.Empty;
        private string flightDate = string.Empty;
        private AirportAdvisoryMessage airportAdvisoryMessage;
        private List<FlightStatusSegment> segments;

        public FlightStatusInfo()
        {
        }

        public string CarrierCode
        {
            get
            {
                return this.carrierCode;
            }
            set
            {
                this.carrierCode = string.IsNullOrEmpty(value) ? string.Empty : value.Trim().ToUpper();
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

        public string CodeShareflightNumber
        {
            get
            {
                return this.codeShareflightNumber;
            }
            set
            {
                this.codeShareflightNumber = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public string FlightDate
        {
            get
            {
                return this.flightDate;
            }
            set
            {
                this.flightDate = string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
            }
        }

        public List<FlightStatusSegment> Segments
        {
            get
            {
                return this.segments;
            }
            set
            {
                this.segments = value;
            }
        }

        public AirportAdvisoryMessage AirportAdvisoryMessage
        {
            get
            {
                return this.airportAdvisoryMessage;
            }
            set
            {
                this.airportAdvisoryMessage = value;
            }
        }
    }


    public class FlightStatusError
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
