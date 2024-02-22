using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.UnitedClub;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.UnitedClubPasses;
using United.Service.Presentation.LoyaltyRequestModel;
using United.Service.Presentation.LoyaltyResponseModel;
using United.Service.Presentation.ProductRequestModel;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Utility.Helper;
using CslDataVaultRequest = United.Service.Presentation.PaymentRequestModel.DataVault<United.Service.Presentation.PaymentModel.Payment>;
using CslDataVaultResponse = United.Service.Presentation.PaymentResponseModel.DataVault<United.Service.Presentation.PaymentModel.Payment>;
using Reservation = United.Mobile.Model.UnitedClubPasses.Reservation;

namespace United.Mobile.UnitedClubPasses.Domain
{
    public class PurchaseOTPPassesBusiness : IPurchaseOTPPassesBusiness
    {
        private readonly ICacheLog<PurchaseOTPPassesBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDPService _tokenService;
        private readonly IDataVaultService _dataVaultService;
        private readonly IPersistentTokenByAccountNumberTokenService _persistentTokenByAccountNumberTokenService;
        private readonly IPayPalCreditCardService _payPalCreditCardService;
        private readonly IMasterPassSessionDetailsService _masterPassSessionDetailsService;
        private readonly IMECSLFullfillmentService _mECSLFullfillmentService;
        private readonly ILoyaltyUnitedClubIssuePassService _loyaltyUnitedClubIssuePassService;
        private readonly ICachingService _cachingService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ICSLStatisticsService _cSLStatisticsService;
        private readonly MerchandizingServices _merchService;
        public OTPPurchaseRequest request { get; set; }
        private CSLStatistics _cSLStatistics;
        private readonly IPaymentDataAccessService _paymentDataAccessService;
        private readonly Utility _utility;
        private readonly IFeatureSettings _featureSettings;
        private readonly IAuroraMySqlService _auroraMySqlService;

        public PurchaseOTPPassesBusiness(ICacheLog<PurchaseOTPPassesBusiness> logger, IConfiguration configuration, IDPService tokenService
            , IDataVaultService dataVaultService
            , IPersistentTokenByAccountNumberTokenService persistentTokenByAccountNumberTokenService
           , IPayPalCreditCardService payPalCreditCardService, IMasterPassSessionDetailsService masterPassSessionDetailsService
            , IMECSLFullfillmentService mECSLFullfillmentService, ILoyaltyUnitedClubIssuePassService loyaltyUnitedClubIssuePassService
            , ICachingService CachingService, IDynamoDBService dynamoDBService, ICSLStatisticsService cSLStatisticsService
            , IPaymentDataAccessService paymentDataAccessService
            ,IFeatureSettings featureSettings
            ,IAuroraMySqlService auroraMySqlService)
        {
            _logger = logger;
            _configuration = configuration;
            _tokenService = tokenService;
            _dataVaultService = dataVaultService;
            _persistentTokenByAccountNumberTokenService = persistentTokenByAccountNumberTokenService;
            _payPalCreditCardService = payPalCreditCardService;
            _masterPassSessionDetailsService = masterPassSessionDetailsService;
            _mECSLFullfillmentService = mECSLFullfillmentService;
            _loyaltyUnitedClubIssuePassService = loyaltyUnitedClubIssuePassService;
            _cachingService = CachingService;
            _dynamoDBService = dynamoDBService;
            _cSLStatisticsService = cSLStatisticsService;
            _utility = new Utility(_configuration);
            _merchService = new MerchandizingServices(_configuration);
            new ConfigUtility(_configuration);
            string isEncrypted = _configuration.GetValue<string>("IsPCIEncryptionEnabledinProd");
            _cSLStatistics = new CSLStatistics(_logger, _configuration, _dynamoDBService, _cSLStatisticsService);
            _paymentDataAccessService = paymentDataAccessService;
            _featureSettings = featureSettings;
            _auroraMySqlService=auroraMySqlService;
        }
        #region"PurchaseOTPPasses"
        public async Task<OTPPurchaseResponse> PurchaseOTPPasses(OTPPurchaseRequest OTPrequest)
        {
            OTPPurchaseResponse response = new OTPPurchaseResponse();
            request = OTPrequest;
            Session session = new Session();
            string sessionId = request.SessionId;
            response.TransactionId = request.TransactionId;
            response.LanguageCode = request.LanguageCode;
            if (string.IsNullOrEmpty(request.SessionId))
            {
                sessionId = Guid.NewGuid().ToString().ToUpper().Replace("-", "");
                var expiry = TimeSpan.FromSeconds(_configuration.GetValue<double>("ucpGeoAirportCachedExp"));
            }
            response.sessionId = sessionId;
            #region Remove Encrypted CardNumber from request while logging (Change from Security Complaince team)
            OTPPurchaseRequest clonedRequest = request.Clone();
            ConfigUtility.RemoveEncyptedCreditcardNumberfromOTPRequest(clonedRequest?.CreditCard);
            #endregion

            if (request.CreditCard != null && !string.IsNullOrEmpty(request.CreditCard.EncryptedCardNumber))
            {
                VormetricUtility vormetricUtility = null;
                string token = string.Empty;
                CslDataVaultResponse vaultResponse = new CslDataVaultResponse();
                if (_configuration.GetValue<bool>("EnablePKDispenserKeyRotationAndOAEPPadding"))
                    vormetricUtility = new VormetricUtility(_logger, request, token, sessionId, _configuration, _dataVaultService, _cachingService);

                bool generatedCCTokenWithDataVault = false;
                string authToken = await _tokenService.GetAnonymousToken(request.Application.Id, request.DeviceId, _configuration);
                //AssignDataVaultAndPersistTokenToCC logic starts from here.
                bool isPersistAssigned = _configuration.GetValue<bool>("VormetricTokenMigration");
                string ccDataVaultToken = string.Empty;
                if (isPersistAssigned)
                {
                    if (!string.IsNullOrEmpty(request.CreditCard.EncryptedCardNumber)) // expecting Client will send only Encrypted Card Number only if the user input is a clear text CC number either for insert CC or update CC not the CC details like CVV number update or expiration date upate no need to call data vault for this type of updates only data vault will be called for CC number update to get the CC token back
                    {
                        Profile profile = new Profile(_configuration, _dataVaultService, _cachingService, _logger);
                        CslDataVaultRequest dataVaultRequest = profile.GetDataValutRequest(request.CreditCard, sessionId, request);
                        string jsonRequest = _utility.JsonSerialize<CslDataVaultRequest>(dataVaultRequest);
                        var jsonResponse = await _dataVaultService.GetCCTokenWithDataVault(authToken, jsonRequest, sessionId);
                        if (!string.IsNullOrEmpty(jsonResponse))
                        {
                            vaultResponse = _utility.DeserializeJsonDataContract<CslDataVaultResponse>(jsonResponse);
                            SetCreditCardRequestForPersistAssigned(vaultResponse, request);
                        }
                        else
                        {
                            string exceptionMessage = _configuration.GetValue<string>("UnableToInsertCreditCardToProfileErrorMessage");
                            if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                            {
                                exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  GenerateCCTokenWithDataVault(MOBUpdateTravelerRequest request, ref string ccDataVaultToken)";
                            }
                            throw new MOBUnitedException(exceptionMessage);
                        }

                    }
                    else if (!string.IsNullOrEmpty(request.CreditCard.AccountNumberToken.Trim()))
                    {
                        generatedCCTokenWithDataVault = true;
                    }

                    if (generatedCCTokenWithDataVault)
                    {
                        if (!string.IsNullOrEmpty(request.CreditCard.PersistentToken))
                        {
                            if (request.CreditCard.UnencryptedCardNumber != null && request.CreditCard.UnencryptedCardNumber.Length > 4)
                            {
                                request.CreditCard.UnencryptedCardNumber = "XXXXXXXXXXXX" + request.CreditCard.UnencryptedCardNumber.Substring((request.CreditCard.UnencryptedCardNumber.Length - 4), 4);
                            }
                            else
                            {
                                request.CreditCard.UnencryptedCardNumber = "XXXXXXXXXXXX";
                            }
                        }
                        else if (String.IsNullOrEmpty(request.CreditCard.AccountNumberToken) && !string.IsNullOrEmpty(authToken) && !string.IsNullOrEmpty(sessionId))
                        {
                            VormetricKeys vormetricKeys = await GetPersistentTokenUsingAccountNumberToken(request.CreditCard.AccountNumberToken, sessionId, authToken, request.TransactionId);
                            request.CreditCard.PersistentToken = vormetricKeys.PersistentToken;
                            request.CreditCard.SecurityCodeToken = vormetricKeys.SecurityCodeToken;
                            request.CreditCard.CardType = vormetricKeys.CardType;
                        }
                        else
                        {
                            _logger.LogInformation("PERSISTENTTOKENNOTFOUND - Unable to retieve PersistentToken {transactionId} ", sessionId);
                        }
                    }
                }
                if (!isPersistAssigned)
                {
                    if (!string.IsNullOrEmpty(request.CreditCard.EncryptedCardNumber)) // expecting Client will send only Encrypted Card Number only if the user input is a clear text CC number either for insert CC or update CC not the CC details like CVV number update or expiration date upate no need to call data vault for this type of updates only data vault will be called for CC number update to get the CC token back
                    {
                        Profile profile = new Profile(_configuration, _dataVaultService, _cachingService, _logger);
                        CslDataVaultRequest dataVaultRequest = profile.GetDataValutRequest(request.CreditCard, sessionId, request);
                        string jsonRequest = _utility.JsonSerialize<CslDataVaultRequest>(dataVaultRequest);
                        var jsonResponse = await _dataVaultService.GetCCTokenWithDataVault(authToken, jsonRequest, sessionId);
                        if (!string.IsNullOrEmpty(jsonResponse))
                        {
                            CslDataVaultResponse valultResponse = _utility.DeserializeJsonDataContract<CslDataVaultResponse>(jsonResponse);//Newtonsoft.Json.JsonConvert.DeserializeObject<CslDataVaultResponse>(jsonResponse);
                            SetCreditCardRequestForNotPersistAssigned(valultResponse, request, sessionId);
                        }
                        else
                        {
                            string exceptionMessage = _configuration.GetValue<string>("UnableToInsertCreditCardToProfileErrorMessage");
                            if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                            {
                                exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  GenerateCCTokenWithDataVault(MOBUpdateTravelerRequest request, ref string ccDataVaultToken)";
                            }
                            throw new MOBUnitedException(exceptionMessage);
                        }

                    }
                    if (request.CreditCard.UnencryptedCardNumber != null)
                    {
                        request.CreditCard.UnencryptedCardNumber = "XXXXXXXXXXXX" + request.CreditCard.UnencryptedCardNumber.Substring((request.CreditCard.UnencryptedCardNumber.Length - 4), 4);
                    }
                }
                string msg = string.Empty;
                int numOfPasses = request.NumberOfPasses;
                double amountPaid = GetOTPPriceForApplication(1, sessionId);
                string[] names = request.CreditCard.CCName.Split(' ');
                string firstName = string.Empty; string lastName = string.Empty;
                switch (names.Length)
                {
                    #region
                    case 1:
                        firstName = names[0];
                        break;
                    case 2:
                        firstName = names[0];
                        lastName = names[1];
                        break;
                    case 3:
                        firstName = names[0];
                        lastName = names[2];
                        break;
                    default:
                        firstName = names[0];
                        lastName = names[names.Length - 1];
                        break;
                        #endregion
                }
                response.passes = new List<ClubDayPass>();
                bool getClubPassFromLoyaltyOTP = _configuration.GetValue<bool>("LoyaltyOTPServiceSwitchONOFF");
                VormetricKeys vormetricKeysSet = new VormetricKeys();

                SetVormetricKeysSetFromDataVaultResponse(vaultResponse, vormetricKeysSet);

                if (_configuration.GetValue<bool>("EnablePassOTPSecurityCodeToken") &&
                    (string.IsNullOrEmpty(request.CreditCard.PersistentToken) || string.IsNullOrEmpty(request.CreditCard.SecurityCodeToken) || string.IsNullOrEmpty(request.CreditCard.CardType)))
                {
                    vormetricKeysSet = await GetPersistentTokenUsingAccountNumberToken(request.CreditCard.AccountNumberToken, sessionId, authToken, request.TransactionId);
                }
                for (int i = 0; i < numOfPasses; i++)
                {
                    #region
                    Stopwatch cslStopWatch;
                    cslStopWatch = new Stopwatch();
                    string eddId = string.Empty;
                    string eddInternalID = string.Empty;
                    ClubDayPass pass = new ClubDayPass();
                    PayPalPayor payPalPayor = null;
                    Reservation reservationForMasterpass = null;
                    United.Service.Presentation.PaymentModel.CreditCard pCreditcard = null;
                    if (request.FormOfPayment == FormofPayment.PayPal || request.FormOfPayment == FormofPayment.PayPalCredit)
                    {
                        Reservation reservationForPayPalPayor = new Reservation();
                        reservationForPayPalPayor = await _cachingService.GetDocument<Reservation>(sessionId, reservationForPayPalPayor.ObjectName);
                        if (reservationForPayPalPayor == null || reservationForPayPalPayor.PayPalPayor == null)
                        {
                            payPalPayor = await GetPayPalCreditCardResponse(request.PayPal, amountPaid, sessionId, request.Application.Id.ToString(), authToken, request.TransactionId);
                        }
                    }
                    else if (request.FormOfPayment == FormofPayment.Masterpass)
                    {
                        reservationForMasterpass = new Reservation();
                        reservationForMasterpass = await _cachingService.GetDocument<Reservation>(sessionId, reservationForMasterpass.ObjectName);
                        reservationForMasterpass = new Reservation();
                        if (reservationForMasterpass == null || reservationForMasterpass.MasterpassSessionDetails == null)
                        {
                            if (reservationForMasterpass == null)
                                reservationForMasterpass = new Reservation();
                            reservationForMasterpass.MasterpassSessionDetails = await GetMasterPassSessionDetails(request.Masterpass, session, authToken, vormetricKeysSet);
                        }
                    }
                    else if (request.FormOfPayment == FormofPayment.ApplePay)
                    {
                        pCreditcard = await _cachingService.GetDocument<United.Service.Presentation.PaymentModel.CreditCard>(sessionId, "United.Service.Presentation.PaymentModel.CreditCard");
                        if (pCreditcard == null)
                        {
                            Application application = new Application();
                            pCreditcard = new Service.Presentation.PaymentModel.CreditCard();
                            pCreditcard = ApplePayLoad(request.ApplePayInfo, amountPaid, sessionId, application, request.DeviceId, request.TransactionId);
                            var expiry = TimeSpan.FromSeconds(_configuration.GetValue<double>("ucpGeoAirportCachedExp"));
                            await _cachingService.SaveDocument<United.Service.Presentation.PaymentModel.CreditCard>(sessionId, pCreditcard, DateTime.Now.Ticks, expiry);
                        }
                    }
                    ProductPurchaseRequest offerRequest = _merchService.GetOfferRequest(request, firstName, lastName, amountPaid, vormetricKeysSet, payPalPayor, reservationForMasterpass, pCreditcard, false);
                    string jsonRequest = _utility.JsonSerialize<United.Service.Presentation.ProductRequestModel.ProductPurchaseRequest>(offerRequest);
                    string jsonResponse = await _mECSLFullfillmentService.GetMECSLFullfillment(authToken, jsonRequest, sessionId);
                    if (!string.IsNullOrEmpty(jsonResponse))
                    {
                        United.Service.Presentation.ProductResponseModel.ProductPurchaseResponse offers = Newtonsoft.Json.JsonConvert.DeserializeObject<United.Service.Presentation.ProductResponseModel.ProductPurchaseResponse>(jsonResponse);
                        if (offers != null && offers.Confirmations != null)
                        {
                            eddInternalID = offers.Confirmations[0].EDDInternalID;
                        }
                        else
                        {
                            throw new MOBUnitedException("EDD payment failed.");
                        }
                    }
                    else
                    {
                        throw new MOBUnitedException("EDD payment failed.");
                    }
                    string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
                    _logger.LogInformation("PurchaseOTPPasses {cslCallTime} and {transactionId}", cslCallTime, OTPrequest.TransactionId);

                    if (getClubPassFromLoyaltyOTP)
                    {
                        pass = await GetLoyaltyUnitedClubIssuePass(request.MileagePlusNumber, sessionId, request.Application.Id, request.Application.Version.Major.ToString(), request.DeviceId, eddInternalID, firstName, lastName, request.CreditCard.CCName, request.Email, request.MileagePlusNumber, "", "IOS", amountPaid.ToString(), string.Empty, authToken, request.TransactionId);
                    }

                    if (_configuration.GetValue<bool>("Log_CSL_Call_Statistics"))
                    {
                        await _cSLStatistics.AddCSLCallStatisticsDetails(string.Empty, "OTP_Purhcase", string.Empty, "GetClubPassCode=" + cslCallTime, "UnitedClubController/GetPKDispenserPublicKey", response.sessionId);
                    }

                    if (pass != null)
                    {
                        response.passes.Add(pass);
                    }
                    #endregion
                }

                if (response.passes.Count > 0)
                {
                    _utility.SendClubPassReceipt(response.passes, string.Empty);
                    string xmlRemark = JsonConvert.SerializeObject(response);
                 

                    if (await _featureSettings.GetFeatureSettingValue("EnableMySqlPaymentTable").ConfigureAwait(false))
                    {
                        PaymentDB payment = new PaymentDB
                        {
                            TransactionId = request.TransactionId,
                            ApplicationId = request.Application.Id,
                            ApplicationVersion = request.Application.Version.Major.ToString(),
                            PaymentType = "OTP Purchase using datavault, ME and OTP Service",
                            Amount = (amountPaid * request.NumberOfPasses),
                            CurrencyCode = "USD",
                            Mileage = 0,
                            Remark = xmlRemark,
                            InsertBy = request.Application.Id.ToString(),
                            IsTest = _configuration.GetValue<bool>("IsBookingTest") ? "Y" : "N",
                            SessionId = sessionId,
                            DeviceId = request.DeviceId,
                            RecordLocator = _configuration.GetValue<string>("LogExceptionOnly") != null ? _configuration.GetValue<string>("RESTWEBAPIVersion") : string.Empty,
                            MileagePlusNumber = request.MileagePlusNumber,
                            FormOfPayment = "CreditCard",
                        };
                        await _auroraMySqlService.InsertpaymentRecord(payment).ConfigureAwait(false);
                    }
                    else
                    {
                        Payment payment = new Payment
                        {
                            TransactionId = request.TransactionId,
                            ApplicationId = request.Application.Id.ToString(),
                            ApplicationVersion = request.Application.Version.Major.ToString(),
                            PaymentType = "OTP Purchase using datavault, ME and OTP Service",
                            Amount = (amountPaid * request.NumberOfPasses),
                            CurrencyCode = "USD",
                            Mileage = 0,
                            Remark = xmlRemark,
                            InsertBy = request.Application.Id.ToString(),
                            IsTest = _configuration.GetValue<bool>("IsBookingTest") ? "Y" : "N",
                            SessionId = sessionId,
                            DeviceId = request.DeviceId,
                            RecordLocator = _configuration.GetValue<string>("LogExceptionOnly") != null ? _configuration.GetValue<string>("RESTWEBAPIVersion") : string.Empty,
                            MileagePlusNumber = request.MileagePlusNumber,
                            FormOfPayment = "CreditCard",
                        };
                        string tableName = _configuration.GetSection("DynamoDBTables").GetValue<string>("uatb-Payment");
                        string key;
                        if (!string.IsNullOrEmpty(request.TransactionId))
                            key = request.TransactionId + "::" + request.Application.Id.ToString() + "::" + request.Application.Version.Major.ToString();
                        else
                            key = request.Application.Id.ToString() + "::" + request.Application.Version.Major.ToString();

                        string transId = string.IsNullOrEmpty(OTPrequest.TransactionId) ? "trans0" : OTPrequest.TransactionId;

                        var resFromDBservice = await _dynamoDBService.SaveRecords<Payment>(tableName, transId, key, payment, sessionId);
                        //SQL DB Service call 
                        if (_configuration.GetValue<bool>("SendPayment"))
                        {
                            string jsonStrRequest = Newtonsoft.Json.JsonConvert.SerializeObject(payment);
                            try
                            {
                                var recCount = await _paymentDataAccessService.AddPaymentNew(string.Empty, jsonStrRequest, sessionId);
                                if (!string.IsNullOrEmpty(recCount) && recCount.Equals("-1"))
                                {
                                    _logger.LogInformation("OnPremSQLService-AddPaymentNew {recordCount} and {transactionId}", recCount, request.TransactionId);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("OnPremSQLService-AddPaymentNew Error {message} {exceptionStackTrace} and {transactionId}", ex.Message, ex.StackTrace, request.TransactionId);
                            }
                        }
                    }
                }
            }
            return response;
        }

        private United.Service.Presentation.PaymentModel.CreditCard ApplePayLoad(ApplePay applePayInfo, double paymentAmount, string sessionId, Application application, string deviceId, string transactionId)
        {
            _logger.LogInformation("ApplePayLoad {applePayInfo} and {transactionId}", applePayInfo.ApplePayLoadJSON, transactionId);

            ApplePayLoad applePayLoad = Newtonsoft.Json.JsonConvert.DeserializeObject<ApplePayLoad>(applePayInfo.ApplePayLoadJSON);// JsonSerializer.Deserialize<ApplePayLoad>(applePayInfo.ApplePayLoadJSON);

            var mobApplePayData = GetApplePayDecryptedData(application, sessionId, applePayLoad.Data, applePayLoad.Header.EphemeralPublicKey, deviceId);
            Service.Presentation.PaymentModel.CreditCard creditCard = new Service.Presentation.PaymentModel.CreditCard();
            creditCard.Amount = paymentAmount;
            creditCard.OperationID = sessionId;
            creditCard.WalletCategory = Service.Presentation.CommonEnumModel.WalletCategory.ApplePay;
            creditCard.Payor = new Service.Presentation.PersonModel.Person();
            creditCard.Name = GetCardHolderFullname(applePayInfo.CardHolderName);
            creditCard.Payor.GivenName = applePayInfo.CardHolderName.First;
            creditCard.Payor.Surname = applePayInfo.CardHolderName.Last;
            creditCard.AccountNumber = mobApplePayData.ApplicationPrimaryAccountNumber;
            creditCard.AccountNumberLastFourDigits = applePayInfo.LastFourDigits;
            creditCard.AccountNumberMasked = "*****" + applePayInfo.LastFourDigits;

            creditCard.BillingAddress = AssignCreditCardBillingAddress(applePayInfo.BillingAddress);
            AssignCSLCreditCardCode(applePayInfo, creditCard);

            creditCard.Currency = new Service.Presentation.CommonModel.Currency();
            creditCard.Currency.Code = applePayInfo.CurrencyCode;
            creditCard.EciIndicator = mobApplePayData.PaymentData.EciIndicator;
            creditCard.ExpirationDate = DateTime.ParseExact(mobApplePayData.ApplicationExpirationDate, "yyMMdd", CultureInfo.InvariantCulture).ToString("MM/yy");
            creditCard.OnlinePaymentCryptogram = mobApplePayData.PaymentData.OnlinePaymentCryptogram; // " PaymentCryptogram ": "AnQeed0ACbTkwQRZF0hUMAACAAA=",
            return creditCard;
        }
        private Service.Presentation.CommonModel.Address AssignCreditCardBillingAddress(MOBAddress billingAddress)
        {
            Service.Presentation.CommonModel.Address address = null;
            if (billingAddress != null)
            {
                address = new Service.Presentation.CommonModel.Address();
                var addressLines = new Collection<string>();
                AddAddressLinesToCslBillingAddress(billingAddress.Line1, ref addressLines);
                AddAddressLinesToCslBillingAddress(billingAddress.Line2, ref addressLines);
                AddAddressLinesToCslBillingAddress(billingAddress.Line3, ref addressLines);
                address.AddressLines = addressLines;
                address.City = billingAddress.City;
                if (billingAddress.State != null)
                {
                    address.StateProvince = new Service.Presentation.CommonModel.StateProvince();
                    address.StateProvince.StateProvinceCode = !string.IsNullOrEmpty(billingAddress.State.Code) ? billingAddress.State.Code : billingAddress.State.Name;
                    address.StateProvince.Name = billingAddress.State.Name;
                    address.StateProvince.ShortName = billingAddress.State.Name;
                }
                if (billingAddress.Country != null)
                {
                    address.Country = new Service.Presentation.CommonModel.Country();
                    address.Country.CountryCode = billingAddress.Country.Code.ToUpper();
                }
                address.PostalCode = billingAddress.PostalCode;
            }
            return address;
        }
        private Collection<string> AddAddressLinesToCslBillingAddress(string line, ref Collection<string> lines)
        {
            if (!string.IsNullOrEmpty(line))
            {
                var linesAfterSplit = line.Replace("\r\n", "|").Replace("\n", "|").Split('|').ToCollection();
                lines = lines.Concat(linesAfterSplit).ToCollection();
            }
            return lines;
        }
        private static void AssignCSLCreditCardCode(ApplePay mobApplePay, United.Service.Presentation.PaymentModel.CreditCard creditCard)
        {
            Dictionary<string, string> dictPaymentTypes = new Dictionary<string, string>();
            dictPaymentTypes.Add("VISA", "VI");
            dictPaymentTypes.Add("AMEX", "AX");
            dictPaymentTypes.Add("DISCOVER", "DS");
            dictPaymentTypes.Add("MASTERCARD", "MC");
            dictPaymentTypes.Add("AMERICANEXPRESS", "AX");
            dictPaymentTypes.Add("UNIONPAY", "UP");

            creditCard.Code = dictPaymentTypes[mobApplePay.CardName.ToUpper()];
        }
        private string GetCardHolderFullname(Name cardHolderName)
        {
            return string.Format("{0} {1} {2}", cardHolderName.First, cardHolderName.Middle, cardHolderName.Last).Replace("  ", " ").Trim();
        }
        private ApplePayData GetApplePayDecryptedData(Application application, string sessionId, string data, string base64Pubkey, string deviceID)
        {
            string certPath = _configuration.GetValue<string>("ApplePayCertPath");
            string certPassword = _configuration.GetValue<string>("ApplePayCertPassword") ?? "";
            ApplePayData mobApplePayData = null;
            var applePayInfo = new ApplePayInfo().Init(base64Pubkey, certPath, certPassword);
            var jsonStr = applePayInfo.DecryptedByAES256GCM(data);
            if (!String.IsNullOrEmpty(jsonStr))
                mobApplePayData = Newtonsoft.Json.JsonConvert.DeserializeObject<ApplePayData>(jsonStr);

            return mobApplePayData;
        }

        private async Task<ClubDayPass> GetLoyaltyUnitedClubIssuePass(string mileagePlusNumber, string sessionId, int applicationId, string appVersion, string deviceId, string edocId, string firstName, string lastName, string ccName, string email, string onePassNumber, string location, string deviceType, string amountPaid, string recordLocator, string authToken, string transactionId)
        {
            ClubDayPass mobClubDayPass = new ClubDayPass();
            OneTimePassRequest oneTimePassRequest = new OneTimePassRequest();
            oneTimePassRequest.PassRequest = new OneTimePassProfile();
            oneTimePassRequest.PassRequest.AcquisitionDate = DateTime.Now; //AcquisitionDate
            oneTimePassRequest.PassRequest.AmountPaid = amountPaid;
            oneTimePassRequest.PassRequest.CardholderName = ccName;
            oneTimePassRequest.PassRequest.Carrier = "";
            oneTimePassRequest.PassRequest.Compensation = "";
            oneTimePassRequest.PassRequest.CustomerMP = ""; //Customer MP is populated if purchasing on behalf of that customer 
            oneTimePassRequest.PassRequest.DateAdmitted = "";
            oneTimePassRequest.PassRequest.DateCreated = DateTime.Now;
            oneTimePassRequest.PassRequest.DeviceType = deviceType;
            oneTimePassRequest.PassRequest.EdocId = edocId;
            oneTimePassRequest.PassRequest.EffectiveDate = DateTime.Now;
            oneTimePassRequest.PassRequest.EmailAddress = email;
            oneTimePassRequest.PassRequest.ExpirationDate = DateTime.Now.AddYears(1);
            oneTimePassRequest.PassRequest.FirstName = firstName;
            oneTimePassRequest.PassRequest.FlightNumber = "";
            oneTimePassRequest.PassRequest.IsAgent = false;
            oneTimePassRequest.PassRequest.IsSendEmail = true;
            oneTimePassRequest.PassRequest.IsTest = false;
            oneTimePassRequest.PassRequest.LastName = lastName;
            oneTimePassRequest.PassRequest.Location = "";
            oneTimePassRequest.PassRequest.MPAccount = mileagePlusNumber;
            oneTimePassRequest.PassRequest.Notes = "";
            oneTimePassRequest.PassRequest.PassReason = Service.Presentation.CommonEnumModel.Reason.Purchased;
            oneTimePassRequest.PassRequest.PassSource = Service.Presentation.CommonEnumModel.PassType.Mobile;
            oneTimePassRequest.PassRequest.ProgramId = 0;
            oneTimePassRequest.PassRequest.PromoCode = "";
            oneTimePassRequest.PassRequest.RecordLocator = recordLocator;
            oneTimePassRequest.PassRequest.RequestInfo = new Service.Presentation.LoyaltyModel.PassRequestInfo(); //right now passing null object as per OTP Service sample request
            oneTimePassRequest.PassRequest.Visitors = 1;
            oneTimePassRequest.PassRequest.WhoCreated = "";
            if (oneTimePassRequest == null)
            {
                throw new MOBUnitedException("GetLoyaltyUnitedClubIssuePass", "oneTimePassRequest cannot be null.");
            }
            if (oneTimePassRequest.PassRequest == null)
            {
                throw new MOBUnitedException("GetLoyaltyUnitedClubIssuePass", "oneTimePassRequest.PassReques cannot be null.");
            }
            string jsonRequest = Newtonsoft.Json.JsonConvert.SerializeObject(oneTimePassRequest);

            _logger.LogInformation("GetLoyaltyUnitedClubIssuePass {cslRequest} and {transactionId}", jsonRequest, transactionId);

            var jsonResponse = await _loyaltyUnitedClubIssuePassService.GetLoyaltyUnitedClubIssuePass(authToken, jsonRequest, sessionId);

            if (!string.IsNullOrEmpty(jsonResponse))
            {

                var oneTimePassResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<OneTimePassResponse>(jsonResponse);
                if (oneTimePassResponse != null && oneTimePassResponse.UnitedClubPass != null)
                {
                    _logger.LogInformation("GetLoyaltyUnitedClubIssuePass {cslResponse} and {transactionId}", oneTimePassResponse, transactionId);
                    string passCode = string.Empty;
                    foreach (United.Service.Presentation.LoyaltyModel.UnitedClubPass unitedClubPass in oneTimePassResponse.UnitedClubPass)
                    {

                        mobClubDayPass.clubPassCode = unitedClubPass.UnitedClubPassCode;
                        mobClubDayPass.electronicClubPassesType = "PurchasedOTP";
                        mobClubDayPass.email = unitedClubPass.EmailAddress;
                        mobClubDayPass.expirationDate = unitedClubPass.ExpirationDate.ToString("MMMM dd, yyyy");
                        mobClubDayPass.firstName = unitedClubPass.FirstName;
                        mobClubDayPass.lastName = unitedClubPass.LastName;
                        mobClubDayPass.mileagePlusNumber = unitedClubPass.MPAccount;
                        mobClubDayPass.passCode = unitedClubPass.UnitedClubPassCode;
                        string[] result = unitedClubPass.UnitedClubPassCode.Split('|');
                        mobClubDayPass.passCode = result[1];
                        mobClubDayPass.paymentAmount = unitedClubPass.AmountPaid;
                        mobClubDayPass.purchaseDate = unitedClubPass.AcquiredDate.ToString("MMMM dd, yyyy");
                        mobClubDayPass.barCode = _utility.GetBarCode(unitedClubPass.UnitedClubPassCode);

                        if (!_configuration.GetValue<bool>("disableSendingOnePassExpirationDateTime")) //Set disableSendingOnePassExpirationDateTime key in appsettings to turn off this feature
                            mobClubDayPass.expirationDateTime = unitedClubPass.ExpirationDate.Date.AddDays(1).AddSeconds(-1).ToString("MMMM dd, yyyy hh:mm:ss tt");
                    }
                }
            }
            return mobClubDayPass;
        }

        private async Task<MasterpassSessionDetails> GetMasterPassSessionDetails(Masterpass masterpass, Session session, string authToken, VormetricKeys vormetricKeys)
        {
            MasterpassSessionDetails response = new MasterpassSessionDetails();
            string token = authToken;
            string url = string.Format(_configuration.GetValue<string>("AcquireMasterpassToken-ShoppingCartURL"), "GetMasterPassSessionDetails");
            United.Service.Presentation.PaymentModel.Wallet wallet = BuildWalletObject(masterpass, session.CartId);
            string jsonRequest = Newtonsoft.Json.JsonConvert.SerializeObject(wallet);
            string jsonResponse;
            jsonResponse = await _masterPassSessionDetailsService.GetMasterPassSessionDetails(authToken, jsonRequest, session.SessionId);
            response = DeserialiseAndBuildMOBMasterpassSessionDetails(jsonResponse, vormetricKeys);
            return response;
        }
        private MasterpassSessionDetails DeserialiseAndBuildMOBMasterpassSessionDetails(string jsonResponse, VormetricKeys vormetricKeys)
        {
            MasterpassSessionDetails response = new MasterpassSessionDetails();

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                var walletSessionResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<United.Service.Presentation.PaymentResponseModel.WalletSessionResponse>(jsonResponse);
                if (walletSessionResponse != null && walletSessionResponse.Response != null)
                {
                    var payment = walletSessionResponse.Payment;
                    Service.Presentation.PaymentModel.CreditCard cc = (Service.Presentation.PaymentModel.CreditCard)walletSessionResponse.Payment;
                    response.AccountNumber = payment.AccountNumber;
                    response.AccountNumberEncrypted = payment.AccountNumberEncrypted;
                    response.AccountNumberHMAC = payment.AccountNumberHMAC;
                    response.AccountNumberLastFourDigits = payment.AccountNumberLastFourDigits;
                    response.AccountNumberMasked = payment.AccountNumberMasked;
                    response.AccountNumberToken = payment.AccountNumberToken;
                    //MOBILE-1218 Booking - Checkout using MasterPass
                    if (!string.IsNullOrEmpty(vormetricKeys.PersistentToken))
                    {
                        response.PersistentToken = vormetricKeys.PersistentToken;
                    }
                    if (!string.IsNullOrEmpty(vormetricKeys.CardType) && string.IsNullOrEmpty(cc.Code))
                    {
                        cc.Code = vormetricKeys.CardType;
                    }

                    response.ExpirationDate = cc.ExpirationDate;
                    response.Amount = payment.Amount;
                    response.OperationID = payment.OperationID;
                    response.GivenName = payment.Payor.GivenName;
                    response.SurName = payment.Payor.Surname;
                    response.Code = cc.Code;
                    response.CreditCardTypeCode = Convert.ToInt32(cc.CreditCardTypeCode);
                    response.Description = Convert.ToInt32(cc.Description);
                    response.Name = cc.Name;
                    bool masterPassCheckCountryNameToggle = _configuration.GetValue<bool>("MasterPassCheckCountryNameToggle");
                    if (masterPassCheckCountryNameToggle &&
                        payment.BillingAddress != null &&
                        payment.BillingAddress.Country != null)
                    {
                        payment.BillingAddress.Country.CountryCode = string.Empty;
                    }
                    response.BillingAddress = ConvertCslBillingAddressToAddress(payment.BillingAddress, FormofPayment.Masterpass);
                    response.ContactPhoneNumber =
                        GetKeyValueFromAddressCharacteristicCollection(payment.BillingAddress, "PHONE");
                    response.ContactEmailAddress =
                        GetKeyValueFromAddressCharacteristicCollection(payment.BillingAddress, "EMAIL");
                    response.MasterpassType = new MasterpassType();
                    response.MasterpassType.DefaultIndicator = payment.Type.DefaultIndicator;
                    response.MasterpassType.Description = payment.Type.Description;
                    response.MasterpassType.Key = payment.Type.Key;
                    response.MasterpassType.Val = payment.Type.Value;
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            return response;
        }
        private United.Service.Presentation.PaymentModel.Wallet BuildWalletObject(Masterpass masterpass, string cartId)
        {
            United.Service.Presentation.PaymentModel.Wallet wallet = new Service.Presentation.PaymentModel.Wallet();
            wallet.CartID = cartId;
            wallet.CheckoutURL = masterpass.CheckoutResourceURL;
            wallet.OathToken = masterpass.OauthToken;
            wallet.OathVerifier = masterpass.Oauth_verifier;
            wallet.PointOfSale = _configuration.GetValue<string>("AcquireMasterPassToken-PointOfSale");
            wallet.WalletType = "MPS";
            wallet.SessionID = masterpass.CslSessionId;
            wallet.Version = "1.0";
            return wallet;
        }
        private async Task<PayPalPayor> GetPayPalCreditCardResponse(PayPal payPal, double amount, string sessionId, string appVersion, string token, string transactionId)
        {
            PayPalPayor Response = new PayPalPayor();
            PayPalCreditCardResponse payPalCreditCardResponse = null;
            PayPalCreditCardRequest request = new PayPalCreditCardRequest();
            request.Request = new Service.Presentation.PaymentModel.PayPal();
            request.Request.Amount = amount;
            request.Request.BillingAddress = new Service.Presentation.CommonModel.Address();
            request.Request.BillingAddress.Country = new Service.Presentation.CommonModel.Country() { CountryCode = payPal.BillingAddressCountryCode }; // Make sure the country code here for AcquirePayPalCreditCard is the same from Billing address counrty code or some thing differenct.
            request.Request.PayerID = payPal.PayerID;
            request.Request.TokenID = payPal.PayPalTokenID;
            #region
            string jsonRequest = Newtonsoft.Json.JsonConvert.SerializeObject(request);

            _logger.LogInformation("GetPayPalCreditCardResponse {cslRequest} and {transactionId}", jsonRequest, transactionId);

            string paypalCSLCallDurations = string.Empty;
            string callTime4Tuning = string.Empty;
            #region//****Get Call Duration Code*******

            Stopwatch paypalCSLCallDurationstopwatch1;
            paypalCSLCallDurationstopwatch1 = new Stopwatch();
            paypalCSLCallDurationstopwatch1.Reset();
            paypalCSLCallDurationstopwatch1.Start();
            #endregion
            string jsonResponse;
            jsonResponse = await _payPalCreditCardService.GetPayPalCreditCardResponse(token, jsonRequest, string.Empty);
            #region
            if (paypalCSLCallDurationstopwatch1.IsRunning)
            {
                paypalCSLCallDurationstopwatch1.Stop();
            }
            paypalCSLCallDurations = paypalCSLCallDurations + "|2=" + paypalCSLCallDurationstopwatch1.ElapsedMilliseconds.ToString() + "|";
            callTime4Tuning = "|CSL =" + (paypalCSLCallDurationstopwatch1.ElapsedMilliseconds / (double)1000).ToString();
            #endregion

            _logger.LogInformation("GetPayPalCreditCardResponse {cslResponse} and {transactionId}", jsonResponse, transactionId);
            if (!string.IsNullOrEmpty(jsonResponse))
            {
                #region
                payPalCreditCardResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<PayPalCreditCardResponse>(jsonResponse);
                if (payPalCreditCardResponse != null && payPalCreditCardResponse.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success))
                {
                    #region Populate values from paypal creditcard response

                    Response.PayPalContactEmailAddress = payPalCreditCardResponse.Response.Payor.Contact.Emails[0].Address; //**TBD-paypal**//

                    Response.PayPalCustomerID = payPalCreditCardResponse.Response.Payor.CustomerID;
                    Response.PayPalGivenName = payPalCreditCardResponse.Response.Payor.GivenName;
                    Response.PayPalSurName = payPalCreditCardResponse.Response.Payor.Surname;
                    Response.PayPalStatus = payPalCreditCardResponse.Response.Payor.Status.Description; //**TBD-paypal**//

                    Response.PayPalContactPhoneNumber = GetKeyValueFromAddressCharacteristicCollection(payPalCreditCardResponse.Response.BillingAddress, "PHONE");
                    Response.PayPalBillingAddress = ConvertCslBillingAddressToAddress(payPalCreditCardResponse.Response.BillingAddress, FormofPayment.PayPal);
                    #endregion 
                }
                else
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                #endregion
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            #endregion

            return Response;
        }
        private MOBAddress ConvertCslBillingAddressToAddress(United.Service.Presentation.CommonModel.Address cslAddress, FormofPayment fop)
        {
            MOBAddress mobAddress = new MOBAddress();
            if (cslAddress != null)
            {
                foreach (string addressLines in cslAddress.AddressLines)
                {
                    if (string.IsNullOrEmpty(mobAddress.Line1))
                    {
                        mobAddress.Line1 = addressLines;
                    }
                    else if (string.IsNullOrEmpty(mobAddress.Line2))
                    {
                        mobAddress.Line2 = addressLines;
                    }
                    else if (string.IsNullOrEmpty(mobAddress.Line3))
                    {
                        mobAddress.Line3 = addressLines;
                    }
                    else
                    {
                        mobAddress.Line3 = mobAddress.Line3 + addressLines;
                    }
                }
                mobAddress.Country = new MOBCountry();
                mobAddress.Country.Code = cslAddress.Country.CountryCode;
                mobAddress.Country.Name = cslAddress.Country.Name;
                mobAddress.City = cslAddress.City;
                mobAddress.PostalCode = cslAddress.PostalCode;
                mobAddress.State = new State();
                if (fop == FormofPayment.Masterpass)
                {
                    mobAddress.State.Code = !string.IsNullOrEmpty(cslAddress.StateProvince.Name) ? cslAddress.StateProvince.Name.ToUpper().Replace("US-", "") : (cslAddress.StateProvince.Name ?? "");
                    mobAddress.State.Name = (cslAddress.StateProvince.Name ?? "").ToUpper();
                }
                else
                {
                    mobAddress.State.Code = cslAddress.StateProvince.ShortName;
                    mobAddress.State.Name = cslAddress.StateProvince.StateProvinceCode;
                }
                bool payPalBillingCountryNotUsaMessageToggle = _configuration.GetValue<bool>("PayPalBillingCountryNotUSAMessageToggle");
                if (payPalBillingCountryNotUsaMessageToggle && !_utility.IsUSACountryAddress(mobAddress.Country))
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("PayPalBillingCountryNotUSAMessage"));
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            return mobAddress;
        }
        private static string GetKeyValueFromAddressCharacteristicCollection(Service.Presentation.CommonModel.Address address, string key)
        {
            string phoneNumber = string.Empty;
            if (address != null &&
                address.Characteristic != null &&
                address.Characteristic.Count > 0)
            {
                foreach (var characterstic in address.Characteristic)
                {
                    if (characterstic != null)
                    {
                        if (((characterstic.Description ?? "").ToUpper() == key || (characterstic.Code ?? "").ToUpper() == key) &&
                             key == "PHONE" &&
                             !string.IsNullOrEmpty(characterstic.Value))
                        {
                            phoneNumber = characterstic.Value.Replace("-", "");
                        }
                        else if ((characterstic.Description ?? "").ToUpper() == key || (characterstic.Code ?? "").ToUpper() == key)
                        {
                            phoneNumber = characterstic.Value;
                        }
                    }
                }
            }
            return phoneNumber;
        }

        private void SetCreditCardRequestForPersistAssigned(CslDataVaultResponse valultResponse, OTPPurchaseRequest request)
        {
            if (valultResponse != null && valultResponse.Responses != null && valultResponse.Responses[0].Error == null && valultResponse.Responses[0].Message != null && valultResponse.Responses[0].Message.Count > 0 && valultResponse.Responses[0].Message[0].Code.Trim() == "0")
            {
                var creditCard = valultResponse.Items[0] as Service.Presentation.PaymentModel.CreditCard;
                request.CreditCard.AccountNumberToken = creditCard.AccountNumberToken;
                request.CreditCard.PersistentToken = creditCard.PersistentToken;
                request.CreditCard.SecurityCodeToken = creditCard.SecurityCodeToken;
                if (String.IsNullOrEmpty(request.CreditCard.CardType))
                {
                    request.CreditCard.CardType = creditCard.Code;
                }
                else if (!_configuration.GetValue<bool>("DisableCheckForUnionPayFOP_MOBILE13762"))
                {
                    string[] checkForUnionPayFOP = _configuration.GetValue<string>("CheckForUnionPayFOP")?.Split('|');
                    if (creditCard?.Code == checkForUnionPayFOP?[0])
                    {
                        request.CreditCard.CardType = creditCard.Code;
                        request.CreditCard.CardTypeDescription = checkForUnionPayFOP?[1];
                    }
                }
            }
            else
            {
                if (valultResponse.Responses[0].Error != null && valultResponse.Responses[0].Error.Count > 0)
                {
                    string errorMessage = string.Empty;
                    foreach (var error in valultResponse.Responses[0].Error)
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
        private void SetCreditCardRequestForNotPersistAssigned(CslDataVaultResponse valultResponse, OTPPurchaseRequest request, string sessionId)
        {
            if (valultResponse != null && valultResponse.Responses != null && valultResponse.Responses[0].Error == null && valultResponse.Responses[0].Message != null && valultResponse.Responses[0].Message.Count > 0 && valultResponse.Responses[0].Message[0].Code.Trim() == "0")
            {
                if (_configuration.GetValue<bool>("VormetricTokenMigration") && String.IsNullOrEmpty(request.CreditCard.CardType))
                {
                    var creditCard = valultResponse.Items[0] as Service.Presentation.PaymentModel.CreditCard;
                    request.CreditCard.CardType = creditCard.Code;
                }
                else if (!_configuration.GetValue<bool>("DisableCheckForUnionPayFOP_MOBILE13762") && !string.IsNullOrEmpty(request.CreditCard?.CardType))
                {
                    var creditCard = valultResponse.Items[0] as Service.Presentation.PaymentModel.CreditCard;
                    string[] checkForUnionPayFOP = _configuration.GetValue<string>("CheckForUnionPayFOP")?.Split('|');
                    if (creditCard?.Code == checkForUnionPayFOP?[0])
                    {
                        request.CreditCard.CardType = creditCard.Code;
                        request.CreditCard.CardTypeDescription = checkForUnionPayFOP?[1];
                    }
                }
                foreach (var item in valultResponse.Items)
                {
                    request.CreditCard.AccountNumberToken = item.AccountNumberToken;
                    break;
                }

                _logger.LogInformation("SetCreditCardRequestForNotPersistAssigned CSLRequest:{@Request}", request);
            }
            else
            {
                if (valultResponse.Responses[0].Error != null && valultResponse.Responses[0].Error.Count > 0)
                {
                    string errorMessage = string.Empty;
                    string errorCode = string.Empty;

                    if (_configuration.GetValue<bool>("EnableBacklogIssueFixes"))
                    {
                        string[] dvErrorCodeAndMessages = _configuration.GetValue<string>("HandleDataVaultErrorCodeAndMessages").Split('|');
                        foreach (var error in valultResponse.Responses[0].Error)
                        {
                            if (dvErrorCodeAndMessages != null)
                            {
                                string errorTextFromConfig = dvErrorCodeAndMessages.Any(row => row.Contains(error.Code)) ?
                                        dvErrorCodeAndMessages[Array.FindIndex(dvErrorCodeAndMessages, row => row.Contains(error.Code))].Split('=')[1] :
                                        error.Text;
                                errorMessage = errorMessage + " " + errorTextFromConfig;
                                errorCode = error.Code;
                            }
                            else
                            {
                                errorMessage = errorMessage + " " + error.Text;
                            }
                        }

                    }
                    else
                    {
                        foreach (var error in valultResponse.Responses[0].Error)
                        {
                            errorMessage = errorMessage + " " + error.Text;
                        }
                    }
                    throw new MOBUnitedException(errorCode, errorMessage);
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
        private void SetVormetricKeysSetFromDataVaultResponse(CslDataVaultResponse valultResponse, VormetricKeys vormetricKeys)
        {
            if (valultResponse != null && valultResponse.Responses != null && valultResponse.Responses[0].Error == null && valultResponse.Responses[0].Message != null && valultResponse.Responses[0].Message.Count > 0 && valultResponse.Responses[0].Message[0].Code.Trim() == "0")
            {
                var creditCard = valultResponse.Items[0] as Service.Presentation.PaymentModel.CreditCard;
                vormetricKeys.PersistentToken = creditCard?.PersistentToken;
                vormetricKeys.SecurityCodeToken = creditCard?.SecurityCodeToken;
                vormetricKeys.CardType = creditCard?.Code;
            }
            else
            {
                if (valultResponse.Responses[0].Error != null && valultResponse.Responses[0].Error.Count > 0)
                {
                    string errorMessage = string.Empty;
                    foreach (var error in valultResponse.Responses[0].Error)
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
        private async Task<VormetricKeys> GetPersistentTokenUsingAccountNumberToken(string accountNumberToken, string sessionId, string token, string transactionId)
        {

            var jsonResponse = await _persistentTokenByAccountNumberTokenService.GetPersistentTokenUsingAccountNumberToken(token, accountNumberToken, sessionId);

            _logger.LogInformation("GetPersistentTokenUsingAccountNumberToken {token} and {transactionId}", jsonResponse, transactionId);

            return GetPersistentTokenFromCSLDatavaultResponse(jsonResponse, sessionId, transactionId);
        }
        private VormetricKeys GetPersistentTokenFromCSLDatavaultResponse(string jsonResponseFromCSL, string sessionID, string transactionId)
        {
            VormetricKeys vormetricKeys = new VormetricKeys();
            if (!string.IsNullOrEmpty(jsonResponseFromCSL))
            {
                CslDataVaultResponse response = _utility.DeserializeJsonDataContract<CslDataVaultResponse>(jsonResponseFromCSL);
                if (response != null && response.Responses != null && response.Responses[0].Error == null && response.Responses[0].Message != null && response.Responses[0].Message.Count > 0 && response.Responses[0].Message[0].Code.Trim() == "0")
                {
                    var creditCard = response.Items[0] as Service.Presentation.PaymentModel.CreditCard;
                    vormetricKeys.PersistentToken = creditCard.PersistentToken;
                    vormetricKeys.SecurityCodeToken = creditCard.SecurityCodeToken;
                    vormetricKeys.CardType = creditCard.Code;

                    _logger.LogInformation("GetPersistentTokenFromCSLDatavaultResponse {cslResponse} and {transactionId}", response, transactionId);
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
        #endregion

        private double GetOTPPriceForApplication(int appID, string sessionId)
        {
            var catalogDynamoDB = new CatalogDynamoDB(_configuration, _dynamoDBService);
            double otpPrice = 59.0;
            try
            {
                var catalogItems = catalogDynamoDB.GetCatalogItems<MOBCatalogItem>("10010", sessionId).Result; // Get OTP Price of iOS app as the OTP price will be the same on all Apps
                if (catalogItems != null)
                {
                    double otpPrice1 = Convert.ToDouble(catalogItems.CurrentValue.Split('|')[0].ToString()); // 50|50
                    double otpPrice2 = Convert.ToDouble(catalogItems.CurrentValue.Split('|')[1].ToString()); // The price from Catalog will be like 50|50 we have to pick the least price for otp.
                    otpPrice = (otpPrice1 < otpPrice2) ? otpPrice1 : otpPrice2;
                    return otpPrice;
                }
                otpPrice = appID == 1 ? _configuration.GetValue<double>("iOSOTPPrice") : _configuration.GetValue<double>("AndroidOTPPrice");

            }
            catch (Exception ex)
            {
                string enterlog = string.IsNullOrEmpty(ex.StackTrace) ? ex.Message : ex.StackTrace;
                _logger.LogError("PurchaseOTPPasses-GetOTPPriceForApplication {@Exception}", enterlog);
            }
            return otpPrice;
        }
    }
}
