using System;
using System.Collections.Generic;
using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Http;
using Newtonsoft.Json;
using United.Service.Presentation.ProductResponseModel;
using United.Utility.Helper;

namespace United.Mobile.DataAccess.UnitedClub
{
    public class MECSLFullfillmentService:IMECSLFullfillmentService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<MECSLFullfillmentService> _logger;

        public MECSLFullfillmentService([KeyFilter("MECSLFullfillmentClientKey")] IResilientClient resilientClient, ICacheLog<MECSLFullfillmentService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }

        public async Task<string> GetMECSLFullfillment(string token, string requestData, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
            string path = string.Empty;
            _logger.LogInformation("United ClubPasses CSL Service- GetMECSLFullfillment {requestPayload} ", requestData);
            
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, requestData, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-United ClubPasses GetMECSLFullfillment Service {requestUrl} error {@response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new System.Exception(responseData.response);
                }
                
                _logger.LogInformation("CSL service-United ClubPasses GetMECSLFullfillment Service {requestUrl} , {@response}", responseData.url, responseData.response);
                return responseData.response;
            
        }
    }
}
