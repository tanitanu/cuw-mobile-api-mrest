using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;
using United.Utility.Serilog;

namespace United.Mobile.DataAccess.Customer
{
    public class CustomerDataService : ICustomerDataService
    {
        private readonly ICacheLog<CustomerDataService> _logger;
        private readonly IResilientClient _resilientClient;

        public CustomerDataService(
              [KeyFilter("CustomerDataClientKey")] IResilientClient resilientClient
            , ICacheLog<CustomerDataService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }
        
        public async Task<(T response, long callDuration)> GetCustomerData<T>(string token, string sessionId, string jsonRequest)
        {

            string returnValue = string.Empty;
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/GetProfile");
            _logger.LogInformation("CSL service-GetCustomerData-request {Request}", jsonRequest);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, jsonRequest, headers).ConfigureAwait(false);

            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-GetCustomerData-requestUrl {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }
            returnValue = responseData.response;

            _logger.LogInformation("CSL service-GetCustomerData-response {@RequestUrl} {Response}", responseData.url, returnValue);

            return (returnValue == null) ? default : (JsonConvert.DeserializeObject<T>(returnValue), 0);
        }
        public async Task<T> InsertMPEnrollment<T>(string token, string request, string sessionId, string path)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };
            _logger.LogInformation("CSL service Request-InsertMPEnrollment {Request}", request);

            var enrollmentData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);

            if (enrollmentData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service -InsertMPEnrollment {@RequestUrl} error {Response}", enrollmentData.url, enrollmentData.response);
                if (enrollmentData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(enrollmentData.response);
            }
            _logger.LogInformation("CSL service Response-InsertMPEnrollment {@RequestUrl} {Response}", enrollmentData.url, enrollmentData.response);
            return JsonConvert.DeserializeObject<T>(enrollmentData.response);
        }

        public async Task<T> GetProfile<T>(string token, string request, string sessionId, string path)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };
            _logger.LogInformation("CSL service-GetProfile Request {Request}",request);

            var profileData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);

            if (profileData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-GetProfile {@RequestUrl} error {Response}", profileData.url, profileData.response);
                if (profileData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(profileData.response);
            }

            _logger.LogInformation("CSL service Response-GetProfile {@RequestUrl} {Response}", profileData.url, profileData.response);

            return JsonConvert.DeserializeObject<T>(profileData.response);
        }

        public async Task<T> InsertTraveler<T>(string token, string request, string sessionId)
        {

            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };
            string path = "/InsertTraveler";
            _logger.LogInformation("CSL service Request InsertTraveler {Request}", request);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers).ConfigureAwait(false);

            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service InsertTraveler Request url for get profile {@RequestUrl} error {response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(responseData.response);
            }
            _logger.LogInformation("CSL service Response-InsertTraveler {@RequestUrl} {response}", responseData.url, responseData.response);
            return JsonConvert.DeserializeObject<T>(responseData.response);

        }
        public async Task<string> GetOnlyEmpID(string token, string requestData, string sessionId, string path = "")
        {
           
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };

                var enrollmentData = await _resilientClient.PostHttpAsyncWithOptions(path, requestData, headers);

                if (enrollmentData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service AccountManagement-GetOnlyEmpID-service {@RequestUrl} error {response}", enrollmentData.url, enrollmentData.response);
                    if (enrollmentData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(enrollmentData.response);
                }

                _logger.LogInformation("CSL service AccountManagement-GetOnlyEmpID-service {@RequestUrl}", enrollmentData.url);

                return enrollmentData.response;
            
        }
        public async Task<(T response, long callDuration)> GetMileagePluses<T>(string token, string sessionId, string jsonRequest)
        {

            string returnValue = string.Empty;
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/GetMileagePluses");
            _logger.LogInformation("CSL service-GetMileagePluses {Request}",jsonRequest);
            var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, jsonRequest, headers).ConfigureAwait(false);

            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-GetMileagePluses {@RequestUrl} error {Response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    return default;
            }
            returnValue = responseData.response;
            _logger.LogInformation("CSL service-GetMileagePluses {@RequestUrl} {Response}", responseData.url, responseData.response);
            return (returnValue == null) ? default : (JsonConvert.DeserializeObject<T>(returnValue), 0);
        }
    }
}
