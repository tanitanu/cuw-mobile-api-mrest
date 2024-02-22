using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FSRHandler;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.Model.Booking;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Common;
using United.Mobile.Model.Shopping.Common.TripPlanner;
using United.Service.Presentation.LoyaltyModel;
using United.Service.Presentation.ReferenceDataModel;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.Boombox;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.LMX;
using United.Services.FlightShopping.Common.OrganizeResults;
using United.Services.FlightShopping.Common.SpecialPricing;
using United.Utility.Helper;
using United.Utility.HttpService;
using CreditType = United.Mobile.Model.Shopping.CreditType;
using CreditTypeColor = United.Mobile.Model.Shopping.CreditTypeColor;
using ErrorInfo = United.Services.FlightShopping.Common.ErrorInfo;
using MOBSHOPSegmentInfoDisplay = United.Mobile.Model.SeatMapEngine.MOBSHOPSegmentInfoDisplay;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using SearchType = United.Services.FlightShopping.Common.SearchType;
using United.Utility.Enum;
using United.Mobile.Model.Common.SSR;

namespace United.Mobile.Services.Shopping.Domain
{
    public partial class ShopBooking : IShopBooking
    {
        private AirportDetailsList airportsList = null;
        private string CURRENCY_TYPE_MILES = "miles";
        private string PRICING_TYPE_CLOSE_IN_FEE = "CLOSEINFEE";
        // Making fare type by default in upper case
        private readonly string _strFARETYPEFULLYREFUNDABLE = "URF";
        private readonly ICacheLog<ShopBooking> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IMileagePlus _mileagePlus;
        private readonly IReferencedataService _referencedataService;
        private readonly IFlightShoppingService _flightShoppingService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ITravelerCSL _travelerCSL;
        private readonly ICachingService _cachingService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly IDPService _dPService;
        private HttpContext _httpContext;
        private readonly IShoppingClientService _shoppingClientService;
        private MOBProductSettings configurationProductSettings;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly IOmniCart _omniCart;
        private readonly IFeatureSettings _featureSettings;
        private readonly IFeatureToggles _featureToggles;
        // private readonly ILogger _cacheLog;
        public ShopBooking(ICacheLog<ShopBooking> logger
            , IConfiguration configuration
            , IHeaders headers
            , IDynamoDBService dynamoDBService
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , IMileagePlus mileagePlus
            , IReferencedataService referencedataService
            , IFlightShoppingService flightShoppingService
            , ITravelerCSL travelerCSL
            , ICachingService cachingService
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , IDPService dPService
            , IShoppingClientService shoppingClientService
            , IFFCShoppingcs fFCShoppingcs
            , IOmniCart omniCart
            , IFeatureSettings featureSettings
            , IFeatureToggles featureToggles
            // , ILogger cacheLog
            )
        {
            _logger = logger;
            _headers = headers;
            _dynamoDBService = dynamoDBService;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
            _mileagePlus = mileagePlus;
            _referencedataService = referencedataService;
            _flightShoppingService = flightShoppingService;
            _travelerCSL = travelerCSL;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _cachingService = cachingService;
            _dPService = dPService;
            _shoppingClientService = shoppingClientService;
            configurationProductSettings = _configuration.GetSection("productSettings").Get<MOBProductSettings>();
            _fFCShoppingcs = fFCShoppingcs;
            _omniCart = omniCart;
            _featureSettings = featureSettings;
            _featureToggles = featureToggles;
            //_logger = cacheLog;

        }

        public string Serialize<T>(T t)
        {
            try
            {
                DataContractJsonSerializer dataContractJsonSerializer = new DataContractJsonSerializer(typeof(T));
                System.IO.MemoryStream memoryStream = new System.IO.MemoryStream();
                dataContractJsonSerializer.WriteObject(memoryStream, t);
                string jsonString = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                memoryStream.Close();
                return jsonString;
            }
            catch
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(t);
            }
        }
        public async Task<MOBSHOPAvailability> GetAvailability(string token, MOBSHOPShopRequest shopRequest, bool isFirstShopCall, HttpContext httpContext)
        {
            _httpContext = httpContext;
            MOBSHOPAvailability availability = null;
            string logAction = shopRequest.IsReshopChange ? "ReShop" : "Shop";
            bool isShop = true;
            //IDisposable timer = null;            
            //U4B Fast Follower changes
            United.CorporateDirect.Models.CustomerProfile.CorpPolicyResponse _corpPolicyResponse = null;
            bool isU4BTravelAddOnPolicyEnabled = await _shoppingUtility.IsEnableU4BTravelAddONPolicy(shopRequest.IsCorporateBooking, shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.CatalogItems).ConfigureAwait(false);
            if (isU4BTravelAddOnPolicyEnabled)
            {
                _corpPolicyResponse = await _shoppingUtility.GetCorporateTravelPolicyResponse(shopRequest.DeviceId, shopRequest.MileagePlusAccountNumber, shopRequest.SessionId);
            }

            ShopRequest request = await GetShopRequest(shopRequest, true, _corpPolicyResponse);
            if (shopRequest.IsReshopChange
               && !_configuration.GetValue<bool>("DisableAlwaysNewCartIdForReshop"))
            {
                request.DeviceId = shopRequest.DeviceId;
                request.CartId = Convert.ToString(Guid.NewGuid()).ToUpper();
            }
            else if (_configuration.GetValue<bool>("EnableOmniChannelCartMVP1"))
            {
                // Need to check remaining references for shop request
                request.DeviceId = shopRequest.DeviceId;
                Guid cartid = new Guid(shopRequest.SessionId);
                request.CartId = cartid.ToString().ToUpper();
            }
            if (EnableAdvanceSearchCouponBooking(shopRequest.Application.Id, shopRequest.Application.Version.Major) && !shopRequest.IsReshopChange && !request.AwardTravel)
            {
                if (!string.IsNullOrEmpty(shopRequest.PromotionCode) && !string.IsNullOrEmpty(shopRequest.MileagePlusAccountNumber) && (request?.LoyaltyPerson == null || string.IsNullOrEmpty(request?.LoyaltyPerson?.LoyaltyProgramMemberID)))
                {
                    if (request.LoyaltyPerson == null)
                        request.LoyaltyPerson = new Service.Presentation.PersonModel.LoyaltyPerson();

                    request.LoyaltyPerson.LoyaltyProgramMemberID = shopRequest.MileagePlusAccountNumber;
                }
            }

            string shopCSLCallDurations = string.Empty;
            string callTime4Tuning = string.Empty;

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch afterPopulateTripStopWatch;
            Stopwatch shopCSLCallDurationstopwatch1;
            shopCSLCallDurationstopwatch1 = new Stopwatch();
            shopCSLCallDurationstopwatch1.Reset();
            shopCSLCallDurationstopwatch1.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******
            string action = "";
            if (_shoppingUtility.EnableAwardNonStop(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshopChange, shopRequest.AwardTravel))
            {
                action = string.Format("/award/{0}", logAction);
            }
            else
            {
                action = (_configuration.GetValue<bool>("EnableTripPlannerView") && shopRequest.TravelType == MOBTripPlannerType.TPSearch.ToString()) ?
               $"/tripplanner/Shop" : string.Format("/{0}", logAction);
            }


            var Token = string.IsNullOrEmpty(token) ? await _dPService.GetAnonymousToken(shopRequest.Application.Id, request.DeviceId, _configuration) : token;
            United.Services.FlightShopping.Common.ShopResponse response = null;
            if (shopRequest.IsReshopChange)
            {
                response = await _shoppingClientService.PostAsyncForReShop<United.Services.FlightShopping.Common.ShopResponse>(Token, shopRequest.SessionId, action, request);
            }
            else
            {
                response = await _shoppingClientService.PostAsync<United.Services.FlightShopping.Common.ShopResponse>(Token, shopRequest.SessionId, action, request);
            }
            //var response = returnValue;
            if (shopCSLCallDurationstopwatch1.IsRunning)
            {
                shopCSLCallDurationstopwatch1.Stop();
            }
            shopCSLCallDurations = shopCSLCallDurations + "|2=" + shopCSLCallDurationstopwatch1.ElapsedMilliseconds.ToString() + "|"; // 2 = shopCSLCallDurationstopwatch1

            List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(shopRequest, shopRequest.SessionId, Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");

            if (response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && NoCSLExceptions(response.Errors))
            {
                #region "CSLSHOP=" //****Get Call Duration Code - Venkat 03/17/2015*******
                Stopwatch populateTripStopWatch;
                populateTripStopWatch = new Stopwatch();
                populateTripStopWatch.Reset();
                populateTripStopWatch.Start();
                shopCSLCallDurations = shopCSLCallDurations + "CSLSHOP=" + response.CallTimeDomain + "|";
                callTime4Tuning = "ITA = " + response.CallTimeBBX + callTime4Tuning;
                #endregion //****Get Call Duration Code - Venkat 03/17/2015*******
                await SaveCSLShopResponseForTripPlanner(shopRequest, response);
                availability = new MOBSHOPAvailability();
                availability.SessionId = shopRequest.SessionId;
                availability.CartId = response.CartId;
                availability.AwardTravel = shopRequest.AwardTravel;
                availability.AwardCalendar = PopulateAwardCalendar(response.Calendar, response.LastBBXSolutionSetId, "");
                availability.ResponseType = MOBAvailabiltyResponseType.Default.GetDescription();
                if (_shoppingUtility.EnableTravelerTypes(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshopChange) && shopRequest.TravelerTypes != null && shopRequest.TravelerTypes.Count > 0)
                {
                    availability.TravelerCount = ShopStaticUtility.GetTravelerCount(shopRequest.TravelerTypes);
                }

                Session session = new Session();
                session = await _sessionHelperService.GetSession<Session>(shopRequest.SessionId, session.ObjectName, new List<string> { shopRequest.SessionId, session.ObjectName });

                if (isU4BTravelAddOnPolicyEnabled && (await _shoppingUtility.IsEnableU4BCorporateTravelSessionFix().ConfigureAwait(false)) && _corpPolicyResponse != null && _corpPolicyResponse.TravelPolicies != null && _corpPolicyResponse.TravelPolicies.Count > 0 && session != null && !session.HasCorporateTravelPolicy)
                {
                    session.HasCorporateTravelPolicy = true;
                    await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName);
                }

                if (_shoppingUtility.CheckSkipElfProperties(shopRequest))
                {
                    await SetAvailabilityELFProperties(availability, shopRequest.NumberOfAdults > 1, _shoppingUtility.EnableSSA(shopRequest.Application.Id, shopRequest.Application.Version.Major));
                    if (_configuration.GetValue<bool>("BasicEconomyContentChange") && request.Trips != null)
                    {
                        foreach (var c in request.Trips)
                        {
                            var differentYearcount = request.Trips.Select(d => Convert.ToDateTime(d.DepartDate).Year).Distinct().Count();
                            if (request != null && request.Trips != null && differentYearcount > 1 && DateTime.Now.Year == 2019)
                            {
                                var presentYear = request.Trips.Where(d => Convert.ToDateTime(d.DepartDate).Year == 2019).FirstOrDefault().DepartDate;
                                var futureYear = request.Trips.Where(d => (Convert.ToDateTime(d.DepartDate).Year) == 2020).FirstOrDefault().DepartDate;
                                if (!string.IsNullOrEmpty(presentYear) && !string.IsNullOrEmpty(futureYear))
                                {
                                    if (availability.ELFShopMessages.Any(i => i.Id == "ELFConfirmFareTypeFooter_19_20"))
                                    {
                                        availability.ELFShopMessages.RemoveWhere(e => !string.IsNullOrEmpty(e.Id) && e.Id == "ELFConfirmFareTypeFooter");
                                        availability.ELFShopMessages.RemoveWhere(e => !string.IsNullOrEmpty(e.Id) && e.Id == "ELFConfirmFareTypeFooter_2020");
                                        (availability.ELFShopMessages.Where(i => !string.IsNullOrEmpty(i.Id) && i.Id == "ELFConfirmFareTypeFooter_19_20").FirstOrDefault().Id) = "ELFConfirmFareTypeFooter";
                                    }
                                }
                            }
                            else
                            {
                                if (request != null && request.Trips != null && Convert.ToDateTime(c.DepartDate).Year == 2019)
                                {
                                    availability.ELFShopMessages.RemoveWhere(e => !string.IsNullOrEmpty(e.Id) && e.Id == "ELFConfirmFareTypeFooter_2020");
                                    availability.ELFShopMessages.RemoveWhere(e => !string.IsNullOrEmpty(e.Id) && e.Id == "ELFConfirmFareTypeFooter_19_20");
                                }
                                else if (request != null && request.Trips != null && Convert.ToDateTime(c.DepartDate).Year >= 2020)
                                {
                                    if (availability.ELFShopMessages.Any(i => !string.IsNullOrEmpty(i.Id) && i.Id == "ELFConfirmFareTypeFooter_2020"))
                                    {
                                        availability.ELFShopMessages.RemoveWhere(e => !string.IsNullOrEmpty(e.Id) && e.Id == "ELFConfirmFareTypeFooter");
                                        availability.ELFShopMessages.RemoveWhere(e => !string.IsNullOrEmpty(e.Id) && e.Id == "ELFConfirmFareTypeFooter_19_20");
                                        (availability.ELFShopMessages.Where(i => !string.IsNullOrEmpty(i.Id) && i.Id == "ELFConfirmFareTypeFooter_2020").FirstOrDefault().Id) = "ELFConfirmFareTypeFooter";
                                    }

                                }
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(request.EmployeeDiscountId))
                {
                    availability.FareWheel = PopulateFareWheelDates(shopRequest.Trips, "SHOP");
                }
                int tripIndex = 0;
                if (shopRequest.IsReshopChange)
                {
                    tripIndex = response.LastTripIndexRequested - 1;
                    await IsLastTripFSR(shopRequest.IsReshopChange, availability, response.Trips);
                }
                //-------Feature 208204--- Common class data carrier for hirarchy methds-----
                MOBSHOPDataCarrier _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                _mOBSHOPDataCarrier.SearchType = shopRequest.SearchType;
                if (_configuration.GetValue<bool>("Shopping - bPricingBySlice"))
                {
                    if (!availability.AwardTravel && !shopRequest.IsReshopChange)
                    {
                        availability.PriceTextDescription = GetPriceTextDescription(shopRequest.SearchType);
                        // availability.FSRFareDescription = GetFSRFareDescription(shopRequest);
                        SetFSRFareDescriptionForShop(availability, shopRequest);

                        // One time decide to assign text for all the products in the Flights. Will be using in BE & Compare Screens
                        if (IsTripPlanSearch(shopRequest.TravelType))
                        {
                            _mOBSHOPDataCarrier.PriceFormText = GetPriceFromText(shopRequest);
                        }
                        else
                            _mOBSHOPDataCarrier.PriceFormText = GetPriceFromText(shopRequest.SearchType);
                        ///208848 : PROD Basic Economy mApps Price in footer of FSR is showing lowest BE fare when BE switch is off 
                        ///Srini - 12/29/2017
                        if (_configuration.GetValue<bool>("BugFixToggleFor18B") && shopRequest.GetFlightsWithStops)
                        {
                            MOBSHOPAvailability nonStopsAvailability = await GetLastTripAvailabilityFromPersist(1, shopRequest.SessionId);
                            if (nonStopsAvailability != null &&
                                nonStopsAvailability.Trip != null &&
                                nonStopsAvailability.Trip.SearchFiltersOut != null &&
                                nonStopsAvailability.Trip.SearchFiltersOut.PriceMin > 0)
                            {
                                _mOBSHOPDataCarrier.FsrMinPrice = nonStopsAvailability.Trip.SearchFiltersOut.PriceMin;
                            }
                        }
                    }
                    else
                    {
                        SetSortDisclaimerForReshop(availability, shopRequest);
                    }
                    var cslReservation = await _sessionHelperService.GetSession<ReservationDetail>(shopRequest.SessionId, new ReservationDetail().GetType().FullName, new List<string> { shopRequest.SessionId, new ReservationDetail().GetType().FullName });
                    if (_shoppingUtility.IsEbulkPNRReshopEnabled(shopRequest.Application.Id, shopRequest.Application.Version.Major, cslReservation))
                    {
                        availability.SortDisclaimerText = null;
                    }
                }

                //-----------
                var isStandardRevenueSearch = IsStandardRevenueSearch(shopRequest.IsCorporateBooking, shopRequest.IsYoungAdultBooking,
                                                                         shopRequest.AwardTravel, shopRequest.EmployeeDiscountId,
                                                                         shopRequest.TravelType, shopRequest.IsReshop || shopRequest.IsReshopChange,
                                                                         shopRequest.FareClass, shopRequest.PromotionCode);

                MOBAdditionalItems additionalItems = new MOBAdditionalItems();
                if (!_configuration.GetValue<bool>("EnableNonStopFlight"))
                {
                    availability.Trip = await PopulateTrip(_mOBSHOPDataCarrier, response.CartId, response.Trips, tripIndex, shopRequest.Trips[0].Cabin,
                            shopRequest.SessionId, shopRequest.Application.Id, shopRequest.DeviceId,
                            shopRequest.Application.Version.Major, shopRequest.ShowMileageDetails,
                            shopRequest.PremierStatusLevel, isStandardRevenueSearch, availability.AwardTravel, shopRequest.IsELFFareDisplayAtFSR, additionalItems: additionalItems, lstMessages: lstMessages);
                }
                else
                {
                    availability.Trip = await PopulateTrip(_mOBSHOPDataCarrier, response.CartId, response.Trips, tripIndex, shopRequest.Trips[0].Cabin,
                            shopRequest.SessionId, shopRequest.Application.Id, shopRequest.DeviceId,
                            shopRequest.Application.Version.Major, shopRequest.ShowMileageDetails,
                            shopRequest.PremierStatusLevel, isStandardRevenueSearch, availability.AwardTravel, shopRequest.IsELFFareDisplayAtFSR,
                            shopRequest.GetNonStopFlightsOnly, shopRequest.GetFlightsWithStops, shopRequest, additionalItems: additionalItems, lstMessages: lstMessages);
                }

                #region // 4 = popuplate trip,PopulateAwardCalendar,PopulateFareWheel  ****Get Call Duration Code - Venkat 03/17/2015*******
                //****Get Call Duration Code - Venkat 03/17/2015*******
                if (populateTripStopWatch.IsRunning)
                {
                    populateTripStopWatch.Stop();
                }
                shopCSLCallDurations = shopCSLCallDurations + "|4=" + populateTripStopWatch.ElapsedMilliseconds.ToString() + "|"; // 4 = popuplate trip,PopulateAwardCalendar,PopulateFareWheel
                afterPopulateTripStopWatch = new Stopwatch();
                afterPopulateTripStopWatch.Reset();
                afterPopulateTripStopWatch.Start();
                #endregion

                #region  //**NOTE**// Venkat - Nov 10,2014 For Oragainze Results
                availability.Trip.Cabin = shopRequest.Trips[0].Cabin;
                availability.Trip.LastTripIndexRequested = response.LastTripIndexRequested;
                #endregion

                #region Amenities
                UpdateAmenitiesIndicatorsRequest amenitiesRequest = new UpdateAmenitiesIndicatorsRequest();
                amenitiesRequest = GetAmenitiesRequest(response.CartId, response.Trips[0].Flights);
                if (_configuration.GetValue<bool>("EnableNonStopFlight") && shopRequest.GetFlightsWithStops)
                {
                    var shopAmenitiesRequest = await _sessionHelperService.GetSession<ShopAmenitiesRequest>(shopRequest.SessionId, (new ShopAmenitiesRequest()).ObjectName, new List<string> { shopRequest.SessionId, (new ShopAmenitiesRequest()).ObjectName });
                    if (shopAmenitiesRequest != null && shopAmenitiesRequest.AmenitiesIndicatorsRequest != null && shopAmenitiesRequest.AmenitiesIndicatorsRequest.Count > 0)
                    {
                        var nonStopFlightsNumbers = shopAmenitiesRequest.AmenitiesIndicatorsRequest[response.LastTripIndexRequested.ToString()].FlightNumbers;
                        if (nonStopFlightsNumbers != null && nonStopFlightsNumbers.Count > 0)
                        {
                            amenitiesRequest.FlightNumbers = amenitiesRequest.FlightNumbers.Concat(nonStopFlightsNumbers).ToCollection();
                        }
                    }
                }
                ShoppingExtend shopExtendDAL = new ShoppingExtend(_sessionHelperService);
                await shopExtendDAL.AddAmenitiesRequestToPersist(amenitiesRequest, request.SessionId, response.LastTripIndexRequested.ToString());

                session.CartId = response.CartId;
                session.ShopSearchTripCount = request.Trips.Count;
                session.VersionID = shopRequest?.Application?.Version?.Major;

                if (!string.IsNullOrEmpty(request.EmployeeDiscountId))
                {
                    session.EmployeeId = request.EmployeeDiscountId;
                }
                ShoppingResponse shop = new ShoppingResponse();
                shop = await _sessionHelperService.GetSession<ShoppingResponse>(shopRequest.SessionId, shop.ObjectName, new List<string> { shopRequest.SessionId, shop.ObjectName });
                shop.SessionId = shopRequest.SessionId;
                shop.CartId = availability.CartId;
                shop.PriceSummary = PopulatePriceSummary(response.PriceSummary);
                await _sessionHelperService.SaveSession<ShoppingResponse>(shop, shopRequest.SessionId, new List<string> { shopRequest.SessionId, shop.ObjectName }, shop.ObjectName);
                #endregion
                if (!string.IsNullOrEmpty(request.EmployeeDiscountId))
                {
                    availability.UaDiscount = _configuration.GetValue<string>("UADiscount");
                }

                #region Corporate Booking

                bool isCorporateBooking = _configuration.GetValue<bool>("CorporateConcurBooking" ?? "false");

                if (IsTravelArranger(shopRequest))
                {
                    session.IsArrangerBooking = true;
                }


                if (isCorporateBooking && shopRequest.IsCorporateBooking && shopRequest.MOBCPCorporateDetails != null && !string.IsNullOrEmpty(shopRequest.MOBCPCorporateDetails.CorporateCompanyName))
                {
                    bool isU4BCorporateBookingEnabled = _shoppingUtility.IsEnableU4BCorporateBooking(shopRequest.Application.Id, shopRequest.Application.Version.Major);
                    bool isEnableSuppressingCompanyNameForBusiness = await _shoppingUtility.IsEnableSuppressingCompanyNameForBusiness(shopRequest.MOBCPCorporateDetails.IsPersonalized).ConfigureAwait(false);

                    string corporateDisclaimer = await _shoppingUtility.IsEnableCorporateNameChange().ConfigureAwait(false) ? _shoppingUtility.GetCorporateDisclaimerText(shopRequest, isU4BCorporateBookingEnabled, isEnableSuppressingCompanyNameForBusiness, session.IsReshopChange) : _shoppingUtility.GetCorporateDisclaimerText(shopRequest, isU4BCorporateBookingEnabled);
                    if (!isU4BTravelAddOnPolicyEnabled && !session.IsReshopChange) // not building the corporate disclaimer as part of U4B FastFollower(MOBILE-27012)
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
                    if (isU4BTravelAddOnPolicyEnabled && _corpPolicyResponse != null && _corpPolicyResponse.TravelPolicies != null && _corpPolicyResponse.TravelPolicies.Count > 0)
                    {
                        availability.CorporateOutOfPolicy = await _shoppingUtility.GetTravelPolicy(_corpPolicyResponse, session, shopRequest, shopRequest.MOBCPCorporateDetails.CorporateCompanyName, shopRequest.MOBCPCorporateDetails.IsPersonalized);
                    }
                }
                #endregion
                if (isCorporateBooking && shopRequest.IsCorporateBooking) // save IsCorporateBooking to session so we can use it later -- Hieu 4/6/2018
                    session.IsCorporateBooking = true;

                if (shopRequest.IsYoungAdultBooking && _shoppingUtility.IsETCchangesEnabled(shopRequest.Application.Id, shopRequest.Application.Version.Major))
                    session.IsYoungAdult = true;

                await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName);

                if (!session.IsReshopChange)
                    SetTitleForFSRPage(availability, shopRequest);

                #region Mileage Balance

                if (_shoppingUtility.EnableMileageBalance(shopRequest.Application.Id, shopRequest.Application.Version.Major))
                {
                    try
                    {
                        if (request.AwardTravel && request.LoyaltyId != null)
                        {
                            Service.Presentation.CommonModel.Characteristic loyaltyId = response.Characteristics.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Code) && c.Code.Trim().Equals("LoyaltyId".Trim(), StringComparison.OrdinalIgnoreCase));
                            if (loyaltyId != null && !string.IsNullOrWhiteSpace(loyaltyId.Value) && loyaltyId.Value.Equals(session.MileagPlusNumber))
                            {
                                Service.Presentation.CommonModel.Characteristic mileageBalance = response.Characteristics.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Code) && c.Code.Trim().Equals("MilesBalance".Trim(), StringComparison.OrdinalIgnoreCase));
                                if (mileageBalance != null && !string.IsNullOrWhiteSpace(mileageBalance.Value))
                                {
                                    if (session.IsFSRRedesign && _configuration.GetValue<bool>("EnableAwardFSRChanges") && GeneralHelper.IsApplicationVersionGreaterorEqual(shopRequest.Application.Id, shopRequest.Application.Version.Major, _configuration.GetValue<string>("AndroidAwardFSRChangesVersion"), _configuration.GetValue<string>("iOSAwardFSRChangesVersion")))
                                    {
                                        availability.AvailableAwardMiles = string.Format("Mileage balance: {0} ", ShopStaticUtility.GetThousandPlaceCommaDelimitedNumberString(mileageBalance.Value));
                                        // populate AvailableAwardMilesWithDesc
                                        GetMilesDescWithFareDiscloser(availability, session, _shoppingUtility.EnableBagCalcSelfRedirect(shopRequest.Application.Id, shopRequest.Application.Version.Major), shopRequest.Experiments, true);
                                    }
                                    else
                                    {
                                        availability.AvailableAwardMilesWithDesc = string.Format("Mileage balance: {0} ", ShopStaticUtility.GetThousandPlaceCommaDelimitedNumberString(mileageBalance.Value));
                                        // populate AvailableAwardMilesWithDesc
                                        GetMilesDescWithFareDiscloser(availability, session, false, shopRequest.Experiments, false);

                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        {
                            //_logger.LogError("GetAvailability - Assigning mileage plus balance {@Exception}", JsonConvert.SerializeObject(ex));
                            _logger.LogError("GetAvailability - Assigning mileage plus balance {@Exception}", JsonConvert.SerializeObject(ex));
                        }
                    }
                }

                availability.IsMoneyAndMilesEligible = await _shoppingUtility.IsMoneyPlusmilesEligible(response, shopRequest.Application, session?.CatalogItems);

                #endregion

                #region FSR Result Handler
                if (_shoppingUtility.EnableFSRAlertMessages(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.TravelType))
                {
                    try
                    {
                        bool isLastShopCallFromUI = !(shopRequest.GetNonStopFlightsOnly && !shopRequest.GetFlightsWithStops && response.Trips[0].UseFilters);

                        bool shouldHandleFRS = false;
                        if (_configuration.GetValue<bool>("EnableMetroCodeFixForMultiTrip"))
                        {
                            shouldHandleFRS = !shopRequest.IsReshopChange
                                               && ("OW,RT,MD".IndexOf(shopRequest.SearchType.ToString()) > -1 ||
                                               (_configuration.GetValue<bool>("FSRForceToGSTSwitch") && response.Trips[0].Flights != null && response.Trips[0].Flights.Count > 0 && response.Trips[0].Flights[0].OriginCountryCode.ToUpper().Equals("IN"))) // For India GST
                                               && isLastShopCallFromUI; // only populate alert messages on the last shop call
                        }
                        else
                        {
                            shouldHandleFRS = !shopRequest.IsReshopChange
                                               && ("OW,RT".IndexOf(shopRequest.SearchType.ToString()) > -1 ||
                                               (_configuration.GetValue<bool>("FSRForceToGSTSwitch") && response.Trips[0].Flights != null && response.Trips[0].Flights.Count > 0 && response.Trips[0].Flights[0].OriginCountryCode.ToUpper().Equals("IN"))) // For India GST
                                               && isLastShopCallFromUI; // only populate alert messages on the last shop call
                        }

                        if (shouldHandleFRS)
                        {
                            availability.FSRAlertMessages = await HandleFlightShoppingThatHasResults(response, shopRequest, isShop);
                        }

                        #region Young Adult messaging - only show if no FSRAlertMessages
                        if (_shoppingUtility.EnableYoungAdult(shopRequest.IsReshop) && isLastShopCallFromUI)
                        {
                            if (availability.FSRAlertMessages == null || availability.FSRAlertMessages.Count == 0 || !availability.FSRAlertMessages.Any())
                            {
                                availability.InfoMessages = GetYAInfoMsg(availability, response);
                            }
                        }
                        #endregion Young Adult messaging

                        availability.FSRAlertMessages = AddMandatoryFSRAlertMessages(shopRequest, availability.FSRAlertMessages);

                        if (_configuration.GetValue<bool>("EnableCorporateLeisure") && session.TravelType == TravelType.CLB.ToString())
                        {
                            if (availability.FSRAlertMessages == null)
                            {
                                availability.FSRAlertMessages = new List<MOBFSRAlertMessage>();
                            }
                            var corporateLeisureOptOutAlert = await GetCorporateLeisureOptOutFSRAlert(shopRequest, session);
                            if (corporateLeisureOptOutAlert != null)
                            {
                                availability.FSRAlertMessages.Add(corporateLeisureOptOutAlert);
                            }
                        }
                        if (_omniCart.IsEnableOmniCartRetargetingChanges(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest?.CatalogItems) && !string.IsNullOrEmpty(shopRequest.Flow) && shopRequest.Flow?.ToUpper() == FlowType.OMNICARTDEEPLINK.ToString())
                        {
                            if (availability.FSRAlertMessages == null)
                            {
                                availability.FSRAlertMessages = new List<MOBFSRAlertMessage>();
                            }
                            MOBFSRAlertMessage OmnicartDeeeplinkFlightNotAvailableAlert = new MOBFSRAlertMessage
                            {
                                BodyMessage = _configuration.GetValue<string>("OmnicarRetargetingFSRAlertMessage"),
                                MessageType = 0,
                                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                                IsAlertExpanded = false,
                            };
                            availability.FSRAlertMessages.Add(OmnicartDeeeplinkFlightNotAvailableAlert);
                        }
                        await SetFSRTravelTypeAlertMessage(availability, session).ConfigureAwait(false);

                    }
                    catch (Exception ex)
                    {
                        {
                            //_logger.LogError("GetAvailability - HandleFlightShoppingThatHasResults {@Exception}", JsonConvert.SerializeObject(ex));
                            _logger.LogError("GetAvailability - HandleFlightShoppingThatHasResults {@Exception}", JsonConvert.SerializeObject(ex));
                        }
                    }
                    try
                    {
                        if (await _featureSettings.GetFeatureSettingValue("EnableOfferCodeFastFollowerChanges").ConfigureAwait(false))
                        {
                            availability.Warnings = _shoppingUtility.AddWarnings(response);
                            if (shopRequest.GetFlightsWithStops && await _shoppingUtility.IsNoDiscountFareAvailbleinShop1(shopRequest.SessionId).ConfigureAwait(false))
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
                    }
                    catch (Exception ex)
                    {
                        _logger.ILoggerError("shop - NoDiscountedFareAlert sessionId {request}, Exception {exception}", shopRequest, ex);
                    }
                }

                #endregion
                if (session.IsFSRRedesign)
                {
                    if (shopRequest.GetFlightsWithStops)
                    {
                        int nonStopColumnCount = 0;

                        MOBSHOPAvailability nonStopsAvailability = await GetLastTripAvailabilityFromPersist(1, shopRequest.SessionId);

                        if (nonStopsAvailability != null &&
                            nonStopsAvailability.Trip != null && nonStopsAvailability.Trip.Columns != null && nonStopsAvailability.Trip.Columns.Count > 0)
                        {
                            nonStopColumnCount = nonStopsAvailability.Trip.Columns.Count;
                            if(await _featureSettings.GetFeatureSettingValue("EnableSpecialMemberPricingFix").ConfigureAwait(false) 
                                && additionalItems.StrikeThroughPricing == false 
                                && nonStopsAvailability.ContentMessages?.Count > 0 
                                && nonStopsAvailability.ContentMessages.Any(c=>c.LocationCode == "AwardSpecialMemberPricingToolTip"))
                            {
                                if(availability.ContentMessages == null)
                                {
                                    availability.ContentMessages = new List<MOBMobileCMSContentMessages>();
                                }
                                availability.ContentMessages.Add(nonStopsAvailability.ContentMessages.FirstOrDefault(c => c.LocationCode == "AwardSpecialMemberPricingToolTip"));
                            }
                        }
                        //if (nonStopColumnCount!=availability.Trip.Columns.Count || availability.Trip.Columns.Count != response.Trips[0].ColumnInformation.Columns.Count)
                        if (nonStopColumnCount < availability.Trip.Columns.Count)
                        {
                            if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == TravelType.TPSearch.ToString())
                            {
                                availability.IsShopRefreshRequired = false;
                            }
                            else
                            {
                                availability.IsShopRefreshRequired = true;
                            }
                            if (_configuration.GetValue<bool>("EnableColumnSelectionFsrRedesign") && nonStopColumnCount > 0)
                            {
                                string columnID = nonStopsAvailability.Trip.Columns.FirstOrDefault(col => col.IsSelectedCabin)?.ColumnID ?? nonStopsAvailability.Trip.Columns[0].ColumnID;
                                if (_configuration.GetValue<bool>("EnableSequenceContainsIssueFixMB28334"))
                                {
                                    var selectedColumn = availability.Trip.Columns.FirstOrDefault(c => c.ColumnID.Equals(columnID));
                                    if (selectedColumn != null) { selectedColumn.IsSelectedCabin = true; }
                                }
                                else
                                {
                                    availability.Trip.Columns.First(c => c.ColumnID.Equals(columnID)).IsSelectedCabin = true;
                                }
                            }
                        }
                        else if (nonStopColumnCount > availability.Trip.Columns.Count)
                        {
                            availability.Trip.Columns = nonStopsAvailability.Trip.Columns;
                        }
                        if (_configuration.GetValue<bool>("SuppressFSREPlusColumn") && availability.Trip.DisableEplus)
                        {
                            //When NonStop columns are more than StopShop we over StopShop Columns in above condition, When DisableEplus Flag in true we need to remove Eplus column 
                            availability.Trip.Columns = availability.Trip.Columns.Where(c => (c.Type != _configuration.GetValue<string>("Economy_Merch_Eplus")
                                                                                            && c.Type != _configuration.GetValue<string>("Economy_Refundable_Merch_Eplus"))).ToList();

                            availability.IsShopRefreshRequired = true;
                        }
                        else if (_configuration.GetValue<bool>("SuppressFSREPlusColumn") && availability.Trip.DisableEplus == false)
                        {
                            //Add Eplus column back if Stop call doesn't return column and DisableEplus is false
                            if (nonStopsAvailability.Trip.Columns.Any(c => c.Type == _configuration.GetValue<string>("Economy_Merch_Eplus")
                                                                                             || c.Type == _configuration.GetValue<string>("Economy_Refundable_Merch_Eplus"))
                                && availability.Trip.Columns.Any(c => c.Type == _configuration.GetValue<string>("Economy_Merch_Eplus")
                                                                                           || c.Type == _configuration.GetValue<string>("Economy_Refundable_Merch_Eplus")) == false)
                            {
                                var economyIndex = availability.Trip.Columns.FindIndex(c => c.Type == _configuration.GetValue<string>("EconomyType") || c.Type == _configuration.GetValue<string>("EconomyRefundableType"));

                                availability.Trip.Columns.InsertRange(economyIndex + 1, nonStopsAvailability.Trip.Columns.Where(c => c.Type == _configuration.GetValue<string>("Economy_Merch_Eplus")
                                                                                            || c.Type == _configuration.GetValue<string>("Economy_Refundable_Merch_Eplus")).ToList());

                                availability.IsShopRefreshRequired = true;
                            }
                        }
                    }
                    await StrikeThroughContentMessages(availability, additionalItems, session, shopRequest);
                }

                if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == MOBTripPlannerType.TPSearch.ToString())
                {
                    availability.MaxTPFlightSelectCount = Convert.ToInt32(_configuration.GetValue<string>("TPMaxFlightSelectCountSearchFlow"));

                    availability.Trip.FlattenedFlights.ForEach(ff => ff.Flights.ForEach(f => f.ShoppingProducts = f.ShoppingProducts?.Where(p => p.IsSelectedCabin).ToList()));
                }

                if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && session.CatalogItems != null && session.CatalogItems.Count > 0 &&
                                   session.CatalogItems.FirstOrDefault(a => a.Id == ((int)Utility.Enum.IOSCatalogEnum.EnableNewPartnerAirlines).ToString() || a.Id == ((int)Utility.Enum.AndroidCatalogEnum.EnableNewPartnerAirlines).ToString())?.CurrentValue == "1"
                                  )
                {


                    availability.FareComparisonMessage = PopulateFareComparisonMessageforAirlines(availability, additionalItems, lstMessages);
                }


            }
            else
            {
                #region FSR No Result Handler

                List<MOBFSRAlertMessage> alertMessages = null;
                Session session = new Session();
                // Only handle this there is mp flights found but no due to CSL FS service is not downn
                // MajorCode="20003.01"; MinorCode="10038"; Message="FLIGHTS NOT FOUND"
                if (response != null && response.Errors != null && response.Errors.Exists(p => p.MajorCode == "20003.01" && p.MinorCode == "10038"))
                {
                    session = await _sessionHelperService.GetSession<Session>(shopRequest.SessionId, session.ObjectName, new List<string> { shopRequest.SessionId, session.ObjectName });
                    #region AwardNoFlightExceptionMsg
                    // For Award flow intially it was throwing exception, Per MOBILE-19271 this should work same as Revenue flow when no Flight found
                    if (session.IsFSRRedesign == false || _shoppingUtility.IsAwardFSRRedesignEnabled(shopRequest.Application.Id, shopRequest.Application.Version.Major) == false)
                        await AwardNoFlightExceptionMsg(shopRequest);
                    #endregion AwardNoFlightExceptionMsg

                    if (_shoppingUtility.EnableFSRAlertMessages(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.TravelType) && !IsTripPlanSearch(shopRequest.TravelType))
                    {
                        try
                        {
                            bool isLastShopCallFromUI = !(shopRequest.GetNonStopFlightsOnly && !shopRequest.GetFlightsWithStops && response.Trips[0].UseFilters);

                            MOBSHOPAvailability avail = null;

                            #region 
                            // Srini and Praveen - if HandleNoPersistFileInCouchForLatestShopAvailabilityResponse toggle is 'true'
                            // Do not call the LastTripAvailabilityFromPersistForFirstShopCall if it is first shop call so that we do not log the exception 'Couchbase Get unsuccessful - Not Found|Not Found - Session Object Name - United.Persist.Definition.Shopping.LatestShopAvailabilityResponse '

                            bool dontCallLastTripAvailabilityFromPersistForFirstShopCall = _configuration.GetValue<bool>("DonotCallLastTripAvailabilityFromPersistForFirstShopCall");

                            if (dontCallLastTripAvailabilityFromPersistForFirstShopCall == false
                                || (dontCallLastTripAvailabilityFromPersistForFirstShopCall == true && isFirstShopCall == false))
                            {
                                avail = await GetLastTripAvailabilityFromPersist(1, shopRequest.SessionId);
                            }
                            #endregion

                            bool shouldHandleFRS = false;
                            if (_configuration.GetValue<bool>("EnableMetroCodeFixForMultiTrip"))
                            {
                                shouldHandleFRS = !shopRequest.IsReshopChange
                                                    && ("OW,RT,MD".IndexOf(shopRequest.SearchType.ToString()) > -1 ||
                                                    (_configuration.GetValue<bool>("FSRForceToGSTSwitch") && response.Trips[0].Flights != null && response.Trips[0].Flights.Count > 0 && response.Trips[0].Flights[0].OriginCountryCode.ToUpper().Equals("IN"))) // For India GST)
                                                    && isLastShopCallFromUI // only populate alert messages on the last shop call
                                                    && (avail == null || !avail.Trip.FlattenedFlights.Any());
                            }
                            else
                            {
                                shouldHandleFRS = !shopRequest.IsReshopChange
                                                    && ("OW,RT".IndexOf(shopRequest.SearchType.ToString()) > -1 ||
                                                    (_configuration.GetValue<bool>("FSRForceToGSTSwitch") && response.Trips[0].Flights != null && response.Trips[0].Flights.Count > 0 && response.Trips[0].Flights[0].OriginCountryCode.ToUpper().Equals("IN"))) // For India GST)
                                                    && isLastShopCallFromUI // only populate alert messages on the last shop call
                                                    && (avail == null || !avail.Trip.FlattenedFlights.Any());
                            }


                            if (shouldHandleFRS)
                            {
                                alertMessages = await HandleFlightShoppingThatHasNoResults(response, shopRequest, isShop);
                            }
                        }
                        catch (Exception ex)
                        {
                            {
                                //_logger.LogError("GetAvailability - HandleFlightShoppingThatHasNoResults {@Exception}", JsonConvert.SerializeObject(ex));
                                _logger.LogError("GetAvailability - HandleFlightShoppingThatHasNoResults {@Exception}", JsonConvert.SerializeObject(ex));
                            }
                        }
                    }
                }

                #endregion

                if (!_shoppingUtility.EnableFSRAlertMessages(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.TravelType) || (alertMessages == null || !alertMessages.Any())
                    || IsTripPlanSearch(shopRequest.TravelType))
                {
                    #region 

                    if (_configuration.GetValue<bool>("EnableNonStopFlight") && (shopRequest.GetFlightsWithStops || shopRequest.GetNonStopFlightsOnly) && response.Errors.Exists(p => p.MinorCode == "10038"))
                    {
                        throw new MOBUnitedException("10038");

                    }
                    if (response.Trips != null && response.Trips.Count > 0 && response.Trips[0].Flights != null && response.Trips[0].Flights.Count > 0)
                    {
                        if (response.Errors != null && response.Errors.Count > 0)
                        {
                            string errorMessage = string.Empty;
                            foreach (var error in response.Errors)
                            {
                                errorMessage = errorMessage + " " + error.Message;
                            }
                            errorMessage = errorMessage + "#" + response.CartId.ToString();
                            throw new System.Exception(errorMessage);
                        }
                        else
                        {
                            if (_configuration.GetValue<string>("Environment - ReShoppingPNRCall") != null && _configuration.GetValue<string>("Environment - ReShoppingPNRCall") == "STAGE")
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0") + "CartId: " + response.CartId);
                            }
                            else
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0"));
                            }
                        }
                    }
                    else
                    {
                        if (_configuration.GetValue<string>("Environment - ReShoppingPNRCall") != null && _configuration.GetValue<string>("Environment - ReShoppingPNRCall") == "STAGE")
                        {
                            throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0") + "CartId: " + response.CartId);
                        }
                        else
                        {
                            throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0"));
                        }
                    }
                    #endregion
                }
                else
                {
                    return await HandleNoFlightsFoundOptions(token, shopRequest, availability, response, alertMessages, session);
                }
            }

            if (_configuration.GetValue<bool>("EnableNonStopFlight"))
            {
                if (shopRequest.GetFlightsWithStops)
                {
                    MOBSHOPAvailability nonStopsAvailability = await GetLastTripAvailabilityFromPersist(1, shopRequest.SessionId);
                    availability.Trip.FlightCount = availability.Trip.FlightCount + nonStopsAvailability.Trip.FlightCount; //**nonstopchanges==> Need the total flight count of Non Stop + With Stops for Organize Results for Paging Call() when we return PageCount = 2 for FlightsWithStop Response.
                }
            }
            await AddAvailabilityToPersist(availability, shopRequest.SessionId, true);

            #region // 5 = After Populate Trip //****Get Call Duration Code - Venkat 03/17/2015*******
            //****Get Call Duration Code - Venkat 03/17/2015*******
            if (afterPopulateTripStopWatch.IsRunning)
            {
                afterPopulateTripStopWatch.Stop();
            }
            shopCSLCallDurations = shopCSLCallDurations + "|5=" + afterPopulateTripStopWatch.ElapsedMilliseconds.ToString() + "|"; // 5 = After Populate Trip

            //****Get Call Duration Code - Venkat 03/17/2015*******            
            availability.Trip.CallDurationText = availability.Trip.CallDurationText + shopCSLCallDurations;
            if (_configuration.GetValue<string>("CartIdForDebug").ToUpper() == "REST_TUNING_CALL_DURATION")
            {
                availability.Trip.CallDurationText = callTime4Tuning;
            }
            //****Get Call Duration Code - Venkat 03/17/2015*******            
            #endregion

            try
            {
                //removing Task.Factory converting to asyn call
                //await Task.Factory.StartNew(() => FireForGetGetRewardProgramsList(token, shopRequest));
                await FireForGetGetRewardProgramsList(token, shopRequest);
            }
            catch { }
            return await Task.FromResult(availability);
        }

        private async Task SetFSRTravelTypeAlertMessage(MOBSHOPAvailability availability, Session session)
        {
            if (await _featureSettings.GetFeatureSettingValue("EnableFSRETCTravelCreditsFeature").ConfigureAwait(false) && !string.IsNullOrEmpty(session.PricingType) && !string.IsNullOrEmpty(session.CreditsAmount))
            {
                if (availability.FSRAlertMessages == null)
                {
                    availability.FSRAlertMessages = new List<MOBFSRAlertMessage>();
                }
                availability.FSRAlertMessages.Add(new MOBFSRAlertMessage
                {
                    HeaderMessage = $"Total credit balance: ${session.CreditsAmount}",
                    BodyMessage = $"You have ${session.CreditsAmount} of applicable credits you can use for this fare.",
                    MessageType = 0,
                    AlertType = MOBFSRAlertMessageType.Information.ToString(),
                    IsAlertExpanded = true,
                    MessageTypeDescription = FSRAlertMessageType.CreditsRemoved,
                    Buttons = new List<MOBFSRAlertMessageButton> { new MOBFSRAlertMessageButton { ButtonLabel = "Apply credits", RedirectUrl = "Applycredits" } }

                });
                availability.FSRAlertMessages.Add(new MOBFSRAlertMessage
                {
                    HeaderMessage = $"Total credit balance: ${session.CreditsAmount}",
                    BodyMessage = $"We've applied ${session.CreditsAmount} of applicable credits towards your total fare, including all passengers.",
                    MessageType = 0,
                    AlertType = MOBFSRAlertMessageType.Success.ToString(),
                    IsAlertExpanded = true,
                    MessageTypeDescription = FSRAlertMessageType.CreditsApplied,
                    Buttons = new List<MOBFSRAlertMessageButton> { new MOBFSRAlertMessageButton { ButtonLabel = "Remove credits", RedirectUrl = "Removecredits" } }
                });
            }
        }


        public async Task<MOBSHOPAvailability> GetAvailabilityTripPlan(string token, MOBTripPlanShopHelper shopRequest, MOBSHOPTripPlanRequest sHOPTripPlanRequest, HttpContext httpContext = null)
        {
            MOBSHOPAvailability availability = null;
            string logAction = "GetAvailabilityTripPlan - " + shopRequest.MobShopRequest.TravelType;
            //string cslEndpoint = GetCslEndpointForShopping(false);

            //ShopResponse tripPlanShopResponse = new ShopResponse();
            ShopRequest request = await GetShopRequestTripPlan(shopRequest);

            //if (Utility.GetBooleanConfigValue("EnableOmniChannelCartMVP1"))
            //{
            //    // Need to check remaining references for shop request
            //    request.DeviceId = shopRequest.MobShopRequest.DeviceId;
            //    Guid cartid = new Guid(shopRequest.MobShopRequest.SessionId);
            //    request.CartId = cartid.ToString().ToUpper();
            //}
            string jsonRequest = null;
            string url;
            string jsonResponse = null;
            United.Services.FlightShopping.Common.ShopResponse response = null;
            Stopwatch afterPopulateTripStopWatch;


            //if (shopRequest.TripPlannerType == MOBSHOPTripPlannerType.Copilot || shopRequest.MobShopRequest.TravelType == TravelType.TPEdit.ToString())
            //{
            jsonRequest = Serialize(request);
            string shopCSLCallDurations = string.Empty;
            string callTime4Tuning = string.Empty;
            //if (traceSwitch.TraceWarning)
            //{
            //    logEntries.Add(LogEntry.GetLogEntry<string>(shopRequest.MobShopRequest.SessionId, "GetAvailability - Request for " + logAction, "Trace", shopRequest.MobShopRequest.Application.Id, shopRequest.MobShopRequest.Application.Version.Major, shopRequest.MobShopRequest.DeviceId, jsonRequest));
            //}

            //url = $"{cslEndpoint}/tripplanner/Shop";Quic

            //if (traceSwitch.TraceWarning)
            //{
            //    logEntries.Add(LogEntry.GetLogEntry<string>(shopRequest.MobShopRequest.SessionId, "GetAvailability - Request url for " + logAction, "Trace", shopRequest.MobShopRequest.Application.Id, shopRequest.MobShopRequest.Application.Version.Major, shopRequest.MobShopRequest.DeviceId, url));
            //}

            #region//****Get Call Duration Code - Venkat 03/17/2015*******

            Stopwatch shopCSLCallDurationstopwatch1;
            shopCSLCallDurationstopwatch1 = new Stopwatch();
            shopCSLCallDurationstopwatch1.Reset();
            shopCSLCallDurationstopwatch1.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            string action = $"/tripplanner/Shop";
            //url = $"{cslEndpoint}/tripplanner/Shop";

            var returnValue = await _shoppingClientService.PostAsync<United.Services.FlightShopping.Common.ShopResponse>(token, shopRequest.MobShopRequest?.SessionId, action, request);
            response = returnValue;

            //jsonResponse = HttpHelper.Post(url, "application/json; charset=utf-8", token, jsonRequest, httpPostTimeOut, httpPostNumberOfRetry);

            #region// 2 = shopCSLCallDurationstopwatch1//****Get Call Duration Code - Venkat 03/17/2015*******
            if (shopCSLCallDurationstopwatch1.IsRunning)
            {
                shopCSLCallDurationstopwatch1.Stop();
            }
            shopCSLCallDurations = shopCSLCallDurations + "|2=" + shopCSLCallDurationstopwatch1.ElapsedMilliseconds.ToString() + "|"; // 2 = shopCSLCallDurationstopwatch1
            callTime4Tuning = "|CSL =" + (shopCSLCallDurationstopwatch1.ElapsedMilliseconds / (double)1000).ToString();
            //if (traceSwitch.TraceError)
            //{
            //    logEntries.Add(LogEntry.GetLogEntry<string>(shopRequest.MobShopRequest.SessionId, "GetAvailabilityTripPlan - CSL Call Duration", "CSS/CSL-CallDuration", shopRequest.MobShopRequest.Application.Id, shopRequest.MobShopRequest.Application.Version.Major, shopRequest.MobShopRequest.DeviceId, "CSLShop=" + (shopCSLCallDurationstopwatch1.ElapsedMilliseconds / (double)1000).ToString()));
            //}

            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            //if (traceSwitch.TraceWarning)
            //{
            //    logEntries.Add(LogEntry.GetLogEntry<string>(shopRequest.MobShopRequest.SessionId, "GetAvailability - Response for " + logAction, "Trace", shopRequest.MobShopRequest.Application.Id, shopRequest.MobShopRequest.Application.Version.Major, shopRequest.MobShopRequest.DeviceId, jsonResponse));
            //}
            #region //****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch shopCSLCallDurationstopwatch2;
            shopCSLCallDurationstopwatch2 = new Stopwatch();
            shopCSLCallDurationstopwatch2.Reset();
            shopCSLCallDurationstopwatch2.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            //response = JsonSerializer.DeserializeUseContract<ShopResponse>(jsonResponse);

            #region // 3= JSON Deserialize //****Get Call Duration Code - Venkat 03/17/2015*******
            if (shopCSLCallDurationstopwatch2.IsRunning)
            {
                shopCSLCallDurationstopwatch2.Stop();
            }
            shopCSLCallDurations = shopCSLCallDurations + "|3=" + shopCSLCallDurationstopwatch2.ElapsedMilliseconds.ToString() + "|"; // 3= JSON Deserializew
            #endregion //****Get Call Duration Code - Venkat 03/17/2015*******
            //}

            //if (shopRequest.TripPlannerType == MOBSHOPTripPlannerType.Pilot && shopRequest.MobShopRequest.TravelType == TravelType.TPBooking.ToString())
            //{
            //      response = tripPlanShopResponse;
            //}


            List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(sHOPTripPlanRequest, shopRequest.MobShopRequest.SessionId, token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");


            if (response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && NoCSLExceptions(response.Errors))
            {
                response.LastTripIndexRequested = 1;
                #region "CSLSHOP=" //****Get Call Duration Code - Venkat 03/17/2015*******
                Stopwatch populateTripStopWatch;
                populateTripStopWatch = new Stopwatch();

                if (shopRequest.TripPlannerType == MOBSHOPTripPlannerType.Copilot)
                {
                    populateTripStopWatch.Reset();
                    populateTripStopWatch.Start();

                    shopCSLCallDurations = shopCSLCallDurations + "CSLSHOP=" + response.CallTimeDomain + "|";
                    callTime4Tuning = "ITA = " + response.CallTimeBBX + callTime4Tuning;
                }
                #endregion //****Get Call Duration Code - Venkat 03/17/2015*******

                availability = new MOBSHOPAvailability();
                if (shopRequest.MobShopRequest.TravelType == TravelType.TPEdit.ToString())
                {
                    await SaveCSLShopResponseForTripPlanner(shopRequest.MobShopRequest, response);
                    availability.MaxTPFlightSelectCount = _configuration.GetValue<int>("TPMaxFlightSelectCountEditAndFSRNFlow");
                    availability.HideFareWheel = true;
                    availability.TripPlanCartId = shopRequest.TripPlanCartId;
                    availability.TripPlanId = shopRequest.TripPlanId;
                }
                //else
                //{
                //    availability.MaxTPFlightSelectCount = Convert.ToInt32(Utility.GetConfigEntries("TPMaxFlightSelectCountSearchFlow"));
                //}
                availability.SessionId = shopRequest.MobShopRequest.SessionId;
                availability.CartId = response.CartId;
                availability.ResponseType = MOBAvailabiltyResponseType.Default.GetDescription();
                availability.TravelerCount = ShopStaticUtility.GetTravelerCount(shopRequest.MobShopRequest.TravelerTypes);
                bool isMultiTravelers = (request?.PaxInfoList?.Where(p => p.PaxType != PaxType.InfantLap).ToList().Count ?? 0) > 1;

                Session session = new Session();
                session = await _sessionHelperService.GetSession<Session>(shopRequest.MobShopRequest.SessionId, new Session().ObjectName, new List<string> { shopRequest.MobShopRequest.SessionId, new Session().ObjectName }).ConfigureAwait(false);


                await SetAvailabilityELFProperties(availability, isMultiTravelers, true);

                if (availability.ELFShopMessages?.Any(i => !string.IsNullOrEmpty(i.Id) && i.Id == "ELFConfirmFareTypeFooter_2020") ?? false)
                {
                    availability.ELFShopMessages.RemoveWhere(e => !string.IsNullOrEmpty(e.Id) && e.Id == "ELFConfirmFareTypeFooter");
                    availability.ELFShopMessages.RemoveWhere(e => !string.IsNullOrEmpty(e.Id) && e.Id == "ELFConfirmFareTypeFooter_19_20");
                    (availability.ELFShopMessages.Where(i => !string.IsNullOrEmpty(i.Id) && i.Id == "ELFConfirmFareTypeFooter_2020").FirstOrDefault().Id) = "ELFConfirmFareTypeFooter";
                }


                //if (string.IsNullOrEmpty(request.EmployeeDiscountId))
                //{
                //    availability.FareWheel = PopulateFareWheelDates(shopRequest.MobShopRequest.Trips, "SHOP");
                //}
                int tripIndex = 0;

                //-------Feature 208204--- Common class data carrier for hirarchy methds-----
                MOBSHOPDataCarrier _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                _mOBSHOPDataCarrier.SearchType = shopRequest.MobShopRequest.SearchType;
                if (_configuration.GetValue<bool>("Shopping - bPricingBySlice"))
                {
                    if (!availability.AwardTravel && !shopRequest.MobShopRequest.IsReshopChange)
                    {
                        availability.PriceTextDescription = GetPriceTextDescription(shopRequest.MobShopRequest.SearchType);
                        //availability.FSRFareDescription = GetFSRFareDescription(shopRequest.MobShopRequest);
                        SetFSRFareDescriptionForShop(availability, shopRequest.MobShopRequest);
                        // One time decide to assign text for all the products in the Flights. Will be using in BE & Compare Screens

                        if (IsTripPlanSearch(shopRequest.MobShopRequest.TravelType))
                        {
                            _mOBSHOPDataCarrier.PriceFormText = GetPriceFromText(shopRequest.MobShopRequest);
                        }
                        else
                            _mOBSHOPDataCarrier.PriceFormText = GetPriceFromText(shopRequest.MobShopRequest.SearchType);

                    }
                }

                //-----------
                MOBAdditionalItems additionalItems = new MOBAdditionalItems();
                availability.Trip = await PopulateTrip(_mOBSHOPDataCarrier, response.CartId, response.Trips, tripIndex, shopRequest.MobShopRequest.Trips[0].Cabin,
                    shopRequest.MobShopRequest.SessionId, shopRequest.MobShopRequest.Application.Id, shopRequest.MobShopRequest.DeviceId,
                    shopRequest.MobShopRequest.Application.Version.Major, shopRequest.MobShopRequest.ShowMileageDetails,
                    shopRequest.MobShopRequest.PremierStatusLevel, false, availability.AwardTravel, shopRequest.MobShopRequest.IsELFFareDisplayAtFSR,
                    shopRequest.MobShopRequest.GetNonStopFlightsOnly, shopRequest.MobShopRequest.GetFlightsWithStops, shopRequest.MobShopRequest, additionalItems,lstMessages:lstMessages, httpContext: httpContext);

                #region // 4 = popuplate trip  ****Get Call Duration Code - Venkat 03/17/2015*******
                //****Get Call Duration Code - Venkat 03/17/2015*******
                if (populateTripStopWatch.IsRunning)
                {
                    populateTripStopWatch.Stop();
                }
                shopCSLCallDurations = shopCSLCallDurations + "|4=" + populateTripStopWatch.ElapsedMilliseconds.ToString() + "|"; // 4 = popuplate trip,PopulateAwardCalendar,PopulateFareWheel
                afterPopulateTripStopWatch = new Stopwatch();
                afterPopulateTripStopWatch.Reset();
                afterPopulateTripStopWatch.Start();
                #endregion

                #region  //**NOTE**// Venkat - Nov 10,2014 For Oragainze Results
                availability.Trip.Cabin = shopRequest.MobShopRequest.Trips[0].Cabin;
                availability.Trip.LastTripIndexRequested = response.LastTripIndexRequested;
                #endregion

                #region Amenities
                //UpdateAmenitiesIndicatorsRequest amenitiesRequest = new UpdateAmenitiesIndicatorsRequest();
                //amenitiesRequest = GetAmenitiesRequest(response.CartId, response.Trips[0].Flights);
                //if (Utility.GetBooleanConfigValue("EnableNonStopFlight") && shopRequest.MobShopRequest.GetFlightsWithStops)
                //{
                //    var shopAmenitiesRequest = United.Persist.FilePersist.Load<United.Persist.Definition.Shopping.ShopAmenitiesRequest>(shopRequest.MobShopRequest.SessionId, (new United.Persist.Definition.Shopping.ShopAmenitiesRequest()).ObjectName);
                //    if (shopAmenitiesRequest != null && shopAmenitiesRequest.AmenitiesIndicatorsRequest != null && shopAmenitiesRequest.AmenitiesIndicatorsRequest.Count > 0)
                //    {
                //        var nonStopFlightsNumbers = shopAmenitiesRequest.AmenitiesIndicatorsRequest[response.LastTripIndexRequested.ToString()].FlightNumbers;
                //        if (nonStopFlightsNumbers != null && nonStopFlightsNumbers.Count > 0)
                //        {
                //            amenitiesRequest.FlightNumbers = amenitiesRequest.FlightNumbers.Concat(nonStopFlightsNumbers).ToCollection();
                //        }
                //    }
                //}
                //ShoppingExtend shopExtendDAL = new ShoppingExtend();
                //shopExtendDAL.AddAmenitiesRequestToPersist(amenitiesRequest, request.SessionId, response.LastTripIndexRequested.ToString());

                session.CartId = response.CartId;
                session.ShopSearchTripCount = request.Trips.Count;

                //if (!string.IsNullOrEmpty(request.EmployeeDiscountId))
                //{
                //    session.EmployeeId = request.EmployeeDiscountId;
                //}
                ShoppingResponse shop = new ShoppingResponse();
                shop = await _sessionHelperService.GetSession<ShoppingResponse>(shopRequest.MobShopRequest.SessionId, shop.ObjectName, new List<string> { shopRequest.MobShopRequest.SessionId, shop.ObjectName });
                shop.SessionId = shopRequest.MobShopRequest.SessionId;
                shop.CartId = availability.CartId;
                shop.PriceSummary = PopulatePriceSummary(response.PriceSummary);
                await _sessionHelperService.SaveSession<ShoppingResponse>(shop, shopRequest.MobShopRequest.SessionId, new List<string> { shopRequest.MobShopRequest.SessionId, shop.ObjectName }, shop.ObjectName);
                #endregion

                #region Corporate Booking

                //bool isCorporateBooking = Convert.ToBoolean(ConfigurationManager.AppSettings["CorporateConcurBooking"] ?? "false");

                //if (IsTravelArranger(shopRequest.MobShopRequest))
                //{
                //    session.IsArrangerBooking = true;
                //}


                //if (isCorporateBooking && shopRequest.MobShopRequest.IsCorporateBooking && shopRequest.MobShopRequest.MOBCPCorporateDetails != null && !string.IsNullOrEmpty(shopRequest.MobShopRequest.MOBCPCorporateDetails.CorporateCompanyName))
                //{
                //    availability.CorporateDisclaimer = string.Format(shopRequest.MobShopRequest.MOBCPCorporateDetails.CorporateCompanyName + " {0}", ConfigurationManager.AppSettings["CorporateDisclaimerText"] ?? string.Empty);
                //}
                #endregion

                await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName);
                if (!session.IsReshopChange)
                    SetTitleForFSRPage(availability, shopRequest.MobShopRequest);

                #region Mileage Balance

                //if (Utility.EnableMileageBalance(shopRequest.MobShopRequest.Application.Id, shopRequest.MobShopRequest.Application.Version.Major))
                //{
                //    try
                //    {
                //        if (request.AwardTravel && request.LoyaltyId != null)
                //        {
                //            Characteristic loyaltyId = response.Characteristics.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Code) && c.Code.Trim().Equals("LoyaltyId".Trim(), StringComparison.OrdinalIgnoreCase));
                //            if (loyaltyId != null && !string.IsNullOrWhiteSpace(loyaltyId.Value) && loyaltyId.Value.Equals(session.MileagPlusNumber))
                //            {
                //                Characteristic mileageBalance = response.Characteristics.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.Code) && c.Code.Trim().Equals("MilesBalance".Trim(), StringComparison.OrdinalIgnoreCase));
                //                if (mileageBalance != null && !string.IsNullOrWhiteSpace(mileageBalance.Value))
                //                {
                //                    availability.AvailableAwardMilesWithDesc = string.Format("Mileage balance: {0}", Utility.GetThousandPlaceCommaDelimitedNumberString(mileageBalance.Value));

                //                }
                //            }

                //            GetMilesDescWithFareDiscloser(availability, session, shopRequest.MobShopRequest.Experiments);
                //        }
                //    }
                //    catch (Exception ex)
                //    {
                //        if (traceSwitch.TraceError)
                //        {
                //            logEntries.Add(LogEntry.GetLogEntry(shopRequest.MobShopRequest.SessionId, "GetAvailability - Assigning mileage plus balance", "Exception", shopRequest.MobShopRequest.Application.Id, shopRequest.MobShopRequest.Application.Version.Major, shopRequest.MobShopRequest.DeviceId, new MOBExceptionWrapper(ex)));
                //        }
                //    }
                //}

                #endregion

                if (IsTripPlanSearch(session.TravelType)) // Will change if edit
                {
                    availability.TravelType = session.TravelType;
                    availability.Trip.FlattenedFlights.ForEach(ff => ff.Flights.ForEach(f => f.ShoppingProducts = f.ShoppingProducts?.Where(p => p.IsSelectedCabin).ToList()));
                }
                if (_configuration.GetValue<bool>("EnableAirlinesFareComparison"))
                {

                    availability.FareComparisonMessage = PopulateFareComparisonMessageforAirlines(availability, additionalItems, lstMessages);
                }
            }
            else
            {
                if (response.Errors != null && response.Errors.Count > 0)
                {
                    if (response.Errors.Exists(p => p?.MinorCode == "10038"))
                    {
                        throw new MOBUnitedException("10038");

                    }

                    string errorMessage = string.Empty;
                    foreach (var error in response.Errors)
                    {
                        errorMessage = errorMessage + " " + error.Message;
                    }
                    errorMessage = errorMessage + "#" + response.CartId?.ToString() ?? "";
                    throw new System.Exception(errorMessage);
                }
                else
                {
                    throw new Exception(_configuration.GetValue<string>("NoAvailabilityError2.0"));
                }

            }
            //if (traceSwitch.TraceWarning)
            //{
            //    logEntries.Add(LogEntry.GetLogEntry<MOBSHOPAvailability>(shopRequest.MobShopRequest.SessionId, "GetAvailabilityTripPlan - ClientResponse for" + logAction, "Trace", shopRequest.MobShopRequest.Application.Id, shopRequest.MobShopRequest.Application.Version.Major, shopRequest.MobShopRequest.DeviceId, availability, true, false));
            //}
            //if (shopRequest.MobShopRequest.TravelType != TravelType.TPBooking.ToString())
            //{
            //    if (Utility.GetBooleanConfigValue("EnableNonStopFlight")) // need to check if required for edit
            //    {
            //        if (shopRequest.MobShopRequest.GetFlightsWithStops)
            //        {
            //            MOBSHOPAvailability nonStopsAvailability = GetLastTripAvailabilityFromPersist(1, shopRequest.MobShopRequest.SessionId);
            //            availability.Trip.FlightCount = availability.Trip.FlightCount + nonStopsAvailability.Trip.FlightCount; //**nonstopchanges==> Need the total flight count of Non Stop + With Stops for Organize Results for Paging Call() when we return PageCount = 2 for FlightsWithStop Response.
            //        }
            //    }
            //}
            await AddAvailabilityToPersist(availability, shopRequest.MobShopRequest.SessionId, true); // need to check if required for edit
            #region // 5 = After Populate Trip //****Get Call Duration Code - Venkat 03/17/2015*******
            //****Get Call Duration Code - Venkat 03/17/2015*******
            if (afterPopulateTripStopWatch.IsRunning)
            {
                afterPopulateTripStopWatch.Stop();
            }
            shopCSLCallDurations = shopCSLCallDurations + "|5=" + afterPopulateTripStopWatch.ElapsedMilliseconds.ToString() + "|"; // 5 = After Populate Trip
            //****Get Call Duration Code - Venkat 03/17/2015*******    

            //****Get Call Duration Code - Venkat 03/17/2015*******            
            availability.Trip.CallDurationText = availability.Trip.CallDurationText + shopCSLCallDurations;
            if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "REST_TUNING_CALL_DURATION")
            {
                availability.Trip.CallDurationText = callTime4Tuning;
            }
            //****Get Call Duration Code - Venkat 03/17/2015*******            
            #endregion

            try
            {
                _ = await Task.Factory.StartNew(() => FireForGetGetRewardProgramsList(token, shopRequest.MobShopRequest));
            }
            catch { }
            return availability;
        }
        public void SetFSRFareDescriptionForShop(MOBSHOPAvailability availability, MOBSHOPShopRequest request)
        {
            availability.fSRFareDescription = GetFSRFareDescription(request, _shoppingUtility.EnableBagCalcSelfRedirect(request.Application.Id, request.Application.Version.Major));
            // MOBILE-14512
            if (_configuration.GetValue<bool>("EnableSortFilterEnhancements") &&
                _shoppingUtility.IsSortDisclaimerForNewFSR(request.Application.Id, request.Application.Version.Major))
            {
                availability.SortDisclaimerText = GetTextForSortDisclaimer(false);
            }
        }
        public bool IsStandardRevenueSearch(bool isCorporateBooking, bool isYoungAdultBooking, bool isAwardTravel,
                                                  string employeeDiscountId, string travelType, bool isReshop, string fareClass,
                                                  string promotionCode)
        {
            return !(isCorporateBooking || travelType == TravelType.CLB.ToString() || isYoungAdultBooking ||
                     isAwardTravel || !string.IsNullOrEmpty(employeeDiscountId) || isReshop ||
                     travelType == TravelType.TPSearch.ToString() || !string.IsNullOrEmpty(fareClass) ||
                     !string.IsNullOrEmpty(promotionCode));
        }

        public void SetSortDisclaimerForReshop(MOBSHOPAvailability availability, MOBSHOPShopRequest request)
        {
            if (!availability.AwardTravel && _configuration.GetValue<bool>("EnableSortFilterEnhancements"))
            {
                // MOBILE-14512
                if (_shoppingUtility.IsSortDisclaimerForNewFSR(request.Application.Id, request.Application.Version.Major))
                {
                    availability.SortDisclaimerText = GetTextForSortDisclaimer(false);
                }
                else
                {
                    var forAppend = !string.IsNullOrEmpty(availability.CorporateDisclaimer);
                    availability.CorporateDisclaimer = $"{availability.CorporateDisclaimer}{GetTextForSortDisclaimer(forAppend)}";

                }
            }
        }
        private string GetTextForSortDisclaimer(bool forAppend)
        {
            return $"{(forAppend ? "\n" : "")}{_configuration.GetValue<string>("AdditionalLegalDisclaimerText")}";
        }
        public bool EnableAdvanceSearchCouponBooking(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableAdvanceSearchCouponBooking") && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("AndroidAdvanceSearchCouponBookingVersion"), _configuration.GetValue<string>("iPhoneAdvanceSearchCouponBookingVersion"));
        }

        public async System.Threading.Tasks.Task AddAvailabilityToPersist(MOBSHOPAvailability availability, string sessionID, bool isCallFromShop = false)
        {
            #region Organize Resulst Shop Filters
            LatestShopAvailabilityResponse persistAvailability = new LatestShopAvailabilityResponse();
            persistAvailability = await _sessionHelperService.GetSession<LatestShopAvailabilityResponse>(sessionID, persistAvailability.ObjectName, new List<string> { sessionID, persistAvailability.ObjectName }).ConfigureAwait(false);
            if (persistAvailability != null && persistAvailability.AvailabilityList != null)
            {
                bool isexist = false;
                foreach (string key in persistAvailability.AvailabilityKeys)
                {
                    if (key == availability.Trip.LastTripIndexRequested.ToString())
                    {
                        var persistAvailabilityCopy = persistAvailability.AvailabilityList[availability.Trip.LastTripIndexRequested.ToString()];
                        persistAvailability.AvailabilityList[availability.Trip.LastTripIndexRequested.ToString()] = availability;
                        MOBSHOPFlattenedFlightList mobSHOPFlattenedFlightList = new MOBSHOPFlattenedFlightList();
                        if (isCallFromShop && _configuration.GetValue<bool>("EnableNonStopFlight") && persistAvailabilityCopy != null)
                        {
                            mobSHOPFlattenedFlightList.FlattenedFlightList = persistAvailability.AvailabilityList[availability.Trip.LastTripIndexRequested.ToString()].Trip.FlattenedFlights.Clone();
                            foreach (var previousFlattendeFlight in persistAvailabilityCopy.Trip.FlattenedFlights)
                            {
                                if (!mobSHOPFlattenedFlightList.FlattenedFlightList.Exists(p => p.FlightId == previousFlattendeFlight.FlightId))
                                {
                                    mobSHOPFlattenedFlightList.FlattenedFlightList.AddRange(persistAvailabilityCopy.Trip.FlattenedFlights);
                                }
                            }
                            persistAvailability.AvailabilityList[availability.Trip.LastTripIndexRequested.ToString()].Trip.FlightCount = mobSHOPFlattenedFlightList.FlattenedFlightList.Count;
                        }
                        await _sessionHelperService.SaveSession<MOBSHOPFlattenedFlightList>(mobSHOPFlattenedFlightList, sessionID, new List<string> { sessionID, mobSHOPFlattenedFlightList.ObjectName }, mobSHOPFlattenedFlightList.ObjectName).ConfigureAwait(false);

                        isexist = true;
                    }
                }
                if (!isexist)
                {
                    persistAvailability.AvailabilityList.Add(availability.Trip.LastTripIndexRequested.ToString(), availability);
                    persistAvailability.AvailabilityKeys.Add(availability.Trip.LastTripIndexRequested.ToString());
                }
            }
            else
            {
                persistAvailability = new LatestShopAvailabilityResponse();
                persistAvailability.SessionId = sessionID;
                persistAvailability.CartId = availability.CartId;
                if (persistAvailability.AvailabilityList == null)
                {
                    persistAvailability.AvailabilityList = new SerializableDictionary<string, MOBSHOPAvailability>();
                    persistAvailability.AvailabilityKeys = new List<string>();
                }
                if (availability.Trip != null)
                {
                    persistAvailability.AvailabilityList.Add(availability.Trip.LastTripIndexRequested.ToString(), availability);
                    persistAvailability.AvailabilityKeys.Add(availability.Trip.LastTripIndexRequested.ToString());
                }
            }

            await _sessionHelperService.SaveSession<LatestShopAvailabilityResponse>(persistAvailability, sessionID, new List<string> { sessionID, persistAvailability.ObjectName }, persistAvailability.ObjectName).ConfigureAwait(false);
            if (_configuration.GetValue<bool>("EnableShoppingProductPersist"))
            {
                await AddColumnListToPersit(availability, sessionID);
            }
            #endregion
        }
        private async System.Threading.Tasks.Task AddColumnListToPersit(MOBSHOPAvailability availability, string sessionID)
        {
            MOBSHOPShoppingProductList persistShoppingProduct = new MOBSHOPShoppingProductList();
            persistShoppingProduct = await _sessionHelperService.GetSession<MOBSHOPShoppingProductList>(sessionID, persistShoppingProduct.ObjectName, new List<string> { sessionID, persistShoppingProduct.ObjectName }).ConfigureAwait(false);
            if (persistShoppingProduct == null)
            {
                persistShoppingProduct = new MOBSHOPShoppingProductList();
            }
            persistShoppingProduct.CartId = availability.CartId;
            persistShoppingProduct.SessionId = sessionID;
            persistShoppingProduct.Columns = availability.Trip.Columns;
            await _sessionHelperService.SaveSession<MOBSHOPShoppingProductList>(persistShoppingProduct, sessionID, new List<string> { sessionID, persistShoppingProduct.ObjectName }, persistShoppingProduct.ObjectName).ConfigureAwait(false);

        }

        private async System.Threading.Tasks.Task FireForGetGetRewardProgramsList(string token, MOBSHOPShopRequest shopRequest)
        {
            #region
            var rewardProgram = await _cachingService.GetCache<List<RewardProgram>>(_configuration.GetValue<string>("FrequestFlyerRewardProgramListStaticGUID") + "Booking2.0FrequentFlyerList", shopRequest.TransactionId);
            var rewardProgramList = JsonConvert.DeserializeObject<List<RewardProgram>>(rewardProgram);


            if (rewardProgramList == null || rewardProgramList.Count == 0)
            {

                rewardProgramList = await GetRewardPrograms(shopRequest.Application.Id, shopRequest.DeviceId, shopRequest.Application.Version.Major, shopRequest.TransactionId, shopRequest.SessionId, token);
                await _cachingService.SaveCache<List<RewardProgram>>(_configuration.GetValue<string>("FrequestFlyerRewardProgramListStaticGUID") + "Booking2.0FrequentFlyerList", rewardProgramList, shopRequest.TransactionId, new TimeSpan(1, 30, 0));

            }
            #endregion
        }

        public static string GetFormatedUrl(string url, string scheme, string relativePath, bool ensureSSL = false)
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

        private async Task<ShopRequest> GetShopRequest(MOBSHOPShopRequest MOBShopShopRequest, bool isShopRequest, CorporateDirect.Models.CustomerProfile.CorpPolicyResponse corporateTravelPolicy = null)
        {
            ShopRequest shopRequest = new ShopRequest();
            shopRequest.RememberedLoyaltyId = MOBShopShopRequest.MileagePlusAccountNumber;
            shopRequest.LoyaltyId = MOBShopShopRequest.MileagePlusAccountNumber;
            shopRequest.ChannelType = _configuration.GetValue<string>("Shopping - ChannelType");
            shopRequest.AccessCode = _configuration.GetValue<string>("AccessCode - CSLShopping");
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
                //*Import
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
            if (!_configuration.GetValue<bool>("ByPassAmenities"))
            {
                bool.TryParse(_configuration.GetValue<string>("IncludeAmenities"), out includeAmenities);
            }
            shopRequest.IncludeAmenities = includeAmenities;

            if (_configuration.GetValue<bool>("EnableFSRBasicEconomyToggleOnBookingMain") /*Master toggle to hide the be column */
                && GeneralHelper.IsApplicationVersionGreaterorEqual(MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, _configuration.GetValue<string>("FSRBasicEconomyToggleOnBookingMainAndroidversion"), _configuration.GetValue<string>("FSRBasicEconomyToggleOnBookingMainiOSversion")) /*Version check for latest client changes which hardcoded IsELFFareDisplayAtFSR to true at Shop By Map*/
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
                 (MOBShopShopRequest.Trips[0].SearchFiltersIn?.RefundableFaresToggle == null && MOBShopShopRequest.FareType == _strFARETYPEFULLYREFUNDABLE)))
            {
                shopRequest.FareType = _configuration.GetValue<string>("RefundableFaresToggleFareType");

                if (shopRequest.Characteristics == null) shopRequest.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                shopRequest.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "filterflightsbytoggle", Value = "true" });
            }

            // Mixed Cabin toggle feature
            if (_shoppingUtility.IsMixedCabinFilerEnabled(MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major) &&
                MOBShopShopRequest.AwardTravel && MOBShopShopRequest.IsReshopChange == false &&
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
                shopRequest.FlexibleDaysBefore = _configuration.GetValue<string>("ShopFareWheelFlexibleDaysBefore") == null ? 0 : _configuration.GetValue<int>("ShopFareWheelFlexibleDaysBefore"); //getFlexibleDaysBefore();
                shopRequest.FlexibleDaysAfter = _configuration.GetValue<string>("ShopFareWheelFlexibleDaysAfter") == null ? 0 : _configuration.GetValue<int>("ShopFareWheelFlexibleDaysAfter");   //getFlexibleDaysAfter();
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
                var reShopTrip = await GetReshopTripsList(MOBShopShopRequest);
                shopRequest.Trips = reShopTrip.trip;
                var isOverride24HrFlex = reShopTrip.isOverride24HrFlex;
                shopRequest.CabinPreferenceMain = GetCabinPreference(shopRequest.Trips[0].CabinType);

                if (_configuration.GetValue<bool>("EnableReshopOverride24HrFlex") && reShopTrip.isOverride24HrFlex)
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
                shopRequest.Trips.Add(trip);
                shopRequest.CabinPreferenceMain = GetCabinPreference(trip.CabinType);

                if (MOBShopShopRequest.Trips.Count > 1)
                {
                    trip = GetTrip(MOBShopShopRequest.Trips[1].Origin, MOBShopShopRequest.Trips[1].Destination, MOBShopShopRequest.Trips[1].DepartDate, MOBShopShopRequest.Trips[1].Cabin, MOBShopShopRequest.Trips[1].UseFilters, MOBShopShopRequest.Trips[1].SearchFiltersIn, MOBShopShopRequest.Trips[1].SearchNearbyOriginAirports, MOBShopShopRequest.Trips[1].SearchNearbyDestinationAirports,
                                 MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, isStandardRevenueSearch, MOBShopShopRequest.IsELFFareDisplayAtFSR, MOBShopShopRequest.FareType,
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
                                paxInfo = new PaxInfo();
                                paxInfo.PaxType = PaxType.Adult;
                                paxInfo.DateOfBirth = DateTime.Today.AddYears(-20).ToShortDateString();
                                AssignPTCValue(MOBShopShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < MOBShopShopRequest.NumberOfSeniors; i++)
                            {
                                paxInfo = new PaxInfo();
                                paxInfo.PaxType = PaxType.Senior;
                                paxInfo.DateOfBirth = DateTime.Today.AddYears(-67).ToShortDateString();
                                AssignPTCValue(MOBShopShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < MOBShopShopRequest.NumberOfChildren2To4; i++)
                            {
                                paxInfo = new PaxInfo();
                                paxInfo.PaxType = PaxType.Child01;
                                paxInfo.DateOfBirth = DateTime.Today.AddYears(-3).ToShortDateString();
                                AssignPTCValue(MOBShopShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < MOBShopShopRequest.NumberOfChildren5To11; i++)
                            {
                                paxInfo = new PaxInfo();
                                paxInfo.PaxType = PaxType.Child02;
                                paxInfo.DateOfBirth = DateTime.Today.AddYears(-8).ToShortDateString();
                                AssignPTCValue(MOBShopShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < MOBShopShopRequest.NumberOfChildren12To17; i++)
                            {
                                paxInfo = new PaxInfo();
                                paxInfo.PaxType = PaxType.Child03;
                                paxInfo.DateOfBirth = DateTime.Today.AddYears(-15).ToShortDateString();
                                AssignPTCValue(MOBShopShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < MOBShopShopRequest.NumberOfInfantOnLap; i++)
                            {
                                paxInfo = new PaxInfo();
                                paxInfo.PaxType = PaxType.InfantLap;
                                paxInfo.DateOfBirth = DateTime.Today.AddYears(-1).ToShortDateString();
                                AssignPTCValue(MOBShopShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);
                            }
                            for (int i = 0; i < MOBShopShopRequest.NumberOfInfantWithSeat; i++)
                            {
                                paxInfo = new PaxInfo();
                                paxInfo.PaxType = PaxType.InfantSeat;
                                paxInfo.DateOfBirth = DateTime.Today.AddYears(-1).ToShortDateString();
                                AssignPTCValue(MOBShopShopRequest, paxInfo);
                                shopRequest.PaxInfoList.Add(paxInfo);

                            }
                        }
                    }
                }
            }
            else if (MOBShopShopRequest.IsReshopChange)
            {
                var cslReservation = await _sessionHelperService.GetSession<ReservationDetail>(MOBShopShopRequest.SessionId, new ReservationDetail().GetType().FullName, new List<string> { MOBShopShopRequest.SessionId, new ReservationDetail().GetType().FullName });
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
                if ((MOBShopShopRequest.AwardTravel || _shoppingUtility.EnableEPlusAncillary(MOBShopShopRequest.Application.Id, MOBShopShopRequest.Application.Version.Major, MOBShopShopRequest.IsReshopChange)) && !string.IsNullOrWhiteSpace(MOBShopShopRequest.MileagePlusAccountNumber))
                {
                    if (_configuration.GetValue<bool>("AddingExtraLogggingForAwardShopAccountSummary_Toggle"))
                    {
                        if (!string.IsNullOrWhiteSpace(MOBShopShopRequest.SessionId))
                        {
                            getAccountSummaryTransactionID = MOBShopShopRequest.SessionId;
                        }
                    }
                    MPAccountSummary summary = await _mileagePlus.GetAccountSummary(getAccountSummaryTransactionID, MOBShopShopRequest.MileagePlusAccountNumber, "en-US", false, MOBShopShopRequest.SessionId);
                    shopRequest.LoyaltyPerson = new Service.Presentation.PersonModel.LoyaltyPerson();
                    shopRequest.LoyaltyPerson.LoyaltyProgramMemberID = summary.MileagePlusNumber;
                    shopRequest.LoyaltyPerson.LoyaltyProgramMemberTierLevel = (Service.Presentation.CommonEnumModel.LoyaltyProgramMemberTierLevel)summary.EliteStatus.Level;
                    shopRequest.LoyaltyPerson.AccountBalances = new Collection<Service.Presentation.CommonModel.LoyaltyAccountBalance>();
                    Service.Presentation.CommonModel.LoyaltyAccountBalance balance = new Service.Presentation.CommonModel.LoyaltyAccountBalance();
                    int bal = 0;
                    int.TryParse(summary.Balance, out bal);
                    balance.Balance = bal;
                    balance.BalanceType = Service.Presentation.CommonEnumModel.LoyaltyAccountBalanceType.MilesBalance;
                    shopRequest.LoyaltyPerson.AccountBalances.Add(balance);

                    // MOBILE-24412
                    if (_configuration.GetValue<bool>("EnableShopChaseCardPaxInfoFix") && summary.IsChaseCardHolder)
                    {
                        FixPTCCodeForChaseCardHolder(MOBShopShopRequest, shopRequest);
                    }
                    if(await _featureSettings.GetFeatureSettingValue("EnableAwardStrikeThroughPriceEnhancement").ConfigureAwait(false) && summary.IsChaseCardHolder)
                    {
                        shopRequest.IsPreferredBankCardHolder = true;
                    }

                }
                else if (MOBShopShopRequest.AwardTravel && string.IsNullOrWhiteSpace(MOBShopShopRequest.MileagePlusAccountNumber))
                {
                    if (_configuration.GetValue<bool>("AddingExtraLogggingForAwardShopAccountSummary_Toggle"))
                    {
                        _logger.LogError("GetShopRequest - AwardTravel_EmptyMpNumber {@MOBShopShopRequest}", JsonConvert.SerializeObject(MOBShopShopRequest));
                    }
                }
            }
            catch (Exception ex)
            {
                if (_configuration.GetValue<bool>("AddingExtraLogggingForAwardShopAccountSummary_Toggle"))
                {
                    _logger.LogError("GetShopRequest - GetAccountSummary {@Exception}", JsonConvert.SerializeObject(ex));
                }
            };
            //TODO - CHECK IF IT CAN BE REMOVED
            if (_configuration.GetValue<bool>("AddingExtraLogggingForAwardShopAccountSummary_Toggle") && _mileagePlus != null)
            {
                //   logEntries.AddRange(mp.LogEntries);
            }

            if (!string.IsNullOrEmpty(MOBShopShopRequest.EmployeeDiscountId))
            {
                shopRequest.EmployeeDiscountId = MOBShopShopRequest.EmployeeDiscountId;
            }
            shopRequest.NGRP = _configuration.GetValue<bool>("NGRPSwitchONOFFValue");
            #region
            AssignCalendarLengthOfStay(MOBShopShopRequest.LengthOfCalendar, shopRequest);
            #endregion
            //persis CSL shop request so we nave Loyalty info without making multiple summary calls
            CSLShopRequest cslShopRequest = new CSLShopRequest();
            if (MOBShopShopRequest.IsReshopChange)
            {
                shopRequest.ConfirmationID = MOBShopShopRequest.RecordLocator;
                shopRequest.DisableMostRestrictive = false;
                var cslReservation = await _sessionHelperService.GetSession<ReservationDetail>(MOBShopShopRequest.SessionId, new ReservationDetail().GetType().FullName, new List<string> { MOBShopShopRequest.SessionId, new ReservationDetail().GetType().FullName });
                if (cslReservation != null && cslReservation.Detail != null)
                    shopRequest.reservation = cslReservation.Detail;

                string riskFreePolicy24Hr = ShopStaticUtility.GetCharacteristicDescription(cslReservation.Detail.Characteristic.ToList(), "24HrFlexibleBookingPolicy");
                if (!string.IsNullOrEmpty(riskFreePolicy24Hr))
                {
                    shopRequest.RiskFreePolicy = riskFreePolicy24Hr;
                }
            }
            cslShopRequest.ShopRequest = shopRequest;

            await _sessionHelperService.SaveSession<CSLShopRequest>(cslShopRequest, MOBShopShopRequest.SessionId, new List<string> { MOBShopShopRequest.SessionId, cslShopRequest.ObjectName }, cslShopRequest.ObjectName, 30000);

            #region Corporate Booking
            bool isCorporateBooking = _configuration.GetValue<bool>("CorporateConcurBooking");
            if ((isCorporateBooking && MOBShopShopRequest.IsCorporateBooking) || MOBShopShopRequest.TravelType == TravelType.CLB.ToString())
            {
                shopRequest.CorporateTravelProvider = MOBShopShopRequest.MOBCPCorporateDetails.CorporateTravelProvider;
                shopRequest.CorporationName = MOBShopShopRequest.MOBCPCorporateDetails.CorporateCompanyName;
                shopRequest.SpecialPricingInfo = new SpecialPricingInfo();
                //TODO:Review with rajesh ..faregroupID is required for corporate lesiure
                if (_configuration.GetValue<bool>("EnableCorporateLeisure"))
                {
                    United.CorporateDirect.Models.CustomerProfile.Corporate corporateData = new CorporateDirect.Models.CustomerProfile.Corporate();
                    bool isEnablePassingTourCodeInCorporateFlowToFS = await _shoppingUtility.IsEnablePassingTourCodeToFSInCorporateFlow().ConfigureAwait(false);
                    if (isEnablePassingTourCodeInCorporateFlowToFS)
                    {
                        var _corprofileResponse = await _sessionHelperService.GetSession<United.CorporateDirect.Models.CustomerProfile.CorpProfileResponse>(MOBShopShopRequest.DeviceId + MOBShopShopRequest.MileagePlusAccountNumber, ObjectNames.CSLCorpProfileResponse, new List<string> { MOBShopShopRequest.DeviceId + MOBShopShopRequest.MileagePlusAccountNumber, ObjectNames.CSLCorpProfileResponse }).ConfigureAwait(false);

                        corporateData = _corprofileResponse != null ? _corprofileResponse?.Profiles?.FirstOrDefault()?.CorporateData : null;
                    }

                    if (!string.IsNullOrEmpty(MOBShopShopRequest.MOBCPCorporateDetails?.DiscountCode) && MOBShopShopRequest.TravelType != TravelType.CLB.ToString())
                    {
                        shopRequest.SpecialPricingInfo.PromoType = PromoType.CorporateDiscount;
                        var corpBookingTravelProgram = GetCorpBookingTravelProgram(corporateData, isEnablePassingTourCodeInCorporateFlowToFS, "Business");

                        if (corpBookingTravelProgram != null)
                        {
                            shopRequest.SpecialPricingInfo.TourCode = !string.IsNullOrEmpty(corpBookingTravelProgram.TourCode) ? corpBookingTravelProgram.TourCode : string.Empty;
                            shopRequest.SpecialPricingInfo.AccountCode = !string.IsNullOrEmpty(corpBookingTravelProgram.AccountCode) ? corpBookingTravelProgram.AccountCode : MOBShopShopRequest.MOBCPCorporateDetails.DiscountCode;

                            if (await _shoppingUtility.EnableGUIDAndUCSIDToFlightShoppingInCorporateFlow().ConfigureAwait(false))
                            {
                                shopRequest.SpecialPricingInfo.CorpGUID = !string.IsNullOrEmpty(corporateData.CompanyGuid) ? corporateData.CompanyGuid : string.Empty;
                                shopRequest.SpecialPricingInfo.CorpUCSID = corporateData.UCSID > 0 ? corporateData.UCSID.ToString() : string.Empty;
                            }
                        }
                        else
                            shopRequest.SpecialPricingInfo.AccountCode = MOBShopShopRequest.MOBCPCorporateDetails.DiscountCode;
                    }
                    else if (!string.IsNullOrEmpty(MOBShopShopRequest.MOBCPCorporateDetails?.LeisureDiscountCode) && MOBShopShopRequest.TravelType == TravelType.CLB.ToString())
                    {
                        shopRequest.SpecialPricingInfo.PromoType = PromoType.CorporateDiscountLeisure;
                        var corpBookingTravelProgram = GetCorpBookingTravelProgram(corporateData, isEnablePassingTourCodeInCorporateFlowToFS, "Personal");

                        if (corpBookingTravelProgram != null)
                        {
                            shopRequest.SpecialPricingInfo.TourCode = !string.IsNullOrEmpty(corpBookingTravelProgram.TourCode) ? corpBookingTravelProgram.TourCode : string.Empty;
                            shopRequest.SpecialPricingInfo.AccountCode = !string.IsNullOrEmpty(corpBookingTravelProgram.AccountCode) ? corpBookingTravelProgram.AccountCode : MOBShopShopRequest.MOBCPCorporateDetails.LeisureDiscountCode;
                            if (await _shoppingUtility.EnableGUIDAndUCSIDToFlightShoppingInCorporateFlow().ConfigureAwait(false))
                            {
                                shopRequest.SpecialPricingInfo.CorpGUID = !string.IsNullOrEmpty(corporateData.CompanyGuid) ? corporateData.CompanyGuid : string.Empty;
                                shopRequest.SpecialPricingInfo.CorpUCSID = corporateData.UCSID > 0 ? corporateData.UCSID.ToString() : string.Empty;
                            }
                        }
                        else
                            shopRequest.SpecialPricingInfo.AccountCode = MOBShopShopRequest.MOBCPCorporateDetails.LeisureDiscountCode;
                    }
                }
                else
                {
                    shopRequest.SpecialPricingInfo.AccountCode = MOBShopShopRequest.MOBCPCorporateDetails.DiscountCode;
                    shopRequest.SpecialPricingInfo.PromoType = PromoType.CorporateDiscount;
                }
                shopRequest.SpecialPricingInfo.FareGroupID = MOBShopShopRequest.MOBCPCorporateDetails.FareGroupId;// Passed from the GetProfile  call; XBE - Xclude BE; Null means include BE
                                                                                                                  // This Value is 10 to get CORPDISC "Corporate Discount Fare
                if (corporateTravelPolicy != null)
                    AssignTravelPolicy(shopRequest, MOBShopShopRequest, corporateTravelPolicy);

            }
            #endregion

            if (_configuration.GetValue<bool>("EnableNonStopFlight") && (MOBShopShopRequest.GetNonStopFlightsOnly || MOBShopShopRequest.GetFlightsWithStops))
            {
                await RequestForNonStopFlights(MOBShopShopRequest, shopRequest);
                if (MOBShopShopRequest.GetFlightsWithStops)
                {
                    Session session = new Session();
                    session = await _sessionHelperService.GetSession<Session>(MOBShopShopRequest.SessionId, session.ObjectName, new List<string> { MOBShopShopRequest.SessionId, session.ObjectName });
                    if (session == null)
                    {
                        throw new MOBUnitedException("Your session has expired. Please start a new search.");
                    }
                    shopRequest.CartId = session.CartId;
                }
            }
            return shopRequest;
        }

        private CorporateDirect.Models.CustomerProfile.TravelProgram GetCorpBookingTravelProgram(CorporateDirect.Models.CustomerProfile.Corporate corporateData, bool isEnablePassingTourCodeInCorporateFlowToFS, string travelProgramName)
        {
            return isEnablePassingTourCodeInCorporateFlowToFS && corporateData != null && corporateData.TravelPrograms != null ? corporateData.TravelPrograms.FirstOrDefault(x => !string.IsNullOrEmpty(x?.TravelProgramName) && x.TravelProgramName.Equals(travelProgramName, StringComparison.OrdinalIgnoreCase)) : null;
        }

        private void AssignTravelPolicy(ShopRequest cslShopRequest, MOBSHOPShopRequest mobShopRequest, United.CorporateDirect.Models.CustomerProfile.CorpPolicyResponse _corpPolicyResponse)
        {
            var corporateTravelPolicy = _corpPolicyResponse != null ? _corpPolicyResponse?.TravelPolicies?.FirstOrDefault() : null;

            if (corporateTravelPolicy != null)
            {
                cslShopRequest.TravelPolicies = new TravelPolicies()
                {

                    CountryCode = corporateTravelPolicy.CountryCode,
                    CurrencyCode = corporateTravelPolicy.CurrencyCode,
                    IsBasicEconomyAllowed = corporateTravelPolicy.IsBasicEconomyAllowed ?? false,
                    MaximumBudget = corporateTravelPolicy.MaximumBudget,
                    IsAirfare = corporateTravelPolicy.IsAirfare,
                    IsAirfarePlusTravelAddOn = corporateTravelPolicy.IsAirfarePlusTravelAddOn,
                    TravelCabinRestrictions = GetTravelCabinRestrictions(corporateTravelPolicy?.TravelCabinRestrictions)
                };
            }
        }
        private List<TravelCabinRestriction> GetTravelCabinRestrictions(List<United.CorporateDirect.Models.CustomerProfile.CorporateTravelCabinRestriction> corporateTravelCabinRestrictions)
        {
            List<TravelCabinRestriction> listTcr = new List<TravelCabinRestriction>();
            if (corporateTravelCabinRestrictions != null && corporateTravelCabinRestrictions.Count > 0)
            {
                corporateTravelCabinRestrictions?.ForEach(r =>
                    listTcr.Add(new TravelCabinRestriction()
                    {
                        Duration = r.Duration,
                        TripTypeCode = r.TripTypeCode,
                        IsEconomyAllowed = r.IsEconomyAllowed ?? false,
                        IsPremiumEconomyAllowed = r.IsPremiumEconomyAllowed ?? false,
                        IsBusinessFirstAllowed = r.IsBusinessFirstAllowed ?? false
                    }
                ));
            }
            return listTcr;
        }
        // MOBILE-24412
        private void FixPTCCodeForChaseCardHolder(MOBSHOPShopRequest mobShopShopRequest,
                                                  ShopRequest shopRequest)
        {
            var ptcCodeIndex = -1;
            var existingPTCCode = _configuration.GetValue<string>("PTCCodeToReplaceForStrikeThrough") ?? string.Empty;
            var newPTCCode = _configuration.GetValue<string>("PTCCodeToUseForStrikeThrough") ?? string.Empty;

            if (mobShopShopRequest.AwardTravel &&
                !string.IsNullOrEmpty(existingPTCCode) &&
                !string.IsNullOrEmpty(newPTCCode) &&
                (shopRequest?.PaxInfoList?.Count ?? 0) > 0)
            {
                foreach (PaxInfo paxInfo in shopRequest.PaxInfoList)
                {
                    ptcCodeIndex = paxInfo.PtcList?.IndexOf(existingPTCCode) ?? -1;
                    if (ptcCodeIndex != -1)
                    {
                        paxInfo.PtcList[ptcCodeIndex] = newPTCCode;
                    }
                }
            }
        }

        private bool IsEnableRefundableFaresToggle(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableRefundableFaresToggle") &&
                   GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("AndroidRefundableFaresToggleVersion"), _configuration.GetValue<string>("iPhoneRefundableFaresToggleVersion"));
        }

        private async Task<bool> RequestForNonStopFlights(MOBSHOPShopRequest mOBShopShopRequest, ShopRequest shopRequest)
        {
            if (_configuration.GetValue<bool>("EnableCodeRefactorForSavingSessionCalls") == false)
            {
                var session = await _sessionHelperService.GetSession<ShoppingResponse>(shopRequest.SessionId, (new ShoppingResponse()).ObjectName, new List<string> { shopRequest.SessionId, (new ShoppingResponse()).ObjectName });

                if (session != null && mOBShopShopRequest.GetFlightsWithStops)
                    shopRequest.CartId = session.CartId;
            }

            foreach (var trip in shopRequest.Trips)
            {
                trip.UseFilters = true;
                SetStopCountsToGetNonStopFlights(mOBShopShopRequest.GetNonStopFlightsOnly, mOBShopShopRequest.GetFlightsWithStops, trip.SearchFiltersIn);
                break; //**nonstopchanges==>  To set use filters bool as true only for Out Bound Segment as we go live for 17G with shop call selectrip later when working on select trip then remove this break
            }
            return true;
        }

        private void SetStopCountsToGetNonStopFlights(bool getNonStopFlights, bool getFlightsWithStops, SearchFilterInfo searchFiltersIn)
        {
            if ((!getNonStopFlights && !getFlightsWithStops) || (getNonStopFlights && getFlightsWithStops))
                return;

            if (searchFiltersIn == null)
                searchFiltersIn = new SearchFilterInfo();

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

        public async System.Threading.Tasks.Task AwardNoFlightExceptionMsg(MOBSHOPShopRequest shopRequest)
        {
            if (_configuration.GetValue<bool>("EnableAwardShopNoFlightsExceptionMsg"))
            {
                if (shopRequest.AwardTravel)
                {
                    var cmsCacheResponseKey = string.IsNullOrEmpty(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID")) ?
                                            "BookingPathRTI_CMSContentMessagesCached_StaticGUID_"
                                            : _configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                    string cmsCacheResponse = await _sessionHelperService.GetSession<string>(cmsCacheResponseKey, ObjectNames.MOBCSLContentMessagesResponseFullName, new List<string> { shopRequest.SessionId, ObjectNames.MOBCSLContentMessagesResponseFullName });
                    CSLContentMessagesResponse cmsResponse = new CSLContentMessagesResponse();
                    if (!string.IsNullOrEmpty(cmsCacheResponse))
                    {
                        cmsResponse = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(cmsCacheResponse);
                    }
                    if (cmsResponse == null || Convert.ToBoolean(cmsResponse.Status) == false || cmsResponse.Messages == null)
                    {
                        Session session = await _sessionHelperService.GetSession<Session>(shopRequest.SessionId, new Session().ObjectName, new List<string> { shopRequest.SessionId, new Session().ObjectName });

                        cmsResponse = await _travelerCSL.GetBookingRTICMSContentMessages(shopRequest, session);
                    }

                    if (cmsResponse != null && (Convert.ToBoolean(cmsResponse.Status) && cmsResponse.Messages != null) && cmsResponse.Messages.Any())
                    {
                        _logger.LogWarning("AwardNoFlightExceptionMsg - Cached - Response {Cached - @Response}", JsonConvert.SerializeObject(cmsResponse));
                        if (cmsResponse.Messages.Any(m => m.Title.ToUpper().Equals(_configuration.GetValue<string>("AwardShopNoFlightsExceptionTitle").ToUpper())))
                        {
                            CMSContentMessage exceptionMsg = cmsResponse.Messages.First(m => m.Title.ToUpper().Equals(_configuration.GetValue<string>("AwardShopNoFlightsExceptionTitle").ToUpper()));

                            if (!string.IsNullOrEmpty(exceptionMsg.Headline))
                                throw new MOBUnitedException(exceptionMsg.Headline + Environment.NewLine + Environment.NewLine + exceptionMsg.ContentFull);
                            else
                                throw new MOBUnitedException(exceptionMsg.ContentFull);
                        }
                    }
                    //throw new MOBUnitedException(@"There are no award flights on the date you selected.It's possible that because of reduced flight schedules, United or our partner airlines might not fly to some destinations or may only fly on certain days of the week.You can use the calendar below to Please select another date that may have availability.");
                }
            }
        }

        public void AssignCalendarLengthOfStay(int lengthOfCalendar, ShopRequest shopRequest)
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

        private List<PaxInfo> GetReshopPaxInfoList(MOBSHOPShopRequest mobShopShopRequest, ReservationDetail cslReservation)
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
                        PaxInfo paxInfo = new PaxInfo();
                        paxInfo.DateOfBirth = cslReservationTraveler.Person.DateOfBirth;
                        paxInfo.Key = cslReservationTraveler.Person.Key;
                        paxInfo.PaxTypeCode = cslReservationTraveler.Person.Type;
                        paxInfo.PaxType = GetRevenuePaxType(cslReservationTraveler.Person.Type);

                        paxInfoList.Add(paxInfo);
                    }
                }
            }
            return paxInfoList;
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

        private void AssignPTCValue(MOBSHOPShopRequest MOBShopShopRequest, PaxInfo paxInfo)
        {
            if (MOBShopShopRequest.AwardTravel)
            {
                if (_configuration.GetValue<bool>("NGRPAwardCalendarMP2017Switch") && MOBShopShopRequest.CustomerMetrics != null) //No null Check for PTCCode because we can pass empty string and CSL defaults PTCCode to PPR
                {
                    string ptcCode = string.Empty;
                    paxInfo.PtcList = new List<string>();
                    ptcCode = MOBShopShopRequest.CustomerMetrics.PTCCode ?? string.Empty;

                    paxInfo.PtcList.Add(ptcCode);
                }
            }
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

        private PaxInfo GetYAPaxInfo()
        {
            PaxInfo paxInfo = new PaxInfo();
            paxInfo.Characteristics = new List<United.Service.Presentation.CommonModel.Characteristic>();
            paxInfo.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "YOUNGADULT", Value = "True" });
            paxInfo.DateOfBirth = DateTime.Today.AddYears(-20).ToShortDateString();
            paxInfo.PaxType = PaxType.Adult;
            return paxInfo;
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

        private Trip GetTrip(string origin, string destination, string departureDate, string cabin, bool useFilters, MOBSearchFilters filters, bool searchNearbyOrigins, bool searchNearbyDestinations,
                             int appId = -1, string appVersion = "", bool isStandardRevenueSearch = false, bool isELFFareDisplayAtFSR = false, string fareType = "",
                             bool isUsed = false, int originAllAirports = -1, int destinationAllAirports = -1)
        {
            United.Services.FlightShopping.Common.Trip trip = null;

            if (!string.IsNullOrEmpty(origin) && !string.IsNullOrEmpty(destination) && origin.Length == 3 && destination.Length == 3 && IsValidDate(departureDate, true, isUsed))
            {
                trip = new United.Services.FlightShopping.Common.Trip();
                trip.Origin = origin;

                // MB-2639 add all airports flag to csl shop call
                if (_configuration.GetValue<bool>("EnableAllAirportsFlightSearch") && originAllAirports != -1 && destinationAllAirports != -1)
                {
                    trip.OriginAllAirports = originAllAirports == 1 ? true : false;
                    trip.DestinationAllAirports = destinationAllAirports == 1 ? true : false;
                }
                //if (_configuration.GetValue<sting>("CityCodeToReturnAllAirportsFlightSearch") != null && _configuration.GetValue<sting>("CityCodeToReturnAllAirportsFlightSearch").Contains(origin))
                //{
                //    trip.OriginAllAirports = true;
                //}
                //if (_configuration.GetValue<sting>("CityCodeToReturnAllAirportsFlightSearch") != null && _configuration.GetValue<sting>("CityCodeToReturnAllAirportsFlightSearch").Contains(destination))
                //{
                //    trip.DestinationAllAirports = true;
                //}

                trip.Destination = destination;
                trip.DepartDate = departureDate;
                if (searchNearbyDestinations)
                    trip.SearchRadiusMilesDestination = getSearchRadiusForNearbyAirports();
                if (searchNearbyOrigins)
                    trip.SearchRadiusMilesOrigin = getSearchRadiusForNearbyAirports();
                trip.CabinType = GetCabinType(cabin);
                trip.SearchFiltersIn = GetSearchFilters(filters, appId, appVersion, isStandardRevenueSearch, isELFFareDisplayAtFSR, fareType);
                trip.UseFilters = useFilters;

            }
            return trip;
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
                    foreach (var kvp in filters.AirportsDestinationList)
                    {
                        CodeDescPair cdp = new CodeDescPair();
                        cdp.Code = kvp.Key;
                        cdp.Description = kvp.Value ?? string.Empty;
                        cdp.Amount = kvp.Amount;
                        cdp.Currency = kvp.Currency ?? string.Empty;

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
                    foreach (var kvp in filters.AirportsOriginList)
                    {
                        CodeDescPair cdp = new CodeDescPair();
                        cdp.Code = kvp.Key;
                        cdp.Description = kvp.Value ?? string.Empty;
                        cdp.Amount = kvp.Amount;
                        cdp.Currency = kvp.Currency ?? string.Empty;

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
                    foreach (var kvp in filters.AirportsStopList)
                    {
                        CodeDescPair cdp = new CodeDescPair();
                        cdp.Code = kvp.Key;
                        cdp.Description = kvp.Value ?? string.Empty;
                        cdp.Amount = kvp.Amount;
                        cdp.Currency = kvp.Currency ?? string.Empty; ;

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
                    foreach (var kvp in filters.AirportsStopToAvoidList)
                    {
                        CodeDescPair cdp = new CodeDescPair();
                        cdp.Code = kvp.Key;
                        cdp.Description = kvp.Value ?? string.Empty;
                        cdp.Amount = kvp.Amount;
                        cdp.Currency = kvp.Currency ?? string.Empty; ;

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
                    foreach (var kvp in filters.CarriersMarketingList)
                    {
                        CodeDescPair cdp = new CodeDescPair();
                        cdp.Code = kvp.Key;
                        cdp.Description = kvp.Value ?? string.Empty;
                        cdp.Amount = kvp.Amount;
                        cdp.Currency = kvp.Currency ?? string.Empty;

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
                    foreach (var kvp in filters.CarriersOperatingList)
                    {
                        CodeDescPair cdp = new CodeDescPair();
                        cdp.Code = kvp.Key;
                        cdp.Description = kvp.Value ?? string.Empty;
                        cdp.Amount = kvp.Amount;
                        cdp.Currency = kvp.Currency ?? string.Empty; ;

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
                    foreach (var kvp in filters.EquipmentList)
                    {
                        CodeDescPair cdp = new CodeDescPair();
                        cdp.Code = kvp.Key;
                        cdp.Description = kvp.Value ?? string.Empty;
                        cdp.Amount = kvp.Amount;
                        cdp.Currency = kvp.Currency ?? string.Empty;

                        filter.EquipmentList.Add(cdp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.EquipmentTypes))
                {
                    filter.EquipmentTypes = filters.EquipmentTypes;
                }

                if (filters.FareFamilies != null && filters.FareFamilies.Count > 0)
                {
                    filter.FareFamilies = new sliceFareFamilies();
                    filter.FareFamilies.fareFamily = new fareFamilyType[filters.FareFamilies.Count];
                    int cnt = 0;
                    foreach (MOBSHOPFareFamily ff in filters.FareFamilies)
                    {

                        fareFamilyType fft = new fareFamilyType();
                        fft.fareFamily = string.IsNullOrEmpty(ff.FareFamily) ? "" : ff.FareFamily;
                        fft.maxMileage = ff.MaxMileage;
                        if (!string.IsNullOrEmpty(ff.MaxPrice))
                        {
                            fft.maxPrice = new price();
                            fft.maxPrice.amount = Convert.ToDecimal(ff.MaxPrice);
                        }
                        fft.minMileage = ff.MinMileage;
                        if (!string.IsNullOrEmpty(ff.MinPrice))
                        {
                            fft.minPrice = new price();
                            fft.minPrice.amount = Convert.ToDecimal(ff.MinPrice);
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
                    foreach (var warningFilter in filters.WarningsFilter)
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
        private void AddRefundableFaresToggleFilter(SearchFilterInfo shopfilter, MOBSearchFilters filters, int appId, string appVersion, bool isStandardRevenueSearch, bool isELFFareDisplayAtFSR, string fareType)
        {
            // Refundable fares toggle feature
            if (IsEnableRefundableFaresToggle(appId, appVersion) && isStandardRevenueSearch)
            {
                shopfilter.ShopIndicators = new ShopIndicators();

                shopfilter.ShopIndicators.IsBESelected = isELFFareDisplayAtFSR;

                if ((filters?.RefundableFaresToggle?.IsSelected ?? false) ||
                    (filters?.RefundableFaresToggle == null && fareType == _strFARETYPEFULLYREFUNDABLE))
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
            // MixedCabin toggle feature
            if (_shoppingUtility.IsMixedCabinFilerEnabled(appId, appVersion) && (fareType == _configuration.GetValue<string>("MixedCabinToggle") || fareType.ToLower() == "lf"))
            {
                if (shopfilter.ShopIndicators == null)
                    shopfilter.ShopIndicators = new ShopIndicators();

                if (filters == null || (filters?.AdditionalToggles?.FirstOrDefault(a => a.Key == _configuration.GetValue<string>("MixedCabinToggleKey"))?.IsSelected ?? false))
                    shopfilter.ShopIndicators.IsMixedToggleSelected = true;
                else
                    shopfilter.ShopIndicators.IsMixedToggleSelected = false;
            }
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

        private bool IsValidDate(string dateString, bool notPastDate, bool used = false)
        {
            bool result = false;

            if (used)
                return used;

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

        private async Task<(List<United.Services.FlightShopping.Common.Trip> trip, bool isOverride24HrFlex)> GetReshopTripsList(MOBSHOPShopRequest mobShopShopRequest)
        {
            var cslReservation = await _sessionHelperService.GetSession<ReservationDetail>(mobShopShopRequest.SessionId, new ReservationDetail().GetType().FullName, new List<string> { mobShopShopRequest.SessionId, new ReservationDetail().GetType().FullName });
            var persistedReservation = await _sessionHelperService.GetSession<Reservation>(mobShopShopRequest.SessionId, new Reservation().ObjectName, new List<string> { mobShopShopRequest.SessionId, new Reservation().ObjectName });

            List<United.Services.FlightShopping.Common.Trip> tripsList = new List<United.Services.FlightShopping.Common.Trip>();
            int segementNumber = 0;

            bool isOverride24HrFlex = (persistedReservation == null) ? false : persistedReservation.Override24HrFlex;
            bool enableLOFChangesForReshop = await _featureSettings.GetFeatureSettingValue("EnableLOFChangesForReshop") && !persistedReservation.IsPartiallyFlown;
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
                    mOBSHOPReShopTrip.ChangeTripTitle = _configuration.GetValue<string>("ReshopChange-RTIFlightBlockTitle");
                    tripRequestIndex++;
                }
                requestTripBase = usedTripBase;
                int requestTripIndex = Convert.ToInt32(cslReservation.Detail.FlightSegments.Min(p => p.TripNumber));

                foreach (var requestTrip in requestTripBase)
                {
                    tripsList.Add(GetTripForReshopChangeRequestUsingMobRequestAndCslReservationSegments(requestTrip, segementNumber, mobShopShopRequest.FareType, cslReservation.Detail.FlightSegments.ToList(), enableLOFChangesForReshop, persistedReservation.Reshop.IsUsedPNR, requestTripIndex));
                    segementNumber++;
                    requestTripIndex++;
                }

                await _sessionHelperService.SaveSession<Reservation>(persistedReservation, mobShopShopRequest.SessionId, new List<string> { mobShopShopRequest.SessionId, new Reservation().ObjectName }, new Reservation().ObjectName);

                return (tripsList, isOverride24HrFlex);
            }

            foreach (var requestTrip in mobShopShopRequest.Trips)
            {
                tripsList.Add(GetTripForReshopChangeRequestUsingMobRequestAndCslReservationSegments(requestTrip, segementNumber, mobShopShopRequest.FareType, cslReservation.Detail.FlightSegments.ToList(), enableLOFChangesForReshop));
                segementNumber++;

                if (persistedReservation.ReshopTrips.Count >= tripsList.Count)
                {
                    var mOBSHOPReShopTrip = persistedReservation.ReshopTrips[tripsList.Count - 1];
                    mOBSHOPReShopTrip.IsReshopTrip = (tripsList[tripsList.Count - 1].Flights == null || tripsList[tripsList.Count - 1].Flights.Count == 0);
                    mOBSHOPReShopTrip.ChangeTripTitle = _configuration.GetValue<string>("ReshopChange-RTIFlightBlockTitle");
                }
            }

            await _sessionHelperService.SaveSession<Reservation>(persistedReservation, mobShopShopRequest.SessionId, new List<string> { mobShopShopRequest.SessionId, new Reservation().ObjectName }, new Reservation().ObjectName);

            return (tripsList, isOverride24HrFlex);
        }

        private Trip GetTripForReshopChangeRequestUsingMobRequestAndCslReservationSegments(MOBSHOPTripBase mobSHOPTripBase, int segmentnumber, string fareType, List<ReservationFlightSegment> cslReservationFlightSegment, bool enableLOFChangesForReshop, bool isused = false, int originalTripIndex = 0)
        {
            United.Services.FlightShopping.Common.Trip trip = null;
            string segmentCabin;

            trip = GetTrip(mobSHOPTripBase.Origin, mobSHOPTripBase.Destination,
               mobSHOPTripBase.DepartDate, mobSHOPTripBase.Cabin,
               mobSHOPTripBase.UseFilters, mobSHOPTripBase.SearchFiltersIn,
               mobSHOPTripBase.SearchNearbyOriginAirports,
               mobSHOPTripBase.SearchNearbyDestinationAirports,
               -1, "", false, false, "", isused);

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
                var tripUsedIndex = enableLOFChangesForReshop ? cslReservationFlightSegment.Where(p => p.LOFNumber == trip.TripIndex) :
                    cslReservationFlightSegment.Where(p => p.TripNumber == trip.TripIndex.ToString());
                if (isused)
                {
                    int index = Convert.ToInt32(trip.TripIndex);
                    tripUsedIndex = enableLOFChangesForReshop ? cslReservationFlightSegment.Where(p => p.LOFNumber == originalTripIndex) :
                        cslReservationFlightSegment.Where(p => p.TripNumber == originalTripIndex.ToString());
                }
                foreach (var cslTripSegments in tripUsedIndex)
                {
                    Flight flight = new Flight();
                    flight.FlightNumber = cslTripSegments.FlightSegment.FlightNumber;
                    flight.OperatingCarrier = cslTripSegments.FlightSegment.OperatingAirlineCode;
                    flight.Origin = cslTripSegments.FlightSegment.DepartureAirport.IATACode;
                    flight.Destination = cslTripSegments.FlightSegment.ArrivalAirport.IATACode;
                    flight.DepartDateTime = cslTripSegments.FlightSegment.DepartureDateTime;
                    trip.Flights.Add(flight);
                }
            }

            return trip;
        }


        private MOBSHOPTripBase ConvertReshopTripToTripBase(ReshopTrip reshopTrip)
        {
            MOBSHOPTripBase tripBase = new MOBSHOPTripBase();
            tripBase = reshopTrip.OriginalTrip;
            tripBase.DepartDate = reshopTrip.OriginalTrip.DepartDate;

            return tripBase;
        }

        private int getShoppingSearchMaxTrips()
        {
            int maxTrips = 125;
            int.TryParse(_configuration.GetValue<string>("ShoppingSearchMaxTrips"), out maxTrips);

            return maxTrips;
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
        private async Task<bool> IsAirportInAllowedRegion(string airportCode, string allowedCountryCodes, string sessionID, string token, int applicationId, string appVersion, string deviceId)
        {
            string url = string.Format("/AirportLookUp?LookupType=AUTO&filter={0}", airportCode);
            var lstAirports = await _referencedataService.GetDataGetAsync<List<Model.Shopping.AirportLookup>>(url, token, sessionID).ConfigureAwait(false);
            return allowedCountryCodes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Any(configCountryCode => string.Equals(configCountryCode, lstAirports?.FirstOrDefault()?.Airport?.IATACountryCode?.CountryCode?.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<MOBFSRAlertMessage>> HandleFlightShoppingThatHasNoResults(United.Services.FlightShopping.Common.ShopResponse cslResponse, MOBSHOPShopRequest restShopRequest, bool isShop)
        {
            United.Services.FlightShopping.Common.ShopResponse cslResponseClone = cslResponse.CloneDeep();
            MOBSHOPShopRequest restShopRequestClone = restShopRequest.CloneDeep();

            var allEnhancements = new List<IRule<MOBFSRAlertMessage>>();
            if (_configuration.GetValue<bool>("FSRForceToGSTSwitch") && isShop)
            {
                allEnhancements.Add(new FSRForceToGSTbyOrigin(cslResponseClone, restShopRequestClone, _configuration));
            }

            if (_configuration.GetValue<bool>("EnableFSRAlertMessages_ForSeasonalMarket"))
            {
                allEnhancements.Add(new FSRSeasonalOrgAndDestSuggestFutureDate(cslResponseClone, restShopRequestClone, _configuration));
                allEnhancements.Add(new FSRSeasonalOriginSuggestFutureDate(cslResponseClone, restShopRequestClone, _configuration));
                allEnhancements.Add(new FSRSeasonalDestinationSuggestFutureDate(cslResponseClone, restShopRequestClone, _configuration));
            }

            if (!restShopRequest.AwardTravel || _configuration.GetValue<bool>("EnableFSRAlertMessages_Nearby_ForAwardBooking") || (_configuration.GetValue<bool>("EnableAwardFSRChanges") && restShopRequest.AwardTravel))
            {
                allEnhancements.Add(new FSRNoResultSuggestNearbyAirports(cslResponseClone, restShopRequestClone, _configuration));
            }

            // Get the first enhancement based on given priorities
            var firstAlert = allEnhancements.FirstOrDefault(rule => rule.ShouldExecuteRule());

            if (_shoppingUtility.CheckFSRRedesignFromShop(restShopRequest))
            {
                if (firstAlert != null)
                {
                    MOBFSRAlertMessage listFsrAlert = new MOBFSRAlertMessage();
                    listFsrAlert = await firstAlert.Execute();

                    listFsrAlert.AlertType = MOBFSRAlertMessageType.Information.ToString();
                    return new List<MOBFSRAlertMessage>() { listFsrAlert };
                }
                else
                {
                    return null;
                }
            }
            else
                return firstAlert != null ? new List<MOBFSRAlertMessage> { await firstAlert.Execute() } : null;
        }

        public async Task<MOBFSRAlertMessage> GetCorporateLeisureOptOutFSRAlert(MOBSHOPShopRequest shoprequest, Session session)
        {
            MOBMobileCMSContentMessages message = new MOBMobileCMSContentMessages();
            MOBFSRAlertMessage alertMessage = null;
            message = await GetCMSContentMessageByKey("Shopping.CorporateDisclaimerMessage.MOB", shoprequest, session);
            if (message != null)
            {
                alertMessage = new MOBFSRAlertMessage
                {
                    BodyMessage = message.ContentFull,
                    HeaderMessage = message.HeadLine,
                    MessageTypeDescription = FSRAlertMessageType.CorporateLeisureOptOut,
                    MessageType = 0,
                    AlertType = MOBFSRAlertMessageType.Information.ToString(),
                    Buttons = new List<MOBFSRAlertMessageButton>()
                {
                     new MOBFSRAlertMessageButton
                     {
                         ButtonLabel =message.ContentShort,
                         UpdatedShopRequest = ConvertCorporateLeisureToRevenueRequest(shoprequest)
                     }
                }
                };
            }
            return alertMessage;
        }

        private async Task<MOBMobileCMSContentMessages> GetCMSContentMessageByKey(string Key, MOBSHOPShopRequest request, Session session)
        {

            CSLContentMessagesResponse cmsResponse = new CSLContentMessagesResponse();
            MOBMobileCMSContentMessages cmsMessage = null;
            List<CMSContentMessage> cmsMessages = null;
            try
            {
                var cmsContentCache = await _cachingService.GetCache<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID") + "MOBCSLContentMessagesResponse", request.TransactionId);
                try
                {
                    if (!string.IsNullOrEmpty(cmsContentCache))
                        cmsResponse = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(cmsContentCache);
                }
                catch { cmsContentCache = null; }

                if (string.IsNullOrEmpty(cmsContentCache) || Convert.ToBoolean(cmsResponse.Status) == false || cmsResponse.Messages == null)
                    cmsResponse = await _travelerCSL.GetBookingRTICMSContentMessages(request, session);

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
            catch (Exception)
            { }
            return cmsMessage;
        }

        public MOBSHOPShopRequest ConvertCorporateLeisureToRevenueRequest(MOBSHOPShopRequest shopRequest)
        {
            if (shopRequest == null)
                return null;
            MOBSHOPShopRequest updatedShopRequest = shopRequest.Clone();
            updatedShopRequest.GetNonStopFlightsOnly = true;
            updatedShopRequest.GetFlightsWithStops = false;
            updatedShopRequest.SessionId = string.Empty;
            updatedShopRequest.MOBCPCorporateDetails = null;
            updatedShopRequest.TravelType = string.Empty;
            return updatedShopRequest;
        }

        public List<MOBFSRAlertMessage> AddMandatoryFSRAlertMessages(MOBSHOPShopRequest restShopRequest, List<MOBFSRAlertMessage> alertMessages)
        {
            if (_configuration.GetValue<bool>("HideFSRChangeFeeWaiverMsg"))
            {
                return alertMessages;
            }
            else
            {
                if (_shoppingUtility.CheckFSRRedesignFromShop(restShopRequest))
                {
                    if (_configuration.GetValue<bool>("ChangeFeeWaiverMessagingToggle"))
                    {
                        if (restShopRequest.Experiments != null && restShopRequest.Experiments.Contains(ShoppingExperiments.NoChangeFee.ToString()))
                        {
                            if (alertMessages == null)
                                alertMessages = new List<MOBFSRAlertMessage>();

                            alertMessages.Add(new MOBFSRAlertMessage()
                            {
                                HeaderMessage = _configuration.GetValue<string>("ChangeFeeWaiverAlertMessageHeader") ?? "No Change fees",
                                BodyMessage = _configuration.GetValue<string>("ChangeFeeWaiver_Message") ?? "Book now and change your flight with no fee. This includes Basic Economy fares",
                                MessageTypeDescription = FSRAlertMessageType.NoChangeFee,
                                AlertType = MOBFSRAlertMessageType.Information.ToString()
                            });
                        }
                    }
                }
                return alertMessages;
            }
        }

        private List<InfoWarningMessages> GetYAInfoMsg(MOBSHOPAvailability availability, United.Services.FlightShopping.Common.ShopResponse response)
        {
            if ((availability.InfoMessages == null || availability.InfoMessages.Count < 1) && IsYoungAdult(response))
            {
                return new List<InfoWarningMessages>()
                            {
                                new InfoWarningMessages()
                                {
                                    Order=FSRINFOMSGORDER.YOUNGADULT.ToString(),
                                    IconType=INFOWARNINGMESSAGEICON.INFORMATION.ToString(),
                                    Messages=new List<string>()
                                    {
                                        _configuration.GetValue<string>("YoungAdultAlertMessage")??string.Empty
                                    }
                                }
                            };
            }

            return availability.InfoMessages;

        }

        private bool IsYoungAdult(United.Services.FlightShopping.Common.ShopResponse response)
        {
            bool isYA = false;
            foreach (var trip in response.Trips.Where(t => t != null && t.Flights != null && t.Flights.Count > 0))
            {
                isYA = isYAFlight(isYA, trip.Flights);
                if (isYA)
                    break;
            }
            return isYA;
        }

        private bool isYAFlight(bool isYA, List<Flight> flights)
        {
            foreach (var flight in flights.Where(f => f != null && f.Products != null && f.Products.Count > 0))
            {
                foreach (var product in flight.Products.Where(p => p != null && !string.IsNullOrEmpty(p.ProductSubtype)).ToList())
                {
                    if (product.ProductSubtype.ToUpper().Equals("YOUNGADULTDISCOUNTEDFARE"))
                    {
                        isYA = true;
                        break;
                    }
                }
                if (flight.Connections != null && flight.Connections.Count > 0 && !isYA)
                {
                    foreach (var connection in flight.Connections)
                    {
                        isYA = isYAFlight(isYA, flight.Connections);

                        if (isYA)
                            break;
                    }
                }

                if (isYA)
                    break;
            }

            return isYA;
        }

        public async Task<List<MOBFSRAlertMessage>> HandleFlightShoppingThatHasResults(United.Services.FlightShopping.Common.ShopResponse cslResponse, MOBSHOPShopRequest restShopRequest, bool isShop)
        {
            United.Services.FlightShopping.Common.ShopResponse cslResponseClone = cslResponse.CloneDeep();
            MOBSHOPShopRequest restShopRequestClone = restShopRequest.CloneDeep();

            var allEnhancements = new List<IRule<MOBFSRAlertMessage>>();

            if (_shoppingUtility.EnableAirportDecodeToCityAllAirports())
            {
                if (!restShopRequest.AwardTravel || _configuration.GetValue<bool>("EnableFSRAlertMessages_Nearby_ForAwardBooking"))
                {
                    allEnhancements.Add(new FSRWithResultOriginOrDestinationWithAllAirports(cslResponseClone, restShopRequestClone, _configuration));
                }
            }

            if (restShopRequest.IsShareTripSearchAgain && _configuration.GetValue<bool>("EnableShareTripInSoftRTI") && GeneralHelper.IsApplicationVersionGreater(restShopRequest.Application.Id, restShopRequest.Application.Version.Major, "AndroidShareTripInSoftRTIVersion", "iPhoneShareTripInSoftRTIVersion", "", "", true, _configuration))
            {
                allEnhancements.Add(new FSRWithResultsShareTripSuggestedByDate(restShopRequestClone, _configuration));
            }

            if (_configuration.GetValue<bool>("FSRForceToGSTSwitch") && isShop)
            {
                allEnhancements.Add(new FSRForceToGSTbyOrigin(cslResponseClone, restShopRequestClone, _configuration));
            }
            // Add all enhancements rule for FSR with results here
            allEnhancements.Add(new FSRForceToNearbyOrgAndDest(cslResponseClone, restShopRequestClone, _sessionHelperService, _shoppingUtility, _configuration, _headers));
            allEnhancements.Add(new FSRForceToNearbyOrigin(cslResponseClone, restShopRequestClone, _sessionHelperService, _shoppingUtility, _configuration));
            allEnhancements.Add(new FSRForceToNearbyDestination(cslResponseClone, restShopRequestClone, _sessionHelperService, _shoppingUtility, _configuration, _headers));

            if (!restShopRequest.AwardTravel || _configuration.GetValue<bool>("EnableFSRAlertMessages_Nearby_ForAwardBooking"))
            {
                allEnhancements.Add(new FSRWithResultSuggestNearbyOrgAndDest(cslResponseClone, restShopRequestClone, _configuration));
                allEnhancements.Add(new FSRWithResultSuggestNearbyDestination(cslResponseClone, restShopRequestClone, _configuration));
                allEnhancements.Add(new FSRWithResultSuggestNearbyOrigin(cslResponseClone, restShopRequestClone, _configuration));
            }

            // Get the first enhancement based on given priorities
            var firstAlert = allEnhancements.FirstOrDefault(rule => rule.ShouldExecuteRule());

            if (_shoppingUtility.CheckFSRRedesignFromShop(restShopRequest))
            {
                if (firstAlert != null)
                {
                    MOBFSRAlertMessage listFsrAlert = new MOBFSRAlertMessage();
                    listFsrAlert = await firstAlert.Execute();
                    if (listFsrAlert != null)
                    {
                        listFsrAlert.AlertType = MOBFSRAlertMessageType.Information.ToString();
                        return new List<MOBFSRAlertMessage>() { listFsrAlert };
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
                return firstAlert != null ? new List<MOBFSRAlertMessage> { await firstAlert.Execute() } : null;
        }

        public async Task<bool> ByPassZeroDollar(Product displayProduct)
        {
            if (await _featureSettings.GetFeatureSettingValue("EnableAdvanceSearchOfferCode").ConfigureAwait(false) && !string.IsNullOrEmpty(displayProduct.ProductType) && displayProduct.ProductType.Contains("PROMOTION"))
            {
                return true;
            }
            else if(await _featureSettings.GetFeatureSettingValue("EnableFSRETCTravelCreditsFeature").ConfigureAwait(false) && !string.IsNullOrEmpty(displayProduct.ProductType) && displayProduct.Prices.Any(r => r.PricingType == "referencePrice" && r.Amount > 0 ))
            {               
                return true;
            }
            else
            {
                return false;
            }
        }

        public void GetMilesDescWithFareDiscloser(MOBSHOPAvailability availability, Session session, bool isMobileRedirect = false, List<string> experimentList = null, bool isNewAwardFSR = false)
        {
            if (_shoppingUtility.EnableFareDisclouserCopyMessage(session.IsReshopChange))
            {
                bool isExperiment = false;

                if (_configuration.GetValue<bool>("IsExperimentEnabled") && experimentList != null && experimentList.Any() &&
                    experimentList.Contains(ShoppingExperiments.NoChangeFee.ToString()))
                {
                    isExperiment = true;
                }
                availability.AvailableAwardMilesWithDesc += "\n" + _configuration.GetValue<string>("AwardFSRCfareDisclosurermessage");
                if (isNewAwardFSR)
                {
                    availability.AvailableAwardMilesWithDesc += "\n" + CheckBagFareDesclaimer(isMobileRedirect);
                }
                else if (_configuration.GetValue<bool>("ChangeFeeWaiverMessagingToggle") && !isExperiment)
                {
                    availability.AvailableAwardMilesWithDesc += "\n" + _shoppingUtility.GetFeeWaiverMessage();
                }
            }
        }

        private string CheckBagFareDesclaimer(bool isMobileRedirect)
        {
            if (isMobileRedirect) return _configuration.GetValue<string>("CheckedBagInfoMobileRedirectURL");

            return _configuration.GetValue<string>("EnableNewBaggageTextOnFSRShop");
        }

        public void SetTitleForFSRPage(MOBSHOPAvailability availability, MOBSHOPShopRequest shopRequest)
        {
            if (_shoppingUtility.CheckFSRRedesignFromShop(shopRequest))
            {
                if (availability != null && availability.Trip != null && !string.IsNullOrEmpty(shopRequest.SearchType) && shopRequest.Trips != null && !string.IsNullOrEmpty(availability.Trip.OriginDecoded)
                && !string.IsNullOrEmpty(availability.Trip.DestinationDecoded) && !string.IsNullOrEmpty(availability.Trip.DepartDate))
                {
                    try
                    {
                        if (shopRequest.Trips.Any(t => t.DestinationAllAirports == 1 || t.OriginAllAirports == 1 || t.SearchNearbyOriginAirports || t.SearchNearbyDestinationAirports))
                        {
                            availability.Title = _configuration.GetValue<string>("FSRRedesignTitleForNoREsults") ?? "Select flights";
                        }
                        else
                        {
                            availability.Title = availability.Trip.OriginDecoded.Split(',')[0] + " to " + availability.Trip.DestinationDecoded.Split(',')[0];
                        }
                        string traveler = string.Empty;
                        if ((shopRequest.TravelType == TravelType.CB.ToString() || shopRequest.TravelType.Equals("E20")) && shopRequest.NumberOfAdults > 0)
                        {
                            traveler = shopRequest.NumberOfAdults == 1 ? Convert.ToString(shopRequest.NumberOfAdults) + " traveler" : Convert.ToString(shopRequest.NumberOfAdults) + " travelers";
                        }
                        else
                        {
                            traveler = availability.TravelerCount <= 1 ? Convert.ToString(availability.TravelerCount) + " traveler" : Convert.ToString(availability.TravelerCount) + " travelers";
                        }
                        string date = Convert.ToDateTime(availability.Trip.DepartDate).ToString("ddd MMM d", new CultureInfo("en-US"));
                        string tripCount = shopRequest.Trips.Count.ToString();
                        string isLastTripIndex = Convert.ToString(availability.Trip.LastTripIndexRequested);
                        string searchType = shopRequest.SearchType.Equals("OW", StringComparison.OrdinalIgnoreCase) ? "One-way" : shopRequest.SearchType.Equals("RT", StringComparison.OrdinalIgnoreCase) ? "Roundtrip" : "Flight " + isLastTripIndex + " of " + tripCount;
                        var joiner = $" {(char)8226} "; //" • "
                        availability.SubTitle = string.Join(joiner, searchType, traveler, date);
                    }
                    catch (Exception ex)
                    {
                        availability.Title = _configuration.GetValue<string>("FSRRedesignTitleForNoREsults") ?? "Select flights";
                        availability.SubTitle = string.Empty;
                    }
                }
            }
        }

        private bool IsTravelArranger(MOBSHOPShopRequest shopRequest)
        {
            return _configuration.GetValue<bool>("EnableIsArranger") && shopRequest.IsCorporateBooking && !shopRequest.IsReshopChange && IsTravelArrangerBooking(shopRequest.MOBCPCorporateDetails);
        }

        private bool IsTravelArrangerBooking(MOBCorporateDetails mOBCPCorporateDetails)
        {
            return mOBCPCorporateDetails != null && !string.IsNullOrEmpty(mOBCPCorporateDetails.CorporateBookingType) &&
                mOBCPCorporateDetails.CorporateBookingType.ToUpper().Equals(CORPORATEBOOKINGTYPE.TravelArranger.ToString().ToUpper());
        }

        private ShopPricesCommon PopulatePriceSummary(PricesCommon summary)
        {
            ShopPricesCommon priceSummary = null;

            if (summary != null)
            {
                priceSummary = new ShopPricesCommon();
                priceSummary.BrokenOutYQSurcharges = summary.BrokenOutYQSurcharges;
                priceSummary.BusinessAirfare = summary.BusinessAirfare;
                priceSummary.CheapestAirfare = summary.CheapestAirfare;
                priceSummary.CheapestAirfareNoConnections = summary.CheapestAirfareNoConnections;
                priceSummary.CheapestAirfareWithConnections = summary.CheapestAirfareWithConnections;
                priceSummary.CheapestAirfareWithConnNonPartner = summary.CheapestAirfareWithConnNonPartner;
                priceSummary.CheapestAirfareWithConnPartner = summary.CheapestAirfareWithConnPartner;
                priceSummary.CheapestAltDate = summary.CheapestAltDate;
                priceSummary.CheapestNearbyAirport = summary.CheapestNearbyAirport;
                priceSummary.CheapestRefundable = summary.CheapestRefundable;
                priceSummary.CheapestWithoutOffer = summary.CheapestWithoutOffer;
                priceSummary.FirstClassAirfare = summary.FirstClassAirfare;
                priceSummary.FirstClassAirfareNotShownReason = summary.FirstClassAirfareNotShownReason;
                priceSummary.FullYAirfare = summary.FullYAirfare;
                priceSummary.MostExpensiveAirfare = summary.MostExpensiveAirfare;
                priceSummary.RefundableAirfare = summary.RefundableAirfare;
                priceSummary.RefundableAverageAirfarePerPax = summary.RefundableAverageAirfarePerPax;
            }

            return priceSummary;
        }

        public UpdateAmenitiesIndicatorsRequest GetAmenitiesRequest(string cartId, List<Flight> flights)
        {
            UpdateAmenitiesIndicatorsRequest request = new UpdateAmenitiesIndicatorsRequest();

            request.CartId = cartId;
            request.CollectionType = UpdateAmenitiesIndicatorsCollectionType.FlightNumbers;
            request.FlightNumbers = new Collection<string>();

            if (flights != null)
            {
                try
                {
                    foreach (Flight flight in flights)
                    {
                        if (!request.FlightNumbers.Contains(flight.FlightNumber))
                        {
                            request.FlightNumbers.Add(flight.FlightNumber);
                        }
                        if (flight.Connections != null && flight.Connections.Count > 0)
                        {
                            foreach (Flight connection in flight.Connections)
                            {
                                if (!request.FlightNumbers.Contains(connection.FlightNumber))
                                {
                                    request.FlightNumbers.Add(connection.FlightNumber);
                                }
                            }
                        }
                        if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                        {
                            foreach (Flight stop in flight.StopInfos)
                            {
                                if (!request.FlightNumbers.Contains(stop.FlightNumber))
                                {
                                    request.FlightNumbers.Add(stop.FlightNumber);
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            return request;
        }

        public async Task<MOBSHOPTrip> PopulateTrip(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string cartId, List<Trip> trips, int tripIndex, string requestedCabin, string sessionId, int appId, string deviceId, string appVersion, bool showMileageDetails, int premierStatusLevel, bool isStandardRevenueSearch, bool isAward = false, bool isELFFareDisplayAtFSR = true, bool getNonStopFlightsOnly = false, bool getFlightsWithStops = false, MOBSHOPShopRequest shopRequest = null, MOBAdditionalItems additionalItems = null, List<CMSContentMessage> lstMessages = null, HttpContext httpContext = null)
         {
            if(httpContext != null)
                _httpContext = httpContext;
            if (_mOBSHOPDataCarrier == null)
                _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);

            if (shopRequest != null && session != null && !session.IsFSRRedesign)
            {
                session = await _shoppingUtility.ValidateFSRRedesign(shopRequest, session);
            }
            await Task.Delay(0);
            return await PopulateTrip(_mOBSHOPDataCarrier, cartId, trips, tripIndex, requestedCabin, sessionId, "", appId, deviceId, appVersion, showMileageDetails, premierStatusLevel, isStandardRevenueSearch, isAward, isELFFareDisplayAtFSR, getNonStopFlightsOnly, getFlightsWithStops, shopRequest, session, additionalItems, lstMessages, _httpContext);
        }

        public async Task<MOBSHOPTrip> PopulateTrip(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string cartId, List<Trip> trips, int tripIndex, string requestedCabin, string sessionId, string tripKey, int appId, string deviceId, string appVersion, bool showMileageDetails, int premierStatusLevel, bool isStandardRevenueSearch, bool isAward = false, bool isELFFareDisplayAtFSR = true, bool getNonStopFlightsOnly = false, bool getFlightsWithStops = false, MOBSHOPShopRequest shopRequest = null, Session session = null, MOBAdditionalItems additionalItems = null, List<CMSContentMessage> lstMessages = null, HttpContext httpContext = null)
        {
            if (httpContext != null)
                _httpContext = httpContext;
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            //  Session session = new Session();
            //  session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName });
            MOBSearchFilters searchFilters = new MOBSearchFilters();
            if (await _featureToggles.IsEnableWheelchairFilterOnFSR(appId, appVersion, session.CatalogItems).ConfigureAwait(false) || (session.IsReshopChange && await _featureToggles.IsEnableReshopWheelchairFilterOnFSR(appId, appVersion, session.CatalogItems).ConfigureAwait(false)))
            {
                searchFilters = await _sessionHelperService.GetSession<MOBSearchFilters>(session.SessionId, searchFilters.GetType().FullName, new List<string> { session.SessionId, searchFilters.GetType().FullName }).ConfigureAwait(false);
            }
            supressLMX = session != null ? session.SupressLMXForAppID : supressLMX;
            #endregion
            MOBSHOPTrip trip = null;
            string getAmenitiesCallDurations = string.Empty;
            var showOriginDestinationForFlights = false;
            int i = 0;
            try
            {
                if (isGetAirportListInOneCallToggleOn() && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    airportsList = await GetAllAiportsList(trips);
                    if (airportsList != null)
                    {
                        await _sessionHelperService.SaveSession<AirportDetailsList>(airportsList, sessionId, new List<string> { sessionId, new AirportDetailsList().ObjectName }, new AirportDetailsList().ObjectName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllAiportsList {@PopulateTrip-GetAllAiportsList}", JsonConvert.SerializeObject(ex));
                _logger.LogError("GetAllAiportsList {@PopulateMetaTrips-Trip}", JsonConvert.SerializeObject(trips));
            }

            if (trips != null && trips.Count > 0 && (tripIndex == 0 || trips.Count > tripIndex))
            {
                i = tripIndex;

                trip = new MOBSHOPTrip();
                trip.TripId = trips[i].BBXSolutionSetId;
                if (_configuration.GetValue<bool>("EnableNonStopFlight"))
                {
                    trip.FlightCount = trips[i].Flights.Count; //**nonstopchanges==> Need to work with client and fix the total flight count.
                    if (getNonStopFlightsOnly && !getFlightsWithStops)
                        trip.TripHasNonStopflightsOnly = trips[i].UseFilters; //**nonstopchanges==> Need to work to return the right bool for first shop/selectrip response & second shop/selectrip response.
                }
                else
                {
                    if (!trips[i].UseFilters)
                    {
                        trip.FlightCount = trips[i].Flights.Count;
                    }
                    else
                    {
                        if (i == 0)
                        {
                            ShoppingResponse shop = new ShoppingResponse();
                            shop = await _sessionHelperService.GetSession<ShoppingResponse>(sessionId, shop.ObjectName, new List<string> { sessionId, shop.ObjectName });
                            trip.FlightCount = shop.Response.Availability.Trip.FlightCount;
                        }
                        else
                        {
                            SelectTrip selectTrip = new SelectTrip();
                            selectTrip = await _sessionHelperService.GetSession<SelectTrip>(sessionId, selectTrip.ObjectName, new List<string> { sessionId, selectTrip.ObjectName });
                            trip.FlightCount = selectTrip.Responses[tripKey].Availability.Trip.FlightCount;
                        }
                    }
                }
                trip.TotalFlightCount = trip.FlightCount;
                trip.DepartDate = FormatDateFromDetails(trips[i].DepartDate);
                trip.ArrivalDate = FormatDateFromDetails(trips[i].Flights[trips[i].Flights.Count - 1].DestinationDateTime);
                trip.Destination = trips[i].Destination;

                string destinationName = string.Empty;
                if (!string.IsNullOrEmpty(trip.Destination))
                {
                    destinationName = await GetAirportNameFromSavedList(trip.Destination);
                }
                if (string.IsNullOrEmpty(destinationName))
                {
                    trip.DestinationDecoded = trips[i].DestinationDecoded;
                }
                else
                {
                    trip.DestinationDecoded = destinationName;
                }

                CultureInfo ci = null;

                if (_configuration.GetValue<bool>("EnableNonStopFlight") && getFlightsWithStops)
                {
                    trip.Columns = await PopulateColumns(trips[i].ColumnInformation, getFlightsWithStops, session);
                }
                else
                {
                    trip.Columns = PopulateColumns(trips[i].ColumnInformation);
                }

                List<Model.Shopping.MOBSHOPFlight> flights = null;
                if (trips[i].Flights != null && trips[i].Flights.Count > 0)
                {
                    //update amenities for all flights
                    UpdateAmenitiesIndicatorsResponse amenitiesResponse = new UpdateAmenitiesIndicatorsResponse();
                    List<Flight> tempFlights = new List<Flight>(trips[i].Flights);

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
                                //****Get Call Duration Code - Venkat 03/17/2015*******
                                Stopwatch getAmenitiesCSLCallDurationstopwatch1;
                                getAmenitiesCSLCallDurationstopwatch1 = new Stopwatch();
                                getAmenitiesCSLCallDurationstopwatch1.Reset();
                                getAmenitiesCSLCallDurationstopwatch1.Start();
                                amenitiesResponse = await GetAmenitiesForFlights(sessionId, cartId, tempFlights, appId, deviceId, appVersion);
                                if (getAmenitiesCSLCallDurationstopwatch1.IsRunning)
                                {
                                    getAmenitiesCSLCallDurationstopwatch1.Stop();
                                }
                                getAmenitiesCallDurations = getAmenitiesCallDurations + "|44=" + getAmenitiesCSLCallDurationstopwatch1.ElapsedMilliseconds.ToString() + "|"; // 44 Flight Amenities call
                                PopulateFlightAmenities(amenitiesResponse.Profiles, ref tempFlights);
                            }
                        },
                            async () =>
                            {
                                if (showMileageDetails && !supressLMX)
                                {
                                    List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights = null;
                                    List<Flight> newFlights = null;
                                    lmxFlights = await GetLmxFlights(session.Token, cartId, GetFlightHasListForLMX(tempFlights), sessionId, appId, appVersion, deviceId);

                                    if (lmxFlights != null && lmxFlights.Count > 0)
                                    {
                                        PopulateLMX(lmxFlights, ref tempFlights);
                                    }
                                }
                            }
                        );
                    }
                    catch { };

                    trips[i].Flights = new List<Flight>(tempFlights);

                    //****Get Call Duration Code - Venkat 03/17/2015*******
                    Stopwatch getFlightsStopWatch;
                    getFlightsStopWatch = new Stopwatch();
                    getFlightsStopWatch.Reset();
                    getFlightsStopWatch.Start();
                    string fareClass = string.Empty;

                    if (_configuration.GetValue<bool>("EnableCodeRefactorForSavingSessionCalls") && !string.IsNullOrWhiteSpace(shopRequest?.FareClass))
                    {
                        fareClass = shopRequest.FareClass;
                    }
                    else
                    {
                        fareClass = await GetFareClassAtShoppingRequestFromPersist(sessionId);
                    }

                    if (_mOBSHOPDataCarrier == null)
                        _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                  
                    flights = await GetFlightsAsync(_mOBSHOPDataCarrier, sessionId, cartId, trips[i].Flights, trips[i].SearchFiltersIn.FareFamily, trips[i].SearchFiltersOut.PriceMin, trip.Columns, premierStatusLevel, fareClass, false, false, isELFFareDisplayAtFSR, trip, appVersion, appId, session, additionalItems, lstMessages, searchFilters);
                    if (getFlightsStopWatch.IsRunning)
                    {
                        getFlightsStopWatch.Stop();
                    }
                }

                trip.Origin = trips[i].Origin;

                if (showMileageDetails && !supressLMX)
                    trip.YqyrMessage = _configuration.GetValue<string>("MP2015YQYRMessage");

                string originName = string.Empty;
                if (!string.IsNullOrEmpty(trip.Origin))
                {
                    originName = await GetAirportNameFromSavedList(trip.Origin);
                }
                if (string.IsNullOrEmpty(originName))
                {
                    trip.OriginDecoded = trips[i].OriginDecoded;
                }
                else
                {
                    trip.OriginDecoded = originName;
                }

                if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI"))
                {
                    //with share trip flag OriginDescription=Chicago, IL, US (ORD)
                    trip.OriginDecodedWithCountry = trips[i].OriginDecoded;
                    trip.DestinationDecodedWithCountry = trips[i].DestinationDecoded;
                }

                MOBAdditionalToggle mOBAdditionalToggle = new MOBAdditionalToggle { MixedCabinFlightExists = false };

                if (flights != null)
                {
                    trip.FlattenedFlights = new List<Model.Shopping.MOBSHOPFlattenedFlight>();
                    if (additionalItems == null)
                    {
                        additionalItems = new MOBAdditionalItems();
                    }
                    trip.FlattenedFlights = await GetFlattendFlights(flights, trip.TripId, trips[i].BBXCellIdSelected, trip.DepartDate, shopRequest, tripIndex, mOBAdditionalToggle, additionalItems, lstMessages: lstMessages, session.IsReshopChange).ConfigureAwait(false);

                    if (_configuration.GetValue<bool>("EnableShowOriginDestinationForFlights") && session.IsFSRRedesign && GeneralHelper.IsApplicationVersionGreaterorEqual(shopRequest.Application.Id, shopRequest.Application.Version.Major, _configuration.GetValue<string>("AndroidShowOriginDestinationForFlightsVersion"), _configuration.GetValue<string>("iOSShowOriginDestinationForFlightsVersion")))
                    {
                        showOriginDestinationForFlights = trip.FlattenedFlights.Any(x => x?.Flights?.First()?.Origin != trip.Origin || x?.Flights?.Last()?.Destination != trip.Destination);
                    }
                }
                if (_configuration.GetValue<bool>("EnableFSRMoneyPlusMilesFeature"))
                {
                    trip.InternationalCitiesExist = trips[i].InternationalCitiesExist;
                }
                //TODO-CHECK IF THIS PIECE OF CODE IS NEEDED. NOT THERE IN MREST
                //bool mixedCabinFlightExists = false;
                //if (_configuration.GetValue<bool>("EnableAwardMixedCabinFiter") && isAward)
                //{
                //    mixedCabinFlightExists = trip.FlattenedFlights.Any(f => f.Flights.Any(s => s.ShoppingProducts.Any(p => p.IsMixedCabin)));
                //}
                trip.UseFilters = trips[i].UseFilters;
                if (trips[i].SearchFiltersIn != null && !string.IsNullOrEmpty(trips[i].SearchFiltersIn.AirportsOrigin))
                {
                    trip.SearchFiltersIn = await GetSearchFilters(trips[i].SearchFiltersIn, ci, appId, appVersion, requestedCabin, trips[i].ColumnInformation, isStandardRevenueSearch, isAward, mOBAdditionalToggle.MixedCabinFlightExists, lstMessages, session, searchFilters).ConfigureAwait(false);
                }
                else
                {
                    trip.SearchFiltersIn = await GetSearchFilters(trips[i].SearchFiltersOut, ci, appId, appVersion, requestedCabin, trips[i].ColumnInformation, isStandardRevenueSearch, isAward, mOBAdditionalToggle.MixedCabinFlightExists, lstMessages, session, searchFilters).ConfigureAwait(false);
                }
                trip.SearchFiltersOut = await GetSearchFilters(trips[i].SearchFiltersOut, ci, appId, appVersion, requestedCabin, trips[i].ColumnInformation, isStandardRevenueSearch, isAward, mOBAdditionalToggle.MixedCabinFlightExists, lstMessages, session, searchFilters).ConfigureAwait(false);
                if (_configuration.GetValue<bool>("SuppressFSREPlusColumn"))
                {
                    trip.DisableEplus = trips[i].AncillaryStatus?.DisableEplus ?? false;
                }
                if (session.IsFSRRedesign && _configuration.GetValue<bool>("IsEnabledFsrRedesignFooterSortring"))
                {
                    if (trip.Columns?.Count > 0)
                    {
                        var sortTypes = GetDefaultFsrRedesignFooterSortTypes();
                        foreach (var column in trip.Columns)
                        {
                            var value = ($"{column.LongCabin} {column.Description}").Trim();
                            sortTypes.Add(new MOBSearchFilterItem()
                            {
                                Key = column.Type,
                                Value = value,
                                DisplayValue = value
                            });
                        }
                        if (trip.SearchFiltersOut?.SortTypes?.Count() > 0)
                        {
                            trip.SearchFiltersOut.SortTypes = sortTypes;
                        }
                        if (trip.SearchFiltersIn?.SortTypes?.Count() > 0)
                        {
                            trip.SearchFiltersIn.SortTypes = sortTypes;
                        }
                    }
                }

                ///208848 : PROD Basic Economy mApps Price in footer of FSR is showing lowest BE fare when BE switch is off 
                ///Srini - 12/29/2017
                if (_configuration.GetValue<bool>("BugFixToggleFor18B") && _mOBSHOPDataCarrier.FsrMinPrice > 0 && trip.SearchFiltersIn.PriceMin < _mOBSHOPDataCarrier.FsrMinPrice && !session.IsReshopChange)
                {
                    if (isAward)
                    {
                        trip.SearchFiltersOut.PriceMinDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(Convert.ToString(_mOBSHOPDataCarrier.FsrMinPrice), true);
                    }
                    else
                    {
                        trip.SearchFiltersOut.PriceMinDisplayValue = TopHelper.FormatAmountForDisplay(_mOBSHOPDataCarrier.FsrMinPrice.ToString(), ci = TopHelper.GetCultureInfo(""), true);
                    }
                    trip.SearchFiltersOut.PriceMin = _mOBSHOPDataCarrier.FsrMinPrice;
                }
            }
            trip.CallDurationText = getAmenitiesCallDurations;
            int pageSize = _configuration.GetValue<int>("OrgarnizeResultsRequestPageSize");
            trip.PageCount = (trip.FlightCount / pageSize) + ((trip.FlightCount % pageSize) == 0 ? 0 : 1);

            if (_configuration.GetValue<bool>("ReturnAllRemainingShopFlightsWithOnly2PageCount") && trip.PageCount > 1)
            {
                trip.PageCount = 2;   //**PageCount==2==>> for paging implementation to send only 15 flights back to client and perisit remaining flights and return remaining when customer page up
            }
            if (_configuration.GetValue<bool>("EnableNonStopFlight"))
            {
                if (getNonStopFlightsOnly && !getFlightsWithStops && trip.TripHasNonStopflightsOnly)
                    trip.PageCount = 1; //**to send pagecount 1 means not to call organize results for paging as we have before after shop call () if the market search has non stop flights.
                else if (getFlightsWithStops && !_configuration.GetValue<bool>("AllowPagingForFlightsWithStops")) //** if decided not to do paging for flights with stops resposne even they are large in number then uncomment these 2 lines or define and set the value of this "AllowPagingForFlightsWithStops" as true;
                    trip.PageCount = 1; //**to send pagecount 1 means not to call organize results for paging as we have before after shop call () if the market search has non stop flights.
            }

            if (session.IsFSRRedesign)
            {
                if (_configuration.GetValue<bool>("EnableColumnSelectionFsrRedesign"))
                {
                    string focusColumnID = GetFocusColumnID(trip, shopRequest);
                    if (_configuration.GetValue<bool>("EnableSequenceContainsIssueFixMB28334"))
                    {
                        var selectedColumn = trip.Columns.FirstOrDefault(c => c.ColumnID.Equals(focusColumnID));
                        if (selectedColumn != null) { selectedColumn.IsSelectedCabin = true; }

                    }
                    else
                    {
                        trip.Columns.First(c => c.ColumnID.Equals(focusColumnID)).IsSelectedCabin = true;
                    }
                }

                trip.ShowOriginDestinationForFlights = showOriginDestinationForFlights;
            }
            trip.FlightDateChangeMessage = _configuration.GetValue<string>("FlightDateChangeMessage");

            return trip;
        }
        private string GetFocusColumnID(MOBSHOPTrip trip, MOBSHOPShopRequest shopRequest)
        {
            if (_configuration.GetValue<bool>("DisableBusinessDefaultWhenFirstNoResults") == false
                && trip.FlattenedFlights[0].Flights[0].ShoppingProducts?.FirstOrDefault(p => p.IsSelectedCabin)?.ColumnID == null)
            {
                if (shopRequest.Trips?.FirstOrDefault()?.Cabin?.Contains("First") == true)
                {
                    if (trip.FlattenedFlights[0].Flights[0].ShoppingProducts?.Any(a => a.Cabin.ToLower().Contains("business")) == true)
                        return trip.FlattenedFlights[0].Flights[0].ShoppingProducts?.FirstOrDefault(a => a.Cabin.ToLower().Contains("business"))?.ColumnID;
                }
            }
            return trip.FlattenedFlights[0].Flights[0].ShoppingProducts?.FirstOrDefault(p => p.IsSelectedCabin)?.ColumnID ?? trip.FlattenedFlights[0].Flights[0].ShoppingProducts[0].ColumnID;

        }
        private async Task<AirportDetailsList> GetAllAiportsList(List<Flight> flights)
        {
            string airPortCodes = GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(flights);
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

        private string GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(List<Flight> flights)
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

        public async Task<(List<Model.Shopping.MOBSHOPFlight>, CultureInfo ci)> GetFlights(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string sessionId, string cartId, List<Flight> segments, string requestedCabin, CultureInfo ci, decimal lowestFare, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClass, bool updateAmenities = false, bool isConnection = false, bool isELFFareDisplayAtFSR = true, MOBSHOPTrip trip = null, string appVersion = "", int appID = 0, Session session = null, MOBAdditionalItems additionalItems = null, List<CMSContentMessage> lstMessages = null, MOBSearchFilters searchFilters = null)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            if (session == null)
            {
                session = new Session();
                session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);
            }
           
            supressLMX = session.SupressLMXForAppID;
            #endregion
            try
            {
                if (isGetAirportListInOneCallToggleOn() && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    airportsList = await _sessionHelperService.GetSession<AirportDetailsList>(sessionId, new AirportDetailsList().ObjectName, new List<string> { sessionId, new AirportDetailsList().ObjectName }).ConfigureAwait(false);

                    if (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0)
                    {
                        airportsList = await GetAllAiportsList(segments);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllAiportsList {@GetFlights-GetAllAiportsList}", JsonConvert.SerializeObject(ex));
                _logger.LogError("GetAllAiportsList {@PopulateMetaTrips-Trip}", JsonConvert.SerializeObject(segments));
            }

            List<Model.Shopping.MOBSHOPFlight> flights = null;
            ci = null;
            if (segments != null && segments.Count > 0)
            {
                flights = new List<Model.Shopping.MOBSHOPFlight>();

                foreach (Flight segment in segments)
                {
                    #region

                    Model.Shopping.MOBSHOPFlight flight = new Model.Shopping.MOBSHOPFlight();
                    flight.Messages = new List<Model.Shopping.MOBSHOPMessage>();
                    string AddCollectProductID = string.Empty;
                    Product displayProductForStopInfo = null;
                    bool selectedForStopInfo = false;
                    string bestProductType = null;
                    // #633226 Reshop SDC Add coller waiver status
                    if (session.IsReshopChange)
                    {
                        flight.isAddCollectWaived = GetAddCollectWaiverStatus(segment, out AddCollectProductID, appID, appVersion);
                        flight.AddCollectProductId = AddCollectProductID;
                    }

                    if (!string.IsNullOrEmpty(segment.BBXHash))
                    {
                        flight.FlightId = segment.BBXHash;
                        flight.ProductId = segment.BBXCellId;
                        flight.GovtMessage = segment.SubjectToGovernmentApproval ? _configuration.GetValue<string>("SubjectToGovernmentApprovalMessage") : string.Empty;
                    }

                    flight.TripIndex = segment.TripIndex;
                    flight.FlightHash = segment.Hash;

                    flight.IsCovidTestFlight = segment.IsCovidTestingRequired;
                    if (!_configuration.GetValue<bool>("DisableOperatingCarrierFlightNumberChanges"))
                    {
                        flight.OperatingCarrierFlightNumber = segment.OriginalFlightNumber;
                    }

                    if (session.IsExpertModeEnabled && !session.IsAward)
                    {
                        if (!string.IsNullOrEmpty(segment.BookingClassAvailability))
                        {
                            flight.BookingClassAvailability = segment.BookingClassAvailability.Replace('|', ' ');
                        }
                    }

                    #region //NEED LOGIC TO DETERMINE SELECTED PRODUCT HERE TO GET PRICING
                    if (segment.Products != null && segment.Products.Count > 0/* && segment.Products[0] != null && segment.Products[0].Prices != null && segment.Products[0].Prices.Count > 0*/)
                    {
                        bool selected;
                        int seatsRemaining = 0;
                        bool mixedCabin;
                        string description = string.Empty;

                        AssignCorporateFareIndicator(segment, flight, session.TravelType);

                        Product displayProduct = GetMatrixDisplayProduct(segment.Products, requestedCabin, columnInfo, out ci, out selected, out description, out mixedCabin, out seatsRemaining, fareClass, isConnection, isELFFareDisplayAtFSR);
                        displayProductForStopInfo = displayProduct;
                        selectedForStopInfo = selected;
                        if (_shoppingUtility.EnableYoungAdult(session.IsReshopChange))
                        {
                            if (IsYoungAdultProduct(segment.Products))
                            {
                                flight.YaDiscount = "Discounted";
                            }
                        }
                        GetBestProductTypeTripPlanner(session, displayProduct, selected, ref bestProductType);
                        if (displayProduct != null && isConnection || (displayProduct.Prices != null && displayProduct.Prices.Count > 0))
                        {
                            if (displayProduct != null && !isConnection && displayProduct.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES)
                            {
                                //WADE-adding logic to add in close in award fee if present
                                decimal closeInFee = 0;
                                if (displayProduct.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES)
                                {
                                    foreach (PricingItem p in displayProduct.Prices)
                                    {
                                        if (p.PricingType.Trim().ToUpper() == PRICING_TYPE_CLOSE_IN_FEE)
                                        {
                                            closeInFee = p.Amount;
                                            break;
                                        }
                                    }

                                }
                                flight.Airfare = displayProduct.Prices[0].Amount;
                                ///208848 : PROD Basic Economy mApps Price in footer of FSR is showing lowest BE fare when BE switch is off 
                                ///Srini - 12/29/2017
                                if (_configuration.GetValue<bool>("BugFixToggleFor18B") &&
                                      (_mOBSHOPDataCarrier.FsrMinPrice > displayProduct.Prices[0].Amount ||
                                      (_mOBSHOPDataCarrier.FsrMinPrice == 0 && displayProduct.Prices[0].Amount > 0)) &&
                                      !mixedCabin &&
                                      (
                                        requestedCabin.Contains("ECONOMY") ||
                                        (requestedCabin.Contains("BUS") && (displayProduct.ProductType.Contains("BUS") || displayProduct.ProductType.Contains("FIRST"))) ||
                                        (requestedCabin.Contains("FIRST") && displayProduct.ProductType.Contains("FIRST"))
                                      )
                                   )
                                {
                                    _mOBSHOPDataCarrier.FsrMinPrice = displayProduct.Prices[0].Amount;
                                }
                                //TFS WI 68391
                                //Do_Not_Allow_Miles_Zero_AwardSearch we using this as a Flag if its Null means allow if its not Null even the value is true or false do not allow Zero Miles Award Booking.
                                if (displayProduct.Prices[0].Amount > 0 || _configuration.GetValue<string>("Do_Not_Allow_Miles_Zero_AwardSearch") == null)
                                {
                                    if (!_configuration.GetValue<bool>("NGRPAwardCalendarMP2017NewUISwitch"))
                                    {
                                        flight.AirfareDisplayValue = TopHelper.FormatAmountForDisplay(displayProduct.Prices[1].Amount + closeInFee, ci, true, true);
                                    }
                                    else
                                    {
                                        flight.AirfareDisplayValue = TopHelper.FormatAmountForDisplay(displayProduct.Prices[1].Amount + closeInFee, ci, false, true);
                                    }
                                    flight.MilesDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(flight.Airfare.ToString(), true);
                                }
                                else
                                {
                                    //Bypassing 0 dollar condition for PromoCode - TODO check the condition
                                    if (!(await ByPassZeroDollar(displayProduct)))
                                    {
                                        flight.AirfareDisplayValue = "N/A";
                                        flight.MilesDisplayValue = "N/A";
                                        if (_configuration.GetValue<string>("ReturnExceptionForAnyZeroDollarAirFareValue") != null && Convert.ToBoolean(_configuration.GetValue<bool>("ReturnExceptionForAnyZeroDollarAirFareValue").ToString()))
                                        {
                                            throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                                        }
                                    }
                                }
                                flight.MilesDisplayValue += !selected || mixedCabin ? "*" : "";
                                flight.IsAwardSaver = displayProduct.AwardType.Trim().ToLower().Contains("saver") && !displayProduct.ProductType.ToUpper().Contains("ECONOMY") ? true : false;
                            }
                            else if (!isConnection)
                            {
                                ///208848 : PROD Basic Economy mApps Price in footer of FSR is showing lowest BE fare when BE switch is off 
                                ///Srini - 12/29/2017
                                if (_configuration.GetValue<bool>("BugFixToggleFor18B") &&
                                      (_mOBSHOPDataCarrier.FsrMinPrice > displayProduct.Prices[0].Amount ||
                                      (_mOBSHOPDataCarrier.FsrMinPrice == 0 && displayProduct.Prices[0].Amount > 0)) &&
                                      !mixedCabin &&
                                      (
                                        requestedCabin.Contains("ECONOMY") ||
                                        (requestedCabin.Contains("BUS") && (displayProduct.ProductType.Contains("BUS") || displayProduct.ProductType.Contains("FIRST"))) ||
                                        (requestedCabin.Contains("FIRST") && displayProduct.ProductType.Contains("FIRST"))
                                      )
                                   )
                                {
                                    _mOBSHOPDataCarrier.FsrMinPrice = displayProduct.Prices[0].Amount;
                                }
                                flight.Airfare = displayProduct.Prices[0].Amount;
                                if (session.IsReshopChange)
                                {
                                    if (session.IsAward)
                                    {
                                        //if(trip.LastTripIndexRequested == 1)
                                        //    flight.AirfareDisplayValue = ReShopAirfareDisplayValue(ci, displayProduct.Prices, session.IsAward, true);
                                        //else
                                        flight.AirfareDisplayValue = ReShopAirfareDisplayValue(ci, displayProduct.Prices, session.IsAward, true);
                                        var milesAmountToDisplay = ReshopAwardPrice(displayProduct.Prices);

                                        if (milesAmountToDisplay == null)
                                            flight.MilesDisplayValue = "NA";
                                        else
                                        {
                                            flight.MilesDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(Convert.ToString(milesAmountToDisplay.Amount), true);
                                            flight.MilesDisplayValue += !selected || mixedCabin ? "*" : "";
                                        }
                                    }
                                    else
                                    {
                                        flight.AirfareDisplayValue = ReShopAirfareDisplayValue(ci, displayProduct.Prices);

                                        if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                                        {
                                            flight.ReshopFees = ReshopAirfareDisplayText(displayProduct.Prices);
                                        }
                                        if (_shoppingUtility.EnableReShopAirfareCreditDisplay(appID, appVersion))
                                        {
                                            flight = ReShopAirfareCreditDisplayFSR(ci, displayProduct, flight);
                                        }
                                    }
                                }
                                else if (displayProduct.Prices[0].Amount > 0)
                                {
                                    string displayPrice = TopHelper.FormatAmountForDisplay(displayProduct.Prices[0].Amount, ci, true);
                                    flight.AirfareDisplayValue = string.IsNullOrEmpty(displayPrice) ? "Not available" : displayPrice;
                                }
                                else
                                {
                                    //Bypassing 0 dollar condition for PromoCode - TODO check the condition
                                    if (!await ByPassZeroDollar(displayProduct))
                                    {
                                        flight.AirfareDisplayValue = "N/A";
                                        if (_configuration.GetValue<string>("ReturnExceptionForAnyZeroDollarAirFareValue") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnExceptionForAnyZeroDollarAirFareValue").ToString()))
                                        {
                                            // Added as part of Bug 180337:mApp: "Sorry something went wrong... " Error message is displayed when selected cabin for second segment in the multi trip
                                            // throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                                            throw new MOBUnitedException(_configuration.GetValue<string>("BookingDetail_ITAError_For_CSL_10047__Error_Code"));
                                        }
                                    }
                                }

                                if (!session.IsFSRRedesign)
                                {
                                    if (!session.IsAward)
                                        flight.AirfareDisplayValue += !selected || mixedCabin ? "*" : "";
                                }
                            }

                            flight.SeatsRemaining = seatsRemaining;
                            flight.Selected = selected;
                            flight.ServiceClassDescription = description;

                            if (string.IsNullOrEmpty(flight.Meal))
                            {
                                flight.Meal = !string.IsNullOrEmpty(displayProduct.MealDescription) ? displayProduct.MealDescription : "None";
                                flight.ServiceClass = displayProduct.BookingCode;

                                flight.Messages = new List<MOBSHOPMessage>();

                                MOBSHOPMessage msg = new MOBSHOPMessage();
                                msg.FlightNumberField = flight.FlightNumber;
                                msg.TripId = flight.TripId;
                                msg.MessageCode = displayProduct.Description + " (" + displayProduct.BookingCode + ")";
                                if (selected && _shoppingUtility.IsIBeLiteFare(displayProduct)) // bug 277549: update the message for IBE Lite only when customer switch ON the 'Show Basic Economy fares'
                                {
                                    msg.MessageCode = msg.MessageCode + " " + displayProduct.CabinTypeText; // EX: United Economy (K) (first bag charge/no changes allowed)
                                }
                                flight.Messages.Add(msg);

                                msg = new MOBSHOPMessage();
                                msg.FlightNumberField = flight.FlightNumber;
                                msg.TripId = flight.TripId;
                                msg.MessageCode = !string.IsNullOrEmpty(displayProduct.MealDescription) ? displayProduct.MealDescription : "None";
                                flight.Messages.Add(msg);

                                flight.MatchServiceClassRequested = selected;
                                if (session.IsFSRRedesign)
                                {
                                    if (flight.Messages != null && flight.Messages.Count > 0)
                                    {
                                        if (mixedCabin)
                                        {
                                            var mixedCabinText = _configuration.GetValue<string>("MixedCabinProductBadgeText");
                                            var firstMessage = flight.Messages.First().MessageCode;
                                            var mixedCabinColorCode = _configuration.GetValue<string>("MixedCabinTextColorCode");
                                            if (await _featureSettings.GetFeatureSettingValue("EnableNewBackGroundColor").ConfigureAwait(false) && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("Android_Atmos2_New_MixedCabinTextColorCode_AppVersion"), _configuration.GetValue<string>("Iphone_Atmos2_New_MixedCabinTextColorCode_AppVersion")))
                                            {
                                                mixedCabinColorCode = _configuration.GetValue<string>("Atmos2_New_MixedCabinTextColorCode");
                                            }
                                            var message1 = string.Format("<span style='color:{0}'>{1} {2}</span>", mixedCabinColorCode, mixedCabinText, firstMessage);
                                            var message2 = flight.Messages.Last().MessageCode;
                                            flight.LineOfFlightMessage = string.Join(" / ", message1, message2);
                                        }
                                        else
                                        {
                                            flight.LineOfFlightMessage = string.Join(" / ", flight.Messages.Select(x => x.MessageCode));
                                        }
                                    }
                                }
                            }

                            if (!supressLMX && displayProduct.LmxLoyaltyTiers != null && displayProduct.LmxLoyaltyTiers.Count > 0)
                            {
                                //flight.YqyrMessage = _configuration.GetValue<string>("MP2015YQYRMessage");

                                foreach (LmxLoyaltyTier tier in displayProduct.LmxLoyaltyTiers)
                                {
                                    if (tier != null && string.IsNullOrEmpty(tier.ErrorCode))
                                    {
                                        int tempStatus = premierStatusLevel;
                                        if (premierStatusLevel > 4)//GS gets same LMX as 1K
                                            tempStatus = 4;

                                        if (tier.Level == tempStatus)
                                        {
                                            if (tier.LmxQuotes != null && tier.LmxQuotes.Count > 0)
                                            {
                                                foreach (LmxQuote quote in tier.LmxQuotes)
                                                {
                                                    switch (quote.Type.Trim().ToUpper())
                                                    {
                                                        case "RDM":
                                                            flight.RdmText = string.Format("{0:#,##0}", quote.Amount);
                                                            break;
                                                        case "PQM":
                                                            flight.PqmText = string.Format("{0:#,##0}", quote.Amount);
                                                            break;
                                                        case "PQD":
                                                            flight.PqdText = TopHelper.FormatAmountForDisplay(quote.Amount, ci, true, false);
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            //Add label to response for seats remaining if AppId = 16.
                            //Make # seats to show and AppIds configurable.
                            //Reuse existing SeatsRemaingVerbiage and logic.
                            int intSeatsRemainingLimit = 0;
                            string strAppIDs = String.Empty;

                            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("mWebSeatsRemainingLimit")))
                            {
                                intSeatsRemainingLimit = _configuration.GetValue<int>("mWebSeatsRemainingLimit");
                            }
                            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("SeatsRemainingAppIDs")))
                            {
                                strAppIDs = _configuration.GetValue<string>("SeatsRemainingAppIDs");
                            }

                            if ((!selected || (_configuration.GetValue<bool>("EnableUPPCabinTextDisplay") && selected && displayProduct.ProductType.ToUpper().Contains("ECO-PREMIUM"))) && string.IsNullOrEmpty(flight.CabinDisclaimer) && !string.IsNullOrEmpty(description))
                            {
                                if (!string.IsNullOrEmpty(requestedCabin))
                                {
                                    if (string.IsNullOrEmpty(flight.YaDiscount))
                                    {
                                        if (requestedCabin.Trim().ToUpper().Contains("BUS") && !displayProduct.ProductType.ToUpper().Contains("BUS"))
                                        {
                                            flight.CabinDisclaimer = GetCabinNameFromColumn(displayProduct.ProductType, columnInfo, displayProduct.Description);

                                        }
                                        else if (requestedCabin.Trim().ToUpper().Contains("FIRST") && !displayProduct.ProductType.ToUpper().Contains("FIRST"))
                                        {
                                            flight.CabinDisclaimer = GetCabinNameFromColumn(displayProduct.ProductType, columnInfo, displayProduct.Description);

                                        }
                                        else if (requestedCabin.Trim().ToUpper().Contains("ECONOMY") && !displayProduct.ProductType.ToUpper().Contains("ECONOMY"))
                                        {
                                            flight.CabinDisclaimer = GetCabinNameFromColumn(displayProduct.ProductType, columnInfo, displayProduct.Description);
                                        }

                                        // *** ALM #23244 fixed booking cabin disclamier - Victoria July 9. 2015

                                        if (flight.CabinDisclaimer != "Economy")
                                        {
                                            if (mixedCabin)
                                            {
                                                flight.CabinDisclaimer = "Mixed cabin";
                                            }
                                            else if (flight.CabinDisclaimer.IndexOf("Business") != -1)
                                            {
                                                flight.CabinDisclaimer = "Business";
                                            }
                                            else if (flight.CabinDisclaimer.IndexOf("First") != -1)
                                            {
                                                flight.CabinDisclaimer = "First";
                                            }
                                        }
                                    }
                                    if (requestedCabin.Trim().ToUpper().Contains("BUS"))
                                    {
                                        flight.PreferredCabinName = "Business";
                                    }
                                    else if (requestedCabin.Trim().ToUpper().Contains("FIRST"))
                                    {
                                        flight.PreferredCabinName = "First";
                                    }
                                    else
                                    {
                                        flight.PreferredCabinName = "Economy";
                                    }
                                    flight.PreferredCabinMessage = "not available";
                                }
                            }
                            else if (string.IsNullOrEmpty(flight.CabinDisclaimer) && mixedCabin && string.IsNullOrEmpty(flight.YaDiscount))
                            {
                                flight.CabinDisclaimer = GetMixedCabinTextForFlight(segment);
                            }
                            //Modified this to check if it's a "Seats Remaining app and if so, don't set this value - JD
                            else if (string.IsNullOrEmpty(flight.CabinDisclaimer) && seatsRemaining < 9 && seatsRemaining > 0 && !strAppIDs.Contains("|" + session.AppID.ToString() + "|"))
                            {
                                if (appID != 0 && _shoppingUtility.FeatureVersionCheck(appID, appVersion, "EnableVerbiage", "AndroidEnableVerbiageVersion", "iPhoneEnableVerbiageVersion"))
                                {
                                    if (!displayProduct.ProductType.ToUpper().Contains("ECO-BASIC") && _configuration.GetValue<bool>("HideBasicEconomyVerbiage"))
                                    {
                                        flight.AvailSeatsDisclaimer = seatsRemaining == 1 ? (seatsRemaining.ToString() + _configuration.GetValue<string>("TicketsRemainingVerbiage")).Replace("tickets", "ticket") : seatsRemaining.ToString() + _configuration.GetValue<string>("TicketsRemainingVerbiage");
                                        if (session.IsFSRRedesign)
                                        {
                                            flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, TicketsLeftSegmentInfo(flight.AvailSeatsDisclaimer));
                                        }
                                    }
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(flight.YaDiscount))
                                    {
                                        flight.CabinDisclaimer = seatsRemaining == 1 ? (seatsRemaining.ToString() + _configuration.GetValue<string>("SeatsRemaingVerbiage")).Replace("seats", "seat") : seatsRemaining.ToString() + _configuration.GetValue<string>("SeatsRemaingVerbiage");
                                    }
                                }
                            }
                            //Added this check if it's a "Seats Remaining app set the new label value - JD
                            if (strAppIDs.Contains("|" + session.AppID.ToString() + "|"))
                            {
                                if (seatsRemaining < 9 && seatsRemaining > 0)
                                {
                                    flight.AvailSeatsDisclaimer = seatsRemaining == 1 ? (seatsRemaining.ToString() + _configuration.GetValue<string>("SeatsRemaingVerbiage")).Replace("seats", "seat") : seatsRemaining.ToString() + _configuration.GetValue<string>("SeatsRemaingVerbiage");
                                }
                            }
                            if (appID != 0 && _shoppingUtility.FeatureVersionCheck(appID, appVersion, "EnableVerbiage", "AndroidEnableVerbiageVersion", "iPhoneEnableVerbiageVersion") && string.IsNullOrEmpty(flight.AvailSeatsDisclaimer))
                            {
                                if (seatsRemaining < 9 && seatsRemaining > 0)
                                {
                                    if (!displayProduct.ProductType.ToUpper().Contains("ECO-BASIC") && _configuration.GetValue<bool>("HideBasicEconomyVerbiage"))
                                    {
                                        flight.AvailSeatsDisclaimer = seatsRemaining == 1 ? (seatsRemaining.ToString() + _configuration.GetValue<string>("TicketsRemainingVerbiage")).Replace("tickets", "ticket") : seatsRemaining.ToString() + _configuration.GetValue<string>("TicketsRemainingVerbiage");

                                        if (session.IsFSRRedesign)
                                        {
                                            flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, TicketsLeftSegmentInfo(flight.AvailSeatsDisclaimer));
                                        }
                                    }
                                }
                            }

                            //if (!string.IsNullOrEmpty(flight.YaDiscount))
                            //{
                            //    flight.CabinDisclaimer = null; // Young Adult discount trump the cabin mismatch & mixed cabin message.
                            //}
                        }
                    }
                    #endregion
                    if (ci == null)
                    {
                        ci = TopHelper.GetCultureInfo("");
                    }

                    #region
                    if (segment.Amenities != null && segment.Amenities.Count > 0)
                    {
                        foreach (Amenity amenity in segment.Amenities)
                        {
                            switch (amenity.Key.ToLower())
                            {
                                case "audiovideoondemand":
                                    flight.HasAVOnDemand = amenity.IsOffered;
                                    break;
                                case "beverages":
                                    flight.HasBeverageService = amenity.IsOffered;
                                    break;
                                case "directv":
                                    flight.HasDirecTV = amenity.IsOffered;
                                    break;
                                case "economylieflatseating":
                                    flight.HasEconomyLieFlatSeating = amenity.IsOffered;
                                    break;
                                case "economymeal":
                                    flight.HasEconomyMeal = amenity.IsOffered;
                                    break;
                                case "firstclasslieflatseating":
                                    flight.HasFirstClassLieFlatSeating = amenity.IsOffered;
                                    break;
                                case "firstclassmeal":
                                    flight.HasFirstClassMeal = amenity.IsOffered;
                                    break;
                                case "inseatpower":
                                    flight.HasInSeatPower = amenity.IsOffered;
                                    break;
                                case "wifi":
                                    flight.HasWifi = amenity.IsOffered;
                                    break;
                            }
                        }
                    }
                    #endregion
                    //flight.Cabin = string.IsNullOrEmpty(segment.CabinType) ? "" : segment.CabinType.Trim();

                    flight.Cabin = GetCabinDescription(flight.ServiceClassDescription);

                    flight.ChangeOfGauge = segment.ChangeOfPlane;
                    if (segment.Connections != null && segment.Connections.Count > 0)
                    {
                        if (_mOBSHOPDataCarrier == null)
                            _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                        var tupleResp = await GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, segment.Connections, requestedCabin, ci, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR, trip, appVersion, appID, session, additionalItems, lstMessages, searchFilters);
                        flight.Connections = tupleResp.Item1;
                        ci = tupleResp.ci;
                    }

                    flight.ConnectTimeMinutes = segment.ConnectTimeMinutes > 0 ? GetFormattedTravelTime(segment.ConnectTimeMinutes) : string.Empty;

                    flight.DepartDate = FormatDate(segment.DepartDateTime);
                    flight.DepartTime = FormatTime(segment.DepartDateTime);
                    flight.Destination = segment.Destination;
                    flight.DestinationDate = FormatDate(segment.DestinationDateTime);
                    flight.DestinationTime = FormatTime(segment.DestinationDateTime);
                    flight.DepartureDateTime = FormatDateTime(segment.DepartDateTime);
                    if (IsTripPlanSearch(session.TravelType))
                    {
                        flight.DepartureDateFormated = FormatDateTimeTripPlan(segment.DepartDateTime);
                    }
                    flight.ArrivalDateTime = FormatDateTime(segment.DestinationDateTime);

                    string destinationName = string.Empty;
                    if (!string.IsNullOrEmpty(flight.Destination))
                    {
                        destinationName = await GetAirportNameFromSavedList(flight.Destination);
                    }
                    if (string.IsNullOrEmpty(destinationName))
                    {
                        flight.DestinationDescription = segment.DestinationDescription;
                    }
                    else
                    {
                        flight.DestinationDescription = destinationName;
                    }
                    flight.DestinationCountryCode = segment.DestinationCountryCode;
                    flight.OriginCountryCode = segment.OriginCountryCode;
                    if (_configuration.GetValue<bool>("AdvisoryMsgUpdateEnable"))
                    {
                        flight.DestinationStateCode = !string.IsNullOrEmpty(segment.DestinationStateCode) ? segment.DestinationStateCode : string.Empty;
                        flight.OriginStateCode = !string.IsNullOrEmpty(segment.OriginStateCode) ? segment.OriginStateCode : string.Empty;
                    }
                    flight.EquipmentDisclosures = GetEquipmentDisclosures(segment.EquipmentDisclosures);
                    flight.IsWheelChairFits = segment.IsWheelChairFits;
                    await GetAircraftDisclaimer(flight, segment,appID,appVersion, lstMessages, session?.CatalogItems, searchFilters, session?.IsReshopChange ?? false).ConfigureAwait(false);
                    flight.FareBasisCode = segment.FareBasisCode;
                    flight.FlightNumber = segment.FlightNumber;
                    flight.GroundTime = segment.GroundTimeMinutes.ToString();
                    flight.InternationalCity = segment.InternationalCity ?? string.Empty;
                    flight.IsConnection = segment.IsConnection;
                    flight.MarketingCarrier = segment.MarketingCarrier;
                    flight.MarketingCarrierDescription = segment.MarketingCarrierDescription;
                    flight.Miles = segment.MileageActual.ToString();

                    if (_configuration.GetValue<string>("ReturnShopSelectTripOnTimePerformance") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnShopSelectTripOnTimePerformance")))
                    {
                        flight.OnTimePerformance = PopulateOnTimePerformanceSHOP(segment.OnTimePerformanceInfo);
                    }
                    flight.OperatingCarrier = segment.OperatingCarrier;
                    if (_configuration.GetValue<bool>("EnableOperatingCarrierShortForDisclosureText"))
                    {
                        if (
                            (flight.OperatingCarrier != null && flight.OperatingCarrier.ToUpper() != "UA") ||
                            (segment.OperatingCarrierShort != null && (
                                segment.OperatingCarrierShort.ToUpper().Contains("EXPRESS") ||
                                segment.OperatingCarrierShort.ToUpper().Contains("AMTRAK")
                            ))
                        )
                        {
                            flight.OperatingCarrierDescription = !string.IsNullOrEmpty(segment.OperatingCarrierShort) ? segment.OperatingCarrierShort : "";
                        }
                    }
                    else
                    {
                        if (
                            (flight.OperatingCarrier != null && flight.OperatingCarrier.ToUpper() != "UA") ||
                            (segment.OperatingCarrierDescription != null && (
                                segment.OperatingCarrierDescription.ToUpper().Contains("EXPRESS") ||
                                segment.OperatingCarrierDescription.ToUpper().Contains("AMTRAK")
                            ))
                        )
                        {
                            if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && flight.OperatingCarrier.ToUpper() == "XE")
                            {
                                flight.OperatingCarrierDescription = !string.IsNullOrEmpty(segment.OperatingCarrierDescription) ? segment.OperatingCarrierDescription : "";
                            }
                            else
                            {
                                TextInfo ti = ci.TextInfo;
                                flight.OperatingCarrierDescription = !string.IsNullOrEmpty(segment.OperatingCarrierDescription) ? ti.ToTitleCase(segment.OperatingCarrierDescription.ToLower()) : "";
                            }
                        }
                    }
                    if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && flight.OperatingCarrier != null
                        && _configuration.GetValue<string>("SupportedAirlinesFareComparison").ToString().Contains(flight.OperatingCarrier.ToUpper())
                        )
                    {
                        flight.ShowInterimScreen = true;
                        if (additionalItems != null)
                        {
                            if (additionalItems.AirlineCodes == null)
                            {
                                additionalItems.AirlineCodes = new List<string>();
                            }
                            if (!additionalItems.AirlineCodes.Contains(flight.OperatingCarrier))
                            {
                                additionalItems.AirlineCodes.Add(flight.OperatingCarrier);
                            }
                        }
                    }
                    flight.Origin = segment.Origin;

                    string originName = string.Empty;
                    if (!string.IsNullOrEmpty(flight.Origin))
                    {
                        originName = await GetAirportNameFromSavedList(flight.Origin);
                    }
                    if (string.IsNullOrEmpty(originName))
                    {
                        flight.OriginDescription = segment.OriginDescription;
                    }
                    else
                    {
                        flight.OriginDescription = originName;
                    }

                    if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI"))
                    {
                        //with share trip flag OriginDescription=Chicago, IL, US (ORD)
                        flight.OriginDecodedWithCountry = segment.OriginDescription;
                        flight.DestinationDecodedWithCountry = segment.DestinationDescription;
                    }

                    //Warnings
                    if (segment.Warnings != null && segment.Warnings.Count > 0)
                    {
                        foreach (Warning warn in segment.Warnings)
                        {
                            if (warn.Key.Trim().ToUpper() == "OVERNIGHTCONN")
                            {
                                flight.OvernightConnection = string.IsNullOrEmpty(_configuration.GetValue<string>("OvernightConnectionMessage")) ? _configuration.GetValue<string>("OvernightConnectionMessage") : warn.Title;
                            }

                            if (_configuration.GetValue<bool>("EnableChangeOfAirport") && warn != null && !string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == "CHANGE_OF_AIRPORT_SLICE" && !session.IsReshopChange)
                            {
                                flight.AirportChange = !string.IsNullOrEmpty(_configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE")) ? _configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE") : warn.Key;
                            }

                            if (session.IsFSRRedesign)
                            {
                                SetSegmentInfoMessages(flight, warn);
                            }
                        }
                    }
                    flight.StopDestination = segment.StopDestination;
                    bool changeOfGauge = false;
                    if (segment.StopInfos != null && segment.StopInfos.Count > 0)
                    {
                        flight.StopInfos = new List<Model.Shopping.MOBSHOPFlight>();
                        flight.ShowSeatMap = true;
                        bool isFlightDestionUpdated = false;
                        int travelMinutes = segment.TravelMinutes;

                        foreach (Flight stop in segment.StopInfos)
                        {
                            if (segment.EquipmentDisclosures != null && !string.IsNullOrEmpty(segment.EquipmentDisclosures.EquipmentType) && stop.EquipmentDisclosures != null && !string.IsNullOrEmpty(stop.EquipmentDisclosures.EquipmentType))
                            {
                                if (segment.EquipmentDisclosures.EquipmentType.Trim() == stop.EquipmentDisclosures.EquipmentType.Trim())
                                {
                                    flight.ChangeOfGauge = true;
                                    flight.ChangeOfPlane = segment.ChangeOfPlane;
                                    flight.IsThroughFlight = !segment.ChangeOfPlane;
                                    List<Flight> stops = new List<Flight>();
                                    stops.Add(stop);
                                    if (_mOBSHOPDataCarrier == null)
                                        _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                                    var tupleRes = await GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, stops, requestedCabin, ci, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR, trip, appVersion, appID, session, additionalItems, lstMessages, searchFilters);
                                    List<Model.Shopping.MOBSHOPFlight> stopFlights = tupleRes.Item1;
                                    ci = tupleRes.ci;
                                    foreach (Model.Shopping.MOBSHOPFlight sf in stopFlights)
                                    {
                                        sf.ChangeOfGauge = true;
                                        if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes"))
                                        {
                                            sf.IsThroughFlight = !stop.ChangeOfPlane;
                                            sf.ChangeOfPlane = stop.ChangeOfPlane;
                                            sf.GroundTime = GetFormattedGroundTime(sf.GroundTime, sf.Origin, segment.Warnings, stop.ChangeOfPlane);
                                        }
                                    }

                                    ///245704 - PROD: Mapp - Incorrect segments info is displayed for a 2-Stop flight UA1666 on choose your travel experience screen
                                    ///Srini - 03/20/2018
                                    if (_configuration.GetValue<bool>("BugFixToggleFor18C"))
                                    {
                                        flight.Destination = stop.Origin;
                                        if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes") && !isFlightDestionUpdated)
                                        {
                                            var isConnectThroughFlightFix = _configuration.GetValue<bool>("OmnicartConnectionThroughFlightFix")
                                                                            ? _configuration.GetValue<bool>("OmnicartConnectionThroughFlightFix")
                                                                            : flight.IsThroughFlight && flight.IsConnection;
                                            flight.Destination = isConnectThroughFlightFix ? segment.StopInfos.Last()?.Destination : stop.Origin;
                                            string destination = string.Empty;
                                            if (!string.IsNullOrEmpty(flight.Destination))
                                            {
                                                destination = await GetAirportNameFromSavedList(flight.Destination);
                                            }
                                            if (string.IsNullOrEmpty(destination))
                                            {
                                                flight.DestinationDescription = isConnectThroughFlightFix ? segment.StopInfos.Last()?.DestinationDescription : stop.OriginDescription;
                                            }
                                            else
                                            {
                                                flight.DestinationDescription = destination;
                                            }
                                            flight.DestinationDecodedWithCountry = isConnectThroughFlightFix ? segment.StopInfos.Last()?.DestinationDescription : stop.OriginDescription;
                                            flight.DestinationStateCode = isConnectThroughFlightFix ? segment.StopInfos.Last()?.DestinationStateCode : stop.OriginStateCode;
                                            flight.DestinationCountryCode = isConnectThroughFlightFix ? segment.StopInfos.Last()?.DestinationCountryCode : stop.OriginCountryCode;
                                        }
                                        isFlightDestionUpdated = true;
                                    }

                                    flight.StopInfos.AddRange(stopFlights);
                                }
                                else
                                {
                                    changeOfGauge = true;
                                    ///245704 - PROD: Mapp - Incorrect segments info is displayed for a 2-Stop flight UA1666 on choose your travel experience screen
                                    ///Srini - 03/20/2018
                                    if (!_configuration.GetValue<bool>("BugFixToggleFor18C") || (_configuration.GetValue<bool>("BugFixToggleFor18C") && !isFlightDestionUpdated))
                                    {
                                        flight.Destination = stop.Origin;
                                        isFlightDestionUpdated = true;
                                    }

                                    string destination = string.Empty;
                                    if (!string.IsNullOrEmpty(flight.Destination))
                                    {
                                        destination = await GetAirportNameFromSavedList(flight.Destination);
                                    }
                                    if (string.IsNullOrEmpty(destination))
                                    {
                                        flight.DestinationDescription = stop.OriginDescription;
                                    }
                                    else
                                    {
                                        flight.DestinationDescription = destination;
                                    }

                                    flight.DestinationDate = FormatDate(Convert.ToDateTime(stop.DepartDateTime).AddMinutes(-stop.GroundTimeMinutes).ToString());
                                    flight.DestinationTime = FormatTime(Convert.ToDateTime(stop.DepartDateTime).AddMinutes(-stop.GroundTimeMinutes).ToString());
                                    flight.ArrivalDateTime = FormatDateTime(Convert.ToDateTime(stop.DepartDateTime).AddMinutes(-stop.GroundTimeMinutes).ToString());
                                    flight.TravelTime = segment.TravelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes > 0 ? GetFormattedTravelTime(segment.TravelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes) : string.Empty;
                                    flight.ChangeOfPlane = segment.ChangeOfPlane;

                                    Model.Shopping.MOBSHOPFlight stopFlight = new Model.Shopping.MOBSHOPFlight();

                                    stopFlight.EquipmentDisclosures = GetEquipmentDisclosures(stop.EquipmentDisclosures);
                                    await GetAircraftDisclaimer(flight, segment, appID, appVersion, lstMessages, session?.CatalogItems, searchFilters, session?.IsReshopChange ?? false).ConfigureAwait(false);
                                    stopFlight.FlightNumber = stop.FlightNumber;
                                    stopFlight.ChangeOfGauge = stop.ChangeOfPlane;
                                    stopFlight.ShowSeatMap = true;
                                    stopFlight.DepartDate = FormatDate(stop.DepartDateTime);
                                    stopFlight.DepartTime = FormatTime(stop.DepartDateTime);
                                    stopFlight.Origin = stop.Origin;
                                    stopFlight.Destination = stop.Destination;
                                    stopFlight.DestinationDate = FormatDate(stop.DestinationDateTime);
                                    stopFlight.DestinationTime = FormatTime(stop.DestinationDateTime);
                                    stopFlight.DepartureDateTime = FormatDateTime(stop.DepartDateTime);
                                    stopFlight.ArrivalDateTime = FormatDateTime(stop.DestinationDateTime);
                                    stopFlight.IsCovidTestFlight = stop.IsCovidTestingRequired;
                                    stopFlight.GroundTime = stop.GroundTimeMinutes > 0 ? GetFormattedTravelTime(stop.GroundTimeMinutes) : String.Empty;
                                    stopFlight.ChangeOfPlane = stop.ChangeOfPlane;
                                    if (!_configuration.GetValue<bool>("DisableOperatingCarrierFlightNumberChanges"))
                                    {
                                        stopFlight.OperatingCarrierFlightNumber = stop.OriginalFlightNumber;
                                    }

                                    ///57783 - BUG 390826 CSL:  Class of service information is not included for certain segments on Mobile
                                    ///Srini - 02/20/2018
                                    if (_configuration.GetValue<bool>("BugFixToggleFor18B") && displayProductForStopInfo != null)
                                    {
                                        stopFlight.Meal = !string.IsNullOrEmpty(displayProductForStopInfo.MealDescription) ? displayProductForStopInfo.MealDescription : "None";
                                        stopFlight.ServiceClass = displayProductForStopInfo.BookingCode;

                                        stopFlight.Messages = new List<MOBSHOPMessage>();

                                        MOBSHOPMessage msg = new MOBSHOPMessage();
                                        msg.FlightNumberField = flight.FlightNumber;
                                        msg.TripId = flight.TripId;
                                        msg.MessageCode = displayProductForStopInfo.Description + " (" + displayProductForStopInfo.BookingCode + ")";
                                        stopFlight.Messages.Add(msg);

                                        msg = new MOBSHOPMessage();
                                        msg.FlightNumberField = flight.FlightNumber;
                                        msg.TripId = flight.TripId;
                                        msg.MessageCode = !string.IsNullOrEmpty(displayProductForStopInfo.MealDescription) ? displayProductForStopInfo.MealDescription : "None";
                                        stopFlight.Messages.Add(msg);

                                        flight.MatchServiceClassRequested = selectedForStopInfo;
                                    }
                                    if (session.IsFSRRedesign)
                                    {
                                        if (stopFlight.Messages != null && stopFlight.Messages.Count > 0)
                                        {
                                            if (stopFlight.ShoppingProducts != null && stopFlight.ShoppingProducts.Count > 0 && stopFlight.ShoppingProducts.Any(p => p.IsMixedCabin))
                                            {
                                                var mixedCabinText = _configuration.GetValue<string>("MixedCabinProductBadgeText");
                                                var firstMessage = stopFlight.Messages.First().MessageCode;
                                                var mixedCabinColorCode = _configuration.GetValue<string>("MixedCabinTextColorCode");
                                                if (await _featureSettings.GetFeatureSettingValue("EnableNewBackGroundColor").ConfigureAwait(false) && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("Android_Atmos2_New_MixedCabinTextColorCode_AppVersion"), _configuration.GetValue<string>("Iphone_Atmos2_New_MixedCabinTextColorCode_AppVersion")))
                                                {
                                                    mixedCabinColorCode = _configuration.GetValue<string>("Atmos2_New_MixedCabinTextColorCode");
                                                }
                                                var message1 = string.Format("<span style='color:{0}'>{1} {2}</span>", mixedCabinColorCode, mixedCabinText, firstMessage);
                                                var message2 = stopFlight.Messages.Last().MessageCode;
                                                stopFlight.LineOfFlightMessage = string.Join(" / ", message1, message2);
                                            }
                                            else
                                            {
                                                stopFlight.LineOfFlightMessage = string.Join(" / ", stopFlight.Messages.Select(x => x.MessageCode));
                                            }
                                        }
                                    }
                                    //Added Carrier code for the bug 218201 by Niveditha.Didn't add Marketing Carrier description as per suggestion by Jada sreenivas.
                                    stopFlight.MarketingCarrier = flight.MarketingCarrier;

                                    string stopDestination = string.Empty;
                                    if (!string.IsNullOrEmpty(stopFlight.Destination))
                                    {
                                        stopDestination = await GetAirportNameFromSavedList(stopFlight.Destination);
                                    }
                                    if (string.IsNullOrEmpty(stopDestination))
                                    {
                                        stopFlight.DestinationDescription = stop.DestinationDescription;
                                    }
                                    else
                                    {
                                        stopFlight.DestinationDescription = stopDestination;
                                    }

                                    string stopOrigin = string.Empty;
                                    if (!string.IsNullOrEmpty(stopFlight.Origin))
                                    {
                                        stopOrigin = await GetAirportNameFromSavedList(stopFlight.Origin);
                                    }
                                    if (string.IsNullOrEmpty(stopOrigin))
                                    {
                                        stopFlight.OriginDescription = stop.OriginDescription;
                                    }
                                    else
                                    {
                                        stopFlight.OriginDescription = stopOrigin;
                                    }

                                    if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI"))
                                    {
                                        //with share trip flag OriginDescription=Chicago, IL, US (ORD)
                                        stopFlight.OriginDecodedWithCountry = stop.OriginDescription;
                                        stopFlight.DestinationDecodedWithCountry = stop.DestinationDescription;
                                    }

                                    stopFlight.TravelTime = stop.TravelMinutes > 0 ? GetFormattedTravelTime(stop.TravelMinutes) : string.Empty;

                                    if (session.IsFSRRedesign)
                                    {
                                        if (stop.Warnings != null && stop.Warnings.Count > 0)
                                        {
                                            foreach (Warning warn in stop.Warnings)
                                            {
                                                SetSegmentInfoMessages(stopFlight, warn);
                                            }
                                        }
                                    }

                                    flight.StopInfos.Add(stopFlight);
                                }
                            }
                            if (_configuration.GetValue<bool>("DoubleDisclosureFix"))
                            {
                                travelMinutes = travelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes > 0 ? travelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes : 0;
                            }
                        }
                        if (_configuration.GetValue<bool>("DoubleDisclosureFix"))
                        {
                            flight.Destination = flight.IsThroughFlight && flight.IsConnection ? segment.StopInfos?.Last().Destination : segment.StopInfos[0].Origin;
                            flight.DestinationDate = FormatDate(Convert.ToDateTime(segment.StopInfos[0].DepartDateTime).AddMinutes(-segment.StopInfos[0].GroundTimeMinutes).ToString());
                            flight.DestinationTime = FormatTime(Convert.ToDateTime(segment.StopInfos[0].DepartDateTime).AddMinutes(-segment.StopInfos[0].GroundTimeMinutes).ToString());
                            flight.TravelTime = travelMinutes > 0 ? GetFormattedTravelTime(travelMinutes) : string.Empty;
                        }
                        if (flight.IsConnection && flight.IsThroughFlight)
                        {
                            flight.TravelTime = segment.TravelMinutes > 0 ? GetFormattedTravelTime(segment.TravelMinutes) : string.Empty;
                        }
                    }

                    flight.Stops = segment.StopInfos != null ? segment.StopInfos.Count : 0;

                    if (_configuration.GetValue<bool>("DoubleDisclosureFix"))
                    {
                        if (!changeOfGauge && string.IsNullOrEmpty(flight.TravelTime))
                            flight.TravelTime = segment.TravelMinutes > 0 ? GetFormattedTravelTime(segment.TravelMinutes) : string.Empty;
                    }
                    else
                    {
                        if (!changeOfGauge)
                        {
                            flight.TravelTime = segment.TravelMinutes > 0 ? GetFormattedTravelTime(segment.TravelMinutes) : string.Empty;
                        }
                    }
                    flight.TotalTravelTime = segment.TravelMinutesTotal > 0 ? GetFormattedTravelTime(segment.TravelMinutesTotal) : string.Empty;
                    flight.TravelTimeInMinutes = segment.TravelMinutes;
                    flight.TripId = segment.BBXSolutionSetId;

                    if (_mOBSHOPDataCarrier == null)
                        _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                    var tupleResponse = await PopulateProducts(_mOBSHOPDataCarrier, segment.Products, sessionId, flight, requestedCabin, segment, lowestFare, columnInfo, premierStatusLevel, fareClass, isELFFareDisplayAtFSR, appVersion, session,additionalItems);
                    flight.ShoppingProducts = tupleResponse.Item1;
                    flight = tupleResponse.flight;
                    SetAutoFocusIfMissed(session, isELFFareDisplayAtFSR, flight.ShoppingProducts, bestProductType);
                    if (await _featureSettings.GetFeatureSettingValue("EnableAwardStrikeThroughPriceEnhancement").ConfigureAwait(false) && additionalItems != null)
                    {
                        flight.StrikeThroughDisplayType = additionalItems?.StrikeThroughDisplayType;
                        additionalItems.StrikeThroughDisplayType = "";
                    }

                    #endregion
                    if (isConnection)
                    {
                        flights.Add(flight);
                    }
                    else
                    {
                        if (flight.ShoppingProducts != null && flight.ShoppingProducts.Count > 0)
                            flights.Add(flight);
                        #region REST SHOP and Select Trip Tuning Changes - Venkat Apirl 20, 2015
                        if (_configuration.GetValue<bool>("HandlePagingAtRESTSide") && flights.Count == _configuration.GetValue<int>("OrgarnizeResultsRequestPageSize"))
                        {
                            break;
                        }
                        #endregion
                    }
                }
            }

            return (flights, ci);
        }
        public static int countDisplay = 0;

        public async Task<List<MOBSHOPFlight>> GetFlightsAsync(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string sessionId, string cartId, List<Flight> segments, string requestedCabin, decimal lowestFare, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClass, bool updateAmenities = false, bool isConnection = false, bool isELFFareDisplayAtFSR = true, MOBSHOPTrip trip = null, string appVersion = "", int appID = 0, Session session = null,
            MOBAdditionalItems additionalItems = null, List<CMSContentMessage> lstMessages = null, MOBSearchFilters searchFilters = null)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            if (session == null)
            {
                session = new Session();
                session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);
            }

            // #region delete code
            // //Delete
            // countDisplay = countDisplay + 1;
            // int localcountDisplay = countDisplay;
            //// _logger.LogWarning("GetFlightsAsync begin {number}", localcountDisplay);
            // #endregion

            supressLMX = session.SupressLMXForAppID;
            #endregion
            try
            {
                if (isGetAirportListInOneCallToggleOn() && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    airportsList = await _sessionHelperService.GetSession<AirportDetailsList>(sessionId, new AirportDetailsList().ObjectName, new List<string> { sessionId, new AirportDetailsList().ObjectName }).ConfigureAwait(false);

                    if (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0)
                    {
                        airportsList = await GetAllAiportsList(segments);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllAiportsList {@GetFlights-GetAllAiportsList}", JsonConvert.SerializeObject(ex));
                _logger.LogError("GetAllAiportsList {@PopulateMetaTrips-Trip}", JsonConvert.SerializeObject(segments));
            }

            List<Model.Shopping.MOBSHOPFlight> flights = null;
            CultureInfo ci = null;
            if (segments != null && segments.Count > 0)
            {
                flights = new List<Model.Shopping.MOBSHOPFlight>();

                foreach (Flight segment in segments)
                {
                    #region

                    Model.Shopping.MOBSHOPFlight flight = new Model.Shopping.MOBSHOPFlight();
                    flight.Messages = new List<Model.Shopping.MOBSHOPMessage>();
                    string AddCollectProductID = string.Empty;
                    Product displayProductForStopInfo = null;
                    bool selectedForStopInfo = false;
                    string bestProductType = null;
                    // #633226 Reshop SDC Add coller waiver status
                    if (session.IsReshopChange)
                    {
                        flight.isAddCollectWaived = GetAddCollectWaiverStatus(segment, out AddCollectProductID, appID, appVersion);
                        flight.AddCollectProductId = AddCollectProductID;
                    }

                    if (!string.IsNullOrEmpty(segment.BBXHash))
                    {
                        flight.FlightId = segment.BBXHash;
                        flight.ProductId = segment.BBXCellId ?? string.Empty;
                        flight.GovtMessage = segment.SubjectToGovernmentApproval ? _configuration.GetValue<string>("SubjectToGovernmentApprovalMessage") : string.Empty;
                    }

                    flight.TripIndex = segment.TripIndex;
                    flight.FlightHash = segment.Hash;
                    flight.IsCovidTestFlight = segment.IsCovidTestingRequired;
                    if (!_configuration.GetValue<bool>("DisableOperatingCarrierFlightNumberChanges"))
                    {
                        flight.OperatingCarrierFlightNumber = segment.OriginalFlightNumber;
                    }

                    if (session.IsExpertModeEnabled && !session.IsAward)
                    {
                        if (!string.IsNullOrEmpty(segment.BookingClassAvailability))
                        {
                            flight.BookingClassAvailability = segment.BookingClassAvailability.Replace('|', ' ');
                        }
                    }

                    #region //NEED LOGIC TO DETERMINE SELECTED PRODUCT HERE TO GET PRICING
                    if (segment.Products != null && segment.Products.Count > 0/* && segment.Products[0] != null && segment.Products[0].Prices != null && segment.Products[0].Prices.Count > 0*/)
                    {
                        bool selected;
                        int seatsRemaining = 0;
                        bool mixedCabin;
                        string description = string.Empty;

                        AssignCorporateFareIndicator(segment, flight, session.TravelType);

                        Product displayProduct = GetMatrixDisplayProduct(segment.Products, requestedCabin, columnInfo, out ci, out selected, out description, out mixedCabin, out seatsRemaining, fareClass, isConnection, isELFFareDisplayAtFSR);
                        displayProductForStopInfo = displayProduct;
                        selectedForStopInfo = selected;
                        if (_shoppingUtility.EnableYoungAdult(session.IsReshopChange))
                        {
                            if (IsYoungAdultProduct(segment.Products))
                            {
                                flight.YaDiscount = "Discounted";
                            }
                        }
                        GetBestProductTypeTripPlanner(session, displayProduct, selected, ref bestProductType);
                        if (displayProduct != null && isConnection || (displayProduct.Prices != null && displayProduct.Prices.Count > 0))
                        {
                            if (displayProduct != null && !isConnection && displayProduct.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES)
                            {
                                //WADE-adding logic to add in close in award fee if present
                                decimal closeInFee = 0;
                                if (displayProduct.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES)
                                {
                                    foreach (PricingItem p in displayProduct.Prices)
                                    {
                                        if (p.PricingType.Trim().ToUpper() == PRICING_TYPE_CLOSE_IN_FEE)
                                        {
                                            closeInFee = p.Amount;
                                            break;
                                        }
                                    }

                                }
                                flight.Airfare = displayProduct.Prices[0].Amount;
                                ///208848 : PROD Basic Economy mApps Price in footer of FSR is showing lowest BE fare when BE switch is off 
                                ///Srini - 12/29/2017
                                if (_configuration.GetValue<bool>("BugFixToggleFor18B") &&
                                      (_mOBSHOPDataCarrier.FsrMinPrice > displayProduct.Prices[0].Amount ||
                                      (_mOBSHOPDataCarrier.FsrMinPrice == 0 && displayProduct.Prices[0].Amount > 0)) &&
                                      !mixedCabin &&
                                      (
                                        requestedCabin.Contains("ECONOMY") ||
                                        (requestedCabin.Contains("BUS") && (displayProduct.ProductType.Contains("BUS") || displayProduct.ProductType.Contains("FIRST"))) ||
                                        (requestedCabin.Contains("FIRST") && displayProduct.ProductType.Contains("FIRST"))
                                      )
                                   )
                                {
                                    _mOBSHOPDataCarrier.FsrMinPrice = displayProduct.Prices[0].Amount;
                                }
                                //TFS WI 68391
                                //Do_Not_Allow_Miles_Zero_AwardSearch we using this as a Flag if its Null means allow if its not Null even the value is true or false do not allow Zero Miles Award Booking.
                                if (displayProduct.Prices[0].Amount > 0 || _configuration.GetValue<string>("Do_Not_Allow_Miles_Zero_AwardSearch") == null)
                                {
                                    if (!_configuration.GetValue<bool>("NGRPAwardCalendarMP2017NewUISwitch"))
                                    {
                                        flight.AirfareDisplayValue = TopHelper.FormatAmountForDisplay(displayProduct.Prices[1].Amount + closeInFee, ci, true, true);
                                    }
                                    else
                                    {
                                        flight.AirfareDisplayValue = TopHelper.FormatAmountForDisplay(displayProduct.Prices[1].Amount + closeInFee, ci, false, true);
                                    }
                                    flight.MilesDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(flight.Airfare.ToString(), true);
                                }
                                else
                                {
                                    //Bypassing 0 dollar condition for PromoCode - TODO check the condition
                                    if (!(await ByPassZeroDollar(displayProduct)))
                                    {
                                        flight.AirfareDisplayValue = "N/A";
                                        flight.MilesDisplayValue = "N/A";
                                        if (_configuration.GetValue<string>("ReturnExceptionForAnyZeroDollarAirFareValue") != null && Convert.ToBoolean(_configuration.GetValue<bool>("ReturnExceptionForAnyZeroDollarAirFareValue").ToString()))
                                        {
                                            throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                                        }
                                    }
                                }
                                flight.MilesDisplayValue += !selected || mixedCabin ? "*" : "";
                                flight.IsAwardSaver = displayProduct.AwardType.Trim().ToLower().Contains("saver") && !displayProduct.ProductType.ToUpper().Contains("ECONOMY") ? true : false;
                            }
                            else if (!isConnection)
                            {
                                ///208848 : PROD Basic Economy mApps Price in footer of FSR is showing lowest BE fare when BE switch is off 
                                ///Srini - 12/29/2017
                                if (_configuration.GetValue<bool>("BugFixToggleFor18B") &&
                                      (_mOBSHOPDataCarrier.FsrMinPrice > displayProduct.Prices[0].Amount ||
                                      (_mOBSHOPDataCarrier.FsrMinPrice == 0 && displayProduct.Prices[0].Amount > 0)) &&
                                      !mixedCabin &&
                                      (
                                        requestedCabin.Contains("ECONOMY") ||
                                        (requestedCabin.Contains("BUS") && (displayProduct.ProductType.Contains("BUS") || displayProduct.ProductType.Contains("FIRST"))) ||
                                        (requestedCabin.Contains("FIRST") && displayProduct.ProductType.Contains("FIRST"))
                                      )
                                   )
                                {
                                    _mOBSHOPDataCarrier.FsrMinPrice = displayProduct.Prices[0].Amount;
                                }
                                flight.Airfare = displayProduct.Prices[0].Amount;
                                if (session.IsReshopChange)
                                {
                                    if (session.IsAward)
                                    {
                                        //if(trip.LastTripIndexRequested == 1)
                                        //    flight.AirfareDisplayValue = ReShopAirfareDisplayValue(ci, displayProduct.Prices, session.IsAward, true);
                                        //else
                                        flight.AirfareDisplayValue = ReShopAirfareDisplayValue(ci, displayProduct.Prices, session.IsAward, true);
                                        var milesAmountToDisplay = ReshopAwardPrice(displayProduct.Prices);

                                        if (milesAmountToDisplay == null)
                                            flight.MilesDisplayValue = "NA";
                                        else
                                        {
                                            flight.MilesDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(Convert.ToString(milesAmountToDisplay.Amount), true);
                                            flight.MilesDisplayValue += !selected || mixedCabin ? "*" : "";
                                        }
                                    }
                                    else
                                    {
                                        flight.AirfareDisplayValue = ReShopAirfareDisplayValue(ci, displayProduct.Prices);

                                        if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                                        {
                                            flight.ReshopFees = ReshopAirfareDisplayText(displayProduct.Prices);
                                        }
                                        if (_shoppingUtility.EnableReShopAirfareCreditDisplay(appID, appVersion))
                                        {
                                            flight = ReShopAirfareCreditDisplayFSR(ci, displayProduct, flight);
                                        }
                                    }
                                }
                                else if (displayProduct.Prices[0].Amount > 0 || (await _featureSettings.GetFeatureSettingValue("EnableFSRETCTravelCreditsFeature").ConfigureAwait(false)
                                                    && displayProduct.Prices.Any(r => r.PricingType == "referencePrice" && r.Amount > 0)))
                                {
                                    string displayPrice = TopHelper.FormatAmountForDisplay(displayProduct.Prices[0].Amount, ci, true);
                                    flight.AirfareDisplayValue = string.IsNullOrEmpty(displayPrice) ? "Not available" : displayPrice;
                                }
                                else
                                {
                                    //Bypassing 0 dollar condition for PromoCode - TODO check the condition
                                    if (!(await ByPassZeroDollar(displayProduct)))// added this condition for testing new promo 0 $ flow.
                                    {
                                        flight.AirfareDisplayValue = "N/A";
                                        if (_configuration.GetValue<string>("ReturnExceptionForAnyZeroDollarAirFareValue") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnExceptionForAnyZeroDollarAirFareValue").ToString()))
                                        {
                                            // Added as part of Bug 180337:mApp: "Sorry something went wrong... " Error message is displayed when selected cabin for second segment in the multi trip
                                            // throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                                            throw new MOBUnitedException(_configuration.GetValue<string>("BookingDetail_ITAError_For_CSL_10047__Error_Code"));
                                        }
                                    }
                                }

                                if (!session.IsFSRRedesign)
                                {
                                    if (!session.IsAward)
                                        flight.AirfareDisplayValue += !selected || mixedCabin ? "*" : "";
                                }
                            }

                            flight.SeatsRemaining = seatsRemaining;
                            flight.Selected = selected;
                            flight.ServiceClassDescription = description;

                            if (string.IsNullOrEmpty(flight.Meal))
                            {
                                flight.Meal = !string.IsNullOrEmpty(displayProduct.MealDescription) ? displayProduct.MealDescription : "None";
                                flight.ServiceClass = displayProduct.BookingCode;

                                flight.Messages = new List<MOBSHOPMessage>();

                                MOBSHOPMessage msg = new MOBSHOPMessage();
                                msg.FlightNumberField = flight.FlightNumber;
                                msg.TripId = flight.TripId;
                                msg.MessageCode = displayProduct.Description + " (" + displayProduct.BookingCode + ")";
                                if (selected && _shoppingUtility.IsIBeLiteFare(displayProduct)) // bug 277549: update the message for IBE Lite only when customer switch ON the 'Show Basic Economy fares'
                                {
                                    msg.MessageCode = msg.MessageCode + " " + displayProduct.CabinTypeText; // EX: United Economy (K) (first bag charge/no changes allowed)
                                }
                                flight.Messages.Add(msg);

                                msg = new MOBSHOPMessage();
                                msg.FlightNumberField = flight.FlightNumber;
                                msg.TripId = flight.TripId;
                                msg.MessageCode = !string.IsNullOrEmpty(displayProduct.MealDescription) ? displayProduct.MealDescription : "None";
                                flight.Messages.Add(msg);

                                flight.MatchServiceClassRequested = selected;
                                if (session.IsFSRRedesign)
                                {
                                    if (flight.Messages != null && flight.Messages.Count > 0)
                                    {
                                        if (mixedCabin)
                                        {
                                            var mixedCabinText = _configuration.GetValue<string>("MixedCabinProductBadgeText");
                                            var firstMessage = flight.Messages.First().MessageCode;
                                            var mixedCabinColorCode = _configuration.GetValue<string>("MixedCabinTextColorCode");
                                            if (await _featureSettings.GetFeatureSettingValue("EnableNewBackGroundColor").ConfigureAwait(false) && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("Android_Atmos2_New_MixedCabinTextColorCode_AppVersion"), _configuration.GetValue<string>("Iphone_Atmos2_New_MixedCabinTextColorCode_AppVersion")))
                                            {
                                                mixedCabinColorCode = _configuration.GetValue<string>("Atmos2_New_MixedCabinTextColorCode");
                                            }
                                            var message1 = string.Format("<span style='color:{0}'>{1} {2}</span>", mixedCabinColorCode, mixedCabinText, firstMessage);
                                            var message2 = flight.Messages.Last().MessageCode;
                                            flight.LineOfFlightMessage = string.Join(" / ", message1, message2);
                                        }
                                        else
                                        {
                                            flight.LineOfFlightMessage = string.Join(" / ", flight.Messages.Select(x => x.MessageCode));
                                        }
                                    }
                                }
                            }

                            if (!supressLMX && displayProduct.LmxLoyaltyTiers != null && displayProduct.LmxLoyaltyTiers.Count > 0)
                            {
                                //flight.YqyrMessage = _configuration.GetValue<string>("MP2015YQYRMessage");

                                foreach (LmxLoyaltyTier tier in displayProduct.LmxLoyaltyTiers)
                                {
                                    if (tier != null && string.IsNullOrEmpty(tier.ErrorCode))
                                    {
                                        int tempStatus = premierStatusLevel;
                                        if (premierStatusLevel > 4)//GS gets same LMX as 1K
                                            tempStatus = 4;

                                        if (tier.Level == tempStatus)
                                        {
                                            if (tier.LmxQuotes != null && tier.LmxQuotes.Count > 0)
                                            {
                                                foreach (LmxQuote quote in tier.LmxQuotes)
                                                {
                                                    switch (quote.Type.Trim().ToUpper())
                                                    {
                                                        case "RDM":
                                                            flight.RdmText = string.Format("{0:#,##0}", quote.Amount);
                                                            break;
                                                        case "PQM":
                                                            flight.PqmText = string.Format("{0:#,##0}", quote.Amount);
                                                            break;
                                                        case "PQD":
                                                            flight.PqdText = TopHelper.FormatAmountForDisplay(quote.Amount, ci, true, false);
                                                            break;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            //Add label to response for seats remaining if AppId = 16.
                            //Make # seats to show and AppIds configurable.
                            //Reuse existing SeatsRemaingVerbiage and logic.
                            int intSeatsRemainingLimit = 0;
                            string strAppIDs = String.Empty;

                            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("mWebSeatsRemainingLimit")))
                            {
                                intSeatsRemainingLimit = _configuration.GetValue<int>("mWebSeatsRemainingLimit");
                            }
                            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("SeatsRemainingAppIDs")))
                            {
                                strAppIDs = _configuration.GetValue<string>("SeatsRemainingAppIDs");
                            }

                            if ((!selected || (_configuration.GetValue<bool>("EnableUPPCabinTextDisplay") && selected && displayProduct.ProductType.ToUpper().Contains("ECO-PREMIUM"))) && string.IsNullOrEmpty(flight.CabinDisclaimer) && !string.IsNullOrEmpty(description))
                            {
                                if (!string.IsNullOrEmpty(requestedCabin))
                                {
                                    if (string.IsNullOrEmpty(flight.YaDiscount))
                                    {
                                        if (requestedCabin.Trim().ToUpper().Contains("BUS") && !displayProduct.ProductType.ToUpper().Contains("BUS"))
                                        {
                                            flight.CabinDisclaimer = GetCabinNameFromColumn(displayProduct.ProductType, columnInfo, displayProduct.Description);

                                        }
                                        else if (requestedCabin.Trim().ToUpper().Contains("FIRST") && !displayProduct.ProductType.ToUpper().Contains("FIRST"))
                                        {
                                            flight.CabinDisclaimer = GetCabinNameFromColumn(displayProduct.ProductType, columnInfo, displayProduct.Description);

                                        }
                                        else if (requestedCabin.Trim().ToUpper().Contains("ECONOMY") && !displayProduct.ProductType.ToUpper().Contains("ECONOMY"))
                                        {
                                            flight.CabinDisclaimer = GetCabinNameFromColumn(displayProduct.ProductType, columnInfo, displayProduct.Description);
                                        }

                                        // *** ALM #23244 fixed booking cabin disclamier - Victoria July 9. 2015

                                        if (flight.CabinDisclaimer != "Economy")
                                        {
                                            if (mixedCabin)
                                            {
                                                flight.CabinDisclaimer = "Mixed cabin";
                                            }
                                            else if (flight.CabinDisclaimer.IndexOf("Business") != -1)
                                            {
                                                flight.CabinDisclaimer = "Business";
                                            }
                                            else if (flight.CabinDisclaimer.IndexOf("First") != -1)
                                            {
                                                flight.CabinDisclaimer = "First";
                                            }
                                        }
                                    }
                                    if (requestedCabin.Trim().ToUpper().Contains("BUS"))
                                    {
                                        flight.PreferredCabinName = "Business";
                                    }
                                    else if (requestedCabin.Trim().ToUpper().Contains("FIRST"))
                                    {
                                        flight.PreferredCabinName = "First";
                                    }
                                    else
                                    {
                                        flight.PreferredCabinName = "Economy";
                                    }
                                    flight.PreferredCabinMessage = "not available";
                                }
                            }
                            else if (string.IsNullOrEmpty(flight.CabinDisclaimer) && mixedCabin && string.IsNullOrEmpty(flight.YaDiscount))
                            {
                                flight.CabinDisclaimer = GetMixedCabinTextForFlight(segment);
                            }
                            //Modified this to check if it's a "Seats Remaining app and if so, don't set this value - JD
                            else if (string.IsNullOrEmpty(flight.CabinDisclaimer) && seatsRemaining < 9 && seatsRemaining > 0 && !strAppIDs.Contains("|" + session.AppID.ToString() + "|"))
                            {
                                if (appID != 0 && _shoppingUtility.FeatureVersionCheck(appID, appVersion, "EnableVerbiage", "AndroidEnableVerbiageVersion", "iPhoneEnableVerbiageVersion"))
                                {
                                    if (!displayProduct.ProductType.ToUpper().Contains("ECO-BASIC") && _configuration.GetValue<bool>("HideBasicEconomyVerbiage"))
                                    {
                                        flight.AvailSeatsDisclaimer = seatsRemaining == 1 ? (seatsRemaining.ToString() + _configuration.GetValue<string>("TicketsRemainingVerbiage")).Replace("tickets", "ticket") : seatsRemaining.ToString() + _configuration.GetValue<string>("TicketsRemainingVerbiage");
                                        if (session.IsFSRRedesign)
                                        {
                                            flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, TicketsLeftSegmentInfo(flight.AvailSeatsDisclaimer));
                                        }
                                    }
                                }
                                else
                                {
                                    if (string.IsNullOrEmpty(flight.YaDiscount))
                                    {
                                        flight.CabinDisclaimer = seatsRemaining == 1 ? (seatsRemaining.ToString() + _configuration.GetValue<string>("SeatsRemaingVerbiage")).Replace("seats", "seat") : seatsRemaining.ToString() + _configuration.GetValue<string>("SeatsRemaingVerbiage");
                                    }
                                }
                            }
                            //Added this check if it's a "Seats Remaining app set the new label value - JD
                            if (strAppIDs.Contains("|" + session.AppID.ToString() + "|"))
                            {
                                if (seatsRemaining < 9 && seatsRemaining > 0)
                                {
                                    flight.AvailSeatsDisclaimer = seatsRemaining == 1 ? (seatsRemaining.ToString() + _configuration.GetValue<string>("SeatsRemaingVerbiage")).Replace("seats", "seat") : seatsRemaining.ToString() + _configuration.GetValue<string>("SeatsRemaingVerbiage");
                                }
                            }
                            if (appID != 0 && _shoppingUtility.FeatureVersionCheck(appID, appVersion, "EnableVerbiage", "AndroidEnableVerbiageVersion", "iPhoneEnableVerbiageVersion") && string.IsNullOrEmpty(flight.AvailSeatsDisclaimer))
                            {
                                if (seatsRemaining < 9 && seatsRemaining > 0)
                                {
                                    if (!displayProduct.ProductType.ToUpper().Contains("ECO-BASIC") && _configuration.GetValue<bool>("HideBasicEconomyVerbiage"))
                                    {
                                        flight.AvailSeatsDisclaimer = seatsRemaining == 1 ? (seatsRemaining.ToString() + _configuration.GetValue<string>("TicketsRemainingVerbiage")).Replace("tickets", "ticket") : seatsRemaining.ToString() + _configuration.GetValue<string>("TicketsRemainingVerbiage");

                                        if (session.IsFSRRedesign)
                                        {
                                            flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, TicketsLeftSegmentInfo(flight.AvailSeatsDisclaimer));
                                        }
                                    }
                                }
                            }

                            //if (!string.IsNullOrEmpty(flight.YaDiscount))
                            //{
                            //    flight.CabinDisclaimer = null; // Young Adult discount trump the cabin mismatch & mixed cabin message.
                            //}
                        }
                    }
                    #endregion
                    if (ci == null)
                    {
                        ci = TopHelper.GetCultureInfo("");
                    }

                    #region
                    if (segment.Amenities != null && segment.Amenities.Count > 0)
                    {
                        foreach (Amenity amenity in segment.Amenities)
                        {
                            switch (amenity.Key.ToLower())
                            {
                                case "audiovideoondemand":
                                    flight.HasAVOnDemand = amenity.IsOffered;
                                    break;
                                case "beverages":
                                    flight.HasBeverageService = amenity.IsOffered;
                                    break;
                                case "directv":
                                    flight.HasDirecTV = amenity.IsOffered;
                                    break;
                                case "economylieflatseating":
                                    flight.HasEconomyLieFlatSeating = amenity.IsOffered;
                                    break;
                                case "economymeal":
                                    flight.HasEconomyMeal = amenity.IsOffered;
                                    break;
                                case "firstclasslieflatseating":
                                    flight.HasFirstClassLieFlatSeating = amenity.IsOffered;
                                    break;
                                case "firstclassmeal":
                                    flight.HasFirstClassMeal = amenity.IsOffered;
                                    break;
                                case "inseatpower":
                                    flight.HasInSeatPower = amenity.IsOffered;
                                    break;
                                case "wifi":
                                    flight.HasWifi = amenity.IsOffered;
                                    break;
                            }
                        }
                    }
                    #endregion
                    //flight.Cabin = string.IsNullOrEmpty(segment.CabinType) ? "" : segment.CabinType.Trim();

                    flight.Cabin = GetCabinDescription(flight.ServiceClassDescription);

                    flight.ChangeOfGauge = segment.ChangeOfPlane;
                    if (segment.Connections != null && segment.Connections.Count > 0)
                    {
                        if (_mOBSHOPDataCarrier == null)
                            _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                        flight.Connections = await GetFlightsAsync(_mOBSHOPDataCarrier, sessionId, cartId, segment.Connections, requestedCabin, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR, trip, appVersion, appID, session, additionalItems, lstMessages, searchFilters);
                    }

                    flight.ConnectTimeMinutes = segment.ConnectTimeMinutes > 0 ? GetFormattedTravelTime(segment.ConnectTimeMinutes) : string.Empty;

                    flight.DepartDate = FormatDate(segment.DepartDateTime);
                    flight.DepartTime = FormatTime(segment.DepartDateTime);
                    flight.Destination = segment.Destination;
                    flight.DestinationDate = FormatDate(segment.DestinationDateTime);
                    flight.DestinationTime = FormatTime(segment.DestinationDateTime);
                    flight.DepartureDateTime = FormatDateTime(segment.DepartDateTime);
                    flight.DestinationTimezoneOffset = segment.DestTimezoneOffset;
                    flight.OriginTimezoneOffset = segment.OrgTimezoneOffset;

                    if (IsTripPlanSearch(session.TravelType))
                    {
                        flight.DepartureDateFormated = FormatDateTimeTripPlan(segment.DepartDateTime);
                    }
                    flight.ArrivalDateTime = FormatDateTime(segment.DestinationDateTime);

                    string destinationName = string.Empty;
                    if (!string.IsNullOrEmpty(flight.Destination))
                    {
                        destinationName = await GetAirportNameFromSavedList(flight.Destination);
                    }
                    if (string.IsNullOrEmpty(destinationName))
                    {
                        flight.DestinationDescription = segment.DestinationDescription;
                    }
                    else
                    {
                        flight.DestinationDescription = destinationName;
                    }
                    flight.DestinationCountryCode = segment.DestinationCountryCode;
                    flight.OriginCountryCode = segment.OriginCountryCode;
                    if (_configuration.GetValue<bool>("AdvisoryMsgUpdateEnable"))
                    {
                        flight.DestinationStateCode = !string.IsNullOrEmpty(segment.DestinationStateCode) ? segment.DestinationStateCode : string.Empty;
                        flight.OriginStateCode = !string.IsNullOrEmpty(segment.OriginStateCode) ? segment.OriginStateCode : string.Empty;
                    }
                    flight.IsWheelChairFits = segment.IsWheelChairFits;
                    flight.EquipmentDisclosures = GetEquipmentDisclosures(segment.EquipmentDisclosures);
                    await GetAircraftDisclaimer(flight, segment, appID, appVersion, lstMessages, session?.CatalogItems, searchFilters, session?.IsReshopChange ?? false).ConfigureAwait(false);
                    flight.FareBasisCode = segment.FareBasisCode;
                    flight.FlightNumber = segment.FlightNumber;
                    flight.GroundTime = segment.GroundTimeMinutes.ToString();
                    flight.InternationalCity = segment.InternationalCity ?? string.Empty;
                    flight.IsConnection = segment.IsConnection;
                    flight.MarketingCarrier = segment.MarketingCarrier;
                    flight.MarketingCarrierDescription = segment.MarketingCarrierDescription;
                    flight.Miles = segment.MileageActual.ToString();

                    if (_configuration.GetValue<string>("ReturnShopSelectTripOnTimePerformance") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnShopSelectTripOnTimePerformance")))
                    {
                        flight.OnTimePerformance = PopulateOnTimePerformanceSHOP(segment.OnTimePerformanceInfo);
                    }
                    flight.OperatingCarrier = segment.OperatingCarrier;
                    if (_configuration.GetValue<bool>("EnableOperatingCarrierShortForDisclosureText"))
                    {
                        if (
                            (flight.OperatingCarrier != null && flight.OperatingCarrier.ToUpper() != "UA") ||
                            (segment.OperatingCarrierShort != null && (
                                segment.OperatingCarrierShort.ToUpper().Contains("EXPRESS") ||
                                segment.OperatingCarrierShort.ToUpper().Contains("AMTRAK")
                            ))
                        )
                        {
                            flight.OperatingCarrierDescription = !string.IsNullOrEmpty(segment.OperatingCarrierShort) ? segment.OperatingCarrierShort : "";
                        }
                    }
                    else
                    {
                        if (
                            (flight.OperatingCarrier != null && flight.OperatingCarrier.ToUpper() != "UA") ||
                            (segment.OperatingCarrierDescription != null && (
                                segment.OperatingCarrierDescription.ToUpper().Contains("EXPRESS") ||
                                segment.OperatingCarrierDescription.ToUpper().Contains("AMTRAK")
                            ))
                        )
                        {
                            if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && flight.OperatingCarrier.ToUpper() == "XE")
                            {
                                flight.OperatingCarrierDescription = !string.IsNullOrEmpty(segment.OperatingCarrierDescription) ? segment.OperatingCarrierDescription : "";
                            }
                            else
                            {
                                TextInfo ti = ci.TextInfo;
                                flight.OperatingCarrierDescription = !string.IsNullOrEmpty(segment.OperatingCarrierDescription) ? ti.ToTitleCase(segment.OperatingCarrierDescription.ToLower()) : "";
                            }
                        }
                    }
                    if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && flight.OperatingCarrier != null
                        && _configuration.GetValue<string>("SupportedAirlinesFareComparison").ToString().Contains(flight.OperatingCarrier.ToUpper())
                        )
                    {
                        flight.ShowInterimScreen = true;
                        if (additionalItems == null)
                        {
                            additionalItems = new MOBAdditionalItems { AirlineCodes = new List<string>() };
                        }
                        if (additionalItems.AirlineCodes == null)
                        {
                            additionalItems.AirlineCodes = new List<string>();
                        }
                        if (!additionalItems.AirlineCodes.Contains(flight.OperatingCarrier))
                        {
                            additionalItems.AirlineCodes.Add(flight.OperatingCarrier);
                        }
                    }

                    flight.Origin = segment.Origin;

                    string originName = string.Empty;
                    if (!string.IsNullOrEmpty(flight.Origin))
                    {
                        originName = await GetAirportNameFromSavedList(flight.Origin);
                    }
                    if (string.IsNullOrEmpty(originName))
                    {
                        flight.OriginDescription = segment.OriginDescription;
                    }
                    else
                    {
                        flight.OriginDescription = originName;
                    }

                    if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI"))
                    {
                        //with share trip flag OriginDescription=Chicago, IL, US (ORD)
                        flight.OriginDecodedWithCountry = segment.OriginDescription;
                        flight.DestinationDecodedWithCountry = segment.DestinationDescription;
                    }

                    //Warnings
                    if (segment.Warnings != null && segment.Warnings.Count > 0)
                    {
                        foreach (Warning warn in segment.Warnings)
                        {
                            if (warn.Key.Trim().ToUpper() == "OVERNIGHTCONN")
                            {
                                flight.OvernightConnection = string.IsNullOrEmpty(_configuration.GetValue<string>("OvernightConnectionMessage")) ? _configuration.GetValue<string>("OvernightConnectionMessage") : warn.Title;
                            }

                            if (_configuration.GetValue<bool>("EnableChangeOfAirport") && warn != null && !string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == "CHANGE_OF_AIRPORT_SLICE" && !session.IsReshopChange)
                            {
                                flight.AirportChange = !string.IsNullOrEmpty(_configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE")) ? _configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE") : warn.Key;
                            }

                            if (session.IsFSRRedesign)
                            {
                                if (!_shoppingUtility.IsFSRNearByAirportAlertEnabled(appID, appVersion) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("WARNING_NEARBYAIRPORT"))
                                {
                                    continue;
                                }
                                SetSegmentInfoMessages(flight, warn);
                            }
                        }
                    }
                    flight.StopDestination = segment.StopDestination;
                    bool changeOfGauge = false;
                    if (segment.StopInfos != null && segment.StopInfos.Count > 0)
                    {
                        flight.StopInfos = new List<Model.Shopping.MOBSHOPFlight>();
                        flight.ShowSeatMap = true;
                        bool isFlightDestionUpdated = false;
                        int travelMinutes = segment.TravelMinutes;

                        foreach (Flight stop in segment.StopInfos)
                        {
                            if (segment.EquipmentDisclosures != null && !string.IsNullOrEmpty(segment.EquipmentDisclosures.EquipmentType) && stop.EquipmentDisclosures != null && !string.IsNullOrEmpty(stop.EquipmentDisclosures.EquipmentType))
                            {
                                if (segment.EquipmentDisclosures.EquipmentType.Trim() == stop.EquipmentDisclosures.EquipmentType.Trim())
                                {
                                    flight.ChangeOfGauge = true;
                                    flight.ChangeOfPlane = segment.ChangeOfPlane;
                                    flight.IsThroughFlight = !segment.ChangeOfPlane;
                                    List<Flight> stops = new List<Flight>();
                                    stops.Add(stop);
                                    if (_mOBSHOPDataCarrier == null)
                                        _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                                    List<Model.Shopping.MOBSHOPFlight> stopFlights = await GetFlightsAsync(_mOBSHOPDataCarrier, sessionId, cartId, stops, requestedCabin, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR, trip, appVersion, appID, session, additionalItems, lstMessages, searchFilters);
                                    foreach (Model.Shopping.MOBSHOPFlight sf in stopFlights)
                                    {
                                        sf.ChangeOfGauge = true;
                                        if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes"))
                                        {
                                            sf.IsThroughFlight = !stop.ChangeOfPlane;
                                            sf.ChangeOfPlane = stop.ChangeOfPlane;
                                            sf.GroundTime = GetFormattedGroundTime(sf.GroundTime, sf.Origin, segment.Warnings, stop.ChangeOfPlane);
                                        }
                                    }

                                    ///245704 - PROD: Mapp - Incorrect segments info is displayed for a 2-Stop flight UA1666 on choose your travel experience screen
                                    ///Srini - 03/20/2018
                                    if (_configuration.GetValue<bool>("BugFixToggleFor18C"))
                                    {
                                        flight.Destination = stop.Origin;
                                        if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes") && !isFlightDestionUpdated)
                                        {
                                            var isConnectThroughFlightFix = _configuration.GetValue<bool>("OmnicartConnectionThroughFlightFix");
                                            flight.Destination = isConnectThroughFlightFix ? segment.StopInfos.Last()?.Destination : stop.Origin;
                                            string destination = string.Empty;
                                            if (!string.IsNullOrEmpty(flight.Destination))
                                            {
                                                destination = await GetAirportNameFromSavedList(flight.Destination);
                                            }
                                            if (string.IsNullOrEmpty(destination))
                                            {
                                                flight.DestinationDescription = isConnectThroughFlightFix ? segment.StopInfos.Last()?.DestinationDescription : stop.OriginDescription;
                                            }
                                            else
                                            {
                                                flight.DestinationDescription = destination;
                                            }
                                            flight.DestinationDecodedWithCountry = isConnectThroughFlightFix ? segment.StopInfos.Last()?.DestinationDescription : stop.OriginDescription;
                                            flight.DestinationStateCode = isConnectThroughFlightFix ? segment.StopInfos.Last()?.DestinationStateCode : stop.OriginStateCode;
                                            flight.DestinationCountryCode = isConnectThroughFlightFix ? segment.StopInfos.Last()?.DestinationCountryCode : stop.OriginCountryCode;
                                        }
                                        isFlightDestionUpdated = true;
                                    }

                                    flight.StopInfos.AddRange(stopFlights);
                                }
                                else
                                {
                                    changeOfGauge = true;
                                    ///245704 - PROD: Mapp - Incorrect segments info is displayed for a 2-Stop flight UA1666 on choose your travel experience screen
                                    ///Srini - 03/20/2018
                                    if (!_configuration.GetValue<bool>("BugFixToggleFor18C") || (_configuration.GetValue<bool>("BugFixToggleFor18C") && !isFlightDestionUpdated))
                                    {
                                        flight.Destination = stop.Origin;
                                        isFlightDestionUpdated = true;
                                    }

                                    string destination = string.Empty;
                                    if (!string.IsNullOrEmpty(flight.Destination))
                                    {
                                        destination = await GetAirportNameFromSavedList(flight.Destination);
                                    }
                                    if (string.IsNullOrEmpty(destination))
                                    {
                                        flight.DestinationDescription = stop.OriginDescription;
                                    }
                                    else
                                    {
                                        flight.DestinationDescription = destination;
                                    }

                                    flight.DestinationDate = FormatDate(Convert.ToDateTime(stop.DepartDateTime).AddMinutes(-stop.GroundTimeMinutes).ToString());
                                    flight.DestinationTime = FormatTime(Convert.ToDateTime(stop.DepartDateTime).AddMinutes(-stop.GroundTimeMinutes).ToString());
                                    flight.ArrivalDateTime = FormatDateTime(Convert.ToDateTime(stop.DepartDateTime).AddMinutes(-stop.GroundTimeMinutes).ToString());
                                    flight.TravelTime = segment.TravelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes > 0 ? GetFormattedTravelTime(segment.TravelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes) : string.Empty;
                                    flight.ChangeOfPlane = segment.ChangeOfPlane;

                                    Model.Shopping.MOBSHOPFlight stopFlight = new Model.Shopping.MOBSHOPFlight();

                                    stopFlight.EquipmentDisclosures = GetEquipmentDisclosures(stop.EquipmentDisclosures);
                                    await GetAircraftDisclaimer(flight, segment, appID, appVersion, lstMessages, session?.CatalogItems, searchFilters, session?.IsReshopChange ?? false).ConfigureAwait(false);
                                    stopFlight.FlightNumber = stop.FlightNumber;
                                    stopFlight.ChangeOfGauge = stop.ChangeOfPlane;
                                    stopFlight.ShowSeatMap = true;
                                    stopFlight.DepartDate = FormatDate(stop.DepartDateTime);
                                    stopFlight.DepartTime = FormatTime(stop.DepartDateTime);
                                    stopFlight.Origin = stop.Origin;
                                    stopFlight.Destination = stop.Destination;
                                    stopFlight.DestinationDate = FormatDate(stop.DestinationDateTime);
                                    stopFlight.DestinationTime = FormatTime(stop.DestinationDateTime);
                                    stopFlight.DepartureDateTime = FormatDateTime(stop.DepartDateTime);
                                    stopFlight.ArrivalDateTime = FormatDateTime(stop.DestinationDateTime);
                                    stopFlight.IsCovidTestFlight = stop.IsCovidTestingRequired;
                                    stopFlight.GroundTime = stop.GroundTimeMinutes > 0 ? GetFormattedTravelTime(stop.GroundTimeMinutes) : String.Empty;
                                    stopFlight.ChangeOfPlane = stop.ChangeOfPlane;
                                    stopFlight.OriginTimezoneOffset = stop.OrgTimezoneOffset;
                                    stopFlight.DestinationTimezoneOffset = stop.DestTimezoneOffset;
                                    if (!_configuration.GetValue<bool>("DisableOperatingCarrierFlightNumberChanges"))
                                    {
                                        stopFlight.OperatingCarrierFlightNumber = stop.OriginalFlightNumber;
                                    }

                                    ///57783 - BUG 390826 CSL:  Class of service information is not included for certain segments on Mobile
                                    ///Srini - 02/20/2018
                                    if (_configuration.GetValue<bool>("BugFixToggleFor18B") && displayProductForStopInfo != null)
                                    {
                                        stopFlight.Meal = !string.IsNullOrEmpty(displayProductForStopInfo.MealDescription) ? displayProductForStopInfo.MealDescription : "None";
                                        stopFlight.ServiceClass = displayProductForStopInfo.BookingCode;

                                        bool isCOGReshopCabinFixEnabled = await _featureSettings.GetFeatureSettingValue("EnableCOGReshopCabinFix");

                                        //COG SeatMap Fix
                                        if (_configuration.GetValue<bool>("COGBookingFix") || isCOGReshopCabinFixEnabled)
                                        {
                                            if (stop.Products != null && stop.Products.Count > 0/* && segment.Products[0] != null && segment.Products[0].Prices != null && segment.Products[0].Prices.Count > 0*/)
                                            {
                                                bool selected;
                                                int seatsRemaining = 0;
                                                bool mixedCabin;
                                                string description = string.Empty;

                                                AssignCorporateFareIndicator(stop, stopFlight, session.TravelType);

                                                United.Services.FlightShopping.Common.Product displayProduct = GetMatrixDisplayProduct(stop.Products, requestedCabin, columnInfo, out ci, out selected, out description, out mixedCabin, out seatsRemaining, fareClass, isConnection, isELFFareDisplayAtFSR);
                                                stopFlight.ServiceClassDescription = description;
                                            }
                                        }

                                        if (isCOGReshopCabinFixEnabled)
                                        {
                                            stopFlight.Cabin = GetCabinDescription(stopFlight.ServiceClassDescription);
                                        }

                                        stopFlight.Messages = new List<MOBSHOPMessage>();

                                        MOBSHOPMessage msg = new MOBSHOPMessage();
                                        msg.FlightNumberField = flight.FlightNumber;
                                        msg.TripId = flight.TripId;
                                        msg.MessageCode = displayProductForStopInfo.Description + " (" + displayProductForStopInfo.BookingCode + ")";
                                        stopFlight.Messages.Add(msg);

                                        msg = new MOBSHOPMessage();
                                        msg.FlightNumberField = flight.FlightNumber;
                                        msg.TripId = flight.TripId;
                                        msg.MessageCode = !string.IsNullOrEmpty(displayProductForStopInfo.MealDescription) ? displayProductForStopInfo.MealDescription : "None";
                                        stopFlight.Messages.Add(msg);

                                        flight.MatchServiceClassRequested = selectedForStopInfo;
                                    }
                                    if (session.IsFSRRedesign)
                                    {
                                        if (stopFlight.Messages != null && stopFlight.Messages.Count > 0)
                                        {
                                            if (stopFlight.ShoppingProducts != null && stopFlight.ShoppingProducts.Count > 0 && stopFlight.ShoppingProducts.Any(p => p.IsMixedCabin))
                                            {
                                                var mixedCabinText = _configuration.GetValue<string>("MixedCabinProductBadgeText");
                                                var firstMessage = stopFlight.Messages.First().MessageCode;
                                                var mixedCabinColorCode = _configuration.GetValue<string>("MixedCabinTextColorCode");
                                                if (await _featureSettings.GetFeatureSettingValue("EnableNewBackGroundColor").ConfigureAwait(false) && GeneralHelper.IsApplicationVersionGreaterorEqual(appID, appVersion, _configuration.GetValue<string>("Android_Atmos2_New_MixedCabinTextColorCode_AppVersion"), _configuration.GetValue<string>("Iphone_Atmos2_New_MixedCabinTextColorCode_AppVersion")))
                                                {
                                                    mixedCabinColorCode = _configuration.GetValue<string>("Atmos2_New_MixedCabinTextColorCode");
                                                }
                                                var message1 = string.Format("<span style='color:{0}'>{1} {2}</span>", mixedCabinColorCode, mixedCabinText, firstMessage);
                                                var message2 = stopFlight.Messages.Last().MessageCode;
                                                stopFlight.LineOfFlightMessage = string.Join(" / ", message1, message2);
                                            }
                                            else
                                            {
                                                stopFlight.LineOfFlightMessage = string.Join(" / ", stopFlight.Messages.Select(x => x.MessageCode));
                                            }
                                        }
                                    }
                                    //Added Carrier code for the bug 218201 by Niveditha.Didn't add Marketing Carrier description as per suggestion by Jada sreenivas.
                                    stopFlight.MarketingCarrier = flight.MarketingCarrier;
                                    stopFlight.OperatingCarrier = flight.OperatingCarrier;

                                    string stopDestination = string.Empty;
                                    if (!string.IsNullOrEmpty(stopFlight.Destination))
                                    {
                                        stopDestination = await GetAirportNameFromSavedList(stopFlight.Destination);
                                    }
                                    if (string.IsNullOrEmpty(stopDestination))
                                    {
                                        stopFlight.DestinationDescription = stop.DestinationDescription;
                                    }
                                    else
                                    {
                                        stopFlight.DestinationDescription = stopDestination;
                                    }

                                    string stopOrigin = string.Empty;
                                    if (!string.IsNullOrEmpty(stopFlight.Origin))
                                    {
                                        stopOrigin = await GetAirportNameFromSavedList(stopFlight.Origin);
                                    }
                                    if (string.IsNullOrEmpty(stopOrigin))
                                    {
                                        stopFlight.OriginDescription = stop.OriginDescription;
                                    }
                                    else
                                    {
                                        stopFlight.OriginDescription = stopOrigin;
                                    }

                                    if (_configuration.GetValue<bool>("EnableShareTripInSoftRTI"))
                                    {
                                        //with share trip flag OriginDescription=Chicago, IL, US (ORD)
                                        stopFlight.OriginDecodedWithCountry = stop.OriginDescription;
                                        stopFlight.DestinationDecodedWithCountry = stop.DestinationDescription;
                                    }

                                    stopFlight.TravelTime = stop.TravelMinutes > 0 ? GetFormattedTravelTime(stop.TravelMinutes) : string.Empty;

                                    if (session.IsFSRRedesign)
                                    {
                                        if (stop.Warnings != null && stop.Warnings.Count > 0)
                                        {
                                            foreach (Warning warn in stop.Warnings)
                                            {
                                                if (!_shoppingUtility.IsFSRNearByAirportAlertEnabled(appID, appVersion) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("WARNING_NEARBYAIRPORT"))
                                                {
                                                    continue;
                                                }
                                                SetSegmentInfoMessages(stopFlight, warn);
                                            }
                                        }
                                    }

                                    flight.StopInfos.Add(stopFlight);
                                }
                            }
                            if (_configuration.GetValue<bool>("DoubleDisclosureFix"))
                            {
                                travelMinutes = travelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes > 0 ? travelMinutes - stop.TravelMinutes - stop.GroundTimeMinutes : 0;
                            }
                        }
                        if (_configuration.GetValue<bool>("DoubleDisclosureFix"))
                        {
                            flight.Destination = segment.StopInfos[0].Origin;
                            flight.DestinationDate = FormatDate(Convert.ToDateTime(segment.StopInfos[0].DepartDateTime).AddMinutes(-segment.StopInfos[0].GroundTimeMinutes).ToString());
                            flight.DestinationTime = FormatTime(Convert.ToDateTime(segment.StopInfos[0].DepartDateTime).AddMinutes(-segment.StopInfos[0].GroundTimeMinutes).ToString());
                            flight.TravelTime = travelMinutes > 0 ? GetFormattedTravelTime(travelMinutes) : string.Empty;
                        }
                        if (_configuration.GetValue<bool>("OmnicartConnectionThroughFlightFix") && flight.IsThroughFlight)
                        {
                            flight.TravelTime = segment.TravelMinutes > 0 ? GetFormattedTravelTime(segment.TravelMinutes) : string.Empty;
                        }
                    }

                    flight.Stops = segment.StopInfos != null ? segment.StopInfos.Count : 0;

                    if (_configuration.GetValue<bool>("DoubleDisclosureFix"))
                    {
                        if (!changeOfGauge && string.IsNullOrEmpty(flight.TravelTime))
                            flight.TravelTime = segment.TravelMinutes > 0 ? GetFormattedTravelTime(segment.TravelMinutes) : string.Empty;
                    }
                    else
                    {
                        if (!changeOfGauge)
                        {
                            flight.TravelTime = segment.TravelMinutes > 0 ? GetFormattedTravelTime(segment.TravelMinutes) : string.Empty;
                        }
                    }
                    flight.TotalTravelTime = segment.TravelMinutesTotal > 0 ? GetFormattedTravelTime(segment.TravelMinutesTotal) : string.Empty;
                    flight.TravelTimeInMinutes = segment.TravelMinutes;
                    flight.TripId = segment.BBXSolutionSetId;

                    if (_mOBSHOPDataCarrier == null)
                        _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                    var tupleResponse = await PopulateProducts(_mOBSHOPDataCarrier, segment.Products, sessionId, flight, requestedCabin, segment, lowestFare, columnInfo, premierStatusLevel, fareClass, isELFFareDisplayAtFSR, appVersion, session, additionalItems);
                    flight.ShoppingProducts = tupleResponse.Item1;
                    flight = tupleResponse.flight;
                    SetAutoFocusIfMissed(session, isELFFareDisplayAtFSR, flight.ShoppingProducts, bestProductType);
                    if (await _featureSettings.GetFeatureSettingValue("EnableAwardStrikeThroughPriceEnhancement").ConfigureAwait(false) && additionalItems != null)
                    {
                        flight.StrikeThroughDisplayType = additionalItems?.StrikeThroughDisplayType;
                        additionalItems.StrikeThroughDisplayType = "";
                    }

                    #endregion
                    if (isConnection)
                    {
                        flights.Add(flight);
                    }
                    else
                    {
                        if (flight.ShoppingProducts != null && flight.ShoppingProducts.Count > 0)
                            flights.Add(flight);
                        #region REST SHOP and Select Trip Tuning Changes - Venkat Apirl 20, 2015
                        if (_configuration.GetValue<bool>("HandlePagingAtRESTSide") != null && Convert.ToBoolean(_configuration.GetValue<string>("HandlePagingAtRESTSide")) && flights.Count == _configuration.GetValue<int>("OrgarnizeResultsRequestPageSize"))
                        {
                            break;
                        }
                        #endregion
                    }
                }
            }
            // _logger.LogWarning("GetFlightsAsync end {number}", localcountDisplay);
            return flights;
        }

        private async Task GetAircraftDisclaimer(MOBSHOPFlight flight, Flight segment, int appID=0, string appVersion="", List<CMSContentMessage> lstMessages = null, List<MOBItem> catalogItems = null, MOBSearchFilters searchFilters = null, bool isReshop = false)
        {
            if (await _shoppingUtility.EnableBookingWheelchairDisclaimer(appID,appVersion).ConfigureAwait(false)
                && !string.IsNullOrWhiteSpace(segment.EquipmentDisclosures.AircraftDoorWidth)
                && !string.IsNullOrWhiteSpace(segment.EquipmentDisclosures.AircraftDoorHeight) && lstMessages != null)
            {

                var sdlMessage = GetSDLStringMessageFromList(lstMessages, "WheelchairDisclaimer");
                if (!string.IsNullOrEmpty(sdlMessage))
                {
                    flight.FlightMessages = new List<MOBItemWithIconName>();
                    MOBItemWithIconName flightMessage = new MOBItemWithIconName
                    {
                       OptionDescription= string.Format(sdlMessage, segment.EquipmentDisclosures.AircraftDoorWidth, segment.EquipmentDisclosures.AircraftDoorHeight),
                    };
                    flight.FlightMessages.Add(flightMessage);

                }
            }
            try
            {
                if (await _featureToggles.IsEnableWheelchairFilterOnFSR(appID, appVersion, catalogItems).ConfigureAwait(false) || (isReshop && await _featureToggles.IsEnableReshopWheelchairFilterOnFSR(appID, appVersion, catalogItems).ConfigureAwait(false)))
                {
                    if (searchFilters != null && searchFilters.WheelchairFilterContent != null
                        && searchFilters.WheelchairFilterContent.DimensionInfo != null
                        && !string.IsNullOrEmpty(searchFilters.WheelchairFilterContent.SelectedBatteryType))
                    {
                        var unitedCarriers = _configuration.GetValue<string>("UnitedCarriers");
                        if (unitedCarriers != null && !unitedCarriers.Contains(segment.OperatingCarrier?.Trim().ToUpper()))
                        {
                            var sdlMessage = GetSDLStringMessageFromList(lstMessages, "WheelchairFilter_FSR_Disclaimer_PartnerAirlines");
                            if (!string.IsNullOrEmpty(sdlMessage))
                            {
                                //if (flight.FlightMessages == null)
                                flight.FlightMessages = new List<MOBItemWithIconName>();
                                MOBItemWithIconName flightDiscliamerMessage = new MOBItemWithIconName
                                {
                                    OptionDescription = sdlMessage,
                                };
                                flight.FlightMessages.Add(flightDiscliamerMessage);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.ILoggerError("WheelChairFilter-Disclaimer message setting error-{@Exception}", JsonConvert.SerializeObject(ex));
            }
        }

        private string GetFormattedGroundTime(string groundTime, string origin, List<Warning> warnings, bool isChangeOfPlane)
        {
            if (int.TryParse(groundTime, out int minutes))
            {
                if (minutes == 0)
                    return string.Empty;

                if (string.IsNullOrEmpty(origin))
                    return GetFormattedTravelTime(minutes);

                if (isChangeOfPlane)
                    return GetFormattedTravelTime(minutes);

                var groundTimeWarningEntry = warnings?.FirstOrDefault(w => string.Equals(w.Key, "stop", StringComparison.OrdinalIgnoreCase))
                                                                                               ?.Stops?.FirstOrDefault(s => string.Equals(s.AdvisoryAirportCode, origin, StringComparison.OrdinalIgnoreCase));
                if (groundTimeWarningEntry == null)
                    return GetFormattedTravelTime(minutes);

                if (string.IsNullOrWhiteSpace(groundTimeWarningEntry.Duration))
                    return GetFormattedTravelTime(minutes);

                return groundTimeWarningEntry.Duration;
            }
            else
                return groundTime;
        }

        private string FormatDateTimeTripPlan(string dateTimeString)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(0);
            DateTime.TryParse(dateTimeString, out dateTime);
            result = string.Format("{0:ddd, MMM dd}", dateTime);

            return result;
        }
        private void GetBestProductTypeTripPlanner(Session session, United.Services.FlightShopping.Common.Product displayProduct, bool isSelected, ref string bestProductType)
        {
            if (IsTripPlanSearch(session.TravelType))
            {
                if (string.IsNullOrEmpty(bestProductType) && !isSelected)
                {
                    bestProductType = displayProduct?.ProductType;
                }
            }
        }
        private PricingItem ReshopAwardPrice(List<PricingItem> price)
        {
            if (price.Exists(p => p.PricingType == "Award"))
                return price.FirstOrDefault(p => p.PricingType == "Award");

            return null;
        }

        private async Task<(List<Model.Shopping.MOBSHOPShoppingProduct>, Model.Shopping.MOBSHOPFlight flight)> PopulateProducts(MOBSHOPDataCarrier _mOBSHOPDataCarrier, ProductCollection products, string sessionId, Model.Shopping.MOBSHOPFlight flight, string cabin, Flight segment, decimal lowestAirfare, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClas, bool isELFFareDisplayAtFSR = true, string appVersion = "", Session session = null, MOBAdditionalItems additionalItems = null, string corporateFareIndicator = "", string yaDiscount = "")
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            //Session session = new Session();
            //session = _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName });
            supressLMX = session.SupressLMXForAppID;
            #endregion
            if (_mOBSHOPDataCarrier == null)
                _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
            return (await PopulateProductsAsync(_mOBSHOPDataCarrier, products, cabin, segment, lowestAirfare, columnInfo, premierStatusLevel, fareClas, supressLMX, session, isELFFareDisplayAtFSR, appVersion, additionalItems, flight.CorporateFareIndicator, flight.YaDiscount), flight); ;
        }

        private async Task<List<MOBSHOPShoppingProduct>> PopulateProductsAsync(MOBSHOPDataCarrier _mOBSHOPDataCarrier, ProductCollection products, string cabin, Flight segment, decimal lowestAirfare,
          List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClas, bool supressLMX, Session session, bool isELFFareDisplayAtFSR = true, string appVersion = "", MOBAdditionalItems additionalItems = null, string corporateFareIndicator = "", string yaDiscount = "")
        {
            var shopProds = new List<Model.Shopping.MOBSHOPShoppingProduct>();
            if (_mOBSHOPDataCarrier == null)
                _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
            CultureInfo ci = null;

            var foundCabinSelected = false;
            var foundFirstAward = false;
            var foundBusinessAward = false;
            var foundEconomyAward = false;

            var fareClass = fareClas;

            var productIndex = -1;
            try
            {
                foreach (var prod in products)
                {
                    var isUaDiscount = !string.IsNullOrEmpty(prod.PromoDescription) &&
                                        prod.PromoDescription.Trim().ToUpper().Equals("EMPLOYEE FARE");
                    productIndex = productIndex + 1;

                    if ((prod.Prices != null && prod.Prices.Count > 0) &&
                        ((session.IsReshopChange && prod.ProductId != "NAP") || !session.IsReshopChange))
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(prod.Prices[0].Currency);
                        }
                        var newProdTuple = await TransformProductWithPriceToNewProduct(cabin, segment, lowestAirfare, columnInfo,
                            premierStatusLevel, isUaDiscount, prod, supressLMX, ci, fareClass, productIndex, session, isELFFareDisplayAtFSR, appVersion, additionalItems, corporateFareIndicator, yaDiscount).ConfigureAwait(false);
                        var newProd = newProdTuple?.Item1;
                        foundEconomyAward = newProdTuple.Item2;
                        foundBusinessAward = newProdTuple.Item3;
                        foundFirstAward = newProdTuple.Item4;
                        if (_shoppingUtility.EnableIBELite() && !string.IsNullOrWhiteSpace(prod.ProductCode))
                        {
                            newProd.IsIBELite = _configuration.GetValue<string>("IBELiteShoppingProductCodes").IndexOf(prod.ProductCode.Trim().ToUpper()) > -1;

                            if (newProd.IsIBELite) // per clients' request when implementing IBELite
                                newProd.ShortProductName = _configuration.GetValue<string>("IBELiteShortProductName");
                        }

                        if (_shoppingUtility.EnableIBEFull() && !string.IsNullOrWhiteSpace(prod.ProductCode))
                        {
                            newProd.IsIBE = _configuration.GetValue<string>("IBEFULLShoppingProductCodes").IndexOf(prod.ProductCode.Trim().ToUpper()) > -1;

                            if (newProd.IsIBE)
                                newProd.ShortProductName = _configuration.GetValue<string>("IBEFULLShortProductName");
                        }
                        if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == MOBTripPlannerType.TPSearch.ToString() || session.TravelType == MOBTripPlannerType.TPEdit.ToString())
                        {
                            newProd.PriceApplyLabelText = GetPriceApplyLabelText(_mOBSHOPDataCarrier.SearchType);
                        }
                        shopProds.Add(newProd);
                    }
                    else
                    {
                        var newProd = await TransformProductWithoutPriceToNewProduct(cabin, columnInfo, isUaDiscount, prod,
                            foundEconomyAward, foundBusinessAward, foundFirstAward, session);

                        shopProds.Add(newProd);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("PopulateProductsAsync exception {@ErrorMessage} {@StackTrace}", ex.Message, ex.StackTrace);
            }
            HighlightProductMatchingSelectedCabin(shopProds, cabin, columnInfo, fareClass, (session == null ? false : session.IsReshopChange), isELFFareDisplayAtFSR);

            //loop thru if award to finalize the award pricing blocks to all cabin option
            int econAwardCount = 0;
            int busAwardCount = 0;
            int firstAwardCount = 0;
            CalculateAwardCounts(shopProds, ref econAwardCount, ref busAwardCount, ref firstAwardCount);

            if (econAwardCount > 1 || busAwardCount > 1 || firstAwardCount > 1)
            {
                ClearMileageButtonAndAllCabinButtonText(shopProds, econAwardCount, busAwardCount, firstAwardCount);
            }

            if (shopProds != null && shopProds.Count > 0)
            {
                foreach (var shopProd in shopProds)
                {
                    //MOBProductSettings configurationProductSettings = _configuration.GetSection("productSettings").Get<MOBProductSettings>();

                    SetShortCabin(shopProd, columnInfo, configurationProductSettings);

                    if (string.IsNullOrEmpty(shopProd.Description))
                    {
                        SetShoppingProductDescriptionBasedOnProductElementDescription(shopProd, columnInfo, configurationProductSettings);
                    }
                    else
                    {
                        if (shopProd.LongCabin.Equals("Economy (lowest)") && !string.IsNullOrEmpty(shopProd.AwardType))
                        {
                            shopProd.Description = string.Empty;
                        }
                    }
                    if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && segment.OperatingCarrier != null
                          && _configuration.GetValue<string>("SupportedAirlinesFareComparison").ToString().Contains(segment.OperatingCarrier.ToUpper())
                          )
                    {
                        shopProd.InterimScreenCode = segment.OperatingCarrier.ToUpper() + "_" + shopProd.Type;
                    }

                }
            }

            #region awardType=saver

            List<Model.Shopping.MOBSHOPShoppingProduct> updatedShopProducts = new List<Model.Shopping.MOBSHOPShoppingProduct>();
            foreach (Model.Shopping.MOBSHOPShoppingProduct mobShopProduct in shopProds)
            {
                SetIsPremierCabinSaverIfApplicable(mobShopProduct);
                updatedShopProducts.Add(mobShopProduct);
                if (_configuration.GetValue<bool>("Shopping - bPricingBySlice"))
                {
                    if (_mOBSHOPDataCarrier != null)
                        mobShopProduct.PriceFromText = _mOBSHOPDataCarrier.PriceFormText;// SetProductPriceFromText();
                }

            }

            #endregion

            return updatedShopProducts;
        }

        private void SetShoppingProductDescriptionBasedOnProductElementDescription(Model.Shopping.MOBSHOPShoppingProduct shopProd, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, MOBProductSettings configurationProductSettings)
        {
            if (configurationProductSettings != null && configurationProductSettings.Products != null && configurationProductSettings.Products.Count > 0 &&
                 columnInfo != null && columnInfo.Count > 0)
            {
                foreach (MOBProductElements productElement in configurationProductSettings.Products)
                {
                    if (productElement.Description.Equals(shopProd.LongCabin))
                    {
                        shopProd.Description = productElement.Header;
                        break;
                    }
                }
            }
        }

        private void SetShortCabin(MOBSHOPShoppingProduct shopProd, List<MOBSHOPShoppingProduct> columnInfo, MOBProductSettings configurationProductSettings)
        {
            foreach (MOBProductElements configElement in configurationProductSettings.Products)
            {
                if (shopProd.Type == configElement.Key)
                {
                    if (!string.IsNullOrEmpty(configElement.ShouldShowShortCabinName))
                    {
                        if (columnInfo != null &&
                        columnInfo.First(column => column.Type == shopProd.Type) != null)
                        {
                            shopProd.Cabin = columnInfo.First(column => column.Type == shopProd.Type).LongCabin;
                        }
                        else
                        {
                            shopProd.Cabin = shopProd.LongCabin;
                        }
                    }

                }
            }
        }

        private void ClearMileageButtonAndAllCabinButtonText(List<Model.Shopping.MOBSHOPShoppingProduct> shopProds, int econAwardCount, int busAwardCount, int firstAwardCount)
        {
            int econClassCount = 0;
            int busClassCount = 0;
            int firstClassCount = 0;

            int econIdx = -1;
            int busIdx = -1;
            int firstIdx = -1;
            int econIdx2 = -1;
            int busIdx2 = -1;
            int firstIdx2 = -1;

            for (int k = 0; k < shopProds.Count; k++)
            {
                if (econAwardCount > 1)
                {
                    if (shopProds[k].LongCabin.Trim().ToUpper().Contains("ECONOMY"))
                    {
                        if (econIdx < 0)
                            econIdx = k;
                        else
                            econIdx2 = k;

                        econClassCount++;
                        if (econClassCount > 1 && econIdx2 >= 0 && shopProds[econIdx].MilesDisplayAmount > 0)
                        {
                            shopProds[econIdx2].MileageButton = -1;
                            shopProds[econIdx2].AllCabinButtonText = "";
                        }
                        else if (econClassCount > 1 && econIdx2 >= 0 && shopProds[econIdx2].MilesDisplayAmount > 0)
                        {
                            shopProds[econIdx].MileageButton = -1;
                            shopProds[econIdx].AllCabinButtonText = "";
                        }
                        else if (econClassCount > 1 && econIdx2 >= 0)
                        {
                            shopProds[econIdx2].MileageButton = -1;
                            shopProds[econIdx2].AllCabinButtonText = "";
                        }
                    }
                }
                else if (busAwardCount > 1)
                {
                    if (shopProds[k].LongCabin.Trim().ToUpper().Contains("BUS"))
                    {
                        if (busIdx < 0)
                            busIdx = k;
                        else
                            busIdx2 = k;

                        busClassCount++;
                        if (busClassCount > 1 && busIdx2 >= 0 && shopProds[busIdx].MilesDisplayAmount > 0)
                        {
                            shopProds[busIdx2].MileageButton = -1;
                            shopProds[busIdx2].AllCabinButtonText = "";
                        }
                        else if (busClassCount > 1 && busIdx2 >= 0 && shopProds[busIdx2].MilesDisplayAmount > 0)
                        {
                            shopProds[busIdx].MileageButton = -1;
                            shopProds[busIdx].AllCabinButtonText = "";
                        }
                        else if (busClassCount > 1 && busIdx2 >= 0)
                        {
                            shopProds[busIdx2].MileageButton = -1;
                            shopProds[busIdx2].AllCabinButtonText = "";
                        }
                    }
                }
                else if (firstAwardCount > 1)
                {
                    if (shopProds[k].LongCabin.Trim().ToUpper().Contains("FIRST"))
                    {
                        if (firstIdx < 0)
                            firstIdx = k;
                        else
                            firstIdx2 = k;

                        firstClassCount++;
                        if (firstClassCount > 1 && firstIdx2 >= 0 && shopProds[firstIdx].MilesDisplayAmount > 0)
                        {
                            shopProds[firstIdx2].MileageButton = -1;
                            shopProds[firstIdx2].AllCabinButtonText = "";
                        }
                        else if (firstClassCount > 1 && firstIdx2 >= 0 && shopProds[firstIdx2].MilesDisplayAmount > 0)
                        {
                            shopProds[firstIdx].MileageButton = -1;
                            shopProds[firstIdx].AllCabinButtonText = "";
                        }
                        else if (firstClassCount > 1 && firstIdx2 >= 0)
                        {
                            shopProds[firstIdx2].MileageButton = -1;
                            shopProds[firstIdx2].AllCabinButtonText = "";
                        }
                    }
                }
            }
        }

        private void CalculateAwardCounts(List<Model.Shopping.MOBSHOPShoppingProduct> shopProds, ref int econAwardCount, ref int busAwardCount, ref int firstAwardCount)
        {
            foreach (Model.Shopping.MOBSHOPShoppingProduct prod in shopProds)
            {
                if (prod.MileageButton > -1)
                {
                    if (prod.LongCabin.Trim().ToUpper().Contains("ECONOMY"))
                    {
                        econAwardCount++;
                    }
                    else if (prod.LongCabin.Trim().ToUpper().Contains("BUS"))
                    {
                        busAwardCount++;
                    }
                    else if (prod.LongCabin.Trim().ToUpper().Contains("FIRST"))
                    {
                        firstAwardCount++;
                    }
                }
            }
        }

        private void SetAutoFocusIfMissed(Session session, bool isELFFareDisplayAtFSR, List<Model.Shopping.MOBSHOPShoppingProduct> shopProds, string productTypeBestMatched)
        {
            if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == MOBTripPlannerType.TPSearch.ToString() ||
                      session.TravelType == MOBTripPlannerType.TPEdit.ToString())
            {
                if (!shopProds.Any(p => p.IsSelectedCabin))
                {
                    if (!string.IsNullOrEmpty(productTypeBestMatched) && shopProds.Any(p => p.Type == productTypeBestMatched))
                    {
                        shopProds.First(p => p.Type == productTypeBestMatched).IsSelectedCabin = true;
                    }
                    else
                    {
                        var priorityProduct = shopProds.FirstOrDefault(p => (!isELFFareDisplayAtFSR) ? p.Type.ToUpper() != "ECO-BASIC" : true && p.PriceAmount > 0);

                        if (priorityProduct != null)
                            priorityProduct.IsSelectedCabin = true;
                    }
                }
            }
        }

        private void HighlightProductMatchingSelectedCabin(List<Model.Shopping.MOBSHOPShoppingProduct> shopProds, string requestedCabin, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, string fareClass, bool isReshopChange, bool isELFFareDisplayAtFSR = true)
        {
            IOrderedEnumerable<Model.Shopping.MOBSHOPShoppingProduct> productsSortedByPrice = null;
            if (isReshopChange)
                productsSortedByPrice = shopProds.Where(p => p.PriceAmount >= 0 && !string.IsNullOrEmpty(p.ProductId)).OrderBy(p => p.PriceAmount);
            else
                productsSortedByPrice = shopProds.Where(p => p.PriceAmount > 0).OrderBy(p => p.PriceAmount);

            foreach (var product in productsSortedByPrice)
            {
                var productMatchesClassRequested = MatchServiceClassRequested(requestedCabin, fareClass, product.Type, columnInfo, isELFFareDisplayAtFSR);
                if (productMatchesClassRequested)
                {
                    product.IsSelectedCabin = true;
                    break;
                }

            }
        }

        public static bool MatchServiceClassRequested(string requestedCabin, string fareClass, string prodType, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, bool isELFFareDisplayAtFSR = true)
        {
            var match = false;
            if (!string.IsNullOrEmpty(requestedCabin))
            {
                requestedCabin = requestedCabin.Trim().ToUpper();
            }

            if (!string.IsNullOrEmpty(fareClass))
            {
                fareClass = fareClass.Trim().ToUpper();
            }

            if (!string.IsNullOrEmpty(fareClass) && prodType.ToUpper().Contains("SPECIFIED"))
            {
                match = true;
            }
            else
            {
                if (string.IsNullOrEmpty(fareClass))
                {
                    switch (requestedCabin)
                    {
                        case "ECON":
                        case "ECONOMY":
                            //Removed FLEXIBLE & UNRESTRICTED as it is not taking ECO-FLEXIBLE as selected when Economy is not available.
                            match = (prodType.ToUpper().Contains("ECON") || (isELFFareDisplayAtFSR && prodType.ToUpper().Contains("ECO-BASIC")) || prodType.ToUpper().Contains("ECO-PREMIUM")) && !prodType.ToUpper().Contains("FLEXIBLE") && !prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "ECONOMY-FLEXIBLE":
                            match = (prodType.ToUpper().Contains("ECON") || prodType.ToUpper().Contains("ECO-PREMIUM")) && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "ECONOMY-UNRESTRICTED":
                            match = (prodType.ToUpper().Contains("ECON") || prodType.ToUpper().Contains("ECO-PREMIUM")) && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "BUSINESS":
                        case "BUSINESSFIRST":
                            match = prodType.ToUpper().Contains("BUS") && !prodType.ToUpper().Contains("FLEXIBLE") && !prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "BUSINESS-FLEXIBLE":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "BUSINESS-UNRESTRICTED":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "BUSINESSFIRST-FLEXIBLE":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "BUSINESSFIRST-UNRESTRICTED":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "FIRST":
                            match = prodType.ToUpper().Contains("FIRST") && !prodType.ToUpper().Contains("FLEXIBLE") && !prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "FIRST-FLEXIBLE":
                            match = prodType.ToUpper().Contains("FIRST") && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "FIRST-UNRESTRICTED":
                            match = prodType.ToUpper().Contains("FIRST") && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "AWARDECONOMY":
                            match = prodType.ToUpper().Contains("ECON") && !prodType.ToUpper().Contains("FLEXIBLE") && !prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "AWARDECONOMY-FLEXIBLE":
                            match = prodType.ToUpper().Contains("ECON") && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "AWARDECONOMY-UNRESTRICTED":
                            match = prodType.ToUpper().Contains("ECON") && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "AWARDBUSINESS":
                        case "AWARDBUSINESSFIRST":
                            {
                                var cabinName = GetCabinNameFromColumn(prodType, columnInfo, string.Empty);
                                match = cabinName.ToUpper().Contains("BUSINESS");
                                break;
                            }
                        case "AWARDBUSINESS-FLEXIBLE":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "AWARDBUSINESS-UNRESTRICTED":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "AWARDBUSINESSFIRST-FLEXIBLE":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "AWARDBUSINESSFIRST-UNRESTRICTED":
                            match = prodType.ToUpper().Contains("BUS") && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        case "AWARDFIRST":
                            {
                                var cabinName = GetCabinNameFromColumn(prodType, columnInfo, string.Empty);
                                match = cabinName.ToUpper().Contains("FIRST");
                                break;
                            }
                        case "AWARDFIRST-FLEXIBLE":
                            match = prodType.ToUpper().Contains("FIRST") && prodType.ToUpper().Contains("FLEXIBLE");
                            break;
                        case "AWARDFIRST-UNRESTRICTED":
                            match = prodType.ToUpper().Contains("FIRST") && prodType.ToUpper().Contains("UNRESTRICTED");
                            break;
                        default:
                            break;
                    }
                }
            }

            return match;
        }



        private async Task<Model.Shopping.MOBSHOPShoppingProduct> TransformProductWithoutPriceToNewProduct(string cabin, List<MOBSHOPShoppingProduct> columnInfo, bool isUADiscount,
            Product prod, bool foundEconomyAward, bool foundBusinessAward, bool foundFirstAward, Session session)
        {
            MOBSHOPShoppingProduct newProd = new MOBSHOPShoppingProduct();
            newProd.ProductDetail = new MOBSHOPShoppingProductDetail();

            newProd.IsUADiscount = isUADiscount;

            string cabinType = string.IsNullOrEmpty(prod.ProductType) ? "" : prod.ProductType.Trim().ToUpper();

            newProd.LongCabin = GetCabinDescriptionFromColumn(prod.ProductType, columnInfo);

            if (session.IsFSRRedesign)
            {
                if (columnInfo != null && columnInfo.Count > 0 && !string.IsNullOrEmpty(prod.ProductType))
                {
                    newProd.ColumnID = columnInfo.First(c => c != null && !string.IsNullOrEmpty(c.Type) && c.Type.ToUpper().Equals(prod.ProductType.ToUpper())).ColumnID;
                }
            }
            newProd.Description = GetDescriptionFromColumn(prod.ProductType, columnInfo);
            newProd.Type = prod.ProductType;
            newProd.Price = "Not available";
            newProd.ProductId = string.Empty;
            newProd.MilesDisplayAmount = 0;
            newProd.MilesDisplayValue = string.Empty;
            newProd.IsELF = prod.IsElf;
            newProd.AllCabinButtonText = _shoppingUtility.formatAllCabinAwardAmountForDisplay(newProd.MilesDisplayAmount.ToString(),
                newProd.LongCabin, true);


            switch (cabinType)
            {
                case "MIN-ECONOMY-SURP-OR-DISP": //award
                    {
                        newProd.MileageButton = -1;
                        if (!newProd.IsSelectedCabin && !foundEconomyAward)
                        {
                            newProd.MileageButton = GetMileageButtonIndex(cabin, newProd.LongCabin);
                        }
                    }
                    break;
                case "BUSINESS-SURPLUS": //award
                case "BUSINESS-DISPLACEMENT": //award
                    {
                        newProd.MileageButton = -1;
                        if (!newProd.IsSelectedCabin && !foundBusinessAward)
                        {
                            newProd.MileageButton = GetMileageButtonIndex(cabin, newProd.LongCabin);
                        }
                    }
                    break;
                case "FIRST-SURPLUS": //award
                case "FIRST-DISPLACEMENT": //award
                    {
                        newProd.MileageButton = -1;
                        if (!newProd.IsSelectedCabin && !foundFirstAward)
                        {
                            newProd.MileageButton = GetMileageButtonIndex(cabin, newProd.LongCabin);
                        }
                    }
                    break;
            }

            if (newProd.IsELF && string.IsNullOrEmpty(newProd.ProductCode))
            {
                newProd.ProductCode = _configuration.GetValue<string>("ELFProductCode"); ;
            }

            if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes"))
            {
                newProd.CabinDescription = prod.Description;
                newProd.BookingCode = prod.BookingCode;
            }
            return await Task.FromResult(newProd);
        }

        private int GetMileageButtonIndex(string requestedCabin, string awardCabin)
        {
            int buttonIndex = -1;

            if (requestedCabin.Trim().ToUpper().Contains("ECON") && awardCabin.Trim().ToUpper().Contains("BUS"))
            {
                buttonIndex = 0;
            }
            else if (requestedCabin.Trim().ToUpper().Contains("ECON") && awardCabin.Trim().ToUpper().Contains("FIRST"))
            {
                buttonIndex = 1;
            }
            else if (requestedCabin.Trim().ToUpper().Contains("BUS") && awardCabin.Trim().ToUpper().Contains("ECONOMY"))
            {
                buttonIndex = 0;
            }
            else if (requestedCabin.Trim().ToUpper().Contains("BUS") && awardCabin.Trim().ToUpper().Contains("FIRST"))
            {
                buttonIndex = 1;
            }
            else if (requestedCabin.Trim().ToUpper().Contains("FIRST") && awardCabin.Trim().ToUpper().Contains("ECONOMY"))
            {
                buttonIndex = 0;
            }
            else if (requestedCabin.Trim().ToUpper().Contains("FIRST") && awardCabin.Trim().ToUpper().Contains("BUS"))
            {
                buttonIndex = 1;
            }

            return buttonIndex;
        }

        private void SetIsPremierCabinSaverIfApplicable(Model.Shopping.MOBSHOPShoppingProduct mobShopProduct)
        {
            if (mobShopProduct.AwardType.Trim().ToUpper().Contains("SAVER") &&
               !mobShopProduct.LongCabin.Trim().ToUpper().Contains("ECON"))
            {
                mobShopProduct.ISPremierCabinSaver = true;
            }
        }

        private string GetPriceApplyLabelText(string searchType)
        {
            String PriceFromTextTripPlanner = _configuration.GetValue<string>("PriceApplyLabelTextTripPlanner") ?? "";

            if (searchType == "OW")
            {
                return PriceFromTextTripPlanner.Split('|')[0];//One Way -- For
            }
            else if (searchType == "RT")
            {
                return PriceFromTextTripPlanner.Split('|')[1];//Roundtrip from
            }
            else if (searchType == "MD")
            {
                return PriceFromTextTripPlanner.Split('|')[2];//Multitrip from
            }
            return "";
        }

        private async Task<Tuple<MOBSHOPShoppingProduct, bool, bool, bool>> TransformProductWithPriceToNewProduct
            (string cabin, Flight segment, decimal lowestAirfare,
            List<MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, bool isUADiscount, Product prod, bool supressLMX, CultureInfo ci,
            string fareClass, int productIndex, Session session, bool isELFFareDisplayAtFSR, string appVersion = "", MOBAdditionalItems additionalItems = null, string corporateFareIndicator = "", string yaDiscount = "")
        {
            bool foundEconomyAward = false, foundBusinessAward = false, foundFirstAward = false;
            MOBSHOPShoppingProduct newProd = new MOBSHOPShoppingProduct();
            newProd.ProductDetail = new MOBSHOPShoppingProductDetail();
            newProd.ProductDetail.ProductCabinMessages = new List<Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage>();

            newProd.IsUADiscount = isUADiscount;

            //get cabin data from column object
            newProd.LongCabin = GetCabinDescriptionFromColumn(prod.ProductType, columnInfo);
            newProd.CabinType = prod.CabinType;
            newProd.Description = GetDescriptionFromColumn(prod.ProductType, columnInfo);

            newProd.Type = prod.ProductType;
            newProd.SeatsRemaining = prod.BookingCount;

            newProd.ProductCode = prod.ProductCode;

            if (session.IsFSRRedesign)
            {
                if (columnInfo != null && columnInfo.Count > 0 && !string.IsNullOrEmpty(prod.ProductType))
                {
                    newProd.ColumnID = columnInfo.First(c => c != null && !string.IsNullOrEmpty(c.Type) && c.Type.ToUpper().Equals(prod.ProductType.ToUpper())).ColumnID;
                    //_logger.LogInformation("ColumnId {ColumnID} {prod.ProdType} {columnInfo}", newProd.ColumnID, prod.ProductType, JsonConvert.SerializeObject(columnInfo));
                }
            }

            SetLmxLoyaltyInformation(premierStatusLevel, prod, supressLMX, ci, newProd);

            SetProductDetails(segment, columnInfo, prod, productIndex, newProd);

            string cabinType = string.IsNullOrEmpty(prod.ProductType) ? "" : prod.ProductType.Trim().ToUpper();
            SetMileageButtonAndAwardFound(cabin, prod, ref foundEconomyAward, ref foundBusinessAward, ref foundFirstAward, cabinType, newProd);

            await SetProductPriceInformation(prod, ci, newProd, session, string.Empty, additionalItems).ConfigureAwait(false);
            newProd.Meal = string.IsNullOrEmpty(prod.MealDescription) ? "None" : prod.MealDescription;


            if ((_shoppingUtility.IsFSROAFlashSaleEnabled(session?.CatalogItems) || _shoppingUtility.IsFSROAFlashSaleEnabledInReShop(session?.CatalogItems)) && prod.PenaltyBoxIndicator == true) // if penaltybox indicator is trye not set the product id which makes the product non-clickbale in UI
            {
                newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, NoLongerAvailableBadge());
                if (session.IsReshopChange)
                    newProd.ProductId = prod.ProductId;
            }
            else
            {
                newProd.ProductId = prod.ProductId;
            }

            newProd.IsMixedCabin = !string.IsNullOrEmpty(prod.CrossCabinMessaging);

            if (newProd.IsMixedCabin)
            {
                SetProductMixedCabinInformation(segment, prod, newProd);
                if (session.IsFSRRedesign)
                {
                    newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, MixedCabinBadge());
                }
            }

            if (session.IsFSRRedesign && _configuration.GetValue<bool>("EnableAwardFSRChanges") && newProd?.AwardType?.ToUpper() == MOBFlightProductAwardType.Saver.ToString().ToUpper())
                newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, SaverAwardBadge());
            newProd.IsELF = prod.IsElf;

            if (_configuration.GetValue<bool>("EnableAdvanceSearchCouponBooking") && session.IsFSRRedesign && !string.IsNullOrEmpty(prod.PromoDescription) && !isUADiscount) //&& prod.PromoDescription.Equals("Special offer")
            {
                newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, PromoTextBadge(prod.PromoDescription));
            }
            if (session.IsFSRRedesign)
            {
                if (isUADiscount)
                {
                    newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, UADiscountBadge());
                }
                else if (!string.IsNullOrEmpty(yaDiscount))
                {
                    newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, YADiscountBadge(yaDiscount));
                }
                else if (!string.IsNullOrEmpty(corporateFareIndicator))
                {
                    newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, CorporateFareIndicatorBadge(corporateFareIndicator));
                    bool isEnableOutOfPolicyBadge = (await _shoppingUtility.IsEnableU4BCorporateTravelSessionFix().ConfigureAwait(false)) ? session.HasCorporateTravelPolicy : true;
                    if (isEnableOutOfPolicyBadge && (await _shoppingUtility.IsEnableU4BTravelAddONPolicy(session.AppID, appVersion).ConfigureAwait(false)) && (corporateFareIndicator == _configuration.GetValue<string>("CorporateFareIndicator")) && (!prod.IsCabinInPolicy || !prod.IsFareInBudget))
                    {
                        newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, CorporateTravelOutOfPolicy(_configuration.GetValue<string>("IsCorporateOutOfPolicyText")));
                    }
                }
            }
            if (newProd.IsELF && string.IsNullOrEmpty(newProd.ProductCode))
            {
                newProd.ProductCode = _configuration.GetValue<string>("ELFProductCode"); ;
            }

            if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes"))
            {
                newProd.CabinDescription = prod.Description;
                newProd.BookingCode = prod.BookingCode;
            }
            return new Tuple<MOBSHOPShoppingProduct, bool, bool, bool>(newProd, foundEconomyAward, foundBusinessAward, foundFirstAward);
        }

        private Mobile.Model.Shopping.MOBStyledText NoLongerAvailableBadge()
        {
            return new Mobile.Model.Shopping.MOBStyledText()
            {
                Text = _configuration.GetValue<string>("NoLongerAvailableProductBadgeText"),
                SortPriority = MOBFlightProductBadgeSortOrder.NoLongerAvailable.ToString()
            };
        }

        private async Task SetStrikeThroughBadges(Product prod, MOBAdditionalItems additionalItems, MOBSHOPShoppingProduct newProd)
        {
            if (await _featureSettings.GetFeatureSettingValue("EnableAwardStrikeThroughPriceEnhancement").ConfigureAwait(false) == true
                )
            {
                if (!string.IsNullOrEmpty(prod?.Context?.PercentageSavings))
                {
                    newProd.ProductBadges = SetProductBadgeInformation(newProd.ProductBadges, StrikeThroughPerCentageBadge(prod?.Context?.PercentageSavings));
                }
                newProd.StrikeThroughDisplayType = prod.Context?.StrikeThroughPricing?.Type;
                if (additionalItems != null)
                {
                    additionalItems.StrikeThroughDisplayType = prod.Context?.StrikeThroughPricing?.Type;
                }
            }
        }

        private MOBStyledText PromoTextBadge(string promoText)
        {
            if (promoText.Contains("United PassPlus Secure&#8480;"))
            {
                return new MOBStyledText() { };
            }
            else
            {
                return new MOBStyledText()
                {
                    Text = promoText,
                    SortPriority = MOBFlightProductBadgeSortOrder.Specialoffer.ToString(),
                    TextColor = _configuration.GetValue<string>("SpecialOfferColorCode")
                };
            }
        }

        private void SetMileageButtonAndAwardFound(string cabin, Product prod, ref bool foundEconomyAward,
            ref bool foundBusinessAward, ref bool foundFirstAward, string cabinType, MOBSHOPShoppingProduct newProd)
        {
            switch (cabinType)
            {
                case "MIN-ECONOMY-SURP-OR-DISP": //award
                    {
                        newProd.AwardType = prod.AwardType.ToLower();

                        newProd.MileageButton = -1;
                        if (!newProd.IsSelectedCabin && !foundEconomyAward)
                        {
                            newProd.MileageButton = GetMileageButtonIndex(cabin, newProd.LongCabin);
                            foundEconomyAward = true;
                        }
                    }
                    break;
                case "BUSINESS-SURPLUS": //award
                case "BUSINESS-DISPLACEMENT": //award
                    {
                        newProd.AwardType = prod.AwardType.ToLower();

                        newProd.MileageButton = -1;
                        if (!newProd.IsSelectedCabin && !foundBusinessAward)
                        {
                            newProd.MileageButton = GetMileageButtonIndex(cabin, newProd.LongCabin);
                            foundBusinessAward = true;
                        }
                    }
                    break;
                case "FIRST-SURPLUS": //award
                case "FIRST-DISPLACEMENT": //award
                    {
                        newProd.AwardType = prod.AwardType.ToLower();

                        newProd.MileageButton = -1;
                        if (!newProd.IsSelectedCabin && !foundFirstAward)
                        {
                            newProd.MileageButton = GetMileageButtonIndex(cabin, newProd.LongCabin);
                            foundFirstAward = true;
                        }
                    }
                    break;
            }
        }

        private List<Model.Shopping.MOBStyledText> SetProductBadgeInformation(List<Model.Shopping.MOBStyledText> badges, Model.Shopping.MOBStyledText badge)
        {
            if (badges == null)
                badges = new List<Model.Shopping.MOBStyledText>();
            //            if(badges?.Where(a=>a.Text.Trim() == badge.Text)?.Any() == false)
            badges.Add(badge);

            if (badges.Count > 1)
            {
                badges = badges.OrderBy(x => (int)Enum.Parse(typeof(MOBFlightProductBadgeSortOrder), x.SortPriority)).ToList();
            }

            return badges;
        }

        private void SetProductMixedCabinInformation(Flight segment, Product prod, MOBSHOPShoppingProduct newProd)
        {
            Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage detailCabinMessage =
              new Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage();
            if (prod.CrossCabinMessaging.ToUpper().Contains("ECONOMY") ||
                prod.CrossCabinMessaging.ToUpper().Contains("COACH"))
            {
                if (newProd.LongCabin.ToUpper().Contains("BUS") || newProd.LongCabin.ToUpper().Contains("FIRST") || newProd.LongCabin.ToUpper().Contains("PREMIUM ECONOMY"))
                {
                    if (prod.Description.ToUpper().Contains("ECONOMY") || prod.Description.ToUpper().Contains("COACH"))
                    {
                        newProd.MixedCabinSegmentMessages = new List<string>();
                        newProd.MixedCabinSegmentMessages.Add(String.Format("{0}-{1} {2}", segment.Origin,
                            segment.Destination, prod.Description + " (" + prod.BookingCode + ")"));
                        newProd.IsSelectedCabin = false;
                        detailCabinMessage.IsMixedCabin = true;
                    }
                    else
                    {
                        detailCabinMessage.IsMixedCabin = false;
                    }

                    detailCabinMessage.Cabin = prod.Description + " (" + prod.BookingCode + ")";
                    detailCabinMessage.Segments = String.Format("{0} - {1}", segment.Origin, segment.Destination);
                }
            }
            else
            {
                if (newProd.LongCabin.ToUpper().Contains("ECONOMY") || newProd.LongCabin.ToUpper().Contains("COACH"))
                {
                    if (prod.Description.ToUpper().Contains("BUS") || prod.Description.ToUpper().Contains("FIRST") || newProd.LongCabin.ToUpper().Contains("PREMIUM ECONOMY"))
                    {
                        newProd.MixedCabinSegmentMessages = new List<string>();
                        newProd.MixedCabinSegmentMessages.Add(String.Format("{0}-{1} {2}", segment.Origin,
                            segment.Destination, prod.Description + " (" + prod.BookingCode + ")"));
                        newProd.IsSelectedCabin = false;
                        detailCabinMessage.IsMixedCabin = true;
                    }
                    else
                    {
                        detailCabinMessage.IsMixedCabin = false;
                    }

                    detailCabinMessage.Cabin = prod.Description + " (" + prod.BookingCode + ")";
                    detailCabinMessage.Segments = String.Format("{0} - {1}", segment.Origin, segment.Destination);
                }
            }
            newProd.ProductDetail.ProductCabinMessages.Add(detailCabinMessage);
        }

        private async Task SetProductPriceInformation(Product prod, CultureInfo ci, MOBSHOPShoppingProduct newProd, Session session, string appVersion = "", MOBAdditionalItems additionalItems = null)
        {
            var closeInFee = CalculateCloseInAwardFee(prod);
            decimal totalAmount = 0;
            var totalAmountDisplay = string.Empty;
            if (session != null && session.IsReshopChange)
            {
                if (session.IsAward)
                {
                    totalAmount = ReshopAwardAirfareDisplayValueInDecimal(prod.Prices, true);
                    if (ReshopAwardPrice(prod.Prices) == null)
                        newProd.MilesDisplayValue = "NA";
                    else
                        newProd.MilesDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(ReshopAwardPrice(prod.Prices).Amount.ToString(), true);
                    newProd.Price = "+ " + TopHelper.FormatAmountForDisplay(totalAmount, ci, false);
                    newProd.MilesDisplayAmount = totalAmount;
                    if (_configuration.GetValue<bool>("EnableAwardPricesForAllProducts"))
                    {
                        newProd.AllCabinButtonText = ReshopAwardPrice(prod.Prices).Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                       ? _shoppingUtility.formatAllCabinAwardAmountForDisplay(ReshopAwardPrice(prod.Prices).Amount.ToString(), newProd.LongCabin, true, newProd.Price)
                       : string.Empty;
                    }
                    else
                    {
                        newProd.AllCabinButtonText = ReshopAwardPrice(prod.Prices).Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                       ? _shoppingUtility.formatAllCabinAwardAmountForDisplay(ReshopAwardPrice(prod.Prices).Amount.ToString(), newProd.LongCabin, true)
                       : string.Empty;
                    }

                    newProd.PriceAmount = totalAmount;
                    if (prod?.PenaltyBoxIndicator == false)
                        newProd.ProductId = prod.ProductId;

                }
                else
                {
                    totalAmount = ReshopAirfareDisplayValueInDecimal(prod.Prices);
                    newProd.Price = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                            ? TopHelper.FormatAmountForDisplay(totalAmount + closeInFee, ci, true, true)
                            : TopHelper.FormatAmountForDisplay(totalAmount, ci);
                    newProd.MilesDisplayAmount = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES ? totalAmount : 0;
                    newProd.MilesDisplayValue = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                        ? ShopStaticUtility.FormatAwardAmountForDisplay(totalAmount.ToString(), true)
                        : string.Empty;
                    newProd.AllCabinButtonText = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                        ? _shoppingUtility.formatAllCabinAwardAmountForDisplay(totalAmount.ToString(), newProd.LongCabin, true)
                        : string.Empty;
                    newProd.PriceAmount = totalAmount;

                    if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                    {
                        if (totalAmount.CompareTo(decimal.Zero) > 0)
                        {
                            newProd.Price = string.Concat("+", newProd.Price);
                        }
                        newProd.ReshopFees = ReshopAirfareDisplayText(prod.Prices);
                    }
                    if (_shoppingUtility.EnableReShopAirfareCreditDisplay(session.AppID, appVersion))
                    {
                        newProd = ReShopAirfareCreditDisplayFSRD(ci, prod, newProd);
                    }
                }
            }
            else
            {
                newProd.Price = prod.Prices[0].Currency.Trim().ToLower() ==
                    CURRENCY_TYPE_MILES && !_configuration.GetValue<bool>("NGRPAwardCalendarMP2017NewUISwitch") ?
                    TopHelper.FormatAmountForDisplay(prod.Prices[1].Amount + closeInFee, ci, true, true) :
                    prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES &&
                    _configuration.GetValue<bool>("NGRPAwardCalendarMP2017NewUISwitch") ?
                    TopHelper.FormatAmountForDisplay(prod.Prices[1].Amount + closeInFee, ci, false, true) :
                    TopHelper.FormatAmountForDisplay(prod.Prices[0].Amount, ci);
                if (session.IsMoneyPlusMilesSelected && _configuration.GetValue<bool>("EnableFSRMoneyPlusMilesFeature"))
                {
                    if (prod.Prices?.Any(c => c.PricingType?.Trim().ToLower() == CURRENCY_TYPE_MILES) == true)
                    {
                        newProd.MilesDisplayAmount = prod.Prices.FirstOrDefault(c => c.PricingType?.Trim().ToLower() == CURRENCY_TYPE_MILES)?.Amount ?? 0;
                        newProd.MilesDisplayValue = prod.Prices.Any(c => c.PricingType?.Trim().ToLower() == CURRENCY_TYPE_MILES)
                                                    ? "+ " + ShopStaticUtility.FormatAwardAmountForDisplay(prod.Prices.FirstOrDefault(c => c.PricingType?.Trim().ToLower() == CURRENCY_TYPE_MILES)?.Amount.ToString(), true)
                                                    : string.Empty;
                        newProd.MilesPlusMoneyDisplayValue = newProd.Price + "<br> <small>" + newProd.MilesDisplayValue + "</small>";
                        newProd.MoneyPlusMilesOptionId = prod.MoneyAndMilesOptionId;
                        if (additionalItems == null)
                        {
                            additionalItems = new MOBAdditionalItems();
                        }
                        if (additionalItems.MoneyPlusMilesPricing == false)
                        {
                            additionalItems.MoneyPlusMilesPricing = true;
                        }
                    }
                }
                else
                {
                    newProd.MilesDisplayAmount = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES ? prod.Prices[0].Amount : 0;
                    newProd.MilesDisplayValue = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                        ? ShopStaticUtility.FormatAwardAmountForDisplay(prod.Prices[0].Amount.ToString(), true)
                        : string.Empty;
                }
                if (_configuration.GetValue<bool>("EnableAwardPricesForAllProducts"))
                {
                    newProd.AllCabinButtonText = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                    ? _shoppingUtility.formatAllCabinAwardAmountForDisplay(prod.Prices[0].Amount.ToString(), newProd.LongCabin, true, newProd.Price)
                    : string.Empty;
                }
                else
                {
                    newProd.AllCabinButtonText = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                    ? _shoppingUtility.formatAllCabinAwardAmountForDisplay(prod.Prices[0].Amount.ToString(), newProd.LongCabin, true)
                    : string.Empty;
                }
                if (_configuration.GetValue<bool>("EnableAwardStrikeThroughPricing") && session.IsAward && session.CatalogItems != null && session.CatalogItems.Count > 0 &&
                        session.CatalogItems.FirstOrDefault(a => a.Id == ((int)Common.Helper.Profile.IOSCatalogEnum.AwardStrikeThroughPricing).ToString() || a.Id == ((int)Common.Helper.Profile.AndroidCatalogEnum.AwardStrikeThroughPricing).ToString())?.CurrentValue == "1"
                        && prod.Context?.StrikeThroughPricing != null && prod.Context?.StrikeThroughPricing?.PaxPrice != null && prod.Prices?.Any(x => x.PricingType == "Award") == true
                         && (int)prod.Context?.StrikeThroughPricing?.PaxPrice?.Miles != (int)prod.Prices?.FirstOrDefault(x => x.PricingType == "Award").Amount
                   )
                {
                    newProd.StrikeThroughDisplayValue = formatAwardAmountForDisplay(prod.Context?.StrikeThroughPricing?.PaxPrice?.Miles.ToString(), true);
                    if (additionalItems == null)
                    {
                        additionalItems = new MOBAdditionalItems();
                    }
                    if (additionalItems.StrikeThroughPricing == false)
                    {
                        additionalItems.StrikeThroughPricing = true;
                    }
                    if (additionalItems.StrikeThroughPricing == true && string.IsNullOrEmpty(additionalItems.StrikeThroughTripDisplayType))
                    {
                       additionalItems.StrikeThroughTripDisplayType = prod.Context?.StrikeThroughPricing?.Type;                       
                    }
                    await SetStrikeThroughBadges(prod, additionalItems, newProd).ConfigureAwait(false);
                }
                if (session.CatalogItems != null && session.CatalogItems.Count > 0
                          //&& session.CatalogItems.FirstOrDefault(a => a.Id == ((int)Common.Helper.Profile.IOSCatalogEnum.EnableETCCreditsInBooking).ToString() || a.Id == ((int)Common.Helper.Profile.AndroidCatalogEnum.EnableETCCreditsInBooking).ToString())?.CurrentValue == "1"
                          && prod.Context?.StrikeThroughPricing != null && prod.Context?.StrikeThroughPricing?.ItaPrice != null && prod.Prices?.Any(x => x.PricingType == "Fare") == true
                           && Convert.ToDouble(prod.Context?.StrikeThroughPricing?.ItaPrice) > (double)prod.Prices?.FirstOrDefault(x => x.PricingType == "Fare").Amount
                     )
                {
                    newProd.StrikeThroughDisplayValue = TopHelper.FormatAmountForDisplay(prod.Context?.StrikeThroughPricing?.ItaPrice, ci);

                }
                newProd.PriceAmount = prod.Prices[0].Amount;
            }
        }
        private MOBSHOPFlight ReShopAirfareCreditDisplayFSR(CultureInfo ci, Product product, MOBSHOPFlight flight)
        {
            var price = product.Prices;

            if (price != null && price.Any())
            {
                decimal displayprice = ReshopAirfareDisplayValueInDecimal(price);

                if (displayprice.CompareTo(decimal.Zero) == 0)
                {
                    decimal displayCredit = ReshopAirfareCreditDisplayInDecimal(price, "refundPrice");

                    if (displayCredit.CompareTo(decimal.Zero) < 0)
                    {
                        displayCredit = displayCredit * -1;
                    }

                    string strDisplayCredit
                        = TopHelper.FormatAmountForDisplay(displayCredit, ci, true);

                    flight.ReshopCreditColor = CreditTypeColor.GREEN;

                    //displayPrice = string.Concat("+", displayPrice);
                    //AirfareDisplayValue
                    if (product.CreditType == CreditTypes.Refund)
                    {
                        flight.ReshopFees = CreditType.REFUND.GetDisplayName();
                        flight.IsReshopCredit = true;
                    }
                    else if (product.CreditType == CreditTypes.FlightCredit)
                    {
                        flight.ReshopFees = CreditType.FLIGHTCREDIT.GetDisplayName();
                        flight.IsReshopCredit = true;
                    }

                    flight.AirfareDisplayValue = strDisplayCredit;
                }
            }
            return flight;
        }

        private MOBSHOPShoppingProduct ReShopAirfareCreditDisplayFSRD
           (CultureInfo ci, United.Services.FlightShopping.Common.Product product, MOBSHOPShoppingProduct shoppingProduct)
        {
            var price = product.Prices;

            if (price != null && price.Any())
            {
                decimal displayprice = ReshopAirfareDisplayValueInDecimal(price);

                if (displayprice.CompareTo(decimal.Zero) == 0)
                {
                    decimal displayCredit = ReshopAirfareCreditDisplayInDecimal(price, "refundPrice");

                    if (displayCredit.CompareTo(decimal.Zero) < 0)
                    {
                        displayCredit = displayCredit * -1;
                    }

                    string strDisplayCredit
                        = TopHelper.FormatAmountForDisplay(displayCredit, ci, true);

                    shoppingProduct.ReshopCreditColor = CreditTypeColor.GREEN;

                    //displayPrice = string.Concat("+", displayPrice);
                    //AirfareDisplayValue
                    if (product.CreditType == CreditTypes.Refund)
                    {
                        shoppingProduct.ReshopFees = CreditType.REFUND.GetDisplayName();
                        shoppingProduct.IsReshopCredit = true;
                    }
                    else if (product.CreditType == CreditTypes.FlightCredit)
                    {
                        shoppingProduct.ReshopFees = CreditType.FLIGHTCREDIT.GetDisplayName();
                        shoppingProduct.IsReshopCredit = true;
                    }

                    shoppingProduct.Price = strDisplayCredit;
                }
            }
            return shoppingProduct;
        }
        private decimal ReshopAirfareCreditDisplayInDecimal(List<PricingItem> price, string priceType)
        {
            decimal retVal = 0;
            if (price.Exists(p => string.Equals(p.PricingType, priceType, StringComparison.OrdinalIgnoreCase)))
                retVal = price.FirstOrDefault(p => string.Equals(p.PricingType, priceType, StringComparison.OrdinalIgnoreCase)).Amount;
            return retVal;
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

        private decimal CalculateCloseInAwardFee(Product prod)
        {
            decimal closeInFee = 0;
            if (prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES)
            {
                foreach (PricingItem p in prod.Prices)
                {
                    if (p.PricingType.Trim().ToUpper() == PRICING_TYPE_CLOSE_IN_FEE)
                    {
                        closeInFee = p.Amount;
                        break;
                    }
                }
            }
            return closeInFee;
        }

        private void SetProductDetails(Flight segment, List<MOBSHOPShoppingProduct> columnInfo, Product prod, int productIndex,
           MOBSHOPShoppingProduct newProd)
        {
            if (productIndex >= 2 && segment.Connections != null && segment.Connections.Count > 0)
            {
                if (!string.IsNullOrEmpty(prod.ProductType) &&
                    (prod.ProductType.Contains("FIRST") || prod.ProductType.Contains("BUSINESS")) &&
                    !string.IsNullOrEmpty(prod.Description) && prod.Description.Contains("Economy"))
                {
                    newProd.LongCabin = GetCabinDescriptionFromColumn(columnInfo[productIndex].Type, columnInfo);
                    newProd.Description = GetDescriptionFromColumn(columnInfo[productIndex].Type, columnInfo);

                    MOBProductSettings section = configurationProductSettings;
                    if (section != null && section.Products != null &&
                        section.Products.Count > 0 && columnInfo != null && columnInfo.Count > 0)
                    {
                        foreach (var cInfo in columnInfo)
                        {
                            foreach (MOBProductElements productElement in section.Products)
                            {
                                if (productElement.Key.Equals(columnInfo[productIndex].Type) &&
                                    productElement.Title.Equals(cInfo.LongCabin) && productElement.CabinCount.Equals("0"))
                                {
                                    newProd.ProductDetail.Body = productElement.Body;
                                    newProd.ProductDetail.Title = cInfo.LongCabin + " " + cInfo.Description;
                                    newProd.ProductDetail.Header = productElement.Header;
                                    newProd.ProductDetail.ProductDetails = productElement.Details.Split('|').ToList();
                                    return;
                                }

                                if (productElement.Key.Equals(columnInfo[productIndex].Type) &&
                                    productElement.Title.Equals(cInfo.LongCabin) &&
                                    productElement.CabinCount.Equals(segment.CabinCount.ToString()))
                                {
                                    newProd.ProductDetail.Body = productElement.Body;
                                    newProd.ProductDetail.Title = cInfo.LongCabin + " " + cInfo.Description;
                                    newProd.ProductDetail.Header = productElement.Header;
                                    newProd.ProductDetail.ProductDetails = productElement.Details.Split('|').ToList();
                                    return;
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(prod.AwardType))
                    {
                        MOBProductSettings section = configurationProductSettings;
                        if (section != null && section.Products != null &&
                            section.Products.Count > 0 && columnInfo != null && columnInfo.Count > 0)
                        {
                            foreach (var cInfo in columnInfo)
                            {
                                foreach (MOBProductElements productElement in section.Products)
                                {
                                    if (productElement.Key.Equals(prod.ProductType) && productElement.Title.Equals(cInfo.LongCabin) &&
                                        productElement.CabinCount.Equals("0"))
                                    {
                                        newProd.ProductDetail.Body = productElement.Body;
                                        newProd.ProductDetail.Title = cInfo.LongCabin + " " + cInfo.Description;
                                        newProd.ProductDetail.Header = productElement.Header;
                                        newProd.ProductDetail.ProductDetails = productElement.Details.Split('|').ToList();
                                        return;
                                    }

                                    if (productElement.Key.Equals(prod.ProductType) && productElement.Title.Equals(cInfo.LongCabin) &&
                                        productElement.CabinCount.Equals(segment.CabinCount.ToString()))
                                    {
                                        newProd.ProductDetail.Body = productElement.Body;
                                        newProd.ProductDetail.Title = cInfo.LongCabin + " " + cInfo.Description;
                                        newProd.ProductDetail.Header = productElement.Header;
                                        newProd.ProductDetail.ProductDetails = productElement.Details.Split('|').ToList();
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(prod.AwardType))
                {
                    MOBProductSettings section = configurationProductSettings;
                    if (section != null && section.Products != null &&
                        section.Products.Count > 0 && columnInfo != null && columnInfo.Count > 0)
                    {
                        foreach (var cInfo in columnInfo)
                        {
                            foreach (MOBProductElements productElement in section.Products)
                            {
                                if (productElement.Key.Equals(prod.ProductType) && productElement.Title.Equals(cInfo.LongCabin) &&
                                    productElement.CabinCount.Equals("0"))
                                {
                                    newProd.ProductDetail.Body = productElement.Body;
                                    newProd.ProductDetail.Title = cInfo.LongCabin + " " + cInfo.Description;
                                    newProd.ProductDetail.Header = productElement.Header;
                                    newProd.ProductDetail.ProductDetails = productElement.Details.Split('|').ToList();
                                    return;
                                }

                                if (productElement.Key.Equals(prod.ProductType) && productElement.Title.Equals(cInfo.LongCabin) &&
                                    productElement.CabinCount.Equals(segment.CabinCount.ToString()))
                                {
                                    newProd.ProductDetail.Body = productElement.Body;
                                    newProd.ProductDetail.Title = cInfo.LongCabin + " " + cInfo.Description;
                                    newProd.ProductDetail.Header = productElement.Header;
                                    newProd.ProductDetail.ProductDetails = productElement.Details.Split('|').ToList();
                                    return;
                                }

                                ///179440 : Booking FSR mApp: First lowest desciption is empty in the Compare screens in the Multi Trip booking flow
                                ///Srini - 11/27/2017
                                if (_configuration.GetValue<bool>("BugFixToggleFor17M"))
                                {
                                    if (
                                        (
                                            (productElement.Key.Equals((prod.CabinType ?? string.Empty).ToUpper()) && productElement.Title.Equals(cInfo.LongCabin)) ||
                                            ///238434 - mApp: Booking - FSR - First lowest description is empty in the Compare screens for specific markets
                                            ///Srini - 03/21/2018
                                            (_configuration.GetValue<bool>("BugFixToggleFor18C") && productElement.Key.Equals((cInfo.LongCabin ?? string.Empty).ToUpper()) && cInfo.Type.Equals(prod.ProductType))
                                        ) &&

                                        (newProd.ProductDetail.ProductDetails == null || newProd.ProductDetail.ProductDetails.Count == 0) &&
                                        (productElement.CabinCount.Equals("0") || productElement.CabinCount.Equals(segment.CabinCount.ToString())))
                                    {
                                        newProd.ProductDetail.Body = productElement.Body;
                                        newProd.ProductDetail.Title = cInfo.LongCabin + " " + cInfo.Description;
                                        newProd.ProductDetail.Header = productElement.Header;
                                        newProd.ProductDetail.ProductDetails = productElement.Details.Split('|').ToList();
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SetLmxLoyaltyInformation(int premierStatusLevel, Product prod, bool supressLMX, CultureInfo ci,
            MOBSHOPShoppingProduct newProd)
        {
            if (!supressLMX && prod.LmxLoyaltyTiers != null && prod.LmxLoyaltyTiers.Count > 0)
            {
                foreach (LmxLoyaltyTier tier in prod.LmxLoyaltyTiers)
                {
                    if (tier != null)
                    {
                        int tempStatus = premierStatusLevel;
                        if (premierStatusLevel > 4) //GS gets same LMX as 1K
                            tempStatus = 4;

                        if (tier.Level == tempStatus)
                        {
                            if (tier.LmxQuotes != null && tier.LmxQuotes.Count > 0)
                            {
                                foreach (LmxQuote quote in tier.LmxQuotes)
                                {
                                    switch (quote.Type.Trim().ToUpper())
                                    {
                                        case "RDM":
                                            newProd.RdmText = string.Format("{0:#,##0}", quote.Amount);
                                            break;
                                        case "PQM":
                                            newProd.PqmText = string.Format("{0:#,##0}", quote.Amount);
                                            break;
                                        case "PQD":
                                            newProd.PqdText = TopHelper.FormatAmountForDisplay(quote.Amount, ci, true, false);
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private string GetDescriptionFromColumn(string productType, List<MOBSHOPShoppingProduct> columnInfo)
        {
            string description = string.Empty;
            MOBProductSettings section = configurationProductSettings;
            if (section != null && section.Products != null &&
                section.Products.Count > 0 && columnInfo != null && columnInfo.Count > 0)
            {
                foreach (var ci in columnInfo)
                {
                    foreach (MOBProductElements productElement in section.Products)
                    {
                        if (productElement.Key.Equals(productType) && productElement.Title.Equals(ci.LongCabin))
                        {
                            description = productElement.Header;
                            return description;
                        }
                    }
                }
            }

            return description;
        }

        private string GetCabinDescriptionFromColumn(string type, List<MOBSHOPShoppingProduct> columnInfo)
        {
            type = type.IsNullOrEmpty() ? string.Empty : type.ToUpper().Trim();

            string cabin = string.Empty;
            if (columnInfo != null && columnInfo.Count > 0)
            {
                foreach (MOBSHOPShoppingProduct prod in columnInfo)
                {
                    if (!prod.Type.IsNullOrEmpty() && type == prod.Type.ToUpper().Trim())
                    {
                        cabin = (prod.LongCabin + " " + prod.Description).Trim();
                        break;
                    }
                }
            }
            return cabin;
        }


        private Model.Shopping.MOBStyledText MixedCabinBadge()
        {
            return new Model.Shopping.MOBStyledText()
            {
                Text = _configuration.GetValue<string>("MixedCabinProductBadgeText"),
                SortPriority = MOBFlightProductBadgeSortOrder.MixedCabin.ToString()
            };
        }
        private void SetSegmentInfoMessages(Model.Shopping.MOBSHOPFlight flight, Warning warn)
        {
            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("ARRIVAL_Slice")
                && !string.IsNullOrEmpty(warn.Title))
            {
                flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, ArrivesNextDaySegmentInfo(warn.Title));
            }

            if (!string.IsNullOrEmpty(warn.Key) && (warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE"))
                || (_configuration.GetValue<bool>("EnableAwardFSRChanges") && !string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE_KEY")))
            {
                if (!string.IsNullOrEmpty(warn.Title))
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, AirportChangeSegmentInfo(warn.Title));
                }
                else
                if (warn.Messages != null && warn.Messages.Count > 0)
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, AirportChangeSegmentInfo(warn.Messages[0]));
                }
            }

            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("SubjectToReceiptOfGovtAuthority_SLICE"))
            {
                if (!string.IsNullOrEmpty(warn.Title))
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, SubjectOfReceiptOfGovtAuthSegmentInfo(warn.Title));
            }

            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("LONG_LAYOVER_SLICE"))
            {
                if (!string.IsNullOrEmpty(warn.Title))
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, LonglayoverSegmentInfo(warn.Title));
                }
                else
                if (warn.Messages != null && warn.Messages.Count > 0)
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, LonglayoverSegmentInfo(warn.Messages[0]));
                }
            }

            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("Red-eyeFlight_Slice"))
            {
                if (!string.IsNullOrEmpty(warn.Title))
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, RedEyeFlightSegmentInfo(warn.Title));
                }
                else
                if (warn.Messages != null && warn.Messages.Count > 0)
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, RedEyeFlightSegmentInfo(warn.Messages[0]));
                }
            }

            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("RISKYCONNECTION_SLICE"))
            {
                if (!string.IsNullOrEmpty(warn.Title))
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, RiskyConnectionSegmentInfo(warn.Title));
                }
                else
                if (warn.Messages != null && warn.Messages.Count > 0)
                {
                    //if (!string.IsNullOrEmpty(warn.Title))
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, RiskyConnectionSegmentInfo(warn.Messages[0]));
                }
            }
            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("TerminalChange_SLICE"))
            {
                if (!string.IsNullOrEmpty(warn.Title))
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, TerminalChangeSegmentInfo(warn.Title));
                }
                else
                if (warn.Messages != null && warn.Messages.Count > 0)
                {
                    //if (!string.IsNullOrEmpty(warn.Title))
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, TerminalChangeSegmentInfo(warn.Messages[0]));
                }
            }
            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("WARNING_NEARBYAIRPORT"))
            {
                if (warn.Messages != null && warn.Messages.Count > 0)
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, NearByAirportSegmentInfo(warn.Messages[0], warn.Key.Trim()));
                }
                else if (!string.IsNullOrEmpty(warn.Title))
                {
                    flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, NearByAirportSegmentInfo(warn.Title, warn.Key.Trim()));
                }
            }
        }

        private SegmentInfoAlerts TerminalChangeSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = SegmentInfoAlertsOrder.TerminalChange.ToString()
            };
        }

        private SegmentInfoAlerts RiskyConnectionSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = SegmentInfoAlertsOrder.RiskyConnection.ToString()
            };
        }

        private SegmentInfoAlerts NearByAirportSegmentInfo(string msg, string warnKey)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.NearByAirport.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRCollapsed.ToString(),
                Key = warnKey
            };
        }

        private SegmentInfoAlerts RedEyeFlightSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = SegmentInfoAlertsOrder.RedEyeFlight.ToString()
            };
        }

        private SegmentInfoAlerts LonglayoverSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = SegmentInfoAlertsOrder.LongLayover.ToString()
            };
        }

        private SegmentInfoAlerts SubjectOfReceiptOfGovtAuthSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = SegmentInfoAlertsOrder.GovAuthority.ToString()
            };
        }

        private SegmentInfoAlerts AirportChangeSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Warning.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = SegmentInfoAlertsOrder.AirportChange.ToString()
            };
        }

        private SegmentInfoAlerts ArrivesNextDaySegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                //_configuration.GetValue<string>("NextDayArrivalSegmentText"),
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = SegmentInfoAlertsOrder.ArrivesNextDay.ToString()
            };
        }

        private Model.Shopping.SHOPOnTimePerformance PopulateOnTimePerformanceSHOP(DOTAirlinePerformance onTimePerformance)
        {

            Model.Shopping.SHOPOnTimePerformance shopOnTimePerformance = null;
            if (_configuration.GetValue<bool>("ReturnOnTimePerformance"))
            {
                #region
                if (onTimePerformance != null)
                {
                    shopOnTimePerformance = new Model.Shopping.SHOPOnTimePerformance();
                    shopOnTimePerformance.Source = onTimePerformance.Source;
                    shopOnTimePerformance.DOTMessages = new Model.Shopping.SHOPOnTimeDOTMessages();
                    string[] dotOnTimeMessages = null;
                    if (!string.IsNullOrEmpty(shopOnTimePerformance.Source) && shopOnTimePerformance.Source.ToUpper().Equals("BR"))
                    {
                        dotOnTimeMessages = _configuration.GetValue<string>("DOTOnTimeMessagesBrazil").Split('|');
                    }
                    else
                    {
                        dotOnTimeMessages = _configuration.GetValue<string>("DOTOnTimeMessages").Split('|');
                    }
                    shopOnTimePerformance.DOTMessages.CancellationPercentageMessage = dotOnTimeMessages[0].ToString();
                    shopOnTimePerformance.DOTMessages.DelayPercentageMessage = dotOnTimeMessages[1].ToString();
                    shopOnTimePerformance.DOTMessages.DelayAndCancellationPercentageMessage = dotOnTimeMessages[2].ToString();
                    shopOnTimePerformance.DOTMessages.DOTMessagePopUpButtonCaption = dotOnTimeMessages[3].ToString();

                    shopOnTimePerformance.EffectiveDate = string.Format("{0}, {1}", onTimePerformance.Month, onTimePerformance.Year);
                    shopOnTimePerformance.PctOnTimeCancelled = onTimePerformance.CancellationRate < 0 ? "---" : onTimePerformance.CancellationRate.ToString();

                    if (!string.IsNullOrEmpty(shopOnTimePerformance.Source) && shopOnTimePerformance.Source.ToUpper().Equals("BR"))
                    {
                        int delay = onTimePerformance.ArrivalMoreThan30MinLateRate + onTimePerformance.ArrivalMoreThan60MinLateRate;
                        shopOnTimePerformance.PctOnTimeDelayed = onTimePerformance.ArrivalMoreThan30MinLateRate < 0 ? "---" : onTimePerformance.ArrivalMoreThan30MinLateRate.ToString();
                        shopOnTimePerformance.PctOnTimeMax = onTimePerformance.ArrivalMoreThan60MinLateRate < 0 ? "---" : onTimePerformance.ArrivalMoreThan60MinLateRate.ToString();
                        shopOnTimePerformance.PctOnTimeMin = onTimePerformance.ArrivalMoreThan60MinLateRate < 0 ? "---" : onTimePerformance.ArrivalMoreThan60MinLateRate.ToString();
                    }
                    else
                    {
                        int delay = -1;
                        if (!int.TryParse(onTimePerformance.ArrivalLateRate.Replace("%", ""), out delay))
                        {
                            delay = -1;
                            onTimePerformance.ArrivalLateRate = "";
                        }
                        shopOnTimePerformance.PctOnTimeDelayed = delay < 0 ? "---" : delay.ToString();
                        int onTime = -1;
                        if (!int.TryParse(onTimePerformance.ArrivalOnTimeRate.Replace("%", ""), out onTime))
                        {
                            onTime = -1;
                            onTimePerformance.ArrivalOnTimeRate = "";
                        }
                        shopOnTimePerformance.PctOnTimeMax = onTime < 0 ? "---" : onTime.ToString();
                        shopOnTimePerformance.PctOnTimeMin = onTime < 0 ? "---" : onTime.ToString();
                    }


                    if (onTimePerformance.ArrivalMoreThan30MinLateRate <= 0 && onTimePerformance.ArrivalMoreThan60MinLateRate <= 0 && onTimePerformance.CancellationRate <= 0 && string.IsNullOrEmpty(onTimePerformance.ArrivalOnTimeRate))
                    {
                        List<string> lstOnTimeNotAvailableMessage = new List<string>(1);
                        lstOnTimeNotAvailableMessage.Add(_configuration.GetValue<string>("DOTOnTimeNotAvailableMessage"));
                        shopOnTimePerformance.OnTimeNotAvailableMessage = lstOnTimeNotAvailableMessage;
                    }
                }
                else
                {
                    shopOnTimePerformance = new Model.Shopping.SHOPOnTimePerformance();
                    shopOnTimePerformance.DOTMessages = new SHOPOnTimeDOTMessages();
                    string[] dotOnTimeMessages = _configuration.GetValue<string>("DOTOnTimeMessages").Split('|');
                    shopOnTimePerformance.DOTMessages.CancellationPercentageMessage = dotOnTimeMessages[0].ToString();
                    shopOnTimePerformance.DOTMessages.DelayPercentageMessage = dotOnTimeMessages[1].ToString();
                    shopOnTimePerformance.DOTMessages.DelayAndCancellationPercentageMessage = dotOnTimeMessages[2].ToString();
                    shopOnTimePerformance.DOTMessages.DOTMessagePopUpButtonCaption = dotOnTimeMessages[3].ToString();

                    List<string> lstOnTimeNotAvailableMessage = new List<string>(1);
                    lstOnTimeNotAvailableMessage.Add(_configuration.GetValue<string>("DOTOnTimeNotAvailableMessage"));
                    shopOnTimePerformance.OnTimeNotAvailableMessage = lstOnTimeNotAvailableMessage;
                }
                #endregion
            }
            return shopOnTimePerformance;


        }

        private Model.Shopping.SHOPEquipmentDisclosure GetEquipmentDisclosures(United.Services.FlightShopping.Common.EquipmentDisclosure equipmentDisclosure)
        {
            Model.Shopping.SHOPEquipmentDisclosure bkEquipmentDisclosure = null;
            if (equipmentDisclosure != null)
            {
                bkEquipmentDisclosure = new Model.Shopping.SHOPEquipmentDisclosure();
                bkEquipmentDisclosure.EquipmentType = equipmentDisclosure.EquipmentType;
                bkEquipmentDisclosure.EquipmentDescription = equipmentDisclosure.EquipmentDescription;
                bkEquipmentDisclosure.IsSingleCabin = equipmentDisclosure.IsSingleCabin;
                bkEquipmentDisclosure.NoBoardingAssistance = equipmentDisclosure.NoBoardingAssistance;
                bkEquipmentDisclosure.NonJetEquipment = equipmentDisclosure.NonJetEquipment;
                bkEquipmentDisclosure.WheelchairsNotAllowed = equipmentDisclosure.WheelchairsNotAllowed;
                bkEquipmentDisclosure.AircraftDoorHeight = equipmentDisclosure.AircraftDoorHeight;
                bkEquipmentDisclosure.AircraftDoorWidth = equipmentDisclosure.AircraftDoorWidth;
            }
            return bkEquipmentDisclosure;
        }


        private string FormatDateTime(string dateTimeString)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(0);
            DateTime.TryParse(dateTimeString, out dateTime);
            result = string.Format("{0:MM/dd/yyyy hh:mm tt}", dateTime);

            return result;
        }

        private string FormatTime(string dateTimeString)
        {
            string result = string.Empty;
            DateTime dateTime = new DateTime(0);
            DateTime.TryParse(dateTimeString, out dateTime);
            if (dateTime.Ticks != 0)
            {
                result = dateTime.ToString("h:mmtt").ToLower();
            }
            return result;
        }

        private string FormatDate(string dateTimeString)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(0);
            DateTime.TryParse(dateTimeString, out dateTime);
            if (dateTime.Ticks != 0)
            {
                if (dateTime.ToString("MMMM").Length > 3)
                {
                    result = string.Format("{0:ddd., MMM. d, yyyy}", dateTime);
                }
                else
                {
                    result = string.Format("{0:ddd., MMM d, yyyy}", dateTime);
                }
            }
            return result;
        }

        private string GetFormattedTravelTime(int minutes)
        {
            if (minutes < 60)
            {
                return string.Format("{0}m", minutes);
            }
            else
            {
                return string.Format("{0}h {1}m", minutes / 60, minutes % 60);
            }
        }

        private string GetCabinDescription(string cos)
        {
            string cabin = string.Empty;
            cos = cos.Trim();
            if (!string.IsNullOrEmpty(cos))
            {
                switch (cos.ToLower())
                {
                    case "united economy":
                        cabin = "Coach";
                        break;
                    case "economy":
                        cabin = "Coach";
                        break;
                    case "united business":
                        cabin = "Business";
                        break;
                    case "business":
                        cabin = "Business";
                        break;
                    case "united businessfirst":
                        cabin = "BusinessFirst";
                        break;
                    case "businessfirst":
                        cabin = "BusinessFirst";
                        break;
                    case "united global first":
                        cabin = "First";
                        break;
                    case "united first":
                        cabin = "First";
                        break;
                    case "first":
                        cabin = "First";
                        break;
                }
            }
            return cabin;
        }

        private SegmentInfoAlerts TicketsLeftSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Warning.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = SegmentInfoAlertsOrder.TicketsLeft.ToString()
            };
        }

        private List<SegmentInfoAlerts> SetFlightInformationMessage(List<SegmentInfoAlerts> flightSegmentAlerts, SegmentInfoAlerts alert)

        {
            if (flightSegmentAlerts == null)
                flightSegmentAlerts = new List<SegmentInfoAlerts>();

            //alert.AlignLeft = flightSegmentAlerts == null || (flightSegmentAlerts.Count % 2 > 0);
            //alert.AlignRight = flightSegmentAlerts != null && flightSegmentAlerts.Count % 2 == 0;
            flightSegmentAlerts.Add(alert);

            if (flightSegmentAlerts.Count > 1)
                flightSegmentAlerts = flightSegmentAlerts.OrderBy(x => (int)Enum.Parse(typeof(SegmentInfoAlertsOrder), x.SortOrder)).ToList();

            int i = 1;
            foreach (var item in flightSegmentAlerts)
            {
                if (i % 2 > 0)
                {
                    item.AlignLeft = true;
                    item.AlignRight = false;
                }
                else
                {
                    item.AlignRight = true;
                    item.AlignLeft = false;
                }

                i++;
            }

            return flightSegmentAlerts;
        }

        private string GetMixedCabinTextForFlight(Flight flt)
        {
            //group the mixed cabin messages                
            string tempMsgs = "";
            if (flt.Products != null && flt.Products.Count > 0)
            {
                foreach (Product prod in flt.Products)
                {
                    if (!string.IsNullOrEmpty(prod.CrossCabinMessaging))
                        tempMsgs = "Mixed cabin";
                }
            }

            return tempMsgs;
        }

        private static string GetCabinNameFromColumn(string type, List<MOBSHOPShoppingProduct> columnInfo, string defaultCabin)
        {
            type = type.IsNullOrEmpty() ? string.Empty : type.ToUpper().Trim();

            string cabin = "Economy";
            if (columnInfo != null && columnInfo.Count > 0)
            {
                foreach (MOBSHOPShoppingProduct prod in columnInfo)
                {
                    if (!prod.Type.IsNullOrEmpty() && type == prod.Type.ToUpper().Trim())
                    {
                        cabin = prod.LongCabin;
                        break;
                    }
                }
            }
            else
            {
                cabin = defaultCabin;
            }
            return cabin;
        }

        private static string ReshopAirfareDisplayText(List<PricingItem> price)
        {
            bool isAddCollect = (price.Exists(p => p.PricingType == "AddCollect"))
                ? price.FirstOrDefault(p => p.PricingType == "AddCollect")?.Amount > 0 : false;

            bool isChangeFee = (price.Exists(p => p.PricingType == "ChangeFee"))
                ? price.FirstOrDefault(p => p.PricingType == "ChangeFee")?.Amount > 0 : false;

            return (isAddCollect && isChangeFee)
                ? "Price difference and change fee" : (isAddCollect) ? "Price difference"
                : (isChangeFee) ? "change fee" : string.Empty;
        }



        public string ReShopAirfareDisplayValue(CultureInfo ci, List<PricingItem> price, bool isAward = false, bool isChangeFee = false)
        {
            string displayPrice = string.Empty;
            if (price != null && price.Count > 0)
            {
                if (!isAward)
                {
                    decimal tempdisplayprice = ReshopAirfareDisplayValueInDecimal(price);

                    displayPrice = TopHelper.FormatAmountForDisplay(tempdisplayprice, ci, true);

                    if (_configuration.GetValue<bool>("EnableReshopChangeFeeElimination"))
                    {
                        if (tempdisplayprice.CompareTo(decimal.Zero) > 0)
                        {
                            displayPrice = string.Concat("+", displayPrice);
                        }
                    }
                }
                else
                    displayPrice = TopHelper.FormatAmountForDisplay(ReshopAwardAirfareDisplayValueInDecimal(price, isChangeFee), ci, false, isAward);
            }
            return string.IsNullOrEmpty(displayPrice) ? "Not available" : displayPrice;
        }


        private bool IsYoungAdultProduct(ProductCollection pc)
        {
            return pc != null && pc.Count > 0 && pc.Any(p => p.ProductSubtype.ToUpper().Equals("YOUNGADULTDISCOUNTEDFARE"));

        }

        public Product GetMatrixDisplayProduct(ProductCollection products, string fareSelected, List<MOBSHOPShoppingProduct> columnInfo, out CultureInfo ci, out bool isSelectedFareFamily, out string serviceClassDesc, out bool isMixedCabin, out int seatsRemaining, string fareClass, bool isConnectionOrStopover = false, bool isELFFareDisplayAtFSR = true)
        {
            var bestMatch = new Product();
            isSelectedFareFamily = false;
            isMixedCabin = false;
            serviceClassDesc = "";
            seatsRemaining = 0;
            const int minimumSeatsRemaining = 4;
            var isSelectedCabin = false;
            ci = null;

            var productsOrderedByPrice = products.Where(p => (p.Prices != null && p.Prices.Any())).OrderBy(p => p.Prices.First().Amount);

            foreach (var prod in productsOrderedByPrice)
            {
                if (prod.IsBundleProduct || prod.ProductId == "NAP")
                {
                    continue;
                }

                if ((isConnectionOrStopover && !string.IsNullOrEmpty(prod.ProductId)) ||
                    (prod.Prices != null && prod.Prices.Any()))
                {
                    if (!isConnectionOrStopover)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(prod.Prices[0].Currency);
                        }
                    }

                    isSelectedCabin = MatchServiceClassRequested(fareSelected, fareClass, prod.ProductType, columnInfo, isELFFareDisplayAtFSR);

                    if (isSelectedCabin)
                    {
                        bestMatch = prod;
                        isMixedCabin = !string.IsNullOrEmpty(prod.CrossCabinMessaging);
                        isSelectedFareFamily = true;
                        serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                        seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                        break;
                    }
                }

            }

            if (!isSelectedCabin)
            {
                foreach (Product prod in products)
                {
                    if (prod.IsBundleProduct || prod.ProductId == "NAP")
                    {
                        continue;
                    }

                    if ((isConnectionOrStopover && !string.IsNullOrEmpty(prod.ProductId)) || (prod.Prices != null && prod.Prices.Any()))
                    {
                        if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("FLEXIBLE"))
                        {
                            if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("BUSINESS"))
                            {
                                //if (lowestAirfare == -1M || tempProdPrice < lowestAirfare)
                                //{
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                // }
                            }
                            else if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("FIRST"))
                            {
                                //if (lowestAirfare == -1M || tempProdPrice < lowestAirfare)
                                //{
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                //}
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                // }
                            }
                        }
                        else if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("UNRESTRICTED"))
                        {
                            if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("BUSINESS"))
                            {
                                //if (lowestAirfare == -1M || tempProdPrice < lowestAirfare)
                                //{
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                // }
                            }
                            else if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("FIRST"))
                            {
                                //if (lowestAirfare == -1M || tempProdPrice < lowestAirfare)
                                //{
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                // }
                            }
                        }
                        else if (_configuration.GetValue<bool>("SwithAwardSelectedCabinMilesDisplay") && !string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("AWARD"))
                        {
                            bestMatch = prod;
                            serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                            seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                            isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                            if ((prod.ProductType.Contains("ECONOMY") && fareSelected.ToUpper().Contains("ECONOMY")) ||
                                (prod.ProductType.Contains("BUS") && (fareSelected.ToUpper().Contains("ECONOMY") || fareSelected.ToUpper().Contains("BUS"))) ||
                                (prod.ProductType.Contains("FIRST") && fareSelected.ToUpper().Contains("FIRST")))
                                break;
                        }
                        else //lowest
                        {
                            if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("BUSINESS"))
                            {
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                // }
                            }
                            else if (!string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("FIRST"))
                            {
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(fareSelected) && (prod.ProductType.Contains("ECONOMY") || prod.ProductType.ToUpper().Contains("ECO-PREMIUM")))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("BUS"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else if (!string.IsNullOrEmpty(fareSelected) && prod.ProductType.ToUpper().Contains("FIRST"))
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                else
                                {
                                    bestMatch = prod;
                                    serviceClassDesc = GetCabinNameFromColumn(prod.ProductType, columnInfo, prod.Description);
                                    seatsRemaining = prod.BookingCount <= minimumSeatsRemaining ? prod.BookingCount : 0;
                                    isMixedCabin = string.IsNullOrEmpty(prod.CrossCabinMessaging) ? false : true;
                                    //break;
                                }
                                //}
                            }
                        }
                    }
                }
            }

            return bestMatch;
        }

        public void AssignCorporateFareIndicator(Flight segment, Model.Shopping.MOBSHOPFlight flight, string travelType = "")
        {
            bool isCorporateBooking = _configuration.GetValue<bool>("CorporateConcurBooking");
            bool isCorpLeisureBooking = _configuration.GetValue<bool>("EnableCorporateLeisure") && travelType == TravelType.CLB.ToString();
            if (isCorporateBooking || isCorpLeisureBooking)
            {
                if (segment.Products.Exists(p => p.ProductSubtype != null))
                {
                    bool hasMatchCorpDisc = segment.Products.Any(p => p.ProductSubtype.Contains("CORPDISC"));
                    flight.CorporateFareIndicator = hasMatchCorpDisc ?
                                                      isCorpLeisureBooking
                                                      ? _configuration.GetValue<string>("FSRLabelForCorporateLeisure")
                                                      : _configuration.GetValue<string>("CorporateFareIndicator") ?? string.Empty
                                                   : string.Empty;
                }
            }
        }

        private bool GetAddCollectWaiverStatus(Flight flight, out string addcollectwaiver, int id, string version)
        {
            addcollectwaiver = string.Empty;

            if (flight.Products == null) return false;

            foreach (var product in flight.Products)
            {
                if (product.ProductId == "NAP")
                    continue;
                if (product.Prices != null)
                {
                    foreach (var price in product.Prices)
                    {
                        if (price.PricingDetails != null)
                        {
                            if (_shoppingUtility.IsEbulkPNRReshopEnabled(id, version))
                            {
                                if (price.PricingDetails.Exists(p => !p.DetailDescription.IsNullOrEmpty() && p.DetailDescription.Contains("-NOAC")))
                                {
                                    addcollectwaiver = product.ProductId;
                                    return true;
                                }

                                if (price.PricingDetails.Exists(p => !p.DetailDescription.IsNullOrEmpty() && p.DetailDescription.Contains("VALID FOR SAME DAY CHANGE")))
                                {
                                    var priceDetails = price.PricingDetails.First(p => p.DetailDescription.Contains("VALID FOR SAME DAY CHANGE"));
                                    if (priceDetails != null)
                                    {
                                        if (priceDetails.PriceType == "AddCollect" && priceDetails.PriceSubtype == "Waiver")
                                        {
                                            addcollectwaiver = product.ProductId;
                                            return true;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (price.PricingDetails.Exists(p => p.DetailDescription.Contains("-NOAC")))
                                {
                                    addcollectwaiver = product.ProductId;
                                    return true;
                                }

                                if (price.PricingDetails.Exists(p => p.DetailDescription.Contains("VALID FOR SAME DAY CHANGE")))
                                {
                                    var priceDetails = price.PricingDetails.First(p => p.DetailDescription.Contains("VALID FOR SAME DAY CHANGE"));
                                    if (priceDetails != null)
                                    {
                                        if (priceDetails.PriceType == "AddCollect" && priceDetails.PriceSubtype == "Waiver")
                                        {
                                            addcollectwaiver = product.ProductId;
                                            return true;
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            }
            return false;
        }

        private List<MOBSearchFilterItem> GetDefaultFsrRedesignFooterSortTypes()
        {
            var sortTypes = new List<MOBSearchFilterItem>();
            List<string> sortTypesList = _configuration.GetValue<string>("FsrRedesignSearchFiletersSortTypes").ToString().Split('|').ToList();
            foreach (string sortType in sortTypesList)
            {
                var item = new MOBSearchFilterItem();
                item.Key = sortType.Split('~')[0].ToString();
                item.Value = sortType.Split('~')[1].ToString();
                item.DisplayValue = sortType.Split('~')[1].ToString();
                sortTypes.Add(item);
            }
            return sortTypes;
        }

        private async Task<MOBSearchFilters> GetSearchFilters(SearchFilterInfo filters, CultureInfo ci, int appId, string appVersion, string requestedCabin, ColumnInformation columnInfo, bool isStandardRevenueSearch, bool isAward = false, bool mixedCabinFlightExists = false, List<CMSContentMessage> lstMessages = null, Session session = null, MOBSearchFilters searchFilters = null, HttpContext httpContext = null)
        {
            var filter = new MOBSearchFilters();

            if (filters != null)
            {
                if (!String.IsNullOrEmpty(filters.AircraftTypes))
                {
                    filter.AircraftTypes = filters.AircraftTypes;
                }

                if (!String.IsNullOrEmpty(filters.AirportsDestination))
                {
                    filter.AirportsDestination = processNearbyAirports(filters.AirportsDestination);
                }

                if (filters.AirportsDestinationList != null && filters.AirportsDestinationList.Count > 0)
                {
                    filter.AirportsDestinationList = new List<MOBSearchFilterItem>();
                    foreach (CodeDescPair cdp in filters.AirportsDestinationList)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(cdp.Currency);
                        }

                        var kvp = new MOBSearchFilterItem();
                        kvp.Key = cdp.Code;
                        kvp.Value = cdp.Description;
                        kvp.DisplayValue = cdp.Description + " (" + cdp.Code + ")";
                        kvp.Currency = cdp.Currency;
                        if (isAward)
                            kvp.Amount = ShopStaticUtility.FormatAwardAmountForDisplay(cdp.Amount, true);
                        else
                            kvp.Amount = TopHelper.FormatAmountForDisplay(cdp.Amount, ci);

                        kvp.IsSelected = true;
                        filter.AirportsDestinationList.Add(kvp);
                    }
                    filter.AirportsDestinationList = processNearbyAirports(filter.AirportsDestinationList);
                }

                if (!String.IsNullOrEmpty(filters.AirportsOrigin))
                {
                    filter.AirportsOrigin = processNearbyAirports(filters.AirportsOrigin);
                }

                if (filters.AirportsOriginList != null && filters.AirportsOriginList.Count > 0)
                {
                    filter.AirportsOriginList = new List<MOBSearchFilterItem>();
                    foreach (CodeDescPair cdp in filters.AirportsOriginList)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(cdp.Currency);
                        }

                        var kvp = new MOBSearchFilterItem();
                        kvp.Key = cdp.Code;
                        kvp.Value = cdp.Description;
                        kvp.DisplayValue = cdp.Description + " (" + cdp.Code + ")";
                        kvp.Currency = cdp.Currency;
                        if (isAward)
                            kvp.Amount = ShopStaticUtility.FormatAwardAmountForDisplay(cdp.Amount, true);
                        else
                            kvp.Amount = TopHelper.FormatAmountForDisplay(cdp.Amount, ci);
                        kvp.IsSelected = true;
                        filter.AirportsOriginList.Add(kvp);
                    }
                    filter.AirportsOriginList = processNearbyAirports(filter.AirportsOriginList);
                }

                if (!String.IsNullOrEmpty(filters.AirportsStop))
                {
                    filter.AirportsStop = filters.AirportsStop;
                }

                if (filters.AirportsStopList != null && filters.AirportsStopList.Count > 0)
                {
                    filter.AirportsStopList = new List<MOBSearchFilterItem>();
                    var sortedByAirportNameList = filters.AirportsStopList.OrderBy(x => x.Description);
                    foreach (CodeDescPair cdp in sortedByAirportNameList)
                    {
                        var kvp = new MOBSearchFilterItem();
                        kvp.Key = cdp.Code;
                        kvp.Value = cdp.Description;
                        kvp.DisplayValue = cdp.Description + " (" + cdp.Code + ")";
                        kvp.Currency = cdp.Currency;
                        kvp.Amount = "";
                        kvp.IsSelected = true;
                        filter.AirportsStopList.Add(kvp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.AirportsStopToAvoid))
                {
                    filter.AirportsStopToAvoid = filters.AirportsStopToAvoid;
                }

                if (filters.AirportsStopToAvoidList != null && filters.AirportsStopToAvoidList.Count > 0)
                {
                    filter.AirportsStopToAvoidList = new List<MOBSearchFilterItem>();
                    foreach (CodeDescPair cdp in filters.AirportsStopToAvoidList)
                    {
                        var kvp = new MOBSearchFilterItem();
                        kvp.Key = cdp.Code;
                        kvp.Value = cdp.Description;
                        kvp.DisplayValue = cdp.Description + " (" + cdp.Code + ")";
                        kvp.Currency = cdp.Currency;
                        kvp.Amount = "";
                        kvp.IsSelected = true;
                        filter.AirportsStopToAvoidList.Add(kvp);
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
                    filter.CarriersMarketingList = new List<MOBSearchFilterItem>();
                    foreach (CodeDescPair cdp in filters.CarriersMarketingList)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(cdp.Currency);
                        }

                        var kvp = new MOBSearchFilterItem();
                        kvp.Key = cdp.Code;
                        kvp.Value = cdp.Description;
                        kvp.DisplayValue = cdp.Description;
                        kvp.Currency = cdp.Currency;
                        kvp.Amount = TopHelper.FormatAmountForDisplay(cdp.Amount, ci);
                        kvp.IsSelected = true;
                        filter.CarriersMarketingList.Add(kvp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.CarriersOperating))
                {
                    filter.CarriersOperating = filters.CarriersOperating;
                }

                if (filters.CarriersOperatingList != null && filters.CarriersOperatingList.Count > 0)
                {
                    filter.CarriersOperatingList = new List<MOBSearchFilterItem>();
                    foreach (CodeDescPair cdp in filters.CarriersOperatingList)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(cdp.Currency);
                        }

                        var kvp = new MOBSearchFilterItem();
                        kvp.Key = cdp.Code;
                        kvp.Value = cdp.Description;
                        kvp.DisplayValue = cdp.Description;
                        kvp.Currency = cdp.Currency;
                        kvp.Amount = TopHelper.FormatAmountForDisplay(cdp.Amount, ci);
                        kvp.IsSelected = true;
                        filter.CarriersOperatingList.Add(kvp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.EquipmentCodes))
                {
                    filter.EquipmentCodes = filters.EquipmentCodes;
                }

                if (filters.EquipmentList != null && filters.EquipmentList.Count > 0)
                {
                    filter.EquipmentList = new List<MOBSearchFilterItem>();
                    foreach (CodeDescPair cdp in filters.EquipmentList)
                    {
                        if (ci == null)
                        {
                            ci = TopHelper.GetCultureInfo(cdp.Currency);
                        }

                        var kvp = new MOBSearchFilterItem();
                        kvp.Key = cdp.Code;
                        kvp.Value = cdp.Description;
                        kvp.DisplayValue = cdp.Description;
                        kvp.Currency = cdp.Currency;
                        kvp.Amount = TopHelper.FormatAmountForDisplay(cdp.Amount, ci);

                        filter.EquipmentList.Add(kvp);
                    }
                }

                if (!String.IsNullOrEmpty(filters.EquipmentTypes))
                {
                    filter.EquipmentTypes = filters.EquipmentTypes;
                }

                if (filters.FareFamilies != null && filters.FareFamilies.fareFamily != null && filters.FareFamilies.fareFamily.Count() > 0)
                {
                    filter.FareFamilies = new List<MOBSHOPFareFamily>();
                    int cnt = 0;
                    foreach (fareFamilyType ff in filters.FareFamilies.fareFamily)
                    {

                        var fft = new MOBSHOPFareFamily();
                        fft.FareFamily = string.IsNullOrEmpty(ff.fareFamily) ? "" : ff.fareFamily;
                        fft.MaxMileage = ff.maxMileage;
                        if (ff.maxPrice != null)
                        {
                            fft.MaxPrice = ff.maxPrice.amount.ToString();
                        }
                        fft.MinMileage = ff.minMileage;
                        fft.MinPrice = ff.minPrice == null ? "" : ff.minPrice.amount.ToString();
                        fft.MinPriceInSummary = ff.minPriceInSummary;
                        filter.FareFamilies.Add(fft);
                        cnt++;
                    }
                }

                if (!String.IsNullOrEmpty(filters.FareFamily))
                {
                    filter.FareFamily = filters.FareFamily;
                }
                if (_configuration.GetValue<bool>("EnableFixMobile14343"))
                {
                    if (!String.IsNullOrEmpty(filters.MaxArrivalDate))
                    {
                        filter.MaxArrivalDate = filters.MaxArrivalDate;
                    }

                    if (!String.IsNullOrEmpty(filters.MinArrivalDate))
                    {
                        filter.MinArrivalDate = filters.MinArrivalDate;
                    }
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

                #region
                if (filters.Warnings != null && filters.Warnings.Count > 0)
                {
                    filter.WarningsFilter = new List<MOBSearchFilterItem>();
                    foreach (string warning in filters.Warnings)
                    {
                        #region // As per stogo only the below 4 types are show to client for Advisories
                        List<string> warningsList = _configuration.GetValue<string>("SearchFiletersWarnings").Split('|').ToList();
                        foreach (string warningType in warningsList)
                        {
                            if (warning.ToUpper().Trim() == warningType.Split('~')[0].ToString().ToUpper().Trim())
                            {
                                var item = new MOBSearchFilterItem();
                                item.Key = warningType.Split('~')[1].ToString();
                                item.Value = warningType.Split('~')[2].ToString();
                                item.DisplayValue = warningType.Split('~')[2].ToString();
                                item.IsSelected = true;
                                filter.WarningsFilter.Add(item);
                                break;
                            }
                        }
                        #endregion
                    }
                }
                #endregion
                //Value types - have to have a value.
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
                if (filters.PriceMin == -1)
                    filters.PriceMin = 0;
                filter.PriceMin = filters.PriceMin;

                if (isAward)
                {
                    filter.PriceMaxDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(filters.PriceMax.ToString(), true);
                    filter.PriceMinDisplayValue = ShopStaticUtility.FormatAwardAmountForDisplay(filters.PriceMin.ToString(), true);
                }
                else
                {
                    filter.PriceMaxDisplayValue = TopHelper.FormatAmountForDisplay(filters.PriceMax, ci, true);
                    filter.PriceMinDisplayValue = TopHelper.FormatAmountForDisplay((int)filters.PriceMin, ci, true);
                }

                filter.ShowPriceFilters = _configuration.GetValue<bool>("ShowPriceFilter");
                filter.ShowArrivalFilters = _configuration.GetValue<bool>("ShowArrivalFilters");
                filter.ShowDepartureFilters = _configuration.GetValue<bool>("ShowDepartureFilters");
                filter.ShowDurationFilters = _configuration.GetValue<bool>("ShowDurationFilters");
                filter.ShowLayOverFilters = _configuration.GetValue<bool>("ShowLayOverFilters");
                filter.ShowSortingandFilters = _configuration.GetValue<bool>("ShowSortingandFilters");

                filter.StopCountExcl = filters.StopCountExcl;
                filter.StopCountMax = filters.StopCountMax;
                filter.StopCountMin = filters.StopCountMin;
                #region
                filter.NumberofStops = new List<MOBSearchFilterItem>();
                if (filter.StopCountMin == 0 || filter.StopCountMax == 0)
                {
                    var item = new MOBSearchFilterItem();
                    item.IsSelected = true;
                    item.Key = StopTypes.NonStop.ToString();
                    item.Value = "0,0";
                    item.DisplayValue = "Nonstop";
                    filter.NumberofStops.Add(item);
                }
                if ((filter.StopCountMin == 1 || filter.StopCountMax == 1) || ((filter.StopCountMin == 0 && filter.StopCountMax >= 2)))
                {
                    var item = new MOBSearchFilterItem();
                    item.IsSelected = true;
                    item.Key = StopTypes.OneStop.ToString();
                    item.Value = "1,1";
                    item.DisplayValue = "1+ stops";
                    filter.NumberofStops.Add(item);
                }
                if (filter.StopCountMin >= 2 || filter.StopCountMax >= 2)
                {
                    var item = new MOBSearchFilterItem();
                    item.IsSelected = true;
                    item.Key = StopTypes.MoreStops.ToString();
                    item.Value = filter.StopCountMax.ToString() + "," + filter.StopCountMax.ToString();
                    item.DisplayValue = "2+ stops";
                    filter.NumberofStops.Add(item);
                }
                #endregion
                // Refundable fares toggle feature
                if (IsEnableRefundableFaresToggle(appId, appVersion) && isStandardRevenueSearch)
                {
                    string requestedFareFamily = GetFareFamily(requestedCabin, "");

                    // Looks if the toggle should be displayed
                    if ((columnInfo?.Columns?.Count ?? 0) > 0)
                    {
                        filter.ShowRefundableFaresToggle = columnInfo.Columns.FirstOrDefault(col => col.IsFullyRefundable = true) != null;
                    }

                    // Looks for the refundable toggle fare
                    if (filter.ShowRefundableFaresToggle && (filters.FareFamilies?.fareFamily?.Length ?? 0) > 0)
                    {
                        decimal minRefundablePrice = 0;
                        minRefundablePrice = filters.FareFamilies.fareFamily.FirstOrDefault(fam => fam.fareFamily.Contains("UNRESTRICTED") && fam.fareFamily.Contains(requestedFareFamily))?.minPriceValue ?? 0;
                        filter.RefundableFaresToggle = new MOBSearchFilterItem
                        {
                            Key = "RefundableFares",
                            Value = _configuration.GetValue<string>("RefundableFaresToggleValue"),
                            DisplayValue = (minRefundablePrice > 0 ?
                                                $"{_configuration.GetValue<string>("RefundableFaresToggleDisplayWithAmt")}{string.Format("{0:0}", minRefundablePrice)}" :
                                                _configuration.GetValue<string>("RefundableFaresToggleDisplay")),
                            IsSelected = filters.ShopIndicators?.IsRefundableSelected ?? false
                        };
                    }
                }

                // Mixed Cabin toggle feature
                if (_shoppingUtility.IsMixedCabinFilerEnabled(appId, appVersion) && isAward && mixedCabinFlightExists)
                {
                    // Looks for the MixedCabin toggle 
                    if (filter.AdditionalToggles == null)
                    {
                        filter.AdditionalToggles = new List<MOBSearchFilterItem>();
                    }
                    if (filter.AdditionalToggles.Any(k => k.Key == _configuration.GetValue<string>("MixedCabinToggleKey")))
                    {
                        filter.AdditionalToggles.FirstOrDefault(k => k.Key == _configuration.GetValue<string>("MixedCabinToggleKey")).IsSelected = filters.ShopIndicators?.IsMixedToggleSelected ?? false;
                    }
                    else
                    {
                        filter.AdditionalToggles.Add(new MOBSearchFilterItem
                        {
                            Key = _configuration.GetValue<string>("MixedCabinToggleKey"),
                            Value = _configuration.GetValue<string>("MixedCabinToggle"),
                            DisplayValue = _configuration.GetValue<string>("MixedCabinToggleDisplay"),
                            IsSelected = filters.ShopIndicators?.IsMixedToggleSelected ?? mixedCabinFlightExists
                        });
                    }
                }
            }
            if (await _featureToggles.IsEnableWheelchairFilterOnFSR(appId, appVersion, session.CatalogItems).ConfigureAwait(false) || (session?.IsReshopChange ?? false && await _featureToggles.IsEnableReshopWheelchairFilterOnFSR(appId, appVersion, session.CatalogItems).ConfigureAwait(false)))
            {
                try
                {
                    if (searchFilters != null && searchFilters.WheelchairFilterContent != null && searchFilters.WheelchairFilter != null)
                    {
                        filter.WheelchairFilter = searchFilters.WheelchairFilter;
                        filter.WheelchairFilterContent = searchFilters.WheelchairFilterContent;
                    }
                    else
                    {
                        filter.WheelchairFilter = new List<MOBSearchFilterItem>
                        {
                            new MOBSearchFilterItem()
                            {
                                Key = "WheelChairKey",
                                IsSelected =false
                            }
                        };
                        if (lstMessages != null && lstMessages.Count >0)
                        {
                            filter.WheelchairFilter[0].DisplayValue = _shoppingUtility.GetSDLMessageFromList(lstMessages, "WheelchairFilter_FSR_ContentMsgs").FirstOrDefault()?.HeadLine ?? "I'm checking a wheelchair";
                        }
                        filter.WheelchairFilterContent = new WheelChairSizerInfo();
                        filter.WheelchairFilterContent.ImageUrl1 = _shoppingUtility.GetFormatedUrl(_httpContext.Request.Host.Value,
                                                                     _httpContext.Request.Scheme, _configuration.GetValue<string>("WheelChairImageUrl"), true);
                        filter.WheelchairFilterContent.ImageUrl2 = _shoppingUtility.GetFormatedUrl(_httpContext.Request.Host.Value,
                            _httpContext.Request.Scheme, _configuration.GetValue<string>("WheelChairFoldedImageUrl"), true);
                        _shoppingUtility.BuildWheelChairFilterContent(filter.WheelchairFilterContent, lstMessages);
                        await _sessionHelperService.SaveSession<MOBSearchFilters>(filter, session.SessionId, new List<string> { session.SessionId, filter.GetType().FullName }, filter.GetType().FullName).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.ILoggerError("WheelChairFilter building error {@Exception},SessionId-{sessionId}", JsonConvert.SerializeObject(ex), session.SessionId);
                }
            }
            filter = _shoppingUtility.SetSearchFiltersOutDefaults(filter);
            return filter;
        }

        private List<MOBSearchFilterItem> processNearbyAirports(List<MOBSearchFilterItem> airportList)
        {
            var newAirportList = new List<MOBSearchFilterItem>();

            Dictionary<string, decimal> minPrice = new Dictionary<string, decimal>();
            Dictionary<string, MOBSearchFilterItem> minPriceItem = new Dictionary<string, MOBSearchFilterItem>();

            foreach (MOBSearchFilterItem item in airportList)
            {
                string key = item.Key.Trim().ToUpper();

                decimal temp = decimal.MaxValue;
                decimal.TryParse(item.Amount, out temp);

                if (minPrice.ContainsKey(key))
                {
                    if (minPrice[key] < temp)
                    {
                        minPrice[key] = temp;
                        minPriceItem[key] = item;
                    }
                }
                else
                {
                    minPrice.Add(key, temp);
                    minPriceItem.Add(key, item);
                }
            }

            newAirportList = minPriceItem.Values.ToList<MOBSearchFilterItem>();

            return newAirportList.Count > 1 ? newAirportList : null;
        }

        private string processNearbyAirports(string airportList)
        {
            HashSet<string> airports = new HashSet<string>(airportList.Trim().Split(",".ToCharArray()));

            string newAirportList = string.Empty;
            foreach (string s in airports)
            {
                if (string.IsNullOrEmpty(newAirportList))
                    newAirportList = s;
                else
                    newAirportList += "," + s;
            }

            return newAirportList;
        }

        private async Task<List<Model.Shopping.MOBSHOPFlattenedFlight>> GetFlattendFlights(List<Model.Shopping.MOBSHOPFlight> flights, string tripId, string productId, string tripDate, MOBSHOPShopRequest shopRequest = null, int tripIndex = -1, MOBAdditionalToggle mOBAdditionalToggle = null, MOBAdditionalItems additionalItems = null, List<CMSContentMessage> lstMessages = null, bool isReshop = false)
        {
            #region
            List<Model.Shopping.MOBSHOPFlattenedFlight> flattendFlights = new List<Model.Shopping.MOBSHOPFlattenedFlight>();
            foreach (var flight in flights)
            {
                #region
                Model.Shopping.MOBSHOPFlattenedFlight flattenedFlight = new Model.Shopping.MOBSHOPFlattenedFlight();

                bool isUADiscount = false;

                if (flight.ShoppingProducts != null)
                {
                    foreach (var product in flight.ShoppingProducts)
                    {
                        if (_shoppingUtility.EnableIBEFull() && !flattenedFlight.IsIBE && product.IsIBE)
                        {
                            flattenedFlight.IsIBE = true;
                        }

                        if (product.IsUADiscount)
                        {
                            isUADiscount = product.IsUADiscount;
                            break;
                        }

                        if (_shoppingUtility.EnableIBELite() && !flattenedFlight.IsIBELite && product.IsIBELite) // set only once
                        {
                            flattenedFlight.IsIBELite = true;
                        }
                    }
                }

                if (_shoppingUtility.EnableIBELite() || _shoppingUtility.EnableIBEFull())
                {
                    flattenedFlight.FlightHash = flight.FlightHash;
                }
                if (shopRequest != null)
                {
                    if (_shoppingUtility.EnableFSRLabelTexts(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                    {

                        if (flattenedFlight.FlightLabelTextList == null)
                            flattenedFlight.FlightLabelTextList = new List<string>();

                        if (!_shoppingUtility.CheckFSRRedesignFromShop(shopRequest))
                        {
                            if (_configuration.GetValue<bool>("ChangeFeeWaiverMessagingToggle"))
                            {
                                if (!flattenedFlight.IsChangeFeeWaiver && tripIndex == 0)
                                {
                                    if (!string.IsNullOrEmpty(_configuration.GetValue<string>("LabelTextChangeFeeWaiver")))
                                    {
                                        flattenedFlight.FlightLabelTextList.Add(_configuration.GetValue<string>("LabelTextChangeFeeWaiver"));
                                        flattenedFlight.IsChangeFeeWaiver = true;
                                    }
                                }
                            }
                        }
                        if (_shoppingUtility.EnableCovidTestFlightShopping(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                        {
                            if (!flattenedFlight.IsCovidTestFlight && (flight.IsCovidTestFlight))
                            //if (!flattenedFlight.IsCovidTestFlight)
                            {
                                if (_shoppingUtility.CheckFSRRedesignFromShop(shopRequest))
                                {
                                    flattenedFlight.FlightBadges = setFlightBadgeInformation(flattenedFlight.FlightBadges, CovidTestBadge());
                                }
                                else
                                {
                                    flattenedFlight.FlightLabelTextList.Add(_configuration.GetValue<string>("LabelTextCovidTest"));
                                }
                                flattenedFlight.IsCovidTestFlight = true;
                            }
                        }



                    }
                }
                flattenedFlight.IsUADiscount = isUADiscount;

                flattenedFlight.TripId = flight.TripId;
                flattenedFlight.FlightId = flight.FlightId;
                flattenedFlight.ProductId = productId;
                flattenedFlight.Flights = new List<Model.Shopping.MOBSHOPFlight>();
                flight.TripId = tripId; // trip.TripId;
                flight.FlightDepartureDays = string.Empty;
                flight.FlightArrivalDays = GetDayDifference(flight.DepartureDateTime, flight.ArrivalDateTime);
                bool flightDateChanged = false;
                flight.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDate, flight.ArrivalDateTime, ref flightDateChanged);
                // Added by Madhavi on 22/Sep/2015
                flight.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDate, flight.DepartureDateTime, ref flightDateChanged);
                flight.FlightDateChanged = flightDateChanged;
                flattenedFlight.Flights.Add(flight);
                if (_configuration.GetValue<bool>("EnableChangeOfAirport"))
                {
                    flattenedFlight.AirportChange = flight.AirportChange;
                }

                if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                {
                    // Make a copy of flight.Connections and release flight.Connections
                    List<Model.Shopping.MOBSHOPFlight> connections = flight.StopInfos.Clone();
                    flight.StopInfos = null;
                    int cnt = 0;
                    foreach (var connection in connections)
                    {
                        if (cnt == 0)
                        {
                            connection.FlightDepartureDays = GetDayDifference(flight.ArrivalDateTime,
                                connection.DepartureDateTime);
                        }
                        else
                        {
                            connection.FlightDepartureDays = GetDayDifference(connections[cnt - 1].ArrivalDateTime,
                                connection.DepartureDateTime);
                        }
                        connection.IsStopOver = true;
                        connection.FlightArrivalDays = GetDayDifference(tripDate, connection.ArrivalDateTime);
                        connection.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDate,
                            connection.ArrivalDateTime, ref flightDateChanged);
                        connection.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDate,
                            connection.DepartureDateTime, ref flightDateChanged);
                        connection.FlightDateChanged = flightDateChanged;
                        connection.TripId = tripId; //trip.TripId;

                        if (shopRequest != null)
                        {
                            if (_shoppingUtility.EnableFSRLabelTexts(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                            {

                                if (flattenedFlight.FlightLabelTextList == null)
                                    flattenedFlight.FlightLabelTextList = new List<string>();
                                if (_shoppingUtility.EnableCovidTestFlightShopping(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                                {
                                    if (!flattenedFlight.IsCovidTestFlight && (connection.IsCovidTestFlight))
                                    {
                                        if (_shoppingUtility.CheckFSRRedesignFromShop(shopRequest))
                                        {
                                            flattenedFlight.FlightBadges = setFlightBadgeInformation(flattenedFlight.FlightBadges, CovidTestBadge());
                                        }
                                        else
                                        {
                                            flattenedFlight.FlightLabelTextList.Add(_configuration.GetValue<string>("LabelTextCovidTest"));
                                        }
                                        flattenedFlight.IsCovidTestFlight = true;
                                    }
                                }
                            }


                        }

                        flattenedFlight.Flights.Add(connection);
                        cnt++;
                    }
                }

                if (flight.Connections != null && flight.Connections.Count > 0)
                {
                    // Make a copy of flight.Connections and release flight.Connections
                    var connections = flight.Connections.Clone();
                    flight.Connections = null;
                    int cnt = 0;
                    foreach (var connection in connections)
                    {
                        if (cnt == 0)
                        {
                            connection.FlightDepartureDays = GetDayDifference(flight.ArrivalDateTime,
                                connection.DepartureDateTime);
                        }
                        else
                        {
                            connection.FlightDepartureDays = GetDayDifference(connections[cnt - 1].ArrivalDateTime,
                                connection.DepartureDateTime);
                        }
                        connection.FlightArrivalDays = GetDayDifference(tripDate, connection.ArrivalDateTime);
                        connection.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDate,
                            connection.ArrivalDateTime, ref flightDateChanged);
                        connection.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDate,
                            connection.DepartureDateTime, ref flightDateChanged);
                        connection.FlightDateChanged = flightDateChanged;
                        connection.TripId = tripId; //trip.TripId;

                        if (shopRequest != null)
                        {
                            if (_shoppingUtility.EnableFSRLabelTexts(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                            {

                                if (flattenedFlight.FlightLabelTextList == null)
                                    flattenedFlight.FlightLabelTextList = new List<string>();

                                if (_shoppingUtility.EnableCovidTestFlightShopping(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                                {
                                    if (!flattenedFlight.IsCovidTestFlight && (connection.IsCovidTestFlight))
                                    {
                                        if (_shoppingUtility.CheckFSRRedesignFromShop(shopRequest))
                                        {
                                            flattenedFlight.FlightBadges = setFlightBadgeInformation(flattenedFlight.FlightBadges, CovidTestBadge());
                                        }
                                        else
                                        {
                                            flattenedFlight.FlightLabelTextList.Add(_configuration.GetValue<string>("LabelTextCovidTest"));
                                        }
                                        flattenedFlight.IsCovidTestFlight = true;
                                    }
                                }
                            }
                        }
                        flattenedFlight.Flights.Add(connection);
                        if (connection.StopInfos != null && connection.StopInfos.Count > 0)
                        {
                            // Make a copy of flight.Connections and release flight.Connections
                            List<Model.Shopping.MOBSHOPFlight> connectionStops = connection.StopInfos.Clone();
                            connection.StopInfos = null;
                            int csnt = 0;
                            foreach (var conn in connectionStops)
                            {
                                if (csnt == 0)
                                {
                                    conn.FlightDepartureDays = GetDayDifference(connection.ArrivalDateTime,
                                        conn.DepartureDateTime);
                                }
                                else
                                {
                                    conn.FlightDepartureDays =
                                        GetDayDifference(connectionStops[csnt - 1].ArrivalDateTime,
                                            conn.DepartureDateTime);
                                }
                                conn.IsStopOver = true;
                                conn.FlightArrivalDays = GetDayDifference(tripDate, conn.ArrivalDateTime);
                                conn.NextDayFlightArrDate = GetRedEyeFlightArrDate(tripDate, conn.ArrivalDateTime,
                                    ref flightDateChanged);
                                conn.RedEyeFlightDepDate = GetRedEyeDepartureDate(tripDate, conn.DepartureDateTime,
                                    ref flightDateChanged);
                                conn.FlightDateChanged = flightDateChanged;
                                conn.TripId = tripId; //trip.TripId;

                                if (shopRequest != null)
                                {
                                    if (_shoppingUtility.EnableFSRLabelTexts(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                                    {

                                        if (flattenedFlight.FlightLabelTextList == null)
                                            flattenedFlight.FlightLabelTextList = new List<string>();

                                        if (_shoppingUtility.EnableCovidTestFlightShopping(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.IsReshop || shopRequest.IsReshopChange))
                                        {
                                            if (!flattenedFlight.IsCovidTestFlight && (conn.IsCovidTestFlight))
                                            {
                                                if (_shoppingUtility.CheckFSRRedesignFromShop(shopRequest))
                                                {
                                                    flattenedFlight.FlightBadges = setFlightBadgeInformation(flattenedFlight.FlightBadges, CovidTestBadge());
                                                }
                                                else
                                                {
                                                    flattenedFlight.FlightLabelTextList.Add(_configuration.GetValue<string>("LabelTextCovidTest"));
                                                }
                                                flattenedFlight.IsCovidTestFlight = true;
                                            }
                                        }

                                    }
                                }

                                flattenedFlight.Flights.Add(conn);
                                csnt++;
                            }
                        }
                        cnt++;
                    }
                }

                if (flight.ShoppingProducts != null && flight.ShoppingProducts.Count > 0)
                {
                    int idx = 0;

                    foreach (Model.Shopping.MOBSHOPShoppingProduct prod in flight.ShoppingProducts)
                    {
                        if (prod.IsMixedCabin)
                        {
                            if (_configuration.GetValue<bool>("EnableAwardMixedCabinFiter"))
                            {
                                if (mOBAdditionalToggle != null && mOBAdditionalToggle.MixedCabinFlightExists == false)
                                {
                                    mOBAdditionalToggle.MixedCabinFlightExists = true;
                                }
                            }

                            prod.MixedCabinSegmentMessages = GetFlightMixedCabinSegments(flattenedFlight.Flights,
                                idx);
                            prod.IsSelectedCabin = GetSelectedCabinInMixedScenario(flattenedFlight.Flights, idx);

                            prod.ProductDetail.ProductCabinMessages =
                                GetProductDetailMixedCabinSegments(flattenedFlight.Flights, idx);
                        }

                        if (!string.IsNullOrEmpty(flight.AddCollectProductId))
                        {
                            if (prod.ProductId == flight.AddCollectProductId && prod.IsSelectedCabin)
                            {
                                flattenedFlight.isAddCollectWaived = flight.isAddCollectWaived;
                                flattenedFlight.AddCollectProductId = flight.AddCollectProductId;
                            }
                        }

                        if (_configuration.GetValue<bool>("EnableAdvanceSearchOfferCode") && prod.PriceAmount == 0)
                        {
                            prod.PriceStyle = new MOBStyledText
                            {
                                TextColor = "#1B7742"
                            };
                        }

                        if (!string.IsNullOrEmpty(prod?.Type) && (prod.Type.Contains("PROMOTION") && prod.PriceAmount == 0))
                        {
                            prod.PriceStyle = new MOBStyledText
                            {
                                TextColor = "#1B7742"
                            };

                        }
                        idx++;
                    }
                }

                flattenedFlight.TripDays = GetDayDifference(flattenedFlight.Flights[0].DepartDate, flattenedFlight.Flights[flattenedFlight.Flights.Count - 1].DestinationDate);
                bool isNotCrashFix = _configuration.GetValue<bool>("ByPassBug106828Fix");

                if (_configuration.GetValue<bool>("EnableOntimePerformance21FFix"))
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

                                if (additionalItems?.AirlineCodes != null && additionalItems?.AirlineCodes.Count > 0)
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
                await SetFlightBadgesForAwardStrikeThroughPricing(flight.StrikeThroughDisplayType, flattenedFlight).ConfigureAwait(false);

                try
                {
                    if ((await _featureToggles.IsEnableWheelchairFilterOnFSR(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.CatalogItems).ConfigureAwait(false)
                        || (isReshop && await _featureToggles.IsEnableReshopWheelchairFilterOnFSR(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.CatalogItems).ConfigureAwait(false)))
                        && flight.IsWheelChairFits == true)
                    {
                        flattenedFlight.FlightBadges = setFlightBadgeInformation(flattenedFlight.FlightBadges, WheelChairFitsBadge(lstMessages));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError("WheelChairFilter-Flight badge setting error-{@Exception},SessionId-{sessionId}", JsonConvert.SerializeObject(ex), shopRequest.SessionId);
                }
                if (isNotCrashFix)
                {
                    flattendFlights.Add(flattenedFlight);
                }
                else
                {
                    GetFlattenedFlightsWithPrices(flattenedFlight, flattendFlights);
                }

                #endregion
            }

            return flattendFlights;
            #endregion
        }

        private async Task SetFlightBadgesForAwardStrikeThroughPricing(string strikeThroughDisplayType, MOBSHOPFlattenedFlight flattenedFlight)
        {
            if (await _featureSettings.GetFeatureSettingValue("EnableAwardStrikeThroughPriceEnhancement").ConfigureAwait(false)
                 && !string.IsNullOrEmpty(strikeThroughDisplayType))
            {
                // var displayType = flattenedFlight?.Flights?.FirstOrDefault()?.ShoppingProducts?.FirstOrDefault().StrikeThroughDisplayType;
                if (strikeThroughDisplayType == "PE" || strikeThroughDisplayType == "EL102" || strikeThroughDisplayType == "EL103" || strikeThroughDisplayType == "EL104" || strikeThroughDisplayType == "EL105")
                {
                    flattenedFlight.FlightBadges = setFlightBadgeInformation(flattenedFlight.FlightBadges, PremierSavingsBadge());
                }
                else if (strikeThroughDisplayType == "CH" || strikeThroughDisplayType == "EL101")
                {
                    flattenedFlight.FlightBadges = setFlightBadgeInformation(flattenedFlight.FlightBadges, ChaseCardMemberBadge());
                }
                else
                {
                    flattenedFlight.FlightBadges = setFlightBadgeInformation(flattenedFlight.FlightBadges, MemberSavingsBadge());
                }
            }
        }

        private List<Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage> GetProductDetailMixedCabinSegments(List<Model.Shopping.MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            List<Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage> tempMsgs = new List<Model.Shopping.MOBSHOPShoppingProductDetailCabinMessage>();
            foreach (Model.Shopping.MOBSHOPFlight flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages != null && flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages.Count > 0)
                        tempMsgs.AddRange(flt.ShoppingProducts[index].ProductDetail.ProductCabinMessages);
                }
            }
            return tempMsgs;
        }

        private void GetFlattenedFlightsWithPrices(Model.Shopping.MOBSHOPFlattenedFlight flattenedFlight, List<Model.Shopping.MOBSHOPFlattenedFlight> flattendFlights)
        {
            bool isAddFlightToFlattenedFlights = flattenedFlight.Flights.All(flight => !flattenedFlight.Flights[0].AirfareDisplayValue.IsNullOrEmpty());

            if (isAddFlightToFlattenedFlights)
            {
                flattendFlights.Add(flattenedFlight);
            }
        }

        private bool GetSelectedCabinInMixedScenario(List<Model.Shopping.MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            bool selected = false;
            foreach (Model.Shopping.MOBSHOPFlight flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].IsSelectedCabin)
                        selected = flt.ShoppingProducts[index].IsSelectedCabin;
                }
            }

            return selected;
        }

        private List<string> GetFlightMixedCabinSegments(List<Model.Shopping.MOBSHOPFlight> flights, int index)
        {
            //group the mixed cabin messages                
            List<string> tempMsgs = new List<string>();
            foreach (Model.Shopping.MOBSHOPFlight flt in flights)
            {
                if (flt.ShoppingProducts != null && flt.ShoppingProducts.Count > index)
                {
                    if (flt.ShoppingProducts[index].MixedCabinSegmentMessages != null && flt.ShoppingProducts[index].MixedCabinSegmentMessages.Count > 0)
                        tempMsgs.AddRange(flt.ShoppingProducts[index].MixedCabinSegmentMessages);
                }
            }

            return tempMsgs;
        }


        private List<Model.Shopping.MOBStyledText> setFlightBadgeInformation(List<Model.Shopping.MOBStyledText> badges, Model.Shopping.MOBStyledText badge)
        {
            if (badges == null)
                badges = new List<Model.Shopping.MOBStyledText>();
            if (badges?.Where(a => a.Text.Trim() == badge.Text.Trim())?.Any() == false)
                badges.Add(badge);

            if (badges.Count > 1)
            {
                badges = badges.OrderBy(x => (int)Enum.Parse(typeof(MOBFlightBadgeSortOrder), x.SortPriority)).ToList();
            }

            return badges;
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

        private Model.Shopping.MOBStyledText CovidTestBadge()
        {
            return new Model.Shopping.MOBStyledText()
            {
                BackgroundColor = MOBStyledColor.Yellow.GetDescription(),
                Text = _configuration.GetValue<string>("LabelTextCovidTest"),
                SortPriority = MOBFlightBadgeSortOrder.CovidTestRequired.ToString()
            };
        }

        private Model.Shopping.MOBStyledText ChaseCardMemberBadge()
        {
            return new Model.Shopping.MOBStyledText()
            {
                BackgroundColor = _configuration.GetValue<string>("ChaseCardHolderColor"),
                Text = _configuration.GetValue<string>("LabelTextChaseCardHolder"),
                SortPriority = MOBFlightBadgeSortOrder.ChaseCardHolder.ToString(),
                TextColor = _configuration.GetValue<string>("LabelTextPremierSavingsColor")
            };
        }

        private Model.Shopping.MOBStyledText PremierSavingsBadge()
        {
            return new Model.Shopping.MOBStyledText()
            {
                BackgroundColor = _configuration.GetValue<string>("PremierSavingsColor"),
                Text = _configuration.GetValue<string>("LabelTextPremierSavings"),
                SortPriority = MOBFlightBadgeSortOrder.PremierSavings.ToString(),
                TextColor = _configuration.GetValue<string>("LabelTextPremierSavingsColor")
            };
        }
        private Model.Shopping.MOBStyledText MemberSavingsBadge()
        {
            return new Model.Shopping.MOBStyledText()
            {
                BackgroundColor = _configuration.GetValue<string>("MemberSavingsBadgeColor"),
                Text = _configuration.GetValue<string>("LabelTextMemberSavingsBadge"),
                SortPriority = MOBFlightBadgeSortOrder.MemberSavings.ToString(),
                TextColor = _configuration.GetValue<string>("LabelTextPremierSavingsColor")
            };
        }
        private Model.Shopping.MOBStyledText WheelChairFitsBadge(List<CMSContentMessage> lstMessages)
        {

            Model.Shopping.MOBStyledText wheelchairBadge = new Model.Shopping.MOBStyledText()
            {
                BackgroundColor = _configuration.GetValue<string>("WheelChairFitsBadgeColor"),
                Text = _configuration.GetValue<string>("LabelTextWheelChairFitsBadge"),
                SortPriority = MOBFlightBadgeSortOrder.WheelChairFits.ToString(),
                TextColor = _configuration.GetValue<string>("LabelTextWheelChairFitsColor")
            };
            if (lstMessages != null)
            {
                var message = _shoppingUtility.GetSDLMessageFromList(lstMessages, "WheelchairFilter_FSR_Badge_ToolTip").FirstOrDefault();
                if (message != null)
                {
                    wheelchairBadge.BadgeToolTipContent = new MOBMobileCMSContentMessages()
                    {
                        Title = message?.HeadLine,
                        ContentFull = message?.ContentFull
                    };
                }
            }
            return wheelchairBadge;
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

        public async Task<string> GetFareClassAtShoppingRequestFromPersist(string sessionID)
        {
            #region
            string fareClass = string.Empty;
            try
            {
                ShoppingResponse shop = new ShoppingResponse();
                shop = await _sessionHelperService.GetSession<ShoppingResponse>(sessionID, shop.ObjectName, new List<string> { sessionID, shop.ObjectName });
                fareClass = !string.IsNullOrEmpty(shop.Request.FareClass) ? shop.Request.FareClass : string.Empty;
            }
            catch { }
            return await Task.FromResult(fareClass);
            #endregion
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


        public async Task<List<United.Services.FlightShopping.Common.LMX.LmxFlight>> GetLmxFlights(string token, string cartId, string hashList, string sessionId, int applicationId, string appVersion, string deviceId)
        {
            List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights = null;

            if (!string.IsNullOrEmpty(cartId))
            {
                var returnValue = await _flightShoppingService.GetLmxQuote<LmxQuoteResponse>(token, sessionId, cartId, hashList).ConfigureAwait(false);

                var lmxQuoteResponse = returnValue.response;
                string cslCallTime = (returnValue.callDuration / (double)1000).ToString();

                FlightStatus flightStatus = new FlightStatus();

                if (lmxQuoteResponse != null && lmxQuoteResponse.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success))
                {
                    if (lmxQuoteResponse.Flights != null && lmxQuoteResponse.Flights.Count > 0)
                    {
                        lmxFlights = lmxQuoteResponse.Flights;
                    }
                }
            }
            return lmxFlights;
        }

        public void PopulateFlightAmenities(Collection<AmenitiesProfile> amenityFlights, ref List<Flight> flights)
        {
            if (amenityFlights != null && amenityFlights.Count > 0)
            {
                try
                {
                    foreach (Flight flight in flights)
                    {
                        Flight tempFlight = flight;
                        GetAmenitiesForFlight(amenityFlights, ref tempFlight);
                        flight.Amenities = tempFlight.Amenities;

                        if (flight.Connections != null && flight.Connections.Count > 0)
                        {
                            List<Flight> tempFlights = flight.Connections;
                            PopulateFlightAmenities(amenityFlights, ref tempFlights);
                            flight.Connections = tempFlights;
                        }
                        if (flight.StopInfos != null && flight.StopInfos.Count > 0)
                        {
                            List<Flight> tempFlights = flight.StopInfos;
                            PopulateFlightAmenities(amenityFlights, ref tempFlights);
                            flight.StopInfos = tempFlights;
                        }
                    }
                }
                catch { }
            }
        }

        public void GetAmenitiesForFlight(Collection<AmenitiesProfile> amenityFlights, ref Flight flight)
        {
            foreach (AmenitiesProfile amenityFlight in amenityFlights)
            {
                if (flight.FlightNumber == amenityFlight.Key)
                {
                    //update flight amenities
                    flight.Amenities = amenityFlight.Amenities;
                    return;
                }
            }
        }

        public async Task<UpdateAmenitiesIndicatorsResponse> GetAmenitiesForFlights(string sessionId, string cartId, List<Flight> flights, int appId, string deviceId, string appVersion, bool isClientCall = false, UpdateAmenitiesIndicatorsRequest amenitiesPersistRequest = null)
        {
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName });
            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }

            UpdateAmenitiesIndicatorsRequest amenitiesRequest = new UpdateAmenitiesIndicatorsRequest();
            UpdateAmenitiesIndicatorsResponse response = null;
            if (isClientCall)
            {
                amenitiesRequest = amenitiesPersistRequest;
            }
            else
            {
                amenitiesRequest = GetAmenitiesRequest(cartId, flights);
            }
            string jsonRequest = JsonConvert.SerializeObject(amenitiesRequest);

            var returnValue = await _flightShoppingService.UpdateAmenitiesIndicators<UpdateAmenitiesIndicatorsResponse>(session.Token, sessionId, jsonRequest).ConfigureAwait(false);
            response = returnValue.response;
            string cslCallTime = (returnValue.callDuration / (double)1000).ToString();

            //we do not want to throw an errors and stop bookings if this fails
            if (response != null && (response.Errors == null || response.Errors.Count < 1) && response.Profiles != null && response.Profiles.Count > 0)
            {
            }
            else
            {
                if (response?.Errors?.Count > 0)
                {
                    string errorMessage = string.Empty;
                    foreach (var error in response.Errors)
                    {
                        errorMessage = errorMessage + " " + error.Message;
                    }
                    _logger.LogError("GetAmenitiesForFlight - Response for GetAmenitiesForFlight {@Response}, {@ErrorMessage}", JsonConvert.SerializeObject(response), errorMessage);
                }
                else
                {
                    _logger.LogError("GetAmenitiesForFlight - Response for GetAmenitiesForFlight {@Response}", JsonConvert.SerializeObject(response));
                }
            }
            return response;
        }

        public List<Model.Shopping.MOBSHOPShoppingProduct> PopulateColumns(ColumnInformation columnInfo)
        {
            List<Model.Shopping.MOBSHOPShoppingProduct> columns = null;

            //if we have columns...
            if (columnInfo != null && columnInfo.Columns != null && columnInfo.Columns.Count > 0)
            {
                columns = new List<Model.Shopping.MOBSHOPShoppingProduct>();
                foreach (Column column in columnInfo.Columns)
                {
                    Model.Shopping.MOBSHOPShoppingProduct product = new Model.Shopping.MOBSHOPShoppingProduct();
                    product.LongCabin = column.DataSourceLabel;
                    product.Description = column.Description;
                    product.Type = column.Type;
                    product.SubType = column.SubType != null ? column.SubType : string.Empty;
                    product.ColumnID = column.DescriptionId;
                    columns.Add(product);
                }
            }
            return columns;
        }

        private async Task<List<Model.Shopping.MOBSHOPShoppingProduct>> PopulateColumns(ColumnInformation columnInfo, bool getFlightsWithStops, Session session)
        {
            List<Model.Shopping.MOBSHOPShoppingProduct> columns = null;

            //if we have columns...
            if (columnInfo != null && columnInfo.Columns != null && columnInfo.Columns.Count > 0)
            {
                columns = new List<Model.Shopping.MOBSHOPShoppingProduct>();
                foreach (Column column in columnInfo.Columns)
                {
                    Model.Shopping.MOBSHOPShoppingProduct product = new Model.Shopping.MOBSHOPShoppingProduct();
                    product.LongCabin = column.DataSourceLabel;
                    product.Description = column.Description;
                    product.Type = column.Type;
                    product.SubType = column.SubType != null ? column.SubType : string.Empty;
                    product.ColumnID = column.DescriptionId;
                    columns.Add(product);
                }
            }

            //Shop non-stop change adding shop 1 call prod type to columns, if not exist
            if (!session.IsFSRRedesign)
            {
                if (_configuration.GetValue<bool>("EnableNonStopFlight"))
                {
                    if (getFlightsWithStops)
                    {
                        LatestShopAvailabilityResponse persistAvailability = new LatestShopAvailabilityResponse();

                        persistAvailability = await _sessionHelperService.GetSession<LatestShopAvailabilityResponse>(session.SessionId, persistAvailability.ObjectName, new List<string> { session.SessionId, persistAvailability.ObjectName });
                        // persistAvailability = United.Persist.FilePersist.Load<United.Persist.Definition.Shopping.LatestShopAvailabilityResponse>(session.SessionId, persistAvailability.ObjectName);

                        if (persistAvailability != null &&
                            !persistAvailability.AvailabilityList.IsNullOrEmpty() &&
                            persistAvailability.AvailabilityList["1"].Trip != null &&
                            !persistAvailability.AvailabilityList["1"].Trip.Columns.IsNullOrEmpty() &&
                            !columns.IsNullOrEmpty()
                            )
                        {
                            foreach (var persitColumn in persistAvailability.AvailabilityList["1"].Trip.Columns)
                            {
                                if (!columns.Exists(p => p.Type == persitColumn.Type))
                                {
                                    columns.Add(persitColumn);
                                }
                            }
                        }
                    }
                }
            }
            return columns;
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
                        if (airportDesc.IsNullOrEmpty())
                        {
                            airportDesc = airPort.AirportInfo;
                        }
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

        private string FormatDateFromDetails(string dateTimeString)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(0);
            DateTime.TryParse(dateTimeString, out dateTime);
            if (dateTime.Ticks != 0)
            {
                result = string.Format("{0:MM/dd/yyyy}", dateTime);
            }

            return result;
        }

        private async Task<AirportDetailsList> GetAllAiportsList(List<United.Services.FlightShopping.Common.Trip> trips)
        {
            string airPortCodes = GetAllAirportCodesWithCommaDelimatedFromCSLTrips(trips);
            return await GetAirportNamesListCollection(airPortCodes);
        }

        private string GetAllAirportCodesWithCommaDelimatedFromCSLTrips(List<United.Services.FlightShopping.Common.Trip> trips)
        {
            string airPortCodes = string.Empty;
            if (trips != null && trips.Count > 0)
            {
                airPortCodes = string.Join(",", trips.Where(t => t != null).Select(t => t.Origin + "," +
                                                                                        t.Destination + "," +
                                                                                        GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(t.Flights))
                                           );
            }
            airPortCodes = Regex.Replace(airPortCodes, ",{2,}", ",").Trim(',');
            airPortCodes = String.Join(",", airPortCodes.Split(',').Distinct());
            return airPortCodes;
        }

        private bool isGetAirportListInOneCallToggleOn()
        {
            return _configuration.GetValue<bool>("GetAirportNameInOneCallToggle");
        }

        public string GetPriceFromText(string searchType)
        {
            string priceFromText = string.Empty;

            if (searchType == "OW")
            {
                priceFromText = _configuration.GetValue<string>("PriceFromText").ToString().Split('|')[0];//One Way -- For
            }
            else if (searchType == "RT")
            {
                priceFromText = _configuration.GetValue<string>("PriceFromText").ToString().Split('|')[1];//Round Trip -- From
            }
            else if (searchType == "MD")
            {
                priceFromText = _configuration.GetValue<string>("PriceFromText").ToString().Split('|')[2];//Multi -- From
            }

            return priceFromText;
        }

        public string GetPriceFromText(MOBSHOPShopRequest shopRequest)
        {
            string priceFromText = string.Empty;
            String PriceFromTextTripPlanner = _configuration.GetValue<string>("PriceFromTextTripPlanner") ?? "";

            if (shopRequest.SearchType == "OW")
            {
                priceFromText = PriceFromTextTripPlanner.Split('|')[0];//One Way -- For
            }
            else if (shopRequest.SearchType == "RT")
            {
                priceFromText = PriceFromTextTripPlanner.Split('|')[1];//Roundtrip from
            }
            else if (shopRequest.SearchType == "MD")
            {
                priceFromText = PriceFromTextTripPlanner.Split('|')[2];//Multitrip from
            }

            return priceFromText;
        }

        private bool IsTripPlanSearch(string travelType)
        {
            return _configuration.GetValue<bool>("EnableTripPlannerView") && travelType == MOBTripPlannerType.TPSearch.ToString() || travelType == MOBTripPlannerType.TPEdit.ToString();
        }

        private string GetFSRFareDescription(MOBSHOPShopRequest request, bool isBagCalcMobileRedirect = false)
        {
            string FSRFareDescription = string.Empty;
            bool isExperiment = false;

            // Need to add new experiment when comparing
            if (_shoppingUtility.CheckFSRRedesignFromShop(request))
            {
                FSRFareDescription = GetNewFSRFareDescriptionMessage(request.SearchType, isBagCalcMobileRedirect);
                if (_configuration.GetValue<bool>("EnableSortFilterEnhancements") &&
                   !_shoppingUtility.IsSortDisclaimerForNewFSR(request.Application.Id, request.Application.Version.Major))
                {
                    var forAppend = !string.IsNullOrEmpty(FSRFareDescription);
                    FSRFareDescription = $"{FSRFareDescription}{GetHtmlForSortDisclaimer(forAppend)}";
                }
            }
            else
            {
                FSRFareDescription = GetFSRFareDescriptionMessage(request.SearchType);
                // MOBILE-14512
                if (_configuration.GetValue<bool>("EnableSortFilterEnhancements") &&
                    !_shoppingUtility.IsSortDisclaimerForNewFSR(request.Application.Id, request.Application.Version.Major))
                {
                    var forAppend = !string.IsNullOrEmpty(FSRFareDescription);
                    FSRFareDescription = $"{FSRFareDescription}{GetTextForSortDisclaimer(forAppend)}";
                }
            }

            if (_configuration.GetValue<bool>("IsExperimentEnabled") && request.Experiments != null && request.Experiments.Any() && request.Experiments.Contains(ShoppingExperiments.NoChangeFee.ToString()))
            {
                isExperiment = true;
            }

            if (request.SearchType == "RT")
            {
                if (_configuration.GetValue<bool>("ChangeFeeWaiverMessagingToggle"))
                {
                    if (isExperiment)
                    {
                        return FSRFareDescription;
                    }
                    else
                        FSRFareDescription += "\n" + _shoppingUtility.GetFeeWaiverMessage();
                }
            }
            else if (request.SearchType == "MD")
            {
                if (_configuration.GetValue<bool>("ChangeFeeWaiverMessagingToggle"))
                {
                    if (isExperiment)
                    {
                        return FSRFareDescription;
                    }
                    else
                        FSRFareDescription += "\n" + _shoppingUtility.GetFeeWaiverMessage();
                }
            }
            else if (request.SearchType == "OW" && _configuration.GetValue<bool>("ChangeFeeWaiverMessagingToggle"))
            {
                if (isExperiment)
                {
                    return FSRFareDescription;
                }
                else
                    FSRFareDescription += "\n" + _shoppingUtility.GetFeeWaiverMessage();
            }

            return FSRFareDescription;
        }
        private string GetHtmlForSortDisclaimer(bool forAppend)
        {
            return $"{(forAppend ? "<br>" : "")}{_configuration.GetValue<string>("AdditionalLegalDisclaimerTextForOldFSR")}"
                   .Replace("$1", GetFormatedUrl(_httpContext.Request.Host.Value,
                                                                 _httpContext.Request.Scheme,
                                                 "/Images/alert-sign.png", true));
        }
        private string GetFSRFareDescriptionMessage(string searchType)
        {
            string FSRDescription = string.Empty;
            switch (searchType)
            {
                case "OW":
                    if (_configuration.GetValue<bool>("EnableFixMobile16188"))
                    {
                        FSRDescription = _configuration.GetValue<string>("FSRFareDescription").ToString().Split('|')[2];
                    }
                    break;
                case "RT":
                    FSRDescription = _configuration.GetValue<string>("FSRFareDescription").ToString().Split('|')[0];
                    break;
                case "MD":
                    FSRDescription = _configuration.GetValue<string>("FSRFareDescription").ToString().Split('|')[1];
                    break;
                default:
                    FSRDescription = string.Empty;
                    break;
            }

            return FSRDescription;
        }

        private string GetNewFSRFareDescriptionMessage(string searchType, bool isMobileRedirect)
        {
            string FSRDescription = _configuration.GetValue<string>("FSRRedesignFareDescription").ToString();
            switch (searchType)
            {
                case "OW":
                    FSRDescription = FSRDescription.Split('|')[2];
                    break;
                case "RT":
                    FSRDescription = FSRDescription.Split('|')[0];
                    break;
                case "MD":
                    FSRDescription = FSRDescription.Split('|')[1];
                    break;
                default:
                    FSRDescription = string.Empty;
                    break;
            }

            if (!string.IsNullOrEmpty(FSRDescription))
            {
                var enableNewBaggageTextOnFSRShop = CheckBagFareDesclaimer(isMobileRedirect);
                FSRDescription = $"{FSRDescription}{enableNewBaggageTextOnFSRShop}";
            }

            return FSRDescription;
        }

        public string GetPriceTextDescription(string searchType)
        {
            string priceTextDescription = string.Empty;

            if (searchType == "RT")
            {
                priceTextDescription = _configuration.GetValue<string>("PriceTextDescription").ToString().Split('|')[0];//Roundtrip
            }
            else if (searchType == "MD")
            {
                priceTextDescription = _configuration.GetValue<string>("PriceTextDescription").ToString().Split('|')[1];//From
            }

            return priceTextDescription;
        }

        public async Task<bool> IsLastTripFSR(bool isReshopChange, MOBSHOPAvailability availability, List<United.Services.FlightShopping.Common.Trip> trips)
        {
            bool isLastTripFSR = false;
            if (isReshopChange)
            {

                if (availability.Reservation == null)
                {
                    availability.Reservation = new MOBSHOPReservation(_configuration, _cachingService);
                }

                isLastTripFSR = (trips.Where(t => (t.ChangeType == United.Services.FlightShopping.Common.Types.ChangeTypes.ChangeTrip ||
                                                   t.ChangeType == United.Services.FlightShopping.Common.Types.ChangeTypes.NewTrip)).Count() == 0);

                Reservation bookingPathReservation = new Reservation();

                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(availability.SessionId, bookingPathReservation.ObjectName, new List<string> { availability.SessionId, bookingPathReservation.ObjectName });

                availability.Reservation.Reshop = bookingPathReservation.Reshop;
                availability.Reservation.IsReshopChange = bookingPathReservation.IsReshopChange;
                if (isLastTripFSR)
                {
                    await GetOldEMailID(availability.SessionId, bookingPathReservation);
                }
                availability.Reservation.HasJSXSegment = bookingPathReservation.HasJSXSegment;
                availability.Reservation.Reshop.IsLastTripFSR = isLastTripFSR;
            }
            return isLastTripFSR;
        }

        private async System.Threading.Tasks.Task GetOldEMailID(string sessionId, Reservation persistedReservation)
        {
            var oldEmail = await _sessionHelperService.GetSession<MOBEmail>(sessionId, new MOBEmail().ObjectName, new List<string> { sessionId, new MOBEmail().ObjectName });

            if (oldEmail != null)
            {
                persistedReservation.ReservationEmail = oldEmail;
            }
            if (persistedReservation.CreditCardsAddress?.Count > 0 && persistedReservation.IsSignedInWithMP)
            {
                persistedReservation.Reshop.RefundAddress = new MOBAddress();
                persistedReservation.Reshop.RefundAddress = persistedReservation.CreditCardsAddress[0];
                if (persistedReservation.CreditCards.Count > 0)
                {
                    persistedReservation.CreditCardsAddress.Clear();
                    persistedReservation.CreditCards.Clear();
                    persistedReservation.ReservationPhone = null;
                }
            }
            else
            {
                persistedReservation.Reshop.RefundAddress = null;
                if (persistedReservation.CreditCards?.Count > 0)
                {
                    persistedReservation.CreditCardsAddress.Clear();
                    persistedReservation.CreditCards.Clear();
                    persistedReservation.ReservationPhone = null;
                }
            }
            await _sessionHelperService.SaveSession<Reservation>(persistedReservation, sessionId, new List<string> { sessionId, persistedReservation.ObjectName }, persistedReservation.ObjectName);
        }

        public List<MOBSHOPFareWheelItem> PopulateFareWheelDates(List<United.Mobile.Model.Shopping.MOBSHOPTripBase> shopTrips, string currentCall)
        {
            List<MOBSHOPFareWheelItem> fareWheelItems = new List<MOBSHOPFareWheelItem>();
            try
            {
                if (shopTrips != null && shopTrips.Count > 0)
                {
                    #region
                    DateTime departureDate = DateTime.ParseExact(shopTrips[0].DepartDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);

                    DateTime fareWheelStart = departureDate.AddDays(-getFlexibleDaysBefore()); ;

                    DateTime fareWheelEnd = departureDate.AddDays(getFlexibleDaysAfter());

                    if (departureDate == DateTime.Today)
                    {
                        fareWheelStart = departureDate;
                    }
                    if (shopTrips.Count >= 2)
                    {
                        DateTime departureDate2 = DateTime.ParseExact(shopTrips[1].DepartDate, "MM/dd/yyyy", CultureInfo.InvariantCulture);

                        if (!string.IsNullOrEmpty(currentCall))
                        {
                            if (currentCall == "SHOP" && (DateTime.Compare(departureDate2, fareWheelEnd)) < 0)
                            {
                                fareWheelEnd = departureDate2;
                            }

                            if (currentCall == "SELECTTRIP" && (DateTime.Compare(departureDate2, fareWheelStart)) > 0)
                            {
                                fareWheelStart = departureDate2;
                            }
                        }
                    }
                    for (DateTime fareWheelDate = fareWheelStart; fareWheelDate <= fareWheelEnd; fareWheelDate = fareWheelDate.AddDays(1))
                    {
                        MOBSHOPFareWheelItem fareWheel = new MOBSHOPFareWheelItem();

                        fareWheel.Key = (fareWheelDate.Month < 10 ? "0" + fareWheelDate.Month.ToString() : fareWheelDate.Month.ToString()) + "/" + (fareWheelDate.Day < 10 ? "0" + fareWheelDate.Day.ToString() : fareWheelDate.Day.ToString()) + "/" + fareWheelDate.Year.ToString();
                        if (currentCall != "SHOP-NOFLIGHTS-FOUND")
                            fareWheel.DisplayValue = fareWheelDate.ToString("ddd MMM dd");

                        fareWheelItems.Add(fareWheel);
                    }
                    #endregion
                }
            }
            catch { }
            return fareWheelItems;
        }

        private async
        Task
SetAvailabilityELFProperties(MOBSHOPAvailability availability, bool isMultiTravelers, bool isSSA)
        {
            if (availability != null)
            {
                availability.ELFShopMessages = await SetElfShopMessage(isMultiTravelers, isSSA);
                availability.ELFShopOptions = await ParseELFShopOptions(isSSA);
            }
        }

        private async Task<List<Option>> ParseELFShopOptions(bool isSSA)
        {
            List<MOBItem> list = isSSA ? await GetELFShopMessages("SSA_ELF_CONFIRMATION_PAGE_OPTIONS") :
                                         await GetELFShopMessages("ELF_CONFIRMATION_PAGE_OPTIONS");
            List<Option> elfOptions = new List<Option>();
            if (list != null && list.Count > 0)
            {
                var orderedList = list.Where(o => o != null).OrderBy(o => Convert.ToInt32(o.Id)).ToList();
                foreach (var mobItem in orderedList)
                {
                    if (mobItem.CurrentValue != string.Empty)
                    {
                        string[] mobShopOptionValueCollection = mobItem.CurrentValue.Split('|');
                        if (mobShopOptionValueCollection.Length == 4)
                        {
                            elfOptions.Add(new Option()
                            {
                                OptionDescription = mobShopOptionValueCollection[0],
                                AvailableInElf = Convert.ToBoolean(mobShopOptionValueCollection[1]),
                                AvailableInEconomy = Convert.ToBoolean(mobShopOptionValueCollection[2]),
                                OptionIcon = mobShopOptionValueCollection[3]
                            });
                        }
                    }
                }
            }
            return elfOptions;
        }

        private async Task<List<MOBItem>> GetELFShopMessages(string elfDocumentLibraryTableKey)
        {
            List<MOBItem> messages = new List<MOBItem>();

            var docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(elfDocumentLibraryTableKey, _headers.ContextValues.SessionId, true);
            if (docs != null && docs.Count > 0)
            {
                foreach (MOBLegalDocument doc in docs)
                {
                    MOBItem item = new MOBItem();
                    item.Id = doc.Title;
                    item.CurrentValue = doc.LegalDocument;
                    messages.Add(item);
                }
            }

            return messages;
        }

        private async Task<List<MOBItem>> SetElfShopMessage(bool isMultiTravelers, bool isSSA)
        {
            var list = isSSA ? await GetELFShopMessages("SSA_ELF_CONFIRMATION_PAGE_HEADER_FOOTER") :
                             await GetELFShopMessages("ELF_CONFIRMATION_PAGE_HEADER_FOOTER");
            if (list != null && list.Count > 0)
            {
                var multiTravelerTitle = list.Find(p => p != null && p.Id == "ELFConfirmFareTypeTitle");
                if (!isMultiTravelers && multiTravelerTitle != null)
                {
                    multiTravelerTitle.CurrentValue = "";
                }
            }

            return list;
        }

        public SHOPAwardCalendar PopulateAwardCalendar(United.Services.FlightShopping.Common.CalendarType calendar, string tripId, string productId)
        {
            SHOPAwardCalendar awardCalendar = new SHOPAwardCalendar();
            awardCalendar.AwardCalendarItems = new List<AwardCalendarItem>();
            awardCalendar.ProductId = productId;
            awardCalendar.TripId = tripId;

            if (calendar != null && calendar.Months != null && calendar.Months.Count > 0)
            {
                foreach (CalendarMonth month in calendar.Months)
                {
                    foreach (CalendarWeek week in month.Weeks)
                    {
                        foreach (CalendarDay day in week.Days)
                        {
                            AwardCalendarItem item = new AwardCalendarItem();
                            item.Departs = DateTime.Parse(day.DateValue);

                            foreach (CalendarDaySolution solution in day.Solutions)
                            {
                                if (solution.AwardType.ToUpper() == "BUSINESS-SURPLUS")
                                    item.HasPremiumSaver = true;

                                if (solution.AwardType.ToUpper() == "ECONOMY-SURPLUS")
                                    item.HasEconomySaver = true;

                            }

                            awardCalendar.AwardCalendarItems.Add(item);
                        }
                    }
                }
            }
            if (awardCalendar.AwardCalendarItems != null && awardCalendar.AwardCalendarItems.Count < 1)
            {
                awardCalendar = null;
            }
            return awardCalendar;
        }

        private async System.Threading.Tasks.Task SaveCSLShopResponseForTripPlanner(MOBSHOPShopRequest shopRequest, United.Services.FlightShopping.Common.ShopResponse response)
        {
            if (_configuration.GetValue<bool>("EnableTripPlannerView") && (shopRequest.TravelType == MOBTripPlannerType.TPSearch.ToString() || shopRequest.TravelType == MOBTripPlannerType.TPEdit.ToString()))
            {
                var persistCSLShopResponse = await _sessionHelperService.GetSession<CSLShopResponse>(shopRequest.SessionId, new CSLShopResponse().ObjectName, new List<string> { shopRequest.SessionId, new CSLShopResponse().ObjectName });

                if ((persistCSLShopResponse?.ShopCSLResponse?.Trips?.Count ?? 0) > 0)
                {
                    persistCSLShopResponse.ShopCSLResponse.Trips[response.LastTripIndexRequested - 1]?.Flights?.AddRange(response?.Trips[response.LastTripIndexRequested - 1]?.Flights);

                    //If stops column count > non stop column count take stops column info
                    if ((persistCSLShopResponse.ShopCSLResponse.Trips[response.LastTripIndexRequested - 1]?.ColumnInformation?.Columns?.Count ?? 0) < (response.Trips[response.LastTripIndexRequested - 1]?.ColumnInformation?.Columns?.Count ?? 0))
                    {
                        persistCSLShopResponse.ShopCSLResponse.Trips[response.LastTripIndexRequested - 1].ColumnInformation = response.Trips[response.LastTripIndexRequested - 1].ColumnInformation;
                    }
                }
                else
                {
                    persistCSLShopResponse = new CSLShopResponse();
                    persistCSLShopResponse.ShopCSLResponse = response;
                }

                await _sessionHelperService.SaveSession<CSLShopResponse>(persistCSLShopResponse, shopRequest.SessionId, new List<string> { shopRequest.SessionId, persistCSLShopResponse?.ObjectName }, persistCSLShopResponse?.ObjectName, saveJsonOnCloudXMLOnPrem: false);
            }
        }

        public bool NoCSLExceptions(List<United.Services.FlightShopping.Common.ErrorInfo> errors)
        {
            #region
            if (errors != null && errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    if (errors.Count == 1 && error.MinorCode.Trim() == "10036") //08/20/2016 - Venkat  : errors.Count == 1 to check If CSL Shop returns only one exception and its  "Request Time Out Exception" as per stogo this is like warning and we can keep Moving and ignore this error.
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                return true;
            }
            #endregion
        }

        public async Task<(bool, MOBPromoAlertMessage couponAlertMessages)> ShopValidate(string token, MOBSHOPShopRequest shopRequest, MOBPromoAlertMessage couponAlertMessages)
        {
            bool validPromoCode = false;

            //rough validation of PassPlus code
            if (!(await _shoppingUtility.IsEnableAdvanceSearchOfferCode(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest?.CatalogItems).ConfigureAwait(false)) && validatePassPlusPromo(shopRequest.PromotionCode))
            {
                ShopRequest request = await GetShopRequest(shopRequest, true);
                string jsonRequest = JsonConvert.SerializeObject(request);

                var returnValue = await _flightShoppingService.ShopValidateSpecialPricing<United.Services.FlightShopping.Common.ShopResponse>(token, shopRequest.SessionId, jsonRequest).ConfigureAwait(false);
                var response = returnValue.response;
                string cslCallTime = (returnValue.callDuration / (double)1000).ToString();
                if (response != null)
                {
                    //need to update this based on response from CSL service.
                    if (response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success))
                    {
                        if (response.SpecialPricingInfo != null && !string.IsNullOrEmpty(response.SpecialPricingInfo.PassengerTypeCodeNonECD))
                        {
                            //PP for PassPluyds for now
                            if (response.SpecialPricingInfo.PassengerTypeCodeNonECD.Trim().ToUpper().StartsWith("PP"))
                            {
                                validPromoCode = true;
                            }
                            else
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("InvalidPromoCodeError"));
                            }
                        }
                        else
                        {
                            throw new MOBUnitedException(_configuration.GetValue<string>("InvalidPromoCodeError"));
                        }
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

                            throw new MOBUnitedException(_configuration.GetValue<string>("InvalidPromoCodeError"));
                        }
                        else
                        {
                            throw new MOBUnitedException(_configuration.GetValue<string>("InvalidPromoCodeError"));
                        }
                    }
                }

                else
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }
            else if (!string.IsNullOrEmpty(shopRequest.PromotionCode))
            {
                ShopRequest request = await GetShopRequest(shopRequest, true);
                if (request.LoyaltyPerson == null)
                    request.LoyaltyPerson = new Service.Presentation.PersonModel.LoyaltyPerson();
                request.LoyaltyPerson.LoyaltyProgramMemberID = shopRequest.MileagePlusAccountNumber;
                string jsonRequest = JsonConvert.SerializeObject(request);

                string url = string.Format("{0}/ShopValidateSpecialPricing", _configuration.GetValue<string>("ServiceEndPointBaseUrl - CSLShopping"));

                #region//****Get Call Duration Code - Venkat 03/17/2015*******
                Stopwatch cslStopWatch;
                cslStopWatch = new Stopwatch();
                cslStopWatch.Reset();
                cslStopWatch.Start();
                #endregion//****Get Call Duration Code - Venkat 03/17/2015*******
                var jsonResponse = await _flightShoppingService.ShopValidateSpecialPricing<United.Services.FlightShopping.Common.ShopResponse>(token, shopRequest.SessionId, jsonRequest).ConfigureAwait(false);
                #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
                if (cslStopWatch.IsRunning)
                {
                    cslStopWatch.Stop();
                }
                string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
                #endregion
                if (!jsonResponse.IsNullOrEmpty())
                {
                    //need to update this based on response from CSL service.
                    if (jsonResponse.response != null && jsonResponse.response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success))
                    {

                        if (jsonResponse.response.SpecialPricingInfo != null && jsonResponse.response.SpecialPricingInfo.MerchOfferCoupon != null && jsonResponse.response.SpecialPricingInfo.MerchOfferCoupon.IsCouponEligible.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
                        {
                            return (true, couponAlertMessages);
                        }
                        else if (await _shoppingUtility.IsEnableAdvanceSearchOfferCode(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest?.CatalogItems).ConfigureAwait(false) && (jsonResponse.response.Errors == null || jsonResponse.response.Errors.Count == 0))
                        {
                            return (true, couponAlertMessages);
                        }
                        else
                        {
                            couponAlertMessages = GetCouponAlertMessages(shopRequest.PromotionCode, null);
                            return (false, couponAlertMessages);
                        }
                    }
                    else
                    {
                        if (jsonResponse.response.Errors != null && jsonResponse.response.Errors.Count > 0)
                        {
                            string errorMessage = string.Empty;
                            foreach (var error in jsonResponse.response.Errors)
                            {
                                var errorCodes = _configuration.GetValue<string>("AdvanceSearchCouponMajorErrorCodesMerch").Split(',');
                                if (!string.IsNullOrEmpty(error.MajorCode)
                                      && errorCodes.Any(x => x.Equals(error.MajorCode, StringComparison.OrdinalIgnoreCase)))
                                {
                                    couponAlertMessages = GetCouponAlertMessages(shopRequest.PromotionCode, error);
                                }
                                else if (await _shoppingUtility.IsEnableAdvanceSearchOfferCode(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest?.CatalogItems).ConfigureAwait(false)
                                        && !string.IsNullOrEmpty(error.MajorCode) && error.MajorCode == "ECD")
                                {
                                    couponAlertMessages = GetOfferCodeErrorMessages(error, shopRequest);
                                }
                            }
                            if (couponAlertMessages == null || string.IsNullOrEmpty(couponAlertMessages?.PromoCode))
                            {
                                couponAlertMessages = GetCouponAlertMessages(shopRequest.PromotionCode, null);
                            }
                            return (false, couponAlertMessages);
                        }
                        else
                        {
                            throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                        }
                    }
                }
                else
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }
            return (validPromoCode, couponAlertMessages);
        }
        private MOBPromoAlertMessage GetCouponAlertMessages(string couponCode, ErrorInfo error)
        {
            MOBPromoAlertMessage couponAlertMessages = new MOBPromoAlertMessage();
            // Need to check for major code and minor codes for each scenario.
            if (error != null && !string.IsNullOrEmpty(error.MajorCode) && !string.IsNullOrEmpty(error.MinorCode))
            {
                var errorCodes = _configuration.GetValue<string>("AdvanceSearchCouponMinorErrorCodesMerch").Split(',');
                couponAlertMessages.PromoCode = couponCode;

                if (error.MinorCode.Equals("12") || error.MinorCode.Equals("14"))
                {
                    couponAlertMessages.IsSignInRequired = true;
                    couponAlertMessages.AlertMessages = new MOBSection
                    {
                        Text2 = string.Format(_configuration.GetValue<string>("AdvanceSearchCouponMPSignonErrorMessage"), _configuration.GetValue<string>("AdvanceSearchCouponMPSignInHTMLText")),
                        IsDefaultOpen = true,
                        MessageType = MOBIConType.Warning.ToString()
                    };
                    couponAlertMessages.CouponInlineAlertMessage = error.Message;
                }
                else if (error.MinorCode.Equals("15"))
                {
                    couponAlertMessages.IsSignInRequired = false;
                    couponAlertMessages.AlertMessages = new MOBSection
                    {
                        Text1 = string.Format(_configuration.GetValue<string>("AdvanceSearchCouponTripTypeErrorHeaderMessage"), couponCode),
                        Text2 = _configuration.GetValue<string>("AdvanceSearchCouponTripTypeErrorBodyMessage") + _configuration.GetValue<string>("AdvanceSearchCouponTermsAndConditionsHTMLText"),
                        IsDefaultOpen = true,
                        MessageType = MOBIConType.Warning.ToString()
                    };
                    couponAlertMessages.TermsandConditions = _configuration.GetValue<string>("AdvanceSearchCouponTermsAndConditionsHTMLText");
                    couponAlertMessages.CouponInlineAlertMessage = error.Message;
                }
                else if (errorCodes.Any(x => x.Equals(error.MinorCode, StringComparison.OrdinalIgnoreCase)))
                {
                    couponAlertMessages.IsSignInRequired = false;
                    couponAlertMessages.AlertMessages = new MOBSection
                    {
                        Text2 = error.MinorCode.Equals("00") ? error.Message : error.Message + " View " + _configuration.GetValue<string>("AdvanceSearchCouponTermsAndConditionsHTMLText"),
                        IsDefaultOpen = true,
                        MessageType = MOBIConType.Warning.ToString()
                    };
                    couponAlertMessages.TermsandConditions = _configuration.GetValue<string>("AdvanceSearchCouponTermsAndConditionsHTMLText");
                    couponAlertMessages.CouponInlineAlertMessage = error.Message;
                }

            }
            else
            {
                couponAlertMessages.PromoCode = couponCode;
                couponAlertMessages.IsSignInRequired = false;
                couponAlertMessages.AlertMessages = new MOBSection
                {
                    Text2 = _configuration.GetValue<string>("AdvanceSearchCouponErrorMessage"),
                    IsDefaultOpen = true,
                    MessageType = MOBIConType.Warning.ToString()
                };
                couponAlertMessages.CouponInlineAlertMessage = _configuration.GetValue<string>("AdvanceSearchCouponErrorMessage");

            }

            return couponAlertMessages;
        }

        private MOBPromoAlertMessage GetOfferCodeErrorMessages(ErrorInfo error, MOBSHOPShopRequest shopRequest)
        {
            string erroressage = string.Empty;
            switch (error.MinorCode)
            {
                case "3":
                case "4":
                case "5":
                case "6":
                case "7":
                case "8":
                    erroressage = _configuration.GetValue<string>("AdvanceSearchInvalidOfferInfoMessage");
                    break;
                case "63":
                    erroressage = _configuration.GetValue<string>("AdvanceSearchOfferMaxPaxAllowedMessage");
                    break;
                case "37":
                    erroressage = _configuration.GetValue<string>("AdvanceSearchOfferInvalidODMessage");
                    break;
                case "62":
                case "262":
                    erroressage = _configuration.GetValue<string>("AdvanceSearchOfferInvalidDateMessage");
                    break;
                case "75":
                    erroressage = _configuration.GetValue<string>("AdvanceSearchOfferInvalidDateRangeMessage");
                    break;
                case "105":
                    erroressage = _configuration.GetValue<string>("AdvanceSearchInvalidOfferMessage");
                    break;
                case "106":
                    erroressage = String.Format(_configuration.GetValue<string>("AdvanceSearchOfferInvalidToFromMessage"), shopRequest.Trips[0].Destination);
                    break;
            }
            if (!string.IsNullOrEmpty(erroressage))
            {
                MOBPromoAlertMessage offerAlertMessage = new MOBPromoAlertMessage
                {
                    AlertMessages = new MOBSection
                    {
                        Text2 = erroressage,
                        IsDefaultOpen = true,
                        MessageType = MOBIConType.Warning.ToString(),
                    },
                    CouponInlineAlertMessage = erroressage,
                    PromoCode = shopRequest.PromotionCode,
                    IsOffer = true
                };
                return offerAlertMessage;
            }
            return null;
        }


        private bool validatePassPlusPromo(string promoCode)
        {
            bool isValid = false;
            if (!string.IsNullOrEmpty(promoCode))
            {
                if (promoCode.Length == 5)
                {
                    if (promoCode.StartsWith("9"))
                    {
                        if (promoCode.All(char.IsLetterOrDigit))
                        {
                            //just letters and digits.
                            isValid = true;
                        }
                    }
                }
            }
            return isValid;
        }

        public async Task<MOBSHOPAvailability> GetLastTripAvailabilityFromPersist(int lastTripIndexRequested, string sessionID)
        {
            #region
            MOBSHOPAvailability lastTripAvailability = null;
            LatestShopAvailabilityResponse persistAvailability = new LatestShopAvailabilityResponse();

            persistAvailability = await _sessionHelperService.GetSession<LatestShopAvailabilityResponse>(sessionID, persistAvailability.ObjectName, new List<string> { sessionID, persistAvailability.ObjectName });

            if (persistAvailability != null && persistAvailability.AvailabilityList != null && persistAvailability.AvailabilityList.Count > 0 && persistAvailability.AvailabilityList[lastTripIndexRequested.ToString()] != null)
            {
                lastTripAvailability = persistAvailability.AvailabilityList[lastTripIndexRequested.ToString()];
                if (lastTripAvailability.Reservation != null)
                {
                    lastTripAvailability.Reservation.UpdateRewards(_configuration, _cachingService);
                    if (lastTripAvailability.Reservation.TCDAdvisoryMessages != null && lastTripAvailability.Reservation.TCDAdvisoryMessages.Count >= 3)
                    {
                        lastTripAvailability.Reservation.TCDAdvisoryMessages.AddRange(lastTripAvailability.Reservation.TCDAdvisoryMessages.ToList<MOBItem>());
                    }
                }
            }

            return await Task.FromResult(lastTripAvailability);
            #endregion

        }

        public async Task<MOBSHOPAvailability> FilterShopSearchResults(ShopOrganizeResultsReqeust organizeResultsRequest, Session session, bool isRemoveAppliedWheelChairFilter = false)
        {
            MOBSHOPAvailability availability = null;
            CSLSelectTrip selectTripResponeTripPlanner = null;
            List<MOBSearchFilterItem> sortTypes = null;
            if (_configuration.GetValue<bool>("EnableFixNoNumberofStops") && organizeResultsRequest.SearchFiltersIn?.NumberofStops != null && !organizeResultsRequest.SearchFiltersIn.NumberofStops.Any(s => s.IsSelected))
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("NoFlightSearchResult"));
            }
            if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == MOBTripPlannerType.TPSearch.ToString() && organizeResultsRequest.LastTripIndexRequested > 1)
            {
                selectTripResponeTripPlanner = await _sessionHelperService.GetSession<CSLSelectTrip>(organizeResultsRequest.CartId, new CSLSelectTrip().ObjectName, new List<string> { organizeResultsRequest.CartId, new CSLSelectTrip().ObjectName });
                availability = selectTripResponeTripPlanner.MobSelectTripResponse.Availability;
            }
            else
            {
                availability = await GetLastTripAvailabilityFromPersist(organizeResultsRequest.LastTripIndexRequested, session.SessionId);
            }
            ShoppingResponse persistShop = new ShoppingResponse();


            persistShop = await _sessionHelperService.GetSession<ShoppingResponse>(session.SessionId, persistShop.ObjectName, new List<string> { session.SessionId, persistShop.ObjectName });

            if (persistShop == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }
           
            OrganizeResultsRequest request = await GetOrganizeResultRequest(session.CartId, organizeResultsRequest, availability, session, isRemoveAppliedWheelChairFilter);

            string jsonRequest = JsonConvert.SerializeObject(request);

            #region//****Get Call Duration Code - Venkat 03/17/2015*******
            Stopwatch cslStopWatch = new Stopwatch();
            cslStopWatch.Start();
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******


            string jsonResponse = await _flightShoppingService.OrganizeResults(session.Token, jsonRequest, session.SessionId);


            #region// 2 = cslStopWatch//****Get Call Duration Code - Venkat 03/17/2015*******
            if (cslStopWatch.IsRunning)
            {
                cslStopWatch.Stop();
            }
            string cslCallTime = (cslStopWatch.ElapsedMilliseconds / (double)1000).ToString();
            #endregion
            List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(organizeResultsRequest, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                var response = JsonConvert.DeserializeObject<OrganizeResultsResponse>(jsonResponse);
                if (response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success))
                {
                    await SaveCSLShopResponseForTripPlannerOrganizeResults(session, response);
                    CultureInfo ci = null;

                    var fareFamily = GetFareFamily(persistShop.Request.Trips[0].Cabin, persistShop.Request.FareType);
                    var isEnableRefundableFaresToggle = IsEnableRefundableFaresToggle(organizeResultsRequest.Application.Id, organizeResultsRequest.Application.Version.Major);

                    // Refundable fares toggle feature
                    if (isEnableRefundableFaresToggle && response.ColumnInformation != null)
                    {
                        availability.Trip.Columns = PopulateColumns(response.ColumnInformation);
                        if (organizeResultsRequest.SearchFiltersIn?.RefundableFaresToggle?.IsSelected ?? false)
                        {
                            fareFamily = GetFareFamily(persistShop.Request.Trips[0].Cabin, _strFARETYPEFULLYREFUNDABLE);
                        }
                    }

                    availability.Trip.FlattenedFlights = new List<Model.Shopping.MOBSHOPFlattenedFlight>();
                    //-------Feature 208204--- Common class data carrier for hirarchy methds-----
                    MOBSHOPDataCarrier _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
                    _mOBSHOPDataCarrier.SearchType = persistShop.Request.SearchType;
                    if (_configuration.GetValue<bool>("Shopping - bPricingBySlice"))
                    {
                        if (!availability.AwardTravel && !session.IsReshopChange)
                        {
                            if (IsTripPlanSearch(persistShop.Request.TravelType))
                            {
                                _mOBSHOPDataCarrier.PriceFormText = GetPriceFromText(persistShop.Request);
                            }
                            else
                            {
                                _mOBSHOPDataCarrier.PriceFormText = GetPriceFromText(persistShop.Request.SearchType);
                            }
                            availability.PriceTextDescription = GetPriceTextDescription(persistShop.Request.SearchType);

                            //availability.fSRFareDescription = GetFSRFareDescription(persistShop.Request);
                            SetFSRFareDescriptionForShop(availability, persistShop.Request);
                        }
                        else
                        {
                            SetSortDisclaimerForReshop(availability, persistShop.Request);
                        }
                    }

                    if (response.OrganizedFlights != null && response.OrganizedFlights.Count > 0)
                    {

                        string fareClass = await GetFareClassAtShoppingRequestFromPersist(session.SessionId);
                        #region Get LMX Data for Organize Results //**// LMX Flag For AppID change
                        bool supressLMX = false;
                        supressLMX = session.SupressLMXForAppID;
                        List<Flight> returnedFlights = response.OrganizedFlights;
                        if (persistShop.Request.ShowMileageDetails && !supressLMX)
                        {
                            returnedFlights = await GetOrganizeResultsLMXFlights(response.OrganizedFlights, session.Token, session.CartId, session.SessionId, organizeResultsRequest.Application.Id, organizeResultsRequest.Application.Version.Major, organizeResultsRequest.DeviceId);
                        }
                        #endregion
                        MOBAdditionalItems additionalItems = new MOBAdditionalItems();
                        MOBSearchFilters searchFilters = new MOBSearchFilters();
                        searchFilters = await _sessionHelperService.GetSession<MOBSearchFilters>(session.SessionId, searchFilters.GetType().FullName, new List<string> { session.SessionId, searchFilters.GetType().FullName });
                        var tupleResponse = await GetFlights(_mOBSHOPDataCarrier, session.SessionId, availability.CartId, returnedFlights, GetFareFamily(persistShop.Request.Trips[0].Cabin, persistShop.Request.FareType), ci, availability.Trip.SearchFiltersOut.PriceMin, availability.Trip.Columns, persistShop.Request.PremierStatusLevel, fareClass, false, false, session.IsBEFareDisplayAtFSR, null, persistShop.Request.Application.Version.Major, persistShop.Request.Application.Id, session, additionalItems, lstMessages, searchFilters);
                        List<Model.Shopping.MOBSHOPFlight> flights = tupleResponse.Item1;
                        ci = tupleResponse.ci;
                        availability.Trip.FlattenedFlights = await GetFlattendFlights(flights, availability.Trip.TripId, "", availability.Trip.DepartDate, persistShop?.Request, organizeResultsRequest.LastTripIndexRequested - 1, additionalItems: additionalItems, lstMessages: lstMessages, isReshop: session.IsReshopChange).ConfigureAwait(false);
                        try
                        {
                            if (await _featureToggles.IsEnableWheelchairFilterOnFSR(organizeResultsRequest.Application.Id, organizeResultsRequest.Application.Version.Major, session.CatalogItems).ConfigureAwait(false) || (session.IsReshopChange && await _featureToggles.IsEnableReshopWheelchairFilterOnFSR(organizeResultsRequest.Application.Id, organizeResultsRequest.Application.Version.Major, session.CatalogItems).ConfigureAwait(false)))
                            {
                                if (availability.OnScreenAlerts != null && availability.OnScreenAlerts.Count >0 
                                    && availability.OnScreenAlerts.Any(x => x.AlertType == MOBOnScreenAlertType.WHEELCHAIRFITS))
                                    availability.OnScreenAlerts.RemoveAll(x => x.AlertType == MOBOnScreenAlertType.WHEELCHAIRFITS);

                                if (organizeResultsRequest.SearchFiltersIn?.WheelchairFilterContent != null
                                    && organizeResultsRequest.SearchFiltersIn?.WheelchairFilterContent.DimensionInfo != null
                                    && flights.Where(x => x.IsWheelChairFits == true).ToList().Count() == 0)
                                {
                                    _shoppingUtility.AddWheelChairAlertMsgWhenNoneFits(availability, lstMessages);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.ILoggerError("WheelChairFilter-OrganiseShopResults alert message error-{@Exception},SessionId-{sessionId}", JsonConvert.SerializeObject(ex), session.SessionId);
                        }
                        // Refundable fares toggle feature
                        if (isEnableRefundableFaresToggle && response.ColumnInformation != null)
                        {
                            if (session.IsFSRRedesign)
                            {
                                if (_configuration.GetValue<bool>("EnableColumnSelectionFsrRedesign"))
                                {
                                    string focusColumnID = availability.Trip.FlattenedFlights[0].Flights[0].ShoppingProducts.FirstOrDefault(p => p.IsSelectedCabin)?.ColumnID ?? availability.Trip.FlattenedFlights[0].Flights[0].ShoppingProducts[0].ColumnID;
                                    var selectedColumn = availability.Trip.Columns.FirstOrDefault(c => c.ColumnID.Equals(focusColumnID));
                                    if (selectedColumn != null)
                                    {
                                        selectedColumn.IsSelectedCabin = true;
                                    }
                                }

                            }
                        }

                        await StrikeThroughContentMessages(availability, additionalItems, session, organizeResultsRequest);
                        if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && session.CatalogItems != null && session.CatalogItems.Count > 0 &&
                                   session.CatalogItems.FirstOrDefault(a => a.Id == ((int)Common.Helper.Profile.IOSCatalogEnum.EnableNewPartnerAirlines).ToString() || a.Id == ((int)Common.Helper.Profile.AndroidCatalogEnum.EnableNewPartnerAirlines).ToString())?.CurrentValue == "1"
                                  )
                        {

                            availability.FareComparisonMessage = PopulateFareComparisonMessageforAirlines(availability, additionalItems, lstMessages);
                        }
                        if (session.IsFSRRedesign)
                            sortTypes = UpdateSortTypes(availability, response.ResultFiltersUsed);
                        if (IsTripPlanSearch(session.TravelType))
                        {
                            availability.Trip.FlattenedFlights.ForEach(ff => ff.Flights.ForEach(f => f.ShoppingProducts = f.ShoppingProducts.Where(p => p.IsSelectedCabin).ToList()));
                        }

                        if (organizeResultsRequest.SearchFiltersIn.PageNumber == 1)
                        {
                            availability.Trip.FlightCount = availability.Trip.FlattenedFlights.Count;
                            int pageSize = _configuration.GetValue<int>("OrgarnizeResultsRequestPageSize");
                            availability.Trip.PageCount = (availability.Trip.FlightCount / pageSize) + ((availability.Trip.FlightCount % pageSize) == 0 ? 0 : 1);
                            if (_configuration.GetValue<bool>("ReturnAllRemainingShopFlightsWithOnly2PageCount") && availability.Trip.PageCount > 1)
                            {
                                availability.Trip.PageCount = 2;
                            }
                            availability.Trip.TotalFlightCount = response.TotalFlightsBeforeFilter;
                        }
                        else
                        {
                            availability.Trip.FlightCount = response.TotalFlightsBeforeFilter;
                            availability.Trip.TotalFlightCount = response.TotalFlightsBeforeFilter;
                        }
                        if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == MOBTripPlannerType.TPSearch.ToString() && organizeResultsRequest.LastTripIndexRequested > 1)
                        {
                            selectTripResponeTripPlanner.MobSelectTripResponse.Availability = availability;

                            await _sessionHelperService.SaveSession<CSLSelectTrip>(selectTripResponeTripPlanner, organizeResultsRequest.CartId, new List<string> { organizeResultsRequest.CartId, new CSLSelectTrip().ObjectName }, new CSLSelectTrip().ObjectName);
                        }
                        else
                        {
                            await AddAvailabilityToPersist(availability, session.SessionId);
                        }
                    }
                    availability.Trip.SearchFiltersIn = organizeResultsRequest.SearchFiltersIn;
                    if (sortTypes != null)
                    {
                        availability.Trip.SearchFiltersIn.SortTypes = sortTypes;
                    }
                    availability.Trip.SearchFiltersIn.ShowPriceFilters = _configuration.GetValue<bool>("ShowPriceFilter");
                    availability.Trip.SearchFiltersIn.ShowArrivalFilters = _configuration.GetValue<bool>("ShowArrivalFilters");
                    availability.Trip.SearchFiltersIn.ShowDepartureFilters = _configuration.GetValue<bool>("ShowDepartureFilters");
                    availability.Trip.SearchFiltersIn.ShowDurationFilters = _configuration.GetValue<bool>("ShowDurationFilters");
                    availability.Trip.SearchFiltersIn.ShowLayOverFilters = _configuration.GetValue<bool>("ShowLayOverFilters");
                    availability.Trip.SearchFiltersIn.ShowSortingandFilters = _configuration.GetValue<bool>("ShowSortingandFilters");

                    // Mixed Cabin toggle feature
                    if (_shoppingUtility.IsMixedCabinFilerEnabled(organizeResultsRequest.Application.Id, organizeResultsRequest.Application.Version.Minor) && availability.AwardTravel)
                    {
                        bool mixedCabinFlightExists = false;
                        if (organizeResultsRequest.SearchFiltersIn?.AdditionalToggles != null)
                        {
                            mixedCabinFlightExists = true;
                        }
                        if (mixedCabinFlightExists)
                        {
                            availability.Trip.SearchFiltersIn.AdditionalToggles = organizeResultsRequest.SearchFiltersIn.AdditionalToggles;
                        }
                        else
                        {
                            if (availability.Trip.SearchFiltersIn.AdditionalToggles != null)
                            {
                                availability.Trip.SearchFiltersIn.AdditionalToggles?.RemoveWhere(c => c.Key == _configuration.GetValue<string>("MixedCabinToggleKey"));
                            }
                        }
                    }
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.Message;
                            //Bug 62673:Bug 336824:FS: Mobile Unhandled Exception CSL Service\Organize Results - Ravitheja - Sep 14, 2016
                            if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10046"))
                                throw new MOBUnitedException(_configuration.GetValue<bool>("BookingExceptionMessage_ServiceErrorSessionNotFound").ToString());
                        }

                        throw new System.Exception(errorMessage);
                    }
                    else
                    {
                        throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0"));
                    }
                }
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0"));
            }

            return availability;
        }

        private List<MOBSearchFilterItem> UpdateSortTypes(MOBSHOPAvailability availability, ResultFilters resultFiltersUsed)
        {
            List<MOBSearchFilterItem> sortTypes = null;
            var sortTypeSelected = false;

            if (_configuration.GetValue<bool>("IsEnabledFsrRedesignFooterSortring"))
            {
                if (availability.Trip.Columns?.Count > 0)
                {
                    sortTypes = GetDefaultFsrRedesignFooterSortTypes();
                    foreach (var column in availability.Trip.Columns)
                    {
                        var value = ($"{column.LongCabin} {column.Description}").Trim();
                        sortTypes.Add(new MOBSearchFilterItem()
                        {
                            Key = column.Type,
                            Value = value,
                            DisplayValue = value
                        });
                    }

                    if (availability.Trip.SearchFiltersOut?.SortTypes?.Count() > 0)
                    {
                        availability.Trip.SearchFiltersOut.SortTypes = sortTypes;
                    }
                    if (availability.Trip.SearchFiltersIn?.SortTypes?.Count() > 0)
                    {
                        availability.Trip.SearchFiltersIn.SortTypes = sortTypes;
                    }

                    if (_configuration.GetValue<bool>("FixSortSelectionIssue18831") && sortTypes != null && sortTypes.Count > 0)
                    {
                        foreach (var sortType in sortTypes)
                        {
                            if (sortType?.Key?.Trim().ToUpper() == resultFiltersUsed?.SortType.ToString().Trim().ToUpper()
                                || (resultFiltersUsed?.SortType == SortTypes.PriceLowToHigh && sortType?.Key?.Trim().ToUpper()
                                    == resultFiltersUsed?.PricingSortProductType?.Trim().ToUpper()))
                            {
                                sortType.IsSelected = true;
                                sortTypeSelected = true;
                            }
                        }

                        if (!sortTypeSelected)
                        {
                            sortTypes[0].IsSelected = true;
                        }
                    }
                }
            }

            return sortTypes;
        }


        private async Task<List<Flight>> GetOrganizeResultsLMXFlights(List<Flight> tempFlights, string token, string cartID, string sessionId, int applicationId, string appVersion, string deviceId)
        {
            #region
            List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights = null;
            lmxFlights = await GetLmxFlights(token, cartID, GetFlightHasListForLMX(tempFlights), sessionId, applicationId, appVersion, deviceId);
            if (lmxFlights != null && lmxFlights.Count > 0)
            {
                PopulateLMX(lmxFlights, ref tempFlights);
            }
            return tempFlights;
            #endregion
        }

        private async System.Threading.Tasks.Task SaveCSLShopResponseForTripPlannerOrganizeResults(Session session, OrganizeResultsResponse responseOrganizeResults)
        {
            if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == MOBTripPlannerType.TPSearch.ToString())
            {

                var persistCSLShopResponse = await _sessionHelperService.GetSession<CSLShopResponse>(session.SessionId, new CSLShopResponse().ObjectName, new List<string> { session.SessionId, new CSLShopResponse().ObjectName });

                if ((persistCSLShopResponse?.ShopCSLResponse?.Trips?.Count ?? 0) > 0)
                {
                    persistCSLShopResponse.ShopCSLResponse.Trips[persistCSLShopResponse.ShopCSLResponse.LastTripIndexRequested - 1].Flights = responseOrganizeResults.OrganizedFlights;
                }
                await _sessionHelperService.SaveSession<CSLShopResponse>(persistCSLShopResponse, session.SessionId, new List<string> { session.SessionId, new CSLShopResponse().ObjectName }, new CSLShopResponse().ObjectName, saveJsonOnCloudXMLOnPrem: false);
            }
        }

        private async Task<OrganizeResultsRequest> GetOrganizeResultRequest(string cartID, ShopOrganizeResultsReqeust organizeResultsRequest, MOBSHOPAvailability availability, Session session, bool isRemoveAppliedWheelChairFilter = false)
        {
            #region
            OrganizeResultsRequest request = new OrganizeResultsRequest();
            if (_configuration.GetValue<bool>("EnableTripPlannerView") && session.TravelType == MOBTripPlannerType.TPSearch.ToString() && organizeResultsRequest.LastTripIndexRequested > 1)
            {
                request.CartId = organizeResultsRequest.CartId;
            }
            else
            {
                request.CartId = cartID;
            }
            request.OverrideTripIndex = organizeResultsRequest.LastTripIndexRequested;
            request.DebugOutput = _configuration.GetValue<bool>("OrganizeResultsRequestDebugOutput");
            ResultFilters resultsFilters = new ResultFilters();

            ///53509 - Booking FSR: Bug 339177 CSL - Booking regression Sort by price low to high on International flights does not sorting the flight search results
            ///183636 - Booking FSR mApp: "Price (Low-High)" sorting is not functional
            ///Srini  01/18/2018            

            MOBSHOPShopRequest shopRequest = null;
            if (_configuration.GetValue<bool>("BugFixToggleFor18B"))
            {
                ShoppingResponse shopResponse = new ShoppingResponse();
                shopResponse = await _sessionHelperService.GetSession<ShoppingResponse>(session.SessionId, shopResponse.ObjectName, new List<string> { session.SessionId, shopResponse.ObjectName }).ConfigureAwait(false);
                shopRequest = shopResponse.Request;
                resultsFilters.PricingSortProductType = GetFareFamily(shopRequest.Trips[request.OverrideTripIndex - 1].Cabin, shopRequest.FareType);
            }


            resultsFilters.PaginationOptions = new PaginationCriteria();
            resultsFilters.PaginationOptions.Page = organizeResultsRequest.SearchFiltersIn.PageNumber;
            resultsFilters.PaginationOptions.PageSize = _configuration.GetValue<int>("ShopAndSelectTripCSLRequestPageSize");
            if (!_configuration.GetValue<bool>("ReturnAllRemainingShopFlightsWithOnly2PageCount"))
            {
                resultsFilters.PaginationOptions.PageSize = _configuration.GetValue<int>("OrgarnizeResultsRequestPageSize");
            }
            if (resultsFilters.PaginationOptions.Page == 1 || organizeResultsRequest.SearchFiltersIn.FilterSortPaging) // means calling organize results for pagin only so no need to pass all other filters and Sorts
            {
                #region
                resultsFilters.Filters = new FilterOptions();
                #region organizeResultsRequest.SearchFiltersIn.NumberofStops
                if (organizeResultsRequest.SearchFiltersIn.NumberofStops != null)
                {
                    StopTypes stopTypes = StopTypes.NoneSpecified;
                    foreach (var stopType in organizeResultsRequest.SearchFiltersIn.NumberofStops)
                    {
                        if (stopType.IsSelected)
                        {
                            if (stopTypes == StopTypes.NoneSpecified)
                            {
                                stopTypes = (StopTypes)Enum.Parse(typeof(StopTypes), stopType.Key.Trim(), true);
                            }
                            else
                            {
                                stopTypes = (stopTypes | (StopTypes)Enum.Parse(typeof(StopTypes), stopType.Key.Trim(), true));
                            }
                        }
                    }
                    resultsFilters.Filters.PriceScheduleOptions = new PriceScheduleFilter();
                    resultsFilters.Filters.PriceScheduleOptions.Stops = stopTypes;
                }
                #endregion
                bool isNotConnectionsChangd = IsConnectionFilterChanged(organizeResultsRequest, availability);
                if (!isNotConnectionsChangd)
                {
                    resultsFilters.Filters.AirportOptions = new AirportFilter();
                    if (organizeResultsRequest.SearchFiltersIn.AirportsStopList != null)
                    {
                        List<string> airortList = new List<string>();
                        foreach (var searchFilter in organizeResultsRequest.SearchFiltersIn.AirportsStopList)
                        {
                            if (searchFilter.IsSelected)
                            {
                                airortList.Add(searchFilter.Key);
                            }
                        }
                        if (_configuration.GetValue<bool>("BugFixToggleFor17M") && airortList.Count == 0)
                        {
                            airortList.Add(string.Empty);
                        }
                        resultsFilters.Filters.AirportOptions.Connections = airortList.ToArray();
                    }
                }
                if (organizeResultsRequest.SearchFiltersIn.AirportsOriginList != null)
                {
                    List<string> airortList = new List<string>();
                    foreach (var searchFilter in organizeResultsRequest.SearchFiltersIn.AirportsOriginList)
                    {
                        if (searchFilter.IsSelected)
                        {
                            airortList.Add(searchFilter.Key);
                        }
                    }

                    ///180616 : mApp: Flights count number dropping if we apply Sort option on FSR screen for LAX-ORD market
                    ///Srini 02/01/2018
                    if (_configuration.GetValue<bool>("BugFixToggleFor18B"))
                    {
                        if (airortList.Count > 0)
                        {
                            if (resultsFilters.Filters.AirportOptions == null)
                            {
                                resultsFilters.Filters.AirportOptions = new AirportFilter();
                            }
                            resultsFilters.Filters.AirportOptions.Origins = airortList.ToArray();
                        }
                    }
                    else
                    {
                        resultsFilters.Filters.AirportOptions.Origins = airortList.ToArray();

                    }
                }
                if (organizeResultsRequest.SearchFiltersIn.AirportsDestinationList != null)
                {
                    List<string> airortList = new List<string>();
                    foreach (var searchFilter in organizeResultsRequest.SearchFiltersIn.AirportsDestinationList)
                    {
                        if (searchFilter.IsSelected)
                        {
                            airortList.Add(searchFilter.Key);
                        }
                    }
                    ///180616 : mApp: Flights count number dropping if we apply Sort option on FSR screen for LAX-ORD market
                    ///Srini 02/01/2018
                    if (_configuration.GetValue<bool>("BugFixToggleFor18B"))
                    {
                        if (airortList.Count > 0)
                        {
                            if (resultsFilters.Filters.AirportOptions == null)
                            {
                                resultsFilters.Filters.AirportOptions = new AirportFilter();
                            }
                            resultsFilters.Filters.AirportOptions.Destinations = airortList.ToArray();
                        }
                    }
                    else
                    {
                        resultsFilters.Filters.AirportOptions.Destinations = airortList.ToArray();
                    }
                }

                if (organizeResultsRequest.SearchFiltersIn.DurationMax > 0 && organizeResultsRequest.SearchFiltersIn.DurationMin > 0)
                {
                    resultsFilters.Filters.PriceScheduleOptions.TripDuration = new Duration();
                    int minsOffSet = 0; // As per stogo their is bug at CSL 6.0 organize results trip duration min and max mins check to fix that until they beta 2 release of CSL 6.1 he suggested a fix at mobile services.
                    minsOffSet = _configuration.GetValue<string>("MinsFixforTripDuration4CSL6.0") != null ? _configuration.GetValue<int>("MinsFixforTripDuration4CSL6.0") : 0;
                    resultsFilters.Filters.PriceScheduleOptions.TripDuration.MaxMinutes = organizeResultsRequest.SearchFiltersIn.DurationMax + minsOffSet;
                    resultsFilters.Filters.PriceScheduleOptions.TripDuration.MinMinutes = organizeResultsRequest.SearchFiltersIn.DurationMin - minsOffSet;
                }
                if (organizeResultsRequest.SearchFiltersIn.PriceMin > 0 && organizeResultsRequest.SearchFiltersIn.PriceMax > 0) // && !string.IsNullOrEmpty(organizeResultsRequest.SearchFiltersIn.FareFamily))
                {
                    resultsFilters.Filters.PriceScheduleOptions.PriceFilter = new PriceFilter();
                    resultsFilters.Filters.PriceScheduleOptions.PriceFilter.MinPrice = Convert.ToDouble(organizeResultsRequest.SearchFiltersIn.PriceMin);
                    if (_configuration.GetValue<string>("OrganizeResultsRequestDefaultMinPriceSenttoCSL") != null && _configuration.GetValue<string>("OrganizeResultsRequestDefaultMinPriceSenttoCSL").Trim() != "")
                    {
                        resultsFilters.Filters.PriceScheduleOptions.PriceFilter.MinPrice = _configuration.GetValue<double>("OrganizeResultsRequestDefaultMinPriceSenttoCSL");
                    }
                    resultsFilters.Filters.PriceScheduleOptions.PriceFilter.MaxPrice = Convert.ToDouble(organizeResultsRequest.SearchFiltersIn.PriceMax);
                    resultsFilters.Filters.PriceScheduleOptions.PriceFilter.Product = string.Empty;

                    if (!string.IsNullOrEmpty(organizeResultsRequest.SearchFiltersIn.FareFamily))
                    {
                        resultsFilters.Filters.PriceScheduleOptions.PriceFilter.Product = organizeResultsRequest.SearchFiltersIn.FareFamily;
                    }
                }

                #region Waiting on stogo for reply as we get Dep, Arr Min and Max times not dates from the shop responses searchfilterout but at organize results its expecting Dep,Arr min and max as DateTime??
                if (!string.IsNullOrEmpty(organizeResultsRequest.SearchFiltersIn.TimeDepartMin) && !string.IsNullOrEmpty(organizeResultsRequest.SearchFiltersIn.TimeDepartMax) &&
                    IsDepartTimeFilterChanged(organizeResultsRequest.SessionId, organizeResultsRequest.LastTripIndexRequested, organizeResultsRequest.SearchFiltersIn.TimeDepartMin, organizeResultsRequest.SearchFiltersIn.TimeDepartMax).Result)
                {
                    resultsFilters.Filters.PriceScheduleOptions.DepartureFilter = new TimeFilter();
                    if (_configuration.GetValue<string>("AddOneHourToMinMaxDepTimeFiltersToFix_Mobile_EST_CSL_EST_TimeZone_Bug") == null || !_configuration.GetValue<bool>("AddOneHourToMinMaxDepTimeFiltersToFix_Mobile_EST_CSL_EST_TimeZone_Bug"))
                    {
                        resultsFilters.Filters.PriceScheduleOptions.DepartureFilter.MinTime = DateTime.Parse(availability.Trip.DepartDate + " " + organizeResultsRequest.SearchFiltersIn.TimeDepartMin); // 03/10/2015 21:08 Convert to DateTime with flight date
                        resultsFilters.Filters.PriceScheduleOptions.DepartureFilter.MaxTime = DateTime.Parse(availability.Trip.DepartDate + " " + organizeResultsRequest.SearchFiltersIn.TimeDepartMax); //organizeResultsRequest.SearchFiltersIn.TimeDepartMax; // Convert to DateTime with flight date
                    }
                    else
                    {
                        resultsFilters.Filters.PriceScheduleOptions.DepartureFilter.MinTime = DateTime.Parse(availability.Trip.DepartDate + " " + organizeResultsRequest.SearchFiltersIn.TimeDepartMin).AddHours(1); // 03/10/2015 21:08 Convert to DateTime with flight date
                        resultsFilters.Filters.PriceScheduleOptions.DepartureFilter.MaxTime = DateTime.Parse(availability.Trip.DepartDate + " " + organizeResultsRequest.SearchFiltersIn.TimeDepartMax).AddHours(1); //organizeResultsRequest.SearchFiltersIn.TimeDepartMax; // Convert to DateTime with flight date
                    }

                    if (_configuration.GetValue<bool>("Mobile-14344BugFixToggle"))
                    {
                        resultsFilters.Filters.PriceScheduleOptions.DepartureFilter.MinTimeStr = DateTime.Parse(availability.Trip.DepartDate + " " + organizeResultsRequest.SearchFiltersIn.TimeDepartMin).ToString("MM/dd/yyyy HH:mm:ss");
                        resultsFilters.Filters.PriceScheduleOptions.DepartureFilter.MaxTimeStr = DateTime.Parse(availability.Trip.DepartDate + " " + organizeResultsRequest.SearchFiltersIn.TimeDepartMax).ToString("MM/dd/yyyy HH:mm:ss");
                    }
                }


                if (!string.IsNullOrEmpty(organizeResultsRequest.SearchFiltersIn.TimeArrivalMin) && !string.IsNullOrEmpty(organizeResultsRequest.SearchFiltersIn.TimeArrivalMax))
                {
                    resultsFilters.Filters.PriceScheduleOptions.ArrivalFilter = new TimeFilter();

                    if (_configuration.GetValue<bool>("EnableFixMobile14343") &&
                       !string.IsNullOrEmpty(availability.Trip.SearchFiltersIn.MaxArrivalDate) &&
                       !string.IsNullOrEmpty(availability.Trip.SearchFiltersIn.MinArrivalDate))
                    {
                        DateTime dateValue;

                        if (DateTime.TryParse(availability.Trip.SearchFiltersIn.MaxArrivalDate, out dateValue))
                        {
                            resultsFilters.Filters.PriceScheduleOptions.ArrivalFilter.MaxTimeStr = dateValue.ToString("MM/dd/yyyy HH:mm:ss");
                        }

                        if (DateTime.TryParse(availability.Trip.SearchFiltersIn.MinArrivalDate, out dateValue))
                        {
                            resultsFilters.Filters.PriceScheduleOptions.ArrivalFilter.MinTimeStr = dateValue.ToString("MM/dd/yyyy HH:mm:ss");
                        }
                    }
                    else
                    {
                        if (_configuration.GetValue<string>("AddOneHourToMinMaxArrivalTimeFiltersToFix_Mobile_EST_CSL_EST_TimeZone_Bug") == null || !_configuration.GetValue<bool>("AddOneHourToMinMaxArrivalTimeFiltersToFix_Mobile_EST_CSL_EST_TimeZone_Bug"))
                        {
                            #region
                            resultsFilters.Filters.PriceScheduleOptions.ArrivalFilter.MinTime = DateTime.Parse(availability.Trip.DepartDate + " " + organizeResultsRequest.SearchFiltersIn.TimeArrivalMin);//organizeResultsRequest.SearchFiltersIn.TimeArrivalMin; // Convert to DateTime with flight date
                            resultsFilters.Filters.PriceScheduleOptions.ArrivalFilter.MaxTime = DateTime.Parse(availability.Trip.ArrivalDate + " " + organizeResultsRequest.SearchFiltersIn.TimeArrivalMax);//organizeResultsRequest.SearchFiltersIn.TimeArrivalMax; // Convert to DateTime with flight date
                            if (_configuration.GetValue<string>("MinsFixforTripDuration4CSL6.0") != null && _configuration.GetValue<int>("MinsFixforTripDuration4CSL6.0") > 0) // this is work around for carrier filter issue at CSL 6.0 Organize results and it will fix at CSL 6.2 so no need to pass this when we moved to CSL 6.2 organize results call()
                            {
                                resultsFilters.Filters.PriceScheduleOptions.ArrivalFilter.MaxTime = DateTime.Parse(availability.Trip.ArrivalDate + " " + organizeResultsRequest.SearchFiltersIn.TimeArrivalMax).AddDays(2);
                                //this is the work around fix for internation flights reach the destination 2 days after the depart date until it will fix at later version of CSL 6.1 or 6.2 SHOP and ORganize Restuls () 
                                //NOTE: CSL SHOP or Select Trip is returning only the Arrival Min and Max times as string type not the date as below sample
                                // TimeArrivalMin=06:25 and TimeArrivalMax=09:35 and CSL Organize Results request is expecting Arrival Filter MinTime and MaxTime as date time but here we don't know the max arrival date so this need to 
                                // fixed at Later verison of CSL at SHOP and Select Trip response searchfilters TimeArrivalMin and TimeArrivalMax should return date time.
                            }
                            #endregion
                        }
                        else
                        {
                            #region
                            resultsFilters.Filters.PriceScheduleOptions.ArrivalFilter.MinTime = DateTime.Parse(availability.Trip.DepartDate + " " + organizeResultsRequest.SearchFiltersIn.TimeArrivalMin).AddHours(1);//organizeResultsRequest.SearchFiltersIn.TimeArrivalMin; // Convert to DateTime with flight date
                            resultsFilters.Filters.PriceScheduleOptions.ArrivalFilter.MaxTime = DateTime.Parse(availability.Trip.ArrivalDate + " " + organizeResultsRequest.SearchFiltersIn.TimeArrivalMax).AddHours(1);//organizeResultsRequest.SearchFiltersIn.TimeArrivalMax; // Convert to DateTime with flight date
                            if (_configuration.GetValue<string>("MinsFixforTripDuration4CSL6.0") != null && _configuration.GetValue<int>("MinsFixforTripDuration4CSL6.0") > 0) // this is work around for carrier filter issue at CSL 6.0 Organize results and it will fix at CSL 6.2 so no need to pass this when we moved to CSL 6.2 organize results call()
                            {
                                resultsFilters.Filters.PriceScheduleOptions.ArrivalFilter.MaxTime = DateTime.Parse(availability.Trip.ArrivalDate + " " + organizeResultsRequest.SearchFiltersIn.TimeArrivalMax).AddDays(2).AddHours(1);
                                //this is the work around fix for internation flights reach the destination 2 days after the depart date until it will fix at later version of CSL 6.1 or 6.2 SHOP and ORganize Restuls () 
                                //NOTE: CSL SHOP or Select Trip is returning only the Arrival Min and Max times as string type not the date as below sample
                                // TimeArrivalMin=06:25 and TimeArrivalMax=09:35 and CSL Organize Results request is expecting Arrival Filter MinTime and MaxTime as date time but here we don't know the max arrival date so this need to 
                                // fixed at Later verison of CSL at SHOP and Select Trip response searchfilters TimeArrivalMin and TimeArrivalMax should return date time.
                            }
                            #endregion
                        }
                        if (_configuration.GetValue<bool>("EnableFixMobile16174"))
                        {
                            resultsFilters.Filters.PriceScheduleOptions.ArrivalFilter.MaxTimeStr = resultsFilters.Filters.PriceScheduleOptions.ArrivalFilter.MaxTime.ToString("MM/dd/yyyy HH:mm:ss");
                            resultsFilters.Filters.PriceScheduleOptions.ArrivalFilter.MinTimeStr = resultsFilters.Filters.PriceScheduleOptions.ArrivalFilter.MinTime.ToString("MM/dd/yyyy HH:mm:ss");
                        }
                    }
                }
                #endregion

                resultsFilters.Filters.ExperienceOptions = new ExperienceFilter();
                if (organizeResultsRequest.SearchFiltersIn.WarningsFilter != null)
                {
                    #region // As per stogo only the below 4 types are show to client for Advisories
                    resultsFilters.Filters.ExperienceOptions.Advisories = new AdvisoryTypes();
                    AdvisoryTypes advisoryTypes = AdvisoryTypes.NoneSpecified;
                    foreach (var warning in organizeResultsRequest.SearchFiltersIn.WarningsFilter)
                    {
                        if (warning.IsSelected)
                        {
                            if (advisoryTypes == AdvisoryTypes.NoneSpecified)
                            {
                                advisoryTypes = (AdvisoryTypes)Enum.Parse(typeof(AdvisoryTypes), warning.Key.Trim(), true);
                            }
                            else
                            {
                                advisoryTypes = (advisoryTypes | (AdvisoryTypes)Enum.Parse(typeof(AdvisoryTypes), warning.Key.Trim(), true));
                            }
                        }
                    }
                    #endregion
                    resultsFilters.Filters.ExperienceOptions.Advisories = advisoryTypes;
                }

                resultsFilters.Filters.ExperienceOptions.Amenities = new AmenityTypes();
                resultsFilters.Filters.ExperienceOptions.Amenities = AmenityTypes.NoneSpecified;
                if (organizeResultsRequest.SearchFiltersIn.AmenityTypes != null)
                {
                    #region
                    AmenityTypes selectedAmenityTypes = new AmenityTypes();
                    selectedAmenityTypes = AmenityTypes.NoneSpecified;
                    foreach (var amenityType in organizeResultsRequest.SearchFiltersIn.AmenityTypes)
                    {
                        if (amenityType.IsSelected)
                        {
                            if (selectedAmenityTypes == AmenityTypes.NoneSpecified)
                            {
                                selectedAmenityTypes = (AmenityTypes)Enum.Parse(typeof(AmenityTypes), amenityType.Key.Trim(), true);
                            }
                            else
                            {
                                selectedAmenityTypes = (selectedAmenityTypes | (AmenityTypes)Enum.Parse(typeof(AmenityTypes), amenityType.Key.Trim(), true));
                            }
                        }
                    }
                    #endregion
                    resultsFilters.Filters.ExperienceOptions.Amenities = selectedAmenityTypes;
                }

                resultsFilters.Filters.ExperienceOptions.Carriers = new CarrierTypes();
                resultsFilters.Filters.ExperienceOptions.Carriers = CarrierTypes.NoneSpecified;
                if (organizeResultsRequest.SearchFiltersIn.CarrierTypes != null)
                {
                    #region
                    CarrierTypes selectedCarrierTypes = new CarrierTypes();
                    selectedCarrierTypes = CarrierTypes.NoneSpecified;
                    foreach (var carrierType in organizeResultsRequest.SearchFiltersIn.CarrierTypes)
                    {
                        if (carrierType.IsSelected)
                        {
                            if (selectedCarrierTypes == CarrierTypes.NoneSpecified)
                            {
                                selectedCarrierTypes = (CarrierTypes)Enum.Parse(typeof(CarrierTypes), carrierType.Key.Trim(), true);
                            }
                            else
                            {
                                selectedCarrierTypes = (selectedCarrierTypes | (CarrierTypes)Enum.Parse(typeof(CarrierTypes), carrierType.Key.Trim(), true));
                            }
                        }
                    }
                    #endregion
                    if (_configuration.GetValue<string>("MinsFixforTripDuration4CSL6.0") != null && _configuration.GetValue<int>("MinsFixforTripDuration4CSL6.0") > 0) // this is work around for carrier filter issue at CSL 6.0 Organize results and it will fix at CSL 6.2 so no need to pass this when we moved to CSL 6.2 organize results call()
                    {
                        selectedCarrierTypes = selectedCarrierTypes | CarrierTypes.Other;
                    }
                    resultsFilters.Filters.ExperienceOptions.Carriers = selectedCarrierTypes;
                }


                resultsFilters.Filters.ExperienceOptions.Equipments = new AircraftTypes();
                resultsFilters.Filters.ExperienceOptions.Equipments = AircraftTypes.NoneSpecified;
                if (organizeResultsRequest.SearchFiltersIn.AircraftCabinTypes != null)
                {
                    #region
                    AircraftTypes selectedAircraftCabinTypes = new AircraftTypes();
                    selectedAircraftCabinTypes = AircraftTypes.NoneSpecified;
                    foreach (var aircraftCabinType in organizeResultsRequest.SearchFiltersIn.AircraftCabinTypes)
                    {
                        if (aircraftCabinType.IsSelected)
                        {
                            if (selectedAircraftCabinTypes == AircraftTypes.NoneSpecified)
                            {
                                selectedAircraftCabinTypes = (AircraftTypes)Enum.Parse(typeof(AircraftTypes), aircraftCabinType.Key.Trim(), true);
                            }
                            else
                            {
                                selectedAircraftCabinTypes = (selectedAircraftCabinTypes | (AircraftTypes)Enum.Parse(typeof(AircraftTypes), aircraftCabinType.Key.Trim(), true));
                            }
                        }
                    }
                    #endregion
                    resultsFilters.Filters.ExperienceOptions.Equipments = selectedAircraftCabinTypes;
                }

                #region
                if (!string.IsNullOrEmpty(organizeResultsRequest.SearchFiltersIn.SortType1))
                {
                    if (session.IsFSRRedesign && _configuration.GetValue<bool>("IsEnabledFsrRedesignFooterSortring"))
                    {
                        if (Enum.TryParse<SortTypes>(organizeResultsRequest.SearchFiltersIn.SortType1.Trim(), out SortTypes sortType))
                        {
                            resultsFilters.SortType = sortType;
                        }
                        else
                        {
                            resultsFilters.PricingSortProductType = organizeResultsRequest.SearchFiltersIn.SortType1;
                            resultsFilters.SortType = SortTypes.PriceLowToHigh;
                        }
                    }
                    else
                    {
                        resultsFilters.SortType = new SortTypes();
                        resultsFilters.SortType = (SortTypes)Enum.Parse(typeof(SortTypes), organizeResultsRequest.SearchFiltersIn.SortType1.Trim(), true);
                    }
                }
                #endregion
                if ((await _featureToggles.IsEnableWheelchairFilterOnFSR(organizeResultsRequest.Application.Id, organizeResultsRequest.Application.Version.Major, session.CatalogItems).ConfigureAwait(false) || (session.IsReshopChange && await _featureToggles.IsEnableReshopWheelchairFilterOnFSR(organizeResultsRequest.Application.Id, organizeResultsRequest.Application.Version.Major, session.CatalogItems).ConfigureAwait(false)))
                && organizeResultsRequest.SearchFiltersIn?.WheelchairFilter != null 
                && organizeResultsRequest.SearchFiltersIn?.WheelchairFilter[0].IsSelected == true
                && organizeResultsRequest.SearchFiltersIn?.WheelchairFilterContent != null
                && organizeResultsRequest.SearchFiltersIn?.WheelchairFilterContent.DimensionInfo != null)
                {
                    var width = organizeResultsRequest.SearchFiltersIn.WheelchairFilterContent.DimensionInfo.Width;
                    var height = organizeResultsRequest.SearchFiltersIn.WheelchairFilterContent.DimensionInfo.Height;
                    if (organizeResultsRequest.SearchFiltersIn?.WheelchairFilterContent.DimensionInfo.Units.ToUpper().Trim() != "INCHES")
                    {
                        width = _shoppingUtility.ConvertToInches(width);
                        height = _shoppingUtility.ConvertToInches(width);
                    }
                    resultsFilters.Filters.AirCraftDoorSizes = new List<AirCraftDoorSizesFilter>
                    { 
                        new AirCraftDoorSizesFilter (){ Height =  height.ToString(), Width = width.ToString()}
                    };
                }
                else if (isRemoveAppliedWheelChairFilter)
                {
                    request.Characteristics ??= new Collection<Service.Presentation.CommonModel.Characteristic>();
                    request.Characteristics.Add(new Service.Presentation.CommonModel.Characteristic { Code = "WheelChairFilterRemove", Value = "true" });
                }
                #endregion
            }
            request.ResultFilters = resultsFilters;

            // Refundable fares toggle feature
            if (IsEnableRefundableFaresToggle(organizeResultsRequest.Application.Id, organizeResultsRequest.Application.Version.Major) &&
               organizeResultsRequest.SearchFiltersIn?.RefundableFaresToggle != null)
            {
                if (_configuration.GetValue<bool>("EnableFixMobile20733ForRefundables"))
                {
                    request.FareType = _configuration.GetValue<string>("RefundableFaresToggleFareType");
                }
                if (request.Characteristics == null) request.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                request.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "filterflightsbytoggle", Value = "true" });
                request.ShopIndicators = new ShopIndicators();
                request.ShopIndicators.IsBESelected = session.IsBEFareDisplayAtFSR;
                request.ShopIndicators.IsRefundableSelected = organizeResultsRequest.SearchFiltersIn.RefundableFaresToggle.IsSelected;
            }
            // MixedCabin toggle feature
            if (_shoppingUtility.IsMixedCabinFilerEnabled(organizeResultsRequest.Application.Id, organizeResultsRequest.Application.Version.Major))
            {
                if (shopRequest == null)
                {
                    ShoppingResponse shopResponse = new ShoppingResponse();
                    //shopResponse = United.Persist.FilePersist.Load<United.Persist.Definition.Shopping.ShoppingResponse>(organizeResultsRequest.SessionId, shopResponse.ObjectName);
                    shopResponse = await _sessionHelperService.GetSession<ShoppingResponse>(organizeResultsRequest.SessionId, shopResponse.ObjectName, new List<string> { organizeResultsRequest.SessionId, shopResponse.ObjectName });
                    shopRequest = shopResponse.Request;
                }
                if (shopRequest.AwardTravel)
                {
                    if (request.Characteristics == null) request.Characteristics = new Collection<United.Service.Presentation.CommonModel.Characteristic>();
                    if (request.Characteristics.Any(f => f.Code == "filterflightsbytoggle" && f.Value == "true") == false)
                    {
                        request.Characteristics.Add(new United.Service.Presentation.CommonModel.Characteristic() { Code = "filterflightsbytoggle", Value = "true" });
                    }
                    request.FareType = _configuration.GetValue<string>("MixedCabinToggle");
                    request.AwardTravel = shopRequest.AwardTravel;
                    if (request.ShopIndicators == null)
                    {
                        request.ShopIndicators = new ShopIndicators();
                    }
                    request.ShopIndicators.IsMixedToggleSelected = organizeResultsRequest.SearchFiltersIn?.AdditionalToggles?.FirstOrDefault(a => a.Key == _configuration.GetValue<string>("MixedCabinToggleKey"))?.IsSelected ?? false;
                }
            }

            if (await _featureSettings.GetFeatureSettingValue("EnableFSRETCTravelCreditsFeature").ConfigureAwait(false) && session.PricingType == PricingType.ETC.ToString())
            {
                request.ShopIndicators.IsTravelCreditsApplied = true;
            }

            return request;
            #endregion
        }

        public async Task<bool> IsDepartTimeFilterChanged(string sessionid, int tripIndex, string timeDepartMin, string timeDepartMax)
        {
            bool isDepartTimeFilterChanged = false;
            try
            {
                if (_configuration.GetValue<bool>("BugFixToggleFor18B"))
                {
                    List<Model.Shopping.MOBSHOPFlattenedFlight> fFlights = null;

                    var mobSHOPFlattenedFlightList = await _sessionHelperService.GetSession<MOBSHOPFlattenedFlightList>(sessionid, new MOBSHOPFlattenedFlightList().ObjectName, new List<string> { sessionid, new MOBSHOPFlattenedFlightList().ObjectName });

                    MOBSHOPAvailability nonStopsAvailability = await GetLastTripAvailabilityFromPersist(tripIndex, sessionid);
                    if (mobSHOPFlattenedFlightList != null && mobSHOPFlattenedFlightList.FlattenedFlightList?.Count >= nonStopsAvailability.Trip.FlattenedFlights.Count)
                    {
                        fFlights = mobSHOPFlattenedFlightList.FlattenedFlightList;
                    }
                    else if (nonStopsAvailability != null && nonStopsAvailability.Trip != null && nonStopsAvailability.Trip.FlattenedFlights != null)
                    {
                        fFlights = nonStopsAvailability.Trip.FlattenedFlights;
                    }

                    //.Where(ff => Convert.ToDateTime(ff.Flights[0].DepartureDateTime) < Convert.ToDateTime(availability.Trip.DepartDate).AddDays(1))
                    var flight1 = fFlights.OrderBy(ff => DateTime.Parse(ff.Flights[0].DepartureDateTime).TimeOfDay).ToList()[0];
                    var flight2 = fFlights.OrderByDescending(ff => DateTime.Parse(ff.Flights[0].DepartureDateTime).TimeOfDay).ToList()[0];
                    isDepartTimeFilterChanged = (Convert.ToDateTime(flight1.Flights[0].DepartureDateTime).TimeOfDay < TimeSpan.Parse(timeDepartMin) ||
                                            Convert.ToDateTime(flight2.Flights[0].DepartureDateTime).TimeOfDay > TimeSpan.Parse(timeDepartMax));
                }
            }
            catch
            {
                isDepartTimeFilterChanged = true;
            }
            return isDepartTimeFilterChanged;
        }

        private bool IsConnectionFilterChanged(ShopOrganizeResultsReqeust organizeResultsRequest, MOBSHOPAvailability availability)
        {
            bool isNotConnectionsChangd = true;
            try
            {
                if (availability != null && availability.Trip != null && availability.Trip.SearchFiltersIn != null && !availability.Trip.SearchFiltersIn.AirportsStopList.IsListNullOrEmpty() &&
                   organizeResultsRequest != null && organizeResultsRequest.SearchFiltersIn != null && !organizeResultsRequest.SearchFiltersIn.AirportsStopList.IsListNullOrEmpty())
                {
                    foreach (var apersist in availability.Trip.SearchFiltersIn.AirportsStopList)
                    {
                        if (!organizeResultsRequest.SearchFiltersIn.AirportsStopList.Where(a1 => a1.IsSelected).Exists(a1 => a1.Key == apersist.Key))
                        {
                            isNotConnectionsChangd = false;
                            break;
                        }
                    }
                }
            }
            catch
            {
                isNotConnectionsChangd = false;
            }

            return isNotConnectionsChangd;
        }
        private async Task<List<RewardProgram>> GetRewardPrograms(int applicationId, string deviceId, string appVersion, string transactionId, string sessionID, string token)
        {
            var rewardPrograms = new List<RewardProgram>();
            var response = new Service.Presentation.ReferenceDataResponseModel.RewardProgramResponse
            {
                Programs = (await _referencedataService.RewardPrograms<Collection<Program>>(token, sessionID)).Response
            };

            if (response?.Programs?.Count > 0)
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
                    _logger.LogError("GetRewardPrograms - Response Error {@response}", JsonConvert.SerializeObject(response));
                }
            }

            return rewardPrograms;
        }

        private MOBStyledText StrikeThroughPerCentageBadge(string percentage)
        {
            return new MOBStyledText()
            {
                Text = string.Format(_configuration.GetValue<string>("StrikeThroughPercentageProductBadgeText"), percentage),
                SortPriority = MOBFlightProductBadgeSortOrder.SaverAward.ToString(),
                TextColor = _configuration.GetValue<string>("StrikeThroughPercentageProductBadgeColorCode")

            };
        }

        private MOBStyledText SaverAwardBadge()
        {
            return new MOBStyledText()
            {
                Text = _configuration.GetValue<string>("SaverAwardProductBadgeText"),
                SortPriority = MOBFlightProductBadgeSortOrder.SaverAward.ToString(),
                TextColor = _configuration.GetValue<string>("SaverAwardColorCode")

            };
        }

        private MOBStyledText UADiscountBadge()
        {
            return new MOBStyledText()
            {
                Text = _configuration.GetValue<string>("UADiscountProductBadgeText"),
                SortPriority = MOBFlightProductBadgeSortOrder.MixedCabin.ToString(),
                TextColor = _configuration.GetValue<string>("UADiscountColorCode")
            };
        }
        private MOBStyledText YADiscountBadge(string yaDiscount = "")
        {
            return new MOBStyledText()
            {
                Text = yaDiscount,
                SortPriority = MOBFlightProductBadgeSortOrder.YADiscounted.ToString(),
                TextColor = _configuration.GetValue<string>("YADiscountColorCode")

            };
        }
        private MOBStyledText CorporateFareIndicatorBadge(string corporateFareIndicator = "")
        {
            return new MOBStyledText()
            {
                Text = corporateFareIndicator,
                SortPriority = corporateFareIndicator == _configuration.GetValue<string>("CorporateFareIndicator") ? MOBFlightProductBadgeSortOrder.CorporateDiscounted.ToString() : MOBFlightProductBadgeSortOrder.BreakFromBusiness.ToString(),
                TextColor = corporateFareIndicator == _configuration.GetValue<string>("CorporateFareIndicator") ? _configuration.GetValue<string>("CorporateFareColorCode") : _configuration.GetValue<string>("BreakFromBusinessFareColorCode")
            };
        }

        private MOBStyledText CorporateTravelOutOfPolicy(string outOfPolicyText)
        {
            return new MOBStyledText()
            {
                Text = outOfPolicyText,
                SortPriority = MOBFlightProductBadgeSortOrder.OutOfPolicy.ToString()
            };
        }

        private Session _CurrentSession;
        public void SetCurrentSession(Session session)
        {
            _CurrentSession = session;
        }

        public string formatAwardAmountForDisplay(string amt, bool truncate = true)
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
                                    newAmt = string.Format("{0:n1}", amount) + "K miles";
                                else
                                    newAmt = string.Format("{0:n0}", amount) + "K miles";
                            }
                            else
                            {
                                newAmt = string.Format("{0:n0}", amount) + " miles";
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

            }
            catch { }

            return newAmt;
        }

        public async Task StrikeThroughContentMessages(MOBSHOPAvailability availability, MOBAdditionalItems additionalItems, Session session, MOBRequest mOBRequest)
        {
            if (_configuration.GetValue<bool>("EnableAwardStrikeThroughPricing") && session.IsAward && session.CatalogItems != null && session.CatalogItems.Count > 0 &&
                                       session.CatalogItems.FirstOrDefault(a => a.Id == ((int)Common.Helper.Profile.IOSCatalogEnum.AwardStrikeThroughPricing).ToString() || a.Id == ((int)Common.Helper.Profile.AndroidCatalogEnum.AwardStrikeThroughPricing).ToString())?.CurrentValue == "1"
                                       && additionalItems != null && additionalItems.StrikeThroughPricing == true)
            {
                if (availability.ContentMessages == null)
                {
                    availability.ContentMessages = new List<MOBMobileCMSContentMessages>();
                }
                List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(mOBRequest, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");

                MOBMobileCMSContentMessages mOBMobileCMSContent = new MOBMobileCMSContentMessages
                {
                    LocationCode = "AwardSpecialMemberPricingToolTip",
                };
                List<MOBMobileCMSContentMessages> listOfMessages = null;
                if (await _featureSettings.GetFeatureSettingValue("EnableAwardStrikeThroughPriceEnhancement").ConfigureAwait(false) && !string.IsNullOrEmpty(additionalItems.StrikeThroughTripDisplayType))
                {
                    listOfMessages = ShopStaticUtility.GetSDLMessageFromList(lstMessages, _configuration.GetValue<string>("AwardStrikeThroughPriceTooltipContent")+"."+additionalItems.StrikeThroughTripDisplayType);
                }
                if (listOfMessages == null || listOfMessages.Count == 0)
                {
                    listOfMessages = ShopStaticUtility.GetSDLMessageFromList(lstMessages, _configuration.GetValue<string>("AwardStrikeThroughPriceTooltipContent"));
                }
                if(listOfMessages != null && listOfMessages.Count > 0)
                {

                    mOBMobileCMSContent.Title = listOfMessages.FirstOrDefault().HeadLine;
                    mOBMobileCMSContent.ContentShort = listOfMessages.FirstOrDefault().ContentFull;
                }

                listOfMessages = ShopStaticUtility.GetSDLMessageFromList(lstMessages, _configuration.GetValue<string>("AwardStrikeThroughPriceTooltipAdditionalContent"));
                mOBMobileCMSContent.HeadLine = listOfMessages.FirstOrDefault().HeadLine;
                mOBMobileCMSContent.ContentFull = listOfMessages.FirstOrDefault().ContentFull;

                availability.ContentMessages.Add(mOBMobileCMSContent);
            }
        }

        private async Task<ShopRequest> GetShopRequestTripPlan(MOBTripPlanShopHelper shopRequest)
        {
            var cslShoprequest = new ShopRequest();
            //shopResponse = null;
            string cartID = shopRequest.TripPlanCartId;
            var tripPlanResponse = _sessionHelperService.GetSession<TripPlanCCEResponse>(shopRequest.TPSessionId, (new TripPlanCCEResponse()).ObjectName, new List<string> { shopRequest.TPSessionId, (new TripPlanCCEResponse()).ObjectName });
            if (shopRequest.MobShopRequest.TravelType == TravelType.TPEdit.ToString())
            {
                cslShoprequest = tripPlanResponse.Result.TripPlanTrips.First(t => t.CslShopRequest?.Trips != null).CslShopRequest;

                cslShoprequest.Trips?.ForEach(t => t.Flights = null);
                cslShoprequest.Trips[0].SearchFiltersIn.StopCountMin = -1;
                cslShoprequest.Trips[0].SearchFiltersIn.StopCountMax = -1;

                await _sessionHelperService.SaveSession<string>($"{shopRequest.TripPlanId}|{shopRequest.TripPlanCartId}", shopRequest.TPSessionId, new List<string> { shopRequest.TPSessionId, ObjectNames.TripPlanEditCartDetailsFullName }, ObjectNames.TripPlanEditCartDetailsFullName).ConfigureAwait(false);

            }
            else
            {
                cslShoprequest = tripPlanResponse.Result.TripPlanTrips.First(t => t.CartID.Equals(cartID, StringComparison.OrdinalIgnoreCase)).CslShopRequest;
                //shopResponse = tripPlanResponse.TripPlanTrips.First(t => t.CartID.Equals(cartID, StringComparison.OrdinalIgnoreCase)).CslShopResponse;

                if (shopRequest.IsTravelCountChanged)
                {
                    cslShoprequest.PaxInfoList.Clear();

                    GetPaxInfo(shopRequest.MobShopRequest, cslShoprequest);

                }

                cslShoprequest.TravelPlanId = shopRequest.TripPlanId;
                cslShoprequest.TravelPlanCartId = shopRequest.TripPlanCartId;
                cslShoprequest.EliteLevel = shopRequest.MobShopRequest.PremierStatusLevel;
                cslShoprequest.LoyaltyId = shopRequest.MobShopRequest.MileagePlusAccountNumber;
            }
            cslShoprequest.CartId = null;
            //persis CSL shop request so we nave Loyalty info without making multiple summary calls
            CSLShopRequest persistCslShopRequest = new CSLShopRequest();

            persistCslShopRequest.ShopRequest = cslShoprequest;
            await _sessionHelperService.SaveSession<CSLShopRequest>(persistCslShopRequest, shopRequest.MobShopRequest.SessionId, new List<string> { shopRequest.MobShopRequest.SessionId, persistCslShopRequest.ObjectName }, persistCslShopRequest.ObjectName).ConfigureAwait(false);

            //FilePersist.Save<string>(shopRequest.MobShopRequest.SessionId, "TripPlanBookingSelectionDetails", $"{ shopRequest.TripPlanId}|{shopRequest.CartId}");
            //return cslShoprequest;
            //}
            return cslShoprequest;
        }
        private List<MOBFareComparison> PopulateFareComparisonMessageforAirlines(MOBSHOPAvailability availability, MOBAdditionalItems additionalItems, List<CMSContentMessage> lstMessages)
        {
            List<MOBFareComparison> mOBFareComparisons = new List<MOBFareComparison>();
            if (availability.Trip?.FlattenedFlights?.Count > 0 && additionalItems.AirlineCodes != null && additionalItems.AirlineCodes.Count > 0)
            {
                var supportedAirlines = _configuration.GetValue<string>("SupportedAirlinesFareComparison").ToString().Split(',');
                foreach (var airline in supportedAirlines)
                {
                    if (additionalItems.AirlineCodes.Contains(airline))
                    {
                        foreach (var columns in availability.Trip?.Columns)
                        {
                            string sdlMessage = GetSDLStringMessageFromList(lstMessages, _configuration.GetValue<string>("FareComparison") + "." + airline.ToUpper() + "_" + columns.Type);

                            MOBFareComparison mOBFareComparison = PopulateAirlineFareCompareSDLContent(airline, columns.Type, sdlMessage);
                            if (mOBFareComparison != null && _configuration.GetValue<bool>("IsEnableJSXDisclaimerText"))
                            {
                                string jsxDisclosureMessage = GetSDLStringMessageFromList(lstMessages, _configuration.GetValue<string>("FareComparisonDisclosure") + "." + airline.ToUpper() + "_" + columns.Type);
                                mOBFareComparison.Disclosure = string.IsNullOrEmpty(jsxDisclosureMessage) ? string.Empty : jsxDisclosureMessage;
                            }
                            if (!mOBFareComparison.FareHeader.IsNullOrEmpty())
                            {
                                mOBFareComparisons.Add(mOBFareComparison);
                            }
                        }
                    }
                }
            }
            return mOBFareComparisons.Count == 0 ? null : mOBFareComparisons;
        }

        private MOBFareComparison PopulateAirlineFareCompareSDLContent(string operatingCarrier, string fareFamily, string sdlMessage)
        {

            MOBFareComparison mOBFareComparison = new MOBFareComparison();

            if (!string.IsNullOrEmpty(sdlMessage))
            {
                mOBFareComparison = JsonConvert.DeserializeObject<MOBFareComparison>(sdlMessage);
            }
            return mOBFareComparison;

        }
        private string GetSDLStringMessageFromList(List<CMSContentMessage> list, string title)
        {
            return list?.Where(x => x.Title.Equals(title))?.FirstOrDefault()?.ContentFull?.Trim();
        }
    }
}
