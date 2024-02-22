using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using United.Mobile.DataAccess.Common;

namespace United.Mobile.DataAccess.DynamoDB
{
    public class EResBetaTesterDynamoDB
    {
        private readonly IConfiguration _configuration;
        private readonly IDynamoDBService _dynamoDBService;
        public EResBetaTesterDynamoDB(IConfiguration configuration, IDynamoDBService dynamoDBService)
        {
            _configuration = configuration;
            _dynamoDBService = dynamoDBService;
        }

        public async Task<T> GetEResBetaTesterItems<T>(string applicationId, string appVersion, string mileageplusNumber, string sessionId)
        {
            string tableName = _configuration.GetSection("DynamoDBTables").GetValue<string>("uatb_EResBetaTester");
            string key = applicationId + "::" + appVersion + "::" + mileageplusNumber;
            return await _dynamoDBService.GetRecords<T>(tableName, "EResBetaTester001", key, sessionId);
        }
    }
}
