using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.Serilog;

namespace United.Mobile.DataAccess.MerchandizeService
{
    public class PurchaseMerchandizingService : IPurchaseMerchandizingService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<PurchaseMerchandizingService> _logger;
        private readonly IConfiguration _configuration;
        public PurchaseMerchandizingService([KeyFilter("MerchandizingClientKey")] IResilientClient resilientClient
            , ICacheLog<PurchaseMerchandizingService> logger
            , IConfiguration configuration)
        {
            _resilientClient = resilientClient;
            _logger = logger;
            _configuration = configuration;

        }
        public async Task<(T response, long callDuration)> GetInflightPurchaseInfo<T>(string token, string action, string request, string sessionId)
        {
            
            string returnValue = string.Empty;
           
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
                string path = string.Format("/{0}", action);
                _logger.LogInformation("CSL service-GetInflightPurchaseInfo {@Request} {@RequestUrl}", request, path);

                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetInflightPurchaseInfo {@RequestUrl} error {@Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                returnValue = responseData.response;
                _logger.LogInformation("CSL service-GetInflightPurchaseInfo {@RequestUrl} {@Response}", responseData.url, responseData.response);
           

            return (JsonConvert.DeserializeObject<T>(returnValue), 0);
        }

        public async Task<(T response, long callDuration)> GetMerchOfferInfo<T>(string token, string action, string request, string sessionId)
        {
            
            string returnValue = string.Empty;
            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
                string requestData = string.Format("/{0}", action);
                _logger.LogInformation("CSL service-GetMerchOfferInfo Request:{@Request}", request);

                var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetMerchOfferInfo {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                returnValue = responseData.response;
                _logger.LogInformation("CSL service-GetMerchOfferInfo {@RequestUrl}, {Response}", responseData.url, responseData.response);
           

            return (JsonConvert.DeserializeObject<T>(returnValue), 0);
        }

        public async Task<(T response, long callDuration)> GetVendorOfferInfo<T>(string token, string request, string sessionId)
        {
         
            string returnValue = string.Empty;
           
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

                var responseData = await _resilientClient.PostHttpAsyncWithOptions("/getvendoroffers", request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetVendorOfferInfo {@RequestUrl} error {response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                returnValue = responseData.response;
                _logger.LogInformation("CSL service-GetVendorOfferInfo {@RequestUrl} ", responseData.url);
           

            return (JsonConvert.DeserializeObject<T>(returnValue), 0);
        }

        public async Task<(T response, long callDuration)> GetInflightPurchaseEligibility<T>(string token, string request, string sessionId)
        {
           
            string returnValue = string.Empty;
           
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

                string url = "/GetProductEligibility";

                var responseData = await _resilientClient.PostHttpAsyncWithOptions(url, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetInflightPurchaseEligibility {@RequestUrl} error {response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                returnValue = responseData.response;
                _logger.LogInformation("CSL service-GetInflightPurchaseEligibility {@RequestUrl}", responseData.url);
            

            return (JsonConvert.DeserializeObject<T>(returnValue), 0);
        }

    }
}
