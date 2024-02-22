using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.SSR;
using United.Mobile.Model.Internal.AccountManagement;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.Shopping.Internal;
using United.Services.FlightShopping.Common;
using United.Utility.Enum;
using United.Utility.Helper;
using MOBFOPTravelBankDetails = United.Mobile.Model.Shopping.FormofPayment.MOBFOPTravelBankDetails;
using ProfileResponse = United.Mobile.Model.Shopping.ProfileResponse;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.MemberProfile.Domain
{
    public class MemberProfileUtility : IMemberProfileUtility
    {
        private readonly ICacheLog<MemberProfileUtility> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IValidateAccountFC _validateAccount;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly ICachingService _cachingService;
        private readonly IValidateHashPinService _validateHashPinService;
        private readonly IHeaders _headers;
        private readonly IFeatureSettings _featureSettings;
        public MemberProfileUtility(ICacheLog<MemberProfileUtility> logger
            , IConfiguration configuration
            , IDynamoDBService dynamoDBService
            , IValidateAccountFC validateAccount
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , IFFCShoppingcs fFCShoppingcs
            , ICachingService cachingService
            , IValidateHashPinService validateHashPinService
            , IHeaders headers
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _dynamoDBService = dynamoDBService;
            _validateAccount = validateAccount;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
            _fFCShoppingcs = fFCShoppingcs;
            _cachingService = cachingService;
            _headers = headers;
            _validateHashPinService = validateHashPinService;
            _featureSettings = featureSettings;
        }

        public async Task<bool> IsValidGetProfileCSLRequest(string transactionId, string deviceId, int applicationId, string mpNumber, long customerID,string sessionId)
        {
            bool validGetProfileCSlRequest = true; // This flag will set to true either if the wallet call request is a valid one (checking the MP device table withthe device Id and MP passed in the request)
            if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(mpNumber) || customerID == 0 || !await VerifyMileagePlusWithDeviceAPPIDCustID(deviceId, applicationId, mpNumber, customerID,sessionId))
            {
                validGetProfileCSlRequest = false;
            }
            return validGetProfileCSlRequest;
        }

        public async Task<bool> IsValidDeviceRequest(string transactionId, string deviceId, int applicationId, string mpNumber, string sessionId)
        {
            bool validWalletRequest = true; // This flag will set to true either if the wallet call request is a valid one (checking the MP device table withthe device Id and MP passed in the request)
            //if (!string.IsNullOrEmpty(request.PushToken) && !string.IsNullOrEmpty(request.MPNumber))
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

        public async Task<bool> ValidateAccountFromCache(string accountNumber, string pinCode, string sessionId)
        {
            /// return _validateAccount.ValidateAccount(accountNumber, pinCode, sessionId).Result;
            bool ok = false;

            ok = await _validateAccount.ValidateAccount(accountNumber, pinCode, sessionId).ConfigureAwait(false); //ValidateAccountNew(accountNumber, pinCode);

            //if (!ok)
            //{
            //    ok = await _validateAccount.ValidateAccount(accountNumber, pinCode, sessionId).ConfigureAwait(false);  //ValidateAccountOld(accountNumber, pinCode);
            //}

            return ok;
        }

        public bool isApplicationVersionGreater2(int applicationID, string appVersion, string androidnontfaversion,
            string iphonenontfaversion, string windowsnontfaversion, string mWebNonELFVersion)
        {
            #region Nizam Code for version check
            bool ValidTFAVersion = false;
            if (!string.IsNullOrEmpty(appVersion))
            {
                string AndroidNonTFAVersion = _configuration.GetValue<string>(androidnontfaversion) ?? "";
                string iPhoneNonTFAVersion = _configuration.GetValue<string>(iphonenontfaversion) ?? "";
                string WindowsNonTFAVersion = _configuration.GetValue<string>(windowsnontfaversion) ?? "";
                string MWebNonTFAVersion = _configuration.GetValue<string>(mWebNonELFVersion) ?? "";

                Regex regex = new System.Text.RegularExpressions.Regex("[0-9.]");
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

        private async Task<bool> VerifyMileagePlusWithDeviceAPPIDCustID(string deviceId, int applicationId, string mpNumber, long customerID, string sessionId)
        {
            var mileagePlusDynamoDB = new MileagePlusDynamoDB(_configuration, _dynamoDBService, _logger);
            string mPAccountFound = await mileagePlusDynamoDB.VerifyMileagePlusWithDeviceAPPIDCustID(deviceId, applicationId, mpNumber, customerID, sessionId).ConfigureAwait(false);

            return mPAccountFound != null && (mPAccountFound.Contains(customerID.ToString()) || mPAccountFound.Contains(mpNumber));
            
        }

        private async Task<bool> VerifyMileagePlusWithDeviceAPPID(string deviceId, int applicationId, string mpNumber, string sessionId)
        {
            bool ok = false;

            MPData data = new MPData()
            {
                MileagePlusNumber = mpNumber,
                DeviceID = deviceId,
                ApplicationID = applicationId
            };
            try
            {
                var mileagePlusDynamoDB = new MileagePlusDynamoDB(_configuration, _dynamoDBService, _logger);
                var key = string.Format("{0}::{1}::{2}", mpNumber, applicationId, deviceId);
                var mileageplusData = await _dynamoDBService.GetRecords<string>(_configuration.GetValue<string>("DynamoDBTables:uatb_MileagePlusDevice"), "MileagePlusDevice001", key, sessionId).ConfigureAwait(false);

                if (!String.IsNullOrEmpty(mileageplusData))
                {
                    ok = true;
                }
            }
            catch { }
            return ok;
        }
        public async Task<(bool, string validAuthToken)> ValidateHashPinAndGetAuthToken(string accountNumber, string hashPinCode, int applicationId, string deviceId, string appVersion, string validAuthToken, string transactionId, string sessionId)
        {
            bool IsTokenValid = false;            
            var mpResponse = await new HashPin(_logger, _configuration, _validateHashPinService, _dynamoDBService,_headers, _featureSettings).ValidateHashPinAndGetAuthTokenDynamoDB(accountNumber, hashPinCode, applicationId, deviceId, appVersion).ConfigureAwait(false);
            if (mpResponse.IsTokenValid != null && mpResponse.HashPincode == hashPinCode)
            {
                validAuthToken = mpResponse.AuthenticatedToken?.ToString();
                IsTokenValid = true;
            }
            return (IsTokenValid, validAuthToken);
        }
        public string GetMPAuthTokenWithoutPersistSave(string accountNumber, int applicationId, string deviceId, string appVersion)
        {
            string validAuthToken = string.Empty;
            bool iSDPAuthentication = _configuration.GetValue<bool>("EnableDPToken");
            #region
            //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("uasp_Get_MileagePlus_AuthToken_CSS");
            //database.AddInParameter(dbCommand, "@MileagePlusNumber", DbType.String, accountNumber);
            //database.AddInParameter(dbCommand, "@ApplicationID", DbType.Int32, applicationId);
            //database.AddInParameter(dbCommand, "@AppVersion", DbType.String, appVersion);
            //database.AddInParameter(dbCommand, "@DeviceID", DbType.String, deviceId);
            //try
            //{
            //    using (IDataReader dataReader = database.ExecuteReader(dbCommand))
            //    {
            //        while (dataReader.Read())
            //        {
            //            if (iSDPAuthentication)
            //            {
            //                validAuthToken = dataReader["DataPowerAccessToken"].ToString();
            //            }
            //            else
            //            {
            //                validAuthToken = dataReader["AuthenticatedToken"].ToString();
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex) { string msg = ex.Message; }
            #endregion
            try
            {
                MPAuthData data = new MPAuthData()
                {
                    AccountNumber = accountNumber,
                    DeviceID = deviceId,
                    ApplicationID = applicationId,
                    AppVersion = appVersion
                };
                var key = string.Format("{0}::{1}::{2}::{3}", deviceId, applicationId, accountNumber);
                string secondaryKey = "001";
                _dynamoDBService.SaveRecords<MPAuthData>(_configuration.GetValue<string>("DynamoDBTables:uatb_MileagePlusDevice"), "MileagePlusDevice001", key, secondaryKey, data, _headers.ContextValues.SessionId);
            }
            catch { }

            return validAuthToken;
        }

        public async Task<bool> IsTSAFlaggedAccount(string accountNumber, string sessionId)
        {
            bool flaggedAccount = false;
            try
            {
                var accountDynamodb = new AccountDynamoDB(_configuration, _dynamoDBService);
                return await accountDynamodb.IsTSAFlaggedAccount(accountNumber, sessionId);
            }
            catch { }

            return flaggedAccount;
        }
        public async Task<bool> ValidateAccountFromCache(string accountNumber, string pinCode)
        {
            var mDb = new MileagePlusDynamoDB(_configuration, _dynamoDBService, _logger);
            string response = await mDb.ValidateAccount(accountNumber, _headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, pinCode, _headers.ContextValues.SessionId);
            var ucMemberShipV2 = Newtonsoft.Json.JsonConvert.DeserializeObject<United.Mobile.Model.Internal.AccountManagement.MileagePlus>(response);
            if (ucMemberShipV2 != null && ucMemberShipV2.HashPincode.Equals(pinCode) && ucMemberShipV2.MileagePlusNumber.Equals(accountNumber))
                return true;
            else
                return false;
        }
        public bool EnableChaseOfferRTI(int appID, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableChaseOfferRTI") && (!_configuration.GetValue<bool>("EnableChaseOfferRTIVersionCheck") ||
                (_configuration.GetValue<bool>("EnableChaseOfferRTIVersionCheck") && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("AndroidEnableChaseOfferRTIVersion"), _configuration.GetValue<string>("iPhoneEnableChaseOfferRTIVersion"))));
        }
        public bool IsEnableOmniCartReleaseCandidateOneChanges(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableOmniCartReleaseCandidateOneChanges") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableOmniCartReleaseCandidateOneChanges_AppVersion"), _configuration.GetValue<string>("iPhone_EnableOmniCartReleaseCandidateOneChanges_AppVersion"));
        }
        public void CompareOmnicartTravelersWithSavedTravelersandUpdate(United.Mobile.Model.Shopping.Reservation reservation, bool isEnableExtraSeatReasonFixInOmniCartFlow)
        {

            SerializableDictionary<string, MOBCPTraveler> travelerList = new SerializableDictionary<string, MOBCPTraveler>();
            reservation.TravelerKeys = new List<string>();
            int paxIndex = 0;
            // Note: Sc travelers paxindex always starts with 0 and paxindex from alleligibletravelers and shoppingcart.sctravelers may not match.
            //Sctravelers paxIndex is being used in seat map when sending the request in selectseats/registerseatsforbooking 
            foreach (var traveler in reservation.TravelersCSL)
            {

                if (IsTravelerExistsinSavedTravelerList(traveler.Value, reservation.ShopReservationInfo2.AllEligibleTravelersCSL, isEnableExtraSeatReasonFixInOmniCartFlow))
                {
                    var selectedSavedTraveler = reservation.ShopReservationInfo2.AllEligibleTravelersCSL.First(savedTraveler => IsTravelerMatched(traveler.Value, savedTraveler, isEnableExtraSeatReasonFixInOmniCartFlow));
                    selectedSavedTraveler.IsPaxSelected = true;
                    selectedSavedTraveler.TravelerNameIndex = traveler.Value.TravelerNameIndex;
                    selectedSavedTraveler.SelectedSpecialNeeds = new List<TravelSpecialNeed>();
                    selectedSavedTraveler.SelectedSpecialNeeds = traveler.Value.SelectedSpecialNeeds;
                    selectedSavedTraveler.CubaTravelReason = traveler.Value.CubaTravelReason;
                    selectedSavedTraveler.CountryOfResidence = traveler.Value.CountryOfResidence;
                    selectedSavedTraveler.Nationality = traveler.Value.Nationality;
                    selectedSavedTraveler.Seats = traveler.Value.Seats;
                    if (_configuration.GetValue<bool>("EnableOmnicartAssignIndividualTotalAmount"))
                    {
                        selectedSavedTraveler.IndividualTotalAmount = traveler.Value.IndividualTotalAmount;
                        selectedSavedTraveler.CslReservationPaxTypeCode = traveler.Value.CslReservationPaxTypeCode;
                    }
                    if (!_configuration.GetValue<bool>("DisableTravelerTypeCodeAssignmentInOmnicartflow"))
                    {
                        selectedSavedTraveler.TravelerTypeCode = traveler.Value.TravelerTypeCode;
                    }
                    var travelerClone = selectedSavedTraveler.Clone();//Inorder not to change the paxindex in alleligible travelers
                    travelerClone.PaxIndex = paxIndex;
                    AssignIsEligibleForSeatSelectionValue(travelerClone);
                    travelerList.Add(paxIndex.ToString(), travelerClone);
                    reservation.TravelerKeys.Add(paxIndex.ToString());
                }
                else
                {
                    traveler.Value.PaxIndex = paxIndex;
                    AssignIsEligibleForSeatSelectionValue(traveler.Value);
                    travelerList.Add(paxIndex.ToString(), traveler.Value);
                    traveler.Value.PaxID = reservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count() + 1;
                    var travelerClone = traveler.Value.Clone();//Inorder not to change the paxindex in travelcsl(Shoppingcart.Sctravelers)
                    travelerClone.PaxIndex = reservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count();
                    reservation.ShopReservationInfo2.AllEligibleTravelersCSL.Add(travelerClone);
                    reservation.TravelerKeys.Add(paxIndex.ToString());
                }
                paxIndex++;
            }
            reservation.TravelersCSL = travelerList;

        }
        public static void AssignIsEligibleForSeatSelectionValue(MOBCPTraveler traveler)
        {
            if (!string.IsNullOrEmpty(traveler.TravelerTypeCode) && traveler.TravelerTypeCode.ToUpper().Equals("INF"))
            {
                traveler.IsEligibleForSeatSelection = false;
            }
            else
            {
                traveler.IsEligibleForSeatSelection = true;
            }
        }
        private static bool IsTravelerExistsinSavedTravelerList(MOBCPTraveler traveler, List<MOBCPTraveler> savedTravelerList, bool isEnableExtraSeatReasonFixInOmniCartFlow)
        {
            return savedTravelerList.Any(savedTraveler => IsTravelerMatched(traveler, savedTraveler, isEnableExtraSeatReasonFixInOmniCartFlow));
        }

        private static bool IsTravelerMatched(MOBCPTraveler traveler, MOBCPTraveler savedTraveler, bool isEnableExtraSeatReasonFixInOmniCartFlow)
        {
            if (isEnableExtraSeatReasonFixInOmniCartFlow &&  traveler != null && savedTraveler != null && traveler.IsExtraSeat)
                return (savedTraveler.FirstName == traveler.FirstName
                        && savedTraveler.MiddleName == traveler.MiddleName
                        && savedTraveler.LastName == traveler.LastName
                        && GeneralHelper.formateDatetime(savedTraveler.BirthDate) == GeneralHelper.formateDatetime(traveler.BirthDate)
                        && savedTraveler.GenderCode == traveler.GenderCode
                        && savedTraveler.TravelerNameIndex == traveler.TravelerNameIndex);
            else
                return (savedTraveler.FirstName == traveler.FirstName
                        && savedTraveler.MiddleName == traveler.MiddleName
                        && savedTraveler.LastName == traveler.LastName
                        && GeneralHelper.formateDatetime(savedTraveler.BirthDate) == GeneralHelper.formateDatetime(traveler.BirthDate)
                        && savedTraveler.GenderCode == traveler.GenderCode);
        }
        public bool IsOmniCartSavedTrip(Model.Shopping.Reservation reservation)
        {
            return reservation?.ShopReservationInfo2 != null && reservation.ShopReservationInfo2.IsOmniCartSavedTrip;
        }

        public void ValidateTravelersForCubaReason(List<MOBCPTraveler> travelersCSL, bool isCuba)
        {
            if (isCuba)
            {
                //var selectedTravelers = travelersCSL.Where(p => p.IsPaxSelected).ToList();
                //if (selectedTravelers == null || selectedTravelers.Count() == 0)
                //{
                ValidateTravelersCSLForCubaReason(travelersCSL);
                //}
                //else
                //{
                //    ValidateTravelersCSLForCubaReason(selectedTravelers);
                //    travelersCSL.Where(p => p != null && !p.IsPaxSelected).ToList().ForEach(p => p.Message = string.Empty);
                //}
            }
        }

        private void ValidateTravelersCSLForCubaReason(List<MOBCPTraveler> travelersCSL)
        {
            if (travelersCSL != null && travelersCSL.Count > 0)
            {
                foreach (MOBCPTraveler traveler in travelersCSL)
                {
                    if (!IsCubaTravelerHasReason(traveler))
                    {
                        traveler.Message = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage");
                    }
                    else if (!string.IsNullOrEmpty(traveler.Message))
                    {
                        if (!string.IsNullOrEmpty(traveler.FirstName) && !string.IsNullOrEmpty(traveler.LastName) && !string.IsNullOrEmpty(traveler.GenderCode) && !string.IsNullOrEmpty(traveler.BirthDate))
                        {
                            traveler.Message = null;
                        }
                    }
                }
            }
        }
        public void ValidateTravelersCSLForCubaReason(MOBCPProfileResponse profileResponse)
        {
            if (profileResponse != null && profileResponse.Reservation != null && profileResponse.Reservation.IsCubaTravel)
            {
                ValidateTravelersCSLForCubaReason(profileResponse.Reservation);
                //if (profileResponse.Profiles != null && profileResponse.Profiles.Count > 0)
                //{
                //    foreach (var profile in profileResponse.Profiles)
                //    {
                //        //ValidateTravelersCSLForCubaReason(profile.Travelers);
                //    }
                //}
            }
        }
        public void ValidateTravelersCSLForCubaReason(MOBSHOPReservation reservation)
        {
            if (reservation.IsCubaTravel)
            {
                var travelersCSL = reservation.TravelersCSL;
                ValidateTravelersCSLForCubaReason(travelersCSL);
                if (reservation.ShopReservationInfo2 != null && reservation.ShopReservationInfo2.AllEligibleTravelersCSL != null && reservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count > 0)
                {
                    ValidateTravelersForCubaReason(reservation.ShopReservationInfo2.AllEligibleTravelersCSL, reservation.IsCubaTravel);
                }
            }
        }
        private bool IsCubaTravelerHasReason(MOBCPTraveler traveler)
        {
            return (traveler.CubaTravelReason != null && !string.IsNullOrEmpty(traveler.CubaTravelReason.Vanity));
        }
        public string TravelersHeaderText(Reservation bookingPathReservation)
        {
            var tempHeader = "";
            var travelerByGroup = "";
            var travelertype = "";
            if (bookingPathReservation.ShopReservationInfo2.displayTravelTypes != null)
            {
                var reservationGroupBy = from book
                                         in bookingPathReservation.ShopReservationInfo2.displayTravelTypes.GroupBy(book => book.TravelerType)
                                         select new { count = book.Count(), desc = book.First().TravelerDescription, traveltype = book.First().TravelerType };


                foreach (var item in reservationGroupBy)
                {
                    switch (item.traveltype)
                    {
                        case PAXTYPE.Adult:
                            travelertype = (item.count > 1) ? "adults (18-64)" : "adult (18-64)";
                            break;
                        case PAXTYPE.Senior:
                            travelertype = (item.count > 1) ? "seniors (65+)" : "senior (65+)";
                            break;
                        case PAXTYPE.Child15To17:
                            travelertype = (item.count > 1) ? "children (15-17)" : "child (15-17)";
                            break;
                        case PAXTYPE.Child12To14:
                            travelertype = (item.count > 1) ? "children (12-14)" : "child (12-14)";
                            break;
                        case PAXTYPE.Child5To11:
                            travelertype = (item.count > 1) ? "children (5-11)" : "child (5-11)";
                            break;
                        case PAXTYPE.Child2To4:
                            travelertype = (item.count > 1) ? "children (2-4)" : "child (2-4)";
                            break;
                        case PAXTYPE.InfantSeat:
                            travelertype = (item.count > 1) ? "infants(under 2)" : "infant(under 2)";
                            break;
                        case PAXTYPE.InfantLap:
                            travelertype = (item.count > 1) ? "infants on lap" : "infant on lap";
                            break;
                    }
                    travelerByGroup = item.count + " " + travelertype;
                    tempHeader = tempHeader + " " + travelerByGroup + ",";
                }

            }
            return tempHeader;
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

        public void UpdateMatchedTravelerCubaReason(MOBCPTraveler traveler, List<MOBCPTraveler> travelers)
        {
            if (travelers != null && travelers.Count > 0)
            {
                travelers.Where(p => p != null && p.Key == traveler.Key).ToList().ForEach(p => p.CubaTravelReason = traveler.CubaTravelReason);
            }
        }
        public async Task<bool> UpdateViewName(MOBUpdateTravelerRequest request)
        {
            var bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, bookingPathReservation.ObjectName, new List<string> { request.SessionId, bookingPathReservation.ObjectName }).ConfigureAwait(false);

            if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null && !string.IsNullOrEmpty(bookingPathReservation.ShopReservationInfo2.NextViewName) &&
                !bookingPathReservation.ShopReservationInfo2.NextViewName.ToUpper().Equals("TRAVELOPTION"))
            {
                if (bookingPathReservation.TravelersCSL != null && bookingPathReservation.TravelersCSL.Count > 0)
                {
                    foreach (var t in bookingPathReservation.TravelersCSL)
                    {
                        if (t.Value.Key.Equals(request.Traveler.Key))
                        {
                            if (!t.Value.FirstName.ToUpper().Equals(request.Traveler.FirstName.ToUpper()) || !t.Value.LastName.ToUpper().Equals(request.Traveler.LastName.ToUpper()) || !t.Value.BirthDate.Equals(request.Traveler.BirthDate))
                            {
                                bookingPathReservation.ShopReservationInfo2.NextViewName = "TravelOption";
                                await _sessionHelperService.SaveSession(bookingPathReservation, request.SessionId, new List<string> { request.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }

            if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null)
            {
                if (bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL != null && bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL.Count > 0)
                {
                    foreach (var t in bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL)
                    {
                        if (!string.IsNullOrEmpty(t.Key) && t.Key.Equals(request.Traveler.Key))
                        {
                            if (string.IsNullOrEmpty(t.FirstName) || string.IsNullOrEmpty(t.LastName) || string.IsNullOrEmpty(t.BirthDate) || string.IsNullOrEmpty(t.GenderCode) ||
                                (!t.FirstName.ToUpper().Equals(request.Traveler.FirstName.ToUpper()) || !t.LastName.ToUpper().Equals(request.Traveler.LastName.ToUpper()) || !t.BirthDate.Equals(request.Traveler.BirthDate) || !t.GenderCode.Equals(request.Traveler.GenderCode)))
                            {
                                return true;
                            }

                        }
                    }
                }
            }

            return false;
        }
        public async Task<bool> isValidDeviceRequest(string transactionId, string deviceId, int applicationId, string mpNumber,string sessionId)
        {
            bool validWalletRequest = true; // This flag will set to true either if the wallet call request is a valid one (checking the MP device table withthe device Id and MP passed in the request)
            if (!string.IsNullOrEmpty(mpNumber))
            {
                if (_configuration.GetValue<string>("ValidateWalletRequest") != null && _configuration.GetValue<bool>("ValidateWalletRequest"))
                {
                    if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(mpNumber) || !await VerifyMileagePlusWithDeviceAPPID(deviceId, applicationId, mpNumber, sessionId))
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

        public bool IsEnableFeature(string feature, int appId, string appVersion)
        {
            var enableFFC = _configuration.GetSection("EnableU4BCorporateBookingFFC").GetValue<bool>("isEnable");
            var android_AppVersion = _configuration.GetSection("EnableU4BCorporateBookingFFC").GetValue<string>("android_EnableU4BCorporateBookingFFC_AppVersion");
            var iPhone_AppVersion = _configuration.GetSection("EnableU4BCorporateBookingFFC").GetValue<string>("iPhone_EnableU4BCorporateBookingFFC_AppVersion");
            return _configuration.GetValue<bool>("EnableU4BCorporateBooking") && enableFFC && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, android_AppVersion, iPhone_AppVersion);
        }
    }
}
