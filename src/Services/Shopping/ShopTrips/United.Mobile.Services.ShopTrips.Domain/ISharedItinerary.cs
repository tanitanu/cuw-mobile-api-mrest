using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.UnfinishedBooking;

namespace United.Mobile.Services.ShopTrips.Domain
{
    public interface ISharedItinerary
    {
        Task<MOBSHOPSelectUnfinishedBookingRequest> GetSharedTripItinerary(Session session, MetaSelectTripRequest request);
        Task<string> InsertSharedItinerary(MOBSHOPReservation reservation, MOBRequest mobRequest, string sessionId, string logAction);
    }
}
