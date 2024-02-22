using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using ProfileResponse = United.Mobile.Model.Shopping.ProfileResponse;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Services.ShopSeats.Test
{
    public class TestDataSet
    {
        public object[] set1()
        {

            //var reservation = TestDataGenerator.GetFileContent(@"set1\SelectSeats\reservation.json");

           var selectSeatsRequestJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\selectSeatsRequest.json");
            var selectSeatsRequest = JsonConvert.DeserializeObject<List<SelectSeatsRequest>>(selectSeatsRequestJson);

            var reservationJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<MOBSHOPReservation>>(reservationJson);

            var persistSelectSeatsResponseJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\persistSelectSeatsResponse.json");
            var persistSelectSeatsResponse = JsonConvert.DeserializeObject<List<SelectSeats>>(persistSelectSeatsResponseJson);

            var persistShoppingCartJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\persistShoppingCart.json");
            var persistShoppingCart = JsonConvert.DeserializeObject<List<MOBShoppingCart>>(persistShoppingCartJson);

            var persistedReservationJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\persistedReservation.json");
            var persistedReservation = JsonConvert.DeserializeObject<List<Reservation>>(persistedReservationJson);

           var sessionJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\session.json");
           var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            var objUASubscriptionsJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\objUASubscriptions.json");
            var objUASubscriptions = JsonConvert.DeserializeObject<List<MOBUASubscriptions>>(objUASubscriptionsJson);

            var selectSeatsResponseJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\SelectSeatsResponse.json");
            var selectSeatsResponse = JsonConvert.DeserializeObject<SelectSeatsResponse>(selectSeatsResponseJson);


            // var persistSelectSeatsResponse = TestDataGenerator.GetXmlData<List<SelectSeats>>(@"set1\SelectSeats\persistSelectSeatsResponse.xml");
            //var objUASubscriptions = TestDataGenerator.GetXmlData<List<MOBUASubscriptions>>(@"set1\SelectSeats\objUASubscriptions.xml");
            // var persistedReservation = TestDataGenerator.GetXmlData<List<UAWSMPTravelCertificateService.ETCServiceSoap.Reservation>>(@"set1\SelectSeats\persistedReservation.xml");
            //  var session = TestDataGenerator.GetXmlData<List<Session>>(@"set1\SelectSeats\session.xml");
            // var persistShoppingCart = TestDataGenerator.GetXmlData<List<MOBShoppingCart>>(@"set1\SelectSeats\persistShoppingCart.xml");
            //var reservation = TestDataGenerator.GetXmlData<List<MOBSHOPReservation>>(@"set1\SelectSeats\reservation.xml");


            return new object[] {selectSeatsRequest[0], reservation[0], persistSelectSeatsResponse[0],persistShoppingCart[0] , persistedReservation[0], session[0], objUASubscriptions[0], selectSeatsResponse };
        }

        public object[] set2()
        {

            var selectSeatsRequestJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\selectSeatsRequest.json");
            var selectSeatsRequest = JsonConvert.DeserializeObject<List<SelectSeatsRequest>>(selectSeatsRequestJson);

            var reservationJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<MOBSHOPReservation>>(reservationJson);

            var persistSelectSeatsResponseJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\persistSelectSeatsResponse.json");
            var persistSelectSeatsResponse = JsonConvert.DeserializeObject<List<SelectSeats>>(persistSelectSeatsResponseJson);

            var persistShoppingCartJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\persistShoppingCart.json");
            var persistShoppingCart = JsonConvert.DeserializeObject<List<MOBShoppingCart>>(persistShoppingCartJson);

            var persistedReservationJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\persistedReservation.json");
            var persistedReservation = JsonConvert.DeserializeObject<List<Reservation>>(persistedReservationJson);

            var sessionJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            var objUASubscriptionsJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\objUASubscriptions.json");
            var objUASubscriptions = JsonConvert.DeserializeObject<List<MOBUASubscriptions>>(objUASubscriptionsJson);

            var selectSeatsResponseJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\SelectSeatsResponse.json");
            var selectSeatsResponse = JsonConvert.DeserializeObject<SelectSeatsResponse>(selectSeatsResponseJson);

            



            return new object[] { selectSeatsRequest[0], reservation[0], persistSelectSeatsResponse[0], persistShoppingCart[0], persistedReservation[1], session[0], objUASubscriptions[0], selectSeatsResponse };
        }

        public object[] set3()
        {

            var selectSeatsRequestJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\selectSeatsRequest.json");
            var selectSeatsRequest = JsonConvert.DeserializeObject<List<SelectSeatsRequest>>(selectSeatsRequestJson);

            var reservationJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<MOBSHOPReservation>>(reservationJson);

            var persistSelectSeatsResponseJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\persistSelectSeatsResponse.json");
            var persistSelectSeatsResponse = JsonConvert.DeserializeObject<List<SelectSeats>>(persistSelectSeatsResponseJson);

            var persistShoppingCartJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\persistShoppingCart.json");
            var persistShoppingCart = JsonConvert.DeserializeObject<List<MOBShoppingCart>>(persistShoppingCartJson);

            var persistedReservationJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\persistedReservation.json");
            var persistedReservation = JsonConvert.DeserializeObject<List<Reservation>>(persistedReservationJson);

            var sessionJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            var objUASubscriptionsJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\objUASubscriptions.json");
            var objUASubscriptions = JsonConvert.DeserializeObject<List<MOBUASubscriptions>>(objUASubscriptionsJson);

            var selectSeatsResponseJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\SelectSeatsResponse.json");
            var selectSeatsResponse = JsonConvert.DeserializeObject<SelectSeatsResponse>(selectSeatsResponseJson);





            return new object[] { selectSeatsRequest[0], reservation[0], persistSelectSeatsResponse[0], persistShoppingCart[0], persistedReservation[1], session[0], objUASubscriptions[0], selectSeatsResponse };
        }

        public object[] set1_1()
        {

            var selectSeatsRequestJson = TestDataGenerator.GetFileContent(@"ReshopFlow\selectSeatsRequest.json");
            var selectSeatsRequest = JsonConvert.DeserializeObject<List<SelectSeatsRequest>>(selectSeatsRequestJson);

            var reservationJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<MOBSHOPReservation>>(reservationJson);

            var persistSelectSeatsResponseJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\persistSelectSeatsResponse.json");
            var persistSelectSeatsResponse = JsonConvert.DeserializeObject<List<SelectSeats>>(persistSelectSeatsResponseJson);

            var persistShoppingCartJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\persistShoppingCart.json");
            var persistShoppingCart = JsonConvert.DeserializeObject<List<MOBShoppingCart>>(persistShoppingCartJson);

            var persistedReservationJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\persistedReservation.json");
            var persistedReservation = JsonConvert.DeserializeObject<List<Reservation>>(persistedReservationJson);

            var sessionJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            var objUASubscriptionsJson = TestDataGenerator.GetFileContent(@"set1\SelectSeats\objUASubscriptions.json");
            var objUASubscriptions = JsonConvert.DeserializeObject<List<MOBUASubscriptions>>(objUASubscriptionsJson);

            var selectSeatsResponseJson = TestDataGenerator.GetFileContent(@"ReshopFlow\SelectSeatsResponse.json");
            var selectSeatsResponse = JsonConvert.DeserializeObject<SelectSeatsResponse>(selectSeatsResponseJson);


            return new object[] { selectSeatsRequest[0], reservation[0], persistSelectSeatsResponse[0], persistShoppingCart[0], persistedReservation[0], session[0], objUASubscriptions[0], selectSeatsResponse };
        }
    }
}
