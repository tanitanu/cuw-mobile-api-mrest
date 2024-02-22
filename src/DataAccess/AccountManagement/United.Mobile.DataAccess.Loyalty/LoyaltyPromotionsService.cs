using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.MPAuthentication
{
    public class LoyaltyPromotionsService : ILoyaltyPromotionsService
    {
        private readonly IResilientClient _statusLiftBannerResilientClient;
        private readonly ICacheLog<LoyaltyPromotionsService> _logger;
        public LoyaltyPromotionsService([KeyFilter("LoyaltyPromotionsClientKey")] IResilientClient statusLiftBannerResilientClient, ICacheLog<LoyaltyPromotionsService> logger)
        {
            _statusLiftBannerResilientClient = statusLiftBannerResilientClient;
            _logger = logger;
        }

        public async Task<string> GetStatusLiftBanner(string token, string path, string sessionId)
        {
            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };
                var serviceResponse = await _statusLiftBannerResilientClient.GetHttpAsyncWithOptions(path, headers);

                if (serviceResponse.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service Accountmanagnement-GetStatusLiftBanner {@RequestUrl} {url} error {response}", _statusLiftBannerResilientClient?.BaseURL, serviceResponse.url, serviceResponse.response);
                    if (serviceResponse.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(serviceResponse.response);
                }
                
                _logger.LogInformation("CSL service Accountmanagnement-GetStatusLiftBanner-service {@RequestUrl}", serviceResponse.url);
                return serviceResponse.response;
            
        }
    }
}
