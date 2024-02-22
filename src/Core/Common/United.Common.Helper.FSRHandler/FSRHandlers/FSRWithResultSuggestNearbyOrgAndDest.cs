using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using ShopResponse = United.Services.FlightShopping.Common.ShopResponse;

namespace United.Common.Helper.FSRHandler
{
    public class FSRWithResultSuggestNearbyOrgAndDest : IRule<MOBFSRAlertMessage>
    {
        private ShopResponse _cslShopResponse;
        private MOBSHOPShopRequest _restShopRequest;
        private int _tripIndex = 0;
        private readonly IConfiguration _configuration;

        public FSRWithResultSuggestNearbyOrgAndDest(ShopResponse cslShopResponse, MOBSHOPShopRequest restShopRequest, IConfiguration configuration)
        {
            _cslShopResponse = cslShopResponse;
            _restShopRequest = restShopRequest;
            _tripIndex = cslShopResponse.LastTripIndexRequested - 1;
            _configuration = configuration;
        }
       
        public bool ShouldExecuteRule()
        {
            return (_cslShopResponse != null && _cslShopResponse.Trips != null && _cslShopResponse.Trips.Count > _tripIndex
                    && _cslShopResponse.Trips[_tripIndex].Flights != null && _cslShopResponse.Trips[_tripIndex].Flights.Any() // has flights
                    && _cslShopResponse.Trips[_tripIndex].DestinationTriggeredAirport && _cslShopResponse.Trips[_tripIndex].OriginTriggeredAirport);
        }

        public async Task<MOBFSRAlertMessage> Execute()
        {
            string headerMsg = _configuration.GetValue<string>("FSRWithResultsSuggestNearbyMsgHeader");
            string bodyMsg = _configuration.GetValue<string>("FSRWithResultsSuggestNearbyOriginAndDestinationMsgBody");

            var button = new MOBFSRAlertMessageButton();
            _restShopRequest.Trips[_tripIndex].SearchNearbyOriginAirports = true;
            _restShopRequest.Trips[_tripIndex].SearchNearbyDestinationAirports = true;
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
                MessageTypeDescription = FSRAlertMessageType.SuggestNearbyAirports,
                MessageType = 0,
                Buttons = new System.Collections.Generic.List<MOBFSRAlertMessageButton> { button },
                RestHandlerType = MOBFSREnhancementType.WithResultsSuggestNearByOrigsAndDests.ToString(),
            };
        }
    }
}
