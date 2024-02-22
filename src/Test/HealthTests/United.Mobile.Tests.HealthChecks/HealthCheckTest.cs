using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using United.Mobile.Services.Shopping.Api.Controllers;
using United.Mobile.Services.ShopTrips.Api.Controllers;
using United.Mobile.Services.ShopBundles.Api.Controllers;
using United.Mobile.Services.ShopFareWheel.Api.Controllers;
using United.Mobile.Services.ShopFlightDetails.Api.Controllers;
using United.Mobile.Services.ShopSeats.Api.Controllers;
using United.Mobile.Services.ShopAward.Api.Controllers;
using United.Mobile.BagCalculator.Api.Controllers;
using United.Mobile.Services.UnfinishedBooking.Api.Controllers;
using United.Mobile.MemberProfile.Api.Controllers;
using United.Mobile.UpdateMemberProfile.Api.Controllers;
using United.Mobile.Services.TripPlannerService.Api.Controllers;
using United.Mobile.Services.TripPlannerGetService.Api.Controllers;
using United.Mobile.PostBooking.Api.Controllers;
using United.Mobile.Travelers.API.Controllers;
using United.Mobile.SeatEngine.Api.Controllers;
using United.Mobile.UnitedClub.Api.Controllers;
using United.Mobile.ReShop.Api.Controllers;
using United.Mobile.SeatMap.Api.Controllers;

namespace United.Mobile.Tests.HealthChecks
{
    public class HealthCheckTest
    {
        #region Shopping

        [Fact]
        public async Task ShoppingHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<ShoppingController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shoppingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShoppingHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<ShoppingController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shoppingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShoppingHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<ShoppingController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shoppingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShoppingHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<ShoppingController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shoppingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region ShopTrips
        [Fact]
        public async Task ShopTripsHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<ShopTripsController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shoptripsservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopTripsHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<ShopTripsController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shoptripsservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopTripsHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<ShopTripsController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shoptripsservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopTripsHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<ShopTripsController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shoptripsservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region Unfinishedbooking

        [Fact]
        public async Task UnfinishedbookingHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<UnfinishedBookingController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("unfinishedbookingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task UnfinishedbookingHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<UnfinishedBookingController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("unfinishedbookingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task UnfinishedbookingHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<UnfinishedBookingController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("unfinishedbookingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task UnfinishedbookingHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<UnfinishedBookingController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("unfinishedbookingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region BagCalculator

        [Fact]
        public async Task BagCalculatorHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<BagCalculatorController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("bagcalculatorservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task BagCalculatorHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<BagCalculatorController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("bagcalculatorservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task BagCalculatorHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<BagCalculatorController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("bagcalculatorservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task BagCalculatorHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<BagCalculatorController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("bagcalculatorservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region MemberProfile

        [Fact]
        public async Task MemberProfileHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<MemberProfileController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("memberprofileservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task MemberProfileHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<MemberProfileController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("memberprofileservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task MemberProfileHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<MemberProfileController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("memberprofileservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task MemberProfileHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<MemberProfileController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("memberprofileservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region UpdateMemberProfile

        [Fact]
        public async Task UpdateMemberProfileHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<UpdateMemberProfileController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("updatememberprofileservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task UpdateMemberProfileHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<UpdateMemberProfileController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("updatememberprofileservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task UpdateMemberProfileHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<UpdateMemberProfileController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("updatememberprofileservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task UpdateMemberProfileHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<UpdateMemberProfileController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("updatememberprofileservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region ShopBundles

        [Fact]
        public async Task ShopBundlesHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<ShopBundlesController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopbundlesservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopBundlesHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<ShopBundlesController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopbundlesservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopBundlesHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<ShopBundlesController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopbundlesservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopBundlesHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<ShopBundlesController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopbundlesservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region ShopAward

        [Fact]
        public async Task ShopAwardHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<ShopAwardController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopawardservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopAwardHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<ShopAwardController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopawardservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopAwardHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<ShopAwardController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopawardservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopAwardHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<ShopAwardController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopawardservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region ShopFareWheel

        [Fact]
        public async Task ShopFareWheelHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<ShopFareWheelController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopfarewheelservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopFareWheelHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<ShopFareWheelController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopfarewheelservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopFareWheelHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<ShopFareWheelController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopfarewheelservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopFareWheelHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<ShopFareWheelController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopfarewheelservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region ShopFlightDetails

        [Fact]
        public async Task ShopFlightDetailsHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<ShopFlightDetailsController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopflightdetailsservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopFlightDetailsHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<ShopFlightDetailsController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopflightdetailsservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopFlightDetailsHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<ShopFlightDetailsController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopflightdetailsservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopFlightDetailsHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<ShopFlightDetailsController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopflightdetailsservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region TripPlanner

        [Fact]
        public async Task TripPlannerHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<TripPlannerServiceController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("tripplannerservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task TripPlannerHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<TripPlannerServiceController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("tripplannerservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task TripPlannerHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<TripPlannerServiceController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("tripplannerservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task TripPlannerHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<TripPlannerServiceController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("tripplannerservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region TripPlannerGetService

        [Fact]
        public async Task TripPlannerGetHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<TripPlannerGetServiceController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("tripplannergetservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task TripPlannerGetHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<TripPlannerGetServiceController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("tripplannergetservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task TripPlannerGetHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<TripPlannerGetServiceController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("tripplannergetservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task TripPlannerGetHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<TripPlannerGetServiceController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("tripplannergetservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region ShopSeats

        [Fact]
        public async Task ShopSeatsHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<ShopSeatsController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopseatsservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopSeatsHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<ShopSeatsController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopseatsservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopSeatsHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<ShopSeatsController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopseatsservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ShopSeatsHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<ShopSeatsController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("shopseatsservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region SeatEngine

        [Fact]
        public async Task SeatEngineHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<SeatEngineController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("seatengineservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task SeatEngineHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<SeatEngineController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("seatengineservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task SeatEngineHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<SeatEngineController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("seatengineservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task SeatEngineHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<SeatEngineController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("seatengineservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region PostBooking

        [Fact]
        public async Task PostBookingHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<PostBookingController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("postbookingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task PostBookingHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<PostBookingController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("postbookingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task PostBookingHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<PostBookingController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("postbookingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task PostBookingHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<PostBookingController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("postbookingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region Travelers

        [Fact]
        public async Task TravelersHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<TravelersController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("travelersservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task TravelersHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<TravelersController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("travelersservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task TravelersHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<TravelersController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("travelersservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task TravelersHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<TravelersController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("travelersservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region UnitedClub

        [Fact]
        public async Task UnitedClubHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<UnitedClubController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("unitedclubservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task UnitedClubHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<UnitedClubController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("unitedclubservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task UnitedClubHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<UnitedClubController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("unitedclubservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task UnitedClubHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<UnitedClubController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("unitedclubservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region Reshop

        [Fact]
        public async Task ReshopHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<ReShopController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("reshoppingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ReshopHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<ReShopController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("reshoppingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ReshopHealthTest_UAT()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "UAT");
            var webappfactory = new WebApplicationFactory<ReShopController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("reshoppingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task ReshopHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<ReShopController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("reshoppingservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

        #region Seatmap

        [Fact]
        public async Task SeatmapHealthTest_Dev()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var webappfactory = new WebApplicationFactory<SeatMapController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("seatmapservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        [Fact]
        public async Task SeatmapHealthTest_QA()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "QA");
            var webappfactory = new WebApplicationFactory<SeatMapController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("seatmapservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
       
        [Fact]
        public async Task SeatmapHealthTest_ProdPerf()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "ProdPerf");
            var webappfactory = new WebApplicationFactory<SeatMapController>();
            var httpclient = webappfactory.CreateDefaultClient();
            var response = await httpclient.GetAsync("seatmapservice/api/HealthCheck");
            var result = await response.Content.ReadAsStringAsync();

            Assert.Equal("Healthy", result);
        }
        #endregion

    }
}