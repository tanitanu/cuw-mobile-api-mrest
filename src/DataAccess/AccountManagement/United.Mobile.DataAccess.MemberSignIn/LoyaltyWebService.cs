﻿using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.MemberSignIn
{
    public class LoyaltyWebService : ILoyaltyWebService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<LoyaltyWebService> _logger;
        private readonly IConfiguration _configuration;

        public LoyaltyWebService(
              [KeyFilter("LoyaltyWebClientKey")] IResilientClient resilientClient
            , ICacheLog<LoyaltyWebService> logger
            , IConfiguration configuration)
        {
            _resilientClient = resilientClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> GetLoyaltyData(string token, string mileagePlusNumber, string sessionId)
        {
           
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
                string requestData = string.Format("/{0}?elite_year={1}", mileagePlusNumber, DateTime.Today.Year);
                var responseData = await _resilientClient.GetHttpAsyncWithOptions(requestData, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetLoyaltyWeb {@RequestUrl} {url} error {response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
               
                _logger.LogError("CSL service-GetLoyaltyWeb {@RequestUrl}", responseData.url);
                return responseData.response;
            
        }
    }
}
