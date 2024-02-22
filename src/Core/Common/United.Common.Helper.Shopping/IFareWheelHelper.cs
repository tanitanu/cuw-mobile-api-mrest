using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model.Shopping;

namespace United.Common.Helper.Shopping
{
    public interface IFareWheelHelper
    {
        Task<United.Services.FlightShopping.Common.ShopSelectRequest> GetShopSelectRequest(SelectTripRequest selectRequest, bool isForSelectTrip = false, bool isEnableWheelChairFilterOnFSR = false);
        United.Services.FlightShopping.Common.ShopSelectRequest GetShopSelectFareWheelRequest(United.Services.FlightShopping.Common.ShopSelectRequest selectRequest);
    }
}
