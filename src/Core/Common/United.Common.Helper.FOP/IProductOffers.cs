﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using United.Mobile.Model.Common;
using United.Mobile.Model.Payment;
using United.Mobile.Model.Shopping;
using United.Service.Presentation.PersonalizationResponseModel;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common.FlightReservation;
using RegisterOfferRequest = United.Services.FlightShopping.Common.FlightReservation.RegisterOfferRequest;
using ProductOffer = United.Service.Presentation.ProductResponseModel.ProductOffer;
using United.Mobile.Model.Shopping.FormofPayment;
using System.Threading.Tasks;

namespace United.Common.Helper.FOP
{
    public interface IProductOffers
    {
        Task<string> CreateCart(MOBRequest request, Session session);
        Task<FlightReservationResponse> RegisterFareLockReservation(MOBRegisterOfferRequest request, United.Service.Presentation.ReservationResponseModel.ReservationDetail reservationDetail, Session session);
        Task<FlightReservationResponse> RegisterOffers
            (MOBRegisterOfferRequest request, ProductOffer productOffer, ProductOffer productVendorOffer, DynamicOfferDetailResponse pomOffer, ReservationDetail reservationDetail, Session session, Collection<RegisterOfferRequest>
            upgradeCabinRegisterOfferRequest = null);
        Task<MOBSHOPTrip> ConvertPNRSegmentToShopTripWithTripNumber(List<ReservationFlightSegment> pnrFlightSegment);
        United.Services.FlightShopping.Common.FlightReservation.RegisterOfferRequest GetRegisterOffersRequest(string cartId, string cartKey, string languageCode, string pointOfSale, string productCode, string productId, List<string> productIds, string subProductCode, bool delete, United.Service.Presentation.ProductResponseModel.ProductOffer Offer, United.Service.Presentation.ReservationResponseModel.ReservationDetail reservation);
        string GetRedEyeFlightArrDate(String flightDepart, String flightArrive, ref bool flightDateChanged);
        string GetRedEyeDepartureDate(String tripDate, String flightDepartureDate, ref bool flightDateChanged);
    }
}
