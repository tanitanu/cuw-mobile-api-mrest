using EmployeeRes.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml.Linq;
using United.Common.Helper.Profile;
using United.Common.Helper.Shopping;
using United.Common.HelperSeatEngine;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.Fitbit;
using United.Mobile.DataAccess.FlightReservation;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MPAuthentication;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Fitbit;
using United.Mobile.Model.FlightReservation;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.Shopping;
using United.Service.Presentation.CommonModel;
using United.Service.Presentation.ReservationModel;
using United.Service.Presentation.ReservationRequestModel;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common.LMX;
using United.Utility.Helper;
using Characteristic = United.Service.Presentation.CommonModel.Characteristic;
using FareType = United.Service.Presentation.CommonEnumModel.FareType;
using FlowType = United.Utility.Enum.FlowType;
using Genre = United.Service.Presentation.CommonModel.Genre;
using LoyaltyAccountBalance = United.Service.Presentation.CommonModel.LoyaltyAccountBalance;
using MOBFutureFlightCredit = United.Mobile.Model.Common.MOBFutureFlightCredit;
using MOBLMXRow = United.Mobile.Model.Common.MOBLMXRow;
using Price = United.Service.Presentation.PriceModel.Price;
using Reservation = United.Service.Presentation.ReservationModel.Reservation;
using RewardType = United.Mobile.Model.ManageRes.RewardType;
using Traveler = United.Service.Presentation.ReservationModel.Traveler;
using UpgradeVisibilityType = United.Service.Presentation.CommonEnumModel.UpgradeVisibilityType;

namespace United.Common.Helper.ManageRes
{
    public class FlightReservation : IFlightReservation
    {
        private readonly ICacheLog<FlightReservation> _logger;
        private readonly IConfiguration _configuration;
        private readonly IShoppingUtility _shoppingUtility;
        private readonly ISeatMapCSL30 _seatMapCSL30;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IEmpProfile _empprofile;
        private readonly IDPService _dPService;
        private readonly IPNRRetrievalService _pNRRetrievalService;
        private readonly IEmployeeIdByMileageplusNumber _employeeIdByMileageplusNumber;
        private readonly IDynamoDBService _dynamoDBService;
        private readonly IFlightSeapMapService _changeSeatService;
        private readonly IFlightReservationService _flightReservationService;
        private readonly ILMXInfo _lmxInfo;
        private readonly IReservationService _reservationService;
        private readonly IFareLockService _fareLockService;
        private readonly IMileagePlusReservationService _mileagePlusReservationService;
        private readonly IPNRServiceEResService _pNRServiceEResService;
        private readonly ManageResUtility _manageResUtility;
        private readonly DocumentLibraryDynamoDB _documentLibraryDynamoDB;
        private readonly AirportDynamoDB _airportDynamoDB;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private readonly IIRROPValidateService _iRROPValidateService;
        private string PATH_COUNTRIES_XML;
        private List<string[]> Countries = new List<string[]>();
        private static readonly string _MSG1 = "MSG1";
        private static readonly string _ERR1 = "ERR1";
        private static readonly string _ERR2 = "ERR2";
        private static readonly string _ERR3 = "ERR3";
        private static readonly string _ERR4 = "Err4";
        private static readonly string _ERR5 = "ERR5";
        private readonly IHeaders _headers;
        private readonly IFFCShoppingcs _fFCShoppingcs; 
            
       public FlightReservation(ICacheLog<FlightReservation> logger
            , IConfiguration configuration
            , IShoppingUtility shoppingUtility
            , ISeatMapCSL30 seatMapCSL30
            , ISessionHelperService sessionHelperService
            , IFlightSeapMapService changeSeatService
            , IDynamoDBService dynamoDBService
            , IDPService dPService
            , IPNRRetrievalService pNRRetrievalService
            , IFlightReservationService flightReservationService
            , IEmpProfile empprofile
            , ILMXInfo lmxInfo
            , IReservationService reservationService
            , IFareLockService fareLockService
            , IMileagePlusReservationService mileagePlusReservationService
            , IPNRServiceEResService pNRServiceEResService
            , IEmployeeIdByMileageplusNumber employeeIdByMileageplusNumber
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , IHeaders headers
            , IIRROPValidateService iRROPValidateService
            , IFFCShoppingcs fFCShoppingcs)
        {
            _logger = logger;
            _configuration = configuration;
            _shoppingUtility = shoppingUtility;
            _seatMapCSL30 = seatMapCSL30;
            _sessionHelperService = sessionHelperService;
            _changeSeatService = changeSeatService;
            _dynamoDBService = dynamoDBService;
            _dPService = dPService;
            _pNRRetrievalService = pNRRetrievalService;
            _flightReservationService = flightReservationService;
            _empprofile = empprofile;
            _lmxInfo = lmxInfo;
            _reservationService = reservationService;
            _fareLockService = fareLockService;
            _mileagePlusReservationService = mileagePlusReservationService;
            _pNRServiceEResService = pNRServiceEResService;
            _documentLibraryDynamoDB = new DocumentLibraryDynamoDB(_configuration, _dynamoDBService);
            _airportDynamoDB = new AirportDynamoDB(_configuration, _dynamoDBService);
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _manageResUtility = new ManageResUtility(_configuration, _legalDocumentsForTitlesService, _dynamoDBService, headers, _logger);
            _employeeIdByMileageplusNumber = employeeIdByMileageplusNumber;
            _headers = headers;
            _iRROPValidateService = iRROPValidateService;
            _fFCShoppingcs = fFCShoppingcs;
        }

        public async Task<(MOBPNR pnr, ReservationDetail response)> GetPNRByRecordLocatorFromCSL(string transactionId, string deviceId, string recordLocator, string lastName, string languageCode, int applicationId, string appVersion, bool forWallet, Session session, ReservationDetail response, bool isOTFConversion = false)
        {
            response = null;
            MOBPNR pnr = null;
            string jsonResponse = string.Empty;

            string token = session != null ? session.Token : string.Empty;

            if (_configuration.GetValue<bool>("EnableSpecialcharacterFilterInPNRLastname"))
                lastName = _manageResUtility.SpecialcharacterFilterInPNRLastname(lastName);

            var tupleResponse = await GetPnrDetailsFromCSL(transactionId, recordLocator, lastName, applicationId, appVersion, "GetPNRByRecordLocatorFromCSL", token);
            jsonResponse = tupleResponse.Item1;
            token = tupleResponse.token;

            if (!string.IsNullOrEmpty(jsonResponse))
            {
                DataContractJsonSerializer dataContractJsonSerializer = new DataContractJsonSerializer(typeof(ReservationDetail));
                MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonResponse));
                response = (ReservationDetail)dataContractJsonSerializer.ReadObject(memoryStream);

                if (session != null)
                {
                    session.Token = token;
                    session.EmployeeId = GetEmployeeId(response);

                    await _sessionHelperService.SaveSession(session, session.SessionId, new List<string> { session.SessionId, session.ObjectName }, session.ObjectName).ConfigureAwait(false);
                    await _sessionHelperService.SaveSession<ReservationDetail>(response, session.SessionId, new List<string> { session.SessionId, response.GetType().FullName }, response.GetType().FullName).ConfigureAwait(false);
                }

                bool isAllFirstCabin = true;
                bool isSpaceAvailblePassRider = false;
                bool isPositiveSpace = false;
                int lowestEliteLevel = 0;
                bool hasGSPax = false;
                bool has1KPax = false;
                bool hasUpgradeVisibility = false;
                bool firstSegmentLifted = false;
                bool isPsSaTravel = false;

                if (response != null && (response.Error == null || response.Error.Count == 0))
                {
                    if (response.Detail != null && response.Detail.FlightSegments != null)
                    {
                        pnr = new MOBPNR();

                        string isActive = GetCharactersticValue(response.Detail.Characteristic, "ActiveTicketsExist");
                        pnr.IsActive = Convert.ToBoolean(string.IsNullOrEmpty(isActive) ? "false" : isActive);
                        pnr.IsATREEligible = ShopStaticUtility.IsATREEligible(response);

                        if (_manageResUtility.EnableActiveFutureFlightCreditPNR(applicationId, appVersion))
                        {
                            if (response.Detail.Characteristic != null && response.Detail.Characteristic.Any())
                            {
                                if (_configuration.GetValue<bool>("DisableFFCVisibilityChanges"))
                                {
                                    if (pnr.IsActive)
                                    {
                                        if (!string.IsNullOrEmpty(ShopStaticUtility.GetCharactersticDescription_New(response.Detail.Characteristic, "FFC")))
                                        {
                                            pnr.IsCanceledWithFutureFlightCredit = true;
                                            pnr.Futureflightcredit = await GetFutureFlightCreditContent(applicationId, appVersion);
                                        }
                                    }
                                    else if (_configuration.GetValue<bool>("AllowFFCWhenTicketInactive"))
                                    {
                                        if (!string.IsNullOrEmpty(ShopStaticUtility.GetCharactersticDescription_New(response.Detail.Characteristic, "FFC")))
                                        {
                                            pnr.IsCanceledWithFutureFlightCredit = true;
                                            pnr.Futureflightcredit = await GetFutureFlightCreditContent(applicationId, appVersion);
                                        }
                                    }
                                }
                                else
                                {
                                    if (!string.IsNullOrEmpty(ShopStaticUtility.GetCharactersticDescription_New(response.Detail.Characteristic, "FFC")))
                                    {
                                        pnr.IsCanceledWithFutureFlightCredit = true;
                                        pnr.Futureflightcredit = await GetFutureFlightCreditContent(applicationId, appVersion);

                                        string formatedAmount = string.Empty;
                                        bool inlcudeOrWaivedInChangeFee = false;
                                        MOBItem originalTrip = null;
                                        MOBItem changeFee = null;
                                        string ticketValidityDate = string.Empty;
                                        bool partiallyFlown = false;

                                        if (response.Detail.Travelers != null &&
                                            response.Detail.Travelers.Count() > 0 &&
                                            response.Detail.Travelers[0].Tickets != null &&
                                            response.Detail.Travelers[0].Tickets.Count > 0 &&
                                            response.Detail.Travelers[0].Tickets[0].Status?.Code == "UA USED")
                                        {
                                            partiallyFlown = true;
                                        }
                                        // Set originalTrip for only revenue PNRs
                                        United.Service.Presentation.CommonModel.Genre iType = response.Detail.Type.FirstOrDefault(x => x != null && x.Description != null && x.Description.Equals("ITIN_TYPE") && !string.IsNullOrEmpty(x.Key));
                                        if (iType != null &&
                                            response.Detail.Type.Count > 0 &&
                                            iType.Key.Equals("AWARD", StringComparison.InvariantCultureIgnoreCase) == false)
                                        {
                                            originalTrip = GetCharactersticValueAndCodeByDescription(response.Detail.Characteristic, "ITINTotalForCurrency");
                                            changeFee = GetCharactersticValueAndCodeByDescription(response.Detail.Characteristic, "CHANGE FEE");

                                            if (response.Detail.Travelers != null &&
                                                  response.Detail.Travelers.Count > 0 &&
                                                  response.Detail.Travelers[0].Tickets != null &&
                                                  response.Detail.Travelers[0].Tickets.Count > 0 &&
                                                  response.Detail.Travelers[0].Tickets[0].TicketValidityDate != null)
                                            {
                                                ticketValidityDate = response.Detail.Travelers[0].Tickets[0].TicketValidityDate;
                                            }

                                            if (_configuration.GetValue<bool>("EnableShowChangeFeeDisclaimerTextForFFCPnr") && changeFee != null)
                                            {
                                                pnr.Futureflightcredit.Messages.Add(
                                                   new MOBItem
                                                   {
                                                       Id = "FFC_Policy_Info",
                                                       CurrentValue = _configuration.GetValue<string>("ChangeFeeDisclaimerText"),
                                                       SaveToPersist = false
                                                   });
                                            }


                                            #region Check change fee waiver policy
                                            if (response.PNRChangeEligibility != null &&
                                            response.PNRChangeEligibility.Policies != null &&
                                            response.PNRChangeEligibility.Policies.Count > 0
                                           )
                                            {
                                                if (_configuration.GetValue<bool>("EnableShowChangeFeeDisclaimerTextForFFCPnr") && changeFee != null)
                                                {
                                                    var ffcPolicyInfoItem = pnr.Futureflightcredit.Messages.Where(i => i.Id == "FFC_Policy_Info").FirstOrDefault();
                                                    if (ffcPolicyInfoItem != null)
                                                        ffcPolicyInfoItem.CurrentValue = string.Format("{0}<BR><BR>{1}", ffcPolicyInfoItem.CurrentValue, _configuration.GetValue<string>("FFC_Policy_Info"));
                                                }
                                                else
                                                {
                                                    pnr.Futureflightcredit.Messages.Add(
                                                    new MOBItem
                                                    {
                                                        Id = "FFC_Policy_Info",
                                                        CurrentValue = _configuration.GetValue<string>("FFC_Policy_Info"),
                                                        SaveToPersist = false
                                                    });
                                                }


                                                if (changeFee != null)
                                                {
                                                    decimal.TryParse(changeFee.CurrentValue, out decimal changeFeeAmount);
                                                    if (changeFeeAmount > 0)
                                                        inlcudeOrWaivedInChangeFee = true;
                                                }
                                                else if (_configuration.GetValue<bool>("EnableShowDefaultTextIfChangeFeeMissing"))
                                                {
                                                    inlcudeOrWaivedInChangeFee = true;
                                                }
                                            }
                                            #endregion

                                        }
                                        pnr.Futureflightcredit = UpdateFFCTable(pnr.Futureflightcredit, applicationId, appVersion, originalTrip, changeFee, inlcudeOrWaivedInChangeFee, ticketValidityDate);
                                        pnr.Prices = new List<MOBReservationPrice>();
                                        if (changeFee != null)
                                        {
                                            pnr.Prices.Add(AddUnformatedPriceInfo("Change fee", changeFee.Id, _manageResUtility.GetCurrencyCode(changeFee.Id), changeFee.CurrentValue));
                                        }
                                        if (originalTrip != null)
                                        {
                                            pnr.Prices.Add(AddUnformatedPriceInfo("Original trip", originalTrip.Id, _manageResUtility.GetCurrencyCode(originalTrip.Id), originalTrip.CurrentValue));
                                        }

                                        if (_configuration.GetValue<bool>("Include_ATRE_Check_For_FFC_Visibility"))
                                        {
                                            if (pnr.IsATREEligible == false ||
                                                iType.Key.Equals("AWARD", StringComparison.InvariantCultureIgnoreCase) == true ||
                                                partiallyFlown)
                                            {
                                                pnr.Futureflightcredit = RemoveFFCVisibilityCaptions(pnr.Futureflightcredit, applicationId, appVersion);
                                            }
                                        }
                                    }
                                }

                            }
                        }

                        if (!pnr.IsCanceledWithFutureFlightCredit)
                        {
                            if (response.Detail.FlightSegments.Count == 0)
                            {
                                throw new MOBUnitedException(_configuration.GetValue<string>("NoSegmentsFoundErrorMessage"));
                            }
                        }

                        if (response.Detail != null && response.Detail.Type.Count() > 0)
                        {
                            if (response.Detail.Type.ToList().Exists(p => p.Description == "GROUP"))
                            {
                                pnr.isgroup = true;
                            }
                            if (response.Detail.Type.ToList().Exists(p => p.Key == "BT"))
                            {
                                pnr.IsBulk = true;
                            }
                        }

                        if (Convert.ToBoolean(_configuration.GetValue<string>("ReshopPricingInformationToggledOn")))
                        {
                            pnr.TripType = new TripTypeMapper(response.Detail.FlightSegments).Map();
                        }

                        if (session != null)
                        {
                            pnr.SessionId = session.SessionId;
                        }

                        pnr.DateCreated = GeneralHelper.FormatDatetime(Convert.ToDateTime(response.Detail.CreateDate).ToString("yyyyMMdd hh:mm tt"), languageCode);

                        pnr.JourneyType = GetTravelType(response.Detail.FlightSegments);

                        //Kirti migrated Concur from XML to CSL 
                        CheckForConcur(response, pnr);

                        pnr.FarelockPurchaseMessage = _configuration.GetValue<string>("FarelockPurchaseMessage");
                        pnr.EarnedMilesHeader = HttpUtility.HtmlDecode(_configuration.GetValue<string>("EarnedMilesHeader"));
                        pnr.EarnedMilesText = _configuration.GetValue<string>("EarnedMilesText");
                        pnr.IneligibleToEarnCreditMessage = _configuration.GetValue<string>("IneligibleToEarnCreditMessage");
                        pnr.OaIneligibleToEarnCreditMessage = _configuration.GetValue<string>("OaIneligibleToEarnCreditMessage");


                        //kirti - per CSL Award is in Type[0].Description = ITIN_TYPE
                        United.Service.Presentation.CommonModel.Genre iTinType = response.Detail.Type.FirstOrDefault(x => x != null && x.Description != null && x.Description.Equals("ITIN_TYPE") && !string.IsNullOrEmpty(x.Key));
                        //Anku - added condition to check if session is not equal to null - Bug:229129
                        if (iTinType != null && (response.Detail.Type.Count > 0 &&
                            iTinType.Key.Equals("AWARD", StringComparison.InvariantCultureIgnoreCase)) && session != null)
                        {
                            pnr.AwardTravel = true;
                            session.IsAward = true;
                        }

                        appVersion = string.IsNullOrEmpty(appVersion) ? string.Empty : appVersion.ToUpper();


                        GetTheFareLock(appVersion, response, pnr);

                        //Farelock enhancements to send message and price for purchasing in manageres
                        if (_manageResUtility.EnableFareLockPurchaseViewRes(applicationId, appVersion) && !string.IsNullOrEmpty(pnr.FarelockExpirationDate) && IsEligibleForCompleteFareLockPurchase(response.Detail))
                        {
                            pnr.FareLockMessage = GetFareLockPurchase(response.Detail);
                            string FareLockPurchaseButton = GetFareLockButton(response.Detail.Characteristic);
                            pnr.FareLockPurchaseButton = !string.IsNullOrEmpty(FareLockPurchaseButton) ? "Purchase now for " + FareLockPurchaseButton : string.Empty;
                            pnr.FareLockPriceButton = !string.IsNullOrEmpty(FareLockPurchaseButton) ? FareLockPurchaseButton : string.Empty;
                        }

                        //There was only one email address before
                        if (response.Detail.EmailAddress != null && response.Detail.EmailAddress.Count != 0)
                            pnr.EmailAddress = response.Detail.EmailAddress[0].Address;

                        pnr.RecordLocator = response.Detail.ConfirmationID;

                        //Other Airline PNR
                        await GetOARecordLocator(pnr, response);

                        lowestEliteLevel = GetLowestEliteLevel(applicationId, appVersion, response, pnr, lowestEliteLevel, ref isPsSaTravel, ref hasGSPax, ref has1KPax);

                        if (!_configuration.GetValue<bool>("TurnOffValidateTripsandSegmentsLogging"))
                        {
                            await _manageResUtility.ValidateTripsandSegments(response.Detail.FlightSegments, recordLocator, lastName, jsonResponse, applicationId, deviceId, appVersion, session.SessionId);
                        }

                        //Unit test
                        await GetTrips(pnr, response);

                        GetPassengerDetails(pnr, response, ref isSpaceAvailblePassRider, ref isPositiveSpace, applicationId, appVersion);

                        // get TPI info in charges
                        pnr.IsTPIIncluded = GetTPIBoughtInfo(response.Detail.Travelers);

                        GetPetsDetails(pnr, response);

                        int pastSegmentsCount = 0;
                        pnr.Segments = new List<MOBPNRSegment>();
                        bool firstsegment = true;

                        foreach (United.Service.Presentation.SegmentModel.ReservationFlightSegment segment in response.Detail.FlightSegments)
                        {
                            bool segmentPast = false;

                            if (segment.FlightSegment != null)
                            {
                                segmentPast = SegmentPast(response, segment);

                                //Madhavi As per CSL ShareIndex is SegmentNumber.
                                if ((firstsegment && segment.SegmentNumber != 1) || (firstsegment && segment.SegmentNumber == 1 && segmentPast))
                                {
                                    firstSegmentLifted = true;
                                }

                                firstsegment = false;

                                if (segmentPast)
                                {
                                    pastSegmentsCount = pastSegmentsCount + 1;
                                }

                                else
                                {
                                    if (segment.FlightSegment.MarketedFlightSegment != null && segment.FlightSegment.MarketedFlightSegment.Count > 0)
                                    {
                                        var carrierCode = segment.FlightSegment.MarketedFlightSegment[0].MarketingAirlineCode;

                                        //per csl Read from MarketingAirline code 
                                        if (!pnr.IsCanceledWithFutureFlightCredit
                                            && !string.IsNullOrEmpty(carrierCode)
                                            && !string.IsNullOrEmpty(GetCharactersticValue(segment.Characteristic, "CheckedIn"))
                                            && !string.IsNullOrEmpty(GetCharactersticValue(segment.Characteristic, "CheckinTriggered")))
                                        {
                                            //Fix as per the recommendation by Praveen for the exception 330517_GetPNRByRecordLocator_NullReference_Exception.Changes by Niveditha.

                                            bool PNRCancelCheck = Convert.ToBoolean(_configuration.GetValue<string>("ViewReservationPNRCancelledCheck"));
                                            if (PNRCancelCheck && segment.FlightSegment.FlightSegmentType == "HX")
                                            {
                                                throw new MOBUnitedException("We are unable to retrieve the latest information for this itinerary.");
                                            }
                                            if (isAllFirstCabin
                                                && carrierCode.ToUpper().Trim() == "UA"
                                                && !Convert.ToBoolean(GetCharactersticValue(segment.Characteristic, "CheckedIn"))
                                                && !Convert.ToBoolean(GetCharactersticValue(segment.Characteristic, "CheckinTriggered"))
                                                && segment.BookingClass.Cabin.Name != null && (segment.BookingClass.Cabin.Description == UAWSFlightReservation.CabinTypes.Coach.ToString()))
                                            {
                                                isAllFirstCabin = false;
                                            }
                                        }
                                    }
                                }

                                var pnrSeatList = GetPnrSeatList(response);

                                var pnrSegment = await GetPnrSegment(languageCode, appVersion, applicationId, segment, lowestEliteLevel);


                                pnrSegment.FareBasisCode = ShopStaticUtility.GetFareBasisCode(response.Detail.Prices, pnrSegment.SegmentNumber);

                                //_configuration.GetValue<bool>("IsByPassVIEWRES24HrsCheckinWindow");                
                                pnrSegment.IsCheckedIn = _shoppingUtility.IsCheckedIn(segment);
                                pnrSegment.IsCheckInEligible = _shoppingUtility.IsCheckInEligible(segment);
                                pnrSegment.IsAllPaxCheckedIn = true;

                                if (segment.FlightSegment.UpgradeVisibilityType != UpgradeVisibilityType.None && !string.IsNullOrWhiteSpace(segment.FlightSegment.UpgradeMessage) && !string.IsNullOrWhiteSpace(segment.FlightSegment.PreviousSegmentActionCode) && !string.IsNullOrWhiteSpace(segment.FlightSegment.UpgradeMessageCode))
                                {
                                    pnrSegment.UpgradeVisibility = GetUpgradeVisibilityCSLWithWaitlist(segment.FlightSegment, ref pnrSegment, response.Detail.Prices);

                                    var waitlist = GetCharactersticValue(segment.Characteristic, "Waitlisted");

                                    if (Convert.ToBoolean(string.IsNullOrWhiteSpace(waitlist) ? "false" : waitlist))
                                    {
                                        pnrSegment.UpgradeVisibility.WaitlistSegments = new List<MOBSegmentResponse>();

                                        var waitListSegment = GetUpgradeVisibilityCSLWithWaitlist(segment.FlightSegment, ref pnrSegment);
                                        pnrSegment.UpgradeVisibility.WaitlistSegments.Add(waitListSegment);
                                    }

                                    if (pnrSegment.UpgradeVisibility != null && (pnrSegment.UpgradeVisibility.UpgradeStatus == MOBUpgradeEligibilityStatus.Requested || pnrSegment.UpgradeVisibility.UpgradeStatus == MOBUpgradeEligibilityStatus.Qualified) && (pnrSegment.UpgradeVisibility.UpgradeType == MOBUpgradeType.ComplimentaryPremierUpgrade || pnrSegment.UpgradeVisibility.UpgradeType == MOBUpgradeType.GlobalPremierUpgrade || pnrSegment.UpgradeVisibility.UpgradeType == MOBUpgradeType.MileagePlusUpgradeAwards || pnrSegment.UpgradeVisibility.UpgradeType == MOBUpgradeType.RegionalPremierUpgrade))
                                    {
                                        pnrSegment.UpgradeEligible = true;
                                    }
                                    if (pnrSegment.UpgradeVisibility != null && pnrSegment.UpgradeVisibility.UpgradeType != MOBUpgradeType.PremierInstantUpgrade)
                                    {
                                        hasUpgradeVisibility = true;
                                    }
                                }
                                //per CSL seatnumber should be mapped from current seats
                                GetSeatDetails(segment, pnrSegment, pnrSeatList);

                                #region StopInfoRegion

                                var tupleRes = await GetStopSegmentDetails(languageCode, segment, pnrSegment, hasUpgradeVisibility);
                                pnrSegment = tupleRes.Item1;
                                hasUpgradeVisibility = tupleRes.hasUpgradeVisibility;
                                #endregion StopInfoRegion

                                //per csl do not add segments which has status Key=ARNK and status == null and is waitlist true
                                if (segment.FlightSegment != null && segment.Status != null)
                                {
                                    if (string.IsNullOrWhiteSpace(segment.Status.Key) || !segment.Status.Key.Equals("ARNK", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        bool segmentFlownCheckToggle = Convert.ToBoolean(_configuration.GetValue<string>("SegmentFlownCheckToggle") ?? "false");
                                        if (segmentFlownCheckToggle)
                                        {
                                            pnrSegment.TicketCouponStatus = GetCharactersticValueByDescription(segment.FlightSegment.Characteristic, "Ticket Coupon Status");
                                        }
                                        pnr.Segments.Add(pnrSegment);
                                    }
                                }
                                //#endregion
                            }
                        }
                        // CTN Birkan
                        if (pnr.Segments != null && pnr.Segments.Any(x => x.IsCanadaSegment))
                        {
                            pnr.TravelerInfo = (pnr.TravelerInfo == null) ? new MOBTravelerInfo() : pnr.TravelerInfo;
                            pnr.TravelerInfo.contentScreens = (pnr.TravelerInfo.contentScreens == null) ? new List<MOBPageContent>() : pnr.TravelerInfo.contentScreens;
                            pnr.TravelerInfo.contentScreens.Add(_manageResUtility.PopulateCtnInfo(pnr));
                        }

                        if (_configuration.GetValue<bool>("GetCheckInStatusFromCSLPNRRetrivalService_Toggle"))
                        {
                            _manageResUtility.GetCheckInEligibilityStatusFromCSLPnrReservation(response.Detail.CheckinEligibility, ref pnr);
                            pnr.GetCheckInStatusFromCSLPNRRetrivalService = true;
                        }

                        GetSpecialNeedsAdvisoryMessage(applicationId, appVersion, ref pnr);

                        //BE changes
                        pnr.ProductCode = GetProductCode(response.Detail.Type);
                        pnr.IsIBELite = HasIBeLiteSegments(response.Detail.Type);
                        pnr.IsIBE = HasIBeSegments(response.Detail.Type);
                        await SetElfContentOnPnr(pnr, applicationId, appVersion);

                        //For Analytics 
                        pnr.MarketType = GetMarketType(response.Detail.FlightSegments);
                        pnr.ProductCategory = GetProductCategory(response.Detail);

                        // FIX on Feb 22,2013 
                        if (_configuration.GetValue<string>("NoSegmentsFoundInPNRErrorMessage") != null && (pnr.Segments == null || pnr.Segments.Count == 0))
                        {
                            throw new MOBUnitedException(_configuration.GetValue<string>("NoSegmentsFoundInPNRErrorMessage"));
                        }

                        GetIsEligibleToSeatChangeCSL(response, pnr, pastSegmentsCount);

                        pnr.IsETicketed = ShopStaticUtility.GetETicketStatus(response.Detail);
                        pnr.ShouldDisplayEmailReceipt = !pnr.isgroup && pnr.IsETicketed;

                        if (string.Equals(ShopStaticUtility.GetCharactersticDescription_New
                        (response.Detail.Characteristic, "Booking Source"), "AGENCY", StringComparison.OrdinalIgnoreCase))
                        {
                            pnr.IsAgencyBooking = true;
                            pnr.AgencyName = response.Detail.Channel;
                        }
                        pnr.IsCheckinEligible = ShopStaticUtility.CheckForCheckinEligible(response.Detail.FlightSegments);

                        #region "ADVISORY MESSAGE"

                        //Set Corporate Booking
                        string corporateTravelVendorName
                            = ShopStaticUtility.GetRemarksDescriptionValue(response.Detail.Remarks, "PWC TRAVELER");

                        pnr.IsCorporateBooking
                            = (string.IsNullOrEmpty(pnr.SyncedWithConcur)) ? false : true;

                        pnr.CorporateVendorName
                            = (string.IsNullOrEmpty(corporateTravelVendorName)) ? string.Empty : corporateTravelVendorName;

                        #region "FFC Residual"
                        if (IsPNRHasFFCRIssued(pnr, response, applicationId, appVersion))
                        {
                            string[] stringarray = _manageResUtility.SplitConcatenatedConfigValue("ManageResFFCResidualAlert", "||");

                            if (stringarray != null && stringarray.Length >= 2)
                            {
                                MOBPNRAdvisory ffcrResidualadvisory = new MOBPNRAdvisory
                                {
                                    Header = stringarray[0],
                                    Body = stringarray[1],
                                    ContentType = ContentType.FFCRRESIDUAL,
                                    AdvisoryType = AdvisoryType.INFORMATION,
                                    IsBodyAsHtml = true,
                                };
                                pnr.AdvisoryInfo = (pnr.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : pnr.AdvisoryInfo;
                                pnr.AdvisoryInfo.Add(ffcrResidualadvisory);
                            }
                        }
                        #endregion //"FFC Residual"                        

                        #region "UMNR"
                        if (_manageResUtility.EnableUMNRInformation(applicationId, appVersion) && !isOTFConversion)
                        {
                            pnr.IsUnaccompaniedMinor = ShopStaticUtility.CheckUnaccompaniedMinorAvailable(response.Detail.Travelers);

                            if (pnr.IsUnaccompaniedMinor)
                                pnr.Umnr = await GetUMNRMsgInformation(response.Detail);
                        }
                        #endregion //"UMNR"

                        #region "TRC"

                        if (_shoppingUtility.IncludeTRCAdvisory(pnr, applicationId, appVersion) && !isOTFConversion)
                        {
                            MOBPNRAdvisory tcrAdvisorymsg = _manageResUtility.PopulateTRCAdvisoryContent(displaycontent: "");
                            if (tcrAdvisorymsg != null)
                            {
                                tcrAdvisorymsg.ContentType = ContentType.TRAVELREADY;
                                tcrAdvisorymsg.AdvisoryType = AdvisoryType.CAUTION;
                                pnr.AdvisoryInfo = (pnr.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : pnr.AdvisoryInfo;
                                pnr.AdvisoryInfo.Add(tcrAdvisorymsg);
                            }
                        }

                        #endregion //"TRC"

                        #region "OTF Conversion"
                        if (!pnr.IsCanceledWithFutureFlightCredit && !pnr.HasScheduleChanged && isOTFConversion)
                        {
                            MOBPNRAdvisory otfConversionadvisory = new MOBPNRAdvisory
                            {
                                Header = _configuration.GetValue<string>("otfConversionAlert"),
                                ContentType = ContentType.OTFCONVERSION,
                                AdvisoryType = AdvisoryType.INFORMATION,
                                IsBodyAsHtml = true,
                                ShouldExpand = false,
                            };
                            pnr.AdvisoryInfo = (pnr.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : pnr.AdvisoryInfo;
                            pnr.AdvisoryInfo.Add(otfConversionadvisory);
                        }
                        #endregion

                        #region "JSX"                        
                        if (_manageResUtility.IsEnableJSXManageRes(applicationId, appVersion))
                        {
                            pnr.HasJSXSegment = (string.IsNullOrEmpty(GetCharactersticValue(response.Detail.Characteristic, "XE"))) ? false : true;

                            if (pnr.HasJSXSegment)
                            {
                                List<CMSContentMessage> lstMessages
                                    = await _fFCShoppingcs.GetSDLContentByGroupName(null, session.SessionId, session.Token,
                                    _configuration.GetValue<string>("CMSContentMessages_GroupName_MANAGERES_Messages"),
                                    "MANAGERES_CMSContentMessagesCached_StaticGUID");

                                if (lstMessages != null)
                                {
                                    MOBPNRAdvisory jxcAdvisorymsg = _manageResUtility.PopulateJSXAdvisoryContent(lstMessages);

                                    if (jxcAdvisorymsg != null)
                                    {
                                        pnr.AdvisoryInfo = (pnr.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : pnr.AdvisoryInfo;
                                        pnr.AdvisoryInfo.Add(jxcAdvisorymsg);
                                    }
                                }
                                pnr.JsxAlertMessageForChangeSeat = _configuration.GetValue<string>("JSXSeatChangeMessage");
                            }
                        }
                        #endregion

                        #region "PET in CABIN"
                        if (_manageResUtility.EnablePetInformation(applicationId, appVersion) && !isOTFConversion)
                        {
                            if (ShopStaticUtility.CheckInCabinPetAvailable(response.Detail.Services))
                            {
                                pnr.IsPetAvailable = true;
                                pnr.InCabinPetInfo = await GetPetMsgInformation(petcontentdbname: "PET_Alert_Content");
                                SetInCabinPetToSegment(response.Detail, ref pnr);
                            }
                        }
                        #endregion //"PET in CABIN"                        

                        #region "TRAVEL WAIVER"

                        var policyexceptionalert
                            = ShopStaticUtility.GetBooleanFromCharacteristics(response.Detail.Characteristic, "24HrFlexibleBookingPolicy");


                        pnr.ShowOverride24HrFlex =
                            (_configuration.GetValue<bool>("EnableReshopOverride24HrFlex")
                            && policyexceptionalert != null && policyexceptionalert.HasValue)
                            ? policyexceptionalert.Value : false;

                        bool ishidepolicyexceptionalert
                            = (policyexceptionalert != null && policyexceptionalert.HasValue) ? policyexceptionalert.Value : true;

                        if (response.PNRChangeEligibility != null)
                        {
                            bool.TryParse(response.PNRChangeEligibility.IsPolicyEligible, out bool ispolicychangeeligible);
                            //Override24HrFlex                          

                            if (!_configuration.GetValue<bool>("Disable_RESHOP_JSENONCHANGEABLEFARE"))
                            {
                                if (response.PNRChangeEligibility.InEligibilityReasons != null)
                                {
                                    if (!string.IsNullOrEmpty(ShopStaticUtility.GetCharactersticDescription_New
                                        (response.PNRChangeEligibility.InEligibilityReasons, "JSE_NONCHANGE_PNR")))
                                    {
                                        pnr.IsJSENonChangeableFare = true;
                                    }
                                }

                                pnr.Is24HrFlexibleBookingPolicy
                                    = (policyexceptionalert != null && policyexceptionalert.HasValue) ? policyexceptionalert.Value : false;

                                if (pnr.IsJSENonChangeableFare)
                                {
                                    MOBPNRAdvisory jseNonChangeableContent;

                                    if (pnr.Is24HrFlexibleBookingPolicy)
                                    {
                                        jseNonChangeableContent
                                            = PopulateConfigContent("RESHOP_JSE_NONCHANGEABLE_INSIDE24HRS", "||");
                                        jseNonChangeableContent.AdvisoryType = AdvisoryType.INFORMATION;
                                    }
                                    else
                                    {
                                        jseNonChangeableContent
                                            = PopulateConfigContent("RESHOP_JSE_NONCHANGEABLE_OUTSIDE24HRS", "||");
                                        jseNonChangeableContent.AdvisoryType = AdvisoryType.CAUTION;
                                    }

                                    if (jseNonChangeableContent != null)
                                    {
                                        jseNonChangeableContent.ContentType = ContentType.JSENONCONVERTEABLEPNR;
                                        pnr.AdvisoryInfo = (pnr.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : pnr.AdvisoryInfo;
                                        pnr.AdvisoryInfo.Add(jseNonChangeableContent);
                                    }
                                }
                            }


                            pnr.IsBEChangeEligible = (ispolicychangeeligible && !pnr.IsATREEligible && ishidepolicyexceptionalert);

                            if (ispolicychangeeligible && !ishidepolicyexceptionalert)
                            {
                                pnr.IsPolicyExceptionAlert = true;
                            }

                            if (_manageResUtility.CheckMax737WaiverFlight(response.PNRChangeEligibility))
                            {
                                MOBPNRAdvisory max737flightwaiver = await PopulatePolicyExceptionContent(displaycontent: "MAX737_WAIVER_MESSAGES");
                                if (max737flightwaiver != null)
                                {
                                    max737flightwaiver.ContentType = ContentType.MAX737WAIVER;
                                    max737flightwaiver.AdvisoryType = AdvisoryType.INFORMATION;
                                    pnr.AdvisoryInfo = (pnr.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : pnr.AdvisoryInfo;
                                    pnr.AdvisoryInfo.Add(max737flightwaiver);
                                }
                            }
                        }

                        #endregion

                        #region "SC ONE TIME CHANGE"

                        //Set - IsSCChangeEligible & IsSCRefundEligible
                        ShopStaticUtility.CheckSCChangeRefundEligible(response, pnr);

                        if (!pnr.PsSaTravel && !pnr.IsBulk && !pnr.isgroup && pnr.IsATREEligible
                            && !(pnr.IsCorporateBooking && pnr.CorporateVendorName.IndexOf("PWC", StringComparison.OrdinalIgnoreCase) > -1))
                        {
                            _manageResUtility.OneTimeSCChangeCancelAlert(pnr, applicationId, appVersion);
                        }

                        #endregion

                        //Setting FFC new ChangeFee Description
                        if (pnr.IsCanceledWithFutureFlightCredit
                            && _configuration.GetValue<bool>("EnableManageResFFCChangeFeeDescription"))
                        {
                            SetFFCChangeFeeDescription(response, pnr);
                        }

                        #region "MILES & MONEY"

                        if (response?.Detail?.BookingIndicators != null
                            && response.Detail.BookingIndicators.IsMilesAndMoney)
                        {
                            pnr.IsMilesAndMoney
                                = response.Detail.BookingIndicators.IsMilesAndMoney;
                        }

                        if (_configuration.GetValue<bool>("EnableReshopMilesMoney")
                            && pnr.IsMilesAndMoney && !pnr.IsATREEligible && !pnr.IsIBE && !pnr.IsIBELite && !pnr.isELF)
                        {
                            string[] stringarray = _manageResUtility.SplitConcatenatedConfigValue("MilesMoneyReshopEligibility", "||");

                            if (stringarray != null && stringarray.Length >= 2)
                            {
                                MOBPNRAdvisory milesMoneyAdvisory = new MOBPNRAdvisory
                                {
                                    Header = stringarray[0],
                                    Body = stringarray[1],
                                    ContentType = ContentType.MILESMONEY,
                                    AdvisoryType = AdvisoryType.INFORMATION,
                                    IsBodyAsHtml = true,
                                };
                                pnr.AdvisoryInfo = (pnr.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : pnr.AdvisoryInfo;
                                pnr.AdvisoryInfo.Add(milesMoneyAdvisory);
                            }
                        }

                        #endregion

                        #region "SCHEDULECHANGE"
                        bool isScheduleChangeSegmentWithNoProtection = false;

                        if (_manageResUtility.GetHasScheduledChangedV2(pnr.Segments, ref isScheduleChangeSegmentWithNoProtection))
                        {
                            pnr.HasScheduleChanged = true;

                            if (_configuration.GetValue<bool>("IncludePNRScheduleChangeInfo")
                                 && GeneralHelper.IsApplicationVersionGreaterorEqual
                                 (applicationId, appVersion, _configuration.GetValue<string>("AndroidPNRScheduleChangeInfoVersion"), _configuration.GetValue<string>("iPhonePNRScheduleChangeInfoVersion")))
                            {
                                pnr.IsTicketedByUA = _manageResUtility.CheckIfTicketedByUA(response);

                                pnr.IsSCBulkGroupPWC = pnr.IsBulk || pnr.isgroup
                                    || (pnr.IsCorporateBooking && pnr.CorporateVendorName.IndexOf("PWC", StringComparison.OrdinalIgnoreCase) > -1);

                                UpdateScheduleChangeSegments(pnr, response);
                                UpdateScheduleChangeInfo(pnr, response);
                            }
                            else
                            {
                                MOBPNRAdvisory schedulechangeadvisory = await PopulateScheduleChangeContent(displaycontent: "SCHEDULE_CHANGE_MESSAGES");
                                if (schedulechangeadvisory != null)
                                {
                                    pnr.ConsolidateScheduleChangeMessage = true;
                                    schedulechangeadvisory.ContentType = ContentType.SCHEDULECHANGE;
                                    schedulechangeadvisory.AdvisoryType = AdvisoryType.WARNING;
                                    pnr.AdvisoryInfo = (pnr.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : pnr.AdvisoryInfo;
                                    pnr.AdvisoryInfo.Add(schedulechangeadvisory);
                                }
                            }
                        }

                        else if (isScheduleChangeSegmentWithNoProtection)
                        {
                            MOBPNRAdvisory schedulechangeadvisory = PopulateScheduleChangeNoProtection();
                            if (schedulechangeadvisory != null)
                            {
                                pnr.ConsolidateScheduleChangeMessage = true;
                                schedulechangeadvisory.IsDefaultOpen = true;
                                schedulechangeadvisory.ShouldExpand = true;
                                schedulechangeadvisory.ContentType = ContentType.SCHEDULECHANGE;
                                schedulechangeadvisory.AdvisoryType = AdvisoryType.WARNING;
                                pnr.AdvisoryInfo = (pnr.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : pnr.AdvisoryInfo;
                                if (pnr.AdvisoryInfo.Any())
                                    pnr.AdvisoryInfo.Insert(0, schedulechangeadvisory);
                                else
                                    pnr.AdvisoryInfo.Add(schedulechangeadvisory);

                            }
                        }

                        #endregion

                        #region "FACE MASK"

                        if (_configuration.GetValue<bool>("EnableManageResFaceCoveringMsg") && !pnr.IsCanceledWithFutureFlightCredit && !isOTFConversion)
                        {
                            //Check to hide FFC/REFUNDED/VOIDED                                
                            MOBPNRAdvisory faceCoveringMsg = PopulateFaceCoveringMsgContent(applicationId, appVersion);
                            if (faceCoveringMsg != null)
                            {
                                faceCoveringMsg.ContentType = ContentType.FACECOVERING;
                                faceCoveringMsg.AdvisoryType = AdvisoryType.INFORMATION;
                                faceCoveringMsg.IsDefaultOpen = false;
                                pnr.AdvisoryInfo = (pnr.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : pnr.AdvisoryInfo;
                                pnr.AdvisoryInfo.Add(faceCoveringMsg);
                            }
                        }

                        #endregion

                        #region "CONSOLIDATED MSG"

                        if (_manageResUtility.EnableConsolidatedAdvisoryMessage(applicationId, appVersion))
                        {
                            if (pnr.IsPolicyExceptionAlert)
                            {
                                if (_configuration.GetValue<bool>("EnableManageResTravelWaiverDynamicAlertMsg")
                                    && _manageResUtility.CheckTravelWaiverAlertAvailable(response.PNRChangeEligibility))
                                {
                                    var selectedpolicy = GetFirstEligiblePolicy(response.PNRChangeEligibility.Policies);

                                    if (selectedpolicy != null)
                                    {
                                        MOBPNRAdvisory travelwaiveralert = PopulateTravelWaiverDynamicAlertContent
                                       (applicationId, appVersion, response, selectedpolicy, pnr.IsCanceledWithFutureFlightCredit);
                                        if (travelwaiveralert != null)
                                        {
                                            travelwaiveralert.ContentType = ContentType.TRAVELWAIVERALERT;
                                            travelwaiveralert.AdvisoryType = AdvisoryType.INFORMATION;
                                            travelwaiveralert.IsDefaultOpen = false;
                                            pnr.AdvisoryInfo = (pnr.AdvisoryInfo == null)
                                                ? new List<MOBPNRAdvisory>() : pnr.AdvisoryInfo;
                                            pnr.AdvisoryInfo.Add(travelwaiveralert);
                                        }
                                    }
                                }
                                else
                                {
                                    if (!pnr.IsCanceledWithFutureFlightCredit)
                                    {
                                        MOBPNRAdvisory policyexceptioncontent
                                            = await PopulatePolicyExceptionContent(displaycontent: "policyexception_alert_content");
                                        if (policyexceptioncontent != null)
                                        {
                                            policyexceptioncontent.ContentType = ContentType.POLICYEXCEPTION;
                                            policyexceptioncontent.AdvisoryType = AdvisoryType.INFORMATION;
                                            policyexceptioncontent.IsDefaultOpen = false;
                                            pnr.AdvisoryInfo = (pnr.AdvisoryInfo == null)
                                                ? new List<MOBPNRAdvisory>() : pnr.AdvisoryInfo;
                                            pnr.AdvisoryInfo.Add(policyexceptioncontent);
                                        }
                                    }
                                }
                            }


                            if (pnr.IsPetAvailable)
                            {
                                pnr.InCabinPetInfo = null;
                                MOBPNRAdvisory incabinpetcontent = await PopulateIncabinPetContent(displaycontent: "PET_Alert_Content");
                                if (incabinpetcontent != null)
                                {
                                    incabinpetcontent.ContentType = ContentType.INCABINPET;
                                    incabinpetcontent.AdvisoryType = AdvisoryType.INFORMATION;
                                    pnr.AdvisoryInfo = (pnr.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : pnr.AdvisoryInfo;
                                    pnr.AdvisoryInfo.Add(incabinpetcontent);
                                }
                            }
                        }

                        #endregion

                        #region "FFC AGENCY"

                        if (pnr.IsCanceledWithFutureFlightCredit
                          && pnr.IsAgencyBooking
                          && response.Detail.BookingAgency != null
                          && !response.Detail.BookingAgency.IsOnlineAgency
                          && _configuration.GetValue<bool>("EnableManageResFFCAgencyNDCAlertMsg")
                          && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion,
                             _configuration.GetValue<string>("AndroidManageResFFCAgencyNDCAlertVersion"), _configuration.GetValue<string>("iPhoneManageResFFCAgencyNDCAlertVersion")))
                        {
                            if (pnr.Futureflightcredit != null && pnr.Futureflightcredit.Messages != null
                                 && pnr.Futureflightcredit.Messages.Any())
                            {
                                pnr.Futureflightcredit.Messages.Add(new MOBItem
                                {
                                    Id = "FFC_AGENCY_ALRTMSG",
                                    CurrentValue = _configuration.GetValue<string>("FFC_AGENCY_ALRTMSG")
                                });
                            }
                        }


                        if (_manageResUtility.IncludeReshopFFCResidual(applicationId, appVersion)
                              && pnr.IsCanceledWithFutureFlightCredit
                              && pnr.IsAgencyBooking && response.Detail.BookingAgency != null
                              && !response.Detail.BookingAgency.IsOnlineAgency)
                        {
                            string[] stringarray
                                = _manageResUtility.SplitConcatenatedConfigValue("ManageResFFCNonOTAAgencyAlert", "||");
                            if (stringarray != null && stringarray.Length >= 2)
                            {
                                pnr.AdvisoryInfo = new List<MOBPNRAdvisory> {
                                new MOBPNRAdvisory{
                                IsBodyAsHtml = true,
                                Header = stringarray[0],
                                Body = stringarray[1],
                                ContentType = ContentType.FUTUREFLIGHTCREDIT,
                                AdvisoryType = AdvisoryType.INFORMATION,

                                } };
                            }

                            if (pnr.Futureflightcredit != null && pnr.Futureflightcredit.Messages != null
                                && pnr.Futureflightcredit.Messages.Any())
                            {
                                //FFC_Validity_Info,FFC_Credit_Title
                                var includeffcmessage = pnr.Futureflightcredit.Messages.Where
                                    (x => string.Equals(x.Id, "FFC_Validity_Info", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(x.Id, "FFC_Credit_Title", StringComparison.OrdinalIgnoreCase));

                                pnr.Futureflightcredit.Messages = null;

                                if (includeffcmessage != null && includeffcmessage.Any())
                                {
                                    pnr.Futureflightcredit.Messages = new List<MOBItem>();
                                    pnr.Futureflightcredit.Messages.AddRange(includeffcmessage);
                                }
                            }
                        }

                        #endregion

                        #endregion //"ADVISORY MESSAGE"                        

                        pnr.ShouldDisplayUpgradeCabin = UpgradeCabinDisplayCheck(pnr);

                        //TOM Shuttle Offer Eligibility Check
                        if (_configuration.GetValue<bool>("EnablePNRShuttleOfferAvailable"))
                        {
                            pnr.IsShuttleOfferEligible = ShopStaticUtility.ShuttleOfferEligibilityCheck(pnr, shuttleOfferAirportCode: "EWR");
                            if (pnr.IsShuttleOfferEligible)
                            {
                                var ewrOffer = await _manageResUtility.GetEWRShuttleOfferInformation();
                                if (ewrOffer != null)
                                {
                                    if (pnr.ShuttleOfferInformation == null) pnr.ShuttleOfferInformation = new List<MOBShuttleOffer>();
                                    pnr.ShuttleOfferInformation.Add(ewrOffer);
                                }
                            }
                        }
                        //End

                        //End
                        if (_configuration.GetValue<bool>("EnableInflightMealsRefreshment")
                            && GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, appVersion, _configuration.GetValue<string>("Android_InflightMealFeatureSupported_AppVersion"), _configuration.GetValue<string>("iPhone_InflightMealFeatureSupported_AppVersion")))
                        {
                            pnr.IsInflightMealsOfferEligible = true;
                        }

                    }
                    else
                    {
                        //Bug 208169:mAPP: Wrong error message is displayed in viewres flow when non-eticketed PNR has no segments 
                        if (response.Detail.FlightSegments == null)
                        {
                            throw new MOBUnitedException("Your itinerary cannot be retrieved, since there are no flight segments.");
                        }
                    }

                    if (pnr.HasScheduleChanged && pnr.ScheduleChangeInfo != null)
                    {
                        pnr.AdvisoryInfo = null;
                        pnr.Umnr = null;
                        pnr.ELFLimitations = null;
                        pnr.IsIBE = pnr.IsIBELite = pnr.isELF = false;
                    }

                    if (!forWallet)
                    {
                        pnr.Segments = ProcessWaitlistUpgradeVisibilityCSL
                            (pnr, ref hasUpgradeVisibility, lowestEliteLevel, applicationId, hasGSPax, has1KPax, response.Detail);

                        if (!pnr.HasScheduleChanged && !hasUpgradeVisibility)
                        {
                            pnr.Segments = CheckUpgradeEligibilityCSL(pnr.SessionId, lowestEliteLevel, pnr.Segments,
                                  hasGSPax, has1KPax, response.Detail.FlightSegments);
                        }
                    }

                    if (_configuration.GetValue<bool>("EnableVBQEarnedMilesVersion"))
                    {
                        if (_manageResUtility.EnableVBQEarnedMiles(applicationId, appVersion))
                            pnr = await GetVBQEarnedMileDetails(response.Detail.Travelers, pnr);
                        else
                            pnr.SupressLMX = true;
                    }
                    else
                    {
                        await GetLmxDetails(transactionId, recordLocator, applicationId, appVersion, firstSegmentLifted, pnr, isPsSaTravel);
                    }

                    if (pnr != null && pnr.Passengers != null && pnr.Passengers.Count > 0)
                    {
                        StringBuilder passengerLastNames = new StringBuilder();
                        foreach (var item in pnr.Passengers)
                        {
                            passengerLastNames.Append("|");
                            passengerLastNames.Append(item.PassengerName.Last);
                            passengerLastNames.Append("|");
                        }
                        pnr.HasCheckedBags = await _manageResUtility.HasCheckedBags(pnr.RecordLocator, passengerLastNames.ToString());
                    }

                    _logger.LogInformation("GetPNRByRecordLocatorFromCSL {ApplicationId} {ApplicationVersion} {DeviceId} {Response} and {TransactionId}", applicationId, appVersion, deviceId, pnr, transactionId);//Log GetPNRByRecordLocator response

                    //Kirti TFS 181102 Do not show upgrade button if upgrade is avaiable for more than 1 pax per Gavin & Venkat
                    if (pnr != null && pnr.Segments != null)
                    {
                        foreach (var segment in pnr.Segments)
                        {
                            if (segment.Upgradeable && pnr.Passengers != null && pnr.Passengers.Count > 1)
                            {
                                segment.Upgradeable = false;
                            }
                        }
                    }
                    if (_configuration.GetValue<bool>("EnableDisplayGroupedPNRPaxByLastname"))
                    {
                        DisplayGroupedPNRPaxByLastname(ref pnr, lastName);
                    }

                    return (pnr, response);
                }
                else
                {
                    throw new MOBUnitedException(response.Error[0].Description);
                }
            }
            return (pnr, response);

        }

        private async Task<(string, string token)> GetPnrDetailsFromCSL(string transactionId, string recordLocator, string lastName, int applicationId, string appVersion, string actionName, string token, bool usedRecall = false)
        {
            var request = new RetrievePNRSummaryRequest();

            if (!usedRecall)
            {
                request.Channel = _configuration.GetValue<string>("ChannelName");
                request.IsIncludeETicketSDS = _configuration.GetValue<string>("IsIncludeETicketSDS");
                request.IsIncludeFlightRange = _configuration.GetValue<string>("IsIncludeFlightRange");
                request.IsIncludeFlightStatus = _configuration.GetValue<string>("IsIncludeFlightStatus");
                request.IncludeManageResDetails = _configuration.GetValue<string>("IncludeManageResDetails");
                request.IsUpgradeDetails = _configuration.GetValue<string>("IsUpgradeDetails");
                if (_configuration.GetValue<bool>("EnablePCUWaitListPNRManageRes"))
                {
                    request.IsUpgradeDetailsWithEMD = _configuration.GetValue<string>("IsUpgradeDetailsWithEMD");
                }
                request.IsIncludePNRChangeEligibility = _configuration.GetValue<string>("IsIncludePNRChangeEligibility");
                request.IsIncludeLMX = _configuration.GetValue<string>("IsIncludeLMX");
                request.IsIncludePNRDB = _configuration.GetValue<string>("IsIncludePNRDB");
                request.IsIncludeSegmentDuration = _configuration.GetValue<string>("IsIncludeSegmentDuration");
                request.ConfirmationID = recordLocator.ToUpper();
                request.LastName = lastName;
                request.PNRType = string.Empty;
                request.FilterHours = _configuration.GetValue<string>("FilterHours");
                request.IsIncludeChangeFee = _configuration.GetValue<bool>("IsIncludeChangeFee");
                if (_configuration.GetValue<bool>("IsIncludeTravelWaiverDetail"))
                {
                    request.IsIncludeTravelWaiverDetail = _configuration.GetValue<bool>("IsIncludeTravelWaiverDetail");
                }
            }
            else
            {
                request.Channel = _configuration.GetValue<string>("ChannelName");
                request.IsIncludeETicketSDS = _configuration.GetValue<string>("IsIncludeETicketSDS");
                request.IsIncludeFlightRange = _configuration.GetValue<string>("IsIncludeFlightRange");
                request.IsIncludeFlightStatus = _configuration.GetValue<string>("IsIncludeFlightStatus");
                request.IsIncludeLMX = _configuration.GetValue<string>("IsIncludeLMX");
                request.IsIncludePNRDB = _configuration.GetValue<string>("IsIncludePNRDB");
                request.IsIncludeSegmentDuration = _configuration.GetValue<string>("IsIncludeSegmentDuration");
                request.ConfirmationID = recordLocator.ToUpper();
                request.LastName = lastName;
                request.PNRType = string.Empty;
            }

            var jsonResponse = await RetrievePnrDetailsFromCsl(applicationId, transactionId, request);
            return (jsonResponse, token);
        }

        public async Task<string> RetrievePnrDetailsFromCsl(int applicationId, string TransactionId, RetrievePNRSummaryRequest request)
        {
            string sessionId = null;

            var jsonRequest = System.Text.Json.JsonSerializer.Serialize<RetrievePNRSummaryRequest>(request);
            string token = await _dPService.GetAnonymousToken(applicationId, _headers.ContextValues.DeviceId, _configuration);

            var jsonResponse = string.Empty;
            jsonResponse = await _pNRRetrievalService.PNRRetrieval(token, jsonRequest, sessionId, "/PNRRetrieval").ConfigureAwait(false);

            return jsonResponse;
        }

        private void GetSeatDetails(ReservationFlightSegment segment, MOBPNRSegment pnrSegment, List<MOBPNRSeat> pnrSeatList)
        {
            if (pnrSegment != null && pnrSegment.Seats == null)
            {
                pnrSegment.Seats = new List<MOBPNRSeat>();
            }
            int i = 0;
            //per csl response.Flight.Segments[0].seats[0].TravelerSharesIndex  response.Detail.FlightSegments[0].CurrentSeats[0].ReservationNameIndex

            // Added below condition to fix the seat number display in view reservation
            if (segment.CurrentSeats != null && segment.CurrentSeats.Count > 0)
            {
                foreach (var seat in segment.CurrentSeats)
                {
                    // Bug - 196346 - Added below condition to loop and display the missed seat number for the passenger
                    for (int j = 0; j < pnrSeatList.Count; j++)
                    {
                        if (pnrSeatList[j].PassengerSHARESPosition.ToUpper().Trim() ==
                        segment.CurrentSeats[i].ReservationNameIndex.ToUpper().Trim())
                        {
                            // Bug - 209363 - Added below condition to display seat number as single digit - Anku
                            if (seat.Seat.Identifier.StartsWith("0"))
                            {
                                pnrSeatList[j].Number = seat.Seat.Identifier.TrimEnd('*').TrimStart('0');
                            }
                            else
                            {
                                pnrSeatList[j].Number = seat.Seat.Identifier.TrimEnd('*');
                            }
                            //pnrSeatList[i].SeatRow = seatNumbers[i].Substring(0, seatNumbers.Length); // not used in UI 
                            // pnrSeatList[i].SeatRow = seat.Seat.
                            // pnrSeatList[i].SeatLetter = seatNumbers[i].Last().ToString();

                            //csl implementation 
                            pnrSeatList[j].SegmentIndex = segment.SegmentNumber.ToString();
                            pnrSeatList[j].Origin = pnrSegment.Departure.Code;
                            pnrSeatList[j].Destination = pnrSegment.Arrival.Code;

                            if (segment.Seat != null)
                            {

                                //pnrSeatList[i].EDocId = seat.EDocId;
                                pnrSeatList[j].EddNumber =
                                    GetCharactersticValue(seat.Seat.Characteristics, "EDDNumber");

                                double price = 0.0;

                                bool ok =
                                    Double.TryParse(
                                        GetCharactersticValue(seat.Seat.Characteristics, "Price"),
                                        out price);

                                if (ok)
                                {
                                    pnrSeatList[j].Price = price;
                                }

                                pnrSeatList[j].Currency = GetCharactersticValue(
                                    seat.Seat.Characteristics, "Currency");
                                pnrSeatList[j].ProgramCode =
                                    GetCharactersticValue(seat.Seat.Characteristics, "ProgramCode");
                            }
                        }
                    }
                    i++;
                }
                pnrSegment.Seats = pnrSeatList;
            }
            else if (segment.Legs != null && segment.Legs.Count > 0) // This condition is added for display COG flight set on view reservation screen - j.srinivas
            {
                foreach (United.Service.Presentation.SegmentModel.PersonFlightSegment stopSegment in segment.Legs)
                {
                    if (stopSegment.CurrentSeats != null && stopSegment.CurrentSeats.Count() > 0)
                    {
                        foreach (var cogseats in stopSegment.CurrentSeats)
                        {
                            // Added below condition to loop and display the missed seat number for the passenger
                            for (int j = 0; j < pnrSeatList.Count; j++)
                            {
                                if (pnrSeatList.Count > i && (pnrSeatList[j].PassengerSHARESPosition.ToUpper().Trim() ==
                                 cogseats.ReservationNameIndex.ToUpper().Trim()))
                                {
                                    // Bug - 209363 - Added below condition to display seat number as single digit - Anku 
                                    if (cogseats.Seat.Identifier.StartsWith("0"))
                                    {
                                        pnrSeatList[j].Number = cogseats.Seat.Identifier.TrimEnd('*').TrimStart('0');
                                    }
                                    else
                                    {
                                        pnrSeatList[j].Number = cogseats.Seat.Identifier.TrimEnd('*');
                                    }

                                    pnrSeatList[j].SegmentIndex = segment.SegmentNumber.ToString();
                                    pnrSeatList[j].Origin = pnrSegment.Departure.Code;
                                    pnrSeatList[j].Destination = pnrSegment.Arrival.Code;

                                    if (segment.Seat != null)
                                    {
                                        pnrSeatList[j].EddNumber =
                                            GetCogCharactersticValue(cogseats.Seat.Characteristics, "EDDNumber");

                                        double price = 0.0;

                                        bool ok =
                                            Double.TryParse(
                                                GetCogCharactersticValue(cogseats.Seat.Characteristics, "Price"),
                                                out price);

                                        if (ok)
                                        {
                                            pnrSeatList[j].Price = price;
                                        }

                                        pnrSeatList[j].Currency = GetCogCharactersticValue(
                                            cogseats.Seat.Characteristics, "Currency");
                                        pnrSeatList[j].ProgramCode =
                                            GetCogCharactersticValue(cogseats.Seat.Characteristics, "ProgramCode");
                                    }
                                }
                            }
                            i++;
                        }
                    }
                    // for bug 191951 and for bug 191952
                    pnrSegment.Seats = pnrSeatList;
                }
            }
        }

        private Boolean UpgradeCabinDisplayCheck(MOBPNR mobpnr)
        {
            if (mobpnr != null)
            {
                //Reservation is BE / IBE
                //Reservation is group                
                //Reservation is NRSA / PS                
                if (mobpnr.IsIBE || mobpnr.IsIBELite || mobpnr.isELF
                    || mobpnr.isgroup || mobpnr.IsFareLockOrNRSA
                    || mobpnr.PsSaTravel || !mobpnr.IsETicketed)
                {
                    return false;
                }
            }
            return true;
        }

        private async Task<MOBFutureFlightCredit> GetFutureFlightCreditContent(int appId, string appVersion)
        {
            MOBFutureFlightCredit mobfutureflightcredit = new MOBFutureFlightCredit();
            mobfutureflightcredit.Messages = new List<MOBItem>();

            try
            {
                var ffcMessages = await _documentLibraryDynamoDB.GetNewLegalDocumentsForTitles(new List<string> { "FFC_Content" }, _headers.ContextValues.SessionId).ConfigureAwait(false);
                if (ffcMessages != null && ffcMessages.Any())
                {
                    foreach (MOBLegalDocument doc in ffcMessages)
                    {
                        mobfutureflightcredit.Messages.Add(new MOBItem() { Id = doc.Title, CurrentValue = doc.LegalDocument });
                    }
                }
            }
            catch (Exception ex) { return null; }
            return mobfutureflightcredit;
        }

        private string GetEmployeeId(United.Service.Presentation.ReservationResponseModel.ReservationDetail reservationDetail)
        {
            if (reservationDetail == null)
            {
                return string.Empty;
            }
            if (reservationDetail.Detail == null)
            {
                return string.Empty;
            }
            if (reservationDetail.Detail.Remarks.IsNotNullNorEmpty())
            {
                var remarks = reservationDetail.Detail.Remarks.ToList();
                var empIdRemark = remarks.FirstOrDefault(p => p.Description.Contains("EMPID"));

                if (empIdRemark != null)
                {
                    return empIdRemark.Description.Replace("EMPID*", string.Empty);
                }
            }
            return string.Empty;
        }
        public async Task<SeatOffer> GetSeatOffer_CFOP(MOBPNR pnr, MOBRequest request, Reservation cslReservation, string token, string flowType, string sessionId)
        {
            bool isEnableSeatsCSL30Flow = _configuration.GetValue<bool>("EnableCSL30ManageResSelectSeatMap");
            try
            {
                if (flowType == FlowType.VIEWRES_SEATMAP.ToString())
                    return null;

                if (!IsReservationEligibleForSeatOffer(pnr, cslReservation) || (cslReservation.Type.ToList().Exists(p => p.Description == "GROUP")))
                    return null;

                if (isEnableSeatsCSL30Flow)
                {
                    decimal lowestEplusPrice, lowestEMinusPrice = 0;
                    string currencyCode = string.Empty;
                    var tupleRes = await _seatMapCSL30.GetSeatPriceForOfferTile(pnr, cslReservation, token, request, sessionId);
                    lowestEplusPrice = tupleRes.lowestEplusPrice;
                    lowestEMinusPrice = tupleRes.lowestEMinusPrice;
                    currencyCode = tupleRes.currencyCode;

                    return GetEPlusTileInViewReservation(lowestEplusPrice, lowestEMinusPrice, currencyCode);
                }
                else
                {
                    if (_shoppingUtility.IsEnableXmlToCslSeatMapMigration(request.Application.Id, request.Application.Version.Major))
                        return await GetSeatOfferCSL(pnr, token, request);
                }
            }
            catch (Exception ex)
            {
                if (isEnableSeatsCSL30Flow)
                {
                    _logger.LogError("GetSeatOffer_CFOP Exception{exception}", JsonConvert.SerializeObject(ex));
                }
            }
            return null;
        }
        public async Task<SeatOffer> GetSeatOfferCSL(MOBPNR pnr, string token, MOBRequest mobRequest)
        {
            SeatOffer seatOffer = null;
            try
            {
                bool isSSA = _shoppingUtility.EnableSSA(mobRequest.Application.Id, mobRequest.Application.Version.Major);
                bool showEminusOffer = false;

                var hasNonElfSegments = pnr.Segments.Any(s => !(s.IsElf || s.IsIBE));

                if (!hasNonElfSegments && isSSA)
                {
                    var noOfTravelers = 0;
                    int.TryParse(pnr.NumberOfPassengers, out noOfTravelers);
                    showEminusOffer = pnr.Segments.Any(seg => (seg.IsElf || seg.IsIBE)
                                                           && (seg.Seats.IsNullOrEmpty() || !seg.Seats.IsNullOrEmpty() && seg.Seats.Count >= noOfTravelers && seg.Seats.Any(s => s.Price.Equals(0))));
                }
                else
                {
                    bool doNotShowEplusForAlreadyOfferedToggle = _configuration.GetValue<bool>("DoNotShowEplusForAlreadyPurchasedToggle");

                    if (doNotShowEplusForAlreadyOfferedToggle)
                    {
                        bool hasAllEPlusSeats = false;
                        foreach (var seat in pnr.Segments.SelectMany(seg => seg.Seats.Select(seat => seat)))
                        {
                            if (!_shoppingUtility.CheckEPlusSeatCode(seat.ProgramCode))
                            {
                                hasAllEPlusSeats = false;
                                break;
                            }
                            else
                            {
                                hasAllEPlusSeats = true;
                            }
                        }

                        if (hasAllEPlusSeats)
                        {
                            return seatOffer;
                        }
                    }
                }

                if (showEminusOffer || hasNonElfSegments)
                {
                    decimal lowestEplusPrice = 0;
                    decimal lowestEMinusPrice = 0;
                    string currencyCode = string.Empty;

                    var request = BuildSeatMapRequestForOfferTile(pnr);
                    var response = await GetCSLSeatMapResponse(request, pnr.SessionId, token, isSSA, mobRequest);

                    if (response != null && response.SeatMap != null && response.SeatMap.SegmentSeatMap != null && response.SeatMap.SegmentSeatMap.Count > 0)
                    {
                        foreach (United.Service.Presentation.CommonModel.AircraftModel.SeatMap seatmap in response.SeatMap.SegmentSeatMap)
                        {
                            if (seatmap.Aircraft != null && seatmap.Aircraft.Cabins != null && seatmap.Aircraft.Cabins.Count > 0)
                            {
                                foreach (United.Service.Presentation.CommonModel.AircraftModel.Cabin cabin in seatmap.Aircraft.Cabins)
                                {
                                    foreach (United.Service.Presentation.CommonModel.AircraftModel.SeatRow row in cabin.SeatRows)
                                    {
                                        foreach (United.Service.Presentation.CommonModel.AircraftModel.Seat seats in row.Seats)
                                        {
                                            if (seats.Price != null && seats.Price.Totals != null && seats.Price.Totals.Count > 0 && seats.Price.Totals[0] != null && CheckSeatRRowType(seats.Characteristics, "IsAvailableSeat"))
                                            {
                                                if (_shoppingUtility.IsEMinusSeat(seats.Price.PromotionCode))
                                                {
                                                    if (showEminusOffer)
                                                        lowestEMinusPrice = FindLowestPrice(lowestEMinusPrice, Convert.ToDecimal(seats.Price.Totals[0].Amount));
                                                }
                                                else if (hasNonElfSegments && !IsPreferredSeatProgramCode(seats.Price.PromotionCode))
                                                {
                                                    lowestEplusPrice = FindLowestPrice(lowestEplusPrice, Convert.ToDecimal(seats.Price.Totals[0].Amount));
                                                }
                                                if (seats.Price.Totals[0].Currency != null && seats.Price.Totals[0].Currency.Code != null)
                                                {
                                                    currencyCode = seats.Price.Totals[0].Currency.Code.ToString();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    seatOffer = GetEPlusTileInViewReservation(lowestEplusPrice, lowestEMinusPrice, currencyCode);
                }
            }
            catch (System.Exception) { }
            return seatOffer;
        }

        private bool IsPreferredSeatProgramCode(string program)
        {
            string preferredProgramCodes = _configuration.GetValue<string>("PreferredSeatProgramCodes");

            if (!string.IsNullOrEmpty(preferredProgramCodes) && !string.IsNullOrEmpty(program))
            {
                string[] codes = preferredProgramCodes.Split('|');

                foreach (string code in codes)
                {
                    if (code.Equals(program))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        public decimal FindLowestPrice(decimal lowestPrice, decimal currentSeatPrice)
        {
            if (currentSeatPrice == 0)
                return lowestPrice;

            if (lowestPrice == 0 && currentSeatPrice > 0)
                lowestPrice = currentSeatPrice;

            if (lowestPrice > currentSeatPrice)
                lowestPrice = currentSeatPrice;

            return lowestPrice;
        }
        private bool CheckSeatRRowType(Collection<United.Service.Presentation.CommonModel.Characteristic> seatCharacteristics, string code)
        {
            bool seatRRowType = false;
            if (seatCharacteristics != null && seatCharacteristics.Count > 0)
            {
                foreach (United.Service.Presentation.CommonModel.Characteristic objChar in seatCharacteristics)
                {
                    if (objChar.Code != null && objChar.Code.ToUpper().Trim() == code.ToUpper().Trim())
                    {
                        seatRRowType = true;
                        break;
                    }
                }
            }
            return seatRRowType;
        }
        private async Task<United.Service.Presentation.ProductModel.FlightSeatMapDetail> GetCSLSeatMapResponse(United.Service.Presentation.FlightRequestModel.SeatMap request, string sessionId, string token, bool isSSA, MOBRequest mobRequest)
        {
            string _jsonRequest = JsonConvert.SerializeObject(request);
            string response = string.Empty;
            string url = string.Empty;
            var _jsonResponse = string.Empty;
            request.OperatingCarrier = !string.IsNullOrEmpty(_configuration.GetValue<string>("SeatMapForACSubsidiary")) ? _configuration.GetValue<string>("SeatMapForACSubsidiary").Contains(request.OperatingCarrier) ? "AC" : request.OperatingCarrier : request.OperatingCarrier;
            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("CSLService-ViewReservationChangeseats")))
            {
                url = isSSA ? string.Format("GetSeatMapDetailWithFare")
                            : string.Format("GetSeatMapDetail");
            }

            //LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(sessionId, "GetSeatOfferCSL", "Url", mobRequest.Application.Id, mobRequest.Application.Version.Major, mobRequest.DeviceId, url)); //CSL URL log
            //LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(sessionId, "GetSeatOfferCSL", "CSL Request", mobRequest.Application.Id, mobRequest.Application.Version.Major, mobRequest.DeviceId, _jsonRequest)); //CSL request log
            //LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(sessionId, "GetSeatOfferCSL", "CSL Token", mobRequest.Application.Id, mobRequest.Application.Version.Major, mobRequest.DeviceId, token));//CSL Token log
            try
            {
                //_jsonResponse = HttpHelper.Post(url, "application/json; charset=utf-8", token, _jsonRequest);
                response = await _changeSeatService.ViewChangeSeats<string>(token, _jsonRequest, sessionId, url).ConfigureAwait(false);
            }
            catch (System.Net.WebException wex)
            {
                //To implement HandleWebException - Madhavi.
            }
            catch (Exception ex)
            {
                //LogEntries.Add(United.Logger.LogEntry.GetLogEntry<Exception>(sessionId, "GetSeatOfferCSL", "Exception", mobRequest.Application.Id, mobRequest.Application.Version.Major, mobRequest.DeviceId, ex));
            }

            //LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(sessionId, "GetSeatOfferCSL", "Response", mobRequest.Application.Id, mobRequest.Application.Version.Major, mobRequest.DeviceId, _jsonResponse));//CSL response log

            //var response = new Service.Presentation.ProductModel.FlightSeatMapDetail();
            var response1 = JsonConvert.DeserializeObject<United.Service.Presentation.ProductModel.FlightSeatMapDetail>(response);
            return response1;
        }
        private Service.Presentation.FlightRequestModel.SeatMap BuildSeatMapRequestForOfferTile(MOBPNR pnr)
        {
            var request = new Service.Presentation.FlightRequestModel.SeatMap();

            if (pnr.Trips != null && pnr.Trips.Count > 0)
            {
                foreach (MOBTrip trip in pnr.Trips)
                {
                    request.ConfirmationID = pnr.RecordLocator;
                    request.ArrivalAirport = trip.Destination;
                    request.DepartureAirport = trip.Origin;
                    request.CabinType = "ALL";
                    DateTime departuretime;
                    if (DateTime.TryParse(trip.DepartureTime, out departuretime))
                    {
                        request.FlightDate = departuretime.ToString("MM/dd/yyyy");
                    }
                }

                request.Rules = BuildSeatMapRequestWithSegmentsCSL(pnr.Segments);
            }

            return request;
        }
        private Collection<SeatRuleParameter> BuildSeatMapRequestWithSegmentsCSL(List<MOBPNRSegment> segments)
        {
            if (segments == null)
                return null;

            var rules = new Collection<SeatRuleParameter>();
            foreach (var segment in segments)
            {
                var seatRule = new United.Service.Presentation.SegmentModel.SeatRuleParameter();
                seatRule.Flight = new FlightProfile();
                seatRule.Flight.FlightNumber = segment.FlightNumber;
                seatRule.Flight.OperatingCarrierCode = segment.OperationoperatingCarrier != null ? segment.OperationoperatingCarrier.Code : string.Empty;
                seatRule.Flight.MarketingCarrierIndicator = segment.MarketingCarrier != null ? segment.MarketingCarrier.Code : string.Empty;
                seatRule.ProductCode = segment.ProductCode;
                seatRule.FareBasisCode = segment.FareBasisCode;
                //Need to add segment index and get the property from seats team.
                seatRule.Segment = segment.Departure.Code + segment.Arrival.Code;
                seatRule.Flight.DepartureAirport = segment.Departure.Code;
                seatRule.Flight.ArrivalAirport = segment.Arrival.Code;
                rules.Add(seatRule);
            }
            return rules;

        }
        private SeatOffer GetEPlusTileInViewReservation(decimal lowestEplusPrice, decimal lowestEMinusPrice, string currencyCode)
        {
            SeatOffer seatOffer = null;
            if (lowestEplusPrice > 0)
            {
                seatOffer = new SeatOffer();
                seatOffer.Price = lowestEplusPrice;
                seatOffer.CurrencyCode = currencyCode;
                if (seatOffer.CurrencyCode == "USD")
                    seatOffer.CurrencyCode = "$";
                seatOffer.OfferTitle = _configuration.GetValue<string>("SeatOfferTitle");
                seatOffer.OfferText1 = _configuration.GetValue<string>("SeatOfferText1");
                seatOffer.OfferText2 = _configuration.GetValue<string>("SeatOfferText2");
                seatOffer.OfferText3 = _configuration.GetValue<string>("SeatOfferText3");
            }
            else if (lowestEMinusPrice > 0)
            {
                seatOffer = new SeatOffer();
                seatOffer.Price = lowestEMinusPrice;
                seatOffer.CurrencyCode = currencyCode;
                if (seatOffer.CurrencyCode == "USD")
                    seatOffer.CurrencyCode = "$";
                seatOffer.OfferTitle = _configuration.GetValue<string>("SeatOfferTitleForEminus");
                seatOffer.OfferText1 = _configuration.GetValue<string>("SeatOfferText1ForEminus");
                seatOffer.OfferText2 = _configuration.GetValue<string>("SeatOfferText2");
                seatOffer.OfferText3 = _configuration.GetValue<string>("SeatOfferText3");
                seatOffer.IsAdvanceSeatOffer = true;
            }
            return seatOffer;
        }
        private bool IsReservationEligibleForSeatOffer(MOBPNR pnr, Reservation cslReservation)
        {
            if (pnr == null || cslReservation == null)
                return false;
            if (_configuration.GetValue<bool>("EnableSupressWhenScheduleChange") && pnr.HasScheduleChanged)
                return false;

            return _configuration.GetValue<bool>("ShowSeatChange") &&
                    pnr.IsEligibleToSeatChange &&
                   !IsPositiveSpace(cslReservation.Travelers) &&
                   HasAnySeatChangeEligibleEconomyCabinSegment(cslReservation.FlightSegments, cslReservation.Prices);
        }
        private bool IsPositiveSpace(Collection<Traveler> travelers)
        {
            if (travelers == null || !travelers.Any())
                return false;

            return travelers.Any(t => t != null && t.Tickets != null && t.Tickets.Any(ticket => ticket != null && GetCharactersticValue(ticket.Characteristic, "PassengerIndicator").Equals("PS", StringComparison.InvariantCultureIgnoreCase)));
        }
        private bool HasAnySeatChangeEligibleEconomyCabinSegment(Collection<ReservationFlightSegment> flightSegments, Collection<Price> prices)
        {
            if (flightSegments == null || !flightSegments.Any() || prices == null || !prices.Any())
                return false;

            return flightSegments.Any(f => IsActiveSeatChangeEligibleEconomySegment(f, prices));
        }
        private bool IsActiveSeatChangeEligibleEconomySegment(ReservationFlightSegment segment, Collection<Price> prices)
        {
            if (segment == null)
                return false;

            return !ShopStaticUtility.IsUsed(prices, segment.SegmentNumber) &&
                  ShowSeatMapForCarriers(segment.FlightSegment.OperatingAirlineCode) &&
                   IsUaMarketed(segment.FlightSegment.MarketedFlightSegment) &&
                   !IsInChecKInWindow(segment.EstimatedDepartureUTCTime) &&
                   IsBookingClassEconomy(segment.BookingClass);
        }

        private bool IsUaMarketed(Collection<MarketedFlightSegment> marketedFlightSegment)
        {
            return marketedFlightSegment != null &&
                   marketedFlightSegment.Any() &&
                   marketedFlightSegment[0].MarketingAirlineCode.Equals("UA", StringComparison.InvariantCultureIgnoreCase);
        }
        private bool IsBookingClassEconomy(BookingClass bookingClass)
        {
            return bookingClass != null &&
                   bookingClass.Cabin != null &&
                   bookingClass.Cabin.Description.Equals(UAWSFlightReservation.CabinTypes.Coach.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }

        public string GetCharactersticValue(System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.Characteristic> Characteristic, string code)
        {
            string value = string.Empty;

            if (Characteristic != null && Characteristic.Count > 0)
            {
                try
                {
                    for (int i = 0; i < Characteristic.Count; i++)
                    {
                        if (Characteristic[i] != null && Characteristic[i].Code != null && Characteristic[i].Code.Equals(code, StringComparison.InvariantCultureIgnoreCase))
                        {
                            value = Characteristic[i].Value;

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    value = string.Empty;
                }
            }

            return value;
        }

        public MOBItem GetCharactersticValueAndCodeByDescription
         (System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.Characteristic> Characteristic, string desc)
        {
            if (Characteristic != null && Characteristic.Count > 0)
            {
                try
                {
                    var cv = Characteristic.FirstOrDefault
                        (c => string.Equals(c.Description, desc, StringComparison.OrdinalIgnoreCase));
                    if (cv != null)
                    {
                        return new MOBItem { CurrentValue = cv.Value, Id = cv.Code };
                    }
                }
                catch { return null; }
            }
            return null;
        }

        //To-DO: need to fix this method after 22M Merge done - Rajesh K
        //public MOBIRROPSChange ValidateIRROPSStatus(MOBPNRByRecordLocatorRequest request, MOBPNRByRecordLocatorResponse response, ReservationDetail cslReservationDetail, Session session)
        //{
        //    MOBIRROPSChange irropsOptions = null;

        //    if (IsIRROPSPnr(cslReservationDetail) == false) return null;


        //    var cslStrResponse = GetIRROPSStatus_Csl(request, cslReservationDetail, session);

        //    if (!string.IsNullOrEmpty(cslStrResponse))
        //    {
        //        var cslResponse = JsonSerializer.Deserialize
        //            <United.Service.Presentation.EServiceCheckInModel.CheckInIrropResponse>(cslStrResponse);
        //        if (cslResponse?.Errors == null && cslResponse?.TravelPlan?.Reaccom != null)
        //        {
        //            var reaccom = cslResponse?.TravelPlan?.Reaccom;

        //            if (reaccom != null)
        //            {
        //                bool isShoppingAllowed
        //                       = cslResponse?.TravelPlan?.Reaccom?.IsShoppingAllowed ?? false;

        //                bool isFlightCancelled = (reaccom.Indicator
        //                    == Service.Presentation.EServiceCheckInModel.RemarkIndicatorEnum.CXLD);

        //                bool isFlightDelayed = (reaccom.Indicator
        //                    == Service.Presentation.EServiceCheckInModel.RemarkIndicatorEnum.DLYD);
        //                bool isFlightMisconnect = (reaccom.Indicator
        //                        == Service.Presentation.EServiceCheckInModel.RemarkIndicatorEnum.MISX);

        //                bool isRebooked = (reaccom.OldItineraryFlights?.Count() >= 1);

        //                if (isRebooked)
        //                {
        //                    string[] contentarray = _manageResUtility.SplitConcatenatedConfigValue("IRROPS_REBOOK_Content", "||");

        //                    string[] btnControls = _manageResUtility.SplitConcatenatedConfigValue("IRROPS_ButtonOptions", "||");

        //                    var irropsAdvisory = new MOBPNRAdvisory
        //                    {
        //                        AdvisoryType = AdvisoryType.INFORMATION,
        //                        ContentType = ContentType.IRROPS,
        //                        Header = ShopStaticUtility.SplitConcatenatedString(contentarray[0], "|")[1],
        //                        Body = ShopStaticUtility.SplitConcatenatedString(contentarray[1], "|")[1],
        //                        Buttontext = ShopStaticUtility.SplitConcatenatedString(btnControls[1], "|")[1]
        //                    };

        //                    response.PNR.AdvisoryInfo
        //                        = (response.PNR.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : response.PNR.AdvisoryInfo;
        //                    response.PNR.AdvisoryInfo.Insert(0, irropsAdvisory);
        //                }
        //                else if (isFlightDelayed)
        //                {
        //                    if (isShoppingAllowed)
        //                    {
        //                        irropsOptions = SetIRROPSPopUpDisplayContent(response, CHECKINDelayed: true, isMAPPURL: true);
        //                    }
        //                    else
        //                    {
        //                        irropsOptions = SetIRROPSPopUpDisplayContent(response, HARDSTOPDelayed: true, isPhone: true);
        //                    }
        //                }//isFlightDelayed
        //                else if (isFlightCancelled)
        //                {
        //                    if (isShoppingAllowed)
        //                    {
        //                        irropsOptions = SetIRROPSPopUpDisplayContent(response, CHECKINCancelled: true, isMAPPURL: true);
        //                    }
        //                    else
        //                    {
        //                        irropsOptions = SetIRROPSPopUpDisplayContent(response, HARDSTOPCancelled: true, isPhone: true);
        //                    }
        //                }//isFlightCancelled
        //                else if (isFlightMisconnect)
        //                {
        //                    irropsOptions = SetIRROPSPopUpDisplayContent(response, CHECKINMisconnect: true, isMAPPURL: true);
        //                }
        //            }
        //        }
        //    }
        //    return irropsOptions;
        //}

        private bool IsIRROPSPnr(ReservationDetail cslReservationDetail)
        {
            try
            {
                //if (cslReservationDetail?.Detail?.FlightSegments == null 
                //    || !cslReservationDetail.Detail.FlightSegments.Any()) return false;               

                //foreach (ReservationFlightSegment segment in cslReservationDetail.Detail.FlightSegments)
                //{
                //    if(segment != null && segment.Characteristic != null)
                //    {
                //        var strIRROPSPnr = United.Mobile.DAL.Utility.GetBooleanFromCharacteristics
                //                    (segment.Characteristic, "IRROP");
                //        var isIRROPSPnr = (strIRROPSPnr.HasValue) ? strIRROPSPnr.Value : false;
                //        if (isIRROPSPnr == true) return true;
                //    }
                //}

                if (cslReservationDetail?.Detail?.Characteristic == null
                   || !cslReservationDetail.Detail.Characteristic.Any()) return false;

                var strIRROPSPnr = _shoppingUtility.GetBooleanFromCharacteristics
                                    (cslReservationDetail.Detail.Characteristic, "IRROPS");
                return strIRROPSPnr.HasValue ? strIRROPSPnr.Value : false;
            }
            catch { }
            return false;
        }

        private MOBIRROPSChange SetIRROPSPopUpDisplayContent(MOBPNRByRecordLocatorResponse response,
    bool CHECKINDelayed = false, bool HARDSTOPDelayed = false, bool CHECKINCancelled = false,
    bool HARDSTOPCancelled = false, bool CHECKINMisconnect = false, bool TRHDelayed = false, bool TRHCancelled = false, bool isPhone = false, bool isMAPPURL = false)
        {
            MOBIRROPSChange irropsOptions = null;

            string configkey = (CHECKINDelayed) ? "IRROPS_CHECKIN_DelayContent"
                : (HARDSTOPDelayed) ? "IRROPS_HARDSTOP_DelayContent"
                : (CHECKINCancelled) ? "IRROPS_CHECKIN_CancelContent"
                : (HARDSTOPCancelled) ? "IRROPS_HARDSTOP_CancelContent"
                : (CHECKINMisconnect) ? "IRROPS_CHECKIN_MisconnectContent"
                : (TRHDelayed) ? "IRROPS_TRH_DLYD"
                : (TRHCancelled) ? "IRROPS_TRH_CNXLD"
                : string.Empty;

            string[] contentarray
                = _shoppingUtility.SplitConcatenatedConfigValue(configkey, "||");

            string[] btnControls
                = _shoppingUtility.SplitConcatenatedConfigValue("IRROPS_ButtonOptions", "||");

            string phonenumber
                = (isPhone) ? _configuration.GetValue<string>("IRROPS_CustomerSupportNumber") : string.Empty;

            if (contentarray?.Length >= 3 && btnControls?.Length >= 4)
            {
                string header = ShopStaticUtility.SplitConcatenatedString(contentarray[0], "|")[1];
                string body = string.Format(ShopStaticUtility.SplitConcatenatedString(contentarray[1], "|")[1], phonenumber);
                string alert = string.Format(ShopStaticUtility.SplitConcatenatedString(contentarray[2], "|")[1], phonenumber);
                string buttonText = string.Empty;

                irropsOptions = new MOBIRROPSChange
                {
                    displayHeader = header,
                    displayBody = body,
                    displayOptions = new List<MOBDisplayItem>()
                };

                if (isPhone)
                {
                    var displayoptn = new MOBDisplayItem
                    {
                        id = Convert.ToString(Convert.ToInt32(MOBDisplayType.PHONE)),
                        displayType = Convert.ToString(MOBDisplayType.PHONE),
                        labelText = phonenumber,
                        displayText = ShopStaticUtility.SplitConcatenatedString(btnControls[3], "|")[1]
                    };
                    irropsOptions.displayOptions.Add(displayoptn);
                }

                if (isMAPPURL)
                {
                    buttonText = ShopStaticUtility.SplitConcatenatedString(btnControls[0], "|")[1];
                    var displayoptn = new MOBDisplayItem
                    {
                        id = Convert.ToString(Convert.ToInt32(MOBDisplayType.MAPPURL)),
                        displayType = Convert.ToString(MOBDisplayType.MAPPURL),
                        displayText = buttonText
                    };
                    irropsOptions.displayOptions.Add(displayoptn);
                }

                if (irropsOptions?.displayOptions != null && irropsOptions.displayOptions.Any())
                {
                    var displayoptn = new MOBDisplayItem
                    {
                        id = Convert.ToString(Convert.ToInt32(MOBDisplayType.NONE)),
                        displayType = Convert.ToString(MOBDisplayType.NONE),
                        displayText = ShopStaticUtility.SplitConcatenatedString(btnControls[2], "|")[1]
                    };
                    irropsOptions.displayOptions.Add(displayoptn);
                }

                if (CHECKINCancelled)
                {
                    var displayoptn = new MOBDisplayItem
                    {
                        id = Convert.ToString(Convert.ToInt32(MOBDisplayType.MAPPCANCEL)),
                        displayType = Convert.ToString(MOBDisplayType.MAPPCANCEL),
                        displayText = ShopStaticUtility.SplitConcatenatedString(btnControls[4], "|")[1]
                    };
                    irropsOptions.displayOptions.Add(displayoptn);
                }

                var irropsAdvisory = new MOBPNRAdvisory
                {
                    AdvisoryType = AdvisoryType.WARNING,
                    ContentType = (TRHCancelled || TRHDelayed) ? ContentType.TRH : ContentType.IRROPS,
                    Header = header,
                    Body = alert,
                    Buttontext = (TRHCancelled || TRHDelayed) ? SplitConcatenatedString(btnControls[5], "|")[1] : buttonText
                };

                if (isMAPPURL || TRHCancelled || TRHDelayed)
                    response.PNR.AdvisoryInfo
                        = (response.PNR.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : response.PNR.AdvisoryInfo;
                response.PNR.AdvisoryInfo.Insert(0, irropsAdvisory);
            }
            return irropsOptions;
        }

        public async Task<MOBFutureFlightCredit> GetFutureFlightCreditMessages(int applicationId, string appVersion)
        {
            return await GetFutureFlightCreditContent(applicationId, appVersion);
        }


        //To-DO: need to fix this method after 22M Merge done - Rajesh K
        //private string GetIRROPSStatus_Csl(MOBPNRByRecordLocatorRequest request,ReservationDetail cslReservationDetail, Session session)
        //{            
        //    string cslRequest = JsonSerializer.Serialize<ReservationDetail>(cslReservationDetail);

        //    string url = _configuration.GetValue<string>("IRROPS_ValidateStatusEndPoint");

        //    string eServiceAuthorization = _configuration.GetValue<string>("IRROPS_eServiceAuthorization");

        //    try
        //    {
        //        if (this.levelSwitch.TraceError)
        //        {
        //            LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>
        //            (request.SessionId, "ValidateIRROPSStatus", "Request-Url", request.Application.Id,
        //            request.Application.Version.Major, request.DeviceId, url));
        //        }

        //        IDictionary<string, string> headers = new Dictionary<string, string>();
        //        headers.Add(new KeyValuePair<string, string>("Authorization", session.Token));
        //        headers.Add(new KeyValuePair<string, string>("eServiceAuthorization", eServiceAuthorization));


        //        var cslstrResponse = HttpHelper.PostAsync(url, cslRequest, headers, retry: 1);

        //        if (this.levelSwitch.TraceWarning)
        //        {
        //            LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(request.SessionId, "ValidateIRROPSStatus", "CSL-Response",
        //                request.Application.Id, request.Application?.Version?.Major, request.DeviceId, cslstrResponse));
        //        }

        //        return cslstrResponse;
        //    }
        //    catch (Exception exc)
        //    {
        //        LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(request.SessionId, "ValidateIRROPSStatus", "CSL-Exception",
        //                request.Application.Id, request.Application?.Version?.Major, request.DeviceId, Utility.ExceptionMessages(exc)));
        //    }
        //    return string.Empty;
        //}




        #region ancillary
        public async Task<MOBPNR> GetPNRByRecordLocator(string transactionId, string recordLocator, string lastName, string languageCode, int applicationId, string appVersion, bool forWallet)
        {
            MOBPNR pnr = null;
            //string logTransactionId = transactionId.Substring(0, transactionId.IndexOf('|'));

            //Check if timer is required
            //UAWSFlightReservation.wsFlightResResponse response = null;

            // FlightReservationSoapClient soapClient = new FlightReservationSoapClient();
            // UAWSFlightReservation.FlightReservationSoap soapClient = new UAWSFlightReservation.FlightReservationSoap();
            //if (_configuration.GetValue<string>("TimeOutMilliSecondsForXMLGetFlightReservation") != null)
            //{
            //    int timeOutMilliSeconds = _configuration.GetValue<string>("TimeOutMilliSecondsForXMLGetFlightReservation") != null ? Convert.ToInt32(_configuration.GetValue<string>("TimeOutMilliSecondsForXMLGetFlightReservation").Trim()) : 10000;
            //    soapClient.Timeout = timeOutMilliSeconds;
            //}


            var response = await _flightReservationService.GetFlightReservation(recordLocator, lastName, _configuration.GetValue<string>("AccessCode - FlightReservation"), "2.0").ConfigureAwait(false);

            //string xml = _fitbitUtility..Serialize(response.Flight, typeof(COWSFlightReservation.Flight));

            bool isAllFirstCabin = true;
            bool isSpaceAvailblePassRider = false;
            bool isPositiveSpace = false;
            int lowestEliteLevel = 0;
            bool hasGSPax = false;
            bool has1KPax = false;
            bool hasUpgradeVisibility = false;
            bool firstSegmentLifted = false;
            bool isPsSaTravel = false;
            if ((response.Errors == null || response.Errors.Length == 0))
            {
                if (response.Flight != null && !response.Flight.NoItin) // DO NOT CHECK IN THIS YET. Need to confirm to use this condition (response.Flight.NoItin == true means cancelled PNR) not to show the View Reservation details. 
                {                                                       // Because as per priya there should be Re Accomidated PNR that we should show View Reservation so if for that PNR if response.Flight.NoItin == true then how to show those Re Accommed PNRS?.                        
                    //if (this.levelSwitch.TraceError)
                    //{
                    //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<UAWSFlightReservation.wsFlightResResponse>(transactionId, "DOTCOMWebserviceGetFlightReservationResponse", "Response", response));//Common Login Code
                    //}

                    if (response.Flight.Segments == null || response.Flight.Segments.Length == 0)
                    {
                        throw new MOBUnitedException("No segments found for this itinerary.");
                    }

                    #region
                    pnr = new MOBPNR();
                    pnr.SessionId = response.Flight.SessionId;
                    pnr.DateCreated = TopHelper.FormatDatetime(Convert.ToDateTime(response.Flight.CreateDate).ToString("yyyyMMdd hh:mm tt"), languageCode);
                    //pnr.Destination = response.Flight.Trips[0].Destination;

                    if (response.Flight.CorporateTravel != null)
                    {
                        if (response.Flight.CorporateTravel.IsValid && !string.IsNullOrEmpty(response.Flight.CorporateTravel.VendorName)) //&& response.Flight.CorporateTravel.VendorName.Equals("Concur", StringComparison.InvariantCultureIgnoreCase))
                        {
                            pnr.SyncedWithConcur = "Synced with " + response.Flight.CorporateTravel.VendorName;
                        }
                    }

                    pnr.FarelockPurchaseMessage = _configuration.GetValue<string>("FarelockPurchaseMessage");
                    pnr.EarnedMilesHeader = _configuration.GetValue<string>("EarnedMilesHeader");
                    pnr.EarnedMilesText = _configuration.GetValue<string>("EarnedMilesText");
                    pnr.IneligibleToEarnCreditMessage = _configuration.GetValue<string>("IneligibleToEarnCreditMessage");
                    pnr.OaIneligibleToEarnCreditMessage = _configuration.GetValue<string>("OaIneligibleToEarnCreditMessage");
                    pnr.AwardTravel = response.Flight.RewardTravel;

                    appVersion = string.IsNullOrEmpty(appVersion) ? string.Empty : appVersion.ToUpper();
                    if (_manageResUtility.IsVersionEnableMP2015LMXCallOrFareLock(appVersion, _configuration.GetValue<string>("AppVersionsForFareLock")))
                    {
                        string farelockExpirationDate = string.Empty;

                        if (!string.IsNullOrEmpty(response.Flight.FareLockExpireDate))
                        {
                            DateTime localTime = Convert.ToDateTime(response.Flight.FareLockExpireDate);
                            TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                            DateTime utcTime = TimeZoneInfo.ConvertTimeToUtc(localTime, timeZoneInfo);
                            farelockExpirationDate = utcTime.ToString("M/d/yyyy hh:mm:ss tt");
                        }
                        pnr.FarelockExpirationDate = farelockExpirationDate;
                    }

                    pnr.EmailAddress = response.Flight.EmailAddress;

                    pnr.RecordLocator = response.Flight.RecordLocator;

                    //pnr.RecordLocator = response.Input.ConfirmationNumber;

                    if (response.Flight.OARecordLocators != null && response.Flight.OARecordLocators.Length > 0)
                    {
                        pnr.OARecordLocators = new List<MOBOARecordLocator>();
                        pnr.OARecordLocatorMessageTitle = _configuration.GetValue<string>("OARecordLocatorMessageTitle");
                        pnr.OARecordLocatorMessage = _configuration.GetValue<string>("OARecordLocatorMessage");

                        foreach (UAWSFlightReservation.OARecordLocator oaRecordLocator in response.Flight.OARecordLocators)
                        {
                            MOBOARecordLocator locator = new MOBOARecordLocator();
                            locator.CarrierCode = oaRecordLocator.CarrierCode;
                            locator.CarrierName = await _airportDynamoDB.GetCarrierInfo(locator.CarrierCode, _headers.ContextValues.SessionId);
                            locator.RecordLocator = oaRecordLocator.RecordLocator;
                            locator.CreateDate = oaRecordLocator.CreateDate;

                            if (locator.CarrierCode.Equals("UA"))
                            {
                                pnr.UARecordLocator = locator.RecordLocator;
                            }

                            if (!locator.CarrierCode.Equals(locator.CarrierName) && !locator.CarrierCode.Equals("UA"))
                            {
                                pnr.OARecordLocators.Add(locator);
                            }
                        }
                    }

                    if (response.Flight.OldRecordLocators != null && response.Flight.OldRecordLocators.Length > 0)
                    {
                        //PopulateOldRecordLocators(ref pnr, response.Flight.OldRecordLocators);
                    }

                    pnr.IsActive = response.Flight.ActiveTicketsExist;
                    if (response.Flight.Travelers != null)
                    {
                        pnr.NumberOfPassengers = response.Flight.Travelers.Length.ToString();

                        if (response.Flight.Travelers.Length > 0)
                        {
                            lowestEliteLevel = response.Flight.Travelers[0].EliteLevel;
                            foreach (var traveler in response.Flight.Travelers)
                            {
                                if (!pnr.PsSaTravel && !string.IsNullOrEmpty(traveler.PassengerIndicator) && (traveler.PassengerIndicator.Equals("SA") || traveler.PassengerIndicator.Equals("PS")))
                                {
                                    isPsSaTravel = true;
                                }

                                if (!string.IsNullOrEmpty(_configuration.GetValue<string>("EnableEResBooking")) && Convert.ToBoolean(_configuration.GetValue<string>("EnableEResBooking")))
                                {
                                    if (!pnr.PsSaTravel && !string.IsNullOrEmpty(traveler.PassengerIndicator) && (traveler.PassengerIndicator.Equals("SA") || traveler.PassengerIndicator.Equals("PS")))
                                    {
                                        if (IsEResBetaTesterRecordLocator(pnr.RecordLocator, applicationId, appVersion))
                                        {
                                            pnr.PsSaTravel = true;
                                        }
                                    }
                                }

                                if (traveler.EliteLevel < lowestEliteLevel)
                                {
                                    lowestEliteLevel = traveler.EliteLevel;
                                }

                                if (traveler.EliteLevel == 5)
                                {
                                    hasGSPax = true;
                                }

                                if (traveler.EliteLevel == 4)
                                {
                                    has1KPax = true;
                                }
                            }
                        }
                    }
                    //pnr.Origin = response.Flight.Trips[0].Origin;

                    pnr.Trips = new List<MOBTrip>();
                    foreach (UAWSFlightReservation.Trip trip in response.Flight.Trips)
                    {
                        #region
                        MOBTrip pnrTrip = new MOBTrip();
                        pnrTrip.Origin = trip.Origin;
                        string airportName = string.Empty;
                        string cityName = string.Empty;
                        var tupleRes = await _airportDynamoDB.GetAirportCityName(pnrTrip.Origin, _headers.ContextValues.SessionId, airportName, cityName);
                        airportName = tupleRes.airportName;
                        cityName = tupleRes.cityName;
                        pnrTrip.OriginName = airportName;
                        //pnrTrip.OriginName = trip.DecodedOrigin;
                        pnrTrip.Destination = trip.Destination;
                        airportName = string.Empty;
                        cityName = string.Empty;
                        tupleRes = await _airportDynamoDB.GetAirportCityName(pnrTrip.Origin, _headers.ContextValues.SessionId, airportName, cityName);
                        airportName = tupleRes.airportName;
                        cityName = tupleRes.cityName;
                        pnrTrip.DestinationName = airportName;
                        pnrTrip.Index = trip.Index;
                        //pnrTrip.DestinationName = trip.DecodedDestination;

                        DateTime departureTime;
                        if (DateTime.TryParse(trip.DepartDate, out departureTime))
                        {
                            pnrTrip.DepartureTime = departureTime.ToString("MM/dd/yyyy hh:mm tt");
                        }

                        DateTime arrivalTime;
                        if (DateTime.TryParse(trip.DestinationDate, out arrivalTime))
                        {
                            pnrTrip.ArrivalTime = arrivalTime.ToString("MM/dd/yyyy hh:mm tt");
                        }

                        pnr.Trips.Add(pnrTrip);
                        #endregion
                    }

                    if (pnr.Trips.Count > 0)
                    {
                        pnr.LastTripDateDepartureDate = pnr.Trips[pnr.Trips.Count - 1].DepartureTime;
                        pnr.LastTripDateArrivalDate = pnr.Trips[pnr.Trips.Count - 1].ArrivalTime;
                    }

                    pnr.Passengers = new List<MOBPNRPassenger>();
                    pnr.PetRecordLocators = new List<string>();
                    foreach (UAWSFlightReservation.Traveler traveler in response.Flight.Travelers)
                    {
                        #region Get passengers in the PNR
                        MOBPNRPassenger pnrPassenger = new MOBPNRPassenger();
                        pnrPassenger.PassengerName = new United.Mobile.Model.Common.MOBName();
                        pnrPassenger.PassengerName.First = traveler.FirstName;
                        pnrPassenger.PassengerName.Middle = traveler.MiddleName;
                        pnrPassenger.PassengerName.Last = traveler.LastName;
                        pnrPassenger.PassengerName.Suffix = traveler.Suffix;
                        pnrPassenger.PassengerName.Title = traveler.Title;
                        pnrPassenger.SHARESPosition = traveler.SHARESPosition;

                        if (traveler.RewardPrograms != null && traveler.RewardPrograms.Length > 0)
                        {
                            foreach (var rewardProgram in traveler.RewardPrograms)
                            {
                                if (!string.IsNullOrEmpty(rewardProgram.VendorCode) && rewardProgram.VendorCode.Equals("UA"))
                                {
                                    MOBEliteStatus eliteStatus = new MOBEliteStatus(_configuration);
                                    eliteStatus.Code = rewardProgram.EliteLevel.ToString();
                                    pnrPassenger.MileagePlus = new MOBCPMileagePlus();
                                    pnrPassenger.MileagePlus.MileagePlusId = rewardProgram.ProgramMemberId;
                                    pnrPassenger.MileagePlus.CurrentEliteLevel = rewardProgram.EliteLevel;
                                    pnrPassenger.MileagePlus.CurrentEliteLevelDescription = eliteStatus.Description;
                                    pnrPassenger.MileagePlus.VendorCode = rewardProgram.VendorCode;
                                }
                                else
                                {
                                    string vendorName = GetStarRewardVendorName(rewardProgram.VendorCode);
                                    if (!string.IsNullOrEmpty(vendorName))
                                    {
                                        if (pnrPassenger.OaRewardPrograms == null)
                                        {
                                            pnrPassenger.OaRewardPrograms = new List<MOBRewardProgram>();
                                        }
                                        MOBRewardProgram oaRewardProgram = new MOBRewardProgram();
                                        oaRewardProgram.StarEliteStatus = rewardProgram.StarEliteStatus;
                                        oaRewardProgram.EliteLevel = rewardProgram.EliteLevel;
                                        oaRewardProgram.ProgramCode = rewardProgram.ProgramCode;
                                        oaRewardProgram.ProgramDescription = rewardProgram.ProgramDescription;
                                        oaRewardProgram.ProgramType = rewardProgram.ProgramType;
                                        oaRewardProgram.OtherVendorCodeStatus = rewardProgram.OtherVendorCodeStatus;
                                        oaRewardProgram.VendorCode = rewardProgram.VendorCode;
                                        oaRewardProgram.ProgramMemberId = rewardProgram.ProgramMemberId;
                                        oaRewardProgram.VendorDescription = vendorName;

                                        pnrPassenger.OaRewardPrograms.Add(oaRewardProgram);
                                    }
                                }
                            }
                        }

                        pnr.Passengers.Add(pnrPassenger);
                        if (!isSpaceAvailblePassRider && traveler.PassengerIndicator != null && traveler.PassengerIndicator.ToUpper().Trim() == "SA")
                        {
                            isSpaceAvailblePassRider = true;
                        }
                        else if (traveler.PassengerIndicator == null && traveler.PassCode != null && traveler.PassCode.ToUpper().Trim() != "") // This check is for Jump Set booking  the traveler.PassengerIndicator is returning NULL.
                        {
                            isSpaceAvailblePassRider = true;
                        }
                        else if (!isPositiveSpace && traveler.PassengerIndicator != null && traveler.PassengerIndicator != null && traveler.PassengerIndicator.ToUpper().Trim() == "PS")
                        {
                            isPositiveSpace = true;
                        }

                        #region Get Pet Information Kept Commented If need in future about More pet information
                        //if (traveler.Pets != null)
                        //{
                        //    if (!pnr.IsInCabinPetInReservation)
                        //    {
                        //        pnr.IsInCabinPetInReservation = true;
                        //    }
                        //    foreach(COWSFlightReservation.Pet petInfo in traveler.Pets)
                        //    {
                        //        PetInformation pet = new PetInformation();
                        //        pet.AnimalType = petInfo.TypeDescription;
                        //        pet.BirthDate = petInfo.BirthDate;
                        //        pet.Breed = petInfo.BreedDescription;
                        //        pet.Name = petInfo.Name;
                        //        pet.Title = petInfo.Name + " - In Cabin Pet";
                        //        pet.Weight = petInfo.Weight.ToString() + " lbs.";
                        //        pnr.Pets.Add(pet);
                        //    }
                        //}
                        #endregion
                        #region Get Pet Record Locators
                        if (traveler.Pets != null)
                        {
                            foreach (UAWSFlightReservation.Pet petInfo in traveler.Pets)
                            {
                                if (!string.IsNullOrEmpty(petInfo.RecordLocator))
                                {
                                    pnr.PetRecordLocators.Add(petInfo.RecordLocator.Trim());
                                }
                            }
                        }
                        #endregion
                    }

                    int pastSegmentsCount = 0;
                    pnr.Segments = new List<MOBPNRSegment>();
                    bool firstSegment = true;
                    foreach (UAWSFlightReservation.Segment segment in response.Flight.Segments)
                    {
                        if ((firstSegment && segment.SharesIndex != 1) || (firstSegment && segment.SharesIndex == 1 && segment.Past))
                        {
                            firstSegmentLifted = true;
                        }

                        firstSegment = false;

                        #region
                        if (segment.Past)
                        {
                            pastSegmentsCount = pastSegmentsCount + 1;
                        }
                        else
                        {
                            if (isAllFirstCabin && segment.Carrier.ToUpper().Trim() == "UA" && !segment.CheckedIn && !segment.CheckinTriggered && segment.Cabin != null && segment.Cabin == UAWSFlightReservation.CabinTypes.Coach)
                            {
                                isAllFirstCabin = false;
                            }
                        }
                        List<MOBPNRSeat> pnrSeatList = new List<MOBPNRSeat>();
                        foreach (UAWSFlightReservation.Traveler traveler in response.Flight.Travelers)
                        {
                            MOBPNRSeat pnrSeat = new MOBPNRSeat();
                            pnrSeat.PassengerSHARESPosition = traveler.SHARESPosition;
                            pnrSeatList.Add(pnrSeat);
                        }

                        MOBPNRSegment pnrSegment = new MOBPNRSegment();

                        if (segment.Waitlisted)
                        {
                            pnrSegment.Waitlisted = true;
                        }

                        pnrSegment.ActionCode = segment.ActionCode;

                        pnrSegment.LowestEliteLevel = lowestEliteLevel;

                        pnrSegment.ActualMileage = segment.ActualMileage.ToString();
                        pnrSegment.Arrival = new MOBAirport();
                        pnrSegment.Arrival.Code = segment.Destination;
                        //pnrSegment.Arrival.Name = segment.DecodedDestination;
                        string airportName = string.Empty;
                        string cityName = string.Empty;
                        var tupleRes = await _airportDynamoDB.GetAirportCityName(segment.Destination, _headers.ContextValues.SessionId, airportName, cityName);
                        airportName = tupleRes.airportName;
                        cityName = tupleRes.cityName;
                        pnrSegment.Arrival.Name = airportName;
                        pnrSegment.Arrival.City = cityName;

                        pnrSegment.Aircraft = new MOBAircraft();
                        pnrSegment.Aircraft.Code = segment.Equipment;
                        pnrSegment.Aircraft.LongName = segment.EquipmentDescription;
                        //pnrSegment.Arrival = new Airport();
                        //pnrSegment.Arrival.Code = segment.Destination;
                        //pnrSegment.Arrival.Name = segment.DecodedDestination;
                        pnrSegment.ClassOfService = segment.ServiceClass;
                        pnrSegment.ClassOfServiceDescription = segment.ServiceClassDescription;
                        pnrSegment.Departure = new MOBAirport();
                        pnrSegment.Departure.Code = segment.Origin;
                        //pnrSegment.Departure.Name = segment.DecodedOrigin;
                        airportName = string.Empty;
                        cityName = string.Empty;
                        tupleRes = await _airportDynamoDB.GetAirportCityName(segment.Destination, _headers.ContextValues.SessionId, airportName, cityName);
                        airportName = tupleRes.airportName;
                        cityName = tupleRes.cityName;
                        pnrSegment.Departure.Name = airportName;
                        pnrSegment.Departure.City = cityName;

                        pnrSegment.EMP = string.Format("{0}%", segment.EMP);


                        pnrSegment.FlightNumber = segment.FlightNumber;

                        if (!string.IsNullOrEmpty(segment.TravelTime))
                        {
                            if (segment.TravelTime.ToUpper().IndexOf("HR") == -1)
                            {
                                if (!string.IsNullOrEmpty(appVersion) && appVersion.ToUpper().Equals("2.1.8I"))
                                {
                                    pnrSegment.FlightTime = "0 HR " + segment.TravelTime;
                                }
                                else
                                {
                                    pnrSegment.FlightTime = segment.TravelTime;
                                }
                            }
                            else
                            {
                                if (segment.TravelTime.ToUpper().IndexOf("MN") == -1)
                                {
                                    if (!string.IsNullOrEmpty(appVersion) && appVersion.ToUpper().Equals("2.1.8I"))
                                    {
                                        pnrSegment.FlightTime = segment.TravelTime + " 0 MN";
                                    }
                                    else
                                    {
                                        pnrSegment.FlightTime = segment.TravelTime;
                                    }
                                }
                                else
                                {
                                    pnrSegment.FlightTime = segment.TravelTime;
                                }
                            }
                        }
                        else
                        {
                            pnrSegment.FlightTime = segment.TravelTime;
                        }

                        pnrSegment.GroundTime = segment.GroundTime;
                        pnrSegment.MarketingCarrier = new MOBAirline();

                        pnrSegment.MarketingCarrier.Code = segment.MarketingCarrier;
                        pnrSegment.MarketingCarrier.Name = segment.CarrierDescription;

                        if (pnrSegment.MarketingCarrier.Name.ToLower().Equals("united"))
                        {
                            pnrSegment.MarketingCarrier.Name = "United Airlines";
                        }

                        pnrSegment.MarketingCarrier.FlightNumber = pnrSegment.FlightNumber;

                        pnrSegment.CabinType = segment.CabinTypeText;

                        pnrSegment.Meal = segment.MealDescription;
                        pnrSegment.MileagePlusMileage = segment.OnePassMileage.ToString();
                        pnrSegment.OperationoperatingCarrier = new MOBAirline();


                        pnrSegment.OperationoperatingCarrier.Code = segment.OperatingCarrier;
                        pnrSegment.OperationoperatingCarrier.FlightNumber = segment.OFN;

                        try
                        {
                            if (pnrSegment.OperationoperatingCarrier.Code != null && pnrSegment.OperationoperatingCarrier.Code.Equals("UA"))
                            {
                                if (segment.Dei != null && !string.IsNullOrEmpty(segment.Dei.DEI50))
                                {
                                    string[] dei50 = segment.Dei.DEI50.Split(' ');
                                    if (dei50.Length == 2)
                                    {
                                        pnrSegment.OperationoperatingCarrier.FlightNumber = dei50[1];
                                    }
                                }
                            }
                        }
                        catch (Exception) { }



                        pnrSegment.OperationoperatingCarrier.Name = segment.OperationalDisclosureCarrier;

                        if (pnrSegment.OperationoperatingCarrier.Name.ToLower().Equals("united"))
                        {
                            pnrSegment.OperationoperatingCarrier.Name = "United Airlines";
                        }


                        pnrSegment.CodeshareCarrier = new MOBAirline();

                        pnrSegment.CodeshareCarrier.Code = segment.CDMC;
                        pnrSegment.CodeshareFlightNumber = segment.CDFN;
                        pnrSegment.CodeshareCarrier.FlightNumber = segment.CDFN;


                        pnrSegment.ScheduledArrivalDateTime = GeneralHelper.FormatDatetime(Convert.ToDateTime(segment.DestinationDate).ToString("yyyyMMdd hh:mm tt"), languageCode);
                        pnrSegment.ScheduledDepartureDateTime = GeneralHelper.FormatDatetime(Convert.ToDateTime(segment.DepartDate).ToString("yyyyMMdd hh:mm tt"), languageCode);

                        pnrSegment.ScheduledArrivalDateTimeGMT = await _manageResUtility.GetGMTTime(segment.DestinationDate, pnrSegment.Arrival.Code, _headers.ContextValues.SessionId);
                        pnrSegment.ScheduledDepartureDateTimeGMT = await _manageResUtility.GetGMTTime(segment.DepartDate, pnrSegment.Departure.Code, _headers.ContextValues.SessionId);

                        pnrSegment.TotalTravelTime = segment.TotalTravelTime;

                        if (segment.UpgVis != null)
                        {
                            pnrSegment.UpgradeVisibility = GetUpgradeVisibility(segment.UpgVis, segment, ref pnrSegment);
                            if (pnrSegment.UpgradeVisibility != null && (pnrSegment.UpgradeVisibility.UpgradeStatus == MOBUpgradeEligibilityStatus.Requested || pnrSegment.UpgradeVisibility.UpgradeStatus == MOBUpgradeEligibilityStatus.Qualified) && (pnrSegment.UpgradeVisibility.UpgradeType == MOBUpgradeType.ComplimentaryPremierUpgrade || pnrSegment.UpgradeVisibility.UpgradeType == MOBUpgradeType.GlobalPremierUpgrade || pnrSegment.UpgradeVisibility.UpgradeType == MOBUpgradeType.MileagePlusUpgradeAwards || pnrSegment.UpgradeVisibility.UpgradeType == MOBUpgradeType.RegionalPremierUpgrade))
                            {
                                pnrSegment.UpgradeEligible = true;
                            }
                            if (pnrSegment.UpgradeVisibility != null && pnrSegment.UpgradeVisibility.UpgradeType != MOBUpgradeType.PremierInstantUpgrade)
                            {
                                hasUpgradeVisibility = true;
                            }
                        }

                        if (segment.Seats != null)
                        {
                            pnrSegment.Seats = new List<MOBPNRSeat>();
                            foreach (UAWSFlightReservation.Seat seat in segment.Seats)
                            {
                                //PNRSeat pnrSeat = new PNRSeat();
                                //pnrSeat.Number = seat.SeatAssignment;
                                //pnrSeat.SeatRow = seat.SeatRow;
                                //pnrSeat.SeatLetter = seat.SeatLetter;
                                //pnrSeat.PassengerSHARESPosition = seat.TravelerSharesIndex;
                                //pnrSegment.Seats.Add(pnrSeat);
                                for (int i = 0; i < pnrSeatList.Count; i++)
                                {
                                    if (pnrSeatList[i].PassengerSHARESPosition.ToUpper().Trim() == seat.TravelerSharesIndex.ToUpper().Trim())
                                    {
                                        pnrSeatList[i].Number = seat.SeatAssignment;
                                        pnrSeatList[i].SeatRow = seat.SeatRow;
                                        pnrSeatList[i].SeatLetter = seat.SeatLetter;

                                        pnrSeatList[i].SeatStatus = seat.SeatStatus;
                                        pnrSeatList[i].SegmentIndex = segment.Index.ToString();
                                        pnrSeatList[i].Origin = pnrSegment.Departure.Code;
                                        pnrSeatList[i].Destination = pnrSegment.Arrival.Code;
                                        pnrSeatList[i].EddNumber = seat.EddNumber;
                                        pnrSeatList[i].EDocId = seat.EDocId;
                                        double price = 0.0;
                                        bool ok = Double.TryParse(seat.Price, out price);
                                        if (ok)
                                        {
                                            pnrSeatList[i].Price = price;
                                        }
                                        pnrSeatList[i].Currency = seat.Currency;
                                        pnrSeatList[i].ProgramCode = seat.ProgramCode;
                                    }
                                }
                            }
                            pnrSegment.Seats = pnrSeatList;
                        }
                        else
                        {
                            pnrSegment.Seats = new List<MOBPNRSeat>();
                        }

                        if (segment.StopInfos != null)
                        {
                            pnrSegment.NumberOfStops = segment.Stops.ToString();

                            pnrSegment.Stops = new List<MOBPNRSegment>();
                            foreach (UAWSFlightReservation.Segment stopSegment in segment.StopInfos)
                            {
                                #region
                                MOBPNRSegment ss = new MOBPNRSegment();

                                ss.ActionCode = stopSegment.ActionCode;
                                ss.ActualMileage = stopSegment.ActualMileage.ToString();
                                ss.Arrival = new MOBAirport();
                                ss.Arrival.Code = stopSegment.Destination;
                                //ss.Arrival.Name = stopSegment.DecodedDestination;
                                string airportName1 = string.Empty;
                                string cityName1 = string.Empty;
                                tupleRes = await _airportDynamoDB.GetAirportCityName(stopSegment.Destination, _headers.ContextValues.SessionId, airportName1, cityName1);
                                airportName1 = tupleRes.airportName;
                                cityName1 = tupleRes.cityName;
                                ss.Arrival.Name = airportName1;
                                ss.Arrival.City = cityName1;

                                ss.Aircraft = new MOBAircraft();
                                ss.Aircraft.Code = stopSegment.Equipment;
                                ss.Aircraft.LongName = stopSegment.EquipmentDescription;
                                //ss.Arrival = new Airport();
                                //ss.Arrival.Code = stopSegment.Destination;
                                //ss.Arrival.Name = stopSegment.DecodedDestination;
                                ss.ClassOfService = stopSegment.ServiceClass;
                                ss.ClassOfServiceDescription = segment.ServiceClassDescription;
                                ss.Departure = new MOBAirport();
                                ss.Departure.Code = stopSegment.Origin;
                                //ss.Departure.Name = stopSegment.DecodedOrigin;
                                airportName1 = string.Empty;
                                cityName1 = string.Empty;
                                tupleRes = await _airportDynamoDB.GetAirportCityName(stopSegment.Origin, _headers.ContextValues.SessionId, airportName1, cityName1);
                                airportName1 = tupleRes.airportName;
                                cityName1 = tupleRes.cityName;
                                ss.Departure.Name = airportName1;
                                ss.Departure.City = cityName1;

                                ss.EMP = string.Format("{0}%", segment.EMP);

                                if (string.IsNullOrEmpty(stopSegment.FlightNumber))
                                {
                                    stopSegment.FlightNumber = segment.FlightNumber;
                                }


                                ss.FlightNumber = stopSegment.FlightNumber;


                                ss.FlightTime = stopSegment.TravelTime;
                                ss.GroundTime = stopSegment.GroundTime;
                                ss.MarketingCarrier = new MOBAirline();


                                ss.MarketingCarrier.Code = "UA";
                                ss.MarketingCarrier.Name = "United Airlines";

                                ss.MarketingCarrier.FlightNumber = stopSegment.CDFN;

                                ss.CabinType = stopSegment.CabinTypeText;

                                ss.Meal = stopSegment.MealDescription;
                                ss.MileagePlusMileage = stopSegment.OnePassMileage.ToString();
                                ss.OperationoperatingCarrier = new MOBAirline();


                                ss.OperationoperatingCarrier.Code = stopSegment.OperatingCarrier;
                                ss.OperationoperatingCarrier.FlightNumber = stopSegment.OFN;

                                try
                                {
                                    if (ss.OperationoperatingCarrier.Code != null && ss.OperationoperatingCarrier.Code.Equals("UA"))
                                    {
                                        if (stopSegment.Dei != null && !string.IsNullOrEmpty(stopSegment.Dei.DEI50))
                                        {
                                            string[] dei50 = stopSegment.Dei.DEI50.Split(' ');
                                            if (dei50.Length == 2)
                                            {
                                                ss.OperationoperatingCarrier.FlightNumber = dei50[1];
                                            }
                                        }
                                    }
                                }
                                catch (Exception) { }



                                ss.OperationoperatingCarrier.Name = stopSegment.OperationalDisclosureCarrier;

                                if (ss.OperationoperatingCarrier.Name.ToLower().Equals("continental"))
                                {
                                    ss.OperationoperatingCarrier.Name = "Continental Airlines";
                                }
                                else if (ss.OperationoperatingCarrier.Name.ToLower().Equals("united"))
                                {
                                    ss.OperationoperatingCarrier.Name = "United Airlines";
                                }

                                ss.CodeshareCarrier = new MOBAirline();

                                ss.CodeshareCarrier.Code = stopSegment.CDMC;
                                ss.CodeshareFlightNumber = stopSegment.CDFN;
                                ss.CodeshareCarrier.FlightNumber = stopSegment.CDFN;

                                ss.ScheduledArrivalDateTime = GeneralHelper.FormatDatetime(Convert.ToDateTime(stopSegment.DestinationDate).ToString("yyyyMMdd hh:mm tt"), languageCode);
                                ss.ScheduledDepartureDateTime = GeneralHelper.FormatDatetime(Convert.ToDateTime(stopSegment.DepartDate).ToString("yyyyMMdd hh:mm tt"), languageCode);

                                ss.ScheduledArrivalDateTimeGMT = await _manageResUtility.GetGMTTime(segment.DestinationDate, pnrSegment.Arrival.Code, _headers.ContextValues.SessionId);
                                ss.ScheduledDepartureDateTimeGMT = await _manageResUtility.GetGMTTime(segment.DestinationDate, pnrSegment.Arrival.Code, _headers.ContextValues.SessionId);


                                ss.TotalMileagePlusMileage = stopSegment.TotalOnePassMileage.ToString();
                                ss.TotalTravelTime = stopSegment.TotalTravelTime;

                                if (stopSegment.UpgVis != null)
                                {
                                    ss.UpgradeVisibility = GetUpgradeVisibility(stopSegment.UpgVis, stopSegment, ref ss);
                                    if (ss.UpgradeVisibility != null && ss.UpgradeVisibility.UpgradeStatus == MOBUpgradeEligibilityStatus.Requested && (ss.UpgradeVisibility.UpgradeType == MOBUpgradeType.ComplimentaryPremierUpgrade || ss.UpgradeVisibility.UpgradeType == MOBUpgradeType.GlobalPremierUpgrade || ss.UpgradeVisibility.UpgradeType == MOBUpgradeType.MileagePlusUpgradeAwards || ss.UpgradeVisibility.UpgradeType == MOBUpgradeType.RegionalPremierUpgrade))
                                    {
                                        pnrSegment.UpgradeEligible = true;
                                    }
                                    if (ss.UpgradeVisibility != null && ss.UpgradeVisibility.UpgradeType != MOBUpgradeType.PremierInstantUpgrade)
                                    {
                                        hasUpgradeVisibility = true;
                                    }
                                }

                                if (stopSegment.Seats != null)
                                {
                                    ss.Seats = new List<MOBPNRSeat>();
                                    foreach (UAWSFlightReservation.Seat seat in stopSegment.Seats)
                                    {
                                        MOBPNRSeat pnrSeat = new MOBPNRSeat();
                                        pnrSeat.Number = seat.SeatAssignment;
                                        pnrSeat.SeatRow = seat.SeatRow;
                                        pnrSeat.SeatLetter = seat.SeatLetter;
                                        pnrSeat.PassengerSHARESPosition = seat.TravelerSharesIndex;

                                        pnrSeat.SeatStatus = seat.SeatStatus;
                                        pnrSeat.SegmentIndex = stopSegment.Index.ToString();
                                        pnrSeat.Origin = ss.Departure.Code;
                                        pnrSeat.Destination = ss.Arrival.Code;
                                        pnrSeat.EddNumber = seat.EddNumber;
                                        pnrSeat.EDocId = seat.EDocId;
                                        double price = 0.0;
                                        bool ok = Double.TryParse(seat.Price, out price);
                                        if (ok)
                                        {
                                            pnrSeat.Price = price;
                                        }
                                        pnrSeat.Currency = seat.Currency;
                                        pnrSeat.ProgramCode = seat.ProgramCode;

                                        ss.Seats.Add(pnrSeat);
                                    }
                                }

                                pnrSegment.Stops.Add(ss);
                                #endregion
                            }
                        }

                        pnr.Segments.Add(pnrSegment);
                        #endregion
                    }

                    await SetElfContentOnPnr(pnr, applicationId, appVersion);

                    // FIX on Feb 22,2013 
                    if (_configuration.GetValue<string>("NoSegmentsFoundInPNRErrorMessage") != null && (pnr.Segments == null || pnr.Segments.Count == 0))
                    {
                        throw new MOBUnitedException(_configuration.GetValue<string>("NoSegmentsFoundInPNRErrorMessage"));
                    }
                    // FIX on Feb 22,2013 
                    //Eticketed is true; ticketcomplete is true; printboardingpass is true then checked in. Printboardindpass is false and eticketed and ticketcomplete is true then not checked in 
                    //if (response.Flight.ETicketed && response.Flight.TicketComplete && !response.Flight.PrintBoardingPasses && !response.Flight.CheckIn)
                    //if (response.Flight.ETicketed && response.Flight.TicketComplete && !isSpaceAvailblePassRider)
                    if ((response.Flight.ETicketed || !response.Flight.PaperTicket) && response.Flight.TicketComplete && !isSpaceAvailblePassRider && !response.Flight.NoItin)
                    {
                        if (response.Flight.Segments.Count() == 1 && !response.Flight.PrintBoardingPasses && !response.Flight.CheckIn)
                        {
                            if (Convert.ToDateTime(response.Flight.Segments[0].DepartDate).AddHours(12) < DateTime.Now)
                            {
                                pnr.IsEligibleToSeatChange = false;
                            }
                            else
                            {
                                pnr.IsEligibleToSeatChange = true;
                            }
                        }
                        else if (response.Flight.Segments.Count() > 1 && (pastSegmentsCount != pnr.Segments.Count())) // Not to show the change seat option if all the segmetns are flown
                        {
                            if ((pnr.Segments.Count() - pastSegmentsCount) == 1)
                            {
                                pnr.IsEligibleToSeatChange = CheckShowChangeSeat(response.Flight.Segments);
                            }
                            else
                            {
                                pnr.IsEligibleToSeatChange = true;
                            }
                        }
                        if (response.Flight.Segments.Count() == 1 && response.Flight.Segments[0].CarrierCode != "UA")
                        {
                            pnr.IsEligibleToSeatChange = false;
                        }
                    }

                    if (Convert.ToBoolean(_configuration.GetValue<string>("ReshopPricingInformationToggledOn")))
                    {
                        //TODO - Ayantika Sen
                        //pnr.TripType = new TripTypeMapper(response.Flight.Trips).Map();
                    }

                    #endregion
                }
                else
                {
                    throw new MOBUnitedException("We are unable to retrieve the latest information for this itinerary.");
                }
            }
            else
            {
                throw new MOBUnitedException(response.Errors[0].Message);
            }

            if (!forWallet)
            {
                if (!isPositiveSpace && !isAllFirstCabin && pnr.IsEligibleToSeatChange && _configuration.GetValue<bool>(("ShowSeatChange")))
                {
                    //pnr.SeatOffer = GetSeatOffer(pnr.RecordLocator, pnr, applicationId, appVersion);
                }

                pnr.Segments = ProcessWaitlistUpgradeVisibility(pnr, ref hasUpgradeVisibility, lowestEliteLevel, applicationId, hasGSPax, has1KPax);

                if (!hasUpgradeVisibility)
                {
                    pnr.Segments = CheckUpgradeEligibility(pnr.SessionId, lowestEliteLevel, pnr.Segments, hasGSPax, has1KPax); //We can also pass 0 for elite level, so it will be handled on .com side.  For booking pass, we need to pass whoever is signed in to purchase the ticket.
                }
            }

            if (!firstSegmentLifted)
            {
                if (pnr != null && pnr.Segments != null)
                {
                    bool makeMP2015Call = false;
                    foreach (var segment in pnr.Segments)
                    {
                        if (Convert.ToDateTime(segment.ScheduledDepartureDateTime) >= Convert.ToDateTime(_configuration.GetValue<string>("MP2015StartDate")) && _manageResUtility.IsVersionEnableMP2015LMXCallOrFareLock(appVersion, _configuration.GetValue<string>("EnableMP2015LMXCallForVersions")))
                        {
                            makeMP2015Call = true;
                            break;
                        }
                    }

                    if (makeMP2015Call && !isPsSaTravel)
                    {
                        List<MOBLmxFlight> lmxFlights = null;
                        try
                        {
                            lmxFlights = await GetLmxFlights(applicationId, transactionId, transactionId, recordLocator);
                        }
                        catch (Exception)
                        {
                            pnr.SupressLMX = true;
                        }

                        bool bypassMPPassengerCheck = false;
                        if (lmxFlights != null && lmxFlights.Count > 0)
                        {
                            Dictionary<string, bool> segmentsAdded = new Dictionary<string, bool>();
                            foreach (var segment in pnr.Segments)
                            {
                                foreach (var lmxFlight in lmxFlights)
                                {
                                    //if (segment.MarketingCarrier.Code.Equals(lmxFlight.MarketingCarrier.Code)
                                    //    && segment.FlightNumber.Equals(lmxFlight.FlightNumber)
                                    //    && segment.Departure.Code.Equals(lmxFlight.Departure.Code)
                                    //    && segment.Arrival.Code.Equals(lmxFlight.Arrival.Code)
                                    //    && Convert.ToDateTime(segment.ScheduledDepartureDateTime).ToString("yyyyMMdd").Equals(Convert.ToDateTime(lmxFlight.ScheduledDepartureDateTime).ToString("yyyyMMdd")))
                                    if (segment.FlightNumber.Equals(lmxFlight.FlightNumber)
                                        && segment.Departure.Code.Equals(lmxFlight.Departure.Code)
                                        && segment.Arrival.Code.Equals(lmxFlight.Arrival.Code)
                                        && Convert.ToDateTime(segment.ScheduledDepartureDateTime).ToString("yyyyMMdd").Equals(Convert.ToDateTime(lmxFlight.ScheduledDepartureDateTime).ToString("yyyyMMdd")))
                                    {
                                        string key = string.Format("{0}{1}{2}{3}", lmxFlight.FlightNumber, segment.Departure.Code, segment.Arrival.Code, Convert.ToDateTime(segment.ScheduledDepartureDateTime).ToString("yyyyMMdd"));
                                        if (!segmentsAdded.ContainsKey(key))
                                        {
                                            segment.NonPartnerFlight = true;

                                            #region Fix WI 281982
                                            //[Niveditha]Fix for 281982:ViewRes - GetPnrByRecordLoacator() Object Reference Exception - Schedule Change--Niveditha
                                            bool enableNullReferenceFix = string.IsNullOrEmpty(_configuration.GetValue<string>("EnableNullReferenceFixForGetPNRByRecordLocator")) ? false :
                                                                                Convert.ToBoolean(_configuration.GetValue<string>("EnableNullReferenceFixForGetPNRByRecordLocator"));

                                            if (enableNullReferenceFix)
                                            {

                                                bool lmxQuoteAvailable = false;
                                                if (lmxFlight.Products != null && lmxFlight.Products.Count > 0)
                                                {
                                                    foreach (var product in lmxFlight.Products)
                                                    {
                                                        if (product.LmxLoyaltyTiers != null && product.LmxLoyaltyTiers.Count > 0)
                                                        {
                                                            foreach (var lmxLoyaltyTier in product.LmxLoyaltyTiers)
                                                            {
                                                                if (lmxLoyaltyTier.LmxQuotes != null && lmxLoyaltyTier.LmxQuotes.Count > 0)
                                                                {
                                                                    lmxQuoteAvailable = true;
                                                                    foreach (var lmxQuote in lmxLoyaltyTier.LmxQuotes)
                                                                    {
                                                                        if (!lmxQuote.Amount.Equals("0"))
                                                                        {
                                                                            segment.NonPartnerFlight = false;
                                                                            bypassMPPassengerCheck = true;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                if (lmxQuoteAvailable)
                                                {
                                                    segmentsAdded.Add(key, true);
                                                    segment.LmxProducts = lmxFlight.Products;
                                                }
                                            }
                                            #endregion Fix WI 281982
                                            else
                                            {
                                                segmentsAdded.Add(key, true);
                                                if (lmxFlight.Products != null && lmxFlight.Products.Count > 0)
                                                {
                                                    foreach (var product in lmxFlight.Products)
                                                    {
                                                        if (product.LmxLoyaltyTiers != null && product.LmxLoyaltyTiers.Count > 0)
                                                        {
                                                            foreach (var lmxLoyaltyTier in product.LmxLoyaltyTiers)
                                                            {
                                                                if (lmxLoyaltyTier.LmxQuotes != null && lmxLoyaltyTier.LmxQuotes.Count > 0)
                                                                {
                                                                    foreach (var lmxQuote in lmxLoyaltyTier.LmxQuotes)
                                                                    {
                                                                        if (!lmxQuote.Amount.Equals("0"))
                                                                        {
                                                                            segment.NonPartnerFlight = false;
                                                                            bypassMPPassengerCheck = true;
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                                segment.LmxProducts = lmxFlight.Products;
                                            }
                                        }
                                        //if (_configuration.GetValue<string>("LMXPartners"].IndexOf(segment.MarketingCarrier.Code) == -1)
                                        //{
                                        //    segment.NonPartnerFlight = true;
                                        //}
                                    }
                                }
                            }

                        }

                        pnr.lmxtravelers = GetLMXTravelersFromFlightsAndTravelers(pnr);

                        if (bypassMPPassengerCheck)
                        {
                            pnr.SupressLMX = false;
                        }
                        else
                        {
                            bool hasMPInPassengers = false;
                            if (pnr.Passengers != null && pnr.Passengers.Count > 0)
                            {
                                foreach (var passenger in pnr.Passengers)
                                {
                                    if (passenger.MileagePlus != null)
                                    {
                                        hasMPInPassengers = true;
                                        break;
                                    }
                                }
                            }

                            if (hasMPInPassengers)
                            {
                                pnr.SupressLMX = true;
                            }
                            else
                            {
                                pnr.SupressLMX = false;
                            }
                        }
                    }
                    else
                    {
                        //wade - adding bool to disable LMX at client
                        pnr.SupressLMX = !makeMP2015Call;
                        if (isPsSaTravel)
                        {
                            pnr.SupressLMX = true;
                        }
                    }
                }
            }
            else
            {
                pnr.SupressLMX = true;
            }

            //if (this.levelSwitch.TraceError)
            //{
            //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBPNR>(transactionId, "GetPNRByRecordLocator_response", "Response", pnr));//Common Login Code
            //}

            if (pnr != null && pnr.Passengers != null && pnr.Passengers.Count > 0)
            {
                StringBuilder passengerLastNames = new StringBuilder();
                foreach (var item in pnr.Passengers)
                {
                    passengerLastNames.Append("|");
                    passengerLastNames.Append(item.PassengerName);
                    passengerLastNames.Append("|");
                }
                pnr.HasCheckedBags = await _manageResUtility.HasCheckedBags(pnr.RecordLocator, passengerLastNames.ToString());
            }

            return pnr;
        }
        private MOBFutureFlightCredit UpdateFFCTable(MOBFutureFlightCredit ffc, int aId, string aVersion, MOBItem originalTrip, MOBItem changeFee, bool inlcudeOrWaivedInChangeFee, string ticketValidityDate)
        {

            if (ffc != null && ffc.Messages != null && ffc.Messages.Count > 0 &&
                _configuration.GetValue<bool>("IsFFCCreditValidityInfoEnabled") &&
                GeneralHelper.IsApplicationVersionGreaterorEqual(aId, aVersion,
                                     _configuration.GetValue<string>("FFCCreditTable_ValidityInfo_Supported_AppVersion_Android"),
                                     _configuration.GetValue<string>("FFCCreditTable_ValidityInfo_Supported_AppVersion_iOS")))
            {
                foreach (var m in ffc.Messages.ToList())
                {
                    if (m.Id == "FFC_Credit_Original_Trip")
                    {
                        if (originalTrip != null)
                        {
                            string currencyCode = originalTrip.Id;
                            double.TryParse(originalTrip.CurrentValue, out double amount);
                            string formatedAmount = _manageResUtility.GetCurrencyAmount(amount, currencyCode, 2);

                            m.Id = m.CurrentValue;
                            m.CurrentValue = formatedAmount;
                        }
                        else
                            ffc.Messages.Remove(m);
                    }
                    else if (m.Id == "FFC_Credit_Change_Fee")
                    {
                        if (changeFee != null)
                        {
                            string currencyCode = changeFee.Id;
                            double.TryParse(changeFee.CurrentValue, out double amount);
                            string formatedAmount = _manageResUtility.GetCurrencyAmount(amount, currencyCode, 2);


                            formatedAmount = inlcudeOrWaivedInChangeFee ?
                                               string.Format("{0} {1}", formatedAmount, _configuration.GetValue<string>("FFCTableChangeFee_OrWaived")) :
                                               formatedAmount;
                            m.Id = m.CurrentValue;
                            m.CurrentValue = formatedAmount;
                        }
                        else if (_configuration.GetValue<bool>("EnableShowDefaultTextIfChangeFeeMissing"))
                        {
                            string changeFeeText = inlcudeOrWaivedInChangeFee ? "Per fare rules or waived" : "Per fare rules";
                            m.Id = "Change fee";
                            m.CurrentValue = changeFeeText;
                        }
                        else
                            ffc.Messages.Remove(m);
                    }
                    else if (m.Id == "FFC_Validity_Info")
                    {
                        if (ticketValidityDate != string.Empty)
                        {
                            string validTill = ticketValidityDate.ToDateTime().ToString("MMMM d, yyyy");
                            m.CurrentValue = string.Format(m.CurrentValue, validTill);
                        }
                        else
                        {
                            ffc.Messages.Remove(m);
                        }
                    }
                }
            }
            return ffc;
        }
        private MOBReservationPrice AddUnformatedPriceInfo(string description, string currencyShortCode, string currencyCode, string amount)
        {
            double.TryParse(amount, out double itemAmount);
            MOBReservationPrice item = new MOBReservationPrice();
            item.CurrencyShortCode = currencyShortCode;
            item.CurrencyCode = currencyCode;
            item.Amount = itemAmount;
            item.PriceTypeDescription = description;
            return item;
        }
        private MOBFutureFlightCredit RemoveFFCVisibilityCaptions(MOBFutureFlightCredit ffc, int aId, string aVersion)
        {

            if (ffc != null && ffc.Messages != null && ffc.Messages.Count > 0 &&
                _configuration.GetValue<bool>("IsFFCCreditValidityInfoEnabled") &&
                GeneralHelper.IsApplicationVersionGreaterorEqual(aId, aVersion,
                                    _configuration.GetValue<string>("FFCCreditTable_ValidityInfo_Supported_AppVersion_Android"),
                                    _configuration.GetValue<string>("FFCCreditTable_ValidityInfo_Supported_AppVersion_iOS")))
            {
                foreach (var m in ffc.Messages.ToList())
                {
                    if (m.Id == "Original trip" ||
                        m.Id == "Change fee" ||
                        m.Id == "FFC_Validity_Info" ||
                        m.Id == "FFC_Credit_Title" ||
                        m.Id == "FFC_Policy_Info")
                    {
                        ffc.Messages.Remove(m);
                    }
                }
            }
            return ffc;
        }
        private void CheckForConcur(ReservationDetail response, MOBPNR pnr)
        {
            if (!string.IsNullOrEmpty(GetCharactersticValue(response.Detail.Characteristic, "IsValidCorporateTravel"))
                 && Convert.ToBoolean(GetCharactersticValue(response.Detail.Characteristic, "IsValidCorporateTravel"))
                 && !string.IsNullOrEmpty(GetCharactersticValue(response.Detail.Characteristic, "CorporateTravelVendorName")))
            // && (GetCharactersticValue(response.Detail.Characteristic, "CorporateTravelVendorName")).Equals("Concur", StringComparison.InvariantCultureIgnoreCase))
            {
                // pnr.SyncedWithConcur = "Synced with " + GetCharactersticValue(response.Detail.Characteristic, "CorporateTravelVendorName");
                string str = GetCharactersticValue(response.Detail.Characteristic, "CorporateTravelVendorName");
                pnr.SyncedWithConcur = "Synced with " + str.First().ToString().ToUpper() + str.Substring(1).ToLower();
            }

        }
        private void GetTheFareLock(string appVersion, ReservationDetail response, MOBPNR pnr)
        {
            if (_manageResUtility.IsVersionEnableMP2015LMXCallOrFareLock(appVersion,
                _configuration.GetValue<string>("AppVersionsForFareLock")))
            {
                string farelockExpirationDate = string.Empty;

                farelockExpirationDate = getFareLockDate(response.Detail.Characteristic);

                pnr.FarelockExpirationDate = farelockExpirationDate;
            }
        }
        private string getFareLockDate(Collection<Characteristic> Characteristic)
        {
            string farelockExpirationDate = string.Empty;
            if (Characteristic != null && Characteristic.Count > 0)
            {
                var cslFareLockDate = GetCharactersticValue(Characteristic, "FARELOCK_DATE");

                if (!string.IsNullOrWhiteSpace(cslFareLockDate))
                {
                    // Commenting below time conversion code as CSL gives time in UTC.
                    //DateTime localTime = Convert.ToDateTime(cslFareLockDate);
                    //TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
                    //DateTime utcTime = TimeZoneInfo.ConvertTimeToUtc(localTime, timeZoneInfo);
                    //farelockExpirationDate = utcTime.ToString("M/d/yyyy hh:mm:ss tt");

                    farelockExpirationDate = cslFareLockDate;
                }
            }
            return farelockExpirationDate;
        }
        private string GetFareLockPurchase(Reservation Detail)
        {
            if (Detail == null || Detail.Characteristic == null)
                return string.Empty;

            string farelockExpirationDate = string.Empty;
            farelockExpirationDate = getFareLockDate(Detail.Characteristic);
            return !string.IsNullOrEmpty(farelockExpirationDate) ? string.Format(_configuration.GetValue<string>("FareLockMessage"), _manageResUtility.fareLockDate(Detail.CreateDate, farelockExpirationDate)) : string.Empty;
        }
        private string GetFareLockButton(Collection<Characteristic> characteristic)
        {
            if (characteristic == null || !characteristic.Any())
                return string.Empty;

            var mileage = characteristic.FirstOrDefault(o => (o.Description != null && o.Description.Equals("ITINTotalForMileage", StringComparison.OrdinalIgnoreCase)));
            var total = characteristic.FirstOrDefault(o => (o.Description != null && o.Description.Equals("ITINTotalForCurrency", StringComparison.OrdinalIgnoreCase)));
            var formatedMiles = mileage != null && Convert.ToInt32(mileage.Value) > 0 ? ShopStaticUtility.FormatAwardAmountForDisplay(mileage.Value, true) : string.Empty;
            var formatedPrice = total != null && Convert.ToDouble(total.Value) > 0 ? TopHelper.FormatAmountForDisplay(total.Value, TopHelper.GetCultureInfo(total.Code), false) : string.Empty;

            if (!formatedMiles.IsNullOrEmpty() && !formatedPrice.IsNullOrEmpty())
                return formatedMiles + " + " + formatedPrice;

            if (!formatedMiles.IsNullOrEmpty())
                return formatedMiles;

            if (!formatedPrice.IsNullOrEmpty())
                return formatedPrice;

            return string.Empty;
        }
        private bool IsEligibleForCompleteFareLockPurchase(Reservation Detail)
        {
            if (Detail != null && Detail.Characteristic != null && Detail.PointOfSale != null)
            {
                return _manageResUtility.TicketingCountryCode(Detail.PointOfSale).Equals("US") && _manageResUtility.IsRevenue(Detail.Type) && !HasAnyTravelerChargesWithAncilary(Detail.Travelers);
            }
            return false;
        }
        private bool HasAnyTravelerChargesWithAncilary(Collection<Traveler> travelers)
        {
            if (!travelers.IsNullOrEmpty())
            {
                bool isFarelockAncillary = false;
                foreach (Traveler p in travelers)
                {
                    if (p != null && p.Person != null && p.Person.Charges != null && p.Person.Charges.Any() && !isFarelockAncillary)
                    {
                        isFarelockAncillary = IsFareLockAncillaryCharge(p.Person.Charges);
                    }
                }
                return isFarelockAncillary;
            }
            return true;
        }
        private bool IsFareLockAncillaryCharge(Collection<Service.Presentation.CommonModel.Charge> pnrTravelerCharges)
        {
            return pnrTravelerCharges.Any(p => p != null && !p.Type.IsNullOrEmpty() && !(p.Type.Equals("FLK") || p.Type.Equals("Total Charges")));
        }
        private async System.Threading.Tasks.Task GetOARecordLocator(MOBPNR pnr, ReservationDetail response)
        {
            pnr.OARecordLocators = new List<MOBOARecordLocator>();
            foreach (var oaRecordLocator in response.Detail.Characteristic)
            {
                MOBOARecordLocator locator = new MOBOARecordLocator();

                if (oaRecordLocator != null && oaRecordLocator.Description != null &&
                    oaRecordLocator.Description.Equals("OARecordLocator", StringComparison.InvariantCultureIgnoreCase))
                {
                    locator.RecordLocator = oaRecordLocator.Value;
                    locator.CarrierName = await _airportDynamoDB.GetCarrierInfo(oaRecordLocator.Code, _headers.ContextValues.SessionId);

                    if (_configuration.GetValue<bool>("BugFixEnableCarrierNameForJSX") && oaRecordLocator.Code.Equals("XE"))
                    {
                        locator.CarrierName = _configuration.GetValue<string>("JXSConfirmation");
                    }

                    if (!oaRecordLocator.Code.Equals(locator.CarrierName) && !locator.CarrierCode.Equals("UA"))
                    {
                        pnr.OARecordLocators.Add(locator);
                    }
                }
            }
        }
        public int GetLowestEliteLevel(int applicationId, string appVersion, ReservationDetail response, MOBPNR pnr,
           int lowestEliteLevel, ref bool isPsSaTravel, ref bool hasGSPax, ref bool has1KPax)
        {
            if (response.Detail.Travelers != null)
            {
                pnr.NumberOfPassengers = response.Detail.Travelers.Count.ToString();

                if (response.Detail.Travelers.Count > 0)
                {
                    //lowestEliteLevel = response.Flight.Travelers[0].EliteLevel;
                    if (response.Detail.Travelers[0].LoyaltyProgramProfile != null)
                    {
                        lowestEliteLevel = Convert.ToInt16(response.Detail.Travelers[0].LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription);
                    }
                    foreach (var traveler in response.Detail.Travelers)
                    {
                        if (traveler.Tickets != null)
                        {
                            foreach (var ticket in traveler.Tickets)
                            {
                                if (!pnr.PsSaTravel && _manageResUtility.CheckEmpPassengerIndicator(ticket))
                                {
                                    isPsSaTravel = true;
                                }

                                if (!string.IsNullOrEmpty(_configuration.GetValue<string>("EnableEResBooking")) &&
                                    Convert.ToBoolean(_configuration.GetValue<string>("EnableEResBooking")))
                                {
                                    if (!pnr.PsSaTravel && _manageResUtility.CheckEmpPassengerIndicator(ticket))
                                    {
                                        // Removing the storeProcedure call due to : MOBILE - 1517-ViewRes NONREV -Remove eRes beta app version check
                                        if (_configuration.GetValue<bool>("RemoveeResBetaAppVersionCheck"))
                                        {
                                            pnr.PsSaTravel = true;
                                        }
                                        else
                                        {
                                            if (IsEResBetaTesterRecordLocator(pnr.RecordLocator, applicationId, appVersion))
                                            {
                                                pnr.PsSaTravel = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (traveler != null && traveler.LoyaltyProgramProfile != null)
                        {
                            int eliteLevelOfTraveler = Convert.ToInt16(traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription);

                            if (eliteLevelOfTraveler < lowestEliteLevel)
                            {
                                lowestEliteLevel = eliteLevelOfTraveler;
                            }

                            if (eliteLevelOfTraveler == 5)
                            {
                                hasGSPax = true;
                            }

                            if (eliteLevelOfTraveler == 4)
                            {
                                has1KPax = true;
                            }
                        }
                    }
                }
            }
            return lowestEliteLevel;
        }
        private bool IsEResBetaTesterRecordLocator(string recordLocator, int applicationId, string applicationVersion)
        {
            bool isEResBetaTesterRecordLocator = false;

            //try
            //{
            //    Database database = DatabaseFactory.CreateDatabase("ConnectionString - iPhone");
            //    DbCommand dbCommand = (DbCommand)database.GetStoredProcCommand("uasp_Select_IsEResBetaTesterRecordLocator");
            //    database.AddInParameter(dbCommand, "@ApplicationId", DbType.Int32, applicationId);
            //    database.AddInParameter(dbCommand, "@applicationVersion", DbType.String, applicationVersion);
            //    database.AddInParameter(dbCommand, "@RecordLocator", DbType.String, recordLocator);

            //    using (IDataReader dataReader = database.ExecuteReader(dbCommand))
            //    {
            //        while (dataReader.Read())
            //        {
            //            if (!dataReader["IsEResBetaTesterRecordLocator"].Equals(DBNull.Value) && Convert.ToInt32(dataReader["IsEResBetaTesterRecordLocator"]) > 0)
            //            {
            //                isEResBetaTesterRecordLocator = true;
            //            }
            //        }
            //    }
            //}
            //catch (System.Exception ex) { Trace.WriteLine(ex.Message); }

            return isEResBetaTesterRecordLocator;
        }
        private async System.Threading.Tasks.Task GetTrips(MOBPNR pnr, ReservationDetail response)
        {
            pnr.Trips = new List<MOBTrip>();


            //int totaltrips = response.Detail.FlightSegments.Select(o => o.TripNumber).Distinct().Count();
            if (response.Detail.FlightSegments != null && response.Detail.FlightSegments.Count > 0)
            {
                int mintripnumber = Convert.ToInt32(response.Detail.FlightSegments.Select(o => o.TripNumber).First());
                int maxtripnumber = Convert.ToInt32(response.Detail.FlightSegments.Select(o => o.TripNumber).Last());

                for (int i = mintripnumber; i <= maxtripnumber; i++)
                {
                    MOBTrip pnrTrip = new MOBTrip();
                    IEnumerable<ReservationFlightSegment> totalTripSegments;
                    if (_configuration.GetValue<bool>("SkipWaitlistedSegmentsInTrips"))
                    {
                        totalTripSegments = response.Detail.FlightSegments.Where(o => o.TripNumber == i.ToString()
                                                                                      && o.Characteristic != null
                                                                                      && GetCharactersticValue(o.Characteristic, "Waitlisted") == "False"
                                                                                  );
                        if (totalTripSegments == null || totalTripSegments.Count() == 0)
                            continue;
                    }
                    else
                    {
                        totalTripSegments = response.Detail.FlightSegments.Where(o => o.TripNumber == i.ToString());
                    }
                    pnrTrip.Index = i;
                    foreach (United.Service.Presentation.SegmentModel.ReservationFlightSegment segment in response.Detail.FlightSegments)
                    {
                        if (!string.IsNullOrEmpty(segment.TripNumber) && Convert.ToInt32(segment.TripNumber) == i)
                        {
                            string airportName = string.Empty;
                            string cityName = string.Empty;

                            if (segment.SegmentNumber == totalTripSegments.Min(x => x.SegmentNumber))
                            {
                                pnrTrip.Origin = segment.FlightSegment.DepartureAirport.IATACode;
                                var tupleRes = await _airportDynamoDB.GetAirportCityName(pnrTrip.Origin, _headers.ContextValues.SessionId, airportName, cityName);
                                airportName = tupleRes.airportName;
                                cityName = tupleRes.cityName;
                                pnrTrip.OriginName = airportName;
                                DateTime departureTime;
                                if (DateTime.TryParse(segment.FlightSegment.DepartureDateTime, out departureTime))
                                {
                                    pnrTrip.DepartureTime = departureTime.ToString("MM/dd/yyyy hh:mm tt");
                                }
                            }
                            if (segment.SegmentNumber == totalTripSegments.Max(x => x.SegmentNumber))
                            {
                                pnrTrip.Destination = segment.FlightSegment.ArrivalAirport.IATACode;
                                var tupleRes = await _airportDynamoDB.GetAirportCityName(pnrTrip.Destination, _headers.ContextValues.SessionId, airportName, cityName);
                                airportName = tupleRes.airportName;
                                cityName = tupleRes.cityName;
                                pnrTrip.DestinationName = airportName;
                                DateTime arrivalTime;
                                if (DateTime.TryParse(segment.FlightSegment.ArrivalDateTime, out arrivalTime))
                                {
                                    pnrTrip.ArrivalTime = arrivalTime.ToString("MM/dd/yyyy hh:mm tt");
                                }
                            }

                        }
                    }
                    pnr.Trips.Add(pnrTrip);
                }
            }

            if (pnr.Trips.Count > 0)
            {
                pnr.LastTripDateDepartureDate = pnr.Trips[pnr.Trips.Count - 1].DepartureTime;
                pnr.LastTripDateArrivalDate = pnr.Trips[pnr.Trips.Count - 1].ArrivalTime;
            }

        }
        public void GetPassengerDetails
         (MOBPNR pnr, ReservationDetail response, ref bool isSpaceAvailblePassRider, ref bool isPositiveSpace, int applicationId = 0, string appVersion = "")
        {
            pnr.Passengers = new List<MOBPNRPassenger>();
            pnr.InfantInLaps = new List<MOBPNRPassenger>();
            // for bug 191187 to get adult keys
            var infantShareIndexList = response.Detail.Travelers.Where(t => t.Person.InfantIndicator != "True").Select(t => t.Person.Key);


            foreach (United.Service.Presentation.ReservationModel.Traveler traveler in response.Detail.Travelers)
            {
                #region Get passengers in the PNR                

                if (traveler != null && traveler.Person != null && !string.IsNullOrEmpty(traveler.Person.InfantIndicator) && !Convert.ToBoolean(traveler.Person.InfantIndicator) && traveler.Person.Type != null && !traveler.Person.Type.Equals("INF", StringComparison.OrdinalIgnoreCase))
                {
                    MOBPNRPassenger pnrPassenger = new MOBPNRPassenger();
                    pnrPassenger.PassengerName = new MOBName();

                    pnrPassenger.SharesGivenName = ExtractCustomerName(traveler.Person.CustomerID);
                    pnrPassenger.PNRCustomerID = traveler.Person.CustomerID;
                    pnrPassenger.PassengerName.First = traveler.Person.GivenName;
                    pnrPassenger.PassengerName.Middle = traveler.Person.MiddleName;
                    pnrPassenger.PassengerName.Last = traveler.Person.Surname;
                    pnrPassenger.PassengerName.Suffix = traveler.Person.Suffix;
                    pnrPassenger.PassengerName.Title = traveler.Person.Title;
                    pnrPassenger.LoyaltyProgramProfile = traveler.LoyaltyProgramProfile;

                    if (GeneralHelper.isApplicationVersionGreater(applicationId, appVersion, "iPhonePNRTravelerContactVersion", "AndroidPNRTravelerContactVersion", "", "", true, _configuration))
                    {
                        pnrPassenger.Contact = GetTravelerContact(traveler);
                    }

                    GetTravelerSecureFlightInfo(traveler.Person.Documents, traveler.Characteristics, ref pnrPassenger);
                    string SSRDisplaySequence = string.Empty;
                    pnrPassenger.SelectedSpecialNeeds = new List<MOBTravelSpecialNeed>();

                    if (GeneralHelper.isApplicationVersionGreater
                    (applicationId, appVersion, "AndroidEnableMgnResUpdateSpecialNeeds", "iPhoneEnableMgnResUpdateSpecialNeeds", string.Empty, string.Empty, true, _configuration))
                        pnrPassenger.SelectedSpecialNeeds = GetSelectedSpecialNeeds(traveler.Characteristics);
                    else
                        pnrPassenger.SelectedSpecialNeeds = null;

                    if (GeneralHelper.isApplicationVersionGreater(applicationId, appVersion, "iPhoneInfantInLapVersion", "AndroidInfantInLapVersion", "", "", true, _configuration))
                    {
                        pnrPassenger.BirthDate = traveler.Person.DateOfBirth;
                        pnrPassenger.TravelerTypeCode = traveler.Person.Type;
                    }

                    pnrPassenger.SHARESPosition = traveler.Person.Key;
                    pnrPassenger.SSRDisplaySequence = SSRDisplaySequence;

                    GetRewardProgramDetails(traveler, pnrPassenger);
                    GetEmployeeProfileDeails(traveler, pnrPassenger);
                    pnr.Passengers.Add(pnrPassenger);

                    isSpaceAvailblePassRider = IsSpaceAvailblePassRider(traveler, isSpaceAvailblePassRider, ref isPositiveSpace);
                }
                // for bug 191187 to get seat for infant who has booked the ticket
                else if (traveler != null && traveler.Person != null && !string.IsNullOrEmpty(traveler.Person.Key) && !infantShareIndexList.Contains(traveler.Person.Key))
                {
                    MOBPNRPassenger pnrPassenger = new MOBPNRPassenger();
                    pnrPassenger.PassengerName = new MOBName();

                    pnrPassenger.SharesGivenName = ExtractCustomerName(traveler.Person.CustomerID);
                    pnrPassenger.PNRCustomerID = traveler.Person.CustomerID;
                    pnrPassenger.PassengerName.First = traveler.Person.GivenName;
                    pnrPassenger.PassengerName.Middle = traveler.Person.MiddleName;
                    pnrPassenger.PassengerName.Last = traveler.Person.Surname;
                    pnrPassenger.PassengerName.Suffix = traveler.Person.Suffix;
                    pnrPassenger.PassengerName.Title = traveler.Person.Title;

                    if (GeneralHelper.isApplicationVersionGreater(applicationId, appVersion, "iPhonePNRTravelerContactVersion", "AndroidPNRTravelerContactVersion", "", "", true, _configuration))
                    {
                        pnrPassenger.Contact = GetTravelerContact(traveler);
                    }

                    GetTravelerSecureFlightInfo(traveler.Person.Documents, traveler.Characteristics, ref pnrPassenger);
                    string SSRDisplaySequence = string.Empty;
                    pnrPassenger.SelectedSpecialNeeds = new List<MOBTravelSpecialNeed>();

                    if (GeneralHelper.isApplicationVersionGreater
                    (applicationId, appVersion, "AndroidEnableMgnResUpdateSpecialNeeds", "iPhoneEnableMgnResUpdateSpecialNeeds", string.Empty, string.Empty, true, _configuration))
                        pnrPassenger.SelectedSpecialNeeds = GetSelectedSpecialNeeds(traveler.Characteristics);
                    else
                        pnrPassenger.SelectedSpecialNeeds = null;

                    if (GeneralHelper.isApplicationVersionGreater(applicationId, appVersion, "iPhoneInfantInLapVersion", "AndroidInfantInLapVersion", "", "", true, _configuration))
                    {
                        pnrPassenger.BirthDate = traveler.Person.DateOfBirth;
                        pnrPassenger.TravelerTypeCode = traveler.Person.Type;
                    }

                    pnrPassenger.SSRDisplaySequence = SSRDisplaySequence;
                    pnrPassenger.SHARESPosition = traveler.Person.Key;

                    GetRewardProgramDetails(traveler, pnrPassenger);
                    GetEmployeeProfileDeails(traveler, pnrPassenger);

                    pnr.Passengers.Add(pnrPassenger);

                    isSpaceAvailblePassRider = IsSpaceAvailblePassRider(traveler, isSpaceAvailblePassRider, ref isPositiveSpace);
                }
                else
                {
                    if (GeneralHelper.isApplicationVersionGreater(applicationId, appVersion, "iPhoneInfantInLapVersion", "AndroidInfantInLapVersion", "", "", true, _configuration))
                    {
                        pnr.InfantInLaps.Add(GetLapInfantDetails(traveler));
                    }
                }

                #endregion
            }
        }


        private MOBPNRPassenger GetLapInfantDetails(United.Service.Presentation.ReservationModel.Traveler traveler)
        {
            MOBPNRPassenger pnrPassenger = new MOBPNRPassenger();

            if (traveler != null && traveler.Person != null)
            {
                pnrPassenger.PassengerName = new MOBName();
                pnrPassenger.SharesGivenName = ExtractCustomerName(traveler.Person.CustomerID);
                pnrPassenger.PNRCustomerID = traveler.Person.CustomerID;
                pnrPassenger.PassengerName.First = traveler.Person.GivenName;
                pnrPassenger.PassengerName.Middle = traveler.Person.MiddleName;
                pnrPassenger.PassengerName.Last = traveler.Person.Surname;
                pnrPassenger.PassengerName.Suffix = traveler.Person.Suffix;
                pnrPassenger.PassengerName.Title = traveler.Person.Title;
                pnrPassenger.SHARESPosition = traveler.Person.Key;
                pnrPassenger.BirthDate = traveler.Person.DateOfBirth;
                pnrPassenger.TravelerTypeCode = traveler.Person.Type;
            }
            return pnrPassenger;
        }



        public bool IsSpaceAvailblePassRider(Traveler traveler, bool isSpaceAvailblePassRider, ref bool isPositiveSpace)
        {
            if (traveler.Tickets != null)
            {
                foreach (var ticket in traveler.Tickets)
                {
                    if (!isSpaceAvailblePassRider &&
                        !string.IsNullOrEmpty(GetCharactersticValue(ticket.Characteristic, "PassengerIndicator"))
                        && (GetCharactersticValue(ticket.Characteristic, "PassengerIndicator").ToUpper() == "SA"))
                    {
                        isSpaceAvailblePassRider = true;
                    }

                    else if (string.IsNullOrEmpty(GetCharactersticValue(ticket.Characteristic, "PassengerIndicator"))
                             && traveler.Tickets != null && ticket.PromotionCode != null &&
                             ticket.PromotionCode.ToUpper().Trim() != "")
                    // This check is for Jump Set booking  the traveler.PassengerIndicator is returning NULL.
                    {
                        isSpaceAvailblePassRider = true;
                    }
                    else if (!isPositiveSpace &&
                             !string.IsNullOrEmpty(GetCharactersticValue(ticket.Characteristic, "PassengerIndicator"))
                             && (GetCharactersticValue(ticket.Characteristic, "PassengerIndicator").ToUpper() == "PS"))
                    {
                        isPositiveSpace = true;
                    }
                }
            }
            return isSpaceAvailblePassRider;
        }


        private void GetEmployeeProfileDeails(Traveler traveler, MOBPNRPassenger mobPnrPax)
        {
            if (_configuration.GetValue<bool>("IncludeEmpProfileDeatils_GetPNRByRecordLocatorResponse") &&
                traveler != null &&
                traveler.EmployeeProfile != null &&
                mobPnrPax != null)
            {
                mobPnrPax.EmployeeProfile = new MOBPNREmployeeProfile
                {
                    EmployeeID = traveler.EmployeeProfile.EmployeeID,
                    PassClassification = traveler.EmployeeProfile.PassClassification,
                    CompanySeniorityDate = traveler.EmployeeProfile.CompanySeniorityDate
                };
            }
        }

        public void GetRewardProgramDetails(Traveler traveler, MOBPNRPassenger pnrPassenger)
        {
            if (traveler.LoyaltyProgramProfile != null)
            {
                //foreach (var rewardProgram in traveler.LoyaltyProgramProfile)
                //{
                if (!string.IsNullOrEmpty(traveler.LoyaltyProgramProfile.LoyaltyProgramCarrierCode) &&
                    traveler.LoyaltyProgramProfile.LoyaltyProgramCarrierCode.Equals("UA"))
                {
                    MOBEliteStatus eliteStatus = new MOBEliteStatus(_configuration);
                    //eliteStatus.Code = traveler.LoyaltyProgramProfile.LoyaltyProgramID;
                    eliteStatus.Code = ((int)traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription).ToString();
                    pnrPassenger.MileagePlus = new MOBCPMileagePlus();
                    pnrPassenger.MileagePlus.MileagePlusId = traveler.LoyaltyProgramProfile.LoyaltyProgramMemberID;
                    pnrPassenger.MileagePlus.CurrentEliteLevel = Convert.ToInt16(traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription);
                    pnrPassenger.MileagePlus.CurrentEliteLevelDescription = eliteStatus.Description;
                    pnrPassenger.MileagePlus.VendorCode = traveler.LoyaltyProgramProfile.LoyaltyProgramCarrierCode;
                }
                else
                {
                    string vendorName = GetStarRewardVendorName(traveler.LoyaltyProgramProfile.LoyaltyProgramCarrierCode);
                    if (!string.IsNullOrEmpty(vendorName))
                    {
                        if (pnrPassenger.OaRewardPrograms == null)
                        {
                            pnrPassenger.OaRewardPrograms = new List<MOBRewardProgram>();
                        }
                        MOBRewardProgram oaRewardProgram = new MOBRewardProgram();
                        //oaRewardProgram.StarEliteStatus = traveler.LoyaltyProgramProfile.StarEliteStatus;
                        oaRewardProgram.EliteLevel = (int)traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription;
                        //oaRewardProgram.ProgramCode = traveler.LoyaltyProgramProfile.LoyaltyProgramCarrierCode; // not used in client
                        oaRewardProgram.ProgramDescription =
                            traveler.LoyaltyProgramProfile.LoyaltyProgramMemberTierDescription.ToString();
                        //oaRewardProgram.ProgramType = traveler.LoyaltyProgramProfile.;
                        //oaRewardProgram.OtherVendorCodeStatus = traveler.LoyaltyProgramProfile.OtherVendorCodeStatus;
                        oaRewardProgram.VendorCode = traveler.LoyaltyProgramProfile.LoyaltyProgramCarrierCode;
                        oaRewardProgram.ProgramMemberId = traveler.LoyaltyProgramProfile.LoyaltyProgramMemberID;
                        oaRewardProgram.VendorDescription = vendorName;
                        pnrPassenger.OaRewardPrograms.Add(oaRewardProgram);
                    }
                }
            }
        }


        public MOBPNRPassenger GetTravelerSecureFlightInfo
            (IEnumerable<United.Service.Presentation.PersonModel.Document> documents,
            IEnumerable<Service.Presentation.CommonModel.Characteristic> characteristics,
            ref MOBPNRPassenger pnrPassenger)
        {
            if (pnrPassenger != null && documents != null && documents.Any())
            {
                //KTN get
                var ktnObj = documents.LastOrDefault(k => string.IsNullOrEmpty(k.KnownTravelerNumber) == false);
                pnrPassenger.KnownTravelerNumber = (ktnObj == null) ? string.Empty : ktnObj.KnownTravelerNumber;
                pnrPassenger.KTNDisplaySequence = (ktnObj != null && ktnObj.Genre != null) ? Convert.ToString(ktnObj.Genre.DisplaySequence) : string.Empty;

                //REDRESS get
                var redressObj = documents.LastOrDefault(R => string.IsNullOrEmpty(R.RedressNumber) == false);
                string redressValue = pnrPassenger.RedressNumber = (redressObj == null) ? string.Empty : redressObj.RedressNumber;
                pnrPassenger.REDRESSDisplaySequence = (redressObj != null && redressObj.Genre != null) ? Convert.ToString(redressObj.Genre.DisplaySequence) : string.Empty;

                var ctnObj = documents.LastOrDefault(R => string.IsNullOrEmpty(R.CanadianTravelNumber) == false);
                string ctnValue = pnrPassenger.CanadianTravelNumber = (ctnObj == null) ? string.Empty : ctnObj.CanadianTravelNumber;
                pnrPassenger.CTNDisplaySequence = (ctnObj != null && ctnObj.Genre != null) ? Convert.ToString(ctnObj.Genre.DisplaySequence) : string.Empty;
            }
            return pnrPassenger;
        }

        public List<MOBTravelSpecialNeed> GetSelectedSpecialNeeds
            (IEnumerable<Service.Presentation.CommonModel.Characteristic> characteristics)
        {
            List<MOBTravelSpecialNeed> selectedspecialneeds;
            if (characteristics != null && characteristics.Any())
            {
                //var ignoreItem = new HashSet<string> { "DOCO", "DOCS", "TKNE", "FQTV", "PASS" };

                //var selectedspecialneedsDisplaySeq = characteristics.Where(x => (!string.IsNullOrEmpty(x.Code) && !ignoreItem.Contains(x.Code)))
                //.Where(y => (y.Genre != null)).GroupBy(z => z.Genre.DisplaySequence).Select(k => k.First())
                //.Select(l => Convert.ToString(l.Genre.DisplaySequence)).ToArray();

                //strselectedspecialneedsDisplaySeq = string.Join("|", selectedspecialneedsDisplaySeq);

                selectedspecialneeds
                    //= characteristics.GroupBy(x => x.Code).Select(y => y.First()).Where(z => !ignoreItem.Contains(z.Code) && !string.IsNullOrEmpty(z.Code)).Select(
                    = characteristics.Where(z => (!string.IsNullOrEmpty(z.Code)))
                    .Select(x => new MOBTravelSpecialNeed()
                    {
                        Code = x.Code,
                        DisplayDescription = x.Description,
                        DisplaySequence = (x.Genre != null) ? Convert.ToString(x.Genre.DisplaySequence) : string.Empty,
                        Value = x.Value
                    }).ToList();

                return selectedspecialneeds;
            }
            return null;
        }

        private static string ExtractCustomerName(string customerId)
        {
            if (!string.IsNullOrEmpty(customerId))
            {
                string[] strCustIdArray = customerId.Split('/');
                if (strCustIdArray.Length == 2)
                    return strCustIdArray[1];
            }
            return string.Empty;
        }

        private MOBContact GetTravelerContact(Traveler traveler)
        {
            MOBContact mcontact = new MOBContact();
            try
            {
                if (traveler != null && traveler.Person != null && traveler.Person.Contact != null)
                {
                    if (traveler.Person.Contact.Emails != null && traveler.Person.Contact.Emails.Any())
                    {
                        mcontact.Emails = new List<MOBEmail>();
                        traveler.Person.Contact.Emails.ToList().ForEach(item =>
                        {
                            string emailDisplaySequence = string.Empty;

                            if (!string.IsNullOrEmpty(item.Address))
                            {
                                if (item?.Genre?.DisplaySequence != null)
                                {
                                    emailDisplaySequence = Convert.ToString(item?.Genre?.DisplaySequence);
                                }
                                //string emailDisplaySequence = string.Empty;
                                //string[] emailitems = item.Address.Split('@');
                                //if (emailitems.Length == 2 && !string.IsNullOrEmpty(emailitems[0]))
                                //{
                                //    emailDisplaySequence = GetContactDisplaySequence(traveler.Characteristics, emailitems[0]);
                                //}
                                mcontact.Emails.Add(new MOBEmail { EmailAddress = item.Address, Key = emailDisplaySequence });
                            }
                        });
                    }

                    if (traveler.Person.Contact.PhoneNumbers != null && traveler.Person.Contact.PhoneNumbers.Any())
                    {
                        mcontact.PhoneNumbers = new List<MOBCPPhone>();
                        traveler.Person.Contact.PhoneNumbers.ToList().ForEach(item =>
                        {
                            var phoneDisplaySequence = string.Empty;
                            var countryphonenumber = string.Empty;
                            var phonenumber = string.Empty;

                            if (!string.IsNullOrEmpty(item.PhoneNumber))
                            {
                                if (item?.Genre?.DisplaySequence != null)
                                {
                                    phoneDisplaySequence = Convert.ToString(item?.Genre?.DisplaySequence);
                                }
                                //phoneDisplaySequence = GetContactDisplaySequence(traveler.Characteristics, item.PhoneNumber);

                                countryphonenumber = GetCountryCode(item.CountryAccessCode);
                                phonenumber = ExcludeCountryCodeFrmPhoneNumber(item.PhoneNumber, countryphonenumber);
                                mcontact.PhoneNumbers.Add(new MOBCPPhone
                                { PhoneNumber = phonenumber, CountryCode = item.CountryAccessCode, CountryPhoneNumber = countryphonenumber, Key = phoneDisplaySequence });
                            }
                        });
                    }
                }
                return mcontact;
            }
            catch
            { return mcontact; }
        }


        public string ExcludeCountryCodeFrmPhoneNumber(string phonenumber, string countrycode)
        {
            try
            {
                Int64 _phonenumber;
                if (!string.IsNullOrEmpty(phonenumber)) phonenumber = phonenumber.Replace(" ", "");
                if (Int64.TryParse(phonenumber, out _phonenumber))
                {
                    if (!string.IsNullOrEmpty(countrycode))
                    {
                        var phonenumbercountrycode = phonenumber.Substring(0, countrycode.Length);
                        if (string.Equals(countrycode, phonenumbercountrycode, StringComparison.OrdinalIgnoreCase))
                        {
                            return phonenumber.Remove(0, countrycode.Length);
                        }
                    }
                }
            }
            catch
            { return string.Empty; }
            return phonenumber;
        }


        public string GetCountryCode(string countryaccesscode)
        {
            var countrycode = string.Empty;
            try
            {
                var _countries = LoadCountries();
                if (_countries != null && _countries.Any())
                {
                    countrycode = _countries.FirstOrDefault(x => (x.Length == 3 && string.Equals
                    (countryaccesscode, x[0], StringComparison.OrdinalIgnoreCase)))[2];
                    countrycode = countrycode.Replace(" ", "");
                }
            }
            catch { return countrycode; }

            return countrycode;
        }

        public List<string[]> LoadCountries()
        {
            PATH_COUNTRIES_XML = _configuration.GetValue<string>("GetCountriesXMlPath") != null ? _configuration.GetValue<string>("GetCountriesXMlPath") : "";

            if (File.Exists(PATH_COUNTRIES_XML))
            {
                XElement data = XElement.Load(PATH_COUNTRIES_XML);
                Countries = data.Elements("COUNTRY").Select(a =>
                                           new string[]
                                           {
                                                a.Attribute("CODE").Value,
                                                a.Attribute("NAME").Value,
                                                a.Attribute("ACCESSCODE").Value,
                                           }).ToList();
            }
            return Countries;
        }

        private bool GetTPIBoughtInfo(Collection<Traveler> travelers)
        {
            bool isTPIIncluded = false;
            if (Convert.ToBoolean(_configuration.GetValue<string>("ShowTripInsuranceViewResSwitch") ?? "false") && travelers != null && travelers.Count > 0)
            {
                foreach (Traveler traveler in travelers)
                {
                    if (traveler != null && traveler.Person != null && traveler.Person.Charges != null && traveler.Person.Charges.Count > 0)
                    {
                        foreach (var charge in traveler.Person.Charges)
                        {
                            if (charge != null && !string.IsNullOrEmpty(charge.Type) && charge.Type.ToUpper().Trim() == "TRIP INS")
                            {
                                isTPIIncluded = true;
                                break;
                            }
                        }
                    }
                }
            }
            return isTPIIncluded;
        }
        private void GetPetsDetails(MOBPNR pnr, ReservationDetail response)
        {
            pnr.PetRecordLocators = new List<string>();
            if (response.Detail.Pets != null)
                foreach (United.Service.Presentation.ReservationModel.PetTraveler petInfo in response.Detail.Pets)
                {
                    if (!string.IsNullOrEmpty(petInfo.ConfirmationID))
                    {
                        pnr.PetRecordLocators.Add(petInfo.ConfirmationID.Trim());
                    }
                }
        }
        private bool SegmentPast(ReservationDetail response, ReservationFlightSegment segment)
        {
            bool segmentPast = false;
            if (response != null && response.Detail != null && response.Detail.Prices != null && response.Detail.Prices.Count > 0 && response.Detail.Prices[0].PriceFlightSegments != null)
            {

                foreach (United.Service.Presentation.SegmentModel.PriceFlightSegment pSegment in response.Detail.Prices[0].PriceFlightSegments)
                {
                    if (pSegment.SegmentNumber == segment.SegmentNumber && pSegment.FlightStatuses != null && pSegment.FlightStatuses.Count > 0)
                    {
                        // Madhavi as per CSL "Past" will be true if FlightStatuses code is UA USED, As of now FlightStatuses is null in  CSL response.
                        if (pSegment != null && pSegment.FlightStatuses[0].Code != null && pSegment.FlightStatuses[0].Code.ToUpper() == "UA USED")
                        {
                            segmentPast = true;
                        }
                    }
                }
            }
            return segmentPast;
        }
        private List<MOBPNRSeat> GetPnrSeatList(ReservationDetail response)
        {
            List<MOBPNRSeat> pnrSeatList = new List<MOBPNRSeat>();
            if (response != null && response.Detail != null && response.Detail.Travelers != null)
            {
                foreach (United.Service.Presentation.ReservationModel.Traveler traveler in response.Detail.Travelers)
                {
                    MOBPNRSeat pnrSeat = new MOBPNRSeat();
                    // for bug 191187 to get adult keys
                    var infantShareIndexList = response.Detail.Travelers.Where(t => t.Person.InfantIndicator != "True").Select(t => t.Person.Key);
                    if (traveler.Person != null && !string.IsNullOrEmpty(traveler.Person.InfantIndicator) && !Convert.ToBoolean(traveler.Person.InfantIndicator) && traveler.Person.Type != null && !traveler.Person.Type.Equals("INF", StringComparison.OrdinalIgnoreCase))
                    {
                        pnrSeat.PassengerSHARESPosition = traveler.Person.Key;
                        pnrSeatList.Add(pnrSeat);
                    }
                    // for bug 191187 to get seat for infant who has booked the ticket

                    else if (traveler.Person != null && !infantShareIndexList.Contains(traveler.Person.Key))
                    {
                        pnrSeat.PassengerSHARESPosition = traveler.Person.Key;
                        pnrSeatList.Add(pnrSeat);
                    }

                }
            }
            return pnrSeatList;
        }
        public async Task<MOBPNRSegment> GetPnrSegment(string languageCode, string appVersion, int applicationId, ReservationFlightSegment segment, int lowestEliteLevel)
        {
            MOBPNRSegment pnrSegment = new MOBPNRSegment();
            if (!string.IsNullOrEmpty(GetCharactersticValue(segment.Characteristic, "Waitlisted"))
                && Convert.ToBoolean(GetCharactersticValue(segment.Characteristic, "Waitlisted")))
            {
                pnrSegment.Waitlisted = true;
            }

            //need Trip/Segment Numbers for club notifications - Sphurthi
            pnrSegment.TripNumber = segment.TripNumber;
            pnrSegment.SegmentNumber = segment.SegmentNumber;

            pnrSegment.ActionCode = segment.FlightSegment.FlightSegmentType;
            pnrSegment.LowestEliteLevel = lowestEliteLevel;
            pnrSegment.ActualMileage = segment.FlightSegment.Distance.ToString();

            pnrSegment.Arrival = new MOBAirport();
            pnrSegment.Arrival.Code = segment.FlightSegment.ArrivalAirport.IATACode;
            //pnrSegment.Arrival.Name = segment.DecodedDestination;
            string airportName = string.Empty;
            string cityName = string.Empty;

            var tupleRes = await _airportDynamoDB.GetAirportCityName(segment.FlightSegment.ArrivalAirport.IATACode, _headers.ContextValues.SessionId, airportName, cityName);
            airportName = tupleRes.airportName;
            cityName = tupleRes.cityName;
            pnrSegment.Arrival.Name = airportName;
            pnrSegment.Arrival.City = cityName;

            pnrSegment.Aircraft = new MOBAircraft();
            pnrSegment.Aircraft.Code = segment.FlightSegment.Equipment != null ? segment.FlightSegment.Equipment.Model.Fleet : "";
            pnrSegment.Aircraft.LongName = segment.FlightSegment.Equipment != null ? segment.FlightSegment.Equipment.Model.Description : "";
            pnrSegment.Aircraft.ModelCode = segment.FlightSegment.Equipment != null ? segment.FlightSegment.Equipment.Model?.Key : string.Empty;
            //pnrSegment.Arrival = new Airport();
            //pnrSegment.Arrival.Code = segment.Destination;
            //pnrSegment.Arrival.Name = segment.DecodedDestination;
            pnrSegment.ClassOfService = segment.BookingClass != null ? segment.BookingClass.Code : "";
            //pnrSegment.ClassOfServiceDescription = segment.BookingClass.Cabin.Name;

            //kirti per csl response.Detail.FlightSegments[0].BookingClass.Cabin.Name 
            pnrSegment.ClassOfServiceDescription = segment.BookingClass != null ? segment.BookingClass.Cabin?.Name : "";
            pnrSegment.Departure = new MOBAirport();
            pnrSegment.Departure.Code = segment.FlightSegment.DepartureAirport.IATACode;
            //pnrSegment.Departure.Name = segment.DecodedOrigin;
            airportName = string.Empty;
            cityName = string.Empty;

            tupleRes = await _airportDynamoDB.GetAirportCityName(segment.FlightSegment.DepartureAirport.IATACode, _headers.ContextValues.SessionId, airportName, cityName);
            airportName = tupleRes.airportName;
            cityName = tupleRes.cityName;
            pnrSegment.Departure.Name = airportName;
            pnrSegment.Departure.City = cityName;

            //Client dont need this property
            //pnrSegment.EMP = string.Format("{0}%", segment.EMP);

            pnrSegment.FlightNumber = segment.FlightSegment.FlightNumber;

            GetTravelTime(appVersion, segment, pnrSegment);

            pnrSegment.GroundTime = segment.FlightSegment.GroundTime.ToString();
            pnrSegment.MarketingCarrier = new MOBAirline();

            pnrSegment.MarketingCarrier.Code = segment.FlightSegment.MarketedFlightSegment.Select(a => a.MarketingAirlineCode).First();
            pnrSegment.MarketingCarrier.Name = segment.FlightSegment.OperatingAirlineName;

            if (pnrSegment.MarketingCarrier.Name.ToLower().Equals("united"))
            {
                pnrSegment.MarketingCarrier.Name = "United Airlines";
            }

            pnrSegment.MarketingCarrier.FlightNumber = pnrSegment.FlightNumber;

            pnrSegment.CabinType = segment.BookingClass != null ? segment.BookingClass.Cabin?.Name : "";

            //kirti per csl Characteristic.Code="MealDescription"
            pnrSegment.Meal = GetCharactersticValue(segment.Characteristic, "MealDescription");
            pnrSegment.MileagePlusMileage = segment.FlightSegment.Distance.ToString();

            pnrSegment.OperationoperatingCarrier = new MOBAirline();
            pnrSegment.OperationoperatingCarrier.Code = segment.FlightSegment.OperatingAirlineCode;
            pnrSegment.OperationoperatingCarrier.FlightNumber = segment.FlightSegment.FlightNumber;

            if (pnrSegment.OperationoperatingCarrier.Code != null &&
                pnrSegment.OperationoperatingCarrier.Code.Equals("UA"))
            {
                pnrSegment.OperationoperatingCarrier.FlightNumber =
                    GetCharactersticValue(segment.Characteristic, "DEI50");
            }

            pnrSegment.OperationoperatingCarrier.Name = segment.FlightSegment.OperatingAirlineName;
            if (pnrSegment.OperationoperatingCarrier.Name.ToLower().Equals("united"))
            {
                pnrSegment.OperationoperatingCarrier.Name = "United Airlines";
            }

            pnrSegment.CodeshareCarrier = new MOBAirline();
            pnrSegment.CodeshareCarrier.Code = segment.FlightSegment.OperatingAirlineCode;
            pnrSegment.CodeshareCarrier.FlightNumber = segment.FlightSegment.FlightNumber;


            pnrSegment.ScheduledArrivalDateTime = GeneralHelper.FormatDatetime(Convert.ToDateTime(segment.FlightSegment.ArrivalDateTime).ToString("yyyyMMdd hh:mm tt"), languageCode);
            pnrSegment.ScheduledDepartureDateTime = GeneralHelper.FormatDatetime(Convert.ToDateTime(segment.FlightSegment.DepartureDateTime).ToString("yyyyMMdd hh:mm tt"), languageCode);

            //Added by Madhavi on 25May for 161705 
            bool isCSLUTC = Convert.ToBoolean(_configuration.GetValue<string>("SwithToCSLUTCTime") ?? "false");
            if (isCSLUTC)
            {
                pnrSegment.ScheduledArrivalDateTimeGMT = GeneralHelper.FormatDatetime(Convert.ToDateTime(segment.FlightSegment.ArrivalUTCDateTime).ToString("yyyyMMdd hh:mm tt"), languageCode);
                pnrSegment.ScheduledDepartureDateTimeGMT = GeneralHelper.FormatDatetime(Convert.ToDateTime(segment.FlightSegment.DepartureUTCDateTime).ToString("yyyyMMdd hh:mm tt"), languageCode);
            }
            else
            {
                pnrSegment.ScheduledArrivalDateTimeGMT = await _manageResUtility.GetGMTTime(segment.FlightSegment.ArrivalDateTime, pnrSegment.Arrival.Code, _headers.ContextValues.SessionId);
                pnrSegment.ScheduledDepartureDateTimeGMT = await _manageResUtility.GetGMTTime(segment.FlightSegment.ArrivalDateTime, pnrSegment.Arrival.Code, _headers.ContextValues.SessionId);
            }

            pnrSegment.TotalTravelTime = GetCharactersticValue(segment.Characteristic, "Journey Duration");
            pnrSegment.TripNumber = segment.TripNumber;
            pnrSegment.SegmentNumber = segment.SegmentNumber;
            pnrSegment.ProductCode = GetCharactersticValue(segment.FlightSegment.Characteristic, "ProductCode");
            pnrSegment.IsIBE = _manageResUtility.IsIBEFullFare(pnrSegment.ProductCode);
            pnrSegment.ShowSeatMapLink = GetSeatLinkEligibility(segment);
            if (_configuration.GetValue<bool>("EnableCSL30ManageResSelectSeatMap"))
            {
                pnrSegment.IsChangeOfGuage = !string.IsNullOrEmpty(segment.FlightSegment.IsChangeOfGauge) ? Convert.ToBoolean(segment.FlightSegment.IsChangeOfGauge) : false;
            }

            //CTN Birkan
            pnrSegment.IsCanadaSegment = CheckIfCanadaTravel(segment, applicationId, appVersion);

            //TRH Birkan
            pnrSegment.UflifoFlightStatus = GetUflifoFlightStatus(segment, applicationId, appVersion);

            return pnrSegment;
        }
        public string GetUflifoFlightStatus(ReservationFlightSegment segment, int appId, string appVersion)
        {

            //TRH LANDED LIFTED Birkan
            try
            {
                if (segment?.Characteristic != null) // && (flightStatus == "DepartedGateLateTaxiingRunway" || flightStatus == "ArrivedGateEarly")
                {
                    string flightStatus = _manageResUtility.GetCharacteristicValue(segment?.Characteristic?.ToList(), "uflifo-FlightStatus");
                    if (!string.IsNullOrEmpty(flightStatus))
                    {
                        if (flightStatus.IndexOf("Arrived", StringComparison.OrdinalIgnoreCase) > -1 || flightStatus.IndexOf("Landed", StringComparison.OrdinalIgnoreCase) > -1) return "LANDED";
                        if (flightStatus.IndexOf("Departed", StringComparison.OrdinalIgnoreCase) > -1 || flightStatus.IndexOf("InFlight", StringComparison.OrdinalIgnoreCase) > -1) return "LIFTED";
                    }
                }
            }
            catch { }
            return string.Empty;

        }

        public bool CheckIfCanadaTravel(ReservationFlightSegment segment, int appId, string appVersion)
        {

            //CTN Birkan
            try
            {
                if (_manageResUtility.IsEnableCanadianTravelNumber(appId, appVersion))
                {
                    if (segment?.FlightSegment != null
                        && (String.Equals(segment.FlightSegment.ArrivalAirport?.IATACountryCode?.CountryCode, "CA", StringComparison.OrdinalIgnoreCase)
                        || String.Equals(segment.FlightSegment.DepartureAirport?.IATACountryCode?.CountryCode, "CA", StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }

                }
            }
            catch { }
            return false;

        }
        private void GetTravelTime(string appVersion, ReservationFlightSegment segment, MOBPNRSegment pnrSegment)
        {
            if (segment.FlightSegment.JourneyDuration.Hours > 0)
            {
                if (!string.IsNullOrEmpty(appVersion) && appVersion.ToUpper().Equals("2.1.8I"))
                {
                    pnrSegment.FlightTime = "0 HR " + segment.FlightSegment.JourneyDuration.Hours;
                }
                else if (segment.FlightSegment.JourneyDuration.Hours > 0) // Madhavi added this condition not to dispaly 0H 
                {
                    pnrSegment.FlightTime = segment.FlightSegment.JourneyDuration.Hours + " HR";
                }
            }
            if (segment.FlightSegment.JourneyDuration.Minutes > 0)
            {
                if (!string.IsNullOrEmpty(appVersion) && appVersion.ToUpper().Equals("2.1.8I"))
                {
                    pnrSegment.FlightTime = pnrSegment.FlightTime + " " + segment.FlightSegment.JourneyDuration.Minutes +
                                            " 0 MN";
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(pnrSegment.FlightTime))
                    {
                        pnrSegment.FlightTime = segment.FlightSegment.JourneyDuration.Minutes + " MN";
                    }
                    else
                    {
                        pnrSegment.FlightTime = pnrSegment.FlightTime + " " + segment.FlightSegment.JourneyDuration.Minutes + " MN";
                    }

                }
            }
        }
        private bool GetSeatLinkEligibility(ReservationFlightSegment segment)
        {
            bool showLink = false;

            if (_configuration.GetValue<bool>("IsSeatNumberClickableEnabled"))
            {
                string marketedFlightCode = string.Empty;
                if (segment.FlightSegment != null && segment.FlightSegment.MarketedFlightSegment != null && segment.FlightSegment.MarketedFlightSegment.Any())
                {
                    marketedFlightCode = segment.FlightSegment.MarketedFlightSegment[0].MarketingAirlineCode;
                }

                bool isUsed = false;
                if (segment.FlightSegment.FlightStatuses != null)
                {
                    foreach (var status in segment.FlightSegment.FlightStatuses)
                    {

                        // as per CSL check both the condition for used flights
                        if ((status != null && status.Code != null && status.Code.Contains("USED")) ||
                            ((status != null && status.StatusType != null && status.StatusType.Contains("USED")) && (status.Code != null && status.Code.Contains("CHECKED-IN"))))
                        {
                            isUsed = true;
                            break;
                        }
                    }
                }

                if ((ShowSeatMapForCarriers(segment.FlightSegment.OperatingAirlineCode) || _manageResUtility.IsSeatMapSupportedOa(segment.FlightSegment.OperatingAirlineCode, marketedFlightCode))
                    && !(isUsed) && !IsInChecKInWindow(segment.FlightSegment.DepartureUTCDateTime))
                {
                    showLink = !ShopStaticUtility.CheckForCheckinEligible(segment);
                }
            }
            return showLink;
        }
        public bool ShowSeatMapForCarriers(string operatingCarrier)
        {
            if (_configuration.GetValue<string>("ShowSeatMapAirlines") != null)
            {
                string[] carriers = _configuration.GetValue<string>("ShowSeatMapAirlines").Split(',');
                foreach (string carrier in carriers)
                {
                    if (operatingCarrier != null && carrier.ToUpper().Trim().Equals(operatingCarrier.ToUpper().Trim()))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public bool IsInChecKInWindow(string departTimeString)
        {
            DateTime departDateTime;
            DateTime.TryParse(departTimeString, out departDateTime);
            return departDateTime > DateTime.UtcNow && departDateTime < DateTime.UtcNow.AddHours(24);
        }

        public MOBSegmentResponse GetUpgradeVisibilityCSLWithWaitlist
          (Service.Presentation.SegmentModel.FlightSegment upgradeSegment, ref MOBPNRSegment pnrSegment, Collection<Price> prices = null)
        {
            MOBSegmentResponse upgradeVisibility = new MOBSegmentResponse();
            upgradeVisibility.CarrierCode = upgradeSegment.MarketedFlightSegment[0].MarketingAirlineCode;
            upgradeVisibility.ClassOfService = upgradeSegment.BookingClasses[0].Code;
            upgradeVisibility.DepartureDateTime = Convert.ToDateTime(upgradeSegment.DepartureDateTime).ToString("MM/dd/yyyy hh:mm tt");
            upgradeVisibility.Destination = upgradeSegment.ArrivalAirport.IATACode;
            upgradeVisibility.FlightNumber = Convert.ToInt32(upgradeSegment.FlightNumber);
            upgradeVisibility.Origin = upgradeSegment.DepartureAirport.IATACode;
            upgradeVisibility.PreviousSegmentActionCode = upgradeSegment.PreviousSegmentActionCode;
            upgradeVisibility.SegmentActionCode = upgradeSegment.FlightSegmentType;
            upgradeVisibility.SegmentNumber = upgradeSegment.SegmentNumber;
            upgradeVisibility.UpgradeMessageCode = GetUpgradeMessageCode(upgradeSegment.UpgradeMessageCode);
            upgradeVisibility.UpgradeMessage = upgradeSegment.UpgradeMessage;
            upgradeVisibility.UpgradeType = GetUpgradeTypeCSL(upgradeSegment.UpgradeVisibilityType);
            if (upgradeVisibility.UpgradeType == MOBUpgradeType.PremierInstantUpgrade)
            {
                pnrSegment.Upgradeable = true;
                pnrSegment.UpgradeableMessageCode = "FE";
            }

            int tripnumber;
            int.TryParse(pnrSegment.TripNumber, out tripnumber);
            upgradeVisibility.DecodedUpgradeMessage
                    = GetDecodedUpgradeMessageCSL(upgradeSegment.UpgradeMessageCode,
                    upgradeSegment.UpgradeMessage, upgradeSegment.UpgradeProperties,
                    upgradeSegment, prices, tripnumber, pnrSegment);


            if (upgradeSegment.UpgradeProperties != null && upgradeSegment.UpgradeProperties.Count > 0)
            {
                upgradeVisibility.UpgradeProperties = new List<MOBUpgradePropertyKeyValue>();
                upgradeVisibility.UpgradeProperties = GetUpgradePropertiesCSL(upgradeSegment.UpgradeProperties);
            }
            upgradeVisibility.UpgradeStatus = GetUpgradeStatusCSL(upgradeSegment.UpgradeEligibilityStatus);
            return upgradeVisibility;
        }
        private List<MOBUpgradePropertyKeyValue> GetUpgradePropertiesCSL(System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.Characteristic> upgradePropertiesKeyValue)
        {
            List<MOBUpgradePropertyKeyValue> upgradeProperties = null;
            if (upgradePropertiesKeyValue != null && upgradePropertiesKeyValue.Count > 0)
            {
                upgradeProperties = new List<MOBUpgradePropertyKeyValue>();
                foreach (var keyValue in upgradePropertiesKeyValue)
                {
                    MOBUpgradePropertyKeyValue upgradePropertyKeyValue = new MOBUpgradePropertyKeyValue();
                    upgradePropertyKeyValue.Key = GetUpgradePropertyKey(keyValue.Code);
                    upgradePropertyKeyValue.Value = keyValue.Value;
                    upgradeProperties.Add(upgradePropertyKeyValue);
                }
            }

            return upgradeProperties;
        }
        private MOBUpgradeProperty GetUpgradePropertyKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return MOBUpgradeProperty.None;
            }
            else
            {
                switch (key)
                {
                    case "None":
                        return MOBUpgradeProperty.None;
                        break;
                    case "UpgradeClassOfService":
                        return MOBUpgradeProperty.UpgradeClassOfService;
                        break;
                    case "UpgradePremierLevel":
                        return MOBUpgradeProperty.UpgradePremierLevel;
                        break;
                    case "UpgradeWindowHours":
                        return MOBUpgradeProperty.UpgradeWindowHours;
                        break;
                }
            }

            return MOBUpgradeProperty.None;
        }
        private MOBMessageCode GetUpgradeMessageCode(string messageCode)
        {
            if (string.IsNullOrEmpty(messageCode))
            {
                return MOBMessageCode.None;
            }
            else
            {
                switch (messageCode)
                {
                    case "ConfirmedUpgrade":
                        return MOBMessageCode.ConfirmedUpgrade;
                        break;
                    case "InstantUpgrade":
                        return MOBMessageCode.InstantUpgrade;
                        break;
                    case "None":
                        return MOBMessageCode.None;
                        break;
                    case "SpaceAvailableBeforeDepartureGoldAndHigher":
                        return MOBMessageCode.SpaceAvailableBeforeDepartureGoldAndHigher;
                        break;
                    case "SpaceAvailableBeforeDepartureSilver":
                        return MOBMessageCode.SpaceAvailableBeforeDepartureSilver;
                        break;
                    case "SpaceAvailableCpuToCabinOutsideWindowGoldAndHigher":
                        return MOBMessageCode.SpaceAvailableCpuToCabinOutsideWindowGoldAndHigher;
                        break;
                    case "SpaceAvailableCpuToCabinOutsideWindowSilver":
                        return MOBMessageCode.SpaceAvailableCpuToCabinOutsideWindowSilver;
                        break;
                    case "SpaceAvailableInsideWindowGoldAndHigher":
                        return MOBMessageCode.SpaceAvailableInsideWindowGoldAndHigher;
                        break;
                    case "SpaceAvailableInsideWindowSilverAndDayOfDeparture":
                        return MOBMessageCode.SpaceAvailableInsideWindowSilverAndDayOfDeparture;
                        break;
                    case "WaitlistClassConfirmed":
                        return MOBMessageCode.WaitlistClassConfirmed;
                        break;
                    case "WaitlistedSameFlightDifferentNonUpgradeClass":
                        return MOBMessageCode.WaitlistedSameFlightDifferentNonUpgradeClass;
                        break;
                    case "WaitlistedSegmentOnAnotherFlight":
                        return MOBMessageCode.WaitlistedSegmentOnAnotherFlight;
                        break;
                    case "WaitlistUpgradeRequested":
                        return MOBMessageCode.WaitlistUpgradeRequested;
                        break;
                }
            }

            return MOBMessageCode.None;
        }
        public MOBUpgradeType GetUpgradeTypeCSL(Service.Presentation.CommonEnumModel.UpgradeVisibilityType upgradeVisibilityType)
        {
            switch (upgradeVisibilityType)
            {
                case Service.Presentation.CommonEnumModel.UpgradeVisibilityType.ComplimentaryPremierUpgrade:
                    return MOBUpgradeType.ComplimentaryPremierUpgrade;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeVisibilityType.GlobalPremierUpgrade:
                    return MOBUpgradeType.GlobalPremierUpgrade;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeVisibilityType.MileagePlusUpgradeAwards:
                    return MOBUpgradeType.MileagePlusUpgradeAwards;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeVisibilityType.None:
                    return MOBUpgradeType.None;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeVisibilityType.PremierCabinUpgrade:
                    return MOBUpgradeType.PremierCabinUpgrade;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeVisibilityType.PremierInstantUpgrade:
                    return MOBUpgradeType.PremierInstantUpgrade;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeVisibilityType.RegionalPremierUpgrade:
                    return MOBUpgradeType.RegionalPremierUpgrade;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeVisibilityType.RevenueUpgradeStandby:
                    return MOBUpgradeType.RevenueUpgradeStandby;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeVisibilityType.Unknown:
                    return MOBUpgradeType.Unknown;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeVisibilityType.Waitlisted:
                    return MOBUpgradeType.Waitlisted;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeVisibilityType.PlusPointsUpgrade:
                    return MOBUpgradeType.PlusPointsUpgrade;
                default:
                    return MOBUpgradeType.Unknown;
                    break;
            }
        }
        public string GetDecodedUpgradeMessageCSL
           (string messageCode, string message, System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.Characteristic>
           upgradePropertiesKeyValue, Service.Presentation.SegmentModel.FlightSegment upgradeSegment, Collection<Price> prices = null,
           int tripnumber = 0, MOBPNRSegment pnrSegment = null)
        {
            string decodedMessage = string.Empty;

            switch (messageCode)
            {
                case "ConfirmedUpgrade":
                    if (_configuration.GetValue<bool>("EnableUpgradePlusPointToolTipMessage") && prices != null
                        && prices.Any(x => (x.FareType == FareType.Confirmable)
                        && (x.PriceFlightSegments != null)
                        && (x.PriceFlightSegments.Any(y => ((y.SegmentNumber == upgradeSegment.SegmentNumber) && (y.LOFNumber == tripnumber))))))
                    {
                        decodedMessage = "Upgrade - Skip Waitlist";
                        pnrSegment.IsPlusPointSegment = true;
                    }
                    else if (_configuration.GetValue<bool>("EnableUpgradePlusPointToolTipMessage")
                        && upgradeSegment.UpgradeVisibilityType == UpgradeVisibilityType.PlusPointsUpgrade
                        && upgradeSegment.UpgradeEligibilityStatus == Service.Presentation.CommonEnumModel.UpgradeEligibilityStatus.Upgraded)
                    {
                        decodedMessage = "Upgrade - Confirmed";
                        pnrSegment.IsPlusPointSegment = true;
                    }
                    else
                    {
                        decodedMessage = _configuration.GetValue<string>("WUPMessageConfirmedUpgrade");
                        var waitlist = GetCharactersticValue(upgradeSegment.Characteristic, "Waitlisted");
                        if (Convert.ToBoolean(string.IsNullOrWhiteSpace(waitlist) ? "false" : waitlist) &&
                            upgradeSegment.FlightSegmentType.Equals("KL", StringComparison.CurrentCultureIgnoreCase))
                        {
                            decodedMessage = _configuration.GetValue<string>("WUPMessageConfirmedUpgrade");
                        }
                    }
                    break;
                case "InstantUpgrade":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageInstantUpgrade");
                    break;
                case "None":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageNone");
                    break;
                case "SpaceAvailableBeforeDepartureGoldAndHigher":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageSpaceAvailableBeforeDepartureGoldAndHigher");
                    break;
                case "SpaceAvailableBeforeDepartureSilver":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageSpaceAvailableBeforeDepartureSilver");
                    break;
                case "SpaceAvailableCpuToCabinOutsideWindowGoldAndHigher":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageSpaceAvailableCpuToCabinOutsideWindowGoldAndHigher");
                    string upgradeWindowsHours = GetUpgradePropertyCSL(MOBUpgradeProperty.UpgradeWindowHours, upgradePropertiesKeyValue);
                    decodedMessage = string.Format(decodedMessage, upgradeWindowsHours);
                    break;
                case "SpaceAvailableCpuToCabinOutsideWindowSilver":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageSpaceAvailableCpuToCabinOutsideWindowSilver");
                    break;
                case "SpaceAvailableInsideWindowGoldAndHigher":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageSpaceAvailableInsideWindowGoldAndHigher");
                    break;
                case "SpaceAvailableInsideWindowSilverAndDayOfDeparture":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageSpaceAvailableInsideWindowSilverAndDayOfDeparture");
                    break;
                case "WaitlistClassConfirmed":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageWaitlistClassConfirmed");
                    break;
                case "WaitlistedSameFlightDifferentNonUpgradeClass":
                    //if (!string.IsNullOrEmpty(message) && message.Equals("SegmentIsWaitlisted"))
                    if (!string.IsNullOrEmpty(message))
                    {
                        decodedMessage = _configuration.GetValue<string>("WUPMessageWaitlistedSameFlightDifferentNonUpgradeClass");
                        string upgradeClassOfService = GetUpgradePropertyCSL(MOBUpgradeProperty.UpgradeClassOfService, upgradePropertiesKeyValue);
                        decodedMessage = string.Format(decodedMessage, upgradeClassOfService);
                    }
                    break;
                case "WaitlistedSegmentOnAnotherFlight":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageWaitlistedSegmentOnAnotherFlight");
                    break;
                case "WaitlistUpgradeRequested": //Changes for the bug 200165 - November 24,j.Srinivas
                    if (!string.IsNullOrEmpty(message) && (upgradeSegment.PreviousSegmentActionCode.Equals("PD") || upgradeSegment.PreviousSegmentActionCode.Equals("PB") || upgradeSegment.PreviousSegmentActionCode.Equals("PC")))
                    {
                        decodedMessage = _configuration.GetValue<string>("WUPMessageWaitlistedSameFlightDifferentNonUpgradeClass");
                        string upgradeClassOfService = GetUpgradePropertyCSL(MOBUpgradeProperty.UpgradeClassOfService, upgradePropertiesKeyValue);
                        decodedMessage = string.Format(decodedMessage, upgradeClassOfService);
                    }
                    else if (_configuration.GetValue<bool>("EnableUpgradePlusPointToolTipMessage") &&
                        upgradeSegment.UpgradeVisibilityType == UpgradeVisibilityType.PlusPointsUpgrade)
                    {
                        //TODO - remove hard coding 
                        bool isSkipped;
                        bool.TryParse(_manageResUtility.GetCharactersticValue(upgradeSegment.UpgradeProperties, "IsSkipped"), out isSkipped);
                        if (isSkipped)
                        {
                            decodedMessage = string.Format
                                ("Upgrade - Skipped ({0})", _manageResUtility.GetCharactersticValue(upgradeSegment.UpgradeProperties, "SkippedReason"));
                            pnrSegment.IsPlusPointSegment = true;
                        }
                        else if (upgradeSegment.UpgradeEligibilityStatus
                            == Service.Presentation.CommonEnumModel.UpgradeEligibilityStatus.Requested)
                        {
                            decodedMessage = "Upgrade - Waitlisted";
                            pnrSegment.IsPlusPointSegment = true;
                        }

                    }
                    else
                    {
                        decodedMessage = _configuration.GetValue<string>("WUPMessageWaitlistUpgradeRequested");
                    }
                    break;
            }

            return decodedMessage;
        }
        private string GetUpgradePropertyCSL(MOBUpgradeProperty upgradeProperty, System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.Characteristic> list)
        {
            string property = string.Empty;

            if (list != null && list.Count > 0)
            {
                foreach (var keyValue in list)
                {
                    if (keyValue.Code.Equals(upgradeProperty.ToString()))
                    {
                        property = keyValue.Value;
                        break;
                    }
                }
            }

            return property;
        }
        private MOBUpgradeEligibilityStatus GetUpgradeStatusCSL(Service.Presentation.CommonEnumModel.UpgradeEligibilityStatus upgradeEligibilityStatus)
        {
            switch (upgradeEligibilityStatus)
            {
                case Service.Presentation.CommonEnumModel.UpgradeEligibilityStatus.NotQualified:
                    return MOBUpgradeEligibilityStatus.NotQualified;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeEligibilityStatus.NotUpgraded:
                    return MOBUpgradeEligibilityStatus.NotUpgraded;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeEligibilityStatus.Qualified:
                    return MOBUpgradeEligibilityStatus.Qualified;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeEligibilityStatus.RequestConfirmed:
                    return MOBUpgradeEligibilityStatus.RequestConfirmed;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeEligibilityStatus.Requested:
                    return MOBUpgradeEligibilityStatus.Requested;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeEligibilityStatus.Unknown:
                    return MOBUpgradeEligibilityStatus.Unknown;
                    break;
                case Service.Presentation.CommonEnumModel.UpgradeEligibilityStatus.Upgraded:
                    return MOBUpgradeEligibilityStatus.Upgraded;
                    break;
                default:
                    return MOBUpgradeEligibilityStatus.Unknown;
                    break;
            }
        }

        public async Task<MOBMPPNRs> GetPNRsByMileagePlusNumberCSL(int applicationId, string transactionId, string mileagePlusNumber, MOBReservationType reservationType, string languageCode, bool includeFarelockInfo, string appVersion, bool forWallet, bool getEmployeeIdFromCSLCustomerData)
        {
            MOBMPPNRs opPNRs = null;

            string requestUrl = string.Empty;
            if (_configuration.GetValue<bool>("GetCheckInStatusFromCSLPNRRetrivalService_Toggle"))
            {
                requestUrl = string.Format(_configuration.GetValue<string>("GetCheckInStatusFromGetReservationsByMPNumberCSL - URL"), mileagePlusNumber);
            }
            //else
            //{
            //    requestUrl = string.Format(_configuration.GetValue<string>("GetReservationsByMPNumberCSL - URL"), mileagePlusNumber);
            //}


            try
            {
                string token = await _dPService.GetAnonymousToken(applicationId, _headers.ContextValues.DeviceId, _configuration).ConfigureAwait(false);

                string jsonResponse = string.Empty;

                if (_configuration.GetValue<string>("WalletCallHttpGetTimeout") != null && _configuration.GetValue<string>("WalletCallHttpGetRetry") != null)
                {
                    int WalletCallHttpGetTimeout = Convert.ToInt32(_configuration.GetValue<string>("WalletCallHttpGetTimeout"));
                    int WalletCallHttpGetRetry = Convert.ToInt32(_configuration.GetValue<string>("WalletCallHttpGetRetry"));
                    jsonResponse = await _reservationService.GetCheckInStatus(token, mileagePlusNumber, _headers.ContextValues.SessionId).ConfigureAwait(false);
                    //HttpHelper.GetWithRetry(requestUrl, "application/json; charset=utf-8", token, WalletCallHttpGetTimeout, WalletCallHttpGetRetry);
                }
                //else
                //{
                //    jsonResponse = HttpHelper.Get(requestUrl, "application/json; charset=utf-8", token);
                //}

                if (!string.IsNullOrEmpty(jsonResponse))
                {

                    List<United.Service.Presentation.ReservationModel.Reservation> response = JsonConvert.DeserializeObject<List<United.Service.Presentation.ReservationModel.Reservation>>(jsonResponse);
                    if (reservationType == MOBReservationType.Current)
                    {
                        if (opPNRs == null)
                        {
                            opPNRs = new MOBMPPNRs();
                        }
                        opPNRs.MileagePlusNumber = mileagePlusNumber;
                        opPNRs.Current = await this.GetPNRsCSL(response, languageCode, true, false, applicationId, appVersion, includeFarelockInfo);
                    }
                    if (!string.IsNullOrEmpty(_configuration.GetValue<string>("ReturnNonRevPNRs")) && Convert.ToBoolean(_configuration.GetValue<string>("ReturnNonRevPNRs")))
                    {
                        try
                        {
                            string displayEmployeeId = string.Empty;
                            string employeeId = string.Empty;
                            var tupleRes = await GetEmployeeIdy(transactionId, mileagePlusNumber, _headers.ContextValues.SessionId, displayEmployeeId);
                            employeeId = tupleRes.employeeId;
                            displayEmployeeId = tupleRes.displayEmployeeId;
                            employeeId = (getEmployeeIdFromCSLCustomerData)
                                ? await GetEmployeeIdFromCSL(transactionId, mileagePlusNumber, languageCode, appVersion, applicationId, "", getEmployeeIdFromCSLCustomerData)
                                : tupleRes.employeeId;


                            if (!string.IsNullOrEmpty(employeeId))
                            {
                                List<MOBEmployeePNR> employeePNRList = await GetEmployeePNRs(transactionId, employeeId);

                                if (employeePNRList != null && employeePNRList.Count > 0)
                                {
                                    //List<MOBPNR> employeePNRs = new List<MOBPNR>();
                                    if (opPNRs == null)
                                    {
                                        opPNRs = new MOBMPPNRs();
                                    }
                                    if (opPNRs.Current == null)
                                    {
                                        opPNRs.Current = new List<MOBPNR>();
                                    }

                                    Parallel.ForEach(employeePNRList, async item =>
                                    {
                                        MOBPNR aPNR = new MOBPNR();
                                        try
                                        {
                                            //View Res XML to CSL migration
                                            bool isCSLPNRServiceOn = Convert.ToBoolean(_configuration.GetValue<string>("SwithToCSLPNRService") ?? "false");
                                            if (!isCSLPNRServiceOn)
                                            {

                                                aPNR = await GetPNRByRecordLocator(transactionId, item.RecordLocator,
                                                    item.LastName, "en-US", 1, appVersion, forWallet);
                                            }
                                            else
                                            {
                                                aPNR = await GetPNRByRecordLocatorFromCSL(transactionId, "", item.RecordLocator,
                                                    item.LastName, "en-US", applicationId, appVersion, forWallet);

                                            }
                                            if (aPNR != null)
                                            {
                                                opPNRs.Current.Add(aPNR);
                                            }
                                        }
                                        catch (System.Exception) { }
                                    });
                                }
                            }
                        }
                        catch (System.Exception) { }
                    }

                    if (opPNRs == null || opPNRs.Current == null || opPNRs.Current.Count == 0)
                    {
                        throw new MOBUnitedException("No current reservation found");
                    }

                }
                else
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("GenericExceptionMessage"));
                }
            }
            catch (MOBUnitedException ex)
            {
                throw new MOBUnitedException(ex.Message);
            }
            //catch (WebException webEx)
            //{
            //    var errorResponse = new StreamReader(webEx.Response.GetResponseStream()).ReadToEnd();
            //    var xmlError = ProviderHelper.SerializeXml(errorResponse);
            //    //if (this.levelSwitch.TraceError)
            //    //{
            //    //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(transactionId, "GetPNRsByMileagePlusNumberCSL", "Exception", xmlError));
            //    //}
            //    if (_fitbitUtility.EnableWalletPNRDropClientFix(applicationId, appVersion)) //appVersion do app versoin check
            //    {
            //        opPNRs = new MOBMPPNRs();
            //        opPNRs.GotException4GetPNRSbyMPCSLcallDoNotDropExistingPnrsInWallet = true;
            //    }
            //}
            catch (ArgumentException argEx)
            {
                //if (this.levelSwitch.TraceError)
                //{
                //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(transactionId, "GetPNRsByMileagePlusNumberCSL", "Exception", argEx.Message));
                //}
                if (_manageResUtility.EnableWalletPNRDropClientFix(applicationId, appVersion)) //appVersion do app versoin check
                {
                    opPNRs = new MOBMPPNRs();
                    opPNRs.GotException4GetPNRSbyMPCSLcallDoNotDropExistingPnrsInWallet = true;
                }
            }
            catch (System.Exception ex)
            {
                //if (this.levelSwitch.TraceError)
                //{
                //    MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(transactionId, "GetPNRsByMileagePlusNumberCSL", "Exception", exceptionWrapper));
                //}
                if (_manageResUtility.EnableWalletPNRDropClientFix(applicationId, appVersion)) //appVersion do app versoin check
                {
                    opPNRs = new MOBMPPNRs();
                    opPNRs.GotException4GetPNRSbyMPCSLcallDoNotDropExistingPnrsInWallet = true;
                }
            }

            return opPNRs;
        }
        public async Task<MOBMPPNRs> GetPNRsByMileagePlusNumber(string transactionId, string mileagePlusNumber, MOBReservationType reservationType, string languageCode, bool includeFarelockInfo, string appVersion, bool forWallet, int applicationId, bool getEmployeeIdFromCSLCustomerData)
        {
            MOBMPPNRs opPNRs = null;

            UAWSMileagePlusReservation.wsReservationResponse response = null;
            // wsReservationSoapClient soapClient = new wsReservationSoapClient();
            response = await _mileagePlusReservationService.GetPNRsByMileagePlusNumber(mileagePlusNumber, _configuration.GetValue<string>("AccessCode - MileagePlusReservation"), string.Empty).ConfigureAwait(false);
            if (response.Errors == null || response.Errors.Length == 0)
            {
                #region
                switch (reservationType)
                {
                    case MOBReservationType.Current:
                        if (response.CurrentItineraryList.Length > 0)
                        {
                            if (opPNRs == null)
                            {
                                opPNRs = new MOBMPPNRs();
                            }
                            opPNRs.MileagePlusNumber = mileagePlusNumber;
                            opPNRs.Current = await this.GetPNRs(response.CurrentItineraryList, languageCode, true, false, applicationId, appVersion, mileagePlusNumber);
                        }

                        if (!string.IsNullOrEmpty(_configuration.GetValue<string>("ReturnNonRevPNRs")) && Convert.ToBoolean(_configuration.GetValue<string>("ReturnNonRevPNRs")))
                        {
                            try
                            {
                                string displayEmployeeId = string.Empty;
                                string employeeId = string.Empty;
                                var tupleRes = await GetEmployeeIdy(transactionId, mileagePlusNumber, _headers.ContextValues.SessionId, displayEmployeeId);
                                employeeId = tupleRes.employeeId;
                                displayEmployeeId = tupleRes.displayEmployeeId;
                                employeeId = (getEmployeeIdFromCSLCustomerData)

                                    ? await GetEmployeeIdFromCSL(transactionId, mileagePlusNumber, languageCode, appVersion, applicationId, "", getEmployeeIdFromCSLCustomerData)
                                    : tupleRes.employeeId;


                                if (!string.IsNullOrEmpty(employeeId))
                                {
                                    List<MOBEmployeePNR> employeePNRList = await GetEmployeePNRs(transactionId, employeeId);

                                    if (employeePNRList != null && employeePNRList.Count > 0)
                                    {
                                        //List<MOBPNR> employeePNRs = new List<MOBPNR>();
                                        if (opPNRs == null)
                                        {
                                            opPNRs = new MOBMPPNRs();
                                        }
                                        if (opPNRs.Current == null)
                                        {
                                            opPNRs.Current = new List<MOBPNR>();
                                        }

                                        Parallel.ForEach(employeePNRList, async item =>
                                        {
                                            MOBPNR aPNR = new MOBPNR();
                                            try
                                            {
                                                //View Res XML to CSL migration
                                                bool isCSLPNRServiceOn = Convert.ToBoolean(_configuration.GetValue<string>("SwithToCSLPNRService") ?? "false");
                                                if (!isCSLPNRServiceOn)
                                                {

                                                    aPNR = await GetPNRByRecordLocator(transactionId, item.RecordLocator,
                                                        item.LastName, "en-US", 1, appVersion, forWallet);
                                                }
                                                else
                                                {
                                                    aPNR = await GetPNRByRecordLocatorFromCSL(transactionId, "", item.RecordLocator,
                                                       item.LastName, "en-US", applicationId, appVersion, forWallet);

                                                }
                                                if (aPNR != null)
                                                {
                                                    opPNRs.Current.Add(aPNR);
                                                }
                                            }
                                            catch (System.Exception) { }
                                        });
                                    }
                                }
                            }
                            catch (System.Exception) { }
                        }

                        if (opPNRs == null || opPNRs.Current == null || opPNRs.Current.Count == 0)
                        {
                            throw new MOBUnitedException("No current reservation found");
                        }
                        else
                        {
                            if (IncludeFareLock(includeFarelockInfo, appVersion))
                            {
                                Dictionary<string, string> farelockPNRs = await GetFarelockPNR(1, string.Empty, transactionId, mileagePlusNumber);
                                if (farelockPNRs != null && farelockPNRs.Count > 0)
                                {
                                    foreach (var pnr in opPNRs.Current)
                                    {
                                        foreach (var farelockPNR in farelockPNRs)
                                        {
                                            if (pnr.RecordLocator.Equals(farelockPNR.Key))
                                            {
                                                pnr.FarelockExpirationDate = farelockPNR.Value;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        break;
                    case MOBReservationType.Past:
                        if (response.PastItineraryList.Length > 0)
                        {
                            if (opPNRs == null)
                            {
                                opPNRs = new MOBMPPNRs();
                            }
                            opPNRs.MileagePlusNumber = mileagePlusNumber;
                            opPNRs.Past = await this.GetPNRs(response.PastItineraryList, languageCode, false, true, applicationId, appVersion);
                        }
                        else
                        {
                            throw new MOBUnitedException("No past reservation found");
                        }

                        break;
                    case MOBReservationType.Cancelled:
                        if (response.CancelledItineraryList.Length > 0)
                        {
                            if (opPNRs == null)
                            {
                                opPNRs = new MOBMPPNRs();
                            }
                            opPNRs.MileagePlusNumber = mileagePlusNumber;
                            opPNRs.Cancelled = await this.GetPNRs(response.CancelledItineraryList, languageCode, false, true, applicationId, appVersion);
                            if (opPNRs.Cancelled != null && opPNRs.Cancelled.Count > 0)
                            {
                                opPNRs.Cancelled.Sort(p => p.DateCreated);
                            }
                        }
                        else
                        {
                            throw new MOBUnitedException("No canceled reservation found");
                        }

                        break;
                    case MOBReservationType.Inactive:
                        if (response.InactiveItineraryList.Length > 0)
                        {
                            if (opPNRs == null)
                            {
                                opPNRs = new MOBMPPNRs();
                            }
                            opPNRs.MileagePlusNumber = mileagePlusNumber;
                            opPNRs.Inactive = await this.GetPNRs(response.InactiveItineraryList, languageCode, false, true, applicationId, appVersion);
                        }
                        else
                        {
                            throw new MOBUnitedException("No inactive reservation found");
                        }
                        break;
                    default:
                        if (response.CurrentItineraryList.Length > 0 || response.PastItineraryList.Length > 0 || response.CancelledItineraryList.Length > 0 || response.InactiveItineraryList.Length > 0)
                        {
                            if (opPNRs == null)
                            {
                                opPNRs = new MOBMPPNRs();
                                opPNRs.MileagePlusNumber = mileagePlusNumber;
                            }

                            if (response.CurrentItineraryList.Length > 0)
                            {
                                opPNRs.Current = await this.GetPNRs(response.CurrentItineraryList, languageCode, true, false, applicationId, appVersion, mileagePlusNumber);
                                if (opPNRs.Current != null && opPNRs.Current.Count > 0)
                                {
                                    if (IncludeFareLock(includeFarelockInfo, appVersion))
                                    {
                                        Dictionary<string, string> farelockPNRs = await GetFarelockPNR(1, string.Empty, transactionId, mileagePlusNumber);
                                        if (farelockPNRs != null && farelockPNRs.Count > 0)
                                        {
                                            foreach (var pnr in opPNRs.Current)
                                            {
                                                foreach (var farelockPNR in farelockPNRs)
                                                {
                                                    if (pnr.RecordLocator.Equals(farelockPNR.Key))
                                                    {
                                                        pnr.FarelockExpirationDate = farelockPNR.Value;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            if (response.PastItineraryList.Length > 0)
                            {
                                opPNRs.Past = await this.GetPNRs(response.PastItineraryList, languageCode, false, true, applicationId, appVersion);
                                opPNRs.Past.SortDescending(pnr => Convert.ToDateTime(pnr.DateCreated));
                            }
                            if (response.CancelledItineraryList.Length > 0)
                            {
                                opPNRs.Cancelled = await this.GetPNRs(response.CancelledItineraryList, languageCode, false, true, applicationId, appVersion);
                                opPNRs.Cancelled.SortDescending(pnr => Convert.ToDateTime(pnr.DateCreated));
                            }
                            if (response.InactiveItineraryList.Length > 0)
                            {
                                opPNRs.Inactive = await this.GetPNRs(response.InactiveItineraryList, languageCode, false, true, applicationId, appVersion);
                            }
                        }

                        if (!string.IsNullOrEmpty(_configuration.GetValue<string>("ReturnNonRevPNRs")) && Convert.ToBoolean(_configuration.GetValue<string>("ReturnNonRevPNRs")))
                        {
                            try
                            {
                                string displayEmployeeId = string.Empty;
                                string employeeId = string.Empty;
                                var tupleRes = await GetEmployeeIdy(transactionId, mileagePlusNumber, _headers.ContextValues.SessionId, displayEmployeeId);
                                employeeId = tupleRes.employeeId;
                                displayEmployeeId = tupleRes.displayEmployeeId;
                                employeeId = (getEmployeeIdFromCSLCustomerData)
                                  ? await GetEmployeeIdFromCSL(transactionId, mileagePlusNumber, languageCode, appVersion, applicationId, "", getEmployeeIdFromCSLCustomerData)
                                  : tupleRes.employeeId;
                                if (!string.IsNullOrEmpty(employeeId))
                                {
                                    List<MOBEmployeePNR> employeePNRList = await GetEmployeePNRs(transactionId, employeeId);

                                    if (employeePNRList != null && employeePNRList.Count > 0)
                                    {
                                        //List<MOBPNR> employeePNRs = new List<MOBPNR>();
                                        if (opPNRs == null)
                                        {
                                            opPNRs = new MOBMPPNRs();
                                        }
                                        if (opPNRs.Current == null)
                                        {
                                            opPNRs.Current = new List<MOBPNR>();
                                        }

                                        //foreach (var item in employeePNRList)
                                        //{
                                        //    MOBPNR aPNR = GetPNRByRecordLocator(transactionId, item.RecordLocator, item.LastName, "en-US", 1);
                                        //    if (aPNR != null)
                                        //    {
                                        //        opPNRs.Current.Add(aPNR);
                                        //    }
                                        //}

                                        Parallel.ForEach(employeePNRList, async item =>
                                        {
                                            MOBPNR aPNR = new MOBPNR();
                                            try
                                            {
                                                //View Res XML to CSL migration
                                                bool isCSLPNRServiceOn = Convert.ToBoolean(_configuration.GetValue<string>("SwithToCSLPNRService") ?? "false");
                                                if (!isCSLPNRServiceOn)
                                                {
                                                    aPNR = await GetPNRByRecordLocator(transactionId, item.RecordLocator,
                                                        item.LastName, "en-US", 1, appVersion, forWallet);
                                                }
                                                else
                                                {
                                                    aPNR = await GetPNRByRecordLocatorFromCSL(transactionId, "", item.RecordLocator,
                                                        item.LastName, "en-US", applicationId, appVersion, forWallet);
                                                }
                                                if (aPNR != null)
                                                {
                                                    opPNRs.Current.Add(aPNR);
                                                }
                                            }
                                            catch (System.Exception ex) { Debug.WriteLine(ex.Message); }
                                        });
                                    }
                                }
                            }
                            catch (System.Exception ex) { Debug.WriteLine(ex.Message); }
                        }


                        if (opPNRs == null || (opPNRs.Current == null && opPNRs.Cancelled == null && opPNRs.Past == null && opPNRs.Inactive == null))
                        {
                            throw new MOBUnitedException("No reservation found");
                        }

                        break;
                }
                #endregion
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("GenericExceptionMessage"));
            }

            //if (this.levelSwitch.TraceError)
            //{
            //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBMPPNRs>(transactionId, "GetPNRsByMileagePlusNumberResponse", "Response", opPNRs));//Common Login Code
            //}
            return opPNRs;
        }

        private async Task<(string employeeId, string displayEmployeeId)> GetEmployeeIdy(string mileageplusNumber, string transactionId, string sessionId, string displayEmployeeId)
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
        public string GetTravelType(Collection<ReservationFlightSegment> flightSegments)
        {
            var journeytype = string.Empty;

            if (_configuration.GetValue<bool>("PlacePassTurnOnToggle_Manageres"))
            {
                if (flightSegments.Any(p => p != null))
                {

                    var maxTripNumber = flightSegments.Max(tq => tq.TripNumber.ToInt());
                    var minTripNumber = flightSegments.Min(f => f.TripNumber.ToInt());

                    if (maxTripNumber == 1)
                    {
                        journeytype = "OW";
                    }

                    if (maxTripNumber == 2)
                    {

                        var firstTripDepartureAirportCode = flightSegments.Where(t => t.TripNumber == minTripNumber.ToString()).Select(t => t.FlightSegment.DepartureAirport.IATACode).FirstOrDefault();
                        var firstTripArrivalAirportCode = flightSegments.Where(t => t.TripNumber == minTripNumber.ToString()).Select(t => t.FlightSegment.ArrivalAirport.IATACode).LastOrDefault();
                        var lastTripArrivalAirportCode = flightSegments.Where(f => f.TripNumber == maxTripNumber.ToString()).Select(t => t.FlightSegment.ArrivalAirport.IATACode).LastOrDefault();
                        var lastTripDepartureAirportCode = flightSegments.Where(f => f.TripNumber == maxTripNumber.ToString()).Select(t => t.FlightSegment.DepartureAirport.IATACode).FirstOrDefault();

                        if (firstTripDepartureAirportCode == lastTripArrivalAirportCode && firstTripArrivalAirportCode == lastTripDepartureAirportCode)
                        {
                            journeytype = "RT";
                        }
                        else
                        {
                            journeytype = "MD";
                        }

                    }
                    if (maxTripNumber > 2)
                    {
                        journeytype = "MD";
                    }
                }
            }
            return journeytype;
        }
        public List<MOBPNRSegment> CheckUpgradeEligibilityCSL(string sessionId, int eliteLevel, List<MOBPNRSegment> segments, bool hasGSPax, bool has1KPax, System.Collections.ObjectModel.Collection<United.Service.Presentation.SegmentModel.ReservationFlightSegment> response)
        {
            const string remark1 = "{0} members are eligible for complimentary upgrades on select flights when traveling in Y, B, or M fare classes, subject to availability. The flights listed below have available upgrade space that may be confirmed at this time. Please indicate if you would like to be upgraded on the following flight(s).";
            const string remark2 = "{0} members are eligible for complimentary upgrades on select flights when traveling in Y or B fare classes, subject to availability. The flights listed below have available upgrade space that may be confirmed at this time. Please indicate if you would like to be upgraded on the following flight(s).";

            List<MOBPNRSegment> segmentList = null;

            // if (!string.IsNullOrEmpty(sessionId) && segments != null && segments.Count > 0)
            //{

            if (response != null && response.Count > 0)
            {
                segmentList = new List<MOBPNRSegment>();
                foreach (var segment in response)
                {
                    if (segments != null)
                    {
                        foreach (var s in segments)
                        {
                            if (s.MarketingCarrier.Code.Equals(segment.FlightSegment.MarketedFlightSegment[0].MarketingAirlineCode)

                                && s.FlightNumber.Equals(segment.FlightSegment.FlightNumber)
                                && s.Departure.Code.Equals(segment.FlightSegment.DepartureAirport.IATACode)
                                && s.Arrival.Code.Equals(segment.FlightSegment.ArrivalAirport.IATACode)
                                && Convert.ToDateTime(s.ScheduledDepartureDateTime) == Convert.ToDateTime(segment.FlightSegment.DepartureDateTime))
                            {
                                s.Upgradeable = segment.FlightSegment.InstantUpgradable;
                                var upgradeMessageCode = GetCharactersticValue(segment.FlightSegment.Characteristic, "UpgradeMessageCode");

                                s.UpgradeableMessageCode = upgradeMessageCode;//segment.UpgradeMessageCode;

                                if (segment.FlightSegment.InstantUpgradable && !string.IsNullOrEmpty(upgradeMessageCode))
                                {
                                    switch (upgradeMessageCode)
                                    {
                                        case "FE":
                                            s.UpgradeableMessage = "Premier Instant Upgrade Fare";
                                            s.UpgradeableRemark = "You are eligible for a complimentary instant upgrade to United First on this flight.";
                                            switch (eliteLevel)
                                            {
                                                case 5:
                                                    s.ComplimentaryInstantUpgradeMessage = string.Format(remark1, "Global Service");
                                                    break;
                                                case 4:
                                                    if (hasGSPax && has1KPax)
                                                    {
                                                        s.ComplimentaryInstantUpgradeMessage = string.Format(remark1, "Global Service and Premier 1K");
                                                    }
                                                    else
                                                    {
                                                        s.ComplimentaryInstantUpgradeMessage = string.Format(remark1, "Premier 1K");
                                                    }
                                                    break;
                                                case 3:
                                                    s.ComplimentaryInstantUpgradeMessage = string.Format(remark2, "Premier");
                                                    break;
                                                case 2:
                                                    s.ComplimentaryInstantUpgradeMessage = string.Format(remark2, "Premier");
                                                    break;
                                                case 1:
                                                    s.ComplimentaryInstantUpgradeMessage = string.Format(remark2, "Premier");
                                                    break;
                                            }
                                            break;
                                    }
                                }

                                segmentList.Add(s);
                            }
                        }
                    }
                }
            }
            //}
            else
            {
                segmentList = segments;
            }

            return segmentList;
        }

        public List<MOBPNRSegment> ProcessWaitlistUpgradeVisibilityCSL
            (MOBPNR pnr, ref bool hasUpgradeVisibility, int lowestEliteLevel,
            int applicationId, bool hasGSPax, bool has1KPax, Reservation details = null)
        {
            List<MOBPNRSegment> segments = null;
            List<MOBPNRSegment> waitedSegments = null;

            List<MOBSegmentResponse> waitlistSegments = null;

            //Bug 200601 - Below check added for findng duplicate sements 
            var duplicatesegmentcout = 0;
            if (pnr != null && pnr.Segments != null && pnr.Segments.Count > 0)
            {
                duplicatesegmentcout = pnr.Segments.GroupBy(x => x.FlightNumber).Where(g => g.Count() > 1).Select(y => y.Key).Count();
            }

            if (pnr != null && pnr.Segments != null && pnr.Segments.Count > 0)
            {
                foreach (MOBPNRSegment segment in pnr.Segments)
                {
                    if (segment.UpgradeVisibility != null && segment.UpgradeVisibility.WaitlistSegments != null && segment.UpgradeVisibility.WaitlistSegments.Count > 0)
                    {
                        foreach (var waitlistSegment in segment.UpgradeVisibility.WaitlistSegments)
                        {
                            if (waitlistSegments == null)
                            {
                                waitlistSegments = new List<MOBSegmentResponse>();
                            }
                            waitlistSegments.Add(waitlistSegment);
                        }
                    }
                }
            }

            if (waitlistSegments != null && waitlistSegments.Count > 0)
            {
                if (pnr != null && pnr.Segments != null && pnr.Segments.Count > 0)
                {
                    foreach (var waitlistSegment in waitlistSegments)
                    {
                        foreach (var segment in pnr.Segments)
                        {
                            if (segment.MarketingCarrier.Code.Equals(waitlistSegment.CarrierCode)
                                && segment.ClassOfService.Equals(waitlistSegment.ClassOfService)
                                && segment.FlightNumber.Equals(waitlistSegment.FlightNumber.ToString())
                                && segment.Departure.Code.Equals(waitlistSegment.Origin)
                                && segment.Arrival.Code.Equals(waitlistSegment.Destination)
                                && Convert.ToDateTime(segment.ScheduledDepartureDateTime) == Convert.ToDateTime(waitlistSegment.DepartureDateTime))
                            {
                                //Bug 185031 - Changed remove property value from true to false to display the flight segment details.
                                if (Convert.ToInt32(duplicatesegmentcout) > 0)
                                {
                                    segment.Remove = true;
                                }

                                if (waitedSegments == null)
                                {
                                    waitedSegments = new List<MOBPNRSegment>();
                                }
                                waitedSegments.Add(segment);
                            }
                        }
                    }
                }
            }

            if (pnr.Segments != null && pnr.Segments.Count > 0)
            {
                segments = new List<MOBPNRSegment>();
                foreach (var segment in pnr.Segments)
                {
                    if (segment.Remove)
                    {
                        //Do not add segment to the return list
                    }
                    else
                    {
                        if (segment.Waitlisted)
                        {
                            if (waitedSegments == null)
                            {
                                waitedSegments = new List<MOBPNRSegment>();
                            }
                            waitedSegments.Add(segment);
                        }

                        segments.Add(segment);
                    }
                }

                if (waitedSegments != null && waitedSegments.Count > 0)
                {
                    string confirmedActionCodes = _configuration.GetValue<string>("flightSegmentTypeCode");
                    foreach (var waitedSegment in waitedSegments)
                    {
                        foreach (var segment in segments)
                        {
                            if (segment.MarketingCarrier.Code.Equals(waitedSegment.MarketingCarrier.Code)
                                && segment.FlightNumber.Equals(waitedSegment.FlightNumber)
                                && segment.Departure.Code.Equals(waitedSegment.Departure.Code)
                                && segment.Arrival.Code.Equals(waitedSegment.Arrival.Code)
                                // Bug - 185144, 185901, 185907 - Commented below condition code to show the correct class of description in the details section of upgrade request - Vijayan
                                //&& segment.ActionCode.Equals(waitedSegment.ActionCode)  //Sometimes flight details are duplicated but FlightSegmentType will be different. Use FlightSegmentType to differentiate duplicate flights.
                                && Convert.ToDateTime(segment.ScheduledDepartureDateTime) == Convert.ToDateTime(waitedSegment.ScheduledDepartureDateTime))
                            {
                                if (waitedSegment.UpgradeVisibility != null && !string.IsNullOrEmpty(waitedSegment.UpgradeVisibility.SegmentActionCode) && waitedSegment.UpgradeVisibility.SegmentActionCode.IndexOf(confirmedActionCodes) != -1)
                                {
                                    segment.ClassOfServiceDescription = waitedSegment.ClassOfServiceDescription;
                                }
                                segment.WaitedCOSDesc = waitedSegment.ClassOfServiceDescription;
                            }

                            //string confirmedActionCodes = "HK|HK1|DK|KL|RR|TK"; //per csl flightsegmentType HK1
                            //string confirmedActionCodes = _configuration.GetValue<string>("flightSegmentTypeCode"];
                            if (segment.MarketingCarrier.Code.Equals(waitedSegment.MarketingCarrier.Code)
                                && segment.Departure.Code.Equals(waitedSegment.Departure.Code)
                                && segment.Arrival.Code.Equals(waitedSegment.Arrival.Code)
                                && segment.ActionCode.Equals(waitedSegment.ActionCode)  //Sometimes flight details are duplicated but FlightSegmentType will be different. Use FlightSegmentType to differentiate duplicate flights.
                                && confirmedActionCodes.IndexOf(segment.ActionCode.Substring(0, 2), StringComparison.Ordinal) != -1) //per csl use first 2 characters 
                            {
                                waitedSegment.Remove = true;
                                break;
                            }
                        }
                    }

                    foreach (var segment in segments)
                    {
                        foreach (var waitedSegment in waitedSegments)
                        {
                            if (waitedSegment.Remove
                                && segment.MarketingCarrier.Code.Equals(waitedSegment.MarketingCarrier.Code)
                                && segment.FlightNumber.Equals(waitedSegment.FlightNumber)
                                && segment.Departure.Code.Equals(waitedSegment.Departure.Code)
                                && segment.Arrival.Code.Equals(waitedSegment.Arrival.Code)
                                && Convert.ToDateTime(segment.ScheduledDepartureDateTime) == Convert.ToDateTime(waitedSegment.ScheduledDepartureDateTime)
                                && segment.ClassOfService.Equals(waitedSegment.ClassOfService))
                            {
                                //Bug 185031 - Changed remove property value from true to false to display the flight segment details.
                                if (Convert.ToInt32(duplicatesegmentcout) > 0)
                                {
                                    segment.Remove = true;
                                }
                                break;
                            }
                        }
                    }

                    List<MOBPNRSegment> tempSegments = new List<MOBPNRSegment>();
                    foreach (var segment in segments)
                    {
                        if (!segment.Remove)
                        {
                            tempSegments.Add(segment);
                        }
                    }
                    segments = tempSegments;
                }

                foreach (MOBPNRSegment segment in segments)
                {
                    if (segment.UpgradeVisibility != null)
                    {
                        string cosDesc = string.Empty;
                        if (segment.WaitedCOSDesc != string.Empty)
                        {
                            cosDesc = segment.WaitedCOSDesc;
                        }
                        else
                        {
                            cosDesc = segment.ClassOfServiceDescription;
                        }

                        segment.UpgradeVisibility.UpgradeRemark
                            = GetUpgradeRemark(segment.UpgradeVisibility, segment.UpgradeVisibility.UpgradeMessageCode,
                            segment.UpgradeVisibility.UpgradeProperties, segment.UpgradeVisibility.UpgradeType, cosDesc,
                            segment, lowestEliteLevel, applicationId, hasGSPax, has1KPax, details);
                    }
                    else
                    {
                        //string confirmedActionCodes = "HK|DK|KL|RR|TK"; per csl flightsegmentType HK1
                        string confirmedActionCodes = _configuration.GetValue<string>("flightSegmentTypeCode");
                        if (confirmedActionCodes.IndexOf(segment.ActionCode.Substring(0, 2), StringComparison.Ordinal) == -1) //per csl use first 2 characters 
                        {
                            if (segment.UpgradeVisibility == null)
                            {
                                segment.UpgradeVisibility = new MOBSegmentResponse();
                            }

                            hasUpgradeVisibility = true;
                            segment.UpgradeVisibility.SegmentActionCode = segment.ActionCode;
                            segment.UpgradeVisibility.UpgradeStatus = MOBUpgradeEligibilityStatus.Requested;
                            segment.UpgradeVisibility.DecodedUpgradeMessage = "This flight segment is waitlisted.";
                            segment.UpgradeVisibility.UpgradeRemark = "This flight segment is waitlisted.";
                        }
                        else
                        {
                            if (segment.ActionCode.Equals("KL"))
                            {
                                if (segment.UpgradeVisibility == null)
                                {
                                    segment.UpgradeVisibility = new MOBSegmentResponse();
                                }
                                hasUpgradeVisibility = true;
                                segment.UpgradeVisibility.SegmentActionCode = "KL";
                                segment.UpgradeVisibility.UpgradeStatus = MOBUpgradeEligibilityStatus.RequestConfirmed;
                                segment.UpgradeVisibility.DecodedUpgradeMessage = _configuration.GetValue<string>("WUPMessageConfirmedUpgrade"); ;
                                segment.UpgradeVisibility.UpgradeRemark = string.Format("Your upgrade to {0} has been confirmed. We are processing your reservation and expect it to be completed shortly. We appreciate your patience.", segment.ClassOfServiceDescription);
                            }
                            //Bug 218862:PROD- mAPP view reservation showing as waitlisted when .com shows as not confirmed - Kirti
                            else if (segment.ActionCode.Substring(0, 2).Equals("UC", StringComparison.InvariantCultureIgnoreCase))
                            {
                                hasUpgradeVisibility = true;
                                segment.UpgradeVisibility = GetSegmentResponse(segment.UpgradeVisibility, segment.ActionCode, MOBUpgradeEligibilityStatus.Requested, _configuration.GetValue<string>("SegmentNotConfirmedErrorMsg"));
                            }
                        }
                    }
                }
            }

            return segments;
        }

        public MOBSegmentResponse GetSegmentResponse(MOBSegmentResponse mobSegmentResponse, string actionCode, MOBUpgradeEligibilityStatus UpgradeStatus, string UpgradeMessage)
        {
            if (mobSegmentResponse == null)
            {
                mobSegmentResponse = new MOBSegmentResponse();
            }

            mobSegmentResponse.SegmentActionCode = actionCode;
            mobSegmentResponse.UpgradeStatus = UpgradeStatus;
            mobSegmentResponse.DecodedUpgradeMessage = UpgradeMessage;
            mobSegmentResponse.UpgradeRemark = UpgradeMessage;
            return mobSegmentResponse;
        }

        public async Task<List<MOBPNR>> GetPNRsCSL(List<Service.Presentation.ReservationModel.Reservation> response, string languageCode, bool includeTrip, bool inactiveOrPastOrCancelled, int applicationId, string appVersion, bool includeFarelockInfo = false)
        {
            List<MOBPNR> pnrs = new List<MOBPNR>();
            foreach (Service.Presentation.ReservationModel.Reservation reservation in response)
            {
                if (reservation == null)
                {
                    continue;
                }

                MOBPNR pnr = new MOBPNR();
                pnr.DateCreated = TopHelper.FormatDatetime(Convert.ToDateTime(reservation.CreateDate).ToString("yyyyMMdd hh:mm tt"), languageCode);
                pnr.RecordLocator = reservation.ConfirmationID;
                pnr.FarelockExpirationDate = IncludeFareLock(includeFarelockInfo, appVersion) ? GetCharactersticValue(reservation.Characteristic, "FARELOCK_ISSUE_DATE") : string.Empty;


                pnr.IsActive = Convert.ToBoolean(GetCharactersticValue(reservation.Characteristic, "HAS_ETICKET"));
                pnr.NumberOfPassengers = reservation.Travelers != null ? reservation.Travelers.Count.ToString() : "0";


                if (includeTrip)
                {
                    pnr.Trips = new List<MOBTrip>();
                    pnr.Trips.Add(await GetTripInfoCSL(reservation.FlightSegments, reservation.NickName));
                }

                pnr.Description = reservation.NickName;

                if (reservation.Travelers == null)
                {
                    throw new MOBUnitedException("Unable to retrieve traveler information.");
                }

                pnr.Passengers = new List<MOBPNRPassenger>();
                foreach (Traveler traveler in reservation.Travelers)
                {

                    MOBPNRPassenger pnrPassenger = new MOBPNRPassenger();
                    pnrPassenger.PassengerName = new United.Mobile.Model.Common.MOBName();
                    if (_configuration.GetValue<string>("DecodeFirstNameCSL") != null && Convert.ToBoolean(_configuration.GetValue<string>("DecodeFirstNameCSL")))
                    {
                        pnrPassenger.PassengerName.First = DecodeFirstName(traveler.Person.GivenName);
                    }
                    else
                    {
                        pnrPassenger.PassengerName.First = traveler.Person.GivenName;
                    }
                    pnrPassenger.PassengerName.Last = traveler.Person.Surname;
                    pnr.Passengers.Add(pnrPassenger);
                }

                if (reservation.FlightSegments == null)
                {
                    throw new MOBUnitedException("Unable to retrieve segment information.");
                }

                string scheduledDepartureDateForLastSegment = string.Empty;
                string scheduledDepartureGMTDateForLastSegment = string.Empty;
                string scheduledArrivalDateForLastSegment = string.Empty;

                pnr.Segments = new List<MOBPNRSegment>();
                foreach (ReservationFlightSegment segment in reservation.FlightSegments)
                {
                    MOBPNRSegment pnrSegment = new MOBPNRSegment();
                    //added Trip/Segment Numbers for club notification - Sphurthi
                    pnrSegment.TripNumber = segment.TripNumber;
                    pnrSegment.SegmentNumber = segment.SegmentNumber;
                    pnrSegment.Arrival = new MOBAirport();
                    pnrSegment.Arrival.Code = segment.FlightSegment.ArrivalAirport.IATACode;
                    string airportName = string.Empty;
                    string cityName = string.Empty;
                    var tupleRes = await _airportDynamoDB.GetAirportCityName(pnrSegment.Arrival.Code, _headers.ContextValues.SessionId, airportName, cityName);
                    airportName = tupleRes.airportName;
                    cityName = tupleRes.cityName;
                    pnrSegment.Arrival.Name = airportName;
                    pnrSegment.Arrival.City = cityName;
                    pnrSegment.ClassOfService = segment.FlightSegment.BookingClasses[0].Code;

                    //pnrSegment.ClassOfServiceDescription = segment.ServiceClassDescription;
                    //Please do not un-comment the following line. .COM returns the incorrect COS description
                    pnrSegment.ClassOfServiceDescription = "--";

                    pnrSegment.Departure = new MOBAirport();
                    pnrSegment.Departure.Code = segment.FlightSegment.DepartureAirport.IATACode;
                    airportName = string.Empty;
                    cityName = string.Empty;
                    tupleRes = await _airportDynamoDB.GetAirportCityName(pnrSegment.Departure.Code, _headers.ContextValues.SessionId, airportName, cityName);

                    airportName = tupleRes.airportName;
                    cityName = tupleRes.cityName;
                    pnrSegment.Departure.Name = airportName;
                    pnrSegment.Departure.City = cityName;
                    pnrSegment.FlightNumber = segment.FlightSegment.FlightNumber;

                    pnrSegment.MarketingCarrier = new MOBAirline();
                    pnrSegment.MarketingCarrier.Code = segment.FlightSegment.OperatingAirlineCode;
                    pnrSegment.MarketingCarrier.Name = await _airportDynamoDB.GetCarrierInfo(pnrSegment.MarketingCarrier.Code, _headers.ContextValues.SessionId);

                    if (segment.FlightSegment.ArrivalDateTime != null && segment.FlightSegment.ArrivalDateTime.Trim().Length != 0)
                    {
                        pnrSegment.ScheduledArrivalDateTime = GeneralHelper.FormatDatetime(Convert.ToDateTime(segment.FlightSegment.ArrivalDateTime).ToString("yyyyMMdd hh:mm tt"), languageCode);
                        scheduledArrivalDateForLastSegment = pnrSegment.ScheduledArrivalDateTime;
                    }
                    else
                    {
                        pnrSegment.ScheduledArrivalDateTime = scheduledArrivalDateForLastSegment;
                    }

                    DateTime scheduledDepartureDateTimeGMT;
                    if (segment.FlightSegment.DepartureDateTime != null && segment.FlightSegment.DepartureDateTime.Trim().Length != 0)
                    {
                        pnrSegment.ScheduledDepartureDateTime = TopHelper.FormatDatetime(Convert.ToDateTime(segment.FlightSegment.DepartureDateTime).ToString("yyyyMMdd hh:mm tt"), languageCode);
                        pnrSegment.ScheduledDepartureDateTimeGMT = await _manageResUtility.GetGMTTime(pnrSegment.ScheduledDepartureDateTime, pnrSegment.Departure.Code, _headers.ContextValues.SessionId);
                        pnrSegment.ScheduledArrivalDateTimeGMT = await _manageResUtility.GetGMTTime(pnrSegment.ScheduledArrivalDateTime, pnrSegment.Arrival.Code, _headers.ContextValues.SessionId);

                        scheduledDepartureDateForLastSegment = pnrSegment.ScheduledDepartureDateTime;
                        scheduledDepartureGMTDateForLastSegment = pnrSegment.ScheduledDepartureDateTimeGMT;

                        if (DateTime.TryParse(pnrSegment.ScheduledDepartureDateTimeGMT, out scheduledDepartureDateTimeGMT))
                        {
                            if (DateTime.UtcNow > scheduledDepartureDateTimeGMT.AddDays(-1) && DateTime.UtcNow <= scheduledDepartureDateTimeGMT)
                            {
                                pnr.CheckinEligible = "Y";
                            }

                        }
                    }
                    else
                    {
                        pnrSegment.ScheduledDepartureDateTime = scheduledDepartureDateForLastSegment;
                        pnrSegment.ScheduledDepartureDateTimeGMT = scheduledDepartureGMTDateForLastSegment;
                        //throw new ContinentalException("We were unable to review the latest information for this itinerary.");
                    }

                    if (segment.Seat != null)
                    {
                        pnrSegment.Seats = new List<MOBPNRSeat>();
                        string[] seatNumbers = segment.Seat.SeatClass.Split('*');
                        foreach (string seatNumber in seatNumbers)
                        {
                            if (seatNumber.Trim().Length > 0)
                            {
                                MOBPNRSeat pnrSeat = new MOBPNRSeat();
                                pnrSeat.Number = seatNumber;
                                pnrSegment.Seats.Add(pnrSeat);
                            }
                        }
                    }

                    pnr.Segments.Add(pnrSegment);
                }

                if (!inactiveOrPastOrCancelled)
                {
                    if (pnr.Segments != null && pnr.Segments.Count > 0)
                    {
                        pnrs.Add(pnr);
                    }
                }
                else
                {
                    pnrs.Add(pnr);
                }

                if (_configuration.GetValue<bool>("GetCheckInStatusFromCSLPNRRetrivalService_Toggle"))
                {
                    _manageResUtility.GetCheckInEligibilityStatusFromCSLPnrReservation(reservation.CheckinEligibility, ref pnr);
                    pnr.GetCheckInStatusFromCSLPNRRetrivalService = true;
                }


                if (includeTrip && pnr.Segments.Count > 0)
                {
                    DateTime lastArrivalTime;
                    if (DateTime.TryParse(pnr.Segments[pnr.Segments.Count - 1].ScheduledArrivalDateTime, out lastArrivalTime))
                    {
                        pnr.LastTripDateArrivalDate = lastArrivalTime.ToString("MM/dd/yyyy hh:mm tt");
                        //pnr.LastTripDateArrivalDate = lastArrivalTime.ToString("MMMM").Length > 3 ? string.Format("{0:ddd., MMM. d, yyyy h:mm tt}", lastArrivalTime) : string.Format("{0:ddd., MMM d, yyyy h:mm tt}", lastArrivalTime); ;
                    }
                }
                //}
            }

            return pnrs;
        }
        private bool IncludeFareLock(bool include, string applicationVersion)
        {
            bool ok = false;
            if (include)
            {
                applicationVersion = string.IsNullOrEmpty(applicationVersion) ? string.Empty : applicationVersion.ToUpper();
                if (_manageResUtility.IsVersionEnableMP2015LMXCallOrFareLock(applicationVersion, _configuration.GetValue<string>("AppVersionsForFareLock")))
                {
                    ok = true;
                }
            }

            return ok;
        }

        #region helper function for GetPNRsCSL to get trip details - Hasnan.
        public async Task<MOBTrip> GetTripInfoCSL(Collection<ReservationFlightSegment> flightSegments, string nickName = "")
        {
            MOBTrip trip = new MOBTrip();
            if (flightSegments != null && flightSegments.Count > 0)
            {
                int mintripnumber = Convert.ToInt32(flightSegments.Select(o => o.TripNumber).FirstOrDefault());
                int maxtripnumber = Convert.ToInt32(flightSegments.Select(o => o.TripNumber).LastOrDefault());

                for (int i = mintripnumber; i <= maxtripnumber; i++)
                {

                    var totalTripSegments = flightSegments.Where(o => o.TripNumber == i.ToString());


                    foreach (ReservationFlightSegment segment in flightSegments)
                    {
                        if (!string.IsNullOrEmpty(segment.TripNumber) && Convert.ToInt32(segment.TripNumber) == i)
                        {
                            string airportName = string.Empty;
                            string cityName = string.Empty;

                            if (segment.SegmentNumber == totalTripSegments.Min(x => x.SegmentNumber)) // If segment.SegmentNumber is first segment of trip i
                            {
                                trip.Origin = segment.FlightSegment.DepartureAirport.IATACode;
                            }
                            if (segment.SegmentNumber == totalTripSegments.Max(x => x.SegmentNumber)) // If segment.SegmentNumber is last segment of trip i
                            {
                                trip.Destination = segment.FlightSegment.ArrivalAirport.IATACode;
                            }

                        }
                    }

                    if (!string.IsNullOrEmpty(trip.Origin) && !string.IsNullOrEmpty(trip.Destination))
                    {
                        // Found the current trip's origin and destination.
                        break;
                    }

                }
            }

            string originAirportName = string.Empty;
            string originCityName = string.Empty;
            string destinationAirportName = string.Empty;
            string destinationCityName = string.Empty;

            var tupleRes = await _airportDynamoDB.GetAirportCityName(trip.Origin, _headers.ContextValues.SessionId, originAirportName, originCityName);
            originAirportName = tupleRes.airportName;
            originCityName = tupleRes.cityName;
            tupleRes = await _airportDynamoDB.GetAirportCityName(trip.Destination, _headers.ContextValues.SessionId, destinationAirportName, destinationCityName);
            originAirportName = tupleRes.airportName;
            originCityName = tupleRes.cityName;
            trip.OriginName = originAirportName;
            trip.DestinationName = destinationAirportName;

            DateTime departureTime;
            if (flightSegments[0] != null && DateTime.TryParse(flightSegments[0].FlightSegment.DepartureDateTime, out departureTime))
            {
                trip.DepartureTime = departureTime.ToString("MM/dd/yyyy hh:mm tt");
                //trip.DepartureTime = departureTime.ToString("MMMM").Length > 3 ? string.Format("{0:ddd., MMM. d, yyyy h:mm tt}", departureTime) : string.Format("{0:ddd., MMM d, yyyy h:mm tt}", departureTime);
            }

            return trip;
        }
        #endregion

        #region helper function for GetPNRsCSL to get firstName of traveler - Hasnan.
        private string DecodeFirstName(string givenName)
        {
            try
            {
                string name = givenName.ToUpper().Trim();

                string decodedFirstName;

                if (name.Contains("PLUSINFANT"))
                {
                    name = name.Replace("PLUSINFANT", "");
                }

                if (name.Contains("PLUSINF"))
                {
                    name = name.Replace("PLUSINF", "");
                }

                int titleStart = Math.Max(1, name.Length - 2);
                string title = name.Substring(titleStart);
                switch (title)
                {
                    case "MR":
                    case "MS":
                    case "DR":
                        decodedFirstName = name.Substring(0, titleStart);
                        break;
                    case "RS": // might be last 2 characters of MRS
                        decodedFirstName = name.Replace("MRS", "");
                        break;
                    case "SS": // might be last 2 characters of MISS
                        decodedFirstName = name.Replace("MISS", "");
                        break;
                    case "TR": // might be last 2 characters of MSTR
                        decodedFirstName = name.Replace("MSTR", "");
                        break;
                    default:
                        decodedFirstName = name;
                        break;
                }
                if (decodedFirstName != null)
                {
                    return decodedFirstName;
                }
                else
                {
                    return name;
                }
            }
            catch (Exception ex)
            {
                return givenName;
            }
        }
        #endregion
        private string GetStarRewardVendorName(string vendorCode)
        {
            string programName = string.Empty;

            var section = _configuration.GetSection("Rewards").Get<List<RewardType>>();
            if (section != null)
            {
                foreach (var rewardType in section)
                {
                    if (rewardType.type.Equals(vendorCode))
                    {
                        programName = rewardType.description;
                        break;
                    }
                }
            }

            return programName;
        }

        private string GetCogCharactersticValue(Collection<Characteristic> characteristics, string code)
        {
            string value = string.Empty;

            if (characteristics != null && characteristics.Count > 0)
            {
                try
                {
                    for (int i = 0; i < characteristics.Count; i++)
                    {
                        if (characteristics[i] != null && characteristics[i].Code != null && characteristics[i].Code.Equals(code, StringComparison.InvariantCultureIgnoreCase))
                        {
                            value = characteristics[i].Value;

                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    value = string.Empty;
                }
            }

            return value;
        }

        public async System.Threading.Tasks.Task GetLmxDetails(string transactionId, string recordLocator, int applicationId, string appVersion,
            bool firstSegmentLifted, MOBPNR pnr, bool isPsSaTravel)
        {
            if (!firstSegmentLifted)
            {
                if (pnr != null && pnr.Segments != null)
                {
                    bool makeMP2015Call = false;
                    foreach (var segment in pnr.Segments)
                    {
                        if (Convert.ToDateTime(segment.ScheduledDepartureDateTime) >=
                            Convert.ToDateTime(_configuration.GetValue<string>("MP2015StartDate")) &&
                            _manageResUtility.IsVersionEnableMP2015LMXCallOrFareLock(appVersion,
                                _configuration.GetValue<string>("EnableMP2015LMXCallForVersions")))
                        {
                            makeMP2015Call = true;
                            break;
                        }
                    }
                    if (makeMP2015Call && !isPsSaTravel)
                    {
                        List<MOBLmxFlight> lmxFlights = null;
                        try
                        {
                            lmxFlights = await GetLmxFlights(applicationId, transactionId, transactionId,
                                recordLocator);
                        }
                        catch (System.Exception)
                        {
                            pnr.SupressLMX = true;
                        }

                        bool bypassMPPassengerCheck = false;
                        if (lmxFlights != null && lmxFlights.Count > 0)
                        {
                            Dictionary<string, bool> segmentsAdded = new Dictionary<string, bool>();
                            foreach (var segment in pnr.Segments)
                            {
                                foreach (var lmxFlight in lmxFlights)
                                {
                                    if (segment.FlightNumber.Equals(lmxFlight.FlightNumber)
                                        && segment.Departure.Code.Equals(lmxFlight.Departure.Code)
                                        && segment.Arrival.Code.Equals(lmxFlight.Arrival.Code)
                                        &&
                                        Convert.ToDateTime(segment.ScheduledDepartureDateTime)
                                            .ToString("yyyyMMdd")
                                            .Equals(Convert.ToDateTime(lmxFlight.ScheduledDepartureDateTime)
                                                .ToString("yyyyMMdd")))
                                    {
                                        string key = string.Format("{0}{1}{2}{3}", lmxFlight.FlightNumber,
                                            segment.Departure.Code, segment.Arrival.Code,
                                            Convert.ToDateTime(segment.ScheduledDepartureDateTime).ToString("yyyyMMdd"));
                                        if (!segmentsAdded.ContainsKey(key))
                                        {
                                            segment.NonPartnerFlight = true;
                                            bool lmxQuoteAvailable = false;
                                            if (lmxFlight.Products != null && lmxFlight.Products.Count > 0)
                                            {
                                                foreach (var product in lmxFlight.Products)
                                                {
                                                    if (product.LmxLoyaltyTiers != null && product.LmxLoyaltyTiers.Count > 0)
                                                    {
                                                        foreach (var lmxLoyaltyTier in product.LmxLoyaltyTiers)
                                                        {
                                                            if (lmxLoyaltyTier.LmxQuotes != null &&
                                                                lmxLoyaltyTier.LmxQuotes.Count > 0)
                                                            {
                                                                lmxQuoteAvailable = true;
                                                                foreach (var lmxQuote in lmxLoyaltyTier.LmxQuotes)
                                                                {
                                                                    if (!lmxQuote.Amount.Equals("0"))
                                                                    {
                                                                        segment.NonPartnerFlight = false;
                                                                        bypassMPPassengerCheck = true;
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }

                                            }
                                            //Fix for 281982:ViewRes - GetPnrByRecordLoacator() Object Reference Exception - Schedule Change--Niveditha
                                            if (lmxQuoteAvailable)
                                            {
                                                segmentsAdded.Add(key, true);
                                                segment.LmxProducts = lmxFlight.Products;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        pnr.lmxtravelers = GetLMXTravelersFromFlightsAndTravelers(pnr);

                        if (bypassMPPassengerCheck)
                        {
                            pnr.SupressLMX = false;
                        }
                        else
                        {
                            bool hasMPInPassengers = false;
                            if (pnr.Passengers != null && pnr.Passengers.Count > 0)
                            {
                                foreach (var passenger in pnr.Passengers)
                                {
                                    if (passenger.MileagePlus != null)
                                    {
                                        hasMPInPassengers = true;
                                        break;
                                    }
                                }
                            }

                            if (hasMPInPassengers)
                            {
                                pnr.SupressLMX = true;
                            }
                            else
                            {
                                pnr.SupressLMX = false;
                            }
                        }
                    }
                    else
                    {
                        //wade - adding bool to disable LMX at client
                        pnr.SupressLMX = !makeMP2015Call;
                        if (isPsSaTravel)
                        {
                            pnr.SupressLMX = true;
                        }
                    }
                }
            }
            else
            {
                pnr.SupressLMX = true;
            }
        }


        public bool CheckShowChangeSeatCSLNewCheck(Service.Presentation.ReservationModel.Reservation reservation)
        {
            bool isNotFlownSegment = false;
            foreach (United.Service.Presentation.SegmentModel.ReservationFlightSegment segment in reservation.FlightSegments)
            {
                //below condition will find one future flight form all FlightSegments
                if (TimeZoneInfo.ConvertTimeToUtc(Convert.ToDateTime(segment.EstimatedDepartureTime)) > DateTime.Now.ToUniversalTime())
                {
                    isNotFlownSegment = true;
                    break;
                }
            }
            return isNotFlownSegment;
        }

        public void GetIsEligibleToSeatChangeCSL(ReservationDetail response, MOBPNR pnr, int pastSegmentsCount)
        {
            //change seat button should be visible bug 53497. 
            pnr.IsEligibleToSeatChange = true;

            if (response.Detail.FlightSegments.Count() > 1 && (pastSegmentsCount == pnr.Segments.Count())) // Not to show the change seat option if all the segmetns are flown
            {
                if ((pnr.Segments.Count() - pastSegmentsCount) == 0)
                {
                    pnr.IsEligibleToSeatChange = CheckShowChangeSeatCSLNewCheck(response.Detail);
                    //pnr.IsEligibleToSeatChange = CheckShowChangeSeatCSL(response.Detail); ////response.Detail.FlightSegments 
                }

                //else
                //{
                //    pnr.IsEligibleToSeatChange = CheckShowChangeSeatCSLNewCheck(response.Detail);
                //}
            }// Bug 180754: Change Seat Button Should not display Farelock PNR - j.Srinivas
            // Story 573423: TPI should not display Farelock PNR - Elise 
            if (response != null && response.Detail != null && response.Detail.Characteristic != null && !string.IsNullOrEmpty(GetCharactersticValue(response.Detail.Characteristic, "FARELOCK_DATE")) && Convert.ToDateTime(GetCharactersticValue(response.Detail.Characteristic, "FARELOCK_DATE")) > DateTime.Now)
            {
                pnr.IsEligibleToSeatChange = false;
                pnr.IsFareLockOrNRSA = true;
            }// Change Seat Button Should not display for NRSA - j.Srinivas
            //TPI should not display for NRSA -Elise
            if (response != null && response.Detail != null && response.Detail.Travelers != null)
            {
                foreach (var traveler in response.Detail.Travelers)
                {
                    if (traveler.Tickets != null)
                    {
                        foreach (var ticket in traveler.Tickets)
                        {
                            string passengerindicator = GetCharactersticValue(ticket.Characteristic, "PassengerIndicator");

                            if (!string.IsNullOrEmpty(passengerindicator))
                            {
                                if (string.Equals(passengerindicator, "SA", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(passengerindicator, "SJ", StringComparison.OrdinalIgnoreCase)
                                    || string.Equals(passengerindicator, "SK", StringComparison.OrdinalIgnoreCase))
                                {
                                    pnr.IsEligibleToSeatChange = false;
                                    pnr.IsFareLockOrNRSA = true;
                                    break;
                                }

                                if (string.Equals(passengerindicator, "PS", StringComparison.OrdinalIgnoreCase))
                                {
                                    pnr.IsFareLockOrNRSA = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            // fix for Change seat not visible for UAX flights bug 166580
            // if (response.Detail.FlightSegments.Count() == 1 && response.Detail.FlightSegments[0].FlightSegment.OperatingAirlineCode != "UA")
            // {
            //     pnr.IsEligibleToSeatChange = false;
            // }
        }

        private async Task<List<MOBPNR>> GetPNRs(UAWSMileagePlusReservation.ItineraryHeader[] itineraryHeaders, string languageCode, bool includeTrip, bool inactiveOrPastOrCancelled, int applicationId, string applicationVersion, string mpNumber = "")
        {
            List<MOBPNR> pnrs = new List<MOBPNR>();

            foreach (UAWSMileagePlusReservation.ItineraryHeader itineraryHeader in itineraryHeaders)
            {
                foreach (UAWSMileagePlusReservation.Itinerary itinerary in itineraryHeader.ItineraryCol)
                {
                    if (itinerary.FlightDetails == null)
                    {
                        continue;
                    }

                    MOBPNR pnr = new MOBPNR();
                    pnr.DateCreated = TopHelper.FormatDatetime(Convert.ToDateTime(itinerary.ReservationCreatedDate).ToString("yyyyMMdd hh:mm tt"), languageCode);
                    //pnr.RecordLocator = itineraryHeader.ReservationNumber;
                    pnr.RecordLocator = itinerary.ReservationNumber;

                    pnr.AwardTravel = itinerary.FlightDetails.RewardTravel;

                    pnr.IsActive = itinerary.FlightDetails.ActiveTicketsExist;
                    pnr.NumberOfPassengers = itinerary.FlightDetails.Travelers.Length.ToString();

                    if (includeTrip)
                    {
                        pnr.Trips = new List<MOBTrip>();
                        pnr.Trips.Add(await GetTripInfo(itinerary));
                    }

                    pnr.Description = itinerary.Description;

                    if (itinerary.FlightDetails.Travelers == null)
                    {
                        throw new MOBUnitedException("Unable to retrieve traveler information.");
                    }

                    pnr.Passengers = new List<MOBPNRPassenger>();
                    foreach (UAWSMileagePlusReservation.Traveler traveler in itinerary.FlightDetails.Travelers)
                    {
                        if (string.IsNullOrEmpty(_configuration.GetValue<string>("EnableEResBooking")) && Convert.ToBoolean(_configuration.GetValue<string>("EnableEResBooking")))
                        {
                            if (!pnr.PsSaTravel && !string.IsNullOrEmpty(traveler.PassengerIndicator) && (traveler.PassengerIndicator.Equals("SA") || traveler.PassengerIndicator.Equals("PS")))
                            {
                                if (IsEResBetaTesterRecordLocator(pnr.RecordLocator, applicationId, applicationVersion))
                                {
                                    pnr.PsSaTravel = true;
                                }
                            }
                            else if (!pnr.PsSaTravel && !string.IsNullOrEmpty(itinerary.FlightDetails.EmployeeId))
                            {
                                if (IsEResBetaTesterRecordLocator(pnr.RecordLocator, applicationId, applicationVersion))
                                {
                                    pnr.PsSaTravel = true;
                                }
                            }
                        }

                        MOBPNRPassenger pnrPassenger = new MOBPNRPassenger();
                        pnrPassenger.PassengerName = new MOBName();
                        pnrPassenger.PassengerName.First = traveler.FirstName;
                        pnrPassenger.PassengerName.Middle = traveler.MiddleName;
                        pnrPassenger.PassengerName.Last = traveler.LastName;
                        pnrPassenger.PassengerName.Suffix = traveler.Suffix;
                        pnrPassenger.PassengerName.Title = traveler.Title;
                        pnrPassenger.SHARESPosition = traveler.SHARESPosition;
                        if (_configuration.GetValue<string>("EnableTravelReadiness") != null && Convert.ToBoolean(_configuration.GetValue<string>("EnableTravelReadiness")))
                        {
                            pnrPassenger.IsMPMember = (traveler.RewardPrograms != null && traveler.RewardPrograms.Any(p => p.ProgramMemberId == mpNumber)) ? true : false;
                        }
                        pnr.Passengers.Add(pnrPassenger);
                    }

                    if (itinerary.FlightDetails.Segments == null)
                    {
                        throw new MOBUnitedException("Unable to retrieve segment information.");
                    }

                    string scheduledDepartureDateForLastSegment = string.Empty;
                    string scheduledDepartureGMTDateForLastSegment = string.Empty;
                    string scheduledArrivalDateForLastSegment = string.Empty;

                    pnr.Segments = new List<MOBPNRSegment>();
                    foreach (UAWSMileagePlusReservation.Segment segment in itinerary.FlightDetails.Segments)
                    {
                        MOBPNRSegment pnrSegment = new MOBPNRSegment();
                        pnrSegment.Arrival = new MOBAirport();
                        pnrSegment.Arrival.Code = segment.Destination;
                        //pnrSegment.Arrival.Name = segment.DecodedDestination;
                        string airportName = string.Empty;
                        string cityName = string.Empty;
                        var tupleRes = await _airportDynamoDB.GetAirportCityName(segment.Destination, _headers.ContextValues.SessionId, airportName, cityName);
                        airportName = tupleRes.airportName;
                        cityName = tupleRes.cityName;
                        pnrSegment.Arrival.Name = airportName;
                        pnrSegment.Arrival.City = cityName;

                        pnrSegment.ClassOfService = segment.ServiceClass;
                        //pnrSegment.ClassOfServiceDescription = segment.ServiceClassDescription;
                        //Please do not un-comment the following line. .COM returns the incorrect COS description
                        pnrSegment.ClassOfServiceDescription = "--";

                        pnrSegment.Departure = new MOBAirport();
                        pnrSegment.Departure.Code = segment.Origin;
                        //pnrSegment.Departure.Name = segment.DecodedOrigin;
                        airportName = string.Empty;
                        cityName = string.Empty;
                        tupleRes = await _airportDynamoDB.GetAirportCityName(segment.Origin, _headers.ContextValues.SessionId, airportName, cityName);
                        airportName = tupleRes.airportName;
                        cityName = tupleRes.cityName;
                        pnrSegment.Departure.Name = airportName;
                        pnrSegment.Departure.City = cityName;
                        pnrSegment.FlightNumber = segment.FlightNumber;

                        pnrSegment.MarketingCarrier = new MOBAirline();
                        pnrSegment.MarketingCarrier.Code = segment.CarrierCode;
                        pnrSegment.MarketingCarrier.Name = segment.CarrierDescription;
                        pnrSegment.TripNumber = segment.TripIndex.ToString();
                        pnrSegment.SegmentNumber = segment.ItaSegmentIndex;


                        pnrSegment.CabinType = segment.CabinTypeText;

                        if (segment.DestinationDate != null && segment.DestinationDate.Trim().Length != 0)
                        {
                            pnrSegment.ScheduledArrivalDateTime = GeneralHelper.FormatDatetime(Convert.ToDateTime(segment.DestinationDate).ToString("yyyyMMdd hh:mm tt"), languageCode);
                            scheduledArrivalDateForLastSegment = pnrSegment.ScheduledArrivalDateTime;
                        }
                        else
                        {
                            pnrSegment.ScheduledArrivalDateTime = scheduledArrivalDateForLastSegment;
                            //throw new ContinentalException("We were unable to review the latest information for this itinerary.");
                        }

                        DateTime scheduledDepartureDateTimeGMT;
                        if (segment.DepartDate != null && segment.DepartDate.Trim().Length != 0)
                        {
                            pnrSegment.ScheduledDepartureDateTime = TopHelper.FormatDatetime(Convert.ToDateTime(segment.DepartDate).ToString("yyyyMMdd hh:mm tt"), languageCode);
                            pnrSegment.ScheduledDepartureDateTimeGMT = await _manageResUtility.GetGMTTime(pnrSegment.ScheduledDepartureDateTime, pnrSegment.Departure.Code, _headers.ContextValues.SessionId);
                            pnrSegment.ScheduledArrivalDateTimeGMT = await _manageResUtility.GetGMTTime(pnrSegment.ScheduledArrivalDateTime, pnrSegment.Arrival.Code, _headers.ContextValues.SessionId);

                            scheduledDepartureDateForLastSegment = pnrSegment.ScheduledDepartureDateTime;
                            scheduledDepartureGMTDateForLastSegment = pnrSegment.ScheduledDepartureDateTimeGMT;

                            if (DateTime.TryParse(pnrSegment.ScheduledDepartureDateTimeGMT, out scheduledDepartureDateTimeGMT))
                            {
                                if (DateTime.UtcNow > scheduledDepartureDateTimeGMT.AddDays(-1) && DateTime.UtcNow <= scheduledDepartureDateTimeGMT)
                                {
                                    pnr.CheckinEligible = "Y";
                                }

                            }
                        }
                        else
                        {
                            pnrSegment.ScheduledDepartureDateTime = scheduledDepartureDateForLastSegment;
                            pnrSegment.ScheduledDepartureDateTimeGMT = scheduledDepartureGMTDateForLastSegment;
                            //throw new ContinentalException("We were unable to review the latest information for this itinerary.");
                        }

                        if (segment.Seats != null)
                        {
                            pnrSegment.Seats = new List<MOBPNRSeat>();

                            foreach (UAWSMileagePlusReservation.Seat seat in segment.Seats)
                            {
                                MOBPNRSeat pnrSeat = new MOBPNRSeat();
                                pnrSeat.Number = seat.SeatAssignment;
                                pnrSeat.SeatRow = seat.SeatRow;
                                pnrSeat.SeatLetter = seat.SeatLetter;
                                pnrSeat.PassengerSHARESPosition = seat.TravelerSharesIndex;

                                pnrSeat.SeatStatus = seat.SeatStatus;
                                pnrSeat.SegmentIndex = segment.Index.ToString();
                                pnrSeat.Origin = pnrSegment.Departure.Code;
                                pnrSeat.Destination = pnrSegment.Arrival.Code;
                                pnrSeat.EddNumber = seat.EddNumber;
                                pnrSeat.EDocId = seat.EDocId;
                                double price = 0.0;
                                bool ok = Double.TryParse(seat.Price, out price);
                                if (ok)
                                {
                                    pnrSeat.Price = price;
                                }
                                pnrSeat.Currency = seat.Currency;
                                pnrSeat.ProgramCode = seat.ProgramCode;

                                pnrSegment.Seats.Add(pnrSeat);
                            }
                        }

                        if (segment.StopInfos != null)
                        {
                            pnrSegment.NumberOfStops = segment.Stops.ToString();

                            pnrSegment.Stops = new List<MOBPNRSegment>();
                            foreach (UAWSMileagePlusReservation.Segment stopSegment in segment.StopInfos)
                            {
                                MOBPNRSegment ss = new MOBPNRSegment();
                                ss.Arrival = new MOBAirport();
                                ss.Arrival.Code = stopSegment.Destination;
                                //ss.Arrival.Name = stopSegment.DecodedDestination;
                                string airportName1 = string.Empty;
                                string cityName1 = string.Empty;
                                 tupleRes = await _airportDynamoDB.GetAirportCityName(stopSegment.Destination, _headers.ContextValues.SessionId, airportName1, cityName1);
                                airportName1 = tupleRes.airportName;
                                cityName1 = tupleRes.cityName;
                                ss.Arrival.Name = airportName1;
                                ss.Arrival.City = cityName1;

                                ss.ClassOfService = stopSegment.OriginalServiceClass;

                                ss.Departure = new MOBAirport();
                                ss.Departure.Code = stopSegment.Origin;
                                //ss.Departure.Name = stopSegment.DecodedOrigin;
                                airportName1 = string.Empty;
                                cityName1 = string.Empty;
                                tupleRes = await _airportDynamoDB.GetAirportCityName(stopSegment.Origin, _headers.ContextValues.SessionId, airportName1, cityName1);
                                airportName1 = tupleRes.airportName;
                                cityName1 = tupleRes.cityName;
                                ss.Departure.Name = airportName1;
                                ss.Departure.City = cityName1;
                                ss.FlightNumber = stopSegment.FlightNumber;
                                ss.MarketingCarrier.Code = stopSegment.CarrierCode;
                                ss.MarketingCarrier.Code = stopSegment.CarrierDescription;

                                ss.CabinType = stopSegment.CabinTypeText;

                                if (stopSegment.DestinationDate != null && stopSegment.DestinationDate.Trim().Length != 0)
                                {
                                    ss.ScheduledArrivalDateTime = GeneralHelper.FormatDatetime(Convert.ToDateTime(stopSegment.DestinationDate).ToString("yyyyMMdd hh:mm tt"), languageCode);
                                    scheduledArrivalDateForLastSegment = ss.ScheduledArrivalDateTime;
                                }
                                else
                                {
                                    ss.ScheduledArrivalDateTime = scheduledArrivalDateForLastSegment;
                                    //throw new ContinentalException("We were unable to review the latest information for this itinerary.");
                                }

                                if (stopSegment.DepartDate != null && stopSegment.DepartDate.Trim().Length != 0)
                                {
                                    ss.ScheduledDepartureDateTime = TopHelper.FormatDatetime(Convert.ToDateTime(stopSegment.DepartDate).ToString("yyyyMMdd hh:mm tt"), languageCode);
                                    ss.ScheduledDepartureDateTimeGMT = await _manageResUtility.GetGMTTime(_configuration.GetValue<string>("ConnectionString - GMTConversion"), ss.ScheduledDepartureDateTime, ss.Departure.Code);
                                    scheduledDepartureDateForLastSegment = ss.ScheduledDepartureDateTime;
                                    scheduledDepartureGMTDateForLastSegment = ss.ScheduledDepartureDateTimeGMT;

                                    if (DateTime.TryParse(ss.ScheduledDepartureDateTimeGMT, out scheduledDepartureDateTimeGMT))
                                    {
                                        if (DateTime.UtcNow > scheduledDepartureDateTimeGMT.AddDays(-1) && DateTime.UtcNow <= scheduledDepartureDateTimeGMT)
                                        {
                                            pnr.CheckinEligible = "Y";
                                        }

                                    }
                                }
                                else
                                {
                                    ss.ScheduledDepartureDateTime = scheduledDepartureDateForLastSegment;
                                    ss.ScheduledDepartureDateTimeGMT = scheduledDepartureGMTDateForLastSegment;
                                    //throw new ContinentalException("We were unable to review the latest information for this itinerary.");
                                }

                                if (stopSegment.Seats != null)
                                {
                                    ss.Seats = new List<MOBPNRSeat>();

                                    foreach (UAWSMileagePlusReservation.Seat seat in stopSegment.Seats)
                                    {
                                        MOBPNRSeat pnrSeat = new MOBPNRSeat();
                                        pnrSeat.Number = seat.SeatAssignment;
                                        pnrSeat.SeatRow = seat.SeatRow;
                                        pnrSeat.SeatLetter = seat.SeatLetter;
                                        pnrSeat.PassengerSHARESPosition = seat.TravelerSharesIndex;

                                        pnrSeat.SeatStatus = seat.SeatStatus;
                                        pnrSeat.SegmentIndex = stopSegment.Index.ToString();
                                        pnrSeat.Origin = ss.Departure.Code;
                                        pnrSeat.Destination = ss.Arrival.Code;
                                        pnrSeat.EddNumber = seat.EddNumber;
                                        pnrSeat.EDocId = seat.EDocId;
                                        double price = 0.0;
                                        bool ok = Double.TryParse(seat.Price, out price);
                                        if (ok)
                                        {
                                            pnrSeat.Price = price;
                                        }
                                        pnrSeat.Currency = seat.Currency;
                                        pnrSeat.ProgramCode = seat.ProgramCode;

                                        ss.Seats.Add(pnrSeat);
                                    }
                                }

                                pnrSegment.Stops.Add(ss);
                            }
                        }

                        pnr.Segments.Add(pnrSegment);
                    }

                    if (!inactiveOrPastOrCancelled)
                    {
                        if (pnr.Segments != null && pnr.Segments.Count > 0)
                        {
                            pnrs.Add(pnr);
                        }
                    }
                    else
                    {
                        pnrs.Add(pnr);
                    }

                    if (includeTrip && pnr.Segments.Count > 0)
                    {
                        DateTime lastArrivalTime;
                        if (DateTime.TryParse(pnr.Segments[pnr.Segments.Count - 1].ScheduledArrivalDateTime, out lastArrivalTime))
                        {
                            pnr.LastTripDateArrivalDate = lastArrivalTime.ToString("MM/dd/yyyy hh:mm tt");
                            //pnr.LastTripDateArrivalDate = lastArrivalTime.ToString("MMMM").Length > 3 ? string.Format("{0:ddd., MMM. d, yyyy h:mm tt}", lastArrivalTime) : string.Format("{0:ddd., MMM d, yyyy h:mm tt}", lastArrivalTime); ;
                        }
                    }
                }
            }

            return pnrs;
        }
        private MOBSegmentResponse GetUpgradeVisibility(UAWSFlightReservation.UpgradeSegment upgradeSegment, UAWSFlightReservation.Segment segment, ref MOBPNRSegment pnrSegment)
        {
            MOBSegmentResponse upgradeVisibility = null;

            if (upgradeSegment != null)
            {
                upgradeVisibility = new MOBSegmentResponse();
                upgradeVisibility.CarrierCode = upgradeSegment.CC;
                upgradeVisibility.ClassOfService = upgradeSegment.COS;
                upgradeVisibility.DepartureDateTime = Convert.ToDateTime(upgradeSegment.DDT).ToString("MM/dd/yyyy hh:mm tt");
                upgradeVisibility.Destination = upgradeSegment.D;
                upgradeVisibility.FlightNumber = upgradeSegment.FN;
                upgradeVisibility.Origin = upgradeSegment.O;
                upgradeVisibility.PreviousSegmentActionCode = upgradeSegment.PAC;
                upgradeVisibility.SegmentActionCode = upgradeSegment.AC;
                upgradeVisibility.SegmentNumber = upgradeSegment.SN;
                upgradeVisibility.UpgradeMessageCode = GetUpgradeMessageCode(upgradeSegment.UMC);
                upgradeVisibility.UpgradeMessage = upgradeSegment.UM;
                upgradeVisibility.UpgradeType = GetUpgradeType(upgradeSegment.UT);
                if (upgradeVisibility.UpgradeType == MOBUpgradeType.PremierInstantUpgrade)
                {
                    pnrSegment.Upgradeable = true;
                    pnrSegment.UpgradeableMessageCode = "FE";
                }
                upgradeVisibility.DecodedUpgradeMessage = GetDecodedUpgradeMessage(upgradeSegment.UMC, upgradeSegment.UM, upgradeSegment.UP, upgradeSegment);
                if (upgradeSegment.UP != null && upgradeSegment.UP.Length > 0)
                {
                    upgradeVisibility.UpgradeProperties = new List<MOBUpgradePropertyKeyValue>();
                    upgradeVisibility.UpgradeProperties = GetUpgradeProperties(upgradeSegment.UP);
                }
                upgradeVisibility.UpgradeStatus = GetUpgradeStatus(upgradeSegment.US);

                if (upgradeSegment != null && upgradeSegment.WLS != null && upgradeSegment.WLS.Length > 0)
                {
                    upgradeVisibility.WaitlistSegments = new List<MOBSegmentResponse>();
                    foreach (var wls in upgradeSegment.WLS)
                    {
                        MOBSegmentResponse waitListSegment = new MOBSegmentResponse();
                        waitListSegment = GetUpgradeVisibility(wls, segment, ref pnrSegment);
                        upgradeVisibility.WaitlistSegments.Add(waitListSegment);
                    }
                }
            }

            return upgradeVisibility;
        }


        private MOBUpgradeType GetUpgradeType(UAWSFlightReservation.UpgradeVisibilityType upgradeVisibilityType)
        {
            switch (upgradeVisibilityType)
            {
                case UAWSFlightReservation.UpgradeVisibilityType.ComplimentaryPremierUpgrade:
                    return MOBUpgradeType.ComplimentaryPremierUpgrade;
                    break;
                case UAWSFlightReservation.UpgradeVisibilityType.GlobalPremierUpgrade:
                    return MOBUpgradeType.GlobalPremierUpgrade;
                    break;
                case UAWSFlightReservation.UpgradeVisibilityType.MileagePlusUpgradeAwards:
                    return MOBUpgradeType.MileagePlusUpgradeAwards;
                    break;
                case UAWSFlightReservation.UpgradeVisibilityType.None:
                    return MOBUpgradeType.None;
                    break;
                case UAWSFlightReservation.UpgradeVisibilityType.PremierCabinUpgrade:
                    return MOBUpgradeType.PremierCabinUpgrade;
                    break;
                case UAWSFlightReservation.UpgradeVisibilityType.PremierInstantUpgrade:
                    return MOBUpgradeType.PremierInstantUpgrade;
                    break;
                case UAWSFlightReservation.UpgradeVisibilityType.RegionalPremierUpgrade:
                    return MOBUpgradeType.RegionalPremierUpgrade;
                    break;
                case UAWSFlightReservation.UpgradeVisibilityType.RevenueUpgradeStandby:
                    return MOBUpgradeType.RevenueUpgradeStandby;
                    break;
                case UAWSFlightReservation.UpgradeVisibilityType.Unknown:
                    return MOBUpgradeType.Unknown;
                    break;
                case UAWSFlightReservation.UpgradeVisibilityType.Waitlisted:
                    return MOBUpgradeType.Waitlisted;
                    break;
                default:
                    return MOBUpgradeType.Unknown;
                    break;
            }
        }

        private string GetDecodedUpgradeMessage(string messageCode, string message, UAWSFlightReservation.UpgradePropertiesKeyValue[] upgradePropertiesKeyValue, UAWSFlightReservation.UpgradeSegment upgradeSegment)
        {
            string decodedMessage = string.Empty;

            switch (messageCode)
            {
                case "ConfirmedUpgrade":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageConfirmedUpgrade");
                    if (upgradeSegment.WLS != null && upgradeSegment.WLS.Length > 0 && upgradeSegment.WLS[0].AC.Equals("KL"))
                    {
                        decodedMessage = _configuration.GetValue<string>("WUPMessageConfirmedUpgrade");
                    }
                    break;
                case "InstantUpgrade":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageInstantUpgrade");
                    break;
                case "None":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageNone");
                    break;
                case "SpaceAvailableBeforeDepartureGoldAndHigher":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageSpaceAvailableBeforeDepartureGoldAndHigher");
                    break;
                case "SpaceAvailableBeforeDepartureSilver":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageSpaceAvailableBeforeDepartureSilver");
                    break;
                case "SpaceAvailableCpuToCabinOutsideWindowGoldAndHigher":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageSpaceAvailableCpuToCabinOutsideWindowGoldAndHigher");
                    string upgradeWindowsHours = GetUpgradeProperty(MOBUpgradeProperty.UpgradeWindowHours.ToString(), upgradePropertiesKeyValue);
                    decodedMessage = string.Format(decodedMessage, upgradeWindowsHours);
                    break;
                case "SpaceAvailableCpuToCabinOutsideWindowSilver":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageSpaceAvailableCpuToCabinOutsideWindowSilver");
                    break;
                case "SpaceAvailableInsideWindowGoldAndHigher":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageSpaceAvailableInsideWindowGoldAndHigher");
                    break;
                case "SpaceAvailableInsideWindowSilverAndDayOfDeparture":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageSpaceAvailableInsideWindowSilverAndDayOfDeparture");
                    break;
                case "WaitlistClassConfirmed":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageWaitlistClassConfirmed");
                    break;
                case "WaitlistedSameFlightDifferentNonUpgradeClass":
                    //if (!string.IsNullOrEmpty(message) && message.Equals("SegmentIsWaitlisted"))
                    if (!string.IsNullOrEmpty(message))
                    {
                        decodedMessage = _configuration.GetValue<string>("WUPMessageWaitlistedSameFlightDifferentNonUpgradeClass");
                        string upgradeClassOfService = GetUpgradeProperty(MOBUpgradeProperty.UpgradeClassOfService.ToString(), upgradePropertiesKeyValue);
                        decodedMessage = string.Format(decodedMessage, upgradeClassOfService);
                    }
                    break;
                case "WaitlistedSegmentOnAnotherFlight":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageWaitlistedSegmentOnAnotherFlight");
                    break;
                case "WaitlistUpgradeRequested":
                    decodedMessage = _configuration.GetValue<string>("WUPMessageWaitlistUpgradeRequested");
                    break;
            }

            return decodedMessage;
        }
        private string GetUpgradeProperty(string upgradeProperty, UAWSFlightReservation.UpgradePropertiesKeyValue[] upgradePropertiesKeyValue)
        {
            string property = string.Empty;

            if (upgradePropertiesKeyValue != null && upgradePropertiesKeyValue.Length > 0)
            {
                foreach (var keyValue in upgradePropertiesKeyValue)
                {
                    if (keyValue.Key.Equals(upgradeProperty))
                    {
                        property = keyValue.Value;
                        break;
                    }
                }
            }

            return property;
        }
        private async System.Threading.Tasks.Task SetElfContentOnPnr(MOBPNR pnr, int appId, string appVersion)
        {
            if (pnr == null || pnr.Segments == null) return;

            pnr.Segments.ForEach(SetElfOnSegment);
            pnr.isELF = HasAnyElfSegment(pnr.Segments);
            pnr.ELFLimitations = await SetElfLimitations(pnr, appId, appVersion);
        }
        private void SetElfOnSegment(MOBPNRSegment segment)
        {
            segment.IsElf = _manageResUtility.IsElfSegment(segment);
        }
        private bool HasAnyElfSegment(List<MOBPNRSegment> segments)
        {
            return segments != null && segments.Any(_manageResUtility.IsElfSegment);
        }
        private async Task<List<MOBItem>> SetElfLimitations(MOBPNR pnr, int appId, string appVersion)
        {
            var elfContentKey = string.Empty;

            if (pnr.isELF)
            {
                elfContentKey = _manageResUtility.EnableSSA(appId, appVersion) ? "SSA_ELF_MR_Limitations" : "ELF_MR_Limitations";
            }
            else if (pnr.IsCBE)
            {
                elfContentKey = "CBE_MR_Limitations";
            }
            else if (pnr.IsIBE)
            {
                if (_configuration.GetValue<bool>("EnablePBE"))
                {
                    string productCode = !string.IsNullOrEmpty(pnr.ProductCode) ? pnr.ProductCode : "IBE";
                    elfContentKey = productCode + "_MR_Limitations";
                }
                else
                {
                    elfContentKey = "IBE_MR_Limitations";
                }
            }
            else if (pnr.IsIBELite)
            {
                elfContentKey = "IBELite_MR_Limitations";
            }

            var messages = await _manageResUtility.GetCaptions(elfContentKey);

            if (messages != null && appId == 1)
            {
                try
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
                catch (Exception ex) { }                
            }

            return messages;
        }
        private bool CheckShowChangeSeat(UAWSFlightReservation.Segment[] segments)
        {
            #region
            foreach (UAWSFlightReservation.Segment segment in segments)
            {
                if (!segment.Past && !segment.CheckedIn && !segment.CheckinTriggered)
                {
                    return true;
                }
            }
            #endregion
            return false;
        }
        private List<MOBPNRSegment> ProcessWaitlistUpgradeVisibility(MOBPNR pnr, ref bool hasUpgradeVisibility, int lowestEliteLevel, int applicationId, bool hasGSPax, bool has1KPax)
        {
            List<MOBPNRSegment> segments = null;
            List<MOBPNRSegment> waitedSegments = null;

            List<MOBSegmentResponse> waitlistSegments = null;

            if (pnr != null && pnr.Segments != null && pnr.Segments.Count > 0)
            {
                foreach (MOBPNRSegment segment in pnr.Segments)
                {
                    if (segment.UpgradeVisibility != null && segment.UpgradeVisibility.WaitlistSegments != null && segment.UpgradeVisibility.WaitlistSegments.Count > 0)
                    {
                        foreach (var waitlistSegment in segment.UpgradeVisibility.WaitlistSegments)
                        {
                            if (waitlistSegments == null)
                            {
                                waitlistSegments = new List<MOBSegmentResponse>();
                            }
                            waitlistSegments.Add(waitlistSegment);
                        }
                    }
                }
            }

            if (waitlistSegments != null && waitlistSegments.Count > 0)
            {
                if (pnr != null && pnr.Segments != null && pnr.Segments.Count > 0)
                {
                    foreach (var waitlistSegment in waitlistSegments)
                    {
                        foreach (var segment in pnr.Segments)
                        {
                            if (segment.MarketingCarrier.Code.Equals(waitlistSegment.CarrierCode)
                                && segment.ClassOfService.Equals(waitlistSegment.ClassOfService)
                                && segment.FlightNumber.Equals(waitlistSegment.FlightNumber.ToString())
                                && segment.Departure.Code.Equals(waitlistSegment.Origin)
                                && segment.Arrival.Code.Equals(waitlistSegment.Destination)
                                && Convert.ToDateTime(segment.ScheduledDepartureDateTime) == Convert.ToDateTime(waitlistSegment.DepartureDateTime))
                            {
                                segment.Remove = true;

                                if (waitedSegments == null)
                                {
                                    waitedSegments = new List<MOBPNRSegment>();
                                }
                                waitedSegments.Add(segment);
                            }
                        }
                    }
                }
            }

            if (pnr.Segments != null && pnr.Segments.Count > 0)
            {
                segments = new List<MOBPNRSegment>();
                foreach (var segment in pnr.Segments)
                {
                    if (segment.Remove)
                    {
                        //Do not add segment to the return list
                    }
                    else
                    {
                        if (segment.Waitlisted)
                        {
                            if (waitedSegments == null)
                            {
                                waitedSegments = new List<MOBPNRSegment>();
                            }
                            waitedSegments.Add(segment);
                        }

                        segments.Add(segment);
                    }
                }

                if (waitedSegments != null && waitedSegments.Count > 0)
                {
                    foreach (var waitedSegment in waitedSegments)
                    {
                        foreach (var segment in segments)
                        {
                            if (segment.MarketingCarrier.Code.Equals(waitedSegment.MarketingCarrier.Code)
                                && segment.FlightNumber.Equals(waitedSegment.FlightNumber)
                                && segment.Departure.Code.Equals(waitedSegment.Departure.Code)
                                && segment.Arrival.Code.Equals(waitedSegment.Arrival.Code)
                                && Convert.ToDateTime(segment.ScheduledDepartureDateTime) == Convert.ToDateTime(waitedSegment.ScheduledDepartureDateTime))
                            {
                                if (waitedSegment.UpgradeVisibility != null && !string.IsNullOrEmpty(waitedSegment.UpgradeVisibility.SegmentActionCode) && waitedSegment.UpgradeVisibility.SegmentActionCode.IndexOf("HK|DK|KL|RR|TK") != -1)
                                {
                                    segment.ClassOfServiceDescription = waitedSegment.ClassOfServiceDescription;
                                }
                                segment.WaitedCOSDesc = waitedSegment.ClassOfServiceDescription;
                            }

                            string confirmedActionCodes = "HK|DK|KL|RR|TK";
                            if (segment.MarketingCarrier.Code.Equals(waitedSegment.MarketingCarrier.Code)
                                && segment.Departure.Code.Equals(waitedSegment.Departure.Code)
                                && segment.Arrival.Code.Equals(waitedSegment.Arrival.Code)
                                && confirmedActionCodes.IndexOf(segment.ActionCode) != -1)
                            {
                                waitedSegment.Remove = true;
                                break;
                            }
                        }
                    }

                    foreach (var segment in segments)
                    {
                        foreach (var waitedSegment in waitedSegments)
                        {
                            if (waitedSegment.Remove
                                && segment.MarketingCarrier.Code.Equals(waitedSegment.MarketingCarrier.Code)
                                && segment.FlightNumber.Equals(waitedSegment.FlightNumber)
                                && segment.Departure.Code.Equals(waitedSegment.Departure.Code)
                                && segment.Arrival.Code.Equals(waitedSegment.Arrival.Code)
                                && Convert.ToDateTime(segment.ScheduledDepartureDateTime) == Convert.ToDateTime(waitedSegment.ScheduledDepartureDateTime)
                                && segment.ClassOfService.Equals(waitedSegment.ClassOfService))
                            {
                                segment.Remove = true;
                                break;
                            }
                        }
                    }

                    List<MOBPNRSegment> tempSegments = new List<MOBPNRSegment>();
                    foreach (var segment in segments)
                    {
                        if (!segment.Remove)
                        {
                            tempSegments.Add(segment);
                        }
                    }
                    segments = tempSegments;
                }

                foreach (MOBPNRSegment segment in segments)
                {
                    if (segment.UpgradeVisibility != null)
                    {
                        string cosDesc = string.Empty;
                        if (segment.WaitedCOSDesc != string.Empty)
                        {
                            cosDesc = segment.WaitedCOSDesc;
                        }
                        else
                        {
                            cosDesc = segment.ClassOfServiceDescription;
                        }

                        segment.UpgradeVisibility.UpgradeRemark = GetUpgradeRemark(segment.UpgradeVisibility, segment.UpgradeVisibility.UpgradeMessageCode, segment.UpgradeVisibility.UpgradeProperties, segment.UpgradeVisibility.UpgradeType, cosDesc, segment, lowestEliteLevel, applicationId, hasGSPax, has1KPax);
                    }
                    else
                    {
                        string confirmedActionCodes = "HK|DK|KL|RR|TK";
                        if (confirmedActionCodes.IndexOf(segment.ActionCode) == -1)
                        {
                            if (segment.UpgradeVisibility == null)
                            {
                                segment.UpgradeVisibility = new MOBSegmentResponse();
                            }

                            hasUpgradeVisibility = true;
                            segment.UpgradeVisibility.SegmentActionCode = segment.ActionCode;
                            segment.UpgradeVisibility.UpgradeStatus = MOBUpgradeEligibilityStatus.Requested;
                            segment.UpgradeVisibility.DecodedUpgradeMessage = "This flight segment is waitlisted.";
                            segment.UpgradeVisibility.UpgradeRemark = "This flight segment is waitlisted.";
                        }
                        else
                        {
                            if (segment.ActionCode.Equals("KL"))
                            {
                                if (segment.UpgradeVisibility == null)
                                {
                                    segment.UpgradeVisibility = new MOBSegmentResponse();
                                }
                                hasUpgradeVisibility = true;
                                segment.UpgradeVisibility.SegmentActionCode = "KL";
                                segment.UpgradeVisibility.UpgradeStatus = MOBUpgradeEligibilityStatus.RequestConfirmed;
                                segment.UpgradeVisibility.DecodedUpgradeMessage = _configuration.GetValue<string>("WUPMessageConfirmedUpgrade"); ;
                                segment.UpgradeVisibility.UpgradeRemark = string.Format("Your upgrade to {0} has been confirmed. We are processing your reservation and expect it to be completed shortly. We appreciate your patience.", segment.ClassOfServiceDescription);
                            }
                        }
                    }
                }
            }

            return segments;
        }
        private string GetUpgradeRemark
           (MOBSegmentResponse upgradeVisibility, MOBMessageCode messageCode,
           List<MOBUpgradePropertyKeyValue> list, MOBUpgradeType upgradeType,
           string classOfServiceDescription, MOBPNRSegment pnrSegment, int lowestEliteLevel,
           int applicationId, bool hasGSPax, bool has1KPax, Reservation details = null)
        {
            const string remark1 = "{0} members are eligible for complimentary upgrades on select flights when traveling in Y, B, or M fare classes, subject to availability. The flights listed below have available upgrade space that may be confirmed at this time. Please indicate if you would like to be upgraded on the following flight(s).";
            const string remark2 = "{0} members are eligible for complimentary upgrades on select flights when traveling in Y or B fare classes, subject to availability. The flights listed below have available upgrade space that may be confirmed at this time. Please indicate if you would like to be upgraded on the following flight(s).";

            bool isKLSegment = false;
            if (pnrSegment != null && pnrSegment.UpgradeVisibility != null && pnrSegment.UpgradeVisibility.WaitlistSegments != null && pnrSegment.UpgradeVisibility.WaitlistSegments.Count > 0)
            {
                foreach (var waitlistSegment in pnrSegment.UpgradeVisibility.WaitlistSegments)
                {
                    if (waitlistSegment.SegmentActionCode.Equals("KL"))
                    {
                        isKLSegment = true;
                    }
                }
            }

            string remark = string.Empty;

            string upgradeCOS = string.Empty;
            string upgradeWindowsHours = string.Empty;
            switch (messageCode)
            {
                case MOBMessageCode.ConfirmedUpgrade:
                    switch (upgradeType)
                    {
                        case MOBUpgradeType.PremierInstantUpgrade:
                            remark = "Your Complimentary Instant Upgrade has been confirmed.";
                            break;
                        case MOBUpgradeType.ComplimentaryPremierUpgrade:
                            remark = "Your Complimentary Premier Upgrade has been confirmed.";
                            break;
                        case MOBUpgradeType.MileagePlusUpgradeAwards:
                            remark = string.Format("Your upgrade to {0} has been confirmed using a MileagePlus Upgrade Award.", classOfServiceDescription);
                            if (isKLSegment)
                            {
                                remark = remark + " " + "We are processing your reservation and expect it to be completed shortly.  We appreciate your patience.";
                            }
                            break;
                        case MOBUpgradeType.GlobalPremierUpgrade:
                            remark = string.Format("Your upgrade to {0} has been confirmed using a Global Premier Upgrade.", classOfServiceDescription);
                            if (isKLSegment)
                            {
                                remark = remark + " " + "We are processing your reservation and expect it to be completed shortly.  We appreciate your patience.";
                            }
                            break;
                        case MOBUpgradeType.RegionalPremierUpgrade:
                            remark = string.Format("Your upgrade to {0} has been confirmed using a Regional Premier Upgrade.", classOfServiceDescription);
                            if (isKLSegment)
                            {
                                remark = remark + " " + "We are processing your reservation and expect it to be completed shortly.  We appreciate your patience.";
                            }
                            break;
                        case MOBUpgradeType.Waitlisted:
                            remark = "Your request for {0} class has been confirmed.  We are processing your reservation and expect it to be completed shortly.  We appreciate your patience.";
                            upgradeCOS = GetUpgradeProperty(MOBUpgradeProperty.UpgradeClassOfService, list);
                            remark = string.Format(remark, upgradeCOS);
                            break;
                        default:
                            remark = string.Format("Your upgrade to {0} has been confirmed.", classOfServiceDescription);
                            break;
                    }
                    break;
                case MOBMessageCode.InstantUpgrade:
                    remark = "Premier Instant Upgrade Fare";
                    pnrSegment.Upgradeable = true;
                    pnrSegment.UpgradeableMessageCode = "FE";
                    pnrSegment.UpgradeableMessage = "Premier Instant Upgrade Fare";
                    pnrSegment.UpgradeableRemark = "You are eligible for a complimentary instant upgrade to United First on this flight.";
                    switch (lowestEliteLevel)
                    {
                        case 5:
                            pnrSegment.ComplimentaryInstantUpgradeMessage = string.Format(remark1, "Global Service");
                            break;
                        case 4:
                            if (hasGSPax && has1KPax)
                            {
                                pnrSegment.ComplimentaryInstantUpgradeMessage = string.Format(remark1, "Global Service and Premier 1K");
                            }
                            else
                            {
                                pnrSegment.ComplimentaryInstantUpgradeMessage = string.Format(remark1, "Premier 1K");
                            }
                            break;
                        case 3:
                            pnrSegment.ComplimentaryInstantUpgradeMessage = string.Format(remark2, "Premier");
                            break;
                        case 2:
                            pnrSegment.ComplimentaryInstantUpgradeMessage = string.Format(remark2, "Premier");
                            break;
                        case 1:
                            pnrSegment.ComplimentaryInstantUpgradeMessage = string.Format(remark2, "Premier");
                            break;
                    }
                    break;
                case MOBMessageCode.None:
                    break;
                case MOBMessageCode.SpaceAvailableBeforeDepartureGoldAndHigher:
                    if (applicationId == 3)
                    {
                        remark = "You are eligible for a space available complimentary upgrade to United First on this flight. When available, these upgrades are cleared automatically and in priority order on the day of departure. \n\nAfter checking in, view your status on the upgrade standby list at mobile.united.com/flightstatus. If you do not see your name on the list, please see a United representative at the airport.";
                    }
                    else
                    {
                        remark = "You are eligible for a space available complimentary upgrade to United First on this flight. When available, these upgrades are cleared automatically and in priority order on the day of departure. \n\nAfter checking in, view your status on the upgrade standby list by checking Flight Status on the United app. If you do not see your name on the list, please see a United representative at the airport.";
                    }
                    break;
                case MOBMessageCode.SpaceAvailableBeforeDepartureSilver:
                    if (applicationId == 3)
                    {
                        remark = "You are eligible for a space available complimentary upgrade to United First on this flight. When available, these upgrades are cleared automatically and in priority order on the day of departure. \n\nAfter checking in, view your status on the upgrade standby list at mobile.united.com/flightstatus. If you do not see your name on the list, please see a United representative at the airport.";
                    }
                    else
                    {
                        remark = "You are eligible for a space available complimentary upgrade to United First on this flight. When available, these upgrades are cleared automatically and in priority order on the day of departure. \n\nAfter checking in, view your status on the upgrade standby list by checking Flight Status on the United app. If you do not see your name on the list, please see a United representative at the airport.";
                    }
                    break;
                case MOBMessageCode.SpaceAvailableCpuToCabinOutsideWindowGoldAndHigher:
                    if (applicationId == 3)
                    {
                        remark = "You are eligible for a space available complimentary upgrade to {0} on this flight, starting {1} hours before departure. When available, these upgrades are cleared automatically and in priority order. \n\nAfter checking in, view your status on the upgrade standby list at mobile.united.com/flightstatus. If you do not see your name on the list, please see a United representative at the airport.";
                    }
                    else
                    {
                        remark = "You are eligible for a space available complimentary upgrade to {0} on this flight, starting {1} hours before departure. When available, these upgrades are cleared automatically and in priority order. \n\nAfter checking in, view your status on the upgrade standby list by checking Flight Status on the United app. If you do not see your name on the list, please see a United representative at the airport.";
                    }
                    upgradeWindowsHours = GetUpgradeProperty(MOBUpgradeProperty.UpgradeWindowHours, list);
                    if (upgradeVisibility.ClassOfService.Length < 3)
                    {
                        upgradeVisibility.ClassOfService = "United First";
                    }
                    remark = string.Format(remark, upgradeVisibility.ClassOfService, upgradeWindowsHours);
                    break;
                case MOBMessageCode.SpaceAvailableCpuToCabinOutsideWindowSilver:
                    if (applicationId == 3)
                    {
                        remark = "You are eligible for a space available complimentary upgrade to United First on this flight. When available, these upgrades are cleared automatically and in priority order on the day of departure. \n\nAfter checking in, view your status on the upgrade standby list at mobile.united.com/flightstatus. If you do not see your name on the list, please see a United representative at the airport.";
                    }
                    else
                    {
                        remark = "You are eligible for a space available complimentary upgrade to United First on this flight. When available, these upgrades are cleared automatically and in priority order on the day of departure. \n\nAfter checking in, view your status on the upgrade standby list by checking Flight Status on the United app. If you do not see your name on the list, please see a United representative at the airport.";
                    }
                    break;
                case MOBMessageCode.SpaceAvailableInsideWindowGoldAndHigher:
                    if (classOfServiceDescription.Equals("United Economy"))
                    {
                        classOfServiceDescription = "United First";
                    }

                    if (applicationId == 3)
                    {
                        remark = string.Format("Your complimentary space-available upgrade to {0} for this flight has been automatically requested. Upgrade requests will remain active up to flight departure time and will be cleared in priority order. \n\nAfter checking in, view your status on the upgrade standby list at mobile.united.com/flightstatus. If you do not see your name on the list, please see a United representative at the airport.", classOfServiceDescription);
                    }
                    else
                    {
                        remark = string.Format("Your complimentary space-available upgrade to {0} for this flight has been automatically requested. Upgrade requests will remain active up to flight departure time and will be cleared in priority order. \n\nAfter checking in, view your status on the upgrade standby list by checking Flight Status on the United app. If you do not see your name on the list, please see a United representative at the airport.", classOfServiceDescription);
                    }
                    break;
                case MOBMessageCode.SpaceAvailableInsideWindowSilverAndDayOfDeparture:
                    if (classOfServiceDescription.Equals("United Economy"))
                    {
                        classOfServiceDescription = "United First";
                    }
                    if (applicationId == 3)
                    {
                        remark = string.Format("Your complimentary space-available upgrade to {0} for this flight has been automatically requested. Upgrade requests will remain active up to flight departure time and will be cleared in priority order. \n\nAfter checking in, view your status on the upgrade standby list at mobile.united.com/flightstatus. If you do not see your name on the list, please see a United representative at the airport.", classOfServiceDescription);
                    }
                    else
                    {
                        remark = string.Format("Your complimentary space-available upgrade to {0} for this flight has been automatically requested. Upgrade requests will remain active up to flight departure time and will be cleared in priority order. \n\nAfter checking in, view your status on the upgrade standby list by checking Flight Status on the United app.. If you do not see your name on the list, please see a United representative at the airport.", classOfServiceDescription);
                    }
                    break;
                case MOBMessageCode.WaitlistClassConfirmed:
                    //string upgradeClassOfService = GetUpgradeProperty(Continental.MOBUpgradeProperty.UpgradeClassOfService.ToString(), upgradePropertiesKeyValue);
                    //decodedMessage = string.Format(decodedMessage, upgradeClassOfService);
                    remark = "You have been confirmed in {0} class for this flight.  We are processing your reservation and expect it to be completed shortly.  We appreciate your patience.";
                    remark = string.Format(remark, upgradeVisibility.ClassOfService);
                    break;
                case MOBMessageCode.WaitlistedSameFlightDifferentNonUpgradeClass:
                    if (upgradeVisibility.UpgradeStatus == MOBUpgradeEligibilityStatus.Upgraded)
                    {
                        //Do nothing for now.
                    }
                    else
                    {
                        remark = "You are confirmed in {0} class on this flight and have requested a change to {1} class. \n\nYou will be automatically confirmed on this flight if space in your requested class ({2}) becomes available.";
                        upgradeCOS = GetUpgradeProperty(MOBUpgradeProperty.UpgradeClassOfService, list);
                        remark = string.Format(remark, upgradeVisibility.ClassOfService, upgradeCOS, upgradeCOS);
                    }
                    break;
                case MOBMessageCode.WaitlistedSegmentOnAnotherFlight:
                    remark = "You will be automatically confirmed on this flight if space in your requested class ({0}) becomes available.";
                    upgradeCOS = GetUpgradeProperty(MOBUpgradeProperty.UpgradeClassOfService, list);
                    remark = string.Format(remark, upgradeVisibility.ClassOfService, upgradeCOS, upgradeCOS);
                    break;
                case MOBMessageCode.WaitlistUpgradeRequested:
                    switch (upgradeType)
                    {
                        case MOBUpgradeType.MileagePlusUpgradeAwards:
                            remark = string.Format("Upgrade to {0} for this flight requested using a MileagePlus Upgrade Award.", classOfServiceDescription);
                            break;
                        case MOBUpgradeType.GlobalPremierUpgrade:
                            remark = string.Format("Upgrade to {0} for this flight requested using a Global Premier Upgrade.", classOfServiceDescription);
                            break;

                        case MOBUpgradeType.RegionalPremierUpgrade:
                            remark = string.Format("Upgrade to {0} for this flight requested using a Regional Premier Upgrade.", classOfServiceDescription);
                            break;
                        case MOBUpgradeType.Waitlisted: //Changes for the bug 200165 - November 24,j.Srinivas
                            if (!string.IsNullOrEmpty(classOfServiceDescription) && (upgradeVisibility.PreviousSegmentActionCode.Equals("PD") || upgradeVisibility.PreviousSegmentActionCode.Equals("PB") || upgradeVisibility.PreviousSegmentActionCode.Equals("PC")))
                            {
                                remark = string.Format("Upgrade to {0} Requested for this flight", classOfServiceDescription);
                            }
                            break;
                            //case MOBUpgradeType.PlusPointsUpgrade:
                            //    {
                            //        remark = GetUpgradePPToolTipMessage(pnrSegment, details, type : string.Empty);
                            //    }
                            //    break;
                    }
                    break;
            }

            if (_configuration.GetValue<bool>("EnableUpgradePlusPointToolTipMessage"))
            {
                if (pnrSegment.IsPlusPointSegment)
                {
                    remark = GetUpgradePPToolTipMessage(pnrSegment, details, type: string.Empty);
                }
            }

            return remark;
        }
        private string GetUpgradeProperty(MOBUpgradeProperty upgradeProperty, List<MOBUpgradePropertyKeyValue> list)
        {
            string property = string.Empty;

            if (list != null && list.Count > 0)
            {
                foreach (var keyValue in list)
                {
                    if (keyValue.Key.Equals(upgradeProperty))
                    {
                        property = keyValue.Value;
                        break;
                    }
                }
            }

            return property;
        }
        private string GetUpgradePPToolTipMessage(MOBPNRSegment segment, Reservation details, string type = "")
        {
            var prices = details.Prices;
            StringBuilder sb = new StringBuilder();

            try
            {
                var pluspointprice = GetPlusPointPriceBasedOnType(prices);

                if (segment == null || pluspointprice == null || !pluspointprice.Any()) return string.Empty;

                //With current segment Trip # to get matching pricing object
                int tripnumber;
                int.TryParse(segment.TripNumber, out tripnumber);
                var selectedPrice = pluspointprice.Where(x => ((x != null) && (x.PriceFlightSegments != null) &&
                (x.PriceFlightSegments.Any()) && (x.PriceFlightSegments.Any(y => (y.LOFNumber == tripnumber))))).FirstOrDefault();

                if (selectedPrice == null || selectedPrice.PriceFlightSegments == null || !selectedPrice.PriceFlightSegments.Any()) return string.Empty;

                var selectedSegments = selectedPrice.PriceFlightSegments.Where(x => x.LOFNumber == tripnumber).Select(y => y);

                if (selectedSegments == null || !selectedSegments.Any()) return string.Empty;

                //In pricing object get all points for the segments
                selectedSegments.ToList().ForEach(x =>
                {
                    string departureairport = (x.DepartureAirport != null) ? x.DepartureAirport.IATACode : string.Empty;
                    string arrivalairport = (x.ArrivalAirport != null) ? x.ArrivalAirport.IATACode : string.Empty;
                    string points = string.Empty;
                    string newcabin = string.Empty;

                    string origcabin = segment.CabinType;

                    if (x.BookingClasses != null && x.BookingClasses.Any())
                    {
                        var newcabinobj = x.BookingClasses.FirstOrDefault(y => ((y.Cabin != null) && (!string.IsNullOrEmpty(y.Cabin.Description))));
                        newcabin = (newcabinobj != null && newcabinobj.Cabin != null) ? newcabinobj.Cabin.Description : string.Empty;
                    }

                    if (x.BasePrice != null && x.BasePrice.Any())
                    {
                        var pluspointpricetypes = _manageResUtility.GetListFrmPipelineSeptdConfigString("UpgradeCabinPriceTypes");
                        var pointsobj = x.BasePrice.FirstOrDefault(y =>
                        ((y != null) && (y.Currency != null) && (pluspointpricetypes.Contains(y.Currency.Code))));
                        points = (pointsobj != null) ? Convert.ToString(pointsobj.Amount) : string.Empty;
                    }

                    int pointval;
                    int.TryParse(points, out pointval);
                    points = (details.Travelers.Count() > 1) ? Convert.ToString(details.Travelers.Count() * pointval) : points;

                    sb.AppendLine(string.Format("{0}-{1} - {2} ({3} points)"
                        , departureairport, arrivalairport, newcabin.Replace("-", "to"), points));
                });
            }
            catch { return string.Empty; }

            return SetUpgradeCabinToolTipText(sb.ToString());
        }

        public IEnumerable<Price> GetPlusPointPriceBasedOnType(Collection<Price> prices)
        {
            try
            {
                var pluspointpricetypes
                    = _manageResUtility.GetListFrmPipelineSeptdConfigString("UpgradeCabinPriceTypes");
                IEnumerable<Price> pluspointprice;
                if (pluspointpricetypes != null && pluspointpricetypes.Any())
                    pluspointprice = prices.Where
                        (ppp => (ppp != null) && (ppp.Type != null) && (pluspointpricetypes.Contains(ppp.Type.Value)));
                else
                    pluspointprice = null;

                return pluspointprice;
            }
            catch { return null; }

        }

        private string SetUpgradeCabinToolTipText(string bodytext)
        {
            if (!string.IsNullOrEmpty(bodytext))
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine(string.Format("Points {0}", Environment.NewLine));
                sb.AppendLine(bodytext);
                sb.AppendLine(string.Format("{0}{1}", Environment.NewLine, _configuration.GetValue<string>("UpgradeCabinToolTipMsg")));
                return sb.ToString();
            }
            return string.Empty;
        }
        private List<MOBPNRSegment> CheckUpgradeEligibility(string sessionId, int eliteLevel, List<MOBPNRSegment> segments, bool hasGSPax, bool has1KPax)
        {
            const string remark1 = "{0} members are eligible for complimentary upgrades on select flights when traveling in Y, B, or M fare classes, subject to availability. The flights listed below have available upgrade space that may be confirmed at this time. Please indicate if you would like to be upgraded on the following flight(s).";
            const string remark2 = "{0} members are eligible for complimentary upgrades on select flights when traveling in Y or B fare classes, subject to availability. The flights listed below have available upgrade space that may be confirmed at this time. Please indicate if you would like to be upgraded on the following flight(s).";

            List<MOBPNRSegment> segmentList = null;

            if (!string.IsNullOrEmpty(sessionId) && segments != null && segments.Count > 0)
            {
                //TODO: Soap call not working
                //UAWSShopping.ShoppingDataSoapClient soapClient = new UAWSShopping.ShoppingDataSoapClient();
                //UAWSShopping.wsShoppingResponse response = soapClient.CheckUpgradeEligibility(sessionId, eliteLevel, _configuration.GetValue<string>("AccessCode - Shopping"), "2.0");

                //if (response != null && response.Flight != null && response.Flight.Segments != null && response.Flight.Segments.Length > 0)
                {
                    //segmentList = new List<MOBPNRSegment>();
                    //foreach (var segment in response.Flight.Segments)
                    //{
                    //    foreach (var s in segments)
                    //    {
                    //        if (s.MarketingCarrier.Code.Equals(segment.MarketingCarrier)

                    //            && s.FlightNumber.Equals(segment.FlightNumber)
                    //            && s.Departure.Code.Equals(segment.Origin)
                    //            && s.Arrival.Code.Equals(segment.Destination)
                    //            && Convert.ToDateTime(s.ScheduledDepartureDateTime) == Convert.ToDateTime(segment.DepartDate))
                    //        {
                    //            s.Upgradeable = segment.Upgradeable;
                    //            s.UpgradeableMessageCode = segment.UpgradeMessageCode;

                    //            if (segment.Upgradeable && !string.IsNullOrEmpty(segment.UpgradeMessageCode))
                    //            {
                    //                switch (segment.UpgradeMessageCode)
                    //                {
                    //                    case "FE":
                    //                        s.UpgradeableMessage = "Premier Instant Upgrade Fare";
                    //                        s.UpgradeableRemark = "You are eligible for a complimentary instant upgrade to United First on this flight.";
                    //                        switch (eliteLevel)
                    //                        {
                    //                            case 5:
                    //                                s.ComplimentaryInstantUpgradeMessage = string.Format(remark1, "Global Service");
                    //                                break;
                    //                            case 4:
                    //                                if (hasGSPax && has1KPax)
                    //                                {
                    //                                    s.ComplimentaryInstantUpgradeMessage = string.Format(remark1, "Global Service and Premier 1K");
                    //                                }
                    //                                else
                    //                                {
                    //                                    s.ComplimentaryInstantUpgradeMessage = string.Format(remark1, "Premier 1K");
                    //                                }
                    //                                break;
                    //                            case 3:
                    //                                s.ComplimentaryInstantUpgradeMessage = string.Format(remark2, "Premier");
                    //                                break;
                    //                            case 2:
                    //                                s.ComplimentaryInstantUpgradeMessage = string.Format(remark2, "Premier");
                    //                                break;
                    //                            case 1:
                    //                                s.ComplimentaryInstantUpgradeMessage = string.Format(remark2, "Premier");
                    //                                break;
                    //                        }
                    //                        break;
                    //                }
                    //            }

                    //            segmentList.Add(s);
                    //        }
                    //    }
                    //}
                }
            }
            else
            {
                segmentList = segments;
            }

            return segmentList;
        }
        private async Task<(MOBPNRSegment, bool hasUpgradeVisibility)> GetStopSegmentDetails(string languageCode, ReservationFlightSegment segment, MOBPNRSegment pnrSegment, bool hasUpgradeVisibility)
        {
            if (segment.FlightSegment != null && segment.FlightSegment.NumberofStops > 0 && segment.Legs != null)
            {
                pnrSegment.NumberOfStops = segment.FlightSegment.NumberofStops.ToString();
                pnrSegment.Stops = new List<MOBPNRSegment>();
                foreach (United.Service.Presentation.SegmentModel.PersonFlightSegment stopSegment in segment.Legs)
                {
                    #region

                    MOBPNRSegment ss = new MOBPNRSegment();
                    ss.ActionCode = segment.FlightSegment.FlightSegmentType;
                    //ss.ActualMileage = stopSegment; // CSl Team need to give
                    ss.ActualMileage = segment.FlightSegment.Distance.ToString();
                    ss.Arrival = new MOBAirport();

                    //ss.Arrival.Name = stopSegment.DecodedDestination;
                    string ssAirportName = string.Empty;
                    string cityName1 = string.Empty;
                    if (stopSegment.ArrivalAirport != null)
                    {
                        ss.Arrival.Code = stopSegment.ArrivalAirport.IATACode;
                        var tupleRes= await _airportDynamoDB.GetAirportCityName(stopSegment.ArrivalAirport.IATACode, _headers.ContextValues.SessionId,  ssAirportName,  cityName1);
                        ssAirportName = tupleRes.airportName;
                        cityName1 = tupleRes.cityName;
                    }
                    ss.Arrival.Name = ssAirportName;
                    ss.Arrival.City = cityName1;

                    ss.Aircraft = new MOBAircraft();
                    if (stopSegment.Equipment != null)
                    {
                        ss.Aircraft.Code = stopSegment.Equipment.Model.Fleet; // Confirm value
                        ss.Aircraft.LongName = stopSegment.Equipment.Model.Description; //Confirm value
                    }
                    if (segment.BookingClass != null)
                    {
                        ss.ClassOfService = segment.BookingClass.Code;
                        ss.ClassOfServiceDescription = segment.BookingClass.Cabin.Name;
                    }
                    ss.Departure = new MOBAirport();
                    //ss.Departure.Name = stopSegment.DecodedOrigin;
                    ssAirportName = string.Empty;
                    cityName1 = string.Empty;
                    if (stopSegment.DepartureAirport != null)
                    {
                        ss.Departure.Code = stopSegment.DepartureAirport.IATACode;
                       var tupleRes=await  _airportDynamoDB.GetAirportCityName(stopSegment.DepartureAirport.IATACode, _headers.ContextValues.SessionId,  ssAirportName,  cityName1);
                        ssAirportName = tupleRes.airportName;
                        cityName1 = tupleRes.cityName;
                    }
                    ss.Departure.Name = ssAirportName;
                    ss.Departure.City = cityName1;

                    //ss.EMP = string.Format("{0}%", segment.EMP); //Client doent neet this properties 

                    if (string.IsNullOrEmpty(stopSegment.FlightNumber))
                    {
                        stopSegment.FlightNumber = segment.FlightSegment.FlightNumber;
                    }

                    ss.FlightNumber = stopSegment.FlightNumber;
                    ss.GroundTime = stopSegment.GroundTime.ToString();
                    ss.MarketingCarrier = new MOBAirline();

                    if (stopSegment.MarketedFlightSegment != null && stopSegment.MarketedFlightSegment.Count > 0)
                    {
                        ss.MarketingCarrier.Code = stopSegment.MarketedFlightSegment[0].MarketingAirlineCode;
                        ss.MarketingCarrier.Name = stopSegment.MarketedFlightSegment[0].Description;
                        ss.MarketingCarrier.FlightNumber = stopSegment.FlightNumber;
                    }
                    else if (_configuration.GetValue<bool>("EnableCSL30ManageResSelectSeatMap") && stopSegment.MarketedFlightSegment == null && segment.FlightSegment.MarketedFlightSegment != null && segment.FlightSegment.MarketedFlightSegment.Count > 0)
                    {
                        ss.MarketingCarrier.Code = segment.FlightSegment.MarketedFlightSegment[0].MarketingAirlineCode;
                        ss.MarketingCarrier.Name = segment.FlightSegment.MarketedFlightSegment[0].Description;
                        ss.MarketingCarrier.FlightNumber = segment.FlightSegment.FlightNumber;
                    }

                    ss.CabinType = segment.BookingClass.Cabin.Name;

                    ss.OperationoperatingCarrier = new MOBAirline();
                    ss.OperationoperatingCarrier.Code = stopSegment.OperatingAirlineCode;
                    ss.OperationoperatingCarrier.FlightNumber = stopSegment.FlightNumber;
                    ss.Meal = GetCharactersticValue(stopSegment.Characteristic, "MealDescription");

                    // CSL team - Property "Journey Duration" is also available in flightsegment section
                    ss.FlightTime = GetCharactersticValue(stopSegment.Characteristic, "Journey Duration");

                    //ss.MileagePlusMileage = stopSegment.OnePassMileage.ToString(); //per client not required 
                    if (ss.OperationoperatingCarrier.Code != null && ss.OperationoperatingCarrier.Code.Equals("UA"))
                    {
                        ss.OperationoperatingCarrier.FlightNumber = GetCharactersticValue(segment.Characteristic, "DEI50");
                    }

                    //Kirti per csl response.Detail.FlightSegments[0].FlightSegment.OperatingAirlineName
                    ss.OperationoperatingCarrier.Name = segment.FlightSegment.OperatingAirlineName;

                    if (ss.OperationoperatingCarrier.Name.ToLower().Equals("continental"))
                    {
                        ss.OperationoperatingCarrier.Name = "Continental Airlines";
                    }
                    else if (ss.OperationoperatingCarrier.Name.ToLower().Equals("united"))
                    {
                        ss.OperationoperatingCarrier.Name = "United Airlines";
                    }

                    ss.CodeshareCarrier = new MOBAirline();

                    ss.CodeshareCarrier.Code = stopSegment.OperatingAirlineCode;
                    ss.CodeshareFlightNumber = stopSegment.FlightNumber;
                    ss.CodeshareCarrier.FlightNumber = stopSegment.FlightNumber;

                    ss.ScheduledArrivalDateTime = GeneralHelper.FormatDatetime(Convert.ToDateTime(stopSegment.ArrivalDateTime).ToString("yyyyMMdd hh:mm tt"), languageCode);
                    ss.ScheduledDepartureDateTime = GeneralHelper.FormatDatetime(Convert.ToDateTime(stopSegment.DepartureDateTime).ToString("yyyyMMdd hh:mm tt"), languageCode);

                    ss.ScheduledArrivalDateTimeGMT = await _manageResUtility.GetGMTTime(stopSegment.ArrivalDateTime, ss.Arrival.Code, _headers.ContextValues.SessionId);
                    ss.ScheduledDepartureDateTimeGMT = await _manageResUtility.GetGMTTime(stopSegment.DepartureDateTime, ss.Departure.Code, _headers.ContextValues.SessionId);


                    //ss.TotalMileagePlusMileage = stopSegment.TotalOnePassMileage.ToString(); // CSL Pending - todo

                    ss.TotalTravelTime = GetCharactersticValue(stopSegment.Characteristic, "Journey Duration");


                    //per csl need to null check UpgradeMessage, PreviousSegmentActionCode, UpgradeMessageCode 
                    if (stopSegment.UpgradeVisibilityType != UpgradeVisibilityType.None && !string.IsNullOrWhiteSpace(stopSegment.UpgradeMessage) && !string.IsNullOrWhiteSpace(stopSegment.PreviousSegmentActionCode) && !string.IsNullOrWhiteSpace(stopSegment.UpgradeMessageCode))
                    {
                        ss.UpgradeVisibility = GetUpgradeVisibilityCSLWithWaitlist(segment.FlightSegment, ref ss);

                        if (Convert.ToBoolean(GetCharactersticValue(segment.FlightSegment.Characteristic, "Waitlisted")))
                        {
                            ss.UpgradeVisibility.WaitlistSegments = new List<MOBSegmentResponse>();

                            MOBSegmentResponse waitListSegment = new MOBSegmentResponse();
                            waitListSegment = GetUpgradeVisibilityCSLWithWaitlist(segment.FlightSegment, ref pnrSegment);
                            ss.UpgradeVisibility.WaitlistSegments.Add(waitListSegment);
                        }
                        if (ss.UpgradeVisibility != null && (ss.UpgradeVisibility.UpgradeStatus == MOBUpgradeEligibilityStatus.Requested || ss.UpgradeVisibility.UpgradeStatus == MOBUpgradeEligibilityStatus.Qualified) && (ss.UpgradeVisibility.UpgradeType == MOBUpgradeType.ComplimentaryPremierUpgrade || ss.UpgradeVisibility.UpgradeType == MOBUpgradeType.GlobalPremierUpgrade || ss.UpgradeVisibility.UpgradeType == MOBUpgradeType.MileagePlusUpgradeAwards || ss.UpgradeVisibility.UpgradeType == MOBUpgradeType.RegionalPremierUpgrade))
                        {
                            pnrSegment.UpgradeEligible = true;
                        }
                        if (ss.UpgradeVisibility != null && ss.UpgradeVisibility.UpgradeType != MOBUpgradeType.PremierInstantUpgrade)
                        {
                            hasUpgradeVisibility = true;
                        }
                    }
                    if (stopSegment.CurrentSeats != null)
                    {
                        ss.Seats = new List<MOBPNRSeat>();
                        foreach (var seats in stopSegment.CurrentSeats)
                        {
                            MOBPNRSeat pnrSeat = new MOBPNRSeat();
                            //per csl response.Detail.FlightSegments[0].CurrentSeats[0].Seat.Identifier
                            pnrSeat.Number = seats.Seat.Identifier;
                            // pnrSeat.SeatRow = seats.Seat.Identifier;
                            //pnrSeat.SeatLetter = seats.Seat.Identifier;
                            pnrSeat.PassengerSHARESPosition = seats.ReservationNameIndex;

                            // CSL change
                            pnrSeat.SegmentIndex = segment.SegmentNumber.ToString();
                            pnrSeat.Origin = ss.Departure.Code;
                            pnrSeat.Destination = ss.Arrival.Code;
                            pnrSeat.EddNumber = seats.EDocID;
                            //pnrSeat.EDocId = seat.EDocId; // not required from csl
                            double price = 0.0;
                            //kirti per csl mapped the price
                            if (segment.Seat != null)
                            {
                                bool ok = Double.TryParse(GetCharactersticValue(segment.Seat.Characteristics, "Price"), out price);

                                if (ok)
                                {
                                    pnrSeat.Price = price;
                                }


                                pnrSeat.Currency = GetCharactersticValue(segment.Seat.Characteristics, "Currency");
                                pnrSeat.ProgramCode = GetCharactersticValue(segment.Seat.Characteristics, "ProgramCode");
                            }
                            ss.Seats.Add(pnrSeat);
                        }
                    }

                    pnrSegment.Stops.Add(ss);

                    #endregion
                }
            }
            return (pnrSegment, hasUpgradeVisibility);
        }
        private string GetCharactersticValueByDescription(System.Collections.ObjectModel.Collection<Service.Presentation.CommonModel.Characteristic> Characteristic, string desc)
        {
            string value = string.Empty;

            if (Characteristic != null && Characteristic.Count > 0)
            {
                try
                {
                    if (Characteristic.ToList().Exists(c => c.Description == desc))
                    {
                        value = Characteristic.ToList().First(c => c.Description == desc).Value;
                    }
                }
                catch (Exception ex)
                {
                    value = string.Empty;
                }
            }
            return value;
        }
        private void GetSpecialNeedsAdvisoryMessage(int applicationId, string appVersion, ref MOBPNR pnr)
        {
            if (GeneralHelper.IsApplicationVersionGreater
                            (applicationId, appVersion, "AndroidEnableMgnResUpdateSpecialNeeds", "iPhoneEnableMgnResUpdateSpecialNeeds", string.Empty, string.Empty, true, _configuration))
            {
                try
                {
                    if (pnr.Segments != null && pnr.Segments.Any())
                    {
                        var getAllCarrierNames = pnr.Segments.Where(x => (x.OperationoperatingCarrier != null))
                            .Where(n => ((!string.Equals(n.OperationoperatingCarrier.Code, "UA", StringComparison.OrdinalIgnoreCase))
                                && (n.OperationoperatingCarrier.Name.IndexOf("United Express", StringComparison.OrdinalIgnoreCase) == -1)))
                            .GroupBy(y => y.OperationoperatingCarrier.Code).Select(z => z.First())
                            .Select(l => l.OperationoperatingCarrier.Name).ToList();

                        string strCarrierNames = _manageResUtility.ConvertListToString(getAllCarrierNames);

                        if (!string.IsNullOrEmpty(strCarrierNames))
                        {
                            pnr.MealAccommodationAdvisory = string.Format(_configuration.GetValue<string>("SSR_OA_MessageNew"), strCarrierNames);
                            pnr.MealAccommodationAdvisoryHeader = "Action required";
                        }
                    }
                }
                catch (Exception ex) { }
            }
        }

        private bool CompareClass(ReservationFlightSegment f, string bookingClass)
        {
            return bookingClass != null &&
                f != null && f.FlightSegment != null && f.FlightSegment.BookingClasses != null && f.FlightSegment.BookingClasses.Any() &&
                f.FlightSegment.BookingClasses[0] != null &&
                f.FlightSegment.BookingClasses[0].Cabin != null &&
                f.FlightSegment.BookingClasses[0].Cabin.Description != null &&
                f.FlightSegment.BookingClasses[0].Cabin.Description.ToUpper().Trim() == bookingClass.ToUpper().Trim();
        }

        private string GetProductCategory(Reservation detail)
        {

            if (detail == null || detail.FlightSegments == null || !detail.FlightSegments.Any())
                return string.Empty;

            if (IsBasicEconomyCatagory(detail.Type))
            {
                return "Basic Economy";
            }
            if (detail.FlightSegments.Any(f => CompareUPPClass(f, "United Premium Plus")))
            {
                return "United Premium Plus";
            }
            if (detail.FlightSegments.Any(f => CompareClass(f, "BUSINESS")))
            {
                return "Business";
            }

            if (detail.FlightSegments.Any(f => CompareClass(f, "FIRST")))
            {
                return "First";
            }

            return "Economy";
        }

        private string GetMarketType(Collection<ReservationFlightSegment> flightSegments)
        {
            if (flightSegments == null || !flightSegments.Any())
                return string.Empty;

            var isInternational = flightSegments.Any(f => f.FlightSegment != null && Convert.ToBoolean(f.FlightSegment.IsInternational));
            return isInternational ? "International" : "Domestic";
        }

        private string GetProductCode(Collection<Genre> type)
        {
            if (type == null || !type.Any()) return string.Empty;


            if (_configuration.GetValue<bool>("EnablePBE"))
            {
                var IBEProductCodes = _configuration.GetValue<string>("IBEFullShoppingProductCodes").Split(',');
                var productCode = type?.FirstOrDefault(t => t != null && !string.IsNullOrEmpty(t.Key) && IBEProductCodes.Contains(t.Key))?.Value;
                if (!string.IsNullOrEmpty(productCode))
                    return productCode;

                var productType = type.FirstOrDefault(t => t.Key.Equals("ELF", StringComparison.InvariantCultureIgnoreCase));
                return productType != null ? productType.Value : string.Empty;
            }
            else
            {
                var productType = type.FirstOrDefault(t => t.Key.Equals("IBE", StringComparison.InvariantCultureIgnoreCase));
                if (productType != null) return productType.Value;

                productType = type.FirstOrDefault(t => t.Key.Equals("ELF", StringComparison.InvariantCultureIgnoreCase));
                return productType != null ? productType.Value : string.Empty;
            }
        }

        private bool HasIBeLiteSegments(Collection<Genre> type)
        {
            return _configuration.GetValue<bool>("EnableiBELite") && type != null && type.Any(IsIBELitetype);
        }

        private bool HasIBeSegments(Collection<Genre> type)
        {
            return _configuration.GetValue<bool>("EnableIBE") && type != null && type.Any(IsIBEtype);
        }
        private bool IsIBEtype(Genre type)
        {
            if (_configuration.GetValue<bool>("EnablePBE"))
            {
                var IBEFullProductCodes = _configuration.GetValue<string>("IBEFullShoppingProductCodes").Split(',');
                return type != null
                      && type.Key != null && IBEFullProductCodes.Contains(type.Key.Trim().ToUpper())
                      && _manageResUtility.IsIBEFullFare(type.Value);
            }
            else
            {
                return type != null
                       && type.Key != null && type.Key.Trim().ToUpper() == "IBE"
                       && _manageResUtility.IsIBEFullFare(type.Value);
            }
        }
        private bool IsPNRHasFFCRIssued(MOBPNR pnr, ReservationDetail response, int applicationId, string appVersion)
        {
            if (!_manageResUtility.IncludeReshopFFCResidual(applicationId, appVersion)) return false;

            if (response?.Detail?.BookingIndicators != null && response.Detail.BookingIndicators.IsFFCR)
            {
                return true;
            }
            return false;
        }
        private async Task<MOBUmnr> GetUMNRMsgInformation(Reservation reservationdetail)
        {
            MOBUmnr umnr = new MOBUmnr();
            umnr.Messages = new List<MOBItem>();
            try
            {
                var umnrMessages = await _documentLibraryDynamoDB.GetNewLegalDocumentsForTitles(new List<string> { "UMNR_Advisory_Content" }, _headers.ContextValues.SessionId).ConfigureAwait(false);
                if (umnrMessages != null && umnrMessages.Any())
                {
                    foreach (MOBLegalDocument doc in umnrMessages)
                    {
                        umnr.Messages.Add(new MOBItem() { Id = doc.Title, CurrentValue = doc.LegalDocument });
                    }
                }
                if (umnr.Messages != null && umnr.Messages.Any())
                {
                    SetPickupDropoffNames(reservationdetail.UMNRInformation, ref umnr);
                    SetEmergencyContact(reservationdetail.Services, ref umnr);
                }
            }
            catch (Exception ex) { return null; }
            return umnr;
        }
        private void SetPickupDropoffNames(UnaccompaniedMinorInformation umnrinfo, ref MOBUmnr umnr)
        {
            List<string> pickupNamesList = new List<string>();
            List<string> dropoffNamesList = new List<string>();
            string pickupNames = string.Empty;
            string dropoffNames = string.Empty;

            try
            {
                if (umnrinfo != null && umnrinfo.AdultContacts != null && umnrinfo.AdultContacts.Any())
                {
                    umnrinfo.AdultContacts.ToList().ForEach(
                        contact =>
                        {
                            if (contact.DropOffPickUpProfile != null && contact.DropOffPickUpProfile.Any())
                            {
                                contact.DropOffPickUpProfile.ToList().ForEach(
                                    profile =>
                                    {
                                        string firstName = (!string.IsNullOrEmpty(contact.FirstName) ? contact.FirstName.Trim() : string.Empty);
                                        string lastName = (!string.IsNullOrEmpty(contact.LastName) ? contact.LastName.Trim() : string.Empty);
                                        string fullName = string.Concat(firstName + " " + lastName);

                                        if (!string.IsNullOrEmpty(firstName) || !string.IsNullOrEmpty(lastName))
                                        {
                                            if (!string.IsNullOrEmpty(profile.WillPickUpAtDestination)
                                            && (string.Equals(profile.WillPickUpAtDestination, "true", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                pickupNamesList.Add(fullName.Trim());
                                            }

                                            if (!string.IsNullOrEmpty(profile.WillDropOffAtOrigin)
                                            && (string.Equals(profile.WillDropOffAtOrigin, "true", StringComparison.OrdinalIgnoreCase)))
                                            {
                                                dropoffNamesList.Add(fullName.Trim());
                                            }
                                        }
                                    });
                            }
                        });
                }

                if (pickupNamesList.Any())
                    umnr.Messages.Add(new MOBItem { Id = "umnr_pickup_info", CurrentValue = pickupNamesList.Distinct().Join(",") });
                else
                    umnr.Messages.Add(new MOBItem
                    {
                        Id = "umnr_pickup_info",
                        CurrentValue = GetUMNRNoDesignatedMessage(umnr)
                    });

                if (dropoffNamesList.Any())
                    umnr.Messages.Add(new MOBItem { Id = "umnr_dropoff_info", CurrentValue = dropoffNamesList.Distinct().Join(",") });
                else
                    umnr.Messages.Add(new MOBItem
                    {
                        Id = "umnr_dropoff_info",
                        CurrentValue = GetUMNRNoDesignatedMessage(umnr)
                    });
            }
            catch { }
        }
        private void SetEmergencyContact(Collection<Service.Presentation.CommonModel.Service> services, ref MOBUmnr umnr)
        {
            string phoneContactCode = "PCTC";
            try
            {
                if (services != null && services.Any())
                {
                    var emergencycontact = services.FirstOrDefault(x => string.Equals(x.Code, phoneContactCode, StringComparison.OrdinalIgnoreCase));
                    if (emergencycontact != null && !string.IsNullOrEmpty(emergencycontact.Description))
                    {
                        string[] contactitem = emergencycontact.Description.Split('*');
                        if (contactitem.Length > 2)
                        {
                            string contactinfo = string.Format("{0}, {1}", contactitem[1], contactitem[2]);
                            umnr.Messages.Add(new MOBItem { Id = "umnr_emergency_info", CurrentValue = contactinfo });
                            return;
                        }
                    }
                }
                umnr.Messages.Add(new MOBItem
                {
                    Id = "umnr_emergency_info",
                    CurrentValue = GetUMNRNoDesignatedMessage(umnr)
                });
            }
            catch { }
        }
        private string GetUMNRNoDesignatedMessage(MOBUmnr umnr)
        {
            return umnr.Messages.FirstOrDefault
                        (x => string.Equals(x.Id, "umnr_no_designated_message", StringComparison.OrdinalIgnoreCase)).CurrentValue;
        }
        private async Task<MOBInCabinPet> GetPetMsgInformation(string petcontentdbname)
        {
            MOBInCabinPet petinfo = new MOBInCabinPet();
            petinfo.Messages = new List<MOBItem>();
            try
            {
                var petmessages = await _documentLibraryDynamoDB.GetNewLegalDocumentsForTitles(new List<string> { petcontentdbname }, _headers.ContextValues.SessionId).ConfigureAwait(false);
                if (petmessages != null && petmessages.Any())
                {
                    foreach (MOBLegalDocument doc in petmessages)
                    {
                        petinfo.Messages.Add(new MOBItem() { Id = doc.Title, CurrentValue = doc.LegalDocument });
                    }
                }
            }
            catch (Exception ex) { return null; }
            return petinfo;
        }
        private void SetInCabinPetToSegment(Reservation reservationdetail, ref MOBPNR pnr)
        {
            try
            {
                //if (reservationdetail.Pets == null || !reservationdetail.Pets.Any()) return;              

                string petCode = "PETC";
                string petRefKey = "OSI";
                var services = reservationdetail.Services;
                if (services != null && services.Any() == false) return;
                if (pnr.Segments != null && pnr.Segments.Any() == false) return;

                pnr.Segments.ForEach(seg =>
                {
                    var petsharesentry = services.Where(x =>
                    (string.Equals(x.Code, petCode, StringComparison.OrdinalIgnoreCase))
                    && (x.SegmentNumber.Contains(seg.SegmentNumber)));

                    if (petsharesentry != null && petsharesentry.Any())
                    {
                        var listreferenceno = services.Where(x =>
                        (string.Equals(x.Key, petRefKey, StringComparison.OrdinalIgnoreCase)))
                        .Select(y => y.Description).ToList<string>();

                        if (listreferenceno != null && listreferenceno.Any())
                        {
                            List<string> refnolist = new List<string>();
                            listreferenceno.ForEach(refnodesc =>
                            {
                                string refno = GetPetReferenceNoFromDesc(refnodesc);
                                if (!string.IsNullOrEmpty(refno))
                                {
                                    refnolist.Add(refno);
                                }
                            });
                            if (refnolist != null && refnolist.Any())
                            {
                                seg.InCabinPetInfo = new MOBInCabinPet();
                                seg.InCabinPetInfo.InCabinPetLabel = (refnolist.Count == 1) ? "IN-CABIN PET" : "IN-CABIN PETS";
                                seg.InCabinPetInfo.InCabinPetRefText = (refnolist.Count == 1) ? "Reference number:" : "Reference numbers:";
                                seg.InCabinPetInfo.InCabinPetRefValue = refnolist.Join(", ");
                            }
                        }
                    }
                });
            }
            catch { }
        }
        private string GetPetReferenceNoFromDesc(string desc)
        {
            try
            {
                if (!string.IsNullOrEmpty(desc))
                {
                    string[] splitDesc = desc.Split('*');
                    if (splitDesc.Length == 2)
                    {
                        return (!string.IsNullOrEmpty(splitDesc[1])) ? splitDesc[1] : string.Empty;
                    }
                }
            }
            catch { return string.Empty; }
            return string.Empty;
        }
        private async Task<MOBPNRAdvisory> PopulatePolicyExceptionContent(string displaycontent)
        {
            try
            {
                List<MOBItem> items = await _manageResUtility.GetDBDisplayContent(displaycontent);
                if (items == null || !items.Any()) return null;
                MOBPNRAdvisory content = new MOBPNRAdvisory();

                items.ForEach(item =>
                {
                    switch (item.Id)
                    {
                        case "policyexceptiontheader":
                            content.Header = item.CurrentValue;
                            break;
                        case "policyexceptionbody":
                            content.Body = item.CurrentValue;
                            break;
                        case "policyexceptionbuttontext":
                            content.Buttontext = item.CurrentValue;
                            break;
                        case "policyexceptionbuttonlink":
                            content.Buttonlink = item.CurrentValue;
                            break;
                    }
                });
                return content;
            }
            catch { return null; }
        }
        private void SetFFCChangeFeeDescription(ReservationDetail response, MOBPNR pnr)
        {
            var changefeedesc = string.Empty;
            var changefeekey = "Change fee";
            var policyinfokey = "FFC_Policy_Info";

            try
            {
                var ffcchangefee = pnr.Futureflightcredit?.Messages?.FirstOrDefault
                    (x => string.Equals(x.Id, changefeekey, StringComparison.OrdinalIgnoreCase));

                var ffcPolicyInfo = pnr.Futureflightcredit?.Messages?.FirstOrDefault
                    (x => string.Equals(x.Id, policyinfokey, StringComparison.OrdinalIgnoreCase));

                var changefee = GetCharactersticValueAndCodeByDescription
                    (response.Detail.Characteristic, "CHANGE FEE");

                if (pnr.IsPolicyExceptionAlert)
                {
                    changefeedesc = "Waived";
                    if (ffcPolicyInfo != null)
                        ffcPolicyInfo.CurrentValue = _configuration.GetValue<string>("FFC_Policy_Info");
                }
                else if (_manageResUtility.CheckTravelWaiverAlertAvailable(response.PNRChangeEligibility))
                {
                    changefeedesc = "Waived (see waiver details)";
                    if (ffcPolicyInfo != null)
                        ffcPolicyInfo.CurrentValue = _configuration.GetValue<string>("FFC_Policy_Info");
                }
                else if (changefee != null)
                {
                    if (Int32.TryParse(changefee?.CurrentValue, out int changefeeamount))
                    {
                        string formatedAmount = _manageResUtility.GetCurrencyAmount(changefeeamount, changefee.Id, 2);
                        if (changefeeamount >= 0)
                        {
                            changefeedesc = formatedAmount;
                        }
                    }
                }
                else if (changefee == null)
                {
                    changefeedesc = "Per fare rules";
                    if (ffcPolicyInfo != null)
                        ffcPolicyInfo.CurrentValue = _configuration.GetValue<string>("FFC_Policy_Info");
                }

                if (!string.IsNullOrEmpty(changefeedesc))
                {
                    if (ffcchangefee != null)
                    {
                        ffcchangefee.CurrentValue = changefeedesc;
                    }
                }
            }
            catch { }
        }
        private void UpdateScheduleChangeSegments(MOBPNR pnr, ReservationDetail response)
        {
            if (pnr.Segments == null) return;
            var segments = pnr.Segments;

            List<MOBPNRSegment> removeSegments = null;
            List<string> displaybytrip = new List<string>();
            bool isDirectToDirect = false;
            bool isConnectionCityChange = false;
            bool isDirectToConnection = false;
            bool isConnectionToDirect = false;

            segments.ForEach(seg =>
            {
                if (seg != null)
                {
                    bool isFirstSegmentofTrip = false;
                    //string[] strSplit = { "||" };
                    string schChangedSegmentTypes = _configuration.GetValue<string>("PNRScheduleChangeSegmentType");
                    //.Split(strSplit, StringSplitOptions.None);

                    string currentsegmenttype = seg.ActionCode.Substring(0, 2).ToUpper();

                    //string selectedsegmenttype = schChangeSegmentTypes?.FirstOrDefault(x => x.StartsWith(currentsegmenttype));

                    if (!string.IsNullOrEmpty(currentsegmenttype))
                    {
                        var currentcslsegment = response.Detail.FlightSegments.FirstOrDefault
                                 (x => x.TripNumber == seg.TripNumber && x.SegmentNumber == seg.SegmentNumber);

                        if (schChangedSegmentTypes.IndexOf(currentsegmenttype, StringComparison.OrdinalIgnoreCase) > -1)
                        {
                            if (!string.Equals(currentcslsegment?.FlightSegment?.NoProtection,
                                "true", StringComparison.OrdinalIgnoreCase))
                            {
                                removeSegments
                                = (removeSegments == null) ? new List<MOBPNRSegment>() : removeSegments;
                                removeSegments.Add(seg);
                            }
                        }
                        else
                        {
                            seg.ScheduleChangeInfo = (seg.ScheduleChangeInfo == null)
                                    ? new List<MOBScheduleChange>() : seg.ScheduleChangeInfo;

                            //var previouscslsegment = response.Detail.FlightSegments.FirstOrDefault
                            //     (x => x.TripNumber == seg.TripNumber && x.SegmentNumber == seg.SegmentNumber);

                            if (currentcslsegment?.FlightSegment?.PreviousSegmentDetails != null)
                            {
                                var previouscslsegmentdetail = currentcslsegment.FlightSegment.PreviousSegmentDetails;

                                if (previouscslsegmentdetail != null && previouscslsegmentdetail.Any())
                                {
                                    var selectedcsltrips = response.Detail.FlightSegments.Where
                                                        (x => x.TripNumber == seg.TripNumber);

                                    if (selectedcsltrips != null && selectedcsltrips.Any())
                                    {
                                        isDirectToDirect = false;
                                        isConnectionCityChange = false;
                                        isDirectToConnection = false;
                                        isConnectionToDirect = false;

                                        UpdateTypeOfSCChange(currentcslsegment, ref isDirectToDirect,
                                        ref isConnectionCityChange, ref isDirectToConnection, ref isConnectionToDirect);

                                        if (isConnectionCityChange || isDirectToConnection || isConnectionToDirect)
                                        {
                                            if (!displaybytrip.Contains(seg.TripNumber))
                                            {
                                                isFirstSegmentofTrip = true;
                                                displaybytrip.Add(seg.TripNumber);
                                            }
                                        }

                                    }

                                    var prevsegmentinfo = AddScheduleChangeDisplayInfo(previouscslsegmentdetail, seg, ref isDirectToDirect,
                                        ref isConnectionCityChange, ref isDirectToConnection, ref isConnectionToDirect, ref isFirstSegmentofTrip);

                                    seg.ScheduleChangeInfo.Add(prevsegmentinfo);


                                }
                            }
                        } //ELSE
                    }//SEG TYPE
                }//SEG NULL Check
            });

            if (removeSegments != null && removeSegments.Any())
            {
                segments.RemoveAll(delegate (MOBPNRSegment x)
                {
                    return removeSegments.Any(y => x.TripNumber == y.TripNumber && x.SegmentNumber == y.SegmentNumber);
                });
            }

            if (displaybytrip.Any())
            {
                displaybytrip.ForEach(tripno =>
                {
                    var selectedsegments = segments.Where
                    (x => string.Equals(x.TripNumber, tripno, StringComparison.OrdinalIgnoreCase));

                    if (selectedsegments != null && selectedsegments.Any())
                    {
                        selectedsegments.OrderBy(x => x.SegmentNumber);

                        List<MOBScheduleChange> scinfolist = new List<MOBScheduleChange>();

                        selectedsegments.ToList().ForEach(scseg =>
                        {
                            if (scseg?.ScheduleChangeInfo != null
                            && scseg.ScheduleChangeInfo.Any())
                            {
                                scseg.ScheduleChangeInfo.ForEach(scinfo =>
                                {
                                    if (scinfo != null)
                                    {
                                        scinfolist.Add(scinfo);
                                    }
                                });
                                scseg.ScheduleChangeInfo = null;
                            }
                        });

                        if (scinfolist != null && scinfolist.Any())
                        {
                            selectedsegments.FirstOrDefault().ScheduleChangeInfo = scinfolist;
                        }
                    }
                });
            }
        }
        private void UpdateTypeOfSCChange(ReservationFlightSegment selectedtripsegment, ref bool isDirectToDirect,
        ref bool isConnectionCityChange, ref bool isDirectToConnection, ref bool isConnectionToDirect)
        {
            if (selectedtripsegment?.FlightSegment?.PreviousSegmentDetails != null
                && selectedtripsegment.FlightSegment.PreviousSegmentDetails.Any())
            {
                var previoussegmentdetail = selectedtripsegment.FlightSegment.PreviousSegmentDetails.FirstOrDefault();

                if (string.Equals(previoussegmentdetail?.PreviouConnectionAirport?.Status?.Description,
                    "CONNECTED", StringComparison.OrdinalIgnoreCase))
                {
                    isConnectionToDirect = true;
                }
                else if (string.Equals(previoussegmentdetail?.PreviouConnectionAirport?.Status?.Description,
                    "NONSTOP", StringComparison.OrdinalIgnoreCase))
                {
                    isDirectToConnection = true;
                }
                else if (!string.IsNullOrEmpty(previoussegmentdetail?.PreviouConnectionAirport?.IATACode))
                //|| string.Equals(previoussegmentdetail?.PreviousDepartureAirport.Status?.Description,
                //"CHANGE", StringComparison.OrdinalIgnoreCase)
                //|| string.Equals(previoussegmentdetail?.PreviousArrivalAirport.Status?.Description,
                //"CHANGE", StringComparison.OrdinalIgnoreCase))
                {
                    isConnectionCityChange = true;
                }
            }
            isDirectToDirect = (!isConnectionCityChange && !isConnectionToDirect && !isDirectToConnection);
        }
        private MOBScheduleChange AddScheduleChangeDisplayInfo
    (Collection<PreviousSegment> previoussegment, MOBPNRSegment schedulechangedsegment, ref bool isDirectToDirect,
    ref bool isConnectionCityChange, ref bool isDirectToConnection, ref bool isConnectionToDirect, ref bool isFirstSegmentofTrip)
        {
            MOBScheduleChange mobScheduleChange = null;
            PreviousSegment previousfirstsegmentdetail = null;
            PreviousSegment previouslastsegmentdetail = null;
            string timeformat = "ddd, MMM dd, yyy h:mm tt";

            //Arrival Airport & Time
            bool isExcludeFirstDirectToConnection = isDirectToConnection && isFirstSegmentofTrip;
            bool isExcludeSecondDirectToConnection = isDirectToConnection && !isFirstSegmentofTrip;
            bool isExcludeFirstConnectionCityChange = isConnectionCityChange && isFirstSegmentofTrip;
            bool isExcludeSecondConnectionCityChange = isConnectionCityChange && !isFirstSegmentofTrip;

            //Set to default
            mobScheduleChange = new MOBScheduleChange
            {
                shouldExpand = true,
                displayHeader = "Show schedule changes",
                displayCity
                = $"{schedulechangedsegment.Departure.City} to {schedulechangedsegment.Arrival.City}",
                displayItems = new List<MOBDisplayItem>(),
                segmentNumber = schedulechangedsegment.SegmentNumber,
                tripNumber = schedulechangedsegment.TripNumber
            };

            previousfirstsegmentdetail = previoussegment.FirstOrDefault();

            if (previoussegment.Count > 1)
            {
                previouslastsegmentdetail = previoussegment.LastOrDefault();
            }

            //Flight Number  
            if (!isExcludeSecondDirectToConnection)
            {
                string flightnumbers = string.Empty;
                bool isflightnosingular = false;
                if (isConnectionToDirect)
                {
                    var fltnumbers = previoussegment?.Where
                        (x => !string.IsNullOrEmpty(x.PreviousFlightNumber))?.Select(x => x.PreviousFlightNumber);
                    isflightnosingular = (fltnumbers.Count() > 1) ? true : false;
                    flightnumbers = string.Join(", ", fltnumbers);
                }
                else
                {
                    flightnumbers = previousfirstsegmentdetail?.PreviousFlightNumber;
                }
                if (!string.IsNullOrEmpty(flightnumbers))
                {
                    var prevconnectioninfo = AddScheduleChangedDisplayItem(MOBScheduleChangeDisplayId.FLIGHTNUMBER, flightnumbers);
                    prevconnectioninfo.labelText = string.Format(prevconnectioninfo.labelText, (isflightnosingular) ? "s" : string.Empty);
                    mobScheduleChange.displayItems.Add(prevconnectioninfo);
                }
            }

            //Departure Airport & Time
            if (!isExcludeSecondDirectToConnection && !isExcludeSecondConnectionCityChange)
            {
                if (string.Equals(previousfirstsegmentdetail?.PreviousDepartureAirport.Status?.Description,
                    "CHANGE", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(previousfirstsegmentdetail.PreviousDepartureAirport?.Name))
                    {
                        mobScheduleChange.displayItems.Add(AddScheduleChangedDisplayItem
                            (MOBScheduleChangeDisplayId.DEPARTURECITY, previousfirstsegmentdetail.PreviousDepartureAirport?.Name));
                    }
                }

                if (!string.IsNullOrEmpty(previousfirstsegmentdetail.PreviousDepartureTime))
                {
                    DateTime.TryParse(previousfirstsegmentdetail.PreviousDepartureTime, out DateTime tempdeparturetime);
                    mobScheduleChange.displayItems.Add(AddScheduleChangedDisplayItem
                        (MOBScheduleChangeDisplayId.DEPARTURETIME, tempdeparturetime.ToString(timeformat)));
                }
            }

            if (!isExcludeFirstDirectToConnection && !isExcludeFirstConnectionCityChange)
            {
                PreviousSegment selectedarrivalsegment = (isConnectionToDirect) ? previouslastsegmentdetail : previousfirstsegmentdetail;
                if (string.Equals(selectedarrivalsegment.PreviousArrivalAirport?.Status?.Description,
                    "CHANGE", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(selectedarrivalsegment.PreviousArrivalAirport?.Name))
                    {
                        mobScheduleChange.displayItems.Add(AddScheduleChangedDisplayItem
                            (MOBScheduleChangeDisplayId.ARRIVALCITY, selectedarrivalsegment.PreviousArrivalAirport?.Name));
                    }
                }

                if (!string.IsNullOrEmpty(selectedarrivalsegment.PreviousArrivalTime))
                {
                    DateTime.TryParse(selectedarrivalsegment.PreviousArrivalTime, out DateTime tempparrivaltime);
                    mobScheduleChange.displayItems.Add(AddScheduleChangedDisplayItem
                        (MOBScheduleChangeDisplayId.ARRIVALTIME, tempparrivaltime.ToString(timeformat)));
                }
            }

            //Dynamic
            if (isConnectionToDirect)
            {
                if (!string.IsNullOrEmpty(previousfirstsegmentdetail.PreviousArrivalAirport?.Name))
                {
                    var prevconnectioninfo = AddScheduleChangedDisplayItem(MOBScheduleChangeDisplayId.CONNECTION, string.Empty);
                    prevconnectioninfo.labelText = string.Format(prevconnectioninfo.labelText, previousfirstsegmentdetail.PreviousArrivalAirport.Name);
                    mobScheduleChange.displayItems.Add(prevconnectioninfo);
                }
            }
            else if (isDirectToConnection)
            {
                if (!isExcludeSecondDirectToConnection)
                {
                    if (!string.IsNullOrEmpty(schedulechangedsegment.Arrival?.Name))
                    {
                        var prevconnectioninfo = AddScheduleChangedDisplayItem(MOBScheduleChangeDisplayId.NONSTOP, string.Empty);
                        prevconnectioninfo.labelText = string.Format(prevconnectioninfo.labelText, schedulechangedsegment.Arrival.Name);
                        mobScheduleChange.displayItems.Add(prevconnectioninfo);
                    }
                }
            }
            else if (isConnectionCityChange)
            {
                if (!string.IsNullOrEmpty(previousfirstsegmentdetail.PreviouConnectionAirport?.Address.Name))
                {
                    var prevconnectioninfo = AddScheduleChangedDisplayItem(MOBScheduleChangeDisplayId.CONNECTIONCHANGE, string.Empty);
                    prevconnectioninfo.labelText = string.Format(prevconnectioninfo.labelText,
                        previousfirstsegmentdetail.PreviouConnectionAirport.Address.Name, schedulechangedsegment.Arrival.City);
                    mobScheduleChange.displayItems.Add(prevconnectioninfo);
                }
            }
            else if (isDirectToDirect)
            {
                if (!string.IsNullOrEmpty(previousfirstsegmentdetail.PreviousOperatingCarrier))
                {
                    mobScheduleChange.displayItems.Add(AddScheduleChangedDisplayItem
                        (MOBScheduleChangeDisplayId.CARRIER, previousfirstsegmentdetail.PreviousOperatingCarrier));
                }

                if (!string.IsNullOrEmpty(previousfirstsegmentdetail?.PreviousAircraft?.Model?.Name))
                {
                    mobScheduleChange.displayItems.Add(AddScheduleChangedDisplayItem
                        (MOBScheduleChangeDisplayId.AIRCRAFT, previousfirstsegmentdetail.PreviousAircraft.Model.Name));
                }

                if (!string.IsNullOrEmpty(previousfirstsegmentdetail?.PreviousAircraft?.Cabins?.FirstOrDefault()?.Name))
                {
                    mobScheduleChange.displayItems.Add(AddScheduleChangedDisplayItem
                        (MOBScheduleChangeDisplayId.CABIN, previousfirstsegmentdetail.PreviousAircraft.Cabins.FirstOrDefault().Name));
                }
            }

            if (mobScheduleChange.displayItems.Any())
            {

                mobScheduleChange.displayContent = BuildSCDisplayContent(mobScheduleChange);
            }
            else
            { mobScheduleChange = null; }

            return mobScheduleChange;
        }
        private MOBDisplayItem AddScheduleChangedDisplayItem
           (MOBScheduleChangeDisplayId schedulechangedisplayid, string previoussegmentdetail)
        {
            var newdisplayitem = new MOBDisplayItem
            {
                id = Convert.ToString(schedulechangedisplayid),
                labelText = schedulechangedisplayid.GetDisplayName(),
                displayText = previoussegmentdetail
            };
            return newdisplayitem;
        }
        private string BuildSCDisplayContent(MOBScheduleChange mobScheduleChange)
        {
            string displaycontent = $"<b>{mobScheduleChange.displayCity}</b><br/>";
            string displaycontentbody = "<ul>";

            mobScheduleChange.displayItems.ForEach(ditem =>
            {
                if (!string.IsNullOrEmpty(ditem.displayText))
                {
                    displaycontentbody = $"{displaycontentbody}<li>{ditem.labelText} <b>" +
                    $"{ditem.displayText}</b></li>";
                }
                else
                {
                    displaycontentbody = $"{displaycontentbody}<li>{ditem.labelText}</li>";
                }
            });
            return $"{displaycontent}{displaycontentbody}</ul>";
        }
        private void UpdateScheduleChangeInfo(MOBPNR pnr, ReservationDetail response)
        {
            string SCMainContent = string.Empty;
            string SCKeeptrip = _configuration.GetValue<string>("PNRScheduleChangeKeeptrip");
            string SCChangeflights = _configuration.GetValue<string>("PNRScheduleChangeChangeflights");
            string SCCanceltrip = _configuration.GetValue<string>("PNRScheduleChangeCanceltrip");
            bool tempIsSCChangeEligible = false;
            bool tempIsSCRefundEligible = false;
            try
            {

                if (pnr.IsTicketedByUA
                    || (response?.Detail?.BookingAgency != null && response.Detail.BookingAgency.IsOnlineAgency))
                {
                    if (!pnr.IsSCBulkGroupPWC)
                    {
                        if (pnr.IsSCChangeEligible)
                        {
                            tempIsSCChangeEligible = true;

                            if (pnr.IsSCRefundEligible)
                            {//Message D
                                tempIsSCRefundEligible = true;
                                SCMainContent = $"{_configuration.GetValue<string>("PNRScheduleChangeCommmonContent")}" +
                                    $"{_configuration.GetValue<string>("PNRScheduleChangeContentD")}";
                            }
                            else
                            {//Message B
                                SCMainContent = $"{_configuration.GetValue<string>("PNRScheduleChangeCommmonContent")}" +
                                    $"{_configuration.GetValue<string>("PNRScheduleChangeContentB")}";
                            }
                        }
                        else
                        {
                            if (pnr.IsSCRefundEligible)
                            {//Message C
                                tempIsSCRefundEligible = true;
                                SCMainContent = $"{_configuration.GetValue<string>("PNRScheduleChangeCommmonContent")}" +
                                    $"{_configuration.GetValue<string>("PNRScheduleChangeContentC")}";
                            }
                            else
                            {//Message E
                                SCMainContent = $"{_configuration.GetValue<string>("PNRScheduleChangeCommmonContent")}" +
                                    $"{_configuration.GetValue<string>("PNRScheduleChangeContentE")}";
                            }
                        }
                    }
                    else
                    {//Message E
                        SCMainContent = $"{_configuration.GetValue<string>("PNRScheduleChangeCommmonContent")}" +
                            $"{_configuration.GetValue<string>("PNRScheduleChangeContentE")}";
                    }
                }
                else
                {//Message A
                    SCMainContent = $"{_configuration.GetValue<string>("PNRScheduleChangeCommmonContent")}" +
                        $"{_configuration.GetValue<string>("PNRScheduleChangeContentA")}";
                }


                pnr.ScheduleChangeInfo = new MOBScheduleChange
                {
                    isHtmlBodyText = true,
                    displayHeader = MOBScheduleChangeDisplayId.SCHEDULECHANGE.GetDisplayName(),
                    displayBody = SCMainContent,
                    displayBtnText = "Continue",
                    displayFooter = _configuration.GetValue<string>("PNRScheduleChangeContactUs")
                };

                if (pnr.PsSaTravel)
                {
                    pnr.ScheduleChangeInfo.displayBody = _configuration.GetValue<string>("PNRPSSAScheduleChangeAlert");
                }
                else
                {
                    pnr.ScheduleChangeInfo.displayOptions = new List<MOBDisplayItem> { };

                    if (!string.IsNullOrEmpty(SCKeeptrip) && (tempIsSCChangeEligible || tempIsSCRefundEligible))
                    {
                        pnr.ScheduleChangeInfo.displayOptions.Add(new MOBDisplayItem
                        {
                            id = Convert.ToString(MOBScheduleChangeDisplayId.KEEPTRIP),
                            labelText = MOBScheduleChangeDisplayId.KEEPTRIP.GetDisplayName(),
                            displayText = SCKeeptrip
                        });
                    }

                    if (!string.IsNullOrEmpty(SCChangeflights) && tempIsSCChangeEligible)
                    {
                        string[] splitpattern = { "||" };
                        string[] splititems
                            = SCChangeflights.Split(splitpattern, System.StringSplitOptions.RemoveEmptyEntries);
                        pnr.ScheduleChangeInfo.displayOptions.Add(new MOBDisplayItem
                        {
                            id = Convert.ToString(MOBScheduleChangeDisplayId.CHANGEFLIGHTS),
                            labelText = MOBScheduleChangeDisplayId.CHANGEFLIGHTS.GetDisplayName(),
                            displayText = splititems[0],
                            displaySubText = splititems[1],
                        });
                    }

                    if (!string.IsNullOrEmpty(SCCanceltrip) && tempIsSCRefundEligible)
                    {
                        pnr.ScheduleChangeInfo.displayOptions.Add(new MOBDisplayItem
                        {
                            id = Convert.ToString(MOBScheduleChangeDisplayId.CANCELTRIP),
                            labelText = MOBScheduleChangeDisplayId.CANCELTRIP.GetDisplayName(),
                            displayText = SCCanceltrip
                        });
                    }

                    //Check
                    if (!pnr.ScheduleChangeInfo.displayOptions.Any())
                    {
                        pnr.ScheduleChangeInfo.displayOptions = null;
                    }
                }
            }
            catch { /*supressing any exceptions*/ }
        }
        private async Task<MOBPNRAdvisory> PopulateScheduleChangeContent(string displaycontent)
        {
            try
            {
                List<MOBItem> items = await _manageResUtility.GetDBDisplayContent(displaycontent);
                if (items == null || !items.Any()) return null;
                MOBPNRAdvisory content = new MOBPNRAdvisory();

                items.ForEach(item =>
                {
                    switch (item.Id)
                    {
                        case "StatusInfoHeader":
                            content.Header = item.CurrentValue;
                            break;
                        case "StatusInfoBody":
                            content.Body = item.CurrentValue;
                            break;
                        case "StatusInfoButtonText":
                            content.Buttontext = item.CurrentValue;
                            break;
                    }
                });
                return content;
            }
            catch { return null; }
        }
        private MOBPNRAdvisory PopulateFaceCoveringMsgContent(int applicationId, string appVersion)
        {
            try
            {
                MOBPNRAdvisory content = new MOBPNRAdvisory
                {
                    IsBodyAsHtml = true,
                    Header = _configuration.GetValue<string>("manageresfacecoveringmsgheader"),
                    Body = _configuration.GetValue<string>("manageresfacecoveringmsgbodyHtml"),
                };

                if (!TopHelper.IsApplicationVersionGreaterorEqual
                    (applicationId.ToString(), appVersion))
                {
                    content.IsBodyAsHtml = false;
                    content.Buttontext = _configuration.GetValue<string>("manageresfacecoveringmsgbuttontext");
                    content.Buttonlink = _configuration.GetValue<string>("manageresfacecoveringmsgbuttonlink");

                    if (applicationId == 1)
                    {
                        content.Body = _configuration.GetValue<string>("manageresfacecoveringmsgbodyText").Replace("|", " ");
                    }
                    else if (applicationId == 2)
                    {
                        content.Body = _configuration.GetValue<string>("manageresfacecoveringmsgbodyText").Replace("|", "\n\n");
                    }
                }
                return content;
            }
            catch { return null; }
        }
        private Restriction GetFirstEligiblePolicy(Collection<Restriction> policies)
        {
            try
            {
                var excludepolicyrules = _manageResUtility.SplitConcatenatedConfigValue("PolicyWaiverExcludePolicyRule", "||");

                if (excludepolicyrules != null && excludepolicyrules.Any())
                {
                    return policies.Where(x => x?.PolicyRule != null && !excludepolicyrules.Contains(x.PolicyRule.RuleId))?.FirstOrDefault();
                }
            }
            catch { return null; }
            return null;
        }
        public MOBPNRAdvisory PopulateTravelWaiverDynamicAlertContent
           (int applicationId, string appVersion, ReservationDetail reservationDetail, Restriction policy, bool iscanceledwithfutureflightcredit)
        {
            string bookByDate = string.Empty;

            bool isoldclient = !TopHelper.IsApplicationVersionGreaterorEqual
                (applicationId.ToString(), appVersion);

            var policyrule = policy?.PolicyRule;

            if (policyrule == null) return null;

            try
            {
                bookByDate = GetBookByDate(reservationDetail.Detail.Travelers);

                MOBPNRAdvisory content = new MOBPNRAdvisory
                {
                    IsBodyAsHtml = true,
                    Header = _configuration.GetValue<string>("MRTravelWaiverHdr"),
                };

                var sbBody = new StringBuilder();
                string strcontent = string.Empty;
                bool isbulletpointadded = false;

                strcontent = _configuration.GetValue<string>("MRTravelWaiverDesc");
                sbBody.Append(FormattedTravelWaiverContent
                    (applicationId, strcontent, isoldclient, isLinebreak: true));

                //Start of AltDteRangesFrom & AltDteRangesTo
                //from > 1  | to > -1 | == 0                
                if (TravelWaiverDateComparison(DateTime.Now.ToShortDateString(), policyrule.AltDteRangesTo) == -1)
                {
                    strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverAltDtRngHdr"));
                    sbBody.Append(FormattedTravelWaiverContent
                        (applicationId, strcontent, isoldclient, isLinebreak: true));

                    int.TryParse(policyrule.AltdayBeforeAfterOption, out int altdaybeforeafteroption);

                    //When AltdayBeforeAfterOption == 0
                    if (string.Equals(policyrule.AltdayBeforeAfterOption, "0", StringComparison.OrdinalIgnoreCase))
                    {
                        strcontent = string.Empty;

                        if (TravelWaiverDateComparison(policyrule.AltDteRangesFrom, policyrule.AltDteRangesTo) == 0)
                        {
                            string new_altdterangesto = string.Empty;

                            new_altdterangesto = (TravelWaiverDateComparison(bookByDate, policyrule.AltDteRangesTo) == -1)
                                    ? bookByDate : policyrule.AltDteRangesTo;

                            strcontent =
                                string.Format(_configuration.GetValue<string>("MRTravelWaiverSameDate"), new_altdterangesto);
                        }
                        else
                        {

                            if (TravelWaiverDateComparison(bookByDate, policyrule.AltDteRangesFrom) == -1)
                            {
                                strcontent =
                                    string.Format(_configuration.GetValue<string>("MRTravelWaiverSameDate"), bookByDate);
                            }
                            else
                            {
                                string new_altdterangesfrom = string.Empty;
                                string new_altdterangesto = string.Empty;

                                if ((TravelWaiverDateComparison(bookByDate, policyrule.AltDteRangesFrom) == 1)
                                    && (TravelWaiverDateComparison(bookByDate, policyrule.AltDteRangesTo) == -1))
                                {
                                    new_altdterangesfrom = policyrule.AltDteRangesFrom;
                                    new_altdterangesto = bookByDate;
                                }
                                else if (TravelWaiverDateComparison(bookByDate, policyrule.AltDteRangesTo) >= 0)
                                {
                                    new_altdterangesfrom = policyrule.AltDteRangesFrom;
                                    new_altdterangesto = policyrule.AltDteRangesTo;
                                }

                                strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverDiffDate"),
                                    new_altdterangesfrom, new_altdterangesto);
                            }
                        }

                        sbBody.Append(FormattedTravelWaiverContent
                            (applicationId, strcontent, isoldclient, isbulletpoint: true, isstartofbulletpoint: !isbulletpointadded));
                        isbulletpointadded = (!isbulletpointadded) ? true : isbulletpointadded;
                    }
                    //When AltdayBeforeAfterOption == 1
                    else if (string.Equals(policyrule.AltdayBeforeAfterOption, "1", StringComparison.OrdinalIgnoreCase))
                    {
                        int.TryParse(policyrule.AltdaysAfter, out int altdaysafter);
                        int.TryParse(policyrule.AltdaysBefore, out int altdaysbefore);

                        strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverBfrAftrDate"), altdaysafter,
                           (altdaysafter == 1 ? "" : "s"), altdaysbefore, (altdaysbefore == 1 ? "" : "s"));

                        sbBody.Append(FormattedTravelWaiverContent
                            (applicationId, strcontent, isoldclient, isbulletpoint: true, isstartofbulletpoint: !isbulletpointadded));
                        isbulletpointadded = (!isbulletpointadded) ? true : isbulletpointadded;
                    }

                    //TODO : confirm AND or OR
                    if (!string.Equals(policyrule.AltOrigin, "ANY", StringComparison.OrdinalIgnoreCase)
                        || !string.Equals(policyrule.AltDest, "ANY", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(policyrule.AltOrigin, "NONE", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(policyrule.AltDest, "NONE", StringComparison.OrdinalIgnoreCase))
                        {
                            strcontent = string.Format
                                (_configuration.GetValue<string>("MRTravelWaiverSameAirport"));
                        }
                        else
                        {
                            strcontent = string.Format
                                (_configuration.GetValue<string>("MRTravelWaiverDiffAirport"), policyrule.AltOrigin, policyrule.AltDest);
                        }
                        sbBody.Append(FormattedTravelWaiverContent
                            (applicationId, strcontent, isoldclient, isbulletpoint: true, isstartofbulletpoint: !isbulletpointadded));
                        isbulletpointadded = (!isbulletpointadded) ? true : isbulletpointadded;
                    }

                    if (!string.Equals(policyrule.ClsOfSvc, "ANY", StringComparison.OrdinalIgnoreCase))
                    {
                        strcontent = string.Format
                               (_configuration.GetValue<string>("MRTravelWaiverSameCabin"));
                        sbBody.Append(FormattedTravelWaiverContent
                            (applicationId, strcontent, isoldclient, isbulletpoint: true, isstartofbulletpoint: !isbulletpointadded));
                        isbulletpointadded = (!isbulletpointadded) ? true : isbulletpointadded;
                    }

                    if (policyrule.NewCarriers.IndexOf("UA", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        strcontent = string.Format
                               (_configuration.GetValue<string>("MRTravelWaiverOperatedByUA"));
                        sbBody.Append(FormattedTravelWaiverContent
                            (applicationId, strcontent, isoldclient, isbulletpoint: true, isstartofbulletpoint: !isbulletpointadded));
                        isbulletpointadded = (!isbulletpointadded) ? true : isbulletpointadded;
                    }

                    if (_configuration.GetValue<bool>("IncludeMAX737AircraftType"))
                    {
                        if (!string.IsNullOrEmpty(policyrule.AltFleetExclusion)
                            && !string.Equals(policyrule.AltFleetExclusion, "ANY", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(policyrule.AltFleetExclusion, "NONE", StringComparison.OrdinalIgnoreCase))
                        {
                            strcontent = string.Format
                                   (_configuration.GetValue<string>("MRTravelWaiverAircraftType"), policyrule.AltFleetExclusionDecode);
                            sbBody.Append(FormattedTravelWaiverContent
                                (applicationId, strcontent, isoldclient, isbulletpoint: true, isstartofbulletpoint: !isbulletpointadded));
                            isbulletpointadded = (!isbulletpointadded) ? true : isbulletpointadded;
                        }
                    }
                    else
                    {
                        if (!string.Equals(policyrule.CfFleetExclusion, "ANY", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(policyrule.CfFleetExclusion, "NONE", StringComparison.OrdinalIgnoreCase))
                        {
                            strcontent = string.Format
                                   (_configuration.GetValue<string>("MRTravelWaiverAircraftType"), policyrule.CfFleetExclusion);
                            sbBody.Append(FormattedTravelWaiverContent
                                (applicationId, strcontent, isoldclient, isbulletpoint: true, isstartofbulletpoint: !isbulletpointadded));
                            isbulletpointadded = (!isbulletpointadded) ? true : isbulletpointadded;
                        }
                    }

                    if (isbulletpointadded)
                    {
                        sbBody.Append(FormattedTravelWaiverContent
                            (applicationId, strcontent, isoldclient, isendofbulletpoint: true));
                    }
                }
                ////End of AltDteRangesFrom & AltDteRangesTo
                isbulletpointadded = false;
                //Start of GRP-2 - CfAltDteRangesFrom & CfAltDteRangesTo
                if (TravelWaiverDateComparison(DateTime.Now.ToShortDateString(), policyrule.CfAltDteRangesTo) == -1)
                {
                    //GRP -2 Header                    
                    strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverCfAltDteRangeHdr"));
                    sbBody.Append(FormattedTravelWaiverContent(applicationId, strcontent, isoldclient, isLinebreak: true));

                    //GRP-2 Content
                    strcontent = string.Empty;

                    if (TravelWaiverDateComparison(policyrule.CfAltDteRangesFrom, policyrule.CfAltDteRangesTo) == 0)
                    {
                        string new_cfaltdterangesto = string.Empty;

                        new_cfaltdterangesto = (TravelWaiverDateComparison(bookByDate, policyrule.AltDteRangesTo) == -1)
                                ? bookByDate : policyrule.AltDteRangesTo;

                        strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverSameDate")
                            , new_cfaltdterangesto);
                    }
                    else
                    {
                        if (TravelWaiverDateComparison(bookByDate, policyrule.AltDteRangesFrom) == -1)
                        {
                            strcontent =
                                string.Format(_configuration.GetValue<string>("MRTravelWaiverSameDate"), bookByDate);
                        }
                        else
                        {
                            string new_cfaltdterangesfrom = string.Empty;
                            string new_cfaltdterangesto = string.Empty;

                            if ((TravelWaiverDateComparison(bookByDate, policyrule.CfAltDteRangesFrom) == 1)
                                && (TravelWaiverDateComparison(bookByDate, policyrule.CfAltDteRangesTo) == -1))
                            {
                                new_cfaltdterangesfrom = policyrule.CfAltDteRangesFrom;
                                new_cfaltdterangesto = bookByDate;
                            }
                            else if (TravelWaiverDateComparison(bookByDate, policyrule.CfAltDteRangesTo) >= 0)
                            {
                                new_cfaltdterangesfrom = policyrule.CfAltDteRangesFrom;
                                new_cfaltdterangesto = policyrule.CfAltDteRangesTo;
                            }

                            strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverDiffDate"),
                                new_cfaltdterangesfrom, new_cfaltdterangesto);
                        }
                    }

                    sbBody.Append(FormattedTravelWaiverContent
                            (applicationId, strcontent, isoldclient, isbulletpoint: true, isstartofbulletpoint: !isbulletpointadded));
                    isbulletpointadded = (!isbulletpointadded) ? true : isbulletpointadded;


                    if (!string.Equals(policyrule.CfAltOrigin, "ANY", StringComparison.OrdinalIgnoreCase)
                       || !string.Equals(policyrule.CfAltDest, "ANY", StringComparison.OrdinalIgnoreCase))
                    {
                        if (string.Equals(policyrule.CfAltOrigin, "NONE", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(policyrule.CfAltDest, "NONE", StringComparison.OrdinalIgnoreCase))
                        {
                            strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverSameAirport"));
                        }
                        else
                        {
                            strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverDiffAirport")
                                , policyrule.CfAltOrigin, policyrule.CfAltDest);
                        }
                        sbBody.Append(FormattedTravelWaiverContent
                            (applicationId, strcontent, isoldclient, isbulletpoint: true, isstartofbulletpoint: !isbulletpointadded));
                        isbulletpointadded = (!isbulletpointadded) ? true : isbulletpointadded;
                    }

                    if (!string.Equals(policyrule.CfClsvc, "ANY", StringComparison.OrdinalIgnoreCase))
                    {
                        strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverSameCabin"));
                        sbBody.Append(FormattedTravelWaiverContent
                            (applicationId, strcontent, isoldclient, isbulletpoint: true, isstartofbulletpoint: !isbulletpointadded));
                        isbulletpointadded = (!isbulletpointadded) ? true : isbulletpointadded;
                    }

                    if (string.Equals(policyrule.CfNewCarriers, "UA", StringComparison.OrdinalIgnoreCase))
                    {
                        strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverOperatedByUA"));
                        sbBody.Append(FormattedTravelWaiverContent
                            (applicationId, strcontent, isoldclient, isbulletpoint: true, isstartofbulletpoint: !isbulletpointadded));
                        isbulletpointadded = (!isbulletpointadded) ? true : isbulletpointadded;
                    }

                    if (isbulletpointadded)
                    {
                        sbBody.Append(FormattedTravelWaiverContent
                            (applicationId, strcontent, isoldclient, isendofbulletpoint: true));
                    }
                }
                //End of CfAltDteRangesFrom & CfAltDteRangesTo
                isbulletpointadded = false;
                //Start of CancelByDate                
                if (TravelWaiverDateComparison(DateTime.Now.ToShortDateString(), policyrule.CancelByDate) <= 0)
                {
                    string new_cancelbydate = string.Empty;

                    strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverChngByDtHdr"));
                    sbBody.Append(FormattedTravelWaiverContent(applicationId, strcontent, isoldclient, isLinebreak: true));

                    new_cancelbydate = (TravelWaiverDateComparison(bookByDate, policyrule.CancelByDate) == -1)
                        ? bookByDate : policyrule.CancelByDate;

                    strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverChngByDtDesc"), new_cancelbydate);
                    sbBody.Append(FormattedTravelWaiverContent
                            (applicationId, strcontent, isoldclient, isbulletpoint: true, isstartofbulletpoint: !isbulletpointadded));
                    isbulletpointadded = (!isbulletpointadded) ? true : isbulletpointadded;

                    if (!iscanceledwithfutureflightcredit)
                    {
                        strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverChngRebookLtr"), new_cancelbydate);
                        sbBody.Append(FormattedTravelWaiverContent
                            (applicationId, strcontent, isoldclient, isbulletpoint: true, isstartofbulletpoint: !isbulletpointadded));
                        isbulletpointadded = (!isbulletpointadded) ? true : isbulletpointadded;
                    }
                }

                if (reservationDetail.PNRChangeEligibility.Policies.Count > 1)
                {
                    strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverMoreThanOne"));
                    sbBody.Append(FormattedTravelWaiverContent
                            (applicationId, strcontent, isoldclient, isbulletpoint: true, isstartofbulletpoint: !isbulletpointadded));
                    isbulletpointadded = (!isbulletpointadded) ? true : isbulletpointadded;
                }

                if (isbulletpointadded)
                {
                    sbBody.Append(FormattedTravelWaiverContent
                        (applicationId, strcontent, isoldclient, isendofbulletpoint: true));
                }

                strcontent = string.Format(_configuration.GetValue<string>("MRTravelWaiverFootr"));
                sbBody.Append(FormattedTravelWaiverContent(applicationId, strcontent, isoldclient));

                content.Body = sbBody.ToString();
                return content;
            }
            catch { return null; }
        }
        private string GetBookByDate(Collection<Traveler> travelers)
        {
            try
            {
                if (travelers.Any())
                {
                    if (DateTime.TryParse(travelers.Where(x => !string.IsNullOrEmpty(x.Tickets?.FirstOrDefault()?.TicketValidityDate))?
                        .FirstOrDefault()?.Tickets?.FirstOrDefault()?.TicketValidityDate, out DateTime formatteddate))
                    {
                        return formatteddate.ToString("MM/dd/yyyy");
                    }
                }
            }
            catch { return string.Empty; }
            return string.Empty;
        }

        private string FormattedTravelWaiverContent
            (int applicationId, string content, bool isoldclient, bool isbulletpoint = false,
            bool isLinebreak = false, bool isstartofbulletpoint = false, bool isendofbulletpoint = false)
        {
            string linebreak = string.Empty;
            string linebreakendofbulletpoint = string.Empty;
            string startbulletpoint = string.Empty;
            string endbulletpoint = string.Empty;
            string bulletpointopen = string.Empty;
            string bulletpointclose = string.Empty;

            if (isoldclient)
            {
                if (applicationId == 1)
                {
                    linebreak = " ";
                    linebreakendofbulletpoint = " ";
                    startbulletpoint = string.Empty;
                    endbulletpoint = " ";
                    bulletpointopen = " ";
                    bulletpointclose = " ";
                }
                else if (applicationId == 2)
                {
                    linebreak = "\n\n";
                    linebreakendofbulletpoint = "\n\n";
                    startbulletpoint = string.Empty;
                    endbulletpoint = "\n\n";
                    bulletpointopen = "\n";
                    bulletpointclose = "\n";
                }
            }
            else
            {
                linebreak = "<br/><br/>";
                linebreakendofbulletpoint = "<br/>";
                startbulletpoint = "<ul><li>";
                endbulletpoint = "</ul><br/>";
                bulletpointopen = "<li>";
                bulletpointclose = "</li>";
            }

            if (isendofbulletpoint)
            {
                return $"{endbulletpoint}";
            }
            else
            {
                return $"{(isstartofbulletpoint ? startbulletpoint : isbulletpoint ? bulletpointopen : string.Empty)} " +
                        $"{content} " +
                        $"{(isendofbulletpoint ? endbulletpoint : isbulletpoint ? bulletpointclose : string.Empty)}" +
                        $"{(isLinebreak ? linebreak : string.Empty)}";
            }
        }
        private int TravelWaiverDateComparison(string fromdate, string todate)
        {
            DateTime.TryParse(fromdate, out DateTime compfromdate);
            DateTime.TryParse(todate, out DateTime comptodate);
            return DateTime.Compare(compfromdate, comptodate);
        }
        private async Task<MOBPNRAdvisory> PopulateIncabinPetContent(string displaycontent)
        {
            try
            {
                List<MOBItem> items = await _manageResUtility.GetDBDisplayContent(displaycontent);
                if (items == null || !items.Any()) return null;
                MOBPNRAdvisory content = new MOBPNRAdvisory();

                items.ForEach(item =>
                {
                    switch (item.Id)
                    {
                        case "incabinpetheader1":
                            content.Header = item.CurrentValue;
                            break;
                        case "incabinpetbody1":
                            content.Body = item.CurrentValue;
                            break;
                        case "incabinpetnavigation1":
                            content.Buttontext = item.CurrentValue;
                            break;
                        case "incabinpetheader2":
                            {
                                content.SubItems = (content.SubItems == null) ? new List<MOBItem>() : content.SubItems;
                                content.SubItems.Add(new MOBItem { Id = "subheader", CurrentValue = item.CurrentValue });
                                break;
                            }
                        case "incabinpetbody2":
                            {
                                content.SubItems = (content.SubItems == null) ? new List<MOBItem>() : content.SubItems;
                                content.SubItems.Add(new MOBItem { Id = "subbody", CurrentValue = item.CurrentValue });
                                break;
                            }
                        case "incabinpetlabel":
                            {
                                content.SubItems = (content.SubItems == null) ? new List<MOBItem>() : content.SubItems;
                                content.SubItems.Add(new MOBItem { Id = item.Id, CurrentValue = item.CurrentValue });
                                break;
                            }
                        case "incabinpetrefnolabel":
                            {
                                content.SubItems = (content.SubItems == null) ? new List<MOBItem>() : content.SubItems;
                                content.SubItems.Add(new MOBItem { Id = item.Id, CurrentValue = item.CurrentValue });
                                break;
                            }
                    }
                });
                return content;
            }
            catch { return null; }
        }
        public async System.Threading.Tasks.Task<MOBPNR> GetVBQEarnedMileDetails(Collection<Traveler> travelers, MOBPNR pnr)
        {
            string earningDisplayType = string.Empty;
            List<MOBLMXTraveler> lmxtravelers = new List<MOBLMXTraveler>();

            List<MOBItem> tripnames = _manageResUtility.GetTripNamesFromSegment(pnr.Segments);

            if (travelers == null || tripnames == null || pnr.Passengers == null
                || !travelers.Any() || !tripnames.Any() || !pnr.Passengers.Any()) return pnr;

            try
            {
                pnr.Passengers.ForEach(pax =>
                {
                    if (pax != null)
                    {
                        var traveler = travelers.FirstOrDefault(x => (x.Person != null
                        && string.Equals(x.Person.Key, pax.SHARESPosition, StringComparison.OrdinalIgnoreCase)));
                        if (traveler != null)
                        {
                            if (traveler.LoyaltyProgramProfile != null
                            && !string.IsNullOrEmpty(traveler.LoyaltyProgramProfile.LoyaltyProgramMemberID))
                            {
                                GetEarnedMiles(ref pax, traveler, tripnames);
                                if (!string.IsNullOrEmpty(pax.EarnedMiles.EarnedMilesType)) earningDisplayType = pax.EarnedMiles.EarnedMilesType;
                            }
                            else
                                pax.EarnedMiles = GetNonMemberMessage(passenger: pax);
                        }
                    }
                });

                //Pax loopLMX_EarnedMiles_Content
                pnr.EarnedMilesInfo = await GetEarningTypeContentFromDB(earningDisplayType);
            }
            catch { return pnr; }
            return pnr;
        }
        private async Task<List<MOBItem>> GetEarningTypeContentFromDB(string earningDisplayType)
        {
            return (string.Equals(earningDisplayType,
                Convert.ToString(MOBEarningDisplayType.VBQ), StringComparison.OrdinalIgnoreCase))
                     ? await _manageResUtility.GetDBDisplayContent("VBQ_EarnedMiles_Content")
                     : await _manageResUtility.GetDBDisplayContent("LMX_EarnedMiles_Content");
        }
        private void DisplayGroupedPNRPaxByLastname(ref MOBPNR pnr, string lastName)
        {
            try
            {
                if (pnr.isgroup && pnr.Passengers != null && pnr.Passengers.Any())
                {
                    List<MOBPNRPassenger> grppassengers = new List<MOBPNRPassenger>();
                    pnr.Passengers.ForEach(pax =>
                    {
                        if (pax.PassengerName != null && !string.IsNullOrEmpty(pax.PassengerName.Last)
                            && string.Equals(pax.PassengerName.Last, lastName, StringComparison.OrdinalIgnoreCase))
                        {
                            grppassengers.Add(pax);
                        }
                    });
                    if (grppassengers != null && grppassengers.Any())
                    {
                        pnr.Passengers = new List<MOBPNRPassenger>();
                        pnr.Passengers = grppassengers;
                        pnr.NumberOfPassengers = Convert.ToString(grppassengers.Count);
                    }
                }
            }
            catch { }
        }
        private async Task<string> GetEmployeeIdFromCSL(string transactionId, string mileagePlusNumber, string languageCode,
          string appVersion, int applicationId, string sessionId = null, bool getEmployeeIdFromCSLCustomerData = false)
        {
            MOBCPProfileRequest profileRequest = new MOBCPProfileRequest();
            profileRequest.Application = new MOBApplication();
            profileRequest.Application.Id = applicationId;
            profileRequest.Application.Version = new MOBVersion();
            profileRequest.Application.Version.Major = appVersion;
            profileRequest.LanguageCode = languageCode;
            profileRequest.TransactionId = transactionId;
            profileRequest.MileagePlusNumber = mileagePlusNumber;
            profileRequest.SessionId = sessionId;
            profileRequest.Token = await _dPService.GetAnonymousToken(applicationId, transactionId, _configuration);
            profileRequest.ProfileOwnerOnly = true;
            profileRequest.IncludeAllTravelerData = false;
            string employeeId = string.Empty;
            if (_configuration.GetValue<string>("GetOnlyEmployeeIDForWalletCall") != null && Convert.ToBoolean(_configuration.GetValue<string>("GetOnlyEmployeeIDForWalletCall")))
            {
                employeeId = await _empprofile.GetOnlyEmpIDForWalletCall(profileRequest, getEmployeeIdFromCSLCustomerData);
            }
            else
            {
                List<MOBCPProfile> profiles = await _empprofile.GetEmpProfile(profileRequest, getEmployeeIdFromCSLCustomerData); //new List<MOBCPProfile>();
                if (profiles != null &&
                    profiles.Count > 0 &&
                    profiles[0].Travelers != null &&
                    profiles[0].Travelers.Count > 0)
                {
                    employeeId = profiles[0].Travelers[0]._employeeId;
                }
            }

            //if (iProfile.LogEntries != null && iProfile.LogEntries.Count > 0)
            //{
            //    LogEntries.AddRange(iProfile.LogEntries);
            //}

            return employeeId;
        }

        private async Task<List<MOBEmployeePNR>> GetEmployeePNRs(string transactionId, string employeeId)
        {
            List<MOBEmployeePNR> employeePNRList = null;

            if (Convert.ToBoolean(_configuration.GetValue<string>("UseEResServiceForEmployeePNRs")))
            {
                //if (this.levelSwitch.TraceError)
                //{
                //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(transactionId, "GetEmployeePNRsRequest",
                //        "Request",
                //        "employeeId=" + employeeId
                //        ));//Common Login Code
                //}

                // PNRServiceSoapClient pnrServiceSoapClient = new PNRServiceSoapClient();
                var serviceResponse = await _pNRServiceEResService.GetEmployeePNRs(employeeId, "true", _configuration.GetValue<string>("AccessCode - UAWSPNRServiceERes")).ConfigureAwait(false);
                if (serviceResponse != null && serviceResponse.COWSException != null && serviceResponse.COWSException.Code.Equals("E0000") && serviceResponse.PNRCollection != null)
                {
                    employeePNRList = new List<MOBEmployeePNR>();
                    foreach (UAWSPNRServiceERes.PNR p in serviceResponse.PNRCollection)
                    {
                        MOBEmployeePNR ePnr = new MOBEmployeePNR();
                        ePnr.RecordLocator = p.RecordLocator;
                        ePnr.LastName = p.Passengers[0].LastName;
                        employeePNRList.Add(ePnr);
                    }
                }
            }
            else
            {
                string url = string.Format(_configuration.GetValue<string>("UAfeedsWebGetEmployeePNRsURL"), employeeId);
                // WebRequest request = WebRequest.Create(url);
                // WebResponse response = null;
                string result = null;
                try
                {
                    // request.ContentType = "text/xml";
                    //request.Method = "GET";

                    //using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    //{
                    // result = sr.ReadToEnd();
                    //  PnrSummaryResponseCollection pnrSummaryResponseCollection = United.Xml.Serialization.XmlSerializer.Deserialize<PnrSummaryResponseCollection>(result);
                    PnrSummaryResponseCollection pnrSummaryResponseCollection = JsonConvert.DeserializeObject<PnrSummaryResponseCollection>(result);

                    //if (this.levelSwitch.TraceError)
                    //{
                    //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<United.Mobile.DAL.Models.PnrSummaryResponseCollection>(transactionId, "GetEmployeePNRsResponse", "Response", pnrSummaryResponseCollection));//Common Login Code
                    //}
                    if (pnrSummaryResponseCollection != null && pnrSummaryResponseCollection.PnrSummaryResponses != null && pnrSummaryResponseCollection.PnrSummaryResponses.Count > 0)
                    {
                        employeePNRList = new List<MOBEmployeePNR>();
                        foreach (PnrSummaryResponse p in pnrSummaryResponseCollection.PnrSummaryResponses)
                        {
                            MOBEmployeePNR ePnr = new MOBEmployeePNR();
                            ePnr.RecordLocator = p.pnrSummary.RecordLocator;
                            ePnr.LastName = p.pnrSummary.LastName;
                            employeePNRList.Add(ePnr);
                        }
                    }
                    // }
                }
                catch (System.Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                    //if (this.levelSwitch.TraceError)
                    //{
                    //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<WebResponse>(transactionId, "GetEmployeePNRsResponse", "Response", response));//Common Login Code
                    //}
                }
            }


            return employeePNRList;
        }
        private List<MOBLMXTraveler> GetLMXTravelersFromFlightsAndTravelers(MOBPNR pnr)
        {
            List<MOBLMXTraveler> lmxTravelers = new List<MOBLMXTraveler>();
            MOBLMXTraveler lmxTraveler;

            foreach (MOBPNRPassenger traveler in pnr.Passengers)
            {
                lmxTraveler = new MOBLMXTraveler();
                if (traveler.PassengerName != null)
                {
                    lmxTraveler.FirstName = traveler.PassengerName.First;
                    lmxTraveler.LastName = traveler.PassengerName.Last;
                }
                if (traveler.MileagePlus != null)
                {
                    lmxTraveler.IsMPMember = true;
                    lmxTraveler.HasIneligibleSegment = false;
                    lmxTraveler.MPEliteLevelDescription = traveler.MileagePlus.CurrentEliteLevelDescription;
                    List<MOBLMXRow> lmxRows = new List<MOBLMXRow>();
                    double pqdTotal = 0;
                    double pqsTotal = 0;
                    double rdmTotal = 0;
                    double pqmTotal = 0;

                    foreach (MOBPNRSegment flight in pnr.Segments)
                    {
                        bool addedItemAtLoyaltyLevel = false;
                        if (flight.LmxProducts != null && flight.LmxProducts.Count > 0)
                        {
                            foreach (MOBLmxLoyaltyTier tier in flight.LmxProducts[0].LmxLoyaltyTiers)
                            {
                                //Fix for 281982:ViewRes - GetPnrByRecordLoacator() Object Reference Exception - Schedule Change--Niveditha
                                if (tier.Level == traveler.MileagePlus.CurrentEliteLevel && !addedItemAtLoyaltyLevel && tier.LmxQuotes != null && tier.LmxQuotes.Count > 0)
                                {
                                    MOBLMXRow row = new MOBLMXRow();
                                    foreach (MOBLmxQuote quote in tier.LmxQuotes)
                                    {
                                        row.Segment = flight.Departure.Code + " - " + flight.Arrival.Code;
                                        switch (quote.Type)
                                        {
                                            case "PQS":
                                                pqsTotal += quote.DblAmount;
                                                row.PQS = quote.DblAmount.ToString();
                                                break;
                                            case "PQD":
                                                pqdTotal += quote.DblAmount;
                                                row.PQD = quote.DblAmount.ToString("C0", CultureInfo.CurrentCulture).Replace(",", "");
                                                break;
                                            case "RDM":
                                                rdmTotal += quote.DblAmount;
                                                //row.AwardMiles = string.Format("{0:#,##0}", quote.DblAmount);
                                                row.AwardMiles = quote.DblAmount.ToString();
                                                break;
                                            case "PQM":
                                                pqmTotal += quote.DblAmount;
                                                row.PQM = quote.DblAmount.ToString();
                                                break;
                                        }
                                    }
                                    if (flight.OperationoperatingCarrier != null && flight.OperationoperatingCarrier.Code != "UA")
                                    {
                                        row.OperatingCarrierDescription = "Operated by " + flight.OperationoperatingCarrier.Name;
                                    }
                                    if (flight.NonPartnerFlight)
                                    {
                                        lmxTraveler.HasIneligibleSegment = true;
                                        row.IsEligibleSegment = false;
                                        row.IneligibleSegmentMessage = pnr.OaIneligibleToEarnCreditMessage;

                                    }
                                    else
                                    {
                                        row.IsEligibleSegment = true;

                                    }
                                    addedItemAtLoyaltyLevel = true;
                                    lmxRows.Add(row);

                                    foreach (MOBPNRSeat seat in flight.Seats)
                                    {
                                        if (traveler.SHARESPosition == seat.PassengerSHARESPosition && seat.Price > 0)
                                        { //eplus for the correct passenger
                                            MOBLMXRow lmxRow = new MOBLMXRow();
                                            lmxRow.Segment = flight.Departure.Code + " - " + flight.Arrival.Code;
                                            lmxRow.AwardMiles = "---";
                                            lmxRow.PQM = "---";
                                            lmxRow.PQS = "-";
                                            double seatCost = seat.Price;
                                            pqdTotal = pqdTotal + seatCost;
                                            lmxRow.PQD = seatCost.ToString("C0", CultureInfo.CurrentCulture);
                                            lmxRow.OperatingCarrierDescription = @"\tEconomy Plus";
                                            lmxRow.IsEligibleSegment = true;
                                            lmxRows.Add(row);
                                        }
                                    }

                                }
                            }
                        }
                    }
                    if (lmxRows.Count == 0)
                    {
                        MOBLMXRow row = new MOBLMXRow();
                        lmxTraveler.HasIneligibleSegment = true;
                        row.IsEligibleSegment = false;
                        row.IneligibleSegmentMessage = "Could not retrieve any earning data";
                        lmxRows.Add(row);
                    }
                    lmxTraveler.LMXRows = lmxRows;
                    lmxTraveler.FormattedAwardMileTotal = string.Format("{0:#,##0}", rdmTotal) + " miles";
                    //lmxTraveler.awardMileTotalFormated = rdmTotal.ToString() + " miles";
                    lmxTraveler.FormattedPQMTotal = pqmTotal.ToString() + " miles";
                    //lmxTraveler.FormattedPQDTotal = pqdTotal.ToString("C2", CultureInfo.CurrentCulture);
                    lmxTraveler.FormattedPQDTotal = pqdTotal.ToString("C0", CultureInfo.CurrentCulture).Replace(",", "");
                    lmxTraveler.FormattedPQSTotal = pqsTotal.ToString();
                    lmxTraveler.AwardMileTotal = string.Format("{0:#,##0}", rdmTotal);
                    //lmxTraveler.awardMileTotalShort = rdmTotal.ToString();
                    lmxTraveler.PQMTotal = pqmTotal.ToString();
                    //lmxTraveler.PQDTotal = string.Format("{0:#,0.00}", pqdTotal);
                    lmxTraveler.PQDTotal = pqdTotal.ToString();
                    lmxTraveler.PQSTotal = pqsTotal.ToString();

                }
                else
                {
                    lmxTraveler.IsMPMember = false;
                    string memberProgram = string.Empty;
                    bool allianceProgramFound = false;
                    if (traveler.OaRewardPrograms != null && traveler.OaRewardPrograms.Count > 0)
                    {
                        foreach (MOBRewardProgram program in traveler.OaRewardPrograms)
                        {
                            if (program != null && !string.IsNullOrEmpty(program.VendorCode) && program.VendorCode != "UA")
                            {
                                memberProgram = program.VendorDescription;
                                break;
                            }
                        }

                        if (!string.IsNullOrEmpty(memberProgram))
                        {
                            lmxTraveler.MPEliteLevelDescription = memberProgram;
                            lmxTraveler.NonMPMemberMessage = string.Concat("This traveler is earning miles with ", memberProgram);
                            allianceProgramFound = true;
                        }
                    }
                    if (!allianceProgramFound)
                    {
                        lmxTraveler.MPEliteLevelDescription = "Non-member";
                        lmxTraveler.NonMPMemberMessage = _configuration.GetValue<string>("NonMPMemberMessage");

                    }
                }
                lmxTravelers.Add(lmxTraveler);
            }

            return lmxTravelers;
        }
        private List<MOBUpgradePropertyKeyValue> GetUpgradeProperties(UAWSFlightReservation.UpgradePropertiesKeyValue[] upgradePropertiesKeyValue)
        {
            List<MOBUpgradePropertyKeyValue> upgradeProperties = null;
            if (upgradePropertiesKeyValue != null && upgradePropertiesKeyValue.Length > 0)
            {
                upgradeProperties = new List<MOBUpgradePropertyKeyValue>();
                foreach (var keyValue in upgradePropertiesKeyValue)
                {
                    MOBUpgradePropertyKeyValue upgradePropertyKeyValue = new MOBUpgradePropertyKeyValue();
                    upgradePropertyKeyValue.Key = GetUpgradePropertyKey(keyValue.Key);
                    upgradePropertyKeyValue.Value = keyValue.Value;
                    upgradeProperties.Add(upgradePropertyKeyValue);
                }
            }

            return upgradeProperties;
        }
        private MOBUpgradeEligibilityStatus GetUpgradeStatus(UAWSFlightReservation.UpgradeEligibilityStatus upgradeEligibilityStatus)
        {
            //switch (upgradeEligibilityStatus)
            //{
            //    case UAWSFlightReservation.UpgradeEligibilityStatus.NotQualified:
            //        return MOBUpgradeEligibilityStatus.NotQualified;
            //        break;
            //    case UAWSFlightReservation.UpgradeEligibilityStatus.NotUpgraded:
            //        return MOBUpgradeEligibilityStatus.NotUpgraded;
            //        break;
            //    case UAWSFlightReservation.UpgradeEligibilityStatus.Qualified:
            //        return MOBUpgradeEligibilityStatus.Qualified;
            //        break;
            //    case UAWSFlightReservation.UpgradeEligibilityStatus.RequestConfirmed:
            //        return MOBUpgradeEligibilityStatus.RequestConfirmed;
            //        break;
            //    case UAWSFlightReservation.UpgradeEligibilityStatus.Requested:
            //        return MOBUpgradeEligibilityStatus.Requested;
            //        break;
            //    case UAWSFlightReservation.UpgradeEligibilityStatus.Unknown:
            //        return MOBUpgradeEligibilityStatus.Unknown;
            //        break;
            //    case UAWSFlightReservation.UpgradeEligibilityStatus.Upgraded:
            //        return MOBUpgradeEligibilityStatus.Upgraded;
            //        break;
            //    default:
            //        return MOBUpgradeEligibilityStatus.Unknown;
            //        break;
            //}
            return default;
        }

        private bool IsBasicEconomyCatagory(Collection<Genre> type)
        {
            return type != null && (type.Any(IsIBEtype) || type.Any(IsIBELitetype) || type.Any(IsELFtype));
        }
        private bool IsIBELitetype(Genre type)
        {
            return type != null &&
                   type.Key != null && type.Key.Trim().ToUpper() == "IBE" &&
                   _manageResUtility.IsIBELiteFare(type.Value);
        }
        private bool IsELFtype(Genre type)
        {
            return type != null
                   && type.Key != null && type.Key.Trim().ToUpper() == "ELF"
                   && _manageResUtility.IsELFFare(type.Value);
        }

        #endregion
        private bool CompareUPPClass(ReservationFlightSegment f, string bookingClass)
        {
            return bookingClass != null &&
                f != null && f.FlightSegment != null && f.FlightSegment.BookingClasses != null && f.FlightSegment.BookingClasses.Any() &&
                f.FlightSegment.BookingClasses[0] != null &&
                f.FlightSegment.BookingClasses[0].Cabin != null &&
                f.FlightSegment.BookingClasses[0].Cabin.Name != null &&
                f.FlightSegment.BookingClasses[0].Cabin.Name.ToUpper().Trim() == bookingClass.ToUpper().Trim();
        }
        private void GetEarnedMiles(ref MOBPNRPassenger pax, Traveler traveler, List<MOBItem> tripnames)
        {
            if (traveler.LoyaltyProgramProfile.Balances != null && traveler.LoyaltyProgramProfile.Balances.Any())
            {
                foreach (LoyaltyAccountBalance balance in traveler.LoyaltyProgramProfile.Balances)
                {
                    if (balance != null)
                    {
                        pax.EarnedMiles
                        = GetMemberMileagePlusEarning(pax, balance.Characteristics, tripnames);
                    }
                } //traveler loop                                    
            }
            else
                pax.EarnedMiles = GetNoEarningsRowMessage(pax);
        }
        private MOBLMXTraveler GetMemberMileagePlusEarning(MOBPNRPassenger pax,
          Collection<Characteristic> characteristics, List<MOBItem> tripnames)
        {
            string codePQM = "PQM";
            string codePQS = "PQS";
            string codePQP = "PQP";
            string codePQF = "PQF";
            string codePQD = "PQD";
            string codeRDM = "RDM";
            MOBLMXTraveler mileageplustraveler = new MOBLMXTraveler();
            mileageplustraveler.LMXRows = new List<MOBLMXRow>();

            try
            {
                if (characteristics != null && characteristics.Any() && tripnames != null && tripnames.Any())
                {
                    if (pax != null)
                    {
                        if (pax.PassengerName != null)
                        {
                            mileageplustraveler.FirstName = pax.PassengerName.First;
                            mileageplustraveler.LastName = pax.PassengerName.Last;
                        }
                        if (pax.MileagePlus != null)
                            mileageplustraveler.MPEliteLevelDescription = pax.MileagePlus.CurrentEliteLevelDescription;
                    }
                    mileageplustraveler.IsMPMember = true;


                    mileageplustraveler.AwardMileTotal = _manageResUtility.GetEarnedMilesFromCharacteristics(characteristics, codeRDM, string.Empty);
                    mileageplustraveler.FormattedAwardMileTotal = _manageResUtility.GetFormattedMiles(mileageplustraveler.AwardMileTotal);

                    mileageplustraveler.PQMTotal = _manageResUtility.GetEarnedMilesFromCharacteristics(characteristics, codePQM, string.Empty);
                    mileageplustraveler.FormattedPQMTotal = _manageResUtility.GetFormattedMiles(mileageplustraveler.PQMTotal);

                    mileageplustraveler.PQDTotal = _manageResUtility.GetEarnedMilesFromCharacteristics(characteristics, codePQD, string.Empty);
                    mileageplustraveler.FormattedPQDTotal = _manageResUtility.GetFormattedCurrency(mileageplustraveler.PQDTotal);

                    mileageplustraveler.PQFTotal = _manageResUtility.GetEarnedMilesFromCharacteristics(characteristics, codePQF, string.Empty);
                    mileageplustraveler.FormatedPQFTotal = _manageResUtility.GetFormattedMiles(mileageplustraveler.PQFTotal);

                    mileageplustraveler.PQSTotal = _manageResUtility.GetEarnedMilesFromCharacteristics(characteristics, codePQS, string.Empty);
                    mileageplustraveler.FormattedPQSTotal = _manageResUtility.GetFormattedSegment(mileageplustraveler.PQSTotal);

                    mileageplustraveler.PQPTotal = _manageResUtility.GetEarnedMilesFromCharacteristics(characteristics, codePQP, string.Empty);
                    mileageplustraveler.FormatedPQPTotal = _manageResUtility.GetFormattedMiles(mileageplustraveler.PQPTotal);

                    //Tocheck earningType
                    mileageplustraveler.EarnedMilesType = GetEarningDisplayType(pqm: mileageplustraveler.PQMTotal,
                        pqf: mileageplustraveler.PQFTotal, pqp: mileageplustraveler.PQFTotal);

                    tripnames.ForEach(trip =>
                    {
                        mileageplustraveler.LMXRows.Add(
                            new MOBLMXRow
                            {
                                AwardMiles = _manageResUtility.GetFormattedMiles(_manageResUtility.GetEarnedMilesFromCharacteristics(characteristics, codeRDM, trip.Id)),
                                PQM = _manageResUtility.GetFormattedMiles(_manageResUtility.GetEarnedMilesFromCharacteristics(characteristics, codePQM, trip.Id)),
                                PQD = _manageResUtility.GetFormattedCurrency(_manageResUtility.GetEarnedMilesFromCharacteristics(characteristics, codePQD, trip.Id)),
                                PQF = _manageResUtility.GetFormattedMiles(_manageResUtility.GetEarnedMilesFromCharacteristics(characteristics, codePQF, trip.Id)),
                                PQS = _manageResUtility.GetFormattedSegment(_manageResUtility.GetEarnedMilesFromCharacteristics(characteristics, codePQS, trip.Id)),
                                PQP = _manageResUtility.GetFormattedMiles(_manageResUtility.GetEarnedMilesFromCharacteristics(characteristics, codePQP, trip.Id)),
                                Segment = trip.CurrentValue,
                                IsEligibleSegment = true
                            });
                    });
                    return mileageplustraveler;
                }
                return null;
            }
            catch { return null; }
        }
        private MOBLMXTraveler GetNoEarningsRowMessage(MOBPNRPassenger passenger)
        {
            if (passenger == null) return null;

            var noearningmilesrows = new MOBLMXTraveler
            {
                FirstName = passenger.PassengerName.First,
                LastName = passenger.PassengerName.Last,
                HasIneligibleSegment = true,
                LMXRows = new List<MOBLMXRow> { new MOBLMXRow {
                    Segment = string.Empty,
                    IsEligibleSegment = false,
                    IneligibleSegmentMessage = "Could not retrieve any earning data."
                }}
            };
            return noearningmilesrows;
        }
        private MOBLMXTraveler GetNonMemberMessage(MOBPNRPassenger passenger)
        {
            MOBLMXTraveler nonmembertraveler = new MOBLMXTraveler();
            bool allianceProgramFound = false;
            string milesProgram = string.Empty;

            try
            {
                if (passenger == null) return null;

                if (passenger.PassengerName != null)
                {
                    nonmembertraveler.FirstName = passenger.PassengerName.First;
                    nonmembertraveler.LastName = passenger.PassengerName.Last;
                }

                nonmembertraveler.IsMPMember = false;
                nonmembertraveler.HasIneligibleSegment = false;

                if (passenger.OaRewardPrograms != null && passenger.OaRewardPrograms.Any())
                {
                    passenger.OaRewardPrograms.ForEach(airRewardPrograms =>
                    {
                        if (airRewardPrograms != null
                            && !string.IsNullOrEmpty(airRewardPrograms.VendorCode)
                            && !string.Equals(airRewardPrograms.VendorCode, "UA", StringComparison.OrdinalIgnoreCase))
                        {
                            milesProgram = airRewardPrograms.VendorDescription;
                        }
                    });

                    if (!string.IsNullOrEmpty(milesProgram))
                    {
                        nonmembertraveler.MPEliteLevelDescription = milesProgram;
                        nonmembertraveler.NonMPMemberMessage = string.Concat("This traveler is earning miles with {0}", milesProgram);
                        allianceProgramFound = true;
                    }
                }

                if (!allianceProgramFound)
                {
                    nonmembertraveler.MPEliteLevelDescription = "Non-member";
                    nonmembertraveler.NonMPMemberMessage = "Add your MileagePlus number to your reservation in order to be eligible to earn miles.\n\nNot a MileagePlus member yet? Join MileagePlus and begin earning miles.";
                }
                return nonmembertraveler;
            }
            catch { return null; }
        }
        private string GetEarningDisplayType(string pqm, string pqf, string pqp)
        {
            return string.IsNullOrEmpty(pqm) ? Convert.ToString(MOBEarningDisplayType.VBQ)
                   : (string.IsNullOrEmpty(pqf) && string.IsNullOrEmpty(pqp))
                   ? Convert.ToString(MOBEarningDisplayType.LMX) : Convert.ToString(MOBEarningDisplayType.LMXVBQ);
        }
        private async Task<MOBTrip> GetTripInfo(UAWSMileagePlusReservation.Itinerary itinerary)
        {
            MOBTrip trip = null;
            if (itinerary.ItineraryType.Equals("F"))
            {
                trip = new MOBTrip();

                string[] airportCodes = null;
                if (!string.IsNullOrEmpty(itinerary.ShortDesc))
                {
                    airportCodes = itinerary.ShortDesc.Split(new string[] { "to" }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    airportCodes = new string[2];
                    if (itinerary.FlightDetails.Segments != null && itinerary.FlightDetails.Segments[0] != null)
                    {
                        airportCodes[0] = itinerary.FlightDetails.Segments[0].Origin;
                        airportCodes[1] = itinerary.FlightDetails.Segments[0].Destination;
                    }
                }

                if (airportCodes.Length == 2)
                {
                    trip.Origin = airportCodes[0] == null ? string.Empty : airportCodes[0].Trim();
                    trip.Destination = airportCodes[1] == null ? string.Empty : airportCodes[1].Trim();
                }

                string originAirportName = string.Empty;
                string originCityName = string.Empty;
                string destinationAirportName = string.Empty;
                string destinationCityName = string.Empty;

                var tupleRes=await _airportDynamoDB.GetAirportCityName(trip.Origin, _headers.ContextValues.SessionId,  originAirportName,  originCityName);
                originAirportName = tupleRes.airportName;
                originCityName = tupleRes.cityName;
               tupleRes=await _airportDynamoDB.GetAirportCityName(trip.Destination, _headers.ContextValues.SessionId,  destinationAirportName,  destinationCityName);
                destinationAirportName = tupleRes.airportName;
                destinationCityName = tupleRes.cityName;

                trip.OriginName = originAirportName;
                trip.DestinationName = destinationAirportName;

                //string[] airportNames = itinerary.Description.Split(new string[] { " to " }, StringSplitOptions.RemoveEmptyEntries);
                //if (airportNames.Length == 2)
                //{
                //    trip.OriginName = airportNames[0] == null ? string.Empty : airportNames[0].Trim().Replace("Flight:", "");
                //    trip.DestinationName = airportNames[1] == null ? string.Empty : airportNames[1].Trim();
                //}

                DateTime departureTime;
                if (DateTime.TryParse(itinerary.TripDate, out departureTime))
                {
                    trip.DepartureTime = departureTime.ToString("MM/dd/yyyy hh:mm tt");
                    //trip.DepartureTime = departureTime.ToString("MMMM").Length > 3 ? string.Format("{0:ddd., MMM. d, yyyy h:mm tt}", departureTime) : string.Format("{0:ddd., MMM d, yyyy h:mm tt}", departureTime);
                }
            }

            return trip;
        }
        // TODO:Ancillary
        private async Task<Dictionary<string, string>> GetFarelockPNR(int applicationId, string deviceId, string transactionId, string mileagePlusNumber)
        {
            Dictionary<string, string> pnrs = null;

            string requestUrl = string.Format(_configuration.GetValue<string>("CSLReservationService - URL"), mileagePlusNumber);

            //if (this.levelSwitch.TraceError)
            //{
            //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(transactionId, "GetFarelockPNR", "Request URL", requestUrl));//Common Login Code
            //}
            //if (this.levelSwitch.TraceError)
            //{
            //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(transactionId, "GetFarelockPNR", "Request URL", requestUrl));//Common Login Code
            //}
            string token = await _dPService.GetAnonymousToken(applicationId, _headers.ContextValues.DeviceId, _configuration);

            try
            {
                var jsonResponse = await _fareLockService.GetPNRManagement(token, mileagePlusNumber, _headers.ContextValues.SessionId).ConfigureAwait(false);

                //if (this.levelSwitch.TraceError)
                //{
                //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(transactionId, "GetFarelockPNR", "Response", jsonResponse));//Common Login Code
                //}
                if (!string.IsNullOrEmpty(jsonResponse))
                {
                    List<Reservation> response = JsonConvert.DeserializeObject<List<Reservation>>(jsonResponse);
                    if (response != null && response.Count > 0)
                    {
                        foreach (var reservation in response)
                        {
                            if (reservation.Characteristic != null && reservation.Characteristic.Count > 0 && reservation.FlightSegments != null && reservation.FlightSegments.Count > 0)
                            {
                                bool currentPNR = false;
                                //foreach (var item in reservation.Characteristic)
                                //{
                                //    if (item.Code.Equals("PNR_STATUS") && item.Value.Equals("Current"))
                                //    {
                                //        currentPNR = true;
                                //        break;
                                //    }
                                //}
                                currentPNR = true;
                                if (currentPNR)
                                {
                                    foreach (var item in reservation.Characteristic)
                                    {
                                        ///184057 - Wallet: mAPP_Wallet/My Reservations Cards_FareLock timer is displayed even after full purchase and eTicketed through united.com
                                        ///Srini - 02/05/2018
                                        if (item.Code.Equals("FARELOCK_ISSUE_DATE") && !string.IsNullOrEmpty(item.Value) &&
                                            (!_configuration.GetValue<bool>("BugFixToggleFor18B") ||
                                                (_configuration.GetValue<bool>("BugFixToggleFor18B") &&
                                                    (reservation.Characteristic.ToList().Exists(c => c.Code == "HAS_ETICKET" && c.Value.ToUpper() == "FALSE") || !reservation.Characteristic.ToList().Exists(c => c.Code == "HAS_ETICKET"))
                                                )
                                            )
                                          )
                                        {
                                            if (Convert.ToDateTime(item.Value) >= DateTime.UtcNow)
                                            {
                                                if (pnrs == null)
                                                {
                                                    pnrs = new Dictionary<string, string>();
                                                }
                                                pnrs.Add(reservation.ConfirmationID, item.Value);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Added Catch Block to Capture WebException Response as part of GetFarelockPNRResponse - CSL Call Response Logging
            //catch (WebException exx)
            //{
            //    if (this.levelSwitch.TraceError)
            //    {

            //        var exReader = new StreamReader(exx.Response.GetResponseStream()).ReadToEnd();
            //        LogEntry objLog = United.Logger.LogEntry.GetLogEntry<string>(transactionId, "GetFarelockPNR", "Exception", exReader.ToString().Trim());
            //        objLog.Message = XmlSerializer.Deserialize<string>(objLog.Message);
            //        LogEntries.Add(objLog);
            //    }


            //}
            catch (System.Exception ex)
            {
                //if (this.levelSwitch.TraceError)
                //{
                //    MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                //    LogEntries.Add(United.Logger.LogEntry.GetLogEntry<MOBExceptionWrapper>(transactionId, "GetFarelockPNR", "Exception", exceptionWrapper));
                //}
            }

            return pnrs;
        }
        public async Task<MOBPNR> GetPNRByRecordLocatorFromCSL(string transactionId, string deviceId, string recordLocator, string lastName, string languageCode, int applicationId, string appVersion, bool forWallet, Session session = null)
        {
            ReservationDetail reservationDetail = new ReservationDetail();
            var tupleRes = await GetPNRByRecordLocatorFromCSL(transactionId, deviceId, recordLocator, lastName, languageCode, applicationId, appVersion, forWallet, session, reservationDetail);
            reservationDetail = tupleRes.response;
            return tupleRes.pnr;
        }

        private async Task<List<MOBLmxFlight>> GetLmxFlights(int applicationId, string deviceId, string transactionId, string recordLocator)
        {
            List<MOBLmxFlight> lmxFlights = null;

            if (!string.IsNullOrEmpty(recordLocator))
            {
                //string url = ConfigurationManager.AppSettings["ServiceEndPointBaseUrl - CSLViewResProducts"];
                string jsonRequest = "{\"RecordLocator\":\"" + recordLocator + "\"}";
                //FlightStatus flightStatus = new FlightStatus();
                string token = string.Empty;
                var response = new LmxQuoteResponse();
                try
                {
                    token = await _dPService.GetAnonymousToken(applicationId, _headers.ContextValues.DeviceId, _configuration);
                    response = await _lmxInfo.GetLmxFlight<LmxQuoteResponse>(token, jsonRequest, _headers.ContextValues.SessionId).ConfigureAwait(false);

                }
                catch (System.Exception)
                {
                    throw new Exception("Unable to retrieve LMX information");
                }

                if (response != null)
                {
                    if (response != null && response.Status.Equals(United.Services.FlightShopping.Common.StatusType.Success))
                    {
                        if (response.Flights != null && response.Flights.Count > 0)
                        {
                            if (lmxFlights == null)
                            {
                                lmxFlights = new List<MOBLmxFlight>();
                            }
                            CultureInfo ci = null;
                            foreach (var flight in response.Flights)
                            {
                                MOBLmxFlight lmxFlight = new MOBLmxFlight();
                                lmxFlight.Arrival = new MOBAirport();
                                lmxFlight.Arrival.Code = flight.Destination;
                                lmxFlight.Departure = new MOBAirport();
                                lmxFlight.Departure.Code = flight.Origin;
                                lmxFlight.FlightNumber = flight.FlightNumber;
                                lmxFlight.MarketingCarrier = new MOBAirline();
                                lmxFlight.MarketingCarrier.Code = flight.MarketingCarrier;
                                lmxFlight.ScheduledDepartureDateTime = flight.DepartDateTime;

                                if (flight.Products != null && flight.Products.Count > 0)
                                {
                                    lmxFlight.Products = new List<MOBLmxProduct>();
                                    foreach (var product in flight.Products)
                                    {
                                        MOBLmxProduct lmxProduct = new MOBLmxProduct();
                                        //lmxProduct.BookingCode = product.BookingCode;
                                        //lmxProduct.Description = product.Description;
                                        lmxProduct.ProductType = product.ProductType;
                                        if (product.LmxLoyaltyTiers != null && product.LmxLoyaltyTiers.Count > 0)
                                        {
                                            lmxProduct.LmxLoyaltyTiers = new List<MOBLmxLoyaltyTier>();
                                            foreach (var loyaltyTier in product.LmxLoyaltyTiers)
                                            {
                                                MOBLmxLoyaltyTier lmxLoyaltyTier = new MOBLmxLoyaltyTier();
                                                lmxLoyaltyTier.Description = loyaltyTier.Descr;
                                                lmxLoyaltyTier.Key = loyaltyTier.Key;
                                                lmxLoyaltyTier.Level = loyaltyTier.Level;
                                                if (loyaltyTier.LmxQuotes != null && loyaltyTier.LmxQuotes.Count > 0)
                                                {
                                                    lmxLoyaltyTier.LmxQuotes = new List<MOBLmxQuote>();
                                                    foreach (var quote in loyaltyTier.LmxQuotes)
                                                    {
                                                        if (ci == null)
                                                            TopHelper.GetCultureInfo(quote.Currency);
                                                        MOBLmxQuote lmxQuote = new MOBLmxQuote();
                                                        lmxQuote.Amount = quote.Amount;
                                                        lmxQuote.Currency = quote.Currency;
                                                        lmxQuote.Description = quote.Descr;
                                                        lmxQuote.Type = quote.Type;
                                                        lmxQuote.DblAmount = Double.Parse(quote.Amount);
                                                        if (ci != null)
                                                        {
                                                            lmxQuote.Currency = TopHelper.GetCurrencySymbol(ci);
                                                        }

                                                        lmxLoyaltyTier.LmxQuotes.Add(lmxQuote);
                                                    }
                                                }
                                                lmxProduct.LmxLoyaltyTiers.Add(lmxLoyaltyTier);
                                            }
                                        }
                                        lmxFlight.Products.Add(lmxProduct);
                                    }
                                }

                                lmxFlights.Add(lmxFlight);
                            }
                        }
                    }
                }
            }

            return lmxFlights;
        }


        public MOBMPPNRs GetPNRsByMileagePlusNumberCSL(string transactionId, string mileagePlusNumber, MOBReservationType reservationType, string languageCode, bool includeFarelockInfo, string appVersion, bool forWallet, int applicationId, bool getEmployeeIdFromCSLCustomerData)
        {
            //CSL call to get the top 100 current pnr's 

            #region Calling CSL service

            //TODO CSL call
            //string requestUrl = Utility.GetConfigEntries("CSLReservationService - MileagePlusSearchURL");
            //string jsonRequest = JsonConvert.SerializeObject(GetMileageSearchRequest(mileagePlusNumber, string.Empty, reservationType));

            //string token = new GetFLIFOSecurityTokenCSSCall(applicationId, transactionId, transactionId);

            //MOBApplication application = new MOBApplication()
            //{
            //    Id = applicationId,
            //    Version = new MOBVersion()
            //    {
            //        Major = appVersion,
            //        Minor = appVersion,
            //    },
            //    Name = Utility.GetCSSAppIDForMobileApp(applicationId)
            //};
            //string jsonResponse = MakeHTTPCallAndLogIt(transactionId, string.Empty, "GetPNRsByMileagePlusNumberCSL - CSL call", application, token, requestUrl, jsonRequest, false);

            //#endregion //Calling CSL service

            //MOBMPPNRs opPNRs = null;

            //if (!string.IsNullOrEmpty(jsonResponse))
            //{
            //    List<Reservation> jsonReservationResponse = JsonConvert.DeserializeObject<List<Reservation>>(jsonResponse);

            //    if (jsonReservationResponse != null)
            //    {
            //        if (jsonReservationResponse.Count != 0)
            //        {
            //            if (opPNRs == null)
            //            {
            //                opPNRs = new MOBMPPNRs();
            //            }
            //            opPNRs.MileagePlusNumber = mileagePlusNumber;
            //            GetPNRs(ref opPNRs, jsonReservationResponse, languageCode, true, applicationId, appVersion, reservationType, true);
            //            #region

            //            switch (reservationType)
            //            {
            //                case MOBReservationType.Current:

            //                    bool flagToCheckOtherService = false;
            //                    if ((opPNRs.Current == null) ||
            //                        (opPNRs.Current != null && opPNRs.Current.Count == 0))
            //                    {
            //                        flagToCheckOtherService = true;
            //                    }


            //                    if (flagToCheckOtherService && _configuration.GetValue<bool>("ReturnNonRevPNRs"))
            //                    {
            //                        try
            //                        {
            //                            MileagePlus mileagePlus = new MileagePlus();

            //                            string displayEmployeeId = string.Empty;
            //                            string employeeId = string.Empty;
            //                            employeeId = (getEmployeeIdFromCSLCustomerData)
            //                                ? GetEmployeeIdFromCSL(transactionId, mileagePlusNumber, languageCode, appVersion, applicationId, "", getEmployeeIdFromCSLCustomerData)
            //                                : GetEmployeeIdy(transactionId, mileagePlusNumber, ref displayEmployeeId);


            //                            if (!string.IsNullOrEmpty(employeeId))
            //                            {
            //                                List<MOBEmployeePNR> employeePNRList = GetEmployeePNRs(transactionId, employeeId);

            //                                if (employeePNRList != null && employeePNRList.Count > 0)
            //                                {
            //                                    //List<MOBPNR> employeePNRs = new List<MOBPNR>();
            //                                    if (opPNRs == null)
            //                                    {
            //                                        opPNRs = new MOBMPPNRs();
            //                                    }
            //                                    if (opPNRs.Current == null)
            //                                    {
            //                                        opPNRs.Current = new List<MOBPNR>();
            //                                    }

            //                                    Parallel.ForEach(employeePNRList, item =>
            //                                    {
            //                                        MOBPNR aPNR = new MOBPNR();
            //                                        try
            //                                        {
            //                                            //View Res XML to CSL migration
            //                                            bool isCSLPNRServiceOn = Convert.ToBoolean(_configuration.GetValue<string>("SwithToCSLPNRService") ?? "false");
            //                                            if (!isCSLPNRServiceOn)
            //                                            {

            //                                                aPNR = GetPNRByRecordLocator(transactionId, item.RecordLocator,
            //                                                    item.LastName, "en-US", 1, appVersion, forWallet);
            //                                            }
            //                                            else
            //                                            {
            //                                                aPNR = GetPNRByRecordLocatorFromCSL(transactionId, "", item.RecordLocator,
            //                                                    item.LastName, "en-US", applicationId, appVersion, forWallet);

            //                                            }
            //                                            if (aPNR != null)
            //                                            {
            //                                                opPNRs.Current.Add(aPNR);
            //                                            }
            //                                        }
            //                                        catch (System.Exception) { }
            //                                    });
            //                                }
            //                            }
            //                        }
            //                        catch (System.Exception) { }
            //                    }

            //                    if (opPNRs == null || opPNRs.Current == null || opPNRs.Current.Count == 0)
            //                    {
            //                        throw new MOBUnitedException("No current reservation found");
            //                    }
            //                    else
            //                    {
            //                        if (IncludeFareLock(includeFarelockInfo, appVersion))
            //                        {
            //                            Dictionary<string, string> farelockPNRs = GetFarelockPNR(1, string.Empty, transactionId, mileagePlusNumber);
            //                            if (farelockPNRs != null && farelockPNRs.Count > 0)
            //                            {
            //                                foreach (var pnr in opPNRs.Current)
            //                                {
            //                                    foreach (var farelockPNR in farelockPNRs)
            //                                    {
            //                                        if (pnr.RecordLocator.Equals(farelockPNR.Key))
            //                                        {
            //                                            pnr.FarelockExpirationDate = farelockPNR.Value;
            //                                        }
            //                                    }
            //                                }
            //                            }
            //                        }
            //                    }

            //                    break;
            //                case MOBReservationType.Past:
            //                    if ((opPNRs.Past == null) ||
            //                        (opPNRs.Past != null && opPNRs.Past.Count == 0))
            //                    {
            //                        throw new MOBUnitedException("No past reservation found");
            //                    }

            //                    break;
            //                case MOBReservationType.Cancelled:
            //                    if (opPNRs.Cancelled != null && opPNRs.Cancelled.Count > 0)
            //                    {
            //                        opPNRs.Cancelled.Sort(p => p.DateCreated);
            //                    }
            //                    else
            //                    {
            //                        throw new MOBUnitedException("No canceled reservation found");
            //                    }

            //                    break;
            //                case MOBReservationType.Inactive:
            //                    if ((opPNRs.Inactive == null) ||
            //                        (opPNRs.Inactive != null && opPNRs.Inactive.Count == 0))
            //                    {
            //                        throw new MOBUnitedException("No inactive reservation found");
            //                    }
            //                    break;
            //                default:

            //                    if (opPNRs.Past != null && opPNRs.Past.Count > 0)
            //                    {
            //                        opPNRs.Past.SortDescending(pnr => Convert.ToDateTime(pnr.DateCreated));
            //                    }
            //                    if (opPNRs.Cancelled != null && opPNRs.Cancelled.Count > 0)
            //                    {
            //                        opPNRs.Cancelled.SortDescending(pnr => Convert.ToDateTime(pnr.DateCreated));
            //                    }

            //                    if (_configuration.GetValue<bool>("ReturnNonRevPNRs"))
            //                    {
            //                        try
            //                        {
            //                            MileagePlus mileagePlus = new MileagePlus();
            //                            string displayEmployeeId = string.Empty;
            //                            string employeeId = string.Empty;
            //                            employeeId = (getEmployeeIdFromCSLCustomerData)
            //                                ? GetEmployeeIdFromCSL(transactionId, mileagePlusNumber, languageCode, appVersion, applicationId, "", getEmployeeIdFromCSLCustomerData)
            //                                : GetEmployeeIdy(transactionId, mileagePlusNumber, ref displayEmployeeId);
            //                            if (!string.IsNullOrEmpty(employeeId))
            //                            {
            //                                List<MOBEmployeePNR> employeePNRList = GetEmployeePNRs(transactionId, employeeId);

            //                                if (employeePNRList != null && employeePNRList.Count > 0)
            //                                {
            //                                    //List<MOBPNR> employeePNRs = new List<MOBPNR>();
            //                                    if (opPNRs == null)
            //                                    {
            //                                        opPNRs = new MOBMPPNRs();
            //                                    }
            //                                    if (opPNRs.Current == null)
            //                                    {
            //                                        opPNRs.Current = new List<MOBPNR>();
            //                                    }

            //                                    //foreach (var item in employeePNRList)
            //                                    //{
            //                                    //    MOBPNR aPNR = GetPNRByRecordLocator(transactionId, item.RecordLocator, item.LastName, "en-US", 1);
            //                                    //    if (aPNR != null)
            //                                    //    {
            //                                    //        opPNRs.Current.Add(aPNR);
            //                                    //    }
            //                                    //}

            //                                    Parallel.ForEach(employeePNRList, item =>
            //                                    {
            //                                        MOBPNR aPNR = new MOBPNR();
            //                                        try
            //                                        {
            //                                            //View Res XML to CSL migration
            //                                            bool isCSLPNRServiceOn = Convert.ToBoolean(_configuration.GetValue<string>("SwithToCSLPNRService") ?? "false");
            //                                            if (!isCSLPNRServiceOn)
            //                                            {
            //                                                aPNR = GetPNRByRecordLocator(transactionId, item.RecordLocator,
            //                                                    item.LastName, "en-US", 1, appVersion, forWallet);
            //                                            }
            //                                            else
            //                                            {
            //                                                aPNR = GetPNRByRecordLocatorFromCSL(transactionId, "", item.RecordLocator,
            //                                                    item.LastName, "en-US", applicationId, appVersion, forWallet);
            //                                            }
            //                                            if (aPNR != null)
            //                                            {
            //                                                opPNRs.Current.Add(aPNR);
            //                                            }
            //                                        }
            //                                        catch (System.Exception ex) { Debug.WriteLine(ex.Message); }
            //                                    });
            //                                }
            //                            }
            //                        }
            //                        catch (System.Exception ex) { Debug.WriteLine(ex.Message); }
            //                    }


            //                    if (opPNRs == null || (opPNRs.Current == null && opPNRs.Cancelled == null && opPNRs.Past == null && opPNRs.Inactive == null))
            //                    {
            //                        throw new MOBUnitedException("No reservation found");
            //                    }

            //                    break;
            //            }

            //            #endregion

            //        }
            //        else
            //        {
            //            throw new MOBUnitedException("No reservation found");
            //        }
            //    }
            //    else
            //    {
            //        throw new MOBUnitedException(_configuration.GetValue<string>("GenericExceptionMessage"));
            //    }

            //    _logger.LogInformation("GetPNRsByMileagePlusNumberResponse {Response}", opPNRs);//Common Login Code
            //}

            //return opPNRs;

            return default;
        }


        private void GetPNRs(ref MOBMPPNRs opPNRs, List<Reservation> reservationsList, string languageCode, bool includeTrip, int applicationId, string appVersion, MOBReservationType reservationType, bool includeFarelockInfo = false)
        {

            if (reservationsList != null)
            {
                bool _includeFarelock = IncludeFareLock(includeFarelockInfo, appVersion);
                bool _FlagToMoveCSLInactivePNRsToCancelList = _configuration.GetValue<bool>("FlagToMoveCSLInactivePNRsToCancelList");
                foreach (Reservation singleReservation in reservationsList)
                {
                    string _pnrStatus = string.Empty;   //Get PNR status
                    string _farelock_issue_date = string.Empty;   //Get farelock date
                    string _has_eticket = string.Empty;   //Get eticket status


                    if (singleReservation.Characteristic != null && singleReservation.Characteristic.Count > 0)
                    {
                        try
                        {
                            foreach (Characteristic element in singleReservation.Characteristic)
                            {
                                if (element.Code.Equals("PNR_STATUS", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    _pnrStatus = element.Value.Trim();
                                }
                                else if (element.Code.Equals("FARELOCK_ISSUE_DATE", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    _farelock_issue_date = element.Value.Trim();
                                }
                                else if (element.Code.Equals("HAS_ETICKET", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    _has_eticket = element.Value.Trim();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }

                    if (string.IsNullOrEmpty(_pnrStatus) ||
                        (reservationType != MOBReservationType.All &&
                        !reservationType.ToString().ToLower().Equals(_pnrStatus.ToLower())))
                    {
                        continue;
                    }

                    bool _includeTrip = (_pnrStatus.ToLower().Equals("current")) ? true : false;

                    #region Prepare PNR
                    MOBPNR pnr = new MOBPNR();
                    pnr.DateCreated = GeneralHelper.FormatDatetime(Convert.ToDateTime(singleReservation.CreateDate).ToString("yyyyMMdd hh:mm tt"), languageCode);
                    pnr.RecordLocator = singleReservation.ConfirmationID;
                    pnr.FarelockExpirationDate = (_pnrStatus.ToLower().Equals("current") && _includeFarelock) ? _farelock_issue_date : string.Empty;

                    //pnr.AwardTravel = itinerary.FlightDetails.RewardTravel;  //Old XML service providing RewardTravel, In new CSL service not found
                    pnr.IsActive = Convert.ToBoolean(_has_eticket);
                    pnr.NumberOfPassengers = (singleReservation.Travelers != null) ? singleReservation.Travelers.Count.ToString() : "0";

                    pnr.Description = singleReservation.NickName;

                    if (singleReservation.Travelers == null)
                    {
                        throw new MOBUnitedException("Unable to retrieve traveler information.");
                    }

                    pnr.Passengers = new List<MOBPNRPassenger>();
                    foreach (Traveler traveler in singleReservation.Travelers)
                    {
                        #region //CSL serivce not seen PsSaTravel details for the traveler
                        //if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["EnableEResBooking"]) && Convert.ToBoolean(ConfigurationManager.AppSettings["EnableEResBooking"]))
                        //{
                        //    if (!pnr.PsSaTravel && !string.IsNullOrEmpty(traveler.PassengerIndicator) && (traveler.PassengerIndicator.Equals("SA") || traveler.PassengerIndicator.Equals("PS")))
                        //    {
                        //        if (IsEResBetaTesterRecordLocator(pnr.RecordLocator, applicationId, applicationVersion))
                        //        {
                        //            pnr.PsSaTravel = true;
                        //        }
                        //    }
                        //    else if (!pnr.PsSaTravel && !string.IsNullOrEmpty(itinerary.FlightDetails.EmployeeId))
                        //    {
                        //        if (IsEResBetaTesterRecordLocator(pnr.RecordLocator, applicationId, applicationVersion))
                        //        {
                        //            pnr.PsSaTravel = true;
                        //        }
                        //    }
                        //}
                        #endregion
                        MOBPNRPassenger pnrPassenger = new MOBPNRPassenger();
                        pnrPassenger.PassengerName = new MOBName();
                        pnrPassenger.PassengerName.First = traveler.Person.GivenName;
                        pnrPassenger.PassengerName.Middle = traveler.Person.MiddleName;
                        pnrPassenger.PassengerName.Last = traveler.Person.Surname;
                        pnrPassenger.PassengerName.Suffix = traveler.Person.Suffix;
                        pnrPassenger.PassengerName.Title = traveler.Person.Title;
                        pnrPassenger.SHARESPosition = traveler.Person.Key;

                        #region Commenting out the lines, not getting MP Member
                        //if (ConfigurationManager.AppSettings["EnableTravelReadiness"] != null && Convert.ToBoolean(ConfigurationManager.AppSettings["EnableTravelReadiness"].ToString()))
                        //{
                        //    pnrPassenger.IsMPMember = (traveler.RewardPrograms != null && traveler.RewardPrograms.Any(p => p.ProgramMemberId == mpNumber)) ? true : false;
                        //}
                        #endregion

                        pnr.Passengers.Add(pnrPassenger);
                    }

                    if (singleReservation.FlightSegments == null)
                    {
                        throw new MOBUnitedException("Unable to retrieve segment information.");
                    }

                    string scheduledDepartureDateForLastSegment = string.Empty;
                    string scheduledDepartureGMTDateForLastSegment = string.Empty;
                    string scheduledArrivalDateForLastSegment = string.Empty;


                    pnr.Segments = new List<MOBPNRSegment>();
                    foreach (ReservationFlightSegment segment in singleReservation.FlightSegments)
                    {
                        MOBPNRSegment pnrSegment = new MOBPNRSegment();

                        ////Segment - Arrival details
                        pnrSegment.Arrival = new MOBAirport();
                        pnrSegment.Arrival.Code = segment.FlightSegment?.ArrivalAirport?.IATACode;
                        string airportName = string.Empty;
                        string cityName = string.Empty;
                        _shoppingUtility.GetAirportCityName(pnrSegment.Arrival.Code, ref airportName, ref cityName);

                        pnrSegment.Arrival.Name = airportName;
                        pnrSegment.Arrival.City = cityName;

                        pnrSegment.ClassOfService = segment.FlightSegment?.BookingClasses[0]?.Code;
                        //pnrSegment.ClassOfServiceDescription = segment.ServiceClassDescription;
                        //Please do not un-comment the following line. .COM returns the incorrect COS description
                        pnrSegment.ClassOfServiceDescription = "--";

                        ////Segment - Departure details
                        pnrSegment.Departure = new MOBAirport();
                        pnrSegment.Departure.Code = segment.FlightSegment.DepartureAirport.IATACode;
                        airportName = string.Empty;
                        cityName = string.Empty;
                        _shoppingUtility.GetAirportCityName(pnrSegment.Departure.Code, ref airportName, ref cityName);
                        pnrSegment.Departure.Name = airportName;
                        pnrSegment.Departure.City = cityName;

                        ////Segment - Flight Number
                        pnrSegment.FlightNumber = segment.FlightSegment.FlightNumber;

                        ////Segment - Marketing Carrier Details
                        pnrSegment.MarketingCarrier = new MOBAirline();
                        pnrSegment.MarketingCarrier.Code = segment.FlightSegment.OperatingAirlineCode;
                        pnrSegment.MarketingCarrier.Name = _manageResUtility.GetCarrierInfo(pnrSegment.MarketingCarrier.Code);
                        pnrSegment.TripNumber = segment.TripNumber;
                        pnrSegment.SegmentNumber = segment.SegmentNumber;

                        //pnrSegment.CabinType = segment.CabinTypeText;  //Cabin Type not seen in CSL service

                        if (segment.FlightSegment.ArrivalDateTime != null && segment.FlightSegment.ArrivalDateTime.Trim().Length != 0)
                        {
                            pnrSegment.ScheduledArrivalDateTime = GeneralHelper.FormatDatetime(Convert.ToDateTime(segment.FlightSegment.ArrivalDateTime).ToString("yyyyMMdd hh:mm tt"), languageCode);
                            scheduledArrivalDateForLastSegment = pnrSegment.ScheduledArrivalDateTime;
                        }
                        else
                        {
                            pnrSegment.ScheduledArrivalDateTime = scheduledArrivalDateForLastSegment;
                        }

                        DateTime scheduledDepartureDateTimeGMT;
                        if (segment.FlightSegment.DepartureDateTime != null && segment.FlightSegment.DepartureDateTime.Trim().Length != 0)
                        {
                            pnrSegment.ScheduledDepartureDateTime = GeneralHelper.FormatDatetime(Convert.ToDateTime(segment.FlightSegment.DepartureDateTime).ToString("yyyyMMdd hh:mm tt"), languageCode);
                            pnrSegment.ScheduledDepartureDateTimeGMT = ConfigUtility.GetGMTTime(pnrSegment.ScheduledDepartureDateTime, pnrSegment.Departure.Code);
                            pnrSegment.ScheduledArrivalDateTimeGMT = ConfigUtility.GetGMTTime(pnrSegment.ScheduledArrivalDateTime, pnrSegment.Arrival.Code);

                            scheduledDepartureDateForLastSegment = pnrSegment.ScheduledDepartureDateTime;
                            scheduledDepartureGMTDateForLastSegment = pnrSegment.ScheduledDepartureDateTimeGMT;

                            if (DateTime.TryParse(pnrSegment.ScheduledDepartureDateTimeGMT, out scheduledDepartureDateTimeGMT))
                            {
                                if (DateTime.UtcNow > scheduledDepartureDateTimeGMT.AddDays(-1) && DateTime.UtcNow <= scheduledDepartureDateTimeGMT)
                                {
                                    pnr.CheckinEligible = "Y";
                                }

                            }
                        }
                        else
                        {
                            pnrSegment.ScheduledDepartureDateTime = scheduledDepartureDateForLastSegment;
                            pnrSegment.ScheduledDepartureDateTimeGMT = scheduledDepartureGMTDateForLastSegment;
                        }

                        if (segment.Seat != null && !string.IsNullOrEmpty(segment.Seat.SeatClass))
                        {
                            pnrSegment.Seats = new List<MOBPNRSeat>();
                            string[] seatNumbers = segment.Seat.SeatClass.Split('*');

                            #region seat assignment
                            foreach (string seat in seatNumbers)
                            {
                                if (seat.Trim().Length > 0)
                                {
                                    MOBPNRSeat pnrSeat = new MOBPNRSeat();
                                    pnrSeat.Number = seat;
                                    //pnrSeat.SeatRow = seat.SeatRow;
                                    //pnrSeat.SeatLetter = seat.SeatLetter;
                                    //pnrSeat.PassengerSHARESPosition = seat.TravelerSharesIndex;

                                    //pnrSeat.SeatStatus = seat.SeatStatus;
                                    pnrSeat.SegmentIndex = segment.SegmentNumber.ToString();
                                    pnrSeat.Origin = pnrSegment.Departure.Code;
                                    pnrSeat.Destination = pnrSegment.Arrival.Code;
                                    //pnrSeat.EddNumber = seat.EddNumber;
                                    //pnrSeat.EDocId = seat.EDocId;
                                    //double price = 0.0;
                                    //bool ok = Double.TryParse(seat.Price, out price);
                                    //if (ok)
                                    //{
                                    //    pnrSeat.Price = price;
                                    //}
                                    //pnrSeat.Currency = seat.Currency;
                                    //pnrSeat.ProgramCode = seat.ProgramCode;

                                    pnrSegment.Seats.Add(pnrSeat);
                                }
                            }
                            #endregion seat assignment
                        }

                        #region stopinfo  //CSL service not found the mapping fields
                        pnrSegment.NumberOfStops = "0";
                        //if (segment.StopInfos != null)
                        //{
                        //    pnrSegment.NumberOfStops = segment.Stops.ToString();

                        //    pnrSegment.Stops = new List<MOBPNRSegment>();
                        //    foreach (UAWSMileagePlusReservation.Segment stopSegment in segment.StopInfos)
                        //    {
                        //        MOBPNRSegment ss = new MOBPNRSegment();
                        //        ss.Arrival = new MOBAirport();
                        //        ss.Arrival.Code = stopSegment.Destination;
                        //        //ss.Arrival.Name = stopSegment.DecodedDestination;
                        //        string airportName1 = string.Empty;
                        //        string cityName1 = string.Empty;
                        //        Utility.GetAirportCityName(stopSegment.Destination, ref airportName1, ref cityName1);
                        //        ss.Arrival.Name = airportName1;
                        //        ss.Arrival.City = cityName1;

                        //        ss.ClassOfService = stopSegment.OriginalServiceClass;

                        //        ss.Departure = new MOBAirport();
                        //        ss.Departure.Code = stopSegment.Origin;
                        //        //ss.Departure.Name = stopSegment.DecodedOrigin;
                        //        airportName1 = string.Empty;
                        //        cityName1 = string.Empty;
                        //        Utility.GetAirportCityName(stopSegment.Origin, ref airportName1, ref cityName1);
                        //        ss.Departure.Name = airportName1;
                        //        ss.Departure.City = cityName1;
                        //        ss.FlightNumber = stopSegment.FlightNumber;
                        //        ss.MarketingCarrier.Code = stopSegment.CarrierCode;
                        //        ss.MarketingCarrier.Code = stopSegment.CarrierDescription;

                        //        ss.CabinType = stopSegment.CabinTypeText;

                        //        if (stopSegment.DestinationDate != null && stopSegment.DestinationDate.Trim().Length != 0)
                        //        {
                        //            ss.ScheduledArrivalDateTime = Utility.FormatDatetime(Convert.ToDateTime(stopSegment.DestinationDate).ToString("yyyyMMdd hh:mm tt"), languageCode);
                        //            scheduledArrivalDateForLastSegment = ss.ScheduledArrivalDateTime;
                        //        }
                        //        else
                        //        {
                        //            ss.ScheduledArrivalDateTime = scheduledArrivalDateForLastSegment;
                        //            //throw new ContinentalException("We were unable to review the latest information for this itinerary.");
                        //        }

                        //        if (stopSegment.DepartDate != null && stopSegment.DepartDate.Trim().Length != 0)
                        //        {
                        //            ss.ScheduledDepartureDateTime = Utility.FormatDatetime(Convert.ToDateTime(stopSegment.DepartDate).ToString("yyyyMMdd hh:mm tt"), languageCode);
                        //            ss.ScheduledDepartureDateTimeGMT = GetGMTTime(ss.ScheduledDepartureDateTime, ss.Departure.Code);
                        //            scheduledDepartureDateForLastSegment = ss.ScheduledDepartureDateTime;
                        //            scheduledDepartureGMTDateForLastSegment = ss.ScheduledDepartureDateTimeGMT;

                        //            if (DateTime.TryParse(ss.ScheduledDepartureDateTimeGMT, out scheduledDepartureDateTimeGMT))
                        //            {
                        //                if (DateTime.UtcNow > scheduledDepartureDateTimeGMT.AddDays(-1) && DateTime.UtcNow <= scheduledDepartureDateTimeGMT)
                        //                {
                        //                    pnr.CheckinEligible = "Y";
                        //                }

                        //            }
                        //        }
                        //        else
                        //        {
                        //            ss.ScheduledDepartureDateTime = scheduledDepartureDateForLastSegment;
                        //            ss.ScheduledDepartureDateTimeGMT = scheduledDepartureGMTDateForLastSegment;
                        //            //throw new ContinentalException("We were unable to review the latest information for this itinerary.");
                        //        }

                        //        if (stopSegment.Seats != null)
                        //        {
                        //            ss.Seats = new List<MOBPNRSeat>();

                        //            foreach (UAWSMileagePlusReservation.Seat seat in stopSegment.Seats)
                        //            {
                        //                MOBPNRSeat pnrSeat = new MOBPNRSeat();
                        //                pnrSeat.Number = seat.SeatAssignment;
                        //                pnrSeat.SeatRow = seat.SeatRow;
                        //                pnrSeat.SeatLetter = seat.SeatLetter;
                        //                pnrSeat.PassengerSHARESPosition = seat.TravelerSharesIndex;

                        //                pnrSeat.SeatStatus = seat.SeatStatus;
                        //                pnrSeat.SegmentIndex = stopSegment.Index.ToString();
                        //                pnrSeat.Origin = ss.Departure.Code;
                        //                pnrSeat.Destination = ss.Arrival.Code;
                        //                pnrSeat.EddNumber = seat.EddNumber;
                        //                pnrSeat.EDocId = seat.EDocId;
                        //                double price = 0.0;
                        //                bool ok = Double.TryParse(seat.Price, out price);
                        //                if (ok)
                        //                {
                        //                    pnrSeat.Price = price;
                        //                }
                        //                pnrSeat.Currency = seat.Currency;
                        //                pnrSeat.ProgramCode = seat.ProgramCode;

                        //                ss.Seats.Add(pnrSeat);
                        //            }
                        //        }

                        //        pnrSegment.Stops.Add(ss);
                        //    }
                        //}
                        #endregion stopinfo

                        pnr.Segments.Add(pnrSegment);

                    }
                    #endregion Prepare PNR


                    if (_includeTrip)
                    {
                        pnr.Trips = new List<MOBTrip>();
                        pnr.Trips.Add(GetTripInfoCSLV2(singleReservation.FlightSegments, singleReservation.NickName));
                    }

                    //eQA Observation:#2: FFC PNRs are not displayed in CSL, where as displayed in XML
                    //Fix: In CSL service, FFC PNRs are seeing as Inactive PNR_Status. So, moving those
                    //     PNR to cancelled list instead of different Inactive list. Moving those PNRs
                    //     based on config value "FlagToMoveCSLInactivePNRsToCancelList"
                    string _pnrStatusToSwitchCondition = (_FlagToMoveCSLInactivePNRsToCancelList && _pnrStatus.ToLower() == "inactive") ? "cancelled" : _pnrStatus.ToLower();
                    switch (_pnrStatusToSwitchCondition)
                    {
                        case "cancelled":
                            if (opPNRs.Cancelled == null)
                            {
                                opPNRs.Cancelled = new List<MOBPNR>();
                            }
                            opPNRs.Cancelled.Add(pnr);
                            break;
                        case "past":
                            if (opPNRs.Past == null)
                            {
                                opPNRs.Past = new List<MOBPNR>();
                            }
                            opPNRs.Past.Add(pnr);
                            break;
                        case "current":
                            if (opPNRs.Current == null)
                            {
                                opPNRs.Current = new List<MOBPNR>();
                            }
                            opPNRs.Current.Add(pnr);
                            break;
                        case "inactive":

                            if (opPNRs.Inactive == null)
                            {
                                opPNRs.Inactive = new List<MOBPNR>();
                            }
                            opPNRs.Inactive.Add(pnr);
                            break;
                    }
                }
            }

        }

        public string GetEmployeeIdy(string transactionId, string mileagePlusNumber, ref string displayEmployeeId)
        {
            string employeeId = string.Empty;

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    int timeout = Convert.ToInt32(_configuration.GetValue<string>("HTTPClientTimeOut-LoyaltyWebserviceGetEmployeeId"));
                    //client.TransportSettings.ConnectionTimeout = new TimeSpan(0, 0, timeout);
                    //client.TransportSettings.ReadWriteTimeout = new TimeSpan(0, 0, timeout);

                    HttpRequestMessage request = new HttpRequestMessage();
                    string url = string.Format(_configuration.GetValue<string>("LoyaltyWebserviceGetEmployeeIdURL"), mileagePlusNumber);
                    //TODO-CSL
                    //request.Uri = new Uri(url);

                    //_logger.LogInformation("LoyaltyWebserviceGetEmployeeIdURL {Url} and {transactionId}", url, transactionId);//Common Login Code
                    //    //                        LogEntries.Add(United.Logger.LogEntry.GetLogEntry<HttpRequestMessage>(transactionId, "LoyaltyWebserviceGetEmployeeIdRequest", "Request", request));//Common Login Code

                    //using (HttpResponseMessage response = client.Send(request))
                    //{
                    //    response.EnsureStatusIsSuccessful();
                    //    string responseString = response.Content.ReadAsString();
                    //    //employeeId = responseString.Substring(15, responseString.Length - 17);
                    //    GetEmpIdByMpNumber empResponse = JsonConvert.DeserializeObject<GetEmpIdByMpNumber>(responseString);
                    //    if (empResponse.MPLinkedId != null)
                    //    {
                    //        displayEmployeeId = empResponse.FileNumber;
                    //        employeeId = empResponse.MPLinkedId;
                    //    }
                    //    else
                    //    {
                    //        displayEmployeeId = empResponse.EmployeeId;
                    //        employeeId = empResponse.EmployeeId;
                    //    }
                    //    if (employeeId.Equals("null"))
                    //    {

                    //        displayEmployeeId = string.Empty;
                    //        employeeId = string.Empty;
                    //    }
                    //}
                }

            }
            //catch (System.Exception ex) { Debug.WriteLine(ex); }
            //NotFound (404) is not one of the following: OK (200), Created (201), Accepted (202), NonAuthoritativeInformation (203), NoContent (204), ResetContent (205), PartialContent (206)
            catch (System.Exception ex)
            {
                if (ex.Message != null && ex.Message.LastIndexOf("404") > 1)
                {
                    employeeId = string.Empty;
                }
            }

            _logger.LogInformation("LoyaltyWebserviceGetEmployeeIdResponse {Response} and {transactionId}", employeeId, transactionId);//Common Login Code

            return employeeId;
        }


        public MOBTrip GetTripInfoCSLV2(Collection<ReservationFlightSegment> flightSegments, string nickName = "")
        {
            MOBTrip trip = new MOBTrip();
            if (flightSegments != null && flightSegments.Count > 0)
            {
                int mintripnumber = Convert.ToInt32(flightSegments.Select(o => o.TripNumber).FirstOrDefault());
                int maxtripnumber = Convert.ToInt32(flightSegments.Select(o => o.TripNumber).LastOrDefault());

                for (int i = mintripnumber; i <= maxtripnumber; i++)
                {

                    var totalTripSegments = flightSegments.Where(o => o.TripNumber == i.ToString());


                    foreach (ReservationFlightSegment segment in flightSegments)
                    {
                        if (!string.IsNullOrEmpty(segment.TripNumber) && Convert.ToInt32(segment.TripNumber) == i)
                        {
                            string airportName = string.Empty;
                            string cityName = string.Empty;

                            if (segment.SegmentNumber == totalTripSegments.Min(x => x.SegmentNumber)) // If segment.SegmentNumber is first segment of trip i
                            {
                                trip.Origin = segment.FlightSegment.DepartureAirport.IATACode;
                            }
                            if (segment.SegmentNumber == totalTripSegments.Max(x => x.SegmentNumber)) // If segment.SegmentNumber is last segment of trip i
                            {
                                trip.Destination = segment.FlightSegment.ArrivalAirport.IATACode;
                            }

                        }
                    }

                    if (!string.IsNullOrEmpty(trip.Origin) && !string.IsNullOrEmpty(trip.Destination))
                    {
                        // Found the current trip's origin and destination.
                        break;
                    }

                }
            }

            string originAirportName = string.Empty;
            string originCityName = string.Empty;
            string destinationAirportName = string.Empty;
            string destinationCityName = string.Empty;

            _shoppingUtility.GetAirportCityName(trip.Origin, ref originAirportName, ref originCityName);
            _shoppingUtility.GetAirportCityName(trip.Destination, ref destinationAirportName, ref destinationCityName);

            trip.OriginName = originAirportName;
            trip.DestinationName = destinationAirportName;

            DateTime departureTime;

            if (flightSegments != null && flightSegments.Count > 0 && flightSegments[0] != null && flightSegments[0].FlightSegment != null &&
                string.IsNullOrEmpty(flightSegments[0].FlightSegment.DepartureDateTime))
            {
                if (DateTime.TryParse(flightSegments[0].FlightSegment.DepartureDateTime, out departureTime))
                {
                    trip.DepartureTime = departureTime.ToString("MM/dd/yyyy hh:mm tt");
                }
            }

            return trip;
        }

        public MOBPNRAdvisory PopulateScheduleChangeNoProtection()
        {
            try
            {
                string[] messageItems = ConfigUtility.SplitConcatenatedConfigValue("SCHEDULECHANGE_NOPROTECTION_ALERT", "||");
                if (messageItems == null && !messageItems.Any()) return null;
                MOBPNRAdvisory content = new MOBPNRAdvisory
                {
                    Header = messageItems[0],
                    Body = messageItems[1],
                    IsBodyAsHtml = true,
                };
                return content;
            }
            catch { return null; }
        }


        #region //Utilty


        #endregion
        #endregion

        public MOBPNRAdvisory PopulateConfigContent(string displaycontent, string splitchar)
        {
            try
            {
                string[] splitSymbol = { splitchar };

                string configentry = _configuration.GetValue<string>(displaycontent);

                if (string.IsNullOrEmpty(configentry)) return null;

                string[] items = configentry.Split(splitSymbol, StringSplitOptions.None);

                if (items == null || !items.Any()) return null;

                MOBPNRAdvisory content = new MOBPNRAdvisory();

                items.ToList().ForEach(item =>
                {
                    if (!string.IsNullOrEmpty(item))
                    {
                        string[] itemcontent = item.Split('|');

                        if (itemcontent != null && itemcontent.Length >= 2)
                        {
                            switch (itemcontent[0])
                            {
                                case "header":
                                    content.Header = itemcontent[1];
                                    break;
                                case "body":
                                    content.Body = itemcontent[1];
                                    break;
                                case "buttontext":
                                    content.Buttontext = itemcontent[1];
                                    break;
                                case "buttonlink":
                                    content.Buttonlink = itemcontent[1];
                                    break;
                            }
                        }
                    }
                });
                return content;
            }
            catch { return null; }
        }

        public async Task<MOBIRROPSChange> ValidateIRROPSStatus(MOBPNRByRecordLocatorRequest request, MOBPNRByRecordLocatorResponse response, ReservationDetail cslReservationDetail, Session session)
        {
            MOBIRROPSChange irropsOptions = null;

            if (IsIRROPSPnr(cslReservationDetail) == false) return null;

            var cslStrResponse = await GetIRROPSStatus_Csl(request, cslReservationDetail, session);

            if (!string.IsNullOrEmpty(cslStrResponse))
            {
                var cslResponse = JsonConvert.DeserializeObject
                    <United.Service.Presentation.EServiceCheckInModel.CheckInIrropResponse>(cslStrResponse);
                if (cslResponse?.Errors == null && cslResponse?.TravelPlan?.Reaccom != null)
                {
                    var reaccom = cslResponse?.TravelPlan?.Reaccom;

                    if (reaccom != null)
                    {
                        bool isShoppingAllowed
                               = cslResponse?.TravelPlan?.Reaccom?.IsShoppingAllowed ?? false;

                        bool isFlightCancelled = (reaccom.Indicator
                            == Service.Presentation.EServiceCheckInModel.RemarkIndicatorEnum.CXLD);

                        bool isFlightDelayed = (reaccom.Indicator
                            == Service.Presentation.EServiceCheckInModel.RemarkIndicatorEnum.DLYD);

                        bool isFlightMisconnect = (reaccom.Indicator
                            == Service.Presentation.EServiceCheckInModel.RemarkIndicatorEnum.MISX);

                        bool isRebooked = (reaccom.OldItineraryFlights?.Count() >= 1);

                        bool isTRHEligible = reaccom.TRH?.IsTRHEntryEligible ?? false;

                        bool isTRHVisited = reaccom.TRH?.WasTRHVisited ?? false;

                        if (isRebooked)
                        {
                            string[] contentarray = SplitConcatenatedConfigValue("IRROPS_REBOOK_Content", "||");

                            string[] btnControls = SplitConcatenatedConfigValue("IRROPS_ButtonOptions", "||");

                            var irropsAdvisory = new MOBPNRAdvisory
                            {
                                AdvisoryType = AdvisoryType.INFORMATION,
                                ContentType = ContentType.IRROPS,
                                Header = SplitConcatenatedString(contentarray[0], "|")[1],
                                Body = SplitConcatenatedString(contentarray[1], "|")[1],
                                Buttontext = SplitConcatenatedString(btnControls[1], "|")[1]
                            };

                            response.PNR.AdvisoryInfo
                                = (response.PNR.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : response.PNR.AdvisoryInfo;
                            response.PNR.AdvisoryInfo.Insert(0, irropsAdvisory);
                        }
                        else if (isFlightDelayed)
                        {
                            if (isShoppingAllowed)
                            {
                                irropsOptions = SetIRROPSPopUpDisplayContent(response, CHECKINDelayed: true, isMAPPURL: true);
                            }
                            else if (!_configuration.GetValue<bool>("IRROPS_TRH_MVP1")
                                && !isShoppingAllowed && isTRHEligible)
                            {
                                if (response.PNR.Segments.Exists(x => String.Equals(x.UflifoFlightStatus, "LANDED", StringComparison.OrdinalIgnoreCase)))
                                {
                                    SetIRROPSLandedLiftedDisplayContent(response, isTRHVisited, TRHisLanded: true);
                                }
                                else if (response.PNR.Segments.Exists(x => String.Equals(x.UflifoFlightStatus, "LIFTED", StringComparison.OrdinalIgnoreCase)))
                                {
                                    SetIRROPSLandedLiftedDisplayContent(response, isTRHVisited, TRHisLifted: true);
                                }
                                else
                                {
                                    SetIRROPSPopUpDisplayContent(response, TRHDelayed: true);

                                }
                            }
                            else
                            {
                                irropsOptions = SetIRROPSPopUpDisplayContent(response, HARDSTOPDelayed: true, isPhone: true);
                            }
                        }//isFlightDelayed
                        else if (isFlightCancelled)
                        {
                            if (isShoppingAllowed)
                            {
                                irropsOptions = SetIRROPSPopUpDisplayContent(response, CHECKINCancelled: true, isMAPPURL: true);
                            }
                            else
                            {
                                irropsOptions = SetIRROPSPopUpDisplayContent(response, HARDSTOPCancelled: true, isPhone: true);
                            }
                        }//isFlightCancelled
                        else if (isFlightMisconnect)
                        {
                            irropsOptions = SetIRROPSPopUpDisplayContent(response, CHECKINMisconnect: true, isMAPPURL: true);
                        }
                    }
                }
            }
            return irropsOptions;
        }
        private MOBIRROPSChange SetIRROPSLandedLiftedDisplayContent(MOBPNRByRecordLocatorResponse response, bool isTRHVisited, bool TRHisLanded = false, bool TRHisLifted = false)
        {
            MOBIRROPSChange irropsOptions = null;

            string configkey = (TRHisLanded) ? "IRROPS_TRH_LANDED"
                   : (TRHisLifted) ? "IRROPS_TRH_LIFTED"
                   : string.Empty;

            string[] contentarray
                = SplitConcatenatedConfigValue(configkey, "||");

            string[] btnControls
                = SplitConcatenatedConfigValue("IRROPS_ButtonOptions", "||");
            if (contentarray?.Length >= 2 && btnControls?.Length >= 3)
            {
                string header = ShopStaticUtility.SplitConcatenatedString(contentarray[0], "|")[1];
                string body = string.Format(ShopStaticUtility.SplitConcatenatedString(contentarray[1], "|")[1]);

                irropsOptions = new MOBIRROPSChange
                {
                    displayHeader = header,
                    displayBody = body,
                    displayOptions = new List<MOBDisplayItem>()
                };
                var irropsAdvisory = new MOBPNRAdvisory
                {
                    AdvisoryType = isTRHVisited ? AdvisoryType.INFORMATION : AdvisoryType.WARNING,
                    ContentType = ContentType.TRH,
                    Header = header,
                    Body = body,
                    Buttontext = SplitConcatenatedString(btnControls[2], "|")[1]
                };
                response.PNR.AdvisoryInfo = (response.PNR.AdvisoryInfo == null) ? new List<MOBPNRAdvisory>() : response.PNR.AdvisoryInfo;
                response.PNR.AdvisoryInfo.Add(irropsAdvisory);
            }

            return irropsOptions;
        }
        private async Task<string> GetIRROPSStatus_Csl(MOBPNRByRecordLocatorRequest request,
       ReservationDetail cslReservationDetail, Session session)
        {
            string cslRequest = JsonConvert.SerializeObject
                (cslReservationDetail);

            string eServiceAuthorization = _configuration.GetValue<string>("IRROPS_eServiceAuthorization");

            try
            {
                var cslstrResponse = await _iRROPValidateService.GetIRROPSStatus
                     (session.Token, cslRequest, session.SessionId, "", eServiceAuthorization);

                return cslstrResponse;
            }
            catch (Exception exc)
            {
                //LogEntries.Add(United.Logger.LogEntry.GetLogEntry<string>(request.SessionId, "ValidateIRROPSStatus", "CSL-Exception",
                        //request.Application.Id, request.Application?.Version?.Major, request.DeviceId, Utility.ExceptionMessages(exc)));
            }
            return string.Empty;
        }

        private string[] SplitConcatenatedString(string value, string splitchar)
        {
            try
            {
                string[] splitSymbol = { splitchar };
                string[] splitString = value.Split(splitSymbol, StringSplitOptions.None);
                return splitString;
            }
            catch { return null; }
        }

        private string[] SplitConcatenatedConfigValue(string configkey, string splitchar)
        {
            try
            {
                string[] splitSymbol = { splitchar };
                string[] splitString = _configuration.GetValue<string>(configkey)
                    .Split(splitSymbol, StringSplitOptions.None);
                return splitString;
            }
            catch { return null; }
        }

        public async Task<bool> AddPNRRemark(MOBPNRRemarkRequest request)
        {
            bool ok = true;
            List<Service.Presentation.CommonModel.Remark> cslRequest = new List<Remark>
            {
                 new Remark{ Description = request.RemarkDescription }
            };

            string jsonRequest = System.Text.Json.JsonSerializer.Serialize<List<Remark>>(cslRequest);

            var Token = await _dPService.GetAnonymousToken(_headers.ContextValues.Application.Id, request.DeviceId, _configuration);

            string suffixUrl = string.Format("/{0}/Remarks?RetrievePNR=True&amp;EndTransaction=True", request.RecordLocator);

            var response = await _flightReservationService.AddPNRRemark(jsonRequest, Token, suffixUrl);

            if (response.IsNullOrEmpty())
            {
                return false;
            }

            return ok;
        }
    }
    #endregion
}