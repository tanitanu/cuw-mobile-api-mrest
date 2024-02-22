using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Utility.Helper;
using ShopResponse = United.Services.FlightShopping.Common.ShopResponse;

namespace United.Common.Helper.FSRHandler
{
    public class FSRForceToNearbyDestination : IRule<MOBFSRAlertMessage>
    {
        //FSRForceNearByOrigin
        private ShopResponse _cslShopResponse;
        private MOBSHOPShopRequest _restShopRequest;
        private int _tripIndex = 0;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IHeaders _headers;

        public FSRForceToNearbyDestination(ShopResponse cslShopResponse
            , MOBSHOPShopRequest restShopRequest
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , IConfiguration configuration
            , IHeaders headers)
        {
            _cslShopResponse = cslShopResponse;
            _restShopRequest = restShopRequest;
            _tripIndex = cslShopResponse.LastTripIndexRequested - 1;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
            _configuration = configuration;
            _headers = headers;
        }
        public bool ShouldExecuteRule()
        {
            return (_cslShopResponse != null && _cslShopResponse.Trips != null && _cslShopResponse.Trips.Count > _tripIndex
                    && _cslShopResponse.Trips[_tripIndex].Flights != null && _cslShopResponse.Trips[_tripIndex].Flights.Any() // has flights
                    && _cslShopResponse.Trips[_tripIndex].DestinationRecommendedAirportUsed); // force to nearby destination
        }

        public async Task<MOBFSRAlertMessage> Execute()
        {
            string headerMsg = _configuration.GetValue<string>("FSRForceNearByOriginOrDestinationMsgHeader");
            string bodyMsg = _configuration.GetValue<string>("FSRForceNearByOriginOrDestinationMsgBody");

            #region Get Destination Airport Codes

            MOBDisplayBagTrackAirportDetails destination = null;
            string destinationDesc = string.Empty;
            AirportDetailsList airportList = new AirportDetailsList();
            if (_configuration.GetValue<bool>("EnableFecthAirportNameFromCSL"))
            {
                airportList.AirportsList = StaticDataLoader.GetAirports();
            }
            else
            { 
                airportList = await _sessionHelperService.GetSession<AirportDetailsList>(_headers.ContextValues.SessionId, new AirportDetailsList().ObjectName, new List<string> { _headers.ContextValues.SessionId, new AirportDetailsList().ObjectName }).ConfigureAwait(false);
            }
            if (airportList != null)
                destination = airportList.AirportsList.FirstOrDefault(airport => airport.AirportCode.Equals(_restShopRequest.Trips[_tripIndex].Destination.ToUpper(), System.StringComparison.OrdinalIgnoreCase));

            if (destination != null)
                destinationDesc = destination.AirportNameMobile;
            else
                destinationDesc = await _shoppingUtility.GetAirportName(_restShopRequest.Trips[_tripIndex].Destination.ToUpper());

            //destinationDesc = destination != null ? destination.AirportInfo : Utility.GetAirportName(_restShopRequest.Trips[_tripIndex].Destination.ToUpper());

            #endregion
            if (_configuration.GetValue<bool>("DisableFSRAlertHideFeature") == true && GeneralHelper.IsApplicationVersionGreater(_restShopRequest.Application.Id, _restShopRequest.Application.Version.Major, "Android_EnableFSRNearByAirportAlert_AppVersion", "IPhone_EnableFSRNearByAirportAlert_AppVersion", "", "", true, _configuration)
                &&  _shoppingUtility.IsFSRNearByAirportAlertEnabled(_restShopRequest.Application.Id, _restShopRequest.Application.Version.Major)
                && _shoppingUtility.HasNearByAirport(_cslShopResponse))
                return null;

            MOBFSRAlertMessage enhancement = new MOBFSRAlertMessage();
            if(_configuration.GetValue<bool>("DisableFSRAlertHideFeature") == true || GeneralHelper.IsApplicationVersionGreater(_restShopRequest.Application.Id, _restShopRequest.Application.Version.Major, "Android_EnableFSRNearByAirportAlert_AppVersion", "IPhone_EnableFSRNearByAirportAlert_AppVersion", "", "", true, _configuration) == false)
                enhancement.HeaderMessage = string.Format(headerMsg, _cslShopResponse.Trips[_tripIndex].DestinationDecoded);
            enhancement.BodyMessage = string.Format(bodyMsg, _cslShopResponse.Trips[_tripIndex].DestinationDecoded);
            enhancement.MessageTypeDescription = FSRAlertMessageType.None;
            enhancement.MessageType = 0;
            enhancement.Buttons = null;
            enhancement.RestHandlerType = MOBFSREnhancementType.WithResultsForceNearByDestination.ToString();

            return enhancement;
        }
    }
}
