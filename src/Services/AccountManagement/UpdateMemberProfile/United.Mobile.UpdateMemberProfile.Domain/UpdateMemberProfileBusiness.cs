using Microsoft.Extensions.Configuration;
using Microsoft.Security.Application;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.Shopping;
using United.Mobile.Model.FeedBack;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.UpdateMemberProfile;
using United.Utility.Enum;
using United.Utility.Helper;
using CslDataVaultResponse = United.Service.Presentation.PaymentResponseModel.DataVault<United.Service.Presentation.PaymentModel.Payment>;
using ProfileFOPCreditCardResponse = United.Mobile.Model.Common.ProfileFOPCreditCardResponse;
using ProfileResponse = United.Mobile.Model.Common.ProfileResponse;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.UpdateMemberProfile.Domain
{
    public class UpdateMemberProfileBusiness : IUpdateMemberProfileBusiness
    {
        private readonly ICacheLog<UpdateMemberProfileBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly ICustomerProfile _customerProfile;
        private readonly IPersistToken _persistToken;
        private readonly IDataVaultService _dataVaultService;
        private readonly IUpdateMemberProfileUtility _updateMemberProfileUtility;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly IMPTraveler _mPTraveler;
        private readonly IProfileCreditCard _profileCreditCard;
        private readonly IEmpProfile _empProfile;
        private readonly IFormsOfPayment _formsOfPayment;
        private readonly IValidateHashPinService _validateHashPinService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IPKDispenserService _pKDispenserService;
        private readonly PKDispenserPublicKey _pKDispenserPublicKey;
        private readonly IDPService _dPService;
        private readonly ICachingService _cachingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentUtility _paymentUtility;
        private readonly IHeaders _headers;
        private readonly IProvisionService _provisionService;
        private readonly IFeatureSettings _featureSettings;


        public UpdateMemberProfileBusiness(ICacheLog<UpdateMemberProfileBusiness> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , ICustomerProfile profile
            , IPersistToken persistToken
            , IDataVaultService dataVaultService
            , IUpdateMemberProfileUtility updateMemberProfileUtility
            , IShoppingSessionHelper shoppingSessionHelper
            , IMPTraveler mPTraveler
            , IProfileCreditCard profileCreditCard
            , IEmpProfile empProfile
            , IFormsOfPayment formsOfPayment
            , IValidateHashPinService validateHashPinService
            , IDynamoDBService dynamoDBService
            , IShoppingUtility shoppingUtility
            , IDPService dPService
            , IPKDispenserService pKDispenserService
            , ICachingService cachingService
            , IShoppingCartService shoppingCartService
            , IPaymentService paymentService
            , IPaymentUtility paymentUtility
            , IHeaders headers
            , IProvisionService provisionService
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _customerProfile = profile;
            _persistToken = persistToken;
            _dataVaultService = dataVaultService;
            _updateMemberProfileUtility = updateMemberProfileUtility;
            _shoppingSessionHelper = shoppingSessionHelper;
            _mPTraveler = mPTraveler;
            _profileCreditCard = profileCreditCard;
            _empProfile = empProfile;
            _formsOfPayment = formsOfPayment;
            _validateHashPinService = validateHashPinService;
            _dynamoDBService = dynamoDBService;
            _shoppingUtility = shoppingUtility;
            _dPService = dPService;
            _pKDispenserService = pKDispenserService;
            _cachingService = cachingService;
            _shoppingCartService = shoppingCartService;
            _paymentService = paymentService;
            _paymentUtility = paymentUtility;
            _headers = headers;
            _provisionService = provisionService;
            _featureSettings = featureSettings;
            _pKDispenserPublicKey = new PKDispenserPublicKey(_configuration, _cachingService, _dPService, _pKDispenserService, headers);
        }


        public async Task<MOBCustomerProfileResponse> UpdateProfileOwner(MOBUpdateCustomerFOPRequest request)
        {
            MOBCustomerProfileResponse response = new MOBCustomerProfileResponse();
            response.SessionId = request.SessionId;


            #region ALM 28439 Bug Bounty - Information Disclosure XSS1 #81 - Ravitheja - May 26,2016

            if (!string.IsNullOrEmpty(request.Traveler.FirstName))
                request.Traveler.FirstName = Sanitizer.GetSafeHtmlFragment(request.Traveler.FirstName);

            if (!string.IsNullOrEmpty(request.Traveler.LastName))
                request.Traveler.LastName = Sanitizer.GetSafeHtmlFragment(request.Traveler.LastName);

            if (request.Traveler.EmailAddresses != null)
                for (int i = 0; i < request.Traveler.EmailAddresses.Count; i++)
                {
                    if (!string.IsNullOrEmpty(request.Traveler.EmailAddresses[i].EmailAddress))
                        request.Traveler.EmailAddresses[i].EmailAddress = Sanitizer.GetSafeHtmlFragment(request.Traveler.EmailAddresses[i].EmailAddress);
                }

            if (request.Traveler.Addresses != null)
                for (int i = 0; i < request.Traveler.Addresses.Count; i++)
                {
                    if (!string.IsNullOrEmpty(request.Traveler.Addresses[i].Line1))
                        request.Traveler.Addresses[i].Line1 = Sanitizer.GetSafeHtmlFragment(request.Traveler.Addresses[i].Line1);
                    if (!string.IsNullOrEmpty(request.Traveler.Addresses[i].Line2))
                        request.Traveler.Addresses[i].Line2 = Sanitizer.GetSafeHtmlFragment(request.Traveler.Addresses[i].Line2);
                    if (!string.IsNullOrEmpty(request.Traveler.Addresses[i].Line3))
                        request.Traveler.Addresses[i].Line3 = Sanitizer.GetSafeHtmlFragment(request.Traveler.Addresses[i].Line3);
                    if (!string.IsNullOrEmpty(request.Traveler.Addresses[i].City))
                        request.Traveler.Addresses[i].City = Sanitizer.GetSafeHtmlFragment(request.Traveler.Addresses[i].City);
                    if (!string.IsNullOrEmpty(request.Traveler.Addresses[i].CompanyName))
                        request.Traveler.Addresses[i].CompanyName = Sanitizer.GetSafeHtmlFragment(request.Traveler.Addresses[i].CompanyName);
                }

            #endregion
            #region ALM 24989  - Dover Release - deviceid validation - Modified by Srini 12/29/2015

            bool validWalletRequest = await _updateMemberProfileUtility.isValidDeviceRequest(request.TransactionId, request.DeviceId, request.Application.Id, request.MileagePlusNumber, request.SessionId);

            request.HashPinCode = (request.HashPinCode == null ? string.Empty : request.HashPinCode);
            bool validateMPWithHash = false;
            string authToken = string.Empty;

            var tupleResponse = await ValidateHashPinAndGetAuthToken<bool>(request.MileagePlusNumber, request.HashPinCode, request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId, request.SessionId, authToken);
            validateMPWithHash = tupleResponse.Item1;
            authToken = tupleResponse.authToken;

            if (!validateMPWithHash) // This is to handle the MP Sign In at home using OLD ValidateMP() REST Web API Service method()
            {
                validateMPWithHash = _updateMemberProfileUtility.ValidateAccountFromCache(request.MileagePlusNumber, request.HashPinCode);
            }

            #endregion

            if (validWalletRequest && validateMPWithHash)
            {
                #region
                Session session = await _shoppingSessionHelper.GetShoppingSession(request.SessionId);

                ProfileFOPCreditCardResponse profilePersist = new ProfileFOPCreditCardResponse();
                profilePersist = await _updateMemberProfileUtility.GetCSLProfileFOPCreditCardResponseInSession(request.SessionId);

                //    profile.LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBUpdateCustomerFOPRequest>(request.SessionId, "UpdateProfileOwner", "Request", request.Application.Id, request.Application.Version.Major, request.DeviceId, request));
                _logger.LogInformation("UpdateProfileOwner {SessionID} and {Request}", request.SessionId, JsonConvert.SerializeObject(request));

                response.TransactionId = request.TransactionId;
                MOBUpdateTravelerRequest req = GetUpdateTravelerRequest(request);
                req.Token = session.Token;

                if (request.UpdateInsertCreditCardInfo)// As per requirment from the payment page update only CC do not update/insert phone and e-mail to the profile.
                {
                    req.UpdateAddressInfoAssociatedWithCC = true;
                    req.UpdateCreditCardInfo = true;
                }

                MOBCustomerProfileResponse profileResponse = new MOBCustomerProfileResponse();
                profileResponse.TransactionId = request.TransactionId;

                if (request.IsSavedToProfile)
                {
                    List<MOBItem> insertUpdateItemKeys = new List<MOBItem>();

                    //Do not update traveler if this is for employee booking.
                    if (string.IsNullOrEmpty(session.EmployeeId))
                    {
                        var UpdateTravelerResponse = await _mPTraveler.UpdateTraveler(req, insertUpdateItemKeys);
                        insertUpdateItemKeys = UpdateTravelerResponse.insertUpdateItemKeys;
                    }

                    #region Build MOBCPProfileRequest for GetProfile.
                    MOBCPProfileRequest profileRequest = new MOBCPProfileRequest();
                    profileRequest.Application = request.Application;
                    profileRequest.SessionId = request.SessionId;
                    profileRequest.AccessCode = request.AccessCode;
                    profileRequest.DeviceId = request.DeviceId;
                    profileRequest.LanguageCode = request.LanguageCode;
                    profileRequest.TransactionId = request.TransactionId;
                    profileRequest.MileagePlusNumber = request.MileagePlusNumber;
                    profileRequest.Token = session.Token;
                    //profileRequest.IncludeAllTravelerDa   ata = true;
                    #endregion


                    if (request.TransactionPath != null && (request.TransactionPath.ToUpper().Equals("CHECKINPATH") || request.TransactionPath.ToUpper().Equals("AWARDCANCELPATH") || request.TransactionPath.ToUpper().Equals("VIEWRESPATH")))
                    {
                        profileRequest.IncludeCreditCards = true;
                        profileRequest.IncludeEmailAddresses = true;
                        profileRequest.IncludePhones = true;
                        profileRequest.CustomerId = request.CustomerId;
                        profileRequest.HashPinCode = request.HashPinCode;
                    }

                    if (string.IsNullOrEmpty(session.EmployeeId))
                    {
                        profileResponse.Profiles = await _customerProfile.GetProfile(profileRequest);
                    }

                    //else
                    //{
                    //    #region Employee
                    //    bool getEmployeeIdFromCSLCustomerData = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["GetEmployeeIDFromGetProfileCustomerData"]) && Convert.ToBoolean(ConfigurationManager.AppSettings["GetEmployeeIDFromGetProfileCustomerData"]);
                    //    profileResponse.Profiles = this.profile.GetEmpProfile(profileRequest, getEmployeeIdFromCSLCustomerData);
                    //    if (profileResponse.Profiles != null && profileResponse.Profiles.Count > 0 && profileResponse.Profiles[0].Travelers != null && profileResponse.Profiles[0].Travelers.Count > 0)
                    //    {
                    //        for (int i = 0; i < profileResponse.Profiles[0].Travelers.Count; i++)
                    //        {
                    //            if (profileResponse.Profiles[0].Travelers[i].Key.Equals(request.Traveler.Key))
                    //            {
                    //                if (!string.IsNullOrEmpty(request.Traveler.FirstName) && !string.IsNullOrEmpty(request.Traveler.LastName) && !string.IsNullOrEmpty(request.Traveler.GenderCode) && !string.IsNullOrEmpty(request.Traveler.BirthDate))
                    //                {
                    //                    request.Traveler.Message = string.Empty;
                    //                }

                    //                profileResponse.Profiles[0].Travelers[i] = request.Traveler;

                    //                break;
                    //            }
                    //        }
                    //    }
                    //    #endregion
                    //}

                    _updateMemberProfileUtility.UpdateTravelerCubaReasonInProfile(profileResponse.Profiles, request.Traveler);

                    response.Profiles = profileResponse.Profiles;
                    profilePersist.SessionId = session.SessionId;
                    //profilePersist.CartId = session.CartId;
                    MOBCustomerProfileRequest customerProfileRequest = new MOBCustomerProfileRequest();
                    customerProfileRequest = GetCustomerProfileReq(profileRequest);
                    profilePersist.Request = customerProfileRequest;
                    profilePersist.Response = profileResponse;
                    response.InsertUpdateKeys = insertUpdateItemKeys;

                    string updatedCCKey = string.Empty;
                    if (request.UpdateInsertCreditCardInfo)
                    {
                        var creditCardKey = (from t in insertUpdateItemKeys
                                             where t.Id.ToUpper().Trim() == "CreditCardKey".ToUpper().Trim()
                                             select t).ToList();
                        updatedCCKey = (creditCardKey == null ? string.Empty : creditCardKey[0].CurrentValue);
                    }

                    if (!string.IsNullOrEmpty(updatedCCKey))
                    {
                        #region
                        var lstAddress = new List<MOBAddress>();
                        List<MOBCreditCard> lstCreditCard = _updateMemberProfileUtility.GetProfileOwnerCreditCardList(response.Profiles[0], ref lstAddress, updatedCCKey);
                        response.SelectedCreditCard = (lstCreditCard != null && lstCreditCard.Count > 0) ? lstCreditCard[0] : null;
                        response.SelectedAddress = (lstAddress != null && lstAddress.Count > 0) ? lstAddress[0] : null;
                        if (_configuration.GetValue<bool>("VormetricTokenMigration"))
                        {
                            profilePersist.Response.SelectedCreditCard = response.SelectedCreditCard;
                            profilePersist.Response.SelectedAddress = response.SelectedAddress;
                        }
                        #endregion
                    }
                }

                else
                {
                    if (!request.IsSavedToProfile)
                    {
                        #region
                        if (request.UpdateInsertCreditCardInfo)
                        {
                            // Added By Ali as part of Bug 132538:Booking Prod mApp and mWeb does not validate Billing address if not saving it
                            if (request != null && request.Traveler != null && request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0 && request.Traveler.Addresses[0].State != null && !string.IsNullOrEmpty(request.Traveler.Addresses[0].State.Code.Trim()))
                            {
                                await _customerProfile.UpdatePersistedReservation(req);
                            }
                            List<MOBCreditCard> ccList = new List<MOBCreditCard>();
                            foreach (MOBCreditCard creditCard in request.Traveler.CreditCards)
                            {
                                if (!string.IsNullOrEmpty(creditCard.EncryptedCardNumber))
                                {
                                    #region
                                    string ccDataVaultToken = string.Empty;
                                    bool isPersistAssigned = await AssignDataVaultAndPersistTokenToCC(request.SessionId, session.Token, creditCard, request.Application);
                                    var tupleRes = await _profileCreditCard.GenerateCCTokenWithDataVault(creditCard, request.SessionId, session.Token, request.Application, request.DeviceId, ccDataVaultToken);
                                    bool generatedCCTokenWithDataVault = tupleRes.Item1;
                                    ccDataVaultToken = tupleRes.ccDataVaultToken;

                                    if (!isPersistAssigned && generatedCCTokenWithDataVault)
                                    {
                                        creditCard.AccountNumberToken = ccDataVaultToken;
                                        if (creditCard.UnencryptedCardNumber != null)
                                        {
                                            creditCard.UnencryptedCardNumber = "XXXXXXXXXXXX" + creditCard.UnencryptedCardNumber.Substring((creditCard.UnencryptedCardNumber.Length - 4), 4);
                                        }
                                    }
                                    #endregion
                                }
                                else
                                {
                                    //MOBILE - 2220 MP Sign In | CFOP OFF | On Purchasing PCU the ancillary from saved different card it is showing error "Sorry something went wrong"
                                    MOBVormetricKeys vormetricKeys = await GetVormetricPersistentTokenForViewRes(creditCard, session.SessionId, session.Token);
                                    if (!string.IsNullOrEmpty(vormetricKeys.PersistentToken))
                                    {
                                        creditCard.PersistentToken = vormetricKeys.PersistentToken;
                                        creditCard.SecurityCodeToken = vormetricKeys.SecurityCodeToken;
                                    }
                                }
                                if (_configuration.GetValue<string>("MakeItEmptyCreditCardInformationNeededMessage") != null && _configuration.GetValue<bool>("MakeItEmptyCreditCardInformationNeededMessage"))
                                {
                                    creditCard.Message = string.Empty;
                                }
                                creditCard.IsPrimary = true;
                                creditCard.IsValidForTPIPurchase = _updateMemberProfileUtility.IsValidFOPForTPIpayment(creditCard.CardType);
                                ccList.Add(creditCard);
                            }
                            profilePersist.Response.SelectedCreditCard = ccList[0];
                            if (request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0)
                            {
                                profilePersist.Response.SelectedAddress = request.Traveler.Addresses[0];
                            }
                        }
                        #endregion
                    }
                }

                if (request.Traveler.Phones != null && request.Traveler.Phones.Count > 0)
                {
                    profilePersist.Response.Profiles[0].Travelers[0].ReservationPhones = new List<MOBCPPhone>();
                    profilePersist.Response.Profiles[0].Travelers[0].ReservationPhones.Add(request.Traveler.Phones[0]);
                }

                if (request.Traveler.EmailAddresses != null && request.Traveler.EmailAddresses.Count > 0)
                {
                    profilePersist.Response.Profiles[0].Travelers[0].ReservationEmailAddresses = new List<MOBEmail>();
                    profilePersist.Response.Profiles[0].Travelers[0].ReservationEmailAddresses.Add(request.Traveler.EmailAddresses[0]);
                }

                //set primary for selected cc and address
                SetPrimaryKeyForSelectedCreditCardAndBillingAddress(profilePersist);

                await SaveProfilePersistForAwardCancelPath(profilePersist, request.TransactionPath, request.DeviceId, request.MileagePlusNumber, profilePersist.SessionId);
                if (request.IsSavedToProfile)
                {
                    profilePersist.Response = response;
                    await _sessionHelperService.SaveSession<ProfileFOPCreditCardResponse>(profilePersist, request.SessionId, new List<string> { request.SessionId, new ProfileFOPCreditCardResponse().ObjectName }, new ProfileFOPCreditCardResponse().ObjectName).ConfigureAwait(false);
                }
                else
                {
                    profilePersist.Response.InsertUpdateKeys = null;
                }
                response = profilePersist.Response;
                #endregion
            }
            else
            {
                response.Exception = new MOBException();
                response.Exception.Message = _configuration.GetValue<string>("GenericExceptionMessage");
            }

            return await Task.FromResult(response);
        }

        public async Task<MOBPromoFeedbackResponse> PromoFeedback(Model.FeedBack.MOBPromoFeedbackRequest promoFeedbackRequest)
        {
            MOBPromoFeedbackResponse response = new MOBPromoFeedbackResponse();

            if (promoFeedbackRequest != null && !string.IsNullOrEmpty(promoFeedbackRequest.MileagePlusAccountNumber) && !string.IsNullOrEmpty(promoFeedbackRequest.TransactionId))
            {
                response.Request = promoFeedbackRequest;
                response.Succeed = await _updateMemberProfileUtility.SendChasePromoFeedbackToCCE(promoFeedbackRequest.MileagePlusAccountNumber, promoFeedbackRequest.MessageKey, promoFeedbackRequest.EventType, (MOBRequest)promoFeedbackRequest, promoFeedbackRequest.SessionId, "PromoFeedback");
            }
            else
            {
                throw new MOBUnitedException("Invalid Request");
            }

            _logger.LogInformation("GetFeedbackParameters {ClientResult} and {sessionId}", JsonConvert.SerializeObject(promoFeedbackRequest), promoFeedbackRequest.SessionId);

            return await Task.FromResult(response);
        }

        public bool IsViewResPath(string viewrespath)
        {
            return !string.IsNullOrEmpty(viewrespath) && viewrespath.ToUpper().Equals("VIEWRESTPATH");
        }

        private MOBUpdateTravelerRequest GetUpdateTravelerRequest(MOBUpdateCustomerFOPRequest request)
        {
            MOBUpdateTravelerRequest res = new MOBUpdateTravelerRequest();

            foreach (var propReq in request.GetType().GetProperties())
            {
                var propRes = res.GetType().GetProperty(propReq.Name);
                if (propRes != null)
                {
                    propRes.SetValue(res, propReq.GetValue(request));
                }
            }

            return res;
        }

        private MOBCustomerProfileRequest GetCustomerProfileReq(MOBCPProfileRequest profileRequest)
        {
            MOBCustomerProfileRequest res = new MOBCustomerProfileRequest();

            foreach (var propReq in profileRequest.GetType().GetProperties())
            {
                var propRes = res.GetType().GetProperty(propReq.Name);
                if (propRes != null)
                {
                    propRes.SetValue(res, propReq.GetValue(profileRequest));
                }
            }
            return res;
        }

        private void SetPrimaryKeyForSelectedCreditCardAndBillingAddress(ProfileFOPCreditCardResponse profilePersist)
        {
            if (profilePersist != null &&
                profilePersist.Response != null &&
                profilePersist.Response.Profiles != null &&
                profilePersist.Response.Profiles.Any() &&
                profilePersist.Response.Profiles[0].Travelers != null &&
                profilePersist.Response.Profiles[0].Travelers.Any())
            {
                //264646:Booking flow exception analysis - Get Profile Owner : Changed by Richa
                //Reason : we are setting the card and address to  profilePersist based on the IsSavedToProfile(save card to profile toggle) . 
                //When IsSavedToProfile is marked as false, profilePersist not contain value for SelectedCreditCard & SelectedAddress. Thus included the null check here.
                if (profilePersist.Response.Profiles[0].Travelers[0].CreditCards != null &&
                    profilePersist.Response.Profiles[0].Travelers[0].CreditCards.Any() &&
                    profilePersist.Response.SelectedCreditCard != null)
                {
                    profilePersist.Response.Profiles[0].Travelers[0].CreditCards.
                        ForEach(c => c.IsPrimary = (c.Key == profilePersist.Response.SelectedCreditCard.Key));
                }
                if (profilePersist.Response.Profiles[0].Travelers[0].Addresses != null &&
                    profilePersist.Response.Profiles[0].Travelers[0].Addresses.Any() &&
                    profilePersist.Response.SelectedAddress != null)
                {
                    profilePersist.Response.Profiles[0].Travelers[0].Addresses.
                        ForEach(a => a.IsPrimary = (a.Key == profilePersist.Response.SelectedAddress.Key));
                }
            }
        }

        private async Task SaveProfilePersistForAwardCancelPath(ProfileFOPCreditCardResponse profilePersist, string transactionPath, string deviceId, string mpNumber, string sessionId, string shoppingCartFlow = "")
        {
            if (_configuration.GetValue<bool>("VormetricTokenMigration"))
            {
                if (string.Equals("AWARDCANCELPATH", transactionPath, StringComparison.OrdinalIgnoreCase))
                {
                    await _sessionHelperService.SaveSession<ProfileFOPCreditCardResponse>(profilePersist, sessionId, new List<string> { sessionId, new ProfileFOPCreditCardResponse().ObjectName }, profilePersist.ObjectName).ConfigureAwait(false);
                }
                else if ((shoppingCartFlow == United.Utility.Enum.FlowType.BOOKING.ToString() || shoppingCartFlow == United.Utility.Enum.FlowType.RESHOP.ToString()) && _configuration.GetValue<bool>("IsBookingCommonFOPEnabled"))
                {
                    ProfileResponse profilePersist1 = new ProfileResponse();
                    profilePersist1 = _updateMemberProfileUtility.ObjectToObjectCasting<ProfileResponse, ProfileFOPCreditCardResponse>(profilePersist);
                    await _sessionHelperService.SaveSession<ProfileResponse>(profilePersist1, sessionId, new List<string> { sessionId, new ProfileResponse().ObjectName }, profilePersist1.ObjectName).ConfigureAwait(false);
                }
                else
                {
                    await _sessionHelperService.SaveSession<ProfileFOPCreditCardResponse>(profilePersist, sessionId, new List<string> { sessionId, profilePersist.ObjectName }, profilePersist.ObjectName).ConfigureAwait(false);
                }
            }
        }

        public async Task<MOBUpdateProfileOwnerFOPResponse> UpdateProfileOwnerCardInfo(MOBUpdateProfileOwnerFOPRequest request)
        {
            MOBUpdateProfileOwnerFOPResponse response = new MOBUpdateProfileOwnerFOPResponse();
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            string updatedCCKey = string.Empty;
            string authToken = string.Empty;
            List<MOBCPProfile> profileDetails = new List<MOBCPProfile>();
            MOBCreditCard selectedProfileCreditCard = null;
            MOBAddress selectedProfileAddress = null;
            List<MOBItem> insertUpdateItemKeys = new List<MOBItem>();
            MOBCustomerProfileRequest customerProfileRequest = null;
            Session session = await _shoppingSessionHelper.GetShoppingSession(request.SessionId);
            request.Traveler.CustomerId = request.CustomerId;
            response.TransactionId = request.TransactionId;

            #region ALM 28439 Bug Bounty - Information Disclosure XSS1 #81 - Ravitheja - May 26,2016

            if (!string.IsNullOrEmpty(request.Traveler.FirstName))
                request.Traveler.FirstName = Sanitizer.GetSafeHtmlFragment(request.Traveler.FirstName);

            if (!string.IsNullOrEmpty(request.Traveler.LastName))
                request.Traveler.LastName = Sanitizer.GetSafeHtmlFragment(request.Traveler.LastName);

            if (request.Traveler.EmailAddresses != null)
                for (int i = 0; i < request.Traveler.EmailAddresses.Count; i++)
                {
                    if (!string.IsNullOrEmpty(request.Traveler.EmailAddresses[i].EmailAddress))
                        request.Traveler.EmailAddresses[i].EmailAddress = Sanitizer.GetSafeHtmlFragment(request.Traveler.EmailAddresses[i].EmailAddress);
                }

            if (request.Traveler.Addresses != null)
                for (int i = 0; i < request.Traveler.Addresses.Count; i++)
                {
                    if (!string.IsNullOrEmpty(request.Traveler.Addresses[i].Line1))
                        request.Traveler.Addresses[i].Line1 = Sanitizer.GetSafeHtmlFragment(request.Traveler.Addresses[i].Line1);
                    if (!string.IsNullOrEmpty(request.Traveler.Addresses[i].Line2))
                        request.Traveler.Addresses[i].Line2 = Sanitizer.GetSafeHtmlFragment(request.Traveler.Addresses[i].Line2);
                    if (!string.IsNullOrEmpty(request.Traveler.Addresses[i].Line3))
                        request.Traveler.Addresses[i].Line3 = Sanitizer.GetSafeHtmlFragment(request.Traveler.Addresses[i].Line3);
                    if (!string.IsNullOrEmpty(request.Traveler.Addresses[i].City))
                        request.Traveler.Addresses[i].City = Sanitizer.GetSafeHtmlFragment(request.Traveler.Addresses[i].City);
                    if (!string.IsNullOrEmpty(request.Traveler.Addresses[i].CompanyName))
                        request.Traveler.Addresses[i].CompanyName = Sanitizer.GetSafeHtmlFragment(request.Traveler.Addresses[i].CompanyName);
                }

            #endregion

            #region ALM 24989  - Dover Release - deviceid validation - Modified by Srini 12/29/2015
            bool validWalletRequest = await _updateMemberProfileUtility.isValidDeviceRequest(request.TransactionId, request.DeviceId, request.Application.Id, request.MileagePlusNumber, request.SessionId);
            request.HashPinCode = (request.HashPinCode == null ? string.Empty : request.HashPinCode);

            HashPin hashPin = new HashPin(_logger, _configuration, _validateHashPinService, _dynamoDBService, _headers, _featureSettings);
            var tupleResponse = await hashPin.ValidateHashPinAndGetAuthToken(request.MileagePlusNumber, request.HashPinCode, request.Application.Id, request.DeviceId, request.Application.Version.Major, authToken);
            bool validateMPWithHash = tupleResponse.returnValue;
            authToken = tupleResponse.validAuthToken;

            if (!validateMPWithHash) // This is to handle the MP Sign In at home using OLD ValidateMP() REST Web API Service method()
            {
                validateMPWithHash = _updateMemberProfileUtility.ValidateAccountFromCache(request.MileagePlusNumber, request.HashPinCode);
            }
            #endregion

            if (validWalletRequest && validateMPWithHash)
            {
                #region Load the Persist Profile Details (BOOKING/VIEWRES)                  
                ProfileFOPCreditCardResponse profilePersist = new ProfileFOPCreditCardResponse();
                ProfileResponse profilePersist1 = new ProfileResponse();
                if (request.Flow == United.Utility.Enum.FlowType.VIEWRES.ToString() || request.Flow == United.Utility.Enum.FlowType.CHECKIN.ToString() || request.Flow == United.Utility.Enum.FlowType.POSTBOOKING.ToString() || request.Flow == United.Utility.Enum.FlowType.VIEWRES_SEATMAP.ToString() || request.Flow == FlowType.BAGGAGECALCULATOR.ToString())

                    profilePersist = await _sessionHelperService.GetSession<ProfileFOPCreditCardResponse>(request.SessionId, profilePersist.ObjectName, new List<string> { request.SessionId, profilePersist.ObjectName });
                else if (request.Flow == United.Utility.Enum.FlowType.BOOKING.ToString() || request.Flow == United.Utility.Enum.FlowType.RESHOP.ToString())
                {
                    profilePersist1 = await _sessionHelperService.GetSession<ProfileResponse>(request.SessionId, profilePersist1.ObjectName, new List<string> { request.SessionId, profilePersist1.ObjectName });
                    profilePersist = _updateMemberProfileUtility.ObjectToObjectCasting<ProfileFOPCreditCardResponse, ProfileResponse>(profilePersist1);
                }
                #endregion
                MOBUpdateTravelerRequest updateTravelerRequest = _updateMemberProfileUtility.ObjectToObjectCasting<MOBUpdateTravelerRequest, MOBUpdateProfileOwnerFOPRequest>(request);
                updateTravelerRequest.Token = session.Token;

                if (request.UpdateInsertCreditCardInfo)// As per requirment from the payment page update only CC do not update/insert phone and e-mail to the profile.
                {
                    updateTravelerRequest.UpdateAddressInfoAssociatedWithCC = true;
                    updateTravelerRequest.UpdateCreditCardInfo = true;
                }

                if (!_configuration.GetValue<bool>("DisableFixforEmptyPersistentTokenForCC_MOBILE12988") && !string.IsNullOrEmpty(session.EmployeeId))
                {
                    request.IsSavedToProfile = false;
                }
                #region IsSavedToProfile = true
                if (request.IsSavedToProfile)
                {
                    MOBUpdateProfileOwnerFOPResponse getProfileResponse = new MOBUpdateProfileOwnerFOPResponse();

                    getProfileResponse.TransactionId = request.TransactionId;
                    //V788383: 11/28/2018 - Do not update traveler if this is for employee booking unless Save to Profile option selected
                    if (string.IsNullOrEmpty(session.EmployeeId))
                    {
                        var UpdateTravelerResponse = await _mPTraveler.UpdateTraveler(updateTravelerRequest, insertUpdateItemKeys);
                        insertUpdateItemKeys = UpdateTravelerResponse.insertUpdateItemKeys;
                       
                        if (session.CatalogItems != null && session.CatalogItems.Count > 0 && _shoppingUtility.IsEnablePartnerProvision(session?.CatalogItems, request.Flow, request.Application.Id, request.Application.Version.Major))
                        {
                            if (request.IsPartnerProvision || request.IsExistCreditCard)
                            {
                                string statusCode;
                                if (request.IsExistCreditCard)
                                    statusCode = "DUPLICATE";
                                else
                                {
                                    if (UpdateTravelerResponse.updateTravelerSuccess)
                                        statusCode = "SUCCESS";
                                    else statusCode = "FAILURE";
                                }
                                System.Threading.Tasks.Task.Factory.StartNew(() => UpdateProvisionLinkStatus(request.AccountReferenceIdentifier, request.MileagePlusNumber, session.Token, statusCode, request.PartnerRequestIdentifier, session.SessionId));
                            }
                        }
                    }

                    #region Build ProfileRequest for GetProfile or GetEmpProfile.
                    MOBCPProfileRequest getProfileRequest = new MOBCPProfileRequest()
                    {
                        Application = request.Application,
                        SessionId = request.SessionId,
                        AccessCode = request.AccessCode,
                        DeviceId = request.DeviceId,
                        LanguageCode = request.LanguageCode,
                        TransactionId = request.TransactionId,
                        MileagePlusNumber = request.MileagePlusNumber,
                        Token = session.Token
                    };
                    #endregion
                    if (_updateMemberProfileUtility.IsInternationalBillingAddress_CheckinFlowEnabled(request.Application))
                    {
                        getProfileRequest.Flow = request.Flow;
                    }

                    #region Call either GetProfile or GetEmpProfile
                    if (string.IsNullOrEmpty(session.EmployeeId))
                    {
                        getProfileRequest.IncludeCreditCards = true;
                        getProfileRequest.IncludeEmailAddresses = true;
                        getProfileRequest.IncludePhones = true;
                        getProfileRequest.CustomerId = request.CustomerId;
                        getProfileRequest.HashPinCode = request.HashPinCode;
                        //v788383: 11/28/2018 - get profile info if flow is "VIEWRES" and "SaveToProfile" is true
                        getProfileResponse.Profiles = await _customerProfile.GetProfile(getProfileRequest);
                    }
                    else
                    {
                        bool getEmployeeIdFromCSLCustomerData = !string.IsNullOrEmpty(_configuration.GetValue<string>("GetEmployeeIDFromGetProfileCustomerData")) && _configuration.GetValue<bool>("GetEmployeeIDFromGetProfileCustomerData");
                        getProfileResponse.Profiles = await _empProfile.GetEmpProfile(getProfileRequest, getEmployeeIdFromCSLCustomerData);
                        //if (getProfileResponse.Profiles != null && getProfileResponse.Profiles.Count > 0 && getProfileResponse.Profiles[0].Travelers != null && getProfileResponse.Profiles[0].Travelers.Count > 0)
                        //{
                        //    for (int i = 0; i < getProfileResponse.Profiles[0].Travelers.Count; i++)
                        //    {
                        //        if (getProfileResponse.Profiles[0].Travelers[i].Key.Equals(request.Traveler.Key))
                        //        {
                        //            if (!string.IsNullOrEmpty(request.Traveler.FirstName) && !string.IsNullOrEmpty(request.Traveler.LastName) && !string.IsNullOrEmpty(request.Traveler.GenderCode) && !string.IsNullOrEmpty(request.Traveler.BirthDate))
                        //            {
                        //                request.Traveler.Message = string.Empty;
                        //            }
                        //            getProfileResponse.Profiles[0].Travelers[i] = request.Traveler;
                        //            break;
                        //        }
                        //    }
                        //}
                    }
                    #endregion

                    #region - Fix by Nizam for 1279 - Excluding the Ghost cards from profile
                    if ((request.Flow.ToUpper().Trim() == "VIEWRES" || request.Flow == (FlowType.BAGGAGECALCULATOR.ToString()) || request.Flow.ToUpper().Equals(FlowType.VIEWRES_SEATMAP) || request.Flow == FlowType.CHECKIN.ToString()) && getProfileResponse.Profiles[0].Travelers != null && getProfileResponse.Profiles[0].Travelers[0].CreditCards != null)
                        getProfileResponse.Profiles[0].Travelers[0].CreditCards = getProfileResponse.Profiles[0].Travelers[0].CreditCards.Where(x => x.IsCorporate == false).ToList();
                    #endregion
                    // [MB-6318]:Booking: CFOP: (Formerly Air Travel card) text is displayed in Payment method screen for UATP	
                    #region Update the Diner's CLub and UATP card description
                    if ((request.Flow == FlowType.VIEWRES.ToString() || request.Flow == FlowType.VIEWRES_SEATMAP.ToString() || request.Flow == FlowType.POSTBOOKING.ToString()) && getProfileResponse.Profiles[0].Travelers[0].CreditCards != null)
                    {
                        foreach (var creditCard in getProfileResponse.Profiles[0].Travelers[0].CreditCards)
                        {
                            if (creditCard.CardTypeDescription != null)
                            {
                                switch (creditCard.CardTypeDescription.ToLower())
                                {
                                    case "diners club":
                                        creditCard.CardTypeDescription = "Diners Club Card";
                                        break;
                                    case "uatp (formerly air travel card)":
                                        creditCard.CardTypeDescription = "UATP";
                                        break;
                                }
                            }

                        }
                    }
                    #endregion

                    _updateMemberProfileUtility.UpdateTravelerCubaReasonInProfile(getProfileResponse.Profiles, request.Traveler);

                    profileDetails = getProfileResponse.Profiles;
                    profilePersist.SessionId = session.SessionId;
                    customerProfileRequest = _updateMemberProfileUtility.ObjectToObjectCasting<MOBCustomerProfileRequest, MOBCPProfileRequest>(getProfileRequest);

                    #region Get the latest inserted key for CC        
                    if (request.UpdateInsertCreditCardInfo && string.IsNullOrEmpty(session.EmployeeId))
                    {
                        var creditCardKey = (from t in insertUpdateItemKeys
                                             where t.Id.ToUpper().Trim() == "CreditCardKey".ToUpper().Trim()
                                             select t).ToList();
                        updatedCCKey = ((creditCardKey == null && creditCardKey.Count() > 1) ? string.Empty : creditCardKey[0].CurrentValue);
                    }
                    #endregion

                    #region Get the latest inserted CC and Address based on updatedCCKey and set as Selected
                    if (string.IsNullOrEmpty(session.EmployeeId) && (!string.IsNullOrEmpty(updatedCCKey)))
                    {
                        #region
                        var lstAddress = new List<MOBAddress>();
                        List<MOBCreditCard> lstCreditCard = _updateMemberProfileUtility.GetProfileOwnerCreditCardList(profileDetails[0], ref lstAddress, updatedCCKey);
                        selectedProfileCreditCard = (lstCreditCard != null && lstCreditCard.Count > 0) ? lstCreditCard[0] : null;
                        selectedProfileAddress = (lstAddress != null && lstAddress.Count > 0) ? lstAddress[0] : null;
                        #endregion
                    }
                    #endregion
                    #region For EMP account set the requested CC as default/Selected
                    else if (!string.IsNullOrEmpty(session.EmployeeId))
                    {
                        selectedProfileCreditCard = request.Traveler.CreditCards[0];
                        selectedProfileAddress = request.Traveler.Addresses[0];
                    }
                    #endregion
                }
                #endregion
                #region IsSavedToProfile = false
                else
                {
                    if (!request.IsSavedToProfile)
                    {
                        #region
                        if (request.UpdateInsertCreditCardInfo)
                        {
                            #region StateCode validation
                            if (request != null && request.Traveler != null && request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0 && request.Traveler.Addresses[0].State != null && !string.IsNullOrEmpty(request.Traveler.Addresses[0].State.Code?.Trim()))
                            {
                                await _customerProfile.UpdatePersistedReservation(updateTravelerRequest);
                            }
                            #endregion

                            #region Get token for CC and set it as default/primary CC and Address
                            List<MOBCreditCard> ccList = new List<MOBCreditCard>();
                            foreach (MOBCreditCard creditCard in request.Traveler.CreditCards)
                            {
                                if (!string.IsNullOrEmpty(creditCard.EncryptedCardNumber))
                                {
                                    #region
                                    string ccDataVaultToken = string.Empty;

                                    bool isPersistAssigned = await AssignDataVaultAndPersistTokenToCC(request.SessionId, session.Token, creditCard, request.Application);
                                    var tupleRes = await _profileCreditCard.GenerateCCTokenWithDataVault(creditCard, request.SessionId, session.Token, request.Application, request.DeviceId, ccDataVaultToken);
                                    bool generatedCCTokenWithDataVault = tupleRes.Item1;
                                    ccDataVaultToken = tupleRes.ccDataVaultToken;

                                    if (!isPersistAssigned && generatedCCTokenWithDataVault)
                                    {
                                        creditCard.AccountNumberToken = ccDataVaultToken;
                                        if (creditCard.UnencryptedCardNumber != null)
                                        {
                                            creditCard.UnencryptedCardNumber = "XXXXXXXXXXXX" + creditCard.UnencryptedCardNumber.Substring((creditCard.UnencryptedCardNumber.Length - 4), 4);
                                        }
                                    }
                                    #endregion
                                }
                                if (_configuration.GetValue<String>("MakeItEmptyCreditCardInformationNeededMessage") != null && _configuration.GetValue<bool>("MakeItEmptyCreditCardInformationNeededMessage"))
                                {
                                    creditCard.Message = string.Empty;
                                }
                                creditCard.IsPrimary = true;
                                //creditCard.IsTemparory = true;
                                creditCard.IsValidForTPIPurchase = _updateMemberProfileUtility.IsValidFOPForTPIpayment(creditCard.CardType);
                                ccList.Add(creditCard);
                            }
                            selectedProfileCreditCard = ccList[0];
                            if (request.Traveler.Addresses != null && request.Traveler.Addresses.Count > 0)
                            {
                                selectedProfileAddress = request.Traveler.Addresses[0];
                            }
                            #endregion
                            if (_configuration.GetValue<bool>("VormetricTokenMigration"))
                            {
                                profilePersist.Response.SelectedCreditCard = selectedProfileCreditCard;
                                profilePersist.Response.SelectedAddress = selectedProfileAddress;
                            }
                        }
                        #endregion
                        profileDetails = profilePersist.Response.Profiles;
                    }
                }
                #endregion
                #region Add newly added Traveler Phone and Address to ProfilePersist
                if (request.Traveler.Phones != null && request.Traveler.Phones.Count > 0)
                {
                    profileDetails[0].Travelers[0].ReservationPhones = new List<MOBCPPhone>();
                    profileDetails[0].Travelers[0].ReservationPhones.Add(request.Traveler.Phones[0]);
                }

                if (request.Traveler.EmailAddresses != null && request.Traveler.EmailAddresses.Count > 0)
                {
                    profileDetails[0].Travelers[0].ReservationEmailAddresses = new List<MOBEmail>();
                    profileDetails[0].Travelers[0].ReservationEmailAddresses.Add(request.Traveler.EmailAddresses[0]);
                }
                #endregion
                profilePersist.Response = new MOBCustomerProfileResponse()
                {
                    Profiles = profileDetails,
                    PKDispenserPublicKey = await _pKDispenserPublicKey.GetCachedOrNewpkDispenserPublicKey(request.Application.Id, request.Application.Version.Major, request.DeviceId, session.SessionId, session.Token, session.CatalogItems),
                    MileagePlusNumber = request.MileagePlusNumber,
                    SelectedCreditCard = selectedProfileCreditCard,
                    SelectedAddress = selectedProfileAddress,
                };
                if (!_configuration.GetValue<bool>("DisableManageResChanges23C"))
                {
                    profilePersist.Response = new MOBCustomerProfileResponse()
                    {
                        Profiles = profileDetails,
                        PKDispenserPublicKey = await _pKDispenserPublicKey.GetCachedOrNewpkDispenserPublicKey(request.Application.Id, request.Application.Version.Major, request.DeviceId, session.SessionId, session.Token, request.Flow, session.CatalogItems),
                        MileagePlusNumber = request.MileagePlusNumber,
                        SelectedCreditCard = selectedProfileCreditCard,
                        SelectedAddress = selectedProfileAddress,
                    };
                }
                profilePersist.Request = customerProfileRequest;
                profilePersist.customerProfileResponse = _updateMemberProfileUtility.ObjectToObjectCasting<MOBUpdateProfileOwnerFOPResponse, MOBCustomerProfileResponse>(profilePersist.Response);
                await SaveProfilePersistForAwardCancelPath(profilePersist, request.TransactionPath, request.DeviceId, request.MileagePlusNumber, profilePersist.SessionId, request.Flow);

                if (request.IsSavedToProfile)
                {
                    SetPrimaryKeyForSelectedCreditCardAndBillingAddress((ProfileFOPCreditCardResponse)profilePersist);

                    if (request.Flow == FlowType.VIEWRES.ToString() || request.Flow == FlowType.VIEWRES_SEATMAP.ToString() || request.Flow == FlowType.BAGGAGECALCULATOR.ToString() || request.Flow == FlowType.CHECKIN.ToString())

                        await _sessionHelperService.SaveSession<ProfileFOPCreditCardResponse>(profilePersist, request.SessionId, new List<string>() { request.SessionId, profilePersist.ObjectName }, profilePersist.ObjectName);

                    else if (request.Flow == FlowType.BOOKING.ToString() || request.Flow == FlowType.RESHOP.ToString())
                    {
                        profilePersist1 = _updateMemberProfileUtility.ObjectToObjectCasting<ProfileResponse, ProfileFOPCreditCardResponse>(profilePersist);
                        await _sessionHelperService.SaveSession<ProfileResponse>(profilePersist1, request.SessionId, new List<string>() { request.SessionId, profilePersist1.ObjectName }, profilePersist1.ObjectName);
                    }

                }
                else if (!request.IsSavedToProfile)
                {
                    if (profilePersist.Response.Profiles != null &&
                        profilePersist.Response.Profiles.Count() > 0 &&
                        profilePersist.Response.Profiles[0].Travelers != null &&
                        profilePersist.Response.Profiles[0].Travelers.Count() > 0 &&
                        profilePersist.Response.Profiles[0].Travelers[0].CreditCards != null &&
                        profilePersist.Response.Profiles[0].Travelers[0].CreditCards.Count() > 0
                        )
                    {
                        if (!(profilePersist.Response.Profiles.SelectMany(x => x.Travelers).SelectMany(x => x.CreditCards).Any(x => x.IsPrimary == true)))
                        {
                            profilePersist.Response.Profiles[0].Travelers[0].CreditCards[0].IsPrimary = true;
                        }
                    }
                }

                response = _updateMemberProfileUtility.ObjectToObjectCasting<MOBUpdateProfileOwnerFOPResponse, MOBCustomerProfileResponse>(profilePersist.Response);
                response.ShoppingCart = await RetrieveRefeshedShoppingCart(request, profilePersist);
                response.PkDispenserPublicKey = profilePersist.Response.PKDispenserPublicKey;
                response.InsertUpdateKeys = insertUpdateItemKeys;
                response.SessionId = request.SessionId;
                response.Flow = request.Flow;


                if (request.Flow == FlowType.BOOKING.ToString() || request.Flow == FlowType.RESHOP.ToString())
                {
                    var bookingPathReservation = await GetReservationFromPersist(request.SessionId, updatedCCKey, request.Traveler);
                    response.Reservation = _updateMemberProfileUtility.MakeReservationFromPersistReservation(response.Reservation, bookingPathReservation, session);
                    if (!request.IsSavedToProfile || !string.IsNullOrEmpty(session.EmployeeId))
                    {
                        if (response.Reservation.CreditCards.Count() > 0)
                        {
                            response.Reservation.CreditCards[0] = response.SelectedCreditCard;
                            bookingPathReservation.CreditCards[0] = response.SelectedCreditCard;
                        }
                        else
                        {
                            response.Reservation.CreditCards = new List<MOBCreditCard> { response.SelectedCreditCard };
                            bookingPathReservation.CreditCards = new List<MOBCreditCard> { response.SelectedCreditCard };
                        }

                        response.Reservation.FormOfPaymentType = MOBFormofPayment.CreditCard;
                        response.Reservation.PayPal = null;
                        response.Reservation.PayPalPayor = null;
                        response.Reservation.Masterpass = null;
                        response.Reservation.MasterpassSessionDetails = null;

                        bookingPathReservation.FormOfPaymentType = MOBFormofPayment.CreditCard;
                        bookingPathReservation.PayPal = null;
                        bookingPathReservation.PayPalPayor = null;
                        bookingPathReservation.Masterpass = null;
                        bookingPathReservation.MasterpassSessionDetails = null;
                        if (response.Reservation?.CreditCardsAddress?.Count > 0)
                        {
                            response.Reservation.CreditCardsAddress[0] = response.SelectedAddress;
                            bookingPathReservation.CreditCardsAddress[0] = response.SelectedAddress;
                        }
                        else
                        {
                            response.Reservation.CreditCardsAddress = new List<MOBAddress> { response.SelectedAddress };
                            bookingPathReservation.CreditCardsAddress = new List<MOBAddress> { response.SelectedAddress };
                        }
                        response.Reservation.ReservationPhone = request.Traveler.Phones[0];
                        response.Reservation.ReservationEmail = request.Traveler.EmailAddresses[0];
                        bool FilePersist = await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, request.SessionId, new List<string>() { request.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName);
                    }
                    UpdateMatchedTravelerCubaReason(request.Traveler, response.Reservation.TravelersCSL);
                    if (response.Reservation != null && session != null && !string.IsNullOrEmpty(session.EmployeeId))
                    {
                        response.Reservation.IsEmp20 = true;
                    }
                    response.Reservation.IsBookingCommonFOPEnabled = bookingPathReservation.IsBookingCommonFOPEnabled;
                    shoppingCart = response.ShoppingCart;
                    shoppingCart = await _shoppingUtility.ReservationToShoppingCart_DataMigration(response.Reservation, shoppingCart, request, session);
                    shoppingCart.Flow = request.Flow;

                    await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, request.SessionId, new List<string>() { request.SessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName);
                    response.ShoppingCart = shoppingCart;
                }

                if (session.CatalogItems != null && session.CatalogItems.Count > 0 && _shoppingUtility.IsEnablePartnerProvision(session?.CatalogItems, request.Flow, request.Application.Id, request.Application.Version.Major))
                {
                    if (request.IsPartnerProvision)
                    {
                        if (request.Flow == FlowType.CHECKIN.ToString())
                        {
                            if (response.ShoppingCart.PartnerProvisionDetails == null)
                                response.ShoppingCart.PartnerProvisionDetails = new PartnerProvisionDetails();
                            response.ShoppingCart.PartnerProvisionDetails.ProvisionLinkedCardMessage = _configuration.GetValue<string>("ProvisionLinkedCardMessage");
                        }
                        else if (response.ShoppingCart.PartnerProvisionDetails != null)
                        {
                            response.ShoppingCart.PartnerProvisionDetails.ProvisionLinkedCardMessage = _configuration.GetValue<string>("ProvisionLinkedCardMessage");
                        }

                        if (await _featureSettings.GetFeatureSettingValue("EnableTrackPartnerProvision_Cards").ConfigureAwait(false))
                        {
                            if (response.ShoppingCart.PartnerProvisionDetails != null)
                            {
                                response.ShoppingCart.PartnerProvisionDetails.IsItChaseProvisionCard = true;
                                await _sessionHelperService.SaveSession<PartnerProvisionDetails>(response.ShoppingCart.PartnerProvisionDetails, request.SessionId, new List<string>() { request.SessionId, new PartnerProvisionDetails().ObjectName }, new PartnerProvisionDetails().ObjectName);
                            }
                        }
                    }
                }
            }
            else
            {
                response.Exception = new MOBException();
                //response.Exception.Message = ConfigurationManager.AppSettings["GenericExceptionMessage"].ToString();
                response.Exception.Message = _configuration.GetValue<string>("UpdateProfileCCException") as string;

                if ((request.IsPartnerProvision || request.IsExistCreditCard) && (session.CatalogItems != null && session.CatalogItems.Count > 0 && _shoppingUtility.IsEnablePartnerProvision(session?.CatalogItems, request.Flow, request.Application.Id, request.Application.Version.Major)))
                {
                    System.Threading.Tasks.Task.Factory.StartNew(() => UpdateProvisionLinkStatus(request.AccountReferenceIdentifier, request.MileagePlusNumber, session.Token, "FAILURE", request.PartnerRequestIdentifier, session.SessionId));
                }
            }
            if ((ConfigUtility.IsETCchangesEnabled(request.Application.Id, request.Application.Version.Major) && (request.Flow == FlowType.BOOKING.ToString()) && response.ShoppingCart != null)
                || (IsManageResETCEnabled(request.Application.Id, request.Application.Version.Major) && (request.Flow == FlowType.VIEWRES.ToString() || request.Flow == FlowType.VIEWRES_SEATMAP.ToString() || request.Flow == FlowType.BAGGAGECALCULATOR.ToString())))
            {
                bool isEnableMFOPBags = await _featureSettings.GetFeatureSettingValue("EnableMfopForBags").ConfigureAwait(false)
                                       && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("AndroidMilesFopBagsVersion"), _configuration.GetValue<string>("iPhoneMilesFopBagsVersion"))
                                       && (response.ShoppingCart?.Products?.Any(p => p.Code == "BAG") ?? false)
                                       && _paymentUtility.GetIsMilesFOPEnabled(response.ShoppingCart);

                bool isDefault = false;

                bool isMilesFOPEligible = ConfigUtility.EnableMilesFOP(request.Application.Id, request.Application.Version.Major)
                                            && (request.Flow == FlowType.VIEWRES.ToString() || request.Flow == FlowType.VIEWRES_SEATMAP.ToString())
                                            && !isEnableMFOPBags ? response?.ShoppingCart?.FormofPaymentDetails?.MilesFOP?.IsMilesFOPEligible != null ? response.ShoppingCart.FormofPaymentDetails.MilesFOP.IsMilesFOPEligible : false : true; //if MFOPB bags code is not enable then logic should go with SC object value otherwise by default to send true to enable Use Miles option

                var getEFoPRespose = await _formsOfPayment.GetEligibleFormofPayments(request, session, response.ShoppingCart, request.CartId, request.Flow, isDefault, response.Reservation, IsMilesFOPEnabled: isMilesFOPEligible);
                var strEligibleFormofPayments = JsonConvert.SerializeObject(getEFoPRespose.response);
                response.EligibleFormofPayments = JsonConvert.DeserializeObject<List<Model.Common.Payment.FormofPaymentOption>>(strEligibleFormofPayments);

            }
            else
            {
                FormofPaymentOption persistedFormofPaymentOption = new FormofPaymentOption();
                response.EligibleFormofPayments = await _sessionHelperService.GetSession<List<United.Mobile.Model.Common.Payment.FormofPaymentOption>>(request.SessionId, typeof(List<FormofPaymentOption>).FullName, new List<string> { request.SessionId, typeof(List<FormofPaymentOption>).FullName }).ConfigureAwait(false);

                if (response.EligibleFormofPayments == null && (request.Flow == FlowType.POSTBOOKING.ToString() || (!await _featureSettings.GetFeatureSettingValue("DisableEFOPObjectNameinCheckin").ConfigureAwait(false) && request.Flow == FlowType.CHECKIN.ToString())))
                    response.EligibleFormofPayments = await _sessionHelperService.GetSession<List<United.Mobile.Model.Common.Payment.FormofPaymentOption>>(request.SessionId, new Model.Common.Payment.FormofPaymentOption().ObjectName, new List<string> { request.SessionId, new Model.Common.Payment.FormofPaymentOption().ObjectName }).ConfigureAwait(false);

            }
            //Covid-19 Emergency WHO TPI content
            if (_configuration.GetValue<bool>("ToggleCovidEmergencytextTPI") == true)
            {
                bool return_TPICOVID_19WHOMessage_For_BackwardBuilds = GeneralHelper.IsApplicationVersionGreater2(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("Android_Return_TPICOVID_19WHOMessage__For_BackwardBuilds"), _configuration.GetValue<string>("iPhone_Return_TPICOVID_19WHOMessage_For_BackwardBuilds"), "", "", _configuration);
                if (!return_TPICOVID_19WHOMessage_For_BackwardBuilds && response.Reservation != null
                    && response.Reservation.TripInsuranceInfoBookingPath != null && response.Reservation.TripInsuranceInfoBookingPath.tpiAIGReturnedMessageContentList != null
                    && response.Reservation.TripInsuranceInfoBookingPath.tpiAIGReturnedMessageContentList.Count > 0)
                {
                    MOBItem tpiCOVID19EmergencyAlertBookingPath = response.Reservation.TripInsuranceInfoBookingPath.tpiAIGReturnedMessageContentList.Find(p => p.Id.ToUpper().Trim() == "COVID19EmergencyAlert".ToUpper().Trim());
                    if (tpiCOVID19EmergencyAlertBookingPath != null)
                    {
                        response.Reservation.TripInsuranceInfoBookingPath.Tnc = response.Reservation.TripInsuranceInfoBookingPath.Tnc +
                            "<br><br>" + tpiCOVID19EmergencyAlertBookingPath.CurrentValue;
                    }
                }
            }

            return response;
        }

        public async Task UpdateProvisionStatus(MOBUpdateProfileOwnerFOPRequest request)
        {
            Session session = await _shoppingSessionHelper.GetShoppingSession(request.SessionId);
            if (session.CatalogItems != null && session.CatalogItems.Count > 0 && _shoppingUtility.IsEnablePartnerProvision(session?.CatalogItems, request.Flow, request.Application.Id, request.Application.Version.Major))
                System.Threading.Tasks.Task.Factory.StartNew(() => UpdateProvisionLinkStatus(request.AccountReferenceIdentifier, request.MileagePlusNumber, session.Token, "FAILURE", request.PartnerRequestIdentifier, session.SessionId));
        }

        private async Task UpdateProvisionLinkStatus(string accReferenceIdentifier, string mpNumber, string token, string statusCode, string partnerRequestIdentifier, string sessionId)
        {
            MOBShoppingCart persistShoppingCart = new MOBShoppingCart();
            persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(sessionId, persistShoppingCart.ObjectName, new List<string> { sessionId, persistShoppingCart.ObjectName }).ConfigureAwait(false);

            if (persistShoppingCart != null && (persistShoppingCart.PartnerProvisionDetails == null || (persistShoppingCart.PartnerProvisionDetails != null && !persistShoppingCart.PartnerProvisionDetails.IsUpdateProvisionLinkStatus)))
            {
                var _cslReq = new
                {
                    AccountReferenceIdentifier = accReferenceIdentifier,
                    ChannelName = _configuration.GetValue<string>("Shopping - ChannelType"),
                    PartnerRequestIdentifier = partnerRequestIdentifier,
                    CustomerIdentifier = mpNumber,
                    LinkageStatusCode = statusCode,
                    MPNumber = mpNumber
                };
                string path = "/UpdateProvisionLinkStatus";
                _provisionService.CSL_PartnerProvisionCall(token, path, JsonConvert.SerializeObject(_cslReq));

                if (persistShoppingCart.PartnerProvisionDetails != null)
                {
                    persistShoppingCart.PartnerProvisionDetails.IsUpdateProvisionLinkStatus = true;
                }
                else
                {
                    persistShoppingCart.PartnerProvisionDetails = new PartnerProvisionDetails();
                    persistShoppingCart.PartnerProvisionDetails.IsUpdateProvisionLinkStatus = true;
                }
                await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, sessionId, new List<string> { sessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName).ConfigureAwait(false);
            }
        }

        public bool IsIncludeMoneyMilesInRTI(List<United.Mobile.Model.Common.Payment.FormofPaymentOption> eligibleFormsOfPayment)
        {
            return (eligibleFormsOfPayment != null && eligibleFormsOfPayment.Exists(x => x.Category == "MILES"));
        }

        private async Task HandleAncillaryOptionsForUplift(string sessionId, Reservation bookingPathReservation)
        {
            if (!_configuration.GetValue<bool>("EnableUpliftPayment"))
                return;

            if (bookingPathReservation?.ShopReservationInfo2 == null)
                return;

            //need to load chaseAd from persist, and update the offer prices.
            //this is to handle when user choose Uplift as FOP and then changed the fop we need to show chaseAd again.
            if (bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement == null)
            {
                var chaseCreditStatement = await _sessionHelperService.GetSession<MOBCCAdStatement>(sessionId, new MOBCCAdStatement().ObjectName, new List<string> { sessionId, new MOBCCAdStatement().ObjectName }).ConfigureAwait(false);
                if (chaseCreditStatement != null)
                {
                    var objPrice = bookingPathReservation.Prices.FirstOrDefault(p => p.PriceType.ToUpper().Equals("GRAND TOTAL"));
                    if (objPrice != null)
                    {
                        decimal price = Convert.ToDecimal(objPrice.Value);
                        if (_configuration.GetValue<bool>("TurnOffChaseBugMOBILE-11134"))
                        {
                            chaseCreditStatement.finalAfterStatementDisplayPrice = _updateMemberProfileUtility.GetPriceAfterChaseCredit(price);
                        }
                        else
                        {
                            chaseCreditStatement.finalAfterStatementDisplayPrice = _updateMemberProfileUtility.GetPriceAfterChaseCredit(price, chaseCreditStatement.statementCreditDisplayPrice);
                        }

                        chaseCreditStatement.initialDisplayPrice = price.ToString("C2", CultureInfo.CurrentCulture);
                        _updateMemberProfileUtility.FormatChaseCreditStatemnet(chaseCreditStatement);
                    }
                    bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement = chaseCreditStatement;
                }
            }
            bookingPathReservation.ShopReservationInfo2.HideTravelOptionsOnRTI = false;
            bookingPathReservation.ShopReservationInfo2.HideSelectSeatsOnRTI = false;

            //need to remove uplift tpi message if any when uplift is removed from formsofpayment
            if (bookingPathReservation.ShopReservationInfo2.InfoWarningMessages?.Any() ?? false)
            {
                bookingPathReservation.ShopReservationInfo2.InfoWarningMessages.RemoveAll(i => i.Order == MOBINFOWARNINGMESSAGEORDER.UPLIFTTPISECONDARYPAYMENT.ToString());
            }
        }

        private async Task<(List<MOBCreditCard> savedProfileOwnerCCList, List<MOBAddress> creditCardAddresses, MOBCPPhone mpPhone, MOBEmail mpEmail)> GetProfileOwnerCreditCardList(string sessionID, List<MOBAddress> creditCardAddresses, MOBCPPhone mpPhone, MOBEmail mpEmail, string updatedCCKey)
        {
            #region
            creditCardAddresses = new List<MOBAddress>();
            List<MOBCreditCard> savedProfileOwnerCCList = null;
            ProfileResponse profilePersist = new ProfileResponse();
            profilePersist = await _updateMemberProfileUtility.GetCSLProfileResponseInSession(sessionID);
            if (profilePersist.Response != null && profilePersist.Response.Profiles != null)
            {
                #region
                foreach (var traveler in profilePersist.Response.Profiles[0].Travelers)
                {
                    if (traveler.IsProfileOwner)
                    {
                        #region
                        if (traveler.CreditCards != null && traveler.CreditCards.Count > 0)
                        {
                            savedProfileOwnerCCList = new List<MOBCreditCard>();
                            foreach (var creditCard in traveler.CreditCards)
                            {
                                if (creditCard.IsPrimary || creditCard.Key == updatedCCKey)
                                {
                                    savedProfileOwnerCCList = new List<MOBCreditCard>();
                                    savedProfileOwnerCCList.Add(creditCard);
                                    if (!String.IsNullOrEmpty(creditCard.AddressKey))
                                    {
                                        foreach (var address in traveler.Addresses)
                                        {
                                            if (address.Key.ToUpper().Trim() == creditCard.AddressKey.ToUpper().Trim())
                                            {
                                                creditCardAddresses = new List<MOBAddress>();
                                                creditCardAddresses.Add(address);
                                                break;
                                            }
                                        }
                                    }
                                    if (creditCard.Key == updatedCCKey || string.IsNullOrEmpty(updatedCCKey))
                                    {
                                        break;
                                    }
                                }
                            }
                            if (savedProfileOwnerCCList == null || savedProfileOwnerCCList.Count == 0) // To check if there is not Primary CC then get the top 1 CC from the List.
                            {
                                savedProfileOwnerCCList = new List<MOBCreditCard>();
                                savedProfileOwnerCCList.Add(traveler.CreditCards[0]);
                                if (traveler.Addresses != null && traveler.Addresses.Count > 0)
                                {
                                    creditCardAddresses = new List<MOBAddress>();
                                    foreach (var address in traveler.Addresses)
                                    {
                                        if (address.Key.ToUpper().Trim() == traveler.CreditCards[0].AddressKey.ToUpper().Trim())
                                        {
                                            creditCardAddresses.Add(address);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        #endregion
                        if (traveler.ReservationPhones != null && traveler.ReservationPhones.Count > 0)
                        {
                            mpPhone = traveler.ReservationPhones[0];
                        }
                        if (traveler.ReservationEmailAddresses != null && traveler.ReservationEmailAddresses.Count > 0)
                        {
                            mpEmail = traveler.ReservationEmailAddresses[0];
                        }
                        break;
                    }
                }
                #endregion
            }
            return (savedProfileOwnerCCList, creditCardAddresses, mpPhone, mpEmail);
            #endregion
        }

        private async Task<bool> AssignDataVaultAndPersistTokenToCC(string sessionId, string sessionToken, MOBCreditCard creditCard, MOBApplication application)
        {
            bool isPersistAssigned = _configuration.GetValue<bool>("VormetricTokenMigration");

            if (isPersistAssigned)
            {
                if (await _profileCreditCard.GenerateCCTokenWithDataVault(creditCard, sessionId, sessionToken, application, _headers.ContextValues.DeviceId))
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
                    else if (String.IsNullOrEmpty(creditCard.AccountNumberToken) && !string.IsNullOrEmpty(sessionToken) && !string.IsNullOrEmpty(sessionId))
                    {
                        MOBVormetricKeys vormetricKeys = await GetPersistentTokenUsingAccountNumberToken(creditCard.AccountNumberToken, sessionId, sessionToken);
                        creditCard.PersistentToken = vormetricKeys.PersistentToken;
                        creditCard.SecurityCodeToken = vormetricKeys.SecurityCodeToken;
                        creditCard.CardType = vormetricKeys.CardType;
                    }
                    else
                    {
                        LogNoPersistentTokenInCSLResponseForVormetricPayment(sessionId);
                    }
                }

            }
            return isPersistAssigned;
        }

        private void LogNoPersistentTokenInCSLResponseForVormetricPayment(string sessionId, string Message = "Unable to retieve PersistentToken")
        {
            _logger.LogWarning("PERSISTENTTOKENNOTFOUND Error {exception} {sessionid}", Message, sessionId);

            _logger.LogInformation("LogNoPersistentTokenInCSLResponseForVormetricPayment -PERSISTENTTOKENNOTFOUND {message} and {sessionID}", Message, sessionId);

            //No need to block the flow as we are calling DataVault for Persistent Token during the final payment
            //throw new System.Exception(ConfigurationManager.AppSettings["VormetricExceptionMessage"]);
        }

        private async Task<MOBVormetricKeys> GetPersistentTokenUsingAccountNumberToken(string accountNumberToke, string sessionId, string token)
        {
            //string url = string.Format("{0}/{1}/RSA", _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLDataVault"), accountNumberToke);
            string url = string.Format("{0}/RSA", accountNumberToke);
            var cslResponse = await MakeHTTPCallAndLogIt(
                                        sessionId,
                                        _headers.ContextValues.DeviceId,
                                        "CSL-ChangeEligibleCheck",
                                        token,
                                        url,
                                        string.Empty,
                                        true,
                                        false);

            return GetPersistentTokenFromCSLDatavaultResponse(cslResponse, sessionId);
        }

        private async Task<string> MakeHTTPCallAndLogIt(string sessionId, string deviceId, string action, string token, string url, string jsonRequest, bool isGetCall, bool isXMLRequest = false)
        {

            string jsonResponse = string.Empty;

            string paypalCSLCallDurations = string.Empty;
            string callTime4Tuning = string.Empty;

            if (isGetCall)
            {
                jsonResponse = await _dataVaultService.GetPersistentToken(token, jsonRequest, url, sessionId).ConfigureAwait(false);
            }
            else
            {
                jsonResponse = await _dataVaultService.PersistentToken(token, jsonRequest, url, sessionId).ConfigureAwait(false);
            }

            return jsonResponse;
        }

        private MOBVormetricKeys GetPersistentTokenFromCSLDatavaultResponse(string jsonResponseFromCSL, string sessionID)
        {
            MOBVormetricKeys vormetricKeys = new MOBVormetricKeys();
            if (!string.IsNullOrEmpty(jsonResponseFromCSL))
            {
                CslDataVaultResponse response = JsonConvert.DeserializeObject<CslDataVaultResponse>(jsonResponseFromCSL);
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
                        if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && _configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL InsertTravelerCreditCard(MOBUpdateTravelerRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }

            return vormetricKeys;
        }

        private async Task<MOBShoppingCart> RetrieveRefeshedShoppingCart(MOBUpdateProfileOwnerFOPRequest request, ProfileFOPCreditCardResponse profileResponse)
        {
            MOBShoppingCart persistShoppingCart = new MOBShoppingCart();
            persistShoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, persistShoppingCart.ObjectName, new List<string> { request.SessionId, persistShoppingCart.ObjectName }).ConfigureAwait(false);
            var bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
            if (persistShoppingCart == null)
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

            persistShoppingCart.CartId = request.CartId;
            var formOfPaymentDetails = new MOBFormofPaymentDetails();
            formOfPaymentDetails.FormOfPaymentType = MOBFormofPayment.CreditCard.ToString();
            formOfPaymentDetails.EmailAddress = request.Traveler.EmailAddresses[0].EmailAddress;
            formOfPaymentDetails.BillingAddress = profileResponse.Response.SelectedAddress;
            formOfPaymentDetails.Phone = request.Traveler.Phones[0];
            formOfPaymentDetails.TravelCertificate = persistShoppingCart.FormofPaymentDetails != null ? persistShoppingCart.FormofPaymentDetails.TravelCertificate : null;
            formOfPaymentDetails.IsOtherFOPRequired = persistShoppingCart.FormofPaymentDetails != null ? persistShoppingCart.FormofPaymentDetails.IsOtherFOPRequired : true;

            if (IsManageResETCEnabled(request.Application.Id, request.Application.Version.Major) && (request.Flow == FlowType.VIEWRES.ToString() || request.Flow == FlowType.VIEWRES_SEATMAP.ToString())
               && persistShoppingCart?.FormofPaymentDetails?.TravelCertificate != null)
            {
                formOfPaymentDetails.TravelCertificate = persistShoppingCart.FormofPaymentDetails.TravelCertificate;
            }
            if (!(persistShoppingCart.Flow == FlowType.BOOKING.ToString() && bookingPathReservation.IsRedirectToSecondaryPayment))
            {
                formOfPaymentDetails.CreditCard = profileResponse.Response.SelectedCreditCard;
            }
            else
            {
                formOfPaymentDetails.CreditCard = persistShoppingCart.FormofPaymentDetails.CreditCard;
                formOfPaymentDetails.SecondaryCreditCard = profileResponse.Response.SelectedCreditCard;
            }
            if (IncludeFFCResidual(request.Application.Id, request.Application.Version.Major))
            {
                formOfPaymentDetails.TravelFutureFlightCredit = persistShoppingCart.FormofPaymentDetails?.TravelFutureFlightCredit;
            }
            if (_updateMemberProfileUtility.IncludeMoneyPlusMiles(request.Application.Id, request.Application.Version.Major))
            {
                formOfPaymentDetails.MoneyPlusMilesCredit = persistShoppingCart.FormofPaymentDetails?.MoneyPlusMilesCredit;
            }
            if (_updateMemberProfileUtility.IncludeTravelCredit(request.Application.Id, request.Application.Version.Major))
            {
                formOfPaymentDetails.TravelCreditDetails = persistShoppingCart.FormofPaymentDetails?.TravelCreditDetails;
            }
            if (_updateMemberProfileUtility.IncludeTravelBankFOP(request.Application.Id, request.Application.Version.Major))
            {
                formOfPaymentDetails.TravelBankDetails = persistShoppingCart.FormofPaymentDetails?.TravelBankDetails;
            }
            if (_updateMemberProfileUtility.IsInternationalBillingAddress_CheckinFlowEnabled(request.Application) && request?.Flow.ToLower() == FlowType.CHECKIN.ToString().ToLower())
            {
                if (persistShoppingCart.FormofPaymentDetails?.InternationalBilling?.BillingAddressProperties.Count > 0)
                {
                    formOfPaymentDetails.InternationalBilling = new MOBInternationalBilling();
                    formOfPaymentDetails.InternationalBilling.BillingAddressProperties = persistShoppingCart.FormofPaymentDetails.InternationalBilling.BillingAddressProperties;
                }
                else
                {
                    formOfPaymentDetails = GetBillingAddressProperties(formOfPaymentDetails);
                }
            }
            if (ConfigUtility.EnableMilesFOP(request.Application.Id, request.Application.Version.Major))
            {
                if (persistShoppingCart?.FormofPaymentDetails?.MilesFOP != null && (persistShoppingCart.Flow == FlowType.VIEWRES.ToString() || request.Flow == FlowType.VIEWRES_SEATMAP.ToString()))
                {
                    formOfPaymentDetails.MilesFOP = persistShoppingCart.FormofPaymentDetails.MilesFOP;
                }
                
                if (persistShoppingCart.DisplayMessage?.Count > 0)
                {
                    persistShoppingCart.DisplayMessage = persistShoppingCart.DisplayMessage.Where(x => x.Text1 != "Not enough miles").ToList();
                }
            }
            persistShoppingCart.FormofPaymentDetails = formOfPaymentDetails;
            if (_updateMemberProfileUtility.EnableInflightContactlessPayment(request.Application.Id, request.Application.Version.Major, persistShoppingCart.Flow != FlowType.BOOKING.ToString()))
            {
                if (persistShoppingCart?.InFlightContactlessPaymentEligibility?.IsEligibleInflightCLPayment ?? false && !request.IsSavedToProfile)
                {
                    persistShoppingCart.InFlightContactlessPaymentEligibility.IsCCSelectedForContactless = request.IsCCSelectedForContactless;
                }
            }
            //Travel Credit summary Banner should not displaying on RTI when Money&Miles||TravelBank FOP is Applied - MOBILE-23748
            if (!_configuration.GetValue<bool>("EnableTravelCreditBanner"))
            {
                if ((persistShoppingCart.FormofPaymentDetails?.MoneyPlusMilesCredit?.SelectedMoneyPlusMiles != null || persistShoppingCart.FormofPaymentDetails?.TravelBankDetails?.TBApplied > 0)
                    && !string.IsNullOrEmpty(persistShoppingCart?.FormofPaymentDetails?.TravelCreditDetails?.TravelCreditSummary)
                    && persistShoppingCart.Flow?.ToLower() == FlowType.BOOKING.ToString()?.ToLower())
                {
                    persistShoppingCart.FormofPaymentDetails.TravelCreditDetails.TravelCreditSummary = string.Empty;
                }
            }

            bool isEnableMFOPBags = await _featureSettings.GetFeatureSettingValue("EnableMfopForBags").ConfigureAwait(false)
                                       && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("AndroidMilesFopBagsVersion"), _configuration.GetValue<string>("iPhoneMilesFopBagsVersion"));
            if (isEnableMFOPBags
                && (persistShoppingCart.Products?.Any(p => p.Code == "BAG") ?? false)
                && persistShoppingCart.FormofPaymentDetails?.FormOfPaymentType != MOBFormofPayment.MilesFOP.ToString()
                && (persistShoppingCart.TermsAndConditions?.Any(t => t.ContentKey == "MilesFOP_TandC") ?? false))
            {
                persistShoppingCart.TermsAndConditions.Remove(persistShoppingCart.TermsAndConditions.First(t => t.ContentKey == "MilesFOP_TandC"));
            }

            await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, request.SessionId, new List<string> { request.SessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName).ConfigureAwait(false);
            return persistShoppingCart;
        }
        private MOBFormofPaymentDetails GetBillingAddressProperties(MOBFormofPaymentDetails formofPaymentDetails)
        {
            if (formofPaymentDetails == null)
            {
                formofPaymentDetails = new MOBFormofPaymentDetails();
            }
            formofPaymentDetails.InternationalBilling = new MOBInternationalBilling();
            //var billingCountries = GetCachedBillingAddressCountries();

            //if (billingCountries == null || !billingCountries.Any())
            //{
            //    billingCountries = new List<MOBCPBillingCountry>();

            //    billingCountries.Add(new MOBCPBillingCountry
            //    {
            //        CountryCode = "US",
            //        CountryName = "United States",
            //        Id = "1",
            //        IsStateRequired = true,
            //        IsZipRequired = true
            //    });
            //}
            //formofPaymentDetails.InternationalBilling.BillingAddressProperties = (billingCountries == null || !billingCountries.Any()) ? null : billingCountries;
            return formofPaymentDetails;
        }


        private bool IncludeFFCResidual(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableFFCResidual")
                && GeneralHelper.IsApplicationVersionGreater
                (appId, appVersion, "AndroidFFCResidualVersion", "iPhoneFFCResidualVersion", "", "", true, _configuration);
        }

        private bool IsManageResETCEnabled(int applicationId, string appVersion)
        {
            if (_configuration.GetValue<bool>("EnableEtcforSeats_PCU_Viewres") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableETCManageRes_AppVersion"), _configuration.GetValue<string>("iPhone_EnableETCManageRes_AppVersion")))
            {
                return true;
            }
            return false;
        }

        private async Task<MOBVormetricKeys> GetVormetricPersistentTokenForViewRes(MOBCreditCard persistedCreditCard, string sessionId, string token)
        {
            MOBVormetricKeys vormetricKeys = new MOBVormetricKeys();
            if (_configuration.GetValue<bool>("VormetricTokenMigration"))
            {
                //MOBILE-1243 CFOP MP Sign IN - ViewRes - TPI - CC saved
                ProfileFOPCreditCardResponse profilePersist = new ProfileFOPCreditCardResponse();
                profilePersist = await _sessionHelperService.GetSession<ProfileFOPCreditCardResponse>(sessionId, profilePersist.ObjectName, new List<string> { sessionId, profilePersist.ObjectName }).ConfigureAwait(false);
                if (persistedCreditCard != null && !string.IsNullOrEmpty(persistedCreditCard.Key) &&
                    profilePersist != null &&
                    profilePersist.Response != null &&
                    profilePersist.Response.Profiles != null &&
                    profilePersist.Response.Profiles.Count > 0 &&
                    profilePersist.Response.Profiles[0].Travelers != null &&
                    profilePersist.Response.Profiles[0].Travelers.Count > 0 &&
                    profilePersist.Response.Profiles[0].Travelers[0].CreditCards != null &&
                    profilePersist.Response.Profiles[0].Travelers[0].CreditCards.Count > 0 &&
                        (profilePersist.Response.Profiles[0].Travelers[0].CreditCards.Exists(p => p.Key == persistedCreditCard.Key) ||
                        profilePersist.Response.Profiles[0].Travelers[0].CreditCards.Exists(p => p.AccountNumberToken == persistedCreditCard.AccountNumberToken))

                    )
                {
                    var cc = profilePersist.Response.Profiles[0].Travelers[0].CreditCards.FirstOrDefault(p => p.Key == persistedCreditCard.Key);
                    if (cc == null)
                    {
                        cc = profilePersist.Response.Profiles[0].Travelers[0].CreditCards.FirstOrDefault(p => p.AccountNumberToken == persistedCreditCard.AccountNumberToken);
                    }
                    vormetricKeys.PersistentToken = cc.PersistentToken;
                    vormetricKeys.AccountNumberToken = cc.AccountNumberToken;
                    vormetricKeys.SecurityCodeToken = cc.SecurityCodeToken;
                    vormetricKeys.CardType = cc.CardType;
                }
                else if (!string.IsNullOrEmpty(profilePersist?.Response?.SelectedCreditCard?.PersistentToken) &&
                   (_configuration.GetValue<bool>("DisableCompareAccountTokenNumber") ? true : profilePersist?.Response?.SelectedCreditCard?.AccountNumberToken == persistedCreditCard.AccountNumberToken))
                {
                    vormetricKeys.PersistentToken = profilePersist.Response.SelectedCreditCard.PersistentToken;
                    vormetricKeys.AccountNumberToken = profilePersist.Response.SelectedCreditCard.AccountNumberToken;
                    vormetricKeys.SecurityCodeToken = profilePersist.Response.SelectedCreditCard.SecurityCodeToken;
                    vormetricKeys.CardType = profilePersist.Response.SelectedCreditCard.CardType;
                }
                //MOBILE-1681 CFOP ON MP Sign IN - ViewRes - PCU - MasterPass
                else if (!string.IsNullOrEmpty(profilePersist?.customerProfileResponse?.SelectedCreditCard?.PersistentToken) &&
                    (_configuration.GetValue<bool>("DisableCompareAccountTokenNumber") ? true : profilePersist?.customerProfileResponse?.SelectedCreditCard?.AccountNumberToken == persistedCreditCard.AccountNumberToken))
                {
                    vormetricKeys.PersistentToken = profilePersist.customerProfileResponse.SelectedCreditCard.PersistentToken;
                    vormetricKeys.AccountNumberToken = profilePersist.customerProfileResponse.SelectedCreditCard.AccountNumberToken;
                    vormetricKeys.SecurityCodeToken = profilePersist.customerProfileResponse.SelectedCreditCard.SecurityCodeToken;
                    vormetricKeys.CardType = profilePersist.customerProfileResponse.SelectedCreditCard.CardType;
                }
                else if (persistedCreditCard != null && (!string.IsNullOrEmpty(persistedCreditCard.AccountNumberToken) || !string.IsNullOrEmpty(persistedCreditCard.PersistentToken)))
                {
                    vormetricKeys.PersistentToken = persistedCreditCard.PersistentToken;
                    vormetricKeys.AccountNumberToken = persistedCreditCard.AccountNumberToken;
                    vormetricKeys.SecurityCodeToken = persistedCreditCard.SecurityCodeToken;
                    vormetricKeys.CardType = persistedCreditCard.CardType;
                }
                //MOBILE-1238 CFOP Guest Flow - ViewRes - TPI - CC


                if (String.IsNullOrEmpty(vormetricKeys.PersistentToken) && !string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(vormetricKeys.AccountNumberToken))
                {
                    vormetricKeys = await GetPersistentTokenUsingAccountNumberToken(vormetricKeys.AccountNumberToken, sessionId, token);
                }

                if (String.IsNullOrEmpty(vormetricKeys.PersistentToken))
                {
                    LogNoPersistentTokenInCSLResponseForVormetricPayment(sessionId);
                }
            }

            return vormetricKeys;
        }

        private async Task<Reservation> GetReservationFromPersist(string sessionID, string updatedCCKey, MOBCPTraveler traveler)
        {
            #region Update Booking Path Persist Reservation IsSignedInWithMP value and Save to session
            Reservation bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionID, bookingPathReservation.ObjectName, new List<string> { sessionID, bookingPathReservation.ObjectName }).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(updatedCCKey) && traveler != null)
            {
                #region
                List<MOBAddress> address = new List<MOBAddress>();
                MOBCPPhone mpPhone = new MOBCPPhone();
                MOBEmail mpEmail = new MOBEmail();
                var tupleResponse = await GetProfileOwnerCreditCardList(sessionID, address, mpPhone, mpEmail, updatedCCKey);
                bookingPathReservation.CreditCards = tupleResponse.savedProfileOwnerCCList;
                address = tupleResponse.creditCardAddresses;
                mpPhone = tupleResponse.mpPhone;
                mpEmail = tupleResponse.mpEmail;
                //profile.LogEntries.AddRange(travelerCSL.LogEntries);
                bookingPathReservation.CreditCardsAddress = address;
                bookingPathReservation.ReservationPhone = traveler.Phones[0]; // As per requirment from the payment page update only CC do not update phoen and email but the update phone and email should be saved to persist reservation.
                bookingPathReservation.ReservationEmail = traveler.EmailAddresses[0];
                bookingPathReservation.FormOfPaymentType = MOBFormofPayment.CreditCard;
                await HandleAncillaryOptionsForUplift(sessionID, bookingPathReservation);

                await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, sessionID, new List<string> { sessionID, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                #endregion
            }

            return bookingPathReservation;
            #endregion
        }

        private void UpdateMatchedTravelerCubaReason(MOBCPTraveler traveler, List<MOBCPTraveler> travelers)
        {
            if (travelers != null && travelers.Count > 0)
            {
                travelers.Where(p => p != null && p.Key == traveler.Key).ToList().ForEach(p => p.CubaTravelReason = traveler.CubaTravelReason);
            }
        }

        public async Task<(bool, string authToken)> ValidateHashPinAndGetAuthToken<T>(string accountNumber, string hashPinCode, int applicationId, string deviceId, string appVersion, string transactionId, string sessionId, string authToken)
        {
            bool IsTokenValid = false;
            _logger.LogInformation("ValidateHashPinAndGetAuthToken - Hashpin Validation request {@AccountNumber} {@HashPinCode}", accountNumber, hashPinCode);
            var mpResponse = await new HashPin(_logger, _configuration, _validateHashPinService, _dynamoDBService, _headers, _featureSettings).ValidateHashPinAndGetAuthTokenDynamoDB(accountNumber, hashPinCode, applicationId, deviceId, appVersion).ConfigureAwait(false);
            if (mpResponse.IsTokenValid != null && mpResponse.HashPincode == hashPinCode)
            {
                authToken = mpResponse.AuthenticatedToken?.ToString();
                IsTokenValid = true;
            }

            return (IsTokenValid, authToken);
        }
    }
}