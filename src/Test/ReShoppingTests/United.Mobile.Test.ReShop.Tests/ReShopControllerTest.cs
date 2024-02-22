using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.EligibleCheck.Domain;
using United.Mobile.Model;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ReShop;
using United.Mobile.Model.Shopping;
using United.Mobile.ReShop.Api.Controllers;
using United.Mobile.ReShop.Domain;
using United.Mobile.ReShop.Domain.GetProducts_CFOP;
using United.Mobile.ReshopSelectTrip.Domain;
using United.Mobile.ScheduleChange.Domain;
using United.Mobile.Services.Shopping.Domain;
using United.Mobile.UpdateProfile.Domain;
using United.Utility.Helper;
using Xunit;
using Constants = United.Mobile.Model.Constants;

namespace United.Mobile.Test.ReShop.Tests
{
    public class ReShopControllerTest
    {
        private readonly Mock<ICacheLog<ReShopController>> _logger;
        private readonly IConfiguration _configuration;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<IShoppingBusiness> _shoppingBusiness;
        private readonly Mock<IReShoppingBusiness> _reShoppingBusiness;
        private readonly Mock<IEligibleCheckBusiness> _eligibleCheckBusiness;
        private readonly Mock<IReshopSelectTripBusiness> _reshopSelectTripBusiness;
        private readonly Mock<IScheduleChangeBusiness> _scheduleChangeBusiness;
        private readonly Mock<IUpdateProfileBusiness> _updateProfileBusiness;
        private readonly ReShopController _reShopController;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly HttpContext _httpContext;
        private readonly Mock<IGetProducts_CFOPBusiness> _getProducts_CFOPBusiness;
        private readonly Mock<IFeatureSettings> _featureSetting;
        public ReShopControllerTest()
        {
            _logger = new Mock<ICacheLog<ReShopController>>();
            _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
            .Build();
            _headers = new Mock<IHeaders>();
            _shoppingBusiness = new Mock<IShoppingBusiness>();
            _reShoppingBusiness = new Mock<IReShoppingBusiness>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _eligibleCheckBusiness = new Mock<IEligibleCheckBusiness>();
            _getProducts_CFOPBusiness = new Mock<IGetProducts_CFOPBusiness>();
            _reshopSelectTripBusiness = new Mock<IReshopSelectTripBusiness>();
            _scheduleChangeBusiness = new Mock<IScheduleChangeBusiness>();
            _updateProfileBusiness = new Mock<IUpdateProfileBusiness>();
            _reShopController = new ReShopController(_logger.Object, _configuration, _headers.Object,  _reShoppingBusiness.Object, _eligibleCheckBusiness.Object , _reshopSelectTripBusiness.Object , _scheduleChangeBusiness.Object,_updateProfileBusiness.Object, 
              _getProducts_CFOPBusiness.Object,_featureSetting.Object);
            _getProducts_CFOPBusiness = new Mock<IGetProducts_CFOPBusiness>();
            _reShopController = new ReShopController(_logger.Object, _configuration, _headers.Object,  _reShoppingBusiness.Object, _eligibleCheckBusiness.Object , _reshopSelectTripBusiness.Object , _scheduleChangeBusiness.Object,_updateProfileBusiness.Object, _getProducts_CFOPBusiness.Object,_featureSetting.Object);
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
            string result = _reShopController.HealthCheck();
            Assert.True(result == "Healthy");
        }


        [Fact]
        public void Reshop_Test()
        {
            var response = new ShopResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44", CallDuration =0 };
            var request = new MOBSHOPShopRequest()
            {
                AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
                LanguageCode = "abcd",
                DeviceId = "abc",
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

            _reShoppingBusiness.Setup(p =>p.ReShop(request, _httpContext)).Returns(Task.FromResult(response));

            var result = _reShopController.Reshop(request);
            
            Assert.True(result.Result.TransactionId == "EE64E779-7B46-4836-B261-62AE35498B44");
            Assert.True(result.Result.Exception == null);
            //Assert.True(result.Result.CallDuration > 0);

        }
        [Fact]
        public void Reshop_SystemException_Test()
        {
            var request = new Request<MOBSHOPShopRequest>()
            {
                Data = new MOBSHOPShopRequest()
                {
                    AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
                    LanguageCode = "abcd",
                    DeviceId = "abc",
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
            _reShopController.ControllerContext = new ControllerContext();
            _reShopController.ControllerContext.HttpContext = new DefaultHttpContext();
            _reShopController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _reShoppingBusiness.Setup(p => p.ReShop(request.Data, _httpContext)).ThrowsAsync(new Exception("Error Message"));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _reShopController.Reshop(request.Data);
            Assert.True(result.Result.Exception.Code == "9999");
        }


        [Fact]
        public void Reshop_UnitedException_Test()
        {
            var request = new Request<MOBSHOPShopRequest>()
            {
                Data = new MOBSHOPShopRequest()
                {
                    AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
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
            _reShopController.ControllerContext = new ControllerContext();
            _reShopController.ControllerContext.HttpContext = new DefaultHttpContext();
            _reShopController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            // _shoppingBusiness.Setup(p => p.GetShop(request.Data,_httpContext)).ThrowsAsync(new MOBUnitedException("Error Message"));

            _reShoppingBusiness.Setup(p => p.ReShop(request.Data, _httpContext)).ThrowsAsync(new Exception("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _reShopController.Reshop(request.Data);
            Assert.False(result.Result.Exception.Message == "Error Message");
            //Assert.True(result.Result.CallDuration > 0);
        }
        [Fact]
        public void ChangeEligibleCheck_Test()
        {
            var response = new MOBRESHOPChangeEligibilityResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };
            var request = new MOBRESHOPChangeEligibilityRequest()
            {
                AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
                LanguageCode = "abcd",
                DeviceId = "abc",
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
           
            _eligibleCheckBusiness.Setup(p =>p.ChangeEligibleCheck(request)).Returns(Task.FromResult(response));


            var result = _reShopController.ChangeEligibleCheck(request);
            Assert.True(result.Result.TransactionId == "EE64E779-7B46-4836-B261-62AE35498B44");
            Assert.True(result.Result.Exception == null);
           // Assert.True(result.Result.CallDuration > 0);
        }
        [Fact]
        public void ChangeEligibleCheck_SystemException_Test()
        {
            var request = new Request<MOBRESHOPChangeEligibilityRequest>()
            {
                Data = new MOBRESHOPChangeEligibilityRequest()
                {
                    AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
                    LanguageCode = "abcd",
                    DeviceId = "abc",
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
            _reShopController.ControllerContext = new ControllerContext();
            _reShopController.ControllerContext.HttpContext = new DefaultHttpContext();
            _reShopController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _eligibleCheckBusiness.Setup(p => p.ChangeEligibleCheck(request.Data)).ThrowsAsync(new Exception("Error Message"));
            

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _reShopController.ChangeEligibleCheck(request.Data);
            Assert.True(result.Result.Exception.Code == "9999");
        }
        [Fact]
        public void ChangeEligibleCheck_UnitedException_Test()
        {
            var request = new Request<MOBRESHOPChangeEligibilityRequest>()
            {
                Data = new MOBRESHOPChangeEligibilityRequest()
                {
                    AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
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
            _reShopController.ControllerContext = new ControllerContext();
            _reShopController.ControllerContext.HttpContext = new DefaultHttpContext();
            _reShopController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            // _shoppingBusiness.Setup(p => p.GetShop(request.Data, _httpContext)).ThrowsAsync(new MOBUnitedException("Error Message"));
            _eligibleCheckBusiness.Setup(p => p.ChangeEligibleCheck(request.Data)).ThrowsAsync(new Exception("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _reShopController.ChangeEligibleCheck(request.Data);
            Assert.False(result.Result.Exception.Message == "Error Message");
            //Assert.True(result.Result.CallDuration > 0);
        }

        [Fact]
        public void ChangeEligibleCheckAndReshop_Test()
        {
            var response = new MOBRESHOPChangeEligibilityResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };
            var request = new MOBRESHOPChangeEligibilityRequest()
            {
                AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
                LanguageCode = "abcd",
                DeviceId = "abc",
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

            _eligibleCheckBusiness.Setup(p => p.ChangeEligibleCheckAndReshop(request)).Returns(Task.FromResult(response));


            var result = _reShopController.ChangeEligibleCheckAndReshop(request);
            Assert.True(result.Result.TransactionId == "EE64E779-7B46-4836-B261-62AE35498B44");
            Assert.True(result.Result.Exception == null);
            // Assert.True(result.Result.CallDuration > 0);
        }
        [Fact]
        public void ChangeEligibleCheckAndReshop_SystemException_Test()
        {
            var request = new Request<MOBRESHOPChangeEligibilityRequest>()
            {
                Data = new MOBRESHOPChangeEligibilityRequest()
                {
                    AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
                    LanguageCode = "abcd",
                    DeviceId = "abc",
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
            _reShopController.ControllerContext = new ControllerContext();
            _reShopController.ControllerContext.HttpContext = new DefaultHttpContext();
            _reShopController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _eligibleCheckBusiness.Setup(p => p.ChangeEligibleCheckAndReshop(request.Data)).ThrowsAsync(new Exception("Error Message"));


            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _reShopController.ChangeEligibleCheckAndReshop(request.Data);
            Assert.True(result.Result.Exception.Code == "9999");
        }
        [Fact]
        public void ChangeEligibleCheckAndReshop_UnitedException_Test()
        {
            var request = new Request<MOBRESHOPChangeEligibilityRequest>()
            {
                Data = new MOBRESHOPChangeEligibilityRequest()
                {
                    AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
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
            _reShopController.ControllerContext = new ControllerContext();
            _reShopController.ControllerContext.HttpContext = new DefaultHttpContext();
            _reShopController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            // _shoppingBusiness.Setup(p => p.GetShop(request.Data, _httpContext)).ThrowsAsync(new MOBUnitedException("Error Message"));
            _eligibleCheckBusiness.Setup(p => p.ChangeEligibleCheckAndReshop(request.Data)).ThrowsAsync(new Exception("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _reShopController.ChangeEligibleCheckAndReshop(request.Data);
            Assert.False(result.Result.Exception.Message == "Error Message");
            //Assert.True(result.Result.CallDuration > 0);
        }

        [Fact]
        public void SelectTrip_Test()
        {
            var response = new SelectTripResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };
            var request = new SelectTripRequest()
            {
                AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
                LanguageCode = "abcd",
                DeviceId = "abc",
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

            _reshopSelectTripBusiness.Setup(p => p.SelectTrip(request, _httpContext)).Returns(Task.FromResult(response));


            var result = _reShopController.ReshopSelectTrip(request);
            Assert.True(result != null);
        }
        [Fact]
        public void SelectTrip_SystemException_Test()
        {
            var request = new Request<SelectTripRequest>()
            {
                Data = new SelectTripRequest()
                {
                    AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
                    LanguageCode = "abcd",
                    DeviceId = "abc",
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
            _reShopController.ControllerContext = new ControllerContext();
            _reShopController.ControllerContext.HttpContext = new DefaultHttpContext();
            _reShopController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _reshopSelectTripBusiness.Setup(p => p.SelectTrip(request.Data, _httpContext)).ThrowsAsync(new Exception("Error Message"));


            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _reShopController.ReshopSelectTrip(request.Data);
            Assert.True(result != null);
        }
        [Fact]
        public void SelectTrip_UnitedException_Test()
        {
            var request = new Request<SelectTripRequest>()
            {
                Data = new SelectTripRequest()
                {
                    AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
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
            _reShopController.ControllerContext = new ControllerContext();
            _reShopController.ControllerContext.HttpContext = new DefaultHttpContext();
            _reShopController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            // _shoppingBusiness.Setup(p => p.GetShop(request.Data, _httpContext)).ThrowsAsync(new MOBUnitedException("Error Message"));
            _reshopSelectTripBusiness.Setup(p => p.SelectTrip(request.Data, _httpContext)).ThrowsAsync(new Exception("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _reShopController.ReshopSelectTrip(request.Data);
            Assert.True(result != null);
        }

        [Fact]
        public void ReshopSaveEmail_CFOP_Test()
        {
            var response = new MOBChangeEmailResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };
            var request = new MOBChangeEmailRequest()
            {
                AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
                LanguageCode = "abcd",
                DeviceId = "abc",
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

            _updateProfileBusiness.Setup(p => p.ReshopSaveEmail_CFOP(request)).Returns(Task.FromResult(response));


            var result = _reShopController.ReshopSaveEmail_CFOP(request);
            Assert.True(result.Result.TransactionId == "EE64E779-7B46-4836-B261-62AE35498B44");
            Assert.True(result.Result.Exception == null);
            // Assert.True(result.Result.CallDuration > 0);
        }
        [Fact]
        public void ReshopSaveEmail_CFOP_SystemException_Test()
        {
            var request = new Request<MOBChangeEmailRequest>()
            {
                Data = new MOBChangeEmailRequest()
                {
                    AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
                    LanguageCode = "abcd",
                    DeviceId = "abc",
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
            _reShopController.ControllerContext = new ControllerContext();
            _reShopController.ControllerContext.HttpContext = new DefaultHttpContext();
            _reShopController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _updateProfileBusiness.Setup(p => p.ReshopSaveEmail_CFOP(request.Data)).ThrowsAsync(new Exception("Error Message"));


            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _reShopController.ReshopSaveEmail_CFOP(request.Data);
            Assert.True(result.Result.Exception.Code == "9999");
        }
        [Fact]
        public void ReshopSaveEmail_CFOP_UnitedException_Test()
        {
            var request = new Request<MOBChangeEmailRequest>()
            {
                Data = new MOBChangeEmailRequest()
                {
                    AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
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
            _reShopController.ControllerContext = new ControllerContext();
            _reShopController.ControllerContext.HttpContext = new DefaultHttpContext();
            _reShopController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            // _shoppingBusiness.Setup(p => p.GetShop(request.Data, _httpContext)).ThrowsAsync(new MOBUnitedException("Error Message"));
            _updateProfileBusiness.Setup(p => p.ReshopSaveEmail_CFOP(request.Data)).ThrowsAsync(new Exception("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _reShopController.ReshopSaveEmail_CFOP(request.Data);
            Assert.False(result.Result.Exception.Message == "Error Message");
            //Assert.True(result.Result.CallDuration > 0);
        }

        [Fact]
        public void GetProducts_CFOP_Test()
        {
            var response = new MOBSHOPProductSearchResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44", CallDuration = 0 };
            var request = new MOBSHOPProductSearchRequest()
            {
                AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
                LanguageCode = "abcd",
                DeviceId = "abc",
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

            _getProducts_CFOPBusiness.Setup(p => p.GetProducts_CFOP(request)).Returns(Task.FromResult(response));

            var result = _reShopController.GetProducts_CFOP(request);

            Assert.True(result.Result.TransactionId == "EE64E779-7B46-4836-B261-62AE35498B44");
            Assert.True(result.Result.Exception == null);
            //Assert.True(result.Result.CallDuration > 0);

        }

        [Fact]
        public void GetProducts_CFOP_SystemException_Test()
        {
            var request = new Request<MOBSHOPProductSearchRequest>()
            {
                Data = new MOBSHOPProductSearchRequest()
                {
                    AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
                    LanguageCode = "abcd",
                    DeviceId = "abc",
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
            _reShopController.ControllerContext = new ControllerContext();
            _reShopController.ControllerContext.HttpContext = new DefaultHttpContext();
            _reShopController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _getProducts_CFOPBusiness.Setup(p => p.GetProducts_CFOP(request.Data)).ThrowsAsync(new Exception("Error Message"));
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _reShopController.GetProducts_CFOP(request.Data);
            Assert.True(result.Result.Exception.Code == "9999");
        }
        [Fact]
        public void GetProducts_CFOP_UnitedException_Test()
        {
            var request = new Request<MOBSHOPProductSearchRequest>()
            {
                Data = new MOBSHOPProductSearchRequest()
                {
                    AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
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
            _reShopController.ControllerContext = new ControllerContext();
            _reShopController.ControllerContext.HttpContext = new DefaultHttpContext();
            _reShopController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            // _shoppingBusiness.Setup(p => p.GetShop(request.Data,_httpContext)).ThrowsAsync(new MOBUnitedException("Error Message"));

            _getProducts_CFOPBusiness.Setup(p => p.GetProducts_CFOP(request.Data)).ThrowsAsync(new Exception("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _reShopController.GetProducts_CFOP(request.Data);
            Assert.False(result.Result.Exception.Message == "Error Message");
            //Assert.True(result.Result.CallDuration > 0);
        }

        [Fact]
        public void ConfirmScheduleChange_Test()
        {
            var response = new MOBConfirmScheduleChangeResponse() { TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44" };
            var request = new MOBConfirmScheduleChangeRequest()
            {
                AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
                LanguageCode = "abcd",
                DeviceId = "abc",
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

           _scheduleChangeBusiness .Setup(p => p.ConfirmScheduleChange(request)).Returns(Task.FromResult(response));


            var result = _reShopController.ConfirmScheduleChange(request);
            Assert.True(result.Result.TransactionId == "EE64E779-7B46-4836-B261-62AE35498B44");
            Assert.True(result.Result.Exception == null);
            // Assert.True(result.Result.CallDuration > 0);
        }
        [Fact]
        public void ConfirmScheduleChange_SystemException_Test()
        {
            var request = new Request<MOBConfirmScheduleChangeRequest>()
            {
                Data = new MOBConfirmScheduleChangeRequest()
                {
                    AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
                    LanguageCode = "abcd",
                    DeviceId = "abc",
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
            _reShopController.ControllerContext = new ControllerContext();
            _reShopController.ControllerContext.HttpContext = new DefaultHttpContext();
            _reShopController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _scheduleChangeBusiness.Setup(p => p.ConfirmScheduleChange(request.Data)).ThrowsAsync(new Exception("Error Message"));


            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _reShopController.ConfirmScheduleChange(request.Data);
            Assert.True(result.Result.Exception.Code == "9999");
        }
        [Fact]
        public void ConfirmScheduleChange_UnitedException_Test()
        {
            var request = new Request<MOBConfirmScheduleChangeRequest>()
            {
                Data = new MOBConfirmScheduleChangeRequest()
                {
                    AccessCode = "0B4D8C69883C46EFB69177D68387BA73",
                    TransactionId = "EE64E779-7B46-4836-B261-62AE35498B44",
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
            _reShopController.ControllerContext = new ControllerContext();
            _reShopController.ControllerContext.HttpContext = new DefaultHttpContext();
            _reShopController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            // _shoppingBusiness.Setup(p => p.GetShop(request.Data, _httpContext)).ThrowsAsync(new MOBUnitedException("Error Message"));
            _scheduleChangeBusiness.Setup(p => p.ConfirmScheduleChange(request.Data)).ThrowsAsync(new Exception("Error Message"));

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[Constants.HeaderTransactionIdText].ToString();
            var result = _reShopController.ConfirmScheduleChange(request.Data);
            Assert.False(result.Result.Exception.Message == "Error Message");
            //Assert.True(result.Result.CallDuration > 0);
        }
    }
}

