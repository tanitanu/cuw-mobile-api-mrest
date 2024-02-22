using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Http;
using United.Utility.Serilog;
using System.Linq;
using United.Utility.Helper;

namespace United.Mobile.DataAccess.ShopTrips
{
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<ShoppingCartService> _logger;
        private readonly IConfiguration _configuration;
        public ShoppingCartService([KeyFilter("ShoppingCartClientKey")] IResilientClient resilientClient
            , ICacheLog<ShoppingCartService> logger
            , IConfiguration configuration)
        {
            _resilientClient = resilientClient;
            _logger = logger;
            _configuration = configuration;

        }
     
        public async Task<T> GetShoppingCartInfo<T>(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", action);

            _logger.LogInformation("CSL service-GetShoppingCartInfo {@Request} {@Url}", request, path);

            
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetShoppingCartInfo {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                _logger.LogInformation("CSL service-GetShoppingCartInfo {@RequestUrl}, {Response}", responseData.url, responseData.response);

                return JsonConvert.DeserializeObject<T>(responseData.response);
          
        }

        public async Task<T> GetCartInformation<T>(string token, string action, string request, string sessionId)
        {
            _logger.LogInformation("CSL service- GetCartInformation parameters Request:{@Request}, Action:{@Action}", request, action);

            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", action);

           
                try
                {
                    var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                    if (responseData.statusCode != HttpStatusCode.OK)
                    {
                        _logger.LogError("CSL service-GetCartInformation {@RequestUrl} error {Response}", responseData.url, responseData.response);
                        if (responseData.statusCode != HttpStatusCode.BadRequest)
                            return default;
                    }

                    
                    _logger.LogInformation("CSL service-GetCartInformation {@RequestUrl}, {Response}", responseData.url, responseData.response);
                    return (responseData.response == null) ? default : JsonConvert.DeserializeObject<T>(responseData.response);
                }
                catch (Exception ex)
                {
                    _logger.LogError("CSL service-GetCartInformation error {@Exception}", JsonConvert.SerializeObject(ex));
                }

                return default;

           
        }

        public async Task<T> GetProductDetailsFromCartID<T>(string token, string cartID, string sessionId)
        {
            _logger.LogInformation("CSL service-GetProductDetailsFromCartID {token}, {cartID} and {sessionId}", token, cartID, sessionId);
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", cartID);

            
                var responseData = await _resilientClient.GetHttpAsyncWithOptions(path, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetProductDetailsFromCartID {@RequestUrl} {url} error {Response}",_resilientClient?.BaseURL, responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                _logger.LogInformation("CSL service-GetProductDetailsFromCartID {@RequestUrl}, {Response}", responseData.url, responseData.response);
                return (responseData.response == null) ? default : JsonConvert.DeserializeObject<T>(responseData.response);
            
        }

        public async Task<(T response, long callDuration)> RegisterOrRemove<T>(string token, string action, string request, string sessionId)
        {
            _logger.LogInformation("CSL service-RegisterOrRemove {token}, {action}, {request} and {sessionId}", token, action, request, sessionId);
            IDisposable timer = null;
            string returnValue = string.Empty;
            string path = string.Format("/{0}", action);


            using (timer = _logger.BeginTimedOperation("Total time taken for RegisterOrRemove business call", transationId: sessionId))
            {
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };


                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-RegisterOrRemove {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                returnValue = responseData.response;
                _logger.LogInformation("CSL service-RegisterOrRemove {@RequestUrl}, {Response}", responseData.url, responseData.response);
            }
            return (returnValue == null) ? default : (JsonConvert.DeserializeObject<T>(returnValue), timer == null ? 0 : ((TimedOperation)timer).GetElapseTime());
        }

        public async Task<T> GetRegisterTravelers<T>(string token, string sessionId, string jsonRequest)
        {
           
            string returnValue = string.Empty;
            string path = string.Format("/RegisterTravelers");
            _logger.LogInformation("CSL service-GetRegisterTravelers parameters Request:{@Request}", jsonRequest);

            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, jsonRequest, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetRegisterTravelers {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                returnValue = responseData.response;

                _logger.LogInformation("CSL service-GetRegisterTravelers {@RequestUrl} {Response}", responseData.url,responseData.response);
           

            return (returnValue == null) ? default : JsonConvert.DeserializeObject<T>(returnValue);
        }

        public async Task<(T response, long callDuration)> GetFormsOfPayments<T>(string token, string action, string sessionId, string jsonRequest, Dictionary<string, string> additionalHeaders)
        {
            _logger.LogInformation("CSL service-GetFormsOfPayments {token}, {action}, {jsonRequest} for {sessionId}", token, action, jsonRequest, sessionId);
            IDisposable timer = null;

            string returnValue = string.Empty;

            using (timer = _logger.BeginTimedOperation("Total time taken for GetFormsOfPayments service call", transationId: sessionId))
            {
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
                if (_configuration.GetValue<bool>("EnableAdditionalHeadersForMosaicInRFOP"))
                {
                    if (additionalHeaders != null && additionalHeaders.Any())
                    {
                        foreach (var item in additionalHeaders)
                        {
                            headers.Add(item.Key, item.Value);
                        }
                    }
                }

                string path = string.Format("/{0}", action);
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, jsonRequest, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetFormsOfPayments {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                returnValue = responseData.response;

                _logger.LogInformation("CSL service-GetFormsOfPayments {@RequestUrl}, {Response}",  responseData.url, responseData.response);
            }

            return (returnValue == null) ? default : (JsonConvert.DeserializeObject<T>(returnValue), timer == null ? 0 : ((TimedOperation)timer).GetElapseTime());
        }

        public async Task<string> CreateCart(string token, string jsonRequest, string sessionId)
        {
            _logger.LogInformation("CSL service-CreateCart {token} {jsonRequest} for {sessionId}", token, jsonRequest, sessionId);
            
            string returnValue = string.Empty;

            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
                var responseData = await _resilientClient.PostHttpAsyncWithOptions("", jsonRequest, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-CreateCart {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                returnValue = responseData.response;

                _logger.LogInformation("CSL service-CreateCart  {@RequestUrl}, {Response}",  responseData.url, responseData.response);
          

            return JsonConvert.DeserializeObject<string>(returnValue);
        }

        public async Task<T> FareLockReservation<T>(string token, string action, string sessionId, string jsonRequest)
        {
            _logger.LogInformation("CSL service-FareLockReservation {token}, {action}, {jsonRequest} for {sessionId}", token, action, jsonRequest, sessionId);
            
            string returnValue = string.Empty;

            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
                string path = string.Format("{0}", action);
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, jsonRequest, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-FareLockReservation {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                returnValue = responseData.response;

                _logger.LogInformation("CSL service-FareLockReservation {@RequestUrl} {Response}", responseData.url, responseData.response);
           

            return JsonConvert.DeserializeObject<T>(returnValue);
        }

        public async Task<(T response, long callDuration)> GetCart<T>(string token, string sessionId, string jsonRequest)
        {
            _logger.LogInformation("CSL service-GetCart  {token}, {request} and {sessionId}", token, jsonRequest, sessionId);
            IDisposable timer = null;
            string returnValue = string.Empty;

            using (timer = _logger.BeginTimedOperation("Total time taken for GetCart service call", transationId: sessionId))
            {
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
                string path = string.Format("/{0}");
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, jsonRequest, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetCart {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                returnValue = responseData.response;

                _logger.LogInformation("CSL service-GetCart {@RequestUrl} {Response}", responseData.url, responseData.response);
            }

            return (returnValue == null) ? default : (JsonConvert.DeserializeObject<T>(returnValue), timer == null ? 0 : ((TimedOperation)timer).GetElapseTime());
        }

        public async Task<(T response, long callDuration)> GetRegisterSeats<T>(string token, string action, string sessionId, string jsonRequest)
        {
            _logger.LogInformation("CSL service-GetRegisterSeats {token}, {action}, {request} for {sessionId}", token, action, jsonRequest, sessionId);
            IDisposable timer = null;
            string returnValue = string.Empty;

            using (timer = _logger.BeginTimedOperation("Total time taken for GetRegisterSeats service call", transationId: sessionId))
            {
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
                string path = string.Format("{0}", action);
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, jsonRequest, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetRegisterSeats {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                returnValue = responseData.response;

                _logger.LogInformation("CSL service-GetRegisterSeats  {@RequestUrl} {Response}",  responseData.url, responseData.response);
            }

            return (returnValue == null) ? default : (JsonConvert.DeserializeObject<T>(returnValue), timer == null ? 0 : ((TimedOperation)timer).GetElapseTime());
        }

        public async Task<T> RegisterFlights<T>(string token, string action, string request, string sessionId)
        {
            _logger.LogInformation("CSL service - RegisterFlights parameters Token:{token}, Request:{request}, Action:{action} SessionId:{sessionId} ", token, request, action, sessionId);
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", action);


            IDisposable timer = null;
            
                try
                {
                    var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                    if (responseData.statusCode != HttpStatusCode.OK)
                    {
                        _logger.LogError("CSL service-RegisterFlights {@RequestUrl} error {Response}", responseData.url, responseData.response);
                        if (responseData.statusCode != HttpStatusCode.BadRequest)
                            return default;
                    }

                    _logger.LogInformation("CSL service-RegisterFlights {@RequestUrl} {Response}", responseData.url, responseData.response);
                    return (responseData.response == null) ? default : JsonConvert.DeserializeObject<T>(responseData.response);
                }

                catch (Exception ex)
                {
                    _logger.LogError("CSL service-RegisterFlights error {stackTrace} for {sessionId}", ex.StackTrace, sessionId);
                }

                return default;
            
        }

        public async Task<T> RegisterOrRemoveCoupon<T>(string token, string action, string request, string sessionId)
        {
            _logger.LogInformation("CSL service-RegisterOrRemoveCoupon  parameters Token:{token},Request:{request} Action:{action} SessionId:{sessionId}", token, request, action, sessionId);
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", action);


            
            
                try
                {
                    var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                    if (responseData.statusCode != HttpStatusCode.OK)
                    {
                        _logger.LogError("CSL service-RegisterOrRemoveCoupon {@RequestUrl} error {Response}", responseData.url, responseData.response);
                        if (responseData.statusCode != HttpStatusCode.BadRequest)
                            return default;
                    }

                    _logger.LogInformation("CSL service-RegisterOrRemoveCoupon {requestUrl} {response}", responseData.url, responseData.response);
                    return (responseData.response == null) ? default : JsonConvert.DeserializeObject<T>(responseData.response);
                }
                catch (Exception ex)
                {
                    _logger.LogError("CSL service-RegisterOrRemoveCoupon error {stackTrace} for {sessionId}", ex.StackTrace, sessionId);
                }

                return default;

        }

        public async Task<T> RegisterOffers<T>(string token, string action, string request, string sessionId)
        {
            _logger.LogInformation("CSL service-RegisterOffers  parameters Token:{token}, Request:{request} Action:{action} SessionId:{sessionId}", token, request, action, sessionId);

            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", action);

            
                try
                {
                    var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                    if (responseData.statusCode != HttpStatusCode.OK)
                    {
                        _logger.LogError("CSL service-RegisterOffers {@RequestUrl} error {Response}", responseData.url, responseData.response);
                        if (responseData.statusCode != HttpStatusCode.BadRequest)
                            return default;
                    }

                    
                    _logger.LogInformation("CSL service-RegisterOffers {@RequestUrl} {Response}", responseData.url, responseData.response);
                    return (responseData.response == null) ? default : JsonConvert.DeserializeObject<T>(responseData.response);
                }

                catch (Exception ex)
                {
                    _logger.LogError("CSL service-RegisterOffers error {stackTrace} for {sessionId}", ex.StackTrace, sessionId);
                }

                return default;
           
        }

        public async Task<string> RegisterFareLockReservation(string token, string action, string request, string sessionId)
        {
            _logger.LogInformation("CSL service-RegisterFareLockReservation  parameters Token:{token}, Request:{request} Action:{action} SessionId:{sessionId}", token, request, action, sessionId);
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

            string path = string.Format("/{0}", action);


           
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-RegisterFareLockReservation {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                _logger.LogInformation("CSL service-RegisterFareLockReservation {@RequestUrl} {Response}", responseData.url, responseData.response);
                return responseData.response;
           
        }

        public async Task<string> RegisterCheckinSeats(string token, string action, string request, string sessionId)
        {
            _logger.LogInformation("CSL service-RegisterCheckinSeats  parameters Token:{token}, Request:{request} Action:{action} SessionId:{sessionId}", token, request, action, sessionId);

            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

            string path = string.Format("/{0}", action);


           
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-RegisterCheckinSeats {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                _logger.LogInformation("CSL service-RegisterCheckinSeats {@RequestUrl} {Response}", responseData.url, responseData.response);
                return responseData.response;
            
        }

        public async Task<string> RegisterBags(string token, string action, string request, string sessionId)
        {
            _logger.LogInformation("CSL service-RegisterBags  parameters Token:{token}, Request:{request} Action:{action} SessionId:{sessionId}", token, request, action, sessionId);
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

            string path = string.Format("/{0}", action);

            
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-RegisterBags {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                _logger.LogInformation("CSL service-RegisterBags {@RequestUrl} {Response}", responseData.url, responseData.response);
                return responseData.response;
            
        }

        public async Task<string> RegisterSameDayChange(string token, string action, string request, string sessionId)
        {
            _logger.LogInformation("CSL service-RegisterSameDayChange  parameters Token:{token}, Request:{request} Action:{action} SessionId:{sessionId}", token, request, action, sessionId);
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

            string path = string.Format("/{0}", action);

            
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-RegisterSameDayChange {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                _logger.LogInformation("CSL service-RegisterSameDayChange {@RequestUrl} {Response} ", responseData.url, responseData.response);
                return responseData.response;
           
        }

        public async Task<string> RegisterFormsOfPayments_CFOP(string token, string action, string request, string sessionId)
        {
            _logger.LogInformation("CSL service-RegisterFormsOfPayments_CFOP  parameters Token:{token}, Request:{request} Action:{action} SessionId:{sessionId}", token, request, action, sessionId);
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

            string path = string.Format("/{0}", action);

           
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-RegisterFormsOfPayments_CFOP {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                _logger.LogInformation("CSL service-RegisterFormsOfPayments_CFOP {@RequestUrl} {Response}", responseData.url, responseData.response);
                return responseData.response;
          
        }

        public async Task<string> RegisterSeats_CFOP(string token, string action, string request, string sessionId)
        {
            _logger.LogInformation("CSL service-RegisterSeats_CFOP  parameters Token:{token}, Request:{request} Action:{action} SessionId:{sessionId}", token, request, action, sessionId);
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

            string path = string.Format("/{0}", action);

            
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-RegisterSeats_CFOP {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                _logger.LogInformation("CSL service-RegisterSeats_CFOP {@RequestUrl} {Response}", responseData.url, responseData.response);
                return responseData.response;
            
        }

        public async Task<string> ClearSeats(string token, string action, string request, string sessionId)
        {
            _logger.LogInformation("CSL service-ClearSeats  parameters Token:{token}, Request:{request} Action:{action} SessionId:{sessionId}", token, request, action, sessionId);

            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

            string path = string.Format("{0}", action);

            using (_logger.BeginTimedOperation("Total time taken for ClearSeats business call", transationId: sessionId))
            {
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-ClearSeats {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }

                _logger.LogInformation("CSL service-ClearSeats {@RequestUrl} {Response}", responseData.url, responseData.response);
                return responseData.response;
            }
        }
    }
}
