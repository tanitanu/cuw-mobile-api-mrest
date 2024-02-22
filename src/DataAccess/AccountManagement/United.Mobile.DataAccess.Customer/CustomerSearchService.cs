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

namespace United.Mobile.DataAccess.Customer
{
    public class CustomerSearchService : ICustomerSearchService
    {
        private readonly ICacheLog<CustomerDataService> _logger;
        private readonly IResilientClient _resilientClient;

        public CustomerSearchService(
              [KeyFilter("CustomerSearchClientKey")] IResilientClient resilientClient
            , ICacheLog<CustomerDataService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }

        public async Task<string> Search(string token, string sessionId, string path = "")
        {
            var headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };

            var data = await _resilientClient.GetHttpAsyncWithOptions(path, headers);

            if (data.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service AccountManagement-Search-service {@RequestUrl} {url} error {response}", _resilientClient?.BaseURL, data.url, data.response);
                if (data.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(data.response);
            }
            _logger.LogInformation("CSL service AccountManagement-Search-service {@RequestUrl}", data.url);
            return data.response;
        }
    }
}
