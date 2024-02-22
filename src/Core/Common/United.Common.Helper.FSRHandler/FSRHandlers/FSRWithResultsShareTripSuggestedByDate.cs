using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;

namespace United.Common.Helper.FSRHandler
{
    public class FSRWithResultsShareTripSuggestedByDate : IRule<MOBFSRAlertMessage>
    {
        private MOBSHOPShopRequest _restShopRequest;
        private readonly IConfiguration _configuration;

        public FSRWithResultsShareTripSuggestedByDate(MOBSHOPShopRequest restShopRequest, IConfiguration configuration)
        {
            _restShopRequest = restShopRequest;
            _configuration = configuration;
        }

        public bool ShouldExecuteRule()
        {
            return _restShopRequest.IsShareTripSearchAgain;
        }

        public async Task<MOBFSRAlertMessage> Execute()
        {
            string headerMsg = _configuration.GetValue<string>("MetaTripFlightSoldOutExceptionMessage");
            string bodyMsg = _configuration.GetValue<string>("ShareTripOtherAvailabilityMesageInShopCall");

            await Task.Delay(0);
            return new MOBFSRAlertMessage()
            {
                HeaderMessage = headerMsg,
                BodyMessage = bodyMsg,
                MessageTypeDescription = FSRAlertMessageType.NoResults,
                MessageType = 0,
                //Buttons = new System.Collections.Generic.List<MOBFSRAlertMessageButton> (),// NO button
                RestHandlerType = MOBFSREnhancementType.WithResultsShareTripSuggestedByDate.ToString(),
            };
        }
    }
}
