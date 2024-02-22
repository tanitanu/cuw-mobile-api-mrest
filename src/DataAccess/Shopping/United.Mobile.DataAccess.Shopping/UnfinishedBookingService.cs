using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Services.FlightShopping.Common;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.Shopping
{
    public class UnfinishedBookingService : IUnfinishedBookingService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<UnfinishedBookingService> _logger;
        private readonly IConfiguration _configuration;

        public UnfinishedBookingService([KeyFilter("FlightShoppingClientKey")] IResilientClient resilientClient
            , ICacheLog<UnfinishedBookingService> logger
            , IConfiguration configuration)
        {
            _resilientClient = resilientClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<T> GetShopPinDown<T>(string token, string sessionId, ShopRequest shopRequest)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"},
                          { "Authorization", token }
                     };
            string shopPinDownActionName = "shoppindown";
            if (_configuration.GetValue<bool>("EnableOmniChannelCartMVP1"))
            {
                shopPinDownActionName = "ShopPinDownV2";
            }
            string url = string.Format("/{0}", shopPinDownActionName);
            string jsonRequest = JsonConvert.SerializeObject(shopRequest);

            
                _logger.LogInformation("CSL service-ShopPinDown {@RequestUrl} {@Request}", url, jsonRequest);
                var response = await _resilientClient.PostHttpAsyncWithOptions(url, jsonRequest, headers);
                if (response.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service-ShopPinDown {@RequestUrl} error {Response}", response.url, response.response);
                    if (response.statusCode != HttpStatusCode.BadRequest)
                        return default;
                }
                
                _logger.LogInformation("CSL service-ShopPinDown {@RequestUrl}, {Response}}", response.url, response.response);
                return (response.response == null) ? default : JsonConvert.DeserializeObject<T>(response.response);
           
        }


    }
}
