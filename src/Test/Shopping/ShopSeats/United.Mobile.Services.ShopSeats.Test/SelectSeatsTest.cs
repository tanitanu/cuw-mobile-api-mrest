using Moq;
using System;
using United.Mobile.Services.ShopSeats.Api.Controllers;
using United.Mobile.Services.ShopSeats.Domain;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using United.Common.Helper;
using United.Ebs.Logging.Enrichers;
using United.Mobile.Model;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Utility.Helper;

namespace United.Mobile.Services.ShopSeats.Test
{
    public class SelectSeatsTest
    {
        private IConfiguration _config;
        private readonly Mock<ICacheLog<ShopSeatsController>> _logger;

        private readonly Mock<IShopSeatsBusiness> _ShopFlightDetailsBusiness;
        private readonly ShopSeatsController _ShopFlightDetailsController;

        private readonly Mock<IRequestEnricher> _requestEnricher;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

        private readonly IConfiguration _configuration;

        private Mock<HttpContextAccessor> _moqhttpContextAccessor;
        private HttpContextAccessor _httpContextAccessor1;

        private readonly Mock<IHeaders> _moqHeader;
        private readonly Mock<IFeatureSettings> _featureSetting;

        public SelectSeatsTest()
        {
            _moqHeader = new Mock<IHeaders>();
            _logger = new Mock<ICacheLog<ShopSeatsController>>();
            _ShopFlightDetailsBusiness = new Mock<IShopSeatsBusiness>();
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(Configuration);
            _requestEnricher = new Mock<IRequestEnricher>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();

            _ShopFlightDetailsController = new ShopSeatsController(_logger.Object, _config, _ShopFlightDetailsBusiness.Object, _moqHeader.Object,_featureSetting.Object);

            SetupHttpContextAccessor();


            _config = new ConfigurationBuilder().AddJsonFile("appSettings.Development.json").Build();
            _logger = new Mock<ICacheLog<ShopSeatsController>>();
            var mockShopFlightDetailsBusiness = new Mock<IConfiguration>();
            _ShopFlightDetailsBusiness = new Mock<IShopSeatsBusiness>();
            _requestEnricher = new Mock<IRequestEnricher>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();

            _ShopFlightDetailsBusiness = new Mock<IShopSeatsBusiness>();
            _requestEnricher = new Mock<IRequestEnricher>();
            _httpContextAccessor1 = new HttpContextAccessor();

            _moqhttpContextAccessor = new Mock<HttpContextAccessor>();
            _ShopFlightDetailsController = new ShopSeatsController(_logger.Object, _config, _ShopFlightDetailsBusiness.Object, _moqHeader.Object,_featureSetting.Object);
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
        public void SelectSeats()
        {
            var returns = new SelectSeatsResponse() { };

            var request = new SelectSeatsRequest()
            {
                SessionId = "S",
                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };

            _ShopFlightDetailsController.ControllerContext = new ControllerContext();
            _ShopFlightDetailsController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopFlightDetailsController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopFlightDetailsBusiness.Setup(p => p.SelectSeats(request)).ReturnsAsync(returns);


            // Act
            var result = _ShopFlightDetailsController.SelectSeats(request);
            var health = _ShopFlightDetailsController.HealthCheck();

            // Assert
            Assert.True(result != null);
            Assert.True(health == "Healthy");
            Assert.True(result.Result.CallDuration > 0);
            Assert.True(result.Result.Exception == null);
        }

        [Fact]
        public void SelectSeats_MOBUnitedException()
        {
            var returns = new SelectSeatsResponse() { };

            var request = new SelectSeatsRequest()


            {
                SessionId = "S",

                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };
            _ShopFlightDetailsController.ControllerContext = new ControllerContext();
            _ShopFlightDetailsController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopFlightDetailsController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopFlightDetailsBusiness.Setup(p => p.SelectSeats(request)).ThrowsAsync(new MOBUnitedException());



            var result = _ShopFlightDetailsController.SelectSeats(request);

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
        public void SelectSeats_SystemException()
        {
            var returns = new SelectSeatsResponse() { };

            var request = new SelectSeatsRequest()


            {
                SessionId = "S",

                TransactionId = "T",
                DeviceId = "D",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "Major" } }
            };
            _ShopFlightDetailsController.ControllerContext = new ControllerContext();
            _ShopFlightDetailsController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShopFlightDetailsController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _ShopFlightDetailsBusiness.Setup(p => p.SelectSeats(request)).ThrowsAsync(new System.Exception());



            var result = _ShopFlightDetailsController.SelectSeats(request);
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
    }
}
