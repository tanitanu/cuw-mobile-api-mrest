using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.PostBooking;
using United.Mobile.Model.Shopping.Pcu;
using United.Mobile.PostBooking.Domain;
using United.Utility.Helper;
using Xunit;
using Constants = United.Mobile.Model.Constants;

namespace United.Mobile.Test.PostBooking.Tests
{
    public class PostBookingBusinesstest
    {
        private readonly Mock<ICacheLog<PostBookingBusiness>> _logger;
        private readonly Mock<IHeaders> _headers;
        private readonly IConfiguration _configuration;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly PostBookingBusiness postBookingBusiness;
        public PostBookingBusinesstest()
        {
            _logger = new Mock<ICacheLog<PostBookingBusiness>>();
            _headers = new Mock<IHeaders>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _configuration = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appSettings.test.json", optional: false, reloadOnChange: true)
             .Build();
            postBookingBusiness = new PostBookingBusiness(_logger.Object, _configuration, _sessionHelperService.Object);
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
            fileName = string.Format("..\\..\\..\\TestData\\{0}", fileName);
            var path = Path.IsPathRooted(fileName) ? fileName
                                                : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetOffers), MemberType = typeof(TestDataGenerator))]
        public void GetOffers_Tests(MOBSHOPGetOffersRequest shopGetoffersRequest)
        {
            var session = GetFileContent("SessionData.json");
            var sessionData = JsonConvert.DeserializeObject<Session>(session);
            var pcu = JsonConvert.DeserializeObject<PcuState>(GetFileContent("PcuStateResponse.json"));
            var priorityBoarding = JsonConvert.DeserializeObject<PriorityBoardingFile>(GetFileContent("PriorityBoardingFile.json"));


            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(sessionData);
            _sessionHelperService.Setup(p => p.GetSession<PcuState>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(pcu);
            _sessionHelperService.Setup(p => p.GetSession<PriorityBoardingFile>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(priorityBoarding);

            var result = postBookingBusiness.GetOffers(shopGetoffersRequest);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.SessionId != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetOffers1), MemberType = typeof(TestDataGenerator))]
        public void GetOffers_Tests1(MOBSHOPGetOffersRequest shopGetoffersRequest)
        {
            var session = GetFileContent("SessionData.json");
            var sessionData = JsonConvert.DeserializeObject<Session>(session);
            var pcu = JsonConvert.DeserializeObject<PcuState>(GetFileContent("PcuStateResponse.json"));
            var priorityBoarding = JsonConvert.DeserializeObject<PriorityBoardingFile>(GetFileContent("PriorityBoardingFile.json"));


            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(sessionData);
            _sessionHelperService.Setup(p => p.GetSession<PcuState>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(pcu);
            _sessionHelperService.Setup(p => p.GetSession<PriorityBoardingFile>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(priorityBoarding);

            var result = postBookingBusiness.GetOffers(shopGetoffersRequest);
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
