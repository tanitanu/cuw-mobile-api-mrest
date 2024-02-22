using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.FlightReservation.Api.Controllers;
using United.Mobile.FlightReservation.Domain;
using United.Mobile.Model;
using United.Mobile.Model.FlightReservation;
using United.Mobile.Model.Internal.Exception;
using United.Utility.Helper;
using Xunit;

namespace United.Mobile.Test.FlightReservation.Tests
{
    public class FlightReservationControllerTests
    {
        private readonly Mock<ICacheLog<FlightReservationController>> _logger;
        private readonly IConfiguration _configuration;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<IFlightReservationBusiness> _flightReservationBusiness;
        private readonly FlightReservationController _flightReservationController;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

        public FlightReservationControllerTests()
        {
            _logger = new Mock<ICacheLog<FlightReservationController>>();
            _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
            .Build();
            _headers = new Mock<IHeaders>();
            _flightReservationBusiness = new Mock<IFlightReservationBusiness>();
            _flightReservationController = new FlightReservationController(_logger.Object, _configuration, _headers.Object, _flightReservationBusiness.Object);
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            SetupHttpContextAccessor();
            SetHeaders();

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
            string result = _flightReservationController.HealthCheck();
            Assert.True(result == "Healthy");
        }

        [Fact]
        public void RequestReceiptByEmail_Test()
        {
            var response = new MOBReceiptByEmailResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };
            var request = new Request<MOBReceiptByEmailRequest>()
            {
                Data = new MOBReceiptByEmailRequest()
                {
                    Application = new MOBApplication()
                    {
                        Id = 1,
                        Version = new MOBVersion()
                        {
                            Major = "4.1.25",
                            Minor = "4.1.25"
                        }
                    },
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44"
                }
            };
            _flightReservationController.ControllerContext = new ControllerContext();
            _flightReservationController.ControllerContext.HttpContext = new DefaultHttpContext();
            _flightReservationController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _flightReservationBusiness.Setup(p => p.RequestReceiptByEmail(request.Data)).Returns(Task.FromResult(response));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _flightReservationController.RequestReceiptByEmail(request.Data);
            Assert.True(result.Result.TransactionId == "EE64E779-7B46-4836-B261-62AE35498B44");
            Assert.True(result.Result.Exception == null);
        }

        [Fact]
        public void RequestReceiptByEmail_SystemException_Test()
        {
            var request = new Request<MOBReceiptByEmailRequest>()
            {
                Data = new MOBReceiptByEmailRequest()
                {
                    Application = new MOBApplication()
                    {
                        Id = 1,
                        Version = new MOBVersion()
                        {
                            Major = "4.1.25",
                            Minor = "4.1.25"
                        }
                    }
                }
            };
            _flightReservationController.ControllerContext = new ControllerContext();
            _flightReservationController.ControllerContext.HttpContext = new DefaultHttpContext();
            _flightReservationController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _flightReservationBusiness.Setup(p => p.RequestReceiptByEmail(request.Data)).ThrowsAsync(new Exception("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _flightReservationController.RequestReceiptByEmail(request.Data);
            Assert.True(result.Result.Exception != null);
        }

        [Fact]
        public void RequestReceiptByEmail_MOBUnitedException_Test()
        {
            var request = new Request<MOBReceiptByEmailRequest>()
            {
                Data = new MOBReceiptByEmailRequest()
                {
                    Application = new MOBApplication()
                    {
                        Id = 1,
                        Version = new MOBVersion()
                        {
                            Major = "4.1.25",
                            Minor = "4.1.25"
                        }
                    },
                }
            };
            _flightReservationController.ControllerContext = new ControllerContext();
            _flightReservationController.ControllerContext.HttpContext = new DefaultHttpContext();
            _flightReservationController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _flightReservationBusiness.Setup(p => p.RequestReceiptByEmail(request.Data)).ThrowsAsync(new MOBUnitedException("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _flightReservationController.RequestReceiptByEmail(request.Data);
            Assert.True(result.Result.Exception.Message == "Error Message");
            Assert.True(result.Result.CallDuration > 0);
        }

        
        [Theory]
        [InlineData(12, "4.1.25", "ACCESSCODE", "DDB4705A-9374-46BD-928B-FCD7762B6CB9|7105D24E-B447-43ED-978A-D0944944C7AC", "DDB4705A-9374-46BD-928B-FCD7762B6CB9",
            "e3ba556edfaf7e73b1a44bf17a20832c026dc65e779495417c08ec67944ac69d", "en-US","false")]
        public void GetPNRsByMileagePlusNumber(int applicationId, string appVersion, string accessCode, string transactionId, string mileagePlusNumber, string pinCode, string reservationType, string languageCode, bool includeFarelockInfo = false)
        {
            var response = new MOBPNRByMileagePlusResponse();

            _flightReservationBusiness.Setup(p => p.GetPNRsByMileagePlusNumber(applicationId, appVersion, accessCode, transactionId, mileagePlusNumber, pinCode, reservationType, languageCode, includeFarelockInfo)).Returns(Task.FromResult(response));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationIdText].ToString();
            httpContext.Request.Headers[Constants.HeaderDeviceIdText].ToString();
            httpContext.Request.Headers[Constants.ApplicationVersionText].ToString();

            var result = _flightReservationController.GetPNRsByMileagePlusNumber(applicationId,appVersion,accessCode, transactionId, mileagePlusNumber, pinCode, reservationType, languageCode, includeFarelockInfo);

            Assert.True(result.Result != null);
        }


        [Fact]
        public void GetPNRsByMileagePlusNumber_Exception()
        {
            _flightReservationBusiness.Setup(p => p.GetPNRsByMileagePlusNumber(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ThrowsAsync(new Exception("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();

            var result = _flightReservationController.GetPNRsByMileagePlusNumber(12, "4.1.25", "ACCESSCODE", "DDB4705A-9374-46BD-928B-FCD7762B6CB9|7105D24E-B447-43ED-978A-D0944944C7AC", "DDB4705A-9374-46BD-928B-FCD7762B6CB9",
            "e3ba556edfaf7e73b1a44bf17a20832c026dc65e779495417c08ec67944ac69d", "en-US", "false");

            Assert.True(result != null);
        }
        [Fact]
        public void GetPNRsByMileagePlusNumber_MOBUnitedException()
        {
            _flightReservationBusiness.Setup(p => p.GetPNRsByMileagePlusNumber(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ThrowsAsync(new MOBUnitedException("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();

            var result = _flightReservationController.GetPNRsByMileagePlusNumber(12, "4.1.25", "ACCESSCODE", "DDB4705A-9374-46BD-928B-FCD7762B6CB9|7105D24E-B447-43ED-978A-D0944944C7AC", "DDB4705A-9374-46BD-928B-FCD7762B6CB9",
            "e3ba556edfaf7e73b1a44bf17a20832c026dc65e779495417c08ec67944ac69d", "en-US", "false");

            Assert.True(result != null);
        }
    }
}
