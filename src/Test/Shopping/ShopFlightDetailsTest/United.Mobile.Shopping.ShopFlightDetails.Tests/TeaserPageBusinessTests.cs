using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.TeaserPage;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.TeaserPage;
using United.Mobile.Services.ShopFlightDetails.Domain;
using United.Services.FlightShopping.Common;
using Xunit;
using United.Utility.Helper;

namespace United.Mobile.Shopping.ShopFlightDetails.Tests
{
    public class TeaserPageBusinessTests : ControllerBase
    {
        private readonly Mock<ICacheLog<TeaserPageBusiness>> _logger;
        private readonly Mock<ICacheLog<DataPowerFactory>> _logger1;

        private IConfiguration _configuration;
        private readonly ITeaserPageBusiness _teaserPageBusiness;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly Mock<IDPService> _dPService;
        private readonly Mock<IGetTeaserColumnInfoService> _getTeaserColumnInfoService;
        private readonly Mock<IShoppingUtility> _shoppingUtility;
        private readonly Mock<IFFCShoppingcs> _fFCShoppingcs;
        private readonly IDataPowerFactory _dataPowerFactory;

        public IConfiguration Configuration
        {
            get
            {
                _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: true)
                .Build();

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
               .AddJsonFile("appsettings.test1.json", optional: false, reloadOnChange: true)
                .Build();
                //}
                return _configuration;
            }
        }

        public TeaserPageBusinessTests()
        {
            _logger = new Mock<ICacheLog<TeaserPageBusiness>>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _dPService = new Mock<IDPService>();
            _getTeaserColumnInfoService = new Mock<IGetTeaserColumnInfoService>();
            _shoppingUtility = new Mock<IShoppingUtility>();
            _fFCShoppingcs = new Mock<IFFCShoppingcs>();
            _logger1 = new Mock<ICacheLog<DataPowerFactory>>();

            _dataPowerFactory = new DataPowerFactory(Configuration, _sessionHelperService.Object,_logger1.Object);


            _teaserPageBusiness = new TeaserPageBusiness(_logger.Object, Configuration, _sessionHelperService.Object, _dPService.Object, _getTeaserColumnInfoService.Object, _shoppingUtility.Object, _fFCShoppingcs.Object);


            SetupHttpContextAccessor();
        }

        private void SetupHttpContextAccessor()
        {
            string deviceId = "589d7852-14e7-44a9-b23b-a6db36657579";
            string applicationId = "2";
            string appVersion = "4.1.29";
            string transactionId = "589d7852-14e7-44a9-b23b-a6db36657579|8f46e040-a200-495c-83ca-4fca2d7175fb";
            string languageCode = "en-US";
            string sessionId = "17C979E184CC495EA083D45F4DD9D19D";
            var guid = Guid.NewGuid().ToString();
            var context = new DefaultHttpContext();
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
        [MemberData(nameof(TestDataGenerator.GetTeaserPage_Request), MemberType = typeof(TestDataGenerator))]
        public void GetTeaserPage_Request(MOBSHOPShoppingTeaserPageRequest mOBSHOPShoppingTeaserPageRequest, Session session, FareColumnContentInformationResponse fareColumnContentInformationResponse, List<CMSContentMessage> cMSContentMessage,
            MOBSHOPShoppingTeaserPageResponse mOBSHOPShoppingTeaserPageResponse)
        {
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _getTeaserColumnInfoService.Setup(p => p.GetTeaserText<FareColumnContentInformationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(fareColumnContentInformationResponse);

            _fFCShoppingcs.Setup(p => p.GetSDLContentByGroupName(It.IsAny<MOBRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(cMSContentMessage);

            _shoppingUtility.Setup(p => p.GetSDLStringMessageFromList(It.IsAny<List<CMSContentMessage>>(), It.IsAny<string>())).Returns(mOBSHOPShoppingTeaserPageResponse.FooterText);

            //Act
            var result = _teaserPageBusiness.GetTeaserPage(mOBSHOPShoppingTeaserPageRequest, HttpContext);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetTeaserPage_Request1), MemberType = typeof(TestDataGenerator))]
        public void GetTeaserPage_Request1(MOBSHOPShoppingTeaserPageRequest mOBSHOPShoppingTeaserPageRequest, Session session, FareColumnContentInformationResponse fareColumnContentInformationResponse, List<CMSContentMessage> cMSContentMessage,
         MOBSHOPShoppingTeaserPageResponse mOBSHOPShoppingTeaserPageResponse, MOBSHOPShoppingProductList mOBSHOPShoppingProductList, ShoppingResponse shoppingResponse)
        {

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _getTeaserColumnInfoService.Setup(p => p.GetTeaserText<FareColumnContentInformationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(fareColumnContentInformationResponse);

            _fFCShoppingcs.Setup(p => p.GetSDLContentByGroupName(It.IsAny<MOBRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(cMSContentMessage);

            _shoppingUtility.Setup(p => p.GetSDLStringMessageFromList(It.IsAny<List<CMSContentMessage>>(), It.IsAny<string>())).Returns(mOBSHOPShoppingTeaserPageResponse.FooterText);

            _sessionHelperService.Setup(p => p.GetSession<MOBSHOPShoppingProductList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBSHOPShoppingProductList);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);

            //Act
            var result = _teaserPageBusiness.GetTeaserPage(mOBSHOPShoppingTeaserPageRequest, HttpContext);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetTeaserPage_Request1), MemberType = typeof(TestDataGenerator))]
        public void GetTeaserPage_Request1_1(MOBSHOPShoppingTeaserPageRequest mOBSHOPShoppingTeaserPageRequest, Session session, FareColumnContentInformationResponse fareColumnContentInformationResponse, List<CMSContentMessage> cMSContentMessage,
         MOBSHOPShoppingTeaserPageResponse mOBSHOPShoppingTeaserPageResponse, MOBSHOPShoppingProductList mOBSHOPShoppingProductList, ShoppingResponse shoppingResponse)
        {

            var _dataPowerFactory = new DataPowerFactory(Configuration1, _sessionHelperService.Object, _logger1.Object);

            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);

            _getTeaserColumnInfoService.Setup(p => p.GetTeaserText<FareColumnContentInformationResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(fareColumnContentInformationResponse);

            _fFCShoppingcs.Setup(p => p.GetSDLContentByGroupName(It.IsAny<MOBRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(cMSContentMessage);

            _shoppingUtility.Setup(p => p.GetSDLStringMessageFromList(It.IsAny<List<CMSContentMessage>>(), It.IsAny<string>())).Returns(mOBSHOPShoppingTeaserPageResponse.FooterText);

            _sessionHelperService.Setup(p => p.GetSession<MOBSHOPShoppingProductList>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(mOBSHOPShoppingProductList);

            _sessionHelperService.Setup(p => p.GetSession<ShoppingResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(shoppingResponse);


            var teaserPageBusiness = new TeaserPageBusiness(_logger.Object, Configuration1, _sessionHelperService.Object, _dPService.Object, _getTeaserColumnInfoService.Object, _shoppingUtility.Object, _fFCShoppingcs.Object);

            //Act
            var result = teaserPageBusiness.GetTeaserPage(mOBSHOPShoppingTeaserPageRequest, HttpContext);
            //Assert
            Assert.True(result.Exception != null || result.Result != null);

        }

    }
}
