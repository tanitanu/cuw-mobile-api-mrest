using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Configuration;
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
    public class CustomerProfileOwnerService : ICustomerProfileOwnerService
    {
        private readonly ICacheLog<CustomerProfileOwnerService> _logger;
        private readonly IResilientClient _resilientClient;
        private readonly IConfiguration _configuration;

        public CustomerProfileOwnerService(
              [KeyFilter("CSLGetProfileOwnerServiceKey")] IResilientClient resilientClient
            , IConfiguration configuration
            , ICacheLog<CustomerProfileOwnerService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
            _configuration = configuration;
        }
        public async Task<T> GetProfileOwnerInfo<T>(string token, string sessionId, string mpNumber)
        {
            string actionName = string.Empty;
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestData = string.Format("{0}", mpNumber);

            _logger.LogInformation("CSL service GetProfile Owner service {Request}", requestData);

            var responseData = await _resilientClient.GetHttpAsyncWithOptions(requestData, headers).ConfigureAwait(false);

            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service GetProfile Owner service {@RequestUrl} {url} error {Response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }
            _logger.LogInformation("CSL service GetProfile Owner service {@RequestUrl}, {Response}", responseData.url, responseData.response);

            if (!_configuration.GetValue<bool>("DisableProfileOwnerJsonConversionInCloud"))
            {
                return DataContextJsonSerializer.NewtonSoftDeserialize<T>(responseData.response);
            }
            else
            {
                return JsonConvert.DeserializeObject<T>(responseData.response);
            }
        }
    }
}
