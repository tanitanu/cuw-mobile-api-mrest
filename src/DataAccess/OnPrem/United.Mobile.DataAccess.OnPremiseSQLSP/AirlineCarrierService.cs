using Autofac.Features.AttributeFilters;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.BagCalculator;
using United.Mobile.Model.Common;
using United.Utility.Helper;
using United.Utility.Http;

namespace United.Mobile.DataAccess.FlightStatus
{
    public class AirlineCarrierService : IAirlineCarrierService
    {
        private readonly ICacheLog<AirlineCarrierService> _logger;
        private readonly IResilientClient _resilientClient;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly string _tableName;
        private readonly IConfiguration _configuration;

        public AirlineCarrierService([KeyFilter("CarrierOnPremSqlClientKey")] IResilientClient resilientClient, ICacheLog<AirlineCarrierService> logger, IDynamoDBService dynamoDBService, IConfiguration configuration)
        {
            _configuration = configuration;
            _resilientClient = resilientClient;
            _logger = logger;
            _dynamoDBService = dynamoDBService;
            _tableName = _configuration.GetValue<string>("DynamoDBTables:CarrierDetails");
        }

        public async Task<T> GetCarriers<T>(string transactionId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string>
                     {
                          {"Accept", "application/json"}
                     };

            IDisposable timer = null;
            using (timer = _logger.BeginTimedOperation("Total time taken for GetCarriers OnPrem service call", transationId: transactionId))
            {
                string requestData = string.Format("/GetCarriers?transactionId={0}",transactionId);
                var responseData = await _resilientClient.GetHttpAsyncWithOptions(requestData, headers).ConfigureAwait(false);
                if (responseData.statusCode != HttpStatusCode.OK)
                {
                    _logger.LogError("BagCalculator - GetCarriers--OnPrem Service {requestUrl} error {response} for {transactionId} ", responseData.url, JsonConvert.SerializeObject(responseData.response), transactionId);
                    if (responseData.statusCode == HttpStatusCode.NotFound)
                        return default;
                    if (responseData.statusCode != HttpStatusCode.BadRequest)
                        throw new Exception(responseData.response);
                }

                _logger.LogInformation("BagCalculator - GetCarriers-OnPrem Service {requestUrl} info {response} for {transactionId}", responseData.url, JsonConvert.SerializeObject(responseData.response), transactionId);

                var responseObject = JsonConvert.DeserializeObject<SessionResponse>(responseData.response);

                var responseObjectData = JsonConvert.DeserializeObject<List<CarrierInfo>>(responseObject?.Data);

                return responseObjectData;
            }
        }

        public async Task<List<CarrierInfo>> GetCarriersInfoDetails(string transactionId)
        {
            try
            {

                var ListOfCarrierInfo = new List<CarrierInfo>();

                var response = await _dynamoDBService.GetAllRecordsByKeys<List<CarrierInfoDetails>>(_tableName, transactionId);
                if (response != null)
                {
                    ListOfCarrierInfo = GetCarriersDetails(response);
                }

                return ListOfCarrierInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception thrown at AirlineCarrierService Dynamodb call: {0} ", transactionId);
            }
            return default;
        }

        public List<CarrierInfo> GetCarriersDetails(List<CarrierInfoDetails> carrerInfoDetails)
        {
            var filteredList = new List<CarrierInfo>();
            
            //This is FirstCondition
            var UAList = carrerInfoDetails.FindAll(x => (x.IsActive == true) && (x.IsStarAirline == true) && (x.CarrierCode == "UA") && (x.AppID == "1"));

            //Second condtion
            var NonUAList = carrerInfoDetails.FindAll(x => (x.IsActive == true) && (x.IsStarAirline == true) && (x.CarrierCode != "UA") && (x.CarrierCode != "STAR") && (x.AppID == "1"));
            var sortedListed = NonUAList.AsEnumerable()
               .OrderBy(x => x.CarrierFullName)
               .ToList();
           
            UAList.AddRange(sortedListed);
            foreach (CarrierInfoDetails UAVal in UAList)
            {
                CarrierInfo carrierInfoObj = new CarrierInfo { };
                carrierInfoObj.Code = UAVal.CarrierCode;
                carrierInfoObj.Name = UAVal.CarrierFullName;
                filteredList.Add(carrierInfoObj);
            }

            return filteredList;

        }

    }
}
