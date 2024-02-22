using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.ManageReservation
{
    public class FlightSeapMapService : IFlightSeapMapService
    {
        private readonly ICacheLog<FlightSeapMapService> _logger;
        private readonly IResilientClient _resilientClient;

        public FlightSeapMapService(ICacheLog<FlightSeapMapService> logger, [KeyFilter("FlightSeapMapClientKey")] IResilientClient resilientClient)
        {
            _logger = logger;
            _resilientClient = resilientClient;
        }

        public async Task<T> ViewChangeSeats<T>(string token, string request, string sessionId, string path)
        {
            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };

                var vSecurityQuestions = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);

                if (vSecurityQuestions.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service AccountManagement-ViewChangeSeats-service {@RequestUrl} error {response}", vSecurityQuestions.url, vSecurityQuestions.response);
                    if (vSecurityQuestions.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(vSecurityQuestions.response);
                }

                _logger.LogInformation("CSL service AccountManagement-ViewChangeSeats-service {@RequestUrl}", vSecurityQuestions.url);

                return JsonConvert.DeserializeObject<T>(vSecurityQuestions.response);
           
        }
    }
}
