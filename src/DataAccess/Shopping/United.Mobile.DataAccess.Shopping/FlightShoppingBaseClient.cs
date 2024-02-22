using Autofac.Features.AttributeFilters;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.Shopping
{
    public class FlightShoppingBaseClient : IFlightShoppingBaseClient
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<FlightShoppingBaseClient> _logger;
        public FlightShoppingBaseClient([KeyFilter("FlightShoppingBaseClientKey")] IResilientClient resilientClient
            , ICacheLog<FlightShoppingBaseClient> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }
        
        public async Task<T> PostAsync<T>(string token, string sessionId, string action, string request)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestUrl = string.Format("/{0}", action);

            _logger.LogInformation("CSL flightShopping-" + action + "-service {@requestUrl} {@requestData} and {sessionId}", requestUrl, request, sessionId);
            var response = await _resilientClient.PostHttpAsyncWithOptions(requestUrl, request, headers);
            if (response.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL flightShopping " + action + " service {@RequestUrl} error {Response}", response.url, response.response);
                if (response.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }
            _logger.LogInformation("CSL flightShopping " + action + " service {@RequestUrl} {Response}", response.url, response.response);
            return (response.response == null) ? default : JsonConvert.DeserializeObject<T>(response.response);
        }
    }
}
