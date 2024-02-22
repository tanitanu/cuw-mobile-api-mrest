using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using United.Common.Helper;
using United.Ebs.Logging.Enrichers;
using United.Mobile.Model;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.TeaserPage;
using United.Mobile.Services.ShopFlightDetails.Api.Controllers;
using United.Mobile.Services.ShopFlightDetails.Domain;
using Xunit;
using United.Utility.Helper;

namespace United.Mobile.Shopping.ShopFlightDetails.Tests
{
    public class ShopFlightDetailsTest:ControllerBase
    {
        private IConfiguration _config;
        private readonly Mock<ICacheLog<ShopFlightDetailsController>> _logger;

        private readonly Mock<IShopFlightDetailsBusiness> _ShopFlightDetailsBusiness;
        private readonly ShopFlightDetailsController _ShopFlightDetailsController;
        private readonly Mock<ITeaserPageBusiness> _teaserPageBusiness;


        private readonly Mock<IRequestEnricher> _requestEnricher;
        private readonly Mock<IApplicationEnricher> _applicationEnricher;

        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

        private readonly IConfiguration _configuration;
        
        private Mock<HttpContextAccessor> _moqhttpContextAccessor;
        private HttpContextAccessor _httpContextAccessor1;

        private readonly Mock<IHeaders> _moqHeader;
        private readonly Mock<IFeatureSettings> _featureSettings;

        public ShopFlightDetailsTest()
        {
            _moqHeader = new Mock<IHeaders>();
            _logger = new Mock<ICacheLog<ShopFlightDetailsController>>();
            _ShopFlightDetailsBusiness = new Mock<IShopFlightDetailsBusiness>();
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(Configuration);
            _requestEnricher = new Mock<IRequestEnricher>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _applicationEnricher = new Mock<IApplicationEnricher>();
            _teaserPageBusiness = new Mock<ITeaserPageBusiness>();
            _ShopFlightDetailsController = new ShopFlightDetailsController(_logger.Object, _config, _moqHeader.Object, _applicationEnricher.Object, _ShopFlightDetailsBusiness.Object, _teaserPageBusiness.Object, _featureSettings.Object);

            SetupHttpContextAccessor();


            _config = new ConfigurationBuilder().AddJsonFile("appSettings.Development.json").Build();
            var mockShopFlightDetailsBusiness = new Mock<IConfiguration>();


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
        //---------------1

        [Fact]
        public void GetONTimePerformence()
        {
            var returns = new OnTimePerformanceResponse() { };

            var request = new OnTimePerformanceRequest()
            {
                SessionId = "S",
                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };

            _ShopFlightDetailsController.ControllerContext = new ControllerContext();
            _ShopFlightDetailsController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopFlightDetailsController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopFlightDetailsBusiness.Setup(p => p.GetONTimePerformence(request)).ReturnsAsync(returns);

  
            // Act
            var result = _ShopFlightDetailsController.GetONTimePerformence(request);

            // Assert
            Assert.True(result != null);

        }

        [Fact]
        public void GetONTimePerformence_MOBUnitedException()
        {
            var returns = new OnTimePerformanceResponse() { };

            var request = new OnTimePerformanceRequest()

         
            {
                SessionId = "S",

                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };
            _ShopFlightDetailsController.ControllerContext = new ControllerContext();
            _ShopFlightDetailsController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopFlightDetailsController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopFlightDetailsBusiness.Setup(p => p.GetONTimePerformence(request)).ThrowsAsync(new MOBUnitedException());

            

            var result = _ShopFlightDetailsController.GetONTimePerformence(request);

            try
            {

                var actual = result;
            }
            catch (Exception ex)
            {
                Assert.True(false, ex.Message);
                throw;
            }
        }

        [Fact]
        public void GetONTimePerformence_SystemException()
        {
            var returns = new OnTimePerformanceResponse() { };

            var request = new OnTimePerformanceRequest()

           
            {
                SessionId = "S",

                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };
            _ShopFlightDetailsController.ControllerContext = new ControllerContext();
            _ShopFlightDetailsController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopFlightDetailsController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopFlightDetailsBusiness.Setup(p => p.GetONTimePerformence(request)).ThrowsAsync(new System.Exception());



            var result = _ShopFlightDetailsController.GetONTimePerformence(request);
            try
            {
                var actual = result;
            }
            catch (Exception ex)
            {
                Assert.Equal("Sorry, something went wrong. Please try again.", ex.Message);
                throw;
            }
        }
        [Fact]
        public void GetTeaserPage()
        {
            var returns = new MOBSHOPShoppingTeaserPageResponse() { };
            var request = new MOBSHOPShoppingTeaserPageRequest()
            {
                SessionID = "A5538976AFFVAVFFKKSJJ",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _teaserPageBusiness.Setup(p => p.GetTeaserPage(request, HttpContext)).ReturnsAsync(returns);
            //Act
            var result = _ShopFlightDetailsController.GetTeaserPage(request);
            // Assert
            Assert.True(result != null);

        }

        [Fact]
        public void GetTeaserPage_Test_Exception()
        {
            var returns = new MOBSHOPShoppingTeaserPageResponse();
            var request = new MOBSHOPShoppingTeaserPageRequest()
            {
                SessionID = "A5538976AFFVAVFFKKSJJ",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _teaserPageBusiness.Setup(p => p.GetTeaserPage(request, HttpContext)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _ShopFlightDetailsController.GetTeaserPage(request);
            // Assert
            Assert.True(result.Result.Exception != null);
        }


        [Fact]
        public void GetTeaserPage_Test_MOBUnitedException()
        {
            var returns = new MOBSHOPShoppingTeaserPageResponse();
            var request = new MOBSHOPShoppingTeaserPageRequest()
            {
                SessionID = "A5538976AFFVAVFFKKSJJ",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _teaserPageBusiness.Setup(p => p.GetTeaserPage(request, HttpContext)).ThrowsAsync(new MOBUnitedException("Error Message"));
            // Act
            var result = _ShopFlightDetailsController.GetTeaserPage(request);
            // Assert
            Assert.True(result.Result.Exception != null);
        }
    }

}

