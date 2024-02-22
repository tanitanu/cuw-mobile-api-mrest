using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Services.FlightShopping.Common.Extensions;

namespace United.Common.Helper.FOP
{
    public class ELFRitMetaShopMessages : IELFRitMetaShopMessages
    {
        private readonly IConfiguration _configuration;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly IHeaders _headers;

        public ELFRitMetaShopMessages(IConfiguration configuration
            , IDynamoDBService dynamoDBService
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , IHeaders headers)
        {
            _configuration = configuration;
            _dynamoDBService = dynamoDBService;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _headers = headers;
        }
        public async Task AddElfRtiMetaSearchMessages(MOBSHOPReservation reservation)
        {
            if (!reservation.IsELF)
                return;
            var elfRtiMessageList = (await GetMPPINPWDTitleMessages("ELF_RTI_MetaSearch")).OrderBy(m => Convert.ToInt32(m.Id)).ToList();

            var elfMetaSearchMessages = (from elfMessage in elfRtiMessageList
                                         select elfMessage.CurrentValue.Split('|')
                into descriptionAndIconArray
                                         where descriptionAndIconArray.Length == 2
                                         select new ItemWithIconName
                                         {
                                             OptionDescription = descriptionAndIconArray[0],
                                             OptionIcon = descriptionAndIconArray[1]
                                         }).ToList();
            reservation.ELFMessagesForVendorQuery.AddRange(elfMetaSearchMessages);
        }
        public async Task<List<MOBItem>> GetELFShopMessagesForRestrictions(United.Mobile.Model.Shopping.MOBSHOPReservation reservation, int appId = 0)
        {
            if (reservation == null) return null;

            var databaseKey = string.Empty;

            if (reservation.IsELF)
            {
                databaseKey = reservation.IsSSA ?
                              "SSA_ELF_RESTRICTIONS_APPLY_MESSAGES" :
                              "ELF_RESTRICTIONS_APPLY_MESSAGES";
            }
            else if (reservation.ShopReservationInfo2 != null && reservation.ShopReservationInfo2.IsIBELite)
            {
                databaseKey = "IBELite_RESTRICTIONS_APPLY_MESSAGES";
            }
            else if (reservation.ShopReservationInfo2 != null && reservation.ShopReservationInfo2.IsIBE)
            {
                if (_configuration.GetValue<bool>("EnablePBE"))
                {
                    string productCode = reservation?.Trips[0]?.FlattenedFlights?[0]?.Flights?[0].ShoppingProducts.First(p => p != null && p.IsIBE).ProductCode;
                    databaseKey = productCode + "_RESTRICTIONS_APPLY_MESSAGES";
                }
                else
                {
                    databaseKey = "IBE_RESTRICTIONS_APPLY_MESSAGES";
                }
            }

            var messages = !string.IsNullOrEmpty(databaseKey) ?
                  await new MPDynamoDB(_configuration, _dynamoDBService, null, _headers).GetMPPINPWDTitleMessages(new List<string> { databaseKey }) : null;

            if (!_configuration.GetValue<bool>("DisableRestrictionsForiOS"))
            {
                if (messages != null && appId == 1)
                {
                    try
                    {
                        UpdateFootNoteForiOS(messages);
                    }
                    catch (Exception ex) { }
                }
            }
            return messages;
        }

        public async Task<List<MOBItem>> GetMPPINPWDTitleMessages(string titleList)
        {
            List<MOBItem> messages = new List<MOBItem>();
            List<MOBLegalDocument> docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(titleList, _headers.ContextValues.TransactionId, true).ConfigureAwait(false);
            if (docs != null && docs.Count > 0)
            {
                foreach (MOBLegalDocument doc in docs)
                {
                    MOBItem item = new MOBItem();
                    item.Id = doc.Title;
                    item.CurrentValue = doc.LegalDocument;
                    messages.Add(item);
                }
            }
            return messages;
        }
        public async Task<List<MOBItem>> GetELFLimitationsViewRes(List<MOBSHOPTrip> shopTrip, Collection<United.Service.Presentation.SegmentModel.ReservationFlightSegment> FlightSegments, int appId = 0)
        {
            if (shopTrip.IsNullOrEmpty())
            { return null; }

            var databaseKey = string.Empty;
            bool isIBE = false;
            bool isELF = false;
            shopTrip.ForEach(p =>
            {
                if (!p.IsNullOrEmpty() && !p.FlattenedFlights.IsNullOrEmpty())
                {
                    isELF = p.FlattenedFlights.Any(k => k.IsElf);
                    isIBE = p.FlattenedFlights.Any(k => k.IsIBE);
                }
            });

            if (isELF)
            {
                databaseKey = "SSA_ELF_MR_Limitations";
            }
            else if (isIBE)
            {
                if (_configuration.GetValue<bool>("EnablePBE"))
                {
                    string productCode = string.Empty;
                    var flightSegment = FlightSegments?.FirstOrDefault(p => p != null && p.TripNumber != null)?.FlightSegment;
                    productCode = flightSegment?.Characteristic?.FirstOrDefault(c => c != null && !string.IsNullOrEmpty(c.Code) && c.Code.Equals("PRODUCTCODE", StringComparison.OrdinalIgnoreCase))?.Value;
                    databaseKey = productCode + "_MR_Limitations";
                }
                else
                {
                    databaseKey = "IBE_MR_Limitations";
                }
            }

            var messages = !string.IsNullOrEmpty(databaseKey) ?
                     (await GetMPPINPWDTitleMessages(databaseKey)) : null; ;

            if (messages != null && appId == 1)
            {
                try
                {
                    UpdateFootNoteForiOS(messages);
                }
                catch (Exception ex) { }
            }

            return messages;
        }

        private void UpdateFootNoteForiOS(List<MOBItem> messages)
        {
            var footNote = messages.Where(x => x.Id == _configuration.GetValue<string>("RestrictionsLimitationsFootNotes")).FirstOrDefault();
            if (footNote != null && footNote?.CurrentValue != null)
            {
                if (footNote.CurrentValue.StartsWith("<p>"))
                {
                    footNote.CurrentValue = footNote.CurrentValue.Replace("<p>", "").Replace("</p>", "").Replace("<br/><br/>", "\n\n");
                }
            }
        }
    }
}
