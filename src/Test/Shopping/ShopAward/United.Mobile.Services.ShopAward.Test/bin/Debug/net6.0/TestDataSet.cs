using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.AwardCalendar;
using United.Mobile.Shopping.ShopAward.Test;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using Session = United.Mobile.Model.Common.Session;
using CSLShopRequest = United.Mobile.Model.Shopping.CSLShopRequest;
using ShopResponse = United.Services.FlightShopping.Common.ShopResponse;
using United.Service.Presentation.ReservationResponseModel;

namespace United.Mobile.Shopping.ShopAward.Tests
{
    public class TestDataSet
    {
        public Object[] set1()
        {
            var RevenueLowestPriceForAwardSearchRequestJson = TestDataGenerator.GetFileContent("RevenueLowestPriceForAwardSearchRequest.json");
            var RevenueLowestPriceForAwardSearchRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(RevenueLowestPriceForAwardSearchRequestJson);

            var RevenueLowestPriceForAwardSearchResponseJson = TestDataGenerator.GetFileContent("RevenueLowestPriceForAwardSearchResponse.json");
            var RevenueLowestPriceForAwardSearchResponse = JsonConvert.DeserializeObject<List<RevenueLowestPriceForAwardSearchResponse>>(RevenueLowestPriceForAwardSearchResponseJson);

            // var shopRequestJson = TestDataGenerator.GetFileContent("shopRequest.json");
            // var shopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(shopRequestJson);

            var shopResponseJson = TestDataGenerator.GetFileContent("shopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponseJson);
            var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");
            var reservation = TestDataGenerator.GetXmlData<Reservation>(@"Set1\Reservation.xml");
            // var reservationDetail = TestDataGenerator.GetXmlData<ReservationDetail>(@"Set1\ReservationDetail.xml");
            var session = TestDataGenerator.GetXmlData<Session>(@"Set1\Session.xml");

            return new object[] { RevenueLowestPriceForAwardSearchRequest[0], session, RevenueLowestPriceForAwardSearchResponse[0], latestShopAvailabilityResponse, reservation, shopResponse[0] };

        }
        public Object[] set2()
        {
            //var shopRequestJson = TestDataGenerator.GetFileContent("shopRequest.json");
            //var shopRequest = JsonConvert.DeserializeObject<List<SelectTripRequest>>(shopRequestJson);

            var selectTripRequestJson = TestDataGenerator.GetFileContent("GetSelectTripAwardCalendarRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<SelectTripRequest>>(selectTripRequestJson);

           // var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set2\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"Set2\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<ShoppingResponse>(@"Set2\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set2\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<ShoppingResponse>(shoppingResponsejson);

            //var shopResponsejson = TestDataGenerator.GetXmlData<ShopResponse>(@"Set2\ShoppingResponse.xml");
            // var shopResponseJson = TestDataGenerator.GetFileContent("shopResponse.json");
            //var shopResponse = JsonConvert.DeserializeObject<List<ShopResponse>>(shopResponseJson);

            var shopResponseJson = TestDataGenerator.GetFileContent("shopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponseJson);


           // var session = TestDataGenerator.GetXmlData<Session>(@"Set1\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent("session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var displayAirportDetails = new DisplayAirportDetails() { AirportCode = "IAH", AirportName = "IAH" };
            return new object[] { selectTripRequest[0], cSLShopRequest[0], shoppingResponse, session[0], shopResponse[0], displayAirportDetails };
        }
        public Object[] set3()
        {
            //var GetShopAwardCalendarRequestJson = TestDataGenerator.GetFileContent("GetShopAwardCalendarRequest.json");
            //var GetShopAwardCalendarRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(GetShopAwardCalendarRequestJson);

            var ShopRequestJson = TestDataGenerator.GetFileContent("shopRequest.json");
            var ShopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(ShopRequestJson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"Set1\Session.xml");
            var sessionjson = TestDataGenerator.GetFileContent("session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");
            var latestShopAvailabilityResponsejson = TestDataGenerator.GetFileContent(@"Set1\LatestShopAvailabilityResponse.json");
            var latestShopAvailabilityResponse = JsonConvert.DeserializeObject<LatestShopAvailabilityResponse>(latestShopAvailabilityResponsejson);

            //var reservation = TestDataGenerator.GetXmlData<Reservation>(@"Set1\Reservation.xml");

            var persistedReservationJson = TestDataGenerator.GetFileContent("persistedReservation.json");
            var persistedReservation = JsonConvert.DeserializeObject<List<Reservation>>(persistedReservationJson);

            var shopResponseJson = TestDataGenerator.GetFileContent("shopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponseJson);

            // var shoppingResponse = TestDataGenerator.GetXmlData<ShoppingResponse>(@"Set2\ShoppingResponse.xml");
            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set2\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<ShoppingResponse>(shoppingResponsejson);

            var mPAccountSummaryJson = TestDataGenerator.GetFileContent("MPAccountSummary.json");
            var mPAccountSummary = JsonConvert.DeserializeObject<List<MPAccountSummary>>(mPAccountSummaryJson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set2\CSLShopRequest.xml");
            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"Set2\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestjson);

            var reservationDetailJson = TestDataGenerator.GetFileContent("ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<ReservationDetail>(reservationDetailJson);

            return new object[] { ShopRequest[0], session[0], latestShopAvailabilityResponse, persistedReservation[0], shopResponse[0],  shoppingResponse, mPAccountSummary[0], cSLShopRequest[0], reservationDetail };
        }

        public Object[] set3_1()
        {
            //var GetShopAwardCalendarRequestJson = TestDataGenerator.GetFileContent("GetShopAwardCalendarRequest.json");
            //var GetShopAwardCalendarRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(GetShopAwardCalendarRequestJson);

            var ShopRequestJson = TestDataGenerator.GetFileContent("shopRequest.json");
            var ShopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(ShopRequestJson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"Set1\Session.xml");
            var sessionjson = TestDataGenerator.GetFileContent("session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");
            var latestShopAvailabilityResponsejson = TestDataGenerator.GetFileContent(@"Set1\LatestShopAvailabilityResponse.json");
            var latestShopAvailabilityResponse = JsonConvert.DeserializeObject<LatestShopAvailabilityResponse>(latestShopAvailabilityResponsejson);

            //var reservation = TestDataGenerator.GetXmlData<Reservation>(@"Set1\Reservation.xml");

            var persistedReservationJson = TestDataGenerator.GetFileContent("persistedReservation.json");
            var persistedReservation = JsonConvert.DeserializeObject<List<Reservation>>(persistedReservationJson);

            var shopResponseJson = TestDataGenerator.GetFileContent("shopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponseJson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<ShoppingResponse>(@"Set2\ShoppingResponse.xml");
            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set2\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<ShoppingResponse>(shoppingResponsejson);

            var mPAccountSummaryJson = TestDataGenerator.GetFileContent("MPAccountSummary.json");
            var mPAccountSummary = JsonConvert.DeserializeObject<List<MPAccountSummary>>(mPAccountSummaryJson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set2\CSLShopRequest.xml");
            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"Set2\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestjson);

            var reservationDetailJson = TestDataGenerator.GetFileContent("ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<ReservationDetail>(reservationDetailJson);

            return new object[] { ShopRequest[0], session[0], latestShopAvailabilityResponse, persistedReservation[0], shopResponse[0], shoppingResponse, mPAccountSummary[0], cSLShopRequest[0], reservationDetail };
        }

        public Object[] set3_2()
        {
            //var GetShopAwardCalendarRequestJson = TestDataGenerator.GetFileContent("GetShopAwardCalendarRequest.json");
            //var GetShopAwardCalendarRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(GetShopAwardCalendarRequestJson);

            var ShopRequestJson = TestDataGenerator.GetFileContent(@"ReshopFlow\shopRequest.json");
            var ShopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(ShopRequestJson);

            var sessionjson = TestDataGenerator.GetFileContent("session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var latestShopAvailabilityResponsejson = TestDataGenerator.GetFileContent(@"Set1\LatestShopAvailabilityResponse.json");
            var latestShopAvailabilityResponse = JsonConvert.DeserializeObject<LatestShopAvailabilityResponse>(latestShopAvailabilityResponsejson);

            var persistedReservationJson = TestDataGenerator.GetFileContent("persistedReservation.json");
            var persistedReservation = JsonConvert.DeserializeObject<List<Reservation>>(persistedReservationJson);

            var shopResponseJson = TestDataGenerator.GetFileContent("shopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponseJson);

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set2\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<ShoppingResponse>(shoppingResponsejson);

            var mPAccountSummaryJson = TestDataGenerator.GetFileContent("MPAccountSummary.json");
            var mPAccountSummary = JsonConvert.DeserializeObject<List<MPAccountSummary>>(mPAccountSummaryJson);

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"Set2\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestjson);

            var reservationDetailJson = TestDataGenerator.GetFileContent("ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<ReservationDetail>(reservationDetailJson);

            return new object[] { ShopRequest[0], session[0], latestShopAvailabilityResponse, persistedReservation[0], shopResponse[0], shoppingResponse, mPAccountSummary[0], cSLShopRequest[0], reservationDetail };
        }

        public Object[] set4()
        {
            var SelectTripRequestJson = TestDataGenerator.GetFileContent("GetSelectTripAwardCalendarRequest.json");
            var SelectTripRequest = JsonConvert.DeserializeObject<List<SelectTripRequest>>(SelectTripRequestJson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set4\CSLShopRequest.xml");
            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"Set4\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<ShoppingResponse>(@"Set4\ShoppingResponse.xml");
            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set4\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<ShoppingResponse>(shoppingResponsejson);


            //var shopResponseJson = TestDataGenerator.GetFileContent("shopResponse.json");
            //var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponseJson);

            var sessionjson = TestDataGenerator.GetFileContent("session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var displayAirportDetails = new DisplayAirportDetails() { AirportCode = "IAH", AirportName = "IAH" };

            var shopResponseJson = TestDataGenerator.GetFileContent("shopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<ShopResponse>>(shopResponseJson);

            return new object[] { SelectTripRequest[0], cSLShopRequest[0], shoppingResponse, session[0],  displayAirportDetails, shopResponse[0] };
        }

        public Object[] set4_1()
        {
            var SelectTripRequestJson = TestDataGenerator.GetFileContent("GetSelectTripAwardCalendarRequest.json");
            var SelectTripRequest = JsonConvert.DeserializeObject<List<SelectTripRequest>>(SelectTripRequestJson);

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"Set4\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestjson);

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set4\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<ShoppingResponse>(shoppingResponsejson);

            var sessionjson = TestDataGenerator.GetFileContent("session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var displayAirportDetails = new DisplayAirportDetails() { AirportCode = "IAH", AirportName = "IAH" };

            var shopResponseJson = TestDataGenerator.GetFileContent("shopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<ShopResponse>>(shopResponseJson);

            return new object[] { SelectTripRequest[0], cSLShopRequest[0], shoppingResponse, session[1], displayAirportDetails, shopResponse[0] };
        }

        public Object[] set4_2()
        {
            var SelectTripRequestJson = TestDataGenerator.GetFileContent(@"ReshopFlow\GetSelectTripAwardCalendarRequest.json");
            var SelectTripRequest = JsonConvert.DeserializeObject<List<SelectTripRequest>>(SelectTripRequestJson);

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"Set4\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestjson);

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set4\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<ShoppingResponse>(shoppingResponsejson);

            //var shopResponseJson = TestDataGenerator.GetFileContent("shopResponse.json");
            //var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponseJson);

            var sessionjson = TestDataGenerator.GetFileContent("session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var displayAirportDetails = new DisplayAirportDetails() { AirportCode = "IAH", AirportName = "IAH" };

            var shopResponseJson = TestDataGenerator.GetFileContent("shopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<ShopResponse>>(shopResponseJson);

            return new object[] { SelectTripRequest[0], cSLShopRequest[0], shoppingResponse, session[0], displayAirportDetails, shopResponse[0] };
        }
    }
}
