using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Common;

namespace United.Mobile.Services.Shopping.Domain
{
    public interface IShoppingBusiness
    {
        Task<ShopResponse> GetShop(MOBSHOPShopRequest request,HttpContext httpContext);

        Task<TripShareV2Response> GetShopRequest(ShareTripRequest shareTripRequest);

        Task<ShopOrganizeResultsResponse> OrganizeShopResults(ShopOrganizeResultsReqeust organizeResultsReqeust);

        Task<ShopResponse> ShopCLBOptOut(CLBOptOutRequest cLBOptOutRequest);
        Task<SelectTripResponse> SelectTrip(SelectTripRequest selectTripRequest, HttpContext httpContext=null );

        Task<ShopResponse> GetShopTripPlan(MOBSHOPTripPlanRequest request, HttpContext httpContext);

        Task<MOBCarbonEmissionsResponse> GetCarbonEmissionDetails(MOBCarbonEmissionsRequest request);

    }
}
