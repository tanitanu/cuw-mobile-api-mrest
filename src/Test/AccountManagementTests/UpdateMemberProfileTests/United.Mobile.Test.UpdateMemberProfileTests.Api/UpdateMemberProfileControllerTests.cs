using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.UpdateMemberProfile;
using United.Mobile.UpdateMemberProfile.Api.Controllers;
using United.Mobile.UpdateMemberProfile.Domain;
using Xunit;
using Constants = United.Mobile.Model.Constants;
using United.Utility.Helper;

namespace United.Mobile.Test.UpdateMemberProfileTests.Api
{
    public class UpdateMemberProfileControllerTests

    {
        private readonly Mock<ICacheLog<UpdateMemberProfileController>> _logger;
        private readonly Mock<IUpdateMemberProfileBusiness> _updateMemberProfileBusiness;
        private readonly UpdateMemberProfileController _updateMemberProfileController;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly IConfiguration _configuration;
        private readonly Mock<IHeaders> _moqHeader;
        private readonly Mock<IFeatureSettings> _featureSettings;
        public UpdateMemberProfileControllerTests()
        {
            _moqHeader = new Mock<IHeaders>();
            _logger = new Mock<ICacheLog<UpdateMemberProfileController>>();
            _updateMemberProfileBusiness = new Mock<IUpdateMemberProfileBusiness>();
            _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appSettings.test.json", optional: false, reloadOnChange: true)
            .Build();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _updateMemberProfileController = new UpdateMemberProfileController(_logger.Object, _configuration, _updateMemberProfileBusiness.Object, _moqHeader.Object,_featureSettings.Object);
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
        public string HealthCheck_Test()
        {
            return _updateMemberProfileController.HealthCheck();
        }

        [Fact]
        public void UpdateProfileOwner_Test()
        {
            var response = new MOBCustomerProfileResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };
            var request = new MOBUpdateCustomerFOPRequest()
            {
                SessionId = "0B4D8C69883C46EFB69177D68387BA73",
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
            _updateMemberProfileController.ControllerContext = new ControllerContext();
            _updateMemberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _updateMemberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _updateMemberProfileBusiness.Setup(p => p.UpdateProfileOwner(request)).Returns(Task.FromResult(response));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _updateMemberProfileController.UpdateProfileOwner(request);
            Assert.True(result.Result.TransactionId == "EE64E779-7B46-4836-B261-62AE35498B44");
            Assert.True(result.Result.Exception == null);
            Assert.True(result.Result.CallDuration > 0);
        }
        [Fact]
        public void UpdateProfileOwner_MOBUnitedException_Test()
        {
            var request = new MOBUpdateCustomerFOPRequest()
            {

                SessionId = "0B4D8C69883C46EFB69177D68387BA73",
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
            _updateMemberProfileController.ControllerContext = new ControllerContext();
            _updateMemberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _updateMemberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _updateMemberProfileBusiness.Setup(p => p.UpdateProfileOwner(request)).ThrowsAsync(new MOBUnitedException("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _updateMemberProfileController.UpdateProfileOwner(request);
            Assert.True(result.Result.Exception.Message == "Error Message");
            Assert.True(result.Result.CallDuration > 0);
        }
        [Fact]
        public void UpdateProfileOwner_SystemException_Test()
        {
            var request = new MOBUpdateCustomerFOPRequest()
            {
                SessionId = "0B4D8C69883C46EFB69177D68387BA73",
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
            _updateMemberProfileController.ControllerContext = new ControllerContext();
            _updateMemberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _updateMemberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _updateMemberProfileBusiness.Setup(p => p.UpdateProfileOwner(request)).ThrowsAsync(new Exception("Sorry, something went wrong. Please try again."));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _updateMemberProfileController.UpdateProfileOwner(request);
            Assert.True(result.Result.Exception.Message == "Sorry, something went wrong. Please try again.");
            Assert.True(result.Result.CallDuration > 0);
        }
        [Fact]
        public void UpdateProfileOwnerCardInfo_Test()
        {
            var response = new MOBUpdateProfileOwnerFOPResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };
            var request = new MOBUpdateProfileOwnerFOPRequest()
            {
                SessionId = "0B4D8C69883C46EFB69177D68387BA73",
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
            _updateMemberProfileController.ControllerContext = new ControllerContext();
            _updateMemberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _updateMemberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _updateMemberProfileBusiness.Setup(p => p.UpdateProfileOwnerCardInfo(request)).Returns(Task.FromResult(response));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _updateMemberProfileController.UpdateProfileOwnerCardInfo(request);
            Assert.True(result.Result.Exception == null);
        }
        [Fact]
        public void UpdateProfileOwnerCardInfo_SystemException_Test()
        {
            var request = new MOBUpdateProfileOwnerFOPRequest()
            {
                SessionId = "0B4D8C69883C46EFB69177D68387BA73",
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
            _updateMemberProfileController.ControllerContext = new ControllerContext();
            _updateMemberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _updateMemberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _updateMemberProfileBusiness.Setup(p => p.UpdateProfileOwnerCardInfo(request)).ThrowsAsync(new Exception("Sorry, something went wrong. Please try again."));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _updateMemberProfileController.UpdateProfileOwnerCardInfo(request);
            Assert.True(result.Result.Exception.Message == "Sorry, something went wrong. Please try again.");
        }
        [Fact]
        public void UpdateProfileOwnerCardInfo_MOBUnitedException_Test()
        {
            var request = new MOBUpdateProfileOwnerFOPRequest()
            {
                SessionId = "0B4D8C69883C46EFB69177D68387BA73",
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
            _updateMemberProfileController.ControllerContext = new ControllerContext();
            _updateMemberProfileController.ControllerContext.HttpContext = new DefaultHttpContext();
            _updateMemberProfileController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _updateMemberProfileBusiness.Setup(p => p.UpdateProfileOwnerCardInfo(request)).ThrowsAsync(new MOBUnitedException("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _updateMemberProfileController.UpdateProfileOwnerCardInfo(request);
            Assert.True(result.Result.Exception.Message == "Error Message");
        }
    }
}
