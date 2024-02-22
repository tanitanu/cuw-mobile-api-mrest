using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using United.Common.Helper;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.TripPlannerService;
using United.Mobile.Services.TripPlannerService.Domain;
using United.Mobile.Model;
using United.Mobile.Model.TripPlannerService;
using Xunit;
using United.Mobile.Model.Common;
using Constants = United.Mobile.Model.Constants;
using Application = United.Mobile.Model.Application;
using United.TravelPlanner.Models;
using Newtonsoft.Json;
using United.Mobile.Model.TripPlannerGetService;
using United.Utility.Helper;

namespace United.Mobile.Test.TripPlannerService.Tests
{
  public class TripPlannerBusinessTest
    {
        private readonly Mock<ICacheLog<TripPlannerServiceBusiness>> _logger;
        private readonly TripPlannerServiceBusiness _tripPlannerServiceBusiness;
        private readonly Mock<IAddTripPlanVotingService> _addTripPlanVotingService;
      //  private readonly IAddTripPlanVotingService _addTripPlanVotingService;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly Mock<IHeaders> _headers;
        private  IConfiguration _configuration;
        private readonly Mock<HttpContext> _httpContext;
        private readonly HttpClient _httpClient;
        private readonly IAsyncPolicy _policyWrap;
        private readonly string _baseUrl;
        private readonly IDataPowerFactory _dataPowerFactory;
        private readonly Mock<ICacheLog<DataPowerFactory>> _logger9;


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

        public TripPlannerBusinessTest()
        {
            _logger = new Mock<ICacheLog<TripPlannerServiceBusiness>>();
            _logger9 = new Mock<ICacheLog<DataPowerFactory>>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _addTripPlanVotingService = new Mock<IAddTripPlanVotingService>() ;
            _httpContext = new Mock<HttpContext>();
            _dataPowerFactory = new DataPowerFactory(Configuration, _sessionHelperService.Object, _logger9.Object);



            _configuration = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
             .Build();


            _tripPlannerServiceBusiness = new TripPlannerServiceBusiness(_logger.Object, _configuration, _sessionHelperService.Object, _addTripPlanVotingService.Object);

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
        }

        private void SetHeaders(string deviceId = "D873298F-F27D-4AEC-BE6C-DE79C4259626"
                , string applicationId = "1"
                , string appVersion = "4.1.26"
                , string transactionId = "3f575588-bb12-41fe-8be7-f57c55fe7762|afc1db10-5c39-4ef4-9d35-df137d56a23e"
                , string languageCode = "en-US"
                , string sessionId = "D58E298C35274F6F873A133386A42916")
        {
            //Headers.ContextValues = new HttpContextValues
            //{
            //    Application = new Application()
            //    {
            //        Id = Convert.ToInt32(applicationId),
            //        Version = new Mobile.Model.Version
            //        {
            //            Major = string.IsNullOrEmpty(appVersion) ? 0 : int.Parse(appVersion.ToString().Substring(0, 1)),
            //            Minor = string.IsNullOrEmpty(appVersion) ? 0 : int.Parse(appVersion.ToString().Substring(2, 1)),
            //            Build = string.IsNullOrEmpty(appVersion) ? 0 : int.Parse(appVersion.ToString().Substring(4, 2))
            //        }
            //    },
            //    DeviceId = deviceId,
            //    LangCode = languageCode,
            //    TransactionId = transactionId,
            //    SessionId = sessionId
            //};
        }


        [Theory]
        [MemberData(nameof(TestDataGenerator.AddTripPlanVoting_Request), MemberType = typeof(TestDataGenerator))]
        public void AddTripPlanVoting_Request(MOBTripPlanVoteRequest request, Session session)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            TravelPlanRatingServiceResponse travelPlanRatingServiceResponse = new TravelPlanRatingServiceResponse()
            {
                Status = "Successful"
            };

            string jsonResponse = JsonConvert.SerializeObject(travelPlanRatingServiceResponse);

            _addTripPlanVotingService.Setup(p => p.TripPlanVoting(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(jsonResponse);

            //Act
            var result = _tripPlannerServiceBusiness.AddTripPlanVoting(request);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.DeleteTripPlan_Request), MemberType = typeof(TestDataGenerator))]
        public void DeleteTripPlan_Request(MOBTripPlanDeleteRequest request, Session session, TripPlanCCEResponse tripPlanCCEResponse)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _sessionHelperService.Setup(p => p.GetSession<TripPlanCCEResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(tripPlanCCEResponse);

            TravelPlanServiceResponse travelPlanServiceResponse = new TravelPlanServiceResponse()
            {
                Status = "Successful"
            };

            string jsonResponse = JsonConvert.SerializeObject(travelPlanServiceResponse);

            _addTripPlanVotingService.Setup(p => p.DeleteTripPlan(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(jsonResponse);

            //Act
            var result = _tripPlannerServiceBusiness.DeleteTripPlan(request);

            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);
        }


        [Theory]
        [MemberData(nameof(TestDataGenerator.DeleteTripPlanVoting_Request), MemberType = typeof(TestDataGenerator))]
        public void DeleteTripPlanVoting_Request(MOBTripPlanVoteRequest request, Session session)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

           // _sessionHelperService.Setup(p => p.GetSession<TripPlanCCEResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(tripPlanCCEResponse);

            TravelPlanRatingServiceResponse travelPlanRatingServiceResponse = new TravelPlanRatingServiceResponse()
            {
                Status = "Successful"
            };

            string jsonResponse = JsonConvert.SerializeObject(travelPlanRatingServiceResponse);

            _addTripPlanVotingService.Setup(p => p.DeleteTripPlan(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(jsonResponse);

            //Act
            var result = _tripPlannerServiceBusiness.DeleteTripPlanVoting(request);

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
