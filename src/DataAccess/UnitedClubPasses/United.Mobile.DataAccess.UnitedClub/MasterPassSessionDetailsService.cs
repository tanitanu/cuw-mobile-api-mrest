using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.UnitedClub
{
    public class MasterPassSessionDetailsService : IMasterPassSessionDetailsService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<MasterPassSessionDetailsService> _logger;

        public MasterPassSessionDetailsService([KeyFilter("MasterPassSessionDetailsClientKey")] IResilientClient resilientClient, ICacheLog<MasterPassSessionDetailsService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }

        public async Task<string> GetMasterPassSessionDetails(string token, string requestData, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
            string path = string.Empty;
            _logger.LogInformation("United ClubPasses CSL Service- GetMasterPassSessionDetails {requestPayload} ", requestData);
            
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, requestData, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-United ClubPasses {@RequestUrl} error {response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new System.Exception(responseData.response);
                }

                _logger.LogInformation("CSL service-United ClubPasses {@RequestUrl} , {response}", responseData.url, responseData.response);
                return responseData.response;
            
        }

        
    }
}
