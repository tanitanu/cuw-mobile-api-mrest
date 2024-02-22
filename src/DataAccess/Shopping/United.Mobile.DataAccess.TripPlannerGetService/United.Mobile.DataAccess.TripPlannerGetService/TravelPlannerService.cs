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
using United.Utility.Serilog;


namespace United.Mobile.DataAccess.TripPlannerGetService
{
    public class TravelPlannerService: ITravelPlannerService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<TravelPlannerService> _logger;
        private readonly IConfiguration _configuration;
        public TravelPlannerService([KeyFilter("TravelPlannerServiceClientKey")] IResilientClient resilientClient, ICacheLog<TravelPlannerService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }
        public async Task<string> TripPlan<T>(string token, string sessionId, string action, string request)
        {
            _logger.LogInformation("CSL service-TripPlan  parameters {@token} {@request} {@action}", token, request, action);

           
            string returnValue = string.Empty;
            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

                var response = await _resilientClient.PostHttpAsyncWithOptions(action, request, headers);
                returnValue = response.response;

                if (response.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-TripPlan {@requestUrl} error {Response}", response.url, response.response);
                    throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0"));
                }

                _logger.LogInformation("CSL service-TripPlan {@requestUrl} {Response}", response.url, response.response);
          

            return (returnValue == null) ? default : JsonConvert.DeserializeObject<string>(returnValue);

        }
        public async Task<string> ShopTripPlanner(string token, string sessionId, string action, string request)
        {
            _logger.LogInformation("CSL service-ShopTripPlanner parameters Request:{@Request} Action:{@Action}", request, action);

            
            string returnValue = string.Empty;
            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

                var response = await _resilientClient.PostHttpAsyncWithOptions(action, request, headers);
                returnValue = response.response;

                if (response.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-ShopTripPlanner {@RequestUrl} error {Response}", response.url, response.response);
                    throw new MOBUnitedException(_configuration.GetValue<string>("NoAvailabilityError2.0"));
                }
                _logger.LogInformation("CSL service-ShopTripPlanner {@RequestUrl} {Response}", response.url, returnValue);
          
            return (returnValue == null) ? default : returnValue;

        }


    }
}
