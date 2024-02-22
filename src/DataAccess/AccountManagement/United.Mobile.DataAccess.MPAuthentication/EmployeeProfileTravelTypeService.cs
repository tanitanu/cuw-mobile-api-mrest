using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.MPAuthentication
{
    public class EmployeeProfileTravelTypeService: IEmployeeProfileTravelTypeService
    {
        private readonly IResilientClient _employeeProfileResilientClient;
        private readonly ICacheLog<EmployeeProfileTravelTypeService> _logger;
        public EmployeeProfileTravelTypeService([KeyFilter("employeeProfileTravelTypeClientKey")] IResilientClient employeeProfileResilientClient, ICacheLog<EmployeeProfileTravelTypeService> logger)
        {
            _employeeProfileResilientClient = employeeProfileResilientClient;
            _logger = logger;
        }

        public async Task<string> GetEmployeeProfileTravelType(string token, string path, string requestPayload, string sessionId)
        {
          
                Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };
                var serviceResponse = await _employeeProfileResilientClient.PostHttpAsyncWithOptions(path, requestPayload, headers);

                if (serviceResponse.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("CSL service Accountmanagnement-EmployeeProfileTravelType-service {@RequestUrl} error {response}", serviceResponse.url, JsonConvert.DeserializeObject<EmployeeProfileTravelTypeService>(serviceResponse.response));
                    if (serviceResponse.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(serviceResponse.response);
                }
                
                _logger.LogInformation("CSL service Accountmanagnement-EmployeeProfileTravelType-service {@RequestUrl}", serviceResponse.url);
                return serviceResponse.response;
           
        }
    }
}
