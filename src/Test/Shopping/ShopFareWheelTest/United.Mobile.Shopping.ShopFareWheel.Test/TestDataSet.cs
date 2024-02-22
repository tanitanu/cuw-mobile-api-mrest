using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Bundles;
using United.Service.Presentation.ReservationResponseModel;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Shopping.ShopFareWheel.Test
{
    public class TestDataSet

    {
        public Object[] set1()
        {

            var selectTripRequestjson = TestDataGenerator.GetFileContent("selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<SelectTripRequest>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"Set1\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"Set1\session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set1\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<Model.Shopping.ShoppingResponse>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var latestShopAvailabilityResponsejson = TestDataGenerator.GetFileContent(@"Set1\LatestShopAvailabilityResponse.json");
            var latestShopAvailabilityResponse = JsonConvert.DeserializeObject<LatestShopAvailabilityResponse>(latestShopAvailabilityResponsejson);

            var mOBSHOPFlattenedFlightList = TestDataGenerator.GetXmlData<MOBSHOPFlattenedFlightList>(@"Set1\MOBSHOPFlattenedFlightList.xml");

            var airportDetailsList = TestDataGenerator.GetXmlData<AirportDetailsList>(@"Set1\AirportDetailsList.xml");

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"Set1\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestjson);

            var shopResponsejson = TestDataGenerator.GetFileContent(@"Set1\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);

            var reservationjson = TestDataGenerator.GetFileContent("Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);


            return new object[] { selectTripRequest, session[0], shoppingResponse, latestShopAvailabilityResponse, mOBSHOPFlattenedFlightList, airportDetailsList, cSLShopRequest[0], shopResponse[0],reservation[0] };
        }

        public Object[] set1_1()
        {

            var selectTripRequestjson = TestDataGenerator.GetFileContent("selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<SelectTripRequest>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"Set1\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"Set1\session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set1\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<Model.Shopping.ShoppingResponse>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var latestShopAvailabilityResponsejson = TestDataGenerator.GetFileContent(@"Set1\LatestShopAvailabilityResponse.json");
            var latestShopAvailabilityResponse = JsonConvert.DeserializeObject<LatestShopAvailabilityResponse>(latestShopAvailabilityResponsejson);

            var mOBSHOPFlattenedFlightList = TestDataGenerator.GetXmlData<MOBSHOPFlattenedFlightList>(@"Set1\MOBSHOPFlattenedFlightList.xml");

            var airportDetailsList = TestDataGenerator.GetXmlData<AirportDetailsList>(@"Set1\AirportDetailsList.xml");

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"Set1\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestjson);

            var shopResponsejson = TestDataGenerator.GetFileContent(@"Set1\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);

            var reservationjson = TestDataGenerator.GetFileContent("Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);


            return new object[] { selectTripRequest, session[0], shoppingResponse, latestShopAvailabilityResponse, mOBSHOPFlattenedFlightList, airportDetailsList, cSLShopRequest[0], shopResponse[1], reservation[0] };
        }

        public Object[] set1_2()
        {

            var selectTripRequestjson = TestDataGenerator.GetFileContent("selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<SelectTripRequest>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"Set1\Session.xml");


            var sessionjson = TestDataGenerator.GetFileContent(@"Set1\session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set1\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<Model.Shopping.ShoppingResponse>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var latestShopAvailabilityResponsejson = TestDataGenerator.GetFileContent(@"Set1\LatestShopAvailabilityResponse.json");
            var latestShopAvailabilityResponse = JsonConvert.DeserializeObject<LatestShopAvailabilityResponse>(latestShopAvailabilityResponsejson);

            var mOBSHOPFlattenedFlightList = TestDataGenerator.GetXmlData<MOBSHOPFlattenedFlightList>(@"Set1\MOBSHOPFlattenedFlightList.xml");

            var airportDetailsList = TestDataGenerator.GetXmlData<AirportDetailsList>(@"Set1\AirportDetailsList.xml");

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"Set1\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestjson);


            var shopResponsejson = TestDataGenerator.GetFileContent(@"Set1\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);

            var reservationjson = TestDataGenerator.GetFileContent("Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);


            return new object[] { selectTripRequest, session[0], shoppingResponse, latestShopAvailabilityResponse, mOBSHOPFlattenedFlightList, airportDetailsList, cSLShopRequest[0], shopResponse[2], reservation[0] };
        }

        public Object[] set1_3()
        {

            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"ReshopFlow\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<SelectTripRequest>(selectTripRequestjson);

            var sessionjson = TestDataGenerator.GetFileContent(@"Set1\session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set1\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<Model.Shopping.ShoppingResponse>(shoppingResponsejson);

            var latestShopAvailabilityResponsejson = TestDataGenerator.GetFileContent(@"Set1\LatestShopAvailabilityResponse.json");
            var latestShopAvailabilityResponse = JsonConvert.DeserializeObject<LatestShopAvailabilityResponse>(latestShopAvailabilityResponsejson);

            var mOBSHOPFlattenedFlightList = TestDataGenerator.GetXmlData<MOBSHOPFlattenedFlightList>(@"Set1\MOBSHOPFlattenedFlightList.xml");

            //var airportDetailsList = TestDataGenerator.GetXmlData<AirportDetailsList>(@"Set1\AirportDetailsList.xml");

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"Set2\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<AirportDetailsList>(airportDetailsListjson);

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"Set1\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestjson);

            var shopResponsejson = TestDataGenerator.GetFileContent(@"Set1\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);

            var reservationjson = TestDataGenerator.GetFileContent("Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);


            return new object[] { selectTripRequest, session[0], shoppingResponse, latestShopAvailabilityResponse, mOBSHOPFlattenedFlightList, airportDetailsList, cSLShopRequest[0], shopResponse[1], reservation[0] };
        }

        public Object[] set2()
        {
            var shopRequestjson = TestDataGenerator.GetFileContent("shopRequest.json");
            var shopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(shopRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"Set2\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"Set2\session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            // var reservation= TestDataGenerator.GetXmlData<Reservation>(@"Set2\Reservation.xml");

            var reservationjson = TestDataGenerator.GetFileContent("Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set2\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set2\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<Model.Shopping.ShoppingResponse>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set2\LatestShopAvailabilityResponse.xml");

            var latestShopAvailabilityResponsejson = TestDataGenerator.GetFileContent(@"Set2\LatestShopAvailabilityResponse.json");
            var latestShopAvailabilityResponse = JsonConvert.DeserializeObject<LatestShopAvailabilityResponse>(latestShopAvailabilityResponsejson);

            var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set2\CSLShopRequest.xml");

            //var airportDetailsList = TestDataGenerator.GetXmlData<AirportDetailsList>(@"Set2\AirportDetailsList.xml");

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"Set2\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<AirportDetailsList>(airportDetailsListjson);

            var shopResponsejson = TestDataGenerator.GetFileContent(@"Set2\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<United.Services.FlightShopping.Common.ShopResponse>(shopResponsejson);

            var mPAccountSummaryjson = TestDataGenerator.GetFileContent(@"Set2\MPAccountSummary.json");
            var mPAccountSummary = JsonConvert.DeserializeObject<MPAccountSummary>(mPAccountSummaryjson);

            var reservationDetailjson = TestDataGenerator.GetFileContent("ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<ReservationDetail>(reservationDetailjson);


            return new object[] { shopRequest[0], session[0], shoppingResponse, cSLShopRequest,latestShopAvailabilityResponse, airportDetailsList, reservation[0], shopResponse, mPAccountSummary, reservationDetail };
        }

        public Object[] set2_1()
        {
            var shopRequestjson = TestDataGenerator.GetFileContent(@"ReshopFlow\shopRequest.json");
            var shopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(shopRequestjson);

            var sessionjson = TestDataGenerator.GetFileContent(@"Set2\session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationjson = TestDataGenerator.GetFileContent("Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set2\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<Model.Shopping.ShoppingResponse>(shoppingResponsejson);

            var latestShopAvailabilityResponsejson = TestDataGenerator.GetFileContent(@"Set2\LatestShopAvailabilityResponse.json");
            var latestShopAvailabilityResponse = JsonConvert.DeserializeObject<LatestShopAvailabilityResponse>(latestShopAvailabilityResponsejson);

            var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set2\CSLShopRequest.xml");

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"Set2\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<AirportDetailsList>(airportDetailsListjson);

            var shopResponsejson = TestDataGenerator.GetFileContent(@"Set2\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<United.Services.FlightShopping.Common.ShopResponse>(shopResponsejson);

            var mPAccountSummaryjson = TestDataGenerator.GetFileContent(@"Set2\MPAccountSummary.json");
            var mPAccountSummary = JsonConvert.DeserializeObject<MPAccountSummary>(mPAccountSummaryjson);

            var reservationDetailjson = TestDataGenerator.GetFileContent("ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<ReservationDetail>(reservationDetailjson);


            return new object[] { shopRequest[0], session[0], shoppingResponse, cSLShopRequest, latestShopAvailabilityResponse, airportDetailsList, reservation[0], shopResponse, mPAccountSummary, reservationDetail };
        }

        public Object[] set3()
        {
            var shopRequestjson = TestDataGenerator.GetFileContent("shopRequest.json");
            var shopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(shopRequestjson);

            var session = TestDataGenerator.GetXmlData<Session>(@"Set2\Session.xml");

            // var reservation= TestDataGenerator.GetXmlData<Reservation>(@"Set2\Reservation.xml");

            var reservationjson = TestDataGenerator.GetFileContent("Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set2\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set2\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<Model.Shopping.ShoppingResponse>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set2\LatestShopAvailabilityResponse.xml");

            var latestShopAvailabilityResponsejson = TestDataGenerator.GetFileContent(@"Set2\LatestShopAvailabilityResponse.json");
            var latestShopAvailabilityResponse = JsonConvert.DeserializeObject<LatestShopAvailabilityResponse>(latestShopAvailabilityResponsejson);

            var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set2\CSLShopRequest.xml");

            var airportDetailsList = TestDataGenerator.GetXmlData<AirportDetailsList>(@"Set2\AirportDetailsList.xml");

            var shopResponsejson = TestDataGenerator.GetFileContent(@"Set2\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<United.Services.FlightShopping.Common.ShopResponse>(shopResponsejson);


            var mPAccountSummaryjson = TestDataGenerator.GetFileContent(@"Set2\MPAccountSummary.json");
            var mPAccountSummary = JsonConvert.DeserializeObject<MPAccountSummary>(mPAccountSummaryjson);

            var reservationDetailjson = TestDataGenerator.GetFileContent("ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<ReservationDetail>(reservationDetailjson);

            return new object[] { shopRequest[1], session, shoppingResponse, cSLShopRequest, latestShopAvailabilityResponse, airportDetailsList, reservation[1], shopResponse, mPAccountSummary, reservationDetail };
        }

        public Object[] set4()
        {
            var shopRequestjson = TestDataGenerator.GetFileContent("shopRequest.json");
            var shopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(shopRequestjson);

            var session = TestDataGenerator.GetXmlData<Session>(@"Set2\Session.xml");

            // var reservation= TestDataGenerator.GetXmlData<Reservation>(@"Set2\Reservation.xml");

            var reservationjson = TestDataGenerator.GetFileContent("Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set2\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"Set2\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<Model.Shopping.ShoppingResponse>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set2\LatestShopAvailabilityResponse.xml");

            var latestShopAvailabilityResponsejson = TestDataGenerator.GetFileContent(@"Set2\LatestShopAvailabilityResponse.json");
            var latestShopAvailabilityResponse = JsonConvert.DeserializeObject<LatestShopAvailabilityResponse>(latestShopAvailabilityResponsejson);

            var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set2\CSLShopRequest.xml");

            var airportDetailsList = TestDataGenerator.GetXmlData<AirportDetailsList>(@"Set2\AirportDetailsList.xml");

            var shopResponsejson = TestDataGenerator.GetFileContent(@"Set2\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<United.Services.FlightShopping.Common.ShopResponse>(shopResponsejson);


            var mPAccountSummaryjson = TestDataGenerator.GetFileContent(@"Set2\MPAccountSummary.json");
            var mPAccountSummary = JsonConvert.DeserializeObject<MPAccountSummary>(mPAccountSummaryjson);

            var reservationDetailjson = TestDataGenerator.GetFileContent("ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<ReservationDetail>(reservationDetailjson);

            return new object[] { shopRequest[2], session, shoppingResponse, cSLShopRequest, latestShopAvailabilityResponse, airportDetailsList, reservation[1], shopResponse, mPAccountSummary, reservationDetail };
        }



    }
}
