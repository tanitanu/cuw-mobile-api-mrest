using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;
using United.Mobile.DataAccess.Common;
using United.Mobile.Model.Internal.Common;

namespace United.Mobile.DataAccess.DynamoDB
{
    public class FlightrequestDynamoDB
    {
        private readonly IConfiguration _configuration;
        private readonly IDynamoDBService _dynamoDBService;
        public FlightrequestDynamoDB(IConfiguration configuration, IDynamoDBService dynamoDBService)
        {
            _configuration = configuration;
            _dynamoDBService = dynamoDBService;
        }

        public async Task<string> GetComplimentary_Upgrade_Offered_flag_By_Cabin_Count(string from, string to, int numberOfCabins, string sessionId)
        {
            var response = await _dynamoDBService.GetRecords<ComplimentaryUpgradeCabin>("abh-PSS_CabinBrandingRule", "Cabin01", "ORDIAH_Cabin3", sessionId);
            string seatMapLegendId = string.Empty;
            int secondCabinBrandingId = string.IsNullOrEmpty(response.SecondCabinBrandingId) ? 0 : Convert.ToInt32(response.SecondCabinBrandingId);
            int thirdCabinBrandingId = string.IsNullOrEmpty(response.ThirdCabinBrandingId) ? 0 : Convert.ToInt32(response.ThirdCabinBrandingId);

            if (thirdCabinBrandingId == 0)
            {
                if (secondCabinBrandingId == 1)
                {
                    seatMapLegendId = "seatmap_legend5";
                }
                else if (secondCabinBrandingId == 2)
                {
                    seatMapLegendId = "seatmap_legend4";
                }
                else if (secondCabinBrandingId == 3)
                {
                    seatMapLegendId = "seatmap_legend3";
                }
            }
            else if (thirdCabinBrandingId == 1)
            {
                seatMapLegendId = "seatmap_legend2";
            }
            else if (thirdCabinBrandingId == 4)
            {
                seatMapLegendId = "seatmap_legend1";
            }
            return seatMapLegendId;
        }
    }
}
