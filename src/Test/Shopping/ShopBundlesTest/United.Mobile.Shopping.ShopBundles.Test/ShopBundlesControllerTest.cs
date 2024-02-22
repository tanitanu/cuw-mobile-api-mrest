using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using United.Mobile.Model;
using System;
using System.IO;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.DataAccess.Common;
using United.Mobile.Services.ShopBundles.Api.Controllers;
using United.Mobile.Services.ShopBundles.Domain;
using Xunit;
using United.Mobile.Model.Shopping.Bundles;
using Microsoft.AspNetCore.Mvc;
using United.Mobile.Model.Internal.Exception;
using Newtonsoft.Json;
using System.Collections.Generic;
using United.Utility.Helper;

namespace United.Mobile.Shopping.ShopBundles.Tests
{
    public class ShopBundlesControllerTest
    {
        private readonly Mock<ICacheLog<ShopBundlesController>> _logger;
        private IConfiguration _configuration;
        private readonly Mock<IShopBundlesBusiness> _shopbundlesBusiness;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
        private readonly Mock<IDPService> _tokenService;
        private readonly ShopBundlesController _shopbundlesController;
        private readonly Mock<IHeaders> _headers;
        private readonly Mock<IFeatureSettings> _featureSetting;
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

        public IConfiguration Configuration1
        {
            get
            {
                //if (_configuration == null)
                //{
                _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings11.test.json", optional: false, reloadOnChange: true)
                .Build();
                //}
                return _configuration;
            }
        }

        public ShopBundlesControllerTest()
        {
            _logger = new Mock<ICacheLog<ShopBundlesController>>();
            _headers = new Mock<IHeaders>();
            _shopbundlesBusiness = new Mock<IShopBundlesBusiness>();
            _httpContextAccessor = new Mock<IHttpContextAccessor>();
            _tokenService = new Mock<IDPService>();
            SetupHttpContextAccessor();
            _shopbundlesController = new ShopBundlesController(_logger.Object, Configuration, _headers.Object, _shopbundlesBusiness.Object,_featureSetting.Object);
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

        public string GetFileContent(string fileName)
        {
            var path = Path.IsPathRooted(fileName) ? fileName
                                                : Path.GetRelativePath(Directory.GetCurrentDirectory(), fileName);
            return File.ReadAllText(path);
        }

        [Fact]
        public void GetBundles_CFOP_Test()
        {
            var shopbundlesrequestdata = GetFileContent(@"TestData\ShopBundlesRequest.json");
            var shopbundlesrequest = JsonConvert.DeserializeObject<List<BookingBundlesRequest>>(shopbundlesrequestdata);
            var returns = new BookingBundlesResponse(_configuration);

            _shopbundlesBusiness.Setup(p => p.GetBundles_CFOP(shopbundlesrequest[0])).Returns(Task.FromResult(returns));

            var result = _shopbundlesController.GetBundles_CFOP(shopbundlesrequest[0]);

            Assert.True(result.Result != null);
        }

        [Fact]
        public void GetBundles_CFOP_Test_Exception()
        {
            var shopbundlesrequestdata = GetFileContent(@"TestData\ShopBundlesRequest.json");
            var shopbundlesrequest = JsonConvert.DeserializeObject<List<BookingBundlesRequest>>(shopbundlesrequestdata);
            var returns = new BookingBundlesResponse(_configuration);

            _shopbundlesBusiness.Setup(p => p.GetBundles_CFOP(shopbundlesrequest[0])).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _shopbundlesController.GetBundles_CFOP(shopbundlesrequest[0]);
            // Assert
            Assert.True(result.Result.Exception != null);
            Assert.True(result.Result.Exception.Message.Equals(Configuration.GetValue<string>("Booking2OGenericExceptionMessage")));
        }

        [Fact]
        public void GetBundles_CFOP_Test_Exception1()
        {
           var _shopbundlesController = new ShopBundlesController(_logger.Object, Configuration1, _headers.Object, _shopbundlesBusiness.Object,_featureSetting.Object);

            var shopbundlesrequestdata = GetFileContent(@"TestData\ShopBundlesRequest.json");
            var shopbundlesrequest = JsonConvert.DeserializeObject<List<BookingBundlesRequest>>(shopbundlesrequestdata);
            var returns = new BookingBundlesResponse(_configuration);

            _shopbundlesBusiness.Setup(p => p.GetBundles_CFOP(shopbundlesrequest[0])).ThrowsAsync(new Exception("Error Message"));
            // Act
            var result = _shopbundlesController.GetBundles_CFOP(shopbundlesrequest[0]);
            // Assert
            Assert.True(result.Result.Exception != null);
        }

        [Fact]
        public void GetBundles_CFOP_Test_MOBUnitedException()
        {
            var shopbundlesrequestdata = GetFileContent(@"TestData\ShopBundlesRequest.json");
            var shopbundlesrequest = JsonConvert.DeserializeObject<List<BookingBundlesRequest>>(shopbundlesrequestdata);
            var returns = new BookingBundlesResponse(_configuration);

            _shopbundlesBusiness.Setup(p => p.GetBundles_CFOP(shopbundlesrequest[0])).ThrowsAsync(new MOBUnitedException("Error Message"));
            // Act
            var result = _shopbundlesController.GetBundles_CFOP(shopbundlesrequest[0]);
            // Assert
            Assert.True(result.Result.Exception != null);

        }
    }
}
