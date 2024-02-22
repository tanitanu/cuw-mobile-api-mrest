using System.Threading.Tasks;
using United.Mobile.Model.Shopping;

namespace United.Mobile.ReshopSelectTrip.Domain
{
    public interface IReshopSelectTripBusiness
    {
        Task<SelectTripResponse> SelectTrip(SelectTripRequest selectTripRequest);
    }
}