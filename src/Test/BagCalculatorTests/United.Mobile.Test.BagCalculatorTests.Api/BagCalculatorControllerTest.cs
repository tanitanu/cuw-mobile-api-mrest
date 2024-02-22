using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.BagCalculator.Api.Controllers;
using United.Mobile.BagCalculator.Domain;
using United.Mobile.Model;
using United.Mobile.Model.BagCalculator;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Utility.Helper;
using Xunit;
using Constants = United.Mobile.Model.Constants;

namespace United.Mobile.Test.BagCalculatorTest.Api
{
    public class BagCalculatorControllerTest
    {
        private IConfiguration _config;
        private readonly Mock<ICacheLog<BagCalculatorController>> _logger;
        //private readonly IConfiguration _configuration;
        private readonly Mock<IBagCalculatorBusiness> _bagCalculatorBusiness;
        private readonly BagCalculatorController _bagCalculatorController;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly IConfiguration _configuration;
        //private Mock<BagCalculatorBusiness> _moqbagCalculatorBusiness;
        private readonly Mock<ICacheLog<BagCalculatorBusiness>> _moqlogger;
        private Mock<HttpContextAccessor> _moqhttpContextAccessor;
        private HttpContextAccessor _httpContextAccessor1;
        private readonly Mock<IHeaders> _moqHeader;
        private readonly Mock<IFeatureSettings> _featureSetting;
        public BagCalculatorControllerTest()
        {
            _moqHeader = new Mock<IHeaders>();
            _logger = new Mock<ICacheLog<BagCalculatorController>>();
            _bagCalculatorBusiness = new Mock<IBagCalculatorBusiness>();
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(Configuration);
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _bagCalculatorController = new BagCalculatorController(_logger.Object, _config, _bagCalculatorBusiness.Object, _moqHeader.Object,_featureSetting.Object);
            _config = new ConfigurationBuilder().AddJsonFile("appSettings.Development.json").Build();
            _logger = new Mock<ICacheLog<BagCalculatorController>>();
            var mockBagCalculatorBusiness = new Mock<IConfiguration>();
            _bagCalculatorBusiness = new Mock<IBagCalculatorBusiness>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _moqlogger = new Mock<ICacheLog<BagCalculatorBusiness>>();
            _bagCalculatorBusiness = new Mock<IBagCalculatorBusiness>();
            _httpContextAccessor1 = new HttpContextAccessor();
            _moqhttpContextAccessor = new Mock<HttpContextAccessor>();
            _bagCalculatorController = new BagCalculatorController(_logger.Object, _config, _bagCalculatorBusiness.Object, _moqHeader.Object,_featureSetting.Object);
            services.AddSingleton<IConfiguration>(Configuration);
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
        public IConfiguration Configuration
        {
            get
            {
                if (_config == null)
                {
                    var builder = new ConfigurationBuilder().AddJsonFile($"appsettings.test.json", optional: false);
                    _config = builder.Build();
                }
                return _config;
            }
        }

        
        //GetBaggageCalculatorSearchValues

        [Theory]
        [InlineData("ACCESSCODE", "d007548c-addf-43fb-8f46-6870aec49647%7Cd5454749-3290-44a7-9782-65f5c30538e8", "en-US", "4.1.31", 2)]
        public void GetBaggageCalculatorSearchValues(string accessCode, string transactionId, string languageCode, string appVersion, int applicationId)
        {
            var returns = new BaggageCalculatorSearchResponse()
            {
                TransactionId = "eb49a82f-59f4-449c-91a3-9c41d2a7a636%7C8810cdc8-6adb-427b-ba6d-da9dcbcdda87"
            };

            _bagCalculatorController.ControllerContext = new ControllerContext();
            _bagCalculatorController.ControllerContext.HttpContext = new DefaultHttpContext();
            _bagCalculatorController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _bagCalculatorBusiness.Setup(p => p.GetBaggageCalculatorSearch(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()
                , It.IsAny<string>(), It.IsAny<int>())).Returns(Task.FromResult(returns));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();

            var result = _bagCalculatorController.GetBaggageCalculatorSearchValues(accessCode, transactionId, languageCode, appVersion, applicationId);

            Assert.True(result != null);
            Assert.True(result.Result.CallDuration != null);
        }

        [Fact]
        public void GetBaggageCalculatorSearchValues_Exception1_UnitedExp()
        {
            var returns = new DOTBaggageCalculatorResponse()
            {
                IsAnyFlightSearch = true,
                TransactionId = "eb49a82f-59f4-449c-91a3-9c41d2a7a636%7C8810cdc8-6adb-427b-ba6d-da9dcbcdda87"
            };

            _bagCalculatorController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            _bagCalculatorController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";
            _bagCalculatorBusiness.Setup(p => p.GetBaggageCalculatorSearch(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).ThrowsAsync(new MOBUnitedException("Error Message"));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();

            var result = _bagCalculatorController.GetBaggageCalculatorSearchValues("ACCESSCODE", "d007548c-addf-43fb-8f46-6870aec49647%7Cd5454749-3290-44a7-9782-65f5c30538e8", "en-US", "4.1.31", 2);

            Assert.True(result != null && result.Exception == null);
            Assert.True(result.Result.CallDuration != null);

            Assert.True(result.Result.Exception.Message == "Error Message");
        }

        [Fact]
        public void GetBaggageCalculatorSearchValues_Exception2_NormalExp()
        {
            var returns = new DOTBaggageCalculatorResponse()
            {
                IsAnyFlightSearch = true,
                TransactionId = "eb49a82f-59f4-449c-91a3-9c41d2a7a636%7C8810cdc8-6adb-427b-ba6d-da9dcbcdda87"
            };

            _bagCalculatorController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            _bagCalculatorController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";
            _bagCalculatorBusiness.Setup(p => p.GetBaggageCalculatorSearch(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>())).ThrowsAsync(new Exception("Error Message"));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();

            var result = _bagCalculatorController.GetBaggageCalculatorSearchValues("ACCESSCODE", "d007548c-addf-43fb-8f46-6870aec49647%7Cd5454749-3290-44a7-9782-65f5c30538e8", "en-US", "4.1.31", 2);

            Assert.True(result != null && result.Exception == null);
            Assert.True(result.Result.CallDuration != null);

            Assert.True(result.Result.Exception.Message == "United data services are not currently available.");
        }
        
        //GetMobileCMSContents

        [Fact]
        public void GetMobileCMSContents()
        {
            var request = new Request<MobileCMSContentRequest>()
            {
                Data = new MobileCMSContentRequest()
                {
                    AccessCode = "ACCESSCODE",
                    MileagePlusNumber = "AW791957",
                    GroupName = "Mobile:Baggage",
                    DeviceId = "eb49a82f-59f4-449c-91a3-9c41d2a7a636",
                    LanguageCode = "en-US",
                    TransactionId = "eb49a82f-59f4-449c-91a3-9c41d2a7a636|658a3985-080c-4364-8493-b15461a6e01f",
                    Application = new MOBApplication()
                    {
                        Id = 2,
                        Version = new MOBVersion()
                        {
                            Major = "4.1.23",
                            Minor = "4.1.23"
                        }
                    },
                }
            };

            _bagCalculatorController.ControllerContext = new ControllerContext();
            _bagCalculatorController.ControllerContext.HttpContext = new DefaultHttpContext();
            _bagCalculatorController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";
            _bagCalculatorBusiness.Setup(p => p.GetMobileCMSContentsData(request.Data));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();
            _bagCalculatorBusiness.Setup(p => p.GetMobileCMSContentsData(request.Data)).ThrowsAsync(new MOBUnitedException());
            var result = _bagCalculatorController.GetMobileCMSContents(request.Data);

            Assert.True(result != null);
            Assert.False(result.Result.CallDuration == null);
        }

        [Fact]
        public void GetMobileCMSContents_MOBUnitedException()
        {
            var request = new Request<MobileCMSContentRequest>()
            {
                Data = new MobileCMSContentRequest()
                {
                    AccessCode = "ACCESSCODE",
                    MileagePlusNumber = "AW791957",
                    GroupName = "Mobile:Baggage",
                    DeviceId = "eb49a82f-59f4-449c-91a3-9c41d2a7a636",
                    LanguageCode = "en-US",
                    TransactionId = "eb49a82f-59f4-449c-91a3-9c41d2a7a636|658a3985-080c-4364-8493-b15461a6e01f",
                    Application = new MOBApplication()
                    {
                        Id = 2,
                        Version = new MOBVersion()
                        {
                            Major = "4.1.23",
                            Minor = "4.1.23"
                        }
                    },
                }
            };

            _bagCalculatorController.ControllerContext = new ControllerContext();
            _bagCalculatorController.ControllerContext.HttpContext = new DefaultHttpContext();
            _bagCalculatorController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";
            _bagCalculatorBusiness.Setup(p => p.GetMobileCMSContentsData(request.Data));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();
            _bagCalculatorBusiness.Setup(p => p.GetMobileCMSContentsData(request.Data)).ThrowsAsync(new MOBUnitedException());
            var result = _bagCalculatorController.GetMobileCMSContents(request.Data);

            Assert.True(result != null && result.Exception == null);
            Assert.True(result.Result.CallDuration != null);

            //Assert.True(result.Result.Exception.Message == "Exception of type 'United.Mobile.Model.Internal.Exception.MOBUnitedException' was thrown.");
        }

        [Theory]
        [InlineData("Error Message")]
        //[InlineData("")]
        public void GetMobileCMSContents_SystemException(string error)
        {
            var request = new Request<MobileCMSContentRequest>()
            {
                Data = new MobileCMSContentRequest()
                {
                    AccessCode = "ACCESSCODE",
                    MileagePlusNumber = "AW791957",
                    GroupName = "Mobile:Baggage",
                    DeviceId = "eb49a82f-59f4-449c-91a3-9c41d2a7a636",
                    LanguageCode = "en-US",
                    TransactionId = "eb49a82f-59f4-449c-91a3-9c41d2a7a636|658a3985-080c-4364-8493-b15461a6e01f",
                    Application = new MOBApplication()
                    {
                        Id = 2,
                        Version = new MOBVersion()
                        {
                            Major = "4.1.23",
                            Minor = "4.1.23"
                        }
                    },
                }
            };

            _bagCalculatorController.ControllerContext = new ControllerContext();
            _bagCalculatorController.ControllerContext.HttpContext = new DefaultHttpContext();
            _bagCalculatorController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";
            _bagCalculatorBusiness.Setup(p => p.GetMobileCMSContentsData(request.Data));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();
            _bagCalculatorBusiness.SetupSequence(p => p.GetMobileCMSContentsData(request.Data)).ThrowsAsync(new Exception());

            if (error == "")
                _configuration["SurfaceErrorToClient"] = "false";
            var result = _bagCalculatorController.GetMobileCMSContents(request.Data);

            Assert.True(result != null && result.Exception == null);
            Assert.True(result.Result.CallDuration != null);

            Assert.True(result.Result.Exception.Message == "Sorry, something went wrong. Please try again.");
        }

        [Fact]
        public string HealthCheck_Test()
        {
            return _bagCalculatorController.HealthCheck();
        }
    }
}

