using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Mobile.Model.Internal.HomePageContent;
using United.Utility.Helper;
using United.Utility.Http;


namespace United.Mobile.DataAccess.MPAuthentication
{
    public class HomePageContentService : IHomePageContentService
    {
        private readonly IResilientClient _homePageContentResilientClient;
        private readonly ICacheLog<HomePageContentService> _logger;
        public HomePageContentService([KeyFilter("homePageContentClientKey")] IResilientClient homePageContentResilientClient, ICacheLog<HomePageContentService> logger)
        {
            _homePageContentResilientClient = homePageContentResilientClient;
            _logger = logger;
        }
        public async Task<string> GetHomePageContents(string token, string requestData, string sessionId)
        {

            
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };
                var serviceResponse = await _homePageContentResilientClient.PostHttpAsyncWithOptions(string.Empty, requestData, headers);

                if (serviceResponse.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service AccountManagement-HomePageConents-service {@RequestUrl} error {@response}", serviceResponse.url, JsonConvert.DeserializeObject<HomePageContentResponse>(serviceResponse.response));
                    if (serviceResponse.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(serviceResponse.response);
                }
                
                _logger.LogInformation("CSL service AccountManagement-HomePageConents-service {@RequestUrl}", serviceResponse.url);
                return serviceResponse.response;
           
        }
    }
}
