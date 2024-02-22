using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model.CSLModels;
using United.Mobile.Model.Internal.Exception;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.Customer
{
    public class CustomerProfileCreditCardsService : ICustomerProfileCreditCardsService
    {
        private readonly ICacheLog<CustomerProfileCreditCardsService> _logger;
        private readonly IResilientClient _resilientClient;
        private readonly IConfiguration _configuration;

        public CustomerProfileCreditCardsService(
              [KeyFilter("CSLGetProfileCreditCardsServiceKey")] IResilientClient resilientClient
            , ICacheLog<CustomerProfileCreditCardsService> logger
            , IConfiguration configuration)
        {
            _resilientClient = resilientClient;
            _logger = logger;
            _configuration = configuration;
        }
        public async Task<T> GetProfileCreditCards<T>(string token, string sessionId, string mpNumber)
        {
            string actionName = string.Empty;
           
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
                string requestData = string.Format("{0}", mpNumber);

                var responseData = await _resilientClient.GetHttpAsyncWithOptions(requestData, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service GetProfile CreditCards service {@RequestUrl} {url} error {response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                _logger.LogInformation("CSL service GetProfile CreditCards service {@RequestUrl},{response}", responseData.url, responseData.response);
                return JsonConvert.DeserializeObject<T>(responseData.response);
            
        }

        public async Task<T> UpsertCreditCard<T>(string token, string sessionId, string mpNumber, string jsonRequest)
        {
            using (_logger.BeginTimedOperation("Total time taken for UpdateCreditCard service call", transationId: sessionId))
            {
                var headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };

                var path = $"{mpNumber}";
                _logger.LogInformation("CSL service-UpdateCreditCard Request:{@Request}", jsonRequest);
                var data = await _resilientClient.PostHttpAsyncWithOptions(path, jsonRequest, headers);
                if (data.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-UpdateCreditCard {@RequestUrl} error {Response}", data.url, data.response);
                    if (data.statusCode == HttpStatusCode.BadRequest)
                    {
                        var responseData = Newtonsoft.Json.JsonConvert.DeserializeObject<Mobile.Model.Common.CslResponse<CreditCardAwsResponse>>(data.response);
                        if (responseData.Errors != null && responseData.Errors.Count() > 0)
                        {
                            if (responseData.Errors.Any(error => error.Code == "400"))
                            {
                                string exceptionMessage = _configuration.GetValue<string>("UnableToInsertCreditCardToProfileErrorMessage");
                                throw new MOBUnitedException(exceptionMessage);
                            }
                        }

                    }
                    else
                    {
                        _logger.LogError("CSL service-UpdateCreditCard {@RequestUrl} error {Response}", data.url, data.response);
                        if (data.statusCode != HttpStatusCode.BadRequest)
                            throw new Exception(data.response);
                    }
                }
                _logger.LogInformation("CSL service-UpdateCreditCard {@RequestUrl},{Response}", data.url, data.response);
                return JsonConvert.DeserializeObject<T>(data.response);
            }
        }
    }
}

