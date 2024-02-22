using Microsoft.Extensions.Configuration;
//using United.Mobile.Model.Shopping.Internal;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.Model;
using United.Mobile.Model.Catalog;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.Shopping;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Bundles;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.Shopping.Misc;
using United.Mobile.Model.Shopping.PriceBreakDown;
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Utility.Enum;
using United.Utility.Helper;
using AdvisoryType = United.Mobile.Model.Common.AdvisoryType;
using Characteristic = United.Service.Presentation.CommonModel.Characteristic;
//using Characteristic = United.Mobile.Model.Common.Characteristic;
using ContentType = United.Mobile.Model.Common.ContentType;
using FlowType = United.Utility.Enum.FlowType;
using MOBBKTraveler = United.Mobile.Model.Shopping.Booking.MOBBKTraveler;
using MOBFOPCertificateTraveler = United.Mobile.Model.Shopping.FormofPayment.MOBFOPCertificateTraveler;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using ShopResponse = United.Services.FlightShopping.Common.ShopResponse;
using Trip = United.Services.FlightShopping.Common.Trip;
using United.Mobile.Model.Common.SSR;
using United.Services.FlightShopping.Common.SpecialPricing;
using ErrorInfo = United.Services.FlightShopping.Common.ErrorInfo;
using United.Mobile.Model.Shopping.Common;

namespace United.Common.Helper.Shopping
{
    public class ShoppingUtility : IShoppingUtility
    {
        private readonly ICacheLog<ShoppingUtility> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IDPService _dPService;
        private readonly IHeaders _headers;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly ICachingService _cachingService;
        //private static Optimizely optimizely = null;
        private readonly IShoppingBuyMiles _shoppingBuyMiles;
        private readonly IValidateHashPinService _validateHashPinService;
        private readonly IOptimizelyPersistService _optimizelyPersistService;
        private readonly IFFCShoppingcs _ffcShoppingcs;
        private readonly IFeatureSettings _featureSettings;
        private readonly IAuroraMySqlService _auroraMySqlService;
        //  private readonly IOmniCart _omniCart;

        public ShoppingUtility(ICacheLog<ShoppingUtility> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IDPService dPService
            , IHeaders headers
            , IDynamoDBService dynamoDBService
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , ICachingService cachingService
            , IValidateHashPinService validateHashPinService
            , IOptimizelyPersistService optimizelyPersistService
            , IShoppingBuyMiles shoppingBuyMiles
            , IFFCShoppingcs ffcShoppingcs
            , IFeatureSettings featureSettings
            //,IOmniCart omniCart
            , IAuroraMySqlService auroraMySqlService)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _dPService = dPService;
            _headers = headers;
            _dynamoDBService = dynamoDBService;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _cachingService = cachingService;
            _validateHashPinService = validateHashPinService;
            _optimizelyPersistService = optimizelyPersistService;
            _shoppingBuyMiles = shoppingBuyMiles;
            _ffcShoppingcs = ffcShoppingcs;
            _featureSettings = featureSettings;
            _auroraMySqlService = auroraMySqlService;
            //  _omniCart = omniCart;
            new ConfigUtility(_configuration);
        }



        public bool IsSeatMapSupportedOa(string operatingCarrier, string MarketingCarrier)
        {
            if (string.IsNullOrEmpty(operatingCarrier)) return false;
            var seatMapSupportedOa = _configuration.GetValue<string>("SeatMapSupportedOtherAirlines");
            if (string.IsNullOrEmpty(seatMapSupportedOa)) return false;

            var seatMapEnabledOa = seatMapSupportedOa.Split(',');
            if (seatMapEnabledOa.Any(s => s == operatingCarrier.ToUpper().Trim()))
                return true;
            else if (_configuration.GetValue<string>("SeatMapSupportedOtherAirlinesMarketedBy") != null)
            {
                return _configuration.GetValue<string>("SeatMapSupportedOtherAirlinesMarketedBy").Split(',').ToList().Any(m => m == MarketingCarrier + "-" + operatingCarrier);
            }

            return false;
        }

        public bool EnablePreferredZone(int appId, string appVersion)
        {
            if (!string.IsNullOrEmpty(appVersion) && appId != -1)
            {
                return _configuration.GetValue<bool>("isEnablePreferredZone")
               && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidPreferredSeatVersion", "iOSPreferredSeatVersion", "", "", true, _configuration);
            }
            return false;
        }

        public bool IsUPPSeatMapSupportedVersion(int appId, string appVersion)
        {
            if (!string.IsNullOrEmpty(appVersion) && appId != -1)
            {
                return _configuration.GetValue<bool>("EnableUPPSeatmap")
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidUPPSeatmapVersion", "iPhoneUPPSeatmapVersion", "", "", true, _configuration);
            }

            return false;
        }

        public bool IsMixedCabinFilerEnabled(int id, string version)
        {
            if (!_configuration.GetValue<bool>("EnableAwardMixedCabinFiter")) return false;
            return GeneralHelper.IsApplicationVersionGreaterorEqual(id, version, _configuration.GetValue<string>("Android_AwardMixedCabinFiterFeatureSupported_AppVersion"), _configuration.GetValue<string>("iPhone_AwardMixedCabinFiterFeatureSupported_AppVersion"));
        }

        public async Task<bool> EnableFSRETCTravelCreditsFeature()
        {
            return await _featureSettings.GetFeatureSettingValue("EnableFSRETCTravelCreditsFeature").ConfigureAwait(false); //&& GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableFSRETCTravelCreditsFeature_AppVersion"), _configuration.GetValue<string>("Iphone_EnableBookingWheelchairDisclaimer_AppVersion"));
        }


        public bool OaSeatMapExceptionVersion(int applicationId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreater(applicationId, appVersion, "AndroidOaSeatMapExceptionVersion", "iPhoneOaSeatMapExceptionVersion", "", "", true, _configuration);
        }

        public bool IsIBE(Reservation persistedReservation)
        {
            if (_configuration.GetValue<bool>("EnablePBE") && (persistedReservation.ShopReservationInfo2 != null))
            {
                return persistedReservation.ShopReservationInfo2.IsIBE;
            }
            return false;
        }

        public bool IsEMinusSeat(string programCode)
        {
            if (!_configuration.GetValue<bool>("EnableSSA") || string.IsNullOrEmpty(programCode))
                return false;
            programCode = programCode.ToUpper().Trim();
            return programCode.Equals("ASA") || programCode.Equals("BSA");
        }

        public bool EnableUnfinishedBookings(BookingBundlesRequest request)
        {

            return _configuration.GetValue<bool>("EnableUnfinishedBookings")
                    && GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "AndroidEnableUnfinishedBookingsVersion", "iPhoneEnableUnfinishedBookingsVersion", "", "", true, _configuration);
        }

        public bool OaSeatMapSupportedVersion(int applicationId, string appVersion, string carrierCode, string MarketingCarrier = "")
        {
            var supportedOA = false;
            if (IsSeatMapSupportedOa(carrierCode, MarketingCarrier))
            {
                switch (carrierCode)
                {
                    case "AC":
                        {
                            supportedOA = EnableAirCanada(applicationId, appVersion);
                            break;
                        }
                    default:
                        {
                            supportedOA = GeneralHelper.IsApplicationVersionGreater(applicationId, appVersion, "AndroidOaSeatMapVersion", "iPhoneOaSeatMapVersion", "", "", true, _configuration);
                            break;
                        }
                }
            }
            return supportedOA;
        }

        public bool EnableAirCanada(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableAirCanada")
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidAirCanadaVersion", "iPhoneAirCanadaVersion", "", "", true, _configuration);
        }

        public bool EnableTravelerTypes(int appId, string appVersion, bool reshop = false)
        {
            if (!string.IsNullOrEmpty(appVersion) && appId != -1)
            {
                return _configuration.GetValue<bool>("EnableTravelerTypes") && !reshop
               && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidTravelerTypesVersion", "iPhoneTravelerTypesVersion", "", "", true, _configuration);
            }
            return false;
        }

        public bool ShopTimeOutCheckforAppVersion(int appID, string appVersion)
        {
            bool _isDisable = false;
            try
            {
                if (_configuration.GetValue<string>("AppVesrionsTimeOutApp2_1_22") != null)
                {
                    //“1~2.1.22|2~2.1.22”
                    foreach (var appVersionWithID in _configuration.GetValue<string>("AppVesrionsTimeOutApp2_1_22").ToString().Split('|'))
                    {
                        if (appVersionWithID.Split('~')[0].ToString().Trim() == appID.ToString().Trim() && appVersionWithID.Split('~')[1].ToString().Trim() == appVersion.Trim())
                        {
                            _isDisable = true; break;
                        }
                    }

                }
            }
            catch { }
            return _isDisable;
        }



        //public Session CreateShoppingSession(int applicationId, string deviceId, string appVersion, string transactionId, string mileagPlusNumber, string employeeId, bool isBEFareDisplayAtFSR = false, bool isReshop = false, bool isAward = false, string travelType = "")
        //{
        //    var session = new Session
        //    {
        //        DeviceID = deviceId,
        //        AppID = applicationId,
        //        SessionId = Guid.NewGuid().ToString().ToUpper().Replace("-", ""),
        //        CreationTime = DateTime.Now,
        //        LastSavedTime = DateTime.Now,
        //        MileagPlusNumber = mileagPlusNumber,
        //        IsBEFareDisplayAtFSR = isBEFareDisplayAtFSR,
        //        IsReshopChange = isReshop,
        //        IsAward = isAward,
        //        EmployeeId = employeeId
        //    };
        //    if (_configuration.GetValue<bool>("EnableCorporateLeisure"))
        //    {
        //        session.TravelType = travelType;
        //    }
        //    #region //**// LMX Flag For AppID change
        //    bool supressLMX = false;
        //    bool.TryParse(_configuration.GetValue<string>("SupressLMX"), out supressLMX); // ["SupressLMX"] = true to make all Apps Turn off. ["SupressLMX"] = false then will check for each app as below.
        //    if (!supressLMX && _configuration.GetValue<string>("AppIDSToSupressLMX").Trim() != "")
        //    {
        //        string appIDS = _configuration.GetValue<string>("AppIDSToSupressLMX"); // AppIDSToSupressLMX = ~1~2~3~ or ~1~ or empty to allow lmx to all apps
        //        supressLMX = appIDS.Contains("~" + applicationId.ToString() + "~");
        //    }
        //    session.SupressLMXForAppID = supressLMX;
        //    #endregion
        //    var isValidToken = false;
        //    if (!string.IsNullOrEmpty(mileagPlusNumber))
        //    {
        //        session.Token = GetMPAuthToken(mileagPlusNumber, applicationId, deviceId, appVersion, ref session);
        //        var refreshShopTokenIfLoggedInTokenExpInThisMinVal = _configuration.GetValue<string>("RefreshShopTokenIfLoggedInTokenExpInThisMinVal") ?? "";
        //        if (string.IsNullOrEmpty(refreshShopTokenIfLoggedInTokenExpInThisMinVal))
        //        {
        //            if (!string.IsNullOrEmpty(session.Token))
        //            {
        //                isValidToken = CheckIsCSSTokenValid(applicationId, deviceId, appVersion, transactionId, ref session, string.Empty);
        //            }
        //        }
        //        else
        //        {
        //            isValidToken = isValidTokenCheckWithExpireTime(applicationId, deviceId, appVersion, transactionId, ref session, refreshShopTokenIfLoggedInTokenExpInThisMinVal);
        //        }
        //    }
        //    if (isValidToken)
        //        return session;

        //    _sessionHelperService.SaveSession<Session>(session, Headers.ContextValues, session.ObjectName);
        //    var token = _dPService.GetAnonymousToken(applicationId, deviceId, _configuration);
        //    session.Token = token;

        //    return session;
        //}

        //private string GetMPAuthToken(string mileagPlusNumber, int applicationId, string deviceId, string appVersion, ref Session shopSession)
        //{
        //    string validAuthToken = string.Empty;
        //    bool iSDPAuthentication = _configuration.GetValue<bool>("EnableDPToken");
        //    try
        //    {
        //        var mileagePlusDynamoDB = new MileagePlusDynamoDB(_configuration, _dynamoDBService);
        //        var MPAuthToken = mileagePlusDynamoDB.GetMPAuthTokenCSS<United.Mobile.Model.Internal.AccountManagement.MileagePlus>(mileagPlusNumber, applicationId, deviceId, appVersion, shopSession.SessionId);

        //        validAuthToken = (iSDPAuthentication) ? MPAuthToken.DataPowerAccessToken : MPAuthToken.AuthenticatedToken;
        //        shopSession.Token = validAuthToken;
        //        shopSession.IsTokenAuthenticated = true; // if the token is cached with MP at DB means its authenticated

        //        shopSession.TokenExpirationValueInSeconds = Convert.ToDouble(MPAuthToken.TokenExpiryInSeconds);

        //        shopSession.TokenExpireDateTime = Convert.ToDateTime(MPAuthToken.TokenExpireDateTime);
        //    }
        //    catch (Exception ex) { string msg = ex.Message; }

        //    return validAuthToken;
        //}

        //private bool isValidTokenCheckWithExpireTime(int applicationId, string deviceId, string appVersion, string transactionId, ref Session session, string refreshShopTokenIfLoggedInTokenExpInThisMinVal)
        //{
        //    bool isValidToken = false;
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(session.Token) && session.TokenExpireDateTime.Subtract(DateTime.Now).TotalMinutes > Convert.ToInt32(refreshShopTokenIfLoggedInTokenExpInThisMinVal))
        //        {
        //            isValidToken = CheckIsCSSTokenValid(applicationId, deviceId, appVersion, transactionId, ref session, string.Empty);
        //        }
        //    }
        //    catch
        //    {
        //        if (!string.IsNullOrEmpty(session.Token))
        //        {
        //            isValidToken = CheckIsCSSTokenValid(applicationId, deviceId, appVersion, transactionId, ref session, string.Empty);
        //        }
        //    }

        //    return isValidToken;
        //}



        public bool IsPosRedirectInShopEnabled(MOBSHOPShopRequest request)
        {
            bool isPosEnabledInShop = false;
            if (!_configuration.GetValue<bool>("PosRedirectEnabledInShop"))
            {
                var isPosVersionGreater = GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "AndroidIPosShopVersion", "iPhoneIPosShopVersion", "", "", true, _configuration);

                if (isPosVersionGreater)
                {
                    if (!_configuration.GetValue<string>("PosMobileSupportedCountryList").Contains(request.CountryCode))
                    {
                        request.CountryCode = "US";
                    }
                }
            }
            else
            {
                var isPosVersionGreater = GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "AndroidIPosShopVersion", "iPhoneIPosShopVersion", "", "", true, _configuration);

                if (isPosVersionGreater && !_configuration.GetValue<string>("PosMobileSupportedCountryList").Contains(request.CountryCode))
                {
                    isPosEnabledInShop = true;
                }
                else if (!_configuration.GetValue<string>("PosMobileSupportedCountryList").Contains(request.CountryCode))
                {
                    request.CountryCode = "US";
                }
            }

            return isPosEnabledInShop;
        }

        public async Task<Session> ValidateFSRRedesign(MOBSHOPShopRequest request, Session session)
        {
            if (ValidateFSRRedesignforShop(request, session))
            {
                session.IsFSRRedesign = true;
                if (!_configuration.GetValue<bool>("EnableFSRPerformanceChange"))
                {
                    await System.Threading.Tasks.Task.Delay(75);
                }
                // _logger.LogInformation("Set IsFSRRedesign {Value}", session.IsFSRRedesign);
                await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);
            }
            else
            {
                if (request != null && request.Experiments != null && request.Experiments.Any() && request.Experiments.Contains(ShoppingExperiments.FSRRedesignA.ToString()))
                {
                    //request.Experiments.RemoveAll(e=>e.Equals(ShoppingExperiments.FSRRedesignA.ToString()));
                    if (session.IsFSRRedesign)
                    {
                        session.IsFSRRedesign = false;
                        await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);
                    }
                }
                else if (request.Experiments != null && request.Experiments.Any() && request.Experiments.Contains(ShoppingExperiments.FSRRedesignSpecialty.ToString()))
                {
                    request.Experiments?.Remove(ShoppingExperiments.FSRRedesignSpecialty.ToString());
                }
            }

            return session;
        }
        public bool ValidateFSRRedesignforShop(MOBSHOPShopRequest request, Session session)
        {
            if (_configuration.GetValue<bool>("IsEnableNewFSRRedesign") && _configuration.GetValue<bool>("IsEnableNewFSRRedesignForSpecialtyPaths"))
            {
                return (request != null && !(request.IsReshop || request.IsReshopChange)
                && request.Experiments != null && request.Experiments.Any()
                && ((!request.AwardTravel && ((request.Experiments.Contains(ShoppingExperiments.FSRRedesignA.ToString())
                && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("FSRRedesignAndroidversion"), _configuration.GetValue<string>("FSRRedesigniOSversion")))
                    || (request.Experiments.Contains(ShoppingExperiments.FSRRedesignSpecialty.ToString())
                    && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("FSRRedesignSpecialtyAndroidversion"), _configuration.GetValue<string>("FSRRedesignSpecialtyiOSversion"))))
                )
                || (request.AwardTravel && request.Experiments.Contains(ShoppingExperiments.FSRRedesignAward.ToString())
                && _configuration.GetValue<bool>("EnableAwardFSRChanges")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("AndroidAwardFSRChangesVersion"), _configuration.GetValue<string>("iOSAwardFSRChangesVersion")))));
            }
            else
            {
                return (request != null && _configuration.GetValue<bool>("IsEnableNewFSRRedesign") && !(request.IsReshop || request.IsReshopChange)
                && !request.IsCorporateBooking && string.IsNullOrEmpty(request.EmployeeDiscountId)
                && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("FSRRedesignAndroidversion"), _configuration.GetValue<string>("FSRRedesigniOSversion"))
                && session.TravelType != TravelType.CLB.ToString() && !request.IsYoungAdultBooking
                && request.Experiments != null && request.Experiments.Any()
                && ((!request.AwardTravel && request.Experiments.Contains(ShoppingExperiments.FSRRedesignA.ToString()))
                || (request.AwardTravel && request.Experiments.Contains(ShoppingExperiments.FSRRedesignAward.ToString())
                && _configuration.GetValue<bool>("EnableAwardFSRChanges")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("AndroidAwardFSRChangesVersion"), _configuration.GetValue<string>("iOSAwardFSRChangesVersion")))));
            }
        }
        public async Task<bool> ValidateHashPinAndGetAuthToken(string accountNumber, string hashPinCode, int applicationId, string deviceId, string appVersion)
        {
            var list = await new HashPin(_logger, _configuration, _validateHashPinService, _dynamoDBService, _headers, _featureSettings).ValidateHashPinAndGetAuthTokenDynamoDB(accountNumber, hashPinCode, applicationId, deviceId, appVersion, _headers.ContextValues.SessionId).ConfigureAwait(false);

            var ok = (list != null && !string.IsNullOrEmpty(list.HashPincode)) ? true : false;

            return ok;
        }

        public async Task ValidateAndGetSignSignOn(MOBSHOPShopRequest request, Mobile.Model.Shopping.ShopResponse response)
        {
            bool validSSORequest = false;
            try
            {
                string authToken = string.Empty;

                if (!string.IsNullOrEmpty(request.MileagePlusAccountNumber) && !string.IsNullOrEmpty(request.MileagePlusAccountNumber.Trim()) && !string.IsNullOrEmpty(request.HashPinCode))
                {
                    validSSORequest = await ValidateHashPinAndGetAuthToken
                        (request.MileagePlusAccountNumber, request.HashPinCode, request.Application.Id, request.DeviceId, request.Application.Version.Major);
                }
                if (validSSORequest)
                {
                    var WebShareToken = _dPService.GetSSOTokenString(request.Application.Id, request.MileagePlusAccountNumber, _configuration);

                    if (!string.IsNullOrEmpty(WebShareToken))
                    {
                        response.PointOfSale.WebShareToken = WebShareToken;
                        response.PointOfSale.WebSessionShareUrl = _configuration.GetValue<string>("DotcomSSOUrl");

                        response.InternationalPointofSale.WebShareToken = WebShareToken; // duplicate property for Android not to crash
                        response.InternationalPointofSale.WebSessionShareUrl = _configuration.GetValue<string>("DotcomSSOUrl");
                    }
                }
            }
            catch (Exception ex)
            {
                string[] messages = ex.Message.Split('#');
                {
                    ExceptionWrapper exceptionWrapper = new ExceptionWrapper(ex);
                    exceptionWrapper.Message = messages[0];
                    //  logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, "Shop", "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, exceptionWrapper, true, false));
                    // logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, "ShopiPOS", "DotComSSOBuildError", request.Application.Id, request.Application.Version.Major, request.DeviceId, exceptionWrapper, true, false));
                }
            }
        }

        public string BuildDotComRedirectUrl(Mobile.Model.Shopping.MOBSHOPShopRequest request)
        {
            string awdOrRev = request.AwardTravel ? "awd?&at=1&rm=1" : "rev?";

            string redirectUrlToDotCom = string.Format(_configuration.GetValue<string>("PosRedirectUrl"), request.CountryCode, awdOrRev);

            try
            {
                string stops = string.Empty, fareType = string.Empty, fareClass = string.Empty, totalPax = string.Empty;
                if (request.SearchType == "OW" && request.Trips != null && request.Trips.Count > 0)
                {
                    string OriginDestAndDate = $"&tt={1}&f={request.Trips[0].Origin}&t={request.Trips[0].Destination}&d={ShopStaticUtility.FormatDate(request.Trips[0].DepartDate)}";

                    //stops
                    //stops = request.GetNonStopFlightsOnly ? "&sc=1" : request.GetFlightsWithStops ? "&sc=7" : string.Empty;

                    //nearby 100 miles
                    string nearbyAirporMiles = (request.Trips[0].SearchNearbyOriginAirports && request.Trips[0].SearchNearbyDestinationAirports) ? "&cbm=100&cbm2=100" : request.Trips[0].SearchNearbyOriginAirports ? "&cbm=100" : request.Trips[0].SearchNearbyDestinationAirports ? "&cbm2=100" : "&cbm=-1&cbm2=-1";

                    //Ebusiness or First
                    string cabinType = (request.Trips[0].Cabin == "first" || request.Trips[0].Cabin == "businessFirst") ? "&ct=1" : string.Empty;

                    ShopStaticUtility.GetDotComRedirectParameters(request, out fareType, out fareClass, out totalPax);

                    redirectUrlToDotCom = $"{redirectUrlToDotCom}{OriginDestAndDate}{cabinType}{nearbyAirporMiles}{stops}{fareType}{fareClass}{totalPax}";
                }
                else if (request.SearchType == "RT" && request.Trips != null && request.Trips.Count > 1)
                {
                    string OriginDestAndDate = $"&f={request.Trips[0].Origin}&t={request.Trips[0].Destination}&d={ShopStaticUtility.FormatDate(request.Trips[0].DepartDate)}&r={ShopStaticUtility.FormatDate(request.Trips[1].DepartDate)}";

                    //stops
                    //stops = request.GetNonStopFlightsOnly ? "&sc=1,1" : request.GetFlightsWithStops ? "&sc=7,7" : string.Empty;

                    //nearby 100 miles
                    string nearbyAirporMiles = (request.Trips[0].SearchNearbyOriginAirports && request.Trips[1].SearchNearbyDestinationAirports) ? "&cbm=100&cbm2=100" : request.Trips[0].SearchNearbyOriginAirports ? "&cbm=100" : request.Trips[1].SearchNearbyDestinationAirports ? "&cbm2=100" : "&cbm=-1&cbm2=-1";

                    //Ebusiness or First
                    string cabinType = (request.Trips[0].Cabin == "first" || request.Trips[0].Cabin == "businessFirst") ? "&ct=1" : string.Empty;

                    ShopStaticUtility.GetDotComRedirectParameters(request, out fareType, out fareClass, out totalPax);

                    redirectUrlToDotCom = $"{redirectUrlToDotCom}{OriginDestAndDate}{cabinType}{nearbyAirporMiles}{stops}{fareType}{fareClass}{totalPax}";
                }
                else if (request.SearchType == "MD" && request.Trips != null && request.Trips.Count > 0)
                {
                    StringBuilder numberOfStops = new StringBuilder("&sc=");

                    //itip=IAH_ORD_2020-04-30*ORD_EWR_2020-05-03*EWR_IAH_2020-05-10
                    StringBuilder tripsSb = new StringBuilder("&itip=");
                    foreach (var trip in request.Trips)
                    {
                        tripsSb.Append(trip.Origin);
                        tripsSb.Append("_");
                        tripsSb.Append(trip.Destination);
                        tripsSb.Append("_");
                        tripsSb.Append(ShopStaticUtility.FormatDate(trip.DepartDate));
                        tripsSb.Append("*");

                    }
                    if (tripsSb != null && tripsSb.ToString().Length > 1 && tripsSb.ToString().EndsWith("*"))
                    {
                        tripsSb.Length--;
                    }
                    string OriginDestAndDate = $"&tt={2}{tripsSb}";

                    //stops
                    //stops = request.GetNonStopFlightsOnly ? "&sc=1" : request.GetFlightsWithStops ? "&sc=7" : string.Empty;

                    bool isAllTripsAreFirstOrBusiness = request.Trips.All(trip => trip.Cabin.Equals("first", StringComparison.OrdinalIgnoreCase) || trip.Cabin.Equals("businessFirst", StringComparison.OrdinalIgnoreCase));
                    //Cabin type if all trip are not first or business then default to econ 
                    string cabinType = isAllTripsAreFirstOrBusiness ? "&ct=1" : string.Empty;
                    ShopStaticUtility.GetDotComRedirectParameters(request, out fareType, out fareClass, out totalPax);

                    redirectUrlToDotCom = $"{redirectUrlToDotCom}{OriginDestAndDate}{cabinType}{stops}{fareType}{fareClass}{totalPax}";
                }
            }
            catch (Exception ex)
            {
                string[] messages = ex.Message.Split('#');
                // if (traceSwitch.TraceInfo)
                {
                    ExceptionWrapper exceptionWrapper = new ExceptionWrapper(ex);
                    exceptionWrapper.Message = messages[0];
                    _logger.LogError("DotComRedirectUrlBuildError for ShopiPOS {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));
                    // logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, "Shop", "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, exceptionWrapper, true, false));
                    //logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, "ShopiPOS", "DotComRedirectUrlBuildError", request.Application.Id, request.Application.Version.Major, request.DeviceId, exceptionWrapper, true, false));
                }
            }

            return redirectUrlToDotCom;
        }

        //added-Kriti
        public bool EnableRoundTripPricing(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("Shopping - bPricingBySlice")
           && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidEnableRoundTripPricingVersion", "iPhoneEnableRoundTripPricingVersion", "", "", true, _configuration);
        }

        public bool EnableYoungAdult(bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableYoungAdultBooking") && !isReshop;
        }

        public bool EnableAirportDecodeToCityAllAirports(bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableAirportDecodeToCityAllAirports") && !isReshop;
        }

        public bool EnableSSA(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableSSA") && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidSSAVersion", "iPhoneSSAVersion", "", "", true, _configuration);
        }

        public bool EnableMileageBalance(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableMileageBalance") && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidEnableMileageBalanceVersion", "iPhoneEnableMileageBalanceVersion", "", "", true, _configuration);
        }

        public bool EnableFSRAlertMessages(int appId, string appVersion, string travelType)
        {
            return _configuration.GetValue<bool>("EnableFSRAlertMessages")
                     && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidEnableFSRAlertMessagesVersion", "iPhoneEnableFSRAlertMessagesVersion", "", "", true, _configuration)
                     && !DisableFSRAlertMessageTripPlan(appId, appVersion, travelType);
        }


        public bool CheckFSRRedesignFromShopRequest(MOBSHOPShopRequest request)
        {
            return (request != null && _configuration.GetValue<bool>("IsEnableNewFSRRedesign") && !(request.IsReshop || request.IsReshopChange)
                 && !request.IsCorporateBooking && string.IsNullOrEmpty(request.EmployeeDiscountId)
                 && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("FSRRedesignAndroidversion"), _configuration.GetValue<string>("FSRRedesigniOSversion"))
                 && request.TravelType != TravelType.CLB.ToString() && !request.IsYoungAdultBooking
                 && request.Experiments != null && request.Experiments.Any()
            && ((!request.AwardTravel && request.Experiments.Contains(ShoppingExperiments.FSRRedesignA.ToString()))
            || (request.AwardTravel && request.Experiments.Contains(ShoppingExperiments.FSRRedesignAward.ToString())
            && IsAwardFSRRedesignEnabled(request.Application.Id, request.Application.Version.Major))));
        }
        public bool CheckFSRRedesignFromShop(MOBSHOPShopRequest request)
        {
            if (_configuration.GetValue<bool>("IsEnableNewFSRRedesign") && _configuration.GetValue<bool>("IsEnableNewFSRRedesignForSpecialtyPaths"))
            {
                return (request != null && !(request.IsReshop || request.IsReshopChange)
            && request.Experiments != null && request.Experiments.Any()
            && ((!request.AwardTravel && ((request.Experiments.Contains(ShoppingExperiments.FSRRedesignA.ToString()) && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("FSRRedesignAndroidversion"), _configuration.GetValue<string>("FSRRedesigniOSversion")))
                || (request.Experiments.Contains(ShoppingExperiments.FSRRedesignSpecialty.ToString()) && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("FSRRedesignSpecialtyAndroidversion"), _configuration.GetValue<string>("FSRRedesignSpecialtyiOSversion")))
                ))
            || (request.AwardTravel && request.Experiments.Contains(ShoppingExperiments.FSRRedesignAward.ToString())
            && IsAwardFSRRedesignEnabled(request.Application.Id, request.Application.Version.Major))));
            }
            else
            {
                return CheckFSRRedesignFromShopRequest(request);
            }
        }
        public bool CheckSkipElfProperties(MOBSHOPShopRequest request)
        {
            return ((_configuration.GetValue<bool>("EnableCodeRefactorForSetAvailabilityELFProperties")
                && _configuration.GetValue<bool>("EnableFSRBasicEconomyToggleOnBookingMain") /*Master toggle to hide the be column */
                && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("FSRBasicEconomyToggleOnBookingMainAndroidversion"), _configuration.GetValue<string>("FSRBasicEconomyToggleOnBookingMainiOSversion")) /*Version check for latest client changes which hardcoded IsELFFareDisplayAtFSR to true at Shop By Map*/
                && CheckFSRRedesignFromShopRequest(request)/*check for FSR resdesign experiment ON Builds*/
                && request.IsELFFareDisplayAtFSR == false) == false);
        }
        public bool IsEbulkPNRReshopEnabled(int id, string version, ReservationDetail cslReservation = null)
        {
            if (!_configuration.GetValue<bool>("EbulkPNRReshopChanges")) return false;
            else if (cslReservation != null && cslReservation?.Detail?.BookingIndicators.IsBulk == false) return false;
            return GeneralHelper.IsApplicationVersionGreaterorEqual(id, version, _configuration.GetValue<string>("Android_EbulkPNRReshopChanges_AppVersion"), _configuration.GetValue<string>("iPhone_EbulkPNRReshopChanges_AppVersion"));
        }

        public bool IsETCchangesEnabled(int applicationId, string appVersion)
        {
            if (_configuration.GetValue<bool>("ETCToggle") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_ETC_AppVersion"), _configuration.GetValue<string>("iPhone_ETC_AppVersion")))
            {
                return true;
            }
            return false;
        }

        public bool EnableFareDisclouserCopyMessage(bool reshop = false)
        {
            return _configuration.GetValue<bool>("EnableFareDisclosureCopyMessage") && !reshop;
        }

        public string GetFeeWaiverMessage()
        {
            return _configuration.GetValue<string>("ChangeFeeWaiver_Message");
        }

        public MOBSearchFilters SetSearchFiltersOutDefaults(MOBSearchFilters shopSearchFiltersOut)
        {
            #region
            shopSearchFiltersOut.SortTypes = new List<MOBSearchFilterItem>();
            List<string> sortTypesList = _configuration.GetValue<string>("SearchFiletersSortTypes").ToString().Split('|').ToList();
            foreach (string sortType in sortTypesList)
            {
                var item = new MOBSearchFilterItem();
                item.Key = sortType.Split('~')[0].ToString();
                item.Value = sortType.Split('~')[1].ToString();
                item.DisplayValue = sortType.Split('~')[1].ToString();
                shopSearchFiltersOut.SortTypes.Add(item);
            }
            shopSearchFiltersOut.AmenityTypes = new List<MOBSearchFilterItem>();
            List<string> amenityTypesList = _configuration.GetValue<string>("SearchFiletersAmenityTypes").ToString().Split('|').ToList();
            foreach (string amenityType in amenityTypesList)
            {
                var item = new MOBSearchFilterItem();
                item.Key = amenityType.Split('~')[0].ToString();
                item.Value = amenityType.Split('~')[1].ToString();
                item.DisplayValue = amenityType.Split('~')[1].ToString();
                item.IsSelected = true;
                shopSearchFiltersOut.AmenityTypes.Add(item);
            }
            shopSearchFiltersOut.CarrierTypes = new List<MOBSearchFilterItem>();
            List<string> carrierTypesList = _configuration.GetValue<string>("SearchFiletersCarrierTypes").ToString().Split('|').ToList();
            foreach (string carrierType in carrierTypesList)
            {
                var item = new MOBSearchFilterItem();
                item.Key = carrierType.Split('~')[0].ToString();
                item.Value = carrierType.Split('~')[1].ToString();
                item.DisplayValue = carrierType.Split('~')[1].ToString();
                item.IsSelected = true;
                shopSearchFiltersOut.CarrierTypes.Add(item);
            }
            shopSearchFiltersOut.AircraftCabinTypes = new List<MOBSearchFilterItem>();
            List<string> aircraftCabinTypesList = _configuration.GetValue<string>("SearchFiletersAircraftCabinTypes").ToString().Split('|').ToList();
            foreach (string aircraftCabinType in aircraftCabinTypesList)
            {
                var item = new MOBSearchFilterItem();
                item.Key = aircraftCabinType.Split('~')[0].ToString();
                item.Value = aircraftCabinType.Split('~')[1].ToString();
                item.DisplayValue = aircraftCabinType.Split('~')[1].ToString();
                item.IsSelected = true;
                shopSearchFiltersOut.AircraftCabinTypes.Add(item);
            }
            return shopSearchFiltersOut;
            #endregion
        }

        public bool EnableIBEFull()
        {
            return _configuration.GetValue<bool>("EnableIBE");
        }

        public bool EnableIBELite()
        {
            return _configuration.GetValue<bool>("EnableIBELite");
        }

        public bool EnableFSRLabelTexts(int appID, string appVersion, bool isReshop = false)
        {
            var version1 = _configuration.GetValue<string>("FSRLabelTextsAndroidversion");
            var version2 = _configuration.GetValue<string>("FSRLabelTextsiOSversion");
            return _configuration.GetValue<bool>("EnableFSRLabelTexts") && !isReshop && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, version1, version2);
        }

        public bool EnableCovidTestFlightShopping(int appID, string appVersion, bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableCovidTestFlightShopping") && EnableRtiMandateContentsToDisplayByMarket(appID, appVersion, isReshop);
        }

        public bool EnableRtiMandateContentsToDisplayByMarket(int appID, string appVersion, bool isReshop)
        {
            return _configuration.GetValue<bool>("EnableRtiMandateContentsToDisplayByMarket") && !isReshop && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("CovidTestAndroidversion"), _configuration.GetValue<string>("CovidTestiOSversion"));
        }

        public void CheckTripsDepartDateAndAssignPreviousTripDateIfItLesser(Services.FlightShopping.Common.ShopRequest request)
        {
            if (_configuration.GetValue<bool>("BugFixToggleFor17M") && request != null && request.Trips != null && request.Trips.Count > 0)
            {
                for (int i = 0; i < request.Trips.Count; i++)
                {
                    if (i < request.Trips.Count - 1 && DateTime.Parse(request.Trips[i].DepartDate) > DateTime.Parse(request.Trips[i + 1].DepartDate))
                    {
                        request.Trips[i + 1].DepartDate = request.Trips[i].DepartDate;
                    }
                }
            }
        }

        public void GetAirportCityName(string airportCode, ref string airportName, ref string cityName)
        {
            #region
            try
            {
                AirportDynamoDB airportDynamoDB = new AirportDynamoDB(_configuration, _dynamoDBService);
                airportDynamoDB.GetAirportCityName(airportCode, ref airportName, ref cityName, _headers.ContextValues.SessionId);

            }
            catch (System.Exception) { }
            #endregion
        }

        public string GetCurrencySymbol(CultureInfo ci)
        {
            string result = string.Empty;

            try
            {
                int amt = 0;
                CultureInfo tempCi = Thread.CurrentThread.CurrentCulture;
                Thread.CurrentThread.CurrentCulture = ci;
                result = amt.ToString("c0").Substring(0, 1);
                if (string.IsNullOrEmpty(result))
                    result = "$";
                Thread.CurrentThread.CurrentCulture = tempCi;
            }
            catch { }

            return result;
        }

        public bool EnableOptoutScreenHyperlinkSupportedContent(int appID, string version)
        {
            var version1 = _configuration.GetValue<string>("AndroidOptOutHyperlinkSupportedVersion");
            var version2 = _configuration.GetValue<string>("iPhoneOptOutHyperlinkSupportedVersion");
            return _configuration.GetValue<bool>("EnableOptOutHyperlinkSupportedContent") && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, version, version1, version2);
        }

        public bool IsIBeLiteFare(Product prod)
        {
            return EnableIBELite() && !string.IsNullOrWhiteSpace(prod.ProductCode) && _configuration.GetValue<string>("IBELiteShoppingProductCodes").IndexOf(prod.ProductCode.Trim().ToUpper()) > -1;
        }

        public bool FeatureVersionCheck(int appId, string appVersion, string featureName, string androidVersion, string iosVersion)
        {
            if (string.IsNullOrEmpty(appVersion) || string.IsNullOrEmpty(featureName))
                return false;
            return _configuration.GetValue<bool>(featureName) && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, androidVersion, iosVersion, "", "", true, _configuration);
        }
        public async Task<bool> EnableBookingWheelchairDisclaimer(int applicationId, string appVersion)
        {
            return await _featureSettings.GetFeatureSettingValue("EnableBookingWheelchairDisclaimer").ConfigureAwait(false) && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableBookingWheelchairDisclaimer_AppVersion"), _configuration.GetValue<string>("Iphone_EnableBookingWheelchairDisclaimer_AppVersion"));
        }
        public async Task<List<MOBDisplayBagTrackAirportDetails>> GetAirportNamesList(string codes)
        {
            string airportName = codes;
            var airPorts = new List<MOBDisplayBagTrackAirportDetails>();
            if (StaticDataLoader.GetAirports() != null && StaticDataLoader.GetAirports().Count > 0)
                airPorts = StaticDataLoader.GetAirports();
            else
            {
                string[] codeS = airportName.Split(',');
                var airportListFromcache = await _cachingService.GetCache<string>("utb_Airport", _headers.ContextValues.TransactionId).ConfigureAwait(false);
                if (airportListFromcache != null)
                {
                    var airportList = JsonConvert.DeserializeObject<List<AirportList>>(airportListFromcache);
                    foreach (AirportList al in airportList)
                    {
                        airPorts.Add(new MOBDisplayBagTrackAirportDetails()
                        {
                            AirportCode = al.AirportCode,
                            CityName = al.Cityname,
                            AirportNameMobile = al.AirportName
                        });

                    }
                    StaticDataLoader.SetAirports(airPorts);
                }
            }
            return airPorts;
        }

        public async Task<string> GetAirportName(string airportCode)
        {
            AirportDynamoDB airportDynamoDB = new AirportDynamoDB(_configuration, _dynamoDBService);
            return await airportDynamoDB.GetAirportName(airportCode, _headers.ContextValues.SessionId);
        }

        public string formatAllCabinAwardAmountForDisplay(string amt, string cabinType, bool truncate = true, string price = null)
        {
            string newAmt = amt;
            decimal amount = 0;
            decimal.TryParse(amt, out amount);

            try
            {
                if (amount > -1)
                {
                    if (truncate)
                    {
                        //int newTempAmt = (int)decimal.Ceiling(amount);
                        try
                        {
                            if (amount > 999)
                            {
                                amount = amount / 1000;
                                if (amount % 1 > 0)
                                    newAmt = string.Format("{0:n1}", amount) + "K";
                                else
                                    newAmt = string.Format("{0:n0}", amount) + "K";
                            }
                            else if (amount == 0)
                            {
                                newAmt = "---";
                            }
                            else
                            {
                                newAmt = string.Format("{0:n0}", amount);
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        try
                        {
                            newAmt = string.Format("{0:n0}", amount) + " miles";
                        }
                        catch { }
                    }
                }

                if (cabinType.Trim().ToUpper().Contains("FIRST"))
                {
                    newAmt += " First";
                }
                else if (cabinType.Trim().ToUpper().Contains("BUS"))
                {
                    newAmt += " Bus";
                }
                else
                {
                    newAmt += " Eco";
                }

                if (_configuration.GetValue<bool>("EnableAwardPricesForAllProducts"))
                {
                    newAmt = ShopStaticUtility.CabinButtonTextWithPrice(price, newAmt);
                }
            }
            catch { }

            return newAmt;
        }

        public string GetConfigEntriesWithDefault(string configKey, string defaultReturnValue)
        {
            var configString = _configuration.GetValue<string>(configKey);
            if (!string.IsNullOrEmpty(configString))
            {
                return configString;
            }

            return defaultReturnValue;
        }

        public bool IsEnableOmniCartMVP2Changes(int applicationId, string appVersion, bool isDisplayCart)
        {
            if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableOmniCartMVP2Changes_AppVersion"), _configuration.GetValue<string>("iPhone_EnableOmniCartMVP2Changes_AppVersion")) && isDisplayCart)
            {
                return true;
            }
            return false;
        }

        public bool IsFeewaiverEnabled(bool isReshop)
        {
            return _configuration.GetValue<bool>("ChangeFeeWaiverMessagingToggle") && !isReshop;
        }

        public List<InfoWarningMessages> UpdateFeeWaiverMessage(List<InfoWarningMessages> infoWarningMessages)
        {
            List<InfoWarningMessages> tmpList = infoWarningMessages;

            try
            {
                if (infoWarningMessages.Count == 0)
                {
                    infoWarningMessages.Add(new InfoWarningMessages() { IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString(), Messages = new List<string>() { GetFeeWaiverMessageSoftRTI() }, Order = MOBINFOWARNINGMESSAGEORDER.BEFAREINVERSION.ToString() });
                }

                if (infoWarningMessages.Count > 0 && !(infoWarningMessages.Count == 1 && infoWarningMessages[0].Messages[0].Equals(GetFeeWaiverMessageSoftRTI())))
                {
                    foreach (var message in infoWarningMessages.ToList())
                    {
                        if (message.Messages != null && message.Messages.Count > 0 && message.Messages[0].Equals(GetFeeWaiverMessageSoftRTI()))
                        {
                            infoWarningMessages.Remove(message);
                        }
                    }

                    if (!infoWarningMessages.Any(m => m.Messages[0].Contains(GetFeeWaiverMessageSoftRTI())))
                    {
                        var msg = infoWarningMessages.FirstOrDefault(m => m.IconType == MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString());

                        if (msg != null)
                        {
                            if (!string.IsNullOrEmpty(msg.Messages[0]))
                            {
                                msg.Messages[0] += System.Environment.NewLine + System.Environment.NewLine;
                                msg.Messages[0] = string.Concat(msg.Messages[0], GetFeeWaiverMessageSoftRTI());
                            }
                            else
                            {
                                msg.Messages[0] = GetFeeWaiverMessageSoftRTI();
                            }
                        }
                        else
                        {
                            var msgFst = infoWarningMessages.First();
                            msgFst.Messages[0] += System.Environment.NewLine + System.Environment.NewLine;
                            msgFst.Messages[0] = string.Concat(msgFst.Messages[0], GetFeeWaiverMessageSoftRTI());
                        }
                    }
                    else
                    {
                        if (infoWarningMessages.Count == 1 || (infoWarningMessages[0].IconType.Equals(MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString()) && infoWarningMessages[0].Messages[0].Contains(GetFeeWaiverMessageSoftRTI()))
                           || (infoWarningMessages.Any(m => m.IconType.Equals(MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString())) && infoWarningMessages.First(m => m.IconType.Equals(MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString())).Messages[0].Contains(GetFeeWaiverMessageSoftRTI()))
                           || !infoWarningMessages.Any(m => m.IconType.Equals(MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString())))
                        {
                            return infoWarningMessages;
                        }
                        else
                        {
                            bool updated = false;
                            foreach (var msgToUpdate in infoWarningMessages.ToList())
                            {
                                if (msgToUpdate.IconType.Equals(MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString()) && !msgToUpdate.Messages[0].Contains(GetFeeWaiverMessageSoftRTI()) && !updated)
                                {
                                    msgToUpdate.Messages[0] += System.Environment.NewLine + System.Environment.NewLine;
                                    msgToUpdate.Messages[0] = string.Concat(msgToUpdate.Messages[0], GetFeeWaiverMessageSoftRTI());
                                    updated = true;
                                }
                                else
                                {
                                    if (msgToUpdate.Messages[0].IndexOf(GetFeeWaiverMessageSoftRTI()) > 0)
                                    {
                                        msgToUpdate.Messages[0] = msgToUpdate.Messages[0].Remove(msgToUpdate.Messages[0].IndexOf(GetFeeWaiverMessageSoftRTI(), GetFeeWaiverMessageSoftRTI().Length)).Trim();
                                    }
                                }
                            }
                        }
                    }
                }
                return infoWarningMessages;
            }
            catch (Exception)
            {
                return tmpList;
            }
        }

        public List<InfoWarningMessages> TripShareFeeWaiverMessage(List<InfoWarningMessages> infoWarningMessages)
        {
            List<InfoWarningMessages> tmpList = infoWarningMessages;

            try
            {
                if (!infoWarningMessages.Any(m => m.Messages.Contains(GetFeeWaiverMessageSoftRTI())))
                {
                    infoWarningMessages.Add(new InfoWarningMessages() { IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString(), Messages = new List<string>() { GetFeeWaiverMessageSoftRTI() }, Order = MOBINFOWARNINGMESSAGEORDER.BEFAREINVERSION.ToString() });
                }

                return infoWarningMessages;
            }
            catch (Exception e)
            {
                return tmpList;
            }
        }

        public string GetFeeWaiverMessageSoftRTI()
        {
            return _configuration.GetValue<string>("ChangeFeeWaiver_Message_SoftRTI");
        }

        public bool EnableAwardFareRules(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableAwardFareRules")
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidEnableAwardFareRulesVersion", "iPhoneEnableAwardFareRulesVersion", "", "", true, _configuration);
        }
        public async Task<MOBSHOPReservation> GetReservationFromPersist(MOBSHOPReservation reservation, string sessionID)
        {
            #region
            Session session = await GetShoppingSession(sessionID);
            Reservation bookingPathReservation = new Reservation();
            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionID, bookingPathReservation.ObjectName, new List<string> { sessionID, bookingPathReservation.ObjectName });
            return await MakeReservationFromPersistReservation(reservation, bookingPathReservation, session);

            #endregion
        }

        private async Task<Session> GetShoppingSession(string sessionId)
        {
            return await GetShoppingSession(sessionId, true);
        }


        private async Task<Session> GetShoppingSession(string sessionId, bool saveToPersist)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToString().ToUpper().Replace("-", "");
            }

            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);
            if (session == null)
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            if (session.TokenExpireDateTime <= DateTime.Now)
            {
                session.IsTokenExpired = true;
            }

            session.LastSavedTime = DateTime.Now;

            await _sessionHelperService.SaveSession<Session>(session, sessionId, new List<string> { sessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);
            return session;
        }

        public async Task<MOBSHOPReservation> MakeReservationFromPersistReservation(MOBSHOPReservation reservation, Reservation bookingPathReservation,
           Session session)
        {
            if (reservation == null)
            {
                reservation = new MOBSHOPReservation(_configuration, _cachingService);
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
            if (_configuration.GetValue<string>("EnableShopPriceBreakDown") != null &&
                Convert.ToBoolean(_configuration.GetValue<string>("EnableShopPriceBreakDown")))
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
                reservation.ShopReservationInfo2.AllowAdditionalTravelers = !session.IsCorporateBooking;
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
            reservation.IsCanadaTravel = GetFlightsByCountryCode(bookingPathReservation?.Trips, "CA");

            await SetEligibilityforETCTravelCredit(reservation, session, bookingPathReservation).ConfigureAwait(false);

            return reservation;
        }

        public TripPriceBreakDown GetPriceBreakDown(Reservation reservation)
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

        public bool IsForceSeatMapforEPlus(bool isReShop, bool isElf, bool is24HoursWindow, int appid, string appversion)
        {
            if (!isReShop && !isElf && is24HoursWindow && EnableForceEPlus(appid, appversion))
            {
                return true;
            }

            return false;
        }

        public bool EnableForceEPlus(int appId, string appVersion)
        {
            // return GetBooleanConfigValue("EnableForceEPlus");
            return _configuration.GetValue<bool>("EnableForceEPlus")
           && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidForceEPlusVersion", "iPhoneForceEPlusVersion", "", "", true, _configuration);
        }

        public bool IsEnabledNationalityAndResidence(bool isReShop, int appid, string appversion)
        {
            if (!isReShop && EnableNationalityResidence(appid, appversion))
            {
                return true;
            }

            return false;
        }

        public bool EnableNationalityResidence(int appId, string appVersion)
        {
            // return GetBooleanConfigValue("EnableForceEPlus");
            return _configuration.GetValue<bool>("EnableNationalityAndCountryOfResidence")
           && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidiPhonePriceChangeVersion", "AndroidiPhonePriceChangeVersion", "", "", true, _configuration);
        }

        public bool EnableSpecialNeeds(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableSpecialNeeds")
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidEnableSpecialNeedsVersion", "iPhoneEnableSpecialNeedsVersion", "", "", true, _configuration);
        }

        public bool IsEnableFarelockforOmniCart(int appId, string appVersion, bool TravelersRegistered, bool isOmniCartHomeScreenChanges)
        {
            return _configuration.GetValue<bool>("FareLockForOmniCart")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("Android_EnableFarelockForOmniCart"), _configuration.GetValue<string>("IPhone_EnableFarelockForOmniCart"))
                && TravelersRegistered == false && isOmniCartHomeScreenChanges == true;
        }

        public bool EnableInflightContactlessPayment(int appID, string appVersion, bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableInflightContactlessPayment") && !isReshop && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("InflightContactlessPaymentAndroidVersion"), _configuration.GetValue<string>("InflightContactlessPaymentiOSVersion"));
        }

        public bool AllowElfMetaSearchUpsell(int appId, string version)
        {
            var isSupportedAppVersion = GeneralHelper.IsApplicationVersionGreater(appId, version, "AndroidELFMetaSearchUpsellVersion", "iPhoneELFMetaSearchUpsellVersion", "", "", true, _configuration);
            if (isSupportedAppVersion)
            {
                return _configuration.GetValue<bool>("AllowELFMetaSearchUpsell");
            }
            return false;
        }

        public bool EnableUnfinishedBookings(MOBRequest request)
        {
            return _configuration.GetValue<bool>("EnableUnfinishedBookings")
                    && GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "AndroidEnableUnfinishedBookingsVersion", "iPhoneEnableUnfinishedBookingsVersion", "", "", true, _configuration);
        }

        public MOBSHOPUnfinishedBookingTrip MapToMOBSHOPUnfinishedBookingTrip(United.Mobile.Model.ShopTrips.Trip csTrip)
        {
            return new MOBSHOPUnfinishedBookingTrip
            {
                DepartDate = csTrip.DepartDate,
                DepartTime = csTrip.DepartTime,
                Destination = csTrip.Destination,
                Origin = csTrip.Origin,
                Flights = csTrip.Flights.Select(MapToMOBSHOPUnfinishedBookingFlight).ToList()
            };
        }

        public MOBSHOPUnfinishedBookingFlight MapToMOBSHOPUnfinishedBookingFlight(United.Mobile.Model.ShopTrips.Flight cslFlight)
        {
            var ubMOBFlight = new MOBSHOPUnfinishedBookingFlight
            {
                BookingCode = cslFlight.BookingCode,
                DepartDateTime = cslFlight.DepartDateTime,
                Origin = cslFlight.Origin,
                Destination = cslFlight.Destination,
                FlightNumber = cslFlight.FlightNumber,
                MarketingCarrier = cslFlight.MarketingCarrier,
                ProductType = cslFlight.ProductType,
            };
            if (cslFlight.Price != 0)
            {
                ubMOBFlight.Products = new List<MOBSHOPUnfinishedBookingFlightProduct>();
                var ubproduct = new MOBSHOPUnfinishedBookingFlightProduct { Prices = new List<MOBSHOPUnfinishedBookingProductPrice>() };
                ubproduct.Prices.Add(new MOBSHOPUnfinishedBookingProductPrice { Amount = cslFlight.Price ?? 0 });
                ubMOBFlight.Products.Add(ubproduct);
            }

            if (_configuration.GetValue<bool>("EnableShareTripDotComConnectionIssueFix"))
            {
                if (cslFlight.Connections == null || cslFlight.Connections.Count == 0)
                    return ubMOBFlight;

                foreach (var conn in cslFlight.Connections)
                {
                    if (ubMOBFlight.Connections == null)
                        ubMOBFlight.Connections = new List<MOBSHOPUnfinishedBookingFlight>();
                    ubMOBFlight.Connections.Add(MapToMOBSHOPUnfinishedBookingFlight(conn));
                }
            }
            else
            {
                if (ubMOBFlight.Connections == null || ubMOBFlight.Connections.Count == 0)
                    return ubMOBFlight;

                cslFlight.Connections.ForEach(x => ubMOBFlight.Connections.Add(MapToMOBSHOPUnfinishedBookingFlight(x)));
            }

            return ubMOBFlight;
        }

        public bool EnableSavedTripShowChannelTypes(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableUnfinishedBookings") // feature toggle
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidEnableUnfinishedBookingsVersion", "iPhoneEnableUnfinishedBookingsVersion", "", "", true, _configuration)

                    && _configuration.GetValue<bool>("EnableSavedTripShowChannelTypes") // story toggle
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidEnableSavedTripShowChannelTypesVersion", "iPhoneEnableSavedTripShowChannelTypesVersion", "", "", true, _configuration);
        }

        public List<MOBTypeOption> GetFopOptions(int applicationID, string appVersion)
        {
            #region
            List<MOBTypeOption> fopOptionsLocal = new List<MOBTypeOption>();
            string[] fopTypesByLatestVersion = null;
            if (applicationID == 1)
            {
                #region
                fopTypesByLatestVersion = _configuration.GetValue<string>("iOSFOPOptionsFromLatestVersion").Split('~');
                if (fopTypesByLatestVersion != null && fopTypesByLatestVersion.Count() > 0)
                {
                    fopOptionsLocal = GetAppsFOPOptions(appVersion, fopTypesByLatestVersion);
                }
                // Sample Value for AndriodFOPOptionsFromLatestVersion = "2.1.14|FOPOption2|FOPOption3~2.1.17|FOPOption2|FOPOption3|FOPOption4" and make sure the "FOPCount" is greater than or equal to 4 like <add key="FOPCount" value="3" /> value is 4 and 
                //there is value define for "FOPOption4" key like <add key="FOPOption4" value="MasterCardCheckOut|Master Check Out" />
                #endregion
            }
            else if (applicationID == 2)
            {
                #region
                fopTypesByLatestVersion = _configuration.GetValue<string>("AndriodFOPOptionsFromLatestVersion").ToString().Split('~');
                if (fopTypesByLatestVersion != null && fopTypesByLatestVersion.Count() > 0)
                {
                    fopOptionsLocal = GetAppsFOPOptions(appVersion, fopTypesByLatestVersion);
                }
                // Sample Value for AndriodFOPOptionsFromLatestVersion = "2.1.13|FOPOption2|FOPOption3~2.1.17|FOPOption2|FOPOption3|FOPOption4" and make sure the "FOPCount" is greater than or equal to 4 like <add key="FOPCount" value="3" /> value is 4 and 
                //there is value define for "FOPOption4" key like <add key="FOPOption4" value="MasterCardCheckOut|Master Check Out" />
                #endregion
            }
            else if (applicationID == 16)
            {
                #region
                fopTypesByLatestVersion = _configuration.GetValue<string>("MWebFOPOptions").Split('|');
                // Sample Value for <add key="mWebOPOptions" value="FOPOption2|FOPOption3" /> 
                foreach (string fopType in fopTypesByLatestVersion)
                {
                    fopOptionsLocal.Add(GetAvailableFopOptions(fopType));
                }
                #endregion
            }
            return fopOptionsLocal;
            #endregion
            #region
            #endregion
        }

        public List<MOBTypeOption> GetAppsFOPOptions(string appVersion, string[] fopTypesByLatestVersion)
        {
            #region
            List<MOBTypeOption> fopOptionsLocal = new List<MOBTypeOption>();
            foreach (string fopOptionsList in fopTypesByLatestVersion) // fopTypesByLatestVersion = { "2.1.13|FOPOption2|FOPOption3","2.1.17|FOPOption2|FOPOption3|FOPOption4"}
            {
                #region
                string latestAppVersion = fopOptionsList.Split('|')[0].ToString(); // latestAppVersion = fopOtionsList.Split('|') = {"2.1.13","FOPOption2","FOPOption3","FOPOption4"} , fopOtionsList.Split('|')[0] = "2.1.13"  if appVersion = 2.1.17
                Regex regex = new Regex("[0-9.]");
                appVersion = string.Join("", regex.Matches(appVersion).Cast<Match>().Select(match => match.Value).ToArray());
                bool returnFOPOptions = false;
                if (appVersion == latestAppVersion)
                {
                    returnFOPOptions = true;
                }
                else
                {
                    returnFOPOptions = GeneralHelper.IsVersion1Greater(appVersion, latestAppVersion);
                }
                if (returnFOPOptions)
                {
                    fopOptionsLocal = new List<MOBTypeOption>();
                    for (int i = 1; i < fopOptionsList.Split('|').Count(); i++) // fopOtionsList = "2.1.13|FOPOption2|FOPOption3" and fopOtionsList.Split('|') = "2.1.17|FOPOption2|FOPOption3|FOPOption4"
                    {
                        fopOptionsLocal.Add(GetAvailableFopOptions(fopOptionsList.Split('|')[i].ToString()));
                    }
                }
                #endregion
            }
            #endregion
            return fopOptionsLocal;
        }

        public MOBTypeOption GetAvailableFopOptions(string fopType)
        {
            MOBTypeOption fopOption = new MOBTypeOption();  // <add key="FOPOption1" value="ApplePay|Apple Pay" />
            fopOption.Key = string.IsNullOrEmpty(_configuration.GetValue<string>(fopType)) ? "" : _configuration.GetValue<string>(fopType).Split('|')[0];
            fopOption.Value = string.IsNullOrEmpty(_configuration.GetValue<string>(fopType)) ? "" : _configuration.GetValue<string>(fopType).Split('|')[1];
            return fopOption;
        }

        public bool IsDisplayCart(Session session, string travelTypeConfigKey = "DisplayCartTravelTypes")
        {
            string[] travelTypes = _configuration.GetValue<string>(travelTypeConfigKey).Split('|');//"Revenue|YoungAdult"
            bool isDisplayCart = false;
            if (session.IsAward && travelTypes.Contains("Award"))
            {
                isDisplayCart = true;
            }
            else if (!string.IsNullOrEmpty(session.EmployeeId) && travelTypes.Contains("UADiscount"))
            {
                isDisplayCart = true;
            }
            else if (session.IsYoungAdult && travelTypes.Contains("YoungAdult"))
            {
                isDisplayCart = true;
            }
            else if (session.IsCorporateBooking && travelTypes.Contains("Corporate"))
            {
                isDisplayCart = true;
            }
            else if (session.TravelType == TravelType.CLB.ToString() && travelTypes.Contains("CorporateLeisure"))
            {
                isDisplayCart = true;
            }
            else if (!session.IsAward && string.IsNullOrEmpty(session.EmployeeId) && !session.IsYoungAdult && !session.IsCorporateBooking && session.TravelType != TravelType.CLB.ToString() && travelTypes.Contains("Revenue"))
            {
                isDisplayCart = true;
            }

            return isDisplayCart;
        }

        public string GetCSSPublicKeyPersistSessionStaticGUID(int applicationId)
        {
            #region Get Aplication and Profile Ids
            string[] cSSPublicKeyPersistSessionStaticGUIDs = _configuration.GetValue<string>("CSSPublicKeyPersistSessionStaticGUID").Split('|');
            List<string> applicationDeviceTokenSessionIDList = new List<string>();
            foreach (string applicationSessionGUID in cSSPublicKeyPersistSessionStaticGUIDs)
            {
                #region
                if (Convert.ToInt32(applicationSessionGUID.Split('~')[0].ToString().ToUpper().Trim()) == applicationId)
                {
                    return applicationSessionGUID.Split('~')[1].ToString().Trim();
                }
                #endregion
            }
            return "1CSSPublicKeyPersistStatSesion4IphoneApp";
            #endregion
        }

        public List<List<MOBSHOPTax>> GetTaxAndFeesAfterPriceChange(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices, bool isReshopChange = false, int appId = 0, string appVersion = "", string travelType = null)
        {
            List<List<MOBSHOPTax>> taxsAndFees = new List<List<MOBSHOPTax>>();
            CultureInfo ci = null;
            decimal taxTotal = 0.0M;
            decimal subTaxTotal = 0.0M;
            bool isTravelerPriceDirty = false;
            bool isEnableOmniCartMVP2Changes = _configuration.GetValue<bool>("EnableOmniCartMVP2Changes");

            foreach (var price in prices)
            {
                List<MOBSHOPTax> tmpTaxsAndFees = new List<MOBSHOPTax>();

                subTaxTotal = 0;

                if (_configuration.GetValue<bool>("EnableAdvanceSearchCouponBooking") && !string.IsNullOrEmpty(price?.Type) && price.Type.ToUpper() == "NONDISCOUNTPRICE-TRAVELERPRICE")
                    continue;

                if (_configuration.GetValue<bool>("EnableFSRMoneyPlusMilesFeature") && !string.IsNullOrEmpty(price?.Type) && price.Type.ToUpper() == "MILESANDMONEY")
                    continue;

                if (price.SubItems != null && price.SubItems.Count > 0 && (!isReshopChange || (isReshopChange && price.Type.ToUpper() == "TRAVELERPRICE" && !isTravelerPriceDirty))) // Added by Hasnan - # 167553 - 10/04/2017
                {
                    foreach (var subItem in price.SubItems)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(subItem.Currency);
                        }
                        MOBSHOPTax taxNfee = new MOBSHOPTax();
                        taxNfee = new MOBSHOPTax();
                        taxNfee.CurrencyCode = subItem.Currency;
                        taxNfee.Amount = subItem.Amount;
                        taxNfee.DisplayAmount = TopHelper.FormatAmountForDisplay(taxNfee.Amount, ci, false);
                        taxNfee.TaxCode = subItem.Type;
                        taxNfee.TaxCodeDescription = subItem.Description;
                        isTravelerPriceDirty = true;
                        tmpTaxsAndFees.Add(taxNfee);

                        subTaxTotal += taxNfee.Amount;
                    }
                }

                if (tmpTaxsAndFees != null && tmpTaxsAndFees.Count > 0)
                {
                    //add new label as first item for UI
                    MOBSHOPTax tnf = new MOBSHOPTax();
                    tnf.CurrencyCode = tmpTaxsAndFees[0].CurrencyCode;
                    tnf.Amount = subTaxTotal;
                    tnf.DisplayAmount = TopHelper.FormatAmountForDisplay(tnf.Amount, ci, false);
                    tnf.TaxCode = "PERPERSONTAX";
                    if (EnableYADesc(isReshopChange) && price.PricingPaxType != null && price.PricingPaxType.ToUpper().Equals("UAY"))
                    {
                        tnf.TaxCodeDescription = string.Format("{0} {1}: {2} per person", price.Count, "young adult (18-23)", tnf.DisplayAmount);
                    }
                    else
                    {
                        string description = price?.Description;
                        if (EnableShoppingcartPhase2ChangesWithVersionCheck(appId, appVersion) && !isReshopChange && !string.IsNullOrEmpty(travelType) && (travelType == TravelType.CB.ToString() || travelType == TravelType.CLB.ToString()))
                        {
                            description = BuildPaxTypeDescription(price?.PaxTypeCode, price?.Description, price.Count);
                        }
                        tnf.TaxCodeDescription = string.Format("{0} {1}: {2} per person", price.Count, description?.ToLower(), tnf.DisplayAmount);
                    }
                    if (isEnableOmniCartMVP2Changes)
                    {
                        tnf.TaxCodeDescription = tnf.TaxCodeDescription.Replace(" per ", "/");
                    }
                    tmpTaxsAndFees.Insert(0, tnf);
                }
                taxTotal += subTaxTotal * price.Count;
                if (tmpTaxsAndFees.Count > 0)
                {
                    taxsAndFees.Add(tmpTaxsAndFees);
                }
            }
            if (taxsAndFees != null && taxsAndFees.Count > 0)
            {
                //add grand total for all taxes
                List<MOBSHOPTax> lstTnfTotal = new List<MOBSHOPTax>();
                MOBSHOPTax tnfTotal = new MOBSHOPTax();
                tnfTotal.CurrencyCode = taxsAndFees[0][0].CurrencyCode;
                tnfTotal.Amount += taxTotal;
                tnfTotal.DisplayAmount = TopHelper.FormatAmountForDisplay(tnfTotal.Amount, ci, false);
                tnfTotal.TaxCode = "TOTALTAX";
                tnfTotal.TaxCodeDescription = "Taxes and fees total";
                lstTnfTotal.Add(tnfTotal);
                taxsAndFees.Add(lstTnfTotal);
            }

            return taxsAndFees;
        }

        public bool EnableYADesc(bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableYoungAdultBooking") && _configuration.GetValue<bool>("EnableYADesc") && !isReshop;
        }

        public bool IsEnableTaxForAgeDiversification(bool isReShop, int appid, string appversion)
        {
            if (!isReShop && EnableTaxForAgeDiversification(appid, appversion))
            {
                return true;
            }
            return false;
        }

        public bool EnableTaxForAgeDiversification(int appId, string appVersion)
        {
            // return GetBooleanConfigValue("EnableForceEPlus");
            return _configuration.GetValue<bool>("EnableTaxForAgeDiversification")
           && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidiPhoneTaxForAgeDiversificationVersion", "AndroidiPhoneTaxForAgeDiversificationVersion", "", "", true, _configuration);
        }

        public async Task SetELFUpgradeMsg(MOBSHOPAvailability availability, string productCode, MOBRequest request, Session session)
        {
            if (_configuration.GetValue<bool>("ByPassSetUpUpgradedFromELFMessages"))
            {
                if (availability?.Reservation?.IsUpgradedFromEntryLevelFare ?? false)
                {
                    if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                    if (IsNonRefundableNonChangable(productCode))
                    {
                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(await BuildUpgradeFromNonRefuNonChanInfoMessage(request, session));
                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                    }
                    else
                    {
                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(BuildUpgradeFromELFInfoMessage(request.Application.Id));
                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)(MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order)).ToList();
                    }
                }
            }
        }

        public InfoWarningMessages BuildUpgradeFromELFInfoMessage(int ID)
        {
            var infoWarningMessages = new InfoWarningMessages
            {
                Order = MOBINFOWARNINGMESSAGEORDER.INHIBITBOOKING.ToString(), // Using existing order for sorting. 
                IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString(),
                HeaderMessage = (ID == 1) ? _configuration.GetValue<string>("UpgradedFromElfTitle") : string.Empty,
                Messages = new List<string>
                {
                   (ID==1)?_configuration.GetValue<string>("UpgradedFromElfText"):_configuration.GetValue<string>("UpgradedFromElfTextWithHtml")
                }
            };

            return infoWarningMessages;
        }

        public InfoWarningMessages GetBEMessage()
        {
            var message = _configuration.GetValue<string>("BEFareInversionMessage") as string ?? string.Empty;
            return ShopStaticUtility.BuildInfoWarningMessages(message);
        }

        public InfoWarningMessages GetBoeingDisclaimer()
        {
            InfoWarningMessages boeingDisclaimerMessage = new InfoWarningMessages();

            boeingDisclaimerMessage.Order = MOBINFOWARNINGMESSAGEORDER.BOEING737WARNING.ToString();
            if (_configuration.GetValue<string>("737DisclaimerMessageType").Equals("WARNING"))
            {
                boeingDisclaimerMessage.IconType = MOBINFOWARNINGMESSAGEICON.WARNING.ToString();
            }
            else
            {
                boeingDisclaimerMessage.IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString();
            }

            boeingDisclaimerMessage.Messages = new List<string>();
            boeingDisclaimerMessage.Messages.Add((_configuration.GetValue<string>("BOEINGDISCLAIMERMESSAGE") as string) ?? string.Empty);

            return boeingDisclaimerMessage;
        }

        public bool IsBoeingDisclaimer(List<DisplayTrip> trips)
        {
            bool isBoeingDisclaimer = false;

            foreach (var trip in trips)
            {
                if (trip != null && trip.Flights != null)
                {
                    foreach (var flight in trip.Flights)
                    {
                        if (flight.EquipmentDisclosures != null && IsMaxBoeing(flight.EquipmentDisclosures.EquipmentType))
                        {
                            isBoeingDisclaimer = true;
                            break;
                        }

                        if (flight.Connections != null && flight.Connections.Count > 0)
                        {
                            isBoeingDisclaimer = IsConBoeingDisclaimer(flight);
                        }

                        if (isBoeingDisclaimer)
                            break;
                    }
                }
                if (isBoeingDisclaimer)
                    break;
            }

            return isBoeingDisclaimer;
        }

        public bool IsMaxBoeing(string boeingType)
        {
            bool isMaxBoeing = false;

            if (!string.IsNullOrEmpty(boeingType))
            {
                string boeingList = _configuration.GetValue<string>("Boeing7MaxCodeList");
                if (boeingList != null)
                {
                    string[] list = boeingList.Split(',');
                    isMaxBoeing = list.Any(l => l.ToUpper().Equals(boeingType.ToUpper()));
                }
            }

            return isMaxBoeing;
        }

        public bool IsConBoeingDisclaimer(Flight flight)
        {
            bool isBoeingDisclaimer = false;

            foreach (var connection in flight.Connections)
            {
                if (connection.EquipmentDisclosures != null && IsMaxBoeing(connection.EquipmentDisclosures.EquipmentType))
                {
                    isBoeingDisclaimer = true;
                    break;
                }

                if (connection.Connections != null && connection.Connections.Count > 0)
                {
                    isBoeingDisclaimer = IsConBoeingDisclaimer(connection);
                }

                if (isBoeingDisclaimer)
                    break;
            }

            return isBoeingDisclaimer;
        }

        public bool EnableBoeingDisclaimer(bool isReshop = false)
        {
            return _configuration.GetValue<bool>("ENABLEBOEINGDISCLOUSER") && !isReshop;
        }

        public InfoWarningMessages GetInhibitMessage(string bookingCutOffMinutes)
        {
            InfoWarningMessages inhibitMessage = new InfoWarningMessages();

            inhibitMessage.Order = MOBINFOWARNINGMESSAGEORDER.INHIBITBOOKING.ToString();
            inhibitMessage.IconType = MOBINFOWARNINGMESSAGEICON.WARNING.ToString();

            inhibitMessage.Messages = new List<string>();

            if (!_configuration.GetValue<bool>("TurnOffBookingCutoffMinsFromCSL") && !string.IsNullOrEmpty(bookingCutOffMinutes))
            {
                inhibitMessage.Messages.Add(string.Format(_configuration.GetValue<string>("InhibitMessageV2"), bookingCutOffMinutes));
            }
            else
            {
                inhibitMessage.Messages.Add((_configuration.GetValue<string>("InhibitMessage") as string) ?? string.Empty);
            }
            return inhibitMessage;
        }

        public bool IsIBEFullFare(DisplayCart displayCart)
        {
            return EnableIBEFull() &&
                    displayCart != null &&
                    IsIBEFullFare(displayCart.ProductCode);
        }

        public bool IsIBEFullFare(string productCode)
        {
            var iBEFullProductCodes = _configuration.GetValue<string>("IBEFullShoppingProductCodes");
            return EnableIBEFull() && !string.IsNullOrWhiteSpace(productCode) &&
                   !string.IsNullOrWhiteSpace(iBEFullProductCodes) &&
                   iBEFullProductCodes.IndexOf(productCode.Trim().ToUpper()) > -1;
        }

        public bool IsIBELiteFare(DisplayCart displayCart)
        {
            return EnableIBELite() &&
                    displayCart != null &&
                    IsIBELiteFare(displayCart.ProductCode);
        }

        public bool IsIBELiteFare(string productCode)
        {
            var iBELiteProductCodes = _configuration.GetValue<string>("IBELiteShoppingProductCodes");
            return !string.IsNullOrWhiteSpace(productCode) &&
                   !string.IsNullOrWhiteSpace(iBELiteProductCodes) &&
                   iBELiteProductCodes.IndexOf(productCode.Trim().ToUpper()) > -1;
        }

        public bool EnablePBE()
        {
            return _configuration.GetValue<bool>("EnablePBE");
        }

        public List<MOBSHOPPrice> GetPrices(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices,
            bool isAwardBooking, string sessionId, bool isReshopChange = false, string searchType = null,
            bool isFareLockViewRes = false, bool isCorporateFare = false, DisplayCart displayCart = null,
            int appId = 0, string appVersion = "", List<MOBItem> catalogItems = null, bool isNotSelectTripCall = false,
             FlightReservationResponse shopBookingDetailsResponse = null
             , List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> displayFees = null, bool isRegisterOffersFlow = false,
             Session session = null)
        {
            List<MOBSHOPPrice> bookingPrices = new List<MOBSHOPPrice>();
            CultureInfo ci = null;
            var isEnableOmniCartMVP2Changes = _configuration.GetValue<bool>("EnableOmniCartMVP2Changes");
            foreach (var price in prices)
            {
                if (ci == null)
                {
                    ci = TopHelper.GetCultureInfo(price.Currency);
                }

                MOBSHOPPrice bookingPrice = new MOBSHOPPrice();
                decimal NonDiscountTravelPrice = 0;
                double promoValue = 0;
                if (_configuration.GetValue<bool>("EnableAdvanceSearchCouponBooking"))
                {
                    if (price.Type.Equals("NONDISCOUNTPRICE-TRAVELERPRICE", StringComparison.OrdinalIgnoreCase) || price.Type.Equals("NonDiscountPrice-Total", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    if (price.Type.Equals("TRAVELERPRICE", StringComparison.OrdinalIgnoreCase) || (price.Type.Equals("TOTAL", StringComparison.OrdinalIgnoreCase)))
                    {
                        if (price.Type.Equals("TRAVELERPRICE", StringComparison.OrdinalIgnoreCase))
                        {
                            var nonDiscountedPrice = prices.Find(dp => dp.PaxTypeCode == price.PaxTypeCode && dp.Type.ToUpper().Equals("NONDISCOUNTPRICE-TRAVELERPRICE"));
                            var discountedPrice = prices.Find(dp => dp.PaxTypeCode == price.PaxTypeCode && dp.Type.ToUpper().Equals("TRAVELERPRICE"));
                            if (discountedPrice != null && nonDiscountedPrice != null)
                            {
                                promoValue = Math.Round(Convert.ToDouble(nonDiscountedPrice.Amount)
                                             - Convert.ToDouble(discountedPrice.Amount), 2, MidpointRounding.AwayFromZero);
                                NonDiscountTravelPrice = nonDiscountedPrice.Amount;
                            }
                            else
                            {
                                promoValue = 0;
                            }
                        }
                        if (price.Type.Equals("TOTAL", StringComparison.OrdinalIgnoreCase))
                        {
                            var nonDiscountedTotalPrice = prices.Find(dp => dp.PaxTypeCode == price.PaxTypeCode && dp.Type.ToUpper().Equals("NONDISCOUNTPRICE-TOTAL"));
                            var discountedTotalPrice = prices.Find(dp => dp.PaxTypeCode == price.PaxTypeCode && dp.Type.ToUpper().Equals("TOTAL"));
                            if (discountedTotalPrice != null && nonDiscountedTotalPrice != null)
                            {
                                promoValue = Math.Round(Convert.ToDouble(nonDiscountedTotalPrice.Amount)
                                            - Convert.ToDouble(discountedTotalPrice.Amount), 2, MidpointRounding.AwayFromZero);
                                promoValue = UpdatePromoValueForFSRMoneyMiles(prices, session, promoValue);
                            }
                            else
                            {
                                promoValue = 0;
                            }
                        }
                        bookingPrice.PromoDetails = promoValue > 0 ? new MOBPromoCode
                        {
                            PriceTypeDescription = price.Type.Equals("TOTAL", StringComparison.OrdinalIgnoreCase) ? _configuration.GetValue<string>("PromoSavedText") : _configuration.GetValue<string>("PromoCodeAppliedText"),
                            PromoValue = Math.Round(promoValue, 2, MidpointRounding.AwayFromZero),
                            FormattedPromoDisplayValue = "-" + promoValue.ToString("C2", CultureInfo.CurrentCulture)
                        } : null;
                    }
                }

                bookingPrice.CurrencyCode = price.Currency;
                bookingPrice.DisplayType = price.Type;
                bookingPrice.Status = price.Status;
                bookingPrice.Waived = price.Waived;
                bookingPrice.DisplayValue = NonDiscountTravelPrice > 0 ? string.Format("{0:#,0.00}", NonDiscountTravelPrice) : string.Format("{0:#,0.00}", price.Amount);
                if (_configuration.GetValue<bool>("EnableCouponsforBooking") && !string.IsNullOrEmpty(price.PaxTypeCode))
                {
                    bookingPrice.PaxTypeCode = price.PaxTypeCode;
                }
                if (!string.IsNullOrEmpty(searchType))
                {
                    string desc = string.Empty;
                    if (price.Description != null && price.Description.Length > 0)
                    {
                        if (!EnableYADesc(isReshopChange) || price.PricingPaxType == null || !price.PricingPaxType.Equals("UAY"))
                        {
                            desc = ShopStaticUtility.GetFareDescription(price);
                        }
                    }
                    if (EnableYADesc(isReshopChange) && !string.IsNullOrEmpty(price.PricingPaxType) && price.PricingPaxType.ToUpper().Equals("UAY"))
                    {
                        bookingPrice.PriceTypeDescription = ShopStaticUtility.BuildYAPriceTypeDescription(searchType);
                        bookingPrice.PaxTypeDescription = $"{price?.Count} {"young adult (18-23)"}".ToLower();
                    }
                    else
                    if (!string.IsNullOrEmpty(price.Description) && price.Description.ToUpper().Contains("TOTAL"))
                    {
                        bookingPrice.PriceTypeDescription = price?.Description.ToLower();
                        bookingPrice.PaxTypeDescription = $"{price?.Count} {price.Description}".ToLower();
                        if (_configuration.GetValue<bool>("EnableFSRETCCreditsFeature") && prices.Any(x => x.Type.Equals("TravelCredits", StringComparison.OrdinalIgnoreCase) && x.Amount > 0)
                            && price.StrikeThroughPricing > 0 && (int)price.StrikeThroughPricing >= (int)price.Amount)
                        {
                            bookingPrice.StrikeThroughDisplayValue = ((int)price.StrikeThroughPricing - (int)price.Amount).ToString("C2", CultureInfo.CurrentCulture);
                        }
                    }
                    else
                    {
                        if (_configuration.GetValue<bool>("EnableAwardStrikeThroughPricing") && session.IsAward && session.CatalogItems != null && session.CatalogItems.Count > 0 &&
                           session.CatalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.AwardStrikeThroughPricing).ToString() || a.Id == ((int)AndroidCatalogEnum.AwardStrikeThroughPricing).ToString())?.CurrentValue == "1"
                               && price.StrikeThroughPricing > 0 && (int)price.StrikeThroughPricing != (int)price.Amount
                           )
                        {
                            if (_configuration.GetValue<bool>("EnableStrikeThroughTotalMilesFix"))
                            {
                                bookingPrice.StrikeThroughDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay((price.StrikeThroughPricing * price.Count).ToString(), false);
                            }
                            else
                            {
                                bookingPrice.StrikeThroughDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(price.StrikeThroughPricing.ToString(), false);
                            }
                            bookingPrice.StrikeThroughDescription = BuildStrikeThroughDescription();
                        }
                        bookingPrice.PriceTypeDescription = ShopStaticUtility.BuildPriceTypeDescription(searchType, price.Description, price.Count, desc, isFareLockViewRes, isCorporateFare);

                        if (isEnableOmniCartMVP2Changes)
                        {
                            string description = price?.Description;
                            if (EnableShoppingcartPhase2ChangesWithVersionCheck(appId, appVersion) && !isReshopChange && !string.IsNullOrEmpty(session?.TravelType) && (session.TravelType == TravelType.CB.ToString() || session.TravelType == TravelType.CLB.ToString()))
                            {
                                description = BuildPaxTypeDescription(price?.PaxTypeCode, price?.Description, price.Count);
                            }
                            bookingPrice.PaxTypeDescription = $"{price.Count} {description}".ToLower();
                        }
                    }
                }

                if (!isReshopChange)
                {
                    if (!string.IsNullOrEmpty(bookingPrice.DisplayType) && bookingPrice.DisplayType.Equals("MILES") && isAwardBooking && !string.IsNullOrEmpty(sessionId))
                    {
                        if (IsBuyMilesFeatureEnabled(appId, appVersion, catalogItems, isNotSelectTripCall: true) == true
                               && shopBookingDetailsResponse?.DisplayCart?.IsPurchaseIneligible == true && isRegisterOffersFlow == true)
                        {
                            throw new MOBUnitedException(_configuration.GetValue<string>("BuyMilesPriceChangeError"));
                        }
                        else if (IsBuyMilesFeatureEnabled(appId, appVersion, catalogItems, isNotSelectTripCall) == false)
                        {
                            ValidateAwardMileageBalance(sessionId, price.Amount);
                        }
                    }
                }

                double tempDouble = 0;
                double.TryParse(NonDiscountTravelPrice > 0 ? NonDiscountTravelPrice.ToString() : price.Amount.ToString(), out tempDouble);
                bookingPrice.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);

                if (price.Currency.ToUpper() == "MIL")
                {
                    bookingPrice.FormattedDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(price.Amount.ToString(), false);
                }
                else
                {
                    bookingPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(NonDiscountTravelPrice > 0 ? NonDiscountTravelPrice : price.Amount, ci, false);
                }
                UpdatePricesForForMoneyPlusMiles(bookingPrices, price, bookingPrice, session);
                UpdatePricesForTravelCredits(bookingPrices, price, bookingPrice, session);
                bookingPrices.Add(bookingPrice);
            }
            if (_configuration.GetValue<bool>("EnableAdvanceSearchCouponBooking"))
            {
                AddGrandTotalIfNotExistInPrices(bookingPrices);
                AddFreeBagDetailsInPrices(displayCart, bookingPrices);
            }
            if (IsBuyMilesFeatureEnabled(appId, appVersion, isNotSelectTripCall: true))
            {
                _shoppingBuyMiles.UpdatePricesForBuyMiles(bookingPrices, shopBookingDetailsResponse, displayFees);
            }
            return bookingPrices;
        }

        public double UpdatePromoValueForFSRMoneyMiles(List<DisplayPrice> prices, Session session, double promoValue)
        {
            if (_configuration.GetValue<bool>("EnableFSRMoneyPlusMilesFeature") && prices != null && session?.IsMoneyPlusMilesSelected == true)
            {
                var moneyMilesPrice = prices.Find(dp => dp.Type?.ToUpper()?.Equals("MILESANDMONEY") == true);
                if (moneyMilesPrice != null && promoValue > 0)
                    promoValue = promoValue - Convert.ToDouble(moneyMilesPrice.Amount);
            }

            return promoValue;
        }

        private void UpdatePricesForForMoneyPlusMiles(List<MOBSHOPPrice> bookingPrices, DisplayPrice price, MOBSHOPPrice bookingPrice, Session session)
        {
            if (_configuration.GetValue<bool>("EnableFSRMoneyPlusMilesFeature") && session.IsMoneyPlusMilesSelected)
            {
                try
                {
                    if (price.Type.ToUpper().Equals("MILESANDMONEY"))
                    {
                        MOBSHOPPrice moneyPlusMilesRTI = new MOBSHOPPrice();
                        moneyPlusMilesRTI.DisplayType = "MONEYPLUSMILES"; // Used for RTI screen display
                        var miles = price?.SubItems?.Where(a => a.Type == "Miles").FirstOrDefault()?.Amount;
                        if (miles != null && miles > 0)
                        {
                            moneyPlusMilesRTI.FormattedDisplayValue = " + " + UtilityHelper.FormatAwardAmountForDisplay(miles.ToString(), false);
                            moneyPlusMilesRTI.PriceTypeDescription = "";
                            bookingPrice.PriceTypeDescription = _configuration.GetValue<string>("FSRMoneyPlusMilesCartMilesNeededText");
                            bookingPrice.FormattedDisplayValue = "-" + bookingPrice.FormattedDisplayValue + " (" + UtilityHelper.FormatAwardAmountForDisplay(miles.ToString(), false) + ")";
                            bookingPrices.Add(moneyPlusMilesRTI);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.ILoggerWarning("UpdatePricesForForMoneyPlusMiles exception" + ex.Message);
                }
            }
        }
        private void UpdatePricesForTravelCredits(List<MOBSHOPPrice> bookingPrices, DisplayPrice price, MOBSHOPPrice bookingPrice, Session session)
        {

            if (_configuration.GetValue<bool>("EnableFSRETCCreditsFeature") && !string.IsNullOrEmpty(price.Type) && price.Type.Equals("TravelCredits", StringComparison.OrdinalIgnoreCase)
                && bookingPrices.Any(x => x.DisplayType.Equals("TOTAL", StringComparison.OrdinalIgnoreCase) && x.Value >= 0 && string.IsNullOrEmpty(x.StrikeThroughDisplayValue) == false)
                && session.PricingType == PricingType.ETC.ToString())
            {
                try
                {
                    var total = bookingPrices.FirstOrDefault(x => x.DisplayType.Equals("TOTAL", StringComparison.OrdinalIgnoreCase));
                    if (total != null && !string.IsNullOrEmpty(total.StrikeThroughDisplayValue))
                    {
                        //bookingPrice.PriceTypeDescription = _configuration.GetValue<string>("ETCCreditsStrikeThroughTypeDescription");
                        bookingPrice.PriceType = price.Type.ToUpper();
                        bookingPrice.FormattedDisplayValue = " + " + total.StrikeThroughDisplayValue + " Travel Credits";
                    }
                }
                catch (Exception ex)
                {
                    _logger.ILoggerWarning("UpdatePricesForTravelCredits exception" + ex.Message);
                }
            }
        }
        public void AddGrandTotalIfNotExistInPrices(List<MOBSHOPPrice> prices)
        {
            var grandTotalPrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper().Equals("GRAND TOTAL"));
            if (grandTotalPrice == null)
            {
                var totalPrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper().Equals("TOTAL"));
                grandTotalPrice = ShopStaticUtility.BuildGrandTotalPriceForReservation(totalPrice.Value);
                if (_configuration.GetValue<bool>("EnableAdvanceSearchCouponBooking"))
                {
                    grandTotalPrice.PromoDetails = totalPrice.PromoDetails;
                }
                prices.Add(grandTotalPrice);
            }
        }
        public void AddFreeBagDetailsInPrices(DisplayCart displayCart, List<MOBSHOPPrice> prices)
        {
            if (isAFSCouponApplied(displayCart))
            {
                if (displayCart.SpecialPricingInfo.MerchOfferCoupon.Product.ToUpper().Equals("BAG"))
                {
                    prices.Add(new MOBSHOPPrice
                    {
                        PriceTypeDescription = _configuration.GetValue<string>("FreeBagCouponDescription"),
                        DisplayType = "TRAVELERPRICE",
                        FormattedDisplayValue = "",
                        DisplayValue = "",
                        Value = 0
                    });
                }
            }
        }
        public bool isAFSCouponApplied(DisplayCart displayCart)
        {
            if (displayCart != null && displayCart.SpecialPricingInfo != null && displayCart.SpecialPricingInfo.MerchOfferCoupon != null && !string.IsNullOrEmpty(displayCart.SpecialPricingInfo.MerchOfferCoupon.PromoCode) && displayCart.SpecialPricingInfo.MerchOfferCoupon.IsCouponEligible.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }
        public async Task ValidateAwardMileageBalance(string sessionId, decimal milesNeeded)
        {
            CSLShopRequest shopRequest = new CSLShopRequest();
            shopRequest = await _sessionHelperService.GetSession<CSLShopRequest>(sessionId, shopRequest.ObjectName, new List<string> { sessionId, shopRequest.ObjectName }).ConfigureAwait(false);
            if (shopRequest != null && shopRequest.ShopRequest != null && shopRequest.ShopRequest.AwardTravel && shopRequest.ShopRequest.LoyaltyPerson != null && shopRequest.ShopRequest.LoyaltyPerson.AccountBalances != null)
            {
                if (shopRequest.ShopRequest.LoyaltyPerson.AccountBalances[0] != null && shopRequest.ShopRequest.LoyaltyPerson.AccountBalances[0].Balance < milesNeeded)
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("NoEnoughMilesForAwardBooking"));
                }
            }
        }

        //added-Kriti
        public void GetFlattedFlightsForCOGorThruFlights(Trip trip)
        {
            if (_configuration.GetValue<bool>("SavedTripThruOrCOGFlightBugFix"))
            {
                if (trip.Flights.Any()
                    && trip.Flights.GroupBy(x => x.FlightNumber).Any(g => g.Count() > 1))
                {
                    for (int i = 0; i < trip.Flights.Count - 1; i++)
                    {
                        if (trip.Flights[i].FlightNumber == trip.Flights[i + 1].FlightNumber)
                        {
                            trip.Flights[i].Destination = trip.Flights[i + 1].Destination;
                            trip.Flights.RemoveAt(i + 1);
                            i = -1;
                        }
                    }
                }
            }
            else if (_configuration.GetValue<bool>("UnfinishedBookingCOGFlightsCheck"))
            {
                for (int i = 0; i < trip.Flights.Count - 1; i++)
                {
                    if (trip.Flights[i].FlightNumber == trip.Flights[i + 1].FlightNumber)
                    {
                        trip.Flights[i].Destination = trip.Flights[i + 1].Destination;
                        trip.Flights.RemoveAt(i + 1);
                    }
                }
            }
        }

        #region Sathwika

        public TripShare IsShareTripValid(SelectTripResponse selectTripResponse)
        {
            var tripShare = new TripShare();
            var reservation = selectTripResponse?.Availability?.Reservation;
            if (reservation != null && (reservation.AwardTravel
                 || reservation.IsEmp20
                 || (reservation.ShopReservationInfo != null && reservation.ShopReservationInfo.IsCorporateBooking)
                 || (reservation.ShopReservationInfo2 != null && reservation.ShopReservationInfo2.IsYATravel)
                 || reservation.IsReshopChange
                 || (_configuration.GetValue<bool>("EnableCorporateLeisure") && reservation?.ShopReservationInfo2?.TravelType == TravelType.CLB.ToString())))
            {
                return tripShare = null;
            }
            else if (selectTripResponse != null && selectTripResponse.Availability != null && reservation != null
                    && reservation.Trips.Count > 0
                    && reservation.FareLock != null && reservation.FareLock.FareLockProducts != null && reservation.FareLock.FareLockProducts.Count > 0)
            {
                tripShare.ShowShareTrip = true;
            }

            return tripShare;
        }

        public bool EnableReshopCubaTravelReasonVersion(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "iPhoneEnableReshopCubaTravelReasonVersion", "AndroidEnableReshopCubaTravelReasonVersion", "", "", true, _configuration);
        }

        public bool IsETCCombinabilityEnabled(int applicationId, string appVersion)
        {
            if (_configuration.GetValue<bool>("CombinebilityETCToggle") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableETCCombinability_AppVersion"), _configuration.GetValue<string>("iPhone_EnableETCCombinability_AppVersion")))
            {
                return true;
            }

            return false;
        }

        public async Task LoadandAddTravelCertificate(MOBShoppingCart shoppingCart, string sessionId, List<MOBSHOPPrice> prices, bool isETCCertificatesExistInShoppingCartPersist, MOBApplication application)
        {
            var persistedTravelCertifcateResponse = new FOPTravelerCertificateResponse();
            if (_configuration.GetValue<bool>("MTETCToggle") && (prices.Exists(price => price.DisplayType.ToUpper().Trim() == "CERTIFICATE") || isETCCertificatesExistInShoppingCartPersist))
            {
                persistedTravelCertifcateResponse = await _sessionHelperService.GetSession<FOPTravelerCertificateResponse>(sessionId, persistedTravelCertifcateResponse.ObjectName, new List<string> { sessionId, persistedTravelCertifcateResponse.ObjectName }).ConfigureAwait(false);
            }
            else
            {
                persistedTravelCertifcateResponse = await _sessionHelperService.GetSession<FOPTravelerCertificateResponse>(sessionId, persistedTravelCertifcateResponse.ObjectName, new List<string> { sessionId, persistedTravelCertifcateResponse.ObjectName }).ConfigureAwait(false);
            }
            if (_configuration.GetValue<bool>("MTETCToggle") && shoppingCart.IsMultipleTravelerEtcFeatureClientToggleEnabled && shoppingCart?.SCTravelers != null && shoppingCart.SCTravelers.Exists(st => !string.IsNullOrEmpty(st.TravelerNameIndex)))
            {
                if (persistedTravelCertifcateResponse?.ShoppingCart?.CertificateTravelers?.Count > 0)
                {
                    shoppingCart.CertificateTravelers = persistedTravelCertifcateResponse.ShoppingCart.CertificateTravelers;
                }
                else if (shoppingCart.CertificateTravelers != null)
                {
                    AssignCertificateTravelers(shoppingCart, persistedTravelCertifcateResponse, prices, application);
                }
            }
            if (persistedTravelCertifcateResponse?.ShoppingCart?.FormofPaymentDetails?.TravelCertificate != null)
            {
                var formOfPayment = shoppingCart.FormofPaymentDetails;

                MOBFormofPaymentDetails persistedFOPDetail = persistedTravelCertifcateResponse.ShoppingCart.FormofPaymentDetails;
                formOfPayment.TravelCertificate = persistedFOPDetail.TravelCertificate;
                formOfPayment.BillingAddress = formOfPayment.BillingAddress == null ? persistedFOPDetail.BillingAddress : formOfPayment.BillingAddress;
                formOfPayment.Email = formOfPayment.Email == null ? persistedFOPDetail.Email : formOfPayment.Email;
                formOfPayment.Phone = formOfPayment.Phone == null ? persistedFOPDetail.Phone : formOfPayment.Phone;
                formOfPayment.EmailAddress = formOfPayment.EmailAddress == null ? persistedFOPDetail.EmailAddress : formOfPayment.EmailAddress;
                Reservation bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionId, (new Reservation()).ObjectName, new List<string> { sessionId, (new Reservation()).ObjectName }).ConfigureAwait(false);
                var requestSCRES = shoppingCart.Products.Find(p => p.Code == "RES");
                var persistSCRES = persistedTravelCertifcateResponse.ShoppingCart.Products.Find(p => p.Code == "RES");
                bool isSCRESProductGotRefreshed = true;
                if (requestSCRES != null && persistSCRES != null)
                {
                    isSCRESProductGotRefreshed = (requestSCRES.ProdTotalPrice != persistSCRES.ProdTotalPrice);
                }
                ShopStaticUtility.AddGrandTotalIfNotExistInPricesAndUpdateCertificateValue(bookingPathReservation.Prices, formOfPayment);
                UpdateCertificateAmountInTotalPrices(bookingPathReservation.Prices, shoppingCart.Products, formOfPayment.TravelCertificate.TotalRedeemAmount, isSCRESProductGotRefreshed);
                AssignIsOtherFOPRequired(formOfPayment, bookingPathReservation.Prices, shoppingCart.FormofPaymentDetails?.SecondaryCreditCard != null);
                await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, _headers.ContextValues.SessionId, new List<string> { _headers.ContextValues.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                persistedTravelCertifcateResponse.ShoppingCart.FormofPaymentDetails = formOfPayment;
                persistedTravelCertifcateResponse.ShoppingCart.CertificateTravelers = shoppingCart.CertificateTravelers;
                if (_configuration.GetValue<bool>("SavedETCToggle"))
                {
                    UpdateSavedCertificate(shoppingCart);
                    persistedTravelCertifcateResponse.ShoppingCart.ProfileTravelerCertificates = shoppingCart.ProfileTravelerCertificates;
                }
                await _sessionHelperService.SaveSession<FOPTravelerCertificateResponse>(persistedTravelCertifcateResponse, sessionId, new List<string> { sessionId, persistedTravelCertifcateResponse.ObjectName }, persistedTravelCertifcateResponse.ObjectName).ConfigureAwait(false);
                await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, sessionId, new List<string> { sessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName).ConfigureAwait(false);
            }
        }

        public void AssignIsOtherFOPRequired(MOBFormofPaymentDetails formofPaymentDetails, List<MOBSHOPPrice> prices, bool IsSecondaryFOP = false, bool isRemoveAll = false)
        {
            var grandTotalPrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper().Equals("GRAND TOTAL"));
            //if(grandTotalPrice == null)
            //{
            //    var totalPrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper().Equals("TOTAL"));
            //    grandTotalPrice = BuildGrandTotalPriceForReservation(totalPrice.Value);
            //    prices.Add(grandTotalPrice);
            //}
            formofPaymentDetails.IsOtherFOPRequired = (grandTotalPrice.Value > 0);

            //need to update only when travelcertificate is added as formofpayment.
            //Need to update formofpaymentype only when travel certificate is not added as other fop or all the certficates are removed
            if (formofPaymentDetails?.TravelCertificate?.Certificates?.Count > 0 || isRemoveAll)
            {
                if (!formofPaymentDetails.IsOtherFOPRequired && !IsSecondaryFOP)
                {
                    formofPaymentDetails.FormOfPaymentType = MOBFormofPayment.ETC.ToString();
                    if (!_configuration.GetValue<bool>("DisableBugMOBILE9122Toggle") &&
                        !string.IsNullOrEmpty(formofPaymentDetails.CreditCard?.Message) &&
                        _configuration.GetValue<string>("CreditCardDateExpiredMessage").IndexOf(formofPaymentDetails.CreditCard?.Message, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        formofPaymentDetails.CreditCard = null;
                    }
                }
                else
                {
                    formofPaymentDetails.FormOfPaymentType = MOBFormofPayment.CreditCard.ToString();
                }
            }
        }

        public void UpdateCertificateAmountInTotalPrices(List<MOBSHOPPrice> prices, List<ProdDetail> scProducts, double certificateTotalAmount, bool isShoppingCartProductsGotRefresh = false)
        {
            var certificatePrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == "CERTIFICATE");
            var scRESProduct = scProducts.Find(p => p.Code == "RES");
            //var total = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == "TOTAL");
            var grandtotal = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == "GRAND TOTAL");
            if (certificatePrice == null && certificateTotalAmount > 0)
            {
                certificatePrice = new MOBSHOPPrice();
                UpdateCertificatePrice(certificatePrice, certificateTotalAmount);
                prices.Add(certificatePrice);
            }
            else if (certificatePrice != null)
            {
                //this two lines adding certificate price back to total for removing latest certificate amount in next lines
                if (!isShoppingCartProductsGotRefresh)
                {
                    ShopStaticUtility.UpdateCertificateRedeemAmountInSCProductPrices(scRESProduct, certificatePrice.Value, false);
                }
                ShopStaticUtility.UpdateCertificateRedeemAmountFromTotalInReserationPrices(grandtotal, certificatePrice.Value, false);
                if (_configuration.GetValue<bool>("MTETCToggle"))
                {
                    UpdateCertificatePrice(certificatePrice, certificateTotalAmount);
                }
            }

            if (certificateTotalAmount == 0 && certificatePrice != null)
            {
                prices.Remove(certificatePrice);
            }

            //UpdateCertificateRedeemAmountFromTotal(total, certificateTotalAmount);
            ShopStaticUtility.UpdateCertificateRedeemAmountInSCProductPrices(scRESProduct, certificateTotalAmount);
            ShopStaticUtility.UpdateCertificateRedeemAmountFromTotalInReserationPrices(grandtotal, certificateTotalAmount);
        }

        public MOBSHOPPrice UpdateCertificatePrice(MOBSHOPPrice certificatePrice, double totalAmount)
        {
            certificatePrice.CurrencyCode = "USD";
            certificatePrice.DisplayType = "Certificate";
            certificatePrice.PriceType = "Certificate";
            certificatePrice.PriceTypeDescription = "Electronic travel certificate";
            if (_configuration.GetValue<bool>("MTETCToggle"))
            {
                certificatePrice.Value = totalAmount;
            }
            else
            {
                certificatePrice.Value += totalAmount;
            }
            certificatePrice.Value = Math.Round(certificatePrice.Value, 2, MidpointRounding.AwayFromZero);
            certificatePrice.FormattedDisplayValue = "-" + (certificatePrice.Value).ToString("C2", CultureInfo.CurrentCulture);
            certificatePrice.DisplayValue = string.Format("{0:#,0.00}", certificatePrice.Value);
            return certificatePrice;
        }

        public bool IsMilesFOPEnabled()
        {
            Boolean isMilesFOP;
            Boolean.TryParse(_configuration.GetValue<string>("EnableMilesAsPayment"), out isMilesFOP);
            return isMilesFOP;
        }

        public void AssignCertificateTravelers(MOBShoppingCart shoppingCart, FOPTravelerCertificateResponse persistedTravelCertifcateResponse, List<MOBSHOPPrice> prices, MOBApplication application)
        {
            List<MOBFOPCertificateTraveler> certTravelersCopy = null;
            if (persistedTravelCertifcateResponse?.ShoppingCart?.CertificateTravelers != null)
            {
                certTravelersCopy = persistedTravelCertifcateResponse.ShoppingCart.CertificateTravelers;
            }

            if (shoppingCart?.SCTravelers != null)
            {
                shoppingCart.CertificateTravelers = new List<MOBFOPCertificateTraveler>();
                if (shoppingCart.SCTravelers.Count > 1)
                {
                    ShopStaticUtility.AddAllTravelersOptionInCertificateTravelerList(shoppingCart);
                }
                foreach (var traveler in shoppingCart.SCTravelers)
                {
                    if (traveler.IndividualTotalAmount > 0)
                    {
                        MOBFOPCertificateTraveler certificateTraveler = new MOBFOPCertificateTraveler();
                        certificateTraveler.Name = traveler.FirstName + " " + traveler.LastName;
                        certificateTraveler.TravelerNameIndex = traveler.TravelerNameIndex;
                        certificateTraveler.PaxId = traveler.PaxID;
                        MOBFOPCertificateTraveler persistTraveler = certTravelersCopy?.Find(ct => ct.Name == traveler.FirstName + " " + traveler.LastName && traveler.PaxID == ct.PaxId);
                        if (persistTraveler != null)
                        {
                            certificateTraveler.IsCertificateApplied = persistTraveler.IsCertificateApplied;
                        }
                        else
                        {
                            certificateTraveler.IsCertificateApplied = false;
                        }
                        certificateTraveler.IndividualTotalAmount = traveler.IndividualTotalAmount;
                        shoppingCart.CertificateTravelers.Add(certificateTraveler);
                    }
                }

                if (!IsETCCombinabilityEnabled(application.Id, application.Version.Major) &&
                    persistedTravelCertifcateResponse?.ShoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates != null && shoppingCart.SCTravelers.Count > 1 &&
                    persistedTravelCertifcateResponse.ShoppingCart.FormofPaymentDetails.TravelCertificate.Certificates.Exists(c => c.CertificateTraveler != null &&
                                                                                                      !string.IsNullOrEmpty(c.CertificateTraveler.TravelerNameIndex))
                    )
                {
                    ShopStaticUtility.ClearUnmatchedCertificatesAfterEditTravelers(shoppingCart, persistedTravelCertifcateResponse, prices);
                }
            }
        }

        public InfoWarningMessages GetIBELiteNonCombinableMessage()
        {
            var message = _configuration.GetValue<string>("IBELiteNonCombinableMessage");
            return ShopStaticUtility.BuildInfoWarningMessages(message);
        }

        public bool IncludeReshopFFCResidual(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableReshopFFCResidual")
                && GeneralHelper.IsApplicationVersionGreater
                (appId, appVersion, "AndroidFFCResidualVersion", "iPhoneFFCResidualVersion", "", "", true, _configuration);
        }

        public WorkFlowType GetWorkFlowType(string flow, string productCode = "")
        {
            switch (flow)
            {
                case "CHECKIN":
                    return WorkFlowType.CheckInProductsPurchase;

                case "BOOKING":
                    return WorkFlowType.InitialBooking;

                case "VIEWRES":
                case "POSTBOOKING":
                case "VIEWRES_SEATMAP":
                    if (productCode == "RES")
                        return WorkFlowType.FareLockPurchase;
                    else if (IsPOMOffer(productCode))
                        return WorkFlowType.PreOrderMeals;
                    else
                        return WorkFlowType.PostPurchase;

                case "RESHOP":
                    return WorkFlowType.Reshop;

                case "FARELOCK":
                    return WorkFlowType.FareLockPurchase;
            }
            return WorkFlowType.UnKnown;
        }

        public bool IsPOMOffer(string productCode)
        {
            if (!_configuration.GetValue<bool>("EnableInflightMealsRefreshment")) return false;
            if (string.IsNullOrEmpty(productCode)) return false;
            return (productCode == _configuration.GetValue<string>("InflightMealProductCode"));
        }

        public bool EnableReshopMixedPTC(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidVersion_ReshopEnableMixedPTC", "iphoneVersion_ReshopEnableMixedPTC", "", "", true, _configuration);
        }

        public bool IncludeFFCResidual(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableFFCResidual")
                && GeneralHelper.IsApplicationVersionGreater
                (appId, appVersion, "AndroidFFCResidualVersion", "iPhoneFFCResidualVersion", "", "", true, _configuration);
        }

        private void AssignCertificateTravelers(MOBShoppingCart shoppingCart)
        {
            if (shoppingCart?.SCTravelers != null)
            {
                shoppingCart.CertificateTravelers = new List<MOBFOPCertificateTraveler>();

                foreach (var traveler in shoppingCart.SCTravelers)
                {
                    if (traveler.IndividualTotalAmount > 0)
                    {
                        MOBFOPCertificateTraveler certificateTraveler = new MOBFOPCertificateTraveler();
                        certificateTraveler.Name = traveler.FirstName + " " + traveler.LastName;
                        certificateTraveler.TravelerNameIndex = traveler.TravelerNameIndex;
                        certificateTraveler.PaxId = traveler.PaxID;
                        certificateTraveler.IndividualTotalAmount = traveler.IndividualTotalAmount;
                        shoppingCart.CertificateTravelers.Add(certificateTraveler);
                    }
                }
            }
        }

        public bool IncludeMoneyPlusMiles(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableMilesPlusMoney")
                && GeneralHelper.IsApplicationVersionGreater
                (appId, appVersion, "AndroidMilesPlusMoneyVersion", "iPhoneMilesPlusMoneyVersion", "", "", true, _configuration);
        }

        public async Task LoadandAddTravelCertificate(MOBShoppingCart shoppingCart, MOBSHOPReservation reservation, bool isETCCertificatesExistInShoppingCartPersist, MOBApplication application)
        {
            var persistedTravelCertifcateResponse = new FOPTravelerCertificateResponse();

            if (_configuration.GetValue<bool>("CombinebilityETCToggle") && (reservation.Prices.Exists(price => price.DisplayType.ToUpper().Trim() == "CERTIFICATE") || isETCCertificatesExistInShoppingCartPersist))
            {
                persistedTravelCertifcateResponse = await _sessionHelperService.GetSession<FOPTravelerCertificateResponse>(reservation.SessionId, persistedTravelCertifcateResponse.ObjectName, new List<string> { reservation.SessionId, persistedTravelCertifcateResponse.ObjectName }).ConfigureAwait(false);
            }

            if (persistedTravelCertifcateResponse?.ShoppingCart?.CertificateTravelers?.Count > 0)
            {
                shoppingCart.CertificateTravelers = persistedTravelCertifcateResponse.ShoppingCart.CertificateTravelers;
            }
            else if (shoppingCart.CertificateTravelers != null)
            {
                AssignCertificateTravelers(shoppingCart);
            }

            await AssignTravelerCertificateToFOP(persistedTravelCertifcateResponse, shoppingCart.Products, shoppingCart.Flow);
            var formOfPayment = shoppingCart.FormofPaymentDetails;

            MOBFormofPaymentDetails persistedFOPDetail = persistedTravelCertifcateResponse.ShoppingCart.FormofPaymentDetails;
            //Check for Shopingcart ETC values
            formOfPayment.TravelCertificate = (ConfigUtility.EnableMFOP(application.Id, application.Version.Major) && persistedFOPDetail?.TravelCertificate?.Certificates == null && formOfPayment?.TravelCertificate?.Certificates != null) ? formOfPayment?.TravelCertificate : persistedFOPDetail.TravelCertificate;
            //formOfPayment.TravelCertificate = persistedFOPDetail.TravelCertificate;
            formOfPayment.BillingAddress = formOfPayment.BillingAddress == null ? persistedFOPDetail.BillingAddress : formOfPayment.BillingAddress;
            formOfPayment.Email = formOfPayment.Email == null ? persistedFOPDetail.Email : formOfPayment.Email;
            formOfPayment.Phone = formOfPayment.Phone == null ? persistedFOPDetail.Phone : formOfPayment.Phone;
            formOfPayment.EmailAddress = formOfPayment.EmailAddress == null ? persistedFOPDetail.EmailAddress : formOfPayment.EmailAddress;

            //Add requested certificaates to TravelerCertificate object in FOP
            formOfPayment.TravelCertificate.AllowedETCAmount = GetAlowedETCAmount(shoppingCart.Products, shoppingCart.Flow);
            formOfPayment?.TravelCertificate?.Certificates?.ForEach(c => c.RedeemAmount = 0);
            ShopStaticUtility.AddRequestedCertificatesToFOPTravelerCertificates(formOfPayment.TravelCertificate.Certificates, shoppingCart.ProfileTravelerCertificates, formOfPayment.TravelCertificate);
            Reservation bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(reservation.SessionId, new Reservation().ObjectName, new List<string> { reservation.SessionId, new Reservation().ObjectName }).ConfigureAwait(false);
            ShopStaticUtility.AddGrandTotalIfNotExistInPricesAndUpdateCertificateValue(bookingPathReservation.Prices, formOfPayment);
            UpdateCertificateAmountInTotalPrices(bookingPathReservation.Prices, formOfPayment.TravelCertificate.TotalRedeemAmount);
            AssignIsOtherFOPRequired(formOfPayment, bookingPathReservation.Prices, shoppingCart.FormofPaymentDetails?.SecondaryCreditCard != null);
            await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, reservation.SessionId, new List<string> { reservation.SessionId, new Reservation().ObjectName }, new Reservation().ObjectName).ConfigureAwait(false);
            reservation.Prices = bookingPathReservation.Prices;
            persistedTravelCertifcateResponse.ShoppingCart.FormofPaymentDetails = formOfPayment;
            persistedTravelCertifcateResponse.ShoppingCart.CertificateTravelers = shoppingCart.CertificateTravelers;
            persistedTravelCertifcateResponse.ShoppingCart.FormofPaymentDetails.TravelCertificate.ReviewETCMessages = await UpdateReviewETCAlertmessages(shoppingCart);
            UpdateSavedCertificate(shoppingCart);
            persistedTravelCertifcateResponse.ShoppingCart.ProfileTravelerCertificates = shoppingCart.ProfileTravelerCertificates;
            await _sessionHelperService.SaveSession<FOPTravelerCertificateResponse>(persistedTravelCertifcateResponse, reservation.SessionId, new List<string> { reservation.SessionId, persistedTravelCertifcateResponse.ObjectName }, persistedTravelCertifcateResponse.ObjectName).ConfigureAwait(false);
            await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, reservation.SessionId, new List<string> { reservation.SessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName).ConfigureAwait(false);
        }

        public double GetAlowedETCAmount(List<ProdDetail> products, string flow)
        {
            string allowedETCAncillaryProducts = string.Empty;
            if (_configuration.GetValue<bool>("EnableEtcforSeats_PCU_Viewres") && flow == United.Utility.Enum.FlowType.VIEWRES.ToString())
            {
                allowedETCAncillaryProducts = _configuration.GetValue<string>("VIewResETCEligibleProducts");
            }
            else
            {
                allowedETCAncillaryProducts = _configuration.GetValue<string>("CombinebilityETCAppliedAncillaryCodes");
            }
            double maximumAllowedETCAmount = Convert.ToDouble(_configuration.GetValue<string>("CombinebilityMaxAmountOfETCsAllowed"));
            double allowedETCAmount = products == null ? 0 : products.Where(p => (p.Code == "RES" || allowedETCAncillaryProducts.IndexOf(p.Code) > -1) && !string.IsNullOrEmpty(p.ProdTotalPrice)).Sum(a => Convert.ToDouble(a.ProdTotalPrice));
            if (_configuration.GetValue<bool>("ETCForAllProductsToggle"))
            {
                allowedETCAmount += GetBundlesAmount(products, flow);
            }
            if (allowedETCAmount > maximumAllowedETCAmount)
            {
                allowedETCAmount = maximumAllowedETCAmount;
            }
            return allowedETCAmount;
        }


        public bool IsCorporateLeisureFareSelected(List<MOBSHOPTrip> trips)
        {
            string corporateFareText = _configuration.GetValue<string>("FSRLabelForCorporateLeisure");
            if (trips != null)
            {
                return trips.Any(
                   x =>
                       x.FlattenedFlights.Any(
                           f =>
                               f.Flights.Any(
                                   fl =>
                                       fl.CorporateFareIndicator ==
                                       corporateFareText.ToString())));
            }

            return false;
        }

        public void UpdateCertificateAmountInTotalPrices(List<MOBSHOPPrice> prices, double certificateTotalAmount)
        {
            var certificatePrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == "CERTIFICATE");
            var grandtotal = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == "GRAND TOTAL");

            if (certificatePrice == null && certificateTotalAmount > 0)
            {
                certificatePrice = new Mobile.Model.Shopping.MOBSHOPPrice();
                UpdateCertificatePrice(certificatePrice, certificateTotalAmount);
                prices.Add(certificatePrice);
            }
            else if (certificatePrice != null)
            {
                ShopStaticUtility.UpdateCertificateRedeemAmountFromTotalInReserationPrices(grandtotal, certificatePrice.Value, false);
                UpdateCertificatePrice(certificatePrice, certificateTotalAmount);
            }

            if (certificateTotalAmount == 0 && certificatePrice != null)
            {
                prices.Remove(certificatePrice);
            }

            ShopStaticUtility.UpdateCertificateRedeemAmountFromTotalInReserationPrices(grandtotal, certificateTotalAmount);
        }

        private async Task<List<MOBMobileCMSContentMessages>> UpdateReviewETCAlertmessages(MOBShoppingCart shoppingCart)
        {
            List<MOBMobileCMSContentMessages> alertMessages = new List<MOBMobileCMSContentMessages>();
            alertMessages = await AssignAlertMessages("TravelCertificate_Combinability_ReviewETCAlertMsg");
            //Show other fop required message only when isOtherFop is required
            if (shoppingCart?.FormofPaymentDetails?.IsOtherFOPRequired == false)
            {
                alertMessages.Remove(alertMessages.Find(x => x.HeadLine == "TravelCertificate_Combinability_ReviewETCAlertMsgs_OtherFopRequiredMessage"));
            }
            //Update the total price
            if (shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates != null &&
                shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates.Count > 0
                )
            {
                double balanceETCAmount = shoppingCart.FormofPaymentDetails.TravelCertificate.Certificates.Sum(x => x.NewValueAfterRedeem);
                if (balanceETCAmount > 0 && shoppingCart.FormofPaymentDetails?.TravelCertificate?.Certificates?.Count > 1)
                {
                    alertMessages.Find(x => x.HeadLine == "TravelCertificate_Combinability_ReviewETCAlertMsgs_ETCBalanceAttentionmessage").ContentFull = string.Format(alertMessages.Find(x => x.HeadLine == "TravelCertificate_Combinability_ReviewETCAlertMsgs_ETCBalanceAttentionmessage").ContentFull, String.Format("{0:0.00}", balanceETCAmount));
                }
                else
                {
                    alertMessages.Remove(alertMessages.Find(x => x.HeadLine == "TravelCertificate_Combinability_ReviewETCAlertMsgs_ETCBalanceAttentionmessage"));
                }
            }
            return alertMessages;
        }

        private async Task AssignTravelerCertificateToFOP(FOPTravelerCertificateResponse persistedTravelCertifcateResponse, List<ProdDetail> products, string flow)
        {
            if (persistedTravelCertifcateResponse == null)
            {
                persistedTravelCertifcateResponse = new FOPTravelerCertificateResponse();
            }
            persistedTravelCertifcateResponse.ShoppingCart = await InitialiseShoppingCartAndDevfaultValuesForETC(persistedTravelCertifcateResponse.ShoppingCart, products, flow);
        }

        public async Task<MOBShoppingCart> InitialiseShoppingCartAndDevfaultValuesForETC(MOBShoppingCart shoppingcart, List<ProdDetail> products, string flow)
        {
            if (shoppingcart == null)
            {
                shoppingcart = new MOBShoppingCart();
            }
            if (shoppingcart.FormofPaymentDetails == null)
            {
                shoppingcart.FormofPaymentDetails = new MOBFormofPaymentDetails();
            }
            if (shoppingcart.FormofPaymentDetails.TravelCertificate == null)
            {
                shoppingcart.FormofPaymentDetails.TravelCertificate = new MOBFOPTravelCertificate();
                shoppingcart.FormofPaymentDetails.TravelCertificate.AllowedETCAmount = GetAlowedETCAmount(shoppingcart.Products ?? products, (string.IsNullOrEmpty(shoppingcart.Flow) ? flow : shoppingcart.Flow));
                shoppingcart.FormofPaymentDetails.TravelCertificate.NotAllowedETCAmount = GetNotAlowedETCAmount(products, (string.IsNullOrEmpty(shoppingcart.Flow) ? flow : shoppingcart.Flow));
                shoppingcart.FormofPaymentDetails.TravelCertificate.MaxAmountOfETCsAllowed = Convert.ToDouble(_configuration.GetValue<string>("CombinebilityMaxAmountOfETCsAllowed"));
                shoppingcart.FormofPaymentDetails.TravelCertificate.MaxNumberOfETCsAllowed = Convert.ToInt32(_configuration.GetValue<string>("CombinebilityMaxNumberOfETCsAllowed"));
                shoppingcart.FormofPaymentDetails.TravelCertificate.ReviewETCMessages = await AssignAlertMessages("TravelCertificate_Combinability_ReviewETCAlertMsg");
                shoppingcart.FormofPaymentDetails.TravelCertificate.SavedETCMessages = await AssignAlertMessages("TravelCertificate_Combinability_SavedETCListAlertMsg");
                string removeAllCertificatesAlertMessage = _configuration.GetValue<string>("RemoveAllTravelCertificatesAlertMessage");
                shoppingcart.FormofPaymentDetails.TravelCertificate.RemoveAllCertificateAlertMessage = new Section { Text1 = removeAllCertificatesAlertMessage, Text2 = "Cancel", Text3 = "Continue" };
            }
            return shoppingcart;
        }

        private double GetNotAlowedETCAmount(List<ProdDetail> products, string flow)
        {
            return products.Sum(a => Convert.ToDouble(a.ProdTotalPrice)) - GetAlowedETCAmount(products, flow);
        }

        public double GetBundlesAmount(List<ProdDetail> products, string flow)
        {
            string nonBundleProductCode = _configuration.GetValue<string>("NonBundleProductCode");
            double bundleAmount = products == null ? 0 : products.Where(p => (nonBundleProductCode.IndexOf(p.Code) == -1) && !string.IsNullOrEmpty(p.ProdTotalPrice)).Sum(a => Convert.ToDouble(a.ProdTotalPrice));
            return bundleAmount;
        }

        private async Task<List<MOBMobileCMSContentMessages>> AssignAlertMessages(string captionKey)
        {
            List<MOBMobileCMSContentMessages> tncs = null;
            var docs = await GetCaptions(captionKey, true);
            if (docs != null && docs.Any())
            {
                tncs = new List<MOBMobileCMSContentMessages>();
                foreach (var doc in docs)
                {
                    var tnc = new MOBMobileCMSContentMessages
                    {
                        ContentFull = doc.CurrentValue,
                        HeadLine = doc.Id
                    };
                    tncs.Add(tnc);
                }
            }
            return tncs;
        }

        private async Task<List<MOBItem>> GetCaptions(string key)
        {
            return !string.IsNullOrEmpty(key) ? await GetCaptions(key, true) : null;
        }

        private async Task<List<MOBItem>> GetCaptions(List<string> keyList, bool isTnC)
        {
            DocumentLibraryDynamoDB documentLibraryDynamoDB = new DocumentLibraryDynamoDB(_configuration, _dynamoDBService);
            var docs = await documentLibraryDynamoDB.GetNewLegalDocumentsForTitles(keyList, _headers.ContextValues.SessionId).ConfigureAwait(false);
            if (docs == null || !docs.Any()) return null;

            var captions = new List<MOBItem>();

            captions.AddRange(
                docs.Select(doc => new MOBItem
                {
                    Id = doc.Title,
                    CurrentValue = doc.LegalDocument
                }));
            return captions;
        }
        private async Task<List<MOBItem>> GetCaptions(string keyList, bool isTnC)
        {

            var docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(keyList, _headers.ContextValues.TransactionId, true).ConfigureAwait(false);
            if (docs == null || !docs.Any()) return null;

            var captions = new List<MOBItem>();

            captions.AddRange(
                docs.Select(doc => new MOBItem
                {
                    Id = doc.Title,
                    CurrentValue = doc.LegalDocument
                }));
            return captions;
        }

        public List<FormofPaymentOption> BuildEligibleFormofPaymentsResponse(List<FormofPaymentOption> response, MOBShoppingCart shoppingCart, MOBRequest request)
        {
            bool isTravelCertificateAdded = shoppingCart.FormofPaymentDetails?.TravelCertificate != null && shoppingCart.FormofPaymentDetails.TravelCertificate.Certificates != null && shoppingCart.FormofPaymentDetails.TravelCertificate.Certificates.Count > 0;
            if (_configuration.GetValue<bool>("EnableEtcinManageresforPreviewTesting"))
            {
                string allowedETCAncillaryProducts = _configuration.GetValue<string>("VIewResETCEligibleProducts");
                if (shoppingCart.Products.Any(p => allowedETCAncillaryProducts.IndexOf(p.Code) > -1))
                {
                    FormofPaymentOption elgibileOption = new FormofPaymentOption();
                    elgibileOption.Category = "CERT";
                    elgibileOption.Code = "ETC";
                    elgibileOption.FoPDescription = "Travel Certificate";
                    elgibileOption.FullName = "Electronic travel certificate";
                    response.Add(elgibileOption);
                }
            }
            if (isTravelCertificateAdded)
            {
                if (shoppingCart?.FormofPaymentDetails?.TravelCertificate?.AllowedETCAmount > shoppingCart?.FormofPaymentDetails?.TravelCertificate?.TotalRedeemAmount
                    && shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates?.Count < shoppingCart?.FormofPaymentDetails.TravelCertificate?.MaxNumberOfETCsAllowed)
                {
                    response = response.Where(x => x.Category == "CC" || x.Category == "CERT").ToList();
                }
                else
                {
                    response = response.Where(x => x.Category == "CC").ToList();
                }
            }

            if (shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.ApplePay.ToString() ||
                shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.PayPal.ToString() ||
                shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.PayPalCredit.ToString() ||
                shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.Masterpass.ToString() ||
                shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.Uplift.ToString())
            {
                response = response.Where(x => x.Category != "CERT").ToList();
            }

            if (response.Exists(x => x.Category == "CERT"))
            {
                response.Where(x => x.Category == "CERT").FirstOrDefault().FullName = _configuration.GetValue<string>("ETCFopFullName");
            }

            return response;
        }

        public List<FormofPaymentOption> BuildEligibleFormofPaymentsResponse(List<FormofPaymentOption> response, MOBShoppingCart shoppingCart, Session session, MOBRequest request, bool isMetaSearch = false)
        {
            //Metasearch
            if (!_configuration.GetValue<bool>("EnableETCFopforMetaSearch") && isMetaSearch && _configuration.GetValue<bool>("CreditCardFOPOnly_MetaSearch"))
            {
                return response;
            }
            if (_configuration.GetValue<bool>("EnableFFCinBookingforPreprodTesting"))
            {
                if (!response.Exists(x => x.Category == "CERT" && x.Code == "FF"))
                {
                    FormofPaymentOption elgibileOption = new FormofPaymentOption();
                    elgibileOption.Category = "CERT";
                    elgibileOption.Code = "FF";
                    elgibileOption.FoPDescription = "Future flight credit";
                    elgibileOption.FullName = "Future flight credit";
                    response.Add(elgibileOption);
                }
            }
            bool isMultiTraveler = shoppingCart.SCTravelers?.Count > 1;
            bool isTravelCertificateAdded = shoppingCart.FormofPaymentDetails.TravelCertificate != null && shoppingCart.FormofPaymentDetails.TravelCertificate.Certificates != null && shoppingCart.FormofPaymentDetails.TravelCertificate.Certificates.Count > 0;
            bool isETCCombinabilityChangeEnabled = IsETCCombinabilityEnabled(request.Application.Id, request.Application.Version.Major);
            bool isFFCAdded = shoppingCart.FormofPaymentDetails.TravelFutureFlightCredit != null && shoppingCart.FormofPaymentDetails.TravelFutureFlightCredit.FutureFlightCredits != null && shoppingCart.FormofPaymentDetails.TravelFutureFlightCredit.FutureFlightCredits.Count > 0;
            if (IsETCEligibleTravelType(session) && !isMultiTraveler && !isETCCombinabilityChangeEnabled)//Check whether ETC Eligible booking Type
            {
                //If travel certificate is added only creditcard should be allowed
                if (isTravelCertificateAdded)
                {
                    response = response.Where(x => x.Category == "CC").ToList();
                }
            }
            else if (IsETCEligibleTravelType(session) && isMultiTraveler && !isETCCombinabilityChangeEnabled)
            {
                if (IsETCEnabledforMultiTraveler(request.Application.Id, request.Application.Version.Major.ToString()) && isTravelCertificateAdded && shoppingCart.IsMultipleTravelerEtcFeatureClientToggleEnabled)
                {
                    //Entire reservation price is covered with ETC..it doesnt matter whether Ancillary is added or not we need to show only credit card
                    if (shoppingCart.Products != null && shoppingCart.Products.Exists(x => x.Code == "RES") && Convert.ToDecimal(shoppingCart.Products?.Where(x => x.Code == "RES").FirstOrDefault().ProdTotalPrice) == 0)
                    {
                        response = response.Where(x => x.Category == "CC").ToList();
                    }//There is residual amount left on reservation and Ancillary products amount
                    else if (shoppingCart.Products != null && shoppingCart.Products.Exists(x => x.Code == "RES") && Convert.ToDecimal(shoppingCart.Products?.Where(x => x.Code == "RES").FirstOrDefault().ProdTotalPrice) > 0)
                    {
                        //If there is residual amount left on reservation but apply for all traveler option is selected it doesnt matter whether we added ancillary or not we need to show only credit card
                        if (shoppingCart.FormofPaymentDetails?.TravelCertificate?.Certificates[0]?.IsForAllTravelers == true)
                        {
                            response = response.Where(x => x.Category == "CC").ToList();
                        }
                        else
                        {
                            if (ShopStaticUtility.IsCertificatesAppliedforAllIndividualTravelers(shoppingCart))
                            {
                                response = response.Where(x => x.Category == "CC").ToList();
                            }
                            else
                            {
                                response = response.Where(x => x.Category == "CC" || x.Category == "CERT").ToList();
                            }
                        }
                    }
                }
            }
            else if (IsETCEligibleTravelType(session) && isETCCombinabilityChangeEnabled && isTravelCertificateAdded)
            {
                if (shoppingCart?.FormofPaymentDetails?.TravelCertificate?.AllowedETCAmount > shoppingCart?.FormofPaymentDetails?.TravelCertificate?.TotalRedeemAmount &&
                    (_configuration.GetValue<bool>("ETCMaxCountCheckToggle") ? shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates?.Count < shoppingCart?.FormofPaymentDetails.TravelCertificate?.MaxNumberOfETCsAllowed : true))
                {
                    response = response.Where(x => x.Category == "CC" || (x.Category == "CERT" && x.Code == "ETC")).ToList();
                }
                else
                {
                    response = response.Where(x => x.Category == "CC").ToList();
                }
            }
            else if (IsETCEligibleTravelType(session, "FFCEligibleTravelTypes") && isFFCAdded && IncludeFFCResidual(request.Application.Id, request.Application.Version.Major))
            {
                if (shoppingCart?.FormofPaymentDetails?.TravelFutureFlightCredit?.AllowedFFCAmount > shoppingCart?.FormofPaymentDetails?.TravelFutureFlightCredit?.TotalRedeemAmount)
                {
                    response = response.Where(x => x.Category == "CC" || (x.Category == "CERT" && x.Code == "FF")).ToList();
                }
                else
                {
                    response = response.Where(x => x.Category == "CC").ToList();
                }
            }
            else if ((!IsETCEligibleTravelType(session) || !IsETCEligibleTravelType(session, "FFCEligibleTravelTypes"))) //ETC Shouldn't be allowed for ineligible travel types
            {
                if (!IncludeFFCResidual(request.Application.Id, request.Application.Version.Major))
                {
                    response = response.Where(x => x.Category != "CERT").ToList();
                }
                else
                {
                    if (!IsETCEligibleTravelType(session))
                    {
                        var etcFOP = response.Where(x => x.Category == "CERT" && x.Code == "ETC").FirstOrDefault();
                        if (etcFOP != null)
                            response.Remove(etcFOP);
                    }
                    if (!IsETCEligibleTravelType(session, "FFCEligibleTravelTypes"))
                    {
                        var ffcFOP = response.Where(x => x.Category == "CERT" && x.Code == "FF").FirstOrDefault();
                        if (ffcFOP != null)
                            response.Remove(ffcFOP);
                    }
                }
            }
            if ((!_configuration.GetValue<bool>("EnableETCFopforMetaSearch") ? !isMetaSearch : true)//to enable ETC for metasearch               
                && /*IsShoppingCarthasOnlyFareLockProduct(shoppingCart)*/
                shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.ApplePay.ToString() ||
                shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.PayPal.ToString() ||
                shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.PayPalCredit.ToString() ||
                shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.Masterpass.ToString() ||
                shoppingCart?.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.Uplift.ToString() ||
                (!(IsETCEnabledforMultiTraveler(request.Application.Id, request.Application.Version.Major.ToString()) && shoppingCart.IsMultipleTravelerEtcFeatureClientToggleEnabled) ? (isMultiTraveler) : false))
            {
                response = response.Where(x => x.Category != "CERT").ToList();
            }

            if (response.Exists(x => x.Category == "CERT" && x.Code == "ETC"))
            {
                response.Where(x => x.Category == "CERT" && x.Code == "ETC").FirstOrDefault().FullName = _configuration.GetValue<string>("ETCFopFullName");
            }
            if (response.Exists(x => x.Category == "CERT" && x.Code == "FF"))
            {
                response.Where(x => x.Category == "CERT" && x.Code == "FF").FirstOrDefault().FullName = _configuration.GetValue<string>("FFCFopFullName");
            }

            return response;
        }

        public bool IsETCEnabledforMultiTraveler(int applicationId, string appVersion)
        {
            if (_configuration.GetValue<bool>("MTETCToggle") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableETCForMultiTraveler_AppVersion"), _configuration.GetValue<string>("iPhone_EnableETCForMultiTraveler_AppVersion")))
            {
                return true;
            }
            return false;
        }

        public bool IsETCEligibleTravelType(Session session, string travelTypeConfigKey = "ETCEligibleTravelTypes")
        {
            string[] travelTypes = _configuration.GetValue<string>(travelTypeConfigKey).Split('|');//"Revenue|YoungAdult"
            bool isEligible = false;
            if (session.IsAward && travelTypes.Contains("Award"))
            {
                isEligible = true;
            }
            else if (!string.IsNullOrEmpty(session.EmployeeId) && travelTypes.Contains("UADiscount"))
            {
                isEligible = true;
            }
            else if (session.IsYoungAdult && travelTypes.Contains("YoungAdult"))
            {
                isEligible = true;
            }
            else if (session.IsCorporateBooking && travelTypes.Contains("Corporate"))
            {
                isEligible = true;
            }
            else if (session.TravelType == TravelType.CLB.ToString() && travelTypes.Contains("CorporateLeisure"))
            {
                isEligible = true;
            }
            else if (!session.IsAward && string.IsNullOrEmpty(session.EmployeeId) && !session.IsYoungAdult && !session.IsCorporateBooking && session.TravelType != TravelType.CLB.ToString() && travelTypes.Contains("Revenue"))
            {
                isEligible = true;
            }
            return isEligible;
        }

        public async Task AssignBalanceAttentionInfoWarningMessage(ReservationInfo2 shopReservationInfo2, MOBFOPTravelCertificate travelCertificate)
        {
            if (shopReservationInfo2 == null)
            {
                shopReservationInfo2 = new ReservationInfo2();
            }
            //To show balance attention message on RTI when Combinability is ON from Shoppingcart service  and OFF from MRest
            if (shopReservationInfo2.InfoWarningMessages == null)
            {
                shopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
            }
            InfoWarningMessages balanceAttentionMessage = new InfoWarningMessages();
            balanceAttentionMessage = await GetETCBalanceAttentionInfoWarningMessage(travelCertificate);
            if (shopReservationInfo2.InfoWarningMessages.Exists(x => x.Order == MOBINFOWARNINGMESSAGEORDER.RTIETCBALANCEATTENTION.ToString()))
            {
                shopReservationInfo2.InfoWarningMessages.Remove(shopReservationInfo2.InfoWarningMessages.Find(x => x.Order == MOBINFOWARNINGMESSAGEORDER.RTIETCBALANCEATTENTION.ToString()));
            }
            if (balanceAttentionMessage != null)
            {
                shopReservationInfo2.InfoWarningMessages.Add(balanceAttentionMessage);
                shopReservationInfo2.InfoWarningMessages = shopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
            }
        }

        private async Task<InfoWarningMessages> GetETCBalanceAttentionInfoWarningMessage(MOBFOPTravelCertificate travelCertificate)
        {
            InfoWarningMessages infoMessage = null;
            double? etcBalanceAttentionAmount = travelCertificate?.Certificates?.Sum(c => c.NewValueAfterRedeem);
            if (etcBalanceAttentionAmount > 0 && travelCertificate?.Certificates?.Count > 1)
            {
                List<MOBMobileCMSContentMessages> alertMessages = new List<MOBMobileCMSContentMessages>();
                alertMessages = await AssignAlertMessages("TravelCertificate_Combinability_ReviewETCAlertMsg");
                infoMessage = new InfoWarningMessages();
                infoMessage.Order = MOBINFOWARNINGMESSAGEORDER.RTIETCBALANCEATTENTION.ToString();
                infoMessage.IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString();
                infoMessage.Messages = new List<string>();
                infoMessage.Messages.Add(string.Format(alertMessages.Find(x => x.HeadLine == "TravelCertificate_Combinability_ReviewETCAlertMsgs_ETCBalanceAttentionmessage").ContentFull, String.Format("{0:0.00}", etcBalanceAttentionAmount)));
            }

            return infoMessage;
        }

        public Collection<FOPProduct> GetProductsForEligibleFopRequest(MOBShoppingCart shoppingCart, SeatChangeState state = null)
        {
            if (shoppingCart == null || shoppingCart.Products == null || !shoppingCart.Products.Any())
                return null;

            var products = shoppingCart.Products.GroupBy(k => new { k.Code, k.ProdDescription }).Select(x => new FOPProduct { Code = x.Key.Code, ProductDescription = x.Key.ProdDescription }).ToCollection();
            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("EnablePCUSelectedSeatPurchaseViewRes")))
            {
                if (!_configuration.GetValue<bool>("ByPassAddingPCUProductToEligibleFopRequest"))
                {
                    ShopStaticUtility.AddPCUToRequestWhenPCUSeatIsSelected(state, ref products);
                }
            }

            return products;
        }

        private void AddPCUToRequestWhenPCUSeatIsSelected(SeatChangeState state, ref Collection<FOPProduct> products)
        {
            if (state != null && state.BookingTravelerInfo.Any(t => t.Seats.Any(s => !string.IsNullOrEmpty(s.PcuOfferOptionId) && s.PriceAfterTravelerCompanionRules > 0)))
            {
                if (!products.Any(p => p.Code == "PCU"))
                {
                    products.Add(new FOPProduct { Code = "PCU", ProductDescription = "Premium Cabin Upsell" });
                }
            }
        }

        public bool IsEligibileForUplift(MOBSHOPReservation reservation, MOBShoppingCart shoppingCart)
        {
            if (shoppingCart?.Flow?.ToUpper() == United.Utility.Enum.FlowType.VIEWRES.ToString().ToUpper())
            {
                return HasEligibleProductsForUplift(shoppingCart.TotalPrice, shoppingCart.Products);
            }

            if (!_configuration.GetValue<bool>("EnableUpliftPayment"))
                return false;

            if (reservation == null || reservation.Prices == null || shoppingCart == null || shoppingCart?.Flow != United.Utility.Enum.FlowType.BOOKING.ToString())
                return false;

            if (reservation.ShopReservationInfo?.IsCorporateBooking ?? false)
                return false;

            if (shoppingCart.Products?.Any(p => p?.Code == "FLK") ?? false)
                return false;

            if (!_configuration.GetValue<bool>("DisableFixForUpliftFareLockDefect"))
            {
                if (shoppingCart.Products?.Any(p => p?.Code?.ToUpper() == "FARELOCK") ?? false)
                    return false;
            }

            if (reservation.Prices.Any(p => "TOTALPRICEFORUPLIFT".Equals(p.DisplayType, StringComparison.CurrentCultureIgnoreCase) && p.Value >= MinimumPriceForUplift && p.Value <= MaxmimumPriceForUplift) &&
               (shoppingCart?.SCTravelers?.Any(t => t?.TravelerTypeCode == "ADT" || t?.TravelerTypeCode == "SNR") ?? false))
            {
                return true;
            }
            return false;
        }

        public bool HasEligibleProductsForUplift(string totalPrice, List<ProdDetail> products)
        {
            decimal.TryParse(totalPrice, out decimal price);
            if (price >= MinimumPriceForUplift && price <= MaxmimumPriceForUplift)
            {
                var eligibleProductsForUplift = _configuration.GetValue<string>("EligibleProductsForUpliftInViewRes").Split(',');
                if (eligibleProductsForUplift.Any())
                {
                    return products.Any(p => eligibleProductsForUplift.Contains(p.Code));
                }
            }

            return false;
        }

        public int MinimumPriceForUplift
        {
            get
            {
                var minimumAmountForUplift = _configuration.GetValue<string>("MinimumPriceForUplift");
                if (string.IsNullOrEmpty(minimumAmountForUplift))
                    return 300;

                int.TryParse(minimumAmountForUplift, out int upliftMinAmount);
                return upliftMinAmount;
            }
        }

        public int MaxmimumPriceForUplift
        {
            get
            {
                var maximumAmountForUplift = _configuration.GetValue<string>("MaximumPriceForUplift");
                if (string.IsNullOrEmpty(maximumAmountForUplift))
                    return 150000;

                int.TryParse(maximumAmountForUplift, out int upliftMaxAmount);
                return upliftMaxAmount;
            }
        }

        public string GetGMTTime(string localTime, string airportCode)
        {
            string gmtTime = localTime;

            DateTime dateTime = new DateTime(0);
            if (DateTime.TryParse(localTime, out dateTime) && airportCode != null && airportCode.Trim().Length == 3)
            {
                //try
                //{
                //    Database database = DatabaseFactory.CreateDatabase("ConnectionString - GMTConversion");
                //    DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("sel_GMT_STD_DST_Dates");
                //    database.AddInParameter(dbCommand, "@InputYear", DbType.Int32, dateTime.Year);
                //    database.AddInParameter(dbCommand, "@StationCode", DbType.String, airportCode.Trim().ToUpper());
                //    database.AddInParameter(dbCommand, "@CarrierCode", DbType.String, "CO");

                //    IDataReader dataReader = null;
                //    using (dataReader = database.ExecuteReader(dbCommand))
                //    {
                //        while (dataReader.Read())
                //        {
                //            dateTime1 = Convert.ToInt64(dataReader["DateTime_1"]);
                //            dateTime2 = Convert.ToInt64(dataReader["DateTime_2"]);
                //            dateTime3 = Convert.ToInt64(dataReader["DateTime_3"]);
                //        }
                //    }

                //    long time = Convert.ToInt64(dateTime.Year.ToString() + dateTime.Month.ToString("00") + dateTime.Day.ToString("00") + dateTime.Hour.ToString("00") + dateTime.Minute.ToString("00"));
                //    bool dayLightSavingTime = false;
                //    if (time >= dateTime2 && time <= dateTime3)
                //    {
                //        dayLightSavingTime = true;
                //    }

                //    int offsetMunite = 0;
                //    database = DatabaseFactory.CreateDatabase("ConnectionString - GMTConversion");
                //    dbCommand = (DbCommand)database.GetStoredProcCommand("sp_GMT_City");

                //    database.AddInParameter(dbCommand, "@StationCode", DbType.String, airportCode.Trim().ToUpper());
                //    database.AddInParameter(dbCommand, "@Carrier", DbType.String, "CO");

                //    using (dataReader = database.ExecuteReader(dbCommand))
                //    {
                //        while (dataReader.Read())
                //        {
                //            if (dayLightSavingTime)
                //            {
                //                offsetMunite = Convert.ToInt32(dataReader["DaySavTime"]);
                //            }
                //            else
                //            {
                //                offsetMunite = Convert.ToInt32(dataReader["StandardTime"]);
                //            }
                //        }
                //    }

                //    dateTime = dateTime.AddMinutes(-offsetMunite);

                //    gmtTime = dateTime.ToString("MM/dd/yyyy hh:mm tt");

                //}
                //catch (System.Exception) { }
            }

            return gmtTime;
        }

        public bool IncludeMOBILE12570ResidualFix(int appId, string appVersion)
        {
            bool isApplicationGreater = GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidMOBILE12570ResidualVersion", "iPhoneMOBILE12570ResidualVersion", "", "", true, _configuration);
            return (_configuration.GetValue<bool>("eableMOBILE12570Toggle") && isApplicationGreater);
        }


        public bool IsManageResETCEnabled(int applicationId, string appVersion)
        {
            if (_configuration.GetValue<bool>("EnableEtcforSeats_PCU_Viewres") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableETCManageRes_AppVersion"), _configuration.GetValue<string>("iPhone_EnableETCManageRes_AppVersion")))
            {
                return true;
            }
            return false;
        }

        public void UpdateSavedCertificate(MOBShoppingCart shoppingcart)
        {
            if (_configuration.GetValue<bool>("SavedETCToggle") && shoppingcart != null)
            {
                var shoppingCaartCertificates = shoppingcart.ProfileTravelerCertificates;
                var appliedCertificates = shoppingcart.FormofPaymentDetails?.TravelCertificate?.Certificates;
                if (shoppingCaartCertificates?.Count > 0 && appliedCertificates != null)
                {
                    foreach (var shoppingCaartCertificate in shoppingCaartCertificates)
                    {
                        var appliedCertificate = appliedCertificates.Exists(c => c.Index == shoppingCaartCertificate.Index);
                        shoppingCaartCertificate.IsCertificateApplied = appliedCertificate;
                        if (appliedCertificate)
                        {
                            appliedCertificates.Find(c => c.Index == shoppingCaartCertificate.Index).IsProfileCertificate = appliedCertificate;
                        }
                    }
                }
            }
        }
        //public  List<MOBItem> GetCaptions(string key)
        //{
        //    //return !string.IsNullOrEmpty(key) ? GetCaptions(new List<string> { key }, true) : null;
        //}

        //public  List<MOBItem> GetCaptions(List<string> keyList, bool isTnC)
        //{
        //    var docs = GetNewLegalDocumentsForTitles(keyList, isTnC);
        //    if (docs == null || !docs.Any()) return null;

        //    var captions = new List<MOBItem>();

        //    captions.AddRange(
        //        docs.Select(doc => new MOBItem
        //        {
        //            Id = doc.Title,
        //            CurrentValue = doc.Document
        //        }));
        //    return captions;
        //}

        #endregion
        public string BuilTripShareEmailBodyTripText(string tripType, List<MOBSHOPTrip> trips, bool isHtml)
        {
            string emailBodyTripText = string.Empty;
            string originCityOnly, destinationCityOnly;


            if (string.IsNullOrEmpty(trips[0].OriginDecodedWithCountry) || string.IsNullOrEmpty(trips[0].DestinationDecodedWithCountry))
            {
                originCityOnly = trips[0].OriginDecoded.Split(',')[0].Trim();
                destinationCityOnly = trips[0].DestinationDecoded.Split(',')[0].Trim();
            }
            else
            {
                originCityOnly = trips[0].OriginDecodedWithCountry.Split(',')[0].Trim();
                destinationCityOnly = trips[0].DestinationDecodedWithCountry.Split(',')[0].Trim();
            }

            if (tripType == "OW")
            {
                if (isHtml)
                {
                    emailBodyTripText = _configuration.GetValue<string>("ShareTripInSoftRTIEmailBodyTripText");
                }
                else
                {
                    emailBodyTripText = _configuration.GetValue<string>("ShareTripInSoftRTIPlaceholderTitleText");
                }
                emailBodyTripText = emailBodyTripText.Replace("{tripType}", "One-way");
                emailBodyTripText = emailBodyTripText.Replace("{originWithStateCode}", originCityOnly);
                emailBodyTripText = emailBodyTripText.Replace("{destinationWithStateCode}", destinationCityOnly);
            }
            else if (tripType == "RT")
            {
                if (isHtml)
                {
                    emailBodyTripText = _configuration.GetValue<string>("ShareTripInSoftRTIEmailBodyTripText");
                }
                else
                {
                    emailBodyTripText = _configuration.GetValue<string>("ShareTripInSoftRTIPlaceholderTitleText");
                }
                emailBodyTripText = emailBodyTripText.Replace("{tripType}", "Roundtrip");
                emailBodyTripText = emailBodyTripText.Replace("{originWithStateCode}", originCityOnly);
                emailBodyTripText = emailBodyTripText.Replace("{destinationWithStateCode}", destinationCityOnly);
            }
            else if (tripType == "MD")
            {
                if (isHtml)
                {
                    emailBodyTripText = _configuration.GetValue<string>("ShareTripInSoftRTIEmailBodyTripMultiSegmentText");
                }
                else
                {
                    emailBodyTripText = _configuration.GetValue<string>("ShareTripInSoftRTIEmailBodyTripMultiSegmentTextNonHtml");
                }

                string numberOfFlights = $"{trips.Count - 1}";

                emailBodyTripText = emailBodyTripText.Replace("{originWithStateCode}", originCityOnly);
                emailBodyTripText = emailBodyTripText.Replace("{destinationWithStateCode}", destinationCityOnly);
                emailBodyTripText = emailBodyTripText.Replace("{numberOfFlights}", numberOfFlights);

            }
            return emailBodyTripText;
        }
        public void AddPromoDetailsInSegments(ProdDetail prodDetail)
        {
            if (prodDetail?.Segments != null)
            {
                double promoValue;
                prodDetail?.Segments.ForEach(p =>
                {
                    p.SubSegmentDetails.ForEach(subSegment =>
                    {
                        if (!string.IsNullOrEmpty(subSegment.OrginalPrice) && !string.IsNullOrEmpty(subSegment.Price))
                        {
                            promoValue = Convert.ToDouble(subSegment.OrginalPrice) - Convert.ToDouble(subSegment.Price);
                            subSegment.Price = subSegment.OrginalPrice;
                            subSegment.DisplayPrice = Decimal.Parse(subSegment.Price).ToString("c");
                            if (promoValue > 0)
                            {
                                subSegment.PromoDetails = new MOBPromoCode
                                {
                                    PriceTypeDescription = _configuration.GetValue<string>("PromoCodeAppliedText"),
                                    PromoValue = Math.Round(promoValue, 2, MidpointRounding.AwayFromZero),
                                    FormattedPromoDisplayValue = "-" + promoValue.ToString("C2", CultureInfo.CurrentCulture)
                                };
                            }
                        }
                    });

                });
            }
        }
        public string BuildTripSharePrice(string priceWithCurrency, string currencyCode, string redirectUrl)
        {
            string emailBodyBodyPriceText = _configuration.GetValue<string>("ShareTripInSoftRTIEmailBodyPriceText");
            emailBodyBodyPriceText = emailBodyBodyPriceText.Replace("{serverCurrentDateTime}", DateTime.Now.ToString("MMM d 'at' h:mm tt"));
            emailBodyBodyPriceText = emailBodyBodyPriceText.Replace("{priceWithCurrency}", priceWithCurrency);
            emailBodyBodyPriceText = emailBodyBodyPriceText.Replace("{currencyCode}", currencyCode);
            emailBodyBodyPriceText = emailBodyBodyPriceText.Replace("{redirectUrl}", redirectUrl);
            return emailBodyBodyPriceText;
        }
        public string BuildTripShareSegmentText(MOBSHOPTrip trip)
        {
            string bodyEmailSegmentCompleteText = string.Empty;

            string emailBodySegmentText = string.Empty;
            string emailBodySegmentConnectionText = string.Empty;

            string segmentDuration = string.Empty;
            string departureTime = string.Empty;
            string arrivalTime = string.Empty;

            string departureAirportWithCountryCode = string.Empty;
            string arrivalAirportWithCountryCode = string.Empty;

            //example 1h 7m connection in Chicago, IL, US (ORD - O'Hare)
            string connectionWithStateAirportCodeAndName = string.Empty;
            string emailBodySegmentOperatedByText = string.Empty;

            //string connectionDuration = string.Empty;
            string operatedByText = string.Empty;

            foreach (var flattenedFlight in trip.FlattenedFlights)
            {
                if (flattenedFlight != null)
                {
                    foreach (var flight in flattenedFlight.Flights)
                    {
                        if (!flight.IsConnection && !string.IsNullOrEmpty(flight.TotalTravelTime))
                        {
                            segmentDuration = flight.TotalTravelTime;
                        }

                        if (flight.IsConnection)
                        {
                            //connectionWithStateAirportCodeAndName = $"{flight.ConnectTimeMinutes} connection in {flight.OriginDescription}";
                            connectionWithStateAirportCodeAndName = $"{flight.ConnectTimeMinutes} connection in {flight.OriginDecodedWithCountry}";
                            emailBodySegmentConnectionText += _configuration.GetValue<string>("ShareTripInSoftRTIEmailBodySegmentConnectionText");

                            emailBodySegmentConnectionText = emailBodySegmentConnectionText.Replace("{connectionDurationAndWithStateAirportCode}", connectionWithStateAirportCodeAndName);
                        }
                        else if (flight.IsStopOver)
                        {
                            //connectionWithStateAirportCodeAndName = $"{flight.GroundTime} connection in {flight.OriginDescription}";
                            connectionWithStateAirportCodeAndName = $"{flight.GroundTime} connection in {flight.OriginDecodedWithCountry}";
                            emailBodySegmentConnectionText += _configuration.GetValue<string>("ShareTripInSoftRTIEmailBodySegmentConnectionText");

                            emailBodySegmentConnectionText = emailBodySegmentConnectionText.Replace("{connectionDurationAndWithStateAirportCode}", connectionWithStateAirportCodeAndName);
                        }

                        //if (string.IsNullOrEmpty(operatedByText) &&  !string.IsNullOrEmpty(flight.OperatingCarrierDescription))
                        if (!string.IsNullOrEmpty(flight.OperatingCarrierDescription))
                        {
                            if (string.IsNullOrEmpty(operatedByText))
                            {
                                operatedByText = flight.OperatingCarrierDescription;
                            }
                            else
                            {
                                operatedByText = $"{operatedByText}, {flight.OperatingCarrierDescription}";
                            }
                        }
                    }
                    if (flattenedFlight.Flights != null && flattenedFlight.Flights.Count > 0)
                    {
                        departureTime = flattenedFlight.Flights[0].DepartureDateTime;
                        arrivalTime = flattenedFlight.Flights[flattenedFlight.Flights.Count - 1].ArrivalDateTime;
                        departureAirportWithCountryCode = flattenedFlight.Flights.FirstOrDefault().OriginDecodedWithCountry;
                        arrivalAirportWithCountryCode = flattenedFlight.Flights.Last().DestinationDecodedWithCountry;
                    }
                }
            }

            emailBodySegmentText = _configuration.GetValue<string>("ShareTripInSoftRTIEmailBodySegmentText");
            emailBodySegmentText = emailBodySegmentText.Replace("{segmentDuration}", segmentDuration);
            //emailBodySegmentText = emailBodySegmentText.Replace("{originWithStateAirportCode}", trip.OriginDecodedWithCountry);
            //emailBodySegmentText = emailBodySegmentText.Replace("{destinationWithStateAirportCode}", trip.DestinationDecodedWithCountry);
            emailBodySegmentText = emailBodySegmentText.Replace("{originWithStateAirportCode}", departureAirportWithCountryCode);
            emailBodySegmentText = emailBodySegmentText.Replace("{destinationWithStateAirportCode}", arrivalAirportWithCountryCode);

            emailBodySegmentText = emailBodySegmentText.Replace("{departureTime}", DateTime.Parse(departureTime).ToString("ddd, MMM dd, yyyy, h:mm tt"));
            emailBodySegmentText = emailBodySegmentText.Replace("{arrivalTime}", DateTime.Parse(arrivalTime).ToString("ddd, MMM dd, yyyy, h:mm tt"));

            if (!string.IsNullOrEmpty(operatedByText))
            {
                emailBodySegmentOperatedByText = _configuration.GetValue<string>("ShareTripInSoftRTIEmailBodySegmentOperatedByText");
                emailBodySegmentOperatedByText = emailBodySegmentOperatedByText.Replace("{OperatingCarrierName}", operatedByText);
            }

            bodyEmailSegmentCompleteText = $"{emailBodySegmentText}{emailBodySegmentConnectionText}{emailBodySegmentOperatedByText}";

            return bodyEmailSegmentCompleteText;
        }

        public async Task<List<ProdDetail>> ConfirmationPageProductInfo(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse
            flightReservationResponse, bool isPost, MOBApplication application, SeatChangeState state = null, string flow = "VIEWRES",
            string sessionId = "")
        {
            List<ProdDetail> prodDetails = new List<ProdDetail>();
            List<string> productCodes = new List<string>();

            bool isFareLockCompletePurchase = _configuration.GetValue<bool>("EnableFareLockPurchaseViewRes") && flightReservationResponse.Reservation.Characteristic.Any(o => (o.Code != null && o.Value != null && o.Code.Equals("FARELOCK") && o.Value.Equals("TRUE"))) &&
                flightReservationResponse.Reservation.Characteristic.Any(o => (o.Code != null && o.Code.Equals("FARELOCK_DATE")));

            if (isFareLockCompletePurchase)
            {
                var displayTotalPrice = flightReservationResponse.DisplayCart.DisplayPrices.FirstOrDefault(o => (o.Description != null && o.Description.Equals("Total", StringComparison.OrdinalIgnoreCase))).Amount;
                var grandTotal = ShopStaticUtility.GetGrandTotalPriceFareLockShoppingCart(flightReservationResponse);
                var prodDetail = new ProdDetail()
                {
                    Code = "FLK_VIEWRES",
                    ProdDescription = string.Empty,
                    ProdTotalPrice = String.Format("{0:0.00}", grandTotal),
                    ProdDisplayTotalPrice = grandTotal.ToString("c"),
                    Segments = new List<ProductSegmentDetail> {
                                     new ProductSegmentDetail {
                                                        SegmentInfo = "",
                                                        SubSegmentDetails = new List<ProductSubSegmentDetail>
                                                                            {
                                                                                new ProductSubSegmentDetail
                                                                                {
                                                                                    Price = String.Format("{0:0.00}", displayTotalPrice),
                                                                                    DisplayPrice = displayTotalPrice.ToString("c", new CultureInfo("en-us")),
                                                                                    Passenger = ShopStaticUtility.GetFareLockPassengerDescription(flightReservationResponse.Reservation),
                                                                                    SegmentDescription = ShopStaticUtility.GetFareLockSegmentDescription(flightReservationResponse.Reservation)
                                                                               }
                                                                            }
                                                                    }
                    },
                };
                prodDetails.Add(prodDetail);
            }
            else
            {
                productCodes = ShopStaticUtility.GetProductCodes(flightReservationResponse, flow, isPost);
                productCodes = ShopStaticUtility.OrderProducts(productCodes);

                //Added this line to replace the ProductCode for FareLock
                int index = productCodes.FindIndex(ind => ind.Equals("FLK"));
                if (index != -1)
                    productCodes[index] = "FareLock";
                foreach (string productCode in productCodes)
                {
                    ProdDetail prodDetail;
                    United.Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart;

                    switch (productCode?.ToUpper()?.Trim())
                    {
                        case "SEATASSIGNMENTS":
                            if (flow == FlowType.BOOKING.ToString())
                            {
                                prodDetail = BuildProdDetailsForSeats(flightReservationResponse, state, flow, application);
                            }
                            else
                            {
                                prodDetail = BuildProdDetailsForSeats(flightReservationResponse, state, isPost, flow);
                            }

                            if (prodDetail != null && ((!string.IsNullOrEmpty(prodDetail.ProdDisplayTotalPrice) || !string.IsNullOrEmpty(prodDetail.ProdDisplayOtherPrice)) || IsFreeSeatCouponApplied(prodDetail, flightReservationResponse)))
                            {
                                prodDetails.Add(prodDetail);
                            }
                            break;
                        case "RES":
                            flightReservationResponseShoppingCart = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart : flightReservationResponse.ShoppingCart;
                            prodDetail = new ProdDetail()
                            {
                                Code = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Code).FirstOrDefault().ToString(),
                                ProdDescription = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Description).FirstOrDefault().ToString(),
                                ProdTotalPrice = String.Format("{0:0.00}", ShopStaticUtility.GetTotalPriceForRESProduct(isPost, flightReservationResponseShoppingCart, flow)),
                                ProdDisplayTotalPrice = Decimal.Parse(ShopStaticUtility.GetTotalPriceForRESProduct(isPost, flightReservationResponseShoppingCart, flow).ToString().Trim()).ToString("c", new CultureInfo("en-us"))
                            };
                            prodDetails.Add(prodDetail);
                            break;
                        case "RBF":
                            flightReservationResponseShoppingCart = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart : flightReservationResponse.ShoppingCart;
                            prodDetail = new ProdDetail()
                            {
                                Code = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Code).FirstOrDefault().ToString(),
                                ProdDescription = flightReservationResponseShoppingCart.Items.SelectMany(d => d.Product).Where(d => d.Code == productCode).Select(d => d.Description).FirstOrDefault().ToString(),
                                ProdTotalPrice = String.Format("{0:0.00}", ShopStaticUtility.GetCloseBookingFee(isPost, flightReservationResponseShoppingCart, flow)),
                                ProdDisplayTotalPrice = Decimal.Parse(ShopStaticUtility.GetTotalPriceForRESProduct(isPost, flightReservationResponseShoppingCart, flow).ToString().Trim()).ToString("c", new CultureInfo("en-us"))
                            };
                            prodDetails.Add(prodDetail);
                            break;
                        case "BAG":
                            if (_configuration.GetValue<bool>("EnableCouponMVP2Changes") && flow == FlowType.BOOKING.ToString())//SC is adding a bag product if free bag coupon is applied..Adding default values
                            {
                                prodDetail = new ProdDetail()
                                {
                                    Code = "BAG",
                                    ProdDescription = string.Empty,
                                    ProdTotalPrice = "0",
                                    ProdDisplayTotalPrice = "0"
                                };
                                prodDetails.Add(prodDetail);
                            }
                            break;
                        case "POM":
                            prodDetails = await BuildProductDetailsForInflightMeals(flightReservationResponse, productCode, sessionId, isPost);
                            break;
                        default:

                            List<string> refundedSegmentNums = null;
                            var travelOptions = ShopStaticUtility.GetTravelOptionItems(flightReservationResponse, productCode);
                            bool isBundleProduct = string.Equals(travelOptions?.FirstOrDefault(t => t.Key == productCode)?.Type, "BE", StringComparison.OrdinalIgnoreCase);
                            if (travelOptions == null || !travelOptions.Any())
                                continue;

                            //ModifyReservationFailed
                            if (flightReservationResponse?.Errors?.Any(e => e?.MinorCode == "90506") ?? false)
                            {
                                bool DisableFixForPCUPurchaseFailMsg_MOBILE15837 = _configuration.GetValue<bool>("DisableFixForPCUPurchaseFailMsg_MOBILE15837");
                                if (!ShopStaticUtility.IsRefundSuccess(flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items, out refundedSegmentNums, DisableFixForPCUPurchaseFailMsg_MOBILE15837))
                                {
                                    continue;
                                }
                            }

                            prodDetail = new ProdDetail()
                            {
                                Code = travelOptions.Where(d => d.Key == productCode).Select(d => d.Key).FirstOrDefault().ToString(),
                                ProdDescription = productCode == "TPI" && _configuration.GetValue<bool>("GetTPIProductName_HardCode") ? "Travel insurance" : travelOptions.Where(d => d.Key == productCode).Select(d => d.Type).FirstOrDefault().ToString(),
                                ProdTotalPrice = String.Format("{0:0.00}", travelOptions.Sum(d => d.Amount)),
                                ProdOriginalPrice = String.Format("{0:0.00}", travelOptions.Select(d => d.OriginalPrice).Sum()),
                                ProdDisplayTotalPrice = Decimal.Parse(travelOptions.Sum(d => d.Amount).ToString().Trim()).ToString("c", new CultureInfo("en-us")),
                                Segments = travelOptions
                                .Where(d => d.Key == productCode)
                                .SelectMany(x => x.SubItems)
                                .Where(x => ShopStaticUtility.ShouldIgnoreAmount(x) ? true : x.Amount != 0 || (!_configuration.GetValue<bool>("DisableFreeCouponFix") && x.OriginalPrice != 0))
                                .OrderBy(x => x.SegmentNumber)
                                .GroupBy(x => x.SegmentNumber).ToList()
                                .Select(x => new ProductSegmentDetail
                                {
                                    SegmentInfo = BuildSegmentInfo(productCode, flightReservationResponse.Reservation.FlightSegments, x),
                                    ProductId = string.Join(",", x.Select(u => u.Value).ToList()),
                                    TripId = string.Join(",", x.Select(u => u.TripIndex).ToList()),
                                    SegmentId = string.Join(",", x.Select(u => u.SegmentNumber).Distinct().ToList()),
                                    ProductIds = x.Select(u => u.Key).ToList(),
                                    SubSegmentDetails = x.GroupBy(f => f.SegmentNumber).Select(t => new ProductSubSegmentDetail
                                    {
                                        SegmentInfo = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) && productCode != "TPI" ? BuildSegmentInfo(productCode, flightReservationResponse.Reservation.FlightSegments, x) : string.Empty,
                                        Price = String.Format("{0:0.00}", t.Sum(i => i.Amount)),
                                        DisplayPrice = Decimal.Parse(t.Sum(i => i.Amount).ToString()).ToString("c"),
                                        OrginalPrice = EnablePromoCodeForAncillaryProductsManageRes() ? String.Format("{0:0.00}", t.Select(i => i.OriginalPrice).Sum()) : string.Empty,
                                        DisplayOriginalPrice = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? ShopStaticUtility.GetDisplayOriginalPrice(t.Select(i => i.Amount).Sum(), t.Select(i => i.OriginalPrice).Sum()) : string.Empty,
                                        Passenger = x.Count().ToString() + (x.Count() > 1 ? " Travelers" : " Traveler"),
                                        SegmentDescription = BuildProductDescription(travelOptions, t, productCode),
                                        IsPurchaseFailure = ShopStaticUtility.IsPurchaseFailed(productCode == "PCU", t.Select(sb => sb.SegmentNumber).FirstOrDefault(), refundedSegmentNums),
                                        //Miles = Convert.ToInt32(_configuration.GetValue<string>("milesFOP"]),
                                        //DisplayMiles = Utility.ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"], false)
                                        ProdDetailDescription = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? GetProductDetailDescrption(t, productCode, sessionId, isBundleProduct).Result : null,
                                        ProductDescription = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? ShopStaticUtility.GetProductDescription(travelOptions, productCode) : string.Empty
                                    }).ToList()
                                }).ToList(),
                                ProdTotalMiles = 0,
                                ProdDisplayTotalMiles = string.Empty
                            };
                            // MOBILE-25395: SAF
                            // Fix the product descriptions and tripID for SAF bundle
                            var safCode = _configuration.GetValue<string>("SAFCode");
                            if (productCode?.ToUpper()?.Trim() == safCode)
                            {
                                prodDetail?.Segments?.ForEach(segment =>
                                {
                                    segment.SubSegmentDetails?.ForEach(subSegment => subSegment.ProductDescription = subSegment.ProdDetailDescription?.FirstOrDefault());
                                    var safProdDetail = flightReservationResponse.ShoppingCart?.Items?.FirstOrDefault(item => item.Product?.Any(product => string.Equals(product.Code, safCode, StringComparison.OrdinalIgnoreCase)) ?? false);
                                    if (safProdDetail != null)
                                    {
                                        segment.TripId = string.Join(",", safProdDetail.Product?.FirstOrDefault(product => string.Equals(product.Code, safCode, StringComparison.OrdinalIgnoreCase))?.SubProducts?.Select(sp => sp.ID)?.ToList());
                                    }
                                });
                            }
                            if (!ShopStaticUtility.IsCheckinFlow(flow))
                            {
                                if (productCode != "FareLock")
                                    ShopStaticUtility.UpdateRefundTotal(prodDetail);
                                else
                                    prodDetail.Code = "FLK";
                            }
                            if (prodDetail != null && (!string.IsNullOrEmpty(prodDetail.ProdDisplayTotalPrice) || !string.IsNullOrEmpty(prodDetail.ProdDisplayOtherPrice) || IsOriginalPriceExists(prodDetail)))
                            {
                                prodDetails.Add(prodDetail);
                            }
                            break;
                    }
                }
                if ((_configuration.GetValue<bool>("EnableCouponsforBooking") && flow == FlowType.BOOKING.ToString() && prodDetails != null)
                      || (_configuration.GetValue<bool>("EnableCouponsInPostBooking") && flow == FlowType.POSTBOOKING.ToString()) || (_configuration.GetValue<bool>("IsEnableManageResCoupon") && (flow == FlowType.VIEWRES.ToString() || flow == FlowType.VIEWRES_SEATMAP.ToString())))
                {
                    AddCouponDetails(prodDetails, flightReservationResponse, isPost, flow, application);
                }

            }
            return prodDetails;
        }

        public async Task<List<string>> GetProductDetailDescrption(IGrouping<String, SubItem> subItem, string productCode, String sessionId, bool isBundleProduct)
        {
            List<string> prodDetailDescription = new List<string>();
            // MOBILE-25395: SAF
            var safCode = _configuration.GetValue<string>("SAFCode");

            if (string.Equals(productCode, "EFS", StringComparison.OrdinalIgnoreCase))
            {
                prodDetailDescription.Add("Included with your fare");
            }

            // MOBILE-25395: SAF
            if ((isBundleProduct || string.Equals(productCode, safCode, StringComparison.OrdinalIgnoreCase)) && !string.IsNullOrEmpty(sessionId))
            {
                var bundleResponse = new MOBBookingBundlesResponse(_configuration);
                bundleResponse = await _sessionHelperService.GetSession<MOBBookingBundlesResponse>(sessionId, bundleResponse.ObjectName, new List<string> { sessionId, bundleResponse.ObjectName }).ConfigureAwait(false);
                if (bundleResponse != null)
                {
                    var selectedBundleResponse = bundleResponse.Products?.FirstOrDefault(p => string.Equals(p.ProductCode, productCode, StringComparison.OrdinalIgnoreCase));
                    if (selectedBundleResponse != null)
                    {
                        // MOBILE-25395: SAF
                        if (string.Equals(productCode, safCode, StringComparison.OrdinalIgnoreCase))
                        {
                            prodDetailDescription.Add(selectedBundleResponse.ProductName);
                        }
                        else
                        {
                            prodDetailDescription.AddRange(selectedBundleResponse.Tile.OfferDescription);
                        }
                    }
                }
            }
            return prodDetailDescription;
        }

        private bool IsFreeSeatCouponApplied(ProdDetail prodDetail, FlightReservationResponse flightReservationResponse)
        {
            return _configuration.GetValue<bool>("IsEnableManageResCoupon") && isAFSCouponApplied(flightReservationResponse?.DisplayCart) && prodDetail?.Segments != null && prodDetail.Segments.Any(x => x != null && IsCouponApplied(x));
        }

        private bool IsCouponApplied(ProductSegmentDetail segmentDetail)
        {
            return segmentDetail?.SubSegmentDetails != null ? segmentDetail.SubSegmentDetails.Any(x => x != null && !string.IsNullOrEmpty(x.OrginalPrice) && Decimal.Parse(x.OrginalPrice) > 0) : false;
        }

        public bool EnablePromoCodeForAncillaryProductsManageRes()
        {
            return _configuration.GetValue<bool>("EnablePromoCodeForAncillaryOffersManageRes");
        }

        public List<string> GetProductDetailDescrption(IGrouping<String, SubItem> subItem)
        {
            List<string> prodDetailDescription = new List<string>();
            if (subItem != null && subItem.Any(si => si.Product != null))
            {
                foreach (var subProduct in subItem.FirstOrDefault(si => si.Product != null).Product.SubProducts)
                {
                    prodDetailDescription.Add(subProduct.Description);
                }
            }
            return prodDetailDescription;
        }

        public void AddCouponDetails(List<ProdDetail> prodDetails, Services.FlightShopping.Common.FlightReservation.FlightReservationResponse cslFlightReservationResponse, bool isPost, string flow, MOBApplication application)
        {
            United.Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart = new United.Service.Presentation.InteractionModel.ShoppingCart();
            flightReservationResponseShoppingCart = isPost ? cslFlightReservationResponse.CheckoutResponse.ShoppingCart : cslFlightReservationResponse.ShoppingCart;
            foreach (var prodDetail in prodDetails)
            {
                var product = flightReservationResponseShoppingCart.Items.SelectMany(I => I.Product).Where(p => p.Code == prodDetail.Code).FirstOrDefault();
                if (product != null && product.CouponDetails != null && product.CouponDetails.Any(c => c != null) && product.CouponDetails.Count() > 0)
                {
                    prodDetail.CouponDetails = new List<CouponDetails>();
                    foreach (var coupon in product.CouponDetails)
                    {
                        if (coupon != null)
                        {
                            prodDetail.CouponDetails.Add(new CouponDetails
                            {
                                PromoCode = coupon.PromoCode,
                                Product = coupon.Product,
                                IsCouponEligible = coupon.IsCouponEligible,
                                Description = coupon.Description,
                                DiscountType = coupon.DiscountType
                            });
                        }
                    }
                }
                if (flow == FlowType.POSTBOOKING.ToString() && prodDetail.CouponDetails != null && prodDetail.CouponDetails.Count > 0
                     || (flow == FlowType.BOOKING.ToString() && prodDetail.CouponDetails != null && prodDetail.CouponDetails.Count > 0 && IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true)) || (_configuration.GetValue<bool>("IsEnableManageResCoupon") && (flow == FlowType.VIEWRES.ToString() || flow == FlowType.VIEWRES_SEATMAP.ToString()) && prodDetail.CouponDetails != null))
                {
                    AddPromoDetailsInSegments(prodDetail);
                }
            }
        }

        public bool IsOriginalPriceExists(ProdDetail prodDetail)
        {
            return !_configuration.GetValue<bool>("DisableFreeCouponFix")
                   && !string.IsNullOrEmpty(prodDetail.ProdOriginalPrice)
                   && Decimal.TryParse(prodDetail.ProdOriginalPrice, out decimal originalPrice)
                   && originalPrice > 0;
        }

        public string BuildProductDescription(Collection<Services.FlightShopping.Common.DisplayCart.TravelOption> travelOptions, IGrouping<string, SubItem> t, string productCode)
        {
            if (string.IsNullOrEmpty(productCode))
                return string.Empty;

            productCode = productCode.ToUpper().Trim();

            if (productCode == "AAC")
                return "Award Accelerator®";

            if (productCode == "PAC")
                return "Premier Accelerator℠";

            if (productCode == "TPI" && _configuration.GetValue<bool>("GetTPIProductName_HardCode"))
                return "Trip insurance";
            if (productCode == "FARELOCK")
                return "FareLock";

            if (_configuration.GetValue<bool>("EnableBasicEconomyBuyOutInViewRes") && productCode == "BEB")
                return !_configuration.GetValue<bool>("EnableNewBEBContentChange") ? "Switch to Economy" : _configuration.GetValue<string>("BEBuyOutPaymentInformationMessage");

            if (productCode == "PCU")
                return GetFormattedCabinName(t.Select(u => u.Description).FirstOrDefault().ToString());


            return travelOptions.Where(d => d.Key == productCode).Select(d => d.Type).FirstOrDefault().ToString();
        }

        public string GetFormattedCabinName(string cabinName)
        {
            if (!_configuration.GetValue<bool>("EnablePcuMultipleUpgradeOptions"))
            {
                return cabinName;
            }

            if (string.IsNullOrWhiteSpace(cabinName))
                return string.Empty;

            switch (cabinName.ToUpper().Trim())
            {
                case "UNITED FIRST":
                    return "United First®";
                case "UNITED BUSINESS":
                    return "United Business®";
                case "UNITED POLARIS FIRST":
                    return "United Polaris℠ first";
                case "UNITED POLARIS BUSINESS":
                    return "United Polaris℠ business";
                case "UNITED PREMIUM PLUS":
                    return "United® Premium Plus";
                default:
                    return string.Empty;
            }
        }

        public string BuildSegmentInfo(string productCode, Collection<ReservationFlightSegment> flightSegments, IGrouping<string, SubItem> x)
        {
            if (productCode == "AAC" || productCode == "PAC")
                return string.Empty;

            if (_configuration.GetValue<bool>("EnableBasicEconomyBuyOutInViewRes") && productCode == "BEB")
            {
                var tripNumber = flightSegments?.Where(y => y.FlightSegment.SegmentNumber == Convert.ToInt32(x.Select(u => u.SegmentNumber).FirstOrDefault())).FirstOrDefault().TripNumber;
                var tripFlightSegments = flightSegments?.Where(c => c != null && !string.IsNullOrEmpty(c.TripNumber) && c.TripNumber.Equals(tripNumber)).ToCollection();
                if (tripFlightSegments != null && tripFlightSegments.Count > 1)
                {
                    return tripFlightSegments?.FirstOrDefault()?.FlightSegment?.DepartureAirport?.IATACode + " - " + tripFlightSegments?.LastOrDefault()?.FlightSegment?.ArrivalAirport?.IATACode;
                }
                else
                {
                    return flightSegments.Where(y => y.FlightSegment.SegmentNumber == Convert.ToInt32(x.Select(u => u.SegmentNumber).FirstOrDefault())).Select(y => y.FlightSegment.DepartureAirport.IATACode + " - " + y.FlightSegment.ArrivalAirport.IATACode).FirstOrDefault().ToString();
                }
            }

            return flightSegments.Where(y => y.FlightSegment.SegmentNumber == Convert.ToInt32(x.Select(u => u.SegmentNumber).FirstOrDefault())).Select(y => y.FlightSegment.DepartureAirport.IATACode + " - " + y.FlightSegment.ArrivalAirport.IATACode).FirstOrDefault().ToString();
        }

        public async Task<List<ProdDetail>> BuildProductDetailsForInflightMeals(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, string productCode, string sessionId, bool isPost)
        {
            List<MOBInFlightMealsRefreshmentsResponse> savedResponse =
           await _sessionHelperService.GetSession<List<MOBInFlightMealsRefreshmentsResponse>>(sessionId, new MOBInFlightMealsRefreshmentsResponse().ObjectName, new List<string> { sessionId, new MOBInFlightMealsRefreshmentsResponse().ObjectName }).ConfigureAwait(false);
            United.Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart;
            if (isPost)
                flightReservationResponseShoppingCart = flightReservationResponse.CheckoutResponse.ShoppingCart;
            else
                flightReservationResponseShoppingCart = flightReservationResponse.ShoppingCart;


            var displayTotalPrice = flightReservationResponse.DisplayCart.DisplayPrices.FirstOrDefault(o => (o.Description != null && o.Description.Equals("Total", StringComparison.OrdinalIgnoreCase))).Amount;
            var grandTotal = flightReservationResponseShoppingCart?.Items.SelectMany(p => p.Product).Where(d => d.Code == "POM")?.Select(p => p.Price?.Totals?.FirstOrDefault().Amount).FirstOrDefault();

            var travelOptions = ShopStaticUtility.GetTravelOptionItems(flightReservationResponse, productCode);
            // For RegisterOffer uppercabin when there is no price no need to build the product
            List<ProdDetail> response = new List<ProdDetail>();
            if (grandTotal > 0 && productCode == _configuration.GetValue<string>("InflightMealProductCode"))
            {
                var productDetail = new ProdDetail()
                {
                    Code = travelOptions.Where(d => d.Key == productCode).Select(d => d.Key).FirstOrDefault().ToString(),
                    ProdDescription = travelOptions.Where(d => d.Key == productCode).Select(d => d.Type).FirstOrDefault().ToString(),
                    ProdTotalPrice = String.Format("{0:0.00}", grandTotal),
                    ProdDisplayTotalPrice = grandTotal?.ToString("c"),
                    Segments = GetProductSegmentForInFlightMeals(flightReservationResponse, savedResponse, travelOptions, flightReservationResponseShoppingCart),
                };
                response.Add(productDetail);
                return response;
            }
            else return response;

        }

        public List<ProductSegmentDetail> GetProductSegmentForInFlightMeals(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse,
                List<MOBInFlightMealsRefreshmentsResponse> savedResponse, Collection<Services.FlightShopping.Common.DisplayCart.TravelOption> travelOptions, United.Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart)
        {
            List<ProductSegmentDetail> response = new List<ProductSegmentDetail>();
            ProductSegmentDetail segmentDetail = new ProductSegmentDetail();
            List<ProductSubSegmentDetail> subSegmentDetails = new List<ProductSubSegmentDetail>();
            var traveler = flightReservationResponse?.Reservation?.Travelers;
            string productCode = _configuration.GetValue<string>("InflightMealProductCode");

            var subProducts = flightReservationResponseShoppingCart.Items
           ?.Where(a => a.Product != null)
           ?.SelectMany(b => b.Product)
           ?.Where(c => c.SubProducts != null && c.SubProducts.Any(d => d.Code == _configuration.GetValue<string>("InflightMealProductCode")))
           ?.SelectMany(d => d.SubProducts);

            var characterStics = flightReservationResponseShoppingCart.Items
           ?.Where(a => a.Product != null)
           ?.SelectMany(b => b.Product)
           ?.Where(c => c.Code == productCode)
           ?.SelectMany(d => d.Characteristics)
           ?.Where(e => e.Code == "SegTravProdSubGroupIDQtyPrice")
           ?.FirstOrDefault();

            string[] items = characterStics.Value.Split(',');
            List<Tuple<string, string, int, string>> tupleList = new List<Tuple<string, string, int, string>>();

            if (items != null && items.Length > 0)
            {
                string[] selectedItems = null;
                foreach (var item in items)
                {
                    //segmentID - TravelerID - ProductID - SubGroupID - Quantity - Price
                    if (item != "")
                        selectedItems = item.Split('|');
                    if (selectedItems != null && selectedItems.Length > 0)
                    {
                        //TravelerID - ProductID - SubGroupID - Quantity - Price
                        tupleList.Add(Tuple.Create(selectedItems[2], selectedItems[3], Convert.ToInt32(selectedItems[4]), selectedItems[5]));
                    }
                }
            }
            for (int i = 0; i < flightReservationResponse.Reservation.Travelers.Count; i++)
            {
                if (response.Count == 0)
                    segmentDetail.SegmentInfo = ShopStaticUtility.GetSegmentDescription(travelOptions);
                List<ProductSubSegmentDetail> snackDetails = new List<ProductSubSegmentDetail>();
                int travelerCouter = 0;
                int prodCounter = 0;
                foreach (var subProduct in subProducts)
                {
                    ProductSubSegmentDetail segDetail = new ProductSubSegmentDetail();
                    if (subProduct.Prices.Where(a => a.Association.TravelerRefIDs[0] == (i + 1).ToString()).Any())
                    {
                        if (subProduct != null && subProduct.Extension != null)
                        {
                            var priceInfo = subProduct.Prices.Where(a => a.Association.TravelerRefIDs[0] == (i + 1).ToString()).FirstOrDefault();
                            double price = priceInfo.PaymentOptions.FirstOrDefault().PriceComponents.FirstOrDefault().Price.Totals.FirstOrDefault().Amount;
                            var tupleSelectedItem = tupleList.Where(a => a.Item2 == subProduct.SubGroupCode && a.Item1 == priceInfo.ID).FirstOrDefault();
                            bool editExtraDataInCheckoutScreenFix = tupleSelectedItem != null && _configuration.GetValue<bool>("EnableisEditablePOMFeature") && (subProduct.Extension.MealCatalog?.MealShortDescription != null);

                            if (tupleSelectedItem != null)
                            {
                                if (_configuration.GetValue<bool>("EnableisEditablePOMFeature"))
                                {
                                    if (price > 0 && subProduct.Extension.MealCatalog?.MealShortDescription != null)
                                    {
                                        if (prodCounter == 0 && travelerCouter == 0)
                                        {
                                            segDetail.Passenger = traveler[i].Person.GivenName.ToLower().ToPascalCase() + " " + traveler[i].Person.Surname.ToLower().ToPascalCase();
                                            segDetail.Price = "0";
                                            snackDetails.Add(segDetail);
                                            segDetail = new ProductSubSegmentDetail();

                                            segDetail.SegmentDescription = subProduct.Extension.MealCatalog?.MealShortDescription + " x " + tupleSelectedItem.Item3;
                                            segDetail.DisplayPrice = "$" + String.Format("{0:0.00}", price * tupleSelectedItem.Item3);
                                            segDetail.Price = price.ToString();
                                        }
                                        else
                                        {
                                            segDetail.SegmentDescription = subProduct.Extension.MealCatalog?.MealShortDescription + " x " + tupleSelectedItem.Item3;
                                            segDetail.DisplayPrice = "$" + String.Format("{0:0.00}", price * tupleSelectedItem.Item3);
                                            segDetail.Price = price.ToString();
                                        }
                                        prodCounter++;
                                        snackDetails.Add(segDetail);
                                    }
                                }
                                else
                                {
                                    //  int quantity = GetQuantity(travelOptions, subProduct.SubGroupCode, subProduct.Prices.Where(a=>a.ID == (i+1).ToString()).Select(b=>b.ID).ToString());
                                    if (prodCounter == 0 && travelerCouter == 0)
                                    {
                                        segDetail.Passenger = traveler[i].Person.GivenName.ToLower().ToPascalCase() + " " + traveler[i].Person.Surname.ToLower().ToPascalCase();
                                        segDetail.Price = "0";
                                        snackDetails.Add(segDetail);
                                        segDetail = new ProductSubSegmentDetail();

                                        segDetail.SegmentDescription = subProduct.Extension.MealCatalog?.MealShortDescription + " x " + tupleSelectedItem.Item3;
                                        segDetail.DisplayPrice = "$" + String.Format("{0:0.00}", price * tupleSelectedItem.Item3);
                                        segDetail.Price = price.ToString();
                                    }
                                    else
                                    {
                                        segDetail.SegmentDescription = subProduct.Extension.MealCatalog?.MealShortDescription + " x " + tupleSelectedItem.Item3;
                                        segDetail.DisplayPrice = "$" + String.Format("{0:0.00}", price * tupleSelectedItem.Item3);
                                        segDetail.Price = price.ToString();
                                    }
                                    prodCounter++;
                                    snackDetails.Add(segDetail);
                                }
                            }

                        }
                    }

                }
                if (segmentDetail.SubSegmentDetails == null) segmentDetail.SubSegmentDetails = new List<ProductSubSegmentDetail>();
                if (snackDetails != null)
                    segmentDetail.SubSegmentDetails.AddRange(snackDetails);
                travelerCouter++;

            }
            if (segmentDetail != null && segmentDetail.SubSegmentDetails != null && !response.Contains(segmentDetail))
                response.Add(segmentDetail);
            return response;
        }

        public ProdDetail BuildProdDetailsForSeats(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, SeatChangeState state, bool isPost, string flow)
        {
            if (flightReservationResponse.DisplayCart.DisplaySeats == null || !flightReservationResponse.DisplayCart.DisplaySeats.Any())
            {
                return null;
            }
            //check here.
            var fliterSeats = flightReservationResponse.DisplayCart.DisplaySeats.Where(d => d.PCUSeat || (CheckSeatAssignMessage(d.SeatAssignMessage, isPost) && d.Seat != "---")).ToList();
            if (_configuration.GetValue<bool>("EnablePCUFromSeatMapErrorCheckViewRes"))
            {
                fliterSeats = HandleCSLDefect(flightReservationResponse, fliterSeats, isPost);
            }
            if (!fliterSeats.Any())
            {
                return null;
            }

            var totalPrice = fliterSeats.Select(s => s.SeatPrice).ToList().Sum();
            var prod = new ProdDetail
            {
                Code = "SEATASSIGNMENTS",
                ProdDescription = string.Empty,
                ProdTotalPrice = String.Format("{0:0.00}", totalPrice),
                ProdDisplayTotalPrice = totalPrice > 0 ? Decimal.Parse(totalPrice.ToString()).ToString("c") : string.Empty,
                Segments = BuildProductSegmentsForSeats(flightReservationResponse, state.Seats, state.BookingTravelerInfo, isPost)
            };
            if (prod.Segments != null && prod.Segments.Any())
            {
                if (IsMilesFOPEnabled())
                {
                    if (prod.Segments.SelectMany(s => s.SubSegmentDetails).ToList().Select(ss => ss.Miles == 0).ToList().Count == 0 && IsMilesFOPEnabled())
                    {
                        prod.ProdTotalMiles = _configuration.GetValue<int>("milesFOP");
                        prod.ProdDisplayTotalMiles = ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false);
                    }
                    else
                    {
                        prod.ProdTotalMiles = 0;
                        prod.ProdDisplayTotalMiles = string.Empty;
                    }
                }
                if (_configuration.GetValue<bool>("IsEnableManageResCoupon") && isAFSCouponApplied(flightReservationResponse.DisplayCart))
                    prod.Segments.Select(x => x.SubSegmentDetails).ToList().ForEach(item => item.RemoveAll(k => Decimal.Parse(k.Price) == 0 && (string.IsNullOrEmpty(k.OrginalPrice) || Decimal.Parse(k.OrginalPrice) == 0)));
                else
                    prod.Segments.Select(x => x.SubSegmentDetails).ToList().ForEach(item => item.RemoveAll(k => Decimal.Parse(k.Price) == 0 && (k.StrikeOffPrice == string.Empty || Decimal.Parse(k.StrikeOffPrice) == 0)));

                prod.Segments.RemoveAll(k => k.SubSegmentDetails.Count == 0);
            }
            ShopStaticUtility.UpdateRefundTotal(prod);
            return prod;
        }
        public List<ProductSegmentDetail> BuildProductSegmentsForSeats(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, List<Seat> seats, MOBApplication application, string flow)
        {
            return flightReservationResponse.DisplayCart.DisplaySeats.OrderBy(d => d.OriginalSegmentIndex)
                                                        .GroupBy(d => new { d.OriginalSegmentIndex, d.LegIndex })
                                                        .Select(d => new ProductSegmentDetail
                                                        {
                                                            SegmentInfo = ShopStaticUtility.GetSegmentInfo(flightReservationResponse, d.Key.OriginalSegmentIndex, Convert.ToInt32(d.Key.LegIndex)),
                                                            SubSegmentDetails = d.GroupBy(s => ShopStaticUtility.GetSeatTypeForDisplay(s, flightReservationResponse.DisplayCart.TravelOptions))
                                                                                .Select(seatGroup => new ProductSubSegmentDetail
                                                                                {
                                                                                    SegmentInfo = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? ShopStaticUtility.GetSegmentInfo(flightReservationResponse, d.Key.OriginalSegmentIndex, Convert.ToInt32(d.Key.LegIndex)) : string.Empty,
                                                                                    OrginalPrice = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? String.Format("{0:0.00}", seatGroup.Select(s => s.OriginalPrice).ToList().Sum()) : string.Empty,
                                                                                    Price = String.Format("{0:0.00}", seatGroup.Select(s => s.SeatPrice).ToList().Sum()),
                                                                                    DisplayPrice = Decimal.Parse(seatGroup.Select(s => s.SeatPrice).ToList().Sum().ToString()).ToString("c"),
                                                                                    DisplayOriginalPrice = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? Decimal.Parse(seatGroup.Select(s => s.OriginalPrice).ToList().Sum().ToString()).ToString("c") : string.Empty,
                                                                                    StrikeOffPrice = ShopStaticUtility.GetOriginalTotalSeatPriceForStrikeOff(seatGroup.ToList(), seats),
                                                                                    DisplayStrikeOffPrice = ShopStaticUtility.GetFormatedDisplayPriceForSeats(ShopStaticUtility.GetOriginalTotalSeatPriceForStrikeOff(seatGroup.ToList(), seats)),
                                                                                    Passenger = seatGroup.Count().ToString() + (seatGroup.Count() > 1 ? " Travelers" : " Traveler"),
                                                                                    SeatCode = seatGroup.Key,
                                                                                    FlightNumber = seatGroup.Select(x => x.FlightNumber).FirstOrDefault(),
                                                                                    SegmentDescription = GetSeatTypeBasedonCode(seatGroup.Key, seatGroup.Count()),
                                                                                    PaxDetails = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? GetPaxDetails(seatGroup, flightReservationResponse, flow, application) : null,

                                                                                    ProductDescription = IsEnableOmniCartMVP2Changes(application.Id, application.Version.Major, true) ? ShopStaticUtility.GetSeatDescription(seatGroup.Key) : string.Empty
                                                                                }).ToList().OrderBy(p => ShopStaticUtility.GetSeatPriceOrder()[p.SegmentDescription]).ToList()
                                                        }).ToList();
        }

        public List<MOBPaxDetails> GetPaxDetails(IGrouping<string, SeatAssignment> t, FlightReservationResponse response, string flow, MOBApplication application)
        {
            List<MOBPaxDetails> paxDetails = new List<MOBPaxDetails>();
            if (response?.Reservation?.Travelers != null)
            {
                bool isExtraSeatEnabled = application != null && IsExtraSeatFeatureEnabled(application.Id, application?.Version?.Major) && flow.ToString() == FlowType.BOOKING.ToString();
                var extraSeatPassengerIndex = GetTravelerNameIndexForExtraSeat(isExtraSeatEnabled, response.Reservation.Services);

                t.ForEach(seat =>
                {
                    var traveler = response.Reservation.Travelers.Where(passenger => passenger.Person != null && passenger.Person.Key == seat.PersonIndex).FirstOrDefault();
                    if (traveler != null && (seat.SeatPrice > 0 || seat.OriginalPrice > 0)) // Added OriginalPrice check as well to handle coupon applied sceanrios where seat price can be 0 but we have original price
                    {
                        paxDetails.Add(new MOBPaxDetails
                        {
                            FullName = PaxName(traveler, isExtraSeatEnabled, extraSeatPassengerIndex),
                            Key = seat.PersonIndex,
                            Seat = seat.Seat

                        });
                    }

                });
            }
            return paxDetails;
        }

        public string PaxName(United.Service.Presentation.ReservationModel.Traveler traveler, bool isExtraSeatEnabled, List<string> extraSeatPassengerIndex)
        {
            if (isExtraSeatEnabled && extraSeatPassengerIndex?.Count > 0 && !string.IsNullOrEmpty(traveler?.Person?.Key) && extraSeatPassengerIndex.Contains(traveler.Person.Key))
            {
                string travelerMiddleInitial = !string.IsNullOrEmpty(traveler.Person?.MiddleName) ? " " + traveler.Person.MiddleName.Substring(0, 1) : string.Empty;
                string travelerSuffix = !string.IsNullOrEmpty(traveler.Person?.Suffix) ? " " + traveler.Person.Suffix : string.Empty;

                return _configuration.GetValue<string>("ExtraSeatName") + " (" + GetGivenNameForExtraSeat(traveler.Person?.GivenName) + travelerMiddleInitial + " " + traveler.Person?.Surname + travelerSuffix + ")";
            }
            else
                return traveler.Person.GivenName + " " + traveler.Person.Surname;
        }

        public string GetGivenNameForExtraSeat(string givenName)
        {
            if (!string.IsNullOrEmpty(givenName))
            {
                if (givenName.Contains(EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXSTTWO.ToString()))
                    return givenName.Remove(0, EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXSTTWO.ToString().Length);
                else if (givenName.Contains(EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBGTWO.ToString()))
                    return givenName.Remove(0, EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBGTWO.ToString().Length);
                else if (givenName.Contains(EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXST.ToString()))
                    return givenName.Remove(0, EXTRASEATCOUNTFORSSRREMARKS_PERSONAL_COMFORT.EXST.ToString().Length);
                else if (givenName.Contains(EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBG.ToString()))
                    return givenName.Remove(0, EXTRASEATCOUNTFORSSRREMARKS_CABIN_BAGGAGE.CBBG.ToString().Length);
            }

            return givenName;
        }

        public bool IsExtraSeatFeatureEnabled(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableExtraSeatsFeature")
                        && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "Android_EnableExtraSeatsFeature_AppVersion", "IPhone_EnableExtraSeatsFeature_AppVersion", "", "", true, _configuration);
        }

        public List<string> GetTravelerNameIndexForExtraSeat(bool isExtraSeatEnabled, Collection<Service.Presentation.CommonModel.Service> services)
        {
            var extraSeatPassengerIndex = new List<string>();
            if (isExtraSeatEnabled)
            {
                var extraSeatCodes = _configuration.GetValue<string>("EligibleSSRCodesForExtraSeat")?.Split("|");
                services?.ForEach(x =>
                {
                    if (!string.IsNullOrEmpty(x?.Code) && extraSeatCodes.Contains(x.Code) && !string.IsNullOrEmpty(x.TravelerNameIndex) && !extraSeatPassengerIndex.Contains(x.TravelerNameIndex))
                    {
                        extraSeatPassengerIndex.Add(x.TravelerNameIndex);
                    }
                });
            }

            return extraSeatPassengerIndex;
        }

        public List<ProductSegmentDetail> BuildProductSegmentsForSeats(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, List<Seat> seats, List<MOBBKTraveler> BookingTravelerInfo, bool isPost)
        {
            if (flightReservationResponse.DisplayCart.DisplaySeats == null || !flightReservationResponse.DisplayCart.DisplaySeats.Any())
                return null;

            var displaySeats = flightReservationResponse.DisplayCart.DisplaySeats.Clone();
            List<string> refundedSegmentNums = null;
            if (flightReservationResponse.Errors != null && flightReservationResponse.Errors.Any(e => e != null && e.MinorCode == "90506"))
            {
                bool DisableFixForPCUPurchaseFailMsg_MOBILE15837 = _configuration.GetValue<bool>("DisableFixForPCUPurchaseFailMsg_MOBILE15837");
                var isRefundSuccess = ShopStaticUtility.IsRefundSuccess(flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items, out refundedSegmentNums, DisableFixForPCUPurchaseFailMsg_MOBILE15837);
                //Remove pcu seats if refund Failed
                if (!isRefundSuccess)
                {
                    displaySeats.RemoveAll(ds => ds.PCUSeat);
                }
                if (!displaySeats.Any())
                    return null;
            }

            //Remove all failed seats other than pcu seats.
            displaySeats.RemoveAll(ds => !ds.PCUSeat && !CheckSeatAssignMessage(ds.SeatAssignMessage, isPost)); // string.IsNullOrEmpty(ds.SeatAssignMessage)
            if (_configuration.GetValue<bool>("EnablePCUFromSeatMapErrorCheckViewRes"))
            {
                displaySeats = HandleCSLDefect(flightReservationResponse, displaySeats, isPost);
            }
            if (!displaySeats.Any())
                return null;

            return displaySeats.OrderBy(d => d.OriginalSegmentIndex)
                                .GroupBy(d => new { d.OriginalSegmentIndex, d.LegIndex })
                                .Select(d => new ProductSegmentDetail
                                {
                                    SegmentInfo = ShopStaticUtility.GetSegmentInfo(flightReservationResponse, d.Key.OriginalSegmentIndex, Convert.ToInt32(d.Key.LegIndex)),
                                    SubSegmentDetails = d.GroupBy(s => ShopStaticUtility.GetSeatTypeForDisplay(s, flightReservationResponse.DisplayCart.TravelOptions))
                                                        .Select(seatGroup => new ProductSubSegmentDetail
                                                        {
                                                            Price = String.Format("{0:0.00}", seatGroup.Select(s => s.SeatPrice).ToList().Sum()),
                                                            OrginalPrice = _configuration.GetValue<bool>("IsEnableManageResCoupon") ? String.Format("{0:0.00}", seatGroup.Select(s => s.OriginalPrice).ToList().Sum()) : string.Empty,
                                                            DisplayPrice = Decimal.Parse(seatGroup.Select(s => s.SeatPrice).ToList().Sum().ToString()).ToString("c"),
                                                            DisplayOriginalPrice = _configuration.GetValue<bool>("IsEnableManageResCoupon") ? Decimal.Parse(seatGroup.Select(s => s.OriginalPrice).ToList().Sum().ToString()).ToString("c") : string.Empty,
                                                            StrikeOffPrice = ShopStaticUtility.GetOriginalTotalSeatPriceForStrikeOff(seatGroup.ToList(), seats, BookingTravelerInfo),
                                                            DisplayStrikeOffPrice = ShopStaticUtility.GetFormatedDisplayPriceForSeats(ShopStaticUtility.GetOriginalTotalSeatPriceForStrikeOff(seatGroup.ToList(), seats, BookingTravelerInfo)),
                                                            Passenger = seatGroup.Count().ToString() + (seatGroup.Count() > 1 ? " Travelers" : " Traveler"),
                                                            SeatCode = seatGroup.Key,
                                                            FlightNumber = seatGroup.Select(x => x.FlightNumber).FirstOrDefault(),
                                                            SegmentDescription = GetSeatTypeBasedonCode(seatGroup.Key, seatGroup.Count()),
                                                            IsPurchaseFailure = ShopStaticUtility.IsPurchaseFailed(seatGroup.Any(s => s.PCUSeat), d.Key.OriginalSegmentIndex.ToString(), refundedSegmentNums),
                                                            Miles = IsMilesFOPEnabled() ? seatGroup.Any(s => s.PCUSeat == true) ? 0 : _configuration.GetValue<int>("milesFOP") : 0,
                                                            DisplayMiles = IsMilesFOPEnabled() ? seatGroup.Any(s => s.PCUSeat == true) ? string.Empty : ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false) : string.Empty,
                                                            StrikeOffMiles = IsMilesFOPEnabled() ? seatGroup.Any(s => s.PCUSeat == true) ? 0 : Convert.ToInt32(_configuration.GetValue<string>("milesFOP")) : 0,
                                                            DisplayStrikeOffMiles = IsMilesFOPEnabled() ? seatGroup.Any(s => s.PCUSeat == true) ? string.Empty : ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false) : string.Empty
                                                        }).ToList().OrderBy(p => ShopStaticUtility.GetSeatPriceOrder()[p.SegmentDescription]).ToList()
                                }).ToList();
        }

        public string GetSeatTypeBasedonCode(string seatCode, int travelerCount)
        {
            string seatType = string.Empty;

            switch (seatCode.ToUpper().Trim())
            {
                case "SXZ": //StandardPreferredExitPlus
                case "SZX": //StandardPreferredExit
                case "SBZ": //StandardPreferredBlukheadPlus
                case "SZB": //StandardPreferredBlukhead
                case "SPZ": //StandardPreferredZone
                case "PZA":
                    seatType = (travelerCount > 1) ? "Preferred seats" : "Preferred seat";
                    break;
                case "SXP": //StandardPrimeExitPlus
                case "SPX": //StandardPrimeExit
                case "SBP": //StandardPrimeBlukheadPlus
                case "SPB": //StandardPrimeBlukhead
                case "SPP": //StandardPrimePlus
                case "PPE": //StandardPrime
                case "BSA":
                case "ASA":
                    seatType = (travelerCount > 1) ? "Advance seat assignments" : "Advance seat assignment";
                    break;
                case "EPL": //EplusPrime
                case "EPU": //EplusPrimePlus
                case "BHS": //BulkheadPrime
                case "BHP": //BulkheadPrimePlus  
                case "PSF": //PrimePlus  
                    seatType = (travelerCount > 1) ? "Economy Plus Seats" : "Economy Plus Seat";
                    break;
                case "PSL": //Prime                            
                    seatType = (travelerCount > 1) ? "Economy Plus Seats (limited recline)" : "Economy Plus Seat (limited recline)";
                    break;
                default:
                    var pcuCabinName = GetFormattedCabinName(seatCode);
                    if (!string.IsNullOrEmpty(pcuCabinName))
                    {
                        return pcuCabinName + ((travelerCount > 1) ? " Seats" : " Seat");
                    }
                    return string.Empty;
            }
            return seatType;
        }


        public List<SeatAssignment> HandleCSLDefect(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, List<SeatAssignment> fliterSeats, bool isPost)
        {
            if (fliterSeats == null || !fliterSeats.Any())
                return fliterSeats;

            fliterSeats = fliterSeats.Where(s => s != null && s.OriginalSegmentIndex != 0 && !string.IsNullOrEmpty(s.DepartureAirportCode) && !string.IsNullOrEmpty(s.ArrivalAirportCode)).ToList();

            if (fliterSeats == null || !fliterSeats.Any())
                return fliterSeats;

            if (flightReservationResponse.Errors != null &&
                flightReservationResponse.Errors.Any(e => e != null && e.MinorCode == "90584") &&
                flightReservationResponse.DisplayCart.DisplaySeats != null &&
                flightReservationResponse.DisplayCart.DisplaySeats.Any(s => s != null && s.PCUSeat) &&
                flightReservationResponse.DisplayCart.DisplaySeats.Any(s => s != null && !s.PCUSeat &&
                 CheckSeatAssignMessage(s.SeatAssignMessage, isPost)))
            {
                //take this from errors
                var item = flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items.Where(t => t.Item.Category == "Reservation.Reservation.SEATASSIGNMENTS").FirstOrDefault();
                if (item != null && item.Item != null && item.Item.Product != null && item.Item.Product.Any())
                {
                    var description = JsonConvert.DeserializeObject<Service.Presentation.FlightResponseModel.AssignTravelerSeat>(item.Item.Product.FirstOrDefault().Status.Description);
                    var unAssignedSeats = description.Travelers.SelectMany(t => t.Seats.Where(s => !string.IsNullOrEmpty(s.AssignMessage))).ToList();
                    if (unAssignedSeats != null && unAssignedSeats.Any())
                    {
                        return fliterSeats.Where(s => !ShopStaticUtility.IsFailedSeat(s, unAssignedSeats)).ToList();
                    }
                }
            }
            return fliterSeats;
        }

        public bool CheckSeatAssignMessage(string seatAssignMessage, bool isPost)
        {
            if (_configuration.GetValue<bool>("EnableCSL30ManageResSelectSeatMap") && isPost)
            {
                return !string.IsNullOrEmpty(seatAssignMessage) && seatAssignMessage.Equals("SEATS ASSIGNED", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return string.IsNullOrEmpty(seatAssignMessage);
            }
        }

        public ProdDetail BuildProdDetailsForSeats(Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, SeatChangeState state, string flow, MOBApplication application)
        {
            if (ShopStaticUtility.IsCheckinFlow(flow) && flightReservationResponse.DisplayCart.TravelOptions != null && flightReservationResponse.DisplayCart.TravelOptions.Any(x => x.Type == "SEATASSIGNMENTS"))
            {
                ProdDetail prod = new ProdDetail();
                prod.Code = "SEATASSIGNMENTS";
                prod.ProdDescription = string.Empty;
                decimal totalPrice = flightReservationResponse.DisplayCart.TravelOptions.Sum(x => x.Type == "SEATASSIGNMENTS" ? x.Amount : 0);
                prod.ProdTotalPrice = String.Format("{0:0.00}", totalPrice);
                prod.ProdDisplayTotalPrice = $"${prod.ProdTotalPrice}";
                if (totalPrice > 0)
                {
                    var displaySeats = flightReservationResponse.DisplayCart.DisplaySeats.Where(x => x.OriginalPrice > 0).Select(x => { x.SeatType = ShopStaticUtility.GetCommonSeatCode(x.SeatType); return x; }).GroupBy(x => $"{x.DepartureAirportCode} - {x.ArrivalAirportCode}");
                    prod.Segments = ShopUtility.BuildCheckinSegmentDetail(displaySeats);
                }
                return prod;
            }

            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("EnablePCUSelectedSeatPurchaseViewRes")) && !ShopStaticUtility.IsCheckinFlow(flow))
            {
                var prod = new ProdDetail
                {
                    Code = "SEATASSIGNMENTS",
                    ProdDescription = string.Empty,
                    ProdTotalPrice = String.Format("{0:0.00}", flightReservationResponse.DisplayCart.DisplaySeats.Select(d => d.SeatPrice).ToList().Sum()),
                    ProdDisplayTotalPrice = Decimal.Parse(flightReservationResponse.DisplayCart.DisplaySeats.Select(d => d.SeatPrice).ToList().Sum().ToString()).ToString("c"),
                    Segments = BuildProductSegmentsForSeats(flightReservationResponse, state?.Seats, application, flow)
                };
                prod.Segments.Select(x => x.SubSegmentDetails).ToList().ForEach(item => item.RemoveAll(k => Decimal.Parse(k.Price) == 0 && (k.StrikeOffPrice == string.Empty || Decimal.Parse(k.StrikeOffPrice) == 0)));
                prod.Segments.RemoveAll(k => k.SubSegmentDetails.Count == 0);
                return prod;
            }

            var prodDetail = new ProdDetail()
            {
                Code = "SEATASSIGNMENTS",
                ProdDescription = string.Empty,
                ProdTotalPrice = String.Format("{0:0.00}", flightReservationResponse.DisplayCart.DisplaySeats.Select(d => d.SeatPrice).ToList().Sum()),
                ProdDisplayTotalPrice = Decimal.Parse(flightReservationResponse.DisplayCart.DisplaySeats.Select(d => d.SeatPrice).ToList().Sum().ToString()).ToString("c"),
                //Mobile-1524: Include all the seats even if seat price is null when user has E+/Preferred subscriptions
                //                                                           |----Ignore the DisplaySeats ---------|------Updating the SeatPromotionCode to a common code for easy grouping ---------------|                                                                                                                            |--Ordering the DisplaySeats based---|--Group the resulted DisplaySeats objects based on OriginalSegmentIndex and LegIndex value and return the List of DisplaySeats -|   
                //       LEVEL 1                                             |----if SeatPromotionCode = null------|------and return the object using GetCommonCode method---------------------------------|----------Ignoring DisplaySeats child object if either SeatPrice = 0  ------------------------------------------------      |--on OriginalSegmentIndex-----------|--This is been done for COG, THRU flights. These flights even though are one segment at high level but have multiple segments---|
                Segments = flightReservationResponse.DisplayCart.DisplaySeats.Where(x => x.SeatPromotionCode != null).Select(x => { x.SeatPromotionCode = ShopStaticUtility.GetCommonSeatCode(x.SeatPromotionCode); return x; }).Where(d => (state != null ? (state.TotalEplusEligible > 0 ? true : d.SeatPrice != 0) : d.SeatPrice != 0)).OrderBy(d => d.OriginalSegmentIndex).GroupBy(d => new { d.OriginalSegmentIndex, d.LegIndex }).Select(d => new ProductSegmentDetail
                //Segments = flightReservationResponse.DisplayCart.DisplaySeats.Where(x => x.SeatPromotionCode != null).Select(x => { x.SeatPromotionCode = ShopStaticUtility.GetCommonSeatCode(x.SeatPromotionCode); return x; }).Where(d => (true? d.SeatPrice != 0 : d.SeatPrice >= 0)).OrderBy(d =>d.Orih]]]]]]]]]]]ginalSegmentIndex).GroupBy(d => new { d.OriginalSegmentIndex, d.LegIndex }).Select(d => new MOBProductSegmentDetail
                {
                    //                |--------------Get the individual Segment Origin and Destination detail based on OriginalSegmentIndex and LegIndex for the list of DisplaySeats from LEVEL 1---| 
                    SegmentInfo = ShopStaticUtility.GetSegmentInfo(flightReservationResponse, d.Select(u => u.OriginalSegmentIndex).FirstOrDefault(), Convert.ToInt32(d.Select(u => u.LegIndex).FirstOrDefault())),
                    ProductId = null,
                    //     LEVEL 2         |---- Further group the LEVEL 1 list of DisplaySeats based on OriginalSegmentIndex and SeatPromotionCode to get SubSegmentDetails--|
                    SubSegmentDetails = d.GroupBy(t => new { t.OriginalSegmentIndex, t.SeatPromotionCode }).Select(t => new ProductSubSegmentDetail
                    {
                        //              |--Getting the sum of SeatPrice from the list of DisplaySeats at LEVEL 2 --|
                        Price = String.Format("{0:0.00}", t.Select(s => s.SeatPrice).ToList().Sum()),
                        DisplayPrice = Decimal.Parse(t.Select(s => s.SeatPrice).ToList().Sum().ToString()).ToString("c"),
                        //                  | --Getting the count of list of DisplaySeats at LEVEL 2-- |
                        Passenger = t.Count().ToString() + (t.Count() > 1 ? " Travelers" : " Traveler"), // t.GroupBy(u => u.TravelerIndex).Count().ToString() + (t.GroupBy(u => u.TravelerIndex).Count() > 1 ? " Travelers" : " Traveler"),
                        SeatCode = t.Select(u => u.SeatPromotionCode).FirstOrDefault(),
                        FlightNumber = t.Select(x => x.FlightNumber).FirstOrDefault(),
                        // DepartureTime = flightReservationResponse.Reservation.FlightSegments.Where(s => s.FlightSegment == ),
                        //                          | --Getting the SeatDescription based on SeatPromotionCode from the list of DisplaySeats at LEVEL 2, TravelerIndex count for pluralizing the text. -- |
                        SegmentDescription = GetSeatTypeBasedonCode(t.Select(u => u.SeatPromotionCode).FirstOrDefault(), t.GroupBy(u => u.TravelerIndex).Count())
                        //             | -- Once Get the final list ordering them based on the order defined in GetSEatPriceOrder method comparing with list SegmentDescription -- |
                    }).ToList().OrderBy(p => ShopStaticUtility.GetSeatPriceOrder()[p.SegmentDescription]).ToList()
                }).ToList(),
            };

            if (state == null ? false : (state.TotalEplusEligible > 0))
            {
                foreach (var segmnt in prodDetail.Segments)
                {
                    var subSegments = segmnt.SubSegmentDetails;
                    foreach (var subSegment in subSegments)
                    {
                        if (state.Seats != null)
                        {
                            var sbSegments = state.Seats.Where(x => x.Origin == segmnt.SegmentInfo.Substring(0, 3) && x.Destination == segmnt.SegmentInfo.Substring(6, 3) && x.FlightNumber == subSegment.FlightNumber && (ShopStaticUtility.GetCommonSeatCode(x.ProgramCode) == subSegment.SeatCode)).ToList();
                            decimal totalPrice = sbSegments.Select(u => u.Price).ToList().Sum();
                            decimal discountPrice = sbSegments.Select(u => u.PriceAfterTravelerCompanionRules).ToList().Sum();
                            if (discountPrice < totalPrice)
                            {
                                subSegment.StrikeOffPrice = String.Format("{0:0.00}", sbSegments.Select(u => u.Price).ToList().Sum().ToString());
                                subSegment.DisplayStrikeOffPrice = Decimal.Parse(sbSegments.Select(u => u.Price).ToList().Sum().ToString()).ToString("c");
                            }
                            subSegment.Passenger = sbSegments.Count() + (sbSegments.Count() > 1 ? " Travelers" : " Traveler");
                            subSegment.Price = String.Format("{0:0.00}", sbSegments.Select(u => u.PriceAfterTravelerCompanionRules).ToList().Sum().ToString());
                            subSegment.DisplayPrice = Decimal.Parse(sbSegments.Select(u => u.PriceAfterTravelerCompanionRules).ToList().Sum().ToString()).ToString("c");
                        }
                    }
                }
            }
            //Mobile-1855: Remove segments with no seats to purchase
            prodDetail.Segments.Select(x => x.SubSegmentDetails).ToList().ForEach(item => item.RemoveAll(k => k.Price == "0" && (k.StrikeOffPrice == "0" || k.StrikeOffPrice == string.Empty)));
            prodDetail.Segments.RemoveAll(k => k.SubSegmentDetails.Count == 0);
            return prodDetail;
        }
        public List<ProductSegmentDetail> BuildCheckinSegmentDetail(IEnumerable<IGrouping<string, SeatAssignment>> seatAssignmentGroup)
        {
            List<ProductSegmentDetail> segmentDetails = new List<ProductSegmentDetail>();
            seatAssignmentGroup.ForEach(seatSegment => segmentDetails.Add(new ProductSegmentDetail()
            {
                SegmentInfo = seatSegment.Key,
                SubSegmentDetails = BuildSubsegmentDetails(seatSegment.ToList()).OrderBy(p => ShopStaticUtility.GetSeatPriceOrder()[p.SegmentDescription]).ToList()
            }));
            return segmentDetails;
        }

        public List<ProductSubSegmentDetail> BuildSubsegmentDetails(List<SeatAssignment> seatAssignments)
        {
            List<ProductSubSegmentDetail> subSegmentDetails = new List<ProductSubSegmentDetail>();
            var groupedByTypeAndPrice = seatAssignments.GroupBy(s => s.SeatType, (key, grpSeats) => new { SeatType = key, OriginalPrice = grpSeats.Sum(x => x.OriginalPrice), SeatPrice = grpSeats.Sum(x => x.SeatPrice), Count = grpSeats.Count() });

            groupedByTypeAndPrice.ForEach(grpSeats =>
            {
                subSegmentDetails.Add(PopulateSubsegmentDetails(grpSeats.SeatType, grpSeats.OriginalPrice, grpSeats.SeatPrice, grpSeats.Count));
            });
            return subSegmentDetails;
        }

        public ProductSubSegmentDetail PopulateSubsegmentDetails(string seatType, decimal originalPrice, decimal seatPrice, int count)
        {
            ProductSubSegmentDetail subsegmentDetail = new ProductSubSegmentDetail();
            subsegmentDetail.Price = String.Format("{0:0.00}", seatPrice);
            subsegmentDetail.DisplayPrice = $"${subsegmentDetail.Price}";
            if (originalPrice > seatPrice)
            {
                subsegmentDetail.StrikeOffPrice = String.Format("{0:0.00}", originalPrice);
                subsegmentDetail.DisplayStrikeOffPrice = $"${subsegmentDetail.StrikeOffPrice}";
            }
            subsegmentDetail.Passenger = $"{count} Traveler{(count > 1 ? "s" : String.Empty)}";
            subsegmentDetail.SegmentDescription = GetSeatTypeBasedonCode(seatType, count, true);
            return subsegmentDetail;
        }
        public string GetSeatTypeBasedonCode(string seatCode, int travelerCount, bool isCheckinPath = false)
        {
            string seatType = string.Empty;

            switch (seatCode.ToUpper().Trim())
            {
                case "SXZ": //StandardPreferredExitPlus
                case "SZX": //StandardPreferredExit
                case "SBZ": //StandardPreferredBlukheadPlus
                case "SZB": //StandardPreferredBlukhead
                case "SPZ": //StandardPreferredZone
                case "PZA":
                    seatType = (travelerCount > 1) ? "Preferred seats" : "Preferred seat";
                    break;
                case "SXP": //StandardPrimeExitPlus
                case "SPX": //StandardPrimeExit
                case "SBP": //StandardPrimeBlukheadPlus
                case "SPB": //StandardPrimeBlukhead
                case "SPP": //StandardPrimePlus
                case "PPE": //StandardPrime
                case "BSA":
                case "ASA":
                    if (isCheckinPath)
                        seatType = (travelerCount > 1) ? "Seat assignments" : "Seat assignment";
                    else
                        seatType = (travelerCount > 1) ? "Advance seat assignments" : "Advance seat assignment";
                    break;
                case "EPL": //EplusPrime
                case "EPU": //EplusPrimePlus
                case "BHS": //BulkheadPrime
                case "BHP": //BulkheadPrimePlus  
                case "PSF": //PrimePlus  
                    seatType = (travelerCount > 1) ? "Economy Plus Seats" : "Economy Plus Seat";
                    break;
                case "PSL": //Prime                            
                    seatType = (travelerCount > 1) ? "Economy Plus Seats (limited recline)" : "Economy Plus Seat (limited recline)";
                    break;
                default:
                    var pcuCabinName = GetFormattedCabinName(seatCode);
                    if (!string.IsNullOrEmpty(pcuCabinName))
                    {
                        return pcuCabinName + ((travelerCount > 1) ? " Seats" : " Seat");
                    }
                    return string.Empty;
            }
            return seatType;
        }


        public double GetGrandTotalPriceForShoppingCart(bool isCompleteFarelockPurchase, Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, bool isPost, string flow = "VIEWRES")
        {
            //Added Null check for price.Since,CSL is not sending the price when we select seat of price zero.
            //Added condition to check whether Total in the price exists(This is for scenario when we register the bundle after registering the seat).
            //return isCompleteFarelockPurchase ? Convert.ToDouble(flightReservationResponse.DisplayCart.DisplayPrices.FirstOrDefault(o => (o.Description != null && o.Description.Equals("GrandTotal", StringComparison.OrdinalIgnoreCase))).Amount)
            //                                  : (Utility.IsCheckinFlow(flow) || flow == FlowType.VIEWRES.ToString()) ? flightReservationResponse.ShoppingCart.Items.Where(x => x.Product.FirstOrDefault().Code != "RES" && (x.Product.FirstOrDefault().Price != null ? (x.Product.FirstOrDefault().Price.Totals.Any()) : false)).Select(x => x.Product.FirstOrDefault().Price.Totals.FirstOrDefault().Amount).ToList().Sum() : flightReservationResponse.ShoppingCart.Items.SelectMany(x => x.Product).Where(x => x.Price != null).SelectMany(x => x.Price.Totals).Where(x => (x.Name != null ? x.Name.ToUpper() == "GrandTotalForCurrency".ToUpper() : true)).Select(x => x.Amount).ToList().Sum();
            double shoppingCartTotalPrice = 0.0;
            double closeBookingFee = 0.0;
            if (isCompleteFarelockPurchase)
                shoppingCartTotalPrice = Convert.ToDouble(flightReservationResponse.DisplayCart.DisplayPrices.FirstOrDefault(o => (o.Description != null && o.Description.Equals("GrandTotal", StringComparison.OrdinalIgnoreCase))).Amount);
            else
            {
                if (isPost ? flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items.Select(x => x.Item).Any(x => x.Product.FirstOrDefault().Code == "FLK")
                        : flightReservationResponse.ShoppingCart.Items.Any(x => x.Product.FirstOrDefault().Code == "FLK"))
                    flow = FlowType.FARELOCK.ToString();
                //[MB-6519]:Getting Sorry Something went wrong for Award Booking for with Reward booking fee and reservation price is zero
                if (_configuration.GetValue<bool>("CFOP19HBugFixToggle") && (isPost ? flightReservationResponse.CheckoutResponse.ShoppingCartResponse.Items.Select(x => x.Item).Any(x => x.Product.FirstOrDefault().Code == "RBF")
                                                                            : flightReservationResponse.ShoppingCart.Items.Any(x => x.Product.FirstOrDefault().Code == "RBF")))

                {
                    United.Service.Presentation.InteractionModel.ShoppingCart flightReservationResponseShoppingCart = new United.Service.Presentation.InteractionModel.ShoppingCart();
                    flightReservationResponseShoppingCart = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart : flightReservationResponse.ShoppingCart;
                    closeBookingFee = ShopStaticUtility.GetCloseBookingFee(isPost, flightReservationResponseShoppingCart, flow);

                }

                switch (flow)
                {
                    case "BOOKING":
                    case "RESHOP":
                        shoppingCartTotalPrice = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart.Items.Where(x => !ShopStaticUtility.CheckFailedShoppingCartItem(flightReservationResponse, x)).SelectMany(x => x.Product).Where(x => x.Price != null).SelectMany(x => x.Price.Totals).Where(x => (x.Name != null ? x.Name.ToUpper() == "GrandTotalForCurrency".ToUpper() /*|| x.Name.ToUpper() == "Close-In Booking Fee".ToUpper()*/ : true)).Select(x => x.Amount).ToList().Sum()
                            : flightReservationResponse.ShoppingCart.Items.SelectMany(x => x.Product).Where(x => x.Price != null).SelectMany(x => x.Price.Totals).Where(x => (x.Name != null ? x.Name.ToUpper() == "GrandTotalForCurrency".ToUpper() /*|| x.Name.ToUpper() == "Close-In Booking Fee".ToUpper()*/ : true)).Select(x => x.Amount).ToList().Sum();
                        break;

                    case "POSTBOOKING":
                        //productCodes = flightReservationResponseShoppingCart.Items.SelectMany(x => x.Product).Where(x => x.Characteristics != null && (x.Characteristics.Any(y => y.Description.ToUpper() == "POSTPURCHASE" && Convert.ToBoolean(y.Value) == true))).Select(x => x.Code).ToList();
                        shoppingCartTotalPrice = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart.Items.Where(x => !ShopStaticUtility.CheckFailedShoppingCartItem(flightReservationResponse, x)).Where(x => (x.Product.FirstOrDefault().Price != null ? (x.Product.FirstOrDefault().Price.Totals.Any()) : false) && x.Product.FirstOrDefault().Characteristics != null && (x.Product.FirstOrDefault().Characteristics.Any(y => y.Description == "PostPurchase" && Convert.ToBoolean(y.Value) == true))).Select(x => x.Product.FirstOrDefault().Price.Totals.FirstOrDefault().Amount).ToList().Sum()
                            : flightReservationResponse.ShoppingCart.Items.Where(x => (x.Product.FirstOrDefault().Price != null ? (x.Product.FirstOrDefault().Price.Totals.Any()) : false) && x.Product.FirstOrDefault().Characteristics != null && (x.Product.FirstOrDefault().Characteristics.Any(y => y.Description == "PostPurchase" && Convert.ToBoolean(y.Value) == true))).Select(x => x.Product.FirstOrDefault().Price.Totals.FirstOrDefault().Amount).ToList().Sum(); ;
                        break;

                    case "FARELOCK":
                        shoppingCartTotalPrice = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart.Items.Where(x => !ShopStaticUtility.CheckFailedShoppingCartItem(flightReservationResponse, x)).Where(x => x.Product.FirstOrDefault().Code == "FLK" && (x.Product.FirstOrDefault().Price != null ? (x.Product.FirstOrDefault().Price.Totals.Any()) : false)).Select(x => x.Product.FirstOrDefault().Price.Totals.FirstOrDefault().Amount).ToList().Sum()
                            : flightReservationResponse.ShoppingCart.Items.Where(x => x.Product.FirstOrDefault().Code == "FLK" && (x.Product.FirstOrDefault().Price != null ? (x.Product.FirstOrDefault().Price.Totals.Any()) : false)).Select(x => x.Product.FirstOrDefault().Price.Totals.FirstOrDefault().Amount).ToList().Sum();
                        break;

                    case "VIEWRES":
                    case "CHECKIN":
                        shoppingCartTotalPrice = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart.Items.Where(x => !ShopStaticUtility.CheckFailedShoppingCartItem(flightReservationResponse, x)).Where(x => x.Product.FirstOrDefault().Code != "RES" && (x.Product.FirstOrDefault().Price != null ? (x.Product.FirstOrDefault().Price.Totals.Any()) : false)).Select(x => x.Product.FirstOrDefault().Price.Totals.FirstOrDefault().Amount).ToList().Sum()
                            : flightReservationResponse.ShoppingCart.Items.Where(x => x.Product.FirstOrDefault().Code != "RES" && (x.Product.FirstOrDefault().Price != null ? (x.Product.FirstOrDefault().Price.Totals.Any()) : false)).Select(x => x.Product.FirstOrDefault().Price.Totals.FirstOrDefault().Amount).ToList().Sum();
                        break;
                }
            }
            if (_configuration.GetValue<bool>("CFOP19HBugFixToggle"))
            {
                shoppingCartTotalPrice = shoppingCartTotalPrice + closeBookingFee;
            }
            return shoppingCartTotalPrice;
        }
        public bool EnableEPlusAncillary(int appID, string appVersion, bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableEPlusAncillaryChanges") && !isReshop && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("EplusAncillaryAndroidversion"), _configuration.GetValue<string>("EplusAncillaryiOSversion"));
        }

        #region SeatMap
        public bool VersionCheck_NullSession_AfterAppUpgradation(MOBRequest request)
        {

            bool isVersionGreaterorEqual = GeneralHelper.IsApplicationVersionGreater2(request.Application.Id, request.Application.Version.Major, "Android_NullSession_AfterUpgradation_AppVersion", "iPhone_NullSession_AfterUpgradation_AppVersion", null, null, _configuration);
            return isVersionGreaterorEqual;
        }

        public bool EnableUMNRInformation(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "iPhoneUMNRInformationVersion", "AndroidUMNRInformationVersion", "", "", true, _configuration);
        }

        public bool EnableNewChangeSeatCheckinWindowMsg(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidNewChangeSeatCheckinWindowMsg", "iPhoneNewChangeSeatCheckinWindowMsg", "", "", true, _configuration);
        }

        public bool IsEnableXmlToCslSeatMapMigration(int appId, string appVersion)
        {
            if (!string.IsNullOrEmpty(appVersion) && appId != -1)
            {
                return _configuration.GetValue<bool>("SwithToCSLSeatMapChangeSeats")
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidXmlToCslSMapVersion", "iPhoneXmlToCslSMapVersion", "", "", true, _configuration);
            }
            return false;
        }

        public bool EnableLufthansaForHigherVersion(string operatingCarrierCode, int applicationId, string appVersion)
        {
            return EnableLufthansa(operatingCarrierCode) &&
                                    GeneralHelper.IsApplicationVersionGreater(applicationId, appVersion, "Android_EnableInterlineLHRedirectLinkManageRes_AppVersion", "iPhone_EnableInterlineLHRedirectLinkManageRes_AppVersion", "", "", true, _configuration);

        }
        public bool EnableLufthansa(string operatingCarrierCode)
        {

            return _configuration.GetValue<bool>("EnableInterlineLHRedirectLinkManageRes")
                                    && _configuration.GetValue<string>("InterlineLHAndParternerCode").Contains(operatingCarrierCode?.ToUpper());
        }

        public string BuildInterlineRedirectLink(MOBRequest mobRequest, string recordLocator, string lastname, string pointOfSale, string operatingCarrierCode)
        {
            string interlineLhRedirectUrl = string.Empty;

            //this condition for LH only 
            if (_configuration.GetValue<string>("InterlineLHAndParternerCode").Contains(operatingCarrierCode))
            {
                if (GeneralHelper.IsApplicationVersionGreater(mobRequest.Application.Id, mobRequest.Application.Version.Major, "Android_EnableInterlineLHRedirectLinkManageRes_AppVersion", "iPhone_EnableInterlineLHRedirectLinkManageRes_AppVersion", "", "", true, _configuration))
                {
                    //validate the LH and CL 
                    string lufthansaLink = CreateLufthansaDeeplink(recordLocator, lastname, pointOfSale, mobRequest.LanguageCode);

                    interlineLhRedirectUrl = _configuration.GetValue<string>("InterlinLHHtmlText").Replace("{lufthansaLink}", lufthansaLink);
                }
                else
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("SeatMapUnavailableOtherAirlines"));
                }
            }
            return interlineLhRedirectUrl;
        }
        public string CreateLufthansaDeeplink(string recordLocator, string lastName, string countryCode, string languageCode)
        {
            var stringToEncrypt = string.Format("Mode=S&Filekey={0}&Lastname={1}&Page=BKGD", recordLocator, lastName); ;
            var _cryptoKey = "528B47E01C257B43DB88ABCE62CC7F5A";
            var aesEncryption = new System.Security.Cryptography.RijndaelManaged
            {
                KeySize = 128,
                BlockSize = 128,
                Mode = System.Security.Cryptography.CipherMode.CBC,
                Padding = System.Security.Cryptography.PaddingMode.PKCS7,
                Key = ShopStaticUtility.StringToByteArray(_cryptoKey)
            };

            System.Security.Cryptography.ICryptoTransform crypto = aesEncryption.CreateEncryptor();
            byte[] plainText = ASCIIEncoding.UTF8.GetBytes(stringToEncrypt);

            // The result of the encryption
            byte[] cipherText = crypto.TransformFinalBlock(plainText, 0, stringToEncrypt.Length);
            var encrypted = ShopStaticUtility.ByteArrayToString(cipherText);
            return string.Format(_configuration.GetValue<string>("InterlineLHRedirectLink"), countryCode, languageCode, encrypted);
        }


        public bool EnablePcuDeepLinkInSeatMap(int appId, string appVersion)
        {
            if (!string.IsNullOrEmpty(appVersion) && appId != -1)
            {
                return _configuration.GetValue<bool>("EnablePcuDeepLinkInSeatMap")
               && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidPcuDeepLinkInSeatMapVersion", "iPhonePcuDeepLinkInSeatMapVersion", "", "", true, _configuration);
            }
            return false;
        }

        public bool IsTokenMiddleOfFlowDPDeployment()
        {
            return (_configuration.GetValue<bool>("ShuffleVIPSBasedOnCSS_r_DPTOken") && _configuration.GetValue<bool>("EnableDpToken")) ? true : false;

        }
        public string ModifyVIPMiddleOfFlowDPDeployment(string token, string url)
        {
            url = token.Length < 50 ? url.Replace(_configuration.GetValue<string>("DPVIPforDeployment"), _configuration.GetValue<string>("CSSVIPforDeployment")) : url;
            return url;
        }
        #endregion

        public bool CheckEPlusSeatCode(string program)
        {
            if (_configuration.GetValue<string>("EPlusSeatProgramCodes") != null)
            {
                string[] codes = _configuration.GetValue<string>("EPlusSeatProgramCodes").Split('|');
                foreach (string code in codes)
                {
                    if (code.Equals(program))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public string SpecialcharacterFilterInPNRLastname(string stringTofilter)
        {
            try
            {
                if (!string.IsNullOrEmpty(stringTofilter))
                {
                    Regex regex = new Regex(_configuration.GetValue<string>("SpecialcharactersFilterInPNRLastname"));
                    return regex.Replace(stringTofilter, string.Empty);
                }
                else
                    return stringTofilter;
            }
            catch (Exception ex) { return stringTofilter; }
        }

        public bool EnableActiveFutureFlightCreditPNR(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "iPhoneActiveFutureFlightCreditPNRVersion", "AndroidActiveFutureFlightCreditPNRVersion", "", "", true, _configuration);
        }

        public string GetCurrencyCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return "";

            string currencyCodeMappings = _configuration.GetValue<string>("CurrencyCodeMappings");

            var ccMap = currencyCodeMappings.Split('|');
            string currencyCode = string.Empty;

            foreach (var item in ccMap)
            {
                if (item.Split('=')[0].Trim() == code.Trim())
                {
                    currencyCode = item.Split('=')[1].Trim();
                    break;
                }
            }
            if (string.IsNullOrEmpty(currencyCode))
                return code;
            else
                return currencyCode;
        }
        public bool EnableFareLockPurchaseViewRes(int appId, string appVersion)
        {
            if (!string.IsNullOrEmpty(appVersion) && appId != -1)
            {
                return _configuration.GetValue<bool>("EnableFareLockPurchaseViewRes")
               && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidFareLockPurchaseViewResVersion", "iPhoneFareLockPurchaseViewResVersion", "", "", true, _configuration);
            }
            return false;
        }
        public void GetCheckInEligibilityStatusFromCSLPnrReservation(System.Collections.ObjectModel.Collection<United.Service.Presentation.CommonEnumModel.CheckinStatus> checkinEligibilityList, ref MOBPNR pnr)
        {
            bool SegmentFlownCheckToggle = Convert.ToBoolean(_configuration.GetValue<string>("SegmentFlownCheckToggle") ?? "false");

            pnr.CheckInStatus = "0";
            pnr.IrrOps = false;
            pnr.IrrOpsViewed = false;

            if (checkinEligibilityList != null && (pnr.Segments != null && pnr.Segments.Count > 0))
            {
                int hours = Convert.ToInt32(_configuration.GetValue<string>("PNRStatusLeadHours")) * -1;
                bool isNotFlownSegmentExist = IsNotFlownSegmentExist(pnr.Segments, hours, SegmentFlownCheckToggle);

                if ((!SegmentFlownCheckToggle && Convert.ToDateTime((pnr.Segments[0].ScheduledDepartureDateTimeGMT)).AddHours(hours) < DateTime.UtcNow) ||
                    (SegmentFlownCheckToggle && isNotFlownSegmentExist))
                {
                    foreach (var checkinEligibility in checkinEligibilityList)
                    {
                        if (checkinEligibility == Service.Presentation.CommonEnumModel.CheckinStatus.AlreadyCheckedin)
                        {
                            pnr.CheckInStatus = "2"; //"AlreadyCheckedin";
                        }
                        else if (checkinEligibility == Service.Presentation.CommonEnumModel.CheckinStatus.CheckinEligible)
                        {
                            pnr.CheckInStatus = "1"; //"CheckInEligible";
                        }
                        else if (checkinEligibility == Service.Presentation.CommonEnumModel.CheckinStatus.IRROPS)
                        {
                            pnr.IrrOps = true; //"IRROPS";
                        }
                        else if (checkinEligibility == Service.Presentation.CommonEnumModel.CheckinStatus.IRROPS_VIEWED)
                        {
                            pnr.IrrOpsViewed = true; //"IRROPS_VIEWED";
                        }
                    }
                }
            }
        }
        private bool IsNotFlownSegmentExist(List<MOBPNRSegment> segments, int hours, bool SegmentFlownCheckToggle)
        {
            bool isNotFlownSegmentExist = false;
            try
            {
                if (SegmentFlownCheckToggle)
                {
                    string segmentTicketCouponStatusCodes = (_configuration.GetValue<string>("SegmentTicketCouponStatusCodes") ?? "");
                    isNotFlownSegmentExist = segments.Exists(segment => (string.IsNullOrEmpty(segment.TicketCouponStatus) ||
                                                                            (segmentTicketCouponStatusCodes != string.Empty &&
                                                                             !string.IsNullOrEmpty(segment.TicketCouponStatus) &&
                                                                             !segmentTicketCouponStatusCodes.Contains(segment.TicketCouponStatus)
                                                                            )
                                                                        ) &&
                                                                        Convert.ToDateTime((segment.ScheduledDepartureDateTimeGMT)).AddHours(hours) < DateTime.UtcNow);
                }
            }
            catch
            {
                isNotFlownSegmentExist = false;
            }
            return isNotFlownSegmentExist;
        }

        public bool IsELFFare(string productCode)
        {
            return EnableIBEFull() && !string.IsNullOrWhiteSpace(productCode) &&
                   "ELF" == productCode.Trim().ToUpper();
        }
        public string[] SplitConcatenatedConfigValue(string configkey, string splitchar)
        {
            try
            {
                string[] splitSymbol = { splitchar };
                string[] splitString = _configuration.GetValue<string>(configkey)
                    .Split(splitSymbol, StringSplitOptions.None);
                return splitString;
            }
            catch { return null; }
        }

        public bool EnablePetInformation(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "iPhonePetInformationVersion", "AndroidPetInformationVersion", "", "", true, _configuration);
        }
        public MOBPNRAdvisory PopulateTRCAdvisoryContent(string displaycontent)
        {
            try
            {
                string[] stringarray
                    = SplitConcatenatedConfigValue("ManageResTRCContent", "||");

                if (stringarray == null || !stringarray.Any()) return null;

                MOBPNRAdvisory content = new MOBPNRAdvisory();

                stringarray.ForEach(item =>
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        string[] lineitem = ShopStaticUtility.SplitConcatenatedString(item, "|");

                        if (lineitem?.Length > 1)
                        {
                            switch (lineitem[0])
                            {
                                case "Header":
                                    content.Header = lineitem[1];
                                    break;
                                case "Body":
                                    content.Body = lineitem[1];
                                    break;
                                case "ButtonText":
                                    content.Buttontext = lineitem[1];
                                    break;
                            }
                        }
                    }
                });
                return content;
            }
            catch { return null; }
        }

        public bool IncludeTRCAdvisory(MOBPNR pnr, int appId, string appVersion)
        {
            try
            {
                if (!GeneralHelper.IsApplicationVersionGreaterorEqual
                    (appId, appVersion, _configuration.GetValue<string>("AndroidTRCAdvisoryVersion"), _configuration.GetValue<string>("iPhoneTRCAdvisoryVersion"))) return false;

                if (!string.IsNullOrEmpty(pnr.FareLockMessage) || pnr.isgroup || !pnr.IsETicketed
                    || pnr.IsPetAvailable || pnr.HasScheduleChanged || pnr.IsCanceledWithFutureFlightCredit) return false;

                return true;
            }
            catch { return false; }
        }
        public bool CheckMax737WaiverFlight
            (United.Service.Presentation.ReservationResponseModel.PNRChangeEligibilityResponse changeEligibilityResponse)
        {
            if (changeEligibilityResponse == null
                || changeEligibilityResponse.Policies == null
                || !changeEligibilityResponse.Policies.Any()) return false;

            foreach (var policies in changeEligibilityResponse.Policies)
            {
                var max737flightnames = GetListFrmPipelineSeptdConfigString("max737flightnames");
                string flightname = (!string.IsNullOrEmpty(policies.Name)) ? policies.Name.ToUpper() : string.Empty;
                if (max737flightnames != null && max737flightnames.Any() && !string.IsNullOrEmpty(flightname))
                {
                    foreach (string name in max737flightnames)
                    {
                        if (flightname.Contains(name))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public List<string> GetListFrmPipelineSeptdConfigString(string configkey)
        {
            try
            {
                var retstrarray = new List<string>();
                var configstring = _configuration.GetValue<string>(configkey);
                if (!string.IsNullOrEmpty(configstring))
                {
                    string[] strarray = configstring.Split('|');
                    if (strarray.Any())
                    {
                        strarray.ToList().ForEach(str =>
                        {
                            if (!string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str))
                                retstrarray.Add(str.Trim());
                        });
                    }
                }
                return (retstrarray.Any()) ? retstrarray : null;
            }
            catch { return null; }
        }

        public void OneTimeSCChangeCancelAlert(MOBPNR pnr)
        {
            try
            {
                if (pnr.IsSCChangeEligible)
                {
                    string[] onetimecontent;

                    if (pnr.IsSCRefundEligible)
                        onetimecontent = SplitConcatenatedConfigValue("ResDtlSCOneTimeChangeCancel", "||");
                    else
                        onetimecontent = SplitConcatenatedConfigValue("ResDtlSCOneTimeChange", "||");

                    if (onetimecontent?.Length == 2)
                    {
                        MOBPNRAdvisory sconetimechangecanceladvisory = new MOBPNRAdvisory
                        {
                            ContentType = ContentType.SCHEDULECHANGE,
                            AdvisoryType = AdvisoryType.INFORMATION,
                            Header = onetimecontent[0],
                            Body = onetimecontent[1],
                            IsBodyAsHtml = true,
                            IsDefaultOpen = false,
                        };
                        pnr.AdvisoryInfo = (pnr.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : pnr.AdvisoryInfo;
                        pnr.AdvisoryInfo.Add(sconetimechangecanceladvisory);
                    }
                }
            }
            catch { }
        }

        public string GetCurrencyAmount(double value = 0, string code = "USD", int decimalPlace = 2, string languageCode = "")
        {

            string isNegative = value < 0 ? "- " : "";
            double amount = Math.Abs(value);

            if (string.IsNullOrEmpty(code))
                code = "USD";

            string currencyCode = GetCurrencyCode(code);

            //Handle the currency code which is not in the app setting key - CurrencyCodeMappings
            if (string.IsNullOrEmpty(currencyCode))
                currencyCode = code;

            double.TryParse(amount.ToString(), out double total);
            string currencyAmount = "";

            if (string.Equals(currencyCode, "Miles", StringComparison.OrdinalIgnoreCase))
            {
                currencyAmount = string.Format("{0} {1}", total.ToString("#,##0"), currencyCode);
            }
            else if (languageCode == "")
            {
                currencyAmount = string.Format("{0}{1}{2}", isNegative, currencyCode, total.ToString("N" + decimalPlace));
            }
            else
            {
                CultureInfo locCutlure = new CultureInfo(languageCode);
                Thread.CurrentThread.CurrentCulture = locCutlure;
                NumberFormatInfo LocalFormat = (NumberFormatInfo)NumberFormatInfo.CurrentInfo.Clone();
                LocalFormat.CurrencySymbol = currencyCode;
                LocalFormat.CurrencyDecimalDigits = 2;

                currencyAmount = string.Format("{0}{1}", isNegative, amount.ToString("c", LocalFormat));

            }

            return currencyAmount;
        }
        public bool EnableConsolidatedAdvisoryMessage(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "iPhoneConsolidatedAdvisoryMessageVersion", "AndroidConsolidatedAdvisoryMessageVersion", "", "", true, _configuration);
        }

        public bool CheckIfTicketedByUA(ReservationDetail response)
        {
            if (response?.Detail?.Characteristic == null) return false;
            string configbookingsource = _configuration.GetValue<string>("PNRUABookingSource");
            var charbookingsource = ShopStaticUtility.GetCharactersticDescription_New(response.Detail.Characteristic, "Booking Source");
            if (string.IsNullOrEmpty(configbookingsource) || string.IsNullOrEmpty(charbookingsource)) return false;
            return (configbookingsource.IndexOf(charbookingsource, StringComparison.OrdinalIgnoreCase) > -1);
        }
        public bool DisableFSRAlertMessageTripPlan(int appId, string appVersion, string travelType)
        {
            return _configuration.GetValue<bool>("EnableAllAirportsOrNearByAirportsAlertOff") && IsTripPlanSearch(travelType) && !GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("AndroidAllAirportsOrNearByAirportsAlertOffVersion"), _configuration.GetValue<string>("iOSAllAirportsOrNearByAirportsAlertOffVersion"));
        }
        private bool IsTripPlanSearch(string travelType)
        {
            return _configuration.GetValue<bool>("EnableTripPlannerView") && (travelType == MOBTripPlannerType.TPSearch.ToString() || travelType == MOBTripPlannerType.TPEdit.ToString()
                || travelType == MOBTripPlannerType.TPBooking.ToString());
        }
        public bool IsBuyMilesFeatureEnabled(int appId, string version, List<MOBItem> catalogItems = null, bool isNotSelectTripCall = false)
        {
            if (_configuration.GetValue<bool>("EnableBuyMilesFeature") == false) return false;
            if ((catalogItems != null && catalogItems.Count > 0 &&
                   catalogItems.FirstOrDefault(a => a.Id == _configuration.GetValue<string>("Android_EnableBuyMilesFeatureCatalogID") || a.Id == _configuration.GetValue<string>("iOS_EnableBuyMilesFeatureCatalogID"))?.CurrentValue == "1")
                   || isNotSelectTripCall)
                return GeneralHelper.IsApplicationVersionGreaterorEqual(appId, version, _configuration.GetValue<string>("Android_BuyMilesFeatureSupported_AppVersion"), _configuration.GetValue<string>("IPhone_BuyMilesFeatureSupported_AppVersion"));
            else
                return false;

        }
        public async Task<List<MOBOptimizelyQMData>> ValidateAwardFSR(MOBSHOPShopRequest request)
        {
            List<MOBOptimizelyQMData> qmDataList = null;
            try
            {
                // to enabled 100 % FSR Award EnableAwardFSRExperiment to false
                if (request.IsReshopChange == false && request.AwardTravel && _configuration.GetValue<bool>("EnableAwardFSRExperiment")
                    && request.Experiments != null && request.Experiments.Any() && request.Experiments.Contains(ShoppingExperiments.FSRRedesignAward.ToString())
                    && IsAwardFSRRedesignEnabled(request.Application.Id, request.Application.Version.Major))
                {
                    var experimentProvider = new ExperimentProvider(_logger, _configuration, _cachingService, _optimizelyPersistService, _headers, request.Experiments.Contains("RefreshOptimizely"));

                    var qmData = experimentProvider.ApplyExperiment(request.DeviceId, _configuration.GetValue<string>("AwardFSRExperimentKey"), request.Application.Id.ToString(), request.Application.Version.Major);
                    if (qmData == null || qmData.Variation == null || qmData.Variation == "control")
                    {
                        _logger.LogWarning("ValidateAwardFSR No qmData");
                        //Remove experiment FSRRedesignAward when app version is lower or EnableAwardFSRChanges flag is off
                        request.Experiments?.Remove(ShoppingExperiments.FSRRedesignAward.ToString());

                    }
                    if (qmData != null && qmData.Variation != null)
                    {
                        qmDataList = new List<MOBOptimizelyQMData>();
                        qmDataList.Add(qmData);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ValidateAwardFSR No qmData Exception {@Exception}", JsonConvert.SerializeObject(ex));
                request.Experiments?.Remove(ShoppingExperiments.FSRRedesignAward.ToString());
            }
            return await System.Threading.Tasks.Task.FromResult(qmDataList);
        }
        public bool IsAwardFSRRedesignEnabled(int appId, string appVersion)
        {
            if (!_configuration.GetValue<bool>("EnableAwardFSRChanges")) return false;
            return GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("AndroidAwardFSRChangesVersion"), _configuration.GetValue<string>("iOSAwardFSRChangesVersion"));
        }
        public bool IsSortDisclaimerForNewFSR(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreater
                (appId, appVersion, "AndroidNewSortDisclaimerVersion", "iPhoneNewSortDisclaimerVersion", "", "", true, _configuration);
        }
        public bool EnableAdvanceSearchCouponBooking(MOBSHOPShopRequest request)
        {
            return (request != null && _configuration.GetValue<bool>("EnableAdvanceSearchCouponBooking") && !(request.IsReshop || request.IsReshopChange)
              && !request.IsCorporateBooking && string.IsNullOrEmpty(request.EmployeeDiscountId)
              && request.Experiments != null && request.Experiments.Any() && request.Experiments.Contains(ShoppingExperiments.FSRRedesignA.ToString())
              && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major,
                _configuration.GetValue<string>("AndroidAdvanceSearchCouponBookingVersion"),
                _configuration.GetValue<string>("iPhoneAdvanceSearchCouponBookingVersion"))
              && !request.AwardTravel) && !request.IsYoungAdultBooking;
        }

        public async Task<bool> Authorize(string sessionId, int applicationId, string applicationVersion, string deviceId, string mileagePlusNumber, string hash)
        {
            bool validateMPHashpinAuthorize = false;
            string validAuthToken = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(mileagePlusNumber) && !string.IsNullOrEmpty(deviceId) && !string.IsNullOrEmpty(hash))
                {

                    if (await ValidateMileagePlusRecordInCouchbase(sessionId, mileagePlusNumber, hash, deviceId, applicationId, applicationVersion))
                    {
                        validateMPHashpinAuthorize = true;
                    }
                    else if (await ValidateHashPinAndGetAuthToken(mileagePlusNumber, hash, applicationId, deviceId, applicationId.ToString()))
                    {
                        validateMPHashpinAuthorize = true;

                    }
                }

                return validateMPHashpinAuthorize;
            }
            catch (Exception ex)
            {
                _logger.LogInformation("AuthorizingCustomer {@Exception}", JsonConvert.SerializeObject(ex));
                return validateMPHashpinAuthorize;
            }

        }
        private async Task<bool> ValidateMileagePlusRecordInCouchbase(string sessionId, string mileagePlusNumber, string hash, string deviceId, int applicationId, string applicationVersion)
        {
            bool validateMPHashpin = false;

            try
            {
                if (!string.IsNullOrEmpty(mileagePlusNumber) && !string.IsNullOrEmpty(deviceId))
                {
                    string mileagePlusNumberKey = GetMileagePlusAuthorizationPredictableKey(mileagePlusNumber, deviceId, applicationId);

                    var customerAuthorizationRecord = await _cachingService.GetCache<CustomerAuthorization>(mileagePlusNumberKey, _headers.ContextValues.TransactionId).ConfigureAwait(false);
                    if (customerAuthorizationRecord != null)
                    {
                        var mileagePlusAuthorizationRecord = JsonConvert.DeserializeObject<CustomerAuthorization>(customerAuthorizationRecord);

                        if (mileagePlusAuthorizationRecord != null && !string.IsNullOrEmpty(mileagePlusAuthorizationRecord.Hash) && mileagePlusAuthorizationRecord.Hash.Equals(hash))
                        {
                            validateMPHashpin = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogInformation("ValidateMPHashDeviceIDCheckInCouchbase {@Exception}", JsonConvert.SerializeObject(ex));
            }

            return validateMPHashpin;
        }
        private string GetMileagePlusAuthorizationPredictableKey(string mileagePlus, string deviceId, int applicationId)
        {
            return string.Format("MileagePlusAuthorization::{0}@{1}::{2}", mileagePlus, deviceId, applicationId);
        }
        public bool EnableReShopAirfareCreditDisplay(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableReShopAirfareCreditDisplay")
           && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidEnableReShopAirfareCreditDisplayVersion", "iPhoneEnableReShopAirfareCreditDisplayVersion", "", "", true, _configuration);
        }
        public async Task<List<MOBMobileCMSContentMessages>> GetProductBasedTermAndConditions(string sessionId, FlightReservationResponse flightReservationResponse, bool isPost)
        {
            var productCodes = isPost ? flightReservationResponse.CheckoutResponse.ShoppingCart.Items.Where(x => x.Product.FirstOrDefault().Code != "RES").Select(x => x.Product.FirstOrDefault().Code).ToList() :
                                        flightReservationResponse.ShoppingCart.Items.Where(x => x.Product.FirstOrDefault().Code != "RES").Select(x => x.Product.FirstOrDefault().Code).ToList();

            if (productCodes == null || !productCodes.Any())
                return null;

            List<MOBMobileCMSContentMessages> tNClist = new List<MOBMobileCMSContentMessages>();
            MOBMobileCMSContentMessages tNC = null;
            List<MOBTypeOption> typeOption = null;
            productCodes = OrderPCUTnC(productCodes);

            foreach (var productCode in productCodes)
            {
                if (isPost == false)
                {
                    switch (productCode)
                    {
                        case "PCU":
                            tNC = new MOBMobileCMSContentMessages();
                            List<MOBMobileCMSContentMessages> tncPCU = await GetTermsAndConditions();
                            tNC.Title = "Terms and conditions";
                            tNC.ContentShort = _configuration.GetValue<string>("PaymentTnCMessage");
                            tNC.ContentFull = tncPCU[0].ContentFull;
                            tNC.HeadLine = tncPCU[0].Title;
                            tNClist.Add(tNC);
                            break;

                        case "PAS":
                            tNC = new MOBMobileCMSContentMessages();
                            typeOption = new List<MOBTypeOption>();
                            typeOption = GetPATermsAndConditionsList();

                            tNC.Title = "Terms and conditions";
                            tNC.ContentShort = _configuration.GetValue<string>("PaymentTnCMessage");
                            tNC.ContentFull = string.Join("<br><br>", typeOption.Select(x => x.Value));
                            tNC.HeadLine = "Premier Access";
                            tNClist.Add(tNC);
                            break;

                        case "PBS":
                            tNC = new MOBMobileCMSContentMessages();
                            typeOption = new List<MOBTypeOption>();
                            typeOption = GetPBContentList("PriorityBoardingTermsAndConditionsList");

                            tNC.Title = "Terms and conditions";
                            tNC.ContentShort = _configuration.GetValue<string>("PaymentTnCMessage");
                            tNC.ContentFull = "<ul><li>" + string.Join("<br></li><li>", typeOption.Select(x => x.Value)) + "</li></ul>";
                            tNC.HeadLine = "Priority Boarding";
                            tNClist.Add(tNC);
                            break;

                        case "TPI":
                            var productVendorOffer = new GetVendorOffers();
                            productVendorOffer = await _sessionHelperService.GetSession<GetVendorOffers>(sessionId, productVendorOffer.ObjectName, new List<string> { sessionId, productVendorOffer.ObjectName }).ConfigureAwait(false);
                            if (productVendorOffer == null)
                                break;

                            tNC = new MOBMobileCMSContentMessages();
                            var product = productVendorOffer.Offers.FirstOrDefault(a => a.ProductInformation.ProductDetails.Where(b => b.Product != null && b.Product.Code.ToUpper().Trim() == "TPI").ToList().Count > 0).
                                ProductInformation.ProductDetails.FirstOrDefault(c => c.Product != null && c.Product.Code.ToUpper().Trim() == "TPI").Product;

                            //string tncTPIMessage1 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCHeader1Message").Select(x => x.Body).FirstOrDefault().ToString();
                            //string tncTPIMessage2 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCBodyUrlMessage").Select(x => x.Body).FirstOrDefault().ToString();
                            //string tncTPIMessage3 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCUrlHeaderMessage").Select(x => x.Body).FirstOrDefault().ToString();
                            //string tncTPIMessage4 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCUrlHeader2Message").Select(x => x.Body).FirstOrDefault().ToString();
                            //string tncTPI = tncTPIMessage1 + " <a href =\"" + tncTPIMessage2 + "\" target=\"_blank\">" + tncTPIMessage3 + "</a> " + tncTPIMessage4;
                            string tncTPI = string.Empty;
                            string tncTPIMessage1 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCHeader1Message").Select(x => x.Body).FirstOrDefault().ToString();
                            string tncTPIMessage2 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCBodyUrlMessage").Select(x => x.Body).FirstOrDefault().ToString();
                            string tncTPIMessage3 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCUrlHeaderMessage").Select(x => x.Body).FirstOrDefault().ToString();
                            string tncTPIMessage4 = product.Presentation.Contents.Where(x => x.Header == "MobPaymentTAndCUrlHeader2Message").Select(x => x.Body).FirstOrDefault().ToString();
                            string tncTPIMessage5 = product.Presentation.Contents.Any(x => x.Header == "MobTIDetailsTAndCUrlMessage") ? product.Presentation.Contents.Where(x => x.Header == "MobTIDetailsTAndCUrlMessage").Select(x => x.Body).FirstOrDefault().ToString() : string.Empty;
                            string tncTPIMessage6 = product.Presentation.Contents.Any(x => x.Header == "MobTIDetailsTAndCUrlHeaderMessage") ? product.Presentation.Contents.Where(x => x.Header == "MobTIDetailsTAndCUrlHeaderMessage").Select(x => x.Body).FirstOrDefault().ToString() : string.Empty;
                            string tncTPIMessage7 = product.Presentation.Contents.Any(x => x.Header == "MobTGIAndMessage") ? product.Presentation.Contents.Where(x => x.Header == "MobTGIAndMessage").Select(x => x.Body).FirstOrDefault().ToString() : string.Empty;
                            if (string.IsNullOrEmpty(tncTPIMessage5) || string.IsNullOrEmpty(tncTPIMessage6) || string.IsNullOrEmpty(tncTPIMessage7))
                                tncTPI = tncTPIMessage1 + " <a href =\"" + tncTPIMessage2 + "\" target=\"_blank\">" + tncTPIMessage3 + "</a> " + tncTPIMessage4;
                            else
                                tncTPI = tncTPIMessage1 + " " + tncTPIMessage4 + " <a href =\"" + tncTPIMessage2 + "\" target=\"_blank\">" + tncTPIMessage3 + "</a> " + tncTPIMessage7 + " <a href =\"" + tncTPIMessage5 + "\" target=\"_blank\">" + tncTPIMessage6 + "</a> ";
                            tNC.Title = "Terms and conditions";
                            tNC.ContentShort = _configuration.GetValue<string>("PaymentTnCMessage");
                            tNC.ContentFull = tncTPI;
                            tNC.HeadLine = "Terms and conditions";
                            tNClist.Add(tNC);
                            break;
                        case "AAC":
                            var acceleratorTnCs = await GetTermsAndConditions(flightReservationResponse.DisplayCart.TravelOptions.Any(d => d.Key == "PAC"));
                            if (acceleratorTnCs != null && acceleratorTnCs.Any())
                            {
                                tNClist.AddRange(acceleratorTnCs);
                            }
                            break;
                        case "POM":
                            break;
                        case "SEATASSIGNMENTS":
                            if (string.IsNullOrEmpty(_configuration.GetValue<string>("EnablePCUSelectedSeatPurchaseViewRes")))
                                break;
                            var seatTypes = flightReservationResponse.DisplayCart.DisplaySeats.Where(s => s.SeatPrice > 0).Select(s => GetCommonSeatCode(s.SeatPromotionCode)).ToList();
                            var seatsTnCs = new List<MOBItem>();
                            if (seatTypes.Any() && seatTypes.Contains("ASA"))
                            {
                                var asaTncs = await GetCaptions("CFOP_UnitedTravelOptions_ASA_TnC");
                                if (asaTncs != null && asaTncs.Any())
                                {
                                    seatsTnCs.AddRange(asaTncs);
                                }
                            }
                            if (seatTypes.Any() && (seatTypes.Contains("EPU") || seatTypes.Contains("PSL")))
                            {
                                var eplusTncs = await GetCaptions("CFOP_UnitedTravelOptions_EPU_TnC");
                                if (eplusTncs != null && eplusTncs.Any())
                                {
                                    seatsTnCs.AddRange(eplusTncs);
                                }
                            }
                            if (seatTypes.Any() && seatTypes.Contains("PZA"))
                            {
                                var pzaTncs = await GetCaptions("CFOP_UnitedTravelOptions_PZA_TnC");
                                if (pzaTncs != null && pzaTncs.Any())
                                {
                                    seatsTnCs.AddRange(pzaTncs);
                                }
                            }

                            if (seatsTnCs.Any())
                            {
                                tNC = new MOBMobileCMSContentMessages
                                {
                                    Title = "Terms and conditions",
                                    ContentShort = _configuration.GetValue<string>("PaymentTnCMessage"),
                                    ContentFull = string.Join("<br>", seatsTnCs.Select(a => a.CurrentValue)),
                                    HeadLine = seatsTnCs[0].Id
                                };
                                tNClist.Add(tNC);
                            }
                            break;
                        case "BEB":
                            tNClist = await GetTermsAndConditions();
                            if (tNClist != null)
                            {
                                tNClist.Add(tNC);
                            }
                            break;
                    }
                }
                else if (isPost == true)
                {
                    switch (productCode)
                    {
                        case "TPI":
                            var productVendorOffer = new GetVendorOffers();
                            productVendorOffer = await _sessionHelperService.GetSession<GetVendorOffers>(sessionId, productVendorOffer.ObjectName, new List<string> { sessionId, productVendorOffer.ObjectName }).ConfigureAwait(false);
                            if (productVendorOffer == null)
                                break;

                            string specialCharacter = _configuration.GetValue<string>("TPIinfo-SpecialCharacter") ?? "";
                            tNC = new MOBMobileCMSContentMessages();
                            var product = productVendorOffer.Offers.FirstOrDefault(a => a.ProductInformation.ProductDetails.Where(b => b.Product != null && b.Product.Code.ToUpper().Trim() == "TPI").ToList().Count > 0).
                                ProductInformation.ProductDetails.FirstOrDefault(c => c.Product != null && c.Product.Code.ToUpper().Trim() == "TPI").Product;

                            string tncTPIMessage1 = product.Presentation.Contents.Where(x => x.Header == "MobTIConfirmationBody1Message").Select(x => x.Body).FirstOrDefault().ToString().Replace("(R)", specialCharacter);
                            string tncTPIMessage2 = product.Presentation.Contents.Where(x => x.Header == "MobTIConfirmationBody2Message").Select(x => x.Body).FirstOrDefault().ToString();

                            string tncTPI = tncTPIMessage1 + "\n\n" + tncTPIMessage2;

                            tNC.Title = _configuration.GetValue<string>("TPIPurchaseResposne-ConfirmationResponseMessage") ?? ""; ;
                            tNC.ContentShort = _configuration.GetValue<string>("TPIPurchaseResposne-ConfirmationResponseEmailMessage"); // + ((flightReservationResponse.Reservation.EmailAddress.Count() > 0) ? flightReservationResponse.Reservation.EmailAddress.Where(x => x.Address != null).Select(x => x.Address).FirstOrDefault().ToString() : null) ?? "";
                            tNC.ContentFull = tncTPI;
                            tNClist.Add(tNC);
                            break;
                    }
                }
            }

            if (!isPost && IsBundleProductSelected(flightReservationResponse))
            {
                tNC = new MOBMobileCMSContentMessages
                {
                    Title = "Terms and conditions",
                    ContentShort = _configuration.GetValue<string>("PaymentTnCMessage"),
                    HeadLine = "Travel Options bundle terms and conditions",
                    ContentFull = GetBundleTermsandConditons("bundlesTermsandConditons")?.Replace('?', '℠')
                };
                tNClist.Add(tNC);
            }

            return tNClist;
        }
        public InfoWarningMessages GetPriceMismatchMessage()
        {
            InfoWarningMessages infoPriceMessage = new InfoWarningMessages();

            infoPriceMessage.Order = MOBINFOWARNINGMESSAGEORDER.PRICECHANGE.ToString();
            infoPriceMessage.IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString();

            infoPriceMessage.Messages = new List<string>();
            infoPriceMessage.Messages.Add((_configuration.GetValue<string>("PriceMismatchMessage") as string) ?? string.Empty);

            return infoPriceMessage;
        }

        public List<MOBSHOPPrice> UpdatePricesForEFS(MOBSHOPReservation reservation, int appID, string appVersion, bool isReshop)
        {
            if (EnableEPlusAncillary(appID, appVersion, false) &&
                         reservation.TravelOptions != null &&
                         reservation.TravelOptions.Exists(t => t?.Key?.Trim()?.ToUpper() == "EFS"))
            {
                return UpdatePricesForBundles(reservation, null, appID, appVersion, isReshop, "EFS");
            }
            return reservation.Prices;
        }
        private List<MOBSHOPPrice> UpdatePricesForBundles(MOBSHOPReservation reservation, Mobile.Model.Shopping.RegisterOfferRequest request, int appID, string appVersion, bool isReshop, string productId = "")
        {
            List<MOBSHOPPrice> prices = reservation.Prices.Clone();

            if (reservation.TravelOptions != null && reservation.TravelOptions.Count > 0)
            {
                foreach (var travelOption in reservation.TravelOptions)
                {
                    //below if condition modified by prasad for bundle checking
                    //MOB-4676-Added condition to ignore the trip insurance which is added as traveloption - sandeep/saikiran
                    if (!travelOption.Key.Equals("PAS") && (!travelOption.Type.IsNullOrEmpty() && !travelOption.Type.ToUpper().Equals("TRIP INSURANCE"))
                        && (EnableEPlusAncillary(appID, appVersion, isReshop) ? !travelOption.Key.Trim().ToUpper().Equals("FARELOCK") : true)
                        && !(_configuration.GetValue<bool>("EnableEplusCodeRefactor") && !string.IsNullOrEmpty(productId) && productId.Trim().ToUpper() != travelOption.Key.Trim().ToUpper()))
                    {
                        List<MOBSHOPPrice> totalPrices = new List<MOBSHOPPrice>();
                        bool totalExist = false;
                        double flightTotal = 0;

                        CultureInfo ci = null;

                        for (int i = 0; i < prices.Count; ++i)
                        {
                            if (ci == null)
                                ci = TopHelper.GetCultureInfo(prices[i].CurrencyCode);

                            if (prices[i].DisplayType.ToUpper() == "GRAND TOTAL")
                            {
                                totalExist = true;
                                prices[i].DisplayValue = string.Format("{0:#,0.00}", (Convert.ToDouble(prices[i].DisplayValue) + travelOption.Amount));
                                prices[i].FormattedDisplayValue = TopHelper.FormatAmountForDisplay(prices[i].DisplayValue, ci, false); // string.Format("{0:c}", prices[i].DisplayValue);
                                double tempDouble1 = 0;
                                double.TryParse(prices[i].DisplayValue.ToString(), out tempDouble1);
                                prices[i].Value = Math.Round(tempDouble1, 2, MidpointRounding.AwayFromZero);
                            }
                            if (prices[i].DisplayType.ToUpper() == "TOTAL")
                            {
                                flightTotal = Convert.ToDouble(prices[i].DisplayValue);
                            }
                        }
                        MOBSHOPPrice travelOptionPrice = new MOBSHOPPrice();
                        travelOptionPrice.CurrencyCode = travelOption.CurrencyCode;
                        travelOptionPrice.DisplayType = "Travel Options";
                        travelOptionPrice.DisplayValue = string.Format("{0:#,0.00}", travelOption.Amount.ToString());
                        travelOptionPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(travelOptionPrice.DisplayValue, ci, false); //Convert.ToDouble(travelOptionPrice.DisplayValue).ToString("C2", CultureInfo.CurrentCulture);

                        if (_configuration.GetValue<bool>("EnableEplusCodeRefactor") && travelOption.Key?.Trim().ToUpper() == "EFS")
                        {
                            travelOptionPrice.PriceType = "EFS";
                        }
                        else
                        {
                            travelOptionPrice.PriceType = "Travel Options";
                        }

                        double tmpDouble1 = 0;
                        double.TryParse(travelOptionPrice.DisplayValue.ToString(), out tmpDouble1);
                        travelOptionPrice.Value = Math.Round(tmpDouble1, 2, MidpointRounding.AwayFromZero);

                        prices.Add(travelOptionPrice);

                        if (!totalExist)
                        {
                            MOBSHOPPrice totalPrice = new MOBSHOPPrice();
                            totalPrice.CurrencyCode = travelOption.CurrencyCode;
                            totalPrice.DisplayType = "Grand Total";
                            totalPrice.DisplayValue = (flightTotal + travelOption.Amount).ToString("N2", CultureInfo.InvariantCulture);
                            totalPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalPrice.DisplayValue, ci, false); //string.Format("${0:c}", totalPrice.DisplayValue);
                            double tempDouble1 = 0;
                            double.TryParse(totalPrice.DisplayValue.ToString(), out tempDouble1);
                            totalPrice.Value = Math.Round(tempDouble1, 2, MidpointRounding.AwayFromZero);
                            totalPrice.PriceType = "Grand Total";
                            prices.Add(totalPrice);
                        }
                    }
                }
            }
            if (request != null && request.ClubPassPurchaseRequest != null)
            {
                List<MOBSHOPPrice> totalPrices = new List<MOBSHOPPrice>();
                bool totalExist = false;
                double flightTotal = 0;

                CultureInfo ci = null;

                for (int i = 0; i < prices.Count; ++i)
                {
                    if (ci == null)
                        ci = TopHelper.GetCultureInfo(prices[i].CurrencyCode);

                    if (prices[i].DisplayType.ToUpper() == "GRAND TOTAL")
                    {
                        totalExist = true;
                        prices[i].DisplayValue = string.Format("{0:#,0.00}", Convert.ToDouble(prices[i].DisplayValue) + request.ClubPassPurchaseRequest.AmountPaid);
                        prices[i].FormattedDisplayValue = Convert.ToDouble(prices[i].DisplayValue).ToString("C2", CultureInfo.CurrentCulture);
                        double tempDouble1 = 0;
                        double.TryParse(prices[i].DisplayValue.ToString(), out tempDouble1);
                        prices[i].Value = Math.Round(tempDouble1, 2, MidpointRounding.AwayFromZero);
                    }
                    if (prices[i].DisplayType.ToUpper() == "TOTAL")
                    {
                        flightTotal = Convert.ToDouble(prices[i].DisplayValue);
                    }
                }
                MOBSHOPPrice otpPrice = new MOBSHOPPrice();
                otpPrice.CurrencyCode = prices[prices.Count - 1].CurrencyCode;
                otpPrice.DisplayType = "One-time Pass";
                otpPrice.DisplayValue = string.Format("{0:#,0.00}", request.ClubPassPurchaseRequest.AmountPaid);
                double tempDouble = 0;
                double.TryParse(otpPrice.DisplayValue.ToString(), out tempDouble);
                otpPrice.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);
                otpPrice.FormattedDisplayValue = request.ClubPassPurchaseRequest.AmountPaid.ToString("C2", CultureInfo.CurrentCulture);
                otpPrice.PriceType = "One-time Pass";
                if (totalExist)
                {
                    prices.Insert(prices.Count - 2, otpPrice);
                }
                else
                {
                    prices.Add(otpPrice);
                }

                if (!totalExist)
                {
                    MOBSHOPPrice totalPrice = new MOBSHOPPrice();
                    totalPrice.CurrencyCode = prices[prices.Count - 1].CurrencyCode;
                    totalPrice.DisplayType = "Grand Total";
                    totalPrice.DisplayValue = (flightTotal + request.ClubPassPurchaseRequest.AmountPaid).ToString("N2", CultureInfo.InvariantCulture);
                    //totalPrice.DisplayValue = string.Format("{0:#,0.00}", (flightTotal + request.ClubPassPurchaseRequest.AmountPaid).ToString("{0:#,0.00}", CultureInfo.InvariantCulture);
                    totalPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalPrice.DisplayValue, ci, false); //string.Format("${0:c}", totalPrice.DisplayValue);
                    double tempDouble1 = 0;
                    double.TryParse(totalPrice.DisplayValue.ToString(), out tempDouble1);
                    totalPrice.Value = Math.Round(tempDouble1, 2, MidpointRounding.AwayFromZero);
                    totalPrice.PriceType = "Grand Total";
                    prices.Add(totalPrice);
                }
            }
            return prices;
        }

        private List<string> OrderPCUTnC(List<string> productCodes)
        {
            if (productCodes == null || !productCodes.Any())
                return productCodes;

            return productCodes.OrderBy(p => GetProductOrderTnC()[GetProductTnCtoOrder(p)]).ToList();
        }
        private string GetCommonSeatCode(string seatCode)
        {
            if (string.IsNullOrEmpty(seatCode))
                return string.Empty;

            string commonSeatCode = string.Empty;

            switch (seatCode.ToUpper().Trim())
            {
                case "SXZ": //StandardPreferredExitPlus
                case "SZX": //StandardPreferredExit
                case "SBZ": //StandardPreferredBlukheadPlus
                case "SZB": //StandardPreferredBlukhead
                case "SPZ": //StandardPreferredZone
                case "PZA":
                    commonSeatCode = "PZA";
                    break;
                case "SXP": //StandardPrimeExitPlus
                case "SPX": //StandardPrimeExit
                case "SBP": //StandardPrimeBlukheadPlus
                case "SPB": //StandardPrimeBlukhead
                case "SPP": //StandardPrimePlus
                case "PPE": //StandardPrime
                case "BSA":
                case "ASA":
                    commonSeatCode = "ASA";
                    break;
                case "EPL": //EplusPrime
                case "EPU": //EplusPrimePlus
                case "BHS": //BulkheadPrime
                case "BHP": //BulkheadPrimePlus  
                case "PSF": //PrimePlus    
                    commonSeatCode = "EPU";
                    break;
                case "PSL": //Prime                           
                    commonSeatCode = "PSL";
                    break;
                default:
                    return seatCode;
            }
            return commonSeatCode;
        }
        private Dictionary<string, int> GetProductOrderTnC()
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) {
                    { "SEATASSIGNMENTS", 0 },
                    { "PCU", 1 },
                    { string.Empty, 2 } };
        }
        private string GetProductTnCtoOrder(string productCode)
        {
            productCode = string.IsNullOrEmpty(productCode) ? string.Empty : productCode.ToUpper().Trim();

            if (productCode == "SEATASSIGNMENTS" || productCode == "PCU")
                return productCode;

            return string.Empty;
        }
        private string GetBundleTermsandConditons(string databaseKeys)
        {
            string docTitles = "bundlesTermsandConditons";
            var docs = _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(docTitles, _headers.ContextValues.TransactionId, true).Result
                ;

            string message = string.Empty;
            if (docs != null && docs.Count > 0)
            {
                foreach (MOBLegalDocument doc in docs)
                {
                    message = doc.LegalDocument;
                }
            }
            return message;
        }
        private bool IsBundleProductSelected(FlightReservationResponse flightReservationResponse)
        {
            if (!_configuration.GetValue<bool>("EnableTravelOptionsBundleInViewRes"))
                return false;

            return flightReservationResponse?.ShoppingCart?.Items?.Where(x => x.Product?.FirstOrDefault()?.Code != "RES")?.Any(x => x.Product?.Any(p => p?.SubProducts?.Any(sp => sp?.GroupCode == "BE") ?? false) ?? false) ?? false;
        }
        private async Task<List<MOBMobileCMSContentMessages>> GetTermsAndConditions(bool hasPremierAccelerator)
        {

            var dbKey = _configuration.GetValue<bool>("EnablePPRChangesForAAPA") ? hasPremierAccelerator ? "PPR_AAPA_TERMS_AND_CONDITIONS_AA_PA_MP"
                                              : "PPR_AAPA_TERMS_AND_CONDITIONS_AA_MP" : hasPremierAccelerator ? "AAPA_TERMS_AND_CONDITIONS_AA_PA_MP"
                                              : "AAPA_TERMS_AND_CONDITIONS_AA_MP";

            var docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(dbKey, _headers.ContextValues.TransactionId, true).ConfigureAwait(false);
            if (docs == null || !docs.Any())
                return null;

            var tncs = new List<MOBMobileCMSContentMessages>();
            foreach (var doc in docs)
            {
                var tnc = new MOBMobileCMSContentMessages
                {
                    Title = "Terms and conditions",
                    ContentFull = doc.LegalDocument,
                    ContentShort = _configuration.GetValue<string>("PaymentTnCMessage"),
                    HeadLine = doc.Title
                };
                tncs.Add(tnc);
            }

            return tncs;
        }
        private List<MOBTypeOption> GetPBContentList(string configValue)
        {
            List<MOBTypeOption> contentList = new List<MOBTypeOption>();
            if (_configuration.GetValue<string>(configValue) != null)
            {
                string pBContentList = _configuration.GetValue<string>(configValue);
                foreach (string eachItem in pBContentList.Split('~'))
                {
                    contentList.Add(new MOBTypeOption(eachItem.Split('|')[0].ToString(), eachItem.Split('|')[1].ToString()));
                }
            }
            return contentList;
        }
        private List<MOBTypeOption> GetPATermsAndConditionsList()
        {
            List<MOBTypeOption> tAndCList = new List<MOBTypeOption>();
            if (_configuration.GetValue<string>("PremierAccessTermsAndConditionsList") != null)
            {
                string premierAccessTermsAndConditionsList = _configuration.GetValue<string>("PremierAccessTermsAndConditionsList");
                foreach (string eachItem in premierAccessTermsAndConditionsList.Split('~'))
                {
                    tAndCList.Add(new MOBTypeOption(eachItem.Split('|')[0].ToString(), eachItem.Split('|')[1].ToString()));
                }
            }
            else
            {
                #region
                tAndCList.Add(new MOBTypeOption("paTandC1", "This Premier Access offer is nonrefundable and non-transferable"));
                tAndCList.Add(new MOBTypeOption("paTandC2", "Voluntary changes to your itinerary may forfeit your Premier Access purchase and \n any associated fees."));
                tAndCList.Add(new MOBTypeOption("paTandC3", "In the event of a flight cancellation or involuntary schedule change, we will refund \n the fees paid for the unused Premier Access product upon request."));
                tAndCList.Add(new MOBTypeOption("paTandC4", "Premier Access is offered only on flights operated by United and United Express."));
                tAndCList.Add(new MOBTypeOption("paTandC5", "This Premier Access offer is processed based on availability at time of purchase."));
                tAndCList.Add(new MOBTypeOption("paTandC6", "Premier Access does not guarantee wait time in airport check-in, boarding, or security lines. Premier Access does not exempt passengers from check-in time limits."));
                tAndCList.Add(new MOBTypeOption("paTandC7", "Premier Access benefits apply only to the customer who purchased Premier Access \n unless purchased for all customers on a reservation. Each travel companion must purchase Premier Access in order to receive benefits."));
                tAndCList.Add(new MOBTypeOption("paTandC8", "“Premier Access” must be printed or displayed on your boarding pass in order to \n receive benefits."));
                tAndCList.Add(new MOBTypeOption("paTandC9", "This offer is made at United's discretion and is subject to change or termination \n at any time with or without notice to the customer."));
                tAndCList.Add(new MOBTypeOption("paTandC10", "By clicking “I agree - Continue to purchase” you agree to all terms and conditions."));
                #endregion
            }
            return tAndCList;
        }
        private async Task<List<MOBMobileCMSContentMessages>> GetTermsAndConditions()
        {
            var cmsContentMessages = new List<MOBMobileCMSContentMessages>();
            var docKeys = "PCU_TnC";
            var docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(docKeys, _headers.ContextValues.TransactionId, true).ConfigureAwait(false);

            if (docs != null && docs.Any())
            {
                foreach (var doc in docs)
                {
                    var cmsContentMessage = new MOBMobileCMSContentMessages();
                    cmsContentMessage.ContentFull = doc.LegalDocument;
                    cmsContentMessage.Title = doc.Title;
                    cmsContentMessages.Add(cmsContentMessage);
                }
            }

            return cmsContentMessages;
        }
        public string GetPaymentTargetForRegisterFop(TravelOptionsCollection travelOptions, bool isCompleteFarelockPurchase = false)
        {
            if (string.IsNullOrEmpty(_configuration.GetValue<string>("EnablePCUSelectedSeatPurchaseViewRes")))
                return string.Empty;

            if (isCompleteFarelockPurchase)
                return "RES";

            if (travelOptions == null || !travelOptions.Any())
                return string.Empty;

            return string.Join(",", travelOptions.Select(x => x.Type == "SEATASSIGNMENTS" ? x.Type : x.Key).Distinct());
        }
        public string GetBookingPaymentTargetForRegisterFop(FlightReservationResponse flightReservationResponse)
        {
            if (flightReservationResponse.ShoppingCart == null || !flightReservationResponse.ShoppingCart.Items.Any())
                return string.Empty;

            return string.Join(",", flightReservationResponse.ShoppingCart.Items.SelectMany(x => x.Product).Select(x => x.Code).Distinct());
        }
        private async System.Threading.Tasks.Task ClearSelectedFOPOnReprice(MOBShoppingCart shoppingCart, MOBSHOPReservation reservation)
        {
            string sessionId = reservation.SessionId;
            if (shoppingCart?.FormofPaymentDetails != null && (shoppingCart?.FormofPaymentDetails?.TravelFutureFlightCredit?.FutureFlightCredits?.Count > 0 || shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates?.Count > 0 || shoppingCart.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.Uplift.ToString() || shoppingCart.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.TB.ToString()))
            {
                if (shoppingCart?.FormofPaymentDetails?.TravelFutureFlightCredit?.FutureFlightCredits?.Count > 0)
                {
                    shoppingCart.FormofPaymentDetails.TravelFutureFlightCredit.FutureFlightCredits = null;
                }
                if (shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates != null && shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates.Count > 0)
                {
                    FOPTravelerCertificateResponse persistedTravelCertifcateResponse = new FOPTravelerCertificateResponse();
                    persistedTravelCertifcateResponse = await _sessionHelperService.GetSession<FOPTravelerCertificateResponse>(sessionId, persistedTravelCertifcateResponse.ObjectName, new List<string> { sessionId, persistedTravelCertifcateResponse.ObjectName }).ConfigureAwait(false);
                    if (persistedTravelCertifcateResponse != null)
                    {
                        persistedTravelCertifcateResponse.ShoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates?.Clear();
                        persistedTravelCertifcateResponse?.ShoppingCart?.CertificateTravelers?.Clear();
                        await _sessionHelperService.SaveSession<FOPTravelerCertificateResponse>(persistedTravelCertifcateResponse, sessionId, new List<string> { sessionId, persistedTravelCertifcateResponse.ObjectName }, persistedTravelCertifcateResponse.ObjectName);
                    }
                    shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates?.Clear();
                }
                if (shoppingCart.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.Uplift.ToString())
                {
                    shoppingCart.FormofPaymentDetails.Uplift = null;
                }
                if (shoppingCart.FormofPaymentDetails?.FormOfPaymentType == MOBFormofPayment.TB.ToString())
                {
                    shoppingCart.FormofPaymentDetails.TravelBankDetails = null;
                }
                shoppingCart.FormofPaymentDetails.FormOfPaymentType = MOBFormofPayment.CreditCard.ToString();
                AssignIsOtherFOPRequired(shoppingCart.FormofPaymentDetails, reservation.Prices);
            }
        }
        private void AssignIsOtherFOPRequired(MOBFormofPaymentDetails formofPaymentDetails, List<MOBSHOPPrice> prices)
        {
            var grandTotalPrice = prices?.FirstOrDefault(p => p?.DisplayType?.ToUpper()?.Equals("GRAND TOTAL") ?? false);
            if (grandTotalPrice != null)
                formofPaymentDetails.IsOtherFOPRequired = (grandTotalPrice.Value > 0);
        }
        public async Task<MOBShoppingCart> PopulateShoppingCart(MOBShoppingCart shoppingCart, string flow, string sessionId, string CartId, MOBRequest request = null, Mobile.Model.Shopping.MOBSHOPReservation reservation = null, bool isAddTravelerFlow = false)
        {
            shoppingCart = shoppingCart ?? new MOBShoppingCart();
            shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(sessionId, shoppingCart.ObjectName, new List<string> { sessionId, shoppingCart.ObjectName }).ConfigureAwait(false);
            if (shoppingCart == null)
                shoppingCart = new MOBShoppingCart();
            shoppingCart.CartId = CartId;
            shoppingCart.Flow = flow;
            if (isAddTravelerFlow)
            {
                await ClearSelectedFOPOnReprice(shoppingCart, reservation);
                if ((shoppingCart.PromoCodeDetails != null && shoppingCart?.PromoCodeDetails?.PromoCodes.Count > 0) || !string.IsNullOrEmpty(shoppingCart?.Offers?.OfferCode))
                {
                    shoppingCart.PromoCodeDetails = null;
                    shoppingCart.Offers = null;
                }
            }
            if (flow == FlowType.BOOKING.ToString() && request?.Application != null
               && IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, reservation?.ShopReservationInfo2?.IsDisplayCart == true)
               )
            {
                BuildOmniCart(shoppingCart, reservation);
            }
            await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, sessionId, new List<string> { sessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName).ConfigureAwait(false);
            return shoppingCart;
        }
        public void BuildOmniCart(MOBShoppingCart shoppingCart, MOBSHOPReservation reservation)
        {
            if (shoppingCart.OmniCart == null)
            {
                shoppingCart.OmniCart = new Cart();
            }
            shoppingCart.OmniCart.CartItemsCount = GetCartItemsCount(shoppingCart);
            shoppingCart.OmniCart.TotalPrice = GetTotalPrice(shoppingCart?.Products, reservation);
            shoppingCart.OmniCart.PayLaterPrice = GetPayLaterAmount(shoppingCart?.Products, reservation);
            shoppingCart.OmniCart.FOPDetails = GetFOPDetails(reservation);
            if (_configuration.GetValue<bool>("EnableShoppingCartPhase2Changes"))
            {
                shoppingCart.OmniCart.CostBreakdownFareHeader = GetCostBreakdownFareHeader(reservation?.ShopReservationInfo2?.TravelType, shoppingCart);
            }
            if (_configuration.GetValue<bool>("EnableLivecartForAwardTravel") && reservation.AwardTravel)
            {
                shoppingCart.OmniCart.AdditionalMileDetail = GetAdditionalMileDetail(reservation);
            }
            if (reservation != null && reservation.ShopReservationInfo2 != null && !string.IsNullOrEmpty(reservation.ShopReservationInfo2.CorporateDisclaimerText))
            {
                shoppingCart.OmniCart.CorporateDisclaimerText = reservation.ShopReservationInfo2.CorporateDisclaimerText;
            }
            AssignUpliftText(shoppingCart, reservation);                //Assign message text and link text to the Uplift
            if (_configuration.GetValue<bool>("EnableFSRMoneyPlusMilesFeature"))
            {
                try
                {
                    if (reservation?.Prices?.FirstOrDefault(p => p.DisplayType.Equals("MONEYPLUSMILES", StringComparison.OrdinalIgnoreCase)) != null && shoppingCart?.OmniCart?.TotalPrice?.CurrentValue != null)
                        shoppingCart.OmniCart.TotalPrice.CurrentValue += " " + reservation.Prices.FirstOrDefault(p => p.DisplayType.Equals("MONEYPLUSMILES", StringComparison.OrdinalIgnoreCase)).FormattedDisplayValue;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("BuildOmniCart FSRMoneyMiles error" + ex.Message);
                }
            }
            try
            {
                if (_configuration.GetValue<bool>("EnableFSRETCCreditsFeature") && reservation.Prices.Any(p => p.DisplayType.Equals("TravelCredits", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(p.DisplayValue)) == true && shoppingCart?.OmniCart?.TotalPrice?.CurrentValue != null)
                    shoppingCart.OmniCart.TotalPrice.CurrentValue += " " + reservation.Prices.FirstOrDefault(p => p.DisplayType.Equals("TRAVELCREDITS", StringComparison.OrdinalIgnoreCase)).FormattedDisplayValue;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("BuildOmniCart TravelCredits error" + ex.Message);
            }
        }

        private string GetCostBreakdownFareHeader(string travelType, MOBShoppingCart shoppingCart)
        {
            string fareHeader = "Fare";
            if (!string.IsNullOrEmpty(travelType))
            {
                travelType = travelType.ToUpper();
                if (travelType == TravelType.CB.ToString())
                {
                    fareHeader = "Corporate fare";
                }
                else if (travelType == TravelType.CLB.ToString())
                {
                    fareHeader = "Break from Business fare";
                }
                else if (shoppingCart?.Offers != null && !string.IsNullOrEmpty(shoppingCart.Offers.OfferCode))
                {
                    fareHeader = _configuration.GetValue<string>("OfferCodeFareText");
                }
            }
            return fareHeader;
        }

        private MOBSection GetAdditionalMileDetail(MOBSHOPReservation reservation)
        {
            var additionalMilesPrice = reservation?.Prices?.FirstOrDefault(price => string.Equals("MPF", price?.DisplayType, StringComparison.OrdinalIgnoreCase));
            if (additionalMilesPrice != null)
            {
                var returnObject = new MOBSection();
                returnObject.Text1 = !string.IsNullOrEmpty(_configuration.GetValue<string>("AdditionalMilesLabelText")) ? _configuration.GetValue<string>("AdditionalMilesLabelText") : "Additional Miles";
                returnObject.Text2 = additionalMilesPrice.PriceTypeDescription?.Replace("Additional", String.Empty).Trim();
                returnObject.Text3 = additionalMilesPrice.FormattedDisplayValue;

                return returnObject;
            }
            return null;
        }

        private List<MOBSection> GetFOPDetails(MOBSHOPReservation reservation)
        {
            var mobSection = default(MOBSection);
            if (reservation?.Prices?.Count > 0)
            {
                var travelCredit = reservation.Prices.FirstOrDefault(price => new[] { "TB", "CERTIFICATE", "FFC", "TRAVELCREDITS" }.Any(credit => string.Equals(price.PriceType, credit, StringComparison.OrdinalIgnoreCase)));
                if (travelCredit != null)
                {
                    if (string.Equals(travelCredit.PriceType, "TB", StringComparison.OrdinalIgnoreCase))
                    {
                        mobSection = new MOBSection();
                        mobSection.Text1 = !string.IsNullOrEmpty(_configuration.GetValue<string>("UnitedTravelBankCashLabelText")) ? _configuration.GetValue<string>("UnitedTravelBankCashLabelText") : "United TravelBank cash";
                        mobSection.Text2 = !string.IsNullOrEmpty(_configuration.GetValue<string>("TravelBankCashAppliedLabelText")) ? _configuration.GetValue<string>("TravelBankCashAppliedLabelText") : "TravelBank cash applied";
                        mobSection.Text3 = travelCredit.FormattedDisplayValue;
                    }
                    else
                    {
                        mobSection = new MOBSection();
                        mobSection.Text1 = !string.IsNullOrEmpty(_configuration.GetValue<string>("TravelCreditsLabelText")) ? _configuration.GetValue<string>("TravelCreditsLabelText") : "Travel credits";
                        mobSection.Text2 = !string.IsNullOrEmpty(_configuration.GetValue<string>("CreditKeyLabelText")) ? _configuration.GetValue<string>("CreditKeyLabelText") : "Credit";
                        mobSection.Text3 = travelCredit.FormattedDisplayValue;
                    }
                }
            }
            return mobSection != null ? new List<MOBSection> { mobSection } : null;
        }
        public MOBItem GetPayLaterAmount(List<ProdDetail> products, MOBSHOPReservation reservation)
        {
            if (products != null && reservation != null)
            {
                if (IsFarelock(products))
                {
                    return new MOBItem { Id = _configuration.GetValue<string>("PayDueLaterLabelText"), CurrentValue = GetGrandTotalPrice(reservation) };
                }
            }
            return null;
        }


        private void AssignUpliftText(MOBShoppingCart shoppingCart, MOBSHOPReservation reservation)
        {

            //if (_shoppingUtility.IsEligibileForUplift(reservation, shoppingCart) && Shoppingcart?.Form)                //Check if eligible for Uplift
            if (IsEligibileForUplift(reservation, shoppingCart) && shoppingCart?.FormofPaymentDetails?.MoneyPlusMilesCredit?.SelectedMoneyPlusMiles == null) //Check if eligible for Uplift
            {
                shoppingCart.OmniCart.IsUpliftEligible = true;      //Set property to true, if Uplift is eligible                
            }
            else //Set Uplift properties to false / empty as Uplift isn't eligible
            {
                shoppingCart.OmniCart.IsUpliftEligible = false;
            }
        }

        public int GetCartItemsCount(MOBShoppingCart shoppingcart)
        {
            int itemsCount = 0;
            if (shoppingcart?.Products != null && shoppingcart.Products.Count > 0)
            {
                shoppingcart.Products.ForEach(product =>
                {
                    if (!string.IsNullOrEmpty(product.ProdTotalPrice) && Decimal.TryParse(product.ProdTotalPrice, out decimal totalprice) && (totalprice > 0 || product.Code == "RES" && totalprice == 0))
                    {
                        if (product?.Segments != null && product.Segments.Count > 0)
                        {
                            product.Segments.ForEach(segment =>
                            {
                                segment.SubSegmentDetails.ForEach(subSegment =>
                                {
                                    if (subSegment != null)
                                    {
                                        if (product.Code == "SEATASSIGNMENTS")
                                        {
                                            itemsCount += subSegment.PaxDetails.Count();
                                        }
                                        else
                                        {
                                            itemsCount += 1;
                                        }
                                    }
                                });

                            });
                            return;
                        }
                        itemsCount += 1;
                    }
                });
            }
            return itemsCount;
        }

        public string GetGrandTotalPrice(MOBSHOPReservation reservation)
        {
            if (reservation?.Prices != null)
            {
                var grandTotalPrice = reservation.Prices.Exists(p => p.DisplayType.ToUpper().Equals("GRAND TOTAL"))
                                ? reservation.Prices.Where(p => p.DisplayType.ToUpper().Equals("GRAND TOTAL")).First()
                                : reservation.Prices.Where(p => p.DisplayType.ToUpper().Equals("TOTAL")).First();
                if (_configuration.GetValue<bool>("EnableLivecartForAwardTravel") && reservation.AwardTravel)
                {
                    var totalDue = string.Empty;
                    var awardPrice = reservation.Prices.FirstOrDefault(p => string.Equals("miles", p.DisplayType, StringComparison.OrdinalIgnoreCase));
                    if (awardPrice != null)
                    {
                        totalDue = FormatedMilesValueAndText(awardPrice.Value);
                    }
                    if (grandTotalPrice != null)
                    {
                        totalDue = string.IsNullOrWhiteSpace(totalDue)
                                    ? grandTotalPrice.FormattedDisplayValue
                                    : $"{totalDue} + {grandTotalPrice.FormattedDisplayValue}";
                    }
                    return totalDue;
                }
                else
                {
                    if (grandTotalPrice != null)
                    {
                        return grandTotalPrice.FormattedDisplayValue;
                    }
                }
            }
            return string.Empty;
        }
        private static string FormatedMilesValueAndText(double milesValue)
        {
            if (milesValue >= 1000)
                return (milesValue / 1000D).ToString("0.#" + "K miles");
            else if (milesValue > 0)
                return milesValue.ToString("0,# miles");
            else
                return string.Empty;
        }
        public bool IsFarelock(List<ProdDetail> products)
        {
            if (products != null)
            {
                if (products.Any(p => p.Code.ToUpper() == "FARELOCK" || p.Code.ToUpper() == "FLK"))
                {
                    return true;
                }
            }
            return false;
        }

        public MOBItem GetTotalPrice(List<ProdDetail> products, MOBSHOPReservation reservation)
        {
            if (products != null && reservation != null)
            {
                return new MOBItem
                {
                    Id = IsFarelock(products) ? _configuration.GetValue<string>("FarelockTotalPriceLabelText") : _configuration.GetValue<string>("TotalPriceLabelText")
                ,
                    CurrentValue = IsFarelock(products) ? GetFareLockPrice(products) : GetGrandTotalPrice(reservation)
                };
            }
            return null;
        }
        public string GetFareLockPrice(List<ProdDetail> products)
        {
            return products.Where(p => p.Code.ToUpper() == "FARELOCK" || p.Code.ToUpper() == "FLK").First().ProdDisplayTotalPrice;
        }

        public bool IsAllFareLockOptionEnabled(int id, string version, List<MOBItem> catalgItems = null)
        {
            if (!_configuration.GetValue<bool>("EnableContinueFareLockDynamicOption")) return false;
            if ((catalgItems != null && catalgItems.Count > 0 &&
                  catalgItems.FirstOrDefault(a => a.Id == _configuration.GetValue<string>("Android_EnableAllFareLockOptionFeatureCatalogID") || a.Id == _configuration.GetValue<string>("iOS_EnableAllFareLockOptionFeatureCatalogID"))?.CurrentValue == "1"))
                return GeneralHelper.IsApplicationVersionGreaterorEqual(id, version, _configuration.GetValue<string>("Android_EnableContinueFareLockDynamicOption_AppVersion"), _configuration.GetValue<string>("IPhone_EnableContinueFareLockDynamicOption_AppVersion"));
            else
                return false;
        }

        public bool? GetBooleanFromCharacteristics(Collection<Characteristic> characteristic, string key)
        {
            if (characteristic != null && characteristic.Any())
            {
                string stringvalue = GetCharactersticValue(characteristic, key);
                if (!string.IsNullOrEmpty(stringvalue))
                {
                    Boolean.TryParse(stringvalue, out bool boolvalue);
                    return boolvalue;
                }
            }
            return null;
        }
        public string GetCharactersticValue(Collection<Characteristic> characteristics, string code)
        {
            if (characteristics == null || characteristics.Count <= 0) return string.Empty;
            var characteristic = characteristics.FirstOrDefault(c => c != null && c.Code != null
            && !string.IsNullOrEmpty(c.Code) && c.Code.Trim().Equals(code, StringComparison.InvariantCultureIgnoreCase));
            return characteristic == null ? string.Empty : characteristic.Value;
        }
        public string GetSDLStringMessageFromList(List<CMSContentMessage> list, string title)
        {
            return list?.Where(x => x.Title.Equals(title))?.FirstOrDefault()?.ContentFull?.Trim();
        }
        public string GetSDLContentShortMessageFromList(List<CMSContentMessage> list, string title)
        {
            return list?.Where(x => x.Title.Equals(title))?.FirstOrDefault()?.ContentShort?.Trim();
        }
        public bool IsEnableMostPopularBundle(int appId, string version)
        {
            if (!_configuration.GetValue<bool>("EnableMostPopularBundleFeature")) return false;
            return GeneralHelper.IsApplicationVersionGreaterorEqual(appId, version, _configuration.GetValue<string>("Android_EnableMostPopularBundleFeature_AppVersion"), _configuration.GetValue<string>("IPhone_EnableMostPopularBundleFeature_AppVersion"));
        }
        public bool IsCheckedIn(ReservationFlightSegment cslSegment)
        {
            var characteristic = cslSegment?.Characteristic;
            try
            {
                if (characteristic != null && characteristic.Any())
                {
                    var selectDesc = characteristic.FirstOrDefault
                        (x => (string.Equals(x.Code, "CheckedIn", StringComparison.OrdinalIgnoreCase)));

                    bool.TryParse(selectDesc?.Value, out bool retValue);

                    return retValue;
                }
            }
            catch { }
            return false;
        }

        public bool IsCheckInEligible(ReservationFlightSegment cslSegment)
        {
            var characteristic = cslSegment?.Characteristic;
            try
            {
                if (characteristic != null && characteristic.Any())
                {
                    var selectDesc = characteristic.FirstOrDefault
                        (x => (string.Equals(x.Code, "CheckinTriggered", StringComparison.OrdinalIgnoreCase)));

                    bool.TryParse(selectDesc?.Value, out bool retValue);

                    return retValue;
                }
            }
            catch { }
            return false;
        }

        public bool IsAllPaxCheckedIn(ReservationFlightSegment cslSegment)
        {
            var characteristic = cslSegment?.Characteristic;
            try
            {
                if (characteristic != null && characteristic.Any())
                {
                    var selectDesc = characteristic.FirstOrDefault
                        (x => (string.Equals(x.Code, "CheckinTriggered", StringComparison.OrdinalIgnoreCase)));

                    bool.TryParse(selectDesc?.Value, out bool retValue);

                    return retValue;
                }
            }
            catch { }
            return false;
        }

        public bool IsAllPaxCheckedIn(ReservationDetail reservation, ReservationFlightSegment cslSegment, bool isCheckedIn)
        {
            //NO-CHECK-CONDITIONS 
            var reservationDetails = reservation?.Detail;

            if (!isCheckedIn && reservationDetails == null && cslSegment == null) return false;

            var travelers = reservationDetails?.Travelers;

            if (reservationDetails.Travelers == null || !reservationDetails.Travelers.Any()) return false;

            var paxCount = reservationDetails.Travelers?.Count();
            var segmentNumber = cslSegment.SegmentNumber;

            bool isAllPaxCheckedIn = true;
            try
            {
                reservation.Detail.Travelers.ForEach(pax =>
                {

                    var firstTicketCoupons = pax.Tickets?.FirstOrDefault()?.FlightCoupons;

                    if (firstTicketCoupons != null && firstTicketCoupons.Any())
                    {
                        firstTicketCoupons.ForEach(coupons =>
                        {
                            if (coupons?.FlightSegment?.SegmentNumber == segmentNumber)
                            {
                                if (!string.Equals
                                (coupons?.Status?.Code, "CHECKED-IN", StringComparison.OrdinalIgnoreCase))
                                {
                                    isAllPaxCheckedIn = isAllPaxCheckedIn && false;
                                }
                            }
                        });
                    }
                });
            }
            catch { }
            return isAllPaxCheckedIn;
        }
        public bool EnableAdvanceSearchCouponBooking(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableAdvanceSearchCouponBooking") && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("AndroidAdvanceSearchCouponBookingVersion"), _configuration.GetValue<string>("iPhoneAdvanceSearchCouponBookingVersion"));
        }

        public bool IsNonRefundableNonChangable(string productCode)
        {
            var supportedProductCodes = _configuration.GetValue<string>("NonRefundableNonChangableProductCodes");
            return EnableNonRefundableNonChangable() && !string.IsNullOrWhiteSpace(productCode) &&
                   !string.IsNullOrWhiteSpace(supportedProductCodes) &&
                   supportedProductCodes.IndexOf(productCode.Trim().ToUpper()) > -1;
        }

        private bool EnableNonRefundableNonChangable()
        {
            return _configuration.GetValue<bool>("EnableNonRefundableNonChangable");
        }

        public async Task<United.Mobile.Model.Shopping.InfoWarningMessages> GetNonRefundableNonChangableInversionMessage(MOBRequest request, Session session)
        {
            List<CMSContentMessage> lstMessages = await _ffcShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
            var message = GetSDLStringMessageFromList(lstMessages, _configuration.GetValue<string>("NonRefundableNonChangableFareInversionMessage"));
            return BuildInfoWarningMessages(message);
        }
        private static United.Mobile.Model.Shopping.InfoWarningMessages BuildInfoWarningMessages(string message)
        {
            var infoWarningMessages = new United.Mobile.Model.Shopping.InfoWarningMessages
            {
                Order = MOBINFOWARNINGMESSAGEORDER.BEFAREINVERSION.ToString(),
                IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString(),
                Messages = new List<string>
                {
                    message
                }
            };
            return infoWarningMessages;
        }

        private async Task<United.Mobile.Model.Shopping.InfoWarningMessages> BuildUpgradeFromNonRefuNonChanInfoMessage(MOBRequest request, Session session)
        {
            List<CMSContentMessage> lstMessages = await _ffcShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
            var message = GetSDLMessageFromList(lstMessages, _configuration.GetValue<string>("UpgradedFromNonRefuNonChanTextWithHtml")).FirstOrDefault();
            var infoWarningMessages = new United.Mobile.Model.Shopping.InfoWarningMessages
            {
                Order = MOBINFOWARNINGMESSAGEORDER.INHIBITBOOKING.ToString(), // Using existing order for sorting. 
                IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString(),
                HeaderMessage = (request.Application.Id == 1) ? message.HeadLine : string.Empty,
                Messages = new List<string>
                {
                   (request.Application.Id==1)? message.ContentShort : message.ContentFull
                }
            };
            return infoWarningMessages;
        }

        public bool IsNonRefundableNonChangable(DisplayCart displayCart)
        {
            return EnableNonRefundableNonChangable() &&
                    displayCart != null &&
                    IsNonRefundableNonChangable(displayCart.ProductCode);
        }

        public bool IsFSRNearByAirportAlertEnabled(int id, string version)
        {
            if (!_configuration.GetValue<bool>("EnableFSRNearByAirportAlertFeature")) return false;
            return GeneralHelper.IsApplicationVersionGreaterorEqual(id, version, _configuration.GetValue<string>("Android_EnableFSRNearByAirportAlertFeature_AppVersion"), _configuration.GetValue<string>("IPhone_EnableFSRNearByAirportAlertFeature_AppVersion"));
        }

        public bool IsServiceAnimalEnhancementEnabled(int id, string version, List<MOBItem> catalogItems)
        {
            if (!_configuration.GetValue<bool>("EnableServiceAnimalEnhancements")) return false;
            if (catalogItems != null && catalogItems.Count > 0 &&
                              catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableTaskTrainedServiceAnimalFeature).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableTaskTrainedServiceAnimalFeature).ToString())?.CurrentValue == "1")
                return GeneralHelper.IsApplicationVersionGreaterorEqual(id, version, _configuration.GetValue<string>("Android_EnableServiceAnimalEnhancements_AppVersion"), _configuration.GetValue<string>("IPhone_EnableServiceAnimalEnhancements_AppVersion"));
            else return false;
        }

        public bool EnableBagCalcSelfRedirect(int id, string version)
        {
            return _configuration.GetValue<bool>("EnableBagCalcSelfRedirect") && GeneralHelper.IsApplicationVersionGreaterorEqual(id, version, _configuration.GetValue<string>("AndroidBagCalcSelfRedirectVersion"), _configuration.GetValue<string>("iOSBagCalcSelfRedirectVersion"));
        }
        public List<MOBMobileCMSContentMessages> GetSDLMessageFromList(List<CMSContentMessage> list, string title)
        {
            List<MOBMobileCMSContentMessages> listOfMessages = new List<MOBMobileCMSContentMessages>();
            list?.Where(l => l.Title.ToUpper().Equals(title.ToUpper()))?.ForEach(i => listOfMessages.Add(new MOBMobileCMSContentMessages()
            {
                Title = i.Title,
                ContentFull = i.ContentFull,
                HeadLine = i.Headline,
                ContentShort = i.ContentShort,
                LocationCode = i.LocationCode
            }));

            return listOfMessages;
        }

        public string BuildStrikeThroughDescription()
        {
            return _configuration.GetValue<string>("StrikeThroughPriceTypeDescription");
        }

        public bool IsEnableBulkheadNoUnderSeatStorage(int appId, string version)
        {
            if (!_configuration.GetValue<bool>("EnableBulkSeatNoUnderSeatCoverageFeature")) return false;
            return GeneralHelper.IsApplicationVersionGreaterorEqual(appId, version, _configuration.GetValue<string>("Android_EnableBulkSeatNoUnderSeatCoverageFeature_AppVersion"), _configuration.GetValue<string>("IPhone_EnableBulkSeatNoUnderSeatCoverageFeature_AppVersion"));
        }

        public bool EnableEditForAllCabinPOM(int appId, string appVersion, List<MOBItem> catalog)
        {
            return _configuration.GetValue<bool>("EnableisEditablePOMFeature") && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("Android_isEditablePOMFeatureSupported_AppVersion"), _configuration.GetValue<string>("IPhone_isEditablePOMFeatureSupported_AppVersion")) && CheckClientCatalogForEnablingFeature("POMClientCatalogValues", catalog);
        }

        public bool EnableEditForAllCabinPOM(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableisEditablePOMFeature") && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("Android_isEditablePOMFeatureSupported_AppVersion"), _configuration.GetValue<string>("IPhone_isEditablePOMFeatureSupported_AppVersion"));
        }

        public bool HasNearByAirport(ShopResponse _cslShopResponse)
        {
            var flights = _cslShopResponse.Trips.FirstOrDefault()?.Flights;
            return (flights?.SelectMany(a => a.Warnings).Where(b => b.Key == _configuration.GetValue<string>("WARNING_NEARBYAIRPORT"))?.Any() == true ? true : false);
        }

        public bool CheckClientCatalogForEnablingFeature(string catalogFeature, List<MOBItem> clientCatalog)
        {
            if (!string.IsNullOrEmpty(catalogFeature) && clientCatalog != null && clientCatalog.Count > 0)
            {
                string catalogId = _configuration.GetValue<string>(catalogFeature);
                var Id = catalogId.Split('|');
                foreach (var item in clientCatalog)
                {
                    if (Id.Contains(item?.Id))
                        return !string.IsNullOrEmpty(item?.CurrentValue) && item?.CurrentValue == "1";
                }
            }
            return false;
        }

        public async Task<(bool returnValue, string validAuthToken)> ValidateHashPinAndGetAuthToken(string accountNumber, string hashPinCode, int applicationId, string deviceId, string appVersion, string validAuthToken)
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

            if (ok == false && _configuration.GetValue<String>("ByPassMPByPassCheckForDpMPSignCall2_1_41") != null &&
                _configuration.GetValue<String>("ByPassMPByPassCheckForDpMPSignCall2_1_41").ToString().ToUpper().Trim() == appVersion.ToUpper().Trim())
            {
                var deviceDynamodb = new DeviceDynamDB(_configuration, _dynamoDBService);
                ok = await deviceDynamodb.ValidateDeviceIDAPPID(deviceId, applicationId, accountNumber, appVersion).ConfigureAwait(false);

            }

            return (ok, validAuthToken);
        }

        public async Task<string> ValidateAndGetSingleSignOnWebShareToken(MOBRequest request, string mileagePlusAccountNumber, string passwordHash, string sessionId)
        {
            bool validSSORequest = false;
            string webShareToken = string.Empty;
            try
            {
                string authToken = string.Empty;

                if (!string.IsNullOrEmpty(mileagePlusAccountNumber) && !string.IsNullOrEmpty(mileagePlusAccountNumber.Trim()) && !string.IsNullOrEmpty(passwordHash))
                {
                    var tupleRes = await ValidateHashPinAndGetAuthToken
                        (mileagePlusAccountNumber, passwordHash, request.Application.Id, request.DeviceId, request.Application.Version.Major, authToken);
                    validSSORequest = tupleRes.returnValue;
                    authToken = tupleRes.validAuthToken;

                }
                if (validSSORequest)
                {
                    webShareToken = _dPService.GetSSOTokenString(request.Application.Id, mileagePlusAccountNumber, _configuration);
                }
            }
            catch (Exception ex)
            {
                string[] messages = ex.Message.Split('#');

                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                exceptionWrapper.Message = messages[0];
                //logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(sessionId, "SingleSignOn", "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, exceptionWrapper, true, false));
                //logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(sessionId, "SingleSignOnIPOS", "DotComSSOBuildError", request.Application.Id, request.Application.Version.Major, request.DeviceId, exceptionWrapper, true, false));

            }
            return webShareToken;
        }

        public bool IsInterLine(string operatingCarrier, string MarketingCarrier)
        {
            if (string.IsNullOrEmpty(operatingCarrier) && string.IsNullOrEmpty(MarketingCarrier)) return false;
            var interLineAirlines = _configuration.GetValue<string>("InterLineAirlines");
            if (string.IsNullOrEmpty(interLineAirlines)) return false;

            if (interLineAirlines.Contains(operatingCarrier.Trim().ToUpper()) || interLineAirlines.Contains(MarketingCarrier.Trim().ToUpper()))
                return true;

            return false;
        }

        public bool IsOperatedBySupportedAirlines(string operatingCarrier, string MarketingCarrier)
        {
            if (string.IsNullOrEmpty(operatingCarrier) && string.IsNullOrEmpty(MarketingCarrier)) return false;
            var operatedBySupportedAirlines = _configuration.GetValue<string>("SupportedCarriers");
            if (string.IsNullOrEmpty(operatedBySupportedAirlines)) return false;

            if (!string.IsNullOrEmpty(MarketingCarrier) && MarketingCarrier.Trim().ToUpper() == "UA"
                && operatedBySupportedAirlines.Contains(operatingCarrier.Trim().ToUpper()))
                return true;

            return false;
        }

        public bool IsOperatedByUA(string operatingCarrier, string MarketingCarrier)
        {
            if (string.IsNullOrEmpty(operatingCarrier) && string.IsNullOrEmpty(MarketingCarrier)) return false;
            var operatedByUA = _configuration.GetValue<string>("UnitedCarriers");
            if (string.IsNullOrEmpty(operatedByUA)) return false;

            if (!string.IsNullOrEmpty(MarketingCarrier) && MarketingCarrier.Trim().ToUpper() == "UA"
                && operatedByUA.Contains(operatingCarrier.Trim().ToUpper()))
                return true;

            return false;
        }

        public bool IsLandTransport(string equipmentType)
        {
            if (!string.IsNullOrEmpty(equipmentType) && (equipmentType.Contains("BUS") || equipmentType.Contains("TRN")))
            {
                return true;
            }
            return false;
        }

        public bool EnableOAMessageUpdate(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableOAMsgUpdate") &&
            GeneralHelper.IsApplicationVersionGreater(applicationId, appVersion, "Android_EnableOAMessageUpdate_AppVersion", "IPhone_EnableOAMessageUpdate_AppVersion", "", "", true, _configuration);
        }

        public bool EnableOAMsgUpdateFixViewRes(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableOAMsgUpdateFix") &&
            GeneralHelper.IsApplicationVersionGreater(applicationId, appVersion, "Android_EnableOAMsgUpdateFixViewRes_AppVersion", "IPhone_EnableOAMsgUpdateFixViewRes_AppVersion", "", "", true, _configuration);
        }

        public bool IsOAReadOnlySeatMap(string operatingCarrier)
        {
            if (string.IsNullOrEmpty(operatingCarrier)) return false;
            var interLineAirlines = _configuration.GetValue<string>("ReadOnlySeatMapCarriers");
            if (string.IsNullOrEmpty(interLineAirlines)) return false;

            if (interLineAirlines.Contains(operatingCarrier.Trim().ToUpper()))
                return true;

            return false;
        }

        public bool IsOperatedByOtherAirlines(string operatingCarrier, string MarketingCarrier, string equipmentType)
        {
            if (!IsInterLine(operatingCarrier, MarketingCarrier) && !IsOperatedByUA(operatingCarrier, MarketingCarrier)
                && !IsOperatedBySupportedAirlines(operatingCarrier, MarketingCarrier) && !IsLandTransport(equipmentType)
                && !_configuration.GetValue<string>("SupportedAirlinesFareComparison").Contains(operatingCarrier))
            {
                return true;
            }
            return false;
        }

        public bool EnableShoppingcartPhase2ChangesWithVersionCheck(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableShoppingCartPhase2Changes")
                 && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "Android_EnableShoppingCartPhase2Changes_AppVersion", "iPhone_EnableShoppingCartPhase2Changes_AppVersion", "", "", true, _configuration);
        }

        public string BuildPaxTypeDescription(string paxTypeCode, string paxDescription, int paxCount)
        {
            string description = paxDescription;
            if (!string.IsNullOrEmpty(paxTypeCode))
            {
                switch (paxTypeCode.ToUpper())
                {
                    case "ADT":
                        description = $"{((paxCount == 1) ? "adult (18-64)" : "adults (18-64)")} ";
                        break;
                    case "SNR":
                        description = $"{((paxCount == 1) ? "senior (65+)" : "seniors (65+)")} ";
                        break;
                    case "C17":
                        description = $"{((paxCount == 1) ? "child (15-17)" : "children (15-17)")} ";
                        break;
                    case "C14":
                        description = $"{((paxCount == 1) ? "child (12-14)" : "children (12-14)")} ";
                        break;
                    case "C11":
                        description = $"{((paxCount == 1) ? "child (5-11)" : "children (5-11)")} ";
                        break;
                    case "C04":
                        description = $"{((paxCount == 1) ? "child (2-4)" : "children (2-4)")} ";
                        break;
                    case "INS":
                        description = $"{((paxCount == 1) ? "infant(under 2) - seat" : "infants(under 2) - seat")} ";
                        break;
                    case "INF":
                        description = $"{((paxCount == 1) ? "infant (under 2) - lap" : "infants (under 2) - lap")} ";
                        break;
                    default:
                        description = paxDescription;
                        break;
                }
            }
            return description;
        }
        #region ReservationToShoppingCart_DataMigration
        public async Task<MOBShoppingCart> ReservationToShoppingCart_DataMigration(MOBSHOPReservation reservation, MOBShoppingCart shoppingCart, MOBRequest request, Session session = null)
        {
            try
            {
                bool isETCCertificatesExistInShoppingCartPersist = (_configuration.GetValue<bool>("MTETCToggle") &&
                                                                    shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates != null &&
                                                                    shoppingCart?.FormofPaymentDetails?.TravelCertificate?.Certificates.Count > 0);
                if (shoppingCart == null)
                    shoppingCart = new MOBShoppingCart();
                var formOfPaymentDetails = new MOBFormofPaymentDetails();
                shoppingCart.CartId = reservation.CartId;
                shoppingCart.PointofSale = reservation.PointOfSale;
                if (_configuration.GetValue<bool>("MTETCToggle"))
                    shoppingCart.IsMultipleTravelerEtcFeatureClientToggleEnabled = reservation.ShopReservationInfo2 != null ? reservation.ShopReservationInfo2.IsMultipleTravelerEtcFeatureClientToggleEnabled : false;
                formOfPaymentDetails.FormOfPaymentType = reservation.FormOfPaymentType.ToString();
                formOfPaymentDetails.PayPal = reservation.PayPal;
                formOfPaymentDetails.PayPalPayor = reservation.PayPalPayor;
                formOfPaymentDetails.MasterPassSessionDetails = reservation.MasterpassSessionDetails;
                formOfPaymentDetails.masterPass = reservation.Masterpass;
                formOfPaymentDetails.Uplift = reservation.ShopReservationInfo2?.Uplift;
                shoppingCart.SCTravelers = (reservation.TravelersCSL != null && reservation.TravelersCSL.Count() > 0) ? reservation.TravelersCSL : null;
                if (shoppingCart.SCTravelers != null && shoppingCart.SCTravelers.Any())
                {
                    shoppingCart.SCTravelers[0].SelectedSpecialNeeds = (reservation.TravelersCSL != null && reservation.TravelersCSL.Count() > 0) ? reservation.TravelersCSL[0].SelectedSpecialNeeds : null;
                    shoppingCart.SCTravelers[0].SelectedSpecialNeedMessages = (reservation.TravelersCSL != null && reservation.TravelersCSL.Count() > 0) ? reservation.TravelersCSL[0].SelectedSpecialNeedMessages : null;
                }
                if (shoppingCart.FormofPaymentDetails != null && shoppingCart.FormofPaymentDetails.SecondaryCreditCard != null)
                {
                    formOfPaymentDetails.CreditCard = shoppingCart.FormofPaymentDetails.CreditCard;
                    formOfPaymentDetails.SecondaryCreditCard = shoppingCart.FormofPaymentDetails.SecondaryCreditCard;
                }
                else
                {
                    formOfPaymentDetails.CreditCard = reservation.CreditCards?.Count() > 0 ? reservation.CreditCards[0] : null;
                }
                if (IncludeFFCResidual(request.Application.Id, request.Application.Version.Major) && shoppingCart.FormofPaymentDetails != null)
                {
                    formOfPaymentDetails.TravelFutureFlightCredit = shoppingCart.FormofPaymentDetails?.TravelFutureFlightCredit;
                    formOfPaymentDetails.FormOfPaymentType = shoppingCart.FormofPaymentDetails.FormOfPaymentType;
                }
                if (IncludeMoneyPlusMiles(request.Application.Id, request.Application.Version.Major) && shoppingCart.FormofPaymentDetails?.MoneyPlusMilesCredit != null)
                {
                    formOfPaymentDetails.MoneyPlusMilesCredit = shoppingCart.FormofPaymentDetails.MoneyPlusMilesCredit;
                }
                bool isTravelCredit = ConfigUtility.IncludeTravelCredit(request.Application.Id, request.Application.Version.Major);
                if (isTravelCredit)
                {
                    formOfPaymentDetails.TravelCreditDetails = shoppingCart.FormofPaymentDetails?.TravelCreditDetails;
                }

                if (ConfigUtility.IncludeTravelBankFOP(request.Application.Id, request.Application.Version.Major))
                {
                    formOfPaymentDetails.TravelBankDetails = shoppingCart.FormofPaymentDetails?.TravelBankDetails;
                    if (formOfPaymentDetails.TravelBankDetails?.TBApplied > 0)
                    {
                        _ffcShoppingcs.AssignIsOtherFOPRequired(formOfPaymentDetails, reservation.Prices);
                        shoppingCart.FormofPaymentDetails.IsOtherFOPRequired = formOfPaymentDetails.IsOtherFOPRequired;
                        shoppingCart.FormofPaymentDetails.FormOfPaymentType = formOfPaymentDetails.FormOfPaymentType;
                    }
                }

                if (ConfigUtility.EnableMilesFOP(request.Application.Id, request.Application.Version.Major))
                {
                    formOfPaymentDetails.MilesFOP = shoppingCart?.FormofPaymentDetails?.MilesFOP;
                }

                await _ffcShoppingcs.AssignFFCValues(reservation.SessionId, shoppingCart, request, formOfPaymentDetails, reservation);
                shoppingCart.FormofPaymentDetails = formOfPaymentDetails;
                shoppingCart.FormofPaymentDetails.Phone = reservation.ReservationPhone;
                shoppingCart.FormofPaymentDetails.Email = reservation.ReservationEmail;
                shoppingCart.FormofPaymentDetails.EmailAddress = reservation.ReservationEmail != null ? reservation.ReservationEmail.EmailAddress : null;
                shoppingCart.FormofPaymentDetails.BillingAddress = reservation.CreditCardsAddress?.Count() > 0 ? reservation.CreditCardsAddress[0] : null;
                if (reservation.IsReshopChange)
                {

                    double changeFee = 0.0;
                    double grandTotal = 0.0;
                    if (reservation.Prices != null && reservation.Prices.Exists(price => price.DisplayType.ToUpper().Trim() == "CHANGEFEE"))
                        changeFee = reservation.Prices.First(price => price.DisplayType.ToUpper().Trim() == "CHANGEFEE").Value;

                    if (reservation.Prices != null && reservation.Prices.Exists(price => price.DisplayType.ToUpper().Trim() == "GRAND TOTAL"))
                        grandTotal = reservation.Prices.First(price => price.DisplayType.ToUpper().Trim() == "GRAND TOTAL").Value;

                    if (!reservation.AwardTravel)
                    {
                        if (grandTotal == 0.0 && (reservation.Prices != null && reservation.Prices.Any()))
                        {
                            grandTotal = (reservation.Prices != null && reservation.Prices.Count > 0) ? reservation.Prices.First(price => price.DisplayType.ToUpper().Trim() == "TOTAL").Value : grandTotal;
                        }
                    }
                    string totalDue = (grandTotal > changeFee ? (grandTotal - changeFee) : 0).ToString();
                    shoppingCart.TotalPrice = String.Format("{0:0.00}", totalDue);
                    shoppingCart.DisplayTotalPrice = totalDue;  //string.Format("${0:c}", totalDue); 
                }
                //MFOP
                if (ConfigUtility.EnableMFOP(request.Application.Id, request.Application.Version.Major) && shoppingCart.Flow == FlowType.BOOKING.ToString())
                {
                    await AssignETCFFCvalues(reservation.SessionId, shoppingCart, request, reservation).ConfigureAwait(false);
                }
                if (IsETCCombinabilityEnabled(request.Application.Id, request.Application.Version.Major) && shoppingCart.Flow == FlowType.BOOKING.ToString())
                {
                    await LoadandAddTravelCertificate(shoppingCart, reservation, isETCCertificatesExistInShoppingCartPersist, request.Application);
                }
                else if (_configuration.GetValue<bool>("ETCToggle") && shoppingCart.Flow == FlowType.BOOKING.ToString())
                {
                    await LoadandAddTravelCertificate(shoppingCart, reservation.SessionId, reservation.Prices, isETCCertificatesExistInShoppingCartPersist, request.Application);
                }
                if (_configuration.GetValue<bool>("EnableETCBalanceAttentionMessageOnRTI") && !IsETCCombinabilityEnabled(request.Application.Id, request.Application.Version.Major))
                {
                    await AssignBalanceAttentionInfoWarningMessage(reservation.ShopReservationInfo2, shoppingCart.FormofPaymentDetails?.TravelCertificate);
                }
                if (isTravelCredit && !ConfigUtility.EnableMFOP(request.Application.Id, request.Application.Version.Major))
                {
                    await UpdateTCPriceAndFOPType(reservation.Prices, shoppingCart.FormofPaymentDetails, request.Application, shoppingCart.Products, shoppingCart.SCTravelers);
                }
                if (_configuration.GetValue<bool>("EnableCouponsforBooking") && shoppingCart.Flow == FlowType.BOOKING.ToString())
                {
                    await LoadandAddPromoCode(shoppingCart, reservation.SessionId, request.Application);
                }
                reservation.CartId = null;
                reservation.PointOfSale = null;
                reservation.PayPal = null;
                reservation.PayPalPayor = null;
                reservation.MasterpassSessionDetails = null;
                reservation.Masterpass = null;
                reservation.TravelersCSL = null;
                reservation.CreditCards2 = null;
                reservation.ReservationPhone2 = null;
                reservation.ReservationEmail2 = null;
                reservation.CreditCardsAddress = null;
                reservation.FOPOptions = null;

                if (_configuration.GetValue<bool>("EnableSelectDifferentFOPAtRTI"))
                {
                    if (!reservation.IsReshopChange)
                    {
                        //If ETC, ghost card, no saved cc presents and no due in reshop disable this button.
                        if (reservation.ShopReservationInfo2 != null && shoppingCart.FormofPaymentDetails != null)
                        {
                            if (((shoppingCart.FormofPaymentDetails.CreditCard != null && (reservation.ShopReservationInfo == null || !reservation.ShopReservationInfo.CanHideSelectFOPOptionsAndAddCreditCard)) ||
                                                        shoppingCart.FormofPaymentDetails.masterPass != null || shoppingCart.FormofPaymentDetails.PayPal != null || shoppingCart.FormofPaymentDetails.Uplift != null ||
                                                      (!string.IsNullOrEmpty(shoppingCart.FormofPaymentDetails.FormOfPaymentType) && shoppingCart.FormofPaymentDetails.FormOfPaymentType.ToUpper().Equals("APPLEPAY"))) && (shoppingCart.FormofPaymentDetails.TravelCertificate == null
                                                      || (shoppingCart.FormofPaymentDetails?.TravelCertificate?.Certificates == null || shoppingCart.FormofPaymentDetails?.TravelCertificate?.Certificates?.Count == 0)
                                                      ))
                            {
                                reservation.ShopReservationInfo2.ShowSelectDifferentFOPAtRTI = true;
                            }
                            else
                            {
                                reservation.ShopReservationInfo2.ShowSelectDifferentFOPAtRTI = false;
                            }
                        }
                    }
                }
                _ffcShoppingcs.AssignNullToETCAndFFCCertificates(shoppingCart.FormofPaymentDetails, request);
                if (IsEnableOmniCartMVP2Changes(request.Application.Id, request.Application.Version.Major, true) && !reservation.IsReshopChange)
                {
                    BuildOmniCart(shoppingCart, reservation);
                }

                if (_shoppingBuyMiles.IsBuyMilesFeatureEnabled(request.Application.Id, request.Application.Version.Major, null, isNotSelectTripCall: true))
                    _shoppingBuyMiles.UpdateGrandTotal(reservation, true);

                if (IsExtraSeatFeatureEnabled(request.Application.Id, request.Application.Version.Major, session?.CatalogItems))
                {
                    reservation.ShopReservationInfo2.AllowExtraSeatSelection = IsExtraSeatExcluded(session, reservation?.Trips, reservation?.ShopReservationInfo2?.displayTravelTypes, shoppingCart, reservation?.ShopReservationInfo2?.IsUnfinihedBookingPath);
                }
                else
                {
                    if (reservation?.ShopReservationInfo2 != null)
                        reservation.ShopReservationInfo2.AllowExtraSeatSelection = false;
                }
                shoppingCart.FormofPaymentDetails.IsFOPRequired = IsFOPRequired(shoppingCart, reservation);
                shoppingCart.FormofPaymentDetails.IsEnableAgreeandPurchaseButton = IsEnableAgreeandPurchaseButton(shoppingCart, reservation);
                shoppingCart.FormofPaymentDetails.MaskedPaymentMethod = await AssignMaskedPaymentMethod(shoppingCart, request.Application).ConfigureAwait(false);
                shoppingCart.IsCorporateBusinessNamePersonalized = reservation.ShopReservationInfo2.IsCorporateBusinessNamePersonalized;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message.ToString());
            }

            return shoppingCart;
        }
        public bool IsExtraSeatExcluded(Session session, List<MOBSHOPTrip> trips, List<DisplayTravelType> displayTravelTypes, MOBShoppingCart response = null, bool? isUnfinishedBooking = false)
        {
            return (displayTravelTypes?.Count() > 1) && !session.IsCorporateBooking
                && (isUnfinishedBooking == true || session.TravelType == TravelType.RA.ToString() || session.TravelType == TravelType.TPBooking.ToString())
                && IsUAFlight(trips)
                && IsExcludedOperatingCarrier(trips);

        }

        public bool IsUAFlight(List<MOBSHOPTrip> trips)
        {
            var unitedCarriers = _configuration.GetValue<string>("UnitedCarriers");
            return (trips?.SelectMany(a => a?.FlattenedFlights)?.SelectMany(b => b?.Flights)?
             .Any(c => !unitedCarriers.Contains(c?.OperatingCarrier)) == true) ? false : true;
        }

        public bool IsExcludedOperatingCarrier(List<MOBSHOPTrip> trips)
        {
            var execludedUnitedCarriers = _configuration.GetValue<string>("ExcludedOperatingCarriersForExtraSeat");
            return (trips?.SelectMany(a => a?.FlattenedFlights)?.SelectMany(b => b?.Flights)?
             .Any(c => execludedUnitedCarriers.Contains(c?.OperatingCarrier)) == true) ? false : true;
        }

        private async Task LoadandAddPromoCode(MOBShoppingCart shoppingCart, string sessionId, MOBApplication application)
        {
            var persistedApplyPromoCodeResponse = new ApplyPromoCodeResponse();
            persistedApplyPromoCodeResponse = await _sessionHelperService.GetSession<ApplyPromoCodeResponse>(sessionId, persistedApplyPromoCodeResponse.ObjectName, new List<string> { sessionId, persistedApplyPromoCodeResponse.ObjectName }).ConfigureAwait(false);
            if (shoppingCart.PromoCodeDetails == null)
            {
                shoppingCart.PromoCodeDetails = new MOBPromoCodeDetails();
            }
            if (persistedApplyPromoCodeResponse != null)
            {
                UpdateShoppinCartWithCouponDetails(shoppingCart);
                persistedApplyPromoCodeResponse.ShoppingCart.PromoCodeDetails = shoppingCart.PromoCodeDetails;
                await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, sessionId, new List<string> { sessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName).ConfigureAwait(false);
                await _sessionHelperService.SaveSession<ApplyPromoCodeResponse>(persistedApplyPromoCodeResponse, sessionId, new List<string> { sessionId, persistedApplyPromoCodeResponse.ObjectName }, persistedApplyPromoCodeResponse.ObjectName).ConfigureAwait(false);
            }
            // DisablePromoOption(shoppingCart);
            IsHidePromoOption(shoppingCart);
        }
        private async Task UpdateTCPriceAndFOPType(List<MOBSHOPPrice> prices, MOBFormofPaymentDetails formofPaymentDetails, MOBApplication application, List<ProdDetail> products, List<MOBCPTraveler> travelers)
        {
            if (ConfigUtility.IncludeTravelCredit(application.Id, application.Version.Major))
            {
                _ffcShoppingcs.ApplyFFCToAncillary(products, application, formofPaymentDetails, prices);
                var price = prices?.FirstOrDefault(p => p.DisplayType.ToUpper() == "CERTIFICATE" || p.DisplayType.ToUpper() == "FFC");
                if (price != null)
                {
                    formofPaymentDetails.TravelCreditDetails.AlertMessages = (formofPaymentDetails.TravelFutureFlightCredit?.FutureFlightCredits?.Count > 0 ?
                                                                              formofPaymentDetails.TravelFutureFlightCredit.ReviewFFCMessages :
                                                                              formofPaymentDetails.TravelCertificate.ReviewETCMessages.Where(m => m.HeadLine != "TravelCertificate_Combinability_ReviewETCAlertMsgs_OtherFopRequiredMessage").ToList());
                }
                else if (formofPaymentDetails.TravelCreditDetails != null)
                {
                    formofPaymentDetails.TravelCreditDetails.AlertMessages = null;
                }

                _ffcShoppingcs.UpdateTravelCreditAmountWithSelectedETCOrFFC(formofPaymentDetails, prices, travelers);
                try
                {
                    CSLContentMessagesResponse lstMessages = null;
                    string s = await _cachingService.GetCache<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID") + "MOBCSLContentMessagesResponse", _headers.ContextValues.TransactionId).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(s) && formofPaymentDetails.TravelCreditDetails != null)
                    {
                        lstMessages = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(s);
                        formofPaymentDetails.TravelCreditDetails.AlertMessages = _ffcShoppingcs.BuildReviewFFCHeaderMessage(formofPaymentDetails?.TravelFutureFlightCredit, travelers, lstMessages.Messages);
                    }
                }
                catch { }

                if (formofPaymentDetails?.FormOfPaymentType == "ETC" ||
                   formofPaymentDetails?.FormOfPaymentType == "FFC")
                    formofPaymentDetails.FormOfPaymentType = "TC";

            }
        }
        public void UpdateShoppinCartWithCouponDetails(MOBShoppingCart persistShoppingCart)
        {
            if (persistShoppingCart != null && persistShoppingCart.Products.Any())
            {
                persistShoppingCart.PromoCodeDetails = new MOBPromoCodeDetails();
                persistShoppingCart.PromoCodeDetails.PromoCodes = new List<MOBPromoCode>();
                persistShoppingCart.Products.ForEach(product =>
                {
                    if (product.CouponDetails != null && product.CouponDetails.Any())
                    {
                        product.CouponDetails.ForEach(CouponDetail =>
                        {
                            if (_configuration.GetValue<bool>("EnableFareandAncillaryPromoCodeChanges") ? !IsDuplicatePromoCode(persistShoppingCart.PromoCodeDetails.PromoCodes, CouponDetail.PromoCode) : true)
                            {
                                persistShoppingCart.PromoCodeDetails.PromoCodes
                                .Add(new MOBPromoCode
                                {
                                    PromoCode = CouponDetail.PromoCode,
                                    AlertMessage = CouponDetail.Description,
                                    IsSuccess = true,
                                    TermsandConditions = new MOBMobileCMSContentMessages
                                    {
                                        Title = _configuration.GetValue<string>("PromoCodeTermsandConditionsTitle"),
                                        HeadLine = _configuration.GetValue<string>("PromoCodeTermsandConditionsTitle")
                                    }
                                });
                            }
                        });
                    }
                });
            }
        }
        public void IsHidePromoOption(MOBShoppingCart shoppingCart)
        {
            bool isTravelCertificateAdded = shoppingCart?.FormofPaymentDetails?.TravelCertificate != null && shoppingCart.FormofPaymentDetails.TravelCertificate.Certificates != null && shoppingCart.FormofPaymentDetails.TravelCertificate.Certificates.Count > 0;
            bool isCouponAdded = (shoppingCart?.PromoCodeDetails?.PromoCodes != null && shoppingCart.PromoCodeDetails.PromoCodes.Count > 0);
            if (shoppingCart?.Products != null && shoppingCart.Products.Any(p => p?.Code?.ToUpper() == "FARELOCK" || p?.Code?.ToUpper() == "FLK"))
            {
                shoppingCart.PromoCodeDetails.IsHidePromoOption = true;
                return;
            }

            if (!isCouponAdded && (_configuration.GetValue<string>("Fops_HidePromoOption").Contains(shoppingCart?.FormofPaymentDetails?.FormOfPaymentType)
                || (_configuration.GetValue<string>("Fops_HidePromoOption").Contains("ETC") && isTravelCertificateAdded)))
            {
                shoppingCart.PromoCodeDetails.IsHidePromoOption = true;
            }
            else
            {
                shoppingCart.PromoCodeDetails.IsHidePromoOption = false;
            }
        }
        private bool IsDuplicatePromoCode(List<MOBPromoCode> promoCodes, string promoCode)
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
        #endregion

        #region MFOP
        private async System.Threading.Tasks.Task AssignETCFFCvalues(string sessionid, MOBShoppingCart shoppingCart, MOBRequest request, MOBSHOPReservation reservation)
        {
            try
            {

                if (shoppingCart.FormofPaymentDetails?.TravelFutureFlightCredit?.FutureFlightCredits?.Count > 0)
                {
                    UpdateFFCPricesInReservation(reservation.Prices);
                }
                else
                {
                    reservation.Prices.Remove(reservation.Prices.FirstOrDefault(x => x.DisplayType.ToUpper() == "REFUNDFFCPRICE"));
                }
                if (shoppingCart.FormofPaymentDetails?.TravelCertificate?.Certificates?.Count > 0)
                {
                    UpdateETCPricesInReservation(reservation.Prices);
                }
                else
                {
                    reservation.Prices.Remove(reservation.Prices.FirstOrDefault(x => x.DisplayType.ToUpper() == "REFUNDCERTIFICATEPRICE"));
                }

                if ((shoppingCart.FormofPaymentDetails?.TravelCertificate?.Certificates?.Count > 0) || (shoppingCart.FormofPaymentDetails?.TravelFutureFlightCredit?.FutureFlightCredits?.Count > 0))
                {
                    Reservation bookingPathReservation = new Reservation();
                    bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionid, bookingPathReservation.ObjectName, new List<string> { sessionid, bookingPathReservation.ObjectName }).ConfigureAwait(false);
                    bookingPathReservation.Prices = reservation.Prices;
                    await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, sessionid, new List<string> { sessionid, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                }
            }
            catch (Exception ex) { }
        }
        private void UpdateETCPricesInReservation(List<MOBSHOPPrice> prices)
        {
            var etcPrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == "CERTIFICATE");
            var totalCreditValue = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == "REFUNDCERTIFICATEPRICE");

            if (totalCreditValue != null)
                prices.Remove(totalCreditValue);

            if (etcPrice != null && etcPrice?.Value > 0)
                UpdateCertificatePrice(etcPrice, etcPrice.Value, "CERTIFICATE", "Travel certificate", "");
            else
                prices.Remove(etcPrice);

            if (totalCreditValue.Value > 0)
            {
                var totalCreditETC = new MOBSHOPPrice();
                prices.Add(totalCreditETC);
                UpdateCertificatePrice(totalCreditETC, totalCreditValue.Value, "REFUNDCERTIFICATEPRICE", "Remaining travel certificate", "RESIDUALCREDIT");
            }
        }
        private void UpdateFFCPricesInReservation(List<MOBSHOPPrice> prices)
        {
            var FFCPrice = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == "FFC");
            var totalCreditValue = prices.FirstOrDefault(p => p.DisplayType.ToUpper() == "REFUNDFFCPRICE");

            string priceTypeDescription = "Future flight credit";

            if (totalCreditValue != null)
                prices.Remove(totalCreditValue);

            if (FFCPrice != null && FFCPrice?.Value > 0)
                UpdateCertificatePrice(FFCPrice, FFCPrice.Value, "FFC", priceTypeDescription, "");
            else
                prices.Remove(FFCPrice);

            if (totalCreditValue != null && totalCreditValue?.Value > 0)
            {
                var totalCreditETC = new MOBSHOPPrice();
                prices.Add(totalCreditETC);
                UpdateCertificatePrice(totalCreditETC, totalCreditValue.Value, "REFUNDFFCPRICE", "Remaining travel certificate", "RESIDUALCREDIT");
            }
        }
        private MOBSHOPPrice UpdateCertificatePrice(MOBSHOPPrice ffc, double totalAmount, string priceType, string priceTypeDescription, string status = "", bool isAddNegative = false)
        {
            ffc.CurrencyCode = "USD";
            ffc.DisplayType = priceType;
            ffc.PriceType = priceType;
            ffc.Status = status;
            ffc.PriceTypeDescription = priceTypeDescription;
            ffc.Value = totalAmount;
            ffc.Value = Math.Round(ffc.Value, 2, MidpointRounding.AwayFromZero);
            ffc.FormattedDisplayValue = (isAddNegative ? "-" : "") + (ffc.Value).ToString("C2", CultureInfo.CurrentCulture);
            ffc.DisplayValue = string.Format("{0:#,0.00}", ffc.Value);
            return ffc;
        }


        #endregion
        public bool IsEnableEditSearchOnFSRHeaderBooking(int applicationId, string appVersion, List<MOBItem> catalogItems = null)
        {
            return _configuration.GetValue<bool>("EnableEditSearchHeaderOnFSRBooking") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableEditSearchHeaderOnFSRBooking_AppVersion"), _configuration.GetValue<string>("iPhone_EnableEditSearchHeaderOnFSRBooking_AppVersion")) && (catalogItems != null && catalogItems.Count > 0 && catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableEditSearchOnFSRHeader).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableEditSearchOnFSRHeader).ToString())?.CurrentValue == "1");
        }

        public bool EnableEditSearchOnFSRHeaderBooking(MOBSHOPShopRequest request, bool isEnableMultiCityEditSearchOnFSRBooking)
        {
            return request != null && !(request.IsReshop || request.IsReshopChange)
              && !request.IsCorporateBooking && string.IsNullOrEmpty(request.EmployeeDiscountId)
                && (request.SearchType == "MD" ? isEnableMultiCityEditSearchOnFSRBooking : true)
                && !request.IsYoungAdultBooking && request.TravelType != TravelType.CLB.ToString();
        }

        public bool EnableEditSearchOnFSRHeaderBooking(SelectTripRequest request, Session session)
        {
            return request != null && !session.IsReshopChange
              && !session.IsCorporateBooking && string.IsNullOrEmpty(session.EmployeeId)
              && !session.IsYoungAdult && session.TravelType != TravelType.CLB.ToString();
        }
        public bool IsEnableU4BCorporateBooking(int applicationId, string appVersion, List<MOBItem> catalogItems = null)
        {
            return _configuration.GetValue<bool>("EnableU4BCorporateBooking") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableU4BCorporateBooking_AppVersion"), _configuration.GetValue<string>("IPhone_EnableU4BCorporateBooking_AppVersion")) && (catalogItems != null && catalogItems.Count > 0 && catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableU4BCorporateBooking).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableU4BCorporateBooking).ToString())?.CurrentValue == "1");
        }
        public bool IsEnableU4BCorporateBooking(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableU4BCorporateBooking") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableU4BCorporateBooking_AppVersion"), _configuration.GetValue<string>("IPhone_EnableU4BCorporateBooking_AppVersion"));
        }

        public async Task<bool> IsEnableU4BCorporateTravelSessionFix()
        {
            return await _featureSettings.GetFeatureSettingValue("IsEnableU4BCorporateTravelSessionFix").ConfigureAwait(false);
        }

        public async Task<bool> IsEnableU4BTravelAddONPolicy(bool isCorporate, int applicationId, string appVersion, List<MOBItem> catalogItems = null)
        {
            if (_configuration.GetValue<bool>("EnableFeatureSettingsChanges"))
            {
                return isCorporate && (await IsEnableU4BTravelAddONPolicy(applicationId, appVersion).ConfigureAwait(false));
            }
            else
            {
                return isCorporate && (await IsEnableU4BTravelAddONPolicy(applicationId, appVersion).ConfigureAwait(false)) && (catalogItems != null && catalogItems.Count > 0 && catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableU4BTravelAddOnPolicy).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableU4BTravelAddOnPolicy).ToString())?.CurrentValue == "1");
            }
        }

        public async Task<bool> IsEnableU4BTravelAddONPolicy(int applicationId, string appVersion)
        {
            if (_configuration.GetValue<bool>("EnableFeatureSettingsChanges"))
            {
                return await _featureSettings.GetFeatureSettingValue("EnableU4BTravelAddOnPolicy").ConfigureAwait(false) && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableU4BTravelAddOnPolicy_AppVersion"), _configuration.GetValue<string>("IPhone_EnableU4BTravelAddOnPolicy_AppVersion"));
            }
            else
            {
                return _configuration.GetValue<bool>("EnableU4BTravelAddOnPolicy") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableU4BTravelAddOnPolicy_AppVersion"), _configuration.GetValue<string>("IPhone_EnableU4BTravelAddOnPolicy_AppVersion"));
            }
        }

        public async Task<Mobile.Model.Shopping.TravelPolicy> GetTravelPolicy(United.CorporateDirect.Models.CustomerProfile.CorpPolicyResponse response, Session session, MOBRequest request, string corporateCompanyName, bool isCorporateNamePersonalized)
        {
            Mobile.Model.Shopping.TravelPolicy corpTravelPolicy = null;
            try
            {
                if (response.TravelPolicies != null && response.TravelPolicies.Count > 0 && request != null && !string.IsNullOrEmpty(corporateCompanyName))
                {
                    corpTravelPolicy = new Mobile.Model.Shopping.TravelPolicy();
                    List<CMSContentMessage> lstMessages = await GetCorporateSDLMessages(session, request);

                    List<MOBMobileCMSContentMessages> travelPolicyTitle = null;
                    List<MOBMobileCMSContentMessages> travelPolicyBudget = null;
                    List<MOBMobileCMSContentMessages> travelPolicySeat = null;
                    List<MOBMobileCMSContentMessages> travelPolicyFSRPageTitle = null;
                    List<MOBMobileCMSContentMessages> travelPolicyCorporateBusinessNamePersonalizedTitle = null;

                    CorporateDirect.Models.CustomerProfile.CorporateTravelPolicy travelPolicy = response?.TravelPolicies?.FirstOrDefault();
                    CorporateDirect.Models.CustomerProfile.CorporateTravelCabinRestriction travelCabinRestrictions = null;
                    travelCabinRestrictions = travelPolicy?.TravelCabinRestrictions?.FirstOrDefault(x => x != null && !string.IsNullOrEmpty(x.TripTypeCode) && x.TripTypeCode == "DE");
                    if (travelCabinRestrictions == null)
                        travelCabinRestrictions = travelPolicy?.TravelCabinRestrictions?.FirstOrDefault();

                    string cabinNameAllowedForLongTripDuration = string.Empty;
                    int duration = 0;
                    string cabinNameAllowed = GetCabinNameFromCorpTravelPolicy(travelCabinRestrictions);
                    if (travelPolicy?.TravelCabinRestrictions != null && travelPolicy?.TravelCabinRestrictions.Count > 1) //If short trip/long trip duration is available
                    {
                        CorporateDirect.Models.CustomerProfile.CorporateTravelCabinRestriction travelCabinRestrictionsForLongTripDuration = travelPolicy?.TravelCabinRestrictions?.FirstOrDefault(x => x != null && !string.IsNullOrEmpty(x.TripTypeCode) && x.TripTypeCode == "LT");
                        duration = travelCabinRestrictionsForLongTripDuration?.Duration != null ? Convert.ToInt32(travelCabinRestrictionsForLongTripDuration?.Duration) : 0;
                        if (duration > 0)
                            cabinNameAllowedForLongTripDuration = GetCabinNameFromCorpTravelPolicy(travelCabinRestrictionsForLongTripDuration);
                    }

                    bool isFarePlusTravelAddOnEnabled = response.TravelPolicies.FirstOrDefault().IsAirfarePlusTravelAddOn;
                    bool hasDurationFromTravelPolicy = duration > 0 && !string.IsNullOrEmpty(cabinNameAllowedForLongTripDuration);
                    if (lstMessages != null && lstMessages.Count > 0)
                    {
                        travelPolicyTitle = GetSDLMessageFromList(lstMessages, "TravelPolicy.Title");
                        string travelBudgetKey = isFarePlusTravelAddOnEnabled ? "TravelPolicyAddOn.Budget" : "TravelPolicy.Budget";
                        string travelPolicySeatKey = hasDurationFromTravelPolicy ? "TravelPolicyDuration.Seat" : "TravelPolicy.Seat";
                        travelPolicyBudget = GetSDLMessageFromList(lstMessages, travelBudgetKey);
                        travelPolicySeat = GetSDLMessageFromList(lstMessages, travelPolicySeatKey);
                        travelPolicyFSRPageTitle = GetSDLMessageFromList(lstMessages, "TravelPolicyAlert.OutOfPolicy.PolicyLink");
                        travelPolicyCorporateBusinessNamePersonalizedTitle = GetSDLMessageFromList(lstMessages, "TravelPolicy.CorporateBusinessNamePersonalizedTitle");
                    }
                    bool isEnableSuppressingCompanyNameForBusiness = await IsEnableSuppressingCompanyNameForBusiness(isCorporateNamePersonalized).ConfigureAwait(false);
                    corpTravelPolicy.TravelPolicyTitleForFSRLink = travelPolicyFSRPageTitle?.FirstOrDefault()?.ContentFull;
                    corpTravelPolicy.TravelPolicyTitle = travelPolicyTitle?.FirstOrDefault()?.ContentShort;
                    if (await IsEnableCorporateNameChange().ConfigureAwait(false))
                        corpTravelPolicy.TravelPolicyHeader = isEnableSuppressingCompanyNameForBusiness ? string.Format(travelPolicyTitle?.FirstOrDefault()?.ContentFull, corporateCompanyName) : travelPolicyCorporateBusinessNamePersonalizedTitle?.FirstOrDefault()?.ContentFull;
                    else
                        corpTravelPolicy.TravelPolicyHeader = string.Format(travelPolicyTitle?.FirstOrDefault()?.ContentFull, corporateCompanyName);

                    corpTravelPolicy.TravelPolicyContent = new List<MOBSection>();
                    if (travelPolicy?.MaximumBudget > 0 && (travelPolicy.IsAirfare || travelPolicy.IsAirfarePlusTravelAddOn))
                    {
                        corpTravelPolicy.TravelPolicyContent.Add(new MOBSection
                        {
                            Text1 = travelPolicyBudget?.FirstOrDefault()?.ContentShort?.Split('|')?.FirstOrDefault(),
                            Text2 = string.Format(travelPolicyBudget?.FirstOrDefault()?.ContentFull, GetTravelPolicyBudgetAmount(travelPolicy?.MaximumBudget)),
                            Text3 = travelPolicyBudget?.FirstOrDefault()?.ContentShort?.Split('|')?.LastOrDefault()
                        });
                    }

                    if (!string.IsNullOrEmpty(cabinNameAllowed))
                    {
                        corpTravelPolicy.TravelPolicyContent.Add(new MOBSection
                        {
                            Text1 = travelPolicySeat?.FirstOrDefault()?.ContentShort?.Split('|')?.FirstOrDefault(),
                            Text2 = hasDurationFromTravelPolicy ? string.Format(travelPolicySeat?.FirstOrDefault()?.ContentFull, duration, cabinNameAllowed, cabinNameAllowedForLongTripDuration) : string.Format(travelPolicySeat?.FirstOrDefault()?.ContentFull, cabinNameAllowed),
                            Text3 = travelPolicySeat?.FirstOrDefault()?.ContentShort?.Split('|')?.LastOrDefault()
                        });
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return corpTravelPolicy;
        }

        public async Task<bool> IsEnableSuppressingCompanyNameForBusiness(bool isCorporateNamePersonalized)
        {
            return await _featureSettings.GetFeatureSettingValue("EnableSuppressingCompanyNameForBusiness").ConfigureAwait(false) && isCorporateNamePersonalized;
        }

        public async Task<bool> IsEnableCorporateNameChange()
        {
            return await _featureSettings.GetFeatureSettingValue("IsEnableCorporateNameChange").ConfigureAwait(false);
        }

        public async Task<bool> IsEnablePassingPersonalizedFlagToClient()
        {
            return await _featureSettings.GetFeatureSettingValue("IsEnablePassingPersonalizedFlagToClient").ConfigureAwait(false);
        }

        public static string GetTravelPolicyBudgetAmount(int? maximumBudget)
        {
            return string.Format("{0:n0}", maximumBudget);
        }

        public async Task<List<CMSContentMessage>> GetCorporateSDLMessages(Session session, MOBRequest request)
        {
            return await _ffcShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("U4BCorporateContentMessageGroupName"), "U4BCorporateContentMessageCache");
        }

        public async Task<List<InfoWarningMessages>> BuildTravelPolicyAlert(United.CorporateDirect.Models.CustomerProfile.CorpPolicyResponse travelPolicies, MOBRequest request, FlightReservationResponse response, Session session, bool isCorporateNamePersonalized)
        {
            List<InfoWarningMessages> outOfPolicyMessage = null;
            try
            {
                if (request != null && session != null && response != null && response.Warnings != null)
                {
                    //Build Cabin descriptions allowed based on travel policy
                    CorporateDirect.Models.CustomerProfile.CorporateTravelPolicy travelPolicy = travelPolicies?.TravelPolicies?.FirstOrDefault();
                    CorporateDirect.Models.CustomerProfile.CorporateTravelCabinRestriction travelCabinRestrictions = null;
                    travelCabinRestrictions = travelPolicy?.TravelCabinRestrictions?.FirstOrDefault(x => x != null && !string.IsNullOrEmpty(x.TripTypeCode) && x.TripTypeCode == "DE");
                    if (travelCabinRestrictions == null)
                        travelCabinRestrictions = travelPolicy?.TravelCabinRestrictions?.FirstOrDefault();

                    string cabinNameAllowedForLongTripDuration = string.Empty;
                    int duration = 0;
                    string cabinNameAllowed = GetCabinNameFromCorpTravelPolicy(travelCabinRestrictions);
                    if (travelPolicy?.TravelCabinRestrictions != null && travelPolicy?.TravelCabinRestrictions.Count > 1) //If short trip/long trip duration is available
                    {
                        CorporateDirect.Models.CustomerProfile.CorporateTravelCabinRestriction travelCabinRestrictionsForLongTripDuration = travelPolicy?.TravelCabinRestrictions?.FirstOrDefault(x => x != null && !string.IsNullOrEmpty(x.TripTypeCode) && x.TripTypeCode == "LT");
                        duration = travelCabinRestrictionsForLongTripDuration?.Duration != null ? Convert.ToInt32(travelCabinRestrictionsForLongTripDuration?.Duration) : 0;
                        if (duration > 0)
                            cabinNameAllowedForLongTripDuration = GetCabinNameFromCorpTravelPolicy(travelCabinRestrictionsForLongTripDuration);
                    }

                    List<CMSContentMessage> lstMessages = await GetCorporateSDLMessages(session, request);

                    if (lstMessages != null && lstMessages.Count > 0)
                    {
                        InfoWarningMessages travelPolicyWarningMessage = new InfoWarningMessages();
                        List<MOBMobileCMSContentMessages> travelPolicyTitle = null;
                        List<MOBMobileCMSContentMessages> travelPolicyMessage = null;
                        List<MOBMobileCMSContentMessages> travelPolicyBudget = null;
                        List<MOBMobileCMSContentMessages> travelPolicySeat = null;
                        List<MOBMobileCMSContentMessages> travelPolicyCorporateBusinessPolicyGuidelines = null;

                        travelPolicyTitle = GetSDLMessageFromList(lstMessages, "TravelPolicyAlert.OutOfPolicy.PolicyLink");
                        travelPolicyWarningMessage.HeaderMessage = travelPolicyTitle?.FirstOrDefault()?.ContentShort;
                        travelPolicyWarningMessage.ButtonLabel = travelPolicyTitle?.FirstOrDefault()?.ContentFull;

                        travelPolicyWarningMessage.Messages = new List<string>();
                        travelPolicyMessage = GetSDLMessageFromList(lstMessages, "TravelPolicyAlert.OutOfPolicy.Message");
                        travelPolicyCorporateBusinessPolicyGuidelines = GetSDLMessageFromList(lstMessages, "TravelPolicyAlert.OutOfPolicy.CorporateBusinessPolicyGuidelines");

                        bool isFarePlusTravelAddOnEnabled = travelPolicy.IsAirfarePlusTravelAddOn;
                        bool hasDurationFromTravelPolicy = duration > 0 && !string.IsNullOrEmpty(cabinNameAllowedForLongTripDuration);

                        travelPolicyBudget = isFarePlusTravelAddOnEnabled ? GetSDLMessageFromList(lstMessages, "TravelPolicyAlertAddOn.OutOfPolicy.Budget") : GetSDLMessageFromList(lstMessages, "TravelPolicyAlert.OutOfPolicy.Budget");
                        bool hasDurationWarningMesssage = response.Warnings.Any(x => x.MinorCode == "21900") && response.Warnings.Any(x => x.MinorCode == "21800");
                        travelPolicySeat = hasDurationFromTravelPolicy && hasDurationWarningMesssage ? GetSDLMessageFromList(lstMessages, "TravelPolicyAlertDurationLT.OutOfPolicy.Seat") : hasDurationFromTravelPolicy ? GetSDLMessageFromList(lstMessages, "TravelPolicyAlertDurationST.OutOfPolicy.Seat") : GetSDLMessageFromList(lstMessages, "TravelPolicyAlert.OutOfPolicy.Seat");

                        var corporateData = response.Reservation?.Travelers?.FirstOrDefault()?.CorporateData;
                        string corporateCompanyName = corporateData?.CompanyName;

                        string headerMessage = string.Empty;
                        if (await IsEnableCorporateNameChange().ConfigureAwait(false))
                            headerMessage = await IsEnableSuppressingCompanyNameForBusiness(isCorporateNamePersonalized).ConfigureAwait(false) ? string.Format(travelPolicyMessage?.FirstOrDefault()?.ContentFull, corporateCompanyName) : travelPolicyCorporateBusinessPolicyGuidelines?.FirstOrDefault()?.ContentFull;
                        else
                            headerMessage = string.Format(travelPolicyMessage?.FirstOrDefault()?.ContentFull, corporateCompanyName);

                        string budgetMessage = string.Empty;
                        if (response.Warnings.Any(x => !string.IsNullOrEmpty(x.MinorCode) && x.MinorCode == "21700") && travelPolicy?.MaximumBudget > 0)
                        {
                            budgetMessage = string.Format(travelPolicyBudget?.FirstOrDefault()?.ContentFull, GetTravelPolicyBudgetAmount(travelPolicy?.MaximumBudget));
                        }
                        string seatMessage = string.Empty;
                        if (response.Warnings.Any(x => !string.IsNullOrEmpty(x.MinorCode) && (x.MinorCode == "21800")))
                        {
                            if (hasDurationFromTravelPolicy && hasDurationWarningMesssage)
                                seatMessage = string.Format(travelPolicySeat?.FirstOrDefault()?.ContentFull, duration, cabinNameAllowedForLongTripDuration);
                            else if (hasDurationFromTravelPolicy)
                                seatMessage = string.Format(travelPolicySeat?.FirstOrDefault()?.ContentFull, duration, cabinNameAllowed);
                            else
                                seatMessage = string.Format(travelPolicySeat?.FirstOrDefault()?.ContentFull, cabinNameAllowed);
                        }

                        string alertMessage = headerMessage + budgetMessage + seatMessage;
                        travelPolicyWarningMessage.Messages.Add(alertMessage);
                        travelPolicyWarningMessage.IconType = MOBINFOWARNINGMESSAGEICON.CAUTION.ToString();
                        travelPolicyWarningMessage.IsCollapsable = true;
                        travelPolicyWarningMessage.Order = MOBINFOWARNINGMESSAGEORDER.CORPORATETRAVELOUTOFPOLICY.ToString();


                        if (!string.IsNullOrEmpty(travelPolicyWarningMessage.HeaderMessage) && travelPolicyMessage != null && travelPolicyMessage.Count > 0)
                        {
                            outOfPolicyMessage = new List<InfoWarningMessages>();
                            outOfPolicyMessage.Add(travelPolicyWarningMessage);
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            return outOfPolicyMessage;
        }

        public async Task<CorporateDirect.Models.CustomerProfile.CorpPolicyResponse> GetCorporateTravelPolicyResponse(string deviceId, string mileagePlusNumber, string sessionId)
        {
            United.CorporateDirect.Models.CustomerProfile.CorpPolicyResponse _corpPolicyResponse = null;
            _corpPolicyResponse = await _sessionHelperService.GetSession<United.CorporateDirect.Models.CustomerProfile.CorpPolicyResponse>(deviceId + mileagePlusNumber, ObjectNames.CSLCorporatePolicyResponse, new List<string> { deviceId + mileagePlusNumber, ObjectNames.CSLCorporatePolicyResponse }).ConfigureAwait(false);
            return _corpPolicyResponse;
        }

        public bool HasPolicyWarningMessage(List<Services.FlightShopping.Common.ErrorInfo> response)
        {
            return response != null && response.Count > 0 && response.Any(x => !string.IsNullOrEmpty(x.MinorCode) && (x.MinorCode == "21700" || x.MinorCode == "21800" || x.MinorCode == "21900"));
        }

        public string GetCabinNameFromCorpTravelPolicy(CorporateDirect.Models.CustomerProfile.CorporateTravelCabinRestriction travelCabinRestrictions)
        {
            string cabinNameAllowed = string.Empty;
            var U4BCorporateCabinTypes = _configuration.GetValue<string>("U4BCorporateCabinTypes").Split('|');

            //if (travelPolicy.IsBasicEconomyAllowed.Value)
            //    cabinNameAllowed = cabinNameAllowed + U4BCorporateCabinTypes[0];
            if (travelCabinRestrictions.IsEconomyAllowed.Value)
                cabinNameAllowed = !string.IsNullOrEmpty(cabinNameAllowed) ? cabinNameAllowed + ", " + U4BCorporateCabinTypes[1] : cabinNameAllowed + U4BCorporateCabinTypes[1];
            if (travelCabinRestrictions.IsPremiumEconomyAllowed.Value)
                cabinNameAllowed = !string.IsNullOrEmpty(cabinNameAllowed) ? cabinNameAllowed + ", " + U4BCorporateCabinTypes[2] : cabinNameAllowed + U4BCorporateCabinTypes[2];
            if (travelCabinRestrictions.IsBusinessFirstAllowed.Value)
                cabinNameAllowed = !string.IsNullOrEmpty(cabinNameAllowed) ? cabinNameAllowed + ", " + U4BCorporateCabinTypes[3] : cabinNameAllowed + U4BCorporateCabinTypes[3];
            return cabinNameAllowed;
        }

        public bool EnableAwardNonStop(int applicationId, string appVersion, bool isReshopChange, bool isAward = true)
        {
            return _configuration.GetValue<bool>("EnableNonstopForAward") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableNonstopForAward_AppVersion"), _configuration.GetValue<string>("IPhone_EnableNonstopForAward_AppVersion")) && !isReshopChange && isAward;
        }

        public bool EnableAwardNonStop(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableNonstopForAward") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableNonstopForAward_AppVersion"), _configuration.GetValue<string>("IPhone_EnableNonstopForAward_AppVersion"));
        }

        public bool GetFlightsByCountryCode(List<MOBSHOPTrip> trips, string countryCode)
        {
            return (trips.SelectMany(a => a.FlattenedFlights)?.SelectMany(b => b.Flights)?
                   .Any(c => c.DestinationCountryCode == countryCode || c.OriginCountryCode == countryCode) == true) ? true : false;
        }

        public bool IsCanadaTravelNumberEnabled(int appId, string version)
        {
            return _configuration.GetValue<bool>("EnableCanadaTravelNumber") && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, version, _configuration.GetValue<string>("Android_EnableCanadaTravelNumber_AppVersion"), _configuration.GetValue<string>("iPhone_EnableCanadaTravelNumber_AppVersion"));
        }

        public bool IsNoFlightsSeasonalityFeatureEnabled(int id, string version, List<MOBItem> catalogItems)
        {
            if (!_configuration.GetValue<bool>("EnableNoFlightsSeasonalityFeature")) return false;
            if (catalogItems != null && catalogItems.Count > 0 &&
                              catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableNoFlightsSeasonalityFeature).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableNoFlightsSeasonalityFeature).ToString())?.CurrentValue == "1")
                return GeneralHelper.IsApplicationVersionGreaterorEqual(id, version, _configuration.GetValue<string>("Android_EnableNoFlightsSeasonalityFeature_AppVersion"), _configuration.GetValue<string>("IPhone_EnableNoFlightsSeasonalityFeature_AppVersion"));
            else
                return false;
        }

        public bool IsReverseSearchByMapOrderForNoFlightsFound(int id, string version, List<MOBItem> catalogItems)
        {
            if (!_configuration.GetValue<bool>("EnableSearchByMapFirstForNoFlightsFound")) return false;
            if (catalogItems != null && catalogItems.Count > 0 &&
                              catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableSearchByMapFirstForNoFlightsFound).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableSearchByMapFirstForNoFlightsFound).ToString())?.CurrentValue == "1")
                return true;
            else
                return false;
        }

        public async void SetCanadaTravelNumberDetails(MOBSHOPAvailability availability, MOBRequest request, Session session)
        {
            if (IsCanadaTravelNumberEnabled(request.Application.Id, request.Application.Version.Major))
            {
                List<CMSContentMessage> lstMessages = await _ffcShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");

                string canadaCountryCode = "CA";
                availability.Reservation.IsCanadaTravel = GetFlightsByCountryCode(availability.Reservation.Trips, canadaCountryCode);
                if (availability.Reservation.IsCanadaTravel)
                {
                    if (availability.Reservation.TCDAdvisoryMessages == null)
                        availability.Reservation.TCDAdvisoryMessages = new List<MOBItem>();
                    availability.Reservation.TCDAdvisoryMessages.Add(new MOBItem
                    {
                        Id = "CanadaTravelDisclaimer",
                        CurrentValue = GetSDLStringMessageFromList(lstMessages, "FSR_CanadaTravelNumber_Disclaimer_MOB")
                    }); ;
                }
            }
        }

        public void SetCarbonEmissionDetailsForConnections(MOBCarbonEmissionsResponse carbonEmissionData, MOBSHOPFlight flight)
        {
            if (_configuration.GetValue<bool>("EnableCarbonEmissionsFeature") && carbonEmissionData != null && carbonEmissionData.CarbonEmissionData?.Count > 0)
            {
                flight.CarbonEmissionData = carbonEmissionData.CarbonEmissionData.Where(a => a.FlightHash == flight.FlightHash)?.LastOrDefault();
            }
        }

        public bool IsEnableCarbonEmissionsFeature(int id, string version, List<MOBItem> catalogItems)
        {
            if (!_configuration.GetValue<bool>("EnableCarbonEmissionsFeature")) return false;

            return GeneralHelper.IsApplicationVersionGreaterorEqual(id, version, _configuration.GetValue<string>("Android_EnableCarbonEmissionsFeature_AppVersion"), _configuration.GetValue<string>("Iphone_EnableCarbonEmissionsFeature_AppVersion"));

        }

        public bool IsEnableSelectTripResponseRequestFromBackend(int id, string version, List<MOBItem> catalogItems)
        {
            if (!_configuration.GetValue<bool>("EnableSelectTripResponseRequestFromBackend")) return false;
            if (catalogItems != null && catalogItems.Count > 0 &&
                              catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableFareWheelFiltersFromSelectTripResponseRequest).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableFareWheelFiltersFromSelectTripResponseRequest).ToString())?.CurrentValue == "1")
                return true;
            else
                return false;
        }


        public bool IsEnableFareLockAmoutDisplayPerPerson(int id, string version, List<MOBItem> catalogItems)
        {
            if (!_configuration.GetValue<bool>("EnableFareLockAmoutDisplayPerPerson")) return false;
            if (catalogItems != null && catalogItems.Count > 0 &&
                              catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableFareLockAmoutDisplayPerPerson).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableFareLockAmoutDisplayPerPerson).ToString())?.CurrentValue == "1")
                return GeneralHelper.IsApplicationVersionGreaterorEqual(id, version, _configuration.GetValue<string>("Android_EnableFareLockAmoutDisplayPerPerson_AppVersion"), _configuration.GetValue<string>("IPhone_EnableFareLockAmoutDisplayPerPerson_AppVersion"));
            else return false;
        }

        public async Task<MOBCarbonEmissionsResponse> LoadCarbonEmissionsDataFromPersist(Session session)
        {
            if (_configuration.GetValue<bool>("EnableCarbonEmissionsFeature"))
            {
                try
                {
                    MOBCarbonEmissionsResponse carboinEmissionData = new MOBCarbonEmissionsResponse();
                    carboinEmissionData = await _sessionHelperService.GetSession<MOBCarbonEmissionsResponse>(session.SessionId, carboinEmissionData.ObjectName, new List<string> { session.SessionId, carboinEmissionData.ObjectName }).ConfigureAwait(false);
                    return carboinEmissionData;
                }
                catch { }
            }
            return null;
        }

        public async Task<string> GetGMTTimeFromOffset(string strDate, double offsetInHours)
        {


            if (!String.IsNullOrEmpty(strDate))
            {
                DateTime getDateTime;
                if (DateTime.TryParse(strDate, out getDateTime))
                {
                    return getDateTime.AddHours(-offsetInHours).ToString("MM/dd/yyyy hh:mm tt");
                }
                else
                {
                    return strDate;
                }
            }
            return await Task.FromResult(strDate);
        }
        public void AddMobLegalDocumentItem(List<MOBLegalDocument> docs, List<MOBItem> messages, string key)
        {
            MOBLegalDocument doc = docs.FirstOrDefault(d => d != null && !string.IsNullOrEmpty(d.Title) && d.Title.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (doc != null)
            {
                MOBItem item = new MOBItem
                {
                    Id = doc.Title,
                    CurrentValue = doc.LegalDocument
                };
                messages.Add(item);
            }
        }

        public bool IsEnableGetFSRDInfoFromCSL(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableGetFSRDInfoFromCSL")
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "Android_EnableGetFSRDInfoFromCSL_AppVersion", "iPhone_EnableGetFSRDInfoFromCSL_AppVersion", "", "", true, _configuration);
        }

        public bool IsBookingSeatMapNotSupportedOtherAirlines(string operatingCarrier)
        {
            var bookingSeatMapNotSupportedOtherAirlines = _configuration.GetValue<string>("BookingSeatMapNotSupportedOtherAirlines");
            if (string.IsNullOrEmpty(bookingSeatMapNotSupportedOtherAirlines)) return false;

            return (bookingSeatMapNotSupportedOtherAirlines.Contains(operatingCarrier.Trim().ToUpper()));
        }

        public async Task<bool> IsDynamicLegendsEnabled(int applicationId, string appVersion)
        {
            return await _featureSettings.GetFeatureSettingValue("EnableBELegendsFeature").ConfigureAwait(false) && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableDynamicLegends_AppVersion"), _configuration.GetValue<string>("Iphone_EnableDynamicLegends_AppVersion"));
        }
        public async Task<List<MOBDimensions>> GetFlightDimensionsList(string transId)
        {
            List<MOBDimensions> response = null;
            try
            {
                var flightDimesions = await _cachingService.GetCache<string>(_configuration.GetValue<string>("MOBFlightCargoDimensions_Cache_key"), transId).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(flightDimesions))
                {
                    response = JsonConvert.DeserializeObject<List<MOBDimensions>>(flightDimesions);
                    if (response != null && response.Count > 0) { return response; }
                }
            }
            catch { }

            response = await _auroraMySqlService.GetFlghtCargoDoorDimensions().ConfigureAwait(false);
            if (response == null)
            {
                _logger.LogError("GetFlightDimensionsList failed to get data from database");
                return null;
            }
            if (response != null && response.Any() && response.Count > 0)
                await _cachingService.SaveCache<List<MOBDimensions>>(_configuration.GetValue<string>("MOBFlightCargoDimensions_Cache_key"), response, transId, new TimeSpan(10950, 1, 30, 0)).ConfigureAwait(false);
            return response;
        }
        public double ConvertToInches(double dimensionInCm)
        {
            double dimensionInInches = 0.393701 * dimensionInCm;
            return dimensionInInches;
        }

        public FlightCombinationType GetFlightCombinationType(List<MOBSHOPTrip> trips)
        {
            var unitedCarriers = _configuration.GetValue<string>("UnitedCarriers");
            var totalFlightSegments = trips?.SelectMany(a => a.FlattenedFlights)?.SelectMany(b => b.Flights).ToList();
            var UAFlightSegments = trips?.SelectMany(a => a.FlattenedFlights)?.SelectMany(b => b.Flights)?
            .Where(c => unitedCarriers.Contains(c.OperatingCarrier?.Trim().ToUpper())).ToList();
            if (UAFlightSegments == null || UAFlightSegments.Count == 0 || !UAFlightSegments.Any())
                return FlightCombinationType.OAFlights;
            else if (UAFlightSegments != null && UAFlightSegments.Count > 0 && totalFlightSegments?.Count == UAFlightSegments?.Count)
                return FlightCombinationType.UAandUAXFlights;
            else
                return FlightCombinationType.UAandOAFlights;
        }
        public void BuildWheelChairSizerOAMsgs(MOBSHOPReservation reservation)
        {
            if (reservation != null && reservation.Trips != null && reservation.ShopReservationInfo2 != null)
            {
                var flightCombinationType = GetFlightCombinationType(reservation?.Trips);
                if (flightCombinationType == FlightCombinationType.UAandOAFlights || flightCombinationType == FlightCombinationType.OAFlights)
                {
                    if (reservation.ShopReservationInfo2.WheelChairSizerInfo == null)
                    {
                        reservation.ShopReservationInfo2.WheelChairSizerInfo = new WheelChairSizerInfo();
                    }
                    reservation.ShopReservationInfo2.WheelChairSizerInfo.WheelChairErrorMessages = new United.Mobile.Model.Common.MOBAlertMessages();
                    reservation.ShopReservationInfo2.WheelChairSizerInfo.WheelChairErrorMessages.AlertMessages = new List<MOBSection>();
                    reservation.ShopReservationInfo2.WheelChairSizerInfo.WheelChairErrorMessages.AlertMessages.Add(new MOBSection()
                    {
                        Text1 = _configuration.GetValue<string>("WheelChairSizeOAMsg")
                    });
                    reservation.ShopReservationInfo2.WheelChairSizerInfo.WheelChairErrorMessages.MessageType = MOBFSRAlertMessageType.Caution.ToString();
                    if (flightCombinationType == FlightCombinationType.OAFlights)
                    {
                        reservation.ShopReservationInfo2.WheelChairSizerInfo.IsWheelChairSizerEligible = false;//If all the flights are OA,disable the WheelChair sizing tool
                    }
                }
            }
        }



        #region Booking Advance Search Offer Code changes
        public MOBOffer GetAFSOfferDetails(DisplayCart displayCart)
        {
            if (!isAFSCouponApplied(displayCart))
            {

                if (displayCart != null && displayCart.SpecialPricingInfo != null && !string.IsNullOrEmpty(displayCart.SpecialPricingInfo.PromoCode))
                {
                    MOBOffer offer = new MOBOffer();
                    offer.OfferCode = displayCart.SpecialPricingInfo.PromoCode;
                    offer.IsPassPlussOffer = IsPassPlussOffer(displayCart);
                    offer.OfferType = GetOfferType(displayCart.SpecialPricingInfo);
                    return offer;
                }
            }
            return null;
        }
        public OfferType GetOfferType(SpecialPricingInfo specialPricingInfo)
        {
            if (!specialPricingInfo.NonECDPromo)
            {
                return OfferType.ECD;
            }
            else if ((!String.IsNullOrEmpty(specialPricingInfo.AccountCodeNonECD) && specialPricingInfo.AccountCodeNonECD == "VATD")
                     || (!String.IsNullOrEmpty(specialPricingInfo.PromoCode) && specialPricingInfo.PromoCode.ToUpper() == "VETERANS"))
            {
                return OfferType.VETERAN;
            }
            else if (specialPricingInfo.PassPlusTypeValue == PassPlusType.Flex)
            {
                return OfferType.UNITEDPASSPLUSFLEX;
            }
            else if (specialPricingInfo.PassPlusTypeValue == PassPlusType.Secure)
            {
                return OfferType.UNITEDPASSPLUSSECURE;
            }
            else if (specialPricingInfo.UnitedMeetings != null && !specialPricingInfo.UnitedMeetings.Any(unitedMeeting => !string.IsNullOrEmpty(unitedMeeting.MeetingCode)))
            {
                return OfferType.UNITEDMEETINGS;
            }
            return OfferType.NONE;
        }
        public bool IsPassPlussOffer(DisplayCart displayCart)
        {
            return (displayCart.SpecialPricingInfo.PassPlusTypeValue == PassPlusType.Flex || displayCart.SpecialPricingInfo.PassPlusTypeValue == PassPlusType.Secure);
        }
        public async Task<bool> IsEnableAdvanceSearchOfferCode(int applicationId, string appVersion, List<MOBItem> catalogItems = null)
        {
            return (await _featureSettings.GetFeatureSettingValue("EnableAdvanceSearchOfferCode").ConfigureAwait(false) && (catalogItems != null && catalogItems.Count > 0 &&
            catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableOfferCodeExpansionChanges).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableOfferCodeExpansionChanges).ToString())?.CurrentValue == "1")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Andriod_EnableAdvanceSearchOfferCode_AppVersion"), _configuration.GetValue<string>("Iphone_EnableAdvanceSearchOfferCode_AppVersion")));
        }
        public bool IsFOPRequired(MOBShoppingCart shoppingCart, MOBSHOPReservation reservation)
        {
            if (shoppingCart.Offers != null && !string.IsNullOrEmpty(shoppingCart.Offers.OfferCode))
            {
                var grandTotalPrice = reservation?.Prices?.FirstOrDefault(p => !string.IsNullOrEmpty(p.DisplayType) && p.DisplayType.ToUpper().Equals("GRAND TOTAL"));
                if (grandTotalPrice != null && grandTotalPrice.Value == 0)
                {
                    return shoppingCart.Products.Any(prod => !string.IsNullOrEmpty(prod.ProdTotalPrice) && Convert.ToDecimal(prod.ProdTotalPrice) > 0);//return true only when none of the product has price(Ex:grandtotal can be zero when entire amount is covered with TravelCredit but we cannot assume no fop is required)
                }
            }
            return true;
        }
        public bool IsEnableAgreeandPurchaseButton(MOBShoppingCart shoppingCart, MOBSHOPReservation reservation)
        {
            if (shoppingCart.Offers != null
                && !string.IsNullOrEmpty(shoppingCart.Offers.OfferCode)
                && shoppingCart.FormofPaymentDetails?.IsFOPRequired == false
                && shoppingCart.FormofPaymentDetails?.BillingAddress != null)
            {
                return true;
            }
            return false;
        }
        public async Task<string> AssignMaskedPaymentMethod(MOBShoppingCart shoppingCart, MOBApplication application)
        {
            if (shoppingCart.Offers != null
                && !string.IsNullOrEmpty(shoppingCart.Offers.OfferCode)
                && shoppingCart.FormofPaymentDetails?.IsFOPRequired == false
                && shoppingCart.FormofPaymentDetails?.BillingAddress != null)
            {
                if (await IsEnableAdvanceSearchOfferCodeFastFollower(application.Id, application.Version.Major).ConfigureAwait(false))
                {
                    return _configuration.GetValue<string>("ZeroDollarPaymentmethodText");
                }
                else
                {
                    return $"****{shoppingCart.Offers.OfferCode.Substring(shoppingCart.Offers.OfferCode.Length - 4)}";
                }
            }
            return string.Empty;
        }
        public async Task<bool> IsMoneyPlusmilesEligible(ShopResponse shopResponse, MOBApplication application, List<MOBItem> catalogItems)
        {
            if (IsEnableMoneyPlusMilesFeature(application.Id, application.Version.Major, catalogItems) ||
                await IsEnableAdvanceSearchOfferCode(application.Id, application.Version.Major, catalogItems).ConfigureAwait(false))
            {
                if (await IsEnableAdvanceSearchOfferCode(application.Id, application.Version.Major, catalogItems).ConfigureAwait(false) &&
                    shopResponse != null && shopResponse.SpecialPricingInfo != null && !string.IsNullOrEmpty(shopResponse.SpecialPricingInfo.PromoCode)
                    && (shopResponse.SpecialPricingInfo.MerchOfferCoupon == null || shopResponse.SpecialPricingInfo.MerchOfferCoupon.IsNullOrEmpty()))
                {
                    return IsVeteransOffer(shopResponse) && shopResponse.IsMoneyAndMilesEligible;
                }
                return shopResponse.IsMoneyAndMilesEligible;
            }
            return false;
        }
        public bool IsVeteransOffer(ShopResponse shopResponse)
        {
            if ((!String.IsNullOrEmpty(shopResponse.SpecialPricingInfo.AccountCodeNonECD) && shopResponse.SpecialPricingInfo.AccountCodeNonECD == "VATD")
               || (!String.IsNullOrEmpty(shopResponse.SpecialPricingInfo.PromoCode) && shopResponse.SpecialPricingInfo.PromoCode.ToUpper() == "VETERANS"))
            {
                return true;
            }
            return false;
        }
        public Tuple<bool, string> IsNoDiscountedFaresAvailable(ShopResponse shopResponse)
        {
            if (shopResponse != null && shopResponse.SpecialPricingInfo != null && !string.IsNullOrEmpty(shopResponse.SpecialPricingInfo.PromoCode)
                    && (shopResponse.SpecialPricingInfo.MerchOfferCoupon == null || shopResponse.SpecialPricingInfo.MerchOfferCoupon.IsNullOrEmpty())
                    && shopResponse.Warnings != null && shopResponse.Warnings.Any(warning => warning.MinorCode == "10127"))
            {
                var SDLContent = shopResponse.Warnings.First(warning => warning.MinorCode == "10127").SDLContent;
                return new Tuple<bool, string>(true, SDLContent?.Key);
            }
            return new Tuple<bool, string>(false, null);
        }
        public MOBFSRAlertMessage GetnoDiscoutedFareAlertMessage(United.Services.FlightShopping.Common.ShopResponse response, List<CMSContentMessage> lstMessages)
        {
            var noDiscountFareAlert = IsNoDiscountedFaresAvailable(response);
            if (noDiscountFareAlert.Item1)
            {

                var noDiscountFareAlertMessage = ShopStaticUtility.GetSDLMessageFromList(lstMessages, noDiscountFareAlert.Item2 ?? "SDLKey_FaresNotAppliedWithPromotions");
                if (noDiscountFareAlertMessage != null && noDiscountFareAlertMessage.Count > 0)
                {
                    MOBFSRAlertMessage fsrNoDiscountFareAlertMessage = new MOBFSRAlertMessage
                    {
                        BodyMessage = noDiscountFareAlertMessage.FirstOrDefault().ContentFull,
                        MessageType = 0,
                        AlertType = MOBFSRAlertMessageType.Information.ToString(),
                        IsAlertExpanded = true
                    };
                    return fsrNoDiscountFareAlertMessage;
                }
            }
            return null;
        }
        public async Task<bool> IsLoadTCOrTB(MOBShoppingCart shoppingCart)
        {
            if (await _featureSettings.GetFeatureSettingValue("EnableAdvanceSearchOfferCode").ConfigureAwait(false))
            {
                if (shoppingCart.Offers == null)
                    return true;
                if (shoppingCart.Offers != null
                    && !string.IsNullOrEmpty(shoppingCart.Offers.OfferCode)
                    && shoppingCart.Offers.IsPassPlussOffer)
                {
                    return false;
                }
            }

            return true;
        }
        public List<MOBErrorInfo> AddWarnings(United.Services.FlightShopping.Common.ShopResponse response)
        {
            if (response.Warnings != null && response.Warnings.Any())
            {
                List<MOBErrorInfo> warnings = new List<MOBErrorInfo>();
                foreach (var warning in response.Warnings)
                {
                    warnings.Add(new MOBErrorInfo
                    {
                        MajorCode = warning.MajorCode,
                        MinorCode = warning.MinorCode,
                        MajorDescription = warning.MajorDescription,
                        MinorDescription = warning.MinorDescription
                    });
                }
                return warnings;
            }
            return null;
        }
        public async Task<bool> IsNoDiscountFareAvailbleinShop1(string sessionId)
        {
            LatestShopAvailabilityResponse persistAvailability = new LatestShopAvailabilityResponse();
            persistAvailability = await _sessionHelperService.GetSession<LatestShopAvailabilityResponse>(sessionId, persistAvailability.ObjectName, new List<string> { sessionId, persistAvailability.ObjectName }).ConfigureAwait(false);
            if (persistAvailability?.AvailabilityList != null)
            {
                var shop1Warnings = persistAvailability.AvailabilityList["1"]?.Warnings;
                if (shop1Warnings != null)
                {
                    return shop1Warnings.Any(warning => warning.MinorCode == "10127");
                }
            }
            return false;
        }
        public async Task<bool> IsEnableAdvanceSearchOfferCodeFastFollower(int applicationId, string appVersion)
        {
            return (await _featureSettings.GetFeatureSettingValue("EnableOfferCodeFastFollowerChanges").ConfigureAwait(false)
                && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Andriod_EnableAdvanceSearchOfferCodefastfollower_AppVersion"), _configuration.GetValue<string>("Iphone_EnableAdvanceSearchOfferCodefastfollower_AppVersion")));
        }
        #endregion
        public async Task<bool> IsEnableWheelChairSizerChanges(int applicationId, string appVersion, List<MOBItem> catalogItems = null)
        {
            return (await _featureSettings.GetFeatureSettingValue("EnableWheelChairSizer").ConfigureAwait(false) && (catalogItems != null && catalogItems.Count > 0 &&
            catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableWheelChairSizer).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableWheelChairSizer).ToString())?.CurrentValue == "1")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableWheelChairSizer_AppVersion"), _configuration.GetValue<string>("iPhone_EnableWheelChairSizer_AppVersion")));
        }

        public string GetFormatedUrl(string url, string scheme, string relativePath, bool ensureSSL = false)
        {
            var finalURL = $"{scheme}://{url}/shoppingservice/{relativePath.TrimStart(new[] { '/' })}";
            if (ensureSSL)
            {
                return finalURL.Replace("http:", "https:");
            }
            else
            {
                return finalURL;
            }
        }

        // Returns true if all flights belongs to business, businessfirts or first cabins
        public bool AreAllFlightsBusinessOrFirstCabin(List<MOBSHOPTrip> trips)
        {
            bool businessOrFirstCabinSelected = false;
            if (trips != null)
            {
                foreach (var trip in trips)
                {
                    #region
                    if (trip.FlattenedFlights != null)
                    {
                        foreach (var flattenedFlight in trip.FlattenedFlights)
                        {
                            if (flattenedFlight.Flights != null && flattenedFlight.Flights.Count > 0)
                            {
                                foreach (var flight in flattenedFlight.Flights)
                                {
                                    if (!string.IsNullOrEmpty(flight.Cabin) &&
                                        (flight.Cabin.ToUpper().Trim().Equals("BUSINESS") ||
                                         flight.Cabin.ToUpper().Trim().Equals("BUSINESSFIRST") ||
                                         flight.Cabin.ToUpper().Trim().Equals("FIRST")
                                        ))
                                    {
                                        businessOrFirstCabinSelected = true;
                                    }
                                    else
                                    {
                                        businessOrFirstCabinSelected = false;
                                        break;
                                    }
                                }
                            }
                            if (!businessOrFirstCabinSelected) { break; }
                        }
                    }
                    if (!businessOrFirstCabinSelected) { break; }
                    #endregion
                }
            }
            return businessOrFirstCabinSelected;
        }

        public async Task<bool> IsEnableExtraSeatReasonFixInOmniCartFlow()
        {
            return await _featureSettings.GetFeatureSettingValue("ExtraSeatReasonFixInOmniCartFlow").ConfigureAwait(false);
        }

        public bool IsExtraSeatFeatureEnabled(int appId, string appVersion, List<MOBItem> catalogItems)
        {
            if (catalogItems != null && catalogItems.Count > 0 &&
                           catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableExtraSeatFeature).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableExtraSeatFeature).ToString())?.CurrentValue == "1")
                return _configuration.GetValue<bool>("EnableExtraSeatsFeature")
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "Android_EnableExtraSeatsFeature_AppVersion", "IPhone_EnableExtraSeatsFeature_AppVersion", "", "", true, _configuration);
            else return false;
        }

        public async Task<bool> IsExtraSeatFeatureEnabledForOmniCart(int appId, string appVersion, List<MOBItem> catalogItems)
        {
            return IsExtraSeatFeatureEnabled(appId, appVersion, catalogItems)
                   && await IsEnableHideEditProfileNavigationForExtraSeatInOmniCart()
                   && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("Android_HideEditProfileNavigationForExtraSeatInOmniCart_AppVersion"), _configuration.GetValue<string>("IPhone_HideEditProfileNavigationForExtraSeatInOmniCart_AppVersion"));
        }

        public async Task<bool> IsEnableHideEditProfileNavigationForExtraSeatInOmniCart()
        {
            return await _featureSettings.GetFeatureSettingValue("HideEditProfileNavigationForExtraSeatInOmniCart").ConfigureAwait(false);
        }

        public async Task<bool> IsEnablePassingTourCodeToFSInCorporateFlow()
        {
            return await _featureSettings.GetFeatureSettingValue("IsEnablePassingTourCodeToFSInCorporateFlow").ConfigureAwait(false);
        }

        public async Task<bool> EnableGUIDAndUCSIDToFlightShoppingInCorporateFlow()
        {
            return await _featureSettings.GetFeatureSettingValue("EnableU4BCorporateObjectUpdates").ConfigureAwait(false);
        }

        public bool IsIncludeMoneyMilesInRTI(List<FormofPaymentOption> eligibleFormsOfPayment)
        {
            return (eligibleFormsOfPayment != null && eligibleFormsOfPayment.Exists(x => x.Category == "MILES"));
        }

        // Check if Express checkout flow is enabled
        public async Task<bool> IsEnabledExpressCheckoutFlow(int applicationId, string appVersion, List<MOBItem> catalogItems = null)
        {
            return (await _featureSettings.GetFeatureSettingValue("EnableExpressCheckoutChanges").ConfigureAwait(false) && (catalogItems != null && catalogItems.Count > 0 &&
            catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableExpressCheckout).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableExpressCheckout).ToString())?.CurrentValue == "1")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableExpressCheckout_AppVersion"), _configuration.GetValue<string>("iPhone_EnableExpressCheckout_AppVersion")));
        }
        public async Task<bool> IsEnableGuatemalaTaxIdCollectionChanges(int applicationId, string appVersion, List<MOBItem> catalogItems = null)
        {
            return (await _featureSettings.GetFeatureSettingValue("EnableGuatemalaTaxIDCollectionChanges").ConfigureAwait(false) && (catalogItems != null && catalogItems.Count > 0 &&
            catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableGuatemalaTaxIdCollection).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableGuatemalaTaxIdCollection).ToString())?.CurrentValue == "1")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableGuatemalaTaxIDChanges_AppVersion"), _configuration.GetValue<string>("IPhone_EnableGuatemalaTaxIDChanges_AppVersion")));
        }
        public bool IsEnablePartnerProvision(List<MOBItem> catalogItems, string flow, int applicationId, string appVersion)
        {
            if (flow == FlowType.BOOKING.ToString() || flow == FlowType.POSTBOOKING.ToString())
            {
                return (catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnablePartnerProvision).ToString() || a.Id == ((int)AndroidCatalogEnum.EnablePartnerProvision).ToString())?.CurrentValue == "1"
                    && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_PartnerProvision_AppVersion"), _configuration.GetValue<string>("IPhone_PartnerProvision_AppVersion")));
            }
            else if (flow == FlowType.CHECKIN.ToString())
            {
                return (catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableProvisionInCHECKIN).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableProvisionInCHECKIN).ToString())?.CurrentValue == "1"
                    && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_PartnerProvision_AppVersion"), _configuration.GetValue<string>("IPhone_PartnerProvision_AppVersion")));
            }
            else
            {
                return (catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableProvisionInVIEWRES).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableProvisionInVIEWRES).ToString())?.CurrentValue == "1"
                    && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_PartnerProvision_AppVersion"), _configuration.GetValue<string>("IPhone_PartnerProvision_AppVersion")));
            }
        }
        public async System.Threading.Tasks.Task GetTaxIdContentFromSDL(MOBTaxIdInformation taxIdInfo, MOBRequest request, Session session)
        {
            List<CMSContentMessage> lstMessages = await _ffcShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
            var message = GetSDLMessageFromList(lstMessages, _configuration.GetValue<string>("TaxId_Collection_RTI_SDL_Key")).FirstOrDefault();
            if (message != null)
            {
                taxIdInfo.TaxIdContentMsgs = new MOBMobileCMSContentMessages();
                taxIdInfo.TaxIdContentMsgs.Title = message?.HeadLine;
                taxIdInfo.TaxIdContentMsgs.ContentShort = message?.ContentShort;
                taxIdInfo.TaxIdContentMsgs.ContentFull = message?.ContentFull;
            }
        }
        public bool IsGuatemalaOriginatingTrip(List<MOBSHOPTrip> trips)
        {
            bool isGuatemalaFlight = false;
            if (trips != null && trips.Count > 0
                && trips[0].FlattenedFlights != null && trips[0].FlattenedFlights.Count > 0
                && trips[0].FlattenedFlights[0].Flights != null && trips[0].FlattenedFlights[0].Flights.Count > 0
                && !string.IsNullOrEmpty(trips[0].FlattenedFlights[0].Flights[0].OriginCountryCode))
            {
                isGuatemalaFlight = trips[0].FlattenedFlights[0].Flights[0].OriginCountryCode.ToUpper().Equals("GT");
            }
            return isGuatemalaFlight;
        }
        public bool IsTaxIdCountryEnable(List<MOBSHOPTrip> trips, List<CMSContentMessage> sdllstMessages = null)
        {
            try
            {
                bool isTaxIdCountryEnable = false;
                //Origin
                if (trips != null && trips.Count > 0
                    && trips[0].FlattenedFlights != null && trips[0].FlattenedFlights.Count > 0
                    && trips[0].FlattenedFlights[0].Flights != null && trips[0].FlattenedFlights[0].Flights.Count > 0
                    && !string.IsNullOrEmpty(trips[0].FlattenedFlights[0].Flights[0].OriginCountryCode))
                {
                    string taxidCountries = null;
                    string taxidCountriesDestination = null;
                    if (sdllstMessages != null)
                    {
                        taxidCountries = GetSDLStringMessageFromList(sdllstMessages, "TaxId_Countries_Enabler"); // For origin countries
                        taxidCountriesDestination = GetSDLStringMessageFromList(sdllstMessages, "TaxId_Countries_Enabler_Destination"); // For destination countries
                    }
                    if (!string.IsNullOrEmpty(taxidCountries) && taxidCountries.Contains(trips[0].FlattenedFlights[0].Flights[0].OriginCountryCode.Trim().ToUpper()))
                    {
                        isTaxIdCountryEnable = true;
                    }
                    // Checking if destionation requires taxId collection
                    if (!isTaxIdCountryEnable)
                    {
                        string destination = GetDestinationCountry(trips);
                        if (!string.IsNullOrEmpty(destination) && !string.IsNullOrEmpty(taxidCountriesDestination) && taxidCountriesDestination.Contains(destination))
                        {
                            isTaxIdCountryEnable = true;
                        }
                    }
                }

                return isTaxIdCountryEnable;
            }
            catch
            {
                return false;
            }
        }

        public bool IsEnableMoneyPlusMilesFeature(int applicationId = 0, string appVersion = "", List<MOBItem> catalogItems = null)
        {
            return (_configuration.GetValue<bool>("EnableFSRMoneyPlusMilesFeature") && (catalogItems != null && catalogItems.Count > 0 &&
            catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableMoneyMilesBooking).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableMoneyMilesBooking).ToString())?.CurrentValue == "1")
                );
        }
        public bool IsEnableMoneyPlusMilesOnScreenAlert(int applicationId = 0, string appVersion = "", List<MOBItem> catalogItems = null)
        {
            return (catalogItems != null && catalogItems.Count > 0 &&
            catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableCancelbuttonOnscreenAlert).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableCancelbuttonOnscreenAlert).ToString())?.CurrentValue == "1"
                );
        }
        public bool IsEnableFSRETCTravelCreditsOnScreenAlert(int applicationId = 0, string appVersion = "", List<MOBItem> catalogItems = null)
        {
            return catalogItems != null && catalogItems.Count > 0 &&
           catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableETCCreditsInBooking).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableETCCreditsInBooking).ToString())?.CurrentValue == "1";
        }

        public bool IsFSROAFlashSaleEnabled(List<MOBItem> catalogItems = null)
        {
            return (_configuration.GetValue<bool>("EnableOAFlashSellFeature") == false && (catalogItems != null && catalogItems.Count > 0 &&
            catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableFSROAFlashFeature).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableFSROAFlashFeature).ToString())?.CurrentValue == "1")
                );
        }

        public bool IsFSROAFlashSaleEnabledInReShop(List<MOBItem> catalogItems = null)
        {
            return (_configuration.GetValue<bool>("EnableOAFlashSellFeature") == false && (catalogItems != null && catalogItems.Count > 0 &&
            catalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableFSROAFlashFeatureInReShop).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableFSROAFlashFeatureInReShop).ToString())?.CurrentValue == "1")
                );
        }

        public async System.Threading.Tasks.Task BuildTaxIdInformation(MOBSHOPReservation reservation, MOBRequest request, Session session)
        {
            if (reservation.ShopReservationInfo2 == null)
                reservation.ShopReservationInfo2 = new ReservationInfo2();
            if (reservation.ShopReservationInfo2.TaxIdInformation == null)
            {
                reservation.ShopReservationInfo2.TaxIdInformation = new MOBTaxIdInformation();
                await GetTaxIdContentFromSDL(reservation.ShopReservationInfo2.TaxIdInformation, request, session).ConfigureAwait(false);
                List<string> guatemalaTaxIdTypes = _configuration.GetValue<string>("GuatemalaTaxIdTypes").Split('|').ToList();
                if (guatemalaTaxIdTypes != null & guatemalaTaxIdTypes.Count > 0)
                {
                    reservation.ShopReservationInfo2.TaxIdInformation.SupportedTaxIdTypes = new List<MOBItem>
                    {
                    new MOBItem () {Id = TaxIdType.ID.ToString(), CurrentValue = guatemalaTaxIdTypes[0]},
                    new MOBItem () {Id = TaxIdType.NI.ToString(), CurrentValue = guatemalaTaxIdTypes[1]},
                    new MOBItem () {Id = TaxIdType.PP.ToString(), CurrentValue = guatemalaTaxIdTypes[2]},
                    };
                }
            }
        }
        public async Task<string> GetSeatMapErrorMessages(string key, string sessionId, int appId, string version)
        {
            if (await _featureSettings.GetFeatureSettingValue("EnableRetirieveSeatMapMessageFromSDL"))
            {


                List<CMSContentMessage> lstMessages = await _ffcShoppingcs.GetSDLContentByGroupName(new MOBRequest { TransactionId = sessionId }, sessionId, "", _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                var message = ShopStaticUtility.GetSDLMessageFromList(lstMessages, key);
                if (message != null && message.Count > 0)
                {
                    if (appId == 1 && !GeneralHelper.IsApplicationVersionGreater(appId, version, "Android_EnableSeatmapAlertHtmlFix_AppVersion", "IPhone_EnableSeatmapAlertHtmlFix_AppVersion", "", "", true, _configuration))
                    {
                        return message.FirstOrDefault().ContentFull.Replace("strong>", "b>");
                    }
                    return message.FirstOrDefault().ContentFull;
                }
                return _configuration.GetValue<string>(key);
            }
            return _configuration.GetValue<string>(key);
        }
        public async System.Threading.Tasks.Task BuildTaxIdInformationForLatidCountries(MOBSHOPReservation reservation, MOBRequest request, Session session, List<CMSContentMessage> sdllstMessages = null)
        {
            string originatingCountry = reservation?.Trips[0]?.FlattenedFlights[0]?.Flights[0]?.OriginCountryCode.ToUpper().Trim();            
            string destinationCountry = GetDestinationCountry(reservation?.Trips);

            if (reservation.ShopReservationInfo2 == null)
                reservation.ShopReservationInfo2 = new ReservationInfo2();
            if (reservation.ShopReservationInfo2.TaxIdInformation == null && !string.IsNullOrEmpty(originatingCountry) && !string.IsNullOrEmpty(destinationCountry))
            {
                reservation.ShopReservationInfo2.TaxIdInformation = new MOBTaxIdInformation();
                reservation.ShopReservationInfo2.TaxIdInformation.IsTravelerOrPurchaserCountry = GetClassificationBasedOnCountry(originatingCountry, destinationCountry);
                reservation.ShopReservationInfo2.TaxIdInformation.IncludeInfantOnLap = IncludeInfantOnLap(originatingCountry);
                if (sdllstMessages == null)
                {
                    sdllstMessages = await _ffcShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID").ConfigureAwait(false);
                }
                var travelerTaxidCountriesDestination = GetSDLStringMessageFromList(sdllstMessages, "TaxId_Countries_Enabler_Destination");
                GetTaxIdContentForLatidCountries(sdllstMessages, reservation.ShopReservationInfo2.TaxIdInformation, request, session);
                List<string> taxIdCountriesInfo = GetSDLStringMessageFromList(sdllstMessages, "TaxId_Countries_Info").Split('|').ToList();
                
                if (taxIdCountriesInfo != null && taxIdCountriesInfo.Count > 0 && taxIdCountriesInfo.Any())
                {
                    var taxIdOriginCountryInfo = taxIdCountriesInfo.Where(x => x.ToUpper().Trim().StartsWith(originatingCountry.ToUpper().Trim())).FirstOrDefault();
                    if (taxIdOriginCountryInfo != null)
                    {
                        reservation.ShopReservationInfo2.TaxIdInformation.Components = BuildComponentsForTaxIdCollection(taxIdOriginCountryInfo);

                    }
                    else if(!string.IsNullOrEmpty(travelerTaxidCountriesDestination) && travelerTaxidCountriesDestination.Contains(destinationCountry))
                    {
                        var taxIdDestinationCountryInfo = taxIdCountriesInfo.Where(x => x.ToUpper().Trim().StartsWith(destinationCountry.ToUpper().Trim())).FirstOrDefault();
                        if (taxIdDestinationCountryInfo != null) 
                        {
                            reservation.ShopReservationInfo2.TaxIdInformation.Components = BuildComponentsForTaxIdCollection(taxIdDestinationCountryInfo);
                        }
                    }                    
                }
                if (reservation.ShopReservationInfo2.TaxIdInformation.Components == null)
                    reservation.ShopReservationInfo2.TaxIdInformation = null;
            }
        }
        public string GetTaxIdType(string taxIDTypeName)
        {
            string taxIDType = taxIDTypeName.Trim().ToUpper();

            switch (taxIDType)
            {
                case "TAX ID":
                case "CARNE DE EXTRANJERIA (FOREIGN RESIDENCY ID)":
                    return TaxIdType.ID.ToString();
                case "NATIONAL ID":
                    return TaxIdType.NI.ToString();
                case "PASSPORT":
                    return TaxIdType.PP.ToString();
                default:
                    return string.Empty;
            }
        }
        public ComponentType GetComponentTypeForTaxId(string taxIDComponentName)
        {
            switch (taxIDComponentName)
            {
                case "id number":
                case "tax id number":
                    return ComponentType.ALPHANUMERIC;
                case "email address":
                    return ComponentType.EMAIL;
                case "tax id holder":
                    return ComponentType.TEXTONLY;
                case "dob":
                    return ComponentType.DATE;
                default:
                    return ComponentType.NONE;
            }
        }

        public string GetInlineErrorMsgForTaxIdComponent(string taxIDComponentName)
        {
            List<string> taxidInlineErrorMsgs = _configuration.GetValue<string>("TaxidInlineErrorMsgs").Split('|').ToList();
            switch (taxIDComponentName)
            {
                case "id type":
                    return taxidInlineErrorMsgs[0];
                case "id number":
                    return taxidInlineErrorMsgs[1];
                case "tax id number":
                    return taxidInlineErrorMsgs[2];
                case "email address":
                    return taxidInlineErrorMsgs[3];
                case "tax id holder":
                    return taxidInlineErrorMsgs[4];
                default:
                    return null;
            }
        }
        private string GetClassificationBasedOnCountry(string originatingCountryCode, string destinationCountryCode, List<CMSContentMessage> sdllstMessages = null)
        {
            var purchaserTaxidCountries = _configuration.GetValue<string>("TaxidPurchaserCountries");
            var travelerTaxidCountries = _configuration.GetValue<string>("TaxidTravelerCountries");
            var travelerTaxidCountriesDestination = string.Empty;
            if (sdllstMessages != null) 
            {
                travelerTaxidCountriesDestination = GetSDLStringMessageFromList(sdllstMessages, "TaxId_Countries_Enabler_Destination");
            }

            return (!string.IsNullOrEmpty(purchaserTaxidCountries) && purchaserTaxidCountries.Contains(originatingCountryCode)) ? TaxIdCountryType.PURCHASER.ToString() :
                ((!string.IsNullOrEmpty(travelerTaxidCountries) && travelerTaxidCountries.Contains(originatingCountryCode)) ||
                (!string.IsNullOrEmpty(travelerTaxidCountriesDestination) && travelerTaxidCountriesDestination.Contains(destinationCountryCode))) ? TaxIdCountryType.TRAVELER.ToString() : null;
            
        }

        private bool IncludeInfantOnLap(string originatingCountryCode) 
        {
            var infantOnLapTaxIdCollection = _configuration.GetValue<string>("TaxidInfantOnLapCollection");

            return (!string.IsNullOrEmpty(infantOnLapTaxIdCollection) && infantOnLapTaxIdCollection.Contains(originatingCountryCode));
        }

        private string GetDestinationCountry(List<MOBSHOPTrip> trips) 
        {
            if (trips != null && trips.Count > 0) 
            {
                MOBSHOPTrip lastTrip = trips.Last();

                if (lastTrip != null && lastTrip.FlattenedFlights.Count > 0) 
                {
                    var lastFlattenedFlights = lastTrip.FlattenedFlights.Last();

                    if (lastFlattenedFlights != null && lastFlattenedFlights.Flights.Count > 0) 
                    {
                        var lastFlight = lastFlattenedFlights.Flights.Last();

                        if (lastFlight != null && !string.IsNullOrEmpty(lastFlight.DestinationCountryCode)) 
                        {
                            return lastFlight.DestinationCountryCode;
                        }
                    }
                }
            }
            return null;
        }

        private List<MOBComponent> BuildComponentsForTaxIdCollection(string taxIdCountryInfo)
        {
            List<MOBComponent> components = null;
            var taxidCountryData = taxIdCountryInfo.Split('-');
            #region
            if (taxidCountryData != null)
            {
                List<string> fields = taxidCountryData[2].ToString().Trim().Split('~').ToList(); //Fields to be displayed based on country
                if (fields != null && fields.Count > 0)
                {
                    components = new List<MOBComponent>();
                    foreach (string field in fields)
                    {
                        if (!string.IsNullOrEmpty(field))
                        {
                            MOBComponent taxIdField = new MOBComponent();
                            List<string> fieldData = field.Split(',').ToList();//Title and Placeholder text for each field
                            taxIdField.Title = fieldData[0];
                            taxIdField.PlaceholderText = fieldData[1];
                            taxIdField.ComponentType = GetComponentTypeForTaxId(fieldData[0].ToLower().Trim());
                            taxIdField.InlineErrorText = GetInlineErrorMsgForTaxIdComponent(fieldData[0].ToLower().Trim());
                            taxIdField.IsOptional = false;
                            //taxIdField.CharRestrictionLimit = Convert.ToInt32(fieldData[2]);

                            //bool isAFIPStatusField = field.ToUpper().Trim().Equals("AFIPSTATUS");
                            if (fieldData[0].ToUpper().Trim() == "ID TYPE") //Assigning SupportedTypes(IdTypes) if the field is IdType.
                            {
                                List<string> taxIDOrAFIPTypesBasedOnCountry = taxidCountryData[1].ToString().Trim().Split('!').ToList();//Id Types supported by the country
                                if (taxIDOrAFIPTypesBasedOnCountry != null && taxIDOrAFIPTypesBasedOnCountry.Count > 0)
                                {
                                    taxIdField.SupportedTypes = new List<MOBItem>();
                                    foreach (var taxIDOrAFIPTypeBasedOnCountry in taxIDOrAFIPTypesBasedOnCountry)
                                    {
                                        MOBItem supportedType = new MOBItem()
                                        {
                                            Id = GetTaxIdType(taxIDOrAFIPTypeBasedOnCountry), //Acronym based on IdType 
                                            CurrentValue = taxIDOrAFIPTypeBasedOnCountry
                                        };
                                        taxIdField.SupportedTypes.Add(supportedType);
                                    }
                                }
                            }
                            components.Add(taxIdField);
                        }
                    }
                }
            }
            #endregion
            return components;
        }

        public void GetTaxIdContentForLatidCountries(List<CMSContentMessage> lstMessages, MOBTaxIdInformation taxIdInfo, MOBRequest request, Session session)
        {
            var message = GetSDLMessageFromList(lstMessages, "RTI_TaxID_Collection").FirstOrDefault();
            if (message != null)
            {
                taxIdInfo.TaxIdContentMsgs = new MOBMobileCMSContentMessages();
                taxIdInfo.TaxIdContentMsgs.Title = message?.HeadLine;
                taxIdInfo.TaxIdContentMsgs.ContentShort = message?.ContentShort;
                taxIdInfo.TaxIdContentMsgs.ContentFull = message?.ContentFull;
            }
        }
        public void GetCartRefId(long cartRefid, ReservationInfo2 shopReservationInfo2, List<CMSContentMessage> lstMessages)
        {
            if (lstMessages != null && lstMessages.Any())
            {
                if (shopReservationInfo2 == null)
                {
                    shopReservationInfo2 = new ReservationInfo2();
                }
                shopReservationInfo2.CartRefId = cartRefid.ToString();
                var sdlMessage = GetSDLMessageFromList(lstMessages, "CUSTOMER_FACING_CARTID_TOOLTIP").FirstOrDefault();
                if (sdlMessage != null)
                {
                    sdlMessage.HeadLine = string.Format(sdlMessage.HeadLine, cartRefid);
                    sdlMessage.ContentFull = string.Format(sdlMessage.ContentFull, cartRefid);
                    shopReservationInfo2.CartRefIdContentMsg = new MOBMobileCMSContentMessages();
                    shopReservationInfo2.CartRefIdContentMsg = sdlMessage;
                }
            }
        }
        public void BuildWheelChairFilterContent(WheelChairSizerInfo WheelChairfilterContent, List<CMSContentMessage> lstMessages)
        {
            if (lstMessages != null)
            {
                var message = GetSDLMessageFromList(lstMessages, "Wheel chair Sizing").FirstOrDefault();
                if (message != null)
                {
                    WheelChairfilterContent.WcBodyMsg = message?.ContentFull;
                }
                var wheelchairContentMsg = GetSDLMessageFromList(lstMessages, "WheelchairFilter_FSR_ContentMsgs").FirstOrDefault();
                if (wheelchairContentMsg != null)
                {
                    WheelChairfilterContent.Disclaimer = wheelchairContentMsg?.ContentShort;
                    WheelChairfilterContent.RedirectUrl = wheelchairContentMsg?.ContentFull;
                }
            }
            //Building batteryTypes to display on UI
            List<string> batterTypesinfo = _configuration.GetValue<string>("SSRWheelchairBatteryTypeCodeDesc").Split('|').ToList();
            if (batterTypesinfo != null && batterTypesinfo.Count > 0 && batterTypesinfo.Any())
            {
                WheelChairfilterContent.BatteryTypes = new List<MOBItem>();
                foreach (string batteryTypeInfo in batterTypesinfo)
                {
                    var batteryTypeCodeDesc = batteryTypeInfo.Split('^');
                    MOBItem batteryInfo = new MOBItem()
                    {
                        Id = batteryTypeCodeDesc[0],
                        CurrentValue = batteryTypeCodeDesc[1]
                    };
                    WheelChairfilterContent.BatteryTypes.Add(batteryInfo);
                }
            }
        }
        public void PrepopulateDimensionInfo(MOBSearchFilters searchFilters, ReservationInfo2 shopReservationInfo2, string sessionId)
        {
            if (searchFilters != null && searchFilters.WheelchairFilterContent != null
                && searchFilters.WheelchairFilterContent.DimensionInfo != null
                && !string.IsNullOrEmpty(searchFilters.WheelchairFilterContent.SelectedBatteryType))
            {
                shopReservationInfo2.WheelChairSizerInfo.DimensionInfo = new MOBDimensions();
                shopReservationInfo2.WheelChairSizerInfo.DimensionInfo = searchFilters.WheelchairFilterContent.DimensionInfo;
                shopReservationInfo2.WheelChairSizerInfo.SelectedBatteryType = searchFilters.WheelchairFilterContent.SelectedBatteryType;
            }
        }
        public async Task RemoveSavedDimensionInfo(MOBSearchFilters searchFilters, string sessionId)
        {
            if (searchFilters != null && searchFilters.WheelchairFilterContent != null
                && searchFilters.WheelchairFilterContent.DimensionInfo != null
                && !string.IsNullOrEmpty(searchFilters.WheelchairFilterContent.SelectedBatteryType))
            {
                searchFilters.WheelchairFilter[0].IsSelected = false;
                searchFilters.WheelchairFilterContent.DimensionInfo = null;
                searchFilters.WheelchairFilterContent.SelectedBatteryType = null;
                await _sessionHelperService.SaveSession<MOBSearchFilters>(searchFilters, sessionId, new List<string> { sessionId, searchFilters.GetType().FullName }, searchFilters.GetType().FullName).ConfigureAwait(false);
            }
        }
        public void AssignWheelChairSpecialNeedForSinglePax(List<MOBCPTraveler> travelers, Reservation bookingPathReservation)
        {
            if (travelers.Count == 1 && bookingPathReservation != null
                && isWheelChairFilterAppliedAtFSR(bookingPathReservation?.ShopReservationInfo2)
                && (travelers[0].SelectedSpecialNeeds == null || travelers[0].SelectedSpecialNeeds != null
                && !travelers[0].SelectedSpecialNeeds.Any(sn => sn.Code == _configuration.GetValue<string>("SSRWheelChairDescription"))))
            {
                if (travelers[0].SelectedSpecialNeeds == null || travelers[0].SelectedSpecialNeeds.Count == 0)
                    travelers[0].SelectedSpecialNeeds = new List<United.Mobile.Model.Common.SSR.TravelSpecialNeed>();

                string selectedWcBatteryTypeCode = bookingPathReservation?.ShopReservationInfo2?.WheelChairSizerInfo?.SelectedBatteryType;
                var wheelChairSpecialNeed = bookingPathReservation?.ShopReservationInfo2?.SpecialNeeds?.SpecialRequests?.FirstOrDefault(sn => sn.Code == _configuration.GetValue<string>("SSRWheelChairDescription")).Clone();
                if (wheelChairSpecialNeed != null && wheelChairSpecialNeed.SubOptions != null)
                {
                    wheelChairSpecialNeed.SubOptions = wheelChairSpecialNeed?.SubOptions?.Where(s => s.Code == selectedWcBatteryTypeCode).ToList();
                    if (wheelChairSpecialNeed.SubOptions != null && wheelChairSpecialNeed.SubOptions.Count > 0)
                    {
                        wheelChairSpecialNeed.SubOptions[0].WheelChairDimensionInfo = new MOBDimensions();
                        wheelChairSpecialNeed.SubOptions[0].WheelChairDimensionInfo = bookingPathReservation?.ShopReservationInfo2.WheelChairSizerInfo?.DimensionInfo;
                        wheelChairSpecialNeed.SubOptions[0].WheelChairDimensionInfo.WcFitConfirmationMsg = _configuration.GetValue<string>("WheelChairSizeSuccessMsg");
                        travelers[0].SelectedSpecialNeeds.Add(wheelChairSpecialNeed);

                        UpdateAllEligibleTravelersCslWithWheelChairSpecialNeed(bookingPathReservation.ShopReservationInfo2.AllEligibleTravelersCSL, travelers[0]);
                    }
                }
            }
        }

        public bool isWheelChairFilterAppliedAtFSR(ReservationInfo2 shopReservationInfo2)
        {
            return shopReservationInfo2 != null &&
                    shopReservationInfo2?.WheelChairSizerInfo != null &&
                    shopReservationInfo2?.WheelChairSizerInfo?.DimensionInfo != null &&
                    !string.IsNullOrEmpty(shopReservationInfo2?.WheelChairSizerInfo?.SelectedBatteryType);
        }
        public void UpdateAllEligibleTravelersCslWithWheelChairSpecialNeed(List<MOBCPTraveler> allEligibleTravelersCSL, MOBCPTraveler traveler)
        {
            if (allEligibleTravelersCSL != null)
            {
                var selectedSavedTraveler = allEligibleTravelersCSL.FirstOrDefault(savedTraveler => IsTravelerMatched(traveler, savedTraveler, true));
                if (selectedSavedTraveler != null)
                {
                    selectedSavedTraveler.SelectedSpecialNeeds = new List<TravelSpecialNeed>();
                    selectedSavedTraveler.SelectedSpecialNeeds = traveler.SelectedSpecialNeeds;
                }
            }
        }
        public bool IsTravelerMatched(MOBCPTraveler traveler, MOBCPTraveler savedTraveler, bool isEnableExtraSeatReasonFixInOmniCartFlow)
        {
            if (isEnableExtraSeatReasonFixInOmniCartFlow && traveler != null && savedTraveler != null && traveler.IsExtraSeat)
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
        public void AddWheelChairAlertMsgWhenNoneFits(MOBSHOPAvailability availability, List<CMSContentMessage> lstMessages)
        {
            if (availability.OnScreenAlerts == null)
            {
                availability.OnScreenAlerts = new List<MOBOnScreenAlert>();
            }
            MOBOnScreenAlert mobOnScreenAlert = new MOBOnScreenAlert();
            if (lstMessages != null)
            {
                var message = GetSDLMessageFromList(lstMessages, "WheelchairFilter_FSR_Alert").FirstOrDefault();
                if (message != null)
                {
                    mobOnScreenAlert.Title = message.HeadLine;
                    mobOnScreenAlert.Message = message.ContentFull;
                }
            }
            mobOnScreenAlert.AlertType = MOBOnScreenAlertType.WHEELCHAIRFITS;
            mobOnScreenAlert.Actions = new List<MOBOnScreenActions>
            {
                new MOBOnScreenActions
                {
                    ActionTitle = "Continue",
                    ActionType = MOBOnScreenAlertActionType.DISMISS_ALERT
                }
            };
            availability.OnScreenAlerts.Add(mobOnScreenAlert);
        }
        public async Task ValidateWheelChairFilterSize(MOBDimensions dimensions, string sessionId, MOBSHOPAvailability availability, List<CMSContentMessage> lstMessages)
        {
            if (dimensions != null && dimensions.Width > 0 && dimensions.Height > 0 && !string.IsNullOrEmpty(dimensions.Units))
            {
                if (dimensions.Units.ToUpper().Trim() != "INCHES")
                {
                    dimensions.Width = ConvertToInches(dimensions.Width);
                    dimensions.Height = ConvertToInches(dimensions.Height);
                }
                var equipmentListInDB = await GetFlightDimensionsList("TransWC01").ConfigureAwait(false);
                var bookingPathReservation = new Reservation();
                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionId, bookingPathReservation.ObjectName, new List<string> { sessionId, bookingPathReservation.ObjectName });
                if (bookingPathReservation == null)
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("BookingSessionExpiryMessage"));
                }
                #region Getting UA/UAX flight segments from reservation
                var unitedCarriers = _configuration.GetValue<string>("UnitedCarriers");
                var UAFlightSegments = bookingPathReservation?.Trips?.SelectMany(a => a.FlattenedFlights)?.SelectMany(b => b.Flights)
                    .Where(f => unitedCarriers.Contains(f.OperatingCarrier?.ToUpper().Trim())).ToList();
                List<string> UAflightEquipmentList = new List<string>();
                if (UAFlightSegments != null && UAFlightSegments.Any() && UAFlightSegments.Count > 0)
                {
                    UAflightEquipmentList = UAFlightSegments?.Select(c => c.EquipmentDisclosures)?.Select(d => d.EquipmentDescription)?.ToList();
                }
                #endregion
                List<MOBDimensions> masterUAEquipList = new List<MOBDimensions>();
                #region Logic for handling UA/UAX and UA+OA flights scenario
                if (UAflightEquipmentList != null && UAflightEquipmentList.Any()
                                    && UAflightEquipmentList.Count > 0 && equipmentListInDB != null
                                    && equipmentListInDB.Count > 0)//indicates some/all the flights are UA/UAX
                {
                    foreach (var flightDesc in UAflightEquipmentList)
                    {
                        var equip = equipmentListInDB?.FirstOrDefault(e => e.FlightEquipmentDescription?.ToUpper() == flightDesc?.ToUpper());
                        if (equip != null && equip.Height > 0 && equip.Width > 0)
                        {
                            masterUAEquipList?.Add(equip);//adding only UA/UAX flights that has dimension data available
                        }
                    }
                    if (masterUAEquipList != null && masterUAEquipList?.Count > 0 && UAflightEquipmentList.Count == masterUAEquipList.Count)//indicates all the UA/UAX flights in reservation has dimension data available in db
                    {
                        var maxWidthAllowed = masterUAEquipList?.Min(e => e.Width);
                        var maxHeightAllowed = masterUAEquipList?.Min(e => e.Height);
                        if (dimensions.Width > maxWidthAllowed || dimensions.Height > maxHeightAllowed)//Customer's WheelChair size exceeded cargo dimensions
                        {
                            availability.OnScreenAlerts = ValidateWheelChairSizeAlertMsg(availability.OnScreenAlerts, lstMessages);
                        }
                        #region WheelChair fits successfully in all UA/UAX flights
                        else
                        {
                            return;
                        }
                        #endregion
                    }
                    #region Dimension data unavailable in db for one of the UA/UAX flights
                    else
                    {
                        availability.OnScreenAlerts = ValidateWheelChairSizeAlertMsg(availability.OnScreenAlerts, lstMessages);
                    }
                    #endregion
                }
                #endregion
                #region when all flights are OA
                else
                {
                    availability.OnScreenAlerts = ValidateWheelChairSizeAlertMsg(availability.OnScreenAlerts, lstMessages);
                }
                #endregion
            }
        }
        public List<MOBOnScreenAlert> ValidateWheelChairSizeAlertMsg(List<MOBOnScreenAlert> onScreenAlerts, List<CMSContentMessage> lstMessages = null)
        {
            if (onScreenAlerts == null)
            {
                onScreenAlerts = new List<MOBOnScreenAlert>();
            }
            var message = GetSDLMessageFromList(lstMessages, "WheelchairFilter_FSR_FinalValidation_Alert").FirstOrDefault();
            var actions = message?.ContentShort?.Split('|');
            if (actions == null)
                actions = _configuration.GetValue<string>("WheelChairOnFSRValidationAlertActionTitles").Split('|');
            var callactions = actions[2]?.Split('~');

            MOBOnScreenAlert mobOnScreenAlert = new MOBOnScreenAlert();
            mobOnScreenAlert.Title = message?.HeadLine ?? _configuration.GetValue<string>("WheelChairOnFSRValidationAlertTitle");
            mobOnScreenAlert.Message = message?.ContentFull ?? _configuration.GetValue<string>("WheelChairOnFSRValidationAlertBody");
            mobOnScreenAlert.AlertType = MOBOnScreenAlertType.WHEELCHAIRFITS;
            mobOnScreenAlert.Actions = new List<MOBOnScreenActions>
            {
                new MOBOnScreenActions
                {
                    ActionTitle = actions[0],
                    ActionType = MOBOnScreenAlertActionType.NAVIGATE_BACK
                },
                new MOBOnScreenActions
                {
                    ActionTitle = actions[1],
                    ActionType = MOBOnScreenAlertActionType.CONTINUE_TO_SELECTTRIP
                },
                new MOBOnScreenActions
                {
                    ActionTitle = callactions[0],
                    ActionType = MOBOnScreenAlertActionType.CALL_US,
                    ActionURL = callactions[1]
                }
            };
            onScreenAlerts.Add(mobOnScreenAlert);
            return onScreenAlerts;
        }
        public async System.Threading.Tasks.Task UpdateFSRMoneyPlusMilesOptionsBasedOnEFOP(MOBRequest request, Session session, MOBShoppingCart shoppingCart, MOBSHOPReservation reservation, List<FormofPaymentOption> response)
        {
            try
            {
                if (IsEnableMoneyPlusMilesFeature(request.Application.Id, request.Application.Version.Major, session?.CatalogItems))
                {

                    if (await _featureSettings.GetFeatureSettingValue("EnableMoneyPlusCheckBoxForAllFopFix"))
                    {
                        if (IsIncludeMoneyMilesInRTI(response) || session.IsMoneyPlusMilesSelected)
                        {
                            if (shoppingCart.FormofPaymentDetails.MoneyPlusMilesCredit == null)
                                shoppingCart.FormofPaymentDetails.MoneyPlusMilesCredit = await _sessionHelperService.GetSession<FOPMoneyPlusMilesCredit>(session.SessionId, new FOPMoneyPlusMilesCredit().ObjectName, new List<string> { session.SessionId, new FOPMoneyPlusMilesCredit().ObjectName }).ConfigureAwait(false);
                            reservation.IsEligibleForMoneyPlusMiles = (shoppingCart.FormofPaymentDetails.MoneyPlusMilesCredit != null);
                            reservation.IsMoneyPlusMilesSelected = session.IsMoneyPlusMilesSelected;// Indiactor for UI display the M+M Option with checkbox selected
                        }
                    }
                    else
                    {
                        reservation.IsEligibleForMoneyPlusMiles = (shoppingCart.FormofPaymentDetails.MoneyPlusMilesCredit != null
                           && (session.IsMoneyPlusMilesSelected || IsIncludeMoneyMilesInRTI(response))); // Indicator for Display the M+M on RTI screen 
                        reservation.IsMoneyPlusMilesSelected = session.IsMoneyPlusMilesSelected;// Indiactor for UI display the M+M Option with checkbox selected
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.ILoggerWarning("GetMoneyPlusMilesOptionsForFinalRTIScreen : There is problem getting setting IsEligibleMoneyMiles , {@Request} " + ex.Message, request);
            }
        }
        public async Task DisplayBuyMiles(MOBSHOPReservation reservation, FlightReservationResponse response, Session session,
          MOBAddTravelersRequest mobAddTravlerRequest, List<CMSContentMessage> contentMessages)
        {
            if (response.IsMileagePurchaseRequired && response.IsPurchaseIneligible)
            {
                //Scenario 1 - MOBILE-20327 mApp | Insufficient Mileage of 50% or more 
                string webShareToken = await _shoppingBuyMiles.GetSSOToken(mobAddTravlerRequest.Application.Id,
                    mobAddTravlerRequest.DeviceId, mobAddTravlerRequest.Application.Version.Major,
                    mobAddTravlerRequest.TransactionId, null, mobAddTravlerRequest.SessionId, session.MileagPlusNumber);
                reservation.OnScreenAlert = await OnScreenAlertForMileageBalanceNOTMeetsThreshhold(webShareToken, response.DisplayCart.ActualMileageRequired, contentMessages);
            }
            else if (response.IsMileagePurchaseRequired)
            {   // Scenario 2 - MOBILE-20326 mApp | Insufficient Mileage under 50%                
                reservation.OnScreenAlert = OnScreenAlertForMileageBalanceMeetsThreshhold(response.DisplayCart.ActualMileageRequired, response.DisplayCart.DetectedUserBalance, contentMessages);
                // Add the Grrand total with PRICE TYPE MPF
                ApplyMPFRequriedAmountToGrandTotal(reservation);
            }
        }

        private void ApplyMPFRequriedAmountToGrandTotal(MOBSHOPReservation reservation)
        {
            if (reservation.Prices != null && reservation.Prices.Count > 0)
            {
                var grandTotalIndex = reservation.Prices.FindIndex(a => a.PriceType == "GRAND TOTAL");
                if (grandTotalIndex >= 0)
                {
                    double extraMilePurchaseAmount = (reservation?.Prices?.Where(a => a.DisplayType == "MPF")?.FirstOrDefault()?.Value != null) ?
                                             Convert.ToDouble(reservation?.Prices?.Where(a => a.DisplayType == "MPF")?.FirstOrDefault()?.Value) : 0;
                    if (extraMilePurchaseAmount > 0)
                    {
                        reservation.Prices[grandTotalIndex].Value += extraMilePurchaseAmount;
                        CultureInfo ci = null;
                        ci = TopHelper.GetCultureInfo(reservation?.Prices[grandTotalIndex].CurrencyCode);
                        reservation.Prices[grandTotalIndex].DisplayValue = TopHelper.FormatAmountForDisplay(reservation?.Prices[grandTotalIndex].Value.ToString(), ci, false);
                        reservation.Prices[grandTotalIndex].FormattedDisplayValue = TopHelper.FormatAmountForDisplay(reservation?.Prices[grandTotalIndex].Value.ToString(), ci, false);
                    }
                }
            }
        }
        private async Task<MOBOnScreenAlert> OnScreenAlertForMileageBalanceNOTMeetsThreshhold(string webShareToken, int actualMileageRequired, List<CMSContentMessage> contentMessages)
        {
            MOBOnScreenAlert onScreenAlert = null;

            if (await _featureSettings.GetFeatureSettingValue("EnableRedirectURLUpdate").ConfigureAwait(false))
            {
                onScreenAlert = new MOBOnScreenAlert
                {
                    AlertType = MOBOnScreenAlertType.BUYMILES,
                    Title = GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.BuyMiles.AlertTitle") ?? "You don't have enough miles",
                    Message = string.Format(GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.BuyMiles.NotMeetsThreshhold.AlertMessage") ?? _configuration.GetValue<string>("SelectTrip.BuyMiles.NOTMeetsThreshhold.AlertMessage"), string.Format("{0:n0}", actualMileageRequired)),
                    Actions = new List<MOBOnScreenActions>()
                {
                    new MOBOnScreenActions
                    {
                        ActionTitle = GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.BuyMiles.NotMeetsThreshhold.PurchaseMilesText") ?? "Purchase miles",
                        ActionType = MOBOnScreenAlertActionType.NAVIGATE_TO_EXTERNAL,
                        ActionURL = $"{_configuration.GetValue<string>("NewDotcomSSOUrl")}?type=sso&token={webShareToken}&landingUrl={_configuration.GetValue<string>("BuyMilesExternalMilegePlusURL")}",
                        WebShareToken = string.Empty,
                        WebSessionShareUrl = string.Empty
                    },
                    new MOBOnScreenActions
                    {
                        ActionTitle = GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.SelectDifferentFlightText") ?? "Select a different flight",
                        ActionType = MOBOnScreenAlertActionType.NAVIGATE_TO_FSR
                    }
                }
                };
            }
            else
            {
                onScreenAlert = new MOBOnScreenAlert
                {
                    AlertType = MOBOnScreenAlertType.BUYMILES,
                    Title = GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.BuyMiles.AlertTitle") ?? "You don't have enough miles",
                    Message = string.Format(GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.BuyMiles.NotMeetsThreshhold.AlertMessage") ?? _configuration.GetValue<string>("SelectTrip.BuyMiles.NOTMeetsThreshhold.AlertMessage"), string.Format("{0:n0}", actualMileageRequired)),
                    Actions = new List<MOBOnScreenActions>()
                {
                    new MOBOnScreenActions
                    {
                        ActionTitle = GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.BuyMiles.NotMeetsThreshhold.PurchaseMilesText") ?? "Purchase miles",
                        ActionType = MOBOnScreenAlertActionType.NAVIGATE_TO_EXTERNAL,
                        ActionURL = _configuration.GetValue<string>("BuyMilesExternalMilegePlusURL"),
                        WebShareToken = webShareToken,
                        WebSessionShareUrl = _configuration.GetValue<string>("DotcomSSOUrl")
                    },
                    new MOBOnScreenActions
                    {
                        ActionTitle = GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.SelectDifferentFlightText") ?? "Select a different flight",
                        ActionType = MOBOnScreenAlertActionType.NAVIGATE_TO_FSR
                    }
                }
                };
            }
            return onScreenAlert;
        }

        private MOBOnScreenAlert OnScreenAlertForMileageBalanceMeetsThreshhold(int actualMileageRequired, int detectedUserBalance, List<CMSContentMessage> contentMessages)
        {
            var onScreenAlert = new MOBOnScreenAlert
            {
                AlertType = MOBOnScreenAlertType.BUYMILES,
                Title = GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.BuyMiles.AlertTitle") ?? "You don't have enough miles",
                Message = string.Format(GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.BuyMiles.MeetsThreshhold.AlertMessage") ?? _configuration.GetValue<string>("SelectTrip.BuyMiles.MeetsThreshhold.AlertMessage"), string.Format("{0:n0}", actualMileageRequired), string.Format("{0:n0}", detectedUserBalance)),
                Actions = new List<MOBOnScreenActions>()
                {
                    new MOBOnScreenActions
                    {
                        ActionTitle = GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.BuyMiles.MeetsThreshhold.AddMilesText") ?? "Continue and add miles",
                        ActionType = MOBOnScreenAlertActionType.ADD_MILES
                    },
                    new MOBOnScreenActions
                    {
                        ActionTitle = GetSDLStringMessageFromList(contentMessages, "Booking.SelectTraveler.SelectDifferentFlightText") ?? "Select a different flight",
                        ActionType = MOBOnScreenAlertActionType.NAVIGATE_TO_FSR
                    }
                }
            };

            return onScreenAlert;
        }

        public string GetCorporateDisclaimerText(MOBSHOPShopRequest shopRequest, bool isU4BCorporateBookingEnabled, bool isEnableSuppressingCompanyNameForBusiness, bool isReshopChange)
        {
            if (isReshopChange)
                return string.Empty;

            if (isEnableSuppressingCompanyNameForBusiness)
            {
                return isU4BCorporateBookingEnabled
                    ? string.Format(_configuration.GetValue<string>("U4BCorporateBookingDisclaimerText"), shopRequest.MOBCPCorporateDetails.CorporateCompanyName)
                    : string.Format(shopRequest.MOBCPCorporateDetails.CorporateCompanyName + " {0}", _configuration.GetValue<string>("CorporateDisclaimerText") ?? string.Empty);
            }
            else
                return _configuration.GetValue<string>("CorporateDisclaimerTextForBusinessTravel");
        }

        public string GetCorporateDisclaimerText(MOBSHOPShopRequest shopRequest, bool isU4BCorporateBookingEnabled)
        {
            return isU4BCorporateBookingEnabled
                ? string.Format(_configuration.GetValue<string>("U4BCorporateBookingDisclaimerText"), shopRequest.MOBCPCorporateDetails.CorporateCompanyName)
                : string.Format(shopRequest.MOBCPCorporateDetails.CorporateCompanyName + " {0}", _configuration.GetValue<string>("CorporateDisclaimerText") ?? string.Empty);
        }

        public MOBSHOPAvailability AddFSROAFalsSaleAlerts(MOBSHOPAvailability availability, List<CMSContentMessage> lstMessages, ErrorInfo errorInfo)
        {
            if (availability == null) availability = new MOBSHOPAvailability();
            if (availability?.OnScreenAlerts == null)
            {
                availability.OnScreenAlerts = new List<MOBOnScreenAlert>();
            }

            List<string> lstFlightDetails = null;
            string alertMessage = "";
            string displayPriceForOA = "";
            string displayRevenuePrice = "";
            string displayAwardPrice = "";
            if (errorInfo != null && errorInfo.AdditionalInfo?.Any(a => a.FlightNumbers?.Count() > 0) == true)
            {
                lstFlightDetails = new List<string>();

                foreach (var flightNumber in errorInfo.AdditionalInfo?.FirstOrDefault()?.FlightNumbers)
                {
                    lstFlightDetails.Add("Flight " + flightNumber);
                }
                if (Convert.ToBoolean(_configuration.GetValue<string>("EnableAddFSROAFalsSaleAlerts")))
                {
                    CultureInfo ci = TopHelper.GetCultureInfo("");
                    displayRevenuePrice = errorInfo.AdditionalInfo?.FirstOrDefault()?.ItineraryPrice?.Amount;
                    displayAwardPrice = errorInfo.AdditionalInfo?.FirstOrDefault()?.ItineraryPrice?.AwardPrice?.Amount;
                    displayPriceForOA = TopHelper.FormatAmountForDisplay(displayRevenuePrice.ToString(), ci, true);
                    if (!string.IsNullOrEmpty(displayAwardPrice))
                    {
                        displayPriceForOA = ShopStaticUtility.FormatAwardAmountForDisplay(displayAwardPrice.ToString(), true) + " + " + TopHelper.FormatAmountForDisplay(displayRevenuePrice.ToString(), ci, true);

                    }
                    string oaFalshSaleMessage = GetSDLStringMessageFromList(lstMessages, "FSR.OAFlashSale_Message");
                    if (!string.IsNullOrEmpty(oaFalshSaleMessage) && !string.IsNullOrEmpty(displayRevenuePrice))
                        alertMessage = string.Format(oaFalshSaleMessage, displayPriceForOA, string.Join(", ", lstFlightDetails));
                }
                else
                {
                    displayPriceForOA = errorInfo.AdditionalInfo?.FirstOrDefault()?.ItineraryPrice?.Amount;
                    CultureInfo ci = TopHelper.GetCultureInfo("");
                    string oaFalshSaleMessage = GetSDLStringMessageFromList(lstMessages, "FSR.OAFlashSale_Message");
                    if (!string.IsNullOrEmpty(oaFalshSaleMessage) && !string.IsNullOrEmpty(displayPriceForOA))
                        alertMessage = string.Format(oaFalshSaleMessage, TopHelper.FormatAmountForDisplay(displayPriceForOA, ci, true), string.Join(", ", lstFlightDetails));

                }
            }
            MOBOnScreenAlert mobOnScreenAlert = new MOBOnScreenAlert();
            mobOnScreenAlert.Title = GetSDLStringMessageFromList(lstMessages, "FSR.OAFlashSale.Header") ?? "Choose another fare";
            mobOnScreenAlert.AlertType = MOBOnScreenAlertType.OAFLASHSALE;
            mobOnScreenAlert.Message = alertMessage;
            mobOnScreenAlert.Actions = new List<MOBOnScreenActions>();
            mobOnScreenAlert.Actions.Add(new MOBOnScreenActions
            {
                ActionTitle = GetSDLContentShortMessageFromList(lstMessages, "FSR.OAFlashSale.Header") ?? "Choose another fare",
                ActionType = MOBOnScreenAlertActionType.NAVIGATE_BACK

            });
            availability.OnScreenAlerts.Add(mobOnScreenAlert);
            return availability;
        }
        public MOBSHOPAvailability AddFSROAFalsSaleAlertsInReshop(MOBSHOPAvailability availability, List<CMSContentMessage> lstMessages, ErrorInfo errorInfo)
        {
            if (availability == null) availability = new MOBSHOPAvailability();
            if (availability?.OnScreenAlerts == null)
            {
                availability.OnScreenAlerts = new List<MOBOnScreenAlert>();
            }

            List<string> lstFlightDetails = null;
            MOBMobileCMSContentMessages objCMSContent = GetSDLMessageFromList(lstMessages, "FSR.ReShop_OAFlashSale_Message").FirstOrDefault();
            string alertMessage = "";
            if (errorInfo != null && errorInfo.AdditionalInfo?.Any(a => a.FlightNumbers?.Count() > 0) == true)
            {
                lstFlightDetails = new List<string>();
                foreach (var flightNumber in errorInfo.AdditionalInfo?.FirstOrDefault()?.FlightNumbers)
                {
                    lstFlightDetails.Add("Flight " + flightNumber);
                }
                string oaFalshSaleMessage = objCMSContent.ContentFull;
                if (!string.IsNullOrEmpty(oaFalshSaleMessage))
                    alertMessage = "<br>" + oaFalshSaleMessage + "<br><br>" + string.Join(", ", lstFlightDetails);
            }

            MOBOnScreenAlert mobOnScreenAlert = new MOBOnScreenAlert();
            mobOnScreenAlert.Title = objCMSContent.HeadLine;
            mobOnScreenAlert.AlertType = MOBOnScreenAlertType.OAFLASHSALE;
            mobOnScreenAlert.Message = alertMessage;
            mobOnScreenAlert.Actions = new List<MOBOnScreenActions>();
            mobOnScreenAlert.Actions.Add(new MOBOnScreenActions
            {
                ActionTitle = objCMSContent.ContentShort,
                ActionType = MOBOnScreenAlertActionType.NAVIGATE_BACK

            });
            availability.OnScreenAlerts.Add(mobOnScreenAlert);
            return availability;
        }
        public string BuildETCCreditsStrikeThroughDescription()
        {
            return _configuration.GetValue<string>("ETCCreditsStrikeThroughTypeDescription");
        }
        public async Task SetEligibilityforETCTravelCredit(MOBSHOPReservation reservation, Session session, Reservation bookingPathReservation)
        {
            if (await _featureSettings.GetFeatureSettingValue("EnableFSRETCTravelCreditsFeature").ConfigureAwait(false))
            {//_configuration.GetValue<bool>("EnableFSRETCCreditsFeature")
                reservation.EligibleForETCPricingType = false;
                if (session.PricingType == PricingType.ETC.ToString())
                {
                    reservation.EligibleForETCPricingType = true;
                    reservation.PricingType = session.PricingType;
                }
                else
                {
                    if (bookingPathReservation.FormOfPaymentType == MOBFormofPayment.ETC)
                    {
                        reservation.EligibleForETCPricingType = true;
                    }
                    reservation.PricingType = session.PricingType;
                }
            }
        }
        public async Task<List<MOBFSRAlertMessage>> SetFSRTravelTypeAlertMessage(Session session, List<CMSContentMessage> lstMessages = null)
        {
            List<MOBFSRAlertMessage> fSRAlertMessages = new List<MOBFSRAlertMessage>();
            if (await _featureSettings.GetFeatureSettingValue("EnableFSRETCTravelCreditsFeature").ConfigureAwait(false) && !string.IsNullOrEmpty(session.PricingType) && !string.IsNullOrEmpty(session.CreditsAmount))
            {

                if (lstMessages != null && lstMessages.Any(x => x.LocationCode?.Equals(_configuration.GetValue<string>("SDLFSRETCCreditsApplyCredits")) ?? false))
                {

                    var sDlContent = GetSDLMessageFromList(lstMessages, _configuration.GetValue<string>("SDLFSRETCCreditsApplyCredits"));
                    fSRAlertMessages.Add(new MOBFSRAlertMessage
                    {
                        HeaderMessage = $"{sDlContent?.First().HeadLine}: ${session.CreditsAmount}",
                        BodyMessage = string.Format(sDlContent?.First().ContentFull, session.CreditsAmount), //$"You have ${session.CreditsAmount} of applicable credits you can use for this fare, including all passengers Note: ETC credits are only displayed at this time.",
                        MessageType = 0,
                        AlertType = MOBFSRAlertMessageType.Information.ToString(),
                        IsAlertExpanded = true,
                        MessageTypeDescription = FSRAlertMessageType.CreditsRemoved,
                        RestHandlerType = MOBFSREnhancementType.WithResultsTravelCreditsETC.ToString(),
                        Buttons = new List<MOBFSRAlertMessageButton> { new MOBFSRAlertMessageButton { ButtonLabel = sDlContent?.First().ContentShort, RedirectUrl = "Applycredits" } }

                    });
                }
                else
                {
                    fSRAlertMessages.Add(
                        new MOBFSRAlertMessage
                        {
                            HeaderMessage = $"Total credit balance: ${session.CreditsAmount}",
                            BodyMessage = string.Format(_configuration.GetValue<string>("FSRETCCreditsApplyMessage"), session.CreditsAmount), //$"You have ${session.CreditsAmount} of applicable credits you can use for this fare, including all passengers Note: ETC credits are only displayed at this time.",
                            MessageType = 0,
                            AlertType = MOBFSRAlertMessageType.Information.ToString(),
                            IsAlertExpanded = true,
                            MessageTypeDescription = FSRAlertMessageType.CreditsRemoved,
                            RestHandlerType = MOBFSREnhancementType.WithResultsTravelCreditsETC.ToString(),
                            Buttons = new List<MOBFSRAlertMessageButton> { new MOBFSRAlertMessageButton { ButtonLabel = "Apply credits", RedirectUrl = "Applycredits" } }

                        });
                }
                if (lstMessages != null && lstMessages.Any(x => x.LocationCode?.Equals(_configuration.GetValue<string>("SDLFSRETCCreditsRemoveCredits")) ?? false))
                {
                    var sDlContent = GetSDLMessageFromList(lstMessages, _configuration.GetValue<string>("SDLFSRETCCreditsRemoveCredits"));
                    fSRAlertMessages.Add(new MOBFSRAlertMessage
                    {
                        HeaderMessage = $"{sDlContent?.First().HeadLine}: ${session.CreditsAmount}",
                        BodyMessage = string.Format(sDlContent?.First().ContentFull, session.CreditsAmount),
                        MessageType = 0,
                        AlertType = MOBFSRAlertMessageType.Success.ToString(),
                        IsAlertExpanded = true,
                        MessageTypeDescription = FSRAlertMessageType.CreditsApplied,
                        RestHandlerType = MOBFSREnhancementType.WithResultsTravelCreditsETC.ToString(),
                        Buttons = new List<MOBFSRAlertMessageButton> { new MOBFSRAlertMessageButton { ButtonLabel = sDlContent?.First().ContentShort, RedirectUrl = "Removecredits" } }
                    });
                }
                else
                {
                    fSRAlertMessages.Add(
                    new MOBFSRAlertMessage
                    {
                        HeaderMessage = $"Total credit balance: ${session.CreditsAmount}",
                        BodyMessage = string.Format(_configuration.GetValue<string>("FSRETCCreditsRemoveMessage"), session.CreditsAmount),
                        MessageType = 0,
                        AlertType = MOBFSRAlertMessageType.Success.ToString(),
                        IsAlertExpanded = true,
                        MessageTypeDescription = FSRAlertMessageType.CreditsApplied,
                        RestHandlerType = MOBFSREnhancementType.WithResultsTravelCreditsETC.ToString(),
                        Buttons = new List<MOBFSRAlertMessageButton> { new MOBFSRAlertMessageButton { ButtonLabel = "Remove credits", RedirectUrl = "Removecredits" } }
                    });
                }
                return fSRAlertMessages;
            }
            return fSRAlertMessages;
        }

    }



    public static class Extension
    {
        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetType()?
                            .GetMember(enumValue.ToString())?
                            .First()?
                            .GetCustomAttribute<DisplayAttribute>()?
                            .Name;
        }

        // Convert the string to Pascal case.
        public static string ToPascalCase(this string the_string)
        {
            // If there are 0 or 1 characters, just return the string.
            if (the_string == null) return the_string;
            if (the_string.Length < 2) return the_string.ToUpper();

            // Split the string into words.
            string[] words = the_string.Split(
                new char[] { },
                StringSplitOptions.RemoveEmptyEntries);

            // Combine the words.
            string result = "";
            foreach (string word in words)
            {
                result +=
                    word.Substring(0, 1).ToUpper() +
                    word.Substring(1);
            }

            return result;
        }


    }

    public class AirportList
    {
        public string AirportCode { get; set; } = string.Empty;
        public string AirportName { get; set; } = string.Empty;
        public string Cityname { get; set; } = string.Empty;
    }

}