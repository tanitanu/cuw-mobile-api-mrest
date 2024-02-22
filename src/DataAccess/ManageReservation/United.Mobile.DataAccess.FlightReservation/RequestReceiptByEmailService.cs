using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.FlightReservation
{
    public class RequestReceiptByEmailService:IRequestReceiptByEmailService
    {
        private readonly ICacheLog<RequestReceiptByEmailService> _logger;
        private readonly IResilientClient _resilientClient;

        public RequestReceiptByEmailService(ICacheLog<RequestReceiptByEmailService> logger, [KeyFilter("SendReceiptByEmailClientKey")] IResilientClient resilientClient)
        {
            _logger = logger;
            _resilientClient = resilientClient;
        } 

        public async Task<string> PostReceiptByEmailViaCSL(string token, string request, string sessionId, string path)
        {
           
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-PostReceiptByEmailViaCSL {@RequestUrl} error {response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                _logger.LogInformation("CSL service-PostReceiptByEmailViaCSL {@RequestUrl}", responseData.url);
                return responseData.response;
            
        }

    }
}
