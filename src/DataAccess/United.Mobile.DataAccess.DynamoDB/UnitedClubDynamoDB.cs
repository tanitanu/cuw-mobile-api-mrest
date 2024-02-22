using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.ClubPasses;

namespace United.Mobile.DataAccess.DynamoDB
{
  public  class UnitedClubDynamoDB
    {
            private readonly IConfiguration _configuration;
            private readonly IDynamoDBService _dynamoDBService;
            private string tableName = string.Empty;

            public UnitedClubDynamoDB(IConfiguration configuration, IDynamoDBService dynamoDBService)
            {
                _configuration = configuration;
                _dynamoDBService = dynamoDBService;
                tableName = _configuration.GetSection("DynamoDBTables").GetValue<string>("utb_UnitedClubAndLounges");                
            }
        
            public List<Club> GetClubAndLoungesDetailsList<Club>(string airportCode)
            {                       
             List<Club> unitedClubAndLoungesDetails = _dynamoDBService.GetRecords<List<Club>>(tableName, "Unitedlonges", airportCode, string.Empty).Result;
             return unitedClubAndLoungesDetails;
            }
    }
  }


