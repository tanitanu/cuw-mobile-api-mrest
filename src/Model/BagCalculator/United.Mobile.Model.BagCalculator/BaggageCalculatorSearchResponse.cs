using System;
using System.Collections.Generic;

namespace United.Mobile.Model.BagCalculator
{
    [Serializable]
    public class BaggageCalculatorSearchResponse : MOBResponse
    {
        public List<MemberShipStatus> LoyaltyLevels { get; set; }
        public List<CarrierInfo> Carriers { get; set; }
        public List<ClassOfService> ClassOfServices { get; set; }

        public BaggageCalculatorSearchResponse()
        {
            LoyaltyLevels = new List<MemberShipStatus>();
            Carriers = new List<CarrierInfo>();
            ClassOfServices = new List<ClassOfService>();
        }
    }
}
