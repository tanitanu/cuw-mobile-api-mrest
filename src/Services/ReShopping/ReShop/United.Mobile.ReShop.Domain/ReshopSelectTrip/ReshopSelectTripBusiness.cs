using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using United.Mobile.Model.Shopping;
using United.Mobile.Services.Shopping.Domain;
using United.Utility.Helper;

namespace United.Mobile.ReshopSelectTrip.Domain
{
    public class ReshopSelectTripBusiness : IReshopSelectTripBusiness
    {
        private readonly ICacheLog<ReshopSelectTripBusiness> _logger;
        private readonly IShoppingBusiness _shoppingBusiness;

        public ReshopSelectTripBusiness(ICacheLog<ReshopSelectTripBusiness> logger
            , IShoppingBusiness shoppingBusiness)
        {
            _logger = logger;
            _shoppingBusiness = shoppingBusiness;
        }

        public async Task<SelectTripResponse> SelectTrip(SelectTripRequest selectTripRequest, HttpContext httpContext = null)
        {
            try
            {
                var response = await _shoppingBusiness.SelectTrip(selectTripRequest, httpContext);
                if (response.Availability != null)
                {
                    response.Availability.FareWheel = null;

                    if (response.Availability.Reservation != null && response.Availability.Reservation.TravelersCSL != null)
                        response.Availability.Reservation.NumberOfTravelers = response.Availability.Reservation.TravelersCSL.Count();
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError("ReShopSelectTrip error {exception} , {stackTrace} and {sessionId}", ex, ex.StackTrace, selectTripRequest.SessionId);
                return default;
            }

        }
    }
}
