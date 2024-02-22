using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.Common;
using United.Mobile.Model.Shopping.Common.MoneyPlusMiles;
using United.Services.FlightShopping.Common;

namespace United.Mobile.DataAccess.Shopping
{

    public interface IMileagePricingService
    {
        Task<MOBMoneyPlusMilesOptionsResponse> GetMoneyPlusMilesOptions(Session session, MOBMoneyPlusMilesOptionsRequest request);
        
        Task<T> GetCSLMoneyAndMilesFareWheel<T>(Session session, MOBSHOPShopRequest shopRequest, ShopRequest request);

        Task<MOBFSRMileagePricingResponse> GetApplyMileagePricing(Session session, MOBFSRMileagePricingRequest request);
    }
}
