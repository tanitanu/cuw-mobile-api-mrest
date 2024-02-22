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

namespace United.Mobile.DataAccess.CMSContent
{
    public class CMSContentService : ICMSContentService
    {
        private readonly ICacheLog<CMSContentService> _logger;
        private readonly IResilientClient _resilientClient;

        public CMSContentService(ICacheLog<CMSContentService> logger, [KeyFilter("CMSContentClientKey")] IResilientClient resilientClient)
        {
            _logger = logger;
            _resilientClient = resilientClient;
        }

        public async Task<string> GetMobileCMSContentDetail(string token, string requestData, string sessionId)
        {
            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };
                _logger.LogInformation("BagCalculator-GetMobileCMSContentDetail-service {@Request}", requestData);
                var pnrData = await _resilientClient.PostHttpAsyncWithOptions("/message", requestData, headers);

                if (pnrData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-BagCalculator-GetMobileCMSContentDetail-service {@RequestUrl} error {Response}", pnrData.url, pnrData.response);
                    if (pnrData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(pnrData.response);
                }

                _logger.LogInformation("CSL service-BagCalculator-GetMobileCMSContentDetail-service {@RequestUrl} {Response}", pnrData.url, pnrData.response);

                return pnrData.response;
           
        }

        public async Task<string> GetMobileCMSContentMessages(string token, string requestData, string sessionId)
        {
            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };
                _logger.LogInformation("GetMobileCMSContentMessages-service {@RequestData}", requestData);
                var pnrData = await _resilientClient.PostHttpAsyncWithOptions("/message", requestData, headers);

                if (pnrData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-GetMobileCMSContentMessages-service {@RequestUrl} error {Response}", pnrData.url, pnrData.response);
                    if (pnrData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(pnrData.response);
                }

                _logger.LogInformation("CSL service-GetMobileCMSContentMessages-service {@RequestUrl} {Response}", pnrData.url, pnrData.response);

                return pnrData.response;
           
        }

        public async Task<T> GetSDLContentByGroupName<T>(string token, string action, string request, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string path = string.Format("/{0}", action);

            _logger.LogInformation("CSL service-GetSDLContentByGroupName Request:{@Request} Path:{@Path}", request, path);

            
                try
                {
                    var responseData = await _resilientClient.PostHttpAsyncWithOptions(path, request, headers);
                    if (responseData.statusCode != HttpStatusCode.OK)
                    {
                        _logger.LogError("CSL service-GetSDLContentByGroupName {@RequestUrl} error {Response}", responseData.url, responseData.response);
                        if (responseData.statusCode != HttpStatusCode.BadRequest)
                            return default;
                    }

                    
                    _logger.LogInformation("CSL service-GetSDLContentByGroupName {@RequestUrl}, {Response}", responseData.url, responseData.response);
                    return (responseData.response == null) ? default : JsonConvert.DeserializeObject<T>(responseData.response);
                }
                catch (Exception ex)
                {
                    _logger.LogError("CSL service-GetSDLContentByGroupName error {@Exception}", JsonConvert.SerializeObject(ex));
                }

                return default;
        }

    }
}
