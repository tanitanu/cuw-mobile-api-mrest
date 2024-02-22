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

namespace United.Mobile.DataAccess.MemberSignIn
{
    public class MembershipHistoryService : IMembershipHistoryService
    {
        private readonly ICacheLog<MembershipHistoryService> _logger;
        private readonly IResilientClient _resilientClient;

        public MembershipHistoryService(ICacheLog<MembershipHistoryService> logger, [KeyFilter("MembershipHistoryClientKey")] IResilientClient resilientClient)
        {
            _logger = logger;
            _resilientClient = resilientClient;
        }

        public async Task<T> GetMembershipHistory<T>(string token, string mpNumber, string sessionId)
        {
           
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };

                var memberShipHistoryData = await _resilientClient.GetHttpAsyncWithOptions( mpNumber, headers);

                if (memberShipHistoryData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service AccountManagement-GetMembershipHistory-service {@RequestUrl} {url} error {response}", _resilientClient?.BaseURL, memberShipHistoryData.url, memberShipHistoryData.response);
                    if (memberShipHistoryData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(memberShipHistoryData.response);
                }

                _logger.LogInformation("CSL service AccountManagement-GetMembershipHistory-service {@RequestUrl}", memberShipHistoryData.url);

                return JsonConvert.DeserializeObject<T>(memberShipHistoryData.response);
            
        }
    }
}
