using System.Collections.Generic;
using System.Threading.Tasks;
using United.Foundations.Practices.Framework.Security.DataPower.Models;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Services.Customer.Common;

namespace United.Common.Helper.Profile
{
    public interface IMileagePlusTFACSL
    {
        Task<bool> GetTfaWrongAnswersFlag(string sessionid, string token, int customerId, string mileagePlusNumber, bool answeredQuestionsIncorrectly, string languageCode);
        string ValidDeviceIDforDP(string deviceId, bool isGuestType = false);
        Task<bool> ValidateDevice(Session session, string appVersion, string languageCode);
        Task<SaveResponse> SendForgotPasswordEmail(string sessionid, string token, string mileagePlusNumber, string emailAddress, string languageCode);
        Task<SaveResponse> SendResetAccountEmail(string sessionid, string token, int customerId, string mileagePlusNumber, string emailAddress, string languageCode);
        Task<List<Securityquestion>> GetMPPINPWDSecurityQuestions(string token, string sessionId, int appId, string appVersion, string deviceId);
        bool SignOutSession(string sessionid, string token, int appId);
        Task<bool> ShuffleSavedSecurityQuestions(string token, string sessionId, int appId, string appVersion, string deviceId, string MileagePlusID, int customerID = 0, string loyaltyId = null);
        Task<bool> AddDeviceAuthentication(Session session, string appVersion, string languageCode);
        Task<List<Securityquestion>> GetMPPinPwdSavedSecurityQuestions(string token, int customerId, string mileagePlusNumber, string sessionId, int appId, string appVersion, string deviceId);
        DpRequest GetDPRequestObject(int applicationID, string deviceId);
    }
}
