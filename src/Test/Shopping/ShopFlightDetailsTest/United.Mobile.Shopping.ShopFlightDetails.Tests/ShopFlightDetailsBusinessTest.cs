using Microsoft.AspNetCore.Http;
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
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Services.ShopFlightDetails.Domain;
using United.Services.FlightShopping.Common;
using United.Utility.Http;
using Xunit;
using United.Utility.Helper;
using United.Mobile.DataAccess.CMSContent;

namespace United.Mobile.Shopping.ShopFlightDetails.Tests
{
    public class ShopFlightDetailsBusinessTest
    {
        private readonly Mock<ICacheLog<ShopFlightDetailsBusiness>> _logger;
        private IConfiguration _configuration;
        private readonly Mock<ISessionHelperService> _sessionHelperService;
        private readonly Mock<IFlightShoppingService> _flightShoppingService;
        private readonly Mock<IShoppingUtility> _shoppingUtility;
        private readonly IHeaders _headers;
        private readonly ShopFlightDetailsBusiness shopFlightDetailsBusiness;
        private readonly Mock<IShoppingSessionHelper> _shoppingSessionHelper;
        private readonly Mock<ICMSContentService> _cMSContentService;
        private readonly Mock<ICachingService> _cachingService;
        private readonly Mock<IDPService> _dPService;



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
                    _configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings1.test.json", optional: false, reloadOnChange: true)
                    .Build();
                return _configuration;
            }
        }

        public ShopFlightDetailsBusinessTest()
        {
            _logger = new Mock<ICacheLog<ShopFlightDetailsBusiness>>();
            _sessionHelperService = new Mock<ISessionHelperService>();
            _flightShoppingService = new Mock<IFlightShoppingService>();
            _shoppingSessionHelper = new Mock<IShoppingSessionHelper>();
            _cMSContentService = new Mock<ICMSContentService>();
            _cachingService = new Mock<ICachingService>();
            _dPService = new Mock<IDPService>();
            _shoppingUtility = new Mock<IShoppingUtility>();
            shopFlightDetailsBusiness = new ShopFlightDetailsBusiness(_logger.Object, Configuration, _headers, _sessionHelperService.Object,
                _flightShoppingService.Object, _shoppingSessionHelper.Object, _cMSContentService.Object, _cachingService.Object, _dPService.Object);
        }
        private void SetupHttpContextAccessor()
        {
            var guid = Guid.NewGuid().ToString();
            var context = new DefaultHttpContext();
            
        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetFlifoScheduleRequests), MemberType = typeof(TestDataGenerator))]
        public void GetONTimePerformence_Test(Session session, OnTimePerformanceRequest onTimePerformanceRequest, OnTimePerformanceInfoResponse onTimePerformanceResponse)
        {
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);
            _flightShoppingService.Setup(p => p.GetOnTimePerformanceInfo<OnTimePerformanceInfoResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(onTimePerformanceResponse);
            //Act
            var result = shopFlightDetailsBusiness.GetONTimePerformence(onTimePerformanceRequest);
            //Assert
            Assert.True(result.Result != null && result.Exception == null);

        }

        [Theory]
        [MemberData(nameof(TestDataGenerator.GetFlifoScheduleRequests), MemberType = typeof(TestDataGenerator))]
        public void GetONTimePerformence_Test1(Session session, OnTimePerformanceRequest onTimePerformanceRequest, OnTimePerformanceInfoResponse onTimePerformanceResponse)
        {
           var shopFlightDetailsBusiness1 = new ShopFlightDetailsBusiness(_logger.Object, Configuration1, _headers, _sessionHelperService.Object, _flightShoppingService.Object, _shoppingSessionHelper.Object, _cMSContentService.Object, _cachingService.Object, _dPService.Object);
            _sessionHelperService.Setup(p => p.GetSession<Session>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<bool>())).ReturnsAsync(session);
            _flightShoppingService.Setup(p => p.GetOnTimePerformanceInfo<OnTimePerformanceInfoResponse>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(onTimePerformanceResponse);
            //Act
            var result = shopFlightDetailsBusiness1.GetONTimePerformence(onTimePerformanceRequest);
            //Assert
            Assert.True(result.Result != null && result.Exception == null);

        }
        [Theory]
        [InlineData("Booking:RTI", "BookingPathRTI_SDL_ContentMessagesCached_StaticGUID_MOBCSLContentMessagesResponse", true)]
        [InlineData("BOOKING:CORPORATETRAVELPOLICY", "CorporateTravelPolicy_SDL_ContentMessagesCached_MOBCSLContentMessagesResponse", true)]
        [InlineData("MANAGERES", "MANAGERES_SDL_ContentMessagesCached_StaticGUID_MOBCSLContentMessagesResponse", true)]
        [InlineData("MANAGERES:VIEWRES", "MANAGERES_CMSContentMessagesCached_DestImgMOBCSLContentMessagesResponse", true)]
        [InlineData("ManageReservation:Offers", "Stage_ManageReservation_Offers_CMSContentMessagesCached_StaticGUID_MOBCSLContentMessagesResponse", true)]
        [InlineData("ManageReservation:Offers", "Stage_ManageReservation_Offers_CMSContentMessagesCached_StaticGUID_", false)]
        public void IsValidSDLContentGroupNameCacheKeyCombination_Test(string groupName, string cacheKey, bool expected)
        {
            var result = shopFlightDetailsBusiness.IsValidSDLContentGroupNameCacheKeyCombination(groupName, cacheKey);

            Assert.True(result == expected);
        }

    }

}
