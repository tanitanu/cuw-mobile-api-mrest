using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;

namespace United.Mobile.MemberProfile.Domain
{
    public interface IMemberProfileBusiness
    {
        Task<MOBContactUsResponse> GetContactUsDetails(MOBContactUsRequest request);
        Task<MOBCustomerPreferencesResponse> RetrieveCustomerPreferences(MOBCustomerPreferencesRequest request);
        Task<MOBCustomerProfileResponse> GetProfileOwner(MOBCustomerProfileRequest request);
        Task<MPAccountSummaryResponse> GetAccountSummaryWithMemberCardPremierActivity(MPAccountValidationRequest request);
        Task<MOBCPProfileResponse> GetProfileCSL_CFOP(MOBCPProfileRequest request);
        Task<MOBCPProfileResponse> GetEmpProfileCSL_CFOP(MOBCPProfileRequest request);
        Task<MOBCPProfileResponse> MPSignedInInsertUpdateTraveler_CFOP(MOBUpdateTravelerRequest request);
        Task<FrequentFlyerRewardProgramsResponse> GetLatestFrequentFlyerRewardProgramList(int applicationId, string appVersion, string accessCode, string transactionId, string languageCode);
        Task<MOBCPProfileResponse> UpdateTravelerCCPromo_CFOP(MOBUpdateTravelerRequest request);
        Task<MOBUpdateTravelerInfoResponse> UpdateTravelersInformation(MOBUpdateTravelerInfoRequest request);
        bool IsEnableFeature(string feature, int appId, string appVersion);

        Task<MOBPartnerSSOTokenResponse> GetPartnerSSOToken(MOBPartnerSSOTokenRequest request);
    }
}
