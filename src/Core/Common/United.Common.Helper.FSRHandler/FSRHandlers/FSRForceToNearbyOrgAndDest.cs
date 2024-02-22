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
    public class FSRForceToNearbyOrgAndDest : IRule<MOBFSRAlertMessage>
    {
        private ShopResponse _cslShopResponse;
        private MOBSHOPShopRequest _restShopRequest;
        private int _tripIndex = 0;
        private ShopResponse cslResponseClone;
        private MOBSHOPShopRequest restShopRequestClone;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IHeaders _headers;
        public FSRForceToNearbyOrgAndDest(ShopResponse cslShopResponse
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
            _headers= headers;

        }

        public FSRForceToNearbyOrgAndDest(ShopResponse cslResponseClone
            , MOBSHOPShopRequest restShopRequestClone
            , IHeaders headers)
        {
            this.cslResponseClone = cslResponseClone;
            this.restShopRequestClone = restShopRequestClone;
            _headers = headers;
        }

        public bool ShouldExecuteRule()
        {
            return (_cslShopResponse != null && _cslShopResponse.Trips != null && _cslShopResponse.Trips.Count > _tripIndex
                   && _cslShopResponse.Trips[_tripIndex].Flights != null && _cslShopResponse.Trips[_tripIndex].Flights.Any() // has flights
                   && _cslShopResponse.Trips[_tripIndex].DestinationRecommendedAirportUsed // force to nearby destination
                   && _cslShopResponse.Trips[_tripIndex].OriginRecommendedAirportUsed); // force to nearby origin
        }

        public async Task<MOBFSRAlertMessage> Execute()
        {
            string headerMsg = _configuration.GetValue<string>("FSRForceNearByOriginAndDestinationMsgHeader");
            string bodyMsg = _configuration.GetValue<string>("FSRForceNearbyOriginAndDestinationMsgBody");

            #region Get Origin and Destination Airport Codes

            MOBDisplayBagTrackAirportDetails origin = null;
            MOBDisplayBagTrackAirportDetails destination = null;
            string originDesc = string.Empty;
            string destinationDesc = string.Empty;
            string originCode = _restShopRequest.Trips[_tripIndex].Origin.ToUpper();
            string destinationCode = _restShopRequest.Trips[_tripIndex].Destination.ToUpper();
            AirportDetailsList airportList = new AirportDetailsList();
            if (_configuration.GetValue<bool>("EnableFecthAirportNameFromCSL"))
            {
                airportList.AirportsList = StaticDataLoader.GetAirports();
            }
            else
            {
                airportList = await _sessionHelperService.GetSession<AirportDetailsList>(_headers.ContextValues.SessionId, new AirportDetailsList().ObjectName, new List<string> { _headers.ContextValues.SessionId, new AirportDetailsList().ObjectName });
            }

            if (airportList != null)
            {
                origin = airportList.AirportsList?.FirstOrDefault(airport => airport.AirportCode.Equals(originCode, System.StringComparison.OrdinalIgnoreCase));
                destination = airportList.AirportsList?.FirstOrDefault(airport => airport.AirportCode.Equals(destinationCode, System.StringComparison.OrdinalIgnoreCase));
            }

            if (origin == null && destination == null)
            {
                // compose codes
                var airportCodes = string.Join("','", new[] { _restShopRequest.Trips[_tripIndex].Origin.ToUpper(), _restShopRequest.Trips[_tripIndex].Destination.ToUpper() });

                // Get from database
                var airports = await _shoppingUtility.GetAirportNamesList("'" + airportCodes + "'");

                originDesc = airports.FirstOrDefault(airport => airport.AirportCode.Equals(originCode, System.StringComparison.OrdinalIgnoreCase)).AirportNameMobile;
                destinationDesc = airports.FirstOrDefault(airport => airport.AirportCode.Equals(destinationCode, System.StringComparison.OrdinalIgnoreCase)).AirportNameMobile;
            }
            else if (origin == null)
            {
                originDesc = await _shoppingUtility.GetAirportName(originCode);
                destinationDesc = destination.AirportNameMobile;
            }
            else if (destination == null)
            {
                destinationDesc = await _shoppingUtility.GetAirportName(destinationCode);
                originDesc = origin.AirportNameMobile;
            }
            else
            {
                originDesc = origin.AirportNameMobile;
                destinationDesc = destination.AirportNameMobile;
            }

            #endregion

            if (_configuration.GetValue<bool>("DisableFSRAlertHideFeature") == true && GeneralHelper.IsApplicationVersionGreater(_restShopRequest.Application.Id, _restShopRequest.Application.Version.Major, "Android_EnableFSRNearByAirportAlert_AppVersion", "IPhone_EnableFSRNearByAirportAlert_AppVersion", "", "", true, _configuration)
                && _shoppingUtility.IsFSRNearByAirportAlertEnabled(_restShopRequest.Application.Id, _restShopRequest.Application.Version.Major)
                && _shoppingUtility.HasNearByAirport(_cslShopResponse))
                return null;

            MOBFSRAlertMessage enhancement = new MOBFSRAlertMessage();
            if (_configuration.GetValue<bool>("DisableFSRAlertHideFeature") == true || GeneralHelper.IsApplicationVersionGreater(_restShopRequest.Application.Id, _restShopRequest.Application.Version.Major, "Android_EnableFSRNearByAirportAlert_AppVersion", "IPhone_EnableFSRNearByAirportAlert_AppVersion", "", "", true, _configuration) == false)
                enhancement.HeaderMessage = string.Format(headerMsg, _cslShopResponse.Trips[_tripIndex].OriginDecoded, _cslShopResponse.Trips[_tripIndex].DestinationDecoded);
            enhancement.BodyMessage = string.Format(bodyMsg, _cslShopResponse.Trips[_tripIndex].OriginDecoded, _cslShopResponse.Trips[_tripIndex].DestinationDecoded);
            enhancement.MessageTypeDescription = FSRAlertMessageType.None;
            enhancement.MessageType = 0;
            enhancement.Buttons = null;
            enhancement.RestHandlerType = MOBFSREnhancementType.WithResultsForceNearByOrigAndDest.ToString();

            await Task.Delay(0);
            return enhancement;
        }
    }
}
