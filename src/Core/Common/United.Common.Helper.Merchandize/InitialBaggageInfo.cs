using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using United.Definition;
using United.Definition.Shopping;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Services.FlightShopping.Common.Extensions;
using United.Utility.Helper;

namespace United.Common.Helper.Merchandize
{
    public class InitialBaggageInfo : IBaggageInfo
    {
        private const string DOTBaggageInfoDBTitle1 = "DOTBaggageInfoText1";
        private const string DOTBaggageInfoDBTitle1ELF = "DOTBaggageInfoText1 - ELF";
        private const string DOTBaggageInfoDBTitle1IBE = "DOTBaggageInfoText1 - IBE";
        private const string DOTBaggageInfoDBTitle2 = "DOTBaggageInfoText2";
        private const string DOTBaggageInfoDBTitle3 = "DOTBaggageInfoText3";
        private const string DOTBaggageInfoDBTitle3IBE = "DOTBaggageInfoText3IBE";
        private const string DOTBaggageInfoDBTitle4 = "DOTBaggageInfoText4";
        //private readonly ILegalDocument legalDocumentProvider;
        private List<MOBLegalDocument> cachedLegalDocuments = null;
        private readonly IConfiguration _configuration;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly DocumentLibraryDynamoDB _documentLibraryDynamoDB;
        private readonly IHeaders _headers;

        private static readonly List<string> Titles = new List<string>
        {
            DOTBaggageInfoDBTitle1,
            DOTBaggageInfoDBTitle1ELF,
            DOTBaggageInfoDBTitle2,
            DOTBaggageInfoDBTitle3,
            DOTBaggageInfoDBTitle4,
            DOTBaggageInfoDBTitle1IBE,
            DOTBaggageInfoDBTitle3IBE
        };

        public InitialBaggageInfo(IConfiguration configuration
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , IHeaders headers)
        {
            _configuration = configuration;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _headers = headers;
        }

        //public InitialBaggageInfo(ILegalDocument legalDocumentProvider)
        //{
        //    this.legalDocumentProvider = legalDocumentProvider;
        //}

        public async Task<DOTBaggageInfo> GetBaggageInfo(MOBSHOPReservation reservation)
        {
            var isElf = reservation != null && reservation.IsELF;

            var isIBE = EnableIBEFull() && reservation != null && (reservation.ShopReservationInfo2 != null) && reservation.ShopReservationInfo2.IsIBE;

            return await GetBaggageInfo(isElf, isIBE);
        }

        private bool EnableIBEFull()
        {
            return _configuration.GetValue<bool>("EnableIBE");
        }

        public async Task<DOTBaggageInfo> GetBaggageInfo(United.Service.Presentation.ReservationModel.Reservation reservation)
        {
            if (reservation == null && reservation.Type.IsNullOrEmpty())
                return null;
          
           var isElf = reservation.FlightSegments.Any(p => !p.IsNullOrEmpty() &&
                                                           !p.FlightSegment.IsNullOrEmpty() && 
                                                            IsElf(p.FlightSegment.Characteristic));
            
            var isIBE = HasIBeSegments(reservation.Type);

            return await GetBaggageInfo(isElf, isIBE);
        }

        private  bool HasIBeSegments(Collection<United.Service.Presentation.CommonModel.Genre> type)
        {
            return _configuration.GetValue<bool>("EnableIBE") && type != null && type.Any(IsIBEtype);
        }


        private  bool IsIBEtype(Service.Presentation.CommonModel.Genre type)
        {
            if (EnablePBE())
            {
                var IBEFullProductCodes = _configuration.GetValue<string>("IBEFullShoppingProductCodes").Split(',');
                return type != null
                      && type.Key != null && IBEFullProductCodes.Contains(type.Key.Trim().ToUpper())
                      && IsIBEFullFare(type.Value);
            }
            else
            {
                return type != null
                       && type.Key != null && type.Key.Trim().ToUpper() == "IBE"
                       && IsIBEFullFare(type.Value);
            }
        }

        private bool IsIBEFullFare(string productCode)
        {
            var iBEFullProductCodes = _configuration.GetValue<string>("IBEFullShoppingProductCodes");
            return EnableIBEFull() && !string.IsNullOrWhiteSpace(productCode) &&
                   !string.IsNullOrWhiteSpace(iBEFullProductCodes) &&
                   iBEFullProductCodes.IndexOf(productCode.Trim().ToUpper()) > -1;
        }

        private bool EnablePBE()
        {
            return _configuration.GetValue<bool>("EnablePBE");
        }

        private bool IsElf(Collection<United.Service.Presentation.CommonModel.Characteristic> characteristics)
        {
            return _configuration.GetValue<bool>("EnableIBE") &&
                    characteristics != null &&
                    characteristics.Any(c => c != null &&
                                            "PRODUCTCODE".Equals(c.Code, StringComparison.OrdinalIgnoreCase) &&
                                            "ELF".Equals(c.Value, StringComparison.OrdinalIgnoreCase));
        }

        public async Task<DOTBaggageInfo> GetBaggageInfo(MOBPNR pnr)
        {
            var isElf = pnr != null && pnr.isELF;
            var isIbe = pnr != null && pnr.IsIBE;
            return await GetBaggageInfo(isElf, isIbe);
        }

        private async Task<DOTBaggageInfo> GetBaggageInfo(bool isElf, bool isIBE)
        {
            if (cachedLegalDocuments.IsNull())
            {
                cachedLegalDocuments = await GetLegalDocumentsForTitles(Titles);
            }
            var legalDocuments = cachedLegalDocuments.Clone();

            if (isElf || isIBE)
            {
                legalDocuments.RemoveAll(l => l.Title == DOTBaggageInfoDBTitle1);
                if (isIBE)
                {
                    legalDocuments.RemoveAll(l => l.Title == DOTBaggageInfoDBTitle1ELF);
                    legalDocuments.RemoveAll(l => l.Title == DOTBaggageInfoDBTitle3);
                }
                else
                {
                    legalDocuments.RemoveAll(l => l.Title == DOTBaggageInfoDBTitle1IBE);
                    legalDocuments.RemoveAll(l => l.Title == DOTBaggageInfoDBTitle3IBE);
                }
            }
            else
            {
                legalDocuments.RemoveAll(l => l.Title == DOTBaggageInfoDBTitle1ELF);
                legalDocuments.RemoveAll(l => l.Title == DOTBaggageInfoDBTitle1IBE);
                legalDocuments.RemoveAll(l => l.Title == DOTBaggageInfoDBTitle3IBE);
            }

            var document1TitleAndDescription = legalDocuments.First(l => l.Title.Contains(DOTBaggageInfoDBTitle1)).LegalDocument.Split('|');
            var document2TitleAndDescription = legalDocuments.First(l => l.Title.Contains(DOTBaggageInfoDBTitle2)).LegalDocument.Split('|');
            var document3TitleAndDescription = legalDocuments.First(l => l.Title.Contains(DOTBaggageInfoDBTitle3)).LegalDocument.Split('|');
            var document4 = legalDocuments.First(l => l.Title.Contains(DOTBaggageInfoDBTitle4)).LegalDocument;

            return new DOTBaggageInfo
            {
                Title1 = document1TitleAndDescription[0],
                Title2 = document2TitleAndDescription[0],
                Title3 = document3TitleAndDescription[0],
                Description1 = document1TitleAndDescription[1],
                Description2 = document2TitleAndDescription[1],
                Description3 = document3TitleAndDescription[1],
                Description4 = document4
            };
        }

        private async Task<List<MOBLegalDocument>> GetLegalDocumentsForTitles(List<string> titles)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var title in titles)
            {
                stringBuilder.Append("'");
                stringBuilder.Append(title);
                stringBuilder.Append("'");
                stringBuilder.Append(",");
            }
            string reqTitles = stringBuilder.ToString().Trim(',');
            List<MOBLegalDocument> documents = null;

            if (titles != null && titles.Count > 0)
            {
                documents = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(reqTitles, _headers.ContextValues.TransactionId, true).ConfigureAwait(false);
            }
            return documents;
        }
    }
}