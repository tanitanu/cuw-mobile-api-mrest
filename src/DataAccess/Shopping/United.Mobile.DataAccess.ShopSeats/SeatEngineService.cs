﻿using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.ShopSeats
{
    public class SeatEngineService : ISeatEngineService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<SeatEngineService> _logger;
        private readonly IConfiguration _configuration;

        public SeatEngineService(
            [KeyFilter("SeatEngineClientKey")] IResilientClient resilientClient
            , ICacheLog<SeatEngineService> logger
            , IConfiguration configuration)
        {
            _resilientClient = resilientClient;
            _logger = logger;
            _configuration = configuration;
        }
        public async Task<string> GetSeatMapDetail(string token, string action, string request, string sessionId)
        {
            _logger.LogInformation("CSL service-GetSeatMapDetail {token} ,{action}, {request},{sessionId}", token, action, request, sessionId);

            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},//"application/xml;"
                          { "Authorization", token }
                     };
            
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(action, request, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetSeatMapDetail {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                _logger.LogInformation("CSL service-GetSeatMapDetail {@RequestUrl}, {Response}", responseData.url, responseData.response);
                return (responseData.response);
            
        }
    }
}
