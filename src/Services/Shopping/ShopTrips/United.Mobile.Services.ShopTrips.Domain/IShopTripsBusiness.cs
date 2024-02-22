using System.Collections.Generic;
using System.Threading.Tasks;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Common.SSR;
using United.Mobile.Model.Shopping;
using United.Service.Presentation.SegmentModel;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using Microsoft.AspNetCore.Http;
using MOBRegisterTravelersResponse = United.Mobile.Model.Travelers.MOBRegisterTravelersResponse;

namespace United.Mobile.Services.ShopTrips.Domain
{
    public interface IShopTripsBusiness
    {
        Task<ShoppingTripFareTypeDetailsResponse> GetTripCompareFareTypes(ShoppingTripFareTypeDetailsRequest shoppingTripFareTypeDetailsRequest);
        Task<TripShare> GetShareTrip(ShareTripRequest shareTripRequest);
        Task<SelectTripResponse> MetaSelectTrip(MetaSelectTripRequest metaSelectTripRequest, HttpContext httpContext = null);
        Task<FareRulesResponse> GetFareRulesForSelectedTrip(GetFareRulesRequest getFareRulesRequest);
        Task<TravelSpecialNeeds> GetItineraryAvailableSpecialNeeds(Session session, int appId, string appVersion, string deviceId, IEnumerable<ReservationFlightSegment> segments, string languageCode, MOBSHOPReservation reservation = null, SelectTripRequest selectRequest = null);
        Reservation ReservationToPersistReservation(MOBSHOPAvailability availability);
        Task<bool> IsRequireNatAndCR(MOBSHOPReservation reservation, MOBApplication application, string sessionID, string deviceID, string token);
        Task<MOBRegisterTravelersResponse> RepriceForAddTravelers(MOBAddTravelersRequest request, HttpContext httpContext = null);
    }
}
