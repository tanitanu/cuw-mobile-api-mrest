using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.Model.Common;

namespace United.Mobile.EligibleCheck.Domain
{
    public class ShuttleOfferInfo
    {
        private readonly IConfiguration _configuration;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly DocumentLibraryDynamoDB _documentLibraryDynamoDB;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly IHeaders _headers;

        public ShuttleOfferInfo(IConfiguration configuration
            , IDynamoDBService dynamoDBService
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , IHeaders headers)
        {
            _configuration = configuration;
            _dynamoDBService = dynamoDBService;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _headers = headers;
            _documentLibraryDynamoDB = new DocumentLibraryDynamoDB(_configuration, _dynamoDBService);
        }

        public async Task<MOBShuttleOffer> GetEWRShuttleOfferInformation()
        {
            MOBShuttleOffer shuttleOffer = new MOBShuttleOffer();
            string offerCode = "offerCode";
            string offerText1 = "offerText1";
            string offerText2 = "offerText2";
            string currencyCode = "currencyCode";
            string offerPrice = "offerPrice";
            string offerText3 = "offerText3";
            string offerTileImageName = "offerTileImageName";
            string eligibleAirport = "eligibleAirport";
            string shuttleStation = "shuttleStation";
            string formattedPriceText = "Check price";
            string shuttleStationDescription = "shuttleStationDescription";
            string childWarningMessage = "childWarningMessage";
            string childCutoffAge = "childCutoffAge";
            try
            {
                var offerItems = await _documentLibraryDynamoDB.GetNewLegalDocumentsForTitles(new List<string> { "EWRNYY_ShuttleOffer_Content" }, _headers.ContextValues.SessionId).ConfigureAwait(false);
                if (offerItems != null && offerItems.Any())
                {
                    shuttleOffer.OfferCode = GetFirstOrDefaultShuttleOfferValue(offerItems, offerCode);
                    shuttleOffer.Text1 = GetFirstOrDefaultShuttleOfferValue(offerItems, offerText1);
                    shuttleOffer.Title = GetFirstOrDefaultShuttleOfferValue(offerItems, offerText2);
                    shuttleOffer.Price = Convert.ToDecimal(GetFirstOrDefaultShuttleOfferValue(offerItems, offerPrice));
                    shuttleOffer.CurrencyCode = GetFirstOrDefaultShuttleOfferValue(offerItems, currencyCode);

                    if (shuttleOffer.Price > 0)
                    {
                        shuttleOffer.FormattedPrice = string.Format("{1}{0:0.00}", Convert.ToDecimal(shuttleOffer.Price), shuttleOffer.CurrencyCode);
                        shuttleOffer.Text2 = GetFirstOrDefaultShuttleOfferValue(offerItems, offerText3);
                    }
                    else
                    {
                        shuttleOffer.FormattedPrice = formattedPriceText;
                        shuttleOffer.Text2 = string.Empty;
                    }

                    shuttleOffer.OfferTileImageName = GetFirstOrDefaultShuttleOfferValue(offerItems, offerTileImageName);
                    shuttleOffer.EligibleAirport = GetFirstOrDefaultShuttleOfferValue(offerItems, eligibleAirport);
                    shuttleOffer.ShuttleStation = GetFirstOrDefaultShuttleOfferValue(offerItems, shuttleStation);
                    shuttleOffer.ShuttleStationDescription = GetFirstOrDefaultShuttleOfferValue(offerItems, shuttleStationDescription);
                    shuttleOffer.ChildCutoffAge = GetFirstOrDefaultShuttleOfferValue(offerItems, childCutoffAge);
                    shuttleOffer.ChildWarningMessage = GetFirstOrDefaultShuttleOfferValue(offerItems, childWarningMessage);
                }
            }
            catch (Exception ex) { return null; }
            return shuttleOffer;
        }

        private string GetFirstOrDefaultShuttleOfferValue(List<MOBLegalDocument> documents, string keyname)
        {
            try
            {
                return (documents.FirstOrDefault
                         (x => string.Equals(x.Title, keyname, StringComparison.OrdinalIgnoreCase)) != null)
                         ? documents.FirstOrDefault(x => string.Equals(x.Title, keyname, StringComparison.OrdinalIgnoreCase)).LegalDocument
                         : string.Empty;
            }
            catch (Exception ex) { return string.Empty; }
        }

        public async Task<List<MOBItem>> GetDBDisplayContent(string contentname)
        {
            List<MOBItem> mobcontentitem;
            if (string.IsNullOrEmpty(contentname)) return null;
            try
            {
                var messageitems = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles( contentname , _headers.ContextValues.TransactionId,true).ConfigureAwait(false);
                if (messageitems != null && messageitems.Any())
                {
                    mobcontentitem = new List<MOBItem>();
                    foreach (MOBLegalDocument doc in messageitems)
                    {
                        mobcontentitem.Add(new MOBItem() { Id = doc.Title, CurrentValue = doc.LegalDocument });
                    }
                    return mobcontentitem;
                }
            }
            catch (Exception ex) { return null; }
            return null;
        }
    }
}
