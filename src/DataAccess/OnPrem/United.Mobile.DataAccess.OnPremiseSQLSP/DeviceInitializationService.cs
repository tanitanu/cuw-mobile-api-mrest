using Autofac.Features.AttributeFilters;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.OnPremiseSQLSP
{
    public   class DeviceInitializationService : IDeviceInitializationService
    {
        private readonly ICacheLog<DeviceInitializationService> _logger;
        private readonly IResilientClient _resilientClient;
        private readonly IHostingEnvironment _hostingEnvironment;

        public DeviceInitializationService([KeyFilter("DevicePremSqlClientKey")] IResilientClient resilientClient, ICacheLog<DeviceInitializationService> logger, IHostingEnvironment hostingEnvironment)
        {
            _resilientClient = resilientClient;
            _logger = logger;
            _hostingEnvironment = hostingEnvironment;
        }

        public async Task<T> RegisterDevice<T>(string requestData,string key,string transId, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"}
                     };

            IDisposable timer = null;
            using (timer = _logger.BeginTimedOperation("Total time taken for RegisterDevice call", transationId: sessionId))
            {
               
                var responseData = await _resilientClient.GetHttpAsyncWithOptions(requestData,headers).ConfigureAwait(false);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("Account Management - RegisterDevice {requestUrl} error {response} for {sessionId} ", responseData.url, responseData.response, sessionId);
                    if (responseData.statusCode == HttpStatusCode.NotFound)
                        return default;
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }

                _logger.LogInformation("Account Management - RegisterDevice {requestUrl} info {response} for {sessionId}", responseData.url, responseData.response, sessionId);

                var responseObject = JsonConvert.DeserializeObject<T>(responseData.response);

                return responseObject;
            }
        }
    }
}
