using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.Serilog;

namespace United.Mobile.DataAccess.MemberSignIn
{
    public class DataVaultService : IDataVaultService
    {
        private readonly ICacheLog<DataVaultService> _logger;
        private readonly IResilientClient _resilientClient;

        public DataVaultService(ICacheLog<DataVaultService> logger, [KeyFilter("DataVaultTokenClientKey")] IResilientClient resilientClient)
        {
            _logger = logger;
            _resilientClient = resilientClient;          
        }

        public async Task<string> GetPersistentToken(string token, string requestData, string url, string sessionId)
        {

            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                         {"Accept", "application/json"},
                         { "Authorization", token }
                     };
            var gPTokenData = await _resilientClient.GetHttpAsyncWithOptions(url, headers);

            if (gPTokenData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service AccountManagement-GetPersistentToken-service {@RequestUrl} {url} error {response}", _resilientClient?.BaseURL, gPTokenData.url, gPTokenData.response);
                if (gPTokenData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(gPTokenData.response);
            }

            _logger.LogInformation("CSL service AccountManagement-GetPersistentToken-service {@RequestUrl} and {Response}", gPTokenData.url, gPTokenData.response);

            return gPTokenData.response;

        }
        public async Task<string> PersistentToken(string token, string requestData, string url, string sessionId)
        {

            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };
            _logger.LogInformation("CSL service AccountManagement-GetPersistentToken1-service{@Request}", requestData);
            var pTokenData = await _resilientClient.PostHttpAsyncWithOptions(url, requestData, headers);

            if (pTokenData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service AccountManagement-GetPersistentToken1-service {@RequestUrl} error {Response}", pTokenData.url, pTokenData.response);
                if (pTokenData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(pTokenData.response);
            }

            _logger.LogInformation("CSL service AccountManagement-GetPersistentToken1-service {@RequestUrl} and {Response}", pTokenData.url, pTokenData.response);

            return pTokenData.response;

        }
        public async Task<string> GetCCTokenWithDataVault(string token, string requestData, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
            string path = "/AddPayment";

            _logger.LogInformation("GetCCTokenWithDataVault CSL Service {Request}", ConfigUtility.RemoveEncryptedCardNumberFromDatavaultCSLRequest(requestData));
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, requestData, headers).ConfigureAwait(false);

            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("GetCCTokenWithDataVault CSL Service {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(responseData.response);
            }
            _logger.LogInformation("GetCCTokenWithDataVault CSL Service {@RequestUrl}, {Response}", responseData.url, ConfigUtility.RemoveEncryptedCardNumberFromDatavaultCSLResponse(responseData));
            return responseData.response;

        }

     
        public async Task<string> GetRSAWithDataVault(string token, string requestData, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
            string path = string.Format("/{0}/RSA", requestData);
            
                var responseData = await _resilientClient.GetHttpAsyncWithOptions(path, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("GetRSAWithDataVault CSL Service {@RequestUrl} {url} error {response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }

                _logger.LogInformation("GetRSAWithDataVault CSL Service {@RequestUrl}  {response}", responseData.url, responseData.response);
               return responseData.response;
            
        }

        public async Task<(T response, long callDuration)> GetCSLWithDataVault<T>(string token, string action, string sessionId, string jsonRequest)
        {

            string returnValue = string.Empty;


            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", action);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, jsonRequest, headers).ConfigureAwait(false);

            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-GetCSLWithDataVault {@RequestUrl} error {response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }
            returnValue = responseData.response;

            _logger.LogError("CSL service-GetCSLWithDataVault {@RequestUrl} for {response}", responseData.url, responseData.response);


            return (returnValue == null) ? default : (JsonConvert.DeserializeObject<T>(returnValue),0);
        }

        public async Task<string> GetDecryptedTextFromDataVault(string token, string action, string sessionId, string jsonRequest)
        {
            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
                string path = string.Format("/{0}", action);
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, jsonRequest, headers).ConfigureAwait(false);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service GetDecryptedTextFromDataVault CSL Service {@RequestUrl} error {response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }

                _logger.LogInformation("CSL service GetDecryptedTextFromDataVault CSL Service {@RequestUrl} , {response}", responseData.url, responseData.response);
                return responseData.response;           
        }
    }
}
