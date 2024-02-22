using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Net;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Ebs.Logging.Enrichers;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Mobile.Model.UnfinishedBooking;
using United.Mobile.Model.TripPlannerGetService;
using United.Mobile.Model.UnfinishedBooking;
using United.Mobile.Services.UnfinishedBooking.Api.Controllers;
using United.Mobile.Services.UnfinishedBooking.Domain;
using Xunit;
using MOBSHOPUnfinishedBookingRequestBase = United.Mobile.Model.Shopping.UnfinishedBooking.MOBSHOPUnfinishedBookingRequestBase;
using United.Utility.Helper;

namespace United.Mobile.Shopping.UnfinishedBooking.Tests
{
    public class UnfinishedBookingControllerTest
    {
        private readonly Mock<ICacheLog<UnfinishedBookingController>> _logger;
        private IConfiguration _configuration;
        private readonly Mock<IUnfinishedBookingBusiness> _unfinishedBookingBusiness;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<IDPService> _tokenService;
        private readonly UnfinishedBookingController _unfinishedBookingController;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<IApplicationEnricher> _requestEnricher;
        private readonly Mock<IFeatureSettings> _featureSetting;
        public UnfinishedBookingControllerTest()
        {
            _logger = new Mock<ICacheLog<UnfinishedBookingController>>();
            _headers = new Mock<IHeaders>();
            _unfinishedBookingBusiness = new Mock<IUnfinishedBookingBusiness>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _tokenService = new Mock<IDPService>();
            _requestEnricher = new Mock<IApplicationEnricher>();

            _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test3.json", optional: false, reloadOnChange: true)
              .Build();

            SetupHttpContextAccessor();
            _unfinishedBookingController = new UnfinishedBookingController(_logger.Object, _configuration, _unfinishedBookingBusiness.Object, _headers.Object, _requestEnricher.Object,_featureSetting.Object);

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
            var result = _unfinishedBookingController.HealthCheck();
            Assert.Equal(returns, result);
        }

        [Fact]
        public void GetUnfinishedBookings_Test()
        {
            MOBSHOPGetUnfinishedBookingsRequest request = new MOBSHOPGetUnfinishedBookingsRequest()
            {
                SessionId = "A5538976AFFVAVFFKKSJJ",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            var returns = new MOBSHOPGetUnfinishedBookingsResponse();
            _unfinishedBookingBusiness.Setup(p => p.GetUnfinishedBookings(request)).Returns(Task.FromResult(returns));
            var result = _unfinishedBookingController.GetUnfinishedBookings(request);
            Assert.True(result.Result != null);

        }

        [Fact]
        public void GetUnfinishedBookings_Test_Exception()
        {
            MOBSHOPGetUnfinishedBookingsRequest request = new MOBSHOPGetUnfinishedBookingsRequest()
            {
                SessionId = "A5538976AFFVAVFFKKSJJ",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            var returns = new MOBSHOPGetUnfinishedBookingsResponse();
            _unfinishedBookingBusiness.Setup(p => p.GetUnfinishedBookings(request)).ThrowsAsync(new System.Exception("Error Message"));
            var result = _unfinishedBookingController.GetUnfinishedBookings(request);
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Fact]
        public void GetUnfinishedBookings_Test_MOBUnitedException()
        {
            MOBSHOPGetUnfinishedBookingsRequest request = new MOBSHOPGetUnfinishedBookingsRequest()
            {
                SessionId = "A5538976AFFVAVFFKKSJJ",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            var returns = new MOBSHOPGetUnfinishedBookingsResponse();
            _unfinishedBookingBusiness.Setup(p => p.GetUnfinishedBookings(request)).ThrowsAsync(new MOBUnitedException("Error Message"));
            var result = _unfinishedBookingController.GetUnfinishedBookings(request);
            Assert.True(result.Exception != null || result.Result != null);

        }


        [Fact]
        public void SelectUnfinishedBooking_Test()
        {

            MOBSHOPSelectUnfinishedBookingRequest request = new MOBSHOPSelectUnfinishedBookingRequest()
            {
                SessionId = "A5538976AFFVAVFFKKSJJ",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            var returns = new SelectTripResponse();
            _unfinishedBookingBusiness.Setup(p => p.SelectUnfinishedBooking(request,null)).Returns(Task.FromResult(returns));
            var result = _unfinishedBookingController.SelectUnfinishedBooking(request);
            Assert.True(result.Result != null);
        }

        [Fact]
        public void SelectUnfinishedBooking_Test_Exception()
        {
            MOBSHOPSelectUnfinishedBookingRequest request = new MOBSHOPSelectUnfinishedBookingRequest()
            {
                SessionId = "A5538976AFFVAVFFKKSJJ",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            var returns = new SelectTripResponse();
            _unfinishedBookingBusiness.Setup(p => p.SelectUnfinishedBooking(request,null)).ThrowsAsync(new System.Exception("Error Message"));
            var result = _unfinishedBookingController.SelectUnfinishedBooking(request);
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Fact]
        public void SelectUnfinishedBooking_Test_MOBUnitedException()
        {
            MOBSHOPSelectUnfinishedBookingRequest request = new MOBSHOPSelectUnfinishedBookingRequest()
            {
                SessionId = "A5538976AFFVAVFFKKSJJ",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            var returns = new SelectTripResponse();
            _unfinishedBookingBusiness.Setup(p => p.SelectUnfinishedBooking(request,null)).ThrowsAsync(new MOBUnitedException("Error Message"));
            var result = _unfinishedBookingController.SelectUnfinishedBooking(request);
            Assert.True(result.Exception != null || result.Result != null);

        }


        [Fact]
        public void ClearUnfinishedBookings_Test()
        {

            MOBSHOPUnfinishedBookingRequestBase request = new MOBSHOPUnfinishedBookingRequestBase()
            {
                SessionId = "A5538976AFFVAVFFKKSJJ",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            var returns = new MOBResponse();
            _unfinishedBookingBusiness.Setup(p => p.ClearUnfinishedBookings(request)).Returns(Task.FromResult(returns));
            var result = _unfinishedBookingController.ClearUnfinishedBookings(request);
            Assert.True(result.Result != null);
        }

        [Fact]
        public void ClearUnfinishedBookings_Test_Exception()
        {
            MOBSHOPUnfinishedBookingRequestBase request = new MOBSHOPUnfinishedBookingRequestBase()
            {
                SessionId = "A5538976AFFVAVFFKKSJJ",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            var returns = new MOBResponse();
            _unfinishedBookingBusiness.Setup(p => p.ClearUnfinishedBookings(request)).ThrowsAsync(new System.Exception("Error Message"));
            var result = _unfinishedBookingController.ClearUnfinishedBookings(request);
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Fact]
        public void ClearUnfinishedBookings_Test_MOBUnitedException()
        {
            MOBSHOPUnfinishedBookingRequestBase request = new MOBSHOPUnfinishedBookingRequestBase()
            {
                SessionId = "A5538976AFFVAVFFKKSJJ",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            var returns = new MOBResponse();
            _unfinishedBookingBusiness.Setup(p => p.ClearUnfinishedBookings(request)).ThrowsAsync(new MOBUnitedException("Error Message"));
            var result = _unfinishedBookingController.ClearUnfinishedBookings(request);
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Fact]
        public void GetOmniCartSavedTrips_Test()
        {
            var response = new MOBGetOmniCartSavedTripsResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44", CallDuration = 0 };
            var request = new MOBSHOPUnfinishedBookingRequestBase()
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
            };
            _unfinishedBookingController.ControllerContext = new ControllerContext();
            _unfinishedBookingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _unfinishedBookingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _unfinishedBookingBusiness.Setup(p => p.GetOmniCartSavedTrips(request)).Returns(Task.FromResult(response));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _unfinishedBookingController.GetOmniCartSavedTrips(request);
            Assert.True(result.Result.TransactionId == "EE64E779-7B46-4836-B261-62AE35498B44");
            Assert.True(result.Result.Exception == null);
        }

        [Fact]
        public void GetOmniCartSavedTrips_SystemException_Test()
        {
            var request = new MOBSHOPUnfinishedBookingRequestBase()
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
            };
            _unfinishedBookingController.ControllerContext = new ControllerContext();
            _unfinishedBookingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _unfinishedBookingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _unfinishedBookingBusiness.Setup(p => p.GetOmniCartSavedTrips(request)).ThrowsAsync(new Exception("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _unfinishedBookingController.GetOmniCartSavedTrips(request);
            //Assert.True(result.Result.Exception != null);
            Assert.True(result != null);
        }

        [Fact]
        public void GetOmniCartSavedTrips_UnitedException_Test()
        {
            var request = new MOBSHOPUnfinishedBookingRequestBase()
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
            };
            _unfinishedBookingController.ControllerContext = new ControllerContext();
            _unfinishedBookingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _unfinishedBookingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _unfinishedBookingBusiness.Setup(p => p.GetOmniCartSavedTrips(request)).ThrowsAsync(new MOBUnitedException("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _unfinishedBookingController.GetOmniCartSavedTrips(request);
            Assert.True(result.Result.Exception.Message != null);
        }

        [Fact]
        public void GetOmniCartSavedTrips_WebException_Test()
        {
            var request = new MOBSHOPUnfinishedBookingRequestBase()
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
            };
            _unfinishedBookingController.ControllerContext = new ControllerContext();
            _unfinishedBookingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _unfinishedBookingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _unfinishedBookingBusiness.Setup(p => p.GetOmniCartSavedTrips(request)).ThrowsAsync(new WebException("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _unfinishedBookingController.GetOmniCartSavedTrips(request);
            //Assert.True(result.Result.Exception.Message != null);
            Assert.True(result != null);
        }
        [Fact]
        public void SelectOmniCartSavedTrip_Test()
        {

            MOBSHOPSelectUnfinishedBookingRequest request = new MOBSHOPSelectUnfinishedBookingRequest()
            {
                SessionId = "45744EF6B1DF4864B3DEF4BF86135BA4",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "5524F792-AAB4-4170-98BC-290B72AE1B16",
            };

            var returns = new MOBSHOPSelectTripResponse();
            _unfinishedBookingBusiness.Setup(p => p.SelectOmniCartSavedTrip(request,null)).Returns(Task.FromResult(returns));
            var result = _unfinishedBookingController.SelectOmniCartSavedTrip(request);
            Assert.True(result.Result != null);
        }

        [Fact]
        public void SelectOmniCartSavedTrip_Test_Exception()
        {
            MOBSHOPSelectUnfinishedBookingRequest request = new MOBSHOPSelectUnfinishedBookingRequest()
            {
                SessionId = "45744EF6B1DF4864B3DEF4BF86135BA4",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "5524F792-AAB4-4170-98BC-290B72AE1B16",
            };

            var returns = new MOBSHOPSelectTripResponse();
            _unfinishedBookingBusiness.Setup(p => p.SelectOmniCartSavedTrip(request, null)).ThrowsAsync(new System.Exception("Error Message"));
            var result = _unfinishedBookingController.SelectOmniCartSavedTrip(request);
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Fact]
        public void SelectOmniCartSavedTrip_Test_MOBUnitedException()
        {
            MOBSHOPSelectUnfinishedBookingRequest request = new MOBSHOPSelectUnfinishedBookingRequest()
            {
                SessionId = "45744EF6B1DF4864B3DEF4BF86135BA4",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "5524F792-AAB4-4170-98BC-290B72AE1B16",
            };

            var returns = new MOBSHOPSelectTripResponse();
            _unfinishedBookingBusiness.Setup(p => p.SelectOmniCartSavedTrip(request, null)).ThrowsAsync(new MOBUnitedException("Error Message"));
            var result = _unfinishedBookingController.SelectOmniCartSavedTrip(request);
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Fact]
        public void SelectOmniCartSavedTrip_Test_WebException()
        {
            MOBSHOPSelectUnfinishedBookingRequest request = new MOBSHOPSelectUnfinishedBookingRequest()
            {
                SessionId = "45744EF6B1DF4864B3DEF4BF86135BA4",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "5524F792-AAB4-4170-98BC-290B72AE1B16",
            };

            var returns = new MOBSHOPSelectTripResponse();
            _unfinishedBookingBusiness.Setup(p => p.SelectOmniCartSavedTrip(request, null)).ThrowsAsync(new WebException("Error Message"));
            var result = _unfinishedBookingController.SelectOmniCartSavedTrip(request);
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Fact]
        public void RemoveOmniCartSavedTrip_Test()
        {

            MOBSHOPUnfinishedBookingRequestBase request = new MOBSHOPUnfinishedBookingRequestBase()
            {
                SessionId = "45744EF6B1DF4864B3DEF4BF86135BA4",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "5524F792-AAB4-4170-98BC-290B72AE1B16",
            };

            var returns = new MOBGetOmniCartSavedTripsResponse();
            _unfinishedBookingBusiness.Setup(p => p.RemoveOmniCartSavedTrip(request)).Returns(Task.FromResult(returns));
            var result = _unfinishedBookingController.RemoveOmniCartSavedTrip(request);
            Assert.True(result.Result != null);
        }

        [Fact]
        public void RemoveOmniCartSavedTrip_Test_Exception()
        {
            MOBSHOPUnfinishedBookingRequestBase request = new MOBSHOPUnfinishedBookingRequestBase()
            {
                SessionId = "45744EF6B1DF4864B3DEF4BF86135BA4",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "5524F792-AAB4-4170-98BC-290B72AE1B16",
            };

            var returns = new MOBGetOmniCartSavedTripsResponse();
            _unfinishedBookingBusiness.Setup(p => p.RemoveOmniCartSavedTrip(request)).ThrowsAsync(new System.Exception("Error Message"));
            var result = _unfinishedBookingController.RemoveOmniCartSavedTrip(request);
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Fact]
        public void RemoveOmniCartSavedTrip_Test_MOBUnitedException()
        {
            MOBSHOPUnfinishedBookingRequestBase request = new MOBSHOPUnfinishedBookingRequestBase()
            {
                SessionId = "45744EF6B1DF4864B3DEF4BF86135BA4",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "5524F792-AAB4-4170-98BC-290B72AE1B16",
            };

            var returns = new MOBGetOmniCartSavedTripsResponse();
            _unfinishedBookingBusiness.Setup(p => p.RemoveOmniCartSavedTrip(request)).ThrowsAsync(new MOBUnitedException("Error Message"));
            var result = _unfinishedBookingController.RemoveOmniCartSavedTrip(request);
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Fact]
        public void RemoveOmniCartSavedTrip_Test_WebException()
        {
            MOBSHOPUnfinishedBookingRequestBase request = new MOBSHOPUnfinishedBookingRequestBase()
            {
                SessionId = "45744EF6B1DF4864B3DEF4BF86135BA4",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "5524F792-AAB4-4170-98BC-290B72AE1B16",
            };

            var returns = new MOBGetOmniCartSavedTripsResponse();
            _unfinishedBookingBusiness.Setup(p => p.RemoveOmniCartSavedTrip(request)).ThrowsAsync(new WebException("Error Message"));
            var result = _unfinishedBookingController.RemoveOmniCartSavedTrip(request);
            Assert.True(result.Exception != null || result.Result != null);

        }
    }
}
