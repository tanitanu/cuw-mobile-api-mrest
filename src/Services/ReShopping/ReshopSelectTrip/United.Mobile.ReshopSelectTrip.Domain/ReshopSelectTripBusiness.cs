using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading.Tasks;
using United.Mobile.Model.Shopping;

namespace United.Mobile.ReshopSelectTrip.Domain
{
    public class ReshopSelectTripBusiness : IReshopSelectTripBusiness
    {
        private readonly ICacheLog<ReshopSelectTripBusiness> _logger;
        private readonly IShopTripsBusiness _shopTripsBusiness;

        public ReshopSelectTripBusiness(ICacheLog<ReshopSelectTripBusiness> logger
            , IShopTripsBusiness shopTripsBusiness)
        {
            _logger = logger;
            _shopTripsBusiness = shopTripsBusiness;
        }

        public async Task<SelectTripResponse> SelectTrip(SelectTripRequest selectTripRequest)
        {
            var response = await _shopTripsBusiness.SelectTrip(selectTripRequest);
            if (response.Availability != null)
            {
                response.Availability.FareWheel = null;

                if (response.Availability.Reservation != null && response.Availability.Reservation.TravelersCSL != null)
                    response.Availability.Reservation.NumberOfTravelers = response.Availability.Reservation.TravelersCSL.Count();
            }

            return response;
        }
    }
}
