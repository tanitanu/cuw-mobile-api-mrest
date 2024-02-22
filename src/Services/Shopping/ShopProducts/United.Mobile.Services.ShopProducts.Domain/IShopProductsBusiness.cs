using System.Threading.Tasks;
using United.Mobile.Model.Shopping;

namespace United.Mobile.Services.ShopProducts.Domain
{
    public interface IShopProductsBusiness
    {
        Task<GetProductInfoForFSRDResponse> GetProductInfoForFSRD(GetProductInfoForFSRDRequest getProductInfoForFSRDRequest);
        Task<ChasePromoRedirectResponse> ChasePromoRTIRedirect(ChasePromoRedirectRequest chasePromoRedirectRequest);
    }
}
