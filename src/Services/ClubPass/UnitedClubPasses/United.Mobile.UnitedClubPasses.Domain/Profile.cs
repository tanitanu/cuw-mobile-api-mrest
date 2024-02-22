using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using United.Common.Helper;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.Model;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.UnitedClubPasses;
using United.Service.Presentation.CommonModel;
using United.Services.Customer.Common;
using United.Utility.Helper;
using CslDataVaultRequest = United.Service.Presentation.PaymentRequestModel.DataVault<United.Service.Presentation.PaymentModel.Payment>;
using CslDataVaultResponse = United.Service.Presentation.PaymentResponseModel.DataVault<United.Service.Presentation.PaymentModel.Payment>;

namespace United.Mobile.UnitedClubPasses.Domain
{
    public class Profile
    {
        private readonly IConfiguration _configuration;
        private readonly Utility _utility;
        private readonly IDataVaultService _dataVaultService;
        private readonly ICachingService _cachingService;
        private readonly ICacheLog<PurchaseOTPPassesBusiness> _logger;
        public Profile(IConfiguration configuration, IDataVaultService dataVaultService, ICachingService cachingService, ICacheLog<PurchaseOTPPassesBusiness> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _utility = new Utility(_configuration);
            _dataVaultService = dataVaultService;
            _cachingService = cachingService;
        }

        public CslDataVaultRequest GetDataValutRequest(United.Mobile.Model.UnitedClubPasses.CreditCard creditCardDetails, string sessionID, OTPPurchaseRequest request)
        {
            #region
            var dataVaultRequest = new CslDataVaultRequest
            {
                Items = new System.Collections.ObjectModel.Collection<United.Service.Presentation.PaymentModel.Payment>(),
                Types = new System.Collections.ObjectModel.Collection<Characteristic>(),
                CallingService = new United.Service.Presentation.CommonModel.ServiceClient { Requestor = new Requestor { AgentAAA = "WEB", ApplicationSource = "mobile services" } }
            };
            InsertCreditCardRequest creditCardInsertRequest = new InsertCreditCardRequest();
            if (creditCardDetails != null)
            {
                var cc = new United.Service.Presentation.PaymentModel.CreditCard();
                cc.ExpirationDate = creditCardDetails.ExpireMonth + "/" + (creditCardDetails.ExpireYear.Trim().Length == 2 ? creditCardDetails.ExpireYear.Trim() : creditCardDetails.ExpireYear.Trim().Substring(2, 2).ToString()); //"05/17";
                cc.SecurityCode = creditCardDetails.CIDCVV2.Trim(); //"1234";
                cc.Code = creditCardDetails.CardType;  //"VI";
                cc.Name = creditCardDetails.CCName; //"Test Testing";
                if (!string.IsNullOrEmpty(creditCardDetails.EncryptedCardNumber))
                {
                    dataVaultRequest.Types = new System.Collections.ObjectModel.Collection<Characteristic>();
                    dataVaultRequest.Types.Add(new Characteristic { Code = "ENCRYPTION", Value = "PKI" });
                    cc.AccountNumberEncrypted = creditCardDetails.EncryptedCardNumber;

                    if (_configuration.GetValue<bool>("EnablePKDispenserKeyRotationAndOAEPPadding") && creditCardDetails.IsOAEPPaddingCatalogEnabled)
                    {
                        if (ConfigUtility.IsSuppressPkDispenserKeyForOTPFlow(request.Application.Id, request.Application.Version.Major, request?.CatalogItems))
                        {
                            #region If encryptedcreditnumber is not empty and KID is empty throw exception.With new implementation of pkDispenserkey call will be done by client so while passing the encrypted card number they pass the kid as well.
                            if (String.IsNullOrEmpty(creditCardDetails.Kid))
                            {
                                _logger.LogError("GetDataValutRequest Validation failed {Exception} and {sessionId}", "Kid is empty in the request", sessionID);
                                throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                            }
                            #endregion
                            dataVaultRequest.Types.Add(new Characteristic { Code = "KID", Value = creditCardDetails.Kid });
                        }
                        else
                        {
                            string transId = string.IsNullOrEmpty(request.TransactionId) ? "trans0" : request.TransactionId;
                            string key = string.Format(_configuration.GetValue<string>("PKDispenserKeyTokenKeyFormat"), request.Application.Id);
                            var cacheResponse = _cachingService.GetCache<United.Service.Presentation.SecurityResponseModel.PKDispenserKey>(key, transId).Result;
                            var obj = JsonConvert.DeserializeObject<United.Service.Presentation.SecurityResponseModel.PKDispenserKey>(cacheResponse);

                            dataVaultRequest.Types.Add(new Characteristic { Code = "KID", Value = obj.Kid });
                        }
                        dataVaultRequest.Types.Add(new Characteristic { Code = "OAEP", Value = "TRUE" });
                    }
                }
                else
                {
                    cc.AccountNumber = creditCardDetails.UnencryptedCardNumber; //"4000000000000002";
                }
                if (_configuration.GetValue<bool>("PassMobileSessionIDInsteadOfDifferntGuidEveryTime"))
                {
                    cc.OperationID = sessionID; // This one we can pass the session id which we using in bookign path.
                }
                else if (_configuration.GetValue<bool>("EDDtoEMDToggle"))
                {
                    cc.OperationID = Guid.NewGuid().ToString(); // This one we can pass the session id which we using in bookign path.
                }
                else
                {
                    cc.OperationID = Guid.NewGuid().ToString().ToUpper().Replace("-", "");
                }
                ///93875 - iOS Android UnionPay bookings are not throwing error for bin ranges that are not 16 digit credit cards
                ///Srini - 02/13/2018
                
                cc.PinEntryCapability = Service.Presentation.CommonEnumModel.PosTerminalPinEntryCapability.PinNotSupported;
                cc.TerminalAttended = Service.Presentation.CommonEnumModel.PosTerminalAttended.Unattended;
                dataVaultRequest.PointOfSaleCountryCode = "US";
                dataVaultRequest.Types.Add(new Characteristic { Code = "DOLLAR_DING", Value = "TRUE" });
               
                dataVaultRequest.Items.Add(cc);
            }
            return dataVaultRequest;
            #endregion
        }

        public bool GenerateCCTokenWithDataVault(United.Mobile.Model.UnitedClubPasses.CreditCard creditCardDetails, string sessionID, string token, MOBApplication applicationDetails, string deviceID)
        {
            bool generatedCCTokenWithDataVault = false;
            if (!string.IsNullOrEmpty(creditCardDetails.EncryptedCardNumber)) // expecting Client will send only Encrypted Card Number only if the user input is a clear text CC number either for insert CC or update CC not the CC details like CVV number update or expiration date upate no need to call data vault for this type of updates only data vault will be called for CC number update to get the CC token back
            {
                #region
                CslDataVaultRequest dataVaultRequest = GetDataValutRequest(creditCardDetails, sessionID);
                string jsonRequest = JsonConvert.SerializeObject(dataVaultRequest);
                    
                string jsonResponse = _dataVaultService.GetCCTokenWithDataVault(token, jsonRequest, sessionID).Result; 
                
                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    CslDataVaultResponse response = DataContextJsonSerializer.DeserializeJsonDataContract<CslDataVaultResponse>(jsonResponse);

                    if (response != null && response.Responses != null && response.Responses[0].Error == null && response.Responses[0].Message != null && response.Responses[0].Message.Count > 0 && response.Responses[0].Message[0].Code.Trim() == "0")
                    {
                        generatedCCTokenWithDataVault = true;
                        var creditCard = response.Items[0] as Service.Presentation.PaymentModel.CreditCard;
                        creditCardDetails.AccountNumberToken = creditCard.AccountNumberToken;
                        creditCardDetails.PersistentToken = creditCard.PersistentToken;
                        creditCardDetails.SecurityCodeToken = creditCard.SecurityCodeToken;
                        if (String.IsNullOrEmpty(creditCardDetails.CardType))
                        {
                            creditCardDetails.CardType = creditCard.Code;
                        }
                        else if (!_configuration.GetValue<bool>("DisableCheckForUnionPayFOP_MOBILE13762"))
                        {
                            string[] checkForUnionPayFOP = _configuration.GetValue<string>("CheckForUnionPayFOP")?.Split('|');
                            if (creditCard?.Code == checkForUnionPayFOP?[0])
                            {
                                creditCardDetails.CardType = creditCard.Code;
                                creditCardDetails.CardTypeDescription = checkForUnionPayFOP?[1];
                            }
                        }
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
                else
                {
                    string exceptionMessage = _configuration.GetValue<string>("UnableToInsertCreditCardToProfileErrorMessage");
                    if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                    {
                        exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL  GenerateCCTokenWithDataVault(MOBUpdateTravelerRequest request, ref string ccDataVaultToken)";
                    }
                    throw new MOBUnitedException(exceptionMessage);
                }
                #endregion
            }
            else if (!string.IsNullOrEmpty(creditCardDetails.AccountNumberToken.Trim()))
            {
                generatedCCTokenWithDataVault = true;
            }
            return generatedCCTokenWithDataVault;
        }

        private CslDataVaultRequest GetDataValutRequest(United.Mobile.Model.UnitedClubPasses.CreditCard creditCardDetails, string sessionID)
        {
            #region
            var dataVaultRequest = new CslDataVaultRequest
            {
                Items = new System.Collections.ObjectModel.Collection<United.Service.Presentation.PaymentModel.Payment>(),
                Types = new System.Collections.ObjectModel.Collection<Characteristic>(),
                CallingService = new United.Service.Presentation.CommonModel.ServiceClient { Requestor = new Requestor { AgentAAA = "WEB", ApplicationSource = "mobile services" } }
            };
            United.Services.Customer.Common.InsertCreditCardRequest creditCardInsertRequest = new InsertCreditCardRequest();
            if (creditCardDetails != null)
            {
                var cc = new United.Service.Presentation.PaymentModel.CreditCard();
                cc.ExpirationDate = creditCardDetails.ExpireMonth + "/" + (creditCardDetails.ExpireYear.Trim().Length == 2 ? creditCardDetails.ExpireYear.Trim() : creditCardDetails.ExpireYear.Trim().Substring(2, 2).ToString()); //"05/17";
                cc.SecurityCode = creditCardDetails.CIDCVV2.Trim(); //"1234";
                cc.Code = creditCardDetails.CardType;  //"VI";
                cc.Name = creditCardDetails.CCName; //"Test Testing";
                if (!string.IsNullOrEmpty(creditCardDetails.EncryptedCardNumber))
                {
                    dataVaultRequest.Types = new System.Collections.ObjectModel.Collection<Characteristic>();
                    dataVaultRequest.Types.Add(new Characteristic { Code = "ENCRYPTION", Value = "PKI" });
                    cc.AccountNumberEncrypted = creditCardDetails.EncryptedCardNumber;
                }
                else
                {
                    cc.AccountNumber = creditCardDetails.UnencryptedCardNumber; //"4000000000000002";
                }
                if (_configuration.GetValue<bool>("PassMobileSessionIDInsteadOfDifferntGuidEveryTime"))
                {
                    cc.OperationID = sessionID; // This one we can pass the session id which we using in bookign path.
                }
                else if (_configuration.GetValue<bool>("EDDtoEMDToggle"))
                {
                    cc.OperationID = Guid.NewGuid().ToString(); // This one we can pass the session id which we using in bookign path.
                }
                else
                {
                    cc.OperationID = Guid.NewGuid().ToString().ToUpper().Replace("-", "");
                }
                ///93875 - iOS Android UnionPay bookings are not throwing error for bin ranges that are not 16 digit credit cards
                ///Srini - 02/13/2018
                if (_configuration.GetValue<bool>("DataVaultRequestAddDollarDingToggle"))
                {
                    cc.PinEntryCapability = Service.Presentation.CommonEnumModel.PosTerminalPinEntryCapability.PinNotSupported;
                    cc.TerminalAttended = Service.Presentation.CommonEnumModel.PosTerminalAttended.Unattended;
                    dataVaultRequest.PointOfSaleCountryCode = "US";
                    dataVaultRequest.Types.Add(new Characteristic { Code = "DOLLAR_DING", Value = "TRUE" });
                }
                dataVaultRequest.Items.Add(cc);
            }
            return dataVaultRequest;
            #endregion
        }
    }
}
