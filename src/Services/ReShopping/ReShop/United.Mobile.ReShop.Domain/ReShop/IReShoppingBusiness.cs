using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using United.Mobile.Model.Shopping;

namespace United.Mobile.ReShop.Domain
{
    public interface IReShoppingBusiness
    {
        Task<ShopResponse> ReShop(MOBSHOPShopRequest request, HttpContext httpContext = null);
    }
}
