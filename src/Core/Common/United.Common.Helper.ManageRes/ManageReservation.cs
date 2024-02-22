using EmployeeRes.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using United.Common.Helper.Merchandize;
using United.Common.Helper.Shopping;
using United.Definition;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Profile;
using United.Mobile.DataAccess.ReShop;
using United.Mobile.Model;
using United.Mobile.Model.Common;
using United.Mobile.Model.Fitbit;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.ManageRes;
using United.Mobile.Model.MPRewards;
using United.Mobile.Model.Shopping;
using United.Service.Presentation.CommonModel;
using United.Service.Presentation.LoyaltyModel;
using United.Service.Presentation.ReferenceDataRequestModel;
using United.Service.Presentation.ReferenceDataResponseModel;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Services.FlightShopping.Common.Extensions;
using United.Utility.Enum;
using United.Utility.Helper;
using FlowType = United.Utility.Enum.FlowType;
using MOBPriorityBoarding = United.Mobile.Model.MPRewards.MOBPriorityBoarding;

namespace United.Common.Helper.ManageRes
{
    public class ManageReservation : IManageReservation
    {
        private readonly ICacheLog<ManageReservation> _logger;
        private readonly IConfiguration _configuration;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IHeaders _headers;
        private readonly IDPService _dPService;
        private readonly ITravelerCSL _travelerCSL;
        private readonly IPNRRetrievalService _pNRRetrievalService;
        private readonly IReferencedataService _referencedataService;
        private readonly IFlightReservation _flightReservation;
        private readonly IMerchandizingServices _merchandizingServices;
        private readonly IDynamoDBService _dynamoDBService;
        private List<MOBLegalDocument> cachedLegalDocuments = null;
        private readonly ICachingService _cachingService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        private const string DOTBaggageInfoDBTitle1 = "DOTBaggageInfoText1";
        private const string DOTBaggageInfoDBTitle1ELF = "DOTBaggageInfoText1 - ELF";
        private const string DOTBaggageInfoDBTitle1IBE = "DOTBaggageInfoText1 - IBE";
        private const string DOTBaggageInfoDBTitle2 = "DOTBaggageInfoText2";
        private const string DOTBaggageInfoDBTitle3 = "DOTBaggageInfoText3";
        private const string DOTBaggageInfoDBTitle3IBE = "DOTBaggageInfoText3IBE";
        private const string DOTBaggageInfoDBTitle4 = "DOTBaggageInfoText4";
        private readonly ManageResUtility _manageResUtility;
        private readonly IRefundService _refundService;

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


        public ManageReservation(ICacheLog<ManageReservation> logger
            , IConfiguration configuration
            , ISessionHelperService sessionHelperService
            , IHeaders headers
            , IDPService dPService
            , ITravelerCSL travelerCSL
            , IPNRRetrievalService pNRRetrievalService
            , IReferencedataService referencedataService
            , IFlightReservation flightReservation
            , IMerchandizingServices merchandizingServices
            , IDynamoDBService dynamoDBService
            , ICachingService cachingService
            , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
            , IRefundService refundService
            )
        {
            _logger = logger;
            _configuration = configuration;
            _sessionHelperService = sessionHelperService;
            _headers = headers;
            _dPService = dPService;
            _travelerCSL = travelerCSL;
            _pNRRetrievalService = pNRRetrievalService;
            _referencedataService = referencedataService;
            _flightReservation = flightReservation;
            _merchandizingServices = merchandizingServices;
            _dynamoDBService = dynamoDBService;
            _cachingService = cachingService;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _manageResUtility = new ManageResUtility(_configuration, _legalDocumentsForTitlesService, _dynamoDBService, _headers, _logger);
            _refundService = refundService;
        }

        public async Task<MOBPNRByRecordLocatorResponse> GetPNRByRecordLocatorCommonMethod(MOBPNRByRecordLocatorRequest request)
        {
            MOBPNRByRecordLocatorResponse response = new MOBPNRByRecordLocatorResponse();

            #region
            CommonDef commonDef = new CommonDef();
            commonDef.SampleJsonResponse = JsonConvert.SerializeObject(request);

            string data = (request.DeviceId + request.RecordLocator).Replace("|", "").Replace("-", "").ToUpper().Trim();
            await _sessionHelperService.SaveSession<CommonDef>(commonDef, data, new List<string> { data, commonDef.ObjectName }, commonDef.ObjectName).ConfigureAwait(false);

            response.TransactionId = request.TransactionId;

            new ForceUpdateVersion(_configuration).ForceUpdateForNonSupportedVersion(request.Application.Id, request.Application.Version.Major, FlowType.MANAGERES);

            //ALM 24932/25453 - Throwing exception, if last name is null
            //Srini Penmetsa - Dec 21, 2015
            if (string.IsNullOrEmpty(request.LastName))
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("InvalidPNRLastName-ExceptionMessage"));
            }
            //End ALM 24932

            #region
            if (GeneralHelper.ValidateAccessCode(request.AccessCode))
            {
                response.RecordLocator = request.RecordLocator;
                response.LastName = request.LastName;

                //Fix for MOBILE-9372 : Incorrect seat number for 2nd segment via seat link on Res detail -- Shashank
                if (!_configuration.GetValue<bool>("DisableFixforEmptyRequestFlowMOBILE9372"))
                {
                    response.Flow = !string.IsNullOrEmpty(request.Flow) ? request.Flow : FlowType.VIEWRES.ToString();
                }

                Session session = new Session();
                session = await _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string> { request.SessionId, session.ObjectName }).ConfigureAwait(false);
                var tupleRes = await LoadPnr(request, session);
                response.PNR = tupleRes.pnr;

                if (_configuration.GetValue<bool>("joinOneClickMileagePlusEnabled") && response.PNR != null)
                {
                    response.PNR.OneClickEnrollmentEligibility = new MOBOneClickEnrollmentEligibility()
                    {
                        JoinMileagePlus = !string.IsNullOrEmpty(_configuration.GetValue<string>("joinMileagePlus")) ? _configuration.GetValue<string>("joinMileagePlus") : string.Empty,
                        JoinMileagePlusHeader = !string.IsNullOrEmpty(_configuration.GetValue<string>("joinMileagePlusHeader")) ? _configuration.GetValue<string>("joinMileagePlusHeader") : string.Empty,
                        JoinMileagePlusText = !string.IsNullOrEmpty(_configuration.GetValue<string>("joinMileagePlusText")) ? _configuration.GetValue<string>("joinMileagePlusText") : string.Empty
                    };
                }

                await CheckForFuzziPNRAndSaveCommonDefPersistsFile(request.RecordLocator + "_" + request.SessionId, request.DeviceId, response.PNR.RecordLocator, commonDef);

                await AddAncillaryToPnrResponse(request, response, session, tupleRes.clsReservationDetail);
                SupressWhenScheduleChange(response);

                if (request.Flow != FlowType.VIEWRES_SEATMAP.ToString() && _configuration.GetValue<bool>("EnableMgnResUpdateTravelerInfo") == true)
                {
                    try
                    {
                        response = await RetrieveAndMapSpecialNeeds(session, request, tupleRes.clsReservationDetail.Detail.FlightSegments, response);
                        await _sessionHelperService.SaveSession<MOBPNR>(response.PNR, request.SessionId, new List<string> { request.SessionId, new MOBPNR().ObjectName }, new MOBPNR().ObjectName).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("GetPNRByRecordLocator_CFOP {ExceptionStackMessage}, {MPNumber}, {transactionID}", ex.Message, response.PNR, response.TransactionId);
                    }
                }
                //Ends Here

                //Adding this to enable us to query logs with pnr
                var sessionDetails = GetSessionDetailsMessageForLogging(request.SessionId, request, response);
                _logger.LogInformation("GetPNRByRecordLocator_CFOP {RecordLocator} and {SessionId}", request.RecordLocator + "_" + request.SessionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, sessionDetails, true, false);
            }
            else
            {
                throw new MOBUnitedException("Invalid access code");
            }
            #endregion

            return response;
            #endregion
        }

        private string GetSessionDetailsMessageForLogging(string transactionId, MOBPNRByRecordLocatorRequest request, MOBPNRByRecordLocatorResponse response)
        {
            if (transactionId != null && request != null &&
                response != null && response.PNR != null && response.PNR.RecordLocator != null)
            {
                return "RecordLocator = " + request.RecordLocator +
                       " | sessionId = " + response.PNR.SessionId +
                       " | transcationId = " + transactionId;
            }

            return "No details found";
        }

        private async Task<MOBPNRByRecordLocatorResponse> RetrieveAndMapSpecialNeeds
            (Session session, MOBPNRByRecordLocatorRequest request,
            Collection<ReservationFlightSegment> flightSegments,
            MOBPNRByRecordLocatorResponse mobResponse,
            string langType = "en-US")
        {
            if (session == null) return mobResponse;
            if (mobResponse.PNR.IsCanceledWithFutureFlightCredit) return mobResponse;

            bool IsAdvisoryMsgNeeded = false;
            var highTouchInclude = new List<string>();
            var hightouchitem = new List<string> { "DPNA_1", "DPNA_2", "WCHC" };
            List<MOBTravelSpecialNeed> selectedHighTouchItem = new List<MOBTravelSpecialNeed>();

            var specialNeeds = await GetItineraryAvailableSpecialNeeds
                            (session, request.Application.Id, request.Application.Version.Major, request.DeviceId, flightSegments, langType, request);

            List<string> specialMealsItem = new List<string>();
            if (specialNeeds?.SpecialMeals != null && specialNeeds.SpecialMeals.Any())
                specialMealsItem = specialNeeds.SpecialMeals.Where(x => (!string.IsNullOrEmpty(x.Code))).Select(y => Convert.ToString(y.Code)).ToList();
            List<string> specialRequestsItem = new List<string>();
            if (specialNeeds?.SpecialRequests != null && specialNeeds.SpecialRequests.Any())
            {
                specialRequestsItem = specialNeeds.SpecialRequests.Where(x => ((!string.IsNullOrEmpty(x.Code) && (x.SubOptions == null))))
                    .Select(y => Convert.ToString(y.Code)).ToList();

                specialNeeds.SpecialRequests.ForEach(x =>
                {
                    if (x.SubOptions != null && x.SubOptions.Any())
                    {
                        x.SubOptions.ForEach(y =>
                        {
                            if (!string.IsNullOrEmpty(y.Code))
                            {
                                specialRequestsItem.Add(y.Code);
                            }
                        });
                    }
                });
            }

            if (mobResponse != null && mobResponse.PNR != null && mobResponse.PNR.Passengers != null)
            {
                mobResponse.PNR.Passengers.ForEach(passenger =>
                {
                    if (passenger.SelectedSpecialNeeds != null && passenger.SelectedSpecialNeeds.Any())
                    {
                        var filteredAllSelectedSpecialNeeds = passenger.SelectedSpecialNeeds
                        .Where(x => (specialMealsItem.Contains(x.Code) || specialRequestsItem.Contains(x.Code)));

                        var ssrDisplaySequenceList = filteredAllSelectedSpecialNeeds
                        .Where(x => (!string.IsNullOrEmpty(x.DisplaySequence))).GroupBy(y => y.DisplaySequence).Select(z => z.First())
                        .Select(u => u.DisplaySequence).ToList();

                        passenger.SSRDisplaySequence = string.Join("|", ssrDisplaySequenceList);

                        passenger.SelectedSpecialNeeds = new List<MOBTravelSpecialNeed>();
                        passenger.SelectedSpecialNeeds = filteredAllSelectedSpecialNeeds.GroupBy(x => x.Code).Select(y => y.First())
                        .Select(z => new MOBTravelSpecialNeed()
                        {
                            Code = z.Code,
                            DisplayDescription = z.DisplayDescription,
                            DisplaySequence = z.DisplaySequence,
                            Value = z.Value
                        }).ToList();

                        if (passenger.SelectedSpecialNeeds != null && passenger.SelectedSpecialNeeds.Any())
                        {
                            IsAdvisoryMsgNeeded = true;

                            var items = passenger.SelectedSpecialNeeds.Where(x => hightouchitem.Contains(x.Code));
                            if (items != null && items.Any())
                            {
                                selectedHighTouchItem.AddRange(items);
                                highTouchInclude.AddRange(items.Select(x => x.Code).ToList());
                            }

                            passenger.SelectedSpecialNeeds.ForEach(item =>
                            {
                                item = SetSpecialNeedProperties(item, specialNeeds);
                            });
                        }
                    }
                });
            }

            if (!IsAdvisoryMsgNeeded)
            {
                mobResponse.PNR.MealAccommodationAdvisory = string.Empty;
                mobResponse.PNR.MealAccommodationAdvisoryHeader = string.Empty;
            }

            if (specialNeeds != null && specialNeeds.SpecialRequests != null && specialNeeds.SpecialRequests.Any())
            {
                hightouchitem.RemoveAll(x => highTouchInclude.Contains(x));

                var specialRequest = specialNeeds.SpecialRequests.Where(sr => !hightouchitem.Contains(sr.Code)).ToList();

                if (specialRequest != null && specialRequest.Any())
                {
                    specialNeeds.SpecialRequests = new List<MOBTravelSpecialNeed>();
                    specialNeeds.SpecialRequests = specialRequest;
                }
            }

            specialNeeds.ServiceAnimals = null;
            mobResponse.SpecialNeeds = specialNeeds;
            specialNeeds.HighTouchItems = selectedHighTouchItem;
            specialNeeds.AccommodationsUnavailable = _configuration.GetValue<string>("travelNeedAttentionMessage");
            specialNeeds.MealUnavailable = _configuration.GetValue<string>("mealAttentionMessage");
            return mobResponse;
        }

        private MOBTravelSpecialNeed SetSpecialNeedProperties
            (MOBTravelSpecialNeed item, MOBTravelSpecialNeeds specialNeeds)
        {
            if (specialNeeds == null && item == null) return null;

            MOBTravelSpecialNeed specialmeal;
            specialmeal = (specialNeeds.SpecialMeals != null) ? specialNeeds.SpecialMeals.FirstOrDefault
                (x => string.Equals(x.Code, item.Code, StringComparison.OrdinalIgnoreCase)) : null;

            MOBTravelSpecialNeed specialrequest;
            specialrequest = (specialNeeds.SpecialRequests != null) ? specialNeeds.SpecialRequests.FirstOrDefault
            (x => string.Equals(x.Code, item.Code, StringComparison.OrdinalIgnoreCase)) : null;

            MOBTravelSpecialNeed specialrequestsuboption;
            specialrequestsuboption = specialNeeds.SpecialRequests.FirstOrDefault
            (x => !string.IsNullOrEmpty(x.SubOptionHeader));

            MOBTravelSpecialNeed suboption;

            if (specialrequestsuboption != null && specialrequestsuboption.SubOptions != null
            && specialrequestsuboption.SubOptions.Any())
            {
                suboption = specialrequestsuboption.SubOptions.FirstOrDefault
                (x => string.Equals(x.Code, item.Code, StringComparison.OrdinalIgnoreCase));
            }
            else
                suboption = null;

            if (suboption != null)
            {
                item.Code = !string.IsNullOrEmpty(specialrequestsuboption.Code) ? specialrequestsuboption.Code : string.Empty;
                item.DisplayDescription = !string.IsNullOrEmpty(specialrequestsuboption.DisplayDescription) ? specialrequestsuboption.DisplayDescription : string.Empty;
                item.RegisterServiceDescription = !string.IsNullOrEmpty(specialrequestsuboption.RegisterServiceDescription) ? specialrequestsuboption.RegisterServiceDescription : string.Empty;
                item.SubOptionHeader = !string.IsNullOrEmpty(specialrequestsuboption.SubOptionHeader) ? specialrequestsuboption.SubOptionHeader : string.Empty;
                item.SubOptions = new List<MOBTravelSpecialNeed>();
                item.SubOptions.Add(suboption);
                item.Type = !string.IsNullOrEmpty(specialrequestsuboption.Type) ? specialrequestsuboption.Type : string.Empty;
            }
            else
            {
                item.Type = (specialmeal != null) ? specialmeal.Type : (specialrequest != null) ? specialrequest.Type : string.Empty;
                item.DisplayDescription = (specialmeal != null) ? specialmeal.DisplayDescription : (specialrequest != null) ? specialrequest.DisplayDescription : string.Empty;
            }

            if (item.Messages != null && item.Messages.Any())
            {
                item.Messages = new List<MOBItem>();
                item.Messages = (specialmeal.Messages != null) ? specialmeal.Messages : (specialrequest.Messages != null) ? specialrequest.Messages : null;
            }

            return item;
        }

        private async Task<MOBTravelSpecialNeeds> GetItineraryAvailableSpecialNeeds(Session session, int appId, string appVersion, string deviceId, IEnumerable<ReservationFlightSegment> segments, string languageCode, MOBPNRByRecordLocatorRequest request)
        {
            MultiCallResponse flightshoppingReferenceData = null;
            IEnumerable<ReservationFlightSegment> pnrOfferedMeals = null;
            var offersSSR = new MOBTravelSpecialNeeds();

            try
            {
                //Parallel.Invoke(() => flightshoppingReferenceData = GetSpecialNeedsReferenceDataFromFlightShopping(session, appId, appVersion, deviceId, languageCode),
                //                () => pnrOfferedMeals = GetOfferedMealsForItineraryFromPNRManagement(session, appId, appVersion, deviceId, segments));

                flightshoppingReferenceData = await GetSpecialNeedsReferenceDataFromFlightShopping(session, appId, appVersion, deviceId, languageCode);
                pnrOfferedMeals = await GetOfferedMealsForItineraryFromPNRManagement(session, appId, appVersion, deviceId, segments);
            }
            catch (Exception) // 'System.ArgumentException' is thrown when any action in the actions array throws an exception.
            {
                if (flightshoppingReferenceData == null) // unable to get reference data, POPULATE DEFAULT SPECIAL REQUESTS
                {
                    offersSSR.ServiceAnimalsMessages = new List<MOBItem> { new MOBItem { CurrentValue = _configuration.GetValue<string>("SSR_RefDataServiceFailure_ServiceAnimalMassage") } };

                    flightshoppingReferenceData = GetMultiCallResponseWithDefaultSpecialRequests();
                }
                else if (pnrOfferedMeals == null) // unable to get market restriction meals, POPULATE DEFAULT MEALS
                {
                    pnrOfferedMeals = PopulateSegmentsWithDefaultMeals(segments);
                }
            }

            Parallel.Invoke(() => offersSSR.SpecialMeals = GetOfferedMealsForItinerary(pnrOfferedMeals, flightshoppingReferenceData),
                            () => offersSSR.SpecialMealsMessages = GetSpecialMealsMessages(pnrOfferedMeals, flightshoppingReferenceData),
                            async () => offersSSR.SpecialRequests = await GetOfferedSpecialRequests(flightshoppingReferenceData, session, request),
                            () => offersSSR.SpecialMealsMessages = GetSpecialMealsMessages(pnrOfferedMeals, flightshoppingReferenceData),
                            () => offersSSR.ServiceAnimals = GetOfferedServiceAnimals(flightshoppingReferenceData, segments, appId, appVersion),
                            () => offersSSR.SpecialNeedsAlertMessages = GetPartnerAirlinesSpecialTravelNeedsMessage(session, segments));

            if (!string.IsNullOrEmpty(_configuration.GetValue<string>("RemoveEmotionalSupportServiceAnimalOption_EffectiveDateTime"))
                && Convert.ToDateTime(_configuration.GetValue<string>("RemoveEmotionalSupportServiceAnimalOption_EffectiveDateTime")) <= DateTime.Now
                && offersSSR.ServiceAnimals != null && offersSSR.ServiceAnimals.Any())
            {
                offersSSR.ServiceAnimals.Remove(offersSSR.ServiceAnimals.FirstOrDefault(x => x.Code == "ESAN" && x.Value == "6"));
            }
            if (IsTaskTrainedServiceDogSupportedAppVersion(appId, appVersion)
                 && offersSSR?.ServiceAnimals != null && offersSSR.ServiceAnimals.Any(x => x.Code == "ESAN" && x.Value == "5"))
            {
                offersSSR.ServiceAnimals.Remove(offersSSR.ServiceAnimals.FirstOrDefault(x => x.Code == "ESAN" && x.Value == "5"));
            }

            await AddServiceAnimalsMessageSection(offersSSR, appId, appVersion, session, deviceId);

            if (offersSSR.ServiceAnimalsMessages == null || !offersSSR.ServiceAnimalsMessages.Any())
                offersSSR.ServiceAnimalsMessages = GetServiceAnimalsMessages(offersSSR.ServiceAnimals);

            return offersSSR;
        }
        internal virtual United.Mobile.Model.Common.MOBAlertMessages GetPartnerAirlinesSpecialTravelNeedsMessage(Session session, IEnumerable<ReservationFlightSegment> segments)
        {
            if (_configuration.GetValue<bool>("EnableAirlinesFareComparison") && session.CatalogItems != null && session.CatalogItems.Count > 0 &&
                  session.CatalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableNewPartnerAirlines).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableNewPartnerAirlines).ToString())?.CurrentValue == "1"
                 && segments?.Any(s => s.FlightSegment.OperatingAirlineCode != null) == true
                   && _configuration.GetValue<string>("SupportedAirlinesFareComparison").Contains(segments?.FirstOrDefault()?.FlightSegment?.OperatingAirlineCode.ToUpper())
                  )
            {

                United.Mobile.Model.Common.MOBAlertMessages specialNeedsAlertMessages = new United.Mobile.Model.Common.MOBAlertMessages
                {
                    HeaderMessage = _configuration.GetValue<string>("PartnerAirlinesSpecialTravelNeedsHeader"),
                    IsDefaultOption = true,
                    MessageType = MOBFSRAlertMessageType.Caution.ToString(),
                    AlertMessages = new List<MOBSection>
                        {
                            new MOBSection
                            {
                                MessageType = MOBFSRAlertMessageType.Caution.ToString(),
                                Text2 = _configuration.GetValue<string>("PartnerAirlinesSpecialTravelNeedsMessage"),
                                Order = "1"
                            }
                        }
                };
                return specialNeedsAlertMessages;
            }
            return null;
        }
        internal virtual List<MOBItem> GetServiceAnimalsMessages(List<MOBTravelSpecialNeed> serviceAnimals)
        {
            if (serviceAnimals != null && serviceAnimals.Any())
                return null;

            return new List<MOBItem> { new MOBItem { CurrentValue = _configuration.GetValue<string>("SSRItineraryServiceAnimalNotAvailableMsg") } };
        }


        private async System.Threading.Tasks.Task AddServiceAnimalsMessageSection(MOBTravelSpecialNeeds offersSSR, int appId, string appVersion, Session session, string deviceId)
        {
            if (IsTaskTrainedServiceDogSupportedAppVersion(appId, appVersion) && offersSSR?.ServiceAnimals != null && offersSSR.ServiceAnimals.Any())
            {
                MOBRequest request = new MOBRequest();
                request.Application = new MOBApplication();
                request.Application.Id = appId;
                request.Application.Version = new MOBVersion();
                request.Application.Version.Major = appVersion;
                request.DeviceId = deviceId;

                string cmsCacheResponse = await _cachingService.GetCache<CSLContentMessagesResponse>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID") + "MOBCSLContentMessagesResponse", "trans0").ConfigureAwait(false);
                CSLContentMessagesResponse content = new CSLContentMessagesResponse();

                if (string.IsNullOrEmpty(cmsCacheResponse))
                    content = await _travelerCSL.GetBookingRTICMSContentMessages(request, session);//, LogEntries);
                else
                    content = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(cmsCacheResponse);

                string emotionalSupportAssistantContent = (content?.Messages?.FirstOrDefault(m => !string.IsNullOrEmpty(m.Title) && m.Title.Equals("TravelNeeds_TaskTrainedDog_Screen_Content_MOB"))?.ContentFull) ?? "";
                string emotionalSupportAssistantCodeVale = _configuration.GetValue<string>("TravelSpecialNeedInfoCodeValue");

                if (!string.IsNullOrEmpty(emotionalSupportAssistantContent) && !string.IsNullOrEmpty(emotionalSupportAssistantCodeVale))
                {
                    var codeValue = emotionalSupportAssistantCodeVale.Split('#');
                    offersSSR.ServiceAnimals.Add(new MOBTravelSpecialNeed
                    {
                        Code = codeValue[0],
                        Value = codeValue[1],
                        DisplayDescription = "",
                        Type = TravelSpecialNeedType.TravelSpecialNeedInfo.ToString(),
                        Messages = new List<MOBItem>
                        {
                            new MOBItem {
                                CurrentValue = emotionalSupportAssistantContent
                            }
                        }
                    });
                }
            }

            else if (_configuration.GetValue<bool>("EnableTravelSpecialNeedInfo")
                && GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("TravelSpecialNeedInfo_Supported_AppVestion_Android"), _configuration.GetValue<string>("TravelSpecialNeedInfo_Supported_AppVestion_iOS"))
                && offersSSR.ServiceAnimals != null && offersSSR.ServiceAnimals.Any())
            {
                string emotionalSupportAssistantHeading = _configuration.GetValue<string>("TravelSpecialNeedInfoHeading");
                string emotionalSupportAssistantContent = _configuration.GetValue<string>("TravelSpecialNeedInfoContent");
                string emotionalSupportAssistantCodeVale = _configuration.GetValue<string>("TravelSpecialNeedInfoCodeValue");

                if (!string.IsNullOrEmpty(emotionalSupportAssistantHeading) &&
                    !string.IsNullOrEmpty(emotionalSupportAssistantContent) &&
                    !string.IsNullOrEmpty(emotionalSupportAssistantCodeVale))
                {
                    var codeValue = emotionalSupportAssistantCodeVale.Split('#');

                    offersSSR.ServiceAnimals.Add(new MOBTravelSpecialNeed
                    {
                        Code = codeValue[0],
                        Value = codeValue[1],
                        DisplayDescription = emotionalSupportAssistantHeading,
                        Type = TravelSpecialNeedType.TravelSpecialNeedInfo.ToString(),
                        Messages = new List<MOBItem>
                        {
                            new MOBItem {
                                CurrentValue = emotionalSupportAssistantContent
                            }
                        }
                    });
                }
            }
        }


        private bool IsTaskTrainedServiceDogSupportedAppVersion(int appId, string appVersion)
        {
            return GeneralHelper.IsApplicationVersionGreaterorEqual(appId, appVersion, _configuration.GetValue<string>("TravelSpecialNeedInfo_TaskTrainedServiceDog_Supported_AppVestion_Android"), _configuration.GetValue<string>("TravelSpecialNeedInfo_TaskTrainedServiceDog_Supported_AppVestion_iOS"));
        }

        private List<MOBTravelSpecialNeed> GetOfferedServiceAnimals(MultiCallResponse flightshoppingReferenceData, IEnumerable<ReservationFlightSegment> segments, int appId, string appVersion)
        {
            if (!IsTaskTrainedServiceDogSupportedAppVersion(appId, appVersion) &&
               !_configuration.GetValue<bool>("ShowServiceAnimalInTravelNeeds"))
                return null;

            if (segments == null || !segments.Any()
                || flightshoppingReferenceData == null || flightshoppingReferenceData.ServiceAnimalResponses == null || !flightshoppingReferenceData.ServiceAnimalResponses.Any()
                || flightshoppingReferenceData.ServiceAnimalResponses[0].Animals == null || !flightshoppingReferenceData.ServiceAnimalResponses[0].Animals.Any()
                || flightshoppingReferenceData.ServiceAnimalTypeResponses == null || !flightshoppingReferenceData.ServiceAnimalTypeResponses.Any()
                || flightshoppingReferenceData.ServiceAnimalTypeResponses[0].Types == null || !flightshoppingReferenceData.ServiceAnimalTypeResponses[0].Types.Any())
                return null;

            if (!DoesItineraryHaveServiceAnimal(segments))
                return null;

            var SSRAnimalValueCodeDesc = _configuration.GetValue<string>("SSRAnimalValueCodeDesc").Split('|').ToDictionary(x => x.Split('^')[0], x => x.Split('^')[1]);
            var SSRAnimalTypeValueCodeDesc = _configuration.GetValue<string>("SSRAnimalTypeValueCodeDesc").Split('|').ToDictionary(x => x.Split('^')[0], x => x.Split('^')[1]);

            Func<string, string, string, string, string, MOBTravelSpecialNeed> createSpecialNeed = (code, value, desc, RegisterServiceDesc, type)
                => new MOBTravelSpecialNeed { Code = code, Value = value, DisplayDescription = desc, RegisterServiceDescription = RegisterServiceDesc, Type = type };

            List<MOBTravelSpecialNeed> animals = flightshoppingReferenceData.ServiceAnimalResponses[0].Animals
                                                .Where(x => !string.IsNullOrWhiteSpace(x.Description))
                                                .Select(x => createSpecialNeed(SSRAnimalValueCodeDesc[x.Value], x.Value, x.Description, x.Description, TravelSpecialNeedType.ServiceAnimal.ToString())).ToList();

            Func<Service.Presentation.CommonModel.Characteristic, MOBTravelSpecialNeed> createServiceAnimalTypeItem = animalType =>
            {
                var type = createSpecialNeed(SSRAnimalTypeValueCodeDesc[animalType.Value], animalType.Value,
                                             animalType.Description, animalType.Description.EndsWith("animal", StringComparison.OrdinalIgnoreCase) ? null : "Dog", TravelSpecialNeedType.ServiceAnimalType.ToString());
                type.SubOptions = animalType.Description.EndsWith("animal", StringComparison.OrdinalIgnoreCase) ? animals : null;
                return type;
            };

            return flightshoppingReferenceData.ServiceAnimalTypeResponses[0].Types.Where(x => !string.IsNullOrWhiteSpace(x.Description))
                                                                                  .Select(createServiceAnimalTypeItem).ToList();
        }

        private bool DoesItineraryHaveServiceAnimal(IEnumerable<ReservationFlightSegment> segments)
        {
            var statesDoNotAllowServiceAnimal = new HashSet<string>(_configuration.GetValue<string>("SSRStatesDoNotAllowServiceAnimal").Split('|'));
            foreach (var segment in segments)
            {
                if (segment == null || segment.FlightSegment == null || segment.FlightSegment.ArrivalAirport == null || segment.FlightSegment.DepartureAirport == null
                    || segment.FlightSegment.ArrivalAirport.IATACountryCode == null || segment.FlightSegment.DepartureAirport.IATACountryCode == null
                    || string.IsNullOrWhiteSpace(segment.FlightSegment.ArrivalAirport.IATACountryCode.CountryCode) || string.IsNullOrWhiteSpace(segment.FlightSegment.DepartureAirport.IATACountryCode.CountryCode)
                    || segment.FlightSegment.ArrivalAirport.StateProvince == null || segment.FlightSegment.DepartureAirport.StateProvince == null
                    || string.IsNullOrWhiteSpace(segment.FlightSegment.ArrivalAirport.StateProvince.StateProvinceCode) || string.IsNullOrWhiteSpace(segment.FlightSegment.DepartureAirport.StateProvince.StateProvinceCode)

                    || !segment.FlightSegment.ArrivalAirport.IATACountryCode.CountryCode.Equals("US") || !segment.FlightSegment.DepartureAirport.IATACountryCode.CountryCode.Equals("US") // is international
                    || statesDoNotAllowServiceAnimal.Contains(segment.FlightSegment.ArrivalAirport.StateProvince.StateProvinceCode) // touches states that not allow service animal
                    || statesDoNotAllowServiceAnimal.Contains(segment.FlightSegment.DepartureAirport.StateProvince.StateProvinceCode)) // touches states that not allow service animal
                    return false;
            }

            return true;
        }

        private async Task<MOBMobileCMSContentMessages> GetCMSContentMessageByKey(string Key, MOBRequest request, Session session)
        {

            CSLContentMessagesResponse cmsResponse = new CSLContentMessagesResponse();
            MOBMobileCMSContentMessages cmsMessage = null;
            List<CMSContentMessage> cmsMessages = null;
            try
            {
                var cmsContentCache = await _cachingService.GetCache<string>(_configuration.GetValue<string>("BookingPathRTI_CMSContentMessagesCached_StaticGUID") + "MOBCSLContentMessagesResponse", request.TransactionId);
                try
                {
                    if (!string.IsNullOrEmpty(cmsContentCache))
                        cmsResponse = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(cmsContentCache);
                }
                catch { cmsContentCache = null; }

                if (string.IsNullOrEmpty(cmsContentCache) || Convert.ToBoolean(cmsResponse.Status) == false || cmsResponse.Messages == null)
                    cmsResponse = await _travelerCSL.GetBookingRTICMSContentMessages(request, session);

                cmsMessages = (cmsResponse != null && cmsResponse.Messages != null && cmsResponse.Messages.Count > 0) ? cmsResponse.Messages : null;
                if (cmsMessages != null)
                {
                    var message = cmsMessages.Find(m => m.Title.Equals(Key));
                    if (message != null)
                    {
                        cmsMessage = new MOBMobileCMSContentMessages()
                        {
                            HeadLine = message.Headline,
                            ContentFull = message.ContentFull,
                            ContentShort = message.ContentShort
                        };
                    }
                }
            }
            catch (Exception)
            { }
            return cmsMessage;
        }

        public bool IsEnableWheelchairLinkUpdate(Session session)
        {
            return _configuration.GetValue<bool>("EnableWheelchairLinkUpdate") &&
                   session.CatalogItems != null &&
                   session.CatalogItems.Count > 0 &&
                   session.CatalogItems.FirstOrDefault(a => a.Id == ((int)IOSCatalogEnum.EnableWheelchairLinkUpdate).ToString() || a.Id == ((int)AndroidCatalogEnum.EnableWheelchairLinkUpdate).ToString())?.CurrentValue == "1";
        }

        private async Task<List<MOBTravelSpecialNeed>> GetOfferedSpecialRequests(MultiCallResponse flightshoppingReferenceData, Session session, MOBPNRByRecordLocatorRequest request)
        {
            if (flightshoppingReferenceData == null || flightshoppingReferenceData.SpecialRequestResponses == null || !flightshoppingReferenceData.SpecialRequestResponses.Any()
                || flightshoppingReferenceData.SpecialRequestResponses[0].SpecialRequests == null || !flightshoppingReferenceData.SpecialRequestResponses[0].SpecialRequests.Any())
                return null;

            var specialRequests = new List<MOBTravelSpecialNeed>();
            var specialNeedType = TravelSpecialNeedType.SpecialRequest.ToString();
            MOBTravelSpecialNeed createdWheelChairItem = null;
            Func<string, string, string, MOBTravelSpecialNeed> createSpecialNeedItem = (code, value, desc)
                => new MOBTravelSpecialNeed { Code = code, Value = value, DisplayDescription = desc, RegisterServiceDescription = desc, Type = specialNeedType };


            foreach (var specialRequest in flightshoppingReferenceData.SpecialRequestResponses[0].SpecialRequests.Where(x => x.Genre != null && !string.IsNullOrWhiteSpace(x.Genre.Description) && !string.IsNullOrWhiteSpace(x.Code)))
            {
                if (specialRequest.Genre.Description.Equals("General"))
                {
                    var sr = createSpecialNeedItem(specialRequest.Code, specialRequest.Value, specialRequest.Description);

                    if (specialRequest.Code.StartsWith("DPNA", StringComparison.OrdinalIgnoreCase)) // add info message for DPNA_1, and DPNA_2 request
                        sr.Messages = new List<MOBItem> { new MOBItem { CurrentValue = _configuration.GetValue<string>("SSR_DPNA_Message") } };

                    specialRequests.Add(sr);
                }
                else if (specialRequest.Genre.Description.Equals("WheelchairReason"))
                {
                    if (createdWheelChairItem == null)
                    {
                        createdWheelChairItem = createSpecialNeedItem(_configuration.GetValue<string>("SSRWheelChairDescription"), null, _configuration.GetValue<string>("SSRWheelChairDescription"));
                        createdWheelChairItem.SubOptionHeader = _configuration.GetValue<string>("SSR_WheelChairSubOptionHeader");

                        // MOBILE-23726
                        if (IsEnableWheelchairLinkUpdate(session))
                        {
                            var sdlKeyForWheelchairLink = _configuration.GetValue<string>("FSRSpecialTravelNeedsWheelchairLinkKey");
                            MOBMobileCMSContentMessages message = null;
                            if (!string.IsNullOrEmpty(sdlKeyForWheelchairLink))
                            {
                                message = await GetCMSContentMessageByKey(sdlKeyForWheelchairLink, request, session);
                            }
                            createdWheelChairItem.InformationLink = message?.ContentFull ?? (_configuration.GetValue<string>("WheelchairLinkUpdateFallback") ?? "");
                        }

                        specialRequests.Add(createdWheelChairItem);
                    }

                    var wheelChairSubItem = createSpecialNeedItem(specialRequest.Code, specialRequest.Value, specialRequest.Description);

                    if (createdWheelChairItem.SubOptions == null)
                    {
                        createdWheelChairItem.SubOptions = new List<MOBTravelSpecialNeed> { wheelChairSubItem };
                    }
                    else
                    {
                        createdWheelChairItem.SubOptions.Add(wheelChairSubItem);
                    }
                }
                else if (specialRequest.Genre.Description.Equals("WheelchairType"))
                {
                    specialRequests.Add(createSpecialNeedItem(specialRequest.Code, specialRequest.Value, specialRequest.Description));
                }
            }

            return specialRequests;
        }

        private List<MOBItem> GetSpecialMealsMessages(IEnumerable<ReservationFlightSegment> allSegmentsWithMeals, MultiCallResponse flightshoppingReferenceData)
        {
            Func<List<MOBItem>> GetMealUnavailableMsg = () => new List<MOBItem> { new MOBItem { CurrentValue = string.Format(_configuration.GetValue<string>("SSRItinerarySpecialMealsNotAvailableMsg"), "") } };

            if (allSegmentsWithMeals == null || !allSegmentsWithMeals.Any()
                || flightshoppingReferenceData == null || flightshoppingReferenceData.SpecialMealResponses == null || !flightshoppingReferenceData.SpecialMealResponses.Any()
                || flightshoppingReferenceData.SpecialMealResponses[0].SpecialMeals == null || !flightshoppingReferenceData.SpecialMealResponses[0].SpecialMeals.Any())
            {
                return GetMealUnavailableMsg();
            }

            // all meals from reference data
            var allRefMeals = flightshoppingReferenceData.SpecialMealResponses[0].SpecialMeals.Select(x => x.Type.Key);
            if (allRefMeals == null || !allRefMeals.Any())
                return GetMealUnavailableMsg();

            var segmentsHaveMeals = allSegmentsWithMeals.Where(seg => seg != null && seg.FlightSegment != null && seg.FlightSegment.Characteristic != null && seg.FlightSegment.Characteristic.Any()
                                                   && seg.FlightSegment.Characteristic[0] != null
                                                   && seg.FlightSegment.Characteristic.Exists(x => x.Code.Equals("SPML", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Value)))
                                                   .Select(seg => new
                                                   {
                                                       segment = string.Join(" - ", seg.FlightSegment.DepartureAirport.IATACode, seg.FlightSegment.ArrivalAirport.IATACode),
                                                       meals = string.IsNullOrWhiteSpace(seg.FlightSegment.Characteristic[0].Value) ? new HashSet<string>() : new HashSet<string>(seg.FlightSegment.Characteristic[0].Value.Split('|', ' ').Intersect(allRefMeals))
                                                   })
                                                   .Where(seg => seg.meals != null && seg.meals.Any())
                                                   .Select(seg => seg.segment)
                                                   .ToList();

            if (segmentsHaveMeals == null || !segmentsHaveMeals.Any())
            {
                return GetMealUnavailableMsg();
            }

            if (segmentsHaveMeals.Count < allSegmentsWithMeals.Count())
            {
                var segments = segmentsHaveMeals.Count > 1 ? string.Join(", ", segmentsHaveMeals.Take(segmentsHaveMeals.Count - 1)) + " and " + segmentsHaveMeals.Last() : segmentsHaveMeals.First();
                return new List<MOBItem> { new MOBItem { CurrentValue = string.Format(_configuration.GetValue<string>("SSR_MarketMealRestrictionMessage"), segments) } };
            }

            return null;
        }

        private List<MOBTravelSpecialNeed> GetOfferedMealsForItinerary(IEnumerable<ReservationFlightSegment> allSegmentsWithMeals, MultiCallResponse flightshoppingReferenceData)
        {
            if (allSegmentsWithMeals == null || !allSegmentsWithMeals.Any()
                || flightshoppingReferenceData == null || flightshoppingReferenceData.SpecialMealResponses == null || !flightshoppingReferenceData.SpecialMealResponses.Any()
                || flightshoppingReferenceData.SpecialMealResponses[0].SpecialMeals == null || !flightshoppingReferenceData.SpecialMealResponses[0].SpecialMeals.Any())
                return null;

            Func<IEnumerable<string>, List<MOBItem>> generateMsg = flightSegments =>
            {
                var segments = flightSegments.Count() > 1 ? string.Join(", ", flightSegments.Take(flightSegments.Count() - 1)) + " and " + flightSegments.Last() : flightSegments.First();
                return new List<MOBItem> { new MOBItem { CurrentValue = string.Format(_configuration.GetValue<string>("SSR_MealRestrictionMessage"), segments) } };
            };

            // all meals from reference data
            var allRefMeals = flightshoppingReferenceData.SpecialMealResponses[0].SpecialMeals.ToDictionary(x => x.Type.Key, x => string.Join("^", x.Value[0], x.Description));
            if (allRefMeals == null || !allRefMeals.Any())
                return null;

            // Dictionary whose keys are segments (orig - dest) and values are list of all meals that are available for each segment
            // These contain only the segments that offer meals
            // These meals also need to exist in reference data table 
            var segmentAndMealsMap = allSegmentsWithMeals.Where(seg => seg != null && seg.FlightSegment != null && seg.FlightSegment.Characteristic != null && seg.FlightSegment.Characteristic.Any()
                                                   && seg.FlightSegment.Characteristic[0] != null
                                                   && seg.FlightSegment.Characteristic.Exists(x => x.Code.Equals("SPML", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(x.Value))) // get all segments that offer meals
                                                   .Select(seg => new // project them
                                                   {
                                                       segment = string.Join(" - ", seg.FlightSegment.DepartureAirport.IATACode, seg.FlightSegment.ArrivalAirport.IATACode), // IAH - NRT if going from IAH to NRT 
                                                       meals = string.IsNullOrWhiteSpace(seg.FlightSegment.Characteristic[0].Value) ? null : new HashSet<string>(seg.FlightSegment.Characteristic[0].Value.Split('|', ' ').Intersect(allRefMeals.Keys)) // List of all meal codes that offer on the segment
                                                   })
                                                   .Where(segment => segment.meals != null && segment.meals.Any()) // filter out the segments that don't offer meals
                                                   .GroupBy(seg => seg.segment) // handle same market exist twice for MD
                                                   .Select(grp => grp.First()) // handle same market exist twice for MD 
                                                   .ToDictionary(seg => seg.segment, seg => seg.meals); // tranform them to dictionary of segment and meals

            if (segmentAndMealsMap == null || !segmentAndMealsMap.Any())
                return null;

            // Get common meals that offers on all segments after filtering out all segments that don't offer meals
            var mealsThatAvailableOnAllSegments = segmentAndMealsMap.Values.Skip(1)
                                                                            .Aggregate(new HashSet<string>(segmentAndMealsMap.Values.First()), (current, next) => { current.IntersectWith(next); return current; });

            // Filter out the common meals
            if (mealsThatAvailableOnAllSegments != null && mealsThatAvailableOnAllSegments.Any())
            {
                segmentAndMealsMap.Values.ToList().ForEach(x => x.RemoveWhere(item => mealsThatAvailableOnAllSegments.Contains(item)));
            }

            // Add the non-common meals, these will have message
            var results = segmentAndMealsMap.Where(kv => kv.Value != null && kv.Value.Any())
                                .SelectMany(item => item.Value.Select(x => new { mealCode = x, segment = item.Key }))
                                .GroupBy(x => x.mealCode, x => x.segment)
                                .ToDictionary(x => x.Key, x => x.ToList())
                                .Select(kv => new MOBTravelSpecialNeed
                                {
                                    Code = kv.Key,
                                    Value = allRefMeals[kv.Key].Split('^')[0],
                                    DisplayDescription = allRefMeals[kv.Key].Split('^')[1],
                                    RegisterServiceDescription = allRefMeals[kv.Key].Split('^')[1],
                                    Type = TravelSpecialNeedType.SpecialMeal.ToString(),
                                    Messages = mealsThatAvailableOnAllSegments.Any() ? generateMsg(kv.Value) : null
                                })
                                .ToList();

            // Add the common meals, these don't have messages
            if (mealsThatAvailableOnAllSegments.Any())
            {
                results.AddRange(mealsThatAvailableOnAllSegments.Select(m => new MOBTravelSpecialNeed
                {
                    Code = m,
                    Value = allRefMeals[m].Split('^')[0],
                    DisplayDescription = allRefMeals[m].Split('^')[1],
                    RegisterServiceDescription = allRefMeals[m].Split('^')[1],
                    Type = TravelSpecialNeedType.SpecialMeal.ToString()
                }));
            }

            return results == null || !results.Any() ? null : results; // return null if empty
        }

        private IEnumerable<ReservationFlightSegment> PopulateSegmentsWithDefaultMeals(IEnumerable<ReservationFlightSegment> segments)
        {
            var pnrOfferedMeals = GetOfferedMealsForItineraryFromPNRManagementRequest(segments);
            pnrOfferedMeals.Where(x => x.FlightSegment != null && x.FlightSegment.IsInternational.Equals("True", StringComparison.OrdinalIgnoreCase))
                           .ToList()
                           .ForEach(x => x.FlightSegment.Characteristic = new Collection<Service.Presentation.CommonModel.Characteristic> { new Service.Presentation.CommonModel.Characteristic {
                                       Code = "SPML",
                                       Description = "Default meals when service is down",
                                       Value = _configuration.GetValue<string>("SSR_DefaultMealCodes")
                                   } });

            return pnrOfferedMeals;
        }

        private MultiCallResponse GetMultiCallResponseWithDefaultSpecialRequests()
        {
            try
            {
                return new MultiCallResponse
                {
                    SpecialRequestResponses = new Collection<SpecialRequestResponse>
                    {
                        new SpecialRequestResponse
                        {
                            SpecialRequests = new Collection<Service.Presentation.CommonModel.Characteristic> (_configuration.GetValue<string>("SSR_DefaultSpecialRequests")
                                                                                                .Split('|')
                                                                                                .Select(request => request.Split('^'))
                                                                                                .Select(request => new Service.Presentation.CommonModel.Characteristic
                                                                                                {
                                                                                                    Code = request[0],
                                                                                                    Description = request[1],
                                                                                                    Genre = new Service.Presentation.CommonModel.Genre { Description = request[2]},
                                                                                                    Value = request[3]
                                                                                                })
                                                                                                .ToList())
                        }
                    }
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task<IEnumerable<ReservationFlightSegment>> GetOfferedMealsForItineraryFromPNRManagement(Session session, int appId, string appVersion, string deviceId, IEnumerable<ReservationFlightSegment> segments)
        {
            string cslActionName = "/SpecialMeals/FlightSegments";

            string jsonRequest = JsonConvert.SerializeObject(GetOfferedMealsForItineraryFromPNRManagementRequest(segments));

            string token = await _dPService.GetAnonymousToken(appId, deviceId, _configuration).ConfigureAwait(false);

            var cslCallDurationstopwatch = new Stopwatch();
            cslCallDurationstopwatch.Start();

            var response = await _pNRRetrievalService.GetOfferedMealsForItinerary<List<ReservationFlightSegment>>(token, cslActionName, jsonRequest, session.SessionId).ConfigureAwait(false);

            if (cslCallDurationstopwatch.IsRunning)
            {
                cslCallDurationstopwatch.Stop();
            }

            if (response != null)
            {
                if (response == null)
                {
                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }

                if (response.Count > 0)
                {
                    await _sessionHelperService.SaveSession<List<ReservationFlightSegment>>(response, session.SessionId, new List<string> { session.SessionId, response.GetType().FullName }, response.GetType().FullName).ConfigureAwait(false); //change session
                }

                return response;
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
        }

        private IEnumerable<ReservationFlightSegment> GetOfferedMealsForItineraryFromPNRManagementRequest(IEnumerable<ReservationFlightSegment> segments)
        {
            if (segments == null || !segments.Any())
                return new List<ReservationFlightSegment>();

            return segments.Select(segment => new ReservationFlightSegment
            {
                FlightSegment = new Service.Presentation.SegmentModel.FlightSegment
                {
                    ArrivalAirport = new Service.Presentation.CommonModel.AirportModel.Airport { IATACode = segment.FlightSegment.ArrivalAirport.IATACode },
                    DepartureAirport = new Service.Presentation.CommonModel.AirportModel.Airport { IATACode = segment.FlightSegment.DepartureAirport.IATACode },
                    DepartureDateTime = segment.FlightSegment.DepartureDateTime,
                    FlightNumber = segment.FlightSegment.FlightNumber,
                    InstantUpgradable = false,
                    IsInternational = segment.FlightSegment.IsInternational,
                    OperatingAirlineCode = segment.FlightSegment.OperatingAirlineCode,
                    UpgradeEligibilityStatus = Service.Presentation.CommonEnumModel.UpgradeEligibilityStatus.Unknown,
                    UpgradeVisibilityType = Service.Presentation.CommonEnumModel.UpgradeVisibilityType.None,
                    BookingClasses = new Collection<BookingClass>(segment.FlightSegment.BookingClasses.Where(y => y != null && y.Cabin != null).Select(y => new BookingClass { Cabin = new Service.Presentation.CommonModel.AircraftModel.Cabin { Name = y.Cabin.Name }, Code = y.Code }).ToList())
                }
            }).ToList();
        }

        private async Task<MultiCallResponse> GetSpecialNeedsReferenceDataFromFlightShopping(Session session, int appId, string appVersion, string deviceId, string languageCode)
        {
            string cslActionName = "MultiCall";

            string jsonRequest = JsonConvert.SerializeObject(GetFlightShoppingMulticallRequest(languageCode));

            string token = await _dPService.GetAnonymousToken(appId, deviceId, _configuration).ConfigureAwait(false);

            var cslCallDurationstopwatch = new Stopwatch();
            cslCallDurationstopwatch.Start();

            var response = await _referencedataService.GetSpecialNeedsInfo<MultiCallResponse>(cslActionName, jsonRequest, token, session.SessionId).ConfigureAwait(false);

            if (cslCallDurationstopwatch.IsRunning)
            {
                cslCallDurationstopwatch.Stop();
            }

            if (response != null)
            {
                if (response == null || response.SpecialRequestResponses == null || response.ServiceAnimalResponses == null || response.SpecialMealResponses == null || response.SpecialRequestResponses == null)
                    throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));

                return response;
            }
            else
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
            }
        }

        private MultiCallRequest GetFlightShoppingMulticallRequest(string languageCode)
        {
            var request = new MultiCallRequest
            {
                ServiceAnimalRequests = new Collection<ServiceAnimalRequest> { new ServiceAnimalRequest { LanguageCode = languageCode } },
                ServiceAnimalTypeRequests = new Collection<ServiceAnimalTypeRequest> { new ServiceAnimalTypeRequest { LanguageCode = languageCode } },
                SpecialMealRequests = new Collection<SpecialMealRequest> { new SpecialMealRequest { LanguageCode = languageCode } },
                SpecialRequestRequests = new Collection<SpecialRequestRequest> { new SpecialRequestRequest { LanguageCode = languageCode/*, Channel = _configuration.GetValue<string>("Shopping - ChannelType")*/ } },
            };

            return request;
        }

        private void SupressWhenScheduleChange(MOBPNRByRecordLocatorResponse response)
        {
            if (_configuration.GetValue<bool>("EnableSupressWhenScheduleChange") && response.PNR.HasScheduleChanged)
            {
                try
                {
                    response.ShowSeatChange
                        = response.ShowSeatChange ? false : response.ShowSeatChange;

                    response.PNR.IsEnableEditTraveler
                        = response.PNR.IsEnableEditTraveler ? false : response.PNR.IsEnableEditTraveler;

                    response.PNR.ShouldDisplayEmailReceipt = false;

                    response.PNR.ShouldDisplayUpgradeCabin = false;

                    response.PNR.SupressLMX = true;

                    if (response.PNR.AdvisoryInfo != null && response.PNR.AdvisoryInfo.Any())
                    {
                        //ContentType.SCHEDULECHANGE
                        response.PNR.AdvisoryInfo.RemoveAll(item => item.ContentType != ContentType.SCHEDULECHANGE);
                    }

                    response.ShowAddCalendar = false;
                    response.ShowBaggageInfo = false;
                    response.PNR.HasCheckedBags = false;
                }
                catch { }
            }
        }

        private void SetMerchandizeChannelValues(string merchChannel, ref string channelId, ref string channelName)
        {
            channelId = string.Empty;
            channelName = string.Empty;

            if (merchChannel != null)
            {
                switch (merchChannel)
                {
                    case "MOBBE":
                        channelId = _configuration.GetValue<string>("MerchandizeOffersServiceMOBBEChannelID").Trim();
                        channelName = _configuration.GetValue<string>("MerchandizeOffersServiceMOBBEChannelName").Trim();
                        break;
                    case "MOBMYRES":
                        channelId = _configuration.GetValue<string>("MerchandizeOffersServiceMOBMYRESChannelID").Trim();
                        channelName = _configuration.GetValue<string>("MerchandizeOffersServiceMOBMYRESChannelName").Trim();
                        break;
                    case "MOBWLT":
                        channelId = _configuration.GetValue<string>("MerchandizeOffersServiceMOBWLTChannelID").Trim();
                        channelName = _configuration.GetValue<string>("MerchandizeOffersServiceMOBWLTChannelName").Trim();
                        break;
                    default:
                        break;
                }
            }
        }

        private async void CheckPNRForOTFEligiblity(MOBPNRByRecordLocatorRequest request, MOBPNRByRecordLocatorResponse response, Session session)
        {
            try
            {
                if (!_configuration.GetValue<bool>("ExcludePNRForOTFEligiblity")
                    && response.PNR.IsATREEligible && !response.PNR.AwardTravel
                    && await CheckPNRForOTFEligiblity1(request, response, session))
                {
                    if (response?.PNR?.Futureflightcredit != null
                        && response?.PNR?.Futureflightcredit?.Messages != null
                        && response.PNR.Futureflightcredit.Messages.Any())
                    {
                        var includeffcOTFmessage = response.PNR.Futureflightcredit.Messages.FirstOrDefault
                                   (x => string.Equals(x.Id, "FFC_AGENCY_ALRTMSG", StringComparison.OrdinalIgnoreCase));

                        if (includeffcOTFmessage == null)
                        {
                            response.PNR.Futureflightcredit.Messages.Add(new MOBItem
                            {
                                Id = "FFC_AGENCY_ALRTMSG",
                                CurrentValue = _configuration.GetValue<string>("OTFFFC_ELIGIBILITY_ALRTMSG")
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private async Task<bool> CheckPNRForOTFEligiblity1(MOBPNRByRecordLocatorRequest request, MOBPNRByRecordLocatorResponse response,
          Session session)
        {
            var cslRequest = new OnTheFlyEligibilityRequest
            {
                Channel = request.Application.Id,
                LastNameBypass = true,
                RecordLocator = request.RecordLocator,
                LastName = request.LastName,
                ReservationBypass = true
            };

            var cslStrResponse = await PNRForOTFEligiblity_Csl(request, cslRequest.ToJsonString(), session);

            if (!string.IsNullOrEmpty(cslStrResponse))
            {
                var cslResponse = JsonConvert.DeserializeObject<OnTheFlyEligibility>(cslStrResponse);
                if (cslResponse != null && cslResponse.OfferEligible)
                {
                    return true;
                }
            }
            return false;
        }

        private async Task<string> PNRForOTFEligiblity_Csl
           (MOBPNRByRecordLocatorRequest mobrequest, string jsonRequest, Session session)
        {
            try
            {
                string path = "/OnTheFly/OfferEligible";

                var cslstrResponse = await _refundService.PostEligibleCheck<string>(session.Token, session.SessionId, path, jsonRequest);

                return cslstrResponse;
            }
            catch (Exception exc)
            {

                _logger.LogError("ValidateIRROPSStatus CSL Exception{exception}", JsonConvert.SerializeObject(exc));
            }

            return string.Empty;
        }

        private async System.Threading.Tasks.Task AddAncillaryToPnrResponse(MOBPNRByRecordLocatorRequest request, MOBPNRByRecordLocatorResponse response, Session session, ReservationDetail cslReservationDetail)
        {
            if (response.PNR == null || response.Exception != null || cslReservationDetail == null || session == null) return;

            if (response.PNR.IsCanceledWithFutureFlightCredit)
            {
                CheckPNRForOTFEligiblity(request, response, session);
                return;
            }

            if (_configuration.GetValue<bool>("EnableSupressWhenScheduleChange") && response.PNR.HasScheduleChanged) return;

            request.RecordLocator = response.PNR.RecordLocator; //To fix fuzzi pnr
            string channelId = string.Empty;
            string channelName = string.Empty;
            if (_configuration.GetValue<bool>("EnabledMERCHChannels"))
            {
                string merchChannel = "MOBMYRES";
                SetMerchandizeChannelValues(merchChannel, ref channelId, ref channelName);
            }

            if (request.SessionId != null)
            {
                //Emptying the existing Shopping Cart
                MOBShoppingCart persistShoppingCart = new MOBShoppingCart();
                await _sessionHelperService.SaveSession<MOBShoppingCart>(persistShoppingCart, request.SessionId, new List<string> { request.SessionId, persistShoppingCart.ObjectName }, persistShoppingCart.ObjectName).ConfigureAwait(false);

                GetOffers productOffer = new GetOffers();
                await _sessionHelperService.SaveSession<GetOffers>(productOffer, request.SessionId, new List<string> { request.SessionId, productOffer.ObjectName }, productOffer.ObjectName).ConfigureAwait(false);

                GetOffersCce productOfferCce = new GetOffersCce();
                await _sessionHelperService.SaveSession<GetOffersCce>(productOfferCce, request.SessionId, new List<string> { request.SessionId, productOfferCce.ObjectName }, productOfferCce.ObjectName).ConfigureAwait(false);

                GetVendorOffers productVendorOffer = new GetVendorOffers();
                await _sessionHelperService.SaveSession<GetVendorOffers>(productVendorOffer, request.SessionId, new List<string> { request.SessionId, productVendorOffer.ObjectName }, productVendorOffer.ObjectName).ConfigureAwait(false);
            }
            response.Ancillary = new MOBAncillary();
            SeatOffer seatOffer = null;
            MOBBundleInfo bundleInfo = null;
            System.Threading.Tasks.Task[] taskArray = new System.Threading.Tasks.Task[]
            {
               System.Threading.Tasks.Task.Factory.StartNew(()=>
                {
                     if (_configuration.GetValue<bool>("EnableShareResDetailAppWidget"))
                     {
                        MOBPNR pnr = response.PNR;
                        string url = GetTripDetailRedirect3dot0Url(response.RecordLocator, response.LastName, ac: "", channel: "mobile", languagecode: "en/US");
                        _manageResUtility.GetShareReservationInfo(response,cslReservationDetail,url);
                    }
                }),
                System.Threading.Tasks.Task.Factory.StartNew(async() =>
                {
                        var productOffers = await _merchandizingServices.GetMerchOffersDetails(session, cslReservationDetail.Detail, request, request.Flow, response.PNR);
                        response.Ancillary.PremiumCabinUpgrade = await _merchandizingServices.GetPremiumCabinUpgrade_CFOP(productOffers, request, session.SessionId, cslReservationDetail.Detail); // need to call this to clear state even when productOffers is null.
                        response.Ancillary.AwardAccelerators = (_configuration.GetValue<bool>("SuppressAwardAcceleratorForBE") && (response.PNR.isELF || response.PNR.IsIBE)) ? null : await _merchandizingServices.GetMileageAndStatusOptions(productOffers, request, session.SessionId);

                        #region PremierAccess and PriorityBoarding
                        MOBPremierAccess premierAccess = new MOBPremierAccess();
                        MOBPriorityBoarding priorityBoarding = new MOBPriorityBoarding();
                        var showPremierAccess = false;
                       var tupleRes= await GetPremierAccessAndPriorityBoarding(request, session, cslReservationDetail, productOffers, priorityBoarding,  premierAccess,  showPremierAccess);
                      priorityBoarding=tupleRes.priorityBoarding;
                    response.PremierAccess = tupleRes.premierAccess;
                        response.ShowPremierAccess = tupleRes.showPremierAccess;
                    response.Ancillary.PriorityBoarding = priorityBoarding;
                        #endregion
                }),
                System.Threading.Tasks.Task.Factory.StartNew(async() =>
                {
                    response.TripInsuranceInfo = await _merchandizingServices.GetTPIINfoDetails_CFOP(response.PNR.IsTPIIncluded,response.PNR.IsFareLockOrNRSA, request, session);
                }),
                System.Threading.Tasks.Task.Factory.StartNew(async() =>
                {
                    if(request.Flow != FlowType.VIEWRES_SEATMAP.ToString())
                    {
                        var productOffersFromCce = await _merchandizingServices.GetMerchOffersDetailsFromCCE(session, cslReservationDetail.Detail, request, request.Flow, response.PNR);

                        var taskArrayMerchOffers = new System.Threading.Tasks.Task[]
                        {
                            System.Threading.Tasks.Task.Factory.StartNew(() => {
                                response.Ancillary.TravelOptionsBundle = _merchandizingServices.TravelOptionsBundleOffer(productOffersFromCce, request, session.SessionId);
                            }),
                            System.Threading.Tasks.Task.Factory.StartNew(() => {
                                response.Ancillary.BasicEconomyBuyOut = _merchandizingServices.BasicEconomyBuyOutOffer(productOffersFromCce, request, session.SessionId,response.PNR);
                                if (response.Ancillary?.BasicEconomyBuyOut?.ElfRestrictionsBeBuyOutLink != null && (response?.PNR?.ELFLimitations?.Any() ?? false))
                                {
                                    response?.PNR?.ELFLimitations.Add(response.Ancillary.BasicEconomyBuyOut.ElfRestrictionsBeBuyOutLink);
                                }
                            })
                        };
                        System.Threading.Tasks.Task.WaitAll(taskArrayMerchOffers);
                    }
                }),
                System.Threading.Tasks.Task.Factory.StartNew(async() =>
                {
                    try
                    {
                        if (request.Flow != FlowType.VIEWRES_SEATMAP.ToString() && _configuration.GetValue<string>("ShowViewReservationDOTBaggaeInfo") != null &&
                            _configuration.GetValue<string>("ShowViewReservationDOTBaggaeInfo").ToUpper().Trim() == "TRUE"
                            && !FeatureVersionCheck(request.Application.Id, request.Application.Version.Major, "EnablePrePaidBags", "AndroidPrePaidBagsVersion", "iPhonePrePaidBagsVersion"))
                        {
                            DOTBaggageInfoResponse dotBaggageInfoResponse = await _merchandizingServices.GetDOTBaggageInfoWithPNR(request.AccessCode, request.SessionId, request.LanguageCode, "XML",request.Application.Id, request.RecordLocator, "01/01/0001", channelId, channelName, null, cslReservationDetail.Detail);

                            response.DotBaggageInformation = new DOTBaggageInfo();
                            response.DotBaggageInformation.SetMerchandizingServicesBaggageInfo(dotBaggageInfoResponse.DotBaggageInfo);
                            if (response.DotBaggageInformation.ErrorMessage.IsNullOrEmpty())
                            {
                                var dotBaggageInformation = await GetBaggageInfo(response.PNR, request.TransactionId);
                                response.DotBaggageInformation.SetDatabaseBaggageInfo(dotBaggageInformation);
                            }

                        }
                    }
                    catch (System.Exception ex)
                    {

                            MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);

                        response.DotBaggageInformation = new DOTBaggageInfo();
                        response.DotBaggageInformation.ErrorMessage = _configuration.GetValue<string>("DOTBaggageGenericExceptionMessage");
                    }
                }),
                System.Threading.Tasks.Task.Factory.StartNew(async() =>
                {
                    seatOffer = await _flightReservation.GetSeatOffer_CFOP(response.PNR, request, cslReservationDetail.Detail, session.Token, request.Flow, session.SessionId);
                }),
                System.Threading.Tasks.Task.Factory.StartNew(async() =>
                {
                    bundleInfo = await GetBundleInfo(request, channelId, channelName);
                }),
                 System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    ValidateIRROPSStatus(request,response,cslReservationDetail, session);
                }),
                System.Threading.Tasks.Task.Factory.StartNew(async()=>
                { //sandeep placepass changes
                     response.Ancillary.PlacePass = await GetPlacePass(request, response.PNR);
                }),
                System.Threading.Tasks.Task.Factory.StartNew(async()=> {
                     try
                    {
                        if (_configuration.GetValue<bool>("EnableMgnResUpdateTravelerInfo") && request.Flow != FlowType.VIEWRES_SEATMAP.ToString())
                        {
                            response.RewardPrograms = await GetAllRewardProgramItems
                            (request.Application.Id, request.DeviceId, request.Application.Version.Major, request.TransactionId, session.SessionId, session.Token);
                        }
                    }
                    catch (Exception ex)
                    {
                       _logger.LogError("GetPNRByRecordLocator_CFOP- GetAllRewardProgramItems Error{error}", JsonConvert.SerializeObject(ex));

                    }
                }),
                System.Threading.Tasks.Task.Factory.StartNew(()=>
                {
                     if (_configuration.GetValue<bool>("EnableAssociateMPNumber"))
                     {
                        MOBPNR pnr = response.PNR;
                        if (AssociateMPIDEligibilityCheck(pnr, request.MileagePlusNumber))
                        {
                                SetAssociateMPNumberDetails(response.PNR, request.MileagePlusNumber, request);
                        }
                    }
                }),
            };

            //Block until all tasks complete.
            System.Threading.Tasks.Task.WaitAll(taskArray);

            await System.Threading.Tasks.Task.Factory.StartNew(() =>
             {
                 if (_configuration.GetValue<bool>("EnableAssociateMPNumberSilentLogging"))
                 {
                     try
                     {
                         MOBPNR pnr = response.PNR;
                         if (AssociateMPIDEligibilityCheck(pnr, request.MileagePlusNumber))
                         {
                             string pnrPaxCount = (pnr.Passengers != null && pnr.Passengers.Count() > 0) ? Convert.ToString(pnr.Passengers.Count()) : "0";
                             string logStatement = string.Format("MP={0}, Lastname={1}, PNR={2},PaxInPNR={3}", request.MileagePlusNumber, request.LastName, response.PNR.RecordLocator, pnrPaxCount);
                             SaveLogToTempAnalysisTable(logStatement, request.SessionId);
                         }
                     }
                     catch (Exception) { }
                 }
             });

            #region
            response.PNR.SeatOffer = seatOffer;
            response.RecordLocator = response.PNR.RecordLocator;
            response.UARecordLocator = response.PNR.UARecordLocator;
            response.PNR.IsEnableEditTraveler = (!response.PNR.isgroup);
            response.ShowSeatChange = ShowSeatChangeButton(response.PNR);
            UpdatePnrWithBundleInfo(request, bundleInfo, ref response);
            #endregion
        }

        private async System.Threading.Tasks.Task<(MOBPriorityBoarding priorityBoarding, MOBPremierAccess premierAccess, bool showPremierAccess)> GetPremierAccessAndPriorityBoarding(MOBPNRByRecordLocatorRequest request, Session session, ReservationDetail cslReservationDetail, Service.Presentation.ProductResponseModel.ProductOffer productOffers, MOBPriorityBoarding priorityBoarding, MOBPremierAccess premierAccess, bool showPremierAccess)
        {
            if (productOffers != null)
            {
                try
                {
                    #region Premier Access and Priority Boarding
                    MOBAncillary ancillary = new MOBAncillary();
                    string jsonRequest = JsonConvert.SerializeObject(request);
                    string pnrCreatedDate = _merchandizingServices.GetPnrCreatedDate(cslReservationDetail.Detail);
                    var jsonResponse = JsonConvert.SerializeObject(productOffers);
                    var tupleRes = await _merchandizingServices.PBAndPADetailAssignment_CFOP(session.SessionId, request.Application, request.RecordLocator, request.LastName, pnrCreatedDate, priorityBoarding, premierAccess, jsonRequest, jsonResponse);
                    ancillary.PriorityBoarding = tupleRes.priorityBoarding;
                    premierAccess = tupleRes.premierAccess;
                    _merchandizingServices.PBAndPAAssignment(request.SessionId, ref ancillary, request.Application, request.DeviceId, priorityBoarding, "GetPAandPBInfoInViewRes", ref premierAccess, ref showPremierAccess);
                    priorityBoarding = ancillary.PriorityBoarding;
                    #endregion
                }
                catch (MOBUnitedException uaex)
                {
                    MOBExceptionWrapper uaexWrapper = new MOBExceptionWrapper(uaex);
                }

                catch (Exception)
                { }
            }
            return (priorityBoarding, premierAccess, showPremierAccess);
        }

        private bool FeatureVersionCheck(int appId, string appVersion, string featureName, string androidVersion, string iosVersion)
        {
            if (string.IsNullOrEmpty(appVersion) || string.IsNullOrEmpty(featureName))
                return false;
            return _configuration.GetValue<bool>(featureName)
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, androidVersion, iosVersion, "", "", true, _configuration);
        }

        private async Task<List<RewardProgram>> GetAllRewardProgramItems(int applicationId, string deviceId, string appVersion, string transactionId, string sessionID, string token)
        {
            //Check in Couchbase if it is available.
            var rewardProgram = await _cachingService.GetCache<List<RewardProgram>>(_configuration.GetValue<string>("FrequestFlyerRewardProgramListStaticGUID") + "Booking2.0FrequentFlyerList", transactionId).ConfigureAwait(false);//United.Persist.FilePersist.Load<List<MOBSHOPRewardProgram>>
            var rewardProgramList = JsonConvert.DeserializeObject<List<RewardProgram>>(rewardProgram);

            if (rewardProgramList == null || (rewardProgramList != null && rewardProgramList.Count == 0))
            {
                //If Not in Couchbase call CSL
                rewardProgramList = await GetRewardPrograms(applicationId, deviceId, appVersion, transactionId, sessionID, token).ConfigureAwait(false);

                //Finally save retrieved data to couchbase.
                await _cachingService.SaveCache<List<RewardProgram>>(_configuration.GetValue<string>("FrequestFlyerRewardProgramListStaticGUID") + "Booking2.0FrequentFlyerList", rewardProgramList, transactionId, new TimeSpan(1, 30, 0)).ConfigureAwait(false);

            }

            return rewardProgramList;
        }

        private async Task<List<RewardProgram>> GetRewardPrograms(int applicationId, string deviceId, string appVersion, string transactionId, string sessionID, string token)
        {
            var rewardPrograms = new List<RewardProgram>();
            var response = new Service.Presentation.ReferenceDataResponseModel.RewardProgramResponse();
            response.Programs = (await _referencedataService.RewardPrograms<Collection<Program>>(token, sessionID)).Response;

            if (response?.Programs?.Count > 0)
            {
                foreach (var reward in response.Programs)
                {
                    if (reward.ProgramID != 5)
                    {
                        rewardPrograms.Add(new RewardProgram() { Description = reward.Description, ProgramID = reward.ProgramID.ToString(), Type = reward.Code.ToString() });
                    }
                }
            }
            else
            {
                if (response.Errors != null && response.Errors.Count > 0)
                {
                    _logger.LogError("GetRewardPrograms - Response Error {sessionID}", sessionID);
                }
            }

            return rewardPrograms;
        }

        private void SetAssociateMPNumberDetails(MOBPNR pnr, string mpNumber, MOBPNRByRecordLocatorRequest request)
        {
            try
            {
                var customerSyncRestAPI = new SyncGatewayRESTClient(_configuration.GetValue<string>("SyncGatewayAdminUrl"), _configuration.GetValue<string>("SyncGatewayMappedPrivateBucket"));

                if (!string.IsNullOrEmpty(mpNumber) && customerSyncRestAPI != null && pnr != null)
                {
                    string predictableKey = "LOOKUP::ACCOUNT::" + mpNumber.ToUpper();

                    //SignedIn Customer Profile data from Couchbase
                    var document = customerSyncRestAPI.GetDocument<LookupAccountDetails>("", predictableKey);


                    if (document != null && pnr.Passengers != null && pnr.Passengers.Count > 0)
                    {

                        var profileFullName = (document.Name.First + document.Name.Middle + document.Name.Last).Trim().ToLower().Replace(" ", "");
                        var profileFullNameWithSiffix = (document.Name.First + document.Name.Middle + document.Name.Last + document.Name.Suffix).Trim().ToLower().Replace(" ", "").Replace(".", "");
                        var profileFirstLastNames = (document.Name.First + document.Name.Last).Trim().ToLower().Replace(" ", "");
                        var profileFirstLastNamesWithSuffix = (document.Name.First + document.Name.Last + document.Name.Suffix).Trim().ToLower().Replace(" ", "").Replace(".", "");

                        foreach (var pax in pnr.Passengers.Where(p => p.MileagePlus == null))
                        {
                            var pnrFullNames = (pax.PassengerName.First + pax.PassengerName.Middle + pax.PassengerName.Last).Trim().ToLower().Replace(" ", "");

                            if (profileFullName == pnrFullNames ||
                                profileFirstLastNames == pnrFullNames ||
                                profileFullNameWithSiffix == pnrFullNames ||
                                profileFirstLastNamesWithSuffix == pnrFullNames)
                            {
                                pnr.AssociateMPId = "true";
                                pnr.AssociateMPIdSharesGivenName = pax.SharesGivenName;
                                pnr.AssociateMPIdSharesPosition = pax.SHARESPosition;
                                pnr.AssociateMPIdMessage = _configuration.GetValue<string>("AssociateMPNumberPopupMsg");
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetPNRByRecordLocator_CFOP - SyncGateway LOOKUP - Exception{exception}", ex);
            }
        }

        private bool AssociateMPIDEligibilityCheck(MOBPNR pnr, string MileagePlusNumber)
        {
            //AssociateMPtoPNR is eligible if
            //      MOBPNR pnr object has atleast 1 Passenger &&
            //      MOBPNR pnr Passenger object has atleast 1 Passenger whose MP number is not already associated &&
            //      Logged in MP number is not already associated to the pnr

            if (string.IsNullOrEmpty(MileagePlusNumber) || pnr == null)
                return false;

            try
            {
                return (pnr.Passengers != null &&
                            pnr.Passengers.Any(p => p.MileagePlus == null) &&
                            !pnr.Passengers.Any(p => p.MileagePlus != null && MileagePlusNumber.Equals(p.MileagePlus.MileagePlusId, StringComparison.OrdinalIgnoreCase)));
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void UpdatePnrWithBundleInfo(MOBPNRByRecordLocatorRequest request, MOBBundleInfo bundleInfo, ref MOBPNRByRecordLocatorResponse response)
        {
            if (_configuration.GetValue<bool>("GetBundleInfo"))
            {
                try
                {
                    var isAwardAcceleratorVersion = GeneralHelper.IsApplicationVersionGreater(request.Application.Id, request.Application.Version.Major, "AndroidAwardAcceleratorVersion", "iPhoneAwardAcceleratorVersion", "", "", true, _configuration);
                    response.PNR.BundleInfo = bundleInfo;
                    if (response.PNR.Segments != null && response.PNR.BundleInfo != null &&
                        response.PNR.BundleInfo.FlightSegments != null)
                    {
                        foreach (var seg in response.PNR.Segments)
                        {
                            var bundleSeg = response.PNR.BundleInfo.FlightSegments.Find(x => x.DepartureAirport == seg.Departure.Code &&
                                                                                             x.ArrivalAirport == seg.Arrival.Code &&
                                                                                             x.FlightNumber == seg.FlightNumber);

                            if (bundleSeg != null)
                            {
                                seg.Bundles = new List<MOBBundle>();
                                foreach (var passenger in response.PNR.Passengers)
                                {
                                    var bundleTraveler = bundleSeg.Travelers.Find(t => t != null && t.Id != null && ((t.Id + ".1") == passenger.SHARESPosition || ("1." + t.Id) == passenger.SHARESPosition));
                                    var bundleLabel = bundleTraveler != null ? bundleTraveler.BundleDescription : string.Empty;
                                    //Remove Award Accelerator and Premier Accelerator texts for unsupported versions.
                                    if (!isAwardAcceleratorVersion && !bundleLabel.Equals(_configuration.GetValue<string>("BundlesCodeCommonDescription")))
                                    {
                                        bundleLabel = string.Empty;
                                    }
                                    MOBBundle bundle = new MOBBundle
                                    {
                                        PassengerSharesPosition = passenger.SHARESPosition,
                                        BundleDescription = bundleLabel
                                    };
                                    seg.Bundles.Add(bundle);
                                }
                            }
                        }
                    }
                }
                catch (System.Exception ex1)
                {
                    _logger.LogError("GetBundleInfoWithPNR Exception{exception}", ex1);

                    response.PNR.BundleInfo = new MOBBundleInfo();
                    response.PNR.BundleInfo.ErrorMessage = "Bundle information is not available.";
                }
            }
        }

        private bool ShowSeatChangeButton(MOBPNR pnr)
        {
            if (pnr.Passengers != null && pnr.Passengers.Count > 9 || pnr.isgroup == true)
            {
                return false;
            }

            return pnr.IsEligibleToSeatChange && _configuration.GetValue<bool>("ShowSeatChange");
        }

        private async System.Threading.Tasks.Task CheckForFuzziPNRAndSaveCommonDefPersistsFile(string clientSentPNR, string DeviceId, string cslRecordLocator, CommonDef commonDef)
        {
            //to resolve the issue where within the PNR there is a character 0, 1 or 5  and client sends O, I, or S, then while reading the CommonDef, there was no file with Device + PNR
            // Combination.  Hence adding this condition to save second CommonDef file by the Device + PNR sent by CSL service combination
            if (_configuration.GetValue<bool>("EnableFuzziPNRCheckChanges") && clientSentPNR.ToUpper().Trim() != cslRecordLocator.ToUpper().Trim())
            {
                await _sessionHelperService.SaveSession<CommonDef>(commonDef, (DeviceId + cslRecordLocator).Replace("|", "").Replace("-", "").ToUpper().Trim(), new List<string> { (DeviceId + cslRecordLocator).Replace("|", "").Replace("-", "").ToUpper().Trim(), commonDef.ObjectName }, commonDef.ObjectName).ConfigureAwait(false);
            }
        }

        private async Task<(MOBPNR pnr, ReservationDetail clsReservationDetail)> LoadPnr(MOBPNRByRecordLocatorRequest request, Session session)
        {
            MOBPNR pnr;
            ReservationDetail clsReservationDetail = null;
            var tupleRes = await _flightReservation.GetPNRByRecordLocatorFromCSL(request.SessionId, request.DeviceId, request.RecordLocator, request.LastName, request.LanguageCode, request.Application.Id, request.Application.Version.Major, false, session, clsReservationDetail, request.IsOTFConversion);
            pnr = tupleRes.pnr;
            clsReservationDetail = tupleRes.response;
            if (pnr.IsIBELite || pnr.IsIBE)
            {
                new ForceUpdateVersion(_configuration).ForceUpdateForNonSupportedVersion(request.Application.Id, request.Application.Version.Major, FlowType.ALL);
            }

            var eligibility = new EligibilityResponse
            {
                IsElf = pnr.isELF,
                IsIBELite = pnr.IsIBELite,
                IsIBE = pnr.IsIBE,
                Passengers = pnr.Passengers,
                Segments = pnr.Segments,
                IsUnaccompaniedMinor = pnr.IsUnaccompaniedMinor,
                InfantInLaps = pnr.InfantInLaps,
                IsReshopWithFutureFlightCredit = pnr.IsCanceledWithFutureFlightCredit,
                IsAgencyBooking = pnr.IsAgencyBooking,
                AgencyName = pnr.AgencyName,
                IsCheckinEligible = pnr.IsCheckinEligible,
                IsCorporateBooking = pnr.IsCorporateBooking,
                CorporateVendorName = pnr.CorporateVendorName,
                IsBEChangeEligible = pnr.IsBEChangeEligible,
                HasScheduleChange = pnr.HasScheduleChanged,
                IsSCChangeEligible = pnr.IsSCChangeEligible,
                IsSCRefundEligible = pnr.IsSCRefundEligible,
                IsATREEligible = pnr.IsATREEligible,
                IsMilesAndMoney = pnr.IsMilesAndMoney,
                Is24HrFlexibleBookingPolicy = pnr.Is24HrFlexibleBookingPolicy,
                IsJSENonChangeableFare = pnr.IsJSENonChangeableFare
            };

            await _sessionHelperService.SaveSession<EligibilityResponse>(eligibility, pnr.SessionId, new List<string> { pnr.SessionId, eligibility.ObjectName }, eligibility.ObjectName).ConfigureAwait(false);
            await _sessionHelperService.SaveSession<Session>(session, (request.DeviceId + request.RecordLocator).Replace("|", "").Replace("-", "").ToUpper().Trim(), new List<string> { (request.DeviceId + request.RecordLocator).Replace("|", "").Replace("-", "").ToUpper().Trim(), session.ObjectName }, session.ObjectName).ConfigureAwait(false);

            return (pnr, clsReservationDetail);
        }

        private async Task<DOTBaggageInfo> GetBaggageInfo(MOBPNR pnr, string transactionId)
        {
            var isElf = pnr != null && pnr.isELF;
            var isIbe = pnr != null && pnr.IsIBE;
            return await GetBaggageInfo(isElf, isIbe, transactionId);
        }
        private string GetList(List<string> titles)
        {
            StringBuilder stringBuilder = new StringBuilder();

            foreach (var title in titles)
            {
                stringBuilder.Append("'");
                stringBuilder.Append(title);
                stringBuilder.Append("'");
                stringBuilder.Append(",");
            }

            return stringBuilder.ToString().Trim(',');
        }

        private async Task<DOTBaggageInfo> GetBaggageInfo(bool isElf, bool isIBE, string transactionId)
        {
            //var titleList = GetList(Titles);

            if (cachedLegalDocuments.IsNull())
            {
                foreach (var title in Titles)
                {
                    var cachedLegalDocument = await _legalDocumentsForTitlesService.GetNewLegalDocumentsForTitles(title, transactionId, true).ConfigureAwait(false);
                    if (cachedLegalDocument.IsNullOrEmpty())
                    {
                        cachedLegalDocuments.Add(cachedLegalDocument[0]);
                    }
                }
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

        private async Task<MOBBundleInfo> GetBundleInfo(MOBPNRByRecordLocatorRequest request, string channelId, string channelName)
        {
            if (!_configuration.GetValue<bool>("GetBundleInfo"))
                return null;

            if (request == null || request.Flow == FlowType.VIEWRES_SEATMAP.ToString())
                return null;

            MOBBundleInfo bundleInfo = null;
            try
            {
                MOBBundlesMerchangdizingRequest bundleRequest = new MOBBundlesMerchangdizingRequest();
                bundleRequest.Application = request.Application;
                bundleRequest.TransactionId = request.SessionId;
                bundleRequest.RecordLocator = request.RecordLocator;

                MOBBundlesMerchandizingResponse bundleResponse = await _merchandizingServices.GetBundleInfoWithPNR(bundleRequest, channelId, channelName);
                bundleInfo = bundleResponse != null ? bundleResponse.BundleInfo : null;

            }
            catch (System.Exception ex1)
            {
                bundleInfo = new MOBBundleInfo();
                bundleInfo.ErrorMessage = "Bundle information is not available.";
            }

            return bundleInfo;
        }

        private async void ValidateIRROPSStatus(MOBPNRByRecordLocatorRequest request, MOBPNRByRecordLocatorResponse response,
            ReservationDetail cslReservationDetail, Session session)
        {
            try
            {
                if (IRROPRedirectEnabled(request.Application.Id, request.Application.Version.Major) == false) return;

                if (response.PNR.IsCanceledWithFutureFlightCredit
                    || !response.PNR.IsETicketed || !string.IsNullOrEmpty(response.PNR.SyncedWithConcur)) return;

                response.PNR.IRROPSChangeInfo = response.PNR.IRROPSChangeInfo ?? new MOBIRROPSChange();
                response.PNR.IRROPSChangeInfo = await _flightReservation.ValidateIRROPSStatus(request, response, cslReservationDetail, session);
            }
            catch { response.PNR.IRROPSChangeInfo = null; }
        }

        private bool IRROPRedirectEnabled(int applicationId, string applicationVersion)
        {
            //Utility.GetBooleanConfigValue("ExcludeOTFConversion")
            return !_configuration.GetValue<bool>("ExcludeValidateIRROPSStatus") &&
               GeneralHelper.IsApplicationVersionGreaterorEqual(applicationId, applicationVersion,
                    _configuration.GetValue<string>("Android_EnableIRROPRedirectAppVersion"), _configuration.GetValue<string>("iPhone_EnableIRROPRedirectAppVersion"));
        }

        private async Task<MOBPlacePass> GetPlacePass(MOBPNRByRecordLocatorRequest request, MOBPNR pnr)
        {
            if (request == null || request.Flow == FlowType.VIEWRES_SEATMAP.ToString())
                return null;

            //sandeep placepass changes
            MOBPlacePass placePass = null;
            if (_configuration.GetValue<bool>("PlacePassTurnOnToggle_Manageres") && !pnr.IsNullOrEmpty()
                && EnablePlacePassManageRes(request.Application.Id, request.Application.Version.Major)
                && !EnableViewResDynamicPlacePass(request.Application.Id, request.Application.Version.Major)
                ) // for 2.1.63 and Below condition shouldbe if( T && T && F) for 2.1.64 and Above if(T && T && T)
            {
                try
                {
                    string searchtype = pnr.JourneyType;
                    string destinationcode = pnr.Trips.Select(t => t.Destination).ToList()[0];
                    placePass = await GetEligiblityPlacePass(destinationcode, searchtype, request.TransactionId, request.Application.Id, request.Application.Version.Major, request.DeviceId, "GetPNRByRecordLocatorPlacePass");
                }
                catch (Exception ex)
                {
                    MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                    _logger.LogError("GetPNRByRecordLocator - Placepass Exception{exception}", ex);
                }
            }
            return placePass;
        }
        #region//utilitites
        private bool EnablePlacePassManageRes(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("PlacePassTurnOnToggle_Manageres")
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidPlacePassVersion", "iPhonePlacePassVersion", "", "", true, _configuration);
        }
        private bool EnableViewResDynamicPlacePass(int appId, string appVersion)
        {
            return _configuration.GetValue<bool>("PlacePassServiceTurnOnToggle_ViewReservation")
                    && GeneralHelper.IsApplicationVersionGreater(appId, appVersion, "AndroidPlacePassVersion_ViewResDynamic", "iPhonePlacePassVersion_ViewResDynamic", "", "", true, _configuration);
        }

        private async Task<MOBPlacePass> GetEligiblityPlacePass(string destinationAiportCode, string tripType, string sessionId, int appID, string appVersion, string deviceId, string logAction)
        {
            #region Load Macthed Place Pass from the Persist Place Passes List
            MOBPlacePass matchedPlacePass = new MOBPlacePass();
            try
            {
                #region
                int flag = 2;
                //Should be Cache not Session
                var response = await _cachingService.GetCache<string>(_configuration.GetValue<string>("GetAllEligiblePlacePassesAndSaveToPersistStaticGUID") + "AllEligiblePlacePasses", _headers.ContextValues?.TransactionId).ConfigureAwait(false); //change session
                List<MOBPlacePass> placePassListFromPersist = JsonConvert.DeserializeObject<List<MOBPlacePass>>(response);

                if (placePassListFromPersist == null)
                {
                    placePassListFromPersist = await GetAllEligiblePlacePasses(sessionId, appID, appVersion, deviceId, logAction, true);
                }

                //logEntries.Add(LogEntry.GetLogEntry<List<MOBPlacePass>>(sessionId, logAction, "MOBPlacepassResponseFromPersist", appID, appVersion, deviceId, placePassListFromPersist, true, false));

                if (!string.IsNullOrEmpty(destinationAiportCode) && !string.IsNullOrEmpty(tripType))
                {
                    switch (tripType.ToLower())
                    {
                        case "ow":
                        case "one_way":
                            flag = 1;
                            break;
                        case "rt":
                        case "round_trip":
                            flag = 1;
                            break;
                        case "md":
                        case "multi_city":
                            flag = 2;
                            break;
                    }

                    foreach (MOBPlacePass placePass in placePassListFromPersist)
                    {
                        if (placePass.PlacePassID == 1)
                        {
                            matchedPlacePass = placePass;
                        }
                        if (placePass.Destination.Trim().ToUpper() == destinationAiportCode.Trim().ToUpper() && flag == 1)
                        {
                            matchedPlacePass = placePass;
                            break;
                        }
                    }
                }
                #endregion
            }
            catch (Exception ex)
            {
                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                //logEntries.Add(LogEntry.GetLogEntry(sessionId, logAction, "Exception", appID, appVersion, deviceId, exceptionWrapper));

                matchedPlacePass = new MOBPlacePass();
            }
            #endregion Load Mached Place Pass from the Persist Place Passes List
            return matchedPlacePass;
        }

        private async Task<List<MOBPlacePass>> GetAllEligiblePlacePasses(string sessionId, int appID, string appVersion, string deviceId, string logAction, bool saveToPersist)
        {
            List<MOBPlacePass> placepasses = new List<MOBPlacePass>();
            logAction = logAction == null ? "GetAllEligiblePlacePasses" : logAction + "- GetAllEligiblePlacePasses";

            var manageresDynamoDB = new ManageResDynamoDB(_configuration, _dynamoDBService);
            string destinationCode = "ALL";
            int flag = 3;
            var eligibleplace = await manageresDynamoDB.GetAllEligiblePlacePasses<List<MOBPlacePass>>(destinationCode, flag, sessionId).ConfigureAwait(false);

            try
            {
                //while (var eligibleplace)
                //{
                //    MOBPlacePass placepass = new MOBPlacePass();
                //    placepass.PlacePassID = Convert.ToInt32(eligibleplace[0]);
                //    placepass.Destination = dataReader["DestinationCode"].ToString().Trim();
                //    placepass.PlacePassImageSrc = dataReader["PlacePassImageSrc"].ToString().Trim();
                //    placepass.OfferDescription = dataReader["CityDescription"].ToString().Trim();
                //    placepass.PlacePassUrl = dataReader["PlacePassUrl"].ToString().Trim();
                //    placepass.TxtPoweredBy = "Powered by";
                //    placepass.TxtPlacepass = "PLACEPASS";
                //    placepasses.Add(placepass);
                //}
                foreach (var place in eligibleplace)
                {
                    MOBPlacePass placepass = new MOBPlacePass();
                    placepass.PlacePassID = Convert.ToInt32(place.PlacePassID);
                    placepass.Destination = place.Destination;
                    placepass.PlacePassImageSrc = place.PlacePassImageSrc;
                    placepass.OfferDescription = place.OfferDescription;
                    placepass.PlacePassUrl = place.PlacePassUrl;
                    placepass.TxtPoweredBy = "Powered by"; ;
                    placepass.PlacePassUrl = "PLACEPASS";
                    placepasses.Add(placepass);
                }

            }
            catch (Exception ex)
            {
                MOBExceptionWrapper exceptionWrapper = new MOBExceptionWrapper(ex);
                //logEntries.Add(LogEntry.GetLogEntry(sessionId, logAction, "Exception", appID, appVersion, deviceId, exceptionWrapper));
            }
            return placepasses;
        }

        private async void SaveLogToTempAnalysisTable(string logStatement, string sessionId)
        {
            LogData data = new LogData
            {
                Comment = logStatement,
                Count = 0
            };

            var manageResDynamodb = new ManageResDynamoDB(_configuration, _dynamoDBService);
            await manageResDynamodb.SaveLogToTempAnalysisTable<LogData>(data, logStatement, sessionId);

        }
        #endregion

        public string GetTripDetailRedirect3dot0Url
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

        private string EncryptString(string data)
        {
            //TODO to find the library
            //return ECommerce.Framework.Utilities.SecureData.EncryptString(data);
            return default;
        }
    }
}
