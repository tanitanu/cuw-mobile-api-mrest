using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Common.Helper.FOP;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.Loyalty;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.MPAuthentication;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.DataAccess.Travelers;
using United.Mobile.MemberProfile.Domain;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.AccountManagement;
using United.Mobile.Model.Internal.Common;
using United.Mobile.Model.MPSignIn;
using United.Mobile.Model.Shopping;
using United.Mobile.Test.MemberProfile.Tests;
using United.Service.Presentation.LoyaltyModel;
//using United.Service.Presentation.LoyaltyModel;
using United.Utility.Http;
using Xunit;
using MOBLegalDocument = United.Definition.MOBLegalDocument;
using ProfileResponse = United.Mobile.Model.Common.ProfileResponse;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using Session = United.Mobile.Model.Common.Session;
using United.Utility.Helper;
using United.Mobile.DataAccess.ShopBundles;

namespace United.Mobile.Test.MemberProfileTests.Api
{
    public class MemberProfileBusinessTests
    {
        private readonly Mock<ICacheLog<MemberProfileBusiness>> _logger;
        private readonly IConfiguration _configuration;
        private readonly Mock<IDynamoDBService> _dynamoDBService;
        private readonly IDynamoDBService _dynamoDBService1;
        private readonly Mock<ICacheLog<DynamoDBService>> _logger10;
        private readonly Mock<DocumentLibraryDynamoDB> _documentLibraryDynamoDB;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly Mock<ICustomerProfile> _customerProfile;
        private readonly Mock<IMileagePlus> _mileagePlus;
        private readonly MemberProfileBusiness _memberProfileBusiness;
        private readonly Mock<IMerchandizingServices> _merchandizingServices;
        private readonly Mock<IDPService> _dpService;
        private readonly Mock<IProfileXML> _profileXML;
        private readonly Mock<IShoppingUtility> _shoppingUtility;
        private readonly Mock<ILoyaltyPromotionsService> _statusLiftBannerService;
        private readonly Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly Mock<IValidateHashPinService> _validateHashPinService;
        private readonly Mock<ILegalDocumentsForTitlesService> _legalDocumentsForTitlesService;
        private readonly Mock<ICSLStatisticsService> _cslStatisticsService;
        private readonly Mock<ITraveler> _traveler;
        private readonly Mock<IFFCShoppingcs> _ffcShoppingcs;
        private readonly Mock<IPaymentService> _paymentService;
        private readonly Mock<IEmpProfile> _empProfile;
        private readonly Mock<ICachingService> _cachingService;
        private readonly ICachingService _cachingService1;
        private readonly Mock<ICacheLog<CachingService>> _logger4;
        private readonly IResilientClient _resilientClient;
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy _policyWrap;
        private readonly Mock<IMPTraveler> _mpTraveler;
        private readonly IMPTraveler _mpTraveler1;
        private readonly Mock<ICacheLog<MPTraveler>> _logger5;
        private readonly Mock<ICustomerDataService> _mpEnrollmentService;
        private readonly Mock<IDataVaultService> _dataVaultService;
        private readonly Mock<IShoppingCartService> _shoppingCartService;
        private readonly Mock<IUtilitiesService> _utilitiesService;
        private readonly Mock<IPNRRetrievalService> _pNRRetrievalService;
        private readonly Mock<IProfileCreditCard> _profileCreditCard;
        private readonly Mock<IMPSecurityCheckDetailsService> _mPSecurityCheckDetailsService;
        private readonly IMPSecurityCheckDetailsService _mPSecurityCheckDetailsService1;
        private readonly Mock<IPKDispenserService> _pKDispenserService;
        //private readonly IMPSecurityCheckDetailsService _mPSecurityCheckDetailsService;
        private readonly Mock<ICacheLog<MPSecurityCheckDetailsService>> _logger6;
        private readonly Mock<IMemberProfileUtility> _memberProfileUtility;
        private readonly IMemberProfileUtility _memberProfileUtility1;
        private readonly Mock<ICacheLog<MemberProfileUtility>> _logger7;
        private readonly Mock<IValidateAccountFC> _validateAccountFC;
        private readonly Mock<ITravelerUtility> _travelerUtility;
        private readonly ITravelerUtility _travelerUtility1;
        private readonly Mock<ICacheLog<TravelerUtility>> _logger8;
        private readonly Mock<IReferencedataService> _referencedataService;
        private readonly IReferencedataService _referencedataService1;
        private readonly Mock<ICacheLog<ReferencedataService>> _logger9;
        private readonly Mock<IBaseEmployeeResService> _baseEmployeeRes;
        private readonly Mock<IEServiceCheckin> _eServiceCheckin;
        private readonly Mock<IShoppingBuyMiles> _shoppingBuyMiles;
        // private readonly PKDispenserPublicKey _pKDispenserPublicKey1;
        private readonly Mock<PKDispenserPublicKey> _pKDispenserPublicKey;
        private readonly InsertOrUpdateTravelInfoService _insertOrUpdateTravelInfoService;
        private readonly Mock<ICacheLog<InsertOrUpdateTravelInfoService>> _logger11;
        private readonly Mock<ILoyaltyUCBService> _loyaltyUCBService;
        private readonly Mock<IOmniCart> _omniCart;
        private readonly Mock<IFlightShoppingProductsService> _getProductsService;
        private readonly Mock<IFormsOfPayment> _formsOfPayment;
        private readonly Mock<ICustomerProfileOwnerService> _customerProfileOwnerService;
        private readonly Mock<ILegalDocumentsForTitlesService> _legalDocumentService;
        private readonly Mock<ISSOTokenKeyService> _sSOTokenKeyService;
        private readonly Mock<IShopBundleService> _shopBundleService;
        private readonly string _baseUrl;
        public static int i = 1;
        public static int j = 1;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<IFeatureSettings> _featureSettings;
        private readonly Mock<ILogger<TravelerUtility>> _mlogger;
        private readonly Mock<IProvisionService> _provisionService;
        private readonly Mock<IFeatureToggles> _featureToggles;
        private readonly Mock<ICorporateProfile> _corpProfile;

        public MemberProfileBusinessTests()
        {
            _logger = new Mock<ICacheLog<MemberProfileBusiness>>();
            _logger4 = new Mock<ICacheLog<CachingService>>();
            _logger5 = new Mock<ICacheLog<MPTraveler>>();
            _logger6 = new Mock<ICacheLog<MPSecurityCheckDetailsService>>();
            _logger7 = new Mock<ICacheLog<MemberProfileUtility>>();
            _logger8 = new Mock<ICacheLog<TravelerUtility>>();
            _logger9 = new Mock<ICacheLog<ReferencedataService>>();
            _logger10 = new Mock<ICacheLog<DynamoDBService>>();
            _logger11 = new Mock<ICacheLog<InsertOrUpdateTravelInfoService>>();
            _mlogger = new Mock<ILogger<TravelerUtility>>();
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.test.json", optional: false, reloadOnChange: true)
                .Build();
            _dynamoDBService = new Mock<IDynamoDBService>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _memberProfileUtility = new Mock<IMemberProfileUtility>();
            _customerProfile = new Mock<ICustomerProfile>();
            _mileagePlus = new Mock<IMileagePlus>();
            _merchandizingServices = new Mock<IMerchandizingServices>();
            _dpService = new Mock<IDPService>();
            _profileXML = new Mock<IProfileXML>();
            _statusLiftBannerService = new Mock<ILoyaltyPromotionsService>();
            _shoppingUtility = new Mock<IShoppingUtility>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _cslStatisticsService = new Mock<ICSLStatisticsService>();
            _traveler = new Mock<ITraveler>();
            _mpTraveler = new Mock<IMPTraveler>();
            _travelerUtility = new Mock<ITravelerUtility>();
            _ffcShoppingcs = new Mock<IFFCShoppingcs>();
            _paymentService = new Mock<IPaymentService>();
            _empProfile = new Mock<IEmpProfile>();
            _shoppingBuyMiles = new Mock<IShoppingBuyMiles>();
            _cachingService = new Mock<ICachingService>();
            _validateHashPinService = new Mock<IValidateHashPinService>();
            _documentLibraryDynamoDB = new Mock<DocumentLibraryDynamoDB>(_configuration, _dynamoDBService.Object);
            _legalDocumentsForTitlesService = new Mock<ILegalDocumentsForTitlesService>();
            _referencedataService = new Mock<IReferencedataService>();
            _mpEnrollmentService = new Mock<ICustomerDataService>();
            _dataVaultService = new Mock<IDataVaultService>();
            _mPSecurityCheckDetailsService = new Mock<IMPSecurityCheckDetailsService>();
            _validateAccountFC = new Mock<IValidateAccountFC>();
            _shoppingCartService = new Mock<IShoppingCartService>();
            _eServiceCheckin = new Mock<IEServiceCheckin>();
            _pKDispenserPublicKey = new Mock<PKDispenserPublicKey>();
          //  _insertOrUpdateTravelInfoService = new Mock<InsertOrUpdateTravelInfoService>();
            _omniCart = new Mock<IOmniCart>();
            _getProductsService = new Mock<IFlightShoppingProductsService>();
            _formsOfPayment = new Mock<IFormsOfPayment>();
            _resilientClient = new ResilientClient(_baseUrl);
            _cachingService1 = new CachingService(_resilientClient, _logger4.Object, _configuration);
            _pKDispenserService = new Mock<IPKDispenserService>();
            _headers = new Mock<IHeaders>();
            _legalDocumentService= new Mock<ILegalDocumentsForTitlesService>();
            _utilitiesService = new Mock<IUtilitiesService>();
            _pNRRetrievalService = new Mock<IPNRRetrievalService>();
            _profileCreditCard = new Mock<IProfileCreditCard>();
            _baseEmployeeRes = new Mock<IBaseEmployeeResService>();
            _loyaltyUCBService = new Mock<ILoyaltyUCBService>();
            _customerProfileOwnerService = new Mock<ICustomerProfileOwnerService>();
            _sSOTokenKeyService = new Mock<ISSOTokenKeyService>();
            _shopBundleService = new Mock<IShopBundleService>();
            _provisionService = new Mock<IProvisionService>();
            _featureToggles = new Mock<IFeatureToggles>();
            _corpProfile = new Mock<ICorporateProfile>();
            _mPSecurityCheckDetailsService1 = new MPSecurityCheckDetailsService(_resilientClient, _logger6.Object);

            _insertOrUpdateTravelInfoService = new InsertOrUpdateTravelInfoService(_logger11.Object, _configuration, _resilientClient);

            //_mpTraveler1 = new MPTraveler(_configuration, _referencedataService.Object, _sessionHelperService.Object, _mpEnrollmentService.Object, _dataVaultService.Object, _utilitiesService.Object, _logger5.Object, _pNRRetrievalService.Object, _profileCreditCard.Object, _mPSecurityCheckDetailsService1, _baseEmployeeRes.Object, _eServiceCheckin.Object, _insertOrUpdateTravelInfoService, _loyaltyUCBService.Object, _customerProfileOwnerService.Object, _headers.Object, _legalDocumentService.Object);

            _referencedataService1 = new ReferencedataService(_logger9.Object, _resilientClient);

            _memberProfileUtility1 = new MemberProfileUtility(_logger7.Object, _configuration, _dynamoDBService.Object, _validateAccountFC.Object, _sessionHelperService.Object, _shoppingUtility.Object, _ffcShoppingcs.Object, _cachingService.Object, _validateHashPinService.Object, _headers.Object, _featureSettings.Object);

            _travelerUtility1 = new TravelerUtility(_logger8.Object, _configuration, _shoppingUtility.Object, _sessionHelperService.Object, _ffcShoppingcs.Object, _shoppingCartService.Object, _dynamoDBService.Object, _legalDocumentsForTitlesService.Object, _cachingService1, _shoppingBuyMiles.Object, _omniCart.Object, _getProductsService.Object,_headers.Object, _mlogger.Object, _featureToggles.Object);

            _dynamoDBService1 = new DynamoDBService(_resilientClient, _logger10.Object);

            _memberProfileBusiness = new MemberProfileBusiness(_logger.Object, _configuration, _dynamoDBService.Object, _sessionHelperService.Object, _memberProfileUtility.Object, _customerProfile.Object, _profileXML.Object, _merchandizingServices.Object, _statusLiftBannerService.Object, _dpService.Object, _shoppingUtility.Object, _mileagePlus.Object, _shoppingSessionHelper.Object, _validateHashPinService.Object, _legalDocumentsForTitlesService.Object, _cslStatisticsService.Object,
                _traveler.Object, _travelerUtility.Object, _ffcShoppingcs.Object, _paymentService.Object, _empProfile.Object,
                _cachingService.Object, _mpTraveler.Object, _omniCart.Object, _referencedataService.Object, _pKDispenserService.Object, _formsOfPayment.Object,_headers.Object,_sSOTokenKeyService.Object,_featureSettings.Object, _shopBundleService.Object, _provisionService.Object, _featureToggles.Object,_corpProfile.Object);

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
        [MemberData(nameof(Input.InputMemberProfile2), MemberType = typeof(Input))]
        public void GetContactUsDetails_Test(MOBContactUsRequest requestPayload)
        {
            var filename = GetFileContent("MOBLegalDocument.json");
            var List = JsonConvert.DeserializeObject<List<MOBLegalDocument>>(filename);
            var callingCard = JsonConvert.DeserializeObject<List<CallingCard>>(GetFileContent("CallingCard.json"));
            _dynamoDBService.Setup(p => p.GetRecords<List<MOBLegalDocument>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(List);
            _dynamoDBService.Setup(p => p.GetRecords<List<CallingCard>>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(callingCard);
            var result = _memberProfileBusiness.GetContactUsDetails(requestPayload);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(Input.InputMemberProfile), MemberType = typeof(Input))]
        public void RetrieveCustomerPreferences_Test(MOBCustomerPreferencesRequest requestPayload)
        {
            var session = GetFileContent("SessionData.json");
            var resp = GetFileContent("ResponseData.json");
            var responseData = JsonConvert.DeserializeObject<MOBCustomerPreferencesResponse>(resp);
            var sessionData = JsonConvert.DeserializeObject<Session>(session);
            _shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(sessionData);
            _customerProfile.Setup(p => p.RetrieveCustomerPreferences(It.IsAny<MOBCustomerPreferencesRequest>(), It.IsAny<string>())).ReturnsAsync(responseData);
            var result = _memberProfileBusiness.RetrieveCustomerPreferences(requestPayload);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.SessionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(Input.InputMemberProfile3), MemberType = typeof(Input))]
        public void GetProfileOwner(bool ok, bool val, MOBCustomerProfileRequest requestPayload)
        {

            var responseData = JsonConvert.DeserializeObject<MOBCustomerProfileResponse>(GetFileContent("MOBCustomerProfileResponse.json"));
            var sessionData = JsonConvert.DeserializeObject<Session>(GetFileContent("SessionData.json"));
            var cart = JsonConvert.DeserializeObject<MOBShoppingCart>(GetFileContent("MOBShoppingCart.json"));

            _memberProfileUtility.Setup(p => p.IsValidGetProfileCSLRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(ok);

            _memberProfileUtility.Setup(p => p.IsValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(ok);

            _memberProfileUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((val, "testing")));

            _memberProfileUtility.Setup(p => p.ValidateAccountFromCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(val);

            _customerProfile.Setup(p => p.GetProfile(It.IsAny<MOBCPProfileRequest>())).ReturnsAsync(responseData.Profiles);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(sessionData);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(sessionData);
            //_sessionHelperService.Setup(p => p.SaveSession<MOBCustomerProfileResponse>(It.IsAny<MOBCustomerProfileResponse>(), It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(sessionData);
            _shoppingUtility.Setup(p => p.IsManageResETCEnabled(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _shoppingUtility.Setup(p => p.InitialiseShoppingCartAndDevfaultValuesForETC(It.IsAny<MOBShoppingCart>(), It.IsAny<List<ProdDetail>>(), It.IsAny<string>()));

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cart);

            _mileagePlus.Setup(p => p.GetTravelCertificateResponseFromETC(It.IsAny<string>(), It.IsAny<MOBApplication>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            _dynamoDBService.Setup(p => p.GetRecords<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"AppVersion\":\"4.1.48\",\"ApplicationID\":\"2\",\"AuthenticatedToken\":null,\"CallDuration\":0,\"CustomerID\":\"123901727\",\"DataPowerAccessToken\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9.NNV8GJ9vnJkyYHShMUWinJEDnT9hMI9IDHksAwDmEGSSAQpgZGV0Zv00DwMQYHSPA0HgAwDjZwESARWQBQEQm_UZfeWVu4hFCrLSGjfTs8WRM4GadvbiNAMYdbxZoEh69D-IXfiLKeTCPU-4GE5RKBhYbkMOv0TrQzcMMhRx3TZVsqJDUMphaQTSKpAyFJUYriwVknmMvLXUrcYmDtLkXAuiEOgfNQWCUqqaUcY9HfqFtTcrIY03SjwHH296Ptu8FJ9OdNtnpEehMuNPLpYz.jwC0naNWpnrKScvZMHhSY2zEFTasGTG3JfCP1jhPoKxBeeuG_YkZkq15WhOLdA-erMVuY0e8MSqHEkQ3pNepiRHXo09f_Ht0f9PJfciIUOjA_haRN9x1WYfsd57mPCMZCLOrTI4tPDLbrFoyGFkElHLpmX1fly3mP_gR7ITMpM-s8Ynjr1XVxtZQ072wUOqfllxg8Dp17MPMdRD9VOpNMj-nXDAi0-9_vKE5d0Lm1xmDSh3R00DqkM0VQb2ScHfG5XChkjhux6vFm6Y8lgcgrfO6t5r-gM3Jq7DU6ZbT6Gk30d14PwAfm-35s5N5Bt39zDlBZ3wOcPjgZtnvGEk5Kt.rlW2MKVvBvVkYwNvYPWdqTxvBvWwAmLjAGOvBP1yLwIuYGEyLwRgLJL5Zv1xZzR1MGMuZwD0BJHvYPWmqJVvBvWwAmLjAGOvBP1yLwIuYGEyLwRgLJL5Zv1xZzR1MGMuZwD0BJHvYPWcp3ZvBvWbqUEjpmbiY2AmoJZhpJRhLKI0nP51ozy0MJDhL29gY29uqKEbZv92ZFVfVzS1MPV6Vx1iLzyfMF1OozElo2yxHTuiozIsIHSZKmL0Z0HkEGD3YGRlAQVgARV2Dl1ODwqSYGL0ZQV0EGEPDmt0DlVfVzyuqPV6ZGL1ZQHlBQR3ZljvMKujVwbkAwHjAGZ1ZmpmYPWuoKVvByfvpUqxVy0fVzS1qTusqTygMFV6ZGL1ZQHlBQR3Zljvoz9hL2HvBvVmnUpjMaN5pIuzqaRvYPWuqS9bLKAbVwbvHHqOIaykDHcUAwAmFSIDFUMZF1ScHG09VvjvqKOxLKEyMS9uqPV6ZGL1ZQHlBQR3ZljvL2kcMJ50K2SjpTkcL2S0nJ9hK2yxVwbvGJ9vnJkyYHShMUWinJEDnT9hMI9IDHksAwDmEGSSAQpgZGV0Zv00DwMQYHSPA0HgAwDjZwESARWQBQEQVvjvqKAypyE5pTHvBvWaqJImqPVfVzAfnJIhqS9cMPV6Vx1iLzyfMF1OozElo2yxHTuiozIsIHSZKmL0Z0HkEGD3YGRlAQVgARV2Dl1ODwqSYGL0ZQV0EGEPDmt0DlVfVzIhMSImMKWOM2IhqRyRVwbvAQD2MGx0Z2DgMQp0BF00MwWxYGuvMwVgMGOvLGIxAzEuAwt1VvjvMJ5xIKAypxSaMJ50FINvBvVkZwphZP4jYwRvsD\",\"DeviceID\":\"446e943d-d749-4f2d-8bf2-e0ba5d6da685\",\"HashPincode\":\"A5D3AFDAE0BF0E6543650D7B928EB77C94A4AD56\",\"IsTokenAnonymous\":\"False\",\"IsTokenValid\":\"True\",\"IsTouchIDSignIn\":\"False\",\"MPUserName\":\"AW719636\",\"MileagePlusNumber\":\"AW719636\",\"PinCode\":\"\",\"TokenExpireDateTime\":\"2022-04-2105:02:53.725\",\"TokenExpiryInSeconds\":\"7200\",\"UpdateDateTime\":\"2022-04-2103:02:54.565\"}");

            _cachingService.Setup(p => p.GetCache<string>(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("Tanya");


            // _pKDispenserPublicKey.Setup(p => p.GetCachedOrNewpkDispenserPublicKey(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns("");


            if (requestPayload.IsRequirePKDispenserPublicKey == false)
            {
                _configuration["ValidateGetProfileCSLRequest"] = "True";
            }


            var result = _memberProfileBusiness.GetProfileOwner(requestPayload);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.SessionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);

        }

        [Theory]
        [MemberData(nameof(Input.InputMemberProfile4), MemberType = typeof(Input))]
        public void GetProfileOwner1(bool ok, bool val, MOBCustomerProfileRequest requestPayload)
        {

            var responseData = JsonConvert.DeserializeObject<MOBCustomerProfileResponse>(GetFileContent("MOBCustomerProfileResponse.json"));
            var sessionData = JsonConvert.DeserializeObject<Session>(GetFileContent("SessionData.json"));
            var cart = JsonConvert.DeserializeObject<MOBShoppingCart>(GetFileContent("MOBShoppingCart.json"));

            _memberProfileUtility.Setup(p => p.IsValidGetProfileCSLRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(ok);

            _memberProfileUtility.Setup(p => p.IsValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(ok);

            _memberProfileUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((val, "testing")));

            _memberProfileUtility.Setup(p => p.ValidateAccountFromCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(val);

            _customerProfile.Setup(p => p.GetProfile(It.IsAny<MOBCPProfileRequest>())).ReturnsAsync(responseData.Profiles);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(sessionData);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(sessionData);
            //_sessionHelperService.Setup(p => p.SaveSession<MOBCustomerProfileResponse>(It.IsAny<MOBCustomerProfileResponse>(), It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(sessionData);
            _shoppingUtility.Setup(p => p.IsManageResETCEnabled(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _shoppingUtility.Setup(p => p.InitialiseShoppingCartAndDevfaultValuesForETC(It.IsAny<MOBShoppingCart>(), It.IsAny<List<ProdDetail>>(), It.IsAny<string>()));

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(cart);

            _mileagePlus.Setup(p => p.GetTravelCertificateResponseFromETC(It.IsAny<string>(), It.IsAny<MOBApplication>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));


            if (requestPayload.IsRequirePKDispenserPublicKey == false)
            {
                _configuration["ValidateGetProfileCSLRequest"] = "True";
            }

            var memberProfileBusiness = new MemberProfileBusiness(_logger.Object, _configuration, _dynamoDBService1, _sessionHelperService.Object, _memberProfileUtility.Object, _customerProfile.Object, _profileXML.Object, _merchandizingServices.Object,
                _statusLiftBannerService.Object, _dpService.Object, _shoppingUtility.Object, _mileagePlus.Object, _shoppingSessionHelper.Object, _validateHashPinService.Object,
                _legalDocumentsForTitlesService.Object, _cslStatisticsService.Object, _traveler.Object, _travelerUtility.Object, _ffcShoppingcs.Object, _paymentService.Object,
                _empProfile.Object, _cachingService1, _mpTraveler.Object, _omniCart.Object, _referencedataService.Object, _pKDispenserService.Object,  _formsOfPayment.Object,_headers.Object,_sSOTokenKeyService.Object,_featureSettings.Object, _shopBundleService.Object, _provisionService.Object, _featureToggles.Object, _corpProfile.Object);

            var result = memberProfileBusiness.GetProfileOwner(requestPayload);


            Assert.True(result.Exception != null || result.Result != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetProfileOwner_flow1), MemberType = typeof(TestDataGenerator))]
        public void GetProfileOwner_flow1( MOBCustomerProfileRequest mOBCustomerProfileRequest, MOBCustomerProfileResponse mOBCustomerProfileResponse, Session session, MOBShoppingCart mOBShoppingCart, HashPinValidate hashPinValidate)
        {

            _memberProfileUtility.Setup(p => p.IsValidGetProfileCSLRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);

            _memberProfileUtility.Setup(p => p.IsValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _memberProfileUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((true, "testing")));

            _memberProfileUtility.Setup(p => p.ValidateAccountFromCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _customerProfile.Setup(p => p.GetProfile(It.IsAny<MOBCPProfileRequest>())).ReturnsAsync(mOBCustomerProfileResponse.Profiles);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.IsManageResETCEnabled(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _shoppingUtility.Setup(p => p.InitialiseShoppingCartAndDevfaultValuesForETC(It.IsAny<MOBShoppingCart>(), It.IsAny<List<ProdDetail>>(), It.IsAny<string>()));

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _mileagePlus.Setup(p => p.GetTravelCertificateResponseFromETC(It.IsAny<string>(), It.IsAny<MOBApplication>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            _dynamoDBService.Setup(p => p.GetRecords<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"AppVersion\":\"4.1.48\",\"ApplicationID\":\"2\",\"AuthenticatedToken\":null,\"CallDuration\":0,\"CustomerID\":\"123901727\",\"DataPowerAccessToken\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9.NNV8GJ9vnJkyYHShMUWinJEDnT9hMI9IDHksAwDmEGSSAQpgZGV0Zv00DwMQYHSPA0HgAwDjZwESARWQBQEQm_UZfeWVu4hFCrLSGjfTs8WRM4GadvbiNAMYdbxZoEh69D-IXfiLKeTCPU-4GE5RKBhYbkMOv0TrQzcMMhRx3TZVsqJDUMphaQTSKpAyFJUYriwVknmMvLXUrcYmDtLkXAuiEOgfNQWCUqqaUcY9HfqFtTcrIY03SjwHH296Ptu8FJ9OdNtnpEehMuNPLpYz.jwC0naNWpnrKScvZMHhSY2zEFTasGTG3JfCP1jhPoKxBeeuG_YkZkq15WhOLdA-erMVuY0e8MSqHEkQ3pNepiRHXo09f_Ht0f9PJfciIUOjA_haRN9x1WYfsd57mPCMZCLOrTI4tPDLbrFoyGFkElHLpmX1fly3mP_gR7ITMpM-s8Ynjr1XVxtZQ072wUOqfllxg8Dp17MPMdRD9VOpNMj-nXDAi0-9_vKE5d0Lm1xmDSh3R00DqkM0VQb2ScHfG5XChkjhux6vFm6Y8lgcgrfO6t5r-gM3Jq7DU6ZbT6Gk30d14PwAfm-35s5N5Bt39zDlBZ3wOcPjgZtnvGEk5Kt.rlW2MKVvBvVkYwNvYPWdqTxvBvWwAmLjAGOvBP1yLwIuYGEyLwRgLJL5Zv1xZzR1MGMuZwD0BJHvYPWmqJVvBvWwAmLjAGOvBP1yLwIuYGEyLwRgLJL5Zv1xZzR1MGMuZwD0BJHvYPWcp3ZvBvWbqUEjpmbiY2AmoJZhpJRhLKI0nP51ozy0MJDhL29gY29uqKEbZv92ZFVfVzS1MPV6Vx1iLzyfMF1OozElo2yxHTuiozIsIHSZKmL0Z0HkEGD3YGRlAQVgARV2Dl1ODwqSYGL0ZQV0EGEPDmt0DlVfVzyuqPV6ZGL1ZQHlBQR3ZljvMKujVwbkAwHjAGZ1ZmpmYPWuoKVvByfvpUqxVy0fVzS1qTusqTygMFV6ZGL1ZQHlBQR3Zljvoz9hL2HvBvVmnUpjMaN5pIuzqaRvYPWuqS9bLKAbVwbvHHqOIaykDHcUAwAmFSIDFUMZF1ScHG09VvjvqKOxLKEyMS9uqPV6ZGL1ZQHlBQR3ZljvL2kcMJ50K2SjpTkcL2S0nJ9hK2yxVwbvGJ9vnJkyYHShMUWinJEDnT9hMI9IDHksAwDmEGSSAQpgZGV0Zv00DwMQYHSPA0HgAwDjZwESARWQBQEQVvjvqKAypyE5pTHvBvWaqJImqPVfVzAfnJIhqS9cMPV6Vx1iLzyfMF1OozElo2yxHTuiozIsIHSZKmL0Z0HkEGD3YGRlAQVgARV2Dl1ODwqSYGL0ZQV0EGEPDmt0DlVfVzIhMSImMKWOM2IhqRyRVwbvAQD2MGx0Z2DgMQp0BF00MwWxYGuvMwVgMGOvLGIxAzEuAwt1VvjvMJ5xIKAypxSaMJ50FINvBvVkZwphZP4jYwRvsD\",\"DeviceID\":\"446e943d-d749-4f2d-8bf2-e0ba5d6da685\",\"HashPincode\":\"A5D3AFDAE0BF0E6543650D7B928EB77C94A4AD56\",\"IsTokenAnonymous\":\"False\",\"IsTokenValid\":\"True\",\"IsTouchIDSignIn\":\"False\",\"MPUserName\":\"AW719636\",\"MileagePlusNumber\":\"AW719636\",\"PinCode\":\"\",\"TokenExpireDateTime\":\"2022-04-2105:02:53.725\",\"TokenExpiryInSeconds\":\"7200\",\"UpdateDateTime\":\"2022-04-2103:02:54.565\"}");

            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinValidate);

           // _cachingService.Setup(p => p.GetCache<string>(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("Tanya");

            _cachingService.Setup(p => p.GetCache<string>(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"PublicKey\":\"1~QA_DP_1NewPublicKeyPersistStatSesion4IphoneApp\",\"CryptoTypeID\":\"2\",\"Kid\":\"PK7654\",\"Exp\":\"qweryi\"}");


            if (mOBCustomerProfileRequest.IsRequirePKDispenserPublicKey == false)
            {
                _configuration["ValidateGetProfileCSLRequest"] = "True";
            }


            var result = _memberProfileBusiness.GetProfileOwner(mOBCustomerProfileRequest);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.SessionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetProfileOwner_flow2), MemberType = typeof(TestDataGenerator))]
        public void GetProfileOwner_flow2(MOBCustomerProfileRequest mOBCustomerProfileRequest, MOBCustomerProfileResponse mOBCustomerProfileResponse, Session session, MOBShoppingCart mOBShoppingCart, HashPinValidate hashPinValidate)
        {

            _memberProfileUtility.Setup(p => p.IsValidGetProfileCSLRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);

            _memberProfileUtility.Setup(p => p.IsValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _memberProfileUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((true, "testing")));

            _memberProfileUtility.Setup(p => p.ValidateAccountFromCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _customerProfile.Setup(p => p.GetProfile(It.IsAny<MOBCPProfileRequest>())).ReturnsAsync(mOBCustomerProfileResponse.Profiles);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.IsManageResETCEnabled(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _shoppingUtility.Setup(p => p.InitialiseShoppingCartAndDevfaultValuesForETC(It.IsAny<MOBShoppingCart>(), It.IsAny<List<ProdDetail>>(), It.IsAny<string>()));

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _mileagePlus.Setup(p => p.GetTravelCertificateResponseFromETC(It.IsAny<string>(), It.IsAny<MOBApplication>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()));

            _dynamoDBService.Setup(p => p.GetRecords<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"AppVersion\":\"4.1.48\",\"ApplicationID\":\"2\",\"AuthenticatedToken\":null,\"CallDuration\":0,\"CustomerID\":\"123901727\",\"DataPowerAccessToken\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9.NNV8GJ9vnJkyYHShMUWinJEDnT9hMI9IDHksAwDmEGSSAQpgZGV0Zv00DwMQYHSPA0HgAwDjZwESARWQBQEQm_UZfeWVu4hFCrLSGjfTs8WRM4GadvbiNAMYdbxZoEh69D-IXfiLKeTCPU-4GE5RKBhYbkMOv0TrQzcMMhRx3TZVsqJDUMphaQTSKpAyFJUYriwVknmMvLXUrcYmDtLkXAuiEOgfNQWCUqqaUcY9HfqFtTcrIY03SjwHH296Ptu8FJ9OdNtnpEehMuNPLpYz.jwC0naNWpnrKScvZMHhSY2zEFTasGTG3JfCP1jhPoKxBeeuG_YkZkq15WhOLdA-erMVuY0e8MSqHEkQ3pNepiRHXo09f_Ht0f9PJfciIUOjA_haRN9x1WYfsd57mPCMZCLOrTI4tPDLbrFoyGFkElHLpmX1fly3mP_gR7ITMpM-s8Ynjr1XVxtZQ072wUOqfllxg8Dp17MPMdRD9VOpNMj-nXDAi0-9_vKE5d0Lm1xmDSh3R00DqkM0VQb2ScHfG5XChkjhux6vFm6Y8lgcgrfO6t5r-gM3Jq7DU6ZbT6Gk30d14PwAfm-35s5N5Bt39zDlBZ3wOcPjgZtnvGEk5Kt.rlW2MKVvBvVkYwNvYPWdqTxvBvWwAmLjAGOvBP1yLwIuYGEyLwRgLJL5Zv1xZzR1MGMuZwD0BJHvYPWmqJVvBvWwAmLjAGOvBP1yLwIuYGEyLwRgLJL5Zv1xZzR1MGMuZwD0BJHvYPWcp3ZvBvWbqUEjpmbiY2AmoJZhpJRhLKI0nP51ozy0MJDhL29gY29uqKEbZv92ZFVfVzS1MPV6Vx1iLzyfMF1OozElo2yxHTuiozIsIHSZKmL0Z0HkEGD3YGRlAQVgARV2Dl1ODwqSYGL0ZQV0EGEPDmt0DlVfVzyuqPV6ZGL1ZQHlBQR3ZljvMKujVwbkAwHjAGZ1ZmpmYPWuoKVvByfvpUqxVy0fVzS1qTusqTygMFV6ZGL1ZQHlBQR3Zljvoz9hL2HvBvVmnUpjMaN5pIuzqaRvYPWuqS9bLKAbVwbvHHqOIaykDHcUAwAmFSIDFUMZF1ScHG09VvjvqKOxLKEyMS9uqPV6ZGL1ZQHlBQR3ZljvL2kcMJ50K2SjpTkcL2S0nJ9hK2yxVwbvGJ9vnJkyYHShMUWinJEDnT9hMI9IDHksAwDmEGSSAQpgZGV0Zv00DwMQYHSPA0HgAwDjZwESARWQBQEQVvjvqKAypyE5pTHvBvWaqJImqPVfVzAfnJIhqS9cMPV6Vx1iLzyfMF1OozElo2yxHTuiozIsIHSZKmL0Z0HkEGD3YGRlAQVgARV2Dl1ODwqSYGL0ZQV0EGEPDmt0DlVfVzIhMSImMKWOM2IhqRyRVwbvAQD2MGx0Z2DgMQp0BF00MwWxYGuvMwVgMGOvLGIxAzEuAwt1VvjvMJ5xIKAypxSaMJ50FINvBvVkZwphZP4jYwRvsD\",\"DeviceID\":\"446e943d-d749-4f2d-8bf2-e0ba5d6da685\",\"HashPincode\":\"A5D3AFDAE0BF0E6543650D7B928EB77C94A4AD56\",\"IsTokenAnonymous\":\"False\",\"IsTokenValid\":\"True\",\"IsTouchIDSignIn\":\"False\",\"MPUserName\":\"AW719636\",\"MileagePlusNumber\":\"AW719636\",\"PinCode\":\"\",\"TokenExpireDateTime\":\"2022-04-2105:02:53.725\",\"TokenExpiryInSeconds\":\"7200\",\"UpdateDateTime\":\"2022-04-2103:02:54.565\"}");

            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinValidate);

            //  _cachingService.Setup(p => p.GetCache<string>(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("Tanya");

            _cachingService.Setup(p => p.GetCache<string>(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"PublicKey\":\"1~QA_DP_1NewPublicKeyPersistStatSesion4IphoneApp\",\"CryptoTypeID\":\"2\",\"Kid\":\"PK7654\",\"Exp\":\"qweryi\"}");


            if (mOBCustomerProfileRequest.IsRequirePKDispenserPublicKey == false)
            {
                _configuration["ValidateGetProfileCSLRequest"] = "True";
            }


            var result = _memberProfileBusiness.GetProfileOwner(mOBCustomerProfileRequest);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.SessionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);

        }

        [Theory]
        [MemberData(nameof(Input.InputMemberProfile1), MemberType = typeof(Input))]
        public void GetAccountSummaryWithMemberCardPremierActivity_Test(bool ok, MPAccountValidationRequest requestPayload, MPAccountSummaryResponse response)
        {
            var profile = JsonConvert.DeserializeObject<MOBProfile>(GetFileContent("MOBProfile.json"));
            var resp = JsonConvert.DeserializeObject<MemberPromotion<MOBStatusLiftBannerResponse>>(GetFileContent("MOBStatusLiftBannerResponse.json"));

            _memberProfileUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((ok,"testing")));
            _merchandizingServices.Setup(p => p.GetUASubscriptions(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response.UASubscriptions);
            _memberProfileUtility.Setup(p => p.IsTSAFlaggedAccount(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response.OPAccountSummary.IsMPAccountTSAFlagON);
            _profileXML.Setup(p => p.GetProfile(It.IsAny<MOBProfileRequest>())).ReturnsAsync(profile);
            _profileXML.Setup(p => p.GetOwnerProfileForMP2014(It.IsAny<string>()));
            _dpService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Sample Token");
            _merchandizingServices.Setup(p => p.GetUASubscriptions(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response.UASubscriptions);
            _mileagePlus.Setup(p => p.GetAccountSummaryWithPremierActivity(It.IsAny<MPAccountValidationRequest>(), It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync(response.OPAccountSummary);
            //_memberProfileUtility.Setup(p => p.isApplicationVersionGreater2(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(false);
            _mileagePlus.Setup(p => p.GetProfile_AllTravelerData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response.OPAccountSummary.IsMPAccountTSAFlagON);
            _statusLiftBannerService.Setup(p => p.GetStatusLiftBanner(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(resp));
            if (response.OPAccountSummary.IsCEO)
                _configuration["NewServieCall_GetProfile_AllTravelerData"] = "false";
            var result = _memberProfileBusiness.GetAccountSummaryWithMemberCardPremierActivity(requestPayload);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(Input.InputMemberProfile1), MemberType = typeof(Input))]
        public void GetAccountSummaryWithMemberCardPremierActivity_Test_Exception(bool ok, MPAccountValidationRequest requestPayload, MPAccountSummaryResponse response)
        {
            var profile = JsonConvert.DeserializeObject<MOBProfile>(GetFileContent("MOBProfile.json"));

            _memberProfileUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((ok, "testing")));
            _merchandizingServices.Setup(p => p.GetUASubscriptions(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response.UASubscriptions);
            _memberProfileUtility.Setup(p => p.IsTSAFlaggedAccount(It.IsAny<string>(),It.IsAny<string>())).ReturnsAsync(response.OPAccountSummary.IsMPAccountTSAFlagON);
            _profileXML.Setup(p => p.GetProfile(It.IsAny<MOBProfileRequest>())).Throws(new System.Exception("Error Ocurred"));
            _profileXML.Setup(p => p.GetOwnerProfileForMP2014(It.IsAny<string>()));
            _dpService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Sample Token");
            _merchandizingServices.Setup(p => p.GetUASubscriptions(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response.UASubscriptions);
            _mileagePlus.Setup(p => p.GetAccountSummaryWithPremierActivity(It.IsAny<MPAccountValidationRequest>(), It.IsAny<bool>(), It.IsAny<string>())).ReturnsAsync(response.OPAccountSummary);
            _memberProfileUtility.Setup(p => p.isApplicationVersionGreater2(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);
            _mileagePlus.Setup(p => p.GetProfile_AllTravelerData(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new System.Exception("Error Ocurred"));
            if (response.OPAccountSummary.IsCEO)
                _configuration["NewServieCall_GetProfile_AllTravelerData"] = "false";
            var result = _memberProfileBusiness.GetAccountSummaryWithMemberCardPremierActivity(requestPayload);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetProfileCSL_CFOP_Request), MemberType = typeof(TestDataGenerator))]
        public void GetProfileCSL_CFOP(MOBCPProfileRequest request, Session session, MOBCustomerProfileResponse mOBCustomerProfileResponse, Reservation reservation, ProfileResponse profileResponse, MOBCPProfileResponse mOBCPProfileResponse, MOBShoppingCart mOBShoppingCart)
        {

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _memberProfileUtility.Setup(p => p.IsValidGetProfileCSLRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(true);

            _memberProfileUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((true, "testing")));

            _memberProfileUtility.Setup(p => p.ValidateAccountFromCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _customerProfile.Setup(p => p.GetProfile(It.IsAny<MOBCPProfileRequest>())).ReturnsAsync(mOBCustomerProfileResponse.Profiles);

            _memberProfileUtility.Setup(p => p.IsValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<ProfileResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(profileResponse);

            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(mOBCPProfileResponse.Reservation);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _shoppingUtility.Setup(p => p.ReservationToShoppingCart_DataMigration(It.IsAny<MOBSHOPReservation>(), It.IsAny<MOBShoppingCart>(), It.IsAny<MOBRequest>(), It.IsAny<Session>())).ReturnsAsync(mOBShoppingCart);

            var _memberProfileBusiness = new MemberProfileBusiness(_logger.Object, _configuration, _dynamoDBService.Object, _sessionHelperService.Object, _memberProfileUtility.Object,
                _customerProfile.Object, _profileXML.Object, _merchandizingServices.Object, _statusLiftBannerService.Object, _dpService.Object, _shoppingUtility.Object,
                _mileagePlus.Object, _shoppingSessionHelper.Object, _validateHashPinService.Object, _legalDocumentsForTitlesService.Object, _cslStatisticsService.Object,
                _traveler.Object, _travelerUtility.Object, _ffcShoppingcs.Object, _paymentService.Object, _empProfile.Object, _cachingService1, _mpTraveler.Object,
                _omniCart.Object, _referencedataService.Object, _pKDispenserService.Object, _formsOfPayment.Object,_headers.Object,_sSOTokenKeyService.Object,_featureSettings.Object, _shopBundleService.Object, _provisionService.Object, _featureToggles.Object, _corpProfile.Object);

            var result = _memberProfileBusiness.GetProfileCSL_CFOP(request);

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
        [MemberData(nameof(TestDataGenerator.GetProfileCSL_CFOP_flow), MemberType = typeof(TestDataGenerator))]
        public void GetProfileCSL_CFOP_flow(MOBCPProfileRequest request, Session session, MOBCustomerProfileResponse mOBCustomerProfileResponse, Reservation reservation, ProfileResponse profileResponse, MOBCPProfileResponse mOBCPProfileResponse, MOBShoppingCart mOBShoppingCart, SelectTrip selectTrip)
        {

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _memberProfileUtility.Setup(p => p.IsValidGetProfileCSLRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<string>())).ReturnsAsync(true);

            _memberProfileUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((true, "testing")));

            _memberProfileUtility.Setup(p => p.ValidateAccountFromCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _customerProfile.Setup(p => p.GetProfile(It.IsAny<MOBCPProfileRequest>())).ReturnsAsync(mOBCustomerProfileResponse.Profiles);

            _memberProfileUtility.Setup(p => p.IsValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<ProfileResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(profileResponse);

            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(mOBCPProfileResponse.Reservation);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _shoppingUtility.Setup(p => p.ReservationToShoppingCart_DataMigration(It.IsAny<MOBSHOPReservation>(), It.IsAny<MOBShoppingCart>(), It.IsAny<MOBRequest>(), It.IsAny<Session>())).ReturnsAsync(mOBShoppingCart);

            _memberProfileUtility.Setup(p => p.EnableChaseOfferRTI(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);

            _shoppingUtility.Setup(p => p.EnableSpecialNeeds(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            var _memberProfileBusiness = new MemberProfileBusiness(_logger.Object, _configuration, _dynamoDBService.Object, _sessionHelperService.Object, _memberProfileUtility.Object,
                _customerProfile.Object, _profileXML.Object, _merchandizingServices.Object, _statusLiftBannerService.Object, _dpService.Object, _shoppingUtility.Object, _mileagePlus.Object, _shoppingSessionHelper.Object, _validateHashPinService.Object, _legalDocumentsForTitlesService.Object, _cslStatisticsService.Object, _traveler.Object, _travelerUtility.Object, _ffcShoppingcs.Object, _paymentService.Object, _empProfile.Object, _cachingService1, _mpTraveler.Object, _omniCart.Object, _referencedataService.Object, _pKDispenserService.Object, _formsOfPayment.Object, _headers.Object, _sSOTokenKeyService.Object,_featureSettings.Object, _shopBundleService.Object, _provisionService.Object, _featureToggles.Object, _corpProfile.Object);

            
            var result = _memberProfileBusiness.GetProfileCSL_CFOP(request);

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
        [MemberData(nameof(TestDataGenerator.GetEmpProfileCSL_CFOP_Request), MemberType = typeof(TestDataGenerator))]
        public void GetEmpProfileCSL_CFOP(MOBCPProfileRequest request, Session session, Reservation reservation, SelectTrip selectTrip, MOBCPProfileResponse mOBCPProfileResponse, ProfileResponse profileResponse, MOBShoppingCart mOBShoppingCart)
        {

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _memberProfileUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((true, "testing")));

            _memberProfileUtility.Setup(p => p.ValidateAccountFromCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);

            _shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);

            _empProfile.Setup(p => p.GetEmpProfile(It.IsAny<MOBCPProfileRequest>(), It.IsAny<bool>())).ReturnsAsync(mOBCPProfileResponse.Profiles);

            _sessionHelperService.Setup(p => p.GetSession<ProfileResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(profileResponse);

            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(mOBCPProfileResponse.Reservation);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _shoppingUtility.Setup(p => p.ReservationToShoppingCart_DataMigration(It.IsAny<MOBSHOPReservation>(), It.IsAny<MOBShoppingCart>(), It.IsAny<MOBRequest>(), It.IsAny<Session>())).ReturnsAsync(mOBShoppingCart);

            var _memberProfileBusiness = new MemberProfileBusiness(_logger.Object, _configuration, _dynamoDBService.Object, _sessionHelperService.Object, _memberProfileUtility.Object,
                _customerProfile.Object, _profileXML.Object, _merchandizingServices.Object, _statusLiftBannerService.Object, _dpService.Object, 
                _shoppingUtility.Object, _mileagePlus.Object, _shoppingSessionHelper.Object, _validateHashPinService.Object, _legalDocumentsForTitlesService.Object,
                _cslStatisticsService.Object, _traveler.Object, _travelerUtility.Object, _ffcShoppingcs.Object, _paymentService.Object, _empProfile.Object, _cachingService1,
                _mpTraveler.Object, _omniCart.Object, _referencedataService.Object, _pKDispenserService.Object, _formsOfPayment.Object,_headers.Object,_sSOTokenKeyService.Object,_featureSettings.Object, _shopBundleService.Object, _provisionService.Object, _featureToggles.Object, _corpProfile.Object);


            var result = _memberProfileBusiness.GetEmpProfileCSL_CFOP(request);

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
        [MemberData(nameof(TestDataGenerator.GetEmpProfileCSL_CFOP_Flow), MemberType = typeof(TestDataGenerator))]
        public void GetEmpProfileCSL_CFOP_Flow(MOBCPProfileRequest request, Session session, Reservation reservation, SelectTrip selectTrip, MOBCPProfileResponse mOBCPProfileResponse, ProfileResponse profileResponse, MOBShoppingCart mOBShoppingCart)
        {

            _shoppingSessionHelper.Setup(p => p.CreateShoppingSession(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(session);

            _memberProfileUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((true, "testing")));

            _memberProfileUtility.Setup(p => p.ValidateAccountFromCache(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<SelectTrip>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(selectTrip);

            _shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);

            _empProfile.Setup(p => p.GetEmpProfile(It.IsAny<MOBCPProfileRequest>(), It.IsAny<bool>())).ReturnsAsync(mOBCPProfileResponse.Profiles);

            _sessionHelperService.Setup(p => p.GetSession<ProfileResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(profileResponse);

            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(mOBCPProfileResponse.Reservation);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _shoppingUtility.Setup(p => p.ReservationToShoppingCart_DataMigration(It.IsAny<MOBSHOPReservation>(), It.IsAny<MOBShoppingCart>(), It.IsAny<MOBRequest>(), It.IsAny<Session>())).ReturnsAsync(mOBShoppingCart);

            var _memberProfileBusiness = new MemberProfileBusiness(_logger.Object, _configuration, _dynamoDBService.Object, _sessionHelperService.Object, _memberProfileUtility.Object,
                _customerProfile.Object, _profileXML.Object, _merchandizingServices.Object, _statusLiftBannerService.Object, _dpService.Object,
                _shoppingUtility.Object, _mileagePlus.Object, _shoppingSessionHelper.Object, _validateHashPinService.Object, _legalDocumentsForTitlesService.Object,
                _cslStatisticsService.Object, _traveler.Object, _travelerUtility.Object, _ffcShoppingcs.Object, _paymentService.Object, _empProfile.Object, _cachingService1,
                _mpTraveler.Object, _omniCart.Object, _referencedataService.Object, _pKDispenserService.Object, _formsOfPayment.Object, _headers.Object, _sSOTokenKeyService.Object,_featureSettings.Object, _shopBundleService.Object, _provisionService.Object, _featureToggles.Object, _corpProfile.Object);


            var result = _memberProfileBusiness.GetEmpProfileCSL_CFOP(request);

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
        [MemberData(nameof(TestDataGenerator.MPSignedInInsertUpdateTraveler_CFOP_Request), MemberType = typeof(TestDataGenerator))]
        public void MPSignedInInsertUpdateTraveler_CFOP(MOBUpdateTravelerRequest request, Session session, Reservation reservation, ProfileResponse profileResponse, MOBCPProfileResponse mOBCPProfileResponse, United.Services.Customer.Common.SaveResponse saveResponse)
        {

            _memberProfileUtility.Setup(p => p.IsValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);

            _shoppingUtility.Setup(p => p.EnableSpecialNeeds(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<ProfileResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(profileResponse);

            _customerProfile.Setup(p => p.GetProfile(It.IsAny<MOBCPProfileRequest>())).ReturnsAsync(mOBCPProfileResponse.Profiles);
            _mpTraveler.Setup(p => p.InsertTraveler(It.IsAny<InsertTravelerRequest>(), It.IsAny<List<MOBItem>>())).Returns(Task.FromResult((true,new List<MOBItem>() { } )));

            var _memberProfileBusiness = new MemberProfileBusiness(_logger.Object, _configuration, _dynamoDBService.Object, _sessionHelperService.Object, _memberProfileUtility.Object, 
                _customerProfile.Object, _profileXML.Object, _merchandizingServices.Object, _statusLiftBannerService.Object, _dpService.Object, _shoppingUtility.Object,
                _mileagePlus.Object, _shoppingSessionHelper.Object, _validateHashPinService.Object, _legalDocumentsForTitlesService.Object, _cslStatisticsService.Object,
                _traveler.Object, _travelerUtility.Object, _ffcShoppingcs.Object, _paymentService.Object, _empProfile.Object, _cachingService1, _mpTraveler.Object,
                _omniCart.Object, _referencedataService.Object, _pKDispenserService.Object, _formsOfPayment.Object,_headers.Object,_sSOTokenKeyService.Object,_featureSettings.Object, _shopBundleService.Object, _provisionService.Object, _featureToggles.Object, _corpProfile.Object);

            var result = _memberProfileBusiness.MPSignedInInsertUpdateTraveler_CFOP(request);

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
        [MemberData(nameof(TestDataGenerator.GetLatestFrequentFlyerRewardProgramList_Request), MemberType = typeof(TestDataGenerator))]
        public void GetLatestFrequentFlyerRewardProgramList(Session session, Reservation reservation)
        {

            _shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);

            _dpService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Sample Token");

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);


            Collection<Program> programs = new Collection<Program>()
            {
                new Program()
                {
                    Code = "AWR123",
                    Description = "try again",
                    ProgramID = 1,
                    Language = new Service.Presentation.CommonModel.Language()
                    {
                        CharacterSet = "avf",
                        LanguageCode = "en-US",
                        Name =  "XYZ"
                    },
                    Type = new Service.Presentation.CommonModel.Genre()
                    {
                        Key = "K765",
                        Description = "try again",
                        DefaultIndicator = "true",
                        DisplaySequence = 1,
                        Value = "yes"
                    },
                    Vendor = new Service.Presentation.CommonModel.VendorModel.Vendor()
                    {
                        Code = "AW12",
                        DisplaySequence = 2,
                        Id = "2",
                        IsGovernmentMustRide = true,
                        IsNOCMustRide = false,
                        Name = "XYZ"
                    }

                }
            };
            // var rewardProgramResponse1 = JsonConvert.SerializeObject(programs);

            var result1 = (programs, 13456789032134);

            _referencedataService.Setup(p => p.RewardPrograms<Collection<Program>>(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult(result1));



            var _memberProfileBusiness = new MemberProfileBusiness(_logger.Object, _configuration, _dynamoDBService.Object, _sessionHelperService.Object, _memberProfileUtility1,
                _customerProfile.Object, _profileXML.Object, _merchandizingServices.Object, _statusLiftBannerService.Object, _dpService.Object, _shoppingUtility.Object,
                _mileagePlus.Object, _shoppingSessionHelper.Object, _validateHashPinService.Object, _legalDocumentsForTitlesService.Object, _cslStatisticsService.Object,
                _traveler.Object, _travelerUtility.Object, _ffcShoppingcs.Object, _paymentService.Object, _empProfile.Object, _cachingService1, _mpTraveler1, _omniCart.Object,
                _referencedataService.Object, _pKDispenserService.Object, _formsOfPayment.Object,_headers.Object,_sSOTokenKeyService.Object,_featureSettings.Object, _shopBundleService.Object, _provisionService.Object, _featureToggles.Object, _corpProfile.Object);

            int applicationId = 1;
            string appVersion = "4.1.8";
            string accessCode = "ACCESSCODE";
            string transactionId = "605b2552 - 9609 - 407b - bf32 - 99246cb13343 | 2b6f0fef - 5c9d - 4175 - a8b1 - 62859af35a94";
            string languageCode = "en-US";

            var result = _memberProfileBusiness.GetLatestFrequentFlyerRewardProgramList(applicationId, appVersion, accessCode, transactionId, languageCode);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetLatestFrequentFlyerRewardProgramList_Request1), MemberType = typeof(TestDataGenerator))]
        public void GetLatestFrequentFlyerRewardProgramList1(Session session, Reservation reservation)
        {

            _shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);

            _dpService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Sample Token");

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            List<RewardProgram> rewardPrograms = new List<RewardProgram>()
            {
                new RewardProgram ()
                {
                    Description = "try again",
                    ProgramID = "1",
                    Type = "abcd"
                },
                new RewardProgram ()
                {
                    Description = "try again",
                    ProgramID = "4",
                    Type = "xyz"
                }
            };

            var response1 = JsonConvert.SerializeObject(rewardPrograms);

            _cachingService.Setup(p => p.GetCache<string>(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response1);


            var _memberProfileBusiness = new MemberProfileBusiness(_logger.Object, _configuration, _dynamoDBService.Object, _sessionHelperService.Object, _memberProfileUtility1, 
                _customerProfile.Object, _profileXML.Object, _merchandizingServices.Object, _statusLiftBannerService.Object, _dpService.Object, _shoppingUtility.Object,
                _mileagePlus.Object, _shoppingSessionHelper.Object, _validateHashPinService.Object, _legalDocumentsForTitlesService.Object, _cslStatisticsService.Object,
                _traveler.Object, _travelerUtility.Object, _ffcShoppingcs.Object, _paymentService.Object, _empProfile.Object, _cachingService.Object, _mpTraveler1, 
                _omniCart.Object, _referencedataService.Object, _pKDispenserService.Object, _formsOfPayment.Object,_headers.Object,_sSOTokenKeyService.Object,_featureSettings.Object, _shopBundleService.Object, _provisionService.Object, _featureToggles.Object, _corpProfile.Object);

            int applicationId = 1;
            string appVersion = "4.1.8";
            string accessCode = "ACCESSCODE";
            string transactionId = "605b2552 - 9609 - 407b - bf32 - 99246cb13343 | 2b6f0fef - 5c9d - 4175 - a8b1 - 62859af35a94";
            string languageCode = "en-US";

            var result = _memberProfileBusiness.GetLatestFrequentFlyerRewardProgramList(applicationId, appVersion, accessCode, transactionId, languageCode);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
            // Assert.True(result.Result != null);
        }


        [Theory]
        [MemberData(nameof(TestDataGenerator.UpdateTravelerCCPromo_CFOP_Request), MemberType = typeof(TestDataGenerator))]
        public void UpdateTravelerCCPromo_CFOP(MOBUpdateTravelerRequest request, Session session, Reservation reservation, ProfileResponse profileResponse, MOBCPProfileResponse mOBCPProfileResponse, MOBShoppingCart mOBShoppingCart)
        {

            _memberProfileUtility.Setup(p => p.IsValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);

            _dpService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Sample Token");

            _shoppingUtility.Setup(p => p.EnableSpecialNeeds(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<ProfileResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(profileResponse);

            _customerProfile.Setup(p => p.GetProfile(It.IsAny<MOBCPProfileRequest>())).ReturnsAsync(mOBCPProfileResponse.Profiles);

            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(mOBCPProfileResponse.Reservation);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _shoppingUtility.Setup(p => p.ReservationToShoppingCart_DataMigration(It.IsAny<MOBSHOPReservation>(), It.IsAny<MOBShoppingCart>(), It.IsAny<MOBRequest>(), It.IsAny<Session>())).ReturnsAsync(mOBShoppingCart);


            var _memberProfileBusiness = new MemberProfileBusiness(_logger.Object, _configuration, _dynamoDBService.Object, _sessionHelperService.Object, _memberProfileUtility1, 
                _customerProfile.Object, _profileXML.Object, _merchandizingServices.Object, _statusLiftBannerService.Object, _dpService.Object, _shoppingUtility.Object,
                _mileagePlus.Object, _shoppingSessionHelper.Object, _validateHashPinService.Object, _legalDocumentsForTitlesService.Object, _cslStatisticsService.Object,
                _traveler.Object, _travelerUtility1, _ffcShoppingcs.Object, _paymentService.Object, _empProfile.Object, _cachingService1, _mpTraveler1, _omniCart.Object,
                _referencedataService.Object, _pKDispenserService.Object, _formsOfPayment.Object,_headers.Object,_sSOTokenKeyService.Object,_featureSettings.Object, _shopBundleService.Object, _provisionService.Object, _featureToggles.Object, _corpProfile.Object);

            var result = _memberProfileBusiness.UpdateTravelerCCPromo_CFOP(request);

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
        [MemberData(nameof(TestDataGenerator.UpdateTravelersInformation_Request), MemberType = typeof(TestDataGenerator))]
        public void UpdateTravelersInformation(MOBUpdateTravelerInfoRequest request, Session session, Reservation reservation, ProfileResponse profileResponse, MOBCPProfileResponse mOBCPProfileResponse, MOBShoppingCart mOBShoppingCart, MOBUpdateTravelerInfoResponse mOBUpdateTravelerInfoResponse)
        {

            _memberProfileUtility.Setup(p => p.IsValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);

            _dpService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Sample Token");

            _shoppingUtility.Setup(p => p.EnableSpecialNeeds(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<ProfileResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(profileResponse);

            _customerProfile.Setup(p => p.GetProfile(It.IsAny<MOBCPProfileRequest>())).ReturnsAsync(mOBCPProfileResponse.Profiles);

            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(mOBCPProfileResponse.Reservation);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _mpTraveler.Setup(p => p.UpdateTravelerInfo(It.IsAny<MOBUpdateTravelerInfoRequest>(), It.IsAny<string>())).ReturnsAsync(mOBUpdateTravelerInfoResponse);



            var _memberProfileBusiness = new MemberProfileBusiness(_logger.Object, _configuration, _dynamoDBService.Object, _sessionHelperService.Object, _memberProfileUtility1, 
                _customerProfile.Object, _profileXML.Object, _merchandizingServices.Object, _statusLiftBannerService.Object, _dpService.Object, _shoppingUtility.Object, 
                _mileagePlus.Object, _shoppingSessionHelper.Object, _validateHashPinService.Object, _legalDocumentsForTitlesService.Object, _cslStatisticsService.Object, 
                _traveler.Object, _travelerUtility.Object, _ffcShoppingcs.Object, _paymentService.Object, _empProfile.Object, _cachingService1, _mpTraveler.Object, _omniCart.Object, 
                _referencedataService.Object, _pKDispenserService.Object, _formsOfPayment.Object,_headers.Object,_sSOTokenKeyService.Object,_featureSettings.Object, _shopBundleService.Object, _provisionService.Object, _featureToggles.Object, _corpProfile.Object);

            var result = _memberProfileBusiness.UpdateTravelersInformation(request);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
            // Assert.True(result.Result != null);
        }


        [Theory]
        [MemberData(nameof(TestDataGenerator.UpdateTravelersInformation_Request1), MemberType = typeof(TestDataGenerator))]
        public void UpdateTravelersInformation1(MOBUpdateTravelerInfoRequest request, Session session, Reservation reservation, ProfileResponse profileResponse, MOBCPProfileResponse mOBCPProfileResponse, MOBShoppingCart mOBShoppingCart, MOBUpdateTravelerInfoResponse mOBUpdateTravelerInfoResponse)
        {

            _memberProfileUtility.Setup(p => p.IsValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            //_shoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _dpService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Sample Token");

            _shoppingUtility.Setup(p => p.EnableSpecialNeeds(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);

            _sessionHelperService.Setup(p => p.GetSession<ProfileResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(profileResponse);

            _customerProfile.Setup(p => p.GetProfile(It.IsAny<MOBCPProfileRequest>())).ReturnsAsync(mOBCPProfileResponse.Profiles);

            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(mOBCPProfileResponse.Reservation);

            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);

            _mpTraveler.Setup(p => p.UpdateTravelerInfo(It.IsAny<MOBUpdateTravelerInfoRequest>(), It.IsAny<string>())).ReturnsAsync(mOBUpdateTravelerInfoResponse);



            var _memberProfileBusiness = new MemberProfileBusiness(_logger.Object, _configuration, _dynamoDBService.Object, _sessionHelperService.Object, _memberProfileUtility1,
                _customerProfile.Object, _profileXML.Object, _merchandizingServices.Object, _statusLiftBannerService.Object, _dpService.Object, _shoppingUtility.Object,
                _mileagePlus.Object, _shoppingSessionHelper.Object, _validateHashPinService.Object, _legalDocumentsForTitlesService.Object, _cslStatisticsService.Object,
                _traveler.Object, _travelerUtility.Object, _ffcShoppingcs.Object, _paymentService.Object, _empProfile.Object, _cachingService1, _mpTraveler1, _omniCart.Object,
                _referencedataService.Object, _pKDispenserService.Object, _formsOfPayment.Object, _headers.Object, _sSOTokenKeyService.Object,_featureSettings.Object, _shopBundleService.Object, _provisionService.Object, _featureToggles.Object, _corpProfile.Object);

            var result = _memberProfileBusiness.UpdateTravelersInformation(request);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
            // Assert.True(result.Result != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.UpdateTravelersInformation_Flow), MemberType = typeof(TestDataGenerator))]
        public void UpdateTravelersInformation_Flow(MOBUpdateTravelerInfoRequest request, Session session, Reservation reservation, ProfileResponse profileResponse, MOBCPProfileResponse mOBCPProfileResponse, MOBShoppingCart mOBShoppingCart, MOBUpdateTravelerInfoResponse mOBUpdateTravelerInfoResponse)
        {



            _memberProfileUtility.Setup(p => p.IsValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);



            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);



            _dpService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Sample Token");



            _shoppingUtility.Setup(p => p.EnableSpecialNeeds(It.IsAny<int>(), It.IsAny<string>())).Returns(true);



            _sessionHelperService.Setup(p => p.GetSession<Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);



            _sessionHelperService.Setup(p => p.GetSession<ProfileResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(profileResponse);



            _customerProfile.Setup(p => p.GetProfile(It.IsAny<MOBCPProfileRequest>())).ReturnsAsync(mOBCPProfileResponse.Profiles);



            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(mOBCPProfileResponse.Reservation);



            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);



            _mpTraveler.Setup(p => p.UpdateTravelerInfo(It.IsAny<MOBUpdateTravelerInfoRequest>(), It.IsAny<string>())).ReturnsAsync(mOBUpdateTravelerInfoResponse);





            var _memberProfileBusiness = new MemberProfileBusiness(_logger.Object, _configuration, _dynamoDBService.Object, _sessionHelperService.Object, _memberProfileUtility1,
                _customerProfile.Object, _profileXML.Object, _merchandizingServices.Object, _statusLiftBannerService.Object, _dpService.Object, _shoppingUtility.Object,
                _mileagePlus.Object, _shoppingSessionHelper.Object, _validateHashPinService.Object, _legalDocumentsForTitlesService.Object, _cslStatisticsService.Object,
                _traveler.Object, _travelerUtility.Object, _ffcShoppingcs.Object, _paymentService.Object, _empProfile.Object, _cachingService1, _mpTraveler.Object, _omniCart.Object,
                _referencedataService.Object, _pKDispenserService.Object, _formsOfPayment.Object, _headers.Object, _sSOTokenKeyService.Object,_featureSettings.Object, _shopBundleService.Object, _provisionService.Object, _featureToggles.Object, _corpProfile.Object);



            var result = _memberProfileBusiness.UpdateTravelersInformation(request);



            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
            // Assert.True(result.Result != null);
        }
    }
}
