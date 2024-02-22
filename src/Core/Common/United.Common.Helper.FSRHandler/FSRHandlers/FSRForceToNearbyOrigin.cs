using Microsoft.Extensions.Configuration;
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
    public class FSRForceToNearbyOrigin : IRule<MOBFSRAlertMessage>
    {
        private ShopResponse _cslShopResponse;
        private MOBSHOPShopRequest _restShopRequest;
        private int _tripIndex = 0;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly IConfiguration _configuration;

        public FSRForceToNearbyOrigin(ShopResponse cslShopResponse
            , MOBSHOPShopRequest restShopRequest
            , ISessionHelperService sessionHelperService
            , IShoppingUtility shoppingUtility
            , IConfiguration configuration)
        {
            _cslShopResponse = cslShopResponse;
            _restShopRequest = restShopRequest;
            _tripIndex = cslShopResponse.LastTripIndexRequested - 1;
            _sessionHelperService = sessionHelperService;
            _shoppingUtility = shoppingUtility;
            _configuration = configuration;
        }
        public bool ShouldExecuteRule()
        {
            return (_cslShopResponse != null && _cslShopResponse.Trips != null && _cslShopResponse.Trips.Count > _tripIndex
                    && _cslShopResponse.Trips[_tripIndex].Flights != null && _cslShopResponse.Trips[_tripIndex].Flights.Any() // has flights
                    && _cslShopResponse.Trips[_tripIndex].OriginRecommendedAirportUsed); // force to nearby origin
        }

        public async Task<MOBFSRAlertMessage> Execute()
        {
            string headerMsg = _configuration.GetValue<string>("FSRForceNearByOriginOrDestinationMsgHeader");
            string bodyMsg = _configuration.GetValue<string>("FSRForceNearByOriginOrDestinationMsgBody");

            #region Get Origin Airport Codes

            MOBDisplayBagTrackAirportDetails origin = null;
            string originDesc = string.Empty;
            AirportDetailsList airportList = new AirportDetailsList();
            airportList.AirportsList = StaticDataLoader.GetAirports();//_sessionHelperService.GetSession<AirportDetailsList>(Headers.ContextValues);

            if (airportList != null)
                origin = airportList.AirportsList.FirstOrDefault(airport => airport.AirportCode.Equals(_restShopRequest.Trips[_tripIndex].Origin.ToUpper(), System.StringComparison.OrdinalIgnoreCase));

            if (origin != null)
                originDesc = origin.AirportNameMobile;
            else
                originDesc = await _shoppingUtility.GetAirportName(_restShopRequest.Trips[_tripIndex].Origin.ToUpper());

            //originDesc = origin != null ? origin.AirportInfo : Utility.GetAirportName(_restShopRequest.Trips[_tripIndex].Origin.ToUpper());

            #endregion

            if (_configuration.GetValue<bool>("DisableFSRAlertHideFeature") == true && GeneralHelper.IsApplicationVersionGreater(_restShopRequest.Application.Id, _restShopRequest.Application.Version.Major, "Android_EnableFSRNearByAirportAlert_AppVersion", "IPhone_EnableFSRNearByAirportAlert_AppVersion", "", "", true, _configuration)
                && _shoppingUtility.IsFSRNearByAirportAlertEnabled(_restShopRequest.Application.Id, _restShopRequest.Application.Version.Major)
                && _shoppingUtility.HasNearByAirport(_cslShopResponse))
                return null;

            MOBFSRAlertMessage enhancement = new MOBFSRAlertMessage();
            if (_configuration.GetValue<bool>("DisableFSRAlertHideFeature") == true || GeneralHelper.IsApplicationVersionGreater(_restShopRequest.Application.Id, _restShopRequest.Application.Version.Major, "Android_EnableFSRNearByAirportAlert_AppVersion", "IPhone_EnableFSRNearByAirportAlert_AppVersion", "", "", true, _configuration) == false)
                enhancement.HeaderMessage = string.Format(headerMsg, _cslShopResponse.Trips[_tripIndex].OriginDecoded);
            enhancement.BodyMessage = string.Format(bodyMsg, _cslShopResponse.Trips[_tripIndex].OriginDecoded);
            enhancement.MessageTypeDescription = FSRAlertMessageType.None;
            enhancement.MessageType = 0;
            enhancement.Buttons = null;
            enhancement.RestHandlerType = MOBFSREnhancementType.WithResultsForceNearByOrigin.ToString();

            await Task.Delay(0);
            return enhancement;
        }
    }
}
