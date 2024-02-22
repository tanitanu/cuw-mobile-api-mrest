using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using United.Common.Helper;
using United.Common.Helper.ManageRes;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.MPRewards;
using United.Service.Presentation.ReservationResponseModel;
using United.Utility.Helper;

namespace United.Mobile.ScheduleChange.Domain
{
    public class RecordLocatorBusiness : IRecordLocatorBusiness
    {
        private readonly ICacheLog<RecordLocatorBusiness> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHeaders _headers;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IManageReservation _manageReservation;
        private readonly ManageResUtility _manageResUtility;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;

        public RecordLocatorBusiness(ICacheLog<RecordLocatorBusiness> logger
            , IConfiguration configuration
            , IHeaders headers
            , IShoppingSessionHelper shoppingSessionHelper
            , ISessionHelperService sessionHelperService
            , IManageReservation manageReservation
            , IDynamoDBService dynamoDBService
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService)
        {
            _logger = logger;
            _configuration = configuration;
            _headers = headers;
            _shoppingSessionHelper = shoppingSessionHelper;
            _sessionHelperService = sessionHelperService;
            _manageReservation = manageReservation;
            _dynamoDBService = dynamoDBService;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _manageResUtility = new ManageResUtility(_configuration, _legalDocumentsForTitlesService, _dynamoDBService, _headers, _logger);
        }

        public async Task<MOBPNRByRecordLocatorResponse> GetPNRByRecordLocator(MOBPNRByRecordLocatorRequest request)
        {
            MOBPNRByRecordLocatorResponse response = new MOBPNRByRecordLocatorResponse();

            try
            {
                _logger.LogInformation("GetPNRByRecordLocator {Request}, {RecordLocator} and {SessionId}", request, request.RecordLocator, request.SessionId);

                Session session = null;
                if (!string.IsNullOrEmpty(request.SessionId))
                {
                    session = await _shoppingSessionHelper.GetValidateSession(request.SessionId, false, true).ConfigureAwait(false);
                    session.Flow = request.Flow;
                }
                else
                {
                    session = await _shoppingSessionHelper.CreateShoppingSession(request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId, request.MileagePlusNumber, string.Empty, false, true).ConfigureAwait(false);
                    session.Flow = request.Flow;
                }

                if (string.IsNullOrEmpty(request.SessionId))
                    request.SessionId = session.SessionId;

                response =await _manageReservation.GetPNRByRecordLocatorCommonMethod(request).ConfigureAwait(false);
                response.Flow = request.Flow;
                response.SessionId = session.SessionId;
                var cslReservation = await _sessionHelperService.GetSession<ReservationDetail>(request.SessionId, (new ReservationDetail()).GetType().FullName, new List<string>() { request.SessionId, (new ReservationDetail()).GetType().FullName }).ConfigureAwait(false);
                if (_configuration.GetValue<bool>("joinOneClickMileagePlusEnabled") && cslReservation != null && cslReservation.Detail != null)
                {
                    response.ShowJoinOneClickEnrollment = ValidateUserEnrollementEligibility(response, cslReservation.Detail.Travelers, cslReservation.Detail.Prices, session);
                }

                if (_configuration.GetValue<bool>("countDownWidgetEnabled"))
                {
                    response.CountDownWidgetInfo = GetCountDownWidgetInfoConfigValues(request.Application.Id);
                }

                if (response.PNR != null)
                {
                    if (response.PNR.HasScheduleChanged && !response.PNR.ConsolidateScheduleChangeMessage)
                    {
                        response.PNR.HasScheduleChanged = _manageResUtility.GetHasScheduledChanged(response.PNR.Segments);
                        response.PNR.StatusMessageItems = await SetScheduledChangeMessage(response.PNR.HasScheduleChanged);
                        SetupRedirectURL(request.RecordLocator, request.LastName, response.PNR.URLItems, "PNRURL");
                    }

                    if (response.PNR.AdvisoryInfo != null && response.PNR.AdvisoryInfo.Any())
                    {
                        var scheduleChange
                            = response.PNR.AdvisoryInfo.Where(x => x.ContentType == ContentType.SCHEDULECHANGE).FirstOrDefault();
                        if (scheduleChange != null)
                        {
                            scheduleChange.Buttonlink =
                                (_configuration.GetValue<bool>("EnableTripDetailScheduleChangeRedirect3dot0Url"))
                                ? GetTripDetailRedirect3dot0Url
                                (request.RecordLocator, request.LastName, ac: "VI", channel: "mobile", languagecode: "en/US")
                                : GetPNRRedirectUrl(request.RecordLocator, request.LastName, reqType: "VI");
                        }
                    }
                }

                //Covid-19 Emergency WHO TPI content
                if (_configuration.GetValue<bool>("ToggleCovidEmergencytextTPI") == true)
                {
                    bool return_TPICOVID_19WHOMessage_For_BackwardBuilds = GeneralHelper.IsApplicationVersionGreater2(request.Application.Id, request.Application.Version.Major, "Android_Return_TPICOVID_19WHOMessage__For_BackwardBuilds", "iPhone_Return_TPICOVID_19WHOMessage_For_BackwardBuilds", null, null, _configuration);
                    if (!return_TPICOVID_19WHOMessage_For_BackwardBuilds && response.TripInsuranceInfo != null
                        && response.TripInsuranceInfo.tpiAIGReturnedMessageContentList != null
                        && response.TripInsuranceInfo.tpiAIGReturnedMessageContentList.Count > 0)
                    {
                        MOBItem tpiCOVID19EmergencyAlert = response.TripInsuranceInfo.tpiAIGReturnedMessageContentList.Find(p => p.Id.ToUpper().Trim() == "COVID19EmergencyAlertManageRes".ToUpper().Trim());
                        if (tpiCOVID19EmergencyAlert != null)
                        {
                            response.TripInsuranceInfo.Body3 = response.TripInsuranceInfo.Body3 +
                                "<br><br>" + tpiCOVID19EmergencyAlert.CurrentValue;
                        }
                    }
                }
            }
            catch (MOBUnitedException uaex)
            {
                _logger.LogWarning("GetPNRByRecordLocator - MOBUnitedException : {Exception}, {stackTrace}, {RecordLocator} and {SessionId}", uaex, uaex.StackTrace, request.RecordLocator, request.SessionId);
            }
            catch (System.Exception ex)
            {

                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                _logger.LogWarning("GetPNRByRecordLocator - Exception : {Exception}, {stackTrace}, {RecordLocator} and {SessionId}", ex, ex.StackTrace, request.RecordLocator, request.SessionId);
                response.Exception = new MOBException("9999", "We are unable to retrieve the latest information for this itinerary.");
            }

            if (response.Exception != null)
            {
                //ALM# 27193 - Changed the Message as petr the ALM comments by Hueramo, Carla 
                string exMessage = response.Exception.Message;
                //if (exMessage.Trim().IndexOf("The confirmation number entered is invalid.") > -1 || exMessage.Trim().IndexOf("Please enter a valid record locator.") > -1)
                if (exMessage.Trim().IndexOf("The confirmation number entered is invalid.") > -1 || exMessage.Trim().IndexOf("Please enter a valid record locator.") > -1 || exMessage.Trim().IndexOf("The last name you entered, does not match the name we have on file.") > -1 || exMessage.Trim().IndexOf("Please enter a valid last name") > -1)
                {
                    exMessage = _configuration.GetValue<string>("ExceptionMessageForInvalidPNROrInvalidLastName");
                }
                string exCode = response.Exception.Code;
                response = new MOBPNRByRecordLocatorResponse();
                response.Exception = new MOBException(exCode, exMessage);

            }

            _logger.LogInformation("GetPNRByRecordLocator {Response}, {RecordLocator} and {SessionId}", response, request.RecordLocator, request.SessionId);
            return response;
        }

        private bool ValidateUserEnrollementEligibility(MOBPNRByRecordLocatorResponse response, Collection<Service.Presentation.ReservationModel.Traveler> travelers, Collection<Service.Presentation.PriceModel.Price> prices, Session session)
        {
            try
            {
                bool showEnroll = false;

                var fareType = prices != null && prices.Any(p => p.FareType == Service.Presentation.CommonEnumModel.FareType.Revenue);
                if (fareType)
                {
                    if (response.PNR.Passengers != null && !string.IsNullOrEmpty(response.PNR.NumberOfPassengers))
                    {

                        foreach (var pax in response.PNR.Passengers.Where(p => p.MileagePlus == null))
                        {
                            var validBillingAddress = BillingAddressValidation(travelers, session);
                            var validPhoneNumber = string.Empty;
                            if (pax.Contact?.PhoneNumbers != null && pax.Contact?.PhoneNumbers.Count() > 0)
                            {
                                var phoneCountryCode = pax.Contact?.PhoneNumbers?.FirstOrDefault().CountryCode;
                                var PhoneNumbers = pax?.Contact?.PhoneNumbers?.FirstOrDefault().PhoneNumber;

                                if (phoneCountryCode.ToUpper() == "US" || phoneCountryCode.ToUpper() == "CA")
                                {
                                    if (PhoneNumbers.Length.ToString() == _configuration.GetValue<string>("OnclickEnrollmentEligibilityCheckCountryCode"))
                                    {
                                        validPhoneNumber = PhoneNumbers;
                                    }
                                }
                                else
                                {
                                    validPhoneNumber = PhoneNumbers;
                                }
                            }

                            if (validBillingAddress != null && !string.IsNullOrEmpty(pax.PassengerName?.First) && !string.IsNullOrEmpty(pax.PassengerName?.Last) && !string.IsNullOrEmpty(pax.BirthDate) && !string.IsNullOrEmpty(validPhoneNumber))
                            {
                                showEnroll = true;
                                break;

                            }
                        }

                    }
                }

                return showEnroll;
            }
            catch
            {
                return false;
            }

        }

        private MOBCountDownWidgetInfo GetCountDownWidgetInfoConfigValues(int applicationId)
        {
            return new MOBCountDownWidgetInfo()
            {
                SectionTitle = !string.IsNullOrEmpty(_configuration.GetValue<string>("countDownWidgetSectionTitle")) ? _configuration.GetValue<string>("countDownWidgetSectionTitle") : string.Empty,
                SectionDescription = !string.IsNullOrEmpty(_configuration.GetValue<string>("countDownWidgetSectionDescription")) ? _configuration.GetValue<string>("countDownWidgetSectionDescription") : string.Empty,
                InstructionLinkText = !string.IsNullOrEmpty(_configuration.GetValue<string>("countDownWidgetInstructionLinkText")) ? _configuration.GetValue<string>("countDownWidgetInstructionLinkText") : string.Empty,
                InstructionPageTitle = !string.IsNullOrEmpty(_configuration.GetValue<string>("countDownWidgetInstructionPageTitle")) ? _configuration.GetValue<string>("countDownWidgetInstructionPageTitle") : string.Empty,
                InstructionPageContent = applicationId == 1 ? _configuration.GetValue<string>("countDownWidgetInstructionPageContentiOS") : _configuration.GetValue<string>("countDownWidgetInstructionPageContentAndroid")
            };
        }

        private async Task<List<MOBItem>> SetScheduledChangeMessage(bool hasScheduleChanged)
        {
            if (hasScheduleChanged)
            {
                return await _manageResUtility.GetCaptions("SCHEDULE_CHANGE_MESSAGES");
            }
            return null;
        }

        private void SetupRedirectURL(string recordLocator, string lastName, List<MOBItem> urlItems, string urlKey)
        {
            if (urlItems == null)
            {
                urlItems = new List<MOBItem>();
            }
            string urlValue = string.Empty;
            switch (urlKey)
            {
                case "PNRURL":
                    urlValue = "http://" + _configuration.GetValue<string>("DotComOneCancelURL") + "/web/en-US/apps/reservation/import.aspx?OP=1&CN=" +
                    recordLocator +
                    "&LN=" +
                    lastName +
                    "&T=F&MobileOff=1";
                    break;
                case "EDITTRAVELER":
                    urlValue = "https://integration.united.com/web/en-US/apps/reservation/main.aspx?TY=F&AC=ED&CN=" + EncryptString(recordLocator) + "&FLN=" + EncryptString(lastName);
                    break;
            }
            if (!string.IsNullOrEmpty(urlValue))
            {
                MOBItem pnrUrl = new MOBItem();
                pnrUrl.Id = urlKey;
                pnrUrl.CurrentValue = urlValue;
                pnrUrl.SaveToPersist = true;
                urlItems.Add(pnrUrl);
            }
        }

        private string EncryptString(string data)
        {
            //TODO to find the library
            //return ECommerce.Framework.Utilities.SecureData.EncryptString(data);
            return default;
        }

        private string GetTripDetailRedirect3dot0Url
           (string cn, string ln, string ac, int timestampvalidity = 0, string channel = "mobile",
           string languagecode = "en/US", string trips = "", string travelers = "", string ddate = "",
           string guid = "", bool isAward = false)
        {
            var retUrl = string.Empty;
            //REF:{0}/{1}/manageres/tripdetails/{2}/{3}?{4}
            //{env}/{en/US}/manageres/tripdetails/{encryptedStuff}/mobile?changepath=true
            var baseUrl = _configuration.GetValue<string>("TripDetailRedirect3dot0BaseUrl");

            var urlPattern = _configuration.GetValue<string>("TripDetailRedirect3dot0UrlPattern");
            var urlPatternFSR = _configuration.GetValue<string>("ReshopFSRRedirect3dot0UrlPattern");


            DateTime timestamp
                = (timestampvalidity > 0) ? DateTime.Now.ToUniversalTime().AddMinutes(timestampvalidity) : DateTime.Now.ToUniversalTime();

            var encryptedstring = string.Empty;
            if (_configuration.GetValue<bool>("EnableRedirect3dot0UrlWithSlashRemoved"))
            {
                encryptedstring = EncryptString
                (string.Format("RecordLocator={0};LastName={1};TimeStamp={2};", cn, ln, timestamp)).Replace("/", "~~");
            }
            else
            {
                encryptedstring = EncryptString
                (string.Format("RecordLocator={0};LastName={1};TimeStamp={2};", cn, ln, timestamp));
            }

            var encodedstring = HttpUtility.UrlEncode(encryptedstring);
            string encodedpnr = HttpUtility.UrlEncode(EncryptString(cn));
            string from = "mobilecheckinsdc";

            if (string.Equals(ac, "EX", StringComparison.OrdinalIgnoreCase))
            {
                return string.Format
                    (urlPattern, baseUrl, languagecode, encodedstring, channel, "changepath=true");
            }
            else if (string.Equals(ac, "CA", StringComparison.OrdinalIgnoreCase))
            {
                return string.Format
                    (urlPattern, baseUrl, languagecode, encodedstring, channel, "cancelpath=true");
            }
            else if (string.Equals(ac, "CSDC", StringComparison.OrdinalIgnoreCase))
            {
                //&td1=01-29-2021&idx=1
                string inputdatapattern = "pnr={0}&trips={1}&travelers={2}&from={3}&guid={4}&td1={5}{6}";
                return string.Format(urlPatternFSR, baseUrl, languagecode, isAward ? "awd" : "rev",
                    string.Format(inputdatapattern, encodedpnr, trips, travelers, from, guid,
                    ddate, isAward ? string.Empty : "&TYPE=rev"));
            }
            else
            {
                return string.Format
                    (urlPattern, baseUrl, languagecode, encodedstring, channel, string.Empty).TrimEnd('?');
            }
        }

        private string GetPNRRedirectUrl(string recordLocator, string lastlName, string reqType)
        {
            string retUrl = string.Empty;

            if (string.Equals(reqType, "EX", StringComparison.OrdinalIgnoreCase))
            {
                retUrl = string.Format("https://{0}/ual/en/US/flight-search/change-a-flight/changeflight/changeflight/rev?PNR={1}&RiskFreePolicy=&TYPE=rev&source=MOBILE",
                 _configuration.GetValue<string>("DotComChangeResBaseUrl"), HttpUtility.UrlEncode(EncryptString(recordLocator)));
            }
            else
            {
                if (string.Equals(reqType, "AWARD_CA", StringComparison.OrdinalIgnoreCase))
                {
                    retUrl = string.Format("http://{0}/{1}?TY=F&CN={2}&FLN={3}&source=MOBILE",
                    _configuration.GetValue<string>("DotComOneCancelURL"),
                    _configuration.GetValue<string>("ReShopRedirectPath"),
                    EncryptString(recordLocator),
                    EncryptString(lastlName)
                   );
                }
                else
                {
                    retUrl = string.Format("http://{0}/{1}?TY=F&AC={2}&CN={3}&FLN={4}&source=MOBILE",
                    _configuration.GetValue<string>("DotComOneCancelURL"),
                    _configuration.GetValue<string>("ReShopRedirectPath"),
                    reqType,
                    EncryptString(recordLocator),
                    EncryptString(lastlName)
                   );
                }
            }
            return retUrl;
        }

        private MOBAddress BillingAddressValidation(Collection<Service.Presentation.ReservationModel.Traveler> travelers, Session session)
        {
            MOBAddress address = null;
            //start
            try
            {
                var allpaymentaddress = travelers?.FirstOrDefault()?.Tickets?.Where(x => x.Payments != null)
                    .SelectMany(x => x.Payments.Where(y => y.BillingAddress != null).Select(z => z.BillingAddress));

                if (allpaymentaddress == null || !allpaymentaddress.Any()) return null;

                var billingaddress = allpaymentaddress.LastOrDefault(x => !string.IsNullOrEmpty(x.AddressLines?.FirstOrDefault(y => !string.IsNullOrEmpty(y)))
                                      && !string.IsNullOrEmpty(x.Country.CountryCode));

                if (billingaddress == null || (_configuration.GetValue<bool>("OneClickValidateAddressEnabled") && (string.IsNullOrEmpty(billingaddress.StateProvince?.StateProvinceCode) || string.IsNullOrEmpty(billingaddress.City) || string.IsNullOrEmpty(billingaddress.PostalCode))))
                    return null;

                address = new MOBAddress
                {
                    Country = new MOBCountry { Code = billingaddress.Country.CountryCode },
                    PostalCode = billingaddress.PostalCode,
                    City = billingaddress.City
                };

                if (!string.IsNullOrEmpty(billingaddress.StateProvince?.StateProvinceCode))
                {
                    address.State = new State { Code = billingaddress.StateProvince.StateProvinceCode };
                }

                foreach (string line in billingaddress.AddressLines)
                {
                    if (string.IsNullOrEmpty(address.Line1)) address.Line1 = line;
                    else if (string.IsNullOrEmpty(address.Line2)) address.Line2 = line;
                    else if (string.IsNullOrEmpty(address.Line3)) address.Line3 = line;
                }
                return address;
            }
            catch { return null; }
        }
    }
}
