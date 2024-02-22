using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.MPAuthentication
{
    public class MPSecurityCheckDetailsService : IMPSecurityCheckDetailsService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<MPSecurityCheckDetailsService> _logger;

        public MPSecurityCheckDetailsService([KeyFilter("MPSecurityCheckDetailsClientKey")] IResilientClient resilientClient, ICacheLog<MPSecurityCheckDetailsService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }

        public async Task<string> GetMPSecurityCheckDetails(string token, string requestData, string sessionId)
        {
            using (_logger.BeginTimedOperation("Total time taken for GetMPSecurityCheckDetails service call", sessionId))
            {
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
                string path = "/GetProfile";
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, requestData, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("AccountManagement-GetMPSecurityCheckDetails - Request url for get profile {requestUrl} error {@response} for {sessionId}", responseData.url, responseData.response, sessionId);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }

                _logger.LogInformation("AccountManagement-GetMPSecurityCheckDetails - Request url for get profile {requestUrl} error {@response} for {sessionId}", responseData.url, responseData.response, sessionId);
                return responseData.response;
            }
        }

        public async Task<string> UpdateTfaWrongAnswersFlag(string token, string requestData, string sessionId)
        {
            using (_logger.BeginTimedOperation("Total time taken for UpdateTfaWrongAnswersFlag service call", sessionId))
            {
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
                string path = "/UpdateTfaWrongAnswersFlag";
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, requestData, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("AccountManagement-UpdateTfaWrongAnswersFlag - Request url for get profile {requestUrl} error {@response} for {sessionId}", responseData.url, responseData.response, sessionId);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }

                _logger.LogInformation("AccountManagement-UpdateTfaWrongAnswersFlag - Request url for get profile {requestUrl} error {@response} for {sessionId}", responseData.url, responseData.response, sessionId);
                return responseData.response;
            }
        }

       
    }
}
