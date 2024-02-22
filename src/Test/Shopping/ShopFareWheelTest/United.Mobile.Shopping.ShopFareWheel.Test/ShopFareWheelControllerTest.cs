
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using United.Mobile.Model;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.DataAccess.Common;
using United.Mobile.Services.ShopFareWheel.Api.Controllers;
using United.Mobile.Services.ShopFareWheel.Domain;
using Xunit;
using United.Mobile.Model.Shopping;
using Microsoft.AspNetCore.Mvc;
using United.Mobile.Model.Internal.Exception;
using United.Ebs.Logging.Enrichers;
using United.Utility.Helper;

namespace United.Mobile.Shopping.ShopFareWheel.Test
{
    public class ShopFareWheelControllerTest
    {
        private readonly Mock<ICacheLog<ShopFareWheelController>> _logger;
        private IConfiguration _configuration;
        private readonly Mock<IShopFareWheelBusiness> _shopfarewheelBusiness;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<IDPService> _tokenService;
        private readonly ShopFareWheelController _shopfarewheelController;
        private readonly Mock<IHeaders> _headers; private IConfigurationRoot configuration;
        private readonly Mock<IApplicationEnricher> _requestEnricher;
        private readonly Mock<IFeatureSettings> _featureSetting;
        public IConfiguration Configuration
        {
            get
            {
                //if (_configuration == null)
                //{
                _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
                .Build();
                //}
                return _configuration;
            }
        }

        public IConfiguration Configuration1
        {
            get
            {
                //if (_configuration == null)
                //{
                _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings1.test.json", optional: false, reloadOnChange: true)
                .Build();
                //}
                return _configuration;
            }
        }
        public ShopFareWheelControllerTest()
        {
            _logger = new Mock<ICacheLog<ShopFareWheelController>>();
            _headers = new Mock<IHeaders>();
            _shopfarewheelBusiness = new Mock<IShopFareWheelBusiness>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _tokenService = new Mock<IDPService>();
             _requestEnricher = new Mock<IApplicationEnricher>();
            SetupHttpContextAccessor();
            _shopfarewheelController = new ShopFareWheelController(_logger.Object, Configuration, _shopfarewheelBusiness.Object, _headers.Object, _requestEnricher.Object,_featureSetting.Object);
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

        [Fact]
        public void HealthCheck_Test()
        {
            var returns = "Healthy";
            var result = _shopfarewheelController.HealthCheck();
            Assert.Equal(returns, result);
        }

        [Fact]
        public void GetShopFareWheelList_Test()
        {
            var returns = new FareWheelResponse();
            var shopfarewheelrequest = new MOBSHOPShopRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                CountryCode = "",
                SessionId = "",
                DeviceId= "20317",
                AccessCode="ACCESSCODE",
                NumberOfSeniors = 8,
                NumberOfChildren5To11 = 6,
                NumberOfChildren12To17 = 6,
                NumberOfInfantOnLap = 5,
                Flow = ""
            };
           
            _shopfarewheelBusiness.Setup(p => p.GetShopFareWheelListResponse(shopfarewheelrequest)).Returns(Task.FromResult(returns)); 
            //Act
            var result = _shopfarewheelController.GetShopFareWheelList(shopfarewheelrequest);
            //Assert
            Assert.True(result.Result != null);
        }

        [Fact]
        public void GetShopFareWheelList_Test_Exception()
        {
            var returns = new FareWheelResponse();
            var shopbundlesrequest = new MOBSHOPShopRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                CountryCode = "",
                SessionId = "",
                DeviceId = "20317",
                AccessCode = "ACCESSCODE",
                NumberOfSeniors = 8,
                NumberOfChildren5To11 = 6,
                NumberOfChildren12To17 = 6,
                NumberOfInfantOnLap = 5,
                Flow = ""
            };
            
            _shopfarewheelBusiness.Setup(p => p.GetShopFareWheelListResponse(shopbundlesrequest)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _shopfarewheelController.GetShopFareWheelList(shopbundlesrequest);
            // Assert
            Assert.True(result.Result != null);
            Assert.True(result.Result.Exception.Message.Equals(Configuration.GetValue<string>("Booking2OGenericExceptionMessage")));

        }
        [Fact]
        public void GetShopFareWheelList_Test_Exception1()
        {

            var _shopfarewheelController = new ShopFareWheelController(_logger.Object, Configuration1, _shopfarewheelBusiness.Object, _headers.Object,_requestEnricher.Object,_featureSetting.Object);
           
            var returns = new FareWheelResponse();
            var shopbundlesrequest = new MOBSHOPShopRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                CountryCode = "",
                SessionId = "",
                DeviceId = "20317",
                AccessCode = "ACCESSCODE",
                NumberOfSeniors = 8,
                NumberOfChildren5To11 = 6,
                NumberOfChildren12To17 = 6,
                NumberOfInfantOnLap = 5,
                Flow = ""
            };

            _shopfarewheelBusiness.Setup(p => p.GetShopFareWheelListResponse(shopbundlesrequest)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _shopfarewheelController.GetShopFareWheelList(shopbundlesrequest);
            // Assert
            Assert.True(result.Result != null);

        }


        [Fact]
        public void GetShopFareWheelList_Test_MOBUnitedException()
        {
            var returns = new FareWheelResponse();
            var shopbundlesrequest = new MOBSHOPShopRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                CountryCode = "",
                SessionId = "",
                DeviceId = "20317",
                AccessCode = "ACCESSCODE",
                NumberOfSeniors = 8,
                NumberOfChildren5To11 = 6,
                NumberOfChildren12To17 = 6,
                NumberOfInfantOnLap = 5,
                Flow = ""
            };
           
            _shopfarewheelBusiness.Setup(p => p.GetShopFareWheelListResponse(shopbundlesrequest)).ThrowsAsync(new MOBUnitedException("Error Message"));
            // Act
            var result = _shopfarewheelController.GetShopFareWheelList(shopbundlesrequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Fact]
        public void GetFareWheelList_Test()
        {
             var returns = new FareWheelResponse();
            var selectTripRequest = new SelectTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                SessionId = "",
                DeviceId = "20317",
                AccessCode = "ACCESSCODE",
            };

            _shopfarewheelBusiness.Setup(p => p.GetFareWheelListResponse(selectTripRequest)).Returns(Task.FromResult(returns));
            //Act
            var result = _shopfarewheelController.GetFareWheelList(selectTripRequest);
            // Assert
            Assert.True(result.Result != null);
        }
        [Fact]
        public void GetFareWheelList_Test_Exception()
        {
            var selectTripRequest = new SelectTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                SessionId = "",
                DeviceId = "20317",
                AccessCode = "ACCESSCODE",
            };

            _shopfarewheelBusiness.Setup(p => p.GetFareWheelListResponse(selectTripRequest)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _shopfarewheelController.GetFareWheelList(selectTripRequest);
            // Assert
            Assert.True(result.Result != null);
            Assert.True(result.Result.Exception.Message.Equals(Configuration.GetValue<string>("Booking2OGenericExceptionMessage")));
        }
        [Fact]
        public void GetFareWheelList_Test_Exception1()
        {
            var _shopfarewheelController = new ShopFareWheelController(_logger.Object, Configuration1, _shopfarewheelBusiness.Object, _headers.Object,_requestEnricher.Object,_featureSetting.Object);

            var selectTripRequest = new SelectTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                SessionId = "",
                DeviceId = "20317",
                AccessCode = "ACCESSCODE",
            };

            _shopfarewheelBusiness.Setup(p => p.GetFareWheelListResponse(selectTripRequest)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _shopfarewheelController.GetFareWheelList(selectTripRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Fact]
        public void GetFareWheelList_Test_MOBUnitedException()
        {
            var selectTripRequest = new SelectTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                SessionId = "",
                DeviceId = "20317",
                AccessCode = "ACCESSCODE",
            };
            
            _shopfarewheelBusiness.Setup(p => p.GetFareWheelListResponse(selectTripRequest)).ThrowsAsync(new MOBUnitedException("Error Message"));
            // Act
            var result = _shopfarewheelController.GetFareWheelList(selectTripRequest);
            // Assert
            Assert.True(result.Result != null);
        }








    }
}

