
#region 
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using United.Common.Helper;
using United.Common.Helper.Shopping;
using United.Common.Helper.Traveler;
using United.ECommerce.Framework.Utilities;
using United.Mobile.DataAccess.Common;
using United.Mobile.DataAccess.DynamoDB;
using United.Mobile.DataAccess.FlightShopping;
using United.Mobile.DataAccess.FlightStatus;
using United.Mobile.DataAccess.ManageReservation;
using United.Mobile.DataAccess.MerchandizeService;
using United.Mobile.DataAccess.OnPremiseSQLSP;
using United.Mobile.DataAccess.Shopping;
using United.Mobile.DataAccess.ShopProducts;
using United.Mobile.DataAccess.ShopTrips;
using United.Mobile.Model;
using United.Mobile.Model.BagCalculator;
using United.Mobile.Model.Common;
using United.Mobile.Model.Internal.Exception;
using United.Mobile.Model.MPRewards;
using United.Service.Presentation.CommonEnumModel;
using United.Service.Presentation.CommonModel;
using United.Service.Presentation.PaymentModel;
using United.Service.Presentation.PersonalizationModel;
using United.Service.Presentation.PersonalizationRequestModel;
using United.Service.Presentation.PersonalizationResponseModel;
using United.Service.Presentation.PriceModel;
using United.Service.Presentation.ProductModel;
using United.Service.Presentation.ReservationRequestModel;
using United.Service.Presentation.ReservationResponseModel;
using United.Service.Presentation.SegmentModel;
using United.Service.Presentation.ValueDocumentModel;
using United.Services.FlightShopping.Common.DisplayCart;
using United.Services.FlightShopping.Common.Extensions;
using United.Utility.Enum;
using United.Utility.Helper;
using Characteristic = United.Service.Presentation.CommonModel.Characteristic;
using Content = United.Service.Presentation.PersonalizationModel.Content;
using Reservation = United.Mobile.Model.Shopping.Reservation;
using ReservationType = United.Service.Presentation.CommonEnumModel.ReservationType;
#endregion

namespace United.Mobile.BagCalculator.Domain
{
    public class BagCalculatorBusiness : IBagCalculatorBusiness
    {
        private readonly ICacheLog<BagCalculatorBusiness> _logger;
        private readonly IConfiguration _configuration;
        private const string ProductCodeBag = "BAG";
        private const string CountryCode = "US";
        private const string LangCode = "en-US";
        private readonly IFlightShoppingService _flightShoppingService;
        private readonly IDPService _tokenService;
        private readonly ISessionHelperService _sessionHelperService;
        private readonly IPNRRetrievalService _pNRRetrievalService;
        private readonly ICMSContentHelper _cMSContentHelper;
        private readonly IHeaders _headers;
        private readonly IMerchOffersService _merchOffersService;
        private readonly IDynamoDBService _dynamoDBService;
        private AirportDynamoDB _airportDynamoDB;
        private readonly IShoppingSessionHelper _shoppingSessionHelper;
        private readonly IAirlineCarrierService _airlineCarrierService;
        private readonly ILegalDocumentsForTitlesService _legalDocumentsForTitlesService;
        public readonly IShoppingUtility _shoppingUtility;
        private readonly IShoppingCcePromoService _shoppingCcePromoService;
        private readonly ICSLStatisticsService _cSLStatisticsService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IValidateHashPinService _validateHashPinService;
        private readonly ICachingService _cachingService;
        private readonly IOptimizelyPersistService _optimizelyPersistService;
        private readonly IFeatureSettings _featureSettings;
        private readonly ICCEDynamicOffersService _cceService;
        private readonly ICCEDynamicOfferDetailsService _cceDODService;
        public BagCalculatorBusiness(ICacheLog<BagCalculatorBusiness> logger
           , IConfiguration configuration
           , IHeaders headers
           , IFlightShoppingService flightShoppingService
           , ISessionHelperService sessionHelperService
           , IDPService tokenService
           , IPNRRetrievalService pNRRetrievalService
           , ICMSContentHelper cMSContentHelper
           , IMerchOffersService merchOffersService
           , IDynamoDBService dynamoDBService
           , IShoppingSessionHelper shoppingSessionHelper
           , IAirlineCarrierService airlineCarrierService
           , ILegalDocumentsForTitlesService legalDocumentsForTitlesService
           , IShoppingUtility shoppingUtility
           , ICachingService cachingService
           , IShoppingCcePromoService shoppingCcePromoService
           , ICSLStatisticsService cSLStatisticsService, IShoppingCartService shoppingCartService, IValidateHashPinService validateHashPinService
           , IOptimizelyPersistService optimizelyPersistService
           , IFeatureSettings featureSettings
            , ICCEDynamicOffersService cceService
            , ICCEDynamicOfferDetailsService cceDODService)
        {
            _logger = logger;
            _configuration = configuration;
            _flightShoppingService = flightShoppingService;
            _sessionHelperService = sessionHelperService;
            _tokenService = tokenService;
            _pNRRetrievalService = pNRRetrievalService;
            _cMSContentHelper = cMSContentHelper;
            _headers = headers;
            _merchOffersService = merchOffersService;
            _dynamoDBService = dynamoDBService;
            _airportDynamoDB = new AirportDynamoDB(_configuration, _dynamoDBService);
            _shoppingSessionHelper = shoppingSessionHelper;
            _airlineCarrierService = airlineCarrierService;
            _legalDocumentsForTitlesService = legalDocumentsForTitlesService;
            _shoppingUtility = shoppingUtility;
            _shoppingCcePromoService = shoppingCcePromoService;
            _cachingService = cachingService;
            _cSLStatisticsService = cSLStatisticsService;
            _shoppingCartService = shoppingCartService;
            _validateHashPinService = validateHashPinService;
            _optimizelyPersistService = optimizelyPersistService;
            _featureSettings = featureSettings;
            _cceService = cceService;
            _cceDODService = cceDODService;
        }
        #region GetBaggageCalculatorSearch


        public async Task<BaggageCalculatorSearchResponse> GetBaggageCalculatorSearch(string accessCode, string transactionId, string languageCode, string appVersion, int applicationId)
        {
            MOBRequest request = new MOBRequest
            {
                AccessCode = accessCode,
                TransactionId = transactionId,
                LanguageCode = languageCode,
                Application = new MOBApplication
                {
                    Id = applicationId,
                    Version = new MOBVersion
                    {
                        Build = appVersion,
                        Major = appVersion,
                        Minor = appVersion,
                        DisplayText = appVersion
                    }
                }
            };

            _logger.LogInformation("GetBaggageCalculatorSearchValuesRequest {Request} {transactionId}", JsonConvert.SerializeObject(request), transactionId);

            var response = new BaggageCalculatorSearchResponse();
            
            if (GeneralHelper.ValidateAccessCode(accessCode))
            {
                response.Carriers = await _airlineCarrierService.GetCarriersInfoDetails(transactionId);
                response.LoyaltyLevels = await GetMemberShipStatuses();
                response.ClassOfServices = GetClassOfServices();
            }
            else
            {
                throw new MOBUnitedException("The access code you provided is not valid.");
            }
            return response;
        }

        private List<MemberShipStatus> MileagePlusMemberships()
        {
            return new List<MemberShipStatus>
            {
                new MemberShipStatus("GeneralMember", "General Member", "1", ""),
                new MemberShipStatus("PremierSilver", "Premier Silver member", "2", "MileagePlus Premier® member"),
                new MemberShipStatus("PremierGold", "Premier Gold member", "2", "MileagePlus Premier® member"),
                new MemberShipStatus("PremierPlatinum", "Premier Platinum member", "2", "MileagePlus Premier® member"),
                new MemberShipStatus("Premier1K", "Premier 1K member", "2", "MileagePlus Premier® member"),
                new MemberShipStatus("GlobalServices", "Global Services member", "2", "MileagePlus Premier® member")
            };
        }

        private List<MemberShipStatus> StarAllianceMemberships()
        {
            return new List<MemberShipStatus>
            {
                new MemberShipStatus("StarAllianceGold", "Star Alliance Gold member", "3", "Star Alliance status"),
                new MemberShipStatus("StarAllianceSilver", "Star Alliance Silver member", "3", "Star Alliance status")
            };
        }

        private async Task<List<MemberShipStatus>> CreditCardsTypes()
        {
            var isNewCreditCards = await _featureSettings.GetFeatureSettingValue("BagCal_AddNewMPCards").ConfigureAwait(false);
            var cards = isNewCreditCards
                ? new List<MemberShipStatus>
                {
                    new MemberShipStatus("MEC" , "United Explorer Card member", "4", "MileagePlus cardmember"),
                    new MemberShipStatus("MT"  , "United Quest Card member", "4", "MileagePlus cardmember"),
                    new MemberShipStatus("CCC" , "United Club Infinite Card member ", "4", "MileagePlus cardmember"),
                    new MemberShipStatus("PPC1", "United Presidential Plus Card member ", "4", "MileagePlus cardmember"),
                    new MemberShipStatus("PPC2", "United Presidential Plus Business Card member", "4", "MileagePlus cardmember"),
                    new MemberShipStatus("UBC" , "United Business Card member ", "4", "MileagePlus cardmember"),
                    new MemberShipStatus("UCC" , "United Club Business Card member ", "4", "MileagePlus cardmember")
                }
                : new List<MemberShipStatus>
                {
                    new MemberShipStatus("MEC", "MileagePlus Explorer Card member", "4", "MileagePlus cardmember"),
                    new MemberShipStatus("CCC", "MileagePlus Club Card member", "4", "MileagePlus cardmember"),
                    new MemberShipStatus("PPC", "Presidental Plus Card member", "4", "MileagePlus cardmember")
                };
                
            return cards;
        }

        private List<MemberShipStatus> MilitaryTypes()
        {
            return new List<MemberShipStatus>
            {
                new MemberShipStatus("MIR", "U.S. Military on orders or relocating", "5", "Active U.S. military"),
                new MemberShipStatus("MIL", "U.S. Military personal travel", "5", "Active U.S. military")
            };
        }
        private async Task<List<MemberShipStatus>> GetMemberShipStatuses()
        {
            var memberShipStatuses = new List<MemberShipStatus>();
            memberShipStatuses.AddRange(MileagePlusMemberships());
            memberShipStatuses.AddRange(StarAllianceMemberships());
            memberShipStatuses.AddRange(await CreditCardsTypes());
            memberShipStatuses.AddRange(MilitaryTypes());

            return memberShipStatuses;
        }
        private List<ClassOfService> GetClassOfServices()
        {
            List<ClassOfService> classOfServices = _configuration.GetValue<string>("DOTBagCalculatorClassOfServices")?.Split('|')
                .Select(x => new ClassOfService(x.Split('~')[0].ToString(), x.Split('~')[1].ToString()))
                .ToList();
            return classOfServices;
        }

        #endregion
       
        private void SetMerchandizeChannelValues(string merchChannel, ref string channelId, ref string channelName)
        {
            channelId = string.Empty;
            channelName = string.Empty;

            if (!string.IsNullOrEmpty(merchChannel))
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
                    case "MBE":
                        channelId = _configuration.GetValue<string>("MerchandizeOffersServiceMBEChannelID").Trim();
                        channelName = _configuration.GetValue<string>("MerchandizeOffersCSLServiceChannelName").Trim();
                        break;
                    default:
                        break;
                }
            }
        }
        
        private string GetWordForTheNumbers(int bagCount)
        {
            var numberInWords = new List<string> { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
            "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen", "twenty"};
            
            return bagCount > 20 ? bagCount.ToString() : numberInWords[bagCount];
        }

        private string TicketedNumber(Collection<ValueDocument> tickets)
        {
            if (tickets == null) return null;

            var eTicket = tickets.FirstOrDefault(t => t.Type == "ETicketNumber");
            return eTicket?.DocumentID;
        }

        private string TicketingDate(Collection<ValueDocument> tickets)
        {
            if (tickets == null) return DefaultDate;

            var eTicket = tickets.FirstOrDefault(t => t.Type == "ETicketNumber");
            return eTicket == null ? DefaultDate : eTicket.IssueDate;
        }

        private string DefaultDate
        {
            get { return new DateTime(0001, 01, 01).ToString(CultureInfo.InvariantCulture); }
        }
        
        private async Task<ReservationDetail> RetrivePnrDetails(MOBRequest mobRequest, string recordLocator, string lastName)
        {
            var request = new RetrievePNRSummaryRequest
            {
                Channel = _configuration.GetValue<string>("ChannelName"),
                IsIncludeETicketSDS = _configuration.GetValue<string>("IsIncludeETicketSDS"),
                IsIncludeFlightRange = _configuration.GetValue<string>("IsIncludeFlightRange"),
                IsIncludeFlightStatus = _configuration.GetValue<string>("IsIncludeFlightStatus"),
                IsIncludeLMX = _configuration.GetValue<string>("IsIncludeLMX"),
                IsIncludePNRDB = _configuration.GetValue<string>("IsIncludePNRDB"),
                IsIncludeSegmentDuration = _configuration.GetValue<string>("IsIncludeSegmentDuration"),
                ConfirmationID = recordLocator.ToUpper().Trim(),
                LastName = lastName,
                IsIncludePNRChangeEligibility = _configuration.GetValue<string>("IsIncludePNRChangeEligibility")
            };
            string token = await _tokenService.GetAnonymousToken(mobRequest.Application.Id, _headers.ContextValues.DeviceId, _configuration);
            var jsonResponse = await _pNRRetrievalService.PNRRetrieval(token: token, JsonConvert.SerializeObject(request), mobRequest.TransactionId);
            return DeserializeUseContract<ReservationDetail>(jsonResponse);
        }

        private T DeserializeUseContract<T>(string jsonString)
        {
            DataContractJsonSerializer dataContractJsonSerializer = new DataContractJsonSerializer(typeof(T));
            MemoryStream memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
            T obj = (T)dataContractJsonSerializer.ReadObject(memoryStream);
            return obj;
        }


        private string GetEliteStatusValue(int statusLevel)
        {
            switch (statusLevel)
            {
                case 0:
                    return "GeneralMember";
                case 1:
                    return "PremierSilver";
                case 2:
                    return "PremierGold";
                case 3:
                    return "PremierPlatinum";
                case 4:
                    return "Premier1K";
                case 5:
                    return "GlobalServices";
                default:
                    return "";
            }
        }
        
        #region GetMobileCMSContentsData

        public async Task<MobileCMSContentResponse> GetMobileCMSContentsData(MobileCMSContentRequest request)
        {
            #region

            MobileCMSContentResponse response = new MobileCMSContentResponse();

            try
            {
                Session session = new Session();
                if (string.IsNullOrEmpty(request.SessionId))
                {
                    session = await _shoppingSessionHelper.CreateShoppingSession(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _headers.ContextValues.Application.Version.Major.ToString(), _headers.ContextValues.TransactionId, request.MileagePlusNumber, string.Empty);
                }
                else
                {
                   session = await _shoppingSessionHelper.GetBookingFlowSession(request.SessionId);
                }
                request.Token = session.Token;
                request.CartId = session.CartId;
                response.CartId = request.CartId;
                response.SessionId = request.SessionId;
                response.Token = session.Token;
                response.MileagePlusNumber = request.MileagePlusNumber;
                response.TransactionId = request.TransactionId;

                var reservation = new Reservation();

                try
                {
                    reservation = await _sessionHelperService.GetSession<Reservation>(request.SessionId, new Reservation().ObjectName, new List<string> { request.SessionId, new Reservation().ObjectName });
                }
                catch (System.Exception) { }

                if (!request.GetShopTnC)
                {
                    response.MobileCMSContentMessages = _cMSContentHelper.GetMobileCMSContents(request).Result;
                }
                else
                {
                    response.MobileCMSContentMessages = _cMSContentHelper.GetMobileTermsAndConditions(request).Result;
                    if (_configuration.GetValue<bool>("BasicEconomyContentChange") && reservation != null)
                    {
                        if (reservation != null && reservation.Trips != null && Convert.ToDateTime(reservation.Trips[0].DepartDate).Year >= 2020)
                        {
                            if (response.MobileCMSContentMessages.Any(t => !string.IsNullOrEmpty(t.Title) && t.Title == "Fare terms and conditions 2020"))
                            {
                                response.MobileCMSContentMessages.RemoveAll(t => !string.IsNullOrEmpty(t.Title) && t.Title == "Fare terms and conditions");
                                response.MobileCMSContentMessages.RemoveAll(t => !string.IsNullOrEmpty(t.Title) && t.Title == "Fare terms and conditions 1920");
                                (response.MobileCMSContentMessages.Where(t => !string.IsNullOrEmpty(t.Title) && t.Title == "Fare terms and conditions 2020").FirstOrDefault().Title) = "Fare terms and conditions";
                            }
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                if (!_configuration.GetValue<bool>("SurfaceErrorToClient"))
                {
                    _logger.LogError("GetMobileCMSContents Error {@Exception}", JsonConvert.SerializeObject(ex));
                    response.Exception = new MOBException("9999", _configuration.GetValue<string>("Booking2OGenericExceptionMessage"));
                }
                else
                {
                    _logger.LogError("GetMobileCMSContents Error {@Exception}", JsonConvert.SerializeObject(ex));
                    response.Exception = new MOBException("9999", ex.Message);
                }
            }
            if (_configuration.GetValue<bool>("Log_CSL_Call_Statistics"))
            {
                try
                {
                    CSLStatistics _cslStatistics = new CSLStatistics(_logger, _configuration, _dynamoDBService, _cSLStatisticsService);
                    string callDurations = string.Empty;
                    await _cslStatistics.AddCSLCallStatisticsDetails(string.Empty, "REST_Shopping", response.CartId, callDurations, "Traveler/GetMobileCMSContents", request.SessionId);

                }
                catch { }
            }
            return response;

            #endregion
        }
        #endregion

        #region CheckedBagEstimatesForAnyFlight
        public async Task<DOTCheckedBagCalculatorResponse> CheckedBagEstimatesForAnyFlight(DOTCheckedBagCalculatorRequest request)
        {
            var offerRequest = await BuildRequestForAnyFlightCheckedBagInfoCCE(request);

           // _logger.LogInformation("CheckedBagEstimatesForAnyFlight-GetDynamicOffers {cslRequest} {TransactionId}", JsonConvert.SerializeObject(offerRequest), request.TransactionId);
            var token = await _tokenService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration);
            var jsonResponse = "";
            if(!await _featureSettings.GetFeatureSettingValue("EnableCCEDynamicOfferUrls").ConfigureAwait(false))
            {
                jsonResponse = await _shoppingCcePromoService.DynamicOffers(token, JsonConvert.SerializeObject(offerRequest), request.TransactionId);
            }
            else
            {
                jsonResponse = await _cceService.GetDynamicOffers(token, JsonConvert.SerializeObject(offerRequest));
            }
           // _logger.LogInformation("CheckedBagEstimatesForAnyFlight-GetDynamicOffers {cslResponse} {TransactionId}", jsonResponse, request.TransactionId);

            var offerResponse = JsonConvert.DeserializeObject<DynamicOfferResponse>(jsonResponse);
            ResponseData bagResponseData = offerResponse.ResponseData.ToObject<ResponseData>();
            var sdlContent = await GetSDLContentForCheckedBags(request, token);
         
            var bagOffer = bagResponseData.InlineOffers?.FirstOrDefault(item => item?.Code == "BAG");
            var response = new DOTCheckedBagCalculatorResponse
            {
                Request = request,
                PageTitle = GetBAGMISCText(bagOffer.Details, "BAGMISC_7"),
                CardBenefitMessage = ChaseAd(bagOffer),
                FooterText = GetBAGMISCText(bagOffer.Details, "BAGMISC_2"),
                ListBaggageFeesPerSegment = await ListBaggageFeesPerSegment(bagOffer, request.ClassOfService, request.LoyaltyLevel),
                AdditionalBagfareDetailsButtonTitle = _configuration.GetValue<string>("ButtonTextAdditionalBags"),
                AdditionalMeasuresBagFeeDetails = AdditionalMeasuresBagFeeDetails(bagOffer.Details),
                AdditionalBagLimitsButtonTitle = _configuration.GetValue<string>("ButtonTextSizeWeightBags"),
                BagSizeAndWeightLimits = BagSizeAndWeightLimits(sdlContent),
                FaqButtonTitle = _configuration.GetValue<string>("ButtonTextFAQs"),
                Faq = BagFaq(offerResponse.DynamicContent),
                IsAnyFlightSearch = true
            };
            return response;
        }

        private Faq BagFaq(dynamic dynamicContent)
        {
            SDLBody faqContent = dynamicContent.ToObject<SDLBody>();
            return new Faq
            {
                PageTitle = _configuration.GetValue<string>("PageTitleFAQs"),
                ListFaqMessages = faqContent.content.Select(c =>
                    new FaqMessage
                    {
                        Header = c.content.title,
                        ListQuestionAndAnswers = c.content.sections.Select(s =>
                            new QuestionAndAnswers
                            {
                                Question = s.content.title,
                                Answer = s.content.body
                            }).ToList()
                    }).ToList()
            };
        }

        private BagSizeAndWeightLimits BagSizeAndWeightLimits(List<CMSContentMessage> content)
        {
            var listWeightLimits = new List<WeightLimits>();
            var wtByBag = GetWeightLimits(content?.FirstOrDefault(m => m?.Title == "Bags_SizeAndWeightlimitsbyCabin_MOB"));
            if (wtByBag != null)
            {
                listWeightLimits.Add(wtByBag);
            }
            var wtByMp = GetWeightLimits(content?.FirstOrDefault(m => m?.Title == "Bags_SizeAndWeightlimitsbyMileagePlus_MOB"));
            if (wtByMp != null)
            {
                listWeightLimits.Add(wtByMp);
            }
            return new BagSizeAndWeightLimits()
            {
                PageTitle = _configuration.GetValue<string>("PageTitlesizeAndWeightLimits"),
                LimitsMessage = content?.FirstOrDefault(m => m?.Title == "Bags_SizeAndWeightLimitsMessage_MOB")?.ContentFull,
                AdditionalServiceChargesMessage = content?.FirstOrDefault(m => m?.Title == "Bags_SizeAndWeightLimitsAdditionalServiceChargesMessage_MOB")?.ContentFull,
                ListWeightLimits = listWeightLimits
            };
        }

        private static WeightLimits GetWeightLimits(CMSContentMessage contentMessage)
        {
            if (contentMessage == null) { return null; }
            return new WeightLimits
            {
                Header = contentMessage.Headline,
                ListWeightLimitItems = contentMessage.ContentFull?.Split('~')
                ?.Select(c => new WeightLimitItem
                {
                    Name = c?.Split('|')[0].Trim(),
                    LimitsMessage = c?.Split('|')[1].Trim()
                })?.ToList()
            };
        }

        private MobileCMSContentRequest GetSDLContentRequest(MOBRequest request, string token)
        {
            return new MobileCMSContentRequest()
            {
                AccessCode = request.AccessCode,
                Application = request.Application,
                LanguageCode = request.LanguageCode,
                DeviceId = request.DeviceId,
                GroupName = "Mobile:Baggage",
                TransactionId = request.TransactionId,
                SessionId = request.TransactionId,
                Token = token
            };
        }


        private AdditionalMeasuresBagFeeDetails AdditionalMeasuresBagFeeDetails(Collection<Detail> details)
        {

            var additionalBagDetails = new List<AdditionalBagDetails>();
            var additionalBag = AdditionalBagDetailItem(details.FirstOrDefault(d => d.Code == "AdditionalBag"));
            if (additionalBag != null)
            {
                additionalBagDetails.Add(additionalBag);
            }

            var overSizeBag = AdditionalBagDetailItem(details.FirstOrDefault(d => d.Code == "OverSizeBag"));
            if (overSizeBag != null)
            {
                additionalBagDetails.Add(overSizeBag);
            }

            var overWeightBag = AdditionalBagDetailItem(details.FirstOrDefault(d => d.Code == "OverWeightBag"));
            if (overWeightBag != null)
            {
                var overWeightHeavyBag = AdditionalBagDetailItem(details.FirstOrDefault(d => d.Code == "OverWeightHeavyBag"));
                if (overWeightHeavyBag != null && overWeightHeavyBag.BagFeeDetails != null)
                {
                    overWeightBag.BagFeeDetails.AddRange(overWeightHeavyBag.BagFeeDetails);
                }
                additionalBagDetails.Add(overWeightBag);
            }

            var specialItem = AdditionalBagDetailItem(details.FirstOrDefault(d => d.Code == "SpecialItem"));
            if (specialItem != null)
            {
                additionalBagDetails.Add(specialItem);
            }

            return new AdditionalMeasuresBagFeeDetails
            {
                PageTitle = _configuration.GetValue<string>("PageTitleAdditionalFee"),
                AdditionalAndOverSizeOverWeightBagDetails = additionalBagDetails,
                MeasuresCautionMessage = GetBAGMISCText(details, "BAGMISC_1")
            };
        }

        private AdditionalBagDetails AdditionalBagDetailItem(Detail detail)
        {
            if (detail == null) { return null; }
            return new AdditionalBagDetails
            {
                Title = detail.SubDetails?.FirstOrDefault(sd => sd.Code == "BAGHEADER")?.Description ?? "",
                BagFeeDetails = new List<OverSizeWeightBagFeeDetails>
                {
                    new OverSizeWeightBagFeeDetails
                    {
                        PriceInfo = detail.Presentation,
                        BagInfo = detail.SubDetails?.FirstOrDefault(sd => sd.Code == "BAGDIMENSIONS")?.Description ?? ""
                    }
                },
                ImageUrl = detail.SubDetails?.FirstOrDefault(sd => sd.Code == "BAGIMAGE")?.Description ?? ""
            };
        }

        private string GetBAGMISCText(Collection<Detail> content, string key)
        {
            return GetDetailForKey(content, "BAGMISC", key)?.Description?.Trim();
        }
        private Characteristic GetDetailForKey(Collection<Detail> content, string key, string subKey, string segmentId = null)
        {
           return segmentId.IsNullOrEmpty()
                ? content.FirstOrDefault(d => d.Code == key)?.SubDetails?.FirstOrDefault(d => d?.Code == subKey)
                : content.FirstOrDefault(d => d.Code == key && d.Association.SegmentRefIDs.Any(refId => refId == segmentId))?.SubDetails?.FirstOrDefault(d => d?.Code == subKey);
        }

        private async Task<List<BaggageFeesPerSegment>> ListBaggageFeesPerSegment(Content bagOffer, string classOfService, string loyaltyLevel)
        {
            var memberTextForBag = await GetMemberTextForBag(loyaltyLevel);
            var checkedBagEligibilityInfo = CheckedBagEligibilityInfo(bagOffer, loyaltyLevel);
            var listOfSegments = new List<BaggageFeesPerSegment>
            {
                new BaggageFeesPerSegment
                {
                    CabinName = GetCabinName(classOfService),
                    subTitle = GetBAGMISCText(bagOffer.Details, "BAGMISC_6") + ": " + memberTextForBag,
                    CheckedBagEligibilityInfo = checkedBagEligibilityInfo,
                    ListFareItems = GetBagItems(bagOffer),
                    DiscountRuleMessage = GetBAGMISCText(bagOffer.Details, "BAGMISC_4"),
                    DefaultCheckInBagDimensionsMessage = GetBAGMISCText(bagOffer.Details, "BAGMISC_3")
                }
            };
            var bagItemForBE = GetBagItems(bagOffer, isBe: true);
            if (!bagItemForBE.IsListNullOrEmpty())
            {
                var basicEconomyBaggageFeesPerSegment = new BaggageFeesPerSegment
                {
                    CabinName = "Basic Economy cabin",
                    subTitle = GetBAGMISCText(bagOffer.Details, "BAGMISC_6") + ": " + memberTextForBag,
                    CheckedBagEligibilityInfo = checkedBagEligibilityInfo,
                    ListFareItems = GetBagItems(bagOffer, true),
                    DiscountRuleMessage = GetBAGMISCText(bagOffer.Details, "BAGMISC_4"),
                    DefaultCheckInBagDimensionsMessage = GetBAGMISCText(bagOffer.Details, "BAGMISC_3")
                };
                listOfSegments.Add(basicEconomyBaggageFeesPerSegment);
            }
            return listOfSegments;
        }

        private List<FareItems> GetBagItems(Content bagOffer, bool isBe = false, string segmentId = null)
        {
            var firstBag = BagChargesItem(bagOffer?.Details, isBe ? "BEFirstCheckedBag" : "FirstCheckedBag", segmentId);
            if (firstBag == null) { return null; }

            var bagItems = new List<FareItems>();
            if (_configuration.GetValue<bool>("DevMockCarryONPersonalItem"))
            {
                bagItems.AddRange(
                                    new List<FareItems>() {
                                            new FareItems() { MessageText = "1 allowed", MessageTextMiles = "1 allowed",Title = "Personal item", SubTitle ="Fits underneath the seat in front"},
                                            new FareItems() { MessageText = isBe ? "Not allowed" : "1 allowed", MessageTextMiles = isBe ? "Not allowed" : "1 allowed", Title = "Carry-on", SubTitle = "Fits in the over head bin"}
                                        }
                                );
            }
            if (firstBag != null)
            {
                bagItems.Add(firstBag);
            }
            var secondBag = BagChargesItem(bagOffer?.Details, isBe ? "BESecondCheckedBag" : "SecondCheckedBag", segmentId);
            if (secondBag != null)
            {
                bagItems.Add(secondBag);
            }
            var thirdBag = BagChargesItem(bagOffer?.Details, isBe ? "BEThirdCheckedBag" : "ThirdCheckedBag", segmentId);
            if (thirdBag != null)
            {
                bagItems.Add(thirdBag);
            }
            var weightPerBag = segmentId.IsNullOrEmpty()
                ? bagOffer.Details.FirstOrDefault(d => d.Code == "WPB")
                : bagOffer.Details.FirstOrDefault(d => d.Code == "WPB" && d.Association.SegmentRefIDs.Any(refid => refid == segmentId));
            if (weightPerBag != null)
            {
                var bagWtItem = new FareItems
                {
                    Title = weightPerBag.Message,
                    MessageText = weightPerBag.Presentation.ToUpper(),
                    MessageTextMiles = weightPerBag.Presentation.ToUpper(),
                    HideDivider = true
                };
                bagItems.Add(bagWtItem);
            }
            return bagItems;
        }

        private FareItems BagChargesItem(Collection<Detail> details, string key, string segmentId = null)
        {
            FareItems fareItem = null;
            if (details.Any(d => d.Code == key))
            {
                var originalPrice = GetDetailForKey(details, key, "Price", segmentId)?.Description?.ToInteger() ?? 0;
                var originalMiles = GetDetailForKey(details, key, "Miles", segmentId)?.Description?.ToInteger() ?? 0;
                var prepayPrice = GetDetailForKey(details, key, "PrePay_Price", segmentId)?.Description?.ToInteger() ?? 0;
                var prepayMiles = GetDetailForKey(details, key, "PrePay_Miles", segmentId)?.Description?.ToInteger() ?? 0;
                if (originalPrice > prepayPrice && prepayPrice != 0)
                {
                    fareItem = new FareItems
                    {
                        Title = details.FirstOrDefault(d => d.Code == key)?.Message,
                        MessageText = (GetDetailForKey(details, key, "PrePay_Price_In_Money", segmentId)?.Description ?? "") + "*",
                        MessageTextMiles = (GetDetailForKey(details, key, "PrePay_Price_In_Miles", segmentId)?.Description ?? "") + "*",
                        OriginalFareMessageText = GetDetailForKey(details, key, "Price_In_Money", segmentId)?.Description ?? "",
                        OriginalFareMessageTextMiles = "", //don't show strikethrough for miles
                        IsFaredItem = true,
                        IsDiscountedItem = true,
                        AccessibilityMessage = string.Format(_configuration.GetValue<string>("AccessibilityPrepayOrActualPriceMessage"), GetDetailForKey(details, key, "PrePay_Price_In_Money", segmentId)?.Description ?? ""),
                        AccessibilityMessageMiles = string.Format(_configuration.GetValue<string>("AccessibilityPrepayOrActualPriceMessage"), GetDetailForKey(details, key, "PrePay_Price_In_Miles", segmentId)?.Description ?? ""),
                        AccessibilityOriginalFare = string.Format(_configuration.GetValue<string>("AccessibilityOriginalPriceMessage"), GetDetailForKey(details, key, "Price_In_Money", segmentId)?.Description ?? ""),
                        AccessibilityOriginalFareMiles = "" //don't show strikethrough for miles
                    };
                }
                else
                {
                    fareItem = new FareItems
                    {
                        Title = details.FirstOrDefault(d => d.Code == key)?.Message,
                        MessageText = originalPrice > 0 ? GetDetailForKey(details, key, "Price_In_Money", segmentId)?.Description ?? "" : "Free",
                        MessageTextMiles = originalMiles > 0 ? GetDetailForKey(details, key, "Price_In_Miles", segmentId)?.Description ?? "" : "Free",
                        IsFaredItem = originalPrice > 0,
                        IsFree = originalPrice == 0,
                        AccessibilityMessage = originalPrice > 0 ? GetDetailForKey(details, key, "Price_In_Money", segmentId)?.Description ?? "" : "Free",
                        AccessibilityMessageMiles = originalMiles > 0 ? GetDetailForKey(details, key, "Price_In_Miles", segmentId)?.Description ?? "" : "Free"
                    };
                }
            }
            return fareItem;
        }
        
        private CheckedBagEligibilityInfo CheckedBagEligibilityInfo(Content bagOffer, string loyaltyLevel)
        {
            CheckedBagEligibilityInfo bagEligibilityInfo = null;
            var bagAllowanceCount = bagOffer.Details.FirstOrDefault(d => d.Code == "ALLOWNCEBAGCNT")?.SubDetails?.FirstOrDefault(d => d?.Code == "ALLOWNCEBAGCNT")?.Description?.ToInteger() ?? 0;
            var bagdisclaimerMessage = bagOffer.Details.FirstOrDefault(d => d.Code == "DISCCLM")?.SubDetails?.FirstOrDefault(d => d?.Code == "PRMDISCLAIMER")?.Description ?? "";
            if(loyaltyLevel == "PPC2" && !string.IsNullOrEmpty(bagdisclaimerMessage))
            {
                bagdisclaimerMessage = bagdisclaimerMessage.Replace("UnitedSM Presidential PlusSM", "UnitedSM Presidential PlusSM Business");
            }
            var bagdisclaimerImgUrl = bagOffer.Details.FirstOrDefault(d => d.Code == "DISCCLM")?.SubDetails?.FirstOrDefault(d => d?.Code == "IMAGEURL")?.Description ?? "";
            if (bagAllowanceCount > 0 && bagdisclaimerMessage.IsNotEmpty())
            {
                bagEligibilityInfo = new CheckedBagEligibilityInfo
                {
                    Header = bagAllowanceCount == 1 ? $"Check up to {GetWordForTheNumbers(bagAllowanceCount)} bag at no charge" : $"Check up to {GetWordForTheNumbers(bagAllowanceCount)} bags at no charge",
                    Message = bagdisclaimerMessage,
                    ImageUrl = bagdisclaimerImgUrl
                };
            }
            return bagEligibilityInfo;
        }

        private CardBenefitMessage ChaseAd(Content bagOffer)
        {
            CardBenefitMessage cardBenefitMessage = null;
            var details = bagOffer.Details.FirstOrDefault(d => d.Code == "CHASEADD")?.SubDetails;
            if (!details.IsListNullOrEmpty())
            {
                cardBenefitMessage = new CardBenefitMessage
                {
                    Header = details.FirstOrDefault(d => d.Code == "CHASE_1")?.Description,
                    Message = details.FirstOrDefault(d => d.Code == "CHASE_2")?.Description,
                    ButtonText = details.FirstOrDefault(d => d.Code == "CHASE_3")?.Description,
                    ImageUrl = details.FirstOrDefault(d => d.Code == "CHASE_4")?.Description,
                    ButtonLink = details.FirstOrDefault(d => d.Code == "CHASE_5")?.Description
                };
            }

            return cardBenefitMessage;
        }

        private async Task<DynamicOfferRequest> BuildRequestForAnyFlightCheckedBagInfoCCE(DOTCheckedBagCalculatorRequest request)
        {
            return new DynamicOfferRequest()
            {
                Requester = new Requester()
                {
                    Requestor = new Requestor()
                    {
                        ChannelName = _configuration.GetValue<string>("MerchandizeOffersCSLServiceChannelName").Trim(),
                        LanguageCode = "en-US"
                    }
                },
                RequestData = new RequestData()
                {
                    Filters = new Collection<Service.Presentation.ProductRequestModel.ProductFilter>()
                    {
                        new Service.Presentation.ProductRequestModel.ProductFilter()
                        {
                            IsIncluded = true.ToString(),
                            ProductCode = "BAG",
                            SubProductCode = "BAGPOLICYCALC"
                        }
                    },
                    Travelers = new Collection<ProductTraveler>
                    {
                        new ProductTraveler
                        {
                            ID = "1",
                            PassengerTypeCode = GetPassengerTypeCode(request.LoyaltyLevel),
                            ProductLoyaltyProgramProfile = new Collection<ProductTravelerLoyaltyProfile>
                            {
                                new ProductTravelerLoyaltyProfile
                                {
                                    CreditCards = GetCreditCards(request.LoyaltyLevel),
                                    IsPrimary = true.ToString(),
                                    LoyaltyProgramCarrierCode = "UA",
                                    LoyaltyProgramMemberTierDescription = GetLoyaltyProgramMemberTierDescription(request.LoyaltyLevel),
                                    LoyaltyProgramMemberTierLevel = GetLoyaltyProgramMemberTierLevel(request.LoyaltyLevel),
                                }
                            },
                            TicketingDate = request.TicketingDate
                        }
                    },
                    FlightSegments = new Collection<ProductFlightSegment>
                    {
                        new ProductFlightSegment
                        {
                            ArrivalAirport = new Service.Presentation.CommonModel.AirportModel.Airport { IATACode = request.ArrivalCode },
                            BookingClasses = new Collection<BookingClass>
                            {
                                new BookingClass
                                {
                                    Code = request.ClassOfService
                                }
                            },
                            ClassOfService = request.ClassOfService,
                            DepartureAirport = new Service.Presentation.CommonModel.AirportModel.Airport { IATACode = request.DepartureCode },
                            DepartureDateTime = request.DepartDate,
                            ID = "1",
                            MarketedFlightSegment = new Collection<MarketedFlightSegment>
                            {
                                new MarketedFlightSegment
                                {
                                    MarketingAirlineCode = request.MarketingCarrierCode.IsNullOrEmpty() ? "UA" : request.MarketingCarrierCode
                                }
                            },
                            OperatingAirlineCode = request.MarketingCarrierCode.IsNullOrEmpty() ? "UA" : request.MarketingCarrierCode
                        }
                    }
                }
            };
        }

        private static string GetPassengerTypeCode(string loyaltyLevel)
        {
            return (loyaltyLevel == "MIR" || loyaltyLevel == "MIL") ? loyaltyLevel : "ADT";
        }

        private string GetLoyaltyProgramMemberTierLevel(string loyaltyLevel)
        {
            var loyaltyLevelType = GetLoyaltyProgramMemberTierDescription(loyaltyLevel); ;
            var description = loyaltyLevelType.ToString();
            if (loyaltyLevelType == LoyaltyProgramMemberTierLevel.StarGold || loyaltyLevelType == LoyaltyProgramMemberTierLevel.StarSilver)
            {
                description = loyaltyLevel;
            }
            return description;
        }

        private Collection<CreditCard> GetCreditCards(string loyaltyLevel)
        {
            var isCreditCard = new List<string> { "MEC", "MT", "CCC", "PPC", "PPC1", "PPC2", "UBC", "UCC" }.Any(str => str == loyaltyLevel);
            var cardType = "";
            
            if (isCreditCard)
            {
                cardType = loyaltyLevel.Trim() == "PPC1" || loyaltyLevel.Trim() == "PPC2"
                    ? "PPC" : loyaltyLevel.Trim(); //CCE DO or MERCH Request only accept "PPC" for United presidential plus or United presidential plus Business card

            }
            return isCreditCard ? new Collection<CreditCard> { new CreditCard { CardType = cardType.Trim() } } : null;
        }

        private LoyaltyProgramMemberTierLevel GetLoyaltyProgramMemberTierDescription(string loyaltyLevel)
        {
            if (loyaltyLevel == "StarAllianceGold")
            {
                loyaltyLevel = "StarGold";
            }

            if (loyaltyLevel == "StarAllianceSilver")
            {
                loyaltyLevel = "StarSilver";
            }
            Enum.TryParse(loyaltyLevel, out LoyaltyProgramMemberTierLevel tierLevel);
            return tierLevel;
        }

        private string GetCabinName(string selectedClassofService)
        {
            string[] classOfServices = _configuration.GetSection("DOTBagCalculatorClassOfServices").Value.Split('|');
            return classOfServices.FirstOrDefault(c => c.Split('~')[0] == selectedClassofService).Split('~')[1] + " cabin";
        }

        private async Task<string> GetMemberTextForBag(string statusLevel)
        {
            var isNewCreditCard = await _featureSettings.GetFeatureSettingValue("BagCal_AddNewMPCards").ConfigureAwait(false);
            if (isNewCreditCard)
            {
                return statusLevel switch
                {
                    "GeneralMember" => "General and non-members",
                    "PremierSilver" => "Premier Silver members",
                    "PremierGold" => "Premier Gold members",
                    "PremierPlatinum" => "Premier Platinum members",
                    "Premier1K" => "Premier 1K members",
                    "GlobalServices" => "Global Services members",
                    "StarAllianceSilver" => "Star Alliance Silver members",
                    "StarAllianceGold" => "Star Alliance Gold members",
                    "MEC" => "United Explorer Card members",
                    "MT" => "United Quest Card members",
                    "CCC" => "United Club Infinite Card members",
                    "PPC1" => "United Presidential Plus Card members",
                    "PPC2" => "United Presidential Plus Business Card members",
                    "UBC" => "United Business Card members",
                    "UCC" => "United Club Business Card members",
                    "MIR" => "U.S. Military on orders or relocating",
                    "MIL" => "U.S. Military personal travel",
                    _ => "",
                };
            }

            return statusLevel switch
            {
                "GeneralMember" => "General and non-members",
                "PremierSilver" => "Premier Silver members",
                "PremierGold" => "Premier Gold members",
                "PremierPlatinum" => "Premier Platinum members",
                "Premier1K" => "Premier 1K members",
                "GlobalServices" => "Global Services members",
                "StarAllianceSilver" => "Star Alliance Silver members",
                "StarAllianceGold" => "Star Alliance Gold members",
                "MEC" => "MileagePlus Explorer Card members",
                "CCC" => "MileagePlus Club Card members",
                "PPC" => "Presidental Plus Card members",
                "MIR" => "U.S. Military on orders or relocating",
                "MIL" => "U.S. Military personal travel",
                _ => "",
            };
        }
        #endregion

        #region CheckedBagEstimatesForMyFlight
        public async Task<DOTCheckedBagCalculatorResponse> CheckedBagEstimatesForMyFlight(DOTCheckedBagCalculatorRequest request)   
        {
            var session = new Session();
            if (request.Flow == FlowType.BOOKING.ToString())
            {
                session = _sessionHelperService.GetSession<Session>(_headers.ContextValues.SessionId, session.ObjectName, new List<string> { _headers.ContextValues.SessionId, session.ObjectName }).Result;
            }
            else if (request.Flow == FlowType.BAGGAGECALCULATOR.ToString())
            {
                session = await _shoppingSessionHelper.CreateShoppingSession(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, request.Application.Version.Major.ToString(), request.TransactionId, null, null, false);
                request.SessionId = session.SessionId;
                _headers.ContextValues.SessionId = session.SessionId;
            }
            var offerRequest = await BuildRequestForMyFlightCheckedBagInfoCCE(request, session.Token);
            //_logger.LogInformation("CheckedBagEstimatesForMyFlight-GetDynamicOffers {cslRequest} {TransactionId}", JsonConvert.SerializeObject(offerRequest), request.TransactionId);

            var token = (request.Flow == FlowType.BOOKING.ToString()) ? session.Token : await _tokenService.GetAnonymousToken(_headers.ContextValues.Application.Id, _headers.ContextValues.DeviceId, _configuration);

            var jsonResponse = "";
            if (!await _featureSettings.GetFeatureSettingValue("EnableCCEDynamicOfferUrls").ConfigureAwait(false))
            {
                jsonResponse = await _shoppingCcePromoService.DynamicOffers(token, JsonConvert.SerializeObject(offerRequest), request.TransactionId);
            }
            else
            {
                jsonResponse = await _cceService.GetDynamicOffers(token, JsonConvert.SerializeObject(offerRequest));
            }
            //_logger.LogInformation("CheckedBagEstimatesForMyFlight-GetDynamicOffers {cslResponse} {TransactionId}", jsonResponse, request.TransactionId);

            var offerResponse = JsonConvert.DeserializeObject<DynamicOfferResponse>(jsonResponse);
            ResponseData bagResponseData = JsonConvert.DeserializeObject<ResponseData>(offerResponse.ResponseData.ToString());

            var bagOffer = bagResponseData.InlineOffers?.FirstOrDefault(item => item?.Code == "BAG");
            var tripCount = bagResponseData.Itinerary.FirstOrDefault().ODOptions.Count();
            List<CMSContentMessage> sdlContent = await GetSDLContentForCheckedBags(request, token);
            if (request.Flow == FlowType.BOOKING.ToString())
            {
                (List<MemberShipStatus> listPremierStatusLevel, MemberShipStatus selectedPremierLevelStatus) = await GetLoyaltyLevelInfoAsync(request);
                return new DOTCheckedBagCalculatorResponse()
                {
                    Request = request,
                    LoyaltyLevels = listPremierStatusLevel,
                    LoyaltyLevelSelected = selectedPremierLevelStatus,
                    PageTitle = GetBAGMISCText(bagOffer.Details, "BAGMISC_7"),
                    ListBaggageFeesPerSegment = bagResponseData.Itinerary.FirstOrDefault().ODOptions.Select((od, index) => ListBaggageFeesPerSegmentMyFlight(bagOffer, od, (tripCount - 1) == index, request.Flow, false).ConfigureAwait(false).GetAwaiter().GetResult()).ToList(),
                    Captions = GetCaptions(bagOffer, request.LoyaltyLevel),
                    SessionId = request.SessionId,
                    CartId = request.CartId
                };
            }
            else
            {
                bool enableMfopForBags = await IsMFopBagsEnabled(request.Application).ConfigureAwait(false);
                var prepaidBagCTA = bagOffer.Details?.FirstOrDefault(d => d.Code == "CTA")?.Presentation?.Trim();
                var hasMiles = false;
                if (enableMfopForBags)
                {
                    hasMiles = bagOffer.Details.Any(d => (d.Code == "FirstCheckedBag" || d.Code == "SecondCheckedBag") && d.SubDetails.Any(sd => sd.Code == "Miles" && (sd.Description?.ToInteger() ?? 0) > 0));
                }

                return new DOTCheckedBagCalculatorResponse
                {
                    Request = request,
                    ConfirmationNumMessage = $"{GetBAGMISCText(bagOffer.Details, "BAGMISC_8")}: {offerResponse.BookingReferenceId}",
                    FeeCollectMessage = GetBAGMISCText(bagOffer.Details, "BAGMISC_5"),
                    PageTitle = GetBAGMISCText(bagOffer.Details, "BAGMISC_7"),
                    CardBenefitMessage = ChaseAd(bagOffer),
                    ListBaggageFeesPerSegment = bagResponseData.Itinerary.FirstOrDefault().ODOptions.Select((od, index) => ListBaggageFeesPerSegmentMyFlight(bagOffer, od, (tripCount - 1) == index, request.Flow, hasMiles).ConfigureAwait(false).GetAwaiter().GetResult()).ToList(),
                    AdditionalBagfareDetailsButtonTitle = _configuration.GetValue<string>("ButtonTextAdditionalBags"),
                    AdditionalMeasuresBagFeeDetails = AdditionalMeasuresBagFeeDetails(bagOffer.Details),
                    AdditionalBagLimitsButtonTitle = _configuration.GetValue<string>("ButtonTextSizeWeightBags"),
                    BagSizeAndWeightLimits = BagSizeAndWeightLimits(sdlContent),
                    CarryOnBagPolicyDetails = GetViewCarryOnBagPolicies(sdlContent),
                    Captions = new List<MOBItem> { new MOBItem { Id = "ShowPricesInMiles", CurrentValue = enableMfopForBags ? GetDetailForKey(bagOffer.Details, "BAGMILESCONTENT", "MILES_HEADER")?.Description?.Trim() : "" } },
                    TermsAndConditions = GetBagsTermsAndConditions(sdlContent, hasMiles, enableMfopForBags),
                    FaqButtonTitle = _configuration.GetValue<string>("ButtonTextFAQs"),
                    Faq = BagFaq(offerResponse.DynamicContent),
                    PrepayBagButtonText = prepaidBagCTA,
                    PrepayBagButtonUrl = string.IsNullOrEmpty(prepaidBagCTA) ? "" : GetActionUrl(offerResponse.BookingReferenceId, bagResponseData.Travelers.FirstOrDefault().Surname, offerResponse.CorrelationId),
                    SessionId = request.SessionId,
                    CorrelationId = offerResponse.CorrelationId
                };
            }
        }

        private async Task<bool> IsMFopBagsEnabled(MOBApplication application)
        {
            return (await _featureSettings.GetFeatureSettingValue("EnableMfopForBags").ConfigureAwait(false)) &&
            GeneralHelper.IsApplicationVersionGreaterorEqual(application.Id, application.Version.Major, _configuration.GetValue<string>("AndroidMilesFopBagsVersion"), _configuration.GetValue<string>("iPhoneMilesFopBagsVersion"));
        }   

        private MOBMobileCMSContentMessages GetBagsTermsAndConditions(List<CMSContentMessage> messages, bool hasMiles, bool enableMfopForBags)
        {
            var bagTnC = messages?.FirstOrDefault(x => x.Title == "BAGGAGE.PrepayForCheckedBags.TermsAndConditions.Bag");
            var milesFopTnC = hasMiles ? "<br>" + (messages?.FirstOrDefault(x => x.Title == "BAGGAGE.TermsAndConditions.MilesFOP")?.ContentFull?.Trim() ?? "") : "";

            return !bagTnC.IsNullOrEmpty()  ? 
                new MOBMobileCMSContentMessages
                {
                    Title = "Terms and conditions",
                    HeadLine = enableMfopForBags ? "" : bagTnC.Headline,
                    ContentFull = bagTnC.ContentFull + milesFopTnC,
                    ContentShort = bagTnC.ContentShort
                } : null;
        }

        private string GetActionUrl(string recordLocator, string lastName, string correlationId)
        {
            string url = null;
            if (!(string.IsNullOrEmpty(recordLocator) && string.IsNullOrEmpty(lastName) && string.IsNullOrEmpty(correlationId)))
            {
                var baseUrl = _configuration.GetValue<string>("Dotcom3dot0PrepayBagBaseUrl");
                var encryptFormat = $"{{'Pnr': '{recordLocator}', 'LastName': '{lastName}'}}";
                url = baseUrl + EncryptString(encryptFormat) + $"/{correlationId}";
            }
            return url;
        }

        private string EncryptString(string data)
        {
            var encryptedString = SecureData.EncryptString(data);
            return HttpUtility.UrlEncode(encryptedString);
        }
        private MOBMobileCMSContentMessages GetViewCarryOnBagPolicies(List<CMSContentMessage> viewCarryOnPolicy)
        {
            var carryOnBagPolicies = viewCarryOnPolicy?.FirstOrDefault(x => x.Title == "BAGGAGE.PrepayForCheckedBags.CarryOnBagsPolicy");
            return !carryOnBagPolicies.IsNullOrEmpty() ?
                new MOBMobileCMSContentMessages
                {
                    Title = "Carry-on bag policies",
                    HeadLine = carryOnBagPolicies.Headline,
                    ContentFull = carryOnBagPolicies.ContentFull,
                    ContentShort = carryOnBagPolicies.ContentShort
                } : null;
        }
        private static List<MOBItem> GetCaptions(Content bagOffer, string loyaltyLevel)
        {
            var message = bagOffer?.Details?.FirstOrDefault(d => d?.Code == "RTIBaggageData")?.SubDetails?.FirstOrDefault(d => d?.Code?.Contains("CheckBagMessage", StringComparison.CurrentCultureIgnoreCase) ?? false)?.Description;
            if (!string.IsNullOrEmpty(message) && loyaltyLevel == "PPC2")
            {
                message = message.Replace("UnitedSM Presidential PlusSM", "UnitedSM Presidential PlusSM Business");
                message = message.Replace("United<sup><small>SM</small></sup> Presidential Plus<sup><small>SM</small></sup>", "United<sup><small>SM</small></sup> Presidential PlusS<sup><small>SM</small></sup> Business");
            }
            return new List<MOBItem>()
                {

                    new MOBItem(){Id="StatusOrMembershipHeader",CurrentValue="Select status or membership"},
                    new MOBItem(){Id="Message",CurrentValue=bagOffer?.Details?.FirstOrDefault(d => d?.Code=="RTIBaggageData")?.SubDetails?.FirstOrDefault(d => d?.Code?.ToUpper() == "CHECKBAGCHARGESVARIENTMESSAGE")?.Description},
                    new MOBItem(){Id="FareSectionHeader",CurrentValue=bagOffer?.Details?.FirstOrDefault(d => d?.Code=="RTIBaggageData")?.SubDetails?.FirstOrDefault(d => d?.Code?.ToUpper() == "CHECKBAGCHARGESVARIENTMESSAGE")?.Value},
                    new MOBItem(){Id="StatusOrMemberMessage",
                        CurrentValue= message}

                };
        }

        private async Task<(List<MemberShipStatus>, MemberShipStatus)> GetLoyaltyLevelInfoAsync(DOTCheckedBagCalculatorRequest request)
        {
            List<MemberShipStatus> LoyaltyLevels = await GetMemberShipStatuses();
           
            return (LoyaltyLevels, GetLoyaltyLevelSelected(request, LoyaltyLevels));

        }

        private static List<MemberShipStatus> GetLoyaltyLevelList()
        {
            //var isNewCreditCard = await _featureSettings.GetFeatureSettingValue("BagCal_AddNewMPCards").ConfigureAwait(false);

            return new List<MemberShipStatus>(){
            new MemberShipStatus("GeneralMember", "General Member", "1", ""),
            new MemberShipStatus("PremierSilver", "Premier Silver member", "2", "MileagePlus Premier® member"),
            new MemberShipStatus("PremierGold", "Premier Gold member", "2", "MileagePlus Premier® member"),
            new MemberShipStatus("PremierPlatinum", "Premier Platinum member", "2", "MileagePlus Premier® member"),
            new MemberShipStatus("Premier1K", "Premier 1K member", "2", "MileagePlus Premier® member"),
            new MemberShipStatus("GlobalServices", "Global Services member", "2", "MileagePlus Premier® member"),
            new MemberShipStatus("StarAllianceGold", "Star Alliance Gold member", "3", "Star Alliance status"),
            new MemberShipStatus("StarAllianceSilver", "Star Alliance Silver member", "3", "Star Alliance status"),
            new MemberShipStatus("MEC", "MileagePlus Explorer Card member", "4", "MileagePlus cardmember"),
            //new MemberShipStatus("OPC", "OnePass Plus Card member", "4", "MileagePlus cardmember"));
            new MemberShipStatus("CCC", "MileagePlus Club Card member", "4", "MileagePlus cardmember"),
            new MemberShipStatus("PPC", "Presidental Plus Card member", "4", "MileagePlus cardmember"),
            new MemberShipStatus("MIR", "U.S. Military on orders or relocating", "5", "Active U.S. military"),
            new MemberShipStatus("MIL", "U.S. Military personal travel", "5", "Active U.S. military")};
        }

        private MemberShipStatus GetLoyaltyLevelSelected(DOTCheckedBagCalculatorRequest request, List<MemberShipStatus> LoyaltyLevels)
        {
            var compareString = string.IsNullOrEmpty(request.LoyaltyLevel)
                ? GetEliteStatusValue(request.PremierStatusLevel)
                : request.LoyaltyLevel;
            var loyaltyLevelSelected = LoyaltyLevels.FirstOrDefault(l => l.LoyaltyTypeLevelCode.ToUpper() == compareString.ToUpper());
            
            return loyaltyLevelSelected ?? new MemberShipStatus("", "Status or Membership", "", "");
        }

        private async Task<BaggageFeesPerSegment> ListBaggageFeesPerSegmentMyFlight(Content bagOffer, ProductOriginDestinationOption od, bool isLast, string flow, bool hasMiles)
        {
            var bagItems = new List<FareItems>();
            var originDestinationItem = new FareItems
            {
                Title = od.FlightSegments.FirstOrDefault().DepartureDateTime.ToDateTime().ToString("ddd., MMM d, yyyy"),
                SubTitle = await GetTripNameCityStateAndAirportCode(od.FlightSegments),
                HideDivider = (flow != FlowType.BOOKING.ToString())
            };
            bagItems.Add(originDestinationItem);
            if (flow != FlowType.BOOKING.ToString())
            {
                var bagFeePerTravelerText = new FareItems { Title = "Bag fees per traveler" };
                bagItems.Add(bagFeePerTravelerText);
            }

            bagItems.AddRange(GetBagItems(bagOffer, false, od.FlightSegments.FirstOrDefault().ID));

            return new BaggageFeesPerSegment
            {
                ListFareItems = bagItems,
                DiscountRuleMessage = isLast ? GetDiscountRuleMessage(bagOffer.Details, hasMiles) : ""
            };
        }

        public string GetDiscountRuleMessage(Collection<Detail> details, bool hasMiles)
        {
            var milesDiscountRuleMsg = hasMiles ? (GetDetailForKey(details, "BAGMILESCONTENT", "MILES_FOOTER")?.Description?.Trim() ?? "") : "";
            return !milesDiscountRuleMsg.IsNullOrEmpty() ? milesDiscountRuleMsg : (GetBAGMISCText(details, "BAGMISC_4") ?? "");
        }

        private async Task<string> GetTripNameCityStateAndAirportCode(Collection<ProductFlightSegment> flightSegments)
        {
            var departCity = await _airportDynamoDB.GetAirportName(flightSegments.FirstOrDefault().DepartureAirport.IATACode, _headers.ContextValues.TransactionId);
            var arrivalCity = await _airportDynamoDB.GetAirportName(flightSegments.LastOrDefault().ArrivalAirport.IATACode, _headers.ContextValues.TransactionId);

            return $"{departCity} to {arrivalCity}";
        }

        private async Task<DynamicOfferRequest> BuildRequestForMyFlightCheckedBagInfoCCE(DOTCheckedBagCalculatorRequest request, string token = "")
        {
            var reservation = await GetCSLReservation(request, token);

            if (IsWaitListPNR(reservation?.FlightSegments, request?.Flow))
            {
                throw new MOBUnitedException(_configuration.GetValue<string>("BagsWaitlistGenericMessage"));
            }

            string premierLevel = request.LoyaltyLevel.IsNullOrEmpty() ? GetEliteStatusValue(request.PremierStatusLevel) : request.LoyaltyLevel;

            return new DynamicOfferRequest()
            {
                Requester = new Requester()
                {
                    Requestor = new Requestor()
                    {
                        ChannelName = _configuration.GetValue<string>("MerchandizeOffersCSLServiceChannelName").Trim(),
                        LanguageCode = "en-US"
                    }
                },
                RequestData = new RequestData()
                {
                    Characteristics = (request.Flow == FlowType.BOOKING.ToString()) ? GetCharacteristicsBooking(reservation, premierLevel) : GetCharacteristics(reservation, request.MileagePlusNumber, request.PartnerRPCIds),
                    Filters = new Collection<Service.Presentation.ProductRequestModel.ProductFilter>()
                    {
                        (request.Flow == FlowType.BOOKING.ToString()) ?
                        new Service.Presentation.ProductRequestModel.ProductFilter()
                        {
                            IsIncluded = true.ToString(),
                            ProductCode = "BAG"
                        }:
                        new Service.Presentation.ProductRequestModel.ProductFilter()
                        {
                            IsIncluded = true.ToString(),
                            ProductCode = "BAG",
                            SubProductCode = "BAGPOLICYCALC"
                        }
                    },
                    Travelers = ProductTravelers(reservation.Travelers),
                    FlightSegments = reservation.FlightSegments.Select(f => ProductFlightSegment(reservation.Prices, f, request.Flow)).ToCollection(),
                    ODOptions = ProductOriginDestinationOptions(reservation.FlightSegments),
                    Solutions = Solutions(reservation.FlightSegments),
                    ReservationReferences = (request.Flow != FlowType.BOOKING.ToString()) ? ReservationReferences(reservation) : null
                }
            };
        }

        private bool IsWaitListPNR(Collection<ReservationFlightSegment> flightSegments, string flow)
        {
            if (flightSegments.IsNullOrEmpty())
            {
                return false;
            }

            if (flow != FlowType.BOOKING.ToString() && flow != FlowType.POSTBOOKING.ToString())
            {
                string FlightSegmentTypeCodes = _configuration.GetValue<string>("flightSegmentTypeCode");
                if (FlightSegmentTypeCodes.IndexOf(flightSegments.FirstOrDefault().FlightSegment.FlightSegmentType.Substring(0, 2), StringComparison.Ordinal) == -1) return true;         
            }
            var flightSegmentCharacteristics = flightSegments.Where(t => t != null).SelectMany(t => t.Characteristic).ToCollection();
            return flightSegmentCharacteristics.Any(p => p != null && !p.Code.IsNullOrEmpty() && !p.Value.IsNullOrEmpty() && p.Code.Equals("Waitlisted", StringComparison.OrdinalIgnoreCase) && p.Value.Equals("True", StringComparison.OrdinalIgnoreCase));
        }

        private async Task<Service.Presentation.ReservationModel.Reservation> GetCSLReservation(DOTCheckedBagCalculatorRequest request, string token = "")
        {
            United.Service.Presentation.ReservationModel.Reservation reservation;
            var objectName = new ReservationDetail().GetType().FullName;
            if (request.Flow == FlowType.POSTBOOKING.ToString())
            {
                var objName = new United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse().GetType().FullName;
                reservation = (await _sessionHelperService.GetSession<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(request.SessionId, objName, new List<string>() { request.SessionId, objName }))?.Reservation;
                if (reservation == null)
                {
                    reservation = (await RetrivePnrDetails(request, request.RecordLocator, request.LastName))?.Detail;
                }
            }
            else if (request.Flow == FlowType.VIEWRES.ToString())
            {
                reservation = (await _sessionHelperService.GetSession<ReservationDetail>(request.SessionId, objectName, new List<string>() { request.SessionId, objectName }))?.Detail;
                if (reservation == null) throw new MOBUnitedException(_configuration.GetValue<string>("ViewResSessionExpiredMessage"));
                #region To Handle FareLock purchase completed flow
                var isFareLockPurchaseCompleted = _configuration.GetValue<bool>("enablePrepayBag") && reservation != null && await IsFareLockPurchasedFlow(reservation, request.SessionId);
                if (isFareLockPurchaseCompleted)
                {
                    var cslReservation = (await RetrivePnrDetails(request, reservation.ConfirmationID, reservation.Travelers.FirstOrDefault().Person.Surname));
                    reservation = cslReservation.Detail;
                    if (cslReservation != null)
                    {
                        await _sessionHelperService.SaveSession<ReservationDetail>(cslReservation, request.SessionId, new List<string> { request.SessionId, objectName }, objectName).ConfigureAwait(false);
                    }
                }
                #endregion
            }
            else if (request.Flow?.ToUpper() == FlowType.BOOKING.ToString())
            {
                var loadReservationAndDisplayCartRequest = new LoadReservationAndDisplayCartRequest
                {
                    CartId = request.CartId,
                    WorkFlowType = Services.FlightShopping.Common.FlightReservation.WorkFlowType.InitialBooking
                };
                string jsonRequest = JsonConvert.SerializeObject(loadReservationAndDisplayCartRequest);
                var loadReservationAndDisplayResponse = await _shoppingCartService.GetCartInformation<LoadReservationAndDisplayCartResponse>(token, "LoadReservationAndDisplayCart", jsonRequest, _headers.ContextValues.SessionId);
                reservation = loadReservationAndDisplayResponse.Reservation;
            }
            else
            {
                var cslReservation = (await RetrivePnrDetails(request, request.RecordLocator, request.LastName));
                reservation = cslReservation.Detail;
                if (cslReservation != null && !string.IsNullOrWhiteSpace(request.SessionId))
                {
                    await _sessionHelperService.SaveSession<ReservationDetail>(cslReservation, request.SessionId, new List<string> { request.SessionId, objectName }, objectName).ConfigureAwait(false);
                }
            };

            if (reservation == null)
            {
                _logger.LogInformation("CheckedBagEstimatesForMyFlight-GetCSLReservation {errorMessage} {TransactionId}", "Reservation is empty.", request.TransactionId);
            }

            return reservation;
        }
        private async Task<bool> IsFareLockPurchasedFlow(Service.Presentation.ReservationModel.Reservation reservation, string sessionId)
        {
            bool isFareLockPNR = reservation.Characteristic != null && reservation.Characteristic.Any(o => (o.Code != null) && o.Code.Equals("FARELOCK") && o.Value.Equals("TRUE"))
                  && reservation.Characteristic.Any(o => o.Code != null && o.Code.Equals("FARELOCK_DATE"));
            if (isFareLockPNR)
            {
                var objName = new United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse().GetType().FullName;
                var farelockReservationResponse = await _sessionHelperService.GetSession<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(sessionId, objName, new List<string>() { sessionId, objName });
                return farelockReservationResponse?.DisplayCart?.Characteristics?.Any(x => x?.Code?.Equals("PurchaseComplete") ?? false) ?? false;
            }
            return false;
        }
        private Collection<Characteristic> GetCharacteristics(Service.Presentation.ReservationModel.Reservation reservation, string mpNumber, string partnerRPCIds)
        {
            var ticketPriceRevenue = reservation.Characteristic?.FirstOrDefault(c => c.Description?.ToUpper() == "ITINTOTALFORCURRENCY")?.Value.Trim();
            var isAward = reservation.Type?.FirstOrDefault(a => a.Description == "ITIN_TYPE")?.Key == "AWARD";

            var characteristics = new Collection<Characteristic>()
            {
                new Characteristic() { Code = "TKT_PRICE", Value = ticketPriceRevenue },
                new Characteristic() { Code = "MILES_NEEDED", Value = isAward ? "Y" : "N" }
            };

            /* Moved out of MFOP MVP1 scope
            if (!string.IsNullOrEmpty(mpNumber))
            {
                characteristics.Add(new Characteristic() { Code = "Mp_accountNumber", Value = mpNumber });
            }

            if (!string.IsNullOrEmpty(partnerRPCIds))
            {
                var rpcChars = partnerRPCIds.Split('|').Select(
                        (rpcId, index) => new Characteristic { Code = "SignIn_MPRPC" + (index + 1), Value = rpcId });
                characteristics.AddRange(rpcChars);
            }
            */

            return characteristics;

        }
    
        private Collection<Characteristic> GetCharacteristicsBooking(Service.Presentation.ReservationModel.Reservation reservation, string overrideBagPolicyLoyaltyLevel)
        {
            if (overrideBagPolicyLoyaltyLevel == "PPC1" || overrideBagPolicyLoyaltyLevel == "PPC2")
            {
                overrideBagPolicyLoyaltyLevel = "PPC";
            }
            //var ticketPriceRevenue = reservation.Characteristic?.FirstOrDefault(c => c.Description?.ToUpper() == "ITINTOTALFORCURRENCY")?.Value.Trim();
            var isAward = reservation.Type?.Any(a => a.Key == "AWARD");
            string ticketPriceRevenue = reservation.Prices[0].Totals.FirstOrDefault(p => p.Name == "GrandTotalForCurrency")?.Amount.ToString();
            return new Collection<Characteristic>()
            {
                //new Characteristic() { Code = "TKT_PRICE", Value = ticketPriceRevenue },
                new Characteristic() { Code = "IsAwardReservation", Value = isAward.ToString() },
                new Characteristic() { Code = "IsRTIBagPolicy", Value =  "True" },
                new Characteristic() { Code = "OverrideBagPolicy", Value =  overrideBagPolicyLoyaltyLevel },
                new Characteristic() { Code = "IsEnabledThroughCCE", Value =  "True" }
            };
        }

        private Collection<ReservationReference> ReservationReferences(Service.Presentation.ReservationModel.Reservation reservation)
        {
            return new Collection<ReservationReference> { new ReservationReference {
                        ID = reservation.ConfirmationID,
                        PaxFares = reservation.Prices ?? new Collection<Price>{},
                        Remarks = reservation.Remarks?.Select(x => new Remark { Description = x.Description, DisplaySequence = x.DisplaySequence }).ToCollection(),
                        SpecialServiceRequests = reservation.Services?.Select(x => new United.Service.Presentation.CommonModel.Service { Description = x.Description }).ToCollection(),
                        ReservationType = GetReservationType(reservation.Type),
                        ReservationCreateDate = reservation.CreateDate
                }};
        }

        private void GetTKTPrice(Collection<Characteristic> characteristics, ref string ticketPriceRevenue, ref string ticketPriceAward, ref string currencyCode)
        {
            if (characteristics != null && characteristics.Count() > 0)
            {
                foreach (var characteristic in characteristics)
                {
                    if (characteristic != null && characteristic.Description != null && characteristic.Value != null)
                    {
                        switch (characteristic.Description.Trim().ToUpper())
                        {
                            case "ITINTOTALFORMILEAGE":
                                ticketPriceAward = characteristic.Value.Trim();
                                break;
                            case "ITINTOTALFORCURRENCY":
                                ticketPriceRevenue = characteristic.Value.Trim();
                                currencyCode = characteristic.Code != null ? characteristic.Code.Trim() : string.Empty;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
        private ReservationType GetReservationType(Collection<Service.Presentation.CommonModel.Genre> type)
        {
            if (type.Any(p => p.Description == "GROUP"))
                return ReservationType.GroupBooking;
            else if (type.Any(p => p.Key == "BT"))
                return ReservationType.Bulk;
            else if (type.FirstOrDefault(a => a.Description == "ITIN_TYPE")?.Key == "NONREVENUE")
                return ReservationType.NonRevenue;
            else
                return ReservationType.None;

        }
        public Collection<Service.Presentation.ProductRequestModel.Solution> Solutions(Collection<ReservationFlightSegment> flightSegments)
        {
            var segmentsWithOd = flightSegments.GroupBy(f => f.TripNumber);
            return new Collection<Service.Presentation.ProductRequestModel.Solution>
            {
                new Service.Presentation.ProductRequestModel.Solution
                {
                    ID = "SOL1",
                    ODOptions = segmentsWithOd.Select(od => new ProductOriginDestinationOption() {RefID = "OD" + od.Key}).ToCollection()
                }
            };
        }

        private Collection<ODOption> ProductOriginDestinationOptions(Collection<ReservationFlightSegment> flightSegments)
        {
            var segmentsWithOd = flightSegments.GroupBy(f => f.TripNumber);
            return segmentsWithOd
                    .Select(od => new ODOption()
                    {
                        ID = "OD" + od.Key,
                        FlightSegments = od.Select(q => new ProductFlightSegment { ID = q.FlightSegment.SegmentNumber.ToString(), RefID = q.FlightSegment.SegmentNumber.ToString() }).ToCollection()
                    }).ToCollection();
        }

        public Collection<ProductTraveler> ProductTravelers(Collection<United.Service.Presentation.ReservationModel.Traveler> travelers)
        {
            var i = 0;
            return travelers.Select(t => new ProductTraveler
            {
                DateOfBirth = t.Person.DateOfBirth,
                GivenName = t.Person.GivenName,
                ID = (++i).ToString(),
                IsSelected = true.ToString(),
                LoyaltyProgramProfile = t.LoyaltyProgramProfile,
                PassengerTypeCode = t.Person.Type,
                ReservationIndex = t.Person.Key,
                Sex = t.Person.Sex,
                Surname = t.Person.Surname,
                TicketingDate = TicketingDate(t.Tickets),
                TicketNumber = TicketedNumber(t.Tickets),
                TravelerNameIndex = t.Person.Key,
                Documents = Documents(t.Person.Documents),
                ProductLoyaltyProgramProfile = ProductLoyaltyProgramProfile(t.LoyaltyProgramProfile),
            }).ToCollection();
        }
        private Collection<ProductTravelerLoyaltyProfile> ProductLoyaltyProgramProfile(LoyaltyProgramProfile loyaltyProgramProfile)
        {
            if (loyaltyProgramProfile == null || string.IsNullOrWhiteSpace(loyaltyProgramProfile.LoyaltyProgramCarrierCode) || string.IsNullOrWhiteSpace(loyaltyProgramProfile.LoyaltyProgramMemberID))
                return null;

            return new Collection<ProductTravelerLoyaltyProfile>
                    {
                        new ProductTravelerLoyaltyProfile
                        {
                            LoyaltyProgramCarrierCode = loyaltyProgramProfile.LoyaltyProgramCarrierCode,
                            LoyaltyProgramMemberID = loyaltyProgramProfile.LoyaltyProgramMemberID,
                            LoyaltyProgramMemberTierLevel=loyaltyProgramProfile.LoyaltyProgramMemberTierLevel,
                            LoyaltyProgramMemberTierDescription=loyaltyProgramProfile.LoyaltyProgramMemberTierDescription,
                            LoyaltyProgramID=loyaltyProgramProfile.LoyaltyProgramID
                        }
                    };
        }

        private Collection<United.Service.Presentation.PersonModel.Document> Documents(Collection<United.Service.Presentation.PersonModel.Document> documents)
        {
            if (documents == null || !documents.Any())
                return null;

            var ktnDocument = documents.Where(h => h != null && h.KnownTravelerNumber != null).FirstOrDefault();
            if (ktnDocument == null || string.IsNullOrEmpty(ktnDocument.KnownTravelerNumber))
                return null;

            return new Collection<United.Service.Presentation.PersonModel.Document> { new United.Service.Presentation.PersonModel.Document { Type = DocumentType.NexusCard, KnownTravelerNumber = ktnDocument.KnownTravelerNumber } };
        }

        private ProductFlightSegment ProductFlightSegment(Collection<Price> prices, ReservationFlightSegment flightSegment, string flow)
        {
            return new ProductFlightSegment
            {
                ArrivalAirport = flightSegment.FlightSegment.ArrivalAirport,
                ArrivalDateTime = flightSegment.FlightSegment.ArrivalDateTime,
                BookingClasses = flightSegment.FlightSegment.BookingClasses,
                Characteristic = flightSegment.FlightSegment.Characteristic,
                ClassOfService = flightSegment.FlightSegment.BookingClasses[0].Code,
                DepartureAirport = flightSegment.FlightSegment.DepartureAirport,
                DepartureDateTime = flightSegment.FlightSegment.DepartureDateTime,
                FareBasisCode = ShopStaticUtility.GetFareBasisCode(prices, flightSegment.FlightSegment.SegmentNumber),
                FlightNumber = flightSegment.FlightSegment.FlightNumber,
                FlightSegmentType = (flow == FlowType.POSTBOOKING.ToString() && flightSegment.FlightSegment.FlightSegmentType.Equals("NN")) ? "HK1" : flightSegment.FlightSegment.FlightSegmentType,
                IsActive = (!ShopStaticUtility.IsUsed(prices, flightSegment.FlightSegment.SegmentNumber)).ToString(),
                ID = flightSegment.FlightSegment.SegmentNumber.ToString(),
                IsInternational = flightSegment.FlightSegment.IsInternational,
                IsConnection = flightSegment.IsConnection,
                MarketedFlightSegment = flightSegment.FlightSegment.MarketedFlightSegment,
                OperatingAirlineCode = flightSegment.FlightSegment.OperatingAirlineCode,
                SegmentNumber = flightSegment.FlightSegment.SegmentNumber,
                TripIndicator = flightSegment.TripNumber,
                RefID = flightSegment.FlightSegment.SegmentNumber.ToString()
            };
        }

        public async Task<PrepayForCheckedBagsResponse> PrepayForCheckedBags(PrepayForCheckedBagsRequest request)
        {
            var response = new PrepayForCheckedBagsResponse();
            int checkedBagAllowance = 0;
            int bagsAlreadyPurchased = 0;           
            var session = new Session();
            session = _sessionHelperService.GetSession<Session>(request.SessionId, session.ObjectName, new List<string> { request.SessionId, session.ObjectName }).Result;
            if (session == null) { throw new MOBUnitedException(_configuration.GetValue<string>("ViewResSessionExpiredMessage"));}
            else
            {
                var dynamicOfferDetailRequest = await BuildCCEDynamicOfferDetailRequestForCheckedBags(session, request.Flow, request.CorrelationId, request.MileagePlusNumber, request.PartnerRPCIds);
                // _logger.LogInformation("PrepayForCheckedBags-GetDynamicOfferDetail {cslRequest} {TransactionId}", JsonConvert.SerializeObject(dynamicOfferDetailRequest), request.TransactionId);
                var jsonResponse = "";
                if (!await _featureSettings.GetFeatureSettingValue("EnableCCEDynamicOfferUrls").ConfigureAwait(false))
                {
                    jsonResponse = await _shoppingCcePromoService.MerchOffersCceDetails(session.Token, JsonConvert.SerializeObject(dynamicOfferDetailRequest), request.TransactionId);
                }
                else
                {
                    jsonResponse = await _cceDODService.GetCCEDynamicOffersDetail(session.Token, JsonConvert.SerializeObject(dynamicOfferDetailRequest));
                }
              //  _logger.LogInformation("PrepayForCheckedBags-GetDynamicOfferDetail {cslResponse} {TransactionId}", jsonResponse, request.TransactionId);

                var dynamicOfferDetailResponse = JsonConvert.DeserializeObject<DynamicOfferDetailResponse>(jsonResponse);

                if (dynamicOfferDetailResponse != null && dynamicOfferDetailResponse.Response?.Error == null &&
                                       dynamicOfferDetailResponse.ODOptions != null && dynamicOfferDetailResponse.ODOptions.Count > 0 &&
                                       dynamicOfferDetailResponse.Offers != null && dynamicOfferDetailResponse.Offers.Count > 0 &&
                                       dynamicOfferDetailResponse.FlightSegments != null && dynamicOfferDetailResponse.Travelers != null)
                {
                    var cceBagOffer = new GetCceBagOffers();
                    await _sessionHelperService.SaveSession<DynamicOfferDetailResponse>(dynamicOfferDetailResponse, request.SessionId, new List<string> { request.SessionId, cceBagOffer.ObjectName }, cceBagOffer.ObjectName);
                    var sdlContent = await GetSDLContentForCheckedBags(request, session.Token);
                    bool enableMfopForBags = await IsMFopBagsEnabled(request.Application).ConfigureAwait(false);

                    response.CartId = session.CartId;
                    response.Flow = request.Flow;
                    response.LanguageCode = request.LanguageCode;
                    response.TransactionId = request.TransactionId;
                    response.SessionId = request.SessionId;
                    response.ProductCode = "BAG";
                    response.Disclaimer = sdlContent?.FirstOrDefault(x => x.Title == "BAGGAGE.PrepayForCheckedBags.Disclaimer").ContentFull;
                    response.FlightSegments = new List<CheckBagFlightSegmentItem>(dynamicOfferDetailResponse.FlightSegments.Count);
                    foreach (ProductOriginDestinationOption oDOption in dynamicOfferDetailResponse.ODOptions)
                    {
                        if (oDOption?.FlightSegments?.Any(x => x?.TripIndicator?.Equals("C", StringComparison.InvariantCultureIgnoreCase) ?? false) ?? false) continue;
                        var segmentIds = string.Join("", oDOption.FlightSegments?.Select(segment => segment.ID))?.Trim();

                        var flightSegmentDetail = new CheckBagFlightSegmentItem
                        {
                            Id = segmentIds,
                            SubTitle = oDOption.FlightSegments?.FirstOrDefault()?.DepartureDateTime?.ToDateTime().ToString("ddd, MMM d, yyyy") ?? "",
                            Title = await GetTripNameCityStateAndAirportCode(oDOption.FlightSegments),
                            IneligibleLOFMessage = OAIneligibleMessage(sdlContent, oDOption, dynamicOfferDetailResponse.FlightSegments)

                        };
                        flightSegmentDetail.Travelers = new List<CheckBagTravelerItem>(dynamicOfferDetailResponse.Travelers.Count);
                        foreach (ProductTraveler traveler in dynamicOfferDetailResponse.Travelers)
                        {
                            int maxAllowedBags = _configuration.GetValue<int>("PrePayMaxAllowedBags");
                            var travelerDetail = new CheckBagTravelerItem();
                            travelerDetail.FullName = $"{traveler.GivenName.ToLower().ToPascalCase()} {traveler.Surname.ToLower().ToPascalCase()}";
                            travelerDetail.BagsCaption = _configuration.GetValue<string>("BagsCaptionForStepper");
                            travelerDetail.BaggageSummaryTravelerTitle = $"For {travelerDetail.FullName}";
                            travelerDetail.Id = traveler.ID;
                            travelerDetail.MinAllowedBags = 0;
                            travelerDetail.MaxAllowedBags = maxAllowedBags;
                            var productOptions = dynamicOfferDetailResponse.Offers?.FirstOrDefault()?.ProductInformation?.ProductDetails?.FirstOrDefault(proddetail => proddetail?.Product?.Code?.Equals("BAG") ?? false)?.Product;
                            if (productOptions != null && productOptions.SubProducts?.Count > 0)
                            {
                                var subProductOptions = productOptions.SubProducts.Where(subProduct => subProduct.Prices?.Exists(price => (string.Join("", price?.Association?.SegmentRefIDs)?.Trim() == segmentIds) && (price?.Association?.TravelerRefIDs?.Exists(travelerId => string.Equals(travelerId, traveler.ID)) ?? false)) ?? false)?.ToList();
                                travelerDetail.BaggageFees = new List<CheckBagBaggageFeeItem>(maxAllowedBags);
                                int baggageFeeItemIndex = 0;
                                int maxAllowedTravelerBags = 0;
                                int allowedBags = 0;
                                int subProductIndex = 0;
                                int totalBagsPurchased = 0;
                                bool isIncluded = false;
                                int travelerCheckBagAllowance = 0;
                                int travelerBagsAlreadyPurchased = 0;
                                foreach (SubProduct subProduct in subProductOptions)
                                {
                                    subProductIndex++;
                                    allowedBags = ((subProduct.Extension?.Bag?.MaximumQuantity ?? 0) -
                                                  (subProduct.Extension?.Bag?.MinimumQuantity ?? 0)) + 1;
                                    if (string.Equals(subProduct.Type?.Description?.ToUpper(), "ALLOWANCE") ||
                                        string.Equals(subProduct.Type?.Description?.ToUpper(), "INCLUDED"))
                                    {
                                        travelerDetail.MinAllowedBags += allowedBags;
                                        isIncluded = true;
                                        maxAllowedTravelerBags = allowedBags;
                                        switch (subProduct.Type?.Description?.ToUpper())
                                        {
                                            case "ALLOWANCE":
                                                travelerCheckBagAllowance += allowedBags;
                                                checkedBagAllowance += allowedBags;
                                                break;
                                            case "INCLUDED":
                                                travelerBagsAlreadyPurchased += allowedBags;
                                                bagsAlreadyPurchased += allowedBags;
                                                break;
                                        }
                                        totalBagsPurchased += allowedBags;
                                    }
                                    else
                                    {
                                        isIncluded = false;
                                        if (subProduct.Extension.Bag.MaximumQuantity > 2 && subProduct.Extension.Bag.MinimumQuantity > 2)
                                        {
                                            maxAllowedTravelerBags = maxAllowedBags;
                                        }
                                        else
                                        {
                                            maxAllowedTravelerBags = (subProduct.Extension?.Bag?.MaximumQuantity ?? 0) > maxAllowedBags ?
                                                                        maxAllowedBags : allowedBags;
                                        }
                                    }
                                    var amount = (subProduct?.Extension?.Bag?.Bags?.FirstOrDefault()?.PrePaidBagPrice?.FirstOrDefault(pp => pp.Price.BasePrice.FirstOrDefault().Type == "Money")?.Price?.BasePrice?.FirstOrDefault()?.Amount ??
                                                  subProduct?.Prices?.FirstOrDefault()?.PaymentOptions?.FirstOrDefault(po => po.Type == "Money")?.PriceComponents?.FirstOrDefault()?.Price?.BasePrice?.FirstOrDefault()?.Amount) ?? 0;

                                    var miles = 0.0;
                                    if (enableMfopForBags)
                                    {
                                        miles = (subProduct?.Extension?.Bag?.Bags?.FirstOrDefault()?.PrePaidBagPrice?.FirstOrDefault(pp => pp.Price.BasePrice.FirstOrDefault().Type == "Miles")?.Price?.BasePrice?.FirstOrDefault()?.Amount ??
                                                      subProduct?.Prices?.FirstOrDefault()?.PaymentOptions?.FirstOrDefault(po => po.Type == "Miles")?.PriceComponents?.FirstOrDefault()?.Price?.BasePrice?.FirstOrDefault()?.Amount) ?? 0;
                                    }
                                    for (int index = 0; index < maxAllowedTravelerBags; index++)
                                    {
                                        baggageFeeItemIndex++;
                                        maxAllowedBags--;
                                        if (!isIncluded)
                                        {
                                            travelerDetail.BaggageFees.Add(
                                               new CheckBagBaggageFeeItem()
                                               {
                                                   Amount = amount,
                                                   Miles = miles,
                                                   BagNumber = baggageFeeItemIndex,
                                                   IsIncluded = isIncluded,
                                                   ProductId = subProduct?.ID,
                                                   SubCode = subProduct?.Extension?.Bag?.Bags?.FirstOrDefault()?.SubCode ?? ""
                                               });
                                        }
                                    }
                                }

                                travelerDetail.MaxAllowedBags = travelerDetail.BaggageFees.Count;
                                travelerDetail.BagAllowance = travelerCheckBagAllowance;
                                travelerDetail.BagsPurchased = travelerBagsAlreadyPurchased;
                            }
                            flightSegmentDetail.Travelers.Add(travelerDetail);
                        }
                        response.FlightSegments.Add(flightSegmentDetail);
                    }
                    if (response.FlightSegments.Count == 0) throw new Exception("Trips are empty");
                    response.Captions = GetPrepayBagsCaptions(sdlContent);
                }
            }
            return response;
        }

        private string OAIneligibleMessage(List<CMSContentMessage> sdlContent, ProductOriginDestinationOption oDOption, Collection<ProductFlightSegment> flightSegments)
        {
            if (_configuration.GetValue<bool>("EnableOAIneligibleMessage"))
            {
                var unitedCarriers = _configuration.GetValue<string>("UnitedCarriers");
                var unitedCarriersList = !string.IsNullOrEmpty(unitedCarriers) ? unitedCarriers.Split(',').ToList() : new List<string>();
                var isUAoperated = unitedCarriersList != null && unitedCarriersList.Any(x => x == GetOperatingCarriers(oDOption, flightSegments));
                return isUAoperated ? "" : sdlContent?.FirstOrDefault(x => x.Title == "BAGGAGE.PrepayForCheckedBags.IneligibleLOFAlertMessage")?.ContentFull;
            }
            return "";
        }
        private string GetOperatingCarriers(ProductOriginDestinationOption oDOption, Collection<ProductFlightSegment> flightSegments)
        {
            var flightId = oDOption?.FlightSegments?.FirstOrDefault()?.ID;
            return flightSegments?.FirstOrDefault(c => c.ID == flightId)?.OperatingAirlineCode;
        }

        private List<MOBItem> GetPrepayBagsCaptions(List<CMSContentMessage> sdlContent)
        {

            return new List<MOBItem>()
            {
                 new MOBItem()
                 {
                    Id = "CheckedBagAllowance",
                    CurrentValue = sdlContent?.FirstOrDefault(x => x.Title == "BAGGAGE.PrepayForCheckedBags.Summary.CheckedBagAllowance")?.ContentFull
                 },
                 new MOBItem()
                 {
                    Id = "BagsAlreadyPurchased",
                    CurrentValue = sdlContent?.FirstOrDefault(x => x.Title == "BAGGAGE.PrepayForCheckedBags.Summary.BagsAlreadyPurchased")?.ContentFull
                 },
                 new MOBItem()
                 {
                    Id = "AdditionalBagsAlreadyPurchased",
                    CurrentValue = sdlContent?.FirstOrDefault(x => x.Title == "BAGGAGE.PrepayForCheckedBags.Summary.AdditionalBagsAlreadyPurchased")?.ContentFull
                 },
                 new MOBItem()
                 {
                    Id = "TotalBagsPurchase",
                    CurrentValue = sdlContent?.FirstOrDefault(x => x.Title == "BAGGAGE.PrepayForCheckedBags.Summary.Total")?.ContentFull
                 },
                 new MOBItem()
                 {
                    Id = "PageTitle",
                    CurrentValue = sdlContent?.FirstOrDefault(x => x.Title == "BAGGAGE.PrepayForCheckedBags.PageTitle")?.ContentFull
                 },
                 new MOBItem()
                 {
                    Id = "SaveButtonTitle",
                    CurrentValue = sdlContent?.FirstOrDefault(x => x.Title == "BAGGAGE.PrepayForCheckedBags.SaveButtonTitle")?.ContentFull
                 },
                 new MOBItem()
                 {
                    Id = "BaggageSummaryTitle",
                    CurrentValue =  sdlContent?.FirstOrDefault(x => x.Title == "BAGGAGE.PrepayForCheckedBags.Summary.Title")?.ContentFull
                 },
                 new MOBItem()
                 {
                    Id = "TotalCheckedBagsForLOF",
                    CurrentValue =  sdlContent?.FirstOrDefault(x => x.Title == "BAGGAGE.PrepayForCheckedBags.OtherOptions.TotalCheckedBagsForLOF")?.ContentFull
                 },
                 new MOBItem()
                 {
                    Id = "TravelerBagAllowance",
                    CurrentValue =  sdlContent?.FirstOrDefault(x => x.Title == "BAGGAGE.PrepayForCheckedBags.OtherOptions.TravelerBagAllowance")?.ContentFull
                 },
                 new MOBItem()
                 {
                    Id = "TotalBagsPurchasedForAllLOF",
                    CurrentValue =  sdlContent?.FirstOrDefault(x => x.Title == "BAGGAGE.PrepayForCheckedBags.OtherOptions.TotalBagsPurchasedForAllLOF")?.ContentFull
                 }
            };
            
        }
        private async Task<DynamicOfferDetailRequest> BuildCCEDynamicOfferDetailRequestForCheckedBags(Session session, string flow, string correlationId, string mpNumber, string partnerRPCIds)
        {
            DynamicOfferDetailRequest dynamicOfferRequest = null;
            var isAward = session?.IsAward ?? false;
            United.Service.Presentation.ReservationModel.Reservation reservation;
            if (flow == FlowType.POSTBOOKING.ToString())
            {
                var objectName = new United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse().GetType().FullName;
                reservation = (await _sessionHelperService.GetSession<United.Services.FlightShopping.Common.FlightReservation.FlightReservationResponse>(session.SessionId, objectName, new List<string>() { session.SessionId, objectName }))?.Reservation;
            }
            else
            {
                var objectName = new ReservationDetail().GetType().FullName;
                reservation = (await _sessionHelperService.GetSession<ReservationDetail>(session.SessionId, objectName, new List<string>() { session.SessionId, objectName }))?.Detail;
            }

            if (reservation != null)
            {
                Collection<Characteristic> characteristics = GetCharacteristics(reservation, mpNumber, partnerRPCIds);
                characteristics.Add(new Characteristic() { Code = "Context", Value = "TD" });
                characteristics.Add(new Characteristic() { Code = "IsAwardReservation", Value = isAward ? "False" : "True" });
                characteristics.Add(new Characteristic() { Code = "IsEnabledThroughCCE", Value = "True" });
                string merchChannel = "MBE";
                string channelId = string.Empty;
                string channelName = string.Empty;
                SetMerchandizeChannelValues(merchChannel, ref channelId, ref channelName);
                dynamicOfferRequest = new DynamicOfferDetailRequest()
                {
                    Characteristics = characteristics,
                    Filters = new Collection<Service.Presentation.ProductRequestModel.ProductFilter>()
                    {
                        new Service.Presentation.ProductRequestModel.ProductFilter()
                        {
                            IsIncluded = true.ToString(),
                            ProductCode = "BAG"
                        }
                    },
                    CountryCode = "US",
                    CurrencyCode = "USD",
                    IsAwardReservation = isAward ? "True" : "False",
                    TicketingCountryCode = "US",
                    Requester = new ServiceClient()
                    {
                        GUIDs = new Collection<UniqueIdentifier>()
                        {
                            new UniqueIdentifier()
                            {
                                ID = correlationId,
                                Name = "CorrelationId"
                            }
                        },
                        Requestor = new Requestor()
                        {
                            ChannelID = channelId,
                            ChannelName = channelName,
                            LanguageCode = "en"
                        }
                    },
                    Travelers = ProductTravelers(reservation.Travelers),
                    FlightSegments = reservation.FlightSegments.Select(f => ProductFlightSegment(reservation.Prices, f, flow)).ToCollection(),
                    ODOptions = ProductOriginDestinationOptionsFoCheckedBags(reservation.FlightSegments),
                    Solutions = Solutions(reservation.FlightSegments),
                    ReservationReferences = (flow != FlowType.BOOKING.ToString()) ? ReservationReferences(reservation) : null
                };
            }
            else
            {
                throw new Exception("Reservation is empty");
            }
            return dynamicOfferRequest;
        }

        private Collection<ProductOriginDestinationOption> ProductOriginDestinationOptionsFoCheckedBags(Collection<ReservationFlightSegment> flightSegments)
        {
            var segmentsWithOd = flightSegments.GroupBy(f => f.TripNumber);
            return segmentsWithOd
                    .Select(od => new ProductOriginDestinationOption()
                    {
                        ID = "OD" + od.Key,
                        FlightSegments = od.Select(q => new ProductFlightSegment { ID = q.FlightSegment.SegmentNumber.ToString(), RefID = q.FlightSegment.SegmentNumber.ToString() }).ToCollection()
                    }).ToCollection();
        }

        private async Task<List<CMSContentMessage>> GetSDLContentForCheckedBags(MOBRequest request, string token)
        {
            CSLContentMessagesResponse response = null;

            try
            {
                string sdlContentCacheKey = _configuration.GetValue<string>("CMSContentMessages_GroupName_Baggage_Messages") + ObjectNames.MOBCSLContentMessagesResponseFullName;
                var cmsContent = await _cachingService.GetCache<string>(sdlContentCacheKey, request.TransactionId).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(cmsContent))
                {
                    response = JsonConvert.DeserializeObject<CSLContentMessagesResponse>(cmsContent);
                }
                if (response != null && response.Messages != null) { return response.Messages; }

                MobileCMSContentRequest sdlRequest = GetSDLContentRequest(request, token);

                _logger.LogInformation("GetMobileCMSContents {@clientRequest} {request}", JsonConvert.SerializeObject(request), request.TransactionId);

                string sdlJsonResponse = await _cMSContentHelper.GETCSLCMSContent(sdlRequest);
                if (!sdlJsonResponse.IsNullOrEmpty())
                {
                     response = System.Text.Json.JsonSerializer.Deserialize<CSLContentMessagesResponse>(sdlJsonResponse);

                    _logger.LogInformation("GetMobileCMSContents {@clientCMSContentsResponse}", JsonConvert.SerializeObject(response, Formatting.None));

                    if (response?.Messages != null && (Convert.ToBoolean(response.Status)))
                    {
                        var saveSDL = await _cachingService.SaveCache<CSLContentMessagesResponse>(sdlContentCacheKey, response, request.TransactionId, new TimeSpan(1, 30, 0)).ConfigureAwait(false);
                        return response.Messages;                      
                    }
                    else
                    {
                        _logger.LogError("GetMobileCMSContentsData Error {Exception} and {TransactionId}", "GetMobileCMSContentsData Message list is empty or null after serialization.", request.TransactionId);
                    }
                }
                else
                {
                    _logger.LogError("GetMobileCMSContentsData Error {Exception} and {TransactionId}", "GetMobileCMSContentsData is empty or null.", request.TransactionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("GetMobileCMSContentsData Error {Exception} and {TransactionId}", ex.Message, request.TransactionId);
                throw new Exception("SDL content failed");
            }
            return null;
        }
        public async Task<bool> UpdateiOSTokenBaGCalcRedesign(int applicationId, string deviceId)
        {
            string tokenResponse = await _tokenService.GetAnonymousTokenV2(applicationId, deviceId, _configuration, "dpTokenRequest", true);
            if (tokenResponse.IsNullOrEmpty())
                return false;
            else
                return true;
        }
        #endregion
    }
   
}

