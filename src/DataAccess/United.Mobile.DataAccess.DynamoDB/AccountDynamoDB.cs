using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.DataAccess.Common;

namespace United.Mobile.DataAccess.DynamoDB
{
    public class AccountDynamoDB
    {
        private readonly IConfiguration _configuration;
        private readonly IDynamoDBService _dynamoDBService;

        public AccountDynamoDB(IConfiguration configuration
            , IDynamoDBService dynamoDBService)
        {
            _configuration = configuration;
            _dynamoDBService = dynamoDBService;
        }
        public async Task<bool> IsTSAFlaggedAccount(string key, string sessionID)
        {
            #region
            //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("usp_Select_TSA_Flagged_account");
            //database.AddInParameter(dbCommand, "@AccountNumber", DbType.String, accountNumber);

            //try
            //{
            //    using (IDataReader dataReader = database.ExecuteReader(dbCommand))
            //    {
            //        while (dataReader.Read())
            //        {
            //            if (Convert.ToInt32(dataReader["AccountExists"]) != 0)
            //            {
            //                flaggedAccount = true;
            //            }
            //        }
            //    }
            //}
            //catch (System.Exception) { }
            #endregion
            try
            {
                string tableName = _configuration.GetSection("DynamoDBTables").GetValue<string>("utb_TSA_Flagged_Account");
                var accountNum= await _dynamoDBService.GetRecords<bool>(tableName, "Account001", key, sessionID);
                return true;
            }
            catch { }
            return false;

        }
    }
}
