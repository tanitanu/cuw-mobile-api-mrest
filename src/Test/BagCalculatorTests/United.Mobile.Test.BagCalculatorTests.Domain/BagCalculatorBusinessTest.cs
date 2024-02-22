using MerchandizingServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Definition;
using United.Ebs.Logging.Enrichers;
using United.Mobile.BagCalculator.Domain;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.FlightStatus;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.DataAccess.ShopProducts;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.Model;
using United.Mobile.Model.BagCalculator;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Test.BagCalculatorTests.Domain;
using United.Services.FlightShopping.Common.Cart;
using United.Utility.Helper;
using United.Utility.Http;
using Xunit;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Test.BagCalculatorTest.Domain
{
    public class BagCalculatorBusinessTest
    {
        private readonly IConfiguration _configuration;
        private readonly IConfiguration _configurationIBELite;
        private readonly IConfiguration _configurationEnableIBE;
        private readonly IConfiguration _configurationEnabledMERCHChannels;
        private IConfiguration _configurationSurfaceToClient;
        private readonly IConfiguration _configurationSessionExpiryMessageChange;
        private readonly ICacheLog<BagCalculatorBusiness> _logger;
        private readonly Mock<ICacheLog<BagCalculatorBusiness>> _moqlogger;
        private Mock<IHttpContextAccessor> _moqhttpContextAccessor;
        private HttpContextAccessor _httpContextAccessor;
        private readonly Mock<IRequestEnricher> _requestEnricher;
        private readonly Mock<IDPService> _moqtokenService;
        private readonly Mock<IResilientClient> _mockResilientClient;
        private readonly Mock<ICachingService> _mockCachingService;
        private Mock<BagCalculatorBusiness> _moqbagCalculatorBusiness;
        private IDPService _tokenService;
        private Mock<ISessionHelperService> _sessionHelperService;
        private Mock<IFlightShoppingService> _flightShoppingService;
        private readonly IPNRRetrievalService _pNRRetrievalService;
        private readonly Mock<IFLIFOTokenService> _moqfLIFOTokenService;
        private readonly Mock<IPNRRetrievalService> _moqpNRRetrievalService;
        private readonly ICMSContentHelper _cMSContentHelper;
        private readonly Mock<ICMSContentHelper> _moqCMSContentHelper;
        private readonly Mock<IDynamoDBService> _moqdynamoDBService;
        private readonly Mock<IHeaders> _moqHeader;
        private readonly Mock<IMerchOffersService> _moqMerchOffersService;
        //private readonly Mock<IShoppingUtility> _moqShoppingUtility;
        private readonly IAirlineCarrierService _airlineCarrierService;
        private readonly Mock<ILegalDocumentsForTitlesService> _legalDocumentsForTitlesService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly Mock<IShoppingCcePromoService> _moqShoppingCcePromoService;
        private readonly Mock<ICSLStatisticsService> _cSLStatisticsService;
        private readonly Mock<IShoppingCartService> _cSLShoppingCartService;
        private readonly Mock<IValidateHashPinService> _validateHashPinService;
        private readonly Mock<ICachingService> _cachingService;
        private readonly Mock<IOptimizelyPersistService> _optimizelyPersistService;
        private readonly Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly Mock<IFeatureSettings> _featureSettings;
        private readonly Mock<ICCEDynamicOffersService> _cceService;
        private readonly Mock<ICCEDynamicOfferDetailsService> _cceDODService;
        // private readonly Mock<IHeaders> _headers;


        public BagCalculatorBusinessTest()
        {
            _configuration = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile($"appSettings.test.json", optional: false, reloadOnChange: true)
             .Build();
            _configurationIBELite = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile($"appSettings.EnableIBELite.json", optional: false, reloadOnChange: true)
             .Build();
            _configurationEnableIBE = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile($"appSettings.EnableIBE.json", optional: false, reloadOnChange: true)
             .Build();
            _configurationEnabledMERCHChannels = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile($"appSettings.EnabledMERCHChannels.json", optional: false, reloadOnChange: true)
             .Build();

            _configurationSessionExpiryMessageChange = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile($"appSettings.SessionExpiryMessageChange.json", optional: false, reloadOnChange: true)
             .Build();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
                    .AddConsole()
                    .AddEventLog();
            });

            //_logger = loggerFactory.CreateLogger<BagCalculatorBusiness>();
            _moqlogger = new Mock<ICacheLog<BagCalculatorBusiness>>();
            _requestEnricher = new Mock<IRequestEnricher>();
            _httpContextAccessor = new HttpContextAccessor();
            _moqtokenService = new Mock<IDPService>();
            _mockResilientClient = new Mock<IResilientClient>();
            _mockCachingService = new Mock<ICachingService>();
            _moqbagCalculatorBusiness = new Mock<BagCalculatorBusiness>();
            _moqhttpContextAccessor = new Mock<IHttpContextAccessor>();
            _flightShoppingService = new Mock<IFlightShoppingService>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _moqCMSContentHelper = new Mock<ICMSContentHelper>();
            _moqfLIFOTokenService = new Mock<IFLIFOTokenService>();
            _moqpNRRetrievalService = new Mock<IPNRRetrievalService>();
            _moqHeader = new Mock<IHeaders>();
            _legalDocumentsForTitlesService = new Mock<ILegalDocumentsForTitlesService>();
            _moqMerchOffersService = new Mock<IMerchOffersService>();
            _moqdynamoDBService = new Mock<IDynamoDBService>();
            _cSLStatisticsService = new Mock<ICSLStatisticsService>();
            _cachingService = new Mock<ICachingService>();
            _cSLShoppingCartService = new Mock<IShoppingCartService>();
            _validateHashPinService = new Mock<IValidateHashPinService>();
            _optimizelyPersistService = new Mock<IOptimizelyPersistService>();
            _moqShoppingCcePromoService = new Mock<IShoppingCcePromoService>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _cceService = new Mock<ICCEDynamicOffersService>();
            _cceDODService = new Mock<ICCEDynamicOfferDetailsService>();
            //_headers = new Mock<IHeaders>();
            //_moqShoppingUtility = new Mock<IShoppingUtility>();
            SetHeaders();
        }

        private void SetHeaders(string deviceId = "589d7852-14e7-44a9-b23b-a6db36657579"
            , string applicationId = "2"
            , string appVersion = "4.1.29"
            , string transactionId = "589d7852-14e7-44a9-b23b-a6db36657579|8f46e040-a200-495c-83ca-4fca2d7175fb"
            , string languageCode = "en-US"
            , string sessionId = "17C979E184CC495EA083D45F4DD9D19D")
        {
            _moqHeader.Setup(_ => _.ContextValues).Returns(
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
        [InlineData("en-US", "4.1.23", 2)]
        [InlineData("en-US", "4.1.00", 2)]
        [InlineData("en-EU", "4.1.23", 2)]
        [InlineData("en-US", "4.1.23", 1)]
        [InlineData("en-US", "4.1.00", 1)]
        [InlineData("en-EU", "4.1.23", 1)]
        [InlineData("en-US", "4.1.23", 3)]
        public void GetBaggageCalculatorSearch_PositiveTest(string languageCode, string appVersion, int applicationId)
        {
            string accessCode = "ACCESSCODE";
            string transactionId = "19cb3998-94b0-4383-8999-60fde39065bb|d0e3d749-d958-4241-936f-575d45a20482";
            SetHeaders("19cb3998-94b0-4383-8999-60fde39065bb", applicationId.ToString(), appVersion, transactionId, languageCode, "d0e3d749-d958-4241-936f-575d45a20482");

            var carriersData = BagCalculatorInput.GetFileContent(@"SampleData\Carriers.json");
            var carriersList = JsonConvert.DeserializeObject<List<Model.BagCalculator.CarrierInfo>>(carriersData);
            _moqdynamoDBService.Setup(_ => _.GetRecords<List<Model.BagCalculator.CarrierInfo>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(carriersList);

            var bagCalculatorBusiness = new BagCalculatorBusiness(_logger, _configuration, _moqHeader.Object, _flightShoppingService.Object
                , _sessionHelperService.Object, _tokenService, _pNRRetrievalService, _cMSContentHelper
                , _moqMerchOffersService.Object, _moqdynamoDBService.Object, null, _airlineCarrierService, _legalDocumentsForTitlesService.Object, _shoppingUtility,
                _cachingService.Object,_moqShoppingCcePromoService.Object, _cSLStatisticsService.Object, _cSLShoppingCartService.Object, _validateHashPinService.Object
                , _optimizelyPersistService.Object, _featureSettings.Object, _cceService.Object, _cceDODService.Object);

            var response = bagCalculatorBusiness.GetBaggageCalculatorSearch(accessCode, transactionId, languageCode, appVersion, applicationId);

            Assert.NotNull(response.Result.ClassOfServices);
            Assert.NotNull(response.Result.Carriers);
            Assert.NotNull(response.Result.LoyaltyLevels);
        }

        [Theory]
        [InlineData("en-US", "4.1.23", 2)]
        [InlineData("en-US", "4.1.23", 1)]
        [InlineData("", "", 0)]
        public void GetBaggageCalculatorSearch_Exceptions(string languageCode, string appVersion, int applicationId)
        {
            string accessCode = "AccessCode_Test";
            string transactionId = "19cb3998-94b0-4383-8999-60fde39065bb|d0e3d749-d958-4241-936f-575d45a20482";
            SetHeaders("19cb3998-94b0-4383-8999-60fde39065bb", applicationId.ToString(), appVersion, transactionId, languageCode, "d0e3d749-d958-4241-936f-575d45a20482");

            var bagCalculatorBusiness = new BagCalculatorBusiness(_logger, _configuration, _moqHeader.Object, _flightShoppingService.Object
                , _sessionHelperService.Object, _tokenService, _pNRRetrievalService, _cMSContentHelper
                , _moqMerchOffersService.Object, _moqdynamoDBService.Object, null, _airlineCarrierService, _legalDocumentsForTitlesService.Object, 
                _shoppingUtility, _cachingService.Object,_moqShoppingCcePromoService.Object, _cSLStatisticsService.Object, _cSLShoppingCartService.Object, 
                _validateHashPinService.Object, _optimizelyPersistService.Object, _featureSettings.Object, _cceService.Object, _cceDODService.Object);

            var response = bagCalculatorBusiness.GetBaggageCalculatorSearch(accessCode, transactionId, languageCode, appVersion, applicationId);
            var ex = Assert.ThrowsAsync<MOBUnitedException>(() => bagCalculatorBusiness.GetBaggageCalculatorSearch(accessCode, transactionId, languageCode, appVersion, applicationId));

            Assert.True(ex.Result.Message == "The access code you provided is not valid.");
        }
     
        private string GetFileContent(string fileName)
        {
            var path = Path.IsPathRooted(fileName) ? fileName
                                                : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }

        [Theory]
        [MemberData(nameof(BagCalculatorInput.MobileCMSContent), MemberType = typeof(BagCalculatorInput))]
        public async Task<bool> GetMobileCMSContentsData_SurfaceErrorToClientTrue_GetShopTnCFalse(Session sessionObj
            , Reservation reservationObj
            , MobileCMSContentRequest mobileCMSContentRequest
            , MobileCMSContentResponse mobileCMSContentResponse
            , List<MobileCMSContentMessages> mobileCMSContentMessages)

        {
            _configurationSurfaceToClient = new ConfigurationBuilder()
                   .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile($"appSettings.SurfaceToClient.json", optional: false, reloadOnChange: true)
             .Build();
            sessionObj.TokenExpireDateTime = DateTime.Now.AddMinutes(30);

            _moqCMSContentHelper.Setup(p => p.GetMobileCMSContents(It.IsAny<MobileCMSContentRequest>())).ReturnsAsync(mobileCMSContentMessages);


            _sessionHelperService.SetupSequence(_ => _.GetSession<Session>(It.IsAny<string>()
                          , It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(sessionObj);

            _sessionHelperService.Setup(_ => _.GetSession<Reservation>(It.IsAny<string>()
                          , It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationObj);

            MobileCMSContentMessages sms = new MobileCMSContentMessages() { ContentFull = "" };
            List<MobileCMSContentMessages> MobileCMSContentMessages = new List<MobileCMSContentMessages>() { };
            MobileCMSContentMessages.Add(sms);

            _moqCMSContentHelper.Setup(_ => _.GetMobileTermsAndConditions(It.IsAny<MobileCMSContentRequest>())).ReturnsAsync(MobileCMSContentMessages);
            //_cMSContentHelper.Setup(_=> _.GetMobileTermsAndConditions(It.IsAny<MobileCMSContentRequest>())).ReturnsAsync(MobileCMSContentsRespone.MobileCMSContentMessages);

              _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(sessionObj);

            BagCalculatorBusiness bagCalculatorBusiness = new BagCalculatorBusiness(_logger, _configurationSurfaceToClient, _moqHeader.Object, 
                _flightShoppingService.Object, _sessionHelperService.Object, _moqtokenService.Object, _moqpNRRetrievalService.Object, _moqCMSContentHelper.Object, 
                _moqMerchOffersService.Object, _moqdynamoDBService.Object, _shoppingSessionHelper.Object, _airlineCarrierService, 
                _legalDocumentsForTitlesService.Object, _shoppingUtility, _cachingService.Object, _moqShoppingCcePromoService.Object, 
                _cSLStatisticsService.Object, _cSLShoppingCartService.Object, _validateHashPinService.Object, _optimizelyPersistService.Object, 
                _featureSettings.Object, _cceService.Object, _cceDODService.Object);


            var result = bagCalculatorBusiness.GetMobileCMSContentsData(mobileCMSContentRequest);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
            return true;
        }

        [Theory]
        [MemberData(nameof(BagCalculatorInput.MobileCMSContent), MemberType = typeof(BagCalculatorInput))]
        public void GetMobileCMSContentsData_SessionExpiryMessageFalse_GetShoppingSession(Session sessionObj
            , Reservation reservationObj
            , MobileCMSContentRequest mobileCMSContentRequest
            , MobileCMSContentResponse mobileCMSContentResponse
            , List<MobileCMSContentMessages> mobileCMSContentMessages)
        {
            sessionObj.TokenExpireDateTime = DateTime.Now.AddMinutes(30);

            _moqCMSContentHelper.Setup(p => p.GetMobileCMSContents(It.IsAny<MobileCMSContentRequest>())).ReturnsAsync(mobileCMSContentMessages);
            //_moqshoppingHelper.Setup(_ => _.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync(sessionObj);
            _sessionHelperService.SetupSequence(_ => _.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(sessionObj);

            _sessionHelperService.Setup(_ => _.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationObj);
            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(sessionObj);


            BagCalculatorBusiness bagCalculatorBusiness = new BagCalculatorBusiness(_logger, _configurationSessionExpiryMessageChange, _moqHeader.Object, 
                _flightShoppingService.Object, _sessionHelperService.Object, _moqtokenService.Object, _moqpNRRetrievalService.Object, _moqCMSContentHelper.Object,
                _moqMerchOffersService.Object, _moqdynamoDBService.Object, _shoppingSessionHelper.Object, _airlineCarrierService, 
                _legalDocumentsForTitlesService.Object, _shoppingUtility, _cachingService.Object, _moqShoppingCcePromoService.Object, _cSLStatisticsService.Object, 
                _cSLShoppingCartService.Object, _validateHashPinService.Object, _optimizelyPersistService.Object, _featureSettings.Object, _cceService.Object, _cceDODService.Object);

            var result = bagCalculatorBusiness.GetMobileCMSContentsData(mobileCMSContentRequest);
            Assert.True(result != null);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(BagCalculatorInput.MobileCMSContent_TnC), MemberType = typeof(BagCalculatorInput))]
        public async Task<bool> GetMobileCMSContentsData_GetShopTnCTrue(Session sessionObj
            , Reservation reservationObj
            , MobileCMSContentRequest mobileCMSContentRequest
            , MobileCMSContentResponse mobileCMSContentResponse
            , List<MobileCMSContentMessages> mobileCMSContentMessages)
        {
            sessionObj.TokenExpireDateTime = DateTime.Now.AddMinutes(30);

            _moqCMSContentHelper.Setup(p => p.GetMobileCMSContents(It.IsAny<MobileCMSContentRequest>())).ReturnsAsync(mobileCMSContentMessages);
            _moqCMSContentHelper.Setup(p => p.GetMobileTermsAndConditions(It.IsAny<MobileCMSContentRequest>())).ReturnsAsync(mobileCMSContentMessages);

            //_moqshoppingHelper.Setup(_ => _.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync(sessionObj);
            _sessionHelperService.SetupSequence(_ => _.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(sessionObj);

            _sessionHelperService.Setup(_ => _.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationObj);


            BagCalculatorBusiness bagCalculatorBusiness = new BagCalculatorBusiness(_logger, _configuration, _moqHeader.Object, _flightShoppingService.Object, 
                _sessionHelperService.Object, _moqtokenService.Object, _moqpNRRetrievalService.Object, _moqCMSContentHelper.Object, _moqMerchOffersService.Object, 
                _moqdynamoDBService.Object, null, _airlineCarrierService, _legalDocumentsForTitlesService.Object, _shoppingUtility, _cachingService.Object, 
                _moqShoppingCcePromoService.Object, _cSLStatisticsService.Object, _cSLShoppingCartService.Object, _validateHashPinService.Object, 
                _optimizelyPersistService.Object, _featureSettings.Object, _cceService.Object, _cceDODService.Object);

            var result = await bagCalculatorBusiness.GetMobileCMSContentsData(mobileCMSContentRequest);

            Assert.True(result != null);
            return true;
        }

        [Theory]
        [MemberData(nameof(BagCalculatorInput.MobileCMSContent), MemberType = typeof(BagCalculatorInput))]
        public void GetMobileCMSContentsData_Testing(Session sessionObj
            , Reservation reservationObj
            , MobileCMSContentRequest mobileCMSContentRequest
            , MobileCMSContentResponse mobileCMSContentResponse
            , List<MobileCMSContentMessages> mobileCMSContentMessages)
        {
            sessionObj.TokenExpireDateTime = DateTime.Now.AddMinutes(30);
            //_moqshoppingHelper.Setup(_ => _.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync(sessionObj);
            _sessionHelperService.SetupSequence(_ => _.GetSession<Session>(It.IsAny<string>()
                          , It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(sessionObj);

            _sessionHelperService.Setup(_ => _.GetSession<Reservation>(It.IsAny<string>()
                          , It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationObj);

            MobileCMSContentMessages sms = new MobileCMSContentMessages() { ContentFull = "" };
            List<MobileCMSContentMessages> MobileCMSContentMessages = new List<MobileCMSContentMessages>() { };
            MobileCMSContentMessages.Add(sms);

            _moqCMSContentHelper.Setup(_ => _.GetMobileTermsAndConditions(It.IsAny<MobileCMSContentRequest>())).ReturnsAsync(MobileCMSContentMessages);
            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(sessionObj);


            BagCalculatorBusiness bagCalculatorBusiness = new BagCalculatorBusiness(_logger, _configuration, _moqHeader.Object, _flightShoppingService.Object, 
                _sessionHelperService.Object, _moqtokenService.Object, _moqpNRRetrievalService.Object, _moqCMSContentHelper.Object, _moqMerchOffersService.Object, 
                _moqdynamoDBService.Object, _shoppingSessionHelper.Object, _airlineCarrierService, _legalDocumentsForTitlesService.Object, _shoppingUtility, 
                _cachingService.Object, _moqShoppingCcePromoService.Object, _cSLStatisticsService.Object, _cSLShoppingCartService.Object,
                _validateHashPinService.Object, _optimizelyPersistService.Object, _featureSettings.Object, _cceService.Object, _cceDODService.Object);


            var result = bagCalculatorBusiness.GetMobileCMSContentsData(mobileCMSContentRequest);
            //mobileCMSContentResponse);
             //Assert.False(result.Result.SessionId == mobileCMSContentRequest.SessionId);
           // Assert.False(result.Result.CartId == sessionObj.CartId);
           // Assert.False(result.Result.MileagePlusNumber == mobileCMSContentRequest.MileagePlusNumber);
            Assert.True(result.Result.Token != null);
            //Assert.False(result.Result.Exception == null);
            Assert.True(result != null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.MobileCMSContent_Request), MemberType = typeof(TestDataGenerator))]
        public void GetMobileCMSContentsData_Testing1(MobileCMSContentRequest mobileCMSContentRequest ,Session session)
        {
            session.TokenExpireDateTime = DateTime.Now.AddMinutes(30);


            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session); ;

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);
             
            BagCalculatorBusiness bagCalculatorBusiness = new BagCalculatorBusiness(_logger, _configuration, _moqHeader.Object, _flightShoppingService.Object, 
                _sessionHelperService.Object, _moqtokenService.Object, _moqpNRRetrievalService.Object, _moqCMSContentHelper.Object, _moqMerchOffersService.Object,
                _moqdynamoDBService.Object, _shoppingSessionHelper.Object, _airlineCarrierService, _legalDocumentsForTitlesService.Object, _shoppingUtility, 
                _cachingService.Object, _moqShoppingCcePromoService.Object, _cSLStatisticsService.Object, _cSLShoppingCartService.Object, 
                _validateHashPinService.Object, _optimizelyPersistService.Object, _featureSettings.Object, _cceService.Object, _cceDODService.Object);


            var result = bagCalculatorBusiness.GetMobileCMSContentsData(mobileCMSContentRequest);
            // mobileCMSContentResponse);
            Assert.True(result.Result.SessionId == mobileCMSContentRequest.SessionId);
            Assert.True(result.Result.CartId == session.CartId);
            Assert.True(result.Result.MileagePlusNumber == mobileCMSContentRequest.MileagePlusNumber);
            Assert.True(result.Result.Token != null);
            //Assert.False(result.Result.Exception == null);
            Assert.True(result != null);
        }

        [Theory]
        [MemberData(nameof(BagCalculatorInput.GetMobileCMSContentsData_Testingflow), MemberType = typeof(BagCalculatorInput))]
        public void GetMobileCMSContentsData_Testingflow(MobileCMSContentRequest mobileCMSContentRequest, MobileCMSContentResponse mobileCMSContentResponse, Session sessionObj, Reservation reservationObj, List<MobileCMSContentMessages> mobileCMSContentMessages)
        {

            sessionObj.TokenExpireDateTime = DateTime.Now.AddMinutes(30);
            //_moqshoppingHelper.Setup(_ => _.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync(sessionObj);
            _sessionHelperService.SetupSequence(_ => _.GetSession<Session>(It.IsAny<string>()
                          , It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(sessionObj);

            _sessionHelperService.Setup(_ => _.GetSession<Reservation>(It.IsAny<string>()
                          , It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationObj);

            MobileCMSContentMessages sms = new MobileCMSContentMessages() { ContentFull = "" };
            List<MobileCMSContentMessages> MobileCMSContentMessages = new List<MobileCMSContentMessages>() { };
            MobileCMSContentMessages.Add(sms);

            _moqCMSContentHelper.Setup(_ => _.GetMobileTermsAndConditions(It.IsAny<MobileCMSContentRequest>())).ReturnsAsync(MobileCMSContentMessages);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(sessionObj);


            BagCalculatorBusiness bagCalculatorBusiness = new BagCalculatorBusiness(_logger, _configuration, _moqHeader.Object, _flightShoppingService.Object, _sessionHelperService.Object, _moqtokenService.Object, 
                _moqpNRRetrievalService.Object, _moqCMSContentHelper.Object, _moqMerchOffersService.Object, _moqdynamoDBService.Object, _shoppingSessionHelper.Object, _airlineCarrierService,
                _legalDocumentsForTitlesService.Object, _shoppingUtility, _cachingService.Object, _moqShoppingCcePromoService.Object, _cSLStatisticsService.Object, 
                _cSLShoppingCartService.Object, _validateHashPinService.Object, _optimizelyPersistService.Object, _featureSettings.Object, _cceService.Object, _cceDODService.Object);


            var result = bagCalculatorBusiness.GetMobileCMSContentsData(mobileCMSContentRequest);
            //mobileCMSContentResponse);
            Assert.True(result.Result.SessionId == mobileCMSContentRequest.SessionId);
            // Assert.False(result.Result.CartId == sessionObj.CartId);
            Assert.True(result.Result.MileagePlusNumber == mobileCMSContentRequest.MileagePlusNumber);
            Assert.True(result.Result.Token != null);
            //Assert.False(result.Result.Exception == null);
            Assert.True(result != null);

        }
    }
}
