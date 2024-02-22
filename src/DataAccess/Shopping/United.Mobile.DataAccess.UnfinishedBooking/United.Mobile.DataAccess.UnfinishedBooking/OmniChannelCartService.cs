using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.UnfinishedBooking
{
    public class OmniChannelCartService:IOmniChannelCartService
    {
        private readonly ICacheLog<OmniChannelCartService> _logger;
        private readonly IResilientClient _resilientClient;

        public OmniChannelCartService(ICacheLog<OmniChannelCartService> logger, [KeyFilter("OmniChannelCartServiceClientKey")] IResilientClient resilientClient)
        {
            _logger = logger;
            _resilientClient = resilientClient;
        }
        public async Task<string> PurgeUnfinshedBookings(string token, string action, string sessionId)
        {
            _logger.LogInformation("CSL service-PurgeUnfinshedBookings {@Action}", action);
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

            
                var responseData = await _resilientClient.DeleteAsync(action, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-PurgeUnfinshedBookings {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }


                _logger.LogInformation("CSL service-PurgeUnfinshedBookings {@RequestUrl}, {Response}", responseData.url, responseData.response);
                return responseData.response;
            
        }
        public async Task<string> GetFlightReservationData(string cartId, bool injectUrl, string token, string transactionId, string sessionId)
        {
            _logger.LogInformation("CSL service-GetFlightReservationData {cartId},{injectUrl} and {transactionId}", cartId, injectUrl, transactionId);

            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

                string requestData = string.Format("cartidandreprice/{0}/US/SEAT%7CBUNDLE/?langCode=en-US&injectUrl={1}", cartId, injectUrl);

                _logger.LogInformation("CSL service-GetFlightReservationData {@requestData},{transactionId} and {sessionId}", requestData, transactionId, sessionId);
                

                    var responseData = await _resilientClient.GetHttpAsyncWithOptions(requestData, headers).ConfigureAwait(false);

                    if (responseData.statusCode != HttpStatusCode.OK)
                    {
                        _logger.LogError("CSL service-GetFlightReservationData {@RequestUrl} {url} error {Response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                        if (responseData.statusCode != HttpStatusCode.BadRequest)
                            throw new Exception("Service did not return any reponse");
                    }

                    _logger.LogInformation("CSL service-GetFlightReservationData {Response}", responseData.response);
                    return responseData.response;
               
            
        }
        public async Task<string> GetCartIdInformation(string cartId, string token, string transactionId, string sessionId)
        {
            _logger.LogInformation("CSL service-GetCartIdInformation {cartId} and {transactionId}", cartId, transactionId);

            
                Dictionary<string, string> headers = new Dictionary<string, string>
                    {
                        {"Accept", "application/json"},
                        { "Authorization", token }
                    };

                string requestData = string.Format("cartid/{0}", cartId);

                _logger.LogInformation("CSL service-GetCartIdInformation {@requestData},{transactionId} and {sessionId}", requestData, transactionId, sessionId);
                

                    var responseData = await _resilientClient.GetHttpAsyncWithOptions(requestData, headers).ConfigureAwait(false);

                    if (responseData.statusCode != HttpStatusCode.OK)
                    {
                        _logger.LogError("CSL service-GetCartIdInformation {@RequestUrl} {url} error {Response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                        if (responseData.statusCode != HttpStatusCode.BadRequest)
                            throw new Exception("Service did not return any reponse");
                    }

                    _logger.LogInformation("CSL service-GetCartIdInformation {Response}", responseData.response);
                    return responseData.response;
           
        }
    }
}
