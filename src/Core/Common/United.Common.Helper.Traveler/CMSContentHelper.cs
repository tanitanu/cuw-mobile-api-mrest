using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using United.Definition;
using United.Mobile.DataAccess.CMSContent;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Utility.Helper;
using Reservation = United.Mobile.Model.Shopping.Reservation;

namespace United.Common.Helper.Traveler
{
    public class CMSContentHelper : ICMSContentHelper
    {
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly ICMSContentService _cMSContentService;
        private readonly ICacheLog<Traveler> _logger;
        private readonly IDynamoDBService _dynamoDBService;
        private DocumentLibraryDynamoDB _documentLibraryDynamoDB;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly IHeaders _headers;

        public CMSContentHelper(ICacheLog<Traveler> logger
            , IConfiguration configuration
             , IHeaders headers
            , ISessionHelperService sessionHelperService
            , ICMSContentService cMSContentService
            , IDynamoDBService dynamoDBService
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            )
        {
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _cMSContentService = cMSContentService;
            _logger = logger;
            _dynamoDBService = dynamoDBService;
            _headers= headers;
            _documentLibraryDynamoDB = new DocumentLibraryDynamoDB(_configuration, _dynamoDBService);
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
        }

        #region GetMobileCMSContents

        public async Task<List<MobileCMSContentMessages>> GetMobileCMSContents(MobileCMSContentRequest request)
        {
            string jsonResponse = await GETCSLCMSContent(request);

            List<MobileCMSContentMessages> cmsContentMessages = new List<MobileCMSContentMessages>();
            var sortedCMSContentMessages = new List<MobileCMSContentMessages>();

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                CSLContentMessagesResponse response = null;

                response = System.Text.Json.JsonSerializer.Deserialize<CSLContentMessagesResponse>(jsonResponse);

                if (response != null && (Convert.ToBoolean(response.Status) && response.Messages != null))
                {
                    foreach (CMSContentMessage contentMessage in response.Messages)
                    {
                        MobileCMSContentMessages cmsContentMessage = new MobileCMSContentMessages();

                        cmsContentMessage.ContentFull = string.IsNullOrEmpty(contentMessage.ContentShort) ? (string.IsNullOrEmpty(contentMessage.ContentFull) ? string.Empty : contentMessage.ContentFull.Trim()) : contentMessage.ContentShort.Trim();
                        cmsContentMessage.ContentFull = string.IsNullOrEmpty(cmsContentMessage.ContentFull) ? string.Empty : cmsContentMessage.ContentFull.Replace("\n", "");
                        cmsContentMessage.ContentShort = string.IsNullOrEmpty(contentMessage.ContentShort) ? string.Empty : contentMessage.ContentShort.Trim();
                        cmsContentMessage.HeadLine = string.IsNullOrEmpty(contentMessage.Headline) ? string.Empty : contentMessage.Headline.Trim();
                        cmsContentMessage.LocationCode = string.IsNullOrEmpty(contentMessage.LocationCode) ? string.Empty : contentMessage.LocationCode.Trim();
                        cmsContentMessage.Title = string.IsNullOrEmpty(contentMessage.Title) ? string.Empty : contentMessage.Title.Trim();
                        if (request.GroupName != null && request.GroupName.ToUpper().Trim() == "Mobile:UNMR".ToUpper().Trim())
                        {
                            cmsContentMessage.Title = string.Empty;
                        }
                        cmsContentMessages.Add(cmsContentMessage);
                    }
                    string sortOrderMobileCMSContentMessages = _configuration.GetValue<string>("SortOrderMobileCMSContentMessages-" + request.GroupName.Trim().ToUpper());
                    if (_configuration.GetValue<bool>("EnableIBE"))
                    {
                        if (!string.IsNullOrEmpty(request.SessionId))
                        {
                            bool isIBE = false;
                            if (!string.IsNullOrEmpty(request.Flow) && request.Flow.ToUpper().Equals(United.Utility.Enum.FlowType.VIEWRES.ToString()))
                            {
                                var reservationDetail = _sessionHelperService.GetSession<United.Service.Presentation.ReservationResponseModel.ReservationDetail>(_headers.ContextValues.SessionId, new Service.Presentation.ReservationResponseModel.ReservationDetail().GetType().FullName, new List<string> { _headers.ContextValues.SessionId, new Service.Presentation.ReservationResponseModel.ReservationDetail().GetType().FullName});
                                if (reservationDetail != null && reservationDetail.Result.Detail != null && reservationDetail.Result.Detail.Characteristic != null && reservationDetail.Result.Detail.Characteristic.Any())
                                {
                                    isIBE = IsIbe(reservationDetail.Result.Detail.Characteristic);
                                }
                            }
                            else
                            {
                                var bookingPathReservation = _sessionHelperService.GetSession<Reservation>(request.SessionId, new Reservation().ObjectName, new List<string>() { request.SessionId, new Reservation().ObjectName });
                                isIBE = IsIBE(bookingPathReservation.Result);
                            }
                            if (isIBE)
                            {
                                sortOrderMobileCMSContentMessages = sortOrderMobileCMSContentMessages = _configuration.GetValue<string>("SortOrderMobileCMSContentMessagesIBE-" + request.GroupName.Trim().ToUpper());
                            }
                        }
                    }
                    if (sortOrderMobileCMSContentMessages != null && sortOrderMobileCMSContentMessages.Trim().Split('~').Length > 0)
                    {
                        foreach (string locationCode in sortOrderMobileCMSContentMessages.Trim().Split('~').ToList())
                        {
                            foreach (MobileCMSContentMessages cmsContentMessage in cmsContentMessages)
                            {
                                if (locationCode.ToUpper().Trim() == cmsContentMessage.LocationCode.ToUpper().Trim())
                                {
                                    sortedCMSContentMessages.Add(cmsContentMessage);
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        sortedCMSContentMessages = cmsContentMessages;
                    }
                }
                else
                {
                    if (response.Errors != null && response.Errors.Count > 0)
                    {
                        string errorMessage = string.Empty;
                        foreach (var error in response.Errors)
                        {
                            errorMessage = errorMessage + " " + error.Message;
                        }
                        throw new MOBUnitedException(errorMessage);
                    }
                    else
                    {
                        string exceptionMessage = _configuration.GetValue<string>("UnableToRegisterTravelerErrorMessage");
                        if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                        {
                            exceptionMessage = exceptionMessage + " response.Status not success and response.Errors.Count == 0 - at DAL RegisterTravelers(MOBRegisterTravelersRequest request)";
                        }
                        throw new MOBUnitedException(exceptionMessage);
                    }
                }
            }
            else
            {
                string exceptionMessage = _configuration.GetValue<string>("UnableToRegisterTravelerErrorMessage");
                if (_configuration.GetValue<bool>("ReturnActualExceptionMessageBackForTesting"))
                {
                    exceptionMessage = exceptionMessage + " - due to jsonResponse is empty at DAL RegisterTravelers(MOBRegisterTravelersRequest request)";
                }
                throw new MOBUnitedException(exceptionMessage);
            }
            return await System.Threading.Tasks.Task.FromResult(sortedCMSContentMessages);
        }

        public async Task<string> GETCSLCMSContent(MobileCMSContentRequest request, bool isTravelAdvisory = false)
        {
            #region
            if (request == null)
            {
                throw new MOBUnitedException("GetMobileCMSContents request cannot be null.");
            }
            MOBCSLContentMessagesRequest cslContentReqeust = BuildCSLContentMessageRequest(request, isTravelAdvisory);
            string jsonResponse = await _cMSContentService.GetMobileCMSContentDetail(token: request.Token, JsonConvert.SerializeObject(cslContentReqeust), request.SessionId);
            #endregion
            return jsonResponse;
        }

        private MOBCSLContentMessagesRequest BuildCSLContentMessageRequest(MobileCMSContentRequest request, bool istravelAdvisory = false)
        {
            MOBCSLContentMessagesRequest cslContentReqeust = new MOBCSLContentMessagesRequest();
            if (request != null)
            {
                cslContentReqeust.Lang = "en";
                cslContentReqeust.Pos = "us";
                cslContentReqeust.Channel = "mobileapp";
                cslContentReqeust.Listname = new List<string>();
                foreach (string strItem in request.ListNames)
                {
                    cslContentReqeust.Listname.Add(strItem);
                }
                if (_configuration.GetValue<string>("CheckCMSContentsLocationCodes").ToUpper().Trim().Split('|').ToList().Contains(request.GroupName.ToUpper().Trim()))
                {
                    cslContentReqeust.LocationCodes = new List<string>();
                    cslContentReqeust.LocationCodes.Add(request.GroupName);
                }
                else
                {
                    cslContentReqeust.Groupname = request.GroupName;
                }
                if (!_configuration.GetValue<bool>("DonotUsecache4CSMContents"))
                {
                    if (!istravelAdvisory)
                        cslContentReqeust.Usecache = true;
                }
            }
            return cslContentReqeust;
        }

        private bool IsIBE(Reservation persistedReservation)
        {
            if (persistedReservation != null)
            {
                if (_configuration.GetValue<bool>("EnableIBE") && (persistedReservation.ShopReservationInfo2 != null))
                {
                    return persistedReservation.ShopReservationInfo2.IsIBE;
                }
            }
            return false;
        }

        private bool IsIbe(Collection<Service.Presentation.CommonModel.Characteristic> characteristics)
        {
            return _configuration.GetValue<bool>("EnableIBE") &&
                    characteristics != null &&
                    characteristics.Any(c => c != null &&
                                            "PRODUCTCODE".Equals(c.Code, StringComparison.OrdinalIgnoreCase) &&
                                            "IBE".Equals(c.Value, StringComparison.OrdinalIgnoreCase));
        }

        #endregion


        #region TermsAndConditions

        public async Task<List<MobileCMSContentMessages>> GetMobileTermsAndConditions(MobileCMSContentRequest request)
        {
            List<MobileCMSContentMessages> cmsContentMessages = new List<MobileCMSContentMessages>();
            Reservation bookingPathReservation = new Reservation();
            bool hasAsaSeats = false;
            bool hasPrefferedSeats = false;
            bool isTermsAndConditionsScreen = false;
            #region
            try
            {
                bookingPathReservation = await _sessionHelperService.GetSession<Reservation>(_headers.ContextValues.SessionId, new Reservation().ObjectName,new List<string> { _headers.ContextValues.SessionId, new Reservation().ObjectName });
            }
            catch (System.Exception ex) { throw new MOBUnitedException("Could not find your booking session."); }
            if (request.ListNames[0].ToString().ToUpper().Trim() == "RTI_TnC".ToUpper().Trim())
            {
                isTermsAndConditionsScreen = true;
                #region
                //'Premier_Access_TnC'
                //'RTI_TnC'
                //'FareLock_TnC'
                if (bookingPathReservation.IsReshopChange)
                {
                    #region reshop change
                    request.ListNames = LegalDocumentsForReshopChange(bookingPathReservation);
                    #endregion
                }
                else
                {
                    request.ListNames = new List<string>();
                    if (bookingPathReservation.AwardTravel)
                    {
                        if (bookingPathReservation.ISInternational)
                        {
                            request.ListNames.Add("RTI_Award_Itnl_TnC");
                        }
                        else
                        {
                            request.ListNames.Add("RTI_Award_Domestic_TnC");
                        }
                    }
                    else if (!bookingPathReservation.IsRefundable)
                    {
                        if (bookingPathReservation.ISInternational && bookingPathReservation.IsELF)
                        {
                            request.ListNames.Add(bookingPathReservation.IsSSA ? "RTI_Non_Refundable_Itnl_ELF_SSA_TnC"
                                                                               : "RTI_Non_Refundable_Itnl_ELF_TnC");
                        }
                        else if (bookingPathReservation.IsELF)
                        {
                            request.ListNames.Add(bookingPathReservation.IsSSA ? "RTI_Non_Refundable_ELF_SSA_TnC"
                                                                               : "RTI_Non_Refundable_ELF_TnC");
                        }
                        else if (bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.IsIBELite)
                        {
                            request.ListNames.Add(bookingPathReservation.ISInternational ? "RTI_Non_Refundable_Itnl_BELite_TnC"
                                                                                         : "RTI_Non_Refundable_BELite_TnC");
                        }
                        else if (_configuration.GetValue<bool>("EnableIBE") && bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.IsIBE)
                        {
                            if (_configuration.GetValue<bool>("EnablePBE"))
                            {
                                string productCode = bookingPathReservation?.Trips[0]?.FlattenedFlights?[0]?.Flights?[0].ShoppingProducts.First(p => p != null && p.IsIBE).ProductCode;
                                request.ListNames.Add(productCode + "_RTI_Non_Refundable_TnC");
                            }
                            else
                            {
                                request.ListNames.Add("RTI_Non_Refundable_IBE_TnC");
                            }
                        }
                        else if (_configuration.GetValue<bool>("EnableNonRefundableNonChangable") && bookingPathReservation.ShopReservationInfo2.IsNonRefundableNonChangable)
                        {
                            request.ListNames.Add("RTI_Non_Refundable_Non_Changable_Itnl_TnC");
                        }
                        else if (bookingPathReservation.ISInternational)
                        {
                            request.ListNames.Add("RTI_Non_Refundable_Itnl_TnC");
                        }
                        else
                        {
                            request.ListNames.Add("RTI_Non_Refundable_Domestic_TnC");
                        }
                    }
                    else if (bookingPathReservation.ISFlexibleSegmentExist)
                    {
                        if (bookingPathReservation.ISInternational)
                        {
                            request.ListNames.Add("RTI_Flexible_Itnl_TnC");
                        }
                        else
                        {
                            request.ListNames.Add("RTI_Flexible_Domestic_TnC");
                        }
                    }
                    else if (bookingPathReservation.IsRefundable)
                    {
                        if (_configuration.GetValue<bool>("AddMissingTnCForBE") && bookingPathReservation.IsELF)
                        {
                            request.ListNames.Add("RTI_Refundable_ELF_TnC");
                        }
                        else if (_configuration.GetValue<bool>("AddMissingTnCForBE") && bookingPathReservation.ShopReservationInfo2 != null && bookingPathReservation.ShopReservationInfo2.IsIBE)
                        {
                            string productCode = bookingPathReservation?.Trips[0]?.FlattenedFlights?[0]?.Flights?[0].ShoppingProducts.First(p => p != null && p.IsIBE).ProductCode;
                            request.ListNames.Add(productCode + "_RTI_Refundable_TnC");
                        }
                        else if (bookingPathReservation.ISInternational)
                        {
                            request.ListNames = new List<string>();
                            request.ListNames.Add("RTI_Refundable_Itnl_TnC");
                        }
                        else
                        {
                            request.ListNames = new List<string>();
                            request.ListNames.Add("RTI_Refundable_Domestic_TnC");
                        }
                    }
                    #endregion
                }

                hasAsaSeats = bookingPathReservation.Prices != null &&
                              bookingPathReservation.Prices.Exists(x => x.DisplayType != null &&
                                                                        x.DisplayType.ToUpper().Equals("ADVANCE SEAT ASSIGNMENT"));

                var preferredSeatText = _configuration.GetValue<string>("PreferedSeat_PriceBreakdownTitle") ?? string.Empty;
                hasPrefferedSeats = bookingPathReservation.Prices != null &&
                                    bookingPathReservation.Prices.Exists(x => x.DisplayType != null &&
                                                                              x.DisplayType.ToUpper().Equals(preferredSeatText.ToUpper()));
            }
            else if (request.ListNames[0].ToString().ToUpper().Trim() == "FareLock_TnC".ToUpper().Trim())
            {
                #region
                //'Premier_Access_TnC'
                //'RTI_TnC'
                //'FareLock_TnC'
                if (bookingPathReservation.AwardTravel)
                {
                    if (bookingPathReservation.ISInternational)
                    {
                        request.ListNames = new List<string>();
                        request.ListNames.Add("FareLock_Award_Itnl_TnC");
                    }
                    else
                    {
                        request.ListNames = new List<string>();
                        request.ListNames.Add("FareLock_Award_Domestic_TnC");
                    }
                }
                else if (!bookingPathReservation.IsRefundable)
                {
                    if (bookingPathReservation.ISInternational)
                    {
                        request.ListNames = new List<string>();
                        request.ListNames.Add("FareLock_Non_Refundable_Itnl_TnC");
                    }
                    else
                    {
                        request.ListNames = new List<string>();
                        request.ListNames.Add("FareLock_Non_Refundable_Domestic_TnC");
                    }
                }
                else if (bookingPathReservation.ISFlexibleSegmentExist)
                {
                    if (bookingPathReservation.ISInternational)
                    {
                        request.ListNames = new List<string>();
                        request.ListNames.Add("FareLock_Flexible_Itnl_TnC");
                    }
                    else
                    {
                        request.ListNames = new List<string>();
                        request.ListNames.Add("FareLock_Flexible_Domestic_TnC");
                    }
                }
                else if (bookingPathReservation.IsRefundable)
                {
                    if (bookingPathReservation.ISInternational)
                    {
                        request.ListNames = new List<string>();
                        request.ListNames.Add("FareLock_Refundable_Itnl_TnC");
                    }
                    else
                    {
                        request.ListNames = new List<string>();
                        request.ListNames.Add("FareLock_Refundable_Domestic_TnC");
                    }
                }
                #endregion
            }
            else if (request.ListNames[0].ToString().ToUpper().Trim() == "Premier_Access_TnC".ToUpper().Trim())
            {
                #region
                //'Premier_Access_TnC'
                //'RTI_TnC'
                //'FareLock_TnC'
                if (bookingPathReservation.AwardTravel)
                {
                    if (bookingPathReservation.ISInternational)
                    {
                        request.ListNames = new List<string>();
                        request.ListNames.Add("Premier_Access_Award_Itnl_TnC");
                    }
                    else
                    {
                        request.ListNames = new List<string>();
                        request.ListNames.Add("Premier_Access_Award_Domestic_TnC");
                    }
                }
                else
                {
                    if (bookingPathReservation.ISInternational)
                    {
                        request.ListNames = new List<string>();
                        request.ListNames.Add("Premier_Access_Itnl_TnC");
                    }
                    else
                    {
                        request.ListNames = new List<string>();
                        request.ListNames.Add("Premier_Access_Domestic_TnC");
                    }
                }
                #endregion
            }
            else if (request.ListNames[0].ToString().ToUpper().Trim() == "24HOUR_TnC".ToUpper().Trim())
            {
                request.ListNames = new List<string>();
                request.ListNames.Add("RTI_24HOUR_TnC");
            }
            #endregion

            var titles = request.ListNames;
            StringBuilder stringBuilder = new StringBuilder();
            
            foreach (var title in titles)
            {
                stringBuilder.Append("'");
                stringBuilder.Append(title);
                stringBuilder.Append("'");
                stringBuilder.Append(",");
            }

            string reqTitles = stringBuilder.ToString().Trim(',');

            List<MOBLegalDocument> docs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(reqTitles, _headers.ContextValues.SessionId,true).ConfigureAwait(false);// GetNewLegalDocumentsForTitles(request.ListNames, true);

            #region adding Ancillary Terms and conditions
            if (hasAsaSeats && docs != null && docs.Count > 0)
            {
                stringBuilder = new StringBuilder();
                var keyAncillaryTnC = new List<string> { "RTI_UnitedTravelOptions_SSA_TnC" };
                titles = keyAncillaryTnC;     
                foreach (var title in titles)
                {
                    stringBuilder.Append("'");
                    stringBuilder.Append(title);
                    stringBuilder.Append("'");
                    stringBuilder.Append(",");
                }
                reqTitles = stringBuilder.ToString().Trim(',');
                var ancillaryTnCs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(reqTitles, _headers.ContextValues.SessionId, true);
                if (ancillaryTnCs != null && ancillaryTnCs.Count > 0)
                    docs.AddRange(ancillaryTnCs);
            }

            if (hasPrefferedSeats && docs != null && docs.Count > 0)
            {
                stringBuilder = new StringBuilder();
                var keyAncillaryTnC = new List<string> { "RTI_UnitedTravelOptions_PZA_TnC" };
                titles = keyAncillaryTnC;
                foreach (var title in titles)
                {
                    stringBuilder.Append("'");
                    stringBuilder.Append(title);
                    stringBuilder.Append("'");
                    stringBuilder.Append(",");
                }
                reqTitles = stringBuilder.ToString().Trim(',');
                var ancillaryTnCs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(reqTitles, _headers.ContextValues.SessionId, true);
                if (ancillaryTnCs != null && ancillaryTnCs.Count > 0)
                    docs.AddRange(ancillaryTnCs);
            }
            if (_configuration.GetValue<bool>("EnableAirlinesFareComparison"))
            {
                stringBuilder = new StringBuilder();
                var airline = bookingPathReservation.Trips?.SelectMany(t => t.FlattenedFlights)?.SelectMany(t2 => t2.Flights).FirstOrDefault(t1 => _configuration.GetValue<string>("SupportedAirlinesTnC").Contains(t1.OperatingCarrier))?.OperatingCarrier;
                if (!string.IsNullOrEmpty(airline))
                {
                    var keyAncillaryTnC = new List<string> { "RTI_TnC_" + airline };
                    titles = keyAncillaryTnC;
                    foreach (var title in titles)
                    {
                        stringBuilder.Append("'");
                        stringBuilder.Append(title);
                        stringBuilder.Append("'");
                        stringBuilder.Append(",");
                    }
                    reqTitles = stringBuilder.ToString().Trim(',');
                    var ancillaryTnCs = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(reqTitles, _headers.ContextValues.SessionId, true);
                    if (ancillaryTnCs != null && ancillaryTnCs.Count > 0) 
                    {
                        if (_configuration.GetValue<bool>("SwitchtoDynamoDb")) 
                        {
                            var newTitle = string.Format("<ul><li>{0}</li></ul>", ancillaryTnCs[0].LegalDocument);
                            var tag = "</ul>";
                            var position = docs[0].LegalDocument.IndexOf(tag) + tag.Length;
                            docs[0].LegalDocument = docs[0].LegalDocument.Insert(position, newTitle);
                        }
                        else 
                        {
                            docs.InsertRange(1, ancillaryTnCs);
                        }
                    }
                }
            }
            #endregion

            foreach (MOBLegalDocument doc in docs)
            {
                MobileCMSContentMessages cmsContentMessage = new MobileCMSContentMessages();
                if(!GeneralHelper.IsApplicationVersionGreaterorEqual(request.Application.Id, request.Application.Version.Major, _configuration.GetValue<string>("AndroidTermsAndConditionsFixVersion"), _configuration.GetValue<string>("iPhoneTermsAndConditionsFixVersion")) 
                    && _configuration.GetValue<bool>("SwitchToDynamoDB") && request.Application.Id == 2 && isTermsAndConditionsScreen)
                { 
                    cmsContentMessage.ContentFull = new Regex("</li></ul>").Replace((new Regex("<ul><li>")).Replace(doc.LegalDocument, "", 1),"",1);
                }
                else
                {
                    cmsContentMessage.ContentFull = doc.LegalDocument;
                }
                cmsContentMessage.Title = doc.Title;
                cmsContentMessages.Add(cmsContentMessage);
            }
            return cmsContentMessages;
        }
      
        private List<string> LegalDocumentsForReshopChange(Reservation bookingPathReservation)
        {
            var listNames = new List<string>();
            if (bookingPathReservation.HasJSXSegment)
            {
                listNames.Add("RTI_Refundable_JSX_TnC_Reshop");
            }
            if (!bookingPathReservation.IsRefundable)
            {
                listNames.Add(bookingPathReservation.ISInternational
                    ? "RTI_Non_Refundable_Itnl_TnC_Reshop"
                    : "RTI_Non_Refundable_Domestic_TnC_Reshop");
            }
            else if (bookingPathReservation.ISFlexibleSegmentExist)
            {
                listNames.Add(bookingPathReservation.ISInternational
                    ? "RTI_Flexible_Itnl_TnC_Reshop"
                    : "RTI_Flexible_Domestic_TnC_Reshop");
            }
            else if (bookingPathReservation.IsRefundable)
            {
                listNames.Add(bookingPathReservation.ISInternational
                    ? "RTI_Refundable_Itnl_TnC_Reshop"
                    : "RTI_Refundable_Domestic_TnC_Reshop");
            }

            return listNames;
        }
        #endregion
    }
}
