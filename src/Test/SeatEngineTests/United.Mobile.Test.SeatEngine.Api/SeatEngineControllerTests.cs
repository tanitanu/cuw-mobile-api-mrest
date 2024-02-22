using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Ebs.Logging.Enrichers;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.SeatMapEngine;
using United.Mobile.SeatEngine.Api.Controllers;
using United.Mobile.SeatEngine.Domain;
using United.Utility.Helper;
using Xunit;

namespace United.Mobile.Test.SeatEngine.Api
{
    public class SeatEngineControllerTests
    {
        private readonly Mock<ICacheLog<SeatEngineController>> _logger;
        private readonly Mock<ISeatEngineBusiness> _seatEngineBusiness;
        private readonly IConfiguration _configuration;
        private readonly Mock<IHeaders> _moqHeader;
        private readonly SeatEngineController _seatEngineController;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<IApplicationEnricher> _requestEnricher;
        private readonly Mock<IFeatureSettings> _featureSetting;
        public SeatEngineControllerTests()
        {
            _moqHeader = new Mock<IHeaders>();
            _logger = new Mock<ICacheLog<SeatEngineController>>();
            _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appSettings.test.json", optional: false, reloadOnChange: true)
            .Build();
            _logger = new Mock<ICacheLog<SeatEngineController>>();
            _seatEngineBusiness = new Mock<ISeatEngineBusiness>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _requestEnricher = new Mock<IApplicationEnricher>();
            SetHeaders();
            _seatEngineController = new SeatEngineController(_logger.Object,_configuration,_moqHeader.Object, _requestEnricher.Object, _seatEngineBusiness.Object,_featureSetting.Object);
          
        }
        private void SetHeaders(string deviceId = "D873298F-F27D-4AEC-BE6C-DE79C4259626"
             , string applicationId = "2"
             , string appVersion = "4.1.26"
             , string transactionId = "3f575588-bb12-41fe-8be7-f57c55fe7762|afc1db10-5c39-4ef4-9d35-df137d56a23e"
             , string languageCode = "en-US"
             , string sessionId = "D58E298C35274F6F873A133386A42916")
        {
            _moqHeader.Setup(_ => _.ContextValues).Returns(
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
            string result = _seatEngineController.HealthCheck();
            Assert.True(result == "Healthy");
        }
        [Fact]
        public void PreviewSeatMapTests()
        {
            var response = new MOBSeatMapResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };
            var request = new MOBSeatMapRequest()
            {
                Application = new MOBApplication()
                {
                    Id = 2,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.23"
                    }
                },
                DeviceId = "87d502f9-5b0b-4ed1-9533-a17b392ea3e0",
                TransactionId = "87d502f9-5b0b-4ed1-9533-a17b392ea3e0|ae2e5e64-5428-47dd-9a60-48f81f2d6324"
            };
            _seatEngineBusiness.Setup(p=>p.PreviewSeatMap(request)).Returns(Task.FromResult(response));
            var result = _seatEngineController.PreviewSeatMap(request);
            Assert.True(result.Result != null && result.Result.TransactionId == "EE64E779-7B46-4836-B261-62AE35498B44");
            Assert.True(result.Result.CallDuration > 0);
            Assert.True(result.Result.Exception == null);
        }
        [Fact]
        public void PreviewSeatMapTests_Exception()
        {
            var response = new MOBSeatMapResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };
            var request = new MOBSeatMapRequest()
            {
                Application = new MOBApplication()
                {
                    Id = 2,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.23"
                    }
                },
                DeviceId = "87d502f9-5b0b-4ed1-9533-a17b392ea3e0",
                TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44"
            };
            _seatEngineBusiness.Setup(p => p.PreviewSeatMap(request)).ThrowsAsync(new System.Exception("Advance seat assignments are not available through United for this flight. Please see an airport agent during check-in to receive your seat assignment"));
            var result = _seatEngineController.PreviewSeatMap(request);
            Assert.True(result.Result != null && result.Result.Exception.Code == "9999");
            Assert.True(result.Result.Exception.Message.Equals("Advance seat assignments are not available through United for this flight. Please see an airport agent during check-in to receive your seat assignment"));
            Assert.True(result.Result.Exception.ErrMessage != null && result.Result.Exception.Message != null);
            Assert.True(result.Result.CallDuration > 0);
        }
        [Fact]
        public void PreviewSeatMapTests_Exception1()
        {
            var response = new MOBSeatMapResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };
            var request = new MOBSeatMapRequest()
            {
                Application = new MOBApplication()
                {
                    Id = 2,
                    Version = new MOBVersion()
                    {
                        Major = "4.1.23"
                    }
                },
                DeviceId = "87d502f9-5b0b-4ed1-9533-a17b392ea3e0",
                TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44"
            };
            _seatEngineBusiness.Setup(p => p.PreviewSeatMap(request)).ThrowsAsync(new MOBUnitedException("Advance seat assignments are not available through United for this flight. Please see an airport agent during check-in to receive your seat assignment"));
            var result = _seatEngineController.PreviewSeatMap(request);
            Assert.True(result.Result != null && result.Result.Exception.Code != null);
            Assert.True(result.Result != null && result.Result.Exception.Message.Equals("Advance seat assignments are not available through United for this flight. Please see an airport agent during check-in to receive your seat assignment"));
            Assert.True(result.Result.Exception.ErrMessage != null && result.Result.Exception.Message != null);
            Assert.True(result.Result.CallDuration > 0);
        }
        
        [Theory]
        [InlineData(2,"4.1.23","ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44","","","","","","", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        [InlineData(2,"4.1.23","ACCESSCODE", "","","","","","","","", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        [InlineData(2,"","ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44","","","","","","","", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        [InlineData(2,"4.1.23","", "EE64E779-7B46-4836-B261-62AE35498B44","","","","","","","", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        [InlineData(2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "", "", "", "", "", "", "", "")]
        [InlineData(2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "", "", "", "", "", "", "", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        public void GetSeatMapTests(int applicationid, string appVersion, string accessCode, string transactionId, string carrierCode, string flightNumber, string flightDate, string departureAirportCode, string arrivalAirportCode, string languageCode, string deviceId, string sessionId)
        {
            var response = new MOBSeatMapResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };
           
            _seatEngineBusiness.Setup(p => p.GetSeatMap(applicationid,appVersion,accessCode,transactionId,carrierCode,flightNumber,flightDate,departureAirportCode,arrivalAirportCode,languageCode,deviceId,sessionId)).Returns(Task.FromResult(response));
            var result = _seatEngineController.GetSeatMap(applicationid,appVersion,accessCode,transactionId,carrierCode,flightNumber,flightDate,departureAirportCode,arrivalAirportCode,languageCode,deviceId,sessionId);
            Assert.True(result.Result != null && result.Result.TransactionId == "EE64E779-7B46-4836-B261-62AE35498B44");
            Assert.True(result.Result.CallDuration > 0);
            Assert.True(result.Result.Exception == null);
        }

        [Theory]
        [InlineData(2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "", "", "", "", "", "", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        [InlineData(2, "4.1.23", "ACCESSCODE", "", "", "", "", "", "", "", "", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        [InlineData(2, "", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "", "", "", "", "", "", "", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        [InlineData(2, "4.1.23", "", "EE64E779-7B46-4836-B261-62AE35498B44", "", "", "", "", "", "", "", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        [InlineData(2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "", "", "", "", "", "", "", "")]
        [InlineData(2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "", "", "", "", "", "", "", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        public void GetSeatMapTests_Exception(int applicationid, string appVersion, string accessCode, string transactionId, string carrierCode, string flightNumber, string flightDate, string departureAirportCode, string arrivalAirportCode, string languageCode, string deviceId, string sessionId)
        {
            var response = new MOBSeatMapResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };

            _seatEngineBusiness.Setup(p => p.GetSeatMap(applicationid, appVersion, accessCode, transactionId, carrierCode, flightNumber, flightDate, departureAirportCode, arrivalAirportCode, languageCode, deviceId, sessionId)).ThrowsAsync(new System.Exception("Advance seat assignments are not available through United for this flight. Please see an airport agent during check-in to receive your seat assignment"));
            var result = _seatEngineController.GetSeatMap(applicationid, appVersion, accessCode, transactionId, carrierCode, flightNumber, flightDate, departureAirportCode, arrivalAirportCode, languageCode, deviceId, sessionId);
            Assert.True(result.Result != null && result.Result.Exception.Code == "9999");
            Assert.True(result.Result != null && result.Result.Exception.Message.Equals("Advance seat assignments are not available through United for this flight. Please see an airport agent during check-in to receive your seat assignment"));
            Assert.True(result.Result.Exception.ErrMessage != null && result.Result.Exception.Message != null);
            Assert.True(result.Result.CallDuration > 0);
        }

        [Theory]
        [InlineData(2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "", "", "", "", "", "", "D873298F-F27D-4AEC-BE6C-DE79C4259626", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        [InlineData(2, "4.1.23", "ACCESSCODE", "", "", "", "", "", "", "", "", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        [InlineData(2, "", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "", "", "", "", "", "", "", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        [InlineData(2, "4.1.23", "", "EE64E779-7B46-4836-B261-62AE35498B44", "", "", "", "", "", "", "", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        [InlineData(2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "", "", "", "", "", "", "", "")]
        [InlineData(2, "4.1.23", "ACCESSCODE", "EE64E779-7B46-4836-B261-62AE35498B44", "", "", "", "", "", "", "", "CD9ED989 - 7BD7 - 44E9 - B4C4 - 1B5A1ACDEF15")]
        public void GetSeatMapTests_Exception1(int applicationid, string appVersion, string accessCode, string transactionId, string carrierCode, string flightNumber, string flightDate, string departureAirportCode, string arrivalAirportCode, string languageCode, string deviceId, string sessionId)
        {
            var response = new MOBSeatMapResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };

            _seatEngineBusiness.Setup(p => p.GetSeatMap(applicationid, appVersion, accessCode, transactionId, carrierCode, flightNumber, flightDate, departureAirportCode, arrivalAirportCode, languageCode, deviceId, sessionId)).ThrowsAsync(new MOBUnitedException("Advance seat assignments are not available through United for this flight. Please see an airport agent during check-in to receive your seat assignment"));
            var result = _seatEngineController.GetSeatMap(applicationid, appVersion, accessCode, transactionId, carrierCode, flightNumber, flightDate, departureAirportCode, arrivalAirportCode, languageCode, deviceId, sessionId);
            Assert.True(result.Result != null && result.Result.Exception.Code != null);
            Assert.True(result.Result != null && result.Result.Exception.Message.Equals("Advance seat assignments are not available through United for this flight. Please see an airport agent during check-in to receive your seat assignment"));
            Assert.True(result.Result.Exception.ErrMessage != null && result.Result.Exception.Message != null);
            Assert.True(result.Result.CallDuration > 0);
        }
    }
}
