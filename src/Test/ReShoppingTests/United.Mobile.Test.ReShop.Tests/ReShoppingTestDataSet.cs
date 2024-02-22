using System.Text;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Bundles;
using United.Service.Presentation.ReservationResponseModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.LMX;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using United.Mobile.Model.ReShop;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.Internal.AccountManagement;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Definition;
using United.Mobile.Model.ManageRes;
//using United.Mobile.Model.Internal.Common;

namespace United.Mobile.Test.ReShop.Tests
{
    public class ReShoppingTestDataSet
    {
        public Object[] set1()
        {

            var reShopRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopRequest.json");
            var reShopRequest = JsonConvert.DeserializeObject<List<MOBSHOPShopRequest>>(reShopRequestjson);

            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"SessionData.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var shoppingResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<Model.Shopping.ShoppingResponse>>(shoppingResponsejson);

            var latestShopAvailabilityResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"LatestShopAvailabilityResponse.json");
            var latestShopAvailabilityResponse = JsonConvert.DeserializeObject<LatestShopAvailabilityResponse>(latestShopAvailabilityResponsejson);

            var mOBSHOPFlattenedFlightList = ReShoppingTestDataGenerator.GetXmlData<MOBSHOPFlattenedFlightList>(@"MOBSHOPFlattenedFlightList.xml");

            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"AirportDetailsList.xml");

            var cSLShopRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestjson);


            var shopResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ShopResponse.json");
            var shopResponse = JsonConvert.DeserializeObject<List<United.Services.FlightShopping.Common.ShopResponse>>(shopResponsejson);

            var shopResponse1json = ReShoppingTestDataGenerator.GetFileContent(@"ShopResponse1.json");
            var shopResponse1 = JsonConvert.DeserializeObject<List<Model.Shopping.ShopResponse>>(shopResponse1json);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent("Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            var displayAirportDetailsjson = ReShoppingTestDataGenerator.GetFileContent(@"DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<MOBDisplayBagTrackAirportDetails>(displayAirportDetailsjson);

            var updateAmenitiesIndicatorsResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"UpdateAmenitiesIndicatorsResponse.json");
            var updateAmenitiesIndicatorsResponse = JsonConvert.DeserializeObject<UpdateAmenitiesIndicatorsResponse>(updateAmenitiesIndicatorsResponsejson);

            var lmxQuoteResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"LmxQuoteResponse.json");
            var lmxQuoteResponse = JsonConvert.DeserializeObject<LmxQuoteResponse>(lmxQuoteResponsejson);

            return new object[] { reShopRequest[0], session[0], shoppingResponse[0], latestShopAvailabilityResponse, mOBSHOPFlattenedFlightList, airportDetailsList, cSLShopRequest[0], shopResponse[0],shopResponse1[0],reservation[0], reservationDetail[0], displayAirportDetails, updateAmenitiesIndicatorsResponse, lmxQuoteResponse };
        }
        public Object[] set2()
        {
            var mOBRESHOPChangeEligibilityRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\MOBRESHOPChangeEligibilityRequest.json");
            var mOBRESHOPChangeEligibilityRequest = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityRequest>(mOBRESHOPChangeEligibilityRequestjson);

            var eligibilityResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\EligibilityResponse.json");
            var eligibilityResponse = JsonConvert.DeserializeObject<EligibilityResponse>(eligibilityResponsejson);

            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var hashPinValidatejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\HashPinValidate.json");
            var hashPinValidate = JsonConvert.DeserializeObject<HashPinValidate>(hashPinValidatejson);

            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ChangeEligibleCheck\AirportDetailsList.xml");


            var mOBLegalDocumentsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\MOBLegalDocuments.json");
            var mOBLegalDocuments = JsonConvert.DeserializeObject<List<MOBLegalDocument>>(mOBLegalDocumentsjson);


            var mOBPNRjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\MOBPNR.json");
            var mOBPNR = JsonConvert.DeserializeObject<MOBPNR>(mOBPNRjson);

            var displayAirportDetailsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck/DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<MOBDisplayBagTrackAirportDetails>(displayAirportDetailsjson);

            //var airportDetailsListjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\AirportDetailsList.json");
            //var airportDetailsList = JsonConvert.DeserializeObject<List<AirportDetailsList>>(airportDetailsListjson);

            return new object[] { mOBRESHOPChangeEligibilityRequest, eligibilityResponse, session[0], reservationDetail[0], reservation[0], hashPinValidate, airportDetailsList,  mOBPNR, mOBLegalDocuments, displayAirportDetails };

        }
        public Object[] set3()
        {
            var mOBRESHOPChangeEligibilityRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\MOBRESHOPChangeEligibilityRequest.json");
            var mOBRESHOPChangeEligibilityRequest = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityRequest>(mOBRESHOPChangeEligibilityRequestjson);


            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var mOBRESHOPChangeEligibilityResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\MOBRESHOPChangeEligibilityResponse.json");
            var mOBRESHOPChangeEligibilityResponse = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityResponse>(mOBRESHOPChangeEligibilityResponsejson);

            var displayAirportDetailsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Mobile.Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);


            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ChangeEligibleCheckAndReshop\AirportDetailsList.xml");




            return new object[] { mOBRESHOPChangeEligibilityRequest, session[0], reservationDetail[0], reservation[0], mOBRESHOPChangeEligibilityResponse, airportDetailsList, displayAirportDetails };
        }
        public Object[] set4()
        {
            var mOBChangeEmailRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\MOBChangeEmailRequest.json");
            var mOBChangeEmailRequest = JsonConvert.DeserializeObject<MOBChangeEmailRequest>(mOBChangeEmailRequestjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var mOBChangeEmailResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\MOBChangeEmailResponse.json");
            //var mOBChangeEmailResponse = JsonConvert.DeserializeObject<MOBChangeEmailResponse>(mOBChangeEmailResponsejson);

            var mOBShoppingCartjson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\MOBShoppingCart.json");
            var mOBShoppingCart = JsonConvert.DeserializeObject<List<MOBShoppingCart>>(mOBShoppingCartjson);

            var seatChangeStatejson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\SeatChangeState.json");
            var seatChangeState = JsonConvert.DeserializeObject<List<SeatChangeState>>(seatChangeStatejson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            //var loadReservationAndDisplayCartResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\LoadReservationAndDisplayCartResponse.json");
            //var loadReservationAndDisplayCartResponse = JsonConvert.DeserializeObject<List<LoadReservationAndDisplayCartResponse>>(loadReservationAndDisplayCartResponsejson);

            //var responsejson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\MOBApplyPromoCodeResponse.json");
            //var response = JsonConvert.DeserializeObject<List<MOBApplyPromoCodeResponse>>(responsejson);

            //var airportDetailsListjson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\AirportDetailsList.json");
            //var airportDetailsList = JsonConvert.DeserializeObject<List<AirportDetailsList>>(airportDetailsListjson);

            //var mOBChangeEmailResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\MOBChangeEmailResponse.json");
            //var mOBChangeEmailResponse = JsonConvert.DeserializeObject<MOBChangeEmailResponse>(mOBChangeEmailResponsejson);

            var mOBChangeEmailResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\MOBChangeEmailResponse.json");
            var mOBChangeEmailResponse = JsonConvert.DeserializeObject<MOBChangeEmailResponse>(mOBChangeEmailResponsejson);

            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ReshopSaveEmail_CFOP\AirportDetailsList.xml");


            return new object[] { mOBChangeEmailRequest, reservation[0], session[0], mOBShoppingCart[0], seatChangeState[0], reservationDetail[0],  airportDetailsList, mOBChangeEmailResponse};

        }
        public Object[] set5()
        {
            var selectTripRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"SelectTrip\SelectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<SelectTripRequest>>(selectTripRequestjson);

            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"SelectTrip\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var selectTripjson = ReShoppingTestDataGenerator.GetFileContent(@"SelectTrip\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<List<SelectTrip>>(selectTripjson);

            var cSLShopRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"SelectTrip\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"SelectTrip\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var selectTripResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"SelectTrip\SelectTripResponse.json");
            var selectTripResponse = JsonConvert.DeserializeObject<List<SelectTripResponse>>(selectTripResponsejson);






            return new object[] { selectTripRequest[0], session[0], selectTrip[0], cSLShopRequest[0], reservation[0], selectTripResponse[0] };
        }

        public Object[] set6()
        {
            var mOBSHOPProductSearchRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"GetProducts_CFOP\MOBSHOPProductSearchRequest.json");
            var mOBSHOPProductSearchRequest = JsonConvert.DeserializeObject<MOBSHOPProductSearchRequest>(mOBSHOPProductSearchRequestjson);


            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"GetProducts_CFOP\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"GetProducts_CFOP\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var shoppingResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"GetProducts_CFOP\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponsejson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"GetProducts_CFOP\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);


            var mOBLegalDocumentsjson = ReShoppingTestDataGenerator.GetFileContent(@"GetProducts_CFOP\MOBLegalDocuments.json");
            var mOBLegalDocuments = JsonConvert.DeserializeObject<List<MOBLegalDocument>>(mOBLegalDocumentsjson);

            var getOffersjson = ReShoppingTestDataGenerator.GetFileContent(@"GetProducts_CFOP\GetOffers.json");
            var getOffers = JsonConvert.DeserializeObject<List<GetOffers>>(getOffersjson);

            return new object[] { mOBSHOPProductSearchRequest, session[0], reservation[0], shoppingResponse[0], reservationDetail[0], mOBLegalDocuments, getOffers[0]};
        }

        public Object[] set7()
        {
            var mOBConfirmScheduleChangeRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ConfirmScheduleChange\MOBConfirmScheduleChangeRequest.json");
            var mOBConfirmScheduleChangeRequest = JsonConvert.DeserializeObject<MOBConfirmScheduleChangeRequest>(mOBConfirmScheduleChangeRequestjson);


            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ConfirmScheduleChange\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var hashPinValidatejson = ReShoppingTestDataGenerator.GetFileContent(@"ConfirmScheduleChange\HashPinValidate.json");
            var hashPinValidate = JsonConvert.DeserializeObject<HashPinValidate>(hashPinValidatejson);


            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ConfirmScheduleChange\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            return new object[] { mOBConfirmScheduleChangeRequest, session[0], hashPinValidate, reservationDetail[0] };    
        }
        public Object[] set8()
        {
            var mOBRESHOPChangeEligibilityRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck1\MOBRESHOPChangeEligibilityRequest.json");
            var mOBRESHOPChangeEligibilityRequest = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityRequest>(mOBRESHOPChangeEligibilityRequestjson);

            var eligibilityResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck1\EligibilityResponse.json");
            var eligibilityResponse = JsonConvert.DeserializeObject<EligibilityResponse>(eligibilityResponsejson);

            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck1\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck1\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck1\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var hashPinValidatejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck1\HashPinValidate.json");
            var hashPinValidate = JsonConvert.DeserializeObject<HashPinValidate>(hashPinValidatejson);

            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ChangeEligibleCheck1\AirportDetailsList.xml");


            var mOBLegalDocumentsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck1\MOBLegalDocuments.json");
            var mOBLegalDocuments = JsonConvert.DeserializeObject<List<MOBLegalDocument>>(mOBLegalDocumentsjson);


            var mOBPNRjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck1\MOBPNR.json");
            var mOBPNR = JsonConvert.DeserializeObject<MOBPNR>(mOBPNRjson);

            var displayAirportDetailsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck1/DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<MOBDisplayBagTrackAirportDetails>(displayAirportDetailsjson);

            //var airportDetailsListjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\AirportDetailsList.json");
            //var airportDetailsList = JsonConvert.DeserializeObject<List<AirportDetailsList>>(airportDetailsListjson);

            return new object[] { mOBRESHOPChangeEligibilityRequest, eligibilityResponse, session[0], reservationDetail[0], reservation[0], hashPinValidate, airportDetailsList, mOBPNR, mOBLegalDocuments, displayAirportDetails };

        }
        public Object[] set9()
        {
            var mOBRESHOPChangeEligibilityRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop1\MOBRESHOPChangeEligibilityRequest.json");
            var mOBRESHOPChangeEligibilityRequest = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityRequest>(mOBRESHOPChangeEligibilityRequestjson);


            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop1\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop1\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop1\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var mOBRESHOPChangeEligibilityResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop1\MOBRESHOPChangeEligibilityResponse.json");
            var mOBRESHOPChangeEligibilityResponse = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityResponse>(mOBRESHOPChangeEligibilityResponsejson);

            var displayAirportDetailsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop1\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Mobile.Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);


            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ChangeEligibleCheckAndReshop1\AirportDetailsList.xml");




            return new object[] { mOBRESHOPChangeEligibilityRequest, session[0], reservationDetail[0], reservation[0], mOBRESHOPChangeEligibilityResponse, airportDetailsList, displayAirportDetails };
        }
        public Object[] set10()
        {
            var mOBRESHOPChangeEligibilityRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck2\MOBRESHOPChangeEligibilityRequest.json");
            var mOBRESHOPChangeEligibilityRequest = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityRequest>(mOBRESHOPChangeEligibilityRequestjson);

            var eligibilityResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck2\EligibilityResponse.json");
            var eligibilityResponse = JsonConvert.DeserializeObject<EligibilityResponse>(eligibilityResponsejson);

            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck2\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck2\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck2\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var hashPinValidatejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck2\HashPinValidate.json");
            var hashPinValidate = JsonConvert.DeserializeObject<HashPinValidate>(hashPinValidatejson);

            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ChangeEligibleCheck2\AirportDetailsList.xml");


            var mOBLegalDocumentsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck2\MOBLegalDocuments.json");
            var mOBLegalDocuments = JsonConvert.DeserializeObject<List<MOBLegalDocument>>(mOBLegalDocumentsjson);


            var mOBPNRjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck2\MOBPNR.json");
            var mOBPNR = JsonConvert.DeserializeObject<MOBPNR>(mOBPNRjson);

            var displayAirportDetailsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck2/DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<MOBDisplayBagTrackAirportDetails>(displayAirportDetailsjson);

            //var airportDetailsListjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\AirportDetailsList.json");
            //var airportDetailsList = JsonConvert.DeserializeObject<List<AirportDetailsList>>(airportDetailsListjson);

            return new object[] { mOBRESHOPChangeEligibilityRequest, eligibilityResponse, session[0], reservationDetail[0], reservation[0], hashPinValidate, airportDetailsList, mOBPNR, mOBLegalDocuments, displayAirportDetails };

        }

        public Object[] set11()
        {
            var mOBRESHOPChangeEligibilityRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative\MOBRESHOPChangeEligibilityRequest.json");
            var mOBRESHOPChangeEligibilityRequest = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityRequest>(mOBRESHOPChangeEligibilityRequestjson);

            var eligibilityResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative\EligibilityResponse.json");
            var eligibilityResponse = JsonConvert.DeserializeObject<EligibilityResponse>(eligibilityResponsejson);

            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var hashPinValidatejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative\HashPinValidate.json");
            var hashPinValidate = JsonConvert.DeserializeObject<HashPinValidate>(hashPinValidatejson);

            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ChangeEligibleCheck_negative\AirportDetailsList.xml");


            var mOBLegalDocumentsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative\MOBLegalDocuments.json");
            var mOBLegalDocuments = JsonConvert.DeserializeObject<List<MOBLegalDocument>>(mOBLegalDocumentsjson);


            var mOBPNRjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative\MOBPNR.json");
            var mOBPNR = JsonConvert.DeserializeObject<MOBPNR>(mOBPNRjson);

            var displayAirportDetailsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative/DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<MOBDisplayBagTrackAirportDetails>(displayAirportDetailsjson);

            //var airportDetailsListjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\AirportDetailsList.json");
            //var airportDetailsList = JsonConvert.DeserializeObject<List<AirportDetailsList>>(airportDetailsListjson);

            return new object[] { mOBRESHOPChangeEligibilityRequest, eligibilityResponse, session[0], reservationDetail[0], reservation[0], hashPinValidate, airportDetailsList, mOBPNR, mOBLegalDocuments, displayAirportDetails };

        }

        public Object[] set12()
        {
            var mOBRESHOPChangeEligibilityRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative1\MOBRESHOPChangeEligibilityRequest.json");
            var mOBRESHOPChangeEligibilityRequest = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityRequest>(mOBRESHOPChangeEligibilityRequestjson);

            var eligibilityResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative1\EligibilityResponse.json");
            var eligibilityResponse = JsonConvert.DeserializeObject<EligibilityResponse>(eligibilityResponsejson);

            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative1\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative1\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative1\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var hashPinValidatejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative1\HashPinValidate.json");
            var hashPinValidate = JsonConvert.DeserializeObject<HashPinValidate>(hashPinValidatejson);

            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ChangeEligibleCheck_negative1\AirportDetailsList.xml");


            var mOBLegalDocumentsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative1\MOBLegalDocuments.json");
            var mOBLegalDocuments = JsonConvert.DeserializeObject<List<MOBLegalDocument>>(mOBLegalDocumentsjson);


            var mOBPNRjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative1\MOBPNR.json");
            var mOBPNR = JsonConvert.DeserializeObject<MOBPNR>(mOBPNRjson);

            var displayAirportDetailsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative1/DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<MOBDisplayBagTrackAirportDetails>(displayAirportDetailsjson);

            //var airportDetailsListjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\AirportDetailsList.json");
            //var airportDetailsList = JsonConvert.DeserializeObject<List<AirportDetailsList>>(airportDetailsListjson);

            return new object[] { mOBRESHOPChangeEligibilityRequest, eligibilityResponse, session[0], reservationDetail[0], reservation[0], hashPinValidate, airportDetailsList, mOBPNR, mOBLegalDocuments, displayAirportDetails };

        }

        public Object[] set13()
        {
            var mOBRESHOPChangeEligibilityRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative2\MOBRESHOPChangeEligibilityRequest.json");
            var mOBRESHOPChangeEligibilityRequest = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityRequest>(mOBRESHOPChangeEligibilityRequestjson);

            var eligibilityResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative2\EligibilityResponse.json");
            var eligibilityResponse = JsonConvert.DeserializeObject<EligibilityResponse>(eligibilityResponsejson);

            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative2\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative2\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative2\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var hashPinValidatejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative2\HashPinValidate.json");
            var hashPinValidate = JsonConvert.DeserializeObject<HashPinValidate>(hashPinValidatejson);

            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ChangeEligibleCheck_negative2\AirportDetailsList.xml");


            var mOBLegalDocumentsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative2\MOBLegalDocuments.json");
            var mOBLegalDocuments = JsonConvert.DeserializeObject<List<MOBLegalDocument>>(mOBLegalDocumentsjson);


            var mOBPNRjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative2\MOBPNR.json");
            var mOBPNR = JsonConvert.DeserializeObject<MOBPNR>(mOBPNRjson);

            var displayAirportDetailsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative2/DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<MOBDisplayBagTrackAirportDetails>(displayAirportDetailsjson);

            //var airportDetailsListjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\AirportDetailsList.json");
            //var airportDetailsList = JsonConvert.DeserializeObject<List<AirportDetailsList>>(airportDetailsListjson);

            return new object[] { mOBRESHOPChangeEligibilityRequest, eligibilityResponse, session[0], reservationDetail[0], reservation[0], hashPinValidate, airportDetailsList, mOBPNR, mOBLegalDocuments, displayAirportDetails };

        }

        public Object[] set14()
        {
            var mOBRESHOPChangeEligibilityRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative3\MOBRESHOPChangeEligibilityRequest.json");
            var mOBRESHOPChangeEligibilityRequest = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityRequest>(mOBRESHOPChangeEligibilityRequestjson);

            var eligibilityResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative3\EligibilityResponse.json");
            var eligibilityResponse = JsonConvert.DeserializeObject<EligibilityResponse>(eligibilityResponsejson);

            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative3\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative3\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative3\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var hashPinValidatejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative3\HashPinValidate.json");
            var hashPinValidate = JsonConvert.DeserializeObject<HashPinValidate>(hashPinValidatejson);

            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ChangeEligibleCheck_negative3\AirportDetailsList.xml");


            var mOBLegalDocumentsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative3\MOBLegalDocuments.json");
            var mOBLegalDocuments = JsonConvert.DeserializeObject<List<MOBLegalDocument>>(mOBLegalDocumentsjson);


            var mOBPNRjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative3\MOBPNR.json");
            var mOBPNR = JsonConvert.DeserializeObject<MOBPNR>(mOBPNRjson);

            var displayAirportDetailsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck_negative3/DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<MOBDisplayBagTrackAirportDetails>(displayAirportDetailsjson);

            //var airportDetailsListjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\AirportDetailsList.json");
            //var airportDetailsList = JsonConvert.DeserializeObject<List<AirportDetailsList>>(airportDetailsListjson);

            return new object[] { mOBRESHOPChangeEligibilityRequest, eligibilityResponse, session[0], reservationDetail[0], reservation[0], hashPinValidate, airportDetailsList, mOBPNR, mOBLegalDocuments, displayAirportDetails };

        }

        public Object[] set15()
        {
            var mOBRESHOPChangeEligibilityRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop_negative\MOBRESHOPChangeEligibilityRequest.json");
            var mOBRESHOPChangeEligibilityRequest = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityRequest>(mOBRESHOPChangeEligibilityRequestjson);


            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var mOBRESHOPChangeEligibilityResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop_negative\MOBRESHOPChangeEligibilityResponse.json");
            var mOBRESHOPChangeEligibilityResponse = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityResponse>(mOBRESHOPChangeEligibilityResponsejson);

            var displayAirportDetailsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Mobile.Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);


            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ChangeEligibleCheckAndReshop\AirportDetailsList.xml");




            return new object[] { mOBRESHOPChangeEligibilityRequest, session[0], reservationDetail[0], reservation[0], mOBRESHOPChangeEligibilityResponse, airportDetailsList, displayAirportDetails };
        }


        public Object[] set16()
        {
            var mOBChangeEmailRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP2\MOBChangeEmailRequest.json");
            var mOBChangeEmailRequest = JsonConvert.DeserializeObject<MOBChangeEmailRequest>(mOBChangeEmailRequestjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP2\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            //var mOBChangeEmailResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\MOBChangeEmailResponse.json");
            //var mOBChangeEmailResponse = JsonConvert.DeserializeObject<MOBChangeEmailResponse>(mOBChangeEmailResponsejson);

            var mOBShoppingCartjson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP2\MOBShoppingCart.json");
            var mOBShoppingCart = JsonConvert.DeserializeObject<List<MOBShoppingCart>>(mOBShoppingCartjson);

            var seatChangeStatejson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\SeatChangeState.json");
            var seatChangeState = JsonConvert.DeserializeObject<List<SeatChangeState>>(seatChangeStatejson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            //var loadReservationAndDisplayCartResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\LoadReservationAndDisplayCartResponse.json");
            //var loadReservationAndDisplayCartResponse = JsonConvert.DeserializeObject<List<LoadReservationAndDisplayCartResponse>>(loadReservationAndDisplayCartResponsejson);

            //var responsejson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\MOBApplyPromoCodeResponse.json");
            //var response = JsonConvert.DeserializeObject<List<MOBApplyPromoCodeResponse>>(responsejson);

            //var airportDetailsListjson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\AirportDetailsList.json");
            //var airportDetailsList = JsonConvert.DeserializeObject<List<AirportDetailsList>>(airportDetailsListjson);

            //var mOBChangeEmailResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP\MOBChangeEmailResponse.json");
            //var mOBChangeEmailResponse = JsonConvert.DeserializeObject<MOBChangeEmailResponse>(mOBChangeEmailResponsejson);

            var mOBChangeEmailResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ReshopSaveEmail_CFOP2\MOBChangeEmailResponse.json");
            var mOBChangeEmailResponse = JsonConvert.DeserializeObject<MOBChangeEmailResponse>(mOBChangeEmailResponsejson);

            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ReshopSaveEmail_CFOP\AirportDetailsList.xml");


            return new object[] { mOBChangeEmailRequest, reservation[0], session[0], mOBShoppingCart[0], seatChangeState[0], reservationDetail[0], airportDetailsList, mOBChangeEmailResponse };

        }

        public Object[] set17()
        {
            var selectTripRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"SelectTrip_negative\SelectTripRequest.json");
            var selectTripRequest = JsonConvert.DeserializeObject<List<SelectTripRequest>>(selectTripRequestjson);

            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"SelectTrip_negative\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var selectTripjson = ReShoppingTestDataGenerator.GetFileContent(@"SelectTrip\SelectTrip.json");
            var selectTrip = JsonConvert.DeserializeObject<List<SelectTrip>>(selectTripjson);

            var cSLShopRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"SelectTrip\CSLShopRequest.json");
            var cSLShopRequest = JsonConvert.DeserializeObject<List<CSLShopRequest>>(cSLShopRequestjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"SelectTrip\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var selectTripResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"SelectTrip_negative\SelectTripResponse.json");
            var selectTripResponse = JsonConvert.DeserializeObject<List<SelectTripResponse>>(selectTripResponsejson);






            return new object[] { selectTripRequest[0], session[0], selectTrip[0], cSLShopRequest[0], reservation[0], selectTripResponse[0] };
        }

        public Object[] set18()
        {
            var mOBRESHOPChangeEligibilityRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop2\MOBRESHOPChangeEligibilityRequest.json");
            var mOBRESHOPChangeEligibilityRequest = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityRequest>(mOBRESHOPChangeEligibilityRequestjson);


            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var mOBRESHOPChangeEligibilityResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop2\MOBRESHOPChangeEligibilityResponse.json");
            var mOBRESHOPChangeEligibilityResponse = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityResponse>(mOBRESHOPChangeEligibilityResponsejson);

            var displayAirportDetailsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Mobile.Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);


            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ChangeEligibleCheckAndReshop1\AirportDetailsList.xml");




            return new object[] { mOBRESHOPChangeEligibilityRequest, session[0], reservationDetail[0], reservation[0], mOBRESHOPChangeEligibilityResponse, airportDetailsList, displayAirportDetails };
        }

        public Object[] set19()
        {
            var mOBRESHOPChangeEligibilityRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck3\MOBRESHOPChangeEligibilityRequest.json");
            var mOBRESHOPChangeEligibilityRequest = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityRequest>(mOBRESHOPChangeEligibilityRequestjson);

            var eligibilityResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck3\EligibilityResponse.json");
            var eligibilityResponse = JsonConvert.DeserializeObject<EligibilityResponse>(eligibilityResponsejson);

            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck3\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var hashPinValidatejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\HashPinValidate.json");
            var hashPinValidate = JsonConvert.DeserializeObject<HashPinValidate>(hashPinValidatejson);

            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ChangeEligibleCheck\AirportDetailsList.xml");


            var mOBLegalDocumentsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\MOBLegalDocuments.json");
            var mOBLegalDocuments = JsonConvert.DeserializeObject<List<MOBLegalDocument>>(mOBLegalDocumentsjson);


            var mOBPNRjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\MOBPNR.json");
            var mOBPNR = JsonConvert.DeserializeObject<MOBPNR>(mOBPNRjson);

            var displayAirportDetailsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck/DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<MOBDisplayBagTrackAirportDetails>(displayAirportDetailsjson);

            //var airportDetailsListjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheck\AirportDetailsList.json");
            //var airportDetailsList = JsonConvert.DeserializeObject<List<AirportDetailsList>>(airportDetailsListjson);

            return new object[] { mOBRESHOPChangeEligibilityRequest, eligibilityResponse, session[0], reservationDetail[0], reservation[0], hashPinValidate, airportDetailsList, mOBPNR, mOBLegalDocuments, displayAirportDetails };

        }

        public Object[] set20()
        {
            var mOBRESHOPChangeEligibilityRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop3\MOBRESHOPChangeEligibilityRequest.json");
            var mOBRESHOPChangeEligibilityRequest = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityRequest>(mOBRESHOPChangeEligibilityRequestjson);


            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop3\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var mOBRESHOPChangeEligibilityResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop3\MOBRESHOPChangeEligibilityResponse.json");
            var mOBRESHOPChangeEligibilityResponse = JsonConvert.DeserializeObject<MOBRESHOPChangeEligibilityResponse>(mOBRESHOPChangeEligibilityResponsejson);

            var displayAirportDetailsjson = ReShoppingTestDataGenerator.GetFileContent(@"ChangeEligibleCheckAndReshop\DisplayAirportDetails.json");
            var displayAirportDetails = JsonConvert.DeserializeObject<Mobile.Model.Internal.Common.DisplayAirportDetails>(displayAirportDetailsjson);


            var airportDetailsList = ReShoppingTestDataGenerator.GetXmlData<AirportDetailsList>(@"ChangeEligibleCheckAndReshop1\AirportDetailsList.xml");




            return new object[] { mOBRESHOPChangeEligibilityRequest, session[0], reservationDetail[0], reservation[0], mOBRESHOPChangeEligibilityResponse, airportDetailsList, displayAirportDetails };
        }

        public Object[] set21()
        {
            var mOBSHOPProductSearchRequestjson = ReShoppingTestDataGenerator.GetFileContent(@"GetProducts_CFOP\MOBSHOPProductSearchRequest1.json");
            var mOBSHOPProductSearchRequest = JsonConvert.DeserializeObject<MOBSHOPProductSearchRequest>(mOBSHOPProductSearchRequestjson);


            var sessionjson = ReShoppingTestDataGenerator.GetFileContent(@"GetProducts_CFOP\Session.json");
            var session = JsonConvert.DeserializeObject<List<Session>>(sessionjson);

            var reservationjson = ReShoppingTestDataGenerator.GetFileContent(@"GetProducts_CFOP\Reservation.json");
            var reservation = JsonConvert.DeserializeObject<List<Reservation>>(reservationjson);

            var shoppingResponsejson = ReShoppingTestDataGenerator.GetFileContent(@"GetProducts_CFOP\ShoppingResponse.json");
            var shoppingResponse = JsonConvert.DeserializeObject<List<ShoppingResponse>>(shoppingResponsejson);

            var reservationDetailjson = ReShoppingTestDataGenerator.GetFileContent(@"GetProducts_CFOP\ReservationDetail.json");
            var reservationDetail = JsonConvert.DeserializeObject<List<ReservationDetail>>(reservationDetailjson);


            var mOBLegalDocumentsjson = ReShoppingTestDataGenerator.GetFileContent(@"GetProducts_CFOP\MOBLegalDocuments.json");
            var mOBLegalDocuments = JsonConvert.DeserializeObject<List<MOBLegalDocument>>(mOBLegalDocumentsjson);

            var getOffersjson = ReShoppingTestDataGenerator.GetFileContent(@"GetProducts_CFOP\GetOffers.json");
            var getOffers = JsonConvert.DeserializeObject<List<GetOffers>>(getOffersjson);

            return new object[] { mOBSHOPProductSearchRequest, session[0], reservation[0], shoppingResponse[0], reservationDetail[0], mOBLegalDocuments, getOffers[0] };
        }
    }
}
