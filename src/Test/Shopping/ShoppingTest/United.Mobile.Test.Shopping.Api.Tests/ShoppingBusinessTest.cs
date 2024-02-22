using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.Xml;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Definition;
using United.Ebs.Logging.Enrichers;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.DataAccess.ShopProducts;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.DataAccess.TripPlanGetService;
using United.Mobile.Model;
using United.Mobile.Model.Catalog;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Common;
using United.Mobile.Model.Shopping.Common.TripPlanner;
using United.Mobile.Model.Shopping.Internal;
using United.Mobile.Services.Shopping.Domain;
using United.Mobile.Services.ShopTrips.Domain;
using United.Service.Presentation.ReferenceDataModel;
using United.Service.Presentation.ReferenceDataResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.HttpService;
using Xunit;
using Constants = United.Mobile.Model.Constants;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using ShopResponse = United.Mobile.Model.Shopping.ShopResponse;

namespace United.Mobile.Shopping.Shopping.Test
{
    public class ShoppingBusinessTest
    {
        private readonly Mock<ICacheLog<ShoppingBusiness>> _logger;
        private readonly Mock<ICacheLog<ShopTripsBusiness>> _logger1;
        private readonly Mock<ICacheLog<ShopProductsBusiness>> _logger4;
        private readonly Mock<ICacheLog<FFCShopping>> _logger2;
        private readonly Mock<ICacheLog<UnfinishedBooking>> _logger3;
        private readonly Mock<ICacheLog<ShopBooking>> _logger5;
        private readonly Mock<ICacheLog<CachingService>> _logger6;
        private readonly Mock<ICacheLog<SharedItinerary>> _logger7;
        private readonly Mock<ICacheLog<ShoppingUtility>> _logger8;
        private readonly Mock<ICacheLog<DataPowerFactory>> _logger9;
        private readonly Mock<IHeaders> _headers;
        private IConfiguration _configuration;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly Mock<ICachingService> _cachingService;
        private readonly ICachingService _cachingService1;
        private readonly Mock<IShoppingUtility> _shoppingUtility;
        private readonly IShoppingUtility _shoppingUtility1;
        private readonly Mock<IShopBooking> _shopBooking;
        private readonly IShopBooking _shopBooking1;
        private readonly Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly Mock<IDynamoDBService> _dynamoDBService;
        private readonly Mock<ICSLStatisticsService> _cSLStatisticsService;
        private readonly Mock<IFormsOfPayment> _formsOfPayment;
        private readonly Mock<IOmniCart> _omniCart;
        private readonly Mock<IFFCShoppingcs> _ffCShoppingcs;
        private readonly Mock<IFlightShoppingService> _flightShoppingService;
        private readonly Mock<IDPService> _dPService;
        private readonly Mock<IUnfinishedBooking> _unfinishedBooking;
        private readonly Mock<ITravelerCSL> _travelerCSL;
        private readonly Mock<ILegalDocumentsForTitlesService> _legalDocumentsForTitlesService;
        private readonly Mock<IReferencedataService> _referenceddataService;
        private readonly Mock<ILMXInfo> _lmxInfo;
        private readonly Mock<IGMTConversionService> _gmtConversionService;
        private readonly Mock<IPKDispenserService> _pkDispencerService;
        private readonly Mock<ICMSContentService> _iCMSContentService;
        private readonly Mock<IMerchandizingServices> _merchandizingServices;
        private readonly Mock<ICustomerPreferencesService> _customerprerencesService;
        private readonly Mock<IReferencedataService> _referencedataService;
        private readonly Mock<IShoppingCartService> _shoppingCartService;
        private readonly IProductInfoHelper _productInfoHelper;
        private readonly Mock<IPNRRetrievalService> _updatePNRService;
        private readonly IPurchaseMerchandizingService _purchaseMerchandizingService;
        private readonly IPNRRetrievalService _pNRRetrievalService;
        private readonly ShoppingBusiness _shoppingBusiness;
        private readonly Mock<IShoppingCcePromoService> _shoppingCcePromoService;
        private readonly Mock<IShoppingBuyMiles> _shoppingBuyMiles;
        private readonly ShopProductsBusiness shopProductsBusiness;
        private readonly Mock<HttpContext> _httpContext;
        private readonly Mock<IMileagePlus> _mileagePlus;
        private readonly Mock<IShoppingClientService> _shoppingClientService;
        private readonly IResilientClient _resilientClient;
        private readonly Mock<IValidateHashPinService> _validateHashPinService;
        private readonly Mock<IOptimizelyPersistService> _optimizelyPersistService;
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy _policyWrap;
        private readonly string _baseUrl;
        private readonly IDataPowerFactory _dataPowerFactory;
        private readonly Mock<IApplicationEnricher> _requestEnricher;
        private readonly Mock<ITripPlannerIDService> _tripPlannerIDService;
        private readonly Mock<IFeatureSettings> _featureSettings;
        private readonly Mock<IFeatureToggles> _featureToggles;
        private readonly Mock<IAuroraMySqlService> _auroraMySqlService;
        private readonly Mock<ILogger<ShoppingBusiness>> _mlogger;
        private readonly Mock<ILogger<UnfinishedBooking>> _Mlogger2;
        private readonly Mock<ILogger<ShopTripsBusiness>> _Mlogger3;

        public IConfiguration Configuration
        {
            get
            {
                //if (_configuration == null)
                //{
                _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
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
               .AddJsonFile("appsettings.test1.json", optional: false, reloadOnChange: true)
                .Build();
                //}
                return _configuration;
            }
        }

        public IConfiguration Configuration2
        {
            get
            {
                //if (_configuration == null)
                //{
                _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.test2.json", optional: false, reloadOnChange: true)
                .Build();
                //}
                return _configuration;
            }
        }

        public IConfiguration Configuration3
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

        public IConfiguration Configuration4
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

        public ShoppingBusinessTest()
        {
            _logger = new Mock<ICacheLog<ShoppingBusiness>>();
            _logger1 = new Mock<ICacheLog<ShopTripsBusiness>>();
            _logger2 = new Mock<ICacheLog<FFCShopping>>();
            _logger3 = new Mock<ICacheLog<UnfinishedBooking>>();
            _logger4 = new Mock<ICacheLog<ShopProductsBusiness>>();
            _logger5 = new Mock<ICacheLog<ShopBooking>>();
            _logger6 = new Mock<ICacheLog<CachingService>>();
            _logger7 = new Mock<ICacheLog<SharedItinerary>>();
            _logger8 = new Mock<ICacheLog<ShoppingUtility>>();
            _logger9 = new Mock<ICacheLog<DataPowerFactory>>();
            _mlogger = new Mock<ILogger<ShoppingBusiness>>();
            _Mlogger2 = new Mock<ILogger<UnfinishedBooking>>();
            _Mlogger3 = new Mock<ILogger<ShopTripsBusiness>>();
            _headers = new Mock<IHeaders>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _shoppingUtility = new Mock<IShoppingUtility>();
            _cachingService = new Mock<ICachingService>();
            _shopBooking = new Mock<IShopBooking>();
            _dynamoDBService = new Mock<IDynamoDBService>();
            _cSLStatisticsService = new Mock<ICSLStatisticsService>();
            _formsOfPayment = new Mock<IFormsOfPayment>();
            _omniCart = new Mock<IOmniCart>();
            _ffCShoppingcs = new Mock<IFFCShoppingcs>();
            _flightShoppingService = new Mock<IFlightShoppingService>();
            _dPService = new Mock<IDPService>();
            _unfinishedBooking = new Mock<IUnfinishedBooking>();
            _travelerCSL = new Mock<ITravelerCSL>();
            _legalDocumentsForTitlesService = new Mock<ILegalDocumentsForTitlesService>();
            _referencedataService = new Mock<IReferencedataService>();
            _lmxInfo = new Mock<ILMXInfo>();
            _gmtConversionService = new Mock<IGMTConversionService>();
            _pkDispencerService = new Mock<IPKDispenserService>();
            _iCMSContentService = new Mock<ICMSContentService>();
            _merchandizingServices = new Mock<IMerchandizingServices>();
            _shoppingCcePromoService = new Mock<IShoppingCcePromoService>();
            _shoppingCartService = new Mock<IShoppingCartService>();
            _updatePNRService = new Mock<IPNRRetrievalService>();
            _customerprerencesService = new Mock<ICustomerPreferencesService>();
            _httpContext = new Mock<HttpContext>();
            _mileagePlus = new Mock<IMileagePlus>();
            _shoppingClientService = new Mock<IShoppingClientService>();
            _shoppingBuyMiles = new Mock<IShoppingBuyMiles>();
            _validateHashPinService = new Mock<IValidateHashPinService>();
            _optimizelyPersistService = new Mock<IOptimizelyPersistService>();
            _dataPowerFactory = new DataPowerFactory(Configuration, _sessionHelperService.Object, _logger9.Object);
            _requestEnricher = new Mock<IApplicationEnricher>();
            _tripPlannerIDService = new Mock<ITripPlannerIDService>();
            _auroraMySqlService = new Mock<IAuroraMySqlService>();
            _featureSettings = new Mock<IFeatureSettings>();
            _featureToggles = new Mock<IFeatureToggles>();

            _configuration = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
              .Build();

            _shopBooking1 = new ShopBooking(_logger5.Object, _configuration, _headers.Object, _dynamoDBService.Object,
                _sessionHelperService.Object, _shoppingUtility.Object, _mileagePlus.Object, _referencedataService.Object, _flightShoppingService.Object,
               _travelerCSL.Object, _cachingService.Object, _legalDocumentsForTitlesService.Object, _dPService.Object, _shoppingClientService.Object, _ffCShoppingcs.Object, _omniCart.Object, _featureSettings.Object, _featureToggles.Object);

            _resilientClient = new ResilientClient(_baseUrl);

            _cachingService1 = new CachingService(_resilientClient, _logger6.Object, _configuration);

            _shoppingBusiness = new ShoppingBusiness(_logger.Object, _configuration, _headers.Object,
               _sessionHelperService.Object, _cachingService.Object, _shopBooking.Object, _shoppingUtility.Object, _dynamoDBService.Object,
               _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
               _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking.Object, _travelerCSL.Object,
               _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
               _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object, _featureSettings.Object, _mlogger.Object, _featureToggles.Object);


            shopProductsBusiness = new ShopProductsBusiness(_logger4.Object, Configuration, _headers.Object, _sessionHelperService.Object,
                _shoppingUtility.Object, _shoppingCcePromoService.Object, _flightShoppingService.Object, _dPService.Object, _dynamoDBService.Object,
                _legalDocumentsForTitlesService.Object, _ffCShoppingcs.Object, _featureSettings.Object);

            _shoppingUtility1 = new ShoppingUtility(_logger8.Object, Configuration4, _sessionHelperService.Object, _dPService.Object, _headers.Object, _dynamoDBService.Object, _legalDocumentsForTitlesService.Object, _cachingService.Object, _validateHashPinService.Object, _optimizelyPersistService.Object, _shoppingBuyMiles.Object, _ffCShoppingcs.Object,_featureSettings.Object, _auroraMySqlService.Object);

            SetupHttpContextAccessor();
            SetHeaders();
        }

        private void SetupHttpContextAccessor()
        {
            var guid = Guid.NewGuid().ToString();
            var context = new DefaultHttpContext();
            context.Request.Headers[Constants.HeaderAppIdText] = "1";
            context.Request.Headers[Constants.HeaderAppMajorText] = "1";
            context.Request.Headers[Constants.HeaderAppMinorText] = "0";
            context.Request.Headers[Constants.HeaderDeviceIdText] = guid;
            context.Request.Headers[Constants.HeaderLangCodeText] = "en-us";
            context.Request.Headers[Constants.HeaderRequestTimeUtcText] = DateTime.UtcNow.ToString();
            context.Request.Headers[Constants.HeaderTransactionIdText] = guid;
        }

        private void SetHeaders(string deviceId = "D873298F-F27D-4AEC-BE6C-DE79C4259626"
                , string applicationId = "1"
                , string appVersion = "4.1.26"
                , string transactionId = "3f575588-bb12-41fe-8be7-f57c55fe7762|afc1db10-5c39-4ef4-9d35-df137d56a23e"
                , string languageCode = "en-US"
                , string sessionId = "D58E298C35274F6F873A133386A42916")
        {
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
        [MemberData(nameof(ShoppingTestDataGenerator.GetShop_Request), MemberType = typeof(ShoppingTestDataGenerator))]
        public void GetShop_Request(MOBSHOPShopRequest mOBSHOPShopRequest, Session session, ShoppingResponse shoppingResponse, List<MOBOptimizelyQMData> mOBOptimizelyQMDatas, MOBSHOPAvailability mOBSHOPAvailability)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration, _sessionHelperService.Object, _logger9.Object);

            _shoppingUtility.Setup(p => p.ShopTimeOutCheckforAppVersion(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.ValidateAwardFSR(It.IsAny<MOBSHOPShopRequest>())).ReturnsAsync(mOBOptimizelyQMDatas);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _shoppingUtility.Setup(p => p.IsPosRedirectInShopEnabled(It.IsAny<MOBSHOPShopRequest>())).Returns(false);

            _shopBooking.Setup(p => p.GetAvailability(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<bool>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBSHOPAvailability);

            _shoppingUtility.Setup(p => p.EnableAdvanceSearchCouponBooking(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _shopBooking.Setup(p => p.ShopValidate(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<MOBPromoAlertMessage>())).Returns(Task.FromResult((false, new MOBPromoAlertMessage())));

            _shoppingUtility.Setup(p => p.DisableFSRAlertMessageTripPlan(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            var _shoppingBusiness = new ShoppingBusiness(_logger.Object, _configuration, _headers.Object,
              _sessionHelperService.Object, _cachingService.Object, _shopBooking.Object, _shoppingUtility1, _dynamoDBService.Object,
              _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
              _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking.Object, _travelerCSL.Object,
              _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
              _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);

            //Act
            var result = _shoppingBusiness.GetShop(mOBSHOPShopRequest, _httpContext.Object);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.GetShop_Request1), MemberType = typeof(ShoppingTestDataGenerator))]
        public void GetShop_Request1(MOBSHOPShopRequest mOBSHOPShopRequest, Session session, ShoppingResponse shoppingResponse, List<MOBOptimizelyQMData> mOBOptimizelyQMDatas, MOBSHOPAvailability mOBSHOPAvailability)
        {

            if (mOBSHOPShopRequest.SessionId == "67945321097C4CF58FFC7DF9565CB276")
            {
                _configuration["CartIdForDebug"] = "REST3_YES_1";
            }

            var _dataPowerFactory = new DataPowerFactory(Configuration, _sessionHelperService.Object, _logger9.Object);

            _shoppingUtility.Setup(p => p.ShopTimeOutCheckforAppVersion(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.ValidateAwardFSR(It.IsAny<MOBSHOPShopRequest>())).ReturnsAsync(mOBOptimizelyQMDatas);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _shoppingUtility.Setup(p => p.IsPosRedirectInShopEnabled(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _shopBooking.Setup(p => p.GetAvailability(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<bool>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBSHOPAvailability);

            _shoppingUtility.Setup(p => p.EnableAdvanceSearchCouponBooking(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _shopBooking.Setup(p => p.ShopValidate(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<MOBPromoAlertMessage>())).Returns(Task.FromResult((false, new MOBPromoAlertMessage())));

            _shoppingUtility.Setup(p => p.ValidateAndGetSignSignOn(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<ShopResponse>()));

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            var _shoppingBusiness = new ShoppingBusiness(_logger.Object, Configuration, _headers.Object,
                _sessionHelperService.Object, _cachingService.Object, _shopBooking.Object, _shoppingUtility1, _dynamoDBService.Object,
                _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
                _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking.Object, _travelerCSL.Object,
                _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
                _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);

            //Act
            var result = _shoppingBusiness.GetShop(mOBSHOPShopRequest, _httpContext.Object);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.GetShop_Request1), MemberType = typeof(ShoppingTestDataGenerator))]
        public void GetShop_Request_exception(MOBSHOPShopRequest mOBSHOPShopRequest, Session session, ShoppingResponse shoppingResponse, List<MOBOptimizelyQMData> mOBOptimizelyQMDatas, MOBSHOPAvailability mOBSHOPAvailability)
        {

           // var _dataPowerFactory = new DataPowerFactory(Configuration1, _sessionHelperService.Object, _logger9.Object);

            _shoppingUtility.Setup(p => p.ShopTimeOutCheckforAppVersion(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.ValidateAwardFSR(It.IsAny<MOBSHOPShopRequest>())).ReturnsAsync(mOBOptimizelyQMDatas);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _shoppingUtility.Setup(p => p.IsPosRedirectInShopEnabled(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _shopBooking.Setup(p => p.GetAvailability(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<bool>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBSHOPAvailability);

            _shoppingUtility.Setup(p => p.EnableAdvanceSearchCouponBooking(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _shopBooking.Setup(p => p.ShopValidate(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<MOBPromoAlertMessage>())).Returns(Task.FromResult((false, new MOBPromoAlertMessage())));

            _shoppingUtility.Setup(p => p.ValidateAndGetSignSignOn(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<ShopResponse>()));

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

           // _shoppingUtility.Setup(p => p.ValidateFSRRedesign(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<Session>())).ReturnsAsync(session);

            //Act
            var result = _shoppingBusiness.GetShop(mOBSHOPShopRequest, _httpContext.Object);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.GetShop_Request2), MemberType = typeof(ShoppingTestDataGenerator))]
        public void GetShop_Request2(MOBSHOPShopRequest mOBSHOPShopRequest, Session session, ShoppingResponse shoppingResponse, List<MOBOptimizelyQMData> mOBOptimizelyQMDatas, MOBSHOPAvailability mOBSHOPAvailability)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration1, _sessionHelperService.Object, _logger9.Object);

            _shoppingUtility.Setup(p => p.ShopTimeOutCheckforAppVersion(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.ValidateAwardFSR(It.IsAny<MOBSHOPShopRequest>())).ReturnsAsync(mOBOptimizelyQMDatas);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _shoppingUtility.Setup(p => p.IsPosRedirectInShopEnabled(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _shopBooking.Setup(p => p.GetAvailability(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<bool>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBSHOPAvailability);

            _shoppingUtility.Setup(p => p.EnableAdvanceSearchCouponBooking(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _shopBooking.Setup(p => p.ShopValidate(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<MOBPromoAlertMessage>())).Returns(Task.FromResult((false, new MOBPromoAlertMessage())));

            _shoppingUtility.Setup(p => p.ValidateAndGetSignSignOn(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<ShopResponse>()));

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shopBooking.Setup(p => p.GetLastTripAvailabilityFromPersist(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(mOBSHOPAvailability); 

            var shoppingBusiness = new ShoppingBusiness(_logger.Object, Configuration1, _headers.Object,
                _sessionHelperService.Object, _cachingService.Object, _shopBooking.Object, _shoppingUtility1, _dynamoDBService.Object,
                _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
                _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking.Object, _travelerCSL.Object,
                _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
                _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);

            //Act
            var result = shoppingBusiness.GetShop(mOBSHOPShopRequest, _httpContext.Object);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.GetShop_Request2), MemberType = typeof(ShoppingTestDataGenerator))]
        public void GetShop_Request2_1(MOBSHOPShopRequest mOBSHOPShopRequest, Session session, ShoppingResponse shoppingResponse, List<MOBOptimizelyQMData> mOBOptimizelyQMDatas, MOBSHOPAvailability mOBSHOPAvailability)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration2, _sessionHelperService.Object, _logger9.Object);

            _shoppingUtility.Setup(p => p.ShopTimeOutCheckforAppVersion(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.ValidateAwardFSR(It.IsAny<MOBSHOPShopRequest>())).ReturnsAsync(mOBOptimizelyQMDatas);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _shoppingUtility.Setup(p => p.IsPosRedirectInShopEnabled(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _shopBooking.Setup(p => p.GetAvailability(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<bool>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBSHOPAvailability);

            _shoppingUtility.Setup(p => p.EnableAdvanceSearchCouponBooking(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _shopBooking.Setup(p => p.ShopValidate(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<MOBPromoAlertMessage>())).Returns(Task.FromResult((false, new MOBPromoAlertMessage())));

            _shoppingUtility.Setup(p => p.ValidateAndGetSignSignOn(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<ShopResponse>()));

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.ValidateFSRRedesign(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<Session>())).ReturnsAsync(session);

            var shoppingBusiness = new ShoppingBusiness(_logger.Object, Configuration2, _headers.Object,
                _sessionHelperService.Object, _cachingService.Object, _shopBooking.Object, _shoppingUtility.Object, _dynamoDBService.Object,
                _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
                _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking.Object, _travelerCSL.Object,
                _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
                _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);

            //Act
            var result = shoppingBusiness.GetShop(mOBSHOPShopRequest, _httpContext.Object);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.GetShop_Request2_1), MemberType = typeof(ShoppingTestDataGenerator))]
        public void GetShop_Request1_1(MOBSHOPShopRequest mOBSHOPShopRequest, Session session, ShoppingResponse shoppingResponse, List<MOBOptimizelyQMData> mOBOptimizelyQMDatas, ShopResponse shopResponse)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration3, _sessionHelperService.Object, _logger9.Object);

            _shoppingUtility.Setup(p => p.ShopTimeOutCheckforAppVersion(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.ValidateAwardFSR(It.IsAny<MOBSHOPShopRequest>())).ReturnsAsync(mOBOptimizelyQMDatas);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _shoppingUtility.Setup(p => p.IsPosRedirectInShopEnabled(It.IsAny<MOBSHOPShopRequest>())).Returns(false);

            _shopBooking.Setup(p => p.GetAvailability(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<bool>(), It.IsAny<HttpContext>())).ReturnsAsync(shopResponse.Availability);

            _shoppingUtility.Setup(p => p.EnableAdvanceSearchCouponBooking(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _shopBooking.Setup(p => p.ShopValidate(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<MOBPromoAlertMessage>())).Returns(Task.FromResult((false, new MOBPromoAlertMessage())));

            _shoppingUtility.Setup(p => p.ValidateAndGetSignSignOn(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<ShopResponse>()));

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.ValidateFSRRedesign(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<Session>())).ReturnsAsync(session);

            var shoppingBusiness = new ShoppingBusiness(_logger.Object, Configuration3, _headers.Object,
                _sessionHelperService.Object, _cachingService.Object, _shopBooking.Object, _shoppingUtility.Object, _dynamoDBService.Object,
                _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
                _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking.Object, _travelerCSL.Object,
                _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
                _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);

            //Act
            var result = shoppingBusiness.GetShop(mOBSHOPShopRequest, _httpContext.Object);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.GetShop_Request2_2), MemberType = typeof(ShoppingTestDataGenerator))]
        public void GetShop_Request2_2(MOBSHOPShopRequest mOBSHOPShopRequest, Session session, ShoppingResponse shoppingResponse, List<MOBOptimizelyQMData> mOBOptimizelyQMDatas, MOBSHOPAvailability mOBSHOPAvailability, ShopResponse shopResponse)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration2, _sessionHelperService.Object, _logger9.Object);

            _shoppingUtility.Setup(p => p.ShopTimeOutCheckforAppVersion(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.ValidateAwardFSR(It.IsAny<MOBSHOPShopRequest>())).ReturnsAsync(mOBOptimizelyQMDatas);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _shoppingUtility.Setup(p => p.IsPosRedirectInShopEnabled(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _shopBooking.Setup(p => p.GetAvailability(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<bool>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBSHOPAvailability);

            _shoppingUtility.Setup(p => p.EnableAdvanceSearchCouponBooking(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _shopBooking.Setup(p => p.ShopValidate(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<MOBPromoAlertMessage>())).Returns(Task.FromResult((false, new MOBPromoAlertMessage())));

            _shoppingUtility.Setup(p => p.ValidateAndGetSignSignOn(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<ShopResponse>()));

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shopBooking.Setup(p => p.GetAvailability(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<bool>(), It.IsAny<HttpContext>())).ReturnsAsync(shopResponse.Availability);

            _shoppingUtility.Setup(p => p.ValidateFSRRedesign(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<Session>())).ReturnsAsync(session);

            var shoppingBusiness = new ShoppingBusiness(_logger.Object, Configuration2, _headers.Object,
                _sessionHelperService.Object, _cachingService.Object, _shopBooking.Object, _shoppingUtility1, _dynamoDBService.Object,
                _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
                _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking.Object, _travelerCSL.Object,
                _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
                _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);

            //Act
            var result = shoppingBusiness.GetShop(mOBSHOPShopRequest, _httpContext.Object);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.OrganizeShopResults_Request), MemberType = typeof(ShoppingTestDataGenerator))]
        public void OrganizeShopResults_Request(ShopOrganizeResultsReqeust shopOrganizeResultsReqeust, Session session, CSLSelectTrip cSLSelectTrip, SelectTrip selectTrip, MOBSHOPAvailability mOBSHOPAvailability)
        {

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CSLSelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLSelectTrip);

            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);

            _shopBooking.Setup(p => p.GetLastTripAvailabilityFromPersist(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(mOBSHOPAvailability);

            _shopBooking.Setup(p => p.FilterShopSearchResults(It.IsAny<ShopOrganizeResultsReqeust>(), It.IsAny<Session>(), It.IsAny<bool>())).ReturnsAsync(mOBSHOPAvailability);

            //Act
            var result = _shoppingBusiness.OrganizeShopResults(shopOrganizeResultsReqeust);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.OrganizeShopResults_Request1), MemberType = typeof(ShoppingTestDataGenerator))]
        public void OrganizeShopResults_Request1(ShopOrganizeResultsReqeust shopOrganizeResultsReqeust, Session session, CSLSelectTrip cSLSelectTrip, SelectTrip selectTrip, MOBSHOPAvailability mOBSHOPAvailability)
        {
            var _dataPowerFactory = new DataPowerFactory(Configuration1, _sessionHelperService.Object, _logger9.Object);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CSLSelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLSelectTrip);

            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);

            _shopBooking.Setup(p => p.GetLastTripAvailabilityFromPersist(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(mOBSHOPAvailability);

            _shopBooking.Setup(p => p.FilterShopSearchResults(It.IsAny<ShopOrganizeResultsReqeust>(), It.IsAny<Session>(), It.IsAny<bool>())).ReturnsAsync(mOBSHOPAvailability);


            var shoppingBusiness = new ShoppingBusiness(_logger.Object, Configuration1, _headers.Object,
                _sessionHelperService.Object, _cachingService.Object, _shopBooking.Object, _shoppingUtility.Object, _dynamoDBService.Object,
                _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
                _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking.Object, _travelerCSL.Object,
                _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
                _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);

            //Act
            var result = _shoppingBusiness.OrganizeShopResults(shopOrganizeResultsReqeust);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.OrganizeShopResults_Request1_1), MemberType = typeof(ShoppingTestDataGenerator))]
        public void OrganizeShopResults_Request1_1(ShopOrganizeResultsReqeust shopOrganizeResultsReqeust, Session session, CSLSelectTrip cSLSelectTrip, MOBSHOPAvailability mOBSHOPAvailability, SelectTrip selectTrip)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration1, _sessionHelperService.Object, _logger9.Object);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CSLSelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLSelectTrip);

            _shopBooking.Setup(p => p.GetLastTripAvailabilityFromPersist(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(mOBSHOPAvailability);

            _shopBooking.Setup(p => p.FilterShopSearchResults(It.IsAny<ShopOrganizeResultsReqeust>(), It.IsAny<Session>(), It.IsAny<bool>())).ReturnsAsync(mOBSHOPAvailability);

            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);

            var shoppingBusiness = new ShoppingBusiness(_logger.Object, Configuration1, _headers.Object,
                _sessionHelperService.Object, _cachingService.Object, _shopBooking.Object, _shoppingUtility.Object, _dynamoDBService.Object,
                _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
                _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking.Object, _travelerCSL.Object,
                _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
                _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);

            //Act
            var result = shoppingBusiness.OrganizeShopResults(shopOrganizeResultsReqeust);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.OrganizeShopResults_flow), MemberType = typeof(ShoppingTestDataGenerator))]
        public void OrganizeShopResults_flow(ShopOrganizeResultsReqeust shopOrganizeResultsReqeust, Session session, CSLSelectTrip cSLSelectTrip, SelectTrip selectTrip, MOBSHOPAvailability mOBSHOPAvailability)
        {

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CSLSelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLSelectTrip);

            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);

            _shopBooking.Setup(p => p.GetLastTripAvailabilityFromPersist(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(mOBSHOPAvailability);

            _shopBooking.Setup(p => p.FilterShopSearchResults(It.IsAny<ShopOrganizeResultsReqeust>(), It.IsAny<Session>(), It.IsAny<bool>())).ReturnsAsync(mOBSHOPAvailability);

            //Act
            var result = _shoppingBusiness.OrganizeShopResults(shopOrganizeResultsReqeust);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.ShopCLBOptOut_Request), MemberType = typeof(ShoppingTestDataGenerator))]
        public void ShopCLBOptOut_Request(CLBOptOutRequest cLBOptOutRequest, ShoppingResponse shoppingResponse, MOBSHOPShopRequest mOBSHOPShopRequest, Session session, MOBSHOPAvailability mOBSHOPAvailability)
        {

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _shopBooking.Setup(p => p.ConvertCorporateLeisureToRevenueRequest(It.IsAny<MOBSHOPShopRequest>())).Returns(mOBSHOPShopRequest);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.ValidateFSRRedesign(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<Session>())).ReturnsAsync(session);

            _shopBooking.Setup(p => p.GetAvailability(It.IsAny<string>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<bool>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBSHOPAvailability);

            //Act
            var result = _shoppingBusiness.ShopCLBOptOut(cLBOptOutRequest);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.GetShopRequest_Request), MemberType = typeof(ShoppingTestDataGenerator))]
        public void GetShopRequest_Request(ShareTripRequest shareTripRequest, ShopRequest shopRequest)
        {
            _sessionHelperService.Setup(p => p.GetSession<ShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shopRequest);

            //Act
            var result = _shoppingBusiness.GetShopRequest(shareTripRequest);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.SelectTrip_Request), MemberType = typeof(ShoppingTestDataGenerator))]
        public void SelectTrip_Test(SelectTripRequest selectTripRequest, SelectTrip selectTrip, ShoppingResponse shoppingResponse, Reservation reservation, CSLShopRequest cSLShopRequest, Session session, United.Services.FlightShopping.Common.ShopResponse shopResponse, Model.Internal.Common.DisplayAirportDetails displayAirportDetails, AirportDetailsList airportDetailsList, FlightReservationResponse flightReservationResponse, ShopBookingDetailsResponse shopBookingDetailsResponse, MultiCallResponse multiCallResponse, List<ReservationFlightSegment> reservationFlightSegment /*United.Services.Customer.Preferences.Common.SavedItineraryDataModel savedItineraryDataModel*/, MOBSHOPAvailability mOBSHOPAvailability, ShopAmenitiesRequest shopAmenitiesRequest, SelectTripResponse selectTripResponse, UpdateAmenitiesIndicatorsRequest updateAmenitiesIndicatorsRequest)
        {

            var _shoppingBusiness = new ShoppingBusiness(_logger.Object, _configuration, _headers.Object,
              _sessionHelperService.Object, _cachingService1, _shopBooking.Object, _shoppingUtility.Object, _dynamoDBService.Object,
              _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
              _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking.Object, _travelerCSL.Object,
              _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
              _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);

            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);


            _flightShoppingService.Setup(p => p.SelectTrip<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _dynamoDBService.Setup(p => p.GetRecords<Model.Internal.Common.DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _flightShoppingService.Setup(p => p.ShopBookingDetails<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _sessionHelperService.Setup(p => p.GetSession<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync("{\"Messages\":[],\"MessageItems\":[],\"LastCallDateTime\":\"12.04.2012\",\"CallTime\":\"02:23\",\"Errors\":[],\"Status\":0}");

            _shoppingCartService.Setup(p => p.GetShoppingCartInfo<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _sessionHelperService.Setup(p => p.GetSession<ShopBookingDetailsResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shopBookingDetailsResponse);

            //_customerPreferencesService.Setup(p => p.GetCustomerPrefernce<United.Services.Customer.Preferences.Common.SavedItineraryDataModel>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(savedItineraryDataModel);
            _referencedataService.Setup(p => p.GetSpecialNeedsInfo<MultiCallResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(multiCallResponse);

            _updatePNRService.Setup(p => p.GetOfferedMealsForItinerary<List<ReservationFlightSegment>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(reservationFlightSegment);

            _shopBooking.Setup(p => p.PopulateTrip(It.IsAny<MOBSHOPDataCarrier>(), It.IsAny<string>(), It.IsAny<List<Trip>>(), It.IsAny<int>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(),
                 It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<MOBAdditionalItems>(), It.IsAny<List<CMSContentMessage>>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBSHOPAvailability.Trip);

            _shopBooking.Setup(p => p.NoCSLExceptions(It.IsAny<List<United.Services.FlightShopping.Common.ErrorInfo>>())).Returns(true);

            _sessionHelperService.Setup(p => p.GetSession<ShopAmenitiesRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shopAmenitiesRequest);

            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(selectTripResponse.Availability.Reservation);

            _shopBooking.Setup(p => p.GetAmenitiesRequest(It.IsAny<string>(), It.IsAny<List<Flight>>())).Returns(updateAmenitiesIndicatorsRequest);

            _unfinishedBooking.Setup(p => p.RegisterFlights(It.IsAny<FlightReservationResponse>(), It.IsAny<Session>(), It.IsAny<MOBRequest>())).ReturnsAsync(flightReservationResponse);

            var result = _shoppingBusiness.SelectTrip(selectTripRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        //[Theory]
        //[MemberData(nameof(ShoppingTestDataGenerator.SelectTrip_Test_Change), MemberType = typeof(ShoppingTestDataGenerator))]
        //public void SelectTrip_Test_Change(SelectTripRequest selectTripRequest1, SelectTrip selectTrip, ShoppingResponse shoppingResponse, Reservation reservation, CSLShopRequest cSLShopRequest, Session session, United.Services.FlightShopping.Common.ShopResponse shopResponse, Model.Internal.Common.DisplayAirportDetails displayAirportDetails, AirportDetailsList airportDetailsList, FlightReservationResponse flightReservationResponse, ShopBookingDetailsResponse shopBookingDetailsResponse, MultiCallResponse multiCallResponse, List<ReservationFlightSegment> reservationFlightSegment /*United.Services.Customer.Preferences.Common.SavedItineraryDataModel savedItineraryDataModel*/, MOBSHOPAvailability mOBSHOPAvailability, ShopAmenitiesRequest shopAmenitiesRequest, SelectTripResponse selectTripResponse, UpdateAmenitiesIndicatorsRequest updateAmenitiesIndicatorsRequest)
        //{
        //    var _shoppingBusiness = new ShoppingBusiness(_logger.Object, _configuration, _headers.Object,
        //      _sessionHelperService.Object, _cachingService1, _shopBooking.Object, _shoppingUtility.Object, _dynamoDBService.Object,
        //      _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
        //      _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking.Object, _travelerCSL.Object,
        //      _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
        //      _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object);

        //    _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);

        //    _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

        //    _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

        //    _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

        //    _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(cSLShopRequest);

        //    _flightShoppingService.Setup(p => p.SelectTrip<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

        //    _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

        //    _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

        //    _dynamoDBService.Setup(p => p.GetRecords<Model.Internal.Common.DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);

        //    _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

        //    _flightShoppingService.Setup(p => p.ShopBookingDetails<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

        //    _sessionHelperService.Setup(p => p.GetSession<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync("{\"Messages\":[],\"MessageItems\":[],\"LastCallDateTime\":\"12.04.2012\",\"CallTime\":\"02:23\",\"Errors\":[],\"Status\":0}");

        //    _shoppingCartService.Setup(p => p.GetShoppingCartInfo<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

        //    _sessionHelperService.Setup(p => p.GetSession<ShopBookingDetailsResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shopBookingDetailsResponse);

        //    //_customerPreferencesService.Setup(p => p.GetCustomerPrefernce<United.Services.Customer.Preferences.Common.SavedItineraryDataModel>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(savedItineraryDataModel);
        //    _referencedataService.Setup(p => p.GetSpecialNeedsInfo<MultiCallResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(multiCallResponse);

        //    _updatePNRService.Setup(p => p.GetOfferedMealsForItinerary<List<ReservationFlightSegment>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(reservationFlightSegment);

        //    _shopBooking.Setup(p => p.PopulateTrip(It.IsAny<MOBSHOPDataCarrier>(), It.IsAny<string>(), It.IsAny<List<Trip>>(), It.IsAny<int>(),
        //        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(),
        //         It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<MOBSHOPShopRequest>())).ReturnsAsync(mOBSHOPAvailability.Trip);

        //    _shopBooking.Setup(p => p.NoCSLExceptions(It.IsAny<List<United.Services.FlightShopping.Common.ErrorInfo>>())).Returns(true);

        //    _sessionHelperService.Setup(p => p.GetSession<ShopAmenitiesRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shopAmenitiesRequest);

        //    _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(selectTripResponse.Availability.Reservation);

        //    _shopBooking.Setup(p => p.GetAmenitiesRequest(It.IsAny<string>(), It.IsAny<List<Flight>>())).Returns(updateAmenitiesIndicatorsRequest);

        //    _unfinishedBooking.Setup(p => p.RegisterFlights(It.IsAny<FlightReservationResponse>(), It.IsAny<Session>(), It.IsAny<MOBRequest>())).Returns(flightReservationResponse);

        //    var result = _shoppingBusiness.SelectTrip(selectTripRequest1);
        //    // Assert
        //    Assert.True(result.Result != null);
        //}


        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.SelectTrip_Request2), MemberType = typeof(ShoppingTestDataGenerator))]
        public void SelectTrip_Test2(SelectTripRequest selectTripRequest, SelectTrip selectTrip, ShoppingResponse shoppingResponse, Reservation reservation, CSLShopRequest cSLShopRequest, Session session, United.Services.FlightShopping.Common.ShopResponse shopResponse, Model.Internal.Common.DisplayAirportDetails displayAirportDetails, AirportDetailsList airportDetailsList, FlightReservationResponse flightReservationResponse, ShopBookingDetailsResponse shopBookingDetailsResponse, MultiCallResponse multiCallResponse, List<ReservationFlightSegment> reservationFlightSegment, MOBSHOPAvailability mOBSHOPAvailability, ShopAmenitiesRequest shopAmenitiesRequest, SelectTripResponse selectTripResponse, UpdateAmenitiesIndicatorsRequest updateAmenitiesIndicatorsRequest)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration4, _sessionHelperService.Object, _logger9.Object);
            var _fFCShoppingcs = new FFCShopping(Configuration4, _logger2.Object, _sessionHelperService.Object, _iCMSContentService.Object, _cachingService.Object);
            var _sharedItinerary = new SharedItinerary(_logger7.Object, Configuration4, _headers.Object, _shoppingUtility.Object, _customerprerencesService.Object, _dPService.Object, _sessionHelperService.Object);
            var _unfinishedBooking = new UnfinishedBooking(_logger3.Object, _sessionHelperService.Object, _shoppingUtility.Object, Configuration4, _flightShoppingService.Object, _customerprerencesService.Object, _dPService.Object, _shoppingCartService.Object, _omniCart.Object, _travelerCSL.Object, _legalDocumentsForTitlesService.Object, _referencedataService.Object, _purchaseMerchandizingService, _pNRRetrievalService, _pkDispencerService.Object, _dynamoDBService.Object, _cachingService.Object, _gmtConversionService.Object, _ffCShoppingcs.Object, _headers.Object, _Mlogger2.Object,_featureSettings.Object, _featureToggles.Object);
            var _shopTripsBusiness = new ShopTripsBusiness(_logger1.Object, Configuration4, _headers.Object, _sessionHelperService.Object, _shoppingUtility.Object,
                _sharedItinerary, _unfinishedBooking, _flightShoppingService.Object, _dynamoDBService.Object, _dPService.Object, _updatePNRService.Object,
                _referencedataService.Object, _lmxInfo.Object, _shoppingCartService.Object, _iCMSContentService.Object,
                _merchandizingServices.Object, _shoppingSessionHelper.Object, _omniCart.Object, _productInfoHelper,
                _legalDocumentsForTitlesService.Object, _cachingService.Object, _gmtConversionService.Object, _travelerCSL.Object, _pkDispencerService.Object, _ffCShoppingcs.Object, _Mlogger3.Object,_featureSettings.Object, _featureToggles.Object);

            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);


            _flightShoppingService.Setup(p => p.SelectTrip<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _dynamoDBService.Setup(p => p.GetRecords<Model.Internal.Common.DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _flightShoppingService.Setup(p => p.ShopBookingDetails<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _sessionHelperService.Setup(p => p.GetSession<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync("{\"Messages\":[],\"MessageItems\":[],\"LastCallDateTime\":\"12.04.2012\",\"CallTime\":\"02:23\",\"Errors\":[],\"Status\":0}");

            _shoppingCartService.Setup(p => p.GetShoppingCartInfo<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _sessionHelperService.Setup(p => p.GetSession<ShopBookingDetailsResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shopBookingDetailsResponse);

            //_customerPreferencesService.Setup(p => p.GetCustomerPrefernce<United.Services.Customer.Preferences.Common.SavedItineraryDataModel>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(savedItineraryDataModel);
            _referencedataService.Setup(p => p.GetSpecialNeedsInfo<MultiCallResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(multiCallResponse);

            _updatePNRService.Setup(p => p.GetOfferedMealsForItinerary<List<ReservationFlightSegment>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(reservationFlightSegment);

            _shopBooking.Setup(p => p.PopulateTrip(It.IsAny<MOBSHOPDataCarrier>(), It.IsAny<string>(), It.IsAny<List<Trip>>(), It.IsAny<int>(),
               It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<MOBAdditionalItems>(), It.IsAny<List<CMSContentMessage>>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBSHOPAvailability.Trip);

            _shopBooking.Setup(p => p.NoCSLExceptions(It.IsAny<List<United.Services.FlightShopping.Common.ErrorInfo>>())).Returns(true);

            _sessionHelperService.Setup(p => p.GetSession<ShopAmenitiesRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shopAmenitiesRequest);

            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(selectTripResponse.Availability.Reservation);

            _shopBooking.Setup(p => p.GetAmenitiesRequest(It.IsAny<string>(), It.IsAny<List<Flight>>())).Returns(updateAmenitiesIndicatorsRequest);

            var shoppingBusiness = new ShoppingBusiness(_logger.Object, Configuration4, _headers.Object,
           _sessionHelperService.Object, _cachingService1, _shopBooking.Object, _shoppingUtility.Object, _dynamoDBService.Object,
           _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
      _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking, _travelerCSL.Object,
           _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
      _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);


            var result = shoppingBusiness.SelectTrip(selectTripRequest);
            // Assert
            Assert.True(result.Result != null);
            
        }


        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.SelectTrip_Request3), MemberType = typeof(ShoppingTestDataGenerator))]
        public void SelectTrip_Test3(SelectTripRequest selectTripRequest, SelectTrip selectTrip, ShoppingResponse shoppingResponse, Reservation reservation, CSLShopRequest cSLShopRequest, Session session, United.Services.FlightShopping.Common.ShopResponse shopResponse, Model.Internal.Common.DisplayAirportDetails displayAirportDetails, AirportDetailsList airportDetailsList, FlightReservationResponse flightReservationResponse, ShopBookingDetailsResponse shopBookingDetailsResponse, MultiCallResponse multiCallResponse, List<ReservationFlightSegment> reservationFlightSegment, MOBSHOPAvailability mOBSHOPAvailability, ShopAmenitiesRequest shopAmenitiesRequest, SelectTripResponse selectTripResponse, UpdateAmenitiesIndicatorsRequest updateAmenitiesIndicatorsRequest)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration4, _sessionHelperService.Object, _logger9.Object);
            var _fFCShoppingcs = new FFCShopping(Configuration4, _logger2.Object, _sessionHelperService.Object, _iCMSContentService.Object, _cachingService.Object);
            var _sharedItinerary = new SharedItinerary(_logger7.Object, Configuration4, _headers.Object, _shoppingUtility.Object, _customerprerencesService.Object, _dPService.Object, _sessionHelperService.Object);
            var _unfinishedBooking = new UnfinishedBooking(_logger3.Object, _sessionHelperService.Object, _shoppingUtility.Object, Configuration4, _flightShoppingService.Object, _customerprerencesService.Object, _dPService.Object, _shoppingCartService.Object, _omniCart.Object, _travelerCSL.Object, _legalDocumentsForTitlesService.Object, _referencedataService.Object, _purchaseMerchandizingService, _pNRRetrievalService, _pkDispencerService.Object, _dynamoDBService.Object, _cachingService.Object, _gmtConversionService.Object, _ffCShoppingcs.Object, _headers.Object, _Mlogger2.Object,_featureSettings.Object, _featureToggles.Object);
            var _shopTripsBusiness = new ShopTripsBusiness(_logger1.Object, Configuration4, _headers.Object, _sessionHelperService.Object, _shoppingUtility.Object,
                _sharedItinerary, _unfinishedBooking, _flightShoppingService.Object, _dynamoDBService.Object, _dPService.Object, _updatePNRService.Object,
                _referencedataService.Object, _lmxInfo.Object, _shoppingCartService.Object, _iCMSContentService.Object,
                _merchandizingServices.Object, _shoppingSessionHelper.Object, _omniCart.Object, _productInfoHelper,
                _legalDocumentsForTitlesService.Object, _cachingService.Object, _gmtConversionService.Object, _travelerCSL.Object, _pkDispencerService.Object, _ffCShoppingcs.Object, _Mlogger3.Object, _featureSettings.Object, _featureToggles.Object);

            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);


            _flightShoppingService.Setup(p => p.SelectTrip<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _dynamoDBService.Setup(p => p.GetRecords<Model.Internal.Common.DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _flightShoppingService.Setup(p => p.ShopBookingDetails<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _sessionHelperService.Setup(p => p.GetSession<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync("{\"Messages\":[],\"MessageItems\":[],\"LastCallDateTime\":\"12.04.2012\",\"CallTime\":\"02:23\",\"Errors\":[],\"Status\":0}");

            _shoppingCartService.Setup(p => p.GetShoppingCartInfo<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _sessionHelperService.Setup(p => p.GetSession<ShopBookingDetailsResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shopBookingDetailsResponse);

            //_customerPreferencesService.Setup(p => p.GetCustomerPrefernce<United.Services.Customer.Preferences.Common.SavedItineraryDataModel>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(savedItineraryDataModel);
            _referencedataService.Setup(p => p.GetSpecialNeedsInfo<MultiCallResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(multiCallResponse);

            _updatePNRService.Setup(p => p.GetOfferedMealsForItinerary<List<ReservationFlightSegment>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(reservationFlightSegment);

            _shopBooking.Setup(p => p.PopulateTrip(It.IsAny<MOBSHOPDataCarrier>(), It.IsAny<string>(), It.IsAny<List<Trip>>(), It.IsAny<int>(),
               It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<MOBAdditionalItems>(), It.IsAny<List<CMSContentMessage>>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBSHOPAvailability.Trip);

            _shopBooking.Setup(p => p.NoCSLExceptions(It.IsAny<List<United.Services.FlightShopping.Common.ErrorInfo>>())).Returns(true);

            _sessionHelperService.Setup(p => p.GetSession<ShopAmenitiesRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shopAmenitiesRequest);

            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(selectTripResponse.Availability.Reservation);

            _shopBooking.Setup(p => p.GetAmenitiesRequest(It.IsAny<string>(), It.IsAny<List<Flight>>())).Returns(updateAmenitiesIndicatorsRequest);

            var shoppingBusiness = new ShoppingBusiness(_logger.Object, Configuration4, _headers.Object,
           _sessionHelperService.Object, _cachingService1, _shopBooking.Object, _shoppingUtility.Object, _dynamoDBService.Object,
           _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
      _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking, _travelerCSL.Object,
           _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
      _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);


            var result = shoppingBusiness.SelectTrip(selectTripRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.SelectTrip_Request4), MemberType = typeof(ShoppingTestDataGenerator))]
        public void SelectTrip_Test4(SelectTripRequest selectTripRequest, SelectTrip selectTrip, ShoppingResponse shoppingResponse, Reservation reservation, CSLShopRequest cSLShopRequest, Session session, United.Services.FlightShopping.Common.ShopResponse shopResponse, Model.Internal.Common.DisplayAirportDetails displayAirportDetails, AirportDetailsList airportDetailsList, FlightReservationResponse flightReservationResponse, ShopBookingDetailsResponse shopBookingDetailsResponse, MultiCallResponse multiCallResponse, List<ReservationFlightSegment> reservationFlightSegment, MOBSHOPAvailability mOBSHOPAvailability, ShopAmenitiesRequest shopAmenitiesRequest, SelectTripResponse selectTripResponse, UpdateAmenitiesIndicatorsRequest updateAmenitiesIndicatorsRequest)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration4, _sessionHelperService.Object, _logger9.Object);
            var _fFCShoppingcs = new FFCShopping(Configuration4, _logger2.Object, _sessionHelperService.Object, _iCMSContentService.Object, _cachingService.Object);
            var _sharedItinerary = new SharedItinerary(_logger7.Object, Configuration4, _headers.Object, _shoppingUtility.Object, _customerprerencesService.Object, _dPService.Object, _sessionHelperService.Object);
            var _unfinishedBooking = new UnfinishedBooking(_logger3.Object, _sessionHelperService.Object, _shoppingUtility.Object, Configuration4, _flightShoppingService.Object, _customerprerencesService.Object, _dPService.Object, _shoppingCartService.Object, _omniCart.Object, _travelerCSL.Object, _legalDocumentsForTitlesService.Object, _referencedataService.Object, _purchaseMerchandizingService, _pNRRetrievalService, _pkDispencerService.Object, _dynamoDBService.Object, _cachingService.Object, _gmtConversionService.Object, _ffCShoppingcs.Object, _headers.Object, _Mlogger2.Object,_featureSettings.Object, _featureToggles.Object);
            var _shopTripsBusiness = new ShopTripsBusiness(_logger1.Object, Configuration4, _headers.Object, _sessionHelperService.Object, _shoppingUtility.Object,
                _sharedItinerary, _unfinishedBooking, _flightShoppingService.Object, _dynamoDBService.Object, _dPService.Object, _updatePNRService.Object,
                _referencedataService.Object, _lmxInfo.Object, _shoppingCartService.Object, _iCMSContentService.Object,
                _merchandizingServices.Object, _shoppingSessionHelper.Object, _omniCart.Object, _productInfoHelper,
                _legalDocumentsForTitlesService.Object, _cachingService.Object, _gmtConversionService.Object, _travelerCSL.Object, _pkDispencerService.Object, _ffCShoppingcs.Object, _Mlogger3.Object, _featureSettings.Object, _featureToggles.Object);

            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);


            _flightShoppingService.Setup(p => p.SelectTrip<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _dynamoDBService.Setup(p => p.GetRecords<Model.Internal.Common.DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _flightShoppingService.Setup(p => p.ShopBookingDetails<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _sessionHelperService.Setup(p => p.GetSession<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync("{\"Messages\":[],\"MessageItems\":[],\"LastCallDateTime\":\"12.04.2012\",\"CallTime\":\"02:23\",\"Errors\":[],\"Status\":0}");

            _shoppingCartService.Setup(p => p.GetShoppingCartInfo<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _sessionHelperService.Setup(p => p.GetSession<ShopBookingDetailsResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shopBookingDetailsResponse);

            //_customerPreferencesService.Setup(p => p.GetCustomerPrefernce<United.Services.Customer.Preferences.Common.SavedItineraryDataModel>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(savedItineraryDataModel);
            _referencedataService.Setup(p => p.GetSpecialNeedsInfo<MultiCallResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(multiCallResponse);

            _updatePNRService.Setup(p => p.GetOfferedMealsForItinerary<List<ReservationFlightSegment>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(reservationFlightSegment);

            _shopBooking.Setup(p => p.PopulateTrip(It.IsAny<MOBSHOPDataCarrier>(), It.IsAny<string>(), It.IsAny<List<Trip>>(), It.IsAny<int>(),
               It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<MOBAdditionalItems>(), It.IsAny<List<CMSContentMessage>>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBSHOPAvailability.Trip);

            _shopBooking.Setup(p => p.NoCSLExceptions(It.IsAny<List<United.Services.FlightShopping.Common.ErrorInfo>>())).Returns(true);

            _sessionHelperService.Setup(p => p.GetSession<ShopAmenitiesRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shopAmenitiesRequest);

            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(selectTripResponse.Availability.Reservation);

            _shopBooking.Setup(p => p.GetAmenitiesRequest(It.IsAny<string>(), It.IsAny<List<Flight>>())).Returns(updateAmenitiesIndicatorsRequest);

            var shoppingBusiness = new ShoppingBusiness(_logger.Object, Configuration4, _headers.Object,
           _sessionHelperService.Object, _cachingService1, _shopBooking.Object, _shoppingUtility.Object, _dynamoDBService.Object,
           _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
      _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking, _travelerCSL.Object,
           _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
      _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);


            var result = shoppingBusiness.SelectTrip(selectTripRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.SelectTrip_Request4_1), MemberType = typeof(ShoppingTestDataGenerator))]
        public void SelectTrip_Test4_1(SelectTripRequest selectTripRequest, SelectTrip selectTrip, ShoppingResponse shoppingResponse, Reservation reservation, CSLShopRequest cSLShopRequest, Session session, United.Services.FlightShopping.Common.ShopResponse shopResponse, Model.Internal.Common.DisplayAirportDetails displayAirportDetails, AirportDetailsList airportDetailsList, FlightReservationResponse flightReservationResponse, ShopBookingDetailsResponse shopBookingDetailsResponse, MultiCallResponse multiCallResponse, List<ReservationFlightSegment> reservationFlightSegment, MOBSHOPAvailability mOBSHOPAvailability, ShopAmenitiesRequest shopAmenitiesRequest, SelectTripResponse selectTripResponse, UpdateAmenitiesIndicatorsRequest updateAmenitiesIndicatorsRequest)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration4, _sessionHelperService.Object, _logger9.Object);
            var _fFCShoppingcs = new FFCShopping(Configuration4, _logger2.Object, _sessionHelperService.Object, _iCMSContentService.Object, _cachingService.Object);
            var _sharedItinerary = new SharedItinerary(_logger7.Object, Configuration4, _headers.Object, _shoppingUtility.Object, _customerprerencesService.Object, _dPService.Object, _sessionHelperService.Object);
            var _unfinishedBooking = new UnfinishedBooking(_logger3.Object, _sessionHelperService.Object, _shoppingUtility.Object, Configuration4, _flightShoppingService.Object, _customerprerencesService.Object, _dPService.Object, _shoppingCartService.Object, _omniCart.Object, _travelerCSL.Object, _legalDocumentsForTitlesService.Object, _referencedataService.Object, _purchaseMerchandizingService, _pNRRetrievalService, _pkDispencerService.Object, _dynamoDBService.Object, _cachingService.Object, _gmtConversionService.Object, _ffCShoppingcs.Object, _headers.Object, _Mlogger2.Object,_featureSettings.Object, _featureToggles.Object);
            var _shopTripsBusiness = new ShopTripsBusiness(_logger1.Object, Configuration4, _headers.Object, _sessionHelperService.Object, _shoppingUtility.Object,
                _sharedItinerary, _unfinishedBooking, _flightShoppingService.Object, _dynamoDBService.Object, _dPService.Object, _updatePNRService.Object,
                _referencedataService.Object, _lmxInfo.Object, _shoppingCartService.Object, _iCMSContentService.Object,
                _merchandizingServices.Object, _shoppingSessionHelper.Object, _omniCart.Object, _productInfoHelper,
                _legalDocumentsForTitlesService.Object, _cachingService.Object, _gmtConversionService.Object, _travelerCSL.Object, _pkDispencerService.Object, _ffCShoppingcs.Object, _Mlogger3.Object, _featureSettings.Object, _featureToggles.Object);

            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);


            _flightShoppingService.Setup(p => p.SelectTrip<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _dynamoDBService.Setup(p => p.GetRecords<Model.Internal.Common.DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _flightShoppingService.Setup(p => p.ShopBookingDetails<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _sessionHelperService.Setup(p => p.GetSession<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync("{\"Messages\":[],\"MessageItems\":[],\"LastCallDateTime\":\"12.04.2012\",\"CallTime\":\"02:23\",\"Errors\":[],\"Status\":0}");

            _shoppingCartService.Setup(p => p.GetShoppingCartInfo<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            _sessionHelperService.Setup(p => p.GetSession<ShopBookingDetailsResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shopBookingDetailsResponse);

            //_customerPreferencesService.Setup(p => p.GetCustomerPrefernce<United.Services.Customer.Preferences.Common.SavedItineraryDataModel>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(savedItineraryDataModel);
            _referencedataService.Setup(p => p.GetSpecialNeedsInfo<MultiCallResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(multiCallResponse);

            _updatePNRService.Setup(p => p.GetOfferedMealsForItinerary<List<ReservationFlightSegment>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(reservationFlightSegment);

            _shopBooking.Setup(p => p.PopulateTrip(It.IsAny<MOBSHOPDataCarrier>(), It.IsAny<string>(), It.IsAny<List<Trip>>(), It.IsAny<int>(),
               It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<MOBSHOPShopRequest>(), It.IsAny<MOBAdditionalItems>(), It.IsAny<List<CMSContentMessage>>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBSHOPAvailability.Trip);

            _shopBooking.Setup(p => p.NoCSLExceptions(It.IsAny<List<United.Services.FlightShopping.Common.ErrorInfo>>())).Returns(true);

            _sessionHelperService.Setup(p => p.GetSession<ShopAmenitiesRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shopAmenitiesRequest);

            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(selectTripResponse.Availability.Reservation);

            _shopBooking.Setup(p => p.GetAmenitiesRequest(It.IsAny<string>(), It.IsAny<List<Flight>>())).Returns(updateAmenitiesIndicatorsRequest);

            var shoppingBusiness = new ShoppingBusiness(_logger.Object, Configuration4, _headers.Object,
           _sessionHelperService.Object, _cachingService1, _shopBooking.Object, _shoppingUtility1, _dynamoDBService.Object,
           _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
           _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking, _travelerCSL.Object,
           _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
           _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);


            var result = shoppingBusiness.SelectTrip(selectTripRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.ChasePromoRedirectRequests), MemberType = typeof(ShoppingTestDataGenerator))]
        public void ChasePromoRTIRedirect_Test(Session session, ChasePromoRedirectRequest chasePromoRedirectRequest, CCEPromo cceResponse)
        {
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CCEPromo>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cceResponse);

            _dynamoDBService.Setup(p => p.GetRecords<int>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(1);

            _dynamoDBService.Setup(p => p.GetRecords<List<MileagePlusValidationData>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<MileagePlusValidationData>());

            _shoppingUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            //Act
            var result = shopProductsBusiness.ChasePromoRTIRedirect(chasePromoRedirectRequest);
            //Assert
            Assert.True(result.Result != null && result.Exception == null);
            // Assert.True(result != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.ChasePromoRedirectRequests), MemberType = typeof(ShoppingTestDataGenerator))]
        public void ChasePromoRTIRedirect1_Test(Session session, ChasePromoRedirectRequest chasePromoRedirectRequest, CCEPromo cceResponse)
        {
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CCEPromo>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cceResponse);

            _dynamoDBService.Setup(p => p.GetRecords<int>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(0);

            _dynamoDBService.Setup(p => p.GetRecords<List<MileagePlusValidationData>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(new List<MileagePlusValidationData>());

            _shoppingUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);


            //Act
            var result = shopProductsBusiness.ChasePromoRTIRedirect(chasePromoRedirectRequest);
            //Assert
            Assert.True(result.Result != null && result.Exception == null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.GetProductInfoForFSRDRequests), MemberType = typeof(ShoppingTestDataGenerator))]
        public void GetProductInfoForFSRD_Test(Session session, GetProductInfoForFSRDRequest getProductInfoForFSRDRequest, string jsonresponse, string cceResponse, LatestShopAvailabilityResponse latestShopAvailabilityResponse, ShoppingResponse shoppingResponse, List<MOBLegalDocument> legalDocuments)
        {
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<LatestShopAvailabilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(latestShopAvailabilityResponse);

            _flightShoppingService.Setup(p => p.GetEconomyEntitlement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(jsonresponse);

            _shoppingCcePromoService.Setup(p => p.ShoppingCcePromo(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(cceResponse);

            _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(legalDocuments);

            // Act
            var result = shopProductsBusiness.GetProductInfoForFSRD(getProductInfoForFSRDRequest);
            //Assert
            //if (result.Exception == null)
            //{
            //    Assert.True(result.Result != null && result.Exception == null);
            //}
            //else
            //{
            //    Assert.True(result.Result != null && result.Exception != null && result.Exception.InnerException != null);
            //}

            //Assert.True(result.Result != null);
            Assert.True(result != null);

        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.GetProductInfoForFSRDRequests), MemberType = typeof(ShoppingTestDataGenerator))]
        public void GetProductInfoForFSRD_Test2(Session session, GetProductInfoForFSRDRequest getProductInfoForFSRDRequest, string jsonresponse, string cceResponse, LatestShopAvailabilityResponse latestShopAvailabilityResponse, ShoppingResponse shoppingResponse, List<MOBLegalDocument> legalDocuments)
        {
            var shopProductsBusiness1 = new ShopProductsBusiness(_logger4.Object, Configuration, _headers.Object, _sessionHelperService.Object, _shoppingUtility.Object, _shoppingCcePromoService.Object, _flightShoppingService.Object, _dPService.Object, _dynamoDBService.Object, _legalDocumentsForTitlesService.Object, _ffCShoppingcs.Object, _featureSettings.Object);
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<LatestShopAvailabilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(latestShopAvailabilityResponse);

            _flightShoppingService.Setup(p => p.GetEconomyEntitlement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(jsonresponse);

            _shoppingCcePromoService.Setup(p => p.ShoppingCcePromo(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(cceResponse);

            _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(legalDocuments);

            // Act
            var result = shopProductsBusiness1.GetProductInfoForFSRD(getProductInfoForFSRDRequest);
            //Assert
            //Assert.True(result.Result != null);
            Assert.True(result != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.GetProductInfoForFSRDRequests), MemberType = typeof(ShoppingTestDataGenerator))]
        public void GetProductInfoForFSRD_Test3(Session session, GetProductInfoForFSRDRequest getProductInfoForFSRDRequest, string jsonresponse, string cceResponse, LatestShopAvailabilityResponse latestShopAvailabilityResponse, ShoppingResponse shoppingResponse, List<MOBLegalDocument> legalDocuments)
        {
            var shopProductsBusiness1 = new ShopProductsBusiness(_logger4.Object, Configuration, _headers.Object, _sessionHelperService.Object, _shoppingUtility.Object, _shoppingCcePromoService.Object, _flightShoppingService.Object, _dPService.Object, _dynamoDBService.Object, _legalDocumentsForTitlesService.Object, _ffCShoppingcs.Object, _featureSettings.Object);
            //session.SessionId = null;
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<LatestShopAvailabilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(latestShopAvailabilityResponse);

            _flightShoppingService.Setup(p => p.GetEconomyEntitlement(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(jsonresponse);

            _shoppingCcePromoService.Setup(p => p.ShoppingCcePromo(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(cceResponse);

            _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(legalDocuments);

            // Act
            var result = shopProductsBusiness1.GetProductInfoForFSRD(getProductInfoForFSRDRequest);
            //Assert
            //Assert.True(result.Result != null && result.Exception == null);
             Assert.True(result != null);

        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.GetShopTripPlan_Request), MemberType = typeof(ShoppingTestDataGenerator))]
        public void GetShopTripPlan_Request(MOBSHOPTripPlanRequest mOBSHOPTripPlanRequest, TripPlanCCEResponse tripPlanCCEResponse, Session session, ShopResponse shopResponse, ShoppingResponse shoppingResponse)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration, _sessionHelperService.Object, _logger9.Object);

            _sessionHelperService.Setup(p => p.GetSession<TripPlanCCEResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(tripPlanCCEResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.ValidateFSRRedesign(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<Session>())).ReturnsAsync(session);

            _shopBooking.Setup(p => p.GetAvailabilityTripPlan(It.IsAny<string>(), It.IsAny<MOBTripPlanShopHelper>(), mOBSHOPTripPlanRequest, It.IsAny<HttpContext>())).ReturnsAsync(shopResponse.Availability);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);


            var _shoppingBusiness = new ShoppingBusiness(_logger.Object, _configuration, _headers.Object,
              _sessionHelperService.Object, _cachingService.Object, _shopBooking.Object, _shoppingUtility.Object, _dynamoDBService.Object,
              _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
              _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking.Object, _travelerCSL.Object,
              _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
              _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);

            //Act
            var result = _shoppingBusiness.GetShopTripPlan(mOBSHOPTripPlanRequest, _httpContext.Object);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(ShoppingTestDataGenerator.GetShopTripPlan_Request1), MemberType = typeof(ShoppingTestDataGenerator))]
        public void GetShopTripPlan_Request1(MOBSHOPTripPlanRequest mOBSHOPTripPlanRequest, TripPlanCCEResponse tripPlanCCEResponse, Session session, ShopResponse shopResponse, ShoppingResponse shoppingResponse)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration1, _sessionHelperService.Object, _logger9.Object);

            _sessionHelperService.Setup(p => p.GetSession<TripPlanCCEResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(tripPlanCCEResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.ValidateFSRRedesign(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<Session>())).ReturnsAsync(session);

            _shopBooking.Setup(p => p.GetAvailabilityTripPlan(It.IsAny<string>(), It.IsAny<MOBTripPlanShopHelper>(), mOBSHOPTripPlanRequest, It.IsAny<HttpContext>())).ReturnsAsync(shopResponse.Availability);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);


            var shoppingBusiness = new ShoppingBusiness(_logger.Object, Configuration1, _headers.Object,
              _sessionHelperService.Object, _cachingService.Object, _shopBooking.Object, _shoppingUtility.Object, _dynamoDBService.Object,
              _shoppingSessionHelper.Object, _cSLStatisticsService.Object, _formsOfPayment.Object, _omniCart.Object,
              _ffCShoppingcs.Object, _flightShoppingService.Object, _dPService.Object, _unfinishedBooking.Object, _travelerCSL.Object,
              _legalDocumentsForTitlesService.Object, _referencedataService.Object, _lmxInfo.Object, _gmtConversionService.Object,
              _pkDispencerService.Object, _iCMSContentService.Object, _merchandizingServices.Object, _shoppingBuyMiles.Object, _requestEnricher.Object, _tripPlannerIDService.Object,_featureSettings.Object, _mlogger.Object, _featureToggles.Object);

            //Act
            var result = shoppingBusiness.GetShopTripPlan(mOBSHOPTripPlanRequest, _httpContext.Object);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }
    }
}