using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using United.Mobile.Model.Shopping.Common;
using United.Mobile.Model.Shopping.Common.MoneyPlusMiles;

namespace United.Mobile.Services.Shopping.Domain
{
    public interface IShopMileagePricingBusiness
    {

        Task<MOBMoneyPlusMilesOptionsResponse> GetMoneyPlusMilesOptions(MOBMoneyPlusMilesOptionsRequest request, HttpContext httpContext);
        Task<MOBFSRMileagePricingResponse> ApplyMileagePricing(MOBFSRMileagePricingRequest request, HttpContext httpContext);
    }
}
