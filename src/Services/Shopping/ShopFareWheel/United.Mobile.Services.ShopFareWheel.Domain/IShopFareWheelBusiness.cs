using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;

namespace United.Mobile.Services.ShopFareWheel.Domain
{
    public interface IShopFareWheelBusiness
    {
        Task<FareWheelResponse> GetShopFareWheelListResponse(MOBSHOPShopRequest bookingBundlesRequest);
        Task<FareWheelResponse> GetFareWheelListResponse(SelectTripRequest selectTripRequest);
    }
}
