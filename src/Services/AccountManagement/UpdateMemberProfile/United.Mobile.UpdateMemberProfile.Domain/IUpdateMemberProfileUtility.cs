using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using ProfileFOPCreditCardResponse = United.Mobile.Model.Common.ProfileFOPCreditCardResponse;
using ProfileResponse = United.Mobile.Model.Common.ProfileResponse;

namespace United.Mobile.UpdateMemberProfile.Domain
{
    public interface IUpdateMemberProfileUtility
    {
        Task<bool> SendChasePromoFeedbackToCCE(string mileagePlusNumber, string messageKey, Model.FeedBack.MOBPromoFeedbackEventType feedbackEventType, MOBRequest mobRequest, string sessionId, string logAction);
        Task<bool> isValidDeviceRequest(string transactionId, string deviceId, int applicationId, string mpNumber,string sessionId);
        bool ValidateAccountFromCache(string accountNumber, string pinCode);
        Task<ProfileFOPCreditCardResponse> GetCSLProfileFOPCreditCardResponseInSession(string sessionId);
        void UpdateTravelerCubaReasonInProfile(List<MOBCPProfile> profiles, MOBCPTraveler traveler);
        void UpdateMatchedTravelerCubaReason(MOBCPTraveler traveler, List<MOBCPTraveler> travelers);
        List<MOBCreditCard> GetProfileOwnerCreditCardList(MOBCPProfile profile, ref List<MOBAddress> creditCardAddresses, string updatedCCKey);
        bool IsValidFOPForTPIpayment(string cardType);
        T ObjectToObjectCasting<T, R>(R request);
        bool EnableInflightContactlessPayment(int appID, string appVersion, bool isReshop = false);
        bool IncludeMoneyPlusMiles(int appId, string appVersion);
        bool isApplicationVersionGreater(int applicationID, string appVersion, string androidnontfaversion,
           string iphonenontfaversion, string windowsnontfaversion, string mWebNonELFVersion, bool ValidTFAVersion);
        bool IncludeFFCResidual(int appId, string appVersion);
        Task<ProfileResponse> GetCSLProfileResponseInSession(string sessionId);
        ///bool ValidateHashPinAndGetAuthToken(string accountNumber, string hashPinCode, int applicationId, string deviceId, string appVersion, ref string validAuthToken);
        MOBSHOPReservation MakeReservationFromPersistReservation(MOBSHOPReservation reservation, Model.Shopping.Reservation bookingPathReservation,
            Session session);
        TripPriceBreakDown GetPriceBreakDown(Model.Shopping.Reservation reservation);
        
        string GetPriceAfterChaseCredit(decimal price);
        string GetPriceAfterChaseCredit(decimal price, string chaseCrediAmount);
        bool IncludeTravelCredit(int appId, string appVersion);
        bool IncludeTravelBankFOP(int appId, string appVersion);
        bool IsInternationalBillingAddress_CheckinFlowEnabled(MOBApplication application);
        void FormatChaseCreditStatemnet(MOBCCAdStatement chaseCreditStatement);

    }
}
