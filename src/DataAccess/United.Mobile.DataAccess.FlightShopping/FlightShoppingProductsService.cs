using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.Serilog;

namespace United.Mobile.DataAccess.Travelers
{
    public class FlightShoppingProductsService : IFlightShoppingProductsService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<FlightShoppingProductsService> _logger;

        public FlightShoppingProductsService(
              [KeyFilter("FlightShoppingClientKey")] IResilientClient resilientClient
            , ICacheLog<FlightShoppingProductsService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }
        public async Task<string> GetProducts(string token, string sessionId, string request)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            // string path = string.Format("/{0}", action);
            string requestData = string.Format("/GetProducts");

            _logger.LogInformation("CSL service-GetProducts Request:{@Request}", request);

            
                try
                {
                    var response = await _resilientClient.PostHttpAsyncWithOptions(requestData, request, headers);

                    if (response.statusCode != HttpStatusCode.OK)
                    {
                        _logger.LogError("CSL service-GetProducts {@RequestUrl} error {Response}", response.url, response.response);
                        if (response.statusCode != HttpStatusCode.BadRequest)
                            return default;
                    }

                    //var CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
                    _logger.LogInformation("CSL service-GetProducts {@RequestUrl}, {Response}", response.url, response.response);
                    return response.response;
                }
                catch (Exception ex)
                {
                    _logger.LogError("CSL service-GetProducts error {@Exception}", JsonConvert.SerializeObject(ex));
                }

                return default;
        }
        //public async Task<string> RegisterOffer(string token, string sessionId, string request)
        //{
        //    Dictionary<string, string> headers = new Dictionary<string, string>
        //             {
        //                  {"Accept", "application/json"},
        //                  { "Authorization", token }
        //             };

        //    string path = string.Format("/RegisterOffer");
        //    using (_logger.BeginTimedOperation("Total time taken for RegisterOffer business call", transationId: sessionId))
        //    {
        //        var response = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);

        //        if (response.statusCode != HttpStatusCode.OK)
        //        {
        //            _logger.LogError("CSL service-RegisterOffer {requestUrl} error {response} for {sessionId}", response.url, response.response, sessionId);
        //            if (response.statusCode != HttpStatusCode.BadRequest)
        //                return default;
        //        }

        //        _logger.LogInformation("CSL service-RegisterOffer {requestUrl} and {sessionId}", response.url, sessionId);
        //        return response.response;
        //    }
        //}
        public async Task<T> MilesAndMoneyOption<T>(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", action);
            _logger.LogInformation("CSL service-ApplyCSLMilesPlusMoneyOptions Request:{request} Path:{path} with {sessionId}", JsonConvert.SerializeObject(request), path, sessionId);

                try
                {
                    var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                    if (responseData.statusCode != HttpStatusCode.OK)
                    {
                        _logger.LogError("CSL service-ApplyCSLMilesPlusMoneyOptions {@RequestUrl} error {Response}", responseData.url, responseData.response);
                        if (responseData.statusCode != HttpStatusCode.BadRequest)
                            return default;
                    }

                    _logger.LogInformation("CSL service-ApplyCSLMilesPlusMoneyOptions {@RequestUrl}", responseData.url);
                    return (responseData.response == null) ? default : JsonConvert.DeserializeObject<T>(responseData.response);
                }
                catch (Exception ex)
                {
                    _logger.LogError("CSL service-ApplyCSLMilesPlusMoneyOptions error {stackTrace} for {sessionId}", ex.StackTrace, sessionId);
                }

                return default;
           
        }

        public async Task<string> ApplyCSLMilesPlusMoneyOptions(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", action);

            _logger.LogInformation("CSL service - ApplyCSLMilesPlusMoneyOptions- Request:{request} Path:{path}SessionId:{sessionId}", JsonConvert.SerializeObject(request), path, sessionId);


            
           
                try
                {
                    var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                    if (responseData.statusCode != HttpStatusCode.OK)
                    {
                        _logger.LogError("CSL service-ApplyCSLMilesPlusMoneyOptions {@RequestUrl} error {Response}}", responseData.url, responseData.response);
                        if (responseData.statusCode != HttpStatusCode.BadRequest)
                            return default;
                    }

                    _logger.LogInformation("CSL service-ApplyCSLMilesPlusMoneyOptions {@RequestUrl}", responseData.url);
                    return responseData.response;
                }

                catch (Exception ex)
                {
                    _logger.LogError("CSL service-ApplyCSLMilesPlusMoneyOptions error {stackTrace} for {sessionId}", ex.StackTrace, sessionId);
                }

                return default;
           
        }

        public async Task<string> GetCSLMilesPlusMoneyOptions(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", action);

            
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetCSLMilesPlusMoneyOptions {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                _logger.LogInformation("CSL service-GetCSLMilesPlusMoneyOptions {@RequestUrl}", responseData.url);
                return responseData.response;
           
        }

        public async Task<string> GetTripInsuranceInfo(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", action);

            _logger.LogInformation("CSL service - GetTripInsuranceInfo- Request:{request} Path:{path}SessionId:{sessionId}", JsonConvert.SerializeObject(request), path, sessionId);

            
                try
                {
                    var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                    if (responseData.statusCode != HttpStatusCode.OK)
                    {
                        _logger.LogError("CSL service-GetTripInsuranceInfo {@RequestUrl} error {Response}", responseData.url, responseData.response);
                        if (responseData.statusCode != HttpStatusCode.BadRequest)
                            return default;
                    }

                    _logger.LogInformation("CSL service-GetTripInsuranceInfo {@RequestUrl} ", responseData.url);
                    return responseData.response;
                }
                catch (Exception ex)
                {
                    _logger.LogError("CSL service-GetTripInsuranceInfo error {stackTrace} for {sessionId}", ex.StackTrace, sessionId);
                }

                return default;

           
        }
        public async Task<string> RegisterFareLocks(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", action);

            
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-RegisterFareLocks {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                _logger.LogInformation("CSL service-RegisterFareLocks {@RequestUrl}", responseData.url);
                return responseData.response;
            
        } 
        public async Task<string> RegisterOffer(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", action);
            _logger.LogInformation("CSL service-RegisterOffer Request:{@Request}", request);

            
                try
                {
                    var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                    if (responseData.statusCode != HttpStatusCode.OK)
                    {
                        _logger.LogError("CSL service-RegisterOffer {@RequestUrl} error {Response}", responseData.url, responseData.response);
                        if (responseData.statusCode != HttpStatusCode.BadRequest)
                            return default;
                    }

                    
                    _logger.LogInformation("CSL service-RegisterOffer {@RequestUrl}, {Response}", responseData.url, responseData.response);
                    return (responseData.response);
                }
                catch (Exception ex)
                {
                    _logger.LogError("CSL service-RegisterOffer error {@Exception}", JsonConvert.SerializeObject(ex));
                }

                return default;
        }

    }
}
