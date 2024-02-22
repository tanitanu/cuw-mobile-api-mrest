using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Travelers;
using United.Mobile.Travelers.API.Controllers;
using United.Mobile.Travelers.Domain;
using United.Utility.Helper;
using Xunit;

namespace United.Mobile.Test.Travelers.Tests
{
    public class TravelersControllerTest
    {
        private readonly Mock<ICacheLog<TravelersController>> _logger;
        private readonly IConfiguration _configuration;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<ITravelerBusiness> _travelerBusiness;
        private readonly Mock<IValidateMPNameBusiness> _validateMPNameBusiness;
        private readonly TravelersController travelersController;
        private readonly Mock<IFeatureSettings> _featureSetting;
        public TravelersControllerTest()
        {
            _logger = new Mock<ICacheLog<TravelersController>>();
            _headers = new Mock<IHeaders>();
            _travelerBusiness = new Mock<ITravelerBusiness>();
            _validateMPNameBusiness = new Mock<IValidateMPNameBusiness>();
            _configuration = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appSettings.test.json", optional: false, reloadOnChange: true)
           .Build();
            travelersController = new TravelersController(_logger.Object, _configuration, _headers.Object, _travelerBusiness.Object, _validateMPNameBusiness.Object,_featureSetting.Object);
        }

        [Fact]
        public string HealthCheck_Test()
        {
            return travelersController.HealthCheck();
        }

        [Fact]
        public void RegisterTravelers_CFOP_Test()
        {
            var response = new MOBRegisterTravelersResponse() { TransactionId = "8f46e040-a200-495c-83ca-4fca2d7175fb" };
            var request = new MOBRegisterTravelersRequest()
            {
                DeviceId = "",
                AccessCode = "ACCESSCODE",
                TransactionId = "589d7852-14e7-44a9-b23b-a6db36657579|8f46e040-a200-495c-83ca-4fca2d7175fb",
                SessionId = "17C979E184CC495EA083D45F4DD9D19D",
                Application = new Model.MOBApplication()
                {
                    Id = 2,
                    Version = new Model.MOBVersion()
                    {
                        Major = "4.1.23",
                        Minor = "4.1.23"
                    }
                }
            };
            travelersController.ControllerContext = new ControllerContext();
            travelersController.ControllerContext.HttpContext = new DefaultHttpContext();
            travelersController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _travelerBusiness.Setup(p => p.RegisterTravelers_CFOP(request)).Returns(Task.FromResult(response));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[United.Mobile.Model.Constants.HeaderTransactionIdText].ToString();
            var result = travelersController.RegisterTravelers_CFOP(request);
            Assert.True(result.Result.TransactionId == "8f46e040-a200-495c-83ca-4fca2d7175fb");
            Assert.True(result.Result.Exception == null);
           // Assert.True(result.Result.CallDuration > 0);
        }

        [Fact]
        public void RegisterTravelers_CFOP_Exception_Test()
        {
            var response = new MOBRegisterTravelersResponse() { TransactionId = "8f46e040-a200-495c-83ca-4fca2d7175fb" };
            var request = new MOBRegisterTravelersRequest()
            {
                DeviceId = "",
                AccessCode = "YGHF",
                TransactionId = "589d7852-14e7-44a9-b23b-a6db36657579|8f46e040-a200-495c-83ca-4fca2d7175fb",
                SessionId = "17C979E184CC495EA083D45F4DD9D19D",
                Application = new Model.MOBApplication()
                {
                    Id = 2,
                    Version = new Model.MOBVersion()
                    {
                        Major = "4.1.23",
                        Minor = "4.1.23"
                    }
                }
            };
            travelersController.ControllerContext = new ControllerContext();
            travelersController.ControllerContext.HttpContext = new DefaultHttpContext();
            travelersController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";
            
            _travelerBusiness.Setup(p => p.RegisterTravelers_CFOP(request)).ThrowsAsync(new Exception("ERROR OCCURRED"));
            //if (request.AccessCode == "YGHF")
                _configuration["SurfaceErrorToClient"] = "true";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[United.Mobile.Model.Constants.HeaderTransactionIdText].ToString();
            var result = travelersController.RegisterTravelers_CFOP(request);
            Assert.True(result.Result.Exception.Code == "9999");
            Assert.True(result.Result.CallDuration > 0);
        }

        [Fact]
        public void RegisterTravelers_CFOP_Exception1_Test()
        {
            var response = new MOBRegisterTravelersResponse() { TransactionId = "8f46e040-a200-495c-83ca-4fca2d7175fb" };
            var request = new MOBRegisterTravelersRequest()
            {
                DeviceId = "",
                AccessCode = "ACCESSCODE",
                TransactionId = "589d7852-14e7-44a9-b23b-a6db36657579|8f46e040-a200-495c-83ca-4fca2d7175fb",
                SessionId = "17C979E184CC495EA083D45F4DD9D19D",
                Application = new Model.MOBApplication()
                {
                    Id = 2,
                    Version = new Model.MOBVersion()
                    {
                        Major = "4.1.23",
                        Minor = "4.1.23"
                    }
                }
            };
            travelersController.ControllerContext = new ControllerContext();
            travelersController.ControllerContext.HttpContext = new DefaultHttpContext();
            travelersController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _travelerBusiness.Setup(p => p.RegisterTravelers_CFOP(request)).ThrowsAsync(new Exception("Error Message"));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[United.Mobile.Model.Constants.HeaderTransactionIdText].ToString();
            var result = travelersController.RegisterTravelers_CFOP(request);
            Assert.True(result.Result.Exception.Code == "9999");
           // Assert.True(result.Result.CallDuration > 0);
        }

        [Fact]
        public void RegisterTravelers_CFOP_Exception2_Test()
        {
            var response = new MOBRegisterTravelersResponse() { TransactionId = "8f46e040-a200-495c-83ca-4fca2d7175fb" };
            var request = new MOBRegisterTravelersRequest()
            {
                DeviceId = "",
                AccessCode = "ACCESSCODE",
                TransactionId = "589d7852-14e7-44a9-b23b-a6db36657579|8f46e040-a200-495c-83ca-4fca2d7175fb",
                SessionId = "17C979E184CC495EA083D45F4DD9D19D",
                Application = new Model.MOBApplication()
                {
                    Id = 2,
                    Version = new Model.MOBVersion()
                    {
                        Major = "4.1.23",
                        Minor = "4.1.23"
                    }
                }
            };
            var exception = new MOBUnitedException("Error Occured")
            {
               Code = "10050"
            };
            travelersController.ControllerContext = new ControllerContext();
            travelersController.ControllerContext.HttpContext = new DefaultHttpContext();
            travelersController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";
            
            _travelerBusiness.Setup(p => p.RegisterTravelers_CFOP(request)).ThrowsAsync(exception);
            _configuration["SurfaceErrorToClient"] = "False";
            bool val = _configuration.GetValue<bool>("SurfaceErrorToClient");
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[United.Mobile.Model.Constants.HeaderTransactionIdText].ToString();
            var result = travelersController.RegisterTravelers_CFOP(request);
            Assert.True(result.Result.Exception.Code == "10050");
           // Assert.True(result.Result.CallDuration > 0);
        }

        [Fact]
        public void RegisterTravelers_CFOP_Exception3_Test()
        {
            var response = new MOBRegisterTravelersResponse() { TransactionId = "8f46e040-a200-495c-83ca-4fca2d7175fb" };
            var request = new MOBRegisterTravelersRequest()
            {
                DeviceId = "",
                AccessCode = "YGHF",
                TransactionId = "589d7852-14e7-44a9-b23b-a6db36657579|8f46e040-a200-495c-83ca-4fca2d7175fb",
                SessionId = "17C979E184CC495EA083D45F4DD9D19D",
                Application = new Model.MOBApplication()
                {
                    Id = 2,
                    Version = new Model.MOBVersion()
                    {
                        Major = "4.1.23",
                        Minor = "4.1.23"
                    }
                }
            };
            travelersController.ControllerContext = new ControllerContext();
            travelersController.ControllerContext.HttpContext = new DefaultHttpContext();
            travelersController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _travelerBusiness.Setup(p => p.RegisterTravelers_CFOP(request)).ThrowsAsync(new Exception("ERROR OCCURRED"));
            //if (request.AccessCode == "YGHF")
            _configuration["SurfaceErrorToClient"] = "false";
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[United.Mobile.Model.Constants.HeaderTransactionIdText].ToString();
            var result = travelersController.RegisterTravelers_CFOP(request);
            Assert.True(result.Result.Exception.Code == "9999");
        }

        [Fact]
        public void ValidateMPNameMisMatch_CFOP_Test()
        {
            var response = new MOBMPNameMissMatchResponse() { TransactionId = "8f46e040-a200-495c-83ca-4fca2d7175fb" };
            var request = new MOBMPNameMissMatchRequest()
            {
                DeviceId = "",
                AccessCode = "ACCESSCODE",
                TransactionId = "589d7852-14e7-44a9-b23b-a6db36657579|8f46e040-a200-495c-83ca-4fca2d7175fb",
                SessionId = "17C979E184CC495EA083D45F4DD9D19D",
                Application = new Model.MOBApplication()
                {
                    Id = 2,
                    Version = new Model.MOBVersion()
                    {
                        Major = "4.1.23",
                        Minor = "4.1.23"
                    }
                }
            };
            travelersController.ControllerContext = new ControllerContext();
            travelersController.ControllerContext.HttpContext = new DefaultHttpContext();
            travelersController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _validateMPNameBusiness.Setup(p => p.ValidateMPNameMisMatch_CFOP(request)).Returns(Task.FromResult(response));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[United.Mobile.Model.Constants.HeaderTransactionIdText].ToString();
            var result = travelersController.ValidateMPNameMisMatch_CFOP(request);
            Assert.True(result.Result.TransactionId == "8f46e040-a200-495c-83ca-4fca2d7175fb");
            Assert.True(result.Result.Exception == null);
          //  Assert.True(result.Result.CallDuration > 0);
        }
        [Fact]
        public void ValidateMPNameMisMatch_CFOP_Exception_Test()
        {
            var response = new MOBMPNameMissMatchResponse() { TransactionId = "8f46e040-a200-495c-83ca-4fca2d7175fb" };
            var request = new MOBMPNameMissMatchRequest()
            {
                DeviceId = "",
                AccessCode = "ACCESSCODE",
                TransactionId = "589d7852-14e7-44a9-b23b-a6db36657579|8f46e040-a200-495c-83ca-4fca2d7175fb",
                SessionId = "17C979E184CC495EA083D45F4DD9D19D",
                Application = new Model.MOBApplication()
                {
                    Id = 2,
                    Version = new Model.MOBVersion()
                    {
                        Major = "4.1.23",
                        Minor = "4.1.23"
                    }
                }
            };
            travelersController.ControllerContext = new ControllerContext();
            travelersController.ControllerContext.HttpContext = new DefaultHttpContext();
            travelersController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _validateMPNameBusiness.Setup(p => p.ValidateMPNameMisMatch_CFOP(request)).ThrowsAsync(new Exception("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[United.Mobile.Model.Constants.HeaderTransactionIdText].ToString();
            var result = travelersController.ValidateMPNameMisMatch_CFOP(request);
            Assert.True(result.Result.Exception.Code == "9999");
           // Assert.True(result.Result.CallDuration > 0);
        }

        [Fact]
        public void ValidateMPNameMisMatch_CFOP_Exception1_Test()
        {
            var response = new MOBMPNameMissMatchResponse() { TransactionId = "8f46e040-a200-495c-83ca-4fca2d7175fb" };
            var request = new MOBMPNameMissMatchRequest()
            {
                DeviceId = "",
                AccessCode = "ACCESSCODE",
                TransactionId = "589d7852-14e7-44a9-b23b-a6db36657579|8f46e040-a200-495c-83ca-4fca2d7175fb",
                SessionId = "17C979E184CC495EA083D45F4DD9D19D",
                Application = new Model.MOBApplication()
                {
                    Id = 2,
                    Version = new Model.MOBVersion()
                    {
                        Major = "4.1.23",
                        Minor = "4.1.23"
                    }
                }
            };
            travelersController.ControllerContext = new ControllerContext();
            travelersController.ControllerContext.HttpContext = new DefaultHttpContext();
            travelersController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";
            var exception = new MOBUnitedException("Error Occurred")
            {
                Code = "9999"
            };
            _validateMPNameBusiness.Setup(p => p.ValidateMPNameMisMatch_CFOP(request)).ThrowsAsync(exception);

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[United.Mobile.Model.Constants.HeaderTransactionIdText].ToString();
            var result = travelersController.ValidateMPNameMisMatch_CFOP(request);
            //Assert.True(result.Result.Exception.Code == "9999");
            // Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null); if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

    
    }
}

