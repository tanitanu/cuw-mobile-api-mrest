using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Internal.Common;

namespace United.Mobile.DataAccess.DynamoDB
{
    public class FlightStatusDynamoDB
    {
        private readonly IConfiguration _configuration;
        private readonly IDynamoDBService _dynamoDBService;

        public FlightStatusDynamoDB(IConfiguration configuration, IDynamoDBService dynamoDBService)
        {
            _configuration = configuration;
            _dynamoDBService = dynamoDBService;
        }

        /// <summary>
        /// Transformed mRest sp usp_Select_AirportName
        /// </summary>
        /// <param name="airportCode"></param>
        /// <param name="sessionId"></param>
        /// <returns></returns>
        public async Task<T> GetAirport<T>(string airportCode, string sessionId)
        {
            return await _dynamoDBService.GetRecords<T>(_configuration.GetValue<string>("DynamoDBTables:utb_Airport"), "Airport-" + airportCode.ToUpper(), airportCode.ToUpper(), sessionId);
        }

        /// <summary>
        /// Transformed mRest sp uasp_Get_Airport_Advisory_Messages
        /// </summary>
        /// <param name="airports"></param>
        /// <param name="flightDate"></param>
        /// <returns></returns>
        public async Task<T> GetAirportAdvisoryMessages<T>(string airports, string flightDate, string transactionId)
        {
            var tableName = _configuration.GetSection("DynamoDBTable").GetValue<string>("uatb_Airport_Advisory_Message") ?? "cuw-airport-advisory-message";
            var response = await _dynamoDBService.GetRecords<string>(tableName, transactionId, airports, string.Empty);
            if (string.IsNullOrEmpty(response))
                return default;

            return JsonConvert.DeserializeObject<T>(response); 
        }

        /// <summary>
        /// Transformed mRest sp usp_GetCabinCount_By_ShipNumber
        /// </summary>
        /// <param name="ship"></param>
        /// <returns></returns>
        public async Task<T> GetCabinCountByShipNumber<T>(string ship)
        {
            return await _dynamoDBService.GetRecords<T>(_configuration.GetValue<string>("DynamoDBTables:PSS_SHIP"), "abh-PSS_SHIP_" + ship, ship, string.Empty);
        }

        /// <summary>
        /// Transformed sp uatb_PushToken_Complications
        /// </summary>
        /// <param name="fsRequest"></param>
        /// <returns></returns>
        public async Task<bool> InsertFlifoPushToken<T>(string key, T pushTokenComplication)
        {
            return await _dynamoDBService.SaveRecords<T>(_configuration.GetValue<string>("DynamoDBTables:uatb_PushToken_Complications"), "flifoToken01", key
                , pushTokenComplication, string.Empty);
        }

        /// <summary>
        /// Transformed mRest sp usp_Get_Verify_WiFi_Available_Ships
        /// </summary>
        /// <param name="shipNumbersToQuery"></param>
        /// <returns></returns>
        public async Task<Tuple<bool, List<string>>> VerifyWiFiAvailable(List<string> shipNumbersToQuery)
        {
            var pssShips = new List<PssShip>();
            foreach (var shipNumbers in shipNumbersToQuery)
            {
                var pssShip = await _dynamoDBService.GetRecords<PssShip>(_configuration.GetValue<string>("DynamoDBTables:PSS_SHIP"), "abh-PSS_SHIP_" + shipNumbersToQuery.FirstOrDefault(), string.Join(",", shipNumbersToQuery), string.Empty);
                if (pssShip != null)
                    pssShips.Add(pssShip);
            }
            return new Tuple<bool, List<string>>(pssShips.Any(), pssShips.Select(p => p.Ship.ToString()).ToList());
        }

        public async Task<T> GetFlightStatusFromCache<T>(int flightNumber, string flightDate, string origin, string destination, string sessionId)
        {
            string key = string.Format("FLIFO::{0}::{1}::{2}::{3}", flightNumber, flightDate, origin, destination);
            return await _dynamoDBService.GetRecords<T>(_configuration.GetValue<string>("DynamoDBTables:uatb_TravelReady_Document"), "flightStatusFromCache01", key, sessionId);
        }

        public async Task<bool> SaveFlightStatusToCache<T>(T saveObj, string key, string sessionId)
        {
            return await _dynamoDBService.SaveRecords<T>(_configuration.GetValue<string>("DynamoDBTables:uatb_TravelReady_Document"), "saveFlightStatusToCache01", key, saveObj, sessionId);
        }
    }
}