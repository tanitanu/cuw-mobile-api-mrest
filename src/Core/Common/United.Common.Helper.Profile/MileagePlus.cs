using IDAutomation.NetStandard.PDF417.FontEncoder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using UAWSMPTravelCertificateService.ETCServiceSoap;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.Customer;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.ETC;
using United.Mobile.DataAccess.Loyalty;
using United.Mobile.DataAccess.MemberSignIn;
using United.Mobile.DataAccess.MPAuthentication;
using United.Mobile.DataAccess.MPRewards;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.UnitedClub;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.CSLModels;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.Shopping;
using United.Mobile.Model.UnitedClubPasses;
using United.Utility.Helper;
using static United.Mobile.Model.Common.MOBTravelCredit;
using AccountProfileInfoResponse = United.Mobile.Model.Common.AccountProfileInfoResponse;
using Activity = United.Mobile.Model.Common.Activity;
using MOBETCDetail = United.Mobile.Model.Common.MOBTravelCredit.MOBETCDetail;
using PremierQualifierTracker = United.Mobile.Model.Common.PremierQualifierTracker;
using ReadMemberInformation = United.Mobile.Model.CSLModels.ReadMemberInformation;

namespace United.Common.Helper.Profile
{
    public class MileagePlus : IMileagePlus
    {
        private readonly IConfiguration _configuration;
        private readonly ICacheLog<MileagePlus> _logger;
        private readonly IDPService _dPService;
        private readonly ILoyaltyAccountService _loyaltyAccountService;
        private readonly ILoyaltyWebService _loyaltyWebService;
        private readonly ICustomerDataService _customerDataService;
        private readonly ILoyaltyUCBService _loyaltyBalanceServices;
        private readonly IMyAccountPremierService _myAccountPremierService;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IETCService _eTCService;
        private readonly IEmployeeIdByMileageplusNumber _employeeIdByMileageplusNumber;
        private readonly IMPFutureFlightCredit _mPFutureFlightCredit;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IUnitedClubMembershipService _unitedClubMembershipService;
        private readonly IUnitedClubMembershipV2Service _unitedClubMembershipV2Service;
        private readonly IHeaders _headers;
        private string _token;

        public MileagePlus(IConfiguration configuration
            , ICacheLog<MileagePlus> logger
            , IDPService dPService
            , IDynamoDBService dynamoDBService
            , ILoyaltyAccountService loyaltyAccountService
            , ILoyaltyWebService loyaltyWebService
            , ICustomerDataService customerDataService
            , IEmployeeIdByMileageplusNumber employeeIdByMileageplusNumber
            , ILoyaltyUCBService loyaltyBalanceServices
            , IMPFutureFlightCredit mPFutureFlightCredit
            , IMyAccountPremierService myAccountPremierService
            , IETCService eTCService
            , ISessionHelperService sessionHelperService
            , IUnitedClubMembershipService unitedClubMembershipService
            , IUnitedClubMembershipV2Service unitedClubMembershipV2Service
            , IHeaders headers
           )
        {
            _configuration = configuration;
            _logger = logger;
            _dPService = dPService;
            _dynamoDBService = dynamoDBService;
            _loyaltyAccountService = loyaltyAccountService;
            _loyaltyWebService = loyaltyWebService;
            _customerDataService = customerDataService;
            _employeeIdByMileageplusNumber = employeeIdByMileageplusNumber;
            _loyaltyBalanceServices = loyaltyBalanceServices;
            _mPFutureFlightCredit = mPFutureFlightCredit;
            _myAccountPremierService = myAccountPremierService;
            _eTCService = eTCService;
            _sessionHelperService = sessionHelperService;
            _unitedClubMembershipService = unitedClubMembershipService;
            _unitedClubMembershipV2Service = unitedClubMembershipV2Service;
            _headers = headers;
        }

        public async Task<MPAccountSummary> GetAccountSummary(string transactionId, string mileagePlusNumber, string languageCode, bool includeMembershipCardBarCode, string sessionId = "")
        {
            if (_configuration.GetValue<bool>("EnableUCBPhase1_MobilePhase1Changes"))
            {
                return await GetAccountSummaryV2(transactionId, mileagePlusNumber, languageCode, includeMembershipCardBarCode, sessionId);
            }
            else
            {

                MPAccountSummary mpSummary = new MPAccountSummary();
                AccountProfileInfoResponse objloyaltyProfileResponse = new AccountProfileInfoResponse();

                if (_configuration.GetValue<bool>("AddingExtraLogggingForAwardShopAccountSummary_Toggle"))
                {
                    if (string.IsNullOrWhiteSpace(mileagePlusNumber))
                    {
                        _logger.LogError("GetAccountSummary - Empty MPNumber Passed");
                    }
                }

                try
                {
                    bool fourSegmentMinimunWaivedMember = false;

                    string balanceExpireDisclaimer = string.Empty;
                    bool noMileageExpiration = false;

                    _token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration);

                    var loyaltyProfileResponse = await _loyaltyAccountService.GetAccountProfileInfo<AccountProfileInfoResponse>(_token, mileagePlusNumber, _headers.ContextValues.SessionId);

                    #region 55359, 81220 Bug Fix
                    //55359 and 81220 to check for closed, temporary closed and ClosedPermanently account-Alekhya 
                    if (loyaltyProfileResponse != null && loyaltyProfileResponse.AccountProfileInfo != null && (loyaltyProfileResponse.AccountProfileInfo.IsClosedTemporarily || loyaltyProfileResponse.AccountProfileInfo.IsClosedPermanently || loyaltyProfileResponse.AccountProfileInfo.IsClosed))
                    {
                        string exceptionMessage = _configuration.GetValue<string>("ErrorContactMileagePlus");
                        throw new MOBUnitedException(exceptionMessage);
                    }
                    //Changes end here
                    #endregion 55359, 81220 Bug Fix
                    if (loyaltyProfileResponse.AccountProfileInfo.BirthDate != null)
                        mpSummary.BirthDate = loyaltyProfileResponse.AccountProfileInfo.BirthDate.ToString();
                    mpSummary.Balance = loyaltyProfileResponse.AccountProfileInfo.CurrentBalance.ToString();
                    mpSummary.LastActivityDate = loyaltyProfileResponse.AccountProfileInfo.LastActivityDate != 0 ? DateTime.ParseExact(loyaltyProfileResponse.AccountProfileInfo.LastActivityDate.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy") : "";
                    mpSummary.LearnMoreTitle = _configuration.GetValue<string>("MilagePluslearnMoreText");
                    mpSummary.LearnMoreHeader = _configuration.GetValue<string>("MilagePluslearnMoreDesc");
                    mpSummary.MilesNeverExpireText = _configuration.GetValue<string>("MilagePlusMilesNeverExpire");
                    if (_configuration.GetValue<bool>("HideMileageBalanceExpireDate") == true)
                    {
                        mpSummary.BalanceExpireDate = "";
                        mpSummary.IsHideMileageBalanceExpireDate = true;
                    }
                    else
                    {
                        mpSummary.BalanceExpireDate = loyaltyProfileResponse.AccountProfileInfo.MilesExpireDate != 0 ? DateTime.ParseExact(loyaltyProfileResponse.AccountProfileInfo.MilesExpireDate.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy") : "";
                    }
                    mpSummary.BalanceExpireDisclaimer = _configuration.GetValue<bool>("HideMileageBalanceExpireDate") ? HttpUtility.HtmlDecode(_configuration.GetValue<string>("BalanceExpireDisclaimerNeverExpire")) : HttpUtility.HtmlDecode(_configuration.GetValue<string>("BalanceExpireDisclaimer"));
                    mpSummary.CustomerId = loyaltyProfileResponse.AccountProfileInfo.CustomerId;
                    mpSummary.EliteMileage = string.Format("{0:###,##0}", loyaltyProfileResponse.AccountProfileInfo.PremierQualifyingMilesBalance);
                    if (loyaltyProfileResponse.AccountProfileInfo.PremierQualifyingSegmentBalance == 0)
                    {
                        mpSummary.EliteSegment = "0";
                    }
                    else
                    {
                        mpSummary.EliteSegment = string.Format("{0:0.#}", loyaltyProfileResponse.AccountProfileInfo.PremierQualifyingSegmentBalance);
                    }
                    // test comments
                    mpSummary.EliteStatus = new MOBEliteStatus(_configuration);
                    mpSummary.EliteStatus.Code = loyaltyProfileResponse.AccountProfileInfo.EliteLevel;
                    mpSummary.EnrollDate = loyaltyProfileResponse.AccountProfileInfo.EnrollDate.ToString("MM/dd/yyyy");
                    //mpSummary.HasUAClubMemberShip = loyaltyProfileResponse.AccountProfileInfo.IsUnitedClubMember;
                    //mpSummary.LastExpiredMileDate = DateTime.ParseExact(loyaltyProfileResponse.AccountProfileInfo.MilesExpireLastActivityDate.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy");
                    mpSummary.LastFlightDate = (loyaltyProfileResponse.AccountProfileInfo.LastFlightDate != 0) ? DateTime.ParseExact(loyaltyProfileResponse.AccountProfileInfo.LastFlightDate.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy") : "";
                    mpSummary.MileagePlusNumber = loyaltyProfileResponse.AccountProfileInfo.AccountId;
                    mpSummary.Name = new MOBName();
                    mpSummary.Name.First = loyaltyProfileResponse.AccountProfileInfo.FirstName;
                    mpSummary.Name.Last = loyaltyProfileResponse.AccountProfileInfo.LastName;
                    mpSummary.Name.Middle = loyaltyProfileResponse.AccountProfileInfo.MiddleName;
                    mpSummary.Name.Suffix = loyaltyProfileResponse.AccountProfileInfo.Suffix;
                    mpSummary.Name.Title = loyaltyProfileResponse.AccountProfileInfo.Title;

                    mpSummary.IsCEO = loyaltyProfileResponse.AccountProfileInfo.IsCeo;

                    mpSummary.LifetimeMiles = loyaltyProfileResponse.AccountProfileInfo.LifetimeMiles;

                    if (loyaltyProfileResponse.AccountProfileInfo.MillionMilerLevel == 0)
                    {
                        mpSummary.MillionMilerIndicator = string.Empty;
                    }
                    else
                    {
                        mpSummary.MillionMilerIndicator = loyaltyProfileResponse.AccountProfileInfo.MillionMilerLevel.ToString();
                    }

                    if (Convert.ToDateTime(_configuration.GetValue<string>("MP2014EnableDate")) < DateTime.Now)
                    {
                        if (loyaltyProfileResponse != null && loyaltyProfileResponse.AccountProfileInfo != null)
                        {
                            bool isValidPqdAddress = false;
                            bool activeNonPresidentialPlusCardMember = false;
                            bool activePresidentialPlusCardMembe = false;
                            bool showChaseBonusTile = false;

                            //Migrate XML to CSL service call
                            //[CLEANUP API-MIGRATION]  Removed XML Service Calls
                            if (_configuration.GetValue<bool>("NewServieCall_GetProfile_PaymentInfos"))
                            {
                                var tupleRes = await IsValidPQDAddressV2("GetAccountSummary", transactionId, _token, mpSummary.MileagePlusNumber, isValidPqdAddress, activeNonPresidentialPlusCardMember, activePresidentialPlusCardMembe, fourSegmentMinimunWaivedMember, showChaseBonusTile);
                                isValidPqdAddress = tupleRes.isValidPqdAddress;
                                activeNonPresidentialPlusCardMember = tupleRes.activeNonPresidentialPlusCardMember;
                                activePresidentialPlusCardMembe = tupleRes.activePresidentialPlusCardMembe;
                                showChaseBonusTile = tupleRes.isValidPqdAddress;
                                fourSegmentMinimunWaivedMember = tupleRes.fourSegmentMinimunWaivedMember;

                            }

                            mpSummary.ShowChaseBonusTile = showChaseBonusTile;
                            AssignCustomerChasePromoType(mpSummary, showChaseBonusTile);

                            noMileageExpiration = activeNonPresidentialPlusCardMember || activePresidentialPlusCardMembe;

                            if (fourSegmentMinimunWaivedMember)
                            {
                                mpSummary.FourSegmentMinimun = "Waived";
                            }
                            else if (loyaltyProfileResponse.AccountProfileInfo.MinimumSegment >= 4)
                            {
                                mpSummary.FourSegmentMinimun = "4 of 4";
                            }
                            else
                            {
                                mpSummary.FourSegmentMinimun = string.Format("{0} of 4", loyaltyProfileResponse.AccountProfileInfo.MinimumSegment);
                            }

                            if (loyaltyProfileResponse.AccountProfileInfo.PremierQualifyingDollars == 0 || !isValidPqdAddress)
                            {
                                if (!isValidPqdAddress)
                                {
                                    mpSummary.PremierQualifyingDollars = string.Empty;
                                }
                                else
                                {
                                    mpSummary.PremierQualifyingDollars = "0";
                                }
                            }
                            else
                            {
                                decimal pqd = 0;
                                try
                                {
                                    pqd = Convert.ToDecimal(loyaltyProfileResponse.AccountProfileInfo.PremierQualifyingDollars);
                                }
                                catch (Exception) { }
                                //Below are the two toggles used in Appsettings 
                                //< add key = "PqdAmount" value = "12000" /> < add key = "PqdText" value = "Over $12,000" />
                                //Work Items LOYAL-3236, LOYAL-3241
                                if (!string.IsNullOrEmpty(_configuration.GetValue<string>("PqdAmount")) && !string.IsNullOrEmpty(_configuration.GetValue<string>("PqdText")))
                                {
                                    if (pqd > Convert.ToDecimal(_configuration.GetValue<string>("PqdAmount")))
                                    {
                                        mpSummary.PremierQualifyingDollars = _configuration.GetValue<string>("PqdText");
                                    }
                                }
                                else
                                {
                                    mpSummary.PremierQualifyingDollars = pqd.ToString("C0");
                                }
                            }

                            string pdqchasewaiverLabel = string.Empty;
                            string pdqchasewavier = string.Empty;
                            if (isValidPqdAddress)
                            {
                                if (!string.IsNullOrEmpty(loyaltyProfileResponse.AccountProfileInfo.ChaseSpendingIndicator) && loyaltyProfileResponse.AccountProfileInfo.ChaseSpendingIndicator.Equals("Y"))
                                {
                                }
                                if (!string.IsNullOrEmpty(loyaltyProfileResponse.AccountProfileInfo.PresidentialPlusIndicator) && loyaltyProfileResponse.AccountProfileInfo.PresidentialPlusIndicator.Equals("Y"))
                                {
                                }
                                //[CLEANUP API-MIGRATION]
                                //if (!_configuration.GetValue<bool>("IsAvoidUAWSChaseMessagingSoapCall"))
                                //{
                                //    GetChaseMessage(activeNonPresidentialPlusCardMember, activePresidentialPlusCardMembe, chaseSpending, presidentialPlus, ref pdqchasewaiverLabel, ref pdqchasewavier, ref balanceExpireDisclaimer);
                                //    mpSummary.PDQchasewaiverLabel = pdqchasewaiverLabel;
                                //    mpSummary.PDQchasewavier = pdqchasewavier;
                                //}
                                if (!string.IsNullOrEmpty(balanceExpireDisclaimer) && !_configuration.GetValue<bool>("HideMileageBalanceExpireDate"))
                                {
                                    mpSummary.BalanceExpireDisclaimer = mpSummary.BalanceExpireDisclaimer + " " + balanceExpireDisclaimer;
                                }
                            }
                            if (!fourSegmentMinimunWaivedMember && !_configuration.GetValue<bool>("HideMileageBalanceExpireDate"))
                            {
                                mpSummary.BalanceExpireDisclaimer = mpSummary.BalanceExpireDisclaimer + " " + HttpUtility.HtmlDecode(_configuration.GetValue<string>("FouSegmentMessage"));
                            }
                        }
                    }

                    if (includeMembershipCardBarCode)
                    {
                        //%B[\w]{2,3}\d{5,6}\s{7}\d{4}(GL|1K|GS|PL|SL|\s\s)\s\s[\s\w\<\-\'\.]{35}\sUA
                        string eliteLevel = "";
                        switch (mpSummary.EliteStatus.Level)
                        {
                            case 0:
                                eliteLevel = "  ";
                                break;
                            case 1:
                                eliteLevel = "SL";
                                break;
                            case 2:
                                eliteLevel = "GL";
                                break;
                            case 3:
                                eliteLevel = "PL";
                                break;
                            case 4:
                                eliteLevel = "1K";
                                break;
                            case 5:
                                eliteLevel = "GS";
                                break;
                        }
                        string name = string.Format("{0}<{1}", mpSummary.Name.Last, mpSummary.Name.First);
                        if (!string.IsNullOrEmpty(mpSummary.Name.Middle))
                        {
                            name = name + " " + mpSummary.Name.Middle.Substring(0, 1);
                        }
                        name = String.Format("{0, -36}", name);

                        bool hasUnitedClubMemberShip = false;
                        mpSummary.uAClubMemberShipDetails = await GetUnitedClubMembershipDetails(mpSummary.MileagePlusNumber, hasUnitedClubMemberShip, languageCode);
                        mpSummary.HasUAClubMemberShip = hasUnitedClubMemberShip;
                        //if (hasUnitedClubMemberShip)
                        //{
                        mpSummary.MembershipCardExpirationDate = await this.GetMembershipCardExpirationDate(mpSummary.MileagePlusNumber, mpSummary.EliteStatus.Level);
                        string expirationDate = _configuration.GetValue<string>("MPCardExpirationDate");
                        try
                        {
                            if (mpSummary.MembershipCardExpirationDate.Equals("Trial Status"))
                            {
                                expirationDate = string.Format("12/{0}", DateTime.Today.Year);
                            }
                            else
                            {
                                expirationDate = string.IsNullOrEmpty(mpSummary.MembershipCardExpirationDate) ? _configuration.GetValue<string>("MPCardExpirationDate") : mpSummary.MembershipCardExpirationDate.Substring(11, 2) + mpSummary.MembershipCardExpirationDate.Substring(16, 2);
                            }
                        }
                        catch (System.Exception) { }

                        //string expirationDate = _configuration.GetValue<string>"MPCardExpirationDate");
                        string barCodeData = string.Format("%B{0}       {1}{2}  {3}UA              ?", mpSummary.MileagePlusNumber, expirationDate, eliteLevel, name);
                        string allianceTierLevel = "   ";
                        switch (mpSummary.EliteStatus.Level)
                        {
                            case 0:
                                allianceTierLevel = "   ";
                                break;
                            case 1:
                                allianceTierLevel = "UAS";
                                break;
                            case 2:
                                allianceTierLevel = "UAG";
                                break;
                            case 3:
                                allianceTierLevel = "UAG";
                                break;
                            case 4:
                                allianceTierLevel = "UAG";
                                break;
                            case 5:
                                allianceTierLevel = "UAG";
                                break;
                        }
                        string allianceTierLevelExpirationDate = "    ";
                        if (!allianceTierLevel.Equals("   "))
                        {
                            if (mpSummary.MembershipCardExpirationDate.Equals("Trial Status"))
                            {
                                allianceTierLevelExpirationDate = string.Format("12/{0}", DateTime.Today.Year);
                            }
                            else
                            {
                                allianceTierLevelExpirationDate = string.IsNullOrEmpty(mpSummary.MembershipCardExpirationDate) ? _configuration.GetValue<string>("MPCardExpirationDate") : mpSummary.MembershipCardExpirationDate.Substring(11, 2) + mpSummary.MembershipCardExpirationDate.Substring(16, 2);
                            }
                        }

                        string paidLoungeIndicator = "N";
                        string paidLoungeExpireationDate = "      ";
                        if (mpSummary.uAClubMemberShipDetails != null)
                        {
                            if (string.IsNullOrEmpty(mpSummary.uAClubMemberShipDetails.PrimaryOrCompanion))
                            {
                                paidLoungeIndicator = "P";
                            }
                            else
                            {
                                paidLoungeIndicator = mpSummary.uAClubMemberShipDetails.PrimaryOrCompanion;
                            }
                            paidLoungeExpireationDate = Convert.ToDateTime(mpSummary.uAClubMemberShipDetails.DiscontinueDate).ToString("ddMMyyyy");
                        }

                        string starBarCodeData = string.Format("FFPC001UA{0}{1}{2}{3}{4}{5}{6}{7}^001", mpSummary.MileagePlusNumber.PadRight(16), mpSummary.Name.Last.PadRight(20), mpSummary.Name.First.PadRight(20), allianceTierLevel, allianceTierLevelExpirationDate, eliteLevel, paidLoungeIndicator, paidLoungeExpireationDate);

                        if (!string.IsNullOrEmpty(_configuration.GetValue<string>("ReturnMPMembershipBarcode")) && Convert.ToBoolean(_configuration.GetValue<string>("ReturnMPMembershipBarcode")))
                        {
                            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("UseStarMembershipCardFormatDateTime")) && Convert.ToDateTime(_configuration.GetValue<string>("UseStarMembershipCardFormatDateTime")) <= DateTime.Now)
                            {
                                mpSummary.MembershipCardBarCodeString = starBarCodeData.ToUpper();
                                mpSummary.MembershipCardBarCode = GetBarCode(starBarCodeData);
                            }
                            else
                            {
                                mpSummary.MembershipCardBarCodeString = barCodeData.ToUpper();
                                mpSummary.MembershipCardBarCode = GetBarCode(barCodeData);
                            }
                        }
                        else
                        {
                            mpSummary.MembershipCardBarCodeString = null;
                            mpSummary.MembershipCardBarCode = null;
                        }
                        //}
                    }

                    //bool noMileageExpiration = HasChaseNoMileageExpirationCard(mpSummary.MileagePlusNumber);
                    mpSummary.NoMileageExpiration = noMileageExpiration.ToString();

                    if (_configuration.GetValue<bool>("HideMileageBalanceExpireDate"))

                    {
                        mpSummary.NoMileageExpirationMessage = HttpUtility.HtmlDecode(_configuration.GetValue<string>("BalanceExpireDisclaimerNeverExpire"));
                    }
                    else if (noMileageExpiration)
                    {
                        mpSummary.NoMileageExpirationMessage = HttpUtility.HtmlDecode(_configuration.GetValue<string>("ChaseNoMileageExpirationMessage") + " " + balanceExpireDisclaimer);
                        if (!fourSegmentMinimunWaivedMember)
                        {
                            mpSummary.NoMileageExpirationMessage = HttpUtility.HtmlDecode(mpSummary.NoMileageExpirationMessage + " " + _configuration.GetValue<string>("FouSegmentMessage"));
                        }
                    }
                }
                catch (MOBUnitedException ex)
                {
                    throw new MOBUnitedException(ex.Message);
                }
                catch (System.Exception ex)
                {
                    throw new System.Exception(ex.Message);
                }
                finally
                {
                    try
                    {
                        //if (response != null)
                        //{
                        //    response.Close();
                        //}
                    }
                    catch
                    {
                        throw new System.Exception("United Data Services Not Available");
                    }
                }

                _logger.LogInformation("Loyalty Get Profile Response to client {@MpSummary}", JsonConvert.SerializeObject(mpSummary));

                if (_configuration.GetValue<string>("StartDateTimeToReturnEmptyMPExpirationDate") != null && _configuration.GetValue<string>("EndtDateTimeToReturnEmptyMPExpirationDate") != null && DateTime.ParseExact(_configuration.GetValue<string>("StartDateTimeToReturnEmptyMPExpirationDate").ToString().ToUpper().Trim(), "MM/dd/yyyy hh:mm tt", CultureInfo.InvariantCulture) <= DateTime.Now && DateTime.ParseExact(_configuration.GetValue<string>("EndtDateTimeToReturnEmptyMPExpirationDate").ToString().ToUpper().Trim(), "MM/dd/yyyy hh:mm tt", CultureInfo.InvariantCulture) >= DateTime.Now)
                {
                    mpSummary.MembershipCardExpirationDate = string.Empty;
                }
                return mpSummary;
            }
        }

        public async Task<(string employeeId, string displayEmployeeId)> GetEmployeeIdy(string mileageplusNumber, string transactionId, string sessionId, string displayEmployeeId)
        {
            string employeeId = string.Empty;

            var response = await _employeeIdByMileageplusNumber.GetEmployeeIdy(mileageplusNumber, transactionId, sessionId).ConfigureAwait(false);
            var empResponse = JsonConvert.DeserializeObject<GetEmpIdByMpNumber>(response);
            if (empResponse.MPLinkedId != null)
            {
                displayEmployeeId = empResponse.FileNumber;
                employeeId = empResponse.MPLinkedId;
            }
            else
            {
                displayEmployeeId = empResponse.EmployeeId;
                employeeId = empResponse.EmployeeId;
            }
            if (employeeId == null)
            {
                displayEmployeeId = string.Empty;
                employeeId = string.Empty;
            }
            return (employeeId, displayEmployeeId);
        }

        private async Task<(bool isValidPqdAddress, bool activeNonPresidentialPlusCardMember, bool activePresidentialPlusCardMembe, bool fourSegmentMinimunWaivedMember, bool chaseBonusTile)> IsValidPQDAddressV2(string callingMethodName, string transactionID, string token, string mpNumber, bool isValidPqdAddress, bool activeNonPresidentialPlusCardMember, bool activePresidentialPlusCardMembe, bool fourSegmentMinimunWaivedMember, bool chaseBonusTile)
        {
            try
            {
                //For MP 2015, we will show PDQ for all 
                isValidPqdAddress = true;

                Services.Customer.Common.ProfileRequest profileRequest = new Services.Customer.Common.ProfileRequest();
                profileRequest.LoyaltyId = mpNumber;
                profileRequest.RefreshCache = false;
                profileRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices");
                profileRequest.LangCode = "en-US";

                List<string> requestStringList = new List<string>();
                requestStringList.Add("PaymentInfos");
                profileRequest.DataToLoad = requestStringList;

                string jsonRequest = JsonConvert.SerializeObject(profileRequest);

                //https://unitedservicesqa.ual.com/8.2/customer/customerdata/api/GetProfile

                var customerDataResponse = await _customerDataService.GetCustomerData<Services.Customer.Common.ProfileResponse>(token, _headers.ContextValues.SessionId, jsonRequest).ConfigureAwait(false);

                var response = customerDataResponse.response;
                if (response != null && response.Profiles != null && response.Profiles.Count > 0 && response.Profiles[0].Travelers != null &&
                    response.Profiles[0].Travelers.Count > 0 && response.Profiles[0].Travelers[0].MileagePlus != null &&
                    response.Profiles[0].Travelers[0].MileagePlus.PaymentInfos != null &&
                    response.Profiles[0].Travelers[0].MileagePlus.PaymentInfos.Count > 0)
                {
                    bool hasChaseCard = false;
                    foreach (Services.Customer.Common.PaymentInfo paymentInfo in response.Profiles[0].Travelers[0].MileagePlus.PaymentInfos)
                    {
                        if (!string.IsNullOrEmpty(paymentInfo.PartnerCode))
                        {
                            if (paymentInfo.PartnerCode.Equals("CH"))
                            {
                                hasChaseCard = true;
                                activeNonPresidentialPlusCardMember = true;
                            }

                            if (paymentInfo.IsPartnerCard && paymentInfo.PartnerCode.Equals("CH"))
                            {
                                if (!string.IsNullOrEmpty(paymentInfo.CardType) && _configuration.GetValue<string>("PresidentialPlusChaseCardTypes").IndexOf(paymentInfo.CardType) != -1)
                                {
                                    fourSegmentMinimunWaivedMember = !_configuration.GetValue<bool>("EnableVBQII") ? true : false;
                                }
                                if (!string.IsNullOrEmpty(paymentInfo.CardType) && _configuration.GetValue<string>("PreferredPresidentialPlusChaseCardTypes").IndexOf(paymentInfo.CardType) != -1)
                                {
                                    activePresidentialPlusCardMembe = true;
                                }

                                if (DateTime.Now >= Convert.ToDateTime(_configuration.GetValue<string>("ChaseBonusTileStartDate")) && DateTime.Now < Convert.ToDateTime(_configuration.GetValue<string>("ChaseBonusTileEndDate")))
                                {
                                    if (!string.IsNullOrEmpty(paymentInfo.CardType) && _configuration.GetValue<string>("ChaseBonusTileChaseCardTypes").IndexOf(paymentInfo.CardType) == -1)
                                    {
                                        chaseBonusTile = true;
                                    }
                                }
                            }
                        }
                    }
                    if (!hasChaseCard)
                    {
                        if (DateTime.Now >= Convert.ToDateTime(_configuration.GetValue<string>("ChaseBonusTileStartDate")) && DateTime.Now < Convert.ToDateTime(_configuration.GetValue<string>("ChaseBonusTileEndDate")))
                        {
                            chaseBonusTile = true;
                        }
                    }
                }
                else
                {
                    if (DateTime.Now >= Convert.ToDateTime(_configuration.GetValue<string>("ChaseBonusTileStartDate")) && DateTime.Now < Convert.ToDateTime(_configuration.GetValue<string>("ChaseBonusTileEndDate")))
                    {
                        chaseBonusTile = true;
                    }
                }
            }
            catch (Exception ex)
            {
                string enterlog = string.IsNullOrEmpty(ex.StackTrace) ? ex.Message : ex.StackTrace;
                _logger.LogError("IsValidPQDAddressV2 {@Exception}", enterlog);
                if (DateTime.Now >= Convert.ToDateTime(_configuration.GetValue<string>("ChaseBonusTileStartDate")) && DateTime.Now < Convert.ToDateTime(_configuration.GetValue<string>("ChaseBonusTileEndDate")))
                {
                    chaseBonusTile = true;
                }
            }
            return (isValidPqdAddress, activeNonPresidentialPlusCardMember, activePresidentialPlusCardMembe, fourSegmentMinimunWaivedMember, chaseBonusTile);
        }

        private void AssignCustomerChasePromoType(MPAccountSummary mpSummary, bool showChaseBonusTile)
        {
            if (mpSummary != null)
            {
                if (showChaseBonusTile)
                {
                    if (mpSummary.EliteStatus != null && mpSummary.EliteStatus.Level > 0)
                    {
                        mpSummary.ChasePromoType = "70K";
                    }
                    else
                    {
                        mpSummary.ChasePromoType = "50K";
                    }
                }
                else
                {
                    mpSummary.ChasePromoType = "";
                }
            }
        }

        private async Task<UnitedClubMemberShipDetails> GetUnitedClubMembershipDetails(string mpAccountNumber, bool hasUnitedClubMemberShip, string languageCode, string token = "")
        {
            #region
            hasUnitedClubMemberShip = false;
            UnitedClubMemberShipDetails clubMemberShipDetails = null;
            try
            {
                var TransactionId = string.Empty;
                MOBClubMembership mobClubMembership = null;
                if (_configuration.GetValue<bool>("EnableUCBPhase1_MobilePhase1Changes"))
                {
                    mobClubMembership = await GetCurrentMembershipInfoV2(mpAccountNumber, token);
                }
                else
                {
                    mobClubMembership = await GetCurrentMembershipInfo(mpAccountNumber);
                }

                Mobile.Model.UnitedClubPasses.ClubMembership currentMembershipInfo = null;

                var jsonResponse = await _loyaltyAccountService.GetCurrentMembershipInfo(mpAccountNumber).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    List<Mobile.Model.UnitedClubPasses.UClubMembershipInfo> uClubMembershipInfoList = JsonConvert.DeserializeObject<List<Mobile.Model.UnitedClubPasses.UClubMembershipInfo>>(jsonResponse);
                    if (uClubMembershipInfoList != null && uClubMembershipInfoList.Count > 0)
                    {
                        foreach (Mobile.Model.UnitedClubPasses.UClubMembershipInfo uClubMembershipInfo in uClubMembershipInfoList)
                        {
                            if (uClubMembershipInfo.DiscontinueDate >= DateTime.Now && string.IsNullOrEmpty(uClubMembershipInfo.ClubStatusCode))
                            {
                                currentMembershipInfo = new Mobile.Model.UnitedClubPasses.ClubMembership
                                {
                                    CompanionMPNumber = string.IsNullOrEmpty(uClubMembershipInfo.CompanionMpNumber) ? string.Empty : uClubMembershipInfo.CompanionMpNumber,
                                    EffectiveDate = uClubMembershipInfo.EffectiveDate.ToString("MM/dd/yyyy"),
                                    ExpirationDate = uClubMembershipInfo.DiscontinueDate.ToString("MM/dd/yyyy"),
                                    IsPrimary = string.IsNullOrEmpty(uClubMembershipInfo.PrimaryOrCompanion),
                                    MembershipTypeCode = string.IsNullOrEmpty(uClubMembershipInfo.MemberTypeCode) ? string.Empty : uClubMembershipInfo.MemberTypeCode,
                                    MembershipTypeDescription = string.IsNullOrEmpty(uClubMembershipInfo.MemberTypeDescription) ? string.Empty : uClubMembershipInfo.MemberTypeDescription
                                };
                            }
                        }
                    }
                }

                if (currentMembershipInfo != null)
                {
                    #region
                    clubMemberShipDetails = new UnitedClubMemberShipDetails();
                    hasUnitedClubMemberShip = true;
                    clubMemberShipDetails.MemberTypeCode = currentMembershipInfo.MembershipTypeCode;
                    if (_configuration.GetValue<string>("United_Club_Membership_Defalut_Desc") != null)
                    {
                        clubMemberShipDetails.MemberTypeDesc = _configuration.GetValue<string>("United_Club_Membership_Defalut_Desc").ToString();
                    }
                    else
                    {
                        clubMemberShipDetails.MemberTypeDesc = currentMembershipInfo.MembershipTypeDescription;
                    }
                    clubMemberShipDetails.EffectiveDate = Convert.ToDateTime(currentMembershipInfo.EffectiveDate).ToString("MM/dd/yyyy");
                    clubMemberShipDetails.DiscontinueDate = Convert.ToDateTime(currentMembershipInfo.ExpirationDate).ToString("MM/dd/yyyy");
                    if (!string.IsNullOrEmpty(currentMembershipInfo.CompanionMPNumber))
                    {
                        clubMemberShipDetails.CompanionMileagePlus = currentMembershipInfo.CompanionMPNumber;
                        clubMemberShipDetails.PrimaryOrCompanion = currentMembershipInfo.IsPrimary ? "P" : "C";
                    }
                    #endregion
                }
            }
            catch (System.Exception)
            {
                hasUnitedClubMemberShip = false;
            }
            return clubMemberShipDetails;
            #endregion
        }

        private async Task<string> GetMembershipCardExpirationDate(string mpAccountNumber, int eliteStatus)
        {
            string membershipCardExpirationDate = string.Empty;

            bool isInstantElite = await IsInstantElite(mpAccountNumber).ConfigureAwait(false);

            if (isInstantElite)
            {
                membershipCardExpirationDate = "Trial Status";
            }
            else
            {
                if (eliteStatus != 0)
                {
                    if (DateTime.Today.Month == 1)
                    {
                        membershipCardExpirationDate = string.Format("Valid thru 01/{0}", DateTime.Today.Year);
                    }
                    else
                    {
                        membershipCardExpirationDate = string.Format("Valid thru 01/{0}", DateTime.Today.AddYears(1).Year);
                    }
                }
            }

            return membershipCardExpirationDate;
        }

        private async Task<bool> IsInstantElite(string mileagePlusNumber)
        {
            bool ok = false;

            try
            {
                string token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration).ConfigureAwait(false);
                string loyaltyWPCLUrl = await _loyaltyWebService.GetLoyaltyData(token, mileagePlusNumber, _headers.ContextValues.TransactionId);
                if (!string.IsNullOrEmpty(loyaltyWPCLUrl) && loyaltyWPCLUrl.ToUpper().IndexOf("<ISTRIALELITE>TRUE</ISTRIALELITE>") != -1)
                {
                    ok = true;
                }
            }
            catch (System.Exception) { }

            return ok;
        }

        private byte[] GetBarCode(string data)
        {
            string dataToEncode = data;
            bool applyTilde = true;
            bool truncate = true;
            int ModuleSize = 5;
            int QuietZone = 3;
            int TotalRows = 3;
            int TotalCols = 5;
            int ECLevel = 8;
            PDF417 obj = new PDF417();
            byte[] bmpstream = obj.EncodePDF417(dataToEncode, applyTilde, ECLevel, EncodingModes.Binary, TotalCols, TotalRows, truncate, QuietZone, ModuleSize);
            return bmpstream;
        }

        public void GetMPEliteLevelExpirationDateAndGenerateBarCode(MPAccountSummary mpSummary, string premierLevelExpirationDate, MOBInstantElite instantElite) // these 2 new properties will be set from Cusotmer Profile Serivce in Mp Validate Sign Action "premierLevelExpirationDate" & "instantElite"
        {
            //config value - "Keep_MREST_MP_EliteLevel_Expiration_Logic" was missing in all config file of MRest app
            if (!_configuration.GetValue<bool>("Keep_MREST_MP_EliteLevel_Expiration_Logic"))
            {
                #region Current Channel Side MP Expiratoin & BarCode Logic
                mpSummary.MembershipCardExpirationDate = string.Empty;
                string expirationDate = _configuration.GetValue<string>("MPCardExpirationDate");
                string consolidatedCode = string.Empty; string instantEliteExpirationDate = string.Empty;
                if (instantElite != null)
                {
                    consolidatedCode = instantElite.ConsolidatedCode;
                    instantEliteExpirationDate = instantElite.ExpirationDate;
                }
                expirationDate = GetMPExpirationDateFromProfileAndQualService(mpSummary, premierLevelExpirationDate, consolidatedCode, instantEliteExpirationDate, expirationDate);
                #region eliteLevel, name for Bar Code Generation
                string eliteLevel = "";
                switch (mpSummary.EliteStatus.Level)
                {
                    case 0:
                        eliteLevel = "  ";
                        break;
                    case 1:
                        eliteLevel = "SL";
                        break;
                    case 2:
                        eliteLevel = "GL";
                        break;
                    case 3:
                        eliteLevel = "PL";
                        break;
                    case 4:
                        eliteLevel = "1K";
                        break;
                    case 5:
                        eliteLevel = "GS";
                        break;
                }
                string name = string.Format("{0}<{1}", mpSummary.Name.Last, mpSummary.Name.First);
                if (!string.IsNullOrEmpty(mpSummary.Name.Middle))
                {
                    name = name + " " + mpSummary.Name.Middle.Substring(0, 1);
                }
                name = String.Format("{0, -36}", name);
                #endregion
                string barCodeData = string.Format("%B{0}       {1}{2}  {3}UA              ?", mpSummary.MileagePlusNumber, expirationDate, eliteLevel, name); // **==>> This is the one Bar code generation for all general members
                string allianceTierLevel = "   ";
                switch (mpSummary.EliteStatus.Level)
                {
                    case 0:
                        allianceTierLevel = "   ";
                        break;
                    case 1:
                        allianceTierLevel = "UAS";
                        break;
                    case 2:
                        allianceTierLevel = "UAG";
                        break;
                    case 3:
                        allianceTierLevel = "UAG";
                        break;
                    case 4:
                        allianceTierLevel = "UAG";
                        break;
                    case 5:
                        allianceTierLevel = "UAG";
                        break;
                }
                string allianceTierLevelExpirationDate = "    ";
                if (!allianceTierLevel.Equals("   "))
                {
                    allianceTierLevelExpirationDate = expirationDate;
                }
                string paidLoungeIndicator = "N";
                string paidLoungeExpireationDate = "      ";
                if (mpSummary.uAClubMemberShipDetails != null)
                {
                    if (string.IsNullOrEmpty(mpSummary.uAClubMemberShipDetails.PrimaryOrCompanion))
                    {
                        paidLoungeIndicator = "P";
                    }
                    else
                    {
                        paidLoungeIndicator = mpSummary.uAClubMemberShipDetails.PrimaryOrCompanion;
                    }
                    paidLoungeExpireationDate = Convert.ToDateTime(mpSummary.uAClubMemberShipDetails.DiscontinueDate).ToString("ddMMyyyy");
                }

                string starBarCodeData = string.Format("FFPC001UA{0}{1}{2}{3}{4}{5}{6}{7}^001", mpSummary.MileagePlusNumber.PadRight(16), mpSummary.Name.Last.PadRight(20), mpSummary.Name.First.PadRight(20), allianceTierLevel, allianceTierLevelExpirationDate, eliteLevel, paidLoungeIndicator, paidLoungeExpireationDate); // **==>> This is the one Bar code generation for all general members

                if (_configuration.GetValue<bool>("ReturnMPMembershipBarcode"))
                {
                    if (!string.IsNullOrEmpty(_configuration.GetValue<string>("UseStarMembershipCardFormatDateTime")) && Convert.ToDateTime(_configuration.GetValue<string>("UseStarMembershipCardFormatDateTime")) <= DateTime.Now)
                    {
                        mpSummary.MembershipCardBarCodeString = starBarCodeData.ToUpper();
                        mpSummary.MembershipCardBarCode = GetBarCode(starBarCodeData);
                    }
                    else
                    {
                        mpSummary.MembershipCardBarCodeString = barCodeData.ToUpper();
                        mpSummary.MembershipCardBarCode = GetBarCode(barCodeData);
                    }
                }
                else
                {
                    mpSummary.MembershipCardBarCodeString = null;
                    mpSummary.MembershipCardBarCode = null;
                }
                #endregion Current Channel Side MP Expiration & BarCode Logic
            }
            //return mpSummary;
        }

        private static string GetMPExpirationDateFromProfileAndQualService(MPAccountSummary mpSummary, string premierLevelExpirationDate, string consolidatedCode, string instantEliteExpirationDate, string expirationDate)
        {
            if (!string.IsNullOrEmpty(premierLevelExpirationDate))
            {
                #region
                DateTime expDateTime = Convert.ToDateTime(premierLevelExpirationDate);//  DateTime.ParseExact(premierLevelExpirationDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (expDateTime.Month < 10)
                {
                    expirationDate = "0" + expDateTime.Month.ToString() + expDateTime.Year.ToString().Substring(2, 2);
                    mpSummary.MembershipCardExpirationDate = string.Format("Valid thru {0}/{1}", "0" + expDateTime.Month.ToString(), expDateTime.Year.ToString());
                }
                else
                {
                    expirationDate = expDateTime.Month.ToString() + expDateTime.Year.ToString().Substring(2, 2);
                    mpSummary.MembershipCardExpirationDate = string.Format("Valid thru {0}/{1}", expDateTime.Month.ToString(), expDateTime.Year.ToString());
                }
                #endregion
            }
            if (string.IsNullOrEmpty(premierLevelExpirationDate) && !string.IsNullOrEmpty(consolidatedCode) && consolidatedCode.ToUpper().Equals("TRIAL", StringComparison.OrdinalIgnoreCase))
            {
                #region
                if (consolidatedCode.ToUpper().Equals("TRIAL", StringComparison.OrdinalIgnoreCase))
                {
                    mpSummary.MembershipCardExpirationDate = "Valid thru Trial";
                }

                if (!string.IsNullOrEmpty(instantEliteExpirationDate))
                {
                    DateTime expDateTime = Convert.ToDateTime(instantEliteExpirationDate); //DateTime.ParseExact(instantElite.ExpirationDate.Trim(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    if (expDateTime.Month < 10)
                    {
                        expirationDate = "0" + expDateTime.Month.ToString() + expDateTime.Year.ToString().Substring(2, 2);
                    }
                    else
                    {
                        expirationDate = expDateTime.Month.ToString() + expDateTime.Year.ToString().Substring(2, 2);
                    }

                }
                else
                {
                    expirationDate = string.Format("12{0}", DateTime.Today.Year.ToString().Substring(2, 2));
                }
                #endregion
            }

            return expirationDate;
        }

        /// <summary>
        /// Takes the Request And Token And returns the pluspoiunts Object. 
        /// </summary>
        /// <param name="req"></param>
        /// <param name="dpToken"></param>
        /// <returns></returns>
        public async Task<MOBPlusPoints> GetPlusPointsFromLoyaltyBalanceService(MPAccountValidationRequest req, string dpToken)
        {
            string loyaltyBalanceUrl = string.Format(_configuration.GetValue<string>("MyAccountLoyaltyBalanceUrl"), req.MileagePlusNumber);
            string jsonResponse = string.Empty;
            try
            {
                //if (Utility.GetBooleanConfigValue("ByPassGetPremierActivityRequestValidationnGetCachedDPTOken") == true) // If the value of ByPassGetPremierActivityRequestValidationnGetCachedDPTOken is true then go get FLIFO Dp Token which we currenlty used by Flight Status NOTE: Alreday confirmed with Greg & Bob that they are not using internal to validate DP Token.
                dpToken = await _dPService.GetAnonymousToken(req.Application.Id, req.DeviceId, _configuration);

                jsonResponse = await _loyaltyBalanceServices.GetLoyaltyBalance(dpToken, req.MileagePlusNumber, req.SessionId);
            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response != null)
                {
                    throw new System.Exception(wex.Message);
                }
            }
            if (!string.IsNullOrEmpty(jsonResponse))
            {
                return await GetPlusPointsFromJson(jsonResponse, req).ConfigureAwait(false);
            }
            return null;
        }

        /// <summary>
        /// Takes the service response and returns the plus points object. 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private Task<MOBPlusPoints> GetPlusPointsFromJson(string request, MPAccountValidationRequest req)
        {
            //TODO missing
            //Need to add package - not compatibility version for this United 2.0\packages\LoyaltyCommon.1.0.6.88\lib\Net45\United.TravelBank.Model.dll
            /*
            BalanceResponse PlusPointResponse = JsonSerializer.NewtonSoftDeserialize<BalanceResponse>(request);
            Balance plusPointsBalance;
            SubBalance subBalanceRequested;
            SubBalance subBalanceConfirmed;
            if (PlusPointResponse.Balances != null && (plusPointsBalance = PlusPointResponse.Balances.FirstOrDefault(ct => ct.ProgramCurrencyType == TravelBankConstants.ProgramCurrencyType.UGC)) != null && plusPointsBalance.SubBalances != null &&
                (subBalanceRequested = plusPointsBalance.SubBalances.FirstOrDefault(s => s.Type.ToUpper() == "REQUESTED")) != null &&
                (subBalanceConfirmed = plusPointsBalance.SubBalances.FirstOrDefault(s => s.Type.ToUpper() == "CONFIRMED")) != null &&
                !(plusPointsBalance.TotalBalance == 0 && subBalanceRequested.Amount == 0 && subBalanceConfirmed.Amount == 0))
            {
                List<MOBKVP> kvpList = new List<MOBKVP>();
                if (plusPointsBalance.BalanceDetails != null)
                {
                    foreach (BalanceDetail bd in plusPointsBalance.BalanceDetails)
                    {
                        kvpList.Add(new MOBKVP(bd.ProgramCurrencyAmount.ToString(), bd.ExpirationDate.ToString("MMM dd, yyyy")));
                    }
                }
                MOBPlusPoints pluspoints = new MOBPlusPoints();
                pluspoints.PlusPointsAvailableText = _configuration.GetValue<string>("PlusPointsAvailableText");
                pluspoints.PlusPointsAvailableValue = plusPointsBalance.TotalBalance.ToString() +
                                                      " (" + subBalanceRequested.Amount + " requested)";
                pluspoints.PlusPointsDeductedText = _configuration.GetValue<string>("PlusPointsDeductedText");
                pluspoints.PlusPointsDeductedValue = subBalanceConfirmed.Amount.ToString();
                pluspoints.PlusPointsExpirationText = _configuration.GetValue<string>("PlusPointsExpirationText");
                if (plusPointsBalance.EarliestExpirationDate != null && plusPointsBalance.EarliestExpirationDate.Value != null)
                {
                    pluspoints.PlusPointsExpirationValue = plusPointsBalance.EarliestExpirationDate.Value.ToString("MMM dd, yyyy");
                }
                else
                {
                    pluspoints.IsHidePlusPointsExpiration = true;
                }
                pluspoints.PlusPointsUpgradesText = _configuration.GetValue<string>("viewUpgradesText");
                pluspoints.PlusPointsUpgradesLink = _configuration.GetValue<string>("viewUpgradesLink");
                pluspoints.PlusPointsExpirationInfo = _configuration.GetValue<string>("PlusPointsExpirationInfo");
                pluspoints.PlusPointsExpirationInfoHeader = _configuration.GetValue<string>("PlusPointsExpirationInfoHeader");
                pluspoints.PlusPointsExpirationInfoPointsSubHeader = _configuration.GetValue<string>("PlusPointsExpirationInfoPointsSubHeader");
                pluspoints.PlusPointsExpirationInfoDateSubHeader = _configuration.GetValue<string>("PlusPointsExpirationInfoDateSubHeader");
                pluspoints.ExpirationPointsAndDatesKVP = kvpList;
                pluspoints.RedirectToDotComMyTripsWithSSOCheck = _configuration.GetValue<bool>("EnablePlusPointsSSO");
                if (_configuration.GetValue<bool>("EnablePlusPointsSSO"))
                {
                    pluspoints.WebSessionShareUrl = _configuration.GetValue<string>("DotcomSSOUrl");
                    //TODO
                    //pluspoints.WebShareToken = Utility.GetSSOToken
                    //  (req.Application.Id, req.DeviceId, req.Application.Version.Major, req.TransactionId,
                    //  null, req.SessionId, req.MileagePlusNumber, LogEntries, levelSwitch);
                }
                return pluspoints;
            }
            */
            return null;
        }
        //added

        public bool IsPremierStatusTrackerSupportedVersion(int appId, string appVersion)
        {
            bool isPremierStatusTrackerSupportedVersion = false;
            if (!string.IsNullOrEmpty(appVersion) && appId != -1)
            {
                isPremierStatusTrackerSupportedVersion = GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidPremierStatusTrackerVersion", "iPhonePremierStatusTrackerVersion", "", "", true, _configuration);
            }
            return isPremierStatusTrackerSupportedVersion;
        }

        private PremierActivity GetTrackingUpgradesActivity(ServiceResponse sResponse, int trackingUpgradeType)
        {
            PremierActivity upgradeActivity = new PremierActivity();
            PremierQualifierTracker pqm1 = new PremierQualifierTracker();
            PremierQualifierTracker pqs1 = new PremierQualifierTracker();
            PremierQualifierTracker pqd1 = new PremierQualifierTracker();
            bool isPresidentialPlusCard = false;
            bool isInternationalMember = false;
            bool isChaseSpendIndicator = false;
            if (sResponse != null && sResponse.CurrentYearActivity != null)
            {
                isPresidentialPlusCard = IsPresidentialPlusCard(sResponse.CurrentYearActivity.PresidentPlusIndicator);
                isInternationalMember = IsInternationalMember(sResponse.CurrentYearActivity.DomesticIndicator);
                isChaseSpendIndicator = IsChaseCardSpendIndicator(sResponse.MileagePlusCardIndicator, sResponse.CurrentYearActivity.ChaseCardSpendIndicator);
                List<MOBKVP> lUpgradeTrackingLevels = new List<MOBKVP>();
                List<MOBKVP> lFlexMiles = new List<MOBKVP>();

                upgradeActivity.PremierActivityTitle = _configuration.GetValue<string>("PremierActivityTitle");
                upgradeActivity.PremierActivityYear = _configuration.GetValue<string>("EarnedInText") + " " + sResponse.CurrentYearActivity.ActivityYear;
                upgradeActivity.PremierActivityStatus = _configuration.GetValue<bool>("EnablePointsActivity") ? _configuration.GetValue<string>("EarnPremierPlusPoints") : _configuration.GetValue<string>("EarnPremierupgrades");

                lUpgradeTrackingLevels = GetUpgradeTrackingLevels(trackingUpgradeType, sResponse.CurrentYearActivity.EliteQualifyingMiles, sResponse.CurrentYearActivity.EliteQualifyingPoints);
                upgradeActivity.PQM = GetPQM(trackingUpgradeType, lUpgradeTrackingLevels, isPresidentialPlusCard, sResponse.CurrentYearActivity.EliteQualifyingMiles, sResponse.CurrentYearActivity.FlexEliteQualifyingMiles);
                upgradeActivity.PQS = GetPQS(trackingUpgradeType, lUpgradeTrackingLevels, sResponse.CurrentYearActivity.EliteQualifyingPoints);
                if (trackingUpgradeType == 1 || trackingUpgradeType == 2)
                {
                    upgradeActivity.PQD = GetPQD(trackingUpgradeType, lUpgradeTrackingLevels, sResponse.CurrentYearActivity.TotalRevenue, isPresidentialPlusCard, isInternationalMember, isChaseSpendIndicator);
                }
                upgradeActivity.KeyValueList = GetUpgradeKeyValues(trackingUpgradeType, isPresidentialPlusCard, sResponse.MileagePlusCardIndicator, sResponse.CurrentYearActivity.ChaseCardSpendIndicator,
                                                    sResponse.CurrentYearActivity.MinimumSegmentsRequired, sResponse.CurrentYearActivity.RpcWaivedIndicator, isInternationalMember, sResponse.AccountNumber
                                                    );
            }
            return upgradeActivity;
        }

        private bool IsPresidentialPlusCard(string presidentPlusIndicator)
        {
            if (!string.IsNullOrEmpty(presidentPlusIndicator) && presidentPlusIndicator.ToUpper() == "Y")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsInternationalMember(Activity currentYearActivity)
        {
            bool isInternationalMember = false;
            if (currentYearActivity != null && currentYearActivity.DomesticIndicator != null && currentYearActivity.DomesticIndicator.ToUpper() == "N")
            {
                isInternationalMember = true;
            }
            return isInternationalMember;
        }

        private bool IsInternationalMember(string domesticIndicator)
        {
            bool isInternationalMember = false;
            if (!string.IsNullOrEmpty(domesticIndicator) && domesticIndicator.ToUpper() == "N")
            {
                isInternationalMember = true;
            }
            return isInternationalMember;
        }

        private bool IsChaseCardSpendIndicator(string mileagePlusCardIndicator, string chaseCardSpendIndicator)
        {
            if (!string.IsNullOrEmpty(mileagePlusCardIndicator) && mileagePlusCardIndicator.ToUpper() == "Y" &&
                !string.IsNullOrEmpty(chaseCardSpendIndicator) && chaseCardSpendIndicator.ToUpper() == "Y")
            {
                return true;
            }
            return false;
        }

        private List<MOBKVP> GetUpgradeTrackingLevels(int trackingUpgradeType, long miles, double segments)
        {
            string PremierLevelStatus = string.Empty;
            List<MOBKVP> list = new List<MOBKVP>();
            string premierActivityStatus = string.Empty;
            long thresholdMiles = 0;
            long thresholdSegments = 0;
            switch (trackingUpgradeType)
            {
                case 1://1=Platinum Upgrade; 
                    list.Add(new MOBKVP("TrackerThresholdMiles", _configuration.GetValue<string>("ThresholdMilesPlatinum")));
                    list.Add(new MOBKVP("TrackerThresholdSegments", _configuration.GetValue<string>("ThresholdSegmentsPlatinum")));
                    list.Add(new MOBKVP("TrackerThresholdDollars", _configuration.GetValue<string>("ThresholdDollarsPlatinum")));
                    break;
                case 2://2=1k Upgrade; 
                    list.Add(new MOBKVP("TrackerThresholdMiles", _configuration.GetValue<string>("ThresholdMiles1K")));
                    list.Add(new MOBKVP("TrackerThresholdSegments", _configuration.GetValue<string>("ThresholdSegments1K")));
                    list.Add(new MOBKVP("TrackerThresholdDollars", _configuration.GetValue<string>("ThresholdDollars1K")));
                    break;
                case 3://3= Incremental Upgrade
                    GetIncrementalThresholdMilesAndSegments(miles, segments, out thresholdMiles, out thresholdSegments);
                    list.Add(new MOBKVP("TrackerThresholdMiles", thresholdMiles.ToString()));
                    list.Add(new MOBKVP("TrackerThresholdSegments", thresholdSegments.ToString()));
                    break;
                default:
                    break;
            }
            return list;
        }

        private PremierQualifierTracker GetPQM(int trackingUpgradeType, List<MOBKVP> lUpgradeTrackingLevels, bool isPresidentialPlusCard, long eliteQualifyingMiles, long flexEliteQualifyingMiles)
        {
            PremierQualifierTracker pqm1 = new PremierQualifierTracker();
            string thresholdMiles = string.Empty;
            pqm1.PremierQualifierTrackerTitle = _configuration.GetValue<string>("PremierQualifierTrackerMilesTitle");
            pqm1.PremierQualifierTrackerCurrentValue = Convert.ToString(eliteQualifyingMiles - flexEliteQualifyingMiles);
            if (isPresidentialPlusCard && trackingUpgradeType == 1 && flexEliteQualifyingMiles > 0)
            {
                pqm1.PremierQualifierTrackerCurrentText = string.Format("{0:###,##0}", eliteQualifyingMiles);
                pqm1.PremierQualifierTrackerCurrentFlexValue = Convert.ToString(flexEliteQualifyingMiles);
                pqm1.PremierQualifierTrackerCurrentFlexTitle = _configuration.GetValue<string>("flexPQMTitle") + " " + string.Format("{0:###,##0}", flexEliteQualifyingMiles);
            }
            else
            {
                pqm1.PremierQualifierTrackerCurrentText = string.Format("{0:###,##0}", eliteQualifyingMiles - flexEliteQualifyingMiles);
                pqm1.PremierQualifierTrackerCurrentFlexValue = "0";
            }
            thresholdMiles = GetValueFromList(lUpgradeTrackingLevels, "TrackerThresholdMiles");
            pqm1.PremierQualifierTrackerThresholdValue = thresholdMiles != string.Empty ? thresholdMiles : "0";
            pqm1.PremierQualifierTrackerThresholdText = thresholdMiles != string.Empty ? string.Format("{0:###,##0}", Convert.ToDecimal(thresholdMiles)) : "0";
            pqm1.PremierQualifierTrackerThresholdPrefix = "of";
            pqm1.Separator = "or";
            pqm1.IsWaived = false;

            return pqm1;
        }

        private string GetValueFromList(List<MOBKVP> list, string key)
        {
            string value = string.Empty;
            if (list != null && list.Count > 0 && list.Where(s => s.Key == key).SingleOrDefault() != null)
            {
                value = list.Where(s => s.Key == key).SingleOrDefault().Value ?? string.Empty;
            }
            return value;
        }

        private string GetValueFromList(List<MOBItem> list, string key)
        {
            string value = string.Empty;
            if (list != null && list.Count > 0 && list.Where(s => s.Id == key).SingleOrDefault() != null)
            {
                value = list.Where(s => s.Id == key).SingleOrDefault().CurrentValue ?? string.Empty;
            }
            return value;

        }

        private PremierQualifierTracker GetPQS(int trackingUpgradeType, List<MOBKVP> lUpgradeTrackingLevels, double eliteQualifyingPoints)
        {
            PremierQualifierTracker pqs1 = new PremierQualifierTracker();
            string thresholdSegments = string.Empty;
            pqs1.PremierQualifierTrackerTitle = _configuration.GetValue<string>("PremierQualifierTrackerSegmentsTitle");
            pqs1.PremierQualifierTrackerCurrentValue = Convert.ToString(eliteQualifyingPoints);
            pqs1.PremierQualifierTrackerCurrentText = string.Format("{0:0.#}", eliteQualifyingPoints);
            thresholdSegments = GetValueFromList(lUpgradeTrackingLevels, "TrackerThresholdSegments");
            pqs1.PremierQualifierTrackerThresholdValue = thresholdSegments != string.Empty ? thresholdSegments : "0";
            pqs1.PremierQualifierTrackerThresholdText = string.Format("{0:###,##0}", thresholdSegments != string.Empty ? thresholdSegments : "0");
            pqs1.PremierQualifierTrackerThresholdPrefix = "of";
            pqs1.Separator = trackingUpgradeType == 3 ? string.Empty : "+"; //for incremental upgrades hide "+"
            pqs1.IsWaived = false;

            return pqs1;
        }

        private PremierQualifierTracker GetPQD(int trackingUpgradeType, List<MOBKVP> lUpgradeTrackingLevels, double totalRevenue, bool isPresidentialPlusCard, bool isInternationalMember, bool isChaseCardSpendIndicator)
        {
            PremierQualifierTracker pqd1 = new PremierQualifierTracker();
            string thresholdDollars = string.Empty;
            pqd1.PremierQualifierTrackerTitle = _configuration.GetValue<string>("PremierQualifierTrackerDollarsTitle"); ;
            pqd1.PremierQualifierTrackerCurrentValue = Convert.ToString(totalRevenue);
            pqd1.PremierQualifierTrackerCurrentText = totalRevenue > 0 ? totalRevenue.ToString("C0") : "$0";
            pqd1.IsWaived = IsPQDWaived(trackingUpgradeType, isPresidentialPlusCard, isInternationalMember, isChaseCardSpendIndicator);
            thresholdDollars = GetValueFromList(lUpgradeTrackingLevels, "TrackerThresholdDollars");
            pqd1.PremierQualifierTrackerThresholdValue = thresholdDollars != string.Empty ? thresholdDollars : "0";
            pqd1.PremierQualifierTrackerThresholdText = pqd1.IsWaived == true ? _configuration.GetValue<string>("WaivedText") : (thresholdDollars != string.Empty ? Convert.ToDecimal(thresholdDollars).ToString("C0") : "$0");
            pqd1.PremierQualifierTrackerThresholdPrefix = pqd1.IsWaived == true ? string.Empty : "of";

            return pqd1;
        }

        private bool IsPQDWaived(int trackingUpgradeType, bool isPresidentialPlusCard, bool isInternationalMember, bool isChaseCardSpendIndicator)
        {
            bool isWaived = false;
            if (!_configuration.GetValue<bool>("EnableVBQII"))
            {
                if (trackingUpgradeType == 1 && (isInternationalMember || isPresidentialPlusCard || isChaseCardSpendIndicator))
                {
                    isWaived = true;
                }
                else if (trackingUpgradeType == 2 && isInternationalMember)
                {
                    isWaived = true;
                }
            }
            return isWaived;
        }

        private List<MOBKVP> GetUpgradeKeyValues(int trackingUpgradeType, bool isPresidentialPlusCard, string mileagePlusCardIndicator, string chaseCardSpendIndicator, double minimumSegmentsRequired, string rpcWaivedIndicator, bool isInternationalMember, string mpAccountNumber)
        {
            List<MOBKVP> lKeyValue = new List<MOBKVP>();
            string chaseOrPresCardKey = string.Empty;
            string chaseOrPresCardValue = string.Empty;
            if (!_configuration.GetValue<bool>("EnableVBQII"))
            {
                chaseOrPresCardKey = isPresidentialPlusCard ? _configuration.GetValue<string>("PresidentialPlusPQDWaiver") : _configuration.GetValue<string>("creditcardspendPQDWaiver");
                //For 1k Upgrade, always display "Not applicable"
                if (trackingUpgradeType == 2)
                {
                    chaseOrPresCardValue = _configuration.GetValue<string>("CreditCardSpendPQDNotapplicable");
                }
                else
                {
                    chaseOrPresCardValue = isPresidentialPlusCard ? !_configuration.GetValue<bool>("EnableVBQII") ? _configuration.GetValue<string>("PresidentialPlusPQDWaiverValue") : GetCreditCardSpendPQDWaiverText(mileagePlusCardIndicator, chaseCardSpendIndicator, trackingUpgradeType) : GetCreditCardSpendPQDWaiverText(mileagePlusCardIndicator, chaseCardSpendIndicator, trackingUpgradeType);
                }
            }
            switch (trackingUpgradeType)
            {
                case 1: //Platinum
                    {
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("MyAccountNextMilestone"), GetNextMilesStoneRPUGPUValue(trackingUpgradeType, mpAccountNumber)));
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("flightsegmentminimum"), Get4FlightSegmentMinimum(rpcWaivedIndicator, minimumSegmentsRequired)));
                        if (!_configuration.GetValue<bool>("EnableVBQII") && !isInternationalMember)
                        {
                            lKeyValue.Add(new MOBKVP(chaseOrPresCardKey, chaseOrPresCardValue));
                        }
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("viewpremierstatustracker"), _configuration.GetValue<string>("viewpremierstatustrackerlink")));
                        break;
                    }
                case 2: //1K
                    {
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("MyAccountNextMilestone"), GetNextMilesStoneRPUGPUValue(trackingUpgradeType, mpAccountNumber)));
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("flightsegmentminimum"), Get4FlightSegmentMinimum(rpcWaivedIndicator, minimumSegmentsRequired)));
                        if (!_configuration.GetValue<bool>("EnableVBQII") && !isInternationalMember)
                        {
                            lKeyValue.Add(new MOBKVP(chaseOrPresCardKey, chaseOrPresCardValue));
                        }
                        //lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("creditcardspendPQDWaiver"), GetCreditCardSpendPQDWaiverText(sResponse, trackingUpgradeType)));
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("viewpremierstatustracker"), _configuration.GetValue<string>("viewpremierstatustrackerlink")));
                        break;
                    }
                case 3://Incremental Upgrades
                    {
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("MyAccountNextMilestone"), GetNextMilesStoneRPUGPUValue(trackingUpgradeType, mpAccountNumber)));
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("viewpremierstatustracker"), _configuration.GetValue<string>("viewpremierstatustrackerlink")));
                        break;
                    }
                default:
                    break;
            }
            return lKeyValue;
        }

        private string Get4FlightSegmentMinimum(string rpcWaivedIndicator, double minSegmentRequired)
        {
            string value = string.Empty;
            if (rpcWaivedIndicator.ToUpper() == "N" || (_configuration.GetValue<bool>("EnableVBQII") && rpcWaivedIndicator.ToUpper() == "Y"))
            {
                value = minSegmentRequired >= 4 ? "4 of 4" : minSegmentRequired + " of 4";
            }
            else if (rpcWaivedIndicator.ToUpper() == "Y")
            {
                value = _configuration.GetValue<string>("WaivedText");
            }
            return value;
        }

        private string GetNextMilesStoneRPUGPUValue(int trackingUpgradeType, string mpAccountNumber)//EX: +2 RPU / +6 GPU or  +1 GPU or +2 RPU
        {
            switch (trackingUpgradeType)
            {
                case 1: //Platinum
                    {
                        return _configuration.GetValue<bool>("EnablePointsActivity") ? _configuration.GetValue<string>("PlatinumNextMilestonePlusPoints") : _configuration.GetValue<string>("PlatinumNextMilestone");
                    }
                case 2: //1K
                    {
                        return _configuration.GetValue<bool>("EnablePointsActivity") ? _configuration.GetValue<string>("1KNextMilestonePlusPoints") : _configuration.GetValue<string>("1KNextMilestone");
                    }
                case 3://Incremental Upgrades
                    {
                        return _configuration.GetValue<bool>("EnablePointsActivity") ? _configuration.GetValue<string>("IncrementalUpgradeNextMilestonePlusPoints") : _configuration.GetValue<string>("IncrementalUpgradeNextMilestone");
                    }
                default:
                    break;
            }
            return string.Empty;
        }

        private string GetCreditCardSpendPQDWaiverText(string mileagePlusCardIndicator, string chaseCardSpendIndicator, int trackingUpgradeType)
        {
            string value = string.Empty;
            if (trackingUpgradeType == 1)//Platinum Upgrade
            {
                if (mileagePlusCardIndicator != null && mileagePlusCardIndicator.ToUpper() == "Y")
                {
                    if (chaseCardSpendIndicator != null && chaseCardSpendIndicator.ToUpper() == "Y")
                    {
                        value = _configuration.GetValue<string>("CreditCardSpendPQDEligibleMet");
                    }
                    else
                    {
                        value = _configuration.GetValue<string>("CreditCardSpendPQDEligibleNotMet");
                    }
                }
                else
                {
                    value = _configuration.GetValue<string>("CreditCardSpendPQDNotEligible");
                }
            }
            return value;
        }

        private string GetCreditCardSpendPQDWaiverText(ServiceResponse serviceResponse)
        {
            string value = string.Empty;
            if (serviceResponse != null && serviceResponse.CurrentYearActivity != null)
            {
                if (serviceResponse.MileagePlusCardIndicator.ToUpper() == "N" &&
                    (serviceResponse.CurrentYearActivity.ChaseCardSpendIndicator == null || serviceResponse.CurrentYearActivity.ChaseCardSpendIndicator.ToUpper() == "N"))
                {
                    value = _configuration.GetValue<string>("CreditCardSpendPQDNotEligible");
                }
                else if (serviceResponse.MileagePlusCardIndicator.ToUpper() == "Y" &&
                    (serviceResponse.CurrentYearActivity.ChaseCardSpendIndicator == null || serviceResponse.CurrentYearActivity.ChaseCardSpendIndicator.ToUpper() == "N"))
                {
                    value = _configuration.GetValue<string>("CreditCardSpendPQDEligibleNotMet");
                }
                else if (serviceResponse.MileagePlusCardIndicator != null && serviceResponse.MileagePlusCardIndicator.ToUpper() == "Y" &&
                    serviceResponse.CurrentYearActivity.ChaseCardSpendIndicator != null && serviceResponse.CurrentYearActivity.ChaseCardSpendIndicator.ToUpper() == "Y")
                {
                    value = _configuration.GetValue<string>("CreditCardSpendPQDEligibleMet");
                }
            }
            return value;
        }

        private void GetIncrementalThresholdMilesAndSegments(long currentMiles, double segments, out long thresholdMiles, out long thresholdSegments)
        {
            thresholdSegments = 0;
            thresholdMiles = 0;
            long milesQuotient = 0;
            long segmentsQuotient = 0;
            try
            {
                long seg = Convert.ToInt64(segments);
                milesQuotient = currentMiles > 0 ? currentMiles / 25000 : 0;
                segmentsQuotient = seg > 0 ? seg / 30 : 0;
                if (milesQuotient >= segmentsQuotient)
                {
                    thresholdMiles = (milesQuotient * 25000) + 25000;
                    thresholdSegments = (milesQuotient * 30) + 30;
                }
                else
                {
                    thresholdMiles = (segmentsQuotient * 25000) + 25000;
                    thresholdSegments = (segmentsQuotient * 30) + 30;
                }
            }
            catch (Exception)
            {
            }
        }

        private int GetTrackingUpgradeType(ServiceResponse sResponse)
        {
            int trackingUpgradeType = 0;//1=Platinum Upgrade; 2=1k Upgrade; 3= Incremental Upgrade More Comments to understand whats this "trackingUpgradeType" use for
            if (sResponse != null && sResponse.CurrentYearActivity != null)
            {
                trackingUpgradeType = IsMPUpgradeEligible(sResponse.TrackingLevel, sResponse.CurrentPremierLevel, sResponse.InfiniteLevel, sResponse.LifetimeMiles,
                                        sResponse.CurrentYearActivity.EliteQualifyingMiles, sResponse.CurrentYearActivity.EliteQualifyingPoints, sResponse.CurrentYearActivity.TotalRevenue)
                                        ?
                    GetTrackingUpgradeType(sResponse.CurrentYearActivity.EliteQualifyingMiles, sResponse.CurrentYearActivity.FlexEliteQualifyingMiles, sResponse.CurrentYearActivity.EliteQualifyingPoints,
                        sResponse.CurrentYearActivity.TotalFlightRevenue, sResponse.CurrentYearActivity.MinimumSegmentsRequired, IsPresidentialPlusCard(sResponse.CurrentYearActivity.PresidentPlusIndicator),
                        IsInternationalMember(sResponse.CurrentYearActivity.DomesticIndicator), IsChaseCardSpendIndicator(sResponse.MileagePlusCardIndicator, sResponse.CurrentYearActivity.ChaseCardSpendIndicator),
                        Is4FlightSegmentWaived(sResponse.CurrentYearActivity.RpcWaivedIndicator)) : 0;
            }
            return trackingUpgradeType;
        }

        private bool IsMPUpgradeEligible(int trackingMPLevel, int currentPremierLevel, int infiniteLevel, long lifeTimeMiles, long pqm, double pqs, double pqd)
        {
            if (trackingMPLevel == 6 && (currentPremierLevel == 4 || currentPremierLevel == 5) &&
                (infiniteLevel == 4 || infiniteLevel == 5 || lifeTimeMiles >= 3000000 || ((pqm >= 100000 || pqs >= 120) && pqd >= 15000)))
            {
                return true;
            }
            return false;
        }

        private bool Is4FlightSegmentWaived(string rpcWaivedIndicator)
        {
            if (!_configuration.GetValue<bool>("EnableVBQII") && !string.IsNullOrEmpty(rpcWaivedIndicator) && rpcWaivedIndicator.ToUpper() == "Y")
            {
                return true;
            }
            return false;
        }

        private int GetTrackingUpgradeType(long eliteQualifyingMiles, long flexEliteQualifyingMiles, double pqs, double pqd, double minSegmentRequired, bool isPresidentialPlusCard, bool isInternationalMember, bool isChaseCardSpendIndicator, bool is4FlightSegmentWaived)
        {
            long pqm = eliteQualifyingMiles - flexEliteQualifyingMiles;

            if ((minSegmentRequired >= 4 || is4FlightSegmentWaived) && (pqm >= 100000 || pqs >= 120) && (pqd >= 15000 || isInternationalMember))
            {
                return 3;
            }
            else if ((minSegmentRequired >= 4 || is4FlightSegmentWaived) && ((pqm >= 75000 && pqm < 100000) || (pqm < 75000 && eliteQualifyingMiles >= 75000) || (pqs >= 90 && pqs < 120))
               && (pqd >= 9000 || IsPQDWaived(isPresidentialPlusCard, isInternationalMember, isChaseCardSpendIndicator))) //1K
            {
                return 2;
            }
            // When user has < 4 Minimum Segments Flown or user has <75K PQM AND (<$9,000 PQD OR PQD Waiver) or 
            // user has<90 PQS AND(<$9,000 PQD OR PQD Waiver) -- Falls in Platinum Upgrade
            else if (minSegmentRequired < 4 ||
                ((eliteQualifyingMiles < 75000 || pqs < 90) && (pqd < 9000 || IsPQDWaived(isPresidentialPlusCard, isInternationalMember, isChaseCardSpendIndicator)))
               ) // For Platinum upgrades consider Flex miles
            {
                return 1; //Platinum upgrade
            }

            return 0;
        }

        private bool IsPQDWaived(bool isPresidentialPlusCard, bool isInternationalMember, bool isChaseCardSpendIndicator)
        {
            bool isWaived = false;
            if (isInternationalMember || isPresidentialPlusCard || isChaseCardSpendIndicator)
            {
                isWaived = true;
            }
            return isWaived;
        }

        public async Task<bool> GetProfile_AllTravelerData(string mileagePlusNumber, string transactionId, string dpToken, int applicationId, string appVersion, string deviceId)
        {
            Services.Customer.Common.ProfileRequest profileRequest = new Services.Customer.Common.ProfileRequest();
            bool isTSAFlagON = false;
            profileRequest.LoyaltyId = mileagePlusNumber;
            profileRequest.RefreshCache = false;
            profileRequest.DataSetting = _configuration.GetValue<string>("CustomerDBDataSettingForCSLServices");
            profileRequest.LangCode = "en-US";
            List<string> requestStringList = new List<string>();
            requestStringList.Add("AllTravelerData");
            requestStringList.Add("SecureTravelers");
            profileRequest.DataToLoad = requestStringList;
            try
            {
                string jsonRequest = DataContextJsonSerializer.Serialize<United.Services.Customer.Common.ProfileRequest>(profileRequest);

                var customerDataResponse = await _customerDataService.GetCustomerData<Services.Customer.Common.ProfileResponse>(dpToken, _headers.ContextValues.SessionId, jsonRequest).ConfigureAwait(false) ;

                var response = customerDataResponse.response;
                if (response != null)
                {
                    if (response?.Profiles?.FirstOrDefault()?.Travelers?.Count > 0)
                    {
                        foreach (var traveler in response?.Profiles?.FirstOrDefault()?.Travelers)
                        {
                            if (traveler.MileagePlusId?.ToUpper() == mileagePlusNumber?.ToUpper())
                            {
                                foreach (var secureTraveler in traveler.SecureTravelers)
                                {
                                    if (secureTraveler?.DocumentType?.Trim()?.ToUpper() == "U")
                                    {
                                        isTSAFlagON = true;
                                        return isTSAFlagON;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response != null)
                {
                    throw new Exception(wex.Message);
                }
            }
            return isTSAFlagON;
        }

        public async Task<MPAccountSummary> GetAccountSummaryWithPremierActivity(MPAccountValidationRequest req, bool includeMembershipCardBarCode, string dpToken)
        {
            MPAccountSummary mpSummary = new MPAccountSummary();
            AccountProfileInfoResponse loyaltyProfileResponse = new AccountProfileInfoResponse();

            loyaltyProfileResponse =await _loyaltyAccountService.GetAccountSummary<AccountProfileInfoResponse>(dpToken, req.MileagePlusNumber, _headers.ContextValues.TransactionId).ConfigureAwait(false);

            try
            {
                bool fourSegmentMinimunWaivedMember = false;

                string balanceExpireDisclaimer = string.Empty;
                bool noMileageExpiration = false;
                ServiceResponse sResponse = new ServiceResponse();

                #region 55359, 81220 Bug Fix
                if (loyaltyProfileResponse != null && loyaltyProfileResponse.AccountProfileInfo != null && (loyaltyProfileResponse.AccountProfileInfo.IsClosedTemporarily || loyaltyProfileResponse.AccountProfileInfo.IsClosedPermanently || loyaltyProfileResponse.AccountProfileInfo.IsClosed))
                {
                    string exceptionMessage = _configuration.GetValue<string>("ErrorContactMileagePlus").ToString();
                    throw new MOBUnitedException(exceptionMessage);
                }
                #endregion 55359, 81220 Bug Fix
                mpSummary.Balance = loyaltyProfileResponse.AccountProfileInfo.CurrentBalance.ToString();
                mpSummary.LastActivityDate = loyaltyProfileResponse.AccountProfileInfo.LastActivityDate != 0 ? DateTime.ParseExact(loyaltyProfileResponse.AccountProfileInfo.LastActivityDate.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy") : "";
                mpSummary.LearnMoreTitle = _configuration.GetValue<string>("MileagePluslearnMoreText");
                mpSummary.LearnMoreHeader = _configuration.GetValue<string>("MileagePluslearnMoreDesc");
                mpSummary.MilesNeverExpireText = _configuration.GetValue<string>("MileagePlusMilesNeverExpire");
                if (_configuration.GetValue<bool>("HideMileageBalanceExpireDate") == true)
                {
                    mpSummary.BalanceExpireDate = "";
                    mpSummary.IsHideMileageBalanceExpireDate = true;
                }
                else
                {
                    mpSummary.BalanceExpireDate = loyaltyProfileResponse.AccountProfileInfo.MilesExpireDate != 0 ? DateTime.ParseExact(loyaltyProfileResponse.AccountProfileInfo.MilesExpireDate.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy") : "";
                }
                mpSummary.BalanceExpireDisclaimer = HttpUtility.HtmlDecode(_configuration.GetValue<bool>("HideMileageBalanceExpireDate") ? _configuration.GetValue<string>("BalanceExpireDisclaimerNeverExpire") : _configuration.GetValue<string>("BalanceExpireDisclaimer"));
                //dotOnTimeMessages = HttpUtility.HtmlDecode(dotOnTimeMessages);
                mpSummary.CustomerId = loyaltyProfileResponse.AccountProfileInfo.CustomerId;
                mpSummary.EliteMileage = string.Format("{0:###,##0}", loyaltyProfileResponse.AccountProfileInfo.PremierQualifyingMilesBalance);
                if (loyaltyProfileResponse.AccountProfileInfo.PremierQualifyingSegmentBalance == 0)
                {
                    mpSummary.EliteSegment = "0";
                }
                else
                {
                    mpSummary.EliteSegment = string.Format("{0:0.#}", loyaltyProfileResponse.AccountProfileInfo.PremierQualifyingSegmentBalance);
                }

                mpSummary.EliteStatus = new MOBEliteStatus(_configuration);
                mpSummary.EliteStatus.Code = loyaltyProfileResponse.AccountProfileInfo.EliteLevel;
                mpSummary.EnrollDate = loyaltyProfileResponse.AccountProfileInfo.EnrollDate.ToString("MM/dd/yyyy");
                mpSummary.LastFlightDate = (loyaltyProfileResponse.AccountProfileInfo.LastFlightDate != 0) ? DateTime.ParseExact(loyaltyProfileResponse.AccountProfileInfo.LastFlightDate.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy") : "";
                mpSummary.MileagePlusNumber = loyaltyProfileResponse.AccountProfileInfo.AccountId;
                mpSummary.Name = new MOBName();
                mpSummary.Name.First = loyaltyProfileResponse.AccountProfileInfo.FirstName;
                mpSummary.Name.Last = loyaltyProfileResponse.AccountProfileInfo.LastName;
                mpSummary.Name.Middle = loyaltyProfileResponse.AccountProfileInfo.MiddleName;
                mpSummary.Name.Suffix = loyaltyProfileResponse.AccountProfileInfo.Suffix;
                mpSummary.Name.Title = loyaltyProfileResponse.AccountProfileInfo.Title;
                mpSummary.IsCEO = loyaltyProfileResponse.AccountProfileInfo.IsCeo;
                mpSummary.LifetimeMiles = loyaltyProfileResponse.AccountProfileInfo.LifetimeMiles;

                if (loyaltyProfileResponse.AccountProfileInfo.MillionMilerLevel == 0)
                {
                    mpSummary.MillionMilerIndicator = string.Empty;
                }
                else
                {
                    mpSummary.MillionMilerIndicator = loyaltyProfileResponse.AccountProfileInfo.MillionMilerLevel.ToString();
                }

                if (Convert.ToDateTime(_configuration.GetValue<string>("MP2014EnableDate")) < DateTime.Now)
                {
                    if (loyaltyProfileResponse != null && loyaltyProfileResponse.AccountProfileInfo != null)
                    {
                        bool isValidPqdAddress = false;
                        bool activeNonPresidentialPlusCardMember = false;
                        bool activePresidentialPlusCardMembe = false;
                        bool showChaseBonusTile = true;

                        //Migrate XML to CSL service call
                        //[CLEANUP API-MIGRATION] 
                        if (_configuration.GetValue<bool>("NewServieCall_GetProfile_PaymentInfos"))
                        {
                           var tupleRes= await IsValidPQDAddressV2("GetAccountSummaryWithPremierActivity", req.TransactionId, dpToken, mpSummary.MileagePlusNumber,  isValidPqdAddress,  activeNonPresidentialPlusCardMember,  activePresidentialPlusCardMembe,  fourSegmentMinimunWaivedMember,  showChaseBonusTile);
                            isValidPqdAddress = tupleRes.isValidPqdAddress;
                             activeNonPresidentialPlusCardMember = tupleRes.activeNonPresidentialPlusCardMember;
                             activePresidentialPlusCardMembe = tupleRes.activePresidentialPlusCardMembe;
                             showChaseBonusTile = tupleRes.chaseBonusTile;
                            fourSegmentMinimunWaivedMember = tupleRes.fourSegmentMinimunWaivedMember;
                        }

                        mpSummary.ShowChaseBonusTile = showChaseBonusTile;
                        AssignCustomerChasePromoType(mpSummary, showChaseBonusTile);

                        noMileageExpiration = activeNonPresidentialPlusCardMember || activePresidentialPlusCardMembe;

                        if (fourSegmentMinimunWaivedMember)
                        {
                            mpSummary.FourSegmentMinimun = "Waived";
                        }
                        else if (loyaltyProfileResponse.AccountProfileInfo.MinimumSegment >= 4)
                        {
                            mpSummary.FourSegmentMinimun = "4 of 4";
                        }
                        else
                        {
                            mpSummary.FourSegmentMinimun = string.Format("{0} of 4", loyaltyProfileResponse.AccountProfileInfo.MinimumSegment);
                        }

                        if (loyaltyProfileResponse.AccountProfileInfo.PremierQualifyingDollars == 0 || !isValidPqdAddress)
                        {
                            if (!isValidPqdAddress)
                            {
                                mpSummary.PremierQualifyingDollars = string.Empty;
                            }
                            else
                            {
                                mpSummary.PremierQualifyingDollars = "0";
                            }
                        }
                        else
                        {
                            decimal pqd = 0;
                            try
                            {
                                pqd = Convert.ToDecimal(loyaltyProfileResponse.AccountProfileInfo.PremierQualifyingDollars);
                            }
                            catch (System.Exception) { }
                            //Below are the two toggles used in Appsettings 
                            //< add key = "PqdAmount" value = "12000" /> < add key = "PqdText" value = "Over $12,000" />
                            //Work Items LOYAL-3236, LOYAL-3241
                            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("PqdAmount")) && !string.IsNullOrEmpty(_configuration.GetValue<string>("PqdText")))
                            {
                                if (pqd > Convert.ToDecimal(_configuration.GetValue<string>("PqdAmount")))
                                {
                                    mpSummary.PremierQualifyingDollars = _configuration.GetValue<string>("PqdText");
                                }
                            }
                            else
                            {
                                mpSummary.PremierQualifyingDollars = pqd.ToString("C0");
                            }
                        }
                        //to be removed
                        string pdqchasewaiverLabel = string.Empty;
                        string pdqchasewavier = string.Empty;
                        if (isValidPqdAddress)
                        {
                            if (!string.IsNullOrEmpty(loyaltyProfileResponse.AccountProfileInfo.ChaseSpendingIndicator) && loyaltyProfileResponse.AccountProfileInfo.ChaseSpendingIndicator.Equals("Y"))
                            {
                            }
                            if (!string.IsNullOrEmpty(loyaltyProfileResponse.AccountProfileInfo.PresidentialPlusIndicator) && loyaltyProfileResponse.AccountProfileInfo.PresidentialPlusIndicator.Equals("Y"))
                            {
                            }
                            //[CLEANUP API-MIGRATION] Removed SOAP Call
                            //if (!_configuration.GetValue<bool>("IsAvoidUAWSChaseMessagingSoapCall"))
                            //{
                            //    GetChaseMessage(activeNonPresidentialPlusCardMember, activePresidentialPlusCardMembe, chaseSpending, presidentialPlus, ref pdqchasewaiverLabel, ref pdqchasewavier, ref balanceExpireDisclaimer);
                            //    mpSummary.PDQchasewaiverLabel = pdqchasewaiverLabel;
                            //    mpSummary.PDQchasewavier = pdqchasewavier;
                            //}

                            if (!string.IsNullOrEmpty(balanceExpireDisclaimer) && !_configuration.GetValue<bool>("HideMileageBalanceExpireDate"))
                            {
                                mpSummary.BalanceExpireDisclaimer = mpSummary.BalanceExpireDisclaimer + " " + balanceExpireDisclaimer;
                            }
                        }
                        //End to be removed
                        //GetPremierActivity
                        //long l = 0;
                        //l = GetIncrementalThresholdMiles(121000);
                        sResponse = await GetActivityFromQualificationService(req, dpToken);
                        if (sResponse != null && sResponse.Status != null && !string.IsNullOrEmpty(sResponse.Status.Code) && sResponse.Status.Code.Trim().ToUpper() == "E0000")
                        {
                            //if ((!string.IsNullOrEmpty(sResponse.RunType) && sResponse.RunType.Trim().ToUpper() == "QL") || ShowActivity(req.MileagePlusNumber))
                            //if (!string.IsNullOrEmpty(sResponse.RunType) && sResponse.RunType.Trim().ToUpper() == "QL")
                            if (!string.IsNullOrEmpty(sResponse.YearEndIndicator) && sResponse.YearEndIndicator.Trim().ToUpper() == "Y")
                            {
                                mpSummary.yearEndPremierActivity = GetYearEndActivity(sResponse);
                                mpSummary.PremierActivityType = 2;//YearEndActivity
                            }
                            else
                            {
                                if (_configuration.GetValue<bool>("ImplementForceUpdateLogicToggleAccountSummary") && !GeneralHelper.IsApplicationVersionGreater(req.Application.Id, req.Application.Version.Major, "ImplementForceUpdateLogicToggleAccountSummaryIOS", "ImplementForceUpdateLogicToggleAccountSummaryAndroid", "", "", true, _configuration))
                                {
                                    mpSummary.PremierActivityType = 3;//ErrorActivity
                                    mpSummary.ErrorPremierActivity = new ErrorPremierActivity()
                                    {
                                        ShowErrorIcon = true,
                                        ErrorPremierActivityTitle = _configuration.GetValue<string>("PremierActivityTitle"),
                                        ErrorPremierActivityText = _configuration.GetValue<string>("MyAccountForceUpdateMessage")
                                    };
                                    mpSummary.PremierStatusTrackerText = _configuration.GetValue<string>("viewpremierstatustracker");
                                    mpSummary.PremierStatusTrackerLink = _configuration.GetValue<string>("viewpremierstatustrackerlink");
                                    var captionsForError = await GetPremierActivityLearnAboutCaptions();
                                    if (captionsForError != null && captionsForError.Count > 0)
                                    {
                                        mpSummary.PremierTrackerLearnAboutTitle = GetValueFromList(captionsForError, "PremierTrackerLearnAboutTitle");
                                        mpSummary.PremierTrackerLearnAboutHeader = GetValueFromList(captionsForError, "PremierTrackerLearnAboutHeader");
                                        mpSummary.PremierTrackerLearnAboutText = GetValueFromList(captionsForError, "PremierTrackerLearnAboutText");
                                        mpSummary.PremierTrackerLearnAboutText = mpSummary.PremierTrackerLearnAboutText + GetValueFromList(captionsForError, "FullTermsAndConditions");
                                    }
                                    //if (!string.IsNullOrEmpty(_configuration.GetValue<string>("ShowWelcomeModel")) && _configuration.GetValue<string>("ShowWelcomeModel") && !Utility.IsVBQWelcomeModelDisplayed(req.MileagePlusNumber, req.Application.Id, req.DeviceId))
                                    //{
                                    //    mpSummary.VBQWelcomeModel = new MOBVBQWelcomeModel();
                                    //}
                                    mpSummary.NoMileageExpiration = noMileageExpiration.ToString();
                                    if (_configuration.GetValue<bool>("HideMileageBalanceExpireDate"))
                                    {
                                        mpSummary.NoMileageExpirationMessage = HttpUtility.HtmlDecode(_configuration.GetValue<string>("BalanceExpireDisclaimerNeverExpire"));
                                    }
                                    else if (noMileageExpiration)
                                    {
                                        mpSummary.NoMileageExpirationMessage = HttpUtility.HtmlDecode(_configuration.GetValue<string>("ChaseNoMileageExpirationMessage") + " " + balanceExpireDisclaimer);
                                        if (!fourSegmentMinimunWaivedMember)
                                        {
                                            mpSummary.NoMileageExpirationMessage = HttpUtility.HtmlDecode(mpSummary.NoMileageExpirationMessage + " " + _configuration.GetValue<string>("FouSegmentMessage"));
                                        }
                                    }
                                    return mpSummary;
                                }
                                int trackingUpgradeType = 0;
                                if (_configuration.GetValue<bool>("EnableVBQII") && IsVBQTrackerSupportedVersion(req.Application.Id, req.Application.Version.Major))
                                {
                                    trackingUpgradeType = GetVBQTrackingUpgradeType(sResponse);
                                    if (trackingUpgradeType > 0)
                                    {
                                        mpSummary.vBQPremierActivity = GetVBQTrackingUpgradesActivity(sResponse, trackingUpgradeType);
                                    }
                                    else
                                    {
                                        mpSummary.vBQPremierActivity = GetVBQPremierActivity(sResponse);
                                    }
                                }
                                else
                                {
                                    if (_configuration.GetValue<bool>("MyAccountEnableUpgrades") &&
                                     IsPremierStatusTrackerUpgradesSupportedVersion(req.Application.Id, req.Application.Version.Major))
                                    {
                                        trackingUpgradeType = GetTrackingUpgradeType(sResponse);
                                        if (trackingUpgradeType > 0)
                                        {
                                            mpSummary.premierActivity = GetTrackingUpgradesActivity(sResponse, trackingUpgradeType);
                                        }
                                        else
                                        {
                                            mpSummary.premierActivity = GetPremierActivity(sResponse, ref pdqchasewaiverLabel, ref pdqchasewavier);
                                        }
                                    }
                                }
                                if (trackingUpgradeType == 3)
                                {
                                    mpSummary.IsIncrementalUpgrade = true;
                                }

                                mpSummary.PremierActivityType = 1;//PremierActivity
                            }
                        }
                        else
                        {
                            mpSummary.PremierActivityType = 3;//ErrorActivity
                                                              //if (!string.IsNullOrEmpty(_configuration.GetValue<string>("ShowWelcomeModel")) && _configuration.GetValue<string>("ShowWelcomeModel") && !Utility.IsVBQWelcomeModelDisplayed(req.MileagePlusNumber, req.Application.Id, req.DeviceId))
                                                              //{
                                                              //    mpSummary.VBQWelcomeModel = new MOBVBQWelcomeModel();
                                                              //}
                            mpSummary.ErrorPremierActivity = GetErrorPremierActivity(sResponse);
                        }
                        //Display PremierActivity for older versions all the time
                        if ((mpSummary.PremierActivityType == 2 || mpSummary.PremierActivityType == 3) && IsPremierStatusTrackerSupportedVersion(req.Application.Id, req.Application.Version.Major) == false)
                        {
                            mpSummary.premierActivity = GetPremierActivity(sResponse, ref pdqchasewaiverLabel, ref pdqchasewavier);
                        }
                        var captions = await GetPremierActivityLearnAboutCaptions();
                        if (captions != null && captions.Count > 0)
                        {
                            mpSummary.PremierTrackerLearnAboutTitle = GetValueFromList(captions, "PremierTrackerLearnAboutTitle");
                            mpSummary.PremierTrackerLearnAboutHeader = GetValueFromList(captions, "PremierTrackerLearnAboutHeader");
                            mpSummary.PremierTrackerLearnAboutText = GetValueFromList(captions, "PremierTrackerLearnAboutText");
                            if (sResponse.CurrentYearActivity.FlexPremierQualifyingPoints > 0)
                            {
                                mpSummary.PremierTrackerLearnAboutText = mpSummary.PremierTrackerLearnAboutText + GetValueFromList(captions, "ChasePQPDescription");

                            }
                            mpSummary.PremierTrackerLearnAboutText = mpSummary.PremierTrackerLearnAboutText + GetValueFromList(captions, "FullTermsAgndConditions");
                        }

                        mpSummary.PremierStatusTrackerText = _configuration.GetValue<string>("viewpremierstatustracker");
                        mpSummary.PremierStatusTrackerLink = _configuration.GetValue<string>("viewpremierstatustrackerlink");
                        mpSummary.PlusPoints = _configuration.GetValue<bool>("EnablePlusPointsSummary") ? GetPlusPointsFromLoyaltyBalanceService(req, dpToken).Result : null;
                        if (!fourSegmentMinimunWaivedMember && !_configuration.GetValue<bool>("HideMileageBalanceExpireDate"))
                        {
                            mpSummary.BalanceExpireDisclaimer = mpSummary.BalanceExpireDisclaimer + " " + HttpUtility.HtmlDecode(_configuration.GetValue<string>("FouSegmentMessage"));
                        }
                    }
                }

                if (includeMembershipCardBarCode)
                {
                    //%B[\w]{2,3}\d{5,6}\s{7}\d{4}(GL|1K|GS|PL|SL|\s\s)\s\s[\s\w\<\-\'\.]{35}\sUA
                    string eliteLevel = "";
                    switch (mpSummary.EliteStatus.Level)
                    {
                        case 0:
                            eliteLevel = "  ";
                            break;
                        case 1:
                            eliteLevel = "SL";
                            break;
                        case 2:
                            eliteLevel = "GL";
                            break;
                        case 3:
                            eliteLevel = "PL";
                            break;
                        case 4:
                            eliteLevel = "1K";
                            break;
                        case 5:
                            eliteLevel = "GS";
                            break;
                    }
                    string name = string.Format("{0}<{1}", mpSummary.Name.Last, mpSummary.Name.First);
                    if (!string.IsNullOrEmpty(mpSummary.Name.Middle))
                    {
                        name = name + " " + mpSummary.Name.Middle.Substring(0, 1);
                    }
                    name = String.Format("{0, -36}", name);

                    bool hasUnitedClubMemberShip = false;
                    mpSummary.uAClubMemberShipDetails = await GetUnitedClubMembershipDetails(mpSummary.MileagePlusNumber, hasUnitedClubMemberShip, req.LanguageCode);
                    mpSummary.HasUAClubMemberShip = hasUnitedClubMemberShip;
                    mpSummary.MembershipCardExpirationDate = await this.GetMembershipCardExpirationDate(mpSummary.MileagePlusNumber, mpSummary.EliteStatus.Level);

                    bool Keep_MREST_MP_EliteLevel_Expiration_Logic = _configuration.GetValue<bool>("Keep_MREST_MP_EliteLevel_Expiration_Logic");
                    string expirationDate = _configuration.GetValue<string>("MPCardExpirationDate");
                    try
                    {
                        if (!Keep_MREST_MP_EliteLevel_Expiration_Logic && sResponse != null)
                        {
                            #region
                            if (!_configuration.GetValue<bool>("Reset_MembershipCardExpirationDate"))
                            {
                                mpSummary.MembershipCardExpirationDate = string.Empty; // Always return the expiration date based on Premier QUAL service #LOYAL-6376
                            }
                            if (sResponse != null && sResponse.CurrentPremierLevel > 0)
                            {
                                DateTime dDate;
                                DateTime.TryParse(_configuration.GetValue<string>("DefaultMileagePlusCardExpirationDateReturnedByQaulSerivceForGeneralMember"), out dDate); //  <add key="DefaultMileagePlusCardExpirationDateReturnedByQaulSerivceForGeneralMember" value="1753-01-01"/>
                                if (sResponse.CurrentPremierLevelExpirationDate.Equals(dDate))
                                {
                                    if (sResponse.CurrentYearInstantElite != null &&
                                        !string.IsNullOrEmpty(sResponse.CurrentYearInstantElite.ConsolidatedCode) &&
                                       sResponse.CurrentYearInstantElite.ExpirationDate != null &&
                                     (sResponse.CurrentYearInstantElite.ConsolidatedCode.ToUpper().Trim() == "TRIAL"))
                                    {
                                        expirationDate = GetMPExpirationDateFromProfileAndQualService(mpSummary, string.Empty, sResponse.CurrentYearInstantElite.ConsolidatedCode, Convert.ToString(sResponse.CurrentYearInstantElite.ExpirationDate), expirationDate);
                                    }
                                }
                                else
                                {
                                    string currentPremierLevelExpirationDate = sResponse.CurrentPremierLevelExpirationDate != null ? Convert.ToString(sResponse.CurrentPremierLevelExpirationDate) : string.Empty;
                                    string consolidatedCode = sResponse.CurrentYearInstantElite != null && !string.IsNullOrEmpty(sResponse.CurrentYearInstantElite.ConsolidatedCode) ? sResponse.CurrentYearInstantElite.ConsolidatedCode : string.Empty;
                                    string currentYearInstantEliteExpirationDate = sResponse.CurrentYearInstantElite != null && sResponse.CurrentYearInstantElite.ExpirationDate != null ? Convert.ToString(sResponse.CurrentYearInstantElite.ExpirationDate) : string.Empty;
                                    bool isPermierLevelExpirationDateGreaterThanCurrentDate = !string.IsNullOrEmpty(currentPremierLevelExpirationDate) ? sResponse.CurrentPremierLevelExpirationDate > DateTime.Now : false;
                                    if (!isPermierLevelExpirationDateGreaterThanCurrentDate)
                                    {
                                        currentPremierLevelExpirationDate = string.Empty;
                                    }
                                    expirationDate = GetMPExpirationDateFromProfileAndQualService(mpSummary, currentPremierLevelExpirationDate, consolidatedCode, currentYearInstantEliteExpirationDate, expirationDate);
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            mpSummary.MembershipCardExpirationDate = await this.GetMembershipCardExpirationDate(mpSummary.MileagePlusNumber, mpSummary.EliteStatus.Level);
                            if (mpSummary.MembershipCardExpirationDate.Equals("Trial Status"))
                            {
                                expirationDate = string.Format("12/{0}", DateTime.Today.Year);
                            }
                            else
                            {
                                expirationDate = string.IsNullOrEmpty(mpSummary.MembershipCardExpirationDate) ? _configuration.GetValue<string>("MPCardExpirationDate") : mpSummary.MembershipCardExpirationDate.Substring(11, 2) + mpSummary.MembershipCardExpirationDate.Substring(16, 2);
                            }
                        }
                    }
                    catch (System.Exception) { }

                    string barCodeData = string.Format("%B{0}       {1}{2}  {3}UA              ?", mpSummary.MileagePlusNumber, expirationDate, eliteLevel, name);
                    string allianceTierLevel = "   ";
                    switch (mpSummary.EliteStatus.Level)
                    {
                        case 0:
                            allianceTierLevel = "   ";
                            break;
                        case 1:
                            allianceTierLevel = "UAS";
                            break;
                        case 2:
                            allianceTierLevel = "UAG";
                            break;
                        case 3:
                            allianceTierLevel = "UAG";
                            break;
                        case 4:
                            allianceTierLevel = "UAG";
                            break;
                        case 5:
                            allianceTierLevel = "UAG";
                            break;
                    }
                    string allianceTierLevelExpirationDate = "    ";
                    if (!allianceTierLevel.Equals("   "))
                    {
                        if (!Keep_MREST_MP_EliteLevel_Expiration_Logic && sResponse != null)
                        {
                            allianceTierLevelExpirationDate = expirationDate;
                        }
                        else
                        {
                            if (mpSummary.MembershipCardExpirationDate.Equals("Trial Status"))
                            {
                                allianceTierLevelExpirationDate = string.Format("12/{0}", DateTime.Today.Year);
                            }
                            else
                            {
                                allianceTierLevelExpirationDate = string.IsNullOrEmpty(mpSummary.MembershipCardExpirationDate) ? _configuration.GetValue<string>("MPCardExpirationDate") : mpSummary.MembershipCardExpirationDate.Substring(11, 2) + mpSummary.MembershipCardExpirationDate.Substring(16, 2);
                            }
                        }
                    }

                    string paidLoungeIndicator = "N";
                    string paidLoungeExpireationDate = "      ";
                    if (mpSummary.uAClubMemberShipDetails != null)
                    {
                        if (string.IsNullOrEmpty(mpSummary.uAClubMemberShipDetails.PrimaryOrCompanion))
                        {
                            paidLoungeIndicator = "P";
                        }
                        else
                        {
                            paidLoungeIndicator = mpSummary.uAClubMemberShipDetails.PrimaryOrCompanion;
                        }
                        paidLoungeExpireationDate = Convert.ToDateTime(mpSummary.uAClubMemberShipDetails.DiscontinueDate).ToString("ddMMyyyy");
                    }

                    string starBarCodeData = string.Format("FFPC001UA{0}{1}{2}{3}{4}{5}{6}{7}^001", mpSummary.MileagePlusNumber.PadRight(16), mpSummary.Name.Last.PadRight(20), mpSummary.Name.First.PadRight(20), allianceTierLevel, allianceTierLevelExpirationDate, eliteLevel, paidLoungeIndicator, paidLoungeExpireationDate);

                    if (!string.IsNullOrEmpty(_configuration.GetValue<string>("ReturnMPMembershipBarcode")) && Convert.ToBoolean(_configuration.GetValue<string>("ReturnMPMembershipBarcode")))
                    {
                        if (!string.IsNullOrEmpty(_configuration.GetValue<string>("UseStarMembershipCardFormatDateTime")) && Convert.ToDateTime(_configuration.GetValue<string>("UseStarMembershipCardFormatDateTime")) <= DateTime.Now)
                        {
                            mpSummary.MembershipCardBarCodeString = starBarCodeData.ToUpper();
                            mpSummary.MembershipCardBarCode = GetBarCode(starBarCodeData);
                        }
                        else
                        {
                            mpSummary.MembershipCardBarCodeString = barCodeData.ToUpper();
                            mpSummary.MembershipCardBarCode = GetBarCode(barCodeData);
                        }
                    }
                    else
                    {
                        mpSummary.MembershipCardBarCodeString = null;
                        mpSummary.MembershipCardBarCode = null;
                    }
                }

                mpSummary.NoMileageExpiration = noMileageExpiration.ToString();
                if (_configuration.GetValue<bool>("HideMileageBalanceExpireDate"))
                {
                    mpSummary.NoMileageExpirationMessage = HttpUtility.HtmlDecode(_configuration.GetValue<string>("BalanceExpireDisclaimerNeverExpire"));
                }
                else if (noMileageExpiration)
                {
                    mpSummary.NoMileageExpirationMessage = HttpUtility.HtmlDecode(_configuration.GetValue<string>("ChaseNoMileageExpirationMessage") + " " + balanceExpireDisclaimer);

                    if (!fourSegmentMinimunWaivedMember)
                    {
                        mpSummary.NoMileageExpirationMessage = HttpUtility.HtmlDecode(mpSummary.NoMileageExpirationMessage + " " + _configuration.GetValue<string>("FouSegmentMessage"));
                    }
                }

                mpSummary.TravelCreditInfo = await GetTravelCreditDetail(req.MileagePlusNumber, req.Application, req.SessionId, req.TransactionId, req.DeviceId);
            }
            catch (MOBUnitedException ex)
            {
                throw new MOBUnitedException(ex.Message);
            }
            catch (System.Exception ex)
            {
                throw new System.Exception(ex.Message);
            }
            finally
            {
                try
                {
                    //if (response != null)
                    //{
                    //    response.Close();
                    //}
                }
                catch
                {
                    throw new System.Exception("United Data Services Not Available");
                }
            }

            _logger.LogInformation("Loyalty Get Profile Response to client {@MpSummary}", JsonConvert.SerializeObject(mpSummary));

            if (_configuration.GetValue<string>("StartDateTimeToReturnEmptyMPExpirationDate") != null && _configuration.GetValue<string>("EndtDateTimeToReturnEmptyMPExpirationDate") != null && DateTime.ParseExact(_configuration.GetValue<string>("StartDateTimeToReturnEmptyMPExpirationDate").ToString().ToUpper().Trim(), "MM/dd/yyyy hh:mm tt", CultureInfo.InvariantCulture) <= DateTime.Now && DateTime.ParseExact(_configuration.GetValue<string>("EndtDateTimeToReturnEmptyMPExpirationDate").ToString().ToUpper().Trim(), "MM/dd/yyyy hh:mm tt", CultureInfo.InvariantCulture) >= DateTime.Now)
            {
                mpSummary.MembershipCardExpirationDate = string.Empty;
            }
            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("ShowWelcomeModel")) && _configuration.GetValue<bool>("ShowWelcomeModel") && !await IsVBQWelcomeModelDisplayed(req.MileagePlusNumber, req.Application.Id, req.DeviceId))
            {
                mpSummary.vBQWelcomeModel = new VBQWelcomeModel(_configuration);
            }
            return mpSummary;
        }

        private ErrorPremierActivity GetErrorPremierActivity(ServiceResponse sResponse)
        {
            ErrorPremierActivity errorPremierActivity = new ErrorPremierActivity();
            if (sResponse != null && sResponse.Status != null && !string.IsNullOrEmpty(sResponse.Status.Code))
            {
                if (sResponse.CurrentYearActivity != null)
                {
                    errorPremierActivity.ErrorPremierActivityTitle = _configuration.GetValue<string>("PremierActivityTitle");
                    if (sResponse.Status.Code.Trim().ToUpper() == "E8000")
                    {
                        errorPremierActivity.ShowErrorIcon = false;
                        errorPremierActivity.ErrorPremierActivityText = _configuration.GetValue<string>("InactiveErrorActivityText");
                    }
                    else
                    {
                        errorPremierActivity.ShowErrorIcon = true;
                        errorPremierActivity.ErrorPremierActivityText = _configuration.GetValue<string>("OtherErrorActivityText");
                    }
                }
            }
            return errorPremierActivity;
        }

        private PremierActivity GetPremierActivity(ServiceResponse serviceResponse, ref string pdqchasewaiverLabel, ref string pdqchasewavier)
        {
            PremierActivity premierActivity = new PremierActivity();
            PremierQualifierTracker pqm1 = new PremierQualifierTracker();
            PremierQualifierTracker pqs1 = new PremierQualifierTracker();
            PremierQualifierTracker pqd1 = new PremierQualifierTracker();

            if (serviceResponse != null && serviceResponse.CurrentYearActivity != null)
            {
                List<MOBKVP> lPremierTrackingLevels = new List<MOBKVP>();
                List<MOBKVP> lFlexMiles = new List<MOBKVP>();
                bool isPresidentialPlusCard = false;
                lPremierTrackingLevels = GetPremierTrackingLevels(serviceResponse.CurrentPremierLevel, serviceResponse.TrackingLevel);

                premierActivity.PremierActivityTitle = _configuration.GetValue<string>("PremierActivityTitle");
                premierActivity.PremierActivityYear = _configuration.GetValue<string>("EarnedInText") + " " + serviceResponse.CurrentYearActivity.ActivityYear;
                premierActivity.PremierActivityStatus = (lPremierTrackingLevels != null && lPremierTrackingLevels.Count > 0) ? lPremierTrackingLevels.Where(s => s.Key == "PremierActivityStatus").SingleOrDefault().Value : "0";

                isPresidentialPlusCard = IsPresidentialPlusCard(serviceResponse.CurrentYearActivity);
                pqm1.PremierQualifierTrackerTitle = _configuration.GetValue<string>("PremierQualifierTrackerMilesTitle");
                pqm1.PremierQualifierTrackerCurrentValue = Convert.ToString(serviceResponse.CurrentYearActivity.EliteQualifyingMiles - serviceResponse.CurrentYearActivity.FlexEliteQualifyingMiles);
                if (isPresidentialPlusCard && serviceResponse.TrackingLevel < 4 && serviceResponse.CurrentYearActivity.FlexEliteQualifyingMiles > 0)
                {
                    pqm1.PremierQualifierTrackerCurrentText = string.Format("{0:###,##0}", serviceResponse.CurrentYearActivity.EliteQualifyingMiles);
                    pqm1.PremierQualifierTrackerCurrentFlexValue = Convert.ToString(serviceResponse.CurrentYearActivity.FlexEliteQualifyingMiles);
                    pqm1.PremierQualifierTrackerCurrentFlexTitle = _configuration.GetValue<string>("flexPQMTitle") + " " + string.Format("{0:###,##0}", serviceResponse.CurrentYearActivity.FlexEliteQualifyingMiles);
                }
                else
                {
                    pqm1.PremierQualifierTrackerCurrentText = string.Format("{0:###,##0}", serviceResponse.CurrentYearActivity.EliteQualifyingMiles - serviceResponse.CurrentYearActivity.FlexEliteQualifyingMiles);
                    pqm1.PremierQualifierTrackerCurrentFlexValue = "0";
                }
                pqm1.PremierQualifierTrackerThresholdValue = (lPremierTrackingLevels != null && lPremierTrackingLevels.Count > 0) ? lPremierTrackingLevels.Where(s => s.Key == "TrackerThresholdMiles").SingleOrDefault().Value : "0";
                pqm1.PremierQualifierTrackerThresholdText = string.Format("{0:###,##0}", (lPremierTrackingLevels != null && lPremierTrackingLevels.Count > 0) ? Convert.ToDecimal(lPremierTrackingLevels.Where(s => s.Key == "TrackerThresholdMiles").SingleOrDefault().Value) : 0);
                pqm1.PremierQualifierTrackerThresholdPrefix = "of";
                pqm1.Separator = "or";
                pqm1.IsWaived = false;
                premierActivity.PQM = pqm1;

                pqs1.PremierQualifierTrackerTitle = _configuration.GetValue<string>("PremierQualifierTrackerSegmentsTitle");
                pqs1.PremierQualifierTrackerCurrentValue = Convert.ToString(serviceResponse.CurrentYearActivity.EliteQualifyingPoints);
                pqs1.PremierQualifierTrackerCurrentText = string.Format("{0:0.#}", serviceResponse.CurrentYearActivity.EliteQualifyingPoints);
                pqs1.PremierQualifierTrackerThresholdValue = (lPremierTrackingLevels != null && lPremierTrackingLevels.Count > 0) ? lPremierTrackingLevels.Where(s => s.Key == "TrackerThresholdSegments").SingleOrDefault().Value : "0";
                pqs1.PremierQualifierTrackerThresholdText = string.Format("{0:###,##0}", (lPremierTrackingLevels != null && lPremierTrackingLevels.Count > 0) ? Convert.ToDecimal(lPremierTrackingLevels.Where(s => s.Key == "TrackerThresholdSegments").SingleOrDefault().Value) : 0);
                pqs1.PremierQualifierTrackerThresholdPrefix = "of";
                pqs1.Separator = "+";
                pqs1.IsWaived = false;
                premierActivity.PQS = pqs1;

                pqd1.PremierQualifierTrackerTitle = _configuration.GetValue<string>("PremierQualifierTrackerDollarsTitle"); ;
                pqd1.PremierQualifierTrackerCurrentValue = Convert.ToString(Convert.ToDecimal(serviceResponse.CurrentYearActivity.TotalRevenue));
                pqd1.PremierQualifierTrackerCurrentText = serviceResponse.CurrentYearActivity.TotalRevenue > 0 ? serviceResponse.CurrentYearActivity.TotalRevenue.ToString("C0") : "$0";
                pqd1.IsWaived = IsPQDWaived(serviceResponse);
                pqd1.PremierQualifierTrackerThresholdValue = (lPremierTrackingLevels != null && lPremierTrackingLevels.Count > 0) ? lPremierTrackingLevels.Where(s => s.Key == "TrackerThresholdDollars").SingleOrDefault().Value : "0";
                pqd1.PremierQualifierTrackerThresholdText = pqd1.IsWaived == true ? _configuration.GetValue<string>("WaivedText") : (lPremierTrackingLevels != null && lPremierTrackingLevels.Count > 0) ? Convert.ToDecimal(lPremierTrackingLevels.Where(s => s.Key == "TrackerThresholdDollars").SingleOrDefault().Value).ToString("C0") : "$0";
                pqd1.PremierQualifierTrackerThresholdPrefix = pqd1.IsWaived == true ? string.Empty : "of";
                premierActivity.PQD = pqd1;

                List<MOBKVP> lKeyValue = new List<MOBKVP>();

                MOBKVP premierActivityKey1 = new MOBKVP();
                premierActivityKey1.Key = _configuration.GetValue<string>("flightsegmentminimum");
                premierActivityKey1.Value = Get4FlightSegmentMinimum(serviceResponse.CurrentYearActivity);
                lKeyValue.Add(premierActivityKey1);
                if (!_configuration.GetValue<bool>("EnableVBQII") && !IsInternationalMember(serviceResponse.CurrentYearActivity))
                {
                    if (_configuration.GetValue<bool>("IsAvoidUAWSChaseMessagingSoapCall"))
                    {
                        MOBKVP premierActivityKey2 = new MOBKVP();
                        premierActivityKey2.Key = isPresidentialPlusCard ? _configuration.GetValue<string>("PresidentialPlusPQDWaiver") : _configuration.GetValue<string>("creditcardspendPQDWaiver");
                        if (serviceResponse.TrackingLevel == 4) //Loyal-3705
                        {
                            premierActivityKey2.Value = _configuration.GetValue<string>("CreditCardSpendPQDNotapplicable");
                        }
                        else
                        {
                            premierActivityKey2.Value = isPresidentialPlusCard ? !_configuration.GetValue<bool>("EnableVBQII") ? _configuration.GetValue<string>("PresidentialPlusPQDWaiverValue") : GetCreditCardSpendPQDWaiverText(serviceResponse) : GetCreditCardSpendPQDWaiverText(serviceResponse);
                        }

                        lKeyValue.Add(premierActivityKey2);
                        if (_configuration.GetValue<bool>("IsAvoidUAWSChaseMessagingSoapCall"))
                        {
                            pdqchasewaiverLabel = premierActivityKey2.Key;
                            pdqchasewavier = premierActivityKey2.Value;
                        }
                    }
                }
                MOBKVP premierActivityKey3 = new MOBKVP();
                premierActivityKey3.Key = _configuration.GetValue<string>("viewpremierstatustracker");
                premierActivityKey3.Value = _configuration.GetValue<string>("viewpremierstatustrackerlink");

                lKeyValue.Add(premierActivityKey3);
                premierActivity.KeyValueList = lKeyValue;
            }
            return premierActivity;

        }

        private string Get4FlightSegmentMinimum(Activity currentYearActivity)
        {
            string value = string.Empty;
            if (currentYearActivity != null)
            {
                if (currentYearActivity.RpcWaivedIndicator.ToUpper() == "N" || (_configuration.GetValue<bool>("EnableVBQII") && currentYearActivity.RpcWaivedIndicator.ToUpper() == "Y"))
                {
                    value = currentYearActivity.MinimumSegmentsRequired >= 4 ? "4 of 4" : currentYearActivity.MinimumSegmentsRequired + " of 4";
                }
                else if (currentYearActivity.RpcWaivedIndicator.ToUpper() == "Y")
                {
                    value = _configuration.GetValue<string>("WaivedText");
                }
            }
            return value;
        }

        private bool IsPQDWaived(ServiceResponse serviceResponse)
        {
            bool isWaived = false;
            if (!_configuration.GetValue<bool>("EnableVBQII") && serviceResponse != null && serviceResponse.CurrentYearActivity != null &&
                (
                (serviceResponse.CurrentYearActivity.DomesticIndicator != null && serviceResponse.CurrentYearActivity.DomesticIndicator.ToUpper() == "N") ||
                (serviceResponse.CurrentYearActivity.PresidentPlusIndicator != null && serviceResponse.CurrentYearActivity.PresidentPlusIndicator.ToUpper() == "Y" && serviceResponse.TrackingLevel < 4) ||
                (serviceResponse.MileagePlusCardIndicator != null && serviceResponse.MileagePlusCardIndicator.ToUpper() == "Y" &&
                serviceResponse.CurrentYearActivity.ChaseCardSpendIndicator != null && serviceResponse.CurrentYearActivity.ChaseCardSpendIndicator.ToUpper() == "Y" && serviceResponse.TrackingLevel < 4)
                )
                )
            {
                isWaived = true;
            }

            return isWaived;
        }

        private bool IsPresidentialPlusCard(Activity currentYearActivity)
        {
            if (currentYearActivity != null && !string.IsNullOrEmpty(currentYearActivity.PresidentPlusIndicator) && currentYearActivity.PresidentPlusIndicator.ToUpper() == "Y")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private List<MOBKVP> GetPremierTrackingLevels(int currentLevel, int trackingLevel)
        {
            string PremierLevelStatus = string.Empty;
            List<MOBKVP> list = new List<MOBKVP>();
            string premierStatusPrefix = string.Empty;
            if (currentLevel == trackingLevel)
            {
                premierStatusPrefix = _configuration.GetValue<string>("TorequalifyPrefix");
            }
            else
            {
                premierStatusPrefix = _configuration.GetValue<string>("ToreachPrefix");
            }
            switch (trackingLevel)
            {
                case 1:
                    list.Add(new MOBKVP("PremierActivityStatus", premierStatusPrefix + " " + _configuration.GetValue<string>("PremierStatusSilverText")));
                    list.Add(new MOBKVP("TrackerThresholdMiles", _configuration.GetValue<string>("ThresholdMilesSilver")));
                    list.Add(new MOBKVP("TrackerThresholdSegments", _configuration.GetValue<string>("ThresholdSegmentsSilver")));
                    list.Add(new MOBKVP("TrackerThresholdDollars", _configuration.GetValue<string>("ThresholdDollarsSilver")));
                    break;
                case 2:
                    list.Add(new MOBKVP("PremierActivityStatus", premierStatusPrefix + " " + _configuration.GetValue<string>("PremierStatusGoldText")));
                    list.Add(new MOBKVP("TrackerThresholdMiles", _configuration.GetValue<string>("ThresholdMilesGold")));
                    list.Add(new MOBKVP("TrackerThresholdSegments", _configuration.GetValue<string>("ThresholdSegmentsGold")));
                    list.Add(new MOBKVP("TrackerThresholdDollars", _configuration.GetValue<string>("ThresholdDollarsGold")));
                    break;
                case 3:
                    list.Add(new MOBKVP("PremierActivityStatus", premierStatusPrefix + " " + _configuration.GetValue<string>("PremierStatusPlatinumText")));
                    list.Add(new MOBKVP("TrackerThresholdMiles", _configuration.GetValue<string>("ThresholdMilesPlatinum")));
                    list.Add(new MOBKVP("TrackerThresholdSegments", _configuration.GetValue<string>("ThresholdSegmentsPlatinum")));
                    list.Add(new MOBKVP("TrackerThresholdDollars", _configuration.GetValue<string>("ThresholdDollarsPlatinum")));
                    break;
                case 4:
                case 6:
                    list.Add(new MOBKVP("PremierActivityStatus", GetPremierActivityStatus(currentLevel, trackingLevel)));
                    list.Add(new MOBKVP("TrackerThresholdMiles", _configuration.GetValue<string>("ThresholdMiles1K")));
                    list.Add(new MOBKVP("TrackerThresholdSegments", _configuration.GetValue<string>("ThresholdSegments1K")));
                    list.Add(new MOBKVP("TrackerThresholdDollars", _configuration.GetValue<string>("ThresholdDollars1K")));
                    break;
                default:
                    break;
            }

            return list;
        }

        private string GetPremierActivityStatus(int currentLevel, int trackingLevel)
        {
            string premierActivityStatus = string.Empty;
            if (currentLevel == 3 && trackingLevel == 4)
            {
                premierActivityStatus = _configuration.GetValue<string>("ToreachPrefix") + " " + string.Format(_configuration.GetValue<string>("Premier1KText"), (char)174);
            }
            else if (currentLevel == 4 && trackingLevel == 4)
            {
                premierActivityStatus = _configuration.GetValue<string>("TorequalifyPrefix") + " " + string.Format(_configuration.GetValue<string>("Premier1KText"), (char)174);
            }
            else if (currentLevel >= 4 && trackingLevel >= 4)
            {
                premierActivityStatus = _configuration.GetValue<bool>("EnablePointsActivity") ? _configuration.GetValue<string>
                    ("EarnPremierPlusPoints") : _configuration.GetValue<string>("EarnPremierupgrades");
            }
            return premierActivityStatus;
        }

        private bool IsPremierStatusTrackerUpgradesSupportedVersion(int appId, string appVersion)
        {
            bool isPremierStatusTrackerSupportedVersion = false;
            if (!string.IsNullOrEmpty(appVersion) && appId != -1)
            {
                isPremierStatusTrackerSupportedVersion = GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidPremierStatusTrackerUpgradesVersion", "iPhonePremierStatusTrackerUpgradesVersion", "", "", true, _configuration);
            }
            return isPremierStatusTrackerSupportedVersion;
        }

        private List<MOBKVP> GetVBQPremierTrackingLevels(int currentLevel, int trackingLevel, int infiniteLevel, long lifetimemiles)
        {
            string PremierLevelStatus = string.Empty;
            List<MOBKVP> list = new List<MOBKVP>();
            string premierStatusPrefix = string.Empty;
            if (currentLevel == trackingLevel)
            {
                premierStatusPrefix = _configuration.GetValue<string>("TorequalifyPrefix");
            }
            else
            {
                premierStatusPrefix = _configuration.GetValue<string>("ToreachPrefix");
            }
            switch (trackingLevel)
            {
                case 1:
                    list.Add(new MOBKVP("PremierActivityStatus", premierStatusPrefix + " " + _configuration.GetValue<string>("PremierStatusSilverText")));
                    list.Add(new MOBKVP("TrackerThresholdPQF", _configuration.GetValue<string>("ThresholdPQFSilver")));
                    list.Add(new MOBKVP("TrackerThresholdPQP", _configuration.GetValue<string>("ThresholdPQPSilver")));
                    list.Add(new MOBKVP("TrackerThresholdOutrightPQP", _configuration.GetValue<string>("ThresholdOutrightPQPSilver")));
                    break;
                case 2:
                    list.Add(new MOBKVP("PremierActivityStatus", premierStatusPrefix + " " + _configuration.GetValue<string>("PremierStatusGoldText")));
                    list.Add(new MOBKVP("TrackerThresholdPQF", _configuration.GetValue<string>("ThresholdPQFGold")));
                    list.Add(new MOBKVP("TrackerThresholdPQP", _configuration.GetValue<string>("ThresholdPQPGold")));
                    list.Add(new MOBKVP("TrackerThresholdOutrightPQP", _configuration.GetValue<string>("ThresholdOutrightPQPGold")));
                    break;
                case 3:
                    list.Add(new MOBKVP("PremierActivityStatus", premierStatusPrefix + " " + _configuration.GetValue<string>("PremierStatusPlatinumText")));
                    list.Add(new MOBKVP("TrackerThresholdPQF", _configuration.GetValue<string>("ThresholdPQFPlatinum")));
                    list.Add(new MOBKVP("TrackerThresholdPQP", _configuration.GetValue<string>("ThresholdPQPPlatinum")));
                    list.Add(new MOBKVP("TrackerThresholdOutrightPQP", _configuration.GetValue<string>("ThresholdOutrightPQPPlatinum")));
                    break;
                case 4:
                case 6:
                    list.Add(new MOBKVP("PremierActivityStatus", GetVBQPremierActivityStatus(currentLevel, trackingLevel, infiniteLevel, lifetimemiles)));
                    list.Add(new MOBKVP("TrackerThresholdPQF", _configuration.GetValue<string>("ThresholdPQF1K")));
                    list.Add(new MOBKVP("TrackerThresholdPQP", _configuration.GetValue<string>("ThresholdPQP1K")));
                    list.Add(new MOBKVP("TrackerThresholdOutrightPQP", _configuration.GetValue<string>("ThresholdOutrightPQP1K")));
                    break;
                default:
                    break;
            }
            return list;
        }

        private string GetVBQPremierActivityStatus(int currentLevel, int trackingLevel, int infiniteLevel, long lifetimemiles)
        {
            string premierActivityStatus = string.Empty;
            if (currentLevel == 3 && trackingLevel == 4)
            {
                premierActivityStatus = _configuration.GetValue<string>("ToreachPrefix") + " " + string.Format(_configuration.GetValue<string>("Premier1KText"), (char)174);
            }
            else if (currentLevel == 4 && trackingLevel == 4)
            {
                premierActivityStatus = _configuration.GetValue<string>("TorequalifyPrefix") + " " + string.Format(_configuration.GetValue<string>("Premier1KText"), (char)174);
            }
            else if (currentLevel == 5 && trackingLevel == 4)
            {
                premierActivityStatus = _configuration.GetValue<string>("ToreachPrefix") + " " + string.Format(_configuration.GetValue<string>("Premier1KText"), (char)174);
            }
            else if (trackingLevel == 6 && (currentLevel == 4 || currentLevel == 5) && ((infiniteLevel == 4 || infiniteLevel == 5) || lifetimemiles >= 3000000))
            {
                premierActivityStatus = _configuration.GetValue<string>("EarnPremierPlusPoints");
            }
            else if (currentLevel >= 4 && trackingLevel >= 4)
            {
                premierActivityStatus = _configuration.GetValue<string>("EarnPremierPlusPoints");
            }
            return premierActivityStatus;
        }

        private VBQPremierActivity GetVBQPremierActivity(ServiceResponse sResponse)
        {
            VBQPremierActivity premierActivity = new VBQPremierActivity();
            PremierQualifierTracker pqf = new PremierQualifierTracker();
            PremierQualifierTracker pqp = new PremierQualifierTracker();
            PremierQualifierTracker outrightPQP = new PremierQualifierTracker();
            if (sResponse != null && sResponse.CurrentYearActivity != null)
            {
                List<MOBKVP> lPremierTrackingLevels = new List<MOBKVP>();
                List<MOBKVP> lFlexMiles = new List<MOBKVP>();
                lPremierTrackingLevels = GetVBQPremierTrackingLevels(sResponse.CurrentPremierLevel, sResponse.TrackingLevel, sResponse.InfiniteLevel, sResponse.LifetimeMiles);
                premierActivity.VBQPremierActivityTitle = _configuration.GetValue<string>("PremierActivityTitle");
                premierActivity.VBQPremierActivityYear = _configuration.GetValue<string>("EarnedInText") + " " + sResponse.CurrentYearActivity.ActivityYear;
                pqf.PremierQualifierTrackerTitle = _configuration.GetValue<string>("PremierQualifierTrackerFlightsTitle");
                pqf.PremierQualifierTrackerCurrentValue = Convert.ToString(sResponse.CurrentYearActivity.PremierQualifyingFlightSegments);
                pqf.PremierQualifierTrackerCurrentText = string.Format("{0:###,##0}", sResponse.CurrentYearActivity.PremierQualifyingFlightSegments);
                pqf.PremierQualifierTrackerThresholdValue = (lPremierTrackingLevels != null && lPremierTrackingLevels.Count > 0) ? lPremierTrackingLevels.Where(s => s.Key == "TrackerThresholdPQF").SingleOrDefault().Value : "0";
                pqf.PremierQualifierTrackerThresholdText = string.Format("{0:###,##0}", (lPremierTrackingLevels != null && lPremierTrackingLevels.Count > 0) ? Convert.ToDecimal(lPremierTrackingLevels.Where(s => s.Key == "TrackerThresholdPQF").SingleOrDefault().Value) : 0);
                pqf.Separator = "+";
                pqf.PremierQualifierTrackerThresholdPrefix = "of";
                pqp.PremierQualifierTrackerTitle = _configuration.GetValue<string>("PremierQualifierTrackerPointsTitle");
                pqp.PremierQualifierTrackerCurrentValue = sResponse.TrackingLevel > 3 ? (sResponse.CurrentYearActivity.PremierQualifyingPoints - sResponse.CurrentYearActivity.FlexPremierQualifyingPoints).ToString() : sResponse.CurrentYearActivity.PremierQualifyingPoints.ToString();
                pqp.PremierQualifierTrackerCurrentText = string.Format("{0:###,##0}", Convert.ToInt32(pqp.PremierQualifierTrackerCurrentValue));

                pqp.PremierQualifierTrackerThresholdValue = (lPremierTrackingLevels != null && lPremierTrackingLevels.Count > 0) ? lPremierTrackingLevels.Where(s => s.Key == "TrackerThresholdPQP").SingleOrDefault().Value : "0";
                pqp.PremierQualifierTrackerThresholdText = string.Format("{0:###,##0}", (lPremierTrackingLevels != null && lPremierTrackingLevels.Count > 0) ? Convert.ToDecimal(lPremierTrackingLevels.Where(s => s.Key == "TrackerThresholdPQP").SingleOrDefault().Value) : 0);
                pqp.Separator = "or";
                pqp.PremierQualifierTrackerThresholdPrefix = "of";

                outrightPQP.PremierQualifierTrackerTitle = _configuration.GetValue<string>("PremierQualifierTrackerPointsTitle");
                outrightPQP.PremierQualifierTrackerCurrentValue = sResponse.TrackingLevel > 3 ? (sResponse.CurrentYearActivity.PremierQualifyingPoints - sResponse.CurrentYearActivity.FlexPremierQualifyingPoints).ToString() : sResponse.CurrentYearActivity.PremierQualifyingPoints.ToString(); ;
                outrightPQP.PremierQualifierTrackerCurrentText = string.Format("{0:###,##0}", Convert.ToInt32(outrightPQP.PremierQualifierTrackerCurrentValue));
                outrightPQP.PremierQualifierTrackerThresholdValue = (lPremierTrackingLevels != null && lPremierTrackingLevels.Count > 0) ? lPremierTrackingLevels.Where(s => s.Key == "TrackerThresholdOutrightPQP").SingleOrDefault().Value : "0";
                outrightPQP.PremierQualifierTrackerThresholdText = string.Format("{0:###,##0}", (lPremierTrackingLevels != null && lPremierTrackingLevels.Count > 0) ? Convert.ToDecimal(lPremierTrackingLevels.Where(s => s.Key == "TrackerThresholdOutrightPQP").SingleOrDefault().Value) : 0);
                premierActivity.VBQPremierActivityStatus = (lPremierTrackingLevels != null && lPremierTrackingLevels.Count > 0) ? lPremierTrackingLevels.Where(s => s.Key == "PremierActivityStatus").SingleOrDefault().Value : "0";
                outrightPQP.PremierQualifierTrackerThresholdPrefix = "of";
                premierActivity.PQF = pqf;
                premierActivity.PQP = pqp;
                premierActivity.OutrightPQP = outrightPQP;
                List<MOBKVP> lKeyValue = new List<MOBKVP>();
                MOBKVP premierActivityKey1 = new MOBKVP();
                premierActivityKey1.Key = _configuration.GetValue<string>("flightsegmentminimum");
                premierActivityKey1.Value = Get4FlightSegmentMinimum(sResponse.CurrentYearActivity);
                lKeyValue.Add(premierActivityKey1);
                if (sResponse.CurrentYearActivity.FlexPremierQualifyingPoints > 0)
                {
                    MOBKVP premierActivityChasePqp = new MOBKVP();
                    premierActivityChasePqp.Key = _configuration.GetValue<string>("ChasePQPText");
                    premierActivityChasePqp.Value = Convert.ToString(sResponse.CurrentYearActivity.FlexPremierQualifyingPoints);
                    lKeyValue.Add(premierActivityChasePqp);
                }
                MOBKVP premierActivityKey3 = new MOBKVP();
                premierActivityKey3.Key = _configuration.GetValue<string>("viewpremierstatustracker");
                premierActivityKey3.Value = _configuration.GetValue<string>("viewpremierstatustrackerlink");
                lKeyValue.Add(premierActivityKey3);
                premierActivity.KeyValueList = lKeyValue;
            }
            return premierActivity;
        }

        private VBQPremierActivity GetVBQTrackingUpgradesActivity(ServiceResponse sResponse, int trackingUpgradeType)
        {
            VBQPremierActivity upgradeActivity = new VBQPremierActivity();
            PremierQualifierTracker pqf = new PremierQualifierTracker();
            PremierQualifierTracker pqp = new PremierQualifierTracker();
            PremierQualifierTracker outrightPQP = new PremierQualifierTracker();
            if (sResponse != null && sResponse.CurrentYearActivity != null)
            {
                List<MOBKVP> lUpgradeTrackingLevels = new List<MOBKVP>();
                string premierStatusPrefix = _configuration.GetValue<string>("TorequalifyPrefix");
                pqf.PremierQualifierTrackerCurrentValue = sResponse.CurrentYearActivity.PremierQualifyingFlightSegments.ToString();
                pqf.PremierQualifierTrackerCurrentText = string.Format("{0:###,##0}", sResponse.CurrentYearActivity.PremierQualifyingFlightSegments);
                pqf.PremierQualifierTrackerThresholdPrefix = "of";
                pqf.Separator = "+";
                pqf.PremierQualifierTrackerTitle = _configuration.GetValue<string>("PremierQualifierTrackerFlightsTitle");
                pqp.PremierQualifierTrackerCurrentValue = trackingUpgradeType == 1 ? sResponse.CurrentYearActivity.PremierQualifyingPoints.ToString() : (sResponse.CurrentYearActivity.PremierQualifyingPoints - sResponse.CurrentYearActivity.FlexPremierQualifyingPoints).ToString();
                pqp.PremierQualifierTrackerCurrentText = string.Format("{0:###,##0}", Convert.ToInt32(pqp.PremierQualifierTrackerCurrentValue));
                pqp.Separator = trackingUpgradeType != 3 ? "or" : string.Empty;
                pqp.PremierQualifierTrackerTitle = _configuration.GetValue<string>("PremierQualifierTrackerPointsTitle");
                outrightPQP.PremierQualifierTrackerCurrentValue = trackingUpgradeType == 1 ? sResponse.CurrentYearActivity.PremierQualifyingPoints.ToString() : (sResponse.CurrentYearActivity.PremierQualifyingPoints - sResponse.CurrentYearActivity.FlexPremierQualifyingPoints).ToString(); ;
                outrightPQP.PremierQualifierTrackerCurrentText = string.Format("{0:###,##0}", Convert.ToInt32(outrightPQP.PremierQualifierTrackerCurrentValue));
                // pqp.PremierQualifierTrackerCurrentChaseFlexValue = string.Format("{0:###,##0}", sResponse.CurrentYearActivity.FlexPremierQualifyingPoints);
                // outrightPQP.PremierQualifierTrackerCurrentChaseFlexValue = string.Format("{0:###,##0}", sResponse.CurrentYearActivity.FlexPremierQualifyingPoints);
                lUpgradeTrackingLevels = GetVBQUpgradeTrackingLevels(trackingUpgradeType, Convert.ToInt64(sResponse.CurrentYearActivity.PremierQualifyingFlightSegments), sResponse.CurrentYearActivity.PremierQualifyingPoints);
                pqf.PremierQualifierTrackerThresholdValue = trackingUpgradeType == 1 ? GetValueFromList(lUpgradeTrackingLevels, "TrackerThresholdPQF") : trackingUpgradeType == 2 ? GetValueFromList(lUpgradeTrackingLevels, "TrackerThresholdPQF") : string.Empty;
                pqp.PremierQualifierTrackerThresholdValue = trackingUpgradeType == 1 ? GetValueFromList(lUpgradeTrackingLevels, "TrackerThresholdPQP") : trackingUpgradeType == 2 ? GetValueFromList(lUpgradeTrackingLevels, "TrackerThresholdPQP") : trackingUpgradeType == 3 ? GetValueFromList(lUpgradeTrackingLevels, "TrackerThresholdPQP") : string.Empty;
                outrightPQP.PremierQualifierTrackerThresholdValue = trackingUpgradeType == 1 ? GetValueFromList(lUpgradeTrackingLevels, "TrackerThresholdOutrightPQP") : trackingUpgradeType == 2 ? GetValueFromList(lUpgradeTrackingLevels, "TrackerThresholdOutrightPQP") : string.Empty;
                pqf.PremierQualifierTrackerThresholdText = string.IsNullOrEmpty(pqf.PremierQualifierTrackerThresholdValue) ? string.Empty : string.Format("{0:###,##0}", Convert.ToDecimal(pqf.PremierQualifierTrackerThresholdValue));
                pqp.PremierQualifierTrackerThresholdText = string.IsNullOrEmpty(pqp.PremierQualifierTrackerThresholdValue) ? string.Empty : string.Format("{0:###,##0}", Convert.ToDecimal(pqp.PremierQualifierTrackerThresholdValue));
                outrightPQP.PremierQualifierTrackerThresholdText = string.IsNullOrEmpty(outrightPQP.PremierQualifierTrackerThresholdValue) ? string.Empty : string.Format("{0:###,##0}", Convert.ToDecimal(outrightPQP.PremierQualifierTrackerThresholdValue));
                upgradeActivity.VBQPremierActivityTitle = _configuration.GetValue<string>("PremierActivityTitle");
                upgradeActivity.VBQPremierActivityYear = _configuration.GetValue<string>("EarnedInText") + " " + sResponse.CurrentYearActivity.ActivityYear;
                upgradeActivity.KeyValueList = GetVBQUpgradeKeyValues(trackingUpgradeType, sResponse.CurrentYearActivity.MinimumSegmentsRequired >= 4 ? 4 : sResponse.CurrentYearActivity.MinimumSegmentsRequired);
                pqp.PremierQualifierTrackerThresholdPrefix = "of";
                outrightPQP.PremierQualifierTrackerThresholdPrefix = "of";
                outrightPQP.PremierQualifierTrackerTitle = _configuration.GetValue<string>("PremierQualifierTrackerPointsTitle");
                upgradeActivity.KeyValueList = new List<MOBKVP>();
                upgradeActivity.KeyValueList.Add(
                   new MOBKVP()
                   {
                       Key = _configuration.GetValue<string>("MyAccountNextMilestone"),
                       Value = trackingUpgradeType == 1 ? _configuration.GetValue<string>("PlatinumNextMilestonePlusPoints") : trackingUpgradeType == 2 ? _configuration.GetValue<string>("1KNextMilestonePlusPoints") : trackingUpgradeType == 3 ? _configuration.GetValue<string>("IncrementalUpgradeNextMilestonePlusPoints") : string.Empty
                   });
                MOBKVP premierActivityKey1 = new MOBKVP();
                if (trackingUpgradeType != 3)
                {
                    premierActivityKey1.Key = _configuration.GetValue<string>("flightsegmentminimum");
                    premierActivityKey1.Value = Get4FlightSegmentMinimum(sResponse.CurrentYearActivity);
                    upgradeActivity.KeyValueList.Add(premierActivityKey1);
                }
                if (sResponse.CurrentYearActivity.FlexPremierQualifyingPoints > 0)
                {
                    MOBKVP premierActivityChasePqp = new MOBKVP();
                    premierActivityChasePqp.Key = _configuration.GetValue<string>("ChasePQPText");
                    premierActivityChasePqp.Value = Convert.ToString(sResponse.CurrentYearActivity.FlexPremierQualifyingPoints);
                    upgradeActivity.KeyValueList.Add(premierActivityChasePqp);
                }
                upgradeActivity.KeyValueList.Add(
                    new MOBKVP()
                    {
                        Key = _configuration.GetValue<string>("viewpremierstatustracker"),
                        Value = _configuration.GetValue<string>("viewpremierstatustrackerlink")
                    });
                pqf.Separator = "+";
                pqp.Separator = "or";
            }
            upgradeActivity.PQP = pqp;
            upgradeActivity.PQF = trackingUpgradeType != 3 ? pqf : null;
            upgradeActivity.OutrightPQP = trackingUpgradeType != 3 ? outrightPQP : null;
            upgradeActivity.VBQPremierActivityStatus = _configuration.GetValue<string>("EarnPremierPlusPoints");
            return upgradeActivity;
        }

        private List<MOBKVP> GetVBQUpgradeKeyValues(int trackingUpgradeType, double minSegmentRequired)
        {
            List<MOBKVP> lKeyValue = new List<MOBKVP>();

            switch (trackingUpgradeType)
            {
                case 1: //Platinum
                    {
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("MyAccountNextMilestone"), GetNextMilesStonePlusPointsValue(trackingUpgradeType)));
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("flightsegmentminimum"), minSegmentRequired + " of 4"));

                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("viewpremierstatustracker"), _configuration.GetValue<string>("viewpremierstatustrackerlink")));
                        break;
                    }
                case 2: //1K
                    {
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("MyAccountNextMilestone"), GetNextMilesStonePlusPointsValue(trackingUpgradeType)));
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("flightsegmentminimum"), minSegmentRequired + " of 4"));
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("viewpremierstatustracker"), _configuration.GetValue<string>("viewpremierstatustrackerlink")));
                        break;
                    }
                case 3://Incremental Upgrades
                    {
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("MyAccountNextMilestone"), GetNextMilesStonePlusPointsValue(trackingUpgradeType)));
                        lKeyValue.Add(new MOBKVP(_configuration.GetValue<string>("viewpremierstatustracker"), _configuration.GetValue<string>("viewpremierstatustrackerlink")));
                        break;
                    }
                default:
                    break;
            }
            return lKeyValue;
        }

        private string GetNextMilesStonePlusPointsValue(int trackingUpgradeType)//EX: +2 RPU / +6 GPU or  +1 GPU or +2 RPU
        {
            switch (trackingUpgradeType)
            {
                case 1: //Platinum
                    {
                        return _configuration.GetValue<string>("PlatinumNextMilestonePlusPoints");
                    }
                case 2: //1K
                    {
                        return _configuration.GetValue<string>("1KNextMilestonePlusPoints");
                    }
                case 3://Incremental Upgrades
                    {
                        return _configuration.GetValue<string>("IncrementalUpgradeNextMilestoneVBQPlusPoints");
                    }
                default:
                    break;
            }
            return string.Empty;
        }

        private List<MOBKVP> GetVBQUpgradeTrackingLevels(int trackingUpgradeType, long PQF, long PQP)
        {
            string PremierLevelStatus = string.Empty;
            List<MOBKVP> list = new List<MOBKVP>();
            string premierActivityStatus = string.Empty;
            long thresholdPQP = 0;
            switch (trackingUpgradeType)
            {
                case 1://1=Platinum Upgrade; 
                    list.Add(new MOBKVP("TrackerThresholdPQF", _configuration.GetValue<string>("ThresholdPQFPlatinum")));
                    list.Add(new MOBKVP("TrackerThresholdPQP", _configuration.GetValue<string>("ThresholdPQPPlatinum")));
                    list.Add(new MOBKVP("TrackerThresholdOutrightPQP", _configuration.GetValue<string>("ThresholdOutrightPQPPlatinum")));
                    break;
                case 2://2=1k Upgrade; 
                    list.Add(new MOBKVP("TrackerThresholdPQF", _configuration.GetValue<string>("ThresholdPQF1K")));
                    list.Add(new MOBKVP("TrackerThresholdPQP", _configuration.GetValue<string>("ThresholdPQP1K")));
                    list.Add(new MOBKVP("TrackerThresholdOutrightPQP", _configuration.GetValue<string>("ThresholdOutrightPQP1K")));
                    break;
                case 3://3= Incremental Upgrade
                    GetVBQIncrementalThresholdPQFAndPQP(PQF, PQP, out thresholdPQP);
                    list.Add(new MOBKVP("TrackerThresholdPQP", thresholdPQP.ToString()));
                    break;
                default:
                    break;
            }
            return list;
        }

        private void GetVBQIncrementalThresholdPQFAndPQP(long PQF, long PQP, out long thresholdPQP)
        {
            bool isNewThresholdValues = _configuration.GetValue<bool>("EnableNewThresholdValues");
            long thresholdPQF1K = !string.IsNullOrEmpty(_configuration.GetValue<string>("ThresholdPQF1K")) ? Convert.ToInt64(_configuration.GetValue<string>("ThresholdPQF1K")) : 0;
            long thresholdPQP1K = !string.IsNullOrEmpty(_configuration.GetValue<string>("ThresholdPQP1K")) ? Convert.ToInt64(_configuration.GetValue<string>("ThresholdPQP1K")) : 0;
            long thresholdOutrightPQP1K = !string.IsNullOrEmpty(_configuration.GetValue<string>("ThresholdOutrightPQP1K")) ? Convert.ToInt64(_configuration.GetValue<string>("ThresholdOutrightPQP1K")) : 0;
            long thresholIncrementalPQP = !string.IsNullOrEmpty(_configuration.GetValue<string>("ThresholIncrementalPQP")) ? Convert.ToInt64(_configuration.GetValue<string>("ThresholIncrementalPQP")) : 0;

            long quotientPQP = 0;
            if (!isNewThresholdValues)
            {
                if (PQF >= 54)
                {
                    quotientPQP = (PQP - 18000) > 0 ? (PQP - 18000) / 3000 : 0;
                    thresholdPQP = (quotientPQP + 1) * 3000 + 18000;
                }
                else
                {
                    quotientPQP = (PQP - 24000) > 0 ? (PQP - 24000) / 3000 : 0;
                    thresholdPQP = (quotientPQP + 1) * 3000 + 24000;
                }
            }
            else
            {
                //LOYAL-5937 - Changes related to COVID-19 Loyalty MP Updates
                if (PQP < thresholIncrementalPQP)
                {
                    thresholdPQP = thresholIncrementalPQP;
                }
                else
                {
                    if (PQF >= thresholdPQF1K)
                    {
                        quotientPQP = (PQP - thresholdPQP1K) > 0 ? (PQP - thresholdPQP1K) / 3000 : 0;
                        thresholdPQP = (quotientPQP + 1) * 3000 + thresholdPQP1K;
                    }
                    else
                    {
                        quotientPQP = (PQP - thresholdOutrightPQP1K) > 0 ? (PQP - thresholdOutrightPQP1K) / 3000 : 0;
                        thresholdPQP = (quotientPQP + 1) * 3000 + thresholdOutrightPQP1K;
                    }
                }
            }
        }

        private int GetVBQTrackingUpgradeType(ServiceResponse sResponse)
        {
            int trackingUpgradeType = 0;//1=Platinum Upgrade; 2=1k Upgrade; 3= Incremental Upgrade More Comments to understand whats this "trackingUpgradeType" use for
            if (sResponse != null && sResponse.CurrentYearActivity != null)
            {
                trackingUpgradeType = IsVBQMPUpgradeEligible(sResponse.TrackingLevel, sResponse.CurrentPremierLevel, sResponse.InfiniteLevel, sResponse.LifetimeMiles,
                                        sResponse.CurrentYearActivity.PremierQualifyingFlightSegments, sResponse.CurrentYearActivity.FlexPremierQualifyingPoints)
                                        ?
                    GetVBQTrackingUpgradeType(sResponse.CurrentYearActivity.PremierQualifyingPoints, sResponse.CurrentYearActivity.FlexPremierQualifyingPoints, sResponse.CurrentYearActivity.PremierQualifyingFlightSegments,
                        sResponse.CurrentYearActivity.MinimumSegmentsRequired) : 0;
            }
            return trackingUpgradeType;
        }

        private int GetVBQTrackingUpgradeType(double elitePQP, double flexPQP, double pQF, double minSegmentRequired)
        {
            if (minSegmentRequired >= 4)
            {
                bool isNewThresholdValues = _configuration.GetValue<bool>("EnableNewThresholdValues");
                if (!isNewThresholdValues)
                {
                    if ((pQF >= 54 && elitePQP >= 18000) || (elitePQP >= 24000))  //Incremental 
                    {
                        return 3;
                    }
                    else if ((pQF >= 36 && (elitePQP + flexPQP) >= 12000) || ((elitePQP + flexPQP) >= 15000)) //1K
                    {
                        return 2;
                    }
                    // When user has < 4 Minimum Segments Flown or 1K threshouldNot Met -- Falls in Platinum Upgrade
                    else   // For Platinum upgrades consider Flex miles if ((pQF < 36 && pqp < 12000) && (pqp < 15000))
                    {
                        return 1; //Platinum upgrade
                    }
                }
                else
                {
                    //LOYAL - 5937 - Changes Related to this Story COVID-19 Loyalty MP updates
                    double thresholdPQF1K = !string.IsNullOrEmpty(_configuration.GetValue<string>("ThresholdPQF1K")) ? Convert.ToDouble(_configuration.GetValue<string>("ThresholdPQF1K")) : 0;
                    double thresholdPQP1K = !string.IsNullOrEmpty(_configuration.GetValue<string>("ThresholdPQP1K")) ? Convert.ToInt64(_configuration.GetValue<string>("ThresholdPQP1K")) : 0;
                    double thresholdOutrightPQP1K = !string.IsNullOrEmpty(_configuration.GetValue<string>("ThresholdOutrightPQP1K")) ? Convert.ToInt64(_configuration.GetValue<string>("ThresholdOutrightPQP1K")) : 0;

                    double thresholdPQFPlatinum = !string.IsNullOrEmpty(_configuration.GetValue<string>("ThresholdPQFPlatinum")) ? Convert.ToDouble(_configuration.GetValue<string>("ThresholdPQFPlatinum")) : 0;
                    double thresholdPQPPlatinum = !string.IsNullOrEmpty(_configuration.GetValue<string>("ThresholdPQPPlatinum")) ? Convert.ToDouble(_configuration.GetValue<string>("ThresholdPQPPlatinum")) : 0;
                    double thresholdOutrightPQPPlatinum = !string.IsNullOrEmpty(_configuration.GetValue<string>("ThresholdOutrightPQPPlatinum")) ? Convert.ToDouble(_configuration.GetValue<string>("ThresholdOutrightPQPPlatinum")) : 0;

                    if ((pQF >= thresholdPQF1K && elitePQP >= thresholdPQP1K) || (elitePQP >= thresholdOutrightPQP1K))  //Incremental       
                    {
                        return 3;
                    }
                    else if ((pQF >= thresholdPQFPlatinum && (elitePQP + flexPQP) >= thresholdPQPPlatinum) || ((elitePQP + flexPQP) >= thresholdOutrightPQPPlatinum)) //1K
                    {
                        return 2;
                    }
                    // When user has < 4 Minimum Segments Flown or 1K threshouldNot Met -- Falls in Platinum Upgrade
                    else   // For Platinum upgrades consider Flex miles if ((pQF < 36 && pqp < 12000) && (pqp < 15000))
                    {
                        return 1; //Platinum upgrade
                    }
                }
            }
            else if (minSegmentRequired < 4)
            {
                return 1;
            }

            return 0;
        }

        private bool IsVBQMPUpgradeEligible(int trackingMPLevel, int currentPremierLevel, int infiniteLevel, long lifeTimeMiles, double PQF, long PQP)
        {
            bool isNewThresholdValues = _configuration.GetValue<bool>("EnableNewThresholdValues");
            if (!isNewThresholdValues)
            {
                if (trackingMPLevel == 6 && (currentPremierLevel == 4 || currentPremierLevel == 5) &&
                    (infiniteLevel == 4 || infiniteLevel == 5 || lifeTimeMiles >= 3000000 || ((PQF >= 54 && PQP >= 18000) || PQP >= 24000)))
                {
                    return true;
                }
            }
            else
            {
                //LOYAL-5937 - Changes related to this Story COVID-19 Loyalty MP Updates
                double thresholdPQF1K = !string.IsNullOrEmpty(_configuration.GetValue<string>("ThresholdPQF1K")) ? Convert.ToDouble(_configuration.GetValue<string>("ThresholdPQF1K")) : 0;
                long thresholdPQP1K = !string.IsNullOrEmpty(_configuration.GetValue<string>("ThresholdPQP1K")) ? Convert.ToInt64(_configuration.GetValue<string>("ThresholdPQP1K")) : 0;
                long thresholdOutrightPQP1K = !string.IsNullOrEmpty(_configuration.GetValue<string>("ThresholdOutrightPQP1K")) ? Convert.ToInt64(_configuration.GetValue<string>("ThresholdOutrightPQP1K")) : 0;
                if (trackingMPLevel == 6 && (currentPremierLevel == 4 || currentPremierLevel == 5) &&
                    (infiniteLevel == 4 || infiniteLevel == 5 || lifeTimeMiles >= 3000000 || ((PQF >= thresholdPQF1K && PQP >= thresholdPQP1K) || PQP >= thresholdOutrightPQP1K)))
                {
                    return true;
                }
                else if (_configuration.GetValue<bool>("EnableMyAccountTrackingLevel5Changes") && trackingMPLevel == 5 && (currentPremierLevel == 4 || currentPremierLevel == 5)) //JIRA LOYAL-5994
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsVBQTrackerSupportedVersion(int appId, string appVersion)
        {
            bool isPremierStatusTrackerSupportedVersion = false;
            if (!string.IsNullOrEmpty(appVersion) && appId != -1)
            {
                isPremierStatusTrackerSupportedVersion = GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidVBQTracker", "iOSVBQTracker", "", "", true, _configuration);
            }
            return isPremierStatusTrackerSupportedVersion;
        }

        private YearEndPremierActivity GetYearEndActivity(ServiceResponse response)
        {
            string sYear = DateTime.Now.ToString("yyyy");
            if (response.CurrentYearActivity.ActivityYear == DateTime.Now.Year)
                sYear = DateTime.Now.AddYears(1).ToString("yyyy");
            YearEndPremierActivity yearEndPremierActivity = new YearEndPremierActivity();
            if (response != null && response.CurrentYearActivity != null)
            {
                yearEndPremierActivity.YearEndPremierActivityTitle = _configuration.GetValue<string>("PremierActivityTitle");
                yearEndPremierActivity.YearEndPremierActivityYear = response.CurrentYearActivity.ActivityYear + " " + _configuration.GetValue<string>("YearendSnapshot");
                yearEndPremierActivity.YearEndPremierActivityStatus = string.Format(_configuration.GetValue<string>("YearendStatus"), sYear);
                yearEndPremierActivity.YearEndPQMTitle = _configuration.GetValue<string>("PremierQualifierTrackerMilesTitle");
                yearEndPremierActivity.YearEndPQMText = string.Format("{0:###,##0}", response.CurrentYearActivity.EliteQualifyingMiles);
                yearEndPremierActivity.YearEndPQSTitle = _configuration.GetValue<string>("PremierQualifierTrackerSegmentsTitle");
                yearEndPremierActivity.YearEndPQSText = string.Format("{0:0.#}", response.CurrentYearActivity.EliteQualifyingPoints);
                yearEndPremierActivity.YearEndPQDTitle = _configuration.GetValue<string>("PremierQualifierTrackerDollarsTitle");
                yearEndPremierActivity.YearEndPQDText = response.CurrentYearActivity.TotalRevenue > 0 ? response.CurrentYearActivity.TotalRevenue.ToString("C0") : "$0";
                yearEndPremierActivity.YearEnd4FlightSegmentMinimumText = _configuration.GetValue<string>("YearEnd4flightsegmentminimum");
                yearEndPremierActivity.YearEnd4FlightSegmentMinimumValue = GetYearEnd4FlightSegmentMinimum(response.CurrentYearActivity);
            }

            return yearEndPremierActivity;
        }

        private string GetYearEnd4FlightSegmentMinimum(Activity currentYearActivity)
        {
            string value = string.Empty;
            if (currentYearActivity != null)
            {
                value = currentYearActivity.MinimumSegmentsRequired >= 4 ? "4" : currentYearActivity.MinimumSegmentsRequired.ToString();
            }
            return value;
        }

        private async Task<List<MOBItem>> GetPremierActivityLearnAboutCaptions()
        {
            var captions = await GetCaptions("MYACCOUNT_PREMIERTRACKER_LEARNABOUTLINK");
            return captions;
        }

        private async Task<List<MOBItem>> GetCaptions(string key)
        {
            return !string.IsNullOrEmpty(key) ?await GetCaptions(new List<string> { key }, true) : null;
        }

        private async Task<List<MOBItem>> GetCaptions(List<string> keyList, bool isTnC)
        {
            DocumentLibraryDynamoDB documentLibraryDynamoDB = new DocumentLibraryDynamoDB(_configuration, _dynamoDBService);
            var docs = await documentLibraryDynamoDB.GetNewLegalDocumentsForTitles(keyList, _headers.ContextValues.SessionId).ConfigureAwait(false);
            if (docs == null || !docs.Any()) return null;

            var captions = new List<MOBItem>();

            captions.AddRange(
                docs.Select(doc => new MOBItem
                {
                    Id = doc.Title,
                    CurrentValue = doc.LegalDocument
                }));
            return captions;
        }

        public async Task<bool> IsVBQWelcomeModelDisplayed(string MileagePlusNumber, int ApplicationID, string DeviceID)
        {
            var mileagePlusDB = new MileagePlusDynamoDB(_configuration, _dynamoDBService, _logger);
            return await mileagePlusDB.IsVBQWelcomeModelDisplayed<bool>(MileagePlusNumber, ApplicationID.ToString(), DeviceID, _headers.ContextValues.SessionId).ConfigureAwait(false);

            //Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("uasp_Get_IsVBQWMDisplayed");
            //database.AddInParameter(dbCommand, "@MileagePlusNumber", DbType.String, MileagePlusNumber);
            //database.AddInParameter(dbCommand, "@ApplicationID", DbType.Int16, ApplicationID);
            //database.AddInParameter(dbCommand, "@DeviceID", DbType.String, DeviceID);
            //try
            //{
            //    using (IDataReader dataReader = database.ExecuteReader(dbCommand))
            //    {
            //        while (dataReader.Read())
            //        {
            //            ok = Convert.ToBoolean(dataReader["IsVBQWMDisplayed"));
            //        }
            //    }
            //}
            //catch (System.Exception ex) { string msg = ex.Message; }
        }

        private async Task<MOBFutureFlightCreditDetails> GetMPFutureFlightCredit(string mileagePlusNumber, MOBApplication application, string sessionId, string transactionId, string deviceId)
        {
            MOBFutureFlightCreditDetails futureFlightCreditDetails = null;

            List<MOBCancelledFFCPNRDetails> cancelledFFCPNRDetails
                = await GetMPFutureFlightCreditFromCancelReservationService
                (mileagePlusNumber, application.Id, application.Version.Major, sessionId, transactionId, deviceId, callsource: "mobileMyAccount");

            if (cancelledFFCPNRDetails != null && cancelledFFCPNRDetails.Count > 0)
            {
                futureFlightCreditDetails = new MOBFutureFlightCreditDetails();
                futureFlightCreditDetails.CancelledFFCPNRList = cancelledFFCPNRDetails;
                if (cancelledFFCPNRDetails.Count == 1)
                {
                    futureFlightCreditDetails.PNR = cancelledFFCPNRDetails[0] != null ? (cancelledFFCPNRDetails[0].RecordLocator ?? string.Empty) : string.Empty;
                    futureFlightCreditDetails.LastName = cancelledFFCPNRDetails[0].PNRLastName ?? string.Empty;
                    futureFlightCreditDetails.Text = _configuration.GetValue<string>("futureFlightCreditDetailsTextSingleFFC");
                    futureFlightCreditDetails.BtnText = _configuration.GetValue<string>("futureFlightCreditDetailsBtnTextSingleFFC");
                }
                else if (cancelledFFCPNRDetails.Count > 1)
                {
                    futureFlightCreditDetails.PNR = string.Empty;
                    futureFlightCreditDetails.LastName = string.Empty;
                    futureFlightCreditDetails.Text = _configuration.GetValue<string>("futureFlightCreditDetailsTextMultipleFFC");
                    futureFlightCreditDetails.BtnText = _configuration.GetValue<string>("futureFlightCreditDetailsBtnTextMultipleFFC");
                    if (application.Id == 2 && !GeneralHelper.IsApplicationVersionGreater(application.Id, application.Version.Major, "AndroidETCMyAccountFutureFlightCreditVersionEditorialChanges", "", "", "", true, _configuration))
                    {
                        //To display different text only in Android App Verions 3.0.35 and 3.0.36
                        futureFlightCreditDetails.MultiFFCText = _configuration.GetValue<string>("multipleFFCRedirectToCancelTripsTabTextAndroidAppVer3035and3036");
                        futureFlightCreditDetails.MultiFFCBtnText = _configuration.GetValue<string>("multipleFFCRedirectToCancelTripsTabBtnTextAndroidAppVer3035and3036");
                    }
                    else
                    {
                        futureFlightCreditDetails.MultiFFCText = _configuration.GetValue<string>("multipleFFCRedirectToCancelTripsTabText");
                        futureFlightCreditDetails.MultiFFCBtnText = _configuration.GetValue<string>("multipleFFCRedirectToCancelTripsTabBtnText");
                    }
                }
            }

            return futureFlightCreditDetails;
        }

        private async Task<List<MOBCancelledFFCPNRDetails>> GetMPFutureFlightCreditFromCancelReservationService
            (string mileagePlusNumber, int applicationId, string version, string sessionId, string transactionId, string deviceId, string callsource)
        {
            string requestUrl = string.Empty;
            var cancelledFFCPNRDetails = new List<MOBCancelledFFCPNRDetails>();

            try
            {
                string token = await _dPService.GetAnonymousToken(applicationId, deviceId, _configuration).ConfigureAwait(false);
                var responseFFC = await _mPFutureFlightCredit.GetMPFutureFlightCredit<getResrvationResponse>(token, callsource, mileagePlusNumber, sessionId).ConfigureAwait(false);

                if (responseFFC != null)
                {
                    if (string.Equals(responseFFC.Status, "Success", StringComparison.OrdinalIgnoreCase))
                    {
                        if (responseFFC.Reservation == null || !responseFFC.Reservation.Any(x => string.IsNullOrEmpty(x.RecLoc) == false)) return null;

                        responseFFC.Reservation?.ToList().ForEach(res =>
                        {
                            if (res.hasFFC)
                            {
                                var cancelledFFCPNRDetail = new MOBCancelledFFCPNRDetails
                                { RecordLocator = res.RecLoc, Passengers = new List<MOBName>() };

                                if (res.Traveller != null && res.Traveller.Any())
                                {
                                    cancelledFFCPNRDetail.PNRLastName = res.Traveller?.FirstOrDefault()?.LastName;

                                    res.Traveller.ToList().ForEach(pax =>
                                    {
                                        cancelledFFCPNRDetail.Passengers.Add(new MOBName
                                        {
                                            First = pax.FirstName,
                                            Last = pax.LastName
                                        });
                                    });
                                }
                                cancelledFFCPNRDetails.Add(cancelledFFCPNRDetail);
                            }
                        });
                    }
                }
            }
            catch (Exception)
            {
                _logger.LogError("GetMPFutureFlightCreditFromCancelReservationService error {mpNumber} {sessionId}", mileagePlusNumber, sessionId);
                return null;
            }
            return cancelledFFCPNRDetails.Any() ? cancelledFFCPNRDetails : null;
        }

        private async Task<MOBTravelCredit> GetTravelCreditDetail(string mileagePlusNumber, MOBApplication application, string sessionId, string transactionId, string deviceId)
        {
            MOBTravelCredit travelCreditInfo = null;
            bool isETCEnabled = _configuration.GetValue<bool>("EnableTravelCertificateDetailsOnMPAccount") && application != null && GeneralHelper.IsApplicationVersionGreater(application.Id, application.Version.Major, "IPhoneETCMyAccountTravelCertificateVersion", "AndroidETCMyAccountTravelCertificateVersion", "", "", true, _configuration);
            bool isFFCEnabled = _configuration.GetValue<bool>("EnableFutureFlightCreditOnMPAccount") && application != null && GeneralHelper.IsApplicationVersionGreater(application.Id, application.Version.Major, "IPhoneETCMyAccountFutureFlightCreditVersion", "AndroidETCMyAccountFutureFlightCreditVersion", "", "", true, _configuration);

            if (isETCEnabled || isFFCEnabled)
            {
                var docs =await GetCaptions("ETC_TravelCertificate_MyAccount");
                var travelCertificateResponse = docs != null && isETCEnabled ? await GetMPTravelCertificate(mileagePlusNumber, application, sessionId, transactionId, deviceId) : null;
                MOBFutureFlightCreditDetails futureFlightCredit = null;
                if (isFFCEnabled && docs != null)
                {
                    // this Condition is not to call Rohan Service until ther go live so we will go live early and return only ETC and no FFC even MP Has FFC as the dependent rohan serivce not live yet. 
                    futureFlightCredit = await GetMPFutureFlightCredit(mileagePlusNumber, application, sessionId, transactionId, deviceId);
                }
                if (docs != null && docs.Any() && ((travelCertificateResponse != null && travelCertificateResponse.Any()) || futureFlightCredit != null))
                {
                    travelCreditInfo = new MOBTravelCredit()
                    {
                        TravelCreditAvailableText = docs.Where(x => x.Id == "Title").Select(x => x.CurrentValue).FirstOrDefault().ToString(),
                        TravelCreditPageTitle = docs.Where(x => x.Id == "PageTitle").Select(x => x.CurrentValue).FirstOrDefault().ToString(),
                        TravelCreditHeaderTitle = docs.Where(x => x.Id == "DetailTitle").Select(x => x.CurrentValue).FirstOrDefault().ToString(),
                        TravelCreditHeaderBody = docs.Where(x => x.Id == "DetailDescription").Select(x => x.CurrentValue).FirstOrDefault().ToString(),
                        TravelCreditLearnAboutText = docs.Where(x => x.Id == "TermsConditionsTitle").Select(x => x.CurrentValue).FirstOrDefault().ToString(),
                        TravelCertificateHeaderForTermsConditions = docs.Where(x => x != null && x.Id == "TermsConditionsPageTitle").Select(x => x.CurrentValue).FirstOrDefault(),
                        ElectronicTravelCertificateHeader = docs.Where(x => x != null && x.Id != null && x.Id == "ElectronicTravelCertificateHeader").Select(x => x.CurrentValue).FirstOrDefault(),
                        FutureFlightCreditHeader = docs.Where(x => x != null && x.Id == "FutureFlightCreditHeader").Select(x => x.CurrentValue).FirstOrDefault(),
                        TravelCertificateTermsConditions = new List<MOBTypeOption>()
                        {
                            new MOBTypeOption
                            {
                                Key = "",
                                Value = application.Id == 1? docs.Where(x => x.Id == "TermsConditions_TravelCertificate_IOS").Select(x => x.CurrentValue).FirstOrDefault():
                                                        docs.Where(x => x.Id == "TermsConditions_TravelCertificate").Select(x => x.CurrentValue).FirstOrDefault()
                            }
                        }
                    };
                }
                if (travelCreditInfo != null && travelCertificateResponse != null && travelCertificateResponse.Any())
                {
                    travelCertificateResponse.Sort(d => d.CertficateExpiryDate);
                    travelCreditInfo.TravelCertificateOrFlightCreditActivity = travelCertificateResponse;
                }
                if (isFFCEnabled)
                {
                    if (docs != null && docs.Any() && travelCreditInfo != null && travelCreditInfo.TravelCertificateTermsConditions != null)
                    {
                        travelCreditInfo.TravelCertificateTermsConditions.Add(
                        new MOBTypeOption
                        {
                            Key = "",
                            Value = application.Id == 1 ? docs.Where(x => x != null && x.Id != null && x.Id == "TermsConditions_FutureFlightCredit_IOS").Select(x => x.CurrentValue).FirstOrDefault() :
                                            docs.Where(x => x != null && x.Id != null && x.Id == "TermsConditions_FutureFlightCredit").Select(x => x.CurrentValue).FirstOrDefault()
                        });
                    }
                    if (futureFlightCredit != null)
                    {
                        travelCreditInfo.FutureFlightCreditDetails = futureFlightCredit;
                    }
                }
            }
            return travelCreditInfo;
        }

        private async Task<List<MOBTravelCertificateOrFlightCredit>> GetMPTravelCertificate(string mileagePlusNumber, MOBApplication application, string sessionId, string transactionId, string deviceId)
        {
            List<MOBTravelCertificateOrFlightCredit> response = null;

            if (!string.IsNullOrEmpty(mileagePlusNumber) && !string.IsNullOrEmpty(transactionId))
            {
                var ETCList = await GetTravelCertificateResponseFromETC(mileagePlusNumber, application, sessionId, transactionId, deviceId);
                if (ETCList != null && ETCList.WSException != null && !string.IsNullOrEmpty(ETCList.WSException.Code) && !string.IsNullOrEmpty(ETCList.WSException.Message) && ETCList.WSException.Code.Equals("E0000") & ETCList.WSException.Message.Equals("Success") && ETCList.PINDetails != null && ETCList.PINDetails.Any())
                {
                    response = new List<MOBTravelCertificateOrFlightCredit>();
                    var travelCertificateDetailInfo = !string.IsNullOrEmpty(_configuration.GetValue<string>("ETCMyAccountTravelCertificateDetailInfo")) ? _configuration.GetValue<string>("ETCMyAccountTravelCertificateDetailInfo").Split(',') : "Travel certificate,Value,Issue date,PIN,Traveler,Promo code".Split(',');
                    var culture = TopHelper.GetCultureInfo("");
                    #region 

                    foreach (PINDetail pinDetail in ETCList.PINDetails)
                    {
                        if (pinDetail != null && !string.IsNullOrEmpty(pinDetail.CertExpDate) && !string.IsNullOrEmpty(pinDetail.CurrentValue) && !string.IsNullOrEmpty(pinDetail.CertPin) && !string.IsNullOrEmpty(pinDetail.FirstName)
                           && !string.IsNullOrEmpty(pinDetail.LastName) && !string.IsNullOrEmpty(pinDetail.PromoID) && !string.IsNullOrEmpty(pinDetail.OrigIssueDate) && EnableDetailForGracePeriod(pinDetail.CertExpDate, "ETCMyAccountTravelerCertificateGracePeriod"))
                        {
                            MOBTravelCertificateOrFlightCredit travelCertificate = new MOBTravelCertificateOrFlightCredit();
                            travelCertificate.FutureFlightCreditLink = string.Empty;
                            travelCertificate.CertficateExpiryDate = Convert.ToDateTime(pinDetail.CertExpDate);
                            travelCertificate.TravelCreditInfoHeader = new List<MOBETCDetail>()
                                {
                                   new MOBETCDetail()
                                   {
                                       Key = travelCertificateDetailInfo[0],
                                       Value = new List<string>()
                                       {
                                         TopHelper.FormatAmountForDisplay(pinDetail.CurrentValue, culture, false)
                                       }
                                   },
                                   new MOBETCDetail()
                                   {
                                       Key = _configuration.GetValue<string>("ETCMyAccountTravelCertificateExpireDateText") + " " +FormatDateForETCCredit(pinDetail.CertExpDate),
                                       Value = new List<string>()
                                       {
                                          travelCertificateDetailInfo[1]
                                       }
                                   }

                                };
                            travelCertificate.TravelCreditInfoBody = new List<MOBETCDetail>()
                                {
                                    new MOBETCDetail
                                    {
                                        Key = travelCertificateDetailInfo[2],
                                        Value = new List<string>
                                        {
                                            FormatDateForETCCredit(pinDetail.OrigIssueDate)
                                        }
                                    },
                                new MOBETCDetail
                                    {
                                        Key = travelCertificateDetailInfo[3],
                                        Value = new List<string>
                                        {
                                            _configuration.GetValue<bool>("EnableTravelCertificateDetailsPinCheck") ? pinDetail.CertPin :  _configuration.GetValue<string>("ETCMyAccountMaskedPINForTravelcertificate")
                                        }
                                    },
                                new MOBETCDetail
                                    {
                                        Key = travelCertificateDetailInfo[4],
                                        Value = new List<string>
                                        {
                                            pinDetail.FirstName + " "+ pinDetail.LastName
                                        }
                                    },
                                new MOBETCDetail
                                    {
                                        Key = travelCertificateDetailInfo[5],
                                        Value = new List<string>
                                        {
                                            pinDetail.PromoID
                                        }
                                    }
                                };
                            response.Add(travelCertificate);
                        }
                    }

                    #endregion
                }
            }
            return response;
        }

        public string FormatDateForETCCredit(string date)
        {
            string expireDate = string.Empty;
            if (!string.IsNullOrEmpty(date))
            {
                DateTime datetime;
                DateTime.TryParse(date, out datetime);
                expireDate = (datetime != null) ? datetime.ToString("MMM dd, yyyy") : string.Empty;
            }
            return expireDate;
        }

        public bool EnableDetailForGracePeriod(string travelCertificateExpiryDate, string configKey)
        {
            bool showCertificate = false;
            if (!string.IsNullOrEmpty(travelCertificateExpiryDate))
            {
                DateTime travelCertificateExpiryDateObj;
                DateTime.TryParse(travelCertificateExpiryDate, out travelCertificateExpiryDateObj);
                int days = _configuration.GetValue<int>(configKey);
                if (travelCertificateExpiryDateObj >= DateTime.Now)
                    return true;
                else if (days > 0 && travelCertificateExpiryDateObj < DateTime.Now)
                {
                    double day = (DateTime.Now - travelCertificateExpiryDateObj).TotalDays;
                    return Convert.ToInt32(day) <= days ? true : showCertificate;
                }
            }
            return showCertificate;
        }

        public async Task<ETCReturnType> GetTravelCertificateResponseFromETC(string mileagePlusNumber, MOBApplication application, string sessionId, string transactionId, string deviceId)
        {
            ETCReturnType response = null;

            string request = _configuration.GetValue<string>("ETCMyAccountTravelCertificateRequest");
            var travelCertificateServiceRequestParameters = !string.IsNullOrEmpty(request) ? request.Split(',') : "IAH,RF,112233,112233".Split(',');

            try
            {
                response = await _eTCService.ETCSearchByFreqFlyerNum(mileagePlusNumber, travelCertificateServiceRequestParameters[0], travelCertificateServiceRequestParameters[1], travelCertificateServiceRequestParameters[2], travelCertificateServiceRequestParameters[3], _configuration.GetValue<string>("ETCMyAccountTCAccessCode"), sessionId).ConfigureAwait(false);

            }
            catch (Exception)
            { }

            return response;

        }

        private async Task<ServiceResponse> GetActivityFromQualificationService(MPAccountValidationRequest req, string dpToken)
        {
            string sDate = DateTime.Now.ToString("MM/dd/yyyy");
            if (_configuration.GetValue<bool>("DisplayYearEndPremierActivity"))
            {
                if (!string.IsNullOrEmpty(_configuration.GetValue<string>("YearEndPremierActivityMP")) && _configuration.GetValue<string>("YearEndPremierActivityMP").Split(';').ToList().Contains(req.MileagePlusNumber))
                {
                    sDate = Convert.ToDateTime(_configuration.GetValue<string>("YearEndPremierActivityMPDate")).ToString("MM/dd/yyyy");
                }
                else if (!string.IsNullOrEmpty(_configuration.GetValue<string>("YearEndPremierActivityMP2")) && _configuration.GetValue<string>("YearEndPremierActivityMP2").Split(';').ToList().Contains(req.MileagePlusNumber))
                {
                    sDate = Convert.ToDateTime(_configuration.GetValue<string>("YearEndPremierActivityMP2Date")).ToString("MM/dd/yyyy");
                }
                else if (!string.IsNullOrEmpty(_configuration.GetValue<string>("YearEndPremierActivityMP3")) && _configuration.GetValue<string>("YearEndPremierActivityMP3").Split(';').ToList().Contains(req.MileagePlusNumber))
                {
                    sDate = Convert.ToDateTime(_configuration.GetValue<string>("YearEndPremierActivityMP3Date")).ToString("MM/dd/yyyy");
                }
            }

            ServiceResponse serviceResponse = new ServiceResponse();
            string jsonResponse = string.Empty;
            try
            {
                //if (_configuration.GetValue<string>("ByPassGetPremierActivityRequestValidationnGetCachedDPTOken") == true) // If the value of ByPassGetPremierActivityRequestValidationnGetCachedDPTOken is true then go get FLIFO Dp Token which we currenlty used by Flight Status NOTE: Alreday confirmed with Greg & Bob that they are not using internal to validate DP Token.

                dpToken =await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration).ConfigureAwait(false);

                serviceResponse = await _myAccountPremierService.GetAccountPremier<ServiceResponse>(dpToken, req.MileagePlusNumber, _configuration.GetValue<string>("PremierStatusActivityServiceAccessCode") ?? "QUAL-190D2B35-6C31-43A7-B3FD-C98E3B7F96A5", _headers.ContextValues.TransactionId).ConfigureAwait(false);

            }
            catch (System.Net.WebException wex)
            {
                if (wex.Response != null)
                {
                    throw new System.Exception(wex.Message);
                }
            }
            //serviceResponse = DataContextJsonSerializer.NewtonSoftDeserialize<ServiceResponse>(jsonResponse);
            return serviceResponse;
        }

        public async Task<MPAccountSummary> GetAccountSummaryV2(string transactionId, string mileagePlusNumber, string languageCode, bool includeMembershipCardBarCode, string sessionId = "")
        {
            MPAccountSummary mpSummary = new MPAccountSummary();

            //string LoyaltyMemberProfileServiceURL = _configuration.GetValue<string>("LoyaltyMemberProfileServiceURL");

            //string loyaltyProfileUrl = string.Format(LoyaltyMemberProfileServiceURL, mileagePlusNumber);
            transactionId = !String.IsNullOrEmpty(sessionId) ? sessionId : transactionId;

            // _logger.LogInformation("GetAccountSummary - Loyalty Get Profile Requst URL: {loyaltyProfileUrl} {transactionId}", loyaltyProfileUrl, transactionId);


            if (_configuration.GetValue<string>("AddingExtraLogggingForAwardShopAccountSummary_Toggle") != null
                && _configuration.GetValue<bool>("AddingExtraLogggingForAwardShopAccountSummary_Toggle"))
            {
                if (string.IsNullOrWhiteSpace(mileagePlusNumber))
                {
                    _logger.LogError("GetAccountSummary - Empty MPNumber Passed");
                }
            }

            try
            {
                bool fourSegmentMinimunWaivedMember = false;
                bool noMileageExpiration = false;
                string balanceExpireDisclaimer = string.Empty;
                Session session = new Session();
                session = await _sessionHelperService.GetSession<Session>(sessionId, session.ObjectName, new List<string> { sessionId, session.ObjectName });

                Mobile.Model.CSLModels.ReadMemberInformation loyaltyProfileResponse = new ReadMemberInformation();

                var csLCallDurationstopwatch = new Stopwatch();
                csLCallDurationstopwatch.Start();

                var jsonResponse = await _loyaltyAccountService.GetAccountProfile(session.Token, mileagePlusNumber, _headers.ContextValues.SessionId);

                if (jsonResponse != null)
                {
                    var responseData = JsonConvert.DeserializeObject<Mobile.Model.CSLModels.CslResponse<ReadMemberInformation>>(jsonResponse);
                    if (responseData != null && responseData.Errors == null)
                    {
                        loyaltyProfileResponse = responseData.Data;

                        #region 55359, 81220 Bug Fix
                        //55359 and 81220 to check for closed, temporary closed and ClosedPermanently account-Alekhya 
                        if (loyaltyProfileResponse != null && (loyaltyProfileResponse.IsClosedTemporarily == true || loyaltyProfileResponse.IsClosedPermanently == true))
                        {
                            string exceptionMessage = _configuration.GetValue<string>("ErrorContactMileagePlus").ToString();
                            throw new MOBUnitedException(exceptionMessage);
                        }
                        //Changes end here
                        #endregion 55359, 81220 Bug Fix
                        if (loyaltyProfileResponse.BirthYear > 0 && loyaltyProfileResponse.BirthMonth > 0 && loyaltyProfileResponse.BirthDate > 0)
                            mpSummary.BirthDate = (new DateTime(loyaltyProfileResponse.BirthYear, loyaltyProfileResponse.BirthMonth, loyaltyProfileResponse.BirthDate)).ToString();
                        var balance = loyaltyProfileResponse.Balances?.FirstOrDefault(balnc => (int)balnc.Currency == 5);
                        mpSummary.Balance = balance != null ? balance.Amount.ToString() : "0";
                        mpSummary.LastActivityDate = loyaltyProfileResponse.LastActivityDate != null ? loyaltyProfileResponse.LastActivityDate.ToString("MM/dd/yyyy") : "";
                        mpSummary.LearnMoreTitle = _configuration.GetValue<string>("MilagePluslearnMoreText");
                        mpSummary.LearnMoreHeader = _configuration.GetValue<string>("MilagePluslearnMoreDesc");
                        mpSummary.MilesNeverExpireText = _configuration.GetValue<string>("MilagePlusMilesNeverExpire");
                        if (_configuration.GetValue<bool>("HideMileageBalanceExpireDate") == true)
                        {
                            mpSummary.BalanceExpireDate = "";
                            mpSummary.IsHideMileageBalanceExpireDate = true;
                        }

                        mpSummary.BalanceExpireDisclaimer = _configuration.GetValue<bool>("HideMileageBalanceExpireDate") ? _configuration.GetValue<string>("BalanceExpireDisclaimerNeverExpire") : _configuration.GetValue<string>("BalanceExpireDisclaimer");
                        mpSummary.CustomerId = loyaltyProfileResponse.CustomerId;
                        var premierQualifyingSegmentBalance = loyaltyProfileResponse.PremierQualifyingMetrics?.FirstOrDefault(pqm => pqm.ProgramCurrency == "PQP");
                        if (premierQualifyingSegmentBalance != null)
                            mpSummary.EliteMileage = string.Format("{0:###,##0}", premierQualifyingSegmentBalance.Balance);
                        if (premierQualifyingSegmentBalance != null)
                        {
                            if (premierQualifyingSegmentBalance.Balance == 0)
                            {
                                mpSummary.EliteSegment = "0";
                            }
                            else
                            {
                                mpSummary.EliteSegment = string.Format("{0:0.#}", premierQualifyingSegmentBalance);
                            }
                        }
                        // test comments
                        mpSummary.EliteStatus = new MOBEliteStatus(_configuration);
                        mpSummary.EliteStatus.Code = loyaltyProfileResponse.MPTierLevel.ToString();
                        if (mpSummary.EnrollDate != null)
                            mpSummary.EnrollDate = loyaltyProfileResponse.EnrollDate.ToString("MM/dd/yyyy");
                        //mpSummary.HasUAClubMemberShip = loyaltyProfileResponse.AccountProfileInfo.IsUnitedClubMember;
                        //mpSummary.LastExpiredMileDate = DateTime.ParseExact(loyaltyProfileResponse.AccountProfileInfo.MilesExpireLastActivityDate.ToString(), "yyyyMMdd", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy");
                        mpSummary.LastFlightDate = loyaltyProfileResponse.LastFlightDate != null ? loyaltyProfileResponse.LastFlightDate.ToString("MM/dd/yyyy") : "";
                        mpSummary.MileagePlusNumber = loyaltyProfileResponse.MileageplusId;
                        mpSummary.Name = new MOBName();
                        mpSummary.Name.First = loyaltyProfileResponse.FirstName;
                        mpSummary.Name.Last = loyaltyProfileResponse.LastName;
                        mpSummary.Name.Middle = loyaltyProfileResponse.MiddleName;
                        mpSummary.Name.Suffix = loyaltyProfileResponse.Suffix;
                        mpSummary.Name.Title = loyaltyProfileResponse.Title;

                        mpSummary.IsCEO = loyaltyProfileResponse.CEO == true;

                        mpSummary.LifetimeMiles = Convert.ToInt32(loyaltyProfileResponse.LifetimeMiles);

                        if (loyaltyProfileResponse.MillionMilerLevel == 0)
                        {
                            mpSummary.MillionMilerIndicator = string.Empty;
                        }
                        else
                        {
                            mpSummary.MillionMilerIndicator = loyaltyProfileResponse.MillionMilerLevel.ToString();
                        }

                        if (Convert.ToDateTime(_configuration.GetValue<string>("MP2014EnableDate")) < DateTime.Now)
                        {
                            if (loyaltyProfileResponse != null)
                            {
                                bool isValidPqdAddress = false;
                                bool activeNonPresidentialPlusCardMember = false;
                                bool activePresidentialPlusCardMembe = false;
                                bool chaseSpending = false;
                                bool presidentialPlus = false;
                                bool showChaseBonusTile = false;

                                //Migrate XML to CSL service call
                                if (_configuration.GetValue<bool>("EnableCallingPQDService") && _configuration.GetValue<bool>("NewServieCall_GetProfile_PaymentInfos"))
                                {

                                   var tupleRes= await IsValidPQDAddressV2("GetAccountSummary", transactionId, session.Token, mpSummary.MileagePlusNumber,  isValidPqdAddress,  activeNonPresidentialPlusCardMember,  activePresidentialPlusCardMembe,  fourSegmentMinimunWaivedMember,  showChaseBonusTile);
                                    isValidPqdAddress = tupleRes.isValidPqdAddress;
                                    activeNonPresidentialPlusCardMember = tupleRes.activeNonPresidentialPlusCardMember;
                                    activePresidentialPlusCardMembe = tupleRes.activePresidentialPlusCardMembe;
                                    showChaseBonusTile = tupleRes.isValidPqdAddress;
                                    fourSegmentMinimunWaivedMember = tupleRes.fourSegmentMinimunWaivedMember;
                                }

                                mpSummary.ShowChaseBonusTile = showChaseBonusTile;
                                AssignCustomerChasePromoType(mpSummary, showChaseBonusTile);

                                noMileageExpiration = activeNonPresidentialPlusCardMember || activePresidentialPlusCardMembe;

                                if (fourSegmentMinimunWaivedMember)
                                {
                                    mpSummary.FourSegmentMinimun = "Waived";
                                }
                                else if (loyaltyProfileResponse.MinimumSegments >= 4)
                                {
                                    mpSummary.FourSegmentMinimun = "4 of 4";
                                }
                                else
                                {
                                    mpSummary.FourSegmentMinimun = string.Format("{0} of 4", loyaltyProfileResponse.MinimumSegments);
                                }
                                //TODO : UCB
                                //if (loyaltyProfileResponse.AccountProfileInfo.PremierQualifyingDollars == 0 || !isValidPqdAddress)
                                //{
                                //    if (!isValidPqdAddress)
                                //    {
                                //        mpSummary.PremierQualifyingDollars = string.Empty;
                                //    }
                                //    else
                                //    {
                                //        mpSummary.PremierQualifyingDollars = "0";
                                //    }
                                //}
                                //else
                                //{
                                //    decimal pqd = 0;
                                //    try
                                //    {
                                //        pqd = Convert.ToDecimal(loyaltyProfileResponse.AccountProfileInfo.PremierQualifyingDollars);
                                //    }
                                //    catch (System.Exception) { }
                                //    //Below are the two toggles used in Appsettings 
                                //    //< add key = "PqdAmount" value = "12000" /> < add key = "PqdText" value = "Over $12,000" />
                                //    //Work Items LOYAL-3236, LOYAL-3241
                                //    if (!string.IsNullOrEmpty(_configuration.GetValue<string>["PqdAmount")) && !string.IsNullOrEmpty(_configuration.GetValue<string>["PqdText")))
                                //    {
                                //        if (pqd > Convert.ToDecimal(_configuration.GetValue<string>"PqdAmount")))
                                //        {
                                //            mpSummary.PremierQualifyingDollars = ConfigurationManager.AppSettings["PqdText");
                                //        }
                                //    }
                                //    else
                                //    {
                                //        mpSummary.PremierQualifyingDollars = pqd.ToString("C0");
                                //    }
                                //}


                                string pdqchasewaiverLabel = string.Empty;
                                string pdqchasewavier = string.Empty;
                                if (isValidPqdAddress)
                                {
                                    if (loyaltyProfileResponse.IsChaseSpend)
                                    {
                                        chaseSpending = true;
                                    }
                                    // PresidentialPlusIndicator is depricated and no longer been used
                                    //if (!string.IsNullOrEmpty(loyaltyProfileResponse.AccountProfileInfo.PresidentialPlusIndicator) && loyaltyProfileResponse.AccountProfileInfo.PresidentialPlusIndicator.Equals("Y"))
                                    //{
                                    //    presidentialPlus = true;
                                    //}

                                    //[CLEANUP API-MIGRATION]
                                    //if (!_configuration.GetValue<bool>("IsAvoidUAWSChaseMessagingSoapCall"))
                                    //{
                                    //    GetChaseMessage(activeNonPresidentialPlusCardMember, activePresidentialPlusCardMembe, chaseSpending, presidentialPlus, ref pdqchasewaiverLabel, ref pdqchasewavier, ref balanceExpireDisclaimer);
                                    //    mpSummary.PDQchasewaiverLabel = pdqchasewaiverLabel;
                                    //    mpSummary.PDQchasewavier = pdqchasewavier;
                                    //}
                                    if (!string.IsNullOrEmpty(balanceExpireDisclaimer) && !_configuration.GetValue<bool>("HideMileageBalanceExpireDate"))
                                    {
                                        mpSummary.BalanceExpireDisclaimer = mpSummary.BalanceExpireDisclaimer + " " + balanceExpireDisclaimer;
                                    }
                                }
                                if (!fourSegmentMinimunWaivedMember && !_configuration.GetValue<bool>("HideMileageBalanceExpireDate"))
                                {
                                    mpSummary.BalanceExpireDisclaimer = mpSummary.BalanceExpireDisclaimer + " " + _configuration.GetValue<string>("FouSegmentMessage");
                                }
                            }
                        }
                        // MOBILE-24412
                        if (_configuration.GetValue<bool>("EnableShopChaseCardPaxInfoFix"))
                        {
                            var chaseCardTypes = _configuration.GetValue<string>("ChaseCardTypesForStrikeThrough") ?? "";
                            if ((loyaltyProfileResponse?.PartnerCards?.Count ?? 0) > 0)
                            {
                                mpSummary.IsChaseCardHolder = loyaltyProfileResponse.PartnerCards.Exists(p => p.PartnerCode == "CH" && chaseCardTypes.Contains(p.CardType));
                            }
                        }

                        // same with Line 3163
                        //_logger.LogInformation("Loyalty Get Profile Deserialised Response, {SessionId} {@loyaltyProfileResponse}", session.SessionId, loyaltyProfileResponse);
                    }
                }

                if (includeMembershipCardBarCode)
                {
                    //%B[\w]{2,3}\d{5,6}\s{7}\d{4}(GL|1K|GS|PL|SL|\s\s)\s\s[\s\w\<\-\'\.]{35}\sUA
                    string eliteLevel = "";
                    switch (mpSummary.EliteStatus.Level)
                    {
                        case 0:
                            eliteLevel = "  ";
                            break;
                        case 1:
                            eliteLevel = "SL";
                            break;
                        case 2:
                            eliteLevel = "GL";
                            break;
                        case 3:
                            eliteLevel = "PL";
                            break;
                        case 4:
                            eliteLevel = "1K";
                            break;
                        case 5:
                            eliteLevel = "GS";
                            break;
                    }
                    string name = string.Format("{0}<{1}", mpSummary.Name.Last, mpSummary.Name.First);
                    if (!string.IsNullOrEmpty(mpSummary.Name.Middle))
                    {
                        name = name + " " + mpSummary.Name.Middle.Substring(0, 1);
                    }
                    name = String.Format("{0, -36}", name);

                    bool hasUnitedClubMemberShip = false;
                    mpSummary.uAClubMemberShipDetails = await GetUnitedClubMembershipDetails(mpSummary.MileagePlusNumber, hasUnitedClubMemberShip, languageCode);
                    mpSummary.HasUAClubMemberShip = hasUnitedClubMemberShip;
                    //if (hasUnitedClubMemberShip)
                    //{
                    mpSummary.MembershipCardExpirationDate = await this.GetMembershipCardExpirationDate(mpSummary.MileagePlusNumber, mpSummary.EliteStatus.Level);
                    string expirationDate = _configuration.GetValue<string>("MPCardExpirationDate");
                    try
                    {
                        if (mpSummary.MembershipCardExpirationDate.Equals("Trial Status"))
                        {
                            expirationDate = string.Format("12/{0}", DateTime.Today.Year);
                        }
                        else
                        {
                            expirationDate = string.IsNullOrEmpty(mpSummary.MembershipCardExpirationDate) ? _configuration.GetValue<string>("MPCardExpirationDate") : mpSummary.MembershipCardExpirationDate.Substring(11, 2) + mpSummary.MembershipCardExpirationDate.Substring(16, 2);
                        }
                    }
                    catch (System.Exception) { }

                    //string expirationDate = ConfigurationManager.AppSettings["MPCardExpirationDate");
                    string barCodeData = string.Format("%B{0}       {1}{2}  {3}UA              ?", mpSummary.MileagePlusNumber, expirationDate, eliteLevel, name);
                    string allianceTierLevel = "   ";
                    switch (mpSummary.EliteStatus.Level)
                    {
                        case 0:
                            allianceTierLevel = "   ";
                            break;
                        case 1:
                            allianceTierLevel = "UAS";
                            break;
                        case 2:
                            allianceTierLevel = "UAG";
                            break;
                        case 3:
                            allianceTierLevel = "UAG";
                            break;
                        case 4:
                            allianceTierLevel = "UAG";
                            break;
                        case 5:
                            allianceTierLevel = "UAG";
                            break;
                    }
                    string allianceTierLevelExpirationDate = "    ";
                    if (!allianceTierLevel.Equals("   "))
                    {
                        if (mpSummary.MembershipCardExpirationDate.Equals("Trial Status"))
                        {
                            allianceTierLevelExpirationDate = string.Format("12/{0}", DateTime.Today.Year);
                        }
                        else
                        {
                            allianceTierLevelExpirationDate = string.IsNullOrEmpty(mpSummary.MembershipCardExpirationDate) ? _configuration.GetValue<string>("MPCardExpirationDate") : mpSummary.MembershipCardExpirationDate.Substring(11, 2) + mpSummary.MembershipCardExpirationDate.Substring(16, 2);
                        }
                    }

                    string paidLoungeIndicator = "N";
                    string paidLoungeExpireationDate = "      ";
                    if (mpSummary.uAClubMemberShipDetails != null)
                    {
                        if (string.IsNullOrEmpty(mpSummary.uAClubMemberShipDetails.PrimaryOrCompanion))
                        {
                            paidLoungeIndicator = "P";
                        }
                        else
                        {
                            paidLoungeIndicator = mpSummary.uAClubMemberShipDetails.PrimaryOrCompanion;
                        }
                        paidLoungeExpireationDate = Convert.ToDateTime(mpSummary.uAClubMemberShipDetails.DiscontinueDate).ToString("ddMMyyyy");
                    }

                    string starBarCodeData = string.Format("FFPC001UA{0}{1}{2}{3}{4}{5}{6}{7}^001", mpSummary.MileagePlusNumber.PadRight(16), mpSummary.Name.Last.PadRight(20), mpSummary.Name.First.PadRight(20), allianceTierLevel, allianceTierLevelExpirationDate, eliteLevel, paidLoungeIndicator, paidLoungeExpireationDate);

                    if (!string.IsNullOrEmpty(_configuration.GetValue<string>("ReturnMPMembershipBarcode")) && Convert.ToBoolean(_configuration.GetValue<string>("ReturnMPMembershipBarcode")))
                    {
                        if (!string.IsNullOrEmpty(_configuration.GetValue<string>("UseStarMembershipCardFormatDateTime")) && Convert.ToDateTime(_configuration.GetValue<string>("UseStarMembershipCardFormatDateTime")) <= DateTime.Now)
                        {
                            mpSummary.MembershipCardBarCodeString = starBarCodeData.ToUpper();
                            mpSummary.MembershipCardBarCode = GetBarCode(starBarCodeData);
                        }
                        else
                        {
                            mpSummary.MembershipCardBarCodeString = barCodeData.ToUpper();
                            mpSummary.MembershipCardBarCode = GetBarCode(barCodeData);
                        }
                    }
                    else
                    {
                        mpSummary.MembershipCardBarCodeString = null;
                        mpSummary.MembershipCardBarCode = null;
                    }
                    //}
                }

                //bool noMileageExpiration = HasChaseNoMileageExpirationCard(mpSummary.MileagePlusNumber);
                mpSummary.NoMileageExpiration = noMileageExpiration.ToString();

                if (_configuration.GetValue<bool>("HideMileageBalanceExpireDate"))

                {
                    mpSummary.NoMileageExpirationMessage = _configuration.GetValue<string>("BalanceExpireDisclaimerNeverExpire");
                }
                else if (noMileageExpiration)
                {
                    mpSummary.NoMileageExpirationMessage = _configuration.GetValue<string>("ChaseNoMileageExpirationMessage") + " " + balanceExpireDisclaimer;
                    if (!fourSegmentMinimunWaivedMember)
                    {
                        mpSummary.NoMileageExpirationMessage = mpSummary.NoMileageExpirationMessage + " " + _configuration.GetValue<string>("FouSegmentMessage");
                    }
                }
            }
            catch (MOBUnitedException ex)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                throw;
            }

            _logger.LogInformation("Loyalty Get Profile Response to client {@MpSummary}", JsonConvert.SerializeObject(mpSummary));


            if (_configuration.GetValue<string>("StartDateTimeToReturnEmptyMPExpirationDate") != null && _configuration.GetValue<String>("EndtDateTimeToReturnEmptyMPExpirationDate") != null && DateTime.ParseExact(_configuration.GetValue<String>("StartDateTimeToReturnEmptyMPExpirationDate").ToString().ToUpper().Trim(), "MM/dd/yyyy hh:mm tt", CultureInfo.InvariantCulture) <= DateTime.Now && DateTime.ParseExact(_configuration.GetValue<String>("EndtDateTimeToReturnEmptyMPExpirationDate").ToString().ToUpper().Trim(), "MM/dd/yyyy hh:mm tt", CultureInfo.InvariantCulture) >= DateTime.Now)
            {
                mpSummary.MembershipCardExpirationDate = string.Empty;
            }
            return mpSummary;
        }

        public async Task<MOBClubMembership> GetCurrentMembershipInfo(string mpNumber)
        {
            MOBClubMembership mobClubMembership = null;
            string jsonResponse = await _unitedClubMembershipV2Service.GetCurrentMembershipInfo(mpNumber, string.Empty);

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                List<UClubMembershipInfo> uClubMembershipInfoList = Newtonsoft.Json.JsonConvert.DeserializeObject<List<UClubMembershipInfo>>(jsonResponse);
                if (uClubMembershipInfoList != null && uClubMembershipInfoList.Count > 0)
                {
                    foreach (UClubMembershipInfo uClubMembershipInfo in uClubMembershipInfoList)
                    {
                        if (uClubMembershipInfo.DiscontinueDate >= DateTime.Now && string.IsNullOrEmpty(uClubMembershipInfo.ClubStatusCode))
                        {
                            mobClubMembership = new MOBClubMembership();
                            mobClubMembership.CompanionMPNumber = string.IsNullOrEmpty(uClubMembershipInfo.CompanionMpNumber) ? string.Empty : uClubMembershipInfo.CompanionMpNumber;
                            mobClubMembership.EffectiveDate = uClubMembershipInfo.EffectiveDate.ToString("MM/dd/yyyy");
                            mobClubMembership.ExpirationDate = uClubMembershipInfo.DiscontinueDate.ToString("MM/dd/yyyy");
                            mobClubMembership.IsPrimary = string.IsNullOrEmpty(uClubMembershipInfo.PrimaryOrCompanion) ? true : false;
                            mobClubMembership.MembershipTypeCode = string.IsNullOrEmpty(uClubMembershipInfo.MemberTypeCode) ? string.Empty : uClubMembershipInfo.MemberTypeCode;
                            mobClubMembership.MembershipTypeDescription = string.IsNullOrEmpty(uClubMembershipInfo.MemberTypeDescription) ? string.Empty : uClubMembershipInfo.MemberTypeDescription;
                        }
                    }
                }
            }
            return mobClubMembership;
        }

        public async Task<MOBClubMembership> GetCurrentMembershipInfoV2(string mpNumber, string Token)
        {
            MOBClubMembership mobClubMembership = null;
            string jsonResponse = await _unitedClubMembershipService.GetCurrentMembershipInfoV2(mpNumber, Token);

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                var responseData = Newtonsoft.Json.JsonConvert.DeserializeObject<Mobile.Model.CSLModels.CslResponse<UClubHistoryResponse>>(jsonResponse);
                if (responseData?.Data != null && responseData?.Errors == null)
                {
                    List<UClubMembershipData> uClubMembershipInfoList = responseData?.Data?.UClubMembershipData;
                    if (uClubMembershipInfoList != null && uClubMembershipInfoList.Count > 0)
                    {
                        foreach (UClubMembershipData uClubMembershipInfo in uClubMembershipInfoList)
                        {
                            if (uClubMembershipInfo.DiscontinueDate >= DateTime.Now && string.IsNullOrEmpty(uClubMembershipInfo.ClubStatusCode))
                            {
                                mobClubMembership = new MOBClubMembership();
                                mobClubMembership.CompanionMPNumber = string.IsNullOrEmpty(uClubMembershipInfo.CompanionMpNumber) ? string.Empty : uClubMembershipInfo.CompanionMpNumber;
                                mobClubMembership.EffectiveDate = uClubMembershipInfo.EffectiveDate.ToString("MM/dd/yyyy");
                                mobClubMembership.ExpirationDate = uClubMembershipInfo.DiscontinueDate.ToString("MM/dd/yyyy");
                                mobClubMembership.IsPrimary = string.IsNullOrEmpty(uClubMembershipInfo.PrimaryOrCompanion) ? true : false;
                                mobClubMembership.MembershipTypeCode = string.IsNullOrEmpty(uClubMembershipInfo.MemberTypeCode) ? string.Empty : uClubMembershipInfo.MemberTypeCode;
                                mobClubMembership.MembershipTypeDescription = string.IsNullOrEmpty(uClubMembershipInfo.MemberTypeDescription) ? string.Empty : uClubMembershipInfo.MemberTypeDescription;
                            }
                        }
                    }
                }
            }
            return mobClubMembership;
        }

        //TO-DO: Rajesh K need to visit
        public async Task<MPAccountSummary> GetAccountSummaryWithPremierActivityV2(MPAccountValidationRequest req, bool includeMembershipCardBarCode, string dpToken)
        {

            MPAccountSummary mpSummary = new MPAccountSummary();


            try
            {
                bool fourSegmentMinimunWaivedMember = false;
                ReadMemberInformation loyaltyProfileResponse = new ReadMemberInformation();
                ServiceResponse sResponse = new ServiceResponse();
                string balanceExpireDisclaimer = string.Empty;
                bool noMileageExpiration = false;
                var csLCallDurationstopwatch = new Stopwatch();
                csLCallDurationstopwatch.Start();

                var jsonResponse = await _loyaltyAccountService.GetAccountProfile(dpToken, req.MileagePlusNumber, _headers.ContextValues.SessionId);

                if (csLCallDurationstopwatch.IsRunning)
                {
                    csLCallDurationstopwatch.Stop();
                }



                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    var responseData = Newtonsoft.Json.JsonConvert.DeserializeObject<Mobile.Model.CSLModels.CslResponse<ReadMemberInformation>>(jsonResponse);
                    if (responseData.Data != null && responseData.Errors == null)
                    {

                        loyaltyProfileResponse = responseData.Data;
                        #region 55359, 81220 Bug Fix
                        if (loyaltyProfileResponse != null && (loyaltyProfileResponse.IsClosedTemporarily == true || loyaltyProfileResponse.IsClosedPermanently == true))
                        {
                            string exceptionMessage = _configuration.GetValue<string>("ErrorContactMileagePlus").ToString();
                            throw new MOBUnitedException(exceptionMessage);
                        }
                        #endregion 55359, 81220 Bug Fix
                        var balance = loyaltyProfileResponse.Balances?.FirstOrDefault(balnc => (int)balnc.Currency == 5);
                        mpSummary.Balance = balance != null ? balance.Amount.ToString() : "0";
                        mpSummary.LastActivityDate = loyaltyProfileResponse.LastActivityDate != null ? loyaltyProfileResponse.LastActivityDate.ToString("MM/dd/yyyy") : "";
                        mpSummary.LearnMoreTitle = _configuration.GetValue<string>("MileagePluslearnMoreText");
                        mpSummary.LearnMoreHeader = _configuration.GetValue<string>("MileagePluslearnMoreDesc");
                        mpSummary.MilesNeverExpireText = _configuration.GetValue<string>("MileagePlusMilesNeverExpire");
                        if (_configuration.GetValue<bool>("HideMileageBalanceExpireDate") == true)
                        {
                            mpSummary.BalanceExpireDate = "";
                            mpSummary.IsHideMileageBalanceExpireDate = true;
                        }

                        mpSummary.BalanceExpireDisclaimer = _configuration.GetValue<bool>("HideMileageBalanceExpireDate") ? _configuration.GetValue<string>("BalanceExpireDisclaimerNeverExpire") : _configuration.GetValue<string>("BalanceExpireDisclaimer");
                        mpSummary.CustomerId = loyaltyProfileResponse.CustomerId;
                        var premierQualifyingSegmentBalance = loyaltyProfileResponse.PremierQualifyingMetrics?.FirstOrDefault(pqm => pqm.ProgramCurrency == "PQP");
                        if (premierQualifyingSegmentBalance != null)
                            mpSummary.EliteMileage = string.Format("{0:###,##0}", premierQualifyingSegmentBalance.Balance);
                        if (premierQualifyingSegmentBalance != null)
                        {
                            if (premierQualifyingSegmentBalance.Balance == 0)
                            {
                                mpSummary.EliteSegment = "0";
                            }
                            else
                            {
                                mpSummary.EliteSegment = string.Format("{0:0.#}", premierQualifyingSegmentBalance);
                            }
                        }

                        mpSummary.EliteStatus = new MOBEliteStatus(_configuration);
                        mpSummary.EliteStatus.Code = loyaltyProfileResponse.MPTierLevel.ToString();
                        if (mpSummary.EnrollDate != null)
                            mpSummary.EnrollDate = loyaltyProfileResponse.EnrollDate.ToString("MM/dd/yyyy");
                        mpSummary.LastFlightDate = loyaltyProfileResponse.LastFlightDate != null ? loyaltyProfileResponse.LastFlightDate.ToString("MM/dd/yyyy") : "";
                        mpSummary.MileagePlusNumber = loyaltyProfileResponse.MileageplusId;
                        mpSummary.Name = new MOBName();
                        mpSummary.Name.First = loyaltyProfileResponse.FirstName;
                        mpSummary.Name.Last = loyaltyProfileResponse.LastName;
                        mpSummary.Name.Middle = loyaltyProfileResponse.MiddleName;
                        mpSummary.Name.Suffix = loyaltyProfileResponse.Suffix;
                        mpSummary.Name.Title = loyaltyProfileResponse.Title;
                        mpSummary.IsCEO = loyaltyProfileResponse.CEO == true;
                        mpSummary.LifetimeMiles = Convert.ToInt32(loyaltyProfileResponse.LifetimeMiles);

                        if (loyaltyProfileResponse.MillionMilerLevel == 0)
                        {
                            mpSummary.MillionMilerIndicator = string.Empty;
                        }
                        else
                        {
                            mpSummary.MillionMilerIndicator = loyaltyProfileResponse.MillionMilerLevel.ToString();
                        }

                        if (Convert.ToDateTime(_configuration.GetValue<string>("MP2014EnableDate")) < DateTime.Now)
                        {
                            if (loyaltyProfileResponse != null)
                            {
                                bool isValidPqdAddress = false;
                                bool activeNonPresidentialPlusCardMember = false;
                                bool activePresidentialPlusCardMembe = false;
                                bool chaseSpending = false;
                                bool presidentialPlus = false;
                                bool showChaseBonusTile = false;

                                //Migrate XML to CSL service call
                                if (_configuration.GetValue<bool>("EnableCallingPQDService") && _configuration.GetValue<bool>("NewServieCall_GetProfile_PaymentInfos"))
                                {
                                   var tupleRes=await IsValidPQDAddressV2("GetAccountSummaryWithPremierActivity", req.TransactionId, dpToken, mpSummary.MileagePlusNumber, isValidPqdAddress,  activeNonPresidentialPlusCardMember,  activePresidentialPlusCardMembe,  fourSegmentMinimunWaivedMember,  showChaseBonusTile);
                                    isValidPqdAddress = tupleRes.isValidPqdAddress;
                                    activeNonPresidentialPlusCardMember = tupleRes.activeNonPresidentialPlusCardMember;
                                    activePresidentialPlusCardMembe = tupleRes.activePresidentialPlusCardMembe;
                                    showChaseBonusTile = tupleRes.isValidPqdAddress;
                                    fourSegmentMinimunWaivedMember = tupleRes.fourSegmentMinimunWaivedMember;
                                }

                                mpSummary.ShowChaseBonusTile = showChaseBonusTile;
                                AssignCustomerChasePromoType(mpSummary, showChaseBonusTile);

                                noMileageExpiration = activeNonPresidentialPlusCardMember || activePresidentialPlusCardMembe;

                                if (fourSegmentMinimunWaivedMember)
                                {
                                    mpSummary.FourSegmentMinimun = "Waived";
                                }
                                else if (loyaltyProfileResponse.MinimumSegments >= 4)
                                {
                                    mpSummary.FourSegmentMinimun = "4 of 4";
                                }
                                else
                                {
                                    mpSummary.FourSegmentMinimun = string.Format("{0} of 4", loyaltyProfileResponse.MinimumSegments);
                                }
                                //TODO : UCB
                                //if (loyaltyProfileResponse.AccountProfileInfo.PremierQualifyingDollars == 0 || !isValidPqdAddress)
                                //{
                                //    if (!isValidPqdAddress)
                                //    {
                                //        mpSummary.PremierQualifyingDollars = string.Empty;
                                //    }
                                //    else
                                //    {
                                //        mpSummary.PremierQualifyingDollars = "0";
                                //    }
                                //}
                                //else
                                //{
                                //    decimal pqd = 0;
                                //    try
                                //    {
                                //        pqd = Convert.ToDecimal(loyaltyProfileResponse.AccountProfileInfo.PremierQualifyingDollars);
                                //    }
                                //    catch (System.Exception) { }

                                //    if (!string.IsNullOrEmpty(_configuration.GetValue<string>"PqdAmount"]) && !string.IsNullOrEmpty(_configuration.GetValue<string>"PqdText"]))
                                //    {
                                //        if (pqd > Convert.ToDecimal(_configuration.GetValue<string>"PqdAmount"]))
                                //        {
                                //            mpSummary.PremierQualifyingDollars = _configuration.GetValue<string>"PqdText");
                                //        }
                                //    }
                                //    else
                                //    {
                                //        mpSummary.PremierQualifyingDollars = pqd.ToString("C0");
                                //    }
                                //}
                                //to be removed
                                string pdqchasewaiverLabel = string.Empty;
                                string pdqchasewavier = string.Empty;
                                if (isValidPqdAddress)
                                {
                                    if (loyaltyProfileResponse.IsChaseSpend)
                                    {
                                        chaseSpending = true;
                                    }
                                    //No longer been use as this property is deprecated
                                    //if (!string.IsNullOrEmpty(loyaltyProfileResponse.AccountProfileInfo.PresidentialPlusIndicator) && loyaltyProfileResponse.AccountProfileInfo.PresidentialPlusIndicator.Equals("Y"))
                                    //{
                                    //    presidentialPlus = true;
                                    //}

                                    if (!_configuration.GetValue<bool>("IsAvoidUAWSChaseMessagingSoapCall"))
                                    {
                                        //GetChaseMessage(activeNonPresidentialPlusCardMember, activePresidentialPlusCardMembe, chaseSpending, presidentialPlus, ref pdqchasewaiverLabel, ref pdqchasewavier, ref balanceExpireDisclaimer);
                                        //mpSummary.PDQchasewaiverLabel = pdqchasewaiverLabel;
                                        //mpSummary.PDQchasewavier = pdqchasewavier;
                                    }

                                    if (!string.IsNullOrEmpty(balanceExpireDisclaimer) && !_configuration.GetValue<bool>("HideMileageBalanceExpireDate"))
                                    {
                                        mpSummary.BalanceExpireDisclaimer = mpSummary.BalanceExpireDisclaimer + " " + balanceExpireDisclaimer;
                                    }
                                }
                                //End to be removed
                                //GetPremierActivity
                                //long l = 0;
                                //l = GetIncrementalThresholdMiles(121000);
                                sResponse = await GetActivityFromQualificationService(req, dpToken);
                                if (sResponse != null && sResponse.Status != null && !string.IsNullOrEmpty(sResponse.Status.Code) && sResponse.Status.Code.Trim().ToUpper() == "E0000")
                                {
                                    //if ((!string.IsNullOrEmpty(sResponse.RunType) && sResponse.RunType.Trim().ToUpper() == "QL") || ShowActivity(req.MileagePlusNumber))
                                    //if (!string.IsNullOrEmpty(sResponse.RunType) && sResponse.RunType.Trim().ToUpper() == "QL")
                                    if (!string.IsNullOrEmpty(sResponse.YearEndIndicator) && sResponse.YearEndIndicator.Trim().ToUpper() == "Y")
                                    {
                                        mpSummary.yearEndPremierActivity = GetYearEndActivity(sResponse);
                                        mpSummary.PremierActivityType = 2;//YearEndActivity
                                    }
                                    else
                                    {
                                        if (_configuration.GetValue<bool>("ImplementForceUpdateLogicToggleAccountSummary") && !GeneralHelper.IsApplicationVersionGreater(req.Application.Id, req.Application.Version.Major, "ImplementForceUpdateLogicToggleAccountSummaryIOS", "ImplementForceUpdateLogicToggleAccountSummaryAndroid", "", "", true, _configuration))
                                        {
                                            mpSummary.PremierActivityType = 3;//ErrorActivity
                                            mpSummary.ErrorPremierActivity = new United.Mobile.Model.Common.ErrorPremierActivity()
                                            {
                                                ShowErrorIcon = true,
                                                ErrorPremierActivityTitle = _configuration.GetValue<string>("PremierActivityTitle"),
                                                ErrorPremierActivityText = _configuration.GetValue<string>("MyAccountForceUpdateMessage")
                                            };
                                            mpSummary.PremierStatusTrackerText = _configuration.GetValue<string>("viewpremierstatustracker");
                                            mpSummary.PremierStatusTrackerLink = _configuration.GetValue<string>("viewpremierstatustrackerlink");
                                            var captionsForError = await GetPremierActivityLearnAboutCaptions();
                                            if (captionsForError != null && captionsForError.Count > 0)
                                            {
                                                mpSummary.PremierTrackerLearnAboutTitle = GetValueFromList(captionsForError, "PremierTrackerLearnAboutTitle");
                                                mpSummary.PremierTrackerLearnAboutHeader = GetValueFromList(captionsForError, "PremierTrackerLearnAboutHeader");
                                                mpSummary.PremierTrackerLearnAboutText = GetValueFromList(captionsForError, "PremierTrackerLearnAboutText");
                                                mpSummary.PremierTrackerLearnAboutText = mpSummary.PremierTrackerLearnAboutText + GetValueFromList(captionsForError, "FullTermsAndConditions");
                                            }
                                            //if (!string.IsNullOrEmpty(Utility.GetConfigEntries("ShowWelcomeModel")) && Utility.GetBooleanConfigValue("ShowWelcomeModel") && !Utility.IsVBQWelcomeModelDisplayed(req.MileagePlusNumber, req.Application.Id, req.DeviceId))
                                            //{
                                            //    mpSummary.VBQWelcomeModel = new MOBVBQWelcomeModel();
                                            //}
                                            mpSummary.NoMileageExpiration = noMileageExpiration.ToString();
                                            if (_configuration.GetValue<bool>("HideMileageBalanceExpireDate"))
                                            {
                                                mpSummary.NoMileageExpirationMessage = _configuration.GetValue<string>("BalanceExpireDisclaimerNeverExpire");
                                            }
                                            else if (noMileageExpiration)
                                            {
                                                mpSummary.NoMileageExpirationMessage = _configuration.GetValue<string>("ChaseNoMileageExpirationMessage") + " " + balanceExpireDisclaimer;
                                                if (!fourSegmentMinimunWaivedMember)
                                                {
                                                    mpSummary.NoMileageExpirationMessage = mpSummary.NoMileageExpirationMessage + " " + _configuration.GetValue<string>("FouSegmentMessage");
                                                }
                                            }
                                            return mpSummary;
                                        }
                                        int trackingUpgradeType = 0;
                                        if (_configuration.GetValue<bool>("EnableVBQII") && IsVBQTrackerSupportedVersion(req.Application.Id, req.Application.Version.Major))
                                        {
                                            trackingUpgradeType = GetVBQTrackingUpgradeType(sResponse);
                                            if (trackingUpgradeType > 0)
                                            {
                                                mpSummary.vBQPremierActivity = GetVBQTrackingUpgradesActivity(sResponse, trackingUpgradeType);
                                            }
                                            else
                                            {
                                                mpSummary.vBQPremierActivity = GetVBQPremierActivity(sResponse);
                                            }
                                        }
                                        else
                                        {
                                            if (_configuration.GetValue<bool>("MyAccountEnableUpgrades") &&
                                             IsPremierStatusTrackerUpgradesSupportedVersion(req.Application.Id, req.Application.Version.Major))
                                            {
                                                trackingUpgradeType = GetTrackingUpgradeType(sResponse);
                                                if (trackingUpgradeType > 0)
                                                {
                                                    mpSummary.premierActivity = GetTrackingUpgradesActivity(sResponse, trackingUpgradeType);
                                                }
                                                else
                                                {
                                                    mpSummary.premierActivity = GetPremierActivity(sResponse, ref pdqchasewaiverLabel, ref pdqchasewavier);
                                                }
                                            }
                                        }
                                        if (trackingUpgradeType == 3)
                                        {
                                            mpSummary.IsIncrementalUpgrade = true;
                                        }

                                        mpSummary.PremierActivityType = 1;//PremierActivity
                                    }
                                }
                                else
                                {
                                    mpSummary.PremierActivityType = 3;//ErrorActivity
                                                                      //if (!string.IsNullOrEmpty(Utility.GetConfigEntries("ShowWelcomeModel")) && Utility.GetBooleanConfigValue("ShowWelcomeModel") && !Utility.IsVBQWelcomeModelDisplayed(req.MileagePlusNumber, req.Application.Id, req.DeviceId))
                                                                      //{
                                                                      //    mpSummary.VBQWelcomeModel = new MOBVBQWelcomeModel();
                                                                      //}
                                    mpSummary.ErrorPremierActivity = GetErrorPremierActivity(sResponse);
                                }
                                //Display PremierActivity for older versions all the time
                                if ((mpSummary.PremierActivityType == 2 || mpSummary.PremierActivityType == 3) && IsPremierStatusTrackerSupportedVersion(req.Application.Id, req.Application.Version.Major) == false)
                                {
                                    mpSummary.premierActivity = GetPremierActivity(sResponse, ref pdqchasewaiverLabel, ref pdqchasewavier);
                                }
                                var captions = await GetPremierActivityLearnAboutCaptions();
                                if (captions != null && captions.Count > 0)
                                {
                                    mpSummary.PremierTrackerLearnAboutTitle = GetValueFromList(captions, "PremierTrackerLearnAboutTitle");
                                    mpSummary.PremierTrackerLearnAboutHeader = GetValueFromList(captions, "PremierTrackerLearnAboutHeader");
                                    mpSummary.PremierTrackerLearnAboutText = GetValueFromList(captions, "PremierTrackerLearnAboutText");
                                    if (sResponse.CurrentYearActivity.FlexPremierQualifyingPoints > 0)
                                    {
                                        mpSummary.PremierTrackerLearnAboutText = mpSummary.PremierTrackerLearnAboutText + GetValueFromList(captions, "ChasePQPDescription");

                                    }
                                    mpSummary.PremierTrackerLearnAboutText = mpSummary.PremierTrackerLearnAboutText + GetValueFromList(captions, "FullTermsAndConditions");
                                }

                                mpSummary.PremierStatusTrackerText = _configuration.GetValue<string>("viewpremierstatustracker");
                                mpSummary.PremierStatusTrackerLink = _configuration.GetValue<string>("viewpremierstatustrackerlink");
                                mpSummary.PlusPoints = _configuration.GetValue<bool>("EnablePlusPointsSummary") ? await GetPlusPointsFromLoyaltyBalanceService(req, dpToken) : null;
                                if (!fourSegmentMinimunWaivedMember && !_configuration.GetValue<bool>("HideMileageBalanceExpireDate"))
                                {
                                    mpSummary.BalanceExpireDisclaimer = mpSummary.BalanceExpireDisclaimer + " " + _configuration.GetValue<string>("FouSegmentMessage");
                                }
                            }
                        }
                    }
                }


                if (includeMembershipCardBarCode)
                {
                    //%B[\w]{2,3}\d{5,6}\s{7}\d{4}(GL|1K|GS|PL|SL|\s\s)\s\s[\s\w\<\-\'\.]{35}\sUA
                    string eliteLevel = "";
                    switch (mpSummary.EliteStatus.Level)
                    {
                        case 0:
                            eliteLevel = "  ";
                            break;
                        case 1:
                            eliteLevel = "SL";
                            break;
                        case 2:
                            eliteLevel = "GL";
                            break;
                        case 3:
                            eliteLevel = "PL";
                            break;
                        case 4:
                            eliteLevel = "1K";
                            break;
                        case 5:
                            eliteLevel = "GS";
                            break;
                    }
                    string name = string.Format("{0}<{1}", mpSummary.Name.Last, mpSummary.Name.First);
                    if (!string.IsNullOrEmpty(mpSummary.Name.Middle))
                    {
                        name = name + " " + mpSummary.Name.Middle.Substring(0, 1);
                    }
                    name = String.Format("{0, -36}", name);

                    bool hasUnitedClubMemberShip = false;
                    mpSummary.uAClubMemberShipDetails = await GetUnitedClubMembershipDetails(mpSummary.MileagePlusNumber, hasUnitedClubMemberShip, req.LanguageCode, dpToken);
                    mpSummary.HasUAClubMemberShip = hasUnitedClubMemberShip;
                    mpSummary.MembershipCardExpirationDate = await this.GetMembershipCardExpirationDate(mpSummary.MileagePlusNumber, mpSummary.EliteStatus.Level);

                    bool Keep_MREST_MP_EliteLevel_Expiration_Logic = _configuration.GetValue<bool>("Keep_MREST_MP_EliteLevel_Expiration_Logic");
                    string expirationDate = _configuration.GetValue<string>("MPCardExpirationDate");
                    try
                    {
                        if (!Keep_MREST_MP_EliteLevel_Expiration_Logic && sResponse != null)
                        {
                            #region
                            if (!_configuration.GetValue<bool>("Reset_MembershipCardExpirationDate"))
                            {
                                mpSummary.MembershipCardExpirationDate = string.Empty; // Always return the expiration date based on Premier QUAL service #LOYAL-6376
                            }
                            if (sResponse != null && sResponse.CurrentPremierLevel > 0)
                            {
                                DateTime dDate;
                                DateTime.TryParse(_configuration.GetValue<string>("DefaultMileagePlusCardExpirationDateReturnedByQaulSerivceForGeneralMember"), out dDate); //  <add key="DefaultMileagePlusCardExpirationDateReturnedByQaulSerivceForGeneralMember" value="1753-01-01"/>
                                if (sResponse.CurrentPremierLevelExpirationDate.Equals(dDate))
                                {
                                    if (sResponse.CurrentYearInstantElite != null &&
                                        !string.IsNullOrEmpty(sResponse.CurrentYearInstantElite.ConsolidatedCode) &&
                                       sResponse.CurrentYearInstantElite.ExpirationDate != null &&
                                     (sResponse.CurrentYearInstantElite.ConsolidatedCode.ToUpper().Trim() == "TRIAL"))
                                    {
                                        expirationDate = GetMPExpirationDateFromProfileAndQualService(mpSummary, string.Empty, sResponse.CurrentYearInstantElite.ConsolidatedCode, Convert.ToString(sResponse.CurrentYearInstantElite.ExpirationDate), expirationDate);
                                    }
                                }
                                else
                                {
                                    string currentPremierLevelExpirationDate = sResponse.CurrentPremierLevelExpirationDate != null ? Convert.ToString(sResponse.CurrentPremierLevelExpirationDate) : string.Empty;
                                    string consolidatedCode = sResponse.CurrentYearInstantElite != null && !string.IsNullOrEmpty(sResponse.CurrentYearInstantElite.ConsolidatedCode) ? sResponse.CurrentYearInstantElite.ConsolidatedCode : string.Empty;
                                    string currentYearInstantEliteExpirationDate = sResponse.CurrentYearInstantElite != null && sResponse.CurrentYearInstantElite.ExpirationDate != null ? Convert.ToString(sResponse.CurrentYearInstantElite.ExpirationDate) : string.Empty;
                                    bool isPermierLevelExpirationDateGreaterThanCurrentDate = !string.IsNullOrEmpty(currentPremierLevelExpirationDate) ? sResponse.CurrentPremierLevelExpirationDate > DateTime.Now : false;
                                    if (!isPermierLevelExpirationDateGreaterThanCurrentDate)
                                    {
                                        currentPremierLevelExpirationDate = string.Empty;
                                    }
                                    expirationDate = GetMPExpirationDateFromProfileAndQualService(mpSummary, currentPremierLevelExpirationDate, consolidatedCode, currentYearInstantEliteExpirationDate, expirationDate);
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            mpSummary.MembershipCardExpirationDate = await this.GetMembershipCardExpirationDate(mpSummary.MileagePlusNumber, mpSummary.EliteStatus.Level);
                            if (mpSummary.MembershipCardExpirationDate.Equals("Trial Status"))
                            {
                                expirationDate = string.Format("12/{0}", DateTime.Today.Year);
                            }
                            else
                            {
                                expirationDate = string.IsNullOrEmpty(mpSummary.MembershipCardExpirationDate) ? _configuration.GetValue<string>("MPCardExpirationDate") : mpSummary.MembershipCardExpirationDate.Substring(11, 2) + mpSummary.MembershipCardExpirationDate.Substring(16, 2);
                            }
                        }
                    }
                    catch (System.Exception) { }

                    string barCodeData = string.Format("%B{0}       {1}{2}  {3}UA              ?", mpSummary.MileagePlusNumber, expirationDate, eliteLevel, name);
                    string allianceTierLevel = "   ";
                    switch (mpSummary.EliteStatus.Level)
                    {
                        case 0:
                            allianceTierLevel = "   ";
                            break;
                        case 1:
                            allianceTierLevel = "UAS";
                            break;
                        case 2:
                            allianceTierLevel = "UAG";
                            break;
                        case 3:
                            allianceTierLevel = "UAG";
                            break;
                        case 4:
                            allianceTierLevel = "UAG";
                            break;
                        case 5:
                            allianceTierLevel = "UAG";
                            break;
                    }
                    string allianceTierLevelExpirationDate = "    ";
                    if (!allianceTierLevel.Equals("   "))
                    {
                        if (!Keep_MREST_MP_EliteLevel_Expiration_Logic && sResponse != null)
                        {
                            allianceTierLevelExpirationDate = expirationDate;
                        }
                        else
                        {
                            if (mpSummary.MembershipCardExpirationDate.Equals("Trial Status"))
                            {
                                allianceTierLevelExpirationDate = string.Format("12/{0}", DateTime.Today.Year);
                            }
                            else
                            {
                                allianceTierLevelExpirationDate = string.IsNullOrEmpty(mpSummary.MembershipCardExpirationDate) ? _configuration.GetValue<string>("MPCardExpirationDate") : mpSummary.MembershipCardExpirationDate.Substring(11, 2) + mpSummary.MembershipCardExpirationDate.Substring(16, 2);
                            }
                        }
                    }

                    string paidLoungeIndicator = "N";
                    string paidLoungeExpireationDate = "      ";
                    if (mpSummary.uAClubMemberShipDetails != null)
                    {
                        if (string.IsNullOrEmpty(mpSummary.uAClubMemberShipDetails.PrimaryOrCompanion))
                        {
                            paidLoungeIndicator = "P";
                        }
                        else
                        {
                            paidLoungeIndicator = mpSummary.uAClubMemberShipDetails.PrimaryOrCompanion;
                        }
                        paidLoungeExpireationDate = Convert.ToDateTime(mpSummary.uAClubMemberShipDetails.DiscontinueDate).ToString("ddMMyyyy");
                    }

                    string starBarCodeData = string.Format("FFPC001UA{0}{1}{2}{3}{4}{5}{6}{7}^001", mpSummary.MileagePlusNumber.PadRight(16), mpSummary.Name.Last.PadRight(20), mpSummary.Name.First.PadRight(20), allianceTierLevel, allianceTierLevelExpirationDate, eliteLevel, paidLoungeIndicator, paidLoungeExpireationDate);

                    if (!string.IsNullOrEmpty(_configuration.GetValue<string>("ReturnMPMembershipBarcode")) && Convert.ToBoolean(_configuration.GetValue<string>("ReturnMPMembershipBarcode")))
                    {
                        if (!string.IsNullOrEmpty(_configuration.GetValue<string>("UseStarMembershipCardFormatDateTime")) && Convert.ToDateTime(_configuration.GetValue<string>("UseStarMembershipCardFormatDateTime")) <= DateTime.Now)
                        {
                            mpSummary.MembershipCardBarCodeString = starBarCodeData.ToUpper();
                            mpSummary.MembershipCardBarCode = GetBarCode(starBarCodeData);
                        }
                        else
                        {
                            mpSummary.MembershipCardBarCodeString = barCodeData.ToUpper();
                            mpSummary.MembershipCardBarCode = GetBarCode(barCodeData);
                        }
                    }
                    else
                    {
                        mpSummary.MembershipCardBarCodeString = null;
                        mpSummary.MembershipCardBarCode = null;
                    }
                }

                mpSummary.NoMileageExpiration = noMileageExpiration.ToString();
                if (_configuration.GetValue<bool>("HideMileageBalanceExpireDate"))
                {
                    mpSummary.NoMileageExpirationMessage = _configuration.GetValue<string>("BalanceExpireDisclaimerNeverExpire");
                }
                else if (noMileageExpiration)
                {
                    mpSummary.NoMileageExpirationMessage = _configuration.GetValue<string>("ChaseNoMileageExpirationMessage") + " " + balanceExpireDisclaimer;
                    if (!fourSegmentMinimunWaivedMember)
                    {
                        mpSummary.NoMileageExpirationMessage = mpSummary.NoMileageExpirationMessage + " " + _configuration.GetValue<string>("FouSegmentMessage");
                    }
                }
                //Client is no longer using travelcreditinfo after RAMP 
                //  mpSummary.TravelCreditInfo = GetTravelCreditDetail(req.MileagePlusNumber, req.Application, req.SessionId, req.TransactionId, req.DeviceId);
            }
            catch (MOBUnitedException ex)
            {
                throw new MOBUnitedException(ex.Message);
            }
            catch (System.Exception ex)
            {
                throw new System.Exception(ex.Message);
            }


            if (_configuration.GetValue<string>("StartDateTimeToReturnEmptyMPExpirationDate") != null && _configuration.GetValue<string>("EndtDateTimeToReturnEmptyMPExpirationDate") != null && DateTime.ParseExact(_configuration.GetValue<string>("StartDateTimeToReturnEmptyMPExpirationDate").ToString().ToUpper().Trim(), "MM/dd/yyyy hh:mm tt", CultureInfo.InvariantCulture) <= DateTime.Now && DateTime.ParseExact(_configuration.GetValue<string>("EndtDateTimeToReturnEmptyMPExpirationDate").ToString().ToUpper().Trim(), "MM/dd/yyyy hh:mm tt", CultureInfo.InvariantCulture) >= DateTime.Now)
            {
                mpSummary.MembershipCardExpirationDate = string.Empty;
            }
            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("ShowWelcomeModel")) && _configuration.GetValue<bool>("ShowWelcomeModel") && await IsVBQWelcomeModelDisplayed(req.MileagePlusNumber, req.Application.Id, req.DeviceId))
            {
                mpSummary.vBQWelcomeModel = new VBQWelcomeModel();
            }
            return mpSummary;
        }
    }
}
