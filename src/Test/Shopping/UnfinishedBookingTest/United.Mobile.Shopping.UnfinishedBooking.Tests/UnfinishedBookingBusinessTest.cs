using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Ebs.Logging.Enrichers;
using United.Foundations.Practices.Framework.Security.DataPower;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.Loyalty;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.ShopBundles;
using United.Mobile.DataAccess.ShopProducts;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.DataAccess.Travelers;
using United.Mobile.DataAccess.UnfinishedBooking;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Mobile.Model.TripPlannerGetService;
using United.Mobile.Services.UnfinishedBooking.Domain;
using United.Service.Presentation.CustomerModel;
using United.Service.Presentation.PersonalizationResponseModel;
using United.Services.Customer.Preferences.Common;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Utility.Http;
using Xunit;
using Constants = United.Mobile.Model.Constants;
using CustomerProfile = United.Common.Helper.Profile.CustomerProfile;
using MOBSHOPUnfinishedBookingRequestBase = United.Mobile.Model.Shopping.UnfinishedBooking.MOBSHOPUnfinishedBookingRequestBase;
using United.Utility.Helper;

namespace United.Mobile.Shopping.UnfinishedBooking.Tests
{
    public class UnfinishedBookingBusinessTest
    {
        private readonly Mock<ICacheLog<UnfinishedBookingBusiness>> _logger;
        private readonly Mock<ICacheLog<Headers>> _logger1;
        private readonly Mock<ILogger<UnfinishedBookingBusiness>> _mlogger;
        private IConfiguration _configuration;
        private readonly IUnfinishedBookingBusiness _unfinishedBookingBusiness;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly Mock<IDynamoDBService> _dynamoDBService;
        private readonly Mock<IBundleOfferService> _bundleOfferService;
        private readonly Mock<IUnfinishedBooking> _unfinishedBooking;
        private readonly Mock<IOmniCart> _omniCart;
        private readonly Mock<IShoppingUtility> _shoppingUtility;
        private readonly Mock<ICustomerPreferencesService> _customerPreferencesService;
        private readonly Mock<IOmniChannelCartService> _omniChannelCartService;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<IDPService> _tokenService;
        private readonly Mock<IValidateHashPinService> _validateHashPinService;
        //private readonly Mock<IFFCShoppingcs> _fFCShoppingcs;
        private readonly Mock<IFormsOfPayment> _formsOfPayment;
        private readonly Mock<ITravelerUtility> _travelerUtility;
        private readonly Mock<IFlightShoppingProductsService> _flightShoppingProductsService;
        private readonly Mock<IELFRitMetaShopMessages> _eLFRitMetaShopMessages;
        private readonly Mock<ITraveler> _traveler;
        private readonly Mock<IReferencedataService> _referencedataService;
        private readonly Mock<ICachingService> _cachingService;
        private readonly Mock<ITravelerCSL> _travelerCSL;
        private readonly Mock<IShoppingBuyMiles> _shoppingBuyMiles;
        private AirportDetailsList airportsList = null;
        private readonly Mock<IFFCShoppingcs> _ffCShoppingcs;
        private readonly Mock<ICustomerDataService> _customerDataService;
        private readonly Mock<IDataVaultService> _dataVaultService;
        private readonly Mock<IGMTConversionService> _iGMTConversionService;
        private readonly Mock<IMileagePlusCSSTokenService> _mileagePlusCSSTokenService;
        private readonly Mock<ILegalDocumentsForTitlesService> _legalDocumentsForTitlesService;
        private readonly Mock<IDPService> _dPService;
        private readonly Mock<IPKDispenserService> _pKDispenserService;
        private readonly Mock<IProfileService> _profileService;
        private readonly Mock<ILMXInfo> _lmxInfo;
        private readonly Mock<IShoppingCartService> _shoppingCartService;
        private readonly Mock<IShoppingCcePromoService> _shoppingCcePromoService;
        private readonly Mock<ILoyaltyUCBService> _loyaltyBalanceServices;
        private readonly Mock<IPurchaseMerchandizingService> _purchaseMerchandizingService;
        private readonly Mock<IFlightShoppingService> _flightShoppingService;
        private readonly Mock<ICMSContentService> _iCMSContentService;
        private readonly Mock<IPaymentService> _paymentService;
        private readonly Mock<IGMTConversionService> _gMTConversionService;
       // private readonly Mock<IDataPowerGateway> _dataPowerGateway;
        private readonly Mock<IMileagePlusTFACSL> _mileagePlusTFACSL;
        private readonly Mock<IMPSecurityQuestionsService> _mPSecurityQuestionsService;
        private readonly Mock<IPaymentUtility> _paymentUtility;
        private readonly Mock<IProductOffers> _shopping;
        private readonly Mock<IRegisterCFOP> _regiserCFOP;
        private readonly Mock<IProductInfoHelper> _productInfoHelper;
        private readonly Mock<IProfileCreditCard> _profileCreditCard;
        private readonly Mock<IApplicationEnricher> _requestEnricher;
        private readonly Mock<IHeaders> _headers;
        //private readonly Mock<Common.Helper.Profile.CustomerProfile> _customerProfile;
        private readonly Mock<ICustomerProfile> _customerProfile;
        private readonly Mock<IFeatureSettings> _featureSettings;
        private readonly Mock<IShopBundleService> _shopBundleService;
        private readonly Mock<IFeatureToggles> _featureToggles;

        public IConfiguration Configuration
        {
            get
            {
                _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test3.json", optional: false, reloadOnChange: true)
                .Build();

                return _configuration;
            }
        }

        public UnfinishedBookingBusinessTest()
        {
            _logger = new Mock<ICacheLog<UnfinishedBookingBusiness>>();
            _logger1 = new Mock<ICacheLog<Headers>>();
            _mlogger = new Mock<ILogger<UnfinishedBookingBusiness>>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _dynamoDBService = new Mock<IDynamoDBService>();
            _bundleOfferService = new Mock<IBundleOfferService>();
            _unfinishedBooking = new Mock<IUnfinishedBooking>();
            _omniCart = new Mock<IOmniCart>();
            _requestEnricher = new Mock<IApplicationEnricher>();
            _shoppingUtility = new Mock<IShoppingUtility>();
            _customerPreferencesService = new Mock<ICustomerPreferencesService>();
            _omniChannelCartService = new Mock<IOmniChannelCartService>();
            _tokenService = new Mock<IDPService>();
            _tokenService = new Mock<IDPService>();
            _validateHashPinService = new Mock<IValidateHashPinService>();
            _formsOfPayment = new Mock<IFormsOfPayment>();
            _travelerUtility = new Mock<ITravelerUtility>();
            _flightShoppingProductsService = new Mock<IFlightShoppingProductsService>();
            _eLFRitMetaShopMessages = new Mock<IELFRitMetaShopMessages>();
            _traveler = new Mock<ITraveler>();
            _cachingService = new Mock<ICachingService>();
            _travelerCSL = new Mock<ITravelerCSL>();
            _shoppingBuyMiles = new Mock<IShoppingBuyMiles>();
            _ffCShoppingcs = new Mock<IFFCShoppingcs>();
            _referencedataService = new Mock<IReferencedataService>();
            _customerDataService = new Mock<ICustomerDataService>();
            _dataVaultService = new Mock<IDataVaultService>();
            _iGMTConversionService = new Mock<IGMTConversionService>();
            _gMTConversionService = new Mock<IGMTConversionService>();
            _legalDocumentsForTitlesService = new Mock<ILegalDocumentsForTitlesService>();
            _dPService = new Mock<IDPService>();
            _pKDispenserService = new Mock<IPKDispenserService>();
            _profileService = new Mock<IProfileService>();
            _lmxInfo = new Mock<ILMXInfo>();
            _shoppingCartService = new Mock<IShoppingCartService>();
            _shoppingCcePromoService = new Mock<IShoppingCcePromoService>();
            _loyaltyBalanceServices = new Mock<ILoyaltyUCBService>();
            _purchaseMerchandizingService = new Mock<IPurchaseMerchandizingService>();
            _flightShoppingService = new Mock<IFlightShoppingService>();
            _iCMSContentService = new Mock<ICMSContentService>();
            _paymentService = new Mock<IPaymentService>();
            _gMTConversionService = new Mock<IGMTConversionService>();
            //_dataPowerGateway = new Mock<IDataPowerGateway>();
            _mileagePlusTFACSL = new Mock<IMileagePlusTFACSL>();
            _mileagePlusCSSTokenService = new Mock<IMileagePlusCSSTokenService>();
            _mPSecurityQuestionsService = new Mock<IMPSecurityQuestionsService>();
            _customerProfile = new Mock<ICustomerProfile>();
            _paymentUtility = new Mock<IPaymentUtility>();
            _shopping = new Mock<IProductOffers>();
            _regiserCFOP = new Mock<IRegisterCFOP>();
            _productInfoHelper = new Mock<IProductInfoHelper>();
            _profileCreditCard = new Mock<IProfileCreditCard>();
            _headers = new Mock<IHeaders>();
            _featureToggles = new Mock<IFeatureToggles>();
            _shopBundleService = new Mock<IShopBundleService>(Configuration, _sessionHelperService.Object, _bundleOfferService.Object, _shoppingUtility, _headers.Object, _dynamoDBService.Object, _omniCart.Object, _featureSettings.Object);

            _unfinishedBookingBusiness = new UnfinishedBookingBusiness(_logger.Object, Configuration, _shoppingSessionHelper.Object, _sessionHelperService.Object, _dynamoDBService.Object, _bundleOfferService.Object, _unfinishedBooking.Object,
               _omniCart.Object, _shoppingUtility.Object, _customerPreferencesService.Object, _omniChannelCartService.Object,
               _validateHashPinService.Object, _ffCShoppingcs.Object, _formsOfPayment.Object, _travelerUtility.Object, _flightShoppingProductsService.Object, _eLFRitMetaShopMessages.Object, _traveler.Object, _referencedataService.Object,
               _cachingService.Object, _travelerCSL.Object, _shoppingBuyMiles.Object, _customerDataService.Object, _dataVaultService.Object, _iGMTConversionService.Object, _mileagePlusCSSTokenService.Object, _legalDocumentsForTitlesService.Object,
               _dPService.Object, _pKDispenserService.Object, _profileService.Object, _lmxInfo.Object, _shoppingCartService.Object, _shoppingCcePromoService.Object, _loyaltyBalanceServices.Object, _purchaseMerchandizingService.Object, _flightShoppingService.Object, _iCMSContentService.Object, _paymentService.Object, _mileagePlusTFACSL.Object, _mPSecurityQuestionsService.Object, _paymentUtility.Object, _shopping.Object, _regiserCFOP.Object, _productInfoHelper.Object, _profileCreditCard.Object, _requestEnricher.Object, _headers.Object, _customerProfile.Object,_featureSettings.Object, _shopBundleService.Object, _mlogger.Object, _featureToggles.Object, _ffCShoppingcs.Object);


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
            context.Request.Headers[Constants.SessionId] = "6A1792ABFC1CF81182F9C4EAQWESA";
            context.Request.Headers[Constants.HeaderRequestTimeUtcText] = DateTime.UtcNow.ToString();
            context.Request.Headers[Constants.HeaderTransactionIdText] = guid;
            _httpContextAccessor.Setup(_ => _.HttpContext).Returns(context);
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
        [MemberData(nameof(TestDataGenerator.GetUnfinishedBookings_Request), MemberType = typeof(TestDataGenerator))]
        public void GetUnfinishedBookings_Request(MOBSHOPGetUnfinishedBookingsRequest request, Session session, ContextualCommResponse contextualCommResponse,
            List<MOBSHOPUnfinishedBooking> mOBSHOPUnfinishedBooking, FlightReservationResponse flightReservationResponse)
        {

            var items = TestDataGenerator.GetFileContent("Getrecords.json");

            // ContextualCommResponse response = new ContextualCommResponse();

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);
            _dynamoDBService.Setup(p => p.GetRecords<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"AppVersion\":\"4.1.48\",\"ApplicationID\":\"2\",\"AuthenticatedToken\":null,\"CallDuration\":0,\"CustomerID\":\"123901727\",\"DataPowerAccessToken\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9.NNV8GJ9vnJkyYHShMUWinJEDnT9hMI9IDHksAwDmEGSSAQpgZGV0Zv00DwMQYHSPA0HgAwDjZwESARWQBQEQm_UZfeWVu4hFCrLSGjfTs8WRM4GadvbiNAMYdbxZoEh69D-IXfiLKeTCPU-4GE5RKBhYbkMOv0TrQzcMMhRx3TZVsqJDUMphaQTSKpAyFJUYriwVknmMvLXUrcYmDtLkXAuiEOgfNQWCUqqaUcY9HfqFtTcrIY03SjwHH296Ptu8FJ9OdNtnpEehMuNPLpYz.jwC0naNWpnrKScvZMHhSY2zEFTasGTG3JfCP1jhPoKxBeeuG_YkZkq15WhOLdA-erMVuY0e8MSqHEkQ3pNepiRHXo09f_Ht0f9PJfciIUOjA_haRN9x1WYfsd57mPCMZCLOrTI4tPDLbrFoyGFkElHLpmX1fly3mP_gR7ITMpM-s8Ynjr1XVxtZQ072wUOqfllxg8Dp17MPMdRD9VOpNMj-nXDAi0-9_vKE5d0Lm1xmDSh3R00DqkM0VQb2ScHfG5XChkjhux6vFm6Y8lgcgrfO6t5r-gM3Jq7DU6ZbT6Gk30d14PwAfm-35s5N5Bt39zDlBZ3wOcPjgZtnvGEk5Kt.rlW2MKVvBvVkYwNvYPWdqTxvBvWwAmLjAGOvBP1yLwIuYGEyLwRgLJL5Zv1xZzR1MGMuZwD0BJHvYPWmqJVvBvWwAmLjAGOvBP1yLwIuYGEyLwRgLJL5Zv1xZzR1MGMuZwD0BJHvYPWcp3ZvBvWbqUEjpmbiY2AmoJZhpJRhLKI0nP51ozy0MJDhL29gY29uqKEbZv92ZFVfVzS1MPV6Vx1iLzyfMF1OozElo2yxHTuiozIsIHSZKmL0Z0HkEGD3YGRlAQVgARV2Dl1ODwqSYGL0ZQV0EGEPDmt0DlVfVzyuqPV6ZGL1ZQHlBQR3ZljvMKujVwbkAwHjAGZ1ZmpmYPWuoKVvByfvpUqxVy0fVzS1qTusqTygMFV6ZGL1ZQHlBQR3Zljvoz9hL2HvBvVmnUpjMaN5pIuzqaRvYPWuqS9bLKAbVwbvHHqOIaykDHcUAwAmFSIDFUMZF1ScHG09VvjvqKOxLKEyMS9uqPV6ZGL1ZQHlBQR3ZljvL2kcMJ50K2SjpTkcL2S0nJ9hK2yxVwbvGJ9vnJkyYHShMUWinJEDnT9hMI9IDHksAwDmEGSSAQpgZGV0Zv00DwMQYHSPA0HgAwDjZwESARWQBQEQVvjvqKAypyE5pTHvBvWaqJImqPVfVzAfnJIhqS9cMPV6Vx1iLzyfMF1OozElo2yxHTuiozIsIHSZKmL0Z0HkEGD3YGRlAQVgARV2Dl1ODwqSYGL0ZQV0EGEPDmt0DlVfVzIhMSImMKWOM2IhqRyRVwbvAQD2MGx0Z2DgMQp0BF00MwWxYGuvMwVgMGOvLGIxAzEuAwt1VvjvMJ5xIKAypxSaMJ50FINvBvVkZwphZP4jYwRvsD\",\"DeviceID\":\"446e943d-d749-4f2d-8bf2-e0ba5d6da685\",\"HashPincode\":\"A5D3AFDAE0BF0E6543650D7B928EB77C94A4AD56\",\"IsTokenAnonymous\":\"False\",\"IsTokenValid\":\"True\",\"IsTouchIDSignIn\":\"False\",\"MPUserName\":\"AW719636\",\"MileagePlusNumber\":\"AW719636\",\"PinCode\":\"\",\"TokenExpireDateTime\":\"2022-04-2105:02:53.725\",\"TokenExpiryInSeconds\":\"7200\",\"UpdateDateTime\":\"2022-04-2103:02:54.565\"}");

            //_dynamoDBService.Setup(p => p.GetRecords<int>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(4);
            _bundleOfferService.Setup(p => p.UnfinishedBookings<ContextualCommResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((contextualCommResponse, 33001222221)));
            _unfinishedBooking.Setup(p => p.GetSavedUnfinishedBookingEntries(It.IsAny<Session>(), It.IsAny<MOBRequest>(), It.IsAny<bool>())).ReturnsAsync(mOBSHOPUnfinishedBooking);
            _unfinishedBooking.Setup(p => p.GetShopPinDown(It.IsAny<Session>(), It.IsAny<string>(), It.IsAny<ShopRequest>(), It.IsAny<bool>())).Returns(Task.FromResult((flightReservationResponse,false)));
            //Act
            var result = _unfinishedBookingBusiness.GetUnfinishedBookings(request);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);

        }


        [Theory]
        [MemberData(nameof(TestDataGenerator.ClearUnfinishedBookings_Request), MemberType = typeof(TestDataGenerator))]
        public void ClearUnfinishedBookings_Request(SavedItineraryDataModel savedItineraryDataModel, Session session)
        {

            var availability = TestDataGenerator.GetFileContent("MOBSHOPUnfinishedBookingRequestBase.json");
            var availData = JsonConvert.DeserializeObject<MOBSHOPUnfinishedBookingRequestBase>(availability);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);
            _omniCart.Setup(p => p.IsClearAllSavedTrips(It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);
            _customerPreferencesService.Setup(p => p.GetCustomerPrefernce<SavedItineraryDataModel>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(savedItineraryDataModel);
            _omniChannelCartService.Setup(p => p.PurgeUnfinshedBookings(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"Meta\":[{\"Key\":\"DeleteOmniChannelShoppingCartByDeviceIDInMs\",\"Value\":\"439\"},{\"Key\":\"Warning\",\"Value\":\"No data to delete\"},{\"Key\":\"UCDResponse\",\"Value\":\"{\\\"timestamp\\\":\\\"2022-04-21T09:50:41.913+0000\\\",\\\"message\\\":\\\"No Data found for the Type\\\",\\\"description\\\":\\\"uri=/getCartIdList\\\"}\"},{\"Key\":\"DEVICEID\",\"Value\":\"446e943d-d749-4f2d-8bf2-e0ba5d6da685\"},{\"Key\":\"DeleteOmniChannelShoppingCartInUCDStorageByDEVICEID\",\"Value\":\"True\"},{\"Key\":\"DeleteOmniChannelShoppingCartInUCDStorageByDEVICEID\",\"Value\":\"True\"}],\"Status\":\"Successful\"}");
            _dynamoDBService.Setup(p => p.GetRecords<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"AppVersion\":\"4.1.48\",\"ApplicationID\":\"2\",\"AuthenticatedToken\":null,\"CallDuration\":0,\"CustomerID\":\"123901727\",\"DataPowerAccessToken\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9.NNV8GJ9vnJkyYHShMUWinJEDnT9hMI9IDHksAwDmEGSSAQpgZGV0Zv00DwMQYHSPA0HgAwDjZwESARWQBQEQm_UZfeWVu4hFCrLSGjfTs8WRM4GadvbiNAMYdbxZoEh69D-IXfiLKeTCPU-4GE5RKBhYbkMOv0TrQzcMMhRx3TZVsqJDUMphaQTSKpAyFJUYriwVknmMvLXUrcYmDtLkXAuiEOgfNQWCUqqaUcY9HfqFtTcrIY03SjwHH296Ptu8FJ9OdNtnpEehMuNPLpYz.jwC0naNWpnrKScvZMHhSY2zEFTasGTG3JfCP1jhPoKxBeeuG_YkZkq15WhOLdA-erMVuY0e8MSqHEkQ3pNepiRHXo09f_Ht0f9PJfciIUOjA_haRN9x1WYfsd57mPCMZCLOrTI4tPDLbrFoyGFkElHLpmX1fly3mP_gR7ITMpM-s8Ynjr1XVxtZQ072wUOqfllxg8Dp17MPMdRD9VOpNMj-nXDAi0-9_vKE5d0Lm1xmDSh3R00DqkM0VQb2ScHfG5XChkjhux6vFm6Y8lgcgrfO6t5r-gM3Jq7DU6ZbT6Gk30d14PwAfm-35s5N5Bt39zDlBZ3wOcPjgZtnvGEk5Kt.rlW2MKVvBvVkYwNvYPWdqTxvBvWwAmLjAGOvBP1yLwIuYGEyLwRgLJL5Zv1xZzR1MGMuZwD0BJHvYPWmqJVvBvWwAmLjAGOvBP1yLwIuYGEyLwRgLJL5Zv1xZzR1MGMuZwD0BJHvYPWcp3ZvBvWbqUEjpmbiY2AmoJZhpJRhLKI0nP51ozy0MJDhL29gY29uqKEbZv92ZFVfVzS1MPV6Vx1iLzyfMF1OozElo2yxHTuiozIsIHSZKmL0Z0HkEGD3YGRlAQVgARV2Dl1ODwqSYGL0ZQV0EGEPDmt0DlVfVzyuqPV6ZGL1ZQHlBQR3ZljvMKujVwbkAwHjAGZ1ZmpmYPWuoKVvByfvpUqxVy0fVzS1qTusqTygMFV6ZGL1ZQHlBQR3Zljvoz9hL2HvBvVmnUpjMaN5pIuzqaRvYPWuqS9bLKAbVwbvHHqOIaykDHcUAwAmFSIDFUMZF1ScHG09VvjvqKOxLKEyMS9uqPV6ZGL1ZQHlBQR3ZljvL2kcMJ50K2SjpTkcL2S0nJ9hK2yxVwbvGJ9vnJkyYHShMUWinJEDnT9hMI9IDHksAwDmEGSSAQpgZGV0Zv00DwMQYHSPA0HgAwDjZwESARWQBQEQVvjvqKAypyE5pTHvBvWaqJImqPVfVzAfnJIhqS9cMPV6Vx1iLzyfMF1OozElo2yxHTuiozIsIHSZKmL0Z0HkEGD3YGRlAQVgARV2Dl1ODwqSYGL0ZQV0EGEPDmt0DlVfVzIhMSImMKWOM2IhqRyRVwbvAQD2MGx0Z2DgMQp0BF00MwWxYGuvMwVgMGOvLGIxAzEuAwt1VvjvMJ5xIKAypxSaMJ50FINvBvVkZwphZP4jYwRvsD\",\"DeviceID\":\"446e943d-d749-4f2d-8bf2-e0ba5d6da685\",\"HashPincode\":\"A5D3AFDAE0BF0E6543650D7B928EB77C94A4AD56\",\"IsTokenAnonymous\":\"False\",\"IsTokenValid\":\"True\",\"IsTouchIDSignIn\":\"False\",\"MPUserName\":\"AW719636\",\"MileagePlusNumber\":\"AW719636\",\"PinCode\":\"\",\"TokenExpireDateTime\":\"2022-04-2105:02:53.725\",\"TokenExpiryInSeconds\":\"7200\",\"UpdateDateTime\":\"2022-04-2103:02:54.565\"}");

            //_dynamoDBService.Setup(p => p.GetRecords<int>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(4);

            _customerPreferencesService.Setup(p => p.PurgeAnUnfinishedBooking(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"Status\":1,\"ServerName\":\"sad\"}");

            //Act
            var result = _unfinishedBookingBusiness.ClearUnfinishedBookings(availData);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);

        }


        [Theory]
        [MemberData(nameof(TestDataGenerator.SelectUnfinishedBooking_Request), MemberType = typeof(TestDataGenerator))]
        public void SelectUnfinishedBooking_Request(MOBSHOPSelectUnfinishedBookingRequest request, Session session)
        {
            MOBShoppingCart shoppingCart = new MOBShoppingCart();
            var items = TestDataGenerator.GetFileContent("Getrecords.json");
            var availability = TestDataGenerator.GetFileContent("Availability.json");
            var availData = JsonConvert.DeserializeObject<MOBSHOPAvailability>(availability);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _omniCart.Setup(p => p.IsEnableOmniCartHomeScreenChanges(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _shoppingUtility.Setup(p => p.PopulateShoppingCart(It.IsAny<MOBShoppingCart>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MOBRequest>(), It.IsAny<MOBSHOPReservation>(), It.IsAny<bool>())).ReturnsAsync(shoppingCart);

            _dynamoDBService.Setup(p => p.GetRecords<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"AppVersion\":\"4.1.48\",\"ApplicationID\":\"2\",\"AuthenticatedToken\":null,\"CallDuration\":0,\"CustomerID\":\"123901727\",\"DataPowerAccessToken\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9.NNV8GJ9vnJkyYHShMUWinJEDnT9hMI9IDHksAwDmEGSSAQpgZGV0Zv00DwMQYHSPA0HgAwDjZwESARWQBQEQm_UZfeWVu4hFCrLSGjfTs8WRM4GadvbiNAMYdbxZoEh69D-IXfiLKeTCPU-4GE5RKBhYbkMOv0TrQzcMMhRx3TZVsqJDUMphaQTSKpAyFJUYriwVknmMvLXUrcYmDtLkXAuiEOgfNQWCUqqaUcY9HfqFtTcrIY03SjwHH296Ptu8FJ9OdNtnpEehMuNPLpYz.jwC0naNWpnrKScvZMHhSY2zEFTasGTG3JfCP1jhPoKxBeeuG_YkZkq15WhOLdA-erMVuY0e8MSqHEkQ3pNepiRHXo09f_Ht0f9PJfciIUOjA_haRN9x1WYfsd57mPCMZCLOrTI4tPDLbrFoyGFkElHLpmX1fly3mP_gR7ITMpM-s8Ynjr1XVxtZQ072wUOqfllxg8Dp17MPMdRD9VOpNMj-nXDAi0-9_vKE5d0Lm1xmDSh3R00DqkM0VQb2ScHfG5XChkjhux6vFm6Y8lgcgrfO6t5r-gM3Jq7DU6ZbT6Gk30d14PwAfm-35s5N5Bt39zDlBZ3wOcPjgZtnvGEk5Kt.rlW2MKVvBvVkYwNvYPWdqTxvBvWwAmLjAGOvBP1yLwIuYGEyLwRgLJL5Zv1xZzR1MGMuZwD0BJHvYPWmqJVvBvWwAmLjAGOvBP1yLwIuYGEyLwRgLJL5Zv1xZzR1MGMuZwD0BJHvYPWcp3ZvBvWbqUEjpmbiY2AmoJZhpJRhLKI0nP51ozy0MJDhL29gY29uqKEbZv92ZFVfVzS1MPV6Vx1iLzyfMF1OozElo2yxHTuiozIsIHSZKmL0Z0HkEGD3YGRlAQVgARV2Dl1ODwqSYGL0ZQV0EGEPDmt0DlVfVzyuqPV6ZGL1ZQHlBQR3ZljvMKujVwbkAwHjAGZ1ZmpmYPWuoKVvByfvpUqxVy0fVzS1qTusqTygMFV6ZGL1ZQHlBQR3Zljvoz9hL2HvBvVmnUpjMaN5pIuzqaRvYPWuqS9bLKAbVwbvHHqOIaykDHcUAwAmFSIDFUMZF1ScHG09VvjvqKOxLKEyMS9uqPV6ZGL1ZQHlBQR3ZljvL2kcMJ50K2SjpTkcL2S0nJ9hK2yxVwbvGJ9vnJkyYHShMUWinJEDnT9hMI9IDHksAwDmEGSSAQpgZGV0Zv00DwMQYHSPA0HgAwDjZwESARWQBQEQVvjvqKAypyE5pTHvBvWaqJImqPVfVzAfnJIhqS9cMPV6Vx1iLzyfMF1OozElo2yxHTuiozIsIHSZKmL0Z0HkEGD3YGRlAQVgARV2Dl1ODwqSYGL0ZQV0EGEPDmt0DlVfVzIhMSImMKWOM2IhqRyRVwbvAQD2MGx0Z2DgMQp0BF00MwWxYGuvMwVgMGOvLGIxAzEuAwt1VvjvMJ5xIKAypxSaMJ50FINvBvVkZwphZP4jYwRvsD\",\"DeviceID\":\"446e943d-d749-4f2d-8bf2-e0ba5d6da685\",\"HashPincode\":\"A5D3AFDAE0BF0E6543650D7B928EB77C94A4AD56\",\"IsTokenAnonymous\":\"False\",\"IsTokenValid\":\"True\",\"IsTouchIDSignIn\":\"False\",\"MPUserName\":\"AW719636\",\"MileagePlusNumber\":\"AW719636\",\"PinCode\":\"\",\"TokenExpireDateTime\":\"2022-04-2105:02:53.725\",\"TokenExpiryInSeconds\":\"7200\",\"UpdateDateTime\":\"2022-04-2103:02:54.565\"}");

            //_dynamoDBService.Setup(p => p.GetRecords<int>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(4);

            _unfinishedBooking.Setup(p => p.GetShopPinDownDetails(It.IsAny<Session>(), It.IsAny<MOBSHOPSelectUnfinishedBookingRequest>())).ReturnsAsync(availData);

            _unfinishedBooking.Setup(p => p.GetShopPinDownDetailsV2(It.IsAny<Session>(), It.IsAny<MOBSHOPSelectUnfinishedBookingRequest>(), It.IsAny<HttpContext>(), It.IsAny<MOBAddTravelersRequest>(), It.IsAny<bool>())).Returns(Task.FromResult((availData, false)));



            //Act
            var result = _unfinishedBookingBusiness.SelectUnfinishedBooking(request);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetOmniCartSavedTrips_Test), MemberType = typeof(TestDataGenerator))]
        public void GetOmniCartSavedTrips_Test(MOBSHOPUnfinishedBookingRequestBase mOBSHOPUnfinishedBookingRequestBase, Session session)
        {
            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _dPService.Setup(p => p.GetAndSaveAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>(), It.IsAny<string>(), It.IsAny<Session>(), It.IsAny<bool>())).ReturnsAsync("Bearer Token");

            // _shoppingCcePromoService.setup(p => p.ChasePromoFromCCE(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync();

            //Act
            var result = _unfinishedBookingBusiness.GetOmniCartSavedTrips(mOBSHOPUnfinishedBookingRequestBase);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.SelectOmniCartSavedTrip_Request), MemberType = typeof(TestDataGenerator))]
        public void SelectOmniCartSavedTrip_Request(MOBSHOPSelectUnfinishedBookingRequest request, Session session, MOBShoppingCart mOBShoppingCart,
            MOBSHOPSelectTripResponse mOBSHOPSelectTripResponse, ShopRequest shopRequest, FlightReservationResponse flightReservationResponse)
        {

            _shoppingSessionHelper.Setup(p => p.GetValidateSession(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(session);

            _omniCart.Setup(p => p.IsEnableOmniCartReleaseCandidate4CChanges(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<List<MOBItem>>())).Returns(true);

            _omniCart.Setup(p => p.IsNonUSPointOfSale(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MOBSHOPSelectTripResponse>())).ReturnsAsync(false);

           _dPService.Setup(p => p.GetSSOTokenString(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>(), It.IsAny<string>(), It.IsAny<string>(),It.IsAny<IResilientClient>())).Returns(mOBSHOPSelectTripResponse.NonUSPOSAlertMessage.Actions.First().WebShareToken);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _shoppingUtility.Setup(p => p.ReservationToShoppingCart_DataMigration(It.IsAny<MOBSHOPReservation>(), It.IsAny<MOBShoppingCart>(), It.IsAny<MOBRequest>(), It.IsAny<Session>())).ReturnsAsync(mOBShoppingCart);

            _unfinishedBooking.Setup(p => p.BuildShopPinDownRequest(It.IsAny<MOBSHOPUnfinishedBooking>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(shopRequest);

            _shoppingUtility.Setup(p => p.EnableRoundTripPricing(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _unfinishedBooking.Setup(p => p.GetShopPinDown(It.IsAny<Session>(), It.IsAny<string>(), It.IsAny<ShopRequest>(), It.IsAny<bool>())).Returns(Task.FromResult((flightReservationResponse, false)));

            _omniCart.Setup(p => p.GetFlightReservationResponseByCartId(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(flightReservationResponse);

            //Act
            var result = _unfinishedBookingBusiness.SelectOmniCartSavedTrip(request);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.RemoveOmniCartSavedTrip_Request), MemberType = typeof(TestDataGenerator))]
        public void RemoveOmniCartSavedTrip_Request(MOBSHOPUnfinishedBookingRequestBase request, Session session)
        {

         _shoppingSessionHelper.Setup(p => p.GetValidateSession(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>())).ReturnsAsync(session);


            //Act
            var result = _unfinishedBookingBusiness.RemoveOmniCartSavedTrip(request);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);

        }
    }


}

