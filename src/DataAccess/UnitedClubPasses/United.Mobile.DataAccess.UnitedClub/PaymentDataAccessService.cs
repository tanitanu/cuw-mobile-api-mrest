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
    public class PaymentDataAccessService:IPaymentDataAccessService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<PaymentDataAccessService> _logger;

        public PaymentDataAccessService([KeyFilter("PaymentDataAccessClientKey")] IResilientClient resilientClient, ICacheLog<PaymentDataAccessService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }

        public async Task<string> AddPaymentNew(string token, string requestData, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
            string path = string.Empty;
            _logger.LogInformation("United ClubPasses DB service-AddPaymentNew Request:{Request}", requestData);
            using (_logger.BeginTimedOperation("Total time taken for AddPaymentNew call", transationId: sessionId))
            {
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, requestData, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-United ClubPasses DB service-AddPaymentNew {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }
                
                _logger.LogInformation("CSL service-United ClubPasses DB service-AddPaymentNew {@RequestUrl}  {Response}", responseData.url, responseData.response);
                return responseData.response;
            }
        }
    }
}

