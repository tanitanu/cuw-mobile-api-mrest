using System.Threading.Tasks;
using UAWSMPTravelCertificateService.ETCServiceSoap;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;

namespace United.Common.Helper.Profile
{
    public interface IMileagePlus
    {
        Task<MPAccountSummary> GetAccountSummary(string transactionId, string mileagePlusNumber, string languageCode, bool includeMembershipCardBarCode, string sessionId = "");
        Task<(string employeeId, string displayEmployeeId)> GetEmployeeIdy(string mileageplusNumber, string transactionId, string sessionId, string displayEmployeeId);
        void GetMPEliteLevelExpirationDateAndGenerateBarCode(MPAccountSummary mpSummary, string premierLevelExpirationDate, MOBInstantElite instantElite);
        Task<MOBPlusPoints> GetPlusPointsFromLoyaltyBalanceService(MPAccountValidationRequest req, string dpToken);
        bool IsPremierStatusTrackerSupportedVersion(int appId, string appVersion);
        Task<MPAccountSummary> GetAccountSummaryWithPremierActivity(MPAccountValidationRequest req, bool includeMembershipCardBarCode, string dpToken);
        Task<bool> GetProfile_AllTravelerData(string mileagePlusNumber, string transactionId, string dpToken, int applicationId, string appVersion, string deviceId);
        Task<ETCReturnType> GetTravelCertificateResponseFromETC(string mileagePlusNumber, MOBApplication application, string sessionId, string transactionId, string deviceId);
        Task<MPAccountSummary> GetAccountSummaryV2(string transactionId, string mileagePlusNumber, string languageCode, bool includeMembershipCardBarCode, string sessionId = "");        
        Task<MOBClubMembership> GetCurrentMembershipInfo(string mpNumber);
        Task<MOBClubMembership> GetCurrentMembershipInfoV2(string mpNumber, string Token);
        Task<MPAccountSummary> GetAccountSummaryWithPremierActivityV2(MPAccountValidationRequest req, bool includeMembershipCardBarCode, string dpToken);

    }
}
