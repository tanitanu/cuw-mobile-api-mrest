using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Ebs.Logging.Enrichers;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Services.ShopTrips.Api.Controllers;
using United.Mobile.Services.ShopTrips.Domain;
using Xunit;
using United.Utility.Helper;

namespace United.Mobile.Shopping.ShopTrip.Test
{
    public class ShopTripControllerTest
    {
        private readonly Mock<ICacheLog<ShopTripsController>> _logger;
        private IConfiguration _configuration;
        private readonly Mock<IShopTripsBusiness> _shoptripsBusiness;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<IDPService> _tokenService;
        private readonly ShopTripsController _shoptripsController;
        private readonly Mock<IHeaders> _headers;
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
                .AddJsonFile(@"TestData\SelectTrip_TestData\appsettings.test.json", optional: false, reloadOnChange: true)
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
        public ShopTripControllerTest()
        {
            _logger = new Mock<ICacheLog<ShopTripsController>>();
            _headers = new Mock<IHeaders>();
            _shoptripsBusiness = new Mock<IShopTripsBusiness>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _tokenService = new Mock<IDPService>();
            _requestEnricher = new Mock<IApplicationEnricher>();
            SetupHttpContextAccessor();
            _shoptripsController = new ShopTripsController(_logger.Object, Configuration, _shoptripsBusiness.Object, _headers.Object, _requestEnricher.Object,_featureSetting.Object);
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
            var result = _shoptripsController.HealthCheck();
            Assert.Equal(returns, result);
        }

        [Fact]
        public void GetTripCompareFareTypes_Test()
        {
            var returns = new ShoppingTripFareTypeDetailsResponse();
            var shoppingtripfaretypedetailsRequest = new ShoppingTripFareTypeDetailsRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoptripsBusiness.Setup(p => p.GetTripCompareFareTypes(shoppingtripfaretypedetailsRequest)).Returns(Task.FromResult(returns));
            //Act
            var result = _shoptripsController.GetTripCompareFareTypes(shoppingtripfaretypedetailsRequest);
            //Assert
            Assert.True(result.Result != null);
        }
        [Fact]
        public void GetTripCompareFareTypes_Test_Exception()
        {
            var returns = new ShoppingTripFareTypeDetailsResponse();
            var shoppingtripfaretypedetailsRequest = new ShoppingTripFareTypeDetailsRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoptripsBusiness.Setup(p => p.GetTripCompareFareTypes(shoppingtripfaretypedetailsRequest)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _shoptripsController.GetTripCompareFareTypes(shoppingtripfaretypedetailsRequest);
            // Assert
            Assert.True(result.Result != null);
            Assert.True(result.Result.Exception.Message.Equals(Configuration.GetValue<string>("Booking2OGenericExceptionMessage")));
        }
        [Fact]
        public void GetTripCompareFareTypes_Test_MOBUnitedException()
        {
            var returns = new ShoppingTripFareTypeDetailsResponse();
            var shoppingtripfaretypedetailsRequest = new ShoppingTripFareTypeDetailsRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoptripsBusiness.Setup(p => p.GetTripCompareFareTypes(shoppingtripfaretypedetailsRequest)).ThrowsAsync(new MOBUnitedException("Error Message"));
            // Act
            var result = _shoptripsController.GetTripCompareFareTypes(shoppingtripfaretypedetailsRequest);
            // Assert
            Assert.True(result.Result != null);
        }



        [Fact]
        public void GetShareTrip_Test()
        {
            var returns = new TripShare();
            var shareTripRequest = new ShareTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoptripsBusiness.Setup(p => p.GetShareTrip(shareTripRequest)).Returns(Task.FromResult(returns));
            //Act
            var result = _shoptripsController.GetShareTrip(shareTripRequest);
            // Assert
            Assert.True(result.Result != null);
        }
        [Fact]
        public void GetShareTrip_Test_Exception()
        {
            var returns = new TripShare();
            var shareTripRequest = new ShareTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoptripsBusiness.Setup(p => p.GetShareTrip(shareTripRequest)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _shoptripsController.GetShareTrip(shareTripRequest);
            // Assert
            Assert.True(result.Result != null);
            Assert.True(result.Result.Exception.Message.Equals(Configuration.GetValue<string>("Booking2OGenericExceptionMessage")));

        }
        [Fact]
        public void GetShareTrip_Test_MOBUnitedException()
        {
            var returns = new TripShare();
            var shareTripRequest = new ShareTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };


            _shoptripsBusiness.Setup(p => p.GetShareTrip(shareTripRequest)).ThrowsAsync(new MOBUnitedException("Error Message"));
            // Act
            var result = _shoptripsController.GetShareTrip(shareTripRequest);
            // Assert
            Assert.True(result.Result != null);
        }
        [Fact]
        public void SelectTrip_Test()
        {
            var returns = new SelectTripResponse();
            var selectTripRequest = new SelectTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            //_shoptripsBusiness.Setup(p => p.SelectTrip(selectTripRequest)).Returns(Task.FromResult(returns));
            ////Act
            //var result = _shoptripsController.SelectTrip(selectTripRequest);
            //// Assert
            //Assert.True(result.Result != null);
        }
        [Fact]
        public void SelectTrip_Test_Exception()
        {
            var returns = new SelectTripResponse();
            var selectTripRequest = new SelectTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            //_shoptripsBusiness.Setup(p => p.SelectTrip(selectTripRequest)).ThrowsAsync(new Exception("Error Message"));
            //// Act
            //var result = _shoptripsController.SelectTrip(selectTripRequest);
            //// Assert
            //Assert.True(result.Result != null);
            //  Assert.True(result.Result.Exception.Message.Equals(Configuration.GetValue<string>("Booking2OGenericExceptionMessage")));



        }
        [Fact]
        public void SelectTrip_Test_MOBUnitedException()
        {
            var returns = new SelectTripResponse();
            var selectTripRequest = new SelectTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            //_shoptripsBusiness.Setup(p => p.SelectTrip(selectTripRequest)).ThrowsAsync(new MOBUnitedException("Error Message"));
            //// Act
            //var result = _shoptripsController.SelectTrip(selectTripRequest);
            //// Assert
            //Assert.True(result.Result != null);
        }

        [Fact]
        public void MetaSelectTripp_Test()
        {
            var returns = new SelectTripResponse()
            {
                TransactionId = "3783486ATWHYT-67389377",
                LanguageCode = "en-US",
            };
            var metaSelectTripRequest = new MetaSelectTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "3783486ATWHYT-67389377",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoptripsBusiness.Setup(p => p.MetaSelectTrip(metaSelectTripRequest,null)).Returns(Task.FromResult(returns));
            //Act
            var result = _shoptripsController.MetaSelectTrip(metaSelectTripRequest);
            // Assert
            Assert.True(result.Result != null);
            Assert.True(result.Result.TransactionId =="3783486ATWHYT-67389377");
        }

        [Fact]
        public void MetaSelectTrip_Test_Exception()
        {
            var returns = new SelectTripResponse();
            var metaSelectTripRequest = new MetaSelectTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoptripsBusiness.Setup(p => p.MetaSelectTrip(metaSelectTripRequest, null)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _shoptripsController.MetaSelectTrip(metaSelectTripRequest);
            // Assert
            Assert.True(result.Result != null);
            //Assert.True(result.Result.Exception.Message.Equals(Configuration.GetValue<string>("Booking2OGenericExceptionMessage")));
            //Assert.True(result.Result.Exception.Message.Equals(Configuration.GetValue<string>("Booking2OGenericExceptionMessage")));

        }
        [Fact]
        public void MetaSelectTrip_Test_MOBUnitedException()
        {
            var returns = new SelectTripResponse();
            var metaSelectTripRequest = new MetaSelectTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoptripsBusiness.Setup(p => p.MetaSelectTrip(metaSelectTripRequest, null)).ThrowsAsync(new MOBUnitedException("Error Message"));
            // Act
            var result = _shoptripsController.MetaSelectTrip(metaSelectTripRequest);
            // Assert
            Assert.True(result.Result != null);
            Assert.True(result.Result.Exception.Message.Equals("Error Message"));
        }

        [Fact]
        public void GetFareRulesForSelectedTrip_Test()
        {
            var returns = new FareRulesResponse();
            var getFareRulesRequest = new GetFareRulesRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoptripsBusiness.Setup(p => p.GetFareRulesForSelectedTrip(getFareRulesRequest)).Returns(Task.FromResult(returns));
            //Act
            var result = _shoptripsController.GetFareRulesForSelectedTrip(getFareRulesRequest);
            Assert.True(result.Result != null);
        }
        [Fact]
        public void GetFareRulesForSelectedTrip_Test_Exception()
        {
            var returns = new FareRulesResponse();
            var getFareRulesRequest = new GetFareRulesRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoptripsBusiness.Setup(p => p.GetFareRulesForSelectedTrip(getFareRulesRequest)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _shoptripsController.GetFareRulesForSelectedTrip(getFareRulesRequest);
            // Assert
            Assert.True(result.Result != null);
            Assert.False(result.Result.Exception.Message.Equals(Configuration.GetValue<string>("Booking2OGenericExceptionMessage")));
        }

        [Fact]
        public void GetFareRulesForSelectedTrip_Test_Exception1()
        {
            var _shoptripsController = new ShopTripsController(_logger.Object, Configuration1, _shoptripsBusiness.Object, _headers.Object, _requestEnricher.Object,_featureSetting.Object);

            var returns = new FareRulesResponse();
            var getFareRulesRequest = new GetFareRulesRequest()
            {

                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoptripsBusiness.Setup(p => p.GetFareRulesForSelectedTrip(getFareRulesRequest)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _shoptripsController.GetFareRulesForSelectedTrip(getFareRulesRequest);
            // Assert
            Assert.True(result.Result != null);
            //Assert.True(result.Result.Exception.Message.Equals(Configuration.GetValue<string>("Booking2OGenericExceptionMessage")));
        }

        [Fact]
        public void GetFareRulesForSelectedTrip_Test_MOBUnitedException()
        {
            var returns = new FareRulesResponse();
            var getFareRulesRequest = new GetFareRulesRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoptripsBusiness.Setup(p => p.GetFareRulesForSelectedTrip(getFareRulesRequest)).ThrowsAsync(new MOBUnitedException("Error Message"));
            // Act
            var result = _shoptripsController.GetFareRulesForSelectedTrip(getFareRulesRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Fact]
        public void GetFareRulesForSelectedTrip_Test_MOBUnitedException1()
        {
            var _shoptripsController = new ShopTripsController(_logger.Object, Configuration1, _shoptripsBusiness.Object, _headers.Object, _requestEnricher.Object,_featureSetting.Object);


            var returns = new FareRulesResponse();
            var getFareRulesRequest = new GetFareRulesRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoptripsBusiness.Setup(p => p.GetFareRulesForSelectedTrip(getFareRulesRequest)).ThrowsAsync(new MOBUnitedException("Error Message"));
            // Act
            var result = _shoptripsController.GetFareRulesForSelectedTrip(getFareRulesRequest);
            // Assert
            Assert.True(result.Result != null);
            //Assert.True(result.Result.Exception.Message.Equals(Configuration.GetValue<string>("Booking2OGenericExceptionMessage")));

        }










    }
}
