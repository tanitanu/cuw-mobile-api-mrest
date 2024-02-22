using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Mobile.Model.Internal.Exception;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.ManageReservation
{
    public class PNRRetrievalService : IPNRRetrievalService
    {
        private readonly ICacheLog<PNRRetrievalService> _logger;
        private readonly IResilientClient _resilientClient;
        private readonly IConfiguration _configuration;

        public PNRRetrievalService([KeyFilter("PNRRetrievalClientKey")] IResilientClient resilientClient, ICacheLog<PNRRetrievalService> logger
            , IConfiguration configuration)
        {
            _logger = logger;
            _resilientClient = resilientClient;
            _configuration = configuration;
        }

        public async Task<string> PNRRetrieval(string token, string requestData, string sessionId, string path = "")
        {
            _logger.LogInformation("CSL service-PNRRetrieval Request:{@Request}", requestData);
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };
          
                var pnrData = await _resilientClient.PostHttpAsyncWithOptions(path, requestData, headers);

                if (pnrData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-PNRRetrieval {@RequestUrl} error {Response}", pnrData.url, pnrData.response);

                    if (pnrData.statusCode == HttpStatusCode.InternalServerError && pnrData.response != null)
                    {
                        var cslerror = JsonConvert.DeserializeObject<CSLError>(pnrData.response);
                        string errorMessage = cslerror?.Message;

                        if (cslerror?.Errors != null && cslerror?.Errors.Length > 0)
                        {
                            foreach (var error in cslerror.Errors)
                            {
                                errorMessage = errorMessage + " " + error.MinorDescription;

                                if (_configuration.GetValue<bool>("BugFixToggleForExceptionAnalysis") && !string.IsNullOrEmpty(error.MinorCode) && (error.MinorCode.Trim().Equals("40030") || error.MinorCode.Trim().Equals("10028")))
                                {
                                    throw new MOBUnitedException(_configuration.GetValue<string>("UnableToPerformInstantUpgradeErrorMessage"));
                                }
                            }
                            if (errorMessage.Contains("Unable to retrieve PNR"))
                            {
                                _logger.LogInformation("CSL service-PNRRetrieval {@RequestUrl}, {Exception}", pnrData.url, "Unable to retrieve PNR");
                                throw new MOBUnitedException(_configuration.GetValue<string>("BaggageInfoErrorMessage"));
                            }
                            else
                            {
                                _logger.LogInformation("CSL service-PNRRetrieval {@RequestUrl}, {Exception}", pnrData.url, errorMessage);
                            }
                            throw new MOBUnitedException(errorMessage);
                        }
                    }

                    if (pnrData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(pnrData.response);
                }

                _logger.LogInformation("CSL service-PNRRetrieval {@RequestUrl}, {Response}", pnrData.url, pnrData.response);
                return pnrData.response;
            
        }

        public async Task<T> GetOfferedMealsForItinerary<T>(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };            
            
                _logger.LogInformation("CSL service-GetOfferedMealsForItinerary {@Request}", request);
                var responseData = await _resilientClient.PostHttpAsyncWithOptions(action, request, headers);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetOfferedMealsForItinerary {@RequestUrl} error {Response}", responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                _logger.LogInformation("CSL service-GetOfferedMealsForItinerary {@RequestUrl}, {Response}", responseData.url, responseData.response);
                return JsonConvert.DeserializeObject<T>(responseData.response);
            
        }

        public async Task<string> UpdateTravelerInfo(string token, string requestData,string path, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };

            var pnrData = await _resilientClient.PostHttpAsyncWithOptions(path, requestData, headers);

            _logger.LogInformation("CSL service UpdateTravelerInfo-service {@RequestUrl}", pnrData.url);

            if (pnrData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service UpdateTravelerInfo-service {@RequestUrl} error {Response}", pnrData.url, pnrData.response);
                if (pnrData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(pnrData.response);
            }
            return pnrData.response;
        }

    }
}