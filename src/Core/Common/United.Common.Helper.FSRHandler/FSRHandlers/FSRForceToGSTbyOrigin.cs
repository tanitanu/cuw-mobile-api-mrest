using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;


namespace United.Common.Helper.FSRHandler
{
    public class FSRForceToGSTbyOrigin : IRule<MOBFSRAlertMessage>
    {
        private United.Services.FlightShopping.Common.ShopResponse _cslShopResponse;
        private MOBSHOPShopRequest _restShopRequest;
        private readonly IConfiguration _configuration;
        private int _tripIndex = 0;
        //TODO-SAKTHI-Verify shopResponse Namespacea ambigity
        public FSRForceToGSTbyOrigin(United.Services.FlightShopping.Common.ShopResponse cslShopResponse, MOBSHOPShopRequest restShopRequest, IConfiguration configuration)
        {
            _cslShopResponse = cslShopResponse;
            _restShopRequest = restShopRequest;
            _configuration = configuration;
            _tripIndex = (cslShopResponse != null && cslShopResponse.LastTripIndexRequested > 0) ? cslShopResponse.LastTripIndexRequested - 1 : 0;
        }
        public bool ShouldExecuteRule()
        {
            return _configuration.GetValue<bool>("FSRForceToGSTSwitch") && _cslShopResponse.Trips[0].Flights != null && _cslShopResponse.Trips[0].Flights.Count > 0 && _cslShopResponse.Trips[0].Flights[0].OriginCountryCode.ToUpper().Equals("IN"); // For India GST

        }

        public async Task<MOBFSRAlertMessage> Execute()
        {
            string headerMsg = _configuration.GetValue<string>("FSRForceToGSTMsgHeader");
            string bodyMsg = _configuration.GetValue<string>("FSRForceToGSTMsgbody");

            var button = new MOBFSRAlertMessageButton();

            button.UpdatedShopRequest = null;
            button.ButtonLabel = _configuration.GetValue<string>("FSRForceToGSTButtonLabel");

            if (!string.IsNullOrEmpty(_restShopRequest.SearchType))
            {
                if (_restShopRequest.SearchType.ToUpper().Equals("MD"))
                {
                    button.RedirectUrl = _configuration.GetValue<string>("FSRForceToGSTButtonMultiCityURL");
                }
                else
                {
                    button.RedirectUrl = _configuration.GetValue<string>("FSRForceToGSTButtonURL");
                }
            }
            await Task.Delay(0);

            return new MOBFSRAlertMessage()
            {
                HeaderMessage = string.Format(headerMsg),
                BodyMessage = string.Format(bodyMsg),
                MessageTypeDescription = FSRAlertMessageType.None,
                MessageType = 0,
                Buttons = new System.Collections.Generic.List<MOBFSRAlertMessageButton> { button },
                RestHandlerType = MOBFSREnhancementType.WithResultsForceGSTByOrigin.ToString(),

            };
        }
    }
}