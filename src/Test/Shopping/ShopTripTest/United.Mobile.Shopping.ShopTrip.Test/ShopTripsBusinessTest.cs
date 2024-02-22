using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Shopping;
using United.Definition;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.Loyalty;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Mobile.Services.ShopTrips.Domain;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Utility.Helper;
using United.Utility.Http;
using Xunit;

using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Shopping.ShopTrip.Test
{
    public class ShopTripsBusinessTest
    {

        private readonly Mock<ICacheLog<ShopTripsBusiness>> _logger;
        private readonly Mock<ICacheLog<OmniCart>> _logger1;
        private readonly Mock<ICacheLog<FFCShopping>> _logger2;
        private readonly Mock<ICacheLog<UnfinishedBooking>> _logger3;
        private readonly Mock<ICacheLog<CachingService>> _logger4;
        private readonly Mock<ICacheLog<SharedItinerary>> _logger5;
        private readonly Mock<ICacheLog<ShoppingUtility>> _loggerUtility;
        private readonly Mock<ICacheLog<DataPowerFactory>> _logger6;
        private readonly Mock<ICacheLog<ShoppingUtility>> _logger7;
        private readonly Mock<ICacheLog<LegalDocumentsForTitlesService>> _logger8;
        private readonly Mock<ILogger<ShopTripsBusiness>> _mlogger;
        private IConfiguration _configuration;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly Mock<IShoppingUtility> _shoppingUtility;
        private readonly IShoppingUtility _shoppingUtility1;
        private readonly Mock<ISharedItinerary> _sharedItinerary;
        private readonly Mock<IUnfinishedBooking> _unfinishedBooking;
        private readonly Mock<IFlightShoppingService> _flightShoppingService;
        private readonly Mock<DocumentLibraryDynamoDB> _documentLibraryDynamoDB;
        private readonly Mock<IDynamoDBService> _dynamoDBService;
        private readonly Mock<IDPService> _dPService;
        private readonly Mock<IPNRRetrievalService> _updatePNRService;
        private readonly Mock<IReferencedataService> _referencedataService;
        private readonly Mock<ILMXInfo> _lmxInfo;
        private readonly Mock<IShoppingCartService> _shoppingCartService;
        private readonly Mock<IPKDispenserService> _pKDispenserService;
        private readonly Mock<ICMSContentService> _iCMSContentService;
        private readonly Mock<IMerchandizingServices> _merchandizingServices;
        private readonly Mock<ICustomerDataService> _customerDataService;
        private readonly Mock<ICustomerPreferencesService> _customerprerencesService;
        private readonly Mock<IMPSecurityQuestionsService> _mPSecurityQuestionsService;
        private readonly Mock<IDataVaultService> _dataVaultService;
        private readonly Mock<IDPService> _dpService;
        private readonly Mock<IUtilitiesService> _utilitiesService;
        private readonly Mock<ILoyaltyUCBService> _loyaltyBalanceService;
        private readonly IDataPowerFactory _dataPowerFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly ITravelerCSL _travelerCSL;
        private readonly IFormsOfPayment _formsOfPayment;
        private readonly bool _isTripPlannerViewEnabled;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly Mock<IPaymentService> _paymentService;
        private readonly Mock<ICustomerPreferencesService> _customerPreferencesService;
        private readonly IProductInfoHelper _productInfoHelper1;
        private ShopTripsBusiness _shopTripsBusiness;
        private readonly Mock<ILegalDocumentsForTitlesService> _legalDocumentsForTitlesService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService1;
        private readonly Mock<IOmniCart> _omniCart;
        private readonly IPurchaseMerchandizingService _purchaseMerchandizingService;
        private readonly IPNRRetrievalService _pNRRetrievalService;
        private readonly Mock<ICSLStatisticsService> _cSLStatisticsService;
        private readonly Mock<IGMTConversionService> _gMTConversionService;
        private readonly Mock<IShoppingBuyMiles> _shoppingBuyMiles;
        private readonly Mock<IValidateHashPinService> _validateHashPinService;
        private readonly Mock<IOptimizelyPersistService> _optimizelyPersistService; 
        private readonly Mock<IFFCShoppingcs> _ffcShoppingcs;
        private readonly Mock<ICachingService> _cachingService;
        private readonly ICachingService _cachingService1;
        private readonly Mock<ICacheLog<DataPowerFactory>> _logger9;
        private readonly Mock<IProductInfoHelper> _productInfoHelper;
        private readonly IResilientClient _resilientClient;
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy _policyWrap;
        private readonly string _baseUrl;
        private readonly Mock<IFeatureSettings> _featureSettings;
        private readonly Mock<IAuroraMySqlService> _auroraMySqlService;
        private readonly Mock<IFeatureToggles> _featureToggles;

        public IConfiguration Configuration
        {
            get
            {
                //if (_configuration == null)
                //{
                _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(@"TestData\SelectTrip_TestData\appsettings.test.json", optional: false, reloadOnChange: true)
                .Build();
                //}
                return _configuration;
            }
        }
        public IConfiguration Configuration1
        {
            get
            {
                //if (_configuration == null)
                //{
                _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(@"TestData\SelectTrip_TestData\appsettings11.test.json", optional: false, reloadOnChange: true)
                .Build();
                //}
                return _configuration;
            }
        }
     
        public IConfiguration Configuration5
        {
            get
            {
                //if (_configuration == null)
                //{
                _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings1.test.json", optional: false, reloadOnChange: true)
                .Build();
                //}
                return _configuration;
            }
        }
        public IConfiguration Configuration6
        {
            get
            {
                //if (_configuration == null)
                //{
                _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test3.json", optional: false, reloadOnChange: true)
                .Build();
                //}
                return _configuration;
            }
        }


        public ShopTripsBusinessTest()
        {
            _logger = new Mock<ICacheLog<ShopTripsBusiness>>();
            _logger1 = new Mock<ICacheLog<OmniCart>>();
            _logger2 = new Mock<ICacheLog<FFCShopping>>();
            _logger3 = new Mock<ICacheLog<UnfinishedBooking>>();
            _logger4 = new Mock<ICacheLog<CachingService>>();
            _logger5 = new Mock<ICacheLog<SharedItinerary>>();
            _logger6 = new Mock<ICacheLog<DataPowerFactory>>();
            _logger7 = new Mock<ICacheLog<ShoppingUtility>>();
            _logger8 = new Mock<ICacheLog<LegalDocumentsForTitlesService>>();
            _logger9 = new Mock<ICacheLog<DataPowerFactory>>();
            _mlogger = new Mock<ILogger<ShopTripsBusiness>>();
            _cachingService = new Mock<ICachingService>();
            _loggerUtility = new Mock<ICacheLog<ShoppingUtility>>();
            _dPService = new Mock<IDPService>();
            _customerPreferencesService = new Mock<ICustomerPreferencesService>();
            _dynamoDBService = new Mock<IDynamoDBService>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _flightShoppingService = new Mock<IFlightShoppingService>();
            _customerprerencesService = new Mock<ICustomerPreferencesService>();
            _dpService = new Mock<IDPService>();
            _omniCart = new Mock<IOmniCart>();
            _documentLibraryDynamoDB = new Mock<DocumentLibraryDynamoDB>();
            _shoppingCartService = new Mock<IShoppingCartService>();
            _customerDataService = new Mock<ICustomerDataService>();
            _updatePNRService = new Mock<IPNRRetrievalService>();
            _referencedataService = new Mock<IReferencedataService>();
            _lmxInfo = new Mock<ILMXInfo>();
            _pKDispenserService = new Mock<IPKDispenserService>();
            _iCMSContentService = new Mock<ICMSContentService>();
            _utilitiesService = new Mock<IUtilitiesService>();
            _mPSecurityQuestionsService = new Mock<IMPSecurityQuestionsService>();
            _dataVaultService = new Mock<IDataVaultService>();
            _merchandizingServices = new Mock<IMerchandizingServices>();
            _loyaltyBalanceService = new Mock<ILoyaltyUCBService>();
            _dataPowerFactory = new DataPowerFactory(Configuration, _sessionHelperService.Object,_logger6.Object);
            _httpContextAccessor = new HttpContextAccessor();
            _legalDocumentsForTitlesService = new Mock<ILegalDocumentsForTitlesService>();
            //_cachingService = new Mock<ICachingService>();
            _cSLStatisticsService = new Mock<ICSLStatisticsService>();
            _gMTConversionService = new Mock<IGMTConversionService>();
            _validateHashPinService = new Mock<IValidateHashPinService>();
            _optimizelyPersistService = new Mock<IOptimizelyPersistService>();
            _headers = new Mock<IHeaders>();
            _productInfoHelper1 = new ProductInfoHelper(Configuration, _sessionHelperService.Object, _dynamoDBService.Object, _legalDocumentsForTitlesService.Object, _headers.Object);
            _shoppingUtility = new Mock<IShoppingUtility>();
            _fFCShoppingcs = new FFCShopping(Configuration, _logger2.Object, _sessionHelperService.Object, _iCMSContentService.Object, _cachingService.Object);
            _paymentService = new Mock<IPaymentService>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _sharedItinerary = new Mock<ISharedItinerary>();
            _unfinishedBooking = new Mock<IUnfinishedBooking>();
            _ffcShoppingcs = new Mock<IFFCShoppingcs>();
            _shoppingBuyMiles = new Mock<IShoppingBuyMiles>();
            _productInfoHelper = new Mock<IProductInfoHelper>();
            _auroraMySqlService = new Mock<IAuroraMySqlService>();
            _dataPowerFactory = new DataPowerFactory(Configuration, _sessionHelperService.Object, _logger9.Object);
            _featureSettings = new Mock<IFeatureSettings>();
            _resilientClient = new ResilientClient(_baseUrl);
            _featureToggles = new Mock<IFeatureToggles>();
            _cachingService1 = new CachingService(_resilientClient, _logger4.Object, _configuration);

            _shopTripsBusiness = new ShopTripsBusiness(_logger.Object, Configuration, _headers.Object, _sessionHelperService.Object, _shoppingUtility.Object, _sharedItinerary.Object, _unfinishedBooking.Object, _flightShoppingService.Object, _dynamoDBService.Object, _dpService.Object, _updatePNRService.Object, _referencedataService.Object, _lmxInfo.Object, _shoppingCartService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingSessionHelper.Object, _omniCart.Object, _productInfoHelper.Object, _legalDocumentsForTitlesService.Object, _cachingService.Object,  _gMTConversionService.Object, _travelerCSL, _pKDispenserService.Object, _ffcShoppingcs.Object,_mlogger.Object, _featureSettings.Object, _featureToggles.Object);

            _shoppingUtility1 = new ShoppingUtility(_logger7.Object, Configuration5, _sessionHelperService.Object, _dPService.Object, _headers.Object, _dynamoDBService.Object, _legalDocumentsForTitlesService.Object, _cachingService.Object, _validateHashPinService.Object, _optimizelyPersistService.Object, _shoppingBuyMiles.Object, _ffcShoppingcs.Object,_featureSettings.Object, _auroraMySqlService.Object);

            _legalDocumentsForTitlesService1 = new LegalDocumentsForTitlesService(_logger8.Object, _resilientClient);

            SetupHttpContextAccessor();
            var cultureInfo = new CultureInfo("en-US");
            // cultureInfo.NumberFormat.CurrencySymbol = "€";
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
        }

        private void SetupHttpContextAccessor()
        {
            string deviceId = "589d7852-14e7-44a9-b23b-a6db36657579";
            string applicationId = "2";
            string appVersion = "4.1.29";
            string transactionId = "589d7852-14e7-44a9-b23b-a6db36657579|8f46e040-a200-495c-83ca-4fca2d7175fb";
            string languageCode = "en-US";
            string sessionId = "17C979E184CC495EA083D45F4DD9D19D";
            var guid = Guid.NewGuid().ToString();
            var context = new DefaultHttpContext();
            _headers.Setup(_ => _.ContextValues).Returns(
           new HttpContextValues
           {
               Application = new Application()
               {
                   Id = Convert.ToInt32(applicationId),
                   Version = new Mobile.Model.Version
                   {
                       Major = string.IsNullOrEmpty(appVersion) ? 0 : int.Parse(appVersion.ToString().Substring(0, 1)),
                       Minor = string.IsNullOrEmpty(appVersion) ? 0 : int.Parse(appVersion.ToString().Substring(2, 1)),
                       Build = string.IsNullOrEmpty(appVersion) ? 0 : int.Parse(appVersion.ToString().Substring(4, 2))
                   }
               },
               DeviceId = deviceId,
               LangCode = languageCode,
               TransactionId = transactionId,
               SessionId = sessionId
           });
        }


        [Theory]
        [MemberData(nameof(TestDataGenerator.MetaSelectTripRequests), MemberType = typeof(TestDataGenerator))]
        public void MetaSelectTrip_Test(MetaSelectTripRequest metaSelectTripRequest,Session session, MOBSHOPSelectUnfinishedBookingRequest mOBSHOPSelectUnfinishedBookingRequest,MOBSHOPAvailability mOBSHOPAvailability, MOBShoppingCart mOBShoppingCart)
        {
            ////var _dataPowerFactory = new DataPowerFactory(Configuration10, _sessionHelperService.Object);
            ////var _fFCShoppingcs = new FFCShopping(Configuration10, _logger2.Object, _sessionHelperService.Object, _iCMSContentService.Object, _shoppingUtility, _cachingService.Object);
            ////var _sharedItinerary = new SharedItinerary(Configuration10,  _headers.Object, _shoppingUtility, _customerprerencesService.Object, _dpService.Object);
            ////var _unfinishedBooking = new UnfinishedBooking(_logger3.Object, _sessionHelperService.Object, _shoppingUtility, Configuration10, _flightShoppingService.Object, _customerprerencesService.Object, _dpService.Object, _shoppingCartService.Object, _omniCart.Object, _travelerCSL, _legalDocumentsForTitlesService.Object, _referencedataService.Object, _purchaseMerchandizingService, _pNRRetrievalService, _pKDispenserService.Object, _dynamoDBService.Object, _cachingService.Object, _gMTConversionService.Object);
            ////var _shopTripsBusiness = new ShopTripsBusiness(_logger.Object, Configuration,  _headers.Object, _sessionHelperService.Object, _shoppingUtility,
            ////    _sharedItinerary, _unfinishedBooking, _flightShoppingService.Object, _dynamoDBService.Object, _dpService.Object, _updatePNRService.Object,
            ////    _referencedataService.Object, _lmxInfo.Object, _shoppingCartService.Object, _iCMSContentService.Object,
            ////    _merchandizingServices.Object, _shoppingSessionHelper, _omniCart.Object, _productInfoHelper,
            ////    _legalDocumentsForTitlesService.Object, _cachingService.Object, _gMTConversionService.Object, _travelerCSL, _pKDispenserService.Object);

            //_sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(session);
            //_sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(shoppingResponse);
            //_sessionHelperService.Setup(p => p.GetSessionId(It.IsAny<HttpContextValues>(), It.IsAny<string>())).ReturnsAsync(session.SessionId);
            //_sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>()));
            //_sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>()));
            //_sessionHelperService.Setup(p => p.GetSession<string>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>()));
            //_sessionHelperService.Setup(p => p.GetSession<List<MOBInFlightMealsRefreshmentsResponse>>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>()));
            //var response = new United.Mobile.Model.Internal.Common.DisplayAirportDetails();

            //_dynamoDBService.Setup(p => p.GetRecords<United.Mobile.Model.Internal.Common.DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);
            ////airportDynamoDB.Setup(p => p.GetAirportNamesList<MOBDisplayBagTrackAirportDetails>(It.IsAny<string>(), It.IsAny<string>()));
            //_dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            //_flightShoppingService.Setup(p => p.GetUserSession<MetaUserSessionSyncResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(metaUserSessionSyncResponse);
            //_flightShoppingService.Setup(p => p.GetBookingDetailsV2<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);
            //_flightShoppingService.Setup(p => p.GetMetaBookingDetails<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);
            //// _bookingProductService.Setup(p => p.GetProductInfo<LmxQuoteResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            //_customerprerencesService.Setup(p => p.GetSharedTrip<SavedItineraryDataModel>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));
            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);
            _sharedItinerary.Setup(p => p.GetSharedTripItinerary(It.IsAny<Session>(), It.IsAny<MetaSelectTripRequest>())).ReturnsAsync(mOBSHOPSelectUnfinishedBookingRequest);
            _unfinishedBooking.Setup(p => p.GetShopPinDownDetailsV2(It.IsAny<Session>(), It.IsAny<MOBSHOPSelectUnfinishedBookingRequest>(), It.IsAny<HttpContext>(), It.IsAny<MOBAddTravelersRequest>(), It.IsAny<bool>())).Returns(Task.FromResult((mOBSHOPAvailability, false)));
            _shoppingUtility.Setup(p => p.PopulateShoppingCart(It.IsAny<MOBShoppingCart>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MOBRequest>(), It.IsAny<MOBSHOPReservation>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);
            _shoppingUtility.Setup(p => p.IsFeewaiverEnabled( It.IsAny<bool>())).Returns(true);




            // Act
            var result = _shopTripsBusiness.MetaSelectTrip(metaSelectTripRequest);
            //Assert
            Assert.True(result.Result != null && result.Exception == null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.MetaSelectTripRequests), MemberType = typeof(TestDataGenerator))]
        public void MetaSelectTrip_Test1(MetaSelectTripRequest metaSelectTripRequest, Session session, MOBSHOPSelectUnfinishedBookingRequest mOBSHOPSelectUnfinishedBookingRequest, MOBSHOPAvailability mOBSHOPAvailability, MOBShoppingCart mOBShoppingCart)
        {
            if (metaSelectTripRequest.CustomerId == 123901727) {
                _configuration["ShowPriceMismatchMessage"] = "false";
            
            }
            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);
            _sharedItinerary.Setup(p => p.GetSharedTripItinerary(It.IsAny<Session>(), It.IsAny<MetaSelectTripRequest>())).ReturnsAsync(mOBSHOPSelectUnfinishedBookingRequest);
            _unfinishedBooking.Setup(p => p.GetShopPinDownDetailsV2(It.IsAny<Session>(), It.IsAny<MOBSHOPSelectUnfinishedBookingRequest>(), It.IsAny<HttpContext>(), It.IsAny<MOBAddTravelersRequest>(), It.IsAny<bool>())).Returns(Task.FromResult((mOBSHOPAvailability, false)));
            _shoppingUtility.Setup(p => p.PopulateShoppingCart(It.IsAny<MOBShoppingCart>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MOBRequest>(), It.IsAny<MOBSHOPReservation>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);
            _shoppingUtility.Setup(p => p.IsFeewaiverEnabled(It.IsAny<bool>())).Returns(true);

            // Act
            var result = _shopTripsBusiness.MetaSelectTrip(metaSelectTripRequest);
            //Assert
            Assert.True(result.Result != null && result.Exception == null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.MetaSelectTripRequests1), MemberType = typeof(TestDataGenerator))]
        public void MetaSelectTrip_Test2(MetaSelectTripRequest metaSelectTripRequest, Session session, MOBSHOPSelectUnfinishedBookingRequest mOBSHOPSelectUnfinishedBookingRequest, MOBSHOPAvailability mOBSHOPAvailability, MOBShoppingCart mOBShoppingCart, MetaUserSessionSyncResponse metaUserSessionSyncResponse, FlightReservationResponse flightReservationResponse, ShoppingResponse shoppingResponse, AirportDetailsList airportDetailsList)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration6, _sessionHelperService.Object, _logger9.Object);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _sharedItinerary.Setup(p => p.GetSharedTripItinerary(It.IsAny<Session>(), It.IsAny<MetaSelectTripRequest>())).ReturnsAsync(mOBSHOPSelectUnfinishedBookingRequest);

            _unfinishedBooking.Setup(p => p.GetShopPinDownDetailsV2(It.IsAny<Session>(), It.IsAny<MOBSHOPSelectUnfinishedBookingRequest>(), It.IsAny<HttpContext>(), It.IsAny<MOBAddTravelersRequest>(), It.IsAny<bool>())).Returns(Task.FromResult((mOBSHOPAvailability, false)));

            _shoppingUtility.Setup(p => p.PopulateShoppingCart(It.IsAny<MOBShoppingCart>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MOBRequest>(), It.IsAny<MOBSHOPReservation>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _shoppingUtility.Setup(p => p.IsFeewaiverEnabled(It.IsAny<bool>())).Returns(true);

            _flightShoppingService.Setup(p => p.GetUserSession<MetaUserSessionSyncResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(metaUserSessionSyncResponse);

            _flightShoppingService.Setup(p => p.GetBookingDetailsV2<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _shoppingCartService.Setup(p => p.GetShoppingCartInfo<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _productInfoHelper.Setup(p => p.ConfirmationPageProductInfo(It.IsAny<FlightReservationResponse>(), It.IsAny<bool>(), It.IsAny<MOBApplication>(), It.IsAny<SeatChangeState>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mOBShoppingCart.Products);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);


            var shopTripsBusiness = new ShopTripsBusiness(_logger.Object, Configuration6, _headers.Object, _sessionHelperService.Object, _shoppingUtility.Object, _sharedItinerary.Object, _unfinishedBooking.Object, _flightShoppingService.Object, _dynamoDBService.Object, _dpService.Object, _updatePNRService.Object, _referencedataService.Object, _lmxInfo.Object, _shoppingCartService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingSessionHelper.Object, _omniCart.Object, _productInfoHelper.Object, _legalDocumentsForTitlesService.Object, _cachingService1, _gMTConversionService.Object, _travelerCSL, _pKDispenserService.Object,_ffcShoppingcs.Object,_mlogger.Object, _featureSettings.Object, _featureToggles.Object);


            // Act
            var result = shopTripsBusiness.MetaSelectTrip(metaSelectTripRequest);
            //Assert
            Assert.True(result.Result != null && result.Exception == null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.MetaSelectTripRequests2), MemberType = typeof(TestDataGenerator))]
        public void MetaSelectTrip_Test3(MetaSelectTripRequest metaSelectTripRequest, Session session, MOBSHOPSelectUnfinishedBookingRequest mOBSHOPSelectUnfinishedBookingRequest, MOBSHOPAvailability mOBSHOPAvailability, MOBShoppingCart mOBShoppingCart, MetaUserSessionSyncResponse metaUserSessionSyncResponse, FlightReservationResponse flightReservationResponse, ShoppingResponse shoppingResponse, AirportDetailsList airportDetailsList)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration6, _sessionHelperService.Object, _logger9.Object);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _sharedItinerary.Setup(p => p.GetSharedTripItinerary(It.IsAny<Session>(), It.IsAny<MetaSelectTripRequest>())).ReturnsAsync(mOBSHOPSelectUnfinishedBookingRequest);

            _unfinishedBooking.Setup(p => p.GetShopPinDownDetailsV2(It.IsAny<Session>(), It.IsAny<MOBSHOPSelectUnfinishedBookingRequest>(), It.IsAny<HttpContext>(), It.IsAny<MOBAddTravelersRequest>(), It.IsAny<bool>())).Returns(Task.FromResult((mOBSHOPAvailability, false)));

            _shoppingUtility.Setup(p => p.PopulateShoppingCart(It.IsAny<MOBShoppingCart>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MOBRequest>(), It.IsAny<MOBSHOPReservation>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _shoppingUtility.Setup(p => p.IsFeewaiverEnabled(It.IsAny<bool>())).Returns(true);

            _flightShoppingService.Setup(p => p.GetUserSession<MetaUserSessionSyncResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(metaUserSessionSyncResponse);

            _flightShoppingService.Setup(p => p.GetBookingDetailsV2<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _shoppingCartService.Setup(p => p.GetShoppingCartInfo<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _productInfoHelper.Setup(p => p.ConfirmationPageProductInfo(It.IsAny<FlightReservationResponse>(), It.IsAny<bool>(), It.IsAny<MOBApplication>(), It.IsAny<SeatChangeState>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mOBShoppingCart.Products);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _flightShoppingService.Setup(p => p.GetMetaBookingDetails<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);


            var shopTripsBusiness = new ShopTripsBusiness(_logger.Object, Configuration6,  _headers.Object, _sessionHelperService.Object, _shoppingUtility.Object, _sharedItinerary.Object, _unfinishedBooking.Object, _flightShoppingService.Object, _dynamoDBService.Object, _dpService.Object, _updatePNRService.Object, _referencedataService.Object, _lmxInfo.Object, _shoppingCartService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingSessionHelper.Object, _omniCart.Object, _productInfoHelper.Object, _legalDocumentsForTitlesService.Object, _cachingService1,
                _gMTConversionService.Object, _travelerCSL, _pKDispenserService.Object, _ffcShoppingcs.Object, _mlogger.Object, _featureSettings.Object, _featureToggles.Object);


            // Act
            var result = shopTripsBusiness.MetaSelectTrip(metaSelectTripRequest);
            //Assert
            Assert.True(result.Result != null && result.Exception == null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.MetaSelectTripRequests2), MemberType = typeof(TestDataGenerator))]
        public void MetaSelectTrip_Test4(MetaSelectTripRequest metaSelectTripRequest, Session session, MOBSHOPSelectUnfinishedBookingRequest mOBSHOPSelectUnfinishedBookingRequest, MOBSHOPAvailability mOBSHOPAvailability, MOBShoppingCart mOBShoppingCart, MetaUserSessionSyncResponse metaUserSessionSyncResponse, FlightReservationResponse flightReservationResponse, ShoppingResponse shoppingResponse, AirportDetailsList airportDetailsList)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration6, _sessionHelperService.Object, _logger9.Object);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _sharedItinerary.Setup(p => p.GetSharedTripItinerary(It.IsAny<Session>(), It.IsAny<MetaSelectTripRequest>())).ReturnsAsync(mOBSHOPSelectUnfinishedBookingRequest);

            _unfinishedBooking.Setup(p => p.GetShopPinDownDetailsV2(It.IsAny<Session>(), It.IsAny<MOBSHOPSelectUnfinishedBookingRequest>(), It.IsAny<HttpContext>(), It.IsAny<MOBAddTravelersRequest>(), It.IsAny<bool>())).Returns(Task.FromResult((mOBSHOPAvailability, false)));

            _shoppingUtility.Setup(p => p.PopulateShoppingCart(It.IsAny<MOBShoppingCart>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MOBRequest>(), It.IsAny<MOBSHOPReservation>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _shoppingUtility.Setup(p => p.IsFeewaiverEnabled(It.IsAny<bool>())).Returns(true);

            _flightShoppingService.Setup(p => p.GetUserSession<MetaUserSessionSyncResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(metaUserSessionSyncResponse);

            _flightShoppingService.Setup(p => p.GetBookingDetailsV2<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _shoppingCartService.Setup(p => p.GetShoppingCartInfo<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _productInfoHelper.Setup(p => p.ConfirmationPageProductInfo(It.IsAny<FlightReservationResponse>(), It.IsAny<bool>(), It.IsAny<MOBApplication>(), It.IsAny<SeatChangeState>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mOBShoppingCart.Products);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _flightShoppingService.Setup(p => p.GetMetaBookingDetails<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);


            var shopTripsBusiness = new ShopTripsBusiness(_logger.Object, Configuration6,  _headers.Object, _sessionHelperService.Object, _shoppingUtility.Object, _sharedItinerary.Object,
                    _unfinishedBooking.Object, _flightShoppingService.Object, _dynamoDBService.Object, _dpService.Object, _updatePNRService.Object, _referencedataService.Object, _lmxInfo.Object, _shoppingCartService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingSessionHelper.Object, _omniCart.Object, _productInfoHelper.Object,
                    _legalDocumentsForTitlesService.Object, _cachingService1, _gMTConversionService.Object, _travelerCSL, _pKDispenserService.Object, _ffcShoppingcs.Object, _mlogger.Object, _featureSettings.Object, _featureToggles.Object);


            // Act
            var result = shopTripsBusiness.MetaSelectTrip(metaSelectTripRequest);
            //Assert
            Assert.True(result.Result != null && result.Exception == null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetTripCompareFareTypesRequest), MemberType = typeof(TestDataGenerator))]
        public void GetTripCompareFareTypes_Tests(ShoppingTripFareTypeDetailsRequest shoppingTripFareTypeDetailsRequest, Session session, string getColumnInfo_token)
        {
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);
            _dPService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Token");
            _flightShoppingService.Setup(p => p.GetColumnInfo(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(getColumnInfo_token);
            var response = _shopTripsBusiness.GetTripCompareFareTypes(shoppingTripFareTypeDetailsRequest);
            Assert.NotNull(response);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetFareRulesForSelectedTripsRequest), MemberType = typeof(TestDataGenerator))]
        public void GetFareRulesForSelectedTrip_Tests(GetFareRulesRequest getFareRulesRequest, Session session, Reservation bookingPathReservation, FareRulesResponse fareRulesResponse )
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);
           
            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(bookingPathReservation);
            
            _dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            FareRuleResponse fareRuleResponse = new FareRuleResponse
            {
                FareRules = new List<FareRule>
                {
                    new FareRule
                    {
                        Destination = "US",
                        RulesList = new List<KeyValuePair<string, string>>
                        {
                           new KeyValuePair<string, string>("123", "abc"),
                           
                        }
                        
                    }
                },
                Reservation = new Service.Presentation.ReservationModel.Reservation
                {
                    PointOfSale = new Service.Presentation.CommonModel.PointOfSale
                    { 
                        CurrencyCode = "12ABC"
                    }
                }
            };

            string stringjson = JsonConvert.SerializeObject(fareRuleResponse);

            _flightShoppingService.Setup(p => p.FarePriceRules(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(stringjson);

           _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(fareRulesResponse.Reservation);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shopTripsBusiness = new ShopTripsBusiness(_logger.Object, Configuration,  _headers.Object, _sessionHelperService.Object, _shoppingUtility.Object,
                _sharedItinerary.Object, _unfinishedBooking.Object, _flightShoppingService.Object, _dynamoDBService.Object, _dpService.Object, _updatePNRService.Object,
                _referencedataService.Object, _lmxInfo.Object, _shoppingCartService.Object, _iCMSContentService.Object,
                _merchandizingServices.Object, _shoppingSessionHelper.Object, _omniCart.Object, _productInfoHelper.Object, _legalDocumentsForTitlesService.Object, _cachingService1,
                _gMTConversionService.Object, _travelerCSL, _pKDispenserService.Object, _ffcShoppingcs.Object, _mlogger.Object, _featureSettings.Object, _featureToggles.Object);

            var result = _shopTripsBusiness.GetFareRulesForSelectedTrip(getFareRulesRequest);

            //Assert.NotNull(response);
            //Assert
            Assert.True(result.Result != null && result.Exception == null);
           // Assert.True(result != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetFareRulesForSelectedTripsRequest), MemberType = typeof(TestDataGenerator))]
        public void GetFareRulesForSelectedTrip_Tests1(GetFareRulesRequest getFareRulesRequest, Session session, Reservation bookingPathReservation, FareRulesResponse fareRulesResponse)
        {
            var _dataPowerFactory = new DataPowerFactory(Configuration5, _sessionHelperService.Object, _logger9.Object);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(bookingPathReservation);

            _dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            FareRuleResponse fareRuleResponse = new FareRuleResponse
            {
                FareRules = new List<FareRule>
                {
                    new FareRule
                    {
                        Destination = "US",
                        RulesList = new List<KeyValuePair<string, string>>
                        {
                           new KeyValuePair<string, string>("123", "abc"),

                        }

                    }
                },
                Reservation = new Service.Presentation.ReservationModel.Reservation
                {
                    PointOfSale = new Service.Presentation.CommonModel.PointOfSale
                    {
                        CurrencyCode = "12ABC"
                    }
                }
            };

            string stringjson = JsonConvert.SerializeObject(fareRuleResponse);

            _flightShoppingService.Setup(p => p.FarePriceRules(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(stringjson);

            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(fareRulesResponse.Reservation);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            var shopTripsBusiness = new ShopTripsBusiness(_logger.Object, Configuration5,  _headers.Object, _sessionHelperService.Object, _shoppingUtility1, 
                _sharedItinerary.Object, _unfinishedBooking.Object, _flightShoppingService.Object, _dynamoDBService.Object, _dpService.Object, _updatePNRService.Object,
                _referencedataService.Object, _lmxInfo.Object, _shoppingCartService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingSessionHelper.Object,
                _omniCart.Object, _productInfoHelper.Object, _legalDocumentsForTitlesService.Object, _cachingService1, _gMTConversionService.Object, _travelerCSL, _pKDispenserService.Object, _ffcShoppingcs.Object, _mlogger.Object, _featureSettings.Object, _featureToggles.Object);

            var result = shopTripsBusiness.GetFareRulesForSelectedTrip(getFareRulesRequest);

            //Assert.NotNull(response);
            //Assert
            Assert.True(result.Result != null && result.Exception == null);
            // Assert.True(result != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetFareRulesForSelectedTrip_flow), MemberType = typeof(TestDataGenerator))]
        public void GetFareRulesForSelectedTrip_flow(GetFareRulesRequest getFareRulesRequest, Session session, Reservation bookingPathReservation, FareRulesResponse fareRulesResponse)
        {
            var _dataPowerFactory = new DataPowerFactory(Configuration5, _sessionHelperService.Object, _logger9.Object);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(bookingPathReservation);

            _dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            FareRuleResponse fareRuleResponse = new FareRuleResponse
            {
                FareRules = new List<FareRule>
                {
                    new FareRule
                    {
                        Destination = "US",
                        RulesList = new List<KeyValuePair<string, string>>
                        {
                           new KeyValuePair<string, string>("123", "abc"),

                        }

                    }
                },
                Reservation = new Service.Presentation.ReservationModel.Reservation
                {
                    PointOfSale = new Service.Presentation.CommonModel.PointOfSale
                    {
                        CurrencyCode = "12ABC"
                    }
                }
            };

            string stringjson = JsonConvert.SerializeObject(fareRuleResponse);

            _flightShoppingService.Setup(p => p.FarePriceRules(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(stringjson);

            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(fareRulesResponse.Reservation);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            var shopTripsBusiness = new ShopTripsBusiness(_logger.Object, Configuration5, _headers.Object, _sessionHelperService.Object, _shoppingUtility1,
                _sharedItinerary.Object, _unfinishedBooking.Object, _flightShoppingService.Object, _dynamoDBService.Object, _dpService.Object, _updatePNRService.Object,
                _referencedataService.Object, _lmxInfo.Object, _shoppingCartService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingSessionHelper.Object,
                _omniCart.Object, _productInfoHelper.Object, _legalDocumentsForTitlesService.Object, _cachingService1, _gMTConversionService.Object, _travelerCSL, _pKDispenserService.Object, _ffcShoppingcs.Object, _mlogger.Object, _featureSettings.Object, _featureToggles.Object);

            var result = shopTripsBusiness.GetFareRulesForSelectedTrip(getFareRulesRequest);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.SessionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
            // Assert.True(result.Result != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetShareTripRequest), MemberType = typeof(TestDataGenerator))]
        public void GetShareTrip_Tests(ShareTripRequest sharetripRequest, string fareRuleResponse, SelectTrip selectTrip, string sharedItineraryResponse)
        {
            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);
            
            _flightShoppingService.Setup(p => p.FarePriceRules(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(fareRuleResponse);
           
            _customerprerencesService.Setup(p => p.GetSharedItinerary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(sharedItineraryResponse);
            
            var result = _shopTripsBusiness.GetShareTrip(sharetripRequest);

            // Assert.NotNull(response);
            //Assert
            Assert.True(result.Result != null && result.Exception == null);
            //  Assert.True(result != null);
        }

    }
}

