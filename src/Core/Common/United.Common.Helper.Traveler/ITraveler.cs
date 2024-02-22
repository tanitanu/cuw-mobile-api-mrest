using System.Collections.Generic;
using System.Threading.Tasks;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.MP2015;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Travelers;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.FlightReservation;

namespace United.Common.Helper.Traveler
{
    public interface ITraveler
    {
        Task<MOBSHOPReservation> RegisterTravelers_CFOP(MOBRegisterTravelersRequest request, bool isRegisterOffersCall = false);
        Task<MOBCPTraveler> RegisterNewTravelerValidateMPMisMatch(MOBCPTraveler registerTraveler, MOBRequest mobRequest, string sessionID, string cartId, string token);
        Task<(List<MOBCreditCard> savedProfileOwnerCCList, List<MOBAddress> creditCardAddresses, MOBCPPhone mpPhone, MOBEmail mpEmail)> GetProfileOwnerCreditCardList(string sessionID, List<MOBAddress> creditCardAddresses, MOBCPPhone mpPhone, MOBEmail mpEmail, string updatedCCKey);
        Task<List<LmxFlight>> GetLmxForRTI(string token, string cartId);
        Task<List<MOBCPTraveler>> GetMPDetailsForSavedTravelers(MOBRegisterTravelersRequest request);
        void AssignFFCsToUnChangedTravelers(List<MOBCPTraveler> travelers, SerializableDictionary<string, MOBCPTraveler> travelersCSL, MOBApplication application, bool continueToChangeTravelers);
        Task<(RegisterTravelersRequest registerTravelerRequest, MOBRegisterTravelersRequest request)> GetRegisterTravelerRequest(MOBRegisterTravelersRequest request, bool isRequireNationalityAndResidence, Mobile.Model.Shopping.Reservation bookingPathReservation);
        Task<PlacePass> GetPlacePass(string destinationcode, string searchType, string sessionId, int id, string major, string deviceId, string v, string placepasscampain);
       Task< MOBRegisterTravelersRequest> GetPopulateProfileOwnerData(MOBRegisterTravelersRequest request);
        List<MOBLMXTraveler> GetLMXTravelersFromFlights(MOBSHOPReservation reservation);
        Task InFlightCLPaymentEligibility(MOBRegisterTravelersRequest request, Mobile.Model.Shopping.Reservation bookingPathReservation, Session session, MOBShoppingCart persistShoppingCart);
        bool IsNatAndResEnabled(MOBRegisterTravelersRequest request, Mobile.Model.Shopping.Reservation bookingPathReservation);
        bool GetEnableTravelerTypes(MOBRegisterTravelersRequest request, Mobile.Model.Shopping.Reservation bookingPathReservation);
        bool comapreTtypesList(Mobile.Model.Shopping.Reservation bookingPathReservation, DisplayCart displayCart);
        List<MOBSHOPPrice> GetPricesAfterPriceChange(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices, bool isAwardBooking, string sessionId, bool isReshopChange = false, FlightReservationResponse flightReservationResponse = null);
        List<MOBSHOPPrice> AdjustTotal(List<MOBSHOPPrice> prices);
        Task<List<MOBSHOPPrice>> GetPrices(List<United.Services.FlightShopping.Common.DisplayCart.DisplayPrice> prices, bool isAwardBooking,
   string sessionId, bool isReshopChange = false, int appId = 0, string appVersion = "", List<MOBItem> catalogItems = null, FlightReservationResponse shopBookingDetailsResponse = null);
        List<List<MOBSHOPTax>> GetTaxAndFeesAfterPriceChange(List<DisplayPrice> displayPrices, int numberOfTravelers, bool isReshopChange, int appId = 0, string appVersion = "", string travelType = null);
        bool IsBuyMilesFeatureEnabled(int appId, string version, List<MOBItem> catalgItems = null, bool isNotSelectTripCall = false);
        string GetNextViewName(MOBRegisterTravelersRequest request, United.Mobile.Model.Shopping.Reservation bookingPathReservation);
        Task GetTravelAdvisoryMessagesBookingRTI_V1(MOBRequest request, United.Mobile.Model.Shopping.Reservation persistedReservation, Session session);
        Task GetTravelAdvisoryMessagesBookingRTI(MOBRequest request, United.Mobile.Model.Shopping.Reservation persistedReservation, Session session);
        Task UnregisterAncillaryOffer(MOBShoppingCart shoppingCart, FlightReservationResponse cslFlightReservationResponse, MOBRequest mobRequest, string sessionId, string cartId);
        bool IsArranger(United.Mobile.Model.Shopping.Reservation bookingPathReservation);
        bool IsFareLockAvailable(United.Mobile.Model.Shopping.Reservation reservation);
        bool ShowViewCheckedBagsAtRti(United.Mobile.Model.Shopping.Reservation reservation);
        Task UpdateCovidTestInfo(MOBRegisterTravelersRequest request, United.Mobile.Model.Shopping.Reservation bookingPathReservation, Session session);
        bool ValidateCorporateMsg(List<InfoWarningMessages> infoWarningMessages);
        bool EnableYADesc(bool isReshop = false);
        bool EnableShoppingcartPhase2ChangesWithVersionCheck(int appId, string appVersion);
        string BuildPaxTypeDescription(string paxTypeCode, string paxDescription, int paxCount);
    }
}
