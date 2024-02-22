using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using ShopResponse = United.Services.FlightShopping.Common.ShopResponse;

namespace United.Common.Helper.FSRHandler
{
    public class FSRWithResultOriginOrDestinationWithAllAirports : IRule<MOBFSRAlertMessage>
    {
        private ShopResponse _cslShopResponse;
        private MOBSHOPShopRequest _restShopRequest;
        private int _tripIndex = 0;
        private readonly string _listOfAirportsNeedDecode = string.Empty;
        private readonly IConfiguration _configuration;

        public FSRWithResultOriginOrDestinationWithAllAirports(
            ShopResponse cslShopResponse
            , MOBSHOPShopRequest restShopRequest
            , IConfiguration configuration)
        {
            _cslShopResponse = cslShopResponse;
            _restShopRequest = restShopRequest;
            _tripIndex = cslShopResponse.LastTripIndexRequested - 1;
            _configuration = configuration;
            _listOfAirportsNeedDecode = _configuration.GetValue<string>("ListOfAirportsNeedDecode").ToUpper(); //JFK|FNL
        }
        
        public bool ShouldExecuteRule()
        {
            return (_restShopRequest!=null && _restShopRequest.CameFromFSRHandler &&_cslShopResponse != null && _cslShopResponse.Trips != null && _cslShopResponse.Trips.Count > _tripIndex
                && _cslShopResponse.Trips[_tripIndex].Flights != null && _cslShopResponse.Trips[_tripIndex].Flights.Any() // has flights
                && ( _listOfAirportsNeedDecode.Contains(_restShopRequest.Trips[_tripIndex].Destination.ToUpper())
                || _listOfAirportsNeedDecode.Contains(_restShopRequest.Trips[_tripIndex].Origin.ToUpper())));
        }

        public async Task<MOBFSRAlertMessage> Execute()
        {
            string headerMsg = _configuration.GetValue<string>("FSRWithResultsSuggestNearbyMsgHeader");
            string bodyMsg = string.Empty;
            
            string airportDecodeOrigin = string.Empty, airportDecodeDestination = string.Empty;


            if (_listOfAirportsNeedDecode.Contains(_restShopRequest.Trips[_tripIndex].Destination.ToUpper())) {

                airportDecodeDestination = _cslShopResponse.Trips[_tripIndex].DestinationDecoded;
            }
            if (_listOfAirportsNeedDecode.Contains(_restShopRequest.Trips[_tripIndex].Origin.ToUpper()))
            {
                airportDecodeOrigin = _cslShopResponse.Trips[_tripIndex].OriginDecoded;
            }
            if(!string.IsNullOrEmpty(airportDecodeDestination) && !string.IsNullOrEmpty(airportDecodeOrigin))
            {
                bodyMsg = string.Format(_configuration.GetValue<string>("FSRWithResultsSuggestNearbyOriginAndDestinationMsgBody"),airportDecodeOrigin,airportDecodeDestination);
            }
            else
            if (!string.IsNullOrEmpty(airportDecodeDestination))
            {
               bodyMsg =string.Format(_configuration.GetValue<string>("FSRWithResultsSuggestNearbyOriginOrDestinationMsgBody"),airportDecodeDestination);
            }
            else {
                bodyMsg = string.Format(_configuration.GetValue<string>("FSRWithResultsSuggestNearbyOriginOrDestinationMsgBody"), airportDecodeOrigin);
            }

            await Task.Delay(0);
            return new MOBFSRAlertMessage()
            {
                HeaderMessage = string.Format(headerMsg),
                BodyMessage = bodyMsg,
                MessageTypeDescription = FSRAlertMessageType.SuggestNearbyAirports,
                MessageType = 0,
                //Buttons = new System.Collections.Generic.List<MOBFSRAlertMessageButton> { button },
                RestHandlerType = MOBFSREnhancementType.WithResultsSuggestNearByOrigins.ToString(),
                AlertType = MOBFSRAlertMessageType.Information.ToString()
            };
        }
    }
}
