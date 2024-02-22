using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
//using United.Mobile.Model.Internal.Common;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
//using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Bundles;
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Mobile.Model.TripPlannerGetService;
using United.Service.Presentation.ReferenceDataResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common.FlightReservation;
using CSLShopRequest = United.Mobile.Model.Shopping.CSLShopRequest;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Shopping.ShopTrip.Test
{
    public class TestDataSet
    {
        public Object[] set1()
        {
            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTripRequest>>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

           // var shoppingResponse1 = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"SelectTrip_TestData\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<Model.Shopping.SelectTrip>(selectTripjson);



            var shopResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<Model.Shopping.AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<Model.Shopping.ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
            var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
            {
                Name = ""
            };
            features.Add(productFeature);
            var products = new United.Service.Presentation.ProductModel.SubProduct()
            {
                Features = features
            };
            var response = JsonConvert.SerializeObject(products);

            return new object[] { selectTripRequest[0], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[0], shopResponse[0], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment };
        }
        public Object[] set2()
        {

            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTripRequest>>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<Model.Shopping.SelectTrip>(selectTripjson);



            var shopResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");


            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);


            var displayAirportDetailsjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<Model.Shopping.AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<Model.Shopping.ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
            var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
            {
                Name = ""
            };
            features.Add(productFeature);
            var products = new United.Service.Presentation.ProductModel.SubProduct()
            {
                Features = features
            };
            var response = JsonConvert.SerializeObject(products);

            return new object[] { selectTripRequest[1], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[0], shopResponse[1], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment};

        }


        public Object[] set3()
        {
            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTripRequest>>(selectTripRequestjson);

            // var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<Model.Shopping.SelectTrip>(selectTripjson);



            var shopResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<Model.Shopping.AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<Model.Shopping.ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
            var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
            {
                Name = ""
            };
            features.Add(productFeature);
            var products = new United.Service.Presentation.ProductModel.SubProduct()
            {
                Features = features
            };
            var response = JsonConvert.SerializeObject(products);

            return new object[] { selectTripRequest[0], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[0], shopResponse[2], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment };
        }

        // For Cover Exception
        public Object[] set4()
        {
            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTripRequest>>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<Model.Shopping.SelectTrip>(selectTripjson);



            var shopResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<Model.Shopping.AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<Model.Shopping.ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
            var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
            {
                Name = ""
            };
            features.Add(productFeature);
            var products = new United.Service.Presentation.ProductModel.SubProduct()
            {
                Features = features
            };
            var response = JsonConvert.SerializeObject(products);

            return new object[] { selectTripRequest[2], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[0], shopResponse[0], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment };
        }

        public Object[] set5()
        {
            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTripRequest>>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<Model.Shopping.SelectTrip>(selectTripjson);



            var shopResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"SelectTrip_TestData\SelectTrip.xml");


            var reservationjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"SelectTrip_TestData\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<Model.Shopping.AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<Model.Shopping.ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
            var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
            {
                Name = ""
            };
            features.Add(productFeature);
            var products = new United.Service.Presentation.ProductModel.SubProduct()
            {
                Features = features
            };
            var response = JsonConvert.SerializeObject(products);

            return new object[] { selectTripRequest[3], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[0], shopResponse[0], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment };
        }


        public Object[] set6()
        {
            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTripRequest>>(selectTripRequestjson);

            // var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<Model.Shopping.SelectTrip>(selectTripjson);



            var shopResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<Model.Shopping.AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<Model.Shopping.ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
            var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
            {
                Name = ""
            };
            features.Add(productFeature);
            var products = new United.Service.Presentation.ProductModel.SubProduct()
            {
                Features = features
            };
            var response = JsonConvert.SerializeObject(products);

            return new object[] { selectTripRequest[4], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[0], shopResponse[0], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment };
        }

        public Object[] set7()
        {

            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTripRequest>>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<Model.Shopping.SelectTrip>(selectTripjson);



            var shopResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");


            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);


            var displayAirportDetailsjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<Model.Shopping.AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<Model.Shopping.ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<Model.Shopping.LatestShopAvailabilityResponse>(@"SelectTrip_TestData\LatestShopAvailabilityResponse.xml");


            var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
            var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
            {
                Name = ""
            };
            features.Add(productFeature);
            var products = new United.Service.Presentation.ProductModel.SubProduct()
            {
                Features = features
            };
            var response = JsonConvert.SerializeObject(products);

            return new object[] { selectTripRequest[1], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[0], shopResponse[1], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment, latestShopAvailabilityResponse };

        }

        public Object[] set8()
        {
            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTripRequest>>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<Model.Shopping.SelectTrip>(selectTripjson);



            var shopResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<Model.Shopping.AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<Model.Shopping.ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
            var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
            {
                Name = ""
            };
            features.Add(productFeature);
            var products = new United.Service.Presentation.ProductModel.SubProduct()
            {
                Features = features
            };
            var response = JsonConvert.SerializeObject(products);

            return new object[] { selectTripRequest[0], selectTrip, shoppingResponse[1], reservation, cSLShopRequest, session[0], shopResponse[3], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment };
        }

        public Object[] set9()
        {
            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTripRequest>>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<Model.Shopping.SelectTrip>(selectTripjson);



            var shopResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<Model.Shopping.AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<Model.Shopping.ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
            var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
            {
                Name = ""
            };
            features.Add(productFeature);
            var products = new United.Service.Presentation.ProductModel.SubProduct()
            {
                Features = features
            };
            var response = JsonConvert.SerializeObject(products);

            return new object[] { selectTripRequest[0], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[0], shopResponse[4], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment };
        }

        public Object[] set10()
        {
            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTripRequest>>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<Model.Shopping.SelectTrip>(selectTripjson);



            var shopResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<Model.Shopping.AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<Model.Shopping.ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
            var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
            {
                Name = ""
            };
            features.Add(productFeature);
            var products = new United.Service.Presentation.ProductModel.SubProduct()
            {
                Features = features
            };
            var response = JsonConvert.SerializeObject(products);

            return new object[] { selectTripRequest[0], selectTrip, shoppingResponse[2], reservation, cSLShopRequest, session[0], shopResponse[0], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment };
        }

        public Object[] set11()
        {
            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTripRequest>>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session1.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<Model.Shopping.SelectTrip>(selectTripjson);



            var shopResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<Model.Shopping.AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<Model.Shopping.ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
            var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
            {
                Name = ""
            };
            features.Add(productFeature);
            var products = new United.Service.Presentation.ProductModel.SubProduct()
            {
                Features = features
            };
            var response = JsonConvert.SerializeObject(products);

            return new object[] { selectTripRequest[0], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[1], shopResponse[0], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment };
        }

        public Object[] set12()
        {
            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTripRequest>>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<Model.Shopping.SelectTrip>(selectTripjson);



            var shopResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<Model.Shopping.AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<Model.Shopping.ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
            var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
            {
                Name = ""
            };
            features.Add(productFeature);
            var products = new United.Service.Presentation.ProductModel.SubProduct()
            {
                Features = features
            };
            var response = JsonConvert.SerializeObject(products);

            return new object[] { selectTripRequest[0], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[2], shopResponse[0], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment };
        }

        public Object[] set13()
        {
            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTripRequest>>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<Model.Shopping.SelectTrip>(selectTripjson);



            var shopResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<Model.Shopping.AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<Model.Shopping.ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var reservationDetailjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(reservationDetailjson);

            var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
            var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
            {
                Name = ""
            };
            features.Add(productFeature);
            var products = new United.Service.Presentation.ProductModel.SubProduct()
            {
                Features = features
            };
            var response = JsonConvert.SerializeObject(products);

            return new object[] { selectTripRequest[5], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[3], shopResponse[0], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment, reservationDetail };
        }
        public Object[] set14()
        {
            var selectTripRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTripRequest>>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<Model.Shopping.SelectTrip>(selectTripjson);



            var shopResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<Model.Shopping.AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<Model.Shopping.ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var reservationDetailjson = TestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(reservationDetailjson);

            var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
            var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
            {
                Name = ""
            };
            features.Add(productFeature);
            var products = new United.Service.Presentation.ProductModel.SubProduct()
            {
                Features = features
            };
            var response = JsonConvert.SerializeObject(products);

            return new object[] { selectTripRequest[5], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[4], shopResponse[0], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment, reservationDetail };
        }
        public Object[] GetTripCompareFareTypesRequestSet1()
        {
            var shoppingTripFareTypeDetailsRequestJson = TestDataGenerator.GetFileContent("shoppingTripFareTypeDetailsRequest.json");
            var shoppingTripFareTypeDetailsRequest = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingTripFareTypeDetailsRequest>>(shoppingTripFareTypeDetailsRequestJson);

            var sessionJson = TestDataGenerator.GetFileContent("session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            var getAnonymoustokenJson = TestDataGenerator.GetFileContent("getAnonymoustoken.json");
            var getAnonymoustoken = JsonConvert.DeserializeObject<List<string>>(getAnonymoustokenJson);

            var getColumnInfo_tokenJson = TestDataGenerator.GetFileContent("getColumnInfo_token.json");
            var getColumnInfo_token = JsonConvert.DeserializeObject<List<string>>(getColumnInfo_tokenJson);

            return new object[] { shoppingTripFareTypeDetailsRequest[0], session[0], getAnonymoustoken[0], getColumnInfo_token[0] };
        }

        public Object[] metaSelectTripSet()
        {
            var MetaSelectTripRequestJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\MetaSelectTripRequest.json");
            var metaSelectTripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.MetaSelectTripRequest>>(MetaSelectTripRequestJson);
            
            var sessionJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            var mOBSHOPSelectUnfinishedBookingRequestJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\MOBSHOPSelectUnfinishedBookingRequest.json");
            var mOBSHOPSelectUnfinishedBookingRequest = JsonConvert.DeserializeObject<List<MOBSHOPSelectUnfinishedBookingRequest>>(mOBSHOPSelectUnfinishedBookingRequestJson);

            var mOBSHOPAvailabilityJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<Model.Shopping.MOBSHOPAvailability>>(mOBSHOPAvailabilityJson);

            var mOBShoppingCartJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\mOBShoppingCart.json");
            var mOBShoppingCart= JsonConvert.DeserializeObject<List<Model.Shopping.MOBShoppingCart>>(mOBShoppingCartJson);


            return new object[] { metaSelectTripRequest[0], session[0], mOBSHOPSelectUnfinishedBookingRequest[0], mOBSHOPAvailability[0], mOBShoppingCart[0] };

        }

        public Object[] metaSelectTripSet1()
        {
            var MetaSelectTripRequestJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\MetaSelectTripRequest.json");
            var MetaSelectTripRequest = JsonConvert.DeserializeObject<List<MetaSelectTripRequest>>(MetaSelectTripRequestJson);

            var sessionJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            var mOBSHOPSelectUnfinishedBookingRequestJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\MOBSHOPSelectUnfinishedBookingRequest.json");
            var mOBSHOPSelectUnfinishedBookingRequest = JsonConvert.DeserializeObject<List<MOBSHOPSelectUnfinishedBookingRequest>>(mOBSHOPSelectUnfinishedBookingRequestJson);

            var mOBSHOPAvailabilityJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityJson);

            var mOBShoppingCartJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\mOBShoppingCart.json");
            var mOBShoppingCart = JsonConvert.DeserializeObject<List<MOBShoppingCart>>(mOBShoppingCartJson);

            var metaUserSessionSyncResponseJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\metaUserSessionSyncResponse.json");
            var metaUserSessionSyncResponse = JsonConvert.DeserializeObject<List<MetaUserSessionSyncResponse>>(metaUserSessionSyncResponseJson);

            var flightReservationResponseJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\flightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<List<FlightReservationResponse>>(flightReservationResponseJson);

            var shoppingResponseJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\shoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<ShoppingResponse>(shoppingResponseJson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<AirportDetailsList>(airportDetailsListjson);


            return new object[] { MetaSelectTripRequest[0], session[0], mOBSHOPSelectUnfinishedBookingRequest[0], mOBSHOPAvailability[0], mOBShoppingCart[0], metaUserSessionSyncResponse[0], flightReservationResponse[0], shoppingResponse, airportDetailsList };

        }

        public Object[] metaSelectTripSet1_1()
        {
            var MetaSelectTripRequestJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\MetaSelectTripRequest.json");
            var MetaSelectTripRequest = JsonConvert.DeserializeObject<List<MetaSelectTripRequest>>(MetaSelectTripRequestJson);

            var sessionJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            var mOBSHOPSelectUnfinishedBookingRequestJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\MOBSHOPSelectUnfinishedBookingRequest.json");
            var mOBSHOPSelectUnfinishedBookingRequest = JsonConvert.DeserializeObject<List<MOBSHOPSelectUnfinishedBookingRequest>>(mOBSHOPSelectUnfinishedBookingRequestJson);

            var mOBSHOPAvailabilityJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityJson);

            var mOBShoppingCartJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\mOBShoppingCart.json");
            var mOBShoppingCart = JsonConvert.DeserializeObject<List<MOBShoppingCart>>(mOBShoppingCartJson);

            var metaUserSessionSyncResponseJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\metaUserSessionSyncResponse.json");
            var metaUserSessionSyncResponse = JsonConvert.DeserializeObject<List<MetaUserSessionSyncResponse>>(metaUserSessionSyncResponseJson);

            var flightReservationResponseJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\flightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<List<FlightReservationResponse>>(flightReservationResponseJson);

            var shoppingResponseJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\shoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<ShoppingResponse>(shoppingResponseJson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<AirportDetailsList>(airportDetailsListjson);


            return new object[] { MetaSelectTripRequest[0], session[1], mOBSHOPSelectUnfinishedBookingRequest[0], mOBSHOPAvailability[0], mOBShoppingCart[0], metaUserSessionSyncResponse[0], flightReservationResponse[0], shoppingResponse, airportDetailsList };

        }

        public Object[] metaSelectTripSet1_2()
        {
            var MetaSelectTripRequestJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\MetaSelectTripRequest.json");
            var MetaSelectTripRequest = JsonConvert.DeserializeObject<List<MetaSelectTripRequest>>(MetaSelectTripRequestJson);

            var sessionJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            var mOBSHOPSelectUnfinishedBookingRequestJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\MOBSHOPSelectUnfinishedBookingRequest.json");
            var mOBSHOPSelectUnfinishedBookingRequest = JsonConvert.DeserializeObject<List<MOBSHOPSelectUnfinishedBookingRequest>>(mOBSHOPSelectUnfinishedBookingRequestJson);

            var mOBSHOPAvailabilityJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityJson);

            var mOBShoppingCartJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\mOBShoppingCart.json");
            var mOBShoppingCart = JsonConvert.DeserializeObject<List<MOBShoppingCart>>(mOBShoppingCartJson);

            var metaUserSessionSyncResponseJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\metaUserSessionSyncResponse.json");
            var metaUserSessionSyncResponse = JsonConvert.DeserializeObject<List<MetaUserSessionSyncResponse>>(metaUserSessionSyncResponseJson);

            var flightReservationResponseJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\flightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<List<FlightReservationResponse>>(flightReservationResponseJson);

            var shoppingResponseJson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\shoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<ShoppingResponse>(shoppingResponseJson);

            var airportDetailsListjson = TestDataGenerator.GetFileContent(@"MetaSelectTripTestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<AirportDetailsList>(airportDetailsListjson);


            return new object[] { MetaSelectTripRequest[0], session[2], mOBSHOPSelectUnfinishedBookingRequest[0], mOBSHOPAvailability[0], mOBShoppingCart[0], metaUserSessionSyncResponse[0], flightReservationResponse[0], shoppingResponse, airportDetailsList };

        }

        public Object[] GetFareRulesForSelectedTripsRequestSet1()
        {
            var sessionJson = TestDataGenerator.GetFileContent(@"GetFareRulesForSelectedTrips\session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            var bookingPathReservationJson = TestDataGenerator.GetFileContent(@"GetFareRulesForSelectedTrips\bookingPathReservation.json");
            var bookingPathReservation = JsonConvert.DeserializeObject<List<Reservation>>(bookingPathReservationJson);

           // var fareRuleResponseJson = TestDataGenerator.GetFileContent(@"GetFareRulesForSelectedTrips\fareRuleResponse.json");
            //var fareRuleResponse = JsonConvert.DeserializeObject<List<string>>(fareRuleResponseJson);

            var getFareRulesRequestJson = TestDataGenerator.GetFileContent(@"GetFareRulesForSelectedTrips\getFareRulesRequest.json");
            var getFareRulesRequest = JsonConvert.DeserializeObject<List<Model.Shopping.GetFareRulesRequest>>(getFareRulesRequestJson);

            var fareRulesResponseJson = TestDataGenerator.GetFileContent(@"GetFareRulesForSelectedTrips\fareRulesResponse.json");
            var fareRulesResponse = JsonConvert.DeserializeObject<List<Model.Shopping.FareRulesResponse>>(fareRulesResponseJson);


            return new object[] { getFareRulesRequest[0], session[0], bookingPathReservation[0], fareRulesResponse[0] };
        }

        public Object[] GetFareRulesForSelectedTripsRequestSet2()
        {
            var sessionJson = TestDataGenerator.GetFileContent(@"GetFareRulesForSelectedTrips\session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            var bookingPathReservationJson = TestDataGenerator.GetFileContent(@"GetFareRulesForSelectedTrips\bookingPathReservation.json");
            var bookingPathReservation = JsonConvert.DeserializeObject<List<Reservation>>(bookingPathReservationJson);

            // var fareRuleResponseJson = TestDataGenerator.GetFileContent(@"GetFareRulesForSelectedTrips\fareRuleResponse.json");
            //var fareRuleResponse = JsonConvert.DeserializeObject<List<string>>(fareRuleResponseJson);

            var getFareRulesRequestJson = TestDataGenerator.GetFileContent(@"ReshopFlow\getFareRulesRequest.json");
            var getFareRulesRequest = JsonConvert.DeserializeObject<List<Model.Shopping.GetFareRulesRequest>>(getFareRulesRequestJson);

            var fareRulesResponseJson = TestDataGenerator.GetFileContent(@"ReshopFlow\fareRulesResponse.json");
            var fareRulesResponse = JsonConvert.DeserializeObject<List<Model.Shopping.FareRulesResponse>>(fareRulesResponseJson);


            return new object[] { getFareRulesRequest[0], session[0], bookingPathReservation[0], fareRulesResponse[0] };
        }

        public Object[] GetShareTripRequestSet1()
        {
            var selectTripJson = TestDataGenerator.GetFileContent(@"GetShareTrip\selectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<List<Model.Shopping.SelectTrip>>(selectTripJson);

            var fareRuleResponseJson = TestDataGenerator.GetFileContent(@"GetShareTrip\fareRuleResponse.json");
            // var fareRuleResponse = JsonConvert.DeserializeObject<List<string>>(fareRuleResponseJson);

            var sharedItineraryResponseJson = TestDataGenerator.GetFileContent(@"GetShareTrip\sharedItineraryResponse.json");

            var sharetripRequestJson = TestDataGenerator.GetFileContent(@"GetShareTrip\sharetripRequest.json");
            var sharetripRequest = JsonConvert.DeserializeObject<List<Model.Shopping.ShareTripRequest>>(sharetripRequestJson);

            return new object[] { sharetripRequest[0], fareRuleResponseJson, selectTrip[0], sharedItineraryResponseJson };
        }
    }
}
