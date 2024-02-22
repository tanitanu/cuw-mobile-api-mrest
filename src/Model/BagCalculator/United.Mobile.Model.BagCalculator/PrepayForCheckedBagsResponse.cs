using System;
using System.Collections.Generic;
using System.Text;

namespace United.Mobile.Model.BagCalculator
{

    public class CheckBagBaggageFeeItem
    {
        public double Amount { get; set; }
        public double Miles { get; set; }
        public int BagNumber { get; set; }
        public bool IsIncluded { get; set; }
        public string ProductId { get; set; }
        public string SubCode { get; set; }
    }

    public class CheckBagTravelerItem
    {
        public string BagsCaption { get; set; }
        public string FullName { get; set; }
        public string Id { get; set; }
        public List<CheckBagBaggageFeeItem> BaggageFees { get; set; }
        public int MaxAllowedBags { get; set; }
        public int MinAllowedBags { get; set; }
        public int BagAllowance { get; set; }
        public int BagsPurchased { get; set; }
        public string BaggageSummaryTravelerTitle { get; set; }
    }

    public class CheckBagFlightSegmentItem
    {
        public string Id { get; set; }
        public string SubTitle { get; set; }
        public string Title { get; set; }
        public string IneligibleLOFMessage { get; set; }
        
        public List<CheckBagTravelerItem> Travelers { get; set; }
    }
    public class PrepayForCheckedBagsResponse : MOBResponse
    {
        public string Disclaimer { get; set; }
        public List<CheckBagFlightSegmentItem> FlightSegments { get; set; }
        public string Flow { get; set; }
        public string SessionId { get; set; }
        public string CartId { get; set; }
        public string ProductCode { get; set; }
        public List<United.Mobile.Model.Common.MOBItem> Captions { get; set; }
    }
}
