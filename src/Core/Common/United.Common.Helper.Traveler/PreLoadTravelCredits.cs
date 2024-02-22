using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.Common.Helper.Shopping;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.Shopping.FormofPayment;
using United.Service.Presentation.CommonModel;
using United.Service.Presentation.PaymentRequestModel;
using United.Service.Presentation.PaymentResponseModel;
using United.Service.Presentation.ReferenceDataModel;
using United.Services.FlightShopping.Common.FlightReservation;
using United.Utility.AppVersion;
using United.Utility.Helper;
using MOBFOPTravelCredit = United.Mobile.Model.Shopping.FormofPayment.MOBFOPTravelCredit;
using MOBFOPTravelCreditDetails = United.Mobile.Model.Shopping.FormofPayment.MOBFOPTravelCreditDetails;

namespace United.Common.Helper.Traveler
{
    public class PreLoadTravelCredits
    {
        private readonly ICacheLog _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly ITravelerUtility _travelerUtility;
        private readonly IFFCShoppingcs _fFCShoppingcs;
        private readonly IPaymentService _paymentService;
        private readonly IFeatureSettings _featureSettings;
        public PreLoadTravelCredits(ICacheLog logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , ITravelerUtility travelerUtility
            , IPaymentService paymentService
            , IFFCShoppingcs fFCShoppingcs
            , IFeatureSettings featureSettings)
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _travelerUtility = travelerUtility;
            _paymentService = paymentService;
            _fFCShoppingcs = fFCShoppingcs;
            _featureSettings = featureSettings;
        }

        public async System.Threading.Tasks.Task PreLoadTravelCredit(string sessionId, MOBShoppingCart shoppingCart, MOBRequest request, bool isLoadFromCSL = false, FlightReservationResponse flightReservationResponse = null, bool isCorporateBusinessNamePersonalized = false)
        {
            try
            {
                Session session = await _sessionHelperService.GetSession<Session>(sessionId, new Session().ObjectName, new List<string> { sessionId, new Session().ObjectName }).ConfigureAwait(false);
                var bookingPathReservation = new Mobile.Model.Shopping.Reservation();
                var tupleRespinse =  await _travelerUtility.LoadBasicFOPResponse(session, bookingPathReservation);
                var response = tupleRespinse.Item1;
                bookingPathReservation = tupleRespinse.bookingPathReservation;

                if (sessionId == null)
                {
                    throw new Exception("empty session");
                }
                if (response?.ShoppingCart?.FormofPaymentDetails == null)
                {
                    response.ShoppingCart.FormofPaymentDetails = new MOBFormofPaymentDetails();
                }

                var travelCreditDetails = new MOBFOPTravelCreditDetails();

                List<CMSContentMessage> lstMessages = await _fFCShoppingcs.GetSDLContentByGroupName(request, sessionId, session.Token, _configuration.GetValue<string>("CMSContentMessages_GroupName_BookingRTI_Messages"), "BookingPathRTI_CMSContentMessagesCached_StaticGUID");
                travelCreditDetails.LookUpMessages = GetSDLContentMessages(lstMessages, "RTI.TravelCertificate.LookUpTravelCredits");
                TCLookupByFreqFlyerNumWithEligibleResponse cslres = new TCLookupByFreqFlyerNumWithEligibleResponse();
                cslres = await GetCSLTravelCredits(sessionId, request, response, isLoadFromCSL, flightReservationResponse).ConfigureAwait(false);
                travelCreditDetails.ReviewMessages = GetSDLContentMessages(lstMessages, "RTI.TravelCertificate.ReviewTravelCredits");
                travelCreditDetails.ReviewMessages.AddRange(GetSDLContentMessages(lstMessages, "RTI.TravelCertificate.AlertTravelCredits"));
                SwapTitleAndLocation(travelCreditDetails.ReviewMessages);
                SwapTitleAndLocation(travelCreditDetails.LookUpMessages);
                travelCreditDetails.ReviewMessages.AddRange(travelCreditDetails.LookUpMessages);
                travelCreditDetails.TravelCredits = LoadCSLResponse(cslres, response, travelCreditDetails.LookUpMessages);
                if (response.ShoppingCart.FormofPaymentDetails.TravelCreditDetails?.TravelCredits?.Count > 0)
                {
                    travelCreditDetails.TravelCredits.ForEach(tc => tc.IsApplied = (response.ShoppingCart.FormofPaymentDetails.TravelCreditDetails.TravelCredits.Exists(existingTC => existingTC.IsApplied && existingTC.PinCode == tc.PinCode)));
                }
                travelCreditDetails.TravelCredits = travelCreditDetails.TravelCredits.OrderBy(x => DateTime.Parse(x.ExpiryDate)).ToList();
                var nameWaiverMatchMessage = ShopStaticUtility.GetSDLMessageFromList(lstMessages, "RTI.TravelCertificate.LookUpTravelCredits.Alert.NameMatchWaiver");
                travelCreditDetails.NameWaiverMatchMessage = nameWaiverMatchMessage.Count() > 0 ? nameWaiverMatchMessage?[0].ContentFull.ToString() : null;

                travelCreditDetails.TravelCreditSummary = string.Empty;

                if (_configuration.GetValue<bool>("EnableU4BCorporateName"))
                {
                    ShoppingResponse shop = new ShoppingResponse();
                    shop = await _sessionHelperService.GetSession<ShoppingResponse>(sessionId, shop.ObjectName, new List<string> { sessionId, shop.ObjectName }).ConfigureAwait(false);
                    if (await EnableCorporateNameForOlderClients(shop).ConfigureAwait(false))
                    {
                        if (travelCreditDetails.TravelCredits.Any())
                        {
                            foreach (var travelCredit in travelCreditDetails.TravelCredits)
                            {
                                if (!string.IsNullOrEmpty(travelCredit.CorporateName))
                                    travelCredit.CorporateName = _configuration.GetValue<string>("CorporateNameForOlderClients");
                            }
                        }
                    }

                    travelCreditDetails.CorporateName = shop != null ? shop.Request?.MOBCPCorporateDetails?.CorporateCompanyName : "";
                }

                if (_configuration.GetValue<bool>("EnableTravelCreditSummary"))
                {
                    var travelCreditCount = travelCreditDetails?.TravelCredits?.Count ?? 0;

                    if (travelCreditCount > 0)
                    {
                        var travelCreditSummary = _configuration.GetValue<string>("TravelCreditSummary");
                        var pluralChar = travelCreditCount > 1 ? "s" : string.Empty;

                        travelCreditDetails.TravelCreditSummary = response.ShoppingCart?.Products?.FirstOrDefault().Code != "FLK" ? string.Format(travelCreditSummary, travelCreditCount, pluralChar) : string.Empty;
                    }
                }

                shoppingCart.FormofPaymentDetails.TravelCreditDetails = travelCreditDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError("PreLoadTravelCredits {@Exception}", JsonConvert.SerializeObject(ex));
            }
        }

        private async Task<bool> EnableCorporateNameForOlderClients(ShoppingResponse shopResponse)
        {
            if (shopResponse != null && shopResponse.Request?.Application != null)
                return await _featureSettings.GetFeatureSettingValue("EnableCorporateTravelCreditToggle").ConfigureAwait(false)
                       && !GeneralHelper.IsApplicationVersionGreaterorEqual(shopResponse.Request.Application.Id, shopResponse.Request.Application.Version.Major, _configuration.GetValue<string>("Android_IsEnableSuppressingCompanyNameForBusiness_AppVersion"), _configuration.GetValue<string>("IPhone_IsEnableSuppressingCompanyNameForBusiness_AppVersion"));

            return false;
        }

        public async Task<TCLookupByFreqFlyerNumWithEligibleResponse> GetCSLTravelCredits(string sessionId, MOBRequest mobRequest, FOPResponse response, bool isLoadFromCSL, FlightReservationResponse flightReservationResponse = null)
        {
            TCLookupByFreqFlyerNumWithEligibleResponse lookupResponse = new TCLookupByFreqFlyerNumWithEligibleResponse();
            string url = "/ECD/TCLookupByFreqFlyerNumWithEligible";
            TCLookupByFreqFlyerNumWithEligibleRequest cslRequest = new TCLookupByFreqFlyerNumWithEligibleRequest();
            cslRequest.FreqFlyerNum = response?.Profiles?[0].Travelers.Find(item => item.IsProfileOwner).MileagePlus.MileagePlusId;
            if (String.IsNullOrEmpty(cslRequest.FreqFlyerNum))
                cslRequest.FreqFlyerNum = response.Reservation?.TravelersCSL?.FirstOrDefault(v => v.IsProfileOwner)?.MileagePlus?.MileagePlusId;
            // In the guest flow we will be able to show the travel credits based on the lastname/DOB 
            if (!_configuration.GetValue<bool>("EnablePreLoadForTCNonMember"))
            {
                if (String.IsNullOrEmpty(cslRequest.FreqFlyerNum))
                {
                    return lookupResponse;
                }
            }
            else
            {
                cslRequest.IsLoadFFCRWithCustomSearch = true;
                cslRequest.IsLoadFFCWithCustomSearch = true;
            }

            cslRequest.IsLoadETC = true;
            cslRequest.IsLoadFFC = true;
            cslRequest.IsLoadFFCR = true;

            Session session = await _sessionHelperService.GetSession<Session>(sessionId, new Session().ObjectName, new List<string> { sessionId, new Session().ObjectName }).ConfigureAwait(false);
            var reservation = new Service.Presentation.ReservationModel.Reservation();
            reservation = await _sessionHelperService.GetSession<Service.Presentation.ReservationModel.Reservation>(sessionId, reservation.GetType().FullName, new List<string> { sessionId, reservation.GetType().FullName }).ConfigureAwait(false);
            if (reservation == null || (!_configuration.GetValue<bool>("DisableMMOptionsReloadInBackButtonFixToggle") && isLoadFromCSL))
            {
                if (flightReservationResponse == null)
                {
                    var cartInfo = await _travelerUtility.GetCartInformation(sessionId, mobRequest.Application, mobRequest.DeviceId, session.CartId, session.Token);
                    reservation = cartInfo.Reservation;
                }
                else
                {
                    reservation = flightReservationResponse.Reservation;
                }
                await _sessionHelperService.SaveSession<Service.Presentation.ReservationModel.Reservation>(reservation, session.SessionId, new List<string> { session.SessionId, reservation.GetType().FullName }, reservation.GetType().FullName).ConfigureAwait(false);
            }
            cslRequest.Reservation = reservation;
            cslRequest.CallingService = new ServiceClient();
            cslRequest.CallingService.Requestor = new Requestor();
            cslRequest.CallingService.AccessCode = _configuration.GetValue<string>("TravelCreditAccessCode").ToString();
            cslRequest.CallingService.Requestor.AgentAAA = "HQS";
            cslRequest.CallingService.Requestor.AgentSine = "UA";
            cslRequest.CallingService.Requestor.ApplicationSource = "Mobile";
            cslRequest.CallingService.Requestor.Device = new Service.Presentation.CommonModel.Device();
            cslRequest.CallingService.Requestor.Device.LNIATA = "Mobile";
            cslRequest.CallingService.Requestor.DutyCode = "SU";
            string jsonRequest = JsonConvert.SerializeObject(cslRequest);
            string jsonResponse = await PostAndLog(sessionId, url, jsonRequest, mobRequest, "GetCSLTravelCredits", "TCLookupByFreqFlyerNumWithEligible");

            lookupResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<TCLookupByFreqFlyerNumWithEligibleResponse>(jsonResponse);
            await _sessionHelperService.SaveSession(lookupResponse, session.SessionId, new List<string> { session.SessionId, lookupResponse.GetType().FullName }, lookupResponse.GetType().FullName).ConfigureAwait(false);
            return lookupResponse;
        }

        public static List<MOBMobileCMSContentMessages> GetSDLContentMessages(List<CMSContentMessage> lstMessages, string title)
        {
            List<MOBMobileCMSContentMessages> messages = new List<MOBMobileCMSContentMessages>();
            messages.AddRange(ShopStaticUtility.GetSDLMessageFromList(lstMessages, title));
            return messages;
        }
        public static List<MOBMobileCMSContentMessages> SwapTitleAndLocation(List<MOBMobileCMSContentMessages> cmsList)
        {
            foreach (var item in cmsList)
            {
                string location = item.LocationCode;
                item.LocationCode = item.Title;
                item.Title = location;
            }
            return cmsList;
        }
        public List<MOBFOPTravelCredit> LoadCSLResponse(TCLookupByFreqFlyerNumWithEligibleResponse cslResponse, FOPResponse response, List<MOBMobileCMSContentMessages> lookUpMessages)
        {
            List<MOBFOPTravelCredit> travelCredits = new List<MOBFOPTravelCredit>();

            if (cslResponse == null)
            {
                return travelCredits;
            }
            var etc = cslResponse.ETCCertificates;
            var ffc = cslResponse.FFCCertificates;
            var ffcr = cslResponse.FFCRCertificates;

            _travelerUtility.AddETCToTC(travelCredits, etc, false);
            _travelerUtility.AddFFCandFFCR(response.Reservation.TravelersCSL, travelCredits, ffc, lookUpMessages, true, false);
            _travelerUtility.AddFFCandFFCR(response.Reservation.TravelersCSL, travelCredits, ffcr, lookUpMessages, false, false);
            return travelCredits;
        }

        public async Task<string> PostAndLog(string sessionId, string path, string jsonRequest, MOBRequest mobRequest, string logAction, string cslAction)
        {
            cslAction = cslAction ?? string.Empty;
            logAction = logAction ?? string.Empty;
            logAction = cslAction + " - " + logAction;
            var session = new Session();
            session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName }).ConfigureAwait(false);
            var token = session.Token;

            var jsonResponse = await _paymentService.GetEligibleFormOfPayments(token, path, jsonRequest, sessionId).ConfigureAwait(false);

            if (string.IsNullOrEmpty(jsonResponse))
                throw new Exception("Service did not return any reponse");

            return jsonResponse;
        }

    }
}