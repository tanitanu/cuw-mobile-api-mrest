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

namespace United.Mobile.DataAccess.TripPlanGetService
{
    public class TripPlannerIDService : ITripPlannerIDService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<TripPlannerIDService> _logger;
        private readonly IConfiguration _configuration;
        public TripPlannerIDService([KeyFilter("TripPlannerIDServiceClientKey")] IResilientClient resilientClient, ICacheLog<TripPlannerIDService> logger, IConfiguration configuration)
        {
            _resilientClient = resilientClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string> GetTripPlanID<T>(string token, string sessionId, string action, string request)
        {
            _logger.LogInformation("CSL service-ShopTripPlanner parameters Request:{@Request} Action:{Action}", request, action);

           
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


                return (returnValue == null) ? default : JsonConvert.DeserializeObject<string>(returnValue);
           

        }
    }
}
