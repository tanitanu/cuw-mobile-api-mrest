using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.PostBooking;
using United.Mobile.PostBooking.Api.Controllers;
using United.Mobile.PostBooking.Domain;
using United.Utility.Helper;
using Xunit;
using Constants = United.Mobile.Model.Constants;

namespace United.Mobile.Test.PostBooking.Tests
{
    public class PostBookingControllerTests
    {
        private readonly Mock<ICacheLog<PostBookingController>> _logger;
        private readonly Mock<IHeaders> _headers;
        private readonly IConfiguration _configuration;
        private readonly Mock<IPostBookingBusiness> _postBookingBusiness;
        private readonly PostBookingController _postBookingController;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<IFeatureSettings> _featureSettings;
        public PostBookingControllerTests()
        {
            _logger = new Mock<ICacheLog<PostBookingController>>();
            _headers = new Mock<IHeaders>();
            _configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appSettings.test.json", optional: false, reloadOnChange: true)
               .Build();
            _postBookingBusiness = new Mock<IPostBookingBusiness>();
            SetHeaders();
            _postBookingController = new PostBookingController(_logger.Object, _headers.Object, _configuration, _postBookingBusiness.Object,_featureSettings.Object);
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            SetupHttpContextAccessor();
            
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
            _httpContextAccessor.Setup(_ => _.HttpContext).Returns(context);
        }

        private void SetHeaders(string deviceId = "589d7852-14e7-44a9-b23b-a6db36657579"
       , string applicationId = "2"
       , string appVersion = "4.1.29"
       , string transactionId = "589d7852-14e7-44a9-b23b-a6db36657579|8f46e040-a200-495c-83ca-4fca2d7175fb"
       , string languageCode = "en-US"
       , string sessionId = "17C979E184CC495EA083D45F4DD9D19D")
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

        [Fact]
        public void HealthCheck_Test()
        {
            string result = _postBookingController.HealthCheck();
            Assert.True(result == "Healthy");
        }

        [Fact]
        public void GetOffers_Test()
        {
            var response = new MOBSHOPGetOffersResponse();
            var request = new MOBSHOPGetOffersRequest()
            {
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.23"
                    }
                },
                AccessCode = "ACCESSCODE",
                TransactionId = "",
                LanguageCode = "en-US",
                DeviceId = "20317"
            };

            _postBookingBusiness.Setup(p => p.GetOffers(request)).Returns(Task.FromResult(response));
            var result = _postBookingController.GetOffers(request);
            Assert.True(result.Result != null);
        }

        [Fact]
        public void GetOffers_Test_Exception()
        {
            var response = new MOBSHOPGetOffersResponse();
            var request = new MOBSHOPGetOffersRequest()
            {
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.23"
                    }
                },
                AccessCode = "ACCESSCODE",
                TransactionId = "",
                LanguageCode = "en-US",
                DeviceId = "20317"
            };

            _postBookingBusiness.Setup(p => p.GetOffers(request)).ThrowsAsync(new MOBUnitedException("Error Message"));
            var result = _postBookingController.GetOffers(request);
            Assert.True(result.Result.Exception != null);
        }

        [Fact]
        public void GetOffers_Test_SystemException()
        {
            var response = new MOBSHOPGetOffersResponse();
            var request = new MOBSHOPGetOffersRequest()
            {
                Application = new MOBApplication()
                {
                    Id = 1,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.23"
                    }
                },
                AccessCode = "ACCESSCODE",
                TransactionId = "",
                LanguageCode = "en-US",
                DeviceId = "20317"
            };

            _postBookingBusiness.Setup(p => p.GetOffers(request)).ThrowsAsync(new Exception("Error Message"));
            var result = _postBookingController.GetOffers(request);
            Assert.True(result.Result.Exception != null);
        }
    }
}
