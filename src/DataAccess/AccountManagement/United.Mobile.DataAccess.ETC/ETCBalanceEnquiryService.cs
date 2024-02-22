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

namespace United.Mobile.DataAccess.ETC
{
    public class ETCBalanceEnquiryService: IETCBalanceEnquiryService
    {
        private readonly ICacheLog<ETCBalanceEnquiryService> _logger;
        private readonly IResilientClient _resilientClient;

        public ETCBalanceEnquiryService(ICacheLog<ETCBalanceEnquiryService> logger, [KeyFilter("ETCBalanceClientKey")] IResilientClient resilientClient)
        {
            _logger = logger;
            _resilientClient = resilientClient;
        }

        public async Task<string> GetETCBalanceInquiry(string path, string request, string sessionId, string token)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                           {"Accept", "application/json"},
                          { "Authorization", token }
                     };

            _logger.LogInformation("CSL service-GetETCBalanceInquiry Request:{request}", JsonConvert.SerializeObject(request));

            IDisposable timer = null;
            
                try
                {
                    var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers).ConfigureAwait(true);

                    if (responseData.statusCode != HttpStatusCode.OK)
                    {
                        _logger.LogError("CSL service-GetETCBalanceInquiry {@RequestUrl} error {response}", responseData.url, responseData.response);
                        if (responseData.statusCode != HttpStatusCode.BadRequest)
                            throw new Exception(responseData.response);
                    }

                    var CallDuration = (timer != null) ? ((TimedOperation)timer).GetElapseTime() : 0;
                    _logger.LogInformation("CSL service-GetETCBalanceInquiry {@RequestUrl}", responseData.url);

                    return responseData.response;
                }
                catch (Exception ex)
                {
                    _logger.LogError("CSL service-GetETCBalanceInquiry error {stackTrace}", ex.StackTrace);
                }

                return default;
            
        }
    }
}
