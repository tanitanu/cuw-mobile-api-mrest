using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.FlightShopping
{
    public class LMXInfo : ILMXInfo
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<LMXInfo> _logger;
        private readonly IConfiguration _configuration;
        public LMXInfo([KeyFilter("FlightShoppingClientKey")] IResilientClient resilientClient
            , ICacheLog<LMXInfo> logger
            , IConfiguration configuration)
        {
            _resilientClient = resilientClient;
            _logger = logger;
            _configuration = configuration;

        }

        public async Task<T> GetProductInfo<T>(string token, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", "GetLmxQuote");
            _logger.LogInformation("CSL service-GetProductInfo {@RequestUrl}, {@Request}", path, request);

            var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-GetProductInfo {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }
            _logger.LogInformation("CSL service-GetProductInfo {@RequestUrl}, {Response}", responseData.url, responseData.response);
            return JsonConvert.DeserializeObject<T>(responseData.response);

        }
        
        public async Task<T> GetLmxRTIInfo<T>(string token, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", "GetLmxQuote");


            _logger.LogInformation("CSL service-GetLmxRTIInfo {@Request}", request);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);

            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-GetLmxRTIInfo {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }

            _logger.LogInformation("CSL service-GetLmxRTIInfo {@RequestUrl}, {Response}", responseData.url, responseData.response);
            return JsonConvert.DeserializeObject<T>(responseData.response);

        }
        public async Task<T> GetLmxFlight<T>(string token, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", "GetLmxQuote");

           
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetLmxFlight {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                _logger.LogInformation("CSL service-GetLmxFlight {@RequestUrl}", responseData.url);
                return JsonConvert.DeserializeObject<T>(responseData.response);            
        }

    }
}
