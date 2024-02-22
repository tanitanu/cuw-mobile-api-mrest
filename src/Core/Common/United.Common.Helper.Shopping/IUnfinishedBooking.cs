using System.Collections.Generic;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.SSR;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Misc;
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Service.Presentation.ReferenceDataResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common;
using United.Services.FlightShopping.Common.DisplayCart;
using Microsoft.AspNetCore.Http;
using MOBRegisterTravelersResponse = United.Mobile.Model.Travelers.MOBRegisterTravelersResponse;
using ErrorInfo = United.Services.FlightShopping.Common.ErrorInfo;

namespace United.Common.Helper.Shopping
{
    public interface IUnfinishedBooking
    {
        string GetFlightShareMessage(MOBSHOPReservation reservation, string cabinType);
        bool IsOneFlexibleSegmentExist(List<MOBSHOPTrip> trips);
        Task<MOBResReservation> PopulateReservation(Session session, Service.Presentation.ReservationModel.Reservation reservation);
        void AssignMissingPropertiesfromRegisterFlightsResponse(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReserationResponse, United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse registerFlightsResponse);
        Task<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse> RegisterFlights(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse flightReservationResponse, Session session, MOBRequest request);
        ShopRequest BuildShopPinDownDetailsRequest(MOBSHOPSelectUnfinishedBookingRequest request, string cartId = "");
        Task<bool> SaveAnUnfinishedBooking(Session session, MOBRequest request, MOBSHOPUnfinishedBooking ub);
        Task<List<MOBSHOPUnfinishedBooking>> GetSavedUnfinishedBookingEntries(Session session, MOBRequest request, bool isCatalogOnForTravelerTypes = false);
        Task<bool> UpdateAnUnfinishedBooking(Session session, MOBRequest request, MOBSHOPUnfinishedBooking ubTobeUpdated);
        Task<(United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse, bool)> GetShopPinDown(Session session, string appVersion, ShopRequest shopRequest, bool addTravelerFlow = false);
        
        List<MOBSHOPPrice> GetPrices(List<DisplayPrice> displayPrices);
        //FareLock GetFareLockOptions(Service.Presentation.ProductResponseModel.ProductOffer cslFareLock, List<DisplayPrice> prices, request.SelectedUnfinishBooking?.CatalogItems, request.Application.Id, request.Application.Version.Major);
        ShopRequest BuildShopPinDownRequest(MOBSHOPUnfinishedBooking unfinishedBooking, string mpNumber, string languageCode, int appID = -1, string appVer = "", bool isCatalogOnForTravelerTypes = false);
        Task<(MOBSHOPAvailability, bool)> GetShopPinDownDetailsV2(Session session, MOBSHOPSelectUnfinishedBookingRequest request = null, HttpContext httpContext = null, MOBAddTravelersRequest addTravelersRequest = null, bool addTravelerFlow = false);
        Task<MOBSHOPAvailability> GetShopPinDownDetails(Session session, MOBSHOPSelectUnfinishedBookingRequest request);
        List<MOBSHOPTax> GetTaxAndFees(List<DisplayPrice> prices, int numPax, bool isReshopChange = false);
        List<Mobile.Model.Shopping.TravelOption> GetTravelOptions(DisplayCart displayCart);
        Task<MultiCallResponse> GetSpecialNeedsReferenceDataFromFlightShopping(Session session, int appId, string appVersion, string deviceId, string languageCode);
        Task<TravelSpecialNeeds> GetItineraryAvailableSpecialNeeds(Session session, int appId, string appVersion, string deviceId, IEnumerable<ReservationFlightSegment> segments, string languageCode,
            MOBSHOPReservation reservation = null, SelectTripRequest selectRequest = null);
        Task<MOBMobileCMSContentMessages> GetCMSContentMessageByKey(string Key, MOBRequest request, Session session);
    }
}