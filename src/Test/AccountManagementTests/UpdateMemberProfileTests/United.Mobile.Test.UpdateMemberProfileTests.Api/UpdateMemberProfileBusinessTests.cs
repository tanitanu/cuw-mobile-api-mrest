using Microsoft.AspNetCore.Http;
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
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.Shopping;
using United.Mobile.Model.Internal.AccountManagement;
using United.Mobile.Model.UpdateMemberProfile;
using United.Mobile.Test.UpdateMProfileTests.Api;
using United.Mobile.UpdateMemberProfile.Domain;
using United.Service.Presentation.SecurityResponseModel;
using United.Utility.Http;
using Xunit;
using Constants = United.Mobile.Model.Constants;
using ProfileFOPCreditCardResponse = United.Mobile.Model.Common.ProfileFOPCreditCardResponse;
using ProfileResponse = United.Mobile.Model.Common.ProfileResponse;
using United.Utility.Helper;

namespace United.Mobile.Test.UpdateMemberProfileTests.Api
{
    public class UpdateMemberProfileBusinessTests
    {
        private readonly Mock<ICacheLog<UpdateMemberProfileBusiness>> _logger;
        private readonly IConfiguration _configuration;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly Mock<ICustomerProfile> _customerProfile;
        private readonly UpdateMemberProfileBusiness _updateMemberProfileBusiness;
        private readonly Mock<IPersistToken> _persistToken;
        private readonly Mock<IDataVaultService> _dataVaultService;
        private readonly Mock<IShoppingUtility> _shoppingUtility;
        private readonly Mock<IDynamoDBService> _dynamoDBService;
        private readonly IDynamoDBService _dynamoDBService1;
        private readonly Mock<ICacheLog<DynamoDBService>> _logger1;
        private readonly Mock<IFFCShoppingcs> _fFCShoppingcs;
        private readonly Mock<IOmniCart> _omniCart;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<IUpdateMemberProfileUtility> _utility;
        private readonly Mock<IShoppingSessionHelper> _ShoppingSessionHelper;
        private readonly Mock<IProfileCreditCard> _profileCreditCard;
        private readonly Mock<IEmpProfile> _empProfile;
        private readonly Mock<IFormsOfPayment> _formsOfPayment;
        private readonly Mock<IValidateHashPinService> _validateHashPinService;
        private readonly IResilientClient _resilientClient;
        private readonly Mock<ICachingService> _cachingService;
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy _policyWrap;
        private readonly string _baseUrl;
        private readonly Mock<IPKDispenserService> _pKDispenserService;
        private readonly Mock<IDPService> _dPService;
        private readonly Mock<IShoppingCartService> _shoppingCartService;
        private readonly Mock<IPaymentService> _paymentService;
        private readonly Mock<IPaymentUtility> _paymentUtility;
        private readonly Mock<IProductOffers> _shopping;
        private readonly Mock<IRegisterCFOP> _registerCFOP;
        private readonly Mock<IReferencedataService> _referencedataService;
        private readonly Mock<IProductInfoHelper> _productInfoHelper;
        private readonly Mock<ILegalDocumentsForTitlesService> _legalDocumentsForTitlesService;
        private readonly PKDispenserPublicKey _pKDispenserPublicKey;
        private readonly Mock<IMPTraveler> _mpTraveler;
        private readonly Mock<IProvisionService> _provisionService;
        private readonly Mock<IFeatureSettings> _featureSetting;

        public UpdateMemberProfileBusinessTests()
        {
            _logger = new Mock<ICacheLog<UpdateMemberProfileBusiness>>();
            _logger1 = new Mock<ICacheLog<DynamoDBService>>();
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.test.json", optional: false, reloadOnChange: true)
                .Build();
            _headers = new Mock<IHeaders>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _customerProfile = new Mock<ICustomerProfile>();
            _persistToken = new Mock<IPersistToken>();
            _dataVaultService = new Mock<IDataVaultService>();
            _dynamoDBService = new Mock<IDynamoDBService>();
            _fFCShoppingcs = new Mock<IFFCShoppingcs>();
            _omniCart = new Mock<IOmniCart>();
            _shoppingUtility = new Mock<IShoppingUtility>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _utility = new Mock<IUpdateMemberProfileUtility>();
            _validateHashPinService = new Mock<IValidateHashPinService>();
            _ShoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _profileCreditCard = new Mock<IProfileCreditCard>();
            _empProfile = new Mock<IEmpProfile>();
            _formsOfPayment = new Mock<IFormsOfPayment>();
            _cachingService = new Mock<ICachingService>();
            _dPService = new Mock<IDPService>();
            _pKDispenserService = new Mock<IPKDispenserService>();
            new ConfigUtility(_configuration);
            _shoppingCartService = new Mock<IShoppingCartService>();
            _paymentService = new Mock<IPaymentService>();
            _paymentUtility = new Mock<IPaymentUtility>();
            _shopping = new Mock<IProductOffers>();
            _registerCFOP = new Mock<IRegisterCFOP>();
            _referencedataService = new Mock<IReferencedataService>();
            _productInfoHelper = new Mock<IProductInfoHelper>();
            _legalDocumentsForTitlesService = new Mock<ILegalDocumentsForTitlesService>();
            _pKDispenserPublicKey = new PKDispenserPublicKey(_configuration, _cachingService.Object, _dPService.Object, _pKDispenserService.Object,_headers.Object);
            _mpTraveler = new Mock<IMPTraveler>();
            _provisionService = new Mock<IProvisionService>();
            _featureSetting = new Mock<IFeatureSettings>();

            _updateMemberProfileBusiness = new UpdateMemberProfileBusiness(_logger.Object , _configuration , _sessionHelperService.Object, _customerProfile.Object , _persistToken.Object, _dataVaultService.Object, _utility.Object, _ShoppingSessionHelper.Object, _mpTraveler.Object, _profileCreditCard.Object, _empProfile.Object, _formsOfPayment.Object , _validateHashPinService.Object, _dynamoDBService.Object, _shoppingUtility.Object , _dPService.Object, _pKDispenserService.Object, _cachingService.Object, _shoppingCartService.Object, _paymentService.Object, _paymentUtility.Object, _headers.Object, _provisionService.Object, _featureSetting.Object);

            _resilientClient = new ResilientClient(_baseUrl);
            _dynamoDBService1 = new DynamoDBService(_resilientClient, _logger1.Object);

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
            context.Request.Headers[Constants.SessionId] = "BEE45169E812468A9BCC79C75BBAB8B9";
            _httpContextAccessor.Setup(_ => _.HttpContext).Returns(context);
        }
        private void SetHeaders(string deviceId = "589d7852-14e7-44a9-b23b-a6db36657579"
       , string applicationId = "2"
       , string appVersion = "4.1.29"
       , string transactionId = "589d7852-14e7-44a9-b23b-a6db36657579|8f46e040-a200-495c-83ca-4fca2d7175fb"
       , string languageCode = "en-US"
       , string sessionId = "17C979E184CC495EA083D45F4DD9D19D")
        {
            _headers.Setup(_=> _.ContextValues).Returns(
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
        private string GetFileContent(string fileName)
        {
            var path = Path.IsPathRooted(fileName) ? fileName
                                                : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }

        [Theory]
        [MemberData(nameof(UpdateMemberProfileInput.InputUpdateProfileOwnerCardInfo), MemberType = typeof(UpdateMemberProfileInput))]
        public void UpdateProfileOwnerCardInfo_Test(string input, string session)
        {
            var requestPayload = JsonConvert.DeserializeObject<MOBUpdateProfileOwnerFOPRequest>(input);
            var request1 = JsonConvert.DeserializeObject<MOBUpdateTravelerRequest>(input);
            //var session = GetFileContent("SessionData.json");
            var creditCard = GetFileContent("UpdateProfileOwnerCardInfoResponse.json");
            var shopres = GetFileContent("MOBShopReservation.json");
            var reservation = GetFileContent("Reservation.json");
            var items = GetFileContent("MOBItem.json");
            var hashPin = GetFileContent("HashPinResponse.json");

            var sessionData = JsonConvert.DeserializeObject<Session>(session);
            var creditCardData = JsonConvert.DeserializeObject<ProfileFOPCreditCardResponse>(creditCard);
            var profileData = JsonConvert.DeserializeObject<MOBUpdateProfileOwnerFOPResponse>(creditCard);
            var customerData = JsonConvert.DeserializeObject<MOBCustomerProfileRequest>(creditCard);
            var profileData1 = JsonConvert.DeserializeObject<ProfileResponse>(creditCard);
            var profileData2 = JsonConvert.DeserializeObject<Model.Shopping.MOBShoppingCart>(creditCard);
            var shopresData = JsonConvert.DeserializeObject<Model.Shopping.MOBSHOPReservation>(shopres);
            var reservationData = JsonConvert.DeserializeObject<Model.Shopping.Reservation>(shopres);
            var itemsData = JsonConvert.DeserializeObject<List<MOBItem>>(items);
            var hashPinData = JsonConvert.DeserializeObject<HashPinValidate>(hashPin);

            _ShoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(sessionData);

            _utility.Setup(p => p.isValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _utility.Setup(p => p.ValidateAccountFromCache(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            // _sessionHelperService.Setup(p => p.GetSession<ProfileFOPCreditCardResponse>(It.IsAny<HttpContextValues>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<string>())).ReturnsAsync(creditCardData);
            _sessionHelperService.Setup(p => p.GetSession<ProfileFOPCreditCardResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(creditCardData);
            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(profileData2);
            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationData);

            _utility.Setup(p => p.ObjectToObjectCasting<ProfileFOPCreditCardResponse, ProfileResponse>(It.IsAny<ProfileResponse>())).Returns(creditCardData);
            _utility.Setup(p => p.ObjectToObjectCasting<MOBUpdateTravelerRequest, MOBUpdateProfileOwnerFOPRequest>(It.IsAny<MOBUpdateProfileOwnerFOPRequest>())).Returns(request1);
            _utility.Setup(p => p.ObjectToObjectCasting<MOBCustomerProfileRequest, MOBCPProfileRequest>(It.IsAny<MOBCPProfileRequest>())).Returns(customerData);
            _utility.Setup(p => p.ObjectToObjectCasting<ProfileResponse, ProfileFOPCreditCardResponse>(It.IsAny<ProfileFOPCreditCardResponse>())).Returns(profileData1);
            _utility.Setup(p => p.ObjectToObjectCasting<MOBUpdateProfileOwnerFOPResponse, MOBCustomerProfileResponse>(It.IsAny<MOBCustomerProfileResponse>())).Returns(profileData);
            _utility.Setup(p => p.MakeReservationFromPersistReservation(It.IsAny<Model.Shopping.MOBSHOPReservation>(), It.IsAny<Model.Shopping.Reservation>(), It.IsAny<Session>())).Returns(shopresData);

            _customerProfile.Setup(p => p.GetProfile(It.IsAny<MOBCPProfileRequest>())).ReturnsAsync(profileData.Profiles);
            _empProfile.Setup(p => p.GetEmpProfile(It.IsAny<MOBCPProfileRequest>(), It.IsAny<bool>())).ReturnsAsync(profileData.Profiles);
            // _mPTraveler.Setup(p => p.UpdateTraveler(It.IsAny<MOBUpdateTravelerRequest>(), It.IsAny <List< MOBItem >>())).ReturnsAsync(true);
            _profileCreditCard.Setup(p => p.GenerateCCTokenWithDataVault(It.IsAny<MOBCreditCard>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MOBApplication>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((true, "testing")));
            _dynamoDBService.Setup(p => p.GetRecords<int>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(1);
            _shoppingUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);
            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinData);




            var _updateMemberProfileBusiness = new UpdateMemberProfileBusiness(_logger.Object
            , _configuration
            , _sessionHelperService.Object
            , _customerProfile.Object
            , _persistToken.Object
            , _dataVaultService.Object
            , _utility.Object
            , _ShoppingSessionHelper.Object
            , _mpTraveler.Object
            , _profileCreditCard.Object
            , _empProfile.Object
            , _formsOfPayment.Object
            , _validateHashPinService.Object
            , _dynamoDBService.Object
            , _shoppingUtility.Object
            , _dPService.Object
            , _pKDispenserService.Object
            , _cachingService.Object
            , _shoppingCartService.Object
            , _paymentService.Object
            , _paymentUtility.Object
            , _headers.Object
            , _provisionService.Object
            , _featureSetting.Object
                );

            var result = _updateMemberProfileBusiness.UpdateProfileOwnerCardInfo(requestPayload);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.SessionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.UpdateProfileOwnerCardInfo_flow1), MemberType = typeof(TestDataGenerator))]
        public void UpdateProfileOwnerCardInfo_flow1(MOBUpdateProfileOwnerFOPRequest mOBUpdateProfileOwnerFOPRequest, MOBUpdateTravelerRequest mOBUpdateTravelerRequest, Session session, List<MOBItem> mOBItems, PKDispenserResponse pKDispenserResponse, List<FormofPaymentOption> formofPaymentOptions)
        {

            var creditCard = GetFileContent(@"ManageResFlow\UpdateProfileOwnerCardInfoResponse.json");
            var shopres = GetFileContent("MOBShopReservation.json");
            var reservation = GetFileContent("Reservation.json");
           // var items = GetFileContent("MOBItem.json");
            var hashPin = GetFileContent("HashPinResponse.json");

            var creditCardData = JsonConvert.DeserializeObject<ProfileFOPCreditCardResponse>(creditCard);
            var profileData = JsonConvert.DeserializeObject<MOBUpdateProfileOwnerFOPResponse>(creditCard);
            var customerData = JsonConvert.DeserializeObject<MOBCustomerProfileRequest>(creditCard);
            var profileData1 = JsonConvert.DeserializeObject<ProfileResponse>(creditCard);
            var profileData2 = JsonConvert.DeserializeObject<Model.Shopping.MOBShoppingCart>(creditCard);
            var shopresData = JsonConvert.DeserializeObject<Model.Shopping.MOBSHOPReservation>(shopres);
            var reservationData = JsonConvert.DeserializeObject<Model.Shopping.Reservation>(shopres);
            //var itemsData = JsonConvert.DeserializeObject<List<MOBItem>>(items);
            var hashPinData = JsonConvert.DeserializeObject<HashPinValidate>(hashPin);

            _ShoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);

            _utility.Setup(p => p.isValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _utility.Setup(p => p.ValidateAccountFromCache(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            _sessionHelperService.Setup(p => p.GetSession<ProfileFOPCreditCardResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(creditCardData);

            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(profileData2);

            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationData);

            _utility.Setup(p => p.ObjectToObjectCasting<ProfileFOPCreditCardResponse, ProfileResponse>(It.IsAny<ProfileResponse>())).Returns(creditCardData);

            _utility.Setup(p => p.ObjectToObjectCasting<MOBUpdateTravelerRequest, MOBUpdateProfileOwnerFOPRequest>(It.IsAny<MOBUpdateProfileOwnerFOPRequest>())).Returns(mOBUpdateTravelerRequest);

            _utility.Setup(p => p.ObjectToObjectCasting<MOBCustomerProfileRequest, MOBCPProfileRequest>(It.IsAny<MOBCPProfileRequest>())).Returns(customerData);

            _utility.Setup(p => p.ObjectToObjectCasting<ProfileResponse, ProfileFOPCreditCardResponse>(It.IsAny<ProfileFOPCreditCardResponse>())).Returns(profileData1);

            _utility.Setup(p => p.ObjectToObjectCasting<MOBUpdateProfileOwnerFOPResponse, MOBCustomerProfileResponse>(It.IsAny<MOBCustomerProfileResponse>())).Returns(profileData);

            _utility.Setup(p => p.MakeReservationFromPersistReservation(It.IsAny<Model.Shopping.MOBSHOPReservation>(), It.IsAny<Model.Shopping.Reservation>(), It.IsAny<Session>())).Returns(shopresData);

            _customerProfile.Setup(p => p.GetProfile(It.IsAny<MOBCPProfileRequest>())).ReturnsAsync(profileData.Profiles);

            _empProfile.Setup(p => p.GetEmpProfile(It.IsAny<MOBCPProfileRequest>(), It.IsAny<bool>())).ReturnsAsync(profileData.Profiles);

            _profileCreditCard.Setup(p => p.GenerateCCTokenWithDataVault(It.IsAny<MOBCreditCard>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MOBApplication>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((true, "testing")));

            _dynamoDBService.Setup(p => p.GetRecords<int>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(1);

            _shoppingUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinData);

            _mpTraveler.Setup(p => p.UpdateTraveler(It.IsAny<MOBUpdateTravelerRequest>(), It.IsAny<List<MOBItem>>())).Returns(Task.FromResult((true, mOBItems)));

            _pKDispenserService.Setup(p => p.GetPkDispenserPublicKey<PKDispenserResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(pKDispenserResponse);

            _formsOfPayment.Setup(p => p.GetEligibleFormofPayments(It.IsAny<MOBRequest>(), It.IsAny<Session>(), It.IsAny<Model.Shopping.MOBShoppingCart>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Model.Shopping.MOBSHOPReservation>(), It.IsAny<bool>(), It.IsAny<Model.Shopping.SeatChangeState>())).Returns(Task.FromResult((formofPaymentOptions, true)));

            var _updateMemberProfileBusiness = new UpdateMemberProfileBusiness(_logger.Object, _configuration , _sessionHelperService.Object, _customerProfile.Object, _persistToken.Object, _dataVaultService.Object, _utility.Object, _ShoppingSessionHelper.Object, _mpTraveler.Object, _profileCreditCard.Object, _empProfile.Object, _formsOfPayment.Object, _validateHashPinService.Object, _dynamoDBService.Object, _shoppingUtility.Object, _dPService.Object, _pKDispenserService.Object, _cachingService.Object, _shoppingCartService.Object, _paymentService.Object, _paymentUtility.Object, _headers.Object, _provisionService.Object, _featureSetting.Object);

            var result = _updateMemberProfileBusiness.UpdateProfileOwnerCardInfo(mOBUpdateProfileOwnerFOPRequest);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.SessionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.UpdateProfileOwnerCardInfo_flow2), MemberType = typeof(TestDataGenerator))]
        public void UpdateProfileOwnerCardInfo_flow2(MOBUpdateProfileOwnerFOPRequest mOBUpdateProfileOwnerFOPRequest, MOBUpdateTravelerRequest mOBUpdateTravelerRequest, Session session, List<MOBItem> mOBItems, PKDispenserResponse pKDispenserResponse, List<FormofPaymentOption> formofPaymentOptions)
        {

            var creditCard = GetFileContent(@"ManageResFlow\UpdateProfileOwnerCardInfoResponse.json");
            var shopres = GetFileContent("MOBShopReservation.json");
            var reservation = GetFileContent("Reservation.json");
            var hashPin = GetFileContent("HashPinResponse.json");
            var cart = GetFileContent("MOBShoppingCart.json");

            var creditCardData = JsonConvert.DeserializeObject<List<ProfileFOPCreditCardResponse>>(creditCard);
            var profileData = JsonConvert.DeserializeObject<List<MOBUpdateProfileOwnerFOPResponse>>(creditCard);
            var customerData = JsonConvert.DeserializeObject<List<MOBCustomerProfileRequest>>(creditCard);
            var profileData1 = JsonConvert.DeserializeObject<List<ProfileResponse>>(creditCard);
            var profileData2 = JsonConvert.DeserializeObject<List<Model.Shopping.MOBShoppingCart>>(creditCard);
            var shopresData = JsonConvert.DeserializeObject<Model.Shopping.MOBSHOPReservation>(shopres);
            var reservationData = JsonConvert.DeserializeObject<Model.Shopping.Reservation>(shopres);
            var hashPinData = JsonConvert.DeserializeObject<HashPinValidate>(hashPin);
            var shopppingcart = JsonConvert.DeserializeObject<Model.Shopping.MOBShoppingCart>(cart);

            _ShoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);

            _utility.Setup(p => p.isValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _utility.Setup(p => p.ValidateAccountFromCache(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            _sessionHelperService.Setup(p => p.GetSession<ProfileFOPCreditCardResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(creditCardData[1]);

            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(profileData2[1]);

            _sessionHelperService.Setup(p => p.GetSession<Model.Shopping.Reservation>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservationData);

            _utility.Setup(p => p.ObjectToObjectCasting<ProfileFOPCreditCardResponse, ProfileResponse>(It.IsAny<ProfileResponse>())).Returns(creditCardData[1]);

            _utility.Setup(p => p.ObjectToObjectCasting<MOBUpdateTravelerRequest, MOBUpdateProfileOwnerFOPRequest>(It.IsAny<MOBUpdateProfileOwnerFOPRequest>())).Returns(mOBUpdateTravelerRequest);

            _utility.Setup(p => p.ObjectToObjectCasting<MOBCustomerProfileRequest, MOBCPProfileRequest>(It.IsAny<MOBCPProfileRequest>())).Returns(customerData[1]);

            _utility.Setup(p => p.ObjectToObjectCasting<ProfileResponse, ProfileFOPCreditCardResponse>(It.IsAny<ProfileFOPCreditCardResponse>())).Returns(profileData1[1]);

            _utility.Setup(p => p.ObjectToObjectCasting<MOBUpdateProfileOwnerFOPResponse, MOBCustomerProfileResponse>(It.IsAny<MOBCustomerProfileResponse>())).Returns(profileData[1]);

            _utility.Setup(p => p.MakeReservationFromPersistReservation(It.IsAny<Model.Shopping.MOBSHOPReservation>(), It.IsAny<Model.Shopping.Reservation>(), It.IsAny<Session>())).Returns(shopresData);

            _customerProfile.Setup(p => p.GetProfile(It.IsAny<MOBCPProfileRequest>())).ReturnsAsync(profileData[1].Profiles);

            _empProfile.Setup(p => p.GetEmpProfile(It.IsAny<MOBCPProfileRequest>(), It.IsAny<bool>())).ReturnsAsync(profileData[1].Profiles);

            _profileCreditCard.Setup(p => p.GenerateCCTokenWithDataVault(It.IsAny<MOBCreditCard>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MOBApplication>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((true, "testing")));

            _dynamoDBService.Setup(p => p.GetRecords<int>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(1);

            _shoppingUtility.Setup(p => p.ValidateHashPinAndGetAuthToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinData);

            _mpTraveler.Setup(p => p.UpdateTraveler(It.IsAny<MOBUpdateTravelerRequest>(), It.IsAny<List<MOBItem>>())).Returns(Task.FromResult((true, mOBItems)));

            _pKDispenserService.Setup(p => p.GetPkDispenserPublicKey<PKDispenserResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(pKDispenserResponse);

            _formsOfPayment.Setup(p => p.GetEligibleFormofPayments(It.IsAny<MOBRequest>(), It.IsAny<Session>(), It.IsAny<Model.Shopping.MOBShoppingCart>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Model.Shopping.MOBSHOPReservation>(), It.IsAny<bool>(), It.IsAny<Model.Shopping.SeatChangeState>())).Returns(Task.FromResult((formofPaymentOptions, true)));

            _shoppingUtility.Setup(p => p.ReservationToShoppingCart_DataMigration(It.IsAny<Model.Shopping.MOBSHOPReservation>(), It.IsAny< Model.Shopping.MOBShoppingCart>(), It.IsAny<MOBRequest>(), It.IsAny<Session>())).ReturnsAsync(shopppingcart);

            var _updateMemberProfileBusiness = new UpdateMemberProfileBusiness(_logger.Object, _configuration, _sessionHelperService.Object, _customerProfile.Object, _persistToken.Object, _dataVaultService.Object, _utility.Object, _ShoppingSessionHelper.Object, _mpTraveler.Object, _profileCreditCard.Object, _empProfile.Object, _formsOfPayment.Object, _validateHashPinService.Object, _dynamoDBService.Object, _shoppingUtility.Object, _dPService.Object, _pKDispenserService.Object, _cachingService.Object, _shoppingCartService.Object, _paymentService.Object, _paymentUtility.Object, _headers.Object, _provisionService.Object, _featureSetting.Object);

            var result = _updateMemberProfileBusiness.UpdateProfileOwnerCardInfo(mOBUpdateProfileOwnerFOPRequest);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.SessionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(UpdateMemberProfileInput.InputUpdateProfileOwnerCardInfo), MemberType = typeof(UpdateMemberProfileInput))]
        public void UpdateProfileOwner_Test(string input, string session)
        {
            var requestPayload = JsonConvert.DeserializeObject<MOBUpdateCustomerFOPRequest>(input);
            var creditCard = GetFileContent("UpdateProfileOwnerCardInfoResponse.json");

            var sessionData = JsonConvert.DeserializeObject<Session>(session);
            var creditCardData = JsonConvert.DeserializeObject<ProfileFOPCreditCardResponse>(creditCard);

            _ShoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(sessionData);

            _utility.Setup(p => p.isValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _utility.Setup(p => p.ValidateAccountFromCache(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            _utility.Setup(p => p.GetCSLProfileFOPCreditCardResponseInSession(It.IsAny<string>())).ReturnsAsync(creditCardData);

            _profileCreditCard.Setup(p => p.GenerateCCTokenWithDataVault(It.IsAny<MOBCreditCard>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MOBApplication>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((true, "testing")));

            var result = _updateMemberProfileBusiness.UpdateProfileOwner(requestPayload);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.SessionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.UpdateProfileOwner_flow), MemberType = typeof(TestDataGenerator))]
        public void UpdateProfileOwner_flow(MOBUpdateCustomerFOPRequest mOBUpdateCustomerFOPRequest, Session session, ProfileFOPCreditCardResponse profileFOPCreditCardResponse, HashPinValidate hashPinValidate, List<MOBItem> mOBItems)
        {

            //var requestPayload = JsonConvert.DeserializeObject<MOBUpdateCustomerFOPRequest>(input);
            //var creditCard = GetFileContent("UpdateProfileOwnerCardInfoResponse.json");

            //var sessionData = JsonConvert.DeserializeObject<Session>(session);
            //var creditCardData = JsonConvert.DeserializeObject<ProfileFOPCreditCardResponse>(creditCard);

            _ShoppingSessionHelper.Setup(p => p.GetShoppingSession(It.IsAny<string>())).ReturnsAsync(session);

            _utility.Setup(p => p.isValidDeviceRequest(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            _utility.Setup(p => p.ValidateAccountFromCache(It.IsAny<string>(), It.IsAny<string>())).Returns(true);

            _utility.Setup(p => p.GetCSLProfileFOPCreditCardResponseInSession(It.IsAny<string>())).ReturnsAsync(profileFOPCreditCardResponse);

            _profileCreditCard.Setup(p => p.GenerateCCTokenWithDataVault(It.IsAny<MOBCreditCard>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MOBApplication>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.FromResult((true, "testing")));

            _dynamoDBService.Setup(p => p.GetRecords<string>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync("{\"AppVersion\":\"4.1.48\",\"ApplicationID\":\"2\",\"AuthenticatedToken\":null,\"CallDuration\":0,\"CustomerID\":\"123901727\",\"DataPowerAccessToken\":\"BearerrlWuoTpvBvWFHmV1AvVfVzgcMPV6VzRlAzRjLmVkYGIwMwDgAQZlZl04LJDkYGxjZwAuAzMwLwL3MFW9.NNV8GJ9vnJkyYHShMUWinJEDnT9hMI9IDHksAwDmEGSSAQpgZGV0Zv00DwMQYHSPA0HgAwDjZwESARWQBQEQm_UZfeWVu4hFCrLSGjfTs8WRM4GadvbiNAMYdbxZoEh69D-IXfiLKeTCPU-4GE5RKBhYbkMOv0TrQzcMMhRx3TZVsqJDUMphaQTSKpAyFJUYriwVknmMvLXUrcYmDtLkXAuiEOgfNQWCUqqaUcY9HfqFtTcrIY03SjwHH296Ptu8FJ9OdNtnpEehMuNPLpYz.jwC0naNWpnrKScvZMHhSY2zEFTasGTG3JfCP1jhPoKxBeeuG_YkZkq15WhOLdA-erMVuY0e8MSqHEkQ3pNepiRHXo09f_Ht0f9PJfciIUOjA_haRN9x1WYfsd57mPCMZCLOrTI4tPDLbrFoyGFkElHLpmX1fly3mP_gR7ITMpM-s8Ynjr1XVxtZQ072wUOqfllxg8Dp17MPMdRD9VOpNMj-nXDAi0-9_vKE5d0Lm1xmDSh3R00DqkM0VQb2ScHfG5XChkjhux6vFm6Y8lgcgrfO6t5r-gM3Jq7DU6ZbT6Gk30d14PwAfm-35s5N5Bt39zDlBZ3wOcPjgZtnvGEk5Kt.rlW2MKVvBvVkYwNvYPWdqTxvBvWwAmLjAGOvBP1yLwIuYGEyLwRgLJL5Zv1xZzR1MGMuZwD0BJHvYPWmqJVvBvWwAmLjAGOvBP1yLwIuYGEyLwRgLJL5Zv1xZzR1MGMuZwD0BJHvYPWcp3ZvBvWbqUEjpmbiY2AmoJZhpJRhLKI0nP51ozy0MJDhL29gY29uqKEbZv92ZFVfVzS1MPV6Vx1iLzyfMF1OozElo2yxHTuiozIsIHSZKmL0Z0HkEGD3YGRlAQVgARV2Dl1ODwqSYGL0ZQV0EGEPDmt0DlVfVzyuqPV6ZGL1ZQHlBQR3ZljvMKujVwbkAwHjAGZ1ZmpmYPWuoKVvByfvpUqxVy0fVzS1qTusqTygMFV6ZGL1ZQHlBQR3Zljvoz9hL2HvBvVmnUpjMaN5pIuzqaRvYPWuqS9bLKAbVwbvHHqOIaykDHcUAwAmFSIDFUMZF1ScHG09VvjvqKOxLKEyMS9uqPV6ZGL1ZQHlBQR3ZljvL2kcMJ50K2SjpTkcL2S0nJ9hK2yxVwbvGJ9vnJkyYHShMUWinJEDnT9hMI9IDHksAwDmEGSSAQpgZGV0Zv00DwMQYHSPA0HgAwDjZwESARWQBQEQVvjvqKAypyE5pTHvBvWaqJImqPVfVzAfnJIhqS9cMPV6Vx1iLzyfMF1OozElo2yxHTuiozIsIHSZKmL0Z0HkEGD3YGRlAQVgARV2Dl1ODwqSYGL0ZQV0EGEPDmt0DlVfVzIhMSImMKWOM2IhqRyRVwbvAQD2MGx0Z2DgMQp0BF00MwWxYGuvMwVgMGOvLGIxAzEuAwt1VvjvMJ5xIKAypxSaMJ50FINvBvVkZwphZP4jYwRvsD\",\"DeviceID\":\"446e943d-d749-4f2d-8bf2-e0ba5d6da685\",\"HashPincode\":\"A5D3AFDAE0BF0E6543650D7B928EB77C94A4AD56\",\"IsTokenAnonymous\":\"False\",\"IsTokenValid\":\"True\",\"IsTouchIDSignIn\":\"False\",\"MPUserName\":\"AW719636\",\"MileagePlusNumber\":\"AW719636\",\"PinCode\":\"\",\"TokenExpireDateTime\":\"2022-04-2105:02:53.725\",\"TokenExpiryInSeconds\":\"7200\",\"UpdateDateTime\":\"2022-04-2103:02:54.565\"}");

            _validateHashPinService.Setup(p => p.ValidateHashPin<HashPinValidate>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(hashPinValidate);

            _mpTraveler.Setup(p => p.UpdateTraveler(It.IsAny<MOBUpdateTravelerRequest>(), It.IsAny<List<MOBItem>>())).Returns(Task.FromResult((true, mOBItems)));

            var result = _updateMemberProfileBusiness.UpdateProfileOwner(mOBUpdateCustomerFOPRequest);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.SessionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

    }
}
