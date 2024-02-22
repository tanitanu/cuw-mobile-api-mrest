using System.Threading.Tasks;
using United.Mobile.Model.Shopping;

namespace United.Mobile.Services.Shopping.Domain
{
    public interface IShopProductsBusiness
    {
        Task<GetProductInfoForFSRDResponse> GetProductInfoForFSRD(GetProductInfoForFSRDRequest getProductInfoForFSRDRequest);
        Task<ChasePromoRedirectResponse> ChasePromoRTIRedirect(ChasePromoRedirectRequest chasePromoRedirectRequest);
    }
}
