using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FlightStatus;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Shopping;
using United.Common.HelperSeatEngine;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightStatus;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.SeatEngine;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.DataAccess.ShopSeats;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Services.ShopSeats.Domain;
using United.Utility.Helper;
using United.Utility.Http;
using Xunit;
using ProfileResponse = United.Mobile.Model.Shopping.ProfileResponse;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Services.ShopSeats.Test
{
    public class ShopSeatsBusinessTest
    {
        private readonly Mock<ICacheLog<ShopSeatsBusiness>> _logger;
        private readonly Mock<ICacheLog<FlightStatusHelper>> _logger1;
        private readonly Mock<ICacheLog<SeatEngine>> _logger2;
        private readonly Mock<ICacheLog<SeatMapCSL30>> _logger3;
        private readonly Mock<ICacheLog<ShoppingUtility>> _loggerUtility;
        private IConfiguration _configuration;
       // private readonly Mock<IResilientClient> _resilientClient;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly ISeatEngine _seatEngine1;
        private readonly Mock<ISeatEngine> _seatEngine;
        private readonly ISeatMapCSL30 _seatMapCSL30_1;
        private readonly Mock<ISeatMapCSL30> _seatMapCSL30;
        private readonly Mock<IMerchandizingServices> _merchandizingServices;
        private readonly Mock<IDPService> _dPService;
        private readonly Mock<IDynamoDBService> _dynamoDBService;
        private readonly Mock<IFLIFOTokenService> _flifoTokenService;
        private readonly Mock<IFlightStatusService> _flightStatusService;
        private readonly Mock<ISeatEngineService> _seatEngineService;
        private readonly Mock<ISeatMapService> _seatMapService;
        private readonly Mock<ISeatMapCSL30Service> _seatMapCSL30Service;
        private readonly IShopSeatsBusiness _shopSeatsBusiness;
        private readonly IFlightStatusHelper _flightStatusHelper;
        private readonly IPersistToken _persistToken;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<IFlightRequestSqlSpOnPremService> _onPremFlightStatusService;
        private readonly Mock<ILegalDocumentsForTitlesService> _legalDocumentsForTitlesService;
        private readonly Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly Mock<ICachingService> _cachingService;
        private readonly ICachingService _cachingService1;
        private readonly Mock<ICacheLog<CachingService>> _logger6;
        private readonly IResilientClient _resilientClient;
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy _policyWrap;
        private readonly string _baseUrl;
        private readonly Mock<IValidateHashPinService> _validateHashPinService;
        private readonly Mock<IShoppingBuyMiles> _shoppingBuyMiles;
        private readonly Mock<IOptimizelyPersistService> _optimizelyPersistService;
        private readonly Mock<IFFCShoppingcs> _ffcShoppingcs;
        private readonly Mock<ICSLStatisticsService> _cslStatisticsService;
        private readonly Mock<ISeatEnginePostService> _seatEnginePostService;
        private readonly Mock<IPNRRetrievalService> _pNRRetrievalService;
        private readonly Mock<IProductInfoHelper>  _productInfoHelper;
        private readonly Mock<IComplimentaryUpgradeService> _complimentaryUpgradeService;
        private readonly Mock<IShoppingUtility> _shoppingUtility;
        private readonly IShoppingUtility _shoppingUtility1;
        private readonly Mock<IFeatureSettings> _featureSettings;
        private readonly Mock<IAuroraMySqlService> _auroraMySqlService;
        private readonly Mock<IFeatureToggles> _featureToggles;


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

        public ShopSeatsBusinessTest()
        {
            _logger = new Mock<ICacheLog<ShopSeatsBusiness>>();
            _logger1 = new Mock<ICacheLog<FlightStatusHelper>>();
            _logger2 = new Mock<ICacheLog<SeatEngine>>();
            _logger3 = new Mock<ICacheLog<SeatMapCSL30>>();
            _logger6 = new Mock<ICacheLog<CachingService>>();
            _loggerUtility = new Mock<ICacheLog<ShoppingUtility>>();
           // _resilientClient = new Mock<IResilientClient>();
            _dPService = new Mock<IDPService>();
            _dynamoDBService = new Mock<IDynamoDBService>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _dynamoDBService = new Mock<IDynamoDBService>();
            _merchandizingServices = new Mock<IMerchandizingServices>();
            _flifoTokenService = new Mock<IFLIFOTokenService>();
            _flightStatusService = new Mock<IFlightStatusService>();
            _seatMapService = new Mock<ISeatMapService>();
            _seatMapCSL30Service = new Mock<ISeatMapCSL30Service>();
            _onPremFlightStatusService = new Mock<IFlightRequestSqlSpOnPremService>();
            _legalDocumentsForTitlesService = new Mock<ILegalDocumentsForTitlesService>();
            _cachingService = new Mock<ICachingService>();
            _optimizelyPersistService = new Mock<IOptimizelyPersistService>();
            _shoppingBuyMiles = new Mock<IShoppingBuyMiles>();
            _ffcShoppingcs = new Mock<IFFCShoppingcs>();
            _cslStatisticsService = new Mock<ICSLStatisticsService>();
            _validateHashPinService = new Mock<IValidateHashPinService>();
            _seatEnginePostService = new Mock<ISeatEnginePostService>();
            _pNRRetrievalService = new Mock<IPNRRetrievalService>();
            _productInfoHelper = new Mock<IProductInfoHelper>();
            _complimentaryUpgradeService = new Mock<IComplimentaryUpgradeService>();
            _seatEngineService = new Mock<ISeatEngineService>();
            _shoppingUtility = new Mock<IShoppingUtility>();
            _seatMapCSL30 = new Mock<ISeatMapCSL30>();
            _seatEngine = new Mock<ISeatEngine>();
            _headers = new Mock<IHeaders>();
            _auroraMySqlService = new Mock<IAuroraMySqlService>();
            _featureSettings = new Mock<IFeatureSettings>();
            _featureToggles = new Mock<IFeatureToggles>();


            _shoppingUtility1 = new ShoppingUtility(_loggerUtility.Object, Configuration, _sessionHelperService.Object, _dPService.Object,  _headers.Object, _dynamoDBService.Object, _legalDocumentsForTitlesService.Object, _cachingService.Object, _validateHashPinService.Object, _optimizelyPersistService.Object,_shoppingBuyMiles.Object, _ffcShoppingcs.Object, _featureSettings.Object, _auroraMySqlService.Object);
            _flightStatusHelper = new FlightStatusHelper(_logger1.Object, Configuration, _dPService.Object, _flightStatusService.Object, _onPremFlightStatusService.Object, _dynamoDBService.Object, _headers.Object);

            _seatMapCSL30_1 = new SeatMapCSL30(_logger3.Object, Configuration, _sessionHelperService.Object, _seatMapCSL30Service.Object, _dynamoDBService.Object, _seatEngine.Object, _dPService.Object, _legalDocumentsForTitlesService.Object, _shoppingUtility1,_cachingService.Object,_headers.Object, _featureSettings.Object);

            _seatEngine1 = new SeatEngine(_logger2.Object, Configuration, _sessionHelperService.Object, _shoppingSessionHelper.Object, _shoppingUtility1, _seatMapService.Object, _dynamoDBService.Object, _merchandizingServices.Object, _seatEngineService.Object, _seatEnginePostService.Object, _dPService.Object, _pNRRetrievalService.Object, _productInfoHelper.Object, _complimentaryUpgradeService.Object, _legalDocumentsForTitlesService.Object,_cachingService.Object,_headers.Object);


            _shopSeatsBusiness = new ShopSeatsBusiness(_logger.Object, Configuration,  _headers.Object, _sessionHelperService.Object, _shoppingUtility1, _seatEngine.Object, _seatMapCSL30.Object, _merchandizingServices.Object, _dynamoDBService.Object, _cslStatisticsService.Object, _featureToggles.Object);

            _resilientClient = new ResilientClient(_baseUrl);
            _cachingService1 = new CachingService(_resilientClient, _logger6.Object, _configuration);

            SetupHttpContextAccessor();
            SetHeaders();
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

        private void SetHeaders(string deviceId = "D873298F-F27D-4AEC-BE6C-DE79C4259626"
             , string applicationId = "2"
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
        [MemberData(nameof(TestDataGenerator.SelectSeatsRequest), MemberType = typeof(TestDataGenerator))]
        public void SelectSeats_test(SelectSeatsRequest selectSeatsRequest, MOBSHOPReservation reservation, SelectSeats persistSelectSeatsResponse, MOBShoppingCart persistShoppingCart, Reservation persistedReservation, Session session , MOBUASubscriptions objUASubscriptions, SelectSeatsResponse selectSeatsResponse)
        {

            _sessionHelperService.Setup(p => p.GetSession<MOBSHOPReservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<SelectSeats>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistSelectSeatsResponse);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistShoppingCart);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistedReservation);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

              _merchandizingServices.Setup(p => p.GetEPlusSubscriptions(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(objUASubscriptions);

           // _shoppingUtility.Setup(p => p.EnableAdvanceSearchCouponBooking(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _seatMapCSL30.Setup(p => p.GetCSL30SeatMapDetail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Section>(), It.IsAny<MOBPromoCodeDetails>(), It.IsAny<bool>(), It.IsAny<int>())).Returns(Task.FromResult((selectSeatsResponse.SeatMap, false)));

            _seatEngine.Setup(p => p.GetSeatMapDetail(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FlightAvailabilitySegment>(), It.IsAny<List<BookingTravelerInfo>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(selectSeatsResponse.SeatMap);

            var result = _shopSeatsBusiness.SelectSeats(selectSeatsRequest);

            if (result.Exception == null)
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);

            //Assert.NotNull(response);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.SelectSeatsRequest1), MemberType = typeof(TestDataGenerator))]
        public void SelectSeats_test1(SelectSeatsRequest selectSeatsRequest, MOBSHOPReservation reservation, SelectSeats persistSelectSeatsResponse, MOBShoppingCart persistShoppingCart, Reservation persistedReservation, Session session, MOBUASubscriptions objUASubscriptions, SelectSeatsResponse selectSeatsResponse)
        {

            _sessionHelperService.Setup(p => p.GetSession<MOBSHOPReservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<SelectSeats>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistSelectSeatsResponse);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistShoppingCart);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistedReservation);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _merchandizingServices.Setup(p => p.GetEPlusSubscriptions(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(objUASubscriptions);

            // _shoppingUtility.Setup(p => p.EnableAdvanceSearchCouponBooking(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _seatMapCSL30.Setup(p => p.GetCSL30SeatMapDetail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Section>(), It.IsAny<MOBPromoCodeDetails>(), It.IsAny<bool>(), It.IsAny<int>())).Returns(Task.FromResult((selectSeatsResponse.SeatMap, false)));

            _seatEngine.Setup(p => p.GetSeatMapDetail(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FlightAvailabilitySegment>(), It.IsAny<List<BookingTravelerInfo>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(selectSeatsResponse.SeatMap);




            var result = _shopSeatsBusiness.SelectSeats(selectSeatsRequest);

            if (result.Exception == null)
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);

            //Assert.NotNull(response);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.SelectSeatsRequest2), MemberType = typeof(TestDataGenerator))]
        public void SelectSeats_test2(SelectSeatsRequest selectSeatsRequest, MOBSHOPReservation reservation, SelectSeats persistSelectSeatsResponse, MOBShoppingCart persistShoppingCart, Reservation persistedReservation, Session session, MOBUASubscriptions objUASubscriptions, SelectSeatsResponse selectSeatsResponse)
        {

            _sessionHelperService.Setup(p => p.GetSession<MOBSHOPReservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<SelectSeats>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistSelectSeatsResponse);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistShoppingCart);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistedReservation);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _merchandizingServices.Setup(p => p.GetEPlusSubscriptions(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(objUASubscriptions);

            // _shoppingUtility.Setup(p => p.EnableAdvanceSearchCouponBooking(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _seatMapCSL30.Setup(p => p.GetCSL30SeatMapDetail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Section>(), It.IsAny<MOBPromoCodeDetails>(), It.IsAny<bool>(), It.IsAny<int>())).Returns(Task.FromResult((selectSeatsResponse.SeatMap, false)));

            _seatEngine.Setup(p => p.GetSeatMapDetail(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FlightAvailabilitySegment>(), It.IsAny<List<BookingTravelerInfo>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(selectSeatsResponse.SeatMap);




            var result = _shopSeatsBusiness.SelectSeats(selectSeatsRequest);

            if (result.Exception == null)
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);

            //Assert.NotNull(response);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.SelectSeats_flow), MemberType = typeof(TestDataGenerator))]
        public void SelectSeats_flow(SelectSeatsRequest selectSeatsRequest, MOBSHOPReservation reservation, SelectSeats persistSelectSeatsResponse, MOBShoppingCart persistShoppingCart, Reservation persistedReservation, Session session, MOBUASubscriptions objUASubscriptions, SelectSeatsResponse selectSeatsResponse)
        {

            _sessionHelperService.Setup(p => p.GetSession<MOBSHOPReservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<SelectSeats>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistSelectSeatsResponse);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistShoppingCart);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistedReservation);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _merchandizingServices.Setup(p => p.GetEPlusSubscriptions(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(objUASubscriptions);

            // _shoppingUtility.Setup(p => p.EnableAdvanceSearchCouponBooking(It.IsAny<MOBSHOPShopRequest>())).Returns(true);

            _seatMapCSL30.Setup(p => p.GetCSL30SeatMapDetail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Section>(), It.IsAny<MOBPromoCodeDetails>(), It.IsAny<bool>(), It.IsAny<int>())).Returns(Task.FromResult((selectSeatsResponse.SeatMap, false)));

            _seatEngine.Setup(p => p.GetSeatMapDetail(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<FlightAvailabilitySegment>(), It.IsAny<List<BookingTravelerInfo>>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(selectSeatsResponse.SeatMap);

            var result = _shopSeatsBusiness.SelectSeats(selectSeatsRequest);

            if (result.Exception == null)
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);

        }



    }
}
