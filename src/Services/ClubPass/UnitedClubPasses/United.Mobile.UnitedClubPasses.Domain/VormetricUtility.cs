using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.Model;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.UnitedClubPasses;
using United.Utility.Helper;
using CslDataVaultResponse = United.Service.Presentation.PaymentResponseModel.DataVault<United.Service.Presentation.PaymentModel.Payment>;

namespace United.Mobile.UnitedClubPasses.Domain
{
    public class VormetricUtility
    {
        private MOBApplication _application = new MOBApplication() { Version = new MOBVersion() };
        private string _action = string.Empty;
        private string _deviceId = string.Empty;
        private string _token = string.Empty;
        private string _sessionId = string.Empty;
        private readonly IConfiguration _configuration;
        private readonly Utility _utility;
        private readonly IDataVaultService _dataVaultService;
        private readonly ICachingService _cachingService;
        private readonly ICacheLog<PurchaseOTPPassesBusiness> _logger;
        public VormetricUtility(ICacheLog<PurchaseOTPPassesBusiness> logger,OTPPurchaseRequest request, string token, string sessionId, IConfiguration configuration
            , IDataVaultService dataVaultService, ICachingService cachingService)
        {
            _logger = logger;
            _application.Id = request.Application.Id;
            _application.Version.Major = request.Application.Version.Major;
            _deviceId = request.DeviceId;
            _token = token;
            _sessionId = sessionId;
            _configuration = configuration;
            _utility = new Utility(_configuration);
            _dataVaultService = dataVaultService;
            _cachingService = cachingService;
        }

        public bool AssignDataVaultAndPersistTokenToCC(string sessionId, string sessionToken, CreditCard creditCard)
        {
            bool isPersistAssigned = _configuration.GetValue<bool>("VormetricTokenMigration");
            if (isPersistAssigned)
            {
                Profile profile = new Profile(_configuration,  _dataVaultService, _cachingService, _logger);
                if (profile.GenerateCCTokenWithDataVault(creditCard, sessionId, sessionToken, _application, _deviceId))
                {
                    if (!string.IsNullOrEmpty(creditCard.PersistentToken))
                    {
                        if (creditCard.UnencryptedCardNumber != null && creditCard.UnencryptedCardNumber.Length > 4)
                        {
                            creditCard.UnencryptedCardNumber = "XXXXXXXXXXXX" + creditCard.UnencryptedCardNumber.Substring((creditCard.UnencryptedCardNumber.Length - 4), 4);
                        }
                        else
                        {
                            creditCard.UnencryptedCardNumber = "XXXXXXXXXXXX";
                        }
                    }
                    else if (string.IsNullOrEmpty(creditCard.AccountNumberToken) && !string.IsNullOrEmpty(_token) && !string.IsNullOrEmpty(_sessionId))
                    {
                        VormetricKeys vormetricKeys = GetPersistentTokenUsingAccountNumberToken(creditCard.AccountNumberToken, _sessionId, _token);
                        creditCard.PersistentToken = vormetricKeys.PersistentToken;
                        creditCard.SecurityCodeToken = vormetricKeys.SecurityCodeToken;
                        creditCard.CardType = vormetricKeys.CardType;
                    }
                }
            }
            return isPersistAssigned;
        }

        private VormetricKeys GetPersistentTokenUsingAccountNumberToken(string accountNumber, string sessionId, string token)
        {
            var cslResponse = _dataVaultService.GetRSAWithDataVault(token, accountNumber, sessionId).Result;

            return GetPersistentTokenFromCSLDatavaultResponse(cslResponse, sessionId);
        }

        private VormetricKeys GetPersistentTokenFromCSLDatavaultResponse(string jsonResponseFromCSL, string sessionID)
        {
            VormetricKeys vormetricKeys = new VormetricKeys();
            if (!string.IsNullOrEmpty(jsonResponseFromCSL))
            {
                CslDataVaultResponse response = DataContextJsonSerializer.DeserializeJsonDataContract<CslDataVaultResponse>(jsonResponseFromCSL);
                if (response != null && response.Responses != null && response.Responses[0].Error == null && response.Responses[0].Message != null && response.Responses[0].Message.Count > 0 && response.Responses[0].Message[0].Code.Trim() == "0")
                {
                    var creditCard = response.Items[0] as Service.Presentation.PaymentModel.CreditCard;
                    vormetricKeys.PersistentToken = creditCard.PersistentToken;
                    vormetricKeys.SecurityCodeToken = creditCard.SecurityCodeToken;
                    vormetricKeys.CardType = creditCard.Code;
                }
                else
                {
                    if (response.Responses[0].Error != null && response.Responses[0].Error.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Responses[0].Error)
                        {
                            errorMessage = errorMessage + " " + error.Text;
                        }
                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToInsertCreditCardToProfileErrorMessage");
                        if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL InsertTravelerCreditCard(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }

            return vormetricKeys;
        }

    }
}
