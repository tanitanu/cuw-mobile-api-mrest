using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Definition.SeatCSL30;
using United.Ebs.Logging.Enrichers;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.SeatEngine;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Common;
using United.Mobile.Model.SeatMapEngine;
using United.Mobile.SeatEngine.Domain;
using United.Services.FlightShopping.Common;
using United.Utility.Helper;
using Xunit;
using Session = United.Mobile.Model.Common.Session;

namespace United.Mobile.Test.SeatEngine.Api
{
    public class SeatEngineBusinessTests
    {
        private readonly Mock<ICacheLog<SeatEngineBusiness>> _logger;
        private readonly IConfiguration _configuration;
        private readonly Mock<IDPService> _tokenService;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        //private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<ISeatMapAvailabilityService> _seatMapAvailabilityService;
        private readonly Mock<IOnTimePerformanceInfoService> _onTimePerformanceInfoService;
        private readonly Mock<IApplicationEnricher> _applicationEnricher;
        private readonly SeatEngineBusiness _seatEngineBusiness;
        private readonly Mock<IComplimentaryUpgradeService >_complimentaryUpgradeOfferService;
        private readonly Mock<IShoppingUtility> _shoppingUtility;
        private readonly Mock<IFFCShoppingcs> _ffcShoppingcs;
        private readonly Mock<IFeatureSettings> _featureSettings;
        public SeatEngineBusinessTests()
        {
            _logger = new Mock<ICacheLog<SeatEngineBusiness>>();
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appSettings.test.json", optional: false, reloadOnChange: true)
                .Build();
            _tokenService = new Mock<IDPService>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _headers = new Mock<IHeaders>();
            _seatMapAvailabilityService = new Mock<ISeatMapAvailabilityService>();
            _onTimePerformanceInfoService = new Mock<IOnTimePerformanceInfoService>();
            _applicationEnricher = new Mock<IApplicationEnricher>();
            _complimentaryUpgradeOfferService = new Mock<IComplimentaryUpgradeService>();
            _shoppingUtility = new Mock<IShoppingUtility>();
            _ffcShoppingcs = new Mock<IFFCShoppingcs>();
            _featureSettings = new Mock<IFeatureSettings>();
            SetHeaders();
            _seatEngineBusiness = new SeatEngineBusiness(_logger.Object, _configuration, _tokenService.Object, _sessionHelperService.Object, _headers.Object, _seatMapAvailabilityService.Object, _onTimePerformanceInfoService.Object, _complimentaryUpgradeOfferService.Object, _shoppingUtility.Object
                , _ffcShoppingcs.Object, _featureSettings.Object);
            
        }
        private string GetFileContent(string fileName)
        {
            var path = Path.IsPathRooted(fileName) ? fileName
                                                : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }
        private void SetHeaders(string deviceId = "D873298F-F27D-4AEC-BE6C-DE79C4259626"
             , string applicationId = "2"
             , string appVersion = "4.1.26"
             , string transactionId = "3f575588-bb12-41fe-8be7-f57c55fe7762|afc1db10-5c39-4ef4-9d35-df137d56a23e"
             , string languageCode = "en-US"
             , string sessionId = "D58E298C35274F6F873A133386A42916")
        {
            _headers.Setup(_=> _.ContextValues).Returns(
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

        [Theory]
        [MemberData(nameof(SeatEngineInput.InputPreviewSeatMap), MemberType = typeof(SeatEngineInput))]
        public void PreviewSeatMap_tests(MOBSeatMapRequest request, SeatMap cslstrResponse, OnTimePerformanceInfoResponse data, List<CabinBrand> cabinBrands)
        {
            var session = GetFileContent("SessionData.json");
            var sessionData = JsonConvert.DeserializeObject<Session>(session);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(sessionData);
            _tokenService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Sample Token");
           if(!request.Application.IsProduction)
                _seatMapAvailabilityService.Setup(p => p.GetCSL30SeatMap(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(cslstrResponse));
            _onTimePerformanceInfoService.Setup(p => p.GetOnTimePerformance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(data));

            _complimentaryUpgradeOfferService.Setup(p => p.GetComplimentaryUpgradeOfferedFlagByCabinCount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(cabinBrands);

            if (request.FFlight.IsElf)
            {
                _configuration["Android_EnableInterlineLHRedirectLinkManageRes_AppVersion"] = "1";
                _configuration["AndroidOaSeatMapVersion"] = "2";
            }

            var result = _seatEngineBusiness.PreviewSeatMap(request);

            if (result.Exception == null)
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            else
                Assert.True(result.Exception.InnerExceptions != null && result.Exception.Message != null);
        }


        [Theory]
        [MemberData(nameof(TestDataGenerator.PreviewSeatMap_flow), MemberType = typeof(TestDataGenerator))]
        public void PreviewSeatMap_flow(MOBSeatMapRequest mOBSeatMapRequest, SeatMap cslstrResponse, OnTimePerformanceInfoResponse onTimePerformanceInfoResponse, List<CabinBrand> cabinBrands, Session session)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _tokenService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Sample Token");

            if (!mOBSeatMapRequest.Application.IsProduction)

                _seatMapAvailabilityService.Setup(p => p.GetCSL30SeatMap(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(cslstrResponse));

            _onTimePerformanceInfoService.Setup(p => p.GetOnTimePerformance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(onTimePerformanceInfoResponse));

            _complimentaryUpgradeOfferService.Setup(p => p.GetComplimentaryUpgradeOfferedFlagByCabinCount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(cabinBrands);

            if (mOBSeatMapRequest.FFlight.IsElf)
            {
                _configuration["Android_EnableInterlineLHRedirectLinkManageRes_AppVersion"] = "1";
                _configuration["AndroidOaSeatMapVersion"] = "2";
            }

            _shoppingUtility.Setup(p => p.EnableOAMessageUpdate(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            _shoppingUtility.Setup(p => p.IsLandTransport(It.IsAny<string>())).Returns(true);

            var result = _seatEngineBusiness.PreviewSeatMap(mOBSeatMapRequest);

            if (result.Exception == null)
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            else
                Assert.True(result.Exception.InnerExceptions != null && result.Exception.Message != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.PreviewSeatMap_floww), MemberType = typeof(TestDataGenerator))]
        public void PreviewSeatMap_floww(MOBSeatMapRequest mOBSeatMapRequest, SeatMap cslstrResponse, OnTimePerformanceInfoResponse onTimePerformanceInfoResponse, List<CabinBrand> cabinBrands, Session session)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _tokenService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Sample Token");

            if (!mOBSeatMapRequest.Application.IsProduction)

                _seatMapAvailabilityService.Setup(p => p.GetCSL30SeatMap(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(cslstrResponse));

            _onTimePerformanceInfoService.Setup(p => p.GetOnTimePerformance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(onTimePerformanceInfoResponse));

            _complimentaryUpgradeOfferService.Setup(p => p.GetComplimentaryUpgradeOfferedFlagByCabinCount(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(cabinBrands);

            if (mOBSeatMapRequest.FFlight.IsElf)
            {
                _configuration["Android_EnableInterlineLHRedirectLinkManageRes_AppVersion"] = "1";
                _configuration["AndroidOaSeatMapVersion"] = "2";
            }

            //_shoppingUtility.Setup(p => p.EnableOAMessageUpdate(It.IsAny<int>(), It.IsAny<string>())).Returns(true);

            //_shoppingUtility.Setup(p => p.IsLandTransport(It.IsAny<string>())).Returns(true);

            var result = _seatEngineBusiness.PreviewSeatMap(mOBSeatMapRequest);

            if (result.Exception == null)
                Assert.True(result.Result != null && result.Result.TransactionId != null);
            else
                Assert.True(result.Exception.InnerExceptions != null && result.Exception.Message != null);
        }

        [Theory]
        [MemberData(nameof(SeatEngineInput.InputGetSeatMap), MemberType = typeof(SeatEngineInput))]
        public void GetSeatMap_tests(OnTimePerformanceInfoResponse data,SeatMap cslstrResponse, int applicationid, string appVersion, string accessCode, string transactionId, string carrierCode, string flightNumber, string flightDate, string departureAirportCode, string arrivalAirportCode, string languageCode, string deviceId, string sessionId)
        {
            var session = GetFileContent("SessionData.json");
            var sessionData = JsonConvert.DeserializeObject<Session>(session);
            if(applicationid==2)
                _onTimePerformanceInfoService.Setup(p => p.GetOnTimePerformance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(data));
            _tokenService.Setup(p => p.GetAnonymousToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IConfiguration>())).ReturnsAsync("Bearer Sample Token");
            if(sessionId== "1")
                _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(sessionData);
            _seatMapAvailabilityService.Setup(p => p.GetCSL30SeatMap(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(JsonConvert.SerializeObject(cslstrResponse));
            if (sessionId == "")
                _configuration["EnableCSL30EresPreviewSeatMap"] = "false";
            if (cslstrResponse.FlightInfo.NoSeatSelectionWindow)
                _configuration["ShuffleVIPSBasedOnCSS_r_DPTOken"] = "True";
            bool val = _configuration.GetValue<bool>("ShuffleVIPSBasedOnCSS_r_DPTOken");
            if (cslstrResponse.FlightInfo.ArrivalAirport == "111")
                _configuration["andriodVersionWithNewDAASeatMap"] = "5";
            var result = _seatEngineBusiness.GetSeatMap(applicationid,appVersion,accessCode,transactionId,carrierCode,flightNumber,flightDate,departureAirportCode,arrivalAirportCode,languageCode,deviceId,sessionId);            
            if (result.Exception == null)
                Assert.True(result.Result != null && result.Result.CallDuration > 0 && result.Result.TransactionId != null);
            else
                Assert.True(result.Exception.InnerExceptions != null && result.Exception.Message != null);
        }
    }
}
