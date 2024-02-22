using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.ShopProducts;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.AccountManagement;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.Shopping.PriceBreakDown;
using United.Service.Presentation.PersonalizationModel;
using United.Service.Presentation.PersonalizationResponseModel;
using United.Utility.Enum;
using United.Utility.Helper;
using MileagePlus = United.Mobile.Model.Internal.AccountManagement.MileagePlus;
using ProfileFOPCreditCardResponse = United.Mobile.Model.Common.ProfileFOPCreditCardResponse;
using ProfileResponse = United.Mobile.Model.Common.ProfileResponse;

namespace United.Mobile.UpdateMemberProfile.Domain
{
    public class UpdateMemberProfileUtility: IUpdateMemberProfileUtility
    {
        private readonly IConfiguration _configuration;
        private readonly MileagePlusDynamoDB _mileagePlusDynamoDB;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly ICachingService _cachingService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly IOmniCart _omniCart;
        private readonly ICacheLog<UpdateMemberProfileUtility> _logger;
        private readonly IHeaders _headers;
        private readonly IShoppingCcePromoService _shoppingCcePromoService;

        public UpdateMemberProfileUtility(IConfiguration configuration
            , IDynamoDBService dynamoDBService
            , ISessionHelperService sessionHelperService
            , ICachingService cachingService
            , IShoppingUtility shoppingUtility
            , IFFCShoppingcs fFCShoppingcs
            , IOmniCart omniCart
            , ICacheLog<UpdateMemberProfileUtility> logger
            , IHeaders headers
            , IShoppingCcePromoService shoppingCcePromoService)
        {
            _configuration = configuration;
            _dynamoDBService = dynamoDBService;
            _sessionHelperService = sessionHelperService;
            _cachingService = cachingService;
            _shoppingUtility = shoppingUtility;
            _mileagePlusDynamoDB = new MileagePlusDynamoDB(_configuration, _dynamoDBService, _logger);
            _fFCShoppingcs = fFCShoppingcs;
            _omniCart = omniCart;
            _logger = logger;
            _headers = headers;
            _shoppingCcePromoService = shoppingCcePromoService;
        }

        public async Task<bool> SendChasePromoFeedbackToCCE(string mileagePlusNumber, string messageKey, Model.FeedBack.MOBPromoFeedbackEventType feedbackEventType, MOBRequest mobRequest, string sessionId, string logAction)
        {
            bool isFeedbackSuccess = false;

            //get cce data from Session
            CCEPromo ccePromoPersist = new CCEPromo();
            var ccePromoFromSession = await _sessionHelperService.GetSession<CCEPromo>(sessionId, ccePromoPersist.ObjectName, new List<string> { sessionId, ccePromoPersist.ObjectName }).ConfigureAwait(false);
            if (ccePromoFromSession == null)
            {
                throw new MOBUnitedException("Could not find the session.");
            }
            var cceResponseFromSession = JsonConvert.DeserializeObject<ContextualCommResponse>(ccePromoFromSession.ContextualCommResponseJson);

            if (cceResponseFromSession != null && cceResponseFromSession.Components.Count > 0
                && cceResponseFromSession.Components[0].ContextualElements.Count > 0
                && cceResponseFromSession.Components[0].ContextualElements[0].Value != null)
            {
                var cceValuesJson = JsonConvert.SerializeObject(cceResponseFromSession.Components[0].ContextualElements[0].Value);
                ContextualMessage cceValues = JsonConvert.DeserializeObject<ContextualMessage>(cceValuesJson);

                if (cceValues != null && ccePromoFromSession != null && ccePromoFromSession.ContextualCommRequest != null)
                {
                    //build request 
                    United.Service.Presentation.PersonalizationRequestModel.PersonalizationFeedbackRequest feedbackRequest = new Service.Presentation.PersonalizationRequestModel.PersonalizationFeedbackRequest();

                    feedbackRequest.LoyaltyProgramMemberID = mileagePlusNumber;
                    feedbackRequest.Requestor = new Service.Presentation.CommonModel.Requestor();
                    feedbackRequest.Requestor.ChannelName = _configuration.GetValue<string>("ChaseBannerCCERequestChannelName");

                    feedbackRequest.Events = new System.Collections.ObjectModel.Collection<FeedbackEvent>();
                    FeedbackEvent feedbackEvent = new FeedbackEvent();
                    feedbackEvent.ChoiceID = cceValues.MessageKey;
                    feedbackEvent.EventTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffK");
                    //feedbackEvent.EventTimeZone  // did not have info from CCE
                    feedbackEvent.Type = feedbackEventType.ToString();
                    feedbackEvent.Page = ccePromoFromSession.ContextualCommRequest.PageToLoad;
                    feedbackEvent.Placement = cceResponseFromSession.Components[0].Name;
                    feedbackEvent.ContextualElement = new ContextualElement();
                    var contextualElementValue = new ContextualMessage();
                    contextualElementValue.MessageKey = cceValues.MessageKey;
                    contextualElementValue.MessageType = cceResponseFromSession.Components[0].Name;
                    contextualElementValue.Params = new Dictionary<string, string>();

                    foreach (var param in cceValues.Params)
                    {
                        contextualElementValue.Params.Add(param.Key, param.Value);
                    }
                    feedbackEvent.ContextualElement.Type = cceResponseFromSession.Components[0].ContextualElements[0].Type;
                    feedbackEvent.ContextualElement.Rank = cceResponseFromSession.Components[0].ContextualElements[0].Rank;
                    feedbackEvent.ContextualElement.Value = contextualElementValue;
                    feedbackRequest.Events.Add(feedbackEvent);

                    //post feedback CCE
                    var jsonRequest = JsonConvert.SerializeObject(feedbackRequest);
                    var session = await _sessionHelperService.GetSession<Session>(sessionId, new Session().ObjectName, new List<string> { sessionId, new Session().ObjectName }).ConfigureAwait(false);
                    var jsonResponse = await _shoppingCcePromoService.ShoppingCcePromo(session.Token, jsonRequest, _headers.ContextValues.SessionId).ConfigureAwait(false);
                    var cceResponse = string.IsNullOrEmpty(jsonResponse) ? null
                        : JsonConvert.DeserializeObject<PersonalizationAcknowledgement>(jsonResponse);
                    //validate 
                    if (cceResponse != null && cceResponse.Response != null)
                    {
                        foreach (var message in cceResponse.Response.Message)
                        {
                            if (message.Status.Equals("Success", StringComparison.OrdinalIgnoreCase))
                            {
                                isFeedbackSuccess = true;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                throw new MOBUnitedException("Invalid Chase promo CCE response in the session");
            }

            return isFeedbackSuccess;
        }

        public async Task<bool> isValidDeviceRequest(string transactionId, string deviceId, int applicationId, string mpNumber,string sessionId)
        {
            bool validWalletRequest = true; // This flag will set to true either if the wallet call request is a valid one (checking the MP device table withthe device Id and MP passed in the request)
            if (!string.IsNullOrEmpty(mpNumber))
            {
                if (_configuration.GetValue<string>("ValidateWalletRequest") != null && _configuration.GetValue<bool>("ValidateWalletRequest"))
                {
                    if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(mpNumber) || !await VerifyMileagePlusWithDeviceAPPID(deviceId, applicationId, mpNumber,sessionId))
                    {
                        validWalletRequest = false;
                    }
                }
                else
                {
                    validWalletRequest = true; // here we set to true to have this work as existing production wiht out checking the MP DeviceId and MP Number validation
                }
            }

            return validWalletRequest;
        }

        private async Task<bool> VerifyMileagePlusWithDeviceAPPID(string deviceId, int applicationId, string mpNumber, string sessionId)
        {
            bool ok = false;
            string response = await _mileagePlusDynamoDB.VerifyMileagePlusWithDeviceAPPID(deviceId, applicationId, mpNumber, sessionId).ConfigureAwait(false);
            var ucMemberShipV2 = Newtonsoft.Json.JsonConvert.DeserializeObject<MileagePlus>(response);
            if (ucMemberShipV2 != null)
                return ok= true;

            #region
            //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("uasp_Validate_MPWithAppIDDeviceID");
            //database.AddInParameter(dbCommand, "@DeviceId", DbType.String, deviceId);
            //database.AddInParameter(dbCommand, "@ApplicationId", DbType.Int32, applicationId);
            //database.AddInParameter(dbCommand, "@MileagePlusNumber", DbType.String, mpNumber);

            //try
            //{
            //    //database.ExecuteNonQuery(dbCommand);
            //    using (IDataReader dataReader = database.ExecuteReader(dbCommand))
            //    {
            //        while (dataReader.Read())
            //        {
            //            if (Convert.ToInt32(dataReader["MPAccountFound"]) > 0)
            //            {
            //                ok = true;
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex) { string msg = ex.Message; }
            #endregion

            return ok;
        }

        //public async Task<bool> ValidateAccountFromCache(string accountNumber, string pinCode)
        //{
        //    var mDb = new MileagePlusDynamoDB(_configuration, _dynamoDBService);
        //    string response = await mDb.ValidateAccount(accountNumber, Headers.ContextValues.Application.Id, Headers.ContextValues.DeviceId, pinCode, Headers.ContextValues.SessionId);
        //    var ucMemberShipV2 = Newtonsoft.Json.JsonConvert.DeserializeObject<United.Mobile.Model.Internal.AccountManagement.MileagePlus>(response);
        //    if (ucMemberShipV2 != null && ucMemberShipV2.HashPincode.Equals(pinCode) && ucMemberShipV2.MileagePlusNumber.Equals(accountNumber))
        //        return true;
        //    else
        //        return false;
        //}
        public  bool ValidateAccountFromCache(string accountNumber, string pinCode)
        {
            bool ok = false;

            ok = ValidateAccountNew(accountNumber, pinCode);

            //if (!ok)
            //{
            //    ok = ValidateAccountOld(accountNumber, pinCode);
            //}

            return ok;
        }

        private bool ValidateAccountNew(string accountNumber, string pinCode)
        {
            bool ok = false;
            #region
            //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("uasp_Select_MileagePlusAndPin");
            //database.AddInParameter(dbCommand, "@MileagePlusNumber", DbType.String, accountNumber);
            //database.AddInParameter(dbCommand, "@PinCode", DbType.String, pinCode);
            //database.AddInParameter(dbCommand, "@HashedInput", DbType.String, pinCode);

            //try
            //{
            //    using (IDataReader dataReader = database.ExecuteReader(dbCommand))
            //    {
            //        while (dataReader.Read())
            //        {
            //            if (Convert.ToInt32(dataReader["AccountFound"]) == 1)
            //            {
            //                ok = true;
            //            }
            //        }
            //    }
            //}
            //catch (System.Exception ex) { string msg = ex.Message; }

            #endregion
            try
            {
                var key = string.Format("{0}::{1}", accountNumber, pinCode);
                _dynamoDBService.GetRecords<ValidateAccount>(_configuration.GetSection("DynamoDBTables").GetValue<string>("uatb_MileagePlusValidation_CSS"), "AccountManagement001", key, "trans0");
                ok = true;
                return ok;
            }
            catch { }
            return ok;
        }

        private bool ValidateAccountOld(string accountNumber, string pinCode)
        {
            bool ok = false;
            #region
            //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("usp_Select_OnePassAndPin1");
            //database.AddInParameter(dbCommand, "@OnePassNumber", DbType.String, accountNumber);
            //database.AddInParameter(dbCommand, "@PinCode", DbType.String, pinCode);
            //database.AddInParameter(dbCommand, "@HashedInput", DbType.String, pinCode);

            //try
            //{
            //    using (IDataReader dataReader = database.ExecuteReader(dbCommand))
            //    {
            //        while (dataReader.Read())
            //        {
            //            if (Convert.ToInt32(dataReader["AccountFound"]) == 1)
            //            {
            //                ok = true;
            //            }
            //        }
            //    }
            //}g
            //catch (System.Exception ex) { string msg = ex.Message; }
            #endregion
            try
            {
                var key = string.Format("{0}::{1}", accountNumber, pinCode);
                _dynamoDBService.GetRecords<ValidateAccount>(_configuration.GetSection("DynamoDBTables").GetValue<string>("uatb_MileagePlusValidation_CSS"), "AccountManagement001", key, "trans0");
                ok = true;
                return ok;

            }
            catch { }
            return ok;
        }

        public async Task<ProfileFOPCreditCardResponse> GetCSLProfileFOPCreditCardResponseInSession(string sessionId)
        {
            ProfileFOPCreditCardResponse profile = new ProfileFOPCreditCardResponse();
            try
            {
                // profile = United.Persist.FilePersist.Load<ProfileFOPCreditCardResponse>(sessionId, profile.ObjectName);
                profile = await _sessionHelperService.GetSession<ProfileFOPCreditCardResponse>(sessionId, profile.ObjectName, new List<string> { sessionId, profile.ObjectName }).ConfigureAwait(false);
            }
            catch (System.Exception)
            {

            }
            return profile;
        }
        public void UpdateTravelerCubaReasonInProfile(List<MOBCPProfile> profiles, MOBCPTraveler traveler)
        {
            if (profiles != null && traveler != null)
            {
                foreach (var profile in profiles)
                {
                    var travelers = profile.Travelers;
                    UpdateMatchedTravelerCubaReason(traveler, travelers);
                }
            }
        }

        public  void UpdateMatchedTravelerCubaReason(MOBCPTraveler traveler, List<MOBCPTraveler> travelers)
        {
            if (travelers != null && travelers.Count > 0)
            {
                travelers.Where(p => p != null && p.Key == traveler.Key).ToList().ForEach(p => p.CubaTravelReason = traveler.CubaTravelReason);
            }
        }

        public List<MOBCreditCard> GetProfileOwnerCreditCardList(MOBCPProfile profile, ref List<MOBAddress> creditCardAddresses, string updatedCCKey)
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
                    break;
                }
            }
            #endregion           
            return savedProfileOwnerCCList;
            #endregion
        }

        public bool IsValidFOPForTPIpayment(string cardType)
        {
            return !string.IsNullOrEmpty(cardType) &&
                (cardType.ToUpper().Trim() == "VI" || cardType.ToUpper().Trim() == "MC" || cardType.ToUpper().Trim() == "AX" || cardType.ToUpper().Trim() == "DS");
        }

        public T ObjectToObjectCasting<T, R>(R request)
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

        public bool EnableInflightContactlessPayment(int appID, string appVersion, bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableInflightContactlessPayment") && !isReshop && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("InflightContactlessPaymentAndroidVersion"), _configuration.GetValue<string>("InflightContactlessPaymentiOSVersion"));
        }

        public bool IncludeMoneyPlusMiles(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableMilesPlusMoney")
                && isApplicationVersionGreater
                (appId, appVersion, "AndroidMilesPlusMoneyVersion", "iPhoneMilesPlusMoneyVersion", "", "", true);
        }

        public bool isApplicationVersionGreater(int applicationID, string appVersion, string androidnontfaversion,
           string iphonenontfaversion, string windowsnontfaversion, string mWebNonELFVersion, bool ValidTFAVersion)
        {
            #region Priya Code for version check

            if (!string.IsNullOrEmpty(appVersion))
            {
                string AndroidNonTFAVersion = _configuration.GetValue<string>(androidnontfaversion) ?? "";
                string iPhoneNonTFAVersion = _configuration.GetValue<string>(iphonenontfaversion) ?? "";
                string WindowsNonTFAVersion = _configuration.GetValue<string>(windowsnontfaversion) ?? "";
                string MWebNonTFAVersion = _configuration.GetValue<string>(mWebNonELFVersion) ?? "";

                Regex regex = new Regex("[0-9.]");
                appVersion = string.Join("",
                    regex.Matches(appVersion).Cast<Match>().Select(match => match.Value).ToArray());
                if (applicationID == 1 && appVersion != iPhoneNonTFAVersion)
                {
                    ValidTFAVersion = GeneralHelper.IsVersion1Greater(appVersion, iPhoneNonTFAVersion);
                }
                else if (applicationID == 2 && appVersion != AndroidNonTFAVersion)
                {
                    ValidTFAVersion = GeneralHelper.IsVersion1Greater(appVersion, AndroidNonTFAVersion);
                }
                else if (applicationID == 6 && appVersion != WindowsNonTFAVersion)
                {
                    ValidTFAVersion = GeneralHelper.IsVersion1Greater(appVersion, WindowsNonTFAVersion);
                }
                else if (applicationID == 16 && appVersion != MWebNonTFAVersion)
                {
                    ValidTFAVersion = GeneralHelper.IsVersion1Greater(appVersion, MWebNonTFAVersion);
                }
            }
            #endregion

            return ValidTFAVersion;
        }

        public bool IncludeFFCResidual(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableFFCResidual")
                && isApplicationVersionGreater
                (appId, appVersion, "AndroidFFCResidualVersion", "iPhoneFFCResidualVersion", "", "", true);
        }

        public async Task<ProfileResponse> GetCSLProfileResponseInSession(string sessionId)
        {
            ProfileResponse profile = new ProfileResponse();
            try
            {
                 profile = await _sessionHelperService.GetSession<ProfileResponse>(sessionId, profile.ObjectName, new List<string> { sessionId, profile.ObjectName }).ConfigureAwait(false);
            }
            catch (System.Exception)
            {

            }
            return profile;
        }
        /*
        public bool ValidateHashPinAndGetAuthToken(string accountNumber, string hashPinCode, int applicationId, string deviceId, string appVersion, ref string validAuthToken)
        {
            bool ok = false;
            bool iSDPAuthentication = _configuration.GetValue<bool>("EnableDPToken");
            string SPname = string.Empty;

            /// CSS Token length is 36 and Data Power Access Token length is more than 1500 to 1700 chars
            if (iSDPAuthentication)
            {
                SPname = "uasp_select_MileagePlusAndPin_DP";
            }
            else
            {
                SPname = "uasp_select_MileagePlusAndPin_CSS";
            }

            //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand(SPname);
            //database.AddInParameter(dbCommand, "@MileagePlusNumber", DbType.String, accountNumber);
            //database.AddInParameter(dbCommand, "@HashPincode", DbType.String, hashPinCode);
            //database.AddInParameter(dbCommand, "@ApplicationID", DbType.Int32, applicationId);
            //database.AddInParameter(dbCommand, "@AppVersion", DbType.String, appVersion);
            //database.AddInParameter(dbCommand, "@DeviceID", DbType.String, deviceId);
            //try
            //{
            //    using (IDataReader dataReader = database.ExecuteReader(dbCommand))
            //    {
            //        while (dataReader.Read())
            //        {
            //            if (Convert.ToInt32(dataReader["AccountFound"]) == 1)
            //            {
            //                ok = true;
            //                validAuthToken = dataReader["AuthenticatedToken"].ToString();
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex) { string msg = ex.Message; }

            if (ok == false && _configuration.GetValue<string>("ByPassMPByPassCheckForDpMPSignCall2_1_41") != null &&
                _configuration.GetValue<string>("ByPassMPByPassCheckForDpMPSignCall2_1_41").ToUpper().Trim() == appVersion.ToUpper().Trim())
            {
                var deviceDynamodb = new DeviceDynamDB(_configuration, _dynamoDBService);
                ok = deviceDynamodb.ValidateDeviceIDAPPID(deviceId, applicationId, accountNumber, appVersion);
            }

            return ok;
        }       
      */
        public MOBSHOPReservation MakeReservationFromPersistReservation(MOBSHOPReservation reservation, Model.Shopping.Reservation bookingPathReservation,
            Session session)
        {
            if (reservation == null)
            {
                reservation = new MOBSHOPReservation(_configuration,_cachingService);
            }
            reservation.CartId = bookingPathReservation.CartId;
            reservation.PointOfSale = bookingPathReservation.PointOfSale;
            reservation.ClubPassPurchaseRequest = bookingPathReservation.ClubPassPurchaseRequest;
            reservation.CreditCards = bookingPathReservation.CreditCards;
            reservation.CreditCardsAddress = bookingPathReservation.CreditCardsAddress;
            reservation.FareLock = bookingPathReservation.FareLock;
            reservation.FareRules = bookingPathReservation.FareRules;
            reservation.IsSignedInWithMP = bookingPathReservation.IsSignedInWithMP;
            reservation.NumberOfTravelers = bookingPathReservation.NumberOfTravelers;
            reservation.Prices = bookingPathReservation.Prices;
            reservation.SearchType = bookingPathReservation.SearchType;
            reservation.SeatPrices = bookingPathReservation.SeatPrices;
            reservation.SessionId = session.SessionId;
            reservation.Taxes = bookingPathReservation.Taxes;
            reservation.UnregisterFareLock = bookingPathReservation.UnregisterFareLock;
            reservation.AwardTravel = bookingPathReservation.AwardTravel;
            reservation.LMXFlights = bookingPathReservation.LMXFlights;
            reservation.IneligibleToEarnCreditMessage = bookingPathReservation.IneligibleToEarnCreditMessage;
            reservation.OaIneligibleToEarnCreditMessage = bookingPathReservation.OaIneligibleToEarnCreditMessage;
            reservation.SeatPrices = bookingPathReservation.SeatPrices;
            reservation.IsCubaTravel = bookingPathReservation.IsCubaTravel;

            if (bookingPathReservation.TravelersCSL != null && bookingPathReservation.TravelerKeys != null)
            {
                List<MOBCPTraveler> lstTravelers = new List<MOBCPTraveler>();
                foreach (string travelerKey in bookingPathReservation.TravelerKeys)
                {
                    lstTravelers.Add(bookingPathReservation.TravelersCSL[travelerKey]);
                }
                reservation.TravelersCSL = lstTravelers;

                if (session.IsReshopChange)
                {
                    if (reservation.IsCubaTravel)
                    {
                        reservation.TravelersCSL.ForEach(x => { x.PaxID = x.PaxIndex + 1; x.IsPaxSelected = true; });
                    }
                    else
                    {
                        reservation.TravelersCSL.ForEach(x => { x.Message = string.Empty; x.CubaTravelReason = null; });
                    }
                    bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL = reservation.TravelersCSL;
                }
            }

            reservation.TravelersRegistered = bookingPathReservation.TravelersRegistered;
            reservation.TravelOptions = bookingPathReservation.TravelOptions;
            reservation.Trips = bookingPathReservation.Trips;
            reservation.ReservationPhone = bookingPathReservation.ReservationPhone;
            reservation.ReservationEmail = bookingPathReservation.ReservationEmail;
            reservation.FlightShareMessage = bookingPathReservation.FlightShareMessage;
            reservation.PKDispenserPublicKey = bookingPathReservation.PKDispenserPublicKey;
            reservation.IsRefundable = bookingPathReservation.IsRefundable;
            reservation.ISInternational = bookingPathReservation.ISInternational;
            reservation.ISFlexibleSegmentExist = bookingPathReservation.ISFlexibleSegmentExist;
            reservation.ClubPassPurchaseRequest = bookingPathReservation.ClubPassPurchaseRequest;
            reservation.GetALLSavedTravelers = bookingPathReservation.GetALLSavedTravelers;
            reservation.IsELF = bookingPathReservation.IsELF;
            reservation.IsSSA = bookingPathReservation.IsSSA;
            reservation.IsMetaSearch = bookingPathReservation.IsMetaSearch;
            reservation.MetaSessionId = bookingPathReservation.MetaSessionId;
            reservation.IsUpgradedFromEntryLevelFare = bookingPathReservation.IsUpgradedFromEntryLevelFare;
            reservation.SeatAssignmentMessage = bookingPathReservation.SeatAssignmentMessage;
            reservation.IsReshopCommonFOPEnabled = bookingPathReservation.IsReshopCommonFOPEnabled;

            if (bookingPathReservation.TCDAdvisoryMessages != null && bookingPathReservation.TCDAdvisoryMessages.Count > 0)
            {
                reservation.TCDAdvisoryMessages = bookingPathReservation.TCDAdvisoryMessages;
            }
            //##Price Break Down - Kirti
            if (_configuration.GetValue<bool>("EnableShopPriceBreakDown"))
            {
                reservation.ShopPriceBreakDown = GetPriceBreakDown(bookingPathReservation);
            }

            if (session != null && !string.IsNullOrEmpty(session.EmployeeId) && reservation != null)
            {
                reservation.IsEmp20 = true;
            }
            if (bookingPathReservation.IsCubaTravel)
            {
                reservation.CubaTravelInfo = bookingPathReservation.CubaTravelInfo;
            }
            reservation.FormOfPaymentType = bookingPathReservation.FormOfPaymentType;
            if (bookingPathReservation.FormOfPaymentType == MOBFormofPayment.PayPal || bookingPathReservation.FormOfPaymentType == MOBFormofPayment.PayPalCredit)
            {
                reservation.PayPal = bookingPathReservation.PayPal;
                reservation.PayPalPayor = bookingPathReservation.PayPalPayor;
            }
            if (bookingPathReservation.FormOfPaymentType == MOBFormofPayment.Masterpass)
            {
                if (bookingPathReservation.MasterpassSessionDetails != null)
                    reservation.MasterpassSessionDetails = bookingPathReservation.MasterpassSessionDetails;
                if (bookingPathReservation.Masterpass != null)
                    reservation.Masterpass = bookingPathReservation.Masterpass;
            }
            if (bookingPathReservation.FOPOptions != null && bookingPathReservation.FOPOptions.Count > 0) //FOP Options Fix Venkat 12/08
            {
                reservation.FOPOptions = bookingPathReservation.FOPOptions;
            }

            if (bookingPathReservation.IsReshopChange)
            {
                reservation.ReshopTrips = bookingPathReservation.ReshopTrips;
                reservation.Reshop = bookingPathReservation.Reshop;
                reservation.IsReshopChange = true;
            }
            reservation.ELFMessagesForRTI = bookingPathReservation.ELFMessagesForRTI;
            if (bookingPathReservation.ShopReservationInfo != null)
            {
                reservation.ShopReservationInfo = bookingPathReservation.ShopReservationInfo;
            }
            if (bookingPathReservation.ShopReservationInfo2 != null)
            {
                reservation.ShopReservationInfo2 = bookingPathReservation.ShopReservationInfo2;
            }

            if (bookingPathReservation.ReservationEmail != null)
            {
                reservation.ReservationEmail = bookingPathReservation.ReservationEmail;
            }

            if (bookingPathReservation.TripInsuranceFile != null && bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo != null)
            {
                reservation.TripInsuranceInfoBookingPath = bookingPathReservation.TripInsuranceFile.TripInsuranceBookingInfo;
            }
            else
            {
                reservation.TripInsuranceInfoBookingPath = null;
            }
            reservation.AlertMessages = bookingPathReservation.AlertMessages;
            reservation.IsRedirectToSecondaryPayment = bookingPathReservation.IsRedirectToSecondaryPayment;
            reservation.RecordLocator = bookingPathReservation.RecordLocator;
            reservation.Messages = bookingPathReservation.Messages;
            reservation.CheckedbagChargebutton = bookingPathReservation.CheckedbagChargebutton;
            reservation.IsCanadaTravel = _shoppingUtility.GetFlightsByCountryCode(bookingPathReservation?.Trips, "CA");
            _shoppingUtility.SetEligibilityforETCTravelCredit(reservation, session, bookingPathReservation);
            return reservation;
        }

        public TripPriceBreakDown GetPriceBreakDown(Model.Shopping.Reservation reservation)
        {
            //##Price Break Down - Kirti
            var priceBreakDownObj = new TripPriceBreakDown();
            bool hasAward = false;
            string awardPrice = string.Empty;
            string basePrice = string.Empty;
            string totalPrice = string.Empty;
            bool hasOneTimePass = false;
            string oneTimePassCost = string.Empty;
            bool hasFareLock = false;
            double awardPriceValue = 0;
            double basePriceValue = 0;

            if (reservation != null)
            {

                priceBreakDownObj.PriceBreakDownDetails = new PriceBreakDownDetails();
                priceBreakDownObj.PriceBreakDownSummary = new PriceBreakDownSummary();

                foreach (var travelOption in reservation.TravelOptions)
                {
                    if (travelOption.Key.Equals("FareLock"))
                    {
                        hasFareLock = true;

                        priceBreakDownObj.PriceBreakDownDetails.FareLock = new List<PriceBreakDown2Items>();
                        priceBreakDownObj.PriceBreakDownSummary.FareLock = new PriceBreakDown2Items();
                        var fareLockAmount = new PriceBreakDown2Items();
                        foreach (var subItem in travelOption.SubItems)
                        {
                            if (subItem.Key.Equals("FareLockHoldDays"))
                            {

                                fareLockAmount.Text1 = string.Format("{0} {1}", subItem.Amount, "Day FareLock");
                            }
                        }
                        //Row 0 Column 0
                        fareLockAmount.Price1 = travelOption.DisplayAmount;
                        priceBreakDownObj.PriceBreakDownDetails.FareLock.Add(fareLockAmount);
                        priceBreakDownObj.PriceBreakDownSummary.FareLock = fareLockAmount;


                        priceBreakDownObj.PriceBreakDownDetails.FareLock.Add(new PriceBreakDown2Items() { Text1 = "Total due now" });
                        //Row 1 Column 0
                    }
                }

                StringBuilder tripType = new StringBuilder();
                if (reservation.SearchType.Equals("OW"))
                {
                    tripType.Append("Oneway");
                }
                else if (reservation.SearchType.Equals("RT"))
                {
                    tripType.Append("Roundtrip");
                }
                else
                {
                    tripType.Append("MultipleTrip");
                }
                tripType.Append(" (");
                tripType.Append(reservation.NumberOfTravelers);
                tripType.Append(reservation.NumberOfTravelers > 1 ? " travelers)" : " traveler)");

                //row 2 coulum 0

                foreach (var price in reservation.Prices)
                {
                    switch (price.DisplayType)
                    {
                        case "MILES":
                            hasAward = true;
                            awardPrice = price.FormattedDisplayValue;
                            awardPriceValue = price.Value;
                            break;

                        case "TRAVELERPRICE":
                            basePrice = price.FormattedDisplayValue;
                            basePriceValue = price.Value;
                            break;

                        case "TOTAL":
                            totalPrice = price.FormattedDisplayValue;
                            break;

                        case "ONE-TIME PASS":
                            hasOneTimePass = true;
                            oneTimePassCost = price.FormattedDisplayValue;
                            break;

                        case "GRAND TOTAL":
                            if (!hasFareLock)
                                totalPrice = price.FormattedDisplayValue;
                            break;
                    }
                }

                string travelPrice = string.Empty;
                double travelPriceValue = 0;
                //row 2 column 1
                if (hasAward)
                {
                    travelPrice = awardPrice;
                    travelPriceValue = awardPriceValue;
                }
                else
                {
                    travelPrice = basePrice;
                    travelPriceValue = basePriceValue;
                }

                priceBreakDownObj.PriceBreakDownDetails.Trip = new PriceBreakDown2Items() { Text1 = tripType.ToString(), Price1 = travelPrice };

                priceBreakDownObj.PriceBreakDownSummary.TravelOptions = new List<PriceBreakDown2Items>();

                decimal taxNfeesTotal = 0;
                ShopStaticUtility.BuildTaxesAndFees(reservation, priceBreakDownObj, out taxNfeesTotal);




                if (((reservation.SeatPrices != null && reservation.SeatPrices.Count > 0) ||
                    reservation.TravelOptions != null && reservation.TravelOptions.Count > 0 || hasOneTimePass) && !hasFareLock)
                {
                    priceBreakDownObj.PriceBreakDownDetails.AdditionalServices = new PriceBreakDownAddServices();

                    // Row n+ 5 column 0
                    // Row n+ 5 column 1

                    priceBreakDownObj.PriceBreakDownDetails.AdditionalServices.Seats = new List<PriceBreakDown4Items>();
                    priceBreakDownObj.PriceBreakDownDetails.AdditionalServices.Seats.Add(new PriceBreakDown4Items() { Text1 = "Additional services:" });

                    ShopStaticUtility.BuildSeatPrices(reservation, priceBreakDownObj);

                    //build travel options
                    ShopStaticUtility.BuildTravelOptions(reservation, priceBreakDownObj);
                }

                if (hasOneTimePass)
                {
                    priceBreakDownObj.PriceBreakDownDetails.AdditionalServices.OneTimePass = new List<PriceBreakDown2Items>();
                    priceBreakDownObj.PriceBreakDownDetails.AdditionalServices.OneTimePass.Add(new PriceBreakDown2Items() { Text1 = "One-Time Pass", Price1 = oneTimePassCost });

                    priceBreakDownObj.PriceBreakDownSummary.TravelOptions.Add(new PriceBreakDown2Items() { Text1 = "One-Time Pass", Price1 = oneTimePassCost });
                }

                var finalPriceSummary = new PriceBreakDown2Items();

                priceBreakDownObj.PriceBreakDownDetails.Total = new List<PriceBreakDown2Items>();
                priceBreakDownObj.PriceBreakDownSummary.Total = new List<PriceBreakDown2Items>();
                if (hasFareLock)
                {
                    //column 0
                    finalPriceSummary.Text1 = "Total price (held)";
                }
                else
                {

                    //  buildDottedLine(); column 1
                    finalPriceSummary.Text1 = "Total price";
                }
                if (hasAward)
                {
                    //colum 1
                    finalPriceSummary.Price1 = awardPrice;
                    priceBreakDownObj.PriceBreakDownDetails.Total.Add(finalPriceSummary);

                    priceBreakDownObj.PriceBreakDownSummary.Total.Add(new PriceBreakDown2Items() { Price1 = string.Format("+{0}", totalPrice) });

                    priceBreakDownObj.PriceBreakDownSummary.Trip = new List<PriceBreakDown2Items>()
                             {
                                 new PriceBreakDown2Items()
                                 {
                                    Text1 = tripType.ToString(), Price1 = string.Format("${0}", taxNfeesTotal.ToString("F"))
                                 }

                             };

                }
                else
                {
                    //column 1
                    finalPriceSummary.Price1 = totalPrice;
                    priceBreakDownObj.PriceBreakDownDetails.Total.Add(new PriceBreakDown2Items() { Text1 = totalPrice });

                    priceBreakDownObj.PriceBreakDownSummary.Trip = new List<PriceBreakDown2Items>()
                             {
                                new PriceBreakDown2Items()
                                {
                                  Text1 = tripType.ToString(), Price1 = string.Format("${0}", (travelPriceValue + Convert.ToDouble(taxNfeesTotal)).ToString("F"))
                                }

                             };
                }


                priceBreakDownObj.PriceBreakDownSummary.Total.Add(finalPriceSummary);

            }
            return priceBreakDownObj;
        }

       
        private void UpdateGrandTotal(MOBSHOPReservation reservation, bool isCommonMethod = false)
        {
            var grandTotalIndex = reservation.Prices.FindIndex(a => a.PriceType == "GRAND TOTAL");
            if (grandTotalIndex >= 0)
            {
                double extraMilePurchaseAmount = (reservation?.Prices?.Where(a => a.DisplayType == "MPF")?.FirstOrDefault()?.Value != null) ?
                                         Convert.ToDouble(reservation?.Prices?.Where(a => a.DisplayType == "MPF")?.FirstOrDefault()?.Value) : 0;
                string priceTypeDescription = reservation?.Prices?.Where(a => a.DisplayType == "MPF")?.FirstOrDefault()?.PriceTypeDescription;
                if (extraMilePurchaseAmount > 0 && (priceTypeDescription == null || priceTypeDescription?.Contains("Additional") == false))
                {
                    reservation.Prices[grandTotalIndex].Value += extraMilePurchaseAmount;
                    CultureInfo ci = null;
                    ci = TopHelper.GetCultureInfo(reservation?.Prices[grandTotalIndex].CurrencyCode);
                    reservation.Prices[grandTotalIndex].DisplayValue = reservation.Prices[grandTotalIndex].Value.ToString();
                    reservation.Prices[grandTotalIndex].FormattedDisplayValue = formatAmountForDisplay(reservation?.Prices[grandTotalIndex].Value.ToString(), ci, false);
                }
            }
        }
        private string formatAmountForDisplay(string amt, CultureInfo ci, /*string currency,*/ bool roundup = true, bool isAward = false)
        {
            string newAmt = amt;
            decimal amount = 0;
            decimal.TryParse(amt, out amount);

            try
            {

                //decimal tempAmt = -1;

                //decimal.TryParse(amt, out tempAmt);

                //if (tempAmt > -1)
                //{
                //    if (roundup)
                //    {
                //        int newTempAmt = (int)decimal.Ceiling(tempAmt);
                //        newAmt = newTempAmt.ToString();
                //    }
                //    else
                //    {
                //        newAmt = tempAmt.ToString("F");
                //    }
                //}

                string currencySymbol = "";

                RegionInfo ri = new RegionInfo(ci.Name);

                switch (ri.ISOCurrencySymbol.ToUpper())
                {
                    case "JPY":
                    case "EUR":
                    case "CAD":
                    case "GBP":
                    case "CNY":
                    case "USD":
                    case "AUD":
                    default:
                        //currencySymbol = GetCurrencySymbol(currency.ToUpper());
                        //newAmt = currencySymbol + newAmt;
                        newAmt = GetCurrencySymbol(ci, amount, roundup);
                        break;
                }

            }
            catch { }

            return isAward ? "+ " + newAmt : newAmt;
        }
        private string GetCurrencySymbol(CultureInfo ci, /*string currencyCode,*/ decimal amount, bool roundup)
        {
            string result = string.Empty;

            try
            {

                //decimal tempAmt = -1;
                //decimal.TryParse(amount, out tempAmt);

                if (amount > -1)
                {
                    if (roundup)
                    {
                        int newTempAmt = (int)decimal.Ceiling(amount);
                        //var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

                        //foreach (var ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
                        //{
                        try
                        {
                            var ri = new RegionInfo(ci.Name);
                            //if (ri.ISOCurrencySymbol.ToUpper() == currencyCode.ToUpper())
                            //{
                            //result = ri.CurrencySymbol;
                            CultureInfo tempCi = Thread.CurrentThread.CurrentCulture;
                            Thread.CurrentThread.CurrentCulture = ci;
                            result = newTempAmt.ToString("c0");
                            Thread.CurrentThread.CurrentCulture = tempCi;
                            //break;
                            //}
                        }
                        catch { }
                        //}
                        //newAmt = newTempAmt.ToString();
                    }
                    else
                    {
                        //var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

                        //foreach (var ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
                        //{
                        try
                        {
                            var ri = new RegionInfo(ci.Name);
                            //if (ri.ISOCurrencySymbol.ToUpper() == currencyCode.ToUpper())
                            //{
                            //result = ri.CurrencySymbol;
                            CultureInfo tempCi = Thread.CurrentThread.CurrentCulture;
                            Thread.CurrentThread.CurrentCulture = ci;
                            result = amount.ToString("c");
                            Thread.CurrentThread.CurrentCulture = tempCi;
                            //break;
                            //}
                        }
                        catch { }
                        //newAmt = tempAmt.ToString("F");
                        //}
                    }
                }
                else
                {
                    if (roundup)
                    {
                        int newTempAmt = (int)decimal.Ceiling(amount);
                        //var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

                        //foreach (var ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
                        //{
                        try
                        {
                            var ri = new RegionInfo(ci.Name);
                            //if (ri.ISOCurrencySymbol.ToUpper() == currencyCode.ToUpper())
                            //{
                            //result = ri.CurrencySymbol;
                            CultureInfo tempCi = Thread.CurrentThread.CurrentCulture;
                            Thread.CurrentThread.CurrentCulture = ci;
                            result = newTempAmt.ToString("c0");
                            Thread.CurrentThread.CurrentCulture = tempCi;
                            //break;
                            //}
                        }
                        catch { }
                        //}
                        //newAmt = newTempAmt.ToString();
                    }
                }

            }
            catch { }

            return result;
        }



        
        
        #region //PromoCode Start


      

        public string GetPriceAfterChaseCredit(decimal price)
        {
            int creditAmt = Convert.ToInt32( _configuration.GetValue<string>("ChaseStatementCredit"));

            CultureInfo culture = new System.Globalization.CultureInfo("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            return String.Format(culture, "{0:C}", price - creditAmt);

            //return (Convert.ToDecimal(price - creditAmt)).ToString("C2", CultureInfo.CurrentCulture);
        }

        public string GetPriceAfterChaseCredit(decimal price, string chaseCrediAmount)
        {
            int creditAmt = 0;

            int.TryParse(chaseCrediAmount, System.Globalization.NumberStyles.AllowCurrencySymbol | System.Globalization.NumberStyles.AllowDecimalPoint, null, out creditAmt);

            CultureInfo culture = new System.Globalization.CultureInfo("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;
            return String.Format(culture, "{0:C}", price - creditAmt);

            //return (Convert.ToDecimal(price - creditAmt)).ToString("C2", CultureInfo.CurrentCulture);
        }

        public bool IsDuplicatePromoCode(List<MOBPromoCode> promoCodes, string promoCode)
        {

            if (promoCodes != null && promoCodes.Any() && promoCodes.Count > 0)
            {
                if (promoCodes.Exists(c => c.PromoCode.Equals(promoCode)))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion   // PromoCode End
        public bool IncludeTravelCredit(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableTravelCredit") &&
                   GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidTravelCreditVersion", "iPhoneTravelCreditVersion", "", "", true, _configuration);
        }
        public  bool IncludeTravelBankFOP(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableTravelBankFOP")
                && GeneralHelper.IsApplicationVersionGreater
                (appId, appVersion, "AndroidTravelBankFOPVersion", "iPhoneTravelBankFOPVersion", "", "", true, _configuration);
        }
        public  bool IsInternationalBillingAddress_CheckinFlowEnabled(MOBApplication application)
        {
            if (_configuration.GetValue<bool>("EnableInternationalBillingAddress_CheckinFlow"))
            {
                if (application != null && GeneralHelper.IsApplicationVersionGreater(application.Id, application.Version.Major, "IntBillingCheckinFlowAndroidversion", "IntBillingCheckinFlowiOSversion", "", "", true, _configuration))
                {
                    return true;
                }
            }
            return false;
        }
        public void FormatChaseCreditStatemnet(MOBCCAdStatement chaseCreditStatement)
        {
            if (_configuration.GetValue<bool>("UpdateChaseColor16788"))
            {
                chaseCreditStatement.styledInitialDisplayPrice = string.IsNullOrWhiteSpace(chaseCreditStatement.initialDisplayPrice) ? "" : HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginning")) + chaseCreditStatement.initialDisplayPrice + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledInitialDisplayText = HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginning")) + _configuration.GetValue<string>("InitialDisplayText") + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledStatementCreditDisplayPrice = string.IsNullOrWhiteSpace(chaseCreditStatement.statementCreditDisplayPrice) ? "" : HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginningWithColor")) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextStrongBeginning")) + GetPriceAfterChaseCredit(0, chaseCreditStatement.statementCreditDisplayPrice) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextStrongEnding")) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledStatementCreditDisplayText = HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginningWithColor")) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextStrongBeginning")) + _configuration.GetValue<string>("StatementCreditDisplayText") + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextStrongEnding")) + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledFinalAfterStatementDisplayPrice = string.IsNullOrWhiteSpace(chaseCreditStatement.finalAfterStatementDisplayPrice) ? "" : HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginning")) + chaseCreditStatement.finalAfterStatementDisplayPrice + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
                chaseCreditStatement.styledFinalAfterStatementDisplayText = HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextBeginning")) + _configuration.GetValue<string>("FinalAfterStatementDisplayText") + HttpUtility.HtmlDecode(_configuration.GetValue<string>("StyledTextEnding"));
            }
        }


    }

}
