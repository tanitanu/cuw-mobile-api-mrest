using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.Serilog;

namespace United.Mobile.DataAccess.Common
{
    public class SSOTokenKeyService : ISSOTokenKeyService
    {
        private readonly ICacheLog<SSOTokenKeyService> _logger;
        private readonly IResilientClient _resilientClient;
        private readonly IConfiguration _configuration;
        public SSOTokenKeyService([KeyFilter("SSOTokenClientKey")] IResilientClient resilientClient, ICacheLog<SSOTokenKeyService> logger, IConfiguration configuration)
        {
            _resilientClient = resilientClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> DecryptToken(string token, string sessionId, string encryptedData)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                           {"Accept", "application/json"},
                          { "Authorization", token }
                     };
          
                string path = "?token=" + encryptedData;
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path,string.Empty, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service PartnerSingleSignOn Decrypt {@RequestUrl} error {response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }

               
                _logger.LogInformation("CSL service PartnerSingleSignOn Decrypt {@RequestUrl}", responseData.url);
                return responseData.response;
            
        }

        public async Task<string> EncryptKey(string token, string sessionId, string Key)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                           {"Accept", "application/json"},
                          { "Authorization", token }
                     };
           
                string path = "?key=" + Key;
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, string.Empty, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service PartnerSingleSignOn Encrypt {@RequestUrl} error {response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }

                
                _logger.LogInformation("CSL service PartnerSingleSignOn Encrypt {@RequestUrl}", responseData.url);
                return responseData.response;
            
        }

        public async Task<string> GetPartnerSSOToken(string token, string sessionId, string mpNumber)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                           {"Accept", "application/json"},
                          { "Authorization", token }
                     };
          
                string path = $"?mpNumber={mpNumber}&expirationInMinutes={ _configuration.GetValue<int>("expirationInMinutesPartnerSSO")}";
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, string.Empty, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service PartnerSingleSignOn GetPartnerSSOToken {@RequestUrl} error {response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }

                
                _logger.LogInformation("CSL service PartnerSingleSignOn GetPartnerSSOToken {@RequestUrl}", responseData.url);
                return responseData.response;
            
        }

    }
}
