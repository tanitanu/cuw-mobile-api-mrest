using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FSRHandler;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Common;
using United.Service.Presentation.ReservationResponseModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.Boombox;
using United.Services.FlightShopping.Common.Extensions;
using United.Utility.Helper;
using United.Utility.HttpService;
using MOBSHOPFlattenedFlight = United.Mobile.Model.Shopping.MOBSHOPFlattenedFlight;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Services.ShopFareWheel.Domain
{
    public class ShopFareWheelBusiness : IShopFareWheelBusiness
    {
        private readonly ICacheLog<ShopFareWheelBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly IMileagePlus _mileagePlus;

        private readonly IDPService _dPService;
        private readonly IFlightShoppingService _flightShoppingService;
        private readonly IDynamoDBService _dynammoService;
        private readonly IShoppingClientService _shoppingClientService;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly IMileagePricingService _mileagePricingService;
        private readonly IFeatureSettings _featureSettings;
        public ShopFareWheelBusiness(ICacheLog<ShopFareWheelBusiness> logger
            , IConfiguration configuration
            , IShoppingUtility shoppingUtility
            , ISessionHelperService sessionHelperService
            , IDPService dPService
            , IFlightShoppingService flightShoppingService
            , IMileagePlus mileagePlus
            , IShoppingSessionHelper shoppingSessionHelper
            , IDynamoDBService dynammoService
            , IShoppingClientService shoppingClientService
            , IFFCShoppingcs fFCShoppingcs
            ,IMileagePricingService mileagePricingService
            ,IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _shoppingUtility = shoppingUtility;
            _sessionHelperService = sessionHelperService;
            _dPService = dPService;
            _flightShoppingService = flightShoppingService;
            _mileagePlus = mileagePlus;
            _shoppingSessionHelper = shoppingSessionHelper;
            _dynammoService = dynammoService;
            _shoppingClientService = shoppingClientService;
            _fFCShoppingcs = fFCShoppingcs;
            _mileagePricingService = mileagePricingService;
            _featureSettings = featureSettings;
        }

        public async Task<FareWheelResponse> GetFareWheelListResponse(SelectTripRequest selectTripRequest)
        {
            FareWheelResponse response = new FareWheelResponse();
            selectTripRequest.GetFlightsWithStops = false;
            selectTripRequest.GetNonStopFlightsOnly = false;
            Session session = await _shoppingSessionHelper.GetBookingFlowSession(selectTripRequest.SessionId);
            if (session.IsReshopChange)
            {
                return response;
            }
            if (_configuration.GetValue<string>("ReturnNoFareWheelData") == null)
            {
                response = await GetFareWheelList(selectTripRequest);
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            response.FareWheelRequest = selectTripRequest;
            response.TransactionId = selectTripRequest.TransactionId;
            response.LanguageCode = selectTripRequest.LanguageCode;
            response.CartId = session.CartId;
            if (response.FareWheel == null || response.FareWheel.Count == 0)
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            if (_configuration.GetValue<string>("Log_CSL_Call_Statistics") != null && _configuration.GetValue<bool>("Log_CSL_Call_Statistics").ToString().ToUpper().Trim() == "TRUE")
            {
                try
                {
                    CSLStatistics _cslStatistics = new CSLStatistics(_logger, _configuration, _dynammoService, null);
                    string callDurations = string.Empty;
                    await _cslStatistics.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", response.CartId, callDurations, "Shopping/GetFareWheelList", selectTripRequest.SessionId);
                }
                catch { }
            }
            return response;
        }

        private async Task<FareWheelResponse> GetFareWheelList(SelectTripRequest selectRequest)
        {
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(selectRequest.SessionId, session.ObjectName, new List<string>() { selectRequest.SessionId, session.ObjectName });
            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }
            FareWheelResponse response = new FareWheelResponse();
           
            United.Services.FlightShopping.Common.ShopSelectRequest request = await GetShopSelectRequest(selectRequest);
            United.Services.FlightShopping.Common.ShopSelectRequest calendarRequest = GetShopSelectFareWheelRequest(request);
            string calendarJsonRequest = JsonConvert.SerializeObject(calendarRequest), shopCSLCallDurations = string.Empty;

            United.Services.FlightShopping.Common.ShopResponse calendarResponse = null;

            Parallel.Invoke((Action)(async() =>
            {
                #region FSR handler

                if (_shoppingUtility.EnableFSRAlertMessages(selectRequest.Application.Id, selectRequest.Application.Version.Major, session.TravelType))
                {
                    if (_configuration.GetValue<bool>("EnableFSRAlertMessages_NonStopFSR2")) // pending CSL implementation for FSR2 nonstop farewheel
                    {
                        try
                        {
                            if (!session.IsReshopChange)
                            {
                                MOBSHOPAvailability avail = await GetLastTripAvailabilityFromPersist(2, selectRequest.SessionId);

                                if (avail == null || !avail.Reservation.SearchType.Equals("RT"))
                                {
                                    return;
                                }
                                bool isSessionModified = false;
                                
                                if (session.SelectTripHasNonStop < 0)
                                {
                                    session.SelectTripHasNonStop = (avail.Trip != null && avail.Trip.FlattenedFlights != null && avail.Trip.FlattenedFlights.Count > 0 && avail.Trip.FlattenedFlights.Any(ff => ff.Flights.Count == 1)) ? 1 : 0;
                                    isSessionModified = true;                                    
                                }
                                if(_shoppingUtility.IsEnableMoneyPlusMilesFeature(selectRequest.Application.Id, selectRequest.Application.Version.Major, session?.CatalogItems)
                                    && session.MileagPlusNumber?.ToUpper().Trim() != selectRequest.MileagePlusAccountNumber?.ToUpper().Trim())
                                {
                                    session.MileagPlusNumber = selectRequest.MileagePlusAccountNumber;
                                    isSessionModified = true;
                                }
                                if (isSessionModified)
                                {
                                    await _sessionHelperService.SaveSession<Session>(session, selectRequest.SessionId, new List<string> { selectRequest.SessionId, new Session().ObjectName }, new Session().ObjectName).ConfigureAwait(false);
                                }
                                bool shouldCheckNonstop = (avail.FSRAlertMessages == null || !avail.FSRAlertMessages.Any()) // no FSR alert was found in the shop call
                                            && session.SelectTripHasNonStop == 0; // Only get nonstop alert if there are no nonstop

                                if (shouldCheckNonstop)
                                {
                                    response.FSRAlertMessages = await GetFareWheelList_FilterNonStop(selectRequest);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("GetFareWheelList - GetFareWheelList_FilterNonStop Error {@Exception}", JsonConvert.SerializeObject(ex));

                        }
                    }
                }

                #endregion
            }),
             () =>  
             {
                 Stopwatch cslSelectTrip_SelectFareWheelWatch;
                 cslSelectTrip_SelectFareWheelWatch = new Stopwatch();
                 cslSelectTrip_SelectFareWheelWatch.Reset();
                 cslSelectTrip_SelectFareWheelWatch.Start();
                 
                 calendarResponse =  _flightShoppingService.SelectFareWheel<United.Services.FlightShopping.Common.ShopResponse>(session.Token, "ShopSelectFareWheel", selectRequest.SessionId, calendarJsonRequest).Result;
                 
                 if (cslSelectTrip_SelectFareWheelWatch.IsRunning)
                 {
                     cslSelectTrip_SelectFareWheelWatch.Stop();
                 }
                 shopCSLCallDurations = shopCSLCallDurations + "|FareWheelCallTime=" + cslSelectTrip_SelectFareWheelWatch.ElapsedMilliseconds.ToString();
             });

            if (calendarResponse != null && calendarResponse.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && (calendarResponse.Errors == null || calendarResponse.Errors.Count == 0))
            {
                shopCSLCallDurations = shopCSLCallDurations + "|ShopSelectFareWheel=" + calendarResponse.CallTimeDomain;
                response.FareWheel = await PopulateFareWheel(calendarResponse.FareWheelGrid, calendarResponse.LastBBXSolutionSetId);
            }
            else
            {
                #region 89882 - CSL: ShopFareWheel : FareWheelOnly is not allowed and  FAREWHEEEL NOT FOUND - Ravi/Issuf
                if (calendarResponse?.Errors != null && calendarResponse.Errors.Count > 0)
                {
                    string errorMessage = string.Empty;
                    foreach (var error in calendarResponse.Errors)
                    {
                        errorMessage = errorMessage + " " + error.Message;

                        if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10035"))
                        {
                            throw new MOBUnitedException(error.Message);
                        }
                        else if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10038"))
                        {
                            throw new MOBUnitedException(error.Message);
                        }
                        else if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10046")) //CSL session not found error code
                        {
                            throw new MOBUnitedException(error.Message);
                        }

                        // Added by Ali as part of Task 264468:Booking Flow Exception analysis - GetFareWheelList 
                        else if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && (error.MinorCode.Trim().Equals("10051") || error.MinorCode.Trim().Equals("10052") || error.MinorCode.Trim().Equals("10056") || error.MinorCode.Trim().Equals("10069")))
                        {
                            throw new MOBUnitedException(error.Message);
                        }
                    }
                    throw new System.Exception(errorMessage);
                }
                #endregion
            }
            response.CallDurationText = shopCSLCallDurations;
            return response;
        }

        private async Task<United.Services.FlightShopping.Common.ShopSelectRequest> GetShopSelectRequest(SelectTripRequest selectRequest, bool isForSelectTrip = false)
        {
            United.Services.FlightShopping.Common.ShopSelectRequest shopSelectRequest = new United.Services.FlightShopping.Common.ShopSelectRequest
            {
                ChannelType = _configuration.GetValue<string>("Shopping - ChannelType"),
                AccessCode = _configuration.GetValue<string>("AccessCode - CSLShopping")
            };

            bool decodeOTP = false;
            bool.TryParse(_configuration.GetValue<string>("DecodesOnTimePerformance"), out decodeOTP);
            shopSelectRequest.DecodesOnTimePerfRequested = decodeOTP;

            bool decodesRequested = false;
            bool.TryParse(_configuration.GetValue<string>("DecodesRequested"), out decodesRequested);
            shopSelectRequest.DecodesRequested = decodesRequested;

            Model.Shopping.ShoppingResponse shop = new Model.Shopping.ShoppingResponse();
            shop = await _sessionHelperService.GetSession<Model.Shopping.ShoppingResponse>(selectRequest.SessionId, shop.ObjectName, new List<string> { selectRequest.SessionId, shop.ObjectName });

            if (shop != null && !string.IsNullOrEmpty(shop.CartId))
            {
                shopSelectRequest.CartId = shop.CartId;
            }
            else
            {
                throw new MOBUnitedException("The booking session could not be found.");
            }

            shopSelectRequest.BBXCellId = selectRequest.ProductId;
            shopSelectRequest.BBXSolutionSetId = selectRequest.TripId;
            if (shop.Request.ResultSortType.ToUpper() == "P")
            {
                shopSelectRequest.SortType = "price";
            }
            shopSelectRequest.SortTypeDescending = false;
            shopSelectRequest.CalendarOnly = false;

            shopSelectRequest.UseFilters = selectRequest.UseFilters;
            var isStandardRevenueSearch = IsStandardRevenueSearch(shop.Request.IsCorporateBooking, shop.Request.IsYoungAdultBooking,
                                                      shop.Request.AwardTravel, shop.Request.EmployeeDiscountId,
                                                      shop.Request.TravelType, shop.Request.IsReshop || shop.Request.IsReshopChange,
                                                      shop.Request.FareClass, shop.Request.PromotionCode);
            if (selectRequest.UseFilters && selectRequest.Filters != null)
            {
                shopSelectRequest.SearchFilters = GetSearchFilters(selectRequest.Filters, selectRequest.Application.Id, selectRequest.Application.Version.Major,
                                                                   isStandardRevenueSearch, shop.Request.IsELFFareDisplayAtFSR, shop.Request.FareType);
            }

            shopSelectRequest.ChannelType = "MOBILE";
            shopSelectRequest.CountryCode = shop.Request.CountryCode;

            shopSelectRequest.FlexibleDaysAfter = 0;
            shopSelectRequest.FlexibleDaysBefore = 0;

            shopSelectRequest.MaxTrips = getShoppingSearchMaxTrips();
            shopSelectRequest.PageIndex = 1;
            shopSelectRequest.PageSize = _configuration.GetValue<int>("ShopAndSelectTripCSLRequestPageSize");//getShoppingSearchMaxTrips();
            shopSelectRequest.CalendarDateChange = selectRequest.CalendarDateChange;

            shopSelectRequest.SortType = selectRequest.ResultSortType;

            bool includeAmenities = false;

            if (!_configuration.GetValue<bool>("ByPassAmenities"))
            {
                bool.TryParse(_configuration.GetValue<string>("IncludeAmenities"), out includeAmenities);
            }
            shopSelectRequest.IncludeAmenities = includeAmenities;

            try
            {
                if (shop.Request.AwardTravel)
                {
                    CSLShopRequest cslShopRequest = new CSLShopRequest();
                    cslShopRequest = await _sessionHelperService.GetSession<CSLShopRequest>(selectRequest.SessionId, cslShopRequest.ObjectName, new List<string> { selectRequest.SessionId, cslShopRequest.ObjectName });
                    shopSelectRequest.LoyaltyPerson = cslShopRequest.ShopRequest.LoyaltyPerson;
                }
            }
            catch { };
            if (_configuration.GetValue<bool>("EnableNonStopFlight") && (selectRequest.GetNonStopFlightsOnly || selectRequest.GetFlightsWithStops))
            {
                if (shopSelectRequest.SearchFilters == null)
                {
                    shopSelectRequest.SearchFilters = new SearchFilterInfo();
                }
                RequestForNonStopFlights(selectRequest, shopSelectRequest);
            }
            // Refundable fares toggle feature
            if (IsEnableRefundableFaresToggle(selectRequest.Application.Id, selectRequest.Application.Version.Major) &&
                isStandardRevenueSearch &&
                (isForSelectTrip || (selectRequest.Filters?.RefundableFaresToggle?.IsSelected ?? false)))
            {
                shopSelectRequest.FareType = _configuration.GetValue<string>("RefundableFaresToggleFareType");
                if (shopSelectRequest.Characteristics == null) shopSelectRequest.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                shopSelectRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "filterflightsbytoggle", Value = "true" });

                if (isForSelectTrip && (!selectRequest.UseFilters || selectRequest.Filters == null))
                {
                    if (shopSelectRequest.SearchFilters == null)
                    {
                        shopSelectRequest.SearchFilters = new SearchFilterInfo();
                    }
                    AddRefundableFaresToggleFilter(shopSelectRequest.SearchFilters, selectRequest.Filters, selectRequest.Application.Id, selectRequest.Application.Version.Major,
                                                   isStandardRevenueSearch, shop.Request.IsELFFareDisplayAtFSR, shop.Request.FareType);
                }
            }
            // Mixed cabin fares toggle feature
            if (IsMixedCabinFilerEnabled(selectRequest.Application.Id, selectRequest.Application.Version.Major)
                && selectRequest.Filters?.AdditionalToggles?.Any(a => a.Key == _configuration.GetValue<string>("MixedCabinToggleKey")) == true)
            {
                shopSelectRequest.FareType = _configuration.GetValue<string>("MixedCabinToggle");
                if (shopSelectRequest.Characteristics == null) shopSelectRequest.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                shopSelectRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "filterflightsbytoggle", Value = "true" });

                if (isForSelectTrip && (!selectRequest.UseFilters || selectRequest.Filters == null))
                {
                    if (shopSelectRequest.SearchFilters == null)
                    {
                        shopSelectRequest.SearchFilters = new SearchFilterInfo();
                    }
                    AddMixedCabinToggleFilter(shopSelectRequest.SearchFilters, selectRequest.Filters, selectRequest.Application.Id, selectRequest.Application.Version.Major,
                                                   isStandardRevenueSearch, shop.Request.IsELFFareDisplayAtFSR, shopSelectRequest.FareType);
                }
            }
            if (_configuration.GetValue<bool>("EnableFSRMoneyPlusMilesFeature")
                && ShopStaticUtility.ValidateMoneyPlusMilesFlow(shop?.Request)
                && selectRequest.IsMoneyPlusMiles && !string.IsNullOrEmpty(selectRequest.MoneyPlusMilesOptionId))
            {
                shopSelectRequest.MoneyAndMilesOptionId = selectRequest.MoneyPlusMilesOptionId;
                shopSelectRequest.MileagePlusNumber = selectRequest.MileagePlusAccountNumber;
                SelectTripSetLoyaltyDetails(selectRequest, shopSelectRequest);
            }
            if (await _featureSettings.GetFeatureSettingValue("EnableFSRETCTravelCreditsFeature").ConfigureAwait(false)
                && selectRequest.PricingType == PricingType.ETC.ToString())
            {
                if (shopSelectRequest.SearchFilters == null)
                {
                    shopSelectRequest.SearchFilters = new SearchFilterInfo { ShopIndicators = new ShopIndicators()};
                }
                shopSelectRequest.SearchFilters.ShopIndicators.IsTravelCreditsApplied = true;
                shopSelectRequest.MileagePlusNumber = selectRequest.MileagePlusAccountNumber ?? shop.Request.MileagePlusAccountNumber;
                shopSelectRequest.LoyaltyId = selectRequest.MileagePlusAccountNumber ?? shop.Request.MileagePlusAccountNumber;
                SelectTripSetLoyaltyDetails(selectRequest, shopSelectRequest);

            }
            return shopSelectRequest;
        }

        private static void SelectTripSetLoyaltyDetails(SelectTripRequest selectRequest, ShopSelectRequest shopSelectRequest)
        {
            if (!string.IsNullOrEmpty(selectRequest.MileagePlusAccountNumber))
            {
                if (shopSelectRequest.LoyaltyPerson == null)
                {
                    shopSelectRequest.LoyaltyPerson = new Service.Presentation.PersonModel.LoyaltyPerson();
                    shopSelectRequest.LoyaltyPerson.LoyaltyProgramMemberID = selectRequest.MileagePlusAccountNumber;
                    if (selectRequest.PremierStatusLevel == -1)
                    {
                        selectRequest.PremierStatusLevel = 0;// General Member
                    }
                    shopSelectRequest.LoyaltyPerson.LoyaltyProgramMemberTierLevel = (Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel)selectRequest.PremierStatusLevel;
                    shopSelectRequest.LoyaltyPerson.AccountBalances = new Collection<Service.Presentation.CommonModel.LoyaltyAccountBalance>();
                    Service.Presentation.CommonModel.LoyaltyAccountBalance balance = new Service.Presentation.CommonModel.LoyaltyAccountBalance();
                    int.TryParse(selectRequest.MileageBalance, out int bal);
                    balance.Balance = bal;
                    balance.BalanceType = Service.Presentation.CommonEnumModel.LoyaltyAccountBalanceType.MilesBalance;
                    shopSelectRequest.LoyaltyPerson.AccountBalances.Add(balance);
                }
            }
        }

        private United.Services.FlightShopping.Common.ShopSelectRequest GetShopSelectFareWheelRequest(United.Services.FlightShopping.Common.ShopSelectRequest selectRequest)
        {
            United.Services.FlightShopping.Common.ShopSelectRequest fareWheelRequest = selectRequest;
            ///224314 : mApp : Booking –Revenue- Multitrip- Fare wheel in FSR2 is not updating correctly after selecting alternate date and user unable to select alternate dates which are not in initial FSR2 farewheel
            ///Srini 11/22/2017 -- CB
            if (_configuration.GetValue<bool>("BugFixToggleFor17M"))
            {
                if (fareWheelRequest.CalendarDateChange == null)
                {
                    fareWheelRequest.CalendarDateChange = "";
                }
            }
            else
            {
                fareWheelRequest.CalendarDateChange = "";
            }
            fareWheelRequest.CalendarOnly = false;
            fareWheelRequest.FareWheelOnly = true;
            fareWheelRequest.FlexibleDaysAfter = getFlexibleDaysAfter();
            fareWheelRequest.FlexibleDaysBefore = getFlexibleDaysBefore();
            fareWheelRequest.DecodesRequested = false;

            return fareWheelRequest;
        }

        private async Task<MOBSHOPAvailability> GetLastTripAvailabilityFromPersist(int lastTripIndexRequested, string sessionID)
        {
            #region
            MOBSHOPAvailability lastTripAvailability = null;
            LatestShopAvailabilityResponse persistAvailability = new LatestShopAvailabilityResponse();
            persistAvailability = await _sessionHelperService.GetSession<LatestShopAvailabilityResponse>(sessionID, persistAvailability.ObjectName, new List<string> { sessionID, persistAvailability.ObjectName });

            if (persistAvailability != null && persistAvailability.AvailabilityList != null && persistAvailability.AvailabilityList.Count > 0 && persistAvailability.AvailabilityList[lastTripIndexRequested.ToString()] != null)
            {
                lastTripAvailability = persistAvailability.AvailabilityList[lastTripIndexRequested.ToString()];
            }

            return lastTripAvailability;
            #endregion
        }

        private async Task<List<MOBFSRAlertMessage>> GetFareWheelList_FilterNonStop(SelectTripRequest selectRequest)
        {
            FareWheelResponse response = new FareWheelResponse();
            United.Services.FlightShopping.Common.ShopSelectRequest request = await GetShopSelectRequest(selectRequest);
            United.Services.FlightShopping.Common.ShopSelectRequest calendarRequest = GetShopSelectFareWheelRequest(request);
            List<MOBFSRAlertMessage> _FSRAlertMessages = null;

            //---------------Non-Stop Filters-----------
            if (calendarRequest.SearchFilters == null)
            {
                calendarRequest.SearchFilters = new SearchFilterInfo();
            }

            calendarRequest.SearchFilters.StopCountMax = 0;
            calendarRequest.UseFilters = true;

            United.Mobile.Model.Shopping.ShoppingResponse shopResponse = new United.Mobile.Model.Shopping.ShoppingResponse();
            shopResponse = await _sessionHelperService.GetSession<United.Mobile.Model.Shopping.ShoppingResponse>(selectRequest.SessionId, shopResponse.ObjectName, new List<string> { selectRequest.SessionId, shopResponse.ObjectName });
            if (shopResponse == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }
            United.Mobile.Model.Shopping.MOBSHOPShopRequest shopRequest = shopResponse.Request;

            string calendarJsonRequest = JsonConvert.SerializeObject(calendarRequest), shopCSLCallDurations = string.Empty;

            //    logEntries.Add(LogEntry.GetLogEntry<string>(selectRequest.SessionId, "ShopSelectFareWheel_FilterNonStop - Request url", "Trace", selectRequest.Application.Id, selectRequest.Application.Version.Major, selectRequest.DeviceId, url2));

            Stopwatch cslSelectTrip_SelectFareWheelWatch;
            cslSelectTrip_SelectFareWheelWatch = new Stopwatch();
            cslSelectTrip_SelectFareWheelWatch.Reset();
            cslSelectTrip_SelectFareWheelWatch.Start();

            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(selectRequest.SessionId, session.ObjectName, new List<string> { selectRequest.SessionId, session.ObjectName });
            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }
            United.Services.FlightShopping.Common.ShopResponse calendarResponse = null;

            calendarResponse = await _flightShoppingService.SelectFareWheel<United.Services.FlightShopping.Common.ShopResponse>(session.Token, "ShopSelectFareWheel", selectRequest.SessionId, calendarJsonRequest);

            if (cslSelectTrip_SelectFareWheelWatch.IsRunning)
            {
                cslSelectTrip_SelectFareWheelWatch.Stop();
            }
            shopCSLCallDurations = shopCSLCallDurations + "|FareWheelCallTime=" + cslSelectTrip_SelectFareWheelWatch.ElapsedMilliseconds.ToString();

            shopCSLCallDurations = shopCSLCallDurations + "|ShopSelectFareWheel_FilterNonStop=" + calendarResponse.CallTimeDomain;
            if (calendarResponse != null && calendarResponse.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && (calendarResponse.Errors == null || calendarResponse.Errors.Count == 0))
            {
                response.FareWheel = await PopulateFareWheel(calendarResponse.FareWheelGrid, calendarResponse.LastBBXSolutionSetId);

                ///--------------- Validate Non-Stop Rules-------------
                var airportList = await GetAiportListDescFromPersist(session.SessionId);
                string OriginDesc = airportList.AirportsList.Where(i => i.AirportCode == shopRequest.Trips[1].Origin.ToUpper()).Select(i => i.AirportNameMobile).FirstOrDefault();
                string DestinationDesc = airportList.AirportsList.Where(i => i.AirportCode == shopRequest.Trips[1].Destination.ToUpper()).Select(i => i.AirportNameMobile).FirstOrDefault();

                List<CMSContentMessage> lstMessages = null;
                MOBMobileCMSContentMessages messages = null;
                if (_configuration.GetValue<bool>("FSRNonstopSuggestFutureDateInfoDisplayed"))
                {
                    lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(shopRequest, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                    messages = _shoppingUtility.GetSDLMessageFromList(lstMessages, _configuration.GetValue<string>("FSRNonstopSuggestFutureDateContent")).FirstOrDefault(); ;
                }

                var FSRNonStopSuggestObj = new List<IRule<MOBFSRAlertMessage>>
                    {
                        // Add all enhancements rule here
                       new FSRNonstopSuggestFutureDate(response.FareWheel, OriginDesc, DestinationDesc, shopRequest.Trips[1].DepartDate, shopRequest,1, _configuration, messages)

                    };

                // Get the first enhancement based on given priority
                var firstAlert = FSRNonStopSuggestObj.FirstOrDefault(rule => rule.ShouldExecuteRule());

                if (firstAlert != null)
                {
                    _FSRAlertMessages = new List<MOBFSRAlertMessage> { await firstAlert.Execute() };
                }
            }

            try
            {
                response.CallDurationText = shopCSLCallDurations;
            }
            catch { }


            return _FSRAlertMessages;
        }

        private async Task<List<MOBSHOPFareWheelItem>> PopulateFareWheel(Grid fareWheelGrid, string tripId)
        {
            List<MOBSHOPFareWheelItem> fares = new List<MOBSHOPFareWheelItem>();

            if (fareWheelGrid != null && fareWheelGrid.Rows != null && fareWheelGrid.Rows.Count > 0)
            {
                CultureInfo ci = null;

                foreach (GridRow row in fareWheelGrid.Rows)
                {
                    if (row.RowItems != null && row.RowItems.Count > 0)
                    {
                        foreach (GridRowItem item in row.RowItems)
                        {
                            if (ci == null)
                            {
                                if (item.PricingItem != null)
                                {
                                    ci = TopHelper.GetCultureInfo(item.PricingItem.Currency);
                                }
                            }

                            MOBSHOPFareWheelItem kvp = new MOBSHOPFareWheelItem();
                            if (item != null)
                            {
                                if (item.PricingItem != null)
                                {
                                    int year = DateTime.Today.Year;
                                    int.TryParse(item.Day.Substring(0, 4), out year);
                                    int month = DateTime.Today.Month;
                                    int.TryParse(item.Day.Substring(5, 2), out month);
                                    int day = DateTime.Today.Day;
                                    int.TryParse(item.Day.Substring(8, 2), out day);

                                    DateTime fareDate = new DateTime(year, month, day);
                                    kvp.Key = (month < 10 ? "0" + month.ToString() : month.ToString()) + "/" + (day < 10 ? "0" + day.ToString() : day.ToString()) + "/" + year.ToString();
                                    kvp.DisplayValue = fareDate.ToString("ddd MMM dd");
                                    if (!_configuration.GetValue<bool>("DisableGetFareWheelProductIdMissingFix"))
                                    {
                                        kvp.Value = "Not available";
                                        kvp.ProductId = "Not available";
                                        if (item.PricingItem.Amount > 0 && !string.IsNullOrWhiteSpace(item.ID))
                                        {
                                            kvp.Value = TopHelper.FormatAmountForDisplay(item.PricingItem.Amount, ci);
                                            kvp.ProductId = item.ID;
                                        }
                                        if (await _featureSettings.GetFeatureSettingValue("EnableFSRETCTravelCreditsFeature").ConfigureAwait(false) 
                                            && !string.IsNullOrWhiteSpace(item.ID) 
                                            && item.PricingItem.StrikeThroughAmount != null 
                                            && item.PricingItem.StrikeThroughAmount > 0)
                                        {
                                            kvp.Value = TopHelper.FormatAmountForDisplay(item.PricingItem.Amount, ci);
                                            kvp.ProductId = item.ID;
                                            kvp.PricingTypeDisplayValue = "<font color='"+_configuration.GetValue<string>("FareWheelStrikeThroughFontColor")+"'><s>" + TopHelper.FormatAmountForDisplay(item.PricingItem.StrikeThroughAmount ?? 0, ci) + "</s></font><br>" + kvp.Value;
                                            kvp.Value = TopHelper.FormatAmountForDisplay(item.PricingItem.StrikeThroughAmount ?? 0, ci);                                            
                                        }
                                    }
                                    else
                                    {
                                        if (item.PricingItem.Amount > 0)
                                        {
                                            kvp.Value = TopHelper.FormatAmountForDisplay(item.PricingItem.Amount, ci);
                                            kvp.ProductId = item.ID; 
                                            if (await _featureSettings.GetFeatureSettingValue("EnableFSRETCTravelCreditsFeature").ConfigureAwait(false) && item.PricingItem.StrikeThroughAmount != null)
                                            {
                                                kvp.PricingTypeDisplayValue = "<font color='"+_configuration.GetValue<string>("FareWheelStrikeThroughFontColor")+"'><s>" + TopHelper.FormatAmountForDisplay(item.PricingItem.StrikeThroughAmount ?? 0, ci) + "</s></font><br>" + kvp.Value;
                                                kvp.Value = TopHelper.FormatAmountForDisplay(item.PricingItem.StrikeThroughAmount ?? 0, ci);
                                            }
                                        }
                                        else
                                        {
                                            kvp.Value = "Not available";
                                            kvp.ProductId = "Not available";
                                        }
                                    }
                                    kvp.TripId = tripId;
                                }
                                else
                                {
                                    int year = DateTime.Today.Year;
                                    int.TryParse(item.Day.Substring(0, 4), out year);
                                    int month = DateTime.Today.Month;
                                    int.TryParse(item.Day.Substring(5, 2), out month);
                                    int day = DateTime.Today.Day;
                                    int.TryParse(item.Day.Substring(8, 2), out day);

                                    DateTime fareDate = new DateTime(year, month, day);
                                    kvp.Key = (month < 10 ? "0" + month.ToString() : month.ToString()) + "/" + (day < 10 ? "0" + day.ToString() : day.ToString()) + "/" + year.ToString();
                                    kvp.Value = "Not available";
                                    kvp.DisplayValue = fareDate.ToString("ddd MMM dd");
                                    kvp.ProductId = "Not available";
                                    kvp.TripId = tripId;
                                }

                                if (item.MoneyAndMilesPricingItem != null && item.MoneyAndMilesPricingItem.Prices.Count > 0)
                                {
                                    foreach (var mmPricing in item.MoneyAndMilesPricingItem.Prices)
                                    {
                                        if (mmPricing.Amount > 0 )
                                        {
                                            if (mmPricing.PricingType == "Fare")
                                            {
                                                kvp.MoneyPlusMilesValue = TopHelper.FormatAmountForDisplay(mmPricing.Amount, ci);
                                            }else if(mmPricing.PricingType == "Miles")
                                            {
                                                kvp.MoneyPlusMilesValue += " + " +TopHelper.FormatMoneyPlusMilesForDisplay(mmPricing.Amount.ToString(), true);
                                            }
                                            kvp.ProductId = item.ID;
                                        }
                                        else
                                        {
                                            kvp.MoneyPlusMilesValue = "Not available";
                                            kvp.ProductId = "Not available";
                                        }
                                    }
                                    
                                                                  
                                }
                            }

                            if (!string.IsNullOrEmpty(kvp.TripId))
                            {
                                fares.Add(kvp);
                            }
                        }
                    }
                }
            }

            return fares;
        }

        private SearchFilterInfo GetSearchFilters(MOBSearchFilters filters, int appId, string appVersion, bool isStandardRevenueSearch, bool isELFFareDisplayAtFSR, string fareType)
        {
            SearchFilterInfo filter = new SearchFilterInfo();

            if (filters != null)
            {
                if (!String.IsNullOrEmpty(filters.AircraftTypes))
                {
                    filter.AircraftTypes = filters.AircraftTypes;
                }

                if (!String.IsNullOrEmpty(filters.AirportsDestination))
                {
                    filter.AirportsDestination = filters.AirportsDestination;
                }

                if (filters.AirportsDestinationList != null && filters.AirportsDestinationList.Count > 0)
                {
                    filter.AirportsDestinationList = new List<CodeDescPair>();
                    foreach (MOBSearchFilterItem kvp in filters.AirportsDestinationList)
                    {
                        CodeDescPair cdp = new CodeDescPair
                        {
                            Code = kvp.Key,
                            Description = kvp.Value,
                            Amount = kvp.Amount,
                            Currency = kvp.Currency
                        };

                        filter.AirportsDestinationList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.AirportsOrigin))
                {
                    filter.AirportsOrigin = filters.AirportsOrigin;
                }

                if (filters.AirportsOriginList != null && filters.AirportsOriginList.Count > 0)
                {
                    filter.AirportsOriginList = new List<CodeDescPair>();
                    foreach (MOBSearchFilterItem kvp in filters.AirportsOriginList)
                    {
                        CodeDescPair cdp = new CodeDescPair
                        {
                            Code = kvp.Key,
                            Description = kvp.Value,
                            Amount = kvp.Amount,
                            Currency = kvp.Currency
                        };

                        filter.AirportsOriginList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.AirportsStop))
                {
                    filter.AirportsStop = filters.AirportsStop;
                }

                if (filters.AirportsStopList != null && filters.AirportsStopList.Count > 0)
                {
                    filter.AirportsStopList = new List<CodeDescPair>();
                    foreach (MOBSearchFilterItem kvp in filters.AirportsStopList)
                    {
                        CodeDescPair cdp = new CodeDescPair
                        {
                            Code = kvp.Key,
                            Description = kvp.Value,
                            Amount = kvp.Amount,
                            Currency = kvp.Currency
                        };

                        filter.AirportsStopList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.AirportsStopToAvoid))
                {
                    filter.AirportsStopToAvoid = filters.AirportsStopToAvoid;
                }

                if (filters.AirportsStopToAvoidList != null && filters.AirportsStopToAvoidList.Count > 0)
                {
                    filter.AirportsStopToAvoidList = new List<CodeDescPair>();
                    foreach (MOBSearchFilterItem kvp in filters.AirportsStopToAvoidList)
                    {
                        CodeDescPair cdp = new CodeDescPair
                        {
                            Code = kvp.Key,
                            Description = kvp.Value,
                            Amount = kvp.Amount,
                            Currency = kvp.Currency
                        };

                        filter.AirportsStopToAvoidList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.BookingCodes))
                {
                    filter.BookingCodes = filters.BookingCodes;
                }

                if (!String.IsNullOrEmpty(filters.CarriersMarketing))
                {
                    filter.CarriersMarketing = filters.CarriersMarketing;
                }

                if (filters.CarriersMarketingList != null && filters.CarriersMarketingList.Count > 0)
                {
                    filter.CarriersMarketingList = new List<CodeDescPair>();
                    foreach (MOBSearchFilterItem kvp in filters.CarriersMarketingList)
                    {
                        CodeDescPair cdp = new CodeDescPair
                        {
                            Code = kvp.Key,
                            Description = kvp.Value,
                            Amount = kvp.Amount,
                            Currency = kvp.Currency
                        };

                        filter.CarriersMarketingList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.CarriersOperating))
                {
                    filter.CarriersOperating = filters.CarriersOperating;
                }

                if (filters.CarriersOperatingList != null && filters.CarriersOperatingList.Count > 0)
                {
                    filter.CarriersOperatingList = new List<CodeDescPair>();
                    foreach (MOBSearchFilterItem kvp in filters.CarriersOperatingList)
                    {
                        CodeDescPair cdp = new CodeDescPair
                        {
                            Code = kvp.Key,
                            Description = kvp.Value,
                            Amount = kvp.Amount,
                            Currency = kvp.Currency
                        };

                        filter.CarriersOperatingList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.EquipmentCodes))
                {
                    filter.EquipmentCodes = filters.EquipmentCodes;
                }

                if (filters.EquipmentList != null && filters.EquipmentList.Count > 0)
                {
                    filter.EquipmentList = new List<CodeDescPair>();
                    foreach (MOBSearchFilterItem kvp in filters.EquipmentList)
                    {
                        CodeDescPair cdp = new CodeDescPair
                        {
                            Code = kvp.Key,
                            Description = kvp.Value,
                            Amount = kvp.Amount,
                            Currency = kvp.Currency
                        };

                        filter.EquipmentList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.EquipmentTypes))
                {
                    filter.EquipmentTypes = filters.EquipmentTypes;
                }

                if (filters.FareFamilies != null && filters.FareFamilies.Count > 0)
                {
                    filter.FareFamilies = new sliceFareFamilies
                    {
                        fareFamily = new fareFamilyType[filters.FareFamilies.Count]
                    };
                    int cnt = 0;
                    foreach (MOBSHOPFareFamily ff in filters.FareFamilies)
                    {

                        fareFamilyType fft = new fareFamilyType
                        {
                            fareFamily = string.IsNullOrEmpty(ff.FareFamily) ? "" : ff.FareFamily,
                            maxMileage = ff.MaxMileage
                        };
                        if (!string.IsNullOrEmpty(ff.MaxPrice))
                        {
                            fft.maxPrice = new price
                            {
                                amount = Convert.ToDecimal(ff.MaxPrice)
                            };
                        }
                        fft.minMileage = ff.MinMileage;
                        if (!string.IsNullOrEmpty(ff.MinPrice))
                        {
                            fft.minPrice = new price
                            {
                                amount = Convert.ToDecimal(ff.MinPrice)
                            };
                        }
                        fft.minPriceInSummary = ff.MinPriceInSummary;
                        filter.FareFamilies.fareFamily[cnt] = fft;
                        cnt++;
                    }
                }

                if (!String.IsNullOrEmpty(filters.FareFamily))
                {
                    filter.FareFamily = filters.FareFamily;
                }

                if (!String.IsNullOrEmpty(filters.TimeArrivalMax))
                {
                    filter.TimeArrivalMax = filters.TimeArrivalMax;
                }

                if (!String.IsNullOrEmpty(filters.TimeArrivalMin))
                {
                    filter.TimeArrivalMin = filters.TimeArrivalMin;
                }

                if (!String.IsNullOrEmpty(filters.TimeDepartMax))
                {
                    filter.TimeDepartMax = filters.TimeDepartMax;
                }

                if (!String.IsNullOrEmpty(filters.TimeDepartMin))
                {
                    filter.TimeDepartMin = filters.TimeDepartMin;
                }

                if (filters.WarningsFilter != null && filters.WarningsFilter.Count > 0)
                {
                    foreach (MOBSearchFilterItem warningFilter in filters.WarningsFilter)
                    {
                        filter.Warnings.Add(warningFilter.Key);
                    }
                }

                filter.CabinCountMax = filters.CabinCountMax;
                filter.CabinCountMin = filters.CabinCountMin;
                filter.CarrierDefault = filters.CarrierDefault;
                filter.CarrierExpress = filters.CarrierExpress;
                filter.CarrierPartners = filters.CarrierPartners;
                filter.CarrierStar = filters.CarrierStar;
                filter.DurationMax = filters.DurationMax;
                filter.DurationMin = filters.DurationMin;
                filter.DurationStopMax = filters.DurationStopMax;
                filter.DurationStopMin = filters.DurationStopMin;
                filter.PriceMax = filters.PriceMax;
                filter.PriceMin = filters.PriceMin;
                filter.StopCountExcl = filters.StopCountExcl;
                filter.StopCountMax = filters.StopCountMax;
                filter.StopCountMin = filters.StopCountMin;
            }
            else //set default values
            {
                filter.FareFamily = "ECONOMY";
            }
            // Refundable fares toggle feature
            AddRefundableFaresToggleFilter(filter, filters, appId, appVersion, isStandardRevenueSearch, isELFFareDisplayAtFSR, fareType);
            // Mixed Cabin toggle feature
            AddMixedCabinToggleFilter(filter, filters, appId, appVersion, isStandardRevenueSearch, isELFFareDisplayAtFSR, fareType);

            return filter;
        }

        private int getFlexibleDaysAfter()
        {
            int flexibleFareDaysAfter = 0;
            int.TryParse(_configuration.GetValue<string>("AffinitySearchFlexibleDaysAfter"), out flexibleFareDaysAfter);

            return flexibleFareDaysAfter;
        }

        private int getFlexibleDaysBefore()
        {
            int flexibleFareDaysBefore = 0;
            int.TryParse(_configuration.GetValue<string>("AffinitySearchFlexibleDaysBefore"), out flexibleFareDaysBefore);

            return flexibleFareDaysBefore;
        }
        private async Task<AirportDetailsList> GetAiportListDescFromPersist(string sessionID)
        {
            #region
            AirportDetailsList persistAirportDetailsList = new AirportDetailsList();

            persistAirportDetailsList = await _sessionHelperService.GetSession<AirportDetailsList>(sessionID, persistAirportDetailsList.ObjectName, new List<string> { sessionID, persistAirportDetailsList.ObjectName });
            //if (persistAirportDetailsList != null)
            //{
            //    persistAirportDetailsList.AirportsList.Where(a => a.AirportCode == "").Select(a => a.AirportInfo);
            //}

            return persistAirportDetailsList;
            #endregion
        }

        private int getShoppingSearchMaxTrips()
        {
            int maxTrips = 125;
            int.TryParse(_configuration.GetValue<string>("ShoppingSearchMaxTrips"), out maxTrips);

            return maxTrips;
        }

        private void RequestForNonStopFlights(SelectTripRequest selectRequest, United.Services.FlightShopping.Common.ShopSelectRequest shopSelectRequest)
        {
            shopSelectRequest.UseFilters = true;
            SetStopCountsToGetNonStopFlights(selectRequest.GetNonStopFlightsOnly, selectRequest.GetFlightsWithStops, shopSelectRequest.SearchFilters);
        }

        private void SetStopCountsToGetNonStopFlights(bool getNonStopFlights, bool getFlightsWithStops, SearchFilterInfo searchFiltersIn)
        {
            if ((!getNonStopFlights && !getFlightsWithStops) || (getNonStopFlights && getFlightsWithStops))
            {
                return;
            }

            if (searchFiltersIn == null)
            {
                searchFiltersIn = new SearchFilterInfo();
            }

            if (getNonStopFlights) // getNonStopFlights == true means First shop or First select trip call()
            {
                searchFiltersIn.StopCountMax = 0;
                searchFiltersIn.StopCountMin = -1;
            }
            else  // getFlightsWithStops == true means second shop or second select trip call()
            {
                searchFiltersIn.StopCountMax = -1;
                searchFiltersIn.StopCountMin = 1;
            }
        }

        public async Task<FareWheelResponse> GetShopFareWheelListResponse(MOBSHOPShopRequest shopRequest)
        {
            shopRequest.GetFlightsWithStops = false;
            shopRequest.GetNonStopFlightsOnly = false;
            FareWheelResponse response = new FareWheelResponse();
            Session session = await _shoppingSessionHelper.GetBookingFlowSession(shopRequest.SessionId);
            if (session.IsReshopChange)
            {
                return response;
            }
            if (_configuration.GetValue<string>("ReturnNoFareWheelData") == null)
            {
                response = await GetShopFareWheelList(shopRequest);
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            response.TransactionId = shopRequest.TransactionId;
            response.LanguageCode = shopRequest.LanguageCode;
            response.CartId = session.CartId;
            if (response.FareWheel == null || response.FareWheel.Count == 0)
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            return response;
        }

        private async Task<FareWheelResponse> GetShopFareWheelList(MOBSHOPShopRequest shopRequest)
        {
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(shopRequest.SessionId, session.ObjectName, new List<string> { shopRequest.SessionId, session.ObjectName });
            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }
            FareWheelResponse response = new FareWheelResponse();
            ShopRequest request = await GetShopRequest(shopRequest, false);

            request.DisableMostRestrictive = !shopRequest.IsELFFareDisplayAtFSR;

            request.CartId = session.CartId;
            string calendarJsonRequest = JsonConvert.SerializeObject(request), shopCSLCallDurations = string.Empty;
            United.Services.FlightShopping.Common.ShopResponse calendarResponse = null;
            if (_shoppingUtility.EnableFSRAlertMessages(shopRequest.Application.Id, shopRequest.Application.Version.Major, session.TravelType))
            {
                try
                {
                    if (!session.IsReshopChange && ("OW,RT".IndexOf(shopRequest.SearchType.ToString()) > -1))
                    {
                        var persistShopFlattenedFlightList = new MOBSHOPFlattenedFlightList();

                        persistShopFlattenedFlightList = await _sessionHelperService.GetSession<MOBSHOPFlattenedFlightList>(shopRequest.SessionId, persistShopFlattenedFlightList.ObjectName, new List<string> { shopRequest.SessionId, persistShopFlattenedFlightList.ObjectName }).ConfigureAwait(false);
                        var avail = await GetLastTripAvailabilityFromPersist(1, session.SessionId);

                        List<MOBSHOPFlattenedFlight> shopFlattenedFlightList = null;
                        if (persistShopFlattenedFlightList != null)
                        {
                            shopFlattenedFlightList = persistShopFlattenedFlightList.FlattenedFlightList;
                        }
                        else if (avail != null)
                        {
                            shopFlattenedFlightList = avail.Trip.FlattenedFlights;
                        }

                        if (session.ShopHasNonStop < 0)
                        {
                            session.ShopHasNonStop = shopFlattenedFlightList != null && shopFlattenedFlightList.Any(ff => ff.Flights.Count == 1) ? 1 : 0;
                            await _sessionHelperService.SaveSession<Session>(session, shopRequest.SessionId, new List<string> { shopRequest.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);
                        }

                        bool shouldCheckNonstop = !string.IsNullOrWhiteSpace(shopRequest.SearchType) && "OW,RT".IndexOf(shopRequest.SearchType) > -1 // only support OW/RT not MT
                                                    && avail != null && (avail.FSRAlertMessages == null || !avail.FSRAlertMessages.Any()) // no FSR alert was found in the shop call
                                                    && session.ShopHasNonStop == 0; // Only get nonstop alert if there are no nonstop

                        if (shouldCheckNonstop)
                        {
                            response.FSRAlertMessages = await GetShopFareWheelList_FilterNonStop(shopRequest, avail.Trip);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("GetShopFareWheelList - GetShopFareWheelList_FilterNonStop Error {@Exception}", JsonConvert.SerializeObject(ex));
                }
            }

            Stopwatch cslSelectTrip_SelectFareWheelWatch;
            cslSelectTrip_SelectFareWheelWatch = new Stopwatch();
            cslSelectTrip_SelectFareWheelWatch.Reset();
            cslSelectTrip_SelectFareWheelWatch.Start();

            if (_configuration.GetValue<bool>("EnableFSRMoneyPlusMilesFeature") && shopRequest.IsMoneyPlusMiles && session.IsEligibleForFSRMoneyPlusMiles)
            {
                calendarResponse = await _mileagePricingService.GetCSLMoneyAndMilesFareWheel<United.Services.FlightShopping.Common.ShopResponse>(session, shopRequest, request);
            }
            else
            {
                calendarResponse = _flightShoppingService.SelectFareWheel<United.Services.FlightShopping.Common.ShopResponse>(session.Token, "ShopFareWheel", shopRequest.SessionId, calendarJsonRequest).Result;
            }
            if (cslSelectTrip_SelectFareWheelWatch.IsRunning)
                {
                    cslSelectTrip_SelectFareWheelWatch.Stop();
                }
                shopCSLCallDurations = shopCSLCallDurations + "|FareWheelCallTime=" + cslSelectTrip_SelectFareWheelWatch.ElapsedMilliseconds.ToString();

                if (calendarResponse != null && calendarResponse.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && (calendarResponse.Errors == null || calendarResponse.Errors.Count == 0))
                {
                    shopCSLCallDurations = shopCSLCallDurations + "|ShopFareWheel=" + calendarResponse.CallTimeDomain;
                    response.FareWheel = await PopulateFareWheel(calendarResponse.FareWheelGrid, calendarResponse.LastBBXSolutionSetId);
                }
                else
                {
                    #region 89882 - CSL: ShopFareWheel : FareWheelOnly is not allowed and  FAREWHEEEL NOT FOUND - Ravi/Issuf
                    if (calendarResponse?.Errors != null && calendarResponse.Errors.Count > 0)
                    {
                        Model.Shopping.ShoppingResponse shop = new Model.Shopping.ShoppingResponse();
                        if (_shoppingUtility.IsNoFlightsSeasonalityFeatureEnabled(shopRequest.Application.Id, shopRequest.Application.Version.Major, session.CatalogItems))
                            shop = await _sessionHelperService.GetSession<ShoppingResponse>(shopRequest.SessionId, new ShoppingResponse().ObjectName, new List<string> { shopRequest.SessionId, new ShoppingResponse().ObjectName });
                        string errorMessage = string.Empty;
                        foreach (var error in calendarResponse.Errors)
                        {
                            errorMessage = errorMessage + " " + error.Message;
                            if (!string.IsNullOrEmpty(error.MinorCode))
                            {
                                if (_shoppingUtility.IsNoFlightsSeasonalityFeatureEnabled(shopRequest.Application.Id, shopRequest.Application.Version.Major, session.CatalogItems)
                                    && shop?.Response?.Availability?.NoFlightFoundMessage != null
                                     && calendarResponse.Warnings?.Any(a => a.MinorCode == _configuration.GetValue<string>("Seasonality201ErrorCode")) == false)
                                {
                                    var seasonalityTupleResponse = await GetSeasonalityFareWheelData(session, shopRequest);
                                    response.FareWheel = seasonalityTupleResponse.response;
                                    response.Messages = seasonalityTupleResponse.messages;
                                    if (response.FareWheel != null && response.FareWheel.Count > 0)
                                    {
                                        response.IsSeasonalFareWheel = true;
                                        return response;
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10035"))
                            {
                                throw new MOBUnitedException(error.Message);
                            }
                            else if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10038"))
                            {

                                throw new MOBUnitedException(error.Message);

                            }
                            else if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && (error.MinorCode.Trim().Equals("10051") || error.MinorCode.Trim().Equals("10052") || error.MinorCode.Trim().Equals("10056") || error.MinorCode.Trim().Equals("10069")))
                            {
                                throw new MOBUnitedException(error.Message);
                            }
                        }
                        throw new System.Exception(errorMessage);
                    }
                    #endregion
                }
                response.CallDurationText = shopCSLCallDurations;
            

            return response;
        }

        private async Task<ShopRequest> GetShopRequest(MOBSHOPShopRequest MOBShopShopRequest, bool isShopRequest, bool isSeasonality = false)
        {
            ShopRequest shopRequest = new ShopRequest
            {
                RememberedLoyaltyId = MOBShopShopRequest.MileagePlusAccountNumber,
                LoyaltyId = MOBShopShopRequest.MileagePlusAccountNumber,
                ChannelType = _configuration.GetValue<string>("Shopping - ChannelType"),
                AccessCode = _configuration.GetValue<string>("AccessCode - CSLShopping")
            };
            var isStandardRevenueSearch = IsStandardRevenueSearch(MOBShopShopRequest.IsCorporateBooking, MOBShopShopRequest.IsYoungAdultBooking,
                                                                  MOBShopShopRequest.AwardTravel, MOBShopShopRequest.EmployeeDiscountId,
                                                                  MOBShopShopRequest.TravelType, MOBShopShopRequest.IsReshop || MOBShopShopRequest.IsReshopChange,
                                                                  MOBShopShopRequest.FareClass, MOBShopShopRequest.PromotionCode);
            if (!MOBShopShopRequest.IsReshopChange && !MOBShopShopRequest.AwardTravel)
            {
                shopRequest.DisablePricingBySlice = _shoppingUtility.EnableRoundTripPricing(MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major);
            }
            if (_configuration.GetValue<bool>("EnableTripPlannerView") && MOBShopShopRequest.TravelType == MOBTripPlannerType.TPSearch.ToString() || MOBShopShopRequest.TravelType == MOBTripPlannerType.TPEdit.ToString())
            {
                if (shopRequest.Characteristics == null) shopRequest.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                shopRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "IsTripPlannerRequest", Value = "true" });
            }
            bool decodeOTP = false;
            bool.TryParse(_configuration.GetValue<string>("DecodesOnTimePerformance"), out decodeOTP);
            shopRequest.DecodesOnTimePerfRequested = decodeOTP;

            bool decodesRequested = false;
            bool.TryParse(_configuration.GetValue<string>("DecodesRequested"), out decodesRequested);
            shopRequest.DecodesRequested = decodesRequested;

            bool includeAmenities = false;
            if (!Convert.ToBoolean(_configuration.GetValue<string>("ByPassAmenities").ToString()))
            {
                bool.TryParse(_configuration.GetValue<string>("IncludeAmenities"), out includeAmenities);
            }
            shopRequest.IncludeAmenities = includeAmenities;
            var version1 = _configuration.GetValue<string>("FSRBasicEconomyToggleOnBookingMainAndroidversion");
            var version2 = _configuration.GetValue<string>("FSRBasicEconomyToggleOnBookingMainiOSversion");
            if (isSeasonality == false 
                && _configuration.GetValue<bool>("EnableFSRBasicEconomyToggleOnBookingMain") /*Master toggle to hide the be column */
                && GeneralHelper.IsApplicationVersionGreaterorEqual(MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, version1, version2) /*Version check for latest client changes which hardcoded IsELFFareDisplayAtFSR to true at Shop By Map*/
                && _shoppingUtility.CheckFSRRedesignFromShop(MOBShopShopRequest)/*check for FSR resdesign experiment ON Builds*/ )
            {
                shopRequest.DisableMostRestrictive = !MOBShopShopRequest.IsELFFareDisplayAtFSR;
            }

            shopRequest.SessionId = MOBShopShopRequest.SessionId;
            shopRequest.CountryCode = MOBShopShopRequest.CountryCode;
            shopRequest.SimpleSearch = true;
            // Refundable fares toggle feature
            if (IsEnableRefundableFaresToggle(MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major) &&
                isStandardRevenueSearch &&
                (isShopRequest ||
                 (MOBShopShopRequest.Trips[0].SearchFiltersIn?.RefundableFaresToggle?.IsSelected ?? false) ||
                 (MOBShopShopRequest.Trips[0].SearchFiltersIn?.RefundableFaresToggle == null && MOBShopShopRequest.FareType == "urf")))
            {
                if (isSeasonality == false)
                    shopRequest.FareType = _configuration.GetValue<string>("RefundableFaresToggleFareType");

                if (shopRequest.Characteristics == null) shopRequest.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                shopRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "filterflightsbytoggle", Value = "true" });
            }
            // Mixed Cabin toggle feature
            if (IsMixedCabinFilerEnabled(MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major) &&
                MOBShopShopRequest.AwardTravel &&
                (isShopRequest ||
                 MOBShopShopRequest?.Trips[0]?.SearchFiltersIn == null || (MOBShopShopRequest?.Trips[0]?.SearchFiltersIn?.AdditionalToggles?.Where(a => a.Key == _configuration.GetValue<string>("MixedCabinToggleKey")) == null) ||
                 (MOBShopShopRequest?.Trips[0]?.SearchFiltersIn?.AdditionalToggles?.FirstOrDefault(a => a.Key == _configuration.GetValue<string>("MixedCabinToggleKey"))?.IsSelected ?? false)))
            {
                shopRequest.FareType = _configuration.GetValue<string>("MixedCabinToggle");

                if (shopRequest.Characteristics == null) shopRequest.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                shopRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "filterflightsbytoggle", Value = "true" });
            }
            //shopRequest.FareFamilies = true;
            if (isShopRequest)
            {
                shopRequest.FlexibleDaysBefore = _configuration.GetValue<int>("ShopFareWheelFlexibleDaysBefore"); //getFlexibleDaysBefore();
                shopRequest.FlexibleDaysAfter = _configuration.GetValue<int>("ShopFareWheelFlexibleDaysAfter");   //getFlexibleDaysAfter();
            }
            else
            {
                shopRequest.FlexibleDaysBefore = getFlexibleDaysBefore();
                shopRequest.FlexibleDaysAfter = getFlexibleDaysAfter();
            }
            shopRequest.FareCalendar = false;
            //to prep for REST filtering, hardcodeing max trips
            shopRequest.MaxTrips = getShoppingSearchMaxTrips();
            shopRequest.PageIndex = 1;
            shopRequest.PageSize = _configuration.GetValue<int>("ShopAndSelectTripCSLRequestPageSize");
            ///172651 : mApp FSR Booking: Flight Results are disappearing  from the FSR screen when user try to Filter the Flights.
            ///184707 : Booking FSR mApp: Wrong flights count is displaying when we tap on Done button in the Airport filter screen
            ///Srini 11/22/2017
            if (_configuration.GetValue<bool>("BugFixToggleFor17M"))
            {
                if (MOBShopShopRequest.GetFlightsWithStops)
                {
                    MOBSHOPAvailability nonStopsAvailability = await GetLastTripAvailabilityFromPersist(1, MOBShopShopRequest.SessionId);
                    shopRequest.PageSize = shopRequest.PageSize - nonStopsAvailability.Trip.FlightCount;
                }
            }
            shopRequest.DepartDateTime = MOBShopShopRequest.Trips[0].DepartDate;
            shopRequest.RecentSearchKey = MOBShopShopRequest.Trips[0].Origin + MOBShopShopRequest.Trips[0].Destination + MOBShopShopRequest.Trips[0].DepartDate;

            shopRequest.Origin = MOBShopShopRequest.Trips[0].Origin;
            shopRequest.Destination = MOBShopShopRequest.Trips[0].Destination;

            shopRequest.PromoCode = MOBShopShopRequest.PromotionCode;
            shopRequest.BookingCodesSpecified = MOBShopShopRequest.FareClass;

            if (MOBShopShopRequest.IsReshopChange)
            {
                var tupleResponse = await GetReshopTripsList(MOBShopShopRequest);
                shopRequest.Trips = tupleResponse.tripsList;
                shopRequest.CabinPreferenceMain = GetCabinPreference(shopRequest.Trips[0].CabinType);

                if (_configuration.GetValue<bool>("EnableReshopOverride24HrFlex") && tupleResponse.isOverride24HrFlex)
                {
                    shopRequest.Characteristics = (shopRequest.Characteristics == null)
                        ? new Collection<United.Service.Presentation.CommonModel.Characteristic>() : shopRequest.Characteristics;
                    shopRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic { Code = "Ignore24HrFlex", Value = "true" });
                }
            }
            else
            {
                shopRequest.Trips = new List<United.Services.FlightShopping.Common.Trip>();
                United.Services.FlightShopping.Common.Trip trip = GetTrip(MOBShopShopRequest.Trips[0].Origin, MOBShopShopRequest.Trips[0].Destination, MOBShopShopRequest.Trips[0].DepartDate, MOBShopShopRequest.Trips[0].Cabin, MOBShopShopRequest.Trips[0].UseFilters, MOBShopShopRequest.Trips[0].SearchFiltersIn, MOBShopShopRequest.Trips[0].SearchNearbyOriginAirports, MOBShopShopRequest.Trips[0].SearchNearbyDestinationAirports,
                                    MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, isStandardRevenueSearch, MOBShopShopRequest.IsELFFareDisplayAtFSR, MOBShopShopRequest.FareType,
                                    false, MOBShopShopRequest.Trips[0].OriginAllAirports, MOBShopShopRequest.Trips[0].DestinationAllAirports);
                if (trip == null)
                {
                    throw new MOBUnitedException("You must specify at least one trip.");
                }
                trip.TripIndex = 1;
                if (trip.SearchFiltersIn == null)
                {
                    trip.SearchFiltersIn = new SearchFilterInfo();
                }
                trip.SearchFiltersIn.FareFamily = GetFareFamily(MOBShopShopRequest.Trips[0].Cabin, MOBShopShopRequest.FareType);
                if (await _featureSettings.GetFeatureSettingValue("EnableFSRETCTravelCreditsFeature").ConfigureAwait(false) 
                    && MOBShopShopRequest.PricingType == PricingType.ETC.ToString())
                {
                    trip.SearchFiltersIn.ShopIndicators.IsTravelCreditsApplied = true;
                }
                shopRequest.Trips.Add(trip);
                shopRequest.CabinPreferenceMain = GetCabinPreference(trip.CabinType);

                if (MOBShopShopRequest.Trips.Count > 1)
                {
                    trip = GetTrip(MOBShopShopRequest.Trips[1].Origin, MOBShopShopRequest.Trips[1].Destination, MOBShopShopRequest.Trips[1].DepartDate, MOBShopShopRequest.Trips[1].Cabin, MOBShopShopRequest.Trips[1].UseFilters, MOBShopShopRequest.Trips[1].SearchFiltersIn, MOBShopShopRequest.Trips[1].SearchNearbyOriginAirports, MOBShopShopRequest.Trips[1].SearchNearbyDestinationAirports, MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, isStandardRevenueSearch, MOBShopShopRequest.IsELFFareDisplayAtFSR, MOBShopShopRequest.FareType,
                                    false, MOBShopShopRequest.Trips[1].OriginAllAirports, MOBShopShopRequest.Trips[1].DestinationAllAirports);
                    if (trip != null)
                    {
                        trip.TripIndex = 2;
                        if (trip.SearchFiltersIn == null)
                        {
                            trip.SearchFiltersIn = new SearchFilterInfo();
                        }
                        trip.SearchFiltersIn.FareFamily = GetFareFamily(MOBShopShopRequest.Trips[1].Cabin, MOBShopShopRequest.FareType);
                        shopRequest.Trips.Add(trip);
                    }
                }
                if (MOBShopShopRequest.Trips.Count > 2)
                {
                    trip = GetTrip(MOBShopShopRequest.Trips[2].Origin, MOBShopShopRequest.Trips[2].Destination, MOBShopShopRequest.Trips[2].DepartDate, MOBShopShopRequest.Trips[2].Cabin, MOBShopShopRequest.Trips[2].UseFilters, MOBShopShopRequest.Trips[2].SearchFiltersIn, MOBShopShopRequest.Trips[2].SearchNearbyOriginAirports, MOBShopShopRequest.Trips[2].SearchNearbyDestinationAirports,
                                      MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, isStandardRevenueSearch, MOBShopShopRequest.IsELFFareDisplayAtFSR, MOBShopShopRequest.FareType,
                                      false, MOBShopShopRequest.Trips[2].OriginAllAirports, MOBShopShopRequest.Trips[2].DestinationAllAirports);
                    if (trip != null)
                    {
                        trip.TripIndex = 3;
                        if (trip.SearchFiltersIn == null)
                        {
                            trip.SearchFiltersIn = new SearchFilterInfo();
                        }
                        trip.SearchFiltersIn.FareFamily = GetFareFamily(MOBShopShopRequest.Trips[2].Cabin, MOBShopShopRequest.FareType);
                        shopRequest.Trips.Add(trip);
                    }
                }
                if (MOBShopShopRequest.Trips.Count > 3)
                {
                    trip = GetTrip(MOBShopShopRequest.Trips[3].Origin, MOBShopShopRequest.Trips[3].Destination, MOBShopShopRequest.Trips[3].DepartDate, MOBShopShopRequest.Trips[3].Cabin, MOBShopShopRequest.Trips[3].UseFilters, MOBShopShopRequest.Trips[3].SearchFiltersIn, MOBShopShopRequest.Trips[3].SearchNearbyOriginAirports, MOBShopShopRequest.Trips[3].SearchNearbyDestinationAirports,
                                    MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, isStandardRevenueSearch, MOBShopShopRequest.IsELFFareDisplayAtFSR, MOBShopShopRequest.FareType,
                                    false, MOBShopShopRequest.Trips[3].OriginAllAirports, MOBShopShopRequest.Trips[3].DestinationAllAirports);
                    if (trip != null)
                    {
                        trip.TripIndex = 4;
                        if (trip.SearchFiltersIn == null)
                        {
                            trip.SearchFiltersIn = new SearchFilterInfo();
                        }
                        trip.SearchFiltersIn.FareFamily = GetFareFamily(MOBShopShopRequest.Trips[3].Cabin, MOBShopShopRequest.FareType);
                        shopRequest.Trips.Add(trip);
                    }
                }
                if (MOBShopShopRequest.Trips.Count > 4)
                {
                    trip = GetTrip(MOBShopShopRequest.Trips[4].Origin, MOBShopShopRequest.Trips[4].Destination, MOBShopShopRequest.Trips[4].DepartDate, MOBShopShopRequest.Trips[4].Cabin, MOBShopShopRequest.Trips[4].UseFilters, MOBShopShopRequest.Trips[4].SearchFiltersIn, MOBShopShopRequest.Trips[4].SearchNearbyOriginAirports, MOBShopShopRequest.Trips[4].SearchNearbyDestinationAirports,
                                       MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, isStandardRevenueSearch, MOBShopShopRequest.IsELFFareDisplayAtFSR, MOBShopShopRequest.FareType,
                                       false, MOBShopShopRequest.Trips[4].OriginAllAirports, MOBShopShopRequest.Trips[4].DestinationAllAirports);
                    if (trip != null)
                    {
                        trip.TripIndex = 5;
                        if (trip.SearchFiltersIn == null)
                        {
                            trip.SearchFiltersIn = new SearchFilterInfo();
                        }
                        trip.SearchFiltersIn.FareFamily = GetFareFamily(MOBShopShopRequest.Trips[4].Cabin, MOBShopShopRequest.FareType);
                        shopRequest.Trips.Add(trip);
                    }
                }
                if (MOBShopShopRequest.Trips.Count > 5)
                {
                    trip = GetTrip(MOBShopShopRequest.Trips[5].Origin, MOBShopShopRequest.Trips[5].Destination, MOBShopShopRequest.Trips[5].DepartDate, MOBShopShopRequest.Trips[5].Cabin, MOBShopShopRequest.Trips[5].UseFilters, MOBShopShopRequest.Trips[5].SearchFiltersIn, MOBShopShopRequest.Trips[5].SearchNearbyOriginAirports, MOBShopShopRequest.Trips[5].SearchNearbyDestinationAirports,
                                    MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, isStandardRevenueSearch, MOBShopShopRequest.IsELFFareDisplayAtFSR, MOBShopShopRequest.FareType,
                                    false, MOBShopShopRequest.Trips[5].OriginAllAirports, MOBShopShopRequest.Trips[5].DestinationAllAirports);
                    if (trip != null)
                    {
                        trip.TripIndex = 6;
                        if (trip.SearchFiltersIn == null)
                        {
                            trip.SearchFiltersIn = new SearchFilterInfo();
                        }
                        trip.SearchFiltersIn.FareFamily = GetFareFamily(MOBShopShopRequest.Trips[5].Cabin, MOBShopShopRequest.FareType);
                        shopRequest.Trips.Add(trip);
                    }
                }
            }

            if ((!MOBShopShopRequest.IsReshopChange || MOBShopShopRequest.ReshopTravelers == null || MOBShopShopRequest.ReshopTravelers.Count == 0))
            {

                shopRequest.PaxInfoList = new List<PaxInfo>();

                PaxInfo paxInfo = null;
                if (_shoppingUtility.EnableYoungAdult(MOBShopShopRequest.IsReshop) && MOBShopShopRequest.IsYoungAdultBooking)
                {
                    shopRequest.PaxInfoList.Add(GetYAPaxInfo());
                }
                else
                {
                    if (_shoppingUtility.EnableTravelerTypes(MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major) && MOBShopShopRequest.TravelerTypes != null && MOBShopShopRequest.TravelerTypes.Count > 0)
                    {
                        GetPaxInfo(MOBShopShopRequest, shopRequest);
                    }
                    else
                    {
                        if ((MOBShopShopRequest.NumberOfAdults > 0 || MOBShopShopRequest.NumberOfSeniors > 0 || MOBShopShopRequest.NumberOfChildren5To11 > 0 || MOBShopShopRequest.NumberOfChildren12To17 > 0))
                        {
                            for (int i = 0; i < MOBShopShopRequest.NumberOfAdults; i++)
                            {
                                paxInfo = new PaxInfo
                                {
                                    PaxType = PaxType.Adult,
                                    DateOfBirth = DateTime.Today.AddYears(-20).ToShortDateString()
                                };
                                AssignPTCValue(MOBShopShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < MOBShopShopRequest.NumberOfSeniors; i++)
                            {
                                paxInfo = new PaxInfo
                                {
                                    PaxType = PaxType.Senior,
                                    DateOfBirth = DateTime.Today.AddYears(-67).ToShortDateString()
                                };
                                AssignPTCValue(MOBShopShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < MOBShopShopRequest.NumberOfChildren2To4; i++)
                            {
                                paxInfo = new PaxInfo
                                {
                                    PaxType = PaxType.Child01,
                                    DateOfBirth = DateTime.Today.AddYears(-3).ToShortDateString()
                                };
                                AssignPTCValue(MOBShopShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < MOBShopShopRequest.NumberOfChildren5To11; i++)
                            {
                                paxInfo = new PaxInfo
                                {
                                    PaxType = PaxType.Child02,
                                    DateOfBirth = DateTime.Today.AddYears(-8).ToShortDateString()
                                };
                                AssignPTCValue(MOBShopShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < MOBShopShopRequest.NumberOfChildren12To17; i++)
                            {
                                paxInfo = new PaxInfo
                                {
                                    PaxType = PaxType.Child03,
                                    DateOfBirth = DateTime.Today.AddYears(-15).ToShortDateString()
                                };
                                AssignPTCValue(MOBShopShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < MOBShopShopRequest.NumberOfInfantOnLap; i++)
                            {
                                paxInfo = new PaxInfo
                                {
                                    PaxType = PaxType.InfantLap,
                                    DateOfBirth = DateTime.Today.AddYears(-1).ToShortDateString()
                                };
                                AssignPTCValue(MOBShopShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < MOBShopShopRequest.NumberOfInfantWithSeat; i++)
                            {
                                paxInfo = new PaxInfo
                                {
                                    PaxType = PaxType.InfantSeat,
                                    DateOfBirth = DateTime.Today.AddYears(-1).ToShortDateString()
                                };
                                AssignPTCValue(MOBShopShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);

                            }
                        }
                    }
                }
            }
            else if (MOBShopShopRequest.IsReshopChange)
            {
                var cslReservation = await _sessionHelperService.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(MOBShopShopRequest.SessionId, new ReservationDetail().GetType().FullName, new List<string> { shopRequest.SessionId, new ReservationDetail().GetType().FullName });
                shopRequest.PaxInfoList = GetReshopPaxInfoList(MOBShopShopRequest, cslReservation);
            }
            else
            {
                throw new MOBUnitedException("You must specify at least one passenger.");
            }

            shopRequest.AwardTravel = MOBShopShopRequest.AwardTravel;
            shopRequest.SearchType = AvailabilitySearchType.ValueNotSet;
            shopRequest.SearchTypeSelection = GetSearchTypeSelection(MOBShopShopRequest.SearchType);
            shopRequest.ServiceType = United.Services.FlightShopping.Common.ServiceType.Boombox;
            shopRequest.Stops = MOBShopShopRequest.MaxNumberOfStops;
            shopRequest.StopsInclusive = true;
            shopRequest.ChannelType = "MOBILE";
            shopRequest.EliteLevel = MOBShopShopRequest.PremierStatusLevel;
            shopRequest.SortType = MOBShopShopRequest.ResultSortType;
            //get account summary for mileage info
            try
            {
                string getAccountSummaryTransactionID = MOBShopShopRequest.TransactionId;
                if ((MOBShopShopRequest.AwardTravel || EnableEPlusAncillary(MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, MOBShopShopRequest.IsReshopChange)) && !string.IsNullOrWhiteSpace(MOBShopShopRequest.MileagePlusAccountNumber))
                {
                    if (_configuration.GetValue<string>("AddingExtraLogggingForAwardShopAccountSummary_Toggle") != null
                        && (_configuration.GetValue<bool>("AddingExtraLogggingForAwardShopAccountSummary_Toggle")))
                    {
                        //    logEntries.Add(LogEntry.GetLogEntry<string>(MOBShopShopRequest.SessionId, "MOBShopShopRequest - TransactionId", "TransactionId", MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, MOBShopShopRequest.DeviceId, MOBShopShopRequest.TransactionId));
                        if (!string.IsNullOrWhiteSpace(MOBShopShopRequest.SessionId))
                        {
                            getAccountSummaryTransactionID = MOBShopShopRequest.SessionId;
                        }
                    }
                    MPAccountSummary summary = null;
                    summary = await _mileagePlus.GetAccountSummary(getAccountSummaryTransactionID, MOBShopShopRequest.MileagePlusAccountNumber, "en-US", false, MOBShopShopRequest.SessionId);
                    shopRequest.LoyaltyPerson = new Service.Presentation.PersonModel.LoyaltyPerson
                    {
                        LoyaltyProgramMemberID = summary.MileagePlusNumber,
                        LoyaltyProgramMemberTierLevel = (Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel)summary.EliteStatus.Level,
                        AccountBalances = new Collection<Service.Presentation.CommonModel.LoyaltyAccountBalance>()
                    };
                    Service.Presentation.CommonModel.LoyaltyAccountBalance balance = new Service.Presentation.CommonModel.LoyaltyAccountBalance();
                    int bal = 0;
                    int.TryParse(summary.Balance, out bal);
                    balance.Balance = bal;
                    balance.BalanceType = Service.Presentation.CommonEnumModel.LoyaltyAccountBalanceType.MilesBalance;
                    shopRequest.LoyaltyPerson.AccountBalances.Add(balance);
                }
                else if (MOBShopShopRequest.AwardTravel && string.IsNullOrWhiteSpace(MOBShopShopRequest.MileagePlusAccountNumber))
                {
                    if (_configuration.GetValue<string>("AddingExtraLogggingForAwardShopAccountSummary_Toggle") != null
                       && (_configuration.GetValue<bool>("AddingExtraLogggingForAwardShopAccountSummary_Toggle")))
                    {
                        _logger.LogError("GetShopRequest-AwardTravel_EmptyMpNumber {@Request}", JsonConvert.SerializeObject(MOBShopShopRequest));
                        //    logEntries.Add(LogEntry.GetLogEntry<MOBSHOPShopRequest>(MOBShopShopRequest.SessionId, "GetShopRequest-AwardTravel_EmptyMpNumber", "Exception", MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, MOBShopShopRequest.DeviceId, MOBShopShopRequest, true, false));
                    }
                }
            }
            catch (Exception ex)
            {
                if (_configuration.GetValue<string>("AddingExtraLogggingForAwardShopAccountSummary_Toggle") != null
                     && _configuration.GetValue<bool>("AddingExtraLogggingForAwardShopAccountSummary_Toggle"))
                {
                    _logger.LogError("GetShopRequest - GetAccountSummary {@Exception}", JsonConvert.SerializeObject(ex));
                    //    logEntries.Add(LogEntry.GetLogEntry<string>(MOBShopShopRequest.SessionId, "GetShopRequest - GetAccountSummary", "Exception", MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, MOBShopShopRequest.DeviceId, ex.Message));
                }
            };
            //if (_configuration.GetValue<string>("AddingExtraLogggingForAwardShopAccountSummary_Toggle") != null
            //&& (_configuration.GetValue<bool>("AddingExtraLogggingForAwardShopAccountSummary_Toggle")
            //&& mp != null && mp.LogEntries != null))
            //{
            //   // logEntries.AddRange(mp.LogEntries);
            //}

            if (!string.IsNullOrEmpty(MOBShopShopRequest.EmployeeDiscountId))
            {
                shopRequest.EmployeeDiscountId = MOBShopShopRequest.EmployeeDiscountId;
            }
            shopRequest.NGRP = _configuration.GetValue<string>("NGRPSwitchONOFFValue") != null ? _configuration.GetValue<bool>("NGRPSwitchONOFFValue") : false;
            #region
            AssignCalendarLengthOfStay(MOBShopShopRequest.LengthOfCalendar, shopRequest);
            #endregion
            //persis CSL shop request so we nave Loyalty info without making multiple summary calls
            CSLShopRequest cslShopRequest = new CSLShopRequest();
            if (MOBShopShopRequest.IsReshopChange)
            {
                shopRequest.ConfirmationID = MOBShopShopRequest.RecordLocator;
                shopRequest.DisableMostRestrictive = false;
                var cslReservation = await _sessionHelperService.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(shopRequest.SessionId, new ReservationDetail().GetType().FullName, new List<string> { shopRequest.SessionId, new ReservationDetail().GetType().FullName });
                if (cslReservation != null && cslReservation.Detail != null)
                {
                    shopRequest.reservation = cslReservation.Detail;
                }

                string riskFreePolicy24Hr = GetCharacteristicDescription(cslReservation?.Detail.Characteristic.ToList(), "24HrFlexibleBookingPolicy");
                if (!string.IsNullOrEmpty(riskFreePolicy24Hr))
                {
                    shopRequest.RiskFreePolicy = riskFreePolicy24Hr;
                }
            }
            cslShopRequest.ShopRequest = shopRequest;
            await _sessionHelperService.SaveSession<CSLShopRequest>(cslShopRequest, shopRequest.SessionId, new List<string> { shopRequest.SessionId, cslShopRequest.ObjectName }, cslShopRequest.ObjectName, 30000);
            #region Corporate Booking
            bool isCorporateBooking = _configuration.GetValue<bool>("CorporateConcurBooking");
            if ((isCorporateBooking && MOBShopShopRequest.IsCorporateBooking) || MOBShopShopRequest.TravelType == TravelType.CLB.ToString())
            {
                shopRequest.CorporateTravelProvider = MOBShopShopRequest.MOBCPCorporateDetails.CorporateTravelProvider;
                shopRequest.CorporationName = MOBShopShopRequest.MOBCPCorporateDetails.CorporateCompanyName;
                shopRequest.SpecialPricingInfo = new United.Services.FlightShopping.Common.SpecialPricing.SpecialPricingInfo();
                //TODO:Review with rajesh ..faregroupID is required for corporate lesiure
                if (_configuration.GetValue<bool>("EnableCorporateLeisure"))
                {
                    if (!string.IsNullOrEmpty(MOBShopShopRequest.MOBCPCorporateDetails?.DiscountCode) && MOBShopShopRequest.TravelType != TravelType.CLB.ToString())
                    {
                        shopRequest.SpecialPricingInfo.AccountCode = MOBShopShopRequest.MOBCPCorporateDetails.DiscountCode;
                        shopRequest.SpecialPricingInfo.PromoType = PromoType.CorporateDiscount;
                    }
                    else if (!string.IsNullOrEmpty(MOBShopShopRequest.MOBCPCorporateDetails?.LeisureDiscountCode) && MOBShopShopRequest.TravelType == TravelType.CLB.ToString())
                    {
                        shopRequest.SpecialPricingInfo.AccountCode = MOBShopShopRequest.MOBCPCorporateDetails.LeisureDiscountCode;
                        shopRequest.SpecialPricingInfo.PromoType = PromoType.CorporateDiscountLeisure;
                    }
                }
                else
                {
                    shopRequest.SpecialPricingInfo.AccountCode = MOBShopShopRequest.MOBCPCorporateDetails.DiscountCode;
                    shopRequest.SpecialPricingInfo.PromoType = PromoType.CorporateDiscount;
                }
                shopRequest.SpecialPricingInfo.FareGroupID = MOBShopShopRequest.MOBCPCorporateDetails.FareGroupId;// Passed from the GetProfile  call; XBE - Xclude BE; Null means include BE
                                                                                                                  // This Value is 10 to get CORPDISC "Corporate Discount Fare"
            }
            #endregion

            if (_configuration.GetValue<bool>("EnableNonStopFlight") && (MOBShopShopRequest.GetNonStopFlightsOnly || MOBShopShopRequest.GetFlightsWithStops))
            {
                await RequestForNonStopFlights(MOBShopShopRequest, shopRequest);
                if (MOBShopShopRequest.GetFlightsWithStops)
                {
                    Session session = new Session();
                    session = await _sessionHelperService.GetSession<Session>(shopRequest.SessionId, session.ObjectName, new List<string> { shopRequest.SessionId, session.ObjectName });
                    if (session == null)
                    {
                        throw new MOBUnitedException("Your session has expired. Please start a new search.");
                    }
                    shopRequest.CartId = session.CartId;
                }
            }
            return shopRequest;
        }

        private async Task<List<MOBFSRAlertMessage>> GetShopFareWheelList_FilterNonStop(MOBSHOPShopRequest shopRequest, MOBSHOPTrip trip)
        {
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(shopRequest.SessionId, session.ObjectName, new List<string> { shopRequest.SessionId, session.ObjectName });
            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }
            List<MOBFSRAlertMessage> _FSRAlertMessages = null;
            FareWheelResponse response = new FareWheelResponse();
            ShopRequest request = await GetShopRequest(shopRequest, false);// Use filters need to update here

            //---------------Non-Stop Filters-----------
            //request.Trips[0].NonStopMarket = true;
            request.Trips[0].SearchFiltersIn.StopCountMax = 0;
            //request.Trips[0].SearchFiltersOut.StopCountMax = 0;
            request.Trips[0].UseFilters = true;
            //---------------
            request.DisableMostRestrictive = !shopRequest.IsELFFareDisplayAtFSR;

            request.CartId = session.CartId;
            string calendarJsonRequest = JsonConvert.SerializeObject(request), shopCSLCallDurations = string.Empty;
            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cslSelectTrip_SelectFareWheelWatch;
            cslSelectTrip_SelectFareWheelWatch = new Stopwatch();
            cslSelectTrip_SelectFareWheelWatch.Reset();
            cslSelectTrip_SelectFareWheelWatch.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******
            United.Services.FlightShopping.Common.ShopResponse calendarResponse;
            if (_configuration.GetValue<bool>("EnableFSRMoneyPlusMilesFeature") && shopRequest.IsMoneyPlusMiles)
            {
                calendarResponse = await _mileagePricingService.GetCSLMoneyAndMilesFareWheel<United.Services.FlightShopping.Common.ShopResponse>(session, shopRequest, request);
            }
            else
            {
                calendarResponse = await _flightShoppingService.SelectFareWheel<United.Services.FlightShopping.Common.ShopResponse>(session.Token, "ShopFareWheel", shopRequest.SessionId, calendarJsonRequest);
            }
            #region// 3 = cslSelectTrip_SelectFareWheelWatch//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cslSelectTrip_SelectFareWheelWatch.IsRunning)
            {
                cslSelectTrip_SelectFareWheelWatch.Stop();
            }
            shopCSLCallDurations = shopCSLCallDurations + "|FareWheelCallTime=" + cslSelectTrip_SelectFareWheelWatch.ElapsedMilliseconds.ToString();

            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******
            shopCSLCallDurations = shopCSLCallDurations + "|ShopFareWheel_FilterNonStop=" + calendarResponse.CallTimeDomain;
            if (calendarResponse != null && calendarResponse.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && (calendarResponse.Errors == null || calendarResponse.Errors.Count == 0))
            {
                response.FareWheel = await PopulateFareWheel(calendarResponse.FareWheelGrid, calendarResponse.LastBBXSolutionSetId);

                ///--------------- Validate Non-Stop Rules-------------
               
                string OriginDesc = string.Empty;
                string DestinationDesc = string.Empty;

                if (_configuration.GetValue<bool>("EnableFecthAirportNameFromCSL"))
                {                        
                    OriginDesc = trip?.OriginDecoded;
                    DestinationDesc = trip?.DestinationDecoded;                              
                }
                else
                {
                    var airportList = await GetAiportListDescFromPersist(session.SessionId);
                    OriginDesc = airportList.AirportsList.Where(i => i.AirportCode == shopRequest.Trips[0].Origin.ToUpper()).Select(i => i.AirportNameMobile).FirstOrDefault();
                    DestinationDesc = airportList.AirportsList.Where(i => i.AirportCode == shopRequest.Trips[0].Destination.ToUpper()).Select(i => i.AirportNameMobile).FirstOrDefault();
                }

                List<CMSContentMessage> lstMessages = null;
                MOBMobileCMSContentMessages messages = null;
                if (_configuration.GetValue<bool>("FSRNonstopSuggestFutureDateInfoDisplayed")) 
                {
                    lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(shopRequest, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                    messages = _shoppingUtility.GetSDLMessageFromList(lstMessages, _configuration.GetValue<string>("FSRNonstopSuggestFutureDateContent")).FirstOrDefault(); ; 
                }

                var FSRNonStopSuggestObj = new List<IRule<MOBFSRAlertMessage>>
                    {
                        // Add all enhancements rule here
                       new FSRNonstopSuggestFutureDate(response.FareWheel, OriginDesc, DestinationDesc, shopRequest.Trips[0].DepartDate, shopRequest, 0,_configuration, messages)

                    };

                // Get the first enhancement based on given priority
                var firstAlert = FSRNonStopSuggestObj.FirstOrDefault(rule => rule.ShouldExecuteRule());

                if (firstAlert != null)
                {
                    _FSRAlertMessages = new List<MOBFSRAlertMessage> {  await firstAlert.Execute() };
                }
            }
            try
            {
                response.CallDurationText = shopCSLCallDurations;
            }
            catch { }
            return _FSRAlertMessages;
        }

        private async Task<(List<United.Services.FlightShopping.Common.Trip> tripsList, bool isOverride24HrFlex)> GetReshopTripsList(MOBSHOPShopRequest mobShopShopRequest)
        {
            var cslReservation = await _sessionHelperService.GetSession<ReservationDetail>(mobShopShopRequest.SessionId, new ReservationDetail().GetType().FullName, new List<string> { mobShopShopRequest.SessionId, new ReservationDetail().GetType().FullName }).ConfigureAwait(false);
            var persistedReservation = await _sessionHelperService.GetSession<Reservation>(mobShopShopRequest.SessionId, (new Reservation()).ObjectName, new List<string> { mobShopShopRequest.SessionId, (new Reservation()).ObjectName }).ConfigureAwait(false);

            List<United.Services.FlightShopping.Common.Trip> tripsList = new List<United.Services.FlightShopping.Common.Trip>();
            int segementNumber = 0;
            bool isOverride24HrFlex;
            isOverride24HrFlex = (persistedReservation == null) ? false : persistedReservation.Override24HrFlex;

            if (persistedReservation.Reshop.IsUsedPNR)
            {
                List<MOBSHOPTripBase> usedTripBase = new List<MOBSHOPTripBase>();
                List<MOBSHOPTripBase> requestTripBase = mobShopShopRequest.Trips;
                foreach (var trip in persistedReservation.ReshopUsedTrips)
                {
                    MOBSHOPTripBase tripbase = ConvertReshopTripToTripBase(trip);
                    usedTripBase.Add(tripbase);
                }
                int tripRequestIndex = 0;
                foreach (var tripRequest in mobShopShopRequest.Trips)
                {
                    usedTripBase.Add(tripRequest);
                    var mOBSHOPReShopTrip = persistedReservation.ReshopTrips[tripRequestIndex];
                    mOBSHOPReShopTrip.IsReshopTrip = (tripRequest.ChangeType == 0);
                    mOBSHOPReShopTrip.ChangeTripTitle = (_configuration.GetValue<string>("ReshopChange-RTIFlightBlockTitle") ?? "");
                    tripRequestIndex++;
                }
                requestTripBase = usedTripBase;
                int requestTripIndex = Convert.ToInt32(cslReservation.Detail.FlightSegments.Min(p => p.TripNumber));

                foreach (var requestTrip in requestTripBase)
                {
                    tripsList.Add(GetTripForReshopChangeRequestUsingMobRequestAndCslReservationSegments(requestTrip, segementNumber, mobShopShopRequest.FareType, cslReservation.Detail.FlightSegments.ToList(), persistedReservation.Reshop.IsUsedPNR, requestTripIndex));
                    segementNumber++;
                    requestTripIndex++;
                }
                await _sessionHelperService.SaveSession<Reservation>(persistedReservation, mobShopShopRequest.SessionId, new List<string> { mobShopShopRequest.SessionId, persistedReservation.ObjectName }, persistedReservation.ObjectName).ConfigureAwait(false);
                return (tripsList,isOverride24HrFlex);
            }

            foreach (var requestTrip in mobShopShopRequest.Trips)
            {
                tripsList.Add(GetTripForReshopChangeRequestUsingMobRequestAndCslReservationSegments(requestTrip, segementNumber, mobShopShopRequest.FareType, cslReservation.Detail.FlightSegments.ToList()));
                segementNumber++;

                if (persistedReservation.ReshopTrips.Count >= tripsList.Count)
                {
                    var mOBSHOPReShopTrip = persistedReservation.ReshopTrips[tripsList.Count - 1];
                    mOBSHOPReShopTrip.IsReshopTrip = (tripsList[tripsList.Count - 1].Flights == null || tripsList[tripsList.Count - 1].Flights.Count == 0);
                    mOBSHOPReShopTrip.ChangeTripTitle = (_configuration.GetValue<string>("ReshopChange-RTIFlightBlockTitle") ?? "");
                }
            }

           await _sessionHelperService.SaveSession<Reservation>(persistedReservation, mobShopShopRequest.SessionId, new List<string> { mobShopShopRequest.SessionId, persistedReservation.ObjectName }, persistedReservation.ObjectName).ConfigureAwait(false);

            return (tripsList, isOverride24HrFlex);
        }

        private string GetCabinPreference(CabinType cabinType)
        {
            string ct = string.Empty;

            //add premium cabin string to align columns to shopping products
            if (cabinType == CabinType.Business ||
                cabinType == CabinType.BusinessFirst ||
                cabinType == CabinType.First)
            {
                ct = "premium";
            }

            return ct;
        }

        private United.Services.FlightShopping.Common.Trip GetTrip(string origin, string destination, string departureDate, string cabin, bool useFilters, MOBSearchFilters filters, bool searchNearbyOrigins, bool searchNearbyDestinations, int appId = -1, string appVersion = "", bool isStandardRevenueSearch = false, bool isELFFareDisplayAtFSR = false, string fareType = "", bool isUsed = false, int originAllAirports = -1, int destinationAllAirports = -1)
        {
            United.Services.FlightShopping.Common.Trip trip = null;

            if (!string.IsNullOrEmpty(origin) && !string.IsNullOrEmpty(destination) && origin.Length == 3 && destination.Length == 3 && IsValidDate(departureDate, true, isUsed))
            {
                trip = new United.Services.FlightShopping.Common.Trip
                {
                    Origin = origin
                };

                // MB-2639 add all airports flag to csl shop call
                if (_configuration.GetValue<bool>("EnableAllAirportsFlightSearch") && originAllAirports != -1 && destinationAllAirports != -1)
                {
                    trip.OriginAllAirports = originAllAirports == 1 ? true : false;
                    trip.DestinationAllAirports = destinationAllAirports == 1 ? true : false;
                }
                //if (ConfigurationManager.AppSettings["CityCodeToReturnAllAirportsFlightSearch"] != null && ConfigurationManager.AppSettings["CityCodeToReturnAllAirportsFlightSearch"].Contains(origin))
                //{
                //    trip.OriginAllAirports = true;
                //}
                //if (ConfigurationManager.AppSettings["CityCodeToReturnAllAirportsFlightSearch"] != null && ConfigurationManager.AppSettings["CityCodeToReturnAllAirportsFlightSearch"].Contains(destination))
                //{
                //    trip.DestinationAllAirports = true;
                //}

                trip.Destination = destination;
                trip.DepartDate = departureDate;
                if (searchNearbyDestinations)
                {
                    trip.SearchRadiusMilesDestination = getSearchRadiusForNearbyAirports();
                }

                if (searchNearbyOrigins)
                {
                    trip.SearchRadiusMilesOrigin = getSearchRadiusForNearbyAirports();
                }

                trip.CabinType = GetCabinType(cabin);
                trip.SearchFiltersIn = GetSearchFilters(filters, appId, appVersion, isStandardRevenueSearch, isELFFareDisplayAtFSR, fareType);
                trip.UseFilters = useFilters;

            }

            return trip;
        }

        private bool IsValidDate(string dateString, bool notPastDate, bool used = false)
        {
            bool result = false;

            if (used)
            {
                return used;
            }

            DateTime date = new DateTime(0);
            DateTime.TryParse(dateString, out date);

            if (date.Ticks != 0)
            {
                if (notPastDate)
                {
                    date = new DateTime(date.Year, date.Month, date.Day);
                    if (date >=
                      (_configuration.GetValue<bool>("InvalidFlightdateFix")
                      ? DateTime.Today.AddHours(-12).Date
                      : DateTime.Today))
                    {
                        result = true;
                    }
                    else
                    {
                        throw new MOBUnitedException(_configuration.GetValue<string>("PastBookingDateErrorMessage"));
                    }
                }
                else
                {
                    result = true;
                }
            }

            return result;
        }

        private CabinType GetCabinType(string cabin)
        {
            CabinType cabinType = new CabinType();

            if (string.IsNullOrEmpty(cabin))
            {
                cabinType = CabinType.ValueNotSet;
            }
            else
            {
                switch (cabin.ToUpper().Trim())
                {
                    case "ECON":
                    case "COACH":
                        cabinType = CabinType.Coach;
                        break;
                    case "FIRST":
                        cabinType = CabinType.First;
                        break;
                    case "BUSINESS":
                        cabinType = CabinType.Business;
                        break;
                    case "BUSINESSFIRST":
                        cabinType = CabinType.BusinessFirst;
                        break;
                    default:
                        cabinType = CabinType.ValueNotSet;
                        break;
                }
            }

            return cabinType;
        }

        private int getSearchRadiusForNearbyAirports()
        {
            int searchRadius = 150;
            int.TryParse(_configuration.GetValue<string>("SearchRadiusForNearbyAirports"), out searchRadius);

            return searchRadius;
        }

        private string GetFareFamily(string cabinSearched, string fareType)
        {
            string cabin = "ECONOMY";

            switch (cabinSearched.Trim().ToLower())
            {
                case "econ":
                    cabin = "ECONOMY";
                    break;
                case "business":
                case "businessfirst":
                    cabin = "BUSINESS";
                    break;
                case "first":
                    cabin = "FIRST";
                    break;
                case "awardecon":
                    cabin = "AWARDECONOMY";
                    break;
                case "awardbusiness":
                case "awardbusinessfirst":
                    cabin = "AWARDBUSINESSFIRST";
                    break;
                case "awardfirst":
                    cabin = "AWARDFIRST";
                    break;
                default:
                    cabin = "ECONOMY";
                    break;
            }

            string FareType = "";

            switch (fareType.Trim().ToLower())
            {
                case "lf":
                    FareType = "";
                    break;
                case "ff":
                    FareType = "-FLEXIBLE";
                    break;
                case "urf":
                    FareType = "-UNRESTRICTED";
                    break;
                default:
                    FareType = "";
                    break;
            }

            return cabin + FareType;
        }

        private PaxInfo GetYAPaxInfo()
        {
            PaxInfo paxInfo = new PaxInfo
            {
                Characteristics = new List<United.Service.Presentation.CommonModel.Characteristic>()
            };
            paxInfo.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "YOUNGADULT", Value = "True" });
            paxInfo.DateOfBirth = DateTime.Today.AddYears(-20).ToShortDateString();
            paxInfo.PaxType = PaxType.Adult;
            return paxInfo;
        }

        private void GetPaxInfo(MOBSHOPShopRequest MOBShopShopRequest, ShopRequest shopRequest)
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
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.Adult,
                                DateOfBirth = DateTime.Today.AddYears(-25).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Senior:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.Senior,
                                DateOfBirth = DateTime.Today.AddYears(-67).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child2To4:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.Child01,
                                DateOfBirth = DateTime.Today.AddYears(-3).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child5To11:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.Child02,
                                DateOfBirth = DateTime.Today.AddYears(-8).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child12To17:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.Child03,
                                DateOfBirth = DateTime.Today.AddYears(-15).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child12To14:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.Child04,
                                DateOfBirth = DateTime.Today.AddYears(-13).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.Child15To17:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.Child05,
                                DateOfBirth = DateTime.Today.AddYears(-16).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.InfantSeat:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.InfantSeat,
                                DateOfBirth = DateTime.Today.AddYears(-1).ToShortDateString()
                            };
                            AssignPTCValue(MOBShopShopRequest, paxInfo);
                            shopRequest.PaxInfoList.Add(paxInfo);
                        }
                        break;

                    case (int)PAXTYPE.InfantLap:
                        for (int i = 0; i < t.Count; i++)
                        {
                            paxInfo = new PaxInfo
                            {
                                PaxType = PaxType.InfantLap,
                                DateOfBirth = DateTime.Today.AddYears(-1).ToShortDateString()
                            };
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

        private List<PaxInfo> GetReshopPaxInfoList(MOBSHOPShopRequest mobShopShopRequest, United.Service.Presentation.ReservationResponseModel.ReservationDetail cslReservation)
        {
            List<PaxInfo> paxInfoList = null;
            if (cslReservation != null && cslReservation.Detail != null && cslReservation.Detail.Travelers != null && cslReservation.Detail.Travelers.Count > 0)
            {
                paxInfoList = new List<PaxInfo>();
                foreach (var cslReservationTraveler in cslReservation.Detail.Travelers)
                {
                    bool isSelectedTraveler = false;
                    if (mobShopShopRequest != null && mobShopShopRequest.ReshopTravelers != null && mobShopShopRequest.ReshopTravelers.Count > 0)
                    {
                        isSelectedTraveler =
                            mobShopShopRequest.ReshopTravelers.Exists(
                                p => cslReservationTraveler.Person.Key == p.SHARESPosition);
                    }

                    if (isSelectedTraveler)
                    {
                        PaxInfo paxInfo = new PaxInfo
                        {
                            DateOfBirth = cslReservationTraveler.Person.DateOfBirth,
                            Key = cslReservationTraveler.Person.Key,
                            PaxTypeCode = cslReservationTraveler.Person.Type,
                            PaxType = GetRevenuePaxType(cslReservationTraveler.Person.Type)
                        };

                        paxInfoList.Add(paxInfo);
                    }
                }
            }
            return paxInfoList;
        }

        private SearchType GetSearchTypeSelection(string searchTypeSelection)
        {
            SearchType searchType = new SearchType();

            if (string.IsNullOrEmpty(searchTypeSelection))
            {
                searchType = SearchType.ValueNotSet;
            }
            else
            {
                switch (searchTypeSelection.Trim().ToUpper())
                {
                    case "OW":
                        searchType = SearchType.OneWay;
                        break;
                    case "RT":
                        searchType = SearchType.RoundTrip;
                        break;
                    case "MD":
                        searchType = SearchType.MultipleDestination;
                        break;
                    default:
                        searchType = SearchType.ValueNotSet;
                        break;
                }

            }

            return searchType;
        }

        private PaxType GetRevenuePaxType(string pnrTravelerPersonType)
        {
            PaxType paxType = PaxType.Adult;
            if (pnrTravelerPersonType == "INS")
            {
                paxType = PaxType.InfantSeat;
            }
            else if (pnrTravelerPersonType == "C05")
            {
                paxType = PaxType.Child01;
            }
            else if (pnrTravelerPersonType == "C08")
            {
                paxType = PaxType.Child02;
            }
            else if (pnrTravelerPersonType == "C12")
            {
                paxType = PaxType.Child03;
            }
            else if (pnrTravelerPersonType == "C15")
            {
                paxType = PaxType.Child04;
            }
            else if (pnrTravelerPersonType == "C17")
            {
                paxType = PaxType.Child05;
            }
            else if (pnrTravelerPersonType == "SNR")
            {
                paxType = PaxType.Senior;
            }
            else if (pnrTravelerPersonType == "INF")
            {
                paxType = PaxType.InfantLap;
            }
            else if (pnrTravelerPersonType == "ADT")
            {
                paxType = PaxType.Adult;
            }
            return paxType;
        }

        private void AssignCalendarLengthOfStay(int lengthOfCalendar, ShopRequest shopRequest)
        {
            bool isAwardCalendarMP2017 = _configuration.GetValue<bool>("NGRPAwardCalendarMP2017Switch");
            if (shopRequest.AwardTravel)
            {
                if (isAwardCalendarMP2017 && lengthOfCalendar == 3)// 3 days calendar i.e. 3 days before and 3 days later from the search date
                {
                    shopRequest.FlexibleDaysAfter = lengthOfCalendar;
                    shopRequest.FlexibleDaysBefore = lengthOfCalendar;
                }
                else if (isAwardCalendarMP2017 && lengthOfCalendar == 6)// 7 days calendar i.e. starting from the search date
                {
                    shopRequest.FlexibleDaysAfter = lengthOfCalendar;
                    shopRequest.FlexibleDaysBefore = 0;
                }
            }
        }

        private United.Services.FlightShopping.Common.Trip GetTripForReshopChangeRequestUsingMobRequestAndCslReservationSegments(MOBSHOPTripBase mobSHOPTripBase, int segmentnumber, string fareType, List<United.Service.Presentation.SegmentModel.ReservationFlightSegment> cslReservationFlightSegment, bool isused = false, int originalTripIndex = 0)
        {
            United.Services.FlightShopping.Common.Trip trip = null;
            string segmentCabin;

            trip = GetTrip(mobSHOPTripBase.Origin, mobSHOPTripBase.Destination,
               mobSHOPTripBase.DepartDate, mobSHOPTripBase.Cabin,
               mobSHOPTripBase.UseFilters, mobSHOPTripBase.SearchFiltersIn,
               mobSHOPTripBase.SearchNearbyOriginAirports,
               mobSHOPTripBase.SearchNearbyDestinationAirports, -1, "", false, false, "", isused);

            trip.ChangeType = (int)mobSHOPTripBase.ChangeType;
            segmentCabin = mobSHOPTripBase.Cabin;
            trip.TripIndex = segmentnumber + 1;

            if (trip.SearchFiltersIn == null)
            {
                trip.SearchFiltersIn = new SearchFilterInfo();
            }
            trip.SearchFiltersIn.FareFamily = GetFareFamily(segmentCabin, fareType);

            if (trip.ChangeType == 3)
            {
                var tripUsedIndex = cslReservationFlightSegment.Where(p => p.TripNumber == trip.TripIndex.ToString());
                if (isused)
                {
                    int index = Convert.ToInt32(trip.TripIndex);
                    tripUsedIndex = cslReservationFlightSegment.Where(p => p.TripNumber == originalTripIndex.ToString());
                }
                foreach (var cslTripSegments in tripUsedIndex)
                {
                    Flight flight = new Flight
                    {
                        FlightNumber = cslTripSegments.FlightSegment.FlightNumber,
                        OperatingCarrier = cslTripSegments.FlightSegment.OperatingAirlineCode,
                        Origin = cslTripSegments.FlightSegment.DepartureAirport.IATACode,
                        Destination = cslTripSegments.FlightSegment.ArrivalAirport.IATACode,
                        DepartDateTime = cslTripSegments.FlightSegment.DepartureDateTime
                    };
                    trip.Flights.Add(flight);
                }
            }

            return trip;
        }

        private string GetCharacteristicDescription(List<Service.Presentation.CommonModel.Characteristic> characteristics, string code)
        {
            string keyDesc = string.Empty;
            if (characteristics != null && characteristics.Exists(p => p?.Code?.Trim() == code && p?.Value?.ToUpper() == "TRUE"))
            {
                keyDesc = characteristics.First(p => p.Code.Trim() == code && p.Value.ToUpper() == "TRUE").Description;
            }
            return keyDesc;
        }

        private async Task RequestForNonStopFlights(MOBSHOPShopRequest mobShopShopRequest, ShopRequest shopRequest)
        {
            if (_configuration.GetValue<bool>("EnableCodeRefactorForSavingSessionCalls") == false)
            {
                var session = await _sessionHelperService.GetSession<ShoppingResponse>(shopRequest.SessionId, new ShoppingResponse().ObjectName, new List<string> { shopRequest.SessionId, new ShoppingResponse().ObjectName });
                if (session != null && mobShopShopRequest.GetFlightsWithStops)
                {
                    shopRequest.CartId = session.CartId;
                }
            }

            foreach (var trip in shopRequest.Trips)
            {
                trip.UseFilters = true;
                SetStopCountsToGetNonStopFlights(mobShopShopRequest.GetNonStopFlightsOnly, mobShopShopRequest.GetFlightsWithStops, trip.SearchFiltersIn);
                break; //**nonstopchanges==>  To set use filters bool as true only for Out Bound Segment as we go live for 17G with shop call selectrip later when working on select trip then remove this break
            }
        }

        public MOBSHOPTripBase ConvertReshopTripToTripBase(ReshopTrip reshopTrip)
        {
            MOBSHOPTripBase tripBase = new MOBSHOPTripBase();
            tripBase = reshopTrip.OriginalTrip;
            tripBase.DepartDate = reshopTrip.OriginalTrip.DepartDate;

            return tripBase;
        }
        private bool IsAwardFSRRedesignEnabled(int appId, string appVersion)
        {
            if (_configuration.GetValue<bool>("EnableAwardFSRChanges")) return false;
            return GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("AndroidAwardFSRChangesVersion"), _configuration.GetValue<string>("iOSAwardFSRChangesVersion"));
        }
        private bool IsStandardRevenueSearch(bool isCorporateBooking, bool isYoungAdultBooking, bool isAwardTravel,
                                                   string employeeDiscountId, string travelType, bool isReshop, string fareClass,
                                                   string promotionCode)
        {
            return !(isCorporateBooking || travelType == TravelType.CLB.ToString() || isYoungAdultBooking ||
                     isAwardTravel || !string.IsNullOrEmpty(employeeDiscountId) || isReshop ||
                     travelType == TravelType.TPSearch.ToString() || !string.IsNullOrEmpty(fareClass) ||
                     !string.IsNullOrEmpty(promotionCode));
        }
        private bool IsEnableRefundableFaresToggle(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableRefundableFaresToggle") &&
                   GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("AndroidRefundableFaresToggleVersion"), _configuration.GetValue<string>("iPhoneRefundableFaresToggleVersion"));
        }
        private bool IsMixedCabinFilerEnabled(int id, string version)
        {
            if (!_configuration.GetValue<bool>("EnableAwardMixedCabinFiter")) return false;
            return GeneralHelper.IsApplicationVersionGreaterorEqual(id, version, _configuration.GetValue<string>("Android_AwardMixedCabinFiterFeatureSupported_AppVersion"), _configuration.GetValue<string>("iPhone_AwardMixedCabinFiterFeatureSupported_AppVersion"));
        }
        private void AddRefundableFaresToggleFilter(SearchFilterInfo shopfilter, MOBSearchFilters filters, int appId, string appVersion, bool isStandardRevenueSearch, bool isELFFareDisplayAtFSR, string fareType)
        {
            //TO DO ABINASH
            //Refundable fares toggle feature
            if (IsEnableRefundableFaresToggle(appId, appVersion) && isStandardRevenueSearch)
            {
                shopfilter.ShopIndicators = new ShopIndicators();

                shopfilter.ShopIndicators.IsBESelected = isELFFareDisplayAtFSR;

                if ((filters?.RefundableFaresToggle?.IsSelected ?? false) ||
                    (filters?.RefundableFaresToggle == null && fareType == "urf"))
                {
                    shopfilter.ShopIndicators.IsRefundableSelected = true;
                }
                else
                {
                    shopfilter.ShopIndicators.IsRefundableSelected = false;
                }
            }
        }
        private void AddMixedCabinToggleFilter(SearchFilterInfo shopfilter, MOBSearchFilters filters, int appId, string appVersion, bool isStandardRevenueSearch, bool isELFFareDisplayAtFSR, string fareType)
        {
            //TO DO ABINASH
            // MixedCabin toggle feature
            if (IsMixedCabinFilerEnabled(appId, appVersion) && (fareType == _configuration.GetValue<string>("MixedCabinToggle") || fareType == "lf"))
            {
                if (shopfilter.ShopIndicators == null)
                    shopfilter.ShopIndicators = new ShopIndicators();

                if (filters == null || (filters?.AdditionalToggles?.FirstOrDefault(a => a.Key == _configuration.GetValue<string>("MixedCabinToggleKey"))?.IsSelected ?? false))
                    shopfilter.ShopIndicators.IsMixedToggleSelected = true;
                else
                    shopfilter.ShopIndicators.IsMixedToggleSelected = false;
            }
        }
        private bool EnableEPlusAncillary(int appID, string appVersion, bool isReshop = false)
        {
            return _configuration.GetValue<bool>("EnableEPlusAncillaryChanges") && !isReshop && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion,_configuration.GetValue<string>("EplusAncillaryAndroidversion"),_configuration.GetValue<string>("EplusAncillaryiOSversion"));
        }


        private async Task<(List<MOBSHOPFareWheelItem> response, List<MOBStyledText> messages)> GetSeasonalityFareWheelData(Session session, MOBSHOPShopRequest request)
        {
            string cslActionName = "/shop";
            List<MOBSHOPFareWheelItem> response = null;
            List<MOBStyledText> messages = null;

            try
            {
                // Make a shop call with Seasonality request parameters 
                var cslSeasonalityRequest = await GetShopRequest(request, true, true);
                cslSeasonalityRequest.CalendarOnly = true;
                cslSeasonalityRequest.FareCalendar = true;
                if (cslSeasonalityRequest.Characteristics == null) cslSeasonalityRequest.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                cslSeasonalityRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "SeasonalityFareLookupFallBack", Value = "true" });
                cslSeasonalityRequest.CalendarLengthOfStay = Convert.ToInt32(_configuration.GetValue<string>("SeasonalityNumberOfDaysOfCalendarDisplay"));
                string jsonRequest = JsonConvert.SerializeObject(cslSeasonalityRequest);


                var jsonResponse = await _shoppingClientService.PostAsync<United.Services.FlightShopping.Common.ShopResponse>(session.Token, session.SessionId, cslActionName, cslSeasonalityRequest);



                if (jsonResponse == null || jsonResponse.Errors?.Count > 0)
                {
                    return (response, messages);// we do not want any errors if this call fails
                }
                if (jsonResponse != null && jsonResponse.Calendar != null && jsonResponse.Calendar.Months?.Count > 0)
                {
                    CultureInfo ci = null;
                    response = new List<MOBSHOPFareWheelItem>();
                    foreach (CalendarMonth month in jsonResponse.Calendar.Months)
                    {
                        foreach (var week in month?.Weeks)
                        {
                            foreach (var day in week?.Days)
                            {
                                if (day != null)
                                {
                                    ci = TopHelper.GetCultureInfo(day.DisplayCurrency);
                                    if (day.DisplayFare > 0)
                                    {
                                       var sHOPFareWheelItem  = new MOBSHOPFareWheelItem
                                        {
                                            Key = GetDateFormatForDisplay(day.DateValue),
                                            DisplayValue = (string.IsNullOrEmpty(day.ReturnDateValue)) ? day.DateValue?.ToDateTime().ToString("MMM dd") : (day.DateValue?.ToDateTime().ToString("MMM dd") + " - " + day.ReturnDateValue?.ToDateTime().ToString("MMM dd")),
                                            TripId = jsonResponse.LastBBXSolutionSetId,
                                            ProductId = "Not Available",// not required for seasonality, populating only because in case something breaks in UI for farewheel code
                                            ReturnKey = (string.IsNullOrEmpty(day.ReturnDateValue) == false) ? GetDateFormatForDisplay(day.ReturnDateValue) : null,
                                            Value = TopHelper.FormatAmountForDisplay(day.DisplayFare, ci, true)
                                        };
                                        if (request.IsMoneyPlusMiles || !request.MileagePlusAccountNumber.IsNullOrEmpty())
                                        {
                                            sHOPFareWheelItem.MoneyPlusMilesValue = TopHelper.FormatAmountForDisplay(day.DisplayFare, ci, true) + " + 10.1k" + "\nmiles";
                                        }
                                        response.Add(sHOPFareWheelItem);
                                    }
                                }
                            }
                        }
                    }
                    List<CMSContentMessage> lstMessages = null;
                    if (_configuration.GetValue<bool>("DisableSDLValuesForNoFlightsFound") == false)
                        lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                    messages = new List<MOBStyledText>();
                    messages.Add(new MOBStyledText { Text = string.Format(_configuration.GetValue<bool>("DisableSDLValuesForNoFlightsFound") ? _configuration.GetValue<string>("SeasonalityHeaderText") : _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_Seasonality_SeasonalityHeaderText"), jsonResponse.Trips?.FirstOrDefault().OriginDecoded, jsonResponse.Trips?.FirstOrDefault().DestinationDecoded), FontSize = (_configuration.GetValue<int>("SeasonalityHeaderFontSize")) });
                    messages.Add(new MOBStyledText { Text = _configuration.GetValue<bool>("DisableSDLValuesForNoFlightsFound") ? _configuration.GetValue<string>("SeasonalityDescriptionText") : _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_Seasonality_SeasonalityDescriptionText"), FontSize = (_configuration.GetValue<int>("SeasonalityDescriptionFontSize")) });
                }
            }
            catch (Exception)
            {
                return (null, null);
            }

            return (response, messages);
        }

        private static string GetDateFormatForDisplay(string date)
        {
            int year = DateTime.Today.Year;
            int.TryParse(date.Substring(0, 4), out year);
            int month = DateTime.Today.Month;
            int.TryParse(date.Substring(5, 2), out month);
            int dayD = DateTime.Today.Day;
            int.TryParse(date.Substring(8, 2), out dayD);
            var dateValue = (month < 10 ? "0" + month.ToString() : month.ToString()) + "/" + (dayD < 10 ? "0" + dayD.ToString() : dayD.ToString()) + "/" + year.ToString();
            return dateValue;
        }


    }
}
