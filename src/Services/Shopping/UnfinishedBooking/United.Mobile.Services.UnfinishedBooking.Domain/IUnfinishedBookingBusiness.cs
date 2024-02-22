using System.Threading.Tasks;
using United.Mobile.Model;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.UnfinishedBooking;
using United.Mobile.Model.TripPlannerGetService;
using United.Mobile.Model.UnfinishedBooking;
using MOBSHOPUnfinishedBookingRequestBase = United.Mobile.Model.Shopping.UnfinishedBooking.MOBSHOPUnfinishedBookingRequestBase;
using Microsoft.AspNetCore.Http;

namespace United.Mobile.Services.UnfinishedBooking.Domain
{
    public interface IUnfinishedBookingBusiness
    {
        Task<MOBSHOPGetUnfinishedBookingsResponse> GetUnfinishedBookings(MOBSHOPGetUnfinishedBookingsRequest request);
        Task<SelectTripResponse> SelectUnfinishedBooking(MOBSHOPSelectUnfinishedBookingRequest request, HttpContext httpContext = null);
        Task<MOBResponse> ClearUnfinishedBookings(MOBSHOPUnfinishedBookingRequestBase request);
        Task<MOBSHOPSelectTripResponse> SelectOmniCartSavedTrip(MOBSHOPSelectUnfinishedBookingRequest request, HttpContext httpContext = null);
        Task<MOBGetOmniCartSavedTripsResponse> GetOmniCartSavedTrips(MOBSHOPUnfinishedBookingRequestBase request);
        Task<MOBGetOmniCartSavedTripsResponse> RemoveOmniCartSavedTrip(MOBSHOPUnfinishedBookingRequestBase request);

    }
}
