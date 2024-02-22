using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Mobile.Model.ShopTrips;
using United.Mobile.Model.TripPlannerGetService;
using United.Service.Presentation.PersonalizationResponseModel;
using United.Services.Customer.Preferences.Common;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.FlightReservation;
using MOBSHOPUnfinishedBookingRequestBase = United.Mobile.Model.Shopping.UnfinishedBooking.MOBSHOPUnfinishedBookingRequestBase;


namespace United.Mobile.Shopping.UnfinishedBooking.Tests
{
    public class TestDataSet
    {
        public Object[] set1()
        {
            var mOBSHOPGetUnfinishedBookingsRequestJson = TestDataGenerator.GetFileContent("MOBSHOPGetUnfinishedBookingsRequest.json");
            var mOBSHOPGetUnfinishedBookingsRequest = JsonConvert.DeserializeObject<List<MOBSHOPGetUnfinishedBookingsRequest>>(mOBSHOPGetUnfinishedBookingsRequestJson);


            var sessionJson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);
            var contextualCommResponseJson = TestDataGenerator.GetFileContent("ContextualCommResponse.json");
            var contextualCommResponse = JsonConvert.DeserializeObject<List<ContextualCommResponse>>(contextualCommResponseJson);

            var mOBSHOPUnfinishedBookingJson = TestDataGenerator.GetFileContent("MOBSHOPUnfinishedBooking.json");
            var mOBSHOPUnfinishedBooking = JsonConvert.DeserializeObject<List<List<MOBSHOPUnfinishedBooking>>>(mOBSHOPUnfinishedBookingJson);

            var flightReservationResponseJson = TestDataGenerator.GetFileContent("FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<List<FlightReservationResponse>>(flightReservationResponseJson);

            return new object[] { mOBSHOPGetUnfinishedBookingsRequest[0], session[0], contextualCommResponse[0], mOBSHOPUnfinishedBooking[0], flightReservationResponse[0] };

        }

        public Object[] set2()
        {
            var mOBSHOPSelectUnfinishedBookingRequestJson = TestDataGenerator.GetFileContent("MOBSHOPSelectUnfinishedBookingRequest.json");
            var mOBSHOPSelectUnfinishedBookingRequest = JsonConvert.DeserializeObject<List<MOBSHOPSelectUnfinishedBookingRequest>>(mOBSHOPSelectUnfinishedBookingRequestJson);


            var sessionJson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            return new object[] { mOBSHOPSelectUnfinishedBookingRequest[0], session[0] };


        }

        public Object[] set3()
        {
           // var mOBSHOPUnfinishedBookingRequestBasejson = TestDataGenerator.GetFileContent("MOBSHOPUnfinishedBookingRequestBase.json");
           // var mOBSHOPUnfinishedBookingRequestBase = JsonConvert.DeserializeObject<List<MOBSHOPUnfinishedBookingRequestBase>>(mOBSHOPUnfinishedBookingRequestBasejson);

            var savedItineraryDataModeljson = TestDataGenerator.GetFileContent("SavedItineraryDataModel.json");
            var savedItineraryDataModel = JsonConvert.DeserializeObject<List<SavedItineraryDataModel>>(savedItineraryDataModeljson);

            var sessionjson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            return new object[] {  savedItineraryDataModel[0], session[0] };
        }

        public Object[] set4()
        {
            var mOBSHOPSelectUnfinishedBookingRequestJson = TestDataGenerator.GetFileContent("MOBSHOPSelectUnfinishedBookingRequest.json");
            var mOBSHOPSelectUnfinishedBookingRequest = JsonConvert.DeserializeObject<List<MOBSHOPSelectUnfinishedBookingRequest>>(mOBSHOPSelectUnfinishedBookingRequestJson);


            var sessionJson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            return new object[] { mOBSHOPSelectUnfinishedBookingRequest[1], session[0] };


        }
        public Object[] set7()
        {
            var mOBSHOPUnfinishedBookingRequestBaseJson = TestDataGenerator.GetFileContent("MOBSHOPUnfinishedBookingRequestBase.json");
            var mOBSHOPUnfinishedBookingRequestBase = JsonConvert.DeserializeObject<MOBSHOPUnfinishedBookingRequestBase>(mOBSHOPUnfinishedBookingRequestBaseJson);


            var sessionJson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            return new object[] { mOBSHOPUnfinishedBookingRequestBase, session[0] };
        }
        public Object[] set5()
        {
            var mOBSHOPSelectUnfinishedBookingRequestJson = TestDataGenerator.GetFileContent("MOBSHOPSelectUnfinishedBookingRequest.json");
            var mOBSHOPSelectUnfinishedBookingRequest = JsonConvert.DeserializeObject<List<MOBSHOPSelectUnfinishedBookingRequest>>(mOBSHOPSelectUnfinishedBookingRequestJson);

            var sessionJson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            var mOBShoppingCartJson = TestDataGenerator.GetFileContent("MOBShoppingCart.json");
            var mOBShoppingCart = JsonConvert.DeserializeObject<MOBShoppingCart>(mOBShoppingCartJson);

            var mOBSHOPSelectTripResponseJson = TestDataGenerator.GetFileContent("MOBSHOPSelectTripResponse.json");
            var mOBSHOPSelectTripResponse = JsonConvert.DeserializeObject<MOBSHOPSelectTripResponse>(mOBSHOPSelectTripResponseJson);

            var shopRequestJson = TestDataGenerator.GetFileContent("ShopRequest.json");
            var shopRequest = JsonConvert.DeserializeObject<List<ShopRequest>>(shopRequestJson);

            var flightReservationResponseJson = TestDataGenerator.GetFileContent("FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<List<FlightReservationResponse>>(flightReservationResponseJson);

            return new object[] { mOBSHOPSelectUnfinishedBookingRequest[0], session[0], mOBShoppingCart, mOBSHOPSelectTripResponse, shopRequest[0], flightReservationResponse[0] };


        }

        public Object[] set6()
        {
            var mOBSHOPUnfinishedBookingRequestBaseJson = TestDataGenerator.GetFileContent("MOBSHOPUnfinishedBookingRequestBase.json");
            var mOBSHOPUnfinishedBookingRequestBase = JsonConvert.DeserializeObject<MOBSHOPUnfinishedBookingRequestBase>(mOBSHOPUnfinishedBookingRequestBaseJson);


            var sessionJson = TestDataGenerator.GetFileContent("Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            return new object[] { mOBSHOPUnfinishedBookingRequestBase, session[0] };


        }

    }
}
