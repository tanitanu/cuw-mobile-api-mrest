using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Bundles;
using United.Services.FlightShopping.Common;
using United.Utility.Helper;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Mobile.Shopping.ShopBundles.Test
{
    public class TestDataSet

    {
        public Object[] set1()
        {

            var bookingbundlerequestjson = TestDataGenerator.GetFileContent("ShopBundlesRequest.json");
            var bookingbundlerequest = JsonConvert.DeserializeObject<List<BookingBundlesRequest>>(bookingbundlerequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"Set1\Session1.xml");

            var sessionjson = TestDataGenerator.GetFileContent(@"Set1\Session1.json");
            var session = JsonConvert.DeserializeObject<Session>(sessionjson);

            //var persistedReservation = TestDataGenerator.GetXmlData<Reservation>(@"Set1\Reservation1.xml");

            var persistedReservationJson = TestDataGenerator.GetFileContent(@"Set1\Reservation1.json");
            var persistedReservation = JsonConvert.DeserializeObject<Reservation>(persistedReservationJson);

            var persistedShopPindownRequestjson = TestDataGenerator.GetFileContent("persistedShopPindownRequest.json");
            var persistedShopPindownRequest = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopRequest>>(persistedShopPindownRequestjson);

            //var persistShopping = TestDataGenerator.GetXmlData<MOBShoppingCart>(@"Set1\MOBShoppingCart1.xml");

            var persistShoppingjson = TestDataGenerator.GetFileContent(@"Set1\MOBShoppingCart1.json");
            var persistShopping = JsonConvert.DeserializeObject<MOBShoppingCart>(persistShoppingjson);

           //  var response = TestDataGenerator.GetXmlData<List<BookingBundlesResponse>>(@"Set1\MOBBookingBundlesResponse1.xml");

            var responsejson = TestDataGenerator.GetFileContent(@"Set1\MOBBookingBundlesResponse1.json");
            var response = JsonConvert.DeserializeObject<BookingBundlesResponse>(responsejson);

            //var request = TestDataGenerator.GetXmlData<ShopSelectRequest>(@"Set1\MOBBookingBundlesResponse1.xml");

            var bundleResponseJson = TestDataGenerator.GetFileContent("bundleResponseJson.json");
            var bundleResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>>(bundleResponseJson);

            var ReservationJson = TestDataGenerator.GetFileContent("Reservation1.json");
            var Reservation = JsonConvert.DeserializeObject<Reservation>(ReservationJson);

            var responsebundlejson = TestDataGenerator.GetFileContent("responsebundle.json");
            var responsebundle = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>>(responsebundlejson);

            return new object[] { bookingbundlerequest[0], session, persistedReservation, persistedShopPindownRequest[0], persistShopping, response, bundleResponse[0], Reservation,responsebundle[0] };
        }
        public Object[] set2()
        {


            var bookingbundlerequestjson = TestDataGenerator.GetFileContent("ShopBundlesRequest.json");
            var bookingbundlerequest = JsonConvert.DeserializeObject<List<BookingBundlesRequest>>(bookingbundlerequestjson);

            var persistedReservation = TestDataGenerator.GetXmlData<Reservation>(@"Set2\Reservation2.xml");

            var session = TestDataGenerator.GetXmlData<Session>(@"Set2\Session2.xml");

            var persistedShopPindownRequestjson = TestDataGenerator.GetFileContent("persistedShopPindownRequest.json");
            var persistedShopPindownRequest = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopRequest>>(persistedShopPindownRequestjson);

            var persistShopping = TestDataGenerator.GetXmlData<MOBShoppingCart>(@"Set2\MOBShoppingCart2.xml");


           var response = TestDataGenerator.GetXmlData<BookingBundlesResponse>(@"Set2\MOBBookingBundlesResponse2.xml");

            //var responsejson = TestDataGenerator.GetFileContent(@"Set2\MOBBookingBundlesResponse2.json");
            //var response = JsonConvert.DeserializeObject<BookingBundlesResponse>(responsejson);

            //var request = TestDataGenerator.GetXmlData<ShopSelectRequest>(@"Set2\MOBBookingBundlesResponse2.xml");

            var bundleResponseJson = TestDataGenerator.GetFileContent("bundleResponseJson.json");
            var bundleResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>>(bundleResponseJson);

            var responsebundlejson = TestDataGenerator.GetFileContent("responsebundle.json");
            var responsebundle = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>>(responsebundlejson);

            return new object[] {bookingbundlerequest[0], session, persistedReservation, persistedShopPindownRequest[0], persistShopping, response, bundleResponse[1], responsebundle[1] };
        }

        public Object[] set3()
        {
            var bookingbundlerequestjson = TestDataGenerator.GetFileContent("ShopBundlesRequest.json");
            var bookingbundlerequest = JsonConvert.DeserializeObject<List<BookingBundlesRequest>>(bookingbundlerequestjson);


            var persistedReservation = TestDataGenerator.GetXmlData<Reservation>(@"Set2\Reservation2.xml");

            var session = TestDataGenerator.GetXmlData<Session>(@"Set2\Session2.xml");

            var persistedShopPindownRequestjson = TestDataGenerator.GetFileContent("persistedShopPindownRequest.json");
            var persistedShopPindownRequest = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopRequest>>(persistedShopPindownRequestjson);

            var persistShopping = TestDataGenerator.GetXmlData<MOBShoppingCart>(@"Set2\MOBShoppingCart2.xml");


            var response = TestDataGenerator.GetXmlData<BookingBundlesResponse>(@"Set2\MOBBookingBundlesResponse2.xml");

            //var request = TestDataGenerator.GetXmlData<ShopSelectRequest>(@"Set2\MOBBookingBundlesResponse2.xml");


            var bundleResponseJson = TestDataGenerator.GetFileContent("bundleResponseJson.json");
            var bundleResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>>(bundleResponseJson);

            var responsebundlejson = TestDataGenerator.GetFileContent("responsebundle.json");
            var responsebundle = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>>(responsebundlejson);

            return new object[] { bookingbundlerequest[0], session, persistedReservation, persistedShopPindownRequest[0], persistShopping, response, bundleResponse[1], responsebundle[2] };
        }

        public Object[] set4()
        {

            var bookingbundlerequestjson = TestDataGenerator.GetFileContent("ShopBundlesRequest.json");
            var bookingbundlerequest = JsonConvert.DeserializeObject<List<BookingBundlesRequest>>(bookingbundlerequestjson);


            var persistedReservation = TestDataGenerator.GetXmlData<Reservation>(@"Set1\Reservation1.xml");

            var session = TestDataGenerator.GetXmlData<Session>(@"Set1\Session1.xml");

            var persistedShopPindownRequestjson = TestDataGenerator.GetFileContent("persistedShopPindownRequest.json");
            var persistedShopPindownRequest = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopRequest>>(persistedShopPindownRequestjson);



            var persistShopping = TestDataGenerator.GetXmlData<MOBShoppingCart>(@"Set1\MOBShoppingCart1.xml");

            var response = TestDataGenerator.GetXmlData<BookingBundlesResponse>(@"Set1\MOBBookingBundlesResponse1.xml");


            //var request = TestDataGenerator.GetXmlData<ShopSelectRequest>(@"Set1\MOBBookingBundlesResponse1.xml");

            var bundleResponseJson = TestDataGenerator.GetFileContent("bundleResponseJson.json");
            var bundleResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>>(bundleResponseJson);

            var responsebundlejson = TestDataGenerator.GetFileContent("responsebundle.json");
            var responsebundle = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>>(responsebundlejson);

            return new object[] { bookingbundlerequest[0], session, persistedReservation, persistedShopPindownRequest[0], persistShopping, response, bundleResponse[1], responsebundle[0] };
        }


    }
}
