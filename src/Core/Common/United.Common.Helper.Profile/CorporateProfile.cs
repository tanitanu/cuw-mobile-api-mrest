using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using United.CorporateDirect.Models.CustomerProfile;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Shopping;
using United.Utility.Helper;

namespace United.Common.Helper.Profile
{
    public class CorporateProfile : ICorporateProfile
    {
        private readonly IConfiguration _configuration;
        private readonly ICacheLog<CorporateProfile> _logger;
        private string _deviceId = string.Empty;
        private MOBApplication _application = new MOBApplication() { Version = new MOBVersion() };
        private readonly ICustomerCorporateProfileService _customerCorporateProfileService;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IFeatureToggles _featureToggles;
        public CorporateProfile(
             ICacheLog<CorporateProfile> logger
            , IConfiguration configuration
            , ICustomerCorporateProfileService customerCorporateProfileService
            , ISessionHelperService sessionHelperService
            , IFeatureToggles featureToggles
            )
        {
            _configuration = configuration;          
            _logger = logger;
            _customerCorporateProfileService = customerCorporateProfileService;
            _sessionHelperService = sessionHelperService;
            _featureToggles = featureToggles;
        }

        public bool IsCorporateLeisureFareSelected(List<MOBSHOPTrip> trips)
        {
            string corporateFareText = _configuration.GetValue<string>("FSRLabelForCorporateLeisure") ?? string.Empty;
            if (trips != null)
            {
                return trips.Any(
                   x =>
                       x.FlattenedFlights.Any(
                           f =>
                               f.Flights.Any(
                                   fl =>
                                       fl.CorporateFareIndicator ==
                                       corporateFareText.ToString())));
            }

            return false;
        }

        public MOBCPCorporate PopulateCorporateData(United.Services.Customer.Common.Corporate corporateData, MOBApplication application = null)
        {
            MOBCPCorporate profileCorporateData = new MOBCPCorporate();
            if (corporateData != null && corporateData.IsValid)
            {
                profileCorporateData.CompanyName = corporateData.CompanyName;
                profileCorporateData.DiscountCode = corporateData.DiscountCode;
                profileCorporateData.FareGroupId = corporateData.FareGroupId;
                profileCorporateData.IsValid = corporateData.IsValid;
                profileCorporateData.VendorId = corporateData.VendorId;
                profileCorporateData.VendorName = corporateData.VendorName;
                if (IsEnableCorporateLeisureBooking(application))
                {
                    profileCorporateData.LeisureDiscountCode = corporateData.LeisureCode;
                }
                if (_configuration.GetValue<bool>("EnableIsArranger"))
                {
                    if (!string.IsNullOrEmpty(corporateData.IsArranger) && corporateData.IsArranger.ToUpper().Equals("TRUE"))
                    {
                        profileCorporateData.NoOfTravelers = string.IsNullOrEmpty(_configuration.GetValue<string>("TravelArrangerCount")) ? 1 : _configuration.GetValue<int>("TravelArrangerCount");
                        profileCorporateData.CorporateBookingType = CORPORATEBOOKINGTYPE.TravelArranger.ToString();
                    }
                }
            }
            return profileCorporateData;
        }
        public bool IsEnableCorporateLeisureBooking(MOBApplication application)
        {
            if (_configuration.GetValue<bool>("EnableCorporateLeisure"))
            {
                if (application != null && GeneralHelper.IsApplicationVersionGreater(application.Id, application.Version.Major, "CorpLiesureAndroidVersion", "CorpLiesureiOSVersion", "", "", true, _configuration))
                {
                    return true;
                }
            }
            return false;
        }
        public bool IsEnableU4BCorporateBooking(int applicationId, string appVersion)
        {
            return _configuration.GetValue<bool>("EnableU4BCorporateBooking") && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_EnableU4BCorporateBooking_AppVersion"), _configuration.GetValue<string>("IPhone_EnableU4BCorporateBooking_AppVersion"));
        }
        #region UCB Migration work 
        public async Task<MOBCPCorporate> PopulateCorporateData(MOBCPProfileRequest request)
        {
            bool isEnableU4BCorporateBooking = request != null && request.Application != null ? IsEnableU4BCorporateBooking(request.Application.Id, request.Application.Version?.Major) : false;
            string sessionId = isEnableU4BCorporateBooking ? request.DeviceId + request.MileagePlusNumber : request.SessionId;
            var _corprofileResponse = await _sessionHelperService.GetSession<United.CorporateDirect.Models.CustomerProfile.CorpProfileResponse>(sessionId, ObjectNames.CSLCorpProfileResponse, new List<string> { sessionId, ObjectNames.CSLCorpProfileResponse }).ConfigureAwait(false);
            MOBCPCorporate profileCorporateData = new MOBCPCorporate();
            if (_corprofileResponse?.Profiles != null && (_corprofileResponse.Errors == null || _corprofileResponse.Errors.Count==0))
            {
                var corporateData = _corprofileResponse.Profiles[0].CorporateData;
                if (corporateData != null && corporateData.IsValid)
                {
                    profileCorporateData.CompanyName = corporateData.CompanyName;
                    profileCorporateData.DiscountCode = corporateData.DiscountCode;
                    profileCorporateData.FareGroupId = corporateData.FareGroupId;
                    profileCorporateData.IsValid = corporateData.IsValid;
                    profileCorporateData.VendorId = corporateData.VendorId;
                    profileCorporateData.VendorName = corporateData.VendorName;
                    profileCorporateData.LeisureDiscountCode = corporateData.LeisureCode;
                        if (corporateData.IsArranger == true)
                        {
                            profileCorporateData.NoOfTravelers = string.IsNullOrEmpty(_configuration.GetValue<string>("TravelArrangerCount")) ? 1 : _configuration.GetValue<int>("TravelArrangerCount");
                            profileCorporateData.CorporateBookingType = CORPORATEBOOKINGTYPE.TravelArranger.ToString();
                        }
                    if (await _featureToggles.IsEnableU4BForMultipax(request.Application.Id, request.Application.Version?.Major, null))
                    {
                        profileCorporateData.IsMultiPaxAllowed = corporateData.IsMultiPaxAllowed;
                        profileCorporateData.UCSID = corporateData.UCSID;
                    }
                }
                return profileCorporateData;
            }
            else
            {
                throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }

        }
        public async Task MakeCorpFopServiceCall(MOBCPProfileRequest request)
        {
            CorporateProfileRequest corpProfileRequest = new CorporateDirect.Models.CustomerProfile.CorporateProfileRequest();
            corpProfileRequest.LoyaltyId = request.MileagePlusNumber;
            string jsonRequest = United.Utility.Helper.DataContextJsonSerializer.Serialize<CorporateProfileRequest>(corpProfileRequest);
            var _corprofileResponse = await _customerCorporateProfileService.GetCorporateCreditCards<United.CorporateDirect.Models.CustomerProfile.CorpFopResponse>(request.Token, jsonRequest, request.SessionId).ConfigureAwait(false);

            #region
            if (_corprofileResponse != null && (_corprofileResponse.Errors == null || _corprofileResponse.Errors.Count == 0))
            {
                await _sessionHelperService.SaveSession<United.CorporateDirect.Models.CustomerProfile.CorpFopResponse>(_corprofileResponse, request.SessionId, new List<string> { request.SessionId, ObjectNames.CSLCorFopResponse }, ObjectNames.CSLCorFopResponse);
            }
            else
            {
                throw new Exception(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
            #endregion
        }
        public async Task MakeCorpProfileServiecall(MOBCPProfileRequest request)
        {
            CorporateProfileRequest corpProfileRequest = new CorporateDirect.Models.CustomerProfile.CorporateProfileRequest();
            corpProfileRequest.LoyaltyId = request.MileagePlusNumber;
            string jsonRequest = United.Utility.Helper.DataContextJsonSerializer.Serialize<CorporateProfileRequest>(corpProfileRequest);
            var _corprofileResponse = await _customerCorporateProfileService.GetCorporateprofile<United.CorporateDirect.Models.CustomerProfile.CorpProfileResponse>(request.Token, jsonRequest, request.SessionId).ConfigureAwait(false);
            if (_corprofileResponse != null && (_corprofileResponse.Errors == null || _corprofileResponse.Errors.Count == 0))
            {
                bool isEnableU4BCorporateBooking = request != null && request.Application != null ? IsEnableU4BCorporateBooking(request.Application.Id, request.Application.Version?.Major) : false;
                string sessionId = isEnableU4BCorporateBooking ? request.DeviceId + request.MileagePlusNumber : request.SessionId;
                await _sessionHelperService.SaveSession<United.CorporateDirect.Models.CustomerProfile.CorpProfileResponse>(_corprofileResponse, sessionId, new List<string> { sessionId, ObjectNames.CSLCorpProfileResponse }, ObjectNames.CSLCorpProfileResponse);
            }
            else
            {
                _logger.LogWarning("CSL Call GetCorporateprofile Exception error ", "Logging this error as corporatedirect service failed to return the data.But not throwing error back to client");
            }

        }
        public async Task<CorpMpNumberValidationResponse> CorpMpNumberValidation(string token, string sessionId, List<string> mpNumbers, int UCSID)
        {
            CorpMpNumberValidationResponse corpMpNumberValidationResponse = new CorpMpNumberValidationResponse();
            try
            {
                CorpMpNumberValidationRequest corpMpNumberValidationRequest = new CorporateDirect.Models.CustomerProfile.CorpMpNumberValidationRequest();
                corpMpNumberValidationRequest.MpNumbers = mpNumbers;
                corpMpNumberValidationRequest.UcsId = UCSID;
                string jsonRequest = United.Utility.Helper.DataContextJsonSerializer.Serialize<CorpMpNumberValidationRequest>(corpMpNumberValidationRequest);
                corpMpNumberValidationResponse = await _customerCorporateProfileService.GetCorpMpNumberValidation<United.CorporateDirect.Models.CustomerProfile.CorpMpNumberValidationResponse>(token, jsonRequest, sessionId).ConfigureAwait(false);
                
            }
            catch (Exception)
            {
                return default;
            }
            return corpMpNumberValidationResponse;
        }

        public InfoWarningMessages CorpMultiPaxinfoWarningMessages(List<CMSContentMessage> cMSContentMessages)
        {
            InfoWarningMessages infoWarningMessages = new InfoWarningMessages();
            try
            {
                
                infoWarningMessages.HeaderMessage = "Traveling for business";
                infoWarningMessages.IsCollapsable = true;
                infoWarningMessages.IsExpandByDefault = true;
                infoWarningMessages.Messages = new List<string> { "Please ensure all travellers on this trip are enrolled in your company’s corporate travel plan and comply with your company’s travel policy." };

                infoWarningMessages.IconType = MOBINFOWARNINGMESSAGEICON.INFORMATION.ToString();
                infoWarningMessages.Order = MOBINFOWARNINGMESSAGEORDER.CORPORATEUNENROLLEDTRAVELER.ToString();
               
            }
            catch (Exception)
            {
                return default;
            }
            
            return infoWarningMessages;
        }
        #endregion
    }
}
