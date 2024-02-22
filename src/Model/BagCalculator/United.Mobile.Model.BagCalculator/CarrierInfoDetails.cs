using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.BagCalculator
{
    public class CarrierInfoDetails
    {
        public string ImageName { get; set; } = string.Empty;
        public bool IsStarAirline { get; set; }
        public string AppID { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string CarrierCode { get; set; } = string.Empty;
        public string CarrierShortName { get; set; } = string.Empty;
        public string CarrierImageSrc { get; set; } = string.Empty;
        public DateTime InsertedDateTime { get; set; }
        public string CarrierFullName { get; set; } = string.Empty;
    }
}