using MerchandizingServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Definition;
using United.Ebs.Logging.Enrichers;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.ReShop;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.DataAccess.TripPlanGetService;
using United.Mobile.EligibleCheck.Domain;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.AccountManagement;
using United.Mobile.Model.Internal.Common;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.ReShop;
using United.Mobile.Model.Shopping;
using United.Mobile.ReShop.Domain;
//using United.Mobile.ReShop.Domain.GetProducts_CFOP;
using United.Mobile.Services.Shopping.Domain;
using United.Mobile.UpdateProfile.Domain;
using United.Service.Presentation.ReservationResponseModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.LMX;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.HttpService;
using Xunit;
using Constants = United.Mobile.Model.Constants;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using Session = United.Mobile.Model.Common.Session;
using United.Common.Helper.Merchandize;
using IMerchandizingServices = United.Common.Helper.Merchandize.IMerchandizingServices;
using United.Mobile.ReshopSelectTrip.Domain;
using United.Mobile.DataAccess.DynamoDB;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.DataCollection;
using United.Mobile.Model.Internal;
using United.Mobile.ScheduleChange.Domain;
using United.Mobile.Model.CSLModels;
using Message = United.Services.FlightShopping.Common.Message;
using Newtonsoft.Json;
using United.Mobile.ReShop.Domain.GetProducts_CFOP;
using United.Mobile.Model.ManageRes;
using Microsoft.Extensions.Configuration;
//using United.Mobile.ReShop.Domain.GetProducts_CFOP;
//using United.Mobile.Model.MPSignIn;

namespace United.Mobile.Test.ReShop.Tests
{
    public class ReShoppingBusinessTest
    {
        private readonly Mock<ICacheLog<ReShoppingBusiness>> _logger;
        private readonly Mock<ICacheLog<ShoppingUtility>> _loggerUtility;
        private readonly Mock<ICacheLog<TravelerCSL>> _loggerTravelerCSL;
        private readonly IConfiguration _configuration;
        private Mock<ISessionHelperService> _sessionHelperService;
        private IShoppingUtility _shoppingUtility;
        private readonly Mock<ICacheLog<ShoppingUtility>> _logger6;
        private readonly Mock<IShoppingBusiness> _shopBusiness;
        private Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly ShoppingSessionHelper _shoppingSessionHelper1;
        private readonly Mock<ICacheLog<ShoppingSessionHelper>> _logger5;
        private readonly ReShoppingBusiness _reShoppingBusiness;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<ICacheLog<ShopBooking>> _logger1;
        private readonly Mock<IReferencedataService> _referencedataService;
        private readonly Mock<IFlightShoppingService> _flightShoppingService;
        private readonly Mock<IMileagePlus> _mileagePlus;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<IDynamoDBService> _dynamoDBService;
        private readonly ITravelerCSL _travelerCSL;
        private readonly IResilientClient _resilientClient;
        private readonly Mock<ICMSContentService> _iCMSContentService;
        private readonly Mock<IDPService> _dPService;
        private readonly IDataPowerFactory _dataPowerFactory;
        private readonly Mock<ICacheLog<DataPowerFactory>> _logger4;
        private readonly IShopBooking _shopBooking;
        private readonly Mock<ILegalDocumentsForTitlesService> _legalDocumentsForTitlesService;
        private readonly Mock<ICachingService> _cachingService;
        private readonly Mock<IValidateHashPinService> _validateHashPinService;
        private readonly Mock<IShoppingBuyMiles> _shoppingBuyMiles;
        private readonly Mock<IDPService> _dpservice1;
        private readonly Mock<IShoppingClientService> _shoppingClientService;
        private readonly Mock<IOptimizelyPersistService> _optimizelyPersistService;
        private readonly Mock<IFFCShoppingcs> _ffcShoppingcs;
        private readonly Mock<IApplicationEnricher> _requestEnricher;
        private readonly IEligibleCheckBusiness _eligibleCheckBusiness;
        private readonly Mock<ICacheLog<EligibleCheckBusiness>> _logger7;
        private readonly IUpdateProfileBusiness _updateProfileBusiness;
        private readonly Mock<ICacheLog<UpdateProfileBusiness>> _logger2;
        private readonly Mock<IShoppingCartService> _shoppingCartService;
        private readonly Mock<ITravelerUtility> _travelerUtility;
        private readonly Mock<IFormsOfPayment> _formsOfPayment;
        private readonly ICachingService _cachingService1;
        private readonly Mock<ICacheLog<CachingService>> _logger3;
        private readonly Mock<IPNRRetrievalService> _pNRRetrievalService;
        private readonly Mock<IRefundService> _refundService;
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy _policyWrap;
        private readonly string _baseUrl;
        private readonly Mock<IShoppingBusiness> _shoppingBusiness;
        private readonly IGetProducts_CFOPBusiness _getProducts_CFOPBusiness;
        private readonly Mock<ICacheLog<GetProducts_CFOPBusiness>> _logger8;
        private readonly Mock<IPurchaseMerchandizingService> _purchaseMerchandizindServices;
        private readonly Mock<IMerchandizingServices> _merchandizingServices;
        private readonly IReshopSelectTripBusiness _reshopSelectTripBusiness;
        private readonly Mock<ICacheLog<ReshopSelectTripBusiness>> _logger9;
        private readonly AirportDynamoDB _airportDynamoDB;
        private readonly Mock<ICacheLog<AirportDynamoDB>> _logger10;
        private readonly IScheduleChangeBusiness _scheduleChangeBusiness;
        private readonly Mock<ICacheLog<ScheduleChangeBusiness>> _logger11;
        private readonly Mock<IReservationService> _reservationService;
        private readonly Mock<IRecordLocatorBusiness> _recordLocatorBusiness;
        private readonly Mock<IShoppingUtility> _shoppingUtility1;
        private readonly Mock<IPurchaseMerchandizingService> _purchaseMerchandizingService;
        private readonly Mock<IOmniCart> _omniCart;
        private readonly Mock<IFeatureSettings> _featureSettings;
        private readonly Mock<IFeatureToggles> _featureToggles;
        private readonly Mock<IAuroraMySqlService> _auroraMySqlService;



        public ReShoppingBusinessTest()
        {
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _logger = new Mock<ICacheLog<ReShoppingBusiness>>();
            _logger1 = new Mock<ICacheLog<ShopBooking>>();
            _logger2 = new Mock<ICacheLog<UpdateProfileBusiness>>();
            _logger3 = new Mock<ICacheLog<CachingService>>();
            _logger4 = new Mock<ICacheLog<DataPowerFactory>>();
            _logger5 = new Mock<ICacheLog<ShoppingSessionHelper>>();
            _logger6 = new Mock<ICacheLog<ShoppingUtility>>();
            _logger7 = new Mock<ICacheLog<EligibleCheckBusiness>>();
            _logger8 = new Mock<ICacheLog<GetProducts_CFOPBusiness>>();
            _logger9 = new Mock<ICacheLog<ReshopSelectTripBusiness>>();
            _logger10 = new Mock<ICacheLog<AirportDynamoDB>>();
            _logger11 = new Mock<ICacheLog<ScheduleChangeBusiness>>();
            _loggerTravelerCSL = new Mock<ICacheLog<TravelerCSL>>();
            _loggerUtility = new Mock<ICacheLog<ShoppingUtility>>();
            _referencedataService = new Mock<IReferencedataService>();
            _flightShoppingService = new Mock<IFlightShoppingService>();
            _iCMSContentService = new Mock<ICMSContentService>();
            _mileagePlus = new Mock<IMileagePlus>();
            _headers = new Mock<IHeaders>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _dynamoDBService = new Mock<IDynamoDBService>();
            _dPService = new Mock<IDPService>();
            _shopBusiness = new Mock<IShoppingBusiness>();
            _cachingService = new Mock<ICachingService>();
            _shoppingClientService = new Mock<IShoppingClientService>();
            _validateHashPinService = new Mock<IValidateHashPinService>();
            _optimizelyPersistService = new Mock<IOptimizelyPersistService>();
            _ffcShoppingcs = new Mock<IFFCShoppingcs>();
            _requestEnricher = new Mock<IApplicationEnricher>();
            _shoppingBuyMiles = new Mock<IShoppingBuyMiles>();
            _dpservice1 = new Mock<IDPService>();
            _travelerUtility = new Mock<ITravelerUtility>();
            _formsOfPayment = new Mock<IFormsOfPayment>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _pNRRetrievalService = new Mock<IPNRRetrievalService>();
            _refundService = new Mock<IRefundService>();
            _shoppingBusiness = new Mock<IShoppingBusiness>();
            _purchaseMerchandizindServices = new Mock<IPurchaseMerchandizingService>();
            _merchandizingServices = new Mock<IMerchandizingServices>();
            _reservationService = new Mock<IReservationService>();
            _recordLocatorBusiness = new Mock<IRecordLocatorBusiness>();
            _shoppingUtility1 = new Mock<IShoppingUtility>();
            _purchaseMerchandizingService = new Mock<IPurchaseMerchandizingService>();
            _omniCart = new Mock<IOmniCart>();
            _auroraMySqlService = new Mock<IAuroraMySqlService>();
            _featureSettings = new Mock<IFeatureSettings>();
            _featureToggles = new Mock<IFeatureToggles>();

            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.test.json", optional: false, reloadOnChange: true)
                .Build();

            _scheduleChangeBusiness = new ScheduleChangeBusiness(_logger11.Object, _configuration,_sessionHelperService.Object,_reservationService.Object,_recordLocatorBusiness.Object);

            _reshopSelectTripBusiness = new ReshopSelectTripBusiness(_logger9.Object,_shoppingBusiness.Object);

            _getProducts_CFOPBusiness = new GetProducts_CFOPBusiness(_logger8.Object, _configuration,_sessionHelperService.Object,
                    _shoppingSessionHelper.Object,
                    _dPService.Object,_dynamoDBService.Object,_cachingService.Object,_headers.Object, _merchandizingServices.Object,
                    _purchaseMerchandizindServices.Object);
             _legalDocumentsForTitlesService = new Mock<ILegalDocumentsForTitlesService>();
            _travelerCSL = new TravelerCSL(_configuration, _iCMSContentService.Object, _cachingService.Object, _loggerTravelerCSL.Object);
            _shoppingSessionHelper1 = new ShoppingSessionHelper(_configuration, _sessionHelperService.Object, _dynamoDBService.Object, _dPService.Object, _httpContextAccessor.Object, _shoppingUtility, _dataPowerFactory, _requestEnricher.Object, _headers.Object, null);

            _shoppingUtility = new ShoppingUtility(_loggerUtility.Object, _configuration, _sessionHelperService.Object, _dPService.Object, _headers.Object, _dynamoDBService.Object, _legalDocumentsForTitlesService.Object, _cachingService.Object, _validateHashPinService.Object, _optimizelyPersistService.Object, _shoppingBuyMiles.Object, _ffcShoppingcs.Object,_featureSettings.Object,_auroraMySqlService.Object);

            _shopBooking = new ShopBooking(_logger1.Object, _configuration, _headers.Object, _dynamoDBService.Object, _sessionHelperService.Object, _shoppingUtility, _mileagePlus.Object, _referencedataService.Object, _flightShoppingService.Object, _travelerCSL, _cachingService.Object, _legalDocumentsForTitlesService.Object, _dpservice1.Object, _shoppingClientService.Object, _ffcShoppingcs.Object, _omniCart.Object, _featureSettings.Object, _featureToggles.Object);
            _reShoppingBusiness = new ReShoppingBusiness(_logger.Object, _configuration, _sessionHelperService.Object, _shopBusiness.Object, _shoppingUtility, _shoppingSessionHelper.Object);

            _resilientClient = new ResilientClient(_baseUrl);

            _cachingService1 = new CachingService(_resilientClient, _logger3.Object, _configuration);

            _updateProfileBusiness = new UpdateProfileBusiness(_logger2.Object, _configuration, _shoppingSessionHelper.Object, _sessionHelperService.Object, _shoppingUtility, _travelerUtility.Object, _formsOfPayment.Object, _cachingService.Object, _headers.Object);

            _eligibleCheckBusiness = new EligibleCheckBusiness(_logger7.Object, _configuration, _validateHashPinService.Object, _sessionHelperService.Object,
                _shoppingSessionHelper.Object, _dPService.Object, _pNRRetrievalService.Object, _dynamoDBService.Object, _refundService.Object, _shopBusiness.Object, _cachingService.Object,
                _legalDocumentsForTitlesService.Object, _headers.Object, _featureSettings.Object, _iCMSContentService.Object);

            _airportDynamoDB = new AirportDynamoDB(_configuration,_dynamoDBService.Object);





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

        public static string GetFileContent(string fileName)
        {
            fileName = string.Format("..\\..\\..\\TestData\\{0}", fileName);
            var path = Path.IsPathRooted(fileName) ? fileName : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }

        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ReShop_Test), MemberType = typeof(ReShoppingTestDataGenerator))]

        public void ReShop_Test(MOBSHOPShopRequest mOBSHOPShopRequest, Session session, Model.Shopping.ShoppingResponse shoppingResponse, LatestShopAvailabilityResponse latestShopAvailabilityResponse, MOBSHOPFlattenedFlightList mOBSHOPFlattenedFlightList, AirportDetailsList airportDetailsList, CSLShopRequest cSLShopRequest, United.Services.FlightShopping.Common.ShopResponse shopResponse, Model.Shopping.ShopResponse shopResponse1, Reservation reservation, ReservationDetail reservationDetail, MOBDisplayBagTrackAirportDetails displayAirportDetails, UpdateAmenitiesIndicatorsResponse updateAmenitiesIndicatorsResponse, LmxQuoteResponse lmxQuoteResponse)
        {
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<LatestShopAvailabilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(latestShopAvailabilityResponse);

            _sessionHelperService.Setup(p => p.GetSession<MOBSHOPFlattenedFlightList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBSHOPFlattenedFlightList);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _flightShoppingService.Setup(p => p.GetShop<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((shopResponse, 33001222221)));

            _flightShoppingService.Setup(p => p.ShopValidateSpecialPricing<United.Services.FlightShopping.Common.ShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((shopResponse, 33001222221)));

            _dynamoDBService.Setup(p => p.GetRecords<MOBDisplayBagTrackAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);

            _sessionHelperService.Setup(p => p.GetSession<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync("{Messages:[{\"Title\":\"Shopping.CorporateDisclaimerMessage.MOB\",\"ContentFull\":\"\",\"ContentShort\":\"\",\"CallToActionUrl1\":\"\",\"Headline\":\"\"}],\"Status\":1}");

            _flightShoppingService.Setup(p => p.UpdateAmenitiesIndicators<UpdateAmenitiesIndicatorsResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((updateAmenitiesIndicatorsResponse, 33001222221)));

            _flightShoppingService.Setup(p => p.GetLmxQuote<LmxQuoteResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((lmxQuoteResponse, 33001222221)));

            _shopBusiness.Setup(p => p.GetShop(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<HttpContext>())).ReturnsAsync(shopResponse1);
            //Act
            var result = _reShoppingBusiness.ReShop(mOBSHOPShopRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);


        }

        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ChangeEligibleCheck_Test), MemberType = typeof(ReShoppingTestDataGenerator))]

        public void ChangeEligibleCheck_Test(MOBRESHOPChangeEligibilityRequest mOBRESHOPChangeEligibilityRequest,
            EligibilityResponse eligibilityResponse, Session session, ReservationDetail reservationDetail, Reservation reservation, HashPinValidate hashPinValidate, AirportDetailsList airportDetailsList,  MOBPNR mOBPNR, List<MOBLegalDocument> mOBLegalDocuments, MOBDisplayBagTrackAirportDetails displayAirportDetails)
        {
            var _dataPowerFactory = new DataPowerFactory(_configuration, _sessionHelperService.Object,_logger4.Object);

            _sessionHelperService.Setup(p => p.GetSession<EligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(eligibilityResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            //_dynamoDBService.Setup(p =>p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()).Returns(displayAirportDetails);


            _dPService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Token");

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinValidate);

            //_dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(mOBLegalDocument);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(mOBLegalDocuments);

           // _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(legalDocuments);

            _refundService.Setup(p => p.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"application\":{\"id\":2,\"isProduction\":false,\"name\":\"Android\",\"version\":{\"major\":\"4.1.25\",\"minor\":\"4.1.25\"}},\"SessionId\":\"17C979E184CC495EA083D45F4DD9D19D\",\"HashPinCode\":\"7C7D6DE3BB0A56DD424303E20EA996E1AF7DE6BF\",\"DeviceId\":\"1E70A8CC-2A59-4450-AF89-0D4AB6C659B4\",\"MileagePlusNumber\":\"MR722052\",\"RecordLocator\":\"ABCD\",\"ReshopRequest\":{\"ObjectName\":\"United.Persist.Definition.Shopping.Session\",\"countryCode\":\"US\",\"CslContext\":{\"_session\":\"17C979E184CC495EA083D45F4DD9D19D\",\"_transactionId\":\"00c87c77-f976-482d-a9a7-c504cc08ab59|5f2bc92d-f584-492a-8371-6e68120e14f2\",\"_token\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9\"}}}");

            _sessionHelperService.Setup(p => p.GetSession<MOBPNR>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBPNR);


            _dynamoDBService.Setup(p => p.GetRecords<MOBDisplayBagTrackAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);



            var _eligibleCheckBusiness = new EligibleCheckBusiness(_logger7.Object, _configuration, _validateHashPinService.Object, _sessionHelperService.Object,_shoppingSessionHelper.Object,_dPService.Object,_pNRRetrievalService.Object,_dynamoDBService.Object,_refundService.Object,_shopBusiness.Object,_cachingService1,_legalDocumentsForTitlesService.Object,_headers.Object, _featureSettings.Object,_iCMSContentService.Object);

            if (mOBRESHOPChangeEligibilityRequest.SessionId == "17C979E184CC495EA083D45F4DD9D19D")
            {
                _configuration["ReshopEligiblityDontLoadReservationWhenExceptionInCSLResponse"] = "true";
            }


            //Act
            var result = _eligibleCheckBusiness.ChangeEligibleCheck(mOBRESHOPChangeEligibilityRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);
            

        }

        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ChangeEligibleCheck1_Test), MemberType = typeof(ReShoppingTestDataGenerator))]

        public void ChangeEligibleCheck1_Test(MOBRESHOPChangeEligibilityRequest mOBRESHOPChangeEligibilityRequest,
           EligibilityResponse eligibilityResponse, Session session, ReservationDetail reservationDetail, Reservation reservation, HashPinValidate hashPinValidate, AirportDetailsList airportDetailsList, MOBPNR mOBPNR, List<MOBLegalDocument> mOBLegalDocuments, MOBDisplayBagTrackAirportDetails displayAirportDetails)
        {
            var _dataPowerFactory = new DataPowerFactory(_configuration, _sessionHelperService.Object, _logger4.Object);

            _sessionHelperService.Setup(p => p.GetSession<EligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(eligibilityResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            //_dynamoDBService.Setup(p =>p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()).Returns(displayAirportDetails);


            _dPService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Token");

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinValidate);

            //_dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(mOBLegalDocument);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(mOBLegalDocuments);

            // _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(legalDocuments);

            _refundService.Setup(p => p.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"application\":{\"id\":2,\"isProduction\":false,\"name\":\"Android\",\"version\":{\"major\":\"4.1.25\",\"minor\":\"4.1.25\"}},\"SessionId\":\"17C979E184CC495EA083D45F4DD9D19D\",\"HashPinCode\":\"7C7D6DE3BB0A56DD424303E20EA996E1AF7DE6BF\",\"DeviceId\":\"1E70A8CC-2A59-4450-AF89-0D4AB6C659B4\",\"MileagePlusNumber\":\"MR722052\",\"RecordLocator\":\"ABCD\",\"ReshopRequest\":{\"ObjectName\":\"United.Persist.Definition.Shopping.Session\",\"countryCode\":\"US\",\"CslContext\":{\"_session\":\"17C979E184CC495EA083D45F4DD9D19D\",\"_transactionId\":\"00c87c77-f976-482d-a9a7-c504cc08ab59|5f2bc92d-f584-492a-8371-6e68120e14f2\",\"_token\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9\"}}}");

            _sessionHelperService.Setup(p => p.GetSession<MOBPNR>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBPNR);

            _dynamoDBService.Setup(p => p.GetRecords<MOBDisplayBagTrackAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);



            var _eligibleCheckBusiness = new EligibleCheckBusiness(_logger7.Object,  _configuration, _validateHashPinService.Object, _sessionHelperService.Object, _shoppingSessionHelper.Object, _dPService.Object, _pNRRetrievalService.Object, _dynamoDBService.Object, _refundService.Object, _shopBusiness.Object, _cachingService1, _legalDocumentsForTitlesService.Object, _headers.Object, _featureSettings.Object, _iCMSContentService.Object);


            if (mOBRESHOPChangeEligibilityRequest.SessionId == "17C979E184CC495EA083D45F4DD9D19D")
            {
                 _configuration["EnableReshopChangeFeeForceUpdate"] = "true";
            }


           

            //Act
            var result = _eligibleCheckBusiness.ChangeEligibleCheck(mOBRESHOPChangeEligibilityRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);

}

        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ChangeEligibleCheck2_Test), MemberType = typeof(ReShoppingTestDataGenerator))]

        public void ChangeEligibleCheck2_Test(MOBRESHOPChangeEligibilityRequest mOBRESHOPChangeEligibilityRequest,
          EligibilityResponse eligibilityResponse, Session session, ReservationDetail reservationDetail, Reservation reservation, HashPinValidate hashPinValidate, AirportDetailsList airportDetailsList, MOBPNR mOBPNR, List<MOBLegalDocument> mOBLegalDocuments, MOBDisplayBagTrackAirportDetails displayAirportDetails)
        {
            var _dataPowerFactory = new DataPowerFactory(_configuration, _sessionHelperService.Object, _logger4.Object);

            _sessionHelperService.Setup(p => p.GetSession<EligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(eligibilityResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            //_dynamoDBService.Setup(p =>p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()).Returns(displayAirportDetails);


            _dPService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny< IConfiguration>())).ReturnsAsync("Bearer Token");

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinValidate);

            //_dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(mOBLegalDocument);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(mOBLegalDocuments);

            // _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(legalDocuments);

            _refundService.Setup(p => p.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"application\":{\"id\":2,\"isProduction\":false,\"name\":\"Android\",\"version\":{\"major\":\"4.1.25\",\"minor\":\"4.1.25\"}},\"SessionId\":\"17C979E184CC495EA083D45F4DD9D19D\",\"HashPinCode\":\"7C7D6DE3BB0A56DD424303E20EA996E1AF7DE6BF\",\"DeviceId\":\"1E70A8CC-2A59-4450-AF89-0D4AB6C659B4\",\"MileagePlusNumber\":\"MR722052\",\"RecordLocator\":\"ABCD\",\"ReshopRequest\":{\"ObjectName\":\"United.Persist.Definition.Shopping.Session\",\"countryCode\":\"US\",\"CslContext\":{\"_session\":\"17C979E184CC495EA083D45F4DD9D19D\",\"_transactionId\":\"00c87c77-f976-482d-a9a7-c504cc08ab59|5f2bc92d-f584-492a-8371-6e68120e14f2\",\"_token\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9\"}}}");

            _sessionHelperService.Setup(p => p.GetSession<MOBPNR>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBPNR);

            _dynamoDBService.Setup(p => p.GetRecords<MOBDisplayBagTrackAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);



            var _eligibleCheckBusiness = new EligibleCheckBusiness(_logger7.Object,  _configuration, _validateHashPinService.Object, _sessionHelperService.Object, _shoppingSessionHelper.Object, _dPService.Object, _pNRRetrievalService.Object, _dynamoDBService.Object, _refundService.Object, _shopBusiness.Object, _cachingService1, _legalDocumentsForTitlesService.Object, _headers.Object, _featureSettings.Object, _iCMSContentService.Object);


            if (mOBRESHOPChangeEligibilityRequest.SessionId == "17C979E184CC495EA083D45F4DD9D19D")
            {
                 _configuration["EnableEncryptedRedirectUrl"] = "true";
            }

            if (mOBRESHOPChangeEligibilityRequest.SessionId == "17C979E184CC495EA083D45F4DD9D19D")
            {
                 _configuration["EnableReshopChangeFeeForceUpdate"] = "false";
            }
            if (mOBRESHOPChangeEligibilityRequest.SessionId == "17C979E184CC495EA083D45F4DD9D19D")
            {
                 _configuration["Reshop_ChangeCancelMsg_Content"] = "true";
            }


            //Act
            var result = _eligibleCheckBusiness.ChangeEligibleCheck(mOBRESHOPChangeEligibilityRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);


        }


        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ChangeEligibleCheck3_Test), MemberType = typeof(ReShoppingTestDataGenerator))]

        public void ChangeEligibleCheck3_Test(MOBRESHOPChangeEligibilityRequest mOBRESHOPChangeEligibilityRequest,
           EligibilityResponse eligibilityResponse, Session session, ReservationDetail reservationDetail, Reservation reservation, HashPinValidate hashPinValidate, AirportDetailsList airportDetailsList, MOBPNR mOBPNR, List<MOBLegalDocument> mOBLegalDocuments, MOBDisplayBagTrackAirportDetails displayAirportDetails)
        {
            var _dataPowerFactory = new DataPowerFactory(_configuration, _sessionHelperService.Object, _logger4.Object);

            _sessionHelperService.Setup(p => p.GetSession<EligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(eligibilityResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            //_dynamoDBService.Setup(p =>p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()).Returns(displayAirportDetails);


            _dPService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Token");

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinValidate);

            //_dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(mOBLegalDocument);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(mOBLegalDocuments);

            // _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(legalDocuments);

            _refundService.Setup(p => p.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"application\":{\"id\":2,\"isProduction\":false,\"name\":\"Android\",\"version\":{\"major\":\"4.1.25\",\"minor\":\"4.1.25\"}},\"SessionId\":\"17C979E184CC495EA083D45F4DD9D19D\",\"HashPinCode\":\"7C7D6DE3BB0A56DD424303E20EA996E1AF7DE6BF\",\"DeviceId\":\"1E70A8CC-2A59-4450-AF89-0D4AB6C659B4\",\"MileagePlusNumber\":\"MR722052\",\"RecordLocator\":\"ABCD\",\"ReshopRequest\":{\"ObjectName\":\"United.Persist.Definition.Shopping.Session\",\"countryCode\":\"US\",\"CslContext\":{\"_session\":\"17C979E184CC495EA083D45F4DD9D19D\",\"_transactionId\":\"00c87c77-f976-482d-a9a7-c504cc08ab59|5f2bc92d-f584-492a-8371-6e68120e14f2\",\"_token\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9\"}}}");

            _sessionHelperService.Setup(p => p.GetSession<MOBPNR>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBPNR);


            _dynamoDBService.Setup(p => p.GetRecords<MOBDisplayBagTrackAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);



            var _eligibleCheckBusiness = new EligibleCheckBusiness(_logger7.Object, _configuration, _validateHashPinService.Object, _sessionHelperService.Object, _shoppingSessionHelper.Object, _dPService.Object, _pNRRetrievalService.Object, _dynamoDBService.Object, _refundService.Object, _shopBusiness.Object, _cachingService1, _legalDocumentsForTitlesService.Object, _headers.Object, _featureSettings.Object,_iCMSContentService.Object);

            if (mOBRESHOPChangeEligibilityRequest.SessionId == "17C979E184CC495EA083D45F4DD9D19D")
            {
                _configuration["EnableEncryptedRedirectUrl"] = "true";
            }
            if (mOBRESHOPChangeEligibilityRequest.SessionId == "17C979E184CC495EA083D45F4DD9D19D")
            {
                _configuration["EnableReshopNewTripResidualAlert"] = "true";
            }


            //Act
            var result = _eligibleCheckBusiness.ChangeEligibleCheck(mOBRESHOPChangeEligibilityRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);


        }




        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ChangeEligibleCheck_negative_Test), MemberType = typeof(ReShoppingTestDataGenerator))]

        public void ChangeEligibleCheck_negative_Test(MOBRESHOPChangeEligibilityRequest mOBRESHOPChangeEligibilityRequest,
          EligibilityResponse eligibilityResponse, Session session, ReservationDetail reservationDetail, Reservation reservation, HashPinValidate hashPinValidate, AirportDetailsList airportDetailsList, MOBPNR mOBPNR, List<MOBLegalDocument> mOBLegalDocuments, MOBDisplayBagTrackAirportDetails displayAirportDetails)
        {
            var _dataPowerFactory = new DataPowerFactory(_configuration, _sessionHelperService.Object, _logger4.Object);

            _sessionHelperService.Setup(p => p.GetSession<EligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(eligibilityResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            //_dynamoDBService.Setup(p =>p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()).Returns(displayAirportDetails);


            _dPService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny< IConfiguration>())).ReturnsAsync("Bearer Token");

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinValidate);

            //_dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(mOBLegalDocument);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(mOBLegalDocuments);

            // _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(legalDocuments);

            _refundService.Setup(p => p.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"application\":{\"id\":2,\"isProduction\":false,\"name\":\"Android\",\"version\":{\"major\":\"4.1.25\",\"minor\":\"4.1.25\"}},\"SessionId\":\"17C979E184CC495EA083D45F4DD9D19D\",\"HashPinCode\":\"7C7D6DE3BB0A56DD424303E20EA996E1AF7DE6BF\",\"DeviceId\":\"1E70A8CC-2A59-4450-AF89-0D4AB6C659B4\",\"MileagePlusNumber\":\"MR722052\",\"RecordLocator\":\"ABCD\",\"ReshopRequest\":{\"ObjectName\":\"United.Persist.Definition.Shopping.Session\",\"countryCode\":\"US\",\"CslContext\":{\"_session\":\"17C979E184CC495EA083D45F4DD9D19D\",\"_transactionId\":\"00c87c77-f976-482d-a9a7-c504cc08ab59|5f2bc92d-f584-492a-8371-6e68120e14f2\",\"_token\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9\"}}}");

            _sessionHelperService.Setup(p => p.GetSession<MOBPNR>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBPNR);

            _dynamoDBService.Setup(p => p.GetRecords<MOBDisplayBagTrackAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);



            var _eligibleCheckBusiness = new EligibleCheckBusiness(_logger7.Object,  _configuration, _validateHashPinService.Object, _sessionHelperService.Object, _shoppingSessionHelper.Object, _dPService.Object, _pNRRetrievalService.Object, _dynamoDBService.Object, _refundService.Object, _shopBusiness.Object, _cachingService1, _legalDocumentsForTitlesService.Object, _headers.Object, _featureSettings.Object, _iCMSContentService.Object);


            if (mOBRESHOPChangeEligibilityRequest.SessionId == "17C979E184CC495EA083D45F4DD9D19D")
            {
                 _configuration["DisableCorporateReshopChange"] = "true";
            }
            if (mOBRESHOPChangeEligibilityRequest.SessionId == "17C979E184CC495EA083D45F4DD9D19D")
            {
                 _configuration["ReshopChangeIneligibleVendorNames"] = "true";
            }

            //Act
            var result = _eligibleCheckBusiness.ChangeEligibleCheck(mOBRESHOPChangeEligibilityRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);


        }


        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ChangeEligibleCheck_negative1_Test), MemberType = typeof(ReShoppingTestDataGenerator))]

        public void ChangeEligibleCheck_negative1_Test(MOBRESHOPChangeEligibilityRequest mOBRESHOPChangeEligibilityRequest,
          EligibilityResponse eligibilityResponse, Session session, ReservationDetail reservationDetail, Reservation reservation, HashPinValidate hashPinValidate, AirportDetailsList airportDetailsList, MOBPNR mOBPNR, List<MOBLegalDocument> mOBLegalDocuments, MOBDisplayBagTrackAirportDetails displayAirportDetails)
        {
            var _dataPowerFactory = new DataPowerFactory(_configuration, _sessionHelperService.Object, _logger4.Object);

            _sessionHelperService.Setup(p => p.GetSession<EligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(eligibilityResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            //_dynamoDBService.Setup(p =>p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()).Returns(displayAirportDetails);


            _dPService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny< IConfiguration>())).ReturnsAsync("Bearer Token");

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinValidate);

            //_dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(mOBLegalDocument);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(mOBLegalDocuments);

            // _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(legalDocuments);

            _refundService.Setup(p => p.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"application\":{\"id\":2,\"isProduction\":false,\"name\":\"Android\",\"version\":{\"major\":\"4.1.25\",\"minor\":\"4.1.25\"}},\"SessionId\":\"17C979E184CC495EA083D45F4DD9D19D\",\"HashPinCode\":\"7C7D6DE3BB0A56DD424303E20EA996E1AF7DE6BF\",\"DeviceId\":\"1E70A8CC-2A59-4450-AF89-0D4AB6C659B4\",\"MileagePlusNumber\":\"MR722052\",\"RecordLocator\":\"ABCD\",\"ReshopRequest\":{\"ObjectName\":\"United.Persist.Definition.Shopping.Session\",\"countryCode\":\"US\",\"CslContext\":{\"_session\":\"17C979E184CC495EA083D45F4DD9D19D\",\"_transactionId\":\"00c87c77-f976-482d-a9a7-c504cc08ab59|5f2bc92d-f584-492a-8371-6e68120e14f2\",\"_token\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9\"}}}");

            _sessionHelperService.Setup(p => p.GetSession<MOBPNR>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBPNR);

            _dynamoDBService.Setup(p => p.GetRecords<MOBDisplayBagTrackAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);



            var _eligibleCheckBusiness = new EligibleCheckBusiness(_logger7.Object,  _configuration, _validateHashPinService.Object, _sessionHelperService.Object, _shoppingSessionHelper.Object, _dPService.Object, _pNRRetrievalService.Object, _dynamoDBService.Object, _refundService.Object, _shopBusiness.Object, _cachingService1, _legalDocumentsForTitlesService.Object, _headers.Object, _featureSettings.Object, _iCMSContentService.Object);


            if (mOBRESHOPChangeEligibilityRequest.SessionId == "17C979E184CC495EA083D45F4DD9D19D")
            {
                 _configuration["MilesMoneyReshopEligibility"] = "true";
            }


            if (mOBRESHOPChangeEligibilityRequest.SessionId == "17C979E184CC495EA083D45F4DD9D19D")
            {
                 _configuration["ReshopChangeIneligibleVendorNames"] = "true";
            }


            if (mOBRESHOPChangeEligibilityRequest.SessionId == "17C979E184CC495EA083D45F4DD9D19D")
            {
                 _configuration["CorporateReshopChangeWarningMsg"] = "true";
            }




            //Act
            var result = _eligibleCheckBusiness.ChangeEligibleCheck(mOBRESHOPChangeEligibilityRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);


        }


        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ChangeEligibleCheck_negative2_Test), MemberType = typeof(ReShoppingTestDataGenerator))]

        public void ChangeEligibleCheck_negative2_Test(MOBRESHOPChangeEligibilityRequest mOBRESHOPChangeEligibilityRequest,
          EligibilityResponse eligibilityResponse, Session session, ReservationDetail reservationDetail, Reservation reservation, HashPinValidate hashPinValidate, AirportDetailsList airportDetailsList, MOBPNR mOBPNR, List<MOBLegalDocument> mOBLegalDocuments, MOBDisplayBagTrackAirportDetails displayAirportDetails)
        {
            var _dataPowerFactory = new DataPowerFactory(_configuration, _sessionHelperService.Object, _logger4.Object);

            _sessionHelperService.Setup(p => p.GetSession<EligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(eligibilityResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            //_dynamoDBService.Setup(p =>p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()).Returns(displayAirportDetails);


            _dPService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny< IConfiguration>())).ReturnsAsync("Bearer Token");

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinValidate);

            //_dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(mOBLegalDocument);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(mOBLegalDocuments);

            // _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(legalDocuments);

            _refundService.Setup(p => p.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"application\":{\"id\":2,\"isProduction\":false,\"name\":\"Android\",\"version\":{\"major\":\"4.1.25\",\"minor\":\"4.1.25\"}},\"SessionId\":\"17C979E184CC495EA083D45F4DD9D19D\",\"HashPinCode\":\"7C7D6DE3BB0A56DD424303E20EA996E1AF7DE6BF\",\"DeviceId\":\"1E70A8CC-2A59-4450-AF89-0D4AB6C659B4\",\"MileagePlusNumber\":\"MR722052\",\"RecordLocator\":\"ABCD\",\"ReshopRequest\":{\"ObjectName\":\"United.Persist.Definition.Shopping.Session\",\"countryCode\":\"US\",\"CslContext\":{\"_session\":\"17C979E184CC495EA083D45F4DD9D19D\",\"_transactionId\":\"00c87c77-f976-482d-a9a7-c504cc08ab59|5f2bc92d-f584-492a-8371-6e68120e14f2\",\"_token\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9\"}}}");

            _sessionHelperService.Setup(p => p.GetSession<MOBPNR>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBPNR);

            _dynamoDBService.Setup(p => p.GetRecords<MOBDisplayBagTrackAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);



            var _eligibleCheckBusiness = new EligibleCheckBusiness(_logger7.Object,  _configuration, _validateHashPinService.Object, _sessionHelperService.Object, _shoppingSessionHelper.Object, _dPService.Object, _pNRRetrievalService.Object, _dynamoDBService.Object, _refundService.Object, _shopBusiness.Object, _cachingService1, _legalDocumentsForTitlesService.Object, _headers.Object, _featureSettings.Object, _iCMSContentService.Object);





            //Act
            var result = _eligibleCheckBusiness.ChangeEligibleCheck(mOBRESHOPChangeEligibilityRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);


        }



        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ChangeEligibleCheck_negative3_Test), MemberType = typeof(ReShoppingTestDataGenerator))]

        public void ChangeEligibleCheck_negative3_Test(MOBRESHOPChangeEligibilityRequest mOBRESHOPChangeEligibilityRequest,
        EligibilityResponse eligibilityResponse, Session session, ReservationDetail reservationDetail, Reservation reservation, HashPinValidate hashPinValidate, AirportDetailsList airportDetailsList, MOBPNR mOBPNR, List<MOBLegalDocument> mOBLegalDocuments, MOBDisplayBagTrackAirportDetails displayAirportDetails)
        {
            var _dataPowerFactory = new DataPowerFactory(_configuration, _sessionHelperService.Object, _logger4.Object);

            _sessionHelperService.Setup(p => p.GetSession<EligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(eligibilityResponse);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            //_dynamoDBService.Setup(p =>p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()).Returns(displayAirportDetails);


            _dPService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny< IConfiguration>())).ReturnsAsync("Bearer Token");

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinValidate);

            //_dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(mOBLegalDocument);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(mOBLegalDocuments);

            // _legalDocumentsForTitlesService.Setup(p => p.GetNewLegalDocumentsForTitles(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(legalDocuments);

            _refundService.Setup(p => p.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"application\":{\"id\":2,\"isProduction\":false,\"name\":\"Android\",\"version\":{\"major\":\"4.1.25\",\"minor\":\"4.1.25\"}},\"SessionId\":\"17C979E184CC495EA083D45F4DD9D19D\",\"HashPinCode\":\"7C7D6DE3BB0A56DD424303E20EA996E1AF7DE6BF\",\"DeviceId\":\"1E70A8CC-2A59-4450-AF89-0D4AB6C659B4\",\"MileagePlusNumber\":\"MR722052\",\"RecordLocator\":\"ABCD\",\"ReshopRequest\":{\"ObjectName\":\"United.Persist.Definition.Shopping.Session\",\"countryCode\":\"US\",\"CslContext\":{\"_session\":\"17C979E184CC495EA083D45F4DD9D19D\",\"_transactionId\":\"00c87c77-f976-482d-a9a7-c504cc08ab59|5f2bc92d-f584-492a-8371-6e68120e14f2\",\"_token\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9\"}}}");

            _sessionHelperService.Setup(p => p.GetSession<MOBPNR>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBPNR);

            _dynamoDBService.Setup(p => p.GetRecords<MOBDisplayBagTrackAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);



            var _eligibleCheckBusiness = new EligibleCheckBusiness(_logger7.Object,  _configuration, _validateHashPinService.Object, _sessionHelperService.Object, _shoppingSessionHelper.Object, _dPService.Object, _pNRRetrievalService.Object, _dynamoDBService.Object, _refundService.Object, _shopBusiness.Object, _cachingService1, _legalDocumentsForTitlesService.Object, _headers.Object, _featureSettings.Object, _iCMSContentService.Object);


            



            //Act
            var result = _eligibleCheckBusiness.ChangeEligibleCheck(mOBRESHOPChangeEligibilityRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);


        }



        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ChangeEligibleCheckAndReshop_Test), MemberType = typeof(ReShoppingTestDataGenerator))]

        public void ChangeEligibleCheckAndReshop_Test(MOBRESHOPChangeEligibilityRequest mOBRESHOPChangeEligibilityRequest, Session session, ReservationDetail reservationDetail, Reservation reservation, MOBRESHOPChangeEligibilityResponse mOBRESHOPChangeEligibilityResponse, AirportDetailsList airportDetailsList, DisplayAirportDetails displayAirportDetails )
        {
            var _dataPowerFactory = new DataPowerFactory(_configuration, _sessionHelperService.Object, _logger4.Object);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            
            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);


            _shopBusiness.Setup(p => p.GetShop(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBRESHOPChangeEligibilityResponse.ShopResponse);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            MOBRESHOPChangeEligibilityResponse mOBRESHOPChangeEligibilityResponse1 = new MOBRESHOPChangeEligibilityResponse
            {
                BWCSessionId = "D4106F22B1F941288F5D0E441720E80C",
                AwardTravel = true,
                ExceptionPolicyEligible = false,
                FailedRule = "ABCD",
                LanguageCode = "en-US",
                MachineName = "Dell"
            };
            var response = JsonConvert.SerializeObject(mOBRESHOPChangeEligibilityResponse1);

            _refundService.Setup(p => p.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            _dPService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny< IConfiguration>())).ReturnsAsync("Bearer Token");

            _dynamoDBService.Setup(p => p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);



            //_airportDynamoDB.Setup(p => p.GetAirportCityName(It.IsAny<int>(), It.IsAny<string>(), It.IsAny< IConfiguration>())).Returns(Task.FromResult((true,"NY","US")));

            var _eligibleCheckBusiness = new EligibleCheckBusiness(_logger7.Object, _configuration, _validateHashPinService.Object,_sessionHelperService.Object, _shoppingSessionHelper.Object, _dPService.Object, _pNRRetrievalService.Object, _dynamoDBService.Object, _refundService.Object, _shopBusiness.Object, _cachingService1, _legalDocumentsForTitlesService.Object, _headers.Object, _featureSettings.Object, _iCMSContentService.Object);

            //Act
            var result = _eligibleCheckBusiness.ChangeEligibleCheckAndReshop(mOBRESHOPChangeEligibilityRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);
        }

        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ChangeEligibleCheckAndReshop1_Test), MemberType = typeof(ReShoppingTestDataGenerator))]

        public void ChangeEligibleCheckAndReshop1_Test(MOBRESHOPChangeEligibilityRequest mOBRESHOPChangeEligibilityRequest, Session session, ReservationDetail reservationDetail, Reservation reservation, MOBRESHOPChangeEligibilityResponse mOBRESHOPChangeEligibilityResponse, AirportDetailsList airportDetailsList, DisplayAirportDetails displayAirportDetails)
        {
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            //_shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);


            //_shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync();

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);


            _shopBusiness.Setup(p => p.GetShop(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBRESHOPChangeEligibilityResponse.ShopResponse);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            MOBRESHOPChangeEligibilityResponse mOBRESHOPChangeEligibilityResponse1 = new MOBRESHOPChangeEligibilityResponse
            {
                BWCSessionId = "D4106F22B1F941288F5D0E441720E80C",
                AwardTravel = true,
                ExceptionPolicyEligible = false,
                FailedRule = "ABCD",
                LanguageCode = "en-US",
                MachineName = "Dell"
            };
            var response = JsonConvert.SerializeObject(mOBRESHOPChangeEligibilityResponse1);

            _refundService.Setup(p => p.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            _dPService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny< IConfiguration>())).ReturnsAsync("Bearer Token");

            _dynamoDBService.Setup(p => p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);

            if (mOBRESHOPChangeEligibilityRequest.SessionId == "17C979E184CC495EA083D45F4DD9D19D")
            {
                _configuration["ReshopEligiblityDontLoadReservationWhenExceptionInCSLResponse"] = "true";
            }



            //_airportDynamoDB.Setup(p => p.GetAirportCityName(It.IsAny<int>(), It.IsAny<string>(), It.IsAny< IConfiguration>())).Returns(Task.FromResult((true,"NY","US")));

            //Act
            var result = _eligibleCheckBusiness.ChangeEligibleCheckAndReshop(mOBRESHOPChangeEligibilityRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);
        }

        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ChangeEligibleCheckAndReshop2_Test), MemberType = typeof(ReShoppingTestDataGenerator))]

        public void ChangeEligibleCheckAndReshop2_Test(MOBRESHOPChangeEligibilityRequest mOBRESHOPChangeEligibilityRequest, Session session, ReservationDetail reservationDetail, Reservation reservation, MOBRESHOPChangeEligibilityResponse mOBRESHOPChangeEligibilityResponse, AirportDetailsList airportDetailsList, DisplayAirportDetails displayAirportDetails)
        {
            var _dataPowerFactory = new DataPowerFactory(_configuration, _sessionHelperService.Object, _logger4.Object);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            //_shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);


            //_shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync();

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);


            _shopBusiness.Setup(p => p.GetShop(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBRESHOPChangeEligibilityResponse.ShopResponse);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            MOBRESHOPChangeEligibilityResponse mOBRESHOPChangeEligibilityResponse1 = new MOBRESHOPChangeEligibilityResponse
            {
                BWCSessionId = "D4106F22B1F941288F5D0E441720E80C",
                AwardTravel = true,
                ExceptionPolicyEligible = false,
                FailedRule = "ABCD",
                LanguageCode = "en-US",
                MachineName = "Dell"
            };
            var response = JsonConvert.SerializeObject(mOBRESHOPChangeEligibilityResponse1);

            _refundService.Setup(p => p.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            _dPService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Token");

            _dynamoDBService.Setup(p => p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);

            var _eligibleCheckBusiness = new EligibleCheckBusiness(_logger7.Object, _configuration, _validateHashPinService.Object, _sessionHelperService.Object, _shoppingSessionHelper.Object, _dPService.Object, _pNRRetrievalService.Object, _dynamoDBService.Object, _refundService.Object, _shopBusiness.Object, _cachingService1, _legalDocumentsForTitlesService.Object, _headers.Object, _featureSettings.Object, _iCMSContentService.Object);

          
            //_airportDynamoDB.Setup(p => p.GetAirportCityName(It.IsAny<int>(), It.IsAny<string>(), It.IsAny< IConfiguration>())).Returns(Task.FromResult((true,"NY","US")));

            //Act
            var result = _eligibleCheckBusiness.ChangeEligibleCheckAndReshop(mOBRESHOPChangeEligibilityRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);
        }

        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ChangeEligibleCheckAndReshop3_Test), MemberType = typeof(ReShoppingTestDataGenerator))]

        public void ChangeEligibleCheckAndReshop3_Test(MOBRESHOPChangeEligibilityRequest mOBRESHOPChangeEligibilityRequest, Session session, ReservationDetail reservationDetail, Reservation reservation, MOBRESHOPChangeEligibilityResponse mOBRESHOPChangeEligibilityResponse, AirportDetailsList airportDetailsList, DisplayAirportDetails displayAirportDetails)
        {
            var _dataPowerFactory = new DataPowerFactory(_configuration, _sessionHelperService.Object, _logger4.Object);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            //_shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);


            //_shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync();

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);


            _shopBusiness.Setup(p => p.GetShop(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBRESHOPChangeEligibilityResponse.ShopResponse);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            MOBRESHOPChangeEligibilityResponse mOBRESHOPChangeEligibilityResponse1 = new MOBRESHOPChangeEligibilityResponse
            {
                BWCSessionId = "D4106F22B1F941288F5D0E441720E80C",
                AwardTravel = true,
                ExceptionPolicyEligible = false,
                FailedRule = "ABCD",
                LanguageCode = "en-US",
                MachineName = "Dell"
            };
            var response = JsonConvert.SerializeObject(mOBRESHOPChangeEligibilityResponse1);

            _refundService.Setup(p => p.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            _dPService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Token");

            _dynamoDBService.Setup(p => p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);

            var _eligibleCheckBusiness = new EligibleCheckBusiness(_logger7.Object, _configuration, _validateHashPinService.Object, _sessionHelperService.Object, _shoppingSessionHelper.Object, _dPService.Object, _pNRRetrievalService.Object, _dynamoDBService.Object, _refundService.Object, _shopBusiness.Object, _cachingService1, _legalDocumentsForTitlesService.Object, _headers.Object, _featureSettings.Object, _iCMSContentService.Object);


            //_airportDynamoDB.Setup(p => p.GetAirportCityName(It.IsAny<int>(), It.IsAny<string>(), It.IsAny< IConfiguration>())).Returns(Task.FromResult((true,"NY","US")));

            //Act
            var result = _eligibleCheckBusiness.ChangeEligibleCheckAndReshop(mOBRESHOPChangeEligibilityRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);
        }


        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ChangeEligibleCheckAndReshop_negative_Test), MemberType = typeof(ReShoppingTestDataGenerator))]

        public void ChangeEligibleCheckAndReshop_negative_Test(MOBRESHOPChangeEligibilityRequest mOBRESHOPChangeEligibilityRequest, Session session, ReservationDetail reservationDetail, Reservation reservation, MOBRESHOPChangeEligibilityResponse mOBRESHOPChangeEligibilityResponse, AirportDetailsList airportDetailsList, DisplayAirportDetails displayAirportDetails)
        {
           // _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);


            //_shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync();

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);


            _shopBusiness.Setup(p => p.GetShop(It.IsAny<MOBSHOPShopRequest>(), It.IsAny<HttpContext>())).ReturnsAsync(mOBRESHOPChangeEligibilityResponse.ShopResponse);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

            MOBRESHOPChangeEligibilityResponse mOBRESHOPChangeEligibilityResponse1 = new MOBRESHOPChangeEligibilityResponse
            {
                BWCSessionId = "D4106F22B1F941288F5D0E441720E80C",
                AwardTravel = true,
                ExceptionPolicyEligible = false,
                FailedRule = "ABCD",
                LanguageCode = "en-US",
                MachineName = "Dell"
            };
            var response = JsonConvert.SerializeObject(mOBRESHOPChangeEligibilityResponse1);

            _refundService.Setup(p => p.GetEligibleCheck<MOBRESHOPChangeEligibilityResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response);

            _dPService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny< IConfiguration>())).ReturnsAsync("Bearer Token");

            _dynamoDBService.Setup(p => p.GetRecords<DisplayAirportDetails>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(displayAirportDetails);


            //_airportDynamoDB.Setup(p => p.GetAirportCityName(It.IsAny<int>(), It.IsAny<string>(), It.IsAny< IConfiguration>())).Returns(Task.FromResult((true,"NY","US")));

            //Act
            var result = _eligibleCheckBusiness.ChangeEligibleCheckAndReshop(mOBRESHOPChangeEligibilityRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);
        }

        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ReshopSaveEmail_CFOP_Test), MemberType = typeof(ReShoppingTestDataGenerator))]
        public void ReshopSaveEmail_CFOP_Test(MOBChangeEmailRequest mOBChangeEmailRequest, Reservation reservation, Session session, MOBShoppingCart mOBShoppingCart, SeatChangeState seatChangeState, ReservationDetail reservationDetail, AirportDetailsList airportDetailsList, MOBChangeEmailResponse mOBChangeEmailResponse  )
        {
            var _dataPowerFactory = new DataPowerFactory( _configuration, _sessionHelperService.Object, _logger4.Object);

            _shoppingSessionHelper.Setup(p => p.GetValidateSession(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingUtility1.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(),It.IsAny<string>())).ReturnsAsync(mOBChangeEmailResponse.Reservation);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            //_sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            //_sessionHelperService.Setup(p => p.GetSessionId(It.IsAny<HttpContextValues>(), It.IsAny<string>())).Returns(sessionId);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _sessionHelperService.Setup(p => p.GetSession<SeatChangeState>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(seatChangeState);

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            //_shoppingCartService.Setup(p => p.GetCartInformation<LoadReservationAndDisplayCartResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(loadReservationAndDisplayCartResponse);

            // _sessionHelperService.Setup(p => p.GetSession<MOBApplyPromoCodeResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBApplyPromoCodeResponse);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);

          

            var updateProfileBusiness = new UpdateProfileBusiness(_logger2.Object,  _configuration, _shoppingSessionHelper.Object, _sessionHelperService.Object, _shoppingUtility, _travelerUtility.Object, _formsOfPayment.Object, _cachingService1,
                _headers.Object);

            //Act
            var result = updateProfileBusiness.ReshopSaveEmail_CFOP(mOBChangeEmailRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);

        }


        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ReshopSaveEmail_CFOP2_Test), MemberType = typeof(ReShoppingTestDataGenerator))]
        public void ReshopSaveEmail_CFOP2_Test(MOBChangeEmailRequest mOBChangeEmailRequest, Reservation reservation, Session session, MOBShoppingCart mOBShoppingCart, SeatChangeState seatChangeState, ReservationDetail reservationDetail, AirportDetailsList airportDetailsList, MOBChangeEmailResponse mOBChangeEmailResponse)
        {
            var _dataPowerFactory = new DataPowerFactory( _configuration, _sessionHelperService.Object, _logger4.Object);

           _shoppingSessionHelper.Setup(p => p.GetValidateSession(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingUtility1.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(mOBChangeEmailResponse.Reservation);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            //_sessionHelperService.Setup(p => p.GetSessionId(It.IsAny<HttpContextValues>(), It.IsAny<string>())).Returns(sessionId);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _sessionHelperService.Setup(p => p.GetSession<SeatChangeState>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(seatChangeState);

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            //_shoppingCartService.Setup(p => p.GetCartInformation<LoadReservationAndDisplayCartResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(loadReservationAndDisplayCartResponse);

            // _sessionHelperService.Setup(p => p.GetSession<MOBApplyPromoCodeResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBApplyPromoCodeResponse);

            _sessionHelperService.Setup(p => p.GetSession<AirportDetailsList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(airportDetailsList);



            var updateProfileBusiness = new UpdateProfileBusiness(_logger2.Object,  _configuration, _shoppingSessionHelper.Object, _sessionHelperService.Object, _shoppingUtility, _travelerUtility.Object, _formsOfPayment.Object, _cachingService1,
                _headers.Object);

            //Act
            var result = updateProfileBusiness.ReshopSaveEmail_CFOP(mOBChangeEmailRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);

        }



        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.SelectTrip_Test), MemberType = typeof(ReShoppingTestDataGenerator))]
        public void SelectTrip_Test(SelectTripRequest selectTripRequest, Session session, SelectTrip selectTrip, CSLShopRequest cSLShopRequest, Reservation reservation, SelectTripResponse selectTripResponse)
        {
            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);


            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);


            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _shoppingBusiness.Setup(p => p.SelectTrip(It.IsAny<SelectTripRequest>(), It.IsAny<HttpContext>())).ReturnsAsync(selectTripResponse);



            //_shoppingUtility.Setup(p => p.PopulateShoppingCart(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(selectTripResponse);


            //_shoppingUtility.Setup(p => p.ReservationToShoppingCart_DataMigration(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            //_sessionHelperService.Setup(p => p.GetSession<MOBSHOPShopRequest>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<string>())).Returns(mOBSHOPShopRequest);

            //_sessionHelperService.Setup(p => p.GetSession<SeatChangeState>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<string>())).Returns(seatChangeState);

            //Act
            var result = _reshopSelectTripBusiness.SelectTrip(selectTripRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);
        }

        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.SelectTrip_negative_Test), MemberType = typeof(ReShoppingTestDataGenerator))]
        public void SelectTrip_Test_negative(SelectTripRequest selectTripRequest, Session session, SelectTrip selectTrip, CSLShopRequest cSLShopRequest, Reservation reservation, SelectTripResponse selectTripResponse)
        {
            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);


            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);


            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _shoppingBusiness.Setup(p => p.SelectTrip(It.IsAny<SelectTripRequest>(), It.IsAny<HttpContext>())).ReturnsAsync(selectTripResponse);



            //_shoppingUtility.Setup(p => p.PopulateShoppingCart(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(selectTripResponse);


            //_shoppingUtility.Setup(p => p.ReservationToShoppingCart_DataMigration(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            //_sessionHelperService.Setup(p => p.GetSession<MOBSHOPShopRequest>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<string>())).Returns(mOBSHOPShopRequest);

            //_sessionHelperService.Setup(p => p.GetSession<SeatChangeState>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<string>())).Returns(seatChangeState);

            //Act
            var result = _reshopSelectTripBusiness.SelectTrip(selectTripRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);
        }

        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.GetProducts_CFOP_Test), MemberType = typeof(ReShoppingTestDataGenerator))]
        public void GetProducts_CFOP_Test(MOBSHOPProductSearchRequest mOBSHOPProductSearchRequest, Session session, Reservation reservation,
            ShoppingResponse shoppingResponse, ReservationDetail reservationDetail,  List<MOBLegalDocument> mOBLegalDocuments, GetOffers 
             getOffers)
        {
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);


            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

           

            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mOBLegalDocuments);


            _purchaseMerchandizingService.Setup(p => p.GetMerchOfferInfo<GetOffers>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((getOffers, 33001222221)));


            //Act
            var result = _getProducts_CFOPBusiness.GetProducts_CFOP(mOBSHOPProductSearchRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);
        }

        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.GetProducts_CFOP_Flow), MemberType = typeof(ReShoppingTestDataGenerator))]
        public void GetProducts_CFOP_Flow(MOBSHOPProductSearchRequest mOBSHOPProductSearchRequest, Session session, Reservation reservation,
         ShoppingResponse shoppingResponse, ReservationDetail reservationDetail, List<MOBLegalDocument> mOBLegalDocuments, GetOffers
          getOffers)
        {
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);


            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);



            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(mOBLegalDocuments);


            _purchaseMerchandizingService.Setup(p => p.GetMerchOfferInfo<GetOffers>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((getOffers, 33001222221)));


            //Act
            var result = _getProducts_CFOPBusiness.GetProducts_CFOP(mOBSHOPProductSearchRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);
        }



        [Theory]
        [MemberData(nameof(ReShoppingTestDataGenerator.ConfirmScheduleChange_Test), MemberType = typeof(ReShoppingTestDataGenerator))]
        public void ConfirmScheduleChange_Test(MOBConfirmScheduleChangeRequest mOBConfirmScheduleChangeRequest, Session session, HashPinValidate hashPinValidate, ReservationDetail reservationDetail )
        {
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);
            _shoppingSessionHelper.Setup(p => p.GetValidateSession(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(session);
            //_sessionHelperService.Setup(p => p.GetSessionId(It.IsAny<HttpContextValues>(), It.IsAny<string>())).Returns(sessionId);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _dPService.Setup(p => p.GetAndSaveAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny< IConfiguration>(), It.IsAny<string>(), It.IsAny<Session>(), It.IsAny<bool>())).ReturnsAsync("Bearer Token");

            //_cachingService.Setup(p => p.GetCache<CustomerAuthorization>(It.IsAny<string>(), It.IsAny<string>())).Returns(customerAuthorization);

            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinValidate);

            _cachingService.Setup(p =>p.GetCache<DPTokenResponse>(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("Token");


            _sessionHelperService.Setup(p => p.GetSession<ReservationDetail>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationDetail);

            _reservationService.Setup(p =>p.ConfirmScheduleChange<List<United.Service.Presentation.CommonModel.Message>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"Text\":\"ABCD\",\"DisplaySequence\":1,\"Key\":\"1234\",\"Type\":\"5678\",\"Status\":\"wxyz\",\"Code\":\"pqrs\",\"Number\":4567,\"ContentType\":\"name123\",\"Value\":\"345678\",\"Flight\":{\"FlightNumber\":\"12345\",\"DepartureAirport\":\"678\"}}");



            //Act
            var result = _scheduleChangeBusiness.ConfirmScheduleChange(mOBConfirmScheduleChangeRequest);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);

        }

        }
}

