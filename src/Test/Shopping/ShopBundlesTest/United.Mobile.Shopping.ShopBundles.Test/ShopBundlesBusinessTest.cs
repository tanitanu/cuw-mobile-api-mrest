using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Common.HelperSeatEngine;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.ShopBundles;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Bundles;
using United.Mobile.Services.ShopBundles.Domain;
using United.Utility.Http;
using Xunit;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using United.Utility.Helper;


namespace United.Mobile.Shopping.ShopBundles.Test
{
    public class ShopBundlesBusinessTest
    {
        private readonly Mock<ICacheLog<ShopBundlesBusiness>> _logger;
        private readonly Mock<ICacheLog<FlightShoppingService>> _logger1;
        private readonly Mock<ICacheLog<ShoppingUtility>> _loggerUtility;
        private IConfiguration _configuration;
        private readonly Mock<IResilientClient> _resilientClient;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly Mock<IFlightShoppingService> _flightShoppingService;
        private readonly Mock<IUnfinishedBookingService> _unfinishedBookingService;
        private readonly Mock<IDPService> _dPService;
        private readonly Mock<IDynamoDBService> _dynamoDBServices;
        private readonly IShopBundlesBusiness _shopBundlesBusiness;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly Mock<IOmniCart> _omnicart;
        private readonly Mock<ISeatMapCSL30> _seatMapCSL30;
        private readonly Mock<ITravelerUtility> _travelerUtility;
        private readonly Mock<IBundleOfferService> _bundleOfferService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly Mock<ICachingService> _cachingService;
        private readonly Mock<IValidateHashPinService> _validateHashPinService;
        private readonly Mock<IShoppingBuyMiles> _shoppingBuyMiles;
        private readonly Mock<IOptimizelyPersistService> _optimizelyPersistService;
        private readonly Mock<IFFCShoppingcs> _ffcShoppingcs;
        private readonly Mock<IFeatureSettings> _featureSettings;
        private readonly Mock<IAuroraMySqlService> _auroraMySqlService;
        private readonly Mock<IShopBundleService> _shopBundleService;
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
                // }
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
                .AddJsonFile("appsettings1.test.json", optional: false, reloadOnChange: true)
                .Build();
                //}
                return _configuration;
            }
        }
        public ShopBundlesBusinessTest()
        {
            _logger = new Mock<ICacheLog<ShopBundlesBusiness>>();
            _loggerUtility = new Mock<ICacheLog<ShoppingUtility>>();
            _resilientClient = new Mock<IResilientClient>();
            _dPService = new Mock<IDPService>();
            _unfinishedBookingService = new Mock<IUnfinishedBookingService>();
            _dynamoDBServices = new Mock<IDynamoDBService>();
            _flightShoppingService = new Mock<IFlightShoppingService>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _cachingService = new Mock<ICachingService>();
            _validateHashPinService = new Mock<IValidateHashPinService>();
            _optimizelyPersistService = new Mock<IOptimizelyPersistService>();
            _shoppingBuyMiles = new Mock<IShoppingBuyMiles>();
            _ffcShoppingcs = new Mock<IFFCShoppingcs>();
            _headers = new Mock<IHeaders>();
            _auroraMySqlService = new Mock<IAuroraMySqlService>();
            _shoppingUtility = new ShoppingUtility(_loggerUtility.Object, Configuration, _sessionHelperService.Object, _dPService.Object, _headers.Object, _dynamoDBServices.Object, _legalDocumentsForTitlesService, _cachingService.Object, _validateHashPinService.Object, _optimizelyPersistService.Object,_shoppingBuyMiles.Object, _ffcShoppingcs.Object,_featureSettings.Object, _auroraMySqlService.Object);
            _resilientClient = new Mock<IResilientClient>();
            _dynamoDBServices = new Mock<IDynamoDBService>();          
            _seatMapCSL30 = new Mock<ISeatMapCSL30>();
            _omnicart = new Mock<IOmniCart>();
           _travelerUtility = new Mock<ITravelerUtility>();
            _bundleOfferService = new Mock<IBundleOfferService>();
            _shopBundleService = new Mock<IShopBundleService>(Configuration, _sessionHelperService.Object, _bundleOfferService.Object, _shoppingUtility, _headers.Object, _dynamoDBServices.Object, _omnicart.Object, _featureSettings.Object);
            _shopBundlesBusiness = new ShopBundlesBusiness(_logger.Object, Configuration, _headers.Object, _sessionHelperService.Object, _shoppingUtility, _flightShoppingService.Object, _unfinishedBookingService.Object, _dPService.Object, _dynamoDBServices.Object, _omnicart.Object, _seatMapCSL30.Object, _travelerUtility.Object, _bundleOfferService.Object, _shopBundleService.Object,_featureSettings.Object);
            SetupHttpContextAccessor();
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
        [MemberData(nameof(TestDataGenerator.GetBundles_CFOP_Request), MemberType = typeof(TestDataGenerator))]
        public void GetBundles_CFOP( BookingBundlesRequest bookingBundlesRequest, Session session, Reservation persistedReservation,  United.Services.FlightShopping.Common.ShopRequest persistedShopPindownRequest, MOBShoppingCart persistShopping, BookingBundlesResponse response, United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse bundleResponse, United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse responsebundle)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistedReservation);

            _sessionHelperService.Setup(p => p.GetSession<United.Services.FlightShopping.Common.ShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistedShopPindownRequest);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistShopping);

            _sessionHelperService.Setup(p => p.GetSession<BookingBundlesResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(response);

            //_sessionHelperService.Setup(p => p.GetSession<United.Services.FlightShopping.Common.ShopSelectRequest>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(request);

            _unfinishedBookingService.Setup(p => p.GetShopPinDown<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<United.Services.FlightShopping.Common.ShopRequest>())).ReturnsAsync(bundleResponse);

            _flightShoppingService.Setup(p => p.GetBundles<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(responsebundle);

            var doc = TestDataGenerator.GetFileContent("doc.json");
            _dynamoDBServices.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));



            //Act
            var result = _shopBundlesBusiness.GetBundles_CFOP(bookingBundlesRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetBundles_CFOP_Request), MemberType = typeof(TestDataGenerator))]
        public void GetBundles_CFOP1(Session session, BookingBundlesRequest bookingBundlesRequest, Reservation persistedReservation, United.Services.FlightShopping.Common.ShopRequest persistedShopPindownRequest, MOBShoppingCart persistShopping, BookingBundlesResponse response, United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse bundleResponse, United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse responsebundle)
        {
            var shopBundlesBusiness = new ShopBundlesBusiness(_logger.Object, Configuration1, _headers.Object, _sessionHelperService.Object, _shoppingUtility, _flightShoppingService.Object, _unfinishedBookingService.Object, _dPService.Object, _dynamoDBServices.Object, _omnicart.Object, _seatMapCSL30.Object, _travelerUtility.Object, _bundleOfferService.Object, _shopBundleService.Object, _featureSettings.Object);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistedReservation);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<United.Services.FlightShopping.Common.ShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistedShopPindownRequest);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(persistShopping);

            _sessionHelperService.Setup(p => p.GetSession<BookingBundlesResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(response);

            //_sessionHelperService.Setup(p => p.GetSession<United.Services.FlightShopping.Common.ShopSelectRequest>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(request);

            _unfinishedBookingService.Setup(p => p.GetShopPinDown<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<United.Services.FlightShopping.Common.ShopRequest>())).ReturnsAsync(bundleResponse);

            _flightShoppingService.Setup(p => p.GetBundles<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(responsebundle);

            var doc = TestDataGenerator.GetFileContent("doc.json");
            _dynamoDBServices.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));



            //Act
            var result = shopBundlesBusiness.GetBundles_CFOP(bookingBundlesRequest);
            // Assert
            Assert.True(result.Result != null);
        }
    }
}