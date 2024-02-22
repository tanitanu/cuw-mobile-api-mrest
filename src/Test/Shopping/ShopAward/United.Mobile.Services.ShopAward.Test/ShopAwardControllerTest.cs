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
using United.Mobile.Model.Shopping.AwardCalendar;
using United.Mobile.Services.ShopAward.Api.Controllers;
using United.Mobile.Services.ShopAward.Domain;
using United.Utility.Helper;
using Xunit;

namespace United.Mobile.Shopping.ShopAward.Tests
{
    public class ShopAwardControllerTest
    {
        private IConfiguration _config;
        private readonly Mock<ICacheLog<ShopAwardController>> _logger;
        private readonly Mock<IShopAwardBusiness> _ShopAwardBusiness;
        private readonly ShopAwardController _ShopAwardController;
        private readonly Mock<IRequestEnricher> _requestEnricher;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private Mock<HttpContextAccessor> _moqhttpContextAccessor;
        private HttpContextAccessor _httpContextAccessor1;
        private readonly Mock<IHeaders> _moqHeader;
        private readonly Mock<IApplicationEnricher> _applicationEnricher;
        private readonly Mock<IFeatureSettings> _featureSetting;
        public ShopAwardControllerTest()
        {
            _moqHeader = new Mock<IHeaders>();
            _logger = new Mock<ICacheLog<ShopAwardController>>();
            _ShopAwardBusiness = new Mock<IShopAwardBusiness>();
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(Configuration);
            _requestEnricher = new Mock<IRequestEnricher>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _applicationEnricher = new Mock<IApplicationEnricher>();
            _ShopAwardController = new ShopAwardController(_logger.Object, _config, _ShopAwardBusiness.Object, _moqHeader.Object,_applicationEnricher.Object, _featureSetting.Object);

            SetupHttpContextAccessor();


            _config = new ConfigurationBuilder().AddJsonFile("appSettings.Development.json").Build();
            _logger = new Mock<ICacheLog<ShopAwardController>>();
            var mockShopFlightDetailsBusiness = new Mock<IConfiguration>();
            _ShopAwardBusiness = new Mock<IShopAwardBusiness>();
            _requestEnricher = new Mock<IRequestEnricher>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();

            _ShopAwardBusiness = new Mock<IShopAwardBusiness>();
            _requestEnricher = new Mock<IRequestEnricher>();
            _httpContextAccessor1 = new HttpContextAccessor();

            _moqhttpContextAccessor = new Mock<HttpContextAccessor>();
            _ShopAwardController = new ShopAwardController(_logger.Object, _config, _ShopAwardBusiness.Object, _moqHeader.Object,_applicationEnricher.Object,_featureSetting.Object);
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
        //---------------1

        [Fact]
        public void RevenueLowestPriceForAwardSearch()
        {
            var returns = new RevenueLowestPriceForAwardSearchResponse() { };

            var request = new MOBSHOPShopRequest()
            {
                SessionId = "S",
                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };

            _ShopAwardController.ControllerContext = new ControllerContext();
            _ShopAwardController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopAwardController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopAwardBusiness.Setup(p => p.RevenueLowestPriceForAwardSearch(request)).ReturnsAsync(returns);

  
            // Act
            var result = _ShopAwardController.RevenueLowestPriceForAwardSearch(request);
            var health = _ShopAwardController.HealthCheck();

            // Assert
            Assert.True(result != null);
            Assert.True(health == "Healthy");

        }

        [Fact]
        public void RevenueLowestPriceForAwardSearch_MOBUnitedException()
        {
            var returns = new RevenueLowestPriceForAwardSearchResponse() { };

            var request = new MOBSHOPShopRequest()

         
            {
                SessionId = "S",

                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };
            _ShopAwardController.ControllerContext = new ControllerContext();
            _ShopAwardController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopAwardController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopAwardBusiness.Setup(p => p.RevenueLowestPriceForAwardSearch(request)).ThrowsAsync(new MOBUnitedException());

            

            var result = _ShopAwardController.RevenueLowestPriceForAwardSearch(request);

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
            var returns = new RevenueLowestPriceForAwardSearchResponse() { };

            var request = new MOBSHOPShopRequest()

           
            {
                SessionId = "S",

                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };
            _ShopAwardController.ControllerContext = new ControllerContext();
            _ShopAwardController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopAwardController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopAwardBusiness.Setup(p => p.RevenueLowestPriceForAwardSearch(request)).ThrowsAsync(new System.Exception());



            var result = _ShopAwardController.RevenueLowestPriceForAwardSearch(request);
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
        public void GetSelectTripAwardCalendar()
        {
            var returns = new AwardCalendarResponse() { };

            var request = new SelectTripRequest()
            {
                SessionId = "S",
                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };

            _ShopAwardController.ControllerContext = new ControllerContext();
            _ShopAwardController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopAwardController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopAwardBusiness.Setup(p => p.GetSelectTripAwardCalendar(request)).ReturnsAsync(returns);


            // Act
            var result = _ShopAwardController.GetSelectTripAwardCalendar(request);

            // Assert
            Assert.True(result != null);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);

        }

        [Fact]
        public void GetSelectTripAwardCalendar_MOBUnitedException()
        {
            var returns = new AwardCalendarResponse() { };

            var request = new SelectTripRequest()


            {
                SessionId = "S",

                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };
            _ShopAwardController.ControllerContext = new ControllerContext();
            _ShopAwardController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopAwardController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopAwardBusiness.Setup(p => p.GetSelectTripAwardCalendar(request)).ThrowsAsync(new MOBUnitedException());



            var result = _ShopAwardController.GetSelectTripAwardCalendar(request);

            try
            {

                var actual = result;
            }
            catch (Exception ex)
            {
                Assert.True(false, ex.Message);
                throw;
            }
            Assert.True(result != null);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Fact]
        public void GetSelectTripAwardCalendar_SystemException()
        {
            var returns = new AwardCalendarResponse() { };

            var request = new SelectTripRequest()


            {
                SessionId = "S",

                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };
            _ShopAwardController.ControllerContext = new ControllerContext();
            _ShopAwardController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopAwardController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopAwardBusiness.Setup(p => p.GetSelectTripAwardCalendar(request)).ThrowsAsync(new System.Exception());



            var result = _ShopAwardController.GetSelectTripAwardCalendar(request);
            try
            {
                var actual = result;
            }
            catch (Exception ex)
            {
                Assert.Equal("Sorry, something went wrong. Please try again.", ex.Message);
                throw;
            }
            Assert.True(result != null);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Fact]
        public void GetShopAwardCalendar()
        {
            var returns = new AwardCalendarResponse() { };

            var request = new MOBSHOPShopRequest()
            {
                SessionId = "S",
                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };

            _ShopAwardController.ControllerContext = new ControllerContext();
            _ShopAwardController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopAwardController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopAwardBusiness.Setup(p => p.GetShopAwardCalendar(request)).ReturnsAsync(returns);


            // Act
            var result = _ShopAwardController.GetShopAwardCalendar(request);

            // Assert
            Assert.True(result != null);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);

        }

        [Fact]
        public void GetShopAwardCalendar_MOBUnitedException()
        {
            var returns = new AwardCalendarResponse() { };

            var request = new MOBSHOPShopRequest()


            {
                SessionId = "S",

                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };
            _ShopAwardController.ControllerContext = new ControllerContext();
            _ShopAwardController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopAwardController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopAwardBusiness.Setup(p => p.GetShopAwardCalendar(request)).ThrowsAsync(new MOBUnitedException());



            var result = _ShopAwardController.GetShopAwardCalendar(request);

            try
            {

                var actual = result;
            }
            catch (Exception ex)
            {
                Assert.True(false, ex.Message);
                throw;
            }
            Assert.True(result != null);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Fact]
        public void GetShopAwardCalendar_SystemException()
        {
            var returns = new AwardCalendarResponse() { };

            var request = new MOBSHOPShopRequest()


            {
                SessionId = "S",

                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };
            _ShopAwardController.ControllerContext = new ControllerContext();
            _ShopAwardController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopAwardController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopAwardBusiness.Setup(p => p.GetShopAwardCalendar(request)).ThrowsAsync(new System.Exception());

            var result = _ShopAwardController.GetShopAwardCalendar(request);
            try
            {
                var actual = result;
            }
            catch (Exception ex)
            {
                Assert.Equal("Sorry, something went wrong. Please try again.", ex.Message);
                throw;
            }
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

