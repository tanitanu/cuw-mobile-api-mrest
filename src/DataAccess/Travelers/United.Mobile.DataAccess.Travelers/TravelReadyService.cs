using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.Serilog;

namespace United.Mobile.DataAccess.Travelers
{
    public class TravelReadyService : ITravelReadyService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<TravelReadyService> _logger;

        public TravelReadyService(
              [KeyFilter("TravelReadyClientKey")] IResilientClient resilientClient
            , ICacheLog<TravelReadyService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }
        public async Task<(T response, long callDuration)> GetCovidLite<T>(string token, string action, string request, string sessionId)
        {
            
            string returnValue = string.Empty;
            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
                string requestData = string.Format("/{0}", action);

                _logger.LogInformation("CSL service-GetCovidLite {@RequestUrl}, {Request}", requestData, request);
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetCovidLite {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                returnValue = responseData.response;
                _logger.LogInformation("CSL service-GetCovidLite {@RequestUrl}, {Response}", responseData.url, responseData.response);
          
            return (returnValue == null) ? default : (JsonConvert.DeserializeObject<T>(returnValue),  0);

        }
    }
}
