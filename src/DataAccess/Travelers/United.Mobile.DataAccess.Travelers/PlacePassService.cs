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
    public class PlacePassService: IPlacePassService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<PlacePassService> _logger;
        public PlacePassService([KeyFilter("PlacePassClientKey")] IResilientClient resilientClient
            , ICacheLog<PlacePassService> logger)
            
        {
            _resilientClient = resilientClient;
            _logger = logger;

        }
        public async Task<(T response, long callDuration)> GetPlacePass<T>(string token,  string path, string sessionId)
        {
            IDisposable timer = null;
            string returnValue = string.Empty;
            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };

                _logger.LogInformation("AccountManagement-GetPlacePass-service {@Request}", path);

                var responseData = await _resilientClient.GetHttpAsyncWithOptions(path, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-AccountManagement-GetPlacePass-service {@RequestUrl} {url} error {Response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }
                returnValue = responseData.response;
                _logger.LogInformation("CSL service-AccountManagement-GetPlacePass-service {@RequestUrl} and {Response}", responseData.url, responseData.response);

           
            return (returnValue == null) ? default : (JsonConvert.DeserializeObject<T>(returnValue), 0);

        }
    }
}
