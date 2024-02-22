﻿using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.ShopProducts
{
    public class ShoppingCcePromoService : IShoppingCcePromoService
    {

        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<ShoppingCcePromoService> _logger;
        private readonly IConfiguration _configuration;
        
        public ShoppingCcePromoService(
            [KeyFilter("ShoppingCcePromoClientKey")] IResilientClient resilientClient
            , ICacheLog<ShoppingCcePromoService> logger
            , IConfiguration configuration)
        {
            _resilientClient = resilientClient;
            _logger = logger;
            _configuration = configuration;
        }
        public async Task<string> ShoppingCcePromo(string token, string request, string sessionId)
        {
            _logger.LogInformation("CSL service-ShoppingCcePromo {@Request}",request);
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestData = string.Format("SendFeedback");

           
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData, request, headers).ConfigureAwait(false);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-ShoppingCcePromo {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception("Service did not return any reponse");
                }
                
                _logger.LogInformation("CSL service-ShoppingCcePromo {@RequestUrl}, {Response}", responseData.url, responseData.response);
                return responseData.response;
           
        }

        public async Task<string> MerchOffersCceDetails(string token, string request, string sessionId)
        {
            _logger.LogInformation("CSL service-MerchOffersCceDetails {token}, {request} and {sessionId}", token, request, sessionId);
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestData = string.Format("dynamicofferdetail");

            
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData, request, headers).ConfigureAwait(false);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-MerchOffersCceDetails {@RequestUrl} error {Response} for {sessionId}", responseData.url, responseData.response, sessionId);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception("Service did not return any reponse");
                }

                _logger.LogInformation("CSL service-MerchOffersCceDetails {@RequestUrl}, {Response} and {sessionId}", responseData.url, responseData.response, sessionId);
                return (responseData.response);
           
        }
        public async Task<string> ChasePromoFromCCE(string token, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestData = "mobile/messages";
            _logger.LogInformation("CSL service-ChasePromoFromCCE {@Request} {@RequestUrl}", request, requestData);
            
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData, request, headers).ConfigureAwait(false);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-ChasePromoFromCCE {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception("Service did not return any reponse");
                }

                _logger.LogInformation("CSL service-ChasePromoFromCCE {@RequestUrl}, {Response}", responseData.url, responseData.response);
                return (responseData.response);
            
        }

        public async Task<string> DynamicOffers(string token, string request, string sessionId)
        {
            _logger.LogInformation("CSL service-DynamicOffers {token}, {request} and {sessionId}", token, request, sessionId);
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("dynamicoffers");
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers).ConfigureAwait(false);
            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-DynamicOffers {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception("Service did not return any reponse");
            }

            _logger.LogInformation("CSL service-DynamicOffers {@RequestUrl}, {Response}", responseData.url, responseData.response);
            return (responseData.response);

        }
        

    }
}
