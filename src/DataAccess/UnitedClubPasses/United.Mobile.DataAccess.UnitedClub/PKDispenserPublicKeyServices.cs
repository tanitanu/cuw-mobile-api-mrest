using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.UnitedClub
{
    public class PKDispenserPublicKeyServices:IPKDispenserPublicKeyService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<PKDispenserPublicKeyServices> _logger;

        public PKDispenserPublicKeyServices([KeyFilter("PKDispenserPublicKeyClientKey")] IResilientClient resilientClient, ICacheLog<PKDispenserPublicKeyServices> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }

        public async Task<string> GetPKDispenserPublicKeyServices(string token, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
            string path = string.Empty;
            _logger.LogInformation("United ClubPasses GetPKDispenserPublicKeyServices Service- GetPKDispenserPublicKeyServices  {token} for {sessionId}", token, sessionId);
           
                var responseData = await _resilientClient.GetHttpAsyncWithOptions(path, headers).ConfigureAwait(false);


                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-United ClubPasses GetPKDispenserPublicKeyServices Service {@RequestUrl} {url} error {response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }

                _logger.LogInformation("CSL service-United ClubPasses GetPKDispenserPublicKeyServices Service {@RequestUrl} , {response}", responseData.url, responseData.response);
                return responseData.response;
           
        }
    }
}
