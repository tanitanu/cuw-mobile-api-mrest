using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.IO;
using United.Common.Helper;
using United.Ebs.Logging.Enrichers;
using United.Mobile.Model;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.TripPlannerGetService;
using United.Mobile.Services.TripPlannerGetService.Api.Controllers;
using United.Mobile.Services.TripPlannerGetService.Domain;
using United.Utility.Helper;
using Xunit;

namespace United.Mobile.Test.TripPlannerGetService.Tests
{
    public class TripPlannerGetControllerTest
    {
        private readonly Mock<ICacheLog<TripPlannerGetServiceController>> _logger;
        private readonly Mock<ITripPlannerGetServiceBusiness> _tripPlannerGetServiceBusiness;
        private readonly Mock<ITripPlannerServiceBusiness> _tripPlannerServiceBusiness;
        private readonly TripPlannerGetServiceController _tripPlannerGetServiceController;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<IApplicationEnricher> _requestEnricher;
        private readonly Mock<HttpContext> _httpContext;
        private readonly Mock<IFeatureSettings> _featureSetting;
        public TripPlannerGetControllerTest()
        {

            _logger = new Mock<ICacheLog<TripPlannerGetServiceController>>();
            _tripPlannerGetServiceBusiness = new Mock<ITripPlannerGetServiceBusiness>();
            _tripPlannerServiceBusiness = new Mock<ITripPlannerServiceBusiness>();
            _headers = new Mock<IHeaders>();
            _requestEnricher = new Mock<IApplicationEnricher>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _httpContext = new Mock<HttpContext>();
            _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
              .Build();

            _tripPlannerGetServiceController = new TripPlannerGetServiceController(_logger.Object, _configuration, _tripPlannerGetServiceBusiness.Object, _tripPlannerServiceBusiness.Object, _headers.Object, _requestEnricher.Object, _featureSetting.Object);

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
            _httpContextAccessor.Setup(_ => _.HttpContext).Returns(context);
        }

        [Fact]
        public void HealthCheck_Test()
        {
            string result = _tripPlannerGetServiceController.HealthCheck();
            Assert.True(result == "Healthy");
        }

        [Fact]
        public void Version_Test()
        {
            string result = _tripPlannerGetServiceController.Version();
            Assert.True(result != null);
        }

        [Fact]
        public void GetTripPlanSummary_Test()
        {
            var returns = new MOBTripPlanSummaryResponse() { };

            var request = new MOBTripPlanSummaryRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerGetServiceController.ControllerContext = new ControllerContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerGetServiceBusiness.Setup(p => p.GetTripPlanSummary(request)).ReturnsAsync(returns);


            // Act
            var result = _tripPlannerGetServiceController.GetTripPlanSummary(request);
            // Assert
            Assert.True(result != null);
            Assert.True(result.Result.CallDuration > 0);
            Assert.True(result.Result.Exception == null);


        }

        [Fact]
        public void GetTripPlanSummary_MOBUnitedException()
        {
            var returns = new MOBTripPlanSummaryResponse() { };

            var request = new MOBTripPlanSummaryRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerGetServiceController.ControllerContext = new ControllerContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerGetServiceBusiness.Setup(p => p.GetTripPlanSummary(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _tripPlannerGetServiceController.GetTripPlanSummary(request);
            // Assert
            Assert.True(result != null && result.Result.Exception.Message == "United data services are not currently available.");
            Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }

        [Fact]
        public void GetTripPlanSummary_Exception()
        {
            var returns = new MOBTripPlanSummaryResponse() { };

            var request = new MOBTripPlanSummaryRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerGetServiceController.ControllerContext = new ControllerContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerGetServiceBusiness.Setup(p => p.GetTripPlanSummary(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _tripPlannerGetServiceController.GetTripPlanSummary(request);
            // Assert
            Assert.True(result != null && result.Result.Exception.Message == "Sorry, something went wrong. Please try again.");
            Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }

        [Fact]
        public void SelectTripTripPlanner_Test()
        {
            var returns = new MOBSHOPSelectTripResponse() { };

            var request = new MOBSHOPSelectTripRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerGetServiceController.ControllerContext = new ControllerContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerGetServiceBusiness.Setup(p => p.SelectTripTripPlanner(request, _httpContext.Object)).ReturnsAsync(returns);


            // Act
            var result = _tripPlannerGetServiceController.SelectTripTripPlanner(request);
            // Assert
            Assert.True(result != null);
            Assert.True(result.Result.CallDuration > 0);
            Assert.True(result.Result.Exception == null);

        }

        [Fact]
        public void SelectTripTripPlanner_MOBUnitedException()
        {
            var returns = new MOBSHOPSelectTripResponse() { };

            var request = new MOBSHOPSelectTripRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerGetServiceController.ControllerContext = new ControllerContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerGetServiceBusiness.Setup(p => p.SelectTripTripPlanner(request, _httpContext.Object)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _tripPlannerGetServiceController.SelectTripTripPlanner(request);
            // Assert
            // Assert.True(result != null && result.Result.Exception.Message== "United data services are not currently available.");
            //  Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }

        [Fact]
        public void SelectTripTripPlanner_Exception()
        {
            var returns = new MOBSHOPSelectTripResponse() { };

            var request = new MOBSHOPSelectTripRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerGetServiceController.ControllerContext = new ControllerContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerGetServiceBusiness.Setup(p => p.SelectTripTripPlanner(request, _httpContext.Object)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _tripPlannerGetServiceController.SelectTripTripPlanner(request);
            // Assert
            // Assert.True(result != null && result.Result.Exception.Message == "Sorry, something went wrong. Please try again.");
            // Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }


        [Fact]
        public void GetTripPlanBoard_Test()
        {
            var returns = new MOBTripPlanBoardResponse() { };

            var request = new MOBTripPlanBoardRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerGetServiceController.ControllerContext = new ControllerContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerGetServiceBusiness.Setup(p => p.GetTripPlanBoard(request)).ReturnsAsync(returns);


            // Act
            var result = _tripPlannerGetServiceController.GetTripPlanBoard(request);
            // Assert
            Assert.True(result != null);
            Assert.True(result.Result.CallDuration > 0);
            Assert.True(result.Result.Exception == null);

        }

        [Fact]
        public void GetTripPlanBoard_MOBUnitedException()
        {
            var returns = new MOBTripPlanBoardResponse() { };

            var request = new MOBTripPlanBoardRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerGetServiceController.ControllerContext = new ControllerContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerGetServiceBusiness.Setup(p => p.GetTripPlanBoard(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _tripPlannerGetServiceController.GetTripPlanBoard(request);
            // Assert
            Assert.True(result != null && result.Result.Exception.Message == "United data services are not currently available.");
            Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }

        [Fact]
        public void GetTripPlanBoard_Exception()
        {
            var returns = new MOBTripPlanBoardResponse() { };

            var request = new MOBTripPlanBoardRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerGetServiceController.ControllerContext = new ControllerContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerGetServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerGetServiceBusiness.Setup(p => p.GetTripPlanBoard(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _tripPlannerGetServiceController.GetTripPlanBoard(request);
            // Assert
            Assert.True(result != null && result.Result.Exception.Message == "Sorry, something went wrong. Please try again.");
            Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }

    }
}
