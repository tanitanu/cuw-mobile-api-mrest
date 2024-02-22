using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.MPRewards
{
    public class MPFutureFlightCredit : IMPFutureFlightCredit
    {
        private readonly ICacheLog<MPFutureFlightCredit> _logger;
        private readonly IResilientClient _resilientClient;

        public MPFutureFlightCredit(ICacheLog<MPFutureFlightCredit> logger, [KeyFilter("MyAccountFutureFlightCreditClientKey")] IResilientClient resilientClient)
        {
            _logger = logger;
            _resilientClient = resilientClient;
        }

        public async Task<T> GetMPFutureFlightCredit<T>(string token, string callsource, string mileagePlusNumber, string sessionId)
        {
           
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };

                string path = "?opId="+ (string.IsNullOrEmpty(callsource)
                    ? mileagePlusNumber
                    : string.Format("{0}&clientId={1}", mileagePlusNumber, callsource));


                var mpFutureFlightData = await _resilientClient.GetHttpAsyncWithOptions(path, headers);

                if (mpFutureFlightData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL Call GetMPFutureFlightCreditFromCancelReservationService {@RequestUrl} {url} error {response}", _resilientClient?.BaseURL, mpFutureFlightData.url, mpFutureFlightData.response);
                    if (mpFutureFlightData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(mpFutureFlightData.response);
                }

                _logger.LogInformation("CSL service Response GetMPFutureFlightCreditFromCancelReservationService {@RequestUrl} {response}", mpFutureFlightData.url, JsonConvert.SerializeObject(mpFutureFlightData.response));

                return XmlSerializerHelper.Deserialize<T>(mpFutureFlightData.response);
            
        }
    }
}
