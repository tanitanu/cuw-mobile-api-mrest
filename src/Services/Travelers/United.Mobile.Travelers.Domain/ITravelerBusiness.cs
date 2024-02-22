using System.Threading.Tasks;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Travelers;

namespace United.Mobile.Travelers.Domain
{
    public interface ITravelerBusiness
    {
        Task<MOBRegisterTravelersResponse> RegisterTravelers_CFOP(MOBRegisterTravelersRequest request);
        Task<MOBSHOPReservation> RegisterTravelersCSL(MOBRegisterTravelersRequest request, bool isRegisterOffersCall = false);
        Task<MOBValidateWheelChairSizeResponse> ValidateWheelChairSize(MOBValidateWheelChairSizeRequest request);
    }
}
