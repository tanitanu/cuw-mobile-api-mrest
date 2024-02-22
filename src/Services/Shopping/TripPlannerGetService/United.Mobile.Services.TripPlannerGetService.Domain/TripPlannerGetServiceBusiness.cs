using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.ShopBundles;
using United.Mobile.DataAccess.TeaserPage;
using United.Mobile.DataAccess.TripPlanGetService;
using United.Mobile.DataAccess.TripPlannerGetService;
using United.Mobile.Model.Booking;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.SSR;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.TripPlannerGetService;
using United.Service.Presentation.PersonalizationModel;
using United.Service.Presentation.PersonalizationRequestModel;
using United.Service.Presentation.PersonalizationResponseModel;
using United.Service.Presentation.ReferenceDataRequestModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.Boombox;
using United.Services.FlightShopping.Common.Extensions;
using United.Services.FlightShopping.Common.LMX;
using United.Services.FlightShopping.Common.OrganizeResults;
using United.TravelPlanner.Models;
using United.Utility.Enum;
using United.Utility.Helper;
using CreditType = United.Mobile.Model.Shopping.CreditType;
using CreditTypeColor = United.Mobile.Model.Shopping.CreditTypeColor;
using CSLShopRequest = United.Mobile.Model.TripPlannerGetService.CSLShopRequest;
using CSLShopResponse = United.Mobile.Model.TripPlannerGetService.CSLShopResponse;
using MOBActionButton = United.Mobile.Model.TripPlannerGetService.MOBActionButton;
using MOBTripPlanOnboarding = United.Mobile.Model.TripPlannerGetService.MOBTripPlanOnboarding;
using MOBTripPlanOnboardItem = United.Mobile.Model.TripPlannerGetService.MOBTripPlanOnboardItem;
using Product = United.Services.FlightShopping.Common.Product;
using ShopResponse = United.Services.FlightShopping.Common.ShopResponse;
using TripPlanCCEResponse = United.Mobile.Model.TripPlannerGetService.TripPlanCCEResponse;
using TripPlanTile = United.Mobile.Model.TripPlannerGetService.TripPlanTile;
using TripPlanTrip = United.Mobile.Model.TripPlannerGetService.TripPlanTrip;

namespace United.Mobile.Services.TripPlannerGetService.Domain
{
    public class TripPlannerGetServiceBusiness : ITripPlannerGetServiceBusiness
    {
        private readonly ICacheLog<TripPlannerGetServiceBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly IBundleOfferService _bundleOfferService;
        private readonly ITravelPlannerService _travelPlannerService;
        private readonly IGetTeaserColumnInfoService _getTeaserColumnInfoService;
        private readonly IDPService _dPService;
        private readonly IFlightShoppingService _flightShoppingService;
        private readonly ILMXInfo _lmxInfo;
        private readonly IShoppingUtility _shoppingUtility;
        private AirportDetailsList airportsList;
        public static string CURRENCY_TYPE_MILES = "miles";
        public static string PRICING_TYPE_CLOSE_IN_FEE = "CLOSEINFEE";
        private MOBProductSettings configurationProductSettings;
        private readonly ITripPlannerIDService _tripPlannerIDService;
        private HttpContext _httpContext;
        private readonly ICachingService _cachingService;
        private readonly IReferencedataService _referencedataService;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly IAuroraMySqlService _auroraMySqlService;
        private readonly IFeatureSettings _featureSettings;
        private readonly IFeatureToggles _featureToggles;
        public TripPlannerGetServiceBusiness(ICacheLog<TripPlannerGetServiceBusiness> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IShoppingSessionHelper shoppingSessionHelper
            , IBundleOfferService bundleOfferService
            , ITravelPlannerService travelPlannerService
            , IGetTeaserColumnInfoService getTeaserColumnInfoService
            , IDPService dPService
            , IFlightShoppingService flightShoppingService
            , ILMXInfo lmxInfo
            , IShoppingUtility shoppingUtility
            , ITripPlannerIDService tripPlannerIDService
            , ICachingService cachingService
            , IReferencedataService referencedataService
            , IFFCShoppingcs fFCShoppingcs
            , IAuroraMySqlService auroraMySqlService
            , IFeatureSettings featureSettings
            , IFeatureToggles featureToggles)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _shoppingSessionHelper = shoppingSessionHelper;
            _bundleOfferService = bundleOfferService;
            _travelPlannerService = travelPlannerService;
            _getTeaserColumnInfoService = getTeaserColumnInfoService;
            _dPService = dPService;
            _flightShoppingService = flightShoppingService;
            _lmxInfo = lmxInfo;
            _shoppingUtility = shoppingUtility;
            _tripPlannerIDService = tripPlannerIDService;
            _cachingService = cachingService;
            _referencedataService = referencedataService;
            _fFCShoppingcs = fFCShoppingcs;
            configurationProductSettings = _configuration.GetSection("productSettings").Get<MOBProductSettings>();
            _auroraMySqlService = auroraMySqlService;
            _featureSettings = featureSettings;
            _featureToggles = featureToggles;
        }
        public async Task<MOBTripPlanSummaryResponse> GetTripPlanSummary(MOBTripPlanSummaryRequest request)
        {
            MOBTripPlanSummaryResponse response = new MOBTripPlanSummaryResponse();
            Session session = null;

            if (string.IsNullOrEmpty(request.SessionId) && string.IsNullOrEmpty(request.TripPlanId))
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

            //ValidateRequest(request);
            if (!string.IsNullOrEmpty(request.SessionId))
            {
                //Pilot
                session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId);
            }
            else
            {
                //Co-Pilot
                session = await _shoppingSessionHelper.CreateShoppingSession(request.Application.Id, request.DeviceId, request.Application.Version.Major,
                    request.TransactionId, request.MpNumber, string.Empty, false, false, false, MOBTripPlannerType.TPSearch.ToString());
                await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string>() { session.SessionId, session.ObjectName }, session.ObjectName);

            }
            if (_configuration.GetValue<bool>("EnableDeepLinkErrorLog") && request.TravelType == TravelType.TPDeepLink.ToString())
            {
                _logger.LogInformation("GetTripPlanSummary DeepLink {@Message} - {@TripPlanId} ", _configuration.GetValue<string>("TripPlanNotAvailableMessage"), request.TripPlanId);
            }
            #region Call to DAL

            if ((!string.IsNullOrEmpty(request.SessionId) && string.IsNullOrEmpty(request.TripPlanId))
                || request.TravelType == TravelType.TPTripDelete.ToString() || request.TravelType == TravelType.TPEdit.ToString())
            {
                response = await TripPlannerSummary(request);
            }
            else if (!request.TripPlanId.IsNullOrEmpty()) //retrive by tripplanid.
            {
                request.SessionId = session.SessionId;
                response = await TripPlannerSummaryByTripPlannerId(request);
            }
            session.TravelType = MOBTripPlannerType.TPBooking.ToString();
            await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string>() { request.SessionId, session.ObjectName }, session.ObjectName);


            response.SessionId = request.SessionId;
            response.Request = request;
            response.TransactionId = request.TransactionId;
            response.LanguageCode = request.LanguageCode;
            #endregion
            return await Task.FromResult(response);

        }
        public async Task<MOBSHOPSelectTripResponse> SelectTripTripPlanner(MOBSHOPSelectTripRequest request, HttpContext httpContext)
        {
            _httpContext = httpContext;
            MOBSHOPSelectTripResponse response = new MOBSHOPSelectTripResponse();
            response.Request = request;
            string logAction = "SelectTripTripPlanner";
            //bool isDefault = false;
            Session session = null;

            ShoppingResponse shop = new ShoppingResponse();
            shop = await _sessionHelperService.GetSession<ShoppingResponse>(request.SessionId, shop.ObjectName, new List<string> { request.SessionId, shop.ObjectName }).ConfigureAwait(false);
            //if (request.SessionId == Utility.GetConfigEntries("TripPlanSessionIDForFSR2Error"))
            session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId);


            #region Call to DAL

            response.TransactionId = request.TransactionId;
            response.LanguageCode = request.LanguageCode;


            int totalPassengers = 1;
            if (shop != null && shop.Request != null)
            {
                totalPassengers = ShopStaticUtility.GetTravelerCount(shop.Request.TravelerTypes);
            }
            //int tripCount = 1;
            response.Availability = await SelectTripTripPlanner(request, totalPassengers, httpContext);
            response.Disclaimer = GetDisclaimerString();

            if (_configuration.GetValue<string>("CartIdForDebug").ToString().ToUpper() == "YES")
                response.CartId = response.Availability.CartId;
            #endregion

            //response.Availability = null;

            if (response.Availability != null && response.Availability.Trip != null && response.Availability.Trip.FlattenedFlights != null)
            {
                //if (!Utility.GetBooleanConfigValue("EnableNonStopFlight")
                //|| (request.GetNonStopFlightsOnly && !response.Availability.Trip.TripHasNonStopflightsOnly) // Means First Shop Call and No Non Stop Flights Found in that Market
                //|| (!request.GetNonStopFlightsOnly && !request.GetFlightsWithStops)) //(!request.GetNonStopFlightsOnly && !request.GetFlightsWithStops) means old Clients
                if (response.Availability.Trip.PageCount == 2) //**PageCount==2==>> for paging implementation to send only 15 flights back to client and perisit remaining flights and return remaining when customer page up
                {
                    //TODO - null out FlightSections
                    //response.Availability.Trip.FlightSections = shopping.PopulateFlightSections(request.SessionId, response.Availability.Trip.FlattenedFlights);
                    response.Availability.Trip.FlightSections = null;
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

            response.IsTokenAuthenticated = session.IsTokenAuthenticated;
            if (response.Availability != null && response.Availability.Trip != null && _configuration.GetValue<bool>("HideSearchFiletersAndSort"))
            {
                if (response.Availability.Trip.SearchFiltersIn != null) { response.Availability.Trip.SearchFiltersIn = null; }
                if (response.Availability.Trip.SearchFiltersOut != null) { response.Availability.Trip.SearchFiltersOut = null; }
            }

            if (response.Availability?.Trip?.FlattenedFlights == null)
            {
                try
                {
                    AddAnotherFlightMessage(request, response, shop);
                }
                catch (Exception ex)
                {

                    MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
            }

            return await Task.FromResult(response);
        }
        public async Task<MOBTripPlanBoardResponse> GetTripPlanBoard(MOBTripPlanBoardRequest request)
        {
            var response = new MOBTripPlanBoardResponse();
            Session session = null;
            session = await _shoppingSessionHelper.CreateShoppingSession(request.Application.Id, request.DeviceId,
                request.Application.Version.Major, request.TransactionId, request.MpNumber, null, false, false, false);

            await _sessionHelperService.SaveSession<Session>(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName);
            request.SessionId = session.SessionId;
            //shopping.LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBTripPlanBoardRequest>(request.SessionId, logAction, "Request", request.Application.Id, request.Application.Version.Major, request.DeviceId, request, true, false));
            if (_configuration.GetValue<bool>("EnableDeepLinkErrorLog") && request.IsNotAvailableMessage)
            {
                var exceptionMsg = _configuration.GetValue<string>("TripPlanNotAvailableMessage");
                // shopping.LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(request.SessionId, logAction + "-deeplink-error", "Exception", request.Application.Id, request.Application.Version.Major, request.DeviceId, Utility.GetConfigEntries("TripPlanNotAvailableMessage"), true, false));
                _logger.LogError("GetTripPlanBoard deeplink-error {@Exception}", exceptionMsg);
            }
            response = await TripPlanBoard(request);
            response.Request = request;
            response.TransactionId = request.TransactionId;
            response.LanguageCode = request.LanguageCode;

            return response;
        }
        private async Task<MOBTripPlanSummaryResponse> TripPlannerSummary(MOBTripPlanSummaryRequest request)
        {
            MOBTripPlanSummaryResponse response = new MOBTripPlanSummaryResponse
            {
                TripPlanTrips = new List<MOBTripPlanTrip>()
            };
            // Build list of Csl shop request
            List<ShopRequest> shopRequests = new List<ShopRequest>();
            List<ShopResponse> shopResponses = new List<ShopResponse>();
            List<MOBTripPlanCCEMemberData> mOBTripPlanCCEMemberDatas = new List<MOBTripPlanCCEMemberData>();
            Session session = new Session();
            //try
            //{
            session = await _sessionHelperService.GetSession<Session>(request.SessionId, new Session().ObjectName, new List<string> { request.SessionId, new Session().ObjectName }).ConfigureAwait(false);
            //if (request.IsTravelCountChanged || request.TravelType == TravelType.TPTripDelete.ToString() || request.TravelType == TravelType.TPEdit.ToString())
            if (request.TravelType == TravelType.TPTripDelete.ToString() || request.TravelType == TravelType.TPEdit.ToString())
            {
                request.TripPlanId = await GetUpdatedTripPlanID(request);
            }
            else
            {
                request.TripPlanId = await GetTripPlanID(request);
            }
            response = await TripPlannerSummaryByTripPlannerId(request, true);

            return response;
        }
        private async Task<MOBTripPlanSummaryResponse> TripPlannerSummaryByTripPlannerId(MOBTripPlanSummaryRequest request, bool retry = false)
        {
            MOBTripPlanSummaryResponse response = new MOBTripPlanSummaryResponse
            {
                TripPlanTrips = new List<MOBTripPlanTrip>()
            };
            string logAction = "TripPlannerSummaryByTripPlannerId";
            // Build list of Csl shop request
            List<ShopRequest> shopRequests = new List<ShopRequest>();
            List<ShopResponse> shopResponses = new List<ShopResponse>();
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string> { request.SessionId, new Session().ObjectName }).ConfigureAwait(false);
            List<United.Service.Presentation.CommonModel.Characteristic> cceCharacteristics = new List<United.Service.Presentation.CommonModel.Characteristic> { new United.Service.Presentation.CommonModel.Characteristic { Code = "TripPlanId", Value = request.TripPlanId.Trim() } };
            MOBRequest mobRequest = request;
            var cceRequestHelper = new CCERequestHelper
            {
                MileagePlusNumber = request.MpNumber,
                ComponentToLoad = new List<string> { "TripPlannerAlert" },
                PageToLoad = "Homepage",
                MOBRequest = mobRequest,
                Characteristics = cceCharacteristics,
                SessionId = request.SessionId,
                LogAction = logAction
            };
            if (request.TravelType == TravelType.TPDeepLink.ToString())
            {
                cceRequestHelper.ComponentToLoad.Add("TravelPlanner");
            }
            ContextualCommResponse cceResponse = null;
            try
            {
                if (_configuration.GetValue<bool>("EnableRetryIfShopReqNullCCE"))
                {
                    cceResponse = await GetCCEResponseTripPlanSummary(cceRequestHelper);
                }
                else
                {
                    var jsonResponse = await GetCCEContentWithRequestor(cceRequestHelper);
                    //add ccecontextualcontentprocess log entries to shopping logentries
                    cceResponse = string.IsNullOrEmpty(jsonResponse) ? null
                    : JsonConvert.DeserializeObject<ContextualCommResponse>(jsonResponse);
                }

                if (_configuration.GetValue<bool>("EnableRetryIfShopReqNullCCE") && retry)
                {
                    if ((cceResponse?.Error?.Count ?? 0) > 0)
                    {
                        _logger.LogError("TripPlannerSummaryByTripPlannerId error: CCE trip plan alert failed first try at trip plan creation flow");
                        cceResponse = await GetCCEResponseTripPlanSummary(cceRequestHelper);
                    }
                }
                if (cceResponse == null || !cceResponse.Components.Any(x => x.Name.ToUpper() == "TRIPPLANNERALERT"))
                {
                    if (request.TravelType?.ToUpper() == TravelType.TPDeepLink.ToString().ToUpper())
                    {
                        throw new MOBUnitedException("CCE Response is null or TripPlannerAlert component is not loaded");
                    }
                    else
                    {
                        throw new Exception("CCE Response is null or TripPlannerAlert component is not loaded");
                    }
                }
                if (cceResponse.Error?.Count > 0)
                {
                    _logger.LogError("TripPlannerSummaryByTripPlannerId CCETripPlannerAlertResponse Error -{@Error}", JsonConvert.SerializeObject(cceResponse.Error.ToList()));
                }

            }
            catch (System.Exception ex)
            {
                throw ex;
            }
            if (request.TravelType == TravelType.TPDeepLink.ToString() && cceResponse != null && cceResponse.Components.Any(x => x.Name.ToUpper() == "TRAVELPLANNER"))
            {
                response.TripPlanOnboarding = GetMOBTripPlanLayout(cceResponse);
            }
            response.TripPlanId = request.TripPlanId;

            bool showShareLink = (request.ListTripPlanSelectTrip?.Count > 0 && (request.TravelType == null || request.TravelType == "TPSearch"));
            await GetTripPlanSummayResponseCCE(cceResponse, response, request.SessionId, showShareLink);
            if (response.Captions == null || response.Captions.Count == 0 || response.TripPlanTrips == null || response.TripPlanTrips.Count == 0)
            {
                throw new Exception("TripPlannerAlert CCE response is not valid (Missing either trips or captions)");
            }
            return response;
        }
        private async Task<string> GetUpdatedTripPlanID(MOBTripPlanSummaryRequest request)
        {
            //return Guid.NewGuid().ToString();
            Session session = await _sessionHelperService.GetSession<Session>(request.SessionId, new Session().ObjectName, new List<string> { request.SessionId, new Session().ObjectName }).ConfigureAwait(false);
            Model.TripPlannerGetService.TripPlanCCEResponse tpCCEResponse = await _sessionHelperService.GetSession<Model.TripPlannerGetService.TripPlanCCEResponse>(request.SessionId, new Model.TripPlannerGetService.TripPlanCCEResponse().ObjectName, new List<string> { request.SessionId, new TripPlanCCEResponse().ObjectName }).ConfigureAwait(false);

            if (request.TravelType == TravelType.TPEdit.ToString())
            {
                if (session.ShopSearchTripCount > 1)
                {
                    foreach (var st in request.ListTripPlanSelectTrip)
                    {
                        //Remove when CCE ready
                        //TripPlanTrip 
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
                    var persistCSLShopResponse = _sessionHelperService.GetSession<CSLShopResponse>(request.SessionId, new CSLShopResponse().ObjectName, new List<string> { request.SessionId, new CSLShopResponse().ObjectName }).Result.ShopCSLResponse;

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

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                // delete when CCE ready
                if (!request.IsTravelCountChanged)
                {
                    await _sessionHelperService.SaveSession<TripPlanCCEResponse>(tpCCEResponse, request.SessionId, new List<string>() { request.SessionId, new TripPlanCCEResponse().ObjectName }, new TripPlanCCEResponse().ObjectName, saveJsonOnCloudXMLOnPrem: false).ConfigureAwait(false);

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
        private async Task<string> GetTripPlanID(MOBTripPlanSummaryRequest request)
        {
            Session session = await _sessionHelperService.GetSession<Session>(request.SessionId, new Session().ObjectName, new List<string> { request.SessionId, new Session().ObjectName }).ConfigureAwait(false);
            var url = "/TravelPlanner/createtravelplan";
            TravelPlan createTripPlanRequest = await GetCreateTripPlanRequest(request, session);
            string jsonRequest = JsonConvert.SerializeObject(createTripPlanRequest);

            var jsonResponse = await _tripPlannerIDService.GetTripPlanID<string>(session.Token, request.SessionId, url, jsonRequest).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(jsonResponse))
            {

                if (!_configuration.GetValue<bool>("DisableTripPlanCreationCharDeserializeFix"))
                {
                    return jsonResponse.Trim();
                }
                else
                {
                    return jsonResponse;
                }
            }
            else
            {
                throw new Exception("Create tripplan failed");
            }

        }
        private async Task<ContextualCommResponse> GetCCEResponseTripPlanSummary(CCERequestHelper cceRequestHelper)
        {
            ContextualCommResponse cceResponse;
            var jsonResponse = await GetCCEContentWithRequestor(cceRequestHelper);

            cceResponse = string.IsNullOrEmpty(jsonResponse) ? null
            : JsonConvert.DeserializeObject<ContextualCommResponse>(jsonResponse);
            return cceResponse;
        }
        private async Task<string> GetCCEContentWithRequestor(CCERequestHelper request)
        {
            var cceRequest = CreateCCERequest(request);
            var cceResponse = await GetCCEContent(cceRequest, request.MOBRequest, request.SessionId);
            return cceResponse;
        }
        private ContextualCommRequest CreateCCERequest(CCERequestHelper request)
        {
            ContextualCommRequest cceRequest = new ContextualCommRequest();
            cceRequest.ComponentsToLoad = new Collection<string>(request.ComponentToLoad);
            cceRequest.PageToLoad = request.PageToLoad;
            cceRequest.MileagePlusId = request.MileagePlusNumber;
            cceRequest.DeviceId = request.MOBRequest.DeviceId;
            //cceRequest.SessionId = sessionId;
            cceRequest.ChannelType = _configuration.GetValue<string>("CCERequestChannelName");
            cceRequest.IPCountry = _configuration.GetValue<string>("CCERequestCountryCode");
            cceRequest.LangCode = _configuration.GetValue<string>("CCERequestLanguageCode");
            cceRequest.Browser = (request.MOBRequest.Application.Id == 1) ? "iOS" : "Android";

            if (request.Characteristics != null && request.Characteristics.Count > 0)
            {
                cceRequest.Requestor = new Service.Presentation.CommonModel.Requestor
                {
                    Characteristic = new Collection<United.Service.Presentation.CommonModel.Characteristic>()
                };

                request.Characteristics.ForEach(x =>
                {
                    cceRequest.Requestor.Characteristic.Add(new United.Service.Presentation.CommonModel.Characteristic { Code = x.Code, Value = x.Value });
                });
            }
            return cceRequest;

        }
        public async Task<string> GetCCEContent(ContextualCommRequest cceRequest, MOBRequest mobRequest, string sessionId)
        {
            var jsonRequest = JsonConvert.SerializeObject(cceRequest);
            var url = "mobile/messages";
            Session session = await _sessionHelperService.GetSession<Session>(sessionId, new Session().ObjectName, new List<string> { sessionId, new Session().ObjectName }).ConfigureAwait(false);
            var jsonResponse = await _bundleOfferService.GetCCEContent<ContextualCommResponse>(session?.Token, sessionId, url, jsonRequest);
            var cceResponse = jsonResponse.IsNullOrEmpty() ? null : jsonResponse.response;

            return cceResponse;

        }
        private MOBTripPlanOnboarding GetMOBTripPlanLayout(ContextualCommResponse cceResponse)
        {
            MOBTripPlanOnboarding cCEOnboardingLayout = null;
            TripPlanTile travelPlannerTrips = null;

            if (cceResponse != null)
            {
                try
                {
                    if (cceResponse != null && cceResponse.Components != null)
                    {
                        var tripPlanner = cceResponse.Components.FirstOrDefault(x => x.Name.ToUpper() == "TRAVELPLANNER");
                        int onboardingOrder = 0;
                        if (tripPlanner != null && tripPlanner.ContextualElements.Any())
                        {

                            cCEOnboardingLayout = new MOBTripPlanOnboarding();
                            tripPlanner.ContextualElements.ToList().ForEach(x =>
                            {
                                var valueJson = Newtonsoft.Json.JsonConvert.SerializeObject(x.Value);
                                ContextualMessage value = Newtonsoft.Json.JsonConvert.DeserializeObject<ContextualMessage>(valueJson);
                                travelPlannerTrips = ToCCETripPlanner(value);


                                if (travelPlannerTrips != null && travelPlannerTrips.Header != "")
                                {
                                    if (value.MessageKey.StartsWith("MOB.HOMEPAGE.TRAVELPLANNER.TRIPPLANNER_COPILOTV1", StringComparison.OrdinalIgnoreCase) && value.Content != null)
                                    {
                                        if (cCEOnboardingLayout.OnboardItems == null)
                                        {
                                            cCEOnboardingLayout.OnboardItems = new List<MOBTripPlanOnboardItem>();
                                        }
                                        cCEOnboardingLayout.Header = travelPlannerTrips.Header;
                                        cCEOnboardingLayout.Body = travelPlannerTrips.Body;
                                        cCEOnboardingLayout.ActionButtons = travelPlannerTrips.ActionButtons;
                                    }
                                    else if (value.MessageKey.StartsWith("MOB.HOMEPAGE.TRAVELPLANNER.STEP", StringComparison.OrdinalIgnoreCase) && value.Content != null)
                                    {
                                        if (cCEOnboardingLayout.OnboardItems == null)
                                        {
                                            cCEOnboardingLayout.OnboardItems = new List<MOBTripPlanOnboardItem>();
                                        }
                                        cCEOnboardingLayout.OnboardItems.Add(new MOBTripPlanOnboardItem
                                        {
                                            Title = travelPlannerTrips.Header,
                                            SubTitle = travelPlannerTrips.Body,
                                            ImageUrl = travelPlannerTrips.ImageUrl,
                                            Order = ++onboardingOrder
                                        });
                                    }
                                }
                            });
                        }
                    }

                    return cCEOnboardingLayout;
                }
                catch (System.Exception ex)
                {
                    return null;
                }

            }
            return cCEOnboardingLayout;
        }
        private TripPlanTile ToCCETripPlanner(ContextualMessage value)
        {
            try
            {
                TripPlanTile tripPlanTile = new TripPlanTile();

                if (value != null)
                {
                    if (value.Content != null)
                    {

                        tripPlanTile.Header = value.Content.Title ?? string.Empty;
                        if (value.MessageKey.ToUpper().StartsWith("MOB.HOMEPAGE.TRAVELPLANNER.NEXTTRIP"))
                        {
                            tripPlanTile.Body = value.Content.BodyText ?? string.Empty;
                            tripPlanTile.PillText = value.Content.SubTitle ?? string.Empty;
                        }
                        else
                        {
                            tripPlanTile.Body = value.Content.SubTitle ?? string.Empty;
                        }

                        if (value.Content.Images != null && value.Content.Images.Count > 0)
                        {
                            tripPlanTile.ImageUrl = value.Content.Images.FirstOrDefault().Url ?? string.Empty;
                        }

                        if (value.Content.Links != null && value.Content.Links.Count > 0)
                        {
                            int rank = 1;
                            tripPlanTile.ActionButtons = new System.Collections.Generic.List<MOBActionButton>();
                            foreach (var actionLink in value.Content.Links)
                            {
                                var actionButton = new MOBActionButton
                                {
                                    ActionURL = actionLink.Link ?? string.Empty,
                                    ActionText = actionLink.LinkText ?? string.Empty,
                                    IsEnabled = true,
                                    Rank = rank
                                };
                                if (rank == 1)
                                {
                                    actionButton.IsPrimary = true;
                                }
                                rank++;
                                tripPlanTile.ActionButtons.Add(actionButton);
                            }

                        }
                    }
                }
                return tripPlanTile;
            }
            catch (System.Exception ex)
            {
                return null;
            }
        }
        private async System.Threading.Tasks.Task GetTripPlanSummayResponseCCE(ContextualCommResponse cceResponse, MOBTripPlanSummaryResponse response, string sessionId, bool showShareLink)
        {
            var tripPlanner = cceResponse.Components.FirstOrDefault(x => x.Name.ToUpper() == "TRIPPLANNERALERT");
            if (tripPlanner != null && tripPlanner.ContextualElements != null)
            {

                TripPlanCCEResponse tpCCEResponse = new TripPlanCCEResponse();
                List<TripPlanTrip> tripPlanTrips = new List<TripPlanTrip>();

                List<MOBTripPlanCCEMemberData> mOBTripPlanCCEShopRequestResponse = new List<MOBTripPlanCCEMemberData>();

                List<MOBTripPlanTrip> mOBTripPlanTrips = new List<MOBTripPlanTrip>();
                bool isPilot = false;
                string tripLastUpdate = "", tripPlannerCreatorDeviceID = "", tripPlannerCreatorMPNumber = "", deviceIDs = string.Empty;
                tripPlanner.ContextualElements.ForEach(t =>
                {
                    var valueJson = Newtonsoft.Json.JsonConvert.SerializeObject(t.Value);
                    ContextualMessage value = Newtonsoft.Json.JsonConvert.DeserializeObject<ContextualMessage>(valueJson);
                    if (value != null)
                    {

                        if (value != null && (value.MessageKey.StartsWith("MOB.HOMEPAGE.TRIPPLANNERALERT.SHARETRIP_PILOT") || value.MessageKey.StartsWith("MOB.HOMEPAGE.TRIPPLANNERALERT.SHARETRIP_COPILOT")))
                        {
                            response.Captions = GetCaptionsTripPlanSummary(value, showShareLink);
                            if (value.Params != null)
                            {
                                if (value.Params.Any(y => string.Equals(y.Key, "TRIPLASTUPDATED", StringComparison.OrdinalIgnoreCase)))
                                {
                                    tripLastUpdate = value.Params.Where(y => string.Equals(y.Key, "TRIPLASTUPDATED", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault();
                                }
                                if (value.Params.Any(y => string.Equals(y.Key, "TRIPPLANNERCREATORDEVICEID", StringComparison.OrdinalIgnoreCase)))
                                {
                                    tripPlannerCreatorDeviceID = value.Params.Where(y => string.Equals(y.Key, "TRIPPLANNERCREATORDEVICEID", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault();
                                }
                                if (value.Params.Any(y => string.Equals(y.Key, "TRIPPLANNERCREATORMPNUMBER", StringComparison.OrdinalIgnoreCase)))
                                {
                                    tripPlannerCreatorMPNumber = value.Params.Where(y => string.Equals(y.Key, "TRIPPLANNERCREATORMPNUMBER", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault();
                                }
                                if (_configuration.GetValue<bool>("EnableTripPlanPushNotifications"))
                                {
                                    if (value.Params.Any(y => string.Equals(y.Key, "DEVICEIDS", StringComparison.OrdinalIgnoreCase)))
                                    {
                                        deviceIDs = value.Params.Where(y => string.Equals(y.Key, "DEVICEIDS", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault();
                                    }
                                }
                            }
                        }
                        else
                        {
                            var tripPlanTrip = LoadTripPlanTripsCCE(value);
                            if (tripPlanTrip.CartId == null)
                            {
                                tripPlanTrip.CartId = Guid.NewGuid().ToString();
                            }
                            var mOBTripPlanCCEMemberData = GetCCETripPlanShopRequest(value, tripPlanTrip.CartId);
                            mOBTripPlanCCEShopRequestResponse.Add(mOBTripPlanCCEMemberData);

                            if (value.Params != null)
                            {
                                if (value.Params.Any(y => string.Equals(y.Key, "FIRSTFLIGHTDEPARTDATE", StringComparison.OrdinalIgnoreCase)))
                                {
                                    tripPlanTrip.FirstFlightDepartDate = value.Params.Where(y => string.Equals(y.Key, "FIRSTFLIGHTDEPARTDATE", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault();
                                }
                                if (value.Params.Any(y => string.Equals(y.Key, "HIDEVOTES", StringComparison.OrdinalIgnoreCase)))
                                {
                                    tripPlanTrip.HideVotes = Convert.ToBoolean(value.Params.Where(y => string.Equals(y.Key, "HIDEVOTES", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault());
                                }
                                if ((!tripPlanTrip.AddTripBtnText.IsNullOrEmpty() || !tripPlanTrip.ItineraryUnavailableText.IsNullOrEmpty()) && !tripPlanTrip.HideVotes)
                                {
                                    tripPlanTrip.HideVotes = true;
                                }
                                if (value.Params.Any(y => string.Equals(y.Key, "ISFLIGHTAVAILABLE", StringComparison.OrdinalIgnoreCase)))
                                {
                                    tripPlanTrip.IsNotAvailable = !Convert.ToBoolean(value.Params.FirstOrDefault(y => string.Equals(y.Key, "ISFLIGHTAVAILABLE", StringComparison.OrdinalIgnoreCase)).Value);
                                }
                            }
                            mOBTripPlanTrips.Add(tripPlanTrip);

                        }
                        if (value.Params != null && value.Params.Any(y => string.Equals(y.Key, "REQUESTORVARIANT", StringComparison.OrdinalIgnoreCase)))
                        {
                            var variant = value.Params.Where(y => string.Equals(y.Key, "REQUESTORVARIANT", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault();
                            response.TripPlannerType = MOBSHOPTripPlannerType.Copilot.ToString().ToUpper();
                            if (variant == MOBSHOPTripPlannerType.Pilot.ToString().ToUpper())
                            {
                                isPilot = true;
                                response.TripPlannerType = MOBSHOPTripPlannerType.Pilot.ToString().ToUpper();
                                response.ShouldShowDeleteTrip = true;
                            }
                        }
                    }
                });
                if (mOBTripPlanTrips.Where(x => x.TripPlanLOFs?.Count > 0)?.Count() == 1 && isPilot)
                {
                    mOBTripPlanTrips.First(x => x.TripPlanLOFs?.Count > 0).EditText = null;
                }
                response.TripPlanTrips = mOBTripPlanTrips;
                response.TravelerTypes = GetTravelTypesTripPlanSummary(mOBTripPlanCCEShopRequestResponse?.FirstOrDefault(x => x?.ShopRequest != null)?.ShopRequest);

                //persist tpCCEResponse
                if (mOBTripPlanCCEShopRequestResponse.Count > 0)
                {
                    mOBTripPlanCCEShopRequestResponse.ForEach(s =>
                    {
                        tripPlanTrips.Add(new TripPlanTrip()
                        {
                            CartID = s.CartId,
                            CslShopRequest = s.ShopRequest,
                            CslShopResponse = null
                        });
                    });
                }
                tpCCEResponse.TripPlanTrips = tripPlanTrips;
                tpCCEResponse.TripPlanID = response.TripPlanId;
                tpCCEResponse.IsPilot = isPilot;
                tpCCEResponse.TripLastUpdate = tripLastUpdate;
                tpCCEResponse.TripPlannerCreatorDeviceID = tripPlannerCreatorDeviceID;
                tpCCEResponse.TripPlannerCreatorMPNumber = tripPlannerCreatorMPNumber;
                if (_configuration.GetValue<bool>("EnableTripPlanPushNotifications"))
                {
                    tpCCEResponse.DeviceIds = deviceIDs;
                }
                if (isPilot)
                {
                    response.Captions.Add(new MOBItem() { Id = "TPSummary_ItineraryDeleteConfirmationMsg ", CurrentValue = _configuration.GetValue<string>("TripPlanSummary_ItineraryDeleteConfirmationMsg"), SaveToPersist = false });
                }
                //FilePersist.Save<TripPlanCCEResponse>(sessionId, new TripPlanCCEResponse().ObjectName, tpCCEResponse);
                await _sessionHelperService.SaveSession<TripPlanCCEResponse>(tpCCEResponse, sessionId, new List<string>() { sessionId, new TripPlanCCEResponse().ObjectName }, new TripPlanCCEResponse().ObjectName, saveJsonOnCloudXMLOnPrem: false).ConfigureAwait(false);
                //response.TripPlanShareLink = $"Join United's trip planner to plan your next group trip. https://qa9.united.com/ual/en/us/fly/travel/tripplanner.html?tpuid={ response.TripPlanId } Download the united app to get latest updates.";
                response.TripPlanShareLink = string.Format(_configuration.GetValue<string>("TripPlanDeepLinkUrl"), response.TripPlanId);
            }
            else
            {
                if (showShareLink)
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                throw new MOBUnitedException(_configuration.GetValue<string>("TripPlanNotAvailableMessage"));

            }
        }
        private List<MOBItem> GetCaptionsTripPlanSummary(ContextualMessage value, bool showShareLink)
        {
            var captions = new List<MOBItem>();

            if (value != null && (value.MessageKey.StartsWith("MOB.HOMEPAGE.TRIPPLANNERALERT.SHARETRIP_PILOT") || value.MessageKey.StartsWith("MOB.HOMEPAGE.TRIPPLANNERALERT.SHARETRIP_COPILOT")))
            {
                if (value.Content != null)
                {
                    captions.Add(new MOBItem() { Id = "TPSummary_PageTitle", CurrentValue = _configuration.GetValue<string>("TripPlanPageTitle"), SaveToPersist = false });
                    captions.Add(new MOBItem() { Id = "TPSummary_HeaderText", CurrentValue = value.Content.Title, SaveToPersist = false });
                    captions.Add(new MOBItem() { Id = "TPSummary_LastUpdatedText", CurrentValue = value.Content.SubTitle, SaveToPersist = false });
                    captions.Add(new MOBItem() { Id = "TPSummary_SubHeaderText", CurrentValue = value.Content.SubTitle, SaveToPersist = false });
                    captions.Add(new MOBItem() { Id = "TPSummary_BodyText", CurrentValue = value.Content.SubTitle2, SaveToPersist = false });
                    captions.Add(new MOBItem() { Id = "TPSummary_ShareTripPlanHeaderText", CurrentValue = _configuration.GetValue<string>("TripPlanShareViewHeaderText"), SaveToPersist = false });
                    captions.Add(new MOBItem() { Id = "TPSummary_ShareTripPlanEmailSubjectText", CurrentValue = _configuration.GetValue<string>("TripPlanShareViewEmailSubjectText"), SaveToPersist = false });
                    if (value.Content.Links != null && value.Content.Links.Any())
                    {
                        value.Content.Links.ForEach(l =>
                        {
                            if (l.LinkStyle.StartsWith("LINK_SHARETRIP") && showShareLink)
                            {
                                captions.Add(new MOBItem() { Id = "TPSummary_ShareButtonText", CurrentValue = l.LinkText, SaveToPersist = false });
                            }

                        });
                    }

                }
            }
            return captions;
        }
        private MOBTripPlanTrip LoadTripPlanTripsCCE(ContextualMessage value)
        {
            List<MOBTripPlanTrip> mOBTripPlanTrips = new List<MOBTripPlanTrip>();
            MOBTripPlanTrip mOBTripPlanTrip = null;
            bool isPilot = false, isVoted = false, hideVotes = false, isFlightAvailable = false, hideEdit = false;
            string cartId = string.Empty, voteId = string.Empty, messageType = string.Empty, requestOrVariant = string.Empty;
            int numberOfBookings = 0, numberOfVotes = 0;

            if (value != null)
            {
                if (value.Params != null && value.Params.Any())
                {
                    cartId = value.Params.Where(y => string.Equals(y.Key, "CARTID", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault();
                    voteId = value.Params.Where(y => string.Equals(y.Key, "UNIQUEVOTEID", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault();
                    messageType = value.Params.Where(y => string.Equals(y.Key, "CARTID", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault();
                    voteId = value.Params.Where(y => string.Equals(y.Key, "UNIQUEVOTEID", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault();
                    isVoted = Convert.ToBoolean(value.Params.Where(y => string.Equals(y.Key, "ENABLEVOTES", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault());
                    hideVotes = Convert.ToBoolean(value.Params.Where(y => string.Equals(y.Key, "HIDEVOTES", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault());
                    isFlightAvailable = Convert.ToBoolean(value.Params.Where(y => string.Equals(y.Key, "ISFLIGHTAVAILABLE", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault());
                    numberOfBookings = Convert.ToInt32(value.Params.Where(y => string.Equals(y.Key, "NUMBEROFBOOKINGS", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault());
                    numberOfVotes = Convert.ToInt32(value.Params.Where(y => string.Equals(y.Key, "NUBMBEROFVOTES", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault());
                    requestOrVariant = value.Params.Where(y => string.Equals(y.Key, "REQUESTORVARIANT", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault();
                    hideEdit = Convert.ToBoolean(value.Params.Where(y => string.Equals(y.Key, "HIDEEDITLINK", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault());
                    if (requestOrVariant == MOBSHOPTripPlannerType.Pilot.ToString().ToUpper())
                    {
                        isPilot = true;
                    }
                }
                if (value.Content != null)
                {
                    mOBTripPlanTrip = new MOBTripPlanTrip
                    {
                        ItineraryTitle = value.Content.Title,
                        ItineraryPriceText = value.Content.Summary,
                        BookingText = value.Content.SubTitle2,
                        VoteText = value.Content.SubTitle

                    };
                    //Itinary deleted or Itinary Not Available
                    if (value.Content.BodyText != "" && !isPilot)
                    {
                        mOBTripPlanTrip.ItineraryUnavailableText = value.Content.BodyText;
                    }
                    // Purchased Tag or Badge
                    if (!value.Content.SubTitle3.IsNullOrEmpty() && numberOfBookings > 0)
                    {
                        mOBTripPlanTrip.Badges = new List<MOBStyledText>
                        {
                            new MOBStyledText
                                {
                                    Text = value.Content.SubTitle3,
                                    BackgroundColor = MOBStyledColor.Green.GetDescription(),
                                    TextColor = MOBStyledColor.White.GetDescription()
                                }
                        };
                    }

                    mOBTripPlanTrip.CartId = cartId;
                    mOBTripPlanTrip.NumberOfBookings = numberOfBookings;
                    mOBTripPlanTrip.NumberOfVotes = numberOfVotes;
                    if (!voteId.IsNullOrEmpty())
                    {
                        mOBTripPlanTrip.VoteInfo = new MOBTripVoteInfo
                        {
                            VoteId = voteId,
                            IsVoted = isVoted
                        };
                    }
                    if (value.Content.Links != null && value.Content.Links.Count > 0)
                    {
                        if (isPilot)
                        {
                            if (!hideEdit)
                            {
                                mOBTripPlanTrip.EditText = value.Content.Links.FirstOrDefault(lk => lk.LinkStyle.StartsWith("LINK_EDIT"))?.LinkText;
                            }
                            var addAnotherFlight = value.Content.Links.FirstOrDefault(lk => lk.LinkStyle.StartsWith("LINK_SEARCH"));
                            if (addAnotherFlight != null)
                            {
                                //Add Another Flight 
                                mOBTripPlanTrip.AddTripBtnText = addAnotherFlight.LinkText;

                            }
                        }
                        // Book link or Book Again Link
                        mOBTripPlanTrip.BookLinkText = value.Content.Links.FirstOrDefault(lk => (lk.LinkStyle.StartsWith("LINK_BOOK") || lk.LinkStyle.StartsWith("LINK_BOOK_AGAIN")))?.LinkText;
                        if (numberOfBookings > 0)
                        {
                            //View More 
                            mOBTripPlanTrip.PnrDetailsLinkText = value.Content.Links.FirstOrDefault(lk => lk.LinkStyle.StartsWith("LINK_VIEW"))?.LinkText;
                        }
                    }
                    if (value.Content.SubContents != null)
                    {
                        var subContentValueJson = Newtonsoft.Json.JsonConvert.SerializeObject(value.Content.SubContents);
                        var valueLOFs = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ContextualContent>>(subContentValueJson);
                        if (valueLOFs != null && valueLOFs.Count > 0)
                        {
                            if (mOBTripPlanTrip.TripPlanLOFs == null)
                            {
                                mOBTripPlanTrip.TripPlanLOFs = new List<MOBTripPlanLOF>();
                            }
                            valueLOFs.ForEach(lof =>
                            {
                                if (lof != null)
                                {
                                    var tripPlanLOF = new MOBTripPlanLOF
                                    {
                                        Title = lof.Title,
                                        SubTitle = lof.SubTitle
                                    };
                                    mOBTripPlanTrip.TripPlanLOFs.Add(tripPlanLOF);
                                }
                            });
                        }
                    }
                    mOBTripPlanTrips.Add(mOBTripPlanTrip);
                }
            }


            return mOBTripPlanTrip;


        }
        private MOBTripPlanCCEMemberData GetCCETripPlanShopRequest(ContextualMessage value, string cartID)
        {
            List<MOBTripPlanCCEMemberData> mOBTripPlanCCEMemberDatas = new List<MOBTripPlanCCEMemberData>();
            MOBTripPlanCCEMemberData memberData = new MOBTripPlanCCEMemberData();
            List<Tuple<string, ShopRequest, ShopResponse>> tupleTripPlanCCEMemberDatas;
            if (value != null)
            {
                if (value.MessageData != null)
                {
                    var memberDataJson = Newtonsoft.Json.JsonConvert.DeserializeObject<ContextualElement>(Newtonsoft.Json.JsonConvert.SerializeObject(value.MessageData));
                    //Get new model from CCE 
                    if (memberDataJson != null)
                    {
                        tupleTripPlanCCEMemberDatas = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Tuple<string, ShopRequest, ShopResponse>>>(Newtonsoft.Json.JsonConvert.SerializeObject(memberDataJson.Value));
                        memberData.CartId = tupleTripPlanCCEMemberDatas.First().Item1;
                        memberData.ShopRequest = tupleTripPlanCCEMemberDatas.First().Item2;
                        // memberData.ShopResponse = tupleTripPlanCCEMemberDatas.FirstOrDefault()?.Item3 //populate FirstFlightDepartDate CCE is sending
                        mOBTripPlanCCEMemberDatas.Add(memberData);
                    }
                }
                else
                {
                    return new MOBTripPlanCCEMemberData()
                    {
                        CartId = cartID
                    };
                }
            }

            return memberData;
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
        private async Task<string> UpdateTripPlan(TravelPlan tripPlanRequest, Session session, MOBRequest request)
        {
            var url = "/TravelPlanner/UpdateTravelPlan";
            string jsonRequest = JsonConvert.SerializeObject(tripPlanRequest);
            var jsonResponse = await _tripPlannerIDService.GetTripPlanID<string>(session.Token, session.SessionId, url, jsonRequest);
            if (jsonResponse != null)
            {
                return jsonResponse;
            }
            else
            {
                throw new Exception("Update tripplan failed");
            }

        }
        private List<MOBTravelerType> GetTravelTypesTripPlanSummary(ShopRequest persistCSLShopRequest)
        {

            var mOBTravelerTypes = new List<MOBTravelerType>();
            if (persistCSLShopRequest != null)
            {
                GetTravelersListFromCSLShop(persistCSLShopRequest, mOBTravelerTypes);
            }
            return mOBTravelerTypes;

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
        private async Task<TravelPlan> GetCreateTripPlanRequest(MOBTripPlanSummaryRequest request, Session session)
        {

            TravelPlan travelPlanrequest = new TravelPlan();
            travelPlanrequest.CreatorDeviceID = request.DeviceId;
            travelPlanrequest.CreatorMpID = session.MileagPlusNumber;
            travelPlanrequest.TravelPlanID = Guid.NewGuid().ToString();
            travelPlanrequest.LastUpdated = DateTime.Now;
            travelPlanrequest.FlightOptions = new Collection<FlightOption>();
            int id = 0;
            if (session.ShopSearchTripCount > 1)
            {
                foreach (var st in request.ListTripPlanSelectTrip)
                {
                    string cartID = Guid.NewGuid().ToString();
                    ShopRequest persistCSLShopRequest = null;
                    ShopResponse persistCSLShopResponse = null;
                    //TripPlanTrip 
                    if (!string.IsNullOrEmpty(st?.CartId) && !string.IsNullOrEmpty(st?.ProductId))
                    {
                        var cslSelectTrip = await _sessionHelperService.GetSession<CSLSelectTrip>(st.CartId, new CSLSelectTrip().ObjectName, new List<string> { st.CartId, new CSLSelectTrip().ObjectName }).ConfigureAwait(false);
                        persistCSLShopRequest = cslSelectTrip.ShopCSLRequest;
                        persistCSLShopResponse = cslSelectTrip.ShopCSLResponse;

                        persistCSLShopRequest.FlexibleDaysAfter = 0;
                        persistCSLShopRequest.FlexibleDaysBefore = 0;

                        persistCSLShopRequest.Trips[persistCSLShopResponse.LastTripIndexRequested - 1].Flights.Add(persistCSLShopResponse.Trips[persistCSLShopResponse.LastTripIndexRequested - 1].Flights.First(f => f.BBXSolutionSetId == st.TripId &&
                        f.Products.Any(p => p.ProductId == st.ProductId)));

                        persistCSLShopRequest.Trips[persistCSLShopResponse.LastTripIndexRequested - 1].ColumnInformation = persistCSLShopResponse.Trips[persistCSLShopResponse.LastTripIndexRequested - 1].ColumnInformation;

                        persistCSLShopRequest.CartId = cartID;
                    }

                    travelPlanrequest.FlightOptions.Add(
                    new FlightOption()
                    {
                        ID = ++id,
                        ShoppingCartID = cartID,
                        ShopRequest = persistCSLShopRequest,
                    }
                  );
                }

            }
            else
            {
                var persistCSLShopResponse = _sessionHelperService.GetSession<CSLShopResponse>(request.SessionId, new CSLShopResponse().ObjectName, new List<string> { request.SessionId, new CSLShopResponse().ObjectName }).Result.ShopCSLResponse;
                var persistCSLShopRequest = _sessionHelperService.GetSession<CSLShopRequest>(request.SessionId, new CSLShopRequest().ObjectName, new List<string> { request.SessionId, new CSLShopRequest().ObjectName }).Result.ShopRequest;


                persistCSLShopRequest.Trips[0].ColumnInformation = persistCSLShopResponse.Trips[0].ColumnInformation;

                foreach (var st in request.ListTripPlanSelectTrip)
                {
                    var _cslShopRequest = persistCSLShopRequest.CloneDeep();
                    _cslShopRequest.Trips[0].Flights.Add(persistCSLShopResponse.Trips[0].Flights.First(f => f.BBXSolutionSetId == st.TripId &&
                f.Products.Any(p => p.ProductId == st.ProductId)));

                    string cartID = Guid.NewGuid().ToString();
                    _cslShopRequest.CartId = cartID;

                    _cslShopRequest.FlexibleDaysBefore = 0;
                    _cslShopRequest.FlexibleDaysAfter = 0;

                    travelPlanrequest.FlightOptions.Add(
                    new FlightOption()
                    {
                        ID = ++id,
                        ShoppingCartID = cartID,
                        ShopRequest = _cslShopRequest
                    }
                  );
                }
            }
            return travelPlanrequest;
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

        private async System.Threading.Tasks.Task AddAnotherFlightMessage(MOBSHOPSelectTripRequest request, MOBSHOPSelectTripResponse response, ShoppingResponse shop)
        {
            response.Availability = new MOBSHOPAvailability()
            {
                Reservation = new MOBSHOPReservation(_configuration, _cachingService)
                {
                    Trips = new List<MOBSHOPTrip>()
                    {
                        new MOBSHOPTrip()
                        {
                            FlattenedFlights = new List<MOBSHOPFlattenedFlight>()
                        }
                    }
                }
            };

            var mobSHOPFlattenedFlightList = await _sessionHelperService.GetSession<MOBSHOPFlattenedFlightList>(request.SessionId, new MOBSHOPFlattenedFlightList().ObjectName, new List<string> { request.SessionId, new MOBSHOPFlattenedFlightList().ObjectName });

            if (mobSHOPFlattenedFlightList == null)
            {
                var persistedAvail = await GetLastTripAvailabilityFromPersist(request.TripId, request.SessionId);
                mobSHOPFlattenedFlightList = new MOBSHOPFlattenedFlightList();
                //List<MOBSHOPFlattenedFlight> shopFlattenedFlightList = null;
                mobSHOPFlattenedFlightList.FlattenedFlightList = persistedAvail.Trip.FlattenedFlights;
            }

            response.Availability.Reservation.Trips[0].FlattenedFlights.Add(mobSHOPFlattenedFlightList.FlattenedFlightList.First(ff => ff.Flights.Any(f => f.FlightId == request.FlightId && f.TripId == request.TripId && f.ShoppingProducts.Any(p => p.ProductId == request.ProductId))));
            response.Availability.TpNoAvailabilityMsg = _configuration.GetValue<string>("TripPlanFSR2NoAvailabilityBody");
            response.Availability.TpSelectAnotherFlightLinkTxt = _configuration.GetValue<string>("TripPlanFSR2NoAvailabilityButtonText");
            response.Availability.SessionId = request.SessionId;
            response.Availability.CartId = Guid.NewGuid().ToString();

        }
        private async Task<MOBSHOPAvailability> GetLastTripAvailabilityFromPersist(string tripID, string sessionID)
        {
            MOBSHOPAvailability lastTripAvailability = null;

            if (string.IsNullOrWhiteSpace(tripID))
                return lastTripAvailability;

            var persistAvailability = new LatestShopAvailabilityResponse();
            persistAvailability = await _sessionHelperService.GetSession<LatestShopAvailabilityResponse>(sessionID, persistAvailability.ObjectName, new List<string> { sessionID, persistAvailability.ObjectName });

            if (persistAvailability != null && persistAvailability.AvailabilityList != null && persistAvailability.AvailabilityList.Any())
            {
                lastTripAvailability = persistAvailability.AvailabilityList.Values.FirstOrDefault(x => x.Trip.TripId.Equals(tripID, StringComparison.OrdinalIgnoreCase));
            }

            return lastTripAvailability;
        }
        private async Task<MOBSHOPAvailability> SelectTripTripPlanner(MOBSHOPSelectTripRequest selectRequest, int totalPassengers, HttpContext httpContext)
        {
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(selectRequest.SessionId, session.ObjectName, new List<string> { selectRequest.SessionId, session.ObjectName });

            if (session == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }

            MOBSHOPAvailability availability = new MOBSHOPAvailability();

            ShoppingResponse persistShop = new ShoppingResponse();
            persistShop = await _sessionHelperService.GetSession<ShoppingResponse>(selectRequest.SessionId, persistShop.ObjectName, new List<string> { selectRequest.SessionId, persistShop.ObjectName }).ConfigureAwait(false);
            if (persistShop == null)
            {
                throw new MOBUnitedException("Could not find your booking session.");
            }

            //-------Feature 208204--- Common class data carrier for hirarchy methds-----
            MOBSHOPDataCarrier _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
            _mOBSHOPDataCarrier.SearchType = persistShop.Request.SearchType;
            if (_configuration.GetValue<bool>("Shopping - bPricingBySlice"))
            {
                availability.PriceTextDescription = GetPriceTextDescription(persistShop.Request.SearchType);
                SetFSRFareDescriptionForShop(availability, persistShop.Request);
                _mOBSHOPDataCarrier.PriceFormText = GetPriceFromText(persistShop.Request);
            }

            ShopRequest request = null;
            request = await GetTripPlannerShopSelectRequest(selectRequest);

            string jsonRequest = DataContextJsonSerializer.Serialize<ShopRequest>(request), shopCSLCallDurations = string.Empty;

            string url = "/tripplanner/Shop";
            var jsonResponse = await _travelPlannerService.ShopTripPlanner(session.Token, selectRequest.SessionId, url, jsonRequest);
            #region// 2 = selectTripCSLCallDurationstopwatch1//****Get Call Duration Code - Venkat 03/17/2015*******
            #endregion//****Get Call Duration Code - Venkat 03/17/2015*******

            if (jsonResponse != null)
            {
                ShopResponse response = JsonConvert.DeserializeObject<ShopResponse>(jsonResponse);

                CSLShopRequest shopRequest = new CSLShopRequest();
                try
                {
                    shopRequest = await _sessionHelperService.GetSession<CSLShopRequest>(selectRequest.SessionId, shopRequest.ObjectName, new List<string> { selectRequest.SessionId, shopRequest.ObjectName });
                }
                catch (System.Exception) { }

                bool bErrorcheckflag = true; // if any errors found in response then this flag will be false. 

                if (response != null && response.Errors != null && response.Errors.Count > 0)
                {
                    bErrorcheckflag = NoCSLExceptions(response.Errors);
                }

                if (response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success) && (bErrorcheckflag))
                {
                    if (availability == null) availability = new MOBSHOPAvailability();
                    availability.SessionId = selectRequest.SessionId;
                    availability.CartId = response.CartId;
                    availability.TravelType = session.TravelType;

                    availability.Reservation = new MOBSHOPReservation(_configuration, _cachingService);
                    MOBAdditionalItems additionalItems = new MOBAdditionalItems();


                    if (response.Trips != null && response.Trips.Count > 0)
                    {

                        availability.Trip = null;
                        availability.Reservation.Trips = null;
                        List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(selectRequest, session.SessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");


                        for (int i = 0; i < response.Trips.Count; i++)
                        {
                            ///95509 - iOS/Android-Farewheel price is differ from lowest price for OW /RT/MT on Business cabin search
                            ///224324 - mApp : Booking –Revenue- Multitrip – Economy/First- Fare wheel in FSR2 is showing prices based on FSR1 cabin search and not on the FSR2 cabin search
                            ///Srini - 03/20/2018
                            if (_configuration.GetValue<bool>("BugFixToggleFor18C"))
                            {
                                _mOBSHOPDataCarrier.FsrMinPrice = 0M;
                            }

                            if (response.Trips[i] != null && (!string.IsNullOrEmpty(response.Trips[i].BBXCellIdSelected)) && response.Trips[i].Flights.Count == 1) //?
                            {
                                if (availability.Reservation == null)
                                    availability.Reservation = new MOBSHOPReservation(_configuration, _cachingService);
                                if (availability.Reservation.Trips == null)
                                    availability.Reservation.Trips = new List<MOBSHOPTrip>();
                                if ((_configuration.GetValue<string>("CheckCSLShopSelectFlightsNull") == null || Convert.ToBoolean(_configuration.GetValue<string>("CheckCSLShopSelectFlightsNull"))) && (response.Trips[i].Flights == null || response.Trips[i].Flights.Count == 0))
                                {
                                    //To ByPass this Flight Null Check if have any issues after weekly releaase 10/27 just add this entry to production web config =  <add key="CheckCSLShopSelectFlightsNull" value="false"/>
                                    // To turn on check Flight Nulls delete the entry in web config or update the value to true <add key="CheckCSLShopSelectFlightsNull" value="true"/>

                                    string actionName = "CSL Shop Select – Flights Null";
                                    _logger.LogWarning("Trace-CheckCSLShopSelectFlightsNull {actionName} {request}", actionName, jsonRequest);
                                    //61048 - Bug 331484:FS (Mobile) Item 29: Flight Shopping (Mobile): Unhandled Exception ArgumentOutofRangeException - 2.1.9 - Flights Object Null
                                    throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0"));
                                }

                                ///208852 : Booking - FSR - PROD Basic Economy mApps Lowest Basic Economy fare is displayed. (Basic Economy switch is off) 
                                ///Srini - 11/27/2017
                                availability.Reservation.Trips.Add(PopulateTrip(_mOBSHOPDataCarrier, response.CartId, response.Trips, i,
                                    persistShop.Response.Availability.Trip.SearchFiltersIn.FareFamily,
                                    selectRequest.SessionId, selectRequest.TripId, selectRequest.Application.Id,
                                    selectRequest.DeviceId, selectRequest.Application.Version.Major,
                                    persistShop.Request.ShowMileageDetails, persistShop.Request.PremierStatusLevel, false,
                                    availability.AwardTravel, (_configuration.GetValue<bool>("BugFixToggleFor17M") ? session.IsBEFareDisplayAtFSR : true), selectRequest.GetNonStopFlightsOnly, selectRequest.GetFlightsWithStops, persistShop?.Request, session,
                                    additionalItems, lstMessages: lstMessages).Result);

                            }
                            else if ((response.Trips[i] != null))//assume this is the trips for selection
                            {
                                if ((_configuration.GetValue<string>("CheckCSLShopSelectFlightsNull") == null || Convert.ToBoolean(_configuration.GetValue<string>("CheckCSLShopSelectFlightsNull"))) && (response.Trips[i].Flights == null || response.Trips[i].Flights.Count == 0) && response.LastTripIndexRequested == i + 1)
                                {
                                    //To ByPass this Flight Null Check if have any issues after weekly releaase 10/27 just add this entry to production web config =  <add key="CheckCSLShopSelectFlightsNull" value="false"/>
                                    // To turn on check Flight Nulls delete the entry in web config or update the value to true <add key="CheckCSLShopSelectFlightsNull" value="true"/>

                                    string actionName = "Trip planner CSL Shop Select – Flights Null";
                                    _logger.LogInformation("Trace-CheckCSLShopSelectFlightsNull {actionName} {request}", actionName, jsonRequest);
                                    //61048 - Bug 331484:FS (Mobile) Item 29: Flight Shopping (Mobile): Unhandled Exception ArgumentOutofRangeException - 2.1.9 - Flights Object Null
                                    throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0"));
                                }
                                if (availability.Trip == null)
                                {

                                    ///208852 : Booking - FSR - PROD Basic Economy mApps Lowest Basic Economy fare is displayed. (Basic Economy switch is off) 
                                    ///Srini - 11/27/2017
                                    availability.Trip = await PopulateTrip(_mOBSHOPDataCarrier, response.CartId, response.Trips, i,
                                        persistShop.Response.Availability.Trip.SearchFiltersIn.FareFamily,
                                        selectRequest.SessionId, selectRequest.TripId, selectRequest.Application.Id,
                                        selectRequest.DeviceId, selectRequest.Application.Version.Major,
                                        persistShop.Request.ShowMileageDetails,
                                        persistShop.Request.PremierStatusLevel, false, availability.AwardTravel, (_configuration.GetValue<bool>("BugFixToggleFor17M") ? session.IsBEFareDisplayAtFSR : true), selectRequest.GetNonStopFlightsOnly, selectRequest.GetFlightsWithStops, persistShop?.Request, session,
                                        additionalItems, lstMessages: lstMessages);

                                }


                                #region Save Amenities Request to Persist
                                //UpdateAmenitiesIndicatorsRequest amenitiesRequest = new UpdateAmenitiesIndicatorsRequest();
                                //amenitiesRequest = GetAmenitiesRequest(response.CartId, response.Trips[i].Flights);
                                //ShoppingExtend shopExtendDAL = new ShoppingExtend();
                                //shopExtendDAL.AddAmenitiesRequestToPersist(amenitiesRequest, selectRequest.SessionId, response.LastTripIndexRequested.ToString());
                                #endregion
                            }

                            // For Reshop & partially used pnr

                            #region  //**NOTE**// Venkat - Nov 10,2014 For Oragainze Results
                            if (availability.Trip != null) // Booking & Reshop
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


                        SetTitleForFSRPage(availability, persistShop.Request);

                        //if (availability.Reservation.Trips != null && availability.Reservation.Trips.Count > 0 && availability.Trip != null)
                        //{
                        //    List<MOBSHOPTripBase> shopTrips = new List<MOBSHOPTripBase>();
                        //    shopTrips.Add(availability.Trip);
                        //    shopTrips.Add(availability.Reservation.Trips[availability.Reservation.Trips.Count - 1]);

                        //    if (shopRequest != null && shopRequest.ShopRequest != null && string.IsNullOrEmpty(shopRequest.ShopRequest.EmployeeDiscountId))
                        //    {
                        //        availability.FareWheel = PopulateFareWheelDates(shopTrips, "SELECTTRIP");
                        //    }
                        //}
                        availability.MaxTPFlightSelectCount = Convert.ToInt32(_configuration.GetValue<string>("TPMaxFlightSelectCountEditAndFSRNFlow"));
                        if (session.TravelType == TravelType.TPEdit.ToString())
                        {
                            var tripPlanEditDetails = _sessionHelperService.GetSession<string>(session.SessionId, ObjectNames.TripPlanEditCartDetailsFullName, new List<string> { session.SessionId, ObjectNames.TripPlanEditCartDetailsFullName }).Result?.Split('|') ?? throw new Exception("Unable to fetch trip plan edit details");
                            availability.TripPlanId = tripPlanEditDetails[0];
                            availability.TripPlanCartId = tripPlanEditDetails[1];
                        }
                        availability.HideFareWheel = true;

                        await _sessionHelperService.SaveSession<CSLSelectTrip>(new CSLSelectTrip() { ShopCSLRequest = request, ShopCSLResponse = response, MobSelectTripResponse = new SelectTripResponse() { Availability = availability } }, response.CartId, new List<string> { response.CartId, new CSLSelectTrip().ObjectName }, new CSLSelectTrip().ObjectName, saveJsonOnCloudXMLOnPrem: false).ConfigureAwait(false);

                    }
                    else
                    {
                        if (response.Errors != null && response.Errors.Count > 0)
                        {
                            string errorMessage = string.Empty;
                            foreach (var error in response.Errors)
                            {
                                errorMessage = errorMessage + " " + error.Message;
                                //Bug 56109:ShopSelect: System.Exception - Object reference not set to an instance of an object - Ravitheja - Sep 14, 2016
                                if (!string.IsNullOrEmpty(error.MinorCode) && error.MinorCode.Trim().Equals("10046"))
                                    throw new MOBUnitedException(_configuration.GetValue<string>("BookingExceptionMessage_ServiceErrorSessionNotFound"));

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

                            throw new System.Exception(errorMessage);
                        }
                    }
                }
                else
                {
                    throw new MOBUnitedException("The service did not return any availability.");
                }
            }

            availability.Trip?.FlattenedFlights?.ForEach(ff => ff?.Flights?.ForEach(f => f.ShoppingProducts = f.ShoppingProducts?.Where(p => p.IsSelectedCabin).ToList()));

            availability?.Reservation?.Trips?.
               ForEach(t => t?.FlattenedFlights?.
              ForEach(ff => ff?.Flights?.
              ForEach(f => f.ShoppingProducts = (f?.ShoppingProducts?.Where(p => p?.IsSelectedCabin ?? false) ?? f.ShoppingProducts)
              ?.ToList())));

            await SetCarbonEmissionDataForTripPlanner(selectRequest, availability, session, request.Trips);
            return availability;
        }

        private async Task SetCarbonEmissionDataForTripPlanner(MOBSHOPSelectTripRequest selectRequest, MOBSHOPAvailability availability, Session session, List<Trip> trips)
        {

            if (_shoppingUtility.IsEnableCarbonEmissionsFeature(selectRequest.Application.Id, selectRequest.Application.Version.Major, session.CatalogItems))
            {
                MOBCarbonEmissionsResponse carbonEmissionData = null;
                if (_configuration.GetValue<bool>("EnableCarbonEmissionsFix"))
                {
                    try
                    {
                        carbonEmissionData = await GetCarbonEmissionsFromReferenceData(availability.CartId, availability, selectRequest, session, trips?.FirstOrDefault());
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
                    carbonEmissionData = await LoadCarbonEmissionsDataFromPersist(selectRequest.SessionId);
                    if (carbonEmissionData != null)
                    {
                        var tripList = availability?.Reservation?.Trips;
                        if (tripList != null && tripList.Count > 0)
                        {
                            foreach (var trip in tripList)
                            {
                                foreach (var flattenedFlight in trip?.FlattenedFlights)
                                {
                                    foreach (var flight in flattenedFlight.Flights)
                                    {
                                        SetCarbonEmissionDetails(carbonEmissionData, flight);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #region CarbonEmissions Reference data

        private async Task<MOBCarbonEmissionsResponse> GetCarbonEmissionsFromReferenceData(string cartId, MOBSHOPAvailability availability, MOBRequest selectTripRequest, Session session, Trip trip = null)
        {
            MOBCarbonEmissionsResponse carboinEmissionData = new MOBCarbonEmissionsResponse();
            // Build the CarbonEmission Reference Data request 
            CarbonEmissionRequest carbonEmissionRequest = await BuildCarbonEmissionsReferenceDataRequestForTripplanner(session, trip, availability.TravelerCount).ConfigureAwait(false);

            var carbonEmissionSelectFlightResponse = await GetFlightCarbonEmissionDetailsByFlight(session, carbonEmissionRequest, new MOBCarbonEmissionsResponse(), carboinEmissionData, new MOBCarbonEmissionsRequest()
            {
                Application = selectTripRequest.Application,
                DeviceId = selectTripRequest.DeviceId,
                CartId = cartId,
                TransactionId = selectTripRequest.TransactionId,
                AccessCode = selectTripRequest.AccessCode
            });

            return carbonEmissionSelectFlightResponse;

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
                _logger.LogError("GetFlightCarbonEmissionDetailsByFlight - DataAccess error{@message} {@stackTrace}", ex.Message, ex.StackTrace);
            }

            return response;
        }

        private async Task<CarbonEmissionRequest> BuildCarbonEmissionsReferenceDataRequestForTripplanner(Session session, Trip displayTrip, int travelerCount)
        {
            CarbonEmissionRequest carbonEmissionRequest = new CarbonEmissionRequest();
            SelectTrip selectTrip = new SelectTrip();
            selectTrip = await _sessionHelperService.GetSession<SelectTrip>(session.SessionId, new SelectTrip().ObjectName, new List<string> { session.SessionId, new SelectTrip().ObjectName }).ConfigureAwait(false);
            carbonEmissionRequest.Trips = new Collection<Service.Presentation.ReferenceDataModel.TripReference>();
            carbonEmissionRequest.PassengerCount = travelerCount;
            if (displayTrip != null)
            {
                Service.Presentation.ReferenceDataModel.TripReference tripRequest = new Service.Presentation.ReferenceDataModel.TripReference();
                tripRequest.Destination = displayTrip?.Destination;
                tripRequest.Origin = displayTrip?.Origin;
                tripRequest.Flights = new Collection<Service.Presentation.ReferenceDataModel.FlightReference>();
                if (displayTrip.Flights != null && displayTrip.Flights.Any())
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

        private async Task<MOBCarbonEmissionData> BuildCarbonEmissionContentForReferenceData(MOBRequest mOBRequest,
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
            content.PageTitle = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_PageTitle_MOB");
            content.Header = _shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Header_MOB_NEW");
            if (!string.IsNullOrEmpty(_shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Details_Screen")))
                content.Body = string.Format(_shoppingUtility.GetSDLStringMessageFromList(lstMessages, "FSR_CarbonEmissions_Details_Screen"), carbonEmissionDetails?.TotalEmission, carbonEmissionDetails?.MilesPerGallon, carbonEmissionDetails?.LitersPerHundredKM);

            carbonEmission.ContentScreen = content;
            return await Task.FromResult(carbonEmission);
        }

        #endregion

        private void SetCarbonEmissionDetails(MOBCarbonEmissionsResponse carbonEmissionData, MOBSHOPFlight flattenedFlight)
        {
            if (carbonEmissionData != null && carbonEmissionData.CarbonEmissionData?.Count > 0)
            {
                flattenedFlight.CarbonEmissionData = carbonEmissionData.CarbonEmissionData.Where(a => a.FlightHash == flattenedFlight.FlightHash)?.FirstOrDefault();
            }
        }

        private async Task<MOBCarbonEmissionsResponse> LoadCarbonEmissionsDataFromPersist(string sessionId)
        {
            if (_configuration.GetValue<bool>("EnableCarbonEmissionsFeature"))
            {
                try
                {
                    MOBCarbonEmissionsResponse carboinEmissionData = new MOBCarbonEmissionsResponse();
                    carboinEmissionData = await _sessionHelperService.GetSession<MOBCarbonEmissionsResponse>(sessionId, new MOBCarbonEmissionsResponse().ObjectName, new List<string> { sessionId, new MOBCarbonEmissionsResponse().ObjectName }).ConfigureAwait(false);
                    return carboinEmissionData;
                }
                catch { }
            }
            return null;
        }

        private void SetTitleForFSRPage(MOBSHOPAvailability availability, MOBSHOPShopRequest shopRequest)
        {
            if (_shoppingUtility.CheckFSRRedesignFromShopRequest(shopRequest))
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

                        string taveler = availability.TravelerCount <= 1 ? Convert.ToString(availability.TravelerCount) + " traveler" : Convert.ToString(availability.TravelerCount) + " travelers";
                        string date = Convert.ToDateTime(availability.Trip.DepartDate).ToString("ddd MMM d", CultureInfo.CreateSpecificCulture("en-US"));
                        string tripCount = shopRequest.Trips.Count.ToString();
                        string isLastTripIndex = Convert.ToString(availability.Trip.LastTripIndexRequested);
                        string searchType = shopRequest.SearchType.Equals("OW", StringComparison.OrdinalIgnoreCase) ? "One-way" : shopRequest.SearchType.Equals("RT", StringComparison.OrdinalIgnoreCase) ? "Roundtrip" : "Flight " + isLastTripIndex + " of " + tripCount;
                        var joiner = $" {(char)8226} "; //" • "
                        availability.SubTitle = string.Join(joiner, searchType, taveler, date);
                    }
                    catch (Exception ex)
                    {
                        availability.Title = _configuration.GetValue<string>("FSRRedesignTitleForNoREsults") ?? "Select flights";
                        availability.SubTitle = string.Empty;
                    }
                }
            }
        }

        private string GetPriceFromText(MOBSHOPShopRequest shopRequest)
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
        private async Task<ShopRequest> GetTripPlannerShopSelectRequest(MOBSHOPSelectTripRequest selectRequest)
        {

            if (!string.IsNullOrEmpty(selectRequest.CalendarDateChange))
            {
                var cSLSelectTrip = await _sessionHelperService.GetSession<CSLSelectTrip>(selectRequest.CartId, new CSLSelectTrip().ObjectName, new List<string> { selectRequest.CartId, new CSLSelectTrip().ObjectName }).ConfigureAwait(false);
                cSLSelectTrip.ShopCSLRequest.Trips[cSLSelectTrip.ShopCSLResponse.LastTripIndexRequested - 1].DepartDate = selectRequest.CalendarDateChange;
                return cSLSelectTrip.ShopCSLRequest;
            }

            //var persistCSLShopResponse = United.Persist.FilePersist.Load<CSLShopResponse>(shop.Request.SessionId, new CSLShopResponse().ObjectName);
            var persistCSLShopResponse = await _sessionHelperService.GetSession<CSLShopResponse>(selectRequest.SessionId, new CSLShopResponse().ObjectName, new List<string> { selectRequest.SessionId, new CSLShopResponse().ObjectName }).ConfigureAwait(false);
            var persistCSLShopRequest = await _sessionHelperService.GetSession<CSLShopRequest>(selectRequest.SessionId, new CSLShopRequest().ObjectName, new List<string> { selectRequest.SessionId, new CSLShopRequest().ObjectName }).ConfigureAwait(false);


            //shopSelectRequest.Trips = persistCSLShopRequest.ShopRequest.Trips;
            //shopSelectRequest.Trips[0].Flights = new List<Flight>();
            persistCSLShopRequest.ShopRequest.Trips[0].Flights.Add(persistCSLShopResponse.ShopCSLResponse.Trips[0].Flights.First(f => f.BBXSolutionSetId == selectRequest.TripId &&
            f.Products.Any(p => p.ProductId == selectRequest.ProductId)));

            //persistCSLShopRequest.ShopRequest.Trips[0].Flights[0].Products.First(p => p.ProductId == selectRequest.ProductId).Selected = true;

            persistCSLShopRequest.ShopRequest.Trips[0].ColumnInformation = persistCSLShopResponse.ShopCSLResponse.Trips[0].ColumnInformation;

            return persistCSLShopRequest.ShopRequest;

        }
        private bool NoCSLExceptions(List<United.Services.FlightShopping.Common.ErrorInfo> errors)
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
        private string GetPriceTextDescription(string searchType)
        {
            string priceTextDescription = string.Empty;

            if (searchType == "RT")
            {
                priceTextDescription = _configuration.GetValue<string>("PriceTextDescription").Split('|')[0];//Roundtrip
            }
            else if (searchType == "MD")
            {
                priceTextDescription = _configuration.GetValue<string>("PriceTextDescription").Split('|')[1];//From
            }

            return priceTextDescription;
        }
        private void SetFSRFareDescriptionForShop(MOBSHOPAvailability availability, MOBSHOPShopRequest request)
        {
            availability.fSRFareDescription = GetFSRFareDescription(request, _shoppingUtility.EnableBagCalcSelfRedirect(request.Application.Id, request.Application.Version.Major));
            // MOBILE-14512
            if (_configuration.GetValue<bool>("EnableSortFilterEnhancements") &&
                _shoppingUtility.IsSortDisclaimerForNewFSR(request.Application.Id, request.Application.Version.Major))
            {
                availability.SortDisclaimerText = GetTextForSortDisclaimer(false);
            }
        }
        private string GetTextForSortDisclaimer(bool forAppend)
        {
            return $"{(forAppend ? "\n" : "")}{_configuration.GetValue<string>("AdditionalLegalDisclaimerText")}";
        }
        private string GetFSRFareDescription(MOBSHOPShopRequest request, bool isBagCalcMobileRedirect = false)
        {
            string FSRFareDescription = string.Empty;
            bool isExperiment = false;

            // Need to add new experiment when comparing
            if (_shoppingUtility.CheckFSRRedesignFromShopRequest(request))
            {
                FSRFareDescription = GetNewFSRFareDescriptionMessage(request.SearchType, isBagCalcMobileRedirect);
                // MOBILE-14512
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
        private static string GetFormatedUrl(string url, string scheme, string relativePath, bool ensureSSL = false)
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
        private string GetNewFSRFareDescriptionMessage(string searchType, bool isMobileRedirect)
        {
            string FSRDescription = _configuration.GetValue<string>("FSRRedesignFareDescription");
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
        private string GetFSRFareDescriptionMessage(string searchType)
        {
            string FSRDescription = string.Empty;
            switch (searchType)
            {
                case "OW":
                    if (_configuration.GetValue<bool>("EnableFixMobile16188"))
                    {
                        FSRDescription = _configuration.GetValue<string>("FSRFareDescription").Split('|')[2];
                    }
                    break;
                case "RT":
                    FSRDescription = _configuration.GetValue<string>("FSRFareDescription").Split('|')[0];
                    break;
                case "MD":
                    FSRDescription = _configuration.GetValue<string>("FSRFareDescription").Split('|')[1];
                    break;
                default:
                    FSRDescription = string.Empty;
                    break;
            }

            return FSRDescription;
        }

        private string CheckBagFareDesclaimer(bool isMobileRedirect)
        {
            if (isMobileRedirect) return _configuration.GetValue<string>("CheckedBagInfoMobileRedirectURL");

            return _configuration.GetValue<string>("EnableNewBaggageTextOnFSRShop");
        }

        public async Task<MOBSHOPTrip> PopulateTrip(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string cartId, List<Trip> trips, int tripIndex, string requestedCabin, string sessionId, string tripKey, int appId, string deviceId, string appVersion, bool showMileageDetails, int premierStatusLevel, bool isStandardRevenueSearch, bool isAward = false, bool isELFFareDisplayAtFSR = true, bool getNonStopFlightsOnly = false, bool getFlightsWithStops = false, MOBSHOPShopRequest shopRequest = null, Session session = null, MOBAdditionalItems additionalItems = null, List<CMSContentMessage> lstMessages = null)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            //  Session session = new Session();
            //  session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName });
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
                _logger.LogError("GetAllAiportsList PopulateTrip-GetAllAiportsList {@Exception}", JsonConvert.SerializeObject(ex));
                _logger.LogError("GetAllAiportsList PopulateMetaTrips-Trip {@trips}", JsonConvert.SerializeObject(trips));

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

                    flights = await GetFlightsAsync(_mOBSHOPDataCarrier, sessionId, cartId, trips[i].Flights, trips[i].SearchFiltersIn.FareFamily, trips[i].SearchFiltersOut.PriceMin, trip.Columns, premierStatusLevel, fareClass, false, false, isELFFareDisplayAtFSR, trip, appVersion, appId, session, additionalItems);
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
                    trip.FlattenedFlights = await GetFlattendFlights(flights, trip.TripId, trips[i].BBXCellIdSelected, trip.DepartDate, shopRequest, tripIndex, mOBAdditionalToggle, lstMessages: lstMessages).ConfigureAwait(false);

                    if (_configuration.GetValue<bool>("EnableShowOriginDestinationForFlights") && session.IsFSRRedesign && GeneralHelper.IsApplicationVersionGreaterorEqual(shopRequest.Application.Id, shopRequest.Application.Version.Major, _configuration.GetValue<string>("AndroidShowOriginDestinationForFlightsVersion"), _configuration.GetValue<string>("iOSShowOriginDestinationForFlightsVersion")))
                    {
                        if ((_shoppingUtility.IsFSRNearByAirportAlertEnabled(shopRequest.Application.Id, shopRequest.Application.Version.Major) && _configuration.GetValue<bool>("DisableShowOrgAndDestAirportFlag") == false))
                            showOriginDestinationForFlights = trip.FlattenedFlights.Any(x => x?.Flights?.First()?.Origin != trip.Origin || x?.Flights?.Last()?.Destination != trip.Destination || (additionalItems != null && additionalItems.ShowOrgAndDestByNearByAirport == true));
                        else
                            showOriginDestinationForFlights = trip.FlattenedFlights.Any(x => x?.Flights?.First()?.Origin != trip.Origin || x?.Flights?.Last()?.Destination != trip.Destination);
                    }
                }
                //TODO-CHECK IF THIS PIECE OF CODE IS NEEDED. NOT THERE IN MREST
                //bool mixedCabinFlightExists = false;
                //if (_configuration.GetValue<bool>("EnableAwardMixedCabinFiter") && isAward)
                //{
                //    mixedCabinFlightExists = trip.FlattenedFlights.Any(f => f.Flights.Any(s => s.ShoppingProducts.Any(p => p.IsMixedCabin)));
                //}
                trip.UseFilters = trips[i].UseFilters;
                trip.SearchFiltersIn = trips[i].SearchFiltersIn != null && !string.IsNullOrEmpty(trips[i].SearchFiltersIn.AirportsOrigin) ? await GetSearchFilters(trips[i].SearchFiltersIn, ci, appId, appVersion, requestedCabin, trips[i].ColumnInformation, isStandardRevenueSearch, isAward, mOBAdditionalToggle.MixedCabinFlightExists, lstMessages, session).ConfigureAwait(false) : await GetSearchFilters(trips[i].SearchFiltersOut, ci, appId, appVersion, requestedCabin, trips[i].ColumnInformation, isStandardRevenueSearch, isAward, mOBAdditionalToggle.MixedCabinFlightExists, lstMessages, session).ConfigureAwait(false);
                trip.SearchFiltersOut = await GetSearchFilters(trips[i].SearchFiltersOut, ci, appId, appVersion, requestedCabin, trips[i].ColumnInformation, isStandardRevenueSearch, isAward, mOBAdditionalToggle.MixedCabinFlightExists, lstMessages, session).ConfigureAwait(false);

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
                    trip.Columns.First(c => c.ColumnID.Equals(focusColumnID)).IsSelectedCabin = true;
                }

                trip.ShowOriginDestinationForFlights = showOriginDestinationForFlights;
            }
            trip.FlightDateChangeMessage = _configuration.GetValue<string>("FlightDateChangeMessage");

            return trip;
        }

        private string GetFocusColumnID(MOBSHOPTrip trip, MOBSHOPShopRequest shopRequest)
        {
            if (_configuration.GetValue<bool>("DisableBusinessDefaultWhenFirstNoResults") == false
                && trip.FlattenedFlights[0].Flights[0].ShoppingProducts.FirstOrDefault(p => p.IsSelectedCabin)?.ColumnID == null)
            {
                if (shopRequest.Trips?.FirstOrDefault()?.Cabin?.Contains("First") == true)
                {
                    if (trip.FlattenedFlights[0].Flights[0].ShoppingProducts?.Where(a => a.Cabin?.ToLower() == "business")?.FirstOrDefault() != null)
                        return trip.FlattenedFlights[0].Flights[0].ShoppingProducts?.Where(a => a.Cabin?.ToLower() == "business")?.FirstOrDefault().ColumnID;
                    else
                        return trip.FlattenedFlights[0].Flights[0].ShoppingProducts.FirstOrDefault(p => p.IsSelectedCabin)?.ColumnID ?? trip.FlattenedFlights[0].Flights[0].ShoppingProducts[0].ColumnID;
                }
            }
            return trip.FlattenedFlights[0].Flights[0].ShoppingProducts.FirstOrDefault(p => p.IsSelectedCabin)?.ColumnID ?? trip.FlattenedFlights[0].Flights[0].ShoppingProducts[0].ColumnID;

        }

        private List<MOBSearchFilterItem> GetDefaultFsrRedesignFooterSortTypes()
        {
            var sortTypes = new List<MOBSearchFilterItem>();
            List<string> sortTypesList = _configuration.GetValue<string>("FsrRedesignSearchFiletersSortTypes").Split('|').ToList();
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

        private bool isGetAirportListInOneCallToggleOn()
        {
            return _configuration.GetValue<bool>(("GetAirportNameInOneCallToggle") ?? "False");
        }
        private async Task<AirportDetailsList> GetAllAiportsList(List<Trip> trips)
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

        private List<Model.Shopping.MOBSHOPShoppingProduct> PopulateColumns(ColumnInformation columnInfo)
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

            var returnValue = await _flightShoppingService.UpdateAmenitiesIndicators<UpdateAmenitiesIndicatorsResponse>(session.Token, sessionId, jsonRequest);
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
                    _logger.LogError("GetAmenitiesForFlight - Response for GetAmenitiesForFlight {@Error}", errorMessage);
                }
                else
                {
                    _logger.LogError("GetAmenitiesForFlight - Response for GetAmenitiesForFlight is null or response profile is null");
                }
            }
            return response;
        }


        private UpdateAmenitiesIndicatorsRequest GetAmenitiesRequest(string cartId, List<Flight> flights)
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
        private void PopulateFlightAmenities(Collection<AmenitiesProfile> amenityFlights, ref List<Flight> flights)
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
        private void GetAmenitiesForFlight(Collection<AmenitiesProfile> amenityFlights, ref Flight flight)
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
        private async Task<List<United.Services.FlightShopping.Common.LMX.LmxFlight>> GetLmxFlights(string token, string cartId, string hashList, string sessionId, int applicationId, string appVersion, string deviceId)
        {
            List<United.Services.FlightShopping.Common.LMX.LmxFlight> lmxFlights = null;

            if (!string.IsNullOrEmpty(cartId))
            {
                string jsonRequest = "{\"CartId\":\"" + cartId + "\"}";

                //logEntries.Add(LogEntry.GetLogEntry<string>(sessionId, "GetLmxFlights - URL", "Trace", applicationId, appVersion, deviceId, url));


                if (!string.IsNullOrEmpty(hashList))
                {
                    jsonRequest = "{\"CartId\":\"" + cartId + "\", \"hashList\":[" + hashList + "]}";
                }

                FlightStatus flightStatus = new FlightStatus();
                LmxQuoteResponse response = new LmxQuoteResponse();
                try
                {
                    response = await _lmxInfo.GetProductInfo<LmxQuoteResponse>(token, jsonRequest, sessionId);
                }
                catch (System.Exception) { }

                if (response != null)
                {
                    if (response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success))
                    {
                        if (response.Flights != null && response.Flights.Count > 0)
                        {
                            lmxFlights = response.Flights;
                        }
                    }
                }
            }

            return lmxFlights;
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
        private async Task<(List<Model.Shopping.MOBSHOPFlight> mOBSHOPs, CultureInfo ci)> GetFlights(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string sessionId, string cartId, List<Flight> segments, string requestedCabin, CultureInfo ci, decimal lowestFare, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClass, bool updateAmenities = false, bool isConnection = false, bool isELFFareDisplayAtFSR = true, MOBSHOPTrip trip = null, string appVersion = "", int appID = 0, MOBCarbonEmissionsResponse carbonEmissionData = null)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            Session session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);
            supressLMX = session.SupressLMXForAppID;
            #endregion

            try
            {
                if (isGetAirportListInOneCallToggleOn() && (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0))
                {
                    airportsList = await _sessionHelperService.GetSession<AirportDetailsList>(sessionId, new AirportDetailsList().ObjectName, new List<string> { sessionId, new AirportDetailsList().ObjectName }).ConfigureAwait(false);//, (new AirportDetailsList()).GetType().FullName);
                    if (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0)
                    {
                        airportsList = await GetAllAiportsList(segments);
                    }
                }
            }
            catch (Exception ex)
            {
                //logEntries.Add(LogEntry.GetLogEntry<Exception>(sessionId, "GetAllAiportsList", "GetFlights-GetAllAiportsList", 1, string.Empty, string.Empty, ex, true, true));
                //logEntries.Add(LogEntry.GetLogEntry<List<Flight>>(sessionId, "GetAllAiportsList", "PopulateMetaTrips-Trip", 1, string.Empty, string.Empty, segments, true, true));
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
                    flight.Messages = new List<United.Mobile.Model.Shopping.MOBSHOPMessage>();
                    string AddCollectProductID = string.Empty;
                    Product displayProductForStopInfo = null;
                    bool selectedForStopInfo = false;
                    string bestProductType = null;

                    // #633226 Reshop SDC Add coller waiver status
                    if (session.IsReshopChange)
                    {
                        flight.isAddCollectWaived = GetAddCollectWaiverStatus(segment, out AddCollectProductID);
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
                    _shoppingUtility.SetCarbonEmissionDetailsForConnections(carbonEmissionData, flight);
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
                                    flight.AirfareDisplayValue = "N/A";
                                    flight.MilesDisplayValue = "N/A";
                                    if (_configuration.GetValue<bool>("ReturnExceptionForAnyZeroDollarAirFareValue"))
                                    {
                                        throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
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
                                    flight.AirfareDisplayValue = "N/A";
                                    if (_configuration.GetValue<bool>("ReturnExceptionForAnyZeroDollarAirFareValue"))
                                    {
                                        // Added as part of Bug 180337:mApp: "Sorry something went wrong... " Error message is displayed when selected cabin for second segment in the multi trip
                                        // throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                                        throw new MOBUnitedException(_configuration.GetValue<string>("BookingDetail_ITAError_For_CSL_10047__Error_Code"));
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

                                flight.Messages = new List<Model.Shopping.MOBSHOPMessage>();

                                Model.Shopping.MOBSHOPMessage msg = new Model.Shopping.MOBSHOPMessage();
                                msg.FlightNumberField = flight.FlightNumber;
                                msg.TripId = flight.TripId;
                                msg.MessageCode = displayProduct.Description + " (" + displayProduct.BookingCode + ")";
                                if (selected && _shoppingUtility.IsIBeLiteFare(displayProduct)) // bug 277549: update the message for IBE Lite only when customer switch ON the 'Show Basic Economy fares'
                                {
                                    msg.MessageCode = msg.MessageCode + " " + displayProduct.CabinTypeText; // EX: United Economy (K) (first bag charge/no changes allowed)
                                }
                                flight.Messages.Add(msg);

                                msg = new Model.Shopping.MOBSHOPMessage();
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

                                foreach (var tier in displayProduct.LmxLoyaltyTiers)
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
                                                foreach (var quote in tier.LmxQuotes)
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
                                intSeatsRemainingLimit = Convert.ToInt32(_configuration.GetValue<string>("mWebSeatsRemainingLimit"));
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
                        foreach (var amenity in segment.Amenities)
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

                        var tupleResponse = await GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, segment.Connections, requestedCabin, ci, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR, carbonEmissionData: carbonEmissionData);
                        flight.Connections = tupleResponse.mOBSHOPs;
                        ci = tupleResponse.ci;
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
                    flight.FareBasisCode = segment.FareBasisCode;
                    flight.FlightNumber = segment.FlightNumber;
                    flight.GroundTime = segment.GroundTimeMinutes.ToString();
                    flight.InternationalCity = segment.InternationalCity;
                    flight.IsConnection = segment.IsConnection;
                    flight.MarketingCarrier = segment.MarketingCarrier;
                    flight.MarketingCarrierDescription = segment.MarketingCarrierDescription;
                    flight.Miles = segment.MileageActual.ToString();

                    if (_configuration.GetValue<string>("ReturnShopSelectTripOnTimePerformance") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnShopSelectTripOnTimePerformance").ToString()))
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
                                    List<Flight> stops = new List<Flight>();
                                    stops.Add(stop);
                                    if (_mOBSHOPDataCarrier == null)
                                        _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();

                                    var tupleFlightRes = await GetFlights(_mOBSHOPDataCarrier, sessionId, cartId, stops, requestedCabin, ci, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR, carbonEmissionData: carbonEmissionData);
                                    List<Model.Shopping.MOBSHOPFlight> stopFlights = tupleFlightRes.mOBSHOPs;
                                    ci = tupleFlightRes.ci;

                                    foreach (Model.Shopping.MOBSHOPFlight sf in stopFlights)
                                    {
                                        sf.ChangeOfGauge = true;
                                    }

                                    ///245704 - PROD: Mapp - Incorrect segments info is displayed for a 2-Stop flight UA1666 on choose your travel experience screen
                                    ///Srini - 03/20/2018
                                    if (_configuration.GetValue<bool>("BugFixToggleFor18C"))
                                    {
                                        flight.Destination = stop.Origin;
                                        if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes") && !isFlightDestionUpdated)
                                        {
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
                                            flight.DestinationDecodedWithCountry = stop.OriginDescription;
                                            flight.DestinationStateCode = stop.OriginStateCode;
                                            flight.DestinationCountryCode = stop.OriginCountryCode;
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

                                        stopFlight.Messages = new List<Model.Shopping.MOBSHOPMessage>();

                                        Model.Shopping.MOBSHOPMessage msg = new Model.Shopping.MOBSHOPMessage();
                                        msg.FlightNumberField = flight.FlightNumber;
                                        msg.TripId = flight.TripId;
                                        msg.MessageCode = displayProductForStopInfo.Description + " (" + displayProductForStopInfo.BookingCode + ")";
                                        stopFlight.Messages.Add(msg);

                                        msg = new Model.Shopping.MOBSHOPMessage();
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
                            flight.Destination = segment.StopInfos[0].Origin;
                            flight.DestinationDate = FormatDate(Convert.ToDateTime(segment.StopInfos[0].DepartDateTime).AddMinutes(-segment.StopInfos[0].GroundTimeMinutes).ToString());
                            flight.DestinationTime = FormatTime(Convert.ToDateTime(segment.StopInfos[0].DepartDateTime).AddMinutes(-segment.StopInfos[0].GroundTimeMinutes).ToString());
                            flight.TravelTime = travelMinutes > 0 ? GetFormattedTravelTime(travelMinutes) : string.Empty;
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

                    var tupleRes = await PopulateProducts(_mOBSHOPDataCarrier, segment.Products, sessionId, flight, requestedCabin, segment, lowestFare, columnInfo, premierStatusLevel, fareClass, isELFFareDisplayAtFSR, appVersion);
                    flight = tupleRes.flight;
                    flight.ShoppingProducts = tupleRes.Item1;


                    SetAutoFocusIfMissed(session, isELFFareDisplayAtFSR, flight.ShoppingProducts, bestProductType);
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
                        if (_configuration.GetValue<string>("HandlePagingAtRESTSide") != null && Convert.ToBoolean(_configuration.GetValue<string>("HandlePagingAtRESTSide").ToString()) && flights.Count == Convert.ToInt32(_configuration.GetValue<string>("OrgarnizeResultsRequestPageSize").ToString()))
                        {
                            break;
                        }
                        #endregion
                    }
                }
            }

            return (flights, ci);
        }
        private string FormatDateTimeTripPlan(string dateTimeString)
        {
            string result = string.Empty;
            DateTime dateTime = new DateTime(0);
            DateTime.TryParse(dateTimeString, out dateTime);
            result = string.Format("{0:ddd, MMM dd}", dateTime);

            return result;
        }
        private bool IsTripPlanSearch(string travelType)
        {
            var isTripPlannerViewEnabled = _configuration.GetValue<bool>("EnableTripPlannerView");
            return isTripPlannerViewEnabled && (travelType == MOBTripPlannerType.TPSearch.ToString() || travelType == MOBTripPlannerType.TPEdit.ToString());
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
        private decimal ReshopAirfareCreditDisplayInDecimal(List<PricingItem> price, string priceType)
        {
            decimal retVal = 0;
            if (price.Exists(p => string.Equals(p.PricingType, priceType, StringComparison.OrdinalIgnoreCase)))
                retVal = price.FirstOrDefault(p => string.Equals(p.PricingType, priceType, StringComparison.OrdinalIgnoreCase)).Amount;
            return retVal;
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
                        var priorityProduct = shopProds.FirstOrDefault(p => (!isELFFareDisplayAtFSR) ? p?.Type?.ToUpper() != "ECO-BASIC" : true && p.PriceAmount > 0);

                        if (priorityProduct != null)
                            priorityProduct.IsSelectedCabin = true;
                    }
                }
            }
        }

        private async Task<AirportDetailsList> GetAllAiportsList(List<Flight> flights)
        {
            string airPortCodes = GetAllAtirportCodeWithCommaDelimatedFromCSLFlighs(flights);
            return await GetAirportNamesListCollection(airPortCodes);
        }
        private bool GetAddCollectWaiverStatus(Flight flight, out string addcollectwaiver)
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
            return false;
        }

        private void AssignCorporateFareIndicator(Flight segment, Model.Shopping.MOBSHOPFlight flight, string travelType = "")
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
        private Product GetMatrixDisplayProduct(ProductCollection products, string fareSelected, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, out CultureInfo ci, out bool isSelectedFareFamily, out string serviceClassDesc, out bool isMixedCabin, out int seatsRemaining, string fareClass, bool isConnectionOrStopover = false, bool isELFFareDisplayAtFSR = true)
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
                        else if (Convert.ToBoolean(_configuration.GetValue<string>("SwithAwardSelectedCabinMilesDisplay") ?? "false") && !string.IsNullOrEmpty(fareSelected) && fareSelected.ToUpper().Contains("AWARD"))
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
        public static string GetCabinNameFromColumn(string type, List<MOBSHOPShoppingProduct> columnInfo, string defaultCabin)
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
        private async Task<List<Model.Shopping.MOBSHOPFlattenedFlight>> GetFlattendFlights(List<Model.Shopping.MOBSHOPFlight> flights, string tripId, string productId, string tripDate, MOBSHOPShopRequest shopRequest = null, int tripIndex = -1, MOBAdditionalToggle mOBAdditionalToggle = null, MOBAdditionalItems additionalItems = null, List<CMSContentMessage> lstMessages = null)
        {
            #region
            List<Model.Shopping.MOBSHOPFlattenedFlight> flattendFlights = new List<Model.Shopping.MOBSHOPFlattenedFlight>();
            foreach (var flight in flights)
            {
                #region
                Model.Shopping.MOBSHOPFlattenedFlight flattenedFlight = new Model.Shopping.MOBSHOPFlattenedFlight();
                string nearByAirportWarningKey = _configuration.GetValue<string>("WARNING_NEARBYAIRPORT");

                bool isUADiscount = false;
                if (_configuration.GetValue<bool>("EnableFSRNearByAirportAlertFeature") == true)
                {
                    if (_shoppingUtility.IsFSRNearByAirportAlertEnabled(shopRequest.Application.Id, shopRequest.Application.Version.Major) &&
                        _configuration.GetValue<bool>("DisableShowOrgAndDestAirportFlag") == false
                        && additionalItems?.ShowOrgAndDestByNearByAirport == false && flight?.FlightSegmentAlerts?.Where(d => d.Key == nearByAirportWarningKey)?.Any() == true)
                    {
                        if (additionalItems != null)
                            additionalItems.ShowOrgAndDestByNearByAirport = true;
                    }
                }

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

                        if (!_shoppingUtility.CheckFSRRedesignFromShopRequest(shopRequest))
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
                                if (_shoppingUtility.CheckFSRRedesignFromShopRequest(shopRequest))
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
                                        if (_shoppingUtility.CheckFSRRedesignFromShopRequest(shopRequest))
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
                    List<Model.Shopping.MOBSHOPFlight> connections = flight.Connections.Clone();
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
                                        if (_shoppingUtility.CheckFSRRedesignFromShopRequest(shopRequest))
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
                                                if (_shoppingUtility.CheckFSRRedesignFromShopRequest(shopRequest))
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

                        idx++;
                    }
                }

                flattenedFlight.TripDays = GetDayDifference(flattenedFlight.Flights[0].DepartDate, flattenedFlight.Flights[flattenedFlight.Flights.Count - 1].DestinationDate);
                bool isNotCrashFix = Convert.ToBoolean(_configuration.GetValue<string>("ByPassBug106828Fix") ?? "False");

                if (_configuration.GetValue<bool>("EnableOntimePerformance21FFix"))
                {
                    if (flattenedFlight?.Flights?.Any(f => !string.IsNullOrEmpty(f.OperatingCarrierDescription) && !f.OperatingCarrierDescription.Equals("United Airlines")) ?? false)
                    {
                        flattenedFlight?.Flights?.Where(f => !string.IsNullOrEmpty(f.OperatingCarrierDescription) || !f.OperatingCarrierDescription.Equals("United Airlines"))?.Select(f => f?.OperatingCarrierDescription)?.ToList().Where(s => !string.IsNullOrEmpty(s))?.Distinct()?.ForEach(c => flattenedFlight.MsgFlightCarrier += c + ", ");

                        if (!string.IsNullOrEmpty(flattenedFlight.MsgFlightCarrier))
                        {
                            if (flattenedFlight?.Flights?.Any(f => f.OperatingCarrier == "UA" && (string.IsNullOrEmpty(f.OperatingCarrierDescription) || f.OperatingCarrierDescription.Equals("United Airlines"))) ?? false)
                            {
                                flattenedFlight.MsgFlightCarrier = "Includes Travel Operated by " + flattenedFlight.MsgFlightCarrier.TrimEnd(',', ' ');
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
                try
                {
                    if (await _featureToggles.IsEnableWheelchairFilterOnFSR(shopRequest.Application.Id, shopRequest.Application.Version.Major, shopRequest.CatalogItems).ConfigureAwait(false)
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
        private Model.Shopping.MOBStyledText CovidTestBadge()
        {
            return new Model.Shopping.MOBStyledText()
            {
                BackgroundColor = MOBStyledColor.Yellow.GetDescription(),
                Text = _configuration.GetValue<string>("LabelTextCovidTest"),
                SortPriority = MOBFlightBadgeSortOrder.CovidTestRequired.ToString()
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
        private void GetFlattenedFlightsWithPrices(Model.Shopping.MOBSHOPFlattenedFlight flattenedFlight, List<Model.Shopping.MOBSHOPFlattenedFlight> flattendFlights)
        {
            bool isAddFlightToFlattenedFlights = flattenedFlight.Flights.All(flight => !flattenedFlight.Flights[0].AirfareDisplayValue.IsNullOrEmpty());

            if (isAddFlightToFlattenedFlights)
            {
                flattendFlights.Add(flattenedFlight);
            }
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
        private List<Model.Shopping.MOBStyledText> setFlightBadgeInformation(List<Model.Shopping.MOBStyledText> badges, Model.Shopping.MOBStyledText badge)
        {
            if (badges == null)
                badges = new List<Model.Shopping.MOBStyledText>();

            badges.Add(badge);

            if (badges.Count > 1)
            {
                badges = badges.OrderBy(x => (int)Enum.Parse(typeof(MOBFlightBadgeSortOrder), x.SortPriority)).ToList();
            }

            return badges;
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

        private static bool MatchServiceClassRequested(string requestedCabin, string fareClass, string prodType, List<MOBSHOPShoppingProduct> columnInfo, bool isELFFareDisplayAtFSR = true)
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

        private bool IsYoungAdultProduct(ProductCollection pc)
        {
            return pc != null && pc.Count > 0 && pc.Any(p => p.ProductSubtype.ToUpper().Equals("YOUNGADULTDISCOUNTEDFARE"));
        }
        private void GetBestProductTypeTripPlanner(Session session, Product displayProduct, bool isSelected, ref string bestProductType)
        {
            bool isTripPlannerViewEnabled = _configuration.GetValue<bool>("EnableTripPlannerView");
            if (isTripPlannerViewEnabled && session.TravelType == MOBTripPlannerType.TPSearch.ToString())
            {
                if (string.IsNullOrEmpty(bestProductType) && !isSelected)
                {
                    bestProductType = displayProduct?.ProductType;
                }
            }
        }
        private string ReShopAirfareDisplayValue(CultureInfo ci, List<PricingItem> price, bool isAward = false, bool isChangeFee = false)
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
        private decimal ReshopAirfareDisplayValueInDecimal(List<PricingItem> price)
        {
            decimal retVal = 0;
            if (price.Exists(p => p.PricingType == "AddCollect"))
                retVal = price.First(p => p.PricingType == "AddCollect").Amount;
            if (price.Exists(p => p.PricingType == "ChangeFee"))
                retVal += price.First(p => p.PricingType == "ChangeFee").Amount;

            return retVal;
        }
        private PricingItem ReshopAwardPrice(List<PricingItem> price)
        {
            if (price.Exists(p => p.PricingType == "Award"))
                return price.FirstOrDefault(p => p.PricingType == "Award");

            return null;
        }
        private string ReshopAirfareDisplayText(List<PricingItem> price)
        {
            bool isAddCollect = (price.Exists(p => p.PricingType == "AddCollect"))
                ? price.FirstOrDefault(p => p.PricingType == "AddCollect")?.Amount > 0 : false;

            bool isChangeFee = (price.Exists(p => p.PricingType == "ChangeFee"))
                ? price.FirstOrDefault(p => p.PricingType == "ChangeFee")?.Amount > 0 : false;

            return (isAddCollect && isChangeFee)
                ? "Price difference and change fee" : (isAddCollect) ? "Price difference"
                : (isChangeFee) ? "change fee" : string.Empty;
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
        private SegmentInfoAlerts TicketsLeftSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Warning.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.TicketsLeft.ToString()
            };
        }
        private string GetCabinDescription(string cos)
        {
            string cabin = string.Empty;

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
        private string FormatDateTime(string dateTimeString)
        {
            string result = string.Empty;

            DateTime dateTime = new DateTime(0);
            DateTime.TryParse(dateTimeString, out dateTime);
            result = string.Format("{0:MM/dd/yyyy hh:mm tt}", dateTime);

            return result;
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
            }
            return bkEquipmentDisclosure;
        }
        private Model.Shopping.SHOPOnTimePerformance PopulateOnTimePerformanceSHOP(United.Service.Presentation.ReferenceDataModel.DOTAirlinePerformance onTimePerformance)
        {
            Model.Shopping.SHOPOnTimePerformance shopOnTimePerformance = null;
            if (_configuration.GetValue<string>("ReturnOnTimePerformance") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnOnTimePerformance")))
            {
                #region
                if (onTimePerformance != null)
                {
                    shopOnTimePerformance = new Model.Shopping.SHOPOnTimePerformance();
                    shopOnTimePerformance.Source = onTimePerformance.Source;
                    shopOnTimePerformance.DOTMessages = new SHOPOnTimeDOTMessages();
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
        private void SetSegmentInfoMessages(Model.Shopping.MOBSHOPFlight flight, Warning warn)
        {
            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("ARRIVAL_Slice")
                && !string.IsNullOrEmpty(warn.Title))
            {
                flight.FlightSegmentAlerts = SetFlightInformationMessage(flight.FlightSegmentAlerts, ArrivesNextDaySegmentInfo(warn.Title));
            }

            if (!string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE") || (_configuration.GetValue<bool>("EnableAwardFSRChanges") && !string.IsNullOrEmpty(warn.Key) && warn.Key.Trim().ToUpper() == _configuration.GetValue<string>("CHANGE_OF_AIRPORT_SLICE_KEY")))

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
        }
        private SegmentInfoAlerts ArrivesNextDaySegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                //ConfigurationManager.AppSettings["NextDayArrivalSegmentText"],
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.ArrivesNextDay.ToString()
            };
        }
        private SegmentInfoAlerts AirportChangeSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Warning.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.AirportChange.ToString()
            };
        }
        private SegmentInfoAlerts SubjectOfReceiptOfGovtAuthSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.GovAuthority.ToString()
            };
        }
        private SegmentInfoAlerts LonglayoverSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.LongLayover.ToString()
            };
        }
        private SegmentInfoAlerts RedEyeFlightSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.RedEyeFlight.ToString()
            };
        }
        private SegmentInfoAlerts RiskyConnectionSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.RiskyConnection.ToString()
            };
        }
        private SegmentInfoAlerts TerminalChangeSegmentInfo(string msg)
        {
            return new SegmentInfoAlerts()
            {
                AlertMessage = msg,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Visibility = MOBSHOPSegmentInfoDisplay.FSRExpanded.ToString(),
                SortOrder = MOBSHOPSegmentInfoAlertsOrder.TerminalChange.ToString()
            };
        }
        private async Task<(List<Model.Shopping.MOBSHOPShoppingProduct>, Model.Shopping.MOBSHOPFlight flight)> PopulateProducts(MOBSHOPDataCarrier _mOBSHOPDataCarrier, ProductCollection products, string sessionId, Model.Shopping.MOBSHOPFlight flight, string cabin, Flight segment, decimal lowestAirfare, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClas, bool isELFFareDisplayAtFSR = true, string appVersion = "", Session session = null, MOBAdditionalItems additionalItems = null)
        {
            #region //**// LMX Flag For AppID change
            bool supressLMX = false;
            supressLMX = session.SupressLMXForAppID;
            #endregion
            if (_mOBSHOPDataCarrier == null)
                _mOBSHOPDataCarrier = new MOBSHOPDataCarrier();
            return (await PopulateProductsAsync(_mOBSHOPDataCarrier, products, cabin, segment, lowestAirfare, columnInfo, premierStatusLevel, fareClas, supressLMX, session, isELFFareDisplayAtFSR, appVersion, additionalItems), flight);
        }

        private async Task<List<MOBSHOPShoppingProduct>> PopulateProductsAsync(MOBSHOPDataCarrier _mOBSHOPDataCarrier, ProductCollection products, string cabin, Flight segment, decimal lowestAirfare,
          List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClas, bool supressLMX, Session session, bool isELFFareDisplayAtFSR = true, string appVersion = "", MOBAdditionalItems additionalItems = null)
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

                        var newProd = TransformProductWithPriceToNewProduct(cabin, segment, lowestAirfare, columnInfo,
                            premierStatusLevel, isUaDiscount, prod, supressLMX, ci, fareClass, productIndex, ref foundCabinSelected,
                            ref foundEconomyAward, ref foundBusinessAward, ref foundFirstAward, session, isELFFareDisplayAtFSR, appVersion, additionalItems);

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
                _logger.LogWarning("PopulateProductsAsync exception {ErrorMessage} {stackTrace} and {sessionId}", ex.Message, ex.StackTrace, session.SessionId);
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

        private MOBStyledText SaverAwardBadge()
        {
            return new MOBStyledText()
            {
                Text = _configuration.GetValue<string>("SaverAwardProductBadgeText"),
                SortPriority = MOBFlightProductBadgeSortOrder.SaverAward.ToString(),
                TextColor = _configuration.GetValue<string>("SaverAwardColorCode")

            };
        }

        private MOBSHOPShoppingProduct ReShopAirfareCreditDisplayFSRD(CultureInfo ci, Product product, MOBSHOPShoppingProduct shoppingProduct)
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

        private Model.Shopping.MOBStyledText MixedCabinBadge()
        {
            return new Model.Shopping.MOBStyledText()
            {
                Text = _configuration.GetValue<string>("MixedCabinProductBadgeText"),
                SortPriority = MOBFlightProductBadgeSortOrder.MixedCabin.ToString()
            };
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

        private string GetCabinDescriptionFromColumn(string type, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo)
        {
            type = type.IsNullOrEmpty() ? string.Empty : type.ToUpper().Trim();

            string cabin = string.Empty;
            if (columnInfo != null && columnInfo.Count > 0)
            {
                foreach (Model.Shopping.MOBSHOPShoppingProduct prod in columnInfo)
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
        private void SetLmxLoyaltyInformation(int premierStatusLevel, Product prod, bool supressLMX, CultureInfo ci,
            Model.Shopping.MOBSHOPShoppingProduct newProd)
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
        private void SetMileageButtonAndAwardFound(string cabin, Product prod, ref bool foundEconomyAward,
           ref bool foundBusinessAward, ref bool foundFirstAward, string cabinType, Model.Shopping.MOBSHOPShoppingProduct newProd)
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
        private void SetProductMixedCabinInformation(Flight segment, Product prod, Model.Shopping.MOBSHOPShoppingProduct newProd)
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
        private List<Model.Shopping.MOBStyledText> SetProductBadgeInformation(List<Model.Shopping.MOBStyledText> badges, Model.Shopping.MOBStyledText badge)
        {
            if (badges == null)
                badges = new List<Model.Shopping.MOBStyledText>();

            badges.Add(badge);

            if (badges.Count > 1)
            {
                badges = badges.OrderBy(x => (int)Enum.Parse(typeof(MOBFlightProductBadgeSortOrder), x.SortPriority)).ToList();
            }

            return badges;
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
        private void CalculateAwardCounts(List<Model.Shopping.MOBSHOPShoppingProduct> shopProds, ref int econAwardCount, ref int busAwardCount,
            ref int firstAwardCount)
        {
            foreach (var prod in shopProds)
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
        private void ClearMileageButtonAndAllCabinButtonText(List<Model.Shopping.MOBSHOPShoppingProduct> shopProds, int econAwardCount, int busAwardCount,
           int firstAwardCount)
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
        private void SetIsPremierCabinSaverIfApplicable(Model.Shopping.MOBSHOPShoppingProduct mobShopProduct)
        {
            if (mobShopProduct.AwardType.Trim().ToUpper().Contains("SAVER") &&
                !mobShopProduct.LongCabin.Trim().ToUpper().Contains("ECON"))
            {
                mobShopProduct.ISPremierCabinSaver = true;
            }
        }
        private async Task<MOBSearchFilters> GetSearchFilters(SearchFilterInfo filters, CultureInfo ci, int appId, string appVersion, string requestedCabin, ColumnInformation columnInfo, bool isStandardRevenueSearch, bool isAward = false, bool mixedCabinFlightExists = false, List<CMSContentMessage> lstMessages = null, Session session = null)
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
                // Get the right values for arrival date filter
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
                        List<string> warningsList = _configuration.GetValue<string>("SearchFiletersWarnings")?.Split('|').ToList();
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

                filter.ShowPriceFilters = Convert.ToBoolean(_configuration.GetValue<string>("ShowPriceFilter"));
                filter.ShowArrivalFilters = Convert.ToBoolean(_configuration.GetValue<string>("ShowArrivalFilters"));
                filter.ShowDepartureFilters = Convert.ToBoolean(_configuration.GetValue<string>("ShowDepartureFilters"));
                filter.ShowDurationFilters = Convert.ToBoolean(_configuration.GetValue<string>("ShowDurationFilters"));
                filter.ShowLayOverFilters = Convert.ToBoolean(_configuration.GetValue<string>("ShowLayOverFilters"));
                filter.ShowSortingandFilters = Convert.ToBoolean(_configuration.GetValue<string>("ShowSortingandFilters"));

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
            if (await _featureToggles.IsEnableWheelchairFilterOnFSR(appId, appVersion, session?.CatalogItems).ConfigureAwait(false))
            {
                try
                {
                    MOBSearchFilters searchFilters = new MOBSearchFilters();
                    searchFilters = await _sessionHelperService.GetSession<MOBSearchFilters>(session.SessionId, searchFilters.GetType().FullName, new List<string> { session?.SessionId, searchFilters.GetType().FullName });
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
                        if (lstMessages != null && lstMessages.Count > 0)
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
                    _logger.ILoggerError("WheelChairFilter building error {@message} {@stackTrace}", ex.Message, ex.StackTrace);
                }
            }
            filter = _shoppingUtility.SetSearchFiltersOutDefaults(filter);
            return filter;
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
        private bool IsEnableRefundableFaresToggle(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableRefundableFaresToggle") &&
                   GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("AndroidRefundableFaresToggleVersion"), _configuration.GetValue<string>("iPhoneRefundableFaresToggleVersion"));
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

        private async Task<MOBTripPlanBoardResponse> TripPlanBoard(MOBTripPlanBoardRequest request)
        {
            //Make CCE Mobile call
            MOBTripPlanBoardResponse mOBTripPlanBoardResponse = new MOBTripPlanBoardResponse();
            ContextualCommResponse cceResponse = null;
            string logAction = "TripPlanBoard";
            try
            {
                // var cceContentProcess = new CCEContextualContentProcess();
                MOBRequest mobRequest = request;
                var cceRequestHelper = new CCERequestHelper
                {
                    MileagePlusNumber = request.MpNumber,
                    ComponentToLoad = new List<string> { "TripBoard" },
                    PageToLoad = "Homepage",
                    MOBRequest = mobRequest,
                    Characteristics = null,
                    SessionId = request.SessionId,
                    LogAction = logAction
                };
                var jsonResponse = await GetCCEContentWithRequestor(cceRequestHelper);
                cceResponse = string.IsNullOrEmpty(jsonResponse) ? null : JsonConvert.DeserializeObject<ContextualCommResponse>(jsonResponse);
                if (cceResponse == null || !cceResponse.Components.Any(x => x.Name.ToUpper() == "TRIPBOARD"))
                {
                    throw new Exception("CCEResponse is empty or TripBoard Component is not loaded");
                }
                mOBTripPlanBoardResponse.Captions = GetCaptionsTripPlanBoard(cceResponse);
                mOBTripPlanBoardResponse.GroupedTrips = GetGroupedTripsTripPlanBoard(cceResponse);
                mOBTripPlanBoardResponse.WarningMessages = GetWarningMessagesTripPlanBoard(request);
                //if (cceResponse.Error?.Count > 0)
                //{
                //    logEntries.Add(LogEntry.GetLogEntry<List<Service.Presentation.CommonModel.ExceptionModel.Error>>(request.SessionId, "TripBoard", "Error - CCETripBoardResponse", mobRequest.Application.Id, mobRequest.Application.Version.Major, mobRequest.DeviceId, cceResponse.Error.ToList(), true, false));
                //}
                if (mOBTripPlanBoardResponse.GroupedTrips == null || mOBTripPlanBoardResponse.Captions == null || (mOBTripPlanBoardResponse.Captions?.FirstOrDefault(l => l.Id == "TPBoard_ButtonText")?.CurrentValue ?? "").IsNullOrEmpty())
                {
                    throw new Exception("TripBoard CCE response is not valid (Missing either trips or captions)");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return mOBTripPlanBoardResponse;

        }

        private List<InfoWarningMessages> GetWarningMessagesTripPlanBoard(MOBTripPlanBoardRequest request)
        {
            List<InfoWarningMessages> mOBInfoWarningMessages = new List<InfoWarningMessages>();
            if (request.IsDeletedMessage)
            {
                mOBInfoWarningMessages.Add(new InfoWarningMessages()
                {
                    Order = "",
                    IconType = INFOWARNINGMESSAGEICON.SUCCESS.ToString(),
                    Messages = new List<string>()
                    {
                        _configuration.GetValue<string>("TripPlanDeletedMessage")
                    }
                });
            }
            if (request.IsNotAvailableMessage)
            {
                mOBInfoWarningMessages.Add(new InfoWarningMessages()
                {
                    Order = "",
                    IconType = INFOWARNINGMESSAGEICON.WARNING.ToString(),
                    Messages = new List<string>()
                    {
                        _configuration.GetValue<string>("TripPlanNotAvailableMessage")
                    }
                });
            }
            return mOBInfoWarningMessages;
        }

        private List<TripPlanShareTrips> GetGroupedTripsTripPlanBoard(ContextualCommResponse contextualCommResponse)
        {
            List<TripPlanShareTrips> tripPlanShareTrips = null;

            if (contextualCommResponse != null)
            {
                var tripPlanner = contextualCommResponse.Components.FirstOrDefault(x => x.Name.ToUpper() == "TRIPBOARD");
                if (tripPlanner != null && tripPlanner.ContextualElements != null)
                {
                    tripPlanShareTrips = new List<TripPlanShareTrips>();
                    tripPlanner.ContextualElements.ForEach(t =>
                    {
                        var tripplantrip = GetTripPlanSharedTrips(t);
                        if (tripplantrip != null)
                        {
                            tripPlanShareTrips.Add(tripplantrip);
                        }
                    });
                }
            }
            //tripPlanShareTrips.Add(GetTripPlanSharedTrips(new ContextualElement(), "YourTrip"));
            //tripPlanShareTrips.Add(GetTripPlanSharedTrips(new ContextualElement(), "TripsShared"));
            //tripPlanShareTrips.Add(GetTripPlanSharedTrips(new ContextualElement(), "NoYourTrip"));

            return tripPlanShareTrips;
        }

        private TripPlanShareTrips GetTripPlanSharedTrips(ContextualElement contextualElement)
        {
            TripPlanShareTrips tripPlanShareTrips = null;
            if (contextualElement != null && contextualElement.Value != null)
            {
                var valueJson = JsonConvert.SerializeObject(contextualElement.Value);
                ContextualMessage value = JsonConvert.DeserializeObject<ContextualMessage>(valueJson);
                if (value != null)
                {
                    if (value.Content != null && (value.MessageKey.StartsWith("MOB.HOMEPAGE.TRIPBOARD.TBSHAREDTRIPTITLE_PILOT") || value.MessageKey.StartsWith("MOB.HOMEPAGE.TRIPBOARD.TBSHAREDTRIPTITLE_COPILOT")))
                    {
                        tripPlanShareTrips = new TripPlanShareTrips();
                        tripPlanShareTrips.ShareTitle = value.Content.Title;

                        tripPlanShareTrips.NoOfTripsToShow = Convert.ToInt32(_configuration.GetValue<string>("TripPlanBoardNoOfTripsToShow"));
                        if (value.Content.SubContents != null && value.Content.SubContents.Any())
                        {
                            tripPlanShareTrips.TripPlanDetails = new List<TripPlanDetail>();
                            value.Content.SubContents.ForEach(tc =>
                            {
                                var tripPlanDetail = new TripPlanDetail
                                {
                                    Title = tc.SubTitle,
                                    SubTitle = tc.SubTitle2
                                };
                                if (tc.Links != null && tc.Links.Count > 0)
                                {
                                    tripPlanDetail.InfoMessage = tc.Links.FirstOrDefault(lk => lk.LinkStyle == "LINK_SUMMARY")?.LinkText ?? "";
                                }
                                if (tc.Params != null && tc.Params.Any())
                                {
                                    var tripPlanId = tc.Params.Where(y => string.Equals(y.Key, "TRAVELPLANID", StringComparison.OrdinalIgnoreCase)).Select(z => z.Value).FirstOrDefault();
                                    tripPlanDetail.TripPlanId = tripPlanId;
                                }
                                tripPlanShareTrips.TripPlanDetails.Add(tripPlanDetail);
                            });
                            if (tripPlanShareTrips.TripPlanDetails.Count > tripPlanShareTrips.NoOfTripsToShow)
                            {
                                tripPlanShareTrips.ViewMoreTripsText = _configuration.GetValue<string>("TripPlanBoardViewMoreTripsText");
                                tripPlanShareTrips.ViewLessTripsText = _configuration.GetValue<string>("TripPlanBoardViewLessTripsText");
                            }
                        }
                        else
                        {
                            if (value.Content.PersonalizationKey.StartsWith("MOB.HOMEPAGE.TRIPBOARD.TBSHAREDTRIPTITLE_PILOT") || value.Content.PersonalizationKey.StartsWith("MOB.HOMEPAGE.TRIPBOARD.TBSHAREDTRIPTITLE_COPILOT"))
                            {
                                tripPlanShareTrips.NoTripsMessage = value.Content.SubTitle;
                            }
                        }
                    }
                }
            }
            return tripPlanShareTrips;
        }

        private List<MOBItem> GetCaptionsTripPlanBoard(ContextualCommResponse contextualCommResponse)
        {
            List<MOBItem> captions = null;
            if (contextualCommResponse != null)
            {
                var tripPlanner = contextualCommResponse.Components.FirstOrDefault(x => x.Name.ToUpper() == "TRIPBOARD");
                if (tripPlanner != null && tripPlanner.ContextualElements != null)
                {
                    tripPlanner.ContextualElements.ForEach(t =>
                    {
                        if (t != null && t.Value != null)
                        {
                            var valueJson = Newtonsoft.Json.JsonConvert.SerializeObject(t.Value);
                            ContextualMessage value = Newtonsoft.Json.JsonConvert.DeserializeObject<ContextualMessage>(valueJson);
                            if (value != null && (value.MessageKey.StartsWith("MOB.HOMEPAGE.TRIPBOARD.NEXTTRIPBOARD") || value.MessageKey.StartsWith("MOB.HOMEPAGE.TRIPBOARD.NEXTTRIP_NA")))
                            {
                                if (value.Content != null)
                                {
                                    captions = new List<MOBItem>();
                                    captions.Add(new MOBItem() { Id = "TPBoard_PageTitle", CurrentValue = value.Content.Title, SaveToPersist = false });
                                    captions.Add(new MOBItem() { Id = "TPBoard_BodyText", CurrentValue = value.Content.SubTitle, SaveToPersist = false });
                                    captions.Add(new MOBItem() { Id = "TPBoard_ButtonText", CurrentValue = value.Content.Links?.FirstOrDefault()?.LinkText, SaveToPersist = false });
                                }
                            }
                        }
                    });
                }
            }

            return captions;
        }


        public async Task<List<MOBSHOPFlight>> GetFlightsAsync(MOBSHOPDataCarrier _mOBSHOPDataCarrier, string sessionId, string cartId, List<Flight> segments, string requestedCabin, decimal lowestFare, List<Model.Shopping.MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, string fareClass, bool updateAmenities = false, bool isConnection = false, bool isELFFareDisplayAtFSR = true, MOBSHOPTrip trip = null, string appVersion = "", int appID = 0, Session session = null, MOBAdditionalItems additionalItems = null)
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
                    airportsList = await _sessionHelperService.GetSession<AirportDetailsList>(sessionId, new AirportDetailsList().ObjectName, new List<string> { sessionId, new AirportDetailsList().ObjectName });

                    if (airportsList == null || airportsList.AirportsList == null || airportsList.AirportsList.Count == 0)
                    {
                        airportsList = await GetAllAiportsList(segments);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetAllAiportsList GetFlights-GetAllAiportsList {@Exception}", JsonConvert.SerializeObject(ex));
                _logger.LogError("GetAllAiportsList PopulateMetaTrips-Trip {@Trip}", segments);
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
                        flight.isAddCollectWaived = GetAddCollectWaiverStatus(segment, out AddCollectProductID);
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
                                    flight.AirfareDisplayValue = "N/A";
                                    flight.MilesDisplayValue = "N/A";
                                    if (_configuration.GetValue<string>("ReturnExceptionForAnyZeroDollarAirFareValue") != null && Convert.ToBoolean(_configuration.GetValue<bool>("ReturnExceptionForAnyZeroDollarAirFareValue").ToString()))
                                    {
                                        throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
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
                                    flight.AirfareDisplayValue = "N/A";
                                    if (_configuration.GetValue<string>("ReturnExceptionForAnyZeroDollarAirFareValue") != null && Convert.ToBoolean(_configuration.GetValue<string>("ReturnExceptionForAnyZeroDollarAirFareValue").ToString()))
                                    {
                                        // Added as part of Bug 180337:mApp: "Sorry something went wrong... " Error message is displayed when selected cabin for second segment in the multi trip
                                        // throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                                        throw new MOBUnitedException(_configuration.GetValue<string>("BookingDetail_ITAError_For_CSL_10047__Error_Code"));
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

                        flight.Connections = await GetFlightsAsync(_mOBSHOPDataCarrier, sessionId, cartId, segment.Connections, requestedCabin, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR, trip, appVersion, appID, session);
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
                    flight.IsWheelChairFits = segment.IsWheelChairFits;
                    flight.EquipmentDisclosures = GetEquipmentDisclosures(segment.EquipmentDisclosures);
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
                                    List<Model.Shopping.MOBSHOPFlight> stopFlights = await GetFlightsAsync(_mOBSHOPDataCarrier, sessionId, cartId, stops, requestedCabin, lowestFare, columnInfo, premierStatusLevel, fareClass, false, true, isELFFareDisplayAtFSR, trip, appVersion, appID, session);
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
                    var tupleRes = await PopulateProducts(_mOBSHOPDataCarrier, segment.Products, sessionId, flight, requestedCabin, segment, lowestFare, columnInfo, premierStatusLevel, fareClass, isELFFareDisplayAtFSR, appVersion, session, additionalItems);
                    flight = tupleRes.flight;
                    flight.ShoppingProducts = tupleRes.Item1;


                    SetAutoFocusIfMissed(session, isELFFareDisplayAtFSR, flight.ShoppingProducts, bestProductType);


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
            // _logger.LogWarning("GetFlightsAsync end {number}", localcountDisplay);
            return flights;
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

        private MOBSHOPShoppingProduct TransformProductWithPriceToNewProduct
            (string cabin, Flight segment, decimal lowestAirfare,
            List<MOBSHOPShoppingProduct> columnInfo, int premierStatusLevel, bool isUADiscount, Product prod, bool supressLMX, CultureInfo ci,
            string fareClass, int productIndex, ref bool foundCabinSelected, ref bool foundEconomyAward,
            ref bool foundBusinessAward, ref bool foundFirstAward, Session session, bool isELFFareDisplayAtFSR, string appVersion = "", MOBAdditionalItems additionalItems = null)
        {
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

            SetProductPriceInformation(prod, ci, newProd, session, string.Empty, additionalItems);
            newProd.Meal = string.IsNullOrEmpty(prod.MealDescription) ? "None" : prod.MealDescription;


            newProd.ProductId = prod.ProductId;

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

            if (newProd.IsELF && string.IsNullOrEmpty(newProd.ProductCode))
            {
                newProd.ProductCode = _configuration.GetValue<string>("ELFProductCode"); ;
            }

            if (_configuration.GetValue<bool>("EnableOmniCartMVP2Changes"))
            {
                newProd.CabinDescription = prod.Description;
                newProd.BookingCode = prod.BookingCode;
            }
            return newProd;
        }


        private void SetProductPriceInformation(Product prod, CultureInfo ci, MOBSHOPShoppingProduct newProd, Session session, string appVersion = "", MOBAdditionalItems additionalItems = null)
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
                newProd.MilesDisplayAmount = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES ? prod.Prices[0].Amount : 0;
                newProd.MilesDisplayValue = prod.Prices[0].Currency.Trim().ToLower() == CURRENCY_TYPE_MILES
                    ? ShopStaticUtility.FormatAwardAmountForDisplay(prod.Prices[0].Amount.ToString(), true)
                    : string.Empty;
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
                        session.CatalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.AwardStrikeThroughPricing).ToString() || a.Id == ((int)AndroidCatalogEnum.AwardStrikeThroughPricing).ToString())?.CurrentValue == "1"
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
                }
                newProd.PriceAmount = prod.Prices[0].Amount;
            }
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
        public async Task<RefreshCacheForFlightCargoDimensionsResponse> RefreshCacheForFlightCargoDimensions()
        {
            RefreshCacheForFlightCargoDimensionsResponse response = null;
            bool isCacheUpdated = false;
            List<MOBDimensions> FlightDataInDb = null;
            try
            {
                FlightDataInDb = await _auroraMySqlService.GetFlghtCargoDoorDimensions().ConfigureAwait(false);
                if (FlightDataInDb == null)
                {
                    _logger.LogError("GetFlightDimensionsList failed to get data from database");
                    throw new MOBUnitedException("GetFlightDimensionsList failed to get data from database");
                }
                if (FlightDataInDb != null && FlightDataInDb.Any() && FlightDataInDb.Count > 0)
                    isCacheUpdated = await _cachingService.SaveCache<List<MOBDimensions>>(_configuration.GetValue<string>("MOBFlightCargoDimensions_Cache_key"), FlightDataInDb, "Trans01", new TimeSpan(10950, 1, 30, 0)).ConfigureAwait(false);
                if (isCacheUpdated)
                {
                    var flightDimensions = await _cachingService.GetCache<string>(_configuration.GetValue<string>("MOBFlightCargoDimensions_Cache_key"), "Trans01").ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(flightDimensions))
                    {
                        response = new RefreshCacheForFlightCargoDimensionsResponse();
                        response.FlightCargoDimensions = JsonConvert.DeserializeObject<List<MOBDimensions>>(flightDimensions);
                        if (response != null && response?.FlightCargoDimensions != null && response?.FlightCargoDimensions.Count > 0)
                        {
                            return response;
                        }
                        else
                        {
                            throw new Exception("Couldn't retrieve flight cargo dimensions data from Cache");
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            return response;
        }
    }
}
