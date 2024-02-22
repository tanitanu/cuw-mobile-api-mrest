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
using United.Mobile.DataAccess.Shopping;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Services.ShopFareWheel.Domain;
using United.Service.Presentation.ReservationResponseModel;
using Xunit;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using United.Utility.Helper;
using United.Utility.HttpService;

namespace United.Mobile.Shopping.ShopFareWheel.Test
{
    public class ShopFareWheelBusinessTest
    {
        private readonly Mock<ICacheLog<ShopFareWheelBusiness>> _logger;
        private readonly Mock<ICacheLog<ShoppingUtility>> _loggerUtility;
        private IConfiguration _configuration;
        private readonly Mock<IHeaders> _headers;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly Mock<IDPService> _dPService;
        private readonly Mock<IFlightShoppingService> _flightShoppingService;
        private readonly Mock<IMileagePlus> _mileagePlus;
        private readonly Mock<IDynamoDBService> _dynamoDBServices;
        private readonly IShopFareWheelBusiness _shopFareWheelBusiness;
        private readonly Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly Mock<ILegalDocumentsForTitlesService> _legalDocumentsForTitlesService;
        private readonly Mock<ICachingService> _cachingService;
        private readonly Mock<IValidateHashPinService> _validateHashPinService;
        private readonly Mock<IShoppingBuyMiles> _shoppingBuyMiles;
        private readonly Mock<IOptimizelyPersistService> _optimizelyPersistService;
        private readonly Mock<IShoppingBuyMiles> _shoppingbuymiles;
        private readonly Mock<IFFCShoppingcs> _ffcShoppingcs;
        private readonly Mock<IShoppingClientService> _shoppingClientService;
        private readonly Mock<IFeatureSettings> _featureSettings;
        private readonly Mock<IAuroraMySqlService> _auroraMySqlService;
        private readonly Mock<IMileagePricingService> _mileagePricingService;
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

        public ShopFareWheelBusinessTest()
        {
            _logger = new Mock<ICacheLog<ShopFareWheelBusiness>>();
            _loggerUtility = new Mock<ICacheLog<ShoppingUtility>>();
            _dynamoDBServices = new Mock<IDynamoDBService>();
            _flightShoppingService = new Mock<IFlightShoppingService>();
            _mileagePlus = new Mock<IMileagePlus>();
            _dPService = new Mock<IDPService>();
            _headers = new Mock<IHeaders>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _legalDocumentsForTitlesService = new Mock<ILegalDocumentsForTitlesService>();
            _cachingService = new Mock<ICachingService>();
            _validateHashPinService = new Mock<IValidateHashPinService>();
            _optimizelyPersistService = new Mock<IOptimizelyPersistService>();
            _ffcShoppingcs = new Mock<IFFCShoppingcs>();
            _shoppingbuymiles = new Mock<IShoppingBuyMiles>();
            _auroraMySqlService = new Mock<IAuroraMySqlService>();
            _mileagePricingService = new Mock<IMileagePricingService>();
            _shoppingUtility = new ShoppingUtility(_loggerUtility.Object, Configuration, _sessionHelperService.Object, _dPService.Object, _headers.Object, _dynamoDBServices.Object, _legalDocumentsForTitlesService.Object, _cachingService.Object, _validateHashPinService.Object, _optimizelyPersistService.Object, _shoppingbuymiles.Object, _ffcShoppingcs.Object, _featureSettings.Object, _auroraMySqlService.Object);
            _shopFareWheelBusiness = new ShopFareWheelBusiness(_logger.Object, Configuration, _shoppingUtility, _sessionHelperService.Object, _dPService.Object, 
                _flightShoppingService.Object, _mileagePlus.Object, _shoppingSessionHelper.Object, _dynamoDBServices.Object, _shoppingClientService.Object, _ffcShoppingcs.Object, _mileagePricingService.Object, _featureSettings.Object);
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
        [MemberData(nameof(TestDataGenerator.GetFareWheelListResponse_Request), MemberType = typeof(TestDataGenerator))]
        public void GetFareWheelListResponse_Test(SelectTripRequest selectTripRequest, Session session, Model.Shopping.ShoppingResponse shoppingResponse, LatestShopAvailabilityResponse latestShopAvailabilityResponse, MOBSHOPFlattenedFlightList mOBSHOPFlattenedFlightList, AirportDetailsList airportDetailsList, CSLShopRequest cSLShopRequest, United.Services.FlightShopping.Common.ShopResponse shopResponse, Reservation reservation)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<LatestShopAvailabilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(latestShopAvailabilityResponse);

            _sessionHelperService.Setup(p => p.GetSession<MOBSHOPFlattenedFlightList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBSHOPFlattenedFlightList);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _flightShoppingService.Setup(p => p.SelectFareWheel<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            //_sessionHelperService.Setup(p => p.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(reservationDetail);

            //  _mileagePlus.Setup(p => p.GetAccountSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(summ);
           
            _sessionHelperService.Setup(p => p.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>()));
           
            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            var result = _shopFareWheelBusiness.GetFareWheelListResponse(selectTripRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetFareWheelListResponse_Request1), MemberType = typeof(TestDataGenerator))]
        public void GetFareWheelListResponse_Test1(SelectTripRequest selectTripRequest, Session session, Model.Shopping.ShoppingResponse shoppingResponse, LatestShopAvailabilityResponse latestShopAvailabilityResponse, MOBSHOPFlattenedFlightList mOBSHOPFlattenedFlightList, AirportDetailsList airportDetailsList, CSLShopRequest cSLShopRequest, United.Services.FlightShopping.Common.ShopResponse shopResponse, Reservation reservation)
        {

            //_shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            //_sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<LatestShopAvailabilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(latestShopAvailabilityResponse);

            _sessionHelperService.Setup(p => p.GetSession<MOBSHOPFlattenedFlightList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBSHOPFlattenedFlightList);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _flightShoppingService.Setup(p => p.SelectFareWheel<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            _sessionHelperService.Setup(p => p.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>()));

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);


            //_sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            //_flightShoppingService.Setup(p => p.SelectFareWheel(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnAsync(shopResponse); 

            var result = _shopFareWheelBusiness.GetFareWheelListResponse(selectTripRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetFareWheelListResponse_flow), MemberType = typeof(TestDataGenerator))]
        public void GetFareWheelListResponse_flow(SelectTripRequest selectTripRequest, Session session, Model.Shopping.ShoppingResponse shoppingResponse, LatestShopAvailabilityResponse latestShopAvailabilityResponse, MOBSHOPFlattenedFlightList mOBSHOPFlattenedFlightList, AirportDetailsList airportDetailsList, CSLShopRequest cSLShopRequest, United.Services.FlightShopping.Common.ShopResponse shopResponse, Reservation reservation)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<LatestShopAvailabilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(latestShopAvailabilityResponse);

            _sessionHelperService.Setup(p => p.GetSession<MOBSHOPFlattenedFlightList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBSHOPFlattenedFlightList);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _flightShoppingService.Setup(p => p.SelectFareWheel<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            //_sessionHelperService.Setup(p => p.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(reservationDetail);

            //  _mileagePlus.Setup(p => p.GetAccountSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(summ);

            _sessionHelperService.Setup(p => p.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>()));

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            var result = _shopFareWheelBusiness.GetFareWheelListResponse(selectTripRequest);

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
        [MemberData(nameof(TestDataGenerator.GetShopFareWheelListResponse_Request), MemberType = typeof(TestDataGenerator))]
        public void GetShopFareWheelListResponse_Test(MOBSHOPShopRequest shopRequest, Session session, Model.Shopping.ShoppingResponse shoppingResponse, CSLShopRequest cSLShopRequest, LatestShopAvailabilityResponse latestShopAvailabilityResponse, AirportDetailsList airportDetailsList, Reservation reservation, United.Services.FlightShopping.Common.ShopResponse shopResponse, MPAccountSummary mPAccountSummary, ReservationDetail reservationDetail)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            // _flightShoppingService.Setup(p => p.SelectFareWheel<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _sessionHelperService.Setup(p => p.GetSession<LatestShopAvailabilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(latestShopAvailabilityResponse);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            //  _sessionHelperService.Setup(p => p.GetSession<MOBSHOPFlattenedFlightList>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(mOBSHOPFlattenedFlightList);

            //_sessionHelperService.Setup(p => p.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(reservationDetail);

            //_mileagePlus.Setup(p => p.GetAccountSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(summary);

            // _sessionHelperService.Setup(p => p.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(reservationDetail);

            // _sessionHelperService.Setup(p => p.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _mileagePlus.Setup(p => p.GetAccountSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync(mPAccountSummary);

            _flightShoppingService.Setup(p => p.SelectFareWheel<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            _sessionHelperService.Setup(p => p.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            //Act
            var result = _shopFareWheelBusiness.GetShopFareWheelListResponse(shopRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetShopFareWheelListResponse_flow), MemberType = typeof(TestDataGenerator))]
        public void GetShopFareWheelListResponse_flow(MOBSHOPShopRequest shopRequest, Session session, Model.Shopping.ShoppingResponse shoppingResponse, CSLShopRequest cSLShopRequest, LatestShopAvailabilityResponse latestShopAvailabilityResponse, AirportDetailsList airportDetailsList, Reservation reservation, United.Services.FlightShopping.Common.ShopResponse shopResponse, MPAccountSummary mPAccountSummary, ReservationDetail reservationDetail)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            // _flightShoppingService.Setup(p => p.SelectFareWheel<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _sessionHelperService.Setup(p => p.GetSession<LatestShopAvailabilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(latestShopAvailabilityResponse);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            //  _sessionHelperService.Setup(p => p.GetSession<MOBSHOPFlattenedFlightList>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(mOBSHOPFlattenedFlightList);

            //_sessionHelperService.Setup(p => p.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(reservationDetail);

            //_mileagePlus.Setup(p => p.GetAccountSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(summary);

            // _sessionHelperService.Setup(p => p.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(reservationDetail);

            // _sessionHelperService.Setup(p => p.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _mileagePlus.Setup(p => p.GetAccountSummary(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync(mPAccountSummary);

            _flightShoppingService.Setup(p => p.SelectFareWheel<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(shopResponse);

            _sessionHelperService.Setup(p => p.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            //Act
            var result = _shopFareWheelBusiness.GetShopFareWheelListResponse(shopRequest);
            // Assert
            Assert.True(result.Result != null);
        }



    }
}
