using System.Collections.Generic;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.SSR;
using United.Mobile.Model.Shopping;
using MOBFOPCertificateTraveler = United.Mobile.Model.Shopping.FormofPayment.MOBFOPCertificateTraveler;

namespace United.Mobile.MemberProfile.Domain
{
    public interface IMemberProfileUtility
    {
        Task<bool> IsValidGetProfileCSLRequest(string transactionId, string deviceId, int applicationId, string mpNumber, long customerID, string sessionId);

        Task<bool> IsValidDeviceRequest(string transactionId, string deviceId, int applicationId, string mpNumber,string sessionId);
        Task<bool> ValidateAccountFromCache(string accountNumber, string pinCode,string sessionId);
        Task<bool> IsTSAFlaggedAccount(string accountNumber,string sessionId);
        string GetMPAuthTokenWithoutPersistSave(string accountNumber, int applicationId, string deviceId, string appVersion);
        
        bool isApplicationVersionGreater2(int applicationID, string appVersion, string androidnontfaversion,
            string iphonenontfaversion, string windowsnontfaversion, string mWebNonELFVersion);
        Task<bool> ValidateAccountFromCache(string accountNumber, string pinCode);
        bool EnableChaseOfferRTI(int appID, string appVersion);
        bool IsEnableOmniCartReleaseCandidateOneChanges(int applicationId, string appVersion);
        void CompareOmnicartTravelersWithSavedTravelersandUpdate(United.Mobile.Model.Shopping.Reservation reservation, bool isEnableExtraSeatReasonFixInOmniCartFlow);
        bool IsOmniCartSavedTrip(Model.Shopping.Reservation reservation);
        void ValidateTravelersForCubaReason(List<MOBCPTraveler> travelersCSL, bool isCuba);
        void ValidateTravelersCSLForCubaReason(MOBCPProfileResponse profileResponse);
        string TravelersHeaderText(Model.Shopping.Reservation bookingPathReservation);
        void UpdateTravelerCubaReasonInProfile(List<MOBCPProfile> profiles, MOBCPTraveler traveler);
        void UpdateMatchedTravelerCubaReason(MOBCPTraveler traveler, List<MOBCPTraveler> travelers);
        Task<bool> UpdateViewName(MOBUpdateTravelerRequest request);
        Task<bool> isValidDeviceRequest(string transactionId, string deviceId, int applicationId, string mpNumber, string sessionId);
        Task<(bool, string validAuthToken)> ValidateHashPinAndGetAuthToken(string accountNumber, string hashPinCode, int applicationId, string deviceId, string appVersion, string validAuthToken, string transactionId, string sessionId);
        bool IsEnableFeature(string feature, int appId, string appVersion);
    }
}
