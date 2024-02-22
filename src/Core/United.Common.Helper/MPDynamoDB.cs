using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.Model.Common;

namespace United.Common.Helper
{
    public class MPDynamoDB
    {
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IConfiguration _configuration;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly IHeaders _headers;

        public MPDynamoDB(IConfiguration configuration
            , IDynamoDBService dynamoDB
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , IHeaders headers)
        {
            _configuration = configuration;
            _dynamoDBService = dynamoDB;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _headers = headers;
        }

        public async Task<List<MOBItem>> GetMPPINPWDTitleMessages(List<string> titleList)
        {
            bool isTermsnConditions = false;
            StringBuilder stringBuilder = new StringBuilder();
            if (!isTermsnConditions)
            {
                foreach (var title in titleList)
                {
                    stringBuilder.Append("'");
                    stringBuilder.Append(title);
                    stringBuilder.Append("'");
                    stringBuilder.Append(",");
                }
            }
            else
            {
                stringBuilder.Append(titleList[0]);
            }

            string reqTitles = stringBuilder.ToString().Trim(',');
            var docs = new List<MOBLegalDocument>();
            try
            {
                docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(reqTitles, _headers.ContextValues.TransactionId, true);
            }
            catch (Exception ex)
            {
            }
            List<MOBItem> messages = new List<MOBItem>();
            if (!_configuration.GetValue<bool>("DisableCubaTravelContentOrderMismatchFix") && !string.IsNullOrEmpty(reqTitles) && reqTitles == "'CUBA_TRAVEL_CONTENT'" && docs != null && docs.Count > 0)
            {
                AddMobLegalDocumentItem(docs, messages, "CubaPage1Title");

                AddMobLegalDocumentItem(docs, messages, "CubaPage1Description");

                AddMobLegalDocumentItem(docs, messages, "CubaPage1ReasonListButton");
            }
            else if (docs != null && docs.Count > 0)
            {
                foreach (MOBLegalDocument doc in docs)
                {
                    MOBItem item = new MOBItem
                    {
                        Id = doc.Title,
                        CurrentValue = doc.LegalDocument
                    };
                    messages.Add(item);
                }
            }
            return messages;
        }
        private void AddMobLegalDocumentItem(List<MOBLegalDocument> docs, List<MOBItem> messages, string key)
        {
            MOBLegalDocument doc = docs.FirstOrDefault(d => d != null && !string.IsNullOrEmpty(d.Title) && d.Title.Equals(key, StringComparison.OrdinalIgnoreCase));
            if (doc != null)
            {
                MOBItem item = new MOBItem
                {
                    Id = doc.Title,
                    CurrentValue = doc.LegalDocument
                };
                messages.Add(item);
            }
        }
    }
}
