using System;
using System.Collections.Generic;
using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Http;
using United.Utility.Helper;

namespace United.Mobile.DataAccess.FlightShopping
{
    public class PayPalCreditCardService: IPayPalCreditCardService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<PayPalCreditCardService> _logger;

        public PayPalCreditCardService([KeyFilter("FlightShoppingClientKey")] IResilientClient resilientClient, ICacheLog<PayPalCreditCardService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }

        public async Task<string> GetPayPalCreditCardResponse(string token, string requestData, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
            string path = "/PayPalCreditCard/AcquirePayPalCreditCard";
            var responseData = await _resilientClient.PostHttpAsync(path, requestData, headers).ConfigureAwait(false);
            var response = responseData.Item1;
            var statusCode = responseData.Item2;
            var url = responseData.Item3;

            _logger.LogInformation("United ClubPasses CSL Service {requestUrl} {Response} for {sessionId}", url, response, sessionId);

            if (statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("United ClubPasses CSL Service {@RequestUrl} error {Response}", url, response);
                if (statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(response);
            }
            return response;
        }
    }
}
