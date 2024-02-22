using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Internal.Common;

namespace United.Mobile.DataAccess.DynamoDB
{
    public class CallingCardDynamoDB
    {
        private readonly IConfiguration _configuration;
        private readonly IDynamoDBService _dynamoDBService;
        private string tableName = string.Empty;

        public CallingCardDynamoDB(IConfiguration configuration
            , IDynamoDBService dynamoDBService)
        {
            _configuration = configuration;
            _dynamoDBService = dynamoDBService;
            tableName = _configuration.GetSection("DynamoDBTables").GetValue<string>("utb_CallingCard");
            if (string.IsNullOrEmpty(tableName))
                tableName = "cuw_CallingCard";
        }
        public async Task<List<CallingCard>> GetInternationalCallingCard(string sessionId)
        {
            #region
            //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("uasp_Get_CallingCard");
            //List<string> lstPhoneNumber = null;
            //MOBContactAccessNumber CityContact = null;
            //using (IDataReader dataReader = database.ExecuteReader(dbCommand))
            //{
            //while (dataReader.Read())
            //{
            //    if (items == null)
            //    {
            //        items = new List<MOBContactUSOusideUSACanadaContactTypePhone>();
            //    }
            //    MOBContactUSOusideUSACanadaContactTypePhone item = items.FirstOrDefault(c => c.Country.CountryCode == dataReader["CountryCode"].ToString());

            //    lstPhoneNumber = new List<string>();
            //    if (dataReader["PhoneNumber"].ToString().Trim() != string.Empty)
            //        lstPhoneNumber.Add(dataReader["PhoneNumber"].ToString());
            //    if (dataReader["PhoneNumber2"].ToString().Trim() != string.Empty)
            //        lstPhoneNumber.Add(dataReader["PhoneNumber2"].ToString());
            //    if (dataReader["PhoneNumber3"].ToString().Trim() != string.Empty)
            //        lstPhoneNumber.Add(dataReader["PhoneNumber3"].ToString());
            //    if (dataReader["PhoneNumber4"].ToString().Trim() != string.Empty)
            //        lstPhoneNumber.Add(dataReader["PhoneNumber4"].ToString());
            //    CityContact = new MOBContactAccessNumber
            //    {
            //        City = dataReader["CityName"].ToString(),
            //        ATTDirectAccessNumbers = lstPhoneNumber
            //    };
            //    if (item == null)
            //    {
            //        item = new MOBContactUSOusideUSACanadaContactTypePhone();
            //        MOBBKCountry country = new MOBBKCountry();
            //        item.Country = new MOBBKCountry
            //        {
            //            CountryCode = dataReader["CountryCode"].ToString(),
            //            Name = dataReader["CountryName"].ToString(),
            //            ShortName = dataReader["CountryLongName"].ToString()
            //        };
            //        item.ContactAccessNumberList = new List<MOBContactAccessNumber>();
            //        item.ContactAccessNumberList.Add(CityContact);
            //        items.Add(item);
            //    }
            //    else
            //    {

            //        item.ContactAccessNumberList.Add(CityContact);
            //        item.ContactAccessNumberList = item.ContactAccessNumberList.OrderBy(c => c.City).ToList();
            //    }

            //}
            //    return items;
            //}
            #endregion

            return await _dynamoDBService.GetRecords<List<CallingCard>>(tableName, "CallingCard01","callingCard" ,sessionId);
        }
    }
}
