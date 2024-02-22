using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using United.Common.Helper;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.ShopAward;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Services.ShopAward.Domain;
using United.Service.Presentation.ReservationResponseModel;
using Xunit;
using CSLShopRequest = United.Mobile.Model.Shopping.CSLShopRequest;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using Session = United.Mobile.Model.Common.Session;
using ShopResponse = United.Services.FlightShopping.Common.ShopResponse;
using United.Utility.Helper;

namespace United.Mobile.Shopping.ShopAward.Test
{
    public class ShopAwardBusinessTest
    {
        private readonly IShopAwardBusiness _shopAwardBusiness;
        private readonly Mock<ICacheLog<ShopAwardBusiness>> _logger;
        private readonly Mock<ICacheLog<DataPowerFactory>> _logger9;
        private readonly Mock<ICacheLog<ShoppingUtility>> _loggerUtility;
        private IConfiguration _configuration;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility1;
        private readonly Mock<IShoppingUtility> _shoppingUtility;
        private readonly Mock<IFlightShoppingService> _flightShoppingService;
        private readonly Mock<IDPService> _dPService;
        private readonly Mock<IAwardCalendarAzureService> _awardCalendarAzureService;
        private readonly Mock<IMileagePlus> _mileagePlus;
        private readonly Mock<IDataPowerFactory> _dataPowerFactory;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<IDynamoDBService> _dynamoDBServices;
        private readonly Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly Mock<ILegalDocumentsForTitlesService> _legalDocumentsForTitlesService;
        private readonly Mock<ICachingService> _cachingService;
        private readonly Mock<IValidateHashPinService> _validateHashPinService;
        private readonly Mock<IShoppingBuyMiles> _shoppingBuyMiles;
        private readonly Mock<IOptimizelyPersistService> _optimizelyPersistService;
        private readonly Mock<IFFCShoppingcs> _ffcShoppingcs;
        private readonly Mock<IFeatureSettings> _featureSettings;
        private readonly Mock<IAuroraMySqlService> _auroraMySqlService;
       

        public IConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
                    .Build();
                }
                return _configuration;
            }
        }

        public IConfiguration Configuration2
        {
            get
            {

                _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings1.test.json", optional: false, reloadOnChange: true)
                .Build();

                return _configuration;
            }
        }

        public ShopAwardBusinessTest()
        {
            _logger = new Mock<ICacheLog<ShopAwardBusiness>>();
            _logger9 = new Mock<ICacheLog<DataPowerFactory>>();
            _loggerUtility = new Mock<ICacheLog<ShoppingUtility>>();
            _dPService = new Mock<IDPService>();
            _dynamoDBServices = new Mock<IDynamoDBService>();
            _flightShoppingService = new Mock<IFlightShoppingService>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _shoppingUtility = new Mock<IShoppingUtility>();
            _awardCalendarAzureService = new Mock<IAwardCalendarAzureService>();
            _mileagePlus = new Mock<IMileagePlus>();
            _dataPowerFactory = new Mock<IDataPowerFactory>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _dynamoDBServices = new Mock<IDynamoDBService>();
            _legalDocumentsForTitlesService = new Mock<ILegalDocumentsForTitlesService>();
            _cachingService = new Mock<ICachingService>();
            _validateHashPinService = new Mock<IValidateHashPinService>();
            _shoppingBuyMiles = new Mock<IShoppingBuyMiles>();
            _optimizelyPersistService = new Mock<IOptimizelyPersistService>();
            _ffcShoppingcs = new Mock<IFFCShoppingcs>();
            _headers = new Mock<IHeaders>();
            _auroraMySqlService = new Mock<IAuroraMySqlService>();

            _shoppingUtility1 = new ShoppingUtility(_loggerUtility.Object, Configuration, _sessionHelperService.Object, _dPService.Object, _headers.Object, _dynamoDBServices.Object, _legalDocumentsForTitlesService.Object, _cachingService.Object, _validateHashPinService.Object, _optimizelyPersistService.Object,_shoppingBuyMiles.Object, _ffcShoppingcs.Object, _featureSettings.Object, _auroraMySqlService.Object);

            _shopAwardBusiness = new ShopAwardBusiness( _logger.Object, Configuration, _sessionHelperService.Object, _shoppingUtility.Object,
                _flightShoppingService.Object, _dPService.Object, _awardCalendarAzureService.Object, _mileagePlus.Object, _shoppingSessionHelper.Object);

            SetupHttpContextAccessor();
            SetHeaders();
            var cultureInfo = new CultureInfo("en-US");
            cultureInfo.NumberFormat.CurrencySymbol = "€";
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

        private void SetHeaders(string deviceId = "589d7852-14e7-44a9-b23b-a6db36657579"
       , string applicationId = "2"
       , string appVersion = "4.1.29"
       , string transactionId = "589d7852-14e7-44a9-b23b-a6db36657579|8f46e040-a200-495c-83ca-4fca2d7175fb"
       , string languageCode = "en-US"
       , string sessionId = "17C979E184CC495EA083D45F4DD9D19D")
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
        [MemberData(nameof(TestDataGenerator.RevenueLowestPriceForAwardSearch_Request), MemberType = typeof(TestDataGenerator))]
        public void RevenueLowestPriceForAwardSearch_Test(MOBSHOPShopRequest shopRequest, Session session, RevenueLowestPriceForAwardSearchResponse response, LatestShopAvailabilityResponse LatestShopAvailabilityResponse, Reservation reservation, United.Services.FlightShopping.Common.ShopResponse shopFareWheelInfoResponse)
        {
            //_sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(reservationDetail);
            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);
            _sessionHelperService.Setup(p => p.GetSession<LatestShopAvailabilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(LatestShopAvailabilityResponse);
            _flightShoppingService.Setup(p => p.ShopFareWheelInfo<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopFareWheelInfoResponse);
            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);
            var doc = TestDataGenerator.GetFileContent("doc.json");

            var result = _shopAwardBusiness.RevenueLowestPriceForAwardSearch(shopRequest);
            
            Assert.True(result.Result != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetSelectTripAwardCalendar_Request), MemberType = typeof(TestDataGenerator))]
        public void GetSelectTripAwardCalendar_Test(SelectTripRequest selectTripRequest, CSLShopRequest cSLShopRequest, ShoppingResponse shoppingResponse, Session session, ShopResponse shopResponse, DisplayAirportDetails displayAirportDetails)
        {
            //CSLShopRequest cSLShopRequest = null;
            //ShoppingResponse ShoppingResponse = null;
            //Session session = null;
            //SelectTripRequest shopRequest = null;
            //ShopResponse shopResponse = null;
            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);
            _awardCalendarAzureService.Setup(p => p.AwardDynamicCalendar<ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);
            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);
            _dynamoDBServices.Setup(p => p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);
            //_awardCalendarAzureService.Setup(p=>p.AwardDynamicCalendar<ShopResponse>)
            _shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);
            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);
            
            var doc = TestDataGenerator.GetFileContent("doc.json");

            var result = _shopAwardBusiness.GetSelectTripAwardCalendar(selectTripRequest);

            Assert.True(result.Result != null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetSelectTripAwardCalendar_Request1), MemberType = typeof(TestDataGenerator))]
        public void GetSelectTripAwardCalendar_Test1(SelectTripRequest selectTripRequest, CSLShopRequest cSLShopRequest, ShoppingResponse ShoppingResponse, Session session,  DisplayAirportDetails displayAirportDetails, ShopResponse shopResponse)
        {
            

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(ShoppingResponse);
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);
            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);
            _dynamoDBServices.Setup(p => p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);
            _shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);
            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);
            _awardCalendarAzureService.Setup(p => p.AwardDynamicCalendar<ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            var doc = TestDataGenerator.GetFileContent("doc.json");

            var result = _shopAwardBusiness.GetSelectTripAwardCalendar(selectTripRequest);

            Assert.True(result.Result != null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetSelectTripAwardCalendar_flow), MemberType = typeof(TestDataGenerator))]
        public void GetSelectTripAwardCalendar_flow(SelectTripRequest selectTripRequest, CSLShopRequest cSLShopRequest, ShoppingResponse ShoppingResponse, Session session, DisplayAirportDetails displayAirportDetails, ShopResponse shopResponse)
        {

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(ShoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _dynamoDBServices.Setup(p => p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);

            _shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);
            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _awardCalendarAzureService.Setup(p => p.AwardDynamicCalendar<ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            var doc = TestDataGenerator.GetFileContent("doc.json");

            var result = _shopAwardBusiness.GetSelectTripAwardCalendar(selectTripRequest);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
            // Assert.True(result.Result != null);
        }


        [Theory]
        [MemberData(nameof(TestDataGenerator.GetSelectTripAwardCalendar_Exception), MemberType = typeof(TestDataGenerator))]
        public void GetSelectTripAwardCalendar_Exception(SelectTripRequest selectTripRequest, CSLShopRequest cSLShopRequest, ShoppingResponse ShoppingResponse, Session session, DisplayAirportDetails displayAirportDetails, ShopResponse shopResponse)
        {

             var _dataPowerFactory = new DataPowerFactory(Configuration2, _sessionHelperService.Object, _logger9.Object);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(ShoppingResponse);
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);
            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);
            _dynamoDBServices.Setup(p => p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);
            _shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);
            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);
            //_awardCalendarAzureService.Setup(p => p.AwardDynamicCalendar<ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            var doc = TestDataGenerator.GetFileContent("doc.json");

            var shopAwardBusiness = new ShopAwardBusiness(_logger.Object, Configuration2, _sessionHelperService.Object, _shoppingUtility.Object,
                _flightShoppingService.Object, _dPService.Object, _awardCalendarAzureService.Object, _mileagePlus.Object, _shoppingSessionHelper.Object);

            var result = shopAwardBusiness.GetSelectTripAwardCalendar(selectTripRequest);

            Assert.True(result.Result != null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetShopAwardCalendar_Request), MemberType = typeof(TestDataGenerator))]
        public void GetShopAwardCalendar_Request(MOBSHOPShopRequest shopRequest, Session session, LatestShopAvailabilityResponse LatestShopAvailabilityResponse, Reservation reservation, ShopResponse shopResponse, ShoppingResponse ShoppingResponse, MPAccountSummary mPAccountSummary, CSLShopRequest cSLShopRequest, ReservationDetail reservationDetail)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<LatestShopAvailabilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(LatestShopAvailabilityResponse);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _awardCalendarAzureService.Setup(p => p.AwardDynamicCalendar<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(ShoppingResponse);

            _mileagePlus.Setup(p => p.GetAccountSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync(mPAccountSummary);

            var response = new DisplayAirportDetails();

            _dynamoDBServices.Setup(p => p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            _shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _shoppingUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            var doc = TestDataGenerator.GetFileContent("doc.json");


            var result = _shopAwardBusiness.GetShopAwardCalendar(shopRequest);

            Assert.True(result.Result != null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetShopAwardCalendar_Request1), MemberType = typeof(TestDataGenerator))]
        public void GetShopAwardCalendar1_Request1(MOBSHOPShopRequest shopRequest, Session session, LatestShopAvailabilityResponse LatestShopAvailabilityResponse, Reservation reservation, ShopResponse shopResponse, ShoppingResponse ShoppingResponse, MPAccountSummary mPAccountSummary, CSLShopRequest cSLShopRequest, ReservationDetail reservationDetail)
        {

            var shoppingUtility = new ShoppingUtility(_loggerUtility.Object, Configuration2, _sessionHelperService.Object, _dPService.Object, _headers.Object, _dynamoDBServices.Object, _legalDocumentsForTitlesService.Object, _cachingService.Object, _validateHashPinService.Object, _optimizelyPersistService.Object,_shoppingBuyMiles.Object, _ffcShoppingcs.Object, _featureSettings.Object, _auroraMySqlService.Object);

            var shopAwardBusiness = new ShopAwardBusiness( _logger.Object, Configuration2, _sessionHelperService.Object, shoppingUtility, _flightShoppingService.Object, _dPService.Object, _awardCalendarAzureService.Object,  _mileagePlus.Object, _shoppingSessionHelper.Object);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<LatestShopAvailabilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(LatestShopAvailabilityResponse);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _awardCalendarAzureService.Setup(p => p.AwardDynamicCalendar<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(ShoppingResponse);

            _mileagePlus.Setup(p => p.GetAccountSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync(mPAccountSummary);

            var response = new DisplayAirportDetails();
            _dynamoDBServices.Setup(p => p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            _dynamoDBServices.Setup(p => p.GetRecords<int>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(2);

            _shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _shoppingUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            var doc = TestDataGenerator.GetFileContent("doc.json");

            var result = shopAwardBusiness.GetShopAwardCalendar(shopRequest);

            Assert.True(result.Result != null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetShopAwardCalendar_flow), MemberType = typeof(TestDataGenerator))]
        public void GetShopAwardCalendar_flow(MOBSHOPShopRequest shopRequest, Session session, LatestShopAvailabilityResponse LatestShopAvailabilityResponse, Reservation reservation, ShopResponse shopResponse, ShoppingResponse ShoppingResponse, MPAccountSummary mPAccountSummary, CSLShopRequest cSLShopRequest, ReservationDetail reservationDetail)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<LatestShopAvailabilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(LatestShopAvailabilityResponse);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _awardCalendarAzureService.Setup(p => p.AwardDynamicCalendar<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(ShoppingResponse);

            _mileagePlus.Setup(p => p.GetAccountSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync(mPAccountSummary);

            var response = new DisplayAirportDetails();

            _dynamoDBServices.Setup(p => p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            _shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _shoppingUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            var doc = TestDataGenerator.GetFileContent("doc.json");


            var result = _shopAwardBusiness.GetShopAwardCalendar(shopRequest);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.CartId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
            // Assert.True(result.Result != null);


        }
    }
}