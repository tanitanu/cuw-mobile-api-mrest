using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using United.Definition;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.Loyalty
{
    public class LoyaltyAWSService : ILoyaltyAWSService
    {
        private readonly ICacheLog<LoyaltyAWSService> _logger;
        private readonly IResilientClient _resilientClient;

        public LoyaltyAWSService(ICacheLog<LoyaltyAWSService> logger, [KeyFilter("LoyaltyAWSClientKey")] IResilientClient resilientClient)
        {
            _logger = logger;
            _resilientClient = resilientClient; ;
        }

        public async Task<string> OneClickEnrollment(string token, string requestData, string sessionId)
        {

            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          { "Authorization", token }
                     };

            var enrollmentData = await _resilientClient.PostHttpAsyncWithOptions("/mp/enroll/", requestData, headers);

            if (enrollmentData.statusCode != HttpStatusCode.OK)
            {
                _logger.LogError("CSL service AccountManagement-OneClickEnrollment-service {@RequestUrl} error {response}", enrollmentData.url, enrollmentData.response);
                if (enrollmentData.statusCode != HttpStatusCode.BadRequest)
                    throw new Exception(enrollmentData.response);
            }

            _logger.LogInformation("CSL service AccountManagement-OneClickEnrollment-service {@RequestUrl}", enrollmentData.url);

            return enrollmentData.response;

        }
    }
}
