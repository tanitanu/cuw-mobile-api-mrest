using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.ReShop
{
    public class ReservationService : IReservationService
    {
        private readonly ICacheLog<ReservationService> _logger;
        private readonly IResilientClient _resilientClient;

        public ReservationService([KeyFilter("ReservationClientKey")] IResilientClient resilientClient, ICacheLog<ReservationService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }

        public async Task<string> ConfirmScheduleChange<T>(string token, string recordLocator, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };

            var path = $"/{recordLocator}" + $"/FlightSegments/UpdateStatus?IsScheduleChange=TRUE";


            var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, string.Empty, headers);

            _logger.LogInformation("CSL service-ConfirmScheduleChange-Reservation-service {@RequestUrl}", responseData.url);

            if (responseData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service-ConfirmScheduleChange-Reservation-service {@RequestUrl} error {response}", responseData.url, responseData.response);
                if (responseData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(responseData.response);
            }

            _logger.LogInformation("CSL service-GetPolarisCabinBranding {@RequestUrl} , {response}", responseData.url, responseData.response);
            return responseData.response;
        }
    }
}
