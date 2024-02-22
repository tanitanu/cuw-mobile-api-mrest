using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Security.Application;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UAWSMPTravelCertificateService.ETCServiceSoap;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.MPAuthentication;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.Common;
using United.Mobile.Model.Common.SSR;
using United.Mobile.Model.CSLModels;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.MPSignIn;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Booking;
using United.Mobile.Model.Shopping.Bundles;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.Travelers;
using United.Service.Presentation.LoyaltyModel;
using United.Utility.Enum;
using United.Utility.Helper;
using MOBFOPCertificateTraveler = United.Mobile.Model.Shopping.FormofPayment.MOBFOPCertificateTraveler;
using MOBLegalDocument = United.Definition.MOBLegalDocument;
using ProfileFOPCreditCardResponse = United.Mobile.Model.Common.ProfileFOPCreditCardResponse;
using ProfileResponse = United.Mobile.Model.Common.ProfileResponse;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.MemberProfile.Domain
{
    public class MemberProfileBusiness : IMemberProfileBusiness
    {
        private readonly ICacheLog<MemberProfileBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly ICustomerProfile _customerProfile;
        private readonly IMemberProfileUtility _memberProfileUtility;
        private readonly IProfileXML _profileXML;
        private readonly IMerchandizingServices _merchandizingServices;
        private readonly ILoyaltyPromotionsService _statusLiftBannerService;
        private readonly IDPService _dpService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IMileagePlus _mileagePlus;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly PKDispenserPublicKey _pKDispenserPublicKey;
        private readonly IValidateHashPinService _validateHashPinService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly ICSLStatisticsService _cSLStatisticsService;
        private readonly ITraveler _traveler;
        private readonly ITravelerUtility _travelerUtility;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly IPaymentService _paymentService;
        private readonly IEmpProfile _empProfile;
        private readonly ICachingService _cachingService;
        private readonly IMPTraveler _mPTraveler;
        private readonly IReferencedataService _referencedataService;
        private readonly IPKDispenserService _pKDispenserService;
        private readonly IOmniCart _omniCart;
        private readonly IHeaders _headers;
        private readonly IFormsOfPayment _formsOfPayment;
        private readonly ISSOTokenKeyService _sSOTokenKeyService;
        private readonly IFeatureSettings _featureSettings;
        private readonly IShopBundleService _shopBundleService;
        private readonly IProvisionService _provisionService;
        private readonly IFeatureToggles _featureToggles;
        private readonly ICorporateProfile _corpProfile;

        public MemberProfileBusiness(ICacheLog<MemberProfileBusiness> logger
            , IConfiguration configuration
            , IDynamoDBService dynamoDBService
            , ISessionHelperService sessionHelperService
            , IMemberProfileUtility memberProfileUtility
            , ICustomerProfile customerProfile
            , IProfileXML profileXML
            , IMerchandizingServices merchandizingServices
            , ILoyaltyPromotionsService statusLiftBannerService
            , IDPService dpService
            , IShoppingUtility shoppingUtility
            , IMileagePlus mileagePlus
            , IShoppingSessionHelper shoppingSessionHelper
            , IValidateHashPinService validateHashPinService
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , ICSLStatisticsService cSLStatisticsService
            , ITraveler traveler
            , ITravelerUtility travelerUtility
            , IFFCShoppingcs fFCShoppingcs
            , IPaymentService paymentService
            , IEmpProfile empProfile
            , ICachingService cachingService
            , IMPTraveler mPTraveler
            , IOmniCart omniCart
            , IReferencedataService referencedataService
            , IPKDispenserService pKDispenserService
            , IFormsOfPayment formsOfPayment
            , IHeaders headers
            , ISSOTokenKeyService sSOTokenKeyService
            , IFeatureSettings featureSettings
            , IShopBundleService shopBundleService
            , IProvisionService provisionService
            , IFeatureToggles featureToggles
            , ICorporateProfile corpProfile
            )
        {
            _logger = logger;
            _configuration = configuration;
            _dynamoDBService = dynamoDBService;
            _sessionHelperService = sessionHelperService;
            _memberProfileUtility = memberProfileUtility;
            _customerProfile = customerProfile;
            _profileXML = profileXML;
            _merchandizingServices = merchandizingServices;
            _statusLiftBannerService = statusLiftBannerService;
            _dpService = dpService;
            _shoppingUtility = shoppingUtility;
            _mileagePlus = mileagePlus;
            _shoppingSessionHelper = shoppingSessionHelper;
            _validateHashPinService = validateHashPinService;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _cSLStatisticsService = cSLStatisticsService;
            _traveler = traveler;
            _travelerUtility = travelerUtility;
            _fFCShoppingcs = fFCShoppingcs;
            _paymentService = paymentService;
            _empProfile = empProfile;
            _cachingService = cachingService;
            _mPTraveler = mPTraveler;
            _referencedataService = referencedataService;
            _pKDispenserService = pKDispenserService;
            _omniCart = omniCart;
            _formsOfPayment = formsOfPayment;
            _headers = headers;
            _pKDispenserPublicKey = new PKDispenserPublicKey(_configuration, _cachingService, _dpService, _pKDispenserService, headers);
            _sSOTokenKeyService = sSOTokenKeyService;
            _featureSettings = featureSettings;
            _shopBundleService = shopBundleService;
            _provisionService = provisionService;
            _featureToggles = featureToggles;
            _corpProfile = corpProfile;
            ConfigUtility.UtilityInitialize(_configuration);
        }

        public async Task<MOBContactUsResponse> GetContactUsDetails(MOBContactUsRequest request)
        {
            var ListNames = new List<string> { "InternationalCallingCard" };
            MOBContactUsResponse response = new MOBContactUsResponse();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var title in ListNames)
            {
                stringBuilder.Append("'");
                stringBuilder.Append(title);
                stringBuilder.Append("'");
                stringBuilder.Append(",");
            }
            string reqTitles = stringBuilder.ToString().Trim(',');
            try
            {
                var CallingcardList = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(reqTitles, request.TransactionId, true);
                _logger.LogInformation("GetContactUsDetails-Business - LegalDocumentsForTitles Response {legaldocumentsresponse} and {transactionId}", JsonConvert.SerializeObject(CallingcardList), request.TransactionId);
                response.ContactUsUSACanada = GetContactUsUSACanada(request.MemberType, request.IsCEO, CallingcardList);
                response.ContactUSOutSideUSACanada = await GetContactUSOusideUSACanada(request.MemberType, request.IsCEO, CallingcardList);
            }
            catch (Exception ex)
            {
                _logger.LogError("GetContactUsDetails - OnPremSQLService-GetLegalDocumentsForTitles Error {message} {exceptionStackTrace} and {transactionId}", ex.Message, ex.StackTrace, _headers.ContextValues.SessionId);
            }

            return response;
        }

        public async Task<MOBCustomerPreferencesResponse> RetrieveCustomerPreferences(MOBCustomerPreferencesRequest request)
        {
            MOBCustomerPreferencesResponse response = new MOBCustomerPreferencesResponse();

            Session session = null;
            if (!string.IsNullOrEmpty(request.SessionId))
            {
                session = await _shoppingSessionHelper.GetShoppingSession(request.SessionId);
                request.SessionId = session.SessionId;
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            _logger.LogInformation("RetrieveCustomerPreferences {Request} and {SessionID}", request, request.SessionId);

            response = await _customerProfile.RetrieveCustomerPreferences(request, session.Token);
            response.SessionId = request.SessionId;
            response.TransactionId = request.TransactionId;
            response.LanguageCode = request.LanguageCode;

            _logger.LogInformation("RetrieveCustomerPreferences {Response} and {SessionID}", response, request.SessionId);
            return await Task.FromResult(response);
        }

        public async Task<MOBCustomerProfileResponse> GetProfileOwner(MOBCustomerProfileRequest request)
        {
            {
                string sessionId = string.Empty;
                MOBCustomerProfileResponse response = new MOBCustomerProfileResponse();

                request.MileagePlusNumber = request.MileagePlusNumber.ToUpper();
                response.MileagePlusNumber = request.MileagePlusNumber;
                bool validWalletRequest = false;

                if (_configuration.GetValue<bool>("ValidateGetProfileCSLRequest"))
                {
                    validWalletRequest = await _memberProfileUtility.IsValidGetProfileCSLRequest(request.TransactionId, request.DeviceId, request.Application.Id, request.MileagePlusNumber, request.CustomerId, request.SessionId);
                }
                else
                {
                    validWalletRequest = await _memberProfileUtility.IsValidDeviceRequest(request.TransactionId, request.DeviceId, request.Application.Id, request.MileagePlusNumber, request.SessionId);
                }

                if (validWalletRequest)
                {
                    #region

                    Session session = new Session();

                    response.TransactionId = request.TransactionId;
                    string authToken = string.Empty;

                    request.HashPinCode = (request.HashPinCode == null ? string.Empty : request.HashPinCode);
                    bool validateMPWithHash = false;

                    var tupleRes = await ValidateHashPinAndGetAuthToken<bool>(request.MileagePlusNumber, request.HashPinCode, request.Application.Id, request.DeviceId, request.Application.Version.Major, authToken, request.TransactionId, request.SessionId);
                    validateMPWithHash = tupleRes.returnValue;
                    authToken = tupleRes.validAuthToken;
                    if (!await _featureSettings.GetFeatureSettingValue("DisableSQLValidateHashPinAndGetAuthToken").ConfigureAwait(false))
                    {
                        if (!validateMPWithHash) // This is to handle the MP Sign In at home using OLD ValidateMP() REST Web API Service method()
                        {
                            validateMPWithHash = await _memberProfileUtility.ValidateAccountFromCache(request.MileagePlusNumber, request.HashPinCode, request.SessionId);
                        }
                    }
                    if (validateMPWithHash)
                    {
                        if (!string.IsNullOrEmpty(request.SessionId) && request.TransactionPath != null &&
                            request.TransactionPath.ToUpper().Equals("VIEWRESPATH"))
                        {
                            session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string> { request.SessionId, session.ObjectName });
                        }
                        else
                        {
                            session = await _shoppingSessionHelper.CreateShoppingSession(request.Application.Id, request.DeviceId,
                                request.Application.Version.Major, request.TransactionId, request.MileagePlusNumber, null, false,
                                false, false);
                            //TODO - CHECK TanishaG
                            response.SessionId = session.SessionId;
                            sessionId = session.SessionId;
                            var sessionData = await _sessionHelperService.SaveSession<MOBCustomerProfileResponse>(response, session.SessionId, new List<string> { session.SessionId, new MOBCustomerProfileResponse().GetType().FullName }, new MOBCustomerProfileResponse().GetType().FullName);

                        }
                        response.SessionId = session.SessionId;
                        sessionId = session.SessionId;
                        _logger.LogInformation("GetProfileOwner {Request} and {SessionID}", request.Application.Id, request.Application.Version.Major, request.DeviceId, request, request.SessionId);

                        MOBCPProfileRequest profileReq = GetMOBCPProfileRequest(request);
                        profileReq.Token = session.Token;
                        profileReq.SessionId = sessionId;

                        response.Profiles = await _customerProfile.GetProfile(profileReq);

                        #region - Fix by Nizam for 1279 - Excluding the Ghost cards from profile
                        if (request.TransactionPath.ToUpper().Trim() == "ViewResPath".ToUpper().Trim() && response.Profiles[0].Travelers != null && response.Profiles[0].Travelers[0].CreditCards != null)
                            response.Profiles[0].Travelers[0].CreditCards = response.Profiles[0].Travelers[0].CreditCards.Where(x => x.IsCorporate == false).ToList();
                        #endregion

                        if (request.TransactionPath.ToUpper().Trim() == "ViewResPath".ToUpper().Trim() && response.Profiles[0].Travelers[0].CreditCards != null)
                        {
                            foreach (var creditCard in response.Profiles[0].Travelers[0].CreditCards)
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

                        if (response.Profiles != null &&
                            response.Profiles[0].Travelers != null &&
                            response.Profiles[0].Travelers.Any() &&
                            response.Profiles[0].Travelers[0] != null)
                        {
                            var ownerProfile = response.Profiles[0].Travelers[0];
                            bool isValidMpDetailRequest = ValidateMPNumberAndCustomerID(request.CustomerId,
                                request.MileagePlusNumber, ownerProfile.CustomerId,
                                ownerProfile.MileagePlus.MileagePlusId);
                            if (!isValidMpDetailRequest)
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("MPValidationErrorMessage") as string);
                            }
                            if (request.IsRequirePKDispenserPublicKey)
                            {
                                response.PKDispenserPublicKey = await _pKDispenserPublicKey.GetCachedOrNewpkDispenserPublicKey(request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId, session.Token, session.CatalogItems);

                                if (!_configuration.GetValue<bool>("DisableManageResChanges23C"))
                                {
                                    response.PKDispenserPublicKey = await _pKDispenserPublicKey.GetCachedOrNewpkDispenserPublicKey(request.Application.Id, request.Application.Version.Major,request.DeviceId,  request.TransactionId, session.Token, request.Flow, session.CatalogItems);
                                }
                            }
                            List<MOBAddress> lstAddress = new List<MOBAddress>();
                            List<MOBCreditCard> lstCreditCard = GetProfileOwnerCreditCardList(response.Profiles[0], ref lstAddress, string.Empty);

                            response.SelectedCreditCard = (lstCreditCard != null && lstCreditCard.Count > 0) ? lstCreditCard[0] : null;
                            response.SelectedAddress = (lstAddress != null && lstAddress.Count > 0) ? lstAddress[0] : null;

                            ProfileFOPCreditCardResponse profilePersist = new ProfileFOPCreditCardResponse();
                            profilePersist.SessionId = session.SessionId;
                            profilePersist.Request = request;
                            profilePersist.Response = response;
                            await _sessionHelperService.SaveSession<ProfileFOPCreditCardResponse>(profilePersist, profilePersist.SessionId, new List<string> { profilePersist.SessionId, profilePersist.ObjectName }, profilePersist.ObjectName); // Saving to persist here first time for use at AutoRegisterTravelerWithProfileOwner()
                                                                                                                                                                                                                                                  //Mobile AppMOBILE-1220 Cancel / Refund Checkout
                                                                                                                                                                                                                                                  //VormetricUtility vormetricUtility = new VormetricUtility(profile.LogEntries);
                            await SaveProfilePersistForAwardCancelPath(profilePersist, request.TransactionPath, request.DeviceId, request.MileagePlusNumber, profilePersist.SessionId);
                        }

                        if (_shoppingUtility.IsManageResETCEnabled(request.Application.Id, request.Application.Version.Major) && (request.Flow == FlowType.VIEWRES.ToString() || request.Flow == FlowType.VIEWRES_SEATMAP.ToString()))
                        {

                            MOBShoppingCart shoppingCart = new MOBShoppingCart();
                            shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(sessionId, shoppingCart.ObjectName, new List<string> { sessionId, shoppingCart.ObjectName }).ConfigureAwait(false);

                            if (shoppingCart == null)
                            {
                                shoppingCart = new MOBShoppingCart();
                            }
                            await _shoppingUtility.InitialiseShoppingCartAndDevfaultValuesForETC(shoppingCart, shoppingCart.Products, shoppingCart.Flow);
                            AssignDefaultCreditCardandBillingAddress(shoppingCart, response.Profiles);
                            shoppingCart.ProfileTravelerCertificates = await GetProfileCertificates(request.MileagePlusNumber, request.Application, request.SessionId, request.SessionId, request.DeviceId);

                            if (session.CatalogItems != null && session.CatalogItems.Count > 0 && _shoppingUtility.IsEnablePartnerProvision(session?.CatalogItems, request.Flow, request.Application.Id, request.Application.Version.Major) && !string.IsNullOrEmpty(request.MileagePlusNumber))
                            {
                                shoppingCart.PartnerProvisionDetails = await GetPartnerProvisionDetails(request.MileagePlusNumber, session.Token);
                            }

                            response.ShoppingCart = shoppingCart;
                            var owner = response.Profiles[0].Travelers?.Find(t => t.IsProfileOwner);
                            if (shoppingCart.FormofPaymentDetails?.Phone == null && owner?.ReservationPhones?.Count > 0)
                            {
                                shoppingCart.FormofPaymentDetails.Phone = owner.ReservationPhones[0];
                            }
                            if (shoppingCart.FormofPaymentDetails?.Email == null && owner?.ReservationEmailAddresses?.Count > 0)
                            {
                                shoppingCart.FormofPaymentDetails.EmailAddress = owner.ReservationEmailAddresses[0].EmailAddress;
                                shoppingCart.FormofPaymentDetails.Email = owner.ReservationEmailAddresses[0];
                            }
                            await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, request.SessionId, new List<string> { request.SessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName);
                        }

                        if (IsPostBookingPromoCodeEnabled(request.Application.Id, request.Application.Version.Major) && request.Flow == FlowType.POSTBOOKING.ToString())
                        {
                            response.ShoppingCart = await AssignDefaultFopDetails(request.SessionId, response.Profiles, session, request);
                        }

                        if (request?.CatalogItems != null && request?.CatalogItems.Count > 0 && request.Flow == FlowType.CHECKIN.ToString() && _shoppingUtility.IsEnablePartnerProvision(request?.CatalogItems, request.Flow, request.Application.Id, request.Application.Version.Major) && !string.IsNullOrEmpty(request.MileagePlusNumber))
                        {
                            session.CatalogItems = request?.CatalogItems;
                            await _sessionHelperService.SaveSession<Session>(session, request.SessionId, new List<string> { request.SessionId, session.ObjectName }, session.ObjectName);
                            response.PartnerProvisionDetails = await GetPartnerProvisionDetails(request.MileagePlusNumber, session.Token);
                        }
                    }
                    else
                    {
                        throw new MOBUnitedException(_configuration.GetValue<string>("bugBountySessionExpiredMsg"));
                    }
                    #endregion
                }
                else
                {
                    response.Exception = new MOBException();
                    response.Exception.Message = _configuration.GetValue<string>("GetProfileException") as string; //need to verify with venkat for exception and message
                    _logger.LogError("GetProfileOwner {MOBUnitedException} and {SessionID}", response.Exception.Message, sessionId);
                }

                _logger.LogInformation("GetProfileOwner {Response} and {SessionID}", response, sessionId);


                if (request.CustomerId == 0)
                {
                    _logger.LogError("GetProfileOwner - CustomerID = 0 Exception {MPNumber} and {SessionID}", request.MileagePlusNumber, sessionId);
                }
                return response;
            }
        }
        public async Task<PartnerProvisionDetails> GetPartnerProvisionDetails(string mileagePlusNumber, string token)
        {
            PartnerProvisionDetails objPartnerProvisionDetails  = new PartnerProvisionDetails();
            bool enableProvisionForFamilyandFriends = await _featureSettings.GetFeatureSettingValue("EnablePartnerProvision_FamilyNFriends").ConfigureAwait(false);
            bool isPartnerProvisionEnabled = true;
            if (enableProvisionForFamilyandFriends)
            {
                var _cslReq = new
                {
                    ChannelName = _configuration.GetValue<string>("Shopping - ChannelType"),
                    FailureRedirectUrl = "",
                    PartnerRequestIdentifier = "",
                    SuccessRedirectUrl = "",
                    MPNumber = mileagePlusNumber
                };
                string path = "/IsPartnerProvisionEnabled";
                var jsonResponse = await _provisionService.CSL_PartnerProvisionCall(token, path, JsonConvert.SerializeObject(_cslReq));
                isPartnerProvisionEnabled = !string.IsNullOrEmpty(jsonResponse) ? JsonConvert.DeserializeObject<bool>(jsonResponse) : false;
            }

            if (isPartnerProvisionEnabled)
            {
                objPartnerProvisionDetails.IsEnableProvisionLogin = true;
                objPartnerProvisionDetails.ProvisionLoginMessage = _configuration.GetValue<string>("ProvisionLoginMessage");
            }
            return objPartnerProvisionDetails;
        }

        public async Task<(bool returnValue, string validAuthToken)> ValidateHashPinAndGetAuthToken<T>(string accountNumber, string hashPinCode, int applicationId, string deviceId, string appVersion, string validAuthToken, string transactionId, string sessionId)
        {
            bool rtnValue = false;
            _logger.LogInformation("ValidateHashPinAndGetAuthToken - Hashpin Validation request {@AccountNumber} {@HashPinCode}", accountNumber, hashPinCode);
            var response = await new HashPin(_logger, _configuration, _validateHashPinService, _dynamoDBService, _headers, _featureSettings).ValidateHashPinAndGetAuthTokenDynamoDB(accountNumber, hashPinCode, applicationId, deviceId, appVersion).ConfigureAwait(false);
            if (response != null && response.HashPincode == hashPinCode)
            {
                validAuthToken = response.AuthenticatedToken?.ToString();
                if (response.IsTokenValid.ToLower() == "true")
                    rtnValue = true;
            }

            return (rtnValue, validAuthToken);
        }


        public async Task<MPAccountSummaryResponse> GetAccountSummaryWithMemberCardPremierActivity(MPAccountValidationRequest request)
        {
            MPAccountSummaryResponse response = new MPAccountSummaryResponse();
            if (_configuration.GetValue<bool>("MyAccountForceUpdateToggle") == true
                && _mileagePlus.IsPremierStatusTrackerSupportedVersion(request.Application.Id, request.Application.Version.Major) == false)
            {
                string myAccountForceUpdate = !string.IsNullOrEmpty(_configuration.GetValue<string>("MyAccountForceUpdateMessage")) ? _configuration.GetValue<string>("MyAccountForceUpdateMessage") : "A newer version of the United Airlines app is available. Please update to the new version to continue.";
                throw new MOBUnitedException(myAccountForceUpdate);
            }

            string dpToken = string.Empty;
            string authToken = string.Empty;
            bool validRequest = false;
            var tupleRes = await ValidateHashPinAndGetAuthToken<bool>(request.MileagePlusNumber, request.HashValue, request.Application.Id, request.DeviceId, request.Application.Version.Major, authToken, request.TransactionId, request.SessionId);
            validRequest = tupleRes.returnValue;
            authToken = tupleRes.validAuthToken;
            if (!validRequest)
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("bugBountySessionExpiredMsg"));
            }
            dpToken = await _dpService.GetAnonymousToken(request.Application.Id, request.DeviceId, _configuration).ConfigureAwait(false);
            response.OPAccountSummary = null;
            if (_configuration.GetValue<bool>("EnableUCBPhase1_MobilePhase1Changes"))
            {
                await _mileagePlus.GetAccountSummaryWithPremierActivityV2(request, true, dpToken);
            }
            else
            {
                await _mileagePlus.GetAccountSummaryWithPremierActivity(request, true, dpToken);
            }
            response.isUASubscriptionsAvailable = false;

            #region GetUASubscriptions
            string channelId = string.Empty;
            string channelName = string.Empty;
            if (_configuration.GetValue<bool>("EnabledMERCHChannels"))
            {
                string merchChannel = "MOBMYRES";
                _merchandizingServices.SetMerchandizeChannelValues(merchChannel, ref channelId, ref channelName);
            }
            Session session = CreateSubscriptionSession();
            response.UASubscriptions = await _merchandizingServices.GetUASubscriptions(request.MileagePlusNumber, 1, request.TransactionId, channelId, channelName);

            if (response.UASubscriptions != null && response.UASubscriptions.SubscriptionTypes != null && response.UASubscriptions.SubscriptionTypes.Count > 0)
            {
                response.isUASubscriptionsAvailable = true;
            }

            #endregion

            #region GetTSAFLAGSTATUS

            response.OPAccountSummary.IsMPAccountTSAFlagON = await _memberProfileUtility.IsTSAFlaggedAccount(request.MileagePlusNumber, request.SessionId);//utility
            if (response.OPAccountSummary.IsMPAccountTSAFlagON)
            {
                if (_configuration.GetValue<bool>("NewServieCall_GetProfile_AllTravelerData"))
                {
                    try
                    {
                        response.OPAccountSummary.IsMPAccountTSAFlagON = await _mileagePlus.GetProfile_AllTravelerData(request.MileagePlusNumber, request.TransactionId, dpToken, request.Application.Id, request.Application.Version.Major, request.DeviceId);
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError("GetAccountSummaryMemberCardPremierActivity Error {message} {StackTrace} and {session}", ex.Message, ex.StackTrace, request.SessionId);
                    }
                }
                else
                {
                    MOBProfileRequest temp_request = new MOBProfileRequest();
                    temp_request.AccessCode = string.Empty;
                    temp_request.LanguageCode = "en-US";
                    temp_request.MileagePlusNumber = request.MileagePlusNumber;
                    temp_request.PinCode = "****";
                    temp_request.SessionID = request.TransactionId;
                    temp_request.MileagePlusNumber = temp_request.MileagePlusNumber;  //the setter should force to uppercase
                    temp_request.IncludeSecureTravelers = true;
                    temp_request.TransactionId = "FromAccountSummaryCall";

                    try
                    {
                        MOBProfile temp_response = await _profileXML.GetProfile(temp_request);
                        if (temp_response != null)
                        {
                            foreach (MOBTraveler traveler in temp_response.Travelers)
                            {
                                if (traveler.MileagePlusNumber.Equals(temp_request.MileagePlusNumber))
                                {
                                    response.OPAccountSummary.IsMPAccountTSAFlagON = traveler.IsTSAFlagON;
                                    break;
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        _logger.LogError("GetAccountSummaryMemberCardPremierActivity Error {message} {StackTrace} and {session}", ex.Message, ex.StackTrace, request.SessionId);
                    }
                }
                if (response.OPAccountSummary.IsMPAccountTSAFlagON)
                {
                    response.OPAccountSummary.TSAMessage = _configuration.GetValue<string>("TSAAccountMessage");
                }
            }
            #endregion

            #region GetStatusLiftBanner
            response.OPAccountSummary.StatusLiftBanner = await GetStatusLiftBanner(request);
            #endregion

            return await Task.FromResult(response);

        }

        public async Task<MOBCPProfileResponse> GetProfileCSL_CFOP(MOBCPProfileRequest request)
        {
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            MOBCPProfileResponse response = new MOBCPProfileResponse();
            response.SessionId = request.SessionId;
            response.CartId = request.CartId;
            request.MileagePlusNumber = request.MileagePlusNumber.ToUpper();
            try
            {
                Session session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId);

                //bool token = Authentication.ValidateAuthenticateCSSToken(request.Application.Id, request.DeviceId,request.Application.Version.Major, request.TransactionId, session, profile.LogEntries, traceSwitch);
                request.Token = session.Token;
                request.CartId = session.CartId;

                bool validWalletRequest = false;

                if (_configuration.GetValue<string>("ValidateGetProfileCSLRequest") != null && Convert.ToBoolean(_configuration.GetValue<string>("ValidateGetProfileCSLRequest").ToString()))
                {
                    validWalletRequest = await _memberProfileUtility.IsValidGetProfileCSLRequest(request.TransactionId, request.DeviceId, request.Application.Id, request.MileagePlusNumber, request.CustomerId, request.SessionId);
                }
                else
                {
                    validWalletRequest = await _memberProfileUtility.IsValidDeviceRequest(request.TransactionId, request.DeviceId, request.Application.Id, request.MileagePlusNumber, request.SessionId);
                }
                if (validWalletRequest)
                {
                    #region
                    request.ReturnAllSavedTravelers = true;
                    response.TransactionId = request.TransactionId;
                    string authToken = string.Empty;
                    request.HashPinCode = (request.HashPinCode == null ? string.Empty : request.HashPinCode);
                    bool validateMPWithHash = false;
                    var tupleResponse = await _memberProfileUtility.ValidateHashPinAndGetAuthToken(request.MileagePlusNumber, request.HashPinCode, request.Application.Id, request.DeviceId, request.Application.Version.Major, authToken, request.TransactionId, request.SessionId);
                    validateMPWithHash = tupleResponse.Item1;
                    authToken = tupleResponse.validAuthToken;
                    if (!validateMPWithHash) // This is to handle the MP Sign In at home using OLD ValidateMP() REST Web API Service method()
                    {
                        validateMPWithHash = await _memberProfileUtility.ValidateAccountFromCache(request.MileagePlusNumber, request.HashPinCode);
                    }
                    if (validateMPWithHash)
                    {
                        #region Chase promo RTI
                        if (_memberProfileUtility.EnableChaseOfferRTI(request.Application.Id, request.Application.Version.Major))
                        {
                            if (_configuration.GetValue<bool>("EnableByPassChaseOfferForOldClients") && !GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "AndroidChaseCCEPromoVersion", "iPhoneChaseCCEPromoVersion", "", "", true, _configuration))
                            {
                                if (!string.IsNullOrEmpty(request.ChaseAdType))
                                    request.ChaseAdType = null;
                            }

                            if (!string.IsNullOrEmpty(request.ChaseAdType) && string.IsNullOrEmpty(session.EmployeeId) && !session.IsCorporateBooking)
                            {
                                await _customerProfile.GetChaseCCStatement(request);
                            }
                        }
                        #endregion Chase promo RTI
                        request.Path = request.Flow;
                        response.Profiles = await _customerProfile.GetProfile(request);

                        if (response.Profiles != null &&
                            response.Profiles.Any() &&
                            response.Profiles[0].Travelers != null &&
                            response.Profiles[0].Travelers.Any() &&
                            response.Profiles[0].Travelers[0] != null)
                        {
                            var ownerProfile = response.Profiles[0].Travelers[0];
                            //Bug 241287 : added below null check, as it is failing in preprod - 7th Feb 2018 j.srinivas
                            if (ownerProfile != null && ownerProfile.MileagePlus != null)
                            {
                                bool isValidMpDetailRequest = ValidateMPNumberAndCustomerID(request.CustomerId,
                                    request.MileagePlusNumber, ownerProfile.CustomerId,
                                    ownerProfile.MileagePlus.MileagePlusId);
                                if (!isValidMpDetailRequest)
                                {
                                    throw new MOBUnitedException(string.Format("Invalid Account Number or Pin."));
                                }
                            }
                        }

                        ProfileResponse profilePersist = new ProfileResponse();
                        profilePersist.SessionId = session.SessionId;
                        profilePersist.CartId = session.CartId;
                        profilePersist.Request = ObjectToObjectCasting<MOBCustomerProfileRequest, MOBCPProfileRequest>(request);

                        if (_configuration.GetValue<bool>("EnableFeatureSettingsChanges")
                           ? await _featureSettings.GetFeatureSettingValue("DisableFixforDefaultSpecialMealsforRegisterTraveler_MOBILE29790").ConfigureAwait(false)
                           : _configuration.GetValue<bool>("DisableFixforDefaultSpecialMealsforRegisterTraveler_MOBILE29790"))
                        {
                            profilePersist.Response = ObjectToObjectCasting<MOBCustomerProfileResponse, MOBCPProfileResponse>(response);
                            await _sessionHelperService.SaveSession<ProfileResponse>(profilePersist, profilePersist.SessionId, new List<string> { profilePersist.SessionId, profilePersist.ObjectName }, profilePersist.ObjectName); // Saving to persist here first time for use at AutoRegisterTravelerWithProfileOwner()
                        }

                        #region Update Booking Path Persist Reservation IsSignedInWithMP value and Save to session
                        Reservation bookingPathReservation = new Reservation();
                        bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                        bool isGetProfileCalledforProfileOWner = bookingPathReservation.IsGetProfileCalledForProfileOwner;
                        if (!_configuration.GetValue<bool>("TurnOffInfantasProfileBugFIx"))
                        {
                            isGetProfileCalledforProfileOWner = false;
                        }
                        bookingPathReservation.IsSignedInWithMP = true; // why here not at Validate Account because if already signed customer with in the valid sign in session intervel client will not call validate account client will call get profile.
                        bookingPathReservation.GetALLSavedTravelers = request.ReturnAllSavedTravelers;
                        bookingPathReservation.IsGetProfileCalledForProfileOwner = true;
                        #region Bug-63507 Fix
                        //63507-Incorrect page displayed when selecting back button at FareLock and changing to Continue to purchase
                        //Fixed applied 10/27/2016 - Pradeep Patil
                        //Check and remove farelock if get Profile is called after Register Farelock
                        if (!request.ReturnAllSavedTravelers || bookingPathReservation.NumberOfTravelers > 1 ||
                            (bookingPathReservation.ShopReservationInfo != null && bookingPathReservation.ShopReservationInfo.IsCorporateBooking))
                        //UnRegister Farelock in Corporate Booking path backbutton press; Adding IsCorporateBooking, because Client Will call GetProfile only once the in this path with ReturnAllSavedTravelers True and numberOfTravelers is always 1
                        {
                            bool containsFareLock = false;
                            List<TravelOption> options = bookingPathReservation.TravelOptions;
                            if (options != null && options.Count > 0)
                            {
                                foreach (TravelOption option in options)
                                {
                                    if (option != null && !string.IsNullOrEmpty(option.Key) && option.Key.ToUpper() == "FARELOCK")
                                    {
                                        containsFareLock = true;
                                        break;
                                    }
                                }
                            }

                            if (containsFareLock)
                            {
                                bookingPathReservation.TravelOptions = null;
                                bookingPathReservation.UnregisterFareLock = true;
                            }
                        }
                        #endregion


                        if (!(_memberProfileUtility.IsEnableOmniCartReleaseCandidateOneChanges(request.Application.Id, request.Application.Version.Major) && _memberProfileUtility.IsOmniCartSavedTrip(bookingPathReservation)))
                        {
                            //Venakt - Need to Save the isSignedInWithMP value to persist and save it as Auto Register Traveler will save the travelers to persist.
                            #region
                            SelectTrip persistSelectTripObj = await _sessionHelperService.GetSession<SelectTrip>(request.SessionId, new SelectTrip().ObjectName, new List<string> { request.SessionId, new SelectTrip().ObjectName }).ConfigureAwait(false);
                            if (persistSelectTripObj != null && !isGetProfileCalledforProfileOWner)
                            {
                                bookingPathReservation.Prices = persistSelectTripObj.Responses[persistSelectTripObj.LastSelectTripKey].Availability.Reservation.Prices;
                            }
                            #endregion
                        }

                        #region Special Needs

                        if (_shoppingUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
                        {
                            if (response.Profiles != null && response.Profiles.Any() && response.Profiles[0] != null && response.Profiles[0].Travelers != null && response.Profiles[0].Travelers.Any()
                                && bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.SpecialNeeds != null)
                            {
                                Action<string, MOBCPTraveler> addSSRMsgToTraveler = (desc, traveler) =>
                                {
                                    var travelSSRInfoMsg = new MOBItem { CurrentValue = desc };

                                    if (traveler.SelectedSpecialNeedMessages == null)
                                    {
                                        traveler.SelectedSpecialNeedMessages = new List<MOBItem> { travelSSRInfoMsg };
                                    }
                                    else
                                    {
                                        traveler.SelectedSpecialNeedMessages.Add(travelSSRInfoMsg);
                                    }
                                };

                                foreach (var traveler in response.Profiles[0].Travelers.Where(t => t.AirPreferences != null && t.AirPreferences.Any()))
                                {
                                    var savedSpecialNeeds = GetSpecialNeedsFromTravelerAirPrefernces(traveler.AirPreferences[0]);
                                    if (savedSpecialNeeds == null || !savedSpecialNeeds.Any())
                                        continue;

                                    if (savedSpecialNeeds != null && savedSpecialNeeds.Any())
                                    {
                                        traveler.SelectedSpecialNeedMessages = new List<MOBItem>();
                                        bool IsServiceAnimals = (savedSpecialNeeds.Any(_ => _.Type == TravelSpecialNeedType.ServiceAnimalType.ToString()) && (bookingPathReservation.ShopReservationInfo2.SpecialNeeds.ServiceAnimals == null || bookingPathReservation.ShopReservationInfo2.SpecialNeeds.ServiceAnimals.Count == 0));
                                        bool IsSpecialMeals = (bookingPathReservation.ShopReservationInfo2.SpecialNeeds.SpecialMeals != null && bookingPathReservation.ShopReservationInfo2.SpecialNeeds.SpecialMeals.Any() && !bookingPathReservation.ShopReservationInfo2.SpecialNeeds.SpecialMeals.Any(_ => savedSpecialNeeds.Select(x => x.Code).Contains(_.Code)));
                                        if (IsServiceAnimals && IsSpecialMeals)
                                        {
                                            if (savedSpecialNeeds.Exists(_ => _.Type == TravelSpecialNeedType.SpecialMeal.ToString()))
                                            {
                                                traveler.SelectedSpecialNeedMessages.Add(new MOBItem { CurrentValue = string.Format(_configuration.GetValue<string>("SSRItinerarySpecialMealsAndServiceAnimalNotAvailableMsg"), savedSpecialNeeds.Where(_ => _.Type == TravelSpecialNeedType.SpecialMeal.ToString()).FirstOrDefault().DisplayDescription) });
                                            }
                                        }
                                        else
                                        {
                                            if (IsServiceAnimals)
                                            {
                                                traveler.SelectedSpecialNeedMessages.Add(new MOBItem { CurrentValue = _configuration.GetValue<string>("SSRItineraryServiceAnimalNotAvailableMsg") });
                                            }
                                            else if (IsSpecialMeals)
                                            {
                                                if (savedSpecialNeeds.Exists(_ => _.Type == TravelSpecialNeedType.SpecialMeal.ToString()))
                                                {
                                                    traveler.SelectedSpecialNeedMessages.Add(new MOBItem { CurrentValue = string.Format(_configuration.GetValue<string>("SSRItinerarySpecialMealsNotAvailableMsg"), savedSpecialNeeds.Where(_ => _.Type == TravelSpecialNeedType.SpecialMeal.ToString()).FirstOrDefault().DisplayDescription) });
                                                }
                                            }
                                        }
                                    }

                                    traveler.SelectedSpecialNeeds =
                                       await _travelerUtility.ValidateSpecialNeedsAgaintsMasterList(savedSpecialNeeds, bookingPathReservation.ShopReservationInfo2.SpecialNeeds, request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false);
                                    if (traveler.SelectedSpecialNeeds == null || !traveler.SelectedSpecialNeeds.Any())
                                        continue;
                                }
                            }
                        }
                        #endregion

                        if (_configuration.GetValue<bool>("EnableFeatureSettingsChanges") 
                            ? !await _featureSettings.GetFeatureSettingValue("DisableFixforDefaultSpecialMealsforRegisterTraveler_MOBILE29790").ConfigureAwait(false)
                            : !_configuration.GetValue<bool>("DisableFixforDefaultSpecialMealsforRegisterTraveler_MOBILE29790"))
                        {
                            profilePersist.Response = ObjectToObjectCasting<MOBCustomerProfileResponse, MOBCPProfileResponse>(response);
                            await _sessionHelperService.SaveSession<ProfileResponse>(profilePersist, profilePersist.SessionId, new List<string> { profilePersist.SessionId, profilePersist.ObjectName }, profilePersist.ObjectName); // Saving to persist here to add traveler default selectedSpecialMeals to use at AutoRegisterTravelerWithProfileOwner()
                        }

                        if (bookingPathReservation.ShopReservationInfo2.IsForceSeatMap)
                        {
                            if (!request.ReturnAllSavedTravelers && bookingPathReservation.NumberOfTravelers == 1)
                            {
                                bookingPathReservation.TravelersCSL = new SerializableDictionary<string, MOBCPTraveler>();
                                bookingPathReservation.TravelerKeys = new List<string>();
                                bookingPathReservation.TravelOptions = new List<TravelOption>();
                            }
                        }

                        bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL = new List<MOBCPTraveler>();
                        foreach (MOBCPTraveler cpTraveler in response.Profiles[0].Travelers)
                        {

                            cpTraveler.PaxID = cpTraveler.PaxIndex + 1;
                            cpTraveler.IsPaxSelected = false;
                            bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Add(cpTraveler);
                        }

                        if (_travelerUtility.IsEnableNavigation(bookingPathReservation.IsReshopChange) && !bookingPathReservation.IsReshopChange && bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.IsArrangerBooking)
                        {
                            bookingPathReservation.ShopReservationInfo2.NextViewName = "TravelList";
                        }
                        //else
                        //{
                        if (!IsArrangerNavigation(request, bookingPathReservation))
                        {
                            if (bookingPathReservation.NumberOfTravelers == 1 && bookingPathReservation.TravelersCSL == null)
                            {
                                bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL[0].IsPaxSelected = true;
                            }
                            else if (bookingPathReservation.TravelersCSL != null && bookingPathReservation.TravelersCSL.Count() > 0)
                            {
                                foreach (var selectedTraveler in bookingPathReservation.TravelersCSL)
                                {
                                    var eligibleTraveler = bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.FirstOrDefault(p => p.Key == selectedTraveler.Value.Key);
                                    if (eligibleTraveler != null)
                                    {
                                        eligibleTraveler.IsPaxSelected = true;
                                    }
                                }
                                //}

                                //if (Utility.IsEnableNavigation(bookingPathReservation.IsReshopChange))
                                //{

                                //    if (bookingPathReservation.NumberOfTravelers == 1){
                                //        bookingPathReservation.ShopReservationInfo2.NextViewName = "TravelOption";
                                //        bookingPathReservation.ShopReservationInfo2.IsForceSeatMapInRTI = true;
                                //    }
                                //    else
                                //    {
                                //        bookingPathReservation.ShopReservationInfo2.NextViewName = "TravelList";
                                //    }
                                //}
                            }
                        }
                        if (_memberProfileUtility.IsEnableOmniCartReleaseCandidateOneChanges(request.Application.Id, request.Application.Version.Major) && _memberProfileUtility.IsOmniCartSavedTrip(bookingPathReservation))
                        {
                            bool isEnableExtraSeatReasonFixInOmniCartFlow = await _shoppingUtility.IsEnableExtraSeatReasonFixInOmniCartFlow().ConfigureAwait(false);
                            _memberProfileUtility.CompareOmnicartTravelersWithSavedTravelersandUpdate(bookingPathReservation, isEnableExtraSeatReasonFixInOmniCartFlow);
                        }

                        _memberProfileUtility.ValidateTravelersForCubaReason(bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL, bookingPathReservation.IsCubaTravel);

                        await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName);
                        response.Reservation = new MOBSHOPReservation(_configuration, _cachingService);
                        response.Reservation.PointOfSale = bookingPathReservation.PointOfSale;
                        response.Reservation.Trips = bookingPathReservation.Trips;
                        response.Reservation.Prices = bookingPathReservation.Prices;
                        response.Reservation.Taxes = bookingPathReservation.Taxes;
                        response.Reservation.NumberOfTravelers = bookingPathReservation.NumberOfTravelers;
                        response.Reservation.TravelOptions = bookingPathReservation.TravelOptions;
                        response.Reservation.FareRules = bookingPathReservation.FareRules;
                        response.Reservation.LMXFlights = bookingPathReservation.LMXFlights;
                        if (bookingPathReservation.IsCubaTravel)
                        {
                            response.Reservation.IsCubaTravel = bookingPathReservation.IsCubaTravel;
                            response.Reservation.CubaTravelInfo = bookingPathReservation.CubaTravelInfo;
                        }

                        response.Reservation.FormOfPaymentType = bookingPathReservation.FormOfPaymentType;
                        if ((bookingPathReservation.FormOfPaymentType == MOBFormofPayment.PayPal || bookingPathReservation.FormOfPaymentType == MOBFormofPayment.PayPalCredit) && bookingPathReservation.PayPal != null)
                        {
                            response.Reservation.PayPal = bookingPathReservation.PayPal;
                            response.Reservation.PayPalPayor = bookingPathReservation.PayPalPayor;
                        }
                        if (bookingPathReservation.FormOfPaymentType == MOBFormofPayment.Masterpass)
                        {
                            if (bookingPathReservation.MasterpassSessionDetails != null)
                                response.Reservation.MasterpassSessionDetails = bookingPathReservation.MasterpassSessionDetails;
                            if (bookingPathReservation.Masterpass != null)
                                response.Reservation.Masterpass = bookingPathReservation.Masterpass;
                        }
                        if (bookingPathReservation.FOPOptions != null && bookingPathReservation.FOPOptions.Count > 0) //FOP Options Fix Venkat 12/08
                        {
                            response.Reservation.FOPOptions = bookingPathReservation.FOPOptions;
                        }
                        if (await _featureToggles.IsEnableU4BForMultipax(request.Application.Id, request?.Application?.Version?.Major, session?.CatalogItems))
                        {
                            try
                            {
                                bool isMultipaxAllowed = response?.Profiles?.Any(a => a.CorporateData?.IsMultiPaxAllowed == true) ?? false;
                                bookingPathReservation.ShopReservationInfo2.CorpMultiPaxInfo = _travelerUtility.corpMultiPaxInfo(session.IsCorporateBooking, session.IsArrangerBooking, isMultipaxAllowed, response.Reservation.RewardPrograms);
                                bookingPathReservation.ShopReservationInfo2.IsShowAddNewTraveler = session.IsArrangerBooking || (session.IsCorporateBooking && isMultipaxAllowed);
                            }
                            catch { }
                        }
                        if (bookingPathReservation.IsReshopChange
                              || bookingPathReservation.NumberOfTravelers > 1
                              || (_memberProfileUtility.IsEnableOmniCartReleaseCandidateOneChanges(request.Application.Id, request.Application.Version.Major) && _memberProfileUtility.IsOmniCartSavedTrip(bookingPathReservation))
                              )
                        {
                            if (bookingPathReservation.IsReshopChange)
                            {
                                response.Reservation.TravelersCSL = new List<MOBCPTraveler>();
                                foreach (var persistTraveler in bookingPathReservation.TravelersCSL)
                                {
                                    response.Reservation.TravelersCSL.Add(persistTraveler.Value);
                                }
                            }

                            List<MOBAddress> address = new List<MOBAddress>();
                            MOBCPPhone mpPhone = new MOBCPPhone();
                            MOBEmail mpEmail = new MOBEmail();
                            var tupleRes = await _traveler.GetProfileOwnerCreditCardList(request.SessionId, address, mpPhone, mpEmail, string.Empty);
                            response.Reservation.CreditCards = tupleRes.savedProfileOwnerCCList;
                            mpEmail = tupleRes.mpEmail;
                            mpPhone = tupleRes.mpPhone;
                            address = tupleRes.creditCardAddresses;
                            response.Reservation.CreditCardsAddress = tupleRes.creditCardAddresses;
                            response.Reservation.ReservationPhone = mpPhone;
                            response.Reservation.ReservationEmail = mpEmail;
                            bookingPathReservation.CreditCards = response.Reservation.CreditCards;
                            bookingPathReservation.CreditCardsAddress = tupleRes.creditCardAddresses;
                            bookingPathReservation.ReservationPhone = mpPhone;
                            bookingPathReservation.ReservationEmail = mpEmail;
                            if (session.IsReshopChange && bookingPathReservation.Reshop != null
                                && bookingPathReservation.CreditCardsAddress.Count > 0)
                            {
                                bookingPathReservation.Reshop.RefundAddress = bookingPathReservation.CreditCardsAddress[0];
                            }
                            bookingPathReservation.GetALLSavedTravelers = request.ReturnAllSavedTravelers;
                            #region Get Multiple Saved Travelers MP Name Miss Match
                            string mpNumbers = string.Empty;
                            foreach (MOBCPTraveler cpTraveler in response.Profiles[0].Travelers)
                            {
                                #region
                                if (cpTraveler.MPNameNotMatchMessage.Trim() != "") // && cpTraveler.MileagePlus != null && !string.IsNullOrEmpty(cpTraveler.MileagePlus.MileagePlusId))
                                {
                                    var airRewardProgramList = (from program in cpTraveler.AirRewardPrograms
                                                                where program.CarrierCode.ToUpper().Trim() == "UA"
                                                                select program).ToList();
                                    if (airRewardProgramList != null && airRewardProgramList.Count > 0)
                                    {
                                        mpNumbers = mpNumbers + "," + airRewardProgramList[0].MemberId;
                                    }
                                }
                                #endregion
                            }
                            if (!string.IsNullOrEmpty(mpNumbers))
                            {
                                List<MOBItem> items = new List<MOBItem>();
                                foreach (MOBItem item in response.Reservation.TCDAdvisoryMessages)
                                {
                                    if (item.Id.ToUpper().Trim() == "SavedTravelerMPNameMismatch".ToUpper().Trim())
                                    {
                                        mpNumbers = mpNumbers.Trim(',');
                                        if (mpNumbers.Split(',').Length > 1)
                                        {
                                            item.CurrentValue = string.Format(item.CurrentValue, "accounts", mpNumbers, "travelers");
                                        }
                                        else
                                        {
                                            item.CurrentValue = string.Format(item.CurrentValue, "account", mpNumbers, "this traveler");
                                        }
                                    }
                                    items.Add(item);
                                }
                                response.Reservation.TCDAdvisoryMessages = items;
                            }
                            bookingPathReservation.TCDAdvisoryMessages = (from item in response.Reservation.TCDAdvisoryMessages
                                                                          where item.SaveToPersist == true
                                                                          select item).ToList();
                            #endregion
                            //Need to save persist as of Populating profile owner as the first auto selected traveler
                            await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName);
                            response.Reservation.TravelersRegistered = false;
                        }
                        else if (bookingPathReservation.NumberOfTravelers == 1 && (!isGetProfileCalledforProfileOWner || bookingPathReservation.ShopReservationInfo2.IsForceSeatMap) && validateBasicInfo(response.Profiles))
                        //else if (bookingPathReservation.NumberOfTravelers == 1 && !isGetProfileCalledforProfileOWner)
                        {
                            if (bookingPathReservation.NumberOfTravelers == 1 && !IsArrangerNavigation(request, bookingPathReservation)
                                && !(_memberProfileUtility.IsEnableOmniCartReleaseCandidateOneChanges(request.Application.Id, request.Application.Version.Major) && _memberProfileUtility.IsOmniCartSavedTrip(bookingPathReservation)))
                            {
                                response.Reservation = await AutoRegisterTravelerWithProfileOwner_CFOP(request);
                                #region Express checkout flow, update setnextviewname and save bundles if needed
                                if (await _shoppingUtility.IsEnabledExpressCheckoutFlow(request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false)
                                    && bookingPathReservation.ShopReservationInfo2.IsExpressCheckoutPath)
                                {
                                    try
                                    {
                                        await UpdatesInExpressCheckoutFlow(bookingPathReservation, request, profilePersist).ConfigureAwait(false);
                                    }
                                    catch (Exception ex) 
                                    {
                                        _logger.LogError("GetProfileCSL_CFOP - ExpressCheckout Exception {error} and SessionId {sessionId}", ex.Message, response.SessionId);
                                    }
                                }
                                #endregion
                            }

                        }

                        if (bookingPathReservation.IsReshopChange)
                        {
                            response.Reservation.ReshopTrips = bookingPathReservation.ReshopTrips;
                            response.Reservation.Reshop = bookingPathReservation.Reshop;
                            response.Reservation.IsReshopChange = true;
                        }

                        //**// A common place to get all the saved reservation data at persist. 
                        response.Reservation = await _shoppingUtility.GetReservationFromPersist(response.Reservation, request.SessionId).ConfigureAwait(false);
                        
                        bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                        if (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange)
                        && bookingPathReservation.ShopReservationInfo2.TravelerTypes != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes.Count > 0 && bookingPathReservation.IsSignedInWithMP)
                        {
                            if (_shoppingUtility.EnableYADesc() && bookingPathReservation.ShopReservationInfo2.IsYATravel)
                            {
                                response.Reservation.ShopReservationInfo2.SelectTravelersHeaderText = "Select 1 Young adult (18-23)";
                            }
                            else
                            {
                                response.Reservation.ShopReservationInfo2.SelectTravelersHeaderText = "Select" + _memberProfileUtility.TravelersHeaderText(bookingPathReservation).TrimEnd(',');
                            }
                        }

                        //update selected special needs to reservation object from bookingpathreservation object
                        if (response.Reservation.TravelersCSL != null && response.Reservation.TravelersCSL.Any() &&
                            bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null &&
                            bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL != null &&
                            bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count > 0)
                        {
                            response.Reservation.TravelersCSL[0].SelectedSpecialNeeds = bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL[0].SelectedSpecialNeeds;
                            response.Reservation.TravelersCSL[0].SelectedSpecialNeedMessages = bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL[0].SelectedSpecialNeedMessages;
                        }

                        if (response.Reservation.IsCubaTravel)
                        {
                            _memberProfileUtility.ValidateTravelersCSLForCubaReason(response);
                        }
                        response.Reservation.GetALLSavedTravelers = request.ReturnAllSavedTravelers;
                        //**//
                        #endregion

                        response.Reservation.IsSignedInWithMP = true;
                        response.Reservation.SessionId = request.SessionId;
                        response.Reservation.SearchType = bookingPathReservation.SearchType;

                        if (!session.IsReshopChange)
                        {
                            response.Reservation.IsBookingCommonFOPEnabled = bookingPathReservation.IsBookingCommonFOPEnabled;
                        }
                        else
                        {
                            response.Reservation.IsReshopCommonFOPEnabled = bookingPathReservation.IsReshopCommonFOPEnabled;
                        }
                        shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName, new List<string> { request.SessionId, shoppingCart.ObjectName }).ConfigureAwait(false);
                        if (_configuration.GetValue<bool>("SavedETCToggle"))
                        {
                            if (shoppingCart == null)
                            {
                                shoppingCart = new MOBShoppingCart();
                            }
                            shoppingCart.ProfileTravelerCertificates = await GetProfileCertificates(request.MileagePlusNumber, request.Application, request.SessionId, request.TransactionId, request.DeviceId);
                        }
                        bool isTC = _travelerUtility.IncludeTravelCredit(request.Application.Id, request.Application.Version.Major);
                        //if (isTC)
                        //{
                        //    UtilityNew.UpdateGrandTotalWithProductsSum(response.Reservation.Prices, shoppingCart);
                        //}
                        shoppingCart = await _shoppingUtility.ReservationToShoppingCart_DataMigration(response.Reservation, shoppingCart, request, session);

                        shoppingCart.Flow = request.Flow;

                        if (session.CatalogItems != null && session.CatalogItems.Count > 0 && _shoppingUtility.IsEnablePartnerProvision(session?.CatalogItems, request.Flow, request.Application.Id, request.Application.Version.Major) && !string.IsNullOrEmpty(request.MileagePlusNumber))
                        {
                            bool enableProvisionForFamilyandFriends = await _featureSettings.GetFeatureSettingValue("EnablePartnerProvision_FamilyNFriends").ConfigureAwait(false);
                            bool isPartnerProvisionEnabled = true;
                            if (enableProvisionForFamilyandFriends)
                            {
                                var _cslReq = new
                                {
                                    ChannelName = _configuration.GetValue<string>("Shopping - ChannelType"),
                                    FailureRedirectUrl = "",
                                    PartnerRequestIdentifier = "",
                                    SuccessRedirectUrl = "",
                                    MPNumber = request.MileagePlusNumber
                                };
                                string path = "/IsPartnerProvisionEnabled";
                                var jsonResponse = await _provisionService.CSL_PartnerProvisionCall(session.Token, path, JsonConvert.SerializeObject(_cslReq));
                                isPartnerProvisionEnabled = !string.IsNullOrEmpty(jsonResponse) ? JsonConvert.DeserializeObject<bool>(jsonResponse) : false;
                            }

                            if (isPartnerProvisionEnabled)
                            {
                                shoppingCart.PartnerProvisionDetails = new PartnerProvisionDetails();
                                shoppingCart.PartnerProvisionDetails.IsEnableProvisionLogin = true;
                                shoppingCart.PartnerProvisionDetails.ProvisionLoginMessage = _configuration.GetValue<string>("ProvisionLoginMessage");
                            }
                        }

                        response.ShoppingCart = shoppingCart;
                        var travelcredit = shoppingCart?.FormofPaymentDetails?.TravelCreditDetails?.TravelCredits;
                        bool isDefault = false;
                        if (!!_configuration.GetValue<bool>("DisableEligibleFormOfPaymentsInGetProfileBugFix") && _omniCart.IsOmniCartSavedTripAndNavigateToFinalRTI(response.Reservation, response.ShoppingCart))
                        {
                            response.EligibleFormofPayments = new List<Model.Common.Payment.FormofPaymentOption>();
                            await _formsOfPayment.GetEligibleFormofPayments(request, session, response.ShoppingCart, request.CartId, request.Flow, isDefault, response.Reservation);
                        }
                        if (!_configuration.GetValue<bool>("DisableFireAndForgetTravelCreditCallInGetProfile") && response?.Reservation?.ShopReservationInfo2?.IsOmniCartSavedTripFlow == false)
                        {
                            await FireForgetForTCAndTBFOP(request, shoppingCart, response.Reservation, session, shoppingCart.IsCorporateBusinessNamePersonalized);
                        }
                        else
                        {
                            if(await _shoppingUtility.IsLoadTCOrTB(response.ShoppingCart).ConfigureAwait(false))
                            {
                                if (isTC && request.Flow == FlowType.BOOKING.ToString() && !session.IsAward && (_memberProfileUtility.IsEnableFeature("EnableU4BCorporateBookingFFC", request.Application.Id, request.Application.Version.Major) ? true : !session.IsCorporateBooking))
                                {
                                    var preLoadTravelCredits = new PreLoadTravelCredits(_logger, _configuration, _sessionHelperService, _travelerUtility, _paymentService, _fFCShoppingcs, _featureSettings);
                                    await preLoadTravelCredits.PreLoadTravelCredit(session.SessionId, shoppingCart, request, true, null, shoppingCart.IsCorporateBusinessNamePersonalized).ConfigureAwait(false);
                                }

                                if (_travelerUtility.IncludeTravelBankFOP(request.Application.Id, request.Application.Version.Major) && shoppingCart?.FormofPaymentDetails?.TravelBankDetails == null)
                                {
                                    var travelBank = new Common.Helper.Traveler.TravelBank(_configuration, _sessionHelperService, _travelerUtility, _fFCShoppingcs, _shoppingUtility, _cachingService, _dpService, _pKDispenserService, _headers);
                                    shoppingCart.FormofPaymentDetails.TravelBankDetails = await travelBank.PopulateTravelBankData(session, response.Reservation, request);
                                }
                            }                         
                        }

                        profilePersist.Response = ObjectToObjectCasting<MOBCustomerProfileResponse, MOBCPProfileResponse>(response);
                        await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, profilePersist.SessionId, new List<string> { profilePersist.SessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName);
                        await _sessionHelperService.SaveSession<ProfileResponse>(profilePersist, profilePersist.SessionId, new List<string> { profilePersist.SessionId, profilePersist.ObjectName }, profilePersist.ObjectName);
                        _travelerUtility.ExtraSeatHandlingForProfile(request, response, session);
                    }
                    else
                    {
                        throw new MOBUnitedException(_configuration.GetValue<string>("bugBountySessionExpiredMsg"));
                    }
                    #endregion
                }
                else
                {
                    response.Exception = new MOBException();
                    response.Exception.Message = _configuration.GetValue<string>("GenericExceptionMessage").ToString();

                    _logger.LogError("GetProfile_CFOP Validate GetProfileCSL Action Request Failed (if web config entry ValidateGetProfileCSLRequest = true) does not Match check the request and EXEC uasp_Validate_MPWithAppIDDeviceID or uasp_Validate_MPWithAppIDDeviceIDCustID (if web config entry ValidateGetProfileCSLRequest = true) ");
                }


            }
            catch (MOBUnitedException uaex)
            {
                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);
                _logger.LogWarning("GetProfileCSL_CFOP Error {@UnitedException}", JsonConvert.SerializeObject(uaexWrapper));

                #region Inhibit Booking
                if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                {
                    if (uaex.InnerException != null && !string.IsNullOrEmpty(uaex.InnerException.Message) && uaex.InnerException.Message.Trim().Equals("10050"))
                    {
                        response.Exception = new MOBException("10050", uaex.Message);
                    }
                }
                #endregion

                //if (uaex != null && !string.IsNullOrEmpty(uaex.Message.Trim()))
                //{
                //    response.Exception = new MOBException();
                //    response.Exception.Message = uaex.Message;
                //}
                //else
                //{
                if (uaex.Message.Trim() == _configuration.GetValue<string>("bugBountySessionExpiredMsg").ToString().Trim())
                {
                    response.Exception = new MOBException();
                    response.Exception.Message = uaex.Message;
                }
                else if (uaex.Message.Trim() == _configuration.GetValue<string>("BookingSessionExpiryMessage").ToString().Trim())
                {
                    response.Exception = new MOBException();
                    response.Exception.Message = uaex.Message;
                    if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                    {
                        response.Exception.Code = uaex.Code;
                    }
                }
                else
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                //}

            }
            catch (System.Exception ex)
            {
                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);

                _logger.LogError("GetProfileCSL_CFOP Error {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));

                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            if (request.CustomerId == 0)
            {
                _logger.LogError("GetProfile_CFOP - CustomerID = 0 for {@MileagePlusNumber}", request.MileagePlusNumber);
            }

            if (_configuration.GetValue<string>("Log_CSL_Call_Statistics") != null && _configuration.GetValue<string>("Log_CSL_Call_Statistics").ToString().ToUpper().Trim() == "TRUE")
            {
                string callDurations = response.CallDuration.ToString();//TODO _shoppingUtility.GetCSLCallDuration();
                var cslStatics = (new CSLStatistics(_logger, _configuration, _dynamoDBService, _cSLStatisticsService));
                try
                {
                    await cslStatics.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", response.CartId, callDurations, "Profile/GetProfileCSL_CFOP", request.SessionId);
                }
                catch { }
            }
            return response;
        }

        public async Task<MOBPartnerSSOTokenResponse> GetPartnerSSOToken(MOBPartnerSSOTokenRequest request)
        {
            MOBPartnerSSOTokenResponse response = new MOBPartnerSSOTokenResponse();
            response.MileagePlusNumber = request.MileagePlusNumber;

            response.TransactionId = request.TransactionId;
            string authToken = string.Empty;
            request.HashPinCode = request.HashPinCode ?? string.Empty;
            bool validateMPWithHash = false;
            var tupleResponse = await _memberProfileUtility.ValidateHashPinAndGetAuthToken(request.MileagePlusNumber, request.HashPinCode, request.Application.Id, request.DeviceId, request.Application.Version.Major, authToken, request.TransactionId, request.TransactionId);
            validateMPWithHash = tupleResponse.Item1;

            if (validateMPWithHash)
            {
                if (String.IsNullOrEmpty(authToken))
                    authToken = await _dpService.GetAnonymousToken(request.Application.Id, request.DeviceId, _configuration).ConfigureAwait(false);

                response.Token = await GetPartnerSSOToken(request, authToken);
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("bugBountySessionExpiredMsg"));
            }
            return response;
        }

        private async Task<string> GetPartnerSSOToken(MOBPartnerSSOTokenRequest request, String authToken)
        {
            string token = null;
            try
            {
                var tokenResponse = await _sSOTokenKeyService.GetPartnerSSOToken(authToken, request.TransactionId, request.MileagePlusNumber);
                if (!string.IsNullOrEmpty(tokenResponse))
                {
                    token = JsonConvert.DeserializeObject<PartnerSSOTokenDecode>(tokenResponse)?.EncryptedToken?.SSOToken;
                }
            }
            catch (System.Net.WebException webException)
            {
                _logger.LogError("GetPartnerSSOToken - {webException},{mpNumber} and {transactionId}", JsonConvert.SerializeObject(webException), request.MileagePlusNumber, request.TransactionId);

            }
            catch (Exception ex)
            {
                _logger.LogError("GetPartnerSSOToken - {exception},{mpNumber} and {transactionId}", JsonConvert.SerializeObject(ex), request.MileagePlusNumber, request.TransactionId);

            }
            return token;
        }

        public async Task<MOBCPProfileResponse> GetEmpProfileCSL_CFOP(MOBCPProfileRequest request)
        {
            string logAction = "";
            MOBCPProfileResponse response = new MOBCPProfileResponse();
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            response.SessionId = request.SessionId;
            request.ReturnAllSavedTravelers = true;
            request.MileagePlusNumber = request.MileagePlusNumber.ToUpper();
            try
            {
                Session session = await _shoppingSessionHelper.GetShoppingSession(request.SessionId);
                logAction = session.IsReshopChange ? "ReShop-" : "";
                request.Token = session.Token;
                request.CartId = session.CartId;

                response.TransactionId = request.TransactionId;
                string authToken = string.Empty;
                request.HashPinCode = (request.HashPinCode == null ? string.Empty : request.HashPinCode);
                bool validateMPWithHash = false;
                var tupleResponse = await _memberProfileUtility.ValidateHashPinAndGetAuthToken(request.MileagePlusNumber, request.HashPinCode, request.Application.Id, request.DeviceId, request.Application.Version.Major, authToken, request.TransactionId, request.SessionId);
                validateMPWithHash = tupleResponse.Item1;
                authToken = tupleResponse.validAuthToken;
                if (!validateMPWithHash) // This is to handle the MP Sign In at home using OLD ValidateMP() REST Web API Service method()
                {
                    validateMPWithHash = await _memberProfileUtility.ValidateAccountFromCache(request.MileagePlusNumber, request.HashPinCode).ConfigureAwait(false);
                }
                if (validateMPWithHash)
                {
                    bool getEmployeeIdFromCSLCustomerData = !string.IsNullOrEmpty(_configuration.GetValue<string>("GetEmployeeIDFromGetProfileCustomerData")) && Convert.ToBoolean(_configuration.GetValue<bool>("GetEmployeeIDFromGetProfileCustomerData"));
                    response.Profiles = await _empProfile.GetEmpProfile(request, getEmployeeIdFromCSLCustomerData);
                    ProfileResponse profilePersist = new ProfileResponse();
                    profilePersist.SessionId = session.SessionId;
                    profilePersist.CartId = session.CartId;
                    profilePersist.Request = ObjectToObjectCasting<MOBCustomerProfileRequest, MOBCPProfileRequest>(request);
                    profilePersist.Response = ObjectToObjectCasting<MOBCustomerProfileResponse, MOBCPProfileResponse>(response);
                    await _sessionHelperService.SaveSession<ProfileResponse>(profilePersist, profilePersist.SessionId, new List<string> { profilePersist.SessionId, profilePersist.ObjectName }, profilePersist.ObjectName); // Saving to persist here first time for use at AutoRegisterTravelerWithProfileOwner()
                    #region Update Booking Path Persist Reservation IsSignedInWithMP value and Save to session
                    Reservation bookingPathReservation = new Reservation();
                    bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                    bool isGetProfileCalledforProfileOWner = bookingPathReservation.IsGetProfileCalledForProfileOwner;
                    bookingPathReservation.IsSignedInWithMP = true; // why here not at Validate Account because if already signed customer with in the valid sign in session intervel client will not call validate account client will call get profile.
                    bookingPathReservation.GetALLSavedTravelers = request.ReturnAllSavedTravelers;
                    bookingPathReservation.IsGetProfileCalledForProfileOwner = true;
                    //Venakt - Need to Save the isSignedInWithMP value to persist and save it as Auto Register Traveler will save the travelers to persist.
                    #region
                    SelectTrip persistSelectTripObj = await _sessionHelperService.GetSession<SelectTrip>(request.SessionId, new SelectTrip().ObjectName, new List<string> { request.SessionId, new SelectTrip().ObjectName }).ConfigureAwait(false);
                    if (persistSelectTripObj != null && !isGetProfileCalledforProfileOWner)
                    {
                        bookingPathReservation.Prices = persistSelectTripObj.Responses[persistSelectTripObj.LastSelectTripKey].Availability.Reservation.Prices;
                    }
                    #endregion
                    await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName);
                    response.Reservation = new MOBSHOPReservation(_configuration, _cachingService);
                    response.Reservation.IsEmp20 = true;
                    response.Reservation.PointOfSale = bookingPathReservation.PointOfSale;
                    response.Reservation.Trips = bookingPathReservation.Trips;
                    response.Reservation.Prices = bookingPathReservation.Prices;
                    response.Reservation.Taxes = bookingPathReservation.Taxes;
                    response.Reservation.NumberOfTravelers = bookingPathReservation.NumberOfTravelers;
                    response.Reservation.TravelOptions = bookingPathReservation.TravelOptions;
                    response.Reservation.FareRules = bookingPathReservation.FareRules;
                    response.Reservation.LMXFlights = bookingPathReservation.LMXFlights;

                    if (bookingPathReservation.IsReshopChange || bookingPathReservation.NumberOfTravelers > 1)
                    {
                        if (bookingPathReservation.IsReshopChange)
                        {
                            response.Reservation.TravelersCSL = new List<MOBCPTraveler>();
                            foreach (var persistTraveler in bookingPathReservation.TravelersCSL)
                            {
                                response.Reservation.TravelersCSL.Add(persistTraveler.Value);
                            }
                        }
                        else if (bookingPathReservation.ShopReservationInfo2.IsForceSeatMap == false)
                        {
                            #region
                            foreach (MOBCPTraveler cpTraveler in response.Profiles[0].Travelers)
                            {
                                response.Reservation.TravelersCSL = new List<MOBCPTraveler>();
                                bookingPathReservation.TravelersCSL = new SerializableDictionary<string, MOBCPTraveler>();
                                bookingPathReservation.TravelerKeys = new List<string>();
                                if (cpTraveler.IsProfileOwner)
                                {
                                    response.Reservation.TravelersCSL.Add(cpTraveler);
                                    bookingPathReservation.TravelersCSL.Add(cpTraveler.PaxIndex.ToString(), cpTraveler);
                                    bookingPathReservation.TravelerKeys.Add(cpTraveler.PaxIndex.ToString());
                                    break;
                                }
                            }
                        }
                        List<MOBAddress> address = new List<MOBAddress>();
                        MOBCPPhone mpPhone = new MOBCPPhone();
                        MOBEmail mpEmail = new MOBEmail();
                        var tupleRes = await _traveler.GetProfileOwnerCreditCardList(request.SessionId, address, mpPhone, mpEmail, string.Empty);
                        response.Reservation.CreditCards = tupleRes.savedProfileOwnerCCList;
                        mpEmail = tupleRes.mpEmail;
                        mpPhone = tupleRes.mpPhone;
                        if (!_configuration.GetValue<bool>("DisableBillingAddressFixForFFC"))
                        {
                            address = tupleRes.creditCardAddresses;
                        }
                        response.Reservation.CreditCardsAddress = address;
                        response.Reservation.ReservationPhone = mpPhone;
                        response.Reservation.ReservationEmail = mpEmail;
                        bookingPathReservation.CreditCards = response.Reservation.CreditCards;
                        bookingPathReservation.CreditCardsAddress = address;
                        bookingPathReservation.ReservationPhone = mpPhone;
                        bookingPathReservation.ReservationEmail = mpEmail;
                        bookingPathReservation.GetALLSavedTravelers = request.ReturnAllSavedTravelers;
                        #region Get Multiple Saved Travelers MP Name Miss Match
                        string mpNumbers = string.Empty;
                        foreach (MOBCPTraveler cpTraveler in response.Profiles[0].Travelers)
                        {
                            #region
                            if (cpTraveler.MPNameNotMatchMessage.Trim() != "") // && cpTraveler.MileagePlus != null && !string.IsNullOrEmpty(cpTraveler.MileagePlus.MileagePlusId))
                            {
                                var airRewardProgramList = (from program in cpTraveler.AirRewardPrograms
                                                            where program.CarrierCode.ToUpper().Trim() == "UA"
                                                            select program).ToList();
                                if (airRewardProgramList != null && airRewardProgramList.Count > 0)
                                {
                                    mpNumbers = mpNumbers + "," + airRewardProgramList[0].MemberId;
                                }
                            }
                            #endregion
                        }
                        if (!string.IsNullOrEmpty(mpNumbers))
                        {
                            List<MOBItem> items = new List<MOBItem>();
                            foreach (MOBItem item in response.Reservation.TCDAdvisoryMessages)
                            {
                                if (item.Id.ToUpper().Trim() == "SavedTravelerMPNameMismatch".ToUpper().Trim())
                                {
                                    mpNumbers = mpNumbers.Trim(',');
                                    if (mpNumbers.Split(',').Length > 1)
                                    {
                                        item.CurrentValue = string.Format(item.CurrentValue, "accounts", mpNumbers, "travelers");
                                    }
                                    else
                                    {
                                        item.CurrentValue = string.Format(item.CurrentValue, "account", mpNumbers, "this traveler");
                                    }
                                }
                                items.Add(item);
                            }
                            response.Reservation.TCDAdvisoryMessages = items;
                        }
                        bookingPathReservation.TCDAdvisoryMessages = (from item in response.Reservation.TCDAdvisoryMessages
                                                                      where item.SaveToPersist == true
                                                                      select item).ToList();
                        #endregion
                        //Need to save persist as of Populating profile owner as the first auto selected traveler
                        await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName);
                        response.Reservation.TravelersRegistered = false;
                        #endregion
                    }
                    //  else if (bookingPathReservation.NumberOfTravelers == 1 && !isGetProfileCalledforProfileOWner)
                    //changes done by prasad for new flow
                    else if (bookingPathReservation.NumberOfTravelers == 1)
                    {
                        response.Reservation = await AutoRegisterTravelerWithProfileOwner_CFOP(request);
                        #region Express checkout flow, update setnextviewname and save bundles if needed
                        if (await _shoppingUtility.IsEnabledExpressCheckoutFlow(request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false)
                            && bookingPathReservation.ShopReservationInfo2.IsExpressCheckoutPath)
                        {
                            try 
                            { 
                                await UpdatesInExpressCheckoutFlow(bookingPathReservation, request, profilePersist).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError("GetEmpProfileCSL_CFOP - ExpressCheckout Exception {error} and SessionId {sessionId}", ex.Message, response.SessionId);
                            }
                        }
                        #endregion
                    }

                    bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

                    #region mapping preferences
                    foreach (var traveler in response.Profiles[0].Travelers.Where(t => t.AirPreferences != null && t.AirPreferences.Any()))
                    {
                        var savedSpecialNeeds = GetSpecialNeedsFromTravelerAirPrefernces(traveler.AirPreferences[0]);
                        if (savedSpecialNeeds == null || !savedSpecialNeeds.Any())
                            continue;

                        if (savedSpecialNeeds != null && savedSpecialNeeds.Any())
                        {
                            traveler.SelectedSpecialNeedMessages = new List<MOBItem>();
                            foreach (var x in savedSpecialNeeds)
                            {
                                if ((!string.IsNullOrWhiteSpace(x.Type) && x.Type.Equals(TravelSpecialNeedType.ServiceAnimalType.ToString())) &&
                                   (bookingPathReservation.ShopReservationInfo2.SpecialNeeds.ServiceAnimals == null || !bookingPathReservation.ShopReservationInfo2.SpecialNeeds.ServiceAnimals.Any()))
                                {
                                    traveler.SelectedSpecialNeedMessages.Add(new MOBItem { CurrentValue = _configuration.GetValue<string>("SSRItineraryServiceAnimalNotAvailableMsg") });
                                }
                                else if (!string.IsNullOrWhiteSpace(x.Type) && x.Type.Equals(TravelSpecialNeedType.SpecialMeal.ToString()) && (bookingPathReservation.ShopReservationInfo2.SpecialNeeds.SpecialMeals != null && bookingPathReservation.ShopReservationInfo2.SpecialNeeds.SpecialMeals.Any() && !bookingPathReservation.ShopReservationInfo2.SpecialNeeds.SpecialMeals.Any(_ => _.Code == x.Code)))
                                {
                                    traveler.SelectedSpecialNeedMessages.Add(new MOBItem { CurrentValue = string.Format(_configuration.GetValue<string>("SSRItinerarySpecialMealsNotAvailableMsg"), x.DisplayDescription) });
                                }
                            }
                        }

                        traveler.SelectedSpecialNeeds =  await _travelerUtility.ValidateSpecialNeedsAgaintsMasterList(savedSpecialNeeds, bookingPathReservation.ShopReservationInfo2.SpecialNeeds, request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false);
                        if (traveler.SelectedSpecialNeeds == null || !traveler.SelectedSpecialNeeds.Any())
                            continue;
                    }
                    #endregion

                    bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL = new List<MOBCPTraveler>();
                    foreach (MOBCPTraveler cpTraveler in response.Profiles[0].Travelers)
                    {
                        cpTraveler.PaxID = cpTraveler.PaxIndex + 1;
                        cpTraveler.IsPaxSelected = false;
                        bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Add(cpTraveler);
                    }

                    if (bookingPathReservation.NumberOfTravelers == 1)
                    {
                        bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL[0].IsPaxSelected = true;
                        if (await _featureToggles.IsEnableWheelchairFilterOnFSR(request.Application.Id, request.Application.Version.Major, session.CatalogItems).ConfigureAwait(false)
                            && _shoppingUtility.isWheelChairFilterAppliedAtFSR(bookingPathReservation?.ShopReservationInfo2))
                        {
                            if (response.Reservation.TravelersCSL != null && response.Reservation.TravelersCSL.Any())
                            _shoppingUtility.UpdateAllEligibleTravelersCslWithWheelChairSpecialNeed(bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL, response.Reservation.TravelersCSL[0]);
                        }
                    }
                    _travelerUtility.ValidateTravelersForCubaReason(bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL, bookingPathReservation.IsCubaTravel);

                    await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName);
                    response.Reservation.IsSignedInWithMP = true;
                    response.Reservation.IsEmp20 = true;
                    response.Reservation.SessionId = request.SessionId;
                    response.Reservation.SearchType = bookingPathReservation.SearchType;
                    //**// A common place to get all the saved reservation data at persist. 
                    response.Reservation = await _shoppingUtility.GetReservationFromPersist(response.Reservation, request.SessionId).ConfigureAwait(false);
                    response.Reservation.GetALLSavedTravelers = request.ReturnAllSavedTravelers;
                    //**//
                    #endregion

                    profilePersist.Response = ObjectToObjectCasting<MOBCustomerProfileResponse, MOBCPProfileResponse>(response);
                    await _sessionHelperService.SaveSession<ProfileResponse>(profilePersist, profilePersist.SessionId, new List<String> { profilePersist.SessionId, profilePersist.ObjectName }, profilePersist.ObjectName);



                    if (response.Reservation.TravelersCSL != null && response.Reservation.TravelersCSL.Any() &&
                        bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null &&
                        bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL != null &&
                        bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count > 0)
                    {
                        response.Reservation.TravelersCSL[0].SelectedSpecialNeeds = bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL[0].SelectedSpecialNeeds;
                        response.Reservation.TravelersCSL[0].SelectedSpecialNeedMessages = bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL[0].SelectedSpecialNeedMessages;
                    }

                    response.Reservation.IsBookingCommonFOPEnabled = bookingPathReservation.IsBookingCommonFOPEnabled;
                    response.Reservation.IsReshopCommonFOPEnabled = bookingPathReservation.IsReshopCommonFOPEnabled;
                    shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName, new List<string> { request.SessionId, shoppingCart.ObjectName }).ConfigureAwait(false);
                    if (_configuration.GetValue<bool>("EnableETCEmp20"))
                    {
                        if (shoppingCart == null)
                        {
                            shoppingCart = new MOBShoppingCart();
                        }
                        shoppingCart.ProfileTravelerCertificates = await GetProfileCertificates(request.MileagePlusNumber, request.Application, request.SessionId, request.SessionId, request.DeviceId);
                    }
                    if (!_configuration.GetValue<bool>("DisableFireAndForgetTravelCreditCallInGetProfile"))
                    {
                        await FireForgetForTCAndTBFOP(request, shoppingCart, response.Reservation, session);
                    }
                    else
                    {
                        if(await _shoppingUtility.IsLoadTCOrTB(shoppingCart).ConfigureAwait(false))
                        {
                            if (_travelerUtility.IncludeTravelBankFOP(request.Application.Id, request.Application.Version.Major) && shoppingCart?.FormofPaymentDetails?.TravelBankDetails == null)
                            {
                                if (shoppingCart.FormofPaymentDetails == null)
                                    shoppingCart.FormofPaymentDetails = new MOBFormofPaymentDetails();
                                var travelBank = new Common.Helper.Traveler.TravelBank(_configuration, _sessionHelperService, _travelerUtility, _fFCShoppingcs, _shoppingUtility, _cachingService, _dpService, _pKDispenserService, _headers);
                                shoppingCart.FormofPaymentDetails.TravelBankDetails = await travelBank.PopulateTravelBankData(session, response.Reservation, request);
                            }
                            if (!_configuration.GetValue<bool>("DisableEMP20PreloadTC_MOBILE17801") && _travelerUtility.IncludeTravelCredit(request.Application.Id, request.Application.Version.Major)
                                && request.Flow == FlowType.BOOKING.ToString() && !session.IsAward && (_memberProfileUtility.IsEnableFeature("EnableU4BCorporateBookingFFC", request.Application.Id, request.Application.Version.Major) ? true : !session.IsCorporateBooking))
                            {
                                var preLoadTravelCredits = new PreLoadTravelCredits(_logger, _configuration, _sessionHelperService, _travelerUtility, _paymentService, _fFCShoppingcs, _featureSettings);
                                await preLoadTravelCredits.PreLoadTravelCredit(session.SessionId, shoppingCart, request);
                            }
                        }
                    }
                    shoppingCart = await _shoppingUtility.ReservationToShoppingCart_DataMigration(response.Reservation, shoppingCart, request, session);

                    shoppingCart.Flow = request.Flow;
                    if (!_configuration.GetValue<bool>("DisableEMP20DefaultCCFix_MOBILE17279")
                        && (shoppingCart.FormofPaymentDetails.FormOfPaymentType == null || shoppingCart.FormofPaymentDetails.FormOfPaymentType == string.Empty))
                    {
                        shoppingCart.FormofPaymentDetails.FormOfPaymentType = response.Reservation.FormOfPaymentType.ToString();
                    }
                    await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, request.SessionId, new List<string> { request.SessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName);
                    response.ShoppingCart = shoppingCart;

                }
                else
                {
                    throw new MOBUnitedException(string.Format("Invalid Account Number or Pin."));
                }
            }
            catch (MOBUnitedException uaex)
            {

                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);
                _logger.LogWarning("GetEmpProfileCSL_CFOP {@UnitedException}", JsonConvert.SerializeObject(uaexWrapper));

                #region Inhibit Booking
                if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                {
                    if (uaex.InnerException != null && !string.IsNullOrEmpty(uaex.InnerException.Message) && uaex.InnerException.Message.Trim().Equals("10050"))
                    {
                        response.Exception = new MOBException("10050", uaex.Message);
                    }
                }
                #endregion
                if (uaex.Message.Trim() == _configuration.GetValue<string>("BookingSessionExpiryMessage").Trim())
                {
                    response.Exception = new MOBException();
                    response.Exception.Message = uaex.Message;
                }
                else
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

            }
            catch (System.Exception ex)
            {
                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);

                _logger.LogError("GetEmpProfileCSL_CFOP {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));

                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            if (request.CustomerId == 0)
            {
                _logger.LogError("GetProfileOwner - CustomerID = 0 Exception {@MPNumber}", request.MileagePlusNumber);
            }

            if (_configuration.GetValue<string>("Log_CSL_Call_Statistics") != null && _configuration.GetValue<string>("Log_CSL_Call_Statistics").ToString().ToUpper().Trim() == "TRUE")
            {
                string callDurations = response.CallDuration.ToString();//TO-DO _shoppingUtility.GetCSLCallDuration();
                var cslStatics = (new CSLStatistics(_logger, _configuration, _dynamoDBService, _cSLStatisticsService));
                try
                {
                    await cslStatics.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", response.CartId, callDurations, "Profile/GetEmpProfileCSL_CFOP", request.SessionId);
                }
                catch { }
            }
            return response;
        }

        public async Task<MOBCPProfileResponse> MPSignedInInsertUpdateTraveler_CFOP(MOBUpdateTravelerRequest request)
        {
            // this is the combination of insert and update Travelerlogic
            MOBCPProfileResponse response = new MOBCPProfileResponse();
            #region  
            response.SessionId = request.SessionId;
            bool insertTraveler = false;
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            var bookingPathReservation = new Reservation();

            try
            {
                if (request.Traveler.PaxID == 0 || await IsTravelerNotExistInProfileResponse(request)) // MAKE SURE request.Traveler.PaxID IS SENT AS 0 ( OR ITS DEFAULT 0 WHEN CLIENT NOT SENDING ANY PAX ID VALUES)
                {
                    insertTraveler = true;
                }
                bool isActionCalledForAddNewCCAddNewAddress = false;
                if (request.Application.Id == 2 && request.UpdateAddressInfoAssociatedWithCC == true && request.UpdateCreditCardInfo == true)// To handle the Andriod Client calling same Action for ADD New CC and Add New Address
                {
                    insertTraveler = false;
                    request.IsTravelSavedToProfile = true;
                    isActionCalledForAddNewCCAddNewAddress = true;

                }
                if (_shoppingUtility.IsCanadaTravelNumberEnabled(request.Application.Id, request.Application.Version.Major) && string.IsNullOrEmpty(request.Traveler.CanadianTravelerNumber) == false)
                {
                    request.Traveler.CanadianTravelerNumber = request.Traveler.CanadianTravelerNumber.ToUpper();
                    if (request.Traveler.CanadianTravelerNumber?.StartsWith("CAN") == false)
                        throw new MOBUnitedException(_configuration.GetValue<string>("CanadaTravelNumberFormatErrorMessage"));
                    else if (request.Traveler.CanadianTravelerNumber?.Length != _configuration.GetValue<int>("CanadaTravelNumberLength"))
                        throw new MOBUnitedException(_configuration.GetValue<string>("CanadaTravelLengthMessage"));
                }
                Session session = await _shoppingSessionHelper.GetShoppingSession(request.SessionId);

                bool isExtraSeatFeatureEnabled = _travelerUtility.IsExtraSeatFeatureEnabled(request.Application.Id, request.Application.Version.Major, session.CatalogItems);

                if (await _featureSettings.GetFeatureSettingValue("EnableFixForDuplicateTravelerMP_MOBILE35236").ConfigureAwait(false))
                {
                    await _travelerUtility.DuplicateTravelerCheck(request.SessionId, request.Traveler, isExtraSeatFeatureEnabled);
                }

                if (insertTraveler)
                {
                    await  _travelerUtility.ExtraSeatHandling(request.Traveler, request, bookingPathReservation, session, request.MileagePlusNumber);
                    #region  Insert Traveler Implementation
                    #region ALM 28440 Bug Bounty - Information Disclosure XSS2 #82 - Ravitheja - May 26,2016

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

                    if (!string.IsNullOrEmpty(request.Traveler.CountryOfResidence))
                        request.Traveler.CountryOfResidence = Sanitizer.GetSafeHtmlFragment(request.Traveler.CountryOfResidence);

                    if (!string.IsNullOrEmpty(request.Traveler.Nationality))
                        request.Traveler.Nationality = Sanitizer.GetSafeHtmlFragment(request.Traveler.Nationality);

                    #endregion

                    #region DeviceId, MP# APPID check

                    bool validRequest = await _memberProfileUtility.IsValidDeviceRequest(request.TransactionId, request.DeviceId, request.Application.Id, request.MileagePlusNumber, request.SessionId);

                    #endregion
                    if (validRequest)
                    {
                        // MB-3116 MP Sign in flow multi PAX PNR with same name
                        if (_configuration.GetValue<bool>("DuplicateTravelerCheck"))
                        {
                            MOBSHOPReservation res = new MOBSHOPReservation(_configuration, _cachingService);
                            res = await GetReservationFromPersist(request.SessionId, string.Empty, null);
                            if (res != null && res.ShopReservationInfo2 != null && res.ShopReservationInfo2.AllEligibleTravelersCSL != null && res.ShopReservationInfo2.AllEligibleTravelersCSL.Exists(tr =>
                                                                                           //!string.IsNullOrEmpty(tr.Key) == request.IsTravelSavedToProfile &&
                                                                                           (_travelerUtility.IsExtraSeatFeatureEnabled(request.Application.Id, request.Application.Version.Major, session.CatalogItems) && request.Traveler.IsExtraSeat == false) &&
                                                                                           request.Traveler.PaxID == 0 &&
                                                                                           tr.FirstName.ToLower() == request.Traveler.FirstName.ToLower() &&
                                                                                           tr.MiddleName.ToLower() == request.Traveler.MiddleName.ToLower() &&
                                                                                           tr.LastName.ToLower() == request.Traveler.LastName.ToLower() &&
                                                                                           tr.Suffix.ToLower() == request.Traveler.Suffix.ToLower()))
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("DuplicateTravelerMessage").ToString());
                            }
                        }
                        bool isCorporateMutipax = session.IsCorporateBooking && await _featureToggles.IsEnableU4BForMultipax(request.Application.Id, request.Application.Version?.Major, session?.CatalogItems) && (bookingPathReservation?.ShopReservationInfo2?.IsMultiPaxAllowed??false);
                        bool isCorpMpValidationServiceFailed = false;
                        List<CMSContentMessage> lstMessages = new List<CMSContentMessage>();
                        if (isCorporateMutipax)
                        {
                            var isCorpMpValidation = await CorpMpValidation(request, session.Token).ConfigureAwait(false);
                             lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("U4BCorporateContentMessageGroupName"), "U4BCorporateContentMessageCache");
                            if (!isCorpMpValidation.Item1)
                            {
                                response.Reservation = new MOBSHOPReservation();
                                response.Reservation.ShopReservationInfo2 = new ReservationInfo2();
                                response.Reservation.ShopReservationInfo2.CorporateUnenrolledTravelerMsg = corporateUnenrolledTravelerMsg(request, lstMessages);
                                response.Reservation.ShopReservationInfo2.CorpMultiPaxInfo = new corpMultiPaxInfo();
                                response.Reservation.ShopReservationInfo2.CorpMultiPaxInfo = bookingPathReservation.ShopReservationInfo2.CorpMultiPaxInfo;
                                return response;
                            }
                            if (isCorpMpValidation.Item2)
                            {
                                isCorpMpValidationServiceFailed = isCorpMpValidation.Item2;
                               
                            }
                        }
                        if (request.IsTravelSavedToProfile)
                        {
                            #region

                            //bool token = Authentication.ValidateAuthenticateCSSToken(request.Application.Id, request.DeviceId,request.Application.Version.Major, request.TransactionId, session, profile.LogEntries, traceSwitch);
                            request.Token = session.Token;

                            response.TransactionId = request.TransactionId;
                            List<MOBItem> insertItemKeys = new List<MOBItem>();
                            #region
                            InsertTravelerRequest insertTravelerRequest = new InsertTravelerRequest();
                            insertTravelerRequest.AccessCode = request.AccessCode;
                            insertTravelerRequest.AlreadySelectedPAXIDs = request.AlreadySelectedPAXIDs;
                            insertTravelerRequest.Application = request.Application;
                            insertTravelerRequest.CartId = request.CartId;
                            insertTravelerRequest.DeviceId = request.DeviceId;
                            insertTravelerRequest.IsTravelSavedToProfile = request.IsTravelSavedToProfile;
                            insertTravelerRequest.LanguageCode = request.LanguageCode;
                            insertTravelerRequest.MileagePlusNumber = request.MileagePlusNumber;
                            insertTravelerRequest.SessionId = request.SessionId;
                            insertTravelerRequest.Token = request.Token;
                            insertTravelerRequest.TransactionId = request.TransactionId;

                            #region Special Needs

                            if (_shoppingUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
                            {
                                //var bookingPathReservation = new Persist.Definition.Shopping.Reservation();
                                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

                                var finalSpecialNeeds =await _travelerUtility.ValidateSpecialNeedsAgaintsMasterList(request.Traveler.SelectedSpecialNeeds, bookingPathReservation.ShopReservationInfo2.SpecialNeeds, request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false);
                                request.Traveler.SelectedSpecialNeeds = finalSpecialNeeds;
                            }

                            #endregion

                            insertTravelerRequest.Traveler = request.Traveler;
                            insertTravelerRequest.SetEmailAsPrimay = request.SetEmailAsPrimay;
                            #endregion
                            var tupleRes = await _mPTraveler.InsertTraveler(insertTravelerRequest, insertItemKeys);
                            insertTraveler = tupleRes.returnValue;
                            insertItemKeys = tupleRes.insertItemKeys;


                            #region
                            request.AccessCode = insertTravelerRequest.AccessCode;
                            request.AlreadySelectedPAXIDs = insertTravelerRequest.AlreadySelectedPAXIDs;
                            request.Application = insertTravelerRequest.Application;
                            request.CartId = insertTravelerRequest.CartId;
                            request.DeviceId = insertTravelerRequest.DeviceId;
                            request.IsTravelSavedToProfile = insertTravelerRequest.IsTravelSavedToProfile;
                            request.LanguageCode = insertTravelerRequest.LanguageCode;
                            request.MileagePlusNumber = insertTravelerRequest.MileagePlusNumber;
                            request.SessionId = insertTravelerRequest.SessionId;
                            request.Token = insertTravelerRequest.Token;
                            request.TransactionId = insertTravelerRequest.TransactionId;
                            request.Traveler = insertTravelerRequest.Traveler;
                            #endregion
                            ProfileResponse profilePersist = new ProfileResponse();
                            profilePersist = await GetCSLProfileResponseInSession(request.SessionId);
                            #region Build MOBCPProfileRequest for GetProfile.
                            MOBCPProfileRequest profileRequest = new MOBCPProfileRequest();
                            profileRequest.Application = request.Application;
                            profileRequest.SessionId = request.SessionId;
                            profileRequest.AccessCode = request.AccessCode;
                            profileRequest.DeviceId = request.DeviceId;
                            profileRequest.LanguageCode = request.LanguageCode;
                            profileRequest.TransactionId = request.TransactionId;
                            profileRequest.MileagePlusNumber = request.MileagePlusNumber;
                            profileRequest.Token = request.Token;
                            profileRequest.IncludeAllTravelerData = true;
                            profileRequest.Path = "BOOKING";
                            #endregion
                            MOBCPProfileResponse profileResponse = new MOBCPProfileResponse();
                            profileResponse.TransactionId = request.TransactionId;
                            profileResponse.Profiles = await _customerProfile.GetProfile(profileRequest);
                            if (insertItemKeys != null && insertItemKeys.Count > 0)
                                request.Traveler.Key = insertItemKeys[0].CurrentValue;

                            #region Special Needs

                            if (_shoppingUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
                            {
                                UpdateSpecialNeedsInProfile(profileResponse.Profiles, request.Traveler);
                            }

                            #endregion

                            // Need to get the newly inserted travler by search by request.Traveler.Key from the profileResponse.Profiles and assing the travler to request.Traveler.
                            foreach (MOBCPTraveler mobCPtraveler in profileResponse.Profiles[0].Travelers)
                            {
                                if (mobCPtraveler.Key == insertItemKeys[0].CurrentValue)
                                {
                                    request.Traveler = mobCPtraveler;
                                    //255590 - mApp-Booking - added to remove CUBA reason for travel which is asked for 2 times for MP sponsored newly added Guest
                                    //Anku - 20/03/2018
                                    if (_configuration.GetValue<bool>("BugFixToggleFor18C") && insertTravelerRequest.Traveler.CubaTravelReason != null)
                                    {
                                        request.Traveler.CubaTravelReason = insertTravelerRequest.Traveler.CubaTravelReason;
                                    }
                                    break;
                                }
                            }

                            _memberProfileUtility.UpdateTravelerCubaReasonInProfile(profileResponse.Profiles, request.Traveler);
                            response.Profiles = profileResponse.Profiles;
                            profilePersist.SessionId = session.SessionId;
                            profilePersist.CartId = session.CartId;
                            profilePersist.Request = ObjectToObjectCasting<MOBCustomerProfileRequest, MOBCPProfileRequest>(profileRequest);
                            profilePersist.Response = ObjectToObjectCasting<MOBCustomerProfileResponse, MOBCPProfileResponse>(profileResponse);
                            response.InsertUpdateKeys = insertItemKeys;
                            await _sessionHelperService.SaveSession<ProfileResponse>(profilePersist, profilePersist.SessionId, new List<string> { profilePersist.SessionId, profilePersist.ObjectName }, profilePersist.ObjectName);
                            response.Reservation = await GetReservationFromPersist(request.SessionId, string.Empty, null);
                            if(isCorporateMutipax && isCorpMpValidationServiceFailed)
                            {
                                response.Reservation.ShopReservationInfo2.InfoWarningMessages = CorpMultiPaxInfoWarningMessages(lstMessages, response?.Reservation?.ShopReservationInfo2?.InfoWarningMessages);
                            }
                            _memberProfileUtility.UpdateMatchedTravelerCubaReason(request.Traveler, response.Reservation.TravelersCSL);
                            await AssignSelectedTraveletToAllEligibleTravelerCsl(insertTravelerRequest, response, session.SessionId, isExtraSeatFeatureEnabled);

                            #endregion
                        }
                        else if (!request.IsTravelSavedToProfile)
                        {
                            #region // Need to fix populate Profile here
                            MOBMPNameMissMatchResponse response1 = new MOBMPNameMissMatchResponse();
                            response1.MileagePlusNumber = response.MileagePlusNumber;
                            try
                            {
                                request.Token = session.Token;
                                request.CartId = session.CartId;
                                #region
                                InsertTravelerRequest insertTravelerRequest = new InsertTravelerRequest();
                                insertTravelerRequest.AccessCode = request.AccessCode;
                                insertTravelerRequest.AlreadySelectedPAXIDs = request.AlreadySelectedPAXIDs;
                                insertTravelerRequest.Application = request.Application;
                                insertTravelerRequest.CartId = request.CartId;
                                insertTravelerRequest.DeviceId = request.DeviceId;
                                insertTravelerRequest.IsTravelSavedToProfile = request.IsTravelSavedToProfile;
                                insertTravelerRequest.LanguageCode = request.LanguageCode;
                                insertTravelerRequest.MileagePlusNumber = request.MileagePlusNumber;
                                insertTravelerRequest.SessionId = request.SessionId;
                                insertTravelerRequest.Token = request.Token;
                                insertTravelerRequest.TransactionId = request.TransactionId;

                                #region Special Needs


                                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

                                // Validate the selected special needs. Unavailable special needs will be filtered out
                                if (_shoppingUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
                                {
                                    var finalSpecialNeeds = await _travelerUtility.ValidateSpecialNeedsAgaintsMasterList(request.Traveler.SelectedSpecialNeeds, bookingPathReservation.ShopReservationInfo2.SpecialNeeds, request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false);
                                    request.Traveler.SelectedSpecialNeeds = finalSpecialNeeds;
                                }

                                #endregion
                                if (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major))
                                {
                                    if (bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes.Count > 0)
                                    {
                                        if (!string.IsNullOrEmpty(request.Traveler.BirthDate))
                                        {
                                            int age = TopHelper.GetAgeByDOB(request.Traveler.BirthDate, bookingPathReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTime);
                                            request.Traveler.TravelerTypeCode = _travelerUtility.GetTypeCodeByAge(age, true);
                                            request.Traveler.TravelerTypeDescription = _travelerUtility.GetPaxDescriptionByDOB(request.Traveler.BirthDate.ToString(), bookingPathReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTime);
                                            //request.Traveler.TravelerTypeDescription = Mobile.DAL.Utility.GetPaxDescription(request.Traveler.TravelerTypeCode);
                                            if (_shoppingUtility.EnableYADesc() && bookingPathReservation.ShopReservationInfo2.IsYATravel)
                                            {
                                                request.Traveler.PTCDescription = _mPTraveler.GetYAPaxDescByDOB();
                                            }
                                            else
                                            {
                                                request.Traveler.PTCDescription = request.Traveler.TravelerTypeDescription;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (_shoppingUtility.EnableYADesc() && bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.IsYATravel)
                                    {
                                        request.Traveler.PTCDescription = _mPTraveler.GetYAPaxDescByDOB();
                                    }
                                }
                                insertTravelerRequest.Traveler = request.Traveler;
                                insertTravelerRequest.SetEmailAsPrimay = request.SetEmailAsPrimay;
                                #endregion

                                response.CartId = request.CartId;
                                response.SessionId = request.SessionId;
                                response.Token = session.Token;
                                response.MileagePlusNumber = request.MileagePlusNumber;
                                response.TransactionId = request.TransactionId;
                                MOBRequest mobRequest = new MOBRequest();
                                mobRequest.Application = request.Application;
                                mobRequest.AccessCode = request.AccessCode;
                                mobRequest.DeviceId = request.DeviceId;
                                mobRequest.LanguageCode = request.LanguageCode;
                                mobRequest.TransactionId = request.TransactionId;
                                                                
                                response.Traveler = await RegisterNewTravelerValidateMPMisMatch(request.Traveler, mobRequest, request.SessionId, request.CartId, request.Token);
                                response.isMPNameMisMatch = response.Traveler.isMPNameMisMatch;
                                response.Reservation = await GetReservationFromPersist(request.SessionId, string.Empty, null);
                                if (isCorporateMutipax && isCorpMpValidationServiceFailed)
                                {
                                    response.Reservation.ShopReservationInfo2.InfoWarningMessages = CorpMultiPaxInfoWarningMessages(lstMessages, response?.Reservation?.ShopReservationInfo2?.InfoWarningMessages);
                                }
                                await AssignSelectedTraveletToAllEligibleTravelerCsl(insertTravelerRequest, response, session.SessionId, isExtraSeatFeatureEnabled);
                                if (response.isMPNameMisMatch)
                                {
                                    response.Exception = new MOBException("1111", _configuration.GetValue<string>("ValidateMPNameMisMatchErrorMessage"));
                                }
                                #region
                                ProfileResponse profilePersistCopy = new ProfileResponse();
                                profilePersistCopy = await GetCSLProfileResponseInSession(request.SessionId);

                                if (profilePersistCopy != null && profilePersistCopy.Response != null)
                                {
                                    MOBCPProfileResponse profileResponse = profilePersistCopy.Response;

                                    if (isExtraSeatFeatureEnabled && profileResponse != null)
                                    {
                                        var profiles = profileResponse?.Profiles;
                                        foreach (var profile in profiles)
                                        {
                                            foreach (var traveler in profile?.Travelers)
                                            {
                                                traveler.IsEligibleForExtraSeatSelection = _travelerUtility.IsExtraSeatEligibleForInfantOnLapAndInfantOnSeat(traveler?.TravelerTypeCode, traveler.IsExtraSeat);
                                            }
                                        }
                                    }

                                    response.Profiles = profileResponse?.Profiles;
                                    response.Traveler.IsEligibleForExtraSeatSelection = isExtraSeatFeatureEnabled && _travelerUtility.IsExtraSeatEligibleForInfantOnLapAndInfantOnSeat(response.Traveler?.TravelerTypeCode, request.Traveler.IsExtraSeat);
                                }
                                #endregion
                            }
                            catch (WebException webResponse)
                            {
                                if (webResponse.Response != null && webResponse.Response.ContentLength != 0)
                                {
                                    string webExceptionText = ReadResponseStream(webResponse.Response.GetResponseStream());

                                    throw;
                                }
                            }
                            catch (MOBUnitedException uaex)
                            {
                                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);
                                _logger.LogWarning("ValidateMPNameMisMatch_CFOP - MPSignedIn {@UnitedException}", JsonConvert.SerializeObject(uaexWrapper));

                                response.Exception = new MOBException();
                                response.Exception.Message = uaex.Message;
                            }
                            catch (System.Exception ex)
                            {
                                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);

                                _logger.LogError("ValidateMPNameMisMatch_CFOP - MPSignedIn {@Exception}", JsonConvert.SerializeObject(ex));


                                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                                {
                                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                                }
                                else
                                {
                                    response.Exception = new MOBException("9999", ex.Message);
                                }
                            }

                            if (_configuration.GetValue<string>("Log_CSL_Call_Statistics") != null && _configuration.GetValue<string>("Log_CSL_Call_Statistics").ToString().ToUpper().Trim() == "TRUE")
                            {
                                string callDurations = response.CallDuration.ToString();
                                var cslStatics = (new CSLStatistics(_logger, _configuration, _dynamoDBService, _cSLStatisticsService));
                                try
                                {

                                    await cslStatics.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", response.CartId, callDurations, "PRofileCSL/ValidateMPNameMisMatch_CFOP - MPSignedIn", request.SessionId);
                                }
                                catch { }
                            }
                            //return response;

                            #endregion
                        }
                    }
                    else
                    {
                        response.Exception = new MOBException();
                        response.Exception.Message = _configuration.GetValue<string>("GenericExceptionMessage").ToString();
                    }
                    #endregion
                }
                else
                {
                    #region  Update Traveler Implementation
                    await _travelerUtility.ExtraSeatHandling(request.Traveler, request, bookingPathReservation, session, request.MileagePlusNumber, true);
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
                    if (!string.IsNullOrEmpty(request.Traveler.CountryOfResidence))
                        request.Traveler.CountryOfResidence = Sanitizer.GetSafeHtmlFragment(request.Traveler.CountryOfResidence);

                    if (!string.IsNullOrEmpty(request.Traveler.Nationality))
                        request.Traveler.Nationality = Sanitizer.GetSafeHtmlFragment(request.Traveler.Nationality);

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

                    bool validUpdateTravlerRequest = await _memberProfileUtility.isValidDeviceRequest(request.TransactionId, request.DeviceId, request.Application.Id, request.MileagePlusNumber, request.SessionId);

                    #endregion
                    if (validUpdateTravlerRequest)
                    {
                        if (!string.IsNullOrEmpty(request.Traveler.Key) && request.IsTravelSavedToProfile)
                        {
                            #region
                            //bool token = Authentication.ValidateAuthenticateCSSToken(request.Application.Id, request.DeviceId,request.Application.Version.Major, request.TransactionId, session, profile.LogEntries, traceSwitch);                           
                            request.Token = session.Token;
                            response.TransactionId = request.TransactionId;
                            List<MOBItem> insertUpdateItemKeys = new List<MOBItem>();
                            if (request.UpdateCreditCardInfo)// As per requirment from the payment page update only CC do not update/insert phone and e-mail to the profile.
                            {
                                // Added By Ali Bug 104572 - New Phone and email address are not saved in the profile, when email address and phone are edited in Payment screen with save card 
                                if (_configuration.GetValue<string>("UpdateEmailandPhoneInfo") != null ? Convert.ToBoolean(_configuration.GetValue<string>("UpdateEmailandPhoneInfo")) : false)
                                {
                                    request.UpdateEmailInfo = true;
                                    request.UpdatePhoneInfo = true;
                                }
                                else
                                {
                                    //Defect ID : 23598 Booking 2.1 CR - Do not send email or phone number on payment page to profile to be updated
                                    request.UpdateEmailInfo = false;
                                    request.UpdatePhoneInfo = false;

                                }

                            }

                            #region Special Needs

                            if (_shoppingUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
                            {
                                //var bookingPathReservation = new Persist.Definition.Shopping.Reservation();
                                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

                                var finalSpecialNeeds = await _travelerUtility.ValidateSpecialNeedsAgaintsMasterList(request.Traveler.SelectedSpecialNeeds, bookingPathReservation.ShopReservationInfo2.SpecialNeeds, request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false);
                                request.Traveler.SelectedSpecialNeeds = finalSpecialNeeds;
                            }

                            #endregion

                            //Do not update traveler if this is for employee booking.
                            if (string.IsNullOrEmpty(session.EmployeeId) || (_configuration.GetValue<bool>("GetEmp20PassRidersFromEResService") && _configuration.GetValue<bool>("EnableEmp20PassRidersUpdate") && request.Traveler.IsProfileOwner))
                            {
                                var UpdateTravelerResponse = await _mPTraveler.UpdateTraveler(request, insertUpdateItemKeys);
                                insertUpdateItemKeys = UpdateTravelerResponse.insertUpdateItemKeys;
                            }
                            else
                            {
                                if (_configuration.GetValue<bool>("EnableEmp20PassRidersUpdate") && _configuration.GetValue<bool>("GetEmp20PassRidersFromEResService"))
                                {
                                    if (request.UpdateTravelerBasiInfo || request.UpdateEmailInfo || request.UpdatePhoneInfo)
                                        await _mPTraveler.UpdatePassRider(request, session.EmployeeId);
                                }
                            }

                            ProfileResponse profilePersist = new ProfileResponse();
                            profilePersist = await GetCSLProfileResponseInSession(request.SessionId);
                            #region Build MOBCPProfileRequest for GetProfile.
                            MOBCPProfileRequest profileRequest = new MOBCPProfileRequest();
                            profileRequest.Application = request.Application;
                            profileRequest.SessionId = request.SessionId;
                            profileRequest.AccessCode = request.AccessCode;
                            profileRequest.DeviceId = request.DeviceId;
                            profileRequest.LanguageCode = request.LanguageCode;
                            profileRequest.TransactionId = request.TransactionId;
                            profileRequest.MileagePlusNumber = request.MileagePlusNumber;
                            profileRequest.Token = request.Token;
                            profileRequest.IncludeAllTravelerData = true;
                            #endregion
                            MOBCPProfileResponse profileResponse = new MOBCPProfileResponse();
                            profileResponse.TransactionId = request.TransactionId;
                            if (string.IsNullOrEmpty(session.EmployeeId))
                            {
                                profileRequest.Path = "BOOKING";
                                profileResponse.Profiles = await _customerProfile.GetProfile(profileRequest);
                            }
                            else
                            {
                                if (_configuration.GetValue<bool>("EnableEmp20PassRidersUpdate") && _configuration.GetValue<bool>("GetEmp20PassRidersFromEResService"))
                                {
                                    profileResponse.Profiles = await _empProfile.GetEmpProfile(profileRequest, true);
                                }
                                else
                                {
                                    bool getEmployeeIdFromCSLCustomerData = !string.IsNullOrEmpty(_configuration.GetValue<string>("GetEmployeeIDFromGetProfileCustomerData")) && Convert.ToBoolean(_configuration.GetValue<string>("GetEmployeeIDFromGetProfileCustomerData"));
                                    profileResponse.Profiles = await _empProfile.GetEmpProfile(profileRequest, getEmployeeIdFromCSLCustomerData);
                                    if (profileResponse.Profiles != null && profileResponse.Profiles.Count > 0 && profileResponse.Profiles[0].Travelers != null && profileResponse.Profiles[0].Travelers.Count > 0)
                                    {
                                        for (int i = 0; i < profileResponse.Profiles[0].Travelers.Count; i++)
                                        {
                                            if (profileResponse.Profiles[0].Travelers[i].Key.Equals(request.Traveler.Key))
                                            {
                                                if (!string.IsNullOrEmpty(request.Traveler.FirstName) && !string.IsNullOrEmpty(request.Traveler.LastName) && !string.IsNullOrEmpty(request.Traveler.GenderCode) && !string.IsNullOrEmpty(request.Traveler.BirthDate))
                                                {
                                                    request.Traveler.Message = string.Empty;
                                                }

                                                profileResponse.Profiles[0].Travelers[i] = request.Traveler;

                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            #region Special Needs

                            // Validate the selected special needs as well update their list of messagesected 
                            if (_travelerUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
                            {
                                UpdateSpecialNeedsInProfile(profileResponse.Profiles, request.Traveler);
                            }

                            #endregion

                            _memberProfileUtility.UpdateTravelerCubaReasonInProfile(profileResponse.Profiles, request.Traveler);
                            response.Profiles = profileResponse.Profiles;
                            profilePersist.SessionId = session.SessionId;
                            profilePersist.CartId = session.CartId;
                            profilePersist.Request = ObjectToObjectCasting<MOBCustomerProfileRequest, MOBCPProfileRequest>(profileRequest);
                            profilePersist.Response = ObjectToObjectCasting<MOBCustomerProfileResponse, MOBCPProfileResponse>(profileResponse);
                            response.InsertUpdateKeys = insertUpdateItemKeys;
                            string updatedCCKey = string.Empty;
                            if (request.UpdateCreditCardInfo)
                            {
                                var creditCardKey = (from t in insertUpdateItemKeys
                                                     where t.Id.ToUpper().Trim() == "CreditCardKey".ToUpper().Trim()
                                                     select t).ToList();
                                updatedCCKey = (creditCardKey == null ? string.Empty : creditCardKey[0].CurrentValue);
                            }
                            await _sessionHelperService.SaveSession<ProfileResponse>(profilePersist, profilePersist.SessionId, new List<string> { profilePersist.SessionId, profilePersist.ObjectName }, profilePersist.ObjectName);
                            response.Reservation = await GetReservationFromPersist(request.SessionId, updatedCCKey, request.Traveler);
                            if (!response.Reservation.IsCubaTravel ||
                                (response.Reservation.IsCubaTravel && request.Traveler.CubaTravelReason != null && !string.IsNullOrEmpty(request.Traveler.CubaTravelReason.Vanity)))
                            {
                                request.Traveler.Message = string.Empty;
                            }
                            if (isActionCalledForAddNewCCAddNewAddress == false) // To handle the Andriod Client calling same Action for ADD New CC and Add New Address
                            {
                                await UpdateTraveletToAllEligibleTravelerCsl(request, request.Traveler, response, session);
                            }
                            _memberProfileUtility.UpdateMatchedTravelerCubaReason(request.Traveler, response.Reservation.TravelersCSL);
                            if (response.Reservation != null && session != null && !string.IsNullOrEmpty(session.EmployeeId))
                            {
                                response.Reservation.IsEmp20 = true;
                            }
                            #endregion
                        }
                        else
                        {
                            #region // Need to fix populate Profile here

                            MOBMPNameMissMatchResponse response1 = new MOBMPNameMissMatchResponse();
                            response1.MileagePlusNumber = response.MileagePlusNumber;
                            try
                            {
                                request.Token = session.Token;
                                //request.CartId = session.CartId;

                                response.CartId = session.CartId;
                                response.SessionId = request.SessionId;
                                response.Token = session.Token;
                                response.MileagePlusNumber = request.MileagePlusNumber;
                                response.TransactionId = request.TransactionId;
                                MOBRequest mobRequest = new MOBRequest();
                                mobRequest.Application = request.Application;
                                mobRequest.AccessCode = request.AccessCode;
                                mobRequest.DeviceId = request.DeviceId;
                                mobRequest.LanguageCode = request.LanguageCode;
                                mobRequest.TransactionId = request.TransactionId;

                                #region Special Needs
                                //var bookingPathReservation = new Persist.Definition.Shopping.Reservation();
                                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

                                // Validate the selected special needs as well update their list of messages
                                if (_shoppingUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
                                {
                                    var finalSpecialNeeds =  await _travelerUtility.ValidateSpecialNeedsAgaintsMasterList(request.Traveler.SelectedSpecialNeeds, bookingPathReservation.ShopReservationInfo2.SpecialNeeds, request.Application.Id, request.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false);
                                    request.Traveler.SelectedSpecialNeeds = finalSpecialNeeds;

                                    // Special case per Lyneth's request. found out when testing Cuba travel scenario
                                    request.Traveler.SelectedSpecialNeedMessages = PersistSelectedSpecialNeedMessages(request.Traveler);
                                }

                                #endregion
                                if (_shoppingUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major))
                                {
                                    if (bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes.Count > 0)
                                    {
                                        if (!string.IsNullOrEmpty(request.Traveler.BirthDate))
                                        {
                                            int age = TopHelper.GetAgeByDOB(request.Traveler.BirthDate, bookingPathReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTime);
                                            request.Traveler.TravelerTypeCode = _travelerUtility.GetTypeCodeByAge(age, true);
                                            //request.Traveler.TravelerTypeDescription = Mobile.DAL.Utility.GetPaxDescription(request.Traveler.TravelerTypeCode);
                                            request.Traveler.TravelerTypeDescription = _travelerUtility.GetPaxDescriptionByDOB(request.Traveler.BirthDate, bookingPathReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTime);
                                            if (_shoppingUtility.EnableYADesc() && bookingPathReservation.ShopReservationInfo2.IsYATravel)
                                            {
                                                request.Traveler.PTCDescription = _mPTraveler.GetYAPaxDescByDOB();
                                            }
                                            else
                                            {
                                                request.Traveler.PTCDescription = request.Traveler.TravelerTypeDescription;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (_shoppingUtility.EnableYADesc() && bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.IsYATravel)
                                    {
                                        request.Traveler.PTCDescription = _mPTraveler.GetYAPaxDescByDOB();
                                    }
                                }
                                response.Traveler = await RegisterNewTravelerValidateMPMisMatch(request.Traveler, mobRequest, request.SessionId, session.CartId, request.Token);
                                response.isMPNameMisMatch = response.Traveler.isMPNameMisMatch;
                                bool updatedBasicInfo = false;
                                if (!request.UpdateCreditCardInfo && !request.UpdateAddressInfoAssociatedWithCC)
                                {
                                    updatedBasicInfo = await _memberProfileUtility.UpdateViewName(request);
                                }
                                response.Reservation = await GetReservationFromPersist(request.SessionId, string.Empty, null);
                                await UpdateTraveletToAllEligibleTravelerCsl(request, request.Traveler, response, session);
                                //Saving update info to DB as need to save basic info regardless of save to profile toggle. 
                                if (!String.IsNullOrEmpty(request.Traveler.Key) && updatedBasicInfo)
                                {
                                    try
                                    {
                                        await _mPTraveler.UpdateTravelerBase(request);
                                    }
                                    catch (Exception e) { }
                                }
                                if (response.isMPNameMisMatch)
                                {
                                    response.Exception = new MOBException("1111", _configuration.GetValue<string>("ValidateMPNameMisMatchErrorMessage"));
                                }
                                #region
                                ProfileResponse profilePersistCopy = new ProfileResponse();
                                profilePersistCopy = await GetCSLProfileResponseInSession(request.SessionId);
                                if (profilePersistCopy != null && profilePersistCopy.Response != null)
                                {
                                    MOBCPProfileResponse profileResponse = profilePersistCopy.Response;
                                    response.Profiles = profileResponse.Profiles;
                                }
                                #endregion
                            }
                            catch (WebException webResponse)
                            {
                                if (webResponse.Response != null && webResponse.Response.ContentLength != 0)
                                {
                                    string webExceptionText = ReadResponseStream(webResponse.Response.GetResponseStream());

                                    throw;
                                }
                            }
                            catch (MOBUnitedException uaex)
                            {

                                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);
                                _logger.LogWarning("ValidateMPNameMisMatch_CFOP - MPSignedIn {@UnitedException}", JsonConvert.SerializeObject(uaex));


                                response.Exception = new MOBException();
                                response.Exception.Message = uaex.Message;
                            }
                            catch (System.Exception ex)
                            {
                                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);

                                _logger.LogError("ValidateMPNameMisMatch_CFOP - MPSignedIn {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));


                                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                                {
                                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                                }
                                else
                                {
                                    response.Exception = new MOBException("9999", ex.Message);
                                }
                            }

                            if (_configuration.GetValue<string>("Log_CSL_Call_Statistics") != null && _configuration.GetValue<string>("Log_CSL_Call_Statistics").ToString().ToUpper().Trim() == "TRUE")
                            {
                                string callDurations = response.CallDuration.ToString();
                                var cslStatics = (new CSLStatistics(_logger, _configuration, _dynamoDBService, _cSLStatisticsService));
                                try
                                {

                                    await cslStatics.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", response.CartId, callDurations, "PRofileCSL/ValidateMPNameMisMatch_CFOP - MPSignedIn", request.SessionId);
                                }
                                catch { }
                            }
                            //return response;

                            #endregion
                        }
                    }
                    else
                    {
                        response.Exception = new MOBException();
                        response.Exception.Message = _configuration.GetValue<string>("GenericExceptionMessage").ToString();
                    }
                    #endregion
                }

                if (!_configuration.GetValue<bool>("DisableCSLGetProfileExceptionCheck") ? response.Exception == null : true)//MOBILE-13448 Bug Fix(Below block should not be executed if there is any exception in the response)
                {
                    response.Reservation.IsBookingCommonFOPEnabled = bookingPathReservation.IsBookingCommonFOPEnabled;
                    response.Reservation.IsReshopCommonFOPEnabled = bookingPathReservation.IsReshopCommonFOPEnabled;
                    shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName, new List<String> { request.SessionId, shoppingCart.ObjectName }).ConfigureAwait(false);
                    shoppingCart = await _shoppingUtility.ReservationToShoppingCart_DataMigration(response.Reservation, shoppingCart, request, session);
                    shoppingCart.Flow = request.Flow;
                    await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, request.SessionId, new List<String> { request.SessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName);
                    response.ShoppingCart = shoppingCart;
                }

                _travelerUtility.ExtraSeatHandlingForProfile(request, response, session);
            }
            catch (MOBUnitedException uaex)
            {
                #region

                MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);
                _logger.LogWarning("MPSignedInInsertUpdateTraveler_CFOP Error {@UnitedException}", JsonConvert.SerializeObject(uaex));


                if (uaex != null && !string.IsNullOrEmpty(uaex.Message.Trim()))
                {
                    response.Exception = new MOBException();
                    response.Exception.Message = uaex.Message;
                }
                else
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                #endregion
            }
            catch (System.Exception ex)
            {
                #region

                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);

                _logger.LogError("MPSignedInInsertUpdateTraveler_CFOP Error {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));


                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
                #endregion
            }

            if (_configuration.GetValue<string>("Log_CSL_Call_Statistics") != null && _configuration.GetValue<string>("Log_CSL_Call_Statistics").ToString().ToUpper().Trim() == "TRUE")
            {
                string callDurations = response.CallDuration.ToString();//TODO _shoppingUtility.GetCSLCallDuration();
                var cslStatics = (new CSLStatistics(_logger, _configuration, _dynamoDBService, _cSLStatisticsService));
                try
                {
                    await cslStatics.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", response.CartId, callDurations, "Profile / MPSignedInInsertAndUpdateTraveler_CFOP", request.SessionId);
                }
                catch { }

            }

            return response;
            #endregion
        }

        public async Task<FrequentFlyerRewardProgramsResponse> GetLatestFrequentFlyerRewardProgramList(int applicationId, string appVersion, string accessCode, string transactionId, string languageCode)
        {
            FrequentFlyerRewardProgramsResponse response = new FrequentFlyerRewardProgramsResponse();
            MOBRequest request = new MOBRequest();
            request.Application = new MOBApplication();
            request.Application.Id = applicationId;
            request.Application.Version = new MOBVersion();
            request.Application.Version.Build = appVersion;
            request.TransactionId = transactionId;
            request.LanguageCode = languageCode;

            response.TransactionId = request.TransactionId;
            response.LanguageCode = request.LanguageCode;

            #region
            List<RewardProgram> rewardProgramList = new List<RewardProgram>();
            var key = _configuration.GetValue<string>("FrequestFlyerRewardProgramListStaticGUID") + "Booking2.0FrequentFlyerList";
            var frequestFlyerList = await _cachingService.GetCache<string>(key, "TID1").ConfigureAwait(false);
            rewardProgramList = JsonConvert.DeserializeObject<List<RewardProgram>>(frequestFlyerList);

            if (rewardProgramList != null && rewardProgramList.Count > 1)
            {
                response.RewardProgramList = rewardProgramList;
                response.Exception = new MOBException();
                response.Exception.Code = "SUCCESS";
                response.Exception.Message = "Successfully returned Frequest Flyer Reward Program List.";
            }
            else if (!_configuration.GetValue<bool>("DisableFFPListUpdate_MOBILE21139") && (rewardProgramList == null || (rewardProgramList != null && rewardProgramList.Count == 0)))
            {
                string deviceID = "SCHEDULED_PublicKey_UPDADE_JOB";
                string cachedTokenForThisApp = await _dpService.GetAnonymousToken(request.Application.Id, request.DeviceId, _configuration).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(cachedTokenForThisApp))
                {
                    rewardProgramList = await GetRewardPrograms(applicationId, deviceID, appVersion, transactionId, transactionId, cachedTokenForThisApp).ConfigureAwait(false);
                    if (rewardProgramList != null && rewardProgramList.Count > 1)
                    {
                        await _cachingService.SaveCache<List<RewardProgram>>(_configuration.GetValue<string>("FrequestFlyerRewardProgramListStaticGUID") + "Booking2.0FrequentFlyerList", rewardProgramList, "TID1", new TimeSpan(1, 30, 0));
                    }
                    response.RewardProgramList = rewardProgramList;
                    response.Exception = new MOBException();
                    response.Exception.Code = "SUCCESS";
                    response.Exception.Message = "Successfully returned Frequest Flyer Reward Program List.";
                }
                else
                {
                    throw new System.Exception("Get Cached Token For the App Failed.");
                }
            }
            else
            {
                response.Exception = new MOBException();
                response.Exception.Code = "9999";
                response.Exception.Message = "No Frequest Flyer List Returned.";
            }
            #endregion

            return response;
        }

        public async Task<MOBCPProfileResponse> UpdateTravelerCCPromo_CFOP(MOBUpdateTravelerRequest request)
        {
            request.Traveler = null;
            MOBCPProfileResponse response = new MOBCPProfileResponse();
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            response.SessionId = request.SessionId;

            #region ALM 24989  - Dover Release - deviceid validation - Modified by Srini 12/29/2015

            bool validUpdateTravlerRequest = await _memberProfileUtility.isValidDeviceRequest(request.TransactionId, request.DeviceId, request.Application.Id, request.MileagePlusNumber, request.SessionId);

            #endregion
            if (validUpdateTravlerRequest)
            {
                #region
                Session session = await _shoppingSessionHelper.GetShoppingSession(request.SessionId);

                #region Creating new token for getprofile cache refresh
                //request.Token = session.]Token;
                request.Token = await _dpService.GetAnonymousToken(request.Application.Id, request.DeviceId, _configuration).ConfigureAwait(false);
                #endregion Creating new token for getprofile cache refresh

                response.TransactionId = request.TransactionId;

                ProfileResponse profilePersist = new ProfileResponse();
                profilePersist = await GetCSLProfileResponseInSession(request.SessionId);

                List<string> lstCardKeys;
                lstCardKeys = profilePersist.Response.Profiles[0].Travelers.First(t => t.IsProfileOwner).CreditCards.Select(c => c.Key).ToList();

                #region Build MOBCPProfileRequest for GetProfile.
                MOBCPProfileRequest profileRequest = new MOBCPProfileRequest();
                profileRequest.Application = request.Application;
                profileRequest.SessionId = request.SessionId;
                profileRequest.AccessCode = request.AccessCode;
                profileRequest.DeviceId = request.DeviceId;
                profileRequest.LanguageCode = request.LanguageCode;
                profileRequest.TransactionId = request.TransactionId;
                profileRequest.MileagePlusNumber = request.MileagePlusNumber;
                profileRequest.Token = request.Token;
                profileRequest.IncludeAllTravelerData = true;
                #endregion
                MOBCPProfileResponse profileResponse = new MOBCPProfileResponse();
                profileResponse.TransactionId = request.TransactionId;
                profileRequest.Path = "BOOKING";
                profileResponse.Profiles = await _customerProfile.GetProfile(profileRequest);

                response.Profiles = profileResponse.Profiles;
                profilePersist.SessionId = session.SessionId;
                profilePersist.CartId = session.CartId;
                profilePersist.Request = ObjectToObjectCasting<MOBCustomerProfileRequest, MOBCPProfileRequest>(profileRequest);
                profilePersist.Response = ObjectToObjectCasting<MOBCustomerProfileResponse, MOBCPProfileResponse>(profileResponse);
                string updatedCCKey = string.Empty;
                List<string> lstCCProfileResponse = response.Profiles[0].Travelers.First(t => t.IsProfileOwner).CreditCards.Select(c => c.Key).ToList();

                if (lstCCProfileResponse.Count > lstCardKeys.Count)
                {
                    string chaseCardKey = lstCCProfileResponse.Except(lstCardKeys).ToList()[0];

                    if (chaseCardKey != null)
                    {
                        updatedCCKey = chaseCardKey;
                    }
                    else
                    {
                        updatedCCKey = string.Empty;
                    }
                }

                await _sessionHelperService.SaveSession<ProfileResponse>(profilePersist, profilePersist.SessionId, new List<String> { profilePersist.SessionId, profilePersist.ObjectName }, profilePersist.ObjectName);
                response.Reservation = await GetReservationFromPersistWithChaseCC(request.SessionId, updatedCCKey);

                if (!string.IsNullOrEmpty(updatedCCKey) && response.Reservation != null && response.Reservation.CreditCards != null && response.Reservation.CreditCards.Count() > 0 && response.Profiles != null &&
                    response.Profiles.Count() > 0 && response.Profiles[0].Travelers != null && response.Profiles[0].Travelers.Count() > 0 && response.Profiles[0].Travelers[0].CreditCards != null)
                {
                    response.Profiles[0].Travelers[0].CreditCards.ForEach(p => p.IsPrimary = (p.Key == updatedCCKey));
                }

                Reservation bookingPathReservation = new Reservation();
                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                response.Reservation.IsBookingCommonFOPEnabled = bookingPathReservation.IsBookingCommonFOPEnabled;
                shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, shoppingCart.ObjectName, new List<string> { request.SessionId, shoppingCart.ObjectName }).ConfigureAwait(false);
                shoppingCart = await _shoppingUtility.ReservationToShoppingCart_DataMigration(response.Reservation, shoppingCart, request,session);
                shoppingCart.Flow = request.Flow;
                response.ShoppingCart = shoppingCart;

                if (await _featureSettings.GetFeatureSettingValue("EnableChaseRTIClickFix_MOBILE-35427"))
                {
                    United.Mobile.Model.Common.Shopping.FormofPaymentOption persistedFormofPaymentOption = new United.Mobile.Model.Common.Shopping.FormofPaymentOption();
                    var efop = await _sessionHelperService.GetSession<List<United.Mobile.Model.Common.Shopping.FormofPaymentOption>>(request.SessionId, "System.Collections.Generic.List`1[United.Definition.FormofPaymentOption]", new List<string> { request.SessionId, "System.Collections.Generic.List`1[United.Definition.FormofPaymentOption]" }).ConfigureAwait(false);
                    await _shoppingUtility.UpdateFSRMoneyPlusMilesOptionsBasedOnEFOP(request, session, shoppingCart, response.Reservation, efop).ConfigureAwait(false);
                }

                profilePersist.Response = ObjectToObjectCasting<MOBCustomerProfileResponse, MOBCPProfileResponse>(response);
                await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, profilePersist.SessionId, new List<string> { profilePersist.SessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName);
                await _sessionHelperService.SaveSession<ProfileResponse>(profilePersist, profilePersist.SessionId, new List<string> { profilePersist.SessionId, profilePersist.ObjectName }, profilePersist.ObjectName);

                #endregion
            }
            else
            {
                response.Exception = new MOBException();
                response.Exception.Message = _configuration.GetValue<string>("GenericExceptionMessage").ToString();
            }

            return response;
        }

        public async Task<MOBUpdateTravelerInfoResponse> UpdateTravelersInformation(MOBUpdateTravelerInfoRequest request)
        {
            MOBUpdateTravelerInfoResponse response = new MOBUpdateTravelerInfoResponse();
            Session session = null;
            if (!string.IsNullOrEmpty(request.SessionId))
            {
                session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId);
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            _logger.LogInformation("UpdateTravelerInfo {Request} and {SessionId}", request, request.SessionId);


            if (GeneralHelper.IsApplicationVersionGreater
                (request.Application.Id, request.Application.Version.Major, "AndroidEnableMgnResUpdateSpecialNeeds", "iPhoneEnableMgnResUpdateSpecialNeeds", string.Empty, string.Empty, true, _configuration))
                response = await _mPTraveler.ManageTravelerInfo(request, session.Token);
            else
                response = await _mPTraveler.UpdateTravelerInfo(request, session.Token);

            response.SessionId = request.SessionId;
            response.TransactionId = request.TransactionId;
            response.LanguageCode = request.LanguageCode;


            _logger.LogInformation("UpdateTravelerInfo {Response} and {SessionId}", response, request.SessionId);

            return response;
        }

        private async Task<MPStatusLiftBanner> GetStatusLiftBanner(MPAccountValidationRequest request)
        {
            MOBStatusLiftBannerResponse statusLiftBannerresponse = new MOBStatusLiftBannerResponse();
            var statusLiftBannerResponse = new MPStatusLiftBanner();
            bool isSupportedVersion = _memberProfileUtility.isApplicationVersionGreater2(request.Application.Id, request.Application.Version.Build, "androidMapVersion33", "iPhoneMapVersion34", "", "");
            if (Convert.ToBoolean(isSupportedVersion))
            {
                string authToken = await _dpService.GetAnonymousToken(request.Application.Id, request.DeviceId, _configuration);
                //We passsing Customer ID = 0 as the SP uasp_Update_MileagePlus_CSS_Token which does not need a custmer ID to update. this SP uasp_Update_MileagePlus_CSS_Token is to update with latest css token
                statusLiftBannerresponse = await GetStatusLiftBanner(request, authToken);
            }

            if (statusLiftBannerresponse != null && statusLiftBannerresponse.EligibleLevelCode != null && !string.IsNullOrEmpty(statusLiftBannerresponse.EligibleLevelCode)
                && statusLiftBannerresponse.StatusLiftURL != null && !string.IsNullOrEmpty(statusLiftBannerresponse.StatusLiftURL))
            {
                statusLiftBannerResponse.ImageSrcURL = string.Format(_configuration.GetValue<string>("statusLiftBannerSourceURL"), statusLiftBannerresponse.EligibleLevelCode);
                statusLiftBannerResponse.PremierStatusURL = statusLiftBannerresponse.StatusLiftURL;
            }

            return statusLiftBannerResponse;
        }

        public async Task<MOBStatusLiftBannerResponse> GetStatusLiftBanner(MPAccountValidationRequest request, string token)
        {
            string jsonResponse = null;
            MOBStatusLiftBannerResponse response = null;
            try
            {
                string jsonRequest = JsonConvert.SerializeObject(request);
                string path = string.Format(_configuration.GetValue<string>("GetPromotionandBasedonMPNumberURL"), _configuration.GetValue<string>("promotionLiftID"), request.MileagePlusNumber);

                jsonResponse = await _statusLiftBannerService.GetStatusLiftBanner(token, path, string.Empty);

                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    var cslResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<MemberPromotion<MOBStatusLiftBannerResponse>>(jsonResponse);
                    response = cslResponse.MetaData;
                    response.StatusLiftURL = cslResponse.Promotion.VanityUrl;
                    response.PromoCode = cslResponse.PromotionId.ToString();
                    response.MileagePlusNumber = cslResponse.MpId.ToString();
                    response.CustID = cslResponse.AltRefId1.ToString();
                    response.ExpirationDate = cslResponse.ExpirationDate;

                    if (response != null)
                    {
                        _logger.LogInformation("GetStatusLiftBanner DeSerialized Response {response} and {sessionID)", response, request.SessionId);
                    }
                }
                else
                {
                    string exceptionMessage = string.Empty;
                    if (_configuration.GetSection("ReturnActualExceptionMessageBackForTesting") != null && _configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                    {
                        exceptionMessage = "Unable to get the promotion due to jsonResponse is empty at DAL GetStatusLiftBanner(MOBMPAccountValidationRequest request, string token  )";
                    }
                    throw new MOBUnitedException(exceptionMessage);
                }
            }
            catch (System.Net.WebException wex)
            {
                if (((System.Net.HttpWebResponse)((System.Net.WebException)wex).Response).StatusCode.ToString() == "NotFound" || ((System.Net.HttpWebResponse)((System.Net.WebException)wex).Response).StatusCode.ToString() == "InternalServerError")
                {
                    response = new MOBStatusLiftBannerResponse();
                    response.Exception = new MOBException("221924", _configuration.GetValue<string>("promotionNotFoundMessage"));

                    if (!_configuration.GetValue<bool>("Log_Handled_Exception_NoPromotion_Available")) //This should be false in PROD and can be true in all othe enviroments. 
                    {
                        _logger.LogError("GetPromotionandCheckProgress United Exception {Message} {StackTrace} and {sessionID}", wex.Message, wex.StackTrace, request.SessionId);
                    }

                    return response;
                }

                if (wex.Response != null)
                {
                    var errorResponse = new StreamReader(wex.Response.GetResponseStream()).ReadToEnd();

                    _logger.LogError("GetStatusLiftBanner - Exception ErrorMessageResponse {Response} and {sessionID}", errorResponse, request.SessionId);

                    throw new System.Exception(wex.Message);
                }
            }
            return response;
        }

        private Session CreateSubscriptionSession()
        {
            Session session = new Session();
            session.SessionId = Guid.NewGuid().ToString().ToUpper().Replace("-", "");
            return session;
        }

        private MOBContactUsUSACanada GetContactUsUSACanada(MOBMemberType memberType, bool isCEO, List<MOBLegalDocument> CallingcardList)
        {
            List<MOBContactUSUSACanadaPhoneNumber> lstCOntactDetails = new List<MOBContactUSUSACanadaPhoneNumber>
            {
                new MOBContactUSUSACanadaPhoneNumber
                                {
                                    ContactUsDeskName =CallingcardList.Find(item => item.Title =="CallingCardReservationAssistanceText").LegalDocument,
                                    ContactUsDeskDescription =string.Empty,
                                    ContactUsDeskPhoneNumber =CallingcardList.Find(item => item.Title =="CallingCardReservationAssistanceCell").LegalDocument
                                },
                                 new MOBContactUSUSACanadaPhoneNumber
                                {
                                    ContactUsDeskName =CallingcardList.Find(item => item.Title =="CallingCardBaggageServiceText").LegalDocument,
                                    ContactUsDeskDescription =string.Empty,
                                    ContactUsDeskPhoneNumber =CallingcardList.Find(item => item.Title =="CallingCardBaggageServiceCell").LegalDocument
                                },
                                new MOBContactUSUSACanadaPhoneNumber
                                {
                                    ContactUsDeskName =CallingcardList.Find(item => item.Title =="CallingFlightArrivalDepartureText").LegalDocument,
                                    ContactUsDeskDescription =CallingcardList.Find(item => item.Title =="CallingAutoMatedInformationText").LegalDocument,
                                    ContactUsDeskPhoneNumber =CallingcardList.Find(item => item.Title =="CallingFlightArrivalDepartureCell").LegalDocument
                                }
            };
            List<MOBContactUSEmail> lstemailContact = new List<MOBContactUSEmail>
            {
                new MOBContactUSEmail
                {
                   ContactUsDeskEmailName= CallingcardList.Find(item => item.Title =="CallingPremier1KCustomerCareText").LegalDocument,
                    ContactUsDeskEmailAddress = CallingcardList.Find(item => item.Title =="CallingFlightArrivalDepartureEmail").LegalDocument
                }
            };

            if (memberType == MOBMemberType.Premier1K)
            {
                lstCOntactDetails.Insert(0, new MOBContactUSUSACanadaPhoneNumber
                {
                    ContactUsDeskName = CallingcardList.Find(item => item.Title == "CallingPremier1kAsissistanceText").LegalDocument,
                    ContactUsDeskDescription = CallingcardList.Find(item => item.Title == "CallingPremier1kAsissistanceDescription").LegalDocument,
                    ContactUsDeskPhoneNumber = CallingcardList.Find(item => item.Title == "CallingPremier1kAsissistanceCell").LegalDocument
                });
                lstCOntactDetails.Insert(3, new MOBContactUSUSACanadaPhoneNumber
                {
                    ContactUsDeskName = CallingcardList.Find(item => item.Title == "CallingMilagePlusAssistanceText").LegalDocument,
                    ContactUsDeskDescription = string.Empty,
                    ContactUsDeskPhoneNumber = CallingcardList.Find(item => item.Title == "CallingMilagePlusAssistanceCell").LegalDocument
                });
                return new MOBContactUsUSACanada(_configuration)
                {
                    USACanadaContactTypeEmail = new MOBContactUSContactTypeEmail
                    {
                        ContactType = CallingcardList.Find(item => item.Title == "DefaultEmailAddressContactType").LegalDocument,
                        EmailAddresses = lstemailContact
                    },
                    USACanadaContactTypePhone = new MOBContactUSUSACanadaContactTypePhone
                    {
                        ContactType = CallingcardList.Find(item => item.Title == "InternaitonPhoneContactType").LegalDocument,
                        PhoneNumbers = lstCOntactDetails
                    }
                };
            }
            if (memberType == MOBMemberType.PremierSilver || memberType == MOBMemberType.PremierGold || memberType == MOBMemberType.PremierPlatinium)
            {
                lstCOntactDetails.Insert(0, new MOBContactUSUSACanadaPhoneNumber
                {
                    ContactUsDeskName = CallingcardList.Find(item => item.Title == "CallingPremierPriorityAssistanceText").LegalDocument,
                    ContactUsDeskDescription = CallingcardList.Find(item => item.Title == "CallingPremierPriorityAssistanceDescription").LegalDocument,
                    ContactUsDeskPhoneNumber = CallingcardList.Find(item => item.Title == "CallingPremierPriorityAssistanceCell").LegalDocument
                });
                lstCOntactDetails.Insert(3, new MOBContactUSUSACanadaPhoneNumber
                {
                    ContactUsDeskName = CallingcardList.Find(item => item.Title == "CallingMilagePlusAssistanceText").LegalDocument,
                    ContactUsDeskDescription = string.Empty,
                    ContactUsDeskPhoneNumber = CallingcardList.Find(item => item.Title == "CallingMilagePlusAssistanceForSilverGoldPlatinumCell").LegalDocument
                });
                return new MOBContactUsUSACanada(_configuration)
                {
                    USACanadaContactTypePhone = new MOBContactUSUSACanadaContactTypePhone
                    {
                        ContactType = CallingcardList.Find(item => item.Title == "InternaitonPhoneContactType").LegalDocument,
                        PhoneNumbers = lstCOntactDetails
                    }
                };

            }
            else if (memberType == MOBMemberType.PremierGlobalServices)
            {
                lstCOntactDetails.Insert(0, new MOBContactUSUSACanadaPhoneNumber
                {
                    ContactUsDeskName = isCEO ? CallingcardList.Find(item => item.Title == "CallingCharimanCircleText").LegalDocument : CallingcardList.Find(item => item.Title == "CallingUnitedGlobalText").LegalDocument,
                    ContactUsDeskDescription = CallingcardList.Find(item => item.Title == "CallingCharimanCircleDescription").LegalDocument,
                    ContactUsDeskPhoneNumber = isCEO ? CallingcardList.Find(item => item.Title == "CallingCharimanCircleCell").LegalDocument : CallingcardList.Find(item => item.Title == "CallingUnitedGlobalCell").LegalDocument
                });

                lstCOntactDetails.Insert(3, new MOBContactUSUSACanadaPhoneNumber
                {
                    ContactUsDeskName = CallingcardList.Find(item => item.Title == "CallingMilagePlusAssistanceText").LegalDocument,
                    ContactUsDeskDescription = string.Empty,
                    ContactUsDeskPhoneNumber = isCEO ? CallingcardList.Find(item => item.Title == "CallingCharimanCircleCell").LegalDocument : CallingcardList.Find(item => item.Title == "CallingUnitedGlobalCell").LegalDocument
                });
                return new MOBContactUsUSACanada(_configuration)
                {
                    USACanadaContactTypeEmail = new MOBContactUSContactTypeEmail
                    {
                        ContactType = CallingcardList.Find(item => item.Title == "DefaultEmailAddressContactType").LegalDocument,
                        EmailAddresses = new List<MOBContactUSEmail>
                        {
                            new MOBContactUSEmail
                            {
                                ContactUsDeskEmailName = CallingcardList.Find(item => item.Title == "CallingFlightGlobalServicesText").LegalDocument,
                                ContactUsDeskEmailAddress = CallingcardList.Find(item => item.Title == "CallingFlightGlobalServicesEmail").LegalDocument
                            }
                        }
                    },
                    USACanadaContactTypePhone = new MOBContactUSUSACanadaContactTypePhone
                    {
                        ContactType = CallingcardList.Find(item => item.Title == "InternaitonPhoneContactType").LegalDocument,
                        PhoneNumbers = lstCOntactDetails
                    }
                };

            }
            else
            {
                lstCOntactDetails.Insert(0, new MOBContactUSUSACanadaPhoneNumber
                {
                    ContactUsDeskName = CallingcardList.Find(item => item.Title == "CallingTravelreservationText").LegalDocument,
                    ContactUsDeskDescription = CallingcardList.Find(item => item.Title == "CallingTravelreservationDescription").LegalDocument,
                    ContactUsDeskPhoneNumber = CallingcardList.Find(item => item.Title == "CallingTravelreservationCell").LegalDocument
                });

                lstCOntactDetails.Insert(3, new MOBContactUSUSACanadaPhoneNumber
                {
                    ContactUsDeskName = CallingcardList.Find(item => item.Title == "CallingMilagePlusAssistanceText").LegalDocument,
                    ContactUsDeskDescription = CallingcardList.Find(item => item.Title == "CallingAutoMatedInformationText").LegalDocument,
                    ContactUsDeskPhoneNumber = CallingcardList.Find(item => item.Title == "CallingMilagePlusGeneralAssistanceCell").LegalDocument
                });
                return new MOBContactUsUSACanada(_configuration)
                {

                    USACanadaContactTypePhone = new MOBContactUSUSACanadaContactTypePhone
                    {
                        ContactType = CallingcardList.Find(item => item.Title == "InternaitonPhoneContactType").LegalDocument,
                        PhoneNumbers = lstCOntactDetails
                    }
                };
            };
        }

        private async Task<MOBContactUSOutSideUSACanada> GetContactUSOusideUSACanada(MOBMemberType memberType, bool isCEO, List<MOBLegalDocument> CallingcardList)
        {
            MOBContactUSOutSideUSACanada outSideUSACanadaContacts = new MOBContactUSOutSideUSACanada(_configuration);
            if (memberType == MOBMemberType.Premier1K || memberType == MOBMemberType.PremierGlobalServices)
            {
                outSideUSACanadaContacts.HowToUseOutsideUSACanadaATTDirectAccessNumberDescription = CallingcardList.Find(item => item.Title == nameof(outSideUSACanadaContacts.HowToUseOutsideUSACanadaATTDirectAccessNumberDescription))?.LegalDocument;
                outSideUSACanadaContacts.OutSideUSACanadaContactATTTollFreeNumber = memberType == MOBMemberType.PremierGlobalServices ? isCEO ? CallingcardList.Find(item => item.Title == "CallingChairMenCircleTollFreeNumber")?.LegalDocument : CallingcardList.Find(item => item.Title == "CallingGlobalServicesTollFreeNumber")?.LegalDocument : memberType == MOBMemberType.Premier1K ? CallingcardList.Find(item => item.Title == "CallingPremier1KTollFreeNumber")?.LegalDocument : string.Empty;
                outSideUSACanadaContacts.DefaultEmailAddressContactType = CallingcardList.Find(item => item.Title == nameof(outSideUSACanadaContacts.DefaultEmailAddressContactType))?.LegalDocument;
                outSideUSACanadaContacts.SelectCountryDefaultText = CallingcardList.Find(item => item.Title == nameof(outSideUSACanadaContacts.SelectCountryDefaultText))?.LegalDocument;
                outSideUSACanadaContacts.CountryListDefaultSelection = CallingcardList.Find(item => item.Title == nameof(outSideUSACanadaContacts.CountryListDefaultSelection))?.LegalDocument;
                outSideUSACanadaContacts.InternaitonPhoneContactType = CallingcardList.Find(item => item.Title == nameof(outSideUSACanadaContacts.InternaitonPhoneContactType))?.LegalDocument;
                outSideUSACanadaContacts.SelectCountryFromListScreenText = CallingcardList.Find(item => item.Title == nameof(outSideUSACanadaContacts.SelectCountryFromListScreenText))?.LegalDocument;
                outSideUSACanadaContacts.ATTAccessNumberDialInfoText = CallingcardList.Find(item => item.Title == nameof(outSideUSACanadaContacts.ATTAccessNumberDialInfoText))?.LegalDocument;
                outSideUSACanadaContacts.InternationalDefaultEmailAddresses = memberType == MOBMemberType.PremierGlobalServices ?
                    new List<MOBContactUSEmail>
                    {
                        new MOBContactUSEmail
                        {
                            ContactUsDeskEmailName = CallingcardList.Find(item => item.Title == "CallingFlightGlobalServicesText").LegalDocument,
                            ContactUsDeskEmailAddress = CallingcardList.Find(item => item.Title == "CallingFlightGlobalServicesEmail").LegalDocument
                        }
                    } :
                    new List<MOBContactUSEmail>
                    {
                        new MOBContactUSEmail
                        {
                            ContactUsDeskEmailName = CallingcardList.Find(item => item.Title == "CallingPremier1KCustomerCareText").LegalDocument,
                            ContactUsDeskEmailAddress = CallingcardList.Find(item => item.Title == "CallingFlightArrivalDepartureEmail").LegalDocument
                        }
                    };
                outSideUSACanadaContacts.ContactUSLocationDescription = CallingcardList.Find(item => item.Title == "ContactUsLocationsDescription")?.LegalDocument;
                outSideUSACanadaContacts.ContactUSLocationHyperlink = CallingcardList.Find(item => item.Title == "ContactUsLocationsLink")?.LegalDocument;
                outSideUSACanadaContacts.ContactUSDirectAccessNumber = CallingcardList.Find(item => item.Title == "ContactUsDirectAccessNumbers")?.LegalDocument;
                outSideUSACanadaContacts.OutSideUSACanadaContactTypePhoneList = await GetInternationalCallingCard();
                return outSideUSACanadaContacts;
            }
            else return null;
        }

        private async Task<List<MOBContactUSOusideUSACanadaContactTypePhone>> GetInternationalCallingCard()
        {
            List<MOBContactUSOusideUSACanadaContactTypePhone> items = null;
            List<MOBBKCountry> lstCountries = new List<MOBBKCountry>();
            var callingCardDynamoDB = new CallingCardDynamoDB(_configuration, _dynamoDBService);
            var callingCardList = await callingCardDynamoDB.GetInternationalCallingCard(_headers.ContextValues.SessionId);
            #region
            List<string> lstPhoneNumber = null;
            MOBContactAccessNumber CityContact = null;

            foreach (var obj in callingCardList)
            {
                if (items == null)
                {
                    items = new List<MOBContactUSOusideUSACanadaContactTypePhone>();
                }
                MOBContactUSOusideUSACanadaContactTypePhone item = items.FirstOrDefault(c => c.Country.CountryCode == obj.CountryCode);

                lstPhoneNumber = new List<string>();
                if (obj.PhoneNumber.Trim() != string.Empty)
                    lstPhoneNumber.Add(obj.PhoneNumber);
                if (obj.PhoneNumber2.Trim() != string.Empty)
                    lstPhoneNumber.Add(obj.PhoneNumber2);
                if (obj.PhoneNumber3.ToString().Trim() != string.Empty)
                    lstPhoneNumber.Add(obj.PhoneNumber3);
                if (obj.PhoneNumber4.ToString().Trim() != string.Empty)
                    lstPhoneNumber.Add(obj.PhoneNumber4);
                CityContact = new MOBContactAccessNumber
                {
                    City = obj.CityName,
                    ATTDirectAccessNumbers = lstPhoneNumber
                };
                if (item == null)
                {
                    item = new MOBContactUSOusideUSACanadaContactTypePhone();
                    MOBBKCountry country = new MOBBKCountry();
                    item.Country = new MOBBKCountry
                    {
                        CountryCode = obj.CountryCode,
                        Name = obj.CountryName,
                        ShortName = obj.CountryLongName
                    };
                    item.ContactAccessNumberList = new List<MOBContactAccessNumber>();
                    item.ContactAccessNumberList.Add(CityContact);
                    items.Add(item);
                }
                else
                {

                    item.ContactAccessNumberList.Add(CityContact);
                    item.ContactAccessNumberList = item.ContactAccessNumberList.OrderBy(c => c.City).ToList();
                }

            }
            return items;

            #endregion

        }

        private MOBCPProfileRequest GetMOBCPProfileRequest(MOBCustomerProfileRequest request)
        {
            MOBCPProfileRequest res = new MOBCPProfileRequest();

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

        private bool ValidateMPNumberAndCustomerID(int? requestCustomerId, string requestMp, int responseCustomerId,
         string responseMp)
        {
            bool isValid = !((requestCustomerId != null && requestCustomerId != responseCustomerId) || requestMp != responseMp);
            return isValid;
        }

        private List<MOBCreditCard> GetProfileOwnerCreditCardList(MOBCPProfile profile, ref List<MOBAddress> creditCardAddresses, string updatedCCKey)
        {
            #region

            creditCardAddresses = new List<MOBAddress>();
            List<MOBCreditCard> savedProfileOwnerCCList = null;
            #region
            foreach (var traveler in profile.Travelers)
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
                                if (!string.IsNullOrEmpty(creditCard.AddressKey))
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
                    break;
                }
            }
            #endregion
            return savedProfileOwnerCCList;
            #endregion
        }
        public async Task<ProfileResponse> GetCSLProfileResponseInSession(string sessionId)
        {
            ProfileResponse profile = new ProfileResponse();
            try
            {
                profile = await _sessionHelperService.GetSession<ProfileResponse>(sessionId, profile.ObjectName, new List<string> { sessionId, profile.ObjectName }).ConfigureAwait(false);
                profile.Response?.Reservation?.UpdateRewards(_configuration, _cachingService);
            }
            catch (System.Exception)
            {

            }
            return profile;
        }
        //private async Task<(List<MOBCreditCard> savedProfileOwnerCCList, List<MOBAddress> creditCardAddresses, MOBCPPhone mpPhone, MOBEmail mpEmail)> GetProfileOwnerCreditCardList(string sessionID, List<MOBAddress> creditCardAddresses,  MOBCPPhone mpPhone,  MOBEmail mpEmail, string updatedCCKey)
        //{
        //    #region
        //    //List<MOBAddress> nonPrimaryCCAddresses = new List<MOBAddress>();
        //    creditCardAddresses = new List<MOBAddress>();
        //    List<MOBCreditCard> savedProfileOwnerCCList = null;
        //    ProfileResponse profilePersist = new ProfileResponse();
        //    profilePersist = await GetCSLProfileResponseInSession(sessionID);
        //    if (profilePersist.Response != null && profilePersist.Response.Profiles != null)
        //    {
        //        #region
        //        foreach (var traveler in profilePersist.Response.Profiles[0].Travelers)
        //        {
        //            if (traveler.IsProfileOwner)
        //            {
        //                #region
        //                if (traveler.CreditCards != null && traveler.CreditCards.Count > 0)
        //                {
        //                    savedProfileOwnerCCList = new List<MOBCreditCard>();
        //                    //List<MOBCreditCard> nonPrimaryCC = new List<MOBCreditCard>();
        //                    foreach (var creditCard in traveler.CreditCards)
        //                    {
        //                        if (creditCard.IsPrimary || creditCard.Key == updatedCCKey)
        //                        {
        //                            savedProfileOwnerCCList = new List<MOBCreditCard>();
        //                            savedProfileOwnerCCList.Add(creditCard);
        //                            if (!String.IsNullOrEmpty(creditCard.AddressKey))
        //                            {
        //                                foreach (var address in traveler.Addresses)
        //                                {
        //                                    if (address.Key.ToUpper().Trim() == creditCard.AddressKey.ToUpper().Trim())
        //                                    {
        //                                        creditCardAddresses = new List<MOBAddress>();
        //                                        creditCardAddresses.Add(address);
        //                                        break;
        //                                    }
        //                                }
        //                            }
        //                            if (creditCard.Key == updatedCCKey || string.IsNullOrEmpty(updatedCCKey))
        //                            {
        //                                break;
        //                            }
        //                        }
        //                    }
        //                    if (savedProfileOwnerCCList == null || savedProfileOwnerCCList.Count == 0) // To check if there is not Primary CC then get the top 1 CC from the List.
        //                    {
        //                        savedProfileOwnerCCList = new List<MOBCreditCard>();
        //                        savedProfileOwnerCCList.Add(traveler.CreditCards[0]);
        //                        if (traveler.Addresses != null && traveler.Addresses.Count > 0)
        //                        {
        //                            creditCardAddresses = new List<MOBAddress>();
        //                            foreach (var address in traveler.Addresses)
        //                            {
        //                                if (address.Key.ToUpper().Trim() == traveler.CreditCards[0].AddressKey.ToUpper().Trim())
        //                                {
        //                                    creditCardAddresses.Add(address);
        //                                    break;
        //                                }
        //                            }
        //                        }
        //                    }
        //                }
        //                #endregion
        //                if (traveler.ReservationPhones != null && traveler.ReservationPhones.Count > 0)
        //                {
        //                    mpPhone = traveler.ReservationPhones[0];
        //                }
        //                if (traveler.ReservationEmailAddresses != null && traveler.ReservationEmailAddresses.Count > 0)
        //                {
        //                    mpEmail = traveler.ReservationEmailAddresses[0];
        //                }
        //                break;
        //            }
        //        }
        //        #endregion
        //    }
        //    return (savedProfileOwnerCCList,creditCardAddresses,mpPhone,mpEmail);
        //    #endregion
        //}

        private async Task SaveProfilePersistForAwardCancelPath(ProfileFOPCreditCardResponse profilePersist, string transactionPath, string deviceId, string mpNumber, string sessionId, string shoppingCartFlow = "")

        {
            if (_configuration.GetValue<bool>("VormetricTokenMigration"))
            {
                if (string.Equals("AWARDCANCELPATH", transactionPath, StringComparison.OrdinalIgnoreCase))
                {
                    await _sessionHelperService.SaveSession<ProfileFOPCreditCardResponse>(profilePersist, sessionId, new List<string> { sessionId, new ProfileFOPCreditCardResponse().ObjectName }, new ProfileFOPCreditCardResponse().ObjectName).ConfigureAwait(false);
                }
                else if ((shoppingCartFlow == FlowType.BOOKING.ToString() || shoppingCartFlow == FlowType.RESHOP.ToString()) && _configuration.GetValue<bool>("IsBookingCommonFOPEnabled"))
                {
                    Model.Common.ProfileResponse profilePersist1 = new Model.Common.ProfileResponse();
                    profilePersist1 = ObjectToObjectCasting<Model.Common.ProfileResponse, ProfileFOPCreditCardResponse>(profilePersist);
                    await _sessionHelperService.SaveSession<Model.Common.ProfileResponse>(profilePersist1, sessionId, new List<string> { sessionId, new Model.Common.ProfileResponse().ObjectName }, new Model.Common.ProfileResponse().ObjectName).ConfigureAwait(false);
                }
                else
                {
                    await _sessionHelperService.SaveSession<ProfileFOPCreditCardResponse>(profilePersist, sessionId, new List<string> { sessionId, new ProfileFOPCreditCardResponse().ObjectName }, new ProfileFOPCreditCardResponse().ObjectName).ConfigureAwait(false);
                }
            }
        }

        private T ObjectToObjectCasting<T, R>(R request)
        {
            var typeInstance = Activator.CreateInstance(typeof(T));

            foreach (var propReq in request.GetType().GetProperties())
            {
                var propRes = typeInstance.GetType().GetProperty(propReq.Name);
                if (propRes != null)
                {
                    propRes.SetValue(typeInstance, propReq.GetValue(request));
                }
            }

            return (T)typeInstance;
        }

        private bool IsPostBookingPromoCodeEnabled(int applicationId, string appVersion)
        {
            if (_configuration.GetValue<bool>("EnableCouponsInPostBooking") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnablePromoCodePostBooking_AppVersion"), _configuration.GetValue<string>("iPhone_EnablePromoCodePostBooking_AppVersion")))
            {
                return true;
            }
            return false;
        }

        private async Task<MOBShoppingCart> AssignDefaultFopDetails(string sessionId, List<MOBCPProfile> profiles, Session session, MOBCustomerProfileRequest request)
        {
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(sessionId, shoppingCart.ObjectName, new List<string> { sessionId, shoppingCart.ObjectName }).ConfigureAwait(false);
            if (shoppingCart == null)
            {
                shoppingCart = new MOBShoppingCart();
            }
            if (shoppingCart.FormofPaymentDetails == null)
            {
                shoppingCart.FormofPaymentDetails = new MOBFormofPaymentDetails();
            }
            AssignDefaultCreditCardandBillingAddress(shoppingCart, profiles);

            var owner = profiles[0].Travelers?.Find(t => t.IsProfileOwner);
            if (shoppingCart.FormofPaymentDetails?.Phone == null && owner?.ReservationPhones?.Count > 0)
            {
                shoppingCart.FormofPaymentDetails.Phone = owner.ReservationPhones[0];
            }

            if (shoppingCart.FormofPaymentDetails?.Email == null && owner?.ReservationEmailAddresses?.Count > 0)
            {
                shoppingCart.FormofPaymentDetails.EmailAddress = owner.ReservationEmailAddresses[0].EmailAddress;
                shoppingCart.FormofPaymentDetails.Email = owner.ReservationEmailAddresses[0];
            }

            if (session.CatalogItems != null && session.CatalogItems.Count > 0 && _shoppingUtility.IsEnablePartnerProvision(session?.CatalogItems, request.Flow, request.Application.Id, request.Application.Version.Major) && !string.IsNullOrEmpty(request.MileagePlusNumber))
            {
                shoppingCart.PartnerProvisionDetails = await GetPartnerProvisionDetails(request.MileagePlusNumber, session.Token);
            }

            await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, sessionId, new List<string> { sessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName).ConfigureAwait(false);

            return shoppingCart;
        }

        private void AssignDefaultCreditCardandBillingAddress(MOBShoppingCart shoppingCart, List<MOBCPProfile> profiles)
        {
            if (profiles != null && profiles[0].Travelers != null &&
                            profiles[0].Travelers.Any() &&
                            profiles[0].Travelers[0] != null &&
                            profiles[0].Travelers[0].CreditCards != null &&
                            profiles[0].Travelers[0].Addresses != null &&
                             profiles[0].Travelers[0].CreditCards.Count > 0 &&
                              profiles[0].Travelers[0].Addresses.Count > 0 &&
                            shoppingCart.FormofPaymentDetails?.CreditCard == null &&
                            shoppingCart.FormofPaymentDetails?.BillingAddress == null)
            {
                shoppingCart.FormofPaymentDetails.CreditCard = profiles[0].Travelers[0].CreditCards[0];
                shoppingCart.FormofPaymentDetails.BillingAddress = profiles[0].Travelers[0].Addresses[0];
                shoppingCart.FormofPaymentDetails.FormOfPaymentType = MOBFormofPayment.CreditCard.ToString();

            }
        }

        private async Task<List<MOBFOPCertificate>> GetProfileCertificates(string mileagePlusNumber, MOBApplication application, string sessionId, string transactionId, string deviceId)
        {
            if (_travelerUtility.IncludeTravelCredit(application.Id, application.Version.Major))
            {
                return null;
            }
            Model.Internal.AccountManagement.MileagePlus mileageplus = new Model.Internal.AccountManagement.MileagePlus();
            var etcReturnType = await _mileagePlus.GetTravelCertificateResponseFromETC(mileagePlusNumber, application, sessionId, transactionId, deviceId);
            List<MOBFOPCertificate> certificates = null;
            if (etcReturnType != null && etcReturnType.WSException != null &&
                !string.IsNullOrEmpty(etcReturnType.WSException.Code) &&
                !string.IsNullOrEmpty(etcReturnType.WSException.Message) &&
                etcReturnType.WSException.Code.Equals("E0000") &&
                etcReturnType.WSException.Message.Equals("Success") &&
                etcReturnType.PINDetails != null && etcReturnType.PINDetails.Any())
            {
                certificates = new List<MOBFOPCertificate>();
                #region 
                int index = 1;

                foreach (PINDetail pinDetail in etcReturnType.PINDetails)
                {
                    certificates.Add(GetMOBFOPCertificate(pinDetail, index));
                    index++;
                }
                #endregion
            }
            return certificates;
        }

        private MOBFOPCertificate GetMOBFOPCertificate(PINDetail pinDetail, int index)
        {
            MOBFOPCertificate certificate = null;
            if (pinDetail != null)
            {
                certificate = new MOBFOPCertificate();
                certificate.PinCode = pinDetail.CertPin;
                certificate.YearIssued = Convert.ToDateTime(pinDetail.OrigIssueDate).Year.ToString();
                TextInfo myTI = new CultureInfo("en-US", false).TextInfo;
                certificate.RecipientsLastName = myTI.ToTitleCase(pinDetail.LastName.ToLower());
                certificate.RecipientsFirstName = myTI.ToTitleCase(pinDetail.FirstName.ToLower());
                certificate.ExpiryDate = Convert.ToDateTime(pinDetail.CertExpDate).ToString("MMMM dd, yyyy");
                certificate.IsForAllTravelers = false;
                certificate.InitialValue = Convert.ToDouble(pinDetail.InitialValue);
                certificate.CurrentValue = Convert.ToDouble(pinDetail.CurrentValue);
                ShopStaticUtility.AssignCertificateRedeemAmount(certificate, 0);
                certificate.Index = index;
                certificate.IsProfileCertificate = true;
                certificate.CertificateTraveler = new MOBFOPCertificateTraveler();
            }
            return certificate;
        }

        private List<TravelSpecialNeed> GetSpecialNeedsFromTravelerAirPrefernces(MOBPrefAirPreference airPref)
        {
            if (airPref == null)
                return null;

            var results = new List<TravelSpecialNeed>();
            // meal preference
            if (!string.IsNullOrWhiteSpace(airPref.MealCode) && airPref.MealId != 1) // has meal reference
            {
                results.Add(new TravelSpecialNeed
                {
                    Code = airPref.MealCode,
                    DisplayDescription = airPref.MealDescription,
                    RegisterServiceDescription = airPref.MealDescription,
                    Value = airPref.MealId.ToString(),
                    Type = TravelSpecialNeedType.SpecialMeal.ToString()

                });
            }

            if (airPref.SpecialRequests != null && airPref.SpecialRequests.Any())
            {
                var wheelChairCodes = new HashSet<string> { "WCMP", "WCLB", "WCBW", "WCBD" };
                var specialRequests = new List<TravelSpecialNeed>();
                airPref.SpecialRequests.Where(x => !string.IsNullOrWhiteSpace(x.SpecialRequestCode) && !x.SpecialRequestCode.ToUpper().Equals("OTHS"))
                                        .ToList()
                                        .ForEach(x =>
                                        {
                                            TravelSpecialNeed mainItem = null;
                                            var subItem = new TravelSpecialNeed
                                            {
                                                Code = x.SpecialRequestCode,
                                                DisplayDescription = x.Description,
                                                RegisterServiceDescription = x.Description,
                                                Value = x.SpecialRequestId.ToString(),
                                                Type = TravelSpecialNeedType.SpecialRequest.ToString()
                                            };

                                            if (wheelChairCodes.Contains(subItem.Code.ToUpper()))
                                            {
                                                mainItem = new TravelSpecialNeed
                                                {
                                                    Code = _configuration.GetValue<string>("SSRWheelChairDescription"),
                                                    DisplayDescription = _configuration.GetValue<string>("SSRWheelChairDescription"),
                                                    RegisterServiceDescription = _configuration.GetValue<string>("SSRWheelChairDescription"),
                                                    Type = TravelSpecialNeedType.SpecialRequest.ToString(),
                                                    SubOptions = new List<TravelSpecialNeed> { subItem }
                                                };
                                            }
                                            specialRequests.Add(mainItem ?? subItem);
                                        });

                if (specialRequests != null && specialRequests.Any())
                    results.AddRange(specialRequests);
            }

            if (airPref.ServiceAnimals != null && airPref.ServiceAnimals.Any()) // has meal reference
            {
                var serviceAnimals = airPref.ServiceAnimals.Select(x => new TravelSpecialNeed
                {
                    DisplayDescription = x.ServiceAnimalTypeIdDesc,
                    Value = x.ServiceAnimalTypeId.ToString(),
                    SubOptions = x.ServiceAnimalId > 0 ? new List<TravelSpecialNeed> { new TravelSpecialNeed
                                                                                        {
                                                                                            DisplayDescription = x.ServiceAnimalIdDesc,
                                                                                            RegisterServiceDescription = x.ServiceAnimalIdDesc,
                                                                                            Value = x.ServiceAnimalId.ToString(),
                                                                                            Type = TravelSpecialNeedType.ServiceAnimal.ToString()
                                                                                        } }
                                                        : null,
                    Type = TravelSpecialNeedType.ServiceAnimalType.ToString()
                });

                if (serviceAnimals != null && serviceAnimals.Any())
                    results.AddRange(serviceAnimals);
            }

            return results;
        }
        private bool IsArrangerNavigation(MOBCPProfileRequest request, Reservation bookingPathReservation)
        {
            return GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("Android_ArrangerAutoRegister_Version"), _configuration.GetValue<string>("iOS_ArrangerAutoRegister_Version"))
             && _configuration.GetValue<bool>("EnableArrangerAutoRegisterOff") && (_travelerUtility.IsEnableNavigation(bookingPathReservation.IsReshopChange))
             && bookingPathReservation.ShopReservationInfo2.IsArrangerBooking;
        }
        private bool validateBasicInfo(List<MOBCPProfile> profile)
        {
            if (profile != null && profile.Count > 0 && profile[0].Travelers != null && profile[0].Travelers.Count > 0)
            {
                var traveler = profile[0].Travelers.Where(t => t.IsProfileOwner).First();
                return (!string.IsNullOrEmpty(traveler.BirthDate) && !string.IsNullOrEmpty(traveler.FirstName) && !string.IsNullOrEmpty(traveler.LastName) && !string.IsNullOrEmpty(traveler.GenderCode));
            }

            return true;
        }
        public async Task<MOBSHOPReservation> AutoRegisterTravelerWithProfileOwner_CFOP(MOBCPProfileRequest profileRequest)
        {
            MOBSHOPReservation reservation = new MOBSHOPReservation(_configuration, _cachingService);
            ProfileResponse profilePersist = new ProfileResponse();
            profilePersist = await GetCSLProfileResponseInSession(profileRequest.SessionId);
            if (profilePersist.Response != null && profilePersist.Response.Profiles != null)
            {
                #region
                MOBRegisterTravelersRequest request = new MOBRegisterTravelersRequest();
                request.Travelers = new List<MOBCPTraveler>();
                foreach (MOBCPTraveler mobCPTraveler in profilePersist.Response.Profiles[0].Travelers)
                {
                    if (mobCPTraveler.IsProfileOwner)
                    {
                        mobCPTraveler.PaxID = 1;
                        mobCPTraveler.IsPaxSelected = true;
                        request.Travelers.Add(mobCPTraveler);
                        request.ProfileOwner = mobCPTraveler;
                        break;
                    }
                }
                request.CartId = profileRequest.CartId;
                request.Flow = profileRequest.Flow;
                request.Token = profileRequest.Token;
                request.SessionId = profileRequest.SessionId;
                request.Application = profileRequest.Application;
                request.AccessCode = profileRequest.AccessCode;
                request.DeviceId = profileRequest.DeviceId;
                request.LanguageCode = profileRequest.LanguageCode;
                request.MileagePlusNumber = profileRequest.MileagePlusNumber;
                request.SessionId = profileRequest.SessionId;
                request.TransactionId = profileRequest.TransactionId;
                request.ProfileId = profilePersist.Response.Profiles[0].ProfileId;
                request.ProfileKey = profilePersist.Response.Profiles[0].ProfileOwnerKey;
                request.ProfileOwnerId = profilePersist.Response.Profiles[0].ProfileOwnerId;
                request.ProfileOwnerKey = profilePersist.Response.Profiles[0].ProfileOwnerKey;
                reservation = await _traveler.RegisterTravelers_CFOP(request);
                if (!_configuration.GetValue<bool>("EnableEplusCodeRefactor"))
                {
                    _shoppingUtility.UpdatePricesForEFS(reservation, request.Application.Id, request.Application.Version.Major, reservation.IsReshopChange);
                }
                #endregion
            }
            return reservation;
        }
        private async Task FireForgetForTCAndTBFOP(MOBCPProfileRequest request, MOBShoppingCart shoppingCart, MOBSHOPReservation reservation, Session session, bool isCorporateBusinessNamePersonalized = false)
        {
            await Task.Factory.StartNew(async () =>
             {
                 if(await _shoppingUtility.IsLoadTCOrTB(shoppingCart).ConfigureAwait(false))
                 {
                     if (request.Flow == FlowType.BOOKING.ToString() && !session.IsAward && (_memberProfileUtility.IsEnableFeature("EnableU4BCorporateBookingFFC", request.Application.Id, request.Application.Version.Major) ? true : !session.IsCorporateBooking))
                     {
                         var preLoadTravelCredits = new PreLoadTravelCredits(_logger, _configuration, _sessionHelperService, _travelerUtility, _paymentService, _fFCShoppingcs, _featureSettings);
                         await preLoadTravelCredits.PreLoadTravelCredit(session.SessionId, shoppingCart, request, true, null, isCorporateBusinessNamePersonalized);
                     }
                     var travelBank = new Common.Helper.Traveler.TravelBank(_configuration, _sessionHelperService, _travelerUtility, _fFCShoppingcs, _shoppingUtility, _cachingService, _dpService, _pKDispenserService, _headers);

                     shoppingCart.FormofPaymentDetails.TravelBankDetails = await travelBank.PopulateTravelBankData(session, reservation, request);
                     await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, session.SessionId, new List<string> { session.SessionId, shoppingCart.ObjectName + "FireAndForget" }, shoppingCart.ObjectName + "FireAndForget").ConfigureAwait(false);

                     _logger.LogInformation("GetProfile_CFOP Fire and forget triggered for TB and Preload credits");
                 }
             }
               
             );
        }

        private async Task<bool> IsTravelerNotExistInProfileResponse(MOBUpdateTravelerRequest request)
        {
            var travelrNotExist = true;
            try
            {
                var profileResponseForCheckNewTraveler = new ProfileResponse();
                profileResponseForCheckNewTraveler = await _sessionHelperService.GetSession<ProfileResponse>(request.SessionId, profileResponseForCheckNewTraveler.ObjectName, new List<string> { request.SessionId, profileResponseForCheckNewTraveler.ObjectName }).ConfigureAwait(false);
                travelrNotExist = (
                       _configuration.GetValue<bool>("BugFixToggleFor18B") &&
                       request.Traveler.PaxID > 0 &&
                       request.IsTravelSavedToProfile == true &&
                       profileResponseForCheckNewTraveler != null &&
                       profileResponseForCheckNewTraveler.Response != null &&
                       profileResponseForCheckNewTraveler.Response.Profiles != null &&
                       profileResponseForCheckNewTraveler.Response.Profiles.Count > 0 &&
                       profileResponseForCheckNewTraveler.Response.Profiles[0].Travelers != null &&
                       profileResponseForCheckNewTraveler.Response.Profiles[0].Travelers.Count > 0 &&
                       !profileResponseForCheckNewTraveler.Response.Profiles[0].Travelers.Exists(t => t.Key == request.Traveler.Key));
            }
            catch { }
            return travelrNotExist;
        }

        private async Task<MOBSHOPReservation> GetReservationFromPersist(string sessionID, string updatedCCKey, MOBCPTraveler traveler)
        {
            MOBSHOPReservation reservationFromPersist = new MOBSHOPReservation(_configuration, _cachingService);
            #region Update Booking Path Persist Reservation IsSignedInWithMP value and Save to session
            Reservation bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionID, bookingPathReservation.ObjectName, new List<string> { sessionID, bookingPathReservation.ObjectName }).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(updatedCCKey) && traveler != null)
            {
                #region
                List<MOBAddress> address = new List<MOBAddress>();
                MOBCPPhone mpPhone = new MOBCPPhone();
                MOBEmail mpEmail = new MOBEmail();
                var tupleRes = await _traveler.GetProfileOwnerCreditCardList(sessionID, address, mpPhone, mpEmail, updatedCCKey);
                bookingPathReservation.CreditCards = tupleRes.savedProfileOwnerCCList;
                mpEmail = tupleRes.mpEmail;
                mpPhone = tupleRes.mpPhone;
                bookingPathReservation.CreditCardsAddress = address;
                bookingPathReservation.ReservationPhone = traveler.Phones[0]; // As per requirment from the payment page update only CC do not update phoen and email but the update phone and email should be saved to persist reservation.
                bookingPathReservation.ReservationEmail = traveler.EmailAddresses[0];
                bookingPathReservation.FormOfPaymentType = MOBFormofPayment.CreditCard;
                await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                #endregion
            }
            if (bookingPathReservation != null)
            {
                reservationFromPersist = await _shoppingUtility.GetReservationFromPersist(reservationFromPersist, sessionID).ConfigureAwait(false);
            }
            return reservationFromPersist;
            #endregion
        }
        private void UpdateSpecialNeedsInProfile(List<MOBCPProfile> profiles, MOBCPTraveler traveler)
        {
            if (profiles != null && traveler != null)
            {
                foreach (var profile in profiles.Where(p => p.Travelers != null && p.Travelers.Any()))
                {
                    profile.Travelers.Where(t => t != null && t.Key == traveler.Key).ToList().ForEach(p =>
                    {
                        p.SelectedSpecialNeeds = traveler.SelectedSpecialNeeds;

                        // Special case found out when testing Cuba travel scenario. We want to persist
                        p.SelectedSpecialNeedMessages = traveler.SelectedSpecialNeedMessages = PersistSelectedSpecialNeedMessages(traveler);
                    });
                }
            }
        }

        private List<MOBItem> PersistSelectedSpecialNeedMessages(MOBCPTraveler traveler)
        {
            if (traveler.SelectedSpecialNeedMessages == null || !traveler.SelectedSpecialNeedMessages.Any())
                return traveler.SelectedSpecialNeedMessages;

            if (traveler.SelectedSpecialNeeds == null || !traveler.SelectedSpecialNeeds.Any())
                return traveler.SelectedSpecialNeedMessages;

            return null;
        }

        private async Task AssignSelectedTraveletToAllEligibleTravelerCsl(InsertTravelerRequest request, MOBCPProfileResponse response, string sessionId, bool isExtraSeatEnabled)
        {

            var bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionId, bookingPathReservation.ObjectName, new List<string> { sessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

            //adding pax
            if (request.Traveler != null)
            {
                /// 228346 : mAPP: Booking-Sponser vs Traveler: MP status wrongly displayed in seatmap for login with General member and selected/provided travler as 1K MP member
                /// Srini - 12/04/2017
                /// Calling getprofile for each traveler to get elite level for a traveler, who hav mp#
                int paxIdInRequestTravelerBeforeAsignTraveler = 0;
                if (
                   request != null &&
                   request.Traveler != null &&
                   !string.IsNullOrEmpty(request.Traveler.Key) &&
                   response != null && response.Profiles != null &&
                   response.Profiles.Count > 0 &&
                   response.Profiles[0].Travelers != null &&
                   response.Profiles[0].Travelers.Exists(t => t.Key == request.Traveler.Key))
                {
                    paxIdInRequestTravelerBeforeAsignTraveler = request.Traveler.PaxID;
                    request.Traveler = response.Profiles[0].Travelers.FirstOrDefault(t => t.Key == request.Traveler.Key);
                }

                if (_shoppingUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
               {
                    if (response.Reservation != null && response.Reservation.ShopReservationInfo2 != null && response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL != null)
                    {
                        bool travelerKey = isExtraSeatEnabled && request.Traveler.IsExtraSeat && string.IsNullOrEmpty(request.Traveler.Key);
                        var insertedTraveler = response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL.FirstOrDefault(t => t.Key == (travelerKey ? null : request.Traveler.Key) || t.PaxID == paxIdInRequestTravelerBeforeAsignTraveler);
                        if (insertedTraveler != null)
                        {
                            if (isExtraSeatEnabled && insertedTraveler.IsExtraSeat)
                            {
                                if (request.Traveler.IsExtraSeat)
                                {
                                    insertedTraveler.SelectedSpecialNeeds = request.Traveler.SelectedSpecialNeeds;
                                    insertedTraveler.SelectedSpecialNeedMessages = request.Traveler.SelectedSpecialNeedMessages;
                                }
                            }
                            else
                            {
                                insertedTraveler.SelectedSpecialNeeds = request.Traveler.SelectedSpecialNeeds;
                                insertedTraveler.SelectedSpecialNeedMessages = request.Traveler.SelectedSpecialNeedMessages;
                            }
                        }
                    }
                }
                var checkIfPaxIDExist = response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL.FirstOrDefault(t => t.PaxID == paxIdInRequestTravelerBeforeAsignTraveler);
                if (paxIdInRequestTravelerBeforeAsignTraveler == 0)
                {
                    request.Traveler.PaxID = response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count + 1;
                    request.Traveler.PaxIndex = request.Traveler.PaxID - 1;
                    request.AlreadySelectedPAXIDs.Add(request.Traveler.PaxID);
                    response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL.Add(request.Traveler);
                }
                else if (checkIfPaxIDExist != null)
                {
                    response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL.RemoveAll(t => t.PaxID == paxIdInRequestTravelerBeforeAsignTraveler);
                    request.Traveler.PaxID = paxIdInRequestTravelerBeforeAsignTraveler;
                    request.Traveler.PaxIndex = request.Traveler.PaxID - 1;
                    response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL.Add(request.Traveler);
                }
                response.Reservation.ShopReservationInfo2.NextViewName = "TRAVELERADDED";

                if(isExtraSeatEnabled)
                {
                    foreach (var traveler in response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL)
                    {
                        if (traveler != null)
                        {
                            traveler.IsEligibleForExtraSeatSelection = _travelerUtility.IsExtraSeatEligibleForInfantOnLapAndInfantOnSeat(traveler.TravelerTypeCode, traveler.IsExtraSeat);

                            if (!request.Traveler.IsExtraSeat && checkIfPaxIDExist != null && traveler.IsExtraSeat && traveler.ExtraSeatData?.SelectedPaxId == request.Traveler.PaxID)
                            {
                                traveler.LastName = _travelerUtility.GetTravelerDisplayNameForExtraSeat(request.Traveler.FirstName, request.Traveler.MiddleName, request.Traveler.LastName, request.Traveler.Suffix);
                                traveler.MiddleName = request.Traveler.MiddleName;
                                traveler.Suffix = request.Traveler.Suffix;
                                traveler.BirthDate = request.Traveler.BirthDate;
                                traveler.GenderCode = request.Traveler.GenderCode;
                                traveler.TravelerTypeCode = request.Traveler.TravelerTypeCode;
                                traveler.TravelerTypeDescription = request.Traveler.TravelerTypeDescription;
                                traveler.Nationality = request.Traveler.Nationality;
                                traveler.CountryOfResidence = request.Traveler.CountryOfResidence;
                                traveler.CubaTravelReason = request.Traveler.CubaTravelReason;
                            }
                        }
                    }
                }
            }
            bookingPathReservation.ShopReservationInfo2 = response.Reservation.ShopReservationInfo2;
            await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
            response.Reservation.TravelersCSL = new List<MOBCPTraveler>();
            foreach (var traveler in response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL)
            {
                traveler.IsPaxSelected = request.AlreadySelectedPAXIDs != null && request.AlreadySelectedPAXIDs.Exists(id => id == traveler.PaxID);
                if (traveler.IsPaxSelected)
                {
                    response.Reservation.TravelersCSL.Add(traveler);
                }
            }
        }
        private async Task UpdateTraveletToAllEligibleTravelerCsl(MOBUpdateTravelerRequest request, MOBCPTraveler mobCPTraveler, MOBCPProfileResponse response, Session session)
        {
            var bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(session.SessionId, bookingPathReservation.ObjectName, new List<string> { session.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

            if (response.Reservation != null && request.Traveler != null)
            {
                if (response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL != null && response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count > 0)
                {
                    var eligibleTravelersCSL = response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL.FirstOrDefault(p => p != null && p.PaxID == request.Traveler.PaxID);
                    if (eligibleTravelersCSL != null)
                    {
                        var locationInCollection = response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL.IndexOf(eligibleTravelersCSL);
                        if (eligibleTravelersCSL.MileagePlus != null && mobCPTraveler.MileagePlus == null)
                        {
                            mobCPTraveler.MileagePlus = eligibleTravelersCSL.MileagePlus;
                        }

                        if (eligibleTravelersCSL.AirRewardPrograms != null && mobCPTraveler.AirRewardPrograms == null)
                        {
                            mobCPTraveler.AirRewardPrograms = eligibleTravelersCSL.AirRewardPrograms;
                        }

                        if (_configuration.GetValue<bool>("EnableNationalityAndCountryOfResidence"))
                        {
                            if (response.Reservation.ShopReservationInfo2.InfoNationalityAndResidence != null && response.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence)
                            {
                                if (!string.IsNullOrEmpty(request.Traveler.Nationality) && !string.IsNullOrEmpty(request.Traveler.CountryOfResidence) && !string.IsNullOrEmpty(request.Traveler.FirstName)
                                    && !string.IsNullOrEmpty(request.Traveler.LastName) && !string.IsNullOrEmpty(request.Traveler.GenderCode) && !string.IsNullOrEmpty(request.Traveler.BirthDate))
                                {
                                    if (!string.IsNullOrEmpty(request.Traveler.Message))
                                    {
                                        request.Traveler.Message = null;
                                    }
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(request.Traveler.FirstName) && !string.IsNullOrEmpty(request.Traveler.LastName) && !string.IsNullOrEmpty(request.Traveler.GenderCode) && !string.IsNullOrEmpty(request.Traveler.BirthDate))
                                {
                                    if (!string.IsNullOrEmpty(request.Traveler.Message))
                                    {
                                        request.Traveler.Message = null;
                                    }
                                }
                            }
                        }

                        /// 216108 - Bug Force Eplus – “The email address entered already exists in your saved email address” error is displayed , 
                        /// when edited the MP saved traveler information with no changes after adding Phone & email.
                        ///--Srini
                        if (_configuration.GetValue<bool>("BugFixToggleFor17M") &&
                           response.Profiles != null &&
                           response.Profiles.Count() > 0 &&
                           response.Profiles[0].Travelers != null &&
                           response.Profiles[0].Travelers.Exists(p => p.Key == mobCPTraveler.Key))
                        {
                            var profileTraveler = response.Profiles[0].Travelers.FirstOrDefault(p => p.Key == mobCPTraveler.Key);
                            mobCPTraveler.EmailAddresses = profileTraveler.EmailAddresses;
                            mobCPTraveler.Phones = profileTraveler.Phones;
                            mobCPTraveler.ReservationPhones = profileTraveler.ReservationPhones;

                            if (_travelerUtility.EnableTravelerTypes(request.Application.Id, request.Application.Version.Major, bookingPathReservation.IsReshopChange) &&
                                bookingPathReservation.ShopReservationInfo2.TravelerTypes != null && bookingPathReservation.ShopReservationInfo2.TravelerTypes.Count > 0)
                            {
                                if (!string.IsNullOrEmpty(request.Traveler.BirthDate))
                                {
                                    if (_shoppingUtility.EnableYADesc(bookingPathReservation.IsReshopChange) && bookingPathReservation.ShopReservationInfo2.IsYATravel)
                                    {
                                        mobCPTraveler.PTCDescription = _mPTraveler.GetYAPaxDescByDOB();
                                    }
                                    else
                                    {
                                        mobCPTraveler.PTCDescription = _travelerUtility.GetPaxDescriptionByDOB(request.Traveler.BirthDate.ToString(), bookingPathReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTime);
                                    }
                                    mobCPTraveler.TravelerTypeDescription = profileTraveler.PTCDescription;
                                }
                                //mobCPTraveler.TravelerTypeCode = Utility.GetTypeCodeByAge(Utility.GetAgeByDOB(request.Traveler.BirthDate.ToString(), bookingPathReservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTime));
                                mobCPTraveler.TravelerTypeCode = profileTraveler.TravelerTypeCode;
                            }

                            #region Special Needs

                            if (_shoppingUtility.EnableSpecialNeeds(request.Application.Id, request.Application.Version.Major))
                            {
                                if (response.Reservation != null && response.Reservation.ShopReservationInfo2 != null && response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL != null)
                                {
                                    var updateTraveler = response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL.FirstOrDefault(t => t.Key == request.Traveler.Key || t.PaxID == request.Traveler.PaxID);
                                    if (updateTraveler != null)
                                    {
                                        updateTraveler.SelectedSpecialNeeds = request.Traveler.SelectedSpecialNeeds;
                                        updateTraveler.SelectedSpecialNeedMessages = request.Traveler.SelectedSpecialNeedMessages;
                                    }
                                }
                            }

                            #endregion
                        }

                        mobCPTraveler.IsPaxSelected = eligibleTravelersCSL.IsPaxSelected;
                        response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL[locationInCollection] = mobCPTraveler;
                        _memberProfileUtility.ValidateTravelersForCubaReason(response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL, response.Reservation.IsCubaTravel);
                    }
                }
            }

            if (_travelerUtility.IsExtraSeatFeatureEnabled(request.Application.Id, request.Application.Version.Major, session.CatalogItems))
            {
                foreach (var traveler in response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL)
                {
                    if (traveler != null)
                    {
                        traveler.IsEligibleForExtraSeatSelection = _travelerUtility.IsExtraSeatEligibleForInfantOnLapAndInfantOnSeat(traveler.TravelerTypeCode, traveler.IsExtraSeat);

                        if (!mobCPTraveler.IsExtraSeat && traveler.IsExtraSeat && traveler.ExtraSeatData?.SelectedPaxId == mobCPTraveler.PaxID)
                        {
                            traveler.LastName = _travelerUtility.GetTravelerDisplayNameForExtraSeat(mobCPTraveler.FirstName, mobCPTraveler.MiddleName, mobCPTraveler.LastName, mobCPTraveler.Suffix);
                            traveler.MiddleName = mobCPTraveler.MiddleName;
                            traveler.Suffix = mobCPTraveler.Suffix;
                            traveler.BirthDate = mobCPTraveler.BirthDate;
                            traveler.GenderCode = mobCPTraveler.GenderCode;
                            traveler.TravelerTypeCode = mobCPTraveler.TravelerTypeCode;
                            traveler.TravelerTypeDescription = mobCPTraveler.TravelerTypeDescription;
                            traveler.Nationality = mobCPTraveler.Nationality;
                            traveler.CountryOfResidence = mobCPTraveler.CountryOfResidence;
                            traveler.CubaTravelReason = mobCPTraveler.CubaTravelReason;
                        }
                    }
                }
            }

            bookingPathReservation.ShopReservationInfo2 = response.Reservation.ShopReservationInfo2;
            await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
            response.Reservation.TravelersCSL = new List<MOBCPTraveler>();
            foreach (var traveler in response.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL)
            {
                traveler.IsPaxSelected = request.AlreadySelectedPAXIDs != null && request.AlreadySelectedPAXIDs.Exists(id => id == traveler.PaxID);

                if (traveler.IsPaxSelected)
                {
                    if (_shoppingUtility.IsETCEnabledforMultiTraveler(request.Application.Id, request.Application.Version.Major))
                    {
                        if (!_configuration.GetValue<bool>("DisableBugMOBILE8963Toggle"))
                        {
                            if (bookingPathReservation != null && bookingPathReservation.TravelersCSL != null && bookingPathReservation.TravelersCSL.Count > 0 && bookingPathReservation.TravelersCSL.Any(t => t.Value.PaxID == traveler.PaxID))
                            {
                                traveler.IndividualTotalAmount = bookingPathReservation.TravelersCSL.Where(t => t.Value.PaxID == traveler.PaxID).FirstOrDefault().Value.IndividualTotalAmount;
                                traveler.TravelerNameIndex = bookingPathReservation.TravelersCSL.Where(t => t.Value.PaxID == traveler.PaxID).FirstOrDefault().Value.TravelerNameIndex;
                            }
                        }
                        else
                        {
                            if (bookingPathReservation != null && bookingPathReservation.TravelersCSL != null && bookingPathReservation.TravelersCSL.Count > 0)
                            {
                                traveler.IndividualTotalAmount = bookingPathReservation.TravelersCSL.Where(t => t.Value.PaxID == traveler.PaxID).FirstOrDefault().Value.IndividualTotalAmount;
                                traveler.TravelerNameIndex = bookingPathReservation.TravelersCSL.Where(t => t.Value.PaxID == traveler.PaxID).FirstOrDefault().Value.TravelerNameIndex;
                            }
                        }

                    }
                    response.Reservation.TravelersCSL.Add(traveler);
                }
            }
        }

        public async Task<MOBCPTraveler> RegisterNewTravelerValidateMPMisMatch(MOBCPTraveler registerTraveler, MOBRequest mobRequest, string sessionID, string cartId, string token)
        {

            //if (request.ValidateMPNameMissMatch && request.Travelers != null && request.Travelers.Count == 1 && request.Travelers[0].AirRewardPrograms != null && request.Travelers[0].AirRewardPrograms.Count > 0)
            #region This scenario is to validate Not Signed In Add Travelers or a Add New Traveler not saved to Profile -> Validate MP Traveler Name Miss Match per traveler.
            string mpNumbers = string.Empty;
            //if (registerTraveler.CustomerId == 0 && registerTraveler.AirRewardPrograms != null && registerTraveler.AirRewardPrograms[0].ProgramId == "7") //Program ID = 7 means United Mileage Plus Account
            if (registerTraveler.AirRewardPrograms != null && registerTraveler.AirRewardPrograms[0].ProgramId == "7") //Program ID = 7 means United Mileage Plus Account
            {
                #region Get Newly Added Traveler Not Saved to Profile MP Name Miss Match
                MOBCPProfileRequest savedTravelerProfileRequest = new MOBCPProfileRequest();
                #region
                savedTravelerProfileRequest.Application = mobRequest.Application;
                savedTravelerProfileRequest.DeviceId = mobRequest.DeviceId;
                savedTravelerProfileRequest.CartId = cartId;
                savedTravelerProfileRequest.AccessCode = mobRequest.AccessCode;
                savedTravelerProfileRequest.LanguageCode = mobRequest.LanguageCode;
                savedTravelerProfileRequest.ProfileOwnerOnly = true; // This is to call getprofile() to return only profile owner details to check the saved traveler FN, MN , LN, DOB and Gender to validate they match with saved traveler MP details and if not matched return a mismatch message to client if matched then get the Mileagplus details to get the elite level for LMX changes for client.
                savedTravelerProfileRequest.Token = token;
                savedTravelerProfileRequest.TransactionId = mobRequest.TransactionId;
                savedTravelerProfileRequest.IncludeAllTravelerData = false;
                savedTravelerProfileRequest.IncludeAddresses = false;
                savedTravelerProfileRequest.IncludeEmailAddresses = false;
                savedTravelerProfileRequest.IncludePhones = false;
                savedTravelerProfileRequest.IncludeCreditCards = false;
                savedTravelerProfileRequest.IncludeSubscriptions = false;
                savedTravelerProfileRequest.IncludeTravelMarkets = false;
                savedTravelerProfileRequest.IncludeCustomerProfitScore = false;
                savedTravelerProfileRequest.IncludePets = false;
                savedTravelerProfileRequest.IncludeCarPreferences = false;
                savedTravelerProfileRequest.IncludeDisplayPreferences = false;
                savedTravelerProfileRequest.IncludeHotelPreferences = false;
                savedTravelerProfileRequest.IncludeAirPreferences = false;
                savedTravelerProfileRequest.IncludeContacts = false;
                savedTravelerProfileRequest.IncludePassports = false;
                savedTravelerProfileRequest.IncludeSecureTravelers = false;
                savedTravelerProfileRequest.IncludeFlexEQM = false;
                savedTravelerProfileRequest.IncludeServiceAnimals = false;
                savedTravelerProfileRequest.IncludeSpecialRequests = false;
                savedTravelerProfileRequest.IncludePosCountyCode = false;
                #endregion
                savedTravelerProfileRequest.MileagePlusNumber = registerTraveler.AirRewardPrograms[0].MemberId;
                savedTravelerProfileRequest.SessionId = sessionID;
                List<MOBCPProfile> travelerProfileList = await _customerProfile.GetProfile(savedTravelerProfileRequest);
                if (travelerProfileList != null && travelerProfileList.Count > 0 && travelerProfileList[0].Travelers != null && travelerProfileList[0].Travelers.Count > 0 && registerTraveler.FirstName.ToUpper().Trim() == travelerProfileList[0].Travelers[0].FirstName.ToUpper().Trim() && registerTraveler.LastName.ToUpper().Trim() == travelerProfileList[0].Travelers[0].LastName.ToUpper().Trim())
                {
                    if (travelerProfileList[0].Travelers[0].MileagePlus != null)
                    {
                        registerTraveler.MileagePlus = travelerProfileList[0].Travelers[0].MileagePlus;
                    }
                }
                else
                {
                    mpNumbers = mpNumbers + "," + registerTraveler.AirRewardPrograms[0].MemberId;
                    registerTraveler.Message = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage");
                    registerTraveler.MPNameNotMatchMessage = _configuration.GetValue<string>("ValidateMPNameMisMatchErrorMessage");
                    registerTraveler.isMPNameMisMatch = true;
                }
                #endregion
            }
            //53606 - For Travel Program Other Than Mileage Plus Loyalty Information Field Accepts O and 1 Digit Numbers - Manoj
            else if (registerTraveler.AirRewardPrograms != null && registerTraveler.AirRewardPrograms[0].ProgramId != "7")
            {
                if (!string.IsNullOrEmpty(registerTraveler.AirRewardPrograms[0].MemberId.ToString()))
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(registerTraveler.AirRewardPrograms[0].MemberId.ToString(), @"^\d$"))
                    {
                        throw new MOBUnitedException(_configuration.GetValue<string>("ValidateLoyalityNumberErrorMessage"));
                    }
                }
            }
            #endregion
            return registerTraveler;
        }
        public string ReadResponseStream(System.IO.Stream stream)
        {
            try
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
        public async Task<List<RewardProgram>> GetRewardPrograms(int applicationId, string deviceId, string appVersion, string transactionId, string sessionID, string token)
        {
            #region
            var rewardPrograms = new List<RewardProgram>();

            var response = new Service.Presentation.ReferenceDataResponseModel.RewardProgramResponse();
            response.Programs = (await _referencedataService.RewardPrograms<Collection<Program>>(token, sessionID)).Response;

            if (response != null)
            {
                #region
                if (response != null && response.Programs.Count > 0)
                {
                    foreach (var reward in response.Programs)
                    {
                        if (reward.ProgramID != 5)
                        {
                            rewardPrograms.Add(new RewardProgram() { Description = reward.Description, ProgramID = reward.ProgramID.ToString(), Type = reward.Code.ToString() });
                        }
                    }
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        _logger.LogError("GetRewardPrograms - Response {@Error}", JsonConvert.SerializeObject(response.Errors));

                    }
                }
                #endregion
            }
            #endregion
            return rewardPrograms;
        }
        private async Task<MOBSHOPReservation> GetReservationFromPersistWithChaseCC(string sessionID, string updatedCCKey)
        {
            MOBSHOPReservation reservationFromPersist = new MOBSHOPReservation(_configuration, _cachingService);
            #region Update Booking Path Persist Reservation IsSignedInWithMP value and Save to session
            Reservation bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionID, bookingPathReservation.ObjectName, new List<string> { sessionID, bookingPathReservation.ObjectName }).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(updatedCCKey))
            {
                #region
                List<MOBAddress> address = new List<MOBAddress>();
                MOBCPPhone mpPhone = new MOBCPPhone();
                MOBEmail mpEmail = new MOBEmail();
                var tupleRes = await _traveler.GetProfileOwnerCreditCardList(sessionID, address, mpPhone, mpEmail, updatedCCKey);
                bookingPathReservation.CreditCards = tupleRes.savedProfileOwnerCCList;
                mpEmail = tupleRes.mpEmail;
                mpPhone = tupleRes.mpPhone;
                bookingPathReservation.ReservationPhone = mpPhone;
                bookingPathReservation.ReservationEmail = mpEmail;
                if (!_configuration.GetValue<bool>("DisableBillingAddressFixForFFCV2"))
                {
                    address = tupleRes.creditCardAddresses;
                }
                bookingPathReservation.CreditCardsAddress = address;
                bookingPathReservation.FormOfPaymentType = MOBFormofPayment.CreditCard;
                if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement != null)
                {
                    bookingPathReservation.ShopReservationInfo2.ChaseCreditStatement = null;
                }
                await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                #endregion
            }
            if (bookingPathReservation != null)
            {
                reservationFromPersist = await _shoppingUtility.GetReservationFromPersist(reservationFromPersist, sessionID).ConfigureAwait(false);
            }
            reservationFromPersist.UpdateRewards(_configuration, _cachingService);

            return reservationFromPersist;
            #endregion
        }

        public bool IsEnableFeature(string feature, int appId, string appVersion)
        {
            var enableFFC = _configuration.GetSection("EnableU4BCorporateBookingFFC").GetValue<bool>("isEnable");
            var android_AppVersion = _configuration.GetSection("EnableU4BCorporateBookingFFC").GetValue<string>("android_EnableU4BCorporateBookingFFC_AppVersion");
            var iPhone_AppVersion = _configuration.GetSection("EnableU4BCorporateBookingFFC").GetValue<string>("iPhone_EnableU4BCorporateBookingFFC_AppVersion");
            return _configuration.GetValue<bool>("EnableU4BCorporateBooking") && enableFFC && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, android_AppVersion, iPhone_AppVersion);
        }

        // Express checkout flow - Save bundles to see them in final RTI screen if needed.
        private async Task SaveBundles(MOBCPProfileRequest request)
        {
            try
            {

                BookingBundlesRequest requestBundles = BuildBundleOfferRequest(request);
                var bundles = _shopBundleService.GetBundleOffer(requestBundles);
            }
            catch (Exception ex)
            {
                _logger.LogError("SaveBundles - Prefetching Bundles Error {message} {exceptionStackTrace} and {transactionId}", ex.Message, ex.StackTrace, _headers.ContextValues.SessionId);
            }
        }

        // Creates the bundle offer request
        private BookingBundlesRequest BuildBundleOfferRequest(MOBCPProfileRequest request)
        {
            var bundleOfferRequest = new BookingBundlesRequest
            {
                Application = request.Application,
                SessionId = request.SessionId,
                CartId = request.CartId,
                DeviceId = request.DeviceId,
                Flow = FlowType.BOOKING.ToString(),
                TransactionId = request.TransactionId
            };
            return bundleOfferRequest;
        }

        private async Task UpdatesInExpressCheckoutFlow(Reservation bookingPathReservation, MOBCPProfileRequest request, United.Mobile.Model.Common.ProfileResponse profilePersist) 
        {
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);
            await _travelerUtility.SetNextViewNameForEliteCustomer(profilePersist, bookingPathReservation).ConfigureAwait(false);
            if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.IsExpressCheckoutPath)
            {
                // Save bundles to be used in the final RTI screen
                Task.Run(() => SaveBundles(request));
            }
        }

        public CorporateUnenrolledTravelerMsg corporateUnenrolledTravelerMsg(MOBUpdateTravelerRequest request , List<CMSContentMessage> list)
        {
            CMSContentMessage cMSContentMessage = list?.Where(x=>x.Title.Equals("CorporateUnenrolled.Traveler.Msg"))?.FirstOrDefault();
            CorporateUnenrolledTravelerMsg corporateUnenrolledTravelerMsg = new CorporateUnenrolledTravelerMsg();
            corporateUnenrolledTravelerMsg.Title = cMSContentMessage?.Headline;
            corporateUnenrolledTravelerMsg.Header = cMSContentMessage?.ContentShort; 
            corporateUnenrolledTravelerMsg.Body =string.Format(cMSContentMessage?.ContentFull,request?.Traveler?.FirstName.ToString()+" "+ request?.Traveler?.LastName.ToString());

            var msgActionText = cMSContentMessage?.CallToActionUrl1?.Split("|");
            var msgButtonText = cMSContentMessage?.CallToAction1?.Split("|");
            List<MOBButton> mOBButton = new List<MOBButton>
            {
               new MOBButton {
                     ActionText = msgActionText[0],
                     ButtonText = msgButtonText[0],
                     IsPrimary = true,
                     IsEnabled = true,
                     Rank = 1,
              },
                new MOBButton {
                 ActionText = msgActionText[1],
                 ButtonText = msgButtonText[1],
                 IsPrimary = false,
                 IsEnabled = true,
                 Rank = 2,
                }
            };
            corporateUnenrolledTravelerMsg.Buttons = new List<MOBButton>();
            corporateUnenrolledTravelerMsg.Buttons = mOBButton;
            return corporateUnenrolledTravelerMsg;
        }
        private async Task<Tuple<bool,bool>> CorpMpValidation(MOBUpdateTravelerRequest request, string token)
        {
            bool isArranger = false;
            bool isCorpMPVerified = true;
            bool isServiceFailed = false;
            int UCSID = 0;
            try
            {
                    string sessionId = request.DeviceId + request.MileagePlusNumber;
                    var _corprofileResponse = await _sessionHelperService.GetSession<United.CorporateDirect.Models.CustomerProfile.CorpProfileResponse>(sessionId, ObjectNames.CSLCorpProfileResponse, new List<string> { sessionId, ObjectNames.CSLCorpProfileResponse }).ConfigureAwait(false);
                    if (_corprofileResponse != null)
                    {
                        isArranger = _corprofileResponse?.Profiles.Any(a => a?.CorporateData?.IsArranger == true)??false;
                        if (isArranger)
                        {
                        return Tuple.Create(isCorpMPVerified, isServiceFailed);
                        }
                        else
                        {

                            UCSID = _corprofileResponse?.Profiles?.Select(a => a.CorporateData?.UCSID).FirstOrDefault() ?? 0;
                            var corpMpNumberValidationResponse = await _corpProfile.CorpMpNumberValidation(token, sessionId, request?.Traveler?.AirRewardPrograms?.Select(a => a.MemberId).ToList(), UCSID);
                            isCorpMPVerified = corpMpNumberValidationResponse?.Data?.Any(a => a?.IsVerified == true)??false;
                            isServiceFailed = corpMpNumberValidationResponse?.Errors?.Any()??false;
                        }
                    }
            }
            catch (Exception)
            {
                isServiceFailed = true;
            }
            return Tuple.Create(isCorpMPVerified, isServiceFailed);
        }
        private List<InfoWarningMessages> CorpMultiPaxInfoWarningMessages(List<CMSContentMessage> lstMessages, List<InfoWarningMessages> infoWarningMessages)
        {
            List<InfoWarningMessages> warningMessage = new List<InfoWarningMessages>();
            try
            {
                if (infoWarningMessages != null && infoWarningMessages.Count > 0)
                {
                    warningMessage = infoWarningMessages;
                }
                if (!warningMessage?.Any(a => a?.Order == MOBINFOWARNINGMESSAGEORDER.CORPORATEUNENROLLEDTRAVELER.ToString()) ?? false)
                {
                    InfoWarningMessages infoWarningMes = _corpProfile.CorpMultiPaxinfoWarningMessages(lstMessages);
                    warningMessage.Add(infoWarningMes);
                    warningMessage = warningMessage.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)System.Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                }
            }
            catch (Exception)
            {
            }
            return warningMessage;

        }
       
    }
}


