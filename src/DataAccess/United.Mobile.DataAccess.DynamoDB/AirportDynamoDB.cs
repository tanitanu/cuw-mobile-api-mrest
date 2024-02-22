using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Internal.Common;
using United.Mobile.Model.Shopping;

namespace United.Mobile.DataAccess.DynamoDB
{
    public class AirportDynamoDB
    {
        private readonly IConfiguration _configuration;
        private readonly IDynamoDBService _dynamoDBService;
        private string tableName = string.Empty;

        public AirportDynamoDB(IConfiguration configuration
            , IDynamoDBService dynamoDBService)
        {
            _configuration = configuration;
            _dynamoDBService = dynamoDBService;
            tableName = _configuration.GetSection("DynamoDBTables").GetValue<string>("utb_Airport");
            if (string.IsNullOrEmpty(tableName))
                tableName = "utb_Airport";
        }

        public async Task<DisplayAirportDetails> GetAirport(string airportCode, string sessionId)
        {
            try
            {
                return await _dynamoDBService.GetRecords<DisplayAirportDetails>(tableName, "Airport-" + airportCode.ToUpper(), airportCode.ToUpper(), sessionId);

            }
            catch (Exception)
            { }
            return default;
        }

        public async Task<string> GetAirportName(string airportCode, string sessionId)
        {
            var response = await _dynamoDBService.GetRecords<DisplayAirportDetails>(tableName, "Airport-" + airportCode.ToUpper(), airportCode.ToUpper(), sessionId);
            return string.IsNullOrEmpty(response?.AirportNameMobile) ? airportCode : response?.AirportNameMobile;
        }

        public async Task<string> GetCarrierInfo(string carrierCode, string sessionId)
        {
            #region
            //using (SqlCommand cmd = new SqlCommand("usp_GetCarrierInfo", conn))
            //{
            //    cmd.CommandType = CommandType.StoredProcedure;
            //    cmd.Parameters.Add("@CarrierCode", SqlDbType.VarChar).Value = carrierCode;
            //    try
            //    {
            //        await conn.OpenAsync();
            //        SqlDataReader reader = await cmd.ExecuteReaderAsync();
            //        while (reader.Read())
            //        {
            //            carrierName = Convert.ToString(reader["AirlineName"]);
            //        }
            //    }
            //    catch (Exception ex) { string msg = ex.Message; }
            //}
            #endregion
            switch (carrierCode.ToUpper().Trim())
            {
                case "UX": return "United Express";
                case "US": return "US Airways";
                default:
                    var response = await _dynamoDBService.GetRecords<DisplayAirportDetails>(tableName, "Airport-" + carrierCode.ToUpper(), carrierCode.ToUpper(), sessionId);
                    return response?.AirportName;
            }
        }

        public async Task<(bool returnvalue,  string airportName, string cityName)> GetAirportCityName(string airportCode, string sessionId, string airportName, string cityName)
        {
            #region
            //    Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //    DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("usp_Select_AirportName");
            //    database.AddInParameter(dbCommand, "@AirportCode", DbType.String, airportCode);
            //            USE[iPhone]
            //GO
            ///****** Object: StoredProcedure [dbo].[usp_Select_AirportName] Script Date: 08-11-2021 21:10:00 ******/
            //SET ANSI_NULLS OFF
            //GO
            //SET QUOTED_IDENTIFIER OFF
            //GO



            //ALTER PROCEDURE[dbo].[usp_Select_AirportName]

            //@AirportCode CHAR(3)



            //AS



            //SELECT AirportNameMobile as AirportName, CityName,AirportNameMobile
            //FROM dbo.utb_Airport NOLOCK
            //WHERE AirportCode = @AirportCode
            #endregion
            var airportDetails = await _dynamoDBService.GetRecords<DisplayAirportDetails>(_configuration?.GetValue<string>("DynamoDBTables:utb_Airport"), "airportCode01", airportCode, sessionId);
            if (airportDetails == null)
            {
                return (false,default,default);
            }

            airportName = airportDetails?.AirportNameMobile;
            airportCode = airportDetails?.AirportCode;
            cityName = airportDetails?.CityName;

            return (true,airportName,cityName);
        }

        public async Task<List<MOBDisplayBagTrackAirportDetails>> GetAirportNamesList<T>(string airportCode, string sessionId)
        {
            //try
            //{
            //    var airportDetails = await _dynamoDBService.GetRecords<MOBDisplayBagTrackAirportDetails>(_configuration?.GetValue<string>("DynamoDBTables:utb_Airport"), "airportCode01", airportCode, sessionId);
            //}
            //catch (Exception ex)
            //{ }
            var displayAirportDetails = new List<MOBDisplayBagTrackAirportDetails>();

            var response = await _dynamoDBService.GetRecords<MOBDisplayBagTrackAirportDetails>(_configuration?.GetValue<string>("DynamoDBTables:utb_Airport"), "airportCode01", airportCode, sessionId);

            if (response != null)
            {
                displayAirportDetails.Add(new MOBDisplayBagTrackAirportDetails()
                {
                    AirportCode = response.AirportCode,
                    CityName = response.CityName,
                    AirportNameMobile = response.AirportNameMobile
                }
                );
            }

            return displayAirportDetails;
        }

        //SQL StoreProc: usp_Select_ClubGeoInfo
        public async Task<T> GetClubGeoLocationInfo<T>(string clubLocationId)
        {
            #region //SQL query
            /*USE [iPhone]
                GO

                DECLARE 	@ClubLocationId INT
                SET @ClubLocationId = 355

                SELECT PlaceId, VenueId, Latitude, Longitude, LLPoiId
                FROM utb_ClubGeoInfo NOLOCK
                WHERE ClubLocationId = @ClubLocationId
            */
            #endregion

            #region //SQL query for utb_ClubGeoInfo
            /*
             SELECT '{"ClubLocationId":"',ClubLocationId,'","PlaceId":"', PlaceId,'","VenueId":"', VenueId
             ,'","Latitude":"', Latitude,'","Longitude":"', Longitude,'","LLPoiId":"', LLPoiId,'"}'
             FROM utb_ClubGeoInfo NOLOCK
             */
            #endregion
            string tableName = _configuration.GetSection("DynamoDBTables").GetValue<string>("utb_ClubGeoInfo");
            try
            {
                return await _dynamoDBService.GetRecords<T>(tableName, "ClubGeoInfo001", clubLocationId, string.Empty);
            }
            catch { }

            return default;
        }

        public async Task<T> GetCarriers<T>(string sessionId)
        {
            string tableName = _configuration.GetValue<string>("DynamoDBTables:CarrierInfo");
            return await _dynamoDBService.GetRecords<T>(tableName, "FLIFOD1", "XV", sessionId);
        }

        public string GetAirportCityName(string airportCode, ref string airportName, ref string cityName, string sessionId)
        {
            #region
            //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("usp_Select_AirportName");
            //database.AddInParameter(dbCommand, "@AirportCode", DbType.String, airportCode);

            //using (IDataReader dataReader = database.ExecuteReader(dbCommand))
            //{
            //    while (dataReader.Read())
            //    {
            //        airportName = dataReader["AirportName"].ToString();
            //        cityName = dataReader["CityName"].ToString();
            //    }
            //}
            #endregion
            try
            {
                var response = _dynamoDBService.GetRecords<DisplayAirportDetails>(tableName, "Airport-" + airportCode.ToUpper(), airportCode.ToUpper(), sessionId).Result;
                if (response != null)
                {
                    airportName = response.AirportNameMobile;
                    cityName = response.CityName;
                }
            }
            catch
            {

            }

            return default;
        }
    }
}
