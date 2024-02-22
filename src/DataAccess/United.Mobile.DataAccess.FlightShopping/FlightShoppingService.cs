using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.Model.Internal.Exception;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.Serilog;

namespace United.Mobile.DataAccess.FlightShopping
{
    public class FlightShoppingService : IFlightShoppingService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<FlightShoppingService> _logger;
        private readonly IConfiguration _configuration;
        public FlightShoppingService([KeyFilter("FlightShoppingClientKey")] IResilientClient resilientClient
            , ICacheLog<FlightShoppingService> logger
            , IConfiguration configuration)
        {
            _resilientClient = resilientClient;
            _logger = logger;
            _configuration = configuration;

        }

        public async Task<(T response, long callDuration)> GetShop<T>(string token, string sessionId, string action, string shopRequest)
        {
            IDisposable timer = null;
            string returnValue = string.Empty;
            using ((timer = _logger.BeginTimedOperation("Total time taken for GetShop service call", transationId: sessionId)))
            {
                _logger.LogInformation("CSL service-GetShop {request} and {sessionId}", shopRequest, sessionId);

                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

                var response = await _resilientClient.PostHttpAsyncWithOptions(action, shopRequest, headers);
                returnValue = response.response;

                if (response.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetShop {@RequestUrl} error {Response}", response.url, response.response);
                    throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0"));
                }

                _logger.LogInformation("CSL service-GetShop {@RequestUrl}", response.url);
            }

            return (returnValue == null) ? default : (Utility.Helper.DataContextJsonSerializer.DeserializeUseContract<T>(returnValue), timer == null ? 0 : ((TimedOperation)timer).GetElapseTime());
        }

        public async Task<string> GetEconomyEntitlement(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestData = string.Format("{0}", action);
            
                _logger.LogInformation("CSL service-GetEconomyEntitlement - request {@Request} {@RequestData}", request, requestData);

                var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData, request, headers);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetEconomyEntitlement {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                _logger.LogInformation("CSL service-GetEconomyEntitlement {@RequestUrl} {Response}", responseData.url, responseData.response);
                return responseData.response;
            
        }

        public async Task<string> GetFareColumnEntitlements(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestData = string.Format("/{0}", action);
           
                _logger.LogInformation("CSL service-GetFareColumnEntitlements - request {@Request} {@RequestData}", request, requestData);

                var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData, request, headers);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetFareColumnEntitlements {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                _logger.LogInformation("CSL service-GetFareColumnEntitlements {@RequestUrl} {Response}", responseData.url, responseData.response);
                return responseData.response;
          
        }            

        public async Task<T> ShopFareWheelInfo<T>(string token, string sessionId, string logAction, string requestData)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

            string request = string.Format("/{0}", logAction);

            var response = await _resilientClient.PostHttpAsyncWithOptions(request, requestData, headers);

            if (response.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-ShopFareWheelInfo {@RequestUrl} error {Response}", response.url, response.response);
                if (response.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }

            _logger.LogError("CSL service-ShopFareWheelInfo {@RequestUrl}", response.url);

            return JsonConvert.DeserializeObject<T>(response.response);
        }
        public async Task<T> GetBundles<T>(string token, string sessionId, string logAction, string requestData)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string request = string.Format("/{0}", logAction);

            _logger.LogInformation("CSL service-GetBundles {@requestUrl} {@requestData} and {sessionId}", request, requestData, sessionId);
            var response = await _resilientClient.PostHttpAsyncWithOptions(request, requestData, headers);
                if (response.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetBundles {@RequestUrl} error {Response}", response.url, response.response);
                    if (response.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                _logger.LogInformation("CSL service-GetBundles {@RequestUrl} {Response}", response.url, response.response);
                return (response.response == null) ? default : JsonConvert.DeserializeObject<T>(response.response);
           
        }
        public async Task<T> SelectFareWheel<T>(string token, string action, string sessionId, string requestData)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string request = string.Format("/{0}", action);
            _logger.LogInformation("CSL service- SelectFareWheel -request {@Request}", requestData);
            
                var response = await _resilientClient.PostHttpAsyncWithOptions(request, requestData, headers);
                if (response.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-SelectFareWheel {@RequestUrl} error {Response}", response.url, response.response);
                    if (response.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                _logger.LogInformation("CSL service- SelectFareWheel-response {@RequestUrl} {Response}", response.url, response.response);
                return (response.response == null) ? default : JsonConvert.DeserializeObject<T>(response.response);
            
        }
        public async Task<T> GetOnTimePerformanceInfo<T>(string token, string MarketingCarrier, string FlightNumber, string Origin, string Destination, string DepartureDateTime, string sessionId)
        {

            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestData = string.Format("/GetOnTimePerformanceInfo?MarketingCarrier={0}&FlightNumber={1}&Origin={2}&Destination={3}&DepartureDateTime={4}", MarketingCarrier, FlightNumber, Origin, Destination, DateTime.ParseExact(DepartureDateTime, "yyyyMMdd", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy"));
            requestData = IsTokenMiddleOfFlowDPDeployment() ? ModifyVIPMiddleOfFlowDPDeployment(token, requestData) : requestData;
            
                _logger.LogInformation("CSL service-GetOnTimePerformanceInfo {@RequestData}", requestData);
                var responseData = await _resilientClient.GetHttpAsyncWithOptions(requestData, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetOnTimePerformanceInfo {@RequestUrl} {url} error {Response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                _logger.LogInformation("CSL service-GetOnTimePerformanceInfo {@RequestUrl},{Response}", responseData.url, responseData.response);

                return JsonConvert.DeserializeObject<T>(responseData.response);
           
        }

        private string ModifyVIPMiddleOfFlowDPDeployment(string token, string url)
        {
            url = token.Length < 50 ? url.Replace(_configuration.GetValue<string>("DPVIPforDeployment"), _configuration.GetValue<string>("CSSVIPforDeployment")) : url;
            return url;
        }

        private bool IsTokenMiddleOfFlowDPDeployment()
        {
            return (_configuration.GetValue<bool>("ShuffleVIPSBasedOnCSS_r_DPTOken") && _configuration.GetValue<bool>("EnableDpToken")) ? true : false;

        }
        public async Task<string> GetProducts(string token, string requestData, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

            var responseData = await _resilientClient.PostHttpAsyncWithOptions("/GetProducts", requestData, headers).ConfigureAwait(false);
            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("csl-GetProducts-CheckedBagChargeInfo {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(responseData.response);
            }

            _logger.LogInformation("csl-GetProducts-CheckedBagChargeInfo {@RequestUrl}", responseData.url);

            return responseData.response;
        }
        public async Task<string> FarePriceRules(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestData = string.Format("/{0}", action);
            
                _logger.LogInformation("CSL service-FarePriceRules {@Request} {@RequestData}", request, requestData);
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-FarePriceRules {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                _logger.LogInformation("CSL service-FarePriceRules {@RequestUrl}, {Response}", responseData.url, responseData.response);

                return responseData.response;
            
        }
        public async Task<string> OrganizeResults(string token, string requestData, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            
                _logger.LogInformation("csl-OrganizeResults {@RequestData}", requestData);
                var responseData = await _resilientClient.PostHttpAsyncWithOptions("/OrganizeResults", requestData, headers).ConfigureAwait(false);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("csl-OrganizeResults {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }

                _logger.LogInformation("csl-OrganizeResults {@RequestUrl}, {Response}", responseData.url, responseData.response);

                return responseData.response;
           
        }

        public async Task<string> GetColumnInfo(string token, string sessionId, string cartId, string langCode, string countryCode)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
            {
                {"Accept", "application/json"},
                { "Authorization", token }
            };
            string requestData = string.Format("/GetFareColumnInformation?cartId={0}&langCode={1}&countryCode={2}", cartId, langCode, countryCode);
            var responseData = await _resilientClient.GetHttpAsyncWithOptions(requestData, headers).ConfigureAwait(false);
            _logger.LogInformation("CSL service-GetColumnInfo {@RequestUrl}", responseData.url);



            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-GetColumnInfo {@RequestUrl} {url} error {Response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }



            return responseData.response;



        }
        public async Task<string> GetShopPinDown(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            
            string requestData = string.Format("/{0}", action);

            _logger.LogInformation("CSL service-GetShopPinDown {@Request} {@RequestUrl}", request, requestData);
           
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetShopPinDown {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                _logger.LogInformation("CSL service-GetShopPinDown {@RequestUrl} {Response}", responseData.url, responseData.response);

                return responseData.response;
            
        }
        public async Task<T> GetBasicEconomyEntitlement<T>(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestData = string.Format("/{0}", action);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData,request , headers);
            _logger.LogInformation("CSL service-GetBasicEconomyEntitlement {request} and {sessionId}", request, sessionId);
            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-GetBasicEconomyEntitlement {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }
            _logger.LogInformation("CSL service-GetBasicEconomyEntitlement {@RequestUrl}, {Response}", responseData.url, responseData.response);
            return JsonConvert.DeserializeObject<T>(responseData.response);
        }
        public async Task<T> GetAmenitiesInfo<T>(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestData = string.Format("/{0}", action);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData,request , headers);
            _logger.LogInformation("CSL service-GetAmenitiesInfo {request} and {sessionId}", request, sessionId);
            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-GetAmenitiesInfo {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }
            _logger.LogInformation("CSL service-GetAmenitiesInfo {@RequestUrl}, {Response}", responseData.url, responseData.response);
            return JsonConvert.DeserializeObject<T>(responseData.response);
        }
        public async Task<T> GetUserSession<T>(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestData = string.Format("/{0}", action);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData,request , headers);
            _logger.LogInformation("CSL service-GetUserSession {@RequestUrl}", responseData.url);
            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-GetUserSession {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }
            _logger.LogInformation("CSL service-GetUserSession {@RequestUrl}, {Response}", responseData.url,responseData.response);
            return JsonConvert.DeserializeObject<T>(responseData.response);
        }
        public async Task<T> GetBookingDetailsV2<T>(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestData = string.Format("/{0}", action);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData,request , headers);
            _logger.LogInformation("CSL service-GetBookingDetailsV2 {@Request} and {@RequestData}", request, requestData);
            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-GetBookingDetailsV2 {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }
            _logger.LogInformation("CSL service-GetBookingDetailsV2 {@RequestUrl}, {Response}", responseData.url, responseData.response);
            return JsonConvert.DeserializeObject<T>(responseData.response);
        }
        public async Task<T> GetMetaBookingDetails<T>(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestData = string.Format("/{0}", action);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData, request, headers);
            _logger.LogInformation("CSL service-GetMetaBookingDetails {request} and {sessionId}", request, sessionId);
            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-GetMetaBookingDetails {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }
            _logger.LogInformation("CSL service-GetMetaBookingDetails {@RequestUrl}, {Response}", responseData.url, responseData.response);

            return JsonConvert.DeserializeObject<T>(responseData.response);
        }
        public async Task<T> SelectTrip<T>(string token, string sessionId, string logAction, string requestData)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string request = string.Format("/{0}", logAction);
            _logger.LogInformation("CSL service-SelectTrip {@Request} {@RequestUrl}", requestData, request);

           
                var response = await _resilientClient.PostHttpAsyncWithOptions(request, requestData, headers);

                if (response.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-SelectTrip {@RequestUrl} error {Response}", response.url, response.response);
                    if (response.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                _logger.LogInformation("CSL service-SelectTrip {@RequestUrl}, {Response}", response.url, response.response);

                return (response.response == null) ? default : JsonConvert.DeserializeObject<T>(response.response);
            
        }

        public async Task<T> AmenitiesForFlights<T>(string token, string sessionId, string action, string requestData)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string request = string.Format("/{0}", action);
            var response = await _resilientClient.PostHttpAsyncWithOptions(request, requestData, headers);
            _logger.LogInformation("CSL service-AmenitiesForFlights {@RequestUrl}", response.url);

            if (response.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-AmenitiesForFlights {@RequestUrl} error {Response}", response.url, response.response);
                if (response.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }
            return (response.response == null) ? default : JsonConvert.DeserializeObject<T>(response.response);
        }

        public async Task<T> ShopBookingDetails<T>(string token, string sessionId, string action, string requestData)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

            string request = string.Format("/{0}", action);
            _logger.LogInformation("CSL service-ShopBookingDetails {@Request} {@RequestUrl}", requestData, request);

            
                var response = await _resilientClient.PostHttpAsyncWithOptions(request, requestData, headers);

                if (response.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-ShopBookingDetails {@RequestUrl} error {Response}", response.url, response.response);
                    if (response.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                _logger.LogInformation("CSL service-ShopBookingDetails {@RequestUrl}, {Response}", response.url, response.response);
                return (response.response == null) ? default : JsonConvert.DeserializeObject<T>(response.response);
           
        }

        public async Task<T> MetaSyncUserSession<T>(string token, string sessionId, string action, string requestData)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string request = string.Format("/{0}", action);
            var response = await _resilientClient.PostHttpAsyncWithOptions(request, requestData, headers);
            _logger.LogInformation("CSL service-MetaSyncUserSession {@RequestUrl}", response.url);

            if (response.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-MetaSyncUserSession {@RequestUrl} error {Response}", response.url, response.response);
                if (response.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }
            return (response.response == null) ? default : JsonConvert.DeserializeObject<T>(response.response);
        }

        public async Task<string> MetaSyncUserSession(string token, string sessionId, string action, string requestData)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string request = string.Format("/{0}", action);
            var response = await _resilientClient.PostHttpAsyncWithOptions(request, requestData, headers);
            _logger.LogInformation("CSL service-MetaSyncUserSession {@RequestUrl}", response.url);

            if (response.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-MetaSyncUserSession {@RequestUrl} error {Response}", response.url, response.response);
                if (response.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }
            return (response.response == null) ? default : (response.response);
        }

        //public async Task<T> ShopValidateSpecialPricing<T>(string token, string sessionId, string action, string requestData)
        //{
        //    Dictionary<string, string> headers = new Dictionary<string, string>
        //             {
        //                  {"Accept", "application/json"},
        //                  { "Authorization", token }
        //             };
        //    string url = string.Format("{0}/ShopValidateSpecialPricing", action);
        //    string jsonRequest = JsonConvert.SerializeObject(requestData);

        //    using (_logger.BeginTimedOperation("Total time taken for ShopValidateSpecialPricing business call", transationId: sessionId))
        //    {
        //        var response = await _resilientClient.PostHttpAsyncWithOptions(url, jsonRequest, headers);
        //        _logger.LogInformation("CSL service-ShopValidateSpecialPricing {@RequestUrl}", response.url);

        //        if (response.statusCode != HttpStatusCode.OK)
        //        {
        //            _logger.LogError("CSL service-ShopValidateSpecialPricing {@RequestUrl} error {Response}", response.url, response.response);
        //            if (response.statusCode != HttpStatusCode.BadRequest)
        //                return default;
        //        }

        //        _logger.LogInformation("CSL service-ShopValidateSpecialPricing {@RequestUrl}", response.url);
        //        return (response.response == null) ? default : JsonConvert.DeserializeObject<T>(response.response);
        //    }
        //}
        
        public async Task<(T response, long callDuration)> GetAmenitiesForFlights<T>(string token, string sessionId, string jsonRequest)
        {
            IDisposable timer = null;
            string returnValue = string.Empty;

            using (timer = _logger.BeginTimedOperation("Total time taken for GetAmenitiesForFlights service call", transationId: sessionId))
            {
                string actionName = @"\UpdateAmenitiesIndicators";

                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

                _logger.LogInformation("CSL service-GetShopPinDown {@Request}", JsonConvert.SerializeObject(jsonRequest));
                var response = await _resilientClient.PostHttpAsyncWithOptions(actionName, jsonRequest, headers);
                returnValue = response.response;
                if (response.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetAmenitiesForFlights  {@RequestUrl} error {Response}", response.url, response.response);
                    if (response.statusCode != HttpStatusCode.BadRequest)
                        throw new MOBUnitedException("Failed to retrieve booking details.");
                }
                _logger.LogInformation("CSL service-GetAmenitiesForFlights  {@RequestUrl}, {Response}", response.url, response.response);
            }
            return (returnValue == null) ? default : (JsonConvert.DeserializeObject<T>(returnValue), timer == null ? 0 : ((TimedOperation)timer).GetElapseTime());

        }
        public async Task<(T response, long callDuration)> UpdateAmenitiesIndicators<T>(string token, string sessionId, string jsonRequest)
        {
            IDisposable timer = null;
            string returnValue = string.Empty;

            using (timer = _logger.BeginTimedOperation("Total time taken for UpdateAmenitiesIndicators service call", transationId: sessionId))
            {
                string actionName = @"\UpdateAmenitiesIndicators";

                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

                _logger.LogInformation("CSL service-UpdateAmenitiesIndicators {@Request} {@RequestUrl}", jsonRequest, actionName);
                var response = await _resilientClient.PostHttpAsyncWithOptions(actionName, jsonRequest, headers);
                returnValue = response.response;
                if (response.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-UpdateAmenitiesIndicators {@RequestUrl} error {Response}}", response.url, response.response);
                    if (response.statusCode != HttpStatusCode.BadRequest)
                        throw new MOBUnitedException("Failed to retrieve booking details.");
                }
                _logger.LogInformation("CSL service-UpdateAmenitiesIndicators {@RequestUrl} {Response}", response.url, response.response);
            }
            return (returnValue == null) ? default : (JsonConvert.DeserializeObject<T>(returnValue), timer == null ? 0 : ((TimedOperation)timer).GetElapseTime());

        }

        public async Task<(T response, long callDuration)> GetLmxQuote<T>(string token, string sessionId, string cartId, string hashList)
        {
            IDisposable timer = null;
            string returnValue = string.Empty;

            using ((timer = _logger.BeginTimedOperation("Total time taken for GetLmxQuote service call", transationId: sessionId)))
            {
                string actionName = @"\GetLmxQuote";
                string jsonRequest = "{\"CartId\":\"" + cartId + "\"}";
                if (!string.IsNullOrEmpty(hashList))
                {
                    jsonRequest = "{\"CartId\":\"" + cartId + "\", \"hashList\":[" + hashList + "]}";
                }

                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

                _logger.LogInformation("CSL service-GetLmxQuote {@Request} {@actionName}", jsonRequest, actionName);
                var response = await _resilientClient.PostHttpAsyncWithOptions(actionName, jsonRequest, headers);
                returnValue = response.response;

                if (response.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetLmxQuote {@RequestUrl} error {Response}", response.url, response.response);
                    if (response.statusCode != HttpStatusCode.BadRequest)
                        throw new MOBUnitedException("Failed to retrieve booking details.");
                }

                _logger.LogInformation("CSL service-GetLmxQuote {@RequestUrl} {Response}", response.url, response.response);
            }

            return (returnValue == null) ? default : (JsonConvert.DeserializeObject<T>(returnValue), timer == null ? 0 : ((TimedOperation)timer).GetElapseTime());
        }

        public async Task<(T response, long callDuration)> ShopValidateSpecialPricing<T>(string token, string sessionId, string jsonRequest)
        {
            IDisposable timer = null;
            string returnValue = string.Empty;

            using (timer = _logger.BeginTimedOperation("Total time taken for ShopValidateSpecialPricing service call", transationId: sessionId))
            {
                string actionName = @"\ShopValidateSpecialPricing";
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

                var response = await _resilientClient.PostHttpAsyncWithOptions(actionName, jsonRequest, headers);
                returnValue = response.response;

                if (response.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-ShopValidateSpecialPricing  {@RequestUrl} error {Response}", response.url, response.response);
                    if (response.statusCode != HttpStatusCode.BadRequest)
                        throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                _logger.LogInformation("CSL service-ShopValidateSpecialPricing  {@RequestUrl}", response.url);
            }

            return (returnValue == null) ? default : (JsonConvert.DeserializeObject<T>(returnValue), timer == null ? 0 : ((TimedOperation)timer).GetElapseTime());
        }

        public async Task<string> GetCartInformation(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string requestData = string.Format("/{0}", action);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(requestData, request, headers);
            _logger.LogInformation("CSL service-GetCartInformation {@RequestUrl}", responseData.url);
            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-GetCartInformation {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }

            return responseData.response;
        }

        public async Task<T> FlightCarbonEmission<T>(string token, string sessionId, string logAction, string requestData)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string request = string.Format("/{0}", logAction);
            _logger.LogInformation("CSL service-FlightCarbonEmission {@Request} {@RequestUrl}", requestData, request);

            using (_logger.BeginTimedOperation("Total time taken for CSL service-SelectTrip call", transationId: sessionId))
            {
                var response = await _resilientClient.PostHttpAsyncWithOptions(request, requestData, headers);

                if (response.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-FlightCarbonEmission {@RequestUrl} error {Response}", response.url, response.response);
                    if (response.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                _logger.LogInformation("CSL service-FlightCarbonEmission {@RequestUrl}, {Response}", response.url, response.response);

                return (response.response == null) ? default : JsonConvert.DeserializeObject<T>(response.response);
            }
        }


    }
}
