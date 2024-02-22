using System.Threading.Tasks;
using United.Mobile.Model.UnitedClubPasses;

namespace United.Mobile.UnitedClubPasses.Domain
{
    public interface IPurchaseOTPPassesBusiness
    {
        Task<OTPPurchaseResponse> PurchaseOTPPasses(OTPPurchaseRequest request);
    }
}
