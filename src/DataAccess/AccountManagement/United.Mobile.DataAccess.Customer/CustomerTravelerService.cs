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
    public class CustomerTravelerService : ICustomerTravelerService
    {
        private readonly ICacheLog<CustomerProfileTravelerService> _logger;
        private readonly IResilientClient _resilientClient;
        private readonly IConfiguration _configuration;

        public CustomerTravelerService(
             [KeyFilter("CSLGetProfileTravelerServiceKey")] IResilientClient resilientClient
            , IConfiguration configuration
            , ICacheLog<CustomerProfileTravelerService> logger)
        {
            _resilientClient = resilientClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<T> InsertTraveler<T>(string token, string sessionId, string loyaltyId, string jsonRequest)
        {
            var headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };
            var path = $"LoyaltyId/{loyaltyId}";
            var data = await _resilientClient.PostHttpAsyncWithOptions(path, jsonRequest, headers);
            _logger.LogInformation("CSL service InsertTraveler all traveler service{@RequestUrl} {Request}", data.url, jsonRequest);
            if (data.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service InsertTraveler {@RequestUrl} error {response}", data.url, data.response);
                if (data.statusCode == HttpStatusCode.BadRequest)
                {
                    var cslErrorResponseData = JsonConvert.DeserializeObject<CslResponse<TravelerAwsResponse>>(data.response);
                    if (cslErrorResponseData.Errors != null && cslErrorResponseData.Errors.Count() > 0)
                    {
                        var errorMessage = cslErrorResponseData.Errors.FirstOrDefault(error => error.Code == "400.106" || error.Code == "400.14");
                        if (errorMessage != null)
                        {
                            throw new MOBUnitedException(_configuration.GetValue<string>("ValidateMPNameMisMatchErrorMessage"));
                        }
                        else
                        {
                            throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                        }
                    }
                    else
                    {
                        throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                    }
                }
                if (data.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            return JsonConvert.DeserializeObject<T>(data.response);

        }

        public async Task<T> UpdateTravelerBase<T>(string token, string sessionId, string customerId, string jsonRequest)
        {
            using (_logger.BeginTimedOperation("Total time taken for UpdateTravelerBase service call", transationId: sessionId))
            {
                var headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };

                var path = $"CustomerId/{customerId}";
                var data = await _resilientClient.PutAsync(path, jsonRequest, headers);

                if (data.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("UpdateTravelerBase-service {requestUrl} error {response} for {sessionId}", data.url, data.statusCode, sessionId);
                    if (data.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(data.response);
                }

                _logger.LogInformation("UpdateTravelerBase-service {requestUrl} and {sessionId}", data.url, sessionId);

                return JsonConvert.DeserializeObject<T>(data.response);
            }
        }
    }
}
