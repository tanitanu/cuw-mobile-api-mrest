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

namespace United.Mobile.DataAccess.Customer
{
    public class CustomerCorporateProfileService : ICustomerCorporateProfileService
    {
        private readonly ICacheLog<CustomerCorporateProfileService> _logger;
        private readonly IResilientClient _resilientClient;

        public CustomerCorporateProfileService(
              [KeyFilter("CSLCorporateGetServiceKey")] IResilientClient resilientClient
            , ICacheLog<CustomerCorporateProfileService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }
        public async Task<T> GetCorporateprofile<T>(string token, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
            string path = "/CorpProfile";
            _logger.LogInformation("CSL Service GetCorporateprofile {Request}", request);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers).ConfigureAwait(false);

            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL Service GetCorporateprofile {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(responseData.response);
            }
            _logger.LogInformation("CSL Service GetCorporateprofile {@RequestUrl} {Response}", responseData.url, responseData.response);
            return JsonConvert.DeserializeObject<T>(responseData.response);
        }
        public async Task<T> GetCorporateCreditCards<T>(string token, string request, string sessionId)
        {

            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
            string path = "/CorpFOP";
            _logger.LogInformation("CSL Service Corporate Creditcard {Request}", request);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers).ConfigureAwait(false);

            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL Service Corporate Creditcard  Request url for get profile {@RequestUrl} error {response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(responseData.response);
            }
            _logger.LogInformation("CSL Service Response-Corporate Creditcard {@RequestUrl} {CSLResponse}", responseData.url, JsonConvert.SerializeObject(responseData));
            return JsonConvert.DeserializeObject<T>(responseData.response);

        }
        public async Task<T> GetCorporatePolicyResponse<T>(string token, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
            string path = "/CorpPolicy";
            _logger.LogInformation("CSL Service Corporate TravelPolicy {Request}", request);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers).ConfigureAwait(false);

            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL Service TravelPolicy  Request url for get profile {@RequestUrl} error {response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(responseData.response);
            }
            _logger.LogInformation("CSL Service Response-Corporate TravelPolicy {@RequestUrl} {CSLResponse}", responseData.url, JsonConvert.SerializeObject(responseData));
            return JsonConvert.DeserializeObject<T>(responseData.response);
        }
        public async Task<T> GetCorpMpNumberValidation<T>(string token, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
            string path = "/CorpMpNumberValidation";
            _logger.LogInformation("CSL Service CorpMpNumberValidation {Request}", request);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers).ConfigureAwait(false);

            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL Service CorpMpNumberValidation {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(responseData.response);
            }
            _logger.LogInformation("CSL Service CorpMpNumberValidation {@RequestUrl} {Response}", responseData.url, responseData.response);
            return JsonConvert.DeserializeObject<T>(responseData.response);
        }
    }
}
