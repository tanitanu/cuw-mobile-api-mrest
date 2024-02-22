using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
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
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Misc;
using United.Mobile.Model.TripPlannerGetService;
using United.Mobile.Services.TripPlannerGetService.Domain;
using United.Service.Presentation.PersonalizationResponseModel;
using United.TravelPlanner.Models;
using United.Utility.Helper;
using United.Utility.Http;
using Xunit;
using Constants = United.Mobile.Model.Constants;
using CSLShopRequest = United.Mobile.Model.TripPlannerGetService.CSLShopRequest;
using CSLShopResponse = United.Mobile.Model.TripPlannerGetService.CSLShopResponse;
using TripPlanCCEResponse = United.Mobile.Model.TripPlannerGetService.TripPlanCCEResponse;

namespace United.Mobile.Test.TripPlannerGetService.Tests
{
   public class TripPlannerGetBusinessTest
    {
        private readonly Mock<ICacheLog<TripPlannerGetServiceBusiness>> _logger;
        private readonly Mock<ICacheLog<CachingService>> _logger6;
        private readonly Mock<ICacheLog<ShoppingUtility>> _logger8;
        private readonly TripPlannerGetServiceBusiness _tripPlannerGetServiceBusiness;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly Mock<IBundleOfferService> _bundleOfferService;
        private readonly Mock<ITravelPlannerService> _travelPlannerService;
        private readonly Mock<IGetTeaserColumnInfoService> _getTeaserColumnInfoService;
        private readonly Mock<IFlightShoppingService> _flightShoppingService;
        private readonly Mock<IShoppingUtility> _shoppingUtility;
        private readonly Mock<ITripPlannerIDService> _tripPlannerIDService;
        private readonly Mock<ICachingService> _cachingService;
        private readonly ICachingService _cachingService1;
        private readonly Mock<IHeaders> _headers;
        private IConfiguration _configuration;
        private readonly IDPService _dPService;
        private readonly ILMXInfo _lmxInfo;
        private readonly Mock<HttpContext> _httpContext;
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy _policyWrap;
        private readonly string _baseUrl;
        private readonly IDataPowerFactory _dataPowerFactory;
        private readonly IResilientClient _resilientClient;
        private readonly Mock<ICacheLog<DataPowerFactory>> _logger9;
        private readonly  Mock<IReferencedataService> _referencedataService;
        private readonly Mock<IFFCShoppingcs> _fFCShoppingcs;
        private readonly Mock<IAuroraMySqlService> _auroraMySqlService;
        private readonly Mock<IFeatureSettings> _featureSettings;
        private readonly Mock<IFeatureToggles> _featureToggles;


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

        public TripPlannerGetBusinessTest()
        {
            _logger = new Mock<ICacheLog<TripPlannerGetServiceBusiness>>();
            _logger6 = new Mock<ICacheLog<CachingService>>();
            _logger8 = new Mock<ICacheLog<ShoppingUtility>>();
            _logger9 = new Mock<ICacheLog<DataPowerFactory>>();
            _headers = new Mock<IHeaders>();
            _cachingService = new Mock<ICachingService>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _bundleOfferService = new Mock<IBundleOfferService>();
            _travelPlannerService = new Mock<ITravelPlannerService>();
            _getTeaserColumnInfoService = new Mock<IGetTeaserColumnInfoService>();
            _flightShoppingService = new Mock<IFlightShoppingService>();
            _shoppingUtility = new Mock<IShoppingUtility>();
            _tripPlannerIDService = new Mock<ITripPlannerIDService>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _httpContext = new Mock<HttpContext>();
            _dataPowerFactory = new DataPowerFactory(Configuration, _sessionHelperService.Object, _logger9.Object);
            _referencedataService = new Mock<IReferencedataService>();
            _fFCShoppingcs = new Mock<IFFCShoppingcs>();
            _auroraMySqlService = new Mock<IAuroraMySqlService>();
            _featureSettings = new Mock<IFeatureSettings>();
            _featureToggles = new Mock<IFeatureToggles>();

            _configuration = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
             .Build();

            _resilientClient = new ResilientClient(_baseUrl);

            _cachingService1 = new CachingService(_resilientClient, _logger6.Object, _configuration);

            _tripPlannerGetServiceBusiness = new TripPlannerGetServiceBusiness(_logger.Object, _configuration, _sessionHelperService.Object, _shoppingSessionHelper.Object,_bundleOfferService.Object, _travelPlannerService.Object, _getTeaserColumnInfoService.Object,_dPService,_flightShoppingService.Object, _lmxInfo, _shoppingUtility.Object, _tripPlannerIDService.Object, _cachingService.Object, _referencedataService.Object, _fFCShoppingcs.Object, _auroraMySqlService.Object,_featureSettings.Object, _featureToggles.Object);

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
        [MemberData(nameof(TestDataGenerator.GetTripPlanSummary_Request), MemberType = typeof(TestDataGenerator))]
        public void GetTripPlanSummary_Request(MOBTripPlanSummaryRequest request, Session session, CSLShopResponse cSLShopResponse, CSLShopRequest cSLShopRequest ,TripPlanCCEResponse tripPlanCCEResponse)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopResponse);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _sessionHelperService.Setup(p => p.GetSession<TripPlanCCEResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(tripPlanCCEResponse);

            _tripPlannerIDService.Setup(p => p.GetTripPlanID<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("Raj");

            _bundleOfferService.Setup(p => p.GetCCEContent<ContextualCommResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(("{\"SessionId\":\"a28ed174-8a61-498a-8bef-b87fb605088e\",\"MileagePlusId\":\"AW792682\",\"CustomerProfile\":{\"MileagePlusProfile\":{\"LoyaltyProgramMemberTierDescription\":0,\"FirstName\":\"David\",\"LastName\":\"Woods\"}},\"Components\":[{\"ContextualElements\":[{\"Type\":\"ContextualMessage\",\"Value\":{\"Itinerary\":{\"ItineraryDisplayPrice\":\"$439\",\"IsELF\":false,\"IsIBE\":false,\"NumberOfChildren12To17\":0,\"NumberOfInfantWithSeat\":0,\"NumberOfChildren5To11\":0,\"NumberOfInfantOnLap\":0,\"NumberOfAdults\":1,\"NumberOfSeniors\":0,\"NumberOfChildren2To4\":0,\"ConversionPrice\":439,\"Currency\":\"USD\",\"InsertTimestamp\":\"04/22/2022 09:11:47 AM\",\"UpdateTimestamp\":\"04/22/2022 09:11:47 AM\",\"InsertID\":null,\"UpdateID\":null,\"SavedItinerary\":{\"Trips\":[{\"Flights\":[{\"BookingCode\":\"U\",\"DepartDateTime\":\"2022-05-11 09:09\",\"Destination\":\"AUS\",\"FlightNumber\":\"1783\",\"MarketingCarrier\":\"UA\",\"Origin\":\"SFO\",\"ProductType\":\"ECONOMY\",\"CurrencyType\":null,\"Price\":null,\"Connections\":[],\"OriginDescription\":\"San Francisco, CA, US (SFO)\",\"DestinationDescription\":\"Austin, TX, US (AUS)\",\"Products\":[{\"BookingCode\":\"U\",\"ProductType\":\"ECONOMY\",\"Prices\":[],\"DisplayOrder\":0,\"NumberOfPassengers\":0,\"TripIndex\":0,\"SegmentNumber\":0,\"IsOverBooked\":0,\"IsDynamicallyPriced\":0,\"Selected\":false,\"MarriedSegmentIndex\":0,\"HasMultipleClasses\":false}]}],\"FlightCount\":0,\"Destination\":\"AUS\",\"Origin\":\"SFO\",\"DepartDate\":\"5/11/2022\",\"DepartTime\":\"9:09 AM\",\"ArrivalDate\":\"5/11/2022\",\"ArrivalTime\":\"2:44 PM\",\"NumberOfStop\":0}],\"TrueAvailability\":true,\"SearchTypeSelection\":1,\"PaxInfoList\":[{\"DateOfBirth\":\"4/22/1997\",\"PaxType\":1,\"PaxTypeCode\":\"ADT\",\"PaxTypeDescription\":\"Adult (18-64)\"}],\"LoyaltyId\":\"AW792682\",\"NGRP\":true,\"InitialShop\":true,\"CountryCode\":\"US\",\"ChannelType\":\"MOBILE\",\"AwardTravel\":false,\"AccessCode\":\"10C7335B-41A9-49CA-A9F1-638134FE7FA5\",\"LangCode\":\"en-US\",\"CorporationName\":null,\"CorporateTravelProvider\":null,\"EmployeeDiscountId\":null}},\"MessageKey\":\"MOB.HOMEPAGE.TRIPPLANNERALERT.SHARETRIP_PILOT\",\"MessageType\":\"ContextualMessage\",\"Params\":{\"ORIGINCITY\":\"San Francisco\",\"DESTCITY\":\"Austin\",\"FLIGHTDATE\":\"Wed, May 11\",\"AIRPORTSANDTIMES\":\"SFO to AUS at 9:09 am\",\"DISPLAYPRICE\":\"$439\",\"TRIPTYPE\":\"one-way\",\"ACCESSCODE\":\"10C7335B-41A9-49CA-A9F1-638134FE7FA5\",\"SEARCHDATE\":\"04/22/2022 09:11 AM\",\"LINKTO\":\"RTI\",\"DEPARTUREDATE\":\"05/11/2022 09:09 AM\",\"ORIGIN\":\"SFO\",\"DESTINATION\":\"AUS\",\"PRICEWITHETC\":\"$439\",\"RETURNDATE\":\"05/11/2022 09:09 AM\",\"TRIPDETAILS\":\"Wed, May 11<br>SFO 9:09 am - AUS 2:44 pm\",\"TRIPDATES\":\"Wed, May 11\",\"TRIPDATETIMES\":\"SFO 9:09 AM - AUS 2:44 PM\",\"VARIANTID\":\"V1\",\"OUTORIGINAIRPORT\":\"SFO\",\"OUTDESTINATIONAIRPORT\":\"AUS\",\"INORIGINAIRPORT\":\"\",\"INDESTINATIONAIRPORT\":\"\",\"OUTDEPARTURETIME\":\"9:09 AM\",\"INDEPARTURETIME\":\"\",\"CURRENCYCODE\":\"USD\",\"ETCBALANCE\":\"0\"},\"Content\":{\"Misc\":[],\"Links\":[{\"Link\":\"RTI\",\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP--Link\",\"ComponentSchema\":\"external_link\",\"IsExternalLink\":false,\"LinkStyle\":\"LINK_BOOK\"}],\"Title\":\"Wed, May 11<br>SFO 9:09 am - AUS 2:44 pm\",\"SubTitle\":\"from\\n$439\",\"Images\":[],\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\",\"ComponentSchema\":\"general_content_basic_rich_text\",\"Location\":\"SavedTrips\",\"OrderIndex\":1,\"PersonalizationKey\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\"}},\"Rank\":1},{\"Type\":\"ContextualMessage\",\"Value\":{\"Itinerary\":{\"ItineraryDisplayPrice\":\"$286\",\"IsELF\":false,\"IsIBE\":false,\"NumberOfChildren12To17\":0,\"NumberOfInfantWithSeat\":0,\"NumberOfChildren5To11\":0,\"NumberOfInfantOnLap\":0,\"NumberOfAdults\":1,\"NumberOfSeniors\":0,\"NumberOfChildren2To4\":0,\"ConversionPrice\":286,\"Currency\":\"USD\",\"InsertTimestamp\":\"04/22/2022 09:10:10 AM\",\"UpdateTimestamp\":\"04/22/2022 09:10:10 AM\",\"InsertID\":null,\"UpdateID\":null,\"SavedItinerary\":{\"Trips\":[{\"Flights\":[{\"BookingCode\":\"V\",\"DepartDateTime\":\"2022-05-23 05:48\",\"Destination\":\"IAH\",\"FlightNumber\":\"492\",\"MarketingCarrier\":\"UA\",\"Origin\":\"EWR\",\"ProductType\":\"ECONOMY\",\"CurrencyType\":null,\"Price\":null,\"Connections\":[],\"OriginDescription\":\"New York/Newark, NJ, US (EWR)\",\"DestinationDescription\":\"Houston, TX, US (IAH)\",\"Products\":[{\"BookingCode\":\"V\",\"ProductType\":\"ECONOMY\",\"Prices\":[],\"DisplayOrder\":0,\"NumberOfPassengers\":0,\"TripIndex\":0,\"SegmentNumber\":0,\"IsOverBooked\":0,\"IsDynamicallyPriced\":0,\"Selected\":false,\"MarriedSegmentIndex\":0,\"HasMultipleClasses\":false}]}],\"FlightCount\":0,\"Destination\":\"IAH\",\"Origin\":\"EWR\",\"DepartDate\":\"5/23/2022\",\"DepartTime\":\"5:48 AM\",\"ArrivalDate\":\"5/23/2022\",\"ArrivalTime\":\"8:31 AM\",\"NumberOfStop\":0}],\"TrueAvailability\":true,\"SearchTypeSelection\":1,\"PaxInfoList\":[{\"DateOfBirth\":\"4/20/1997\",\"PaxType\":1,\"PaxTypeCode\":\"ADT\",\"PaxTypeDescription\":\"Adult (18-64)\"}],\"LoyaltyId\":\"\",\"NGRP\":true,\"InitialShop\":true,\"CountryCode\":\"US\",\"ChannelType\":\"MOBILE\",\"AwardTravel\":false,\"AccessCode\":\"CD8225E3-73FD-484F-8159-2FF0A2449B71\",\"LangCode\":\"en-US\",\"CorporationName\":null,\"CorporateTravelProvider\":null,\"EmployeeDiscountId\":null}},\"MessageKey\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\",\"MessageType\":\"ContextualMessage\",\"Params\":{\"ORIGINCITY\":\"New York/Newark\",\"DESTCITY\":\"Houston\",\"FLIGHTDATE\":\"Mon, May 23\",\"AIRPORTSANDTIMES\":\"EWR to IAH at 5:48 am\",\"DISPLAYPRICE\":\"$286\",\"TRIPTYPE\":\"one-way\",\"ACCESSCODE\":\"CD8225E3-73FD-484F-8159-2FF0A2449B71\",\"SEARCHDATE\":\"04/22/2022 09:10 AM\",\"LINKTO\":\"RTI\",\"DEPARTUREDATE\":\"05/23/2022 05:48 AM\",\"ORIGIN\":\"EWR\",\"DESTINATION\":\"IAH\",\"PRICEWITHETC\":\"$286\",\"RETURNDATE\":\"05/23/2022 05:48 AM\",\"TRIPDETAILS\":\"Mon, May 23<br>EWR 5:48 am - IAH 8:31 am\",\"TRIPDATES\":\"Mon, May 23\",\"TRIPDATETIMES\":\"EWR 5:48 AM - IAH 8:31 AM\",\"VARIANTID\":\"V1\",\"OUTORIGINAIRPORT\":\"EWR\",\"OUTDESTINATIONAIRPORT\":\"IAH\",\"INORIGINAIRPORT\":\"\",\"INDESTINATIONAIRPORT\":\"\",\"OUTDEPARTURETIME\":\"5:48 AM\",\"INDEPARTURETIME\":\"\",\"CURRENCYCODE\":\"USD\",\"ETCBALANCE\":\"0\"},\"Content\":{\"Misc\":[],\"Links\":[{\"Link\":\"RTI\",\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP--Link\",\"ComponentSchema\":\"external_link\",\"IsExternalLink\":false}],\"Title\":\"Mon, May 23<br>EWR 5:48 am - IAH 8:31 am\",\"SubTitle\":\"from\\n$286\",\"Images\":[],\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\",\"ComponentSchema\":\"general_content_basic_rich_text\",\"Location\":\"SavedTrips\",\"OrderIndex\":2,\"PersonalizationKey\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\"}},\"Rank\":2},{\"Type\":\"ContextualMessage\",\"Value\":{\"Itinerary\":{\"ItineraryDisplayPrice\":\"$245\",\"IsELF\":false,\"IsIBE\":false,\"NumberOfChildren12To17\":0,\"NumberOfInfantWithSeat\":0,\"NumberOfChildren5To11\":0,\"NumberOfInfantOnLap\":0,\"NumberOfAdults\":1,\"NumberOfSeniors\":0,\"NumberOfChildren2To4\":0,\"ConversionPrice\":245,\"Currency\":\"USD\",\"InsertTimestamp\":\"04/22/2022 09:10:10 AM\",\"UpdateTimestamp\":\"04/22/2022 09:10:10 AM\",\"InsertID\":null,\"UpdateID\":null,\"SavedItinerary\":{\"Trips\":[{\"Flights\":[{\"BookingCode\":\"V\",\"DepartDateTime\":\"2022-05-23 05:32\",\"Destination\":\"ORD\",\"FlightNumber\":\"2653\",\"MarketingCarrier\":\"UA\",\"Origin\":\"IAH\",\"ProductType\":\"ECONOMY\",\"CurrencyType\":null,\"Price\":null,\"Connections\":[],\"OriginDescription\":\"Houston, TX, US (IAH)\",\"DestinationDescription\":\"Chicago, IL, US (ORD)\",\"Products\":[{\"BookingCode\":\"V\",\"ProductType\":\"ECONOMY\",\"Prices\":[],\"DisplayOrder\":0,\"NumberOfPassengers\":0,\"TripIndex\":0,\"SegmentNumber\":0,\"IsOverBooked\":0,\"IsDynamicallyPriced\":0,\"Selected\":false,\"MarriedSegmentIndex\":0,\"HasMultipleClasses\":false}]}],\"FlightCount\":0,\"Destination\":\"ORD\",\"Origin\":\"IAH\",\"DepartDate\":\"5/23/2022\",\"DepartTime\":\"5:32 AM\",\"ArrivalDate\":\"5/23/2022\",\"ArrivalTime\":\"8:13 AM\",\"NumberOfStop\":0}],\"TrueAvailability\":true,\"SearchTypeSelection\":1,\"PaxInfoList\":[{\"DateOfBirth\":\"4/19/1997\",\"PaxType\":1,\"PaxTypeCode\":\"ADT\",\"PaxTypeDescription\":\"Adult (18-64)\"}],\"LoyaltyId\":\"\",\"NGRP\":true,\"InitialShop\":true,\"CountryCode\":\"US\",\"ChannelType\":\"MOBILE\",\"AwardTravel\":false,\"AccessCode\":\"EF0A4527-07D0-4155-BB13-8D50F7A5B9FA\",\"LangCode\":\"en-US\",\"CorporationName\":null,\"CorporateTravelProvider\":null,\"EmployeeDiscountId\":null}},\"MessageKey\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\",\"MessageType\":\"ContextualMessage\",\"Params\":{\"ORIGINCITY\":\"Houston\",\"DESTCITY\":\"Chicago\",\"FLIGHTDATE\":\"Mon, May 23\",\"AIRPORTSANDTIMES\":\"IAH to ORD at 5:32 am\",\"DISPLAYPRICE\":\"$245\",\"TRIPTYPE\":\"one-way\",\"ACCESSCODE\":\"EF0A4527-07D0-4155-BB13-8D50F7A5B9FA\",\"SEARCHDATE\":\"04/22/2022 09:10 AM\",\"LINKTO\":\"RTI\",\"DEPARTUREDATE\":\"05/23/2022 05:32 AM\",\"ORIGIN\":\"IAH\",\"DESTINATION\":\"ORD\",\"PRICEWITHETC\":\"$245\",\"RETURNDATE\":\"05/23/2022 05:32 AM\",\"TRIPDETAILS\":\"Mon, May 23<br>IAH 5:32 am - ORD 8:13 am\",\"TRIPDATES\":\"Mon, May 23\",\"TRIPDATETIMES\":\"IAH 5:32 AM - ORD 8:13 AM\",\"VARIANTID\":\"V1\",\"OUTORIGINAIRPORT\":\"IAH\",\"OUTDESTINATIONAIRPORT\":\"ORD\",\"INORIGINAIRPORT\":\"\",\"INDESTINATIONAIRPORT\":\"\",\"OUTDEPARTURETIME\":\"5:32 AM\",\"INDEPARTURETIME\":\"\",\"CURRENCYCODE\":\"USD\",\"ETCBALANCE\":\"0\"},\"Content\":{\"Misc\":[],\"Links\":[{\"Link\":\"RTI\",\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP--Link\",\"ComponentSchema\":\"external_link\",\"IsExternalLink\":false}],\"Title\":\"Mon, May 23<br>IAH 5:32 am - ORD 8:13 am\",\"SubTitle\":\"from\\n$245\",\"Images\":[],\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\",\"ComponentSchema\":\"general_content_basic_rich_text\",\"Location\":\"SavedTrips\",\"OrderIndex\":3,\"PersonalizationKey\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\"}},\"Rank\":3}],\"Rank\":1,\"Name\":\"TRIPPLANNERALERT\",\"Content\":{\"Title\":\"Saved trips\",\"BodyText\":\"*Availability and pricing are subject to change\",\"Images\":[],\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS\",\"ComponentSchema\":\"general_content_basic_rich_text\",\"OrderIndex\":1,\"PersonalizationKey\":\"MOB.BOOKING.SAVEDTRIPS\"}}],\"TransactionID\":\"4e8b3945-e7a9-40f4-a9a3-c93263f70af3\",\"GUIDs\":[{\"ID\":\"b9a24cb9-49a5-42a3-abc3-1ae13fb9a91d\",\"Name\":\"CorrelationId\",\"LanguageCode\":null},{\"ID\":\"a28ed174-8a61-498a-8bef-b87fb605088e\",\"Name\":\"SessionId\",\"LanguageCode\":null},{\"ID\":\"-1549730220\",\"Name\":\"XDPAuthToken\",\"LanguageCode\":null}],\"TimeStamps\":[{\"FunctionName\":\"GetRTDCustomerViewAsync\",\"ResponseTime\":\"00:00:00.0500000\"},{\"FunctionName\":\"GetCustomerFutureBookingsAsync\",\"ResponseTime\":\"00:00:00.0500000\"},{\"FunctionName\":\"GetOmniChannelCartResponse\",\"ResponseTime\":\"00:00:00.3220000\"},{\"FunctionName\":\"GetOmniChannelCartResponse\",\"ResponseTime\":\"00:00:00.3340000\"},{\"FunctionName\":\"GetFlightShopResponsesAsync\",\"ResponseTime\":\"00:00:00.5200000\"},{\"FunctionName\":\"GetRulesAsync\",\"ResponseTime\":\"00:00:00.0200000\"},{\"FunctionName\":\"GetDecisionsAsync\",\"ResponseTime\":\"00:00:00.0120000\"},{\"FunctionName\":\"GetContentsAsync\",\"ResponseTime\":\"00:00:00.0290000\"}]}", 33001222221)));

          //  _bundleOfferService.Setup(p => p.GetCCEContent<ContextualCommResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((contextualCommResponse, 33001222221)));

            //Act
            var result = _tripPlannerGetServiceBusiness.GetTripPlanSummary(request);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetTripPlanSummary_Request), MemberType = typeof(TestDataGenerator))]
        public void GetTripPlanSummary_Request1(MOBTripPlanSummaryRequest request, Session session, CSLShopResponse cSLShopResponse, CSLShopRequest cSLShopRequest, TripPlanCCEResponse tripPlanCCEResponse)
        {
            var _dataPowerFactory = new DataPowerFactory(Configuration1, _sessionHelperService.Object, _logger9.Object);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopResponse);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _sessionHelperService.Setup(p => p.GetSession<TripPlanCCEResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(tripPlanCCEResponse);

            _tripPlannerIDService.Setup(p => p.GetTripPlanID<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("Raj");

            _bundleOfferService.Setup(p => p.GetCCEContent<ContextualCommResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(("{\"SessionId\":\"a28ed174-8a61-498a-8bef-b87fb605088e\",\"MileagePlusId\":\"AW792682\",\"CustomerProfile\":{\"MileagePlusProfile\":{\"LoyaltyProgramMemberTierDescription\":0,\"FirstName\":\"David\",\"LastName\":\"Woods\"}},\"Components\":[{\"ContextualElements\":[{\"Type\":\"ContextualMessage\",\"Value\":{\"Itinerary\":{\"ItineraryDisplayPrice\":\"$439\",\"IsELF\":false,\"IsIBE\":false,\"NumberOfChildren12To17\":0,\"NumberOfInfantWithSeat\":0,\"NumberOfChildren5To11\":0,\"NumberOfInfantOnLap\":0,\"NumberOfAdults\":1,\"NumberOfSeniors\":0,\"NumberOfChildren2To4\":0,\"ConversionPrice\":439,\"Currency\":\"USD\",\"InsertTimestamp\":\"04/22/2022 09:11:47 AM\",\"UpdateTimestamp\":\"04/22/2022 09:11:47 AM\",\"InsertID\":null,\"UpdateID\":null,\"SavedItinerary\":{\"Trips\":[{\"Flights\":[{\"BookingCode\":\"U\",\"DepartDateTime\":\"2022-05-11 09:09\",\"Destination\":\"AUS\",\"FlightNumber\":\"1783\",\"MarketingCarrier\":\"UA\",\"Origin\":\"SFO\",\"ProductType\":\"ECONOMY\",\"CurrencyType\":null,\"Price\":null,\"Connections\":[],\"OriginDescription\":\"San Francisco, CA, US (SFO)\",\"DestinationDescription\":\"Austin, TX, US (AUS)\",\"Products\":[{\"BookingCode\":\"U\",\"ProductType\":\"ECONOMY\",\"Prices\":[],\"DisplayOrder\":0,\"NumberOfPassengers\":0,\"TripIndex\":0,\"SegmentNumber\":0,\"IsOverBooked\":0,\"IsDynamicallyPriced\":0,\"Selected\":false,\"MarriedSegmentIndex\":0,\"HasMultipleClasses\":false}]}],\"FlightCount\":0,\"Destination\":\"AUS\",\"Origin\":\"SFO\",\"DepartDate\":\"5/11/2022\",\"DepartTime\":\"9:09 AM\",\"ArrivalDate\":\"5/11/2022\",\"ArrivalTime\":\"2:44 PM\",\"NumberOfStop\":0}],\"TrueAvailability\":true,\"SearchTypeSelection\":1,\"PaxInfoList\":[{\"DateOfBirth\":\"4/22/1997\",\"PaxType\":1,\"PaxTypeCode\":\"ADT\",\"PaxTypeDescription\":\"Adult (18-64)\"}],\"LoyaltyId\":\"AW792682\",\"NGRP\":true,\"InitialShop\":true,\"CountryCode\":\"US\",\"ChannelType\":\"MOBILE\",\"AwardTravel\":false,\"AccessCode\":\"10C7335B-41A9-49CA-A9F1-638134FE7FA5\",\"LangCode\":\"en-US\",\"CorporationName\":null,\"CorporateTravelProvider\":null,\"EmployeeDiscountId\":null}},\"MessageKey\":\"MOB.HOMEPAGE.TRIPPLANNERALERT.SHARETRIP_PILOT\",\"MessageType\":\"ContextualMessage\",\"Params\":{\"ORIGINCITY\":\"San Francisco\",\"DESTCITY\":\"Austin\",\"FLIGHTDATE\":\"Wed, May 11\",\"AIRPORTSANDTIMES\":\"SFO to AUS at 9:09 am\",\"DISPLAYPRICE\":\"$439\",\"TRIPTYPE\":\"one-way\",\"ACCESSCODE\":\"10C7335B-41A9-49CA-A9F1-638134FE7FA5\",\"SEARCHDATE\":\"04/22/2022 09:11 AM\",\"LINKTO\":\"RTI\",\"DEPARTUREDATE\":\"05/11/2022 09:09 AM\",\"ORIGIN\":\"SFO\",\"DESTINATION\":\"AUS\",\"PRICEWITHETC\":\"$439\",\"RETURNDATE\":\"05/11/2022 09:09 AM\",\"TRIPDETAILS\":\"Wed, May 11<br>SFO 9:09 am - AUS 2:44 pm\",\"TRIPDATES\":\"Wed, May 11\",\"TRIPDATETIMES\":\"SFO 9:09 AM - AUS 2:44 PM\",\"VARIANTID\":\"V1\",\"OUTORIGINAIRPORT\":\"SFO\",\"OUTDESTINATIONAIRPORT\":\"AUS\",\"INORIGINAIRPORT\":\"\",\"INDESTINATIONAIRPORT\":\"\",\"OUTDEPARTURETIME\":\"9:09 AM\",\"INDEPARTURETIME\":\"\",\"CURRENCYCODE\":\"USD\",\"ETCBALANCE\":\"0\"},\"Content\":{\"Misc\":[],\"Links\":[{\"Link\":\"RTI\",\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP--Link\",\"ComponentSchema\":\"external_link\",\"IsExternalLink\":false,\"LinkStyle\":\"LINK_SHARETRIP\"}],\"Title\":\"Wed, May 11<br>SFO 9:09 am - AUS 2:44 pm\",\"SubTitle\":\"from\\n$439\",\"Images\":[],\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\",\"ComponentSchema\":\"general_content_basic_rich_text\",\"Location\":\"SavedTrips\",\"OrderIndex\":1,\"PersonalizationKey\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\"}},\"Rank\":1},{\"Type\":\"ContextualMessage\",\"Value\":{\"Itinerary\":{\"ItineraryDisplayPrice\":\"$286\",\"IsELF\":false,\"IsIBE\":false,\"NumberOfChildren12To17\":0,\"NumberOfInfantWithSeat\":0,\"NumberOfChildren5To11\":0,\"NumberOfInfantOnLap\":0,\"NumberOfAdults\":1,\"NumberOfSeniors\":0,\"NumberOfChildren2To4\":0,\"ConversionPrice\":286,\"Currency\":\"USD\",\"InsertTimestamp\":\"04/22/2022 09:10:10 AM\",\"UpdateTimestamp\":\"04/22/2022 09:10:10 AM\",\"InsertID\":null,\"UpdateID\":null,\"SavedItinerary\":{\"Trips\":[{\"Flights\":[{\"BookingCode\":\"V\",\"DepartDateTime\":\"2022-05-23 05:48\",\"Destination\":\"IAH\",\"FlightNumber\":\"492\",\"MarketingCarrier\":\"UA\",\"Origin\":\"EWR\",\"ProductType\":\"ECONOMY\",\"CurrencyType\":null,\"Price\":null,\"Connections\":[],\"OriginDescription\":\"New York/Newark, NJ, US (EWR)\",\"DestinationDescription\":\"Houston, TX, US (IAH)\",\"Products\":[{\"BookingCode\":\"V\",\"ProductType\":\"ECONOMY\",\"Prices\":[],\"DisplayOrder\":0,\"NumberOfPassengers\":0,\"TripIndex\":0,\"SegmentNumber\":0,\"IsOverBooked\":0,\"IsDynamicallyPriced\":0,\"Selected\":false,\"MarriedSegmentIndex\":0,\"HasMultipleClasses\":false}]}],\"FlightCount\":0,\"Destination\":\"IAH\",\"Origin\":\"EWR\",\"DepartDate\":\"5/23/2022\",\"DepartTime\":\"5:48 AM\",\"ArrivalDate\":\"5/23/2022\",\"ArrivalTime\":\"8:31 AM\",\"NumberOfStop\":0}],\"TrueAvailability\":true,\"SearchTypeSelection\":1,\"PaxInfoList\":[{\"DateOfBirth\":\"4/20/1997\",\"PaxType\":1,\"PaxTypeCode\":\"ADT\",\"PaxTypeDescription\":\"Adult (18-64)\"}],\"LoyaltyId\":\"\",\"NGRP\":true,\"InitialShop\":true,\"CountryCode\":\"US\",\"ChannelType\":\"MOBILE\",\"AwardTravel\":false,\"AccessCode\":\"CD8225E3-73FD-484F-8159-2FF0A2449B71\",\"LangCode\":\"en-US\",\"CorporationName\":null,\"CorporateTravelProvider\":null,\"EmployeeDiscountId\":null}},\"MessageKey\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\",\"MessageType\":\"ContextualMessage\",\"Params\":{\"ORIGINCITY\":\"New York/Newark\",\"DESTCITY\":\"Houston\",\"FLIGHTDATE\":\"Mon, May 23\",\"AIRPORTSANDTIMES\":\"EWR to IAH at 5:48 am\",\"DISPLAYPRICE\":\"$286\",\"TRIPTYPE\":\"one-way\",\"ACCESSCODE\":\"CD8225E3-73FD-484F-8159-2FF0A2449B71\",\"SEARCHDATE\":\"04/22/2022 09:10 AM\",\"LINKTO\":\"RTI\",\"DEPARTUREDATE\":\"05/23/2022 05:48 AM\",\"ORIGIN\":\"EWR\",\"DESTINATION\":\"IAH\",\"PRICEWITHETC\":\"$286\",\"RETURNDATE\":\"05/23/2022 05:48 AM\",\"TRIPDETAILS\":\"Mon, May 23<br>EWR 5:48 am - IAH 8:31 am\",\"TRIPDATES\":\"Mon, May 23\",\"TRIPDATETIMES\":\"EWR 5:48 AM - IAH 8:31 AM\",\"VARIANTID\":\"V1\",\"OUTORIGINAIRPORT\":\"EWR\",\"OUTDESTINATIONAIRPORT\":\"IAH\",\"INORIGINAIRPORT\":\"\",\"INDESTINATIONAIRPORT\":\"\",\"OUTDEPARTURETIME\":\"5:48 AM\",\"INDEPARTURETIME\":\"\",\"CURRENCYCODE\":\"USD\",\"ETCBALANCE\":\"0\"},\"Content\":{\"Misc\":[],\"Links\":[{\"Link\":\"RTI\",\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP--Link\",\"ComponentSchema\":\"external_link\",\"IsExternalLink\":false}],\"Title\":\"Mon, May 23<br>EWR 5:48 am - IAH 8:31 am\",\"SubTitle\":\"from\\n$286\",\"Images\":[],\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\",\"ComponentSchema\":\"general_content_basic_rich_text\",\"Location\":\"SavedTrips\",\"OrderIndex\":2,\"PersonalizationKey\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\"}},\"Rank\":2},{\"Type\":\"ContextualMessage\",\"Value\":{\"Itinerary\":{\"ItineraryDisplayPrice\":\"$245\",\"IsELF\":false,\"IsIBE\":false,\"NumberOfChildren12To17\":0,\"NumberOfInfantWithSeat\":0,\"NumberOfChildren5To11\":0,\"NumberOfInfantOnLap\":0,\"NumberOfAdults\":1,\"NumberOfSeniors\":0,\"NumberOfChildren2To4\":0,\"ConversionPrice\":245,\"Currency\":\"USD\",\"InsertTimestamp\":\"04/22/2022 09:10:10 AM\",\"UpdateTimestamp\":\"04/22/2022 09:10:10 AM\",\"InsertID\":null,\"UpdateID\":null,\"SavedItinerary\":{\"Trips\":[{\"Flights\":[{\"BookingCode\":\"V\",\"DepartDateTime\":\"2022-05-23 05:32\",\"Destination\":\"ORD\",\"FlightNumber\":\"2653\",\"MarketingCarrier\":\"UA\",\"Origin\":\"IAH\",\"ProductType\":\"ECONOMY\",\"CurrencyType\":null,\"Price\":null,\"Connections\":[],\"OriginDescription\":\"Houston, TX, US (IAH)\",\"DestinationDescription\":\"Chicago, IL, US (ORD)\",\"Products\":[{\"BookingCode\":\"V\",\"ProductType\":\"ECONOMY\",\"Prices\":[],\"DisplayOrder\":0,\"NumberOfPassengers\":0,\"TripIndex\":0,\"SegmentNumber\":0,\"IsOverBooked\":0,\"IsDynamicallyPriced\":0,\"Selected\":false,\"MarriedSegmentIndex\":0,\"HasMultipleClasses\":false}]}],\"FlightCount\":0,\"Destination\":\"ORD\",\"Origin\":\"IAH\",\"DepartDate\":\"5/23/2022\",\"DepartTime\":\"5:32 AM\",\"ArrivalDate\":\"5/23/2022\",\"ArrivalTime\":\"8:13 AM\",\"NumberOfStop\":0}],\"TrueAvailability\":true,\"SearchTypeSelection\":1,\"PaxInfoList\":[{\"DateOfBirth\":\"4/19/1997\",\"PaxType\":1,\"PaxTypeCode\":\"ADT\",\"PaxTypeDescription\":\"Adult (18-64)\"}],\"LoyaltyId\":\"\",\"NGRP\":true,\"InitialShop\":true,\"CountryCode\":\"US\",\"ChannelType\":\"MOBILE\",\"AwardTravel\":false,\"AccessCode\":\"EF0A4527-07D0-4155-BB13-8D50F7A5B9FA\",\"LangCode\":\"en-US\",\"CorporationName\":null,\"CorporateTravelProvider\":null,\"EmployeeDiscountId\":null}},\"MessageKey\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\",\"MessageType\":\"ContextualMessage\",\"Params\":{\"ORIGINCITY\":\"Houston\",\"DESTCITY\":\"Chicago\",\"FLIGHTDATE\":\"Mon, May 23\",\"AIRPORTSANDTIMES\":\"IAH to ORD at 5:32 am\",\"DISPLAYPRICE\":\"$245\",\"TRIPTYPE\":\"one-way\",\"ACCESSCODE\":\"EF0A4527-07D0-4155-BB13-8D50F7A5B9FA\",\"SEARCHDATE\":\"04/22/2022 09:10 AM\",\"LINKTO\":\"RTI\",\"DEPARTUREDATE\":\"05/23/2022 05:32 AM\",\"ORIGIN\":\"IAH\",\"DESTINATION\":\"ORD\",\"PRICEWITHETC\":\"$245\",\"RETURNDATE\":\"05/23/2022 05:32 AM\",\"TRIPDETAILS\":\"Mon, May 23<br>IAH 5:32 am - ORD 8:13 am\",\"TRIPDATES\":\"Mon, May 23\",\"TRIPDATETIMES\":\"IAH 5:32 AM - ORD 8:13 AM\",\"VARIANTID\":\"V1\",\"OUTORIGINAIRPORT\":\"IAH\",\"OUTDESTINATIONAIRPORT\":\"ORD\",\"INORIGINAIRPORT\":\"\",\"INDESTINATIONAIRPORT\":\"\",\"OUTDEPARTURETIME\":\"5:32 AM\",\"INDEPARTURETIME\":\"\",\"CURRENCYCODE\":\"USD\",\"ETCBALANCE\":\"0\"},\"Content\":{\"Misc\":[],\"Links\":[{\"Link\":\"RTI\",\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP--Link\",\"ComponentSchema\":\"external_link\",\"IsExternalLink\":false}],\"Title\":\"Mon, May 23<br>IAH 5:32 am - ORD 8:13 am\",\"SubTitle\":\"from\\n$245\",\"Images\":[],\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\",\"ComponentSchema\":\"general_content_basic_rich_text\",\"Location\":\"SavedTrips\",\"OrderIndex\":3,\"PersonalizationKey\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\"}},\"Rank\":3}],\"Rank\":1,\"Name\":\"TRIPPLANNERALERT\",\"Content\":{\"Title\":\"Saved trips\",\"BodyText\":\"*Availability and pricing are subject to change\",\"Images\":[],\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS\",\"ComponentSchema\":\"general_content_basic_rich_text\",\"OrderIndex\":1,\"PersonalizationKey\":\"MOB.BOOKING.SAVEDTRIPS\"}}],\"TransactionID\":\"4e8b3945-e7a9-40f4-a9a3-c93263f70af3\",\"GUIDs\":[{\"ID\":\"b9a24cb9-49a5-42a3-abc3-1ae13fb9a91d\",\"Name\":\"CorrelationId\",\"LanguageCode\":null},{\"ID\":\"a28ed174-8a61-498a-8bef-b87fb605088e\",\"Name\":\"SessionId\",\"LanguageCode\":null},{\"ID\":\"-1549730220\",\"Name\":\"XDPAuthToken\",\"LanguageCode\":null}],\"TimeStamps\":[{\"FunctionName\":\"GetRTDCustomerViewAsync\",\"ResponseTime\":\"00:00:00.0500000\"},{\"FunctionName\":\"GetCustomerFutureBookingsAsync\",\"ResponseTime\":\"00:00:00.0500000\"},{\"FunctionName\":\"GetOmniChannelCartResponse\",\"ResponseTime\":\"00:00:00.3220000\"},{\"FunctionName\":\"GetOmniChannelCartResponse\",\"ResponseTime\":\"00:00:00.3340000\"},{\"FunctionName\":\"GetFlightShopResponsesAsync\",\"ResponseTime\":\"00:00:00.5200000\"},{\"FunctionName\":\"GetRulesAsync\",\"ResponseTime\":\"00:00:00.0200000\"},{\"FunctionName\":\"GetDecisionsAsync\",\"ResponseTime\":\"00:00:00.0120000\"},{\"FunctionName\":\"GetContentsAsync\",\"ResponseTime\":\"00:00:00.0290000\"}],\"Error\":[{\"Code\":\"E404\",\"ErrorType\":\"exception\",\"Text\":\"error\",\"Description\":\"try again\",\"ApplicationName\":\"xyz\",\"Characteristics\":[{\"Code\":\"SHOP_INITIAL_REQUEST_DATETIME\",\"Value\":\"11/25/2022 15:22:32\"},{\"Code\":\"CLIENTIP\"}]}]}", 33001222221)));

            //  _bundleOfferService.Setup(p => p.GetCCEContent<ContextualCommResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((contextualCommResponse, 33001222221)));


            var tripPlannerGetServiceBusiness = new TripPlannerGetServiceBusiness(_logger.Object, Configuration1, _sessionHelperService.Object, _shoppingSessionHelper.Object, _bundleOfferService.Object, _travelPlannerService.Object, _getTeaserColumnInfoService.Object, _dPService, _flightShoppingService.Object, _lmxInfo, _shoppingUtility.Object, _tripPlannerIDService.Object, _cachingService.Object, _referencedataService.Object, _fFCShoppingcs.Object, _auroraMySqlService.Object,_featureSettings.Object, _featureToggles.Object);

            //Act
            var result = tripPlannerGetServiceBusiness.GetTripPlanSummary(request);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }


        [Theory]
        [MemberData(nameof(TestDataGenerator.SelectTripTripPlanner_Request), MemberType = typeof(TestDataGenerator))]
        public void SelectTripTripPlanner_Request(MOBSHOPSelectTripRequest request, Session session, ShoppingResponse shoppingResponse, CSLShopResponse cSLShopResponse, CSLShopRequest cSLShopRequest, CSLSelectTrip cSLSelectTrip)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopResponse);

            _sessionHelperService.Setup(p => p.GetSession<CSLShopRequest>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLShopRequest);

            _sessionHelperService.Setup(p => p.GetSession<CSLSelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cSLSelectTrip);



            //Act
            var result = _tripPlannerGetServiceBusiness.SelectTripTripPlanner(request, _httpContext.Object);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetTripPlanBoard_Request), MemberType = typeof(TestDataGenerator))]
        public void GetTripPlanBoard_Request(MOBTripPlanBoardRequest request, Session session)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _bundleOfferService.Setup(p => p.GetCCEContent<ContextualCommResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(("{\"SessionId\":\"a28ed174-8a61-498a-8bef-b87fb605088e\",\"MileagePlusId\":\"AW792682\",\"CustomerProfile\":{\"MileagePlusProfile\":{\"LoyaltyProgramMemberTierDescription\":0,\"FirstName\":\"David\",\"LastName\":\"Woods\"}},\"Components\":[{\"ContextualElements\":[{\"Type\":\"ContextualMessage\",\"Value\":{\"Itinerary\":{\"ItineraryDisplayPrice\":\"$439\",\"IsELF\":false,\"IsIBE\":false,\"NumberOfChildren12To17\":0,\"NumberOfInfantWithSeat\":0,\"NumberOfChildren5To11\":0,\"NumberOfInfantOnLap\":0,\"NumberOfAdults\":1,\"NumberOfSeniors\":0,\"NumberOfChildren2To4\":0,\"ConversionPrice\":439,\"Currency\":\"USD\",\"InsertTimestamp\":\"04/22/2022 09:11:47 AM\",\"UpdateTimestamp\":\"04/22/2022 09:11:47 AM\",\"InsertID\":null,\"UpdateID\":null,\"SavedItinerary\":{\"Trips\":[{\"Flights\":[{\"BookingCode\":\"U\",\"DepartDateTime\":\"2022-05-11 09:09\",\"Destination\":\"AUS\",\"FlightNumber\":\"1783\",\"MarketingCarrier\":\"UA\",\"Origin\":\"SFO\",\"ProductType\":\"ECONOMY\",\"CurrencyType\":null,\"Price\":null,\"Connections\":[],\"OriginDescription\":\"San Francisco, CA, US (SFO)\",\"DestinationDescription\":\"Austin, TX, US (AUS)\",\"Products\":[{\"BookingCode\":\"U\",\"ProductType\":\"ECONOMY\",\"Prices\":[],\"DisplayOrder\":0,\"NumberOfPassengers\":0,\"TripIndex\":0,\"SegmentNumber\":0,\"IsOverBooked\":0,\"IsDynamicallyPriced\":0,\"Selected\":false,\"MarriedSegmentIndex\":0,\"HasMultipleClasses\":false}]}],\"FlightCount\":0,\"Destination\":\"AUS\",\"Origin\":\"SFO\",\"DepartDate\":\"5/11/2022\",\"DepartTime\":\"9:09 AM\",\"ArrivalDate\":\"5/11/2022\",\"ArrivalTime\":\"2:44 PM\",\"NumberOfStop\":0}],\"TrueAvailability\":true,\"SearchTypeSelection\":1,\"PaxInfoList\":[{\"DateOfBirth\":\"4/22/1997\",\"PaxType\":1,\"PaxTypeCode\":\"ADT\",\"PaxTypeDescription\":\"Adult (18-64)\"}],\"LoyaltyId\":\"AW792682\",\"NGRP\":true,\"InitialShop\":true,\"CountryCode\":\"US\",\"ChannelType\":\"MOBILE\",\"AwardTravel\":false,\"AccessCode\":\"10C7335B-41A9-49CA-A9F1-638134FE7FA5\",\"LangCode\":\"en-US\",\"CorporationName\":null,\"CorporateTravelProvider\":null,\"EmployeeDiscountId\":null}},\"MessageKey\":\"MOB.HOMEPAGE.TRIPBOARD.NEXTTRIPBOARD\",\"MessageType\":\"ContextualMessage\",\"Params\":{\"ORIGINCITY\":\"San Francisco\",\"DESTCITY\":\"Austin\",\"FLIGHTDATE\":\"Wed, May 11\",\"AIRPORTSANDTIMES\":\"SFO to AUS at 9:09 am\",\"DISPLAYPRICE\":\"$439\",\"TRIPTYPE\":\"one-way\",\"ACCESSCODE\":\"10C7335B-41A9-49CA-A9F1-638134FE7FA5\",\"SEARCHDATE\":\"04/22/2022 09:11 AM\",\"LINKTO\":\"RTI\",\"DEPARTUREDATE\":\"05/11/2022 09:09 AM\",\"ORIGIN\":\"SFO\",\"DESTINATION\":\"AUS\",\"PRICEWITHETC\":\"$439\",\"RETURNDATE\":\"05/11/2022 09:09 AM\",\"TRIPDETAILS\":\"Wed, May 11<br>SFO 9:09 am - AUS 2:44 pm\",\"TRIPDATES\":\"Wed, May 11\",\"TRIPDATETIMES\":\"SFO 9:09 AM - AUS 2:44 PM\",\"VARIANTID\":\"V1\",\"OUTORIGINAIRPORT\":\"SFO\",\"OUTDESTINATIONAIRPORT\":\"AUS\",\"INORIGINAIRPORT\":\"\",\"INDESTINATIONAIRPORT\":\"\",\"OUTDEPARTURETIME\":\"9:09 AM\",\"INDEPARTURETIME\":\"\",\"CURRENCYCODE\":\"USD\",\"ETCBALANCE\":\"0\"},\"Content\":{\"Misc\":[],\"Links\":[{\"Link\":\"RTI\",\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP--Link\",\"ComponentSchema\":\"external_link\",\"IsExternalLink\":false,\"LinkStyle\":\"LINK_SHARETRIP\"}],\"Title\":\"Wed, May 11<br>SFO 9:09 am - AUS 2:44 pm\",\"SubTitle\":\"from\\n$439\",\"Images\":[],\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\",\"ComponentSchema\":\"general_content_basic_rich_text\",\"Location\":\"SavedTrips\",\"OrderIndex\":1,\"PersonalizationKey\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\"}},\"Rank\":1},{\"Type\":\"ContextualMessage\",\"Value\":{\"Itinerary\":{\"ItineraryDisplayPrice\":\"$286\",\"IsELF\":false,\"IsIBE\":false,\"NumberOfChildren12To17\":0,\"NumberOfInfantWithSeat\":0,\"NumberOfChildren5To11\":0,\"NumberOfInfantOnLap\":0,\"NumberOfAdults\":1,\"NumberOfSeniors\":0,\"NumberOfChildren2To4\":0,\"ConversionPrice\":286,\"Currency\":\"USD\",\"InsertTimestamp\":\"04/22/2022 09:10:10 AM\",\"UpdateTimestamp\":\"04/22/2022 09:10:10 AM\",\"InsertID\":null,\"UpdateID\":null,\"SavedItinerary\":{\"Trips\":[{\"Flights\":[{\"BookingCode\":\"V\",\"DepartDateTime\":\"2022-05-23 05:48\",\"Destination\":\"IAH\",\"FlightNumber\":\"492\",\"MarketingCarrier\":\"UA\",\"Origin\":\"EWR\",\"ProductType\":\"ECONOMY\",\"CurrencyType\":null,\"Price\":null,\"Connections\":[],\"OriginDescription\":\"New York/Newark, NJ, US (EWR)\",\"DestinationDescription\":\"Houston, TX, US (IAH)\",\"Products\":[{\"BookingCode\":\"V\",\"ProductType\":\"ECONOMY\",\"Prices\":[],\"DisplayOrder\":0,\"NumberOfPassengers\":0,\"TripIndex\":0,\"SegmentNumber\":0,\"IsOverBooked\":0,\"IsDynamicallyPriced\":0,\"Selected\":false,\"MarriedSegmentIndex\":0,\"HasMultipleClasses\":false}]}],\"FlightCount\":0,\"Destination\":\"IAH\",\"Origin\":\"EWR\",\"DepartDate\":\"5/23/2022\",\"DepartTime\":\"5:48 AM\",\"ArrivalDate\":\"5/23/2022\",\"ArrivalTime\":\"8:31 AM\",\"NumberOfStop\":0}],\"TrueAvailability\":true,\"SearchTypeSelection\":1,\"PaxInfoList\":[{\"DateOfBirth\":\"4/20/1997\",\"PaxType\":1,\"PaxTypeCode\":\"ADT\",\"PaxTypeDescription\":\"Adult (18-64)\"}],\"LoyaltyId\":\"\",\"NGRP\":true,\"InitialShop\":true,\"CountryCode\":\"US\",\"ChannelType\":\"MOBILE\",\"AwardTravel\":false,\"AccessCode\":\"CD8225E3-73FD-484F-8159-2FF0A2449B71\",\"LangCode\":\"en-US\",\"CorporationName\":null,\"CorporateTravelProvider\":null,\"EmployeeDiscountId\":null}},\"MessageKey\":\"MOB.HOMEPAGE.TRIPBOARD.NEXTTRIPBOARD\",\"MessageType\":\"ContextualMessage\",\"Params\":{\"ORIGINCITY\":\"New York/Newark\",\"DESTCITY\":\"Houston\",\"FLIGHTDATE\":\"Mon, May 23\",\"AIRPORTSANDTIMES\":\"EWR to IAH at 5:48 am\",\"DISPLAYPRICE\":\"$286\",\"TRIPTYPE\":\"one-way\",\"ACCESSCODE\":\"CD8225E3-73FD-484F-8159-2FF0A2449B71\",\"SEARCHDATE\":\"04/22/2022 09:10 AM\",\"LINKTO\":\"RTI\",\"DEPARTUREDATE\":\"05/23/2022 05:48 AM\",\"ORIGIN\":\"EWR\",\"DESTINATION\":\"IAH\",\"PRICEWITHETC\":\"$286\",\"RETURNDATE\":\"05/23/2022 05:48 AM\",\"TRIPDETAILS\":\"Mon, May 23<br>EWR 5:48 am - IAH 8:31 am\",\"TRIPDATES\":\"Mon, May 23\",\"TRIPDATETIMES\":\"EWR 5:48 AM - IAH 8:31 AM\",\"VARIANTID\":\"V1\",\"OUTORIGINAIRPORT\":\"EWR\",\"OUTDESTINATIONAIRPORT\":\"IAH\",\"INORIGINAIRPORT\":\"\",\"INDESTINATIONAIRPORT\":\"\",\"OUTDEPARTURETIME\":\"5:48 AM\",\"INDEPARTURETIME\":\"\",\"CURRENCYCODE\":\"USD\",\"ETCBALANCE\":\"0\"},\"Content\":{\"Misc\":[],\"Links\":[{\"Link\":\"RTI\",\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP--Link\",\"ComponentSchema\":\"external_link\",\"IsExternalLink\":false}],\"Title\":\"Mon, May 23<br>EWR 5:48 am - IAH 8:31 am\",\"SubTitle\":\"from\\n$286\",\"Images\":[],\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\",\"ComponentSchema\":\"general_content_basic_rich_text\",\"Location\":\"SavedTrips\",\"OrderIndex\":2,\"PersonalizationKey\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\"}},\"Rank\":2},{\"Type\":\"ContextualMessage\",\"Value\":{\"Itinerary\":{\"ItineraryDisplayPrice\":\"$245\",\"IsELF\":false,\"IsIBE\":false,\"NumberOfChildren12To17\":0,\"NumberOfInfantWithSeat\":0,\"NumberOfChildren5To11\":0,\"NumberOfInfantOnLap\":0,\"NumberOfAdults\":1,\"NumberOfSeniors\":0,\"NumberOfChildren2To4\":0,\"ConversionPrice\":245,\"Currency\":\"USD\",\"InsertTimestamp\":\"04/22/2022 09:10:10 AM\",\"UpdateTimestamp\":\"04/22/2022 09:10:10 AM\",\"InsertID\":null,\"UpdateID\":null,\"SavedItinerary\":{\"Trips\":[{\"Flights\":[{\"BookingCode\":\"V\",\"DepartDateTime\":\"2022-05-23 05:32\",\"Destination\":\"ORD\",\"FlightNumber\":\"2653\",\"MarketingCarrier\":\"UA\",\"Origin\":\"IAH\",\"ProductType\":\"ECONOMY\",\"CurrencyType\":null,\"Price\":null,\"Connections\":[],\"OriginDescription\":\"Houston, TX, US (IAH)\",\"DestinationDescription\":\"Chicago, IL, US (ORD)\",\"Products\":[{\"BookingCode\":\"V\",\"ProductType\":\"ECONOMY\",\"Prices\":[],\"DisplayOrder\":0,\"NumberOfPassengers\":0,\"TripIndex\":0,\"SegmentNumber\":0,\"IsOverBooked\":0,\"IsDynamicallyPriced\":0,\"Selected\":false,\"MarriedSegmentIndex\":0,\"HasMultipleClasses\":false}]}],\"FlightCount\":0,\"Destination\":\"ORD\",\"Origin\":\"IAH\",\"DepartDate\":\"5/23/2022\",\"DepartTime\":\"5:32 AM\",\"ArrivalDate\":\"5/23/2022\",\"ArrivalTime\":\"8:13 AM\",\"NumberOfStop\":0}],\"TrueAvailability\":true,\"SearchTypeSelection\":1,\"PaxInfoList\":[{\"DateOfBirth\":\"4/19/1997\",\"PaxType\":1,\"PaxTypeCode\":\"ADT\",\"PaxTypeDescription\":\"Adult (18-64)\"}],\"LoyaltyId\":\"\",\"NGRP\":true,\"InitialShop\":true,\"CountryCode\":\"US\",\"ChannelType\":\"MOBILE\",\"AwardTravel\":false,\"AccessCode\":\"EF0A4527-07D0-4155-BB13-8D50F7A5B9FA\",\"LangCode\":\"en-US\",\"CorporationName\":null,\"CorporateTravelProvider\":null,\"EmployeeDiscountId\":null}},\"MessageKey\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\",\"MessageType\":\"ContextualMessage\",\"Params\":{\"ORIGINCITY\":\"Houston\",\"DESTCITY\":\"Chicago\",\"FLIGHTDATE\":\"Mon, May 23\",\"AIRPORTSANDTIMES\":\"IAH to ORD at 5:32 am\",\"DISPLAYPRICE\":\"$245\",\"TRIPTYPE\":\"one-way\",\"ACCESSCODE\":\"EF0A4527-07D0-4155-BB13-8D50F7A5B9FA\",\"SEARCHDATE\":\"04/22/2022 09:10 AM\",\"LINKTO\":\"RTI\",\"DEPARTUREDATE\":\"05/23/2022 05:32 AM\",\"ORIGIN\":\"IAH\",\"DESTINATION\":\"ORD\",\"PRICEWITHETC\":\"$245\",\"RETURNDATE\":\"05/23/2022 05:32 AM\",\"TRIPDETAILS\":\"Mon, May 23<br>IAH 5:32 am - ORD 8:13 am\",\"TRIPDATES\":\"Mon, May 23\",\"TRIPDATETIMES\":\"IAH 5:32 AM - ORD 8:13 AM\",\"VARIANTID\":\"V1\",\"OUTORIGINAIRPORT\":\"IAH\",\"OUTDESTINATIONAIRPORT\":\"ORD\",\"INORIGINAIRPORT\":\"\",\"INDESTINATIONAIRPORT\":\"\",\"OUTDEPARTURETIME\":\"5:32 AM\",\"INDEPARTURETIME\":\"\",\"CURRENCYCODE\":\"USD\",\"ETCBALANCE\":\"0\"},\"Content\":{\"Misc\":[],\"Links\":[{\"Link\":\"RTI\",\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP--Link\",\"ComponentSchema\":\"external_link\",\"IsExternalLink\":false}],\"Title\":\"Mon, May 23<br>IAH 5:32 am - ORD 8:13 am\",\"SubTitle\":\"from\\n$245\",\"Images\":[],\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\",\"ComponentSchema\":\"general_content_basic_rich_text\",\"Location\":\"SavedTrips\",\"OrderIndex\":3,\"PersonalizationKey\":\"MOB.BOOKING.SAVEDTRIPS.SAVEDTRIP\"}},\"Rank\":3}],\"Rank\":1,\"Name\":\"TRIPBOARD\",\"Content\":{\"Title\":\"Saved trips\",\"BodyText\":\"*Availability and pricing are subject to change\",\"Images\":[],\"ComponentTitle\":\"MOB.BOOKING.SAVEDTRIPS\",\"ComponentSchema\":\"general_content_basic_rich_text\",\"OrderIndex\":1,\"PersonalizationKey\":\"MOB.BOOKING.SAVEDTRIPS\"}}],\"TransactionID\":\"4e8b3945-e7a9-40f4-a9a3-c93263f70af3\",\"GUIDs\":[{\"ID\":\"b9a24cb9-49a5-42a3-abc3-1ae13fb9a91d\",\"Name\":\"CorrelationId\",\"LanguageCode\":null},{\"ID\":\"a28ed174-8a61-498a-8bef-b87fb605088e\",\"Name\":\"SessionId\",\"LanguageCode\":null},{\"ID\":\"-1549730220\",\"Name\":\"XDPAuthToken\",\"LanguageCode\":null}],\"TimeStamps\":[{\"FunctionName\":\"GetRTDCustomerViewAsync\",\"ResponseTime\":\"00:00:00.0500000\"},{\"FunctionName\":\"GetCustomerFutureBookingsAsync\",\"ResponseTime\":\"00:00:00.0500000\"},{\"FunctionName\":\"GetOmniChannelCartResponse\",\"ResponseTime\":\"00:00:00.3220000\"},{\"FunctionName\":\"GetOmniChannelCartResponse\",\"ResponseTime\":\"00:00:00.3340000\"},{\"FunctionName\":\"GetFlightShopResponsesAsync\",\"ResponseTime\":\"00:00:00.5200000\"},{\"FunctionName\":\"GetRulesAsync\",\"ResponseTime\":\"00:00:00.0200000\"},{\"FunctionName\":\"GetDecisionsAsync\",\"ResponseTime\":\"00:00:00.0120000\"},{\"FunctionName\":\"GetContentsAsync\",\"ResponseTime\":\"00:00:00.0290000\"}]}", 33001222221)));





            //Act
            var result = _tripPlannerGetServiceBusiness.GetTripPlanBoard(request);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

    }
}
