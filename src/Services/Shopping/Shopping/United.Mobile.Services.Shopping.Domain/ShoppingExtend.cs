using System.Collections.Generic;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Services.FlightShopping.Common;

namespace United.Mobile.Services.Shopping.Domain
{
    internal class ShoppingExtend
    {
        private readonly ISessionHelperService _sessionHelperService;
        public ShoppingExtend( ISessionHelperService sessionHelperService)
        {
            _sessionHelperService = sessionHelperService;
        }

        internal async Task<bool> AddAmenitiesRequestToPersist(UpdateAmenitiesIndicatorsRequest shopAmenitiesRequest, string sessionID, string lastTripIndexRequested)
        {
            try
            {
                #region Add Flight Amenities To Persist
                ShopAmenitiesRequest persistShopAmenitiesRequest = new ShopAmenitiesRequest();
                persistShopAmenitiesRequest = await _sessionHelperService.GetSession<ShopAmenitiesRequest>(sessionID, persistShopAmenitiesRequest.ObjectName, new List<string> { sessionID, persistShopAmenitiesRequest.ObjectName });

                if (persistShopAmenitiesRequest != null && persistShopAmenitiesRequest.AmenitiesIndicatorsRequest != null)
                {
                    bool isexist = false;
                    foreach (string key in persistShopAmenitiesRequest.TripIndexKeys)
                    {
                        if (key == lastTripIndexRequested)
                        {
                            persistShopAmenitiesRequest.AmenitiesIndicatorsRequest[lastTripIndexRequested] = shopAmenitiesRequest;
                            isexist = true;
                        }
                    }
                    if (!isexist)
                    {
                        persistShopAmenitiesRequest.AmenitiesIndicatorsRequest.Add(lastTripIndexRequested, shopAmenitiesRequest);
                        persistShopAmenitiesRequest.TripIndexKeys.Add(lastTripIndexRequested);
                    }
                }
                else
                {
                    persistShopAmenitiesRequest = new ShopAmenitiesRequest
                    {
                        SessionId = sessionID,
                        CartId = shopAmenitiesRequest.CartId
                    };
                    if (persistShopAmenitiesRequest.AmenitiesIndicatorsRequest == null)
                    {
                        persistShopAmenitiesRequest.AmenitiesIndicatorsRequest = new SerializableDictionary<string, UpdateAmenitiesIndicatorsRequest>();
                        persistShopAmenitiesRequest.TripIndexKeys = new List<string>();
                    }
                    if (shopAmenitiesRequest != null)
                    {
                        persistShopAmenitiesRequest.AmenitiesIndicatorsRequest.Add(lastTripIndexRequested, shopAmenitiesRequest);
                        persistShopAmenitiesRequest.TripIndexKeys.Add(lastTripIndexRequested);
                    }
                }
                await _sessionHelperService.SaveSession<ShopAmenitiesRequest>(persistShopAmenitiesRequest, sessionID, new List<string> { sessionID, persistShopAmenitiesRequest.ObjectName }, persistShopAmenitiesRequest.ObjectName);

                #endregion
            }
            catch (System.Exception)
            {
                return false;
            }

            return true;
        }
    }
}