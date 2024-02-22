using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Mobile.Model.Internal.Common;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.OnPremiseSQLSP
{
    public class GMTConversionService : IGMTConversionService
    {
        private readonly ICacheLog<GMTConversionService> _logger;
        private readonly IResilientClient _resilientClient;

        public GMTConversionService(ICacheLog<GMTConversionService> logger, [KeyFilter("OnPremSQLServiceClientKey")] IResilientClient resilientClient)
        {
            _logger = logger;
            _resilientClient = resilientClient;
        }

        public async Task<string> GETGMTTime(string localTime, string airportCode, string sessionId)
        {
            using (_logger.BeginTimedOperation("Total time taken for GETGMTTime OnPrem service call", transationId: sessionId))
            {
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"}
                     };

                var rbody = Convert.ToDateTime(localTime).ToString("yyyy-MM-ddTHH:mm:ss");
                var path = string.Format("/GMTConversion/GetGMTTimeAsync?airportCode={0}&localTime={1}", airportCode, rbody);

                _logger.LogInformation("GETGMTTime-OnPrem Service {@Path}", path);

                var responseData = await _resilientClient.GetHttpAsyncWithOptions(path, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("GETGMTTime-OnPrem Service {@RequestUrl} {url} error {@Response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                    #region If this api fails it should be a softfailure thats the reason not throwingexception back to client
                    //if (responseData.statusCode != HttpStatusCode.BadRequest)
                    //    throw new Exception(responseData.response);
                    #endregion If this api fails it should be a softfailure thats the reason not throwingexception back to client
                }

                _logger.LogInformation("GETGMTTime-OnPrem Service {@RequestUrl} {@Response}", responseData.url, responseData. response);

                return JsonConvert.DeserializeObject<string>(responseData.response);
            }
        }
    }
}
