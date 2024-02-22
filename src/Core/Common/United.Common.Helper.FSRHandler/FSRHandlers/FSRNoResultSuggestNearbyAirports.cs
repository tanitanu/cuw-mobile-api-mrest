using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using ShopResponse = United.Services.FlightShopping.Common.ShopResponse;

namespace United.Common.Helper.FSRHandler
{
    public class FSRNoResultSuggestNearbyAirports : IRule<MOBFSRAlertMessage>
    {
        private ShopResponse _cslShopResponse;
        private MOBSHOPShopRequest _restShopRequest;
        private int _tripIndex = 0;
        private readonly IConfiguration _configuration;

        public FSRNoResultSuggestNearbyAirports(ShopResponse cslShopResponse, MOBSHOPShopRequest restShopRequest, IConfiguration configuration)
        {
            _cslShopResponse = cslShopResponse;
            _restShopRequest = restShopRequest;
            _tripIndex = cslShopResponse.LastTripIndexRequested - 1;
            _configuration = configuration;
        }
        
        public bool ShouldExecuteRule()
        {
            return (_cslShopResponse != null && _cslShopResponse.Trips != null && _cslShopResponse.Trips.Count > _tripIndex
                    && (!_configuration.GetValue<bool>(_configuration.GetValue<string>("CorporateConcurBooking")) || !_restShopRequest.IsCorporateBooking)) // exclude corporatebooking
                    && (_cslShopResponse.Trips[_tripIndex].Flights == null || !_cslShopResponse.Trips[_tripIndex].Flights.Any()); // no results
        }

        public async Task<MOBFSRAlertMessage> Execute()
        {
            string headerMsg = _configuration.GetValue<string>("FSRNoResultsMsgHeader");
            string bodyMsg = _configuration.GetValue<string>("FSRNoResultsSuggestNearbyMsgBody");

            var button = new MOBFSRAlertMessageButton();

            if (_configuration.GetValue<bool>("EnableFSRNoResultsMTFix"))
            {
                foreach (var trip in _restShopRequest.Trips)
                {
                    trip.SearchNearbyOriginAirports = true;
                    trip.SearchNearbyDestinationAirports = true;
                }
            }
            else
            {
                _restShopRequest.Trips[_tripIndex].SearchNearbyOriginAirports = true;
                _restShopRequest.Trips[_tripIndex].SearchNearbyDestinationAirports = true;
            }
            _restShopRequest.CameFromFSRHandler = true;
            _restShopRequest.SessionId = null;
            _restShopRequest.GetNonStopFlightsOnly = true;
            _restShopRequest.GetFlightsWithStops = false;
            button.UpdatedShopRequest = _restShopRequest;
            button.ButtonLabel = _configuration.GetValue<string>("FSRSearchNearbyButtonLabel");

            await Task.Delay(0);
            return new MOBFSRAlertMessage()
            {
                HeaderMessage = string.Format(headerMsg),
                BodyMessage = string.Format(bodyMsg, _cslShopResponse.Trips[_tripIndex].OriginDecoded, _cslShopResponse.Trips[_tripIndex].DestinationDecoded),
                MessageTypeDescription = FSRAlertMessageType.NoResults,
                MessageType = 0,
                AlertType = MOBFSRAlertMessageType.Information.ToString(),
                Buttons = new System.Collections.Generic.List<MOBFSRAlertMessageButton> { button },
                RestHandlerType = MOBFSREnhancementType.NoResultsSuggestNearByAirports.ToString(),
            };
        }
    }
}
