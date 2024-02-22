using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Ebs.Logging.Enrichers;
using United.Mobile.Model;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Common;
using United.Mobile.Services.Shopping.Api.Controllers;
using United.Mobile.Services.Shopping.Domain;
using United.Utility.Helper;
using Xunit;

namespace United.Mobile.Shopping.Tests
{
    public class ShoppingDetailsControllerTest 
    {
        private readonly Mock<ICacheLog<ShoppingController>> _logger;
        private readonly Mock<IShoppingBusiness> _shoppingBusiness;
        private readonly Mock<IShopProductsBusiness> _shopProductsBusiness;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly ShoppingController _ShoppingController;
        private readonly IConfiguration _configuration;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<IApplicationEnricher> _requestEnricher;
        private readonly Mock<HttpContext> _httpContext;
        private readonly Mock<IFeatureSettings> _featureSetting;
        private readonly Mock<IShopMileagePricingBusiness> _shopMileagePricingBusiness;
        public ShoppingDetailsControllerTest()
        {
           
            _logger = new Mock<ICacheLog<ShoppingController>>();
            _shoppingBusiness = new Mock<IShoppingBusiness>();
            _shopProductsBusiness = new Mock<IShopProductsBusiness>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _headers = new Mock<IHeaders>();
            _requestEnricher = new Mock<IApplicationEnricher>();
            _httpContext = new Mock<HttpContext>();
            _shopMileagePricingBusiness = new Mock<IShopMileagePricingBusiness>();
            _configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
              .Build();
            _ShoppingController = new ShoppingController(_logger.Object, _configuration, _shoppingBusiness.Object, _shopProductsBusiness.Object, _headers.Object, _requestEnricher.Object, _featureSetting.Object, _shopMileagePricingBusiness.Object);
         //   _shopproductsController = new ShopProductsController(_logger.Object, Configuration1, _shopproductsBusiness.Object, _headers.Object, _requestEnricher.Object);
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
            string result = _ShoppingController.HealthCheck();
            Assert.True(result == "Healthy");
        }


        [Fact]
        public void Shop_Test()
        {

            var returns = new ShopResponse()
            {

                CallDuration = 23456789012556721,
                CartId = "375D3212-9BFF-42F7-94A6-42342E2E24D2"

            };

            var request = new MOBSHOPShopRequest()
            {
                SessionId = "67945321097C4CF58FFC7DF9565CB276",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.GetShop(request,_httpContext.Object)).ReturnsAsync(returns);


            // Act
            var result = _ShoppingController.Shop(request);
            // Assert
            Assert.True(result != null);
            //Assert.True(result.Result.CallDuration > 0);
            // Assert.True(result.Result.Exception == null);

        }

        [Fact]
        public void Shop_MOBUnitedException()
        {
            var returns = new ShopResponse() {};

            var request = new MOBSHOPShopRequest()
            {
                SessionId = "67945321097C4CF58FFC7DF9565CB276",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.GetShop(request,_httpContext.Object)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _ShoppingController.Shop(request);
            // Assert
           // Assert.True(result != null && result.Result.Exception.Message== "United data services are not currently available.");
            //  Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }

        [Fact]
        public void Shop_Exception()
        {
            var returns = new ShopResponse() { };

            var request = new MOBSHOPShopRequest()
            {
                SessionId = "67945321097C4CF58FFC7DF9565CB276",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.GetShop(request,_httpContext.Object)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _ShoppingController.Shop(request);
            // Assert
            // Assert.True(result != null && result.Result.Exception.Message == "Sorry, something went wrong. Please try again.");
             Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }


        [Fact]
        public void ShopCLBOptOut_Test()
        {
            var returns = new ShopResponse() { TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c" };

            var request = new CLBOptOutRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.ShopCLBOptOut(request)).ReturnsAsync(returns);


            // Act
            var result = _ShoppingController.ShopCLBOptOut(request);
            // Assert
            Assert.True(result != null && result.Result.TransactionId == "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c");
           // Assert.True(result.Result.CallDuration > 0);
            Assert.True(result.Result.Exception == null);

        }

        [Fact]
        public void ShopCLBOptOut_MOBUnitedException()
        {
            var returns = new ShopResponse() { };

            var request = new CLBOptOutRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.ShopCLBOptOut(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _ShoppingController.ShopCLBOptOut(request);
            // Assert
            Assert.True(result != null);
            //Assert.True(result.Result.CallDuration > 0);

        }

        [Fact]
        public void ShopCLBOptOut_Exception()
        {
            var returns = new ShopResponse() { };

            var request = new CLBOptOutRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.ShopCLBOptOut(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _ShoppingController.ShopCLBOptOut(request);
            // Assert
            Assert.True(result != null);

        }

        [Fact]
        public void OrganizeShopResults_Test()
        {
            var returns = new ShopOrganizeResultsResponse() { TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c" };

            var request = new ShopOrganizeResultsReqeust()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.OrganizeShopResults(request)).ReturnsAsync(returns);


            // Act
            var result = _ShoppingController.OrganizeShopResults(request);
            // Assert
            //Assert.True(result != null && result.Result.TransactionId == "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c");
           // Assert.True(result.Result.CallDuration > 0);
            Assert.True(result.Result.Exception == null);

        }

        [Fact]
        public void OrganizeShopResults_MOBUnitedException()
        {
            var returns = new ShopOrganizeResultsResponse() { };

            var request = new ShopOrganizeResultsReqeust()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.OrganizeShopResults(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _ShoppingController.OrganizeShopResults(request);
            // Assert
            //Assert.True(result != null && result.Result.Exception.Message == "United data services are not currently available.");
            //  Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);
            if (result?.Exception == null)
            {
                Assert.True(result.Result != null && result.Result.TransactionId != null);
                Assert.True(result.Result.LanguageCode != null);
            }
            else
                Assert.True(result.Exception != null && result.Exception.InnerException != null);

        }

        [Fact]
        public void OrganizeShopResults_Exception()
        {
            var returns = new ShopOrganizeResultsResponse() { };

            var request = new ShopOrganizeResultsReqeust()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.OrganizeShopResults(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _ShoppingController.OrganizeShopResults(request);
            // Assert
            Assert.True(result != null && result.Result.Exception.Message == "Sorry, something went wrong. Please try again.");
          //  Assert.True(result.Result.CallDuration > 0);

        }

        [Fact]
        public void GetShopRequest_Test()
        {
            var returns = new TripShareV2Response() { TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c" };

            var request = new ShareTripRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.GetShopRequest(request)).ReturnsAsync(returns);


            // Act
            var result = _ShoppingController.GetShopRequest(request);
            // Assert
            Assert.True(result != null && result.Result.TransactionId == "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c");
           // Assert.True(result.Result.CallDuration > 0);
            Assert.True(result.Result.Exception == null);

        }

        [Fact]
        public void GetShopRequest_MOBUnitedException()
        {
            var returns = new TripShareV2Response() { };

            var request = new ShareTripRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.GetShopRequest(request)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _ShoppingController.GetShopRequest(request);
            // Assert
            Assert.True(result != null && result.Result.Exception.Message == "United data services are not currently available.");
           // Assert.True(result.Result.CallDuration > 0);

        }

        [Fact]
        public void GetShopRequest_Exception()
        {
            var returns = new TripShareV2Response() { };

            var request = new ShareTripRequest()
            {
                SessionId = "S",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.GetShopRequest(request)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _ShoppingController.GetShopRequest(request);
            // Assert
            Assert.True(result != null && result.Result.Exception.Message == "Sorry, something went wrong. Please try again.");
           // Assert.True(result.Result.CallDuration > 0);

        }

        [Fact]
        public void SelectTrip_Test()
        {
            var returns = new SelectTripResponse();
            var selectTripRequest = new SelectTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoppingBusiness.Setup(p => p.SelectTrip(selectTripRequest,null)).Returns(Task.FromResult(returns));
            //Act
            var result = _ShoppingController.SelectTrip(selectTripRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Fact]
        public void SelectTrip_Test_Exception()
        {
            var returns = new SelectTripResponse();
            var selectTripRequest = new SelectTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shoppingBusiness.Setup(p => p.SelectTrip(selectTripRequest,null)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _ShoppingController.SelectTrip(selectTripRequest);
            // Assert
            Assert.True(result.Result != null);
            Assert.True(result.Result.Exception.Message.Equals(_configuration.GetValue<string>("Booking2OGenericExceptionMessage")));

        }

        [Fact]
        public void SelectTrip_Test_MOBUnitedException()
        {
            var returns = new SelectTripResponse();
            var selectTripRequest = new SelectTripRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };
            

            _shoppingBusiness.Setup(p => p.SelectTrip(selectTripRequest,null)).ThrowsAsync(new MOBUnitedException("Error Message"));
            // Act
            var result = _ShoppingController.SelectTrip(selectTripRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Fact]
        public void ChasePromoRTIRedirect_Test()
        {
            var returns = new ChasePromoRedirectResponse();
            var chasePromoRedirectRequest = new ChasePromoRedirectRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };
            
            _shopProductsBusiness.Setup(p => p.ChasePromoRTIRedirect(chasePromoRedirectRequest)).Returns(Task.FromResult(returns));
            //Act
            var result = _ShoppingController.ChasePromoRTIRedirect(chasePromoRedirectRequest);
            //Assert
            Assert.True(result.Result != null);
        }

        [Fact]
        public void ChasePromoRTIRedirect_Test_Exception()
        {
            var returns = new ChasePromoRedirectResponse();
            var chasePromoRedirectRequest = new ChasePromoRedirectRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",

            };
            
            _shopProductsBusiness.Setup(p => p.ChasePromoRTIRedirect(chasePromoRedirectRequest)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _ShoppingController.ChasePromoRTIRedirect(chasePromoRedirectRequest);
            // Assert
            Assert.True(result.Result != null);
           Assert.True(result.Result.Exception.Message.Equals(_configuration.GetValue<string>("Booking2OGenericExceptionMessage")));
        }

        [Fact]
        public void ChasePromoRTIRedirect_Test_Exception1()
        {
          //todo 
         // var _shopproductsController = new ShopProductsController(_logger.Object, Configuration1, _shopproductsBusiness.Object, _headers.Object, _requestEnricher.Object);

            var returns = new ChasePromoRedirectResponse();
            var chasePromoRedirectRequest = new ChasePromoRedirectRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",

            };
            _shopProductsBusiness.Setup(p => p.ChasePromoRTIRedirect(chasePromoRedirectRequest)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _ShoppingController.ChasePromoRTIRedirect(chasePromoRedirectRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Fact]
        public void ChasePromoRTIRedirect_Test_MOBUnitedException()
        {
            var returns = new ChasePromoRedirectResponse();
            var chasePromoRedirectRequest = new ChasePromoRedirectRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };
            
            _shopProductsBusiness.Setup(p => p.ChasePromoRTIRedirect(chasePromoRedirectRequest)).ThrowsAsync(new MOBUnitedException("Error Message"));
            // Act
            var result = _ShoppingController.ChasePromoRTIRedirect(chasePromoRedirectRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Fact]
        public void GetProductInfoForFSRD_Test()
        {
            var returns = new GetProductInfoForFSRDResponse();
            var getProductInfoForFSRDRequest = new GetProductInfoForFSRDRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };
            
            _shopProductsBusiness.Setup(p => p.GetProductInfoForFSRD(getProductInfoForFSRDRequest)).Returns(Task.FromResult(returns));
            //Act
            var result = _ShoppingController.GetProductInfoForFSRD(getProductInfoForFSRDRequest);
            //Assert
            Assert.True(result.Result != null);
        }

        [Fact]
        public void GetProductInfoForFSRD_Test_Exception()
        {
            var returns = new GetProductInfoForFSRDResponse();
            var getProductInfoForFSRDRequest = new GetProductInfoForFSRDRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };
            
            _shopProductsBusiness.Setup(p => p.GetProductInfoForFSRD(getProductInfoForFSRDRequest)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _ShoppingController.GetProductInfoForFSRD(getProductInfoForFSRDRequest);
            // Assert
            Assert.True(result.Result != null);
            Assert.True(result.Result.Exception.Message.Equals(_configuration.GetValue<string>("Booking2OGenericExceptionMessage")));
        }

        [Fact]
        public void GetProductInfoForFSRD_Test_Exception1()
        {
         // var _shopproductsController = new ShopProductsController(_logger.Object, Configuration1, _shopproductsBusiness.Object, _headers.Object, _requestEnricher.Object);

            var returns = new GetProductInfoForFSRDResponse();
            var getProductInfoForFSRDRequest = new GetProductInfoForFSRDRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shopProductsBusiness.Setup(p => p.GetProductInfoForFSRD(getProductInfoForFSRDRequest)).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _ShoppingController.GetProductInfoForFSRD(getProductInfoForFSRDRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Fact]
        public void GetProductInfoForFSRD_Test_WebException()
        {
            var returns = new GetProductInfoForFSRDResponse();
            var getProductInfoForFSRDRequest = new GetProductInfoForFSRDRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shopProductsBusiness.Setup(p => p.GetProductInfoForFSRD(getProductInfoForFSRDRequest)).ThrowsAsync(new System.Net.WebException("Error Message"));
            // Act
            var result = _ShoppingController.GetProductInfoForFSRD(getProductInfoForFSRDRequest);
            // Assert
            Assert.True(result.Result != null);

        }

        [Fact]
        public void GetProductInfoForFSRD_Test_MOBUnitedException()
        {
            var returns = new GetProductInfoForFSRDResponse();
            var getProductInfoForFSRDRequest = new GetProductInfoForFSRDRequest()
            {
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4" } },
                TransactionId = "",
                LanguageCode = "en-US",
                AccessCode = "ACCESSCODE",
                DeviceId = "20317",
            };

            _shopProductsBusiness.Setup(p => p.GetProductInfoForFSRD(getProductInfoForFSRDRequest)).ThrowsAsync(new MOBUnitedException("Error Message"));
            // Act
            var result = _ShoppingController.GetProductInfoForFSRD(getProductInfoForFSRDRequest);
            // Assert
            Assert.True(result.Result != null);
        }

        [Fact]
        public void ShopTripPlan_Test()
        {

            var returns = new ShopResponse()
            {
                
                CallDuration = 23456789012556721,
                CartId = "375D3212-9BFF-42F7-94A6-42342E2E24D2"

            };

            var request = new MOBSHOPTripPlanRequest()
            {
                SessionId = "67945321097C4CF58FFC7DF9565CB276",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.GetShopTripPlan(request, _httpContext.Object)).ReturnsAsync(returns);


            // Act
            var result = _ShoppingController.ShopTripPlan(request);
            // Assert
            Assert.True(result != null);
            //Assert.True(result.Result.CallDuration > 0);
            // Assert.True(result.Result.Exception == null);


        }

        [Fact]
        public void ShopTripPlan_MOBUnitedException()
        {
            var returns = new ShopResponse() { };

            var request = new MOBSHOPTripPlanRequest()
            {
                SessionId = "67945321097C4CF58FFC7DF9565CB276",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.GetShopTripPlan(request, _httpContext.Object)).ThrowsAsync(new MOBUnitedException("United data services are not currently available."));


            // Act
            var result = _ShoppingController.ShopTripPlan(request);
            // Assert
            // Assert.True(result != null && result.Result.Exception.Message== "United data services are not currently available.");
            //  Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }

        [Fact]
        public void ShopTripPlan_Exception()
        {
            var returns = new ShopResponse() { };

            var request = new MOBSHOPTripPlanRequest()
            {
                SessionId = "67945321097C4CF58FFC7DF9565CB276",
                TransactionId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685|a43aa991-9d57-40f0-9b83-79f34d880e7c",
                DeviceId = "446e943d-d749-4f2d-8bf2-e0ba5d6da685",
                Application = new MOBApplication() { Id = 1, Version = new MOBVersion() { Major = "4.1.48" } }
            };

            _ShoppingController.ControllerContext = new ControllerContext();
            _ShoppingController.ControllerContext.HttpContext = new DefaultHttpContext();
            _ShoppingController.ControllerContext.HttpContext.Request.Headers["device-id"] = "20317";

            _shoppingBusiness.Setup(p => p.GetShopTripPlan(request, _httpContext.Object)).ThrowsAsync(new System.Exception("United data services are not currently available."));


            // Act
            var result = _ShoppingController.ShopTripPlan(request);
            // Assert
            // Assert.True(result != null && result.Result.Exception.Message == "Sorry, something went wrong. Please try again.");
            //Assert.True(result.Result.CallDuration > 0);
            Assert.True(result != null);

        }
    }
}

