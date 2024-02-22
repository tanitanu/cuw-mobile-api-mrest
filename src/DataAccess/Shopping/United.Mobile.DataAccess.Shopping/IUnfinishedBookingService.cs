using System.Threading.Tasks;
using United.Services.FlightShopping.Common;

namespace United.Mobile.DataAccess.Shopping
{
    public interface IUnfinishedBookingService
    {
        Task<T> GetShopPinDown<T>(string token, string sessionId, ShopRequest shopRequest);

    }
}
