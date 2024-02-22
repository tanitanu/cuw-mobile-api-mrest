using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.DataAccess.Travelers;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Mobile.Model.Travelers;
using United.Mobile.Travelers.Domain;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Utility.Helper;
using United.Utility.Http;
using Xunit;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Test.Travelers.Tests
{
    public class TravelersBusinessTest
    {
        private readonly Mock<ICacheLog<TravelerBusiness>> _logger;
        private readonly IConfiguration _configuration;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly Mock<IShoppingUtility> _shoppingUtility;
        private readonly IShoppingUtility _shoppingUtility1;
        private readonly Mock<ICacheLog<ShoppingUtility>> _logger7;
        private readonly Mock<ITravelerUtility> _travelerUtility;
        private readonly Mock<IFormsOfPayment> _formsOfPayment;
        private readonly Mock<IFFCShoppingcs> _fFCShoppingcs;
        private readonly Mock<ITraveler> _traveler;
        private readonly Mock<IFlightShoppingProductsService> _getProductsService;
        private readonly Mock<IShoppingCartService> _shoppingCartService;
        private readonly Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly TravelerBusiness _travelerBusiness;
        private readonly Mock<IPKDispenserService> _moqpKDispenserService;
        private readonly Mock<ICachingService> _moqcachingService;
        private readonly Mock<IDPService> _moqdPService;
        private readonly Mock<IProductInfoHelper> _productInfoHelper;
        private readonly Mock<IPaymentService> _paymentService;
        private readonly Mock<PKDispenserPublicKey> _pKDispenserPublicKey;
        private readonly Mock<IShoppingBuyMiles> _mockShoppingBuyMiles;
        private readonly Mock<ICacheLog<CachingService>> _logger6;
        private readonly ICachingService _moqcachingService1;
        private readonly IResilientClient _resilientClient;
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy _policyWrap;
        private readonly string _baseUrl;
        private readonly Mock<IOmniCart> _omniCart;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ICSLStatisticsService _cSLStatisticsService;
        private readonly Mock<IPaymentUtility> _paymentUtility;
        private readonly Mock<IProductOffers> _shopping;
        private readonly Mock<ILegalDocumentsForTitlesService> _legalDocumentsForTitlesService;
        private readonly Mock<IValidateHashPinService> _validateHashPinService;
        private readonly Mock<IOptimizelyPersistService> _optimizelyPersistService;
        private readonly IDataPowerFactory _dataPowerFactory;
        private readonly Mock<ICacheLog<DataPowerFactory>> _logger8;
        private readonly Mock<IFeatureSettings> _featureSettings;
        private readonly Mock<IAuroraMySqlService> _auroraMySqlService;
        private readonly Mock<ILogger<TravelerBusiness>> _mlogger;
        private readonly Mock<IFeatureToggles> _featureToggles;

        public TravelersBusinessTest()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.test.json", optional: false, reloadOnChange: true)
                .Build();
            _logger = new Mock<ICacheLog<TravelerBusiness>>();
            _logger6 = new Mock<ICacheLog<CachingService>>();
            _logger7 = new Mock<ICacheLog<ShoppingUtility>>();
            _logger8 = new Mock<ICacheLog<DataPowerFactory>>();
            _mlogger = new Mock<ILogger<TravelerBusiness>>();
            _headers = new Mock<IHeaders>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _shoppingUtility = new Mock<IShoppingUtility>();
            _travelerUtility = new Mock<ITravelerUtility>();
            _formsOfPayment = new Mock<IFormsOfPayment>();
            _fFCShoppingcs = new Mock<IFFCShoppingcs>();
            _traveler = new Mock<ITraveler>();
            _getProductsService = new Mock<IFlightShoppingProductsService>();
            _shoppingCartService = new Mock<IShoppingCartService>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _moqpKDispenserService = new Mock<IPKDispenserService>();
            _moqcachingService = new Mock<ICachingService>();
            _moqdPService = new Mock<IDPService>();
            _productInfoHelper = new Mock<IProductInfoHelper>();
            _paymentService = new Mock<IPaymentService>();
            _mockShoppingBuyMiles = new Mock<IShoppingBuyMiles>();
            _omniCart = new Mock<IOmniCart>();
            _paymentUtility = new Mock<IPaymentUtility>();
            _shopping = new Mock<IProductOffers>();
            _legalDocumentsForTitlesService = new Mock<ILegalDocumentsForTitlesService>();
            _validateHashPinService = new Mock<IValidateHashPinService>();
            _optimizelyPersistService = new Mock<IOptimizelyPersistService>();
            _auroraMySqlService = new Mock<IAuroraMySqlService>();
            _pKDispenserPublicKey = new Mock<PKDispenserPublicKey>(_configuration, _moqcachingService.Object, _moqdPService.Object, _moqpKDispenserService.Object);

            _resilientClient = new ResilientClient(_baseUrl);

            _moqcachingService1 = new CachingService(_resilientClient, _logger6.Object, _configuration);

            _shoppingUtility1 = new ShoppingUtility(_logger7.Object , _configuration,_sessionHelperService.Object, _moqdPService.Object,_headers.Object,_dynamoDBService, _legalDocumentsForTitlesService.Object, _moqcachingService1, _validateHashPinService.Object, _optimizelyPersistService.Object, _mockShoppingBuyMiles.Object, _fFCShoppingcs.Object,_featureSettings.Object, _auroraMySqlService.Object);

            _travelerBusiness = new TravelerBusiness(_logger.Object, _configuration, _sessionHelperService.Object, _shoppingUtility.Object, _travelerUtility.Object, _fFCShoppingcs.Object, _traveler.Object, _getProductsService.Object, _shoppingCartService.Object, _shoppingSessionHelper.Object, _moqcachingService.Object, _moqdPService.Object, _moqpKDispenserService.Object, _productInfoHelper.Object, _formsOfPayment.Object, _paymentService.Object, _mockShoppingBuyMiles.Object, _omniCart.Object, _dynamoDBService, _cSLStatisticsService, _paymentUtility.Object, _shopping.Object, _headers.Object, _mlogger.Object, _featureSettings.Object, _featureToggles.Object);

            SetHeaders();
        }

        private string GetFileContent(string fileName)
        {
            var path = Path.IsPathRooted(fileName) ? fileName
                                                : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
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
        [MemberData(nameof(Input.TravelerInput1), MemberType = typeof(Input))]
        public void RegisterTravelers_CFOP_Test(MOBRegisterTravelersRequest request, MOBRegisterTravelersResponse response, Model.Shopping.Reservation reservation, Session session, bool ok, bool val, FlightReservationResponse flightReservationResponse, MOBShoppingCart shoppingCart, string seat , RegisterTravelersRequest registerTravelerRequest)
        {
            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);
            if (request.ProfileId != 111)
            {
                _sessionHelperService.Setup(_ => _.GetSession<Model.Shopping.Reservation>(It.IsAny<string>()
                         , It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);
            }
            _shoppingUtility.Setup(p => p.EnableTravelerTypes(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(ok);
            _travelerUtility.Setup(p => p.EnableYoungAdultValidation(It.IsAny<bool>())).Returns(ok);
            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(response.Reservation);
            var profileResponse = JsonConvert.DeserializeObject<Model.Shopping.ProfileResponse>(GetFileContent("ProfileResponse.json"));
            _travelerUtility.Setup(p => p.GetCSLProfileResponseInSession(It.IsAny<string>())).ReturnsAsync(profileResponse);
            _travelerUtility.Setup(p => p.AssignInfantWithSeat(It.IsAny<Model.Shopping.Reservation>(), It.IsAny<List<MOBCPTraveler>>())).Returns(request.Travelers);
            _travelerUtility.Setup(p => p.AssignInfantInLap(It.IsAny<Model.Shopping.Reservation>(), It.IsAny<List<MOBCPTraveler>>())).Returns(request.Travelers);
            //_shoppingUtility.Setup(p => p.GetTTypeValue(It.IsAny<int>(), It.IsAny<bool>()));
            _travelerUtility.Setup(p => p.GetChildInLapCount(It.IsAny<MOBCPTraveler>(), It.IsAny<List<int>>(), It.IsAny<int>(), It.IsAny<string>())).Returns(1);
            _shoppingUtility.Setup(p => p.EnableTravelerTypes(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(ok);
            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(response.Reservation);
            
            _travelerUtility.Setup(p => p.GetCSLProfileResponseInSession(It.IsAny<string>())).ReturnsAsync(profileResponse);
            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>()
                       , It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingCart);
           _shoppingUtility.Setup(p => p.IsETCEnabledforMultiTraveler(It.IsAny<int>(), It.IsAny<string>())).Returns(true);
            _shoppingUtility.Setup(p => p.IsETCCombinabilityEnabled(It.IsAny<int>(), It.IsAny<string>())).Returns(val);
            _traveler.Setup(p => p.RegisterTravelers_CFOP(It.IsAny<MOBRegisterTravelersRequest>(), It.IsAny<bool>())).ReturnsAsync(response.Reservation);
            _shoppingUtility.Setup(p => p.EnableReshopCubaTravelReasonVersion(It.IsAny<int>(), It.IsAny<string>())).Returns(ok);
            _travelerUtility.Setup(p => p.EnableTPI(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>())).Returns(ok);
            _shoppingUtility.Setup(p => p.IncludeMoneyPlusMiles(It.IsAny<int>(), It.IsAny<string>())).Returns(ok);
            _travelerUtility.Setup(p => p.EnableTravelerTypes(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(ok);
            _travelerUtility.Setup(p => p.IsETCEnabledforMultiTraveler(It.IsAny<int>(), It.IsAny<string>())).Returns(ok);
            _travelerUtility.Setup(p => p.IsETCCombinabilityEnabled(It.IsAny<int>(), It.IsAny<string>())).Returns(val);
            _travelerUtility.Setup(p => p.IncludeMoneyPlusMiles(It.IsAny<int>(), It.IsAny<string>())).Returns(ok);
            var loadResponse = JsonConvert.DeserializeObject<LoadReservationAndDisplayCartResponse>(GetFileContent("LoadReservationAndDisplayCartResponse.json"));
            _travelerUtility.Setup(p => p.GetCartInformation(It.IsAny<string>(), It.IsAny<MOBApplication>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Service.Presentation.CommonEnumModel.WorkFlowType>())).ReturnsAsync(loadResponse);
            _formsOfPayment.Setup(p => p.GetEligibleFormofPayments(It.IsAny<MOBRequest>(), It.IsAny<Session>(), It.IsAny<MOBShoppingCart>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<MOBSHOPReservation>(), It.IsAny<bool>(), It.IsAny<SeatChangeState>())).Returns(Task.FromResult((response.EligibleFormofPayments, false)));
            _sessionHelperService.Setup(_ => _.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingCart);

            var messagesList = JsonConvert.DeserializeObject<List<CMSContentMessage>>(GetFileContent("CMSContentMessage.json"));
            var messagesList1 = JsonConvert.DeserializeObject<List<MOBMobileCMSContentMessages>>(GetFileContent("CMSContentMessage.json"));
            //_shoppingUtility.Setup(p => p.GetSDLMessageFromList(It.IsAny<List<CMSContentMessage>>(), It.IsAny<string>())).Returns(messagesList1);
            _fFCShoppingcs.Setup(p => p.GetSDLContentByGroupName(It.IsAny<MOBRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(messagesList);
            //_pKDispenserPublicKey.Setup(p => p.GetCachedOrNewpkDispenserPublicKey(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(response.Reservation.PKDispenserPublicKey);
            _getProductsService.Setup(p => p.MilesAndMoneyOption<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);
            long duration = 9223372036854775807;
            _shoppingCartService.Setup(p => p.RegisterOrRemove<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((flightReservationResponse, duration)));
            _shoppingCartService.Setup(p => p.GetShoppingCartInfo<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);
            _travelerUtility.Setup(p => p.IsEnableNavigation(It.IsAny<bool>())).Returns(ok);
            _travelerUtility.Setup(p => p.IncludeFFCResidual(It.IsAny<int>(), It.IsAny<string>())).Returns(!ok);
            _travelerUtility.Setup(p => p.IsFFCTravelerChanged(It.IsAny<List<MOBCPTraveler>>(), It.IsAny<SerializableDictionary<string, MOBCPTraveler>>())).Returns(!ok);
            _travelerUtility.Setup(p => p.IsETCTravelerChanged(It.IsAny<List<MOBCPTraveler>>(), It.IsAny<SerializableDictionary<string, MOBCPTraveler>>(), It.IsAny<List<MOBFOPCertificate>>())).Returns(ok);
            if (request.MileagePlusNumber == "11")
                _configuration["BugFixToggleFor17M"] = "false";
            var ApplyPromoCodeResponse = JsonConvert.DeserializeObject<ApplyPromoCodeResponse>(GetFileContent("ApplyPromoCodeResponse.json"));
            _sessionHelperService.Setup(p => p.GetSession<ApplyPromoCodeResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(ApplyPromoCodeResponse);
            var ProdDetails = JsonConvert.DeserializeObject<List<ProdDetail>>(GetFileContent("ProdDetail.json"));
            _productInfoHelper.Setup(p => p.ConfirmationPageProductInfo(It.IsAny<FlightReservationResponse>(), It.IsAny<bool>(), It.IsAny<MOBApplication>(), It.IsAny<SeatChangeState>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(ProdDetails);
            //_travelerUtility.Setup(p => p.GetCommonSeatCode(It.IsAny<string>())).Returns(seat);
            _travelerUtility.Setup(p => p.EnableServicePlacePassBooking(It.IsAny<int>(), It.IsAny<string>())).Returns(true);
            _travelerUtility.Setup(p => p.EnablePlacePassBooking(It.IsAny<int>(), It.IsAny<string>())).Returns(true);
            _shoppingUtility.Setup(p => p.EnableIBEFull()).Returns(true);
            _shoppingCartService.Setup(p => p.GetRegisterTravelers<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);
           
           var registerTravelersRequest = JsonConvert.DeserializeObject<RegisterTravelersRequest>(GetFileContent("RegisterTravelersRequest.json"));
            _traveler.Setup(p => p.GetRegisterTravelerRequest(It.IsAny<MOBRegisterTravelersRequest>(), It.IsAny<bool>(), It.IsAny<Reservation>())).Returns(Task.FromResult((registerTravelersRequest,new MOBRegisterTravelersRequest() { })));
            _traveler.Setup(p => p.GetMPDetailsForSavedTravelers(It.IsAny<MOBRegisterTravelersRequest>())).ReturnsAsync(request.Travelers);
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);
            _travelerUtility.Setup(p => p.EnableConcurrCardPolicy(It.IsAny<bool>())).Returns(true);

            FlightReservationResponse flightReservationResponse1 = new FlightReservationResponse
            {
                CartId = "testcartid",
                DisplayCart = new DisplayCart
                {
                    CountryCode = "India",
                    DisplayPrices = new List<DisplayPrice>
                    {
                        new DisplayPrice
                        {
                            Amount=134,
                            ResidualAmount=5346,
                            Description="tesytdesc",
                            Type="MILESANDMONEY",
                            Currency="rupees",
                            Status="teststatis",
                            Waived=true
                        }
                    }
                },
                Status = new Services.FlightShopping.Common.StatusType { },
                Reservation = new Service.Presentation.ReservationModel.Reservation
                {
                    CdmAddressCheck = "testcheck"
                }
            };
            string cslresponse = JsonConvert.SerializeObject(flightReservationResponse1);

            _getProductsService.Setup(p => p.RegisterOffer(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(cslresponse);

            _traveler.Setup(p => p.GetRegisterTravelerRequest(It.IsAny<MOBRegisterTravelersRequest>(), It.IsAny<bool>(), It.IsAny<Reservation>())).Returns(Task.FromResult((registerTravelerRequest, new MOBRegisterTravelersRequest() { })));

            if (request.ProfileId == 222)
            {
                _sessionHelperService.Setup(p => p.GetSession<ApplyPromoCodeResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(ApplyPromoCodeResponse);
            }

            var _travelerBusiness = new TravelerBusiness(_logger.Object, _configuration, _sessionHelperService.Object, _shoppingUtility.Object, _travelerUtility.Object, _fFCShoppingcs.Object, _traveler.Object, _getProductsService.Object, _shoppingCartService.Object, _shoppingSessionHelper.Object, _moqcachingService1, _moqdPService.Object, _moqpKDispenserService.Object, _productInfoHelper.Object, _formsOfPayment.Object, _paymentService.Object, _mockShoppingBuyMiles.Object, _omniCart.Object,_dynamoDBService, _cSLStatisticsService, _paymentUtility.Object, _shopping.Object, _headers.Object, _mlogger.Object, _featureSettings.Object, _featureToggles.Object);

            var result = _travelerBusiness.RegisterTravelers_CFOP(request);

            if (result.Exception == null)
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(Input.TravelerInput1), MemberType = typeof(Input))]
        public void RegisterTravelers_CFOP_Test_Exception(MOBRegisterTravelersRequest request, MOBRegisterTravelersResponse response, Model.Shopping.Reservation reservation, Session session, bool ok, bool val, FlightReservationResponse flightReservationResponse, MOBShoppingCart shoppingCart, string seat , RegisterTravelersRequest registerTravelerRequest)
        {
            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);
            if (request.ProfileId != 111)
            {
                _sessionHelperService.Setup(_ => _.GetSession<Model.Shopping.Reservation>(It.IsAny<string>()
                         , It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);
            }
            _shoppingUtility.Setup(p => p.EnableTravelerTypes(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(ok);
            _travelerUtility.Setup(p => p.EnableYoungAdultValidation(It.IsAny<bool>())).Returns(ok);
            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(response.Reservation);
            var profileResponse = JsonConvert.DeserializeObject<Model.Shopping.ProfileResponse>(GetFileContent("ProfileResponse.json"));
            _travelerUtility.Setup(p => p.GetCSLProfileResponseInSession(It.IsAny<string>())).ReturnsAsync(profileResponse);
            _travelerUtility.Setup(p => p.AssignInfantWithSeat(It.IsAny<Model.Shopping.Reservation>(), It.IsAny<List<MOBCPTraveler>>())).Returns(request.Travelers);
            _travelerUtility.Setup(p => p.AssignInfantInLap(It.IsAny<Model.Shopping.Reservation>(), It.IsAny<List<MOBCPTraveler>>())).Returns(request.Travelers);
            //_shoppingUtility.Setup(p => p.GetTTypeValue(It.IsAny<int>(), It.IsAny<bool>()));
            _travelerUtility.Setup(p => p.GetChildInLapCount(It.IsAny<MOBCPTraveler>(), It.IsAny<List<int>>(), It.IsAny<int>(), It.IsAny<string>())).Returns(1);
            _shoppingUtility.Setup(p => p.EnableTravelerTypes(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(ok);
            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(response.Reservation);
            _travelerUtility.Setup(p => p.GetCSLProfileResponseInSession(It.IsAny<string>())).ReturnsAsync(profileResponse);
            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ThrowsAsync(new MOBUnitedException("Error Occurred"));
            _shoppingUtility.Setup(p => p.IsETCEnabledforMultiTraveler(It.IsAny<int>(), It.IsAny<string>())).Returns(true);
            _shoppingUtility.Setup(p => p.IsETCCombinabilityEnabled(It.IsAny<int>(), It.IsAny<string>())).Returns(val);
            _traveler.Setup(p => p.RegisterTravelers_CFOP(It.IsAny<MOBRegisterTravelersRequest>(), It.IsAny<bool>())).ReturnsAsync(response.Reservation);
            _shoppingUtility.Setup(p => p.EnableReshopCubaTravelReasonVersion(It.IsAny<int>(), It.IsAny<string>())).Returns(ok);
            _travelerUtility.Setup(p => p.EnableTPI(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>())).Returns(ok);
            _shoppingUtility.Setup(p => p.IncludeMoneyPlusMiles(It.IsAny<int>(), It.IsAny<string>())).Returns(ok);
            _travelerUtility.Setup(p => p.EnableTravelerTypes(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(ok);
            _travelerUtility.Setup(p => p.IsETCEnabledforMultiTraveler(It.IsAny<int>(), It.IsAny<string>())).Returns(ok);
            _travelerUtility.Setup(p => p.IsETCCombinabilityEnabled(It.IsAny<int>(), It.IsAny<string>())).Returns(val);
            _travelerUtility.Setup(p => p.IncludeMoneyPlusMiles(It.IsAny<int>(), It.IsAny<string>())).Returns(ok);
            var loadResponse = JsonConvert.DeserializeObject<LoadReservationAndDisplayCartResponse>(GetFileContent("LoadReservationAndDisplayCartResponse.json"));
            _travelerUtility.Setup(p => p.GetCartInformation(It.IsAny<string>(), It.IsAny<MOBApplication>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Service.Presentation.CommonEnumModel.WorkFlowType>())).ReturnsAsync(loadResponse);
            _formsOfPayment.Setup(p => p.GetEligibleFormofPayments(It.IsAny<MOBRequest>(), It.IsAny<Session>(), It.IsAny<MOBShoppingCart>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<MOBSHOPReservation>(), It.IsAny<bool>(), It.IsAny<SeatChangeState>())).Returns(Task.FromResult((response.EligibleFormofPayments, false)));
            _sessionHelperService.Setup(_ => _.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingCart);
            var messagesList = JsonConvert.DeserializeObject<List<CMSContentMessage>>(GetFileContent("CMSContentMessage.json"));
            var messagesList1 = JsonConvert.DeserializeObject<List<MOBMobileCMSContentMessages>>(GetFileContent("CMSContentMessage.json"));
            //_shoppingUtility.Setup(p => p.GetSDLMessageFromList(It.IsAny<List<CMSContentMessage>>(), It.IsAny<string>())).Returns(messagesList1);
            _fFCShoppingcs.Setup(p => p.GetSDLContentByGroupName(It.IsAny<MOBRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(messagesList);
            // _pKDispenserPublicKey.Setup(p => p.GetCachedOrNewpkDispenserPublicKey(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(response.Reservation.PKDispenserPublicKey);
            _getProductsService.Setup(p => p.MilesAndMoneyOption<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);
            long duration = 9223372036854775807;
            _shoppingCartService.Setup(p => p.RegisterOrRemove<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((flightReservationResponse, duration)));
            _shoppingCartService.Setup(p => p.GetShoppingCartInfo<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);
            _travelerUtility.Setup(p => p.IsEnableNavigation(It.IsAny<bool>())).Returns(ok);
            _travelerUtility.Setup(p => p.IncludeFFCResidual(It.IsAny<int>(), It.IsAny<string>())).Returns(!ok);
            _travelerUtility.Setup(p => p.IsFFCTravelerChanged(It.IsAny<List<MOBCPTraveler>>(), It.IsAny<SerializableDictionary<string, MOBCPTraveler>>())).Returns(!ok);
            _travelerUtility.Setup(p => p.IsETCTravelerChanged(It.IsAny<List<MOBCPTraveler>>(), It.IsAny<SerializableDictionary<string, MOBCPTraveler>>(), It.IsAny<List<MOBFOPCertificate>>())).Returns(ok);
            if (request.MileagePlusNumber == "11")
                _configuration["BugFixToggleFor17M"] = "false";
            var ApplyPromoCodeResponse = JsonConvert.DeserializeObject<ApplyPromoCodeResponse>(GetFileContent("ApplyPromoCodeResponse.json"));
            var ProdDetails = JsonConvert.DeserializeObject<List<ProdDetail>>(GetFileContent("ProdDetail.json"));
            _productInfoHelper.Setup(p => p.ConfirmationPageProductInfo(It.IsAny<FlightReservationResponse>(), It.IsAny<bool>(), It.IsAny<MOBApplication>(), It.IsAny<SeatChangeState>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(ProdDetails);
            //_travelerUtility.Setup(p => p.GetCommonSeatCode(It.IsAny<string>())).Returns(seat);

            _traveler.Setup(p => p.GetRegisterTravelerRequest(It.IsAny<MOBRegisterTravelersRequest>(), It.IsAny<bool>(), It.IsAny<Reservation>())).Returns(Task.FromResult((registerTravelerRequest, new MOBRegisterTravelersRequest() { })));

            if (request.ProfileId == 222)
            {
                _sessionHelperService.Setup(p => p.GetSession<ApplyPromoCodeResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(ApplyPromoCodeResponse);
            }

            var travelerBusiness = new TravelerBusiness(_logger.Object, _configuration, _sessionHelperService.Object, _shoppingUtility.Object, _travelerUtility.Object, _fFCShoppingcs.Object, _traveler.Object, _getProductsService.Object, _shoppingCartService.Object, _shoppingSessionHelper.Object, _moqcachingService1, _moqdPService.Object, _moqpKDispenserService.Object, _productInfoHelper.Object, _formsOfPayment.Object, _paymentService.Object, _mockShoppingBuyMiles.Object, _omniCart.Object, _dynamoDBService, _cSLStatisticsService, _paymentUtility.Object, _shopping.Object, _headers.Object, _mlogger.Object, _featureSettings.Object, _featureToggles.Object);

          var result = travelerBusiness.RegisterTravelers_CFOP(request);

            if (result.Exception == null)
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(Input.TravelerInput1), MemberType = typeof(Input))]
        public void RegisterTravelers_CFOP_Test_Exception1(MOBRegisterTravelersRequest request, MOBRegisterTravelersResponse response, Model.Shopping.Reservation reservation, Session session, bool ok, bool val, FlightReservationResponse flightReservationResponse, MOBShoppingCart shoppingCart, string seat , RegisterTravelersRequest registerTravelersRequest, FOPResponse fOPResponse)
        {
            var _dataPowerFactory = new DataPowerFactory(_configuration, _sessionHelperService.Object, _logger8.Object);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);
            if (request.ProfileId != 111)
            {
                _sessionHelperService.Setup(_ => _.GetSession<Model.Shopping.Reservation>(It.IsAny<string>()
                         , It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);
            }
            _shoppingUtility.Setup(p => p.EnableTravelerTypes(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(ok);
            _travelerUtility.Setup(p => p.EnableYoungAdultValidation(It.IsAny<bool>())).Returns(ok);
            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(response.Reservation);
            var profileResponse = JsonConvert.DeserializeObject<Model.Shopping.ProfileResponse>(GetFileContent("ProfileResponse.json"));
            _travelerUtility.Setup(p => p.GetCSLProfileResponseInSession(It.IsAny<string>())).ReturnsAsync(profileResponse);
            _travelerUtility.Setup(p => p.AssignInfantWithSeat(It.IsAny<Model.Shopping.Reservation>(), It.IsAny<List<MOBCPTraveler>>())).Returns(request.Travelers);
            _travelerUtility.Setup(p => p.AssignInfantInLap(It.IsAny<Model.Shopping.Reservation>(), It.IsAny<List<MOBCPTraveler>>())).Returns(request.Travelers);
            //_shoppingUtility.Setup(p => p.GetTTypeValue(It.IsAny<int>(), It.IsAny<bool>()));
            _travelerUtility.Setup(p => p.GetChildInLapCount(It.IsAny<MOBCPTraveler>(), It.IsAny<List<int>>(), It.IsAny<int>(), It.IsAny<string>())).Returns(1);
            _shoppingUtility.Setup(p => p.EnableTravelerTypes(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(ok);
            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(response.Reservation);
            _travelerUtility.Setup(p => p.GetCSLProfileResponseInSession(It.IsAny<string>())).ReturnsAsync(profileResponse);
            //_sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<HttpContextValues>()
            //           , It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ThrowsAsync(new MOBUnitedException("Error Occurred"));
            _shoppingUtility.Setup(p => p.IsETCEnabledforMultiTraveler(It.IsAny<int>(), It.IsAny<string>())).Returns(true);
            _shoppingUtility.Setup(p => p.IsETCCombinabilityEnabled(It.IsAny<int>(), It.IsAny<string>())).Returns(val);
            _traveler.Setup(p => p.RegisterTravelers_CFOP(It.IsAny<MOBRegisterTravelersRequest>(), It.IsAny<bool>())).ReturnsAsync(response.Reservation);
            _shoppingUtility.Setup(p => p.EnableReshopCubaTravelReasonVersion(It.IsAny<int>(), It.IsAny<string>())).Returns(ok);
            _travelerUtility.Setup(p => p.EnableTPI(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>())).Returns(ok);
            _shoppingUtility.Setup(p => p.IncludeMoneyPlusMiles(It.IsAny<int>(), It.IsAny<string>())).Returns(ok);
            _travelerUtility.Setup(p => p.EnableTravelerTypes(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(ok);
            _travelerUtility.Setup(p => p.IsETCEnabledforMultiTraveler(It.IsAny<int>(), It.IsAny<string>())).Returns(ok);
            _travelerUtility.Setup(p => p.IsETCCombinabilityEnabled(It.IsAny<int>(), It.IsAny<string>())).Returns(val);
            _travelerUtility.Setup(p => p.IncludeMoneyPlusMiles(It.IsAny<int>(), It.IsAny<string>())).Returns(ok);
            var loadResponse = JsonConvert.DeserializeObject<LoadReservationAndDisplayCartResponse>(GetFileContent("LoadReservationAndDisplayCartResponse.json"));
            _travelerUtility.Setup(p => p.GetCartInformation(It.IsAny<string>(), It.IsAny<MOBApplication>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Service.Presentation.CommonEnumModel.WorkFlowType>())).ReturnsAsync(loadResponse);
            _formsOfPayment.Setup(p => p.GetEligibleFormofPayments(It.IsAny<MOBRequest>(), It.IsAny<Session>(), It.IsAny<MOBShoppingCart>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<MOBSHOPReservation>(), It.IsAny<bool>(), It.IsAny<SeatChangeState>())).Returns(Task.FromResult((response.EligibleFormofPayments,false)));
            _sessionHelperService.Setup(_ => _.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingCart);
            var messagesList = JsonConvert.DeserializeObject<List<CMSContentMessage>>(GetFileContent("CMSContentMessage.json"));
            var messagesList1 = JsonConvert.DeserializeObject<List<MOBMobileCMSContentMessages>>(GetFileContent("CMSContentMessage.json"));
            // _shoppingUtility.Setup(p => p.GetSDLMessageFromList(It.IsAny<List<CMSContentMessage>>(), It.IsAny<string>())).Returns(messagesList1);
            _fFCShoppingcs.Setup(p => p.GetSDLContentByGroupName(It.IsAny<MOBRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(messagesList);
            // _pKDispenserPublicKey.Setup(p => p.GetCachedOrNewpkDispenserPublicKey(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(response.Reservation.PKDispenserPublicKey);
            _getProductsService.Setup(p => p.MilesAndMoneyOption<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);
            long duration = 9223372036854775807;
            _shoppingCartService.Setup(p => p.RegisterOrRemove<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((flightReservationResponse, duration)));
            _shoppingUtility.Setup(p => p.GetGrandTotalPriceForShoppingCart(It.IsAny<bool>(), It.IsAny<FlightReservationResponse>(), It.IsAny<bool>(), It.IsAny<string>())).Returns(22.8765431);
            _shoppingCartService.Setup(p => p.GetShoppingCartInfo<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);
            _travelerUtility.Setup(p => p.IsEnableNavigation(It.IsAny<bool>())).Returns(ok);
            _travelerUtility.Setup(p => p.IncludeFFCResidual(It.IsAny<int>(), It.IsAny<string>())).Returns(!ok);
            _travelerUtility.Setup(p => p.IsFFCTravelerChanged(It.IsAny<List<MOBCPTraveler>>(), It.IsAny<SerializableDictionary<string, MOBCPTraveler>>())).Returns(!ok);
            _travelerUtility.Setup(p => p.IsETCTravelerChanged(It.IsAny<List<MOBCPTraveler>>(), It.IsAny<SerializableDictionary<string, MOBCPTraveler>>(), It.IsAny<List<MOBFOPCertificate>>())).Returns(ok);
            if (request.MileagePlusNumber == "11")
                _configuration["BugFixToggleFor17M"] = "false";
            var ApplyPromoCodeResponse = JsonConvert.DeserializeObject<ApplyPromoCodeResponse>(GetFileContent("ApplyPromoCodeResponse.json"));
            var ProdDetails = JsonConvert.DeserializeObject<List<ProdDetail>>(GetFileContent("ProdDetail.json"));
            _productInfoHelper.Setup(p => p.ConfirmationPageProductInfo(It.IsAny<FlightReservationResponse>(), It.IsAny<bool>(), It.IsAny<MOBApplication>(), It.IsAny<SeatChangeState>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(ProdDetails);
            // _travelerUtility.Setup(p => p.GetCommonSeatCode(It.IsAny<string>())).Returns(seat);
            if (request.ProfileId == 222)
            {
                _sessionHelperService.Setup(p => p.GetSession<ApplyPromoCodeResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(ApplyPromoCodeResponse);
            }
            _shoppingUtility.Setup(p => p.EnableIBEFull()).Returns(true);
            _shoppingCartService.Setup(p => p.GetRegisterTravelers<FlightReservationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);
           // var registerTravelersRequest = JsonConvert.DeserializeObject<RegisterTravelersRequest>(GetFileContent("RegisterTravelersRequest.json"));
            _traveler.Setup(p => p.GetRegisterTravelerRequest(It.IsAny<MOBRegisterTravelersRequest>(), It.IsAny<bool>(), It.IsAny<Reservation>())).Returns(Task.FromResult((registerTravelersRequest, request)));

            _traveler.Setup(p => p.GetMPDetailsForSavedTravelers(It.IsAny<MOBRegisterTravelersRequest>())).ReturnsAsync(request.Travelers);
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);
            _travelerUtility.Setup(p => p.EnableConcurrCardPolicy(It.IsAny<bool>())).Returns(true);

            _shoppingUtility.Setup(p => p.ReservationToShoppingCart_DataMigration(It.IsAny<MOBSHOPReservation>(), It.IsAny<MOBShoppingCart>(), It.IsAny<MOBRequest>(), It.IsAny<Session>())).ReturnsAsync(shoppingCart);

            _travelerUtility.Setup(p => p.LoadBasicFOPResponse(It.IsAny<Session>(), It.IsAny<Reservation>())).Returns(Task.FromResult((fOPResponse, reservation)));

            var travelerBusiness = new TravelerBusiness(_logger.Object, _configuration, _sessionHelperService.Object, _shoppingUtility.Object, _travelerUtility.Object, _fFCShoppingcs.Object, _traveler.Object, _getProductsService.Object, _shoppingCartService.Object, _shoppingSessionHelper.Object, _moqcachingService1, _moqdPService.Object, _moqpKDispenserService.Object, _productInfoHelper.Object, _formsOfPayment.Object, _paymentService.Object, _mockShoppingBuyMiles.Object, _omniCart.Object, _dynamoDBService, _cSLStatisticsService, _paymentUtility.Object, _shopping.Object, _headers.Object, _mlogger.Object, _featureSettings.Object, _featureToggles.Object);


            var result = travelerBusiness.RegisterTravelers_CFOP(request);

            if (result.Exception == null)
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }


    }
}
