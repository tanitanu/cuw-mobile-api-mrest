using System;
using System.Threading.Tasks;
using United.Mobile.Model.Shopping;

namespace United.Mobile.Services.ShopSeats.Domain
{
    public interface IShopSeatsBusiness
    {
        Task<SelectSeatsResponse> SelectSeats(SelectSeatsRequest selectSeatsRequest);
        Task<bool> ValidateEPlusVersion(int applicationID, string appVersion);
    }
}
