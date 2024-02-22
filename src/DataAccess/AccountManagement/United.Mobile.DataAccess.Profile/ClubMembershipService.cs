using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.Profile
{
    public class ClubMembershipService: IClubMembershipService
    {
        private readonly ICacheLog<ClubMembershipService> _logger;
        private readonly IResilientClient _resilientClient;

        public ClubMembershipService(ICacheLog<ClubMembershipService> logger, [KeyFilter("ClubMembershipClientKey")] IResilientClient resilientClient)
        {
            _logger = logger;
            _resilientClient = resilientClient;
        }

        public async Task<T> GetClubMembership<T>(string token, string requestData, string sessionId)
        {
           
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };
                var glbData = await _resilientClient.GetHttpAsyncWithOptions(requestData, headers);

                if (glbData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service AccountManagement-GetClubMembership-service {@RequestUrl} {url} error {response}", _resilientClient?.BaseURL, glbData.url, glbData.response);
                    if (glbData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(glbData.response);
                }

                _logger.LogInformation("CSL service AccountManagement-GetClubMembership-service {@RequestUrl}", glbData.url);

                return JsonConvert.DeserializeObject<T>( glbData.response);
           
        }
    }
}
