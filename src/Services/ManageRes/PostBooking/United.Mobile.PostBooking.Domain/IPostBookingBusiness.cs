using System.Threading.Tasks;
using United.Mobile.Model.PostBooking;

namespace United.Mobile.PostBooking.Domain
{
    public interface IPostBookingBusiness
    {
        Task<MOBSHOPGetOffersResponse> GetOffers(MOBSHOPGetOffersRequest request);
    }
}
