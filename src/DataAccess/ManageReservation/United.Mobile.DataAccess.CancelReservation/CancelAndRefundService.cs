using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.CancelReservation
{
    public class CancelAndRefundService : ICancelAndRefundService
    {
        private readonly ICacheLog<CancelAndRefundService> _logger;
        private readonly IResilientClient _resilientClient;
        public CancelAndRefundService(ICacheLog<CancelAndRefundService> logger, [KeyFilter("CancelAndRefundClientKey")] IResilientClient resilientClient)
        {
            _logger = logger;
            _resilientClient = resilientClient;
        }

        public async Task<string> PutCancelReservation(string token, string sessionId, string path,string requestData)
        {
           
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
                var responseData = await _resilientClient.PutAsync(path,requestData,headers).ConfigureAwait(false);

                if (responseData.statusCode!= HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-PutCancelReservation {@RequestUrl} error {response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                _logger.LogInformation("CSL service-PutCancelReservation {@requestUrl}", responseData.url);
                return responseData.response; //TODO
            
        }
    }
}
