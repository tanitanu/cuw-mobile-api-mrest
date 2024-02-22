using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.MemberSignIn
{
    public class UtilitiesService : IUtilitiesService
    {
        private readonly ICacheLog<UtilitiesService> _logger;
        private readonly IResilientClient _resilientClient;

        public UtilitiesService(ICacheLog<UtilitiesService> logger, [KeyFilter("UtilitiesServiceClientKey")] IResilientClient resilientClient)
        {
            _logger = logger;
            _resilientClient = resilientClient;
        }

        public async Task<string> ValidateMPNames(string token, string requestData, string sessionId, string path = "")
        {
           
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };

                var vMPNamesData = await _resilientClient.PostHttpAsyncWithOptions(path, requestData, headers);

                if (vMPNamesData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service AccountManagement-ValidateMPNames-service {@RequestUrl} error {response} ", vMPNamesData.url, vMPNamesData.response);
                    if (vMPNamesData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(vMPNamesData.response);
                }

                _logger.LogInformation("CSL service AccountManagement-ValidateMPNames-service {@RequestUrl}", vMPNamesData.url);

                return vMPNamesData.response;
            
        }
        public async Task<T> ValidateMileagePlusNames<T>(string token, string requestData, string sessionId, string path)
        {
            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };
                string url = string.Format("/profilevalidation/api/{0}", path);
                var profileValidateData = await _resilientClient.PostHttpAsyncWithOptions(url, requestData, headers);

                if (profileValidateData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service AccountManagement-ValidateMileagePlusNames-service {@RequestUrl} error {response}", profileValidateData.url, profileValidateData.response);
                    if (profileValidateData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(profileValidateData.response);
                }

                _logger.LogInformation("CSL service AccountManagement-ValidateMileagePlusNames-service {@RequestUrl}", profileValidateData.url);

                return JsonConvert.DeserializeObject<T>(profileValidateData.response);
            
        }

        public async Task<T> ValidatePhoneWithCountryCode<T>(string token, string path, string requestData, string sessionId)
        {
           
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };

                var pnrData = await _resilientClient.PostHttpAsyncWithOptions(path, requestData, headers);

                if (pnrData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service Account Management-GetAndValidateStateCode-service {@RequestUrl} error {response}", pnrData.url, pnrData.response);
                    if (pnrData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(pnrData.response);
                }

                _logger.LogInformation("CSL service Account Management-GetAndValidateStateCode-service {@RequestUrl}", pnrData.url);

                return JsonConvert.DeserializeObject<T>(pnrData.response);
            
        }
    }
}
