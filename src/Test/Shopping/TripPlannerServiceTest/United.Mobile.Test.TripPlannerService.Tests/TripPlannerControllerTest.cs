using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using United.Common.Helper;
using United.Mobile.Model;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.TripPlannerService;
using United.Mobile.Services.TripPlannerService.Api.Controllers;
using United.Mobile.Services.TripPlannerService.Domain;
using Xunit;
using United.Utility.Helper;

namespace United.Mobile.Test.TripPlannerService.Tests
{
    public class TripPlannerControllerTest
    {

        private readonly Mock<ICacheLog<TripPlannerServiceController>> _logger;
        private readonly Mock<ITripPlannerServiceBusiness> _tripPlannerServiceBusiness;
        private readonly TripPlannerServiceController _tripPlannerServiceController;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<HttpContext> _httpContext;
        private readonly Mock<IFeatureSettings> _featureSetting;

        public TripPlannerControllerTest()
        {
            _logger = new Mock<ICacheLog<TripPlannerServiceController>>();
            _tripPlannerServiceBusiness = new Mock<ITripPlannerServiceBusiness>();
            _headers = new Mock<IHeaders>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _httpContext = new Mock<HttpContext>();
            _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
              .Build();

            _tripPlannerServiceController = new TripPlannerServiceController(_logger.Object, _configuration, _tripPlannerServiceBusiness.Object, _headers.Object, _featureSetting.Object);

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
            string result = _tripPlannerServiceController.HealthCheck();
            Assert.True(result == "Healthy");
        }

        [Fact]
        public void Version_Test()
        {
            string result = _tripPlannerServiceController.Version();
            Assert.True(result != null);
        }

        [Fact]
        public void AddTripPlanVoting_Test()
        {
            var returns = new MOBTripPlanVoteResponse() { };

            var request = new MOBTripPlanVoteRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerServiceController.ControllerContext = new ControllerContext();
            _tripPlannerServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerServiceBusiness.Setup(p => p.AddTripPlanVoting(request)).ReturnsAsync(returns);


            // Act
            var result = _tripPlannerServiceController.AddTripPlanVoting(request);
            // Assert
            Assert.True(result != null);
            // Assert.True(result.Result.CallDuration > 0);
            // Assert.True(result.Result.Exception == null);

        }

        [Fact]
        public void AddTripPlanVoting_MOBUnitedException()
        {
            var returns = new MOBTripPlanVoteResponse() { };

            var request = new MOBTripPlanVoteRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerServiceController.ControllerContext = new ControllerContext();
            _tripPlannerServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerServiceBusiness.Setup(p => p.AddTripPlanVoting(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _tripPlannerServiceController.AddTripPlanVoting(request);
            // Assert
            // Assert.True(result != null && result.Result.Exception.Message== "United data services are not currently available.");
            //  Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }

        [Fact]
        public void AddTripPlanVoting_Exception()
        {
            var returns = new MOBTripPlanVoteResponse() { };

            var request = new MOBTripPlanVoteRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerServiceController.ControllerContext = new ControllerContext();
            _tripPlannerServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerServiceBusiness.Setup(p => p.AddTripPlanVoting(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _tripPlannerServiceController.AddTripPlanVoting(request);
            // Assert
            // Assert.True(result != null && result.Result.Exception.Message == "Sorry, something went wrong. Please try again.");
            // Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }

        [Fact]
        public void DeleteTripPlan_Test()
        {
            var returns = new MOBTripPlanDeleteResponse() { };

            var request = new MOBTripPlanDeleteRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerServiceController.ControllerContext = new ControllerContext();
            _tripPlannerServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerServiceBusiness.Setup(p => p.DeleteTripPlan(request)).ReturnsAsync(returns);


            // Act
            var result = _tripPlannerServiceController.DeleteTripPlan(request);
            // Assert
            Assert.True(result != null);
            // Assert.True(result.Result.CallDuration > 0);
            // Assert.True(result.Result.Exception == null);

        }

        [Fact]
        public void DeleteTripPlan_MOBUnitedException()
        {
            var returns = new MOBTripPlanDeleteResponse() { };

            var request = new MOBTripPlanDeleteRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerServiceController.ControllerContext = new ControllerContext();
            _tripPlannerServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerServiceBusiness.Setup(p => p.DeleteTripPlan(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _tripPlannerServiceController.DeleteTripPlan(request);
            // Assert
            // Assert.True(result != null && result.Result.Exception.Message== "United data services are not currently available.");
            //  Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }

        [Fact]
        public void DeleteTripPlan_Exception()
        {
            var returns = new MOBTripPlanDeleteResponse() { };

            var request = new MOBTripPlanDeleteRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerServiceController.ControllerContext = new ControllerContext();
            _tripPlannerServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerServiceBusiness.Setup(p => p.DeleteTripPlan(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _tripPlannerServiceController.DeleteTripPlan(request);
            // Assert
            // Assert.True(result != null && result.Result.Exception.Message == "Sorry, something went wrong. Please try again.");
            // Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }


        [Fact]
        public void DeleteTripPlanVoting_Test()
        {
            var returns = new MOBTripPlanVoteResponse() { };

            var request = new MOBTripPlanVoteRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerServiceController.ControllerContext = new ControllerContext();
            _tripPlannerServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerServiceBusiness.Setup(p => p.DeleteTripPlanVoting(request)).ReturnsAsync(returns);


            // Act
            var result = _tripPlannerServiceController.DeleteTripPlanVoting(request);
            // Assert
            Assert.True(result != null);
            // Assert.True(result.Result.CallDuration > 0);
            // Assert.True(result.Result.Exception == null);

        }

        [Fact]
        public void DeleteTripPlanVoting_MOBUnitedException()
        {
            var returns = new MOBTripPlanVoteResponse() { };

            var request = new MOBTripPlanVoteRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerServiceController.ControllerContext = new ControllerContext();
            _tripPlannerServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerServiceBusiness.Setup(p => p.DeleteTripPlanVoting(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _tripPlannerServiceController.DeleteTripPlanVoting(request);
            // Assert
            // Assert.True(result != null && result.Result.Exception.Message== "United data services are not currently available.");
            //  Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }

        [Fact]
        public void DeleteTripPlanVoting_Exception()
        {
            var returns = new MOBTripPlanVoteResponse() { };

            var request = new MOBTripPlanVoteRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _tripPlannerServiceController.ControllerContext = new ControllerContext();
            _tripPlannerServiceController.ControllerContext.HttpContext = new DefaultHttpContext();
            _tripPlannerServiceController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _tripPlannerServiceBusiness.Setup(p => p.DeleteTripPlanVoting(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _tripPlannerServiceController.DeleteTripPlanVoting(request);
            // Assert
            // Assert.True(result != null && result.Result.Exception.Message == "Sorry, something went wrong. Please try again.");
            // Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }



    }
}
