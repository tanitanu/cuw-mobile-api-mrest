using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using United.Mobile.Model.Common;

namespace United.Common.Helper.Profile
{
    public interface IMileagePlusCSL
    {
        Task<bool> ValidateMileagePlusNames(MOBMPSignInNeedHelpRequest request, string token, string sessionId, int appId, string appVersion, string deviceId);
        MOBMPAccountValidation ValidateAccount(int applicationId, string deviceId, string appVersion, string transactionId, string mileagePlusNumber, string pinCode, Session shopTokenSession, bool validAuthTokenHashPinCheckFailed);
        Task<bool> GetWrongAnswersFlag(bool answeredQuestionsIncorrectly, string token, string sessionId, int appId, string appVersion, string deviceId, int customerID = 0, string loyaltyId = null);
        Task<bool> UpdateWrongAnswersFlag(bool answeredQuestionsIncorrectly, string token, string sessionId, int appId, string appVersion, string deviceId, int customerID = 0, string loyaltyId = null);
        Task<bool> SearchMPAccount(MOBMPSignInNeedHelpRequest needHelpRequest, string token, string sessionId, int appId, string appVersion, string deviceId);
        Task<(List<MOBCustomerSearchDetail>, bool)> SearchCustomer(MOBMPSignInNeedHelpRequest request, string token, string sessionId, int appId, string appVersion, string deviceId);
        Task<bool> ShuffleSavedSecurityQuestions(string token, string sessionId, int appId, string appVersion, string deviceId, string MileagePlusID, int customerID = 0, string loyaltyId = null);
    }
}
