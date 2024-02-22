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

namespace United.Mobile.DataAccess.SeatEngine
{
    public class OnTimePerformanceInfoService : IOnTimePerformanceInfoService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<OnTimePerformanceInfoService> _logger;

        public OnTimePerformanceInfoService([KeyFilter("OnTimePerformanceServiceKey")] IResilientClient resilientClient, ICacheLog<OnTimePerformanceInfoService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }

        public async Task<string> GetOnTimePerformance(string token, string url, string transactionId, int applicationid, string appVersion, string deviceId, string carrierCode, string flightNumber, string origin, string destination, string flightDate, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token },
                          {"Accept","application/json" }
                     };

            string path = url;

            try
            {
                var responseData = await _resilientClient.GetHttpAsyncWithOptions(path, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL Service GetOnTimePerformance {@RequestUrl} {url} error {Response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }              
                _logger.LogInformation("CSL Service GetOnTimePerformance {@RequestUrl} {Response}", responseData.url, responseData.response);

                return responseData.response;
            }
            catch (Exception ex)
            {
                _logger.LogError("CSL service-GetOnTimePerformance error {Exception}", JsonConvert.SerializeObject(ex));
            }

            return default;


        }
    }
}
