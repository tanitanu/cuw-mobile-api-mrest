using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using United.Definition;
using United.Mobile.Model.Catalog;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Bundles;
using United.Mobile.Model.Shopping.Common;
using United.Service.Presentation.PersonalizationResponseModel;
using United.Service.Presentation.ReferenceDataResponseModel;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Services.FlightShopping.Common.LMX;
using static United.Common.Helper.Shopping.ShoppingUtility;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using ShopResponse = United.Mobile.Model.Shopping.ShopResponse;

namespace United.Mobile.Shopping.Shopping.Test
{
    public class ShoppingTestDataSet
    {

        public Object[] set1()
        {

            var mOBSHOPShopRequestjson = ShoppingTestDataGenerator.GetFileContent("mOBSHOPShopRequest.json");
            var mOBSHOPShopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(mOBSHOPShopRequestjson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponsejson);

            var mOBOptimizelyQMDatajson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\MOBOptimizelyQMData.json");
            var mOBOptimizelyQMData = JsonConvert.DeserializeObject<List<MOBOptimizelyQMData>>(mOBOptimizelyQMDatajson);

            var mOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityjson);

            return new object[] { mOBSHOPShopRequest[0], session[0], shoppingResponse[0], mOBOptimizelyQMData, mOBSHOPAvailability[0] };
        }

        public Object[] set1_1()
        {

            var mOBSHOPShopRequestjson = ShoppingTestDataGenerator.GetFileContent("mOBSHOPShopRequest.json");
            var mOBSHOPShopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(mOBSHOPShopRequestjson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponsejson);

            var mOBOptimizelyQMDatajson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\MOBOptimizelyQMData.json");
            var mOBOptimizelyQMData = JsonConvert.DeserializeObject<List<MOBOptimizelyQMData>>(mOBOptimizelyQMDatajson);

            var mOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityjson);

            return new object[] { mOBSHOPShopRequest[0], session[0], shoppingResponse[0], mOBOptimizelyQMData, mOBSHOPAvailability[0] };
        }

        public Object[] set1_1_1()
        {

            var mOBSHOPShopRequestjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\mOBSHOPShopRequest1.json");
            var mOBSHOPShopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(mOBSHOPShopRequestjson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponsejson);

            var mOBOptimizelyQMDatajson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\MOBOptimizelyQMData.json");
            var mOBOptimizelyQMData = JsonConvert.DeserializeObject<List<MOBOptimizelyQMData>>(mOBOptimizelyQMDatajson);

            var mOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityjson);

            return new object[] { mOBSHOPShopRequest[0], session[0], shoppingResponse[0], mOBOptimizelyQMData, mOBSHOPAvailability[0] };
        }

        public Object[] set1_1_2()
        {

            var mOBSHOPShopRequestjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\mOBSHOPShopRequest1.json");
            var mOBSHOPShopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(mOBSHOPShopRequestjson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponsejson);

            var mOBOptimizelyQMDatajson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\MOBOptimizelyQMData.json");
            var mOBOptimizelyQMData = JsonConvert.DeserializeObject<List<MOBOptimizelyQMData>>(mOBOptimizelyQMDatajson);

            var mOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityjson);

            return new object[] { mOBSHOPShopRequest[0], session[0], shoppingResponse[0], mOBOptimizelyQMData, mOBSHOPAvailability[0] };
        }

        public Object[] set1_1_3()
        {

            var mOBSHOPShopRequestjson = ShoppingTestDataGenerator.GetFileContent("mOBSHOPShopRequest.json");
            var mOBSHOPShopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(mOBSHOPShopRequestjson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponsejson);

            var mOBOptimizelyQMDatajson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\MOBOptimizelyQMData.json");
            var mOBOptimizelyQMData = JsonConvert.DeserializeObject<List<MOBOptimizelyQMData>>(mOBOptimizelyQMDatajson);

            var shopResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<ShopResponse>>(shopResponsejson);

            return new object[] { mOBSHOPShopRequest[0], session[0], shoppingResponse[0], mOBOptimizelyQMData, shopResponse[0] };
        }

        public Object[] set1_1_4()
        {

            var mOBSHOPShopRequestjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\mOBSHOPShopRequest1.json");
            var mOBSHOPShopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(mOBSHOPShopRequestjson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponsejson);

            var mOBOptimizelyQMDatajson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\MOBOptimizelyQMData.json");
            var mOBOptimizelyQMData = JsonConvert.DeserializeObject<List<MOBOptimizelyQMData>>(mOBOptimizelyQMDatajson);

            var mOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityjson);

            var shopResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<ShopResponse>>(shopResponsejson);

            return new object[] { mOBSHOPShopRequest[0], session[3], shoppingResponse[0], mOBOptimizelyQMData, mOBSHOPAvailability[0], shopResponse[0] };
        }

        public Object[] set2()
        {

            var shopOrganizeResultsReqeustjson = ShoppingTestDataGenerator.GetFileContent("ShopOrganizeResultsReqeust.json");
            var shopOrganizeResultsReqeust = JsonConvert.DeserializeObject<List<ShopOrganizeResultsReqeust>>(shopOrganizeResultsReqeustjson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);
            
            var cSLSelectTripjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\CSLSelectTrip.json");
            var cSLSelectTrip = JsonConvert.DeserializeObject<List<CSLSelectTrip>>(cSLSelectTripjson);

            var selectTripjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<SelectTrip>(selectTripjson);


            var mOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityjson);


            return new object[] { shopOrganizeResultsReqeust[1], session[1], cSLSelectTrip[0], selectTrip, mOBSHOPAvailability[0] };
        }
        public Object[] set2_1()
        {

            var shopOrganizeResultsReqeustjson = ShoppingTestDataGenerator.GetFileContent("ShopOrganizeResultsReqeust.json");
            var shopOrganizeResultsReqeust = JsonConvert.DeserializeObject<List<ShopOrganizeResultsReqeust>>(shopOrganizeResultsReqeustjson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var cSLSelectTripjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\CSLSelectTrip.json");
            var cSLSelectTrip = JsonConvert.DeserializeObject<List<CSLSelectTrip>>(cSLSelectTripjson);

            var selectTripjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<SelectTrip>(selectTripjson);

            var mOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityjson);
           
            return new object[] { shopOrganizeResultsReqeust[0], session[1], cSLSelectTrip[0], selectTrip, mOBSHOPAvailability[0] };
        }

        public Object[] set2_2()
        {

            var shopOrganizeResultsReqeustjson = ShoppingTestDataGenerator.GetFileContent("ShopOrganizeResultsReqeust.json");
            var shopOrganizeResultsReqeust = JsonConvert.DeserializeObject<List<ShopOrganizeResultsReqeust>>(shopOrganizeResultsReqeustjson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var cSLSelectTripjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\CSLSelectTrip.json");
            var cSLSelectTrip = JsonConvert.DeserializeObject<List<CSLSelectTrip>>(cSLSelectTripjson);

            var selectTripjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<SelectTrip>(selectTripjson);

            var mOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityjson);

            return new object[] { shopOrganizeResultsReqeust[2], session[1], cSLSelectTrip[0], selectTrip, mOBSHOPAvailability[0] };
        }

        public Object[] set2_3()
        {

            var shopOrganizeResultsReqeustjson = ShoppingTestDataGenerator.GetFileContent("ShopOrganizeResultsReqeust.json");
            var shopOrganizeResultsReqeust = JsonConvert.DeserializeObject<List<ShopOrganizeResultsReqeust>>(shopOrganizeResultsReqeustjson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var cSLSelectTripjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\CSLSelectTrip.json");
            var cSLSelectTrip = JsonConvert.DeserializeObject<List<CSLSelectTrip>>(cSLSelectTripjson);

            var mOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityjson);

            var selectTripjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<SelectTrip>(selectTripjson);


            return new object[] { shopOrganizeResultsReqeust[3], session[1], cSLSelectTrip[0], mOBSHOPAvailability[0], selectTrip };
        }

        public Object[] set2_4()
        {

            var shopOrganizeResultsReqeustjson = ShoppingTestDataGenerator.GetFileContent(@"ReshopFlow\ShopOrganizeResultsReqeust.json");
            var shopOrganizeResultsReqeust = JsonConvert.DeserializeObject<List<ShopOrganizeResultsReqeust>>(shopOrganizeResultsReqeustjson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var cSLSelectTripjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\CSLSelectTrip.json");
            var cSLSelectTrip = JsonConvert.DeserializeObject<List<CSLSelectTrip>>(cSLSelectTripjson);

            var selectTripjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<SelectTrip>(selectTripjson);

            var mOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityjson);


            return new object[] { shopOrganizeResultsReqeust[0], session[1], cSLSelectTrip[0], selectTrip, mOBSHOPAvailability[0] };
        }

        public Object[] set3()
        {
            var mOBSHOPShopRequestjson = ShoppingTestDataGenerator.GetFileContent("mOBSHOPShopRequest.json");
            var mOBSHOPShopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(mOBSHOPShopRequestjson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"OrganizeShopResults_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var cLBOptOutRequestjson = ShoppingTestDataGenerator.GetFileContent(@"ShopCLBOptOut_TestData\ShareTripRequest.json");
            var cLBOptOutRequest = JsonConvert.DeserializeObject<List<CLBOptOutRequest>>(cLBOptOutRequestjson);

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponsejson);

            var mOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"ShopCLBOptOut_TestData\MOBSHOPAvailability.json");
            var mOBSHOPAvailability = JsonConvert.DeserializeObject<List<MOBSHOPAvailability>>(mOBSHOPAvailabilityjson);

            return new object[] { cLBOptOutRequest[0], shoppingResponse[0], mOBSHOPShopRequest[0], session[0], mOBSHOPAvailability[0]};
        }

        public Object[] set4()
        {

            var shareTripRequestjson = ShoppingTestDataGenerator.GetFileContent(@"GetShopRequest_TestData\ShareTripRequest.json");
            var shareTripRequest = JsonConvert.DeserializeObject<ShareTripRequest>(shareTripRequestjson);

            var shopRequestjson = ShoppingTestDataGenerator.GetFileContent(@"GetShopRequest_TestData\ShopRequest.json");
            var shopRequest = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopRequest>>(shopRequestjson);



            return new object[] { shareTripRequest, shopRequest[0] };
        }

        public Object[] set5()
        {
            var selectTripRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<SelectTripRequest>>(selectTripRequestjson);

            // var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<SelectTrip>(selectTripjson);



            var shopResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var MOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\MOBSHOPAvailability.json");
            var MOBSHOPAvailability = JsonConvert.DeserializeObject<MOBSHOPAvailability>(MOBSHOPAvailabilityjson);

            var shopAmenitiesRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopAmenitiesRequest.json");
            var shopAmenitiesRequest = JsonConvert.DeserializeObject<ShopAmenitiesRequest>(shopAmenitiesRequestjson);

            var selectTripResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTripResponse.json");
            var selectTripResponse = JsonConvert.DeserializeObject<List<SelectTripResponse>>(selectTripResponsejson);

            var updateAmenitiesIndicatorsRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\UpdateAmenitiesIndicatorsRequest.json");
            var updateAmenitiesIndicatorsRequest = JsonConvert.DeserializeObject<List<UpdateAmenitiesIndicatorsRequest>>(updateAmenitiesIndicatorsRequestjson);

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

            return new object[] { selectTripRequest[0], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[0], shopResponse[2], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment, MOBSHOPAvailability, shopAmenitiesRequest, selectTripResponse[0], updateAmenitiesIndicatorsRequest[0] };
        }

        public Object[] set6()
        {
            var selectTripRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest1.json");
            var selectTripRequest1 = JsonConvert.DeserializeObject<List<SelectTripRequest>>(selectTripRequestjson);

            // var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<SelectTrip>(selectTripjson);



            var shopResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var MOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\MOBSHOPAvailability.json");
            var MOBSHOPAvailability = JsonConvert.DeserializeObject<MOBSHOPAvailability>(MOBSHOPAvailabilityjson);

            var shopAmenitiesRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopAmenitiesRequest.json");
            var shopAmenitiesRequest = JsonConvert.DeserializeObject<ShopAmenitiesRequest>(shopAmenitiesRequestjson);

            var selectTripResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTripResponse.json");
            var selectTripResponse = JsonConvert.DeserializeObject<List<SelectTripResponse>>(selectTripResponsejson);

            var updateAmenitiesIndicatorsRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\UpdateAmenitiesIndicatorsRequest.json");
            var updateAmenitiesIndicatorsRequest = JsonConvert.DeserializeObject<List<UpdateAmenitiesIndicatorsRequest>>(updateAmenitiesIndicatorsRequestjson);

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

            return new object[] { selectTripRequest1[0], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[0], shopResponse[2], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment, MOBSHOPAvailability, shopAmenitiesRequest, selectTripResponse[0], updateAmenitiesIndicatorsRequest[0] };
        }
        public Object[] set7()
        {

            var selectTripRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<SelectTripRequest>>(selectTripRequestjson);

            // var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session1.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<SelectTrip>(selectTripjson);



            var shopResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var MOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\MOBSHOPAvailability1.json");
            var MOBSHOPAvailability = JsonConvert.DeserializeObject<MOBSHOPAvailability>(MOBSHOPAvailabilityjson);

            var shopAmenitiesRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopAmenitiesRequest.json");
            var shopAmenitiesRequest = JsonConvert.DeserializeObject<ShopAmenitiesRequest>(shopAmenitiesRequestjson);

            var selectTripResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTripResponse.json");
            var selectTripResponse = JsonConvert.DeserializeObject<List<SelectTripResponse>>(selectTripResponsejson);

            var updateAmenitiesIndicatorsRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\UpdateAmenitiesIndicatorsRequest.json");
            var updateAmenitiesIndicatorsRequest = JsonConvert.DeserializeObject<List<UpdateAmenitiesIndicatorsRequest>>(updateAmenitiesIndicatorsRequestjson);

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

            return new object[] { selectTripRequest[0], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[1], shopResponse[1], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment, MOBSHOPAvailability, shopAmenitiesRequest, selectTripResponse[0], updateAmenitiesIndicatorsRequest[0] };

        }

        public Object[] set8()
        {

            var selectTripRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<SelectTripRequest>>(selectTripRequestjson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session1.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            var selectTripjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<SelectTrip>(selectTripjson);



            var shopResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            var reservationjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            var cSLShopRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var MOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\MOBSHOPAvailability1.json");
            var MOBSHOPAvailability = JsonConvert.DeserializeObject<MOBSHOPAvailability>(MOBSHOPAvailabilityjson);

            var shopAmenitiesRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopAmenitiesRequest.json");
            var shopAmenitiesRequest = JsonConvert.DeserializeObject<ShopAmenitiesRequest>(shopAmenitiesRequestjson);

            var selectTripResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTripResponse.json");
            var selectTripResponse = JsonConvert.DeserializeObject<List<SelectTripResponse>>(selectTripResponsejson);

            var updateAmenitiesIndicatorsRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\UpdateAmenitiesIndicatorsRequest.json");
            var updateAmenitiesIndicatorsRequest = JsonConvert.DeserializeObject<List<UpdateAmenitiesIndicatorsRequest>>(updateAmenitiesIndicatorsRequestjson);

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

            return new object[] { selectTripRequest[0], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[1], shopResponse[2], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment, MOBSHOPAvailability, shopAmenitiesRequest, selectTripResponse[0], updateAmenitiesIndicatorsRequest[0] };

        }
        public Object[] set9()
        {

            var selectTripRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<SelectTripRequest>>(selectTripRequestjson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session1.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            var selectTripjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<SelectTrip>(selectTripjson);



            var shopResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            var reservationjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            var cSLShopRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var MOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\MOBSHOPAvailability1.json");
            var MOBSHOPAvailability = JsonConvert.DeserializeObject<MOBSHOPAvailability>(MOBSHOPAvailabilityjson);

            var shopAmenitiesRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopAmenitiesRequest.json");
            var shopAmenitiesRequest = JsonConvert.DeserializeObject<ShopAmenitiesRequest>(shopAmenitiesRequestjson);

            var selectTripResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTripResponse.json");
            var selectTripResponse = JsonConvert.DeserializeObject<List<SelectTripResponse>>(selectTripResponsejson);

            var updateAmenitiesIndicatorsRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\UpdateAmenitiesIndicatorsRequest.json");
            var updateAmenitiesIndicatorsRequest = JsonConvert.DeserializeObject<List<UpdateAmenitiesIndicatorsRequest>>(updateAmenitiesIndicatorsRequestjson);

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

            return new object[] { selectTripRequest[0], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[0], shopResponse[2], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment, MOBSHOPAvailability, shopAmenitiesRequest, selectTripResponse[0], updateAmenitiesIndicatorsRequest[0] };

        }
        //public Object[] set13()
        // {
        //     var selectTripRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
        //     var selectTripRequest = JsonConvert.DeserializeObject<List<SelectTripRequest>>(selectTripRequestjson);

        //     //var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

        //     var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
        //     var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

        //     //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

        //     var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
        //     var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

        //     // var latestShopAvailabilityResponse = ShoppingTestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

        //     var selectTripjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
        //     var selectTrip = JsonConvert.DeserializeObject<SelectTrip>(selectTripjson);



        //     var shopResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
        //     var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


        //     // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


        //     var reservationjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
        //     var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

        //     //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

        //     var cSLShopRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
        //     var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

        //     var displayAirportDetailsjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
        //     var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

        //     var airportDetailsListjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
        //     var airportDetailsList = JsonConvert.DeserializeObject<AirportDetailsList>(airportDetailsListjson);

        //     var flightReservationResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
        //     var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


        //     var shopBookingDetailsResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
        //     var shopBookingDetailsResponse = JsonConvert.DeserializeObject<ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

        //     var multiCallResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
        //     var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

        //     var reservationFlightSegmentjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
        //     var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

        //     var reservationDetailjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationDetail.json");
        //     var reservationDetail = JsonConvert.DeserializeObject<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(reservationDetailjson);

        //     var MOBSHOPAvailabilityjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\MOBSHOPAvailability.json");
        //     var MOBSHOPAvailability = JsonConvert.DeserializeObject<MOBSHOPAvailability>(MOBSHOPAvailabilityjson);


        //     var features = new Collection<Service.Presentation.ProductModel.ProductFeature>();
        //     var productFeature = new United.Service.Presentation.ProductModel.ProductFeature()
        //     {
        //         Name = ""
        //     };
        //     features.Add(productFeature);
        //     var products = new United.Service.Presentation.ProductModel.SubProduct()
        //     {
        //         Features = features
        //     };
        //     var response = JsonConvert.SerializeObject(products);

        //     return new object[] { selectTripRequest[2], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[3], shopResponse[0], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment, reservationDetail , MOBSHOPAvailability };
        // }
        public Object[] set14()
        {
            var selectTripRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\selectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<SelectTripRequest>>(selectTripRequestjson);

            //var session = TestDataGenerator.GetXmlData<Session>(@"SelectTrip_TestData\Session.xml");

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var shoppingResponse = TestDataGenerator.GetXmlData<Model.Shopping.ShoppingResponse>(@"Set1\ShoppingResponse.xml");

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            // var latestShopAvailabilityResponse = TestDataGenerator.GetXmlData<LatestShopAvailabilityResponse>(@"Set1\LatestShopAvailabilityResponse.xml");

            var selectTripjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<SelectTrip>(selectTripjson);



            var shopResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);


            // var selectTrip = TestDataGenerator.GetXmlData<SelectTrip>(@"Set1\SelectTrip.xml");


            var reservationjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<Reservation>(reservationjson);

            //var cSLShopRequest = TestDataGenerator.GetXmlData<CSLShopRequest>(@"Set1\CSLShopRequest.xml");

            var cSLShopRequestjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<CSLShopRequest>(cSLShopRequestjson);

            var displayAirportDetailsjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);

            var airportDetailsListjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\AirportDetailsList.json");
            var airportDetailsList = JsonConvert.DeserializeObject<AirportDetailsList>(airportDetailsListjson);

            var flightReservationResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\FlightReservationResponse.json");
            var flightReservationResponse = JsonConvert.DeserializeObject<FlightReservationResponse>(flightReservationResponsejson);


            var shopBookingDetailsResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ShopBookingDetailsResponse.json");
            var shopBookingDetailsResponse = JsonConvert.DeserializeObject<ShopBookingDetailsResponse>(shopBookingDetailsResponsejson);

            var multiCallResponsejson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\MultiCallResponse.json");
            var multiCallResponse = JsonConvert.DeserializeObject<MultiCallResponse>(multiCallResponsejson);

            var reservationFlightSegmentjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationFlightSegment.json");
            var reservationFlightSegment = JsonConvert.DeserializeObject<List<ReservationFlightSegment>>(reservationFlightSegmentjson);

            var reservationDetailjson = ShoppingTestDataGenerator.GetFileContent(@"SelectTrip_TestData\ReservationDetail.json");
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

            return new object[] { selectTripRequest[3], selectTrip, shoppingResponse[0], reservation, cSLShopRequest, session[4], shopResponse[0], displayAirportDetails, airportDetailsList, flightReservationResponse, shopBookingDetailsResponse, multiCallResponse, reservationFlightSegment, reservationDetail };
        }

        public Object[] set15()
        {
            var ChasePromoRedirectRequestJson = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\ChasePromoRedirectRequest.json");
            var ChasePromoRedirectRequest = JsonConvert.DeserializeObject<List<ChasePromoRedirectRequest>>(ChasePromoRedirectRequestJson);

           // var ChasePromoRedirectResponseJson = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\ChasePromoRedirectResponse.json");
            //var ChasePromoRedirectResponse = JsonConvert.DeserializeObject<List<ChasePromoRedirectResponse>>(ChasePromoRedirectResponseJson);

            var cceResponseJson = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\cceResponse.json");
             var cceResponse = JsonConvert.DeserializeObject<CCEPromo>(cceResponseJson);

            //var shop = TestDataGenerator.GetXmlData< CCEPromo>(@"ShopProducts_TestData\Set1\ShoppingResponse.xml");

            //var session = ShoppingTestDataGenerator.GetXmlData<Session>(@"ShopProducts_TestData\Set1\Session1.xml");
            var sessionJson = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\Set2\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);



            return new object[] { session[0] , ChasePromoRedirectRequest[0] , cceResponse };


        }
        public Object[] set16()
        {
            var GetProductInfoForFSRDRequestJson = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\GetProductInfoForFSRDRequest.json");
            var GetProductInfoForFSRDRequest = JsonConvert.DeserializeObject<List<GetProductInfoForFSRDRequest>>(GetProductInfoForFSRDRequestJson);

            var GetProductInfoForFSRDResponseJson = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\GetProductInfoForFSRDResponse.json");
            var GetProductInfoForFSRDResponse = JsonConvert.DeserializeObject<List<ChasePromoRedirectResponse>>(GetProductInfoForFSRDResponseJson);

            var latestShopAvailResponseJson = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\LatestShopAvailabilityResponse.json");
            var latestShopAvailResponse = JsonConvert.DeserializeObject<LatestShopAvailabilityResponse>(latestShopAvailResponseJson);

            var jsonResponse = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\jsonresponse.json");

            var ccePromoresponse = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\cceResponse.json");

            List<MOBLegalDocument> legalDocuments = new List<MOBLegalDocument>();
            MOBLegalDocument mOBLegalDocument = new MOBLegalDocument()
            {
                LegalDocument = "AWE2|true|true|65TR|XYZ|ABC",
                Title = "1"
            };
            legalDocuments.Add(mOBLegalDocument);

            //var session = ShoppingTestDataGenerator.GetXmlData<Session>(@"ShopProducts_TestData\Set2\Session2.xml");

            var sessionJson = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\Set2\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

            //var shoppingResponse = ShoppingTestDataGenerator.GetXmlData<ShoppingResponse>(@"ShopProducts_TestData\Set1\ShoppingResponse.xml");

            var shoppingResponseJson = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\Set2\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponseJson);

            return new object[] { session[0], GetProductInfoForFSRDRequest[0], jsonResponse, ccePromoresponse, latestShopAvailResponse, shoppingResponse[0], legalDocuments };
        }

        public Object[] set17()
        {
            var GetProductInfoForFSRDRequestJson = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\GetProductInfoForFSRDRequest.json");
            var GetProductInfoForFSRDRequest = JsonConvert.DeserializeObject<List<GetProductInfoForFSRDRequest>>(GetProductInfoForFSRDRequestJson);

            var GetProductInfoForFSRDResponseJson = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\GetProductInfoForFSRDResponse.json");
            var GetProductInfoForFSRDResponse = JsonConvert.DeserializeObject<List<ChasePromoRedirectResponse>>(GetProductInfoForFSRDResponseJson);

            var latestShopAvailResponseJson = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\LatestShopAvailabilityResponse.json");
            var latestShopAvailResponse = JsonConvert.DeserializeObject<LatestShopAvailabilityResponse>(latestShopAvailResponseJson);

            var jsonResponse = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\jsonresponse.json");
            var ccePromoresponse = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\cceResponse.json");

            // var session = ShoppingTestDataGenerator.GetXmlData<Session>(@"ShopProducts_TestData\Set2\Session2.xml");

            var sessionJson = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\Set2\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionJson);

           // var shoppingResponse = ShoppingTestDataGenerator.GetXmlData<ShoppingResponse>(@"ShopProducts_TestData\Set2\ShoppingResponse.xml");

            var shoppingResponseJson = ShoppingTestDataGenerator.GetFileContent(@"ShopProducts_TestData\Set2\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponseJson);

            List<MOBLegalDocument> legalDocuments = new List<MOBLegalDocument>();
            MOBLegalDocument mOBLegalDocument = new MOBLegalDocument()
            {
                LegalDocument = "AWE2|true|true|65TR|XYZ|ABC",
                Title = "1"
            };
            legalDocuments.Add(mOBLegalDocument);



            return new object[] { session[0], GetProductInfoForFSRDRequest[0], jsonResponse, ccePromoresponse, latestShopAvailResponse, shoppingResponse[0], legalDocuments };
        }

        public Object[] set18()
        {

            var mOBSHOPTripPlanRequestjson = ShoppingTestDataGenerator.GetFileContent("MOBSHOPTripPlanRequest.json");
            var mOBSHOPTripPlanRequest = JsonConvert.DeserializeObject<List<MOBSHOPTripPlanRequest>>(mOBSHOPTripPlanRequestjson);

            var tripPlanCCEResponsejson = ShoppingTestDataGenerator.GetFileContent("TripPlanCCEResponse.json");
            var tripPlanCCEResponse = JsonConvert.DeserializeObject<TripPlanCCEResponse>(tripPlanCCEResponsejson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var shopResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<ShopResponse>>(shopResponsejson);

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponsejson);


            return new object[] { mOBSHOPTripPlanRequest[0], tripPlanCCEResponse, session[0], shopResponse[0], shoppingResponse[0] };
        }

        public Object[] set18_1()
        {

            var mOBSHOPTripPlanRequestjson = ShoppingTestDataGenerator.GetFileContent("MOBSHOPTripPlanRequest.json");
            var mOBSHOPTripPlanRequest = JsonConvert.DeserializeObject<List<MOBSHOPTripPlanRequest>>(mOBSHOPTripPlanRequestjson);

            var tripPlanCCEResponsejson = ShoppingTestDataGenerator.GetFileContent("TripPlanCCEResponse.json");
            var tripPlanCCEResponse = JsonConvert.DeserializeObject<TripPlanCCEResponse>(tripPlanCCEResponsejson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var shopResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<ShopResponse>>(shopResponsejson);

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponsejson);


            return new object[] { mOBSHOPTripPlanRequest[1], tripPlanCCEResponse, session[0], shopResponse[0], shoppingResponse[0] };

        }

        public Object[] set18_2()
        {

            var mOBSHOPTripPlanRequestjson = ShoppingTestDataGenerator.GetFileContent("MOBSHOPTripPlanRequest.json");
            var mOBSHOPTripPlanRequest = JsonConvert.DeserializeObject<List<MOBSHOPTripPlanRequest>>(mOBSHOPTripPlanRequestjson);

            var tripPlanCCEResponsejson = ShoppingTestDataGenerator.GetFileContent("TripPlanCCEResponse.json");
            var tripPlanCCEResponse = JsonConvert.DeserializeObject<TripPlanCCEResponse>(tripPlanCCEResponsejson);

            var sessionjson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var shopResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<ShopResponse>>(shopResponsejson);

            var shoppingResponsejson = ShoppingTestDataGenerator.GetFileContent(@"GetShop_TestData\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponsejson);


            return new object[] { mOBSHOPTripPlanRequest[2], tripPlanCCEResponse, session[0], shopResponse[0], shoppingResponse[0] };

        }


    }
}
