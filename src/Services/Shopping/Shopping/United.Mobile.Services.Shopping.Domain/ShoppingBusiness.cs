using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.Shopping;
using United.Definition.Shopping;
using United.Ebs.Logging.Enrichers;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.TripPlanGetService;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Common;
using United.Mobile.Model.Shopping.Common.TripPlanner;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.Shopping.Misc;
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Service.Presentation.ReferenceDataRequestModel;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Services.FlightShopping.Common.LMX;
using United.TravelPlanner.Models;
using United.Utility.Enum;
using United.Utility.Helper;
using United.Utility.Serilog;
using ELFRitMetaShopMessages = United.Common.Helper.Shopping.ELFRitMetaShopMessages;
using Flight = United.Services.FlightShopping.Common.Flight;
using IMerchandizingServices = United.Common.Helper.Merchandize.IMerchandizingServices;
using MOBFutureFlightCredit = United.Mobile.Model.Shopping.MOBFutureFlightCredit;
using PKDispenserResponse = United.Mobile.Model.Shopping.PKDispenserResponse;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using ShopResponse = United.Mobile.Model.Shopping.ShopResponse;
using StatusType = United.Services.FlightShopping.Common.StatusType;
using United.Mobile.Model.Common.SSR;
using United.CorporateDirect.Models.UniversalConnect;
using United.Utility.AppVersion;


namespace United.Mobile.Services.Shopping.Domain
{
    public class ShoppingBusiness : IShoppingBusiness
    {
        private readonly ICacheLog<ShoppingBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly ICachingService _cachingService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IShopBooking _shopBooking;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IFormsOfPayment _formsOfPayment;
        private readonly IOmniCart _omniCart;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly ICSLStatisticsService _cSLStatisticsService;
        private readonly IFlightShoppingService _flightShoppingService;
        private readonly IUnfinishedBooking _unfinishedBooking;
        private readonly IDPService _dPService;
        private readonly ITravelerCSL _travelerCSL;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly IReferencedataService _referencedataService;
        private readonly ILMXInfo _lmxInfo;
        private readonly IGMTConversionService _gMTConversionService;
        private readonly IPKDispenserService _pKDispenserService;
        private readonly ICMSContentService _iCMSContentService;
        private readonly IMerchandizingServices _merchandizingServices;
        private readonly IShoppingBuyMiles _shoppingBuyMiles;
        private readonly IApplicationEnricher _requestEnricher;
        private CSLStatistics _cSLStatistics;
        private AirportDetailsList airportsList = null;
        private HttpContext _httpContext;
        private readonly PKDispenserPublicKey _pKDispenserPublicKey;
        private ITripPlannerIDService _tripPlannerIDService;
        private readonly IFeatureSettings _featureSettings;
        private readonly ILogger<ShoppingBusiness> _logger1;
        private readonly IFeatureToggles _featureToggles;
        public ShoppingBusiness(IConfiguration configuration, ISessionHelperService sessionHelperService)
        {
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
        }

        public ShoppingBusiness(ICacheLog<ShoppingBusiness> logger
            , IConfiguration configuration
            , IHeaders headers
            , ISessionHelperService sessionHelperService
            , ICachingService cachingService
            , IShopBooking shopBooking
            , IShoppingUtility shoppingUtility
            , IDynamoDBService dynamoDBService
            , IShoppingSessionHelper shoppingSessionHelper
            , ICSLStatisticsService cSLStatisticsService
            , IFormsOfPayment formsOfPayment
            , IOmniCart omniCart
            , IFFCShoppingcs fFCShoppingcs
            , IFlightShoppingService flightShoppingService
            , IDPService dPService
            , IUnfinishedBooking unfinishedBooking
            , ITravelerCSL travelerCSL
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , IReferencedataService referencedataService
            , ILMXInfo lmxInfo
            , IGMTConversionService gMTConversionService
            , IPKDispenserService pKDispenserService
            , ICMSContentService iCMSContentService
            , IMerchandizingServices merchandizingServices
            , IShoppingBuyMiles shoppingBuyMiles
            , IApplicationEnricher requestEnricher, ITripPlannerIDService tripPlannerIDService
            ,IFeatureSettings featureSettings
            , ILogger<ShoppingBusiness> logger1
            , IFeatureToggles featureToggles)
            
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _sessionHelperService = sessionHelperService;
            _cachingService = cachingService;
            _shopBooking = shopBooking;
            _shoppingUtility = shoppingUtility;
            _shoppingSessionHelper = shoppingSessionHelper;
            _dynamoDBService = dynamoDBService;
            _cSLStatisticsService = cSLStatisticsService;
            _formsOfPayment = formsOfPayment;
            _omniCart = omniCart;
            _fFCShoppingcs = fFCShoppingcs;
            _flightShoppingService = flightShoppingService;
            _dPService = dPService;
            _unfinishedBooking = unfinishedBooking;
            _travelerCSL = travelerCSL;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _referencedataService = referencedataService;
            _lmxInfo = lmxInfo;
            _gMTConversionService = gMTConversionService;
            _pKDispenserService = pKDispenserService;
            _iCMSContentService = iCMSContentService;
            _merchandizingServices = merchandizingServices;
            _shoppingBuyMiles = shoppingBuyMiles;
            _requestEnricher = requestEnricher;
            _cSLStatistics = new CSLStatistics(_logger, _configuration, _dynamoDBService, _cSLStatisticsService);
            _tripPlannerIDService = tripPlannerIDService;
            _pKDispenserPublicKey = new PKDispenserPublicKey(_configuration, _cachingService, _dPService, _pKDispenserService, headers);
            _featureSettings = featureSettings;
            _logger1 = logger1;
            _featureToggles = featureToggles;
        }

        public async Task<ShopResponse> GetShop(MOBSHOPShopRequest request, HttpContext httpContext)
        {
            //if (request?.Trips != null && request.Trips.Any())
            //{
            //    request.Trips.ForEach(q => q.OriginAllAirports = (q.OriginAllAirports == -1) ? 0 : q.OriginAllAirports);
            //}
            _httpContext = httpContext;
            string cssCallDuration = string.Empty;
            string cssCallDuration2 = string.Empty;
            ShopResponse response = new ShopResponse();
            IDisposable timer = null;
            string timedOperationMessage = string.Empty;
            string logAction = request.IsReshopChange ? "ReShop - Shop" : "Shop";
            bool isTripPlanFlow = IsTripPlanFlow(request.TravelType);
            if (!request.IsReshopChange && isTripPlanFlow)
            {
                logAction = "TripPlan" + logAction;
                DisableSearchNearByAirportsTripPlan(request);
            }
            string logMessageType = request.IsReshopChange ? " - " + request.RecordLocator : "";
            bool isFirstShopCall = string.IsNullOrEmpty(request.SessionId) || request.IsReshopChange;

            bool IsEnableEditSearchOnFSRHeaderBooking = false;
            try
            {
                bool isEnableMultiCityEditSearchOnFSRBooking = await _featureToggles.IsEnableMultiCityEditSearchOnFSRBooking(request.Application.Id, request.Application.Version.Major, request.CatalogItems);
                #region Code for edit search
                IsEnableEditSearchOnFSRHeaderBooking = _shoppingUtility.IsEnableEditSearchOnFSRHeaderBooking(request.Application.Id, request.Application.Version.Major, request?.CatalogItems) && _shoppingUtility.EnableEditSearchOnFSRHeaderBooking(request, isEnableMultiCityEditSearchOnFSRBooking) && !isTripPlanFlow;
                if (request.IsEditSearchEnabledOnBookingFSR && IsEnableEditSearchOnFSRHeaderBooking)
                {
                    MOBSHOPShopRequest shopRequest = new MOBSHOPShopRequest();
                    shopRequest = await _sessionHelperService.GetSession<MOBSHOPShopRequest>(request?.SessionId, shopRequest.ObjectName, new List<string> { request?.SessionId, shopRequest.ObjectName });

                    if (shopRequest != null && !request.AwardTravel)
                    {
                        shopRequest.SearchType = request.SearchType;
                        shopRequest.Trips = request.Trips;
                        shopRequest.AwardTravel = request.AwardTravel;
                        shopRequest.TravelType = request.TravelType;
                        shopRequest.CatalogItems = request.CatalogItems;
                        shopRequest.GetNonStopFlightsOnly = request.GetNonStopFlightsOnly;
                        shopRequest.GetFlightsWithStops = request.GetFlightsWithStops;
                        shopRequest.SessionId = request.SessionId;
                        shopRequest.IsEditSearchEnabledOnBookingFSR = request.IsEditSearchEnabledOnBookingFSR;
                        shopRequest.Experiments = request.Experiments;

                        request = shopRequest;
                    }
                    else if (shopRequest != null && request.AwardTravel)
                    {
                        shopRequest.SearchType = request.SearchType;
                        shopRequest.Trips = request.Trips;
                        shopRequest.AwardTravel = request.AwardTravel;
                        shopRequest.IsELFFareDisplayAtFSR = false;
                        shopRequest.TravelType = request.TravelType;
                        shopRequest.CatalogItems = request.CatalogItems;
                        shopRequest.GetNonStopFlightsOnly = request.GetNonStopFlightsOnly;
                        shopRequest.GetFlightsWithStops = request.GetFlightsWithStops;
                        shopRequest.SessionId = request.SessionId;
                        shopRequest.IsEditSearchEnabledOnBookingFSR = request.IsEditSearchEnabledOnBookingFSR;
                        shopRequest.Experiments = request.Experiments;
                        shopRequest.ShowMileageDetails = request.ShowMileageDetails;
                        shopRequest.MileagePlusAccountNumber = request.MileagePlusAccountNumber;
                        shopRequest.HashPinCode = request.HashPinCode;
                        shopRequest.PremierStatusLevel = request.PremierStatusLevel;
                        shopRequest.PromotionCode = string.Empty;
                        shopRequest.FareClass = string.Empty;
                        shopRequest.IsExpertModeEnabled = request.IsExpertModeEnabled;
                        shopRequest.MOBCPCorporateDetails = request.MOBCPCorporateDetails;
                        shopRequest.CustomerMetrics = request.CustomerMetrics;

                        request = shopRequest;
                    }
                }
                #endregion
                if (_configuration.GetValue<bool>("DisableTimeOutApp2_1_22") && _shoppingUtility.ShopTimeOutCheckforAppVersion(request.Application.Id, request.Application.Version.Major))
                {
                    return response;
                }

                               
                if (request.IsReshopChange || (_shoppingUtility.EnableAwardNonStop(request.Application.Id, request.Application.Version.Major) == false && request.AwardTravel))
                {
                        request.GetNonStopFlightsOnly = false;
                        request.GetFlightsWithStops = false;
                }
                                

                SetIsBEFareDisplayAtFSR(request);

                new ForceUpdateVersion(_configuration).ForceUpdateForNonSupportedVersion(request.Application.Id, request.Application.Version.Major, United.Utility.Enum.FlowType.BOOKING);
                ValidateTravelerTypeForCLB(request);//Atleat one senior or adult should be there on reservation for Corporate Leisure Booking

                Session session = null;
                if (request.IsReshopChange && !string.IsNullOrEmpty(request.SessionId))
                {
                    session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId);
                }
                else if (_configuration.GetValue<bool>("EnableNonStopFlight") && !string.IsNullOrEmpty(request.SessionId))
                {
                    session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId);
                }
                else
                {
                    session = await _shoppingSessionHelper.CreateShoppingSession(request.Application.Id, request.DeviceId,
                        request.Application.Version.Major, request.TransactionId, request.MileagePlusAccountNumber,
                        request.EmployeeDiscountId, request.IsELFFareDisplayAtFSR,
                        request.IsReshopChange, request.AwardTravel, request.TravelType, FlowType.BOOKING.ToString(), request.HashPinCode);
                }
                if (!String.IsNullOrEmpty(session.SessionId) && _headers.ContextValues != null)
                {
                    _headers.ContextValues.SessionId = session.SessionId;
                    _requestEnricher.Add(United.Mobile.Model.Constants.SessionId, session.SessionId);
                }

                if (string.IsNullOrEmpty(request.SessionId))
                {
                    request.SessionId = session.SessionId;
                }

                //Book with Travel Credit
                if (!_configuration.GetValue<bool>("EnableBookWithCredit") && !string.IsNullOrEmpty(session.SessionId) && !string.IsNullOrEmpty(request.BWCSessionId) && !request.IsReshop)
                {
                    session.BWCSessionId = request.BWCSessionId;
                }

                response.ExperimentEvents = await _shoppingUtility.ValidateAwardFSR(request);

                session = await _shoppingUtility.ValidateFSRRedesign(request, session);
                _shoppingSessionHelper.CurrentSession = session;
                if (session.IsExpertModeEnabled != request.IsExpertModeEnabled)
                {
                    session.IsExpertModeEnabled = request.IsExpertModeEnabled;
                    await _sessionHelperService.SaveSession<Session>(session, request.SessionId, new List<string> { request.SessionId, session.ObjectName }, session.ObjectName);
                }

                if (request.CatalogItems != null && request.CatalogItems.Count > 0)
                {
                    session.CatalogItems = request.CatalogItems;

                    if (IsEnableEditSearchOnFSRHeaderBooking)
                    {
                        session.ShowEditSearchHeaderOnFSRBooking = true;
                        response.ShowEditSearchHeaderOnFSRBooking = true;
                    }
                    else
                    {
                        session.ShowEditSearchHeaderOnFSRBooking = false;
                    }
                    await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName);
                }

                //Fix for Empty TravelerTypeCode for non-profileowner pax EMP20 myUADiscount travel - MOBILE-10688 (EMP20 PNRs with no fare quote and unexpected error) - Shashank  
                if (!_configuration.GetValue<bool>("DisableCheckforEmptyEMP20TravelerTypeCode") && request.Application.Id == 1 && !request.EmployeeDiscountId.IsNullOrEmpty()
                    && !request.TravelerTypes.IsNullOrEmpty() && request.TravelerTypes.Count() > 0)
                {
                    request.NumberOfAdults = request.TravelerTypes.FirstOrDefault().Count;
                    request.TravelerTypes = null;
                }

                ShoppingResponse shop = new ShoppingResponse();
                shop.Request = request;
                if (_shoppingUtility.IsEnableMoneyPlusMilesFeature(request.Application.Id, request.Application.Version.Minor, session?.CatalogItems))
                {
                    await _sessionHelperService.SaveSession<MOBSHOPShopRequest>(request, session.SessionId, new List<string> { session.SessionId, request.ObjectName }, request.ObjectName).ConfigureAwait(false);
                }
                if (!_configuration.GetValue<bool>("EnableNonStopFlight"))
                {
                    await _sessionHelperService.SaveSession<ShoppingResponse>(shop, request.SessionId, new List<string> { request.SessionId, shop.ObjectName }, shop.ObjectName);
                }
                else
                {
                    if ((!request.GetNonStopFlightsOnly && !request.GetFlightsWithStops) || (request.GetNonStopFlightsOnly && !request.GetFlightsWithStops))
                    {
                        await _sessionHelperService.SaveSession<ShoppingResponse>(shop, request.SessionId, new List<string> { request.SessionId, shop.ObjectName }, shop.ObjectName);
                    }
                    else if (!request.GetNonStopFlightsOnly && request.GetFlightsWithStops)
                    {
                        shop = await _sessionHelperService.GetSession<ShoppingResponse>(request.SessionId, shop.ObjectName, new List<string> { request.SessionId, shop.ObjectName });
                    }
                }
                response.TransactionId = request.TransactionId;
                response.LanguageCode = request.LanguageCode;
                response.ShopRequest = request;
                
                int nonStopFlightsCount = 0;
                if (_configuration.GetValue<bool>("EnableNonStopFlight"))
                {
                    if (request.GetFlightsWithStops)
                    {
                        MOBSHOPAvailability nonStopsAvailability = await _shopBooking.GetLastTripAvailabilityFromPersist(1, session.SessionId);
                        nonStopFlightsCount = nonStopsAvailability.Trip.FlightCount;
                    }
                }
                string mobilePosCountries = _configuration.GetValue<string>("PosMobileSupportedCountryList");
                if (_shoppingUtility.IsPosRedirectInShopEnabled(request))
                {
                    response.PointOfSale = new PointOfSale();
                    response.InternationalPointofSale = new PointOfSale();  // duplicate property for android not to crash
                    string redirectUrlToDotCom = _shoppingUtility.BuildDotComRedirectUrl(request);

                    if (_configuration.GetValue<bool>("ForTestingPurposeReturnOnlyWWWUnitedDotCom") && request.Trips[0].Origin.ToUpper().Trim() == "CLL" && request.Trips[0].Destination == "BOM")
                    {
                        redirectUrlToDotCom = "https://www.united.com";
                    }

                    await _shoppingUtility.ValidateAndGetSignSignOn(request, response);
                    response.PointOfSale.RedirectUrl = redirectUrlToDotCom;
                    if (_configuration.GetValue<bool>("DisablePosCpuntryMessage") == false)
                        response.PointOfSale.CountryNotSupportedMessage = _configuration.GetValue<string>("PosPopUpMessageTextV2");
                    else
                        response.PointOfSale.CountryNotSupportedMessage = $"{request.PointOfSaleCountryName}{_configuration.GetValue<string>("PosPopUpMessageText")}";

                    response.InternationalPointofSale.RedirectUrl = redirectUrlToDotCom;
                    if (_configuration.GetValue<bool>("DisablePosCpuntryMessage") == false)
                        response.InternationalPointofSale.CountryNotSupportedMessage = _configuration.GetValue<string>("PosPopUpMessageTextV2");
                    else
                        response.InternationalPointofSale.CountryNotSupportedMessage = $"{request.PointOfSaleCountryName}{_configuration.GetValue<string>("PosPopUpMessageText")}";

                    if (await _featureSettings.GetFeatureSettingValue("EnableRedirectURLUpdate").ConfigureAwait(false) && !string.IsNullOrEmpty(response.PointOfSale.WebShareToken) && !string.IsNullOrEmpty(response.PointOfSale.WebSessionShareUrl))
                    {
                        response.InternationalPointofSale.RedirectUrl = response.PointOfSale.RedirectUrl = $"{_configuration.GetValue<string>("NewDotcomSSOUrl")}?type=sso&token={response.PointOfSale.WebShareToken}&landingUrl={redirectUrlToDotCom}";
                        response.PointOfSale.WebSessionShareUrl = response.InternationalPointofSale.WebSessionShareUrl = string.Empty;
                        response.PointOfSale.WebShareToken = response.InternationalPointofSale.WebShareToken = string.Empty;
                    }
                }
                else
                {
                    //Fix for GetShopRequest-AwardTravel_EmptyMpNumber. HashPin check for AwardTravel Shop & Reshop Request (JIRA - ILE-6701) - Shashank
                    if (request.AwardTravel == true && _configuration.GetValue<bool>("ForAwardTravelShopRequestWithEmptyOrInvalidMpNo")
                        && GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("AndroidAwardShopHashPinSupportedVersion"), _configuration.GetValue<string>("iPhoneAwardShopHashPinSupportedVersion"))
                        && (string.IsNullOrEmpty(request.MileagePlusAccountNumber) || string.IsNullOrEmpty(request.HashPinCode) || !await _shoppingUtility.ValidateHashPinAndGetAuthToken(request.MileagePlusAccountNumber, request.HashPinCode, request.Application.Id, request.DeviceId, request.Application.Version.Major)))
                    {
                        throw new MOBUnitedException(_configuration.GetValue<string>("MPValidationErrorMessage").ToString());
                    }
                    if (_shoppingUtility.EnableAdvanceSearchCouponBooking(request) && !string.IsNullOrEmpty(request.PromotionCode))
                    {
                        if (request.Trips.Any(x => x != null && x.SearchNearbyDestinationAirports))
                            request.PromotionCode = string.Empty;

                        if (!request.GetFlightsWithStops && !request.Trips.Any(x => x != null && x.SearchNearbyDestinationAirports))
                        {
                            MOBPromoAlertMessage couponAlertMessages = new MOBPromoAlertMessage();
                            if (!request.GetFlightsWithStops && !request.Trips.Any(x => x != null && x.SearchNearbyDestinationAirports))
                            {
                                var tupleRes = await _shopBooking.ShopValidate(session.Token, request, couponAlertMessages);
                                couponAlertMessages = tupleRes.couponAlertMessages;
                                if (!tupleRes.Item1)
                                {
                                    if (couponAlertMessages != null && couponAlertMessages.AlertMessages != null && !string.IsNullOrEmpty(couponAlertMessages.PromoCode))
                                    {
                                        response.PromoAlertMessage = couponAlertMessages;
                                        throw new MOBUnitedException(_configuration.GetValue<string>("AdvanceSearchCouponErrorMessage"));
                                    }
                                }
                            }
                        }
                    }
                    _shopBooking.SetCurrentSession(session);

                    response.Availability = await _shopBooking.GetAvailability(session.Token, request, isFirstShopCall, _httpContext);

                    response.Disclaimer = GetDisclaimerString();

                    response.RefreshResultsData = string.Format("{0},{1},{2}", shop.Request.Trips[0].DepartDate, "", "");
                    shop = await _sessionHelperService.GetSession<ShoppingResponse>(session.SessionId, shop.ObjectName, new List<string> { request.SessionId, shop.ObjectName });
                    shop.Response = response;

                    if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "YES")
                    {
                        response.CartId = response.Availability.CartId;
                    }

                    await _sessionHelperService.SaveSession<ShoppingResponse>(shop, request.SessionId, new List<string> { request.SessionId, shop.ObjectName }, shop.ObjectName);
                    if (response.Availability != null && response.Availability.Trip != null && _configuration.GetValue<bool>("HideSearchFiletersAndSort"))
                    {
                        if (response.Availability.Trip.SearchFiltersIn != null) { response.Availability.Trip.SearchFiltersIn = null; }
                        if (response.Availability.Trip.SearchFiltersOut != null) { response.Availability.Trip.SearchFiltersOut = null; }
                    }

                    if (response.Availability != null && response.Availability.Trip != null && response.Availability.Trip.FlattenedFlights != null)
                    {
                        // mREST ALSO COMMENTED
                        //if (!_configuration.GetValue<bool>("EnableNonStopFlight") 
                        //    || (request.GetNonStopFlightsOnly && !response.Availability.Trip.TripHasNonStopflightsOnly) // Means First Shop Call and No Non Stop Flights Found in that Market
                        //    || (!request.GetNonStopFlightsOnly && !request.GetFlightsWithStops) //(!request.GetNonStopFlightsOnly && !request.GetFlightsWithStops) means old Clients
                        //    || (request.GetFlightsWithStops && response.Availability.Trip.PageCount == 2)) 
                        if (!_configuration.GetValue<bool>("EnableNonStopFlight") || response.Availability.Trip.PageCount == 2) //**PageCount==2==>> for paging implementation to send only 15 flights back to client and perisit remaining flights and return remaining when customer page up
                        {
                            //TODO - null out flattenedflights - mREST ALSO COMMENTED
                            //response.Availability.Trip.FlightSections = shopping.PopulateFlightSections(session.SessionId, response.Availability.Trip.FlattenedFlights);
                            response.Availability.Trip.FlightSections = null;
                            // Implemented the Paging to return only the number scecified at    <add key="OrgarnizeResultsRequestPageSize" value="5" />
                            List<MOBSHOPFlattenedFlight> flattenedFlights = response.Availability.Trip.FlattenedFlights.Clone();
                            int organizeResultsPageSize = Convert.ToInt32(_configuration.GetValue<string>("OrgarnizeResultsRequestPageSize").ToString());
                            if (flattenedFlights.Count > organizeResultsPageSize)
                            {
                                response.Availability.Trip.FlattenedFlights = flattenedFlights.GetRange(0, organizeResultsPageSize);
                            }
                            response.Availability.Trip.FlightCount = flattenedFlights.Count;
                            response.Availability.Trip.TotalFlightCount = flattenedFlights.Count;
                        }

                        if (_configuration.GetValue<bool>("EnableNonStopFlight") && request.GetFlightsWithStops && response.Availability.Trip.PageCount == 2)
                        {
                            response.Availability.Trip.FlightCount = response.Availability.Trip.FlightCount + nonStopFlightsCount; // To retrn total flights count both Non Stop from First Shop() call and Stops Flight Count from Second Shop Call()
                            response.Availability.Trip.TotalFlightCount = response.Availability.Trip.FlightCount;
                        }
                    }
                    else
                    {
                        if (response.Availability == null || response.Availability.FSRAlertMessages == null)
                        {
                            throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError"));
                        }
                    }

                    response.IsRevenueLowestPriceForAwardSearchEnabled = ShopStaticUtility.CheckRevenueLowestPriceForAwardSearchEnabled(request, response);
                    if (request.Application.Id == 2 && !request.IsReshop && (request.IsRevShopCallFromAwardFSR1 || _configuration.GetValue<bool>("HideBackButtonOnFSRForAndriodAllFlows"))) // To hide back button for all Search Types at Andriod FSR add this flag "HideBackButtonOnFSRForAndriodAllFlows" to config as true
                    {
                        response.HideBackButtonOnFSRForAndroid = true;
                    }
                                                                                                             //****Get Call Duration Code - Venkat 03/17/2015*******
                }
                if (_shoppingUtility.IsEnableMoneyPlusMilesFeature(request.Application.Id, request.Application.Version.Minor, session?.CatalogItems))
                {
                    response.IsFSRMoneyPlusMilesEligible = ShopStaticUtility.ValidateMoneyPlusMilesEligiblity(request, response.Availability);
                    session.IsEligibleForFSRMoneyPlusMiles = response.IsFSRMoneyPlusMilesEligible;
                    if (await _featureSettings.GetFeatureSettingValue("EnableFSRNewBadgeFeature") && response.IsFSRMoneyPlusMilesEligible)
                    {
                        if (response.Availability.FsrBadges == null)
                        {
                            response.Availability.FsrBadges = new List<MOBStyledText>();
                        }
                        MOBStyledText newStyleColor = new MOBStyledText();
                        newStyleColor.Text = _configuration.GetValue<string>("FSRMoneyPlusMilesNewBadgeText");
                        newStyleColor.BackgroundColor = MOBStyledColor.AlertGreen.GetDescription();
                        newStyleColor.TextColor = MOBStyledColor.White.GetDescription();
                        newStyleColor.Key = "MPMNEWBADGE";
                        response.Availability.FsrBadges.Add(newStyleColor);
                    }

                    //await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("Shop Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                //_cacheLog.LogWarning("Shop Error {@UnitedException}",JsonConvert.SerializeObject(uaex));
                //Calling regular shop call, if non-stop returned notflight found error code
                if (_configuration.GetValue<bool>("EnableNonStopFlight") && request.GetNonStopFlightsOnly && !request.GetFlightsWithStops && uaex.Message == "10038")
                {
                    request.GetNonStopFlightsOnly = false;
                    request.GetFlightsWithStops = false;
                    return await GetShop(request, _httpContext);
                }
           
                    response.Exception = new MOBException
                {
                    Message = uaex.Message
                };
                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = uaex.Code;
                }

                //TODO-add uaex.Code=="500"
                //assigning right exception, if stop flights call returned no flights found in 2'nd shop call
                if (_configuration.GetValue<bool>("EnableNonStopFlight") && request.GetFlightsWithStops  )
                {
                    if(_configuration.GetValue<bool>("EnableNonStopFlightItaPerformance") == false &&  uaex.Message == "10038")
                    {
                        response.NoFlightsWithStops = true;
                    }
                    else if(uaex.Code == "10038")
                    {
                        response.NoFlightsWithStops = true;
                        response.Exception.Message = _configuration.GetValue<string>("NoAvailabilityError2.0");
                    }
                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Shop Error {@Exception}", JsonConvert.SerializeObject(ex));
                //_cacheLog.LogError("Shop Error {@Exception}", JsonConvert.SerializeObject(ex));
                string[] messages = ex.Message.Split('#');

                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    if ((_configuration.GetValue<string>("Environment - ReShoppingPNRCall") == "STAGE") && messages.ToList().Count > 1)
                    {
                        response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage") + "CartId " + messages[1].ToString());
                    }
                    else
                    {
                        response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                    }
                }
                else
                {
                    response.Exception = new MOBException("9999", messages[0]);
                }
            }
            try
            {
                string callDuration = string.Empty;
                if (response != null && response.Availability != null && response.Availability.Trip != null &&
                    !string.IsNullOrEmpty(response.Availability.Trip.CallDurationText))
                {
                    callDuration = response.Availability.Trip.CallDurationText;
                }

                if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "YES" || (_configuration.GetValue<string>("DeviceIDSToReturnShopCallDurations") != null && _configuration.GetValue<string>("DeviceIDSToReturnShopCallDurations").ToString().ToUpper().Trim().Split('|').Contains(request.DeviceId.ToUpper().Trim())))//request.DeviceId.Trim().ToUpper() == "THIS_IS_FOR_TEST_" + DateTime.Now.ToString("MMM-dd-yyyy").ToUpper().Trim())
                {
                    //response.CartId = "<ul><li>" + response.CartId + " </li> </li> Total Shop Call Duration = " + response.CallDuration + "</li>" + response.Availability.Trip.CallDurationText.Replace("(", "<li>").Replace("(", "</li>") + "</ul>";
                    //response.Availability.Trip.FlattenedFlights[0].Flights[0].OperatingCarrierDescription = "<ul></li> Total Shop Call Duration = " + response.CallDuration + "</li>" + response.Availability.Trip.CallDurationText.Replace("(", "<li>").Replace("(", "</li>") + "</ul>";

                    response.CartId = response.CartId + cssCallDuration + callDuration + "|" + response.CallDuration;
                    if (response.Availability != null && response.Availability.Trip != null)
                    {
                        response.Availability.Trip.FlattenedFlights[0].Flights[0].OperatingCarrierDescription = "AWSCloudMigration - Cartid =" + response.CartId;
                    }
                }
                else if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "REST3_YES")
                {
                    response.CartId = response.ShopRequest.SessionId;
                }
                else if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "REST3_YES_1")
                {
                    response.CartId = response.Availability.CartId;
                    if (response.Availability != null && response.Availability.Trip != null)
                    {
                        response.Availability.Trip.FlattenedFlights[0].Flights[0].OperatingCarrierDescription = response.CartId;
                    }
                }
                if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "REST_TUNING_CALL_DURATION")
                {
                    response.CartId = "CSS = " + cssCallDuration2 + callDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString();
                }
                if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "REST_TUNING_CALL_DURATION_WITH_CARTID")
                {
                    response.CartId = "CSS = " + cssCallDuration2 + callDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString() + "|Cart ID = " + response.Availability.CartId;
                }
                if (_configuration.GetValue<bool>("Log_CSL_Call_Statistics"))
                {
                    if (response.Availability != null && response.Availability.Trip != null)
                    {
                        try
                        {
                            //Utility.AddCSLCallStatistics(request.Application.Id, request.Application.Version.Major, request.DeviceId, request.SessionId, response.Availability.Trip.Origin, response.Availability.Trip.Destination, response.Availability.Trip.DepartDate, request.SearchType, response.Availability.Trip.Cabin, request.FareType, request.AwardTravel, request.NumberOfAdults, request.MileagePlusAccountNumber, string.Empty);
                            //Utility.AddCSLCallStatisticsDetails(request.MileagePlusAccountNumber, "REST_Shopping", response.Availability.CartId, "CSS = " + cssCallDuration2 + callDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString(), "Shop", request.SessionId);
                            string recordLocator = request.RecordLocator != null ? request.RecordLocator : string.Empty;
                            string mileagePlusAccountNumber = request.MileagePlusAccountNumber;
                            if (!_configuration.GetValue<bool>("DisableAppendingCartIdWithMileaguePlusNumber"))
                            {

                                mileagePlusAccountNumber = mileagePlusAccountNumber + "_" + response.Availability.CartId;
                            }

                            timedOperationMessage = string.Format("Total time taken for {0} CSLStatistics call", string.IsNullOrEmpty(request.SessionId) || request.IsReshopChange ? "Shop 1" : "Shop 2");
                            CSLStatistics cSLStatistics = new CSLStatistics(_logger, _configuration, _dynamoDBService, _cSLStatisticsService);
                            using (timer = _logger.BeginTimedOperation(timedOperationMessage, transationId: request.TransactionId))
                            {
                               // cSLStatistics.AddCSLStatistics(request.Application.Id, request.Application.Version.Major, request.DeviceId, response.ShopRequest.SessionId, response.Availability.Trip.Origin, response.Availability.Trip.Destination, response.Availability.Trip.DepartDate, request.SearchType, response.Availability.Trip.Cabin, request.FareType, request.AwardTravel, request.NumberOfAdults, mileagePlusAccountNumber, recordLocator, "REST_Shopping", response.Availability.CartId, "Server:" + response.MachineName + "||CSS = " + cssCallDuration2 + callDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString(), "Shopping/Shop");
                            }
                        }
                        catch { }
                    }
                }
            }
            catch { }
            if (response?.ShopRequest?.Trips != null && response.ShopRequest.Trips.Any())
            {
                response.ShopRequest.Trips.ForEach(s => { s.OriginAllAirports = s.OriginAllAirports == -1 ? 0 : s.OriginAllAirports; s.DestinationAllAirports = s.DestinationAllAirports == -1 ? 0 : s.DestinationAllAirports; });
            }
            if (response?.Availability?.FSRAlertMessages != null && response.Availability.FSRAlertMessages.Any())
            {
                response.Availability?.FSRAlertMessages.ForEach(s => s?.Buttons?.ForEach(z => z.UpdatedShopRequest?.Trips?.ForEach(q => q.OriginAllAirports = q.OriginAllAirports == -1 ? 0 : q.OriginAllAirports)));
            }

            // Save shop request for edit search booking flow //check if needed to add error condition for this to not save request
            if (!request.GetFlightsWithStops && !string.IsNullOrEmpty(request.SessionId) && (IsEnableEditSearchOnFSRHeaderBooking || (request.IsCorporateBooking && _shoppingUtility.IsEnableU4BCorporateBooking(request.Application.Id, request.Application.Version.Major, request.CatalogItems))))
            {
                await _sessionHelperService.SaveSession<MOBSHOPShopRequest>(request, request.SessionId, new List<string> { request.SessionId, (new MOBSHOPShopRequest()).ObjectName }, (new MOBSHOPShopRequest()).ObjectName);
            }
            if (!string.IsNullOrEmpty(request?.PromotionCode) && response?.PromoAlertMessage != null && !string.IsNullOrEmpty(response?.PromoAlertMessage?.PromoCode) && response?.ShopRequest != null)
            {
                response.ShopRequest.Flow = Utility.Enum.FlowType.BOOKING.ToString();
            }
            return await Task.FromResult(response);
        }

        private bool IsTripPlanFlow(string travelType)
        {
            return travelType == TravelType.TPBooking.ToString() || travelType == TravelType.TPEdit.ToString() || travelType == TravelType.TPSearch.ToString();
        }
        private void DisableSearchNearByAirportsTripPlan(MOBSHOPShopRequest request)
        {
            if (_shoppingUtility.DisableFSRAlertMessageTripPlan(request.Application.Id, request.Application.Version.Major, request.TravelType))
            {
                request?.Trips?.ForEach(t =>
                {
                    if (t != null)
                    {
                        string[] lstAirports = _configuration.GetValue<string>("DecodeAirportListTripPlan").Split('~'); //NYC|EWR~DAL|DFW~MDW|ORD
                        lstAirports?.ForEach(l =>
                        {
                            if (l != null)
                            {
                                if (l.Split('|')[0].ToUpper() == t.Origin?.ToUpper()) //NYC|EWR
                                {
                                    t.Origin = l.Split('|')[1];
                                }

                                if (l.Split('|')[0].ToUpper() == t.Destination?.ToUpper())
                                {
                                    t.Destination = l.Split('|')[1];
                                }
                            }
                        });

                        t.SearchNearbyDestinationAirports = false;
                        t.SearchNearbyOriginAirports = false;
                        t.OriginAllAirports = 0;
                        t.DestinationAllAirports = 0;
                    }
                }
                );
            }
        }

        private void ValidateTravelerTypeForCLB(MOBSHOPShopRequest request)
        {
            if (_configuration.GetValue<bool>("EnableCorporateLeisure") && request.TravelType == TravelType.CLB.ToString())
            {
                if (request.TravelerTypes != null
                    && !request.TravelerTypes.Any(t => (t.TravelerType.ToUpper().Equals("SENIOR") || t.TravelerType.ToUpper().Equals("ADULT")) && t.Count > 0))
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("InvalidTravelerTypeMessageforCorpLeisure").ToString());
                }
            }
        }

        private void SetIsBEFareDisplayAtFSR(MOBSHOPShopRequest request)
        {
            bool IsBEFareDisplayAtFSRFlag = _configuration.GetValue<bool>("IsBEFareDisplayAtFSRFlag" ?? "false");
            request.IsELFFareDisplayAtFSR = request.IsELFFareDisplayAtFSR &&
                                           IsBEFareDisplayAtFSRFlag &&
                                           !request.AwardTravel;

            if (!request.IsELFFareDisplayAtFSR && !request.AwardTravel)
            {
                ELFFareDisplayVersionCheck(request);
            }
        }

        private void ELFFareDisplayVersionCheck(MOBSHOPShopRequest request)
        {
            var androidnontfaversion = "AndroidElfFareDisplayVersion";
            var iphonenontfaversion = "iPhoneElfFareDisplayVersion";
            var windowsnontfaversion = "WindowsElfFareDisplayVersion";
            var mWebNonELFVersion = "MWebNonElfFareDisplayVersion";
            bool ValidBAFareCheckVersion = false;
            ValidBAFareCheckVersion = GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, androidnontfaversion, iphonenontfaversion, windowsnontfaversion, mWebNonELFVersion, ValidBAFareCheckVersion, _configuration);
            request.IsELFFareDisplayAtFSR = (!ValidBAFareCheckVersion || (ValidBAFareCheckVersion && request.IsELFFareDisplayAtFSR));
        }

        private List<string> GetDisclaimerString()
        {
            List<string> disclaimer = new List<string>();

            if (_configuration.GetValue<string>("MakeReservationDisclaimer") != null)
            {
                disclaimer.Add(_configuration.GetValue<string>("MakeReservationDisclaimer").ToString());
            }
            else
            {
                disclaimer.Add("*Miles shown are the actual miles flown for this segment.Mileage accrued will vary depending on the terms and conditions of your frequent flyer program.");
            }
            return disclaimer;
        }

        public async Task<TripShareV2Response> GetShopRequest(ShareTripRequest request)
        {
            string actionName = "GetShopRequest";
            TripShareV2Response response = new TripShareV2Response();
            MOBShoppingCart shoppingCart = null;

            if (!string.IsNullOrEmpty(request.SessionId))
            {
                var session = await _sessionHelperService.GetSession<Session>(request.SessionId, new Session().ObjectName, new List<string> { request.SessionId, new Session().ObjectName });
                //load from from persist
                // var cslShopRequestFromSession = FilePersist.Load<ShopRequest>(request.SessionId, typeof(ShopRequest).FullName);
                var cslShopRequestFromSession = await _sessionHelperService.GetSession<United.Services.FlightShopping.Common.ShopRequest>(request.SessionId, new United.Services.FlightShopping.Common.ShopRequest().GetType().FullName, new List<string> { request.SessionId, new United.Services.FlightShopping.Common.ShopRequest().GetType().FullName });
                if (await _featureSettings.GetFeatureSettingValue("EnableAddTravelers").ConfigureAwait(false) && request.ModifyReservationFlow)
                {
                    // load shoppingcart object for promo code details
                    shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(request.SessionId, new MOBShoppingCart().ObjectName, new List<string> { request.SessionId, new MOBShoppingCart().ObjectName }).ConfigureAwait(false);

                    if (cslShopRequestFromSession == null)
                    {
                        var loadCslShopRequest = await _sessionHelperService.GetSession<CSLShopRequest>(request.SessionId, new CSLShopRequest().ObjectName, new List<string> { request.SessionId, new CSLShopRequest().ObjectName });
                        cslShopRequestFromSession = loadCslShopRequest.ShopRequest;
                    }
                }

                if (cslShopRequestFromSession == null)
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("BookingSessionExpiredMessage") ?? _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                if (cslShopRequestFromSession != null)
                {
                    //build MobShop request
                    response.ShopRequest = ShopStaticUtility.GetMobShopRequest(cslShopRequestFromSession, request, request.ModifyReservationFlow, actionName, session, shoppingCart);
                }
            }
            response.TransactionId = request.TransactionId;

            return response;
        }

        public async Task<ShopOrganizeResultsResponse> OrganizeShopResults(ShopOrganizeResultsReqeust request)
        {
            ShopOrganizeResultsResponse response = new ShopOrganizeResultsResponse();
            bool ShowEditSearchHeaderOnFSRBooking = false;
            bool isRemoveAppliedWheelChairFilter = false;

            try
            {
                Session session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId);
                if (String.IsNullOrEmpty(request.SessionId))
                {
                    request.SessionId = session.SessionId;
                }

                ShowEditSearchHeaderOnFSRBooking = session != null ? session.ShowEditSearchHeaderOnFSRBooking : false;

                response.TransactionId = request.TransactionId;
                response.LanguageCode = request.LanguageCode;
                response.OrganizeResultsRequest = request;

                if (await _featureToggles.IsEnableWheelchairFilterOnFSR(request.Application.Id, request.Application.Version.Major, session.CatalogItems).ConfigureAwait(false) && request.SearchFiltersIn?.WheelchairFilter != null && request.SearchFiltersIn?.WheelchairFilterContent != null || (session.IsReshopChange && await _featureToggles.IsEnableReshopWheelchairFilterOnFSR(request.Application.Id, request.Application.Version.Major, session.CatalogItems).ConfigureAwait(false)))
                {
                    try
                    {
                        var searchFilers = await _sessionHelperService.GetSession<MOBSearchFilters>(session.SessionId, new MOBSearchFilters().GetType().FullName, new List<string> { session.SessionId, new MOBSearchFilters().GetType().FullName }).ConfigureAwait(false);
                        isRemoveAppliedWheelChairFilter = searchFilers?.WheelchairFilterContent?.DimensionInfo != null && request?.SearchFiltersIn?.WheelchairFilterContent?.DimensionInfo == null;

                        await _sessionHelperService.SaveSession<MOBSearchFilters>(request.SearchFiltersIn, session.SessionId, new List<string> { session.SessionId, request.SearchFiltersIn.GetType().FullName }, request.SearchFiltersIn.GetType().FullName).ConfigureAwait(false);
                    }
                    catch (Exception) { }
                }
                if (_configuration.GetValue<bool>("EnableAndroidFixForPagination") && request.Application.Id == 2 && request.LastTripIndexRequested > 1 && !GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("AndroidFixForPaginationVersion"), "") && request.SearchFiltersIn == null)
                {
                    SelectTrip selectTrip = new SelectTrip();
                    var selectTripFromPersist = await _sessionHelperService.GetSession<SelectTrip>(request.SessionId, selectTrip.ObjectName);
                    if (selectTripFromPersist != null)
                    {
                        var searchFilterInRequest = selectTripFromPersist.Responses?.ElementAt(request.LastTripIndexRequested - 2).Value?.Availability?.Trip?.SearchFiltersIn;
                        if (searchFilterInRequest != null)
                        {
                            searchFilterInRequest.PageNumber = 2;
                            request.SearchFiltersIn = searchFilterInRequest;
                        }
                    }
                }

                if (request.SearchFiltersIn.PageNumber == 2 && _configuration.GetValue<bool>("ReturnAllRemainingShopFlightsWithOnly2PageCount"))
                {
                    if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == MOBTripPlannerType.TPSearch.ToString() && request.LastTripIndexRequested > 1)
                    {

                        var selectTripResponeTripPlanner = await _sessionHelperService.GetSession<CSLSelectTrip>(request.CartId, new CSLSelectTrip().ObjectName, new List<string> { request.CartId, new CSLSelectTrip().ObjectName });

                        response.Availability = selectTripResponeTripPlanner.MobSelectTripResponse.Availability;
                    }
                    else
                    {
                        response.Availability = await _shopBooking.GetLastTripAvailabilityFromPersist(request.LastTripIndexRequested, session.SessionId);
                    }

                    List<MOBSHOPFlattenedFlight> flattenedFlights = response.Availability.Trip.FlattenedFlights.Clone();
                    int defaultReturnedFlightCount = _configuration.GetValue<int>("OrgarnizeResultsRequestPageSize");
                    if (flattenedFlights.Count > defaultReturnedFlightCount)
                    {
                        response.Availability.Trip.FlattenedFlights = flattenedFlights.GetRange(defaultReturnedFlightCount, flattenedFlights.Count - defaultReturnedFlightCount);
                    }
                }
                else
                {
                    response.Availability = await _shopBooking.FilterShopSearchResults(request, session, isRemoveAppliedWheelChairFilter);
                }
                response.Disclaimer = GetDisclaimerString();
                       
                if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "YES")
                {
                    response.CartId = response.Availability.CartId;
                }


                if (response.Availability != null && response.Availability.Trip != null && response.Availability.Trip.FlattenedFlights != null && response.Availability.Trip.FlattenedFlights.Count > 0)
                {
                    //TODO - null out flattenedflights
                    //response.Availability.Trip.FlightSections = _shopBooking.PopulateFlightSections(session.SessionId, response.Availability.Trip.FlattenedFlights);
                    response.Availability.Trip.FlightSections = null;
                    response.Availability.Trip.SearchFiltersIn = request.SearchFiltersIn;
                    //response.Availability.Trip.FlightCount = response.Availability.Trip.FlattenedFlights.Count;
                    if (request.SearchFiltersIn.PageNumber == 1)
                    {
                        List<MOBSHOPFlattenedFlight> flattenedFlights = response.Availability.Trip.FlattenedFlights.Clone();
                        int organizeResultsPageSize = Convert.ToInt32(_configuration.GetValue<string>("OrgarnizeResultsRequestPageSize").ToString());
                        if (flattenedFlights.Count > organizeResultsPageSize)
                        {
                            response.Availability.Trip.FlattenedFlights = flattenedFlights.GetRange(0, organizeResultsPageSize);
                        }
                        response.Availability.Trip.FlightCount = flattenedFlights.Count;
                        if (response.Availability.Trip.SearchFiltersIn != null)
                        {
                            response.Availability.Trip.SearchFiltersIn.FilterSortPaging = true;
                        }
                        if (response.Availability.Trip.SearchFiltersOut != null)
                        {
                            response.Availability.Trip.SearchFiltersOut.FilterSortPaging = true;
                        }
                    }

                }
                else
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("NoOrganizeShopResultsError"));
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("OrganizeShopResults Error {@UnitedException}", JsonConvert.SerializeObject(uaex));
                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = uaex.Code;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("OrganizeShopResults Error {@Exception}", JsonConvert.SerializeObject(ex));
                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }

            if (response.Availability?.ResponseType == "")
                response.Availability.ResponseType = MOBAvailabiltyResponseType.Default.GetDescription();

            if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "YES" || request.DeviceId.Trim().ToUpper() == "THIS_IS_FOR_TEST_" + DateTime.Now.ToString("MMM-dd-yyyy").ToUpper().Trim())
            {
                response.CartId = response.CartId + "| Call Duration = " + response.CallDuration;
            }
            if (_configuration.GetValue<bool>("Log_CSL_Call_Statistics"))
            {
                try
                {
                    string callDurations = response.CallDuration.ToString();//TODO _shoppingUtility.GetCSLCallDuration();
                    var cslStatics = (new CSLStatistics(_logger, _configuration, _dynamoDBService, _cSLStatisticsService));
                    await cslStatics.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", response.CartId, callDurations, "Shopping/OrganizeResults", request.SessionId);
                }
                catch { }
            }

            if (ShowEditSearchHeaderOnFSRBooking && response != null)
            {
                MOBSHOPShopRequest shopRequest = new MOBSHOPShopRequest();
                shopRequest = await _sessionHelperService.GetSession<MOBSHOPShopRequest>(request?.SessionId, shopRequest.ObjectName, new List<string> { request?.SessionId, shopRequest.ObjectName });
                response.ShopRequest = shopRequest;
            }

            return await Task.FromResult(response);
        }

        public async Task<ShopResponse> ShopCLBOptOut(CLBOptOutRequest request)
        {
            ShopResponse response = new ShopResponse();
            MOBSHOPShopRequest shopRequest = null;
            Session session = null;

            if (!string.IsNullOrEmpty(request.SessionId))
            {
                session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId);
            }
            if (session != null)
            {
                ShoppingResponse persistShop = new ShoppingResponse();
                persistShop = await _sessionHelperService.GetSession<ShoppingResponse>(request.SessionId, persistShop.ObjectName, new List<string> { request.SessionId, persistShop.ObjectName });

                shopRequest = new MOBSHOPShopRequest();
                shopRequest = _shopBooking.ConvertCorporateLeisureToRevenueRequest(persistShop.Request);
                return await GetShop(shopRequest, _httpContext);
            }
            if (response?.ShopRequest?.Trips != null && response.ShopRequest.Trips.Any())
            {
                response.ShopRequest.Trips.ForEach(s => { s.OriginAllAirports = s.OriginAllAirports == -1 ? 0 : s.OriginAllAirports; s.DestinationAllAirports = s.DestinationAllAirports == 0 ? -1 : s.DestinationAllAirports; });
            }

            return response;
        }

        #region SelectTrip
        public async Task<SelectTripResponse> SelectTrip(SelectTripRequest selectTripRequest, HttpContext httpContext=null)
        {
            _httpContext = httpContext;
            SelectTripResponse response = new SelectTripResponse();
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            response.Request = selectTripRequest;
            string logAction = "SelectTrip";
            bool isDefault = false;
            Session session = null;

            var version1 = _configuration.GetValue<string>("AndriodCFOP_Booking_Reshop_PostbookingAppVersion");
            var version2 = _configuration.GetValue<string>("IphoneCFOP_Booking_Reshop_PostbookingAppVersion");
            bool isCFOPVersionCheck = GeneralHelper.IsApplicationVersionGreaterorEqual(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, version1, version2);
            try
            {
                session = await _shoppingSessionHelper.GetBookingFlowSession(selectTripRequest.SessionId);
                logAction = session.IsReshopChange ? "ReShop - SelectTrip" : "SelectTrip";
                if (!session.IsReshopChange)
                {
                    //Making reservation object as null to handle the scenario when the user clicks back button after the last SelectTrip call is made.
                    Reservation bookingPathRes = new Reservation();
                    if (!_configuration.GetValue<bool>("DisableSaveSessionDataNullFix"))
                    {
                        await _sessionHelperService.SaveSession<Reservation>(null, selectTripRequest.SessionId, new List<string> { selectTripRequest.SessionId, bookingPathRes.ObjectName }, bookingPathRes.ObjectName, 5400, false);
                    }
                    else
                    {
                        await _sessionHelperService.SaveSession<Reservation>(null, selectTripRequest.SessionId, new List<string> { selectTripRequest.SessionId, bookingPathRes.ObjectName }, bookingPathRes.ObjectName, 5400, true);
                    }
                }
                if (await _featureSettings.GetFeatureSettingValue("EnableFSRETCTravelCreditsFeature").ConfigureAwait(false))
                {
                    session.PricingType = selectTripRequest.PricingType;
                }
                //No Need to add all CatalogItems on SelectTrip only add new catalogs
                if (selectTripRequest.CatalogItems != null && selectTripRequest.CatalogItems.Count > 0)
                {
                    if (session.CatalogItems == null) {
                        session.CatalogItems = new List<MOBItem>();
                    }
                    session.CatalogItems.AddRange(selectTripRequest.CatalogItems);
                    //United.Persist.FilePersist.Save<United.Persist.Definition.Shopping.Session>(session.SessionId, session.ObjectName, session);
                    await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName);
                }               
                if (_shoppingUtility.IsEnableMoneyPlusMilesFeature(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Minor, session?.CatalogItems))
                {
                    session.IsMoneyPlusMilesSelected = selectTripRequest.IsMoneyPlusMiles;
                    if(session.MileagPlusNumber?.ToUpper().Trim() != selectTripRequest.MileagePlusAccountNumber?.ToUpper().Trim())
                    {
                        session.MileagPlusNumber = selectTripRequest.MileagePlusAccountNumber;
                    }
                    await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);
                }

                United.Mobile.Model.Shopping.SelectTrip selectTrip = new United.Mobile.Model.Shopping.SelectTrip();
                try
                {
                    selectTrip = await _sessionHelperService.GetSession<SelectTrip>(selectTripRequest.SessionId, selectTrip.ObjectName, new List<string> { selectTripRequest.SessionId, selectTrip.ObjectName });
                }
                catch (System.Exception ex)
                {
                    #region
                    ExceptionWrapper exceptionWrapper = new ExceptionWrapper(ex);
                    //shopping.//logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, logAction, "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, exceptionWrapper, true, false));
                    _logger.LogError("SelectTripGetSession {@Exception}", JsonConvert.SerializeObject(ex));
                    #endregion
                }
                if (IsLatestClientAppVerWithSelectTripBackButtonFix(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                {
                    #region Verify the Select Trip Persist if its a Back Button Click on Client - Index out of Range Fix - Venkat
                    if (selectTrip != null && selectTripRequest.BackButtonClick)
                    {
                        #region
                        selectTrip.Requests = ShopStaticUtility.RebuildSelectTripRequestDictionary2(selectTrip.Requests.Count - 1, selectTrip.Requests); // Get all  Select Trip Requests of Previous Trips in Back Button Client Click
                        selectTrip.Responses = ShopStaticUtility.RebuildSelectTripResponseDictionary2(selectTrip.Responses.Count - 1, selectTrip.Responses); // Get all Select Trip Responses of Previous Trips in Back Button Client Click
                        SelectTripRequest prevSelectedTripRequest = ShopStaticUtility.GetLastSelectedTrip_TripID(selectTrip.Requests); // Get the Last Select Trip Request ID in Back Button Click
                        // Get Last Select Trip Trip Key
                        // Added By Ali as part of Tech Task 264624:Booking flow exception analysis - Get Select Trip - System.NullReference Exception
                        if ((!_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis")) || (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && prevSelectedTripRequest != null))
                        {
                            selectTrip.LastSelectTripKey = prevSelectedTripRequest.TripId;
                            session.LastSelectTripKey = prevSelectedTripRequest.TripId;
                        }
                        // Get Last Select Trip Trip Key
                        // Save to Persist the Select Trip Requets and Responses after we got only 
                        await _sessionHelperService.SaveSession<SelectTrip>(selectTrip, session.SessionId, new List<string> { session.SessionId, selectTrip.ObjectName }, selectTrip.ObjectName);
                        await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName);
                        #endregion
                    }
                    #endregion
                }
                await AwardMTThrowErrorIfCurrentTripDepartDateGreaterThanNextTripDepartDate(selectTripRequest, selectTrip, session);
                #region Call to DAL
                int nonStopFlightsCount = 0;

                response.TransactionId = selectTripRequest.TransactionId;
                response.LanguageCode = selectTripRequest.LanguageCode;

                ShoppingResponse shop = new ShoppingResponse();
                try
                {
                    shop = await _sessionHelperService.GetSession<ShoppingResponse>(session.SessionId, shop.ObjectName, new List<string> { session.SessionId, shop.ObjectName });
                }
                catch (System.Exception ex) { }

                int totalPassengers = 1;
                if (shop != null && shop.Request != null)
                {
                    if (_shoppingUtility.EnableTravelerTypes(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, session.IsReshopChange) && shop.Request.TravelerTypes != null && shop.Request.TravelerTypes.Count > 0)
                    {
                        totalPassengers = ShopStaticUtility.GetTravelerCount(shop.Request.TravelerTypes);
                    }
                    else
                    {
                        totalPassengers = shop.Request.NumberOfAdults + shop.Request.NumberOfSeniors + shop.Request.NumberOfChildren12To17 + shop.Request.NumberOfChildren5To11 + shop.Request.NumberOfChildren2To4 + shop.Request.NumberOfInfantWithSeat + shop.Request.NumberOfInfantOnLap;
                    }
                }
                //int tripCount = 1;
               
                MOBAdditionalItems additionalItems = new MOBAdditionalItems();
                response.Availability = await SelectTrip(selectTripRequest, totalPassengers, additionalItems);
                response.Disclaimer = GetDisclaimerString();

                if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "YES")
                    response.CartId = response.Availability.CartId;
                #endregion

                if (IsLatestClientAppVerWithSelectTripBackButtonFix(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                {
                    #region Select Trip Persist Code
                    if (selectTrip == null)
                    {
                        selectTrip = new SelectTrip();
                    }

                    if (selectTrip.Requests == null || selectTrip.Responses == null)
                    {
                        selectTrip.Requests = new SerializableDictionary<string, SelectTripRequest>();
                        selectTrip.Responses = new SerializableDictionary<string, SelectTripResponse>();

                        //first flight so get refresh data from shop
                        response.RefreshResultsData = string.Format("{0},{1},{2}", shop.Request.Trips[0].DepartDate, "", "");
                    }
                    #region
                    //////string tripId = tripCount.ToString();
                    //if (selectTrip.Requests.ContainsKey(request.TripId))
                    //{
                    //    selectTrip.Requests[request.TripId] = request;
                    //    selectTrip.Responses[request.TripId] = response;

                    //    // get info for refresh results data
                    //    if (selectTrip.Requests.Count > 0)
                    //    {
                    //        response.RefreshResultsData = string.Format("{0},{1},{2}", shop.Request.Trips[selectTrip.Requests.Count - 1].DepartDate, request.TripId, request.ProductId);
                    //    }

                    //    //need to clear out all requests after this one as it's a reshop
                    //    selectTrip.Requests = _shoppingUtility.RebuildSelectTripRequestDictionary(request.TripId, selectTrip.Requests);
                    //    selectTrip.Responses = _shoppingUtility.RebuildSelectTripResponseDictionary(request.TripId, selectTrip.Responses);
                    //}
                    #endregion
                    if (!selectTripRequest.BackButtonClick && selectTripRequest.ISProductSelected)
                    {
                        #region Mobile EXC: Added the below if condition to check the duplicate value in selectTrip dictionary object to avoid the System.ArgumentException(An item with the same key has already been added Exception). - Srinivas
                        if (selectTrip.Requests.ContainsKey(selectTripRequest.TripId))
                        {
                            selectTrip.Requests[selectTripRequest.TripId] = selectTripRequest;
                            selectTrip.Responses[selectTripRequest.TripId] = response;
                        }
                        else
                        {
                            selectTrip.Requests.Add(selectTripRequest.TripId, selectTripRequest);
                            selectTrip.Responses.Add(selectTripRequest.TripId, response);
                        }
                        selectTrip.LastSelectTripKey = selectTripRequest.TripId;
                        session.LastSelectTripKey = selectTrip.LastSelectTripKey;
                        #endregion
                    }
                    try
                    {
                        if (selectTrip != null && selectTrip.Requests != null && selectTrip.Requests.Count > 0
                            && shop.Request != null && shop.Request.Trips != null && shop.Request.Trips.Count >= selectTrip.Requests.Count)
                        {
                            response.RefreshResultsData = string.Format("{0},{1},{2}", shop.Request.Trips[selectTrip.Requests.Count - 1].DepartDate, selectTripRequest.TripId, selectTripRequest.ProductId);
                        }
                    }
                    catch (Exception ex) { } // Why around try catch and not capturing the exception is need to check why client needs this data RefreshResultsData

                    await _sessionHelperService.SaveSession<SelectTrip>(selectTrip, selectTripRequest.SessionId, new List<string> { selectTripRequest.SessionId, selectTrip.ObjectName }, selectTrip.ObjectName);
                    await _sessionHelperService.SaveSession<Session>(session, selectTripRequest.SessionId, new List<string> { selectTripRequest.SessionId, session.ObjectName }, session.ObjectName);
                    #endregion
                }
                else
                {
                    #region Existing Code before Index out of Range Exception
                    if (selectTrip == null)
                    {
                        selectTrip = new SelectTrip();
                    }

                    if (selectTrip.Requests == null || selectTrip.Responses == null)
                    {
                        selectTrip.Requests = new SerializableDictionary<string, SelectTripRequest>();
                        selectTrip.Responses = new SerializableDictionary<string, SelectTripResponse>();

                        //first flight so get refresh data from shop
                        response.RefreshResultsData = string.Format("{0},{1},{2}", shop.Request.Trips[0].DepartDate, "", "");
                    }
                    //string tripId = tripCount.ToString();
                    if (selectTrip.Requests.ContainsKey(selectTripRequest.TripId))
                    {
                        selectTrip.Requests[selectTripRequest.TripId] = selectTripRequest;
                        selectTrip.Responses[selectTripRequest.TripId] = response;

                        // get info for refresh results data
                        if (selectTrip.Requests.Count > 0)
                        {
                            string prevTripId = ShopStaticUtility.GetPreviousKey(selectTripRequest.TripId, selectTrip.Requests);
                            response.RefreshResultsData = string.Format("{0},{1},{2}", shop.Request.Trips[selectTrip.Requests.Count - 1].DepartDate, selectTripRequest.TripId, selectTripRequest.ProductId);
                        }

                        //need to clear out all requests after this one as it's a reshop
                        selectTrip.Requests = ShopStaticUtility.RebuildSelectTripRequestDictionary(selectTripRequest.TripId, selectTrip.Requests);
                        selectTrip.Responses = ShopStaticUtility.RebuildSelectTripResponseDictionary(selectTripRequest.TripId, selectTrip.Responses);
                    }
                    else
                    {
                        selectTrip.Requests.Add(selectTripRequest.TripId, selectTripRequest);
                        selectTrip.Responses.Add(selectTripRequest.TripId, response);

                        // get info for refresh results data
                        ////if (selectTrip.Requests.Count > 0)
                        ////{
                        ////    string prevTripId = _shoppingUtility.GetPreviousKey(request.TripId, selectTrip.Requests);
                        ////    response.RefreshResultsData = string.Format("{0},{1},{2}", shop.Request.Trips[selectTrip.Requests.Count - 1].DepartDate, request.TripId, request.ProductId);
                        ////}
                    }
                    selectTrip.LastSelectTripKey = selectTripRequest.TripId;
                    await _sessionHelperService.SaveSession<SelectTrip>(selectTrip, selectTripRequest.SessionId, new List<string> { selectTripRequest.SessionId, selectTrip.ObjectName }, selectTrip.ObjectName);

                    session.LastSelectTripKey = selectTrip.LastSelectTripKey;
                    await _sessionHelperService.SaveSession<Session>(session, selectTripRequest.SessionId, new List<string> { selectTripRequest.SessionId, session.ObjectName }, session.ObjectName);

                    #endregion
                }

                if (response.Availability != null && response.Availability.Trip != null && response.Availability.Trip.FlattenedFlights != null)
                {
                    //if (!_configuration.GetValue<bool>("EnableNonStopFlight")
                    //|| (request.GetNonStopFlightsOnly && !response.Availability.Trip.TripHasNonStopflightsOnly) // Means First Shop Call and No Non Stop Flights Found in that Market
                    //|| (!request.GetNonStopFlightsOnly && !request.GetFlightsWithStops)) //(!request.GetNonStopFlightsOnly && !request.GetFlightsWithStops) means old Clients
                    if (!_configuration.GetValue<bool>("EnableNonStopFlight") || response.Availability.Trip.PageCount == 2) //**PageCount==2==>> for paging implementation to send only 15 flights back to client and perisit remaining flights and return remaining when customer page up
                    {
                        //TODO - null out FlightSections
                        //response.Availability.Trip.FlightSections = shopping.PopulateFlightSections(request.SessionId, response.Availability.Trip.FlattenedFlights);
                        response.Availability.Trip.FlightSections = null;
                        List<Model.Shopping.MOBSHOPFlattenedFlight> flattenedFlights = response.Availability.Trip.FlattenedFlights.Clone();
                        int organizeResultsPageSize = Convert.ToInt32(_configuration.GetValue<string>("OrgarnizeResultsRequestPageSize"));
                        if (flattenedFlights.Count > organizeResultsPageSize)
                        {
                            response.Availability.Trip.FlattenedFlights = flattenedFlights.GetRange(0, organizeResultsPageSize);
                        }
                        response.Availability.Trip.FlightCount = flattenedFlights.Count;
                        response.Availability.Trip.TotalFlightCount = flattenedFlights.Count;
                    }
                    if (_configuration.GetValue<bool>("EnableNonStopFlight") && selectTripRequest.GetFlightsWithStops)
                    {
                        response.Availability.Trip.FlightCount = response.Availability.Trip.FlightCount + nonStopFlightsCount; // To retrn total flights count both Non Stop from First Shop() call and Stops Flight Count from Second Shop Call()
                        response.Availability.Trip.TotalFlightCount = response.Availability.Trip.FlightCount;
                    }
                    if (_shoppingUtility.IsEnableMoneyPlusMilesFeature(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Minor, session?.CatalogItems)
                        && additionalItems.MoneyPlusMilesPricing == false && selectTripRequest.IsMoneyPlusMiles == true)
                    {
                        response.Request.IsMoneyPlusMiles = false;
                        session.IsMoneyPlusMilesSelected = false;
                        await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);
                        response.Exception = new MOBException("7390", _configuration.GetValue<string>("FSRMoneyPlusMilesUnavailableMessage"));
                    }
                }
                // Fixed duplicate messages due to .net deserialization bugs
                if (response.Availability != null && response.Availability.Reservation != null && response.Availability.Reservation.Trips != null && response.Availability.Reservation.Trips.Count > 0)
                {
                    foreach (MOBSHOPTrip trip in response.Availability.Reservation.Trips)
                    {
                        if (trip.FlattenedFlights != null)
                        {
                            foreach (Model.Shopping.MOBSHOPFlattenedFlight ff in trip.FlattenedFlights)
                            {
                                if (ff.Flights != null)
                                {
                                    foreach (Model.Shopping.MOBSHOPFlight flight in ff.Flights)
                                    {
                                        if (flight.OnTimePerformance != null && flight.OnTimePerformance.OnTimeNotAvailableMessage != null && flight.OnTimePerformance.OnTimeNotAvailableMessage.Count > 1)
                                        {
                                            List<string> lstOnTimeNotAvailableMessage = new List<string>();
                                            lstOnTimeNotAvailableMessage.Add(flight.OnTimePerformance.OnTimeNotAvailableMessage[0].ToString());
                                            flight.OnTimePerformance.OnTimeNotAvailableMessage = lstOnTimeNotAvailableMessage;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                response.IsTokenAuthenticated = session.IsTokenAuthenticated;
                if (response.Availability != null && response.Availability.Trip != null && _configuration.GetValue<bool>("HideSearchFiletersAndSort"))
                {
                    if (response.Availability.Trip.SearchFiltersIn != null) { response.Availability.Trip.SearchFiltersIn = null; }
                    if (response.Availability.Trip.SearchFiltersOut != null) { response.Availability.Trip.SearchFiltersOut = null; }
                }

                if (response != null && response.Availability != null && response.Availability.Reservation != null && session != null)
                {
                    response.Availability.Reservation.IsEmp20 = string.IsNullOrEmpty(session.EmployeeId) ? false : true;
                }

                if (isCFOPVersionCheck)
                {
                    if (_configuration.GetValue<bool>("IsBookingCommonFOPEnabled") && session.IsReshopChange == false)
                    {
                        response.ShoppingCart = await _shoppingUtility.PopulateShoppingCart(shoppingCart, FlowType.BOOKING.ToString(), selectTripRequest.SessionId, response.Availability.CartId, selectTripRequest, response.Availability.Reservation);
                        response.Availability.Reservation.IsBookingCommonFOPEnabled = _configuration.GetValue<bool>("IsBookingCommonFOPEnabled");                       
                    }
                    else if (_configuration.GetValue<bool>("IsReshopCommonFOPEnabled") && session.IsReshopChange == true)
                    {
                        response.ShoppingCart = await _shoppingUtility.PopulateShoppingCart(shoppingCart, FlowType.RESHOP.ToString(), selectTripRequest.SessionId, response.Availability.CartId);
                        response.Availability.Reservation.IsReshopCommonFOPEnabled = _configuration.GetValue<bool>("IsReshopCommonFOPEnabled");
                        Reservation reshopPathRes = new Reservation();
                        reshopPathRes = await _sessionHelperService.GetSession<Reservation>(selectTripRequest.SessionId, reshopPathRes.ObjectName, new List<string> { selectTripRequest.SessionId, reshopPathRes.ObjectName });
                        reshopPathRes.IsReshopCommonFOPEnabled = true;
                        await _sessionHelperService.SaveSession<Reservation>(reshopPathRes, selectTripRequest.SessionId, new List<string> { selectTripRequest.SessionId, reshopPathRes.ObjectName }, reshopPathRes.ObjectName);
                    }
                    if (response.ShoppingCart != null && response.ShoppingCart.OmniCart?.OmniCartPricingInfos != null && !response.ShoppingCart.OmniCart.OmniCartPricingInfos.Any())
                    {
                        response.ShoppingCart.OmniCart.OmniCartPricingInfos = null;
                    }
                }
                if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI") && GeneralHelper.IsApplicationVersionGreater(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, "AndroidShareTripInSoftRTIVersion", "iPhoneShareTripInSoftRTIVersion", "", "", true, _configuration))
                {
                    if (response.ShoppingCart == null)
                        response.ShoppingCart = new MOBShoppingCart();

                    //response.ShoppingCart.TripShare = _shoppingUtility.GetTripShare(response, shopping.LogEntries, traceSwitch, flightShopping, "SelectTrip");
                    response.ShoppingCart.TripShare = _shoppingUtility.IsShareTripValid(response);
                }

                // Fix for MOBILE-25851 Farewheel not found exception : Issue is UI is taking the selectTripResponse.Request.Filters to form the GetFarewheelList filters
                if (_shoppingUtility.IsEnableSelectTripResponseRequestFromBackend(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, session?.CatalogItems))
                    response.Request.Filters = response?.Availability?.Trip?.SearchFiltersIn;
                
                if (_shoppingUtility.IsEnableMoneyPlusMilesFeature(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Minor, session?.CatalogItems))
                {
                    //response.IsFSRMoneyPlusMilesEligible = session.IsEligibleForFSRMoneyPlusMiles;
                    MOBSHOPShopRequest shopRequest = new MOBSHOPShopRequest();
                    shopRequest = await _sessionHelperService.GetSession<MOBSHOPShopRequest>(selectTripRequest?.SessionId, shopRequest.ObjectName, new List<string> { selectTripRequest?.SessionId, shopRequest.ObjectName }).ConfigureAwait(false);

                    if (await _featureSettings.GetFeatureSettingValue("EnableMoneyPlusMilesFastFollower").ConfigureAwait(false))
                    {
                        response.IsFSRMoneyPlusMilesEligible = selectTripRequest.IsMoneyPlusMiles && ShopStaticUtility.ValidateMoneyPlusMilesEligiblity(shopRequest, response.Availability);
                    }
                    else
                    {
                        response.IsFSRMoneyPlusMilesEligible = ShopStaticUtility.ValidateMoneyPlusMilesEligiblity(shopRequest, response.Availability);
                    }
                    if (response.IsFSRMoneyPlusMilesEligible)
                    {
                        await SetFSROnScreenAlerts(selectTripRequest, response, session, shopRequest).ConfigureAwait(false);
                        if (await _featureSettings.GetFeatureSettingValue("EnableFSRNewBadgeFeature"))
                        {
                            if (response.Availability.FsrBadges == null)
                            {
                                response.Availability.FsrBadges = new List<MOBStyledText>();
                            }
                            MOBStyledText newStyleColor = new MOBStyledText();
                            newStyleColor.Text = _configuration.GetValue<string>("FSRMoneyPlusMilesNewBadgeText");
                            newStyleColor.BackgroundColor = MOBStyledColor.AlertGreen.GetDescription();
                            newStyleColor.TextColor = MOBStyledColor.White.GetDescription();
                            newStyleColor.Key = "MPMNEWBADGE";
                            response.Availability.FsrBadges.Add(newStyleColor);
                        }
                    }

                }

            }
            catch (MOBUnitedException uaex)
            {
                #region
                ExceptionWrapper uaexWrapper = new ExceptionWrapper(uaex);
                _logger.LogWarning("SelectTrip MOBUnitedException {@UnitedException}", JsonConvert.SerializeObject(uaexWrapper));
                //shopping.//logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, logAction, "MOBUnitedException", request.Application.Id, request.Application.Version.Major, request.DeviceId, uaexWrapper, true, false));

                response.Exception = new MOBException();
                response.Exception.Message = uaex.Message;
                if (_configuration.GetValue<bool>("SessionExpiryMessageRedirectToHomerBookingMain"))
                {
                    response.Exception.Code = uaex.Code;
                }
                if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                {
                    if (uaex.InnerException != null && !string.IsNullOrEmpty(uaex.InnerException.Message) && uaex.InnerException.Message.Trim().Equals("10050"))
                    {
                        response.Exception.Code = "10050";
                    }
                }

                if (!_configuration.GetValue<bool>("DisableFSRAlertRefresh")
                   && (uaex.Message.Equals(_configuration.GetValue<string>("NoAvailabilityError2.0")) || uaex.Message.Equals(_configuration.GetValue<string>("BuyMilesPriceChangeError.0"))))
                {
                    // Refresh the FSR screen based on this code when this error occurs
                    response.Exception.Code = _configuration.GetValue<string>("ErrorCodeForFSRRefresh");
                }
                #endregion
            }
            catch (System.Exception ex)
            {
                #region
                ExceptionWrapper exceptionWrapper = new ExceptionWrapper(ex);
                _logger.LogError("SelectTrip Error {@Exception}", JsonConvert.SerializeObject(exceptionWrapper));
                //    shopping.//logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(request.SessionId, logAction, "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, exceptionWrapper, true, false));
                if (!Convert.ToBoolean(_configuration.GetValue<string>("SurfaceErrorToClient")))
                {
                    if (selectTripRequest.BackButtonClick && selectTripRequest.Application.Version.Major == "2.1.9I" && ex.Message.ToLower().Trim() == "Object reference not set to an instance of an object.".ToLower().Trim())
                    {
                        string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4iOS2.1.9ITouchIDBackButtonBug") != null ? _configuration.GetValue<string>("BookingExceptionMessage4iOS2.1.9ITouchIDBackButtonBug") : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                        response.Exception = new MOBException("9999", expMessage);
                    }
                    else if (!GeneralHelper.IsVersion1Greater(selectTripRequest.Application.Version.Major, "2.1.8", true) && ex.Message.ToLower().Trim() == "Object reference not set to an instance of an object.".ToLower().Trim())
                    {
                        #region
                        bool isProductSelected = false;

                        if (isProductSelected)
                        {
                            string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_2") != null ? _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_2") : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                            response.Exception = new MOBException("9999", expMessage);
                        }
                        else
                        {
                            string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1") != null ? _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1") : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                            response.Exception = new MOBException("9999", expMessage);
                        }
                        #endregion
                    }
                    else if (!GeneralHelper.IsVersion1Greater(selectTripRequest.Application.Version.Major, "2.1.8", true) &&
                    ex.Message.ToLower().Replace("\r\n", string.Empty).Trim() == "index was out of range. must be non-negative and less than the size of the collection.parameter name: index".ToLower().Trim())
                    {
                        string expMessage = _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1") != null ? _configuration.GetValue<string>("BookingExceptionMessage4Client2.1.8AndLess_1") : _configuration.GetValue<string>("Booking2OGenericExceptionMessage");
                        response.Exception = new MOBException("9999", expMessage);
                    }
                    else
                    {
                        response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                    }
                }
                else
                {
                    response.Exception = new MOBException("9999", ex.Message);
                }
                #endregion
            }


            //stopwatch.Stop();
            //response.CallDuration = stopwatch.ElapsedMilliseconds;


            if (response.Availability != null && response.Availability.Reservation != null)
            {
                Reservation bookingPathReservation = new Reservation();
                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(selectTripRequest.SessionId, bookingPathReservation.ObjectName, new List<string> { selectTripRequest.SessionId, bookingPathReservation.ObjectName });
                if (bookingPathReservation != null)
                {
                    //var availableReservationTrips = response.Availability.Reservation.Trips;
                    response.Availability.Reservation = await _shoppingUtility.GetReservationFromPersist(response.Availability.Reservation, selectTripRequest.SessionId).ConfigureAwait(false); //##Price Break Down - Kirti
                    //if (selectTripRequest.BackButtonClick)
                    //    response.Availability.Reservation.Trips = response.Availability.Reservation.Trips.Where(t => availableReservationTrips.Any(rt => rt.TripId == t.TripId)).ToList();

                    if (response.Availability.Reservation.NumberOfTravelers == 1)
                    {
                        bookingPathReservation.GetALLSavedTravelers = false;
                        //United.Persist.FilePersist.Save<Reservation>(request.SessionId, bookingPathReservation.ObjectName, bookingPathReservation); //commented as its saved below //FOP Options Fix Venkat 12/08

                        if (response.Availability.Reservation.IsEmp20 || (response.Availability.Reservation.ShopReservationInfo != null && response.Availability.Reservation.ShopReservationInfo.IsCorporateBooking))
                        {
                            response.Availability.Reservation.GetALLSavedTravelers = true;
                        }
                        else
                        {
                            response.Availability.Reservation.GetALLSavedTravelers = bookingPathReservation.GetALLSavedTravelers;
                        }
                    }
                    response.Availability.Reservation.FOPOptions = _shoppingUtility.GetFopOptions(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);//FOP Options Fix Venkat 12/08
                    if (session != null && session.IsReshopChange && response.Availability.Reservation.FOPOptions != null && response.Availability.Reservation.FOPOptions.Count > 0)
                    {
                        response.Availability.Reservation.FOPOptions.RemoveAll(p => p.Key.ToUpper().Contains("PAYPAL"));
                    }
                    bookingPathReservation.FOPOptions = response.Availability.Reservation.FOPOptions;//FOP Options Fix Venkat 12/08
                    bookingPathReservation.TCDAdvisoryMessages = response.Availability.Reservation.TCDAdvisoryMessages; //Nizam - 01/22/2018 - Assigning the message to object to save it in Reservation persist file.
                    bookingPathReservation.IsBookingCommonFOPEnabled = response.Availability.Reservation.IsBookingCommonFOPEnabled;
                    bookingPathReservation.IsReshopCommonFOPEnabled = response.Availability.Reservation.IsReshopCommonFOPEnabled;

                    #region Ebulk MApp
                    var cslReservation = await _sessionHelperService.GetSession<ReservationDetail>(selectTripRequest.SessionId, new ReservationDetail().GetType().FullName, new List<string> { selectTripRequest.SessionId, new ReservationDetail().GetType().FullName });
                    if (_shoppingUtility.IsEbulkPNRReshopEnabled(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major,cslReservation)) {
                        response.Availability.Reservation.Taxes = null;
                        response.Availability.Reservation.Prices=ReShopRemoveLineItemsForEbulk(selectTripRequest, response.Availability.Reservation.Prices, cslReservation);
                    }
                    #endregion
                    await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, selectTripRequest.SessionId, new List<string> { selectTripRequest.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName); //FOP Options Fix Venkat 12/08
                }

                if (session.IsReshopChange && response.Availability.Reservation.IsCubaTravel)
                {
                    if (_shoppingUtility.EnableReshopCubaTravelReasonVersion(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                    {
                        string cubaTravelerMsg = _configuration.GetValue<string>("SavedTravelerInformationNeededMessage") as string;
                        response.Availability.Reservation.TravelersCSL.ForEach(x => x.Message = cubaTravelerMsg);
                        response.Availability.Reservation.ShopReservationInfo2.AllEligibleTravelersCSL
                            = response.Availability.Reservation.TravelersCSL;
                    }
                }
            }
            if (isCFOPVersionCheck)
            {
                if ((!_configuration.GetValue<bool>("DisableSelectTripSessionNullCheckFix") && session != null)
                    || _configuration.GetValue<bool>("DisableSelectTripSessionNullCheckFix"))
                {
                    // Added for CFOP integration in RESHOP flow
                    if (_configuration.GetValue<bool>("IsReshopCommonFOPEnabled") && session.IsReshopChange)
                    {
                        try
                        {
                            shoppingCart = response.ShoppingCart;
                            if (shoppingCart != null)
                            {
                                shoppingCart = await _shoppingUtility.ReservationToShoppingCart_DataMigration(response.Availability.Reservation, shoppingCart, selectTripRequest, session);
                                response.ShoppingCart = shoppingCart;
                                await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, session.SessionId, new List<string> { session.SessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName);
                                var tupleRes = await _formsOfPayment.GetEligibleFormofPayments(selectTripRequest, session, shoppingCart, session.CartId, "RESHOP", isDefault, response.Availability.Reservation);
                                response.EligibleFormofPayments = tupleRes.response;
                                response.IsDefaultPaymentOption = tupleRes.isDefault;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            ExceptionWrapper exceptionWrapper = new ExceptionWrapper(ex);
                            _logger.LogWarning("ShoppingCartGetProductsFailed Exception {@ExceptionWrapper}", JsonConvert.SerializeObject(exceptionWrapper));
                        }
                    }
                }
            }

            try
            {
                if (_configuration.GetValue<string>("CartIdForDebug")?.ToUpper() == "YES" || (_configuration.GetValue<string>("DeviceIDSToReturnShopCallDurations") != null && _configuration.GetValue<string>("DeviceIDSToReturnShopCallDurations").ToUpper().Trim().Split('|').Contains(selectTripRequest.DeviceId.ToUpper().Trim())))//request.DeviceId.Trim().ToUpper() == "THIS_IS_FOR_TEST_" + DateTime.Now.ToString("MMM-dd-yyyy").ToUpper().Trim())
                {
                    response.CartId = response.Availability?.CartId;
                    if (response.Availability?.Trip?.FlattenedFlights != null && response.Availability?.Trip?.FlattenedFlights.Count > 0
                        && response.Availability?.Trip?.FlattenedFlights[0].Flights != null && response.Availability?.Trip?.FlattenedFlights[0].Flights.Count > 0)
                    {
                        response.CartId = response.Availability?.Trip?.CallDurationText + "|" + response.CallDuration;
                        response.Availability.Trip.FlattenedFlights[0].Flights[0].OperatingCarrierDescription = response.CartId;
                    }
                }

                else if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "REST3_YES")
                {
                    response.CartId = response.Availability.CartId;
                }
                else if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "REST3_YES_1")
                {
                    response.CartId = response.Availability.CartId;
                    response.Availability.Trip.FlattenedFlights[0].Flights[0].OperatingCarrierDescription = response.CartId;
                }
                if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "REST_TUNING_CALL_DURATION")
                {
                    response.CartId = response.Availability.CallDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString();
                }
                if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "REST_TUNING_CALL_DURATION_WITH_CARTID")
                {
                    response.CartId = response.Availability.CallDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString() + "|Cart ID = " + response.Availability.CartId;
                }
                if (_configuration.GetValue<string>("Log_CSL_Call_Statistics").ToUpper().Trim() == "TRUE")
                {
                    try
                    {
                        //Utility.AddCSLCallStatistics(request.Application.Id, request.Application.Version.Major, request.DeviceId, request.SessionId, response.Availability.Trip.Origin, response.Availability.Trip.Destination, response.Availability.Trip.DepartDate, request.SearchType, response.Availability.Trip.Cabin, request.FareType, request.AwardTravel, request.NumberOfAdults, request.MileagePlusAccountNumber, string.Empty);
                        //Utility.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", response.Availability.CartId, response.Availability.CallDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString(), "SelectTrip", request.SessionId);
                        //Utility.AddCSLStatistics(request.Application.Id, request.Application.Version.Major, request.DeviceId, request.SessionId, response.Availability.Trip.Origin, response.Availability.Trip.Destination, response.Availability.Trip.DepartDate, string.Empty, response.Availability.Trip.Cabin, string.Empty, response.Availability.Reservation.AwardTravel, 0, string.Empty, string.Empty, "REST_Shopping", response.Availability.CartId, response.Availability.CallDuration + "|REST Total = " + (response.CallDuration / (double)1000).ToString(), "Shopping/SelectTrip");
                        //string callDurations = _shoppingUtility.GetCSLCallDuration(shopping.LogEntries);
                        string origin = string.Empty, dest = string.Empty, cabin = string.Empty, flightdate = string.Empty;
                        // TFS- 64834 - Added below Not null check for availability property to overcome nullreference exception in the selecttrip - Vijayan
                        if (response.Availability != null)
                        {
                            if (response.Availability.Trip != null)
                            {
                                origin = response.Availability.Trip.Origin != null ? response.Availability.Trip.Origin : string.Empty;
                                dest = response.Availability.Trip.Destination != null ? response.Availability.Trip.Destination : string.Empty;
                                cabin = response.Availability.Trip.Cabin;
                                flightdate = response.Availability.Trip.DepartDate;
                               // _cSLStatistics.AddCSLStatistics(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, selectTripRequest.SessionId, origin, dest, flightdate, string.Empty, cabin, string.Empty, response.Availability.Reservation.AwardTravel, 0, string.Empty, string.Empty, "REST_Shopping", response.Availability.CartId, "Server:" + response.MachineName + "||" + (response.CallDuration / (double)1000).ToString(), "Shopping/SelectTrip");

                            }
                            else
                            {
                                await _cSLStatistics.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", response.Availability.CartId, "Server:" + response.MachineName + "||" + (response.CallDuration / (double)1000).ToString(), "Shopping/SelectTrip", selectTripRequest.SessionId);
                            }
                        }
                    }
                    catch { }
                }
            }

            catch { }
            if (response?.Availability?.Reservation?.Trips != null)
            {
                for (int i = 0; i < response?.Availability?.Reservation?.Trips.Count; i++)
                {
                    response.Availability.Reservation.Trips[i].ShareMessage = string.Empty;
                }

            }
            if (response?.Availability?.FSRAlertMessages != null)
            {
                for (int i = 0; i < response?.Availability?.FSRAlertMessages.Count; i++)
                {
                    if (response?.Availability?.FSRAlertMessages[i]?.Buttons != null)
                    {
                        for (int j = 0; j < response?.Availability?.FSRAlertMessages[i].Buttons.Count; j++)
                        {
                            if (response?.Availability?.FSRAlertMessages[i].Buttons[j]?.UpdatedShopRequest?.CatalogItems != null)
                                response.Availability.FSRAlertMessages[i].Buttons[j].UpdatedShopRequest.CatalogItems = null;
                        }
                    }
                }
            }

            if (_shoppingUtility.IsEnableEditSearchOnFSRHeaderBooking(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.CatalogItems) && response != null && session != null)
            {
                MOBSHOPShopRequest shopRequest = new MOBSHOPShopRequest();
                shopRequest = await _sessionHelperService.GetSession<MOBSHOPShopRequest>(selectTripRequest?.SessionId, shopRequest.ObjectName, new List<string> { selectTripRequest?.SessionId, shopRequest.ObjectName });
                response.ShowEditSearchHeaderOnFSRBooking = session.ShowEditSearchHeaderOnFSRBooking;
                if (!string.IsNullOrEmpty(selectTripRequest.CalendarDateChange) && shopRequest?.Trips != null && shopRequest?.Trips.Count > 1)
                {
                    if (shopRequest?.SearchType == "RT")
                        shopRequest.Trips.LastOrDefault().DepartDate = selectTripRequest.CalendarDateChange;
                    else if(shopRequest?.SearchType == "MD" && session.ShowEditSearchHeaderOnFSRBooking && !string.IsNullOrEmpty(Convert.ToString(response?.Availability?.Trip?.LastTripIndexRequested)))
                    {
                        shopRequest.Trips[response.Availability.Trip.LastTripIndexRequested-1].DepartDate = selectTripRequest.CalendarDateChange;
                    }
                }
                response.ShopRequest = shopRequest;
            }

            if (response?.Availability?.Reservation != null && response?.Availability?.Reservation?.TravelOptions == null)
            {
                response.Availability.Reservation.TravelOptions = new List<Model.Shopping.TravelOption>();
            }
            if (await _featureSettings.GetFeatureSettingValue("NonOTAMessageReshop"))
            {
                List<MOBPNRAdvisory> advisories = new List<MOBPNRAdvisory>();
                var advisoryMessages = await _sessionHelperService.GetSession<List<MOBPNRAdvisory>>(selectTripRequest.SessionId, advisories.GetType()?.FullName, new List<string> { selectTripRequest.SessionId, advisories.GetType()?.FullName });
                if (!advisoryMessages.IsListNullOrEmpty())
                {
                    var nonOTAMessage = advisoryMessages.FirstOrDefault(advisory => advisory.Header == "Agency credit");
                    if (nonOTAMessage?.Header != null && nonOTAMessage?.Body != null && response.Availability != null && response.Availability?.Reservation != null && response.Availability?.Reservation?.ShopReservationInfo2 != null)
                    {
                        if (response.Availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                        {
                            response.Availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
                        }
                        response.Availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(new InfoWarningMessages
                        {
                            IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString(),
                            HeaderMessage = nonOTAMessage?.Header,
                            Messages = new List<string> { nonOTAMessage?.Body },
                            IsCollapsable = true,
                            IsExpandByDefault = true
                        });
                    }
                }
            }

            return await Task.FromResult(response);
        }

        private async Task SetFSROnScreenAlerts(SelectTripRequest selectTripRequest, SelectTripResponse response, Session session, MOBSHOPShopRequest shopRequest)
        {
            if (await _featureSettings.GetFeatureSettingValue("EnableMoneyPlusMilesFastFollower").ConfigureAwait(false))
            {
                if (response.Availability.OnScreenAlerts == null)
                {
                    response.Availability.OnScreenAlerts = new List<MOBOnScreenAlert>();
                }
                MOBOnScreenAlert mobOnScreenAlert = new MOBOnScreenAlert();
                mobOnScreenAlert.Title = _configuration.GetValue<string>("UntiedOnScreenAlertTitle");
                mobOnScreenAlert.AlertType = MOBOnScreenAlertType.FSRMONEYPLUSMILES;
                string alerMessage = "";// GetSDLStringMessageFromList(lstMessages, "SelectTrip.MoneyPlusMiles.BackToMoney.AlertMessage");
                if (string.IsNullOrEmpty(alerMessage) == false)
                    mobOnScreenAlert.Message = alerMessage;
                else
                    mobOnScreenAlert.Message = _configuration.GetValue<string>("FSRSelectTripMoneyPlusMilesAlertMessage");
                mobOnScreenAlert.Actions = new List<MOBOnScreenActions>
                        {
                            new MOBOnScreenActions
                            {
                                ActionTitle = "Go back",//GetSDLStringMessageFromList(lstMessages, "SelectTrip.MoneyPlusMiles.BackToMoney.ActionText"),
                                ActionType = MOBOnScreenAlertActionType.NAVIGATE_BACK
                            }
                        };
                if (_shoppingUtility.IsEnableMoneyPlusMilesOnScreenAlert(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Minor, session?.CatalogItems)
                    && await _featureSettings.GetFeatureSettingValue("EnableMoneyPlusMilesCancelAlert").ConfigureAwait(false)
                    && shopRequest?.SearchType == "RT")
                {
                    mobOnScreenAlert.Actions.Add(new MOBOnScreenActions
                    {
                        ActionTitle = "Cancel",//GetSDLStringMessageFromList(lstMessages, "SelectTrip.MoneyPlusMiles.BackToMoney.ActionText"),
                        ActionType = MOBOnScreenAlertActionType.CANCEL
                    });
                }
                response.Availability.OnScreenAlerts.Add(mobOnScreenAlert);
            }
            if (await _featureSettings.GetFeatureSettingValue("EnableFSRETCTravelCreditsFeature").ConfigureAwait(false) && _shoppingUtility.IsEnableFSRETCTravelCreditsOnScreenAlert(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Minor, session?.CatalogItems))
            {
                if (response.Availability.OnScreenAlerts == null)
                {
                    response.Availability.OnScreenAlerts = new List<MOBOnScreenAlert>();
                }
                MOBOnScreenAlert mobOnScreenAlert = new MOBOnScreenAlert();
                mobOnScreenAlert.Title = _configuration.GetValue<string>("UntiedOnScreenAlertTitle"); 
                mobOnScreenAlert.AlertType = MOBOnScreenAlertType.FSRMONEYPLUSMILESMILEAGEPRICING;
                string alerMessage = "";// GetSDLStringMessageFromList(lstMessages, "SelectTrip.MoneyPlusMiles.BackToMoney.AlertMessage");
                if (string.IsNullOrEmpty(alerMessage) == false)
                    mobOnScreenAlert.Message = alerMessage;
                else
                {
                    mobOnScreenAlert.Title = _configuration.GetValue<string>("FSRMoneyPlusMilesETCCreditAlertTitle");
                    mobOnScreenAlert.Message = _configuration.GetValue<string>("FSRMoneyPlusMilesETCCreditAlertDescription");
                }
                if (_shoppingUtility.IsEnableMoneyPlusMilesOnScreenAlert(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Minor, session?.CatalogItems)
                    && shopRequest?.SearchType == "RT")
                {
                    mobOnScreenAlert.Actions = new List<MOBOnScreenActions>
                            {
                                new MOBOnScreenActions
                                {
                                    ActionTitle = "Continue",
                                    ActionType = MOBOnScreenAlertActionType.DISMISS_ALERT
                                }
                            };
                    response.Availability.OnScreenAlerts.Add(mobOnScreenAlert);
                }
            }
        }

        private bool IsLatestClientAppVerWithSelectTripBackButtonFix(int applicationID, string appVersion)
        {
            #region Priya Code for version check
            bool ValidVersion = false;
            string androidVersion = _configuration.GetValue<string>("AndroidIsLatestClientAppVerWithSelectTripBackButtonFix");
            string iPhoneVersion = _configuration.GetValue<string>("iPhoneIsLatestClientAppVerWithSelectTripBackButtonFix");
            string mWebVersion = _configuration.GetValue<string>("MWebIsLatestClientAppVerWithSelectTripBackButtonFix");

            Regex regex = new Regex("[0-9.]");
            appVersion = string.Join("", regex.Matches(appVersion).Cast<Match>().Select(match => match.Value).ToArray());

            if (applicationID == 1 && appVersion != iPhoneVersion)
            {
                ValidVersion = GeneralHelper.IsVersion1Greater(appVersion, iPhoneVersion);
            }
            else if (applicationID == 2 && appVersion != androidVersion)
            {
                ValidVersion = GeneralHelper.IsVersion1Greater(appVersion, androidVersion);
            }
            else if (applicationID == 16 && appVersion != mWebVersion)
            {
                ValidVersion = GeneralHelper.IsVersion1Greater(appVersion, mWebVersion);
            }
            #endregion
            return ValidVersion;
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

        private async Task AwardMTThrowErrorIfCurrentTripDepartDateGreaterThanNextTripDepartDate(SelectTripRequest request, SelectTrip selectTrip, Session session)
        {
            if (_configuration.GetValue<bool>("BugFixToggleFor17M") && selectTrip != null && session.IsAward)
            {
                CSLShopRequest cslShopRequest = new CSLShopRequest();

                cslShopRequest = await _sessionHelperService.GetSession<CSLShopRequest>(request.SessionId, cslShopRequest.ObjectName, new List<string> { request.SessionId, cslShopRequest.ObjectName }).ConfigureAwait(false);
                if (cslShopRequest == null)
                {
                    throw new MOBUnitedException("Could not find your booking session.");
                }
                ShopRequest shopRequest = cslShopRequest.ShopRequest;
                if (selectTrip.Responses.Count < shopRequest.Trips.Count && shopRequest.SearchTypeSelection == SearchType.MultipleDestination)
                {
                    if (_configuration.GetValue<string>("AwardCalenderMessageIfSelectedDateBeyondNextTripDepartDate") != null &&
                        !string.IsNullOrEmpty(request.CalendarDateChange) &&
                        (
                         DateTime.Parse(request.CalendarDateChange) > DateTime.Parse(shopRequest.Trips[selectTrip.Responses.Count + 1].DepartDate) ||
                         (CheckPreviousSelectedTripDepartDate(request.CalendarDateChange, selectTrip))
                        ))
                    {
                        throw new MOBUnitedException(_configuration.GetValue<string>("AwardCalenderMessageIfSelectedDateBeyondNextTripDepartDate").ToString());
                    }
                    else if ((string.IsNullOrEmpty(request.ProductId) || string.IsNullOrEmpty(request.TripId)) &&
                            selectTrip.Requests != null && selectTrip.Requests.Count > 0)
                    {
                        if (string.IsNullOrEmpty(request.ProductId))
                        {
                            request.ProductId = selectTrip.Requests[selectTrip.Requests.Keys.ElementAt(selectTrip.Requests.Count - 1)].ProductId;
                        }
                        if (string.IsNullOrEmpty(request.TripId))
                        {
                            request.TripId = selectTrip.Requests[selectTrip.Requests.Keys.ElementAt(selectTrip.Requests.Count - 1)].TripId;
                        }
                    }
                }
            }
        }
        private bool CheckPreviousSelectedTripDepartDate(string calendarDate, SelectTrip selectTrip)
        {
            string departDate = string.Empty;
            if (selectTrip.Responses.Count > 0 && selectTrip.Responses[selectTrip.Responses.Keys.ElementAt(selectTrip.Responses.Count - 1)] != null)
            {
                var response = selectTrip.Responses[selectTrip.Responses.Keys.ElementAt(selectTrip.Responses.Count - 1)];
                if (response.Availability != null && response.Availability.Trip != null)
                {
                    var trip = response.Availability.Trip;
                    departDate = trip.DepartDate;
                }
            }
            return (!string.IsNullOrEmpty(departDate) && DateTime.Parse(calendarDate) < DateTime.Parse(departDate));
        }
        private async Task<MOBSHOPAvailability> SelectTrip(SelectTripRequest selectRequest, int totalPassengers, MOBAdditionalItems additionalItems = null)
        {
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(selectRequest.SessionId, session.ObjectName, new List<string> { selectRequest.SessionId, session.ObjectName });
            string logAction = session.IsReshopChange ? "ReShopSelectTrip" : "SelectTrip";
            bool isShop = false;
            MOBSHOPAvailability availability = null; 
            if (additionalItems == null)
            {
                additionalItems = new MOBAdditionalItems();
            }
            bool isEnableWheelChairFilterOnFSR = await _featureToggles.IsEnableWheelchairFilterOnFSR(selectRequest.Application.Id, selectRequest.Application.Version.Major, session.CatalogItems).ConfigureAwait(false) || (session.IsReshopChange && await _featureToggles.IsEnableReshopWheelchairFilterOnFSR(selectRequest.Application.Id, selectRequest.Application.Version.Major, session.CatalogItems).ConfigureAwait(false));
            //int count = 0;
            FareWheelHelper fareWheelHelper = new FareWheelHelper(_configuration, _logger, _headers, _sessionHelperService, _shoppingUtility);
            United.Services.FlightShopping.Common.ShopSelectRequest request = await fareWheelHelper.GetShopSelectRequest(selectRequest, true, isEnableWheelChairFilterOnFSR);
            //request.FareWheelDateChange = "";
            string jsonRequest = JsonConvert.SerializeObject(request), shopCSLCallDurations = string.Empty;
            United.Services.FlightShopping.Common.ShopSelectRequest calendarRequest = fareWheelHelper.GetShopSelectFareWheelRequest(request);

            //calendarRequest.FareWheelOnly = true;
            //calendarRequest.CalendarDateChange = "";
            string calendarJsonRequest = JsonConvert.SerializeObject(calendarRequest);
            _logger.LogInformation("SelectTrip with CalendarRequest {@CalendarRequest}", calendarJsonRequest);

            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }

            Reservation persistReservation = new Reservation();
            persistReservation = await _sessionHelperService.GetSession<Reservation>(selectRequest.SessionId, persistReservation.ObjectName, new List<string> { selectRequest.SessionId, persistReservation.ObjectName });


            ShoppingResponse persistShop = new ShoppingResponse();
            persistShop = await _sessionHelperService.GetSession<ShoppingResponse>(selectRequest.SessionId, persistShop.ObjectName, new List<string> { selectRequest.SessionId, persistShop.ObjectName });
            if (persistShop == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }
            //string response = string.Empty;
            string calendarJsonResponse = string.Empty;
            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            string callTime4Tuning = string.Empty;
            Stopwatch cslSelectTripWatch;
            cslSelectTripWatch = new Stopwatch();
            cslSelectTripWatch.Reset();
            cslSelectTripWatch.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            var action = "";
            
            if(_shoppingUtility.EnableAwardNonStop(session.AppID, session.VersionID, session.IsReshopChange ,session.IsAward))
            {
                action = "award/ShopSelect";
            }
            else
            {
                action = session.IsReshopChange ? "ReShop/Select" : "ShopSelect";
            }
            var response = await _flightShoppingService.SelectTrip<United.Services.FlightShopping.Common.ShopResponse>(session.Token, selectRequest.SessionId, action, jsonRequest);

            #region// 2 = selectTripCSLCallDurationstopwatch1//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cslSelectTripWatch.IsRunning)
            {
                cslSelectTripWatch.Stop();
            }
            shopCSLCallDurations = shopCSLCallDurations + "|2=" + cslSelectTripWatch.ElapsedMilliseconds.ToString() + "|"; // 2 = selectTripCSLCallDurationstopwatch1
            string cslCallDuration = "|CSL_Select_Trip =" + (cslSelectTripWatch.ElapsedMilliseconds / (double)1000).ToString(); //CSL_Select_Trip= selectTripCSLCallDurationstopwatch1
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            if (response != null)
            {
                shopCSLCallDurations = shopCSLCallDurations + "|CSLShopSelect=" + response.CallTimeDomain;
                callTime4Tuning = "ITA = " + response.CallTimeBBX + callTime4Tuning;
                bool isLastTripSelected = false;

                CSLShopRequest shopRequest = new CSLShopRequest();
                try
                {
                    shopRequest = await _sessionHelperService.GetSession<CSLShopRequest>(selectRequest.SessionId, shopRequest.ObjectName, new List<string> { selectRequest.SessionId, shopRequest.ObjectName });
                }
                catch (System.Exception) { }

                United.Services.FlightShopping.Common.ShopResponse calendarResponse = JsonConvert.DeserializeObject<United.Services.FlightShopping.Common.ShopResponse>(calendarJsonResponse);
                /// Bug 216007 : mApp: First segment FSR displayed for the third segment in the Multi Trip Booking, observed only first time in the fresh installation (Sporadic issue)
                /// Srini - 28/08/2017 CB
                bool bErrorcheckflag = true; // if any errors found in response then this flag will be false. 
                if (_configuration.GetValue<bool>("BugFixToggleFor17M"))
                {
                    if (response != null && response.Errors != null)
                    {
                        bErrorcheckflag = _shopBooking.NoCSLExceptions(response.Errors);
                    }
                }

                if (response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && (bErrorcheckflag))
                {
                    availability = new MOBSHOPAvailability();
                    availability.SessionId = selectRequest.SessionId;
                    availability.CartId = response.CartId;
                    if (!string.IsNullOrEmpty(session.EmployeeId))
                    {
                        availability.UaDiscount = _configuration.GetValue<string>("UADiscount");
                    }
                    availability.AwardTravel = persistShop.Request.AwardTravel;

                    availability.Reservation = new MOBSHOPReservation(_configuration, _cachingService);

                   
                    //-------Feature 208204--- Common class data carrier for hirarchy methds-----
                    MOBSHOPDataCarrier _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                    _mOBSHOPDataCarrier.SearchType = persistShop.Request.SearchType;
                    if (_configuration.GetValue<bool>("Shopping - bPricingBySlice"))
                    {
                        if (!availability.AwardTravel && !session.IsReshopChange)
                        {
                            //availability.DisablePricingBySlice = _configuration.GetValue<bool>("Shopping - bPricingBySlice");
                            availability.PriceTextDescription = _shopBooking.GetPriceTextDescription(persistShop.Request.SearchType);
                            //availability.FSRFareDescription = GetFSRFareDescription(persistShop.Request);
                            _shopBooking.SetFSRFareDescriptionForShop(availability, persistShop.Request);
                            _mOBSHOPDataCarrier.PriceFormText = _shopBooking.GetPriceFromText(persistShop.Request.SearchType);
                        }
                        else
                        {
                            _shopBooking.SetSortDisclaimerForReshop(availability, persistShop.Request);
                        }
                    }

                    var isStandardRevenueSearch = _shopBooking.IsStandardRevenueSearch(session.IsCorporateBooking, session.IsYoungAdult, availability.AwardTravel,
                                                                          session.EmployeeId, session.TravelType, session.IsReshopChange,
                                                                          persistShop.Request.FareClass, persistShop.Request.PromotionCode);


                    if (response.Trips != null && response.Trips.Count > 0)
                    {
                        bool readFromShop = false;

                        availability.Trip = null;
                        availability.Reservation.Trips = null;
                        List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(selectRequest, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                        int partiallyUsed = 0;
                        for (int i = 0; i < response.Trips.Count; i++)
                        {
                            ///95509 - iOS/Android-Farewheel price is differ from lowest price for OW /RT/MT on Business cabin search
                            ///224324 - mApp : Booking –Revenue- Multitrip – Economy/First- Fare wheel in FSR2 is showing prices based on FSR1 cabin search and not on the FSR2 cabin search
                            ///Srini - 03/20/2018
                            if (_configuration.GetValue<bool>("BugFixToggleFor18C"))
                            {
                                _mOBSHOPDataCarrier.FsrMinPrice = 0M;
                            }

                            if (response.Trips[i] != null && (!string.IsNullOrEmpty(response.Trips[i].BBXCellIdSelected) || (session.IsReshopChange && i < response.LastTripIndexRequested - 1 && response.Trips[i].ChangeType != 3)) && response.Trips[i].Flights.Count == 1)
                            {
                                if (availability.Reservation == null)
                                    availability.Reservation = new MOBSHOPReservation(_configuration, _cachingService);
                                if (availability.Reservation.Trips == null)
                                    availability.Reservation.Trips = new List<MOBSHOPTrip>();
                                if (_configuration.GetValue<bool>("CheckCSLShopSelectFlightsNull") && (response.Trips[i].Flights == null || response.Trips[i].Flights.Count == 0))
                                {
                                    //To ByPass this Flight Null Check if have any issues after weekly releaase 10/27 just add this entry to production web config =  <add key="CheckCSLShopSelectFlightsNull" value="false"/>
                                    // To turn on check Flight Nulls delete the entry in web config or update the value to true <add key="CheckCSLShopSelectFlightsNull" value="true"/>
                                    string actionName = session.IsReshopChange ? "CSL ReShop SelectTrip – Flights Null" : "CSL Shop Select – Flights Null";
                                    if (shopRequest != null && shopRequest.ShopRequest != null && shopRequest.ShopRequest.AwardTravel)
                                    {
                                        actionName = actionName + " - Award Search";
                                    }
                                    _logger.LogWarning("SelectTrip with {@ErrorMessage}", actionName);
                                    //61048 - Bug 331484:FS (Mobile) Item 29: Flight Shopping (Mobile): Unhandled Exception ArgumentOutofRangeException - 2.1.9 - Flights Object Null
                                    throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0"));
                                }
                                if (!_configuration.GetValue<bool>("EnableNonStopFlight"))
                                {
                                    availability.Reservation.Trips.Add(await _shopBooking.PopulateTrip(_mOBSHOPDataCarrier, response.CartId, response.Trips, i,
                                        persistShop.Response.Availability.Trip.SearchFiltersIn.FareFamily,
                                        selectRequest.SessionId, selectRequest.Application.Id,
                                        selectRequest.DeviceId, selectRequest.Application.Version.Major,
                                        persistShop.Request.ShowMileageDetails, persistShop.Request.PremierStatusLevel,
                                        isStandardRevenueSearch, availability.AwardTravel, additionalItems: additionalItems,lstMessages:lstMessages, httpContext: _httpContext));
                                }
                                else
                                {
                                    ///208852 : Booking - FSR - PROD Basic Economy mApps Lowest Basic Economy fare is displayed. (Basic Economy switch is off) 
                                    ///Srini - 11/27/2017
                                    availability.Reservation.Trips.Add(await _shopBooking.PopulateTrip(_mOBSHOPDataCarrier, response.CartId, response.Trips, i,
                                        persistShop.Response.Availability.Trip.SearchFiltersIn.FareFamily,
                                        selectRequest.SessionId, selectRequest.Application.Id,
                                        selectRequest.DeviceId, selectRequest.Application.Version.Major,
                                        persistShop.Request.ShowMileageDetails, persistShop.Request.PremierStatusLevel,
                                        isStandardRevenueSearch, availability.AwardTravel, (_configuration.GetValue<bool>("BugFixToggleFor17M") ? session.IsBEFareDisplayAtFSR : true), selectRequest.GetNonStopFlightsOnly, selectRequest.GetFlightsWithStops, persistShop?.Request
                                        , additionalItems: additionalItems,lstMessages:lstMessages, httpContext: _httpContext));
                                }
                            }
                            else if ((response.Trips[i] != null && !session.IsReshopChange) || (session.IsReshopChange && (i + 1) == response.LastTripIndexRequested))//assume this is the trips for selection
                            {
                                if ((string.IsNullOrEmpty(_configuration.GetValue<string>("CheckCSLShopSelectFlightsNull")) || _configuration.GetValue<bool>("CheckCSLShopSelectFlightsNull")) && (response.Trips[i].Flights == null || response.Trips[i].Flights.Count == 0) && response.LastTripIndexRequested == i + 1)
                                {
                                    //To ByPass this Flight Null Check if have any issues after weekly releaase 10/27 just add this entry to production web config =  <add key="CheckCSLShopSelectFlightsNull" value="false"/>
                                    // To turn on check Flight Nulls delete the entry in web config or update the value to true <add key="CheckCSLShopSelectFlightsNull" value="true"/>
                                    string actionName = session.IsReshopChange ? "CSL ReShop SelectTrip – Flights Null" : "CSL Shop Select – Flights Null";
                                    if (shopRequest != null && shopRequest.ShopRequest != null && shopRequest.ShopRequest.AwardTravel)
                                    {
                                        actionName = actionName + " - Award Search";
                                    }
                                    ////logEntries.Add(LogEntry.GetLogEntry<string>(selectRequest.SessionId, actionName, "Trace", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, jsonRequest));
                                    _logger.LogWarning("SelectTrip with {@ErrorMessage}", actionName);
                                    //61048 - Bug 331484:FS (Mobile) Item 29: Flight Shopping (Mobile): Unhandled Exception ArgumentOutofRangeException - 2.1.9 - Flights Object Null
                                    throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0"));
                                }
                                if (availability.Trip == null)
                                {
                                    if (!_configuration.GetValue<bool>("EnableNonStopFlight"))
                                    {
                                        availability.Trip = await _shopBooking.PopulateTrip(_mOBSHOPDataCarrier, response.CartId, response.Trips, i,
                                            persistShop.Response.Availability.Trip.SearchFiltersIn.FareFamily,
                                            selectRequest.SessionId, selectRequest.TripId, selectRequest.Application.Id,
                                            selectRequest.DeviceId, selectRequest.Application.Version.Major,
                                            persistShop.Request.ShowMileageDetails,
                                            persistShop.Request.PremierStatusLevel, availability.AwardTravel, additionalItems:additionalItems,lstMessages:lstMessages, httpContext: _httpContext);
                                        partiallyUsed++;
                                    }
                                    else
                                    {
                                        ///208852 : Booking - FSR - PROD Basic Economy mApps Lowest Basic Economy fare is displayed. (Basic Economy switch is off) 
                                        ///Srini - 11/27/2017
                                        availability.Trip = await _shopBooking.PopulateTrip(_mOBSHOPDataCarrier, response.CartId, response.Trips, i,
                                            persistShop.Response.Availability.Trip.SearchFiltersIn.FareFamily,
                                            selectRequest.SessionId, selectRequest.TripId, selectRequest.Application.Id,
                                            selectRequest.DeviceId, selectRequest.Application.Version.Major,
                                            persistShop.Request.ShowMileageDetails,
                                            persistShop.Request.PremierStatusLevel, isStandardRevenueSearch, availability.AwardTravel, (_configuration.GetValue<bool>("BugFixToggleFor17M") ? session.IsBEFareDisplayAtFSR : true), selectRequest.GetNonStopFlightsOnly, selectRequest.GetFlightsWithStops, persistShop?.Request, session,
                                            additionalItems,lstMessages:lstMessages, httpContext: _httpContext);
                                        partiallyUsed++;
                                    }
                                }

                                availability.AwardCalendar = _shopBooking.PopulateAwardCalendar(response.Calendar, response.LastBBXSolutionSetId, request.BBXCellId);

                                #region Save Amenities Request to Persist
                                UpdateAmenitiesIndicatorsRequest amenitiesRequest = new UpdateAmenitiesIndicatorsRequest();
                                amenitiesRequest = _shopBooking.GetAmenitiesRequest(response.CartId, response.Trips[i].Flights);
                                ShoppingExtend shopExtendDAL = new ShoppingExtend(_sessionHelperService);
                                await shopExtendDAL.AddAmenitiesRequestToPersist(amenitiesRequest, selectRequest.SessionId, response.LastTripIndexRequested.ToString());
                                #endregion
                            }

                            // For Reshop & partially used pnr
                            if (availability.Trip != null && session.IsReshopChange && persistReservation.Reshop.IsUsedPNR)
                            {
                                availability.Trip.Cabin = persistShop.Request.Trips[partiallyUsed].Cabin;
                                availability.Trip.LastTripIndexRequested = response.LastTripIndexRequested;
                            }
                            #region  //**NOTE**// Venkat - Nov 10,2014 For Oragainze Results
                            else if (availability.Trip != null) // Booking & Reshop
                            {
                                availability.Trip.Cabin = persistShop.Request.Trips[i].Cabin;
                                availability.Trip.LastTripIndexRequested = response.LastTripIndexRequested;
                            }
                            #endregion
                        }
                        availability.Reservation.SessionId = selectRequest.SessionId;
                        availability.Reservation.SearchType = persistShop.Request.SearchType;
                        if (_shoppingUtility.EnableTravelerTypes(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, session.IsReshopChange) && persistShop.Request.TravelerTypes != null && persistShop.Request.TravelerTypes.Count > 0)
                        {
                            availability.TravelerCount = ShopStaticUtility.GetTravelerCount(persistShop.Request.TravelerTypes);
                            availability.Reservation.NumberOfTravelers = availability.TravelerCount;

                            if (availability.Reservation.ShopReservationInfo2 == null)
                                availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();

                            availability.Reservation.ShopReservationInfo2.TravelerTypes = persistShop.Request.TravelerTypes;

                        }
                        else
                        {
                            availability.Reservation.NumberOfTravelers = persistShop.Request.NumberOfAdults + persistShop.Request.NumberOfChildren12To17 + persistShop.Request.NumberOfChildren2To4 + persistShop.Request.NumberOfChildren5To11 + persistShop.Request.NumberOfInfantOnLap + persistShop.Request.NumberOfInfantWithSeat + persistShop.Request.NumberOfSeniors;
                            availability.TravelerCount = availability.Reservation.NumberOfTravelers;
                        }


                        #region
                        SelectTrip selectTrip = new SelectTrip();
                        try
                        {
                            selectTrip = await _sessionHelperService.GetSession<SelectTrip>(selectRequest.SessionId, selectTrip.ObjectName, new List<string> { selectRequest.SessionId, selectTrip.ObjectName });
                        }
                        catch (System.Exception ex) { }
                        #endregion

                        await _shopBooking.IsLastTripFSR(session.IsReshopChange, availability, response.Trips);

                        if (session.IsReshopChange &&
                            response.Trips.All(x => x.ChangeType != United.Services.FlightShopping.Common.Types.ChangeTypes.ChangeTrip) &&
                            response.Trips.Where(t => t.ChangeType == United.Services.FlightShopping.Common.Types.ChangeTypes.SelectedTrip).All(x => x.Selected))
                        {
                            isLastTripSelected = true;
                        }
                        else if (!session.IsReshopChange && IsLatestClientAppVerWithSelectTripBackButtonFix(selectRequest.Application.Id, selectRequest.Application.Version.Major))
                        {
                            if (_configuration.GetValue<bool>("EnableNonStopFlight"))
                            {
                                if (!_configuration.GetValue<bool>("EnableProfileCallFixDueToSeleteTripMultipleTimeCalled"))
                                {
                                    // Index Out Of Range Fix with Back Button and Is Product Selected flags implementation
                                    if ((selectTrip != null && selectTrip.Requests.Count + 1 == session.ShopSearchTripCount && selectRequest.ISProductSelected && !selectRequest.GetFlightsWithStops) || session.ShopSearchTripCount == 1) //==> Newly Added Condition for Non Stop Flights Changes !selectRequest.GetFlightsWithStops means its second Select Trip Call.
                                    {
                                        isLastTripSelected = true;
                                    }
                                }
                                else
                                {
                                    if ((selectTrip != null && selectTrip.Requests.Count + 1 >= session.ShopSearchTripCount && selectRequest.ISProductSelected && !selectRequest.GetFlightsWithStops) || session.ShopSearchTripCount == 1) //==> Newly Added Condition for Non Stop Flights Changes !selectRequest.GetFlightsWithStops means its second Select Trip Call.
                                    {
                                        isLastTripSelected = true;
                                    }
                                }
                            }
                            else
                            {
                                if (!_configuration.GetValue<bool>("EnableProfileCallFixDueToSeleteTripMultipleTimeCalled"))
                                {
                                    // Index Out Of Range Fix with Back Button and Is Product Selected flags implementation
                                    if ((selectTrip != null && selectTrip.Requests.Count + 1 == session.ShopSearchTripCount && selectRequest.ISProductSelected) || session.ShopSearchTripCount == 1)
                                    {
                                        isLastTripSelected = true;
                                    }
                                }
                                else
                                {
                                    if ((selectTrip != null && selectTrip.Requests.Count + 1 >= session.ShopSearchTripCount && selectRequest.ISProductSelected) || session.ShopSearchTripCount == 1)
                                    {
                                        isLastTripSelected = true;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Old Code Before Index out of Range Exception Fix
                            if (!session.IsReshopChange && response.Trips[response.Trips.Count - 1] != null && response.Trips[response.Trips.Count - 1].BBXCellIdSelected != null && response.Trips[response.Trips.Count - 1].BBXCellIdSelected == selectRequest.ProductId)
                            {
                                isLastTripSelected = true;
                            }
                        }

                        bool isCorporateBooking = _configuration.GetValue<bool>("CorporateConcurBooking") && persistShop.Request.IsCorporateBooking;
                        if (!session.IsReshopChange && !isLastTripSelected)
                        {
                            _shopBooking.SetTitleForFSRPage(availability, persistShop.Request);
                        }

                        if (isLastTripSelected)
                        {

                            MOBResReservation resReservation = new MOBResReservation();
                            //As per Sai we have ignored register flights for reshop earlier and now as per CFOP - MOBILE-30828 we have to do a registerflights call for reshop as well in future we don't need else part
                            if (_configuration.GetValue<bool>("EnableOmniChannelCartMVP1") && !session.IsReshopChange)
                            {
                                var tupleResponse = await GetShopBookingDetailsV2(_mOBSHOPDataCarrier, selectRequest.SessionId, request, availability, selectRequest, persistShop);
                                resReservation = tupleResponse.reservation;
                                availability = tupleResponse.availability;
                            }
                            else
                            {
                                var tupleResponse = await GetShopBookingDetails(_mOBSHOPDataCarrier, selectRequest.SessionId, request, availability, selectRequest, persistShop);
                                resReservation = tupleResponse.reservation;
                                availability = tupleResponse.availability;
                            }
                            #region


                            if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "REST_TUNING_CALL_DURATION")
                            {
                                cslCallDuration = cslCallDuration + "|CSL_Booking_Details=" + (resReservation.CallDuration / (double)1000).ToString();
                            }
                            #endregion

                            #region Save Unfinished Booking

                            if (_shoppingUtility.EnableUnfinishedBookings(selectRequest) && !_configuration.GetValue<bool>("EnableCCEServiceforGetUnfinishedBookings"))
                            {
                                try
                                {
                                    // Only save unfinished booking for regular revenue customer
                                    if (!session.IsAward && !session.IsReshopChange && !isCorporateBooking && string.IsNullOrEmpty(session.EmployeeId) && !IsYABooking(availability.Reservation))
                                    {
                                        if (session.CustomerID > 0) // for signed in customer only
                                        {
                                            ////removing Task.Factory converting to asyn call
                                            await _unfinishedBooking.SaveAnUnfinishedBooking(session, selectRequest, MapUnfinishedBookingFromMOBSHOPReservation(availability.Reservation));
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    if (session != null)
                                    {
                                        ////logEntries.Add(LogEntry.GetLogEntry(selectRequest.SessionId, "SelectTrip - SaveAnUnfinishedBooking", "Exception", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, new MOBExceptionWrapper(ex)));
                                        _logger.LogError("SelectTrip sessionId {sessionid}, Exception {@ex}, deviceId {dev}", selectRequest.SessionId, JsonConvert.SerializeObject(ex), selectRequest.DeviceId);
                                    }
                                }
                            }

                            #endregion

                            #region SSR

                            if (_shoppingUtility.EnableSpecialNeeds(selectRequest.Application.Id, selectRequest.Application.Version.Major))
                            {
                                //testing, remove
                                try
                                {
                                    var persistedShopBookingDetailsResponse = new ShopBookingDetailsResponse();
                                    persistedShopBookingDetailsResponse = await _sessionHelperService.GetSession<ShopBookingDetailsResponse>(selectRequest.SessionId, persistedShopBookingDetailsResponse.ObjectName, new List<string> { selectRequest.SessionId, persistedShopBookingDetailsResponse.ObjectName }).ConfigureAwait(false);

                                    if (persistedShopBookingDetailsResponse != null)
                                    {
                                        // populate avail. special needs for the itinerary
                                        availability.Reservation.ShopReservationInfo2.SpecialNeeds = await _unfinishedBooking.GetItineraryAvailableSpecialNeeds(session, selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, persistedShopBookingDetailsResponse.Reservation.FlightSegments, "en-US", availability.Reservation, selectRequest);

                                        // update persisted reservation object too
                                        var bookingPathReservation = new Reservation();
                                        bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(selectRequest.SessionId, bookingPathReservation.ObjectName, new List<string> { selectRequest.SessionId, bookingPathReservation.ObjectName });
                                        if (bookingPathReservation != null && bookingPathReservation.ShopReservationInfo2 != null)
                                        {
                                            bookingPathReservation.ShopReservationInfo2.SpecialNeeds = availability.Reservation.ShopReservationInfo2.SpecialNeeds;
                                            await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, selectRequest.SessionId, new List<string> { selectRequest.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName);
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    //logEntries.Add(LogEntry.GetLogEntry(session.SessionId, "SelectTrip - GetItineraryAvailableSpecialNeeds", "Exception", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, new MOBExceptionWrapper(e)));
                                    _logger.LogError("SelectTrip - GetItineraryAvailableSpecialNeeds sessionId {sessionid}, Exception {@exception}, deviceId {dev}", selectRequest?.SessionId, JsonConvert.SerializeObject(e), selectRequest?.DeviceId);
                                }
                            }
                            var bookingReservation = new Reservation();
                            bookingReservation = await _sessionHelperService.GetSession<Reservation>(selectRequest.SessionId, bookingReservation.ObjectName, new List<string> { selectRequest.SessionId, bookingReservation.ObjectName });
                            #endregion
                            #region WheelChair Sizer changes
                            if (await _shoppingUtility.IsEnableWheelChairSizerChanges(selectRequest.Application.Id, selectRequest.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false)
                                || (session.IsReshopChange && await _featureToggles.IsEnableReshopWheelchairFilterOnFSR(selectRequest.Application.Id, selectRequest.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false)))
                            {
                                try
                                {
                                    if (availability.Reservation != null && availability.Reservation.ShopReservationInfo2 != null)
                                    {
                                        availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo = new WheelChairSizerInfo();
                                        availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.ImageUrl1 = ShopBooking.GetFormatedUrl(_httpContext.Request.Host.Value,
                                                         _httpContext.Request.Scheme, _configuration.GetValue<string>("WheelChairImageUrl"), true);
                                        availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.ImageUrl2 = _shoppingUtility.GetFormatedUrl(_httpContext.Request.Host.Value,
                                            _httpContext.Request.Scheme, _configuration.GetValue<string>("WheelChairFoldedImageUrl"), true);

                                        var sdlKeyForWheelchairSizerContent = _configuration.GetValue<string>("SDLKeyForWheelChairSizerContent");
                                        var message = !string.IsNullOrEmpty(sdlKeyForWheelchairSizerContent) ? await GetCMSContentMessageByKey(sdlKeyForWheelchairSizerContent, selectRequest, session).ConfigureAwait(false) : null;
                                        if (message != null)
                                        {
                                            availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.WcHeaderMsg = message?.HeadLine;
                                            availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.WcBodyMsg = message?.ContentFull;
                                        }
                                        _shoppingUtility.BuildWheelChairSizerOAMsgs(availability.Reservation);
                                        try
                                        {
                                            //Any changes here,add in RepriceForAddTravelers api call (UnfinishedBooking.cs) as well
                                            if (await _featureToggles.IsEnableWheelchairFilterOnFSR(selectRequest.Application.Id, selectRequest.Application.Version.Major, session.CatalogItems).ConfigureAwait(false) || (session.IsReshopChange && await _featureToggles.IsEnableReshopWheelchairFilterOnFSR(selectRequest.Application.Id, selectRequest.Application.Version.Major, session.CatalogItems).ConfigureAwait(false)))
                                            {
                                                MOBSearchFilters searchFilters = new MOBSearchFilters();
                                                searchFilters = await _sessionHelperService.GetSession<MOBSearchFilters>(session.SessionId, searchFilters.GetType().FullName, new List<string> { session.SessionId, searchFilters.GetType().FullName });
                                                if (selectRequest.Filters != null && selectRequest.Filters.RemoveWheelChairFilterApplied)
                                                {
                                                    await _shoppingUtility.RemoveSavedDimensionInfo(searchFilters, session.SessionId).ConfigureAwait(false);
                                                }
                                                else
                                                {
                                                    _shoppingUtility.PrepopulateDimensionInfo(searchFilters, availability.Reservation.ShopReservationInfo2, session.SessionId);
                                                    await _shoppingUtility.ValidateWheelChairFilterSize(availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.DimensionInfo, session.SessionId, availability, lstMessages).ConfigureAwait(false);
                                                    if (!availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.IsWheelChairSizerEligible)
                                                    {
                                                        availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo.DimensionInfo = null;
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            _logger.LogError("WheelChairFilter-Dimension Info Prepopulating error-{@Exception},SessionId-{sessionId}", JsonConvert.SerializeObject(ex), shopRequest.SessionId);
                                        }

                                        if (bookingReservation != null && bookingReservation.ShopReservationInfo2 != null)
                                        {
                                            bookingReservation.ShopReservationInfo2.WheelChairSizerInfo = availability.Reservation.ShopReservationInfo2.WheelChairSizerInfo;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger1.LogError("SelectTrip - WheelChairSizerContent {@message} {@stackTrace}", ex.Message, ex.StackTrace);
                                }
                            }
                            #endregion
                            #region TaxID Collection
                            if(await _featureToggles.IsEnableTaxIdCollectionForLATIDCountries(selectRequest.Application.Id, selectRequest.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false)
                        && !session.IsReshopChange && _shoppingUtility.IsTaxIdCountryEnable(availability.Reservation?.Trips, lstMessages))
                            {
                                try
                                {
                                    await _shoppingUtility.BuildTaxIdInformationForLatidCountries(availability.Reservation, selectRequest, session, lstMessages).ConfigureAwait(false);

                                    if (bookingReservation != null && bookingReservation.ShopReservationInfo2 != null)
                                    {
                                        bookingReservation.ShopReservationInfo2.TaxIdInformation = availability.Reservation.ShopReservationInfo2.TaxIdInformation;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.ILoggerError("SelectTrip - TaxID Collection {@message} {@stackTrace}", ex.Message, ex.StackTrace);
                                }
                            }
                            #endregion
                            if(bookingReservation != null)
                            await _sessionHelperService.SaveSession<Reservation>(bookingReservation, selectRequest.SessionId, new List<string> { selectRequest.SessionId, bookingReservation.ObjectName }, bookingReservation.ObjectName);
                            //Build Shoppingcart products(i.e RES) only when if it is the last select trip
                            try
                            {

                                var version1 = _configuration.GetValue<string>("AndriodCFOP_Booking_Reshop_PostbookingAppVersion");
                                var version2 = _configuration.GetValue<string>("IphoneCFOP_Booking_Reshop_PostbookingAppVersion");
                                bool isCFOPVersionCheck = GeneralHelper.IsApplicationVersionGreaterorEqual(selectRequest.Application.Id, selectRequest.Application.Version.Major, version1, version2);
                                if (isCFOPVersionCheck)
                                {
                                    if (_configuration.GetValue<bool>("IsReshopCommonFOPEnabled") && session.IsReshopChange)
                                    {
                                        await PopulateCFOPReshopProducts(session, selectRequest);//, LogEntries);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                                //logEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(selectRequest.SessionId, "ShoppingCartGetProductsFailed", "Exception", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, exceptionWrapper, true, false));
                                _logger.LogError("ShoppingCartGetProductsFailed sessionId {sessionid}, Exception {@exception}, deviceId {dev}", selectRequest?.SessionId, JsonConvert.SerializeObject(exceptionWrapper), selectRequest?.DeviceId);
                            }
                        }
                        else
                        {
                            _shopBooking.StrikeThroughContentMessages(availability, additionalItems, session, selectRequest);
                        }
                        if (availability.Reservation.Trips != null && availability.Reservation.Trips.Count > 0 && availability.Trip != null)
                        {
                            List<MOBSHOPTripBase> shopTrips = new List<MOBSHOPTripBase>();
                            shopTrips.Add(availability.Trip);
                            shopTrips.Add(availability.Reservation.Trips[availability.Reservation.Trips.Count - 1]);

                            if (shopRequest != null && shopRequest.ShopRequest != null && string.IsNullOrEmpty(shopRequest.ShopRequest.EmployeeDiscountId))
                            {
                                availability.FareWheel = _shopBooking.PopulateFareWheelDates(shopTrips, "SELECTTRIP");
                            }
                        }
                        #region Corporate Booking
                        if (isCorporateBooking && persistShop.Request.MOBCPCorporateDetails != null && !string.IsNullOrEmpty(persistShop.Request.MOBCPCorporateDetails.CorporateCompanyName))
                        {
                            bool isU4BCorporateBookingEnabled = _shoppingUtility.IsEnableU4BCorporateBooking(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major);
                            bool isEnableSuppressingCompanyNameForBusiness = await _shoppingUtility.IsEnableSuppressingCompanyNameForBusiness(persistShop.Request.MOBCPCorporateDetails.IsPersonalized).ConfigureAwait(false);
                            string corporateDisclaimer = await _shoppingUtility.IsEnableCorporateNameChange().ConfigureAwait(false) ? _shoppingUtility.GetCorporateDisclaimerText(persistShop.Request, isU4BCorporateBookingEnabled, isEnableSuppressingCompanyNameForBusiness, session.IsReshopChange) : _shoppingUtility.GetCorporateDisclaimerText(persistShop.Request, isU4BCorporateBookingEnabled);

                            bool isEnableU4BTravelAddONPolicy = await _shoppingUtility.IsEnableU4BTravelAddONPolicy(isCorporateBooking, selectRequest.Application.Id, selectRequest.Application.Version.Major, persistShop?.Request?.CatalogItems).ConfigureAwait(false);
                            if (!isEnableU4BTravelAddONPolicy && !session.IsReshopChange)
                            {
                                if (_configuration.GetValue<bool>("EnableSortFilterEnhancements"))
                                {
                                    availability.CorporateDisclaimer = $"{corporateDisclaimer}{(!string.IsNullOrEmpty(availability.CorporateDisclaimer) ? "\n" + availability.CorporateDisclaimer : "")}";
                                }
                                else
                                {
                                    availability.CorporateDisclaimer = corporateDisclaimer;
                                }
                            }
                            if (isEnableU4BTravelAddONPolicy)
                            {
                                availability.CorporateOutOfPolicy = persistShop?.Response?.Availability?.CorporateOutOfPolicy;
                            }
                        }
                        #endregion  

                        if (session.IsReshopChange)
                        {
                            Reservation bookingPathReservation = new Reservation();
                            bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(session.SessionId, bookingPathReservation.ObjectName, new List<string> { session.SessionId, bookingPathReservation.ObjectName });
                            bookingPathReservation.Trips = availability.Reservation.Trips;
                            bookingPathReservation.Reshop = availability.Reservation.Reshop;
                            if (bookingPathReservation.ShopReservationInfo2 == null)
                                bookingPathReservation.ShopReservationInfo2 = new ReservationInfo2();
                            bookingPathReservation.ShopReservationInfo2.NextViewName = "RTI";
                            if (persistShop?.Request?.MOBCPCorporateDetails != null && !string.IsNullOrEmpty(persistShop.Request.MOBCPCorporateDetails.CorporateCompanyName))
                            {
                                bool isEnableSuppressingCompanyNameForBusiness = await _shoppingUtility.IsEnableSuppressingCompanyNameForBusiness(persistShop.Request.MOBCPCorporateDetails.IsPersonalized).ConfigureAwait(false);
                                if (await _shoppingUtility.IsEnableCorporateNameChange().ConfigureAwait(false))
                                {
                                    bookingPathReservation.ShopReservationInfo2.CorporateDisclaimerText
                                            = isEnableSuppressingCompanyNameForBusiness
                                                    ? string.Format(_configuration.GetValue<string>("U4BCorporateBookingDisclaimerText").ToString(), persistShop.Request.MOBCPCorporateDetails.CorporateCompanyName)
                                                    : _configuration.GetValue<string>("CorporateDisclaimerTextForBusinessTravel");
                                }
                                else
                                {
                                    bookingPathReservation.ShopReservationInfo2.CorporateDisclaimerText = string.Format(_configuration.GetValue<string>("U4BCorporateBookingDisclaimerText"), persistShop.Request.MOBCPCorporateDetails.CorporateCompanyName);
                                }
                            }

                            #region Ebulk MApp
                            var cslReservation = await _sessionHelperService.GetSession<ReservationDetail>(selectRequest.SessionId, new ReservationDetail().GetType().FullName, new List<string> { session.SessionId, new ReservationDetail().GetType().FullName });
                            if (_shoppingUtility.IsEbulkPNRReshopEnabled(selectRequest.Application.Id, selectRequest.Application.Version.Major,cslReservation)) {
                                bookingPathReservation.Taxes = null;
                                bookingPathReservation.Prices=ReShopRemoveLineItemsForEbulk(selectRequest, bookingPathReservation.Prices, cslReservation);
                            }
                            #endregion

                            await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, session.SessionId, new List<string> { session.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName);
                        }

                        #region Award Dynamic Calendar

                        bool isAwardCalendarMP2017 = _configuration.GetValue<bool>("NGRPAwardCalendarMP2017Switch");
                        if (isAwardCalendarMP2017 && session.IsAward)
                        {
                            // Save ShoppingResponse in Persist
                            await BuildAwardShopRequestAndSaveToPersist(response, selectRequest);

                        }
                        #endregion

                        #region Mileage Balance

                        if (_shoppingUtility.EnableMileageBalance(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major))
                        {
                            try
                            {
                                if (session.IsAward)
                                {
                                    Service.Presentation.CommonModel.Characteristic loyaltyId = response.Characteristics.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Code) && c.Code.Trim().Equals("LoyaltyId".Trim(), StringComparison.OrdinalIgnoreCase));
                                    if (loyaltyId != null && !string.IsNullOrWhiteSpace(loyaltyId.Value) && loyaltyId.Value.Equals(session.MileagPlusNumber))
                                    {
                                        Service.Presentation.CommonModel.Characteristic mileageBalance = response.Characteristics.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Code) && c.Code.Trim().Equals("MilesBalance".Trim(), StringComparison.OrdinalIgnoreCase));
                                        if (mileageBalance != null && !string.IsNullOrWhiteSpace(mileageBalance.Value))
                                        {
                                            if (session.IsFSRRedesign && _configuration.GetValue<bool>("EnableAwardFSRChanges") && GeneralHelper.IsApplicationVersionGreaterorEqual(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, _configuration.GetValue<string>("AndroidAwardFSRChangesVersion"), _configuration.GetValue<string>("iOSAwardFSRChangesVersion")))
                                            {
                                                availability.AvailableAwardMiles = string.Format("Mileage balance: {0} ", ShopStaticUtility.GetThousandPlaceCommaDelimitedNumberString(mileageBalance.Value));
                                                _shopBooking.GetMilesDescWithFareDiscloser(availability, session, _shoppingUtility.EnableBagCalcSelfRedirect(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major), persistShop.Request.Experiments, true);
                                            }
                                            else
                                            {
                                                availability.AvailableAwardMilesWithDesc = string.Format("Mileage balance: {0} ", ShopStaticUtility.GetThousandPlaceCommaDelimitedNumberString(mileageBalance.Value));
                                                _shopBooking.GetMilesDescWithFareDiscloser(availability, session, false, persistShop.Request.Experiments, false);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                //logEntries.Add(LogEntry.GetLogEntry(shopRequest.SessionId, "SelectTrip - Assigning mileage plus balance", "Exception", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, new MOBExceptionWrapper(ex)));
                                _logger.LogError("SelectTrip - Assigning mileage plus balance sessionId {sessionid}, Exception {@exception}, deviceId {dev}", selectRequest?.SessionId, JsonConvert.SerializeObject(ex), selectRequest?.DeviceId);
                            }
                        }

                        #endregion

                        #region FSR Result Handler
                        
                        if (_shoppingUtility.EnableFSRAlertMessages(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, session.TravelType))
                        {
                            try
                            {
                                if (!session.IsReshopChange)
                                {
                                    var shouldHandleFRS = false;

                                    if (_configuration.GetValue<bool>("EnableMetroCodeFixForMultiTrip"))
                                    {
                                        shouldHandleFRS = !isLastTripSelected && ("OW,RT,MD".IndexOf(persistShop.Request.SearchType.ToString()) > -1);
                                    }
                                    else
                                    {
                                        shouldHandleFRS = !isLastTripSelected && ("OW,RT".IndexOf(persistShop.Request.SearchType.ToString()) > -1);
                                    }

                                    // Corporate Check
                                    if (shouldHandleFRS)
                                    {
                                        availability.FSRAlertMessages = await _shopBooking.HandleFlightShoppingThatHasResults(response, persistShop.Request, isShop).ConfigureAwait(false);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                //logEntries.Add(LogEntry.GetLogEntry(shopRequest.SessionId, "SelectTrip - HandleFlightShoppingThatHasResults", "Exception", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, new MOBExceptionWrapper(ex)));
                                _logger.LogError("SelectTrip - HandleFlightShoppingThatHasResults sessionId {sessionid}, Exception {@exception}, deviceId {dev}", selectRequest?.SessionId, JsonConvert.SerializeObject(ex), selectRequest?.DeviceId);

                            }
                        
                        }

                        #endregion

                        availability.FSRAlertMessages = _shopBooking.AddMandatoryFSRAlertMessages(persistShop.Request, availability.FSRAlertMessages);
                        if (_configuration.GetValue<bool>("EnableCorporateLeisure") && session.TravelType == TravelType.CLB.ToString())
                        {
                            if (availability.FSRAlertMessages == null)
                            {
                                availability.FSRAlertMessages = new List<MOBFSRAlertMessage>();
                            }
                            var corporateLeisureOptOutAlert = await _shopBooking.GetCorporateLeisureOptOutFSRAlert(persistShop.Request, session);
                            if (corporateLeisureOptOutAlert != null)
                            {
                                availability.FSRAlertMessages.Add(corporateLeisureOptOutAlert);
                            }
                        }
                        PartnerAirlineFSRAlertMessages(selectRequest, session, availability);
                        availability.FSRAlertMessages = await _shoppingUtility.SetFSRTravelTypeAlertMessage(session, lstMessages);
                        try
                        {
                            if (await _shoppingUtility.IsEnableAdvanceSearchOfferCode(selectRequest.Application.Id, selectRequest.Application.Version.Major, session?.CatalogItems))
                            {
                                var noDiscountFareAlertMessage = _shoppingUtility.GetnoDiscoutedFareAlertMessage(response, lstMessages);
                                if (noDiscountFareAlertMessage != null)
                                {
                                    if (availability.FSRAlertMessages == null)
                                    {
                                        availability.FSRAlertMessages = new List<MOBFSRAlertMessage>();
                                    }
                                    availability.FSRAlertMessages.Add(noDiscountFareAlertMessage);
                                }                                    
                            }
                        }
                        catch(Exception ex)
                        {
                            _logger.ILoggerError("SelectTrip - NoDiscountedFareAlert sessionId {request}, Exception {exception}", selectRequest, ex);
                        }

                        availability.IsMoneyAndMilesEligible = await _shoppingUtility.IsMoneyPlusmilesEligible(response, selectRequest.Application, session?.CatalogItems);                            
                    }

                }
                else
                {
                    #region FSR No Result Handler

                    List<MOBFSRAlertMessage> alertMessages = null;

                    // Only handle this there is mp flights found but no due to CSL FS service is not downn
                    // MajorCode="20003.01"; MinorCode="10038"; Message="FLIGHTS NOT FOUND"
                    if (response != null && response.Errors != null && response.Errors.Exists(p => p.MajorCode == "20003.01" && p.MinorCode == "10038"))
                    {
                        await _shopBooking.AwardNoFlightExceptionMsg(persistShop.Request);
                        if (_shoppingUtility.EnableFSRAlertMessages(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, session.TravelType))
                        {
                            try
                            {
                                if (!session.IsReshopChange && _shoppingUtility.EnableFSRAlertMessages(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, session.TravelType))
                                {
                                    var shouldHandleFRS = false;

                                    if (_configuration.GetValue<bool>("EnableMetroCodeFixForMultiTrip"))
                                    {
                                        shouldHandleFRS = !isLastTripSelected && ("OW,RT,MD".IndexOf(persistShop.Request.SearchType.ToString()) > -1);
                                    }
                                    else
                                    {
                                        shouldHandleFRS = !isLastTripSelected && ("OW,RT".IndexOf(persistShop.Request.SearchType.ToString()) > -1);
                                    }

                                    // Corporate Check
                                    if (shouldHandleFRS)
                                    {
                                        alertMessages = await _shopBooking.HandleFlightShoppingThatHasNoResults(response, persistShop.Request, isShop);
                                    }
                                    PartnerAirlineFSRAlertMessages(selectRequest, session, availability);
                                }
                            }
                            catch (Exception ex)
                            {
                                //logEntries.Add(LogEntry.GetLogEntry(shopRequest.SessionId, "SelectTrip - HandleFlightShoppingThatHasNoResults", "Exception", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, new MOBExceptionWrapper(ex)));
                                _logger.LogError("SelectTrip - HandleFlightShoppingThatHasNoResults sessionId {sessionid}, Exception {@exception}, deviceId {dev}", selectRequest?.SessionId, JsonConvert.SerializeObject(ex), selectRequest?.DeviceId);

                            }
                        }
                    }

                    #endregion

                    if (!_shoppingUtility.EnableFSRAlertMessages(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, session.TravelType) || alertMessages == null || !alertMessages.Any())
                    {
                        if (response.Errors != null && response.Errors.Count > 0)
                        {
                            string errorMessage = string.Empty;
                            foreach (var error in response.Errors)
                            {
                                errorMessage = errorMessage + " " + error.Message;
                                //Bug 56109:ShopSelect: System.Exception - Object reference not set to an instance of an object - Ravitheja - Sep 14, 2016
                                if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10046"))
                                    throw new MOBUnitedException(_configuration.GetValue<string>("BookingExceptionMessage_ServiceErrorSessionNotFound").ToString());

                                // Added By Ali as part of Task 264624 : Select Trip - The Boombox user's session has expired
                                if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10036"))
                                {
                                    throw new MOBUnitedException(_configuration.GetValue<string>("BookingExceptionMessage_ServiceErrorSessionNotFound").ToString());
                                }

                                //  Added by Ali as part of Task 278032 : System.Exception:FLIGHTS NOT FOUND-No flight options were found for this trip.
                                if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10051"))
                                {
                                    throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError").ToString());
                                }
                            }

                            throw new System.Exception(errorMessage);
                        }
                    }
                    else
                    {
                        if (_shoppingUtility.EnableFSRAlertMessages(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, session.TravelType))
                        {
                            availability = new MOBSHOPAvailability { FSRAlertMessages = alertMessages };

                            // Offer the customer other search options due to no flights available.
                            _logger.LogInformation("SelectTrip - HandleFlightShoppingThatHasNoResults {MOBUnitedException}", "Offer the customer other search options due to no flights available.");
                        }
                    }
                }
            }
            else
            {
                throw new MOBUnitedException("The service did not return any availability.");
            }
            if (availability.Trip != null)
            {
                _shopBooking.AddAvailabilityToPersist(availability, selectRequest.SessionId);
            }

            try
            {
                if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "REST_TUNING_CALL_DURATION")
                {
                    availability.CallDuration = callTime4Tuning + cslCallDuration;
                }
            }
            catch { }


            return availability;
        }

        private United.Mobile.Model.Shopping.TravelPolicy GetCorporateOutOfPolicy(MOBSHOPShopRequest request, FlightReservationResponse response, Session session, List<CMSContentMessage> lstMessages, bool isEnableSuppressingCompanyNameForBusiness, bool isEnableCorporateNameChange)
        {
            United.Mobile.Model.Shopping.TravelPolicy outOfPolicyMessage = null;
            try
            {
                if (response != null && response.Warnings != null && response.Warnings.Count > 0 && response.Warnings.Any(x => !string.IsNullOrEmpty(x.MinorCode) && (x.MinorCode == "21700" || x.MinorCode == "21800")) && lstMessages != null && lstMessages.Count > 0)
                {
                    List<MOBMobileCMSContentMessages> travelPolicyTitle = null;
                    List<MOBMobileCMSContentMessages> travelPolicyMessage = null;
                    List<MOBMobileCMSContentMessages> travelPolicyBudget = null;
                    List<MOBMobileCMSContentMessages> travelPolicySeat = null;
                    List<MOBMobileCMSContentMessages> travelPolicyButton = null;

                    if (lstMessages != null && lstMessages.Count > 0)
                    {
                        outOfPolicyMessage = new United.Mobile.Model.Shopping.TravelPolicy();

                        travelPolicyTitle = _shoppingUtility.GetSDLMessageFromList(lstMessages, "TravelPolicy.OutOfPolicy.Title");
                        travelPolicyMessage = _shoppingUtility.GetSDLMessageFromList(lstMessages, "TravelPolicy.OutOfPolicy.Message");
                        travelPolicyBudget = _shoppingUtility.GetSDLMessageFromList(lstMessages, "TravelPolicy.OutOfPolicy.Budget");
                        travelPolicySeat = _shoppingUtility.GetSDLMessageFromList(lstMessages, "TravelPolicy.OutOfPolicy.Seat");
                        travelPolicyButton = _shoppingUtility.GetSDLMessageFromList(lstMessages, "TravelPolicy.OutOfPolicy.Button");

                        var corporateData = response.Reservation?.Travelers?.FirstOrDefault()?.CorporateData;
                        var corporateTravelPolicy = response.Reservation?.Travelers?.FirstOrDefault()?.TravelPolicies;

                        string corporateCompanyName = corporateData?.CompanyName;
                        outOfPolicyMessage.TravelPolicyTitle = travelPolicyTitle?.FirstOrDefault()?.ContentShort;
                        if (isEnableCorporateNameChange)
                        {
                            List<MOBMobileCMSContentMessages> travelPolicyCorporateBusinessNamePersonalizedTitle = null;
                            if (lstMessages != null && lstMessages.Count > 0)
                                travelPolicyCorporateBusinessNamePersonalizedTitle = _shoppingUtility.GetSDLMessageFromList(lstMessages, "TravelPolicy.CorporateBusinessNamePersonalizedTitle");

                            outOfPolicyMessage.TravelPolicyHeader = isEnableSuppressingCompanyNameForBusiness ? string.Format(travelPolicyTitle?.FirstOrDefault()?.ContentFull, corporateCompanyName) : travelPolicyCorporateBusinessNamePersonalizedTitle?.FirstOrDefault()?.ContentFull;
                            outOfPolicyMessage.TravelPolicyBody = string.Format(travelPolicyMessage?.FirstOrDefault()?.ContentFull, isEnableSuppressingCompanyNameForBusiness ? corporateCompanyName : _configuration.GetValue<string>("TravelPolicySplashScreenBodyReplaceCompanyName"));
                        }
                        else
                        {
                            outOfPolicyMessage.TravelPolicyHeader = string.Format(travelPolicyTitle?.FirstOrDefault()?.ContentFull, corporateCompanyName);
                            outOfPolicyMessage.TravelPolicyBody = string.Format(travelPolicyMessage?.FirstOrDefault()?.ContentFull, corporateCompanyName);
                        }

                        United.Service.Presentation.ReservationModel.CorporateTravelPolicy travelPolicy = corporateTravelPolicy?.FirstOrDefault();
                        United.Service.Presentation.ReservationModel.CorporateTravelCabinRestriction travelCabinRestrictions = travelPolicy?.TravelCabinRestrictions?.FirstOrDefault();

                        var U4BCorporateCabinTypes = _configuration.GetValue<string>("U4BCorporateCabinTypes").Split('|');
                        string cabinNameAllowed = string.Empty;
                        //if (travelPolicy.IsBasicEconomyAllowed.Value)
                        //cabinNameAllowed = cabinNameAllowed + _shoppingUtility.GetPolicyCabinName(U4BCorporateCabinTypes[0]) + ",";
                        if (travelCabinRestrictions.IsEconomyAllowed.Value)
                            cabinNameAllowed = !string.IsNullOrEmpty(cabinNameAllowed) ? cabinNameAllowed + ", " + U4BCorporateCabinTypes[1] : cabinNameAllowed + U4BCorporateCabinTypes[1];
                        if (travelCabinRestrictions.IsPremiumEconomyAllowed.Value)
                            cabinNameAllowed = !string.IsNullOrEmpty(cabinNameAllowed) ? cabinNameAllowed + ", " + U4BCorporateCabinTypes[2] : cabinNameAllowed + U4BCorporateCabinTypes[2];
                        if (travelCabinRestrictions.IsBusinessFirstAllowed.Value)
                            cabinNameAllowed = !string.IsNullOrEmpty(cabinNameAllowed) ? cabinNameAllowed + ", " + U4BCorporateCabinTypes[3] : cabinNameAllowed + U4BCorporateCabinTypes[3];


                        outOfPolicyMessage.TravelPolicyContent = new List<MOBSection>();
                        if (response.Warnings.Any(x => !string.IsNullOrEmpty(x.MinorCode) && x.MinorCode == "21700") && travelPolicy?.MaximumBudget > 0)
                        {
                            outOfPolicyMessage.TravelPolicyContent.Add(new MOBSection
                            {
                                Text1 = travelPolicyBudget?.FirstOrDefault()?.ContentShort?.Split('|')?.FirstOrDefault(),
                                Text2 = string.Format(travelPolicyBudget?.FirstOrDefault()?.ContentFull, travelPolicy?.MaximumBudget),
                                Text3 = travelPolicyBudget?.FirstOrDefault()?.ContentShort?.Split('|')?.LastOrDefault()
                            });
                        }
                        if (response.Warnings.Any(x => !string.IsNullOrEmpty(x.MinorCode) && x.MinorCode == "21800") && !string.IsNullOrEmpty(cabinNameAllowed))
                        {
                            outOfPolicyMessage.TravelPolicyContent.Add(new MOBSection
                            {
                                Text1 = travelPolicySeat?.FirstOrDefault()?.ContentShort?.Split('|')?.FirstOrDefault(),
                                Text2 = string.Format(travelPolicySeat?.FirstOrDefault()?.ContentFull, cabinNameAllowed),
                                Text3 = travelPolicySeat?.FirstOrDefault()?.ContentShort?.Split('|')?.LastOrDefault()
                            });
                        }
                        outOfPolicyMessage.TravelPolicyButton = new List<string>();
                        outOfPolicyMessage.TravelPolicyButton.Add(travelPolicyButton?.FirstOrDefault()?.ContentShort);
                        outOfPolicyMessage.TravelPolicyButton.Add(travelPolicyButton?.FirstOrDefault()?.ContentFull);

                        if (outOfPolicyMessage.TravelPolicyContent == null || outOfPolicyMessage.TravelPolicyContent.Count == 0)
                            outOfPolicyMessage = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("SelectTrip - CorporateOutOfPolicy sessionId {sessionid}, Exception {@exception}, deviceId {dev}", request?.SessionId, JsonConvert.SerializeObject(ex), request?.DeviceId);
            }
            return outOfPolicyMessage;
        }
        private async void PartnerAirlineFSRAlertMessages(SelectTripRequest selectRequest, Session session, MOBSHOPAvailability availability)
        {
            if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && session.CatalogItems != null && session.CatalogItems.Count > 0 &&
                    session.CatalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableNewPartnerAirlines).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableNewPartnerAirlines).ToString())?.CurrentValue == "1")
            {
                if (availability.FSRAlertMessages == null)
                    availability.FSRAlertMessages = new List<MOBFSRAlertMessage>();
                var airline = availability.Reservation?.Trips?.FirstOrDefault()?.FlattenedFlights?.FirstOrDefault()?.Flights?.FirstOrDefault(f => f.OperatingCarrier == "XE")?.OperatingCarrier;
                if (!string.IsNullOrEmpty(airline))
                {
                    List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(selectRequest, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                    var sdlMessages = GetSDLMessageFromList(lstMessages, _configuration.GetValue<string>("PartnerAirlinesOutboundMessage") + "." + airline.ToUpper());

                    availability.FSRAlertMessages.Add(new MOBFSRAlertMessage()
                    {
                        HeaderMessage = sdlMessages.ContentShort,
                        BodyMessage = sdlMessages.ContentFull,
                        MessageTypeDescription = FSRAlertMessageType.PartnerResults,
                        AlertType = MOBFSRAlertMessageType.Information.ToString()
                    });
                }
            }
        }

        private async System.Threading.Tasks.Task BuildAwardShopRequestAndSaveToPersist(United.Services.FlightShopping.Common.ShopResponse cslShopResponse, SelectTripRequest selectTripRequest, bool isCallFromShop = false)
        {
            #region Save CSL
            try
            {
                if (cslShopResponse != null && cslShopResponse.Trips != null && cslShopResponse.Trips.Count > 0)
                {
                    var persistShopRequest = await _sessionHelperService.GetSession<CSLShopRequest>(selectTripRequest.SessionId, new CSLShopRequest().ObjectName, new List<string> { selectTripRequest.SessionId, new CSLShopRequest().ObjectName }).ConfigureAwait(false);
                    var trip = cslShopResponse.Trips.FirstOrDefault(t => t.Flights.Any(f => f.Products.Any(p => p.ProductId == selectTripRequest.ProductId.ToString())));
                    United.Services.FlightShopping.Common.Trip cslTrip = null;
                    if (trip != null)
                    {
                        if (selectTripRequest.ISProductSelected && !selectTripRequest.BackButtonClick)
                        {
                            cslTrip = persistShopRequest.ShopRequest.Trips[trip.TripIndex - 1];
                            cslTrip.Origin = trip.Origin;
                            cslTrip.Destination = trip.Destination;
                            cslTrip.BBXCellIdSelected = selectTripRequest.ProductId;
                            cslTrip.BBXSolutionSetId = selectTripRequest.TripId;
                            cslTrip.BBXSession = trip.BBXSession;
                            cslTrip.Flights = new List<Flight>();
                            cslTrip.Flights = trip.Flights.CloneDeep();
                            persistShopRequest.ShopRequest.Trips[trip.TripIndex - 1] = cslTrip;
                        }
                        else
                        {
                            //Select trip back button award calendar, sending productid as trip1, and hence tripIndex
                            cslTrip = persistShopRequest.ShopRequest.Trips[trip.TripIndex];
                            cslTrip.BBXCellIdSelected = null;
                            cslTrip.BBXSolutionSetId = null;
                            cslTrip.BBXSession = null;
                            cslTrip.Flights = null;
                            persistShopRequest.ShopRequest.Trips[trip.TripIndex] = cslTrip;
                        }

                        _shopBooking.AssignCalendarLengthOfStay(selectTripRequest.LengthOfCalendar, persistShopRequest.ShopRequest);
                        await _sessionHelperService.SaveSession<CSLShopRequest>(persistShopRequest, selectTripRequest.SessionId, new List<string> { selectTripRequest.SessionId, persistShopRequest.ObjectName }, persistShopRequest.ObjectName, 30000).ConfigureAwait(false);
                    }
                    else
                    {
                        //logEntries.Add(LogEntry.GetLogEntry<string>(selectTripRequest.SessionId, "SelectTrip - BuildAwardShopRequestAndSaveToPersist", "United Exception", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, "Trips are null"));
                        throw new MOBUnitedException("Trips are null");
                    }
                }
            }
            catch (System.Exception ex)
            {
                // logEntries.Add(LogEntry.GetLogEntry<string>(selectTripRequest.SessionId, "SelectTrip - BuildAwardShopRequestAndSaveToPersist", "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, ex.Message));
                throw new MOBUnitedException("Could not find your booking session.");
            }
            #endregion
        }
        private async System.Threading.Tasks.Task PopulateCFOPReshopProducts(Session session, SelectTripRequest request)
        {
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            shoppingCart = await _sessionHelperService.GetSession<MOBShoppingCart>(session.SessionId, shoppingCart.ObjectName, new List<string> { session.SessionId, shoppingCart.ObjectName }).ConfigureAwait(false);
            var products = await _formsOfPayment.GetProductDetailsFromCartID(session, request);
            if (products != null && products.Count() > 0)
            {
                shoppingCart = shoppingCart ?? new MOBShoppingCart();
                shoppingCart.Products = new List<ProdDetail>();
                shoppingCart.Products = products;
                await _sessionHelperService.SaveSession<MOBShoppingCart>(shoppingCart, session.SessionId, new List<string> { session.SessionId, shoppingCart.ObjectName }, shoppingCart.ObjectName).ConfigureAwait(false);
            }
        }

        private bool IsYABooking(MOBSHOPReservation reservation)
        {
            if (_shoppingUtility.EnableYoungAdult() && reservation != null && reservation.ShopReservationInfo2 != null && reservation.ShopReservationInfo2.IsYATravel) return true;

            return false;
        }
        private MOBSHOPUnfinishedBooking MapUnfinishedBookingFromMOBSHOPReservation(MOBSHOPReservation reservation)
        {
            var result = new MOBSHOPUnfinishedBooking
            {
                IsELF = reservation.IsELF,
                CountryCode = reservation.PointOfSale,
                SearchType = reservation.SearchType,
                NumberOfAdults = reservation.NumberOfTravelers,
                TravelerTypes = reservation.ShopReservationInfo2.TravelerTypes,
                SearchExecutionDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time")).ToString("G")
            };
            result.Trips = reservation.Trips.Select(MapToUnfinishedBookingTripFromMOBSHOPTrip).ToList();

            return result;
        }
        private MOBSHOPUnfinishedBookingTrip MapToUnfinishedBookingTripFromMOBSHOPTrip(MOBSHOPTrip trip)
        {
            var ubTrip = new MOBSHOPUnfinishedBookingTrip
            {
                DepartDate = trip.FlattenedFlights.First().Flights.First().DepartDate,
                DepartTime = trip.FlattenedFlights.First().Flights.First().DepartTime,
                ArrivalDate = trip.FlattenedFlights.First().Flights.First().DestinationDate,
                ArrivalTime = trip.FlattenedFlights.First().Flights.First().DestinationTime,
                Destination = trip.Destination,
                Origin = trip.Origin,
                Flights = trip.FlattenedFlights.First().Flights.Select(MapToUnfinishedBookingFlightFromMOBSHOPFlight).ToList(),
            };

            return ubTrip;
        }
        private MOBSHOPUnfinishedBookingFlight MapToUnfinishedBookingFlightFromMOBSHOPFlight(MOBSHOPFlight shopFlight)
        {
            var ubFlight = new MOBSHOPUnfinishedBookingFlight
            {
                BookingCode = shopFlight.ServiceClass,
                DepartDateTime = shopFlight.DepartureDateTime,
                Origin = shopFlight.Origin,
                Destination = shopFlight.Destination,
                FlightNumber = shopFlight.FlightNumber,
                MarketingCarrier = shopFlight.MarketingCarrier,
                ProductType = shopFlight.ShoppingProducts.First().Type,
            };

            if (shopFlight.Connections != null)
                ubFlight.Connections = shopFlight.Connections.Select(MapToUnfinishedBookingFlightFromMOBSHOPFlight).ToList();

            return ubFlight;
        }
        public async Task<(MOBResReservation reservation, MOBSHOPAvailability availability)> GetShopBookingDetailsV2(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string sessionId, United.Services.FlightShopping.Common.ShopSelectRequest request, MOBSHOPAvailability availability, SelectTripRequest selectTripRequest, ShoppingResponse persistShop)
        {

            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);
            
            string logAction = session.IsReshopChange ? "ReShopBookingDetails" : "ShopBookingDetailsv2";

            MOBResReservation reservation = null;

            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }
            request.DeviceId = selectTripRequest.DeviceId;
            string jsonRequest = JsonConvert.SerializeObject(request);
            await _sessionHelperService.SaveSession<string>(jsonRequest, sessionId, new List<string> { sessionId, typeof(United.Services.FlightShopping.Common.ShopSelectRequest).FullName }, typeof(United.Services.FlightShopping.Common.ShopSelectRequest).FullName).ConfigureAwait(false);

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cSLCallDurationstopwatch1;
            cSLCallDurationstopwatch1 = new Stopwatch();
            cSLCallDurationstopwatch1.Reset();
            cSLCallDurationstopwatch1.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            var shopBookingDetailsResponse = await _flightShoppingService.ShopBookingDetails<FlightReservationResponse>(session.Token, sessionId, logAction, jsonRequest).ConfigureAwait(false);
            await _sessionHelperService.SaveSession<FlightReservationResponse>(shopBookingDetailsResponse, sessionId, new List<string> { sessionId, shopBookingDetailsResponse.GetType().FullName }, shopBookingDetailsResponse.GetType().FullName);
            List<CMSContentMessage> sdllstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(persistShop.Request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cSLCallDurationstopwatch1.IsRunning)
            {
                cSLCallDurationstopwatch1.Stop();
            }
            #endregion/***Get Call Duration Code - Venkat 03/17/2015*******
            ShoppingResponse shop = new ShoppingResponse();
            shop = await _sessionHelperService.GetSession<ShoppingResponse>(selectTripRequest.SessionId, shop.ObjectName, new List<string> { selectTripRequest.SessionId, shop.ObjectName }).ConfigureAwait(false);
            if (shop == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }

            if (shopBookingDetailsResponse != null)
            {
                if (shopBookingDetailsResponse != null && shopBookingDetailsResponse.Status.Equals(StatusType.Success) && shopBookingDetailsResponse.Reservation != null)
                {
                    var response = new FlightReservationResponse();
                    response = await _unfinishedBooking.RegisterFlights(shopBookingDetailsResponse, session, selectTripRequest);

                    #region Populate properties which are missing from RegisterFlights Response
                    _unfinishedBooking.AssignMissingPropertiesfromRegisterFlightsResponse(shopBookingDetailsResponse, response);
                    #endregion

                    if (session.IsAward && session.IsReshopChange)
                    {
                        if (response.DisplayCart != null)
                            ValidateAwardReshopMileageBalance(response.IsMileagePurchaseRequired);
                    }

                    reservation = await PopulateReservation(sessionId, response.Reservation);

                    if (response.DisplayCart != null && response.DisplayCart.DisplayPrices != null)
                    {
                        if (availability.Reservation.ShopReservationInfo2 == null)
                        {
                            availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();
                        }
                        availability.Reservation.IsSSA = _shoppingUtility.EnableSSA(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                        availability.Reservation.IsELF = response.DisplayCart.IsElf;
                        availability.Reservation.ShopReservationInfo2.IsIBELite = _shoppingUtility.IsIBELiteFare(response.DisplayCart);
                        availability.Reservation.ShopReservationInfo2.IsIBE = _shoppingUtility.IsIBEFullFare(response.DisplayCart);
                        availability.Reservation.ShopReservationInfo2.IsNonRefundableNonChangable = _shoppingUtility.IsNonRefundableNonChangable(response.DisplayCart);
                        availability.Reservation.ShopReservationInfo2.AllowAdditionalTravelers = !session.IsCorporateBooking;
                        availability.Reservation.ShopReservationInfo2.Characteristics = ShopStaticUtility.GetCharacteristics(response.Reservation);
                        availability.Reservation.ELFMessagesForRTI = await new ELFRitMetaShopMessages(_configuration, _dynamoDBService, _legalDocumentsForTitlesService, _headers).GetELFShopMessagesForRestrictions(availability.Reservation, selectTripRequest.Application.Id);
                        availability.Reservation.IsUpgradedFromEntryLevelFare = response.DisplayCart.IsUpgradedFromEntryLevelFare && !_shoppingUtility.IsIBELiteFare(response.DisplayCart.ProductCodeBeforeUpgrade);
                        availability.Reservation.PointOfSale = shop.Request.CountryCode;
                        string fareClass = await _shopBooking.GetFareClassAtShoppingRequestFromPersist(sessionId).ConfigureAwait(false);
                        List<string> flightDepartDatesForSelectedTrip = new List<string>();

                        foreach (MOBSHOPTrip shopTrip in availability.Reservation.Trips)
                        {
                            flightDepartDatesForSelectedTrip.Add(shopTrip.TripId + "|" + shopTrip.DepartDate);
                        }
                        if (_mOBSHOPDataCarrier == null)
                            _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                        MOBAdditionalItems additionalItems = new MOBAdditionalItems();

                        availability.Reservation.Trips = await PopulateTrips(_mOBSHOPDataCarrier, response.CartId, persistShop, selectTripRequest.SessionId, selectTripRequest.Application.Id, selectTripRequest.DeviceId, selectTripRequest.Application.Version.Major, shop.Request.ShowMileageDetails, response.DisplayCart.DisplayTrips, fareClass, flightDepartDatesForSelectedTrip, additionalItems).ConfigureAwait(false);
                        if (_shoppingUtility.IsEnableTaxForAgeDiversification(shop.Request.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                        {
                            availability.Reservation.Prices = _shoppingUtility.GetPrices(response.DisplayCart.DisplayPrices, availability.AwardTravel, session.SessionId, session.IsReshopChange, availability.Reservation.SearchType, displayCart: response.DisplayCart,
                                                                 appId: selectTripRequest.Application.Id, appVersion: selectTripRequest.Application.Version.Major, catalogItems: selectTripRequest.CatalogItems, shopBookingDetailsResponse: response, displayFees: response.DisplayCart.DisplayFees, session: session);
                        }
                        else
                        {
                            availability.Reservation.Prices = GetPrices(response.DisplayCart.DisplayPrices, availability.AwardTravel, session.SessionId, session.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major,
                                catalogItems: selectTripRequest.CatalogItems, shopBookingDetailsResponse: response);

                        }
                        bool isAdvanceSearchCoupon = _shopBooking.EnableAdvanceSearchCouponBooking(shop.Request.Application.Id, shop.Request.Application.Version.Major);
                        // availability.Reservation.ShopReservationInfo2.CouponDetails = isAdvanceSearchCoupon ? AddPromoCodeDetails(response.DisplayCart) : null;

                        bool U4BForMultipax = shop?.Request?.MOBCPCorporateDetails?.IsMultiPaxAllowed??false && await _featureToggles.IsEnableU4BForMultipax(selectTripRequest.Application.Id, shop.Request.Application.Version?.Major, shop?.Request?.CatalogItems);
                        AssignCorporateRate(availability.Reservation, shop.Request.IsCorporateBooking, session.IsArrangerBooking);

                        availability.Reservation.ShopReservationInfo2.IsMultiPaxAllowed = shop?.Request?.MOBCPCorporateDetails?.IsMultiPaxAllowed??false;
                        bool isU4BCorporateBookingEnable = _shoppingUtility.IsEnableU4BCorporateBooking(shop.Request.Application.Id, shop.Request.Application.Version?.Major, shop.Request.CatalogItems);
                        if (isU4BCorporateBookingEnable && session.IsCorporateBooking && shop.Request?.MOBCPCorporateDetails != null && !string.IsNullOrEmpty(shop.Request?.MOBCPCorporateDetails?.CorporateCompanyName))
                        {
                            bool isEnableSuppressingCompanyNameForBusiness = await _shoppingUtility.IsEnableSuppressingCompanyNameForBusiness(shop.Request.MOBCPCorporateDetails.IsPersonalized).ConfigureAwait(false);
                            bool isEnableCorporateNameChange = await _shoppingUtility.IsEnableCorporateNameChange().ConfigureAwait(false);
                            if (isEnableCorporateNameChange)
                            {
                                availability.Reservation.ShopReservationInfo2.CorporateDisclaimerText = isEnableSuppressingCompanyNameForBusiness ? string.Format(_configuration.GetValue<string>("U4BCorporateBookingDisclaimerText"), shop.Request.MOBCPCorporateDetails.CorporateCompanyName) : _configuration.GetValue<string>("CorporateDisclaimerTextForBusinessTravel");
                                availability.Reservation.ShopReservationInfo2.IsCorporateBusinessNamePersonalized = shop.Request.MOBCPCorporateDetails.IsPersonalized;
                            }
                            else
                                availability.Reservation.ShopReservationInfo2.CorporateDisclaimerText = string.Format(_configuration.GetValue<string>("U4BCorporateBookingDisclaimerText"), shop.Request.MOBCPCorporateDetails.CorporateCompanyName);

                            if (!(await _shoppingUtility.IsEnableU4BTravelAddONPolicy(session.IsCorporateBooking, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, shop?.Request?.CatalogItems).ConfigureAwait(false)))
                            {
                                List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(shop.Request, session.SessionId, session.Token, _configuration.GetValue<string>("U4BCorporateContentMessageGroupName"), "U4BCorporateContentMessageCache");
                                availability.CorporateOutOfPolicy = GetCorporateOutOfPolicy(shop.Request, response, session, lstMessages, isEnableSuppressingCompanyNameForBusiness, isEnableCorporateNameChange);
                            }
                        }
                       if (await _shoppingUtility.IsEnableCorporateNameChange().ConfigureAwait(false)
                                && await _shoppingUtility.IsEnablePassingPersonalizedFlagToClient().ConfigureAwait(false) 
                                && shop?.Request != null && shop.Request.MOBCPCorporateDetails != null && !string.IsNullOrEmpty(shop.Request.TravelType) && shop.Request.TravelType.Equals(TravelType.RA.ToString(), StringComparison.OrdinalIgnoreCase) 
                                && shop.Request.MOBCPCorporateDetails.IsPersonalized && !session.IsReshopChange)
                        {
                            availability.Reservation.ShopReservationInfo2.IsCorporateBusinessNamePersonalized = shop.Request.MOBCPCorporateDetails.IsPersonalized;
                        }
                        if (_shoppingUtility.EnableCovidTestFlightShopping(shop.Request.Application.Id, shop.Request.Application.Version.Major, session.IsReshopChange))
                        {
                            ShopStaticUtility.AssignCovidTestIndicator(availability.Reservation);
                        }
                        if (_configuration.GetValue<bool>("EnableIsArranger") && session.IsArrangerBooking)
                        {
                            if (availability.Reservation.ShopReservationInfo2 == null)
                                availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();

                            availability.Reservation.ShopReservationInfo2.IsArrangerBooking = true;
                        }
                        bool Is24HoursWindow = false;
                        if (_configuration.GetValue<bool>("EnableForceEPlus"))
                        {
                            if (availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT != null)
                            {
                                Is24HoursWindow = TopHelper.Is24HourWindow(Convert.ToDateTime(availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT));
                            }
                        }

                        availability.Reservation.ShopReservationInfo2.IsForceSeatMap = _shoppingUtility.IsForceSeatMapforEPlus(shop.Request.IsReshopChange, response.DisplayCart.IsElf, Is24HoursWindow, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                        bool isSupportedVersion = GeneralHelper.IsApplicationVersionGreater(shop.Request.Application.Id, shop.Request.Application.Version.Major, "AndroidBundleVersion", "IOSBundleVersion", "", "", true, _configuration);
                        if (isSupportedVersion)
                        {
                            if (_configuration.GetValue<bool>("IsEnableBundlesForBasicEconomyBooking"))
                            {
                                availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles") && !shop.Request.IsReshopChange;
                            }
                            else
                            {
                                availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles") && !(response.DisplayCart.IsElf || shop.Request.IsReshopChange || _shoppingUtility.IsIBEFullFare(response.DisplayCart));
                            }
                        }

                        if (shop.Request.IsReshopChange)
                        {
                            availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = false;
                            availability.Reservation.ShopReservationInfo2.ShouldHideBackButton = false;
                        }

                        #region 159514 - Added for inhibit booking message,177113 - 179536 BE Fare Inversion and stacking messages  

                        if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                        {
                            if (ShopStaticUtility.IdentifyInhibitWarning(response))
                            {
                                if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                                if (!availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.INHIBITBOOKING.ToString()))
                                {
                                    //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - Response with Inhibit warning ", "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, jsonResponse));
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
                                    if (!_configuration.GetValue<bool>("TurnOffBookingCutoffMinsFromCSL"))
                                    {
                                        string bookingCutOffminsFromCSL = (response?.DisplayCart?.BookingCutOffMinutes > 0) ? response.DisplayCart.BookingCutOffMinutes.ToString() : string.Empty;

                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetInhibitMessage(bookingCutOffminsFromCSL));
                                        availability.Reservation.ShopReservationInfo2.BookingCutOffMinutes = bookingCutOffminsFromCSL;

                                    }
                                    else
                                    {
                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetInhibitMessage(string.Empty));
                                    }


                                    if (_shoppingUtility.EnableBoeingDisclaimer(availability.Reservation.IsReshopChange))
                                    {
                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                                    }
                                }
                            }
                        }

                        if (_shoppingUtility.EnableBoeingDisclaimer(availability.Reservation.IsReshopChange) && _shoppingUtility.IsBoeingDisclaimer(response.DisplayCart.DisplayTrips))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                            if (!availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.BOEING737WARNING.ToString()))
                            {
                                //if (traceSwitch.TraceWarning)
                                //{
                                //    //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - Response with Inhibit warning ", "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, jsonResponse));
                                //}
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetBoeingDisclaimer());

                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                            }
                        }

                        if (_shoppingUtility.IsIBELiteFare(response.DisplayCart.ProductCodeBeforeUpgrade))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetIBELiteNonCombinableMessage());
                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                        }

                        ///202150 - Getting both messages for fare inversions trying to select mixed itinerary (Reported by Andrew)
                        ///Srini - 12/26/2017
                        ///This If condition, we can remove, when we take "BugFixToggleFor17M" toggle out and directly "response.DisplayCart.IsUpgradedFromEntryLevelFare" check to next if condition
                        if (!_configuration.GetValue<bool>("BugFixToggleFor17M") || (_configuration.GetValue<bool>("BugFixToggleFor17M") && response.DisplayCart.IsUpgradedFromEntryLevelFare))
                        {
                            if (_configuration.GetValue<bool>("EnableBEFareInversion"))
                            {

                                if (ShopStaticUtility.IdentifyBEFareInversion(response))
                                {
                                    if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                                    if (_shoppingUtility.IsNonRefundableNonChangable(response.DisplayCart.ProductCodeBeforeUpgrade))
                                    {
                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(await _shoppingUtility.GetNonRefundableNonChangableInversionMessage(shop.Request, session));
                                    }
                                    else
                                    {
                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetBEMessage());
                                    }
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                                }
                            }
                        }
                        #endregion

                        await _shoppingUtility.SetELFUpgradeMsg(availability, response?.DisplayCart?.ProductCodeBeforeUpgrade, shop.Request, session);
                        if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                        {
                            availability.Reservation.Prices.AddRange(GetPrices(response.DisplayCart.DisplayFees, false, string.Empty, appId: selectTripRequest.Application.Id, appVersion: selectTripRequest.Application.Version.Major,
                                                                                        shopBookingDetailsResponse: response));
                        }
                        //need to add close in fee to TOTAL
                        availability.Reservation.Prices = AdjustTotal(availability.Reservation.Prices);
                        if (_shoppingUtility.IsEnableTaxForAgeDiversification(shop.Request.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();

                            availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _shoppingUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices, session.IsReshopChange, appId: selectTripRequest.Application.Id, appVersion: selectTripRequest.Application.Version.Major, travelType: shop?.Request?.TravelType);

                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes.Add(ShopStaticUtility.AddFeesAfterPriceChange(response.DisplayCart.DisplayFees));
                            }
                        }
                        else
                        {
                            if (_shoppingUtility.EnableReshopMixedPTC(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                            {
                                if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                    availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();

                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _shoppingUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices, session.IsReshopChange);
                            }

                            availability.Reservation.Taxes = GetTaxAndFees(response.DisplayCart.DisplayPrices, shop.Request.NumberOfAdults, session.IsReshopChange);

                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                //combine fees into taxes so that totals are correct
                                List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> tempList = new List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice>();
                                tempList.AddRange(response.DisplayCart.DisplayPrices);
                                tempList.AddRange(response.DisplayCart.DisplayFees);
                                availability.Reservation.Taxes = GetTaxAndFees(tempList, shop.Request.NumberOfAdults, session.IsReshopChange);
                            }

                            bool hasFareRefund = false;

                            if (response.DisplayCart != null && response.DisplayCart.TravelOptions != null && response.DisplayCart.TravelOptions.Any()
                                && response.DisplayCart.TravelOptions.Any(x => string.Equals(x.Status, "REFUND", StringComparison.OrdinalIgnoreCase)))
                            {
                                var traveloptionrefundobj = RefundAmountTravelOption(response.DisplayCart.TravelOptions);
                                var traveloptionrefund = traveloptionrefundobj.FirstOrDefault(x => string.Equals(x.DisplayType, "TOTALTRAVELOPTIONREFUND", StringComparison.OrdinalIgnoreCase));
                                var totalpricerefund = availability.Reservation.Prices.FirstOrDefault(x => string.Equals(x.DisplayType, "REFUNDPRICE", StringComparison.OrdinalIgnoreCase));

                                if (totalpricerefund != null)
                                {
                                    double tempDouble1 = 0;
                                    double.TryParse(totalpricerefund.DisplayValue, out tempDouble1);
                                    if (tempDouble1 > 0)
                                        hasFareRefund = true;
                                }

                                if (traveloptionrefund != null)
                                {
                                    decimal totalrefund = (traveloptionrefund != null && totalpricerefund != null) ?
                                        Convert.ToDecimal(traveloptionrefund.DisplayValue) + Convert.ToDecimal(totalpricerefund.DisplayValue) :
                                        Convert.ToDecimal(traveloptionrefund.DisplayValue);

                                    CultureInfo ci = null;
                                    double tempDouble = 0;
                                    double.TryParse(Convert.ToString(totalrefund), out tempDouble);
                                    if (ci == null) ci = TopHelper.GetCultureInfo(traveloptionrefund.CurrencyCode);

                                    if (totalpricerefund != null)
                                    {
                                        totalpricerefund.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);
                                        totalpricerefund.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalrefund, ci, false);
                                        totalpricerefund.DisplayValue = string.Format("{0:#,0.00}", totalrefund);
                                    }
                                    else
                                    {
                                        United.Mobile.Model.Shopping.MOBSHOPPrice totalPriceRefund = new United.Mobile.Model.Shopping.MOBSHOPPrice
                                        {
                                            CurrencyCode = traveloptionrefund.CurrencyCode,
                                            DisplayType = "REFUNDPRICE",
                                            Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero),
                                            FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalrefund, ci, false),
                                            DisplayValue = string.Format("{0:#,0.00}", totalrefund)

                                        };
                                        availability.Reservation.Prices.Add(totalPriceRefund);
                                    }

                                    if (traveloptionrefundobj.Any())
                                        availability.Reservation.Prices.AddRange(traveloptionrefundobj);
                                }
                            }

                            if (!hasFareRefund)
                            {
                                availability.Reservation.Prices.RemoveWhere
                                    (x => (string.Equals(x.DisplayType, "TAXDIFFERENCE", StringComparison.OrdinalIgnoreCase)));
                                availability.Reservation.Prices.RemoveWhere
                                    (x => (string.Equals(x.DisplayType, "FAREDIFFERENCE", StringComparison.OrdinalIgnoreCase)));
                            }
                        }

                        availability.Reservation.TravelOptions = GetTravelOptions(response.DisplayCart, shop.Request.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                        availability.Reservation.Prices = _shoppingUtility.UpdatePricesForEFS(availability.Reservation, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, shop.Request.IsReshopChange);
                        if (string.IsNullOrEmpty(session.EmployeeId))
                        {
                            if (availability.AwardTravel)
                            {
                                availability.Reservation.FareLock = GetFareLockOptions(response.FareLockResponse, GeTotalFromPrices(availability.Reservation.Prices), GetFareCurrency(response.DisplayCart.DisplayPrices), availability.AwardTravel, GetFareMiles(response.DisplayCart.DisplayPrices),
                                    selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, catalogItems: selectTripRequest.CatalogItems);
                            }
                            else
                            {
                                availability.Reservation.FareLock = GetFareLockOptions(response.FareLockResponse, GetFarePrice(response.DisplayCart.DisplayPrices),
                                    GetFareCurrency(response.DisplayCart.DisplayPrices), availability.AwardTravel, GetFareMiles(response.DisplayCart.DisplayPrices), selectTripRequest.Application.Id,
                                    selectTripRequest.Application.Version.Major, catalogItems: selectTripRequest.CatalogItems);
                            }
                        }
                        _shoppingBuyMiles.AddBuyMilesFeature(request, availability, selectTripRequest, session, shopBookingDetailsResponse, response);
                        availability.PromoCodeRemoveAlertForProducts = GetFareLockAdvanceSearchCouponWarningMessageWithAncillary(availability.Reservation.FareLock, isAdvanceSearchCoupon, response.DisplayCart);
                        availability.Reservation.AwardTravel = availability.AwardTravel;

                        if (!availability.AwardTravel)
                        {
                            availability.Reservation.LMXFlights = PopulateLMX(response.CartId, persistShop, selectTripRequest.SessionId, selectTripRequest.Application.Id, selectTripRequest.DeviceId, selectTripRequest.Application.Version.Major, shop.Request.ShowMileageDetails, response.DisplayCart.DisplayTrips);
                            availability.Reservation.IneligibleToEarnCreditMessage = _configuration.GetValue<string>("IneligibleToEarnCreditMessage");
                            availability.Reservation.OaIneligibleToEarnCreditMessage = _configuration.GetValue<string>("OaIneligibleToEarnCreditMessage");
                        }

                        _shoppingUtility.SetCanadaTravelNumberDetails(availability, selectTripRequest, session);
                        availability.Reservation.IsCubaTravel = IsCubaTravelTrip(availability.Reservation);

                        if (_shoppingUtility.IsEnableCarbonEmissionsFeature(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, session.CatalogItems))
                        {
                            MOBCarbonEmissionsResponse carbonEmissionData = null;
                            if (_configuration.GetValue<bool>("EnableCarbonEmissionsFix"))
                            {
                                try
                                {
                                    carbonEmissionData = await GetCarbonEmissionsFromReferenceData(request, availability, selectTripRequest, session, shopBookingDetailsResponse);
                                    if (carbonEmissionData != null && carbonEmissionData?.CarbonEmissionData != null)
                                    {
                                        for (int i = 0; i < carbonEmissionData?.CarbonEmissionData?.Count; i++)
                                        {
                                            availability.Reservation.Trips[i].FlattenedFlights.FirstOrDefault().Flights.FirstOrDefault().CarbonEmissionData = carbonEmissionData?.CarbonEmissionData[i];
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError("GetFlightCarbonEmissionDetailsByFlight - DataAccess error{@message} {@stackTrace}", ex.Message, ex.StackTrace);
                                }

                            }
                            else
                            {
                                carbonEmissionData = await _shoppingUtility.LoadCarbonEmissionsDataFromPersist(session);
                                foreach (var trip in availability.Reservation?.Trips)
                                {
                                    foreach (var flattendFlight in trip.FlattenedFlights)
                                    {
                                        foreach (var flight in flattendFlight.Flights)
                                        {
                                            _shoppingUtility.SetCarbonEmissionDetailsForConnections(carbonEmissionData, flight);
                                        }
                                    }
                                }
                            }

                        }

                        if (availability.Reservation.IsCubaTravel)
                        {
                            MobileCMSContentRequest mobMobileCMSContentRequest = new MobileCMSContentRequest();
                            mobMobileCMSContentRequest.Application = selectTripRequest.Application;
                            mobMobileCMSContentRequest.Token = session.Token;
                            availability.Reservation.CubaTravelInfo = await GetCubaTravelResons(mobMobileCMSContentRequest);
                            availability.Reservation.CubaTravelInfo.CubaTravelTitles = await new MPDynamoDB(_configuration, _dynamoDBService, _legalDocumentsForTitlesService, _headers).GetMPPINPWDTitleMessages(new List<string> { "CUBA_TRAVEL_CONTENT" });
                        }
                        if (_shoppingUtility.IsEnabledNationalityAndResidence(shop.Request.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();
                            availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence = await IsRequireNatAndCR(availability.Reservation, selectTripRequest.Application, sessionId, selectTripRequest.DeviceId, session.Token);
                            if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence)
                            {
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityErrMsg = NationalityResidenceMsgs.NationalityErrMsg;
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ResidenceErrMsg = NationalityResidenceMsgs.ResidenceErrMsg;
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityAndResidenceErrMsg = NationalityResidenceMsgs.NationalityAndResidenceErrMsg;
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityAndResidenceHeaderMsg = NationalityResidenceMsgs.NationalityAndResidenceHeaderMsg;
                            }
                        }
                    }
                    if (await _featureToggles.IsEnableCustomerFacingCartId(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, session.CatalogItems).ConfigureAwait(false) && response?.CartRefId > 0)
                    {
                        try
                        {
                            _shoppingUtility.GetCartRefId(response.CartRefId, availability?.Reservation?.ShopReservationInfo2, sdllstMessages);
                        }
                        catch { }
                    }
                    availability.Reservation.ISFlexibleSegmentExist = IsOneFlexibleSegmentExist(availability.Reservation.Trips);
                    availability.Reservation.FlightShareMessage = GetFlightShareMessage(availability.Reservation, string.Empty);
                    availability.Reservation.IsRefundable = reservation.IsRefundable;
                    availability.Reservation.ISInternational = reservation.ISInternational;
                    //**RSA Publick Key Implmentaion*/
                    availability.Reservation.PKDispenserPublicKey = await new PKDispenserPublicKey(_configuration, _cachingService, _dPService, _pKDispenserService, _headers).GetCachedOrNewpkDispenserPublicKey(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, sessionId, session.Token, session.CatalogItems);
                    //**RSA Publick Key Implmentaion**//
                    //#region 214448 - Unaccompanied Minor Age
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo = new ReservationAgeBoundInfo();
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.MinimumAge = Convert.ToInt32(_configuration.GetValue<string>("umnrMinimumAge"));
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.UpBoundAge = Convert.ToInt32(_configuration.GetValue<string>("umnrUpBoundAge"));
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.ErrorMessage = _configuration.GetValue<string>("umnrErrorMessage");
                    //#endregion
                    availability.Reservation.CheckedbagChargebutton = availability.Reservation.IsReshopChange ? "" : _configuration.GetValue<string>("ViewCheckedBagChargesButton");
                    if (_shoppingUtility.EnableTravelerTypes(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, session.IsReshopChange)
                        && availability.Reservation.ShopReservationInfo2.TravelerTypes != null && availability.Reservation.ShopReservationInfo2.TravelerTypes.Count > 0)
                    {
                        availability.Reservation.ShopReservationInfo2.displayTravelTypes = ShopStaticUtility.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
                    }
                    if (_shoppingUtility.EnableYoungAdult(session.IsReshopChange))
                    {
                        if (IsYATravel(response.DisplayCart.DisplayTravelers))
                        {
                            availability.Reservation.ShopReservationInfo2.IsYATravel = true;

                            if (availability.Reservation.ShopReservationInfo2.displayTravelTypes != null && availability.Reservation.ShopReservationInfo2.displayTravelTypes.Count > 0)
                                availability.Reservation.ShopReservationInfo2.displayTravelTypes[0].TravelerDescription = "Young adult (18-23)";
                        }
                    }

                    if (_configuration.GetValue<bool>("EnableCorporateLeisure"))
                    {
                        if (availability.Reservation.ShopReservationInfo2 == null)
                            availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();
                        availability.Reservation.ShopReservationInfo2.TravelType = shop.Request.TravelType;
                        if (session.TravelType == TravelType.CLB.ToString())
                        {
                            if (_shoppingUtility.IsCorporateLeisureFareSelected(availability.Reservation.Trips))
                            {
                                await AddCorporateLeisureOptOutFSRAlert(availability.Reservation.ShopReservationInfo2, selectTripRequest, session);
                            }
                        }
                    }
                    if (_omniCart.IsEnableOmniCartMVP2Changes(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, true) && !session.IsReshopChange)
                    {
                        if (availability.Reservation.ShopReservationInfo2 == null)
                            availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();
                        availability.Reservation.ShopReservationInfo2.IsDisplayCart = _shoppingUtility.IsDisplayCart(session, "DisplayCartTravelTypes");
                    }
                    if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && session.CatalogItems != null && session.CatalogItems.Count > 0 &&
                    session.CatalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableNewPartnerAirlines).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableNewPartnerAirlines).ToString())?.CurrentValue == "1"
                    )
                    {
                        var airline = availability.Reservation.Trips?.FirstOrDefault()?.FlattenedFlights?.FirstOrDefault()?.Flights?.FirstOrDefault(f => _configuration.GetValue<string>("SupportedAirlinesFareComparison").Contains(f.OperatingCarrier))?.OperatingCarrier;
                        if (!string.IsNullOrEmpty(airline))
                        {
                            string operatingCarrier = airline.ToUpper() == "XE" ? "JSX" : airline;
                            List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(persistShop.Request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                            var sdlMessage = GetSDLStringMessageFromList(lstMessages, _configuration.GetValue<string>("PartnerCarrierBookingConfirmationInfoSDL"));
                            if (!string.IsNullOrEmpty(sdlMessage))
                            {
                                sdlMessage = string.Format(sdlMessage, operatingCarrier);
                            }
                            else
                            {
                                sdlMessage = string.Format(_configuration.GetValue<string>("PartnerCarrierBookingConfirmationInfo"), operatingCarrier);
                            }
                            var mobSection = new MOBSection
                            {
                                MessageType = MOBFSRAlertMessageType.Information.ToString(),
                                Text2 = sdlMessage,
                                Order = "1"
                            };
                            if (availability.Reservation.ShopReservationInfo2.ScreenAlertMessages == null)
                            {
                                availability.Reservation.ShopReservationInfo2.ScreenAlertMessages = new United.Mobile.Model.Common.MOBAlertMessages
                                {
                                    MessageType = MOBFSRAlertMessageType.Information.ToString(),
                                    HeaderMessage = "Attention",
                                    AlertMessages = new List<MOBSection>(),
                                    IsDefaultOption = true
                                };
                            }

                            availability.Reservation.ShopReservationInfo2.ScreenAlertMessages.AlertMessages.Add(mobSection);
                        }
                    }
                    #region Express checkout flow
                    if (await _shoppingUtility.IsEnabledExpressCheckoutFlow((int)persistShop?.Request?.Application.Id, persistShop?.Request?.Application.Version.Major, session?.CatalogItems).ConfigureAwait(false)
                        && availability != null
                        && availability.Reservation != null
                        && availability.Reservation.ShopReservationInfo2 != null
                        && response != null
                        && response.IsExpressCheckout)
                    {
                        try 
                        { 
                            if (availability.Reservation.FareLock != null)
                                availability.Reservation.FareLock = null;
                            availability.Reservation.ShopReservationInfo2.IsExpressCheckoutPath = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("GetShopBookingDetailsV2 - ExpressCheckout Exception {error} and SessionId {sessionId}", ex.Message, response.SessionId);
                        }
                    }
                    #endregion
                    #region Define Booking Path Persist Reservation and Save to session - Venkat 08/13/2014
                    if (_omniCart.IsEnableOmniCartMVP2Changes(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, true) && !session.IsReshopChange)
                    {
                        if (availability.Reservation.ShopReservationInfo2 == null)
                            availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();
                        availability.Reservation.ShopReservationInfo2.IsDisplayCart = _shoppingUtility.IsDisplayCart(session, GeneralHelper.IsApplicationVersionGreaterorEqual(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, _configuration.GetValue<string>("Android_EnableAwardLiveCart_AppVersion"), _configuration.GetValue<string>("iPhone_EnableAwardLiveCart_AppVersion"))
                                                                                                                        ? "DisplayCartTravelTypesWithAward"
                                                                                                                        : "DisplayCartTravelTypes");
                    }
                    session.AppID = selectTripRequest.Application.Id;
                    session.VersionID = selectTripRequest.Application.Version.Major;
                    var bookingPathReservation = await CreateBookingPathReservation(sessionId, request, availability, shop, session, response);
                    await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                    #endregion

                    if (_shoppingUtility.EnableInflightContactlessPayment(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, bookingPathReservation.IsReshopChange))
                    {
                        await FireForGetInFlightCLEligibilityCheck(bookingPathReservation, selectTripRequest, session);
                    }
                    var persistedShopBookingDetailsResponse = new ShopBookingDetailsResponse
                    {
                        SessionId = sessionId,
                        Reservation = response.Reservation
                    };

                    if (string.IsNullOrEmpty(session.EmployeeId))
                    {
                        persistedShopBookingDetailsResponse.FareLock = response.FareLockResponse;
                    }

                    await _sessionHelperService.SaveSession<ShopBookingDetailsResponse>(persistedShopBookingDetailsResponse, persistedShopBookingDetailsResponse.SessionId, new List<string> { persistedShopBookingDetailsResponse.SessionId, persistedShopBookingDetailsResponse.ObjectName }, persistedShopBookingDetailsResponse.ObjectName).ConfigureAwait(false);
                    if (_configuration.GetValue<bool>("EnableSessionForceSavePersistInCloud"))
                    {
                        await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);
                    }

                }
                else
                {
                    if (shopBookingDetailsResponse.Errors != null && shopBookingDetailsResponse.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in shopBookingDetailsResponse.Errors)
                        {
                            errorMessage = errorMessage + " " + error.Message;

                            #region 159514 - Inhibit booking error
                            if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                            {
                                if (error != null && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10050"))
                                {
                                    //throw new MOBUnitedException(error.Message);
                                    throw new MOBUnitedException(error.Message, new Exception(error.MinorCode));
                                }
                            }
                            #endregion


                            //59249:Bug 323966: Flight Shopping (Mobile) - Item 9: CSL_Service\ShopBookingDetails Unhandled Exception: System.Exception - bookingDetail has no solutions being returned from ITA - Sep 22, 2016 - Vijayan
                            if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10047"))
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("BookingDetail_ITAError_For_CSL_10047__Error_Code").ToString());
                            }

                            //67660 Bug 345403 CSL - System.Exception - Input error for "revenueBookingDetails" (search), solutionId - Sep 30,2016 - Issuf 
                            if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10048"))
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("BookingDetailsErrorMessage_For_CSL_10048__Error_Code").ToString());
                            }



                            //59232 - Bug 323968: Flight Shopping (Mobile) - Item 11: CSL_Service\ShopBookingDetails Unhandled Exception: System.Exception - Ras Check Failed - Sep 12,2016 - Ravitheja G 
                            if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10045"))
                            {
                                List<string> lstFlightDetails = null;
                                if (_configuration.GetValue<string>("RasCheckFailedErrorMessageWithFlightNumber") != null && shopBookingDetailsResponse.DisplayCart != null)
                                {
                                    lstFlightDetails = new List<string>();
                                    if (shopBookingDetailsResponse.DisplayCart != null)
                                        if (shopBookingDetailsResponse.DisplayCart.DisplayTrips != null && shopBookingDetailsResponse.DisplayCart.DisplayTrips.Count > 0)
                                            foreach (var trips in shopBookingDetailsResponse.DisplayCart.DisplayTrips)
                                            {
                                                foreach (var flightDetails in trips.Flights)
                                                {
                                                    lstFlightDetails.Add(flightDetails.MarketingCarrierDescription + " flight " + flightDetails.FlightNumber);
                                                }
                                            }
                                    if (await _featureSettings.GetFeatureSettingValue("EnableRASCheckEnhancement") == false)
                                    {
                                        throw new MOBUnitedException(string.Format(_configuration.GetValue<string>("RasCheckFailedErrorMessageWithFlightNumberOLD"), string.Join(",", lstFlightDetails)));
                                    }
                                    else
                                    {
                                        AddRASCheckAlert(availability, shopBookingDetailsResponse);
                                    }
                                }
                                else
                                {
                                    throw new MOBUnitedException(_configuration.GetValue<string>("GenericRasCheckFailedErrorMessage").ToString());
                                }
                            }

                            // OA Flash sale todo : need to update the messages
                            if ( _shoppingUtility.IsFSROAFlashSaleEnabled(session?.CatalogItems) &&
                                !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10129")
                                )
                            {
                                availability = _shoppingUtility.AddFSROAFalsSaleAlerts(availability, sdllstMessages, error);
                            }

                            // Added By Ali as part of Task 264624 : Select Trip - The Boombox user's session has expired
                            if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10036"))
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("BookingExceptionMessage_ServiceErrorSessionNotFound").ToString());
                            }

                            //  Added by Ali as part of Task 278032 : System.Exception:FLIGHTS NOT FOUND-No flight options were found for this trip.
                            if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10051"))
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError").ToString());
                            }

                        }
                        if(shopBookingDetailsResponse.Errors?.Any(a=>a.MinorCode == "10129" || a.MinorCode == "10045")  == false)
                         throw new System.Exception(errorMessage);
                    }
                    else
                    {
                        throw new MOBUnitedException("Failed to retrieve booking details.");
                    }
                }
             
            }
            else
            {
                throw new MOBUnitedException("Failed to retrieve booking details.");
            }
            if(_configuration.GetValue<bool>("EnableReservationValidation") == false && reservation != null)
                reservation.CallDuration = cSLCallDurationstopwatch1.ElapsedMilliseconds;
            return (reservation, availability);
        }

        private void AddRASCheckAlert(MOBSHOPAvailability availability, FlightReservationResponse shopBookingDetailsResponse)
        {
            if (availability == null) availability = new MOBSHOPAvailability();
            string displayPriceForOA = "";
            if (availability?.OnScreenAlerts == null)
            {
                availability.OnScreenAlerts = new List<MOBOnScreenAlert>();
            }
            List<string> lstFlightDetails = null;
                if (shopBookingDetailsResponse.DisplayCart != null)
                {
                    lstFlightDetails = new List<string>();
                    if (shopBookingDetailsResponse.DisplayCart != null)
                        if (shopBookingDetailsResponse.DisplayCart.DisplayTrips != null && shopBookingDetailsResponse.DisplayCart.DisplayTrips.Count > 0)
                            foreach (var trips in shopBookingDetailsResponse.DisplayCart.DisplayTrips)
                            {
                                foreach (var flightDetails in trips.Flights)
                                {
                                    lstFlightDetails.Add(flightDetails.MarketingCarrierDescription + " Flight " + flightDetails.FlightNumber);
                                }
                            }
                if (shopBookingDetailsResponse.DisplayCart.DisplayPrices != null && shopBookingDetailsResponse.DisplayCart.DisplayPrices.Count > 0)
                {
                    CultureInfo ci = TopHelper.GetCultureInfo(shopBookingDetailsResponse.DisplayCart.DisplayPrices.FirstOrDefault(a => a.Type == "Total")?.Currency ?? "");
                    displayPriceForOA = ShopStaticUtility.FormatAwardAmountForDisplay(shopBookingDetailsResponse.DisplayCart.DisplayPrices.Where(a => a.Type == "Miles")?.FirstOrDefault()?.Amount.ToString(), true) + " + "+ TopHelper.FormatAmountForDisplay(shopBookingDetailsResponse.DisplayCart.DisplayPrices.Where(a => a.Type == "Total")?.FirstOrDefault()?.Amount.ToString(), ci, true);
                }
                MOBOnScreenAlert mobOnScreenAlert = new MOBOnScreenAlert();
                    mobOnScreenAlert.Title = _configuration.GetValue<string>("FSRRASCheckMessageHeader");
                    mobOnScreenAlert.AlertType = MOBOnScreenAlertType.RASCHECK;
                    mobOnScreenAlert.Message = string.Format(_configuration.GetValue<string>("RasCheckFailedErrorMessageWithFlightNumber"), displayPriceForOA, string.Join("&", lstFlightDetails));
                    mobOnScreenAlert.Actions = new List<MOBOnScreenActions>();
                    mobOnScreenAlert.Actions.Add(new MOBOnScreenActions
                    {
                        ActionTitle = _configuration.GetValue<string>("FSRRASCheckActionText"),
                        ActionType = MOBOnScreenAlertActionType.NAVIGATE_BACK

                    });
                    availability.OnScreenAlerts.Add(mobOnScreenAlert);
                }            
        }
        private void AddRASCheckAlertForReshop(MOBSHOPAvailability availability, FlightReservationResponse shopBookingDetailsResponse)
        {
            if (availability == null) availability = new MOBSHOPAvailability();
            if (availability?.OnScreenAlerts == null)
            {
                availability.OnScreenAlerts = new List<MOBOnScreenAlert>();
            }
            List<string> lstFlightDetails = null;
            if (shopBookingDetailsResponse.DisplayCart != null)
            {
                lstFlightDetails = new List<string>();
                    if (shopBookingDetailsResponse.DisplayCart.DisplayTrips != null && shopBookingDetailsResponse.DisplayCart.DisplayTrips.Count > 0)
                        foreach (var trips in shopBookingDetailsResponse.DisplayCart.DisplayTrips)
                        {
                            foreach (var flightDetails in trips.Flights)
                            {
                                if (flightDetails.OperatingCarrier != null && flightDetails.OperatingCarrier != "UA") { 
                                    lstFlightDetails.Add(" Flight " + flightDetails.OperatingCarrier + flightDetails.FlightNumber);
                                }
                            }
                        }
                MOBOnScreenAlert mobOnScreenAlert = new MOBOnScreenAlert();
                mobOnScreenAlert.Title = _configuration.GetValue<string>("FSRRASCheckMessageHeader");
                mobOnScreenAlert.AlertType = MOBOnScreenAlertType.RASCHECK;
                mobOnScreenAlert.Message = (lstFlightDetails != null && lstFlightDetails.Count > 0) ? string.Format(_configuration.GetValue<string>("RasCheckFailedErrorMessageWithFlightNumber"), string.Join(" &", lstFlightDetails)) : _configuration.GetValue<string>("GenericRasCheckFailedErrorMessage");
                mobOnScreenAlert.Actions = new List<MOBOnScreenActions>();
                mobOnScreenAlert.Actions.Add(new MOBOnScreenActions
                {
                    ActionTitle = _configuration.GetValue<string>("FSRRASCheckActionText"),
                    ActionType = MOBOnScreenAlertActionType.NAVIGATE_BACK

                });
                availability.OnScreenAlerts.Add(mobOnScreenAlert);
            }
        }

        private void AddRASCheckAlert(MOBSHOPAvailability availability, List<string> lstFlightDetails)
        {
            MOBOnScreenAlert mobOnScreenAlert = new MOBOnScreenAlert();
            mobOnScreenAlert.Title = _configuration.GetValue<string>("FSRRASCheckMessageHeader");
            mobOnScreenAlert.AlertType = MOBOnScreenAlertType.RASCHECK;
            mobOnScreenAlert.Message = string.Format(_configuration.GetValue<string>("RasCheckFailedErrorMessageWithFlightNumber"), string.Join(",", lstFlightDetails));
            mobOnScreenAlert.Actions = new List<MOBOnScreenActions>();
            mobOnScreenAlert.Actions.Add(new MOBOnScreenActions
            {
                ActionTitle = _configuration.GetValue<string>("FSRRASCheckMessageHeader"),
                ActionType = MOBOnScreenAlertActionType.NAVIGATE_BACK

            });
            availability.OnScreenAlerts.Add(mobOnScreenAlert);
        }


        private async Task<(MOBResReservation reservation, MOBSHOPAvailability availability)> GetShopBookingDetails(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string sessionId, United.Services.FlightShopping.Common.ShopSelectRequest request, MOBSHOPAvailability availability, SelectTripRequest selectTripRequest, ShoppingResponse persistShop)
        {

            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);

            string logAction = session.IsReshopChange ? "ReShopBookingDetails" : "ShopBookingDetails";
            string cslEndpoint = GetCslEndpointForShopping(session.IsReshopChange);           
            MOBResReservation reservation = null;
            //string url = GetShopBookingDetailsUrl(selectRequest);
            //MOBILE-30828 Migrate to CFOP in Reshop flow.
            if (await _featureSettings.GetFeatureSettingValue("EnableRegisterFlightsForReshop").ConfigureAwait(false) && session.IsReshopChange)
            {
                if (request.Characteristics == null) request.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                request.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "ISOMNICARTFLOW", Value = "true" });
            }

            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }

            string jsonRequest = JsonConvert.SerializeObject(request);
            await _sessionHelperService.SaveSession<string>(jsonRequest, sessionId, new List<string> { sessionId, typeof(United.Services.FlightShopping.Common.ShopSelectRequest).FullName }, typeof(United.Services.FlightShopping.Common.ShopSelectRequest).FullName).ConfigureAwait(false);

            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - Request for " + logAction, "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, jsonRequest));


            //string url = string.Format("{0}/{1}", cslEndpoint, logAction);
            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - Request url for " + logAction, "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, url));

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cSLCallDurationstopwatch1;
            cSLCallDurationstopwatch1 = new Stopwatch();
            cSLCallDurationstopwatch1.Reset();
            cSLCallDurationstopwatch1.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******
            var response = await _flightShoppingService.ShopBookingDetails<FlightReservationResponse>(session.Token, sessionId, logAction, jsonRequest).ConfigureAwait(false);
            //string jsonResponse = HttpHelper.Post(url, "application/json; charset=utf-8", session.Token, jsonRequest, httpPostTimeOut, httpPostNumberOfRetry);
            await _sessionHelperService.SaveSession<FlightReservationResponse>(response, sessionId, new List<string> { sessionId, response.GetType().FullName }, response.GetType().FullName).ConfigureAwait(false);
            List<CMSContentMessage> sdllstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(persistShop.Request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cSLCallDurationstopwatch1.IsRunning)
            {
                cSLCallDurationstopwatch1.Stop();
            }
            #endregion/***Get Call Duration Code - Venkat 03/17/2015*******
            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - CSL Call Duration", "CSS/CSL-CallDuration", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, "CSLShopBookingDetails=" + (cSLCallDurationstopwatch1.ElapsedMilliseconds / (double)1000).ToString()));
            //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - Response for " + logAction, "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, jsonResponse));

            ShoppingResponse shop = new ShoppingResponse();
            shop = await _sessionHelperService.GetSession<ShoppingResponse>(selectTripRequest.SessionId, shop.ObjectName, new List<string> { selectTripRequest.SessionId, shop.ObjectName }).ConfigureAwait(false);
            if (shop == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }

            if (response != null)
            {
                //FlightReservationResponse response = JsonConvert.DeserializeObject<FlightReservationResponse>(jsonResponse);
                //  United.Persist.FilePersist.Save<FlightReservationResponse>(session.SessionId, response.GetType().FullName, response);
                if (response != null && response.Status.Equals(StatusType.Success) && response.Reservation != null)
                {
                    if (session.IsAward && session.IsReshopChange)
                    {
                        if (response.DisplayCart != null)
                            ValidateAwardReshopMileageBalance(response.IsMileagePurchaseRequired);
                    }

                    if (await _featureSettings.GetFeatureSettingValue("EnableRegisterFlightsForReshop").ConfigureAwait(false) && session.IsReshopChange)
                    {
                        var res = new FlightReservationResponse();
                        res = await _unfinishedBooking.RegisterFlights(response, session, selectTripRequest);
                    }

                    reservation = await PopulateReservation(sessionId, response.Reservation);

                    if (response.DisplayCart != null && response.DisplayCart.DisplayPrices != null)
                    {
                        if (availability.Reservation.ShopReservationInfo2 == null)
                        {
                            availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();
                        }
                        availability.Reservation.IsSSA = _shoppingUtility.EnableSSA(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                        availability.Reservation.IsELF = response.DisplayCart.IsElf;
                        availability.Reservation.ShopReservationInfo2.IsIBELite = _shoppingUtility.IsIBELiteFare(response.DisplayCart);
                        availability.Reservation.ShopReservationInfo2.IsIBE = _shoppingUtility.IsIBEFullFare(response.DisplayCart);
                        availability.Reservation.ShopReservationInfo2.IsNonRefundableNonChangable = _shoppingUtility.IsNonRefundableNonChangable(response.DisplayCart);
                        availability.Reservation.ShopReservationInfo2.AllowAdditionalTravelers = !session.IsCorporateBooking;
                        availability.Reservation.ShopReservationInfo2.Characteristics = ShopStaticUtility.GetCharacteristics(response.Reservation);
                        availability.Reservation.ELFMessagesForRTI = await new ELFRitMetaShopMessages(_configuration, _dynamoDBService, _legalDocumentsForTitlesService, _headers).GetELFShopMessagesForRestrictions(availability.Reservation, selectTripRequest.Application.Id);
                        availability.Reservation.IsUpgradedFromEntryLevelFare = response.DisplayCart.IsUpgradedFromEntryLevelFare && !_shoppingUtility.IsIBELiteFare(response.DisplayCart.ProductCodeBeforeUpgrade);
                        availability.Reservation.PointOfSale = shop.Request.CountryCode;
                        string fareClass = await GetFareClassAtShoppingRequestFromPersist(sessionId);
                        List<string> flightDepartDatesForSelectedTrip = new List<string>();
                        foreach (MOBSHOPTrip shopTrip in availability.Reservation.Trips)
                        {
                            flightDepartDatesForSelectedTrip.Add(shopTrip.TripId + "|" + shopTrip.DepartDate);
                        }
                        if (_mOBSHOPDataCarrier == null)
                            _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                        availability.Reservation.Trips = await PopulateTrips(_mOBSHOPDataCarrier, response.CartId, persistShop, selectTripRequest.SessionId, selectTripRequest.Application.Id, selectTripRequest.DeviceId, selectTripRequest.Application.Version.Major, shop.Request.ShowMileageDetails, response.DisplayCart.DisplayTrips, fareClass, flightDepartDatesForSelectedTrip).ConfigureAwait(false);
                        if (_shoppingUtility.IsEnableTaxForAgeDiversification(shop.Request.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                        {
                            availability.Reservation.Prices = _shoppingUtility.GetPrices(response.DisplayCart.DisplayPrices, availability.AwardTravel, session.SessionId, session.IsReshopChange, availability.Reservation.SearchType);

                        }
                        else
                        {
                            availability.Reservation.Prices = GetPrices(response.DisplayCart.DisplayPrices, availability.AwardTravel, session.SessionId, session.IsReshopChange);

                        }
                        AssignCorporateRate(availability.Reservation, shop.Request.IsCorporateBooking, session.IsArrangerBooking);
                        if (_shoppingUtility.EnableCovidTestFlightShopping(shop.Request.Application.Id, shop.Request.Application.Version.Major, session.IsReshopChange))
                        {
                            ShopStaticUtility.AssignCovidTestIndicator(availability.Reservation);
                        }
                        if (_configuration.GetValue<bool>("EnableIsArranger") && session.IsArrangerBooking)
                        {
                            if (availability.Reservation.ShopReservationInfo2 == null)
                                availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();

                            availability.Reservation.ShopReservationInfo2.IsArrangerBooking = true;
                        }
                        bool Is24HoursWindow = false;
                        if (_configuration.GetValue<bool>("EnableForceEPlus"))
                        {
                            if (availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT != null)
                            {
                                Is24HoursWindow = TopHelper.Is24HourWindow(Convert.ToDateTime(availability.Reservation.Trips[0].FlattenedFlights[0].Flights[0].DepartureDateTimeGMT));
                            }
                        }

                        availability.Reservation.ShopReservationInfo2.IsForceSeatMap = _shoppingUtility.IsForceSeatMapforEPlus(shop.Request.IsReshopChange, response.DisplayCart.IsElf, Is24HoursWindow, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                        bool isSupportedVersion = GeneralHelper.IsApplicationVersionGreater(shop.Request.Application.Id, shop.Request.Application.Version.Major, "AndroidBundleVersion", "IOSBundleVersion", "", "", true, _configuration);
                        if (isSupportedVersion)
                        {
                            if (_configuration.GetValue<bool>("IsEnableBundlesForBasicEconomyBooking"))
                            {
                                availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles") && !shop.Request.IsReshopChange;
                            }
                            else
                            {
                                availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = _configuration.GetValue<bool>("EnableDynamicBundles") && !(response.DisplayCart.IsElf || shop.Request.IsReshopChange || _shoppingUtility.IsIBEFullFare(response.DisplayCart));
                            }
                        }

                        if (shop.Request.IsReshopChange)
                        {
                            availability.Reservation.ShopReservationInfo2.IsShowBookingBundles = false;
                            availability.Reservation.ShopReservationInfo2.ShouldHideBackButton = false;
                        }

                        #region 159514 - Added for inhibit booking message,177113 - 179536 BE Fare Inversion and stacking messages  

                        if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                        {
                            if (ShopStaticUtility.IdentifyInhibitWarning(response))
                            {
                                if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                                if (!availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.INHIBITBOOKING.ToString()))
                                {
                                    //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - Response with Inhibit warning ", "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, jsonResponse));
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                                    if (!_configuration.GetValue<bool>("TurnOffBookingCutoffMinsFromCSL"))
                                    {
                                        string bookingCutOffminsFromCSL = (response?.DisplayCart?.BookingCutOffMinutes > 0) ? response.DisplayCart.BookingCutOffMinutes.ToString() : string.Empty;

                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetInhibitMessage(bookingCutOffminsFromCSL));
                                        availability.Reservation.ShopReservationInfo2.BookingCutOffMinutes = bookingCutOffminsFromCSL;

                                    }
                                    else
                                    {
                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetInhibitMessage(string.Empty));
                                    }

                                    if (_shoppingUtility.EnableBoeingDisclaimer(availability.Reservation.IsReshopChange))
                                    {
                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                                    }
                                }
                            }
                        }

                        if (_shoppingUtility.EnableBoeingDisclaimer(availability.Reservation.IsReshopChange) && _shoppingUtility.IsBoeingDisclaimer(response.DisplayCart.DisplayTrips))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                            if (!availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Exists(m => m.Order == MOBINFOWARNINGMESSAGEORDER.BOEING737WARNING.ToString()))
                            {
                                //if (traceSwitch.TraceWarning)
                                //{
                                //    //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetShopBookingDetails - Response with Inhibit warning ", "Trace", selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, jsonResponse));
                                //}
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetBoeingDisclaimer());

                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                            }
                        }

                        if (_shoppingUtility.IsIBELiteFare(response.DisplayCart.ProductCodeBeforeUpgrade))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetIBELiteNonCombinableMessage());
                            availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                        }

                        ///202150 - Getting both messages for fare inversions trying to select mixed itinerary (Reported by Andrew)
                        ///Srini - 12/26/2017
                        ///This If condition, we can remove, when we take "BugFixToggleFor17M" toggle out and directly "response.DisplayCart.IsUpgradedFromEntryLevelFare" check to next if condition
                        if (!_configuration.GetValue<bool>("BugFixToggleFor17M") || (_configuration.GetValue<bool>("BugFixToggleFor17M") && response.DisplayCart.IsUpgradedFromEntryLevelFare))
                        {
                            if (_configuration.GetValue<bool>("EnableBEFareInversion"))
                            {

                                if (ShopStaticUtility.IdentifyBEFareInversion(response))
                                {
                                    if (availability.Reservation.ShopReservationInfo2.InfoWarningMessages == null)
                                        availability.Reservation.ShopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();

                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages.Add(_shoppingUtility.GetBEMessage());
                                    availability.Reservation.ShopReservationInfo2.InfoWarningMessages = availability.Reservation.ShopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
                                }
                            }
                        }
                        #endregion
                        await _shoppingUtility.SetELFUpgradeMsg(availability, response?.DisplayCart?.ProductCodeBeforeUpgrade, shop.Request, session);
                        if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                        {
                            availability.Reservation.Prices.AddRange(GetPrices(response.DisplayCart.DisplayFees, false, string.Empty));
                        }
                        if (_shoppingUtility.IsBuyMilesFeatureEnabled(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major) && response != null
                            && session.IsAward)
                        {
                            string webShareToken = string.Empty;
                            if (response?.IsMileagePurchaseRequired == true && response?.IsPurchaseIneligible == true)
                                webShareToken = _dPService.GetSSOTokenString(selectTripRequest.Application.Id, session.MileagPlusNumber, _configuration)?.ToString();
                            await DisplayBuyMiles(availability.Reservation, response, session,
                                  selectTripRequest, webShareToken);
                        }
                        //need to add close in fee to TOTAL
                        availability.Reservation.Prices = AdjustTotal(availability.Reservation.Prices);
                        if (_shoppingUtility.IsEnableTaxForAgeDiversification(shop.Request.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();

                            availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _shoppingUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices, session.IsReshopChange);

                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes.Add(ShopStaticUtility.AddFeesAfterPriceChange(response.DisplayCart.DisplayFees));
                            }
                        }
                        else
                        {
                            if (_shoppingUtility.EnableReshopMixedPTC(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                            {
                                if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                    availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();

                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ComplianceTaxes = _shoppingUtility.GetTaxAndFeesAfterPriceChange(response.DisplayCart.DisplayPrices, session.IsReshopChange);
                            }

                            availability.Reservation.Taxes = GetTaxAndFees(response.DisplayCart.DisplayPrices, shop.Request.NumberOfAdults, session.IsReshopChange);

                            if (response.DisplayCart.DisplayFees != null && response.DisplayCart.DisplayFees.Count > 0)
                            {
                                //combine fees into taxes so that totals are correct
                                List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> tempList = new List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice>();
                                tempList.AddRange(response.DisplayCart.DisplayPrices);
                                tempList.AddRange(response.DisplayCart.DisplayFees);
                                availability.Reservation.Taxes = GetTaxAndFees(tempList, shop.Request.NumberOfAdults, session.IsReshopChange);
                            }


                            if (response.DisplayCart != null && response.DisplayCart.TravelOptions != null && response.DisplayCart.TravelOptions.Any()
                                && response.DisplayCart.TravelOptions.Any(x => string.Equals(x.Status, "REFUND", StringComparison.OrdinalIgnoreCase)))
                            {
                                bool hasFareRefund = false;

                                var traveloptionrefundobj = RefundAmountTravelOption(response.DisplayCart.TravelOptions);
                                var traveloptionrefund = traveloptionrefundobj.FirstOrDefault(x => string.Equals(x.DisplayType, "TOTALTRAVELOPTIONREFUND", StringComparison.OrdinalIgnoreCase));
                                var totalpricerefund = availability.Reservation.Prices.FirstOrDefault(x => string.Equals(x.DisplayType, "REFUNDPRICE", StringComparison.OrdinalIgnoreCase));

                                if (totalpricerefund != null)
                                {
                                    double tempDouble1 = 0;
                                    double.TryParse(totalpricerefund.DisplayValue, out tempDouble1);
                                    if (tempDouble1 > 0)
                                        hasFareRefund = true;
                                }

                                if (traveloptionrefund != null)
                                {
                                    decimal totalrefund = (traveloptionrefund != null && totalpricerefund != null) ?
                                        Convert.ToDecimal(traveloptionrefund.DisplayValue) + Convert.ToDecimal(totalpricerefund.DisplayValue) :
                                        Convert.ToDecimal(traveloptionrefund.DisplayValue);

                                    CultureInfo ci = null;
                                    double tempDouble = 0;
                                    double.TryParse(Convert.ToString(totalrefund), out tempDouble);
                                    if (ci == null) ci = TopHelper.GetCultureInfo(traveloptionrefund.CurrencyCode);

                                    if (totalpricerefund != null)
                                    {
                                        totalpricerefund.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);
                                        totalpricerefund.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalrefund, ci, false);
                                        totalpricerefund.DisplayValue = string.Format("{0:#,0.00}", totalrefund);
                                    }
                                    else
                                    {
                                        Model.Shopping.MOBSHOPPrice totalPriceRefund = new Model.Shopping.MOBSHOPPrice
                                        {
                                            CurrencyCode = traveloptionrefund.CurrencyCode,
                                            DisplayType = "REFUNDPRICE",
                                            Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero),
                                            FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalrefund, ci, false),
                                            DisplayValue = string.Format("{0:#,0.00}", totalrefund)

                                        };
                                        availability.Reservation.Prices.Add(totalPriceRefund);
                                    }

                                    if (traveloptionrefundobj.Any())
                                        availability.Reservation.Prices.AddRange(traveloptionrefundobj);
                                }

                                if (!hasFareRefund)
                                {
                                    availability.Reservation.Prices.RemoveWhere
                                        (x => (string.Equals(x.DisplayType, "TAXDIFFERENCE", StringComparison.OrdinalIgnoreCase)));
                                    availability.Reservation.Prices.RemoveWhere
                                        (x => (string.Equals(x.DisplayType, "FAREDIFFERENCE", StringComparison.OrdinalIgnoreCase)));
                                }
                            }
                        }

                        availability.Reservation.TravelOptions = GetTravelOptions(response.DisplayCart, session.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);

                        if (string.IsNullOrEmpty(session.EmployeeId))
                        {
                            if (availability.AwardTravel)
                            {
                                availability.Reservation.FareLock = GetFareLockOptions(response.FareLockResponse, GeTotalFromPrices(availability.Reservation.Prices), GetFareCurrency(response.DisplayCart.DisplayPrices), availability.AwardTravel, GetFareMiles(response.DisplayCart.DisplayPrices), selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                            }
                            else
                            {
                                availability.Reservation.FareLock = GetFareLockOptions(response.FareLockResponse, GetFarePrice(response.DisplayCart.DisplayPrices), GetFareCurrency(response.DisplayCart.DisplayPrices), availability.AwardTravel, GetFareMiles(response.DisplayCart.DisplayPrices), selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major);
                            }
                        }
                        availability.Reservation.AwardTravel = availability.AwardTravel;

                        if (!availability.AwardTravel)
                        {
                            availability.Reservation.LMXFlights = PopulateLMX(response.CartId, persistShop, selectTripRequest.SessionId, selectTripRequest.Application.Id, selectTripRequest.DeviceId, selectTripRequest.Application.Version.Major, shop.Request.ShowMileageDetails, response.DisplayCart.DisplayTrips);
                            availability.Reservation.IneligibleToEarnCreditMessage = _configuration.GetValue<string>("IneligibleToEarnCreditMessage");
                            availability.Reservation.OaIneligibleToEarnCreditMessage = _configuration.GetValue<string>("OaIneligibleToEarnCreditMessage");
                        }

                        availability.Reservation.IsCubaTravel = IsCubaTravelTrip(availability.Reservation);
                        if (_shoppingUtility.IsEnableCarbonEmissionsFeature(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, session.CatalogItems))
                        {
                            MOBCarbonEmissionsResponse carbonEmissionData = null;
                            if (_configuration.GetValue<bool>("EnableCarbonEmissionsFix"))
                            {
                                try
                                {
                                    carbonEmissionData = await GetCarbonEmissionsFromReferenceData(request, availability, selectTripRequest, session, response);
                                    if (carbonEmissionData != null && carbonEmissionData.CarbonEmissionData.Any())
                                    {
                                        for (int i = 0; i < carbonEmissionData?.CarbonEmissionData?.Count; i++)
                                        {
                                            availability.Reservation.Trips[i].FlattenedFlights.FirstOrDefault().Flights.FirstOrDefault().CarbonEmissionData = carbonEmissionData?.CarbonEmissionData[i];
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError("GetFlightCarbonEmissionDetailsByFlight - DataAccess error{@message} {@stackTrace}", ex.Message, ex.StackTrace);
                                }
                            }
                            else
                            {
                                carbonEmissionData = await _shoppingUtility.LoadCarbonEmissionsDataFromPersist(session);
                                foreach (var trip in availability.Reservation?.Trips)
                                {
                                    foreach (var flattendFlight in trip.FlattenedFlights)
                                    {
                                        foreach (var flight in flattendFlight.Flights)
                                        {
                                            _shoppingUtility.SetCarbonEmissionDetailsForConnections(carbonEmissionData, flight);
                                        }
                                    }
                                }
                            }

                        }
                        if (availability.Reservation.IsCubaTravel)
                        {
                            MobileCMSContentRequest mobMobileCMSContentRequest = new MobileCMSContentRequest();
                            mobMobileCMSContentRequest.Application = selectTripRequest.Application;
                            mobMobileCMSContentRequest.Token = session.Token;
                            availability.Reservation.CubaTravelInfo = await _travelerCSL.GetCubaTravelResons(mobMobileCMSContentRequest);
                            availability.Reservation.CubaTravelInfo.CubaTravelTitles = await new MPDynamoDB(_configuration, _dynamoDBService, null, _headers).GetMPPINPWDTitleMessages(new List<string> { "CUBA_TRAVEL_CONTENT" });
                        }
                        if (_shoppingUtility.IsEnabledNationalityAndResidence(shop.Request.IsReshopChange, selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major))
                        {
                            if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence == null)
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence = new InfoNationalityAndResidence();
                            availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence = await IsRequireNatAndCR(availability.Reservation, selectTripRequest.Application, sessionId, selectTripRequest.DeviceId, session.Token);
                            if (availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.IsRequireNationalityAndResidence)
                            {
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityErrMsg = NationalityResidenceMsgs.NationalityErrMsg;
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.ResidenceErrMsg = NationalityResidenceMsgs.ResidenceErrMsg;
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityAndResidenceErrMsg = NationalityResidenceMsgs.NationalityAndResidenceErrMsg;
                                availability.Reservation.ShopReservationInfo2.InfoNationalityAndResidence.NationalityAndResidenceHeaderMsg = NationalityResidenceMsgs.NationalityAndResidenceHeaderMsg;
                            }
                        }
                    }
                    availability.Reservation.ISFlexibleSegmentExist = IsOneFlexibleSegmentExist(availability.Reservation.Trips);
                    availability.Reservation.FlightShareMessage = GetFlightShareMessage(availability.Reservation, string.Empty);
                    availability.Reservation.IsRefundable = reservation.IsRefundable;
                    availability.Reservation.ISInternational = reservation.ISInternational;
                    //**RSA Publick Key Implmentaion*/

                    availability.Reservation.PKDispenserPublicKey = await _pKDispenserPublicKey.GetCachedOrNewpkDispenserPublicKey(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, selectTripRequest.DeviceId, selectTripRequest.TransactionId, session.Token, session.CatalogItems);

                    //**RSA Publick Key Implmentaion**//

                    //#region 214448 - Unaccompanied Minor Age
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo = new ReservationAgeBoundInfo();
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.MinimumAge = Convert.ToInt32(_configuration.GetValue<string>("umnrMinimumAge"));
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.UpBoundAge = Convert.ToInt32(_configuration.GetValue<string>("umnrUpBoundAge"));
                    availability.Reservation.ShopReservationInfo2.ReservationAgeBoundInfo.ErrorMessage = _configuration.GetValue<string>("umnrErrorMessage");
                    //#endregion
                    availability.Reservation.CheckedbagChargebutton = availability.Reservation.IsReshopChange ? "" : _configuration.GetValue<string>("ViewCheckedBagChargesButton");
                    if (_shoppingUtility.EnableTravelerTypes(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, session.IsReshopChange)
                        && availability.Reservation.ShopReservationInfo2.TravelerTypes != null && availability.Reservation.ShopReservationInfo2.TravelerTypes.Count > 0)
                    {
                        availability.Reservation.ShopReservationInfo2.displayTravelTypes = ShopStaticUtility.GetDisplayTravelerTypes(response.DisplayCart.DisplayTravelers);
                    }
                    if (_shoppingUtility.EnableYoungAdult(session.IsReshopChange))
                    {
                        if (IsYATravel(response.DisplayCart.DisplayTravelers))
                        {
                            availability.Reservation.ShopReservationInfo2.IsYATravel = true;

                            if (availability.Reservation.ShopReservationInfo2.displayTravelTypes != null && availability.Reservation.ShopReservationInfo2.displayTravelTypes.Count > 0)
                                availability.Reservation.ShopReservationInfo2.displayTravelTypes[0].TravelerDescription = "Young adult (18-23)";

                        }
                    }
                    if (_omniCart.IsEnableOmniCartMVP2Changes(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, true) && !session.IsReshopChange)
                    {
                        if (availability.Reservation.ShopReservationInfo2 == null)
                            availability.Reservation.ShopReservationInfo2 = new ReservationInfo2();
                        availability.Reservation.ShopReservationInfo2.IsDisplayCart = _shoppingUtility.IsDisplayCart(session, GeneralHelper.IsApplicationVersionGreaterorEqual(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, _configuration.GetValue<string>("Android_EnableAwardLiveCart_AppVersion"), _configuration.GetValue<string>("iPhone_EnableAwardLiveCart_AppVersion"))
                                                                                                                        ? "DisplayCartTravelTypesWithAward"
                                                                                                                        : "DisplayCartTravelTypes");
                    }
                    if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && session.CatalogItems != null && session.CatalogItems.Count > 0 &&
                   session.CatalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableNewPartnerAirlines).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableNewPartnerAirlines).ToString())?.CurrentValue == "1"
                 )
                    {
                        var airline = availability.Reservation.Trips?.FirstOrDefault()?.FlattenedFlights?.FirstOrDefault()?.Flights?.FirstOrDefault(f => _configuration.GetValue<string>("SupportedAirlinesFareComparison").Contains(f.OperatingCarrier))?.OperatingCarrier;
                        if (!string.IsNullOrEmpty(airline))
                        {
                            string operatingCarrier = airline.ToUpper() == "XE" ? "JSX" : airline;
                            List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(persistShop.Request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                            var sdlMessage = GetSDLStringMessageFromList(lstMessages, _configuration.GetValue<string>("PartnerCarrierBookingConfirmationInfoSDL"));
                            if (!string.IsNullOrEmpty(sdlMessage))
                            {
                                sdlMessage = string.Format(sdlMessage, operatingCarrier);
                            }
                            else
                            {
                                sdlMessage = string.Format(_configuration.GetValue<string>("PartnerCarrierBookingConfirmationInfo"), operatingCarrier);
                            }
                            var mobSection = new MOBSection
                            {
                                MessageType = MOBFSRAlertMessageType.Information.ToString(),
                                Text2 = sdlMessage,
                                Order = "1"
                            };
                            if (availability.Reservation.ShopReservationInfo2.ScreenAlertMessages == null)
                            {
                                availability.Reservation.ShopReservationInfo2.ScreenAlertMessages = new United.Mobile.Model.Common.MOBAlertMessages
                                {
                                    MessageType = MOBFSRAlertMessageType.Information.ToString(),
                                    HeaderMessage = "Attention",
                                    AlertMessages = new List<MOBSection>(),
                                    IsDefaultOption = true
                                };
                            }

                            availability.Reservation.ShopReservationInfo2.ScreenAlertMessages.AlertMessages.Add(mobSection);
                        }
                    }

                    #region Define Booking Path Persist Reservation and Save to session - Venkat 08/13/2014                    
                    session.AppID = selectTripRequest.Application.Id;
                    session.VersionID = selectTripRequest.Application.Version.Major;

                    var bookingPathReservation
                        = await CreateBookingPathReservation(sessionId, request, availability, shop, session, response);

                    //Adding FFCResidual data - TODO hardcoding/mapping
                    if (session.IsReshopChange
                        && _shoppingUtility.IncludeReshopFFCResidual
                        (selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major)
                        && availability?.Reservation?.Reshop != null && availability.Reservation.Reshop.IsResidualFFCRAvailable)
                    {
                        AssignFFCResidualForTravelerV2(availability, response);
                    }

                    await _sessionHelperService.SaveSession<Reservation>(bookingPathReservation, bookingPathReservation.SessionId, new List<string> { bookingPathReservation.SessionId, bookingPathReservation.ObjectName }, bookingPathReservation.ObjectName).ConfigureAwait(false);
                    #endregion

                    var persistedShopBookingDetailsResponse = new ShopBookingDetailsResponse
                    {
                        SessionId = sessionId,
                        Reservation = response.Reservation
                    };

                    if (string.IsNullOrEmpty(session.EmployeeId))
                    {
                        persistedShopBookingDetailsResponse.FareLock = response.FareLockResponse;
                    }
                    await _sessionHelperService.SaveSession<ShopBookingDetailsResponse>(persistedShopBookingDetailsResponse, persistedShopBookingDetailsResponse.SessionId, new List<string> { persistedShopBookingDetailsResponse.SessionId, persistedShopBookingDetailsResponse.ObjectName }, persistedShopBookingDetailsResponse.ObjectName).ConfigureAwait(false);
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.Message;

                            #region 159514 - Inhibit booking error
                            if (_configuration.GetValue<bool>("EnableInhibitBooking"))
                            {
                                if (error != null && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10050"))
                                {
                                    //throw new MOBUnitedException(error.Message);
                                    throw new MOBUnitedException(error.Message, new Exception(error.MinorCode));
                                }
                            }
                            #endregion

                            //59249:Bug 323966: Flight Shopping (Mobile) - Item 9: CSL_Service\ShopBookingDetails Unhandled Exception: System.Exception - bookingDetail has no solutions being returned from ITA - Sep 22, 2016 - Vijayan
                            if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10047"))
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("BookingDetail_ITAError_For_CSL_10047__Error_Code"));
                            }

                            //67660 Bug 345403 CSL - System.Exception - Input error for "revenueBookingDetails" (search), solutionId - Sep 30,2016 - Issuf 
                            if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10048"))
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("BookingDetailsErrorMessage_For_CSL_10048__Error_Code"));
                            }

                            bool enableRASCheckEnhancementForReshop = await _featureSettings.GetFeatureSettingValue("EnableRASCheckEnhancementForReshop").ConfigureAwait(false) && GeneralHelper.IsApplicationVersionGreaterorEqual(persistShop.Request.Application.Id, persistShop.Request.Application.Version.Major, _configuration.GetValue<string>("RASCheck_Version_Android"), _configuration.GetValue<string>("RASCheck_Version_iOS"));
                            //59232 - Bug 323968: Flight Shopping (Mobile) - Item 11: CSL_Service\ShopBookingDetails Unhandled Exception: System.Exception - Ras Check Failed - Sep 12,2016 - Ravitheja G 
                            if (!enableRASCheckEnhancementForReshop && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10045"))
                            {
                                List<string> lstFlightDetails = null;
                                if (_configuration.GetValue<string>("RasCheckFailedErrorMessageWithFlightNumber") != null && response.DisplayCart != null)
                                {
                                    lstFlightDetails = new List<string>();
                                    if (response.DisplayCart != null)
                                        if (response.DisplayCart.DisplayTrips != null && response.DisplayCart.DisplayTrips.Count > 0)
                                            foreach (var trips in response.DisplayCart.DisplayTrips)
                                            {
                                                foreach (var flightDetails in trips.Flights)
                                                {
                                                    lstFlightDetails.Add(flightDetails.MarketingCarrierDescription + " flight " + flightDetails.FlightNumber);
                                                }
                                            }
                                    throw new MOBUnitedException(string.Format(_configuration.GetValue<string>("RasCheckFailedErrorMessageWithFlightNumber"), string.Join(",", lstFlightDetails)));
                                }
                                else
                                {
                                    throw new MOBUnitedException(_configuration.GetValue<string>("GenericRasCheckFailedErrorMessage"));
                                }
                            }
                            if (enableRASCheckEnhancementForReshop && response.Errors != null && response.Errors.Count > 0 && response.Errors.Exists(p => p.MajorCode.Trim().Equals("20003.44") && p.MinorCode.Trim().Equals("10045")))
                            {
                                try
                                {
                                    AddRASCheckAlertForReshop(availability, response);
                                }
                                catch
                                {
                                    throw new MOBUnitedException(_configuration.GetValue<string>("GenericRasCheckFailedErrorMessage"));
                                }
                            }

                            // OA Flash sale todo : need to update the messages
                            if (_shoppingUtility.IsFSROAFlashSaleEnabledInReShop(session?.CatalogItems) &&
                                !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10129")
                                )
                            {
                                availability = _shoppingUtility.AddFSROAFalsSaleAlertsInReshop(availability, sdllstMessages, error);
                            }

                            // Added By Ali as part of Task 264624 : Select Trip - The Boombox user's session has expired
                            if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10036"))
                            {
                                 throw new MOBUnitedException(_configuration.GetValue<string>("BookingExceptionMessage_ServiceErrorSessionNotFound"));
                            }

                            //  Added by Ali as part of Task 278032 : System.Exception:FLIGHTS NOT FOUND-No flight options were found for this trip.
                            if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10051"))
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError"));
                            }
                        }
                        if (response.Errors?.Any(a => a.MinorCode == "10129" || a.MinorCode == "10045") == false)
                            throw new System.Exception(errorMessage);
                    }
                    else
                    {
                        throw new MOBUnitedException("Failed to retrieve booking details.");
                    }
                }
            }
            else
            {
                throw new MOBUnitedException("Failed to retrieve booking details.");
            }
            if (_configuration.GetValue<bool>("EnableReservationValidation") == false && reservation != null)
                reservation.CallDuration = cSLCallDurationstopwatch1.ElapsedMilliseconds;
            return (reservation, availability);
        }

        private async Task<MOBCarbonEmissionsResponse> GetCarbonEmissionsFromReferenceData(United.Services.FlightShopping.Common.ShopSelectRequest request, MOBSHOPAvailability availability, SelectTripRequest selectTripRequest, Session session, FlightReservationResponse shopBookingDetailsResponse)
        {
            MOBCarbonEmissionsResponse carboinEmissionData = new MOBCarbonEmissionsResponse();
            // Build the CarbonEmission Reference Data request 
            CarbonEmissionRequest carbonEmissionRequest = await BuildCarbonEmissionsReferenceDataRequest(session, shopBookingDetailsResponse).ConfigureAwait(false);
            var carbonEmissionSelectFlightResponse = await GetFlightCarbonEmissionDetailsByFlight(session, carbonEmissionRequest, new MOBCarbonEmissionsResponse(), carboinEmissionData, new MOBCarbonEmissionsRequest()
            {
                Application = selectTripRequest.Application,
                DeviceId = selectTripRequest.DeviceId,
                CartId = request.CartId,
                TransactionId = selectTripRequest.TransactionId,
                AccessCode = selectTripRequest.AccessCode
            });

            return carbonEmissionSelectFlightResponse;

        }

        private async Task<CarbonEmissionRequest> BuildCarbonEmissionsReferenceDataRequest(Session session, FlightReservationResponse shopBookingDetailsResponse)
        {
            CarbonEmissionRequest carbonEmissionRequest = new CarbonEmissionRequest();
            SelectTrip selectTrip = new SelectTrip();
            selectTrip = await _sessionHelperService.GetSession<SelectTrip>(session.SessionId, new SelectTrip().ObjectName, new List<string> { session.SessionId, new SelectTrip().ObjectName }).ConfigureAwait(false);
            carbonEmissionRequest.Trips = new Collection<Service.Presentation.ReferenceDataModel.TripReference>();
            carbonEmissionRequest.PassengerCount = shopBookingDetailsResponse.Reservation.Travelers.Count();
            foreach (var displayTrip in shopBookingDetailsResponse.DisplayCart?.DisplayTrips)
            {
                if (displayTrip != null)
                {
                    Service.Presentation.ReferenceDataModel.TripReference tripRequest = new Service.Presentation.ReferenceDataModel.TripReference();
                    tripRequest.Destination = displayTrip?.Destination;
                    tripRequest.Origin = displayTrip?.Origin;
                    tripRequest.Flights = new Collection<Service.Presentation.ReferenceDataModel.FlightReference>();
                    if(displayTrip.Flights != null && displayTrip.Flights.Any())
                    {
                        foreach (var flight in displayTrip.Flights)
                        {
                                AddTripRequest(tripRequest, flight);
                            if (flight.Connections != null && flight.Connections.Count > 0)
                            {
                                foreach (var connectionFlight in flight.Connections)
                                {
                                    AddTripRequest(tripRequest, connectionFlight);
                                }
                            }
                            if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                            {
                                foreach (var stopFlights in flight.StopInfos)
                                {
                                    AddTripRequest(tripRequest, stopFlights);
                                }
                            }
                        }
                    }
                    carbonEmissionRequest.Trips.Add(tripRequest);
                }
            }

            return carbonEmissionRequest;
        }

        private static void AddTripRequest(Service.Presentation.ReferenceDataModel.TripReference tripRequest, Flight flight)
        {
            Service.Presentation.ReferenceDataModel.FlightReference flightReq = new Service.Presentation.ReferenceDataModel.FlightReference();
            flightReq.DepartDateTime = flight.DepartDateTime;
            flightReq.Destination = flight.Destination;
            flightReq.EquipmentDisclosure = new Service.Presentation.ReferenceDataModel.EquipmentDisclosure();
            flightReq.EquipmentDisclosure.EquipmentDescription = flight.EquipmentDisclosures?.EquipmentDescription;
            flightReq.EquipmentDisclosure.EquipmentType = flight.EquipmentDisclosures?.EquipmentType;
            flightReq.FlightHash = flight.Hash;
            flightReq.FlightNumber = flight.FlightNumber;
            flightReq.International = flight.International.ToString();
            flightReq.MarketingCarrier = flight.MarketingCarrier;
            flightReq.OperatingCarrier = flight.OperatingCarrier;
            flightReq.Origin = flight.Origin;
            flightReq.CabinCount = flight.CabinCount;

            Collection<Service.Presentation.ReferenceDataModel.ProductReference> products = new Collection<Service.Presentation.ReferenceDataModel.ProductReference>();
            products.Add(new Service.Presentation.ReferenceDataModel.ProductReference
            {
                BookingCode = flight.Products?.FirstOrDefault()?.BookingCode,
                Description = flight.Products?.FirstOrDefault()?.Description,
                ProductType = flight.Products?.FirstOrDefault()?.ProductType
            });
            flightReq.Products = products;
            flightReq.TravelMinutesTotal = flight.TravelMinutes;
            tripRequest.Flights.Add(flightReq);
        }

        private async Task<MOBCarbonEmissionsResponse> GetFlightCarbonEmissionDetailsByFlight(Session session, CarbonEmissionRequest carbonEmissionRequest, MOBCarbonEmissionsResponse response, MOBCarbonEmissionsResponse carboinEmissionData, MOBCarbonEmissionsRequest request)
        {
            try
            {
                string jsonRequest = JsonConvert.SerializeObject(carbonEmissionRequest);
                var jsonResponse = await _referencedataService.GetCarbonEmissionReferenceData<United.Service.Presentation.ReferenceDataRequestModel.CarbonEmissionRequest>("CarbonEmission", jsonRequest, session.Token, request.SessionId);
                if (jsonResponse != null)
                {

                    if (jsonResponse != null && jsonResponse.Trips != null && jsonResponse.Trips.Count > 0)
                    {
                        List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");

                        response = new MOBCarbonEmissionsResponse();
                        response.CarbonEmissionData = new List<MOBCarbonEmissionData>();
                        foreach (var trip in jsonResponse.Trips)
                        {
                                if (trip.CarbonEmission != null && trip.CarbonEmission.TotalEmission != 0)
                                {
                                MOBCarbonEmissionData carbonEmission = await BuildCarbonEmissionContentForReferenceData(request, lstMessages, trip.CarbonEmission);
                                        
                                    if (carbonEmission != null)
                                    {
                                        response.CarbonEmissionData.Add(carbonEmission);
                                    }
                                }
                        }
                    }
                    else
                    {
                        _logger.LogError("GetFlightCarbonEmissionDetailsByFlight - CSL CarbonEmission no data available");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetFlightCarbonEmissionDetailsByFlight - DataAccess error{@message} {@stackTrace}",  ex.Message, ex.StackTrace);
            }
            return response;
        }
        private async Task<CPCubaTravel> GetCubaTravelResons(MobileCMSContentRequest request)
        {

            request.GroupName = "General:CubaTravelCerts";
            string jsonResponse = await GETCSLCMSContent(request);

            CPCubaTravel mobCPCubaTravel = new CPCubaTravel();
            if (!string.IsNullOrEmpty(jsonResponse))
            {

                var response = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(jsonResponse);

                if (response != null && (Convert.ToBoolean(response.Status) && response.MessageItems != null))
                {

                    //logEntries.Add(LogEntry.GetLogEntry<MOBCSLContentMessagesResponse>(request.SessionId, "GetMobileCMSContents - DeSerialized Response", "DeSerialized Response", request.Application.Id, request.Application.Version.Major, request.DeviceId, response));


                    mobCPCubaTravel.TravelReasons = GetMOBCPCubaTravelReasons(response.MessageItems);



                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.Message;
                        }
                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToRegisterTravelerErrorMessage").ToString();
                        if (_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnActualExceptionMessageBackForTesting").ToString()))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL RegisterTravelers(MOBRegisterTravelersRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToRegisterTravelerErrorMessage").ToString();
                if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL RegisterTravelers(MOBRegisterTravelersRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }


            //logEntries.Add(LogEntry.GetLogEntry<List<MOBMobileCMSContentMessages>>(request.SessionId, "GetMobileCMSContents - Client Response", "to client Response", request.Application.Id, request.Application.Version.Major, request.DeviceId, sortedCMSContentMessages));

            return mobCPCubaTravel;
        }

        private List<MOBCPCubaTravelReason> GetMOBCPCubaTravelReasons(List<CMSContentMessageitem> messageItems)
        {
            List<MOBCPCubaTravelReason> mobCPCubaTravelReason = null;
            if (messageItems != null)
            {
                string messageItemsJson = JsonConvert.SerializeObject(messageItems);
                mobCPCubaTravelReason = JsonConvert.DeserializeObject<List<MOBCPCubaTravelReason>>(messageItemsJson);
                mobCPCubaTravelReason.Where(p => p.Vanity == "LICEN").ToList().ForEach(p => p.IsInputRequired = true);
                mobCPCubaTravelReason.ForEach(p => p.ContentFull = p.ContentFull.Replace("<p>", "").Replace("</p>", ""));
            }
            return mobCPCubaTravelReason;
        }

        private async Task<string> GETCSLCMSContent(MobileCMSContentRequest request, bool isTravelAdvisory = false)
        {
            #region
            if (request == null)
            {
                throw new MOBUnitedException("GetMobileCMSContents request cannot be null.");
            }
            #region Get CSL Content request
            MOBCSLContentMessagesRequest cslContentReqeust = BuildCSLContentMessageRequest(request, isTravelAdvisory);
            #endregion

            string jsonResponse = await GetCSLCMSContentMesseges(request, cslContentReqeust);
            #endregion
            return jsonResponse;

        }
        private async Task<string> GetCSLCMSContentMesseges(MobileCMSContentRequest request, MOBCSLContentMessagesRequest cslContentReqeust)
        {
            string jsonRequest = JsonConvert.SerializeObject(cslContentReqeust);

            //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetMobileCMSContents - Request", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonRequest));

            //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetMobileCMSContents - Request url", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, url));

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cslStopWatch;
            cslStopWatch = new Stopwatch();
            cslStopWatch.Reset();
            cslStopWatch.Start();
            #endregion****Get Call Duration Code - Venkat 03/17/2015*******
            var response = await _iCMSContentService.GetMobileCMSContentMessages(token: request.Token, jsonRequest, request.SessionId).ConfigureAwait(false);
            #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cslStopWatch.IsRunning)
            {
                cslStopWatch.Stop();
            }
            string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

            //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetMobileCMSContents - CSL Call Duration", "CSS/CSL-CallDuration", request.Application.Id, request.Application.Version.Major, request.DeviceId, "CSLGetCMSContentMessages=" + cslCallTime));

            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******   

            //logEntries.Add(LogEntry.GetLogEntry<string>(request.SessionId, "GetMobileCMSContents - Response", "Trace", request.Application.Id, request.Application.Version.Major, request.DeviceId, jsonResponse));


            return response;
        }
        private string GetFlightShareMessage(MOBSHOPReservation reservation, string cabinType)
        {
            #region Build Reservation Share Message 
            string flightDatesText = DateTime.Parse(reservation.Trips[0].DepartDate.Replace("\\", ""), CultureInfo.InvariantCulture).ToString("MMM dd") + (reservation.Trips.Count == 1 ? "" : (" - " + (DateTime.Parse(reservation.Trips[reservation.Trips.Count - 1].ArrivalDate.Replace("\\", ""), CultureInfo.InvariantCulture).ToString("MMM dd"))));
            string travelersText = reservation.NumberOfTravelers.ToString() + " " + (reservation.NumberOfTravelers > 1 ? "travelers" : "traveler");
            string searchType = string.Empty, flightNumbers = string.Empty, viaAirports = string.Empty;
            string initialOrigin = reservation.Trips[0].Origin.ToUpper().Trim();
            string finalDestination = reservation.Trips[reservation.Trips.Count - 1].Destination.ToUpper().Trim();

            switch (reservation.SearchType.ToUpper().Trim())
            {
                case "OW":
                    searchType = "one way";
                    break;
                case "RT":
                    searchType = "roundtrip";
                    break;
                case "MD":
                    searchType = "multiple destinations";
                    break;
                default:
                    break;
            }
            foreach (MOBSHOPTrip trip in reservation.Trips)
            {
                foreach (Model.Shopping.MOBSHOPFlattenedFlight flattenedFlight in trip.FlattenedFlights)
                {
                    if (string.IsNullOrEmpty(cabinType))
                    {
                        cabinType = flattenedFlight.Flights[0].Cabin.ToUpper().Trim() == "COACH" ? "Economy" : flattenedFlight.Flights[0].Cabin;
                    }
                    foreach (Model.Shopping.MOBSHOPFlight flight in flattenedFlight.Flights)
                    {
                        flightNumbers = flightNumbers + "," + flight.FlightNumber;
                        if (flight.Destination.ToUpper().Trim() != initialOrigin && flight.Destination.ToUpper().Trim() != finalDestination)
                        {
                            if (string.IsNullOrEmpty(viaAirports))
                            {
                                viaAirports = " via ";
                            }
                            viaAirports = viaAirports + flight.Destination + ",";
                        }
                    }
                }
            }
            if (flightNumbers.Trim(',').Split(',').Count() > 1)
            {
                flightNumbers = "Flights " + flightNumbers.Trim(',');
            }
            else
            {
                flightNumbers = "Flight " + flightNumbers.Trim(',');
            }
            string reservationShareMessage = string.Format(_configuration.GetValue<string>("Booking20ShareMessage"), flightDatesText, travelersText, searchType, cabinType, flightNumbers.Trim(','), initialOrigin, finalDestination, viaAirports.Trim(','));
            reservation.FlightShareMessage = reservationShareMessage;
            #endregion
            return reservationShareMessage;
        }
        private MOBCSLContentMessagesRequest BuildCSLContentMessageRequest(MobileCMSContentRequest request, bool istravelAdvisory = false)
        {
            MOBCSLContentMessagesRequest cslContentReqeust = new MOBCSLContentMessagesRequest();
            if (request != null)
            {
                cslContentReqeust.Lang = "en";
                cslContentReqeust.Pos = "us";
                cslContentReqeust.Channel = "mobileapp";
                cslContentReqeust.Listname = new List<string>();
                foreach (string strItem in request.ListNames)
                {
                    cslContentReqeust.Listname.Add(strItem);
                }
                if (_configuration.GetValue<string>("CheckCMSContentsLocationCodes").ToUpper().Trim().Split('|').ToList().Contains(request.GroupName.ToUpper().Trim()))
                {
                    cslContentReqeust.LocationCodes = new List<string>();
                    cslContentReqeust.LocationCodes.Add(request.GroupName);
                }
                else
                {
                    cslContentReqeust.Groupname = request.GroupName;
                }
                if (_configuration.GetValue<string>("DonotUsecache4CSMContents") == null || (_configuration.GetValue<string>("DonotUsecache4CSMContents") != null && !_configuration.GetValue<bool>("DonotUsecache4CSMContents")))
                {
                    if (!istravelAdvisory)
                        cslContentReqeust.Usecache = true;
                }
            }

            return cslContentReqeust;
        }
        private List<Model.Shopping.TravelOption> GetTravelOptions(DisplayCart displayCart, bool isReshop, int appID, string appVersion)
        {
            List<Model.Shopping.TravelOption> travelOptions = null;
            if (displayCart != null && displayCart.TravelOptions != null && displayCart.TravelOptions.Count > 0)
            {
                CultureInfo ci = null;
                travelOptions = new List<Model.Shopping.TravelOption>();
                bool addTripInsuranceInTravelOption =
                    !_configuration.GetValue<bool>("ShowTripInsuranceBookingSwitch")
                    && Convert.ToBoolean(_configuration.GetValue<string>("ShowTripInsuranceSwitch") ?? "false");
                foreach (var anOption in displayCart.TravelOptions)
                {
                    //wade - added check for farelock as we were bypassing it
                    if (!anOption.Type.Equals("Premium Access") && !anOption.Key.Trim().ToUpper().Contains("FARELOCK") && !(addTripInsuranceInTravelOption && anOption.Key.Trim().ToUpper().Contains("TPI"))
                    && !(_shoppingUtility.EnableEPlusAncillary(appID, appVersion, isReshop) && anOption.Key.Trim().ToUpper().Contains("EFS")))
                    {
                        continue;
                    }
                    if (ci == null)
                    {
                        ci = TopHelper.GetCultureInfo(anOption.Currency);
                    }

                    Model.Shopping.TravelOption travelOption = new Model.Shopping.TravelOption();
                    travelOption.Amount = (double)anOption.Amount;

                    travelOption.DisplayAmount = TopHelper.FormatAmountForDisplay(anOption.Amount, ci, false);

                    //??
                    if (anOption.Key.Trim().ToUpper().Contains("FARELOCK") || (addTripInsuranceInTravelOption && anOption.Key.Trim().ToUpper().Contains("TPI")))
                        travelOption.DisplayButtonAmount = TopHelper.FormatAmountForDisplay(anOption.Amount, ci, false);
                    else
                        travelOption.DisplayButtonAmount = TopHelper.FormatAmountForDisplay(anOption.Amount, ci, true);

                    travelOption.CurrencyCode = anOption.Currency;
                    travelOption.Deleted = anOption.Deleted;
                    travelOption.Description = anOption.Description;
                    travelOption.Key = anOption.Key;
                    travelOption.ProductId = anOption.ProductId;
                    travelOption.SubItems = GetTravelOptionSubItems(anOption.SubItems);
                    if (_shoppingUtility.EnableEPlusAncillary(appID, appVersion, isReshop) && anOption.SubItems != null && anOption.SubItems.Count > 0)
                    {
                        travelOption.BundleCode = GetTravelOptionEplusAncillary(anOption.SubItems, travelOption.BundleCode);
                        GetTravelOptionAncillaryDescription(anOption.SubItems, travelOption, displayCart);
                    }
                    if (!string.IsNullOrEmpty(anOption.Type))
                    {
                        travelOption.Type = anOption.Type.Equals("Premium Access") ? "Premier Access" : anOption.Type;
                    }
                    travelOptions.Add(travelOption);
                }
            }

            return travelOptions;
        }
        private bool IsOneFlexibleSegmentExist(List<MOBSHOPTrip> trips)
        {
            bool isFlexibleSegment = true;
            if (trips != null)
            {
                foreach (MOBSHOPTrip trip in trips)
                {
                    #region
                    if (trip.FlattenedFlights != null)
                    {
                        foreach (Model.Shopping.MOBSHOPFlattenedFlight flattenedFlight in trip.FlattenedFlights)
                        {
                            if (flattenedFlight.Flights != null && flattenedFlight.Flights.Count > 0)
                            {
                                foreach (Model.Shopping.MOBSHOPFlight flight in flattenedFlight.Flights)
                                {
                                    //TFS 53620:Booking - Certain Flights From IAH- ANC Are Displaying An Error Message
                                    if (flight.ShoppingProducts != null && flight.ShoppingProducts.Count > 0)
                                    {
                                        foreach (Model.Shopping.MOBSHOPShoppingProduct product in flight.ShoppingProducts)
                                        {
                                            if (!product.Type.ToUpper().Trim().Contains("FLEXIBLE"))
                                            {
                                                isFlexibleSegment = false;
                                                break;
                                            }

                                        }
                                    }
                                    if (!isFlexibleSegment)
                                    {
                                        break;
                                    }
                                }
                            }
                            if (!isFlexibleSegment) { break; }
                        }
                    }
                    if (!isFlexibleSegment) { break; }
                    #endregion
                }
            }
            return isFlexibleSegment;
        }
        public async Task<bool> IsRequireNatAndCR(MOBSHOPReservation reservation, MOBApplication application, string sessionID, string deviceID, string token)
        {
            bool isRequireNatAndCR = false;
            List<string> NationalityResidenceCountriesList = new List<string>();

            #region Load list of countries from cache/persist
            var list = await _cachingService.GetCache<List<MOBSHOPCountry>>(_configuration.GetValue<string>("NationalityResidenceCountriesListStaticGUID") + "Booking2.0NationalityResidenceCountries", _headers.ContextValues.TransactionId).ConfigureAwait(false);
            var lst = JsonConvert.DeserializeObject<List<MOBSHOPCountry>>(list);
            #endregion Load list of countries from cache/persist

            if (lst == null)
            {
                lst = await GetNationalityResidenceCountries(application.Id, deviceID, application.Version.Major, _headers.ContextValues.TransactionId, sessionID, token);
                try
                {
                    await _cachingService.SaveCache<List<MOBSHOPCountry>>(_configuration.GetValue<string>("NationalityResidenceCountriesListStaticGUID") + "Booking2.0NationalityResidenceCountries", lst, _headers.ContextValues.TransactionId, new TimeSpan(1, 30, 0));
                }
                catch (Exception e)
                {
                    _logger.LogError("ShoppingCartBusiness -SelectTrip IsRequireNatAndCR SaveCache Failed - Exception {error} and SessionId {guid}", e.Message, sessionID);
                }
            }
            if (lst != null && lst.Count > 0)
            {
                NationalityResidenceCountriesList = lst.Select(c => c.CountryCode).ToList();
            }
            else if (lst == null)
            {
                string dList = _configuration.GetValue<string>("TaxPriceChangeCountries") as string; // If any issue with CSL loading country list from Web.Config
                if (!string.IsNullOrEmpty(dList))
                {
                    foreach (string s in dList.Split(',').ToList())
                    {
                        NationalityResidenceCountriesList.Add(s);
                    }
                }
            }

            if (reservation != null && reservation.Trips != null && NationalityResidenceCountriesList != null && NationalityResidenceCountriesList.Count > 1)
            {
                foreach (MOBSHOPTrip trip in reservation.Trips)
                {
                    if (isRequireNatAndCR)
                        break;
                    foreach (var flight in trip.FlattenedFlights)
                    {
                        foreach (var stopFlights in flight.Flights)
                        {
                            isRequireNatAndCR = IsNatAndCRExists(stopFlights.OriginCountryCode, stopFlights.DestinationCountryCode, NationalityResidenceCountriesList);

                            if (!isRequireNatAndCR && stopFlights.StopInfos != null)
                            {
                                foreach (var stop in stopFlights.StopInfos)
                                {
                                    isRequireNatAndCR = IsNatAndCRExists(stop.OriginCountryCode, stop.DestinationCountryCode, NationalityResidenceCountriesList);
                                    isRequireNatAndCR = IsNatAndCRExists(stop.OriginCountryCode, stop.DestinationCountryCode, NationalityResidenceCountriesList);
                                }
                                if (isRequireNatAndCR)
                                    break;
                            }
                            if (isRequireNatAndCR)
                                break;
                        }

                        if (isRequireNatAndCR)
                            break;
                    }
                }
            }
            return isRequireNatAndCR;
        }
        private bool IsNatAndCRExists(string origin, string destination, List<string> NatAndCRList)
        {
            bool isNatAndCRExists = false;
            if (!string.IsNullOrEmpty(origin) && !string.IsNullOrEmpty(destination) && NatAndCRList != null && NatAndCRList.Count > 0)
            {
                if (NatAndCRList != null && (NatAndCRList.Exists(p => p == origin) || NatAndCRList.Exists(p => p == destination)))
                {
                    isNatAndCRExists = true;
                }
            }
            return isNatAndCRExists;
        }
        private void GetTravelOptionAncillaryDescription(SubitemsCollection subitemsCollection, Model.Shopping.TravelOption travelOption, DisplayCart displayCart)
        {
            List<AncillaryDescriptionItem> ancillaryDesciptionItems = new List<AncillaryDescriptionItem>();
            CultureInfo ci = null;

            if (subitemsCollection.Any(t => t?.Type?.Trim().ToUpper() == "EFS"))
            {
                var trips = subitemsCollection.GroupBy(x => x.TripIndex);
                foreach (var trip in trips)
                {
                    if (trip != null)
                    {
                        decimal ancillaryAmount = 0;
                        foreach (var item in trip)
                        {
                            ancillaryAmount += item.Amount;
                            if (ci == null)
                            {
                                ci = TopHelper.GetCultureInfo(item.Currency);
                            }
                        }

                        AncillaryDescriptionItem objeplus = new AncillaryDescriptionItem();
                        objeplus.DisplayValue = TopHelper.FormatAmountForDisplay(ancillaryAmount, ci, false);
                        objeplus.SubTitle = displayCart.DisplayTravelers?.Count.ToString() + (displayCart.DisplayTravelers?.Count > 1 ? " travelers" : " traveler");
                        var displayTrip = displayCart.DisplayTrips?.FirstOrDefault(s => s.Index == Convert.ToInt32(trip.FirstOrDefault().TripIndex));
                        if (displayTrip != null)
                        {
                            objeplus.Title = displayTrip.Origin + " - " + displayTrip.Destination;
                        }
                        ancillaryDesciptionItems.Add(objeplus);
                    }
                }

                travelOption.BundleOfferTitle = "Economy Plus®";
                travelOption.BundleOfferSubtitle = "Included with your fare";
                travelOption.AncillaryDescriptionItems = ancillaryDesciptionItems;
            }
        }
        private void AssignFFCResidualForTravelerV2
       (MOBSHOPAvailability availability, FlightReservationResponse response)
        {
            if (availability?.Reservation?.TravelersCSL?.Count > 0
                && response?.DisplayCart?.DisplayPrices?.Count > 0
                && response.Reservation?.Travelers != null && response.Reservation.Travelers.Any())
            {
                bool isFFCR = false;
                var title = _configuration.GetValue<string>("ReshopChangeFFCRTitle");
                var content = _configuration.GetValue<string>("ReshopChangeFFCRShortContent");

                availability.Reservation.TravelersCSL.ForEach(tvlr =>
                {
                    if (tvlr != null)
                    {
                        var scCslTraveler = response.Reservation.Travelers.Select(x => x.Person).FirstOrDefault
                        (x => string.Equals(x.Key, tvlr.Key, StringComparison.OrdinalIgnoreCase));

                        if (!string.IsNullOrEmpty(scCslTraveler?.Type) && !string.Equals(tvlr.TravelerTypeCode, "INF", StringComparison.OrdinalIgnoreCase))
                        {
                            var paxffcrprice = response.DisplayCart.DisplayPrices.FirstOrDefault
                            (x => string.Equals(x.PaxTypeCode, scCslTraveler.Type, StringComparison.OrdinalIgnoreCase) && x.ResidualAmount > 0);

                            if (paxffcrprice != null)
                            {
                                isFFCR = true;
                                var perpaxamount = Convert.ToDouble(paxffcrprice.ResidualAmount);
                                tvlr.FutureFlightCredits = new List<MOBFOPFutureFlightCredit>
                            {
                                new MOBFOPFutureFlightCredit{
                                NewValueAfterRedeem = perpaxamount,
                                DisplayNewValueAfterRedeem = $"{perpaxamount.ToString("C2", CultureInfo.CurrentCulture)}",
                                RecipientsFirstName = tvlr.FirstName,
                                RecipientsLastName = tvlr.LastName,
                                ExpiryDate = string.Empty,
                                TravelerNameIndex = tvlr.Key
                                }};
                            } //End - If

                            //FFCShopping fFCShopping = new FFCShopping(_configuration, null, _sessionHelperService, _sDLContentService, _shoppingUtility);
                            _fFCShoppingcs.AssignTravelerTotalFFCNewValueAfterReDeem(tvlr);
                        }
                    }
                }); //END - foreach

                if (isFFCR)
                {
                    availability.Reservation.Reshop.FFCMessage = new MOBFutureFlightCredit
                    {
                        Messages = new List<MOBItem>
                            {
                                new MOBItem{ Id="TITLE", CurrentValue = title },
                                new MOBItem{ Id="CONTENTSHORT", CurrentValue = content },
                            }
                    };
                } //END - If 
            }
        }

        private List<ShopBundleEplus> GetTravelOptionEplusAncillary(SubitemsCollection subitemsCollection, List<ShopBundleEplus> bundlecode)
        {
            if (bundlecode == null || bundlecode.Count == 0)
            {
                bundlecode = new List<ShopBundleEplus>();
            }

            foreach (var item in subitemsCollection)
            {
                if (item?.Type?.Trim().ToUpper() == "EFS")
                {
                    ShopBundleEplus objeplus = new ShopBundleEplus();
                    objeplus.ProductKey = item.Type;
                    objeplus.AssociatedTripIndex = Convert.ToInt32(item.TripIndex);
                    bundlecode.Add(objeplus);
                }
            }

            return bundlecode;
        }
        private MOBSection GetFareLockAdvanceSearchCouponWarningMessageWithAncillary(FareLock fareLock, bool isAdvanceSearchCoupon, DisplayCart displayCart)
        {
            MOBSection promoAlertForFareLock = null;
            if (isAdvanceSearchCoupon && fareLock != null
                && fareLock.FareLockProducts != null
                && fareLock.FareLockProducts.Count > 0
                && isAFSCouponApplied(displayCart)
                && !IsFareOnlyCoupon(displayCart))
            {
                promoAlertForFareLock = new MOBSection()
                {
                    Text1 = _configuration.GetValue<string>("AdvanceSearchCouponWithFarelockBookingErrorMessage"),
                    Text2 = "Cancel",
                    Text3 = "Continue"
                };
            }
            return promoAlertForFareLock;
        }
        private bool isAFSCouponApplied(DisplayCart displayCart)
        {
            if (displayCart != null && displayCart.SpecialPricingInfo != null && displayCart.SpecialPricingInfo.MerchOfferCoupon != null && !string.IsNullOrEmpty(displayCart.SpecialPricingInfo.MerchOfferCoupon.PromoCode) && displayCart.SpecialPricingInfo.MerchOfferCoupon.IsCouponEligible.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            return false;
        }
        private async Task<List<MOBSHOPCountry>> GetNationalityResidenceCountries(int applicationId, string deviceId, string appVersion, string transactionId, string sessionID, string token)
        {
            List<MOBSHOPCountry> lstNationalityResidenceCountries = null;


            try
            {
                string logAction = "NationalityResidenceCountries";


                var response = await _referencedataService.GetNationalityResidence<List<United.Service.Presentation.CommonModel.Characteristic>>(logAction, token, sessionID).ConfigureAwait(false);


                lstNationalityResidenceCountries = new List<MOBSHOPCountry>();
                List<United.Service.Presentation.CommonModel.Characteristic> lst = response;

                if (lst != null && lst.Count > 0)
                {
                    foreach (var l in lst)
                    {
                        lstNationalityResidenceCountries.Add(new MOBSHOPCountry() { CountryCode = l.Code, Name = l.Description });
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogInformation("GetNationalityResidenceCountries Exception {sessionId} and {Message}", sessionID, ex.Message);

            }
            if (deviceId.ToUpper().Trim() != "SCHEDULED_PublicKey_UPDADE_JOB".ToUpper().Trim())
            {
                //System.Threading.Tasks.Task.Factory.StartNew(() => Authentication.Write(logEntries));
            }

            return lstNationalityResidenceCountries;
        }
        private List<TravelOptionSubItem> GetTravelOptionSubItems(SubitemsCollection subitemsCollection)
        {
            List<TravelOptionSubItem> subItems = null;

            if (subitemsCollection != null && subitemsCollection.Count > 0)
            {
                CultureInfo ci = null;
                subItems = new List<TravelOptionSubItem>();

                foreach (var item in subitemsCollection)
                {
                    if (ci == null)
                    {
                        ci = TopHelper.GetCultureInfo(item.Currency);
                    }

                    TravelOptionSubItem subItem = new TravelOptionSubItem();
                    subItem.Amount = (double)item.Amount;
                    subItem.DisplayAmount = TopHelper.FormatAmountForDisplay(item.Amount, ci, false);
                    subItem.CurrencyCode = item.Currency;
                    subItem.Description = item.Description;
                    subItem.Key = item.Key;
                    subItem.ProductId = item.Type;
                    subItem.Value = item.Value;


                    //    subItem.BundleCode = new List<MobShopBundleEplus>();
                    //    foreach (var v in item.Product.SubProducts)
                    //    {

                    //      MobShopBundleEplus objeplus = new MobShopBundleEplus();
                    //       if (v.Code == "EPU")
                    //        {
                    //            objeplus.ProductKey = item.Product.ProductType;
                    //            objeplus.SegmentName = item.Product.PromoDescription;
                    //        }
                    //        subItem.BundleCode.Add(objeplus);
                    //    }
                    subItems.Add(subItem);
                }

            }

            return subItems;
        }
        private bool IsFareOnlyCoupon(DisplayCart displayCart)
        {
            if (string.IsNullOrEmpty(displayCart.SpecialPricingInfo.MerchOfferCoupon.Product))
            {
                return true;
            }
            return false;
        }
        private async Task AddCorporateLeisureOptOutFSRAlert(ReservationInfo2 shopReservationInfo2, SelectTripRequest request, Session session)
        {
            var message = new MOBMobileCMSContentMessages();
            if (shopReservationInfo2 == null)
            {
                shopReservationInfo2 = new ReservationInfo2();
            }
            if (shopReservationInfo2.InfoWarningMessages == null)
            {
                shopReservationInfo2.InfoWarningMessages = new List<InfoWarningMessages>();
            }
            if (!shopReservationInfo2.InfoWarningMessages.Exists(x => x.Order == MOBINFOWARNINGMESSAGEORDER.RTICORPORATELEISUREOPTOUTMESSAGE.ToString()))
            {
                message = await GetCMSContentMessageByKey("Shopping.CorporateDisclaimerMessage.MOB", request, session).ConfigureAwait(false);
                if (message != null)
                {
                    shopReservationInfo2.InfoWarningMessages.Add(new InfoWarningMessages
                    {
                        Order = MOBINFOWARNINGMESSAGEORDER.RTICORPORATELEISUREOPTOUTMESSAGE.ToString(),
                        IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString(),
                        Messages = new List<string>() { message.ContentFull },
                        ButtonLabel = message.ContentShort,
                        HeaderMessage = message.HeadLine,
                        IsCollapsable = true,
                        IsExpandByDefault = true
                    });
                }
                shopReservationInfo2.InfoWarningMessages = shopReservationInfo2.InfoWarningMessages.OrderBy(c => (int)((MOBINFOWARNINGMESSAGEORDER)Enum.Parse(typeof(MOBINFOWARNINGMESSAGEORDER), c.Order))).ToList();
            }
        }
        private async Task<MOBMobileCMSContentMessages> GetCMSContentMessageByKey(string Key, MOBRequest request, Session session)
        {
            string cmsContentCache;
            CSLContentMessagesResponse cmsResponse = new CSLContentMessagesResponse();
            MOBMobileCMSContentMessages cmsMessage = null;
            List<CMSContentMessage> cmsMessages = null;
            try
            {

                cmsContentCache = await _cachingService.GetCache<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID") + ObjectNames.MOBCSLContentMessagesResponseFullName, request.TransactionId);

                if (!string.IsNullOrEmpty(cmsContentCache))
                {
                    cmsResponse = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(cmsContentCache);
                }
                if (cmsResponse == null || Convert.ToBoolean(cmsResponse.Status) == false || cmsResponse.Messages == null)
                {
                    cmsResponse = await _travelerCSL.GetBookingRTICMSContentMessages(request, session);
                }
                cmsMessages = (cmsResponse != null && cmsResponse.Messages != null && cmsResponse.Messages.Count > 0) ? cmsResponse.Messages : null;
                if (cmsMessages != null)
                {
                    var message = cmsMessages.Find(m => m.Title.Equals(Key));
                    if (message != null)
                    {
                        cmsMessage = new MOBMobileCMSContentMessages()
                        {
                            HeadLine = message.Headline,
                            ContentFull = message.ContentFull,
                            ContentShort = message.ContentShort
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                //logEntries.Add(LogEntry.GetLogEntry<string>(session.SessionId, "RTICMSContentMessages", "MOBUnitedException", request.Application.Id, request.Application.Version.Major, request.DeviceId, ex.Message));
            }
            return cmsMessage;
        }
        private bool IsYATravel(List<DisplayTraveler> displayTravelers)
        {
            if (displayTravelers == null || displayTravelers.Count == 0) return false;

            return displayTravelers.Any(t => t != null && !string.IsNullOrEmpty(t.PricingPaxType) && t.PricingPaxType.ToUpper().Equals("UAY"));
        }

        private async System.Threading.Tasks.Task FireForGetInFlightCLEligibilityCheck(Reservation reservation, MOBRequest request, Session session)
        {
            if (!reservation.IsReshopChange)
            {
                //System.Threading.Tasks.Task.Factory.StartNew(() =>
                //{
                //    _merchandizingServices.IsEligibleInflightContactlessPayment(reservation, request, session);
                //});

                await _merchandizingServices.IsEligibleInflightContactlessPayment(reservation, request, session);
            }
        }
        private async Task<Reservation> CreateBookingPathReservation(string sessionId, United.Services.FlightShopping.Common.ShopSelectRequest request,
           MOBSHOPAvailability availability, Model.Shopping.ShoppingResponse shop, Session session, FlightReservationResponseBase response)
        {

            Reservation bookingPathReservation = new Reservation();
            if (session.IsReshopChange && !string.IsNullOrEmpty(sessionId))
            {
                var cslReservation = await _sessionHelperService.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(sessionId, new United.Service.Presentation.ReservationResponseModel.ReservationDetail().GetType().FullName, new List<string> { sessionId, new United.Service.Presentation.ReservationResponseModel.ReservationDetail().GetType().FullName }).ConfigureAwait(false);
                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(sessionId, new Reservation().ObjectName, new List<string> { sessionId, new Reservation().ObjectName }).ConfigureAwait(false);
                //cslReservation = JsonConvert.DeserializeObject<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(bookingPathReservation.CSLReservationJSONFormat);
                bookingPathReservation.Trips = availability.Reservation.Trips;
                bookingPathReservation = await AssignTravelerSeatObjectForSelectedChangeFlight(session, bookingPathReservation, cslReservation);
                if (bookingPathReservation != null && session.IsReshopChange)
                {
                    if (bookingPathReservation.ReshopTrips != null)
                    {
                        if (bookingPathReservation.ReshopTrips.Count <= availability.Reservation.Trips.Count)
                        {

                            int countDiff = availability.Reservation.Trips.Count - bookingPathReservation.ReshopTrips.Count;
                            if (countDiff > 0)
                            {
                                for (int i = 0; i < countDiff; i++)
                                {
                                    ReshopTrip reshObj1 = new ReshopTrip();
                                    reshObj1.IsReshopTrip = true;
                                    bookingPathReservation.ReshopTrips.Add(reshObj1);
                                }
                            }

                            for (int tripIndex = 0; tripIndex < availability.Reservation.Trips.Count; tripIndex++)
                            {
                                if (bookingPathReservation.ReshopTrips[tripIndex].IsReshopTrip)
                                {
                                    bookingPathReservation.ReshopTrips[tripIndex].ChangeTrip = availability.Reservation.Trips[tripIndex];
                                }
                                else
                                {
                                    bookingPathReservation.ReshopTrips[tripIndex].ChangeTrip = null;
                                }
                            }

                        }
                        else if (Convert.ToBoolean(_configuration.GetValue<string>("ReshopChange-PartiallyUsedFix"))) // Fix for partailly used pnr
                        {
                            var newTrip = availability.Reservation.Trips.Where(p => p.ChangeType == MOBSHOPTripChangeType.ChangeFlight).ToList();
                            var reshopTrip = bookingPathReservation.ReshopTrips.Where(p => p.IsReshopTrip == true).ToList();
                            for (int tripIndex = 0; tripIndex < newTrip.Count(); tripIndex++)
                            {
                                if (newTrip[tripIndex].ChangeType == MOBSHOPTripChangeType.ChangeFlight)
                                {
                                    reshopTrip[tripIndex].ChangeTrip = newTrip[tripIndex];
                                }
                                else
                                {
                                    reshopTrip[tripIndex].ChangeTrip = null;
                                }
                            }
                        }
                    }

                    bookingPathReservation.IsReshopChange = true;
                    availability.Reservation.IsReshopChange = true;
                    availability.Reservation.Reshop = bookingPathReservation.Reshop;
                    bookingPathReservation.SeatPrices = null;
                    bookingPathReservation.ClubPassPurchaseRequest = null;
                    if (bookingPathReservation.TravelersCSL != null && bookingPathReservation.TravelersCSL.Count > 0)
                    {
                        availability.Reservation.TravelersCSL = new List<MOBCPTraveler>();
                        foreach (var travelCSL in bookingPathReservation.TravelersCSL)
                        {
                            availability.Reservation.TravelersCSL.Add(travelCSL.Value);
                        }
                    }

                    if (availability.Reservation.Reshop != null
                       && response?.DisplayCart?.DisplayPrices != null)
                    {
                        availability.Reservation.Reshop.IsResidualFFCRAvailable
                            = response.DisplayCart.DisplayPrices.Any(x => !string.IsNullOrEmpty(x.Status)
                            && string.Equals(x.Status, "FFCR", StringComparison.OrdinalIgnoreCase));
                    }

                    UpdatePricesAndTaxesForReshopChangeFlight(availability, session, cslReservation);
                }
                else
                {
                    bookingPathReservation = new Reservation();
                }
            }

            bookingPathReservation.SessionId = sessionId;
            bookingPathReservation.CartId = request.CartId;
            bookingPathReservation.PointOfSale = shop.Request.CountryCode;

            if (string.IsNullOrEmpty(session.EmployeeId))
            {
                bookingPathReservation.FareLock = availability.Reservation.FareLock;
            }
            bookingPathReservation.Trips = availability.Reservation.Trips;
            bookingPathReservation.Prices = availability.Reservation.Prices;
            bookingPathReservation.Taxes = availability.Reservation.Taxes;
            bookingPathReservation.TravelOptions = availability.Reservation.TravelOptions;
            bookingPathReservation.NumberOfTravelers = availability.Reservation.NumberOfTravelers;
            bookingPathReservation.SearchType = availability.Reservation.SearchType;
            bookingPathReservation.AwardTravel = availability.Reservation.AwardTravel;
            bookingPathReservation.FlightShareMessage = availability.Reservation.FlightShareMessage;
            bookingPathReservation.IsRefundable = availability.Reservation.IsRefundable;
            bookingPathReservation.ISInternational = availability.Reservation.ISInternational;
            bookingPathReservation.ISFlexibleSegmentExist = availability.Reservation.ISFlexibleSegmentExist;
            bookingPathReservation.PKDispenserPublicKey = availability.Reservation.PKDispenserPublicKey;
            bookingPathReservation.LMXFlights = availability.Reservation.LMXFlights;
            bookingPathReservation.IneligibleToEarnCreditMessage = availability.Reservation.IneligibleToEarnCreditMessage;
            bookingPathReservation.OaIneligibleToEarnCreditMessage = availability.Reservation.OaIneligibleToEarnCreditMessage;
            bookingPathReservation.IsELF = availability.Reservation.IsELF;
            bookingPathReservation.IsSSA = availability.Reservation.IsSSA;
            bookingPathReservation.SeatAssignmentMessage = availability.Reservation.SeatAssignmentMessage;
            bookingPathReservation.AlertMessages = availability.Reservation.AlertMessages;
            bookingPathReservation.Messages = availability.Reservation.Messages;
            bookingPathReservation.IsRedirectToSecondaryPayment = availability.Reservation.IsRedirectToSecondaryPayment;
            bookingPathReservation.IsUpgradedFromEntryLevelFare = availability.Reservation.IsUpgradedFromEntryLevelFare;
            bookingPathReservation.ELFMessagesForRTI = availability.Reservation.ELFMessagesForRTI.Clone();
            bookingPathReservation.CheckedbagChargebutton = availability.Reservation.CheckedbagChargebutton;
            bookingPathReservation.IsCubaTravel = availability.Reservation.IsCubaTravel;

            if (availability.Reservation.IsCubaTravel)
            {
                bookingPathReservation.CubaTravelInfo = availability.Reservation.CubaTravelInfo;
            }

            bookingPathReservation.FormOfPaymentType = availability.Reservation.FormOfPaymentType;
            if ((availability.Reservation.FormOfPaymentType == MOBFormofPayment.PayPal || availability.Reservation.FormOfPaymentType == MOBFormofPayment.PayPalCredit) && availability.Reservation.PayPal != null)
            {
                bookingPathReservation.PayPal = availability.Reservation.PayPal;
                bookingPathReservation.PayPalPayor = availability.Reservation.PayPalPayor;
            }
            if (availability.Reservation.FormOfPaymentType == MOBFormofPayment.Masterpass)
            {
                if (availability.Reservation.MasterpassSessionDetails != null)
                    bookingPathReservation.MasterpassSessionDetails = availability.Reservation.MasterpassSessionDetails;
                if (availability.Reservation.Masterpass != null)
                    bookingPathReservation.Masterpass = availability.Reservation.Masterpass;
            }
            try
            {
                //TODO :ASSEMBLY NOT LOADING CHECK THIS LATER
                var segments = CommonMethods.FlattenSegmentsForSeatDisplay(response.Reservation);
                bookingPathReservation.CSLReservationJSONFormat = JsonConvert.SerializeObject(segments);
                if (availability.Reservation.FOPOptions != null && availability.Reservation.FOPOptions.Count > 0) //FOP Options Fix Venkat 12/08
                {
                    bookingPathReservation.FOPOptions = availability.Reservation.FOPOptions;
                }
                if (availability.Reservation.ShopReservationInfo != null)
                {
                    bookingPathReservation.ShopReservationInfo = availability.Reservation.ShopReservationInfo;
                }
                if (availability.Reservation.ShopReservationInfo2 != null)
                {
                    bookingPathReservation.ShopReservationInfo2 = availability.Reservation.ShopReservationInfo2;
                }
            }
            catch (Exception)
            {

            }
            return bookingPathReservation;
        }
        private async Task<Reservation> AssignTravelerSeatObjectForSelectedChangeFlight(Session session, Reservation reservation, United.Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation)
        {

            if (session.IsReshopChange)
            {
                ShoppingResponse persistShop = new ShoppingResponse();
                persistShop = await _sessionHelperService.GetSession<ShoppingResponse>(session.SessionId, persistShop.ObjectName, new List<string> { session.SessionId, persistShop.ObjectName }).ConfigureAwait(false);
                if (persistShop == null)
                {
                    throw new MOBUnitedException("Could not find your booking session.");
                }
                //Reshopping reshopping = new Reshopping(_configuration, _logger);
                reservation = AssignPnrTravelerToReservation(persistShop.Request, reservation, cslReservation);

                foreach (var traveler in reservation.TravelersCSL)
                {
                    int tripAndFlightIndexForSeatInsert = 0;
                    foreach (var trip in reservation.Trips)
                    {
                        foreach (var flattenedFlight in trip.FlattenedFlights)
                        {
                            foreach (var flight in flattenedFlight.Flights)
                            {
                                if (traveler.Value.Seats == null || !traveler.Value.Seats.Exists(s => s.Origin == flight.Origin && s.Destination == flight.Destination && s.FlightNumber == flight.FlightNumber))
                                {
                                    if (traveler.Value.Seats == null)
                                        traveler.Value.Seats = new List<MOBSeat>();
                                    var mobseat = new MOBSeat();
                                    mobseat.TravelerSharesIndex = traveler.Key;
                                    mobseat.Destination = flight.Destination;
                                    mobseat.Origin = flight.Origin;
                                    mobseat.UAOperated = (flight.OperatingCarrier.ToUpper() == "UA");
                                    //mobseat.ProgramCode = flight.pr;
                                    traveler.Value.Seats.Insert(tripAndFlightIndexForSeatInsert, mobseat);
                                    if (_shoppingUtility.IsMilesFOPEnabled())
                                    {
                                        mobseat.OldSeatMiles = Convert.ToInt32(_configuration.GetValue<string>("milesFOP"));
                                        mobseat.DisplayOldSeatMiles = ShopStaticUtility.FormatAwardAmountForDisplay(_configuration.GetValue<string>("milesFOP"), false);
                                    }
                                }
                                tripAndFlightIndexForSeatInsert++;
                            }
                        }
                    }

                }
            }
            return reservation;
        }
        private void UpdatePricesAndTaxesForReshopChangeFlight(MOBSHOPAvailability availability, Session session, United.Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation)
        {
            if (session.IsReshopChange)
            {
                if (session.IsAward)
                {
                    AwardUpdatePricesAndTaxesForReshopChangeFlight(availability, session, cslReservation);
                }
                else
                {
                    RevenueReshopPricesAndTaxesForReshopChangeFlight(availability, session, cslReservation);
                }
            }
        }
        private void RevenueReshopPricesAndTaxesForReshopChangeFlight(MOBSHOPAvailability availability, Session session, United.Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation)
        {
            var reservation = availability.Reservation;

            if (session.IsReshopChange)
            {
                availability.Trip = null;
                availability.Reservation.Reshop.IsRefundBillingAddressRequired = false;
                availability.Reservation.Reshop.AncillaryRefundFormOfPayment = "To original form of payment";
                reservation.Taxes.RemoveAll(p => p.TaxCode.ToUpper() == "CHANGEFEE" || p.TaxCode.ToUpper() == "ADDCOLLECT");

                bool isChangeFeeExistInPrices = false;
                bool isAddCollectExistInPrices = false;
                double newtaxtotal = 0;
                double originalTotal = 0;
                double totalWithChangeFee = 0;

                string waivedDesc = "";
                string changeWaivedPriceValue = "";
                bool isNonResidualCredit = false;
                //var cslReservation = United.Persist.FilePersist.Load<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(Convert.ToString(session.SessionId), (new United.Service.Presentation.ReservationResponseModel.ReservationDetail()).GetType().FullName);
                if (cslReservation != null && cslReservation.Detail != null && !cslReservation.Detail.Characteristic.IsNullOrEmpty())
                {
                    waivedDesc = ShopStaticUtility.GetCharacteristicDescription(cslReservation.Detail.Characteristic.ToList(), "24HrFlexibleBookingPolicy");
                }

                if (!string.IsNullOrEmpty(waivedDesc))
                {
                    //reservation.Prices.Add(GetPriceObject("CHANGEFEE", "Change fee", 0, "Waived"));
                    changeWaivedPriceValue = "Waived";
                }

                foreach (var price in reservation.Prices)
                {
                    switch (price.DisplayType.ToUpper())
                    {
                        case "NEWTOTAL":
                        case "TRAVELERPRICE":
                        case "NEWTAXTOTAL":
                            var totalTravels = (reservation.TravelersCSL != null ? reservation.TravelersCSL.Count : 0);
                            var totlaTravelsString = string.Empty;
                            if (totalTravels > 1)
                            {
                                totlaTravelsString = " (" + totalTravels.ToString() + " travelers)";
                            }
                            else if (totalTravels == 1)
                            {
                                totlaTravelsString = " (" + totalTravels.ToString() + " traveler)";
                            }
                            price.PriceTypeDescription = "New trip" + totlaTravelsString;
                            if (price.DisplayType.ToUpper() == "NEWTAXTOTAL" || price.DisplayType.ToUpper() == "NEWTOTAL")
                            {
                                newtaxtotal += price.Value;
                            }
                            break;
                        case "TAXDIFFERENCE":
                            price.PriceTypeDescription = "Tax Difference";
                            break;
                        case "FAREDIFFERENCE":
                            price.PriceTypeDescription = "Fare Difference";
                            break;
                        case "ORIGINALTOTAL":
                            price.PriceTypeDescription = "Original trip";
                            originalTotal += price.Value;
                            break;
                        case "ORIGINALTAXTOTAL":
                            originalTotal += price.Value;
                            break;
                        case "REFUNDPRICE":
                            price.PriceTypeDescription = "Total refund";
                            if (price.Status != string.Empty)
                            {
                                var refundTypeMessage = GetOriginalFormOfPaymentLabelForReshopChange(price.Status, cslReservation);
                                if (refundTypeMessage != string.Empty)
                                {
                                    availability.Reservation.Reshop.RefundFormOfPaymentMessage = refundTypeMessage;
                                    if (price.Status != "CC")
                                    {
                                        availability.Reservation.Reshop.IsRefundBillingAddressRequired = true;
                                    }
                                }

                                if (_shoppingUtility.IncludeReshopFFCResidual(session.AppID, session.VersionID))
                                {
                                    if (string.Equals(price.Status, "FFCR", StringComparison.OrdinalIgnoreCase))
                                    {
                                        price.Status = "RESIDUALCREDIT";
                                        price.PriceTypeDescription = "Total credit";
                                        availability.Reservation.Reshop.RefundFormOfPaymentMessage = string.Empty;
                                    }
                                }
                                else
                                {
                                    if (price.Status == "NORESIDUALCREDIT")
                                    {
                                        isNonResidualCredit = true;
                                    }
                                }
                            }
                            break;
                        case "CHANGEFEE":
                            price.PriceTypeDescription = "Change fee";
                            isChangeFeeExistInPrices = true;
                            if ((price.Waived || changeWaivedPriceValue == "Waived") && price.Value == 0)
                            {
                                price.FormattedDisplayValue = "Waived";
                                availability.Reservation.Reshop.FeeWaiverMessage = (_configuration.GetValue<string>("ReshopChange-FEEWAIVEDMESSAGE") ?? "");

                                if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                                {
                                    price.FormattedDisplayValue = "No Fee";
                                    availability.Reservation.Reshop.FeeWaiverMessage = string.Empty;
                                }
                            }
                            else
                            {
                                availability.Reservation.Reshop.FeeWaiverMessage = string.Empty;
                            }
                            break;
                        case "TOTAL":
                            price.PriceTypeDescription = "Total due";
                            isAddCollectExistInPrices = true;
                            totalWithChangeFee += price.Value;
                            break;

                    }
                }

                if (!isChangeFeeExistInPrices)
                {
                    if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                    {
                        reservation.Prices.Add(GetPriceObject("CHANGEFEE", "Change fee", 0, "No Fee"));
                    }
                    else
                    {
                        reservation.Prices.Add(GetPriceObject("CHANGEFEE", "Change fee", 0, (changeWaivedPriceValue == "Waived" ? "Waived" : "$0")));
                    }

                    if (changeWaivedPriceValue == "Waived")
                    {
                        availability.Reservation.Reshop.FeeWaiverMessage = (_configuration.GetValue<string>("ReshopChange-FEEWAIVEDMESSAGE") ?? "");

                        if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                        {
                            availability.Reservation.Reshop.FeeWaiverMessage = string.Empty;
                        }
                    }
                    else
                    {
                        availability.Reservation.Reshop.FeeWaiverMessage = string.Empty;
                    }
                }

                if (isNonResidualCredit)
                {
                    if (!_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                    {
                        string nonResidualMessageFromConfig = _configuration.GetValue<string>("Reshop_NonResidualCreditMessage");

                        availability.Reservation.Reshop.FeeWaiverMessage = availability.Reservation.Reshop.FeeWaiverMessage == string.Empty ?
                                                nonResidualMessageFromConfig :
                                                string.Format("{0}\n\n{1}", availability.Reservation.Reshop.FeeWaiverMessage, nonResidualMessageFromConfig);
                        availability.Reservation.Reshop.DisplayNonResidualCreditMessage = true;
                    }

                    var removeRefundPrice = availability.Reservation.Prices.SingleOrDefault(r => r.DisplayType == "REFUNDPRICE");
                    if (removeRefundPrice != null)
                        availability.Reservation.Prices.Remove(removeRefundPrice);
                }

                if (!isAddCollectExistInPrices)
                {
                    reservation.Prices.Add(GetPriceObject("ADDCOLLECT", "Total due", 0));
                }

                var totalPrice = reservation.Prices.First(p => p.DisplayType == "TOTAL");
                AssignValueToPriceAndDisplayFormat(totalWithChangeFee, totalPrice, false);

                var originalPrice = reservation.Prices.First(p => p.DisplayType == "ORIGINALTOTAL");
                AssignValueToPriceAndDisplayFormat(originalTotal, originalPrice, true);

                var newTOtal = reservation.Prices.First(p => p.DisplayType == "NEWTOTAL");
                reservation.Prices.Add(GetPriceObject("NEWBASETOTAL", newTOtal.PriceTypeDescription, newTOtal.Value));
                AssignValueToPriceAndDisplayFormat(newtaxtotal, newTOtal, false);

                if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                {
                    availability.Reservation.Reshop.RTIChangeCancelTxt = string.Empty;

                    if (isNonResidualCredit)
                    {
                        var removeoriginaltotal = availability.Reservation.Prices.SingleOrDefault(r => r.DisplayType == "ORIGINALTOTAL");
                        if (removeoriginaltotal != null)
                            availability.Reservation.Prices.Remove(removeoriginaltotal);

                        var updatenewtotal = availability.Reservation.Prices.SingleOrDefault(r => r.DisplayType == "NEWTOTAL");
                        if (updatenewtotal != null)
                        {
                            availability.Reservation.Reshop.RTIChangeCancelTxt
                                = string.Format(_configuration.GetValue<string>("ReshopRTIChangeCancelText"), updatenewtotal.FormattedDisplayValue);
                            updatenewtotal.FormattedDisplayValue = string.Empty;
                        }
                    }
                } //End of --EnableReshopChangeFeeElimination

            }
        }

        private void AwardUpdatePricesAndTaxesForReshopChangeFlight(MOBSHOPAvailability availability, Session session, United.Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation)
        {
            var reservation = availability.Reservation;
            availability.Trip = null;
            availability.Reservation.Reshop.IsRefundBillingAddressRequired = false;
            availability.Reservation.Reshop.AncillaryRefundFormOfPayment = "To original form of payment";
            reservation.Taxes.RemoveAll(p => p.TaxCode.ToUpper() == "CHANGEFEE" || p.TaxCode.ToUpper() == "ADDCOLLECT");

            bool isChangeFeeExistInPrices = false;
            bool isAddCollectExistInPrices = false;
            double newtaxtotal = 0;
            double originalTotal = 0;
            double totalWithChangeFee = 0;

            string waivedDesc = "";
            string changeWaivedPriceValue = "";

            List<Model.Shopping.MOBSHOPPrice> awardNewPrices = new List<Model.Shopping.MOBSHOPPrice>();

            //var cslReservation = United.Persist.FilePersist.Load<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(Convert.ToString(session.SessionId), (new United.Service.Presentation.ReservationResponseModel.ReservationDetail()).GetType().FullName);
            if (cslReservation != null && cslReservation.Detail != null && !cslReservation.Detail.Characteristic.IsNullOrEmpty())
            {
                waivedDesc = ShopStaticUtility.GetCharacteristicDescription(cslReservation.Detail.Characteristic.ToList(), "24HrFlexibleBookingPolicy");
            }

            if (!string.IsNullOrEmpty(waivedDesc))
            {
                //reservation.Prices.Add(GetPriceObject("CHANGEFEE", "Change fee", 0, "Waived"));
                changeWaivedPriceValue = "Waived";
            }

            foreach (var price in reservation.Prices)
            {
                switch (price.DisplayType.ToUpper())
                {
                    case "NEWTOTAL":
                    case "TRAVELERPRICE":
                    case "NEWTAXTOTAL":
                        var totalTravels = (reservation.TravelersCSL != null ? reservation.TravelersCSL.Count : 0);
                        var totlaTravelsString = string.Empty;
                        if (totalTravels > 1)
                        {
                            totlaTravelsString = " (" + totalTravels.ToString() + " travelers)";
                        }
                        else if (totalTravels == 1)
                        {
                            totlaTravelsString = " (" + totalTravels.ToString() + " traveler)";
                        }
                        price.PriceTypeDescription = "New trip" + totlaTravelsString;
                        if (price.DisplayType.ToUpper() == "NEWTAXTOTAL" || price.DisplayType.ToUpper() == "NEWTOTAL")
                        {
                            newtaxtotal += price.Value;
                        }
                        break;
                    case "NEWMILES":
                        Model.Shopping.MOBSHOPPrice milesPrice = new Model.Shopping.MOBSHOPPrice();
                        milesPrice.CurrencyCode = price.CurrencyCode;
                        milesPrice.DisplayType = "NEWTOTAL";
                        milesPrice.DisplayValue = price.DisplayValue;
                        milesPrice.PriceIndex = price.PriceIndex;
                        milesPrice.PriceType = price.PriceType;
                        milesPrice.Status = price.Status;
                        milesPrice.TotalBaseFare = price.TotalBaseFare;
                        milesPrice.TotalOtherTaxes = price.TotalOtherTaxes;
                        milesPrice.Value = price.Value;
                        milesPrice.Waived = price.Waived;

                        var totalMilesTravels = (reservation.TravelersCSL != null ? reservation.TravelersCSL.Count : 0);
                        var totalTravelsString = string.Empty;
                        if (totalMilesTravels > 1)
                        {
                            totalTravelsString = " (" + totalMilesTravels.ToString() + " travelers)";
                        }
                        else if (totalMilesTravels == 1)
                        {
                            totalTravelsString = " (" + totalMilesTravels.ToString() + " traveler)";
                        }
                        milesPrice.PriceTypeDescription = "New trip" + totalTravelsString;
                        milesPrice.FormattedDisplayValue = string.Format("{0:#,##0}", price.Value).Replace(".00", string.Empty) + " miles";
                        awardNewPrices.Add(milesPrice);
                        Model.Shopping.MOBSHOPPrice newBaseMiles = new Model.Shopping.MOBSHOPPrice();
                        newBaseMiles = milesPrice.Clone();
                        newBaseMiles.DisplayType = "NEWBASETOTAL";
                        awardNewPrices.Add(newBaseMiles);
                        break;
                    case "ORIGINALTOTAL":
                        if (reservation.AwardTravel)
                        {
                            price.DisplayType = "ORIGINALTOTAL";
                            price.PriceTypeDescription = "Original trip";
                            price.FormattedDisplayValue = "- " + string.Format("{0:#,##0}", price.Value).Replace(".00", string.Empty) + " miles";
                        }
                        else
                        {
                            price.PriceTypeDescription = "Original trip";
                            originalTotal += price.Value;
                        }
                        break;
                    case "TAXDIFFERENCE":
                        price.PriceTypeDescription = "Tax Difference";
                        break;
                    case "ORIGINALTAXTOTAL":
                        if (!reservation.AwardTravel)
                        {
                            originalTotal += price.Value;
                        }
                        break;
                    case "REFUNDPRICE":
                        if (!reservation.AwardTravel)
                        {
                            price.PriceTypeDescription = "Total refund";
                            var refundTypeMessage = GetOriginalFormOfPaymentLabelForReshopChange(price.Status, cslReservation);
                            if (refundTypeMessage != string.Empty)
                            {
                                availability.Reservation.Reshop.RefundFormOfPaymentMessage = refundTypeMessage;
                                if (price.Status != "CC")
                                {
                                    availability.Reservation.Reshop.IsRefundBillingAddressRequired = true;
                                }
                            }
                        }
                        else
                        {
                            price.DisplayType = "OLDREFUND";
                        }
                        break;
                    case "MILEAGEDIFFERENCE":
                        if (reservation.AwardTravel)
                        {
                            Model.Shopping.MOBSHOPPrice refundPrice = new Model.Shopping.MOBSHOPPrice();
                            refundPrice.CurrencyCode = price.CurrencyCode;
                            refundPrice.DisplayValue = price.DisplayValue;
                            refundPrice.PriceIndex = price.PriceIndex;
                            refundPrice.PriceType = price.PriceType;
                            refundPrice.Status = price.Status;
                            refundPrice.TotalBaseFare = price.TotalBaseFare;
                            refundPrice.TotalOtherTaxes = price.TotalOtherTaxes;
                            refundPrice.Value = price.Value;
                            refundPrice.Waived = price.Waived;

                            Model.Shopping.MOBSHOPPrice totalPrice = new Model.Shopping.MOBSHOPPrice();
                            totalPrice.CurrencyCode = price.CurrencyCode;
                            totalPrice.DisplayValue = price.DisplayValue;
                            totalPrice.PriceIndex = price.PriceIndex;
                            totalPrice.PriceType = price.PriceType;
                            totalPrice.Status = price.Status;
                            totalPrice.TotalBaseFare = price.TotalBaseFare;
                            totalPrice.TotalOtherTaxes = price.TotalOtherTaxes;
                            totalPrice.Value = price.Value;
                            totalPrice.Waived = price.Waived;

                            var refundTypeMessage = GetOriginalFormOfPaymentLabelForReshopChange(price.Status, cslReservation);
                            if (refundTypeMessage != string.Empty)
                            {
                                availability.Reservation.Reshop.RefundFormOfPaymentMessage = refundTypeMessage;
                                if (price.Status != "CC")
                                {
                                    availability.Reservation.Reshop.IsRefundBillingAddressRequired = true;
                                }
                            }

                            if (price.Value == 0)
                            {
                                refundPrice.Value = 0;
                                refundPrice.PriceTypeDescription = "Total refund";
                                refundPrice.DisplayType = "REFUNDPRICE";
                                refundPrice.FormattedDisplayValue = "0 miles";
                                awardNewPrices.Add(refundPrice);
                            }
                            if (price.Value < 0)
                            {
                                refundPrice.PriceTypeDescription = "Total refund";
                                refundPrice.DisplayType = "REFUNDPRICE";
                                refundPrice.FormattedDisplayValue = string.Format("{0:#,##0}", price.Value).Trim('-').Replace(".00", string.Empty) + " miles";
                                awardNewPrices.Add(refundPrice);

                                totalPrice.PriceTypeDescription = "Total due";
                                totalPrice.DisplayType = "TOTAL";
                                totalPrice.DisplayValue = "0.0";
                                totalPrice.Value = 0.0;
                                totalPrice.FormattedDisplayValue = "0 miles";
                                awardNewPrices.Add(totalPrice);
                            }
                            if (price.Value >= 0)
                            {
                                refundPrice.Value = 0;
                                refundPrice.PriceTypeDescription = "Total refund";
                                refundPrice.DisplayType = "REFUNDPRICE";
                                refundPrice.DisplayValue = "0.0";
                                refundPrice.FormattedDisplayValue = "0 miles";
                                awardNewPrices.Add(refundPrice);

                                totalPrice.PriceTypeDescription = "Total due";
                                totalPrice.DisplayType = "TOTAL";
                                totalPrice.Value = 0;
                                totalPrice.DisplayValue = "0.0";
                                totalPrice.FormattedDisplayValue = string.Format("{0:#,##0}", price.Value).Trim('-').Replace(".00", string.Empty) + " miles";
                                awardNewPrices.Add(totalPrice);
                            }
                        }
                        break;
                    case "CHANGEFEE":
                        price.PriceTypeDescription = "Change fee";
                        isChangeFeeExistInPrices = true;
                        if ((price.Waived || changeWaivedPriceValue == "Waived") && price.Value == 0)
                        {
                            price.FormattedDisplayValue = "Waived";
                            availability.Reservation.Reshop.FeeWaiverMessage = (_configuration.GetValue<string>("ReshopChange-FEEWAIVEDMESSAGE") ?? "");
                        }
                        else
                        {
                            availability.Reservation.Reshop.FeeWaiverMessage = string.Empty;
                        }
                        break;
                    case "TOTAL":
                        price.PriceTypeDescription = "Total due";
                        if (!reservation.AwardTravel)
                        {

                            isAddCollectExistInPrices = true;
                            totalWithChangeFee += price.Value;
                        }
                        else
                        {
                            price.DisplayType = "OLDTOTAL";
                        }
                        break;
                }
            }

            foreach (var newPrice in awardNewPrices)
            {
                reservation.Prices.Add(newPrice);
            }
            //AddedNewTotalDollarAmount(reservation);
            if (reservation.AwardTravel)
            {
                try
                {
                    // New total dollar amount
                    var newTotalDollar = reservation.Prices.FirstOrDefault(x => x.DisplayType == "TAX");
                    if (reservation.Prices.FirstOrDefault(x => x.DisplayType == "RBF") != null)
                        reservation.Prices.Add(GetPriceObject("NEW_TOTAL_DOLLAR_AMOUNT", string.Empty, (Convert.ToDouble(newTotalDollar.DisplayValue) + Convert.ToDouble(reservation.Prices.FirstOrDefault(x => x.DisplayType == "RBF").DisplayValue))));
                    else
                        reservation.Prices.Add(GetPriceObject("NEW_TOTAL_DOLLAR_AMOUNT", string.Empty, Convert.ToDouble(newTotalDollar.DisplayValue)));
                    // Original total dollar amount
                    var orginalTotalDollar = reservation.Prices.FirstOrDefault(x => x.DisplayType.ToUpper() == "ORIGINALTAXTOTAL");
                    reservation.Prices.Add(GetPriceObject("ORIGINAL_TOTAL_DOLLAR_AMOUNT", string.Empty, Convert.ToDouble(orginalTotalDollar.DisplayValue)));
                    var originalDollarAmount = reservation.Prices.First(p => p.DisplayType == "ORIGINAL_TOTAL_DOLLAR_AMOUNT");
                    originalDollarAmount.FormattedDisplayValue = "- " + originalDollarAmount.FormattedDisplayValue;
                    originalDollarAmount.PriceTypeDescription = "Taxes and Fees Total";
                    orginalTotalDollar.DisplayType = "OLDORIGINALTAXTOTAL";

                    // Grand total dollar amount                                            
                    double grandTotal = Convert.ToDouble(ReshopAwardAirfareDisplayValueInDecimal(reservation.Prices));
                    if (grandTotal >= 0)
                    {
                        reservation.Prices.Add(GetPriceObject("GRAND TOTAL", string.Empty, grandTotal));
                    }
                    else
                    {
                        reservation.Prices.Add(GetPriceObject("GRAND TOTAL", string.Empty, 0.0));
                    }

                    availability.Reservation.Reshop.IsRefundBillingAddressRequired = false;

                    double taxDifference = 0.0;
                    if (reservation.Prices.Exists(p => p.DisplayType.ToUpper() == "TAXDIFFERENCE"))
                    {
                        taxDifference = Convert.ToDouble(reservation.Prices.First(p => p.DisplayType.ToUpper() == "TAXDIFFERENCE").Value);
                        string status = reservation.Prices.First(p => p.DisplayType.ToUpper() == "TAXDIFFERENCE").Status;
                        availability.Reservation.Reshop.RefundFormOfPaymentMessage = string.Empty;
                        if (taxDifference < 0)
                        {
                            reservation.Prices.Add(GetPriceObject("REFUND_PRICE_DOLLAR_AMOUNT", string.Empty, taxDifference * (-1)));

                            var refundTypeMessage = GetOriginalFormOfPaymentLabelForReshopChange(status, cslReservation);
                            if (refundTypeMessage != string.Empty)
                            {
                                availability.Reservation.Reshop.RefundFormOfPaymentMessage = refundTypeMessage;
                                if (status != "CC")
                                {
                                    availability.Reservation.Reshop.IsRefundBillingAddressRequired = true;
                                }
                            }
                            availability.Reservation.Reshop.RefundFormOfPaymentMessage = "To original form of payment";
                        }
                        else
                        {
                            reservation.Prices.Add(GetPriceObject("REFUND_PRICE_DOLLAR_AMOUNT", string.Empty, 0.0));
                            availability.Reservation.Reshop.RefundFormOfPaymentMessage = string.Empty;
                        }
                    }
                    else
                    {
                        reservation.Prices.Add(GetPriceObject("REFUND_PRICE_DOLLAR_AMOUNT", string.Empty, 0.0));
                    }

                    AwardTotalTravelOptionAndRefund(ref reservation);

                    PopulateRefundIfDollarAmountIsNotZero(reservation);
                }
                catch { }
            }

            if (!isChangeFeeExistInPrices)
            {
                reservation.Prices.Add(GetPriceObject("CHANGEFEE", "Change fee", 0, (changeWaivedPriceValue == "Waived" ? "Waived" : "$0")));
                if (changeWaivedPriceValue == "Waived")
                {
                    availability.Reservation.Reshop.FeeWaiverMessage = (_configuration.GetValue<string>("ReshopChange-FEEWAIVEDMESSAGE") ?? "");
                }
                else
                {
                    availability.Reservation.Reshop.FeeWaiverMessage = string.Empty;
                }
            }


            if (!isAddCollectExistInPrices)
            {
                reservation.Prices.Add(GetPriceObject("ADDCOLLECT", "Total due", 0));
            }

            if (!reservation.AwardTravel)
            {
                var totalPrice = reservation.Prices.First(p => p.DisplayType == "TOTAL");
                AssignValueToPriceAndDisplayFormat(totalWithChangeFee, totalPrice, false);

                var originalPrice = reservation.Prices.First(p => p.DisplayType == "ORIGINALTOTAL");
                AssignValueToPriceAndDisplayFormat(originalTotal, originalPrice, true);

                var newTOtal = reservation.Prices.First(p => p.DisplayType == "NEWTOTAL");
                reservation.Prices.Add(GetPriceObject("NEWBASETOTAL", newTOtal.PriceTypeDescription, newTOtal.Value));
                AssignValueToPriceAndDisplayFormat(newtaxtotal, newTOtal, false);
            }
        }
        private string GetOriginalFormOfPaymentLabelForReshopChange(string status, United.Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation)
        {
            string msg = string.Empty;
            if (status == "PP" || status == "AP")
            {
                msg = string.Format((_configuration.GetValue<string>("ReshopChange-RTIOriginalFOPMessage") ?? ""), "PayPal");
            }
            else if (status == "ETC")
            {
                msg = string.Format(_configuration.GetValue<string>("ReshopChange-RTIElectronicCertMessage") ?? "");
            }
            else if (status == "CC")
            {
                var cslPayment = cslReservation.Detail.Payment;
                var lastFourDigits = string.Empty;
                if ((!string.IsNullOrEmpty(cslPayment.AccountNumber)))
                {
                    lastFourDigits = "**" + (cslPayment.AccountNumber.Length > 4 ? cslPayment.AccountNumber.Substring(cslPayment.AccountNumber.Length - 4) : cslPayment.AccountNumber);
                }

                if (!string.IsNullOrEmpty(lastFourDigits))
                {
                    msg = string.Format((_configuration.GetValue<string>("ReshopChange-RTIOriginalFOPMessage") ?? ""), lastFourDigits);
                }

            }

            return msg;
        }

        private void PopulateRefundIfDollarAmountIsNotZero(MOBSHOPReservation reservation)
        {
            var refundDollarAmount = reservation.Prices.First(p => p.DisplayType == "REFUND_PRICE_DOLLAR_AMOUNT");
            var refundMiles = reservation.Prices.First(p => p.DisplayType == "REFUNDPRICE");

            if (refundDollarAmount.Value == 0 && refundMiles.Value == 0)
            {
                reservation.Prices.RemoveWhere(p => p.DisplayType == "REFUND_PRICE_DOLLAR_AMOUNT");
                reservation.Prices.RemoveWhere(p => p.DisplayType == "REFUNDPRICE");
            }
        }
        private void AwardTotalTravelOptionAndRefund(ref MOBSHOPReservation reservation)
        {
            // To set REFUND_PRICE_DOLLAR_AMOUNT starts here ....
            var refundPriceDollarAmount
                = reservation.Prices.FirstOrDefault(x => string.Equals(x.DisplayType, "REFUND_PRICE_DOLLAR_AMOUNT", StringComparison.OrdinalIgnoreCase));

            var totalTravelOptionRefund
                = reservation.Prices.FirstOrDefault(x => string.Equals(x.DisplayType, "TOTALTRAVELOPTIONREFUND", StringComparison.OrdinalIgnoreCase));

            if (totalTravelOptionRefund != null && totalTravelOptionRefund.Value > 0)
            {
                decimal totalrefund = (totalTravelOptionRefund != null && refundPriceDollarAmount != null) ?
                                Convert.ToDecimal(totalTravelOptionRefund.DisplayValue) + Convert.ToDecimal(refundPriceDollarAmount.DisplayValue) :
                                Convert.ToDecimal(totalTravelOptionRefund.DisplayValue);

                CultureInfo ci = null;
                double tempDouble = 0;
                double.TryParse(Convert.ToString(totalrefund), out tempDouble);
                if (ci == null) ci = TopHelper.GetCultureInfo(totalTravelOptionRefund.CurrencyCode);

                refundPriceDollarAmount.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);
                refundPriceDollarAmount.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalrefund, ci, false);
                refundPriceDollarAmount.DisplayValue = string.Format("{0:#,0.00}", totalrefund);
            }
            //REFUND_PRICE_DOLLAR_AMOUNT ends here
        }

        private void AssignValueToPriceAndDisplayFormat(double priceValue, Model.Shopping.MOBSHOPPrice mobShopPrice, bool isFormattedDisplayValueShouldBeNegetive)
        {
            if (mobShopPrice != null)
            {
                CultureInfo ci = TopHelper.GetCultureInfo(mobShopPrice.CurrencyCode); ;
                mobShopPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(priceValue.ToString(), ci, false); ;// string.Format("{0:c}", price.Amount);
                if (isFormattedDisplayValueShouldBeNegetive)
                {
                    mobShopPrice.FormattedDisplayValue = "-" + mobShopPrice.FormattedDisplayValue;
                    priceValue = priceValue * -1;
                }
                mobShopPrice.Value = priceValue;
                mobShopPrice.DisplayValue = string.Format("{0:#,0.00}", mobShopPrice.Value);
            }
        }
        private Model.Shopping.MOBSHOPPrice GetPriceObject(string displayType, string priceTypeTitle, double value, string displayValue = "")
        {
            Model.Shopping.MOBSHOPPrice price = new Model.Shopping.MOBSHOPPrice();
            price.DisplayType = displayType;
            price.CurrencyCode = "USD";
            price.PriceTypeDescription = priceTypeTitle;
            price.Value = value;
            price.DisplayValue = string.IsNullOrEmpty(displayValue) ? value.ToString() : displayValue;
            var ci = TopHelper.GetCultureInfo(price.CurrencyCode);
            price.FormattedDisplayValue = string.IsNullOrEmpty(displayValue) ? TopHelper.FormatAmountForDisplay(price.Value.ToString(), ci, false) : displayValue;
            price.PriceType = displayType;
            return price;
        }
        private void ValidateAwardReshopMileageBalance(bool IsMileagePurchaseRequired)
        {
            if (IsMileagePurchaseRequired)
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("NoEnoughMilesForAwardBooking"));
            }
        }
        private async Task<MOBResReservation> PopulateReservation(string sessionId, Service.Presentation.ReservationModel.Reservation reservation)
        {
            MOBResReservation mobReservation = null;
            if (reservation != null)
            {
                CSLReservation persistedCSLReservation = new CSLReservation();
                persistedCSLReservation = await _sessionHelperService.GetSession<CSLReservation>(sessionId, persistedCSLReservation.ObjectName, new List<string> { sessionId, persistedCSLReservation.ObjectName }).ConfigureAwait(false);
                if (persistedCSLReservation == null)
                {
                    persistedCSLReservation = new CSLReservation();
                    persistedCSLReservation.SessionId = sessionId;
                }
                if (persistedCSLReservation.Reservation == null)
                {
                    mobReservation = new MOBResReservation();
                    persistedCSLReservation.Reservation = mobReservation;
                }
                else
                {
                    mobReservation = persistedCSLReservation.Reservation;
                }

                mobReservation.FlightSegments = PopulateReservationFlightSegments(reservation.FlightSegments);
                mobReservation.TelephoneNumbers = PopulateReservationTelephone(reservation.TelephoneNumbers);
                mobReservation.Travelers = PopulateReservationTravelers(reservation.Travelers);

                mobReservation.IsRefundable =
                                    reservation.Characteristic.FirstOrDefault(
                                        x => x.Code?.ToUpper().Trim() == "Refundable".ToUpper().Trim() && SafeConverter.ToBoolean(x.Value)) != null;

                mobReservation.ISInternational = mobReservation.FlightSegments.FirstOrDefault(item => item.FlightSegment.IsInternational.ToUpper().Trim() == "TRUE") != null;
                await _sessionHelperService.SaveSession<CSLReservation>(persistedCSLReservation, sessionId, new List<string> { sessionId, persistedCSLReservation.ObjectName }, persistedCSLReservation.ObjectName).ConfigureAwait(false);
            }
            return mobReservation;
        }
        private List<MOBResTraveler> PopulateReservationTravelers(Collection<Service.Presentation.ReservationModel.Traveler> travelers)
        {
            List<MOBResTraveler> mobTravelers = null;

            if (travelers != null)
            {
                mobTravelers = new List<MOBResTraveler>();
                foreach (var traveler in travelers)
                {
                    MOBResTraveler mobTraveler = new MOBResTraveler();
                    if (traveler.Person != null)
                    {
                        mobTraveler.Person = new MOBPerPerson();
                        mobTraveler.Person.ChildIndicator = traveler.Person.ChildIndicator;
                        mobTraveler.Person.CustomerId = traveler.Person.CustomerID;
                        mobTraveler.Person.DateOfBirth = traveler.Person.DateOfBirth;
                        mobTraveler.Person.Title = traveler.Person.Title;
                        mobTraveler.Person.GivenName = traveler.Person.GivenName;
                        mobTraveler.Person.MiddleName = traveler.Person.MiddleName;
                        mobTraveler.Person.Surname = traveler.Person.Surname;
                        mobTraveler.Person.Suffix = traveler.Person.Suffix;
                        mobTraveler.Person.Suffix = traveler.Person.Sex;
                        if (traveler.Person.Documents != null)
                        {
                            mobTraveler.Person.Documents = new List<MOBPerDocument>();
                            foreach (var dcoument in traveler.Person.Documents)
                            {
                                MOBPerDocument mobDocument = new MOBPerDocument();
                                mobDocument.DocumentId = dcoument.DocumentID;
                                mobDocument.KnownTravelerNumber = dcoument.KnownTravelerNumber;
                                mobDocument.RedressNumber = dcoument.RedressNumber;
                                mobTraveler.Person.Documents.Add(mobDocument);
                            }
                        }
                    }

                    if (traveler.LoyaltyProgramProfile != null)
                    {
                        mobTraveler.LoyaltyProgramProfile = new MOBComLoyaltyProgramProfile();
                        mobTraveler.LoyaltyProgramProfile.CarrierCode = traveler.LoyaltyProgramProfile.LoyaltyProgramCarrierCode;
                        mobTraveler.LoyaltyProgramProfile.MemberId = traveler.LoyaltyProgramProfile.LoyaltyProgramMemberID;
                    }

                    mobTravelers.Add(mobTraveler);
                }
            }

            return mobTravelers;
        }

        private decimal ReshopAwardAirfareDisplayValueInDecimal(List<PricingItem> price, bool isChangeFee = false)
        {
            decimal retCloseInFee = 0;
            decimal retChangeFee = 0;
            decimal retTax = 0;

            if (price.Exists(p => p.PricingType.ToUpper() == "CLOSEINFEE") && isChangeFee)
            {
                retCloseInFee = price.First(p => p.PricingType.ToUpper() == "CLOSEINFEE").Amount;
            }
            if (price.Exists(p => p.PricingType.ToUpper() == "CHANGEFEE") && isChangeFee)
            {
                retChangeFee = price.First(p => p.PricingType.ToUpper() == "CHANGEFEE").Amount;
            }
            if (price.Exists(p => p.PricingType.ToUpper() == "SALETAXTOTAL"))
            {
                retTax = price.First(p => p.PricingType.ToUpper() == "SALETAXTOTAL").Amount;
            }

            return retCloseInFee + retChangeFee + retTax;
        }
        private List<MOBComTelephone> PopulateReservationTelephone(Collection<Service.Presentation.CommonModel.Telephone> telephones)
        {
            List<MOBComTelephone> mobTelephones = null;

            if (telephones != null)
            {
                mobTelephones = new List<MOBComTelephone>();
                foreach (var telephone in telephones)
                {
                    MOBComTelephone mobTelephone = new MOBComTelephone();
                    mobTelephone.AreaCityCode = telephone.AreaCityCode;
                    mobTelephone.PhoneNumber = telephone.PhoneNumber;
                    mobTelephone.Description = telephone.Description;

                    mobTelephones.Add(mobTelephone);
                }
            }

            return mobTelephones;
        }

        private List<MOBSegReservationFlightSegment> PopulateReservationFlightSegments(System.Collections.ObjectModel.Collection<Service.Presentation.SegmentModel.ReservationFlightSegment> segments)
        {
            List<MOBSegReservationFlightSegment> reservationflightSegments = null;

            if (segments != null && segments.Count > 0)
            {
                reservationflightSegments = new List<MOBSegReservationFlightSegment>();
                foreach (var segment in segments)
                {
                    MOBSegReservationFlightSegment reservationFlightSegment = new MOBSegReservationFlightSegment();

                    reservationFlightSegment.FlightSegment = PopulateFlightSegment(segment.FlightSegment);

                    reservationflightSegments.Add(reservationFlightSegment);
                }
            }

            return reservationflightSegments;
        }

        private SegFlightSegment PopulateFlightSegment(Service.Presentation.SegmentModel.FlightSegment segment)
        {
            SegFlightSegment flightSegment = null;

            if (segment != null)
            {
                flightSegment = new SegFlightSegment();
                flightSegment.ArrivalAirport = PopulateAirport(segment.ArrivalAirport);
                flightSegment.BookingClasses = PopulateBookingClasses(segment.BookingClasses);
                flightSegment.DepartureAirport = PopulateAirport(segment.DepartureAirport);
                flightSegment.DepartureDateTime = segment.DepartureDateTime;
                flightSegment.FlightNumber = segment.FlightNumber;
                flightSegment.OperatingAirlineCode = segment.OperatingAirlineCode;
                flightSegment.OperatingAirlineName = segment.OperatingAirlineName;
                flightSegment.IsInternational = segment.IsInternational;
            }

            return flightSegment;
        }
        private MOBTMAAirport PopulateAirport(Service.Presentation.CommonModel.AirportModel.Airport airport)
        {
            MOBTMAAirport mobAirport = null;

            if (airport != null)
            {
                mobAirport = new MOBTMAAirport();
                mobAirport.IATACode = airport.IATACode;
                mobAirport.Name = airport.Name;
                mobAirport.ShortName = airport.ShortName;
            }

            return mobAirport;
        }

        private List<ComBookingClass> PopulateBookingClasses(System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.BookingClass> bookingClasses)
        {
            List<ComBookingClass> mobBookingClasses = null;

            if (bookingClasses != null && bookingClasses.Count > 0)
            {
                mobBookingClasses = new List<ComBookingClass>();
                foreach (var bookingClass in bookingClasses)
                {
                    ComBookingClass mobBookingClass = new ComBookingClass();
                    mobBookingClass.Code = bookingClass.Code;
                    mobBookingClasses.Add(mobBookingClass);
                }
            }

            return mobBookingClasses;
        }

        private void AssignCorporateRate(MOBSHOPReservation reservation, bool isShopRequestCorporateBooking, bool isArranger = false)
        {
            bool isCorporateBooking = Convert.ToBoolean(_configuration.GetValue<string>("CorporateConcurBooking") ?? "false");
            string corporateFareText = _configuration.GetValue<string>("CorporateFareIndicator") ?? string.Empty;
            //bool hasMatchCorpDisc = segment.Products.Any(p => p.ProductSubtype.Contains("CORPDISC"));

            bool isCorporateFareSelected =
                reservation.Trips.Any(
                    x =>
                        x.FlattenedFlights.Any(
                            f =>
                                f.Flights.Any(
                                    fl =>
                                        fl.CorporateFareIndicator ==
                                        corporateFareText.ToString())));
            if (isCorporateBooking && isShopRequestCorporateBooking)
            {
                reservation.ShopReservationInfo = new Model.Shopping.ReservationInfo()
                {
                    IsCorporateBooking = true,
                    CorporateSuppressSavedTravelerMessage = (isArranger) ? string.Empty : _configuration.GetValue<string>("CorporateSuppressSavedTravelerMessage") ?? string.Empty

                };
                if (isCorporateFareSelected)
                {
                    reservation.ShopReservationInfo.CorporateRate =
                        _configuration.GetValue<string>("CorporateRateText") ?? string.Empty;
                }
            }
            
        }
        private string CorporateSavedTravelerMessage(bool isArranger, bool isMulPax,bool isCorporate)
        {
            if (isCorporate && isMulPax)
            {
                return _configuration.GetValue<string>("CorporateSuppressSavedTravelerMessage") ?? string.Empty;
            }
            else if(isArranger && isMulPax)
            {
                return _configuration.GetValue<string>("CorporateSuppressSavedTravelerMessage") ?? string.Empty;
            }
            else if (isCorporate)
            {
                return _configuration.GetValue<string>("CorporateSuppressSavedTravelerMessage") ?? string.Empty;
            }
            else if (isArranger)
            {
                return _configuration.GetValue<string>("CorporateSuppressSavedTravelerMessage") ?? string.Empty;
            }
            return default;
        }

        private decimal ReshopAirfareDisplayValueInDecimal(List<PricingItem> price)
        {
            decimal retVal = 0;
            if (price.Exists(p => p.PricingType == "AddCollect"))
                retVal = price.First(p => p.PricingType == "AddCollect").Amount;
            if (price.Exists(p => p.PricingType == "ChangeFee"))
                retVal += price.First(p => p.PricingType == "ChangeFee").Amount;

            return retVal;
        }
        private decimal ReshopAwardAirfareDisplayValueInDecimal(List<Model.Shopping.MOBSHOPPrice> price)
        {
            decimal retCloseInFee = 0;
            decimal retChangeFee = 0;
            decimal retTaxDifference = 0;

            if (price.Exists(p => p.DisplayType.ToUpper() == "RBF"))
            {
                retCloseInFee = Convert.ToDecimal(price.First(p => p.DisplayType.ToUpper() == "RBF").DisplayValue);
            }
            if (price.Exists(p => p.DisplayType.ToUpper() == "CHANGEFEE"))
            {
                retChangeFee = Convert.ToDecimal(price.First(p => p.DisplayType.ToUpper() == "CHANGEFEE").DisplayValue);
            }
            if (price.Exists(p => p.DisplayType.ToUpper() == "TAXDIFFERENCE"))
            {
                retTaxDifference = Convert.ToDecimal(price.First(p => p.DisplayType.ToUpper() == "TAXDIFFERENCE").DisplayValue);
                if (retTaxDifference < 0)
                    retTaxDifference = 0;
            }
            return retCloseInFee + retChangeFee + retTaxDifference;
        }
        public List<MOBSHOPPrice> GetPrices(List<DisplayPrice> prices, bool isAwardBooking, string sessionId, bool isReshopChange = false,
            int appId = 0, string appVersion = "", List<MOBItem> catalogItems = null, FlightReservationResponse shopBookingDetailsResponse = null)
        {
            List<Model.Shopping.MOBSHOPPrice> bookingPrices = new List<Model.Shopping.MOBSHOPPrice>();
            bool isEnableOmniCartMVP2Changes = _configuration.GetValue<bool>("EnableOmniCartMVP2Changes");
            CultureInfo ci = null;
            foreach (var price in prices)
            {
                if (ci == null)
                {
                    ci = TopHelper.GetCultureInfo(price.Currency);
                }

                Model.Shopping.MOBSHOPPrice bookingPrice = new Model.Shopping.MOBSHOPPrice();
                bookingPrice.CurrencyCode = price.Currency;
                bookingPrice.DisplayType = price.Type;
                bookingPrice.Status = price.Status;
                bookingPrice.Waived = price.Waived;
                bookingPrice.DisplayValue = string.Format("{0:#,0.00}", price.Amount);

                if (!isReshopChange)
                {
                    if (!string.IsNullOrEmpty(bookingPrice.DisplayType) && bookingPrice.DisplayType.Equals("MILES") && isAwardBooking && !string.IsNullOrEmpty(sessionId))
                    {
                        ValidateAwardMileageBalance(sessionId, price.Amount, appId, appVersion, catalogItems);
                    }
                }

                double tempDouble = 0;
                double.TryParse(price.Amount.ToString(), out tempDouble);
                bookingPrice.Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero);

                if (price.Currency.ToUpper() == "MIL")
                {
                    bookingPrice.FormattedDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(price.Amount.ToString(), false);
                }
                else
                {
                    if (price.Amount < 0
                        && (string.Equals("TaxDifference", price.Type, StringComparison.OrdinalIgnoreCase)
                        || string.Equals("FareDifference", price.Type, StringComparison.OrdinalIgnoreCase)))
                        bookingPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(price.Amount * -1, ci, false);
                    else
                        bookingPrice.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(price.Amount, ci, false);
                }
                if (_shoppingUtility.EnableYADesc(isReshopChange) && price.PricingPaxType != null && price.PricingPaxType.ToUpper().Equals("UAY"))   //If Young adult account
                {
                    bookingPrice.PaxTypeDescription = $"{price.Count} {"young adult (18-23)"}".ToLower(); //string.Format("{0} {1}: {2} per person", price?.Count, "young adult (18-23)", price?.Amount);
                    if (isEnableOmniCartMVP2Changes)
                        bookingPrice.PaxTypeDescription = bookingPrice?.PaxTypeDescription.Replace(" per ", "/");
                }
                else
                    bookingPrice.PaxTypeDescription = $"{price.Count} {price.Description}".ToLower();

                _shoppingBuyMiles.UpdatePriceTypeDescForBuyMiles(appId, appVersion, catalogItems, shopBookingDetailsResponse, bookingPrice);
                bookingPrices.Add(bookingPrice);
            }

            return bookingPrices;
        }
        private async Task ValidateAwardMileageBalance(string sessionId, decimal milesNeeded, int appId = 0, string appVersion = "", List<MOBItem> catalogItems = null)
        {
            CSLShopRequest shopRequest = new CSLShopRequest();
            shopRequest = await _sessionHelperService.GetSession<CSLShopRequest>(sessionId, shopRequest.ObjectName, new List<string> { sessionId, shopRequest.ObjectName }).ConfigureAwait(false);
            if (shopRequest != null && shopRequest.ShopRequest != null && shopRequest.ShopRequest.AwardTravel && shopRequest.ShopRequest.LoyaltyPerson != null && shopRequest.ShopRequest.LoyaltyPerson.AccountBalances != null)
            {
                if (shopRequest.ShopRequest.LoyaltyPerson.AccountBalances[0] != null && shopRequest.ShopRequest.LoyaltyPerson.AccountBalances[0].Balance < milesNeeded)
                {
                    if (_shoppingUtility.IsBuyMilesFeatureEnabled(appId, appVersion, catalogItems) == false)
                        throw new MOBUnitedException(_configuration.GetValue<string>("NoEnoughMilesForAwardBooking"));
                }
            }
        }
        private List<Model.Shopping.MOBSHOPTax> GetTaxAndFees(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices, int numPax, bool isReshopChange = false)
        {
            List<Model.Shopping.MOBSHOPTax> taxsAndFees = new List<Model.Shopping.MOBSHOPTax>();
            CultureInfo ci = null;
            decimal taxTotal = 0.0M;
            bool isTravelerPriceDirty = false;
            string reshopTaxCodeDescription = string.Empty;
            bool isEnableOmniCartMVP2Changes = _configuration.GetValue<bool>("EnableOmniCartMVP2Changes");

            foreach (var price in prices)
            {
                if (price.SubItems != null && price.SubItems.Count > 0 &&
                    (!isReshopChange || (isReshopChange && price.Type.ToUpper() == "TRAVELERPRICE" && !isTravelerPriceDirty))
                    && price.Type.Trim().ToUpper() != "RBF" // Added by Hasnan - # 167553 - 10/04/2017
                   )
                {
                    foreach (var subItem in price.SubItems)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(subItem.Currency);
                        }
                        if (!_configuration.GetValue<bool>("DisableTaxValueFilterReshopDetails") && isReshopChange) //after discussion with Mahiban, we decided to go for Reshop flow only.
                        {
                            if (subItem.Value != null && String.Equals(Convert.ToString(subItem.Value), "Tax", StringComparison.OrdinalIgnoreCase))
                            {

                                MOBSHOPTax taxNfee = new MOBSHOPTax();
                                taxNfee.CurrencyCode = subItem.Currency;
                                taxNfee.Amount = subItem.Amount;
                                taxNfee.DisplayAmount = TopHelper.FormatAmountForDisplay(taxNfee.Amount, ci, false);
                                taxNfee.TaxCode = subItem.Type;
                                taxNfee.TaxCodeDescription = subItem.Description;
                                isTravelerPriceDirty = true;
                                taxsAndFees.Add(taxNfee);

                                taxTotal += taxNfee.Amount;
                            }
                        }

                        else
                        {
                            MOBSHOPTax taxNfee = new MOBSHOPTax();
                            taxNfee.CurrencyCode = subItem.Currency;
                            taxNfee.Amount = subItem.Amount;
                            taxNfee.DisplayAmount = TopHelper.FormatAmountForDisplay(taxNfee.Amount, ci, false);
                            taxNfee.TaxCode = subItem.Type;
                            taxNfee.TaxCodeDescription = subItem.Description;
                            isTravelerPriceDirty = true;
                            taxsAndFees.Add(taxNfee);

                            taxTotal += taxNfee.Amount;
                        }
                    }

                    reshopTaxCodeDescription = price.Description;
                }
                else if (price.Type.Trim().ToUpper() == "RBF") //Reward Booking Fee
                {
                    if (ci == null)
                    {
                        ci = TopHelper.GetCultureInfo(price.Currency);
                    }
                    Model.Shopping.MOBSHOPTax taxNfee = new Model.Shopping.MOBSHOPTax();
                    taxNfee.CurrencyCode = price.Currency;
                    taxNfee.Amount = price.Amount / numPax;
                    taxNfee.DisplayAmount = TopHelper.FormatAmountForDisplay(taxNfee.Amount, ci, false);
                    taxNfee.TaxCode = price.Type;
                    taxNfee.TaxCodeDescription = price.Description;
                    taxsAndFees.Add(taxNfee);

                    taxTotal += taxNfee.Amount;
                }
            }

            if (taxsAndFees != null && taxsAndFees.Count > 0)
            {
                //add new label as first item for UI
                Model.Shopping.MOBSHOPTax tnf = new Model.Shopping.MOBSHOPTax();
                tnf.CurrencyCode = taxsAndFees[0].CurrencyCode;
                tnf.Amount = taxTotal;
                tnf.DisplayAmount = TopHelper.FormatAmountForDisplay(tnf.Amount, ci, false);
                tnf.TaxCode = "PERPERSONTAX";
                tnf.TaxCodeDescription = string.Format("{0} {1}: {2}{3}", numPax,
                    (!string.IsNullOrEmpty(reshopTaxCodeDescription)) ? reshopTaxCodeDescription : (numPax > 1) ? "travelers" : "traveler", tnf.DisplayAmount
                    , isEnableOmniCartMVP2Changes ? "/person" : " per person");
                taxsAndFees.Insert(0, tnf);

                //add grand total for all taxes
                Model.Shopping.MOBSHOPTax tnfTotal = new Model.Shopping.MOBSHOPTax();
                tnfTotal.CurrencyCode = taxsAndFees[0].CurrencyCode;
                tnfTotal.Amount = taxTotal * numPax;
                tnfTotal.DisplayAmount = TopHelper.FormatAmountForDisplay(tnfTotal.Amount, ci, false);
                tnfTotal.TaxCode = "TOTALTAX";
                tnfTotal.TaxCodeDescription = "Taxes and Fees Total";
                taxsAndFees.Add(tnfTotal);

            }

            return taxsAndFees;
        }
        private FareLock GetFareLockOptions(United.Service.Presentation.ProductResponseModel.ProductOffer cslFareLock, double flightPrice, string currency, bool isAward, double miles, int appId, string appVersion, List<MOBItem> catalogItems = null)
        {
            FareLock shopFareLock = new FareLock();

            if (cslFareLock != null && cslFareLock.Offers != null && cslFareLock.Offers.Count > 0)
            {
                /// This code added to farelock with implementation of 14-day option to support old clients
                bool isEnableFarelockForOldClientsWithTwoOptions = _configuration.GetValue<bool>("IsEnableFarelocVersionForOldClients");
                bool isEnableFarelockOption = isEnableFarelockForOldClientsWithTwoOptions && !appId.IsNullOrEmpty() && !string.IsNullOrEmpty(appVersion) ? GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidFarelockOldClientVersion", "iPhoneFarelockOldClientVersion", "", "", true, _configuration) : false;
                string farelockCodeFor7Day = isEnableFarelockForOldClientsWithTwoOptions ? _configuration.GetValue<string>("FarelockCodeFor7DayOption") : string.Empty;
                CultureInfo ci = null;
                double lowest = 999999.9;
                string prodAmountDisplay = string.Empty;

                foreach (United.Service.Presentation.ProductResponseModel.Offer offer in cslFareLock.Offers)
                {
                    if (offer.ProductInformation != null && offer.ProductInformation.ProductDetails != null && offer.ProductInformation.ProductDetails.Count > 0)
                    {
                        shopFareLock.FareLockProducts = new List<FareLockProduct>();
                        foreach (United.Service.Presentation.ProductResponseModel.ProductDetail prodDetail in offer.ProductInformation.ProductDetails)
                        {
                            foreach (United.Service.Presentation.ProductModel.SubProduct subProduct in prodDetail.Product.SubProducts)
                            {
                                FareLockProduct flProd = new FareLockProduct();
                                foreach (United.Service.Presentation.ProductModel.ProductPriceOption prodPrice in subProduct.Prices)
                                {
                                    if (ci == null)
                                    {
                                        ci = TopHelper.GetCultureInfo(prodPrice.PaymentOptions[0].PriceComponents[0].Price.Totals[0].Currency.Code);
                                    }

                                    flProd.FareLockProductAmount = prodPrice.PaymentOptions[0].PriceComponents[0].Price.Totals[0].Amount;
                                    if(_shoppingUtility.IsEnableFareLockAmoutDisplayPerPerson(appId, appVersion, catalogItems))
                                    {
                                        prodAmountDisplay = (prodPrice.PaymentOptions[0].PriceComponents[0].Price?.Adjustments == null)
                                                ? flProd.FareLockProductAmount.ToString() : prodPrice.PaymentOptions[0].PriceComponents[0].Price?.Adjustments?.Where(a => a.Designator == _configuration.GetValue<string>("FarelockDesignatorKey"))?.FirstOrDefault()?.Result.ToString();
                                        flProd.FareLockProductAmountDisplayText =  (TopHelper.FormatAmountForDisplay(prodAmountDisplay, ci, false) + _configuration.GetValue<string>("FareLockPerPersonText"));
                                    }
                                    else
                                    {
                                        flProd.FareLockProductAmountDisplayText = TopHelper.FormatAmountForDisplay(flProd.FareLockProductAmount.ToString(), ci, false);
                                    }
                                   
                                    //Retrieving the ProductId and ProductCode inorder for the client to send it back to us when calling the RegisterFairLock.
                                    //Note: Since we are using "Shopping/cart/RegisterOffer" Instead of "flightShopping/RegisterFairlock".Old CSL doesn't require productCode.But "Shopping/Cart/RegisterOffer" productId/productCode is mandate.
                                    flProd.ProductId = prodPrice.ID;

                                    if (lowest == -1 || flProd.FareLockProductAmount < lowest)
                                        lowest = flProd.FareLockProductAmount;
                                }
                                flProd.ProductCode = prodDetail.Product.Code;
                                flProd.FareLockProductCode = subProduct.Code;
                                if (_shoppingUtility.IsAllFareLockOptionEnabled(appId, appVersion, catalogItems))
                                    flProd.FareLockProductTitle = "Hold for " + subProduct.Features[0].Value + " " + subProduct.Features[0].Name;
                                else
                                    flProd.FareLockProductTitle = subProduct.Features[0].Value + " " + subProduct.Features[0].Name;
                                if (isEnableFarelockForOldClientsWithTwoOptions)
                                {
                                    if (!isEnableFarelockOption && prodDetail.Product.SubProducts.Count > 2)
                                    {
                                        if (!subProduct.Code.Equals(farelockCodeFor7Day))
                                            shopFareLock.FareLockProducts.Add(flProd);
                                    }
                                    else
                                    {
                                        shopFareLock.FareLockProducts.Add(flProd);
                                    }
                                }
                                else
                                {
                                    shopFareLock.FareLockProducts.Add(flProd);
                                }
                            }
                        }

                        shopFareLock.FareLockDescriptionText = offer.ProductInformation.ProductDetails[0].Product.Description != null ? offer.ProductInformation.ProductDetails[0].Product.Description : "Farelock";
                        shopFareLock.FareLockHoldButtonText = _configuration.GetValue<string>("FareLockHoldButtonText"); //"Hold fare";
                        shopFareLock.FareLockTextTop = _configuration.GetValue<string>("FarelockTextTop");
                        shopFareLock.FareLockTextBottom = _configuration.GetValue<string>("FarelockTextBottom");
                        shopFareLock.FareLockMinAmount = lowest;
                        shopFareLock.FareLockDisplayMinAmount = TopHelper.FormatAmountForDisplay(lowest.ToString(), ci, true);
                        shopFareLock.FareLockTermsAndConditions = new List<string>();
                        shopFareLock.FareLockPurchaseButtonAmount = flightPrice;

                        if (isAward)
                        {
                            shopFareLock.FareLockPurchaseButtonAmountDisplayText = ShopStaticUtility.FormatAwardAmountForDisplay(miles.ToString(), true) + " + " + TopHelper.FormatAmountForDisplay(flightPrice.ToString(), ci, false);
                        }
                        else
                        {
                            shopFareLock.FareLockPurchaseButtonAmountDisplayText = TopHelper.FormatAmountForDisplay(flightPrice.ToString(), ci, false);
                        }
                        shopFareLock.FareLockPurchaseButtonText = _configuration.GetValue<string>("FareLockPurchaseButtonText");//"Purchase now";
                        shopFareLock.FareLockTitleText = _configuration.GetValue<string>("FareLockTitleText");//"FareLock";
                    }
                }
            }
            if (_shoppingUtility.IsAllFareLockOptionEnabled(appId, appVersion, catalogItems) && shopFareLock?.FareLockProducts?.Count > 0)
            {
                FareLockProduct flProd = new FareLockProduct();
                flProd.FareLockProductTitle = _configuration.GetValue<string>("FareLockTextContinueWithOutFareLock");
                flProd.ProductCode = shopFareLock.FareLockProducts.FirstOrDefault().ProductCode;
                flProd.ProductId = shopFareLock.FareLockProducts.FirstOrDefault().ProductId;
                shopFareLock.FareLockProducts.Insert(0, flProd);

            }
            return shopFareLock;
        }

        private string GetFareCurrency(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices)
        {
            string bookingCurrency = "";
            foreach (United.Services.FlightShopping.Common.DisplayCart.DisplayPrice price in prices)
            {
                if (price.Type.ToUpper() == "TOTAL")
                {
                    bookingCurrency = price.Currency;
                    break;
                }
            }
            return bookingCurrency;
        }
        private double GetFareMiles(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices)
        {
            double bookingPrice = 0;
            foreach (United.Services.FlightShopping.Common.DisplayCart.DisplayPrice price in prices)
            {
                if (price.Type.ToUpper() == "MILES")
                {
                    bookingPrice = (double)price.Amount;
                    break;
                }
            }
            return bookingPrice;
        }


        private async Task<List<MOBSHOPTrip>> PopulateTrips(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string cartId, ShoppingResponse persistShop, string sessionId, int appId, string deviceId, string appVersion, bool showMileageDetails, List<United.Services.FlightShopping.Common.DisplayCart.DisplayTrip> flightSegmentCollection, string fareClass, List<string> flightDepartDatesForSelectedTrip, MOBAdditionalItems additionalItems = null)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName });
            supressLMX = session.SupressLMXForAppID;
            #endregion
            List<MOBSHOPTrip> trips = new List<MOBSHOPTrip>();            
            try
            {
                if (_configuration.GetValue<bool>("EnableFecthAirportNameFromCSL"))
                {
                    airportsList = null;
                }
                else
                {
                    airportsList = await _sessionHelperService.GetSession<AirportDetailsList>(sessionId, new AirportDetailsList().GetType().FullName, new List<string> { sessionId, new AirportDetailsList().GetType().FullName }).ConfigureAwait(false);
                }
                if (isGetAirportListInOneCallToggleOn() && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    airportsList = await GetAllAiportsList(flightSegmentCollection);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllAiportsList {PopulateTrips-GetAllAiportsList}", JsonConvert.SerializeObject(ex));
                _logger.LogError("GetAllAiportsList {PopulateTrips-DisplayTrip}", JsonConvert.SerializeObject(flightSegmentCollection));
            }

            for (int i = 0; i < flightSegmentCollection.Count; i++)
            {
                MOBSHOPTrip trip = null;

                if (flightSegmentCollection != null && flightSegmentCollection.Count > 0)
                {
                    trip = new MOBSHOPTrip();
                    trip.TripId = flightSegmentCollection[i].BBXSolutionSetId;
                    trip.FlightCount = flightSegmentCollection[i].Flights.Count;


                    trip.DepartDate = GeneralHelper.FormatDateFromDetails(flightSegmentCollection[i].DepartDate);
                    trip.ArrivalDate = GeneralHelper.FormatDateFromDetails(flightSegmentCollection[i].Flights[flightSegmentCollection[i].Flights.Count - 1].DestinationDateTime);
                    trip.Destination = flightSegmentCollection[i].Destination;

                    // Fix for Partially used: To know change type
                    if (flightSegmentCollection[i].ChangeType == 0)
                        trip.ChangeType = MOBSHOPTripChangeType.ChangeFlight;
                    else if (flightSegmentCollection[i].ChangeType == 1)
                        trip.ChangeType = MOBSHOPTripChangeType.AddFlight;

                    CultureInfo ci = null;

                    List<Model.Shopping.MOBSHOPFlight> flights = null;
                    if (flightSegmentCollection[i].Flights != null && flightSegmentCollection[i].Flights.Count > 0)
                    {
                        //update amenities for all flights
                        UpdateAmenitiesIndicatorsResponse amenitiesResponse = new UpdateAmenitiesIndicatorsResponse();
                        List<Flight> tempFlights = new List<Flight>(flightSegmentCollection[i].Flights);

                        //we do not want the search to fail if one of these fail...
                        try
                        {

                            Parallel.Invoke(async () =>
                            {
                                bool includeAmenities = false;
                                bool.TryParse(_configuration.GetValue<string>("IncludeAmenities"), out includeAmenities);

                                //if we are asking for amenities in the CSL call, do not make this seperate call
                                if (!includeAmenities)
                                {
                                    amenitiesResponse = await _shopBooking.GetAmenitiesForFlights(sessionId, cartId, tempFlights, appId, deviceId, appVersion).ConfigureAwait(false);
                                    _shopBooking.PopulateFlightAmenities(amenitiesResponse.Profiles, ref tempFlights);
                                }
                            },
                                   async () =>
                                    {
                                        if (showMileageDetails && !supressLMX)
                                        {
                                            //get all flight numbers
                                            List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights = null;
                                            lmxFlights = await _shopBooking.GetLmxFlights(session.Token, cartId, GetFlightHasListForLMX(tempFlights), sessionId, appId, appVersion, deviceId);// GetFlightHasListForLMX(tempFlights));

                                            if (lmxFlights != null && lmxFlights.Count > 0)
                                                PopulateLMX(lmxFlights, ref tempFlights);//tempFlights = lmxFlights;
                                        }

                                    }
                                );
                        }
                        catch { };

                        flightSegmentCollection[i].Flights = new List<Flight>(tempFlights);
                        if (_mOBSHOPDataCarrier == null)
                            _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                        ShopBooking.countDisplay = 0;
                        MOBSearchFilters searchFilters = new MOBSearchFilters();
                        searchFilters = await _sessionHelperService.GetSession<MOBSearchFilters>(session.SessionId, searchFilters.GetType().FullName, new List<string> { session.SessionId, searchFilters.GetType().FullName }).ConfigureAwait(false);
                        flights = await _shopBooking.GetFlightsAsync(_mOBSHOPDataCarrier, sessionId, cartId, flightSegmentCollection[i].Flights, persistShop.Response.Availability.Trip.SearchFiltersIn.FareFamily, 0.0M, trip.Columns, persistShop.Request.PremierStatusLevel, fareClass, false, false, true, null, appVersion, appId, additionalItems: additionalItems, searchFilters: searchFilters);
                    }

                    trip.Origin = flightSegmentCollection[i].Origin;

                    if (showMileageDetails && !supressLMX)
                        trip.YqyrMessage = _configuration.GetValue<string>("MP2015YQYRMessage");

                    string originName = string.Empty;
                    if (!string.IsNullOrEmpty(trip.Origin))
                    {
                        originName = await GetAirportNameFromSavedList(trip.Origin);
                    }
                    if (string.IsNullOrEmpty(originName))
                    {
                        trip.OriginDecoded = flightSegmentCollection[i].Flights[0].OriginDescription;
                    }
                    else
                    {
                        trip.OriginDecoded = originName;
                    }

                    string destinationDecodedWithCountry = string.Empty;
                    if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI"))
                    {
                        trip.OriginDecodedWithCountry = flightSegmentCollection[i].Flights[0].OriginDescription;
                        foreach (var flight in flightSegmentCollection[i].Flights)
                        {
                            if (flight.Connections != null && flight.Connections.Count > 0)
                            {
                                foreach (var conn in flight.Connections)
                                {
                                    if (conn.Destination.Equals(flightSegmentCollection[i].Destination))
                                    {
                                        destinationDecodedWithCountry = conn.DestinationDescription;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (flight.Destination.Equals(flightSegmentCollection[i].Destination))
                                {
                                    destinationDecodedWithCountry = flight.DestinationDescription;
                                    break;
                                }
                            }
                        }
                        trip.DestinationDecodedWithCountry = destinationDecodedWithCountry;
                    }

                    string destinationName = string.Empty;
                    if (!string.IsNullOrEmpty(trip.Destination))
                    {
                        destinationName = await GetAirportNameFromSavedList(trip.Destination);
                    }
                    if (string.IsNullOrEmpty(destinationName))
                    {
                        if (_configuration.GetValue<bool>("EnableFecthAirportNameFromCSL"))
                        {
                            trip.DestinationDecoded = destinationDecodedWithCountry;
                        }
                        else
                        {
                            trip.DestinationDecoded = flightSegmentCollection[i].Flights[flightSegmentCollection[i].Flights.Count - 1].DestinationDescription;
                        }
                    }
                    else
                    {
                        trip.DestinationDecoded = destinationName;
                    }

                    if (flights != null)
                    {

                        string tripDepartDate = string.Empty;
                        foreach (string tripIDDepDate in flightDepartDatesForSelectedTrip)
                        {
                            if (tripIDDepDate.Split('|')[0].ToString().Trim() == trip.TripId)
                            {
                                tripDepartDate = tripIDDepDate.Split('|')[1].ToString().Trim();
                                break;
                            }
                        }
                        var isEnabledGMTConversionUsingCslData = _configuration.GetValue<bool>("EnableGMTConversionUsingCslData");
                        trip.FlattenedFlights = new List<Model.Shopping.MOBSHOPFlattenedFlight>();
                        foreach (var flight in flights)
                        {
                            Model.Shopping.MOBSHOPFlattenedFlight flattenedFlight = new Model.Shopping.MOBSHOPFlattenedFlight();
                            //need to overwrite trip id otherwise it's the previous trip's id
                            trip.TripId = flight.TripId;
                            flattenedFlight.TripId = flight.TripId;
                            flattenedFlight.FlightId = flight.FlightId;
                            flattenedFlight.ProductId = flight.ProductId;
                            flattenedFlight.Flights = new List<Model.Shopping.MOBSHOPFlight>();
                            if (_configuration.GetValue<bool>("EnableChangeOfAirport"))
                            {
                                flattenedFlight.AirportChange = flight.AirportChange;
                            }
                            flight.TripId = trip.TripId;
                            if (isEnabledGMTConversionUsingCslData) {
                                flight.DepartureDateTimeGMT = await _shoppingUtility.GetGMTTimeFromOffset(flight.DepartureDateTime, flight.OriginTimezoneOffset);
                                flight.ArrivalDateTimeGMT = await _shoppingUtility.GetGMTTimeFromOffset(flight.ArrivalDateTime, flight.DestinationTimezoneOffset);
                            }
                            else
                            {
                                flight.DepartureDateTimeGMT = await GetGMTTime(flight.DepartureDateTime, flight.Origin, sessionId);
                                flight.ArrivalDateTimeGMT = await GetGMTTime(flight.ArrivalDateTime, flight.Destination, sessionId);
                            }
                            
                            #region Red Eye Flight Changes

                            flight.FlightArrivalDays = GetDayDifference(tripDepartDate, flight.ArrivalDateTime);
                            bool flightDateChanged = false;
                            flight.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDepartDate, flight.ArrivalDateTime, ref flightDateChanged);
                            flight.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDepartDate, flight.DepartureDateTime, ref flightDateChanged);
                            flight.FlightDateChanged = flightDateChanged;

                            #endregion

                            if (_shoppingUtility.EnableIBELite() || _shoppingUtility.EnableIBEFull())
                            {
                                flattenedFlight.FlightHash = flight.FlightHash;
                            }
                            flattenedFlight.Flights.Add(flight);
                            // Added By Ali as part of Bug :213198 mAPP: Award - Connection cities are showing incorrect in RTI initial & Payment screen for COG flight
                            if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                            {
                                // Make a copy of flight.Connections and release flight.Connections
                                List<Model.Shopping.MOBSHOPFlight> connections = flight.StopInfos.Clone();
                                flight.StopInfos = null;

                                foreach (var connection in connections)
                                {
                                    connection.TripId = trip.TripId;
                                    connection.IsStopOver = true;
                                    connection.DepartureDateTimeGMT = isEnabledGMTConversionUsingCslData? await _shoppingUtility.GetGMTTimeFromOffset(connection.DepartureDateTime,connection.OriginTimezoneOffset) : await GetGMTTime(connection.DepartureDateTime, connection.Origin, sessionId);
                                    connection.ArrivalDateTimeGMT = isEnabledGMTConversionUsingCslData? await _shoppingUtility.GetGMTTimeFromOffset(connection.ArrivalDateTime,connection.DestinationTimezoneOffset):await GetGMTTime(connection.ArrivalDateTime, connection.Destination, sessionId);
                                    #region Red Eye Flight Changes

                                    connection.FlightArrivalDays = GetDayDifference(tripDepartDate, connection.ArrivalDateTime);
                                    connection.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDepartDate, connection.ArrivalDateTime, ref flightDateChanged);
                                    connection.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDepartDate, connection.DepartureDateTime, ref flightDateChanged);
                                    connection.FlightDateChanged = flightDateChanged;


                                    #endregion
                                    flattenedFlight.Flights.Add(connection);
                                }
                            }

                            if (flight.Connections != null && flight.Connections.Count > 0)
                            {
                                // Make a copy of flight.Connections and release flight.Connections
                                List<Model.Shopping.MOBSHOPFlight> connections = flight.Connections.Clone();
                                flight.Connections = null;

                                foreach (var connection in connections)
                                {
                                    connection.TripId = trip.TripId;

                                    connection.DepartureDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(connection.DepartureDateTime, connection.OriginTimezoneOffset) : await GetGMTTime(connection.DepartureDateTime, connection.Origin, sessionId);
                                    connection.ArrivalDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(connection.ArrivalDateTime, connection.DestinationTimezoneOffset) : await GetGMTTime(connection.ArrivalDateTime, connection.Destination, sessionId);

                                    #region Red Eye Flight Changes

                                    connection.FlightArrivalDays = GetDayDifference(tripDepartDate, connection.ArrivalDateTime);
                                    connection.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDepartDate, connection.ArrivalDateTime, ref flightDateChanged);
                                    connection.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDepartDate, connection.DepartureDateTime, ref flightDateChanged);
                                    connection.FlightDateChanged = flightDateChanged;


                                    #endregion

                                    flattenedFlight.Flights.Add(connection);
                                    if (connection.StopInfos != null && connection.StopInfos.Count > 0)
                                    {
                                        // Make a copy of flight.Connections and release flight.Connections
                                        List<Model.Shopping.MOBSHOPFlight> connStops = connection.StopInfos.Clone();
                                        connection.StopInfos = null;

                                        foreach (var conn in connStops)
                                        {
                                            conn.TripId = trip.TripId;
                                            conn.IsStopOver = true;
                                            conn.DepartureDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(conn.DepartureDateTime, conn.OriginTimezoneOffset) : await GetGMTTime(conn.DepartureDateTime, conn.Origin, sessionId);
                                            conn.ArrivalDateTimeGMT = isEnabledGMTConversionUsingCslData ? await _shoppingUtility.GetGMTTimeFromOffset(conn.ArrivalDateTime, conn.DestinationTimezoneOffset) : await GetGMTTime(conn.ArrivalDateTime, conn.Destination, sessionId);
                                            #region Red Eye Flight Changes

                                            conn.FlightArrivalDays = GetDayDifference(tripDepartDate, conn.ArrivalDateTime);
                                            conn.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDepartDate, conn.ArrivalDateTime, ref flightDateChanged);
                                            conn.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDepartDate, conn.DepartureDateTime, ref flightDateChanged);
                                            conn.FlightDateChanged = flightDateChanged;


                                            #endregion

                                            flattenedFlight.Flights.Add(conn);
                                        }
                                    }
                                }
                            }
                            // Commented by Ali and moved this code above as part of Bug :213198 mAPP: Award - Connection cities are showing incorrect in RTI initial & Payment screen for COG flight

                            //if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                            //{
                            //    // Make a copy of flight.Connections and release flight.Connections
                            //    List<MOBSHOPFlight> connections = flight.StopInfos.Clone();
                            //    flight.StopInfos = null;

                            //    foreach (var connection in connections)
                            //    {
                            //        connection.TripId = trip.TripId;
                            //        connection.IsStopOver = true;
                            //        connection.DepartureDateTimeGMT = GetGMTTime(connection.DepartureDateTime, connection.Origin);
                            //        connection.ArrivalDateTimeGMT = GetGMTTime(connection.ArrivalDateTime, connection.Destination);
                            //        #region Red Eye Flight Changes

                            //        connection.FlightArrivalDays = GetDayDifference(tripDepartDate, connection.ArrivalDateTime);
                            //        connection.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDepartDate, connection.ArrivalDateTime, ref flightDateChanged);
                            //        connection.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDepartDate, connection.DepartureDateTime, ref flightDateChanged);
                            //        connection.FlightDateChanged = flightDateChanged;

                            //        #endregion
                            //        flattenedFlight.Flights.Add(connection);
                            //    }
                            //}

                            if (flight.ShoppingProducts != null && flight.ShoppingProducts.Count > 0)
                            {
                                int idx = 0;
                                foreach (Model.Shopping.MOBSHOPShoppingProduct prod in flight.ShoppingProducts)
                                {
                                    if (_shoppingUtility.EnableIBELite() && !flattenedFlight.IsIBELite && prod.IsIBELite) // set only once
                                    {
                                        flattenedFlight.IsIBELite = true;
                                    }

                                    if (_shoppingUtility.EnableIBEFull() && !flattenedFlight.IsIBE && prod.IsIBE)
                                    {
                                        flattenedFlight.IsIBE = true;
                                    }

                                    if (prod.IsMixedCabin)
                                    {
                                        prod.MixedCabinSegmentMessages = GetFlightMixedCabinSegments(flattenedFlight.Flights, idx);
                                        prod.IsSelectedCabin = GetSelectedCabinInMixedScenario(flattenedFlight.Flights, idx);

                                        prod.ProductDetail.ProductCabinMessages = GetProductDetailMixedCabinSegments(flattenedFlight.Flights, idx);
                                        //break;
                                    }
                                    idx++;
                                }
                            }
                            if (_omniCart.IsEnableOmniCartMVP2Changes(appId, appVersion, true))
                            {
                                if (flattenedFlight?.Flights?.Any(f => !string.IsNullOrEmpty(f.OperatingCarrierDescription) && !f.OperatingCarrierDescription.Equals("United Airlines")) ?? false)
                                {
                                    flattenedFlight?.Flights?.Where(f => !string.IsNullOrEmpty(f.OperatingCarrierDescription) || !f.OperatingCarrierDescription.Equals("United Airlines"))?.Select(f => f?.OperatingCarrierDescription)?.ToList().Where(s => !string.IsNullOrEmpty(s))?.Distinct()?.ForEach(c => flattenedFlight.MsgFlightCarrier += c + ", ");

                                    if (!string.IsNullOrEmpty(flattenedFlight.MsgFlightCarrier))
                                    {
                                        if (flattenedFlight?.Flights?.Any(f => f.OperatingCarrier == "UA" && (string.IsNullOrEmpty(f.OperatingCarrierDescription) || f.OperatingCarrierDescription.Equals("United Airlines"))) ?? false)
                                        {
                                            flattenedFlight.MsgFlightCarrier = "Includes Travel Operated By " + flattenedFlight.MsgFlightCarrier.TrimEnd(',', ' ');
                                        }
                                        else
                                        {
                                            flattenedFlight.MsgFlightCarrier = "Operated by " + flattenedFlight.MsgFlightCarrier.TrimEnd(',', ' ');
                                            if (additionalItems != null && additionalItems?.AirlineCodes != null && additionalItems?.AirlineCodes.Count > 0)
                                            {
                                                var supportedAirlines = _configuration.GetValue<string>("SupportedAirlinesFareComparison").Split(',');
                                                foreach (var airline in supportedAirlines)
                                                {
                                                    if (additionalItems.AirlineCodes.Contains(airline))
                                                    {
                                                        flattenedFlight.CarrierMessage = flattenedFlight.MsgFlightCarrier.TrimEnd(',', ' ') + ". " + _configuration.GetValue<string>("CarrierMessage" + airline);
                                                        if (_configuration.GetValue<bool>("EnableUpdateDisclaimerOnFlightDetail"))
                                                        {
                                                            flattenedFlight.CarrierMessage = flattenedFlight.CarrierMessage.Replace(_configuration.GetValue<string>("RemoveCarrierMessageDisclaimerAirline" + airline), _configuration.GetValue<string>("CarrierMessageDisclaimer" + airline));
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            trip.FlattenedFlights.Add(flattenedFlight);
                        }
                    }
                    trip.UseFilters = false;
                    trip.SearchFiltersIn = null;
                    trip.SearchFiltersOut = null;
                }
                trips.Add(trip);
            }

            return trips;
        }

       
        private string GetDayDifference(String flightDepart, String flightArrive)
        {
            try
            {

                DateTime depart = DateTime.MinValue;
                DateTime arrive = DateTime.MinValue;

                DateTime.TryParse(flightDepart, out depart);
                DateTime.TryParse(flightArrive, out arrive);

                int days = (arrive.Date - depart.Date).Days;

                if (days == 0)
                {
                    return string.Empty;
                }
                else if (days > 0 && days < 2)
                {
                    return "+" + days.ToString() + " day";
                }
                else if (days > 0 && days > 1)
                {
                    return "+" + days.ToString() + " days";
                }
                else if (days < 0 && days > -2)
                {
                    return days.ToString() + " day";
                }
                else if (days < 0 && days < -1)
                {
                    return days.ToString() + " days";
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }
        private List<string> GetFlightMixedCabinSegments(List<Model.Shopping.MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            List<string> tempMsgs = new List<string>();
            foreach (var flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].MixedCabinSegmentMessages != null && flt.ShoppingProducts[index].MixedCabinSegmentMessages.Count > 0)
                        tempMsgs.AddRange(flt.ShoppingProducts[index].MixedCabinSegmentMessages);
                }
            }

            return tempMsgs;
        }

        private string GetRedEyeDepartureDate(String tripDate, String flightDepartureDate, ref bool flightDateChanged)
        {
            try
            {
                DateTime trip = DateTime.MinValue;
                DateTime departure = DateTime.MinValue;

                DateTime.TryParse(tripDate, out trip);
                DateTime.TryParse(flightDepartureDate, out departure);

                int days = (departure.Date - trip.Date).Days;

                if (days > 0)
                {
                    flightDateChanged = true; // Venkat - Showing Flight Date Change message is only for Departure date is different than Flight Search Date.
                    return departure.ToString("ddd. MMM dd"); // Wed. May 20                    
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }

        private string GetRedEyeFlightArrDate(String flightDepart, String flightArrive, ref bool flightDateChanged)
        {
            try
            {

                DateTime depart = DateTime.MinValue;
                DateTime arrive = DateTime.MinValue;

                DateTime.TryParse(flightDepart, out depart);
                DateTime.TryParse(flightArrive, out arrive);

                int days = (arrive.Date - depart.Date).Days;

                if (days == 0)
                {
                    return string.Empty;
                }
                else if (days > 0)
                {
                    return arrive.ToString("ddd. MMM dd"); // Wed. May 20
                }
                else
                {
                    if (_configuration.GetValue<bool>("EnableFlightDateChangeAlertFix"))
                    {
                        var daysText = "day";
                        if (days < -1)
                        {
                            daysText = $"{daysText}s";
                        }
                        return $"{days} {daysText} arrival";
                    }
                    return string.Empty;
                }
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }
        private async Task<string> GetGMTTime(string localTime, string airportCode, string sessionId)
        {
            var gmtTime = await _gMTConversionService.GETGMTTime(localTime, airportCode, sessionId).ConfigureAwait(false);
            if(!String.IsNullOrEmpty(gmtTime))
            {
                DateTime getDateTime;
                DateTime.TryParse(gmtTime, out getDateTime);
                return getDateTime.ToString("MM/dd/yyyy hh:mm tt");
            }
            return localTime;
        }
        private List<Model.Shopping.MOBSHOPPrice> AdjustTotal(List<Model.Shopping.MOBSHOPPrice> prices)
        {
            CultureInfo ci = null;

            List<Model.Shopping.MOBSHOPPrice> newPrices = prices;
            double fee = 0;
            foreach (Model.Shopping.MOBSHOPPrice p in newPrices)
            {
                if (ci == null)
                {
                    ci = TopHelper.GetCultureInfo(p.CurrencyCode);
                }

                if (fee == 0)
                {
                    foreach (Model.Shopping.MOBSHOPPrice q in newPrices)
                    {
                        if (q.DisplayType.Trim().ToUpper() == "RBF")
                        {
                            fee = q.Value;
                            break;
                        }
                    }
                }
                if (p.DisplayType.Trim().ToUpper() == "REFUNDPRICE" && p.Value < 0)
                {
                    p.Value *= -1;
                }
                if ((fee > 0 && p.DisplayType.Trim().ToUpper() == "TOTAL") || p.DisplayType.Trim().ToUpper() == "REFUNDPRICE")
                {
                    //update total
                    p.Value += fee;
                    p.DisplayValue = string.Format("{0:#,0.00}", p.Value);
                    p.FormattedDisplayValue = TopHelper.FormatAmountForDisplay(p.Value.ToString(), ci, false); ;// string.Format("{0:c}", price.Amount);
                }
            }
            return newPrices;
        }
        private List<Mobile.Model.Shopping.MOBSHOPPrice> RefundAmountTravelOption(TravelOptionsCollection traveloption)
        {
            var traveloptionprices = new List<Mobile.Model.Shopping.MOBSHOPPrice>();
            var traveloptionprice = new Mobile.Model.Shopping.MOBSHOPPrice();

            CultureInfo ci = null;
            var currencyType = string.Empty;
            decimal totalAmount = 0.0m;

            traveloption.ForEach(option =>
            {
                if (option.Status.Equals("REFUND", StringComparison.OrdinalIgnoreCase))
                {
                    if (ci == null)
                        ci = TopHelper.GetCultureInfo(option.Currency);

                    totalAmount = totalAmount + option.Amount;

                    double tempDouble = 0;
                    double.TryParse(Convert.ToString(option.Amount), out tempDouble);

                    traveloptionprice = new Mobile.Model.Shopping.MOBSHOPPrice
                    {
                        Value = Math.Round(tempDouble, 2, MidpointRounding.AwayFromZero),
                        CurrencyCode = option.Currency,
                        DisplayValue = string.Format("{0:#,0.00}", option.Amount),
                        FormattedDisplayValue = TopHelper.FormatAmountForDisplay(option.Amount, ci, false),
                        PriceTypeDescription = option.Description,
                        Status = "To original form of payment",
                        DisplayType = "TRAVELOPTIONREFUND"
                    };
                    traveloptionprices.Add(traveloptionprice);
                }
                currencyType = option.Currency;
            });


            if (traveloptionprices.Any())
            {
                double tempTotalDouble = 0;
                double.TryParse(Convert.ToString(totalAmount), out tempTotalDouble);

                traveloptionprice = new Mobile.Model.Shopping.MOBSHOPPrice
                {
                    Value = Math.Round(tempTotalDouble, 2, MidpointRounding.AwayFromZero),
                    CurrencyCode = currencyType,
                    DisplayValue = string.Format("{0:#,0.00}", totalAmount),
                    FormattedDisplayValue = TopHelper.FormatAmountForDisplay(totalAmount, ci, false),
                    PriceTypeDescription = "Total travel option refund",
                    Status = "To original form of payment",
                    DisplayType = "TOTALTRAVELOPTIONREFUND"
                };

                traveloptionprices.Add(traveloptionprice);
            }

            return traveloptionprices;
        }
        public double GeTotalFromPrices(List<United.Mobile.Model.Shopping.MOBSHOPPrice> Prices)
        {
            double totalamount = 0.0;
            if (Prices != null)
            {
                var total = (from s in Prices
                             where s.DisplayType.Trim().ToUpper() == "TOTAL"
                             select s.DisplayValue).ToList();
                if (total.Count > 0 && !string.IsNullOrEmpty(total[0]))
                {
                    Double.TryParse(total[0], out totalamount);
                }
            }
            return totalamount;
        }
        private double GetFarePrice(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices)
        {
            double bookingPrice = 0;
            foreach (United.Services.FlightShopping.Common.DisplayCart.DisplayPrice price in prices)
            {
                if (price.Type.ToUpper() == "TOTAL")
                {
                    bookingPrice = (double)price.Amount;
                    break;
                }
            }
            return bookingPrice;
        }
        private List<United.Mobile.Model.Common.MP2015.LmxFlight> PopulateLMX(string cartId, ShoppingResponse persistShop, string sessionId, int appId, string deviceId, string appVersion, bool showMileageDetails, List<United.Services.FlightShopping.Common.DisplayCart.DisplayTrip> flightSegmentCollection)
        {
            List<United.Mobile.Model.Common.MP2015.LmxFlight> lmxFlights = null;

            for (int i = 0; i < flightSegmentCollection.Count; i++)
            {
                if (flightSegmentCollection != null && flightSegmentCollection.Count > 0)
                {
                    if (flightSegmentCollection[i].Flights != null && flightSegmentCollection[i].Flights.Count > 0)
                    {
                        //update amenities for all flights
                        UpdateAmenitiesIndicatorsResponse amenitiesResponse = new UpdateAmenitiesIndicatorsResponse();
                        List<Flight> tempFlights = new List<Flight>(flightSegmentCollection[i].Flights);

                        //we do not want the search to fail if one of these fail...
                        try
                        {

                            Parallel.Invoke(async () =>
                            {
                                bool supressLMX = false;
                                #region //**// LMX Flag For AppID change
                                Session session = new Session();
                                session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);
                                supressLMX = session.SupressLMXForAppID;
                                #endregion
                                if (!supressLMX && showMileageDetails)
                                {
                                    lmxFlights = await GetLmxForRTI(session.Token, cartId, GetFlightHasListForLMX(tempFlights), sessionId, appId, deviceId, appVersion);
                                }

                            }
                                );
                        }
                        catch { };
                    }
                }
            }

            return lmxFlights;
        }
        private async Task<List<United.Mobile.Model.Common.MP2015.LmxFlight>> GetLmxForRTI(string token, string cartId, string hashList, string sessionId, int appId, string deviceId, string appVersion)
        {
            List<United.Mobile.Model.Common.MP2015.LmxFlight> lmxFlights = null;

            if (!string.IsNullOrEmpty(cartId))
            {

                string jsonRequest = "{\"CartId\":\"" + cartId + "\"}";

                if (!string.IsNullOrEmpty(hashList))
                {
                    jsonRequest = "{\"CartId\":\"" + cartId + "\", \"hashList\":[" + hashList + ")}";
                }

                FlightStatus flightStatus = new FlightStatus();



                #region//****Get Call Duration Code - Venkat 03/17/2015*******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

                var response = await _lmxInfo.GetLmxRTIInfo<LmxQuoteResponse>(token, jsonRequest, sessionId).ConfigureAwait(false);

                #region// 2 = cslStopWatch/***Get Call Duration Code - Venkat 03/17/2015*******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();

                //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetLmxForRTI - CSL Call url", "Trace", appId, appVersion, deviceId, url));
                //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetLmxForRTI - CSL Call Request", "Trace", appId, appVersion, deviceId, jsonRequest));
                //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetLmxForRTI - CSL Call Duration", "CSS/CSL-CallDuration", appId, appVersion, deviceId, "CSLGetLmxQuote=" + cslCallTime));
                //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetLmxForRTI - CSL Call Response", "Trace", appId, appVersion, deviceId, jsonResponse));

                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******


                if (response != null)
                {
                    if (response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success))
                    {
                        if (response.Flights != null && response.Flights.Count > 0)
                        {
                            if (lmxFlights == null)
                            {
                                lmxFlights = new List<United.Mobile.Model.Common.MP2015.LmxFlight>();
                            }
                            CultureInfo ci = null;
                            foreach (var flight in response.Flights)
                            {
                                United.Mobile.Model.Common.MP2015.LmxFlight lmxFlight = new United.Mobile.Model.Common.MP2015.LmxFlight();
                                //lmxFlight.Arrival = new MOBAirport();
                                //lmxFlight.Arrival.Code = flight.Destination;
                                //lmxFlight.Departure = new MOBAirport();
                                //lmxFlight.Departure.Code = flight.Origin;
                                //lmxFlight.FlightNumber = flight.FlightNumber;
                                //lmxFlight.MarketingCarrier = new MOBAirline();
                                lmxFlight.MarketingCarrier.Code = flight.MarketingCarrier;
                                lmxFlight.ScheduledDepartureDateTime = flight.DepartDateTime;


                                if (_configuration.GetValue<string>("LMXPartners").IndexOf(flight.MarketingCarrier) == -1)
                                {
                                    lmxFlight.NonPartnerFlight = true;
                                }

                                if (flight.Products != null && flight.Products.Count > 0)
                                {
                                    lmxFlight.Products = new List<United.Mobile.Model.Common.MP2015.LmxProduct>();
                                    foreach (var product in flight.Products)
                                    {
                                        United.Mobile.Model.Common.MP2015.LmxProduct lmxProduct = new United.Mobile.Model.Common.MP2015.LmxProduct();
                                        lmxProduct.ProductType = product.ProductType;
                                        if (product.LmxLoyaltyTiers != null && product.LmxLoyaltyTiers.Count > 0)
                                        {
                                            lmxProduct.LmxLoyaltyTiers = new List<United.Mobile.Model.Common.MP2015.LmxLoyaltyTier>();
                                            foreach (var loyaltyTier in product.LmxLoyaltyTiers)
                                            {
                                                if (string.IsNullOrEmpty(loyaltyTier.ErrorCode))
                                                {
                                                    United.Mobile.Model.Common.MP2015.LmxLoyaltyTier lmxLoyaltyTier = new United.Mobile.Model.Common.MP2015.LmxLoyaltyTier();
                                                    lmxLoyaltyTier.Description = loyaltyTier.Descr;
                                                    lmxLoyaltyTier.Key = loyaltyTier.Key;
                                                    lmxLoyaltyTier.Level = loyaltyTier.Level;
                                                    if (loyaltyTier.LmxQuotes != null && loyaltyTier.LmxQuotes.Count > 0)
                                                    {
                                                        lmxLoyaltyTier.LmxQuotes = new List<United.Mobile.Model.Common.MP2015.LmxQuote>();
                                                        foreach (var quote in loyaltyTier.LmxQuotes)
                                                        {
                                                            if (ci == null)
                                                                TopHelper.GetCultureInfo(quote.Currency);
                                                            United.Mobile.Model.Common.MP2015.LmxQuote lmxQuote = new United.Mobile.Model.Common.MP2015.LmxQuote();
                                                            lmxQuote.Amount = quote.Amount;
                                                            lmxQuote.Currency = quote.Currency;
                                                            lmxQuote.Description = quote.Descr;
                                                            lmxQuote.Type = quote.Type;
                                                            lmxQuote.DblAmount = Double.Parse(quote.Amount);
                                                            lmxQuote.Currency = _shoppingUtility.GetCurrencySymbol(ci);
                                                            lmxLoyaltyTier.LmxQuotes.Add(lmxQuote);
                                                        }
                                                    }
                                                    lmxProduct.LmxLoyaltyTiers.Add(lmxLoyaltyTier);
                                                }
                                            }
                                        }
                                        lmxFlight.Products.Add(lmxProduct);
                                    }
                                }

                                lmxFlights.Add(lmxFlight);
                            }


                            //logEntries.Add(LogEntry.GetLogEntry<List<MOBLmxFlight>>(sessionId, "GetLmxForRTI - ClientResponse for GetLmxQuote", "Trace", appId, appVersion, deviceId, lmxFlights));

                        }
                    }
                }
            }

            return lmxFlights;
        }
        private void PopulateLMX(List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights, ref List<Flight> flights)
        {
            if (lmxFlights != null && lmxFlights.Count > 0)
            {
                try
                {
                    for (int i = 0; i < flights.Count; i++)
                    {
                        Flight tempFlight = flights[i];
                        GetLMXForFlight(lmxFlights, ref tempFlight);
                        flights[i].Products = tempFlight.Products;

                        if (flights[i].Connections != null && flights[i].Connections.Count > 0)
                        {
                            List<Flight> tempFlights = flights[i].Connections;
                            PopulateLMX(lmxFlights, ref tempFlights);
                            flights[i].Connections = tempFlights;
                        }
                        if (flights[i].StopInfos != null && flights[i].StopInfos.Count > 0)
                        {
                            List<Flight> tempFlights = flights[i].StopInfos;
                            PopulateLMX(lmxFlights, ref tempFlights);
                            flights[i].StopInfos = tempFlights;
                        }
                    }
                }
                catch { }
            }
        }
        private void GetLMXForFlight(List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights, ref Flight flight)
        {
            foreach (United.Services.FlightShopping.Common.LMX.LmxFlight lmxFlight in lmxFlights)
            {
                if (flight.Hash == lmxFlight.Hash)
                {
                    //overwrite the products with new LMX versions
                    for (int i = 0; i < flight.Products.Count; i++)
                    {
                        Product tempProduct = flight.Products[i];
                        GetLMXForProduct(lmxFlight.Products, ref tempProduct);
                        flight.Products[i] = tempProduct;
                    }
                    return;
                }
            }
        }

        private void GetLMXForProduct(List<LmxProduct> productCollection, ref Product tempProduct)
        {
            foreach (LmxProduct p in productCollection)
            {
                if (p.ProductId == tempProduct.ProductId)
                {
                    tempProduct.LmxLoyaltyTiers = p.LmxLoyaltyTiers;
                }
            }
        }

        private string GetFlightHasListForLMX(List<Flight> flights)
        {
            List<string> flightNumbers = new List<string>();
            string flightHash = string.Empty;
            if (flights != null)
            {
                try
                {
                    foreach (Flight flight in flights)
                    {
                        if (!flightNumbers.Contains(flight.Hash))
                        {
                            flightNumbers.Add(flight.Hash);
                        }
                        if (flight.Connections != null && flight.Connections.Count > 0)
                        {
                            foreach (Flight connection in flight.Connections)
                            {
                                if (!flightNumbers.Contains(connection.Hash))
                                {
                                    flightNumbers.Add(connection.Hash);
                                }
                            }
                        }
                        if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                        {
                            foreach (Flight stop in flight.StopInfos)
                            {
                                if (!flightNumbers.Contains(stop.Hash))
                                {
                                    flightNumbers.Add(stop.Hash);
                                }
                            }
                        }
                    }

                    foreach (string str in flightNumbers)
                    {
                        if (flightHash == string.Empty)
                            flightHash += "\"" + str + "\"";
                        else
                            flightHash += "," + "\"" + str + "\"";
                    }
                }
                catch { }
            }
            return flightHash;
        }
        private bool IsCubaTravelTrip(United.Mobile.Model.Shopping.MOBSHOPReservation reservation)
        {
            bool isCubaFight = false;
            string CubaAirports = _configuration.GetValue<string>("CubaAirports");
            List<string> CubaAirportList = CubaAirports.Split('|').ToList();

            if (reservation != null && reservation.Trips != null)
            {
                foreach (MOBSHOPTrip trip in reservation.Trips)
                {
                    isCubaFight = isCubaAirportCodeExist(trip.Origin, trip.Destination, CubaAirportList);

                    if (isCubaFight)
                        break;

                    foreach (var flight in trip.FlattenedFlights)
                    {
                        foreach (var stopFlights in flight.Flights)
                        {
                            isCubaFight = isCubaAirportCodeExist(stopFlights.Origin, stopFlights.Destination, CubaAirportList);
                            if (!isCubaFight && stopFlights.StopInfos != null)
                            {
                                foreach (var stop in stopFlights.StopInfos)
                                {
                                    isCubaFight = isCubaAirportCodeExist(stop.Origin, stop.Destination, CubaAirportList);
                                }
                                if (isCubaFight)
                                    break;
                            }
                            if (isCubaFight)
                                break;
                        }

                        if (isCubaFight)
                            break;
                    }
                }
            }
            return isCubaFight;
        }
        private bool isCubaAirportCodeExist(string origin, string destination, List<string> CubaAirports)
        {
            bool isCubaFight = false;
            if (CubaAirports != null && (CubaAirports.Exists(p => p == origin) || CubaAirports.Exists(p => p == destination)))
            {
                isCubaFight = true;
            }
            return isCubaFight;
        }
        private async Task<string> GetAirportNameFromSavedList(string airportCode)
        {
            if (_configuration.GetValue<bool>("EnableFecthAirportNameFromCSL"))
            {
                return string.Empty;
            }

            string airportDesc = string.Empty;
            try
            {
                if (isGetAirportListInOneCallToggleOn())
                {
                    if (airportsList != null && airportsList.AirportsList != null && airportsList.AirportsList.Exists(p => p.AirportCode == airportCode))
                    {
                        var airPort = airportsList.AirportsList.First(p => p.AirportCode == airportCode);
                        airportDesc = airPort.AirportNameMobile;
                    }
                    else
                    {
                        var airportObj = await _shoppingUtility.GetAirportNamesList("'" + airportCode + "'");
                        if (airportObj != null && airportObj.Exists(p => p.AirportCode == airportCode))
                        {
                            if (airportsList == null)
                                airportsList = new AirportDetailsList();
                            if (airportsList.AirportsList == null)
                                airportsList.AirportsList = new List<MOBDisplayBagTrackAirportDetails>();

                            var airPort = airportObj.First(p => p.AirportCode == airportCode);
                            airportsList.AirportsList.Add(airPort);   //.Add(new Definition.Bag.MOBDisplayBagTrackAirportDetails() { AirportCode = airportCode, AirportCityName = airPort.AirportCityName });
                            airportDesc = airPort.AirportNameMobile;
                        }
                        else
                        {
                            airportDesc = await _shoppingUtility.GetAirportName(airportCode);
                        }
                    }
                }
                else
                {
                    airportDesc = await _shoppingUtility.GetAirportName(airportCode);
                }
            }
            catch (Exception ex)
            {
                airportDesc = await _shoppingUtility.GetAirportName(airportCode);
            }
            return airportDesc;
        }
        private bool isGetAirportListInOneCallToggleOn()
        {
            return _configuration.GetValue<bool>("GetAirportNameInOneCallToggle");
        }
        private async Task<AirportDetailsList> GetAllAiportsList(List<DisplayTrip> displayTrip)
        {
            string airPortCodes = GetAllAirportCodesWithCommaDelimatedFromCSLDisplayTrips(displayTrip);
            return await GetAirportNamesListCollection(airPortCodes);
        }
        private async Task<AirportDetailsList> GetAirportNamesListCollection(string airPortCodes)
        {
            AirportDetailsList retVal = null;
            if (airPortCodes != string.Empty)
            {
                airPortCodes = "'" + airPortCodes + "'";
                airPortCodes = String.Join(",", airPortCodes.Split(',').Distinct());
                airPortCodes = System.Text.RegularExpressions.Regex.Replace(airPortCodes, ",", "','");
                retVal = new AirportDetailsList();
                retVal.AirportsList = await _shoppingUtility.GetAirportNamesList(airPortCodes);
            }
            return retVal;
        }
        private string GetAllAirportCodesWithCommaDelimatedFromCSLDisplayTrips(List<DisplayTrip> displayTrip)
        {
            string airPortCodes = string.Empty;
            if (displayTrip != null && displayTrip.Count > 0)
            {
                airPortCodes = string.Join(",", displayTrip.Where(t => t != null).Select(t => t.Origin + "," +
                                                                                              t.Destination + "," +
                                                                                              GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(t.Flights))
                                          );
            }
            airPortCodes = System.Text.RegularExpressions.Regex.Replace(airPortCodes, ",{2,}", ",").Trim(',');
            airPortCodes = String.Join(",", airPortCodes.Split(',').Distinct());
            return airPortCodes;
        }

        private List<MOBSHOPShoppingProductDetailCabinMessage> GetProductDetailMixedCabinSegments(List<Model.Shopping.MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            List<Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage> tempMsgs = new List<Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage>();
            foreach (var flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages != null && flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages.Count > 0)
                        tempMsgs.AddRange(flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages);
                }
            }

            return tempMsgs;
        }
        private static string GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(List<Flight> flights)
        {
            string airPortCodes = string.Empty;
            if (flights != null && flights.Count > 0)
            {
                airPortCodes = string.Join(",", flights.Where(f => f != null).Select(flight => flight.Origin + "," +
                                                                                               flight.Destination + "," +
                                                                                               string.Join(",", flight.Connections.Where(c => c != null).Select(connection => connection.Origin + "," + connection.Destination)) + "," +
                                                                                               string.Join(",", flight.StopInfos.Where(s => s != null).Select(stop => stop.Origin + "," + stop.Destination))
                                                                                          )
                                                                    );
            }
            airPortCodes = System.Text.RegularExpressions.Regex.Replace(airPortCodes, ",{2,}", ",").Trim(',');
            airPortCodes = String.Join(",", airPortCodes.Split(',').Distinct());
            return airPortCodes;
        }

        private bool GetSelectedCabinInMixedScenario(List<Model.Shopping.MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            bool selected = false;
            foreach (var flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].IsSelectedCabin)
                        selected = flt.ShoppingProducts[index].IsSelectedCabin;
                }
            }

            return selected;
        }
        public Reservation AssignPnrTravelerToReservation(MOBSHOPShopRequest request, Reservation persistedReservation, Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation)
        {
            //var cslReservation = United.Persist.FilePersist.Load<Service.Presentation.ReservationResponseModel.ReservationDetail>(request.SessionId, (new Service.Presentation.ReservationResponseModel.ReservationDetail()).GetType().FullName);
            var mobCPPhones = MapTravelerModel.GetMobCpPhones(cslReservation.Detail.TelephoneNumbers);
            var mobEmails = MapTravelerModel.GetMobEmails(cslReservation.Detail.EmailAddress);

            if (cslReservation.Detail.Travelers != null && cslReservation.Detail.Travelers.Count > 0)
            {
                persistedReservation.TravelerKeys = new List<string>();
            }
            persistedReservation.TravelersCSL = ConvertPnrTravelerToMobShopTraveler(request, cslReservation.Detail.Travelers.ToList(), persistedReservation.TravelerKeys, cslReservation.Detail.FlightSegments, request.Trips, persistedReservation.IsPartiallyFlown);
            
            AssignMainTCDIfNullForAnyTravler(persistedReservation.TravelersCSL, mobCPPhones, mobEmails);
            //FilePersist.Save<Reservation>(request.SessionId, persistedReservation.ObjectName, persistedReservation);
            return persistedReservation;
        }

        private SerializableDictionary<string, MOBCPTraveler> ConvertPnrTravelerToMobShopTraveler(MOBSHOPShopRequest request, List<Service.Presentation.ReservationModel.Traveler> cslTravelers, List<string> travelerKeys, Collection<Service.Presentation.SegmentModel.ReservationFlightSegment> flightSegments, List<MOBSHOPTripBase> trips, bool isPartiallyFlown = false)
        {
            List<MOBPNRPassenger> reshopTravelers = request.ReshopTravelers;
            SerializableDictionary<string, MOBCPTraveler> travelerCsl = null;

            if (reshopTravelers != null && reshopTravelers.Count > 0)
            {
                travelerCsl = new SerializableDictionary<string, MOBCPTraveler>();
                List<string> travelersIdList = null;
                //if (!request.ReshopTravelerSHARESPositions.IsNullOrEmpty())
                //{
                //    travelersIdList = request.ReshopTravelerSHARESPositions;
                //}
                //else
                //{
                travelersIdList = reshopTravelers.Select(t => t.SHARESPosition).ToList();
                //}

                var pnrCslTravelers = cslTravelers.Where(p => travelersIdList.Contains(p.Person.Key)).ToList();
                if (travelerKeys == null)
                {
                    travelerKeys = new List<string>();
                }
                travelerCsl = BuildMOBCPTravelersDictionaryFromCslPersonsList(pnrCslTravelers, travelerKeys, flightSegments, trips, isPartiallyFlown);
            }

            return travelerCsl;
        }

        private void AssignMainTCDIfNullForAnyTravler(SerializableDictionary<string, MOBCPTraveler> travelersCSL, List<MOBCPPhone> mobCPPhones, List<MOBEmail> mobEmails)
        {
            foreach (var traveler in travelersCSL)
            {
                //if ((traveler.Value.Phones == null || traveler.Value.Phones.Count == 0) && mobCPPhones.Count > 0)
                //{
                //    traveler.Value.Phones = mobCPPhones;
                //}
                //if ((traveler.Value.ReservationPhones == null || traveler.Value.ReservationPhones.Count == 0) && mobCPPhones.Count > 0)
                //{
                //    traveler.Value.ReservationPhones = mobCPPhones;
                //}                
                if ((traveler.Value.EmailAddresses == null || traveler.Value.EmailAddresses.Count == 0) && mobEmails.Count > 0)
                {
                    traveler.Value.EmailAddresses = mobEmails;
                }
                if ((traveler.Value.ReservationEmailAddresses == null || traveler.Value.ReservationEmailAddresses.Count == 0) && mobEmails.Count > 0)
                {
                    traveler.Value.ReservationEmailAddresses = mobEmails;
                }
            }
        }

        private SerializableDictionary<string, MOBCPTraveler> BuildMOBCPTravelersDictionaryFromCslPersonsList(List<Service.Presentation.ReservationModel.Traveler> cslTravelers, List<string> travelerKeys, Collection<Service.Presentation.SegmentModel.ReservationFlightSegment> flightSegments, List<MOBSHOPTripBase> trips,bool isPartiallyFlown = false)
        {
            SerializableDictionary<string, MOBCPTraveler> travelerCsl = new SerializableDictionary<string, MOBCPTraveler>();
            int paxIndex = 0;
            cslTravelers.ForEach(p =>
            {
                MOBCPTraveler mobCPTraveler = MapTravelerModel.MapCslPersonToMOBCPTravel(p, paxIndex, flightSegments, trips, isPartiallyFlown, _logger);
                travelerCsl.Add(mobCPTraveler.PaxIndex.ToString(), mobCPTraveler);
                travelerKeys.Add(mobCPTraveler.PaxIndex.ToString());
                paxIndex++;
            });

            return travelerCsl;
        }
        public async Task<string> GetFareClassAtShoppingRequestFromPersist(string sessionID)
        {
            #region
            string fareClass = string.Empty;
            try
            {
                ShoppingResponse shop = new ShoppingResponse();
                shop = await _sessionHelperService.GetSession<ShoppingResponse>(sessionID, shop.ObjectName, new List<string> { sessionID, shop.ObjectName }).ConfigureAwait(false);
                fareClass = !string.IsNullOrEmpty(shop.Request.FareClass) ? shop.Request.FareClass : string.Empty;
            }
            catch { }
            return fareClass;
            #endregion
        }
        private string GetCslEndpointForShopping(bool isReshop)
        {
            string cslEndpoint = string.Empty;
            if (isReshop)
            {
                cslEndpoint = _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLReShoppingService");
            }
            else
            {
                cslEndpoint = _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLShopping");
            }
            return cslEndpoint;
        }
        private async Task DisplayBuyMiles(MOBSHOPReservation reservation, FlightReservationResponse response, Session session,
            SelectTripRequest selectTripRequest, string webShareToken)
        {
            List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(selectTripRequest, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
            if (response.IsMileagePurchaseRequired && response.IsPurchaseIneligible)
            {
                //Scenario 1
                // MOBILE-20327 mApp | Insufficient Mileage of 50% or more 
                //threshold does not meet 50% mileage balance criteria
                await AddActionsOnMileageBalanceNOTMeetsThreshhold(reservation, response, webShareToken, lstMessages);

            }
            else if (response.IsMileagePurchaseRequired)
            {
                // Scenario 2
                //MOBILE-20326	mApp | Insufficient Mileage under 50%
                AddActionsOnMileageBalanceMeetsThreshhold(reservation, response, lstMessages);
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

        private void AddActionsOnMileageBalanceMeetsThreshhold(MOBSHOPReservation reservation, FlightReservationResponse response, List<CMSContentMessage> lstMessages)
        {
            reservation.OnScreenAlert = new MOBOnScreenAlert();
            reservation.OnScreenAlert.Title = "United Airlines";
            string alerMessage = GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.MeetsThreshhold.AlertMessage");
            if (string.IsNullOrEmpty(alerMessage) == false)
                reservation.OnScreenAlert.Message = string.Format(alerMessage, response.DisplayCart?.ActualMileageRequired, response.DisplayCart?.DetectedUserBalance);
            else
                throw new MOBUnitedException(_configuration.GetValue<string>("NoEnoughMilesForAwardBooking"));
            reservation.OnScreenAlert.Actions = new List<MOBOnScreenActions>();
            reservation.OnScreenAlert.Actions.Add(new MOBOnScreenActions
            {
                ActionTitle = GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.MeetsThreshhold.AddMilesText"),
                ActionType = MOBOnScreenAlertActionType.NAVIGATE_TO_FARELOCK
            });
            reservation.OnScreenAlert.Actions.Add(new MOBOnScreenActions
            {
                ActionTitle = GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.MeetsThreshhold.SelectDifferentFlightText"),
                ActionType = MOBOnScreenAlertActionType.DISMISS_ALERT
            });
            if (reservation?.Prices?.Count() > 0)
            {
                reservation.Prices.Add(new MOBSHOPPrice
                {
                    DisplayType = "ADDITIONALMILES",
                    DisplayValue = response.DisplayCart?.ActualMileageRequired.ToString(),
                    FormattedDisplayValue = response.DisplayCart?.ActualMileageRequired.ToString(),
                    Value = Convert.ToDouble(response.DisplayCart?.ActualMileageRequired)
                });

            }
        }

        private async Task AddActionsOnMileageBalanceNOTMeetsThreshhold(MOBSHOPReservation reservation, FlightReservationResponse response, string webShareToken, List<CMSContentMessage> lstMessages)
        {
            reservation.OnScreenAlert = new MOBOnScreenAlert();
            reservation.OnScreenAlert.Title = _configuration.GetValue<string>("POSAlertMessageHeaderText");
            string alerMessage = GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.NOTMeetsThreshhold.AlertMessage");
            if (string.IsNullOrEmpty(alerMessage) == false)
                reservation.OnScreenAlert.Message = string.Format(alerMessage, response.DisplayCart?.ActualMileageRequired, response.DisplayCart?.DetectedUserBalance);
            else
                throw new MOBUnitedException(_configuration.GetValue<string>("NoEnoughMilesForAwardBooking"));
            reservation.OnScreenAlert.Actions = new List<MOBOnScreenActions>();

            if (await _featureSettings.GetFeatureSettingValue("EnableRedirectURLUpdate").ConfigureAwait(false))
            {
                reservation.OnScreenAlert.Actions.Add(new MOBOnScreenActions
                {
                    ActionTitle = GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.MeetsThreshhold.PurchaseMilesText"),
                    ActionType = MOBOnScreenAlertActionType.NAVIGATE_TO_EXTERNAL,
                    ActionURL = $"{_configuration.GetValue<string>("NewDotcomSSOUrl")}?type=sso&token={webShareToken}&landingUrl={_configuration.GetValue<string>("BuyMilesExternalMilegePlusURL")}",
                    WebShareToken = string.Empty,
                    WebSessionShareUrl = string.Empty
                });
            }
            else
            {
                reservation.OnScreenAlert.Actions.Add(new MOBOnScreenActions
                {
                    ActionTitle = GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.MeetsThreshhold.PurchaseMilesText"),
                    ActionType = MOBOnScreenAlertActionType.NAVIGATE_TO_EXTERNAL,
                    ActionURL = _configuration.GetValue<string>("BuyMilesExternalMilegePlusURL"),
                    WebShareToken = webShareToken,
                    WebSessionShareUrl = _configuration.GetValue<string>("DotcomSSOUrl")
                });
            }
            
            if (reservation?.FareLock?.FareLockProducts != null || reservation?.FareLock?.FareLockProducts?.Count > 0)
            {
                reservation.OnScreenAlert.Actions.Add(new MOBOnScreenActions
                {
                    ActionTitle = GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.MeetsThreshhold.SelectDifferentFlightText"),
                    ActionType = MOBOnScreenAlertActionType.NAVIGATE_TO_FARELOCK,

                });
            }
            reservation.OnScreenAlert.Actions.Add(new MOBOnScreenActions
            {
                ActionTitle = GetSDLStringMessageFromList(lstMessages, "SelectTrip.BuyMiles.MeetsThreshhold.SelectDifferentFlightText"),
                ActionType = MOBOnScreenAlertActionType.DISMISS_ALERT
            });
        }
        private string GetSDLStringMessageFromList(List<CMSContentMessage> list, string title)
        {
            return list?.Where(x => x.Title.Equals(title))?.FirstOrDefault()?.ContentFull?.Trim();
        }

        private CMSContentMessage GetSDLMessageFromList(List<CMSContentMessage> list, string title)
        {
            return list?.Where(x => x.Title.Equals(title))?.FirstOrDefault();
        }

        #endregion
        public async Task<Model.Shopping.ShopResponse> GetShopTripPlan(MOBSHOPTripPlanRequest request, HttpContext httpContext)
        {

            Model.Shopping.ShopResponse response = new Model.Shopping.ShopResponse();
            MOBTripPlanShopHelper tripPlanShopHelper = new MOBTripPlanShopHelper();
            //ForceUpdateForNonSupportedVersion(request.Application.Id, request.Application.Version.Major, FlowType.BOOKING);

            MOBSHOPShopRequest shopRequest = await GetTripPlanShopRequest(request);

            tripPlanShopHelper.TripPlanId = request.TripPlanId;
            tripPlanShopHelper.TripPlanCartId = request.CartId;
            tripPlanShopHelper.TripPlannerType = (MOBSHOPTripPlannerType)Enum.Parse(typeof(MOBSHOPTripPlannerType), request.TripPlannerType, true);
            tripPlanShopHelper.TPSessionId = request.SessionId;
            tripPlanShopHelper.MobShopRequest = shopRequest;
            if (shopRequest.TravelType == TravelType.TPBooking.ToString())
            {
                tripPlanShopHelper.IsTravelCountChanged = ISTravelCountChanged(shopRequest.TravelerTypes, request.TravelerTypes);
                if (tripPlanShopHelper.IsTravelCountChanged)
                    tripPlanShopHelper.MobShopRequest.TravelerTypes = request.TravelerTypes;
            }

            //****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cssCallStopWatch;

            //****Get Call Duration Code - Venkat 03/17/2015*******
            Session session = null;
            if (request.TravelType == TravelType.TPEdit.ToString()
                && !string.IsNullOrEmpty(request.SessionId))
            {
                session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId);
                session.TravelType = request.TravelType;
                session.MileagPlusNumber = request.MileagePlusAccountNumber;
                session.ShopSearchTripCount = shopRequest.Trips.Count;
            }
            else
            {
                session = await _shoppingSessionHelper.CreateShoppingSession(request.Application.Id, request.DeviceId,
                            request.Application.Version.Major, request.TransactionId, request.MileagePlusAccountNumber,
                             null, shopRequest.IsELFFareDisplayAtFSR,
                            false, false, shopRequest.TravelType);
            }
            if ((request.CatalogItems?.Count??0) > 0)
            {
                session.CatalogItems = request.CatalogItems;
            }
            await _sessionHelperService.SaveSession<Session>(session, request.SessionId, new List<string> { request.SessionId, session.ObjectName }, session.ObjectName);

            session = await _shoppingUtility.ValidateFSRRedesign(shopRequest, session);


            if (string.IsNullOrEmpty(shopRequest.SessionId))
            {
                shopRequest.SessionId = session.SessionId;
            }

            ShoppingResponse shop = new ShoppingResponse();
            shop.Request = shopRequest;
            _headers.ContextValues.SessionId = session.SessionId;
            await _sessionHelperService.SaveSession<ShoppingResponse>(shop, session.SessionId, new List<string> { session.SessionId, shop.ObjectName }, shop.ObjectName);

            response.ShopRequest = shopRequest;

            //****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch getAvailabilityStopWatch;
            getAvailabilityStopWatch = new Stopwatch();
            getAvailabilityStopWatch.Reset();
            getAvailabilityStopWatch.Start();
            //****Get Call Duration Code - Venkat 03/17/2015*******
            //****Get Call Duration Code - Venkat 03/17/2015*******

            response.Availability = await _shopBooking.GetAvailabilityTripPlan(session.Token, tripPlanShopHelper, request, httpContext);

            await FireAndForgetUpdateTripPlanPaxInfo(tripPlanShopHelper);
            //****Get Call Duration Code - Venkat 03/17/2015*******
            if (getAvailabilityStopWatch.IsRunning)
            {
                getAvailabilityStopWatch.Stop();
            }
            //cssCallDuration = cssCallDuration + "|66=" + getAvailabilityStopWatch.ElapsedMilliseconds.ToString() + "|"; // 66= getAvailabilityStopWatch
            //****Get Call Duration Code - Venkat 03/17/2015*******

            //****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch afterGetAvailabilityStopWatch;
            afterGetAvailabilityStopWatch = new Stopwatch();
            afterGetAvailabilityStopWatch.Reset();
            afterGetAvailabilityStopWatch.Start();
            //****Get Call Duration Code - Venkat 03/17/2015*******

            response.Disclaimer = GetDisclaimerString();

            response.RefreshResultsData = string.Format("{0},{1},{2}", shop.Request.Trips[0].DepartDate, "", "");

            shop = await _sessionHelperService.GetSession<ShoppingResponse>(session.SessionId, (new ShoppingResponse()).ObjectName, new List<string> { session.SessionId, (new ShoppingResponse()).ObjectName }).ConfigureAwait(false);
            shop.Response = response;

            if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "YES")
                response.CartId = response.Availability.CartId;

            await _sessionHelperService.SaveSession<ShoppingResponse>(shop, session.SessionId, new List<string> { session.SessionId, shop.ObjectName }, shop.ObjectName);

            if (response.Availability != null && response.Availability.Trip != null && _configuration.GetValue<bool>("hidesearchfiletersandsort"))
            {
                if (response.Availability.Trip.SearchFiltersIn != null) { response.Availability.Trip.SearchFiltersIn = null; }
                if (response.Availability.Trip.SearchFiltersOut != null) { response.Availability.Trip.SearchFiltersOut = null; }
            }

            if (response.Availability != null && response.Availability.Trip != null && response.Availability.Trip.FlattenedFlights != null)
            {
                //response.Availability.Trip.SearchFiltersIn = null;
                //response.Availability.Trip.SearchFiltersOut = null;

                if (!_configuration.GetValue<bool>("EnableNonStopFlight") || response.Availability.Trip.PageCount == 2) //**PageCount==2==>> for paging implementation to send only 15 flights back to client and perisit remaining flights and return remaining when customer page up
                {

                    response.Availability.Trip.FlightSections = null;
                    // Implemented the Paging to return only the number scecified at    <add key="OrgarnizeResultsRequestPageSize" value="5" />
                    List<MOBSHOPFlattenedFlight> flattenedFlights = response.Availability.Trip.FlattenedFlights.Clone();
                    int organizeResultsPageSize = Convert.ToInt32(_configuration.GetValue<string>("OrgarnizeResultsRequestPageSize"));
                    if (flattenedFlights.Count > organizeResultsPageSize)
                    {
                        response.Availability.Trip.FlattenedFlights = flattenedFlights.GetRange(0, organizeResultsPageSize);
                    }
                    response.Availability.Trip.FlightCount = flattenedFlights.Count;
                    response.Availability.Trip.TotalFlightCount = flattenedFlights.Count;
                }

            }
            else
            {
                if (response.Availability == null)
                    throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError"));
            }

            //****Get Call Duration Code - Venkat 03/17/2015*******
            if (afterGetAvailabilityStopWatch.IsRunning)
            {
                afterGetAvailabilityStopWatch.Stop();
            }
            return await Task.FromResult(response);
            //cssCallDuration = cssCallDuration + "|6=" + afterGetAvailabilityStopWatch.ElapsedMilliseconds.ToString() + "|"; // 6= afterGetAvailabilityStopWatch
        }
        public async Task<MOBSHOPShopRequest> GetTripPlanShopRequest(MOBSHOPTripPlanRequest shopTripPlanRequest)
        {
            MOBSHOPShopRequest request = new MOBSHOPShopRequest();
            try
            {
                //var shopRequest = FilePersist.Load<CSLShopRequest>(shopTripPlanRequest.CartId, new CSLShopRequest().ObjectName).ShopRequest;
                var shopRequest = new ShopRequest();

                TripPlanCCEResponse tpCCEResponse = await _sessionHelperService.GetSession<TripPlanCCEResponse>(shopTripPlanRequest.SessionId, new TripPlanCCEResponse().ObjectName, new List<string> { shopTripPlanRequest.SessionId, new TripPlanCCEResponse().ObjectName }).ConfigureAwait(false);
                if (shopTripPlanRequest.TravelType == TravelType.TPEdit.ToString())
                {
                    shopRequest = tpCCEResponse.TripPlanTrips.First(t => t.CslShopRequest?.Trips != null).CslShopRequest;
                }
                else
                {
                    shopRequest = tpCCEResponse.TripPlanTrips.First(t => t.CartID.Equals(shopTripPlanRequest.CartId, StringComparison.OrdinalIgnoreCase)).CslShopRequest;
                }
                //shopRequest.TravelPlanId = shopTripPlanRequest.TripPlanId;
                //shopRequest.TravelPlanCartId = shopTripPlanRequest.CartId;

                shopRequest.LoyaltyId = shopTripPlanRequest.MileagePlusAccountNumber;
                shopRequest.EliteLevel = shopTripPlanRequest.PremierStatusLevel;

                request = GetShopRequest(shopRequest);
                request.Application = shopTripPlanRequest.Application;
                request.DeviceId = shopTripPlanRequest.DeviceId;
                request.LanguageCode = shopTripPlanRequest.LanguageCode;
                request.TransactionId = shopTripPlanRequest.TransactionId;
                request.AccessCode = shopTripPlanRequest.AccessCode;
                request.CountryCode = shopTripPlanRequest.CountryCode;
                request.TravelType = shopTripPlanRequest.TravelType ?? TravelType.TPBooking.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError("GetTripPlanShopRequest Exception {@Exception}", JsonConvert.SerializeObject(ex));
                throw new Exception(ex.Message);
            }

            return request;
        }
        private MOBSHOPShopRequest GetShopRequest(ShopRequest shopRequest)
        {
            MOBSHOPShopRequest MOBShopShopRequest = new MOBSHOPShopRequest();
            MOBShopShopRequest.Experiments = new List<string>() { "NoChangeFee", "FSRRedesignA" };
            MOBShopShopRequest.MileagePlusAccountNumber = shopRequest.LoyaltyId;
            MOBShopShopRequest.GetFlightsWithStops = false;
            MOBShopShopRequest.GetNonStopFlightsOnly = true;

            //if (Utility.GetBooleanConfigValue("EnableFSRBasicEconomyToggleOnBookingMain") /*Master toggle to hide the be column */
            //    && Mobile.DAL.Utility.isApplicationVersionGreaterorEqual(MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, "FSRBasicEconomyToggleOnBookingMainAndroidversion", "FSRBasicEconomyToggleOnBookingMainiOSversion") /*Version check for latest client changes which hardcoded IsELFFareDisplayAtFSR to true at Shop By Map*/
            //    && Utility.CheckFSRRedesignFromShopRequest(MOBShopShopRequest)/*check for FSR resdesign experiment ON Builds*/ )
            //{
            MOBShopShopRequest.IsELFFareDisplayAtFSR = !shopRequest.DisableMostRestrictive;
            //}

            //MOBShopShopRequest.SessionId= shopRequest.SessionId;
            MOBShopShopRequest.CountryCode = shopRequest.CountryCode;

            MOBShopShopRequest.PromotionCode = shopRequest.PromoCode;
            MOBShopShopRequest.FareClass = shopRequest.BookingCodesSpecified;
            MOBShopShopRequest.Trips = new List<MOBSHOPTripBase>();
            shopRequest.Trips.ForEach(t => MOBShopShopRequest.Trips.Add(
                new MOBSHOPTripBase()
                {
                    Origin = t.Origin,
                    Destination = t.Destination,
                    DepartDate = t.DepartDate,
                    OriginAllAirports = (t.OriginAllAirports) ? 1 : 0,
                    DestinationAllAirports = (t.DestinationAllAirports) ? 1 : 0,
                    UseFilters = t.UseFilters,
                    SearchNearbyDestinationAirports = t.SearchRadiusMilesDestination > 0,
                    SearchNearbyOriginAirports = t.SearchRadiusMilesOrigin > 0,
                    Cabin = GetCabinType(t.CabinType)
                }));
            MOBShopShopRequest.FareType = GetShopFareType(shopRequest.Trips[0].SearchFiltersIn);
            MOBShopShopRequest.TravelerTypes = new List<MOBTravelerType>();
            GetTravelersListFromCSLShop(shopRequest, MOBShopShopRequest.TravelerTypes);

            MOBShopShopRequest.SearchType = GetSearchTypeSelection(shopRequest.SearchTypeSelection);
            MOBShopShopRequest.MaxNumberOfStops = shopRequest.Stops;
            MOBShopShopRequest.PremierStatusLevel = shopRequest.EliteLevel;
            MOBShopShopRequest.ResultSortType = shopRequest.SortType;
            //MOBShopShopRequest.TravelType = MOBTripPlannerType.TPBooking.ToString();

            ////persis CSL shop request so we nave Loyalty info without making multiple summary calls
            //United.Persist.Definition.Shopping.CSLShopRequest cslShopRequest = new CSLShopRequest();

            //cslShopRequest.ShopRequest = shopRequest;
            //United.Persist.FilePersist.Save<Persist.Definition.Shopping.CSLShopRequest>(MOBShopShopRequest.SessionId, cslShopRequest.ObjectName, cslShopRequest);

            return MOBShopShopRequest;
        }

        private string GetCabinType(CabinType cabin)
        {
            string cabinType = "";

            switch (cabin)
            {
                case CabinType.Coach: cabinType = "ECON"; break;
                case CabinType.First: cabinType = "FIRST"; break;
                case CabinType.Business: cabinType = "BUSINESS"; break;
                case CabinType.BusinessFirst: cabinType = "BUSINESSFIRST"; break;

                default: cabinType = "ECON"; break;
            }

            return cabinType;
        }

        private string GetShopFareType(SearchFilterInfo searchFiltersIn)
        {
            string f = "lf";
            if (searchFiltersIn?.FareFamily?.Contains('-') ?? false)
            {
                string fareType = searchFiltersIn.FareFamily.Split('-')[1];
                if (!string.IsNullOrEmpty(fareType))
                {
                    switch (fareType.ToUpper())
                    {
                        case "FLEXIBLE": f = "ff"; break;
                        case "UNRESTRICTED": f = "urf"; break;
                        default: f = "lf"; break;
                    }
                }
            }

            return f;
        }
        private static void GetTravelersListFromCSLShop(ShopRequest persistCSLShopRequest, List<MOBTravelerType> mOBTravelerTypes)
        {
            foreach (var t in persistCSLShopRequest.PaxInfoList.GroupBy(p => p.PaxType))
            {
                switch ((int)t.Key)
                {
                    case (int)PaxType.Adult:
                        mOBTravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Adult.ToString() });
                        break;

                    case (int)PaxType.Senior:
                        mOBTravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Senior.ToString() });
                        break;

                    case (int)PaxType.Child01:
                        mOBTravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child2To4.ToString() });
                        break;

                    case (int)PaxType.Child02:
                        mOBTravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child5To11.ToString() });
                        break;

                    case (int)PaxType.Child03:
                        mOBTravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child12To17.ToString() });
                        break;

                    case (int)PaxType.Child04:
                        mOBTravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child12To14.ToString() });
                        break;

                    case (int)PaxType.Child05:
                        mOBTravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Child15To17.ToString() });
                        break;

                    case (int)PaxType.InfantSeat:
                        mOBTravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.InfantSeat.ToString() });
                        break;

                    case (int)PaxType.InfantLap:
                        mOBTravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.InfantLap.ToString() });
                        break;
                    default:
                        mOBTravelerTypes.Add(new MOBTravelerType() { Count = t.Count(), TravelerType = PAXTYPE.Adult.ToString() });
                        break;
                }
            }
        }


        private string GetSearchTypeSelection(SearchType searchTypeSelection)
        {
            string searchType = "OW";
            switch (searchTypeSelection)
            {
                case SearchType.OneWay: searchType = "OW"; break;
                case SearchType.RoundTrip: searchType = "RT"; break;
                case SearchType.MultipleDestination: searchType = "MD"; break;
                default: searchType = ""; break;
            }
            return searchType;
        }

        private bool ISTravelCountChanged(List<MOBTravelerType> travelerTypes, List<MOBTravelerType> tripPlanTravelerTypes)
        {
            return (travelerTypes.Where(t => t.Count > 0).ToList().Count != tripPlanTravelerTypes.Where(t => t.Count > 0).ToList().Count) ||
                tripPlanTravelerTypes.Any(tt => tt.Count != (travelerTypes.FirstOrDefault(t => t.TravelerType.Equals(tt.TravelerType))?.Count ?? 0));

        }
        private async System.Threading.Tasks.Task FireAndForgetUpdateTripPlanPaxInfo(MOBTripPlanShopHelper shopRequest)
        {
            if (shopRequest.TripPlannerType == MOBSHOPTripPlannerType.Pilot && shopRequest.IsTravelCountChanged)
            {
                await Task.Factory.StartNew(async () => await UpdateTripPlanWithNewPaxInfo(shopRequest));
            }

        }
        private async System.Threading.Tasks.Task UpdateTripPlanWithNewPaxInfo(MOBTripPlanShopHelper shopRequest)
        {
            try
            {
                await GetUpdatedTripPlanID(
                                         new United.Mobile.Model.TripPlannerGetService.MOBTripPlanSummaryRequest()
                                         {
                                             SessionId = shopRequest.TPSessionId,
                                             IsTravelCountChanged = true,
                                             MpNumber = shopRequest.MobShopRequest.MileagePlusAccountNumber,
                                             Application = shopRequest.MobShopRequest.Application,
                                             DeviceId = shopRequest.MobShopRequest.DeviceId,
                                             TransactionId = shopRequest.MobShopRequest.TransactionId,
                                             AccessCode = shopRequest.MobShopRequest.AccessCode,
                                             LanguageCode = shopRequest.MobShopRequest.LanguageCode,
                                             TravelerTypes = shopRequest.MobShopRequest.TravelerTypes
                                         }
                                     );
            }
            catch { }
        }
        private async Task<string> GetUpdatedTripPlanID(United.Mobile.Model.TripPlannerGetService.MOBTripPlanSummaryRequest request)
        {
            //return Guid.NewGuid().ToString();
            Session session = await _sessionHelperService.GetSession<Session>(request.SessionId, new Session().ObjectName, new List<string> { request.SessionId, new Session().ObjectName }).ConfigureAwait(false);
            TripPlanCCEResponse tpCCEResponse = await _sessionHelperService.GetSession<TripPlanCCEResponse>(request.SessionId, new TripPlanCCEResponse().ObjectName, new List<string> { request.SessionId, new TripPlanCCEResponse().ObjectName }).ConfigureAwait(false);

            if (request.TravelType == TravelType.TPEdit.ToString())
            {
                if (session.ShopSearchTripCount > 1)
                {
                    foreach (var st in request.ListTripPlanSelectTrip)
                    {
                        //Remove when CCE ready
                        //TripPlanTrip 
                        //var cslSelectTrip = FilePersist.Load<CSLSelectTrip>(st.CartId, new CSLSelectTrip().ObjectName);
                        var cslSelectTrip = await _sessionHelperService.GetSession<CSLSelectTrip>(st.CartId, new CSLSelectTrip().ObjectName, new List<string> { st.CartId, new CSLSelectTrip().ObjectName }).ConfigureAwait(false);

                        var persistCSLShopRequest = cslSelectTrip.ShopCSLRequest;
                        var persistCSLShopResponse = cslSelectTrip.ShopCSLResponse;

                        persistCSLShopRequest.Trips[persistCSLShopResponse.LastTripIndexRequested - 1].Flights.Add(persistCSLShopResponse.Trips[persistCSLShopResponse.LastTripIndexRequested - 1].Flights.First(f => f.BBXSolutionSetId == st.TripId &&
                        f.Products.Any(p => p.ProductId == st.ProductId)));

                        persistCSLShopRequest.Trips[persistCSLShopResponse.LastTripIndexRequested - 1].ColumnInformation = persistCSLShopResponse.Trips[persistCSLShopResponse.LastTripIndexRequested - 1].ColumnInformation;
                        persistCSLShopRequest.CartId = st.CartId;

                        persistCSLShopRequest.FlexibleDaysAfter = 0;
                        persistCSLShopRequest.FlexibleDaysBefore = 0;

                        tpCCEResponse.TripPlanTrips.First(t => t.CartID.Equals(request.deleteTripCartId, StringComparison.OrdinalIgnoreCase)).CslShopRequest = persistCSLShopRequest;
                        tpCCEResponse.TripPlanTrips.First(t => t.CartID.Equals(request.deleteTripCartId, StringComparison.OrdinalIgnoreCase)).CartID = st.CartId;
                    }

                }
                else
                {
                    //var persistCSLShopResponse = United.Persist.FilePersist.Load<CSLShopResponse>(request.SessionId, new CSLShopResponse().ObjectName).ShopCSLResponse;
                    var persistCSLShopResponse = _sessionHelperService.GetSession<CSLShopResponse>(request.SessionId, new CSLShopResponse().ObjectName, new List<string> { request.SessionId, new CSLShopResponse().ObjectName }).Result.ShopCSLResponse;

                    //var persistCSLShopRequest = United.Persist.FilePersist.Load<CSLShopRequest>(request.SessionId, new CSLShopRequest().ObjectName)?.ShopRequest;
                    var persistCSLShopRequest = _sessionHelperService.GetSession<Model.TripPlannerGetService.CSLShopRequest>(request.SessionId, new Model.TripPlannerGetService.CSLShopRequest().ObjectName, new List<string> { request.SessionId, new CSLShopRequest().ObjectName }).Result.ShopRequest;


                    persistCSLShopRequest.Trips[0].ColumnInformation = persistCSLShopResponse.Trips[0].ColumnInformation;

                    foreach (var st in request.ListTripPlanSelectTrip)
                    {
                        var _cslShopRequest = persistCSLShopRequest.Clone();
                        _cslShopRequest.Trips[0].Flights.Add(persistCSLShopResponse.Trips[0].Flights.First(f => f.BBXSolutionSetId == st.TripId &&
                    f.Products.Any(p => p.ProductId == st.ProductId)));

                        _cslShopRequest.CartId = st.CartId;

                        _cslShopRequest.FlexibleDaysAfter = 0;
                        _cslShopRequest.FlexibleDaysBefore = 0;

                        tpCCEResponse.TripPlanTrips.First(t => t.CartID.Equals(request.deleteTripCartId, StringComparison.OrdinalIgnoreCase)).CslShopRequest = _cslShopRequest;
                        tpCCEResponse.TripPlanTrips.First(t => t.CartID.Equals(request.deleteTripCartId, StringComparison.OrdinalIgnoreCase)).CartID = st.CartId;
                    }
                }

            }
            Collection<FlightOption> flightOptions = new Collection<FlightOption>();
            int i = 0;

            tpCCEResponse.TripPlanTrips.ForEach(t =>
            {
                if (request.IsTravelCountChanged)
                {
                    if (t.CslShopRequest != null)
                    {
                        t.CslShopRequest.PaxInfoList = new List<PaxInfo>();
                        GetPaxInfo(new MOBSHOPShopRequest() { TravelerTypes = request.TravelerTypes }, t.CslShopRequest);
                    }
                }
                if (request.TravelType == TravelType.TPTripDelete.ToString())
                {
                    if (t.CartID.Equals(request.deleteTripCartId, StringComparison.OrdinalIgnoreCase))
                    {
                        t.CslShopRequest = null;
                    }
                }
                flightOptions.Add(
                        new FlightOption()
                        {
                            ShoppingCartID = t.CslShopRequest?.TravelPlanCartId ?? t.CartID,
                            ShopRequest = t.CslShopRequest,
                            ID = ++i
                        }
                    );
            });
            TravelPlan updateTripPlanRequest = new TravelPlan()
            {
                CreatorDeviceID = tpCCEResponse.TripPlannerCreatorDeviceID,
                CreatorMpID = tpCCEResponse.TripPlannerCreatorMPNumber,
                TravelPlanID = tpCCEResponse.TripPlanID,
                LastUpdated = GetLastUpdatedTime(tpCCEResponse.TripLastUpdate),
                FlightOptions = flightOptions
            };

            var jsonResponse = await UpdateTripPlan(updateTripPlanRequest, session, request);
            //var jsonResponse = PostAndLog(session.SessionId, url, jsonRequest, mobReq, "GetCovidLiteInfo", getCovidLiteActionName, tapServiceToken);

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                // delete when CCE ready
                if (!request.IsTravelCountChanged)
                {
                    //FilePersist.Save<TripPlanCCEResponse>(request.SessionId, new TripPlanCCEResponse().ObjectName, tpCCEResponse);
                    await _sessionHelperService.SaveSession<TripPlanCCEResponse>(tpCCEResponse, request.SessionId, new List<string>() { request.SessionId, new TripPlanCCEResponse().ObjectName }, new TripPlanCCEResponse().ObjectName).ConfigureAwait(false);

                }
                if (!_configuration.GetValue<bool>("DisableTripPlanCreationCharDeserializeFix"))
                {
                    return jsonResponse.Trim();
                }
                else
                {
                    //TO DO
                    return JsonConvert.DeserializeObject<string>(jsonResponse);
                }

            }
            else
            {
                throw new Exception("Update tripplan failed");
            }
        }
        public void GetPaxInfo(MOBSHOPShopRequest MOBShopShopRequest, ShopRequest shopRequest)
        {
            PaxInfo paxInfo = null;
            foreach (MOBTravelerType t in MOBShopShopRequest.TravelerTypes.Where(t => t.TravelerType != null && t.Count > 0))
            {
                int tType = (int)Enum.Parse(typeof(PAXTYPE), t.TravelerType);

                switch (tType)
                {
                    case (int)PAXTYPE.Adult:
                    default:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo();
                            paxInfo.PaxType = PaxType.Adult;
                            paxInfo.DateOfBirth = DateTime.Today.AddYears(-25).ToShortDateString();
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Senior:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo();
                            paxInfo.PaxType = PaxType.Senior;
                            paxInfo.DateOfBirth = DateTime.Today.AddYears(-67).ToShortDateString();
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child2To4:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo();
                            paxInfo.PaxType = PaxType.Child01;
                            paxInfo.DateOfBirth = DateTime.Today.AddYears(-3).ToShortDateString();
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child5To11:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo();
                            paxInfo.PaxType = PaxType.Child02;
                            paxInfo.DateOfBirth = DateTime.Today.AddYears(-8).ToShortDateString();
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child12To17:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo();
                            paxInfo.PaxType = PaxType.Child03;
                            paxInfo.DateOfBirth = DateTime.Today.AddYears(-15).ToShortDateString();
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child12To14:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo();
                            paxInfo.PaxType = PaxType.Child04;
                            paxInfo.DateOfBirth = DateTime.Today.AddYears(-13).ToShortDateString();
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child15To17:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo();
                            paxInfo.PaxType = PaxType.Child05;
                            paxInfo.DateOfBirth = DateTime.Today.AddYears(-16).ToShortDateString();
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.InfantSeat:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo();
                            paxInfo.PaxType = PaxType.InfantSeat;
                            paxInfo.DateOfBirth = DateTime.Today.AddYears(-1).ToShortDateString();
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.InfantLap:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo();
                            paxInfo.PaxType = PaxType.InfantLap;
                            paxInfo.DateOfBirth = DateTime.Today.AddYears(-1).ToShortDateString();
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;
                }
            }
        }
        private void AssignPTCValue(MOBSHOPShopRequest MOBShopShopRequest, PaxInfo paxInfo)
        {
            bool isAwardCalendarMP2017 = _configuration.GetValue<bool>("NGRPAwardCalendarMP2017Switch");
            if (MOBShopShopRequest.AwardTravel)
            {
                if (isAwardCalendarMP2017 && MOBShopShopRequest.CustomerMetrics != null) //No null Check for PTCCode because we can pass empty string and CSL defaults PTCCode to PPR
                {
                    string ptcCode = string.Empty;
                    paxInfo.PtcList = new List<string>();
                    ptcCode = MOBShopShopRequest.CustomerMetrics.PTCCode ?? string.Empty;

                    paxInfo.PtcList.Add(ptcCode);
                }
            }
        }
        private async Task<string> UpdateTripPlan(TravelPlan tripPlanRequest, Session session, MOBRequest request)
        {
            //var url = $"{_tripPlanServiceBaseUrl}/TravelPlanner/{_postUpdateTripPlanActionName}";
            var url = "/TravelPlanner/UpdateTravelPlan";
            string jsonRequest = JsonConvert.SerializeObject(tripPlanRequest);
            //var jsonResponse = new Utility().MakeHTTPPost(session.SessionId, request.DeviceId, "UpdateTripPlan", request.Application, session.Token, url, jsonRequest, logEntries);
            var jsonResponse = await _tripPlannerIDService.GetTripPlanID<string>(session.Token, session.SessionId, url, jsonRequest).ConfigureAwait(false);
            if (jsonResponse != null)
            {
                return jsonResponse;
            }
            else
            {
                throw new Exception("Update tripplan failed");
            }

        }
        private DateTime GetLastUpdatedTime(string tripLastUpdate)
        {
            DateTime lastUpdateDate;
            if (!(DateTime.TryParse(tripLastUpdate, out lastUpdateDate)))
            {
                lastUpdateDate = DateTime.Now;
            }
            return lastUpdateDate;
        }

        public async Task<MOBCarbonEmissionsResponse> GetCarbonEmissionDetails(MOBCarbonEmissionsRequest request)
        {
            MOBCarbonEmissionsResponse response = null;
            Session session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId);
            await SetCatalogDetailsForCarbonEmissions(request, session);
            MOBCarbonEmissionsResponse carboinEmissionData = new MOBCarbonEmissionsResponse();
            response = await GetFlightCarbonEmissionDetails(request, response, session);
         
            if (_configuration.GetValue<bool>("EnableCarbonEmissionsFix") == false || session.TravelType == TravelType.TPSearch.ToString())
                carboinEmissionData = await SetCarbonEmissionDataInSession(request, response, session, carboinEmissionData).ConfigureAwait(false);
            return response;
        }

        private async Task<MOBCarbonEmissionsResponse> SetCarbonEmissionDataInSession(MOBCarbonEmissionsRequest request, MOBCarbonEmissionsResponse response, Session session, MOBCarbonEmissionsResponse carboinEmissionData)
        {
            carboinEmissionData = await _sessionHelperService.GetSession<MOBCarbonEmissionsResponse>(session.SessionId, new MOBCarbonEmissionsResponse().ObjectName, new List<string> { session.SessionId, new MOBCarbonEmissionsResponse().ObjectName }).ConfigureAwait(false);
            if (carboinEmissionData == null && response != null)
            {
                await _sessionHelperService.SaveSession<MOBCarbonEmissionsResponse>(response, request.SessionId, new List<string> { request.SessionId, new MOBCarbonEmissionsResponse().ObjectName }, new MOBCarbonEmissionsResponse().ObjectName);
                carboinEmissionData = response;
            }
            else
            {
                if (response?.CarbonEmissionData != null && carboinEmissionData?.CarbonEmissionData != null)
                {
                    carboinEmissionData.CarbonEmissionData.AddRange(response.CarbonEmissionData); // required for multi trip / round trip calls
                    await _sessionHelperService.SaveSession<MOBCarbonEmissionsResponse>(carboinEmissionData, request.SessionId, new List<string> { request.SessionId, new MOBCarbonEmissionsResponse().ObjectName }, new MOBCarbonEmissionsResponse().ObjectName);
                }
            }

            return carboinEmissionData;
        }

        private async Task<MOBCarbonEmissionsResponse> GetFlightCarbonEmissionDetails(MOBCarbonEmissionsRequest request, MOBCarbonEmissionsResponse response, Session session)
        {
            var json = new
            {
                cartid = request.CartId
            };

            string jsonRequest = JsonConvert.SerializeObject(json);
            var jsonResponse = await _flightShoppingService.FlightCarbonEmission<United.Service.Presentation.ReferenceDataModel.TripReference>(session.Token, request.SessionId, "FlightCarbonEmission", jsonRequest);
            if (jsonResponse != null)
            {

                if (jsonResponse != null && jsonResponse.Flights != null && jsonResponse.Flights.Count > 0)
                {
                    List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                    response = new MOBCarbonEmissionsResponse();
                    response.CarbonEmissionData = new List<MOBCarbonEmissionData>();
                    jsonResponse.Flights = jsonResponse.Flights.Where(a => a.CarbonEmission?.MilesPerGallon != 0)?.ToCollection<Service.Presentation.ReferenceDataModel.FlightReference>();
                    jsonResponse.Flights = jsonResponse.Flights.OrderBy(a => a.CarbonEmission.TotalEmission).ToCollection<Service.Presentation.ReferenceDataModel.FlightReference>();
                    for (int i = 0; i < jsonResponse.Flights.Count(); i++)
                    {
                        if (jsonResponse.Flights[i].CarbonEmission != null && jsonResponse.Flights[i].CarbonEmission.MilesPerGallon != 0)
                        {
                            MOBCarbonEmissionData carbonEmission = await BuildCarbonEmissionContent(jsonResponse.Flights[i], (i <= _configuration.GetValue<int>("CabonEmissionEcoIconTopNCount") ? true : false), request, session, lstMessages);
                            if (carbonEmission != null)
                                response.CarbonEmissionData.Add(carbonEmission);
                        }
                    }
                }
                else
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("CarbonEmissionFlightCountError"));
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

            return response;
        }

        private async Task<MOBCarbonEmissionData> BuildCarbonEmissionContent(Service.Presentation.ReferenceDataModel.FlightReference flight, bool showIconForTop3, MOBRequest mOBRequest, Session session,
            List<CMSContentMessage> lstMessages)
        {
            if (flight.CarbonEmission == null) return null;

            var carbonEmission = new MOBCarbonEmissionData();
            carbonEmission.FlightHash = flight.FlightHash;
            var itemWithIconName = new MOBItemWithIconName();
            flight.CarbonEmission.TotalEmission = Math.Round(flight.CarbonEmission.TotalEmission, 0);
            flight.CarbonEmission.MilesPerGallon = Math.Round(flight.CarbonEmission.MilesPerGallon, 0);
            flight.CarbonEmission.LitersPerHundredKM = Math.Round(flight.CarbonEmission.LitersPerHundredKM, 1);
            itemWithIconName.OptionDescription = flight.CarbonEmission?.TotalEmission + " " + _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent12_MOB");
            if (showIconForTop3)
                itemWithIconName.OptionIcon = "iconLeaf";
            carbonEmission.CarbonDetails = itemWithIconName;
            var contentDetails = new List<MOBContentDetails>();
            MOBContentScreen content = new MOBContentScreen();

            if (_configuration.GetValue<bool>("DisableOLDSDLForCarbonEmissions"))
            {
                content.PageTitle = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_PageTitle_MOB");
                content.Header = string.Format(_shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Header_MOB"), flight.CarbonEmission?.TotalEmission);
                content.Body = string.Format(_shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Body_MOB"), flight?.CarbonEmission?.MilesPerGallon, flight?.CarbonEmission?.LitersPerHundredKM);

                var subContent = new List<MOBItem>();
                subContent.Add(new MOBItem { Id = "co_flightTime", CurrentValue = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent1_MOB") });
                subContent.Add(new MOBItem { Id = "co_aircraftType", CurrentValue = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent2_MOB") });
                subContent.Add(new MOBItem { Id = "co_paxText", CurrentValue = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent3_MOB") });
                subContent.Add(new MOBItem { Id = "co_CabonEmissions", CurrentValue = string.Format(_shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Footer_MOB"), _configuration.GetValue<string>("CarbonEmissionLearnMoreLink")) });

                var subContent2 = new List<MOBItem>();
                subContent2.Add(new MOBItem { Id = "co_ecoText", CurrentValue = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent13_MOB") });
                subContent2.Add(new MOBItem { Id = "co_SAF", CurrentValue = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent5_MOB") });

                contentDetails.Add(new MOBContentDetails
                {
                    IconData = new MOBItemWithIconName { OptionDescription = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent7_MOB") }
                                    ,
                    SubContent = subContent
                });
                contentDetails.Add(new MOBContentDetails
                {
                    IconData = new MOBItemWithIconName { OptionDescription = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent11_MOB") }
                                    ,
                    SubContent = subContent2
                });
                content.ContentDetails = contentDetails;

            }
            else
            {
                content.PageTitle = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_PageTitle_MOB");
                content.Header = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Header_MOB_NEW");
                if (!string.IsNullOrEmpty(_shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Body_MOB_NEW")))
                    content.Body = string.Format(_shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Body_MOB_NEW"), flight.CarbonEmission?.TotalEmission, flight?.CarbonEmission?.MilesPerGallon, flight?.CarbonEmission?.LitersPerHundredKM);
            }

            carbonEmission.ContentScreen = content;
            return await Task.FromResult(carbonEmission);
        }

        private async Task<MOBCarbonEmissionData> BuildCarbonEmissionContentForReferenceData( MOBRequest mOBRequest, 
           List<CMSContentMessage> lstMessages, United.Service.Presentation.ReferenceDataModel.CarbonEmission carbonEmissionDetails)
        {

            var carbonEmission = new MOBCarbonEmissionData();
            var itemWithIconName = new MOBItemWithIconName();
            carbonEmissionDetails.TotalEmission = Math.Round(carbonEmissionDetails.TotalEmission, 0);
            carbonEmissionDetails.MilesPerGallon = Math.Round(carbonEmissionDetails.MilesPerGallon, 0);
            carbonEmissionDetails.LitersPerHundredKM = Math.Round(carbonEmissionDetails.LitersPerHundredKM, 1);
            itemWithIconName.OptionDescription = carbonEmissionDetails?.TotalEmission + " " + _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent12_MOB");
            
            carbonEmission.CarbonDetails = itemWithIconName;
            var contentDetails = new List<MOBContentDetails>();
            MOBContentScreen content = new MOBContentScreen();

            if (_configuration.GetValue<bool>("DisableOLDSDLForCarbonEmissions"))
            {
                content.PageTitle = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_PageTitle_MOB");
                content.Header = string.Format(_shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Header_MOB"), carbonEmissionDetails?.TotalEmission);
                content.Body = string.Format(_shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Body_MOB"), carbonEmissionDetails?.MilesPerGallon, carbonEmissionDetails?.LitersPerHundredKM);

                var subContent = new List<MOBItem>();
                subContent.Add(new MOBItem { Id = "co_flightTime", CurrentValue = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent1_MOB") });
                subContent.Add(new MOBItem { Id = "co_aircraftType", CurrentValue = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent2_MOB") });
                subContent.Add(new MOBItem { Id = "co_paxText", CurrentValue = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent3_MOB") });
                subContent.Add(new MOBItem { Id = "co_CabonEmissions", CurrentValue = string.Format(_shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Footer_MOB"), _configuration.GetValue<string>("CarbonEmissionLearnMoreLink")) });

                var subContent2 = new List<MOBItem>();
                subContent2.Add(new MOBItem { Id = "co_ecoText", CurrentValue = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent13_MOB") });
                subContent2.Add(new MOBItem { Id = "co_SAF", CurrentValue = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent5_MOB") });

                contentDetails.Add(new MOBContentDetails
                {
                    IconData = new MOBItemWithIconName { OptionDescription = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent7_MOB") }
                                    ,
                    SubContent = subContent
                });
                contentDetails.Add(new MOBContentDetails
                {
                    IconData = new MOBItemWithIconName { OptionDescription = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Subconent11_MOB") }
                                    ,
                    SubContent = subContent2
                });
                content.ContentDetails = contentDetails;

            }
            else
            {
                content.PageTitle = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_PageTitle_MOB");
                content.Header = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Header_MOB_NEW");
                if (!string.IsNullOrEmpty(_shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Details_Screen")))
                    content.Body = string.Format(_shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Details_Screen"), carbonEmissionDetails?.TotalEmission, carbonEmissionDetails?.MilesPerGallon, carbonEmissionDetails?.LitersPerHundredKM);
            }

            carbonEmission.ContentScreen = content;
            return await Task.FromResult(carbonEmission);
        }


        private async Task SetCatalogDetailsForCarbonEmissions(MOBCarbonEmissionsRequest request, Session session)
        {
            if (session.CatalogItems == null) session.CatalogItems = new List<MOBItem>();
            MOBItem catalog = new MOBItem();            
            if (request.Application.Id == 1)
            {
                catalog.Id = ((int)IOSCatalogEnum.EnableCarbonEmissionsFeature).ToString();
                catalog.CurrentValue = "1";
                if (session.CatalogItems.Any(a => a.Id == catalog.Id) == false)
                    session.CatalogItems.Add(catalog);                
            }
            else if (request.Application.Id == 2)
            {
                catalog.Id = ((int)AndroidCatalogEnum.EnableCarbonEmissionsFeature).ToString();
                catalog.CurrentValue = "1";
                if (session.CatalogItems.Any(a => a.Id == catalog.Id) == false)
                    session.CatalogItems.Add(catalog);
            }
            await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string>() { session.SessionId, session.ObjectName }, session.ObjectName);                        
        }

        private List<MOBSHOPPrice> ReShopRemoveLineItemsForEbulk(SelectTripRequest selectTripRequest, List<MOBSHOPPrice> prices, ReservationDetail cslReservation) {
            if (_shoppingUtility.IsEbulkPNRReshopEnabled(selectTripRequest.Application.Id, selectTripRequest.Application.Version.Major, cslReservation))
            {
                foreach (var price in prices.Where(x => x.PriceType.Equals("CHANGEFEE", StringComparison.OrdinalIgnoreCase)))
                {
                    price.FormattedDisplayValue = null;
                    price.PriceTypeDescription = null;
                    price.DisplayType = null;
                }
            }
            return prices;
        }
    }
}
