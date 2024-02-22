using System.Collections.Generic;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;

namespace United.Mobile.Services.ShopFlightDetails.Domain
{
    public interface IShopFlightDetailsBusiness
    {
        Task<OnTimePerformanceResponse> GetONTimePerformence(OnTimePerformanceRequest onTimePerformanceRequest);
        Task<RefreshCacheForSDLContentResponse> RefreshCacheForSDLContent(string groupName, string cacheKey);
        bool IsValidSDLContentGroupNameCacheKeyCombination(string groupName, string cacheKey);
    }
}
