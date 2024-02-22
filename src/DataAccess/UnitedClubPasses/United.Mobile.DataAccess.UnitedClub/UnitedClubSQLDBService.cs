using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.UnitedClub
{
    public class UnitedClubSQLDBService:IUnitedClubSQLDBService
    {
        private readonly IResilientClient _resilientClient;
        private readonly ICacheLog<UnitedClubSQLDBService> _logger;

        public UnitedClubSQLDBService([KeyFilter("UnitedClubSQLDBServiceClientKey")] IResilientClient resilientClient, ICacheLog<UnitedClubSQLDBService> logger)
        {
            _resilientClient = resilientClient;
            _logger = logger;
        }

        public async Task<bool> VerifyMileagePlusWithDeviceAPPID(string deviceId, int applicationId, string mpNumber, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept","application/json" }
                     };
            string path = string.Format("/VerifyMileagePlusWithDeviceAPPID?deviceId={0}&applicationId={1}&mpNumber={2}&sessionId={3}", deviceId, applicationId, mpNumber, sessionId);
            
                var responseData = await _resilientClient.GetHttpAsyncWithOptions(path, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogWarning("CSL service-United Club Passes VerifyMileagePlusWithDeviceAPPID SQLDB Service {@RequestUrl} {url} error {response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }
                _logger.LogInformation("CSL service-United Club Passes VerifyMileagePlusWithDeviceAPPID SQLDB Service {@RequestUrl} , {response}", responseData.url, responseData.response);
                return Convert.ToBoolean(responseData.response);
            
        }

        public async Task<bool> ValidateAccountFromCache(string mpNumber, int appId, string appVersion, string deviceId, string hashPinCode, string sessionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept","application/json" }
                     };
            string path = string.Format("/ValidateAccountFromCache?mpNumber={0}&appId={1}&appVersion={2}&deviceId={3}&hashPinCode={4}&sessionId={5}", mpNumber,appId,appVersion,deviceId,hashPinCode,sessionId);
            
                var responseData = await _resilientClient.GetHttpAsyncWithOptions(path, headers).ConfigureAwait(false);

                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogWarning("CSL service-United Club Passes ValidateAccountFromCache SQLDB Service {@RequestUrl} {url} error {response}", _resilientClient?.BaseURL, responseData.url, responseData.response);
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }
                _logger.LogInformation("CSL service-United Club Passes ValidateAccountFromCache SQLDB Service {@RequestUrl} , {response}", responseData.url, responseData.response);
                return Convert.ToBoolean(responseData.response);
          
        }
    }
}
