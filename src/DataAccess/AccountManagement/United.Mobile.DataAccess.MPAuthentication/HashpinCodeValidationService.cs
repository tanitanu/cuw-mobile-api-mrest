using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.MPAuthentication
{
    public class HashpinCodeValidationService: IHashpinCodeValidationService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<HashpinCodeValidationService> _logger;

        public HashpinCodeValidationService([KeyFilter("HashPinCodeDataAccessService")] IResilientClient resilientClient, ICacheLog<HashpinCodeValidationService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }

        public async Task<bool> ValidateHashPinCode(string accountNumber, string hashPinCode, int applicationId, string deviceId, string appVersion, string sessionId)
        {
            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", string.Empty },
                          {"Accept","application/json" }
                     };
                string requestData = string.Format("?accountNumber={0}&hashPinCode={1}&applicationId={2}&deviceId={3}&appVersion={4}&mpSignIn=false", accountNumber, hashPinCode, applicationId, deviceId, appVersion);
                var responseData = await _resilientClient.GetHttpAsyncWithOptions(requestData, headers).ConfigureAwait(false);
                
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("ValidateHashPinCode service {@RequestUrl} {url} error {response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }
                
                _logger.LogInformation("ValidateHashPinCode service {@RequestUrl}", responseData.url);
                return true;
          
        }
    }
}
