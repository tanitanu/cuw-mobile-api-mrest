using System.Threading.Tasks;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.AwardCalendar;

namespace United.Mobile.Services.ShopAward.Domain
{
    public interface IShopAwardBusiness
    {
        Task<RevenueLowestPriceForAwardSearchResponse> RevenueLowestPriceForAwardSearch(MOBSHOPShopRequest shopRequest);

        Task<AwardCalendarResponse> GetSelectTripAwardCalendar(SelectTripRequest shopRequest);

        Task<AwardCalendarResponse> GetShopAwardCalendar(MOBSHOPShopRequest shopRequest);
        Task<MOBException> GetAwardCalendarExceptionMessage(string code);
    }
}
