using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.FlightReservation;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Common.Helper.Shopping
{
    public interface IShoppingBuyMiles
    {
        void UpdatePricesForBuyMiles(List<MOBSHOPPrice> displayPrices, FlightReservationResponse shopBookingDetailsResponse, List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> displayFees = null);
        void ApplyPriceChangesForBuyMiles(FlightReservationResponse flightReservationResponse,
        Reservation reservation = null, Reservation bookingPathReservation = null);
        void AddBuyMilesFeature(ShopSelectRequest request, MOBSHOPAvailability availability, SelectTripRequest selectTripRequest, Session session,
           FlightReservationResponse shopBookingDetailsResponse, FlightReservationResponse response);
        void UpdatePriceTypeDescForBuyMiles(int appId, string appVersion, List<MOBItem> catalogItems, FlightReservationResponse shopBookingDetailsResponse, MOBSHOPPrice bookingPrice);
        //void ApplyPriceChangesForBuyMiles(FlightReservationResponse flightReservationResponse, MOBSHOPReservation reservation = null,
        //  Reservation bookingPathReservation = null);

        bool IsBuyMilesFeatureEnabled(int appId, string version, List<MOBItem> catalogItems = null, bool isNotSelectTripCall = false);
        void UpdateGrandTotal(MOBSHOPReservation reservation, bool isCommonMethod = false);
        Task<string> GetSSOToken(int applicationId, string deviceId, string appVersion, string transactionId, String webConfigSession, string sessionID, string mileagPlusNumber);
    }
}
