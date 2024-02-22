using System.Threading.Tasks;
using United.Mobile.Model.ReShop;

namespace United.Mobile.UpdateProfile.Domain
{
    public interface IUpdateProfileBusiness
    {
        Task<MOBChangeEmailResponse> ReshopSaveEmail_CFOP(MOBChangeEmailRequest request);
        Task<MOBSHOPReservationResponse> ReshopSaveAddress_CFOP(MOBChangeAddressRequest request);
    }
}