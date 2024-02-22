using System.Collections.Generic;
using United.Mobile.Model.HomePageContent;
using United.Mobile.Model.Internal.Common;
namespace United.Mobile.Model.Internal.EmployeeProfile
{
    public class EmployeeProfileEResResponse
    {
        public bool IsAllowedToSelectFlight { get; set; }
        public EmployeeJA EmployeeJA { get; set; }
        public EmployeeJA LoggedInJA { get; set; }
        public bool AllowImpersonation { get; set; }
        public string ImpersonateType { get; set; }
        public PassRiderExtended LoggedInPassRider { get; set; }
        public bool FlightWatchOptinStatus { get; set; }
        public List<int> FlightwatchEmailDays { get; set; }
        public List<EResAlert> ProfileVerbiage { get; set; }
        public List<TravelDocument> TravelDocuments { get; set; }
        public Dictionary<string, string> EmailNotificationDays { get; set; }
        public bool IsMileagePlusEligible { get; set; }
        public bool IsSidaBadgeEligible { get; set; }
        public List<EPassSumaryAllotment> EpassSummaryDetails { get; set; }
        public MileagePlusRequest MileagePlus { get; set; }
        public List<EPassBalance> EPassAccountBalances { get; set; }
        public bool ShowSelectPrimaryFriend { get; set; }
        public string BoardDate { get; set; }
        public EmployeeAddress AddressVerification { get; set; }
        public SSRs Error { get; set; }
        public string LastCallDateTime { get; set; }
        public string ServerName { get; set; }
        public string Status { get; set; }
        public string TransactionID { get; set; }
        public EResAlert BaseAlert { get; set; }
        public string TransferMessage { get; set; }
        public List<EmpTravelTypeItem>TravelTypes {get; set;}
    }
}
