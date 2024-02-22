using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;


namespace United.Mobile.DataAccess.UnitedClub
{
    public class PersistentTokenByAccountNumberTokenService : IPersistentTokenByAccountNumberTokenService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<PersistentTokenByAccountNumberTokenService> _logger;

        public PersistentTokenByAccountNumberTokenService([KeyFilter("PersistentTokenByAccountNumberTokenClientKey")] IResilientClient resilientClient, ICacheLog<PersistentTokenByAccountNumberTokenService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }

        public async Task<string> GetPersistentTokenUsingAccountNumberToken(string token, string accountNumberToken, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
            string requestData = string.Format("{0}/RSA", accountNumberToken);

                var responseData = await _resilientClient.GetHttpAsyncWithOptions(requestData, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-United ClubPasses {@RequestUrl} {url} error {Response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new System.Exception(responseData.response);
                }

                _logger.LogInformation("CSL service-United ClubPasses {@RequestUrl} , {Response}", responseData.url, responseData.response);
                return responseData.response;
           
        }
    }
}
