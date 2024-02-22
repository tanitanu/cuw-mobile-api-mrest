using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Travelers;
using United.Mobile.Travelers.Domain;
using United.Utility.Helper;
using United.Utility.Http;
using Xunit;

namespace United.Mobile.Test.Travelers.Tests
{
     public class ValidateMPNameBusinessTest
    {
        private readonly Mock<ICacheLog<ValidateMPNameBusiness>> _logger;
        private readonly IConfiguration _configuration;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly Mock<IShoppingUtility> _shoppingUtility;
        private readonly Mock<ITravelerUtility> _travelerUtility;
        private readonly Mock<ITraveler> _traveler;
        private readonly Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly Mock<ICachingService> _cachingService;
        private readonly ICachingService _cachingService1;
        private readonly Mock<ICacheLog<CachingService>> _logger6;
        private readonly IResilientClient _resilientClient;
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy _policyWrap;
        private readonly string _baseUrl;
        private readonly ValidateMPNameBusiness _validateMPNameBusiness;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ICSLStatisticsService _cSLStatisticsService;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<IFeatureSettings> _featureSettings;

        public ValidateMPNameBusinessTest()
        {
            _logger = new Mock<ICacheLog<ValidateMPNameBusiness>>();
            _logger6 = new Mock<ICacheLog<CachingService>>();
            _cachingService = new Mock<ICachingService>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _shoppingUtility = new Mock<IShoppingUtility>();
            _travelerUtility = new Mock<ITravelerUtility>();
            _configuration = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appSettings.test.json", optional: false, reloadOnChange: true)
             .Build();
            _traveler = new Mock<ITraveler>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _headers = new Mock<IHeaders>();
            _resilientClient = new ResilientClient(_baseUrl);
            _cachingService1 = new CachingService(_resilientClient, _logger6.Object, _configuration);
            _featureSettings = new Mock<IFeatureSettings>();

            _configuration = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
           .Build();

            _validateMPNameBusiness = new ValidateMPNameBusiness(_logger.Object, _configuration, _sessionHelperService.Object, _shoppingUtility.Object, _travelerUtility.Object, _traveler.Object, _shoppingSessionHelper.Object, _cachingService1, _dynamoDBService, _cSLStatisticsService, _featureSettings.Object);

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
        [MemberData(nameof(Input.TravelerInput), MemberType = typeof(Input))]
        public void ValidateMPNameMisMatch_CFOP_Test(Session session, MOBMPNameMissMatchRequest request, MOBMPNameMissMatchResponse response, Model.Shopping.Reservation reservation, bool ok, MOBShoppingCart mOBShoppingCart)
        {

            _shoppingSessionHelper.Setup(p => p.GetBookingFlowSession(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(session);
            _shoppingUtility.Setup(p => p.EnableTravelerTypes(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(ok);
            _sessionHelperService.Setup(_ => _.GetSession<Model.Shopping.Reservation>(It.IsAny<string>()
                       , It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(reservation);
            _travelerUtility.Setup(p => p.GetTypeCodeByAge(It.IsAny<int>(), It.IsAny<bool>())).Returns(request.Traveler.TravelerTypeCode);
            //_shoppingUtility.Setup(p => p.GetAgeByDOB(It.IsAny<string>(), It.IsAny<string>())).Returns(19);
            _travelerUtility.Setup(p => p.GetPaxDescriptionByDOB(It.IsAny<string>(), It.IsAny<string>())).Returns(request.Traveler.TravelerTypeDescription);
            _traveler.Setup(p => p.RegisterNewTravelerValidateMPMisMatch(It.IsAny<MOBCPTraveler>(), It.IsAny<MOBRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(response.Traveler);
            _shoppingUtility.Setup(p => p.EnableSpecialNeeds(It.IsAny<int>(), It.IsAny<string>())).Returns(true);
            _travelerUtility.Setup(p => p.EnableSpecialNeeds(It.IsAny<int>(), It.IsAny<string>())).Returns(ok);
            _shoppingUtility.Setup(p => p.GetReservationFromPersist(It.IsAny<MOBSHOPReservation>(), It.IsAny<string>())).ReturnsAsync(response.Reservation);
            _shoppingUtility.Setup(p => p.EnableReshopCubaTravelReasonVersion(It.IsAny<int>(), It.IsAny<string>())).Returns(ok);
            _sessionHelperService.Setup(p => p.GetSession<MOBShoppingCart>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBShoppingCart);
            _travelerUtility.Setup(p => p.EnableTravelerTypes(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(true);
            _travelerUtility.Setup(p => p.EnableReshopCubaTravelReasonVersion(It.IsAny<int>(), It.IsAny<string>())).Returns(true);
            _shoppingUtility.Setup(p => p.ReservationToShoppingCart_DataMigration(It.IsAny<MOBSHOPReservation>(), It.IsAny<MOBShoppingCart>(), It.IsAny<MOBRequest>(), It.IsAny<Session>())).ReturnsAsync(mOBShoppingCart);

            var validateMPNameBusiness = new ValidateMPNameBusiness(_logger.Object, _configuration, _sessionHelperService.Object, _shoppingUtility.Object, _travelerUtility.Object, _traveler.Object, _shoppingSessionHelper.Object, _cachingService1,_dynamoDBService,_cSLStatisticsService, _featureSettings.Object);


            var result = _validateMPNameBusiness.ValidateMPNameMisMatch_CFOP(request);

            if (result.Exception == null)
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }
    }
}
